using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;
using AINotes.Helpers.Imaging;
using Helpers;

namespace AINotes.Controls.ImageEditing {
    public partial class AnalyzedImage {
        
        // events
        public event Action OnSizeCalculated;

        public AnalyzedImage(byte[] bytes, string absolutePath, bool autoCrop = true, bool setCropped = false) {
            InitializeComponent();

            Logger.Log("[AnalyzedImage]", $"Constructor: absolutePath={absolutePath}, autoCrop={autoCrop}, setCropped={setCropped}");
            
            AbsolutePath = absolutePath;
            DisplayBytes = bytes;
            CroppedBytes = bytes;
            
            SetImageSource(bytes).GetAwaiter();

            if (autoCrop) AutoCrop(setCropped);
        }

        public void Select() {
            BorderBrush = new SolidColorBrush {
                Color = Colors.Black
            };
        }

        public void Deselect() {
            BorderBrush = null;
        }
        
        public async Task SetImageSource(byte[] imageBytes) {
            Logger.Log("[AnalyzedImage]", "SetImageSource");
            DisplayBytes = imageBytes;
            
            var image = await ImageSourceHelper.AsBitmapImage(imageBytes, this);
            OnSizeCalculated?.Invoke();
            DisplayImage.Source = image;
        }

        public byte[] UsePreCropped() {
            Logger.Log("[AnalyzedImage]", "UsePreCropped");
            SetImageSource(AutoCroppedBytes).Wait();
            return AutoCroppedBytes;
        }

        public void OnSourceChanged() {
            Logger.Log("[AnalyzedImage]", "OnSourceChanged");
            Recalculate();
        }
    }
}