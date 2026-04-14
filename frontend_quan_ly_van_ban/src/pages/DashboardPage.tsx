import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
  FileText, Clock, CheckCircle, AlertTriangle, Bell, Inbox,
  ArrowRight, TrendingUp, Shield, Send
} from 'lucide-react'
import { vanBanAPI } from '@/api'
import { useAuthStore } from '@/stores/authStore'
import { StatusBadge, Spinner } from '@/components/custom-ui'
import { fmtRelative, fmtDate, canApproveStep2, canApproveStep3, canIssueNumber } from '@/utils'
import { AppLayout } from '@/components/layout'
import type { Dashboard } from '@/types'

// ─── Stat card ────────────────────────────────────────────────────────────────
function StatCard({ icon: Icon, label, value, color, to, urgent }: {
  icon: React.ElementType; label: string; value: number;
  color: string; to?: string; urgent?: boolean
}) {
  const Wrap = to ? Link : 'div'
  return (
    <motion.div whileHover={{ y: -2 }} className="h-full">
      <Wrap to={to as string}
        className={`card p-5 flex items-start gap-4 h-full
          ${to ? 'card-hover cursor-pointer' : ''}
          ${urgent && value > 0 ? 'ring-2 ring-rose-400/30 border-rose-200' : ''}`}>
        <div className={`p-2.5 rounded-xl ${color}`}>
          <Icon size={20} className="text-white"/>
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-xs text-ink-400 font-medium">{label}</p>
          <p className={`text-3xl font-display font-semibold mt-1 ${urgent && value > 0 ? 'text-rose-600' : 'text-ink-900'}`}>
            {value}
          </p>
        </div>
      </Wrap>
    </motion.div>
  )
}

