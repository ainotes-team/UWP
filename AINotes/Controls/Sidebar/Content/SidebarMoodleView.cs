using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.Pages;
using AINotes.Controls.Popups;
using AINotes.Helpers.Integrations;
using Helpers;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class SidebarMoodleView : Frame, ISidebarView {
        private IEnumerable<MDToolbarItem> _extraButtons;
        public IEnumerable<MDToolbarItem> ExtraButtons {
            get {
                if (_extraButtons != null) return _extraButtons;
                var menuTBI = new MDToolbarItem {
                    ImageSource = new BitmapImage(new Uri(Icon.MenuVertical)),
                };
                menuTBI.Released += OnMenuTBIReleased;
                
                var reloadTBI = new MDToolbarItem {
                    ImageSource = new BitmapImage(new Uri(Icon.Reset)),
                };
                reloadTBI.Released += OnReloadTBIReleased;
                
                _extraButtons = new List<MDToolbarItem> { menuTBI, reloadTBI };
                return _extraButtons;
            }
        }

        private ScrollViewer _resultScroll;
        
        private readonly Dictionary<int, ListView> _resultListViews = new Dictionary<int, ListView>();
        private readonly Dictionary<int, Frame> _resultHeaders = new Dictionary<int, Frame>();
        private readonly Dictionary<int, ObservableCollection<MoodleAssignmentsAssignmentModel>> _resultSources = new Dictionary<int, ObservableCollection<MoodleAssignmentsAssignmentModel>>();

        private bool _isFirstOverride = true;
        protected override Size ArrangeOverride(Size finalSize) {
            // ReSharper disable once InvertIf
            if (_isFirstOverride) {
                if (!MoodleHelper.IsLoggedIn) {
                    LoadLoginLayout();
                } else {
                    LoadContentLayout();
                }
                _isFirstOverride = false;
            }

            return base.ArrangeOverride(finalSize);
        }
        
        private void LoadContentLayout() {
            Content = _resultScroll ??= new ScrollViewer {
                Content = new StackPanel(),
            };
            
            Reload();
        }

        private void OnMenuTBIReleased(object sender, EventArgs e) {
            OpenMenu(sender as MDToolbarItem);
        }

        private void OnReloadTBIReleased(object sender, EventArgs e) {
            Reload();
        }

        private async void Reload() {
            Logger.Log("Reload");
            
            var userModel = await MoodleHelper.GetUserModel();
            var courseModels = await MoodleHelper.GetUserCourses(userModel.UserId);
            var assignments = await MoodleHelper.GetUserAssignments();

            if (!(_resultScroll.Content is StackPanel resultStack)) return;
            try {
                foreach (var course in courseModels) {
                    Logger.Log(course.Id, course.FullName, course.Progress, course.Completed, course.EnrolledUserCount);
                
                    var categories = await MoodleHelper.GetCourseCategories(course.Id);
                    var courseAssignments = assignments.Courses.FirstOrDefault(c => c.Id == course.Id)?.Assignments ?? new List<MoodleAssignmentsAssignmentModel>();
                    
                    foreach (var cat in categories) {
                        Logger.Log(" C>", cat.Id, cat.Name);
                        foreach (var mod in cat.Modules) {
                            Logger.Log(" C> M>", mod.Id, mod.Name, mod.CompletionData?.State, mod.ModuleName);
                            foreach (var c in mod.Contents ?? new List<MoodleContentModel>()) {
                                Logger.Log(" C> M> C>", c.Type, c.FileName, c.FileUrl);
                            }
                        }
                    }

                    foreach (var assignment in courseAssignments) {
                        Logger.Log(" A>", assignment.Id, assignment.CategoryId, assignment.Name, assignment.Intro, assignment.DueDate, assignment.SubmissionDrafts);
                        foreach (var iA in assignment.IntroAttachments) {
                            Logger.Log(" A> A>", iA.FileName, iA.FileUrl);
                        }
                        foreach (var iF in assignment.IntroFiles) {
                            Logger.Log(" A> F>", iF.FileName, iF.FileUrl);
                        }
                    }
                    
                    Logger.Log(course.DisplayName, "=>", categories.ToFString());
                    if (_resultSources.ContainsKey(course.Id)) {
                        foreach (var assignment in courseAssignments.Where(assignment => _resultSources[course.Id].All(itm => itm.Id != assignment.Id))) {
                            _resultSources[course.Id].Add(assignment);
                        }
                    } else {
                        _resultSources.Add(course.Id, new ObservableCollection<MoodleAssignmentsAssignmentModel>());
                        
                        var lv = new ListView {
                            ItemsSource = _resultSources[course.Id],
                            SelectionMode = ListViewSelectionMode.None,
                            Transitions = new TransitionCollection(),
                            ItemContainerTransitions = new TransitionCollection(),
                            IsItemClickEnabled = true,
                            ItemContainerStyle = SidebarContentResources.SimpleListViewStyleNotRounded,
                            ItemTemplate = SidebarContentResources.MoodleDataTemplate,
                            Margin = new Thickness(0, 0, 0, 4),
                            Visibility = Visibility.Collapsed,
                        };

                        var expandTBI = new MDToolbarItem {
                            ImageSource = new BitmapImage(new Uri(Icon.ExpandArrow)),
                        };
                        expandTBI.Released += (_, _) => {
                            if (lv.Visibility == Visibility.Visible) {
                                lv.Visibility = Visibility.Collapsed;
                                expandTBI.ImageSource = new BitmapImage(new Uri(Icon.ExpandArrow));
                            } else {
                                lv.Visibility = Visibility.Visible;
                                expandTBI.ImageSource = new BitmapImage(new Uri(Icon.CollapseArrow));
                            }
                        };
                        var header = new Frame {
                            VerticalContentAlignment = VerticalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            BorderBrush = Configuration.Theme.CardBorder,
                            BorderThickness = new Thickness(0, 1, 0, 1),
                            Content = new StackPanel {
                                Orientation = Orientation.Horizontal,
                                Children = {
                                    expandTBI,
                                    new MDLabel {
                                        Text = course.DisplayName,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        TextAlignment = TextAlignment.Center,
                                        Margin = new Thickness(0, 0, 2, 0),
                                    },
                                    new MDLabel {
                                        Text = $"({Math.Round(course.Progress ?? 0)}%) (ID: {course.Id})",
                                        VerticalAlignment = VerticalAlignment.Center,
                                        TextAlignment = TextAlignment.Center,
                                        Foreground = Colors.DarkGray.ToBrush(),
                                        Margin = new Thickness(2, 0, 2, 0),
                                    },
                                }
                            }
                        };
                        
                        resultStack.Children.Add(header);
                        resultStack.Children.Add(lv);
                        _resultListViews.Add(course.Id, lv);
                        _resultHeaders.Add(course.Id, header);

                        foreach (var assignment in assignments.Courses.FirstOrDefault(c => c.Id == course.Id)?.Assignments ?? new List<MoodleAssignmentsAssignmentModel>()) {
                            assignment.Intro = assignment.Intro.Replace(new[] {"<p>"}, "").Replace(new[] {"<br>", "</p>"}, "\n");
                            while (assignment.Intro.Contains("\n\n")) {
                                assignment.Intro = assignment.Intro.Replace("\n\n", "\n");
                            }
                            if (assignment.Intro.EndsWith("\n")) {
                                assignment.Intro = assignment.Intro.Substring(0, assignment.Intro.LastIndexOf("\n", StringComparison.Ordinal));
                            }
                            _resultSources[course.Id].Add(assignment);
                        }
                    }
                }

                foreach (var (sKey, _) in _resultSources.ToList().Where(sKey => courseModels.All(itm => itm.Id != sKey.Key))) {
                    resultStack.Children.Remove(_resultListViews[sKey]);
                    resultStack.Children.Remove(_resultHeaders[sKey]);
                    _resultSources.Remove(sKey);
                    _resultListViews.Remove(sKey);
                    _resultHeaders.Remove(sKey);
                }
            } catch (Exception ex) {
                Logger.Log("[SidebarMoodleView]", "Reload: Exception:", ex.ToString());
            }
        }

        private void LoadLoginLayout() {
            var loginButton = new MDButton {
                Content = "Einloggen",
            };

            Content = new StackPanel {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = {
                    new MDLabel("Bitte melde dich an um auf Moodle zuzugreifen.", 15),
                    new MDLabel($"ggf. musst du die Moodle URL in den Einstellungen anpassen\n(aktuelle URL: {(string) Preferences.MoodleUrl})", 10),
                    loginButton,
                }
            };

            loginButton.Click += OnLoginButtonClick;
        }

        private void OnLoginButtonClick(object sender, RoutedEventArgs args) {
            Login();
        }

        private void OpenMenu(MDToolbarItem tbi) {
            List<CustomDropdownViewTemplate> dropdownItems;
            if (MoodleHelper.IsLoggedIn) {
                dropdownItems = new List<CustomDropdownViewTemplate> {
                    new CustomDropdownItem("Reload", Reload),
                    new CustomDropdownItem("Logout", Logout),
                };
            } else {
                dropdownItems = new List<CustomDropdownViewTemplate> {
                    new CustomDropdownItem("Login", Login),
                };
            }
            CustomDropdown.ShowDropdown(dropdownItems, tbi);
        }
        
        private void Login() {
            var usernameEntry = new MDEntry {
                Placeholder = "Username"
            };

            var passwordEntry = new MDEntry {
                Placeholder = "Password"
            };

            var errorLabel = new TextBlock {
                Visibility = Visibility.Collapsed,
                Foreground = MDEntry.ErrorBrush,
                Text = "Login failed",
            };

            new MDContentPopup("Moodle Login", new StackPanel {
                Children = {
                    usernameEntry,
                    passwordEntry,
                    errorLabel,
                }
            }, async () => {
                var username = usernameEntry.Text;
                var password = passwordEntry.Text;

                var success = await MoodleHelper.LoginAsync(username, password);
                if (success) {
                    PopupNavigation.CloseCurrentPopup();
                    LoadContentLayout();
                } else {
                    errorLabel.Visibility = Visibility.Visible;
                }
            }, closeOnOk: false, cancelable: true, okText: "Login", closeWhenBackgroundIsClicked: true).Show();
        }

        private void Logout() {
            MoodleHelper.Logout();
            LoadLoginLayout();
        }
    }
}