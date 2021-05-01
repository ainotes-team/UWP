using System.Collections.Generic;
using Windows.Foundation;
using AINotes.Models.Enums;
using AINotes.Screens;
using MaterialComponents;

namespace AINotes.Controls.Input {
    public class CustomLineModePicker : MDPickerGrid {
        private static readonly List<MDPickerGridEntry> ModeOptions = new List<MDPickerGridEntry> {
            new MDPickerGridEntry(DocumentLineMode.LinesSmall, Icon.Lines0, OnSelected),
            new MDPickerGridEntry(DocumentLineMode.LinesMedium, Icon.Lines1, OnSelected),
            new MDPickerGridEntry(DocumentLineMode.LinesLarge, Icon.Lines2, OnSelected),
            new MDPickerGridEntry(DocumentLineMode.GridSmall, Icon.Grid0, OnSelected),
            new MDPickerGridEntry(DocumentLineMode.GridMedium, Icon.Grid1, OnSelected),
            new MDPickerGridEntry(DocumentLineMode.GridLarge, Icon.Grid2, OnSelected),
            new MDPickerGridEntry(DocumentLineMode.None, Icon.Close, OnSelected),
        };

        private static void OnSelected(object lineModeObject) {
            App.EditorScreen.BackgroundLineMode = (DocumentLineMode) lineModeObject;
        }
        
        public CustomLineModePicker() : base(1, 3, ModeOptions) { }

        protected override Size MeasureOverride(Size availableSize) {
            if (App.Page.Content.GetType() == typeof(EditorScreen)) {
                SetSelectedValue(App.EditorScreen.BackgroundLineMode);
            }
            
            return base.MeasureOverride(availableSize);
        }
    }
}