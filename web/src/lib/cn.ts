import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

/**
 * 合并 className。
 *
 * 先用 clsx 处理条件值，再用 tailwind-merge 消除冲突的 Tailwind 类；
 * 组件库不存在时，这是保持样式覆盖行为一致的最小工具。
 * @param inputs 待合并的类名、条件值与 Tailwind 冲突类。
 * @returns 合并后的 className。
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs))
}
