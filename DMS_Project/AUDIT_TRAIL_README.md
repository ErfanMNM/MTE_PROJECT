# Audit Trail & Authentication - Triển khai DMS_Project

Tài liệu này ghi lại những gì đã được build theo kế hoạch tại [`.cursor/plans/audit_trail_+_authentication_plan_b5f24949.plan.md`](.cursor/plans/audit_trail_+_authentication_plan_b5f24949.plan.md).

## 1. Tổng quan hệ thống

Hệ thống `DMS_Project` (ASP.NET Core 10 Web API) hiện có 2 tính năng lớn được thêm vào:

1. **Authentication & Authorization**: JWT Bearer (HS256) + 3 roles (`Admin`, `Operator`, `Viewer`).
2. **Audit Trail**: ghi lại toàn bộ HTTP request + mọi thay đổi business entity (Pool, Code, PO, UniqueCode, Carton, Order, User, TCP message, Config), lưu ở SQLite riêng `audit.db`.

### Stack bổ sung

| Package | Version | Mục đích |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.10 | JWT middleware cho ASP.NET Core |
| `System.IdentityModel.Tokens.Jwt` | 8.0.2 | Issue/validate HS256 token |
| `BCrypt.Net-Next` | 4.0.3 | Hash password (workFactor=11) |

## 2. Cấu trúc thư mục mới

```
DMS_Project/
├── Auth/                                # Module authentication
│   ├── AuthModels.cs                    # User, LoginRequest/Response, DTOs
│   ├── AuthRepository.cs                # users table CRUD + schema
│   ├── AuthDbInitializer.cs             # EnsureSchema + seed admin
│   ├── PasswordHasher.cs                # BCrypt wrapper + interface
│   ├── JwtTokenService.cs               # Issue HS256, BuildValidationParameters
│   └── AuthService.cs                   # Login/CRUD user + audit hooks
├── Audit/                               # Module audit trail
│   ├── AuditAction.cs                   # enum entity type, outcome, source
│   ├── AuditModels.cs                   # AuditEvent, AuditQuery, PagedAuditDto, AuditExportRequest
│   ├── AuditExecutionContext.cs         # AsyncLocal scoped context (actor, correlation)
│   ├── AuditEnricher.cs                 # DiffFieldNames + ToJson (camelCase)
│   ├── AuditRepository.cs               # Insert / Query / FindByEventId / Stream
│   ├── AuditDbInitializer.cs            # EnsureSchema (table + indexes)
│   ├── AuditService.cs                  # IAuditService.Record* + sync insert
│   └── AuditMiddleware.cs               # HTTP capture: Correlation-Id, actor, status, duration
├── Controllers/
│   ├── AuthController.cs                # /api/auth/login, /me, /users CRUD
│   └── AuditController.cs               # /api/audit query + /export
├── Program.cs                           # DI + middleware order đã cập nhật
├── Configs/AppConfigs.cs                # Thêm Jwt*/Auth*/Audit* fields
└── API_ENDPOINTS.md                     # Bổ sung tài liệu Auth + Audit
```

## 3. Database mới

### `C:\DMS\Auth\auth.db`

Bảng `Users`:

| Column | Type | Ghi chú |
|---|---|---|
| `Id` | INTEGER PK | AUTOINCREMENT |
| `Username` | TEXT UNIQUE NOT NULL | |
| `PasswordHash` | TEXT NOT NULL | BCrypt |
| `DisplayName` | TEXT NOT NULL | |
| `Email` | TEXT NULL | |
| `Role` | TEXT NOT NULL | `Admin` / `Operator` / `Viewer` |
| `IsActive` | INTEGER NOT NULL | 0/1 |
| `CreatedAt` | TEXT NOT NULL | UTC ISO 8601 |
| `CreatedBy` | TEXT NULL | username hoặc `user#{id}` |
| `LastLoginAt` | TEXT NULL | UTC ISO 8601 |

Indexes: `Role`, `IsActive`.

### `C:\DMS\Audit\audit.db`

Bảng `AuditEvents` (đầy đủ cột):

