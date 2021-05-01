using Windows.UI;
using Helpers.Extensions;
using SQLite;

namespace AINotes.Models {
    public class LabelModel {
        [PrimaryKey, AutoIncrement, Unique]
        public int LabelId { get; set; }
        
        public string Name { get; set; }
        
        public string HexColor { get; set; }

        [Ignore]
        public Color Color {
            get => ColorCreator.FromHex(HexColor);
            set => HexColor = value.ToHex();
        }

        public bool Archived { get; set; }

        public LabelModel() { }

        public LabelModel(string name, Color color) {
            Name = name;
            Color = color;
        }
    }
}