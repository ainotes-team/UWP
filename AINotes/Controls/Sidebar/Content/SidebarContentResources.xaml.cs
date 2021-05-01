using Windows.UI.Xaml;

namespace AINotes.Controls.Sidebar.Content {
    public sealed partial class SidebarContentResources {
        private static SidebarContentResources _self;
        
        public static Style SimpleListViewStyle => (_self ??= new SidebarContentResources()).InternalSimpleListViewStyle;
        public static Style SimpleListViewStyleNotRounded => (_self ??= new SidebarContentResources()).InternalSimpleListViewStyleNotRounded;
        
        public static DataTemplate TaskTemplate => (_self ??= new SidebarContentResources()).TaskModelTemplate;
        public static DataTemplate RepresentationItemModelTemplate => (_self ??= new SidebarContentResources()).InternalRepresentationItemModelTemplate;
        public static DataTemplate SidebarItemTemplate => (_self ??= new SidebarContentResources()).InternalSidebarItemTemplate;
        public static DataTemplate MoodleDataTemplate => (_self ??= new SidebarContentResources()).InternalMoodleDataTemplate;

        public SidebarContentResources() {
            InitializeComponent();
        }
    }
}