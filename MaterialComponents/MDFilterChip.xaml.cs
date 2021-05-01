using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Helpers.Essentials;
using Helpers.Extensions;

namespace MaterialComponents {
    public class BoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            return (bool) value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    
    public sealed partial class MDFilterChip {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), 
            typeof(string),
            typeof(MDChipGroup),
            PropertyMetadata.Create("")
        );
        
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), 
            typeof(bool),
            typeof(MDChipGroup),
            PropertyMetadata.Create(false)
        );

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(ColorBrush), 
            typeof(Brush),
            typeof(MDChipGroup),
            PropertyMetadata.Create(Colors.Transparent.ToBrush())
        );

        public string Text {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool IsSelected {
            get => (bool) GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public Brush ColorBrush {
            get => (Brush) GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public event Action Selected;
        public event Action Deselected;

        public MDFilterChip() {
            InitializeComponent();

            var defaultBackground = ColorCreator.FromHex("#E0E0E0").ToBrush();
            var selectedBackground = ColorCreator.FromHex("#BBBBBB").ToBrush();
            
            MainGrid.PointerReleased += async (sender, args) => {
                IsSelected = !IsSelected;
                
                (IsSelected ? Selected : Deselected)?.Invoke();
                
                await MainThread.InvokeOnMainThreadAsync(() => {
                    MainGrid.Background = IsSelected ? selectedBackground : defaultBackground;
                });
            };
        }
    }
}
