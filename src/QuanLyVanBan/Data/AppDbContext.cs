using Microsoft.EntityFrameworkCore;
using QuanLyVanBan.Models;
using QuanLyVanBan.Models.Enums;

namespace QuanLyVanBan.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<BoMon> BoMons => Set<BoMon>();
    public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
    public DbSet<VanBan> VanBans => Set<VanBan>();
    public DbSet<PhienBanFile> PhienBanFiles => Set<PhienBanFile>();
    public DbSet<BuocPheDuyet> BuocPheDuyets => Set<BuocPheDuyet>();
    public DbSet<NhatKyWorkflow> NhatKyWorkflows => Set<NhatKyWorkflow>();
    public DbSet<SoHieuVanBan> SoHieuVanBans => Set<SoHieuVanBan>();
    public DbSet<BoDemSoHieu> BoDemSoHieus => Set<BoDemSoHieu>();
    public DbSet<NguoiNhanVanBan> NguoiNhanVanBans => Set<NguoiNhanVanBan>();
    public DbSet<ThongBaoHeThong> ThongBaos => Set<ThongBaoHeThong>();
    public DbSet<NhatKyKiemToan> NhatKyKiemToans => Set<NhatKyKiemToan>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── Role ───────────────────────────────────────────────────────────────
        mb.Entity<Role>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Name).HasMaxLength(50);
            e.Property(r => r.TenHienThi).HasMaxLength(100);
        });

        // ── BoMon ──────────────────────────────────────────────────────────────
        mb.Entity<BoMon>(e =>
        {
            e.Property(b => b.Ten).HasMaxLength(200);
            e.Property(b => b.MaBoMon).HasMaxLength(20);
            e.HasIndex(b => b.MaBoMon).IsUnique().HasFilter("[MaBoMon] IS NOT NULL");

            // Trưởng BM: quan hệ 1-1 tùy chọn
            e.HasOne(b => b.TruongBoMon)
             .WithOne()
             .HasForeignKey<BoMon>(b => b.TruongBoMonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── NguoiDung ──────────────────────────────────────────────────────────
        mb.Entity<NguoiDung>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.HoTen).HasMaxLength(200);
            e.Property(u => u.ChucDanh).HasMaxLength(100);
            e.Property(u => u.SoDienThoai).HasMaxLength(20);

            e.HasOne(u => u.Role)
             .WithMany()
             .HasForeignKey(u => u.RoleId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(u => u.BoMon)
             .WithMany()
             .HasForeignKey(u => u.BoMonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── VanBan ─────────────────────────────────────────────────────────────
        mb.Entity<VanBan>(e =>
        {
            e.Property(v => v.TieuDe).HasMaxLength(500);
            e.Property(v => v.MoTa).HasMaxLength(2000);
            e.Property(v => v.BuocBiTuChoiCuoi).HasMaxLength(20);

            e.HasOne(v => v.NguoiTao)
             .WithMany()
             .HasForeignKey(v => v.NguoiTaoId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(v => v.BoMon)
             .WithMany()
             .HasForeignKey(v => v.BoMonId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            // Index tìm kiếm nhanh
            e.HasIndex(v => v.TrangThai);
            e.HasIndex(v => v.NguoiTaoId);
            e.HasIndex(v => v.BoMonId);
            e.HasIndex(v => v.NgayTao);
            e.HasIndex(v => v.LoaiVanBan);
        });

        // ── PhienBanFile ───────────────────────────────────────────────────────
        mb.Entity<PhienBanFile>(e =>
        {
            // Unique: mỗi văn bản chỉ có 1 phiên bản với số cụ thể
            e.HasIndex(p => new { p.VanBanId, p.SoPhienBan }).IsUnique();
            e.Property(p => p.TenFile).HasMaxLength(500);
            e.Property(p => p.ContentType).HasMaxLength(100);
            e.Property(p => p.GhiChuChinhSua).HasMaxLength(500);

            e.HasOne(p => p.VanBan)
             .WithMany(v => v.PhienBans)
             .HasForeignKey(p => p.VanBanId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.NguoiUpload)
             .WithMany()
             .HasForeignKey(p => p.NguoiUploadId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BuocPheDuyet ───────────────────────────────────────────────────────
        mb.Entity<BuocPheDuyet>(e =>
        {
            // Unique: mỗi văn bản chỉ có 1 bước với thứ tự cụ thể
            e.HasIndex(b => new { b.VanBanId, b.ThuTu }).IsUnique();
            e.Property(b => b.TenBuoc).HasMaxLength(200);
            e.Property(b => b.YKien).HasMaxLength(1000);

            e.HasOne(b => b.VanBan)
             .WithMany(v => v.BuocPheDuyets)
             .HasForeignKey(b => b.VanBanId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.NguoiDuocGiao)
             .WithMany()
             .HasForeignKey(b => b.NguoiDuocGiaoId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(b => b.NguoiXuLy)
             .WithMany()
             .HasForeignKey(b => b.NguoiXuLyId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── NhatKyWorkflow ─────────────────────────────────────────────────────
        mb.Entity<NhatKyWorkflow>(e =>
        {
            e.Property(n => n.HanhDong).HasMaxLength(100);
            e.Property(n => n.GhiChu).HasMaxLength(1000);
            e.Property(n => n.DiaChi_IP).HasMaxLength(50);

            e.HasOne(n => n.VanBan)
             .WithMany(v => v.NhatKyWorkflows)
             .HasForeignKey(n => n.VanBanId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.NguoiThucHien)
             .WithMany()
             .HasForeignKey(n => n.NguoiThucHienId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(n => new { n.VanBanId, n.ThoiGian });
        });

        // ── SoHieuVanBan ───────────────────────────────────────────────────────
        mb.Entity<SoHieuVanBan>(e =>
        {
            e.HasIndex(s => s.SoHieu).IsUnique();
            e.HasIndex(s => new { s.Nam, s.LoaiVanBan, s.SoThuTu }).IsUnique();
            e.Property(s => s.SoHieu).HasMaxLength(50);
            e.Property(s => s.NguoiKy).HasMaxLength(200);
            e.Property(s => s.ChucVuKy).HasMaxLength(100);

            // Quan hệ 1-1 với VanBan
            e.HasOne(s => s.VanBan)
             .WithOne(v => v.SoHieu)
             .HasForeignKey<SoHieuVanBan>(s => s.VanBanId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.NguoiCapSo)
             .WithMany()
             .HasForeignKey(s => s.NguoiCapSoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BoDemSoHieu ────────────────────────────────────────────────────────
        mb.Entity<BoDemSoHieu>(e =>
        {
            e.HasIndex(b => new { b.Nam, b.LoaiVanBan }).IsUnique();
        });

        // ── NguoiNhanVanBan ────────────────────────────────────────────────────
        mb.Entity<NguoiNhanVanBan>(e =>
        {
            // Mỗi người chỉ nhận 1 văn bản 1 lần
            e.HasIndex(n => new { n.VanBanId, n.NguoiNhanId }).IsUnique();
            e.Property(n => n.GhiChuTiepNhan).HasMaxLength(500);

            e.HasOne(n => n.VanBan)
             .WithMany(v => v.NguoiNhans)
             .HasForeignKey(n => n.VanBanId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.NguoiNhan)
             .WithMany()
             .HasForeignKey(n => n.NguoiNhanId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(n => n.NguoiPhanPhoi)
             .WithMany()
             .HasForeignKey(n => n.NguoiPhanPhoiId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ThongBaoHeThong ────────────────────────────────────────────────────
        mb.Entity<ThongBaoHeThong>(e =>
        {
            e.Property(t => t.TieuDe).HasMaxLength(200);
            e.Property(t => t.NoiDung).HasMaxLength(1000);

            e.HasOne(t => t.NguoiNhan)
             .WithMany()
             .HasForeignKey(t => t.NguoiNhanId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => new { t.NguoiNhanId, t.DaDoc });
        });

        // ── NhatKyKiemToan ─────────────────────────────────────────────────────
        mb.Entity<NhatKyKiemToan>(e =>
        {
            e.Property(n => n.HanhDong).HasMaxLength(100);
            e.Property(n => n.LoaiDoiTuong).HasMaxLength(50);
            e.Property(n => n.DiaChi_IP).HasMaxLength(50);
            e.Property(n => n.UserAgent).HasMaxLength(500);

            e.HasOne(n => n.NguoiDung)
             .WithMany()
             .HasForeignKey(n => n.NguoiDungId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(n => n.ThoiGian);
        });
    }
}
