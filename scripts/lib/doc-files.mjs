/**
 * 文档守卫脚本共用的「哪些 Markdown 文件要检查」。
 *
 * 为什么单独抽出来：引用、重复、结构、状态四个脚本必须使用同一套扫描范围与排除规则。
 * 各写一份会让「ADR 正文要不要查」「归档代码要不要查」在脚本之间逐渐不一致 ——
 * 那时会出现「引用检查放过、重复检查报错」的自相矛盾。
 */

import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

/** 递归收集时跳过的目录。 */
export const SKIP_DIRS = new Set([
  'node_modules',
  '.git',
  '.idea',
  'bin',
  'obj',
  'dist',
  'wwwroot',
  'TestResults',
  'test-results',
]);

const ADR_DIR = 'docs/20-adr';

/**
 * 整体排除的仓库目录：历史归档不参与现行文档守卫。
 *
 * v1 代码、部署单元与旧库导出在 `src/k-spider-dotnet-old/`，按 [docs/README.md]
 * 「归档」的约定不作现行依据；它里面的 README 与代码注释是历史快照，不是漂移。
 */
const SKIP_ROOTS = new Set(['src/k-spider-dotnet-old']);

/**
 * vendored 的上游技能目录（由 `.agents/dotnet-skills/manifest.json` 声明），整体不参与检查。
 *
 * 这些文档里的示例路径是按上游语境书写的通用示例；且上游技能之间本身存在重复正文，
 * 那不是「我们的文档漂移」。按 manifest 而不是按目录前缀判定，是因为技能平铺在
 * `.agents/skills/` 下、与自有技能同级 —— 前缀排除会连带放过自有技能。
 */
export function vendoredSkillRoots(rootDir) {
  const manifestFile = join(rootDir, '.agents', 'dotnet-skills', 'manifest.json');
  if (!existsSync(manifestFile)) return new Set();

  try {
    const manifest = JSON.parse(readFileSync(manifestFile, 'utf8'));
    return new Set((manifest.skills ?? []).map((skill) => `.agents/skills/${skill.name}`));
  } catch {
    // 清单损坏时宁可少排除（检查更多），也不静默放过
    return new Set();
  }
}

/**
 * 收集需要检查的 Markdown 文件，返回**仓库根相对路径**（统一用 `/`），已排序。
 *
 * @param {string} rootDir 仓库根目录
 * @param {{ includeAdrBodies?: boolean }} [options]
 *   ADR 一经接受正文不改，正文里的旧路径/旧表述是历史快照，默认排除（索引 README 仍检查）。
 */
export function collectMarkdownFiles(rootDir, { includeAdrBodies = false } = {}) {
  const vendored = vendoredSkillRoots(rootDir);
  const acc = [];

  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const child = join(dir, entry.name);
      const childRel = relative(rootDir, child).split(sep).join('/');

      if (entry.isDirectory()) {
        if (SKIP_DIRS.has(entry.name)) continue;
        if (SKIP_ROOTS.has(childRel)) continue;
        if (vendored.has(childRel)) continue;
        walk(child);
        continue;
      }

      if (!entry.name.endsWith('.md')) continue;
      if (!includeAdrBodies && childRel.startsWith(`${ADR_DIR}/`) && childRel !== `${ADR_DIR}/README.md`) {
        continue;
      }
      acc.push(childRel);
    }
  };

  walk(rootDir);
  return acc.sort();
}
