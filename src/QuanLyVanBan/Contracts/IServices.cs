using QuanLyVanBan.DTOs.Requests;
using QuanLyVanBan.DTOs.Responses;
using QuanLyVanBan.Models.Enums;

namespace QuanLyVanBan.Contracts;

public interface IAuthService
{
    Task<DangNhapResponse> DangNhapAsync(DangNhapRequest req, string ipAddress);
    Task<DangNhapResponse> DangKyAsync(DangKyRequest req);
    Task<DangNhapResponse> LamMoiTokenAsync(string refreshToken, string ipAddress);
    Task DangXuatAsync(int nguoiDungId);
    Task<bool> DoiMatKhauAsync(int nguoiDungId, DoiMatKhauRequest req);
    Task<bool> QuenMatKhauAsync(QuenMatKhauRequest req);
    Task<bool> DatLaiMatKhauAsync(DatLaiMatKhauRequest req);
}

public interface IVanBanService
{
    /// <summary>Bước 1: Tạo văn bản mới (với file bắt buộc)</summary>
    Task<VanBanChiTietResponse> TaoAsync(TaoVanBanRequest req, int nguoiTaoId);

    /// <summary>Lấy chi tiết văn bản (có kiểm tra quyền xem)</summary>
    Task<VanBanChiTietResponse?> LayChiTietAsync(int id, int nguoiYeuCauId);

    /// <summary>Tìm kiếm + phân trang (có lọc theo role)</summary>
    Task<KetQuaPhanTrang<VanBanTomTatResponse>> TimKiemAsync(TimKiemVanBanRequest req, int nguoiYeuCauId);

    /// <summary>
    /// Chỉnh sửa văn bản (Bước 4 - sau khi bị từ chối).
    /// Chỉ cho phép khi TrangThai = Draft VÀ người yêu cầu = NguoiTao.
    /// Upload phiên bản mới (v2, v3...) – KHÔNG xóa file cũ.
    /// </summary>
    Task<VanBanChiTietResponse> ChinhSuaAsync(int id, ChinhSuaVanBanRequest req, int nguoiYeuCauId);

    /// <summary>Xóa văn bản - chỉ khi Draft và là người tạo</summary>
    Task XoaAsync(int id, int nguoiYeuCauId);

    /// <summary>Tải xuống file (kiểm tra quyền)</summary>
    Task<(byte[] DuLieu, string TenFile, string ContentType)> TaiXuongAsync(int vanBanId, int? phienBanId, int nguoiYeuCauId);

    Task<DashboardResponse> LayDashboardAsync(int nguoiDungId);
}

public interface IWorkflowService
{
    /// <summary>
    /// Bước 2: Nộp văn bản để duyệt.
    /// Logic phân cấp: 
    ///   - GiangVien → BuocBiTuChoiCuoi == "Faculty" ? Bỏ qua Bước 2, thẳng lên Bước 3
    ///                                                : → PendingDepartment (Bước 2)
    ///   - VanThuKhoa → BỎ QUA Bước 2 hoàn toàn → thẳng PendingFaculty (Bước 3)
    ///     (Giải quyết vấn đề "xung đột vai trò và phân cấp" Bước 1 & 2)
    /// </summary>
    Task<string> NopAsync(NopVanBanRequest req, int nguoiDungId);

    /// <summary>
    /// Bước 2: Trưởng BM xác minh chuyên môn.
    /// Nếu đạt → PendingFaculty. Validate: chỉ TruongBoMon của BoMon sở hữu văn bản mới được duyệt.
    /// </summary>
    Task<string> XacMinhChuyenMonAsync(PheDuyetRequest req, int truongBMId);

    /// <summary>
    /// Bước 2: Trưởng BM từ chối → Draft. LyDo bắt buộc.
    /// Lưu BuocBiTuChoiCuoi = "Department" để biết nộp lại thì quay về Bước 2.
    /// </summary>
    Task<string> TuChoiBMAsync(TuChoiRequest req, int truongBMId);

    /// <summary>
    /// Bước 3: Lãnh đạo Khoa phê duyệt cuối.
    /// → Approved. IsLocked = true (KHÔNG ai được sửa file sau bước này).
    /// Nếu LoaiVanBan cần số hiệu → thông báo VanThuKhoa vào cấp số.
    /// Nếu không cần số hiệu → thông báo VanThuKhoa có thể phân phối.
    /// </summary>
    Task<string> PheDuyetCuoiAsync(PheDuyetRequest req, int lanhDaoId);

    /// <summary>
    /// Bước 3: Lãnh đạo Khoa từ chối → Draft. LyDo bắt buộc.
    /// Lưu BuocBiTuChoiCuoi = "Faculty" → nộp lại sẽ bỏ qua Bước 2, thẳng lên Bước 3.
    /// (Giải quyết vấn đề "kẹt luồng deadlock": sau khi sửa biết quay về đâu)
    /// </summary>
    Task<string> TuChoiKhoaAsync(TuChoiRequest req, int lanhDaoId);

