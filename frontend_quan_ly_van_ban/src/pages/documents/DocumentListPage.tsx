// ═══════════════════════════════════════════════════════════════════
// DocumentListPage.tsx
// ═══════════════════════════════════════════════════════════════════
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Search, Plus, Clock } from 'lucide-react'
import { motion } from 'framer-motion'
import { vanBanAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { StatusBadge, Spinner, Pagination, EmptyState } from '@/components/custom-ui'
import { fmtDate, DOC_TYPE_OPTIONS } from '@/utils'
import { useAuthStore } from '@/stores/authStore'
import type { KetQuaPhanTrang, VanBanTomTat } from '@/types'

const STATUS_OPTIONS = [
  { value: '', label: 'Tất cả trạng thái' },
  { value: 'Draft', label: 'Bản nháp' },
  { value: 'PendingDepartment', label: 'Chờ BM xác minh' },
  { value: 'PendingFaculty', label: 'Chờ Lãnh đạo duyệt' },
  { value: 'Approved', label: 'Đã phê duyệt' },
  { value: 'Issued', label: 'Đã ban hành' },
  { value: 'Distributed', label: 'Đã phân phối' },
]

export default function DocumentListPage() {
  const { user } = useAuthStore()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [type, setType] = useState('')
  const [trang, setTrang] = useState(1)
  

  const { data, isLoading } = useQuery<KetQuaPhanTrang<VanBanTomTat>>({
    queryKey: ['van-ban', search, status, type, trang],
    queryFn: () => vanBanAPI.timKiem({
      tuKhoa: search || undefined,
      trangThai: status || undefined,
      loaiVanBan: type || undefined,
      trang, kichThuocTrang: 15,
    }).then(r => r.data),
  })

  return (
    <AppLayout title="Văn bản">
      <div className="space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <div>
            <h1 className="page-title">Văn bản</h1>
            <p className="page-subtitle">
              {data ? `${data.tongSoBanGhi} văn bản` : 'Đang tải...'}
            </p>
          </div>
          {['GiangVien', 'VanThuKhoa'].includes(user?.role ?? '') && (
            <Link to="/van-ban/tao-moi" className="btn-primary">
              <Plus size={16}/> Tạo văn bản
            </Link>
          )}
        </div>

        {/* Search + Filter bar */}
        <div className="card p-3">
          <div className="flex gap-2 flex-wrap">
            <div className="flex-1 min-w-[200px] relative">
              <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-ink-400"/>
              <input value={search} onChange={e => { setSearch(e.target.value); setTrang(1) }}
                placeholder="Tìm theo tiêu đề, số hiệu..."
                className="input pl-9 bg-ink-50 border-ink-100"/>
            </div>

            <select value={status} onChange={e => { setStatus(e.target.value); setTrang(1) }}
              className="input w-auto min-w-[180px] bg-ink-50 border-ink-100">
              {STATUS_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>

            <select value={type} onChange={e => { setType(e.target.value); setTrang(1) }}
              className="input w-auto min-w-[160px] bg-ink-50 border-ink-100">
              <option value="">Tất cả loại</option>
              {DOC_TYPE_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          {isLoading ? (
            <div className="flex justify-center py-16"><Spinner size="lg"/></div>
          ) : data?.duLieu.length === 0 ? (
            <EmptyState icon="📄" title="Không có văn bản nào"
              desc="Thử thay đổi bộ lọc hoặc tạo văn bản mới"
              action={<Link to="/van-ban/tao-moi" className="btn-primary btn-sm"><Plus size={14}/> Tạo mới</Link>}
            />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr>
                    <th className="table-th">Tiêu đề</th>
                    <th className="table-th">Loại</th>
                    <th className="table-th">Số hiệu</th>
                    <th className="table-th">Người tạo</th>
                    <th className="table-th">Trạng thái</th>
                    <th className="table-th">Ngày tạo</th>
                    <th className="table-th text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.duLieu.map((vb, i) => (
                    <motion.tr key={vb.id} className="table-row-hover"
                      initial={{ opacity: 0 }} animate={{ opacity: 1 }}
                      transition={{ delay: i * 0.02 }}>
                      <td className="table-td">
                        <div className="flex items-start gap-2">
                          {vb.quaHan && <Clock size={13} className="text-rose-500 mt-0.5 flex-shrink-0"/>}
                          <div>
                            <Link to={`/van-ban/${vb.id}`}
                              className="font-medium text-ink-900 hover:text-jade-600 transition-colors line-clamp-2 max-w-[280px]">
                              {vb.tieuDe}
                            </Link>
                            <p className="text-xs text-ink-400 mt-0.5">{vb.tenBoMon}</p>
                          </div>
                        </div>
                      </td>
                      <td className="table-td">
                        <span className="badge bg-ink-100 text-ink-600 text-xs">{vb.loaiVanBanHienThi}</span>
                      </td>
                      <td className="table-td">
                        <span className="font-mono text-xs text-ink-600">{vb.soHieu ?? '—'}</span>
                      </td>
                      <td className="table-td text-sm text-ink-600">{vb.tenNguoiTao}</td>
                      <td className="table-td"><StatusBadge status={vb.trangThai}/></td>
                      <td className="table-td text-sm text-ink-500">{fmtDate(vb.ngayTao)}</td>
                      <td className="table-td text-right">
                        <Link to={`/van-ban/${vb.id}`}
                          className="btn-ghost btn-sm inline-flex items-center gap-1">
                          Chi tiết
                        </Link>
                      </td>
                    </motion.tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {data && data.tongSoTrang > 1 && (
            <div className="px-4 py-3 border-t border-ink-100 flex items-center justify-between">
              <p className="text-xs text-ink-400">
                Hiển thị {(trang-1)*15+1}–{Math.min(trang*15, data.tongSoBanGhi)} / {data.tongSoBanGhi}
              </p>
              <Pagination trang={trang} tongSoTrang={data.tongSoTrang} onChange={setTrang}/>
            </div>
          )}
        </div>
      </div>
    </AppLayout>
  )
}
