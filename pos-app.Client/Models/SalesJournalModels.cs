namespace pos_app.Client.Models
{
    // Sales Journal Report Models
    public class SalesJournalItem
    {
        public DateTime? Date { get; set; }
        public string VoucherNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public bool IsVoucherTotal { get; set; }
        public bool IsGrandTotal { get; set; }
    }

    public class SalesJournalResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SalesJournalItem> Data { get; set; } = new List<SalesJournalItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public string InvoiceType { get; set; } = string.Empty;
    }
}

