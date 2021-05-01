using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Helpers.Controls {
    public class CustomFrame : Frame {
        public event EventHandler<WTouchEventArgs> Touch;
        
        public bool CaptureTouch { get; set; }
        
        public double X => Canvas.GetLeft(this);
        public double Y => Canvas.GetTop(this);
        
        public string ToolTip {
            set => ToolTipService.SetToolTip(this, new ToolTip { Content = value });
        }

        public CustomFrame() {
            TouchHelper.SetTouchEventHandler(this, InvokeTouch);
            
            PointerPressed += OnPointerPressed;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs args) {
            if (!CaptureTouch) return;
            Logger.Log("Captured PointerPressed");
            CapturePointer(args.Pointer);
        }

        private void InvokeTouch(WTouchEventArgs args) {
            Touch?.Invoke(this, args);
        }
    }
}