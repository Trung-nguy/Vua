// ═══════════════════════════════════════════════════════════════════
// CreateDocumentPage.tsx
// ═══════════════════════════════════════════════════════════════════
import { useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { toast } from 'react-hot-toast'
import { Upload, X, FileText, Info } from 'lucide-react'
import { vanBanAPI, boMonAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { FormField, Alert, Spinner } from '@/components/custom-ui'
import { DOC_TYPE_OPTIONS, fileIcon, fmtFileSize } from '@/utils'
import { useAuthStore } from '@/stores/authStore'

const schema = z.object({
  tieuDe: z.string().min(5, 'Tiêu đề ít nhất 5 ký tự').max(500),
  moTa: z.string().max(2000).optional(),
  loaiVanBan: z.string().min(1, 'Vui lòng chọn loại văn bản'),
  hanXuLy: z.string().optional(),
})
type FormData = z.infer<typeof schema>

export default function CreateDocumentPage() {
  const navigate = useNavigate()
  const { user } = useAuthStore()
  const [file, setFile] = useState<File | null>(null)
  const [dragOver, setDragOver] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  const { register, handleSubmit, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { loaiVanBan: '0' },
  })

  const selectedType = watch('loaiVanBan')
  const needsIssuance = ['10', '11', '12'].includes(selectedType)

  const { mutate, isPending } = useMutation({
    mutationFn: (fd: FormData) => {
      const form = new FormData()
      form.append('tieuDe', fd.tieuDe)
      if (fd.moTa) form.append('moTa', fd.moTa)
      form.append('loaiVanBan', fd.loaiVanBan)
      if (fd.hanXuLy) form.append('hanXuLy', fd.hanXuLy)
      if (file) form.append('file', file)
      return vanBanAPI.tao(form)
    },
    onSuccess: (res) => {
      toast.success('Tạo văn bản thành công!')
      navigate(`/van-ban/${res.data.id}`)
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi tạo văn bản'),
  })

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragOver(false)
    const f = e.dataTransfer.files[0]
    if (f) setFile(f)
  }

  return (
    <AppLayout title="Tạo văn bản">
      <div className="max-w-2xl mx-auto">
        <div className="mb-6">
          <h1 className="page-title">Tạo văn bản mới</h1>
          <p className="page-subtitle">Soạn thảo và đề nghị phê duyệt văn bản</p>
        </div>

        <form onSubmit={handleSubmit(d => mutate(d))} className="space-y-5">
          <div className="card p-6 space-y-5">
            <h2 className="text-sm font-semibold text-ink-700 border-b border-ink-100 pb-3">
              Thông tin văn bản
            </h2>

            <FormField label="Tiêu đề / Trích yếu" required error={errors.tieuDe?.message}>
              <input {...register('tieuDe')} placeholder="VD: Tờ trình xin kinh phí mua thiết bị..."
                className={`input ${errors.tieuDe ? 'input-error' : ''}`}/>
            </FormField>

            <div className="grid sm:grid-cols-2 gap-4">
              <FormField label="Loại văn bản" required error={errors.loaiVanBan?.message}>
                <select {...register('loaiVanBan')} className={`input ${errors.loaiVanBan ? 'input-error' : ''}`}>
                  {DOC_TYPE_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </FormField>

              <FormField label="Hạn xử lý" hint="Chống 'ngâm' hồ sơ">
                <input {...register('hanXuLy')} type="datetime-local" className="input"/>
              </FormField>
            </div>

            {needsIssuance && (
              <Alert type="info" title="Loại này cần cấp số hiệu">
                Sau khi được phê duyệt, Văn thư Khoa sẽ cấp số hiệu chính thức (VD: 05/2025/TB-KCN) trước khi phân phối.
              </Alert>
            )}

            {user?.role === 'VanThuKhoa' && (
              <Alert type="success" title="Văn thư Khoa – bỏ qua Bước 2">
                Văn bản của bạn sẽ được gửi thẳng lên Lãnh đạo Khoa phê duyệt, không qua Trưởng Bộ môn.
              </Alert>
            )}

            <FormField label="Mô tả (tuỳ chọn)">
              <textarea {...register('moTa')} rows={3} className="input resize-none"
                placeholder="Nội dung tóm tắt, mục đích, đối tượng liên quan..."/>
            </FormField>
          </div>

          {/* File upload */}
          <div className="card p-6">
            <h2 className="text-sm font-semibold text-ink-700 border-b border-ink-100 pb-3 mb-4">
              File đính kèm <span className="text-rose-500">*</span>
            </h2>

            {file ? (
              <div className="flex items-center gap-3 p-4 rounded-xl bg-jade-50 border border-jade-200">
                <span className="text-3xl">{fileIcon(file.type)}</span>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-ink-900 truncate">{file.name}</p>
                  <p className="text-xs text-ink-500">{fmtFileSize(file.size)} • v1</p>
                </div>
                <button type="button" onClick={() => setFile(null)} className="btn-ghost btn-icon text-ink-400">
                  <X size={15}/>
                </button>
              </div>
            ) : (
              <div
                onDrop={handleDrop}
                onDragOver={e => { e.preventDefault(); setDragOver(true) }}
                onDragLeave={() => setDragOver(false)}
                onClick={() => fileRef.current?.click()}
                className={`border-2 border-dashed rounded-xl p-10 text-center cursor-pointer
                  transition-all duration-200
                  ${dragOver ? 'border-jade-400 bg-jade-50' : 'border-ink-200 hover:border-jade-300 hover:bg-ink-50'}`}
              >
                <Upload size={32} className="mx-auto text-ink-300 mb-3"/>
                <p className="text-sm font-medium text-ink-600">Kéo thả file hoặc click để chọn</p>
                <p className="text-xs text-ink-400 mt-1">PDF, DOCX, XLSX, JPG, PNG – Tối đa 10MB</p>
                <input ref={fileRef} type="file" className="hidden"
                  accept=".pdf,.docx,.doc,.xlsx,.jpg,.jpeg,.png"
                  onChange={e => e.target.files?.[0] && setFile(e.target.files[0])}/>
              </div>
            )}
          </div>

          <div className="flex gap-3 justify-end">
            <button type="button" onClick={() => navigate(-1)} className="btn-secondary">
              Huỷ
            </button>
            <button type="submit" disabled={isPending || !file} className="btn-primary btn-lg">
              {isPending ? <><Spinner size="sm"/> Đang tạo...</> : <><FileText size={17}/> Tạo văn bản</>}
            </button>
          </div>
        </form>
      </div>
    </AppLayout>
  )
}
