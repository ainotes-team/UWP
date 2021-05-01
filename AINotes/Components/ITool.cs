using System;
using AINotes.Models;
using Helpers;

namespace AINotes.Components {
    public interface ITool {
        public bool RequiresDrawingLayer { get; }
        
        public void Select();
        public void Deselect();
        
        public void SubscribeToPressedEvents(EventHandler<EventArgs> handler);
        public void OnDocumentClicked(WTouchEventArgs touchEventArgs, ComponentModel componentModel);
    }
}