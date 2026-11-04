#!/usr/bin/env node
/**
 * 文档引用检查（守门脚本）。
 *
 * 为什么需要它：
 *   代码被移动、删除或重命名后，文档里的引用不会自动跟着变。这类漂移（如参考页面删除后
 *   文档仍在引用已迁移的旧路径、组件库重构后仍写旧文件名）只会被「碰巧
 *   读到的人」发现 —— 历次文档审计都靠人工逐个比对。本脚本把它变成机器守卫：引用失效即
 *   verify 红灯。
 *
 * 检查范围（只查「可判定」的引用，避免用误报淹没真问题）：
 *   1. Markdown 相对链接 `[x](./y.md)` —— 目标文件/目录必须存在（忽略锚点、外部 URL）；
 *   2. 行内代码中的**仓库根相对路径**：`web/…` `docs/…` `src/…` `db/…`
 *      `.agents/…` `.github/…`，以及根文件（`AGENTS.md`、`global.json` 等）；
 *   3. 行内代码中的**前端相对路径**：`api/…` `components/…` `lib/…` `pages/…` `stores/…`
 *      按 `web/src/` 解析；`public/…` 与 `scripts/…` 按 `web/` 解析；
 *   4. `docs/20-adr/NNNN`（编号简写，不带扩展名）—— 按「存在 NNNN-*.md」判定。
 *
 * 明确排除（不是漏检，是「判不了或不该判」）：
 *   - 含占位符或通配符的引用（`<name>`、`{Domain,Services}`、`*.ts`、`xxx`、`…`）；
 *   - http(s) / mailto 链接、纯锚点、绝对路径；
 *   - `docs/20-adr/` 下的决策记录（README 索引除外）—— ADR 一经接受正文不改，
 *     旧路径是历史快照，不是漂移；
 *   - `.agents/skills/` 下由 manifest 登记的上游技能（见 VENDORED_SKILL_ROOTS）；
 *   - ALLOWED_MISSING_REFERENCES 中登记的「有意引用」（示例/模板、规划中、历史叙述），
 *     每条必须写明理由；未命中任何引用的条目会在通过时提示清理，防止清单膨胀。
 *
 * 用法：
 *   node scripts/check-doc-references.mjs
 *
 * 退出码：0 = 全部有效；1 = 存在失效引用（逐条列出 文件:行 → 引用 → 解析结果）。
 */

