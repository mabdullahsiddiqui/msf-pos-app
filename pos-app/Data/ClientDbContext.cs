using Microsoft.EntityFrameworkCore;
using pos_app.Models;

namespace pos_app.Data
{
    /// <summary>
    /// Entity Framework DbContext for client databases (multi-tenant)
    /// Supports both SQLite and SQL Server providers
    /// </summary>
    public class ClientDbContext : DbContext
    {
        public ClientDbContext(DbContextOptions<ClientDbContext> options) : base(options)
        {
        }

        // Core transactional entities
        public DbSet<ClientCustomer> Customers { get; set; }
        public DbSet<ClientItem> Items { get; set; }
        public DbSet<ClientItemGroup> ItemGroups { get; set; }
        public DbSet<ClientItemVariety> ItemVarieties { get; set; }

        // Sales entities
        public DbSet<ClientSaleInvoice> SaleInvoices { get; set; }
        public DbSet<ClientSaleInvoiceDetail> SaleInvoiceDetails { get; set; }
        public DbSet<ClientSaleContract> SaleContracts { get; set; }
        public DbSet<ClientSaleReturnInvoice> SaleReturnInvoices { get; set; }
        public DbSet<ClientSaleReturnDetail> SaleReturnDetails { get; set; }

        // Purchase entities
        public DbSet<ClientPurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<ClientPurchaseInvoiceDetail> PurchaseInvoiceDetails { get; set; }
        public DbSet<ClientPurchaseContract> PurchaseContracts { get; set; }

        // Voucher/Journal entities
        public DbSet<ClientJournalVoucher> JournalVouchers { get; set; }
        public DbSet<ClientGeneralJournal> GeneralJournals { get; set; }
        public DbSet<ClientCashPaymentVoucher> CashPaymentVouchers { get; set; }
        public DbSet<ClientCashReceiptVoucher> CashReceiptVouchers { get; set; }
        public DbSet<ClientBankPayment> BankPayments { get; set; }
        public DbSet<ClientBankReceipt> BankReceipts { get; set; }
        public DbSet<ClientVoucherScroll> VoucherScrolls { get; set; }

        // System entities
        public DbSet<ClientOpeningBalance> OpeningBalances { get; set; }
        public DbSet<ClientUserInfo> UserInfos { get; set; }
        public DbSet<ClientSession> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite keys for detail tables (invoice line items)
            modelBuilder.Entity<ClientSaleInvoiceDetail>()
                .HasKey(e => new { e.InvNo, e.InvType, e.ItemCode });

            modelBuilder.Entity<ClientPurchaseInvoiceDetail>()
                .HasKey(e => new { e.InvNo, e.InvType, e.ItemCode });

            modelBuilder.Entity<ClientSaleReturnDetail>()
                .HasKey(e => new { e.InvNo, e.ItemCode });

            // Configure composite key for journal voucher
            modelBuilder.Entity<ClientJournalVoucher>()
                .HasKey(e => new { e.VouchNo, e.SrNo });

            // Configure composite key for general journal
            modelBuilder.Entity<ClientGeneralJournal>()
                .HasKey(e => new { e.VouchNo, e.SrNo });

            // Configure composite key for cash payment voucher
            modelBuilder.Entity<ClientCashPaymentVoucher>()
                .HasKey(e => new { e.VouchNo, e.SrNo });

            // Configure composite key for cash receipt voucher
            modelBuilder.Entity<ClientCashReceiptVoucher>()
                .HasKey(e => new { e.VouchNo, e.SrNo });

            // Note: We're not configuring migrations since client databases are managed by desktop app
            // EF is used here for read/write operations only, not schema management
        }
    }
}

