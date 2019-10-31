using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;

namespace FileDissector.Infrastructure
{
    public class SearchableTextControl : Control
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(SearchableTextControl), new UIPropertyMetadata(string.Empty, UpdateControlCallback));

        public static readonly DependencyProperty HighlightBackgroundProperty =
            DependencyProperty.Register("HighlightBackground", typeof(Brush), typeof(SearchableTextControl), new UIPropertyMetadata(Brushes.Yellow, UpdateControlCallback));

        public static readonly DependencyProperty HighlightForegroundProperty =
            DependencyProperty.Register("HighlightForeground", typeof(Brush), typeof(SearchableTextControl), new UIPropertyMetadata(Brushes.Black, UpdateControlCallback));

        public static readonly DependencyProperty IsMatchCaseProperty =
            DependencyProperty.Register("IsMatchCase", typeof(bool), typeof(SearchableTextControl), new UIPropertyMetadata(true, UpdateControlCallback));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(SearchableTextControl), new UIPropertyMetadata(string.Empty, UpdateControlCallback));

        public static readonly DependencyProperty IsHighlightProperty =
            DependencyProperty.Register("IsHighlight", typeof(bool), typeof(SearchableTextControl), new UIPropertyMetadata(false, UpdateControlCallback));

        static SearchableTextControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchableTextControl), new FrameworkPropertyMetadata(typeof(SearchableTextControl)));
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Brush HighlightBackground
        {
            get => (Brush)GetValue(HighlightBackgroundProperty);
            set => SetValue(HighlightBackgroundProperty, value);
        }

        public Brush HighlightForeground
        {
            get => (Brush)GetValue(HighlightForegroundProperty);
            set => SetValue(HighlightForegroundProperty, value);
        }

        public bool IsMatchCase
        {
            get => (bool)GetValue(IsMatchCaseProperty);
            set => SetValue(IsMatchCaseProperty, value);
        }


        public bool IsHighlight
        {
            get => (bool)GetValue(IsHighlightProperty);
            set => SetValue(IsHighlightProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        private static void UpdateControlCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SearchableTextControl control = d as SearchableTextControl;

            control?.InvalidateVisual();
        }

        /// <summary>
        /// Overrides the OnRender method which is used to search for the keyword andhighlightit when the operation gets the result
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (string.IsNullOrEmpty(Text))
            {
                base.OnRender(drawingContext);
                return;
            }

            // Define a TextBlock to hold the search result
            TextBlock displayTextBlock = this.Template.FindName("PART_TEXT", this) as TextBlock;

            if (displayTextBlock == null)
            {
                return;
            }

            if (!IsHighlight)
            {
                displayTextBlock.Text = Text;
                base.OnRender(drawingContext);
                return;
            }
           
            displayTextBlock.Inlines.Clear();
            string searchString = IsMatchCase ? SearchText : SearchText.ToUpper();

            string compareText = IsMatchCase ? Text : Text.ToUpper();
            string displayText = Text;

            Run run;
            while (!string.IsNullOrEmpty(searchString) && compareText.IndexOf(searchString, StringComparison.Ordinal) >= 0)
            {
                int position = compareText.IndexOf(searchString, StringComparison.Ordinal);
                run = GenerateRun(displayText.Substring(0, position), false);

                if (run != null)
                {
                    displayTextBlock.Inlines.Add(run);
                }

                run = GenerateRun(displayText.Substring(position, searchString.Length), true);

                if (run != null)
                {
                    displayTextBlock.Inlines.Add(run);
                }

                compareText = compareText.Substring(position + searchString.Length);
                displayText = displayText.Substring(position + searchString.Length);
            }

            run = GenerateRun(displayText, false);

            if (run != null)
            {
                displayTextBlock.Inlines.Add(run);
            }

            base.OnRender(drawingContext);
        }

        /// <summary>
        /// Set inline-level flow content element intended to contain a run of formatted or unformatted text into your background and foreground setting
        /// </summary>
        /// <param name="searchedString"></param>
        /// <param name="isHighlight"></param>
        /// <returns></returns>
        private Run GenerateRun(string searchedString, bool isHighlight)
        {
            if (string.IsNullOrEmpty(searchedString)) return null;
            var run = new Run(searchedString)
            {
                Background = isHighlight ? HighlightBackground : Background,
                Foreground = isHighlight ? HighlightForeground : Foreground,
                FontWeight = isHighlight ? FontWeights.Bold : FontWeights.Normal
            };
            return run;
        }
    }
}
