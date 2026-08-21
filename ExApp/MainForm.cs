using AttnSoft.BarcodeHook;
namespace ExApp
{
    public partial class MainForm : Form
    {
        private BarcodeReaders scanner;
        public MainForm()
        {
            InitializeComponent();
            scanner = new BarcodeReaders();
            scanner.ScanerEvent += Scanner_ScanerEvent;
            scanner.Start();
        }

        private void Scanner_ScanerEvent(string barcode)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ProcessBarcode(barcode)));
            }
            else
            {
                ProcessBarcode(barcode);
            }
        }

        private void ProcessBarcode(string barcode)
        {
            // Hiển thị lên màn hình (tùy chọn)
            listBox1.Items.Insert(0, $"Nhận barcode: {barcode}");

            // Ghi file log (tùy chọn)
            System.IO.File.AppendAllText("barcode_log.txt",
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {barcode}{Environment.NewLine}");
        }
    }
}
