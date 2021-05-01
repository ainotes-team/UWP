using System;

namespace AINotes.Helpers.Geometry {
    public class HoughLine {
        public double Theta; 
        public double R;

        public double Angle => Theta / Math.PI * 180.0;

        public HoughLine(double theta, double r) { 
            Theta = theta; 
            R = r; 
        }

        public void Deconstruct(out double rho, out double theta) {
            rho = R;
            theta = Theta;
        }
    }
}