using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Components;
using AINotes.Components.Implementations;
using AINotes.Components.Tools;
using AINotes.Controls.Input;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.UserActions;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using AINotes.Models;
using MaterialComponents;
using Newtonsoft.Json;
using Point = Windows.Foundation.Point;

namespace AINotes.Screens {
    // TODO: fix LoadUserExtensions crashing on some devices
    // TODO: read InkCanvasPointerPositionUpdate to _lastPointerPosition paste

    public partial class EditorScreen {
        // do not refocus
        public readonly List<DependencyObject> DoNotRefocus = new List<DependencyObject>();
       
        // init state
        public bool Initialized;

        // loading state
        private bool _mainFileContentLoaded;
        private bool _threadFileContentLoaded;
        private bool _internalThreadFileContentLoaded;
        private bool _componentLoadingFinished;

        public bool FileContentLoaded {
            get => _mainFileContentLoaded && _threadFileContentLoaded && _internalThreadFileContentLoaded && _componentLoadingFinished;
            set {
                if (value) throw new ArgumentException("Only use this to set all values to false.");
                _mainFileContentLoaded = _threadFileContentLoaded = _internalThreadFileContentLoaded = _componentLoadingFinished = false;
            }
        }

        private bool _isPageLoaded;
        public event Action PageLoaded;
        public bool IsPageLoaded {
            get => _isPageLoaded;
            set {
                if (value == _isPageLoaded) return;
                _isPageLoaded = value;
                if (_isPageLoaded) PageLoaded?.Invoke();
                Logger.Log("[EditorScreen]", $"PageLoaded = {value}", logLevel: LogLevel.Debug);
            }
        }

        // content changed (used to check if saving is required)
        private bool _contentChanged;
        public bool ContentChanged {
            get => _contentChanged;
            set {
                if (value == _contentChanged) return;
                _contentChanged = value;
                Logger.Log("[EditorScreen]", $"ContentChanged = {value}", logLevel: LogLevel.Debug);
            }
        }

        // indicates if currently saving a file
        public bool Saving;

        // live events
        public event Action<Component> ComponentAdded;
        public event Action<Component> ComponentChanged;
        public event Action<Component> ComponentDeleted;

        public event Action<InkStroke> StrokeAdded;
        public event Action<InkStroke> StrokeChanged;
        public event Action<int> StrokeRemoved;

        // ReSharper disable UnusedMember.Global
        public void InvokeComponentAdded(Component p) => ComponentAdded?.Invoke(p);
        public void InvokeComponentChanged(Component p) => ComponentChanged?.Invoke(p);
        public void InvokeComponentDeleted(Component p) => ComponentDeleted?.Invoke(p);
        // ReSharper restore UnusedMember.Global

        // toolbar
        private ITool _selectedToolbarItem;
        public ITool SelectedToolbarItem {
            get => _selectedToolbarItem;
            set {
                _selectedToolbarItem = value;
                Logger.Log("[EditorScreen]", "Selected:", value?.ToString() ?? "null");
            }
        }

        private MDToolbarItem _moreOptionsToolbarItem;
        private MDToolbarItem _fullScreenModeToolbarItem;
        private Frame _toolbarSpacer;
        private readonly List<ITool> _componentToolbarItems = new List<ITool>();

        private SelectionComponent _selectionComponent;
        
        public EditorScreen() {
            Logger.Log("[EditorScreen]", "-> Constructor", logLevel: LogLevel.Verbose);
            InitializeComponent();

            // load the page
            LoadLayout();

            // preload
            PreloadToolbarItems();

            // quick save points
            StrokeAdded += async _ => await QuickSave();
            StrokeChanged += async _ => await QuickSave();
            StrokeRemoved += async _ => await QuickSave();

            Logger.Log("[EditorScreen]", "<- Constructor", logLevel: LogLevel.Verbose);
        }

        public enum DrawingShape {
            Polygon,
            Circle
        }

        public DrawingShape CurrentDrawingShape = DrawingShape.Polygon;
        
        public readonly List<Point> DrawingPolygonPoints = new List<Point>();
        public readonly List<Point> DrawingCirclePoints = new List<Point>();

