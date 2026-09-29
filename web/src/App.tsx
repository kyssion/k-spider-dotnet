import { NavLink, Route, Routes } from 'react-router-dom'
import {
  BarChart3,
  LayoutDashboard,
  Newspaper,
  RotateCw,
  Settings2,
  Zap,
} from 'lucide-react'
import { cn } from './lib/utils'
import DashboardPage from './pages/DashboardPage'
import NewsPage from './pages/NewsPage'
import FlashPage from './pages/FlashPage'
import AnalysisPage from './pages/AnalysisPage'
import JobsPage from './pages/JobsPage'
import ReplayPage from './pages/ReplayPage'

const NAV_ITEMS = [
  { to: '/', label: '总览', icon: LayoutDashboard },
  { to: '/news', label: '网页新闻', icon: Newspaper },
  { to: '/flash', label: '实时快讯', icon: Zap },
  { to: '/analysis', label: '数据分析', icon: BarChart3 },
  { to: '/jobs', label: '任务管理', icon: Settings2 },
  { to: '/replay', label: '数据重放', icon: RotateCw },
]

export default function App() {
  return (
    <div className="flex min-h-screen">
      <aside className="sticky top-0 flex h-screen w-52 shrink-0 flex-col border-r bg-card">
        <div className="flex items-center gap-2 px-5 py-5">
          <span className="flex size-8 items-center justify-center rounded-lg bg-primary font-bold text-primary-foreground">
            K
          </span>
          <div>
            <div className="text-sm font-semibold">KSpider 控制台</div>
            <div className="text-xs text-muted-foreground">金融数据爬虫</div>
          </div>
        </div>
        <nav className="flex flex-col gap-1 px-3">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2 rounded-lg px-3 py-2 text-sm transition-colors',
                  isActive
                    ? 'bg-primary/10 font-medium text-primary'
                    : 'text-muted-foreground hover:bg-accent hover:text-foreground',
                )
              }
            >
              <item.icon className="size-4" />
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="mt-auto px-5 py-4 text-xs text-muted-foreground">
          数据来自爬虫节点上报
        </div>
      </aside>
      <main className="min-w-0 flex-1 p-6">
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/news" element={<NewsPage />} />
          <Route path="/flash" element={<FlashPage />} />
          <Route path="/analysis" element={<AnalysisPage />} />
          <Route path="/jobs" element={<JobsPage />} />
          <Route path="/replay" element={<ReplayPage />} />
        </Routes>
      </main>
    </div>
  )
}
