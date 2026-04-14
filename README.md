# Hệ Thống Quản Lý Văn Bản – Backend API

> **Cơ sở áp dụng**: Khoa Kỹ thuật Máy tính – Đại học Bách Khoa Hà Nội  
> **Công nghệ**: ASP.NET Core 8, Entity Framework Core, SQLite, JWT

---

## Cấu Trúc Thư Mục

```
src/QuanLyVanBan/
├── Controllers/
│   └── AllControllers.cs        # 8 Controllers: Auth, VanBan, Workflow,
│                                #   SoHieu, PhanPhoi, NguoiDung, BoMon,
│                                #   ThongBao, ThongKe
├── Services/
│   ├── WorkflowService.cs       # Logic nghiệp vụ cốt lõi (phức tạp nhất)
│   └── AllServices.cs           # VanBanService, SoHieuService, PhanPhoiService,
│                                #   ThongBaoService, AuthService, NguoiDungService,
│                                #   BoMonService, ThongKeService, AuditService
├── Contracts/
│   └── IServices.cs             # 10 interfaces đầy đủ
├── Models/
│   ├── Entities.cs              # Tất cả entity: VanBan, NguoiDung, BuocPheDuyet...
│   └── Enums/Enums.cs           # DocumentStatus, DocumentType, RoleName...
├── DTOs/
│   └── AllDTOs.cs               # Requests + Responses (một file gọn)
├── Data/
│   ├── AppDbContext.cs          # EF Core với đầy đủ cấu hình quan hệ
│   └── DbSeeder.cs              # Dữ liệu mẫu 5 role, 4 bộ môn, 8 user
├── Helpers/
│   └── Helpers.cs               # ClaimsExtensions, FileHelper (MD5 checksum)
├── Security/
│   └── SecurityAndMiddleware.cs # YeuCauRoleAttribute, ExceptionMiddleware
├── Program.cs                   # DI, JWT, CORS, Serilog, Swagger
├── appsettings.json
└── appsettings.Development.json
```

---

## Quy Trình Nghiệp Vụ (7 Bước)

