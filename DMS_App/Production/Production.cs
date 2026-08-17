using System.Data;
using System.Data.SQLite;
using System.IO;
using DMS_App.DataPool;

namespace DMS_App.Production
{
    /// <summary>
    /// Production Module - Quản lý Production Order (PO)
    /// Kết hợp với DataPool để lấy mã theo GTIN
    /// </summary>
    public class Production
    {
        #region Private Fields & Constants

        private readonly string _basePath = @"C:\DMS\ProductionData";
        private readonly DataPool.DataPool _dataPool;

        #endregion

        #region Constructor

        public Production(DataPool.DataPool? dataPool = null)
        {
            _dataPool = dataPool ?? new DataPool.DataPool();

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        #endregion

        #region ============== 1. GET PO LIST ==============

        /// <summary>
        /// Lấy danh sách tất cả PO
        /// </summary>
        public POResult<POListResult> GetPOList()
        {
            string poListDb = Path.Combine(_basePath, "POList.db");
            if (!File.Exists(poListDb))
            {
                return new POResult<POListResult>(true, "Chưa có PO nào", new POListResult(new List<POInfo>(), 0));
            }

            try
            {
                var poList = new List<POInfo>();
                using (var conn = new SQLiteConnection($"Data Source={poListDb};Version=3;"))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(@"
                        SELECT orderNo, site, factory, productionLine, productionDate, shift,
                               orderQty, lotNumber, productCode, productName, gtin, customerOrderNo,
                               uom, packSize, totalCZCode, createDatetime, createUser, IsEnable
                        FROM POList ORDER BY createDatetime DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var po = new POInfo
                            {
                                orderNo = reader.GetString(0),
                                site = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                                factory = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                                productionLine = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                                productionDate = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                                shift = reader.IsDBNull(5) ? "-" : reader.GetString(5),
                                orderQty = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                                lotNumber = reader.IsDBNull(7) ? "-" : reader.GetString(7),
                                productCode = reader.IsDBNull(8) ? "-" : reader.GetString(8),
                                productName = reader.IsDBNull(9) ? "-" : reader.GetString(9),
                                gtin = reader.IsDBNull(10) ? "-" : reader.GetString(10),
                                customerOrderNo = reader.IsDBNull(11) ? "-" : reader.GetString(11),
                                uom = reader.IsDBNull(12) ? "-" : reader.GetString(12),
                                packSize = reader.IsDBNull(13) ? "-" : reader.GetString(13),
                                totalCZCode = reader.IsDBNull(14) ? "-" : reader.GetString(14),
                                createDatetime = reader.IsDBNull(15) ? "" : reader.GetString(15),
                                createUser = reader.IsDBNull(16) ? "" : reader.GetString(16),
                                IsEnable = reader.IsDBNull(17) ? true : reader.GetInt32(17) == 1
                            };
                            poList.Add(po);
                        }
                    }
                }

                return new POResult<POListResult>(true, "Success", new POListResult(poList, poList.Count));
            }
            catch (Exception ex)
            {
                return new POResult<POListResult>(false, $"Lỗi: {ex.Message}", null);
            }
        }

        #endregion

        #region ============== 2. GET PO INFO ==============

