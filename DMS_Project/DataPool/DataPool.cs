using System.Data;
using System.Data.SQLite;
using System.IO;

namespace DMS_Project.DataPool
{
    /// <summary>
    /// DataPool Module - Quản lý Pool và Codes
    /// Sử dụng System.Data.SQLite
    /// </summary>
    public class DataPool
    {
        #region Private Fields & Constants
        private string _databasePath = @"C:\DMS\DataPool";
        #endregion

        #region Constructor
        public DataPool()
        {
            // Constructor - có thể khởi tạo đường dẫn mặc định
        }
        #endregion

        #region ============== 1. GET POOL PATH ==============

        /// <summary>
        /// Lấy đường dẫn pool theo tên pool
        /// </summary>
        public DataPoolResultString GetPoolPath(string poolName)
        {
            if (string.IsNullOrWhiteSpace(poolName))
            {
                return new DataPoolResultString(false, "Tên Pool không được trống.", string.Empty);
            }
            return new DataPoolResultString(true, "Success", Path.Combine(_databasePath, poolName + ".db"));
        }

        #endregion

        #region ============== 2. CREATE POOL ==============

        /// <summary>
        /// Tạo pool mới
        /// </summary>
        public DataPoolResultString CreatePool(PoolInfo poolInfo)
        {
            if (poolInfo == null)
            {
                return new DataPoolResultString(false, "Không hợp lệ, class PoolInfo là null", string.Empty);
            }
            if (string.IsNullOrWhiteSpace(poolInfo.PoolName))
            {
                return new DataPoolResultString(false, "Không hợp lệ, PoolName là rỗng", string.Empty);
            }

            string poolPath = GetPoolPath(poolInfo.PoolName).Data;
            if (File.Exists(poolPath))
            {
                return new DataPoolResultString(false, "Pool đã tồn tại", string.Empty);
            }

            if (!Directory.Exists(_databasePath))
            {
                Directory.CreateDirectory(_databasePath);
            }

            const string sql = @"
                CREATE TABLE IF NOT EXISTS Pool (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PoolName TEXT NOT NULL UNIQUE,
                    PoolDescription TEXT NOT NULL,
                    PoolCreateID TEXT NOT NULL,
                    PoolNote TEXT NOT NULL,
                    PoolCreatedBy TEXT NOT NULL,
                    PoolCreateDatetime TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Codes (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PoolCode TEXT NOT NULL UNIQUE,
                    Status INTEGER NOT NULL DEFAULT 0,
                    PoolCodeUsedBatchID TEXT NOT NULL DEFAULT '',
                    PoolCodeUsedDatetime TEXT NOT NULL DEFAULT '',
                    PoolCodeNote TEXT NOT NULL DEFAULT '',
                    PoolCodeCreateID TEXT NOT NULL DEFAULT '',
                    PoolCodeCreatedBy TEXT NOT NULL DEFAULT '',
                    PoolCodeCreateDatetime TEXT NOT NULL DEFAULT ''
                );

                CREATE INDEX IF NOT EXISTS IDX_Codes_Status ON Codes(Status);
                CREATE INDEX IF NOT EXISTS IDX_Codes_UsedBatchID ON Codes(PoolCodeUsedBatchID);
                CREATE INDEX IF NOT EXISTS IDX_Codes_CreateID ON Codes(PoolCodeCreateID);
                CREATE INDEX IF NOT EXISTS IDX_Codes_CreatedBy ON Codes(PoolCodeCreatedBy);
                CREATE INDEX IF NOT EXISTS IDX_Codes_CreateDatetime ON Codes(PoolCodeCreateDatetime);
                CREATE INDEX IF NOT EXISTS IDX_Codes_Status_CreateDatetime ON Codes(Status, PoolCodeCreateDatetime);
                CREATE INDEX IF NOT EXISTS IDX_Codes_Batch_Status ON Codes(PoolCodeUsedBatchID, Status);
            ";

            using (var conn = new SQLiteConnection($"Data Source={poolPath};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Insert pool info
                string insertSql = @"INSERT INTO Pool (PoolName, PoolDescription, PoolCreateID, PoolNote, PoolCreatedBy, PoolCreateDatetime)
                                     VALUES (@name, @desc, @createID, @note, @createdBy, @createDatetime)";
                using (var insertCmd = new SQLiteCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", poolInfo.PoolName);
                    insertCmd.Parameters.AddWithValue("@desc", poolInfo.PoolDescription);
                    insertCmd.Parameters.AddWithValue("@createID", poolInfo.PoolCreateID);
                    insertCmd.Parameters.AddWithValue("@note", poolInfo.PoolNote);
                    insertCmd.Parameters.AddWithValue("@createdBy", poolInfo.PoolCreatedBy);
                    insertCmd.Parameters.AddWithValue("@createDatetime", poolInfo.PoolCreateDatetime);
                    insertCmd.ExecuteNonQuery();
                }
            }

            return new DataPoolResultString(true, "Pool created successfully", poolPath);
        }

        #endregion

        #region ============== 3. ADD CODES ==============

        /// <summary>
        /// Thêm mã vào pool
        /// mode: 0 = file CSV, 1 = single code, 2 = DataTable
        /// </summary>
        public DataPoolAddCodesResult AddCodes(
            string poolName,
            int mode,
            string? filePath,
            string? singleCode,
            DataTable? dataTable,
            string createID,
            string createdBy,
            Action<int, int>? progressCallback = null)
        {
            var result = new DataPoolAddCodesResult();

            if (string.IsNullOrWhiteSpace(poolName))
            {
                result.Message = "Tên Pool không được trống.";
                return result;
            }

            if (mode < 0 || mode > 2)
            {
                result.Message = "Mode không hợp lệ. Chỉ chấp nhận 0 (file), 1 (1 code), hoặc 2 (DataTable).";
                return result;
            }

            var poolPathResult = GetPoolPath(poolName);
            if (!poolPathResult.Success)
            {
                result.Message = poolPathResult.Message;
                return result;
            }

            string poolPath = poolPathResult.Data;
            if (!File.Exists(poolPath))
            {
                result.Message = "Pool không tồn tại.";
                return result;
            }

            List<string> codesToAdd = new List<string>();

            switch (mode)
            {
                case 0: // File CSV
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        result.Message = "Đường dẫn file không được trống khi mode = 0.";
                        return result;
                    }
                    if (!File.Exists(filePath))
                    {
                        result.Message = $"File không tồn tại: {filePath}";
                        return result;
                    }
                    try
                    {
                        var lines = File.ReadAllLines(filePath);
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                codesToAdd.Add(trimmed);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Message = $"Lỗi khi đọc file: {ex.Message}";
                        return result;
                    }
                    break;

                case 1: // 1 code
                    if (string.IsNullOrWhiteSpace(singleCode))
                    {
                        result.Message = "Code không được trống khi mode = 1.";
                        return result;
                    }
                    codesToAdd.Add(singleCode.Trim());
                    break;

                case 2: // DataTable
                    if (dataTable == null || dataTable.Rows.Count == 0)
                    {
                        result.Message = "DataTable không hợp lệ hoặc không có dữ liệu khi mode = 2.";
                        return result;
                    }
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var code = row[0]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(code))
                        {
                            codesToAdd.Add(code);
                        }
                    }
                    break;
            }

