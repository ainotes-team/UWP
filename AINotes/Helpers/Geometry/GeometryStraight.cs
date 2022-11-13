namespace AINotes.Helpers.Geometry; 

public class GeometryStraight {
    public readonly double M;
    public readonly double B;

    public GeometryStraight(GeometryPoint referencePoint, double m) {
        M = m;
        B = referencePoint.Y - m * referencePoint.X;
    }

    public override bool Equals(object obj) {
        if (obj is GeometryStraight geometryStraight) {
            return geometryStraight.M == M && geometryStraight.B == B;
        }

        return false;
    }

    protected bool Equals(GeometryStraight other) {
        return M.Equals(other.M) && B.Equals(other.B);
    }

    public override int GetHashCode() {
        unchecked {
            return (M.GetHashCode() * 397) ^ B.GetHashCode();
        }
    }
}