using System.Text.Json.Serialization;

namespace DMS_Project.Communications.Orders
{
    public class OrderRequest
    {
        [JsonPropertyName("orderNo")]
        public string OrderNo { get; set; } = string.Empty;

        [JsonPropertyName("gtin")]
        public string GTIN { get; set; } = string.Empty;

        [JsonPropertyName("blockNo")]
        public string BlockNo { get; set; } = string.Empty;

        [JsonPropertyName("uniqueCodes")]
        public List<string> UniqueCodes { get; set; } = new();

        [JsonPropertyName("site")]
        public string Site { get; set; } = string.Empty;

        [JsonPropertyName("factory")]
        public string Factory { get; set; } = string.Empty;

        [JsonPropertyName("productionLine")]
        public string ProductionLine { get; set; } = string.Empty;

        [JsonPropertyName("productionDate")]
        public string ProductionDate { get; set; } = string.Empty;

        [JsonPropertyName("shift")]
        public string Shift { get; set; } = string.Empty;

        [JsonPropertyName("orderQty")]
        public int OrderQty { get; set; }

        [JsonPropertyName("lotNumber")]
        public string LotNumber { get; set; } = string.Empty;

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("customerOrderNo")]
        public string CustomerOrderNo { get; set; } = string.Empty;

        [JsonPropertyName("uom")]
        public string Uom { get; set; } = string.Empty;
    }

    public class OrderResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("orderNo")]
        public string OrderNo { get; set; } = string.Empty;

        [JsonPropertyName("insertedCount")]
        public int InsertedCount { get; set; }

        [JsonPropertyName("duplicateCount")]
        public int DuplicateCount { get; set; }

        [JsonPropertyName("totalCodes")]
        public int TotalCodes { get; set; }

        [JsonPropertyName("receiveQty")]
        public int ReceiveQty { get; set; }

        [JsonPropertyName("at")]
        public DateTime At { get; set; }
    }
}