import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { dirname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT_DIR = resolve(dirname(fileURLToPath(import.meta.url)), '..');

/** 递归收集时跳过的目录。 */
const SKIP_DIRS = new Set(['node_modules', '.git', '.claude', 'bin', 'obj', 'dist', 'wwwroot', 'TestResults', 'test-results']);

/**
 * vendored 的上游技能目录（由 `.agents/dotnet-skills/manifest.json` 声明），整体不参与检查。
 *
 * 这些文档里的 `tests/*.py`、`scripts/*.ps1`、`.agents/skill-overlays/...` 是**上游语境下的
 * 通用示例**，从不指向本仓库；逐条改成本仓库路径会破坏与上游的可同步性，且它们不是
 * 「我们的文档漂移」。按 manifest 而不是按目录前缀判定，是因为技能平铺在
 * `.agents/skills/` 下、与自有技能同级 —— 前缀排除会连带放过自有技能。
 */
function vendoredSkillRoots() {
  const manifestFile = join(ROOT_DIR, '.agents', 'dotnet-skills', 'manifest.json');
  if (!existsSync(manifestFile)) return new Set();

  try {
    const manifest = JSON.parse(readFileSync(manifestFile, 'utf8'));
    return new Set((manifest.skills ?? []).map((skill) => `.agents/skills/${skill.name}`));
  } catch {
    // 清单损坏时宁可少排除（检查更多），也不静默放过
    return new Set();
  }
}

const VENDORED_SKILL_ROOTS = vendoredSkillRoots();

/** 仓库根相对路径的识别前缀。 */
const ROOT_PREFIXES = ['db/', 'deploy/', 'docs/', 'scripts/', 'src/', 'web/', '.agents/', '.github/'];

/** 前端 src 相对路径的识别前缀（按 web/src/ 解析）。 */
const FRONTEND_SRC_PREFIXES = ['api/', 'components/', 'lib/', 'pages/', 'stores/'];

/** 根级文件（反引号中直接引用文件名时按仓库根解析）。 */
const ROOT_FILES = [
  'AGENTS.md',
  'README.md',
  'opencode.json',
  'k-spider-dotnet.sln',
  'global.json',
  'Directory.Build.props',
  'Directory.Packages.props',
  '.editorconfig',
  '.gitignore',
];

/**
 * 有意的「非现存路径」引用清单：每条 = 前缀 + 理由。
 *
 * 仅允许三类进入：
 *   1. 示例/模板（按设计不存在，如新增特性指南的演示资源 Notes）；
 *   2. 规划中（明确说明「将来创建」，实现落地后应删除对应条目）；
 *   3. 历史叙述（明确说明「已被删除 / 已迁移」）。
 * 真实引用的漂移不允许进这里 —— 那正是本脚本要拦住的（红线 9 的豁免精神：
 * 在受影响的位置做范围内豁免，并写明原因）。
 */
const ALLOWED_MISSING_REFERENCES = [
  { prefix: 'src/k-spider-grpc/', reason: '分布式契约项目：待创建，见 docs/10-architecture/distributed.md', hits: 0 },
  { prefix: 'web/dist', reason: '前端构建产物：目标由 admin 托管，构建后生成，不入库', hits: 0 },
  { prefix: 'pages/LoginPage', reason: '认证前端页面：登录页待实现', hits: 0 },
  { prefix: 'pages/UsersPage', reason: '认证前端页面：账号管理页待实现', hits: 0 },
  { prefix: 'api/auth', reason: '认证前端 API 客户端：待实现', hits: 0 },
  { prefix: 'api/client', reason: '前端 API 客户端：待实现', hits: 0 },
  { prefix: 'web/src/api', reason: '前端目录规划：待实现', hits: 0 },
  { prefix: 'web/src/components', reason: '前端目录规划：待实现', hits: 0 },
  { prefix: 'web/src/pages', reason: '前端目录规划：待实现', hits: 0 },
  { prefix: 'web/src/stores', reason: '前端目录规划：待实现', hits: 0 },
  { prefix: 'web/src/lib', reason: '前端目录规划：待实现', hits: 0 },
];

/** 占位符或通配符：无法判定目标，不检查（含 `xxx` 与 `NNNN` 形态的编号占位段）。 */
const PLACEHOLDER_PATTERN = /[<>{}*…|~]|\.\.\.|(^|\/)xxx(\/|$)|(^|\/)[n]{2,}(\/|$)/i;

const ADR_DIR = 'docs/20-adr';
const adrFileNames = readdirSync(join(ROOT_DIR, ADR_DIR)).filter((name) => name.endsWith('.md'));

/** 递归收集需要检查的 Markdown 文件（仓库根相对路径，统一用 `/`）。 */
function collectMarkdownFiles(dir, acc = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      if (SKIP_DIRS.has(entry.name)) continue;

      const childRel = relative(ROOT_DIR, join(dir, entry.name)).split(sep).join('/');
      if (VENDORED_SKILL_ROOTS.has(childRel)) continue;

      collectMarkdownFiles(join(dir, entry.name), acc);
      continue;
    }

    if (!entry.name.endsWith('.md')) continue;

    const rel = relative(ROOT_DIR, join(dir, entry.name)).split(sep).join('/');
    if (rel.startsWith(`${ADR_DIR}/`) && rel !== `${ADR_DIR}/README.md`) continue;
    acc.push(rel);
  }

  return acc;
}

/** 生成候选绝对路径；任一存在即视为有效。返回空数组表示不检查。 */
function buildCandidates(pathPart, mdRelPath, kind) {
  if (kind === 'link') {
    return [resolve(ROOT_DIR, dirname(join(ROOT_DIR, mdRelPath)), pathPart)];
  }

  if (ROOT_FILES.includes(pathPart)) return [join(ROOT_DIR, pathPart)];

  if (pathPart.startsWith('../')) {
    return [resolve(ROOT_DIR, dirname(join(ROOT_DIR, mdRelPath)), pathPart)];
  }

  // `scripts/` 在仓库根与 web/ 下各有一份，做双候选消歧
  if (pathPart.startsWith('scripts/')) {
    return [join(ROOT_DIR, pathPart), join(ROOT_DIR, 'web', pathPart)];
  }

  // `public/` 只存在于 web/ 下
  if (pathPart.startsWith('public/')) {
    return [join(ROOT_DIR, 'web', pathPart)];
  }

  if (FRONTEND_SRC_PREFIXES.some((prefix) => pathPart.startsWith(prefix))) {
    return [join(ROOT_DIR, 'web', 'src', pathPart)];
  }

  if (ROOT_PREFIXES.some((prefix) => pathPart.startsWith(prefix))) {
    return [join(ROOT_DIR, pathPart)];
  }

  return [];
}

/** 验证一条引用：skip（不检查）/ exempt（有意引用）/ ok / missing。 */
function checkReference(raw, mdRelPath, kind) {
  const ref = kind === 'link' ? raw.trim().split(/\s+/)[0] : raw.trim();
  if (!ref || /^(https?:|mailto:)/i.test(ref)) return { status: 'skip' };
  if (kind === 'link' && ref.startsWith('#')) return { status: 'skip' };
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

  const candidates = buildCandidates(pathPart, mdRelPath, kind);
  if (candidates.length === 0) return { status: 'skip' };

  const valid = candidates.some((candidate) => existsSync(candidate));
  return { status: valid ? 'ok' : 'missing', resolved: relative(ROOT_DIR, candidates[0]) };
}