| Column | Type | Ghi chú |
|---|---|---|
| `Id` | INTEGER PK | AUTOINCREMENT |
| `EventId` | TEXT UNIQUE | GUID |
| `TimestampUtc` | TEXT | ISO 8601 UTC (`yyyy-MM-ddTHH:mm:ss.fffZ`) |
| `ActorId` | INTEGER NULL | từ JWT `sub` |
| `ActorUsername` | TEXT | từ JWT hoặc `system`/`anonymous` |
| `ActorRole` | TEXT | `Admin`/`Operator`/`Viewer`/`Anonymous`/`System` |
| `ActorSource` | TEXT | `JWT`/`System`/`Queue`/`TCP`/`Seed` |
| `CorrelationId` | TEXT | GUID - group các event của 1 request |
| `Source` | TEXT | `HTTP`/`QueueWorker`/`BackgroundService`/`TCPCamera` |
| `HttpMethod`, `HttpPath`, `HttpStatusCode`, `ClientIp`, `UserAgent` | TEXT/INT NULL | từ middleware |
| `Action` | TEXT NOT NULL | e.g. `Pool.Create`, `Production.CodeActivated` |
| `EntityType` | TEXT NOT NULL | `Pool`, `PoolCode`, `UniqueCode`, ... |
| `EntityId` | TEXT NULL | natural key (GTIN, code, orderNo, ...) |
| `ParentEntityType`, `ParentEntityId` | TEXT NULL | quan hệ cha-con |
| `Outcome` | TEXT | `Success`/`Failure`/`Partial`/`Denied` |
| `ErrorMessage` | TEXT NULL | message lỗi |
| `BeforeJson`, `AfterJson` | TEXT NULL | snapshot JSON (camelCase) |
| `ChangedFields` | TEXT NULL | comma-separated property name |
| `MetadataJson` | TEXT NULL | counts, batch info |
| `ApiGroup` | TEXT NULL | `main` / `orders` |
| `DurationMs` | INTEGER NULL | từ middleware stopwatch |

Indexes: `TimestampUtc DESC`, `(EntityType, EntityId, TimestampUtc DESC)`, `(ActorUsername, TimestampUtc DESC)`, `(Action, TimestampUtc DESC)`, `CorrelationId`, `(Outcome, TimestampUtc DESC)`.

## 4. Authentication flow

1. Lần đầu chạy app, `AuthDbInitializer.EnsureCreated()` tạo `auth.db`, nếu rỗng seed user `admin` (`Configs::InitialAdminPassword`).
2. Client gọi `POST /api/auth/login` với `{ username, password }`. Server verify BCrypt, issue JWT HS256 (8h mặc định), trả `{ token, expiresAt, user }`.
3. Mọi request kèm `Authorization: Bearer {token}`.
4. Middleware:
   - `UseAuthentication` parse token → `HttpContext.User` (claims: `sub`, `unique_name`, role, `jti`, `iat`).
   - `UseAuthorization` enforce `[Authorize(Roles="...")]`.
   - `AuditMiddleware` map `HttpContext.User` → `AuditExecutionContext.Current` cho audit.

### Roles

| Endpoint | Role được phép |
|---|---|
| `POST /api/auth/login` | Anonymous |
| `GET /api/auth/me` | Authenticated |
| `POST /api/auth/me/password` | Authenticated |
| `GET/POST/PATCH /api/auth/users[/{id}]` | Admin |
| `POST /api/auth/users/{id}/password` | Admin |
| `GET /api/audit`, `/api/audit/{id}` | Admin, Operator |
| `GET /api/audit/export` | Admin |
| `DataPool`, `Production`, `Orders` controllers | Admin, Operator |

## 5. Audit capture points

Instrumentation đặt tại các service mutation boundaries (không qua ActionFilter):

| Service method | Audit Action | Entity Type | Parent |
|---|---|---|---|
| `DataPool.CreatePool` | `Pool.Create` | Pool | - |
| `DataPool.AddCodes` | `Pool.CodesAdded` (Success/Partial/Failure) | Pool | - |
| `DataPool.AddCodesBatchCsv` | `Pool.CodesAdded` (source=`CsvStream`) | Pool | - |
| `DataPool.UpdateCodeStatus` | `Pool.CodeStatusChanged` | PoolCode | Pool |
| `Production.CreatePO` | `Production.POCreated` | ProductionOrder | - |
| `Production.LoadCodesFromGTIN` | `Production.CodesLoaded` | ProductionOrder | Pool |
| `Production.ActivateCode` | `Production.CodeActivated` | UniqueCode | ProductionOrder |
| `Production.UpdateCodeStatus` | `Production.CodeStatusChanged` | UniqueCode | ProductionOrder |
| `Production.CreateCarton` | `Production.CartonCreated` | Carton | ProductionOrder |
| `Production.AddToCarton` | `Production.CodeAddedToCarton` | UniqueCode | ProductionOrder |
| `Production.UpdateSendStatus` | `Production.AwsSendStatusChanged` | UniqueCode | ProductionOrder |
| `OrdersController.CreateOrUpdateOrder` | `Order.Enqueued` | Order | - |
| `OrderQueueService.ExecuteAsync` | `Order.Processed`/`Order.Rejected`/`Order.Failed` | Order | - |
| `TCPClient` callback `RECEIVED` | `Telegram.TcpReceived` | TcpMessage | - |
| `AuthService.Login` | `Auth.Login` / `Auth.LoginFailed` | User | - |
| `AuthService.CreateUser` | `Auth.UserCreated` | User | - |
| `AuthService.UpdateUser` | `Auth.UserUpdated` | User | - |
| `AuthService.ChangePassword` | `Auth.PasswordChanged` | User | - |
| `AuditMiddleware` | `Http.Request` | HttpRequest | - |

