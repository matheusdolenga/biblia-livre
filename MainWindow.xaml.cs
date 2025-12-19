using System.Windows;
using BibleApp.ViewModels;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System;
using BibleApp.Helpers;


namespace BibleApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            vm.ScrollToVerseRequested += OnScrollToVerseRequested;
        }
    }



    private async void OnScrollToVerseRequested(int verseNumber)
    {
        // Allow UI to settle (load content)
        await Task.Delay(300);

        // Find the item
        var vm = (this.DataContext as MainViewModel)?.Verses.FirstOrDefault(v => v.Verse.VerseNumber == verseNumber);
        if (vm == null) return;

        // Get Container
        var container = VersesList.ItemContainerGenerator.ContainerFromItem(vm) as FrameworkElement;
        if (container == null) 
        {
            VersesList.UpdateLayout();
            container = VersesList.ItemContainerGenerator.ContainerFromItem(vm) as FrameworkElement;
        }

        if (container != null && ReaderScrollViewer != null)
        {
            // Scroll to Top Alignment
            var transform = container.TransformToAncestor(ReaderScrollViewer);
            var scrollTargetRelative = transform.Transform(new Point(0, 0)).Y;
            var currentOffset = ReaderScrollViewer.VerticalOffset;
            var targetOffset = currentOffset + scrollTargetRelative;
            
            // Check if scroll is needed (tolerance 5px)
            bool needsScroll = Math.Abs(targetOffset - currentOffset) > 5;

            // Setup Animation Logic
            var border = FindVisualChild<Border>(container, "VerseBorder");
            if (border != null)
            {
                // Create Storyboard
                var sb = new Storyboard();
                var ease = new SineEase { EasingMode = EasingMode.EaseInOut };

                // Breath Scale (Up and Down)
                var scaleX = new DoubleAnimation(1, 1.02, new Duration(TimeSpan.FromSeconds(0.6))) { AutoReverse = true, EasingFunction = ease };
                Storyboard.SetTarget(scaleX, border);
                Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                sb.Children.Add(scaleX);

                var scaleY = new DoubleAnimation(1, 1.02, new Duration(TimeSpan.FromSeconds(0.6))) { AutoReverse = true, EasingFunction = ease };
                Storyboard.SetTarget(scaleY, border);
                Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                sb.Children.Add(scaleY);

                // Background Color
                var animBrush = new SolidColorBrush(Colors.Transparent);
                border.Background = animBrush;

                var colorAnim = new ColorAnimation(Colors.Transparent, (Color)ColorConverter.ConvertFromString("#EEEEEE"), new Duration(TimeSpan.FromSeconds(0.6))) { AutoReverse = true, EasingFunction = ease };
                
                Storyboard.SetTarget(colorAnim, border);
                Storyboard.SetTargetProperty(colorAnim, new PropertyPath("(Border.Background).(SolidColorBrush.Color)"));
                sb.Children.Add(colorAnim);

                sb.Completed += (s, e) => 
                {
                    border.ClearValue(Border.BackgroundProperty);
                    border.RenderTransform = null;
                };
                
                // Ensure Transform Exists
                border.RenderTransformOrigin = new Point(0.5, 0.5);
                border.RenderTransform = new ScaleTransform(1, 1);

                if (needsScroll)
                {
                    // Scroll then Animate
                    ScrollViewerHelper.SetVerticalOffset(ReaderScrollViewer, currentOffset);

                    var scrollAnim = new DoubleAnimation(targetOffset, new Duration(TimeSpan.FromMilliseconds(800)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };
                    
                    // Delay breath by 0.5s after scroll
                    sb.BeginTime = TimeSpan.FromSeconds(0.5);

                    scrollAnim.Completed += (s, e) => 
                    {
                         sb.Begin();
                    };

                    ReaderScrollViewer.BeginAnimation(ScrollViewerHelper.VerticalOffsetProperty, scrollAnim);
                }
                else
                {
                    // No scroll needed, just wait 0.5s and Animate
                    await Task.Delay(500);
                    sb.BeginTime = null; // Start immediately after delay
                    sb.Begin();
                }
            }
        }
    }

    private T FindVisualChild<T>(DependencyObject parent, string name = null) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                if (name == null || (child as FrameworkElement)?.Name == name)
                    return typedChild;
            }
            var result = FindVisualChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void DialogHost_DialogClosing(object sender, DialogClosingEventArgs eventArgs)
    {
        if (eventArgs.Parameter is string note && eventArgs.Session.Content is VerseViewModel vm)
        {
            vm.SaveNoteCommand.Execute(note);
        }
    }
}