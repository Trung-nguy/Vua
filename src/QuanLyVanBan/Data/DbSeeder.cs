using QuanLyVanBan.Models;
using QuanLyVanBan.Models.Enums;

namespace QuanLyVanBan.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Roles.Any()) return; // Đã seed rồi

        // ── 1. Roles ─────────────────────────────────────────────────────────
        var roles = new List<Role>
        {
            new() { Name = "GiangVien",    TenHienThi = "Giảng viên" },
            new() { Name = "VanThuKhoa",   TenHienThi = "Văn thư Khoa" },
            new() { Name = "TruongBoMon",  TenHienThi = "Trưởng Bộ môn" },
            new() { Name = "LanhDaoKhoa",  TenHienThi = "Lãnh đạo Khoa" },
            new() { Name = "Admin",        TenHienThi = "Quản trị hệ thống" }
        };
        db.Roles.AddRange(roles);
        await db.SaveChangesAsync();

        // ── 2. Bộ môn (chưa có Trưởng BM – sẽ gán sau khi có user) ─────────
        var boMons = new List<BoMon>
        {
            new() { Ten = "Bộ môn Khoa học Máy tính",  MaBoMon = "KHMT" },
            new() { Ten = "Bộ môn Kỹ thuật Máy tính",  MaBoMon = "KTMT" },
            new() { Ten = "Bộ môn Hệ thống Thông tin", MaBoMon = "HTTT" },
            new() { Ten = "Văn phòng Khoa",             MaBoMon = "VPK" }
        };
        db.BoMons.AddRange(boMons);
        await db.SaveChangesAsync();

        var roleMap = db.Roles.ToDictionary(r => r.Name, r => r.Id);
        var boMonMap = db.BoMons.ToDictionary(b => b.MaBoMon!, b => b.Id);

        // ── 3. Người dùng ────────────────────────────────────────────────────
        var users = new List<NguoiDung>
        {
            // Admin
            new()
            {
                HoTen = "Quản Trị Viên", Email = "admin@khoa.edu.vn",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                RoleId = roleMap["Admin"], IsActive = true
            },
            // Lãnh đạo Khoa
            new()
            {
                HoTen = "PGS.TS. Nguyễn Văn Khoa", Email = "nvkhoa.ldkhoa@khoa.edu.vn",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("LdKhoa@123456"),
                RoleId = roleMap["LanhDaoKhoa"], BoMonId = boMonMap["VPK"],
                ChucDanh = "Trưởng Khoa", IsActive = true
            },
            new()
            {
                HoTen = "TS. Trần Thị Minh", Email = "ttminh.ldkhoa@khoa.edu.vn",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("LdKhoa@123456"),
                RoleId = roleMap["LanhDaoKhoa"], BoMonId = boMonMap["VPK"],
                ChucDanh = "Phó Trưởng Khoa", IsActive = true
            },
            // Trưởng Bộ môn
            new()
            {
                HoTen = "TS. Lê Văn An", Email = "lvan.tbm@khoa.edu.vn",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("TBM@123456"),
                RoleId = roleMap["TruongBoMon"], BoMonId = boMonMap["KHMT"],
                ChucDanh = "Tiến sĩ", IsActive = true
            },
            new()
            {
                HoTen = "TS. Phạm Thị Bình", Email = "ptbinh.tbm@khoa.edu.vn",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("TBM@123456"),
                RoleId = roleMap["TruongBoMon"], BoMonId = boMonMap["KTMT"],
                ChucDanh = "Tiến sĩ", IsActive = true
            },
            // Văn thư Khoa
            new()
            {
                HoTen = "Nguyễn Thị Cam", Email = "ntcam.vanThu@khoa.edu.vn",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("VanThu@123456"),
                RoleId = roleMap["VanThuKhoa"], BoMonId = boMonMap["VPK"],
                ChucDanh = "Cán bộ Văn phòng", IsActive = true
            },
            // Giảng viên
            new()
            {
                HoTen = "ThS. Hoàng Văn Dũng", Email = "hvdung.gv@khoa.edu.vn",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("GVien@123456"),
                RoleId = roleMap["GiangVien"], BoMonId = boMonMap["KHMT"],
                ChucDanh = "Thạc sĩ", IsActive = true
            },
            new()
            {
                HoTen = "ThS. Vũ Thị Giang", Email = "vtgiang.gv@khoa.edu.vn",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("GVien@123456"),
                RoleId = roleMap["GiangVien"], BoMonId = boMonMap["KTMT"],
                ChucDanh = "Thạc sĩ", IsActive = true
            }
        };
        db.NguoiDungs.AddRange(users);
        await db.SaveChangesAsync();

        // ── 4. Gán Trưởng BM cho Bộ môn ─────────────────────────────────────
        var userMap = db.NguoiDungs.ToDictionary(u => u.Email, u => u.Id);

        var bmKHMT = db.BoMons.First(b => b.MaBoMon == "KHMT");
        bmKHMT.TruongBoMonId = userMap["lvan.tbm@khoa.edu.vn"];

        var bmKTMT = db.BoMons.First(b => b.MaBoMon == "KTMT");
        bmKTMT.TruongBoMonId = userMap["ptbinh.tbm@khoa.edu.vn"];

        await db.SaveChangesAsync();
    }
}
