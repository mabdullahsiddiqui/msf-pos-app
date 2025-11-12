namespace pos_app.Client.Models
{
    // Item Sales Ledger Report Models
    public class ItemSalesLedgerItem
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public decimal Qty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal Rate { get; set; }
        public string AsPer { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public bool IsSubTotal { get; set; } = false;
    }

    public class ItemSalesLedgerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ItemSalesLedgerItem> Data { get; set; } = new List<ItemSalesLedgerItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Variety { get; set; } = string.Empty;
        public decimal PackSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalNetAmount { get; set; }
        public decimal AverageRatePerUnit { get; set; }
        public decimal AverageRatePerKgs { get; set; }
        public decimal AverageRatePerMounds { get; set; }
    }
}

