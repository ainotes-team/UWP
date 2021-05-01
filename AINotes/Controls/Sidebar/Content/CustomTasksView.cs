using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.Input;
using AINotes.Controls.Pages;
using AINotes.Helpers.Sidebar;
using Helpers;
using Helpers.Extensions;
using MaterialComponents;
using Newtonsoft.Json;

namespace AINotes.Controls.Sidebar.Content {
    public class TaskModel {
        public event Action<TaskModel> Changed;
        public event Action<TaskModel> Delete;
        public event Action<EditableCustomEntry, TaskModel> EntryLoaded;

        private string _text;

        [JsonProperty]
        public string Text {
            get => _text;
            set {
                _text = value;
                Logger.Log("Text =", value);
                Changed?.Invoke(this);
            }
        }

        private long _deadline;

        [JsonProperty]
        public long Deadline {
            get => _deadline;
            set {
                _deadline = value;
                Logger.Log("Deadline =", value);
                Changed?.Invoke(this);
            }
        }

        private bool _completed;

        [JsonProperty]
        public bool Completed {
            get => _completed;
            set {
                _completed = value;
                Logger.Log("Completed =", value);
                Changed?.Invoke(this);
            }
        }

        public TaskModel(string text, long deadline = -1, bool completed = false) {
            Text = text;
            Deadline = deadline;
            Completed = completed;
        }

        public void SetCompleted() => Completed = true;

        public void OnEntryLoaded(object sender, object _) {
            EntryLoaded?.Invoke((EditableCustomEntry) sender, this);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public void OnEditingStart(object sender, object _) {
            if (((FrameworkElement) sender).GetParent(2) is ContentPresenter c) c.Background = ColorCreator.FromHex("#E1E3E6").ToBrush();
        }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public void OnEditingStopping(object sender, object _) {
            if (((FrameworkElement) sender).GetParent(2) is ContentPresenter c) c.Background = Colors.Transparent.ToBrush();
        }

        public void OpenContextMenu(object sender, object _) {
            FrameworkElement anchor = (MDToolbarItem) sender;
            CustomDropdown.ShowDropdown(new[] {
                new CustomDropdownItem("Set Deadline"),
                new CustomDropdownItem("Delete", () => Delete?.Invoke(this)),
            }, anchor);
        }
    }

    public class CustomTasksView : Frame, ISidebarView {
        public const string DefaultValue = "[]";

        private IEnumerable<MDToolbarItem> _extraButtons;
        public IEnumerable<MDToolbarItem> ExtraButtons {
            get {
                if (_extraButtons != null) return _extraButtons;

                var addItem = new MDToolbarItem {
                    ImageSource = new BitmapImage(new Uri(Icon.Add)),
                };

                addItem.Released += (_, _) => CreateTask(new TaskModel(""));


                _extraButtons = new List<MDToolbarItem> {
                    addItem
                };

                return _extraButtons;
            }
        }

        private readonly ObservableCollection<TaskModel> _tasksSource = new ObservableCollection<TaskModel>();


        private bool _isFirstOverride = true;

        protected override Size ArrangeOverride(Size finalSize) {
            // ReSharper disable once InvertIf
            if (_isFirstOverride) {
                Content = new ListView {
                    ItemsSource = _tasksSource,
                    ItemTemplate = SidebarContentResources.TaskTemplate,
                    SelectionMode = ListViewSelectionMode.Single,
                    Transitions = new TransitionCollection(),
                    ItemContainerTransitions = new TransitionCollection(),
                    ItemContainerStyle = SidebarContentResources.SimpleListViewStyle,
                };

                _tasksSource.CollectionChanged += OnCollectionChanged;

                _tasksSource.AddRange(Load());
                _isFirstOverride = false;
            }

            return base.ArrangeOverride(finalSize);
        }

        private void OnCollectionChanged(object s, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (TaskModel itm in e.NewItems) {
                    itm.Changed += changedItem => {
                        if (changedItem.Completed) DeleteTask(changedItem);
                        Save();
                    };
                    itm.Delete += DeleteTask;
                }
            }

            Save();
        }


        private void FocusEntryOnce(EditableCustomEntry entry, TaskModel tm) {
            entry?.Focus(FocusState.Programmatic);
            tm.EntryLoaded -= FocusEntryOnce;
        }

        private void CreateTask(TaskModel tm) {
            _tasksSource.Add(tm);
            tm.EntryLoaded += FocusEntryOnce;
        }

        private void DeleteTask(TaskModel tm) => _tasksSource.Remove(tm);

        private IEnumerable<TaskModel> Load() => SidebarHelper.GetTasksJson().Deserialize<List<TaskModel>>();
        private void Save() => SidebarHelper.SetTasksJson(_tasksSource.ToList().Serialize());
    }
}