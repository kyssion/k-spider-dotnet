/**
 * 文档引用解析：引用检查、结构检查与状态检查共用的唯一口径。
 *
 * 为什么必须共用：各脚本各写一套解析规则时，「同一个路径算不算引用、往哪里解析」
 * 会各自漂移，最终出现「结构检查认为文档可达、引用检查认为路径不存在」的自相矛盾。
 * 扫描范围（哪些 Markdown 要查、哪些排除）归 `doc-files.mjs`；本文件只管一条引用指向哪。
 */

import { dirname, join, resolve } from 'node:path';

/** 根级文件（行内代码里直接写文件名时按仓库根解析）。 */
export const ROOT_FILES = [
  'AGENTS.md',
  'README.md',
  'CONTRIBUTING.md',
  'SECURITY.md',
  'k-spider-dotnet.sln',
  'global.json',
  'opencode.json',
  'Directory.Build.props',
  'Directory.Packages.props',
  '.editorconfig',
  '.gitignore',
];

/** 仓库根相对路径的识别前缀。 */
const ROOT_PREFIXES = ['db/', 'deploy/', 'docs/', 'scripts/', 'src/', 'web/', '.agents/', '.github/'];

/** 前端 src 相对路径的识别前缀（按 web/src/ 解析）。 */
const FRONTEND_SRC_PREFIXES = ['api/', 'components/', 'pages/', 'stores/', 'lib/'];

/**
 * 占位符或通配符：无法判定目标，不检查。
 * 编号占位按「路径段」判定，因此 `NNNN-短横线标题.md`、`0001-xxx.md` 都算占位；
 * `n{2,}` 只在段首/分隔符之后成立，不会误伤 `annotation` 这类正常单词。
 */
export const PLACEHOLDER_PATTERN =
  /[<>{}*…|~]|\.\.\.|(^|[-_./])xxx([-_./]|$)|(^|[-_./])n{2,}([-_./]|$)/i;

/** Markdown 链接目标：相对链接所在文件解析，没有第二种解释。 */
export function linkCandidates(rootDir, file, pathPart) {
  return [resolve(rootDir, dirname(file), pathPart)];
}

/**
 * 行内代码里一个 token 的所有候选位置；返回空数组 = 不检查。
 *
 * `includeFileRelative` 的取舍（这是各脚本唯一允许的差异）：
 *   - 结构检查传 `true`：额外把「相对当前文件」算作候选。它只回答「指没指到某个真实文档」，
 *     多一个候选不会制造失败；
 *   - 引用检查不传：行内代码里除了路径还有命令（`./scripts/verify.sh`）、路由（`#/projects`）、
 *     HTTP 端点（`GET /api/v1/meta/info`）、项目内相对路径（`Auth/`）—— 它们都含 `/`，
 *     无法从字面判断归属。按当前文件解析会把几十处正常表述判成「失效引用」，
 *     误报比漏报更伤；这类简写只能靠约定约束成链接（[docs/README.md]「引用与标题」）。
 */
export function inlineCandidates(rootDir, file, raw, { includeFileRelative = false } = {}) {
  if (ROOT_FILES.includes(raw)) return [join(rootDir, raw)];
  if (raw.startsWith('../')) return [resolve(rootDir, dirname(file), raw)];

  // `scripts/` 在仓库根与 web/ 下各有一份，做双候选消歧
  if (raw.startsWith('scripts/')) return [join(rootDir, raw), join(rootDir, 'web', raw)];

  // `public/` 只存在于 web/ 下
  if (raw.startsWith('public/')) return [join(rootDir, 'web', raw)];

  if (FRONTEND_SRC_PREFIXES.some((prefix) => raw.startsWith(prefix))) {
    return [join(rootDir, 'web', 'src', raw)];
  }

  if (ROOT_PREFIXES.some((prefix) => raw.startsWith(prefix))) return [join(rootDir, raw)];

  if (includeFileRelative) return [resolve(rootDir, dirname(file), raw)];
  return [];
}