            if (codesToAdd.Count == 0)
            {
                result.Message = "Không có code nào để thêm.";
                return result;
            }

            result.TotalCount = codesToAdd.Count;
            string createDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            using (var conn = new SQLiteConnection($"Data Source={poolPath};Version=3;"))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int processed = 0;
                        foreach (var code in codesToAdd)
                        {
                            if (string.IsNullOrWhiteSpace(code)) continue;

                            try
                            {
                                // Check duplicate
                                using (var checkCmd = new SQLiteCommand(
                                    "SELECT COUNT(*) FROM Codes WHERE PoolCode = @code", conn, transaction))
                                {
                                    checkCmd.Parameters.AddWithValue("@code", code);
                                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                                    if (count > 0)
                                    {
                                        result.DuplicateCount++;
                                        processed++;
                                        progressCallback?.Invoke(processed, codesToAdd.Count);
                                        continue;
                                    }
                                }

                                // Insert
                                using (var insertCmd = new SQLiteCommand(@"
                                    INSERT INTO Codes (PoolCode, Status, PoolCodeCreateID, PoolCodeCreatedBy, PoolCodeCreateDatetime)
                                    VALUES (@code, 0, @createID, @createdBy, @createDatetime)", conn, transaction))
                                {
                                    insertCmd.Parameters.AddWithValue("@code", code);
                                    insertCmd.Parameters.AddWithValue("@createID", createID);
                                    insertCmd.Parameters.AddWithValue("@createdBy", createdBy);
                                    insertCmd.Parameters.AddWithValue("@createDatetime", createDatetime);
                                    insertCmd.ExecuteNonQuery();
                                }

                                result.AddedCount++;
                                processed++;
                                progressCallback?.Invoke(processed, codesToAdd.Count);
                            }
                            catch (Exception ex)
                            {
                                result.ErrorCount++;
                                processed++;
                                progressCallback?.Invoke(processed, codesToAdd.Count);
                                if (result.Errors.Count < 10)
                                {
                                    result.Errors.Add($"Code '{code}': {ex.Message}");
                                }
                            }
                        }

                        transaction.Commit();
                        result.Success = true;
                        result.Message = $"Hoàn tất. Thêm mới: {result.AddedCount}, Trùng: {result.DuplicateCount}, Lỗi: {result.ErrorCount}";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        result.Message = $"Lỗi transaction: {ex.Message}";
                    }
                }
            }

