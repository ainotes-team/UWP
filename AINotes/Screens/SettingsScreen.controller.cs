namespace AINotes.Screens {
    public partial class SettingsScreen {
        private void LoadToolbar() {
            App.Page.OnBackPressed = () => App.Page.Load(App.FileManagerScreen);
        }
    }
}