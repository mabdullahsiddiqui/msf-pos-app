namespace pos_app.Client.Models
{
    // Supplier Tax Ledger Report Models
    public class SupplierTaxLedgerDetailItem
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public decimal InvAmount { get; set; }
        public decimal Commission { get; set; }
        public decimal Tax1Rate { get; set; }
        public decimal Tax1Amount { get; set; }
        public decimal Tax2Rate { get; set; }
        public decimal Tax2Amount { get; set; }
        public decimal Total { get; set; }
    }

    public class SupplierTaxLedgerSummaryItem
    {
        public string SupplierName { get; set; } = string.Empty;
        public string NTN { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Weight { get; set; }
        public decimal Commission { get; set; }
        public decimal IncomeTax { get; set; }
    }

    public class SupplierTaxLedgerGroup
    {
        public string SupplierAccount { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string NTN { get; set; } = string.Empty;
        public List<SupplierTaxLedgerDetailItem> DetailItems { get; set; } = new List<SupplierTaxLedgerDetailItem>();
        public SupplierTaxLedgerSummaryItem? SummaryItem { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class SupplierTaxLedgerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SupplierTaxLedgerGroup> Data { get; set; } = new List<SupplierTaxLedgerGroup>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromAccount { get; set; } = string.Empty;
        public string UptoAccount { get; set; } = string.Empty;
        public bool TaxCalculateAsPerBag { get; set; }
        public decimal TaxRatePerBag { get; set; }
        public string ReportType { get; set; } = "Detail"; // "Detail" or "Summary"
        public decimal GrandTotal { get; set; }
    }
}

