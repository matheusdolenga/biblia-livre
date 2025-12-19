using System;

namespace BibleApp.Models
{
    public class VerseSearchItem
    {
        public int BookId { get; set; }
        public string BookName { get; set; }
        public int TestamentId { get; set; }
        public int Chapter { get; set; }
        public int VerseNumber { get; set; }
        public string Text { get; set; }
        
        // Cached normalized text for fast searching
        public string NormalizedText { get; set; }
    }
}
