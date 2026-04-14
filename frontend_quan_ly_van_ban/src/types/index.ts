export enum DocumentStatus {
  Draft = 0,
  PendingDepartment = 1,
  PendingFaculty = 2,
  Approved = 3,
  Issued = 4,
  Distributed = 5,
  Archived = 6,
  Recalled = 7,
}

export enum DocumentType {
  ToTrinh = 0,
  PhieuDeXuat = 1,
  BaoCao = 2,
  QuyetDinh = 10,
  ThongBao = 11,
  CongVan = 12,
}

export type RoleName = 'GiangVien' | 'VanThuKhoa' | 'TruongBoMon' | 'LanhDaoKhoa' | 'Admin'

export type NotificationType =
  | 'System' | 'DocumentSubmitted' | 'DocumentApproved'
  | 'DocumentRejected' | 'DocumentIssued' | 'DocumentDistributed' | 'OverdueWarning'

export interface AuthUser {
  id: number
  hoTen: string
  email: string
  chucDanh?: string
  soDienThoai?: string
  role: RoleName
  roleTenHienThi: string
  boMonId?: number
  tenBoMon?: string
  isActive: boolean
  ngayTao: string
  lanDangNhapCuoi?: string
}

export interface LoginResponse {
  thanhCong: boolean
  thongBao?: string
  accessToken?: string
  refreshToken?: string
  hetHanLuc?: string
  nguoiDung?: AuthUser
}

export interface PhienBanFile {
  id: number
  soPhienBan: number
  tenFile: string
  kichThuocBytes: number
  kichThuocHienThi: string
  donVi: string
  contentType?: string
  ghiChuChinhSua?: string
  tenNguoiUpload?: string
  ngayUpload: string
}

export interface BuocPheDuyet {
  id: number
  thuTu: number
  tenBuoc: string
  roleYeuCau: string
  trangThai: string
  trangThaiHienThi: string
  tenNguoiDuocGiao?: string
  tenNguoiXuLy?: string
  yKien?: string
  ngayXuLy?: string
  hanXuLy?: string
  quaHan: boolean
}

export interface NhatKyWorkflow {
  id: number
  hanhDong: string
  hanhDongHienThi: string
  trangThaiTu: string
  trangThaiDen: string
  tenNguoiThucHien: string
  roleNguoiThucHien: string
  ghiChu?: string
  thoiGian: string
}

export interface SoHieu {
  id: number
  soHieu: string
  nguoiKy?: string
  chucVuKy?: string
  tenNguoiCapSo: string
  ngayCapSo: string
  ngayHieuLuc?: string
  ngayHetHieuLuc?: string
}

export interface NguoiNhan {
  nguoiNhanId: number
  hoTen: string
  email?: string
  daDoc: boolean
  ngayDoc?: string
  daTiepNhan: boolean
  ngayTiepNhan?: string
  ngayPhanPhoi: string
}

export interface VanBanTomTat {
  id: number
  tieuDe: string
  loaiVanBan: string
  loaiVanBanHienThi: string
  trangThai: string
  trangThaiHienThi: string
  canCapSoHieu: boolean
  isLocked: boolean
  soHieu?: string
  tenNguoiTao?: string
  tenBoMon?: string
  soPhienBan: number
  hanXuLy?: string
  quaHan: boolean
  ngayTao: string
  ngayCapNhat?: string
}

export interface VanBanChiTiet extends VanBanTomTat {
  moTa?: string
  phienBans: PhienBanFile[]
  buocPheDuyets: BuocPheDuyet[]
  lichSuXuLy: NhatKyWorkflow[]
  thongTinSoHieu?: SoHieu
  danhSachNguoiNhan: NguoiNhan[]
}

export interface ThongBao {
  id: number
  loaiThongBao: NotificationType
  tieuDe: string
  noiDung: string
  vanBanId?: number
  daDoc: boolean
  ngayTao: string
}

export interface Dashboard {
  vanBanCuaToi: number
  thongBaoChuaDoc: number
  vanBanDuocPhanPhoiChuaDoc: number
  vanBanChoXuLy: number
  vanBanBiTuChoi: number
  choTuXacMinh: number
  choTuPheDuyet: number
  choCapSoHieu: number
  quaHan: number
  vanBanGanDay: VanBanTomTat[]
  thongBaoGanDay: ThongBao[]
}

export interface ThongKe {
  tongVanBan: number
  banNhap: number
  choTruongBMDuyet: number
  choLanhDaoDuyet: number
  daDuyet: number
  daBanHanh: number
  daPhanPhoi: number
  biBacBo: number
  quaHan: number
  theoLoaiVanBan: Record<string, number>
  theoBoMon: Record<string, number>
  theoThang: Record<string, number>
  tuNgay: string
  denNgay: string
}

export interface BoMon {
  id: number
  ten: string
  maBoMon?: string
  truongBoMonId?: number
  tenTruongBoMon?: string
  isActive: boolean
  soThanhVien: number
}

export interface NguoiDung {
  id: number
  hoTen: string
  email: string
  chucDanh?: string
  soDienThoai?: string
  role: RoleName
  roleTenHienThi: string
  boMonId?: number
  tenBoMon?: string
  isActive: boolean
  ngayTao: string
  lanDangNhapCuoi?: string
}

export interface KetQuaPhanTrang<T> {
  duLieu: T[]
  tongSoBanGhi: number
  trang: number
  kichThuocTrang: number
  tongSoTrang: number
  coTrangTruoc: boolean
  coTrangSau: boolean
}

export type ApiResponse<T> = {
  thanhCong: boolean
  thongBao?: string
  data?: T
}
