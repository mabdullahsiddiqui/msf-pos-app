using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pos_app.Models
{
    /// <summary>
    /// Represents the session table from the client database
    /// </summary>
    [Table("session")]
    public class ClientSession
    {
        [Key]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("db_path")]
        public string DbPath { get; set; } = string.Empty;

        [Column("dest_1")]
        public string Dest1 { get; set; } = string.Empty;

        [Column("dest_2")]
        public string Dest2 { get; set; } = string.Empty;

        [Column("dest_3")]
        public string Dest3 { get; set; } = string.Empty;

        [Column("dest_cloud")]
        public string DestCloud { get; set; } = string.Empty;

        [Column("sdb_pass")]
        public string SdbPass { get; set; } = string.Empty;

        [Column("db_token")]
        public string DbToken { get; set; } = string.Empty;

        [Column("last_tid")]
        public decimal LastTid { get; set; }

        [Column("wp_count")]
        public string WpCount { get; set; } = string.Empty;

        [Column("wp_login")]
        public string WpLogin { get; set; } = string.Empty;

        [Column("email_ac")]
        public string EmailAc { get; set; } = string.Empty;

        [Column("time_stamp")]
        public string TimeStamp { get; set; } = string.Empty;
    }
}

