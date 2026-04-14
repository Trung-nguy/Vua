import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { Send, Users, CheckSquare, Eye, Search } from 'lucide-react'
import { toast } from 'react-hot-toast'
import { vanBanAPI, phanPhoiAPI, nguoiDungAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { StatusBadge, Modal, FormField, Alert, EmptyState, Spinner, Tabs } from '@/components/custom-ui'
import { fmtDate, fmtDateTime } from '@/utils'
import type { NguoiDung } from '@/types'

// ═══════════════════════════════════════════════════════════════════
// DISTRIBUTION PAGE – Phân phối văn bản
// ═══════════════════════════════════════════════════════════════════
export default function DistributionPage() {
  const qc = useQueryClient()
  const [modal, setModal] = useState<{ id: number; title: string } | null>(null)
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set())
  const [userSearch, setUserSearch] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['van-ban-cho-phan-phoi'],
    queryFn: () => vanBanAPI.timKiem({
      trangThai: 'Issued', kichThuocTrang: 50,
    }).then(r => r.data),
    refetchInterval: 30_000,
  })

  // Also show Approved (for non-issuance types)
  const { data: dataApproved } = useQuery({
    queryKey: ['van-ban-approved-no-number'],
    queryFn: () => vanBanAPI.timKiem({
      trangThai: 'Approved', kichThuocTrang: 50,
    }).then(r => ({
      ...r.data,
      duLieu: r.data.duLieu.filter(v => !v.canCapSoHieu),
    })),
  })

  const { data: allUsers } = useQuery({
    queryKey: ['users-for-distribute'],
    queryFn: () => nguoiDungAPI.layDanhSach({ kichThuoc: 200 }).then(r => r.data.duLieu),
    enabled: !!modal,
  })

  const { mutate, isPending } = useMutation({
    mutationFn: () => phanPhoiAPI.phanPhoi(modal!.id, Array.from(selectedIds)),
    onSuccess: () => {
      toast.success(`Đã phân phối đến ${selectedIds.size} người nhận`)
      setModal(null); setSelectedIds(new Set())
      qc.invalidateQueries({ queryKey: ['van-ban-cho-phan-phoi'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi phân phối'),
  })

  const allDocs = [
    ...(data?.duLieu ?? []),
    ...(dataApproved?.duLieu ?? []),
  ]

  const filteredUsers = allUsers?.filter(u =>
    u.hoTen.toLowerCase().includes(userSearch.toLowerCase()) ||
    u.email.toLowerCase().includes(userSearch.toLowerCase())
  ) ?? []

  const toggleUser = (id: number) => {
    setSelectedIds(prev => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  return (
    <AppLayout title="Phân phối văn bản">
      <div className="space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title">Phân phối văn bản</h1>
            <p className="page-subtitle">Bước 6 – Gửi văn bản đến các bên liên quan</p>
          </div>
          <div className="badge bg-jade-50 text-jade-700 border border-jade-200 text-sm px-3 py-1.5">
            {allDocs.length} sẵn sàng phân phối
          </div>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : allDocs.length === 0 ? (
          <EmptyState icon="📬" title="Không có văn bản nào cần phân phối"
            desc="Văn bản cần được phê duyệt và (nếu cần) cấp số hiệu trước." />
        ) : (
          <div className="space-y-3">
            {allDocs.map((vb, i) => (
              <motion.div key={vb.id} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.04 }}
                className="card p-5 flex flex-col sm:flex-row sm:items-center gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1 flex-wrap">
                    <StatusBadge status={vb.trangThai} />
                    <span className="badge bg-ink-100 text-ink-600">{vb.loaiVanBanHienThi}</span>
                    {vb.soHieu && (
                      <span className="font-mono text-xs font-bold text-jade-600 bg-jade-50 px-2 py-0.5 rounded-lg">
                        {vb.soHieu}
                      </span>
                    )}
                  </div>
                  <Link to={`/van-ban/${vb.id}`}
                    className="text-sm font-semibold text-ink-900 hover:text-jade-600 transition-colors line-clamp-1">
                    {vb.tieuDe}
                  </Link>
                  <div className="flex items-center gap-3 mt-1.5 text-xs text-ink-400 flex-wrap">
                    <span>👤 {vb.tenNguoiTao}</span>
                    <span>📅 {fmtDate(vb.ngayTao)}</span>
                  </div>
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                  <Link to={`/van-ban/${vb.id}`} className="btn-ghost btn-sm"><Eye size={13} /> Xem</Link>
                  <button onClick={() => {
                    setModal({ id: vb.id, title: vb.tieuDe })
                    setSelectedIds(new Set())
                    setUserSearch('')
                  }} className="btn-primary btn-sm">
                    <Send size={13} /> Phân phối
                  </button>
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </div>

      {/* Distribution modal */}
      <Modal open={!!modal} onClose={() => setModal(null)} title="Phân phối văn bản" size="lg"
        footer={<>
          <button onClick={() => setModal(null)} className="btn-secondary">Huỷ</button>
          <button onClick={() => mutate()} disabled={selectedIds.size === 0 || isPending} className="btn-primary">
            {isPending ? <Spinner size="sm" /> : <><Send size={15} /> Phân phối ({selectedIds.size})</>}
          </button>
        </>}>
        <div className="space-y-4">
          <p className="text-sm text-ink-600 bg-ink-50 rounded-xl p-3 line-clamp-2 font-medium">
            📄 {modal?.title}
          </p>

          <div className="relative">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-ink-400" />
            <input value={userSearch} onChange={e => setUserSearch(e.target.value)}
              placeholder="Tìm người nhận theo tên hoặc email..."
              className="input pl-9 bg-ink-50 border-ink-100" />
          </div>

          {selectedIds.size > 0 && (
            <p className="text-xs text-jade-600 font-medium">
              ✓ Đã chọn {selectedIds.size} người nhận
            </p>
          )}

          <div className="max-h-72 overflow-y-auto space-y-1 rounded-xl border border-ink-100 p-2">
            {filteredUsers.length === 0 ? (
              <p className="text-center py-6 text-sm text-ink-400">Không tìm thấy người dùng</p>
            ) : (
              filteredUsers.map(u => (
                <label key={u.id}
                  className={`flex items-center gap-3 p-3 rounded-xl cursor-pointer transition-colors
                    ${selectedIds.has(u.id) ? 'bg-jade-50 border border-jade-200' : 'hover:bg-ink-50'}`}>
                  <input type="checkbox" checked={selectedIds.has(u.id)}
                    onChange={() => toggleUser(u.id)} className="rounded text-jade-600 w-4 h-4" />
                  <div className="w-7 h-7 rounded-full bg-ink-200 flex items-center justify-center
                                  text-ink-600 text-xs font-bold flex-shrink-0">
                    {u.hoTen.charAt(0)}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-ink-800 truncate">{u.hoTen}</p>
                    <p className="text-xs text-ink-400 truncate">{u.roleTenHienThi} • {u.email}</p>
                  </div>
                </label>
              ))
            )}
          </div>
        </div>
      </Modal>
    </AppLayout>
  )
}
