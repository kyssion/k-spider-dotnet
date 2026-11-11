import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

/** 组件测试使用 jsdom；纯逻辑测试同样在该环境运行，成本可接受。 */
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
  },
})
