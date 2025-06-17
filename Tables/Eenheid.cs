using SQLite;
namespace dashboard.Tables;

public class Eenheid
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int EenhedenId { get; set; }

}