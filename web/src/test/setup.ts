// jest-dom 提供 toBeInTheDocument 等断言；Vitest 环境需要显式注册。
import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// 每个用例后清理 DOM 与持久化偏好，避免组件测试互相污染。
afterEach(() => {
  cleanup()
  localStorage.clear()
})
