import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './app/App'
import './index.css'

// 入口只负责挂载根组件；Provider、路由与主题都在 App 内组合。
ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
