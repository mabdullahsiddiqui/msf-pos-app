namespace pos_app.Client.Models
{
    // Item Purchase Ledger Report Models
    public class ItemPurchaseLedgerItem
    {
        public string InvoiceType { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Weight { get; set; }
        public decimal Rate { get; set; }
        public string AsPer { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class ItemPurchaseLedgerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ItemPurchaseLedgerItem> Data { get; set; } = new List<ItemPurchaseLedgerItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Variety { get; set; } = string.Empty;
        public decimal PackSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalAmount { get; set; }
    }
}



