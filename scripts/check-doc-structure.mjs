#!/usr/bin/env node
/**
 * 文档结构检查（守门脚本）。
 *
 * 为什么需要它：
 *   文档体系里有一批**不会让编译或测试红灯**的结构规则：状态块、目录索引、ADR 必备章节、
 *   标题编号风格、代码块语言。它们只写在文档里，只有人记得时才成立。本脚本把这些规则
 *   变成 verify 的硬失败，与引用检查、重复检查、状态检查同思路。
 *
 * 八条规则（判据仍以文档为准，本脚本只做判定，不复述条款）：
 *   1. 状态块：`docs/` 下每篇文档开头必须有 `> 状态：` 与 `> 最后更新：YYYY-MM-DD`；
 *   2. 目录索引：`docs/README.md` 必须列出每个目录的 README；每个目录 README 必须列出
 *      本目录下的每篇文档 —— 索引是唯一入口，孤儿文档即缺索引；
 *   3. ADR 必备章节：`## 备选项`、`## 后果`、以及「重新评估」触发条件（历史 ADR 豁免）；
 *   4. 标题不编号：`docs/` 与 `AGENTS.md` 的正文标题不得用 `## 1.` / `## 一、` / `## 第 N 步：`；
 *   5. 架构文档骨架：`docs/10-architecture/` 下每篇文档的 H2 必须与 README 代码块里的八段
 *      完全一致（顺序与完整性），不适用的章节写「不适用」而不是删段；
 *   6. 代码块语言：开围栏必须带语言标记（纯文本用 `text`）；
 *   7. k-spider 专属：含待确认内容的架构文档必须登记到 `20-adr/README.md`；
 *      历史路径/命令模式不得复活；
 *   8. ADR 正文不承载进度：新 ADR 不得出现 `## 待办`；0004–0008 为旧模板历史豁免。
 *
 * 与状态检查的分工：本脚本只问「有没有、登记没登记、格式对不对」；**两处状态是否一致**、
 * 索引是否与正文对应、已实现规格是否补了验收结论，归 `check-doc-status.mjs`。
 *
 * 明确排除（不是漏检，是「不该判」）：
 *   - `docs/README.md` 是索引本身，不要求状态块；
 *   - `src/k-spider-dotnet-old/` 历史归档与 manifest 登记的上游技能不参与。
 *
 * 用法：
 *   node scripts/check-doc-structure.mjs
 *
 * 退出码：0 = 结构完整；1 = 存在问题（逐条列出文件与原因）。
 */

import { existsSync, readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  ADR_DIR,
  ADR_INDEX,
  DATE_PATTERN,
  DOCS_INDEX,
  HEAD_LINES,
  STATUS_PATTERN,
  createDocModel,
} from './lib/doc-model.mjs';

const ROOT_DIR = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const { docsFiles, adrFiles, toAbs, referencesOf } = createDocModel(ROOT_DIR);

/** 状态块豁免：索引自身。 */
const STATUS_EXEMPT = new Set([DOCS_INDEX]);

/** 早于 ADR 0004 格式要求的决策记录：保留原文，不追溯补章节。 */
const LEGACY_ADRS = new Set([
  `${ADR_DIR}/0001-admin-central-scheduling.md`,
  `${ADR_DIR}/0002-grpc-transport.md`,
  `${ADR_DIR}/0003-docs-and-skill-governance.md`,
]);

/** 旧模板遗留待办：仅 0004–0008 允许，正文不改；进度见 roadmap。 */
const LEGACY_TODO_ADRS = new Set([
  `${ADR_DIR}/0004-docs-and-skill-governance-v2.md`,
  `${ADR_DIR}/0005-engineering-gates-first.md`,
  `${ADR_DIR}/0006-frontend-static-gates.md`,
  `${ADR_DIR}/0007-ci-parallel-jobs.md`,
  `${ADR_DIR}/0008-grpc-contract-project.md`,
]);

/** 有 README 索引的文档目录：由实际文件推导，新增目录不能漏检。 */
const INDEXED_DIRS = [
  ...new Set(
    docsFiles
      .map((file) => file.split('/').slice(0, 2).join('/'))
      .filter((dir) => dir !== DOCS_INDEX && !dir.endsWith('.md')),
  ),
].sort();

