using System;

namespace AINotes.Helpers.Geometry {
    public class GeometryLine {
        public GeometryPoint P0;
        public GeometryPoint P1;

        public GeometryLine(GeometryPoint p0, GeometryPoint p1) {
            P0 = p0;
            P1 = p1;
        }

        public GeometryLine(HoughLine houghLine, GeometryRectangle bounds) {
            var houghHeight = (int) (Math.Sqrt(2) * Math.Max(bounds.Height, bounds.Width)) / 2;

            // find edge points and vote in array 
            var centerX = (float) (bounds.Width / 2);
            var centerY = (float) (bounds.Height / 2);

            var tsin = Math.Sin(houghLine.Theta);
            var tcos = Math.Cos(houghLine.Theta);

            double x0, x1, y0, y1;
            if (houghLine.Theta < Math.PI * 0.25 || houghLine.Theta > Math.PI * 0.75) {
                // vertical
                y0 = 0;
                y1 = bounds.Height;
                x0 = (int) ((houghLine.R - houghHeight - (y0 - centerY) * tsin) / tcos + centerX);
                x1 = (int) ((houghLine.R - houghHeight - (y1 - centerY) * tsin) / tcos + centerX);

            } else {
                // horizontal
                x0 = 0;
                x1 = bounds.Width;
                y0 = (int) ((houghLine.R - houghHeight - (x0 - centerX) * tcos) / tsin + centerY);
                y1 = (int) ((houghLine.R - houghHeight - (x1 - centerX) * tcos) / tsin + centerY);
            }
            
            P0 = new GeometryPoint(x0, y0);
            P1 = new GeometryPoint(x1, y1);
        }

        public void Deconstruct(out GeometryPoint p0, out GeometryPoint p1) {
            p0 = P0;
            p1 = P1;
        }
    }
}