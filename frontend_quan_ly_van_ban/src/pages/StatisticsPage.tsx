import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart, Bar, PieChart, Pie, Cell, LineChart, Line,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import { AlertTriangle, TrendingUp } from 'lucide-react'
import { Link } from 'react-router-dom'
import { thongKeAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { Spinner, EmptyState } from '@/components/custom-ui'
import { fmtDate } from '@/utils'

const COLORS = ['#1ea06a', '#0ea5e9', '#f59e0b', '#f43f5e', '#8b5cf6', '#06b6d4']

// ─── Stat box ─────────────────────────────────────────────────────
function StatBox({ label, value, color = 'text-ink-900', sub }: {
  label: string; value: number; color?: string; sub?: string
}) {
  return (
    <div className="card p-4 text-center">
      <p className={`text-3xl font-display font-semibold ${color}`}>{value}</p>
      <p className="text-xs text-ink-500 mt-1 font-medium">{label}</p>
      {sub && <p className="text-xs text-ink-400 mt-0.5">{sub}</p>}
    </div>
  )
}

export default function StatisticsPage() {
  const [tuNgay, setTuNgay] = useState(() => {
    const d = new Date(); d.setMonth(0); d.setDate(1)
    return d.toISOString().split('T')[0]
  })
  const [denNgay, setDenNgay] = useState(() => new Date().toISOString().split('T')[0])

  const { data, isLoading } = useQuery({
    queryKey: ['thong-ke', tuNgay, denNgay],
    queryFn: () => thongKeAPI.lay({ tuNgay, denNgay }).then(r => r.data),
  })

  const { data: overdue } = useQuery({
    queryKey: ['van-ban-qua-han'],
    queryFn: () => thongKeAPI.layQuaHan().then(r => r.data),
  })

  // Prepare chart data
  const typeData = Object.entries(data?.theoLoaiVanBan ?? {}).map(([name, value]) => ({ name, value }))
  const deptData = Object.entries(data?.theoBoMon ?? {}).map(([name, value]) => ({ name, value }))
  const monthData = Object.entries(data?.theoThang ?? {})
    .map(([name, value]) => ({ name: name.replace('2025-', ''), value }))
    .sort((a, b) => a.name.localeCompare(b.name))

  const statusData = data ? [
    { name: 'Bản nháp',         value: data.banNhap,            color: '#9ca3af' },
    { name: 'Chờ BM duyệt',     value: data.choTruongBMDuyet,   color: '#f59e0b' },
    { name: 'Chờ LĐ duyệt',     value: data.choLanhDaoDuyet,    color: '#f97316' },
    { name: 'Đã phê duyệt',      value: data.daDuyet,            color: '#1ea06a' },
    { name: 'Đã ban hành',       value: data.daBanHanh,          color: '#0ea5e9' },
    { name: 'Đã phân phối',      value: data.daPhanPhoi,         color: '#06b6d4' },
  ].filter(d => d.value > 0) : []

  return (
    <AppLayout title="Thống kê">
      <div className="space-y-6">
        <div>
          <h1 className="page-title">Tổng hợp & Báo cáo</h1>
          <p className="page-subtitle">Bước 7 – Thống kê văn bản theo thời gian và bộ môn</p>
        </div>

        {/* Date range filter */}
        <div className="card p-4 flex items-center gap-4 flex-wrap">
          <div className="flex items-center gap-2">
            <label className="text-sm font-medium text-ink-600 whitespace-nowrap">Từ ngày</label>
            <input type="date" value={tuNgay} onChange={e => setTuNgay(e.target.value)}
              className="input w-40" />
          </div>
          <div className="flex items-center gap-2">
            <label className="text-sm font-medium text-ink-600 whitespace-nowrap">Đến ngày</label>
            <input type="date" value={denNgay} onChange={e => setDenNgay(e.target.value)}
              className="input w-40" />
          </div>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : (
          <>
            {/* Summary stats */}
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
              <StatBox label="Tổng văn bản" value={data?.tongVanBan ?? 0} />
              <StatBox label="Chờ xử lý"   value={(data?.choTruongBMDuyet ?? 0) + (data?.choLanhDaoDuyet ?? 0)} color="text-amber-600" />
              <StatBox label="Đã phê duyệt" value={data?.daDuyet ?? 0}   color="text-jade-600" />
              <StatBox label="Đã ban hành"  value={data?.daBanHanh ?? 0} color="text-sky-600" />
              <StatBox label="Đã phân phối" value={data?.daPhanPhoi ?? 0} color="text-jade-700" />
              <StatBox label="Quá hạn"      value={data?.quaHan ?? 0}    color="text-rose-600" />
            </div>

            {/* Charts row 1 */}
            <div className="grid lg:grid-cols-2 gap-5">
              {/* Theo tháng */}
              <div className="card p-5">
                <h3 className="text-sm font-semibold text-ink-700 mb-4 flex items-center gap-2">
                  <TrendingUp size={15} className="text-jade-500" /> Xu hướng theo tháng
                </h3>
                {monthData.length > 0 ? (
                  <ResponsiveContainer width="100%" height={220}>
                    <LineChart data={monthData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#f0f1f5" />
                      <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                      <YAxis tick={{ fontSize: 11 }} />
                      <Tooltip contentStyle={{ borderRadius: '10px', fontSize: '12px' }} />
                      <Line type="monotone" dataKey="value" stroke="#1ea06a"
                        strokeWidth={2.5} dot={{ r: 4, fill: '#1ea06a' }} name="Văn bản" />
                    </LineChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="h-[220px] flex items-center justify-center text-ink-300 text-sm">
                    Chưa có dữ liệu
                  </div>
                )}
              </div>

              {/* Theo trạng thái */}
              <div className="card p-5">
                <h3 className="text-sm font-semibold text-ink-700 mb-4">Theo trạng thái</h3>
                {statusData.length > 0 ? (
                  <ResponsiveContainer width="100%" height={220}>
                    <PieChart>
                      <Pie data={statusData} cx="50%" cy="50%" outerRadius={80}
                        dataKey="value" nameKey="name" label={({ name, percent }) =>
                          `${name} ${(percent * 100).toFixed(0)}%`}
                        labelLine={{ stroke: '#c2c6d8' }}
                        fontSize={10}>
                        {statusData.map((entry, i) => (
                          <Cell key={i} fill={entry.color} />
                        ))}
                      </Pie>
                      <Tooltip contentStyle={{ borderRadius: '10px', fontSize: '12px' }} />
                    </PieChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="h-[220px] flex items-center justify-center text-ink-300 text-sm">
                    Chưa có dữ liệu
                  </div>
                )}
              </div>
            </div>

            {/* Charts row 2 */}
            <div className="grid lg:grid-cols-2 gap-5">
              {/* Theo loại văn bản */}
              {typeData.length > 0 && (
                <div className="card p-5">
                  <h3 className="text-sm font-semibold text-ink-700 mb-4">Theo loại văn bản</h3>
                  <ResponsiveContainer width="100%" height={200}>
                    <BarChart data={typeData} layout="vertical">
                      <CartesianGrid strokeDasharray="3 3" stroke="#f0f1f5" horizontal={false} />
                      <XAxis type="number" tick={{ fontSize: 11 }} />
                      <YAxis type="category" dataKey="name" tick={{ fontSize: 11 }} width={90} />
                      <Tooltip contentStyle={{ borderRadius: '10px', fontSize: '12px' }} />
                      <Bar dataKey="value" name="Số lượng" radius={[0, 6, 6, 0]}>
                        {typeData.map((_, i) => (
                          <Cell key={i} fill={COLORS[i % COLORS.length]} />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              )}

              {/* Theo bộ môn */}
              {deptData.length > 0 && (
                <div className="card p-5">
                  <h3 className="text-sm font-semibold text-ink-700 mb-4">Theo bộ môn</h3>
                  <ResponsiveContainer width="100%" height={200}>
                    <BarChart data={deptData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#f0f1f5" />
                      <XAxis dataKey="name" tick={{ fontSize: 10 }} />
                      <YAxis tick={{ fontSize: 11 }} />
                      <Tooltip contentStyle={{ borderRadius: '10px', fontSize: '12px' }} />
                      <Bar dataKey="value" name="Số văn bản" fill="#1ea06a" radius={[6, 6, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              )}
            </div>

            {/* Overdue list */}
            {(overdue?.length ?? 0) > 0 && (
              <div className="card overflow-hidden">
                <div className="px-5 py-3.5 border-b border-ink-100 flex items-center gap-2">
                  <AlertTriangle size={15} className="text-rose-500" />
                  <h3 className="text-sm font-semibold text-rose-700">
                    Văn bản quá hạn ({overdue!.length})
                  </h3>
                </div>
                <div className="divide-y divide-ink-50">
                  {overdue!.slice(0, 10).map(vb => (
                    <div key={vb.id} className="flex items-center gap-4 px-5 py-3.5">
                      <div className="flex-1 min-w-0">
                        <Link to={`/van-ban/${vb.id}`}
                          className="text-sm font-medium text-ink-900 hover:text-jade-600 transition-colors truncate block">
                          {vb.tieuDe}
                        </Link>
                        <p className="text-xs text-ink-400 mt-0.5">
                          {vb.trangThaiHienThi} • {vb.tenNguoiTao}
                        </p>
                      </div>
                      <span className="text-xs font-semibold text-rose-600 whitespace-nowrap">
                        Hạn: {fmtDate(vb.hanXuLy)}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </AppLayout>
  )
}