/** 文档正文的 H2（代码块外），用于校验架构文档的实际章节。 */
const headingsOf = (file) => {
  const headings = [];
  let inFence = false;

  for (const line of readFileSync(toAbs(file), 'utf8').split(/\r?\n/)) {
    if (line.trim().startsWith('```')) {
      inFence = !inFence;
      continue;
    }
    if (inFence) continue;
    if (line.startsWith('## ')) headings.push(line.slice(3).trim());
  }

  return headings;
};

/** 从 README 的 ```markdown 代码块中读取骨架 H2，避免脚本里再抄一份骨架。 */
const codeBlockHeadings = (file) => {
  const headings = [];
  let inFence = false;
  let fenceLang = '';

  for (const line of readFileSync(toAbs(file), 'utf8').split(/\r?\n/)) {
    if (line.trim().startsWith('```')) {
      if (!inFence) {
        inFence = true;
        fenceLang = line.trim().slice(3).trim();
      } else {
        inFence = false;
        fenceLang = '';
      }
      continue;
    }
    if (inFence && fenceLang === 'markdown' && line.startsWith('## ')) {
      headings.push(line.slice(3).trim());
    }
  }

  return headings;
};

const ARCH_DIR = 'docs/10-architecture';
const ARCH_INDEX = `${ARCH_DIR}/README.md`;
const ARCH_SKELETON = codeBlockHeadings(ARCH_INDEX);

const failures = [];

// ── 1. 状态块 + 日期 ─────────────────────────────────────────────────────
for (const file of docsFiles) {
  if (STATUS_EXEMPT.has(file)) continue;

  const head = readFileSync(toAbs(file), 'utf8').split(/\r?\n/).slice(0, HEAD_LINES).join('\n');
  if (!STATUS_PATTERN.test(head)) {
    failures.push(`${file} 缺少状态块（前 ${HEAD_LINES} 行内没有 \`> 状态：\`）`);
  } else if (!DATE_PATTERN.test(head)) {
    failures.push(`${file} 的状态块缺少日期（\`> 最后更新：YYYY-MM-DD\`）`);
  } else if (/^> 日期：/m.test(head)) {
    failures.push(`${file} 使用了旧日期键「日期」，统一为「最后更新」`);
  }
}

// ── 2. 目录索引覆盖 ─────────────────────────────────────────────────────
const docsIndexed = new Set(referencesOf(DOCS_INDEX));

for (const dir of INDEXED_DIRS) {
  const dirIndex = `${dir}/README.md`;

  if (!docsIndexed.has(dirIndex)) {
    failures.push(`${dirIndex} 未登记到 ${DOCS_INDEX} 的目录索引`);
    continue;
  }

  if (!existsSync(toAbs(dirIndex))) {
    failures.push(`${dirIndex} 不存在（目录索引必须指向真实文件）`);
    continue;
  }

  const indexed = new Set(referencesOf(dirIndex));

  for (const file of docsFiles) {
    if (!file.startsWith(`${dir}/`)) continue;
    if (file === dirIndex) continue;
    if (!indexed.has(file)) failures.push(`${file} 未登记到 ${dirIndex} 的索引表`);
  }
}

