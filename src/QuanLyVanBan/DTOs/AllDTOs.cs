using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using QuanLyVanBan.Models.Enums;

// ═══════════════════════════════════════════════════════════════════
// REQUEST DTOs
// ═══════════════════════════════════════════════════════════════════

namespace QuanLyVanBan.DTOs.Requests
{

// ── Auth ──────────────────────────────────────────────────────────

public record DangNhapRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string MatKhau
);

public record DangKyRequest(
    [Required, MaxLength(200)] string HoTen,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(8)] string MatKhau,
    int? BoMonId,
    string? ChucDanh,
    string? SoDienThoai
);

public record DoiMatKhauRequest(
    [Required] string MatKhauHienTai,
    [Required, MinLength(8)] string MatKhauMoi
);

public record QuenMatKhauRequest([Required, EmailAddress] string Email);

public record DatLaiMatKhauRequest(
    [Required] string Token,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string MatKhauMoi
);

public record LamMoiTokenRequest([Required] string RefreshToken);

// ── Văn bản ───────────────────────────────────────────────────────

/// <summary>Tạo văn bản mới - dùng [FromForm] vì có file</summary>
public class TaoVanBanRequest
{
    [Required(ErrorMessage = "Tiêu đề không được trống")]
    [MaxLength(500)]
    public string TieuDe { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? MoTa { get; set; }

    [Required]
    public DocumentType LoaiVanBan { get; set; }

    /// <summary>File bản thảo - bắt buộc khi tạo</summary>
    [Required(ErrorMessage = "Phải đính kèm file bản thảo")]
    public IFormFile File { get; set; } = null!;

    public DateTime? HanXuLy { get; set; }
}

/// <summary>Chỉnh sửa văn bản (Bước 4 - sau khi bị từ chối)</summary>
public class ChinhSuaVanBanRequest
{
    [MaxLength(500)]
    public string? TieuDe { get; set; }

    [MaxLength(2000)]
    public string? MoTa { get; set; }

    /// <summary>File phiên bản mới - bắt buộc khi chỉnh sửa</summary>
    [Required(ErrorMessage = "Phải đính kèm file phiên bản mới")]
    public IFormFile File { get; set; } = null!;

    /// <summary>Nhật ký chỉnh sửa: mô tả đã sửa gì so với phiên bản trước</summary>
    [Required(ErrorMessage = "Phải ghi rõ đã chỉnh sửa gì")]
    [MaxLength(500)]
    public string GhiChuChinhSua { get; set; } = string.Empty;
}

public class TimKiemVanBanRequest
{
    public string? TuKhoa { get; set; }               // Tìm theo tiêu đề / trích yếu
    public DocumentStatus? TrangThai { get; set; }
    public DocumentType? LoaiVanBan { get; set; }
    public int? NguoiTaoId { get; set; }
    public int? BoMonId { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public string? SoHieu { get; set; }               // Tìm theo số hiệu ban hành
    public int Trang { get; set; } = 1;
    public int KichThuocTrang { get; set; } = 20;
    public string SapXepTheo { get; set; } = "NgayTao";
    public bool GiamDan { get; set; } = true;
}

// ── Workflow ──────────────────────────────────────────────────────

public record NopVanBanRequest(
    [Required] int VanBanId,
    string? GhiChu
);

public record PheDuyetRequest(
    [Required] int VanBanId,
    string? YKien  // Không bắt buộc khi duyệt
);

/// <summary>Từ chối PHẢI có lý do - giải quyết vấn đề "từ chối không nêu lý do"</summary>
public record TuChoiRequest(
    [Required] int VanBanId,
    [Required(ErrorMessage = "Phải ghi rõ lý do từ chối"), MaxLength(1000)]
    string LyDoTuChoi
);

public record NopLaiRequest(
    [Required] int VanBanId,
    string? GhiChu
);

// ── Cấp số hiệu (Bước 5) ─────────────────────────────────────────

public class CapSoHieuRequest
{
    [Required]
    public int VanBanId { get; set; }

    /// <summary>Để trống → hệ thống tự sinh. Nhập tay → validate không trùng</summary>
    [MaxLength(50)]
    public string? SoHieuTuyChinh { get; set; }

    [MaxLength(200)]
    public string? NguoiKy { get; set; }

    [MaxLength(100)]
    public string? ChucVuKy { get; set; }

    public DateTime? NgayHieuLuc { get; set; }
    public DateTime? NgayHetHieuLuc { get; set; }
}

// ── Phân phối (Bước 6) ───────────────────────────────────────────

public class PhanPhoiVanBanRequest
{
    [Required]
    public int VanBanId { get; set; }

