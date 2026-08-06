# DMS_Project API Endpoints

Tài liệu liệt kê toàn bộ HTTP endpoint hiện có trong dự án `DMS_Project`.

- Base URL: `http://<host>:5000`
- Content-Type: `application/json`
- Swagger UI: `http://<host>:5000/swagger`
- Response wrapper: `{ success, message, data }` (`ApiResponse<T>` cho DataPool, `POResult` cho Production, `OrderResponse` cho Orders)

---

## 1. DataPoolController (`/api/datapool`)

File: `DMS_Project/Controllers/DataPoolController.cs`

### 1.1 Pool endpoints

| Method | Route | Mô tả |
|---|---|---|
| GET  | `/api/datapool/test` | Health check, trả `{ status: "ok", message: "API is running" }` |
| GET  | `/api/datapool/pools?pageIndex=1&pageSize=100` | Lấy danh sách pools (phân trang) |
| GET  | `/api/datapool/pools/{poolName}` | Lấy thông tin 1 pool (kèm code count) |
| POST | `/api/datapool/pools` | Tạo pool mới |
| GET  | `/api/datapool/pools/{poolName}/path` | Lấy đường dẫn file `.db` của pool |

**`POST /api/datapool/pools`** — body `CreatePoolRequest`:

```json
{
  "poolName": "GTIN001",
  "poolDescription": "Mô tả",
  "createID": "optional-guid-or-id",
  "note": "ghi chú",
  "createdBy": "API"
}
```

### 1.2 Code endpoints

| Method | Route | Mô tả |
|---|---|---|
| GET  | `/api/datapool/pools/{poolName}/codes?pageIndex&pageSize&status&batchID&createID&createdBy&fromCreateDate&toCreateDate&fromUsedDate&toUsedDate` | Lấy danh sách codes trong pool (phân trang + lọc) |
| GET  | `/api/datapool/pools/{poolName}/codes/counts` | Đếm số codes theo trạng thái |
| GET  | `/api/datapool/pools/{poolName}/codes/{code}` | Lấy 1 code cụ thể |
| GET  | `/api/datapool/pools/{poolName}/codes/status/{status}` | Lấy codes theo status (`0` chưa dùng, `1` đã dùng, `-1` lỗi) |
| POST | `/api/datapool/pools/{poolName}/codes` | Thêm codes vào pool |
| PATCH | `/api/datapool/pools/{poolName}/codes/{code}/status` | Cập nhật trạng thái 1 code |

**`POST /api/datapool/pools/{poolName}/codes`** — body `AddCodesRequest`:

```json
{
  "mode": 1,
  "singleCode": null,
  "codes": ["C1", "C2", "C3"],
  "createID": "optional",
  "createdBy": "API"
}
```

- `mode = 0` → dùng `singleCode`
- `mode = 1` → dùng mảng `codes`

**`PATCH /api/datapool/pools/{poolName}/codes/{code}/status`** — body `UpdateStatusRequest`:

```json
{ "status": 1 }
```

---

## 2. ProductionController (`/api/production`)

File: `DMS_Project/Controllers/ProductionController.cs`

### 2.1 PO endpoints

| Method | Route | Mô tả |
|---|---|---|
| GET  | `/api/production/polist` | Lấy danh sách tất cả PO |
| GET  | `/api/production/{orderNo}` | Lấy thông tin chi tiết 1 PO (kèm counter) |
| POST | `/api/production` | Tạo PO mới (body `POInfo`) |

### 2.2 Code endpoints

| Method | Route | Mô tả |
|---|---|---|
| POST | `/api/production/{orderNo}/loadcodes` | Tải mã từ DataPool theo GTIN |
| GET  | `/api/production/{orderNo}/nextcode` | Lấy 1 code tiếp theo (chưa active) |
| POST | `/api/production/{orderNo}/activate` | Kích hoạt mã (Pass) |
| PUT  | `/api/production/{orderNo}/code/{code}/status` | Cập nhật trạng thái mã trong PO |

**`POST /api/production/{orderNo}/loadcodes`** — body `LoadCodesRequest`:

