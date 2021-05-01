using Helpers.Lists;

namespace AINotes.Models {
    public interface IFMSListableModel {
        string Name { get; set; }
        long LastChangedDate { get; set; }
        long CreationDate { get; set; }
        ObservableList<int> Labels { get; }
        string Owner { get; set; }
        string Status { get; set; }
        bool IsFavorite { get; set; }
    }
}