import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { Search, UserCheck, UserX, Edit3, Building2, Plus, Shield } from 'lucide-react'
import { toast } from 'react-hot-toast'
import { nguoiDungAPI, boMonAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { Spinner, EmptyState, Modal, FormField, Alert, Pagination } from '@/components/custom-ui'
import { ROLE_LABELS, ROLE_BADGE_COLOR, fmtDate, fmtDateTime } from '@/utils'
import type { NguoiDung, BoMon, RoleName } from '@/types'

// ═══════════════════════════════════════════════════════════════════
// USERS PAGE
// ═══════════════════════════════════════════════════════════════════
export default function UsersPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState<RoleName | ''>('')
  const [trang, setTrang] = useState(1)
  const [editModal, setEditModal] = useState<NguoiDung | null>(null)
  const [editRole, setEditRole] = useState('')
  const [editBoMon, setEditBoMon] = useState<number | ''>('')

  const { data: usersData, isLoading } = useQuery({
    queryKey: ['admin-users', roleFilter, trang],
    queryFn: () => nguoiDungAPI.layDanhSach({
      role: roleFilter || undefined,
      activeOnly: false,
      trang,
      kichThuoc: 20,
    }).then(r => r.data),
  })

  const { data: boMons } = useQuery({
    queryKey: ['bo-mon-list'],
    queryFn: () => boMonAPI.layDanhSach().then(r => r.data),
  })

  const { data: roles } = useQuery({
    queryKey: ['roles'],
    queryFn: () => nguoiDungAPI.layDanhSach({ trang: 1, kichThuoc: 1, activeOnly: false })
      .then(() => Object.entries(ROLE_LABELS).map(([k, v]) => ({ name: k as RoleName, label: v }))),
  })

  const toggleMut = useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      nguoiDungAPI.khoaTaiKhoan(id, isActive),
    onSuccess: (_, { isActive }) => {
      toast.success(isActive ? 'Đã kích hoạt tài khoản' : 'Đã khoá tài khoản')
      qc.invalidateQueries({ queryKey: ['admin-users'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi'),
  })

  const updateRoleMut = useMutation({
    mutationFn: () => nguoiDungAPI.capNhatRole({
      nguoiDungId: editModal!.id,
      roleId: parseInt(editRole),
      boMonId: editBoMon ? parseInt(String(editBoMon)) : undefined,
    }),
    onSuccess: () => {
      toast.success('Đã cập nhật role')
      setEditModal(null)
      qc.invalidateQueries({ queryKey: ['admin-users'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi'),
  })

  const filtered = usersData?.duLieu.filter(u =>
    !search || u.hoTen.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase())
  ) ?? []

  return (
    <AppLayout title="Quản lý người dùng">
      <div className="space-y-5">
        <div>
          <h1 className="page-title">Quản lý người dùng</h1>
          <p className="page-subtitle">{usersData?.tongSoBanGhi ?? 0} tài khoản trong hệ thống</p>
        </div>

        {/* Filters */}
        <div className="card p-3 flex gap-2 flex-wrap">
          <div className="flex-1 min-w-[200px] relative">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-ink-400" />
            <input value={search} onChange={e => setSearch(e.target.value)}
              placeholder="Tìm theo tên hoặc email..." className="input pl-9 bg-ink-50 border-ink-100" />
          </div>
          <select value={roleFilter}
            onChange={e => { setRoleFilter(e.target.value as RoleName | ''); setTrang(1) }}
            className="input w-auto min-w-[180px] bg-ink-50 border-ink-100">
            <option value="">Tất cả vai trò</option>
            {Object.entries(ROLE_LABELS).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          {isLoading ? (
            <div className="flex justify-center py-16"><Spinner size="lg" /></div>
          ) : filtered.length === 0 ? (
            <EmptyState icon="👥" title="Không tìm thấy người dùng" />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr>
                    <th className="table-th">Người dùng</th>
                    <th className="table-th">Vai trò</th>
                    <th className="table-th">Bộ môn</th>
                    <th className="table-th">Trạng thái</th>
                    <th className="table-th">Đăng nhập</th>
                    <th className="table-th text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((u, i) => (
                    <motion.tr key={u.id} className="table-row-hover"
                      initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: i * 0.02 }}>
                      <td className="table-td">
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 rounded-full bg-jade-600 flex items-center justify-center
                                          text-white text-xs font-bold flex-shrink-0">
                            {u.hoTen.charAt(0)}
                          </div>
                          <div>
                            <p className="text-sm font-medium text-ink-900">{u.hoTen}</p>
                            <p className="text-xs text-ink-400">{u.email}</p>
                            {u.chucDanh && <p className="text-xs text-ink-400 italic">{u.chucDanh}</p>}
                          </div>
                        </div>
                      </td>
                      <td className="table-td">
                        <span className={`badge text-xs ${ROLE_BADGE_COLOR[u.role]}`}>
                          {u.roleTenHienThi}
                        </span>
                      </td>
                      <td className="table-td text-sm text-ink-500">{u.tenBoMon ?? '—'}</td>
                      <td className="table-td">
                        <span className={`badge text-xs ${u.isActive ? 'badge-approved' : 'badge-recalled'}`}>
                          {u.isActive ? 'Hoạt động' : 'Đã khoá'}
                        </span>
                      </td>
                      <td className="table-td text-xs text-ink-400">{fmtDateTime(u.lanDangNhapCuoi)}</td>
                      <td className="table-td text-right">
                        <div className="flex items-center justify-end gap-1">
                          <button onClick={() => {
                            setEditModal(u)
                            setEditRole(String(Object.keys(ROLE_LABELS).indexOf(u.role)))
                            setEditBoMon(u.boMonId ?? '')
                          }} className="btn-ghost btn-icon">
                            <Edit3 size={14} />
                          </button>
                          <button
                            onClick={() => toggleMut.mutate({ id: u.id, isActive: !u.isActive })}
                            className={`btn-icon ${u.isActive ? 'btn-ghost text-rose-500 hover:bg-rose-50' : 'btn-ghost text-jade-600 hover:bg-jade-50'}`}
                            title={u.isActive ? 'Khoá tài khoản' : 'Kích hoạt'}>
                            {u.isActive ? <UserX size={14} /> : <UserCheck size={14} />}
                          </button>
                        </div>
                      </td>
                    </motion.tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {usersData && usersData.tongSoTrang > 1 && (
            <div className="px-4 py-3 border-t border-ink-100 flex items-center justify-between">
              <p className="text-xs text-ink-400">
                {usersData.tongSoBanGhi} tài khoản
              </p>
              <Pagination trang={trang} tongSoTrang={usersData.tongSoTrang} onChange={setTrang} />
            </div>
          )}
        </div>
      </div>

      {/* Edit role modal */}
      <Modal open={!!editModal} onClose={() => setEditModal(null)}
        title={`Cập nhật role – ${editModal?.hoTen}`} size="sm"
        footer={<>
          <button onClick={() => setEditModal(null)} className="btn-secondary">Huỷ</button>
          <button onClick={() => updateRoleMut.mutate()} disabled={updateRoleMut.isPending} className="btn-primary">
            {updateRoleMut.isPending ? <Spinner size="sm" /> : 'Lưu thay đổi'}
          </button>
        </>}>
        <div className="space-y-4">
          <Alert type="warning">Thay đổi role sẽ ảnh hưởng đến quyền hạn của người dùng ngay lập tức.</Alert>
          <FormField label="Vai trò">
            <select value={editRole} onChange={e => setEditRole(e.target.value)} className="input">
              {Object.entries(ROLE_LABELS).map(([k, v], i) => (
                <option key={k} value={i}>{v}</option>
              ))}
            </select>
          </FormField>
          <FormField label="Bộ môn">
            <select value={editBoMon} onChange={e => setEditBoMon(e.target.value ? parseInt(e.target.value) : '')} className="input">
              <option value="">— Không thuộc bộ môn —</option>
              {boMons?.map(b => <option key={b.id} value={b.id}>{b.ten}</option>)}
            </select>
          </FormField>
        </div>
      </Modal>
    </AppLayout>
  )
}
