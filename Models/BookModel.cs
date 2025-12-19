using SQLite;

namespace BibleApp.Models
{
    [Table("book")]
    public class BookModel
    {
        [Column("id")]
        [PrimaryKey]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }
        
        [Column("testament_reference_id")]
        public int Testament { get; set; }
    }
}
