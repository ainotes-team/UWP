using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace AINotes.Controls {
    public sealed partial class CustomDesmosView {
        public CustomDesmosView() {
            InitializeComponent();
        }

        public async Task<byte[]> GetImage() {
            var image64 = (await ContentWebView.InvokeScriptAsync("eval", new[] {
                "calculator.screenshot()"
            })).Replace("data:image/png;base64,", "");
            return Convert.FromBase64String(image64);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            ContentWebView.Width = finalSize.Width;
            ContentWebView.Height = finalSize.Height;
            return base.ArrangeOverride(finalSize);
        }
    }
}