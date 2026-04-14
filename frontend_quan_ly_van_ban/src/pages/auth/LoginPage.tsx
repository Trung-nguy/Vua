// ═══════════════════════════════════════════════════════════════════
// src/pages/auth/LoginPage.tsx
// ═══════════════════════════════════════════════════════════════════
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { motion } from 'framer-motion'
import { Eye, EyeOff, FileText, Loader2 } from 'lucide-react'
import { toast } from 'react-hot-toast'
import { authAPI } from '@/api'
import { useAuthStore } from '@/stores/authStore'

const schema = z.object({
  email: z.string().email('Email không hợp lệ'),
  matKhau: z.string().min(6, 'Mật khẩu tối thiểu 6 ký tự'),
})
type FormData = z.infer<typeof schema>

export default function LoginPage() {
  const navigate = useNavigate()
  const { setAuth } = useAuthStore()
  const [showPw, setShowPw] = useState(false)
  const [loading, setLoading] = useState(false)

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const onSubmit = async (data: FormData) => {
    setLoading(true)
    try {
      const res = await authAPI.dangNhap(data.email, data.matKhau)
      const r = res.data
      if (r.thanhCong && r.nguoiDung && r.accessToken && r.refreshToken) {
        setAuth(r.nguoiDung, r.accessToken, r.refreshToken)
        toast.success(`Chào mừng, ${r.nguoiDung.hoTen}!`)
        navigate('/dashboard')
      } else {
        toast.error(r.thongBao ?? 'Đăng nhập thất bại')
      }
    } catch {
      toast.error('Không thể kết nối đến máy chủ')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-ink-950 flex">
      {/* Left decorative panel */}
      <div className="hidden lg:flex flex-1 items-center justify-center p-16 relative overflow-hidden">
        <div className="absolute inset-0 opacity-10"
          style={{ backgroundImage: 'radial-gradient(circle at 30% 40%, #1ea06a 0%, transparent 60%), radial-gradient(circle at 70% 70%, #0ea5e9 0%, transparent 50%)' }}/>
        <div className="relative z-10 max-w-md">
          <div className="w-16 h-16 rounded-2xl bg-jade-500 flex items-center justify-center mb-8">
            <FileText size={32} className="text-white"/>
          </div>
          <h1 className="font-display text-4xl font-semibold text-white leading-tight mb-4">
            Hệ thống<br/>Quản lý Văn bản
          </h1>
          <p className="text-ink-400 text-lg leading-relaxed">
            Quản lý toàn bộ quy trình văn bản hành chính từ soạn thảo đến phân phối, đảm bảo đúng phân cấp và minh bạch.
          </p>
          <div className="mt-10 space-y-4">
            {[
              { step: '1', label: 'Soạn thảo & Đề nghị duyệt', sub: 'Giảng viên / Văn thư Khoa' },
              { step: '2', label: 'Xác minh chuyên môn', sub: 'Trưởng Bộ môn' },
              { step: '3', label: 'Phê duyệt cuối cùng', sub: 'Lãnh đạo Khoa' },
            ].map(s => (
              <div key={s.step} className="flex items-center gap-4">
                <div className="w-8 h-8 rounded-full bg-jade-500/20 border border-jade-500/40
                                flex items-center justify-center text-jade-400 text-sm font-bold">
                  {s.step}
                </div>
                <div>
                  <p className="text-white text-sm font-medium">{s.label}</p>
                  <p className="text-ink-500 text-xs">{s.sub}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Right login form */}
      <div className="w-full lg:w-[440px] flex items-center justify-center p-8 bg-white">
        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.4 }} className="w-full max-w-sm">

          <div className="mb-8">
            <div className="w-10 h-10 rounded-xl bg-jade-600 flex items-center justify-center mb-5 lg:hidden">
              <FileText size={20} className="text-white"/>
            </div>
            <h2 className="font-display text-2xl font-semibold text-ink-900 mb-1">Đăng nhập</h2>
            <p className="text-ink-400 text-sm">Nhập thông tin tài khoản để tiếp tục</p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div>
              <label className="label">Email</label>
              <input {...register('email')} type="email" placeholder="ten@truong.edu.vn"
                className={`input ${errors.email ? 'input-error' : ''}`}/>
              {errors.email && <p className="error-msg">⚠ {errors.email.message}</p>}
            </div>

            <div>
              <label className="label">Mật khẩu</label>
              <div className="relative">
                <input {...register('matKhau')} type={showPw ? 'text' : 'password'}
                  placeholder="••••••••" className={`input pr-10 ${errors.matKhau ? 'input-error' : ''}`}/>
                <button type="button" onClick={() => setShowPw(!showPw)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-ink-400 hover:text-ink-600">
                  {showPw ? <EyeOff size={16}/> : <Eye size={16}/>}
                </button>
              </div>
              {errors.matKhau && <p className="error-msg">⚠ {errors.matKhau.message}</p>}
            </div>

            <button type="submit" disabled={loading} className="btn-primary w-full btn-lg">
              {loading ? <><Loader2 size={18} className="animate-spin"/> Đang đăng nhập...</> : 'Đăng nhập'}
            </button>
          </form>

          <div className="mt-8 p-4 rounded-xl bg-ink-50 border border-ink-100">
            <p className="text-xs font-semibold text-ink-500 uppercase tracking-wider mb-3">Tài khoản demo</p>
            <div className="space-y-2">
              {[
                ['Giảng viên', 'hvdung.gv@khoa.edu.vn', 'GVien@123456'],
                ['Trưởng BM', 'lvan.tbm@khoa.edu.vn', 'TBM@123456'],
                ['Lãnh đạo', 'nvkhoa.ldkhoa@khoa.edu.vn', 'LdKhoa@123456'],
                ['Văn thư', 'ntcam.vanThu@khoa.edu.vn', 'VanThu@123456'],
              ].map(([role, email, pw]) => (
                <button key={email} type="button"
                  onClick={() => { /* prefill */ toast('Nhấn đăng nhập sau khi điền', { icon: 'ℹ️' }) }}
                  className="w-full text-left p-2 rounded-lg hover:bg-ink-100 transition-colors group">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-medium text-ink-700">{role}</span>
                    <span className="text-xs text-ink-400 font-mono">{pw}</span>
                  </div>
                  <p className="text-xs text-ink-400">{email}</p>
                </button>
              ))}
            </div>
          </div>
        </motion.div>
      </div>
    </div>
  )
}
