using System;
using System.IO;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using BibleApp.Models;
using System.Linq;

namespace BibleApp.Services
{
    public class BibleService
    {
        private SQLiteAsyncConnection _db;
        private string _dbPath;

        public BibleService(string dbPath)
        {
            _dbPath = dbPath;
            _db = new SQLiteAsyncConnection(_dbPath);
        }

        public async Task<List<BookModel>> GetBooksAsync()
        {
            try
            {
                return await _db.Table<BookModel>().ToListAsync();
            }
            catch
            {
                return new List<BookModel>(); 
            }
        }

        private static void Log(string message)
        {
            try { File.AppendAllText("debug_log.txt", $"{DateTime.Now}: {message}\n"); } catch {}
        }

        public async Task<int> GetChapterCountAsync(int bookId)
        {
            try
            {
                return await _db.ExecuteScalarAsync<int>("SELECT MAX(chapter) FROM verse WHERE book_id = ?", bookId);
            }
            catch
            {
               return 1; // Fallback
            }
        }

        public async Task<List<Verse>> GetChapterAsync(int bookId, int chapter)
        {
            return await _db.Table<Verse>()
                            .Where(v => v.BookId == bookId && v.Chapter == chapter)
                            .OrderBy(v => v.VerseNumber)
                            .ToListAsync();
        }

        // --- SEARCH SYSTEM ---
        private List<VerseSearchItem> _searchCache = new();
        private bool _isSearchInitialized = false;

        public async Task InitializeSearchAsync()
        {
            if (_isSearchInitialized) return;

            try 
            {
                // Join Verse and Book to get everything we need in one go
                var query = @"
                    SELECT 
                        v.book_id AS BookId,
                        b.name AS BookName,
                        b.testament_reference_id AS TestamentId,
                        v.chapter AS Chapter,
                        v.verse AS VerseNumber,
                        v.text AS Text
                    FROM verse v
                    JOIN book b ON v.book_id = b.id";

                var rawData = await _db.QueryAsync<VerseSearchItem>(query);
                
                // Process in parallel for speed if needed, but 30k is fast enough sequentially usually
                _searchCache = rawData.Select(v => {
                    v.NormalizedText = RemoveDiacritics(v.Text).ToLowerInvariant();
                    return v;
                }).ToList();

                _isSearchInitialized = true;
            }
            catch (Exception ex)
            {
                Log($"Search Init Error: {ex.Message}");
                _searchCache = new List<VerseSearchItem>();
            }
        }

        public async Task<List<VerseSearchItem>> SearchVersesAsync(string query, int? testamentId = null, int? bookId = null)
        {
            if (!_isSearchInitialized || _searchCache == null) await InitializeSearchAsync();

            if (string.IsNullOrWhiteSpace(query)) return new List<VerseSearchItem>();

            var normalizedQuery = RemoveDiacritics(query).ToLowerInvariant();

            // Simple LINQ filtering
            var results = _searchCache!.AsEnumerable();

            if (testamentId.HasValue) results = results.Where(v => v.TestamentId == testamentId.Value);
            if (bookId.HasValue) results = results.Where(v => v.BookId == bookId.Value);

            // Filter by text
            results = results.Where(v => v.NormalizedText.Contains(normalizedQuery));

            // Prioritize whole word matches
            // We want "Maria" to come before "Tomaria"
            // Whole word means: Exact match OR Starts with "query " OR Ends with " query" OR Contains " query "
            var startPattern = normalizedQuery + " ";
            var endPattern = " " + normalizedQuery;
            var middlePattern = " " + normalizedQuery + " ";

            results = results.OrderByDescending(v => 
                v.NormalizedText == normalizedQuery || 
                v.NormalizedText.StartsWith(startPattern) || 
                v.NormalizedText.EndsWith(endPattern) || 
                v.NormalizedText.Contains(middlePattern)
            );

            // Return top 100 to avoid UI lag
            return results.Take(100).ToList();
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
