using System.Data;

namespace DMS_Project.Production
{
    #region ============== PRODUCTION ENUMS ==============

    // Production State - Rút gọn cho API
    // NoSelectedPO -> Ready -> Running <-> Paused -> Completed
    public enum e_Production_State
    {
        NoSelectedPO = 0,
        Ready = 1,
        Running = 2,
        Paused = 3,
        Completed = 4,
        Error = 99
    }

    // Production Status (Camera Result)
    public enum e_Production_Status
    {
        Pass = 1,
        Fail = -1,
        Duplicate = -3,
        ReadFail = -2,
        NotFound = -4,
        Error = -5,
        Timeout = -6,
        FormatError = -7,
        GSfail = -8
    }

    // AWS Send Status
    public enum e_AWS_Send_Status
    {
        Pending = 0,
        Sent = 1,
        Failed = -1
    }

    // AWS Receive Status
    public enum e_AWS_Receive_Status
    {
        Waiting = 0,
        Success = 200,
        Duplicate = 409,
        Error = 500
    }

    // Code Status (trong PO)
    public enum e_Code_Status
    {
        Inactive = 0,
        Active = 1,
        Error = -1
    }

    #endregion

    #region ============== PO INFO ==============

    public class POInfo
    {
        public string orderNo { get; set; } = "-";
        public string site { get; set; } = "-";
        public string factory { get; set; } = "-";
        public string productionLine { get; set; } = "-";
        public string productionDate { get; set; } = "-";
        public string shift { get; set; } = "-";
        public string orderQty { get; set; } = "-";
        public string lotNumber { get; set; } = "-";
        public string productCode { get; set; } = "-";
        public string productName { get; set; } = "-";
        public string gtin { get; set; } = "-";
        public string customerOrderNo { get; set; } = "-";
        public string uom { get; set; } = "-";
        public string packSize { get; set; } = "-";
        public string totalCZCode { get; set; } = "-";

        public string createDatetime { get; set; } = string.Empty;
        public string createUser { get; set; } = string.Empty;
        public bool IsEnable { get; set; } = true;

        public Product_Counter Counter { get; set; } = new Product_Counter();

        public POInfo() { }
    }

    #endregion

    #region ============== PRODUCT COUNTER ==============

    public class Product_Counter
    {
        public int totalCount { get; set; } = 0;
        public int passCount { get; set; } = 0;
        public int failCount { get; set; } = 0;
        public int timeoutCount { get; set; } = 0;
        public int duplicateCount { get; set; } = 0;
        public int noreadCount { get; set; } = 0;
        public int notfoundCount { get; set; } = 0;
        public int errorCount { get; set; } = 0;
        public int formaterrorCount { get; set; } = 0;
        public int gsfailCount { get; set; } = 0;

        public int totalCartonCount { get; set; } = 0;
        public int activatedCartonCount { get; set; } = 0;
        public int errorCartonCount { get; set; } = 0;
        public int cartonID { get; set; } = 0;
        public string carton_Packing_Code { get; set; } = "";
        public int carton_Packing_ID { get; set; } = 0;
        public int carton_Packing_Count { get; set; } = 0;

        public void Reset()
        {
            totalCount = 0; passCount = 0; failCount = 0;
            timeoutCount = 0; duplicateCount = 0; noreadCount = 0;
            notfoundCount = 0; errorCount = 0; formaterrorCount = 0;
            gsfailCount = 0; totalCartonCount = 0; activatedCartonCount = 0;
            errorCartonCount = 0; cartonID = 0; carton_Packing_Code = "";
            carton_Packing_ID = 0; carton_Packing_Count = 0;
        }
    }

    #endregion

    #region ============== CARTON INFO ==============

    public class CartonInfo
    {
        public string carton_Code { get; set; } = "0";
        public string carton_Start_Time { get; set; } = "0";
        public string carton_Count { get; set; } = "0";
        public string activateUser { get; set; } = "";

        public CartonInfo() { }

        public CartonInfo(string code, string startTime, string count)
        {
            carton_Code = code;
            carton_Start_Time = startTime;
            carton_Count = count;
        }
    }

    #endregion

    #region ============== PRODUCT INFO ==============

    public class ProductInfo
    {
        public string product_Code { get; set; } = "";
        public string product_CartonID { get; set; } = "";
        public string product_Status { get; set; } = "";
        public string product_Active_Time { get; set; } = "";

        public ProductInfo() { }

        public ProductInfo(string code, string cartonID, string status, string activeTime)
        {
            product_Code = code;
            product_CartonID = cartonID;
            product_Status = status;
            product_Active_Time = activeTime;
        }
    }

    #endregion

    #region ============== UNIQUE CODE (trong PO) ==============

    public class UniqueCode
    {
        public int ID { get; set; }
        public string Code { get; set; } = "";
        public string cartonCode { get; set; } = "";
        public int Status { get; set; } = 0; // 1=Pass, 0=Inactive, -1=Error
        public string ActivateDate { get; set; } = "";
        public string ProductionDate { get; set; } = "";
        public string ActivateUser { get; set; } = "";
        public int Send_Status { get; set; } = 0; // 0=Pending, 1=Sent, -1=Failed
        public int Receive_Status { get; set; } = 0; // 0=Waiting, 200=OK, others=Error
        public int PrintedCount { get; set; } = 0;
    }

    #endregion

    #region ============== RECORD (History) ==============

    public class RecordInfo
    {
        public int ID { get; set; }
        public string Code { get; set; } = "";
        public string cartonCode { get; set; } = "";
        public int Status { get; set; } = 0;
        public int PLC_Status { get; set; } = 0; // 1=OK, -1=Error
        public string ActivateDate { get; set; } = "";
        public string ActivateUser { get; set; } = "";
        public string ProductionDate { get; set; } = "";
    }

    #endregion

    #region ============== RESULT CLASSES ==============

    public class POResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public POResult() { }
        public POResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public class POResult<T> : POResult
    {
        public T? Data { get; set; }

        public POResult() { }
        public POResult(bool success, string message, T? data) : base(success, message)
        {
            Data = data;
        }
    }

    public class POResultString : POResult
    {
        public string Data { get; set; } = string.Empty;

        public POResultString() { }
        public POResultString(bool success, string message, string data = "") : base(success, message)
        {
            Data = data;
        }
    }

    public class POListResult
    {
        public List<POInfo> Items { get; set; }
        public int TotalCount { get; set; }

        public POListResult(List<POInfo> items, int totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }
    }

    public class GetCodeResult
    {
        public string Code { get; set; } = "";
        public string CartonCode { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    #endregion
}
