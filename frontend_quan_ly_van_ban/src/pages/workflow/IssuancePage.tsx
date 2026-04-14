import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { Hash, ExternalLink, Eye, CheckCircle } from 'lucide-react'
import { toast } from 'react-hot-toast'
import { vanBanAPI, soHieuAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { Modal, FormField, Alert, EmptyState, Spinner } from '@/components/custom-ui'
import { fmtDate, fmtDateTime } from '@/utils'

export default function IssuancePage() {
  const qc = useQueryClient()
  const [modal, setModal] = useState<{ id: number; title: string; loai: string } | null>(null)
  const [previewNum, setPreviewNum] = useState<string | null>(null)
  const [form, setForm] = useState({ nguoiKy: '', chucVuKy: '', ngayHieuLuc: '', soHieuTuyChinh: '' })

  const { data, isLoading } = useQuery({
    queryKey: ['van-ban-cho-cap-so'],
    queryFn: () => vanBanAPI.timKiem({
      trangThai: 'Approved',
      kichThuocTrang: 50,
    }).then(r => ({
      ...r.data,
      duLieu: r.data.duLieu.filter(v => v.canCapSoHieu),
    })),
    refetchInterval: 30_000,
  })

  const { mutate, isPending } = useMutation({
    mutationFn: () => soHieuAPI.capSo({
      vanBanId: modal!.id,
      nguoiKy: form.nguoiKy || undefined,
      chucVuKy: form.chucVuKy || undefined,
      ngayHieuLuc: form.ngayHieuLuc || undefined,
      soHieuTuyChinh: form.soHieuTuyChinh || undefined,
    }),
    onSuccess: (res) => {
      toast.success(`Đã cấp số hiệu: ${res.data.soHieu}`)
      setModal(null)
      qc.invalidateQueries({ queryKey: ['van-ban-cho-cap-so'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi cấp số hiệu'),
  })

  const handlePreview = async (id: number) => {
    try {
      const r = await soHieuAPI.xemTruoc(id)
      setPreviewNum(r.data.soHieu)
    } catch { setPreviewNum(null) }
  }

  const pending = data?.duLieu ?? []

  return (
    <AppLayout title="Cấp số hiệu">
      <div className="space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title">Cấp số hiệu văn bản</h1>
            <p className="page-subtitle">Bước 5 – Vào sổ công văn điện tử cho văn bản đã phê duyệt</p>
          </div>
          <div className="badge bg-sky-50 text-sky-700 border border-sky-200 text-sm px-3 py-1.5">
            {pending.length} chờ cấp số
          </div>
        </div>

        <Alert type="info" title="Quy tắc cấp số hiệu">
          <div className="mt-1 text-xs space-y-0.5">
            <p>• Chỉ áp dụng cho <strong>Quyết định, Thông báo, Công văn</strong></p>
            <p>• Để trống → hệ thống tự sinh: <code className="bg-white/60 px-1 rounded">05/2025/TB-KCN</code></p>
            <p>• Số tùy chỉnh → phải đảm bảo không trùng với số đã cấp</p>
          </div>
        </Alert>

        {isLoading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : pending.length === 0 ? (
          <EmptyState icon="📋" title="Không có văn bản nào chờ cấp số"
            desc="Tất cả văn bản đã có số hiệu hoặc chưa được phê duyệt." />
        ) : (
          <div className="space-y-3">
            {pending.map((vb, i) => (
              <motion.div key={vb.id} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.04 }}
                className="card p-5 flex flex-col sm:flex-row sm:items-center gap-4">
                <div className="w-10 h-10 rounded-xl bg-sky-50 border border-sky-200 flex items-center
                                justify-center flex-shrink-0">
                  <Hash size={18} className="text-sky-600" />
                </div>
                <div className="flex-1 min-w-0">
                  <Link to={`/van-ban/${vb.id}`}
                    className="text-sm font-semibold text-ink-900 hover:text-jade-600 transition-colors line-clamp-1">
                    {vb.tieuDe}
                  </Link>
                  <div className="flex items-center gap-3 mt-1 text-xs text-ink-400 flex-wrap">
                    <span>{vb.loaiVanBanHienThi}</span>
                    <span>•</span>
                    <span>👤 {vb.tenNguoiTao}</span>
                    <span>•</span>
                    <span>📅 {fmtDate(vb.ngayTao)}</span>
                  </div>
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                  <Link to={`/van-ban/${vb.id}`} className="btn-ghost btn-sm">
                    <ExternalLink size={13} /> Xem
                  </Link>
                  <button onClick={() => {
                    setModal({ id: vb.id, title: vb.tieuDe, loai: vb.loaiVanBanHienThi })
                    setForm({ nguoiKy: '', chucVuKy: '', ngayHieuLuc: '', soHieuTuyChinh: '' })
                    setPreviewNum(null)
                    handlePreview(vb.id)
                  }} className="btn-primary btn-sm">
                    <Hash size={13} /> Cấp số
                  </button>
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </div>

      {/* Issuance modal */}
      <Modal open={!!modal} onClose={() => setModal(null)} title="Cấp số hiệu chính thức" size="md"
        footer={<>
          <button onClick={() => setModal(null)} className="btn-secondary">Huỷ</button>
          <button onClick={() => mutate()} disabled={isPending} className="btn-primary">
            {isPending ? <Spinner size="sm" /> : <><CheckCircle size={15} /> Xác nhận cấp số</>}
          </button>
        </>}>
        <div className="space-y-4">
          <div className="p-4 rounded-xl bg-sky-50 border border-sky-200">
            <p className="text-xs text-sky-600 font-semibold uppercase tracking-wider mb-1">
              {modal?.loai}
            </p>
            <p className="text-sm font-medium text-ink-800 line-clamp-2">{modal?.title}</p>
            {previewNum && (
              <div className="mt-3 flex items-center gap-2">
                <Hash size={14} className="text-sky-500" />
                <span className="text-base font-mono font-bold text-sky-700">{previewNum}</span>
                <span className="text-xs text-sky-500">(số tự sinh)</span>
              </div>
            )}
          </div>

          <FormField label="Số hiệu tùy chỉnh" hint="Để trống → tự sinh theo mẫu chuẩn">
            <input value={form.soHieuTuyChinh}
              onChange={e => setForm(p => ({ ...p, soHieuTuyChinh: e.target.value }))}
              placeholder="VD: 03/2025/QĐ-KCN (không bắt buộc)" className="input font-mono" />
          </FormField>

          <div className="grid sm:grid-cols-2 gap-4">
            <FormField label="Người ký">
              <input value={form.nguoiKy}
                onChange={e => setForm(p => ({ ...p, nguoiKy: e.target.value }))}
                placeholder="PGS.TS. Nguyễn Văn Khoa" className="input" />
            </FormField>
            <FormField label="Chức vụ người ký">
              <input value={form.chucVuKy}
                onChange={e => setForm(p => ({ ...p, chucVuKy: e.target.value }))}
                placeholder="Trưởng Khoa" className="input" />
            </FormField>
          </div>

          <FormField label="Ngày có hiệu lực">
            <input type="date" value={form.ngayHieuLuc}
              onChange={e => setForm(p => ({ ...p, ngayHieuLuc: e.target.value }))} className="input" />
          </FormField>
        </div>
      </Modal>
    </AppLayout>
  )
}