            return result;
        }

        #endregion

        #region ============== 4. UPDATE CODE STATUS ==============

        /// <summary>
        /// Cập nhật trạng thái mã trong pool
        /// Status = 0: chưa sử dụng, 1: đã sử dụng, -1: lỗi
        /// </summary>
        public DataPoolResult UpdateCodeStatus(string poolName, string? poolCode, double? id, int newStatus)
        {
            if (string.IsNullOrWhiteSpace(poolName))
            {
                return new DataPoolResult(false, "Tên Pool không được trống.");
            }
            if (newStatus != 0 && newStatus != 1 && newStatus != -1)
            {
                return new DataPoolResult(false, "Status không hợp lệ. Chỉ chấp nhận: 0 (chưa dùng), 1 (đã dùng), -1 (lỗi).");
            }
            if (string.IsNullOrWhiteSpace(poolCode) && !id.HasValue)
            {
                return new DataPoolResult(false, "Phải cung cấp PoolCode hoặc ID.");
            }

            var poolPathResult = GetPoolPath(poolName);
            if (!poolPathResult.Success)
            {
                return new DataPoolResult(false, poolPathResult.Message);
            }
            string poolPath = poolPathResult.Data;
            if (!File.Exists(poolPath))
            {
                return new DataPoolResult(false, "Pool không tồn tại.");
            }

            using (var conn = new SQLiteConnection($"Data Source={poolPath};Version=3;"))
            {
                conn.Open();

                string sql;
                SQLiteCommand cmd;

                if (!string.IsNullOrWhiteSpace(poolCode))
                {
                    sql = @"UPDATE Codes SET Status = @status";
                    if (newStatus == 1)
                    {
                        sql += @", PoolCodeUsedBatchID = @batchID, PoolCodeUsedDatetime = @usedDatetime";
                    }
                    else if (newStatus == 0)
                    {
                        sql += @", PoolCodeUsedBatchID = '', PoolCodeUsedDatetime = ''";
                    }
                    sql += @" WHERE PoolCode = @code";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@code", poolCode);
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    if (newStatus == 1)
                    {
                        cmd.Parameters.AddWithValue("@batchID", $"BATCH_{DateTime.Now:yyyyMMddHHmmss}");
                        cmd.Parameters.AddWithValue("@usedDatetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }
                else
                {
                    sql = @"UPDATE Codes SET Status = @status";
                    if (newStatus == 1)
                    {
                        sql += @", PoolCodeUsedBatchID = @batchID, PoolCodeUsedDatetime = @usedDatetime";
                    }
                    else if (newStatus == 0)
                    {
                        sql += @", PoolCodeUsedBatchID = '', PoolCodeUsedDatetime = ''";
                    }
                    sql += @" WHERE ID = @id";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", id!.Value);
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    if (newStatus == 1)
                    {
                        cmd.Parameters.AddWithValue("@batchID", $"BATCH_{DateTime.Now:yyyyMMddHHmmss}");
                        cmd.Parameters.AddWithValue("@usedDatetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    string statusName = newStatus == 0 ? "chưa dùng" : (newStatus == 1 ? "đã dùng" : "lỗi");
                    return new DataPoolResult(true, $"Cập nhật thành công sang trạng thái '{statusName}'. {rowsAffected} dòng bị ảnh hưởng.");
                }
                return new DataPoolResult(false, "Không tìm thấy mã code phù hợp.");
            }
        }

        #endregion

        #region ============== 5. GET POOL INFO ==============

        /// <summary>
        /// Lấy thông tin pool theo tên pool (trả về thông tin pool và Count)
        /// </summary>
        public DataPoolResult<PoolInfoWithCount> GetPoolInfo(string poolName)
        {
            if (string.IsNullOrWhiteSpace(poolName))
            {
                return new DataPoolResult<PoolInfoWithCount>(false, "Tên Pool không được trống.", null);
            }

            var poolPathResult = GetPoolPath(poolName);
            if (!poolPathResult.Success)
            {
                return new DataPoolResult<PoolInfoWithCount>(false, poolPathResult.Message, null);
            }
            string poolPath = poolPathResult.Data;
            if (!File.Exists(poolPath))
            {
                return new DataPoolResult<PoolInfoWithCount>(false, "Pool không tồn tại.", null);
            }

            using (var conn = new SQLiteConnection($"Data Source={poolPath};Version=3;"))
            {
                conn.Open();

                using (var poolCmd = new SQLiteCommand(@"SELECT ID, PoolName, PoolDescription, PoolCreateID, PoolNote, PoolCreatedBy, PoolCreateDatetime FROM Pool LIMIT 1", conn))
                using (var reader = poolCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new DataPoolResult<PoolInfoWithCount>(false, "Không tìm thấy thông tin Pool.", null);
                    }

                    var info = new PoolInfoWithCount(
                        id: reader.GetDouble(0),
                        name: reader.GetString(1),
                        description: reader.GetString(2),
                        createID: reader.GetString(3),
                        note: reader.GetString(4),
                        createdBy: reader.GetString(5),
                        createDatetime: reader.GetString(6)
                    );
                    reader.Close();

                    // Get count
                    using (var countCmd = new SQLiteCommand(@"
                        SELECT 
                            COUNT(*) as TotalCount,
                            SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) as UnusedCount,
                            SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as UsedCount,
                            SUM(CASE WHEN Status = -1 THEN 1 ELSE 0 END) as ErrorCount
                        FROM Codes", conn))
                    using (var countReader = countCmd.ExecuteReader())
                    {
                        if (countReader.Read())
                        {
                            info.Count = new PoolInfoWithCount.CodeCount(
                                total: countReader.IsDBNull(0) ? 0 : countReader.GetInt32(0),
                                unused: countReader.IsDBNull(1) ? 0 : countReader.GetInt32(1),
                                used: countReader.IsDBNull(2) ? 0 : countReader.GetInt32(2),
                                error: countReader.IsDBNull(3) ? 0 : countReader.GetInt32(3)
                            );
                        }
                    }

                    return new DataPoolResult<PoolInfoWithCount>(true, "Success", info);
                }
            }
        }

        #endregion

        #region ============== 6. GET POOL CODE ==============

        /// <summary>
        /// Lấy mã code trong pool theo PoolCode hoặc ID
        /// </summary>
        public DataPoolResult<DataTable> GetPoolCode(string poolName, string? poolCode, double? id)
        {
            if (string.IsNullOrWhiteSpace(poolName))
            {
                return new DataPoolResult<DataTable>(false, "Tên Pool không được trống.", null);
            }
            if (string.IsNullOrWhiteSpace(poolCode) && !id.HasValue)
            {
                return new DataPoolResult<DataTable>(false, "Phải cung cấp PoolCode hoặc ID.", null);
            }

            var poolPathResult = GetPoolPath(poolName);
            if (!poolPathResult.Success)
            {
                return new DataPoolResult<DataTable>(false, poolPathResult.Message, null);
            }
            string poolPath = poolPathResult.Data;
            if (!File.Exists(poolPath))
            {
                return new DataPoolResult<DataTable>(false, "Pool không tồn tại.", null);
            }

            using (var conn = new SQLiteConnection($"Data Source={poolPath};Version=3;"))
            {
                conn.Open();

                string sql;
                SQLiteCommand cmd;

                if (!string.IsNullOrWhiteSpace(poolCode))
                {
                    sql = @"SELECT ID, PoolCode, Status, PoolCodeUsedBatchID, PoolCodeUsedDatetime, PoolCodeNote, PoolCodeCreateID, PoolCodeCreatedBy, PoolCodeCreateDatetime FROM Codes WHERE PoolCode = @code";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@code", poolCode);
                }
                else
                {
                    sql = @"SELECT ID, PoolCode, Status, PoolCodeUsedBatchID, PoolCodeUsedDatetime, PoolCodeNote, PoolCodeCreateID, PoolCodeCreatedBy, PoolCodeCreateDatetime FROM Codes WHERE ID = @id";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", id!.Value);
                }

                var dt = new DataTable();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }

                if (dt.Rows.Count == 0)
                {
                    return new DataPoolResult<DataTable>(false, "Không tìm thấy mã code.", dt);
                }
                return new DataPoolResult<DataTable>(true, "Success", dt);
            }
        }

        #endregion

        #region ============== 7. GET POOL CODES PAGINATED ==============

        /// <summary>
        /// Lấy danh sách mã code có phân trang
        /// </summary>
        public DataPoolResult<PoolCodePageResult> GetPoolCodesPaginated(
            string poolName,
            int pageIndex = 1,
            int pageSize = 100,
            int? status = null,
            string? batchID = null,
            string? createID = null,
            string? createdBy = null,
            DateTime? fromCreateDate = null,
            DateTime? toCreateDate = null,
            DateTime? fromUsedDate = null,
            DateTime? toUsedDate = null)
        {
            if (string.IsNullOrWhiteSpace(poolName))
            {
                return new DataPoolResult<PoolCodePageResult>(false, "Tên Pool không được trống.", null);
            }
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 100;

            var poolPathResult = GetPoolPath(poolName);
            if (!poolPathResult.Success)
            {
                return new DataPoolResult<PoolCodePageResult>(false, poolPathResult.Message, null);
            }
            string poolPath = poolPathResult.Data;
            if (!File.Exists(poolPath))
            {
                return new DataPoolResult<PoolCodePageResult>(false, "Pool không tồn tại.", null);
            }

            using (var conn = new SQLiteConnection($"Data Source={poolPath};Version=3;"))
            {
                conn.Open();

                var whereClauses = new List<string>();
                var parameters = new List<SQLiteParameter>();

                if (status.HasValue)
                {
                    whereClauses.Add("Status = @status");
                    parameters.Add(new SQLiteParameter("@status", status.Value));
                }
                if (!string.IsNullOrWhiteSpace(batchID))
                {
                    whereClauses.Add("PoolCodeUsedBatchID = @batchID");
                    parameters.Add(new SQLiteParameter("@batchID", batchID));
                }
                if (!string.IsNullOrWhiteSpace(createID))
                {
                    whereClauses.Add("PoolCodeCreateID = @createID");
                    parameters.Add(new SQLiteParameter("@createID", createID));
                }
                if (!string.IsNullOrWhiteSpace(createdBy))
                {
                    whereClauses.Add("PoolCodeCreatedBy LIKE @createdBy");
                    parameters.Add(new SQLiteParameter("@createdBy", $"%{createdBy}%"));
                }
                if (fromCreateDate.HasValue)
                {
                    whereClauses.Add("PoolCodeCreateDatetime >= @fromCreateDate");
                    parameters.Add(new SQLiteParameter("@fromCreateDate", fromCreateDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                if (toCreateDate.HasValue)
                {
                    whereClauses.Add("PoolCodeCreateDatetime <= @toCreateDate");
                    parameters.Add(new SQLiteParameter("@toCreateDate", toCreateDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                if (fromUsedDate.HasValue)
                {
                    whereClauses.Add("PoolCodeUsedDatetime >= @fromUsedDate");
                    parameters.Add(new SQLiteParameter("@fromUsedDate", fromUsedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                if (toUsedDate.HasValue)
                {
                    whereClauses.Add("PoolCodeUsedDatetime <= @toUsedDate");
                    parameters.Add(new SQLiteParameter("@toUsedDate", toUsedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                }

                string whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                using (var countCmd = new SQLiteCommand($"SELECT COUNT(*) FROM Codes {whereClause}", conn))
                {
                    foreach (var p in parameters) countCmd.Parameters.Add(new SQLiteParameter(p.ParameterName, p.Value));
                    int totalCount = Convert.ToInt32(countCmd.ExecuteScalar());

                    int offset = (pageIndex - 1) * pageSize;

                    string sql = $@"SELECT ID, PoolCode, Status, PoolCodeUsedBatchID, PoolCodeUsedDatetime, PoolCodeNote, PoolCodeCreateID, PoolCodeCreatedBy, PoolCodeCreateDatetime 
                                    FROM Codes {whereClause} 
                                    ORDER BY ID 
                                    LIMIT @limit OFFSET @offset";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        foreach (var p in parameters) cmd.Parameters.Add(new SQLiteParameter(p.ParameterName, p.Value));
                        cmd.Parameters.AddWithValue("@limit", pageSize);
                        cmd.Parameters.AddWithValue("@offset", offset);

                        var dt = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }

                        return new DataPoolResult<PoolCodePageResult>(true, "Success", new PoolCodePageResult(dt, totalCount, pageIndex, pageSize));
                    }
                }
            }
        }

        #endregion

        #region ============== 8. GET CODE COUNTS ==============

        /// <summary>
        /// Lấy số đếm mã code: Tổng số, số lượng đã dùng
        /// </summary>
        public DataPoolResult<CodeCount> GetCodeCounts(string poolName)
        {
            if (string.IsNullOrWhiteSpace(poolName))
            {
                return new DataPoolResult<CodeCount>(false, "Tên Pool không được trống.", null);
            }

            var poolPathResult = GetPoolPath(poolName);
            if (!poolPathResult.Success)
            {
                return new DataPoolResult<CodeCount>(false, poolPathResult.Message, null);
            }
            string poolPath = poolPathResult.Data;
            if (!File.Exists(poolPath))
            {
                return new DataPoolResult<CodeCount>(false, "Pool không tồn tại.", null);
            }

            using (var conn = new SQLiteConnection($"Data Source={poolPath};Version=3;"))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand(@"
                    SELECT 
                        COUNT(*) as TotalCount,
                        SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as UsedCount
                    FROM Codes", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new DataPoolResult<CodeCount>(false, "Không thể đếm mã code.", null);
                    }

                    var count = new CodeCount(
                        total: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        used: reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
                    );

                    return new DataPoolResult<CodeCount>(true, "Success", count);
                }
            }
        }

        #endregion

        #region ============== 9. GET CODES BY STATUS ==============

        /// <summary>
        /// Lấy toàn bộ code trong pool theo trạng thái
        /// </summary>
        public DataPoolResult<DataTable> GetCodesByStatus(string poolName, int? status = null)
        {
            if (string.IsNullOrWhiteSpace(poolName))
            {
                return new DataPoolResult<DataTable>(false, "Tên Pool không được trống.", null);
            }

            var poolPathResult = GetPoolPath(poolName);
            if (!poolPathResult.Success)
            {
                return new DataPoolResult<DataTable>(false, poolPathResult.Message, null);
            }
            string poolPath = poolPathResult.Data;
            if (!File.Exists(poolPath))
            {
                return new DataPoolResult<DataTable>(false, "Pool không tồn tại.", null);
            }

            using (var conn = new SQLiteConnection($"Data Source={poolPath};Version=3;"))
            {
                conn.Open();

                string sql = @"SELECT ID, PoolCode, Status, PoolCodeUsedBatchID, PoolCodeUsedDatetime, PoolCodeNote, PoolCodeCreateID, PoolCodeCreatedBy, PoolCodeCreateDatetime FROM Codes";
                if (status.HasValue)
                {
                    sql += " WHERE Status = @status";
                }
                sql += " ORDER BY ID";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    if (status.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@status", status.Value);
                    }

                    var dt = new DataTable();
                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }

                    if (dt.Rows.Count > 0)
                    {
                        dt.Columns.Add("StatusName", typeof(string));
                        foreach (DataRow row in dt.Rows)
                        {
                            int st = Convert.ToInt32(row["Status"]);
                            row["StatusName"] = st == 0 ? "Chưa dùng" : (st == 1 ? "Đã dùng" : "Lỗi");
                        }
                    }

                    return new DataPoolResult<DataTable>(true, $"Tìm thấy {dt.Rows.Count} mã code.", dt);
                }
            }
        }

        #endregion

        #region ============== 10. GET POOLS PAGINATED ==============

        /// <summary>
        /// Lấy danh sách Pool có phân trang
        /// </summary>
        public DataPoolResult<PoolListResult> GetPoolsPaginated(int pageIndex = 1, int pageSize = 100)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 100;

            if (!Directory.Exists(_databasePath))
            {
                return new DataPoolResult<PoolListResult>(true, "Thư mục không tồn tại.", new PoolListResult(new List<PoolInfoBasic>(), 0, pageIndex, pageSize));
            }

            var files = Directory.GetFiles(_databasePath, "*.db");
            var poolInfos = new List<PoolInfoBasic>();

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                try
                {
                    using (var conn = new SQLiteConnection($"Data Source={file};Version=3;"))
                    {
                        conn.Open();

                        using (var cmd = new SQLiteCommand(@"SELECT ID, PoolName, PoolDescription, PoolCreateID, PoolNote, PoolCreatedBy, PoolCreateDatetime FROM Pool LIMIT 1", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                poolInfos.Add(new PoolInfoBasic(
                                    id: reader.GetDouble(0),
                                    name: reader.GetString(1),
                                    description: reader.GetString(2),
                                    createID: reader.GetString(3),
                                    note: reader.GetString(4),
                                    createdBy: reader.GetString(5),
                                    createDatetime: reader.GetString(6),
                                    filePath: file
                                ));
                            }
                        }
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }

            int totalCount = poolInfos.Count;
            if (totalCount == 0)
            {
                return new DataPoolResult<PoolListResult>(true, "Không có Pool nào.", new PoolListResult(new List<PoolInfoBasic>(), 0, pageIndex, pageSize));
            }

            int offset = (pageIndex - 1) * pageSize;
            var pagedList = poolInfos.OrderBy(p => p.ID).Skip(offset).Take(pageSize).ToList();

            return new DataPoolResult<PoolListResult>(true, "Success", new PoolListResult(pagedList, totalCount, pageIndex, pageSize));
        }

        #endregion
    }
}
