namespace pos_app.Models
{
    // Cash Book Models
    public class CashBookItem
    {
        public int SrNo { get; set; }
        public DateTime? TransDate { get; set; }
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VoucherNo { get; set; } = string.Empty;
        public decimal Receipts { get; set; }
        public decimal Payments { get; set; }
    }

    public class CashBookResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CashBookItem> Data { get; set; } = new List<CashBookItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal CashBalance { get; set; }
        public long ProcessingTimeMs { get; set; }
    }

    public class CashBookRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
