using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyVanBan.Contracts;
using QuanLyVanBan.DTOs.Requests;
using QuanLyVanBan.Helpers;
using QuanLyVanBan.Models.Enums;
using QuanLyVanBan.Security;

namespace QuanLyVanBan.Controllers;

// ═══════════════════════════════════════════════════════════════════
// AUTH CONTROLLER
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) { _auth = auth; }

    /// <summary>Đăng nhập – trả về Access Token + Refresh Token</summary>
    [HttpPost("dang-nhap")]
    public async Task<IActionResult> DangNhap([FromBody] DangNhapRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.DangNhapAsync(req, ip);
        return result.ThanhCong ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Đăng ký tài khoản (mặc định role: Giảng viên)</summary>
    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKy([FromBody] DangKyRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _auth.DangKyAsync(req);
        return result.ThanhCong ? Ok(result) : BadRequest(result);
    }

    /// <summary>Làm mới Access Token bằng Refresh Token (không cần đăng nhập lại)</summary>
    [HttpPost("lam-moi-token")]
    public async Task<IActionResult> LamMoiToken([FromBody] LamMoiTokenRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.LamMoiTokenAsync(req.RefreshToken, ip);
        return result.ThanhCong ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Đăng xuất – huỷ Refresh Token</summary>
    [HttpPost("dang-xuat")]
    [Authorize]
    public async Task<IActionResult> DangXuat()
    {
        await _auth.DangXuatAsync(User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = "Đăng xuất thành công." });
    }

    /// <summary>Đổi mật khẩu (cần đăng nhập)</summary>
    [HttpPost("doi-mat-khau")]
    [Authorize]
    public async Task<IActionResult> DoiMatKhau([FromBody] DoiMatKhauRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            await _auth.DoiMatKhauAsync(User.LayNguoiDungId(), req);
            return Ok(new { thanhCong = true, thongBao = "Đổi mật khẩu thành công." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { thanhCong = false, thongBao = ex.Message }); }
    }

    /// <summary>Quên mật khẩu – gửi link reset qua email</summary>
    [HttpPost("quen-mat-khau")]
    public async Task<IActionResult> QuenMatKhau([FromBody] QuenMatKhauRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _auth.QuenMatKhauAsync(req);
        return Ok(new { thanhCong = true, thongBao = "Nếu email tồn tại, link đặt lại mật khẩu sẽ được gửi." });
    }

    /// <summary>Đặt lại mật khẩu bằng token từ email</summary>
    [HttpPost("dat-lai-mat-khau")]
    public async Task<IActionResult> DatLaiMatKhau([FromBody] DatLaiMatKhauRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            await _auth.DatLaiMatKhauAsync(req);
            return Ok(new { thanhCong = true, thongBao = "Đặt lại mật khẩu thành công." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { thanhCong = false, thongBao = ex.Message }); }
    }
}

// ═══════════════════════════════════════════════════════════════════
// VAN BAN CONTROLLER
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/van-ban")]
[Authorize]
public class VanBanController : ControllerBase
{
    private readonly IVanBanService _svc;
    public VanBanController(IVanBanService svc) { _svc = svc; }

