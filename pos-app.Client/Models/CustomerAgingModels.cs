namespace pos_app.Client.Models
{
    // Customer Aging Detailed Report Item
    public class CustomerAgingDetailedItem
    {
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public string BillNo { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int Days { get; set; }
        public decimal BillAmount { get; set; }
        public decimal Pending { get; set; }
        public bool IsCustomerHeader { get; set; }
        public bool IsTotalRow { get; set; }
        public decimal? CustomerBalance { get; set; }
    }

    // Customer Aging Summary Report Item
    public class CustomerAgingSummaryItem
    {
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Days91To120 { get; set; }
        public decimal Above120 { get; set; }
    }

    // Customer Aging Response
    public class CustomerAgingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty; // "Detailed" or "Summary"
        public List<CustomerAgingDetailedItem>? DetailedData { get; set; }
        public List<CustomerAgingSummaryItem>? SummaryData { get; set; }
        public DateTime AsOnDate { get; set; }
        public string FromAccount { get; set; } = string.Empty;
        public string UptoAccount { get; set; } = string.Empty;
        
        // Totals for Summary Report
        public decimal TotalBalance { get; set; }
        public decimal TotalDays1To30 { get; set; }
        public decimal TotalDays31To60 { get; set; }
        public decimal TotalDays61To90 { get; set; }
        public decimal TotalDays91To120 { get; set; }
        public decimal TotalAbove120 { get; set; }
        
        // Total for Detailed Report
        public decimal TotalPending { get; set; }
    }
}




