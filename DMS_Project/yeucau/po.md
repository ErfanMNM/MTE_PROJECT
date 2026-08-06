ProductionOrder

Cấu trúc 1 PO như sau: 

phần 1 là file db lưu thông tin 

file db lưu danh sách các PO và lưu thông tin cơ bản của PO

C:/DMS/ProductionData/
├── POList.db                    # Danh sách PO

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

        createDatetime 
        createUser
        IsEnable
        



phần 2 là file db lưu dữ liệu sản xuất, khi sản xuất sẽ kiểm tra và tạo các file áp dụng cho sản xuất.

C:/DMS/ProductionData/
├── POList.db                    # Danh sách PO
└── yyyy-MM/
    └── {gtin}/
        ├── {orderNo}.db          # UniqueCodes (mã sản phẩm)
        ├── Record_{orderNo}.db   # Bản ghi camera => Ghi tất cả các trạng thái mà camera đã đọc
        └── Carton_{orderNo}.db   # Thông tin thùng


{orderNo}.db - Chứa full mã code được tải từ GTIN 
1. Tạo file 
2. Khi lấy mã từ GTIN chỉ lấy các mã chưa sử dụng (GTIN chính là PoolName bên DataPool)
3. Mã sẽ được insert sẵn vào hết (thoả điều kiện số 2)

file gồm các trường sau
Code - Mã Code
cartonCode - Mã thùng
Status - trạng thái = 1 Pass, 0 là chưa kích hoạt, = -1 là lỗi
ActivateDate - Thời gian được kích hoạt
ProductionDate - ngày sản xuất
ActivateUser
Send_Status - trạng thái gửi lên aws (mes) = 0 là sẵn sàng gửi, = 1 là đã gửi, =-1 là gửi lỗi
Recive_Status - trạng thái mes trả về = 0 là chưa có gì, =200 là gửi ok, khác 200 là lỗi (ví dụ trùng là 409)
PrintedCount - số lần in

Record_{orderNo}.db chứa lịch sử sản phẩm chạy trên line (dùng để lấy counter luôn)

các trường gồm
Code
cartonCode
Status - Dựa theo enum trạng thái (không bao gồm Fail) gồm pass, noread, duplicate, notfound, error, timeout, formaterror, GSfail
PLC_Status = trạng thái gửi xuống plc 1 là ok, -1 là lỗi
ActivateDate
ActivateUser
ProductionDate


File chứa thông tin thùng (thùng sẽ được tạo trước từ việc lấy số lượng orderQty chia cho packSize)

tạo các record có cartonCode = 0 để có thể xử lý sau này á

các trường gồm : 
cartonCode
Start_Datetime
Stop_Datetime 
ActivateUser

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
        public int timeoutCount
        public int duplicateCount { get; set; } = 0; //trùng
        public int noreadCount { get; set; } = 0;
        public int notfoundCount { get; set; } = 0;
        public int errorCount { get; set; } = 0;
        public int formaterrorCount { get; set; } = 0;
        public int GSfailCount {}

    }

    public class CartonInfo
    {
        public string carton_Code { get; set; } = "0";
        public string carton_Start_Time { get; set; } = "0";
        public string carton_Count { get; set; } = "0";//số lượng sản phẩm đã đóng vào thùng
    }

    public class ProductInfo
    {
        public string product_Code //mã code
        public string product_CartonID //mã thùng
        public string Product_Status //trạng thái đã kích hoạt hay chưa có lỗi hay không,.....
        public string Product_Active_Time //Thời gian kích hoạt
    }

    

    Lên kế hoạch hoàn thiện cho tôi
- Bạn sẽ làm những hàm gì?
- Làm bao nhiêu file
- Cần bổ sung gì không?


Tôi muốn có thể load ra ram dùng         //lưu danh sách thùng và thông tin của từng thùng để dùng ở đây cho đỡ cấn tùm lum
        public Dictionary<string, CartonInfo> CartonInfo { get; set; } = new Dictionary<string, CartonInfo>();
        public Dictionary<string, ProductInfo> ProductInfo { get; set; } = new Dictionary<string, ProductInfo>();

Nên cần làm class sao mà có thể Khai báo Global sau đó dùng ở nhiều nơi á