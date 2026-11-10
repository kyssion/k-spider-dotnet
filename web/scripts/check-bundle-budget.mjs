import { readFileSync, readdirSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { gzipSync } from 'node:zlib'

const here = dirname(fileURLToPath(import.meta.url))
const webRoot = dirname(here)
const distAssets = join(webRoot, 'dist', 'assets')
const budget = JSON.parse(readFileSync(join(webRoot, 'bundle-budget.json'), 'utf8'))

let files
try {
  files = readdirSync(distAssets)
} catch {
  console.error('✖ 未找到 dist/assets，请先运行 pnpm build')
  process.exit(1)
}

const sum = (extension) =>
  files
    .filter((file) => file.endsWith(extension))
    .map((file) => {
      const content = readFileSync(join(distAssets, file))
      return { raw: content.length, gzip: gzipSync(content).length }
    })
    .reduce((total, size) => ({ raw: total.raw + size.raw, gzip: total.gzip + size.gzip }), {
      raw: 0,
      gzip: 0,
    })

const assets = [
  { name: 'JS', key: 'js', extension: '.js' },
  { name: 'CSS', key: 'css', extension: '.css' },
]

let failed = false
console.log('首屏体积：')
for (const asset of assets) {
  const size = sum(asset.extension)
  const limit = budget[asset.key]
  const withinBudget = size.raw <= limit.raw && size.gzip <= limit.gzip
  if (!withinBudget) failed = true
  console.log(
    `  ${asset.name} 原始 ${(size.raw / 1000).toFixed(2)} kB / gzip ${(size.gzip / 1000).toFixed(2)} kB；` +
      `预算 ${(limit.raw / 1000).toFixed(2)} / ${(limit.gzip / 1000).toFixed(2)} kB ${withinBudget ? '✔' : '✖'}`,
  )
}

if (failed) {
  console.error('✖ 首屏体积超出预算；调高预算必须在提交说明中给出理由。')
  process.exit(1)
}

console.log('✔ 首屏体积在预算内')
