using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Helpers;
using AINotes.Helpers.Imaging;
using Helpers.Extensions;
using AINotes.Models;

namespace AINotes.Controls.FileManagement {
    public class LabelFrame : Frame {
        private LabelModel _labelModel;
        
        public readonly int LabelId;

        public LabelFrame(int labelId) {
            LabelId = labelId;
            
            Height = 16;
            Width = 16;
            CornerRadius = new CornerRadius(8);
            Content = new TextBlock {
                Foreground = ColorCreator.FromHex("#2D2D2D").ToBrush(),
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            
            Loading += OnFirstLoad;
        }

        private async void OnFirstLoad(FrameworkElement sender, object args) {
            Loading -= OnFirstLoad;
            
            _labelModel = await FileHelper.GetLabelAsync(LabelId);
            if (_labelModel == null) return;
            Background = _labelModel.Color.ToBrush();
            if (Content is TextBlock txtBlock) {
                txtBlock.Text = _labelModel.Name.Substring(0, 1).ToUpperInvariant();
            }
        }
    }
    
    public sealed partial class FileLabelView {
        // Properties
        public static readonly DependencyProperty IsFavoriteProperty = DependencyProperty.Register("IsFavorite", typeof(bool), typeof(FileLabelView), new PropertyMetadata(null));
        public static readonly DependencyProperty IsSharedProperty = DependencyProperty.Register("IsShared", typeof(bool), typeof(FileLabelView), new PropertyMetadata(null));
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(FileLabelView), new PropertyMetadata(null));
        
        public bool IsFavorite {
            get => (bool) GetValue(IsFavoriteProperty);
            set {
                SetValue(IsFavoriteProperty, value);
                UpdateLabels();
            }
        }

        public bool IsShared {
            get => (bool) GetValue(IsSharedProperty);
            set {
                SetValue(IsSharedProperty, value);
                UpdateLabels();
            }
        }

        public IEnumerable ItemsSource {
            get => (IEnumerable) GetValue(ItemsSourceProperty);
            set {
                SetValue(ItemsSourceProperty, value);
                UpdateLabels();
            }
        }

        private void UpdateLabels() {
            var valueList = ItemsSource?.Cast<int>().ToList() ?? new List<int>();
            
            LabelContainer.Children.Clear();
            
            foreach (var lblId in valueList) {
                LabelContainer.Children.Add(new LabelFrame(lblId));
            }
            if (IsFavorite) LabelContainer.Children.Add(_isFavoriteFrame);
            if (IsShared) LabelContainer.Children.Add(_isSharedFrame);

            LabelContainer.Background = valueList.Count == 0 && !IsFavorite && !IsShared ? null : ColorCreator.FromHex("#8C2D2D2D").ToBrush();
        }
        
        // Special Labels
        private readonly Frame _isFavoriteFrame;
        private readonly Frame _isSharedFrame;

        public FileLabelView() {
            // Logger.Log("-> FileLabelView .ctor");
            InitializeComponent();
            
            _isFavoriteFrame = new Frame {
                Height = 16,
                Width = 16,
                CornerRadius = new CornerRadius(8),
                Content = new Image {
                    Source = ImageSourceHelper.FromName(Icon.White.Star),
                    Margin = new Thickness(2),
                }
            };
            _isSharedFrame = new Frame {
                Height = 16,
                Width = 16,
                CornerRadius = new CornerRadius(8),
                Content = new Image {
                    Source = ImageSourceHelper.FromName(Icon.White.People),
                    Margin = new Thickness(2),
                }
            };
        }
    }
}