    /// <summary>
    /// Bước 1: Tạo văn bản mới kèm file bắt buộc.
    /// Người dùng: GiangVien hoặc VanThuKhoa.
    /// Dùng multipart/form-data (có file).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Tao([FromForm] TaoVanBanRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _svc.TaoAsync(req, User.LayNguoiDungId());
        return CreatedAtAction(nameof(LayChiTiet), new { id = result.Id }, result);
    }

    /// <summary>Tìm kiếm văn bản có phân trang (lọc theo role tự động)</summary>
    [HttpGet]
    public async Task<IActionResult> TimKiem([FromQuery] TimKiemVanBanRequest req)
    {
        var result = await _svc.TimKiemAsync(req, User.LayNguoiDungId());
        return Ok(result);
    }

    /// <summary>Chi tiết văn bản: lịch sử duyệt, các phiên bản file, danh sách người nhận</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> LayChiTiet(int id)
    {
        var result = await _svc.LayChiTietAsync(id, User.LayNguoiDungId());
        return result == null ? NotFound(new { thongBao = "Không tìm thấy văn bản." }) : Ok(result);
    }

    /// <summary>
    /// Bước 4 (rẽ nhánh): Chỉnh sửa văn bản SAU KHI bị từ chối.
    /// Bắt buộc upload file phiên bản mới + ghi chú đã sửa gì.
    /// KHÔNG xóa phiên bản cũ (đảm bảo Audit Trail).
    /// Chỉ hoạt động khi TrangThai = Draft.
    /// </summary>
    [HttpPut("{id:int}/chinh-sua")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ChinhSua(int id, [FromForm] ChinhSuaVanBanRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _svc.ChinhSuaAsync(id, req, User.LayNguoiDungId());
        return Ok(result);
    }

    /// <summary>Xoá văn bản – chỉ khi Draft và là người tạo</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Xoa(int id)
    {
        await _svc.XoaAsync(id, User.LayNguoiDungId());
        return NoContent();
    }

    /// <summary>Tải xuống file (tự động đánh dấu đã đọc nếu là người nhận)</summary>
    [HttpGet("{id:int}/tai-xuong")]
    public async Task<IActionResult> TaiXuong(int id, [FromQuery] int? phienBanId = null)
    {
        var (data, tenFile, contentType) = await _svc.TaiXuongAsync(id, phienBanId, User.LayNguoiDungId());
        return File(data, contentType, tenFile);
    }

    /// <summary>Dashboard cá nhân: thống kê nhanh, việc cần làm, văn bản gần đây</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result = await _svc.LayDashboardAsync(User.LayNguoiDungId());
        return Ok(result);
    }
}

// ═══════════════════════════════════════════════════════════════════
// WORKFLOW CONTROLLER
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/workflow")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _svc;
    public WorkflowController(IWorkflowService svc) { _svc = svc; }

    /// <summary>
    /// Nộp văn bản để duyệt.
    /// Logic tự động:
    ///   - GiangVien → PendingDepartment (Bước 2)
    ///   - VanThuKhoa → PendingFaculty (bỏ qua Bước 2)
    ///   - GiangVien đã bị Faculty từ chối → PendingFaculty (bỏ qua Bước 2)
    /// </summary>
    [HttpPost("nop")]
    public async Task<IActionResult> Nop([FromBody] NopVanBanRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var msg = await _svc.NopAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = msg });
    }

    /// <summary>
    /// Bước 2: Trưởng BM xác minh chuyên môn → chuyển lên Lãnh đạo Khoa.
    /// Chỉ Trưởng BM của đúng Bộ môn sở hữu văn bản.
    /// </summary>
    [HttpPost("xac-minh-chuyen-mon")]
    [YeuCauRole(RoleName.TruongBoMon, RoleName.Admin)]
    public async Task<IActionResult> XacMinhChuyenMon([FromBody] PheDuyetRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var msg = await _svc.XacMinhChuyenMonAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = msg });
    }

    /// <summary>
    /// Bước 2: Trưởng BM từ chối → văn bản về Draft.
    /// Lý do từ chối BẮT BUỘC phải nhập.
    /// </summary>
    [HttpPost("tu-choi-bo-mon")]
    [YeuCauRole(RoleName.TruongBoMon, RoleName.Admin)]
    public async Task<IActionResult> TuChoiBoMon([FromBody] TuChoiRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var msg = await _svc.TuChoiBMAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = msg });
    }

    /// <summary>
    /// Bước 3: Lãnh đạo Khoa phê duyệt cuối → Approved.
    /// File bị KHÓA ngay lập tức sau khi duyệt.
    /// Hệ thống tự thông báo Văn thư bước tiếp theo.
    /// </summary>
    [HttpPost("phe-duyet-cuoi")]
    [YeuCauRole(RoleName.LanhDaoKhoa, RoleName.Admin)]
    public async Task<IActionResult> PheDuyetCuoi([FromBody] PheDuyetRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var msg = await _svc.PheDuyetCuoiAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = msg });
    }

    /// <summary>
    /// Bước 3: Lãnh đạo Khoa từ chối → Draft.
    /// Lý do BẮT BUỘC. Nộp lại sẽ bỏ qua Bước 2.
    /// </summary>
    [HttpPost("tu-choi-khoa")]
    [YeuCauRole(RoleName.LanhDaoKhoa, RoleName.Admin)]
    public async Task<IActionResult> TuChoiKhoa([FromBody] TuChoiRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var msg = await _svc.TuChoiKhoaAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = msg });
    }

    /// <summary>
    /// Nộp lại sau khi chỉnh sửa.
    /// Hệ thống tự định tuyến đúng bước dựa vào lịch sử từ chối.
    /// </summary>
    [HttpPost("nop-lai")]
    public async Task<IActionResult> NopLai([FromBody] NopLaiRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var msg = await _svc.NopLaiAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = msg });
    }

    /// <summary>Lịch sử xử lý văn bản (Audit Trail đầy đủ)</summary>
    [HttpGet("{vanBanId:int}/lich-su")]
    public async Task<IActionResult> LichSu(int vanBanId)
    {
        var result = await _svc.LayLichSuAsync(vanBanId);
        return Ok(result);
    }
}

