#!/usr/bin/env node
import { execFileSync } from 'node:child_process'
import {
  copyFileSync,
  cpSync,
  existsSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  readdirSync,
  renameSync,
  rmSync,
  writeFileSync,
} from 'node:fs'
import { tmpdir } from 'node:os'
import { dirname, join, relative, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const scriptDir = dirname(fileURLToPath(import.meta.url))
const repoRoot = resolve(scriptDir, '..')
const skillsDir = join(repoRoot, '.agents', 'skills')
const metadataDir = join(repoRoot, '.agents', 'dotnet-skills')
const manifestPath = join(metadataDir, 'manifest.json')
const namePattern = /^[a-z0-9][a-z0-9-]{0,63}$/

const fail = (message) => {
  throw new Error(message)
}

const unquote = (value) => value.trim().replace(/^["']|["']$/g, '')

const parseFrontmatter = (skillFile) => {
  const text = readFileSync(skillFile, 'utf8')
  const match = text.match(/^---\r?\n([\s\S]*?)\r?\n---(?:\r?\n|$)/)
  if (!match) fail(`${skillFile} 缺少完整的 YAML frontmatter`)

  const lines = match[1].split(/\r?\n/)
  let name = ''
  let description = ''
  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index]
    const nameMatch = line.match(/^name:\s*(.*)$/)
    if (nameMatch) {
      name = unquote(nameMatch[1])
      continue
    }

    const descriptionMatch = line.match(/^description:\s*(.*)$/)
    if (!descriptionMatch) continue
    let value = descriptionMatch[1].trim()
    if (['', '>', '>-', '|', '|-'].includes(value)) {
      const block = []
      for (let next = index + 1; next < lines.length; next += 1) {
        if (lines[next] && !/^\s/.test(lines[next])) break
        block.push(lines[next].trim())
      }
      value = block.filter(Boolean).join(' ')
    }
    description = unquote(value)
  }

  if (!name) fail(`${skillFile} 缺少 name`)
  if (!description) fail(`${skillFile} 缺少 description`)
  return { name, description }
}

const listDirectories = (path) =>
  readdirSync(path, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)
    .sort()

const discover = (source) => {
  const discovered = []
  const seen = new Map()
  for (const plugin of listDirectories(join(source, 'plugins'))) {
    const skillsPath = join(source, 'plugins', plugin, 'skills')
    if (!existsSync(skillsPath)) continue
    for (const skill of listDirectories(skillsPath)) {
      const skillFile = join(skillsPath, skill, 'SKILL.md')
      if (!existsSync(skillFile)) continue
      const { name } = parseFrontmatter(skillFile)
      if (!namePattern.test(name)) fail(`技能名不合法: ${name} (${skillFile})`)
      if (name !== skill) fail(`技能名与目录不一致: ${name} != ${skill}`)
      if (seen.has(name)) fail(`技能名重复: ${name} (${seen.get(name)} / ${skillFile})`)
      seen.set(name, skillFile)
      discovered.push({
        name,
        plugin,
        path: relative(source, join(skillsPath, skill)).split('\\').join('/'),
      })
    }
  }
  if (discovered.length === 0) fail(`未在 ${source} 找到任何插件技能`)
  return discovered
}

const loadManifest = () => {
  if (!existsSync(manifestPath)) return {}
  return JSON.parse(readFileSync(manifestPath, 'utf8'))
}

const cloneUpstream = async (ref) => {
  const repository = process.env.DOTNET_SKILLS_REPO || 'https://github.com/dotnet/skills.git'
  for (let attempt = 1; attempt <= 3; attempt += 1) {
    const temp = mkdtempSync(join(tmpdir(), 'kspider-dotnet-skills-'))
    try {
      console.log(`==> 克隆 dotnet/skills (${ref})`)
      execFileSync('git', ['clone', '--depth', '1', '--branch', ref, repository, temp], {
        stdio: ['ignore', 'ignore', 'inherit'],
      })
      return temp
    } catch {
      rmSync(temp, { recursive: true, force: true })
      if (attempt === 3) fail('克隆 dotnet/skills 失败；可用 DOTNET_SKILLS_SOURCE 指向本地仓库')
      console.error(`克隆失败，重试 (${attempt}/3)...`)
      await new Promise((resolvePromise) => setTimeout(resolvePromise, 2000))
    }
  }
  return null
}

const run = async () => {
  const ref = process.argv[2] || 'main'
  let temp = null
  let source = process.env.DOTNET_SKILLS_SOURCE
  if (source) {
    source = resolve(source)
  } else {
    temp = await cloneUpstream(ref)
    source = temp
  }

  try {
    const discovered = discover(source)
    const incoming = new Set(discovered.map((skill) => skill.name))
    const previous = loadManifest()
    const managed = new Set((previous.skills || []).map((skill) => skill.name))

    for (const skill of discovered) {
      const destination = join(skillsDir, skill.name)
      if (existsSync(destination) && !managed.has(skill.name)) {
        fail(`目标已存在且不属于本脚本管理: ${destination}; 请改名或移走后再同步`)
      }
    }

    for (const stale of managed) {
      if (!incoming.has(stale)) rmSync(join(skillsDir, stale), { recursive: true, force: true })
    }

    mkdirSync(skillsDir, { recursive: true })
    for (const skill of discovered) {
      const destination = join(skillsDir, skill.name)
      rmSync(destination, { recursive: true, force: true })
      cpSync(join(source, skill.path), destination, {
        recursive: true,
        preserveTimestamps: true,
      })
    }

    mkdirSync(metadataDir, { recursive: true })
    if (existsSync(join(source, 'LICENSE'))) {
      copyFileSync(join(source, 'LICENSE'), join(metadataDir, 'LICENSE'))
    }

    let commit = 'unknown'
    try {
      commit = execFileSync('git', ['-C', source, 'rev-parse', 'HEAD'], { encoding: 'utf8' }).trim()
    } catch {
      // 本地目录不是 git 仓库时保留 unknown
    }

    const unchanged =
      previous.commit === commit &&
      JSON.stringify(previous.skills || []) === JSON.stringify(discovered)
    const manifest = {
      source: 'https://github.com/dotnet/skills',
      commit,
      installedAt: unchanged && previous.installedAt ? previous.installedAt : new Date().toISOString(),
      skills: discovered,
    }
    const temporaryManifest = `${manifestPath}.tmp`
    writeFileSync(temporaryManifest, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8')
    renameSync(temporaryManifest, manifestPath)

    console.log(`==> 已同步 ${discovered.length} 个技能 -> ${skillsDir}`)
    console.log(`==> 来源提交: ${commit}`)
  } finally {
    if (temp) rmSync(temp, { recursive: true, force: true })
  }
}

run().catch((error) => {
  console.error(`错误: ${error instanceof Error ? error.message : String(error)}`)
  process.exit(1)
})
