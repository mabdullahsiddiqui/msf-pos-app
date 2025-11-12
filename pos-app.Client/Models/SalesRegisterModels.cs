namespace pos_app.Client.Models
{
    // Sales Register Report Models
    public class SalesRegisterItem
    {
        public string InvoiceType { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Customer { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public string Packing { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Weight { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal Fare { get; set; }
        public decimal NetAmt { get; set; }
        public bool IsSubTotal { get; set; }
    }

    public class SalesRegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SalesRegisterItem> Data { get; set; } = new List<SalesRegisterItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string InvoiceType { get; set; } = string.Empty;
        public decimal TotalWeight { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalFare { get; set; }
        public decimal TotalNetAmt { get; set; }
    }
}

