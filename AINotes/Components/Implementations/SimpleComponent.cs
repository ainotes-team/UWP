using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Models;
using Helpers;

namespace AINotes.Components.Implementations {
    public class SimpleComponent : Component {
        private UIElement _view;

        // public override void SetData(object data) {
        //     var stringData = (string) data;
        //     if (string.IsNullOrWhiteSpace(stringData)) return;
        //     Logger.Log("[SimpleComponent]", $"SetData({stringData})");
        //     var viewType = TypeHelper.FindType(stringData);
        //     if (viewType == null) return;
        //     Children.Add(_view =  (UIElement) Activator.CreateInstance(viewType));
        // }

        // ReSharper disable once UnusedMember.Global
        public SimpleComponent(ComponentModel componentModel) : base(componentModel) {
            Logger.Log("[SimpleComponent]", $"SimpleComponent({componentModel.ComponentId})");
            Movable = Resizeable = true;
            ResizeableToRight = false;
            MinWidth = 80;
            MinHeight = 25;
        }

        public SimpleComponent(ComponentModel componentModel, UIElement view) : base(componentModel) {
            Logger.Log("[SimpleComponent]", $"SimpleComponent({componentModel.ComponentId}, {view?.ToString() ?? "null"})");
            Movable = Resizeable = true;
            ResizeableToRight = false;
            MinWidth = 80;
            MinHeight = 25;
            if (view != null) Children.Add(_view = view);
        }

        protected override void Focus() {
            (_view as Control)?.Focus(FocusState.Programmatic);
        }

        protected override FrameworkElement GetFocusTarget() => _view as Control;
        
        public override void Unfocus() {
            if (!(_view is Control content)) return;
            var isTabStop = content.IsTabStop;
            content.IsTabStop = false;
            content.IsEnabled = false;
            content.IsEnabled = true;
            content.IsTabStop = isTabStop;
        }
    }
}