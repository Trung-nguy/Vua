using QuanLyVanBan.Models.Enums;

namespace QuanLyVanBan.Models;

// ═══════════════════════════════════════════════════════════════════
// ROLE & DEPARTMENT
// ═══════════════════════════════════════════════════════════════════

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;         // "GiangVien", "VanThuKhoa"...
    public string TenHienThi { get; set; } = string.Empty;   // "Giảng viên", "Văn thư Khoa"...
}

public class BoMon // Bộ môn / Phòng ban
{
    public int Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? MaBoMon { get; set; }    // VD: "CNTT", "KTDD"

    // Trưởng Bộ môn – người có quyền xác minh chuyên môn (Bước 2)
    public int? TruongBoMonId { get; set; }
    public NguoiDung? TruongBoMon { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════
// NGƯỜI DÙNG
// ═══════════════════════════════════════════════════════════════════

public class NguoiDung
{
    public int Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }       // ThS., TS., PGS.TS.
    public string? SoDienThoai { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    // Giảng viên thuộc Bộ môn; VanThuKhoa thuộc cấp Khoa (DepartmentId = null hoặc = VP Khoa)
    public int? BoMonId { get; set; }
    public BoMon? BoMon { get; set; }

    // Bảo mật tài khoản
    public bool IsActive { get; set; } = true;
    public int SoLanDangNhapSai { get; set; } = 0;
    public DateTime? KhoaTaiKhoanDen { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenHetHan { get; set; }
    public string? TokenDatLaiMatKhau { get; set; }
    public DateTime? TokenDatLaiHetHan { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
// VĂN BẢN - thực thể trung tâm
// ═══════════════════════════════════════════════════════════════════

public class VanBan
{
    public int Id { get; set; }
    public string TieuDe { get; set; } = string.Empty;         // Trích yếu nội dung
    public string? MoTa { get; set; }

    public DocumentType LoaiVanBan { get; set; }
    public DocumentStatus TrangThai { get; set; } = DocumentStatus.Draft;

    // Người tạo (GiangVien hoặc VanThuKhoa)
    public int NguoiTaoId { get; set; }
    public NguoiDung NguoiTao { get; set; } = null!;

    // Bộ môn sở hữu văn bản – dùng để xác định Trưởng BM nào duyệt Bước 2
    public int? BoMonId { get; set; }
    public BoMon? BoMon { get; set; }

    // Kiểm soát toàn vẹn
    // IsLocked = true SAU KHI Lãnh đạo phê duyệt (Bước 3) → không ai được sửa file nữa
    public bool IsLocked { get; set; } = false;

    // Theo dõi lịch sử từ chối để WorkflowService biết nộp lại phải về Bước 2 hay Bước 3
    // Lưu bước bị từ chối lần cuối: "Department" | "Faculty" | null
    public string? BuocBiTuChoiCuoi { get; set; }

    // Thời hạn xử lý (chống "ngâm" hồ sơ)
    public DateTime? HanXuLy { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }

    // Navigation
    public List<PhienBanFile> PhienBans { get; set; } = new();
    public List<BuocPheDuyet> BuocPheDuyets { get; set; } = new();
    public List<NhatKyWorkflow> NhatKyWorkflows { get; set; } = new();
    public SoHieuVanBan? SoHieu { get; set; }
    public List<NguoiNhanVanBan> NguoiNhans { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════
// PHIÊN BẢN FILE - Version Control (chống xóa file cũ / ghi đè)
// ═══════════════════════════════════════════════════════════════════
// Nghiệp vụ: Khi bị từ chối → người tạo sửa → upload v2, v3...
// Không được xóa phiên bản cũ. File bị lock sau khi Lãnh đạo duyệt.

public class PhienBanFile
{
    public int Id { get; set; }

    public int VanBanId { get; set; }
    public VanBan VanBan { get; set; } = null!;

    public string TenFile { get; set; } = string.Empty;        // Tên file gốc của người dùng
    public string DuongDanLuuTru { get; set; } = string.Empty; // Đường dẫn vật lý (server)
    public string? ContentType { get; set; }
    public long KichThuocBytes { get; set; }

    // Kiểm tra toàn vẹn – chống ghi đè lén lút sau khi duyệt
    public string? ChecksumMD5 { get; set; }

    public int SoPhienBan { get; set; }   // 1, 2, 3...
    public string? GhiChuChinhSua { get; set; } // Nhật ký chỉnh sửa (v2, v3...)

    public int NguoiUploadId { get; set; }
    public NguoiDung NguoiUpload { get; set; } = null!;

    public DateTime NgayUpload { get; set; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════
// BƯỚC PHÊ DUYỆT - tự động sinh khi văn bản được nộp
// ═══════════════════════════════════════════════════════════════════
// Giải quyết vấn đề "kẹt luồng": hệ thống biết chính xác đang ở bước nào,
// ai phải xử lý, và sau khi nộp lại thì quay về bước nào.

public class BuocPheDuyet
{
    public int Id { get; set; }

    public int VanBanId { get; set; }
    public VanBan VanBan { get; set; } = null!;

    public int ThuTu { get; set; }           // 1 = Trưởng BM, 2 = Lãnh đạo Khoa
    public string TenBuoc { get; set; } = string.Empty;
    public RoleName RoleYeuCau { get; set; } // Role được phép xử lý bước này

    // Người được giao cụ thể (tự động điền từ Trưởng BM của Bộ môn)
    public int? NguoiDuocGiaoId { get; set; }
    public NguoiDung? NguoiDuocGiao { get; set; }

    public ApprovalStepStatus TrangThai { get; set; } = ApprovalStepStatus.Waiting;

    // Ai đã thực hiện hành động
    public int? NguoiXuLyId { get; set; }
    public NguoiDung? NguoiXuLy { get; set; }

    // Lý do từ chối PHẢI được nhập (bắt buộc theo nghiệp vụ)
    public string? YKien { get; set; }

    public DateTime? NgayXuLy { get; set; }

    // Hạn phải xử lý (chống "ngâm" hồ sơ)
    public DateTime? HanXuLy { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════
// NHẬT KÝ WORKFLOW - Audit Trail đầy đủ
// ═══════════════════════════════════════════════════════════════════
// Ghi nhận: ai tạo, ai duyệt, ai sửa, vào thời gian nào, từ trạng thái nào sang trạng thái nào

public class NhatKyWorkflow
{
    public int Id { get; set; }

    public int VanBanId { get; set; }
    public VanBan VanBan { get; set; } = null!;

    public int NguoiThucHienId { get; set; }
    public NguoiDung NguoiThucHien { get; set; } = null!;

    public string HanhDong { get; set; } = string.Empty;      // "Tạo", "NộpDuyệt", "DuyệtBM", "TừChốiBM", "DuyệtKhoa", "TừChốiKhoa", "NộpLại", "CấpSố", "PhânPhối"
    public DocumentStatus TrangThaiTu { get; set; }
    public DocumentStatus TrangThaiDen { get; set; }

    public string? GhiChu { get; set; }       // Lý do từ chối, ý kiến duyệt...
    public string? DiaChi_IP { get; set; }

    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════
// SỐ HIỆU VĂN BẢN - Bước 5 (chỉ loại cần cấp số)
// ═══════════════════════════════════════════════════════════════════
// Chỉ VanThuKhoa được cấp số. Chỉ cấp được khi TrangThai = Approved.
// Sau khi cấp số → IsLocked vẫn giữ (không unlock lại).

public class SoHieuVanBan
{
    public int Id { get; set; }

    public int VanBanId { get; set; }
    public VanBan VanBan { get; set; } = null!;

    // Dạng: 01/QĐ-KCN, 05/TB-KCN, 12/CV-KCN
    public string SoHieu { get; set; } = string.Empty;
    public int Nam { get; set; }
    public DocumentType LoaiVanBan { get; set; }
    public int SoThuTu { get; set; }   // Số thứ tự trong năm (dùng để tự sinh)

    public int NguoiCapSoId { get; set; }
    public NguoiDung NguoiCapSo { get; set; } = null!;

    public DateTime NgayCapSo { get; set; } = DateTime.UtcNow;

    // Thông tin vào sổ công văn điện tử
    public string? NguoiKy { get; set; }    // Họ tên người ký
    public string? ChucVuKy { get; set; }   // Chức vụ người ký
    public DateTime? NgayHieuLuc { get; set; }
    public DateTime? NgayHetHieuLuc { get; set; }
}

// Bộ đếm số hiệu theo năm + loại để tránh race condition
public class BoDemSoHieu
{
    public int Id { get; set; }
    public int Nam { get; set; }
    public DocumentType LoaiVanBan { get; set; }
    public int SoThuTuCuoi { get; set; } = 0;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════
// NGƯỜI NHẬN VĂN BẢN - Bước 6 (Phân phối + Read Receipt)
// ═══════════════════════════════════════════════════════════════════
// Nghiệp vụ: "ghi nhận người nhận đã Đọc hoặc Đã tiếp nhận văn bản"
// Giải quyết vấn đề "thiếu tính năng phản hồi (Feedback/Read Receipt)"

public class NguoiNhanVanBan
{
    public int Id { get; set; }

    public int VanBanId { get; set; }
    public VanBan VanBan { get; set; } = null!;

    public int NguoiNhanId { get; set; }
    public NguoiDung NguoiNhan { get; set; } = null!;

    // Trạng thái đọc
    public bool DaDoc { get; set; } = false;
    public DateTime? NgayDoc { get; set; }

    // Xác nhận tiếp nhận (có trách nhiệm pháp lý)
    public bool DaTiepNhan { get; set; } = false;
    public DateTime? NgayTiepNhan { get; set; }
    public string? GhiChuTiepNhan { get; set; }

    public DateTime NgayPhanPhoi { get; set; } = DateTime.UtcNow;
    public int NguoiPhanPhoiId { get; set; }
    public NguoiDung NguoiPhanPhoi { get; set; } = null!;
}

// ═══════════════════════════════════════════════════════════════════
// THÔNG BÁO
// ═══════════════════════════════════════════════════════════════════

public class ThongBaoHeThong
{
    public int Id { get; set; }

    public int NguoiNhanId { get; set; }
    public NguoiDung NguoiNhan { get; set; } = null!;

    public NotificationType LoaiThongBao { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;

    // Link đến văn bản liên quan
    public int? VanBanId { get; set; }

    public bool DaDoc { get; set; } = false;
    public DateTime? NgayDoc { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════
// NHẬT KÝ KIỂM TOÁN (Audit Log)
// ═══════════════════════════════════════════════════════════════════

public class NhatKyKiemToan
{
    public int Id { get; set; }
    public int? NguoiDungId { get; set; }
    public NguoiDung? NguoiDung { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public string? LoaiDoiTuong { get; set; }   // "VanBan", "NguoiDung"...
    public int? DoiTuongId { get; set; }
    public string? GiaTriCu { get; set; }        // JSON snapshot trước
    public string? GiaTriMoi { get; set; }       // JSON snapshot sau
    public string? DiaChi_IP { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;
}
