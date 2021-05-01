using Newtonsoft.Json;

namespace AINotes.Models {
    public class ExtensionModel {
        [JsonIgnore]
        public object Extension { get; }
        
        public string DisplayName { get; }
        public string Description { get; }
        public string UniqueId { get; }
        public byte[] LogoBytes { get; }
        
        // public ImageSource LogoImageSource => ImageSourceHelper.FromStream(() => new MemoryStream(LogoBytes));

        public ExtensionModel(object extension, string displayName, string description, string uniqueId, byte[] logoBytes) {
            Extension = extension;
            DisplayName = displayName;
            Description = description;
            UniqueId = uniqueId;
            LogoBytes = logoBytes;
        }

        public override string ToString() => $"Extension: {DisplayName} [{UniqueId}] - {Description}";
    }
}