        /// <summary>
        /// Lấy thông tin chi tiết một PO
        /// </summary>
        public POResult<POInfo> GetPOInfo(string orderNo)
        {
            if (string.IsNullOrWhiteSpace(orderNo))
            {
                return new POResult<POInfo>(false, "OrderNo là bắt buộc", null);
            }

            string poListDb = Path.Combine(_basePath, "POList.db");
            if (!File.Exists(poListDb))
            {
                return new POResult<POInfo>(false, "Không tìm thấy PO", null);
            }

            try
            {
                POInfo? po = null;
                using (var conn = new SQLiteConnection($"Data Source={poListDb};Version=3;"))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(@"
                        SELECT orderNo, site, factory, productionLine, productionDate, shift,
                               orderQty, lotNumber, productCode, productName, gtin, customerOrderNo,
                               uom, packSize, totalCZCode, createDatetime, createUser, IsEnable
                        FROM POList WHERE orderNo = @orderNo", conn))
                    {
                        cmd.Parameters.AddWithValue("@orderNo", orderNo);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                po = new POInfo
                                {
                                    orderNo = reader.GetString(0),
                                    site = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                                    factory = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                                    productionLine = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                                    productionDate = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                                    shift = reader.IsDBNull(5) ? "-" : reader.GetString(5),
                                    orderQty = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                                    lotNumber = reader.IsDBNull(7) ? "-" : reader.GetString(7),
                                    productCode = reader.IsDBNull(8) ? "-" : reader.GetString(8),
                                    productName = reader.IsDBNull(9) ? "-" : reader.GetString(9),
                                    gtin = reader.IsDBNull(10) ? "-" : reader.GetString(10),
                                    customerOrderNo = reader.IsDBNull(11) ? "-" : reader.GetString(11),
                                    uom = reader.IsDBNull(12) ? "-" : reader.GetString(12),
                                    packSize = reader.IsDBNull(13) ? "-" : reader.GetString(13),
                                    totalCZCode = reader.IsDBNull(14) ? "-" : reader.GetString(14),
                                    createDatetime = reader.IsDBNull(15) ? "" : reader.GetString(15),
                                    createUser = reader.IsDBNull(16) ? "" : reader.GetString(16),
                                    IsEnable = reader.IsDBNull(17) ? true : reader.GetInt32(17) == 1
                                };
                            }
                        }
                    }
                }

                if (po == null)
                {
                    return new POResult<POInfo>(false, $"Không tìm thấy PO {orderNo}", null);
                }

                // Lấy counter
                po.Counter = GetCounter(orderNo).Data ?? new Product_Counter();

