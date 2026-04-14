import api from './client'
import type {
  LoginResponse, AuthUser, VanBanTomTat, VanBanChiTiet,
  KetQuaPhanTrang, Dashboard, ThongKe, ThongBao, BoMon, NguoiDung, SoHieu,
} from '@/types'

export const authAPI = {
  dangNhap: (email: string, matKhau: string) =>
    api.post<LoginResponse>('/auth/dang-nhap', { email, matKhau }),
  dangXuat: () => api.post('/auth/dang-xuat'),
  doiMatKhau: (matKhauHienTai: string, matKhauMoi: string) =>
    api.post('/auth/doi-mat-khau', { matKhauHienTai, matKhauMoi }),
  quenMatKhau: (email: string) =>
    api.post('/auth/quen-mat-khau', { email }),
}

export const vanBanAPI = {
  timKiem: (params: Record<string, unknown>) =>
    api.get<KetQuaPhanTrang<VanBanTomTat>>('/van-ban', { params }),
  layChiTiet: (id: number) =>
    api.get<VanBanChiTiet>(`/van-ban/${id}`),
  tao: (formData: FormData) =>
    api.post<VanBanChiTiet>('/van-ban', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }),
  chinhSua: (id: number, formData: FormData) =>
    api.put<VanBanChiTiet>(`/van-ban/${id}/chinh-sua`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }),
  xoa: (id: number) => api.delete(`/van-ban/${id}`),
  taiXuong: (id: number, phienBanId?: number) =>
    api.get(`/van-ban/${id}/tai-xuong`, {
      params: phienBanId ? { phienBanId } : {},
      responseType: 'blob',
    }),
  layDashboard: () => api.get<Dashboard>('/van-ban/dashboard'),
}

export const workflowAPI = {
  nop: (vanBanId: number, ghiChu?: string) =>
    api.post('/workflow/nop', { vanBanId, ghiChu }),
  xacMinhChuyenMon: (vanBanId: number, yKien?: string) =>
    api.post('/workflow/xac-minh-chuyen-mon', { vanBanId, yKien }),
  tuChoiBoMon: (vanBanId: number, lyDoTuChoi: string) =>
    api.post('/workflow/tu-choi-bo-mon', { vanBanId, lyDoTuChoi }),
  pheDuyetCuoi: (vanBanId: number, yKien?: string) =>
    api.post('/workflow/phe-duyet-cuoi', { vanBanId, yKien }),
  tuChoiKhoa: (vanBanId: number, lyDoTuChoi: string) =>
    api.post('/workflow/tu-choi-khoa', { vanBanId, lyDoTuChoi }),
  nopLai: (vanBanId: number, ghiChu?: string) =>
    api.post('/workflow/nop-lai', { vanBanId, ghiChu }),
  layLichSu: (vanBanId: number) =>
    api.get(`/workflow/${vanBanId}/lich-su`),
}

export const soHieuAPI = {
  capSo: (data: {
    vanBanId: number
    soHieuTuyChinh?: string
    nguoiKy?: string
    chucVuKy?: string
    ngayHieuLuc?: string
  }) => api.post<SoHieu>('/so-hieu/cap-so', data),
  xemTruoc: (vanBanId: number) =>
    api.get<{ soHieu: string }>(`/so-hieu/xem-truoc/${vanBanId}`),
}

export const phanPhoiAPI = {
  phanPhoi: (vanBanId: number, danhSachNguoiNhanIds: number[]) =>
    api.post('/phan-phoi', { vanBanId, danhSachNguoiNhanIds }),
  danhDauDaDoc: (vanBanId: number) =>
    api.put(`/phan-phoi/${vanBanId}/da-doc`),
  xacNhanTiepNhan: (vanBanId: number, ghiChu?: string) =>
    api.post('/phan-phoi/xac-nhan-tiep-nhan', { vanBanId, ghiChu }),
  hopThuDen: (params?: { chuaDocThoi?: boolean; trang?: number; kichThuoc?: number }) =>
    api.get<KetQuaPhanTrang<VanBanTomTat>>('/phan-phoi/hop-thu-den', { params }),
}

export const thongBaoAPI = {
  layDanhSach: (chuaDocThoi = false) =>
    api.get<ThongBao[]>('/thong-bao', { params: { chuaDocThoi } }),
  demChuaDoc: () => api.get<{ soChuaDoc: number }>('/thong-bao/so-chua-doc'),
  danhDauDaDoc: (id: number) => api.put(`/thong-bao/${id}/da-doc`),
  danhDauTatCaDaDoc: () => api.put('/thong-bao/da-doc-tat-ca'),
}

export const nguoiDungAPI = {
  layToi: () => api.get<AuthUser>('/nguoi-dung/toi'),
  layDanhSach: (params?: { boMonId?: number; role?: string; activeOnly?: boolean; trang?: number; kichThuoc?: number }) =>
    api.get<KetQuaPhanTrang<NguoiDung>>('/nguoi-dung', { params }),
  capNhatHoSo: (data: { hoTen: string; chucDanh?: string; soDienThoai?: string }) =>
    api.put<AuthUser>('/nguoi-dung/toi', data),
  capNhatRole: (data: { nguoiDungId: number; roleId: number; boMonId?: number }) =>
    api.put('/nguoi-dung/cap-nhat-role', data),
  khoaTaiKhoan: (nguoiDungId: number, isActive: boolean) =>
    api.put('/nguoi-dung/khoa-tai-khoan', { nguoiDungId, isActive }),
}

export const boMonAPI = {
  layDanhSach: (activeOnly = true) => api.get<BoMon[]>('/bo-mon', { params: { activeOnly } }),
  tao: (data: { ten: string; maBoMon?: string; truongBoMonId?: number }) =>
    api.post<BoMon>('/bo-mon', data),
  capNhat: (id: number, data: { ten: string; maBoMon?: string; truongBoMonId?: number }) =>
    api.put<BoMon>(`/bo-mon/${id}`, data),
  xoa: (id: number) => api.delete(`/bo-mon/${id}`),
}

export const thongKeAPI = {
  lay: (params?: { tuNgay?: string; denNgay?: string; boMonId?: number }) =>
    api.get<ThongKe>('/thong-ke', { params }),
  layQuaHan: () => api.get<VanBanTomTat[]>('/thong-ke/qua-han'),
}
