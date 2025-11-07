namespace pos_app.Models
{
    // Traditional Trial Balance Models
    public class TrialBalanceItem
    {
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public string AccType { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class TrialBalanceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<TrialBalanceItem> Data { get; set; } = new List<TrialBalanceItem>();
        public DateTime AsOfDate { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public long ProcessingTimeMs { get; set; }
    }


    // 3 Trial Balance Report Models
    public class ThreeTrialBalanceItem
    {
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public decimal PrevBal { get; set; }
        public string BalType { get; set; } = string.Empty; // "Dr." or "Cr."
        public decimal CurDebit { get; set; }
        public decimal CurCredit { get; set; }
        public decimal CurBal { get; set; }
        public string CurBalType { get; set; } = string.Empty; // "Dr." or "Cr."
    }

    public class ThreeTrialBalanceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ThreeTrialBalanceItem> Data { get; set; } = new List<ThreeTrialBalanceItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromAccount { get; set; } = string.Empty;
        public string UptoAccount { get; set; } = string.Empty;
        public decimal TotalPrevBal { get; set; }
        public decimal TotalCurDebit { get; set; }
        public decimal TotalCurCredit { get; set; }
        public decimal TotalCurBal { get; set; }
    }

    // Monthly Account Balance Models
    public class MonthlyAccountBalanceItem
    {
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public Dictionary<string, decimal> MonthlyBalances { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class MonthlyAccountBalanceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<MonthlyAccountBalanceItem> Data { get; set; } = new();
        public List<string> MonthColumns { get; set; } = new(); // e.g., ["APR_2025", "MAY_2025"]
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromAccount { get; set; } = string.Empty;
        public string UptoAccount { get; set; } = string.Empty;
    }

    // Ledger Report Models
    public class LedgerItem
    {
        public string VouchNo { get; set; } = string.Empty;
        public DateTime VouchDate { get; set; }
        public string AccCode { get; set; } = string.Empty;
        public string Descript { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string BalType { get; set; } = string.Empty; // "Dr" or "Cr"
    }

    public class LedgerReportResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<LedgerItem> Data { get; set; } = new List<LedgerItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
    }

    // Journal Book Report Models
    public class JournalBookItem
    {
        public DateTime? Date { get; set; }
        public string Particulars { get; set; } = string.Empty;
        public string VoucherNo { get; set; } = string.Empty;
        public decimal Receipts { get; set; }
        public decimal Payments { get; set; }
        public int SrNo { get; set; }
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsTotalRow { get; set; }
    }

    public class JournalBookVoucherGroup
    {
        public string VoucherNo { get; set; } = string.Empty;
        public DateTime VoucherDate { get; set; }
        public List<JournalBookItem> Entries { get; set; } = new List<JournalBookItem>();
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
    }

    public class JournalBookResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<JournalBookItem> Data { get; set; } = new List<JournalBookItem>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
    }

    // Account Position Models
    public class AccountPositionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Balance { get; set; }
        public string BalanceType { get; set; } = string.Empty; // "Dr" or "Cr"
        public DateTime UptoDate { get; set; }
    }
}
