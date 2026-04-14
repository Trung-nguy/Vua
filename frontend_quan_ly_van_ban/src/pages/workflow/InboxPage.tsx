import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { Eye, CheckSquare, Inbox, Filter } from 'lucide-react'
import { toast } from 'react-hot-toast'
import { phanPhoiAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { StatusBadge, Modal, FormField, EmptyState, Spinner, Tabs } from '@/components/custom-ui'
import { fmtDateTime, fmtDate } from '@/utils'
import type { VanBanTomTat } from '@/types'

export default function InboxPage() {
  const qc = useQueryClient()
  const [tab, setTab] = useState('all')
  const [ackModal, setAckModal] = useState<{ id: number; title: string } | null>(null)
  const [ghiChu, setGhiChu] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['hop-thu-den', tab],
    queryFn: () => phanPhoiAPI.hopThuDen({
      chuaDocThoi: tab === 'unread',
      kichThuoc: 50,
    }).then(r => r.data),
    refetchInterval: 30_000,
  })

  const ackMut = useMutation({
    mutationFn: () => phanPhoiAPI.xacNhanTiepNhan(ackModal!.id, ghiChu || undefined),
    onSuccess: () => {
      toast.success('Đã xác nhận tiếp nhận văn bản')
      setAckModal(null); setGhiChu('')
      qc.invalidateQueries({ queryKey: ['hop-thu-den'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi xác nhận'),
  })

  const items = data?.duLieu ?? []
  const unreadCount = items.filter(v => !v.isLocked).length // proxy for unread

  return (
    <AppLayout title="Hộp thư đến">
      <div className="space-y-5">
        <div>
          <h1 className="page-title">Hộp thư đến</h1>
          <p className="page-subtitle">Văn bản được phân phối đến bạn</p>
        </div>

        <Tabs
          tabs={[
            { key: 'all',    label: 'Tất cả',      count: data?.tongSoBanGhi },
            { key: 'unread', label: 'Chưa đọc' },
          ]}
          active={tab}
          onChange={setTab}
        />

        {isLoading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : items.length === 0 ? (
          <EmptyState icon="📭" title="Hộp thư trống"
            desc={tab === 'unread' ? 'Bạn đã đọc tất cả văn bản' : 'Chưa có văn bản nào được gửi đến bạn'} />
        ) : (
          <div className="space-y-3">
            {items.map((vb, i) => (
              <motion.div key={vb.id} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.03 }}
                className="card p-5 flex flex-col sm:flex-row sm:items-center gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1 flex-wrap">
                    <StatusBadge status={vb.trangThai} />
                    <span className="badge bg-ink-100 text-ink-600">{vb.loaiVanBanHienThi}</span>
                    {vb.soHieu && (
                      <span className="font-mono text-xs font-bold text-jade-600">{vb.soHieu}</span>
                    )}
                  </div>
                  <Link to={`/van-ban/${vb.id}`}
                    className="text-sm font-semibold text-ink-900 hover:text-jade-600 transition-colors line-clamp-1">
                    {vb.tieuDe}
                  </Link>
                  <div className="flex items-center gap-3 mt-1.5 text-xs text-ink-400 flex-wrap">
                    <span>👤 {vb.tenNguoiTao}</span>
                    {vb.tenBoMon && <span>🏢 {vb.tenBoMon}</span>}
                    <span>📅 {fmtDate(vb.ngayTao)}</span>
                  </div>
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                  <Link to={`/van-ban/${vb.id}`} className="btn-ghost btn-sm">
                    <Eye size={13} /> Xem
                  </Link>
                  <button onClick={() => {
                    setAckModal({ id: vb.id, title: vb.tieuDe })
                    setGhiChu('')
                  }} className="btn-secondary btn-sm">
                    <CheckSquare size={13} /> Xác nhận tiếp nhận
                  </button>
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </div>

      {/* Acknowledge modal */}
      <Modal open={!!ackModal} onClose={() => setAckModal(null)}
        title="Xác nhận tiếp nhận văn bản" size="sm"
        footer={<>
          <button onClick={() => setAckModal(null)} className="btn-secondary">Huỷ</button>
          <button onClick={() => ackMut.mutate()} disabled={ackMut.isPending} className="btn-primary">
            {ackMut.isPending ? <Spinner size="sm" /> : <><CheckSquare size={15} /> Xác nhận</>}
          </button>
        </>}>
        <div className="space-y-4">
          <p className="text-sm font-medium text-ink-800 bg-ink-50 rounded-xl p-3 line-clamp-2">
            📄 {ackModal?.title}
          </p>
          <p className="text-sm text-ink-500">
            Bấm xác nhận có nghĩa bạn đã đọc và tiếp nhận trách nhiệm liên quan đến văn bản này.
          </p>
          <FormField label="Ghi chú (tuỳ chọn)">
            <textarea value={ghiChu} onChange={e => setGhiChu(e.target.value)} rows={3}
              className="input resize-none" placeholder="Ghi chú thêm nếu cần..." />
          </FormField>
        </div>
      </Modal>
    </AppLayout>
  )
}
