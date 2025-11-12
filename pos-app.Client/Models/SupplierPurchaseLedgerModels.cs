namespace pos_app.Client.Models
{
    // Supplier Purchase Ledger Report Models
    public class SupplierPurchaseLedgerDetailItem
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string VehicleNo { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal Rate { get; set; }
        public string AsPer { get; set; } = string.Empty;
        public decimal NetAmt { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public decimal Packing { get; set; }
    }

    public class SupplierPurchaseLedgerSummaryItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal PackSize { get; set; }
        public decimal Qty { get; set; }
        public decimal Weight { get; set; }
        public decimal Amount { get; set; }
        public decimal AverageRate { get; set; }
        public decimal AverageKgs { get; set; }
    }

    public class SupplierPurchaseLedgerGroup
    {
        public string SupplierAccount { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public List<SupplierPurchaseLedgerDetailItem> DetailItems { get; set; } = new List<SupplierPurchaseLedgerDetailItem>();
        public List<SupplierPurchaseLedgerSummaryItem> SummaryItems { get; set; } = new List<SupplierPurchaseLedgerSummaryItem>();
        public decimal TotalQty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SupplierPurchaseLedgerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SupplierPurchaseLedgerGroup> Data { get; set; } = new List<SupplierPurchaseLedgerGroup>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SupplierAccount { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string ReportType { get; set; } = "Summary"; // "Detail" or "Summary"
        public decimal GrandTotalQty { get; set; }
        public decimal GrandTotalWeight { get; set; }
        public decimal GrandTotalAmount { get; set; }
    }
}

