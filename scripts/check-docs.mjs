#!/usr/bin/env node
import { existsSync, readFileSync, readdirSync } from 'node:fs'
import { dirname, join, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const docsRoot = join(root, 'docs')
const docsIndex = join(docsRoot, 'README.md')
const ignoredDirectories = new Set([
  '.git',
  '.agents',
  '.claude',
  'bin',
  'dist',
  'node_modules',
  'obj',
  'test-results',
])
const legacyPatterns = [
  /\bsync-dotnet-skills\.sh\b/,
  /\bsync_dotnet_skills\.py\b/,
  /\bpython3\b/,
  /docs\/(?:crawler|process|admin|web)\.md/,
  /k-spider-web\/wwwroot/,
]

const errors = []

const walk = (directory) => {
  const files = []
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      if (ignoredDirectories.has(entry.name)) continue
      files.push(...walk(join(directory, entry.name)))
    } else if (entry.isFile() && entry.name.endsWith('.md')) {
      files.push(join(directory, entry.name))
    }
  }
  return files
}

const linksOf = (file) => {
  const text = readFileSync(file, 'utf8')
  const links = []
  const pattern = /\]\(([^)\s]+)(?:\s+["'][^"']*["'])?\)/g
  for (const match of text.matchAll(pattern)) {
    links.push(match[1])
  }
  return { text, links }
}

const slug = (heading) =>
  heading
    .toLowerCase()
    .replace(/[`*_~]/g, '')
    .replace(/[^\p{L}\p{N}\s-]/gu, '')
    .trim()
    .replace(/\s+/g, '-')

const anchorsOf = (file) => {
  const text = readFileSync(file, 'utf8')
  return new Set(
    [...text.matchAll(/^#{1,6}\s+(.+)$/gm)].map((match) => slug(match[1])),
  )
}

const isLocalLink = (link) =>
  !link.startsWith('#') &&
  !link.startsWith('http://') &&
  !link.startsWith('https://') &&
  !link.startsWith('mailto:') &&
  !link.startsWith('tel:') &&
  !link.startsWith('/')

const checkLinks = (file) => {
  const { links } = linksOf(file)
  for (const raw of links) {
    if (!isLocalLink(raw)) continue
    const [pathPart, anchor] = raw.split('#')
    if (!pathPart) continue
    let decoded
    try {
      decoded = decodeURIComponent(pathPart)
    } catch {
      decoded = pathPart
    }
    const target = resolve(dirname(file), decoded)
    if (!existsSync(target)) {
      errors.push(`${relative(root, file)} -> 缺失文件: ${raw}`)
      continue
    }
    if (anchor && target.endsWith('.md') && !anchorsOf(target).has(anchor)) {
      errors.push(`${relative(root, file)} -> 缺失锚点: ${raw}`)
    }
  }
}

const linkedPaths = (indexFile) => {
  const paths = new Set()
  for (const link of linksOf(indexFile).links) {
    if (!isLocalLink(link)) continue
    const pathPart = link.split('#')[0]
    if (pathPart) paths.add(resolve(dirname(indexFile), decodeURIComponent(pathPart)))
  }
  return paths
}

const checkIndex = (files) => {
  const indexed = linkedPaths(docsIndex)
  for (const file of files) {
    if (!file.startsWith(`${docsRoot}${sep}`)) continue
    const rel = relative(docsRoot, file)
    if (rel === 'README.md') continue
    if (rel.startsWith(`decisions${sep}`) || rel.startsWith(`templates${sep}`)) continue
    if (!indexed.has(file)) errors.push(`${relative(root, file)} 未在 docs/README.md 索引`)
  }

  const decisionsIndex = join(docsRoot, 'decisions', 'README.md')
  const indexedDecisions = linkedPaths(decisionsIndex)
  for (const file of files) {
    if (!file.startsWith(`${join(docsRoot, 'decisions')}${sep}`)) continue
    const name = relative(join(docsRoot, 'decisions'), file)
    if (name === 'README.md' || name === '0000-template.md') continue
    if (!indexedDecisions.has(file)) errors.push(`${relative(root, file)} 未在 decisions/README.md 索引`)
  }
}

const checkStatus = (files) => {
  for (const file of files) {
    if (!file.startsWith(`${docsRoot}${sep}`)) continue
    const rel = relative(docsRoot, file)
    const needsStatus = rel !== 'README.md' && !rel.startsWith(`templates${sep}`)
    if (!needsStatus) continue
    const text = readFileSync(file, 'utf8')
    const match = text.match(/^> \*\*状态\*\*：(.*)$/m)
    if (!match) {
      errors.push(`${relative(root, file)} 缺少状态块 ( > **状态**：... )`)
    } else if (
      !file.endsWith('0000-template.md') &&
      !/最后核对：\d{4}-\d{2}-\d{2}/.test(match[1])
    ) {
      errors.push(`${relative(root, file)} 状态缺少最后核对日期`)
    }
  }
}

const checkLegacyPaths = (files) => {
  for (const file of files) {
    if (!file.startsWith(`${docsRoot}${sep}`) && file !== join(root, 'README.md') && file !== join(root, 'AGENTS.md')) {
      continue
    }
    const { text } = linksOf(file)
    for (const pattern of legacyPatterns) {
      if (pattern.test(text)) errors.push(`${relative(root, file)} 含过期路径/命令: ${pattern}`)
    }
  }
}

const files = walk(root)
for (const file of files) checkLinks(file)
checkIndex(files)
checkStatus(files)
checkLegacyPaths(files)

if (errors.length > 0) {
  for (const error of [...new Set(errors)].sort()) console.error(`- ${error}`)
  console.error(`==> 文档检查失败 (${errors.length} 个问题)`)
  process.exit(1)
}

console.log(`==> 文档检查通过 (${files.length} 个 Markdown 文件)`)