// ── 3. ADR 必备章节 ─────────────────────────────────────────────────────
const REQUIRED_ADR_PARTS = [
  { label: '备选项对比（`## 备选项`）', pattern: /^## 备选项/m },
  { label: '后果（`## 后果`）', pattern: /^## 后果/m },
  { label: '重新评估触发条件（正文出现「重新评估」）', pattern: /重新评估/ },
];

for (const file of adrFiles) {
  if (LEGACY_ADRS.has(file)) continue;

  const text = readFileSync(toAbs(file), 'utf8');
  for (const part of REQUIRED_ADR_PARTS) {
    if (!part.pattern.test(text)) failures.push(`${file} 缺少${part.label}`);
  }

  if (!LEGACY_TODO_ADRS.has(file) && /^## 待办/m.test(text)) {
    failures.push(`${file} 含「## 待办」；ADR 正文不承载进度，进度写 roadmap`);
  }
}

// ── 4. 标题不编号 ───────────────────────────────────────────────────────
// H1 不参与：ADR / 规格的 `# 0005. 标题` 是记录编号（文件命名的一部分），不是章节编号。
const NUMBERED_HEADING = /^#{2,4}\s+(?:第\s*\d+\s*步[：:]|\d+[.、]|[一二三四五六七八九十]+、)/;

for (const file of [...docsFiles, 'README.md', 'CONTRIBUTING.md', 'SECURITY.md', 'AGENTS.md']) {
  if (!existsSync(toAbs(file))) continue;

  const lines = readFileSync(toAbs(file), 'utf8').split(/\r?\n/);
  let inFence = false;
  lines.forEach((line, index) => {
    if (line.trim().startsWith('```')) {
      inFence = !inFence;
      return;
    }
    if (inFence) return;

    if (NUMBERED_HEADING.test(line)) {
      failures.push(`${file}:${index + 1} 标题带编号（${line.trim()}），标题不编号`);
    }
  });
}

// ── 5. 架构文档骨架 ─────────────────────────────────────────────────────
if (ARCH_SKELETON.length === 0) {
  failures.push(`${ARCH_INDEX} 没有可用于校验的二级标题骨架（需要 \`\`\`markdown 代码块）`);
}

for (const file of docsFiles) {
  if (!file.startsWith(`${ARCH_DIR}/`) || file === `${ARCH_DIR}/README.md`) continue;

  const actual = headingsOf(file);
  if (JSON.stringify(actual) !== JSON.stringify(ARCH_SKELETON)) {
    failures.push(
      `${file} 的章节与架构文档骨架不一致（应为 ${ARCH_SKELETON.join(' / ')}；` +
        `实际 ${actual.join(' / ')}）`,
    );
  }
}

// ── 6. 代码块语言标记 ───────────────────────────────────────────────────
for (const file of [...docsFiles, 'README.md', 'CONTRIBUTING.md', 'SECURITY.md', 'AGENTS.md']) {
  if (!existsSync(toAbs(file))) continue;

  let inFence = false;
  readFileSync(toAbs(file), 'utf8')
    .split(/\r?\n/)
    .forEach((line, index) => {
      if (!line.trim().startsWith('```')) return;

      if (!inFence) {
        inFence = true;
        if (!/^`{3,}\s*\S+/.test(line.trim())) {
          failures.push(`${file}:${index + 1} 代码块缺少语言标记（纯文本用 text）`);
        }
      } else {
        inFence = false;
      }
    });
}

// ── 7. 待确认索引 + 历史路径/命令 ───────────────────────────────────────
const adrIndexTargets = new Set(referencesOf(ADR_INDEX));

for (const file of [...docsFiles, 'AGENTS.md']) {
  if (!file.startsWith(`${ARCH_DIR}/`) && file !== 'AGENTS.md') continue;
  // 代码块里引用骨架/模板不算「本文有待确认内容」
  const text = readFileSync(toAbs(file), 'utf8').replace(/```[\s\S]*?```/g, '');
  if (!/【待确认/.test(text) && !/^## .*待确认事项/m.test(text)) continue;
  if (!adrIndexTargets.has(file)) {
    failures.push(`${file} 有待确认内容但未在 ${ADR_INDEX} 索引`);
  }
}

const legacyPatterns = [
  /docs\/tech\//,
  /docs\/decisions\//,
  /docs\/templates\//,
  /docs\/(?:architecture|data-model|distributed|auth|features|skills)\.md/,
  /check-docs\.mjs/,
  /sync-dotnet-skills\.sh/,
  /sync_dotnet_skills\.py/,
  /\bpython3\b/,
];

for (const file of [...docsFiles, 'README.md', 'AGENTS.md']) {
  if (!existsSync(toAbs(file))) continue;
  const text = readFileSync(toAbs(file), 'utf8');
  for (const pattern of legacyPatterns) {
    if (pattern.test(text)) failures.push(`${file} 含过期路径/命令：${pattern}`);
  }
}

if (failures.length > 0) {
  console.error(`✖ 发现 ${failures.length} 个文档结构问题：\n`);
  for (const failure of failures) console.error(`  ${failure}`);
  console.error(
    `\n判据：${DOCS_INDEX}「文件命名」「文档状态」「写作原则」、` +
      `${ADR_DIR}/README.md「规则」与 docs/10-architecture/README.md「技术文档骨架」。` +
      '\n状态块缺失就补上；新文档补进所属目录 README；ADR 缺章节按模板补齐；' +
      '\nADR 的进度写 roadmap，不在正文新增待办；' +
      '\n标题去掉编号（顺序交给正文与列表）；架构文档按模板补齐，不适用也要保留小节。' +
      '\n两处状态是否一致由 check-doc-status.mjs 判定。',
  );
  process.exitCode = 1;
} else {
  console.log(
    `✔ 文档结构检查通过（${docsFiles.length} 篇文档，${INDEXED_DIRS.length} 个目录索引完整，` +
      `${adrFiles.length} 篇 ADR 章节齐全，架构文档骨架一致，标题无编号，代码块均有语言标记）`,
  );
}
