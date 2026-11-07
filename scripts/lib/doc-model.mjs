/**
 * 文档模型：结构检查、状态检查与引用检查共用的「把文档读成数据」。
 *
 * 为什么单独抽：多个脚本都需要知道「有哪些 ADR / 规格」「正文的状态行是什么」
 * 「索引表的最后一列怎么取」。各写一份的话，这类约定会各自漂移 —— 那时会出现
 * 「结构检查说索引没问题、状态检查说对不上」的自相矛盾。
 * 分工：`doc-files.mjs` 决定扫描范围，`doc-refs.mjs` 决定一条引用指向哪，
 * 本文件只负责读取文档事实（文件清单、状态、索引行、引用目标）。
 */

import { readFileSync } from 'node:fs';
import { dirname, relative, resolve, sep } from 'node:path';
import { collectMarkdownFiles } from './doc-files.mjs';
import { PLACEHOLDER_PATTERN, inlineCandidates, linkCandidates } from './doc-refs.mjs';

export const DOCS_DIR = 'docs/';
export const DOCS_INDEX = 'docs/README.md';
export const ADR_DIR = 'docs/20-adr';
export const ADR_INDEX = `${ADR_DIR}/README.md`;
export const ADR_TEMPLATE = `${ADR_DIR}/0000-template.md`;
export const SPEC_DIR = 'docs/40-specs';
export const SPEC_INDEX = `${SPEC_DIR}/README.md`;
export const TECH_DOC_TEMPLATE = 'docs/30-guides/tech-doc-template.md';

/** 「开头」的判定范围：状态块必须出现在前 N 行内，避免被埋到正文中段。 */
export const HEAD_LINES = 10;

export const STATUS_PATTERN = /^> 状态：\S/m;
export const DATE_PATTERN = /^> 最后更新：\d{4}-\d{2}-\d{2}/m;

/** 去掉状态值后面的括注（`已取代（见 0007）` → `已取代`），便于跨文件比较。 */
export const stripNote = (value) => value.replace(/（[^）]*）/g, '').replace(/\([^)]*\)/g, '').trim();

/**
 * 建立文档模型。
 *
 * @param {string} rootDir 仓库根目录
 */
export function createDocModel(rootDir) {
  const toRel = (path) => relative(rootDir, path).split(sep).join('/');
  const toAbs = (relPath) => resolve(rootDir, relPath);

  // 含 ADR 正文：目录索引覆盖需要看到它们，引用可达性也要把 ADR 里的引用算作一条边。
  const files = collectMarkdownFiles(rootDir, { includeAdrBodies: true });
  const docsFiles = files.filter((file) => file.startsWith(DOCS_DIR));
  const adrFiles = files.filter(
    (file) => file.startsWith(`${ADR_DIR}/`) && /\/\d{4}-/.test(file) && file !== ADR_TEMPLATE,
  );
  const specFiles = files.filter((file) => file.startsWith(`${SPEC_DIR}/`) && /\/\d{4}-/.test(file));

  /**
   * 收集一个文件里能「指到别的文档」的目标（仓库相对路径）。
   *
   * 解析规则与引用检查共用 `doc-refs.mjs`；`includeFileRelative` 打开：结构检查只问
   * 「指没指到真实文档」，裸文件名也多算一条边。
   */
  function referencesOf(file) {
    const text = readFileSync(toAbs(file), 'utf8');
    const targets = [];

    for (const match of text.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
      const raw = match[1].trim().split(/\s+/)[0];
      if (!raw || /^(https?:|mailto:|#|\/|~)/i.test(raw)) continue;

      const pathPart = raw.split('#')[0];
      if (!pathPart || PLACEHOLDER_PATTERN.test(pathPart)) continue;
      targets.push(...linkCandidates(rootDir, file, pathPart).map(toRel));
    }

    for (const match of text.matchAll(/`([^`]+)`/g)) {
      const raw = match[1].trim();
      if (!raw.endsWith('.md') || /\s/.test(raw) || PLACEHOLDER_PATTERN.test(raw)) continue;
      targets.push(...inlineCandidates(rootDir, file, raw, { includeFileRelative: true }).map(toRel));
    }

    return targets;
  }

  /** 正文的状态行（去掉括注，便于与索引比较）；跳过代码块里的示例。 */
  function bodyStatus(file) {
    let inFence = false;

    for (const line of readFileSync(toAbs(file), 'utf8').split(/\r?\n/)) {
      if (line.trim().startsWith('```')) {
        inFence = !inFence;
        continue;
      }
      if (inFence) continue;

      const match = line.match(/^> 状态：(.+)$/);
      if (match) return stripNote(match[1]);
    }

    return '';
  }

  /** 解析索引表：`| [0001](./0001-x.md) | 标题 | 状态 |` → Map(仓库相对路径 → 最后一列)。 */
  function indexRows(indexFile) {
    const rows = new Map();
    for (const line of readFileSync(toAbs(indexFile), 'utf8').split(/\r?\n/)) {
      const link = line.match(/\]\(([^)]+\.md)\)/);
      if (!link) continue;

      const cells = line.split('|').map((cell) => cell.trim()).filter(Boolean);
      if (cells.length < 2) continue;
      rows.set(`${dirname(indexFile)}/${link[1].split('/').pop()}`, cells[cells.length - 1]);
    }

    return rows;
  }

  return { files, docsFiles, adrFiles, specFiles, toAbs, referencesOf, bodyStatus, indexRows };
}