    [Required, MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 người nhận")]
    public List<int> DanhSachNguoiNhanIds { get; set; } = new();
}

public record XacNhanTiepNhanRequest(
    [Required] int VanBanId,
    [MaxLength(500)] string? GhiChu
);

// ── Người dùng ────────────────────────────────────────────────────

public class CapNhatHoSoRequest
{
    [Required, MaxLength(200)]
    public string HoTen { get; set; } = string.Empty;
    [MaxLength(100)] public string? ChucDanh { get; set; }
    [MaxLength(20)] public string? SoDienThoai { get; set; }
}

public record CapNhatRoleRequest(
    [Required] int NguoiDungId,
    [Required] int RoleId,
    int? BoMonId
);

public record KhoaTaiKhoanRequest([Required] int NguoiDungId, bool IsActive);

// ── Bộ môn ────────────────────────────────────────────────────────

public class TaoBoMonRequest
{
    [Required, MaxLength(200)] public string Ten { get; set; } = string.Empty;
    [MaxLength(20)] public string? MaBoMon { get; set; }
    [MaxLength(500)] public string? MoTa { get; set; }
    public int? TruongBoMonId { get; set; }
}
}

// ═══════════════════════════════════════════════════════════════════
// RESPONSE DTOs
// ═══════════════════════════════════════════════════════════════════

namespace QuanLyVanBan.DTOs.Responses
{

// ── Common ────────────────────────────────────────────────────────

public class KetQuaPhanTrang<T>
{
    public List<T> DuLieu { get; set; } = new();
    public int TongSoBanGhi { get; set; }
    public int Trang { get; set; }
    public int KichThuocTrang { get; set; }
    public int TongSoTrang => (int)Math.Ceiling((double)TongSoBanGhi / KichThuocTrang);
    public bool CoTrangTruoc => Trang > 1;
    public bool CoTrangSau => Trang < TongSoTrang;
}

// ── Auth ──────────────────────────────────────────────────────────

public class DangNhapResponse
{
    public bool ThanhCong { get; set; }
    public string? ThongBao { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? HetHanLuc { get; set; }
    public NguoiDungResponse? NguoiDung { get; set; }
}

// ── Người dùng ────────────────────────────────────────────────────

public class NguoiDungResponse
{
    public int Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    public string? SoDienThoai { get; set; }
    public string Role { get; set; } = string.Empty;          // "GiangVien"
    public string RoleTenHienThi { get; set; } = string.Empty; // "Giảng viên"
    public int? BoMonId { get; set; }
    public string? TenBoMon { get; set; }
    public bool IsActive { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }
}

public class BoMonResponse
{
    public int Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? MaBoMon { get; set; }
    public int? TruongBoMonId { get; set; }
    public string? TenTruongBoMon { get; set; }
    public bool IsActive { get; set; }
    public int SoThanhVien { get; set; }
}

// ── Văn bản ───────────────────────────────────────────────────────

public class VanBanTomTatResponse
{
    public int Id { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string LoaiVanBan { get; set; } = string.Empty;
    public string LoaiVanBanHienThi { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string TrangThaiHienThi { get; set; } = string.Empty;
    public bool CanCapSoHieu { get; set; }   // true nếu loại cần số hiệu
    public bool IsLocked { get; set; }
    public string? SoHieu { get; set; }       // Số hiệu sau khi cấp (nếu có)
    public string? TenNguoiTao { get; set; }
    public string? TenBoMon { get; set; }
    public int SoPhienBan { get; set; }
    public DateTime? HanXuLy { get; set; }
    public bool QuaHan { get; set; }          // true nếu HanXuLy < Now và chưa xong
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

public class VanBanChiTietResponse : VanBanTomTatResponse
{
    public string? MoTa { get; set; }
    public List<PhienBanFileResponse> PhienBans { get; set; } = new();
    public List<BuocPheDuyetResponse> BuocPheDuyets { get; set; } = new();
    public List<NhatKyWorkflowResponse> LichSuXuLy { get; set; } = new();
    public SoHieuResponse? ThongTinSoHieu { get; set; }
    public List<NguoiNhanResponse> DanhSachNguoiNhan { get; set; } = new();
}

public class PhienBanFileResponse
{
    public int Id { get; set; }
    public int SoPhienBan { get; set; }
    public string TenFile { get; set; } = string.Empty;
    public long KichThuocBytes { get; set; }
    public string KichThuocHienThi { get; set; } = string.Empty; // "1.2 MB"
    public string? ContentType { get; set; }
    public string? GhiChuChinhSua { get; set; }
    public string? TenNguoiUpload { get; set; }
    public DateTime NgayUpload { get; set; }
}

// ── Workflow ──────────────────────────────────────────────────────

public class BuocPheDuyetResponse
{
    public int Id { get; set; }
    public int ThuTu { get; set; }
    public string TenBuoc { get; set; } = string.Empty;
    public string RoleYeuCau { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string TrangThaiHienThi { get; set; } = string.Empty;
    public string? TenNguoiDuocGiao { get; set; }
    public string? TenNguoiXuLy { get; set; }
    public string? YKien { get; set; }
    public DateTime? NgayXuLy { get; set; }
    public DateTime? HanXuLy { get; set; }
    public bool QuaHan { get; set; }
}

public class NhatKyWorkflowResponse
{
    public int Id { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public string HanhDongHienThi { get; set; } = string.Empty;
    public string TrangThaiTu { get; set; } = string.Empty;
    public string TrangThaiDen { get; set; } = string.Empty;
    public string TenNguoiThucHien { get; set; } = string.Empty;
    public string RoleNguoiThucHien { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public DateTime ThoiGian { get; set; }
}

// ── Cấp số / Phân phối ───────────────────────────────────────────

public class SoHieuResponse
{
    public int Id { get; set; }
    public string SoHieu { get; set; } = string.Empty;
    public string? NguoiKy { get; set; }
    public string? ChucVuKy { get; set; }
    public string TenNguoiCapSo { get; set; } = string.Empty;
    public DateTime NgayCapSo { get; set; }
    public DateTime? NgayHieuLuc { get; set; }
    public DateTime? NgayHetHieuLuc { get; set; }
}

public class NguoiNhanResponse
{
    public int NguoiNhanId { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool DaDoc { get; set; }
    public DateTime? NgayDoc { get; set; }
    public bool DaTiepNhan { get; set; }
    public DateTime? NgayTiepNhan { get; set; }
    public DateTime NgayPhanPhoi { get; set; }
}

// ── Thông báo ─────────────────────────────────────────────────────

public class ThongBaoResponse
{
    public int Id { get; set; }
    public string LoaiThongBao { get; set; } = string.Empty;
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public int? VanBanId { get; set; }
    public bool DaDoc { get; set; }
    public DateTime NgayTao { get; set; }
}

// ── Thống kê ──────────────────────────────────────────────────────

public class ThongKeResponse
{
    public int TongVanBan { get; set; }
    public int BanNhap { get; set; }
    public int ChoTruongBMDuyet { get; set; }
    public int ChoLanhDaoDuyet { get; set; }
    public int DaDuyet { get; set; }
    public int DaBanHanh { get; set; }
    public int DaPhanPhoi { get; set; }
    public int BiBacBo { get; set; }           // Tổng số bị từ chối (ít nhất 1 lần)
    public int QuaHan { get; set; }            // Đang xử lý nhưng quá hạn HanXuLy

    public Dictionary<string, int> TheoLoaiVanBan { get; set; } = new();
    public Dictionary<string, int> TheoBoMon { get; set; } = new();
    public Dictionary<string, int> TheoThang { get; set; } = new();  // "2025-03" → số lượng

    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
}

public class DashboardResponse
{
    // Cho tất cả roles
    public int VanBanCuaToi { get; set; }
    public int ThongBaoChuaDoc { get; set; }
    public int VanBanDuocPhanPhoiChuaDoc { get; set; }

    // Cho GiangVien / VanThuKhoa
    public int VanBanChoXuLy { get; set; }  // Đang trong luồng duyệt
    public int VanBanBiTuChoi { get; set; } // Cần chỉnh sửa và nộp lại

    // Cho TruongBoMon
    public int ChoTuXacMinh { get; set; }   // Chờ mình duyệt Bước 2

    // Cho LanhDaoKhoa
    public int ChoTuPheDuyet { get; set; }  // Chờ mình duyệt Bước 3

    // Cho VanThuKhoa / Admin
    public int ChoCapSoHieu { get; set; }   // Approved nhưng chưa cấp số (loại cần số)
    public int QuaHan { get; set; }         // Cảnh báo ngâm hồ sơ

    public List<VanBanTomTatResponse> VanBanGanDay { get; set; } = new();
    public List<ThongBaoResponse> ThongBaoGanDay { get; set; } = new();
}
}
