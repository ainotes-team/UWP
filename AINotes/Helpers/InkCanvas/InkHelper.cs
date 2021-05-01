using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;

namespace AINotes.Helpers.InkCanvas {
    public static class InkHelper {
        public static async Task<IReadOnlyList<InkStroke>> GetStrokesFromIsf(byte[] isfBytes) {
            var strokeContainer = new InkStrokeContainer();
            var stream = new MemoryStream(isfBytes).AsRandomAccessStream();
            await strokeContainer.LoadAsync(stream);
            stream.Dispose();
            return strokeContainer.GetStrokes();
        }
    }
}