using System.Data;

namespace DMS_App.DataPool
{
    #region ============== POOL INFO MODELS ==============

    public class PoolInfo
    {
        public double ID { get; set; }
        public string PoolName { get; set; } = string.Empty;
        public string PoolDescription { get; set; } = string.Empty;
        public string PoolCreateID { get; set; } = string.Empty;
        public string PoolNote { get; set; } = string.Empty;
        public string PoolCreatedBy { get; set; } = string.Empty;
        public string PoolCreateDatetime { get; set; } = string.Empty;

        public PoolInfo() { }

        public PoolInfo(double id, string name, string description, string batchID,
                        string createID, string note, string createdBy, string createDatetime)
        {
            ID = id;
            PoolName = name;
            PoolDescription = description;
            PoolCreateID = createID;
            PoolNote = note;
            PoolCreatedBy = createdBy;
            PoolCreateDatetime = createDatetime;
        }
    }

    public class PoolCodeInfo
    {
        public double ID { get; set; }
        public string PoolCode { get; set; } = string.Empty;
        public int PoolCodeStatus { get; set; } = 0;
        public string PoolCodeUsedBatchID { get; set; } = string.Empty;
        public string PoolCodeUsedDatetime { get; set; } = string.Empty;
        public string PoolCodeNote { get; set; } = string.Empty;
        public string PoolCodeCreateID { get; set; } = string.Empty;
        public string PoolCodeCreatedBy { get; set; } = string.Empty;
        public string PoolCodeCreateDatetime { get; set; } = string.Empty;

        public PoolCodeInfo() { }

        public PoolCodeInfo(double id, string code, int status, string usedBatchID,
                           string usedDatetime, string note, string createID,
                           string createdBy, string createDatetime)
        {
            ID = id;
            PoolCode = code;
            PoolCodeStatus = status;
            PoolCodeUsedBatchID = usedBatchID;
            PoolCodeUsedDatetime = usedDatetime;
            PoolCodeNote = note;
            PoolCodeCreateID = createID;
            PoolCodeCreatedBy = createdBy;
            PoolCodeCreateDatetime = createDatetime;
        }
    }

    #endregion

    #region ============== RESULT CLASSES ==============

    public class DataPoolResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public DataPoolResult() { }
        public DataPoolResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public class DataPoolResult<T> : DataPoolResult
    {
        public T? Data { get; set; }

        public DataPoolResult() { }
        public DataPoolResult(bool success, string message, T? data) : base(success, message)
        {
            Data = data;
        }
    }

    public class DataPoolResultString : DataPoolResult
    {
        public string Data { get; set; } = string.Empty;

        public DataPoolResultString() { }
        public DataPoolResultString(bool success, string message, string data) : base(success, message)
        {
            Data = data;
        }
    }

    #endregion

    #region ============== PAGINATION & COUNT ==============

    public class CodeCount
    {
        public int TotalCount { get; set; }
        public int UsedCount { get; set; }

        public CodeCount() { }
        public CodeCount(int total, int used)
        {
            TotalCount = total;
            UsedCount = used;
        }
    }

    public class PoolInfoWithCount
    {
        public double ID { get; set; }
        public string PoolName { get; set; } = string.Empty;
        public string PoolDescription { get; set; } = string.Empty;
        public string PoolCreateID { get; set; } = string.Empty;
        public string PoolNote { get; set; } = string.Empty;
        public string PoolCreatedBy { get; set; } = string.Empty;
        public string PoolCreateDatetime { get; set; } = string.Empty;

        public PoolInfoWithCount() { }

        public PoolInfoWithCount(double id, string name, string description, string createID,
                                string note, string createdBy, string createDatetime)
        {
            ID = id;
            PoolName = name;
            PoolDescription = description;
            PoolCreateID = createID;
            PoolNote = note;
            PoolCreatedBy = createdBy;
            PoolCreateDatetime = createDatetime;
        }

        public class CodeCount
        {
            public int TotalCount { get; set; }
            public int UnusedCount { get; set; }
            public int UsedCount { get; set; }
            public int ErrorCount { get; set; }

            public CodeCount() { }
            public CodeCount(int total, int unused, int used, int error)
            {
                TotalCount = total;
                UnusedCount = unused;
                UsedCount = used;
                ErrorCount = error;
            }
        }

        public CodeCount? Count { get; set; }
    }

    public class PoolCodePageResult
    {
        public DataTable Data { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => PageIndex < TotalPages;
        public bool HasPrevPage => PageIndex > 1;

        public PoolCodePageResult(DataTable data, int totalCount, int pageIndex, int pageSize)
        {
            Data = data;
            TotalCount = totalCount;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }

    public class PoolListResult
    {
        public List<PoolInfoBasic> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageIndex < TotalPages;
        public bool HasPrevPage => PageIndex > 1;

        public PoolListResult(List<PoolInfoBasic> items, int totalCount, int pageIndex, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }

    public class PoolInfoBasic
    {
        public double ID { get; set; }
        public string PoolName { get; set; } = string.Empty;
        public string PoolDescription { get; set; } = string.Empty;
        public string PoolCreateID { get; set; } = string.Empty;
        public string PoolNote { get; set; } = string.Empty;
        public string PoolCreatedBy { get; set; } = string.Empty;
        public string PoolCreateDatetime { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public PoolInfoBasic() { }

        public PoolInfoBasic(double id, string name, string description, string createID,
                            string note, string createdBy, string createDatetime, string filePath)
        {
            ID = id;
            PoolName = name;
            PoolDescription = description;
            PoolCreateID = createID;
            PoolNote = note;
            PoolCreatedBy = createdBy;
            PoolCreateDatetime = createDatetime;
            FilePath = filePath;
        }
    }

    public class DataPoolAddCodesResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int AddedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();

        public DataPoolAddCodesResult() { }
    }

    #endregion
}
