/**
 * 技能清单的唯一定义：检查脚本与同步脚本共用同一套路径与 schema。
 *
 * 为什么单独抽：`check-skills-manifest.mjs`（离线校验）与 `sync-dotnet-skills.mjs`
 * （联网同步）都要知道「清单在哪、技能目录在哪、入口文件叫什么」。各写一份时，
 * 改一个路径就可能出现「同步写到这里、检查看那里」的静默不一致。
 */

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

/** 仓库相对路径：技能目录。 */
export const SKILLS_DIR = '.agents/skills';

/** 仓库相对路径：受管技能的元数据目录。 */
export const META_DIR = '.agents/dotnet-skills';

/** 仓库相对路径：受管技能清单。 */
export const MANIFEST_FILE = `${META_DIR}/manifest.json`;

/** 技能入口文件名。 */
export const SKILL_ENTRY = 'SKILL.md';

/** 读取清单；文件不存在时返回 null，解析失败抛错（由调用方决定如何报告）。 */
export function readManifest(rootDir) {
  const file = join(rootDir, MANIFEST_FILE);
  if (!existsSync(file)) return null;
  return JSON.parse(readFileSync(file, 'utf8'));
}

/** 清单声明的技能名（过滤空值，保持文件顺序）。 */
export function declaredSkillNames(manifest) {
  return (manifest?.skills ?? []).map((skill) => skill?.name).filter(Boolean);
}
