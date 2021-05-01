using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Helpers.Extensions;
using AINotes.Models.Enums;

namespace AINotes.Controls.Containers {
    public partial class CustomMaxSizeBackgroundCanvas {
        private DocumentLineMode _lineMode = Preferences.BackgroundDefaultLineMode;

        public DocumentLineMode LineMode {
            get => _lineMode;
            set {
                _lineMode = value;
                Rebuild();
            }
        }

        public double VerticalStep => GetStepSizes(LineMode).Item1;
        public double HorizontalStep => GetStepSizes(LineMode).Item2;
        
        private const int Buffer = 500;

        private ScrollViewer _parentScrollView;
        public ScrollViewer ParentScrollView {
            get => _parentScrollView;
            set {
                if (_parentScrollView != null) {
                    _parentScrollView.ViewChanged -= OnParentScrollViewOnScrolled;
                }
                
                _parentScrollView = value;
                _parentScrollView.ViewChanged += OnParentScrollViewOnScrolled;
            }
        }

        public CustomMaxSizeBackgroundCanvas() {
            InitializeComponent();
            
            Width = 2000;
            Height = 2000;
            
            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object s, SizeChangedEventArgs e) {
            Rebuild();
        }

        private void OnParentScrollViewOnScrolled(object sender, object args) {
            // Logger.Log(Width, "|", Height, "<=", _parentScrollView.ExtentWidth, "|", _parentScrollView.ExtentHeight, "||", _parentScrollView.HorizontalOffset, "|", _parentScrollView.VerticalOffset);
            var widthRemaining = ParentScrollView.ExtentWidth - (ParentScrollView.HorizontalOffset + ParentScrollView.RenderSize.Width);
            var heightRemaining = ParentScrollView.ExtentHeight - (ParentScrollView.VerticalOffset + ParentScrollView.RenderSize.Height);
            
            if (widthRemaining < Buffer) {
                Width += Buffer;
                // Logger.Log("WBuffer", Width);
                ParentScrollView.InvalidateMeasure();
            }
            
            if (heightRemaining < Buffer) {
                Height += Buffer;
                // Logger.Log("HBuffer", Height);
                ParentScrollView.InvalidateMeasure();
            }
        }

        private (double, double) GetStepSizes(DocumentLineMode mode) {
            switch (mode) {
                case DocumentLineMode.LinesSmall:
                    return (Preferences.BackgroundLineModeStepsSmall, 0);
                case DocumentLineMode.LinesMedium:
                    return (Preferences.BackgroundLineModeStepsMedium, 0);
                case DocumentLineMode.LinesLarge:
                    return (Preferences.BackgroundLineModeStepsLarge, 0);

                case DocumentLineMode.GridSmall:
                    return (Preferences.BackgroundLineModeStepsSmall, Preferences.BackgroundLineModeStepsSmall);
                case DocumentLineMode.GridMedium:
                    return (Preferences.BackgroundLineModeStepsMedium, Preferences.BackgroundLineModeStepsMedium);
                case DocumentLineMode.GridLarge:
                    return (Preferences.BackgroundLineModeStepsLarge, Preferences.BackgroundLineModeStepsLarge);
                
                case DocumentLineMode.None:
                    return (0, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, @"out of range");
            }
        }

        public void Rebuild() {
            Background = ColorCreator.FromHex(Preferences.BackgroundCanvasColor).ToBrush();
                
            Children.Clear();
            var horizontalStep = HorizontalStep;
            var verticalStep = VerticalStep;
            var brush = ColorCreator.FromHex(Preferences.BackgroundCanvasLineColor).ToBrush();
            brush.Opacity = ((double) Preferences.BackgroundCanvasOpacity / 100).Clamp(0, 1);

            if (horizontalStep > 0) {
                for (var x = horizontalStep; x < ActualWidth; x += horizontalStep) {
                    var line = new Rectangle {
                        Width = 1,
                        Height = ActualHeight,
                        Fill = brush
                    };
                    SetLeft(line, x);

                    Children.Add(line);
                }
            }

            if (verticalStep > 0) {
                for (var y = horizontalStep; y < ActualHeight; y += verticalStep) {
                    var line = new Rectangle {
                        Width = ActualWidth,
                        Height = 1,
                        Fill = brush
                    };
                    SetTop(line, y);

                    Children.Add(line);
                }
            }
        }
    }
}