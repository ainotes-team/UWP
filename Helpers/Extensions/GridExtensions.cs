using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Helpers.Extensions {
    public static class GridExtensions {
        public static void AddChild(this Grid grid, FrameworkElement child, int row, int column) {
            grid.Children.Add(child);
            Grid.SetRow(child, row);
            Grid.SetColumn(child, column);
        }
    }
}