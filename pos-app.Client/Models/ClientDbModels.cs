namespace pos_app.Client.Models
{
    // Customer model for client databases
    public class ClientCustomer
    {
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public string AccType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string GstNo { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string CellNo { get; set; } = string.Empty;
        public string OldAc { get; set; } = string.Empty;
        public string ContPers { get; set; } = string.Empty;
        public bool SendSms { get; set; }
        public int ScaleAc { get; set; }
    }

    // Item model for client databases
    public class ClientItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemGroup { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string VrtyCode { get; set; } = string.Empty;
        public string Variety { get; set; } = string.Empty;
        public decimal PackSize { get; set; }
        public string PackStat { get; set; } = string.Empty;
        public decimal MrUnit { get; set; }
        public decimal NetWght { get; set; }
    }

    // Sale Invoice model for client databases
    public class ClientSaleInvoice
    {
        public string InvNo { get; set; } = string.Empty;
        public string InvType { get; set; } = string.Empty;
        public string OrderNo { get; set; } = string.Empty;
        public DateTime InvDate { get; set; }
        public string SmanCode { get; set; } = string.Empty;
        public string SmanName { get; set; } = string.Empty;
        public string WhCode { get; set; } = string.Empty;
        public string WhName { get; set; } = string.Empty;
        public string DeliverTo { get; set; } = string.Empty;
        public string AcCode { get; set; } = string.Empty;
        public string AcName { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public string TRNo { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public string User { get; set; } = string.Empty;
    }

    // Purchase Invoice model for client databases
    public class ClientPurchaseInvoice
    {
        public string InvNo { get; set; } = string.Empty;
        public DateTime InvDate { get; set; }
        public string SmanCode { get; set; } = string.Empty;
        public string SmanName { get; set; } = string.Empty;
        public string WhCode { get; set; } = string.Empty;
        public string WhName { get; set; } = string.Empty;
        public string AcCode { get; set; } = string.Empty;
        public string AcName { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public string TRNo { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public string User { get; set; } = string.Empty;
    }

    // Item Group model for client databases
    public class ClientItemGroup
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string TimeStamp { get; set; } = string.Empty;
    }

    // Report models
    public class ClientSalesReportSummary
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime LastSaleDate { get; set; }
        public decimal AverageSaleAmount { get; set; }
    }

    public class ClientMonthlySalesReport
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageSaleAmount { get; set; }
    }

    public class ClientDashboardData
    {
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalItems { get; set; }
        public decimal NetProfit { get; set; }
        public List<ClientSaleInvoice> RecentSales { get; set; } = new();
        public string MonthName { get; set; } = string.Empty;
    }
}
