using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace BibleApp.Helpers
{
    public static class SearchHighlighter
    {
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.RegisterAttached("SearchText", typeof(string), typeof(SearchHighlighter), new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public static string GetSearchText(DependencyObject obj) => (string)obj.GetValue(SearchTextProperty);
        public static void SetSearchText(DependencyObject obj, string value) => obj.SetValue(SearchTextProperty, value);

        public static readonly DependencyProperty HighlightTermProperty =
            DependencyProperty.RegisterAttached("HighlightTerm", typeof(string), typeof(SearchHighlighter), new PropertyMetadata(string.Empty, OnHighlightTermChanged));

        public static string GetHighlightTerm(DependencyObject obj) => (string)obj.GetValue(HighlightTermProperty);
        public static void SetHighlightTerm(DependencyObject obj, string value) => obj.SetValue(HighlightTermProperty, value);

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateTextBlock((TextBlock)d);
        }

        private static void OnHighlightTermChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateTextBlock((TextBlock)d);
        }

        private static void UpdateTextBlock(TextBlock textBlock)
        {
            var text = GetSearchText(textBlock);
            var term = GetHighlightTerm(textBlock);

            textBlock.Inlines.Clear();
            if (string.IsNullOrEmpty(text)) return;

            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                textBlock.Text = text;
                return;
            }

            try
            {
                // Simple case-insensitive split
                // Escape simple regex chars in term
                string pattern = Regex.Escape(term);
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);

                int lastIndex = 0;
                foreach (Match match in matches)
                {
                    // Text before match
                    if (match.Index > lastIndex)
                    {
                        textBlock.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                    }
                    
                    // Match
                    textBlock.Inlines.Add(new Run(text.Substring(match.Index, match.Length)) 
                    { 
                        FontWeight = FontWeights.Bold, 
                        Background = Brushes.Yellow,
                        Foreground = Brushes.Black
                    });

                    lastIndex = match.Index + match.Length;
                }
                
                // Remaining text
                if (lastIndex < text.Length)
                {
                    textBlock.Inlines.Add(new Run(text.Substring(lastIndex)));
                }
            }
            catch
            {
                textBlock.Text = text;
            }
        }
    }
}
