using System.Collections.Generic;

namespace pos_app.Client.Models
{
    public class CEOSummaryData
    {
        public int Sr { get; set; }
        public string Legend { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal SalesRet { get; set; }
        public decimal NetSales { get; set; }
        public decimal Recovery { get; set; }
        public decimal Purchase { get; set; }
    }

    public class CEOBankSummaryData
    {
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public decimal PrevBalance { get; set; }
        public string PrevType { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal CurBal { get; set; }
        public string CurType { get; set; } = string.Empty;
    }

    public class CEOReportResponse
    {
        public List<CEOSummaryData> PerformanceSummary { get; set; } = new();
        public List<CEOBankSummaryData> BankSummary { get; set; } = new();
    }
}
