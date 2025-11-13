namespace pos_app.Models
{
    // Customer Sales Ledger Report Models
    public class CustomerSalesLedgerItem
    {
        // Invoice Information
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string InvoiceType { get; set; } = string.Empty;
        
        // Customer Information
        public string CustomerAccount { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        
        // Item Information
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Variety { get; set; } = string.Empty;
        public decimal PackSize { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Quantity and Weight
        public decimal Qty { get; set; }
        public decimal TotalWeight { get; set; }
        
        // Rate and Amount
        public decimal Rate { get; set; }
        public string AsPer { get; set; } = string.Empty;
        public decimal NetAmount { get; set; }
        
        // Average Rates (for Summary mode)
        public decimal AverageRatePerUnit { get; set; }
        public decimal AverageRatePerKgs { get; set; }
        
        // Tax Information (for tax reports)
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        
        // Commission Information (for tax summary)
        public decimal CommissionRate { get; set; }
        public decimal Commission { get; set; }
        
        // Grouping flags
        public bool IsSubTotal { get; set; } = false;
        public bool IsCustomerTotal { get; set; } = false;
        public bool IsInvoiceTotal { get; set; } = false;
    }

    public class CustomerSalesLedgerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CustomerSalesLedgerItem> Data { get; set; } = new List<CustomerSalesLedgerItem>();
        
        // Filter Information
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string CustomerAccount { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty; // "Detail", "Summary", "Invoice Wise"
        public bool TaxReport { get; set; }
        public bool TaxReportSummary { get; set; }
        
        // Item Filter (if single item report)
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Variety { get; set; } = string.Empty;
        public decimal PackSize { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Totals
        public decimal TotalQty { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalCommission { get; set; }
        
        // Average Rates (for Summary mode)
        public decimal AverageRatePerUnit { get; set; }
        public decimal AverageRatePerKgs { get; set; }
    }
}

