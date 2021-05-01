using System;
using AINotes.Controls.Pages;
using AINotes.Helpers.Imaging;
using AINotes.Models;
using Helpers;
using MaterialComponents;

namespace AINotes.Components.Tools {
    public class SimpleTool : MDToolbarItem, ITool {
        public bool RequiresDrawingLayer => false;

        public SimpleTool() {
            ImageSource = ImageSourceHelper.FromName(Icon.Access);
            Selectable = true;
            Deselectable = false;
        }
        
        public SimpleTool(string iconName) {
            ImageSource = ImageSourceHelper.FromName(iconName);
            Selectable = true;
            Deselectable = false;
        }

        public void Select() {
            SendPress();
            CustomDropdown.CloseDropdown();
            App.Page.SecondaryToolbarChildren.Clear();
        }

        public void Deselect() {
            IsSelected = false;
        }

        public Action<WTouchEventArgs, ComponentModel> DocumentClickedCallback;
        
        public void SubscribeToPressedEvents(EventHandler<EventArgs> handler) => Pressed += handler;

        public void OnDocumentClicked(WTouchEventArgs touchEventArgs, ComponentModel componentModel) {
            DocumentClickedCallback?.Invoke(touchEventArgs, componentModel);
        }
    }
}