#!/usr/bin/env node
/**
 * 文档重复正文检查（守门脚本）。
 *
 * 为什么需要它：
 *   仓库的写作原则是「一处编写，其余引用」（`docs/README.md`）。副本一定会漂移 ——
 *   同一个模块状态、同一张命令表、同一段配置说明写在两处，两处说法迟早互相矛盾，
 *   却不会让任何编译、测试或格式检查红灯。本脚本把「同一段正文出现两次」变成
 *   verify 的硬失败。
 *
 * 判定口径（刻意保守，误报为 0 优先）：
 *   1. 只看 **Markdown 正文行**，不看代码块内；
 *   2. 去掉行首尾空白与内部连续空白后**完全相同**才算重复 —— 不猜近义改写；
 *   3. 同一行出现在 2 处及以上（跨文件或同一文件内）即判定。
 *
 * 明确排除（不是漏检，是「不该判」）：
 *   - 代码块内容：同一段示例命令在多处出现是允许的（见 docs/README.md 的短命令例外）；
 *   - 标题（`#` 开头）：章节重名不构成内容重复；
 *   - 文档元数据（`> 状态：` / `> 最后更新：`）：按约定每篇都要有，本来就该一样；
 *   - 表格分隔行、分隔线、纯链接行：无信息量；
 *   - 长度 < MIN_LENGTH 的行：短句重合不足以证明是副本；
 *   - 第三方技能（manifest 登记的）与 ADR 正文：上游语境示例 / 历史快照；
 *   - ALLOWED_DUPLICATES 中登记的「有意重复」，每条必须写明理由；
 *     未命中任何重复的条目会在通过时提示清理，防止清单膨胀。
 *
 * 用法：
 *   node scripts/check-doc-duplication.mjs
 *
 * 退出码：0 = 无重复；1 = 存在重复正文（逐条列出 位置 → 文本）。
 */

import { readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { collectMarkdownFiles } from './lib/doc-files.mjs';

const ROOT_DIR = resolve(dirname(fileURLToPath(import.meta.url)), '..');

/** 短于此长度的行不参与判定（短句重合不足以证明是副本）。 */
const MIN_LENGTH = 24;

/** 去掉链接与标记后剩余不足此长度，视为导航行/占位行。 */
const MIN_RESIDUAL_LENGTH = 8;

/** 文档元数据：按约定每篇都有，内容天然相同。 */
const METADATA_PATTERN = /^>\s*(状态|最后更新|日期)\s*[:：]/;

/**
 * 有意的重复：每条 = 文本片段 + 理由。
 *
 * 只允许进三类：
 *   1. 历史快照（已实现的规格/ADR 引用同一段验收结论）；
 *   2. 模板占位（按设计就一样）；
 *   3. 术语定义（词典条目与正文同义，属不同粒度）。
 * 「懒得改」的重复不允许进这里 —— 那正是本脚本要拦住的。
 */
const ALLOWED_DUPLICATES = [
  {
    textIncludes: '| 文件 | 来源接口 | 抓取时间 | 内容 |',
    reason: '夹具登记文档的两张表共用同一表头（模板结构，不是正文副本）',
    hits: 0,
  },
];

/** 该行是否参与「重复」判定。 */
function isCheckable(line) {
  if (line.length < MIN_LENGTH) return false;
  if (line.startsWith('#')) return false;
  if (METADATA_PATTERN.test(line)) return false;
  if (/^\|[-:\s|]+\|$/.test(line)) return false;
  if (/^[-*_]{3,}$/.test(line)) return false;

  const residual = line
    .replace(/\[[^\]]*\]\([^)]*\)/g, '')
    .replace(/[`*_>#\-|:\s()[\]{}.,;!?'"]/g, '');
  return residual.length >= MIN_RESIDUAL_LENGTH;
}

const files = collectMarkdownFiles(ROOT_DIR);

/** key = 归一化后的整行；value = 出现位置列表。 */
const occurrences = new Map();
let checkedLines = 0;

for (const file of files) {
  const lines = readFileSync(join(ROOT_DIR, file), 'utf8').split(/\r?\n/);
  let inFence = false;

  lines.forEach((raw, index) => {
    const line = raw.trim();

    if (line.startsWith('```')) {
      inFence = !inFence;
      return;
    }
    if (inFence) return;
    if (!isCheckable(line)) return;

    checkedLines += 1;
    const key = line.replace(/\s+/g, ' ');
    if (!occurrences.has(key)) occurrences.set(key, []);
    occurrences.get(key).push({ file, line: index + 1 });
  });
}

const duplicates = [];
let exemptGroups = 0;
let exemptPlaces = 0;

for (const [text, places] of occurrences) {
  if (places.length < 2) continue;

  const exemption = ALLOWED_DUPLICATES.find((item) => text.includes(item.textIncludes));
  if (exemption) {
    exemption.hits += 1;
    exemptGroups += 1;
    exemptPlaces += places.length;
    continue;
  }

  duplicates.push({ text, places });
}

duplicates.sort((a, b) => {
  const fa = a.places[0];
  const fb = b.places[0];
  return fa.file.localeCompare(fb.file) || fa.line - fb.line;
});

const unusedExemptions = ALLOWED_DUPLICATES.filter((item) => item.hits === 0);

if (duplicates.length > 0) {
  console.error(`✖ 发现 ${duplicates.length} 处重复正文（共检查 ${checkedLines} 条正文行）：`);

  duplicates.forEach((dup, index) => {
    const where = dup.places.map((p) => `${p.file}:${p.line}`).join(' 与 ');
    const preview = dup.text.length > 90 ? `${dup.text.slice(0, 90)}…` : dup.text;
    console.error(`  ${index + 1}. ${where}`);
    console.error(`     「${preview}」\n`);
  });

  console.error(
    '按 docs/README.md「写作原则」处理：保留**唯一来源**那份，其余改为链接 + 一句话说明。\n' +
      '若确认属于历史快照 / 模板占位 / 术语定义，请在 ALLOWED_DUPLICATES 中登记并写明理由。',
  );
  process.exitCode = 1;
} else {
  console.log(
    `✔ 文档重复检查通过（${files.length} 个文件 / ${checkedLines} 条正文行` +
      `${exemptGroups > 0 ? `，已登记的 ${exemptGroups} 组（${exemptPlaces} 处）除外` : ''}）`,
  );

  if (unusedExemptions.length > 0) {
    console.log('\n以下豁免条目未命中任何重复，建议清理：');
    for (const item of unusedExemptions) {
      console.log(`  · ${item.textIncludes}（${item.reason}）`);
    }
  }
}
