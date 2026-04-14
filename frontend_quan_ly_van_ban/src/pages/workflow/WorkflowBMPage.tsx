import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { CheckCircle, XCircle, ExternalLink, Clock, AlertTriangle, Filter } from 'lucide-react'
import { toast } from 'react-hot-toast'
import { vanBanAPI, workflowAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { StatusBadge, Modal, FormField, Alert, EmptyState, Spinner } from '@/components/custom-ui'
import { fmtDate, fmtDateTime } from '@/utils'
import type { VanBanTomTat } from '@/types'

// ─── Shared ApprovalCard ─────────────────────────────────────────
function ApprovalCard({
  vb, onApprove, onReject, approving, rejecting, actionLabel
}: {
  vb: VanBanTomTat
  onApprove: (id: number) => void
  onReject: (id: number) => void
  approving: number | null
  rejecting: number | null
  actionLabel: string
}) {
  return (
    <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
      className="card p-5 flex flex-col sm:flex-row sm:items-center gap-4">
      <div className="flex-1 min-w-0">
        <div className="flex items-start gap-2 flex-wrap mb-1">
          <StatusBadge status={vb.trangThai} />
          <span className="badge bg-ink-100 text-ink-600">{vb.loaiVanBanHienThi}</span>
          {vb.quaHan && (
            <span className="badge badge-rejected flex items-center gap-1">
              <AlertTriangle size={10} /> Quá hạn
            </span>
          )}
        </div>
        <Link to={`/van-ban/${vb.id}`}
          className="text-sm font-semibold text-ink-900 hover:text-jade-600 transition-colors line-clamp-2">
          {vb.tieuDe}
        </Link>
        <div className="flex items-center gap-3 mt-2 flex-wrap text-xs text-ink-400">
          <span className="flex items-center gap-1"><span>👤</span>{vb.tenNguoiTao}</span>
          {vb.tenBoMon && <span className="flex items-center gap-1"><span>🏢</span>{vb.tenBoMon}</span>}
          <span className="flex items-center gap-1"><Clock size={11} />{fmtDate(vb.ngayTao)}</span>
          {vb.hanXuLy && (
            <span className={`flex items-center gap-1 ${vb.quaHan ? 'text-rose-500 font-medium' : ''}`}>
              ⏰ Hạn: {fmtDate(vb.hanXuLy)}
            </span>
          )}
        </div>
      </div>
      <div className="flex items-center gap-2 flex-shrink-0">
        <Link to={`/van-ban/${vb.id}`} className="btn-ghost btn-sm">
          <ExternalLink size={13} /> Xem
        </Link>
        <button onClick={() => onReject(vb.id)}
          disabled={rejecting === vb.id || approving === vb.id}
          className="btn-danger btn-sm">
          <XCircle size={13} /> Từ chối
        </button>
        <button onClick={() => onApprove(vb.id)}
          disabled={approving === vb.id || rejecting === vb.id}
          className="btn-primary btn-sm">
          {approving === vb.id ? <Spinner size="sm" /> : <CheckCircle size={13} />}
          {actionLabel}
        </button>
      </div>
    </motion.div>
  )
}

// ═══════════════════════════════════════════════════════════════════
// TRƯỞNG BM PAGE – Bước 2: Xác minh chuyên môn
// ═══════════════════════════════════════════════════════════════════
export function WorkflowBMPage() {
  const qc = useQueryClient()
  const [approvingId, setApprovingId] = useState<number | null>(null)
  const [rejectingId, setRejectingId] = useState<number | null>(null)
  const [rejectModal, setRejectModal] = useState<{ id: number; title: string } | null>(null)
  const [lyDo, setLyDo] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['van-ban-cho-xac-minh'],
    queryFn: () => vanBanAPI.timKiem({ trangThai: 'PendingDepartment', kichThuocTrang: 50 }).then(r => r.data),
    refetchInterval: 30_000,
  })

  const approveMut = useMutation({
    mutationFn: (id: number) => workflowAPI.xacMinhChuyenMon(id),
    onMutate: (id) => setApprovingId(id),
    onSuccess: () => { toast.success('Đã xác minh chuyên môn'); qc.invalidateQueries({ queryKey: ['van-ban-cho-xac-minh'] }) },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi xác minh'),
    onSettled: () => setApprovingId(null),
  })

  const rejectMut = useMutation({
    mutationFn: ({ id, lyDo }: { id: number; lyDo: string }) => workflowAPI.tuChoiBoMon(id, lyDo),
    onMutate: ({ id }) => setRejectingId(id),
    onSuccess: () => {
      toast.success('Đã từ chối')
      setRejectModal(null); setLyDo('')
      qc.invalidateQueries({ queryKey: ['van-ban-cho-xac-minh'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi từ chối'),
    onSettled: () => setRejectingId(null),
  })

  const pending = data?.duLieu ?? []

  return (
    <AppLayout title="Xác minh chuyên môn">
      <div className="space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title">Xác minh chuyên môn</h1>
            <p className="page-subtitle">Bước 2 – Kiểm tra nội dung văn bản thuộc phạm vi Bộ môn</p>
          </div>
          <div className="badge bg-amber-50 text-amber-700 border border-amber-200 text-sm px-3 py-1.5">
            {pending.length} chờ xử lý
          </div>
        </div>

        {/* How-to hint */}
        <Alert type="info" title="Hướng dẫn xử lý">
          <ul className="mt-1 space-y-0.5 text-xs list-disc list-inside">
            <li>Nhấn <strong>Xem</strong> để đọc toàn bộ nội dung và tải file đính kèm</li>
            <li>Nếu đạt → <strong>Xác minh đạt</strong> để chuyển lên Lãnh đạo Khoa</li>
            <li>Nếu chưa đạt → <strong>Từ chối</strong> (phải ghi rõ lý do)</li>
          </ul>
        </Alert>

        {isLoading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : pending.length === 0 ? (
          <EmptyState icon="✅" title="Không có văn bản nào chờ xác minh"
            desc="Tất cả văn bản đã được xử lý. Kiểm tra lại sau." />
        ) : (
          <div className="space-y-3">
            {pending.map(vb => (
              <ApprovalCard key={vb.id} vb={vb}
                onApprove={(id) => approveMut.mutate(id)}
                onReject={(id) => { setRejectModal({ id, title: vb.tieuDe }); setLyDo('') }}
                approving={approvingId}
                rejecting={rejectingId}
                actionLabel="Xác minh đạt"
              />
            ))}
          </div>
        )}
      </div>

      {/* Reject modal */}
      <Modal open={!!rejectModal} onClose={() => setRejectModal(null)}
        title="Từ chối xác minh chuyên môn" size="sm"
        footer={<>
          <button onClick={() => setRejectModal(null)} className="btn-secondary">Huỷ</button>
          <button onClick={() => rejectMut.mutate({ id: rejectModal!.id, lyDo })}
            disabled={!lyDo.trim() || rejectMut.isPending} className="btn-danger">
            {rejectMut.isPending ? <Spinner size="sm" /> : 'Từ chối'}
          </button>
        </>}>
        <div className="space-y-4">
          <p className="text-sm text-ink-600 bg-ink-50 rounded-xl p-3 line-clamp-2">
            📄 {rejectModal?.title}
          </p>
          <Alert type="warning" title="Lý do từ chối là bắt buộc">
            Người soạn thảo cần biết cụ thể phải chỉnh sửa gì để nộp lại.
          </Alert>
          <FormField label="Lý do từ chối" required>
            <textarea value={lyDo} onChange={e => setLyDo(e.target.value)}
              rows={4} className="input resize-none"
              placeholder="Nêu rõ: nội dung nào cần chỉnh sửa, bổ sung gì, sai sót ở đâu..." />
          </FormField>
        </div>
      </Modal>
    </AppLayout>
  )
}

// ═══════════════════════════════════════════════════════════════════
// LÃNH ĐẠO KHOA PAGE – Bước 3: Phê duyệt cuối cùng
// ═══════════════════════════════════════════════════════════════════
export function WorkflowKhoaPage() {
  const qc = useQueryClient()
  const [approvingId, setApprovingId] = useState<number | null>(null)
  const [rejectingId, setRejectingId] = useState<number | null>(null)
  const [rejectModal, setRejectModal] = useState<{ id: number; title: string } | null>(null)
  const [lyDo, setLyDo] = useState('')
  const [yKien, setYKien] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['van-ban-cho-phe-duyet'],
    queryFn: () => vanBanAPI.timKiem({ trangThai: 'PendingFaculty', kichThuocTrang: 50 }).then(r => r.data),
    refetchInterval: 30_000,
  })

  const approveMut = useMutation({
    mutationFn: ({ id, yKien }: { id: number; yKien?: string }) => workflowAPI.pheDuyetCuoi(id, yKien),
    onMutate: ({ id }) => setApprovingId(id),
    onSuccess: () => {
      toast.success('Đã phê duyệt văn bản. File đã được khoá.')
      qc.invalidateQueries({ queryKey: ['van-ban-cho-phe-duyet'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi phê duyệt'),
    onSettled: () => setApprovingId(null),
  })

  const rejectMut = useMutation({
    mutationFn: ({ id, lyDo }: { id: number; lyDo: string }) => workflowAPI.tuChoiKhoa(id, lyDo),
    onMutate: ({ id }) => setRejectingId(id),
    onSuccess: () => {
      toast.success('Đã từ chối. Nộp lại sẽ bỏ qua Bộ môn.')
      setRejectModal(null); setLyDo('')
      qc.invalidateQueries({ queryKey: ['van-ban-cho-phe-duyet'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi từ chối'),
    onSettled: () => setRejectingId(null),
  })

  const pending = data?.duLieu ?? []

  return (
    <AppLayout title="Phê duyệt văn bản">
      <div className="space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title">Phê duyệt văn bản</h1>
            <p className="page-subtitle">Bước 3 – Ký duyệt điện tử hoặc yêu cầu chỉnh sửa</p>
          </div>
          <div className="badge bg-amber-50 text-amber-700 border border-amber-200 text-sm px-3 py-1.5">
            {pending.length} chờ phê duyệt
          </div>
        </div>

        <Alert type="warning" title="Lưu ý quan trọng">
          <ul className="mt-1 space-y-0.5 text-xs list-disc list-inside">
            <li>Sau khi phê duyệt → <strong>file bị khoá</strong>, không ai được sửa nội dung nữa</li>
            <li>Nếu từ chối → văn bản về bản nháp. Khi nộp lại sẽ bỏ qua Bộ môn (thẳng lên đây)</li>
            <li>Văn bản loại <em>Quyết định, Thông báo, Công văn</em> → cần Văn thư cấp số sau khi duyệt</li>
          </ul>
        </Alert>

        {isLoading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : pending.length === 0 ? (
          <EmptyState icon="✅" title="Không có văn bản nào chờ phê duyệt"
            desc="Tất cả hồ sơ đã được xử lý." />
        ) : (
          <div className="space-y-3">
            {pending.map(vb => (
              <ApprovalCard key={vb.id} vb={vb}
                onApprove={(id) => approveMut.mutate({ id })}
                onReject={(id) => { setRejectModal({ id, title: vb.tieuDe }); setLyDo('') }}
                approving={approvingId}
                rejecting={rejectingId}
                actionLabel="Phê duyệt"
              />
            ))}
          </div>
        )}
      </div>

      <Modal open={!!rejectModal} onClose={() => setRejectModal(null)}
        title="Từ chối phê duyệt cuối" size="sm"
        footer={<>
          <button onClick={() => setRejectModal(null)} className="btn-secondary">Huỷ</button>
          <button onClick={() => rejectMut.mutate({ id: rejectModal!.id, lyDo })}
            disabled={!lyDo.trim() || rejectMut.isPending} className="btn-danger">
            {rejectMut.isPending ? <Spinner size="sm" /> : 'Từ chối'}
          </button>
        </>}>
        <div className="space-y-4">
          <p className="text-sm text-ink-600 bg-ink-50 rounded-xl p-3 line-clamp-2">📄 {rejectModal?.title}</p>
          <Alert type="warning">Nộp lại sau khi sửa sẽ bỏ qua Bộ môn, gửi thẳng lên đây.</Alert>
          <FormField label="Lý do từ chối" required>
            <textarea value={lyDo} onChange={e => setLyDo(e.target.value)} rows={4}
              className="input resize-none"
              placeholder="Nêu cụ thể yêu cầu chỉnh sửa để người soạn thảo thực hiện đúng..." />
          </FormField>
        </div>
      </Modal>
    </AppLayout>
  )
}

export default WorkflowBMPage
