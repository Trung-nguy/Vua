using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyVanBan.Contracts;
using QuanLyVanBan.Data;
using QuanLyVanBan.DTOs.Requests;
using QuanLyVanBan.DTOs.Responses;
using QuanLyVanBan.Helpers;
using QuanLyVanBan.Models;
using QuanLyVanBan.Models.Enums;

namespace QuanLyVanBan.Services;

// ═══════════════════════════════════════════════════════════════════
// VAN BAN SERVICE
// ═══════════════════════════════════════════════════════════════════
public class VanBanService : IVanBanService
{
    private readonly AppDbContext _db;
    private readonly FileHelper _fileHelper;
    private readonly IAuditService _audit;

    public VanBanService(AppDbContext db, FileHelper fileHelper, IAuditService audit)
    {
        _db = db; _fileHelper = fileHelper; _audit = audit;
    }

    public async Task<VanBanChiTietResponse> TaoAsync(TaoVanBanRequest req, int nguoiTaoId)
    {
        var nguoiTao = await _db.NguoiDungs.Include(u => u.BoMon)
            .FirstOrDefaultAsync(u => u.Id == nguoiTaoId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var vanBan = new VanBan
        {
            TieuDe = req.TieuDe.Trim(),
            MoTa = req.MoTa?.Trim(),
            LoaiVanBan = req.LoaiVanBan,
            TrangThai = DocumentStatus.Draft,
            NguoiTaoId = nguoiTaoId,
            BoMonId = nguoiTao.BoMonId,
            HanXuLy = req.HanXuLy,
            NgayTao = DateTime.UtcNow
        };

        _db.VanBans.Add(vanBan);
        await _db.SaveChangesAsync();

        // Lưu file bắt buộc (phiên bản 1)
        var (duongDan, kichThuoc, checksum) = await _fileHelper.LuuFileAsync(req.File, vanBan.Id.ToString());
        _db.PhienBanFiles.Add(new PhienBanFile
        {
            VanBanId = vanBan.Id,
            TenFile = req.File.FileName,
            DuongDanLuuTru = duongDan,
            ContentType = req.File.ContentType,
            KichThuocBytes = kichThuoc,
            ChecksumMD5 = checksum,
            SoPhienBan = 1,
            GhiChuChinhSua = "Phiên bản gốc",
            NguoiUploadId = nguoiTaoId,
            NgayUpload = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _audit.GhiAsync(nguoiTaoId, "TaoVanBan", "VanBan", vanBan.Id);
        return (await LayChiTietAsync(vanBan.Id, nguoiTaoId))!;
    }

    public async Task<VanBanChiTietResponse?> LayChiTietAsync(int id, int nguoiYeuCauId)
    {
        var vanBan = await _db.VanBans
            .Include(v => v.NguoiTao).ThenInclude(u => u.Role)
            .Include(v => v.BoMon)
            .Include(v => v.PhienBans).ThenInclude(p => p.NguoiUpload)
            .Include(v => v.BuocPheDuyets).ThenInclude(b => b.NguoiDuocGiao)
            .Include(v => v.BuocPheDuyets).ThenInclude(b => b.NguoiXuLy)
            .Include(v => v.NhatKyWorkflows).ThenInclude(n => n.NguoiThucHien).ThenInclude(u => u.Role)
            .Include(v => v.SoHieu).ThenInclude(s => s!.NguoiCapSo)
            .Include(v => v.NguoiNhans).ThenInclude(n => n.NguoiNhan)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vanBan == null) return null;

        await KiemTraQuyenXemAsync(vanBan, nguoiYeuCauId);
        return MapChiTiet(vanBan);
    }

    public async Task<KetQuaPhanTrang<VanBanTomTatResponse>> TimKiemAsync(TimKiemVanBanRequest req, int nguoiYeuCauId)
    {
        var nguoiDung = await _db.NguoiDungs.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == nguoiYeuCauId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var query = _db.VanBans
            .Include(v => v.NguoiTao)
            .Include(v => v.BoMon)
            .Include(v => v.PhienBans)
            .Include(v => v.SoHieu)
            .AsNoTracking();

        // ── Lọc theo role ──────────────────────────────────────────────────
        var roleName = nguoiDung.Role.Name;
        if (roleName == "GiangVien")
        {
            // Chỉ thấy văn bản mình tạo + văn bản được phân phối đến mình
            var vbDuocPhanPhoi = await _db.NguoiNhanVanBans
                .Where(n => n.NguoiNhanId == nguoiYeuCauId)
                .Select(n => n.VanBanId).ToListAsync();
            query = query.Where(v => v.NguoiTaoId == nguoiYeuCauId || vbDuocPhanPhoi.Contains(v.Id));
        }
        else if (roleName == "TruongBoMon")
        {
            // Thấy văn bản của Bộ môn mình + được phân phối
            var vbDuocPhanPhoi = await _db.NguoiNhanVanBans
                .Where(n => n.NguoiNhanId == nguoiYeuCauId)
                .Select(n => n.VanBanId).ToListAsync();
            query = query.Where(v =>
                v.BoMonId == nguoiDung.BoMonId ||
                v.NguoiTaoId == nguoiYeuCauId ||
                vbDuocPhanPhoi.Contains(v.Id));
        }
        // LanhDaoKhoa, VanThuKhoa, Admin: thấy tất cả

        // ── Filters ────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(req.TuKhoa))
        {
            var kw = req.TuKhoa.Trim().ToLower();
            query = query.Where(v => v.TieuDe.ToLower().Contains(kw) ||
                                     (v.MoTa != null && v.MoTa.ToLower().Contains(kw)));
        }
        if (req.TrangThai.HasValue) query = query.Where(v => v.TrangThai == req.TrangThai.Value);
        if (req.LoaiVanBan.HasValue) query = query.Where(v => v.LoaiVanBan == req.LoaiVanBan.Value);
        if (req.NguoiTaoId.HasValue) query = query.Where(v => v.NguoiTaoId == req.NguoiTaoId.Value);
        if (req.BoMonId.HasValue) query = query.Where(v => v.BoMonId == req.BoMonId.Value);
        if (req.TuNgay.HasValue) query = query.Where(v => v.NgayTao >= req.TuNgay.Value);
        if (req.DenNgay.HasValue) query = query.Where(v => v.NgayTao <= req.DenNgay.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(req.SoHieu))
            query = query.Where(v => v.SoHieu != null && v.SoHieu.SoHieu.Contains(req.SoHieu));

        // ── Sort ───────────────────────────────────────────────────────────
        query = req.SapXepTheo?.ToLower() switch
        {
            "tieude"   => req.GiamDan ? query.OrderByDescending(v => v.TieuDe)   : query.OrderBy(v => v.TieuDe),
            "trangthai"=> req.GiamDan ? query.OrderByDescending(v => v.TrangThai) : query.OrderBy(v => v.TrangThai),
            _          => req.GiamDan ? query.OrderByDescending(v => v.NgayTao)   : query.OrderBy(v => v.NgayTao)
        };

        var tong = await query.CountAsync();
        var items = await query.Skip((req.Trang - 1) * req.KichThuocTrang).Take(req.KichThuocTrang).ToListAsync();

        return new KetQuaPhanTrang<VanBanTomTatResponse>
        {
            DuLieu = items.Select(MapTomTat).ToList(),
            TongSoBanGhi = tong,
            Trang = req.Trang,
            KichThuocTrang = req.KichThuocTrang
        };
    }

    public async Task<VanBanChiTietResponse> ChinhSuaAsync(int id, ChinhSuaVanBanRequest req, int nguoiYeuCauId)
    {
        var vanBan = await _db.VanBans.Include(v => v.PhienBans)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy văn bản.");

        if (vanBan.NguoiTaoId != nguoiYeuCauId)
            throw new UnauthorizedAccessException("Bạn không phải người tạo văn bản này.");

        if (vanBan.TrangThai != DocumentStatus.Draft)
            throw new InvalidOperationException("Chỉ chỉnh sửa được văn bản ở trạng thái Bản nháp (sau khi bị từ chối).");

        if (vanBan.IsLocked)
            throw new InvalidOperationException("Văn bản đã bị khóa sau khi phê duyệt. Không thể chỉnh sửa.");

        if (!string.IsNullOrWhiteSpace(req.TieuDe)) vanBan.TieuDe = req.TieuDe.Trim();
        if (!string.IsNullOrWhiteSpace(req.MoTa)) vanBan.MoTa = req.MoTa.Trim();

        // Upload phiên bản mới – KHÔNG xóa file cũ (giải quyết vấn đề "che giấu lịch sử")
        var soPhienBanMoi = (vanBan.PhienBans.Any() ? vanBan.PhienBans.Max(p => p.SoPhienBan) : 0) + 1;
        var (duongDan, kichThuoc, checksum) = await _fileHelper.LuuFileAsync(req.File, vanBan.Id.ToString());
        _db.PhienBanFiles.Add(new PhienBanFile
        {
            VanBanId = vanBan.Id,
            TenFile = req.File.FileName,
            DuongDanLuuTru = duongDan,
            ContentType = req.File.ContentType,
            KichThuocBytes = kichThuoc,
            ChecksumMD5 = checksum,
            SoPhienBan = soPhienBanMoi,
            GhiChuChinhSua = $"v{soPhienBanMoi}: {req.GhiChuChinhSua}",
            NguoiUploadId = nguoiYeuCauId,
            NgayUpload = DateTime.UtcNow
        });

        vanBan.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.GhiAsync(nguoiYeuCauId, "ChinhSuaVanBan", "VanBan", id);
        return (await LayChiTietAsync(id, nguoiYeuCauId))!;
    }

    public async Task XoaAsync(int id, int nguoiYeuCauId)
    {
        var vanBan = await _db.VanBans.Include(v => v.PhienBans)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy văn bản.");

        if (vanBan.NguoiTaoId != nguoiYeuCauId)
            throw new UnauthorizedAccessException("Bạn không phải người tạo văn bản này.");

        if (vanBan.TrangThai != DocumentStatus.Draft)
            throw new InvalidOperationException("Chỉ xóa được văn bản ở trạng thái Bản nháp.");

        foreach (var pv in vanBan.PhienBans)
            _fileHelper.XoaFile(pv.DuongDanLuuTru);

        _db.VanBans.Remove(vanBan);
        await _db.SaveChangesAsync();
        await _audit.GhiAsync(nguoiYeuCauId, "XoaVanBan", "VanBan", id);
    }

    public async Task<(byte[] DuLieu, string TenFile, string ContentType)> TaiXuongAsync(int vanBanId, int? phienBanId, int nguoiYeuCauId)
    {
        var vanBan = await _db.VanBans.Include(v => v.PhienBans)
            .AsNoTracking().FirstOrDefaultAsync(v => v.Id == vanBanId)
            ?? throw new KeyNotFoundException("Không tìm thấy văn bản.");

        await KiemTraQuyenXemAsync(vanBan, nguoiYeuCauId);

        // Nếu đang đọc → đánh dấu đã đọc cho người nhận
        await DanhDauDaDocNeuCo(vanBanId, nguoiYeuCauId);

        PhienBanFile? pv = phienBanId.HasValue
            ? vanBan.PhienBans.FirstOrDefault(p => p.Id == phienBanId.Value)
            : vanBan.PhienBans.OrderByDescending(p => p.SoPhienBan).FirstOrDefault();

        if (pv == null) throw new InvalidOperationException("Không tìm thấy file đính kèm.");

        var bytes = await _fileHelper.DocFileAsync(pv.DuongDanLuuTru);
        return (bytes, pv.TenFile, pv.ContentType ?? "application/octet-stream");
    }

    public async Task<DashboardResponse> LayDashboardAsync(int nguoiDungId)
    {
        var nguoiDung = await _db.NguoiDungs.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == nguoiDungId)
            ?? throw new KeyNotFoundException();
        var role = nguoiDung.Role.Name;

        var dashboard = new DashboardResponse
        {
            VanBanCuaToi = await _db.VanBans.CountAsync(v => v.NguoiTaoId == nguoiDungId),
            ThongBaoChuaDoc = await _db.ThongBaos.CountAsync(t => t.NguoiNhanId == nguoiDungId && !t.DaDoc),
            VanBanDuocPhanPhoiChuaDoc = await _db.NguoiNhanVanBans.CountAsync(n => n.NguoiNhanId == nguoiDungId && !n.DaDoc),
            VanBanChoXuLy = await _db.VanBans.CountAsync(v => v.NguoiTaoId == nguoiDungId &&
                (v.TrangThai == DocumentStatus.PendingDepartment || v.TrangThai == DocumentStatus.PendingFaculty)),
            VanBanBiTuChoi = await _db.VanBans.CountAsync(v => v.NguoiTaoId == nguoiDungId &&
                v.TrangThai == DocumentStatus.Draft && v.BuocBiTuChoiCuoi != null),
            ChoTuXacMinh = role == "TruongBoMon"
                ? await _db.VanBans.CountAsync(v => v.TrangThai == DocumentStatus.PendingDepartment && v.BoMonId == nguoiDung.BoMonId)
                : 0,
            ChoTuPheDuyet = role == "LanhDaoKhoa" || role == "Admin"
                ? await _db.VanBans.CountAsync(v => v.TrangThai == DocumentStatus.PendingFaculty)
                : 0,
            ChoCapSoHieu = role is "VanThuKhoa" or "Admin"
                ? await _db.VanBans.CountAsync(v => v.TrangThai == DocumentStatus.Approved &&
                    (v.LoaiVanBan == DocumentType.QuyetDinh || v.LoaiVanBan == DocumentType.ThongBao || v.LoaiVanBan == DocumentType.CongVan))
                : 0,
            QuaHan = await _db.VanBans.CountAsync(v =>
                v.HanXuLy.HasValue && v.HanXuLy < DateTime.UtcNow &&
                v.TrangThai != DocumentStatus.Approved &&
                v.TrangThai != DocumentStatus.Issued &&
                v.TrangThai != DocumentStatus.Distributed &&
                v.TrangThai != DocumentStatus.Archived)
        };

        dashboard.VanBanGanDay = await _db.VanBans
            .Include(v => v.NguoiTao).Include(v => v.PhienBans).Include(v => v.SoHieu)
            .Where(v => v.NguoiTaoId == nguoiDungId)
            .OrderByDescending(v => v.NgayCapNhat ?? v.NgayTao).Take(5)
            .AsNoTracking().ToListAsync().ContinueWith(t => t.Result.Select(MapTomTat).ToList());

        dashboard.ThongBaoGanDay = await _db.ThongBaos
            .Where(t => t.NguoiNhanId == nguoiDungId)
            .OrderByDescending(t => t.NgayTao).Take(5)
            .Select(t => new ThongBaoResponse
            {
                Id = t.Id, LoaiThongBao = t.LoaiThongBao.ToString(),
                TieuDe = t.TieuDe, NoiDung = t.NoiDung,
                VanBanId = t.VanBanId, DaDoc = t.DaDoc, NgayTao = t.NgayTao
            }).ToListAsync();

        return dashboard;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task KiemTraQuyenXemAsync(VanBan vanBan, int nguoiId)
    {
        var nguoiDung = await _db.NguoiDungs.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == nguoiId)
            ?? throw new KeyNotFoundException();
        var role = nguoiDung.Role.Name;

        if (vanBan.NguoiTaoId == nguoiId) return;
        if (role is "LanhDaoKhoa" or "VanThuKhoa" or "Admin") return;
        if (role == "TruongBoMon" && vanBan.BoMonId == nguoiDung.BoMonId) return;
        var duocPhanPhoi = await _db.NguoiNhanVanBans.AnyAsync(n => n.VanBanId == vanBan.Id && n.NguoiNhanId == nguoiId);
        if (duocPhanPhoi) return;
        throw new UnauthorizedAccessException("Bạn không có quyền xem văn bản này.");
    }

    private async Task DanhDauDaDocNeuCo(int vanBanId, int nguoiId)
    {
        var record = await _db.NguoiNhanVanBans
            .FirstOrDefaultAsync(n => n.VanBanId == vanBanId && n.NguoiNhanId == nguoiId);
        if (record != null && !record.DaDoc)
        {
            record.DaDoc = true;
            record.NgayDoc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static VanBanTomTatResponse MapTomTat(VanBan v) => new()
    {
        Id = v.Id, TieuDe = v.TieuDe,
        LoaiVanBan = v.LoaiVanBan.ToString(),
        LoaiVanBanHienThi = MapLoaiVanBan(v.LoaiVanBan),
        TrangThai = v.TrangThai.ToString(),
        TrangThaiHienThi = MapTrangThai(v.TrangThai),
        CanCapSoHieu = WorkflowService.CanCapSoHieu(v.LoaiVanBan),
        IsLocked = v.IsLocked,
        SoHieu = v.SoHieu?.SoHieu,
        TenNguoiTao = v.NguoiTao?.HoTen,
        TenBoMon = v.BoMon?.Ten,
        SoPhienBan = v.PhienBans?.Count ?? 0,
        HanXuLy = v.HanXuLy,
        QuaHan = v.HanXuLy.HasValue && v.HanXuLy < DateTime.UtcNow &&
                 v.TrangThai < DocumentStatus.Approved,
        NgayTao = v.NgayTao, NgayCapNhat = v.NgayCapNhat
    };

    private static VanBanChiTietResponse MapChiTiet(VanBan v)
    {
        var tomTat = MapTomTat(v);
        return new VanBanChiTietResponse
        {
            Id = tomTat.Id, TieuDe = tomTat.TieuDe,
            LoaiVanBan = tomTat.LoaiVanBan, LoaiVanBanHienThi = tomTat.LoaiVanBanHienThi,
            TrangThai = tomTat.TrangThai, TrangThaiHienThi = tomTat.TrangThaiHienThi,
            CanCapSoHieu = tomTat.CanCapSoHieu, IsLocked = tomTat.IsLocked,
            SoHieu = tomTat.SoHieu, TenNguoiTao = tomTat.TenNguoiTao,
            TenBoMon = tomTat.TenBoMon, SoPhienBan = tomTat.SoPhienBan,
            HanXuLy = tomTat.HanXuLy, QuaHan = tomTat.QuaHan,
            NgayTao = tomTat.NgayTao, NgayCapNhat = tomTat.NgayCapNhat,
            MoTa = v.MoTa,
            PhienBans = (v.PhienBans ?? new()).OrderByDescending(p => p.SoPhienBan).Select(p => new PhienBanFileResponse
            {
                Id = p.Id, SoPhienBan = p.SoPhienBan, TenFile = p.TenFile,
                KichThuocBytes = p.KichThuocBytes,
                KichThuocHienThi = FormatKichThuoc(p.KichThuocBytes),
                ContentType = p.ContentType, GhiChuChinhSua = p.GhiChuChinhSua,
                TenNguoiUpload = p.NguoiUpload?.HoTen, NgayUpload = p.NgayUpload
            }).ToList(),
            BuocPheDuyets = (v.BuocPheDuyets ?? new()).OrderBy(b => b.ThuTu).Select(b => new BuocPheDuyetResponse
            {
                Id = b.Id, ThuTu = b.ThuTu, TenBuoc = b.TenBuoc,
                RoleYeuCau = b.RoleYeuCau.ToString(), TrangThai = b.TrangThai.ToString(),
                TrangThaiHienThi = MapTrangThaiBuoc(b.TrangThai),
                TenNguoiDuocGiao = b.NguoiDuocGiao?.HoTen, TenNguoiXuLy = b.NguoiXuLy?.HoTen,
                YKien = b.YKien, NgayXuLy = b.NgayXuLy, HanXuLy = b.HanXuLy,
                QuaHan = b.HanXuLy.HasValue && b.HanXuLy < DateTime.UtcNow && b.TrangThai == ApprovalStepStatus.Pending
            }).ToList(),
            LichSuXuLy = (v.NhatKyWorkflows ?? new()).OrderBy(n => n.ThoiGian).Select(n => new NhatKyWorkflowResponse
            {
                Id = n.Id, HanhDong = n.HanhDong, HanhDongHienThi = WorkflowService_MapHanhDong(n.HanhDong),
                TrangThaiTu = n.TrangThaiTu.ToString(), TrangThaiDen = n.TrangThaiDen.ToString(),
                TenNguoiThucHien = n.NguoiThucHien?.HoTen ?? "",
                RoleNguoiThucHien = n.NguoiThucHien?.Role?.TenHienThi ?? "",
                GhiChu = n.GhiChu, ThoiGian = n.ThoiGian
            }).ToList(),
            ThongTinSoHieu = v.SoHieu == null ? null : new SoHieuResponse
            {
                Id = v.SoHieu.Id, SoHieu = v.SoHieu.SoHieu,
                NguoiKy = v.SoHieu.NguoiKy, ChucVuKy = v.SoHieu.ChucVuKy,
                TenNguoiCapSo = v.SoHieu.NguoiCapSo?.HoTen ?? "",
                NgayCapSo = v.SoHieu.NgayCapSo,
                NgayHieuLuc = v.SoHieu.NgayHieuLuc, NgayHetHieuLuc = v.SoHieu.NgayHetHieuLuc
            },
            DanhSachNguoiNhan = (v.NguoiNhans ?? new()).Select(n => new NguoiNhanResponse
            {
                NguoiNhanId = n.NguoiNhanId, HoTen = n.NguoiNhan?.HoTen ?? "",
                Email = n.NguoiNhan?.Email, DaDoc = n.DaDoc, NgayDoc = n.NgayDoc,
                DaTiepNhan = n.DaTiepNhan, NgayTiepNhan = n.NgayTiepNhan,
                NgayPhanPhoi = n.NgayPhanPhoi
            }).ToList()
        };
    }

    private static string WorkflowService_MapHanhDong(string h) => h switch
    {
        "TaoMoi" => "Tạo văn bản", "NopDuyet" => "Nộp để duyệt",
        "DuyetBM" => "Trưởng BM xác minh đạt", "TuChoiBM" => "Trưởng BM từ chối",
        "PheDuyetCuoi" => "Lãnh đạo Khoa phê duyệt", "TuChoiKhoa" => "Lãnh đạo Khoa từ chối",
        "CapSoHieu" => "Cấp số hiệu", "PhanPhoi" => "Phân phối văn bản",
        "ChinhSua" => "Chỉnh sửa (upload phiên bản mới)", _ => h
    };

    public static string MapLoaiVanBan(DocumentType t) => t switch
    {
        DocumentType.ToTrinh => "Tờ trình", DocumentType.PhieuDeXuat => "Phiếu đề xuất",
        DocumentType.BaoCao => "Báo cáo", DocumentType.QuyetDinh => "Quyết định",
        DocumentType.ThongBao => "Thông báo", DocumentType.CongVan => "Công văn", _ => t.ToString()
    };

    public static string MapTrangThai(DocumentStatus s) => s switch
    {
        DocumentStatus.Draft => "Bản nháp",
        DocumentStatus.PendingDepartment => "Chờ Trưởng BM xác minh",
        DocumentStatus.PendingFaculty => "Chờ Lãnh đạo Khoa phê duyệt",
        DocumentStatus.Approved => "Đã phê duyệt",
        DocumentStatus.Issued => "Đã ban hành (có số hiệu)",
        DocumentStatus.Distributed => "Đã phân phối",
        DocumentStatus.Archived => "Lưu trữ",
        DocumentStatus.Recalled => "Đã thu hồi", _ => s.ToString()
    };

    private static string MapTrangThaiBuoc(ApprovalStepStatus s) => s switch
    {
        ApprovalStepStatus.Waiting => "Chưa đến lượt",
        ApprovalStepStatus.Pending => "Đang chờ xử lý",
        ApprovalStepStatus.Approved => "Đã chấp thuận",
        ApprovalStepStatus.Rejected => "Đã từ chối", _ => s.ToString()
    };

    private static string FormatKichThuoc(long bytes) =>
        bytes switch { >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB", >= 1024 => $"{bytes / 1024.0:F0} KB", _ => $"{bytes} B" };
}

// ═══════════════════════════════════════════════════════════════════
// SO HIEU SERVICE
// ═══════════════════════════════════════════════════════════════════
public class SoHieuService : ISoHieuService
{
    private readonly AppDbContext _db;
    private readonly IThongBaoService _thongBao;
    private const string MaToChuc = "KCN";

    private static readonly Dictionary<DocumentType, string> TienTo = new()
    {
        [DocumentType.QuyetDinh] = "QĐ",
        [DocumentType.ThongBao]  = "TB",
        [DocumentType.CongVan]   = "CV"
    };

    public SoHieuService(AppDbContext db, IThongBaoService thongBao) { _db = db; _thongBao = thongBao; }

    public async Task<SoHieuResponse> CapSoAsync(CapSoHieuRequest req, int vanThuId)
    {
        var vanBan = await _db.VanBans.Include(v => v.SoHieu)
            .FirstOrDefaultAsync(v => v.Id == req.VanBanId)
            ?? throw new KeyNotFoundException("Không tìm thấy văn bản.");

        if (vanBan.TrangThai != DocumentStatus.Approved)
            throw new InvalidOperationException("Chỉ cấp số cho văn bản đã được phê duyệt.");

        if (!WorkflowService.CanCapSoHieu(vanBan.LoaiVanBan))
            throw new InvalidOperationException($"Loại văn bản '{VanBanService.MapLoaiVanBan(vanBan.LoaiVanBan)}' không cần cấp số hiệu chính thức.");

        if (vanBan.SoHieu != null)
            throw new InvalidOperationException($"Văn bản đã có số hiệu '{vanBan.SoHieu.SoHieu}'.");

        var nguoiVanThu = await _db.NguoiDungs.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == vanThuId)
            ?? throw new KeyNotFoundException();
        if (nguoiVanThu.Role.Name != "VanThuKhoa" && nguoiVanThu.Role.Name != "Admin")
            throw new UnauthorizedAccessException("Chỉ Văn thư Khoa mới có quyền cấp số hiệu.");

        var nam = DateTime.UtcNow.Year;
        string soHieu;
        int soThuTu;

        if (!string.IsNullOrWhiteSpace(req.SoHieuTuyChinh))
        {
            soHieu = req.SoHieuTuyChinh.Trim();
            if (await _db.SoHieuVanBans.AnyAsync(s => s.SoHieu == soHieu))
                throw new InvalidOperationException($"Số hiệu '{soHieu}' đã tồn tại.");
            soThuTu = 0;
        }
        else
        {
            soThuTu = await LaySoThuTuTiepTheoAsync(nam, vanBan.LoaiVanBan);
            soHieu = $"{soThuTu:D2}/{nam}/{TienTo[vanBan.LoaiVanBan]}-{MaToChuc}";
        }

        var soHieuEntity = new SoHieuVanBan
        {
            VanBanId = vanBan.Id, SoHieu = soHieu, Nam = nam, LoaiVanBan = vanBan.LoaiVanBan, SoThuTu = soThuTu,
            NguoiCapSoId = vanThuId, NgayCapSo = DateTime.UtcNow,
            NguoiKy = req.NguoiKy, ChucVuKy = req.ChucVuKy,
            NgayHieuLuc = req.NgayHieuLuc, NgayHetHieuLuc = req.NgayHetHieuLuc
        };
        _db.SoHieuVanBans.Add(soHieuEntity);

        vanBan.TrangThai = DocumentStatus.Issued;
        vanBan.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _thongBao.GuiAsync(vanBan.NguoiTaoId, "Văn bản đã được ban hành",
            $"Văn bản \"{vanBan.TieuDe}\" đã được cấp số hiệu '{soHieu}'.",
            NotificationType.DocumentIssued, vanBan.Id);

        return new SoHieuResponse
        {
            Id = soHieuEntity.Id, SoHieu = soHieu, NguoiKy = req.NguoiKy, ChucVuKy = req.ChucVuKy,
            TenNguoiCapSo = nguoiVanThu.HoTen, NgayCapSo = soHieuEntity.NgayCapSo,
            NgayHieuLuc = req.NgayHieuLuc, NgayHetHieuLuc = req.NgayHetHieuLuc
        };
    }

    public async Task<string> XemTruocSoHieuAsync(int vanBanId)
    {
        var vanBan = await _db.VanBans.FindAsync(vanBanId)
            ?? throw new KeyNotFoundException("Không tìm thấy văn bản.");
        if (!WorkflowService.CanCapSoHieu(vanBan.LoaiVanBan))
            return "(Loại văn bản này không cần số hiệu)";
        var nam = DateTime.UtcNow.Year;
        var next = await LaySoThuTuTiepTheoAsync(nam, vanBan.LoaiVanBan);
        return $"{next:D2}/{nam}/{TienTo[vanBan.LoaiVanBan]}-{MaToChuc}";
    }

    public async Task<SoHieuResponse?> LayThongTinSoHieuAsync(int vanBanId)
    {
        var s = await _db.SoHieuVanBans.Include(x => x.NguoiCapSo)
            .FirstOrDefaultAsync(x => x.VanBanId == vanBanId);
        return s == null ? null : new SoHieuResponse
        {
            Id = s.Id, SoHieu = s.SoHieu, NguoiKy = s.NguoiKy, ChucVuKy = s.ChucVuKy,
            TenNguoiCapSo = s.NguoiCapSo?.HoTen ?? "", NgayCapSo = s.NgayCapSo,
            NgayHieuLuc = s.NgayHieuLuc, NgayHetHieuLuc = s.NgayHetHieuLuc
        };
    }

    private async Task<int> LaySoThuTuTiepTheoAsync(int nam, DocumentType loai)
    {
        var dem = await _db.BoDemSoHieus.FirstOrDefaultAsync(b => b.Nam == nam && b.LoaiVanBan == loai);
        if (dem == null)
        {
            dem = new BoDemSoHieu { Nam = nam, LoaiVanBan = loai, SoThuTuCuoi = 1 };
            _db.BoDemSoHieus.Add(dem);
        }
        else dem.SoThuTuCuoi++;
        dem.NgayCapNhat = DateTime.UtcNow;
        return dem.SoThuTuCuoi;
    }
}

// ═══════════════════════════════════════════════════════════════════
// PHAN PHOI SERVICE
// ═══════════════════════════════════════════════════════════════════
public class PhanPhoiService : IPhanPhoiService
{
    private readonly AppDbContext _db;
    private readonly IThongBaoService _thongBao;

    public PhanPhoiService(AppDbContext db, IThongBaoService thongBao) { _db = db; _thongBao = thongBao; }

    public async Task<string> PhanPhoiAsync(PhanPhoiVanBanRequest req, int nguoiPhanPhoiId)
    {
        var vanBan = await _db.VanBans.Include(v => v.NguoiNhans)
            .FirstOrDefaultAsync(v => v.Id == req.VanBanId)
            ?? throw new KeyNotFoundException("Không tìm thấy văn bản.");

        if (vanBan.TrangThai != DocumentStatus.Approved && vanBan.TrangThai != DocumentStatus.Issued)
            throw new InvalidOperationException("Chỉ phân phối văn bản đã được phê duyệt hoặc ban hành.");

        var daTonTai = vanBan.NguoiNhans.Select(n => n.NguoiNhanId).ToHashSet();
        var nguoiMoi = req.DanhSachNguoiNhanIds.Where(id => !daTonTai.Contains(id)).Distinct().ToList();

        if (!nguoiMoi.Any()) return "Tất cả người nhận đã được phân phối trước đó.";

        var nguoiHopLe = await _db.NguoiDungs
            .Where(u => nguoiMoi.Contains(u.Id) && u.IsActive)
            .ToDictionaryAsync(u => u.Id, u => u.HoTen);

        foreach (var id in nguoiMoi.Where(id => nguoiHopLe.ContainsKey(id)))
        {
            _db.NguoiNhanVanBans.Add(new NguoiNhanVanBan
            {
                VanBanId = req.VanBanId, NguoiNhanId = id,
                NguoiPhanPhoiId = nguoiPhanPhoiId, NgayPhanPhoi = DateTime.UtcNow
            });
            await _thongBao.GuiAsync(id, "Bạn có văn bản mới",
                $"Văn bản \"{vanBan.TieuDe}\" đã được gửi đến bạn. Vui lòng xem và xác nhận tiếp nhận.",
                NotificationType.DocumentDistributed, req.VanBanId);
        }

        if (vanBan.TrangThai == DocumentStatus.Issued)
        {
            vanBan.TrangThai = DocumentStatus.Distributed;
            vanBan.NgayCapNhat = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return $"Đã phân phối văn bản đến {nguoiMoi.Count} người nhận.";
    }

    public async Task DanhDauDaDocAsync(int vanBanId, int nguoiNhanId)
    {
        var r = await _db.NguoiNhanVanBans
            .FirstOrDefaultAsync(n => n.VanBanId == vanBanId && n.NguoiNhanId == nguoiNhanId);
        if (r != null && !r.DaDoc) { r.DaDoc = true; r.NgayDoc = DateTime.UtcNow; await _db.SaveChangesAsync(); }
    }

    public async Task<string> XacNhanTiepNhanAsync(XacNhanTiepNhanRequest req, int nguoiNhanId)
    {
        var r = await _db.NguoiNhanVanBans
            .FirstOrDefaultAsync(n => n.VanBanId == req.VanBanId && n.NguoiNhanId == nguoiNhanId)
            ?? throw new KeyNotFoundException("Bạn không nằm trong danh sách nhận văn bản này.");
        if (r.DaTiepNhan) return "Bạn đã xác nhận tiếp nhận văn bản này trước đó.";
        r.DaDoc = true; r.NgayDoc ??= DateTime.UtcNow;
        r.DaTiepNhan = true; r.NgayTiepNhan = DateTime.UtcNow; r.GhiChuTiepNhan = req.GhiChu;
        await _db.SaveChangesAsync();
        return "Đã xác nhận tiếp nhận văn bản thành công.";
    }

    public async Task<List<NguoiNhanResponse>> LayDanhSachNguoiNhanAsync(int vanBanId) =>
        await _db.NguoiNhanVanBans.Include(n => n.NguoiNhan)
            .Where(n => n.VanBanId == vanBanId)
            .Select(n => new NguoiNhanResponse
            {
                NguoiNhanId = n.NguoiNhanId, HoTen = n.NguoiNhan.HoTen,
                Email = n.NguoiNhan.Email, DaDoc = n.DaDoc, NgayDoc = n.NgayDoc,
                DaTiepNhan = n.DaTiepNhan, NgayTiepNhan = n.NgayTiepNhan, NgayPhanPhoi = n.NgayPhanPhoi
            }).ToListAsync();

    public async Task<KetQuaPhanTrang<VanBanTomTatResponse>> LayVanBanDuocPhanPhoiAsync(int nguoiDungId, bool chuaDocThoi, int trang, int kichThuoc)
    {
        var query = _db.NguoiNhanVanBans
            .Include(n => n.VanBan).ThenInclude(v => v.NguoiTao)
            .Include(n => n.VanBan).ThenInclude(v => v.PhienBans)
            .Include(n => n.VanBan).ThenInclude(v => v.SoHieu)
            .Where(n => n.NguoiNhanId == nguoiDungId);
        if (chuaDocThoi) query = query.Where(n => !n.DaDoc);
        var tong = await query.CountAsync();
        var items = await query.OrderByDescending(n => n.NgayPhanPhoi)
            .Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToListAsync();
        return new KetQuaPhanTrang<VanBanTomTatResponse>
        {
            DuLieu = items.Select(n => VanBanService_MapTomTat(n.VanBan)).ToList(),
            TongSoBanGhi = tong, Trang = trang, KichThuocTrang = kichThuoc
        };
    }

    private static VanBanTomTatResponse VanBanService_MapTomTat(VanBan v) => new()
    {
        Id = v.Id, TieuDe = v.TieuDe, LoaiVanBan = v.LoaiVanBan.ToString(),
        LoaiVanBanHienThi = VanBanService.MapLoaiVanBan(v.LoaiVanBan),
        TrangThai = v.TrangThai.ToString(), TrangThaiHienThi = VanBanService.MapTrangThai(v.TrangThai),
        CanCapSoHieu = WorkflowService.CanCapSoHieu(v.LoaiVanBan),
        IsLocked = v.IsLocked, SoHieu = v.SoHieu?.SoHieu,
        TenNguoiTao = v.NguoiTao?.HoTen, SoPhienBan = v.PhienBans?.Count ?? 0,
        NgayTao = v.NgayTao, NgayCapNhat = v.NgayCapNhat
    };
}

// ═══════════════════════════════════════════════════════════════════
// THONG BAO SERVICE
// ═══════════════════════════════════════════════════════════════════
public class ThongBaoService : IThongBaoService
{
    private readonly AppDbContext _db;
    public ThongBaoService(AppDbContext db) { _db = db; }

    public async Task GuiAsync(int nguoiNhanId, string tieuDe, string noiDung,
        NotificationType loai = NotificationType.System, int? vanBanId = null)
    {
        _db.ThongBaos.Add(new ThongBaoHeThong
        {
            NguoiNhanId = nguoiNhanId, LoaiThongBao = loai,
            TieuDe = tieuDe, NoiDung = noiDung, VanBanId = vanBanId, NgayTao = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task GuiTheoRoleAsync(RoleName role, int? boMonId, string tieuDe, string noiDung, NotificationType loai)
    {
        var tenRole = role.ToString();
        var query = _db.NguoiDungs.Include(u => u.Role)
            .Where(u => u.Role.Name == tenRole && u.IsActive);
        if (boMonId.HasValue) query = query.Where(u => u.BoMonId == boMonId);
        var ids = await query.Select(u => u.Id).ToListAsync();
        foreach (var id in ids)
            _db.ThongBaos.Add(new ThongBaoHeThong
            {
                NguoiNhanId = id, LoaiThongBao = loai,
                TieuDe = tieuDe, NoiDung = noiDung, NgayTao = DateTime.UtcNow
            });
        await _db.SaveChangesAsync();
    }

    public async Task<List<ThongBaoResponse>> LayThongBaoCuaTuiAsync(int nguoiDungId, bool chuaDocThoi = false)
    {
        var query = _db.ThongBaos.Where(t => t.NguoiNhanId == nguoiDungId);
        if (chuaDocThoi) query = query.Where(t => !t.DaDoc);
        return await query.OrderByDescending(t => t.NgayTao).Take(50)
            .Select(t => new ThongBaoResponse
            {
                Id = t.Id, LoaiThongBao = t.LoaiThongBao.ToString(),
                TieuDe = t.TieuDe, NoiDung = t.NoiDung, VanBanId = t.VanBanId,
                DaDoc = t.DaDoc, NgayTao = t.NgayTao
            }).ToListAsync();
    }

    public async Task DanhDauDaDocAsync(int id, int nguoiDungId)
    {
        var t = await _db.ThongBaos.FirstOrDefaultAsync(x => x.Id == id && x.NguoiNhanId == nguoiDungId);
        if (t != null && !t.DaDoc) { t.DaDoc = true; t.NgayDoc = DateTime.UtcNow; await _db.SaveChangesAsync(); }
    }

    public async Task DanhDauTatCaDaDocAsync(int nguoiDungId)
    {
        var list = await _db.ThongBaos.Where(t => t.NguoiNhanId == nguoiDungId && !t.DaDoc).ToListAsync();
        foreach (var t in list) { t.DaDoc = true; t.NgayDoc = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
    }

    public async Task<int> DemChuaDocAsync(int nguoiDungId) =>
        await _db.ThongBaos.CountAsync(t => t.NguoiNhanId == nguoiDungId && !t.DaDoc);
}

// ═══════════════════════════════════════════════════════════════════
// AUTH SERVICE
// ═══════════════════════════════════════════════════════════════════
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private const int MaxSaiMatKhau = 5;
    private const int PhutKhoa = 15;

    public AuthService(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    public async Task<DangNhapResponse> DangNhapAsync(DangNhapRequest req, string ipAddress)
    {
        var u = await _db.NguoiDungs.Include(x => x.Role).Include(x => x.BoMon)
            .FirstOrDefaultAsync(x => x.Email == req.Email);
        if (u == null) return Loi("Email hoặc mật khẩu không đúng.");
        if (u.KhoaTaiKhoanDen.HasValue && u.KhoaTaiKhoanDen > DateTime.UtcNow)
            return Loi($"Tài khoản bị khóa đến {u.KhoaTaiKhoanDen:HH:mm dd/MM/yyyy}.");
        if (!u.IsActive) return Loi("Tài khoản bị vô hiệu hóa.");
        if (!BCrypt.Net.BCrypt.Verify(req.MatKhau, u.MatKhauHash))
        {
            u.SoLanDangNhapSai++;
            if (u.SoLanDangNhapSai >= MaxSaiMatKhau)
            { u.KhoaTaiKhoanDen = DateTime.UtcNow.AddMinutes(PhutKhoa); u.SoLanDangNhapSai = 0; }
            await _db.SaveChangesAsync();
            return Loi("Email hoặc mật khẩu không đúng.");
        }
        u.SoLanDangNhapSai = 0; u.KhoaTaiKhoanDen = null;
        u.LanDangNhapCuoi = DateTime.UtcNow;
        var (access, hetHan) = TaoAccessToken(u);
        var refresh = TaoRefreshToken();
        u.RefreshToken = refresh;
        u.RefreshTokenHetHan = DateTime.UtcNow.AddDays(_cfg.GetValue<int>("Jwt:RefreshTokenNgay", 30));
        await _db.SaveChangesAsync();
        return new DangNhapResponse { ThanhCong = true, ThongBao = "Đăng nhập thành công.",
            AccessToken = access, RefreshToken = refresh, HetHanLuc = hetHan, NguoiDung = MapUser(u) };
    }

    public async Task<DangNhapResponse> DangKyAsync(DangKyRequest req)
    {
        if (await _db.NguoiDungs.AnyAsync(u => u.Email == req.Email))
            return Loi("Email đã được sử dụng.");
        var roleGV = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "GiangVien")
            ?? throw new InvalidOperationException("Role GiangVien chưa được khởi tạo.");
        var u = new NguoiDung
        {
            HoTen = req.HoTen.Trim(), Email = req.Email.ToLower().Trim(),
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(req.MatKhau),
            RoleId = roleGV.Id, BoMonId = req.BoMonId,
            ChucDanh = req.ChucDanh, SoDienThoai = req.SoDienThoai, IsActive = true
        };
        _db.NguoiDungs.Add(u); await _db.SaveChangesAsync();
        return new DangNhapResponse { ThanhCong = true, ThongBao = "Đăng ký thành công. Vui lòng đăng nhập." };
    }

    public async Task<DangNhapResponse> LamMoiTokenAsync(string refreshToken, string ipAddress)
    {
        var u = await _db.NguoiDungs.Include(x => x.Role).Include(x => x.BoMon)
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken && x.RefreshTokenHetHan > DateTime.UtcNow && x.IsActive);
        if (u == null) return Loi("Refresh token không hợp lệ hoặc đã hết hạn.");
        var (access, hetHan) = TaoAccessToken(u);
        var newRefresh = TaoRefreshToken();
        u.RefreshToken = newRefresh;
        u.RefreshTokenHetHan = DateTime.UtcNow.AddDays(_cfg.GetValue<int>("Jwt:RefreshTokenNgay", 30));
        await _db.SaveChangesAsync();
        return new DangNhapResponse { ThanhCong = true, AccessToken = access, RefreshToken = newRefresh,
            HetHanLuc = hetHan, NguoiDung = MapUser(u) };
    }

    public async Task DangXuatAsync(int id)
    {
        var u = await _db.NguoiDungs.FindAsync(id);
        if (u != null) { u.RefreshToken = null; u.RefreshTokenHetHan = null; await _db.SaveChangesAsync(); }
    }

    public async Task<bool> DoiMatKhauAsync(int id, DoiMatKhauRequest req)
    {
        var u = await _db.NguoiDungs.FindAsync(id) ?? throw new KeyNotFoundException();
        if (!BCrypt.Net.BCrypt.Verify(req.MatKhauHienTai, u.MatKhauHash))
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        u.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(req.MatKhauMoi);
        u.NgayCapNhat = DateTime.UtcNow; await _db.SaveChangesAsync(); return true;
    }

    public async Task<bool> QuenMatKhauAsync(QuenMatKhauRequest req)
    {
        var u = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Email == req.Email && x.IsActive);
        if (u == null) return true;
        u.TokenDatLaiMatKhau = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        u.TokenDatLaiHetHan = DateTime.UtcNow.AddHours(2); await _db.SaveChangesAsync(); return true;
    }

    public async Task<bool> DatLaiMatKhauAsync(DatLaiMatKhauRequest req)
    {
        var u = await _db.NguoiDungs.FirstOrDefaultAsync(x =>
            x.Email == req.Email && x.TokenDatLaiMatKhau == req.Token && x.TokenDatLaiHetHan > DateTime.UtcNow)
            ?? throw new InvalidOperationException("Token không hợp lệ hoặc đã hết hạn.");
        u.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(req.MatKhauMoi);
        u.TokenDatLaiMatKhau = null; u.TokenDatLaiHetHan = null;
        u.NgayCapNhat = DateTime.UtcNow; await _db.SaveChangesAsync(); return true;
    }

    private (string token, DateTime expiry) TaoAccessToken(NguoiDung u)
    {
        var key = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key chưa cấu hình.");
        var expiry = DateTime.UtcNow.AddHours(_cfg.GetValue<int>("Jwt:AccessTokenGio", 8));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Email, u.Email),
            new Claim(ClaimTypes.Role, u.Role?.Name ?? ""),
            new Claim("HoTen", u.HoTen),
            new Claim("BoMonId", u.BoMonId?.ToString() ?? ""),
            new Claim("RoleId", u.RoleId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"], audience: _cfg["Jwt:Audience"],
            claims: claims, expires: expiry,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private static string TaoRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private static DangNhapResponse Loi(string msg) => new() { ThanhCong = false, ThongBao = msg };
    private static NguoiDungResponse MapUser(NguoiDung u) => new()
    {
        Id = u.Id, HoTen = u.HoTen, Email = u.Email, ChucDanh = u.ChucDanh,
        SoDienThoai = u.SoDienThoai, Role = u.Role?.Name ?? "",
        RoleTenHienThi = u.Role?.TenHienThi ?? "", BoMonId = u.BoMonId,
        TenBoMon = u.BoMon?.Ten, IsActive = u.IsActive, NgayTao = u.NgayTao, LanDangNhapCuoi = u.LanDangNhapCuoi
    };
}

// ═══════════════════════════════════════════════════════════════════
// NGUOI DUNG & BO MON & THONG KE & AUDIT SERVICE
// ═══════════════════════════════════════════════════════════════════
public class NguoiDungService : INguoiDungService
{
    private readonly AppDbContext _db;
    public NguoiDungService(AppDbContext db) { _db = db; }

    public async Task<NguoiDungResponse?> LayTheoIdAsync(int id)
    {
        var u = await _db.NguoiDungs.Include(x => x.Role).Include(x => x.BoMon).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return u == null ? null : Map(u);
    }

    public async Task<KetQuaPhanTrang<NguoiDungResponse>> LayDanhSachAsync(int? boMonId, RoleName? role, bool activeOnly, int trang, int kichThuoc)
    {
        var q = _db.NguoiDungs.Include(x => x.Role).Include(x => x.BoMon).AsNoTracking();
        if (activeOnly) q = q.Where(u => u.IsActive);
        if (boMonId.HasValue) q = q.Where(u => u.BoMonId == boMonId);
        if (role.HasValue) q = q.Where(u => u.Role.Name == role.ToString());
        var tong = await q.CountAsync();
        var items = await q.OrderBy(u => u.HoTen).Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToListAsync();
        return new KetQuaPhanTrang<NguoiDungResponse> { DuLieu = items.Select(Map).ToList(), TongSoBanGhi = tong, Trang = trang, KichThuocTrang = kichThuoc };
    }

    public async Task<NguoiDungResponse> CapNhatHoSoAsync(int id, CapNhatHoSoRequest req)
    {
        var u = await _db.NguoiDungs.Include(x => x.Role).Include(x => x.BoMon).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        u.HoTen = req.HoTen.Trim(); u.ChucDanh = req.ChucDanh; u.SoDienThoai = req.SoDienThoai; u.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync(); return Map(u);
    }

    public async Task<NguoiDungResponse> CapNhatRoleAsync(CapNhatRoleRequest req, int adminId)
    {
        var u = await _db.NguoiDungs.Include(x => x.Role).Include(x => x.BoMon).FirstOrDefaultAsync(x => x.Id == req.NguoiDungId)
            ?? throw new KeyNotFoundException();
        u.RoleId = req.RoleId; if (req.BoMonId.HasValue) u.BoMonId = req.BoMonId; u.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync(); await _db.Entry(u).Reference(x => x.Role).LoadAsync(); return Map(u);
    }

    public async Task KhoaTaiKhoanAsync(KhoaTaiKhoanRequest req, int adminId)
    {
        if (req.NguoiDungId == adminId) throw new InvalidOperationException("Không thể tự khóa tài khoản của mình.");
        var u = await _db.NguoiDungs.FindAsync(req.NguoiDungId) ?? throw new KeyNotFoundException();
        u.IsActive = req.IsActive; u.NgayCapNhat = DateTime.UtcNow; await _db.SaveChangesAsync();
    }

    private static NguoiDungResponse Map(NguoiDung u) => new()
    {
        Id = u.Id, HoTen = u.HoTen, Email = u.Email, ChucDanh = u.ChucDanh, SoDienThoai = u.SoDienThoai,
        Role = u.Role?.Name ?? "", RoleTenHienThi = u.Role?.TenHienThi ?? "",
        BoMonId = u.BoMonId, TenBoMon = u.BoMon?.Ten, IsActive = u.IsActive, NgayTao = u.NgayTao, LanDangNhapCuoi = u.LanDangNhapCuoi
    };
}

public class BoMonService : IBoMonService
{
    private readonly AppDbContext _db;
    public BoMonService(AppDbContext db) { _db = db; }

    public async Task<List<BoMonResponse>> LayDanhSachAsync(bool activeOnly = true)
    {
        var q = _db.BoMons.Include(b => b.TruongBoMon).AsNoTracking();
        if (activeOnly) q = q.Where(b => b.IsActive);
        var list = await q.OrderBy(b => b.Ten).ToListAsync();
        var dem = await _db.NguoiDungs.Where(u => u.BoMonId != null).GroupBy(u => u.BoMonId)
            .Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id!.Value, x => x.Count);
        return list.Select(b => new BoMonResponse { Id = b.Id, Ten = b.Ten, MaBoMon = b.MaBoMon,
            TruongBoMonId = b.TruongBoMonId, TenTruongBoMon = b.TruongBoMon?.HoTen,
            IsActive = b.IsActive, SoThanhVien = dem.GetValueOrDefault(b.Id, 0) }).ToList();
    }

    public async Task<BoMonResponse?> LayTheoIdAsync(int id)
    {
        var b = await _db.BoMons.Include(x => x.TruongBoMon).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return null;
        var dem = await _db.NguoiDungs.CountAsync(u => u.BoMonId == id);
        return new BoMonResponse { Id = b.Id, Ten = b.Ten, MaBoMon = b.MaBoMon, TruongBoMonId = b.TruongBoMonId, TenTruongBoMon = b.TruongBoMon?.HoTen, IsActive = b.IsActive, SoThanhVien = dem };
    }

    public async Task<BoMonResponse> TaoAsync(TaoBoMonRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.MaBoMon) && await _db.BoMons.AnyAsync(b => b.MaBoMon == req.MaBoMon))
            throw new InvalidOperationException($"Mã '{req.MaBoMon}' đã tồn tại.");
        var b = new BoMon { Ten = req.Ten.Trim(), MaBoMon = req.MaBoMon?.ToUpper(), TruongBoMonId = req.TruongBoMonId };
        _db.BoMons.Add(b); await _db.SaveChangesAsync();
        return (await LayTheoIdAsync(b.Id))!;
    }

    public async Task<BoMonResponse> CapNhatAsync(int id, TaoBoMonRequest req)
    {
        var b = await _db.BoMons.FindAsync(id) ?? throw new KeyNotFoundException();
        b.Ten = req.Ten.Trim(); b.MaBoMon = req.MaBoMon?.ToUpper(); b.TruongBoMonId = req.TruongBoMonId;
        await _db.SaveChangesAsync(); return (await LayTheoIdAsync(id))!;
    }

    public async Task XoaAsync(int id)
    {
        var b = await _db.BoMons.FindAsync(id) ?? throw new KeyNotFoundException();
        if (await _db.NguoiDungs.AnyAsync(u => u.BoMonId == id))
            throw new InvalidOperationException("Không thể xóa bộ môn đang có thành viên.");
        b.IsActive = false; await _db.SaveChangesAsync();
    }
}

public class ThongKeService : IThongKeService
{
    private readonly AppDbContext _db;
    public ThongKeService(AppDbContext db) { _db = db; }

    public async Task<ThongKeResponse> LayThongKeAsync(DateTime tuNgay, DateTime denNgay, int? boMonId = null)
    {
        var q = _db.VanBans.Include(v => v.BoMon).AsNoTracking()
            .Where(v => v.NgayTao >= tuNgay && v.NgayTao <= denNgay.AddDays(1));
        if (boMonId.HasValue) q = q.Where(v => v.BoMonId == boMonId);
        var list = await q.ToListAsync();
        return new ThongKeResponse
        {
            TongVanBan = list.Count, BanNhap = list.Count(v => v.TrangThai == DocumentStatus.Draft),
            ChoTruongBMDuyet = list.Count(v => v.TrangThai == DocumentStatus.PendingDepartment),
            ChoLanhDaoDuyet = list.Count(v => v.TrangThai == DocumentStatus.PendingFaculty),
            DaDuyet = list.Count(v => v.TrangThai == DocumentStatus.Approved),
            DaBanHanh = list.Count(v => v.TrangThai == DocumentStatus.Issued),
            DaPhanPhoi = list.Count(v => v.TrangThai == DocumentStatus.Distributed),
            BiBacBo = list.Count(v => v.BuocBiTuChoiCuoi != null),
            QuaHan = list.Count(v => v.HanXuLy.HasValue && v.HanXuLy < DateTime.UtcNow && v.TrangThai < DocumentStatus.Approved),
            TheoLoaiVanBan = list.GroupBy(v => VanBanService.MapLoaiVanBan(v.LoaiVanBan)).ToDictionary(g => g.Key, g => g.Count()),
            TheoBoMon = list.Where(v => v.BoMon != null).GroupBy(v => v.BoMon!.Ten).ToDictionary(g => g.Key, g => g.Count()),
            TheoThang = list.GroupBy(v => v.NgayTao.ToString("yyyy-MM")).ToDictionary(g => g.Key, g => g.Count()),
            TuNgay = tuNgay, DenNgay = denNgay
        };
    }

    public async Task<List<VanBanTomTatResponse>> LayVanBanQuaHanAsync() =>
        await _db.VanBans.Include(v => v.NguoiTao).Include(v => v.PhienBans).Include(v => v.SoHieu)
            .Where(v => v.HanXuLy.HasValue && v.HanXuLy < DateTime.UtcNow && v.TrangThai < DocumentStatus.Approved)
            .OrderBy(v => v.HanXuLy).AsNoTracking().ToListAsync().ContinueWith(t => t.Result.Select(v => new VanBanTomTatResponse
            {
                Id = v.Id, TieuDe = v.TieuDe, LoaiVanBan = v.LoaiVanBan.ToString(),
                LoaiVanBanHienThi = VanBanService.MapLoaiVanBan(v.LoaiVanBan),
                TrangThai = v.TrangThai.ToString(), TrangThaiHienThi = VanBanService.MapTrangThai(v.TrangThai),
                CanCapSoHieu = WorkflowService.CanCapSoHieu(v.LoaiVanBan),
                IsLocked = v.IsLocked, TenNguoiTao = v.NguoiTao?.HoTen,
                HanXuLy = v.HanXuLy, QuaHan = true, NgayTao = v.NgayTao
            }).ToList());
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) { _db = db; }
    public async Task GhiAsync(int? nguoiDungId, string hanhDong, string? loaiDoiTuong = null,
        int? doiTuongId = null, string? giaTriCu = null, string? giaTriMoi = null, string? ipAddress = null)
    {
        _db.NhatKyKiemToans.Add(new NhatKyKiemToan
        {
            NguoiDungId = nguoiDungId, HanhDong = hanhDong, LoaiDoiTuong = loaiDoiTuong,
            DoiTuongId = doiTuongId, GiaTriCu = giaTriCu, GiaTriMoi = giaTriMoi,
            DiaChi_IP = ipAddress, ThoiGian = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