// ═══════════════════════════════════════════════════════════════════
// SO HIEU CONTROLLER (Bước 5)
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/so-hieu")]
[Authorize]
public class SoHieuController : ControllerBase
{
    private readonly ISoHieuService _svc;
    public SoHieuController(ISoHieuService svc) { _svc = svc; }

    /// <summary>
    /// Bước 5: Cấp số hiệu chính thức.
    /// Chỉ VanThuKhoa. Chỉ văn bản đã Approved và thuộc loại cần số hiệu.
    /// Tự sinh số nếu không nhập tay (VD: 05/2025/TB-KCN).
    /// </summary>
    [HttpPost("cap-so")]
    [YeuCauRole(RoleName.VanThuKhoa, RoleName.Admin)]
    public async Task<IActionResult> CapSo([FromBody] CapSoHieuRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _svc.CapSoAsync(req, User.LayNguoiDungId());
        return Ok(result);
    }

    /// <summary>Xem trước số hiệu sẽ được cấp (chưa lưu DB)</summary>
    [HttpGet("xem-truoc/{vanBanId:int}")]
    [YeuCauRole(RoleName.VanThuKhoa, RoleName.Admin)]
    public async Task<IActionResult> XemTruoc(int vanBanId)
    {
        var soHieu = await _svc.XemTruocSoHieuAsync(vanBanId);
        return Ok(new { soHieu });
    }

    /// <summary>Lấy thông tin số hiệu của văn bản</summary>
    [HttpGet("{vanBanId:int}")]
    public async Task<IActionResult> LayThongTin(int vanBanId)
    {
        var result = await _svc.LayThongTinSoHieuAsync(vanBanId);
        return result == null ? NotFound() : Ok(result);
    }
}

// ═══════════════════════════════════════════════════════════════════
// PHAN PHOI CONTROLLER (Bước 6)
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/phan-phoi")]
[Authorize]
public class PhanPhoiController : ControllerBase
{
    private readonly IPhanPhoiService _svc;
    public PhanPhoiController(IPhanPhoiService svc) { _svc = svc; }

