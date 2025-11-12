namespace pos_app.Client.Models
{
    // Item Sales Register Report Models
    public class ItemSalesRegisterItem
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
        public string ItemName { get; set; } = string.Empty;
        public string Variety { get; set; } = string.Empty;
        public decimal Packing { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsItemHeader { get; set; }
        public bool IsSubTotal { get; set; }
    }

    public class ItemSalesRegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ItemSalesRegisterItem> Data { get; set; } = new List<ItemSalesRegisterItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal GrandTotalQty { get; set; }
        public decimal GrandTotalWeight { get; set; }
        public decimal GrandTotalGrossAmount { get; set; }
        public decimal GrandTotalDiscount { get; set; }
        public decimal GrandTotalNetAmount { get; set; }
    }
}

