using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Helpers.Extensions {
    public static class CollectionExtensions {
        public static void AddRange(this ColumnDefinitionCollection cdc, IEnumerable<ColumnDefinition> definitions) {
            foreach (var definition in definitions) {
                cdc.Add(definition);
            }
        }
        
        public static void AddRange(this RowDefinitionCollection rdc, IEnumerable<RowDefinition> definitions) {
            foreach (var definition in definitions) {
                rdc.Add(definition);
            }
        }
        
        public static void Add(this UIElementCollection uec, FrameworkElement child, int row, int column) {
            uec.Add(child);
            Grid.SetRow(child, row);
            Grid.SetColumn(child, column);
        }
        
        public static void Add(this UIElementCollection uec, FrameworkElement child, int row, int column, int rowSpan, int columnSpan) {
            uec.Add(child);
            Grid.SetRow(child, row);
            Grid.SetColumn(child, column);
            Grid.SetRowSpan(child, rowSpan);
            Grid.SetColumnSpan(child, columnSpan);
        }
        
        public static void Add(this UIElementCollection uec, FrameworkElement child, Point p) {
            uec.Add(child);
            Canvas.SetLeft(child, p.X);
            Canvas.SetTop(child, p.Y);
        }
    }
}