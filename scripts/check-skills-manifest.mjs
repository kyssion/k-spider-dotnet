#!/usr/bin/env node
/**
 * 技能清单一致性检查（守门脚本）。
 *
 * 为什么需要它：
 *   `.agents/skills/` 下受管技能与自有技能平铺在同一层，归属只记在
 *   `.agents/dotnet-skills/manifest.json`。同步脚本的 `--check` 需要联网，平时不会跑；
 *   手工删目录、改坏清单或漏同步都不会让编译/测试红灯，直到任务按 AGENTS.md 的路由加载
 *   技能时才暴露。本脚本把“清单声明”和“磁盘事实”做纯离线比对。
 *
 * 判定：
 *   1. 清单可解析、非空、技能名不重复；
 *   2. 声明过的每个技能目录存在，且含 SKILL.md；
 *   3. 磁盘上不在清单里的目录视为自有技能（合法），但同样必须含 SKILL.md。
 *
 * 用法：node scripts/check-skills-manifest.mjs
 * 退出码：0 = 一致；1 = 不一致（逐条列出）。
 */

import { existsSync, readdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  MANIFEST_FILE,
  SKILL_ENTRY,
  SKILLS_DIR,
  declaredSkillNames,
  readManifest,
} from './lib/skills-manifest.mjs';

const ROOT_DIR = resolve(dirname(fileURLToPath(import.meta.url)), '..');

const skillsPath = join(ROOT_DIR, SKILLS_DIR);
const manifestPath = join(ROOT_DIR, MANIFEST_FILE);
const failures = [];
let declared = [];
let ownSkills = [];

if (!existsSync(manifestPath)) {
  failures.push(`未找到 ${MANIFEST_FILE}（受管技能的归属来源）`);
} else if (!existsSync(skillsPath)) {
  failures.push(`未找到 ${SKILLS_DIR}/ 目录`);
} else {
  let manifest = null;
  try {
    ({ manifest } = readManifest(ROOT_DIR));
  } catch (error) {
    failures.push(`${MANIFEST_FILE} 不是合法 JSON：${error.message}`);
  }

  declared = declaredSkillNames(manifest);
  if (declared.length === 0) {
    failures.push(`${MANIFEST_FILE} 没有声明任何技能（文件损坏或被清空）`);
  }

  const duplicated = declared.filter((name, index) => declared.indexOf(name) !== index);
  for (const name of new Set(duplicated)) {
    failures.push(`${MANIFEST_FILE} 重复声明了技能：${name}`);
  }

  for (const name of declared) {
    const skillDir = join(skillsPath, name);
    if (!existsSync(skillDir)) {
      failures.push(`声明了 ${name}，但 ${SKILLS_DIR}/${name} 不存在（先同步，或从清单移除）`);
      continue;
    }

    if (!existsSync(join(skillDir, SKILL_ENTRY))) {
      failures.push(`${SKILLS_DIR}/${name} 缺少 ${SKILL_ENTRY}（宿主按该文件发现技能）`);
    }
  }

  const onDisk = readdirSync(skillsPath, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name);
  ownSkills = onDisk.filter((name) => !declared.includes(name));

  for (const name of ownSkills) {
    if (!existsSync(join(skillsPath, name, SKILL_ENTRY))) {
      failures.push(`${SKILLS_DIR}/${name} 缺少 ${SKILL_ENTRY}（自有技能同样必须可被宿主发现）`);
    }
  }
}

if (failures.length > 0) {
  console.error(`✖ 技能清单不一致（${failures.length} 个问题）：\n`);
  for (const failure of failures) console.error(`  ${failure}`);
  console.error(
    '\n清单是受管技能的归属来源（见 AGENTS.md 的技能一节）：用 `node scripts/sync-dotnet-skills.mjs` 同步，' +
      '\n不要手工增删受管目录；自有技能不需要登记。',
  );
  process.exitCode = 1;
} else {
  console.log(
    `✔ 技能清单一致（受管 ${declared.length} 个 / 自有 ${ownSkills.length} 个` +
      `${ownSkills.length > 0 ? `：${ownSkills.join('、')}` : ''}）`,
  );
}