        private void OnSelectionCanvasPressed(object sender, PointerRoutedEventArgs args) {
            const int nobSize = 12;
            
            switch (CurrentDrawingShape) {
                case DrawingShape.Circle:
                    if (DrawingCirclePoints.Count == 0) {
                        DrawingCirclePoints.Add(args.GetCurrentPoint((UIElement) sender).Position);
                        SelectionCanvas.Children.Add(new Frame {
                            Width = nobSize,
                            Height = nobSize,
                            Background = new SolidColorBrush(Colors.Green),
                            CornerRadius = new CornerRadius(nobSize / 2.0)
                        }, DrawingCirclePoints[0]);
                    }
                    
                    break;
                case DrawingShape.Polygon:
                    break;
            }
        }

        
        private void OnSelectionCanvasReleased(object sender, PointerRoutedEventArgs args) {
            const int nobSize = 12;
            
            switch (CurrentDrawingShape) {
                case DrawingShape.Circle:
                    if (DrawingCirclePoints.Count == 1) {
                        DrawingCirclePoints.Add(args.GetCurrentPoint((UIElement) sender).Position);
                        SelectionCanvas.Children.Add(new Frame {
                            Width = nobSize,
                            Height = nobSize,
                            Background = new SolidColorBrush(Colors.Green),
                            CornerRadius = new CornerRadius(nobSize / 2.0)
                        }, DrawingCirclePoints[1]);
                        
                        var linePoints = new List<Point>();

                        var radius = Math.Sqrt(Math.Pow(DrawingCirclePoints[0].X - DrawingCirclePoints[1].X, 2) + 
                                               Math.Pow(DrawingCirclePoints[0].Y - DrawingCirclePoints[1].Y, 2));
                        
                        for (var i = 0.0; i <= 2 * Math.PI + 1; i += 0.05) {
                            var x = DrawingCirclePoints[0].X + radius * Math.Cos(i);
                            var y = DrawingCirclePoints[0].Y + radius * Math.Sin(i);
                    
                            linePoints.Add(new Point(x, y));
                        }
                        
                        InkCanvas.AddLine(linePoints);

                        ShapeDrawingMode = false;
                    }

                    break;
                case DrawingShape.Polygon:
                    DrawingPolygonPoints.Add(args.GetCurrentPoint((UIElement) sender).Position);
                    var polygonVertex = new Frame {
                        Width = nobSize,
                        Height = nobSize,
                        CornerRadius = new CornerRadius(nobSize / 2.0)
                    };
                    SelectionCanvas.Children.Add(polygonVertex, DrawingPolygonPoints.Last());

                    if (DrawingPolygonPoints.Count == 1) {
                        polygonVertex.PointerReleased += (_, _) => {
                            DrawingPolygonPoints.Add(new Point(DrawingPolygonPoints[0].X, DrawingPolygonPoints[0].Y));
                            InkCanvas.AddLine(DrawingPolygonPoints);

                            ShapeDrawingMode = false;
                        };

                        polygonVertex.PointerEntered += (_, _) => {
                            polygonVertex.Background = new SolidColorBrush(Colors.Gray);
                        };

                        polygonVertex.PointerExited += (_, _) => {
                            polygonVertex.Background = new SolidColorBrush(Colors.Blue);
                        };
                        
                        polygonVertex.Background = new SolidColorBrush(Colors.Blue);
                        break;
                    }
                    
                    polygonVertex.Background = new SolidColorBrush(Colors.Green);
                    
                    break;
            }
        }

        private static bool _shapeDrawingMode;
        
        public static bool ShapeDrawingMode {
            get => _shapeDrawingMode;
            set {
                App.EditorScreen.SelectionCanvas.Children.Clear();
                App.EditorScreen.EndSelection();
                
                App.EditorScreen.DrawingPolygonPoints.Clear();
                App.EditorScreen.DrawingCirclePoints.Clear();

                if (value) {
                    App.EditorScreen.Document.PointerReleased += App.EditorScreen.OnSelectionCanvasReleased;
                    App.EditorScreen.Document.PointerPressed += App.EditorScreen.OnSelectionCanvasPressed;
                    
                    App.EditorScreen.InkCanvas.InputTransparent = true;
                } else {
                    App.EditorScreen.Document.PointerReleased -= App.EditorScreen.OnSelectionCanvasReleased;
                    App.EditorScreen.Document.PointerPressed -= App.EditorScreen.OnSelectionCanvasPressed;
                }

                _shapeDrawingMode = value;
            }
        }

        private void DrawPolygon() {
            CurrentDrawingShape = DrawingShape.Polygon;
            ShapeDrawingMode = true;
        }

        private void DrawCircle() {
            CurrentDrawingShape = DrawingShape.Circle;
            ShapeDrawingMode = true;
        }

        // preload toolbar items
        private void PreloadToolbarItems() {
            _moreOptionsToolbarItem = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(Icon.More))
            };

