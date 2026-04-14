// ═══════════════════════════════════════════════════════════════════
// DocumentDetailPage.tsx
// ═══════════════════════════════════════════════════════════════════
import { useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import {
  Download, Send, CheckCircle, XCircle, ArrowLeft,
  FileText, Clock, Lock, Unlock, Users, History,
  ChevronDown, ChevronUp, AlertTriangle
} from 'lucide-react'
import { toast } from 'react-hot-toast'
import { vanBanAPI, workflowAPI, soHieuAPI, phanPhoiAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { StatusBadge, Spinner, Modal, FormField, Alert } from '@/components/custom-ui'
import { fmtDateTime, fmtDate, fileIcon, fmtFileSize, canApproveStep2, canApproveStep3, canIssueNumber, canDistribute, TYPE_NEEDS_ISSUANCE } from '@/utils'
import { useAuthStore } from '@/stores/authStore'

export default function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { user } = useAuthStore()

  const [showRejectBM, setShowRejectBM] = useState(false)
  const [showRejectKhoa, setShowRejectKhoa] = useState(false)
  const [showIssue, setShowIssue] = useState(false)
  const [showDistribute, setShowDistribute] = useState(false)
  const [showHistory, setShowHistory] = useState(false)

  const [lyDoTuChoi, setLyDoTuChoi] = useState('')
  const [issueData, setIssueData] = useState({ nguoiKy: '', chucVuKy: '', ngayHieuLuc: '' })
  const [selectedRecipients, setSelectedRecipients] = useState<number[]>([])

  const vbId = parseInt(id!)

  const { data: vb, isLoading } = useQuery({
    queryKey: ['van-ban', vbId],
    queryFn: () => vanBanAPI.layChiTiet(vbId).then(r => r.data),
  })

  const invalidate = () => qc.invalidateQueries({ queryKey: ['van-ban', vbId] })

  // ── Action mutations ──
  const submitMut = useMutation({
    mutationFn: () => workflowAPI.nop(vbId),
    onSuccess: () => { toast.success('Đã nộp văn bản'); invalidate() },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi nộp văn bản'),
  })

  const approveBMMut = useMutation({
    mutationFn: () => workflowAPI.xacMinhChuyenMon(vbId),
    onSuccess: () => { toast.success('Đã xác minh chuyên môn'); invalidate() },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi xác minh'),
  })

  const rejectBMMut = useMutation({
    mutationFn: () => workflowAPI.tuChoiBoMon(vbId, lyDoTuChoi),
    onSuccess: () => { toast.success('Đã từ chối'); setShowRejectBM(false); invalidate() },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi từ chối'),
  })

  const approveKhoaMut = useMutation({
    mutationFn: () => workflowAPI.pheDuyetCuoi(vbId),
    onSuccess: () => { toast.success('Đã phê duyệt văn bản'); invalidate() },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi phê duyệt'),
  })

  const rejectKhoaMut = useMutation({
    mutationFn: () => workflowAPI.tuChoiKhoa(vbId, lyDoTuChoi),
    onSuccess: () => { toast.success('Đã từ chối'); setShowRejectKhoa(false); invalidate() },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi từ chối'),
  })

  const resubmitMut = useMutation({
    mutationFn: () => workflowAPI.nopLai(vbId),
    onSuccess: () => { toast.success('Đã nộp lại văn bản'); invalidate() },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi nộp lại'),
  })

  const issueMut = useMutation({
    mutationFn: () => soHieuAPI.capSo({ vanBanId: vbId, ...issueData }),
    onSuccess: () => { toast.success('Đã cấp số hiệu'); setShowIssue(false); invalidate() },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi cấp số hiệu'),
  })

  const distributeMut = useMutation({
    mutationFn: () => phanPhoiAPI.phanPhoi(vbId, selectedRecipients),
    onSuccess: () => { toast.success('Đã phân phối văn bản'); setShowDistribute(false); invalidate() },
    onError: (e: any) => toast.error(e.response?.data?.thongBao ?? 'Lỗi phân phối'),
  })

  const downloadFile = async (phienBanId?: number) => {
    try {
      const res = await vanBanAPI.taiXuong(vbId, phienBanId)
      const url = URL.createObjectURL(res.data as Blob)
      const a = document.createElement('a')
      a.href = url
      a.download = vb?.phienBans[0]?.tenFile ?? 'file'
      a.click()
      URL.revokeObjectURL(url)
    } catch { toast.error('Không tải được file') }
  }

  if (isLoading) return (
    <AppLayout title="Chi tiết văn bản">
      <div className="flex justify-center py-20"><Spinner size="lg"/></div>
    </AppLayout>
  )
  if (!vb) return (
    <AppLayout><div className="text-center py-20 text-ink-400">Không tìm thấy văn bản</div></AppLayout>
  )

  const role = user?.role
  const isOwner = user?.id === undefined || vb.tenNguoiTao === user?.hoTen
  const isDraft = vb.trangThai === 'Draft'
  const isPendingBM = vb.trangThai === 'PendingDepartment'
  const isPendingKhoa = vb.trangThai === 'PendingFaculty'
  const isApproved = vb.trangThai === 'Approved'
  const wasRejected = vb.lichSuXuLy?.some(l => l.hanhDong.startsWith('TuChoi'))

  return (
    <AppLayout title={vb.tieuDe}>
      <div className="max-w-5xl mx-auto space-y-5">
        {/* Back + Header */}
        <div className="flex items-start gap-4 flex-wrap">
          <button onClick={() => navigate(-1)} className="btn-ghost btn-icon flex-shrink-0 mt-0.5">
            <ArrowLeft size={17}/>
          </button>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap mb-1">
              <StatusBadge status={vb.trangThai}/>
              <span className="badge bg-ink-100 text-ink-600">{vb.loaiVanBanHienThi}</span>
              {vb.isLocked && <span className="badge bg-amber-50 text-amber-600 border border-amber-200"><Lock size={11}/> Đã khoá</span>}
              {vb.quaHan && <span className="badge badge-rejected"><AlertTriangle size={11}/> Quá hạn</span>}
            </div>
            <h1 className="text-xl font-display font-semibold text-ink-900 leading-tight">{vb.tieuDe}</h1>
            <p className="text-sm text-ink-400 mt-1">
              {vb.tenNguoiTao} • {vb.tenBoMon} •{' '}
              {fmtDateTime(vb.ngayCapNhat ?? vb.ngayTao)}
              {vb.soHieu && <span className="ml-2 font-mono text-jade-600 font-medium">{vb.soHieu}</span>}
            </p>
          </div>
        </div>

        {/* Overdue alert */}
        {vb.quaHan && (
          <Alert type="error" title="Quá hạn xử lý">
            Văn bản này đã quá hạn {vb.hanXuLy ? `(hạn: ${fmtDate(vb.hanXuLy)})` : ''}. Vui lòng xử lý sớm.
          </Alert>
        )}

        {/* Rejected alert */}
        {isDraft && wasRejected && (
          <Alert type="warning" title="Văn bản bị từ chối – cần chỉnh sửa">
            Vui lòng chỉnh sửa nội dung theo ý kiến người duyệt, tải lên phiên bản mới rồi nộp lại.
            <div className="mt-2 flex gap-2 flex-wrap">
              <Link to={`/van-ban/${vbId}/chinh-sua`} className="btn btn-sm bg-amber-500 text-white hover:bg-amber-600">
                Chỉnh sửa văn bản
              </Link>
              <button onClick={() => resubmitMut.mutate()} disabled={resubmitMut.isPending} className="btn-secondary btn-sm">
                Nộp lại
              </button>
            </div>
          </Alert>
        )}

        {/* Action buttons */}
        <div className="flex gap-2 flex-wrap">
          {/* Owner: nộp (Draft chưa bị từ chối) */}
          {isDraft && !wasRejected && ['GiangVien', 'VanThuKhoa'].includes(role ?? '') && (
            <button onClick={() => submitMut.mutate()} disabled={submitMut.isPending} className="btn-primary">
              <Send size={15}/> Nộp duyệt
            </button>
          )}

          {/* Trưởng BM actions */}
          {isPendingBM && canApproveStep2(role!) && <>
            <button onClick={() => approveBMMut.mutate()} disabled={approveBMMut.isPending} className="btn-primary">
              <CheckCircle size={15}/> Xác minh đạt
            </button>
            <button onClick={() => { setLyDoTuChoi(''); setShowRejectBM(true) }} className="btn-danger">
              <XCircle size={15}/> Từ chối
            </button>
          </>}

          {/* Lãnh đạo actions */}
          {isPendingKhoa && canApproveStep3(role!) && <>
            <button onClick={() => approveKhoaMut.mutate()} disabled={approveKhoaMut.isPending} className="btn-primary">
              <CheckCircle size={15}/> Phê duyệt
            </button>
            <button onClick={() => { setLyDoTuChoi(''); setShowRejectKhoa(true) }} className="btn-danger">
              <XCircle size={15}/> Từ chối
            </button>
          </>}

          {/* Văn thư: cấp số */}
          {isApproved && canIssueNumber(role!) && TYPE_NEEDS_ISSUANCE(vb.loaiVanBan) && (
            <button onClick={() => setShowIssue(true)} className="btn-primary">
              Cấp số hiệu
            </button>
          )}

          {/* Phân phối */}
          {(isApproved || vb.trangThai === 'Issued') && canDistribute(role!) && (
            <button onClick={() => setShowDistribute(true)} className="btn-secondary">
              <Users size={15}/> Phân phối
            </button>
          )}

          {/* Download */}
          {vb.phienBans.length > 0 && (
            <button onClick={() => downloadFile()} className="btn-secondary">
              <Download size={15}/> Tải xuống
            </button>
          )}
        </div>

        {/* Main content grid */}
        <div className="grid lg:grid-cols-3 gap-5">
          {/* Left: details */}
          <div className="lg:col-span-2 space-y-5">
            {/* Description */}
            {vb.moTa && (
              <div className="card p-5">
                <h3 className="text-sm font-semibold text-ink-700 mb-2">Mô tả</h3>
                <p className="text-sm text-ink-600 leading-relaxed">{vb.moTa}</p>
              </div>
            )}

            {/* File versions */}
            <div className="card overflow-hidden">
              <div className="px-5 py-3.5 border-b border-ink-100">
                <h3 className="text-sm font-semibold text-ink-700">
                  File đính kèm ({vb.phienBans.length} phiên bản)
                </h3>
              </div>
              <div className="divide-y divide-ink-50">
                {vb.phienBans.map(pv => (
                  <div key={pv.id} className="flex items-center gap-3 px-5 py-3.5">
                    <span className="text-2xl">{fileIcon(pv.contentType)}</span>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-ink-800 truncate">{pv.tenFile}</p>
                      <p className="text-xs text-ink-400 mt-0.5">
                        v{pv.soPhienBan} • {fmtFileSize(pv.kichThuocBytes)} •{' '}
                        {fmtDateTime(pv.ngayUpload)} • {pv.tenNguoiUpload}
                      </p>
                      {pv.ghiChuChinhSua && (
                        <p className="text-xs text-jade-600 mt-0.5 italic">{pv.ghiChuChinhSua}</p>
                      )}
                    </div>
                    <button onClick={() => downloadFile(pv.id)} className="btn-ghost btn-icon flex-shrink-0">
                      <Download size={15}/>
                    </button>
                  </div>
                ))}
              </div>
            </div>

            {/* Approval steps */}
            <div className="card overflow-hidden">
              <div className="px-5 py-3.5 border-b border-ink-100">
                <h3 className="text-sm font-semibold text-ink-700">Quy trình phê duyệt</h3>
              </div>
              <div className="p-5 space-y-4">
                {vb.buocPheDuyets.map((buoc, i) => {
                  const colors = {
                    Waiting: 'border-ink-200 bg-ink-50',
                    Pending: 'border-amber-300 bg-amber-50',
                    Approved: 'border-jade-300 bg-jade-50',
                    Rejected: 'border-rose-300 bg-rose-50',
                  }
                  const icons = {
                    Waiting: '○',
                    Pending: '●',
                    Approved: '✓',
                    Rejected: '✗',
                  }
                  const ts = buoc.trangThai as keyof typeof colors
                  return (
                    <div key={buoc.id} className={`flex gap-4 p-4 rounded-xl border ${colors[ts]}`}>
                      <div className={`w-7 h-7 rounded-full flex items-center justify-center text-sm font-bold flex-shrink-0
                        ${ts === 'Approved' ? 'bg-jade-600 text-white' :
                          ts === 'Rejected' ? 'bg-rose-500 text-white' :
                          ts === 'Pending'  ? 'bg-amber-500 text-white' : 'bg-ink-200 text-ink-500'}`}>
                        {icons[ts]}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between flex-wrap gap-2">
                          <p className="text-sm font-semibold text-ink-800">{buoc.tenBuoc}</p>
                          <span className={`badge text-xs ${ts === 'Approved' ? 'badge-approved' : ts === 'Rejected' ? 'badge-rejected' : ts === 'Pending' ? 'badge-pending' : 'badge-draft'}`}>
                            {buoc.trangThaiHienThi}
                          </span>
                        </div>
                        {buoc.tenNguoiDuocGiao && (
                          <p className="text-xs text-ink-500 mt-1">Giao cho: {buoc.tenNguoiDuocGiao}</p>
                        )}
                        {buoc.tenNguoiXuLy && (
                          <p className="text-xs text-ink-600 mt-1">Xử lý bởi: <span className="font-medium">{buoc.tenNguoiXuLy}</span> • {fmtDateTime(buoc.ngayXuLy)}</p>
                        )}
                        {buoc.yKien && (
                          <div className={`mt-2 px-3 py-2 rounded-lg text-xs ${ts === 'Rejected' ? 'bg-rose-100 text-rose-700' : 'bg-white text-ink-600'}`}>
                            💬 {buoc.yKien}
                          </div>
                        )}
                        {buoc.quaHan && (
                          <p className="text-xs text-rose-500 mt-1 flex items-center gap-1">
                            <AlertTriangle size={11}/> Quá hạn {fmtDate(buoc.hanXuLy)}
                          </p>
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          </div>

          {/* Right: sidebar info */}
          <div className="space-y-4">
            {/* Issuance info */}
            {vb.thongTinSoHieu && (
              <div className="card p-4 bg-jade-50 border-jade-200">
                <h3 className="text-xs font-semibold text-jade-800 uppercase tracking-wider mb-3">
                  Số hiệu ban hành
                </h3>
                <p className="text-2xl font-mono font-bold text-jade-700">{vb.thongTinSoHieu.soHieu}</p>
                <div className="mt-3 space-y-1.5 text-xs text-jade-700">
                  {vb.thongTinSoHieu.nguoiKy && <p>Người ký: {vb.thongTinSoHieu.nguoiKy}</p>}
                  {vb.thongTinSoHieu.chucVuKy && <p>Chức vụ: {vb.thongTinSoHieu.chucVuKy}</p>}
                  <p>Ngày cấp: {fmtDate(vb.thongTinSoHieu.ngayCapSo)}</p>
                  <p>Người cấp: {vb.thongTinSoHieu.tenNguoiCapSo}</p>
                </div>
              </div>
            )}

            {/* Recipients */}
            {vb.danhSachNguoiNhan.length > 0 && (
              <div className="card overflow-hidden">
                <div className="px-4 py-3 border-b border-ink-100">
                  <h3 className="text-xs font-semibold text-ink-600 uppercase tracking-wider">
                    Người nhận ({vb.danhSachNguoiNhan.length})
                  </h3>
                </div>
                <div className="divide-y divide-ink-50 max-h-64 overflow-y-auto">
                  {vb.danhSachNguoiNhan.map(r => (
                    <div key={r.nguoiNhanId} className="px-4 py-3 flex items-center gap-3">
                      <div className="w-7 h-7 rounded-full bg-ink-200 flex items-center justify-center
                                      text-ink-600 text-xs font-bold flex-shrink-0">
                        {r.hoTen.charAt(0)}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-xs font-medium text-ink-800 truncate">{r.hoTen}</p>
                        <div className="flex gap-2 mt-0.5">
                          <span className={`text-xs ${r.daDoc ? 'text-jade-600' : 'text-ink-400'}`}>
                            {r.daDoc ? '✓ Đã đọc' : '○ Chưa đọc'}
                          </span>
                          {r.daTiepNhan && <span className="text-xs text-jade-600">✓ Đã tiếp nhận</span>}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Workflow history toggle */}
            <div className="card overflow-hidden">
              <button onClick={() => setShowHistory(!showHistory)}
                className="w-full px-4 py-3 flex items-center justify-between hover:bg-ink-50 transition-colors">
                <span className="text-xs font-semibold text-ink-600 uppercase tracking-wider flex items-center gap-2">
                  <History size={13}/> Lịch sử xử lý
                </span>
                {showHistory ? <ChevronUp size={14}/> : <ChevronDown size={14}/>}
              </button>
              {showHistory && (
                <div className="border-t border-ink-100 max-h-80 overflow-y-auto">
                  {vb.lichSuXuLy.map(log => (
                    <div key={log.id} className="px-4 py-3 border-b border-ink-50 last:border-0">
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <p className="text-xs font-semibold text-ink-800">{log.hanhDongHienThi}</p>
                          <p className="text-xs text-ink-500">{log.tenNguoiThucHien} • {log.roleNguoiThucHien}</p>
                          {log.ghiChu && <p className="text-xs text-rose-600 mt-1 italic">"{log.ghiChu}"</p>}
                        </div>
                        <p className="text-xs text-ink-400 whitespace-nowrap">{fmtDateTime(log.thoiGian)}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ── Reject BM modal ── */}
      <Modal open={showRejectBM} onClose={() => setShowRejectBM(false)}
        title="Từ chối xác minh chuyên môn" size="sm"
        footer={<>
          <button onClick={() => setShowRejectBM(false)} className="btn-secondary">Huỷ</button>
          <button onClick={() => rejectBMMut.mutate()} disabled={!lyDoTuChoi.trim() || rejectBMMut.isPending} className="btn-danger">
            Từ chối
          </button>
        </>}>
        <Alert type="warning" title="Lý do từ chối là bắt buộc">
          Người soạn thảo cần biết cụ thể phải sửa gì để nộp lại.
        </Alert>
        <div className="mt-4">
          <FormField label="Lý do từ chối" required>
            <textarea value={lyDoTuChoi} onChange={e => setLyDoTuChoi(e.target.value)}
              rows={4} placeholder="Nêu rõ nội dung cần chỉnh sửa..."
              className="input resize-none"/>
          </FormField>
        </div>
      </Modal>

      {/* ── Reject Khoa modal ── */}
      <Modal open={showRejectKhoa} onClose={() => setShowRejectKhoa(false)}
        title="Từ chối phê duyệt" size="sm"
        footer={<>
          <button onClick={() => setShowRejectKhoa(false)} className="btn-secondary">Huỷ</button>
          <button onClick={() => rejectKhoaMut.mutate()} disabled={!lyDoTuChoi.trim() || rejectKhoaMut.isPending} className="btn-danger">
            Từ chối
          </button>
        </>}>
        <Alert type="warning" title="Văn bản sẽ về bản nháp">
          Khi nộp lại, văn bản sẽ bỏ qua Bước 2 (Bộ môn) và chuyển thẳng lên phê duyệt.
        </Alert>
        <div className="mt-4">
          <FormField label="Lý do từ chối" required>
            <textarea value={lyDoTuChoi} onChange={e => setLyDoTuChoi(e.target.value)}
              rows={4} placeholder="Nêu rõ yêu cầu chỉnh sửa..." className="input resize-none"/>
          </FormField>
        </div>
      </Modal>

      {/* ── Issue number modal ── */}
      <Modal open={showIssue} onClose={() => setShowIssue(false)} title="Cấp số hiệu văn bản" size="sm"
        footer={<>
          <button onClick={() => setShowIssue(false)} className="btn-secondary">Huỷ</button>
          <button onClick={() => issueMut.mutate()} disabled={issueMut.isPending} className="btn-primary">
            Cấp số hiệu
          </button>
        </>}>
        <div className="space-y-4">
          <Alert type="info">Để trống "Số hiệu tùy chỉnh" → hệ thống tự sinh theo chuẩn (VD: 05/2025/TB-KCN)</Alert>
          <FormField label="Người ký">
            <input value={issueData.nguoiKy} onChange={e => setIssueData(p => ({...p, nguoiKy: e.target.value}))}
              placeholder="PGS.TS. Nguyễn Văn Khoa" className="input"/>
          </FormField>
          <FormField label="Chức vụ người ký">
            <input value={issueData.chucVuKy} onChange={e => setIssueData(p => ({...p, chucVuKy: e.target.value}))}
              placeholder="Trưởng Khoa" className="input"/>
          </FormField>
          <FormField label="Ngày hiệu lực">
            <input type="date" value={issueData.ngayHieuLuc}
              onChange={e => setIssueData(p => ({...p, ngayHieuLuc: e.target.value}))} className="input"/>
          </FormField>
        </div>
      </Modal>
    </AppLayout>
  )
}
