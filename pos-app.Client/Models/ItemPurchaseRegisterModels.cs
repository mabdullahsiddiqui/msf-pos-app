namespace pos_app.Client.Models
{
    // Item Purchase Register Report Models
    public class ItemPurchaseRegisterItem
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public decimal Qty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal Rate { get; set; }
        public string AsPer { get; set; } = string.Empty;
        public decimal NetAmt { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Variety { get; set; } = string.Empty;
        public decimal Packing { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsItemHeader { get; set; }
        public bool IsSubTotal { get; set; }
    }

    public class ItemPurchaseRegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ItemPurchaseRegisterItem> Data { get; set; } = new List<ItemPurchaseRegisterItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal GrandTotalQty { get; set; }
        public decimal GrandTotalWeight { get; set; }
        public decimal GrandTotalAmount { get; set; }
    }
}

