using System.Reflection;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AINotesTests {
    sealed partial class App {
        protected override void OnInitializeRunner() {
            AddTestAssembly(GetType().GetTypeInfo().Assembly);
        }
        
        public App() {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            if (!(Window.Current.Content is Frame)) Window.Current.Content = new Frame();
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();
            Window.Current.Activate();
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(e.Arguments);
        }
    }
}