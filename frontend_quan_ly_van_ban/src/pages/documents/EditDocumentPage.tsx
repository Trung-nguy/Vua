import { useState, useRef } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { useQuery, useMutation } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'react-hot-toast'
import { Upload, X, ArrowLeft, AlertTriangle, History } from 'lucide-react'
import { vanBanAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { FormField, Alert, Spinner } from '@/components/custom-ui'
import { fileIcon, fmtFileSize, fmtDateTime } from '@/utils'

const schema = z.object({
  tieuDe:          z.string().min(5, 'Tiêu đề ít nhất 5 ký tự').max(500),
  moTa:            z.string().max(2000).optional(),
  ghiChuChinhSua:  z.string().min(5, 'Ghi rõ đã sửa gì (ít nhất 5 ký tự)').max(500),
})
type FormData = z.infer<typeof schema>

export default function EditDocumentPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [file, setFile] = useState<File | null>(null)
  const [dragOver, setDragOver] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  const vbId = parseInt(id!)

  const { data: vb, isLoading } = useQuery({
    queryKey: ['van-ban', vbId],
    queryFn: () => vanBanAPI.layChiTiet(vbId).then(r => r.data),
  })

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    values: vb ? { tieuDe: vb.tieuDe, moTa: vb.moTa ?? '', ghiChuChinhSua: '' } : undefined,
  })

  const { mutate, isPending } = useMutation({
    mutationFn: (fd: FormData) => {
      const form = new FormData()
      form.append('tieuDe', fd.tieuDe)
      if (fd.moTa) form.append('moTa', fd.moTa)
      form.append('ghiChuChinhSua', fd.ghiChuChinhSua)
      if (file) form.append('file', file)
      return vanBanAPI.chinhSua(vbId, form)
    },
    onSuccess: () => {
      toast.success('Đã lưu phiên bản mới!')
      navigate(`/van-ban/${vbId}`)
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi chỉnh sửa'),
  })

  // Find last rejection reason
  const lastReject = vb?.lichSuXuLy
    ?.filter(l => l.hanhDong.startsWith('TuChoi'))
    .at(-1)

  if (isLoading) return (
    <AppLayout title="Chỉnh sửa văn bản">
      <div className="flex justify-center py-20"><Spinner size="lg" /></div>
    </AppLayout>
  )

  if (!vb) return (
    <AppLayout>
      <div className="text-center py-20 text-ink-400">Không tìm thấy văn bản</div>
    </AppLayout>
  )

  if (vb.trangThai !== 'Draft') return (
    <AppLayout title="Chỉnh sửa văn bản">
      <div className="max-w-lg mx-auto mt-12">
        <Alert type="error" title="Không thể chỉnh sửa">
          Văn bản đang ở trạng thái "{vb.trangThaiHienThi}". Chỉ chỉnh sửa được khi ở trạng thái Bản nháp.
        </Alert>
        <Link to={`/van-ban/${vbId}`} className="btn-secondary mt-4 inline-flex">
          ← Quay lại
        </Link>
      </div>
    </AppLayout>
  )

  const nextVersion = (vb.phienBans?.length ?? 0) + 1

  return (
    <AppLayout title="Chỉnh sửa văn bản">
      <div className="max-w-2xl mx-auto space-y-5">
        <div className="flex items-center gap-3">
          <button onClick={() => navigate(-1)} className="btn-ghost btn-icon">
            <ArrowLeft size={17} />
          </button>
          <div>
            <h1 className="page-title">Chỉnh sửa văn bản</h1>
            <p className="page-subtitle">Tải lên phiên bản mới v{nextVersion} sau khi chỉnh sửa theo yêu cầu</p>
          </div>
        </div>

        {/* Show rejection reason */}
        {lastReject && (
          <Alert type="warning" title={`Lý do từ chối (${lastReject.roleNguoiThucHien})`}>
            <p className="mt-1">"{lastReject.ghiChu}"</p>
            <p className="text-xs mt-2 text-ink-400">{lastReject.tenNguoiThucHien} • {fmtDateTime(lastReject.thoiGian)}</p>
          </Alert>
        )}

        <form onSubmit={handleSubmit(d => mutate(d))} className="space-y-5">
          <div className="card p-6 space-y-5">
            <h2 className="text-sm font-semibold text-ink-700 border-b border-ink-100 pb-3">
              Thông tin văn bản
            </h2>

            <FormField label="Tiêu đề" required error={errors.tieuDe?.message}>
              <input {...register('tieuDe')} className={`input ${errors.tieuDe ? 'input-error' : ''}`} />
            </FormField>

            <FormField label="Mô tả">
              <textarea {...register('moTa')} rows={3} className="input resize-none" />
            </FormField>

            <FormField label="Nhật ký chỉnh sửa" required error={errors.ghiChuChinhSua?.message}
              hint="Ghi rõ đã thay đổi gì so với phiên bản trước (bắt buộc)">
              <textarea {...register('ghiChuChinhSua')} rows={3} className={`input resize-none ${errors.ghiChuChinhSua ? 'input-error' : ''}`}
                placeholder={`v${nextVersion}: Đã bổ sung bảng giá theo yêu cầu Trưởng BM, chỉnh sửa mục 2.3...`} />
            </FormField>
          </div>

          {/* File upload */}
          <div className="card p-6">
            <div className="flex items-center justify-between mb-4 border-b border-ink-100 pb-3">
              <h2 className="text-sm font-semibold text-ink-700">
                File phiên bản mới <span className="text-rose-500">*</span>
                <span className="ml-2 badge bg-amber-50 text-amber-600">v{nextVersion}</span>
              </h2>
            </div>

            {/* Previous versions */}
            {vb.phienBans.length > 0 && (
              <div className="mb-4 p-3 rounded-xl bg-ink-50 border border-ink-100">
                <p className="text-xs font-semibold text-ink-500 mb-2 flex items-center gap-1">
                  <History size={12} /> Phiên bản cũ (không bị xóa)
                </p>
                {vb.phienBans.map(pv => (
                  <div key={pv.id} className="flex items-center gap-2 text-xs text-ink-500 py-1">
                    <span>{fileIcon(pv.contentType)}</span>
                    <span className="truncate">{pv.tenFile}</span>
                    <span className="text-ink-300">v{pv.soPhienBan}</span>
                    <span className="text-ink-300">•</span>
                    <span>{fmtFileSize(pv.kichThuocBytes)}</span>
                  </div>
                ))}
              </div>
            )}

            {file ? (
              <div className="flex items-center gap-3 p-4 rounded-xl bg-jade-50 border border-jade-200">
                <span className="text-3xl">{fileIcon(file.type)}</span>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-ink-900 truncate">{file.name}</p>
                  <p className="text-xs text-ink-500">{fmtFileSize(file.size)} • v{nextVersion}</p>
                </div>
                <button type="button" onClick={() => setFile(null)} className="btn-ghost btn-icon text-ink-400">
                  <X size={15} />
                </button>
              </div>
            ) : (
              <div
                onDrop={e => { e.preventDefault(); setDragOver(false); const f = e.dataTransfer.files[0]; if (f) setFile(f) }}
                onDragOver={e => { e.preventDefault(); setDragOver(true) }}
                onDragLeave={() => setDragOver(false)}
                onClick={() => fileRef.current?.click()}
                className={`border-2 border-dashed rounded-xl p-10 text-center cursor-pointer transition-all duration-200
                  ${dragOver ? 'border-jade-400 bg-jade-50' : 'border-ink-200 hover:border-jade-300 hover:bg-ink-50'}`}
              >
                <Upload size={28} className="mx-auto text-ink-300 mb-3" />
                <p className="text-sm font-medium text-ink-600">Kéo thả hoặc click để chọn file mới</p>
                <p className="text-xs text-ink-400 mt-1">PDF, DOCX, XLSX – Tối đa 10MB</p>
                <input ref={fileRef} type="file" className="hidden"
                  accept=".pdf,.docx,.doc,.xlsx,.jpg,.jpeg,.png"
                  onChange={e => e.target.files?.[0] && setFile(e.target.files[0])} />
              </div>
            )}
          </div>

          <div className="flex gap-3 justify-end">
            <button type="button" onClick={() => navigate(-1)} className="btn-secondary">Huỷ</button>
            <button type="submit" disabled={isPending || !file} className="btn-primary btn-lg">
              {isPending ? <><Spinner size="sm" /> Đang lưu...</> : `Lưu phiên bản v${nextVersion}`}
            </button>
          </div>
        </form>
      </div>
    </AppLayout>
  )
}
