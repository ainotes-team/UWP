using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace Helpers {
    public static class FilePicker {
        public static async Task<(IRandomAccessStream, string, string)> PickFile(IEnumerable<string> fileTypes, string commitButtonText = null) {
            var openPicker = new FileOpenPicker {
                ViewMode = PickerViewMode.Thumbnail,
            };
            
            foreach (var fileType in fileTypes) {
                openPicker.FileTypeFilter.Add(fileType);
            }

            if (commitButtonText != null) {
                openPicker.CommitButtonText = commitButtonText;
            }
            
            var storageItem = await openPicker.PickSingleFileAsync();
            return storageItem == null ? (null, null, null) : (await storageItem.OpenAsync(FileAccessMode.Read), storageItem.Name, storageItem.Path);
        }
        
        public static async Task<(Stream, string, string)> PickSaveFile(string suggestedFileName, string commitButtonText = null) {
            var openPicker = new FileSavePicker {
                SuggestedFileName = suggestedFileName,
            };
            
            if (commitButtonText != null) {
                openPicker.CommitButtonText = commitButtonText;
            }
            
            var storageItem = await openPicker.PickSaveFileAsync();
            return storageItem == null ? (null, null, null) : (null, storageItem.Name, storageItem.Path);
        }
    }
}