const files = collectMarkdownFiles(ROOT_DIR).sort();
const failures = [];
let referenceCount = 0;
let exemptCount = 0;

for (const file of files) {
  const lines = readFileSync(join(ROOT_DIR, file), 'utf8').split(/\r?\n/);

  lines.forEach((line, index) => {
    const handle = (raw, kind) => {
      const result = checkReference(raw, file, kind);
      if (result.status === 'skip') return;

      referenceCount += 1;
      if (result.status === 'exempt') {
        exemptCount += 1;
        return;
      }

      if (result.status === 'missing') {
        failures.push({ file, line: index + 1, ref: raw.trim(), resolved: result.resolved });
      }
    };

    for (const match of line.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
      handle(match[1], 'link');
    }

    for (const match of line.matchAll(/`([^`]+)`/g)) {
      handle(match[1], 'code');
    }
  });
}

// ── 结构检查：索引覆盖、状态块、过期路径 ───────────────────────────────

const structuralFailures = [];
const docsFiles = files.filter((file) => file.startsWith('docs/'));
const docsIndex = 'docs/README.md';

const linkTargets = (file) => {
  const targets = new Set();
  const text = readFileSync(join(ROOT_DIR, file), 'utf8');
  for (const match of text.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
    const ref = match[1].trim().split(/\s+/)[0];
    if (!ref || /^(https?:|mailto:|#|\/|~)/i.test(ref)) continue;
    const pathPart = ref.split('#')[0];
    if (pathPart) targets.add(relative(ROOT_DIR, resolve(ROOT_DIR, dirname(file), pathPart)).split(sep).join('/'));
  }
  return targets;
};

const indexed = linkTargets(docsIndex);
for (const file of docsFiles) {
  if (file === docsIndex) continue;
  if (file.startsWith('docs/20-adr/') && file !== 'docs/20-adr/README.md') continue;
  if (file === 'docs/30-guides/tech-doc-template.md') continue;
  if (!indexed.has(file)) structuralFailures.push(`${file} 未在 docs/README.md 索引`);
}

const adrIndex = linkTargets('docs/20-adr/README.md');
for (const file of docsFiles) {
  if (!file.startsWith('docs/20-adr/')) continue;
  if (file === 'docs/20-adr/README.md' || file === 'docs/20-adr/0000-template.md') continue;
  if (!adrIndex.has(file)) structuralFailures.push(`${file} 未在 docs/20-adr/README.md 索引`);
}

for (const file of docsFiles) {
  if (file === docsIndex) continue;
  if (file.endsWith('0000-template.md') || file === 'docs/30-guides/tech-doc-template.md') continue;
  if (file.startsWith('docs/20-adr/')) continue;
  const text = readFileSync(join(ROOT_DIR, file), 'utf8');
  if (!/^> \*\*状态\*\*：/m.test(text)) {
    structuralFailures.push(`${file} 缺少状态块（> **状态**：...）`);
  } else if (!/最后核对：\d{4}-\d{2}-\d{2}/.test(text)) {
    structuralFailures.push(`${file} 状态缺少最后核对日期`);
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
  const text = readFileSync(join(ROOT_DIR, file), 'utf8');
  for (const pattern of legacyPatterns) {
    if (pattern.test(text)) structuralFailures.push(`${file} 含过期路径/命令：${pattern}`);
  }
}

const unusedExemptions = ALLOWED_MISSING_REFERENCES.filter((item) => item.hits === 0);

if (failures.length > 0 || structuralFailures.length > 0) {
  if (failures.length > 0) {
    console.error(`✖ 发现 ${failures.length} 条失效引用（共检查 ${referenceCount} 条）：\n`);
    for (const failure of failures) {
      console.error(`  ${failure.file}:${failure.line}  「${failure.ref}」 → ${failure.resolved} 不存在`);
    }
  }

  if (structuralFailures.length > 0) {
    console.error(`\n✖ 发现 ${structuralFailures.length} 个文档结构问题：\n`);
    for (const failure of [...new Set(structuralFailures)]) console.error(`  ${failure}`);
  }

  console.error(
    '\n修正引用/结构问题。若确认属于「示例/模板 / 规划中 / 历史叙述」三类有意引用，' +
      '请在 scripts/check-doc-references.mjs 的 ALLOWED_MISSING_REFERENCES 中登记并写明理由。',
  );
  process.exitCode = 1;
} else {
  console.log(
    `✔ 文档检查通过（${files.length} 个文件 / ${referenceCount} 条引用` +
      `${exemptCount > 0 ? `，其中 ${exemptCount} 条为已登记的有意引用` : ''}）`,
  );

  if (unusedExemptions.length > 0) {
    console.log('\n以下豁免条目未命中任何引用，建议清理：');
    for (const item of unusedExemptions) {
      console.log(`  · ${item.prefix}（${item.reason}）`);
    }
  }
}
