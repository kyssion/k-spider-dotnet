#!/usr/bin/env node
/**
 * 同步 vendored 的上游 .NET 技能包到 `.agents/skills/<技能>/`。
 *
 * 为什么是「平铺」而不是按插件分子目录：
 *   `.agents/skills/*\/SKILL.md` 是 Agent Skills 的通用发现约定，宿主扫的是**一层**。
 *   曾经把它放进 `dotnet-skills/<包>/<技能>/`，结果宿主扫不到、只能靠 AGENTS.md 手工指路 ——
 *   技能放进去的目的就是被加载，扫不到等于没装。分组信息不需要靠路径表达，
 *   它记在 `manifest.json` 的 `plugin` 字段里。
 *
 * 为什么是「按清单解析」而不是「按目录拷贝」：
 *   上游是**插件市场包**：`marketplace.json` 声明插件与 `source`，插件的 `plugin.json`
 *   再声明 `skills` 路径，技能名只存在于文件系统里。硬编码 `plugins/<包>/skills/<技能>`
 *   在上游调整目录时会**静默少导**（旧实现就是这样，且会因为「这一版没有」而删掉库存）。
 *   本脚本先解析清单、再校验、最后才写盘；任何一步对不上就整体失败，不写半棵树。
 *
 * 归属模型（与上游安装器一致）：
 *   `manifest.json` 记录「哪些技能由本脚本管理」。同步只增删改这些目录；
 *   同名的非受管目录（手写技能）会让脚本报错而不是被覆盖。
 *   因此 `.agents/skills/` 里可以同时放自有技能，互不干扰。
 *
 * 用法：
 *   ./scripts/sync-dotnet-skills.mjs                      # 跟随上游 main
 *   ./scripts/sync-dotnet-skills.mjs --check              # 只报告是否有更新（不写文件）
 *   ./scripts/sync-dotnet-skills.mjs --ref v1.0.0         # 指定分支 / 标签 / 提交
 *   ./scripts/sync-dotnet-skills.mjs --source ../checkout # 用本地检出（离线 / 内网镜像）
 *   ./scripts/sync-dotnet-skills.mjs --force              # 版本未变也重写（修复本地改动）
 *   ./scripts/sync-dotnet-skills.mjs --allow-plugin-removal
 *
 * 环境变量：
 *   DOTNET_SKILLS_SOURCE   等价于 --source
 *   DOTNET_SKILLS_REPO     覆盖上游仓库地址（镜像）
 *   HTTPS_PROXY / https_proxy / ALL_PROXY   命令行 git 不继承系统代理，需要时显式导出
 *
 * 退出码：0 = 已是最新或同步完成；1 = 失败。
 *   校验阶段失败时不动任何技能目录；写入阶段失败会留下半棵树，但 manifest 未更新，
 *   下次同步会整体重写（自愈），因此不必手工收拾。
 * 同步后必须跑 `./scripts/verify.sh` 再提交。
 */

import { execFileSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, mkdtempSync, readdirSync, readFileSync, renameSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { basename, dirname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  MANIFEST_FILE as MANIFEST_FILE_REL,
  META_DIR as META_DIR_REL,
  SKILL_ENTRY,
  SKILLS_DIR as SKILLS_DIR_REL,
  buildManifest,
  declaredSkills,
  readManifest,
  serializeManifest,
} from './lib/skills-manifest.mjs';

const ROOT_DIR = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const SKILLS_DIR = join(ROOT_DIR, SKILLS_DIR_REL);
const META_DIR = join(ROOT_DIR, META_DIR_REL);
const MANIFEST_FILE = join(ROOT_DIR, MANIFEST_FILE_REL);
const SOURCE_URL = 'https://github.com/dotnet/skills';

const MARKETPLACE_CANDIDATES = ['.claude-plugin/marketplace.json', '.agents/plugins/marketplace.json'];
const CLONE_ATTEMPTS = 3;

const COLOR = {
  bold: '\x1b[1m',
  reset: '\x1b[0m',
  green: '\x1b[1;32m',
  yellow: '\x1b[1;33m',
  red: '\x1b[1;31m',
  cyan: '\x1b[1;36m',
};

const say = (text = '') => console.log(text);
const step = (text) => console.log(`\n${COLOR.cyan}▶ ${text}${COLOR.reset}`);
const ok = (text) => console.log(`${COLOR.green}✔ ${text}${COLOR.reset}`);
const warn = (text) => console.log(`${COLOR.yellow}! ${text}${COLOR.reset}`);

function fail(text) {
  console.error(`${COLOR.red}✖ ${text}${COLOR.reset}`);
  process.exit(1);
}

// ── 参数 ────────────────────────────────────────────────────────────────

const options = {
  check: false,
  ref: 'main',
  source: process.env.DOTNET_SKILLS_SOURCE || '',
  force: false,
  allowPluginRemoval: false,
};

const argv = process.argv.slice(2);
for (let index = 0; index < argv.length; index += 1) {
  switch (argv[index]) {
    case '--check':
      options.check = true;
      break;
    case '--force':
      options.force = true;
      break;
    case '--allow-plugin-removal':
      options.allowPluginRemoval = true;
      break;
    case '--ref':
      options.ref = argv[(index += 1)] ?? '';
      if (!options.ref) fail('--ref 需要一个参数（分支 / 标签 / 提交）');
      break;
    case '--source':
      options.source = argv[(index += 1)] ?? '';
      if (!options.source) fail('--source 需要一个本地目录');
      break;
    default:
      fail(`未知参数：${argv[index]}（可用：--check / --ref <ref> / --source <dir> / --force / --allow-plugin-removal）`);
  }
}

if (options.source) options.source = resolve(ROOT_DIR, options.source);

// ── 归属清单 ────────────────────────────────────────────────────────────

function loadManifest() {
  try {
    return readManifest(ROOT_DIR).manifest;
  } catch (error) {
    fail(`manifest.json 无法解析（${MANIFEST_FILE}）：${error.message}`);
  }
}

const previous = loadManifest();
const previousSkills = declaredSkills(previous);
const managedNames = new Set(previousSkills.map((skill) => skill.name));
const unlistedPlugins = previous?.unlisted ?? [];

// ── git（代理只对本次调用生效，不改任何配置） ──────────────────────────

const proxy = process.env.HTTPS_PROXY || process.env.https_proxy || process.env.ALL_PROXY || process.env.all_proxy || '';
const repository = process.env.DOTNET_SKILLS_REPO || `${SOURCE_URL}.git`;

function git(args, { stream = false } = {}) {
  const proxyArgs = proxy ? ['-c', `http.proxy=${proxy}`, '-c', `https.proxy=${proxy}`] : [];

  return execFileSync('git', [...proxyArgs, ...args], {
    encoding: 'utf8',
    stdio: stream ? 'inherit' : ['ignore', 'pipe', 'pipe'],
    env: { ...process.env, GIT_TERMINAL_PROMPT: '0' },
  });
}

function remoteCommit(target) {
  if (/^[0-9a-f]{40}$/.test(target)) return target;

  let output = '';
  try {
    output = git(['ls-remote', repository, target, `refs/heads/${target}`, `refs/tags/${target}`]);
  } catch (error) {
    fail(`无法访问上游仓库：${error.message.split('\n')[0]}\n  检查网络，或按需导出 HTTPS_PROXY`);
  }

  const sha = output.split('\n').map((line) => line.split('\t')[0]).filter(Boolean)[0];
  if (!sha) fail(`上游没有 ref：${target}`);
  return sha;
}

const sleep = (ms) => new Promise((complete) => setTimeout(complete, ms));

async function cloneUpstream(commit) {
  for (let attempt = 1; attempt <= CLONE_ATTEMPTS; attempt += 1) {
    const directory = mkdtempSync(join(tmpdir(), 'dotnet-skills-'));
    try {
      git(['clone', '--quiet', '--depth', '1', repository, directory]);
      if (git(['-C', directory, 'rev-parse', 'HEAD']).trim() !== commit) {
        git(['-C', directory, 'fetch', '--quiet', '--depth', '1', 'origin', commit]);
        git(['-C', directory, '-c', 'advice.detachedHead=false', 'checkout', '--quiet', 'FETCH_HEAD']);
      }
      return directory;
    } catch (error) {
      rmSync(directory, { recursive: true, force: true });
      if (attempt === CLONE_ATTEMPTS) {
        fail(`克隆上游失败：${error.message.split('\n')[0]}\n  可用 --source <本地检出> 离线同步`);
      }
      warn(`克隆失败，重试（${attempt}/${CLONE_ATTEMPTS}）…`);
      await sleep(2000);
    }
  }
}

// ── 解析上游格式 ────────────────────────────────────────────────────────

const toPosix = (path) => path.split(sep).join('/');

function readJson(file, label) {
  try {
    return JSON.parse(readFileSync(file, 'utf8'));
  } catch (error) {
    fail(`${label} 无法解析：${file}\n  ${error.message}`);
  }
}

function findMarketplace(upstream) {
  for (const candidate of MARKETPLACE_CANDIDATES) {
    const full = join(upstream, candidate);
    if (existsSync(full)) return { file: candidate, manifest: readJson(full, '插件清单') };
  }

  fail(
    `上游找不到插件清单（已查找：${MARKETPLACE_CANDIDATES.join('、')}）。\n` +
      '  这是「上游格式已变」的信号，请先人工确认清单新位置，再更新 MARKETPLACE_CANDIDATES。',
  );
}

function publishedPlugins(marketplace) {
  const entries = marketplace.manifest.plugins;
  if (!Array.isArray(entries) || entries.length === 0) {
    fail(`清单里没有 plugins 数组：${marketplace.file}（上游格式可能已变）`);
  }

  return entries.map((entry) => {
    if (typeof entry.source !== 'string') {
      fail(
        `插件 ${entry.name} 的 source 不是本地相对路径（${JSON.stringify(entry.source)}）。\n` +
          '  远端 / 包管理器 source 无法静态导入，请先确认上游是否换了分发方式。',
      );
    }

    return {
      name: entry.name,
      source: entry.source,
      unlisted: false,
      agentCount: 0,
      mcpServers: entry.mcpServers ? Object.keys(entry.mcpServers) : [],
    };
  });
}

/** 清单未登记、但 manifest 里保留的插件（上游未发布的实验包）。 */
function withUnlistedPlugins(upstream, published) {
  const extra = [];
  const pluginsRoot = dirname(published[0].source);

  for (const name of unlistedPlugins) {
    if (published.some((plugin) => plugin.name === name)) continue;

    const source = resolve(upstream, pluginsRoot, name);
    if (!existsSync(join(source, 'plugin.json'))) {
      warn(`清单外插件 ${name} 在本次上游里已不存在，将从 manifest 的 unlisted 中移除`);
      continue;
    }

    extra.push({ name, source: toPosix(relative(upstream, source)), unlisted: true, agentCount: 0, mcpServers: [] });
  }

  return [...published, ...extra];
}

/** 插件声明的技能根目录（plugin.json 的 skills 字段，可能是多个）。 */
function declaredSkillPaths(upstream, plugin) {
  const pluginRoot = resolve(upstream, plugin.source);
  const manifestFile = join(pluginRoot, 'plugin.json');

  if (!existsSync(manifestFile)) fail(`插件 ${plugin.name} 缺少 plugin.json（${plugin.source}）—— 上游格式可能已变`);

  const manifest = readJson(manifestFile, `${plugin.name}/plugin.json`);
  if (!Array.isArray(manifest.skills) || manifest.skills.length === 0) {
    fail(`插件 ${plugin.name} 的 plugin.json 没有声明 skills —— 上游可能改了技能声明方式`);
  }

  plugin.agentCount = Array.isArray(manifest.agents) ? manifest.agents.length : 0;

  return manifest.skills.map((entry) => resolve(pluginRoot, entry));
}

/** 技能根目录可能是「单个技能」或「技能容器」，两种都要认。 */
function discoverSkillDirs(upstream, plugin, skillsPath) {
  if (!existsSync(skillsPath)) {
    fail(`插件 ${plugin.name} 声明的技能路径不存在：${toPosix(relative(upstream, skillsPath))}`);
  }

  if (existsSync(join(skillsPath, SKILL_ENTRY))) return [skillsPath];

  const found = readdirSync(skillsPath, { withFileTypes: true })
    .filter((entry) => entry.isDirectory() && existsSync(join(skillsPath, entry.name, SKILL_ENTRY)))
    .map((entry) => join(skillsPath, entry.name));

  if (found.length === 0) {
    fail(
      `插件 ${plugin.name} 的技能目录下没有任何 SKILL.md：${toPosix(relative(upstream, skillsPath))}\n` +
        '  上游可能改了技能文件名或层级，请先确认再更新本脚本。',
    );
  }

  return found;
}

/** 只取 frontmatter 里的顶层标量，够用且不需要引入 YAML 依赖。 */
function readFrontmatter(skillDir) {
  const text = readFileSync(join(skillDir, SKILL_ENTRY), 'utf8');
  const block = /^---\r?\n([\s\S]*?)\r?\n---/.exec(text);

  if (!block) fail(`${skillDir}/SKILL.md 缺少 YAML frontmatter`);

  const fields = {};
  for (const line of block[1].split(/\r?\n/)) {
    const match = /^([A-Za-z][\w-]*):\s*(.*)$/.exec(line);
    if (match) fields[match[1]] = match[2].trim().replace(/^["'](.*)["']$/, '$1');
  }

  return fields;
}

function validateSkill(upstream, skillDir) {
  const fields = readFrontmatter(skillDir);
  const name = basename(skillDir);
  const where = toPosix(relative(upstream, skillDir));

  if (!fields.name) fail(`${where}/SKILL.md 的 frontmatter 缺少 name`);
  if (!fields.description) fail(`${where}/SKILL.md 的 frontmatter 缺少 description`);
  if (fields.name !== name) {
    fail(`${where}/SKILL.md 的 name=${fields.name} 与目录名 ${name} 不一致 —— 平铺后无法安全改名，请先人工确认`);
  }

  return name;
}

// ── 主流程 ──────────────────────────────────────────────────────────────

async function main() {
  let commit;
  let upstream;

  if (options.source) {
    if (!existsSync(options.source)) fail(`--source 目录不存在：${options.source}`);
    upstream = options.source;

    try {
      commit = git(['-C', upstream, 'rev-parse', 'HEAD']).trim();
    } catch {
      commit = 'unknown';
    }
  } else {
    commit = remoteCommit(options.ref);
  }

  say(`${COLOR.bold}上游技能包同步${COLOR.reset}`);
  say(`  来源 ${options.source || repository}`);
  say(`  上游 ${commit}`);
  say(`  本地 ${previous?.commit ?? '未安装'}`);

  const unchangedCommit = previous?.commit === commit;

  if (unchangedCommit && !options.force) {
    ok('已是最新，无需同步（manifest 与上游一致）');
    return;
  }

  if (options.check) {
    console.log(`\n${COLOR.yellow}! 上游有新提交，执行 ./scripts/sync-dotnet-skills.mjs 同步${COLOR.reset}`);
    return;
  }

  let clonedByUs = false;
  try {
    if (!upstream) {
      step('克隆上游（--depth 1）');
      upstream = await cloneUpstream(commit);
      clonedByUs = true;
    }

    step('解析上游清单');
    const marketplace = findMarketplace(upstream);
    const plugins = withUnlistedPlugins(upstream, publishedPlugins(marketplace));

    const discovered = [];
    let ignoredAgents = 0;
    const mcpPlugins = [];

    for (const plugin of plugins) {
      for (const skillsPath of declaredSkillPaths(upstream, plugin)) {
        for (const skillDir of discoverSkillDirs(upstream, plugin, skillsPath)) {
          discovered.push({
            name: validateSkill(upstream, skillDir),
            plugin: plugin.name,
            path: toPosix(relative(upstream, skillDir)),
            dir: skillDir,
          });
        }
      }

      ignoredAgents += plugin.agentCount ?? 0;
      if (plugin.mcpServers.length > 0) mcpPlugins.push(`${plugin.name}（${plugin.mcpServers.join('/')}）`);
    }

    discovered.sort((left, right) => left.name.localeCompare(right.name));

    const seen = new Map();
    for (const skill of discovered) {
      if (seen.has(skill.name)) fail(`技能名重复：${skill.name}（${seen.get(skill.name)} / ${skill.plugin}）`);
      seen.set(skill.name, skill.plugin);
    }

    const publishedCount = plugins.filter((plugin) => !plugin.unlisted).length;
    const unlistedCount = plugins.length - publishedCount;

    say(`  清单 ${marketplace.file}`);
    say(
      `  已发布插件 ${publishedCount}${unlistedCount > 0 ? ` + 清单外 ${unlistedCount}` : ''} · 技能 ${discovered.length}`,
    );

    // 归属：同名目录若不是本脚本装的，宁可报错也不覆盖（可能是手写技能）
    for (const skill of discovered) {
      const destination = join(SKILLS_DIR, skill.name);
      if (existsSync(destination) && !managedNames.has(skill.name)) {
        fail(
          `目标已存在且不受本脚本管理：.agents/skills/${skill.name}\n` +
            '  请改名或移走后重试；本脚本只增删改 manifest 里记录过的目录。',
        );
      }
    }

    const incomingNames = new Set(discovered.map((skill) => skill.name));
    const incomingPlugins = new Set(discovered.map((skill) => skill.plugin));
    const unlistedSet = new Set(plugins.filter((plugin) => plugin.unlisted).map((plugin) => plugin.name));

    const vanishedPlugins = [...new Set(previousSkills.map((skill) => skill.plugin))].filter(
      (plugin) => !incomingPlugins.has(plugin) && !unlistedSet.has(plugin),
    );

    if (vanishedPlugins.length > 0 && !options.allowPluginRemoval) {
      fail(
        `上游清单里不再有这些插件：${vanishedPlugins.join('、')}。\n` +
          '  这可能是上游重组（拆包/改名），也可能是我们的解析漏了 —— 必须先人工确认，\n' +
          '  否则会连带删掉它们的全部技能。确认后加 --allow-plugin-removal 重跑。',
      );
    }

    const added = discovered.filter((skill) => !managedNames.has(skill.name));
    const removed = previousSkills.filter((skill) => !incomingNames.has(skill.name));

    step(`写入 .agents/skills/（${discovered.length} 个技能）`);

    for (const skill of removed) {
      rmSync(join(SKILLS_DIR, skill.name), { recursive: true, force: true });
    }

    for (const skill of discovered) {
      const destination = join(SKILLS_DIR, skill.name);
      rmSync(destination, { recursive: true, force: true });
      cpSync(skill.dir, destination, { recursive: true, preserveTimestamps: true });
    }

    const licenseFile = join(upstream, 'LICENSE');
    if (!existsSync(licenseFile)) fail('上游缺少 LICENSE —— MIT 要求随分发保留，已中止');
    mkdirSync(META_DIR, { recursive: true });
    cpSync(licenseFile, join(META_DIR, 'LICENSE'));

    const skills = discovered.map((skill) => ({ name: skill.name, plugin: skill.plugin, path: skill.path }));
    const sameSkills = JSON.stringify(declaredSkills(previous)) === JSON.stringify(skills);
    const manifest = buildManifest({
      source: SOURCE_URL,
      commit,
      installedAt: sameSkills && unchangedCommit && previous?.installedAt ? previous.installedAt : new Date().toISOString(),
      unlisted: plugins.filter((plugin) => plugin.unlisted).map((plugin) => plugin.name),
      skills,
    });

    const temporaryManifest = `${MANIFEST_FILE}.tmp`;
    writeFileSync(temporaryManifest, serializeManifest(manifest), 'utf8');
    renameSync(temporaryManifest, MANIFEST_FILE);

    if (added.length > 0) {
      say('');
      say(`新增（${added.length}）：`);
      for (const skill of added) say(`  + ${skill.name}（${skill.plugin}）`);
    }

    if (removed.length > 0) {
      say('');
      say(`移除（${removed.length}）：`);
      for (const skill of removed) say(`  - ${skill.name}（${skill.plugin}）`);
    }

    if (added.length === 0 && removed.length === 0) {
      say('');
      say('技能集合无增减（仅正文变更，详见 git diff）');
    }

    if (ignoredAgents > 0 || mcpPlugins.length > 0) {
      say('');
      say(`未导入：agents ${ignoredAgents} 个${mcpPlugins.length > 0 ? `、MCP 服务器 ${mcpPlugins.join('、')}` : ''}`);
    }

    say('');
    ok(`已同步 ${skills.length} 个技能 → .agents/skills/（上游 ${commit}）`);
    say('下一步：./scripts/verify.sh 全绿后提交；AGENTS.md 技能章节若受影响需同步修改');
  } finally {
    if (clonedByUs && upstream) rmSync(upstream, { recursive: true, force: true });
  }
}

main().catch((error) => {
  fail(error instanceof Error ? error.message : String(error));
});
