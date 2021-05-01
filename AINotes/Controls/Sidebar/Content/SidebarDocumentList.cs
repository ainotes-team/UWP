using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.FileManagement;
using System;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class SidebarDocumentList : Frame, ISidebarView {
        private static readonly MDToolbarItem ParentDirectoryButton = new MDToolbarItem {
            ImageSource = new BitmapImage(new Uri(Icon.ArrowLeft))
        };
        
        public IEnumerable<MDToolbarItem> ExtraButtons { get; } = new [] {
            ParentDirectoryButton
        };

        private bool _isFirstOverride = true;
        protected override Size ArrangeOverride(Size finalSize) {
            // ReSharper disable once InvertIf
            if (_isFirstOverride) {
                Content = new CustomFileGridView();
                _isFirstOverride = false;
            }
            return base.ArrangeOverride(finalSize);
        }
    }
}