```json
{ "gtin": "GTIN001", "qty": 100 }
```

**`POST /api/production/{orderNo}/activate`** — body `ActivateCodeRequest`:

```json
{ "code": "ABC123", "user": "system" }
```

**`PUT /api/production/{orderNo}/code/{code}/status`** — body `UpdateCodeStatusRequest`:

```json
{ "status": 1 }
```

### 2.3 Carton endpoints

| Method | Route | Mô tả |
|---|---|---|
| POST | `/api/production/{orderNo}/carton` | Tạo thùng mới |
| POST | `/api/production/{orderNo}/carton/add` | Thêm sản phẩm vào thùng |

**`POST /api/production/{orderNo}/carton/add`** — body `AddToCartonRequest`:

```json
{ "code": "ABC123", "cartonCode": "CT001" }
```

### 2.4 Counter & Records

| Method | Route | Mô tả |
|---|---|---|
| GET  | `/api/production/{orderNo}/counter` | Lấy counter hiện tại (pass/fail/duplicate/...) |
| GET  | `/api/production/{orderNo}/records?pageIndex=1&pageSize=100` | Lấy records (lịch sử) có phân trang |

### 2.5 AWS send status

| Method | Route | Mô tả |
|---|---|---|
| PUT  | `/api/production/{orderNo}/aws/sendstatus` | Cập nhật trạng thái gửi AWS cho danh sách codes |

**`PUT /api/production/{orderNo}/aws/sendstatus`** — body `UpdateSendStatusRequest`:

```json
{ "codes": ["C1", "C2"], "sendStatus": 1 }
```

---

## 3. OrdersController (`/api/orders`) — mới

File: `DMS_Project/Controllers/OrdersController.cs`

| Method | Route | Mô tả |
|---|---|---|
| POST | `/api/orders` | Tạo/cập nhật Order (qua Channel queue) |

**`POST /api/orders`** — body `OrderRequest`:

```json
{
  "orderNo": "PO001",
  "gtin": "GT001",
  "blockNo": "B001",
  "uniqueCodes": ["C1", "C2", "C3"],
  "site": "SITE_MASAN",
  "factory": "FACTORY_01",
  "productionLine": "LINE_1",
  "productionDate": "2026-08-06",
  "shift": "A",
  "orderQty": 100,
  "lotNumber": "LOT_001",
  "productCode": "PROD_XYZ",
  "productName": "San pham A",
  "customerOrderNo": "CUST_001",
  "uom": "PCS"
}
```

**Response** — `OrderResponse`:

```json
{
  "success": true,
  "message": "OK",
  "orderNo": "PO001",
  "insertedCount": 3,
  "duplicateCount": 0,
  "totalCodes": 3,
  "receiveQty": 3,
  "at": "2026-08-06T22:00:00"
}
```

**Luồng xử lý** (xem `OrderQueueService`):

1. Validate (`orderNo`, `gtin`, `orderQty > 24`, `uniqueCodes` không rỗng)
2. Nếu pool theo GTIN chưa có → `DataPool.CreatePool`
3. `DataPool.AddCodes` với `blockNo` làm `createID` (mode 2 — DataTable)
4. Nếu PO theo `orderNo` chưa có → `Production.CreatePO`
5. Trả `OrderResponse` cho client (qua `TaskCompletionSource`)

---

## Tổng hợp route prefix

| Prefix | Controller | Method hỗ trợ |
|---|---|---|
| `/api/datapool`  | `DataPoolController`  | GET, POST, PATCH |
| `/api/production` | `ProductionController` | GET, POST, PUT |
| `/api/orders`    | `OrdersController`     | POST |
| `/`              | (Program.cs)           | GET (redirect → `/swagger`) |

---

## Ghi chú chung

- Tất cả controller kế thừa `ControllerBase`, đăng ký qua `builder.Services.AddControllers()` trong `Program.cs`.
- Service lifetime: `DataPool` và `Production` đều `Singleton`, dùng chung 1 instance xuyên suốt app.

