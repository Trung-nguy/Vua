using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuanLyVanBan.Contracts;
using QuanLyVanBan.Data;
using QuanLyVanBan.Helpers;
using QuanLyVanBan.Middleware;
using QuanLyVanBan.Services;
using Serilog;
using Serilog.Events;

// ── Serilog khởi tạo sớm ─────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    builder.WebHost.UseUrls("https://localhost:5001");
    builder.Services.AddHttpsRedirection(options =>
    {
        options.HttpsPort = 5001;
    });

    // ── Controllers ──────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            // camelCase cho tất cả response
            o.JsonSerializerOptions.PropertyNamingPolicy =
                System.Text.Json.JsonNamingPolicy.CamelCase;
            // Không trả về null trong JSON
            o.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });
    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger ──────────────────────────────────────────────────
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Hệ Thống Quản Lý Văn Bản – API",
            Version = "v1",
            Description = """
                **Backend cho hệ thống quản lý văn bản – Khoa Kỹ thuật Máy tính**

                ## Quy trình 7 bước:
                1. **Soạn thảo** (GiangVien / VanThuKhoa) → `POST /api/van-ban`
                2. **Xác minh chuyên môn** (TruongBoMon) → `POST /api/workflow/xac-minh-chuyen-mon`
                   *(VanThuKhoa bỏ qua bước này)*
                3. **Phê duyệt cuối** (LanhDaoKhoa) → `POST /api/workflow/phe-duyet-cuoi`
                   *→ File bị KHÓA ngay sau khi duyệt*
                4. **Chỉnh sửa** (nếu bị từ chối) → `PUT /api/van-ban/{id}/chinh-sua`
                   *(Rẽ nhánh, không phải bước tuần tự)*
                5. **Cấp số hiệu** (VanThuKhoa) → `POST /api/so-hieu/cap-so`
                   *(Chỉ QuyetDinh, ThongBao, CongVan)*
                6. **Phân phối & Lưu trữ** (VanThuKhoa) → `POST /api/phan-phoi`
                7. **Tổng hợp & Báo cáo** → `GET /api/thong-ke`

                ## Tài khoản mẫu sau seed:
                | Email | Mật khẩu | Role |
                |---|---|---|
                | admin@khoa.edu.vn | Admin@123456 | Admin |
                | nvkhoa.ldkhoa@khoa.edu.vn | LdKhoa@123456 | Lãnh đạo Khoa |
                | lvan.tbm@khoa.edu.vn | TBM@123456 | Trưởng BM |
                | ntcam.vanThu@khoa.edu.vn | VanThu@123456 | Văn thư Khoa |
                | hvdung.gv@khoa.edu.vn | GVien@123456 | Giảng viên |
                """
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập: Bearer {access_token}"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── CORS ─────────────────────────────────────────────────────
    builder.Services.AddCors(o =>
    {
        o.AddPolicy("AllowFrontend", p =>
        {
            var origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()
                ?? new[] { "http://localhost:3000", "http://localhost:5173" };
            p.WithOrigins(origins)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        });
    });

    // ── Database ─────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.CommandTimeout(30)
        )
    );

    // ── JWT Authentication ────────────────────────────────────────
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key trong appsettings.");

    if (jwtKey.Length < 32)
        throw new InvalidOperationException("Jwt:Key phải có ít nhất 32 ký tự.");

    builder.Services.AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.FromSeconds(30)
        };

        // Trả JSON thay vì HTML redirect
        o.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync(
                    "{\"thanhCong\":false,\"thongBao\":\"Chưa đăng nhập hoặc token hết hạn.\",\"maLoi\":401}");
            },
            OnForbidden = ctx =>
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync(
                    "{\"thanhCong\":false,\"thongBao\":\"Bạn không có quyền thực hiện thao tác này.\",\"maLoi\":403}");
            }
        };
    });

    builder.Services.AddAuthorization();

    // ── Dependency Injection ──────────────────────────────────────
    builder.Services.AddScoped<IAuthService,      AuthService>();
    builder.Services.AddScoped<IVanBanService,    VanBanService>();
    builder.Services.AddScoped<IWorkflowService,  WorkflowService>();
    builder.Services.AddScoped<ISoHieuService,    SoHieuService>();
    builder.Services.AddScoped<IPhanPhoiService,  PhanPhoiService>();
    builder.Services.AddScoped<INguoiDungService, NguoiDungService>();
    builder.Services.AddScoped<IBoMonService,     BoMonService>();
    builder.Services.AddScoped<IThongKeService,   ThongKeService>();
    builder.Services.AddScoped<IThongBaoService,  ThongBaoService>();
    builder.Services.AddScoped<IAuditService,     AuditService>();
    builder.Services.AddScoped<FileHelper>();

    // ── Health Check ──────────────────────────────────────────────
    builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

    // ════════════════════════════════════════════════════════════
    var app = builder.Build();
    // ════════════════════════════════════════════════════════════

    // Migrate DB và Seed dữ liệu mẫu khi khởi động
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
        Log.Information("Database sẵn sàng.");
    }

    // ── Pipeline ──────────────────────────────────────────────────
    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuanLyVanBan v1");
        c.RoutePrefix = string.Empty; // Swagger tại root: https://localhost:5001
        c.DisplayRequestDuration();
    });

    app.UseSerilogRequestLogging(o =>
    {
        o.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
    });

    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    app.UseStaticFiles();        // Phục vụ wwwroot/uploads
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("API khởi động – Môi trường: {Env}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API khởi động thất bại.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
