ProductionOrder

Cấu trúc 1 PO như sau: 

phần 1 là file db lưu thông tin 

phần 2 là file db lưu dữ liệu sản xuất

C:/DMS/ProductionData/
├── POList.db                    # Danh sách PO
├── POHistory.db                  # Lịch sử chạy PO
└── yyyy-MM/
    └── {gtin}/
        ├── {orderNo}.db          # UniqueCodes (mã sản phẩm)
        ├── Record_{orderNo}.db   # Bản ghi camera => Ghi tất cả các trạng thái mà camera đã đọc
        └── Carton_{orderNo}.db   # Thông tin thùng


Một PO gồm các trường sau: 

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
        public Product_Counter Counter { get; set; } = new Product_Counter();

        //lưu danh sách thùng và thông tin của từng thùng để dùng ở đây cho đỡ cấn tùm lum
        public Dictionary<string, CartonInfo> CartonInfo { get; set; } = new Dictionary<string, CartonInfo>();
        public Dictionary<string, ProductInfo> ProductInfo { get; set; } = new Dictionary<string, ProductInfo>();
    }


        public class Product_Counter
    {
        public int totalCount { get; set; } = 0; //total count số lượng sản phẩm
        public int passCount { get; set; } = 0;//pass count số tốt
        public int failCount { get; set; } = 0;//fail count số xấu
        public int duplicateCount { get; set; } = 0; //trùng
        public int readfailCount { get; set; } = 0;
        public int notfoundCount { get; set; } = 0;
        public int errorCount { get; set; } = 0;
        public int formatErrorCount { get; set; } = 0;

    }

    public class CartonInfo
    {
        public string carton_Code { get; set; } = "0";
        public string carton_Start_Time { get; set; } = "0";
        public string carton_End_Time { get; set; } = "0";
    }

    public class ProductInfo
    {
        public string product_Code //mã code
        public string product_CartonID //mã thùng
        public string Product_Status //trạng thái đã kích hoạt hay chưa có lỗi hay không,.....
        public string Product_Active_Time //Thời gian kích hoạt
    }