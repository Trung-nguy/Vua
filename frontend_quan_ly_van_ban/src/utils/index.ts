import { format, formatDistanceToNow, isAfter } from 'date-fns'
import { vi } from 'date-fns/locale'
import clsx from 'clsx'
import type { RoleName } from '@/types'
export { clsx }

export const fmtDate = (d?: string | null) =>
  d ? format(new Date(d), 'dd/MM/yyyy', { locale: vi }) : '—'

export const fmtDateTime = (d?: string | null) =>
  d ? format(new Date(d), 'HH:mm dd/MM/yyyy', { locale: vi }) : '—'

export const fmtRelative = (d?: string | null) =>
  d ? formatDistanceToNow(new Date(d), { addSuffix: true, locale: vi }) : '—'

export const isOverdue = (d?: string | null) =>
  d ? isAfter(new Date(), new Date(d)) : false

export const STATUS_LABELS: Record<string, string> = {
  Draft: 'Bản nháp',
  PendingDepartment: 'Chờ BM xác minh',
  PendingFaculty: 'Chờ Lãnh đạo duyệt',
  Approved: 'Đã phê duyệt',
  Issued: 'Đã ban hành',
  Distributed: 'Đã phân phối',
  Archived: 'Lưu trữ',
  Recalled: 'Đã thu hồi',
}

export const STATUS_BADGE_CLASS: Record<string, string> = {
  Draft: 'badge-draft',
  PendingDepartment: 'badge-pending',
  PendingFaculty: 'badge-pending',
  Approved: 'badge-approved',
  Issued: 'badge-issued',
  Distributed: 'badge-distributed',
  Archived: 'badge-archived',
  Recalled: 'badge-recalled',
}

export const TYPE_LABELS: Record<string, string> = {
  ToTrinh: 'Tờ trình',
  PhieuDeXuat: 'Phiếu đề xuất',
  BaoCao: 'Báo cáo',
  QuyetDinh: 'Quyết định',
  ThongBao: 'Thông báo',
  CongVan: 'Công văn',
}

export const TYPE_NEEDS_ISSUANCE = (type: string) =>
  ['QuyetDinh', 'ThongBao', 'CongVan'].includes(type)

export const ROLE_LABELS: Record<RoleName, string> = {
  GiangVien: 'Giảng viên',
  VanThuKhoa: 'Văn thư Khoa',
  TruongBoMon: 'Trưởng Bộ môn',
  LanhDaoKhoa: 'Lãnh đạo Khoa',
  Admin: 'Quản trị viên',
}

export const ROLE_BADGE_COLOR: Record<RoleName, string> = {
  GiangVien: 'bg-ink-100 text-ink-700',
  VanThuKhoa: 'bg-sky-50 text-sky-700',
  TruongBoMon: 'bg-amber-50 text-amber-700',
  LanhDaoKhoa: 'bg-jade-50 text-jade-700',
  Admin: 'bg-rose-50 text-rose-700',
}

export const NOTIF_ICON_COLOR: Record<string, string> = {
  DocumentSubmitted: 'text-amber-500',
  DocumentApproved: 'text-jade-500',
  DocumentRejected: 'text-rose-500',
  DocumentIssued: 'text-sky-500',
  DocumentDistributed: 'text-jade-500',
  OverdueWarning: 'text-rose-500',
  System: 'text-ink-400',
}

export const fmtFileSize = (bytes: number) => {
  if (bytes >= 1_048_576) return `${(bytes / 1_048_576).toFixed(1)} MB`
  if (bytes >= 1_024) return `${Math.round(bytes / 1_024)} KB`
  return `${bytes} B`
}

export const fileIcon = (contentType?: string) => {
  if (!contentType) return '📄'
  if (contentType.includes('pdf')) return '📕'
  if (contentType.includes('word') || contentType.includes('docx')) return '📘'
  if (contentType.includes('sheet') || contentType.includes('xlsx')) return '📗'
  if (contentType.includes('image')) return '🖼️'
  return '📄'
}

export const canSubmit = (role: RoleName) =>
  ['GiangVien', 'VanThuKhoa'].includes(role)

export const canApproveStep2 = (role: RoleName) =>
  ['TruongBoMon', 'Admin'].includes(role)

export const canApproveStep3 = (role: RoleName) =>
  ['LanhDaoKhoa', 'Admin'].includes(role)

export const canIssueNumber = (role: RoleName) =>
  ['VanThuKhoa', 'Admin'].includes(role)

export const canDistribute = (role: RoleName) =>
  ['VanThuKhoa', 'LanhDaoKhoa', 'Admin'].includes(role)

export const canViewStats = (role: RoleName) =>
  ['VanThuKhoa', 'LanhDaoKhoa', 'Admin'].includes(role)

export const canManageUsers = (role: RoleName) => role === 'Admin'

export const DOC_TYPE_OPTIONS = [
  { value: '0',  label: 'Tờ trình',      group: 'Không cần số hiệu' },
  { value: '1',  label: 'Phiếu đề xuất', group: 'Không cần số hiệu' },
  { value: '2',  label: 'Báo cáo',       group: 'Không cần số hiệu' },
  { value: '10', label: 'Quyết định',    group: 'Cần cấp số hiệu' },
  { value: '11', label: 'Thông báo',     group: 'Cần cấp số hiệu' },
  { value: '12', label: 'Công văn',      group: 'Cần cấp số hiệu' },
]
