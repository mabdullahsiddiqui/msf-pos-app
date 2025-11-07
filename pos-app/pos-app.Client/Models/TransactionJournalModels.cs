namespace pos_app.Client.Models
{
    // Transaction Journal Report Models
    public class TransactionJournalItem
    {
        public DateTime? Date { get; set; }
        public string Particulars { get; set; } = string.Empty;
        public string VrNo { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DeliverTo { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public bool IsTotalRow { get; set; }
        public bool IsHeaderRow { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
    }

    public class TransactionJournalResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<TransactionJournalItem> Data { get; set; } = new List<TransactionJournalItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public List<string> SelectedDocumentTypes { get; set; } = new List<string>();
    }
}

