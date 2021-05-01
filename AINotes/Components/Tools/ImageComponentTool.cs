using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Components.Implementations;
using AINotes.Controls;
using AINotes.Controls.Pages;
using AINotes.Controls.Popups;
using AINotes.Helpers.Imaging;
using AINotes.Helpers.UserActions;
using AINotes.Models;
using Helpers;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Components.Tools {
    public class ImageComponentTool : MDToolbarItem, ITool {
        public bool RequiresDrawingLayer => false;
        
        public static readonly string[] SupportedImageExtensions = {".jpeg", ".jpg", ".png", ".gif"};
        public static readonly string[] SupportedPdfExtensions = {".pdf"};

        public ImageComponentTool() {
            ImageSource = ImageSourceHelper.FromName(Icon.ImageDocument);
            Selectable = false;
            Deselectable = false;
        }

        public void Select() {
            App.Page.SecondaryToolbarChildren.Clear();
            
            CustomDropdown.ShowDropdown(new List<CustomDropdownViewTemplate> {
                new CustomDropdownItem("Photo from Camera", OpenCamera, Icon.Camera),
                new CustomDropdownItem("From Device", SearchImage, Icon.Search),
                new CustomDropdownItem("Graph", OpenDesmos, Icon.Calculator)
            }, this);
        }
        
        public void Deselect() {
            App.Page.SecondaryToolbarChildren.Clear();
            IsSelected = false;
        }

        public void SubscribeToPressedEvents(EventHandler<EventArgs> handler) => Pressed += handler;
        
        public static async void InsertPdfDocument(PdfDocument document) {
            var documentComponents = new List<ImageComponent>();
            if (document != null && document.PageCount > 0) {
                var statusLabel = new MDLabel("Starting Import...");
                var cancelled = false;
                new MDContentPopup("Loading PDF...", statusLabel, submitable: false, cancelable: true, closeWhenBackgroundIsClicked: false, cancelCallback: () => {
                    cancelled = true;
                }).Show();
                for (var pageIndex = 0; pageIndex < document.PageCount; pageIndex++) {
                    if (cancelled) break;
                    Logger.Log("Getting Page", pageIndex);
                    statusLabel.Text = $"Page {pageIndex+1} / {document.PageCount}";
                    
                    var pdfPage = document.GetPage((uint) pageIndex);
                    if (pdfPage == null) continue;

                    var (imageComponent, imagePath) = await ImageComponent.New(App.EditorScreen.FileId);
                    
                    var randomStream = await FileRandomAccessStream.OpenAsync(LocalFileHelper.ToAbsolutePath(imagePath), FileAccessMode.ReadWrite);
                    
                    Logger.Log("Create Render Options");
                    
                    var pdfPageRenderOptions = new PdfPageRenderOptions { DestinationWidth = (uint) App.Page.Toolbar.ActualWidth - 100 };
                    imageComponent.OriginalImageSize = pdfPage.Size;
                    
                    Logger.Log("Render");
                    await pdfPage.RenderToStreamAsync(randomStream, pdfPageRenderOptions);
                    
                    Logger.Log("Flush");
                    await randomStream.FlushAsync();
                    Logger.Log("created", imageComponent.Path);
                    
                    randomStream.Dispose();
                    pdfPage.Dispose();

                    imageComponent.SetContent(imagePath);
                    
                    documentComponents.Add(imageComponent);
                }
                MDPopup.CloseCurrentPopup();
            }
        
            // place images
            AddDocumentComponents(documentComponents);
        }

        private static async void SearchImage() {
            CustomDropdown.CloseDropdown();
            
            var (selectedFileStream, fileName, _) = await FilePicker.PickFile(SupportedImageExtensions.Concat(SupportedPdfExtensions));
            
            if (fileName == null) return;
            if (selectedFileStream == null) return;
            
            fileName = fileName.ToLower();
            
            if (fileName.EndsWithAny(SupportedImageExtensions)) {
                var (imageComponent, imagePath) = await ImageComponent.New(App.EditorScreen.FileId);
                var randomStream = await FileRandomAccessStream.OpenAsync(LocalFileHelper.ToAbsolutePath(imagePath), FileAccessMode.ReadWrite);

                await randomStream.WriteAsync(selectedFileStream.AsStreamForRead().ReadAllBytes().AsBuffer());
                
                await randomStream.FlushAsync();
                randomStream.Dispose();
                
                imageComponent.ShouldWriteToFile = true;
                imageComponent.SetContent(imagePath);
                
                AddDocumentComponents(imageComponent);
            } else if (fileName.EndsWithAny(SupportedPdfExtensions)) {
                try {
                    var pdfDocument = await PdfDocument.LoadFromStreamAsync(selectedFileStream);
                    InsertPdfDocument(pdfDocument);
                    selectedFileStream.Dispose();
                } catch (Exception ex) {
                    Logger.Log("ex:", ex.ToString());
                    var passwordBox = new MDEntry {
                        Placeholder = "Password"
                    };
                    new MDContentPopup("PDF File is password protected", new StackPanel {
                        Children = {
                            passwordBox
                        }
                    }, async () => {
                        try {
                            var pdfDocument = await PdfDocument.LoadFromStreamAsync(selectedFileStream, passwordBox.Text);
                            passwordBox.Error = false;
                            PopupNavigation.CloseCurrentPopup();
                            InsertPdfDocument(pdfDocument);
                            selectedFileStream.Dispose();
                        } catch (Exception lEx) {
                            Logger.Log("lEx:", lEx.ToString());
                            passwordBox.Error = true;
                        }
                    }, closeOnOk: false, cancelable: true, closeWhenBackgroundIsClicked: false).Show();
                }
            } else {
                Logger.Log("Wrong File Type selected", logLevel: LogLevel.Error);
            }
        }

        private void OpenDesmos() {
            ContentDialog popup = null;
            var cdv = new CustomDesmosView {
                Width = 800,
                Height = 800,
            };

            var cancelButton = new MDButton {
                ButtonStyle = MDButtonStyle.Secondary,
                Text = "Cancel",
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = () => popup?.Hide()
            };
            var submitButton = new MDButton {
                Text = "Ok",
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = async () => {
                    var bytes = await cdv.GetImage();

                    var (imageComponent, imagePath) = await ImageComponent.New(App.EditorScreen.FileId);
                    
                    await LocalFileHelper.WriteFileAsync(imagePath, bytes);
                    
                    popup?.Hide();
                    
                    AddDocumentComponents(imageComponent);
                    imageComponent.SetContent(imagePath);
                }
            };

            popup = new MDPopup {
                CloseWhenBackgroundIsClicked = false,
                Content = new Frame {
                    Background = Configuration.Theme.Background,
                    Margin = new Thickness(15),
                    Content = new Grid {
                        RowDefinitions = {
                            new RowDefinition {Height = new GridLength(1, GridUnitType.Auto)},
                            new RowDefinition {Height = new GridLength(1, GridUnitType.Star)},
                            new RowDefinition {Height = new GridLength(1, GridUnitType.Auto)},
                        },
                        ColumnDefinitions = {
                            new ColumnDefinition {Width = new GridLength(1, GridUnitType.Auto)},
                        },
                        ColumnSpacing = 10,
                        Children = {
                            {
                                new MDLabel {
                                    Text = "Desmos",
                                    FontSize = 24,
                                    Margin = new Thickness(0, 0, 0, 10)
                                },
                                0, 0
                            },

                            { new Frame {BorderBrush = Colors.Transparent.ToBrush(), BorderThickness = new Thickness(2), Content = cdv}, 1, 0 }, {
                                new StackPanel {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    Children = {
                                        cancelButton,
                                        new Frame {Width = 3},
                                        submitButton,
                                    },
                                },
                                2, 0
                            },
                        }
                    }
                }
            };
            PopupNavigation.OpenPopup(popup);
        }

        private static void OpenCamera() {
            CustomDropdown.CloseDropdown();
            App.Page.Load(App.CameraScreen);
        }

        public static Size GetComponentBounds(Size originalSize) {
            var windowWidth = 1000;

            var imageAspectRatio = originalSize.Width / originalSize.Height;

            return new Size(windowWidth, windowWidth / (imageAspectRatio * 1d));
        }
        
        public static void AddDocumentComponents(ImageComponent imageComponent, Point offset=default) => AddDocumentComponents(new[] {imageComponent}, offset);
        public static void AddDocumentComponents(IEnumerable<ImageComponent> imageComponentsEnumerable, Point offset=default) {
            Logger.Log("[DocumentComponentToolbarItem]", "AddDocumentComponents");
            var imageComponents = imageComponentsEnumerable.ToArray();

            UserActionManager.OnComponentsAdded(imageComponents);

            foreach (var imageComponent in imageComponents) imageComponent.CreateUserAction = false;
            
            double offsetX, offsetY;
            if (offset == default) {
                // minComponentX => offsetX
                // maxComponentY => offsetY
                offsetX = double.MaxValue;
                offsetY = 0d;
            
                foreach (var p in App.EditorScreen.GetDocumentComponents()) {
                    if (p.GetY() + p.ActualHeight > offsetY) {
                        offsetY = p.GetY() + p.ActualHeight;
                    }
                    if (p.GetX() < offsetX) {
                        offsetX = p.GetX();
                    }
                }
                
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var boundingRect in App.EditorScreen.GetInkStrokes().Select(s => s.BoundingRect)) {
                    if (boundingRect.Y + boundingRect.Height > offsetY) {
                        offsetY = boundingRect.Y + boundingRect.Height;
                    }
                    if (boundingRect.X < offsetX) {
                        offsetX = boundingRect.X;
                    }
                }

                if (offsetX < 20 || offsetX == double.MaxValue) {
                    offsetX = 20;
                }

                offsetY += 20;
            } else {
                (offsetX, offsetY) = offset;
            }
            
            var currentY = 0d;
            
            var waiters = new ObservableCollection<object>();
            foreach (var component in imageComponents) {
                if (component.Succeeded) {
                    var p = new Point(offsetX, offsetY + currentY);
                    var b = GetComponentBounds(component.OriginalImageSize);
                    
                    component.SetBounds(new RectangleD(p, b));
                    App.EditorScreen.AddContentComponent(component);

                    component.CreateUserAction = true;
                    
                    currentY += b.Height + 40;
                    Logger.Log("Success 1");
                } else {
                    waiters.Add(new object());
                    component.Success += () => {
                        component.SetBounds(new RectangleD(new Point(offsetX, offsetY + currentY), GetComponentBounds(component.OriginalImageSize)));
                        App.EditorScreen.AddContentComponent(component);

                        component.CreateUserAction = true;
                        
                        currentY += GetComponentBounds(component.OriginalImageSize).Height + 40;
                        Logger.Log("Success 2", offsetX, "|", offsetY + currentY);
                        waiters.RemoveAt(0);
                    };
                }
            }

            void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
                if (waiters.Count != 0) return;
                if (offset == default) {
                    Logger.Log("Scroll", 0, offsetY + currentY);
                    App.EditorScreen.ChangeScrollView(0, offsetY, App.EditorScreen.ScrollZoom);
                } else {
                    Logger.Log("Scroll", offsetX, offsetY + currentY);
                    App.EditorScreen.ChangeScrollView(offsetX, offsetY, App.EditorScreen.ScrollZoom);
                }
            }

            waiters.CollectionChanged += OnCollectionChanged;
        }
        
        public void OnDocumentClicked(WTouchEventArgs touchEventArgs, ComponentModel componentModel) { }
    }
}