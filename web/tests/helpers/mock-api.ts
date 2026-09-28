import fs from 'node:fs'
import type { Page } from '@playwright/test'

/** 夹具目录 ( ESM 无 __dirname , 用 import.meta.url 定位 ) */
const FIXTURES_DIR = new URL('../fixtures/', import.meta.url)

/** 接口路径 → 夹具文件名 ( 相对 tests/fixtures ) */
export type RouteTable = Record<string, string>

/**
 * 把 /api/* 全部拦截为夹具响应 :
 * - 夹具文本里的 "@@NOW@@" 会替换为当前时刻 ( 用于"在线"这类相对时间断言 )
 * - 命中未登记的接口直接抛错——比静默 404 更早暴露"页面新增了 API 却没配夹具"
 * 返回值带请求记录 , 供"筛选/分页参数是否真的发出"这类断言使用
 */
export async function mockApi(page: Page, routes: RouteTable) {
  const requested: string[] = []
  // 只拦业务请求 /api/* : 不能用 "**/api/**" 这类 glob , 会把 vite 的源码模块 /src/api/*.ts 一起拦掉
  await page.route(
    url => new URL(url).pathname.startsWith('/api/'),
    async route => {
      const url = new URL(route.request().url())
      requested.push(url.pathname + url.search)
      const file = routes[url.pathname]
      if (file == null) throw new Error(`未 mock 的接口 : ${url.pathname}`)
      const raw = fs
        .readFileSync(new URL(file, FIXTURES_DIR), 'utf8')
        .replaceAll('@@NOW@@', new Date().toISOString())
      return route.fulfill({ status: 200, contentType: 'application/json', body: raw })
    },
  )
  return {
    requested,
    /** 某个接口被请求过的完整 URL ( 含查询参数 ) 列表 */
    requests(pathname: string) {
      return requested.filter(item => item.startsWith(pathname))
    },
  }
}

/**
 * 拦截任务控制 POST 并回 202 受理 ( 注册在 mockApi 之后 , Playwright 后注册的路由先匹配 ) ;
 * 返回的数组记录实际发出的 POST 地址 , 供断言
 */
export async function mockPostAccepted(page: Page, action: string) {
  const posted: string[] = []
  await page.route(`**/api/jobs/*/${action}`, async route => {
    posted.push(new URL(route.request().url()).pathname)
    return route.fulfill({
      status: 202,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 99,
        nodeId: null,
        jobName: 'FlashNewsJob',
        action,
        status: 'pending',
        result: null,
        consumedAt: null,
        createTime: new Date().toISOString(),
      }),
    })
  })
  return posted
}