    /// <summary>
    /// Nộp lại sau khi chỉnh sửa.
    /// Tự động định tuyến dựa vào BuocBiTuChoiCuoi:
    ///   "Department" → PendingDepartment (Bước 2)
    ///   "Faculty"    → PendingFaculty    (Bước 3)
    /// </summary>
    Task<string> NopLaiAsync(NopLaiRequest req, int nguoiDungId);

    Task<List<NhatKyWorkflowResponse>> LayLichSuAsync(int vanBanId);
}

public interface ISoHieuService
{
    /// <summary>
    /// Bước 5: Cấp số hiệu chính thức (chỉ VanThuKhoa).
    /// Validate: TrangThai phải = Approved VÀ LoaiVanBan phải cần số hiệu.
    /// Tự động sinh số nếu không nhập tay.
    /// </summary>
    Task<SoHieuResponse> CapSoAsync(CapSoHieuRequest req, int vanThuId);

    /// <summary>Preview số hiệu sẽ được cấp (chưa lưu DB)</summary>
    Task<string> XemTruocSoHieuAsync(int vanBanId);

    Task<SoHieuResponse?> LayThongTinSoHieuAsync(int vanBanId);
}

public interface IPhanPhoiService
{
    /// <summary>
    /// Bước 6: Phân phối + lưu trữ.
    /// Validate: TrangThai phải = Approved hoặc Issued.
    /// Ghi nhận DanhSachNguoiNhan để theo dõi Read Receipt.
    /// </summary>
    Task<string> PhanPhoiAsync(PhanPhoiVanBanRequest req, int nguoiPhanPhoiId);

    /// <summary>Người nhận mở văn bản → tự động đánh dấu Đã đọc</summary>
    Task DanhDauDaDocAsync(int vanBanId, int nguoiNhanId);

    /// <summary>Người nhận bấm "Xác nhận tiếp nhận" – có trách nhiệm pháp lý</summary>
    Task<string> XacNhanTiepNhanAsync(XacNhanTiepNhanRequest req, int nguoiNhanId);

    Task<List<NguoiNhanResponse>> LayDanhSachNguoiNhanAsync(int vanBanId);

    /// <summary>Văn bản được phân phối đến tôi (có lọc chưa đọc)</summary>
    Task<KetQuaPhanTrang<VanBanTomTatResponse>> LayVanBanDuocPhanPhoiAsync(int nguoiDungId, bool chuaDocThoi = false, int trang = 1, int kichThuoc = 20);
}

public interface INguoiDungService
{
    Task<NguoiDungResponse?> LayTheoIdAsync(int id);
    Task<KetQuaPhanTrang<NguoiDungResponse>> LayDanhSachAsync(int? boMonId, RoleName? role, bool activeOnly, int trang, int kichThuoc);
    Task<NguoiDungResponse> CapNhatHoSoAsync(int nguoiDungId, CapNhatHoSoRequest req);
    Task<NguoiDungResponse> CapNhatRoleAsync(CapNhatRoleRequest req, int adminId);
    Task KhoaTaiKhoanAsync(KhoaTaiKhoanRequest req, int adminId);
}

public interface IBoMonService
{
    Task<List<BoMonResponse>> LayDanhSachAsync(bool activeOnly = true);
    Task<BoMonResponse?> LayTheoIdAsync(int id);
    Task<BoMonResponse> TaoAsync(TaoBoMonRequest req);
    Task<BoMonResponse> CapNhatAsync(int id, TaoBoMonRequest req);
    Task XoaAsync(int id);
}

public interface IThongKeService
{
    Task<ThongKeResponse> LayThongKeAsync(DateTime tuNgay, DateTime denNgay, int? boMonId = null);
    Task<List<VanBanTomTatResponse>> LayVanBanQuaHanAsync();
}

public interface IThongBaoService
{
    Task GuiAsync(int nguoiNhanId, string tieuDe, string noiDung,
        NotificationType loai = NotificationType.System, int? vanBanId = null);

    Task GuiTheoRoleAsync(RoleName role, int? boMonId, string tieuDe, string noiDung, NotificationType loai);

    Task<List<ThongBaoResponse>> LayThongBaoCuaTuiAsync(int nguoiDungId, bool chuaDocThoi = false);
    Task DanhDauDaDocAsync(int thongBaoId, int nguoiDungId);
    Task DanhDauTatCaDaDocAsync(int nguoiDungId);
    Task<int> DemChuaDocAsync(int nguoiDungId);
}

public interface IAuditService
{
    Task GhiAsync(int? nguoiDungId, string hanhDong, string? loaiDoiTuong = null,
        int? doiTuongId = null, string? giaTriCu = null, string? giaTriMoi = null,
        string? ipAddress = null);
}
