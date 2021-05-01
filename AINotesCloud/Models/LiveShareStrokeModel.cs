using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Newtonsoft.Json;

namespace AINotesCloud.Models {
    public class LiveShareStrokeModel {
        [JsonIgnore]
        public List<InkPoint> InkPoints {
            get => InternalInkPoints.Select(s => new InkPoint(s.Item1, s.Item2)).ToList();
            set => InternalInkPoints = value.Select(p => (p.Position, p.Pressure)).ToList();
        }

        [JsonProperty("inkPoints")]
        public List<(Point, float)> InternalInkPoints { get; set; }

        [JsonProperty("drawingAttributes")]
        public InkDrawingAttributes InkDrawingAttributes;

        [JsonProperty("transformation")]
        public Matrix3x2 Transformation;

        public LiveShareStrokeModel() { }

        public LiveShareStrokeModel(List<InkPoint> inkPoints, InkDrawingAttributes inkDrawingAttributes, Matrix3x2 transformation) {
            InkPoints = inkPoints;
            InkDrawingAttributes = inkDrawingAttributes;
            Transformation = transformation;
        }

        public LiveShareStrokeModel(InkStroke stroke) {
            InkPoints = stroke.GetInkPoints().ToList();
            InkDrawingAttributes = stroke.DrawingAttributes;
            Transformation = stroke.PointTransform;
        }

        private InkStrokeBuilder _builder;

        public InkStroke GetInkStroke() {
            if (_builder == null) _builder = new InkStrokeBuilder();
            _builder.SetDefaultDrawingAttributes(InkDrawingAttributes);
            return _builder.CreateStrokeFromInkPoints(InkPoints, Transformation);
        }
    }
}