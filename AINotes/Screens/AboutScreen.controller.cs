namespace AINotes.Screens {
    public sealed partial class AboutScreen {
        private void LoadToolbar() {
            App.Page.OnBackPressed = () => App.Page.Load(App.FileManagerScreen);
            App.Page.PrimaryToolbarChildren.Clear();
        }
    }
}
