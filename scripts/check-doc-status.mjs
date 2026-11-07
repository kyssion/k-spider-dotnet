#!/usr/bin/env node
/**
 * 文档状态一致性检查（守门脚本）。
 *
 * 为什么需要它：
 *   索引表是正文状态的**第二份副本**（ADR README 与规格 README 各有一张状态表）。
 *   这类不一致不会让任何编译或测试红灯 —— 只能靠比对发现。本脚本只做跨文件比对，
 *   **不抄词表**：允许值从 `docs/README.md` 的状态示例里读取。
 *
 * 四条规则（判据以文档为准，本脚本只做判定）：
 *   1. 索引状态 = 正文状态（ADR 与规格）：索引是副本，冲突时改正文；
 *   2. ADR 分组 = 正文状态：「当前生效」只能是已确认，「历史」只能是已取代 / 已废弃；
 *   3. 规格状态为「已实现」必须带「## 验收结论」；
 *   4. 状态取值必须属于 `docs/README.md` 的单一词表。
 *
 * 明确排除（不是漏检，是「不该判」）：
 *   - 按文档类型划分的**合法子集**不做校验（词表只有一份，子集语义仍靠评审）；
 *   - 索引里没有的条目不在本脚本报（漏登记归 check-doc-structure.mjs 的索引覆盖）。
 *
 * 用法：
 *   node scripts/check-doc-status.mjs
 *
 * 退出码：0 = 一致；1 = 存在不一致（逐条列出）。
 */

import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  ADR_INDEX,
  DOCS_INDEX,
  SPEC_INDEX,
  createDocModel,
  stripNote,
} from './lib/doc-model.mjs';

const ROOT_DIR = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const { docsFiles, adrFiles, specFiles, toAbs, bodyStatus, indexRows } = createDocModel(ROOT_DIR);

const failures = [];

// 允许值从 docs/README「文档状态」的示例行读取，脚本里不再抄一份词表
const statusSection = readFileSync(toAbs(DOCS_INDEX), 'utf8').split('## 文档状态')[1] ?? '';
const allowedStatuses = new Set(
  (statusSection.match(/^> 状态：(.+)$/m)?.[1] ?? '')
    .split('|')
    .map((value) => stripNote(value).trim())
    .filter(Boolean),
);

if (allowedStatuses.size === 0) {
  failures.push(`${DOCS_INDEX} 读不到状态词表（「文档状态」的示例行）`);
}

for (const file of docsFiles) {
  if (file === DOCS_INDEX) continue; // 索引自身的状态示例在代码块里

  const value = bodyStatus(file);
  if (value && !allowedStatuses.has(value)) {
    failures.push(`${file} 状态「${value}」不在词表（${DOCS_INDEX}「文档状态」）`);
  }
}

// ── 1. 索引表状态 = 正文状态（ADR 与规格）────────────────────────────────
for (const [indexFile, group] of [
  [ADR_INDEX, adrFiles],
  [SPEC_INDEX, specFiles],
]) {
  const rows = indexRows(indexFile);

  for (const file of group) {
    const indexed = rows.get(file);
    if (indexed === undefined) continue; // 未登记由结构检查或人工兜底，不在本条重复报

    const body = bodyStatus(file);
    if (body && body !== stripNote(indexed)) {
      failures.push(
        `${file} 状态与索引不一致（正文「${body}」，${indexFile} 索引「${stripNote(indexed)}」）`,
      );
    }
  }
}

// ── 2. ADR 分组 = 正文状态 ───────────────────────────────────────────────
const adrIndexText = readFileSync(toAbs(ADR_INDEX), 'utf8');
const effectivePart = adrIndexText.split('### 当前生效')[1]?.split('### 历史')[0];
const historyPart = adrIndexText.split('### 历史')[1];

if (effectivePart === undefined || historyPart === undefined) {
  // 分组标题被改名时不能静默跳过：否则「分组 = 状态」这条规则等于失效
  failures.push(`${ADR_INDEX} 缺少「### 当前生效」或「### 历史」分组标题，分组一致性无法校验`);
} else {
  for (const file of adrFiles) {
    const name = file.split('/').pop();
    const body = bodyStatus(file);

    if (effectivePart.includes(name) && body !== '已确认') {
      failures.push(`${file} 列在「当前生效」但正文状态是「${body}」`);
    }

    if (historyPart.includes(name) && body !== '已取代' && body !== '已废弃') {
      failures.push(`${file} 列在「历史」但正文状态是「${body}」`);
    }
  }
}

// ── 3. 规格「已实现」必须有验收结论 ─────────────────────────────────────
for (const file of specFiles) {
  if (!bodyStatus(file).startsWith('已实现')) continue;

  if (!/^## 验收结论/m.test(readFileSync(toAbs(file), 'utf8'))) {
    failures.push(`${file} 状态是「已实现」但缺少「## 验收结论」`);
  }
}

if (failures.length > 0) {
  console.error(`✖ 发现 ${failures.length} 个状态不一致问题：\n`);
  for (const failure of failures) console.error(`  ${failure}`);
  console.error(
    `\n判据：${ADR_INDEX}「规则」与 ${SPEC_INDEX}「命名与状态」。` +
      '\n索引是副本，冲突时改正文；ADR 在分组与状态之间二选一时，先改正文再同步索引与分组；' +
      '\n「已实现」的规格补一节「## 验收结论」。',
  );
  process.exitCode = 1;
} else {
  console.log(
    `✔ 文档状态检查通过（${adrFiles.length} 篇 ADR / ${specFiles.length} 篇规格，` +
      '状态与索引一致、已实现规格均有验收结论）',
  );
}
