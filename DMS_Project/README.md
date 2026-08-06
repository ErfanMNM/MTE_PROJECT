# DMS_Project - Document Management System

## Project Overview

DMS_Project là hệ thống REST API quản lý mã code cho production, bao gồm:

- **DataPool**: Quản lý Pool và Codes (SQLite-based)
- **Production (PO)**: Quản lý đơn hàng sản xuất
- **REST API**: Expose các chức năng qua HTTP endpoints

## Project Structure

```
DMS_Project/
├── Controllers/
│   ├── DataPoolController.cs      # DataPool REST API
│   └── ProductionController.cs     # Production REST API
├── DataPool/
│   ├── DataPool.cs                # Core DataPool logic
│   └── DataPoolModels.cs          # Data models
├── Production/
│   ├── Production.cs             # Core Production logic
│   └── ProductionModels.cs        # PO, Carton, Counter models + Enums
├── Infrastructure/
│   └── Global_Variable_Class.cs  # Global variables
├── AppControls/
│   └── AppEnum.cs                # Enumerations
├── Program.cs                     # Application entry point
└── DMS_Project.csproj            # Project file
```

## Quick Start

```bash
cd DMS_Project
dotnet restore
dotnet run
```

API chạy tại: **http://localhost:5000**  
Swagger UI: **http://localhost:5000/swagger**

## DataPool API

### Endpoints

#### Pool Management

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| GET | `/api/datapool/pools` | Danh sách pools (phân trang) |
| GET | `/api/datapool/pools/{poolName}` | Thông tin pool + số đếm |
| POST | `/api/datapool/pools` | Tạo pool mới |
| GET | `/api/datapool/pools/{poolName}/path` | Lấy đường dẫn pool |

#### Code Management

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| GET | `/api/datapool/pools/{poolName}/codes` | Danh sách codes (phân trang) |
| GET | `/api/datapool/pools/{poolName}/codes/counts` | Số đếm codes |
| GET | `/api/datapool/pools/{poolName}/codes/{code}` | Lấy code theo PoolCode |
| GET | `/api/datapool/pools/{poolName}/codes/status/{status}` | Lấy codes theo status |
| POST | `/api/datapool/pools/{poolName}/codes` | Thêm codes |
| PATCH | `/api/datapool/pools/{poolName}/codes/{code}/status` | Cập nhật status code |

### Examples

#### Tạo Pool

```http
POST /api/datapool/pools
Content-Type: application/json

{
    "poolName": "GTIN-8934567890123",
    "poolDescription": "Pool cho san pham A",
    "createdBy": "admin"
}
```

#### Thêm Codes

```http
POST /api/datapool/pools/GTIN-8934567890123/codes
Content-Type: application/json

{
    "mode": 1,
    "codes": ["CODE001", "CODE002", "CODE003"],
    "createdBy": "admin"
}
```

- `mode: 0` = Single code
- `mode: 1` = List of codes

### DataPool Database

```
C:\DMS\DataPool\
└── {poolName}.db    # SQLite database per pool
```

## Production API

### Endpoints

#### PO Management

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| GET | `/api/production/polist` | Danh sách tất cả PO |
| GET | `/api/production/{orderNo}` | Chi tiết PO |
| POST | `/api/production` | Tạo PO mới |

#### Code Operations

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| POST | `/api/production/{orderNo}/loadcodes` | Tải mã từ GTIN Pool |
| GET | `/api/production/{orderNo}/nextcode` | Lấy mã tiếp theo (chưa active) |
| POST | `/api/production/{orderNo}/activate` | Kích hoạt mã (Pass) |
| PUT | `/api/production/{orderNo}/code/{code}/status` | Cập nhật trạng thái mã |

#### Carton Management

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| POST | `/api/production/{orderNo}/carton` | Tạo thùng mới |
| POST | `/api/production/{orderNo}/carton/add` | Thêm sản phẩm vào thùng |

#### Monitoring

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| GET | `/api/production/{orderNo}/counter` | Lấy counter hiện tại |
| GET | `/api/production/{orderNo}/records` | Lấy records (phân trang) |
| PUT | `/api/production/{orderNo}/aws/sendstatus` | Cập nhật trạng thái gửi AWS |

### Production Examples

#### Tạo PO

```http
POST /api/production
Content-Type: application/json

{
    "orderNo": "PO-2024-001",
    "site": "VINA",
    "factory": "VINA-CF",
    "productionLine": "LINE-01",
    "productionDate": "2024-08-06",
    "shift": "A",
    "orderQty": "1000",
    "gtin": "8934567890123",
    "productCode": "PROD-001",
    "productName": "San pham A",
    "createUser": "admin"
}
```

#### Tải mã từ GTIN Pool

```http
POST /api/production/PO-2024-001/loadcodes
Content-Type: application/json

{
    "gtin": "8934567890123",
    "qty": 100
}
```

#### Kích hoạt mã

```http
POST /api/production/PO-2024-001/activate
Content-Type: application/json

{
    "code": "893456789012301",
    "user": "operator1"
}
```

### Production Database

```
C:\DMS\ProductionData\
├── POList.db                    # Danh sách PO
└── yyyy-MM/
    └── {gtin}/
        ├── {orderNo}.db         # UniqueCodes (mã sản phẩm)
        ├── Record_{orderNo}.db  # Bản ghi camera
        └── Carton_{orderNo}.db  # Thông tin thùng
```

## Production Enums

### e_Production_State

| Value | Name | Mô tả |
|-------|------|--------|
| 0 | NoSelectedPO | Chưa chọn PO |
| 1 | Ready | Sẵn sàng |
| 2 | Running | Đang chạy |
| 3 | Paused | Tạm dừng |
| 4 | Completed | Hoàn thành |
| 99 | Error | Lỗi |

### e_Production_Status

| Value | Name | Mô tả |
|-------|------|--------|
| 1 | Pass | Đạt |
| -1 | Fail | Không đạt |
| -2 | ReadFail | Đọc thất bại |
| -3 | Duplicate | Trùng mã |
| -4 | NotFound | Không tìm thấy |
| -5 | Error | Lỗi hệ thống |
| -6 | Timeout | Timeout |
| -7 | FormatError | Lỗi định dạng |
| -8 | GSfail | GS verification fail |

## DataPool Enums

### Code Status

| Value | Name | Mô tả |
|-------|------|--------|
| 0 | Inactive | Chưa dùng |
| 1 | Active | Đã dùng |
| -1 | Error | Lỗi |

## Technology Stack

- **.NET 10.0**
- **ASP.NET Core Web API**
- **SQLite** (System.Data.SQLite)
- **Swagger/OpenAPI** (Swashbuckle)

## NuGet Packages

```xml
<PackageReference Include="System.Data.SQLite" Version="2.0.3" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
```

## License

Internal use only.
