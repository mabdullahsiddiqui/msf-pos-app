namespace pos_app.Models
{
    // Item Sales Summary Report Models
    public class ItemSalesSummaryItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Variety { get; set; } = string.Empty;
        public decimal Packing { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AvgRatePerUnit { get; set; }
        public decimal AvgRatePerKg { get; set; }
        public decimal AvgRatePerMound { get; set; }
    }

    public class ItemSalesSummaryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ItemSalesSummaryItem> Data { get; set; } = new List<ItemSalesSummaryItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? InvoiceType { get; set; }
        public string? ItemGroup { get; set; }
        public string? ItemGroupName { get; set; }
        public decimal GrandTotalQty { get; set; }
        public decimal GrandTotalWeight { get; set; }
        public decimal GrandTotalAmount { get; set; }
    }
}

