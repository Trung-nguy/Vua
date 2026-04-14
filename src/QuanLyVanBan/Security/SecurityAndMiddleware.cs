using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuanLyVanBan.Helpers;
using QuanLyVanBan.Models.Enums;

namespace QuanLyVanBan.Security {

/// <summary>
/// Kiểm tra role dựa trên JWT claim, không cần Authorize policy phức tạp.
/// Dùng tên role theo enum RoleName.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class YeuCauRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public YeuCauRoleAttribute(params RoleName[] roles)
    {
        _roles = roles.Select(r => r.ToString()).ToArray();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any()) return;

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new { thanhCong = false, thongBao = "Chưa đăng nhập." });
            return;
        }

        var role = user.LayRole();
        if (!_roles.Contains(role))
            context.Result = new ObjectResult(new { thanhCong = false, thongBao = "Bạn không có quyền thực hiện thao tác này." })
                { StatusCode = StatusCodes.Status403Forbidden };
    }
}

}

namespace QuanLyVanBan.Middleware {

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xử lý: {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await XuLyLoi(ctx, ex);
        }
    }

    private static async Task XuLyLoi(HttpContext ctx, Exception ex)
    {
        var (status, msg) = ex switch
        {
            KeyNotFoundException       => (404, ex.Message),
            UnauthorizedAccessException=> (403, ex.Message),
            InvalidOperationException  => (400, ex.Message),
            ArgumentException          => (400, ex.Message),
            FileNotFoundException      => (404, "File không tồn tại."),
            _                          => (500, "Lỗi hệ thống. Vui lòng thử lại sau.")
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        var body = JsonSerializer.Serialize(new { thanhCong = false, thongBao = msg, maLoi = status },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await ctx.Response.WriteAsync(body);
    }
}
}

