using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;

namespace AINotes.Models {
    public class InkHelperModel {
        public List<Point> Points;
        public Color Color;
        
        public InkHelperModel(List<Point> points, Color color) {
            Points = points;
            Color = color;
        }
    }
}