    /// <summary>
    /// Bước 6: Phân phối văn bản đến danh sách người nhận.
    /// Chỉ VanThuKhoa hoặc LanhDaoKhoa. Văn bản phải đã Approved hoặc Issued.
    /// Tự gửi thông báo đến từng người nhận.
    /// </summary>
    [HttpPost]
    [YeuCauRole(RoleName.VanThuKhoa, RoleName.LanhDaoKhoa, RoleName.Admin)]
    public async Task<IActionResult> PhanPhoi([FromBody] PhanPhoiVanBanRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var msg = await _svc.PhanPhoiAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = msg });
    }

    /// <summary>
    /// Đánh dấu đã đọc văn bản (tự động khi tải xuống).
    /// Hoặc có thể gọi thủ công.
    /// </summary>
    [HttpPut("{vanBanId:int}/da-doc")]
    public async Task<IActionResult> DanhDauDaDoc(int vanBanId)
    {
        await _svc.DanhDauDaDocAsync(vanBanId, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = "Đã đánh dấu đã đọc." });
    }

    /// <summary>
    /// Xác nhận tiếp nhận văn bản (có trách nhiệm pháp lý).
    /// Khác với "đã đọc" – đây là hành động có chủ ý của người nhận.
    /// </summary>
    [HttpPost("xac-nhan-tiep-nhan")]
    public async Task<IActionResult> XacNhanTiepNhan([FromBody] XacNhanTiepNhanRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var msg = await _svc.XacNhanTiepNhanAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = msg });
    }

    /// <summary>Danh sách người nhận kèm trạng thái đọc/tiếp nhận</summary>
    [HttpGet("{vanBanId:int}/nguoi-nhan")]
    public async Task<IActionResult> LayDanhSachNguoiNhan(int vanBanId)
    {
        var result = await _svc.LayDanhSachNguoiNhanAsync(vanBanId);
        return Ok(result);
    }

    /// <summary>Văn bản được phân phối đến tôi (hộp thư đến)</summary>
    [HttpGet("hop-thu-den")]
    public async Task<IActionResult> HopThuDen(
        [FromQuery] bool chuaDocThoi = false,
        [FromQuery] int trang = 1,
        [FromQuery] int kichThuoc = 20)
    {
        var result = await _svc.LayVanBanDuocPhanPhoiAsync(User.LayNguoiDungId(), chuaDocThoi, trang, kichThuoc);
        return Ok(result);
    }
}

// ═══════════════════════════════════════════════════════════════════
// NGUOI DUNG CONTROLLER
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/nguoi-dung")]
[Authorize]
public class NguoiDungController : ControllerBase
{
    private readonly INguoiDungService _svc;
    public NguoiDungController(INguoiDungService svc) { _svc = svc; }