                return new POResult<POInfo>(true, "Success", po);
            }
            catch (Exception ex)
            {
                return new POResult<POInfo>(false, $"Lỗi: {ex.Message}", null);
            }
        }

        #endregion

        #region ============== 3. CREATE PO ==============

        /// <summary>
        /// Tạo PO mới
        /// </summary>
        public POResult CreatePO(POInfo poInfo)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(poInfo.orderNo))
                return new POResult(false, "OrderNo là bắt buộc");
            if (string.IsNullOrWhiteSpace(poInfo.gtin))
                return new POResult(false, "GTIN là bắt buộc");

            string poListDb = Path.Combine(_basePath, "POList.db");

            // Tạo POList.db nếu chưa có
            EnsurePOListDatabase(poListDb);

            // Check duplicate
            using (var conn = new SQLiteConnection($"Data Source={poListDb};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM POList WHERE orderNo = @orderNo", conn))
                {
                    cmd.Parameters.AddWithValue("@orderNo", poInfo.orderNo);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count > 0)
                        return new POResult(false, $"PO {poInfo.orderNo} đã tồn tại");
                }
            }

            // Tạo thư mục theo cấu trúc yyyy-MM/gtin/
            string poFolder = GetPOFolder(poInfo.gtin, poInfo.productionDate);
            Directory.CreateDirectory(poFolder);

            // Tạo các file db
            string orderDb = Path.Combine(poFolder, $"{poInfo.orderNo}.db");
            string recordDb = Path.Combine(poFolder, $"Record_{poInfo.orderNo}.db");
            string cartonDb = Path.Combine(poFolder, $"Carton_{poInfo.orderNo}.db");

            // Create databases
            CreateOrderDatabase(orderDb);
            CreateRecordDatabase(recordDb);
            CreateCartonDatabase(cartonDb, poInfo);

            // Insert vào POList
            InsertPOToList(poListDb, poInfo);

            return new POResult(true, $"Tạo PO {poInfo.orderNo} thành công");
        }

        #endregion

        #region ============== 4. LOAD CODES FROM GTIN ==============

        /// <summary>
        /// Lấy mã từ DataPool (GTIN = PoolName)
        /// </summary>
        public POResult LoadCodesFromGTIN(string orderNo, string gtin, int qty)
        {
            // 1. Lấy mã từ DataPool (GTIN = PoolName, status 0 = chưa dùng)
            var poolCodes = _dataPool.GetCodesByStatus(gtin, status: 0);
            if (!poolCodes.Success || poolCodes.Data == null || poolCodes.Data.Rows.Count == 0)
                return new POResult(false, $"Không có mã nào trong pool {gtin}");

            string poFolder = GetPOFolder(gtin, DateTime.Now.ToString("yyyy-MM-dd"));
            string orderDb = Path.Combine(poFolder, $"{orderNo}.db");

            if (!File.Exists(orderDb))
                return new POResult(false, $"PO {orderNo} không tồn tại");

            int added = 0;
            using (var conn = new SQLiteConnection($"Data Source={orderDb};Version=3;"))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    foreach (DataRow row in poolCodes.Data.Rows)
                    {
                        if (added >= qty) break;

                        string code = row["PoolCode"]?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(code)) continue;

                        using (var cmd = new SQLiteCommand(@"
                            INSERT OR IGNORE INTO UniqueCodes (Code, Status, PrintedCount)
                            VALUES (@code, 0, 0)", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@code", code);
                            added += cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }

            // Cập nhật counter
            UpdateTotalCount(orderNo, added);

            return new POResult(true, $"Đã tải {added} mã từ pool {gtin}");
        }

        #endregion

        #region ============== 5. GET NEXT CODE ==============

        /// <summary>
        /// Lấy 1 mã tiếp theo (chưa active)
        /// </summary>
        public POResult<UniqueCode> GetNextCode(string orderNo)
        {
            string dbPath = GetOrderDbPath(orderNo);
            if (!File.Exists(dbPath))
                return new POResult<UniqueCode>(false, $"PO {orderNo} không tồn tại", null);

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // Lấy 1 mã chưa active, ưu tiên chưa in
                using (var cmd = new SQLiteCommand(@"
                    SELECT ID, Code, cartonCode, Status, ActivateDate, ProductionDate, 
                           ActivateUser, Send_Status, Receive_Status, PrintedCount
                    FROM UniqueCodes 
                    WHERE Status = 0 
                    ORDER BY PrintedCount ASC, ID ASC 
                    LIMIT 1", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var code = new UniqueCode
                        {
                            ID = reader.GetInt32(0),
                            Code = reader.GetString(1),
                            cartonCode = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Status = reader.GetInt32(3),
                            ActivateDate = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            ProductionDate = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            ActivateUser = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            Send_Status = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                            Receive_Status = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                            PrintedCount = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
                        };
                        return new POResult<UniqueCode>(true, "Success", code);
                    }
                }
            }

            return new POResult<UniqueCode>(false, "Không còn mã nào", null);
        }

        #endregion

        #region ============== 6. ACTIVATE CODE ==============

        /// <summary>
        /// Kích hoạt mã (Pass) - Cập nhật cả DataPool gốc
        /// </summary>
        public POResult ActivateCode(string orderNo, string code, string user)
        {
            string dbPath = GetOrderDbPath(orderNo);
            if (!File.Exists(dbPath))
                return new POResult(false, $"PO {orderNo} không tồn tại");

            // Lấy GTIN từ PO
            var poResult = GetPOInfo(orderNo);
            if (!poResult.Success || poResult.Data == null)
                return new POResult(false, $"Không tìm thấy thông tin PO {orderNo}");

            string gtin = poResult.Data.gtin;

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // 1. Cập nhật trong PO database
                string activateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string sql = @"UPDATE UniqueCodes 
                               SET Status = 1, ActivateDate = @date, ActivateUser = @user, ProductionDate = @date
                               WHERE Code = @code AND Status = 0";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@date", activateDate);
                    cmd.Parameters.AddWithValue("@user", user);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return new POResult(false, "Mã không tồn tại hoặc đã được kích hoạt");
                }

                // 2. Cập nhật lại Status trong DataPool gốc (GTIN = PoolName)
                _dataPool.UpdateCodeStatus(gtin, code, null, 1);

                return new POResult(true, $"Kích hoạt mã {code} thành công");
            }
        }

        #endregion

        #region ============== 7. UPDATE CODE STATUS ==============

        /// <summary>
        /// Cập nhật trạng thái mã (Fail, Error, etc.)
        /// </summary>
        public POResult UpdateCodeStatus(string orderNo, string code, int status)
        {
            string dbPath = GetOrderDbPath(orderNo);
            if (!File.Exists(dbPath))
                return new POResult(false, $"PO {orderNo} không tồn tại");

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string sql = @"UPDATE UniqueCodes SET Status = @status WHERE Code = @code";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@status", status);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return new POResult(false, "Mã không tồn tại");
                }

                // Cập nhật counter
                UpdateCounter(orderNo);

                return new POResult(true, $"Cập nhật trạng thái mã {code} thành {status}");
            }
        }

        #endregion

        #region ============== 8. CREATE CARTON ==============

        /// <summary>
        /// Tạo thùng mới
        /// </summary>
        public POResult<string> CreateCarton(string orderNo, string user)
        {
            string dbPath = GetCartonDbPath(orderNo);
            if (!File.Exists(dbPath))
                return new POResult<string>(false, $"Carton database không tồn tại cho PO {orderNo}", "");

            string cartonCode = $"CTN_{orderNo}_{DateTime.Now:yyyyMMddHHmmss}";
            string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO Cartons (cartonCode, Start_Datetime, ActivateUser, cartonCount)
                    VALUES (@code, @start, @user, 0)", conn))
                {
                    cmd.Parameters.AddWithValue("@code", cartonCode);
                    cmd.Parameters.AddWithValue("@start", startTime);
                    cmd.Parameters.AddWithValue("@user", user);
                    cmd.ExecuteNonQuery();
                }
            }

            return new POResult<string>(true, $"Tạo thùng {cartonCode} thành công", cartonCode);
        }

        #endregion

        #region ============== 9. ADD TO CARTON ==============

        /// <summary>
        /// Thêm sản phẩm vào thùng
        /// </summary>
        public POResult AddToCarton(string orderNo, string code, string cartonCode)
        {
            string orderDbPath = GetOrderDbPath(orderNo);
            string cartonDbPath = GetCartonDbPath(orderNo);

            if (!File.Exists(orderDbPath))
                return new POResult(false, $"PO {orderNo} không tồn tại");
            if (!File.Exists(cartonDbPath))
                return new POResult(false, $"Carton database không tồn tại");

            using (var conn = new SQLiteConnection($"Data Source={orderDbPath};Version=3;"))
            {
                conn.Open();

                // Update cartonCode trong UniqueCodes
                string sql = @"UPDATE UniqueCodes SET cartonCode = @cartonCode WHERE Code = @code";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@cartonCode", cartonCode);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return new POResult(false, "Mã không tồn tại");
                }
            }

            // Cập nhật carton count
            using (var conn = new SQLiteConnection($"Data Source={cartonDbPath};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    UPDATE Cartons SET cartonCount = cartonCount + 1 
                    WHERE cartonCode = @cartonCode", conn))
                {
                    cmd.Parameters.AddWithValue("@cartonCode", cartonCode);
                    cmd.ExecuteNonQuery();
                }
            }

            return new POResult(true, $"Thêm mã {code} vào thùng {cartonCode}");
        }

        #endregion

        #region ============== 10. GET COUNTER ==============

        /// <summary>
        /// Lấy counter hiện tại của PO
        /// </summary>
        public POResult<Product_Counter> GetCounter(string orderNo)
        {
            string dbPath = GetOrderDbPath(orderNo);
            if (!File.Exists(dbPath))
                return new POResult<Product_Counter>(false, $"PO {orderNo} không tồn tại", null);

            var counter = new Product_Counter();

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // Get total & status counts
                using (var cmd = new SQLiteCommand(@"
                    SELECT 
                        COUNT(*) as TotalCount,
                        SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as PassCount,
                        SUM(CASE WHEN Status < 0 THEN 1 ELSE 0 END) as FailCount
                    FROM UniqueCodes", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        counter.totalCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        counter.passCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        counter.failCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    }
                }

                // Get carton count
                string cartonDb = GetCartonDbPath(orderNo);
                if (File.Exists(cartonDb))
                {
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Cartons", new SQLiteConnection($"Data Source={cartonDb};Version=3;")))
                    {
                        cmd.Connection.Open();
                        counter.totalCartonCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }

            return new POResult<Product_Counter>(true, "Success", counter);
        }

        #endregion

        #region ============== 11. RECORD PRODUCTION ==============

        /// <summary>
        /// Ghi lịch sử sản xuất
        /// </summary>
        public POResult RecordProduction(string orderNo, RecordInfo record)
        {
            string recordDb = GetRecordDbPath(orderNo);
            if (!File.Exists(recordDb))
                return new POResult(false, $"Record database không tồn tại cho PO {orderNo}");

            using (var conn = new SQLiteConnection($"Data Source={recordDb};Version=3;"))
            {
                conn.Open();

                string sql = @"INSERT INTO Records (Code, cartonCode, Status, PLC_Status, ActivateDate, ActivateUser, ProductionDate)
                               VALUES (@code, @carton, @status, @plc, @date, @user, @prodDate)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@code", record.Code);
                    cmd.Parameters.AddWithValue("@carton", record.cartonCode ?? "");
                    cmd.Parameters.AddWithValue("@status", record.Status);
                    cmd.Parameters.AddWithValue("@plc", record.PLC_Status);
                    cmd.Parameters.AddWithValue("@date", record.ActivateDate ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@user", record.ActivateUser ?? "");
                    cmd.Parameters.AddWithValue("@prodDate", record.ProductionDate ?? DateTime.Now.ToString("yyyy-MM-dd"));
                    cmd.ExecuteNonQuery();
                }
            }

            return new POResult(true, "Ghi lịch sử thành công");
        }

        #endregion

        #region ============== 12. GET RECORDS (PAGINATED) ==============

        /// <summary>
        /// Lấy records có phân trang
        /// </summary>
        public POResult<RecordPageResult> GetRecords(string orderNo, int pageIndex = 1, int pageSize = 100)
        {
            string recordDb = GetRecordDbPath(orderNo);
            if (!File.Exists(recordDb))
                return new POResult<RecordPageResult>(false, $"Record database không tồn tại cho PO {orderNo}", null);

            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 100;

            using (var conn = new SQLiteConnection($"Data Source={recordDb};Version=3;"))
            {
                conn.Open();

                // Get total count
                int totalCount;
                using (var countCmd = new SQLiteCommand("SELECT COUNT(*) FROM Records", conn))
                {
                    totalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                int offset = (pageIndex - 1) * pageSize;
                var records = new List<RecordInfo>();

                using (var cmd = new SQLiteCommand(@"
                    SELECT ID, Code, cartonCode, Status, PLC_Status, ActivateDate, ActivateUser, ProductionDate
                    FROM Records 
                    ORDER BY ID DESC
                    LIMIT @limit OFFSET @offset", conn))
                {
                    cmd.Parameters.AddWithValue("@limit", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            records.Add(new RecordInfo
                            {
                                ID = reader.GetInt32(0),
                                Code = reader.GetString(1),
                                cartonCode = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Status = reader.GetInt32(3),
                                PLC_Status = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                ActivateDate = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                ActivateUser = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                ProductionDate = reader.IsDBNull(7) ? "" : reader.GetString(7)
                            });
                        }
                    }
                }

                return new POResult<RecordPageResult>(true, "Success", 
                    new RecordPageResult(records, totalCount, pageIndex, pageSize));
            }
        }

        #endregion

        #region ============== 13. UPDATE SEND STATUS ==============

        /// <summary>
        /// Cập nhật trạng thái gửi AWS
        /// </summary>
        public POResult UpdateSendStatus(string orderNo, List<string> codes, int sendStatus)
        {
            string dbPath = GetOrderDbPath(orderNo);
            if (!File.Exists(dbPath))
                return new POResult(false, $"PO {orderNo} không tồn tại");

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    foreach (var code in codes)
                    {
                        using (var cmd = new SQLiteCommand(@"
                            UPDATE UniqueCodes SET Send_Status = @status WHERE Code = @code", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@code", code);
                            cmd.Parameters.AddWithValue("@status", sendStatus);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }

            return new POResult(true, $"Cập nhật {codes.Count} mã thành công");
        }

        #endregion

        #region ============== PRIVATE HELPERS ==============

        private string GetPOFolder(string gtin, string productionDate)
        {
            string dateFolder = string.IsNullOrWhiteSpace(productionDate) || productionDate == "-"
                ? DateTime.Now.ToString("yyyy-MM")
                : productionDate[..7];
            return Path.Combine(_basePath, dateFolder, gtin);
        }

        private string GetOrderDbPath(string orderNo)
        {
            var poInfo = GetPOInfo(orderNo);
            if (!poInfo.Success || poInfo.Data == null)
                return "";

            string poFolder = GetPOFolder(poInfo.Data.gtin, poInfo.Data.productionDate);
            return Path.Combine(poFolder, $"{orderNo}.db");
        }

        private string GetRecordDbPath(string orderNo)
        {
            var poInfo = GetPOInfo(orderNo);
            if (!poInfo.Success || poInfo.Data == null)
                return "";

            string poFolder = GetPOFolder(poInfo.Data.gtin, poInfo.Data.productionDate);
            return Path.Combine(poFolder, $"Record_{orderNo}.db");
        }

        private string GetCartonDbPath(string orderNo)
        {
            var poInfo = GetPOInfo(orderNo);
            if (!poInfo.Success || poInfo.Data == null)
                return "";

            string poFolder = GetPOFolder(poInfo.Data.gtin, poInfo.Data.productionDate);
            return Path.Combine(poFolder, $"Carton_{orderNo}.db");
        }

        private string GetGTINFromPO(string orderNo)
        {
            var poInfo = GetPOInfo(orderNo);
            return poInfo.Success && poInfo.Data != null ? poInfo.Data.gtin : "";
        }

        private void EnsurePOListDatabase(string dbPath)
        {
            if (File.Exists(dbPath)) return;

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                new SQLiteCommand("PRAGMA journal_mode=WAL;", conn).ExecuteNonQuery();

                using (var cmd = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS POList (
                        orderNo TEXT PRIMARY KEY,
                        site TEXT, factory TEXT, productionLine TEXT,
                        productionDate TEXT, shift TEXT,
                        orderQty TEXT, lotNumber TEXT,
                        productCode TEXT, productName TEXT,
                        gtin TEXT, customerOrderNo TEXT,
                        uom TEXT, packSize TEXT, totalCZCode TEXT,
                        createDatetime TEXT, createUser TEXT, IsEnable INTEGER
                    )", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CreateOrderDatabase(string dbPath)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                new SQLiteCommand("PRAGMA journal_mode=WAL;", conn).ExecuteNonQuery();

                using (var cmd = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS UniqueCodes (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Code TEXT NOT NULL UNIQUE,
                        cartonCode TEXT DEFAULT '',
                        Status INTEGER DEFAULT 0,
                        ActivateDate TEXT DEFAULT '',
                        ProductionDate TEXT DEFAULT '',
                        ActivateUser TEXT DEFAULT '',
                        Send_Status INTEGER DEFAULT 0,
                        Receive_Status INTEGER DEFAULT 0,
                        PrintedCount INTEGER DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS IDX_UniqueCodes_Status ON UniqueCodes(Status);
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CreateRecordDatabase(string dbPath)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                new SQLiteCommand("PRAGMA journal_mode=WAL;", conn).ExecuteNonQuery();

                using (var cmd = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS Records (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Code TEXT NOT NULL,
                        cartonCode TEXT DEFAULT '',
                        Status INTEGER NOT NULL,
                        PLC_Status INTEGER DEFAULT 0,
                        ActivateDate TEXT DEFAULT '',
                        ActivateUser TEXT DEFAULT '',
                        ProductionDate TEXT DEFAULT ''
                    );
                    CREATE INDEX IF NOT EXISTS IDX_Records_Code ON Records(Code);
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CreateCartonDatabase(string dbPath, POInfo poInfo)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                new SQLiteCommand("PRAGMA journal_mode=WAL;", conn).ExecuteNonQuery();

                using (var cmd = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS Cartons (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        cartonCode TEXT NOT NULL UNIQUE,
                        Start_Datetime TEXT NOT NULL,
                        Stop_Datetime TEXT DEFAULT '',
                        ActivateUser TEXT DEFAULT '',
                        cartonCount INTEGER DEFAULT 0
                    )", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertPOToList(string dbPath, POInfo poInfo)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO POList (orderNo, site, factory, productionLine, productionDate, shift,
                                        orderQty, lotNumber, productCode, productName, gtin, customerOrderNo,
                                        uom, packSize, totalCZCode, createDatetime, createUser, IsEnable)
                    VALUES (@orderNo, @site, @factory, @line, @date, @shift,
                            @qty, @lot, @prodCode, @prodName, @gtin, @custOrder,
                            @uom, @pack, @total, @create, @user, 1)", conn))
                {
                    cmd.Parameters.AddWithValue("@orderNo", poInfo.orderNo);
                    cmd.Parameters.AddWithValue("@site", poInfo.site);
                    cmd.Parameters.AddWithValue("@factory", poInfo.factory);
                    cmd.Parameters.AddWithValue("@line", poInfo.productionLine);
                    cmd.Parameters.AddWithValue("@date", poInfo.productionDate);
                    cmd.Parameters.AddWithValue("@shift", poInfo.shift);
                    cmd.Parameters.AddWithValue("@qty", poInfo.orderQty);
                    cmd.Parameters.AddWithValue("@lot", poInfo.lotNumber);
                    cmd.Parameters.AddWithValue("@prodCode", poInfo.productCode);
                    cmd.Parameters.AddWithValue("@prodName", poInfo.productName);
                    cmd.Parameters.AddWithValue("@gtin", poInfo.gtin);
                    cmd.Parameters.AddWithValue("@custOrder", poInfo.customerOrderNo);
                    cmd.Parameters.AddWithValue("@uom", poInfo.uom);
                    cmd.Parameters.AddWithValue("@pack", poInfo.packSize);
                    cmd.Parameters.AddWithValue("@total", poInfo.totalCZCode);
                    cmd.Parameters.AddWithValue("@create", now);
                    cmd.Parameters.AddWithValue("@user", poInfo.createUser);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateTotalCount(string orderNo, int added)
        {
            // Update totalCZCode in POList if needed
            string poListDb = Path.Combine(_basePath, "POList.db");
            if (!File.Exists(poListDb)) return;

            using (var conn = new SQLiteConnection($"Data Source={poListDb};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    UPDATE POList SET totalCZCode = (
                        SELECT CAST(COUNT(*) AS TEXT) FROM UniqueCodes
                    ) WHERE orderNo = @orderNo", conn))
                {
                    cmd.Parameters.AddWithValue("@orderNo", orderNo);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateCounter(string orderNo)
        {
            // Counter được tự động tính khi gọi GetCounter
        }

        #endregion
    }

    #region ============== HELPER CLASSES ==============

    public class RecordPageResult
    {
        public List<RecordInfo> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => PageIndex < TotalPages;
        public bool HasPrevPage => PageIndex > 1;

        public RecordPageResult(List<RecordInfo> items, int totalCount, int pageIndex, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }

    #endregion
}
