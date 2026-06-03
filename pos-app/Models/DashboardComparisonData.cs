namespace pos_app.Models
{
    public class DashboardComparisonData
    {
        public string TransMonth { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal RecAmt { get; set; }
        public decimal ExpAmt { get; set; }
    }
}
