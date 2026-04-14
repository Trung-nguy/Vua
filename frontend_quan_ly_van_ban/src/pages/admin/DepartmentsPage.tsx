import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { Plus, Edit3, Trash2, Building2, Users } from 'lucide-react'
import { toast } from 'react-hot-toast'
import { boMonAPI, nguoiDungAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { Spinner, EmptyState, Modal, FormField, ConfirmDialog } from '@/components/custom-ui'
import type { BoMon } from '@/types'

export default function DepartmentsPage() {
  const qc = useQueryClient()
  const [createModal, setCreateModal] = useState(false)
  const [editModal, setEditModal] = useState<BoMon | null>(null)
  const [deleteModal, setDeleteModal] = useState<BoMon | null>(null)

  const initForm = { ten: '', maBoMon: '', truongBoMonId: '' }
  const [form, setForm] = useState(initForm)

  const { data: boMons, isLoading } = useQuery({
    queryKey: ['bo-mon-admin'],
    queryFn: () => boMonAPI.layDanhSach(false).then(r => r.data),
  })

  const { data: users } = useQuery({
    queryKey: ['users-for-bomon'],
    queryFn: () => nguoiDungAPI.layDanhSach({ role: 'TruongBoMon', kichThuoc: 50 }).then(r => r.data.duLieu),
    enabled: createModal || !!editModal,
  })

  const createMut = useMutation({
    mutationFn: () => boMonAPI.tao({
      ten: form.ten,
      maBoMon: form.maBoMon || undefined,
      truongBoMonId: form.truongBoMonId ? parseInt(form.truongBoMonId) : undefined,
    }),
    onSuccess: () => {
      toast.success('Tạo bộ môn thành công')
      setCreateModal(false); setForm(initForm)
      qc.invalidateQueries({ queryKey: ['bo-mon-admin'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi tạo bộ môn'),
  })

  const updateMut = useMutation({
    mutationFn: () => boMonAPI.capNhat(editModal!.id, {
      ten: form.ten,
      maBoMon: form.maBoMon || undefined,
      truongBoMonId: form.truongBoMonId ? parseInt(form.truongBoMonId) : undefined,
    }),
    onSuccess: () => {
      toast.success('Đã cập nhật bộ môn')
      setEditModal(null)
      qc.invalidateQueries({ queryKey: ['bo-mon-admin'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi cập nhật'),
  })

  const deleteMut = useMutation({
    mutationFn: () => boMonAPI.xoa(deleteModal!.id),
    onSuccess: () => {
      toast.success('Đã xoá bộ môn')
      setDeleteModal(null)
      qc.invalidateQueries({ queryKey: ['bo-mon-admin'] })
    },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi xoá bộ môn'),
  })

  const openEdit = (b: BoMon) => {
    setEditModal(b)
    setForm({ ten: b.ten, maBoMon: b.maBoMon ?? '', truongBoMonId: b.truongBoMonId?.toString() ?? '' })
  }

  const ModalForm = () => (
    <div className="space-y-4">
      <FormField label="Tên bộ môn" required>
        <input value={form.ten} onChange={e => setForm(p => ({ ...p, ten: e.target.value }))}
          placeholder="Bộ môn Khoa học Máy tính" className="input" />
      </FormField>
      <FormField label="Mã bộ môn" hint="VD: KHMT, KTMT">
        <input value={form.maBoMon} onChange={e => setForm(p => ({ ...p, maBoMon: e.target.value.toUpperCase() }))}
          placeholder="KHMT" className="input font-mono" maxLength={10} />
      </FormField>
      <FormField label="Trưởng bộ môn">
        <select value={form.truongBoMonId}
          onChange={e => setForm(p => ({ ...p, truongBoMonId: e.target.value }))} className="input">
          <option value="">— Chưa chỉ định —</option>
          {users?.map(u => <option key={u.id} value={u.id}>{u.hoTen}</option>)}
        </select>
      </FormField>
    </div>
  )

  return (
    <AppLayout title="Quản lý bộ môn">
      <div className="space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title">Quản lý bộ môn</h1>
            <p className="page-subtitle">{boMons?.length ?? 0} bộ môn / phòng ban</p>
          </div>
          <button onClick={() => { setCreateModal(true); setForm(initForm) }} className="btn-primary">
            <Plus size={16} /> Thêm bộ môn
          </button>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : boMons?.length === 0 ? (
          <EmptyState icon="🏢" title="Chưa có bộ môn nào"
            action={<button onClick={() => setCreateModal(true)} className="btn-primary btn-sm">
              <Plus size={14} /> Thêm mới
            </button>} />
        ) : (
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {boMons?.map((b, i) => (
              <motion.div key={b.id} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.04 }}
                className="card p-5 flex flex-col gap-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-start gap-3">
                    <div className="w-10 h-10 rounded-xl bg-ink-100 flex items-center justify-center flex-shrink-0">
                      <Building2 size={18} className="text-ink-500" />
                    </div>
                    <div>
                      <p className="text-sm font-semibold text-ink-900">{b.ten}</p>
                      {b.maBoMon && (
                        <span className="font-mono text-xs bg-ink-100 text-ink-600 px-1.5 py-0.5 rounded mt-1 inline-block">
                          {b.maBoMon}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-1 flex-shrink-0">
                    <button onClick={() => openEdit(b)} className="btn-ghost btn-icon">
                      <Edit3 size={14} />
                    </button>
                    <button onClick={() => setDeleteModal(b)} className="btn-ghost btn-icon text-rose-400 hover:bg-rose-50">
                      <Trash2 size={14} />
                    </button>
                  </div>
                </div>

                <div className="space-y-1.5 text-xs">
                  {b.tenTruongBoMon ? (
                    <div className="flex items-center gap-2 text-ink-600">
                      <span className="text-jade-500">👤</span>
                      <span>{b.tenTruongBoMon}</span>
                      <span className="text-jade-600 font-medium">(Trưởng BM)</span>
                    </div>
                  ) : (
                    <p className="text-ink-400 italic">Chưa có Trưởng BM</p>
                  )}
                  <div className="flex items-center gap-2 text-ink-500">
                    <Users size={12} />
                    <span>{b.soThanhVien} thành viên</span>
                  </div>
                </div>

                <div className={`text-xs font-medium px-2 py-1 rounded-lg self-start
                  ${b.isActive ? 'bg-jade-50 text-jade-700' : 'bg-ink-100 text-ink-500'}`}>
                  {b.isActive ? 'Hoạt động' : 'Ngừng hoạt động'}
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </div>

      {/* Create modal */}
      <Modal open={createModal} onClose={() => setCreateModal(false)} title="Thêm bộ môn mới" size="sm"
        footer={<>
          <button onClick={() => setCreateModal(false)} className="btn-secondary">Huỷ</button>
          <button onClick={() => createMut.mutate()} disabled={!form.ten.trim() || createMut.isPending} className="btn-primary">
            {createMut.isPending ? <Spinner size="sm" /> : 'Tạo bộ môn'}
          </button>
        </>}>
        <ModalForm />
      </Modal>

      {/* Edit modal */}
      <Modal open={!!editModal} onClose={() => setEditModal(null)} title={`Sửa: ${editModal?.ten}`} size="sm"
        footer={<>
          <button onClick={() => setEditModal(null)} className="btn-secondary">Huỷ</button>
          <button onClick={() => updateMut.mutate()} disabled={!form.ten.trim() || updateMut.isPending} className="btn-primary">
            {updateMut.isPending ? <Spinner size="sm" /> : 'Lưu thay đổi'}
          </button>
        </>}>
        <ModalForm />
      </Modal>

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteModal}
        title="Xoá bộ môn"
        message={`Bạn chắc chắn muốn xoá "${deleteModal?.ten}"? Không thể xoá nếu đang có thành viên.`}
        confirmLabel="Xoá"
        danger
        loading={deleteMut.isPending}
        onConfirm={() => deleteMut.mutate()}
        onCancel={() => setDeleteModal(null)}
      />
    </AppLayout>
  )
}
