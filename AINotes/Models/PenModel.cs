using System;
using AINotes.Components.Tools;
using Helpers.Extensions;
using Newtonsoft.Json;
using Color = Windows.UI.Color;
using Size = Windows.Foundation.Size;

namespace AINotes.Models {
    public class PenModel {
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color Color { get; }

        public InkPenType PenType { get; }
        
        [JsonConverter(typeof(SizeJsonConverter))]
        public Size Size { get; }
        
        public PenModel(Color color, Size size, InkPenType penType) {
            Color = color;
            Size = size;
            PenType = penType;
        }
        
        [JsonConstructor]
        public PenModel(double[] color, int penType, double[] size) {
            Color = Color.FromArgb(255, (byte) color[0], (byte) color[1], (byte) color[2]);
            PenType = (InkPenType) penType;
            Size = new Size(size[0], size[1]);
        }

        public override string ToString() => $"PenModel: Color={Color}, PenType={PenType}, Size={Size}";
    }

    public class ColorJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(double[]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value is Color c) {
                var r = (double) c.R;
                var g = (double) c.G;
                var b = (double) c.B;
                var res = new [] {r, g, b};
                writer.WriteValue(res.Serialize());
            } else {
                throw new Exception("Object value is not a color");
            }
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.Value is double[] color) {
                return Color.FromArgb(255, (byte) color[0], (byte) color[1], (byte) color[2]);
            }

            return JsonConvert.DeserializeObject<double[]>((string) reader.Value ?? "[]");
        }
    }

    public class SizeJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(double[]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value is Size s) {
                var res = new [] { s.Width, s.Height };
                writer.WriteValue(res.Serialize());
            } else {
                throw new NotImplementedException();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.Value is double[] s) {
                return new Size(s[0], s[1]);
            }

            return JsonConvert.DeserializeObject<double[]>((string) reader.Value ?? "[]");
        }
    }
}