            _fullScreenModeToolbarItem = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(Icon.FullScreen)),
            };

            _moreOptionsToolbarItem.Pressed += OnMoreOptionsTBIPressed;
            
            _fullScreenModeToolbarItem.Pressed += OnFullscreenTBIPressed;

            // subscribe to fullscreen changed events
            Window.Current.SizeChanged += OnWindowSizeChanged;

            _toolbarSpacer = new Frame {Width = 24};

            // default components
            foreach (var componentToolbarItem in ComponentManager.InstalledComponentTools.Select(componentToolbarItemType => (ITool) Activator.CreateInstance(componentToolbarItemType))) {
                componentToolbarItem.SubscribeToPressedEvents(OnToolbarItemClicked);
                _componentToolbarItems.Add(componentToolbarItem);
            }

            // user extensions
            LoadUserExtensions();
        }

        private void OnFullscreenTBIPressed(object s, EventArgs e) {
            if (Fullscreen.IsFullscreen()) {
                Fullscreen.Disable();
            } else {
                Fullscreen.Enable();
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs args) {
            _fullScreenModeToolbarItem.ImageSource = new BitmapImage(new Uri(Fullscreen.IsFullscreen() ? Icon.Close : Icon.FullScreen));
        }

        private void OnMoreOptionsTBIPressed(object s, EventArgs e) {
            CustomDropdown.ShowDropdown(new List<CustomDropdownViewTemplate> {new CustomDropdownItem("Draw Polygon", DrawPolygon, Icon.Polygon), new CustomDropdownItem("Draw Circle", DrawCircle, Icon.Circle), new CustomDropdownItemGroup("Change Line Mode", new List<CustomDropdownViewTemplate> {new CustomDropdownView(new StackPanel {Children = {new StackPanel {Padding = new Thickness(0), Margin = new Thickness(0), Spacing = 0, Children = {new CustomLineModePicker(),}}}})})}, _moreOptionsToolbarItem);
        }

        private void LoadUserExtensions() {
            // var extensionManager = new ExtensionManager();
            // if (!extensionManager.IsInitialized()) await extensionManager.InitializeAsync();
            //
            // // init maps
            // MapService.ServiceToken = "abcdef-abcdefghijklmno";
            // MainThread.BeginInvokeOnMainThread(() => {
            //     var m = new MapControl();
            //     m.MapServiceToken = MapService.ServiceToken;
            //     Logger.Log(m);
            //     
            // });
            //
            // foreach (var ext in extensionManager.GetExtensionModels()) {
            //     var result = await extensionManager.InvokeExtensionActionAsync(ext, ("type", "requestToolbarItems"));
            //     Logger.Log("InvokeExtensionActionAsync ->", result.ToFString());
            //     var max = int.Parse(result["count"]);
            //     for (var i = 0; i < max; i++) {
            //         var name = result[i + "-name"];
            //         var callbackId = result[i + "-callbackId"];
            //         var icon = result[i + "-icon"].Deserialize<byte[]>();
            //         
            //         Logger.Log($"ToolbarItem {i}");
            //         Logger.Log($" > Name: {name}");
            //         Logger.Log($" > CallbackId: {callbackId}");
            //         Logger.Log($" > Icon: {icon}");
            //
            //         async Task ExecuteResponseAction(Dictionary<string, string> message) {
            //             try {
            //                 var action = message["action"];
            //                 string actionExtra;
            //                 string[] extraSplit;
            //                 Logger.Log("Action:", action);
            //                 switch (action) {
            //                     case "nop":
            //                         break;
            //                     case "message":
            //                         actionExtra = message["actionExtra"];
            //                         extraSplit = actionExtra.Split('|');
            //                         App.Page.Notifications.Add(new MDNotification(extraSplit[0], additionalInfo: extraSplit[1], acceptButtonText: "Ok"));
            //                         break;
            //                     case "addComponent":
            //                         
            //                         actionExtra = message["actionExtra"];
            //                         Logger.Log("ActionExtra", actionExtra);
            //                         extraSplit = actionExtra.Split('|');
            //                         var typeName = extraSplit[0];
            //                         var data = extraSplit[1];
            //                         var rect = new RectangleD(
            //                             double.Parse(extraSplit[2], CultureInfo.InvariantCulture), 
            //                             double.Parse(extraSplit[3], CultureInfo.InvariantCulture), 
            //                             double.Parse(extraSplit[4], CultureInfo.InvariantCulture), 
            //                             double.Parse(extraSplit[5], CultureInfo.InvariantCulture)
            //                         );
            //                         
            //                         var type = Type.GetType(typeName);
            //                         Logger.Log("Type", type);
            //                         var component = new SimpleComponent(null, (UIElement) Activator.CreateInstance(type!));
            //                         AddContentComponent(component);
            //                         break;
            //                 }
            //             } catch (Exception ex) {
            //                 Logger.Log($"Exception in ExecuteResponseAction ({message}):", ex.ToString(), logLevel: LogLevel.Error);
            //             }
            //         }
            //
            //         var item = new SimpleTool(Icon.Access);
            //         item.SubscribeToPressedEvents(OnToolbarItemClicked);
            //         
            //         item.Selected += async () => {
            //             Logger.Log($"Item \"{name}\": Selected");
            //             var responseAction = await extensionManager.InvokeExtensionActionAsync(ext, 
            //                 ("type", "toolbarItemCallback"),
            //                 ("callbackId", callbackId),
            //                 ("callbackType", "selected")
            //             );
            //             await ExecuteResponseAction(responseAction);
            //         };
            //
            //         item.Deselected += async () => {
            //             Logger.Log($"Item \"{name}\": Deselected");
            //             var responseAction = await extensionManager.InvokeExtensionActionAsync(ext, ("type", "toolbarItemCallback"), ("callbackId", callbackId), ("callbackType", "deselected"));
            //             await ExecuteResponseAction(responseAction);
            //         };
            //         
            //         
            //         item.DocumentClickedCallback = async (WTouchEventArgs args, ComponentModel componentModel) => {
            //             Logger.Log($"Item \"{name}\": Document clicked");
            //             var responseAction = await extensionManager.InvokeExtensionActionAsync(ext, 
            //                 ("type", "toolbarItemCallback"),
            //                 ("callbackId", callbackId),
            //                 ("callbackType", "touch"),
            //                 ("callbackExtra", args.Serialize())
            //             );
            //             await ExecuteResponseAction(responseAction);
            //         };
            //         
            //         _componentToolbarItems.Add(item);
            //     }
            // }
        }

        public async void SetTitlebarColor() {
            if (!Preferences.ColoredTitlebarEnabled) return;
            if (LoadedFileModel.Labels.Count < 1) return;
            var label = await FileHelper.GetLabelAsync(LoadedFileModel.Labels[0]);
            var c = label.Color;
            App.Page.Toolbar.Background = c.ToBrush();
            Titlebar.SetColor(c);
        }

        // set the back button action when the parent is set
        public override void OnLoad() {
            base.OnLoad();
            IsPageLoaded = false;
            Logger.Log("[EditorScreen]", "-> OnLoad", logLevel: LogLevel.Verbose);
            
            // set the back button action
            MainThread.BeginInvokeOnMainThread(() => {
                Logger.Log("[EditorScreen]", "OnLoad -> Main", logLevel: LogLevel.Verbose);
                if (LoadedFileModel != null) {
                    // set the title
                    App.Page.Title = LoadedFileModel.Name;
                    
                    // set the titlebar color (if enabled)
                    SetTitlebarColor();
                }
                App.Page.OnBackPressed = OnBackPressed;
                Logger.Log("[EditorScreen]", "OnLoad <- Main", logLevel: LogLevel.Verbose);
            });

            if (!Initialized) {
                // execute only once
                LoadTouchHandler();
                RegisterShortcuts();
                Initialized = true;
            }
            
            // execute on every load
            LoadToolbarItems();

            IsPageLoaded = true;
            Logger.Log("[EditorScreen]", "<- OnLoad", logLevel: LogLevel.Verbose);
            UserActionManager.ClearStacks();
            UserActionManager.UndoToolbarItem.IsEnabled = false;
            UserActionManager.RedoToolbarItem.IsEnabled = false;
        }
        
        public void OnComponentNobMoving(Point pointerPosition) {
            
        }

        public override async void OnUnload() {
            Logger.Log("[EditorScreen]", "-> OnUnload", logLevel: LogLevel.Verbose);
            base.OnUnload();

            foreach (var component in Document.Children) {
                if (component is ImageComponent documentComponent) {
                    documentComponent.OnEditorScreenUnload();
                }
            }
            
            // clear the toolbar
            SelectedToolbarItem?.Deselect();
            App.Page.SecondaryToolbarChildren.Clear();
            
            // save the currently loaded file
            try {
                await SaveFile();
            } catch (InvalidOperationException ex) {
                Logger.Log("[EditorScreen]", "OnUnload: Error while Saving File", ex.ToString(), logLevel: LogLevel.Verbose);

            }

            // reset the titlebar color
            var titlebarColor = Configuration.Theme.Toolbar;
            App.Page.Toolbar.Background = titlebarColor;
            Titlebar.SetColor(titlebarColor.Color);
            
            Logger.Log("[EditorScreen]", "<- OnUnload", logLevel: LogLevel.Verbose);
        }

        // load the main layout
        private async void LoadLayout() {
            Logger.Log("[EditorScreen]", "-> LoadLayout", logLevel: LogLevel.Verbose);
            await Task.Run(async () => {
                Logger.Log("[EditorScreen]", "LoadLayout -> Task", logLevel: LogLevel.Verbose);
                
                void InkCanvasPointerPositionUpdate(object sender, PointerEventArgs args) {
                    //_lastPointerPosition = new Point(args.CurrentPoint.Position.X - Scroll.HorizontalOffset, args.CurrentPoint.Position.Y - Scroll.VerticalOffset);
                }
                
                // initialize all components
                await MainThread.InvokeOnMainThreadAsync(() => {
                    InkCanvas.InkPresenter.InputDeviceTypes = DeviceProperties.GetTouchCapable() ? CoreInputDeviceTypes.Pen : CoreInputDeviceTypes.Mouse;

                    InkCanvas.PointerMoving += InkCanvasPointerPositionUpdate;
                    InkCanvas.PointerHovering += InkCanvasPointerPositionUpdate;

                    // scroll drop event
                    Scroll.DragOver += OnScrollDragOver;
                    Scroll.AllowDrop = true;
                    Scroll.Drop += OnScrollDrop;

                    // hide dropdown on scroll
                    Scroll.ViewChanged += OnScrollViewChanged;
                    
                    // live events
                    Document.ChildAdded += addedElement => {
                        if (!FileContentLoaded || !IsPageLoaded) return;
                        Logger.Log("[EditorScreen]", "Document.ChildAdded", addedElement);
                        if (addedElement is Component component) {
                            ComponentAdded?.Invoke(component);
                        } else {
                            Logger.Log("[EditorScreen]", "Document Child added (non-component):", addedElement, logLevel: LogLevel.Warning);
                        }
                    };
                    
                    Document.ChildRemoved += removedElement => {
                        if (!FileContentLoaded || !IsPageLoaded) return;
                        Logger.Log("[EditorScreen]", "Document.ChildRemoved", removedElement);
                        if (removedElement is Component component) {
                            ComponentDeleted?.Invoke(component);
                        } else {
                            Logger.Log("[EditorScreen]", "Document Child removed (non-component):", removedElement, logLevel: LogLevel.Warning);
                        }
                    };
                    
                    InkCanvas.ControlStrokeAdded += sT => StrokeAdded?.Invoke(sT);
                    InkCanvas.ControlStrokeChanged += sT => StrokeChanged?.Invoke(sT);
                    InkCanvas.ControlStrokeRemoved += sT => StrokeRemoved?.Invoke(sT);
                });
                
                // add the content
                MainThread.BeginInvokeOnMainThread(() => {
                    Logger.Log("[EditorScreen]", "LoadLayout -> Main", logLevel: LogLevel.Verbose);
                    
                    MaxSizeBackgroundCanvas.Rebuild();

                    Logger.Log("[EditorScreen]", "LoadLayout <- Main", logLevel: LogLevel.Verbose);
                });
                Logger.Log("[EditorScreen]", "LoadLayout <- Task", logLevel: LogLevel.Verbose);
            });

            Logger.Log("[EditorScreen]", "<- LoadLayout", logLevel: LogLevel.Verbose);
        }

        private void OnScrollViewChanged(object sender, ScrollViewerViewChangedEventArgs args) {
            CustomDropdown.CloseDropdown();
        }

        private void OnScrollDragOver(object sender, DragEventArgs args) {
            args.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void OnScrollDrop(object sender, DragEventArgs args) {
            Logger.Log("[EditorScreen]", "Drop", args.DataView.AvailableFormats.ToFString());
            if (args.DataView.Contains(StandardDataFormats.StorageItems)) {
                var resultStorageItems = await args.DataView.GetStorageItemsAsync();

                var resultContent = new List<Stream>();
                foreach (var f in resultStorageItems.Where(sI => sI is StorageFile sF && sF.Name.ToLower().EndsWithAny(ImageComponentTool.SupportedImageExtensions))) {
                    resultContent.Add(await ((StorageFile) f).OpenStreamForReadAsync());
                }

                var pos = args.GetPosition(this);
                Logger.Log("[EditorScreen]", "Drop", "@", pos);

                var fileStreams = resultContent;
                Logger.Log("[EditorScreen]", "Drop", $"fileStreams: {fileStreams.Count}");
                foreach (var imageFileStream in fileStreams) {
                    var (imageComponent, imagePath) = await ImageComponent.New(App.EditorScreen.FileId);
                    var randomStream = await FileRandomAccessStream.OpenAsync(LocalFileHelper.ToAbsolutePath(imagePath), FileAccessMode.ReadWrite);

                    await randomStream.WriteAsync(imageFileStream.ReadAllBytes().AsBuffer());
                
                    await randomStream.FlushAsync();
                    randomStream.Dispose();
                
                    imageComponent.ShouldWriteToFile = true;
                    imageComponent.SetContent(imagePath);

                    ImageComponentTool.AddDocumentComponents(imageComponent, pos);
                }
            } else {
                // other
            }
        }

        // loads the touch handler
        private Point _lastPointerPosition;
        private void LoadTouchHandler() {
            Logger.Log("[EditorScreen]", "-> LoadTouchHandler", logLevel: LogLevel.Verbose);
            var waitForMouseRelease = false;
            void TouchAction (WTouchEventArgs e) {
                _lastPointerPosition = e.Location;

                _ = e.Handled;

                // long press
                if (e.Id == -10 && e.ActionType == WTouchAction.Pressed) {
                    OpenContextMenu(e.Location);
                    waitForMouseRelease = true;
                    return;
                }
                
                // right click
                if (e.MouseButton == WMouseButton.Right && e.ActionType == WTouchAction.Released && e.Id != -10) {
                    OpenContextMenu(e.Location);
                    return;
                }
                
                if (e.Id == -10) return;
                if (waitForMouseRelease) {
                    if (e.InContact) return;
                    if (e.ActionType == WTouchAction.Released) {
                        waitForMouseRelease = false;
                        return;
                    }
                }
                if (e.Handled) return;
                try {
                    OnCanvasTouch(new WTouchEventArgs(e.Id, e.ActionType, e.MouseButton, e.DeviceType, new Point((float) (e.Location.X * DeviceProperties.DisplayDensity), (float) (e.Location.Y * DeviceProperties.DisplayDensity)), e.InContact, e.Pointer, e.Handled));
                } catch (Exception ex) {
                    Logger.Log("[EditorScreen]", "OnCanvasTouch Exception:", ex.ToString(), logLevel: LogLevel.Error);
                }
            }
            TouchHelper.SetTouchEventHandler(Document, TouchAction);
                
            Logger.Log("[EditorScreen]", "<- LoadTouchHandler", logLevel: LogLevel.Verbose);
        }

        // load all toolbar items (including component toolbar items)
        private async void LoadToolbarItems() {
            Logger.Log("[EditorScreen]", "-> LoadToolbarItems", logLevel: LogLevel.Verbose);
            await Task.Run(() => {
                Logger.Log("[EditorScreen]", "LoadToolbarItems -> Task", logLevel: LogLevel.Verbose);
                SelectedToolbarItem = null;
                
                MainThread.BeginInvokeOnMainThread(() => {
                    Logger.Log("[EditorScreen]", "LoadToolbarItems -> Main", logLevel: LogLevel.Verbose);
                    var items = new List<UIElement> {
                        _moreOptionsToolbarItem,
                        _fullScreenModeToolbarItem,
                        UserActionManager.RedoToolbarItem,
                        UserActionManager.UndoToolbarItem,
                        _toolbarSpacer
                    };
                    items.AddRange(_componentToolbarItems.Cast<UIElement>());

                    App.Page.PrimaryToolbarChildren.Clear();
                    foreach (var itm in items) {
                        App.Page.PrimaryToolbarChildren.Add(itm);
                    }
                    
                    ((MDToolbarItem) App.Page.PrimaryToolbarChildren[5]).SendPress();
                    
                    Logger.Log("[EditorScreen]", "LoadToolbarItems <- Main", logLevel: LogLevel.Verbose);
                });
                
                Logger.Log("[EditorScreen]", "LoadToolbarItems <- Task", logLevel: LogLevel.Verbose);
            });
            
            Logger.Log("[EditorScreen]", "<- LoadToolbarItems", logLevel: LogLevel.Verbose);
        }

        // handle a click on the back button
        private void OnBackPressed() {
            Logger.Log("[EditorScreen]", "-> OnBackPressed", logLevel: LogLevel.Verbose);

            // pop the page
            App.Page.Load(App.FileManagerScreen);
            Logger.Log("[EditorScreen]", "<- OnBackPressed", logLevel: LogLevel.Verbose);
        }

        // handle selection of toolbar items
        private void OnToolbarItemClicked(object sender, EventArgs args) {
            Logger.Log("[EditorScreen]", "OnToolbarItemClicked", sender);
            SelectedToolbarItem?.Deselect();
            SelectedToolbarItem = (ITool) sender;
            InkCanvas.InputTransparent = !SelectedToolbarItem.RequiresDrawingLayer;
            InkCanvas.ClearSelection();
            if (SelectedToolbarItem.RequiresDrawingLayer) {
                foreach (var item in Document.Children) {
                    (item as Component)?.Unfocus();
                }
            }

            SelectedToolbarItem.Select();
        }

        // handle touches if not handled by a component or an overlay
        private Point _lastMiddleMousePosition;
        private void OnCanvasTouch(WTouchEventArgs e) {

            // close dropdowns etc.
            if (e.ActionType == WTouchAction.Pressed) {
                CustomDropdown.CloseDropdown();
                InkCanvas.ClearSelection();
                App.EditorScreen.EndSelection();
            }

            // middle mouse button panning
            if (e.DeviceType == WTouchDeviceType.Mouse && e.MouseButton == WMouseButton.Middle) {
                switch (e.ActionType) {
                    case WTouchAction.Pressed:
                        PointerHelper.SetPointerCursor(CoreCursorType.Hand);
                        _lastMiddleMousePosition = e.Location;
                        break;
                    case WTouchAction.Moved:
                        PointerHelper.SetPointerCursor(CoreCursorType.Hand);
                        Scroll.ChangeView(Scroll.HorizontalOffset - (e.Location.X - _lastMiddleMousePosition.X), Scroll.VerticalOffset - (e.Location.Y - _lastMiddleMousePosition.Y), Scroll.ZoomFactor);
                        break;
                    case WTouchAction.Released:
                    case WTouchAction.Cancelled:
                    case WTouchAction.Exited:
                        PointerHelper.SetPointerCursor(CoreCursorType.Arrow);
                        break;
                }

                e.Handled = true;
                return;
            }
            
            if (e.ActionType == WTouchAction.Released && ShapeDrawingMode) return;

            switch (e.DeviceType) {
                // select text if handwriting or eraser is selected when using the touch
                case WTouchDeviceType.Touch:
                    Logger.Log("WTouchDeviceType.Touch", e.ActionType, e.Handled);
                    if (e.ActionType != WTouchAction.Released) break;
                    if (SelectedToolbarItem?.GetType() != typeof(HandwritingTool) && SelectedToolbarItem?.GetType() != typeof(EraserTool)) break;
                    if (SelectedToolbarItem?.GetType() == typeof(TextComponentTool)) break;
                    ((ITool) App.Page.PrimaryToolbarChildren.FirstOrDefault(t => (t as ITool)?.GetType() == typeof(TextComponentTool)))?.Select();

                    break;
                // select text if handwriting or eraser is selected when using touch
                case WTouchDeviceType.Mouse:
                    if (!e.InContact) break;
                    if (!DeviceProperties.GetTouchCapable()) break; // disable on non-touch devices
                    if (SelectedToolbarItem.GetType() != typeof(HandwritingTool) && SelectedToolbarItem.GetType() != typeof(EraserTool)) break;
                    ((ITool) App.Page.PrimaryToolbarChildren.FirstOrDefault(t => (t as ITool)?.GetType() == typeof(TextComponentTool)))?.Select();
                    break;
                
                // select handwriting / eraser / selection when using the pen
                case WTouchDeviceType.Pen:
                    switch (e.ActionType) {
                        case WTouchAction.Entered:
                        case WTouchAction.Pressed:
                            Logger.Log(e.ToString());
                            switch (e.MouseButton) {
                                case WMouseButton.Unknown: // select handwriting
                                    if (SelectedToolbarItem?.GetType() == typeof(HandwritingTool)) break;
                                    ((ITool) App.Page.PrimaryToolbarChildren.FirstOrDefault(t => (t as ITool)?.GetType() == typeof(HandwritingTool)))?.Select();
                                    break;
                                case WMouseButton.Left: // select handwriting
                                    if (SelectedToolbarItem?.GetType() == typeof(HandwritingTool)) break;
                                    ((ITool) App.Page.PrimaryToolbarChildren.FirstOrDefault(t => (t as ITool)?.GetType() == typeof(HandwritingTool)))?.Select();
                                    break;
                                case WMouseButton.Middle: // select eraser
                                    if (SelectedToolbarItem?.GetType() == typeof(EraserTool)) break;
                                    ((ITool) App.Page.PrimaryToolbarChildren.FirstOrDefault(t => (t as ITool)?.GetType() == typeof(EraserTool)))?.Select();
                                    break;
                                case WMouseButton.Right: // select selection
                                    if (SelectedToolbarItem?.GetType() == typeof(SelectionComponentTool)) break;
                                    ((ITool) App.Page.PrimaryToolbarChildren.FirstOrDefault(t => (t as ITool)?.GetType() == typeof(SelectionComponentTool)))?.Select();
                                    break;
                            }

                            break;
                    }

                    break;
            }

            // right clicks
            if (e.MouseButton == WMouseButton.Right) return;
            if (LoadedFileModel == null) return;

            SelectedToolbarItem?.OnDocumentClicked(e, new ComponentModel {
                FileId = LoadedFileModel.Id,
                Deleted = false,
                PosX = e.Location.X / DeviceProperties.DisplayDensity,
                PosY = e.Location.Y / DeviceProperties.DisplayDensity,
                ZIndex = -1,
                ComponentId = -1,
            });
        }


        // open a general context menu
        private void OpenContextMenu(Point touchLocation) {
            var (frmPositionX, frmPositionY) = Document.GetAbsoluteCoordinates();
            var (touchX, touchY) = touchLocation;
            
            var dropdownX = frmPositionX + touchX * Scroll.ZoomFactor;
            var dropdownY = frmPositionY + touchY * Scroll.ZoomFactor;
            
            var dropdownPoint = new Point(dropdownX, dropdownY);
            CustomDropdown.ShowDropdown(new List<CustomDropdownViewTemplate> {
                new CustomDropdownItem("Paste", () => {
                    var resultPoint = new Point(touchX, touchY);
                    ClipboardManager.Paste(resultPoint);
                })
            }, dropdownPoint);
        }

        public event Action<FileModel> LoadingFile;

        
        // load a file from a specified path
        public async void LoadFile(int fileId, bool loadWithoutSaving = false) {
            Logger.Log("[EditorScreen]", "-> LoadFile", logLevel: LogLevel.Debug);

            // check if the file is already loaded
            MaxSizeBackgroundCanvas.Rebuild();
            if (fileId == FileId) {
                Logger.Log("[EditorScreen]", "LoadFile: File already loaded", logLevel: LogLevel.Debug);
                Logger.Log("[EditorScreen]", "<- LoadFile", logLevel: LogLevel.Verbose);
                return;
            }

            // wait for saving
            var _ = await Task.Run(() => {
                while (Saving) {
                    Thread.Sleep(10);
                }

                return true;
            });

            FileContentLoaded = false;
            SelectedContent.Clear();

            // save the file id
            FileId = fileId;

            // save the currently loaded file
            if (!loadWithoutSaving) {
                Logger.Log("[EditorScreen]", "LoadFile -> SaveFile", logLevel: LogLevel.Verbose);
                await SaveFile();
                Logger.Log("[EditorScreen]", "LoadFile <- SaveFile", logLevel: LogLevel.Verbose);
            } else {
                Logger.Log("[EditorScreen]", "LoadFile: Load without Saving", logLevel: LogLevel.Debug);
            }

            // remove remains of previous file
            Logger.Log("[EditorScreen]", "LoadFile -> Clear Children", logLevel: LogLevel.Verbose);
            foreach (var docChild in Document.Children.ToArray()) Document.Children.Remove(docChild);
            Logger.Log("[EditorScreen]", "LoadFile <- Clear Children", logLevel: LogLevel.Verbose);

            Logger.Log("[EditorScreen]", "LoadFile -> Reset Canvas", logLevel: LogLevel.Verbose);
            InkCanvas.Reset();
            Logger.Log("[EditorScreen]", "LoadFile <- Reset Canvas", logLevel: LogLevel.Verbose);
            
            Logger.Log("[EditorScreen]", "LoadFile -> Clear AbsoluteOverlay", logLevel: LogLevel.Verbose);
            foreach (var docChild in AbsoluteOverlay.Children.ToArray()) {
                Document.Children.Remove(docChild);
            }

            Logger.Log("[EditorScreen]", "LoadFile <- Clear AbsoluteOverlay", logLevel: LogLevel.Verbose);

            // load the new file
            Logger.Log("[EditorScreen]", "LoadFile -> InternalLoadFile", logLevel: LogLevel.Verbose);
            LoadedFileModel = await FileHelper.GetFileAsync(fileId);

            LoadingFile?.Invoke(LoadedFileModel);
            Logger.Log("[EditorScreen]", "-> InternalLoadFile", logLevel: LogLevel.Verbose);

            // background
            MaxSizeBackgroundCanvas.LineMode = LoadedFileModel.LineMode;

            var loadingTask = new Task(() => {
                Logger.Log("[EditorScreen]", "InternalLoadFile -> Task", logLevel: LogLevel.Verbose);
                MainThread.BeginInvokeOnMainThread(async () => {
                    Logger.Log("[EditorScreen]", "InternalLoadFile -> Main", logLevel: LogLevel.Verbose);

                    // load strokes
                    if (LoadedFileModel.StrokeContent != null) {
                        Logger.Log("[EditorScreen]", "InternalLoadFile -> Strokes", logLevel: LogLevel.Verbose);
                        InkCanvas.LoadStrokesFromIsf(LoadedFileModel.StrokeContent.Deserialize<byte[]>());
                        Logger.Log("[EditorScreen]", "InternalLoadFile <- Strokes", logLevel: LogLevel.Verbose);
                    }

                    if (App.Page.Content == this) {
                        // set the title
                        Title = App.Page.Title = LoadedFileModel.Name;
                        
                        // set the titlebar color (if enabled)
                        SetTitlebarColor();
                    }

                    // load the components
                    var componentModels = (await LoadedFileModel.GetComponentModels()).Where(model => !model.Deleted).ToList();

                    Logger.Log("[EditorScreen]", $"InternalLoadFile -> Components ({componentModels.Count()})", logLevel: LogLevel.Verbose);
                    foreach (var componentModel in componentModels) {
                        var component = componentModel.ToComponent();
                        component.Deselect();
                        AddContentComponent(component);
                    }

                    _componentLoadingFinished = true;

                    Logger.Log("[EditorScreen]", "InternalLoadFile <- Components", logLevel: LogLevel.Verbose);

                    // set the directory
                    Logger.Log("[EditorScreen]", "InternalLoadFile -> DocumentList Directory", logLevel: LogLevel.Verbose);
                    // App.Page.LeftSidebar.DocumentList.CurrentDirectory = await FileHelper.GetDirectoryAsync(LoadedFileModel.ParentDirectoryId);
                    Logger.Log("[EditorScreen]", "InternalLoadFile <- DocumentList Directory", logLevel: LogLevel.Verbose);
                    
                    // load width / height
                    if (LoadedFileModel.Height != null) MaxSizeBackgroundCanvas.Height = (double) LoadedFileModel.Height;
                    if (LoadedFileModel.Width != null) MaxSizeBackgroundCanvas.Width = (double) LoadedFileModel.Width;
                    Scroll.InvalidateMeasure();

                    // load zoom / pan
                    Logger.Log("[EditorScreen]", "InternalLoadFile -> Zoom / Scroll", logLevel: LogLevel.Verbose);
                    if (LoadedFileModel.Zoom != default || LoadedFileModel.ScrollX != default || LoadedFileModel.Zoom != default) {
                        Logger.Log("[EditorScreen]", $"InternalLoadFile: Zoom / Pan: {LoadedFileModel.Zoom}, {LoadedFileModel.ScrollX}|{LoadedFileModel.ScrollY}");
                        Scroll.ChangeView(LoadedFileModel.ScrollX, LoadedFileModel.ScrollY, LoadedFileModel.Zoom);
                    }

                    Logger.Log("[EditorScreen]", "InternalLoadFile <- Zoom / Scroll", logLevel: LogLevel.Verbose);

                    _internalThreadFileContentLoaded = true;
                    Logger.Log("[EditorScreen]", "InternalLoadFile <- Main", logLevel: LogLevel.Verbose);
                });

                ContentChanged = false;
                _threadFileContentLoaded = true;

                Logger.Log("[EditorScreen]", "InternalLoadFile <- Task", logLevel: LogLevel.Verbose);
            }, new CancellationToken(), TaskCreationOptions.LongRunning);
            loadingTask.Start();

            Logger.Log("[EditorScreen]", "<- InternalLoadFile", logLevel: LogLevel.Verbose);
            UserActionManager.ClearStacks();
            UserActionManager.UndoToolbarItem.IsEnabled = false;
            UserActionManager.RedoToolbarItem.IsEnabled = false;

            if (App.Connection != null) {
                await App.SendToAppService(new ValueSet {{"setDiscordPresence", (DiscordDetails, DiscordDetailsState).Serialize()}});
            }
            
            Logger.Log("[EditorScreen]", "LoadFile <- InternalLoadFile", logLevel: LogLevel.Verbose);
            Logger.Log("[EditorScreen]", "<- LoadFile", logLevel: LogLevel.Verbose);
            _mainFileContentLoaded = true;
            _lastQuickSave = Time.CurrentTimeMillis();
        }

        private long _lastQuickSave;
        private async Task QuickSave() {
            if (!Preferences.QuickSavesEnabled) return;
            if (!FileContentLoaded) return;
            if (Time.CurrentTimeMillis() - _lastQuickSave < Preferences.QuickSavesInterval) return;
            Logger.Log("[EditorScreen]", "-> QuickSave");

            try {
                await SaveFile();
            } catch (Exception ex) {
                Logger.Log("[EditorScreen]", "Exception during QuickSave:", ex.ToString(), logLevel: LogLevel.Error);
            }

            _lastQuickSave = Time.CurrentTimeMillis();
            Logger.Log("[EditorScreen]", "<- QuickSave");
        }

        // save the current file
        public async Task SaveFile() {
            try {
                Logger.Log("[EditorScreen]", $"-> SaveFile {LoadedFileModel?.Name}|{LoadedFileModel?.FileId}");
                var startTime = Time.CurrentTimeMillis();
                // check for reasons to abort
                if (!FileContentLoaded || LoadedFileModel == null) {
                    Logger.Log("[EditorScreen]", $"No file content loaded ({_mainFileContentLoaded}, {_threadFileContentLoaded}, {_internalThreadFileContentLoaded}). Aborting Saving of", FileId, logLevel: LogLevel.Warning);
                    return;
                }

                if (!IsPageLoaded) {
                    Logger.Log("[EditorScreen]", "No page content loaded. Aborting Saving of", FileId, logLevel: LogLevel.Warning);
                    return;
                }

                if (Saving) {
                    Logger.Log("[EditorScreen]", "The file is already being saved. Aborting Saving of", FileId, logLevel: LogLevel.Warning);
                    return;
                }

                Saving = true;
                Logger.Log("[EditorScreen]", "Saving", FileId, logLevel: LogLevel.Debug);

                if (!ContentChanged) {
                    ContentChanged = InkCanvas.HasChanged();
                }

                if (!ContentChanged) {
                    Logger.Log("[EditorScreen]", "The file content is unchanged. Aborting Saving of", FileId, logLevel: LogLevel.Warning);
                    Saving = false;
                    return;
                }
                
                // strokes & line mode, scroll & pan
                var t1 = Time.CurrentTimeMillis();
                var strokes = JsonConvert.SerializeObject(InkCanvas.StrokeBytes);
                await FileHelper.UpdateFileAsync(FileId, 
                    strokeContent: strokes,
                    lineMode: MaxSizeBackgroundCanvas.LineMode,
                    zoom: Scroll.ZoomFactor,
                    scrollX: Scroll.HorizontalOffset,
                    scrollY: Scroll.VerticalOffset,
                    width: MaxSizeBackgroundCanvas.Width,
                    height: MaxSizeBackgroundCanvas.Height
                );
                Logger.Log("[EditorScreen]", "Saved: 2. Strokes:", Time.CurrentTimeMillis() - t1 + " ms", logLevel: LogLevel.Timing);

                Logger.Log("[EditorScreen]", "Saving of " + FileId + " finished in a total of about", Time.CurrentTimeMillis() - startTime + "ms", logLevel: LogLevel.Timing);
                Logger.Log("[EditorScreen]", "Saved", FileId, logLevel: LogLevel.Debug);
                Saving = false;
            } finally {
                Saving = false;
            }
        }

        // helper: add a component to the document
        public void AddContentComponent(Component component) {
            Logger.Log("[EditorScreen]", "-> AddContentComponent", component.GetType());
            if (Document.Children.Any(element => {
                if (element is Component c) {
                    return c.ComponentId == component.ComponentId;
                }
                return false;
            })) {
                Logger.Log("[EditorScreen]", $"AddContentComponent: Component {component.ComponentId} already in Document");
                return;
            }
            
            try {
                MainThread.InvokeOnMainThreadAsync(() => Document.Children.Add(component));
            } catch (Exception e) {
                Logger.Log("[EditorScreen]", "Error adding element of type", component, "with data", component.GetContent(), "and bounds", component.GetBounds(), "\ne:", e, logLevel: LogLevel.Error);
            }
        }

        // helper: adds a toolbar item to the secondary toolbar
        public void AddExtraToolbarItem(MDToolbarItem toolbarItem, bool doRefocus = false) {
            App.Page.SecondaryToolbarChildren.Add(toolbarItem);
            if (doRefocus) return;
            DoNotRefocus.Add(toolbarItem);
        }
    }
}