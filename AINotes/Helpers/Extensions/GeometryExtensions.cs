using System.Collections.Generic;
using System.Linq;
using AINotes.Helpers.Geometry;

namespace AINotes.Helpers.Extensions {
    public static class GeometryExtensions {
        public static GeometryPolyline ToGeometryPolyline(this IEnumerable<GeometryPoint> enumerable) {
            return new GeometryPolyline(enumerable.ToArray());
        }
    }
}