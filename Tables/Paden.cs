using SQLite;
namespace dashboard.Tables;
    public class Paden
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int ModulesId { get; set; }        
        public string Timestamp { get; set; } = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-dd HH:mm:ss");
        public string? LocationLat { get; set; }
        public string? LocationLon { get; set; }
    }