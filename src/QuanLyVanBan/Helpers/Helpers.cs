using System.Security.Claims;
using System.Security.Cryptography;

namespace QuanLyVanBan.Helpers;

public static class ClaimsExtensions
{
    public static int LayNguoiDungId(this ClaimsPrincipal user)
    {
        var val = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Token không hợp lệ.");
        return int.Parse(val);
    }
    public static string LayEmail(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Email) ?? "";
    public static string LayRole(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role) ?? "";
    public static int? LayBoMonId(this ClaimsPrincipal user)
    {
        var val = user.FindFirstValue("BoMonId");
        return int.TryParse(val, out var id) && id > 0 ? id : null;
    }
    public static string LayHoTen(this ClaimsPrincipal user) => user.FindFirstValue("HoTen") ?? "";
}

public class FileHelper
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<FileHelper> _logger;

    private static readonly HashSet<string> DinhDangChoPhep = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".jpg", ".jpeg", ".png" };

    public FileHelper(IConfiguration cfg, ILogger<FileHelper> logger) { _cfg = cfg; _logger = logger; }

    public async Task<(string DuongDan, long KichThuoc, string Checksum)> LuuFileAsync(
        Microsoft.AspNetCore.Http.IFormFile file, string subFolder)
    {
        var maxSize = _cfg.GetValue<long>("FileStorage:KichThuocToiDaBytes", 10_485_760);
        if (file.Length > maxSize)
            throw new InvalidOperationException($"File quá lớn. Tối đa {maxSize / 1024 / 1024} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!DinhDangChoPhep.Contains(ext))
            throw new InvalidOperationException($"Định dạng '{ext}' không được phép. Cho phép: {string.Join(", ", DinhDangChoPhep)}");

        var root = _cfg["FileStorage:DuongDanLuu"] ?? "wwwroot/uploads";
        var folder = Path.Combine(root, subFolder, DateTime.UtcNow.ToString("yyyy/MM"));
        Directory.CreateDirectory(folder);

        var tenFile = $"{DateTime.UtcNow:yyyyMMddHHmmssffff}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, tenFile);

        using var md5 = MD5.Create();
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await using var hashStream = new CryptoStream(stream, md5, CryptoStreamMode.Write);
        await file.CopyToAsync(hashStream);
        await hashStream.FlushFinalBlockAsync();

        var checksum = Convert.ToHexString(md5.Hash!).ToLowerInvariant();
        var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        _logger.LogInformation("File saved: {Path} ({Size} bytes, MD5: {MD5})", relative, file.Length, checksum);
        return (relative, file.Length, checksum);
    }

    public async Task<byte[]> DocFileAsync(string relativePath)
    {
        var root = _cfg["FileStorage:DuongDanLuu"] ?? "wwwroot/uploads";
        var full = Path.Combine(root, relativePath);
        if (!File.Exists(full)) throw new FileNotFoundException($"File không tồn tại: {relativePath}");
        return await File.ReadAllBytesAsync(full);
    }

    public void XoaFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var root = _cfg["FileStorage:DuongDanLuu"] ?? "wwwroot/uploads";
        var full = Path.Combine(root, relativePath);
        if (File.Exists(full)) { File.Delete(full); _logger.LogInformation("File deleted: {Path}", relativePath); }
    }
}
