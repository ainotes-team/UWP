using System.Collections.Generic;

namespace AINotes.Controls.Input {
    public partial class CustomFontSizeInput {
        public float Value => SelectedItem as float? ?? 10.5f;

        public CustomFontSizeInput() {
            var values = new List<float> {
                8f,
                9f,
                9.5f,
                10f,
                10.5f,
                11f,
                11.5f,
                12f,
                14f,
                16f,
                18f,
                20f,
                22f,
                24f,
                26f,
                28f,
                36f,
                48f,
                72f
            };

            foreach (var value in values) {
                Items?.Add(value);
            }

            SelectedItem = 10.5f;
        }
    }
}