## 6. Middleware pipeline

```
app.UseSwagger/UI → PortGroupValidation → UseRouting → UseAuthentication
  → UseAuthorization → AuditMiddleware (capture HTTP context) → UseCors
  → SwaggerJsonRewrite → MapControllers → MapRootRedirect → Run
```

`AuditMiddleware` chạy SAU `UseAuthentication` để có `HttpContext.User`; nó:
- Bỏ qua `/swagger`, `/`, `/favicon.ico` để tránh nhiễu audit.
- Sinh `X-Correlation-Id` nếu request không có sẵn, expose qua response header.
- Đặt `AuditExecutionContext.Current` (actor, source=HTTP, correlation, clientIP, method/path, apiGroup).
- Trong `finally`: ghi `Http.Request` với status code + duration (ms).

## 7. Order queue actor propagation

`OrdersController.CreateOrUpdateOrder` clone `AuditExecutionContext.Capture()` trước khi enqueue, lưu vào `OrderQueueItem.AuditContext`. Background consumer re-set context với `Source = QueueWorker`, `ActorSource = Queue` rồi mới gọi `_dataPool` / `_production` (vẫn giữ actor gốc của người gửi). `Order.Enqueued` ghi ở HTTP boundary, `Order.Processed` ghi ở queue worker - cùng `CorrelationId` để trace.

## 8. TCP camera audit

`Program.cs`:
- Declare `IServiceProvider? _tcpServiceProvider` ở top-level.
- Sau `app = builder.Build()`, gán `_tcpServiceProvider = app.Services`.
- `tcpCamera.ClientCallBack` lambda gọi `TcpCamera_ClientCallBack(state, data)` - local function resolve `IAuditService` từ `_tcpServiceProvider`.
- Khi nhận `enumClient.RECEIVED` ghi `Telegram.TcpReceived` với `length` + `preview` (cắt còn 200 ký tự), source `Camera`.

## 9. AuditService write strategy

- Triển khai ban đầu dùng `Channel<AuditEvent>` bounded (10.000) + background flush 32 events/đợt.
- Phát hiện trong smoke test: worker bị block vô thời hạn (không rõ nguyên nhân - có thể liên quan race condition giữa singleton DI và start-up).
- **Quyết định**: chuyển sang **synchronous insert** (`_repo.Insert(evt)` ngay trong `Enqueue`). Đảm bảo 0 event mất; cost +1-5ms / audit call - acceptable cho hệ thống internal.
- AudService.Dispose() đã giữ lại nhưng hiện không còn worker; dispose chỉ cancel CTS.

## 10. Smoke test đã chạy

**Build**: `dotnet build` succeed, 0 errors, 2 warnings (code cũ TCPClient + Production line 259 không liên quan).

**E2E flow chạy được**:
1. `dotnet run` → app listen port 5000 + 49211.
2. `POST /api/auth/login` với `{username:"admin", password:"admin@123"}` → 200 + JWT token.
3. `GET /api/auth/me` với header `Authorization: Bearer ...` → 200, trả user admin.
4. `POST /api/datapool/pools` với `{poolName:"TEST_POOL_003"...}` → 201.
5. `GET /api/audit?pageSize=10` → 200 trả 4 events:
   - `Auth.Login` (User, Success, anonymous)
   - `Http.Request` (HttpRequest, Success, anonymous)  - login call
   - `Pool.Create` (Pool, Success, admin)
   - `Http.Request` (HttpRequest, Success, admin)  - create pool call

**Correlation**: tất cả event của cùng 1 HTTP request chia sẻ `CorrelationId`.