    /// <summary>Thông tin cá nhân của tôi</summary>
    [HttpGet("toi")]
    public async Task<IActionResult> LayThongTinToi()
    {
        var result = await _svc.LayTheoIdAsync(User.LayNguoiDungId());
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Cập nhật hồ sơ cá nhân</summary>
    [HttpPut("toi")]
    public async Task<IActionResult> CapNhatHoSo([FromBody] CapNhatHoSoRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _svc.CapNhatHoSoAsync(User.LayNguoiDungId(), req);
        return Ok(result);
    }

    /// <summary>Danh sách người dùng (để chọn người nhận khi phân phối)</summary>
    [HttpGet]
    [YeuCauRole(RoleName.VanThuKhoa, RoleName.LanhDaoKhoa, RoleName.TruongBoMon, RoleName.Admin)]
    public async Task<IActionResult> LayDanhSach(
        [FromQuery] int? boMonId,
        [FromQuery] RoleName? role,
        [FromQuery] bool activeOnly = true,
        [FromQuery] int trang = 1,
        [FromQuery] int kichThuoc = 50)
    {
        var result = await _svc.LayDanhSachAsync(boMonId, role, activeOnly, trang, kichThuoc);
        return Ok(result);
    }

    /// <summary>Lấy thông tin người dùng theo ID</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> LayTheoId(int id)
    {
        var result = await _svc.LayTheoIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Admin: cập nhật role người dùng</summary>
    [HttpPut("cap-nhat-role")]
    [YeuCauRole(RoleName.Admin)]
    public async Task<IActionResult> CapNhatRole([FromBody] CapNhatRoleRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _svc.CapNhatRoleAsync(req, User.LayNguoiDungId());
        return Ok(result);
    }

    /// <summary>Admin: khoá / mở khoá tài khoản</summary>
    [HttpPut("khoa-tai-khoan")]
    [YeuCauRole(RoleName.Admin)]
    public async Task<IActionResult> KhoaTaiKhoan([FromBody] KhoaTaiKhoanRequest req)
    {
        await _svc.KhoaTaiKhoanAsync(req, User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = req.IsActive ? "Tài khoản đã được kích hoạt." : "Tài khoản đã bị khoá." });
    }
}

// ═══════════════════════════════════════════════════════════════════
// BO MON CONTROLLER
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/bo-mon")]
[Authorize]
public class BoMonController : ControllerBase
{
    private readonly IBoMonService _svc;
    public BoMonController(IBoMonService svc) { _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> LayDanhSach([FromQuery] bool activeOnly = true)
        => Ok(await _svc.LayDanhSachAsync(activeOnly));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> LayTheoId(int id)
    {
        var result = await _svc.LayTheoIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [YeuCauRole(RoleName.Admin)]
    public async Task<IActionResult> Tao([FromBody] TaoBoMonRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _svc.TaoAsync(req);
        return CreatedAtAction(nameof(LayTheoId), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [YeuCauRole(RoleName.Admin)]
    public async Task<IActionResult> CapNhat(int id, [FromBody] TaoBoMonRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _svc.CapNhatAsync(id, req);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [YeuCauRole(RoleName.Admin)]
    public async Task<IActionResult> Xoa(int id)
    {
        await _svc.XoaAsync(id);
        return NoContent();
    }
}

// ═══════════════════════════════════════════════════════════════════
// THONG BAO CONTROLLER
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/thong-bao")]
[Authorize]
public class ThongBaoController : ControllerBase
{
    private readonly IThongBaoService _svc;
    public ThongBaoController(IThongBaoService svc) { _svc = svc; }

    /// <summary>Danh sách thông báo của tôi</summary>
    [HttpGet]
    public async Task<IActionResult> LayThongBao([FromQuery] bool chuaDocThoi = false)
        => Ok(await _svc.LayThongBaoCuaTuiAsync(User.LayNguoiDungId(), chuaDocThoi));

    /// <summary>Số thông báo chưa đọc (dùng cho badge trên UI)</summary>
    [HttpGet("so-chua-doc")]
    public async Task<IActionResult> SoChuaDoc()
        => Ok(new { soChuaDoc = await _svc.DemChuaDocAsync(User.LayNguoiDungId()) });

    /// <summary>Đánh dấu 1 thông báo đã đọc</summary>
    [HttpPut("{id:int}/da-doc")]
    public async Task<IActionResult> DanhDauDaDoc(int id)
    {
        await _svc.DanhDauDaDocAsync(id, User.LayNguoiDungId());
        return Ok(new { thanhCong = true });
    }

    /// <summary>Đánh dấu tất cả thông báo đã đọc</summary>
    [HttpPut("da-doc-tat-ca")]
    public async Task<IActionResult> DanhDauTatCaDaDoc()
    {
        await _svc.DanhDauTatCaDaDocAsync(User.LayNguoiDungId());
        return Ok(new { thanhCong = true, thongBao = "Đã đánh dấu tất cả là đã đọc." });
    }
}

// ═══════════════════════════════════════════════════════════════════
// THONG KE CONTROLLER (Bước 7)
// ═══════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/thong-ke")]
[Authorize]
[YeuCauRole(RoleName.VanThuKhoa, RoleName.LanhDaoKhoa, RoleName.Admin)]
public class ThongKeController : ControllerBase
{
    private readonly IThongKeService _svc;
    public ThongKeController(IThongKeService svc) { _svc = svc; }

    /// <summary>
    /// Bước 7: Tổng hợp & Báo cáo.
    /// Thống kê số lượng văn bản theo loại, theo thời gian, theo bộ môn.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LayThongKe(
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay,
        [FromQuery] int? boMonId)
    {
        var tu = tuNgay ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var den = denNgay ?? DateTime.UtcNow;
        var result = await _svc.LayThongKeAsync(tu, den, boMonId);
        return Ok(result);
    }

    /// <summary>Danh sách văn bản quá hạn xử lý (cảnh báo "ngâm" hồ sơ)</summary>
    [HttpGet("qua-han")]
    public async Task<IActionResult> LayVanBanQuaHan()
        => Ok(await _svc.LayVanBanQuaHanAsync());
}
