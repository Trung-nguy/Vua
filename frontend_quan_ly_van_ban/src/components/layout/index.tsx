import { NavLink, useNavigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import {
  LayoutDashboard, FileText, CheckCircle, Send, Archive,
  Bell, Users, BarChart3, Settings, LogOut, ChevronDown,
  Building2, Menu, X, Inbox, FileCheck
} from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { useAuthStore } from '@/stores/authStore'
import { thongBaoAPI } from '@/api'
import { canViewStats, canManageUsers, canIssueNumber, canDistribute, ROLE_LABELS } from '@/utils'
import type { RoleName } from '@/types'

// ─── Nav item config by role ──────────────────────────────────────────────────
const getNavItems = (role: RoleName) => {
  const base = [
    { to: '/dashboard', icon: LayoutDashboard, label: 'Tổng quan' },
    { to: '/van-ban', icon: FileText, label: 'Văn bản' },
  ]
  if (['GiangVien', 'VanThuKhoa'].includes(role))
    base.push({ to: '/van-ban/tao-moi', icon: Send, label: 'Tạo văn bản' })
  if (['TruongBoMon', 'Admin'].includes(role))
    base.push({ to: '/workflow/xac-minh', icon: CheckCircle, label: 'Xác minh chuyên môn' })
  if (['LanhDaoKhoa', 'Admin'].includes(role))
    base.push({ to: '/workflow/phe-duyet', icon: FileCheck, label: 'Phê duyệt văn bản' })
  if (canIssueNumber(role))
    base.push({ to: '/so-hieu', icon: Archive, label: 'Cấp số hiệu' })
  if (canDistribute(role))
    base.push({ to: '/phan-phoi', icon: Inbox, label: 'Phân phối' })
  if (canViewStats(role))
    base.push({ to: '/thong-ke', icon: BarChart3, label: 'Thống kê' })
  if (canManageUsers(role)) {
    base.push({ to: '/admin/nguoi-dung', icon: Users, label: 'Người dùng' })
    base.push({ to: '/admin/bo-mon', icon: Building2, label: 'Bộ môn' })
  }
  return base
}

// ─── Sidebar ─────────────────────────────────────────────────────────────────
export function Sidebar({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { user, clearAuth } = useAuthStore()
  const navigate = useNavigate()

  const handleLogout = async () => {
    clearAuth()
    navigate('/dang-nhap')
  }

  const navItems = user ? getNavItems(user.role) : []

  return (
    <>
      {/* Mobile overlay */}
      <AnimatePresence>
        {open && (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
            className="fixed inset-0 bg-ink-950/40 z-30 lg:hidden" onClick={onClose}/>
        )}
      </AnimatePresence>

      <motion.aside
        initial={false}
        animate={{ x: open ? 0 : -280 }}
        transition={{ type: 'spring', damping: 30, stiffness: 300 }}
        className="fixed left-0 top-0 bottom-0 w-[260px] z-40
                   bg-ink-950 flex flex-col lg:translate-x-0 lg:sticky lg:top-0 lg:h-screen"
      >
        {/* Logo */}
        <div className="px-5 pt-6 pb-5 flex items-center justify-between">
          <div>
            <div className="flex items-center gap-2.5">
              <div className="w-8 h-8 rounded-lg bg-jade-500 flex items-center justify-center
                              text-white font-display font-semibold text-sm">VB</div>
              <div>
                <p className="text-white font-semibold text-sm leading-tight">Quản Lý</p>
                <p className="text-ink-400 text-xs leading-tight">Văn Bản</p>
              </div>
            </div>
          </div>
          <button onClick={onClose} className="lg:hidden text-ink-400 hover:text-white transition-colors">
            <X size={18}/>
          </button>
        </div>

        <div className="px-3 mb-3">
          <div className="h-px bg-ink-800"/>
        </div>

        {/* Nav */}
        <nav className="flex-1 px-3 overflow-y-auto no-scrollbar space-y-0.5">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink key={to} to={to} onClick={onClose}
              className={({ isActive }) =>
                `nav-item ${isActive ? 'nav-item-active' : 'hover:bg-ink-800 hover:text-ink-100 text-ink-400'}`}
            >
              <Icon size={17} className="flex-shrink-0"/>
              <span className="truncate">{label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="px-3 mb-2"><div className="h-px bg-ink-800"/></div>

        {/* User section */}
        <div className="px-3 pb-4 space-y-1">
          <NavLink to="/cai-dat" onClick={onClose}
            className={({ isActive }) =>
              `nav-item ${isActive ? 'nav-item-active' : 'hover:bg-ink-800 hover:text-ink-100 text-ink-400'}`}>
            <Settings size={17}/><span>Cài đặt</span>
          </NavLink>
          <button onClick={handleLogout} className="nav-item w-full text-ink-400 hover:bg-rose-900/30 hover:text-rose-400">
            <LogOut size={17}/><span>Đăng xuất</span>
          </button>
        </div>

        {/* User info */}
        {user && (
          <div className="mx-3 mb-4 p-3 rounded-xl bg-ink-900 flex items-center gap-3">
            <div className="w-8 h-8 rounded-full bg-jade-600 flex items-center justify-center
                            text-white text-xs font-bold flex-shrink-0">
              {user.hoTen.charAt(0)}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-white text-xs font-medium truncate">{user.hoTen}</p>
              <p className="text-ink-400 text-xs truncate">{user.roleTenHienThi}</p>
            </div>
          </div>
        )}
      </motion.aside>
    </>
  )
}

// ─── Header ──────────────────────────────────────────────────────────────────
export function Header({ onMenuClick, title }: { onMenuClick: () => void; title?: string }) {
  const { user } = useAuthStore()
  const [unread, setUnread] = useState(0)
  const navigate = useNavigate()

  useEffect(() => {
    thongBaoAPI.demChuaDoc().then(r => setUnread(r.data.soChuaDoc)).catch(() => {})
    const t = setInterval(() => {
      thongBaoAPI.demChuaDoc().then(r => setUnread(r.data.soChuaDoc)).catch(() => {})
    }, 30_000)
    return () => clearInterval(t)
  }, [])

  return (
    <header className="h-[60px] bg-white border-b border-ink-100 flex items-center px-4 gap-3 sticky top-0 z-20">
      <button onClick={onMenuClick} className="btn-ghost btn-icon lg:hidden">
        <Menu size={18}/>
      </button>

      {title && (
        <h1 className="text-sm font-semibold text-ink-900 hidden sm:block">{title}</h1>
      )}

      <div className="flex-1"/>

      {/* Notifications */}
      <button onClick={() => navigate('/thong-bao')}
        className="btn-ghost btn-icon relative">
        <Bell size={18}/>
        {unread > 0 && (
          <span className="absolute -top-0.5 -right-0.5 h-4 w-4 rounded-full bg-rose-500
                           flex items-center justify-center text-white text-[9px] font-bold">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      {/* User avatar */}
      {user && (
        <button onClick={() => navigate('/cai-dat')}
          className="flex items-center gap-2 hover:bg-ink-50 rounded-xl px-2 py-1.5 transition-colors">
          <div className="w-7 h-7 rounded-full bg-jade-600 flex items-center justify-center
                          text-white text-xs font-bold">
            {user.hoTen.charAt(0)}
          </div>
          <div className="hidden md:block text-left">
            <p className="text-xs font-medium text-ink-900 leading-tight max-w-[120px] truncate">
              {user.hoTen}
            </p>
            <p className="text-xs text-ink-400 leading-tight">{user.roleTenHienThi}</p>
          </div>
          <ChevronDown size={14} className="text-ink-400 hidden md:block"/>
        </button>
      )}
    </header>
  )
}

// ─── App Layout ───────────────────────────────────────────────────────────────
export function AppLayout({ children, title }: { children: React.ReactNode; title?: string }) {
  const [sidebarOpen, setSidebarOpen] = useState(false)

  return (
    <div className="flex min-h-screen">
      <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)}/>
      <div className="flex-1 flex flex-col min-w-0">
        <Header onMenuClick={() => setSidebarOpen(true)} title={title}/>
        <main className="flex-1 p-4 md:p-6 overflow-auto">
          <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.25 }}>
            {children}
          </motion.div>
        </main>
      </div>
    </div>
  )
}
