using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using AINotes.Controls.Sidebar;
using AINotes.Controls.Sidebar.Content;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using MaterialComponents;
using Newtonsoft.Json;

namespace AINotes.Helpers.PreferenceHelpers {
    public class SidebarItemModel {
        [JsonProperty("content_type")]
        public Type ContentType { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("icon")]
        public string Icon { get; set; }
        
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }
        
        [JsonProperty("edt_only")]
        public bool IsEditorScreenOnly { get; set; }

        [JsonIgnore]
        public Action<SidebarItemModel, DependencyObject> MoveUpCallback { get; set; }
        
        [JsonIgnore]
        public Action<SidebarItemModel, DependencyObject> MoveDownCallback { get; set; }
        
        [JsonIgnore]
        public Action<SidebarItemModel, DependencyObject> DisableEnableCallback { get; set; }

        public SidebarItemModel(Type contentType, string title, string icon, bool isEnabled=true, bool editorScreenOnly=false) {
            ContentType = contentType;
            Title = title;
            Icon = icon;
            IsEnabled = isEnabled;
            IsEditorScreenOnly = editorScreenOnly;
        }
        
        public void MoveUp(object s, object _) => MoveUpCallback?.Invoke(this, s as DependencyObject);

        public void MoveDown(object s, object _) => MoveDownCallback?.Invoke(this, s as DependencyObject);

        public void DisableEnable(object s, object _) => DisableEnableCallback?.Invoke(this, s as DependencyObject);

        public void AddToSidebar(CustomSidebar sidebar) {
            var thisModel = this;
            
            var contentElement = (UIElement) Activator.CreateInstance(ContentType);

            IEnumerable<MDToolbarItem> extraButtons = null;
            if (contentElement is ISidebarView isv) {
                extraButtons = isv.ExtraButtons;
            }

            sidebar.AddItem(thisModel.Icon, _ => {
                sidebar.SetContent(contentElement);
                sidebar.SetTitle(thisModel.Title);

                if (extraButtons != null) {
                    sidebar.ShowExtraButtons(extraButtons);
                } else {
                    sidebar.ClearTitlebar();
                }
            });
        }
    }
    
    public class SidebarItemsPreference : Preference {
        private readonly List<SidebarItemModel> _defaultValue;
        private ListView _view;
        
        public SidebarItemsPreference(string displayName, List<SidebarItemModel> defaultValue=null, Action onChanged=null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _defaultValue = defaultValue;
            
            // add new from default
            // UserPreferenceHelper.Set(PropertyName, null);
            var value = GetValue();
            if (_defaultValue != null) {
                foreach (var newItm in _defaultValue.Where(itm => value.All(i => i.Title != itm.Title))) {
                    value.Add(newItm);
                }
            }
            
            UserPreferenceHelper.Set(PropertyName, value.Serialize());
        }
        
        private List<SidebarItemModel> GetValue() => UserPreferenceHelper.Get(PropertyName, _defaultValue.Serialize()).Deserialize<List<SidebarItemModel>>() ?? _defaultValue;

        private ObservableCollection<SidebarItemModel> _listViewItems;
        public override UIElement GetView() {
            if (_view != null) return _view;

            _listViewItems = new ObservableCollection<SidebarItemModel>();
            _view = new ListView {
                ItemsSource = _listViewItems,
                SelectionMode = ListViewSelectionMode.None,
                ItemTemplate = SidebarContentResources.SidebarItemTemplate,
                Transitions = new TransitionCollection(),
                ItemContainerTransitions = new TransitionCollection(),
                ItemContainerStyle = SidebarContentResources.SimpleListViewStyle,
            };

            _listViewItems.CollectionChanged += OnCollectionChanged;
            
            _listViewItems.AddRange(GetValue());
            return _view;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
            if (args.NewItems == null) return;
            foreach (SidebarItemModel newItem in args.NewItems) {
                newItem.MoveUpCallback = OnItemMoveUp;
                newItem.MoveDownCallback = OnItemMoveDown;
                newItem.DisableEnableCallback = OnItemEnableDisable;
            }
        }

        private void OnItemEnableDisable(SidebarItemModel itm, DependencyObject itmSender) {
            Logger.Log("-> OnItemEnableDisable");
            itm.IsEnabled = !itm.IsEnabled;
            
            var oldItem = _listViewItems.FirstOrDefault(model => model.Title == itm.Title);
            if (oldItem == null) {
                Logger.Log("<- OnItemEnableDisable: oldItem == null");
                return;
            }

            var oldIdx = _listViewItems.IndexOf(oldItem);
            _listViewItems.Remove(oldItem);
            _listViewItems.Insert(oldIdx, itm);
            Save();
            _view.ItemsSource = null;
            _view.ItemsSource = _listViewItems;
        }

        private void OnItemMoveUp(SidebarItemModel itm, DependencyObject itmSender) {
            Logger.Log("-> OnItemMoveUp");
            var oldIdx = _listViewItems.IndexOf(itm);
            if (oldIdx < 1) {
                Logger.Log("<- OnItemMoveUp: oldIdx =", oldIdx);
                return;
            }
            _listViewItems.Move(oldIdx, oldIdx - 1);
            Save();
        }

        private void OnItemMoveDown(SidebarItemModel itm, DependencyObject itmSender) {
            Logger.Log("-> OnItemMoveDown");
            var oldIdx = _listViewItems.IndexOf(itm);
            if (oldIdx + 1 == _listViewItems.Count) {
                Logger.Log("<- OnItemMoveUp: oldIdx =", oldIdx);
                return;
            }
            _listViewItems.Move(oldIdx, oldIdx + 1);
            Save();
        }

        private void Save() {
            UserPreferenceHelper.Set(PropertyName, _listViewItems.Serialize());
        }

        public static implicit operator List<SidebarItemModel>(SidebarItemsPreference x) => x.GetValue();
    }
}