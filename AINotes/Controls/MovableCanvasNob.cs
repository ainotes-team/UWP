using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Helpers;
using Helpers.Controls;
using Point = Windows.Foundation.Point;

namespace AINotes.Controls {
    public class MovableCanvasNob : CustomFrame {
        // settings
        public const int NobRadius = 15;

        // state
        public bool NobMoving { get; private set; }
        public bool NobMoved { get; private set; }
        public Point TouchTrackingStart { get; private set; }

        // properties
        public Func<bool> TouchCondition;
        public Func<Point, Point> PreprocessNewPosition;
        
        // events
        public event Action<MovableCanvasNob, Point> Moved;
        
        public MovableCanvasNob() {
            Background = Configuration.Theme.Background;
            BorderBrush = new SolidColorBrush(Colors.DarkBlue);
            Opacity = .4;
            CornerRadius = new CornerRadius(NobRadius);
            CaptureTouch = true;
            Width = NobRadius * 2;
            Height = NobRadius * 2;

            Touch += OnTouch;
        }

        private void OnTouch(object sender, WTouchEventArgs args) {
            if (!TouchCondition()) return;
            if (!args.InContact) {
                NobMoving = false;
                return;
            }

            switch (args.ActionType) {
                case WTouchAction.Pressed:
                    TouchTrackingStart = new Point(args.Location.X, args.Location.Y);
                    NobMoving = true;
                    break;
                case WTouchAction.Moved:
                    if (NobMoving) {
                        NobMoved = true;
                        
                        // get values
                        var movedX = X + args.Location.X - TouchTrackingStart.X;
                        var movedY = Y + args.Location.Y - TouchTrackingStart.Y;

                        // preprocess
                        var newPosition = PreprocessNewPosition(new Point(movedX, movedY));
                        
                        // update to new position
                        CanvasHelper.SetPosition(this, newPosition);
                        Moved?.Invoke(this, newPosition);
                    }

                    break;
            }
        }
    }
}