```
┌─────────────────────────────────────────────────────────────────┐
│                    LUỒNG CHÍNH (Happy Path)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [1] Soạn thảo & Đề nghị duyệt                                  │
│      GiangVien / VanThuKhoa tạo văn bản + upload file           │
│      → POST /api/van-ban    (multipart/form-data)                │
│      → POST /api/workflow/nop                                    │
│                                                                  │
│  [2] Xác minh chuyên môn ── Chỉ áp dụng cho GiangVien           │
│      TruongBoMon của đúng Bộ môn xem xét                         │
│      → POST /api/workflow/xac-minh-chuyen-mon                    │
│      ⚠️ VanThuKhoa BỎ QUA bước này → thẳng Bước 3              │
│                                                                  │
│  [3] Phê duyệt cuối cùng                                         │
│      LanhDaoKhoa (Trưởng/Phó Khoa) ký duyệt điện tử             │
│      → POST /api/workflow/phe-duyet-cuoi                         │
│      🔒 FILE BỊ KHÓA NGAY SAU BƯỚC NÀY                         │
│                                                                  │
│  [5] Ban hành & Cấp số ── Chỉ QuyetDinh, ThongBao, CongVan     │
│      VanThuKhoa cấp số hiệu: 05/2025/TB-KCN                     │
│      → POST /api/so-hieu/cap-so                                  │
│      ⚠️ ToTrinh, PhieuDeXuat, BaoCao: bỏ qua Bước 5            │
│                                                                  │
│  [6] Lưu trữ & Phân phối                                         │
│      VanThuKhoa / LanhDaoKhoa gửi đến người nhận                 │
│      → POST /api/phan-phoi                                       │
│      Người nhận xác nhận: POST /api/phan-phoi/xac-nhan-tiep-nhan│
│                                                                  │
│  [7] Tổng hợp & Báo cáo                                          │
│      → GET /api/thong-ke                                         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                  NHÁNH TỪ CHỐI (Không phải bước tuần tự)        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  TuChoiBM   → Draft (BuocBiTuChoiCuoi = "Department")           │
│  TuChoiKhoa → Draft (BuocBiTuChoiCuoi = "Faculty")              │
│                                                                  │
│  Người tạo chỉnh sửa + upload file mới (v2, v3...)              │
│  → PUT /api/van-ban/{id}/chinh-sua                               │
│                                                                  │
│  Nộp lại → Hệ thống TỰ ĐỊNH TUYẾN:                             │
│    BuocBiTuChoiCuoi = "Department" → Bước 2 (TruongBoMon)       │
│    BuocBiTuChoiCuoi = "Faculty"    → Bước 3 (LanhDaoKhoa)       │
│  → POST /api/workflow/nop-lai                                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Phân Quyền Chi Tiết

| Role | Mô tả | Quyền chính |
|------|--------|-------------|
| `GiangVien` | Giảng viên | Tạo VB, nộp → Bước 2, chỉnh sửa, tải xuống VB của mình |
| `VanThuKhoa` | Văn thư Khoa | Tạo VB (bỏ qua Bước 2), cấp số hiệu, phân phối, thống kê |
| `TruongBoMon` | Trưởng Bộ môn | Xác minh chuyên môn Bước 2 (đúng BM của mình) |
| `LanhDaoKhoa` | Lãnh đạo Khoa | Phê duyệt cuối Bước 3, từ chối, phân phối |
| `Admin` | Quản trị | Toàn quyền |

---

## API Endpoints Đầy Đủ

### Auth `/api/auth`
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/dang-nhap` | Đăng nhập → Access Token + Refresh Token |
| POST | `/dang-ky` | Đăng ký (role mặc định: GiangVien) |
| POST | `/lam-moi-token` | Làm mới token không cần đăng nhập lại |
| POST | `/dang-xuat` | Đăng xuất (huỷ Refresh Token) |
| POST | `/doi-mat-khau` | Đổi mật khẩu |
| POST | `/quen-mat-khau` | Gửi link reset qua email |
| POST | `/dat-lai-mat-khau` | Đặt lại mật khẩu bằng token |

### Văn bản `/api/van-ban`
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/` | Tạo văn bản + upload file |
| GET | `/` | Tìm kiếm (lọc tự động theo role) |
| GET | `/{id}` | Chi tiết (lịch sử, file, người nhận) |
| PUT | `/{id}/chinh-sua` | Chỉnh sửa (upload v2, v3... sau khi bị từ chối) |
| DELETE | `/{id}` | Xóa (chỉ Draft) |
| GET | `/{id}/tai-xuong` | Tải file (đánh dấu đã đọc tự động) |
| GET | `/dashboard` | Dashboard cá nhân |

### Workflow `/api/workflow`
| Method | Endpoint | Role | Mô tả |
|--------|----------|------|-------|
| POST | `/nop` | All | Nộp văn bản (tự định tuyến) |
| POST | `/xac-minh-chuyen-mon` | TruongBoMon | Bước 2: Xác minh đạt |
| POST | `/tu-choi-bo-mon` | TruongBoMon | Bước 2: Từ chối (lý do bắt buộc) |
| POST | `/phe-duyet-cuoi` | LanhDaoKhoa | Bước 3: Phê duyệt + lock file |
| POST | `/tu-choi-khoa` | LanhDaoKhoa | Bước 3: Từ chối (lý do bắt buộc) |
| POST | `/nop-lai` | All | Nộp lại sau chỉnh sửa (tự định tuyến) |
| GET | `/{vanBanId}/lich-su` | All | Lịch sử xử lý đầy đủ |

### Số hiệu `/api/so-hieu`
| Method | Endpoint | Role | Mô tả |
|--------|----------|------|-------|
| POST | `/cap-so` | VanThuKhoa | Cấp số hiệu chính thức |
| GET | `/xem-truoc/{vanBanId}` | VanThuKhoa | Preview số sẽ cấp |
| GET | `/{vanBanId}` | All | Thông tin số hiệu |

### Phân phối `/api/phan-phoi`
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/` | Phân phối đến danh sách người nhận |
| PUT | `/{vanBanId}/da-doc` | Đánh dấu đã đọc |
| POST | `/xac-nhan-tiep-nhan` | Xác nhận tiếp nhận (có trách nhiệm pháp lý) |
| GET | `/{vanBanId}/nguoi-nhan` | Trạng thái đọc/tiếp nhận của từng người |
| GET | `/hop-thu-den` | Văn bản được gửi đến tôi |

