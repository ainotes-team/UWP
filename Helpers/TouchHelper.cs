using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Helpers {
    public enum WTouchAction {
        Entered,
        Pressed,
        Moved,
        Released,
        Cancelled,
        Exited,
    }

    public enum WTouchDeviceType {
        Touch,
        Mouse,
        Pen,
    }

    public enum WMouseButton {
        Unknown,
        Left,
        Middle,
        Right,
    }

    public class WTouchEventArgs {
        public Pointer Pointer { get; }

        public bool Handled { get; set; }
        public long Id { get; }

        public WTouchAction ActionType { get; }
        public WTouchDeviceType DeviceType { get; }
        public WMouseButton MouseButton { get; }
        public Point Location { get; }
        public bool InContact { get; }
        
        public WTouchEventArgs(long id, WTouchAction type, WMouseButton mouseButton, WTouchDeviceType deviceType, Point location, bool inContact, Pointer pointer, bool handled) {
            Id = id;
            ActionType = type;
            DeviceType = deviceType;
            MouseButton = mouseButton;
            Location = location;
            InContact = inContact;
            Pointer = pointer;
            Handled = handled;
        }
    }

    public static class TouchHelper {
        public static void SetTouchEventHandler(UIElement control, Action<WTouchEventArgs> callback) {
            var e = new EventHandler<WTouchEventArgs>((sender, args) => callback(args));

            control.Holding += (sender, args) => {
                WTouchEventArgs wTouch;
                switch (args.HoldingState) {
                    case HoldingState.Started:
                        wTouch = new WTouchEventArgs(-10, WTouchAction.Pressed, WMouseButton.Right, WTouchDeviceType.Touch, args.GetPosition(control), true, null, args.Handled);
                        break;
                    default:
                        wTouch = new WTouchEventArgs(-10, WTouchAction.Released, WMouseButton.Right, WTouchDeviceType.Touch, args.GetPosition(control), false, null, args.Handled);
                        break;
                }

                e.Invoke(control, wTouch);
            };
            control.PointerEntered += (sender, args) => {
                var pointer = args.GetCurrentPoint((UIElement) sender);
                var pos = new Point(pointer.Position.X, pointer.Position.Y);
                var skTouch = new WTouchEventArgs(pointer.PointerId, WTouchAction.Entered, GetMouseButton(pointer), GetTouchDevice(pointer), pos, pointer.IsInContact, args.Pointer, args.Handled);
                e.Invoke(control, skTouch);
            };
            control.PointerPressed += (sender, args) => {
                var pointer = args.GetCurrentPoint((UIElement) sender);
                var pos = new Point(pointer.Position.X, pointer.Position.Y);
                var skTouch = new WTouchEventArgs(pointer.PointerId, WTouchAction.Pressed, GetMouseButton(pointer), GetTouchDevice(pointer), pos, pointer.IsInContact, args.Pointer, args.Handled);
                e.Invoke(control, skTouch);
            };
            control.PointerMoved += (sender, args) => {
                var pointer = args.GetCurrentPoint((UIElement) sender);
                var pos = new Point(pointer.Position.X, pointer.Position.Y);
                var skTouch = new WTouchEventArgs(pointer.PointerId, WTouchAction.Moved, GetMouseButton(pointer), GetTouchDevice(pointer), pos, pointer.IsInContact, args.Pointer, args.Handled);
                e.Invoke(control, skTouch);
            };
            control.PointerReleased += (sender, args) => {
                var pointer = args.GetCurrentPoint((UIElement) sender);
                var pos = new Point(pointer.Position.X, pointer.Position.Y);
                var skTouch = new WTouchEventArgs(pointer.PointerId, WTouchAction.Released, GetMouseButton(pointer), GetTouchDevice(pointer), pos, pointer.IsInContact, args.Pointer, args.Handled);
                e.Invoke(control, skTouch);
            };
            control.PointerExited += (sender, args) => {
                var pointer = args.GetCurrentPoint((UIElement) sender);
                var pos = new Point(pointer.Position.X, pointer.Position.Y);
                var skTouch = new WTouchEventArgs(pointer.PointerId, WTouchAction.Exited, GetMouseButton(pointer), GetTouchDevice(pointer), pos, pointer.IsInContact, args.Pointer, args.Handled);
                e.Invoke(control, skTouch);
            };
            control.PointerCanceled += (sender, args) => {
                var pointer = args.GetCurrentPoint((UIElement) sender);
                var pos = new Point(pointer.Position.X, pointer.Position.Y);
                var skTouch = new WTouchEventArgs(pointer.PointerId, WTouchAction.Cancelled, GetMouseButton(pointer), GetTouchDevice(pointer), pos, pointer.IsInContact, args.Pointer, args.Handled);
                e.Invoke(control, skTouch);
            };
            control.PointerCaptureLost += (sender, args) => {
                var pointer = args.GetCurrentPoint((UIElement) sender);
                var pos = new Point(pointer.Position.X, pointer.Position.Y);
                var skTouch = new WTouchEventArgs(pointer.PointerId, WTouchAction.Cancelled, GetMouseButton(pointer), GetTouchDevice(pointer), pos, pointer.IsInContact, args.Pointer, args.Handled);
                e(control, skTouch);
            };
        }
        
        
        public static WTouchDeviceType GetTouchDevice(PointerPoint pointer) {
            WTouchDeviceType device;
            switch (pointer.PointerDevice.PointerDeviceType) {
                case PointerDeviceType.Pen:
                    device = WTouchDeviceType.Pen;
                    break;
                case PointerDeviceType.Mouse:
                    device = WTouchDeviceType.Mouse;
                    break;
                case PointerDeviceType.Touch:
                    device = WTouchDeviceType.Touch;
                    break;
                default:
                    device = (WTouchDeviceType) 3;
                    break;
            }

            return device;
        }

        public static WMouseButton GetMouseButton(PointerPoint pointer) {
            var properties = pointer.Properties;
            var mouse = WMouseButton.Unknown;

            if (properties.IsLeftButtonPressed)
                mouse = WMouseButton.Left;
            else if (properties.IsMiddleButtonPressed)
                mouse = WMouseButton.Middle;
            else if (properties.IsRightButtonPressed) mouse = WMouseButton.Right;

            switch (properties.PointerUpdateKind) {
                case PointerUpdateKind.LeftButtonPressed:
                case PointerUpdateKind.LeftButtonReleased:
                    mouse = WMouseButton.Left;
                    break;
                case PointerUpdateKind.RightButtonPressed:
                case PointerUpdateKind.RightButtonReleased:
                    mouse = WMouseButton.Right;
                    break;
                case PointerUpdateKind.MiddleButtonPressed:
                case PointerUpdateKind.MiddleButtonReleased:
                    mouse = WMouseButton.Middle;
                    break;
            }

            if (properties.IsEraser) mouse = WMouseButton.Middle;

            return mouse;
        }
    }
}