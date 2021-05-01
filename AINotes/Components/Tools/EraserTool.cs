using System;
using AINotes.Controls.Pages;
using AINotes.Helpers.Imaging;
using AINotes.Models;
using AINotes.Models.Enums;
using Helpers;
using MaterialComponents;

namespace AINotes.Components.Tools {
    public class EraserTool : MDToolbarItem, ITool {
        public bool RequiresDrawingLayer => true;

        public EraserTool() {
            ImageSource = ImageSourceHelper.FromName(Icon.Eraser);
            Selectable = true;
            Deselectable = false;
        }

        public void Select() {
            SendPress();
            CustomDropdown.CloseDropdown();
            App.Page.SecondaryToolbarChildren.Clear();
            App.EditorScreen.SetInkDrawingMode(InkCanvasMode.Erase);
        }

        public void Deselect() {
            IsSelected = false;
        }
        
        public void SubscribeToPressedEvents(EventHandler<EventArgs> handler) => Pressed += handler;

        public void OnDocumentClicked(WTouchEventArgs touchEventArgs, ComponentModel componentModel) { }
    }
}