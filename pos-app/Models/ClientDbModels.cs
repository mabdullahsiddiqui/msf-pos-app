using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pos_app.Models
{
    // Customer model for client databases
    [Table("customer")]
    public class ClientCustomer
    {
        [Key]
        [Column("acc_code")]
        [StringLength(12)]
        public string AccCode { get; set; } = string.Empty;

        [Column("acc_name")]
        [StringLength(60)]
        public string AccName { get; set; } = string.Empty;

        [Column("acc_type")]
        [StringLength(1)]
        public string AccType { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("gst_no")]
        [StringLength(17)]
        public string GstNo { get; set; } = string.Empty;

        [Column("user")]
        [StringLength(2)]
        public string User { get; set; } = string.Empty;

        [Column("cell_no")]
        [StringLength(11)]
        public string CellNo { get; set; } = string.Empty;

        [Column("old_ac")]
        [StringLength(12)]
        public string OldAc { get; set; } = string.Empty;

        [Column("send_sms")]
        public bool SendSms { get; set; }

        [Column("scale_ac")]
        public int ScaleAc { get; set; }

        [Column("cont_pers")]
        [StringLength(60)]
        public string ContPers { get; set; } = string.Empty;

        [Column("ntn")]
        [StringLength(17)]
        public string Ntn { get; set; } = string.Empty;

        [Column("time_stamp")]
        [StringLength(150)]
        public string TimeStamp { get; set; } = string.Empty;
    }

    // Item model for client databases
    [Table("item")]
    public class ClientItem
    {
        [Key]
        [Column("item_code")]
        [StringLength(6)]
        public string ItemCode { get; set; } = string.Empty;

        [Column("item_group")]
        [StringLength(2)]
        public string ItemGroup { get; set; } = string.Empty;

        [Column("group_name")]
        [StringLength(20)]
        public string GroupName { get; set; } = string.Empty;

        [Column("item_name")]
        [StringLength(30)]
        public string ItemName { get; set; } = string.Empty;

        [Column("vrty_code")]
        [StringLength(2)]
        public string VrtyCode { get; set; } = string.Empty;

        [Column("variety")]
        [StringLength(30)]
        public string Variety { get; set; } = string.Empty;

        [Column("pack_size", TypeName = "decimal(8,2)")]
        public decimal PackSize { get; set; }

        [Column("pack_stat")]
        [StringLength(10)]
        public string PackStat { get; set; } = string.Empty;

        [Column("mr_unit", TypeName = "decimal(6,3)")]
        public decimal MrUnit { get; set; }

        [Column("net_wght", TypeName = "decimal(8,2)")]
        public decimal NetWght { get; set; }
    }

    // Sale Invoice model for client databases
    [Table("sale_inv")]
    public class ClientSaleInvoice
    {
        [Column("inv_type")]
        [StringLength(10)]
        public string InvType { get; set; } = string.Empty;

        [Key]
        [Column("inv_no")]
        [StringLength(6)]
        public string InvNo { get; set; } = string.Empty;

        [Column("order_no")]
        [StringLength(12)]
        public string OrderNo { get; set; } = string.Empty;

        [Column("inv_date")]
        public DateTime InvDate { get; set; }

        [Column("sman_code")]
        [StringLength(2)]
        public string SmanCode { get; set; } = string.Empty;

        [Column("sman_name")]
        [StringLength(50)]
        public string SmanName { get; set; } = string.Empty;

        [Column("wh_code")]
        [StringLength(2)]
        public string WhCode { get; set; } = string.Empty;

        [Column("wh_name")]
        [StringLength(25)]
        public string WhName { get; set; } = string.Empty;

        [Column("deliver_to")]
        [StringLength(25)]
        public string DeliverTo { get; set; } = string.Empty;

        [Column("ac_code")]
        [StringLength(12)]
        public string AcCode { get; set; } = string.Empty;

        [Column("ac_name")]
        [StringLength(60)]
        public string AcName { get; set; } = string.Empty;

        [Column("vehicle_no")]
        [StringLength(12)]
        public string VehicleNo { get; set; } = string.Empty;

        [Column("t_r_no")]
        [StringLength(10)]
        public string TRNo { get; set; } = string.Empty;

        [Column("gros_amt", TypeName = "decimal(14,2)")]
        public decimal GrosAmt { get; set; }

        [Column("disc_amt", TypeName = "decimal(7,2)")]
        public decimal DiscAmt { get; set; }

        [Column("net_amt", TypeName = "decimal(14,2)")]
        public decimal NetAmt { get; set; }

        [Column("grand_tot", TypeName = "decimal(14,2)")]
        public decimal GrandTot { get; set; }

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;
    }

    // Purchase Invoice model for client databases
    [Table("pur_inv")]
    public class ClientPurchaseInvoice
    {
        [Key]
        [Column("inv_no")]
        [StringLength(6)]
        public string InvNo { get; set; } = string.Empty;

        [Column("inv_date")]
        public DateTime InvDate { get; set; }

        [Column("sman_code")]
        [StringLength(2)]
        public string SmanCode { get; set; } = string.Empty;

        [Column("sman_name")]
        [StringLength(50)]
        public string SmanName { get; set; } = string.Empty;

        [Column("wh_code")]
        [StringLength(2)]
        public string WhCode { get; set; } = string.Empty;

        [Column("wh_name")]
        [StringLength(25)]
        public string WhName { get; set; } = string.Empty;

        [Column("ac_code")]
        [StringLength(12)]
        public string AcCode { get; set; } = string.Empty;

        [Column("ac_name")]
        [StringLength(60)]
        public string AcName { get; set; } = string.Empty;

        [Column("vehicle_no")]
        [StringLength(12)]
        public string VehicleNo { get; set; } = string.Empty;

        [Column("t_r_no")]
        [StringLength(10)]
        public string TRNo { get; set; } = string.Empty;

        [Column("gross_amt", TypeName = "decimal(14,2)")]
        public decimal GrossAmt { get; set; }

        [Column("commission", TypeName = "decimal(8,2)")]
        public decimal Commission { get; set; }

        [Column("grand_tot", TypeName = "decimal(14,2)")]
        public decimal GrandTot { get; set; }

        [Column("net_amt", TypeName = "decimal(14,2)")]
        public decimal NetAmt { get; set; }

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;

        [Column("inv_type")]
        [StringLength(10)]
        public string InvType { get; set; } = string.Empty;
    }

    // Item Group model for client databases
    [Table("item_group")]
    public class ClientItemGroup
    {
        [Key]
        [Column("group_code")]
        [StringLength(2)]
        public string GroupCode { get; set; } = string.Empty;

        [Column("group_name")]
        [StringLength(20)]
        public string GroupName { get; set; } = string.Empty;

        [Column("time_stamp")]
        [StringLength(150)]
        public string TimeStamp { get; set; } = string.Empty;
    }

    // Sale Invoice Detail (line items) model for client databases
    [Table("sub_sinv")]
    public class ClientSaleInvoiceDetail
    {
        [Key]
        [Column("inv_no")]
        [StringLength(6)]
        public string InvNo { get; set; } = string.Empty;

        [Column("inv_type")]
        [StringLength(10)]
        public string InvType { get; set; } = string.Empty;

        [Column("inv_date")]
        public DateTime InvDate { get; set; }

        [Column("item_code")]
        [StringLength(6)]
        public string ItemCode { get; set; } = string.Empty;

        [Column("item_name")]
        [StringLength(30)]
        public string ItemName { get; set; } = string.Empty;

        [Column("variety")]
        [StringLength(30)]
        public string Variety { get; set; } = string.Empty;

        [Column("packing", TypeName = "decimal(8,2)")]
        public decimal Packing { get; set; }

        [Column("status")]
        [StringLength(10)]
        public string Status { get; set; } = string.Empty;

        [Column("net_wght", TypeName = "decimal(8,2)")]
        public decimal NetWght { get; set; }

        [Column("qty", TypeName = "decimal(8,0)")]
        public decimal Qty { get; set; }

        [Column("total_wght", TypeName = "decimal(10,2)")]
        public decimal TotalWght { get; set; }

        [Column("rate", TypeName = "decimal(8,2)")]
        public decimal Rate { get; set; }

        [Column("as_per")]
        [StringLength(10)]
        public string AsPer { get; set; } = string.Empty;

        [Column("gros_amt", TypeName = "decimal(14,2)")]
        public decimal GrosAmt { get; set; }

        [Column("discount", TypeName = "decimal(7,2)")]
        public decimal Discount { get; set; }

        [Column("grand_tot", TypeName = "decimal(14,2)")]
        public decimal GrandTot { get; set; }
    }

    // Purchase Invoice Detail (line items) model for client databases
    [Table("sub_pinv")]
    public class ClientPurchaseInvoiceDetail
    {
        [Key]
        [Column("inv_no")]
        [StringLength(6)]
        public string InvNo { get; set; } = string.Empty;

        [Column("inv_type")]
        [StringLength(10)]
        public string InvType { get; set; } = string.Empty;

        [Column("inv_date")]
        public DateTime InvDate { get; set; }

        [Column("item_code")]
        [StringLength(6)]
        public string ItemCode { get; set; } = string.Empty;

        [Column("item_name")]
        [StringLength(30)]
        public string ItemName { get; set; } = string.Empty;

        [Column("variety")]
        [StringLength(30)]
        public string Variety { get; set; } = string.Empty;

        [Column("packing", TypeName = "decimal(8,2)")]
        public decimal Packing { get; set; }

        [Column("status")]
        [StringLength(10)]
        public string Status { get; set; } = string.Empty;

        [Column("qty_jute", TypeName = "decimal(8,0)")]
        public decimal QtyJute { get; set; }

        [Column("total_wght", TypeName = "decimal(10,2)")]
        public decimal TotalWght { get; set; }

        [Column("rate", TypeName = "decimal(8,2)")]
        public decimal Rate { get; set; }

        [Column("as_per")]
        [StringLength(10)]
        public string AsPer { get; set; } = string.Empty;

        [Column("grand_tot", TypeName = "decimal(14,2)")]
        public decimal GrandTot { get; set; }
    }

    // Sale Contract model for client databases
    [Table("sale_contract")]
    public class ClientSaleContract
    {
        [Key]
        [Column("cont_no")]
        [StringLength(6)]
        public string ContNo { get; set; } = string.Empty;

        [Column("cont_date")]
        public DateTime ContDate { get; set; }

        [Column("dlvry_date")]
        public DateTime DlvryDate { get; set; }

        [Column("ac_code")]
        [StringLength(12)]
        public string AcCode { get; set; } = string.Empty;

        [Column("ac_name")]
        [StringLength(60)]
        public string AcName { get; set; } = string.Empty;

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;

        [Column("time_stamp")]
        [StringLength(150)]
        public string TimeStamp { get; set; } = string.Empty;
    }

    // Purchase Contract model for client databases
    [Table("pur_contract")]
    public class ClientPurchaseContract
    {
        [Key]
        [Column("order_no")]
        [StringLength(6)]
        public string OrderNo { get; set; } = string.Empty;

        [Column("order_date")]
        public DateTime OrderDate { get; set; }

        [Column("supplier_c")]
        [StringLength(12)]
        public string SupplierC { get; set; } = string.Empty;

        [Column("supplier_n")]
        [StringLength(50)]
        public string SupplierN { get; set; } = string.Empty;

        [Column("aprox_wght", TypeName = "decimal(12,0)")]
        public decimal AproxWght { get; set; }

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;

        [Column("cont_no")]
        [StringLength(6)]
        public string ContNo { get; set; } = string.Empty;

        [Column("cont_date")]
        public DateTime ContDate { get; set; }

        [Column("time_stamp")]
        [StringLength(150)]
        public string TimeStamp { get; set; } = string.Empty;
    }

    // Sale Return Invoice model for client databases
    [Table("saleret_inv")]
    public class ClientSaleReturnInvoice
    {
        [Key]
        [Column("inv_no")]
        [StringLength(6)]
        public string InvNo { get; set; } = string.Empty;

        [Column("inv_date")]
        public DateTime InvDate { get; set; }

        [Column("ac_code")]
        [StringLength(12)]
        public string AcCode { get; set; } = string.Empty;

        [Column("ac_name")]
        [StringLength(60)]
        public string AcName { get; set; } = string.Empty;

        [Column("vehicle_no")]
        [StringLength(12)]
        public string VehicleNo { get; set; } = string.Empty;

        [Column("gros_amt", TypeName = "decimal(12,2)")]
        public decimal GrosAmt { get; set; }

        [Column("fare", TypeName = "decimal(8,2)")]
        public decimal Fare { get; set; }

        [Column("net_amt", TypeName = "decimal(12,2)")]
        public decimal NetAmt { get; set; }

        [Column("time_stamp")]
        [StringLength(150)]
        public string TimeStamp { get; set; } = string.Empty;
    }

    // Sale Return Detail model for client databases
    [Table("sub_saleret")]
    public class ClientSaleReturnDetail
    {
        [Key]
        [Column("inv_no")]
        [StringLength(6)]
        public string InvNo { get; set; } = string.Empty;

        [Column("inv_date")]
        public DateTime InvDate { get; set; }

        [Column("item_code")]
        [StringLength(6)]
        public string ItemCode { get; set; } = string.Empty;

        [Column("item_name")]
        [StringLength(30)]
        public string ItemName { get; set; } = string.Empty;

        [Column("variety")]
        [StringLength(30)]
        public string Variety { get; set; } = string.Empty;

        [Column("packing", TypeName = "decimal(8,2)")]
        public decimal Packing { get; set; }

        [Column("qty", TypeName = "decimal(4,0)")]
        public decimal Qty { get; set; }

        [Column("total_wght", TypeName = "decimal(9,2)")]
        public decimal TotalWght { get; set; }

        [Column("rate", TypeName = "decimal(8,2)")]
        public decimal Rate { get; set; }

        [Column("as_per")]
        [StringLength(10)]
        public string AsPer { get; set; } = string.Empty;

        [Column("gros_amt", TypeName = "decimal(12,2)")]
        public decimal GrosAmt { get; set; }
    }

    // Journal Voucher model for client databases
    [Table("j_vouch")]
    public class ClientJournalVoucher
    {
        [Key]
        [Column("vouch_no")]
        [StringLength(6)]
        public string VouchNo { get; set; } = string.Empty;

        [Column("vouch_date")]
        public DateTime VouchDate { get; set; }

        [Column("acc_code")]
        [StringLength(12)]
        public string AccCode { get; set; } = string.Empty;

        [Column("descript")]
        [StringLength(40)]
        public string Descript { get; set; } = string.Empty;

        [Column("dr_amount", TypeName = "decimal(13,2)")]
        public decimal DrAmount { get; set; }

        [Column("cr_amount", TypeName = "decimal(13,2)")]
        public decimal CrAmount { get; set; }

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;

        [Column("sr_no")]
        public int SrNo { get; set; }

        [Column("vouch_type")]
        [StringLength(10)]
        public string VouchType { get; set; } = string.Empty;
    }

    // General Journal model for client databases
    [Table("g_journal")]
    public class ClientGeneralJournal
    {
        [Key]
        [Column("vouch_no")]
        [StringLength(8)]
        public string VouchNo { get; set; } = string.Empty;

        [Column("vouch_date")]
        public DateTime VouchDate { get; set; }

        [Column("sr_no", TypeName = "decimal(2,0)")]
        public decimal SrNo { get; set; }

        [Column("acc_code")]
        [StringLength(12)]
        public string AccCode { get; set; } = string.Empty;

        [Column("descript")]
        [StringLength(200)]
        public string Descript { get; set; } = string.Empty;

        [Column("dr_amount", TypeName = "decimal(11,2)")]
        public decimal DrAmount { get; set; }

        [Column("cr_amount", TypeName = "decimal(11,2)")]
        public decimal CrAmount { get; set; }

        [Column("user")]
        [StringLength(2)]
        public string User { get; set; } = string.Empty;

        [Column("tr_id", TypeName = "decimal(8,0)")]
        public decimal TrId { get; set; }
    }

    // Cash Payment Voucher model for client databases
    [Table("cpvoucher")]
    public class ClientCashPaymentVoucher
    {
        [Key]
        [Column("vouch_no")]
        [StringLength(6)]
        public string VouchNo { get; set; } = string.Empty;

        [Column("vouch_date")]
        public DateTime VouchDate { get; set; }

        [Column("ac_code")]
        [StringLength(12)]
        public string AcCode { get; set; } = string.Empty;

        [Column("ac_name")]
        [StringLength(60)]
        public string AcName { get; set; } = string.Empty;

        [Column("descript")]
        public string Descript { get; set; } = string.Empty;

        [Column("amount", TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;

        [Column("sr_no")]
        public int SrNo { get; set; }
    }

    // Cash Receipt Voucher model for client databases
    [Table("crvoucher")]
    public class ClientCashReceiptVoucher
    {
        [Key]
        [Column("vouch_no")]
        [StringLength(6)]
        public string VouchNo { get; set; } = string.Empty;

        [Column("vouch_date")]
        public DateTime VouchDate { get; set; }

        [Column("ac_code")]
        [StringLength(12)]
        public string AcCode { get; set; } = string.Empty;

        [Column("ac_name")]
        [StringLength(60)]
        public string AcName { get; set; } = string.Empty;

        [Column("descript")]
        public string Descript { get; set; } = string.Empty;

        [Column("amount", TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;

        [Column("sr_no")]
        public int SrNo { get; set; }
    }

    // Bank Payment model for client databases
    [Table("bank_payment")]
    public class ClientBankPayment
    {
        [Key]
        [Column("vouch_no")]
        [StringLength(6)]
        public string VouchNo { get; set; } = string.Empty;

        [Column("vouch_date")]
        public DateTime VouchDate { get; set; }

        [Column("rec_by")]
        [StringLength(40)]
        public string RecBy { get; set; } = string.Empty;

        [Column("payee_ac")]
        [StringLength(12)]
        public string PayeeAc { get; set; } = string.Empty;

        [Column("payee_a_n")]
        [StringLength(60)]
        public string PayeeAN { get; set; } = string.Empty;

        [Column("desc_1")]
        [StringLength(100)]
        public string Desc1 { get; set; } = string.Empty;

        [Column("paid_amt", TypeName = "decimal(12,2)")]
        public decimal PaidAmt { get; set; }

        [Column("bank_a_c")]
        [StringLength(12)]
        public string BankAC { get; set; } = string.Empty;

        [Column("bank_a_n")]
        [StringLength(60)]
        public string BankAN { get; set; } = string.Empty;

        [Column("ref_no", TypeName = "decimal(20,0)")]
        public decimal RefNo { get; set; }

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;
    }

    // Bank Receipt model for client databases
    [Table("bank_receipt")]
    public class ClientBankReceipt
    {
        [Key]
        [Column("vouch_no")]
        [StringLength(6)]
        public string VouchNo { get; set; } = string.Empty;

        [Column("vouch_date")]
        public DateTime VouchDate { get; set; }

        [Column("rec_by")]
        [StringLength(40)]
        public string RecBy { get; set; } = string.Empty;

        [Column("credit_ac")]
        [StringLength(12)]
        public string CreditAc { get; set; } = string.Empty;

        [Column("credit_a_n")]
        [StringLength(60)]
        public string CreditAN { get; set; } = string.Empty;

        [Column("desc_1")]
        [StringLength(100)]
        public string Desc1 { get; set; } = string.Empty;

        [Column("rec_amt", TypeName = "decimal(12,2)")]
        public decimal RecAmt { get; set; }

        [Column("bank_a_c")]
        [StringLength(12)]
        public string BankAC { get; set; } = string.Empty;

        [Column("bank_a_n")]
        [StringLength(60)]
        public string BankAN { get; set; } = string.Empty;

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;
    }

    // Opening Balance model for client databases
    [Table("op_bal")]
    public class ClientOpeningBalance
    {
        [Key]
        [Column("part_code")]
        [StringLength(12)]
        public string PartCode { get; set; } = string.Empty;

        [Column("ope_date")]
        public DateTime OpeDate { get; set; }

        [Column("acc_name")]
        [StringLength(60)]
        public string AccName { get; set; } = string.Empty;

        [Column("amount", TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [Column("type")]
        [StringLength(2)]
        public string Type { get; set; } = string.Empty;

        [Column("user")]
        [StringLength(2)]
        public string User { get; set; } = string.Empty;

        [Column("byuser")]
        [StringLength(2)]
        public string ByUser { get; set; } = string.Empty;
    }

    // User Info model for client databases
    [Table("user_info")]
    public class ClientUserInfo
    {
        [Key]
        [Column("user_no")]
        [StringLength(2)]
        public string UserNo { get; set; } = string.Empty;

        [Column("user_name")]
        [StringLength(25)]
        public string UserName { get; set; } = string.Empty;

        [Column("password")]
        [StringLength(10)]
        public string Password { get; set; } = string.Empty;

        [Column("user_dept")]
        [StringLength(10)]
        public string UserDept { get; set; } = string.Empty;

        [Column("user_desig")]
        [StringLength(20)]
        public string UserDesig { get; set; } = string.Empty;

        [Column("status")]
        [StringLength(8)]
        public string Status { get; set; } = string.Empty;

        [Column("backup")]
        public bool Backup { get; set; }
    }

    // Voucher Scroll (temporary entry) model for client databases
    [Table("vouch_scrl")]
    public class ClientVoucherScroll
    {
        [Key]
        [Column("sr_no", TypeName = "decimal(3,0)")]
        public decimal SrNo { get; set; }

        [Column("acc_code")]
        [StringLength(12)]
        public string AccCode { get; set; } = string.Empty;

        [Column("ac_name")]
        [StringLength(50)]
        public string AcName { get; set; } = string.Empty;

        [Column("descript")]
        [StringLength(80)]
        public string Descript { get; set; } = string.Empty;

        [Column("dr_amount", TypeName = "decimal(12,2)")]
        public decimal DrAmount { get; set; }

        [Column("cr_amount", TypeName = "decimal(12,2)")]
        public decimal CrAmount { get; set; }
    }

    // Item Variety model for client databases
    [Table("item_variety")]
    public class ClientItemVariety
    {
        [Key]
        [Column("vrty_code")]
        [StringLength(2)]
        public string VrtyCode { get; set; } = string.Empty;

        [Column("vrty_name")]
        [StringLength(30)]
        public string VrtyName { get; set; } = string.Empty;

        [Column("time_stamp")]
        [StringLength(150)]
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
