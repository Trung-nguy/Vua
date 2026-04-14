using Microsoft.EntityFrameworkCore;
using QuanLyVanBan.Contracts;
using QuanLyVanBan.Data;
using QuanLyVanBan.DTOs.Requests;
using QuanLyVanBan.DTOs.Responses;
using QuanLyVanBan.Models;
using QuanLyVanBan.Models.Enums;

namespace QuanLyVanBan.Services;

/// <summary>
/// Giải quyết các vấn đề nghiệp vụ từ tài liệu:
/// 1. Xung đột vai trò: VanThuKhoa bỏ qua Bước 2 hoàn toàn
/// 2. Kẹt luồng: BuocBiTuChoiCuoi ghi nhớ nộp lại đi về đâu
/// 3. Từ chối bắt buộc ghi lý do
/// 4. Lock file sau khi Lãnh đạo Khoa phê duyệt
/// 5. Phân biệt rõ "chỉnh sửa" là rẽ nhánh, không phải bước tuần tự
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly AppDbContext _db;
    private readonly IThongBaoService _thongBao;
    private readonly IAuditService _audit;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(AppDbContext db, IThongBaoService thongBao,
        IAuditService audit, ILogger<WorkflowService> logger)
    {
        _db = db;
        _thongBao = thongBao;
        _audit = audit;
        _logger = logger;
    }

    // ── Bước 2: Nộp văn bản ─────────────────────────────────────────────────

    public async Task<string> NopAsync(NopVanBanRequest req, int nguoiDungId)
    {
        var vanBan = await LayVanBanHoacNemAsync(req.VanBanId);

        if (vanBan.TrangThai != DocumentStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể nộp văn bản ở trạng thái Bản nháp.");

        if (vanBan.NguoiTaoId != nguoiDungId)
            throw new UnauthorizedAccessException("Bạn không có quyền nộp văn bản này.");

        if (!vanBan.PhienBans.Any())
            throw new InvalidOperationException("Văn bản chưa có file đính kèm. Vui lòng tải lên file trước khi nộp.");

        var nguoiDung = await _db.NguoiDungs.Include(u => u.Role).Include(u => u.BoMon)
            .FirstOrDefaultAsync(u => u.Id == nguoiDungId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var roleHienTai = Enum.Parse<RoleName>(nguoiDung.Role.Name);
        var tuNgay = vanBan.TrangThai;

        string ketQua;

        // ════ LOGIC PHÂN CẤP QUAN TRỌNG ════════════════════════════════════
        //
        // Vấn đề từ tài liệu: "Nếu người soạn thảo là Văn thư Khoa,
        // hệ thống không thể bắt họ gửi xuống Trưởng BM được"
        //
        // Giải pháp:
        //   VanThuKhoa → BỎ QUA Bước 2 hoàn toàn → thẳng PendingFaculty
        //   GiangVien bị từ chối bởi Faculty → nộp lại cũng bỏ qua Bước 2
        //   GiangVien lần đầu / bị từ chối bởi BM → qua Bước 2
        // ════════════════════════════════════════════════════════════════════

        if (roleHienTai == RoleName.VanThuKhoa)
        {
            // Văn thư Khoa: bỏ qua Bước 2 hoàn toàn
            vanBan.TrangThai = DocumentStatus.PendingFaculty;
            await KichHoatBuocAsync(vanBan, 2); // Kích hoạt thẳng Bước 3 (ThuTu=2)
            ketQua = "Đã nộp văn bản. Chờ Lãnh đạo Khoa phê duyệt.";

            // Thông báo Lãnh đạo Khoa
            await _thongBao.GuiTheoRoleAsync(RoleName.LanhDaoKhoa, null,
                "Văn bản chờ phê duyệt",
                $"Văn thư Khoa vừa trình văn bản \"{vanBan.TieuDe}\" để phê duyệt.",
                NotificationType.DocumentSubmitted);
        }
        else if (vanBan.BuocBiTuChoiCuoi == "Faculty")
        {
            // GiangVien bị Lãnh đạo Khoa từ chối → nộp lại bỏ qua Bước 2
            vanBan.TrangThai = DocumentStatus.PendingFaculty;
            await ResetVaKichHoatBuocAsync(vanBan, 2);
            ketQua = "Đã nộp lại. Văn bản chuyển thẳng lên Lãnh đạo Khoa (đã qua Bước 2 trước đó).";

            await _thongBao.GuiTheoRoleAsync(RoleName.LanhDaoKhoa, null,
                "Văn bản nộp lại sau chỉnh sửa",
                $"Văn bản \"{vanBan.TieuDe}\" đã được chỉnh sửa và trình lại.",
                NotificationType.DocumentSubmitted);
        }
        else
        {
            // GiangVien lần đầu hoặc bị Trưởng BM từ chối → phải qua Bước 2
            vanBan.TrangThai = DocumentStatus.PendingDepartment;
            await ResetVaKichHoatBuocAsync(vanBan, 1);
            ketQua = "Đã nộp văn bản. Chờ Trưởng Bộ môn xác minh chuyên môn.";

            // Thông báo Trưởng BM của Bộ môn sở hữu văn bản
            await ThongBaoTruongBMAsync(vanBan,
                "Văn bản chờ xác minh chuyên môn",
                $"Văn bản \"{vanBan.TieuDe}\" cần được xác minh chuyên môn.",
                NotificationType.DocumentSubmitted);
        }

        vanBan.NgayCapNhat = DateTime.UtcNow;
        await GhiNhatKyAsync(vanBan.Id, nguoiDungId, "NopDuyet", tuNgay, vanBan.TrangThai, req.GhiChu);
        await _db.SaveChangesAsync();

        _logger.LogInformation("VanBan {Id} noped boi {User}, trang thai: {Status}", vanBan.Id, nguoiDungId, vanBan.TrangThai);
        return ketQua;
    }

    // ── Bước 2: Trưởng BM xác minh ─────────────────────────────────────────

    public async Task<string> XacMinhChuyenMonAsync(PheDuyetRequest req, int truongBMId)
    {
        var vanBan = await LayVanBanHoacNemAsync(req.VanBanId);

        if (vanBan.TrangThai != DocumentStatus.PendingDepartment)
            throw new InvalidOperationException("Văn bản không đang ở bước xác minh chuyên môn.");

        // Validate: chỉ Trưởng BM của Bộ môn sở hữu văn bản
        await ValidateTruongBMAsync(vanBan, truongBMId);

        var buoc = vanBan.BuocPheDuyets.First(b => b.ThuTu == 1);
        buoc.TrangThai = ApprovalStepStatus.Approved;
        buoc.NguoiXuLyId = truongBMId;
        buoc.YKien = req.YKien;
        buoc.NgayXuLy = DateTime.UtcNow;

        // Kích hoạt Bước 3
        var buoc3 = vanBan.BuocPheDuyets.First(b => b.ThuTu == 2);
        buoc3.TrangThai = ApprovalStepStatus.Pending;

        var tuNgay = vanBan.TrangThai;
        vanBan.TrangThai = DocumentStatus.PendingFaculty;
        vanBan.NgayCapNhat = DateTime.UtcNow;

        await GhiNhatKyAsync(vanBan.Id, truongBMId, "DuyetBM", tuNgay, vanBan.TrangThai, req.YKien);
        await _db.SaveChangesAsync();

        // Thông báo Lãnh đạo Khoa
        await _thongBao.GuiTheoRoleAsync(RoleName.LanhDaoKhoa, null,
            "Văn bản qua xác minh chuyên môn, chờ phê duyệt",
            $"Văn bản \"{vanBan.TieuDe}\" đã được Trưởng BM xác minh, chờ phê duyệt cuối.",
            NotificationType.DocumentSubmitted);

        // Thông báo người tạo
        await _thongBao.GuiAsync(vanBan.NguoiTaoId,
            "Văn bản đã qua xác minh chuyên môn",
            $"Văn bản \"{vanBan.TieuDe}\" đã được Trưởng BM xác nhận, chuyển lên Lãnh đạo Khoa.",
            NotificationType.DocumentSubmitted, vanBan.Id);

        return "Đã xác minh chuyên môn. Văn bản chuyển lên Lãnh đạo Khoa phê duyệt.";
    }

    // ── Bước 2: Trưởng BM từ chối ───────────────────────────────────────────

    public async Task<string> TuChoiBMAsync(TuChoiRequest req, int truongBMId)
    {
        var vanBan = await LayVanBanHoacNemAsync(req.VanBanId);

        if (vanBan.TrangThai != DocumentStatus.PendingDepartment)
            throw new InvalidOperationException("Văn bản không đang ở bước xác minh chuyên môn.");

        await ValidateTruongBMAsync(vanBan, truongBMId);

        var buoc = vanBan.BuocPheDuyets.First(b => b.ThuTu == 1);
        buoc.TrangThai = ApprovalStepStatus.Rejected;
        buoc.NguoiXuLyId = truongBMId;
        buoc.YKien = req.LyDoTuChoi;
        buoc.NgayXuLy = DateTime.UtcNow;

        var tuNgay = vanBan.TrangThai;
        vanBan.TrangThai = DocumentStatus.Draft;
        vanBan.BuocBiTuChoiCuoi = "Department"; // Nộp lại → Bước 2
        vanBan.NgayCapNhat = DateTime.UtcNow;

        await GhiNhatKyAsync(vanBan.Id, truongBMId, "TuChoiBM", tuNgay, vanBan.TrangThai, req.LyDoTuChoi);
        await _db.SaveChangesAsync();

        await _thongBao.GuiAsync(vanBan.NguoiTaoId,
            "Văn bản bị từ chối tại Bộ môn",
            $"Văn bản \"{vanBan.TieuDe}\" bị từ chối. Lý do: {req.LyDoTuChoi}. Vui lòng chỉnh sửa và nộp lại.",
            NotificationType.DocumentRejected, vanBan.Id);

        return $"Đã từ chối. Văn bản trả về bản nháp để chỉnh sửa. Lý do: {req.LyDoTuChoi}";
    }

    // ── Bước 3: Lãnh đạo Khoa phê duyệt cuối ───────────────────────────────

    public async Task<string> PheDuyetCuoiAsync(PheDuyetRequest req, int lanhDaoId)
    {
        var vanBan = await LayVanBanHoacNemAsync(req.VanBanId);

        if (vanBan.TrangThai != DocumentStatus.PendingFaculty)
            throw new InvalidOperationException("Văn bản không đang ở bước phê duyệt cuối.");

        await ValidateLanhDaoKhoaAsync(lanhDaoId);

        var buoc = vanBan.BuocPheDuyets.First(b => b.ThuTu == 2);
        buoc.TrangThai = ApprovalStepStatus.Approved;
        buoc.NguoiXuLyId = lanhDaoId;
        buoc.YKien = req.YKien;
        buoc.NgayXuLy = DateTime.UtcNow;

        var tuNgay = vanBan.TrangThai;
        vanBan.TrangThai = DocumentStatus.Approved;
        vanBan.BuocBiTuChoiCuoi = null; // Reset sau khi được duyệt
        vanBan.NgayCapNhat = DateTime.UtcNow;

        // ════ KHÓA FILE SAU KHI LÃNH ĐẠO DUYỆT ════════════════════════════
        // Giải quyết vấn đề: "Sửa văn bản sau khi đã duyệt"
        // Từ thời điểm này KHÔNG AI được upload file mới nữa
        vanBan.IsLocked = true;
        // ════════════════════════════════════════════════════════════════════

        await GhiNhatKyAsync(vanBan.Id, lanhDaoId, "PheDuyetCuoi", tuNgay, vanBan.TrangThai, req.YKien);
        await _db.SaveChangesAsync();

        // Thông báo người tạo
        await _thongBao.GuiAsync(vanBan.NguoiTaoId,
            "Văn bản đã được phê duyệt",
            $"Văn bản \"{vanBan.TieuDe}\" đã được Lãnh đạo Khoa phê duyệt chính thức.",
            NotificationType.DocumentApproved, vanBan.Id);

        var canCapSo = CanCapSoHieu(vanBan.LoaiVanBan);

        // Thông báo Văn thư Khoa bước tiếp theo
        if (canCapSo)
        {
            await _thongBao.GuiTheoRoleAsync(RoleName.VanThuKhoa, null,
                "Văn bản cần cấp số hiệu",
                $"Văn bản \"{vanBan.TieuDe}\" đã được phê duyệt. Vui lòng cấp số hiệu chính thức.",
                NotificationType.DocumentApproved);
        }
        else
        {
            await _thongBao.GuiTheoRoleAsync(RoleName.VanThuKhoa, null,
                "Văn bản sẵn sàng phân phối",
                $"Văn bản \"{vanBan.TieuDe}\" đã được phê duyệt và sẵn sàng để phân phối.",
                NotificationType.DocumentApproved);
        }

        return canCapSo
            ? "Văn bản đã được phê duyệt. File đã bị khóa. Văn thư Khoa cần cấp số hiệu."
            : "Văn bản đã được phê duyệt. File đã bị khóa. Văn thư Khoa có thể phân phối.";
    }

    // ── Bước 3: Lãnh đạo Khoa từ chối ──────────────────────────────────────

    public async Task<string> TuChoiKhoaAsync(TuChoiRequest req, int lanhDaoId)
    {
        var vanBan = await LayVanBanHoacNemAsync(req.VanBanId);

        if (vanBan.TrangThai != DocumentStatus.PendingFaculty)
            throw new InvalidOperationException("Văn bản không đang ở bước phê duyệt cuối.");

        await ValidateLanhDaoKhoaAsync(lanhDaoId);

        var buoc = vanBan.BuocPheDuyets.First(b => b.ThuTu == 2);
        buoc.TrangThai = ApprovalStepStatus.Rejected;
        buoc.NguoiXuLyId = lanhDaoId;
        buoc.YKien = req.LyDoTuChoi;
        buoc.NgayXuLy = DateTime.UtcNow;

        var tuNgay = vanBan.TrangThai;
        vanBan.TrangThai = DocumentStatus.Draft;

        // ════ GIẢI QUYẾT VẤN ĐỀ "KẸT LUỒNG DEADLOCK" ══════════════════════
        // Ghi nhớ bị từ chối ở đâu → nộp lại sẽ bỏ qua Bước 2
        // Nếu VanThuKhoa tạo: đã bỏ qua Bước 2 từ đầu, vẫn đúng
        vanBan.BuocBiTuChoiCuoi = "Faculty";
        // ════════════════════════════════════════════════════════════════════

        vanBan.NgayCapNhat = DateTime.UtcNow;

        await GhiNhatKyAsync(vanBan.Id, lanhDaoId, "TuChoiKhoa", tuNgay, vanBan.TrangThai, req.LyDoTuChoi);
        await _db.SaveChangesAsync();

        await _thongBao.GuiAsync(vanBan.NguoiTaoId,
            "Văn bản bị từ chối tại Lãnh đạo Khoa",
            $"Văn bản \"{vanBan.TieuDe}\" bị từ chối. Lý do: {req.LyDoTuChoi}. Chỉnh sửa và nộp lại (sẽ bỏ qua Bộ môn).",
            NotificationType.DocumentRejected, vanBan.Id);

        return $"Đã từ chối. Văn bản trả về bản nháp. Lý do: {req.LyDoTuChoi}. Nộp lại sẽ bỏ qua Bộ môn.";
    }

    // ── Nộp lại (dùng lại NopAsync) ─────────────────────────────────────────

    public async Task<string> NopLaiAsync(NopLaiRequest req, int nguoiDungId)
    {
        return await NopAsync(new NopVanBanRequest(req.VanBanId, req.GhiChu), nguoiDungId);
    }

    // ── Lịch sử ─────────────────────────────────────────────────────────────

    public async Task<List<NhatKyWorkflowResponse>> LayLichSuAsync(int vanBanId)
    {
        return await _db.NhatKyWorkflows
            .Include(n => n.NguoiThucHien).ThenInclude(u => u.Role)
            .Where(n => n.VanBanId == vanBanId)
            .OrderBy(n => n.ThoiGian)
            .Select(n => new NhatKyWorkflowResponse
            {
                Id = n.Id,
                HanhDong = n.HanhDong,
                HanhDongHienThi = MapHanhDong(n.HanhDong),
                TrangThaiTu = n.TrangThaiTu.ToString(),
                TrangThaiDen = n.TrangThaiDen.ToString(),
                TenNguoiThucHien = n.NguoiThucHien.HoTen,
                RoleNguoiThucHien = n.NguoiThucHien.Role.TenHienThi,
                GhiChu = n.GhiChu,
                ThoiGian = n.ThoiGian
            })
            .ToListAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<VanBan> LayVanBanHoacNemAsync(int vanBanId)
    {
        return await _db.VanBans
            .Include(v => v.PhienBans)
            .Include(v => v.BuocPheDuyets)
            .Include(v => v.BoMon)
            .FirstOrDefaultAsync(v => v.Id == vanBanId)
            ?? throw new KeyNotFoundException($"Không tìm thấy văn bản Id={vanBanId}.");
    }

    /// <summary>Tạo BuocPheDuyet khi văn bản được nộp lần đầu</summary>
    private async Task KichHoatBuocAsync(VanBan vanBan, int thuTuBuoc)
    {
        if (!vanBan.BuocPheDuyets.Any())
        {
            // Lấy Trưởng BM của Bộ môn
            int? truongBMId = null;
            if (vanBan.BoMonId.HasValue)
            {
                var boMon = await _db.BoMons.FindAsync(vanBan.BoMonId.Value);
                truongBMId = boMon?.TruongBoMonId;
            }

            _db.BuocPheDuyets.AddRange(
                new BuocPheDuyet
                {
                    VanBanId = vanBan.Id, ThuTu = 1,
                    TenBuoc = "Trưởng Bộ môn xác minh chuyên môn",
                    RoleYeuCau = RoleName.TruongBoMon,
                    NguoiDuocGiaoId = truongBMId,
                    TrangThai = thuTuBuoc == 1 ? ApprovalStepStatus.Pending : ApprovalStepStatus.Waiting
                },
                new BuocPheDuyet
                {
                    VanBanId = vanBan.Id, ThuTu = 2,
                    TenBuoc = "Lãnh đạo Khoa phê duyệt cuối cùng",
                    RoleYeuCau = RoleName.LanhDaoKhoa,
                    TrangThai = thuTuBuoc == 2 ? ApprovalStepStatus.Pending : ApprovalStepStatus.Waiting
                }
            );
        }
        else
        {
            var buoc = vanBan.BuocPheDuyets.First(b => b.ThuTu == thuTuBuoc);
            buoc.TrangThai = ApprovalStepStatus.Pending;
        }
    }

    /// <summary>Reset bước bị từ chối và kích hoạt lại khi nộp lại</summary>
    private async Task ResetVaKichHoatBuocAsync(VanBan vanBan, int thuTuBuocKichHoat)
    {
        if (!vanBan.BuocPheDuyets.Any())
        {
            await KichHoatBuocAsync(vanBan, thuTuBuocKichHoat);
            return;
        }

        foreach (var buoc in vanBan.BuocPheDuyets)
        {
            if (buoc.ThuTu == thuTuBuocKichHoat)
            {
                buoc.TrangThai = ApprovalStepStatus.Pending;
                buoc.NguoiXuLyId = null;
                buoc.YKien = null;
                buoc.NgayXuLy = null;
            }
            else if (buoc.ThuTu > thuTuBuocKichHoat)
            {
                buoc.TrangThai = ApprovalStepStatus.Waiting;
                buoc.NguoiXuLyId = null;
                buoc.YKien = null;
                buoc.NgayXuLy = null;
            }
        }
        await Task.CompletedTask;
    }

    private async Task ValidateTruongBMAsync(VanBan vanBan, int truongBMId)
    {
        var nguoiDung = await _db.NguoiDungs.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == truongBMId)
            ?? throw new KeyNotFoundException("Không tìm thấy người duyệt.");

        if (nguoiDung.Role.Name != "TruongBoMon" && nguoiDung.Role.Name != "Admin")
            throw new UnauthorizedAccessException("Chỉ Trưởng Bộ môn mới có quyền xác minh chuyên môn.");

        // Kiểm tra Trưởng BM đúng Bộ môn (trừ Admin)
        if (nguoiDung.Role.Name == "TruongBoMon" && vanBan.BoMonId.HasValue)
        {
            var boMon = await _db.BoMons.FindAsync(vanBan.BoMonId.Value);
            if (boMon?.TruongBoMonId != truongBMId)
                throw new UnauthorizedAccessException("Bạn chỉ có quyền xác minh văn bản thuộc Bộ môn của mình.");
        }
    }

    private async Task ValidateLanhDaoKhoaAsync(int lanhDaoId)
    {
        var nguoiDung = await _db.NguoiDungs.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == lanhDaoId)
            ?? throw new KeyNotFoundException("Không tìm thấy người duyệt.");

        if (nguoiDung.Role.Name != "LanhDaoKhoa" && nguoiDung.Role.Name != "Admin")
            throw new UnauthorizedAccessException("Chỉ Lãnh đạo Khoa mới có quyền phê duyệt cuối cùng.");
    }

    private async Task ThongBaoTruongBMAsync(VanBan vanBan, string tieuDe, string noiDung, NotificationType loai)
    {
        if (!vanBan.BoMonId.HasValue) return;
        var boMon = await _db.BoMons.FindAsync(vanBan.BoMonId.Value);
        if (boMon?.TruongBoMonId != null)
            await _thongBao.GuiAsync(boMon.TruongBoMonId.Value, tieuDe, noiDung, loai, vanBan.Id);
    }

    private async Task GhiNhatKyAsync(int vanBanId, int nguoiId, string hanhDong,
        DocumentStatus tu, DocumentStatus den, string? ghiChu = null)
    {
        await _db.NhatKyWorkflows.AddAsync(new NhatKyWorkflow
        {
            VanBanId = vanBanId,
            NguoiThucHienId = nguoiId,
            HanhDong = hanhDong,
            TrangThaiTu = tu,
            TrangThaiDen = den,
            GhiChu = ghiChu,
            ThoiGian = DateTime.UtcNow
        });
    }

    public static bool CanCapSoHieu(DocumentType loai) =>
        loai is DocumentType.QuyetDinh or DocumentType.ThongBao or DocumentType.CongVan;

    private static string MapHanhDong(string hanhDong) => hanhDong switch
    {
        "TaoMoi"      => "Tạo văn bản",
        "NopDuyet"    => "Nộp để duyệt",
        "DuyetBM"     => "Trưởng BM xác minh đạt",
        "TuChoiBM"    => "Trưởng BM từ chối",
        "PheDuyetCuoi"=> "Lãnh đạo Khoa phê duyệt",
        "TuChoiKhoa"  => "Lãnh đạo Khoa từ chối",
        "CapSoHieu"   => "Cấp số hiệu",
        "PhanPhoi"    => "Phân phối văn bản",
        "ChinhSua"    => "Chỉnh sửa (upload phiên bản mới)",
        _ => hanhDong
    };
}
