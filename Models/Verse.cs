using SQLite;

namespace BibleApp.Models
{
    [Table("verse")]
    public class Verse
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("book_id")]
        public int BookId { get; set; }

        [Column("chapter")]
        public int Chapter { get; set; }

        [Column("verse")]
        public int VerseNumber { get; set; }

        [Column("text")]
        public string Text { get; set; }
    }
}
