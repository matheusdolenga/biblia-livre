using SQLite;
using System;

namespace BibleApp.Models
{
    public class Highlight
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int BookId { get; set; }
        public int Chapter { get; set; }
        public int VerseNumber { get; set; }
        public string Color { get; set; } = string.Empty; // Hex code
        public DateTime Date { get; set; }
    }

    public class Note
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int BookId { get; set; }
        public int Chapter { get; set; }
        public int VerseNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
