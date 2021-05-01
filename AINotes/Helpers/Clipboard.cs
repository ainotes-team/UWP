using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using AINotes.Components.Tools;
using Helpers;
using Helpers.Extensions;

namespace AINotes.Helpers {
    public enum ClipboardContentType {
        Rtf,
        Text,
        Image,
        ImageFile,
        Ink,
        Other
    }

    public struct ClipboardContent {
        public readonly ClipboardContentType ContentType;
        public readonly object Content;

        public ClipboardContent(ClipboardContentType contentType, object content) {
            ContentType = contentType;
            Content = content;
        }
    }
    
    public static class Clipboard {
        public static async Task<ClipboardContent> GetContent() {
            ClipboardContentType resultType;
            object resultContent;

            var clipboardContent = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            Logger.Log("AvailableFormats", clipboardContent.AvailableFormats.ToFString());

            if (clipboardContent.Contains(StandardDataFormats.Rtf)) {
                resultType = ClipboardContentType.Rtf;
                resultContent = await clipboardContent.GetRtfAsync();
            } else if (clipboardContent.Contains(StandardDataFormats.Text)) {
                resultType = ClipboardContentType.Text;
                resultContent = await clipboardContent.GetTextAsync();
            } else if (clipboardContent.AvailableFormats.Select(itm => itm.ToString()).Contains("Ink Serialized Format")) {
                resultType = ClipboardContentType.Ink;
                using (var bmpStream = await (await clipboardContent.GetBitmapAsync()).OpenReadAsync()) {
                    resultContent = bmpStream.AsStream().ReadAllBytes();
                }
            } else if (clipboardContent.Contains(StandardDataFormats.Bitmap)) {
                resultType = ClipboardContentType.Image;
                using (var bmpStream = await (await clipboardContent.GetBitmapAsync()).OpenReadAsync()) {
                    resultContent = bmpStream.AsStream().ReadAllBytes();
                }
            } else if (clipboardContent.Contains(StandardDataFormats.StorageItems)) {
                resultType = ClipboardContentType.ImageFile;
                var resultStorageItems = await clipboardContent.GetStorageItemsAsync();
                resultContent = new List<StorageFile>();
                foreach (var f in resultStorageItems.Where(sI => sI is StorageFile sF && sF.Name.ToLower().EndsWithAny(ImageComponentTool.SupportedImageExtensions)).Cast<StorageFile>()) {
                    ((List<StorageFile>) resultContent).Add(f);
                }
            } else {
                resultType = ClipboardContentType.Other;
                resultContent = null;
            }

            return new ClipboardContent(resultType, resultContent);
        }


        public static void SetTextAsync(string txt) {
            var pkg = new DataPackage();  
            pkg.SetText(txt);  
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pkg);
        }
    }
}