import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { toast } from 'react-hot-toast'
import { AppLayout } from '@/components/layout'
import { Spinner, Tabs, Alert } from '@/components/custom-ui'
import { useAuthStore } from '@/stores/authStore'
import { nguoiDungAPI, authAPI } from '@/api'

export default function SettingsPage() {
  const { user, updateUser } = useAuthStore()
  const [tab, setTab] = useState('profile')

  const [profile, setProfile] = useState({
    hoTen: user?.hoTen ?? '',
    chucDanh: user?.chucDanh ?? '',
    soDienThoai: user?.soDienThoai ?? '',
  })
  const [pw, setPw] = useState({ matKhauHienTai: '', matKhauMoi: '', xacNhan: '' })
  const [pwError, setPwError] = useState('')

  const profileMut = useMutation({
    mutationFn: () => nguoiDungAPI.capNhatHoSo(profile),
    onSuccess: (res) => { updateUser(res.data as any); toast.success('Đã cập nhật hồ sơ') },
    onError: () => toast.error('Lỗi cập nhật hồ sơ'),
  })

  const pwMut = useMutation({
    mutationFn: () => authAPI.doiMatKhau(pw.matKhauHienTai, pw.matKhauMoi),
    onSuccess: () => {
      toast.success('Đổi mật khẩu thành công')
      setPw({ matKhauHienTai: '', matKhauMoi: '', xacNhan: '' })
      setPwError('')
    },
    onError: (e: any) => setPwError(e.response?.data?.thongBao ?? 'Lỗi đổi mật khẩu'),
  })

  const handleChangePw = () => {
    if (pw.matKhauMoi !== pw.xacNhan) { setPwError('Mật khẩu xác nhận không khớp'); return }
    if (pw.matKhauMoi.length < 8) { setPwError('Mật khẩu mới ít nhất 8 ký tự'); return }
    setPwError('')
    pwMut.mutate()
  }

  return (
    <AppLayout title="Cài đặt">
      <div className="max-w-xl mx-auto space-y-5">
        <div>
          <h1 className="page-title">Cài đặt tài khoản</h1>
          <p className="page-subtitle">Quản lý thông tin cá nhân và bảo mật</p>
        </div>

        <Tabs
          tabs={[
            { key: 'profile',  label: 'Thông tin cá nhân' },
            { key: 'security', label: 'Bảo mật' },
          ]}
          active={tab} onChange={setTab}
        />

        {tab === 'profile' && (
          <div className="card p-6 space-y-5">
            {/* Avatar block */}
            <div className="flex items-center gap-4 pb-4 border-b border-ink-100">
              <div className="w-14 h-14 rounded-2xl bg-jade-600 flex items-center justify-center
                              text-white text-xl font-bold font-display flex-shrink-0">
                {user?.hoTen.charAt(0)}
              </div>
              <div>
                <p className="font-semibold text-ink-900">{user?.hoTen}</p>
                <span className="inline-flex items-center gap-1 mt-1 text-xs px-2 py-0.5 rounded-full bg-jade-50 text-jade-700 font-medium">
                  {user?.roleTenHienThi}
                </span>
                {user?.tenBoMon && <p className="text-xs text-ink-400 mt-1">{user?.tenBoMon}</p>}
              </div>
            </div>

            <div className="space-y-4">
              <div>
                <label className="label">Họ và tên <span className="text-rose-500">*</span></label>
                <input value={profile.hoTen}
                  onChange={e => setProfile(p => ({ ...p, hoTen: e.target.value }))}
                  className="input" />
              </div>
              <div className="grid sm:grid-cols-2 gap-4">
                <div>
                  <label className="label">Chức danh</label>
                  <input value={profile.chucDanh}
                    onChange={e => setProfile(p => ({ ...p, chucDanh: e.target.value }))}
                    placeholder="ThS., TS., PGS.TS." className="input" />
                </div>
                <div>
                  <label className="label">Số điện thoại</label>
                  <input value={profile.soDienThoai}
                    onChange={e => setProfile(p => ({ ...p, soDienThoai: e.target.value }))}
                    placeholder="0912 345 678" className="input" />
                </div>
              </div>
              <div>
                <label className="label">Email (không thể thay đổi)</label>
                <input value={user?.email} disabled
                  className="input bg-ink-50 text-ink-400 cursor-not-allowed" />
              </div>
            </div>

            <div className="flex justify-end pt-2">
              <button onClick={() => profileMut.mutate()} disabled={profileMut.isPending}
                className="btn-primary">
                {profileMut.isPending ? <Spinner size="sm" /> : 'Lưu thay đổi'}
              </button>
            </div>
          </div>
        )}

        {tab === 'security' && (
          <div className="card p-6 space-y-5">
            <h2 className="text-sm font-semibold text-ink-700">Đổi mật khẩu</h2>

            <Alert type="info">
              Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và số.
            </Alert>

            <div className="space-y-4">
              <div>
                <label className="label">Mật khẩu hiện tại <span className="text-rose-500">*</span></label>
                <input type="password" value={pw.matKhauHienTai}
                  onChange={e => setPw(p => ({ ...p, matKhauHienTai: e.target.value }))}
                  className="input" placeholder="••••••••" />
              </div>
              <div>
                <label className="label">Mật khẩu mới <span className="text-rose-500">*</span></label>
                <input type="password" value={pw.matKhauMoi}
                  onChange={e => setPw(p => ({ ...p, matKhauMoi: e.target.value }))}
                  className="input" placeholder="Tối thiểu 8 ký tự" />
              </div>
              <div>
                <label className="label">Xác nhận mật khẩu mới <span className="text-rose-500">*</span></label>
                <input type="password" value={pw.xacNhan}
                  onChange={e => setPw(p => ({ ...p, xacNhan: e.target.value }))}
                  className={`input ${pwError ? 'input-error' : ''}`} placeholder="Nhập lại mật khẩu mới" />
                {pwError && <p className="error-msg">⚠ {pwError}</p>}
              </div>
            </div>

            <div className="flex justify-end pt-2">
              <button onClick={handleChangePw} disabled={pwMut.isPending}
                className="btn-primary">
                {pwMut.isPending ? <Spinner size="sm" /> : 'Đổi mật khẩu'}
              </button>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  )
}
