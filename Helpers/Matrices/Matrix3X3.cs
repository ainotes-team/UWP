using System.Runtime.CompilerServices;
using Windows.Foundation;
using Helpers.Extensions;

namespace Helpers.Matrices {
    public class Matrix3X3 {
        private readonly double[,] _values = new double [3, 3];

        public Matrix3X3() { }

        public Matrix3X3(Point p1, Point p2, Point p3) {
            _values = new double[3, 3];
            
            var (x, y) = p1;
            var (d, d1) = p2;
            var (x1, y1) = p3;

            _values[0, 0] = x;
            _values[0, 1] = d;
            _values[0, 2] = x1;
            
            _values[1, 0] = y;
            _values[1, 1] = d1;
            _values[1, 2] = y1;

            _values[2, 0] = 1.0;
            _values[2, 1] = 1.0;
            _values[2, 2] = 1.0;
        }

        public double Determinant() {
            var result = _values[0, 0] * (_values[1, 1] * _values[2, 2] - _values[1, 2] * _values[2, 1]);
            result -= _values[0, 1] * (_values[1, 0] * _values[2, 2] - _values[1, 2] * _values[2, 0]);
            result += _values[0, 2] * (_values[1, 0] * _values[2, 1] - _values[1, 1] * _values[2, 0]);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetValue(int i, int j) {
            return _values[i - 1, j - 1];
        }

        public void Inverse() {
            var a11 = GetValue(2, 2) * GetValue(3, 3) - GetValue(2, 3) * GetValue(3, 2);
            var a12 = GetValue(2, 1) * GetValue(3, 3) - GetValue(2, 3) * GetValue(3, 1);
            var a13 = GetValue(2, 1) * GetValue(3, 2) - GetValue(2, 2) * GetValue(3, 1);

            var a21 = GetValue(1, 2) * GetValue(3, 3) - GetValue(1, 3) * GetValue(3, 2);
            var a22 = GetValue(1, 1) * GetValue(3, 3) - GetValue(1, 3) * GetValue(3, 1);
            var a23 = GetValue(1, 1) * GetValue(3, 2) - GetValue(1, 2) * GetValue(3, 1);

            var a31 = GetValue(1, 2) * GetValue(2, 3) - GetValue(1, 3) * GetValue(2, 2);
            var a32 = GetValue(1, 1) * GetValue(2, 3) - GetValue(1, 3) * GetValue(2, 1);
            var a33 = GetValue(1, 1) * GetValue(2, 2) - GetValue(1, 2) * GetValue(2, 1);

            var od = 1.0 / Determinant();

            _values[0, 0] = od * a11;
            _values[0, 1] = -od * a21;
            _values[0, 2] = od * a31;

            _values[1, 0] = -od * a12;
            _values[1, 1] = od * a22;
            _values[1, 2] = -od * a32;

            _values[2, 0] = od * a13;
            _values[2, 1] = -od * a23;
            _values[2, 2] = od * a33;
        }

        public void MultiByVector(double[] vector) {
            for (var row = 0; row < 3; ++row) {
                _values[row, 0] *= vector[0];
                _values[row, 1] *= vector[1];
                _values[row, 2] *= vector[2];
            }
        }

        public (int, int) Update(int vX, int vY) {
            var x = _values[0, 0] * vX + _values[0, 1] * vY + _values[0, 2]; 
            var y = _values[1, 0] * vX + _values[1, 1] * vY + _values[1, 2];
            var z = _values[2, 0] * vX + _values[2, 1] * vY + _values[2, 2];
            
            return ((int, int)) (x / z, y / z);
        }

        public Matrix3X3 MultiplyMatrix(Matrix3X3 matrixIn) {
            var result = new Matrix3X3();

            result._values[0, 0] = GetValue(1, 1) * matrixIn.GetValue(1, 1) + GetValue(1, 2) * matrixIn.GetValue(2, 1) + GetValue(1, 3) * matrixIn.GetValue(3, 1);
            result._values[0, 1] = GetValue(1, 1) * matrixIn.GetValue(1, 2) + GetValue(1, 2) * matrixIn.GetValue(2, 2) + GetValue(1, 3) * matrixIn.GetValue(3, 2);
            result._values[0, 2] = GetValue(1, 1) * matrixIn.GetValue(1, 3) + GetValue(1, 2) * matrixIn.GetValue(2, 3) + GetValue(1, 3) * matrixIn.GetValue(3, 3);

            result._values[1, 0] = GetValue(2, 1) * matrixIn.GetValue(1, 1) + GetValue(2, 2) * matrixIn.GetValue(2, 1) + GetValue(2, 3) * matrixIn.GetValue(3, 1);
            result._values[1, 1] = GetValue(2, 1) * matrixIn.GetValue(1, 2) + GetValue(2, 2) * matrixIn.GetValue(2, 2) + GetValue(2, 3) * matrixIn.GetValue(3, 2);
            result._values[1, 2] = GetValue(2, 1) * matrixIn.GetValue(1, 3) + GetValue(2, 2) * matrixIn.GetValue(2, 3) + GetValue(2, 3) * matrixIn.GetValue(3, 3);

            result._values[2, 0] = GetValue(3, 1) * matrixIn.GetValue(1, 1) + GetValue(3, 2) * matrixIn.GetValue(2, 1) + GetValue(3, 3) * matrixIn.GetValue(3, 1);
            result._values[2, 1] = GetValue(3, 1) * matrixIn.GetValue(1, 2) + GetValue(3, 2) * matrixIn.GetValue(2, 2) + GetValue(3, 3) * matrixIn.GetValue(3, 2);
            result._values[2, 2] = GetValue(3, 1) * matrixIn.GetValue(1, 3) + GetValue(3, 2) * matrixIn.GetValue(2, 3) + GetValue(3, 3) * matrixIn.GetValue(3, 3);

            return result;
        }

        public static Matrix3X3 HomogeneousTransformation(Point p1, Point p2, Point p3, Point p4) {
            var major = new Matrix3X3(p1, p2, p3);
            var minor1 = new Matrix3X3(p4, p2, p3);
            var minor2 = new Matrix3X3(p1, p4, p3);
            var minor3 = new Matrix3X3(p1, p2, p4);
            var majorD = major.Determinant();
            var minor1D = minor1.Determinant();
            var minor2D = minor2.Determinant();
            var minor3D = minor3.Determinant();

            var coefficient = new double[3];
            coefficient[0] = minor1D / majorD;
            coefficient[1] = minor2D / majorD;
            coefficient[2] = minor3D / majorD;
            
            major.MultiByVector(coefficient);

            return major;
        }
    }
}