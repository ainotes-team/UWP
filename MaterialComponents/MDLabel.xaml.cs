using System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace MaterialComponents {
    public partial class MDLabel {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), 
            typeof(string),
            typeof(MDLabel),
            PropertyMetadata.Create("")
        );
        
        public string AutomationId {
            set => AutomationProperties.SetAutomationId(TextBlock, value);
        }

        public new double Height {
            get => TextBlock.Height;
            set => TextBlock.Height = value;
        }
        
        public new double FontSize {
            get => TextBlock.FontSize;
            set => TextBlock.FontSize = value;
        }

        public new FontStyle FontStyle {
            get => TextBlock.FontStyle;
            set => TextBlock.FontStyle = value;
        }

        public TextAlignment TextAlignment {
            get => TextBlock.TextAlignment;
            set => TextBlock.TextAlignment = value;
        }
        
        public TextAlignment HorizontalTextAlignment {
            get => TextBlock.HorizontalTextAlignment;
            set => TextBlock.HorizontalTextAlignment = value;
        }

        public new VerticalAlignment VerticalAlignment {
            get => TextBlock.VerticalAlignment;
            set => TextBlock.VerticalAlignment = value;
        }
        public new HorizontalAlignment HorizontalAlignment {
            get => TextBlock.HorizontalAlignment;
            set => TextBlock.HorizontalAlignment = value;
        }

        public string Text {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public new Thickness Margin {
            get => TextBlock.Margin;
            set => TextBlock.Margin = value;
        }

        public int MaxLines {
            get => TextBlock.MaxLines;
            set => TextBlock.MaxLines = value;
        }

        public new Brush Foreground {
            get => TextBlock.Foreground;
            set => TextBlock.Foreground = value;
        }

        public new double MaxWidth {
            get => TextBlock.MaxWidth;
            set => TextBlock.MaxWidth = value;
        }

        public TextTrimming TextTrimming {
            get => TextBlock.TextTrimming;
            set => TextBlock.TextTrimming = value;
        }

        private Brush _highlightBrush;
        public Brush HighlightBrush {
            get => _highlightBrush;
            set {
                _highlightBrush = value;
                UpdateHighlight();
            }
        }

        private int _highlightStart;
        public int HighlightStart {
            get => _highlightStart;
            set {
                _highlightStart = value;
                UpdateHighlight();
            }
        }

        private int _highlightLength;
        public int HighlightLength {
            get => _highlightLength;
            set {
                _highlightLength = value;
                UpdateHighlight();
            }
        }

        public new double ActualWidth => TextBlock.ActualWidth;

        public MDLabel() {
            InitializeComponent();
            TextBlock.Foreground = Theming.CurrentTheme.Text;
        }

        public MDLabel(string text) : this() {
            TextBlock.Text = text;
        }

        public MDLabel(string text, double fontSize) : this(text) {
            TextBlock.FontSize = fontSize;
        }

        public static implicit operator TextBlock(MDLabel self) => self.TextBlock;
        
        private void UpdateHighlight() {
            TextBlock.TextHighlighters.Clear();
            TextBlock.TextHighlighters.Add(new TextHighlighter {
                Background = HighlightBrush,
                Ranges = {
                    new TextRange {
                        StartIndex = HighlightStart,
                        Length = HighlightLength
                    }
                }
            });
        }

        public new event EventHandler<ContextRequestedEventArgs> ContextRequested;

        private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args) {
            ContextRequested?.Invoke(sender, args);
        }
    }
}