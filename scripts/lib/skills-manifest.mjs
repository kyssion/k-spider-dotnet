/**
 * 技能清单的唯一定义：检查脚本与同步脚本共用同一套路径、schema 与序列化。
 *
 * 为什么单独抽：`check-skills-manifest.mjs`（离线校验）与 `sync-dotnet-skills.mjs`
 * （联网同步）都要知道「清单在哪、技能目录在哪、入口文件叫什么、清单长什么样」。
 * 各写一份时，改一个路径或字段就可能出现「同步写到这里、检查看那里」的静默不一致。
 *
 * 清单 schema（只在这里定义）：
 *   {
 *     source: string,         上游仓库地址
 *     commit: string,         上游提交
 *     installedAt: string,    本次安装时间（ISO）
 *     unlisted: string[],     清单外但保留的插件名
 *     skills: [{ name, plugin, path }]
 *   }
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

/**
 * 读取清单；文件不存在时 `manifest` 为 null（首次同步），解析失败抛错。
 *
 * @param {string} rootDir 仓库根目录
 * @returns {{ manifest: object|null, file: string }}
 */
export function readManifest(rootDir) {
  const file = join(rootDir, MANIFEST_FILE);
  if (!existsSync(file)) return { manifest: null, file };
  return { manifest: JSON.parse(readFileSync(file, 'utf8')), file };
}

/** 清单声明的技能条目（保持声明顺序，跳过没有名字的损坏条目）。 */
export function declaredSkills(manifest) {
  return (manifest?.skills ?? [])
    .map((skill) => ({ name: skill?.name, plugin: skill?.plugin, path: skill?.path }))
    .filter((skill) => skill.name);
}

/** 清单声明的技能名（过滤空值，保持文件顺序）。 */
export function declaredSkillNames(manifest) {
  return declaredSkills(manifest).map((skill) => skill.name);
}

/** 构造清单对象：字段顺序与技能条目形状只在这里定义。 */
export function buildManifest({ source, commit, installedAt, unlisted, skills }) {
  return {
    source,
    commit,
    installedAt,
    unlisted,
    skills: skills.map(({ name, plugin, path }) => ({ name, plugin, path })),
  };
}

/** 序列化清单：统一缩进与末尾换行，避免两次写入产生无意义 diff。 */
export function serializeManifest(manifest) {
  return `${JSON.stringify(manifest, null, 2)}\n`;
}