export default function DashboardPage() {
  const { user } = useAuthStore()
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard'],
    queryFn: () => vanBanAPI.layDashboard().then(r => r.data),
    refetchInterval: 60_000,
  })

  if (isLoading) return (
    <AppLayout title="Tổng quan">
      <div className="flex justify-center py-20"><Spinner size="lg"/></div>
    </AppLayout>
  )

  const db = data as Dashboard
  const role = user?.role

  const getStats = () => {
    if (!db || !role) return [] as Array<{ icon: React.ElementType; label: string; value: number; color: string; to: string; urgent?: boolean }>
    const stats: Array<{ icon: React.ElementType; label: string; value: number; color: string; to: string; urgent?: boolean }> = [
      { icon: FileText, label: 'Văn bản của tôi', value: db.vanBanCuaToi, color: 'bg-ink-700', to: '/van-ban' },
      { icon: Send, label: 'Đang xử lý', value: db.vanBanChoXuLy, color: 'bg-amber-500', to: '/van-ban?trangThai=PendingDepartment' },
    ]
    if (db.vanBanBiTuChoi > 0)
      stats.push({ icon: AlertTriangle, label: 'Bị từ chối – cần sửa', value: db.vanBanBiTuChoi, color: 'bg-rose-500', to: '/van-ban?trangThai=Draft', urgent: true })
    if (canApproveStep2(role) && db.choTuXacMinh > 0)
      stats.push({ icon: Shield, label: 'Chờ tôi xác minh', value: db.choTuXacMinh, color: 'bg-jade-600', to: '/workflow/xac-minh', urgent: true })
    if (canApproveStep3(role) && db.choTuPheDuyet > 0)
      stats.push({ icon: CheckCircle, label: 'Chờ tôi phê duyệt', value: db.choTuPheDuyet, color: 'bg-jade-600', to: '/workflow/phe-duyet', urgent: true })
    if (canIssueNumber(role) && db.choCapSoHieu > 0)
      stats.push({ icon: TrendingUp, label: 'Chờ cấp số hiệu', value: db.choCapSoHieu, color: 'bg-sky-500', to: '/so-hieu', urgent: true })
    if (db.vanBanDuocPhanPhoiChuaDoc > 0)
      stats.push({ icon: Inbox, label: 'Văn bản chưa đọc', value: db.vanBanDuocPhanPhoiChuaDoc, color: 'bg-sky-500', to: '/phan-phoi/hop-thu-den?chuaDocThoi=true', urgent: true })
    if (db.quaHan > 0)
      stats.push({ icon: Clock, label: 'Quá hạn xử lý', value: db.quaHan, color: 'bg-rose-600', to: '/thong-ke/qua-han', urgent: true })
    stats.push({ icon: Bell, label: 'Thông báo chưa đọc', value: db.thongBaoChuaDoc, color: 'bg-ink-500', to: '/thong-bao' })
    return stats
  }

  return (
    <AppLayout title="Tổng quan">
      <div className="space-y-6">
        {/* Greeting */}
        <div>
          <h1 className="page-title">Xin chào, {user?.hoTen?.split(' ').pop()} 👋</h1>
          <p className="page-subtitle">{user?.roleTenHienThi} • {user?.tenBoMon ?? 'Khoa Kỹ thuật Máy tính'}</p>
        </div>

        {/* Stats grid */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
          {getStats().map((s, i) => (
            <motion.div key={s.label} initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.05 }}>
              <StatCard {...s}/>
            </motion.div>
          ))}
        </div>

        {/* Two column */}
        <div className="grid lg:grid-cols-5 gap-6">
          {/* Recent documents */}
          <div className="lg:col-span-3 card">
            <div className="px-5 py-4 border-b border-ink-100 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-ink-900">Văn bản gần đây</h2>
              <Link to="/van-ban" className="text-xs text-jade-600 hover:text-jade-700 font-medium flex items-center gap-1">
                Xem tất cả <ArrowRight size={13}/>
              </Link>
            </div>
            <div className="divide-y divide-ink-50">
              {db?.vanBanGanDay?.length === 0 && (
                <div className="py-10 text-center text-ink-400 text-sm">Chưa có văn bản nào</div>
              )}
              {db?.vanBanGanDay?.map(vb => (
                <Link key={vb.id} to={`/van-ban/${vb.id}`}
                  className="flex items-start gap-3 px-5 py-3.5 hover:bg-ink-50/60 transition-colors group">
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-ink-900 truncate group-hover:text-jade-600 transition-colors">
                      {vb.tieuDe}
                    </p>
                    <div className="flex items-center gap-2 mt-1 flex-wrap">
                      <StatusBadge status={vb.trangThai}/>
                      <span className="text-xs text-ink-400">{vb.loaiVanBanHienThi}</span>
                      <span className="text-xs text-ink-300">•</span>
                      <span className="text-xs text-ink-400">{fmtRelative(vb.ngayCapNhat ?? vb.ngayTao)}</span>
                    </div>
                  </div>
                  {vb.quaHan && (
                    <span className="badge bg-rose-50 text-rose-500 border border-rose-200 text-xs flex-shrink-0">
                      Quá hạn
                    </span>
                  )}
                </Link>
              ))}
            </div>
          </div>

          {/* Recent notifications */}
          <div className="lg:col-span-2 card">
            <div className="px-5 py-4 border-b border-ink-100 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-ink-900">Thông báo</h2>
              <Link to="/thong-bao" className="text-xs text-jade-600 hover:text-jade-700 font-medium flex items-center gap-1">
                Tất cả <ArrowRight size={13}/>
              </Link>
            </div>
            <div className="divide-y divide-ink-50">
              {db?.thongBaoGanDay?.length === 0 && (
                <div className="py-10 text-center text-ink-400 text-sm">Không có thông báo</div>
              )}
              {db?.thongBaoGanDay?.map(n => (
                <div key={n.id}
                  className={`px-5 py-3.5 ${!n.daDoc ? 'bg-jade-50/30' : ''}`}>
                  <div className="flex items-start gap-2">
                    {!n.daDoc && <div className="w-1.5 h-1.5 rounded-full bg-jade-500 mt-1.5 flex-shrink-0"/>}
                    <div className="flex-1 min-w-0">
                      <p className="text-xs font-semibold text-ink-800 truncate">{n.tieuDe}</p>
                      <p className="text-xs text-ink-500 mt-0.5 line-clamp-2">{n.noiDung}</p>
                      <p className="text-xs text-ink-300 mt-1">{fmtRelative(n.ngayTao)}</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </AppLayout>
  )
}
