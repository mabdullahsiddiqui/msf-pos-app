namespace pos_app.Models
{
    // Purchase Register Report Models
    public class PurchaseRegisterItem
    {
        public string InvoiceType { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public string Packing { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Weight { get; set; }
        public decimal Rate { get; set; }
        public string AsPer { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsSubTotal { get; set; }
    }

    public class PurchaseRegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<PurchaseRegisterItem> Data { get; set; } = new List<PurchaseRegisterItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string InvoiceType { get; set; } = string.Empty;
        public decimal TotalWeight { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