---

# Authentication & Audit Trail (tính năng mới)

## Authentication (JWT Bearer)

| Method | Route | Role | Mô tả |
|---|---|---|---|
| POST | `/api/auth/login` | Anonymous | Đăng nhập, trả về JWT token (HS256, mặc định 8h) |
| GET | `/api/auth/me` | Authenticated | Thông tin user hiện tại |
| GET | `/api/auth/users` | Admin | Liệt kê users |
| POST | `/api/auth/users` | Admin | Tạo user mới (username, password, role: Admin/Operator/Viewer) |
| PATCH | `/api/auth/users/{id}` | Admin | Cập nhật displayName/email/role/isActive |
| POST | `/api/auth/users/{id}/password` | Admin | Reset mật khẩu user khác |
| POST | `/api/auth/me/password` | Authenticated | Đổi mật khẩu của chính mình (cần oldPassword) |

**Default admin** (seed lần đầu khi chưa có user nào):
- Username: từ `configs.json::InitialAdminUsername` (mặc định `admin`)
- Password: từ `configs.json::InitialAdminPassword` (mặc định `admin@123`)
- **Phải đổi password ngay lần đầu đăng nhập.**

**Cách dùng token:** Gửi header `Authorization: Bearer {token}` cho mọi request sau khi đăng nhập. Hết hạn thì gọi lại `/api/auth/login` để lấy token mới.

## Audit Trail

| Method | Route | Role | Mô tả |
|---|---|---|---|
| GET | `/api/audit` | Admin, Operator | Query events (filter: from/to, entityType, entityId, actorUsername, action, outcome, source, correlationId, page, pageSize) |
| GET | `/api/audit/{eventId}` | Admin, Operator | Chi tiết 1 event (full before/after/JSON) |
| GET | `/api/audit/export` | Admin | Export CSV hoặc JSONL (query `format=csv|jsonl`) |

Lưu trữ tại `C:\DMS\Audit\audit.db` (SQLite, WAL mode). Các DB khác không bị ảnh hưởng. Mỗi event capture: timestamp UTC, actor (id/username/role/source), correlationId, HTTP context, action, entityType, entityId, outcome, before/after JSON, changedFields.

Các action chính được track: `Http.Request`, `Auth.Login`/`LoginFailed`/`UserCreated`/`PasswordChanged`, `Pool.Create`/`CodesAdded`/`CodeStatusChanged`, `Production.POCreated`/`CodesLoaded`/`CodeActivated`/`CodeStatusChanged`/`CartonCreated`/`CodeAddedToCarton`/`AwsSendStatusChanged`, `Order.Enqueued`/`Processed`/`Failed`/`Rejected`, `Telegram.TcpReceived`, `Config.Updated`.

## Phân quyền (roles)

- **Admin**: toàn quyền, bao gồm quản lý user và export audit
- **Operator**: CRUD business (datapool, production, orders), đọc audit
- **Viewer**: dự kiến (chưa apply per-action trong MVP; hiện tại chỉ Admin+Operator truy cập DataPool/Production/Orders)

## Bảo mật vận hành

- Đổi `JwtSecret` trong `configs.json` thành chuỗi >=32 ký tự trước khi chạy production.
- Trong môi trường thật nên đọc `JwtSecret` từ biến môi trường `DMS_JWT_SECRET` (tính năng mở rộng - hiện đang ở configs.json).
- Backup `C:\DMS\Auth\auth.db` và `C:\DMS\Audit\audit.db` định kỳ.
- Retention: mặc định **giữ vĩnh viễn** (`AuditRetentionDays=0`). Cấu hình này chưa áp dụng job xóa tự động; admin dùng `/api/audit/export` để archive dữ liệu cũ.

- `OrderQueueService` vừa là `AddSingleton` vừa là `AddHostedService` (chạy `ExecuteAsync` đọc từ `Channel<OrderQueueItem>` unbounded).
- Swagger UI bật ở `/swagger` (chỉ dev env). Production env có thể tắt bằng cách bỏ `app.UseSwagger()`.