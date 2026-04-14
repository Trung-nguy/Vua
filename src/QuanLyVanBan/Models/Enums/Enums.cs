namespace QuanLyVanBan.Models.Enums;

// ═══════════════════════════════════════════════════════════════════
// TRẠNG THÁI VĂN BẢN - bám sát chu trình nghiệp vụ 7 bước
// ═══════════════════════════════════════════════════════════════════
// Luồng chính:
//   Draft → PendingDepartment → PendingFaculty → Approved → Issued → Distributed → Archived
// Rẽ nhánh từ chối (KHÔNG phải bước tuần tự):
//   PendingDepartment --[Từ chối]--> Draft  (quay về bước 1, nộp lại từ bước 2)
//   PendingFaculty    --[Từ chối]--> Draft  (quay về bước 1, nộp lại từ bước 2)
// Lưu ý: "Chỉnh sửa" KHÔNG phải bước riêng - chỉ là trạng thái Draft sau khi bị từ chối
public enum DocumentStatus
{
    Draft = 0,               // Bản nháp – đang soạn (Bước 1 - chưa nộp / đã bị trả về sửa)
    PendingDepartment = 1,   // Chờ Trưởng BM xác minh chuyên môn (Bước 2)
    PendingFaculty = 2,      // Chờ Lãnh đạo Khoa phê duyệt cuối (Bước 3)
    Approved = 3,            // Đã được Lãnh đạo Khoa phê duyệt (Bước 3 hoàn tất)
    Issued = 4,              // Đã cấp số hiệu & ban hành (Bước 5 - chỉ loại cần số)
    Distributed = 5,         // Đã phân phối / lưu trữ (Bước 6)
    Archived = 6,            // Lưu trữ dài hạn
    Recalled = 7             // Đã thu hồi (chỉ Admin)
}

// ═══════════════════════════════════════════════════════════════════
// LOẠI VĂN BẢN - quyết định có bắt buộc qua Bước 5 (cấp số) hay không
// ═══════════════════════════════════════════════════════════════════
public enum DocumentType
{
    // Loại KHÔNG cần cấp số hiệu chính thức (bỏ qua Bước 5, nhảy thẳng sang Bước 6)
    ToTrinh = 0,        // Tờ trình nội bộ
    PhieuDeXuat = 1,    // Phiếu đề xuất (mua sắm, công tác...)
    BaoCao = 2,         // Báo cáo tổng kết / định kỳ

    // Loại CẦN cấp số hiệu chính thức (bắt buộc qua Bước 5)
    QuyetDinh = 10,     // Quyết định hành chính → dạng: 01/QĐ-KCN
    ThongBao = 11,      // Thông báo ra bên ngoài → dạng: 05/TB-KCN
    CongVan = 12        // Công văn đi/đến       → dạng: 12/CV-KCN
}

// ═══════════════════════════════════════════════════════════════════
// VAI TRÒ NGƯỜI DÙNG - theo đúng tài liệu nghiệp vụ
// ═══════════════════════════════════════════════════════════════════
// Bước 1: GiangVien HOẶC VanThuKhoa tạo & nộp văn bản
// Bước 2: TruongBoMon xác minh chuyên môn
//          → Nếu người tạo là VanThuKhoa: BỎ QUA Bước 2, gửi thẳng Bước 3
// Bước 3: LanhDaoKhoa phê duyệt cuối cùng
// Bước 5: VanThuKhoa cấp số hiệu & vào sổ công văn
// Bước 6: Hệ thống / VanThuKhoa phân phối
// Admin: quản trị hệ thống
public enum RoleName
{
    GiangVien = 0,      // Giảng viên – tạo văn bản, nộp qua Bước 2
    VanThuKhoa = 1,     // Văn thư Khoa – tạo văn bản (bỏ qua Bước 2), cấp số, phân phối
    TruongBoMon = 2,    // Trưởng Bộ môn – xác minh chuyên môn (Bước 2)
    LanhDaoKhoa = 3,    // Lãnh đạo Khoa (Trưởng/Phó Khoa) – phê duyệt cuối (Bước 3)
    Admin = 4           // Quản trị hệ thống
}

// ═══════════════════════════════════════════════════════════════════
// TRẠNG THÁI BƯỚC PHÊ DUYỆT
// ═══════════════════════════════════════════════════════════════════
public enum ApprovalStepStatus
{
    Waiting = 0,    // Chưa đến lượt (bước này chưa được kích hoạt)
    Pending = 1,    // Đang chờ người có thẩm quyền xử lý
    Approved = 2,   // Đã chấp thuận
    Rejected = 3    // Đã từ chối (kèm lý do bắt buộc)
}

// ═══════════════════════════════════════════════════════════════════
// LOẠI THÔNG BÁO
// ═══════════════════════════════════════════════════════════════════
public enum NotificationType
{
    System = 0,
    DocumentSubmitted = 1,   // Văn bản được nộp lên duyệt
    DocumentApproved = 2,    // Văn bản được phê duyệt
    DocumentRejected = 3,    // Văn bản bị từ chối (kèm lý do)
    DocumentIssued = 4,      // Văn bản được cấp số hiệu
    DocumentDistributed = 5, // Văn bản được phân phối đến bạn
    OverdueWarning = 6       // Cảnh báo "ngâm" hồ sơ quá hạn
}
