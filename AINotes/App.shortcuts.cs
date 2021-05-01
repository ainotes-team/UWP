using System;
using System.Linq;
using System.Timers;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.Pages;
using AINotes.Controls.Popups;
using Helpers.Essentials;
using Helpers.Extensions;
using AINotes.Models;
using AINotes.Helpers;
using Helpers;
using System.Collections.Generic;
#if DEBUG
using AINotes.Helpers.Integrations;
using MaterialComponents;
#endif

namespace AINotes {
    sealed partial class App {
        private void RegisterShortcuts() {
            // global shortcuts
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.FeedbackShortcut, "global_feedbackScreen", () => {
                if (Page.Content == FeedbackScreen) return;
                CustomDropdown.CloseDropdown();
                Page.Load(FeedbackScreen);
            }));
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.FullscreenShortcut, "global_fullscreen", Fullscreen.Toggle));
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CancelShortcut, "global_closePopups", () => {
                MDPopup.CloseCurrentPopup();
                CustomDropdown.CloseDropdown();
            }));
            Shortcuts.AddShortcut(new ShortcutModel(() => new List<string> { "Menu", "Left" }, "global_back", Page.GoBack));
            
            // test shortcuts
            #if DEBUG
            Shortcuts.AddShortcut(new ShortcutModel(() => new List<string> { "Control", "F6" }, "global_test_f6", () => {
                StackPanel content;
                Action okCallback;
                if (!MoodleHelper.IsLoggedIn) {
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
                
                    content = new StackPanel {
                        Children = {
                            usernameEntry,
                            passwordEntry,
                            errorLabel,
                        }
                    };
                    okCallback = async () => {
                        var username = usernameEntry.Text;
                        var password = passwordEntry.Text;

                        var success = await MoodleHelper.LoginAsync(username, password);
                        if (success) {
                            PopupNavigation.CloseCurrentPopup();

                            var resp = await MoodleHelper.SendRequest("core_course_get_categories");
                            Logger.Log(await resp.Content.ReadAsStringAsync());
                        } else {
                            errorLabel.Visibility = Visibility.Visible;
                        }
                    };
                } else {
                    content = new StackPanel {
                        Children = {
                            new MDLabel("Already logged in"),
                        }
                    };

                    okCallback = async () => {
                        var user = await MoodleHelper.GetUserModel();
                        var courses = await MoodleHelper.GetUserCourses(user.UserId);
                        var assignments = await MoodleHelper.GetUserAssignments();
                        
                        foreach (var course in courses) {
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
                                Logger.Log(" A>", assignment.Id, assignment.Name, assignment.Intro, assignment.DueDate, assignment.SubmissionDrafts);
                                foreach (var iA in assignment.IntroAttachments) {
                                    Logger.Log(" A> A>", iA.FileName, iA.FileUrl);
                                }
                                foreach (var iF in assignment.IntroFiles) {
                                    Logger.Log(" A> F>", iF.FileName, iF.FileUrl);
                                }
                            }
                        }
                    };
                }

                new MDContentPopup("Moodle Login", content, okCallback, closeOnOk: false, cancelable: true, okText: "Login", closeWhenBackgroundIsClicked: true).Show();
            }));
            #endif

            // toast sequence
            Shortcuts.AddSequence(new ShortcutModel(() => "Toast".ToArray().Select(itm => itm.ToString()).ToList(), "toast_sequence", () => {
                Logger.Log("TOAST!");
                MainThread.BeginInvokeOnMainThread(() => {
                    Image toastImage;
                    Page.AbsoluteOverlay.Children.Add(toastImage = new Image {
                        Source = new BitmapImage {
                            UriSource = new Uri("https://cdn.discordapp.com/emojis/651864261248679946.gif"),
                            AutoPlay = true,
                        },
                        Width = 200,
                        Height = 200,
                        MaxWidth = 200,
                        MaxHeight = 200,
                    }, new Point(Window.Current.CoreWindow.Bounds.Width / 2 - 100, Window.Current.CoreWindow.Bounds.Height / 2 - 100));

                    var t = new Timer {
                        AutoReset = false,
                        Enabled = true,
                        Interval = 1000,
                    };

                    void OnTimerElapsed(object sender, ElapsedEventArgs args) {
                        MainThread.BeginInvokeOnMainThread(() => {
                            Page.AbsoluteOverlay.Children.Remove(toastImage);
                        });
                    }

                    t.Elapsed += OnTimerElapsed;
                    t.Start();
                });
            }));
        }
    }
}