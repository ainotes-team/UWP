using System.Collections.Generic;
using Windows.UI;
using Newtonsoft.Json;
using SQLite;

namespace AINotes.Models {
    public class StrokePointModel {
        public float X { get; set; }
        public float Y { get; set; }
        public float Pressure { get; set; }

        public StrokePointModel(float x, float y, float pressure) {
            X = x;
            Y = y;
            Pressure = pressure;
        }

        public static implicit operator float[](StrokePointModel x) => new[] {x.X, x.Y, x.Pressure};
    }

    public enum PenTip {
        Circle,
        Rectangle
    }

    public enum PenType {
        Default,
        Marker
    }

    public class StrokeModel {
        [Ignore]
        public List<float[]> Points { get; set; }

        [Ignore]
        public Color Color { get; set; }

        [Ignore]
        public double Width { get; set; }

        [Ignore]
        public double Height { get; set; }

        [Ignore]
        public double Transparency { get; set; }

        [Ignore]
        public PenTip PenTip { get; set; }
        
        [Ignore]
        public PenType PenType { get; set; }
        
        [Ignore]
        public bool IgnorePressure { get; set; }
        
        [Ignore]
        public bool AntiAliased { get; set; }
        
        [Ignore]
        public bool FitToCurve { get; set; }
        
        
        [Column("Points")]
        [JsonIgnore]
        public string DatabasePoints {
            get => JsonConvert.SerializeObject(Points);
            set => Points = JsonConvert.DeserializeObject<List<float[]>>(value);
        }
        
        [Column("Color")]
        [JsonIgnore]
        public string DatabaseColor {
            get => $"{Color.R},{Color.G},{Color.B},{Color.A}";
            set {
                var split = value.Split(',');
                var r = double.Parse(split[0]);
                var g = double.Parse(split[1]);
                var b = double.Parse(split[2]);
                var a = double.Parse(split[3]);
                Color = Color.FromArgb((byte) a, (byte) r, (byte) g, (byte) b);
            }
        }

        public int StrokeId { get; set; }

        public StrokeModel(List<float[]> points, Color color, double width, double height, double transparency, PenTip penTip, PenType penType, bool ignorePressure, bool antiAliased, bool fitToCurve) {
            Points = points;
            Color = color;
            Width = width;
            Height = height;
            Transparency = transparency;
            PenTip = penTip;
            PenType = penType;
            IgnorePressure = ignorePressure;
            AntiAliased = antiAliased;
            FitToCurve = fitToCurve;
        }

        public string ToJson() => JsonConvert.SerializeObject(this);
    }
}