using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BibleApp.Models;
using BibleApp.ViewModels;
using System.Linq;

namespace BibleApp.Services
{
    public class VerseReference
    {
        public int BookId { get; set; }
        public int Chapter { get; set; }
        public int Verse { get; set; }
    }

    public class VerseOfDayService
    {
        private readonly BibleService _bibleService;

        // Curated List of Inspirational Verses (BookId mapping: 1=Genesis, 19=Psalms, 43=John, etc.)
        // Note: verify BookIds match the SQLite DB. Usually:
        // OT: 1-39 (Genesis=1, Psalms=19, Prov=20, Isa=23, Jer=24)
        // NT: 40-66 (Matt=40, John=43, Rom=45, Phil=50)
        // I will use a safe subset or generic logic if IDs vary, but assuming standard Protestant 66 books order.
        private static readonly List<VerseReference> _curatedVerses = new List<VerseReference>
        {
            new VerseReference { BookId = 19, Chapter = 23, Verse = 1 },  // Salmos 23:1
            new VerseReference { BookId = 19, Chapter = 91, Verse = 1 },  // Salmos 91:1
            new VerseReference { BookId = 19, Chapter = 119, Verse = 105 }, // Salmos 119:105
            new VerseReference { BookId = 19, Chapter = 121, Verse = 1 },  // Salmos 121:1
            new VerseReference { BookId = 20, Chapter = 3, Verse = 5 },    // Proverbios 3:5
            new VerseReference { BookId = 23, Chapter = 40, Verse = 31 },  // Isaias 40:31
            new VerseReference { BookId = 23, Chapter = 41, Verse = 10 },  // Isaias 41:10
            new VerseReference { BookId = 24, Chapter = 29, Verse = 11 },  // Jeremias 29:11
            new VerseReference { BookId = 27, Chapter = 12, Verse = 3 },   // Daniel 12:3
            new VerseReference { BookId = 40, Chapter = 5, Verse = 3 },    // Mateus 5:3
            new VerseReference { BookId = 40, Chapter = 6, Verse = 33 },   // Mateus 6:33
            new VerseReference { BookId = 40, Chapter = 11, Verse = 28 },  // Mateus 11:28
            new VerseReference { BookId = 43, Chapter = 3, Verse = 16 },   // Joao 3:16
            new VerseReference { BookId = 43, Chapter = 14, Verse = 6 },   // Joao 14:6
            new VerseReference { BookId = 43, Chapter = 14, Verse = 27 },  // Joao 14:27
            new VerseReference { BookId = 45, Chapter = 8, Verse = 28 },   // Romanos 8:28
            new VerseReference { BookId = 45, Chapter = 12, Verse = 2 },   // Romanos 12:2
            new VerseReference { BookId = 46, Chapter = 13, Verse = 13 },  // 1 Corintios 13:13
            new VerseReference { BookId = 47, Chapter = 5, Verse = 7 },    // 2 Corintios 5:7
            new VerseReference { BookId = 49, Chapter = 2, Verse = 8 },    // Efesios 2:8
            new VerseReference { BookId = 50, Chapter = 4, Verse = 6 },    // Filipenses 4:6
            new VerseReference { BookId = 50, Chapter = 4, Verse = 13 },   // Filipenses 4:13
            new VerseReference { BookId = 51, Chapter = 3, Verse = 16 },   // Colossenses 3:16
            new VerseReference { BookId = 52, Chapter = 5, Verse = 16 },   // 1 Tessalonicenses 5:16
            new VerseReference { BookId = 55, Chapter = 1, Verse = 7 },    // 2 Timoteo 1:7
            new VerseReference { BookId = 58, Chapter = 11, Verse = 1 },   // Hebreus 11:1
            new VerseReference { BookId = 59, Chapter = 1, Verse = 5 },    // Tiago 1:5
            new VerseReference { BookId = 60, Chapter = 5, Verse = 7 },    // 1 Pedro 5:7
            new VerseReference { BookId = 62, Chapter = 4, Verse = 7 },    // 1 Joao 4:7
            new VerseReference { BookId = 66, Chapter = 21, Verse = 4 }    // Apocalipse 21:4
        };

        public VerseOfDayService(BibleService bibleService)
        {
            _bibleService = bibleService;
        }

        public async Task<VerseViewModel> GetVerseOfDayAsync(UserService userService)
        {
            // Pick based on day of year to ensure consistency for the whole day
            int dayOfYear = DateTime.Now.DayOfYear;
            
            // Cycle through the list: Index = Day % Count
            int index = dayOfYear % _curatedVerses.Count;
            var reference = _curatedVerses[index];

            var verses = await _bibleService.GetChapterAsync(reference.BookId, reference.Chapter);
            var verse = verses.FirstOrDefault(v => v.VerseNumber == reference.Verse);

            if (verse != null)
            {
                // We also need the Book Name properly formatted
                return new VerseViewModel(verse, userService, false, null);
            }
            
            return null;
        }
    }
}
