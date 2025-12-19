using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using BibleApp.Models;
using BibleApp.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Windows;
using System;

namespace BibleApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly BibleService _bibleService;
        private readonly UserService _userService;
        private readonly VerseOfDayService _verseOfDayService;

        [ObservableProperty]
        private ObservableCollection<BookModel> _books;

        [ObservableProperty]
        private BookModel? _selectedBook;
        
        [ObservableProperty]
        private ObservableCollection<int> _chapters; // Numbers 1..N

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanGoNext))]
        [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
        private int? _selectedChapter;

        public bool CanGoNext => SelectedChapter.HasValue && Chapters.Count > 0 && SelectedChapter.Value < Chapters.Count;
        public bool CanGoPrevious => SelectedChapter.HasValue && SelectedChapter.Value > 1;

        [ObservableProperty]
        private ObservableCollection<VerseViewModel> _verses;

        [ObservableProperty]
        private double _currentFontSize = 18;

        public MainViewModel(BibleService bibleService, UserService userService, VerseOfDayService verseOfDayService)
        {
            _bibleService = bibleService;
            _userService = userService;
            _verseOfDayService = verseOfDayService;

            Books = new ObservableCollection<BookModel>();
            Chapters = new ObservableCollection<int>();
            Verses = new ObservableCollection<VerseViewModel>();
            
            _searchQuery = string.Empty;
            _verseOfDayTitle = string.Empty;

            // Initial Load
            LoadBooksCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadBooks()
        {
            var books = await _bibleService.GetBooksAsync();
            Books.Clear();
            foreach (var b in books) Books.Add(b);
            
            if (Books.Count > 0)
                SelectedBook = Books[0];
        }

        [RelayCommand]
        private void IncreaseFont()
        {
            if (CurrentFontSize < 36) CurrentFontSize += 2;
        }

        [RelayCommand]
        private void DecreaseFont()
        {
            if (CurrentFontSize > 12) CurrentFontSize -= 2;
        }

        partial void OnSelectedBookChanged(BookModel? value)
        {
            if (value == null) return;
            LoadChaptersForBook(value.Id);
        }

        private int? _pendingNavigationChapter;

        private async void LoadChaptersForBook(int bookId)
        {
            SelectedChapter = null; // Reset to safe value before changing collection
            
            int count = await _bibleService.GetChapterCountAsync(bookId);
            if (count == 0) count = 1;

            Chapters.Clear();
            for (int i = 1; i <= count; i++) Chapters.Add(i);
            
            // Auto-select Chapter (Pending or 1)
            if (_pendingNavigationChapter.HasValue && _pendingNavigationChapter.Value <= count)
            {
                SelectedChapter = _pendingNavigationChapter.Value;
                _pendingNavigationChapter = null;
            }
            else
            {
                SelectedChapter = 1;
            }
        }

        partial void OnSelectedChapterChanged(int? value)
        {
            // Only load if valid chapter and book selected
            if (SelectedBook != null && value.HasValue && value.Value > 0)
            {
                LoadChapterContentCommand.ExecuteAsync(null);
            }
        }

        [RelayCommand]
        private void NextChapter()
        {
            if (CanGoNext) SelectedChapter++;
        }

        [RelayCommand]
        private void PreviousChapter()
        {
            if (CanGoPrevious) SelectedChapter--;
        }

        [RelayCommand]
        private async Task LoadChapterContent()
        {
            if (SelectedBook == null || !SelectedChapter.HasValue || SelectedChapter.Value <= 0) return;

            var verses = await _bibleService.GetChapterAsync(SelectedBook.Id, SelectedChapter.Value);
            var highlights = await _userService.GetHighlightsAsync(SelectedBook.Id, SelectedChapter.Value);
            var notes = await _userService.GetNotesAsync(SelectedBook.Id, SelectedChapter.Value);

            Verses.Clear();
            foreach (var v in verses)
            {
                bool isHighlighted = highlights.Exists(h => h.VerseNumber == v.VerseNumber);
                var note = notes.Find(n => n.VerseNumber == v.VerseNumber);
                
                Verses.Add(new VerseViewModel(v, _userService, isHighlighted, note?.Content));
            }

            if (_targetScrollVerse.HasValue)
            {
                ScrollToVerseRequested?.Invoke(_targetScrollVerse.Value);
                _targetScrollVerse = null;
            }
        }

        // --- SEARCH SYSTEM ---
        [ObservableProperty]
        private bool _isSearchMode;

        [ObservableProperty]
        private string _searchQuery;

        [ObservableProperty]
        private ObservableCollection<VerseSearchItem> _searchResults = new();

        // Filters
        [ObservableProperty]
        private int _searchTestamentFilter = 0; // 0: Todos, 1: Velho, 2: Novo

        [ObservableProperty]
        private BookModel? _searchBookFilter;

        private CancellationTokenSource? _searchCts;

        async partial void OnSearchQueryChanged(string value)
        {
            await RunSearch();
        }

        async partial void OnSearchTestamentFilterChanged(int value)
        {
             await RunSearch();
        }

        async partial void OnSearchBookFilterChanged(BookModel? value)
        {
             await RunSearch();
        }

        public event Action<int>? ScrollToVerseRequested;
        private int? _targetScrollVerse;

        [RelayCommand]
        private void ClearBookFilter()
        {
            SearchBookFilter = null;
        }

        [RelayCommand]
        private Task NavigateToSearchResult(VerseSearchItem item)
        {
            if (item == null) return Task.CompletedTask;

            IsSearchMode = false;
            
            var book = Books.FirstOrDefault(b => b.Id == item.BookId);
            if (book != null)
            {
                // Check if we are changing books
                if (SelectedBook?.Id != book.Id)
                {
                    // Different Book: Set pending chapter and let loading logic handle selection
                    // This prevents race condition where LoadChaptersForBook overwrites our selection
                    _pendingNavigationChapter = item.Chapter;
                    _targetScrollVerse = item.VerseNumber; // Will be handled by LoadChapterContent
                    
                    SelectedBook = book; // Triggers OnSelectedBookChanged -> LoadChaptersForBook
                }
                else
                {
                    // Same Book
                    if (SelectedChapter == item.Chapter)
                    {
                        // Same Page -> Invoke Scroll Directly
                        ScrollToVerseRequested?.Invoke(item.VerseNumber);
                    }
                    else
                    {
                        // Different Chapter -> Change Chapter
                        _targetScrollVerse = item.VerseNumber;
                        SelectedChapter = item.Chapter;
                    }
                }
            }
            return Task.CompletedTask;
        }
        
        [RelayCommand]
        private async Task ToggleSearch()
        {
            IsSearchMode = !IsSearchMode;
            if (IsSearchMode)
            {
                // Init cache if not ready
                await _bibleService.InitializeSearchAsync();
            }
        }

        private async Task RunSearch()
        {
             _searchCts?.Cancel();
             _searchCts = new CancellationTokenSource();
             var token = _searchCts.Token;

             if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 3)
             {
                 SearchResults.Clear();
                 return;
             }

             try
             {
                 await Task.Delay(300, token); // Debounce
                 if (token.IsCancellationRequested) return;

                 int? testament = SearchTestamentFilter == 0 ? null : SearchTestamentFilter;
                 int? book = SearchBookFilter?.Id;

                 var results = await _bibleService.SearchVersesAsync(SearchQuery, testament, book);
                 
                 if (token.IsCancellationRequested) return;

                 SearchResults.Clear();
                 foreach (var r in results) SearchResults.Add(r);
             }
             catch (TaskCanceledException) { }
             catch (Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Search Error: {ex.Message}");
             }
        }
        
        // --- SPEECH SYSTEM ---

        [ObservableProperty]
        private bool _isSpeaking;

        private System.Speech.Synthesis.SpeechSynthesizer? _synthesizer;

        [RelayCommand]
        private void ToggleSpeech()
        {
            if (_synthesizer == null)
            {
                _synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
                _synthesizer.SpeakCompleted += (s, e) => IsSpeaking = false;
            }

            if (IsSpeaking)
            {
                _synthesizer.SpeakAsyncCancelAll();
                IsSpeaking = false;
            }
            else
            {
                if (Verses.Count == 0) return;

                var textBuilder = new System.Text.StringBuilder();
                foreach (var v in Verses)
                {
                    // Read only text, skip verse number
                    textBuilder.Append($"{v.Verse.Text} ");
                }

                _synthesizer.SpeakAsync(textBuilder.ToString());
                IsSpeaking = true;
            }
        }
        // --- THEME SYSTEM ---
        [ObservableProperty]
        private bool _isDarkMode;

        [RelayCommand]
        private void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            // Try standard Theme.Dark / Theme.Light properties if available, or direct enum
            // theme.SetBaseTheme(IsDarkMode ? Theme.Dark : Theme.Light);
            
            // Fallback that often works:
            if (IsDarkMode)
                theme.SetBaseTheme(Theme.Dark);
            else
                theme.SetBaseTheme(Theme.Light);
                
            paletteHelper.SetTheme(theme);
        }

        // --- VERSE OF THE DAY ---
        [ObservableProperty]
        private VerseViewModel? _verseOfDay;

        [ObservableProperty]
        private string _verseOfDayTitle;

        [ObservableProperty]
        private bool _isVerseOfDayOpen;

        [RelayCommand]
        private async Task OpenVerseOfDay()
        {
            IsVerseOfDayOpen = true;
            
            if (VerseOfDay == null)
            {
                var verse = await _verseOfDayService.GetVerseOfDayAsync(_userService);
                if (verse != null)
                {
                    VerseOfDay = verse;
                    var book = Books.FirstOrDefault(b => b.Id == verse.Verse.BookId);
                    VerseOfDayTitle = book != null 
                        ? $"{book.Name} {verse.Verse.Chapter}:{verse.Verse.VerseNumber}" 
                        : $"Ref: {verse.Verse.BookId} {verse.Verse.Chapter}:{verse.Verse.VerseNumber}";
                }
            }

            if (VerseOfDay != null)
            {
                // Show Dialog
                var content = new VerseOfDayResult { Verse = VerseOfDay, Title = VerseOfDayTitle };
                await MaterialDesignThemes.Wpf.DialogHost.Show(content, "RootDialog");
            }
        }
    }

    public class VerseOfDayResult
    {
        public VerseViewModel? Verse { get; set; }
        public string? Title { get; set; }
    }

    public partial class VerseViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly UserService _userService;
        public Verse Verse { get; }

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private bool _isHighlighted;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private SolidColorBrush _highlightBrush = Brushes.Transparent;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private bool _hasNote;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private string? _noteContent;

        public VerseViewModel(Verse verse, UserService userService, bool isHighlighted = false, string? noteContent = null)
        {
            Verse = verse;
            _userService = userService;
            IsHighlighted = isHighlighted;
            NoteContent = noteContent;
            HasNote = !string.IsNullOrEmpty(NoteContent);
            UpdateBrush();
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private async Task ToggleHighlight()
        {
            IsHighlighted = !IsHighlighted;
            UpdateBrush();

            if (IsHighlighted)
            {
                await _userService.AddHighlightAsync(new Highlight 
                { 
                    BookId = Verse.BookId, 
                    Chapter = Verse.Chapter, 
                    VerseNumber = Verse.VerseNumber,
                    Color = "#FFFF00",
                    Date = System.DateTime.Now 
                });
            }
            else
            {
                await _userService.RemoveHighlightAsync(Verse.BookId, Verse.Chapter, Verse.VerseNumber);
            }
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private async Task SaveNote(string content) 
        {
            if (string.IsNullOrWhiteSpace(content))
            {
               await _userService.RemoveNoteAsync(Verse.BookId, Verse.Chapter, Verse.VerseNumber);
               NoteContent = null;
               HasNote = false;
            }
            else
            {
               await _userService.AddNoteAsync(new Note
               {
                   BookId = Verse.BookId,
                   Chapter = Verse.Chapter,
                   VerseNumber = Verse.VerseNumber,
                   Content = content,
                   Date = System.DateTime.Now
               });
               NoteContent = content;
               HasNote = true;
            }
        }

        private void UpdateBrush()
        {
            HighlightBrush = IsHighlighted ? new SolidColorBrush(Colors.Yellow) { Opacity = 0.4 } : Brushes.Transparent;
        }
    }
}