## 11. Cấu hình mới trong `configs.json`

```jsonc
{
  // ... existing fields ...

  // Auth / JWT
  "jwtSecret": "CHANGE_ME_DMS_PROJECT_DEV_SECRET_KEY_32B_MIN",  // >=32 chars
  "jwtIssuer": "DMS",
  "jwtAudience": "DMS_Clients",
  "jwtExpirationMinutes": 480,
  "initialAdminUsername": "admin",
  "initialAdminPassword": "admin@123",
  "initialAdminDisplayName": "System Administrator",
  "authDbPath": "C:\\DMS\\Auth\\auth.db",

  // Audit
  "auditDbPath": "C:\\DMS\\Audit\\audit.db",
  "auditRetentionDays": 0  // 0 = keep forever
}
```

## 12. Endpoints mới (tóm tắt)

| Method | Route | Role | Ghi chú |
|---|---|---|---|
| `POST` | `/api/auth/login` | - | Trả JWT (HS256, 8h). Body: `{ username, password }` |
| `GET` | `/api/auth/me` | Auth | Current user info |
| `PATCH` | `/api/auth/me/password` | Auth | Body: `{ oldPassword, newPassword }` |
| `GET` | `/api/auth/users` | Admin | List |
| `POST` | `/api/auth/users` | Admin | Body: `{ username, password, displayName, email?, role }` |
| `PATCH` | `/api/auth/users/{id}` | Admin | Body: `{ displayName?, email?, role?, isActive? }` |
| `POST` | `/api/auth/users/{id}/password` | Admin | Body: `{ newPassword }` |
| `GET` | `/api/audit` | Admin, Operator | Query: from, to (ISO 8601 UTC), entityType, entityId, actorUsername, action, outcome, source, correlationId, page (default 1), pageSize (default 50, max 500) |
| `GET` | `/api/audit/{eventId}` | Admin, Operator | Chi tiết 1 event |
| `GET` | `/api/audit/export` | Admin | `format=csv|jsonl`, các filter giống query; default cap 100.000 rows (max 1.000.000). Tên file: `audit-export-{from}-{to}.{ext}` |

## 13. Tài liệu tham chiếu

- Kế hoạch gốc: [`.cursor/plans/audit_trail_+_authentication_plan_b5f24949.plan.md`](.cursor/plans/audit_trail_+_authentication_plan_b5f24949.plan.md)
- API endpoints (cập nhật): [`DMS_Project/API_ENDPOINTS.md`](DMS_Project/API_ENDPOINTS.md)

## 14. Vận hành

1. **Lần đầu**: app tự tạo `auth.db` + `audit.db`, seed admin mặc định. Đăng nhập và **đổi password ngay**.
2. **Production**: thay `JwtSecret` thành secret >=32 ký tự (khuyến nghị lưu env var, tích hợp ở phase sau).
3. **Backup**: copy `C:\DMS\Auth\auth.db` và `C:\DMS\Audit\audit.db` định kỳ.
4. **Audit growth**: mỗi call REST + mỗi mutation DB ghi 1 event. Để archive, gọi `GET /api/audit/export?format=csv&from=2026-01-01T00:00:00Z&to=2026-02-01T00:00:00Z` rồi move file ra lưu trữ ngoài.
5. **Truy vết actor**: filter `ActorUsername` hoặc `CorrelationId` để xem tất cả hành động của 1 user / 1 request.

## 15. Giới hạn hiện tại & đề xuất tiếp

- JWT secret lấy từ `configs.json` (plaintext). Cần đọc env var ở phase tiếp.
- `AuditRetentionDays` chưa áp dụng job purge (mục tiêu keep forever). Nếu sau này cần auto-purge, thêm `AuditRetentionBackgroundService` quét hàng ngày.
- `Viewer` role đã định nghĩa nhưng chưa apply per-action (chỉ Admin+Operator truy cập DataPool/Production/Orders trong MVP). Khi cần refine, thêm `[Authorize(Roles="Admin,Operator,Viewer")]` ở GET và restrict POST/PUT/PATCH ở Admin+Operator.
- `User.IsEnable` trên PO chưa được expose qua endpoint. Có thể thêm `DELETE /api/production/{orderNo}` set IsEnable=false kèm audit.
- AuditService synchronous insert hiện tại phù hợp tải internal. Nếu lên production nhiều traffic, chuyển lại Channel + batch insert và tìm root cause của block trước đó.
