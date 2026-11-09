#!/usr/bin/env node
/**
 * 文档引用检查（守门脚本）。
 *
 * 为什么需要它：
 *   代码被移动、删除或重命名后，文档里的引用不会自动跟着变。这类漂移只会被「碰巧
 *   读到的人」发现 —— 历次文档审计都靠人工逐个比对。本脚本把它变成机器守卫：
 *   引用失效即 verify 红灯，与数据契约守卫同思路。
 *
 * 检查范围（只查「可判定」的引用，避免用误报淹没真问题）：
 *   1. Markdown 相对链接 `[x](./y.md)` —— 目标文件必须存在；内部链接**不得带 `#` 锚点**
 *      （章节引用统一写成 `[文件](路径)「节名」`）；
 *   2. 行内代码中的**仓库根相对路径**：`db/…` `docs/…` `scripts/…` `src/…` `web/…`
 *      `.agents/…` `.github/…` `deploy/…`，以及根文件（`AGENTS.md`、`global.json` 等）；
 *   3. 行内代码中的**前端相对路径**：`api/…` `components/…` `pages/…` `stores/…` `lib/…`
 *      按 `web/src/` 解析；`public/…` 与 `scripts/…` 按 `web/` 解析；
 *   4. `docs/20-adr/NNNN`（编号简写，不带扩展名）—— 按「存在 NNNN-*.md」判定；
 *   5. 行内代码里**能解析到真实文档**的 `.md` 路径 —— 必须写成链接；裸文件名判不了归属，
 *      要么写成链接，要么在 ALLOWED_BARE_NAMES 登记理由；
 *   6. 链接文字：必须是目标文件名（索引文件写成 `<目录>/README.md`）；
 *   7. 章节引用 `[文件](路径)「节名」`：节名必须是目标文件某个标题的前缀；
 *   8. 章节指代不得用序号：`第 N 节` 会随标题调整失效，必须写「节名」；
 *   9. 链接不得指向目录：目录用反引号表示，链接只指向文件；
 *  10. 同一对象只用一种称呼：`本文件` / `本文档` 写 `本文`；跨文档条目用 ID 或标题，不用
 *      `第 N 条`；相对章节引用不用 `见上文 / 见下文` / `§`，统一 `见本文「节名」`。
 *
 * 解析规则只在 `scripts/lib/doc-refs.mjs` 维护，与结构检查共用同一套口径。
 *
 * 明确排除（不是漏检，是「判不了或不该判」）：
 *   - 代码块内的内容（示例、命令、模板），与重复检查口径一致；
 *   - 含占位符或通配符的引用（`<name>`、`{Domain,Services}`、`*.ts`、`xxx`、`…`）；
 *   - http(s) / mailto 链接、绝对路径；
 *   - `docs/20-adr/` 下的决策记录正文（README 索引除外）—— ADR 一经接受正文不改，
 *     旧路径是历史快照，不是漂移；
 *   - `.agents/skills/` 下由 manifest 登记的上游技能；
 *   - `src/k-spider-dotnet-old/` 历史归档（见 `scripts/lib/doc-files.mjs`）；
 *   - ALLOWED_MISSING_REFERENCES 中登记的「有意引用」（示例/模板、规划中、历史叙述），
 *     每条必须写明理由；未命中任何引用的条目会在通过时提示清理，防止清单膨胀。
 *
 * 用法：
 *   node scripts/check-doc-references.mjs
 *
 * 退出码：0 = 全部有效；1 = 存在失效引用（逐条列出 文件:行 → 引用 → 解析结果）。
 */