---

## Các Vấn Đề Nghiệp Vụ Được Giải Quyết

| Vấn đề từ tài liệu | Giải pháp trong code |
|---------------------|----------------------|
| **Xung đột vai trò Bước 1&2**: VanThuKhoa không thể bắt gửi xuống TruongBoMon | `WorkflowService.NopAsync()`: kiểm tra role, VanThuKhoa bỏ qua Bước 2 hoàn toàn |
| **Kẹt luồng deadlock**: Sau khi sửa xong không biết quay về đâu | Trường `BuocBiTuChoiCuoi` lưu "Department"/"Faculty" → `NopLai` tự định tuyến |
| **Chỉnh sửa là rẽ nhánh**, không phải bước tuần tự | Không có status `Revising`, chỉnh sửa = Upload file mới khi `TrangThai = Draft` |
| **Từ chối phải có lý do** | `TuChoiRequest.LyDoTuChoi` có `[Required]`, thiếu → 400 Bad Request |
| **Sửa văn bản sau khi đã duyệt** | `IsLocked = true` ngay trong `PheDuyetCuoiAsync()`, `ChinhSuaAsync()` kiểm tra `IsLocked` |
| **Che giấu lịch sử chỉnh sửa** | `ChinhSuaAsync()` chỉ thêm `PhienBanFile` mới, KHÔNG xóa file cũ |
| **Bước 5 áp dụng cho tất cả loại** | `CanCapSoHieu()` chỉ true cho QuyetDinh/ThongBao/CongVan |
| **Thiếu Read Receipt** | `NguoiNhanVanBan` có `DaDoc` + `DaTiepNhan` riêng biệt |
| **"Ngâm" hồ sơ** | Trường `HanXuLy` + `QuaHan` + endpoint `/api/thong-ke/qua-han` |
| **Trình duyệt vượt cấp (Bypass)** | `ValidateTruongBMAsync()` kiểm tra TruongBoMon đúng BoMon sở hữu VB |
| **"Ban hành chui"** | `CapSoAsync()` validate `TrangThai == Approved` và role phải là VanThuKhoa |

---

## Cài Đặt & Chạy

```bash
# Yêu cầu: .NET 8 SDK
dotnet tool install -g dotnet-ef

cd src/QuanLyVanBan

# Tạo migration (lần đầu)
dotnet ef migrations add InitialCreate

# Chạy (tự động migrate + seed dữ liệu mẫu)
dotnet run

# Swagger UI tại: http://localhost:5000
# Health check:   http://localhost:5000/health
```

---

## Tài Khoản Mẫu (Sau Seed)

| Email | Mật khẩu | Role |
|-------|----------|------|
| `admin@khoa.edu.vn` | `Admin@123456` | Admin |
| `nvkhoa.ldkhoa@khoa.edu.vn` | `LdKhoa@123456` | Lãnh đạo Khoa (Trưởng Khoa) |
| `ttminh.ldkhoa@khoa.edu.vn` | `LdKhoa@123456` | Lãnh đạo Khoa (Phó Trưởng Khoa) |
| `lvan.tbm@khoa.edu.vn` | `TBM@123456` | Trưởng BM (BM KHMT) |
| `ptbinh.tbm@khoa.edu.vn` | `TBM@123456` | Trưởng BM (BM KTMT) |
| `ntcam.vanThu@khoa.edu.vn` | `VanThu@123456` | Văn thư Khoa |
| `hvdung.gv@khoa.edu.vn` | `GVien@123456` | Giảng viên (BM KHMT) |
| `vtgiang.gv@khoa.edu.vn` | `GVien@123456` | Giảng viên (BM KTMT) |
