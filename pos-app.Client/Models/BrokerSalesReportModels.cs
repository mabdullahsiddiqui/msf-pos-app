namespace pos_app.Client.Models
{
    // Broker Sales Report Models
    public class BrokerSalesReportItem
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public decimal Qty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal Rate { get; set; }
        public string AsPer { get; set; } = string.Empty;
        public decimal NetAmount { get; set; }
        public string DeliverTo { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Packing { get; set; } = string.Empty;
        public bool IsItemHeader { get; set; }
        public bool IsSubTotal { get; set; }
    }

    public class BrokerSalesReportResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<BrokerSalesReportItem> Data { get; set; } = new List<BrokerSalesReportItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string BrokerName { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalNetAmount { get; set; }
    }
}