import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { basename, dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { collectMarkdownFiles } from './lib/doc-files.mjs';
import { withoutFencedCode } from './lib/doc-model.mjs';
import { PLACEHOLDER_PATTERN, inlineCandidates, linkCandidates } from './lib/doc-refs.mjs';

const ROOT_DIR = resolve(dirname(fileURLToPath(import.meta.url)), '..');

/**
 * 有意的「非现存路径」引用清单：每条 = 前缀 + 理由。
 *
 * 仅允许三类进入：
 *   1. 示例/模板（按设计不存在）；
 *   2. 规划中（明确说明「将来创建」，实现落地后应删除对应条目）；
 *   3. 历史叙述（明确说明「已被删除 / 已迁移」）。
 * 真实引用的漂移不允许进这里 —— 那正是本脚本要拦住的。
 */
const ALLOWED_MISSING_REFERENCES = [
  { prefix: 'src/k-spider-grpc/', reason: '分布式契约项目：待创建，见 docs/10-architecture/distributed.md', hits: 0 },
  { prefix: 'web/dist', reason: '前端构建产物：目标由 admin 托管，构建后生成，不入库', hits: 0 },
  { prefix: 'web/src/api', reason: '前端目录规划：待创建，见 docs/10-architecture/web.md', hits: 0 },
  { prefix: 'web/src/components', reason: '前端目录规划：待创建，见 docs/10-architecture/web.md', hits: 0 },
  { prefix: 'web/src/pages', reason: '前端目录规划：待创建，见 docs/10-architecture/web.md', hits: 0 },
  { prefix: 'web/src/stores', reason: '前端目录规划：待创建，见 docs/10-architecture/web.md', hits: 0 },
  { prefix: 'web/src/lib', reason: '前端目录规划：待创建，见 docs/10-architecture/web.md', hits: 0 },
  { prefix: 'pages/LoginPage', reason: '认证前端页面：待实现，见 docs/10-architecture/auth.md', hits: 0 },
  { prefix: 'pages/UsersPage', reason: '认证前端页面：待实现，见 docs/10-architecture/auth.md', hits: 0 },
  { prefix: 'api/auth', reason: '认证前端 API 客户端：待实现，见 docs/10-architecture/auth.md', hits: 0 },
  { prefix: 'api/client', reason: '前端 API 客户端：待实现，见 docs/10-architecture/web.md', hits: 0 },
];

/**
 * 行内代码里按设计**不写成链接**的裸文件名：每条 = 文件名 + 理由。
 *
 * 裸文件名（不带目录）无法判定归属，脚本只能跳过 —— 跳过等于「永远不会红灯」。
 * 所以把「跳过」改成「必须登记」：新出现的裸 .md 名一律失败，逼作者写成链接
 * 或在此说明为什么不能写成链接。仅允许三类进入：
 *   1. 反向示例（文档在说明「本仓库不维护它」）；
 *   2. 通用名（同一名字在每个目录下都有，没有单一路径可指）；
 *   3. 待创建（指南要求读者按步骤新建，落地后应改成链接）。
 */
const ALLOWED_BARE_NAMES = [
  { token: 'README.md', reason: '索引文件的通用名（每个目录各一份），行内出现时指「目录/README.md」这个约定', hits: 0 },
  { token: 'SKILL.md', reason: '通用文件名：每个技能目录各一份，没有单一路径可指', hits: 0 },
];

/** 链接文字的约定形式：目标文件名；索引文件用 `README.md`（歧义时写 `<目录>/README.md`）。 */
function expectedLabel(resolvedPath) {
  const pathPart = resolvedPath.split('#')[0];
  const base = basename(pathPart);
  const parent = basename(dirname(pathPart));
  return base === 'README.md' ? (parent === '.' ? 'README.md' : `${parent}/README.md`) : base;
}

function isAllowedLabel(label, resolvedPath) {
  const base = basename(resolvedPath.split('#')[0]);
  const parent = basename(dirname(resolvedPath.split('#')[0]));

  if (base === 'README.md') {
    return label === (parent === '.' ? 'README.md' : `${parent}/README.md`);
  }

  return label === base;
}

/**
 * 章节引用的校验：节名必须是目标文件某个标题的前缀（比较前去掉标题自己的编号）。
 * 目标解析不出来时直接放过 —— 引用本身失效会由别的规则报，不在这里重复报。
 */
function checkSectionRef(section, candidates, file, line) {
  const target = candidates.find((candidate) => existsSync(candidate));
  if (!target) return;

  const headings = withoutFencedCode(readFileSync(target, 'utf8'))
    .split(/\r?\n/)
    .filter((text) => /^#{1,6} /.test(text))
    .map((text) =>
      text
        .replace(/^#{1,6}\s+/, '')
        .replace(/^(?:第\s*\d+\s*步[：:]\s*|\d+[.、]\s*|[一二三四五六七八九十]+、\s*)/, ''));

  if (!headings.some((heading) => heading.startsWith(section))) {
    sectionFailures.push({ file, line, section, target: relative(ROOT_DIR, target) });
  }
}

const ADR_DIR = 'docs/20-adr';
const adrFileNames = readdirSync(join(ROOT_DIR, ADR_DIR)).filter((name) => name.endsWith('.md'));

/** 验证一条引用：skip（不检查）/ exempt（有意引用）/ bare（裸文件名，须登记）/ ok / missing。 */
function checkReference(raw, mdRelPath, kind) {
  const ref = kind === 'link' ? raw.trim().split(/\s+/)[0] : raw.trim();
  if (!ref || /^(https?:|mailto:)/i.test(ref)) return { status: 'skip' };
  if (kind === 'link' && ref.includes('#')) return { status: 'anchor', ref };
  if (ref.startsWith('/') || ref.startsWith('~')) return { status: 'skip' };

  const exemption = ALLOWED_MISSING_REFERENCES.find((item) => ref.startsWith(item.prefix));
  if (exemption) {
    exemption.hits += 1;
    return { status: 'exempt' };
  }

  const pathPart = kind === 'link' ? ref.split('#')[0] : ref;
  if (!pathPart || PLACEHOLDER_PATTERN.test(pathPart)) return { status: 'skip' };

  // `docs/20-adr/NNNN` 编号简写 → 检查目录内是否存在 `NNNN-*.md`
  const adrNumber = new RegExp(`^${ADR_DIR}/(\\d{4})$`).exec(pathPart);
  if (adrNumber) {
    const prefix = `${adrNumber[1]}-`;
    const valid = adrFileNames.some((name) => name.startsWith(prefix));

    return { status: valid ? 'ok' : 'missing', resolved: `${ADR_DIR}/${prefix}*.md` };
  }

  const candidates =
    kind === 'link'
      ? linkCandidates(ROOT_DIR, mdRelPath, pathPart)
      : inlineCandidates(ROOT_DIR, mdRelPath, pathPart);
  if (candidates.length === 0) {
    // 裸 .md 文件名：判不了归属，但不允许静默放过（见 ALLOWED_BARE_NAMES）。
    // 纯扩展名（`.md`）说的是「扩展名」而不是「某个文件」，不算文件名。
    if (kind === 'code' && pathPart.endsWith('.md') && !/^\.[a-z0-9]+$/i.test(pathPart)) {
      return { status: 'bare', ref };
    }

    return { status: 'skip' };
  }

  if (kind === 'code' && pathPart.endsWith('.md') && candidates.some((candidate) => existsSync(candidate))) {
    return { status: 'inline-doc', ref };
  }

  const valid = candidates.some((candidate) => existsSync(candidate));
  return { status: valid ? 'ok' : 'missing', resolved: relative(ROOT_DIR, candidates[0]) };
}

const files = collectMarkdownFiles(ROOT_DIR);
const failures = [];
const bareFailures = [];
const labelFailures = [];
const sectionFailures = [];
const dirLinkFailures = [];
const ordinalFailures = [];
const inlineDocFailures = [];
const anchorFailures = [];
const phraseFailures = [];
let referenceCount = 0;
let exemptCount = 0;

for (const file of files) {
  const lines = readFileSync(join(ROOT_DIR, file), 'utf8').split(/\r?\n/);
  let inFence = false;

  lines.forEach((line, index) => {
    // 代码块是示例与命令，不是活引用：与重复检查口径一致，整块跳过
    if (line.trim().startsWith('```')) {
      inFence = !inFence;
      return;
    }
    if (inFence) return;

    const handle = (raw, kind) => {
      const result = checkReference(raw, file, kind);
      if (result.status === 'skip') return;

      referenceCount += 1;
      if (result.status === 'exempt') {
        exemptCount += 1;
        return;
      }

      if (result.status === 'bare') {
        const allowed = ALLOWED_BARE_NAMES.find((item) => item.token === result.ref);
        if (allowed) {
          allowed.hits += 1;
        } else {
          bareFailures.push({ file, line: index + 1, ref: result.ref });
        }
        return;
      }

      if (result.status === 'inline-doc') {
        // 已登记的通用名/入口名（如 `README.md`、`AGENTS.md`）在说明约定时允许保留行内写法，
        // 其余能解析到真实文档的行内路径必须写成链接。
        const allowed = ALLOWED_BARE_NAMES.find((item) => item.token === result.ref || item.token === raw.trim());
        if (allowed) {
          allowed.hits += 1;
        } else {
          inlineDocFailures.push({ file, line: index + 1, ref: raw.trim() });
        }
        return;
      }

      if (result.status === 'anchor') {
        anchorFailures.push({ file, line: index + 1, ref: raw.trim() });
        return;
      }

      if (result.status === 'missing') {
        failures.push({ file, line: index + 1, ref: raw.trim(), resolved: result.resolved });
      }
    };

    // 行内代码里的 `[文件](路径)` 只是示例，不当作链接处理
    const codeSpans = [...line.matchAll(/`[^`]+`/g)].map((match) => [
      match.index,
      match.index + match[0].length,
    ]);
    const inCodeSpan = (position) => codeSpans.some(([start, end]) => position >= start && position < end);

    const phrasePatterns = [
      /本文件/g,
      /本文档/g,
      /第\s*[0-9]+\s*条/g,
      /第\s*[Nn]\s*[条节]/g,
      /第\s*[一二三四五六七八九十]+\s*条/g,
      /见(?:上文|下文)/g,
      /见[“"][^”"]+[”"]/g,
      /见「[^」]+」/g,
      /§/g,
    ];

    for (const pattern of phrasePatterns) {
      for (const match of line.matchAll(pattern)) {
        if (!inCodeSpan(match.index)) phraseFailures.push({ file, line: index + 1, ref: match[0] });
      }
    }

    const linkMatches = [...line.matchAll(/\[([^\]]*)\]\(([^)]+)\)/g)].filter(
      (match) => !inCodeSpan(match.index),
    );
    for (const match of linkMatches) {
      const rawTarget = match[2].trim();
      // Markdown 允许 `(path "title")`；路径取第一个空白前的 token，title 不参与解析
      const target = rawTarget.split(/\s+/)[0];
      handle(target, 'link');

      if (/^(https?:|mailto:|#|\/|~)/i.test(target)) continue;
      if (target.includes('#')) continue; // 锚点由 anchor 规则报，避免一条链接报两次

      const resolvedAbs = linkCandidates(ROOT_DIR, file, target.split('#')[0])[0];
      if (existsSync(resolvedAbs) && statSync(resolvedAbs).isDirectory()) {
        dirLinkFailures.push({ file, line: index + 1, label: match[1], target });
        continue;
      }

      const resolved = relative(ROOT_DIR, resolvedAbs);
      if (!isAllowedLabel(match[1], resolved)) {
        labelFailures.push({
          file,
          line: index + 1,
          label: match[1],
          expected: expectedLabel(resolved),
        });
      }
    }

    // 章节引用：`[文件](路径)「节名」`，允许一处链接后连续引用多节
    for (const match of line.matchAll(/\]\(([^)]+)\)((?:\s*「[^」]+」)+)/g)) {
      if (inCodeSpan(match.index)) continue;
      const linkTarget = match[1].trim().split(/\s+/)[0].split('#')[0];
      const candidates = linkCandidates(ROOT_DIR, file, linkTarget);
      for (const section of match[2].matchAll(/「([^」]+)」/g)) {
        checkSectionRef(section[1], candidates, file, index + 1);
      }
    }

    // 链接文字里的 `x.md` 不是独立引用（它只是链接的显示文字），先把链接片段抹掉再找行内代码
    let rest = line;
    for (const match of linkMatches) {
      rest = `${rest.slice(0, match.index)}${' '.repeat(match[0].length)}${rest.slice(match.index + match[0].length)}`;
    }

    for (const match of rest.matchAll(/`([^`]+)`/g)) {
      handle(match[1], 'code');
    }

    // 章节引用：行内代码路径 +（可选「的」）+ 一个或多个「节名」
    for (const match of rest.matchAll(/`([^`]+\.md)`\s*(?:的\s*)?((?:「[^」]+」)+)/g)) {
      const candidates = inlineCandidates(ROOT_DIR, file, match[1].trim(), { includeFileRelative: true });
      for (const section of match[2].matchAll(/「([^」]+)」/g)) {
        checkSectionRef(section[1], candidates, file, index + 1);
      }
    }

    // 同文档章节引用：`见本文/本节「节名」`
    for (const match of line.matchAll(/见(?:本文|本节|本章)(?:的)?((?:「[^」]+」)+)/g)) {
      if (inCodeSpan(match.index)) continue;
      const candidates = [resolve(ROOT_DIR, file)];
      for (const section of match[1].matchAll(/「([^」]+)」/g)) {
        checkSectionRef(section[1], candidates, file, index + 1);
      }
    }

    // 章节指代：`第 N 节` 在标题不编号后没有稳定含义
    for (const match of line.matchAll(/第\s*[0-9一二三四五六七八九十]+\s*节/g)) {
      if (!inCodeSpan(match.index)) ordinalFailures.push({ file, line: index + 1, ref: match[0] });
    }
  });
}

const unusedExemptions = ALLOWED_MISSING_REFERENCES.filter((item) => item.hits === 0);
const unusedBareNames = ALLOWED_BARE_NAMES.filter((item) => item.hits === 0);
const bareAllowedCount = ALLOWED_BARE_NAMES.reduce((sum, item) => sum + item.hits, 0);

if (
  failures.length > 0 ||
  bareFailures.length > 0 ||
  labelFailures.length > 0 ||
  sectionFailures.length > 0 ||
  ordinalFailures.length > 0 ||
  dirLinkFailures.length > 0 ||
  inlineDocFailures.length > 0 ||
  anchorFailures.length > 0 ||
  phraseFailures.length > 0
) {
  if (failures.length > 0) {
    console.error(`✖ 发现 ${failures.length} 条失效引用（共检查 ${referenceCount} 条）：\n`);

    for (const failure of failures) {
      console.error(`  ${failure.file}:${failure.line}  「${failure.ref}」 → ${failure.resolved} 不存在`);
    }

    console.error(
      '\n修正引用，或改写为不再引用该路径的描述。若确认属于「示例/模板 / 规划中 / 历史叙述」' +
        '三类有意引用，请在 scripts/check-doc-references.mjs 的 ALLOWED_MISSING_REFERENCES 中登记并写明理由。',
    );
  }

  if (bareFailures.length > 0) {
    console.error(`\n✖ 发现 ${bareFailures.length} 处裸文件名（机器无法判定它指向哪份文档）：\n`);

    for (const failure of bareFailures) {
      console.error(`  ${failure.file}:${failure.line}  「${failure.ref}」`);
    }

    console.error(
      '\n改成 Markdown 链接（裸文件名没有目录信息，判不了归属）。' +
        '\n若它按设计就是「不存在的反向示例 / 每个目录都有的通用名 / 待创建的文件」，' +
        '请登记到 scripts/check-doc-references.mjs 的 ALLOWED_BARE_NAMES 并写明理由。',
    );
  }

  if (labelFailures.length > 0) {
    console.error(`\n✖ 发现 ${labelFailures.length} 处链接文字不合约定：\n`);

    for (const failure of labelFailures) {
      console.error(`  ${failure.file}:${failure.line}  「${failure.label}」应写成「${failure.expected}」`);
    }

    console.error(
      '\n链接文字一律用目标文件名；索引文件写成 `<目录>/README.md`（根 README 写 `README.md`）。',
    );
  }

  if (sectionFailures.length > 0) {
    console.error(`\n✖ 发现 ${sectionFailures.length} 处章节引用不匹配：\n`);

    for (const failure of sectionFailures) {
      console.error(
        `  ${failure.file}:${failure.line}  「${failure.section}」不是 ${failure.target} 的标题前缀`,
      );
    }

    console.error('\n节名必须是目标文件某个标题的前缀（比较时忽略标题编号）；标题改名时同步引用。');
  }

  if (dirLinkFailures.length > 0) {
    console.error(`\n✖ 发现 ${dirLinkFailures.length} 处链接指向目录：\n`);

    for (const failure of dirLinkFailures) {
      console.error(`  ${failure.file}:${failure.line}  [${failure.label}](${failure.target})`);
    }

    console.error('\n目录用反引号表示（如 `30-guides/`），链接只指向文件。');
  }

  if (inlineDocFailures.length > 0) {
    console.error(`\n✖ 发现 ${inlineDocFailures.length} 处行内代码文档引用：\n`);

    for (const failure of inlineDocFailures) {
      console.error(`  ${failure.file}:${failure.line}  「${failure.ref}」`);
    }

    console.error('\n文档引用只有一种写法：Markdown 链接；行内代码只用于命令与非文档路径。');
  }

  if (anchorFailures.length > 0) {
    console.error(`\n✖ 发现 ${anchorFailures.length} 处内部链接带 # 片段：\n`);

    for (const failure of anchorFailures) {
      console.error(`  ${failure.file}:${failure.line}  ${failure.ref}`);
    }

    console.error('\n章节引用只用「节名」（[文件](路径)「节」），链接目标不带 # 锚点。');
  }

  if (phraseFailures.length > 0) {
    console.error(`\n✖ 发现 ${phraseFailures.length} 处不统一措辞：\n`);

    for (const failure of phraseFailures) {
      console.error(`  ${failure.file}:${failure.line}  ${failure.ref}`);
    }

    console.error('\n统一写「本文」「见本文「节名」」；跨文档条目用 ID 或标题；不用「第 N 条」与 §。');
  }

  if (ordinalFailures.length > 0) {
    console.error(`\n✖ 发现 ${ordinalFailures.length} 处「第 N 节」指代：\n`);

    for (const failure of ordinalFailures) {
      console.error(`  ${failure.file}:${failure.line}  ${failure.ref}`);
    }

    console.error('\n标题不编号，序号会随章节调整失效；改成「节名」或链接 +「节名」。');
  }

  process.exitCode = 1;
} else {
  console.log(
    `✔ 文档引用检查通过（${files.length} 个文件 / ${referenceCount} 条引用` +
      `${exemptCount > 0 ? `，其中 ${exemptCount} 条为已登记的有意引用` : ''}` +
      `${bareAllowedCount > 0 ? `，${bareAllowedCount} 处为已登记的裸文件名` : ''}）`,
  );

  if (unusedExemptions.length > 0) {
    console.log('\n以下豁免条目未命中任何引用，建议清理：');
    for (const item of unusedExemptions) {
      console.log(`  · ${item.prefix}（${item.reason}）`);
    }
  }

  if (unusedBareNames.length > 0) {
    console.log('\n以下裸文件名条目未命中任何位置，建议清理：');
    for (const item of unusedBareNames) {
      console.log(`  · ${item.token}（${item.reason}）`);
    }
  }
}
