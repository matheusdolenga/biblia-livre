using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BibleApp.Models;

namespace BibleApp.Services
{
    public class UserService
    {
        private SQLiteAsyncConnection _db;

        public UserService()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BibleApp", "user.db");
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            
            _db = new SQLiteAsyncConnection(path);
            _ = InitializeAsync(); // Safe fire and forget with suppression? No, still risky if unhandled.
        }

        private async Task InitializeAsync()
        {
            try
            {
                await _db.CreateTableAsync<Highlight>();
                await _db.CreateTableAsync<Note>();
            }
            catch (Exception ex)
            {
                // Log or handle? For now, swallow to prevent crash
                System.Diagnostics.Debug.WriteLine($"DB Init Error: {ex.Message}");
            }
        }

        public async Task AddHighlightAsync(Highlight highlight)
        {
            await _db.InsertAsync(highlight);
        }

        public async Task RemoveHighlightAsync(int bookId, int chapter, int verse)
        {
            await _db.Table<Highlight>().DeleteAsync(h => h.BookId == bookId && h.Chapter == chapter && h.VerseNumber == verse);
        }

        public async Task<int> GetStreakAsync()
        {
            // Simple file storage for streak
            try
            {
               var dir = Path.GetDirectoryName(_db.DatabasePath);
               if (dir == null) return 0;
               var path = Path.Combine(dir, "streak.txt");
               if (File.Exists(path))
               {
                   var lines = await File.ReadAllLinesAsync(path);
                   if (lines.Length >= 2)
                   {
                       return int.Parse(lines[1]);
                   }
               }
            }
            catch {}
            return 0;
        }

        public async Task UpdateStreakAsync()
        {
             var dir = Path.GetDirectoryName(_db.DatabasePath);
             if (dir == null) return;
             var path = Path.Combine(dir, "streak.txt");
             DateTime lastDate = DateTime.MinValue;
             int currentStreak = 0;

             if (File.Exists(path))
             {
                 var lines = await File.ReadAllLinesAsync(path);
                 if (lines.Length >= 2)
                 {
                     DateTime.TryParse(lines[0], out lastDate);
                     int.TryParse(lines[1], out currentStreak);
                 }
             }

             if (lastDate.Date == DateTime.Now.Date) return; // Already checked in today

             if (lastDate.Date == DateTime.Now.AddDays(-1).Date)
             {
                 currentStreak++;
             }
             else
             {
                 currentStreak = 1;
             }

             await File.WriteAllLinesAsync(path, new string[] { DateTime.Now.ToString(), currentStreak.ToString() });
        }

        public async Task<List<Highlight>> GetHighlightsAsync(int bookId, int chapter)
        {
            return await _db.Table<Highlight>()
                            .Where(h => h.BookId == bookId && h.Chapter == chapter)
                            .ToListAsync();
        }

        public async Task AddNoteAsync(Note note)
        {
            // Upsert: Remove existing for this verse first? Or Update?
            // Let's assume one note per verse for simplicity.
            var existing = await _db.Table<Note>().Where(n => n.BookId == note.BookId && n.Chapter == note.Chapter && n.VerseNumber == note.VerseNumber).FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.Content = note.Content;
                existing.Date = note.Date;
                await _db.UpdateAsync(existing);
            }
            else
            {
                await _db.InsertAsync(note);
            }
        }

        public async Task RemoveNoteAsync(int bookId, int chapter, int verse)
        {
            await _db.Table<Note>().DeleteAsync(n => n.BookId == bookId && n.Chapter == chapter && n.VerseNumber == verse);
        }

        public async Task<Note?> GetNoteAsync(int bookId, int chapter, int verse)
        {
            return await _db.Table<Note>()
                            .Where(n => n.BookId == bookId && n.Chapter == chapter && n.VerseNumber == verse)
                            .FirstOrDefaultAsync();
        }

        public async Task<List<Note>> GetNotesAsync(int bookId, int chapter)
        {
            return await _db.Table<Note>()
                            .Where(n => n.BookId == bookId && n.Chapter == chapter)
                            .ToListAsync();
        }
    }
}
