using SQLite;
namespace dashboard.Tables;
    public class Modules
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int EenheidId { get; set; }


        public int Hartslag { get; set; }
        public int Zuurstof { get; set; }
        public float Temperatuur { get; set; }

        public string Timestamp { get; set; } = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-dd HH:mm:ss");
    }