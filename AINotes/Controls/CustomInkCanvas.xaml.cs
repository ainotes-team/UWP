using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using AINotes.Components.Implementations;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.Geometry;
using AINotes.Helpers.InkCanvas;
using AINotes.Helpers.UserActions;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using AINotes.Models;
using AINotes.Models.Enums;
using MaterialComponents;

namespace AINotes.Controls {
    /* TODO Use CoreWetStrokeUpdateSource for conversion - similar to Scenario8 in SimpleInk
     * coreWetStrokeUpdateSource = CoreWetStrokeUpdateSource.Create(inkCanvas.InkPresenter);
     * coreWetStrokeUpdateSource.WetStrokeStarting += CoreWetStrokeUpdateSource_WetStrokeStarting;
     * coreWetStrokeUpdateSource.WetStrokeContinuing += CoreWetStrokeUpdateSource_WetStrokeContinuing;
     * coreWetStrokeUpdateSource.WetStrokeStopping += CoreWetStrokeUpdateSource_WetStrokeStopping;
     * coreWetStrokeUpdateSource.WetStrokeCompleted += CoreWetStrokeUpdateSource_WetStrokeCompleted;
     * coreWetStrokeUpdateSource.WetStrokeCanceled += CoreWetStrokeUpdateSource_WetStrokeCanceled;
     */
    public partial class CustomInkCanvas {
        // state
        private bool _convertingToShape;

        private readonly InkDrawingAttributes _currentDrawingAttributes = new InkDrawingAttributes {
            Color = Colors.Black,
            IgnorePressure = false,
            FitToCurve = true
        };

        private readonly Dictionary<int, InkStroke> _inkStrokesDictionary = new Dictionary<int, InkStroke>();

        // selection
        private Polyline _lasso;
        private Rectangle _selectionRectangle;
        private Rect _selectionBoundingRect;

        private const int RectPadding = 10;
        private const int ComponentPadding = 2;

        // conversion
        private readonly List<Point> _currentStrokePoints = new List<Point>();
        private Point _lastPoint;
        private const int ToleranceRadius = 12;
        private static TimeSpan StrokeConversionInterval => new TimeSpan(0, 0, 0, 0, Preferences.InkConversionTime);
        private readonly DispatcherTimer _conversionTimer = new DispatcherTimer();
        private readonly List<InkStroke> _currentlyConvertedShapes = new List<InkStroke>();
        private readonly InkStrokeBuilder _generalInkStrokeBuilder = new InkStrokeBuilder();
        private readonly List<uint> _convertedStrokeIds = new List<uint>();

        // erasing
        private readonly List<int> _erasedInkStrokeIds = new List<int>();

        public bool InkCanvasClipboardActive;

        public event EventHandler<PointerEventArgs> PointerMoving;
        public event EventHandler<PointerEventArgs> PointerHovering;

        /// initialising the InkCanvas object and subscribing to the elements events
        public CustomInkCanvas() {
            Logger.Log("[CustomInkCanvas]", "-> *ctor", logLevel: LogLevel.Debug);
            InitializeComponent();

            // set input devices & default drawing attributes
            InkPresenter.InputDeviceTypes = DeviceProperties.GetTouchCapable() ? CoreInputDeviceTypes.Pen : CoreInputDeviceTypes.Mouse;
            InkPresenter.UpdateDefaultDrawingAttributes(_currentDrawingAttributes);

            // make input transparent by default
            SetInputTransparency(true);

            // selection on right click
            InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
            InkPresenter.UnprocessedInput.PointerPressed += UnprocessedPointerPressed;
            InkPresenter.UnprocessedInput.PointerMoved += UnprocessedPointerMoved;
            InkPresenter.UnprocessedInput.PointerReleased += UnprocessedPointerReleased;

            // stroke events
            InkPresenter.StrokeInput.StrokeStarted += StrokeStarted;
            InkPresenter.StrokeInput.StrokeContinued += StrokeContinued;
            InkPresenter.StrokeInput.StrokeEnded += StrokeEnded;

            InkPresenter.StrokesCollected += StrokesCollected;
            InkPresenter.StrokesErased += StrokesErased;

            _conversionTimer.Interval = StrokeConversionInterval;
            _conversionTimer.Tick += OnConversionTimerTick;
            
            // parallel pointer events
            // TODO: HACK - Replace
            var coreInputSource = CoreInkIndependentInputSource.Create(InkPresenter);
            coreInputSource.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
            coreInputSource.PointerHovering += OnCoreInputSourcePointerHovering;
            coreInputSource.PointerMoving += OnCoreInputSourcePointerMoving;

            Logger.Log("[CustomInkCanvas]", "<- *ctor", logLevel: LogLevel.Debug);
        }

        private void OnCoreInputSourcePointerHovering(CoreInkIndependentInputSource s, PointerEventArgs e) {
            PointerHovering?.Invoke(this, e);
        }

        private void OnCoreInputSourcePointerMoving(CoreInkIndependentInputSource s, PointerEventArgs e) {
            PointerMoving?.Invoke(this, e);
        }

        public bool InputTransparent {
            set => SetInputTransparency(value);
        }

        public Canvas OverlayCanvas { get; set; }

        public IReadOnlyList<InkStroke> GetStrokes() {
            return InkPresenter.StrokeContainer.GetStrokes();
        }

        public void DeleteStrokeById(int strokeId) {
            var selectedStrokes = new List<InkStroke>();
            foreach (var stroke in InkPresenter.StrokeContainer.GetStrokes().Where(s => s.Selected)) {
                selectedStrokes.Add(stroke);
                stroke.Selected = false;
            }

            InkPresenter.StrokeContainer.GetStrokeById((uint) strokeId).Selected = true;
            InkPresenter.StrokeContainer.DeleteSelected();

            foreach (var stroke in selectedStrokes) {
                stroke.Selected = true;
            }
        }

        public void InvertSelection() {
            // TODO: Update Selection Rect
            foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) {
                inkStroke.Selected = !inkStroke.Selected;
            }
        }

        public int GetSelectedInkStrokeCount() => InkPresenter.StrokeContainer.GetStrokes().Count(stroke => stroke.Selected);

		public UserAction CutStrokes() {
			try {
				Logger.Log("[CustomInkCanvas]", "Cutting selected Strokes and Shapes", logLevel: LogLevel.Debug);

                InkPresenter.StrokeContainer.CopySelectedToClipboard();
                InkCanvasClipboardActive = true;

                var selectedInkStrokes = InkPresenter.StrokeContainer.GetStrokes().Where(inkStroke => inkStroke.Selected).ToArray();
                foreach (var stroke in selectedInkStrokes) {
                    InvokeControlStrokeRemoved((int) stroke.Id);
                }

                InkPresenter.StrokeContainer.DeleteSelected();

                var ids = selectedInkStrokes.Select(GetInkStrokeId).ToArray();
                if (ids.Length == 0) InkCanvasClipboardActive = false;

                return new UserAction(options => {
                    var inkStrokeIds = (int[]) options["inkStrokeIds"];

                    // TODO: fix bug using AddStrokes
                    foreach (var inkStrokeId in inkStrokeIds) {
                        var inkStroke = GetInkStrokeById(inkStrokeId);
                        var newInkStroke = inkStroke.Clone();
                        UpdateInkStrokeObject(newInkStroke, inkStrokeId);
                        InkPresenter.StrokeContainer.AddStroke(newInkStroke);
                    }
                }, options => {
                    var inkStrokeIds = (int[]) options["inkStrokeIds"];

                    foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) inkStroke.Selected = false;
                    foreach (var inkStrokeId in inkStrokeIds) GetInkStrokeById(inkStrokeId).Selected = true;

                    InkPresenter.StrokeContainer.DeleteSelected();
                }, new Dictionary<string, object> {
                    {"inkStrokeIds", ids},
                });
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "Error in CutStrokesAndShapes:", ex.ToString(), logLevel: LogLevel.Error);
            }

            return new UserAction(objects => {
                Logger.Log("[CustomInkCanvas]", "TODO: Undo Action", objects, logLevel: LogLevel.Warning);
            }, objects => {
                Logger.Log("[CustomInkCanvas]", "TODO: Redo Action", objects, logLevel: LogLevel.Warning);
            });
        }

        public void CopyStrokes(Point point) {
            try {
                Logger.Log("[CustomInkCanvas]", "Copying selected Strokes and Shapes", logLevel: LogLevel.Debug);

                InkPresenter.StrokeContainer.CopySelectedToClipboard();
                InkCanvasClipboardActive = true;

                if (InkPresenter.StrokeContainer.GetStrokes().Count(inkStroke => inkStroke.Selected) == 0) {
                    InkCanvasClipboardActive = false;
                }
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "Error in CopyStrokesAndShapes:", ex.ToString(), logLevel: LogLevel.Error);
            }
        }

        public UserAction PasteStrokes(Point newPosition) {
            try {
                if (!InkCanvasClipboardActive) {
                    return new UserAction(objects => {
                        Logger.Log("[CustomInkCanvas]", "TODO: Undo Action", objects, logLevel: LogLevel.Warning);
                    }, objects => {
                        Logger.Log("[CustomInkCanvas]", "TODO: Redo Action", objects, logLevel: LogLevel.Warning);
                    });
                }

                Logger.Log("[CustomInkCanvas]", "Pasting selected Strokes and Shapes at", newPosition, logLevel: LogLevel.Debug);

                var index = InkPresenter.StrokeContainer.GetStrokes().Count;

                InkPresenter.StrokeContainer.PasteFromClipboard(new Point(newPosition.X, newPosition.Y));

                var newIds = InkPresenter.StrokeContainer.GetStrokes().Skip(index).Select(AddInkStrokeToDict).ToArray();

                return new UserAction(objects => {
                    foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) {
                        inkStroke.Selected = false;
                    }

                    foreach (var id in (int[]) objects["inkStrokeIds"]) {
                        GetInkStrokeById(id).Selected = true;
                    }

                    InkPresenter.StrokeContainer.DeleteSelected();
                }, objects => {
                    foreach (var id in (int[]) objects["inkStrokeIds"]) {
                        var inkStroke = GetInkStrokeById(id);
                        var newInkStroke = inkStroke.Clone();
                        UpdateInkStrokeObject(newInkStroke, id);
                        InkPresenter.StrokeContainer.AddStroke(newInkStroke);
                    }
                }, new Dictionary<string, object> {
                    {"inkStrokeIds", newIds}
                });
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "Error in CopyStrokesAndShapes:", ex.ToString(), logLevel: LogLevel.Error);
            }

            return new UserAction(objects => {
                Logger.Log("[CustomInkCanvas]", "TODO: Undo Action", objects, logLevel: LogLevel.Warning);
            }, objects => {
                Logger.Log("[CustomInkCanvas]", "TODO: Redo Action", objects, logLevel: LogLevel.Warning);
            });
        }

        /// invoked as soon as the pointer is staying in a circle with a specific radius ToleranceRadius
        private void OnConversionTimerTick(object _, object __) {
            Logger.Log("[CustomInkCanvas]", "OnConversionTimerClick", logLevel: LogLevel.Debug);
            
            try {
                if (_convertingToShape) return;
                _convertingToShape = true;
                _conversionTimer.Stop();
            
                ConvertCurrentStrokeToShape(_currentStrokePoints.ConvertAll(input => ((int) input.X, (int) input.Y)));
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "OnConversionTimerClick: Exception:", ex.ToString(), logLevel: LogLevel.Error);
            }
            _convertingToShape = false;
        }

        private bool _currentlyConverting;

        /// trying to convert stroke that is currently drawn to shape (polygon, ellipse, line)
        private void ConvertCurrentStrokeToShape(List<(int, int)> points) {
            if (_currentlyConverting) return;
            if (points.Count == 0) {
                _currentlyConverting = false;
                return;
            }

            try {
                // check highlighter conversion rules if highlighter
                if (_currentDrawingAttributes.DrawAsHighlighter && !Preferences.MarkerInkConversionEnabled) return;

                var geometryPoints = points.Select(point => new GeometryPoint(point.Item1, point.Item2)).ToArray();

                var shape = new GeometryPolyline(geometryPoints).GetShape();

                if (shape == null || shape.ShapeType == ShapeType.None) {
                    _currentlyConverting = false;
                    return;
                }

                _generalInkStrokeBuilder.SetDefaultDrawingAttributes(new InkDrawingAttributes {
                    Color = _currentDrawingAttributes.Color,
                    Size = _currentDrawingAttributes.Size,
                    IgnorePressure = true,
                    IgnoreTilt = true,
                    PenTip = PenTipShape.Circle,
                    FitToCurve = false,
                    DrawAsHighlighter = false,
                    PenTipTransform = Matrix3x2.Identity,
                });

                Logger.Log(shape.ShapeType);
                InkStroke stroke = null;
                switch (shape.ShapeType) {
                    case ShapeType.Ellipse:
                        stroke = _generalInkStrokeBuilder.CreateStrokeFromInkPoints(shape.Polyline.Points.ConvertAll(i => new InkPoint(new Point(i.X, i.Y), .5f)), Matrix3x2.Identity);
                        break;
                    case ShapeType.Polygon:
                        stroke = _generalInkStrokeBuilder.CreateStrokeFromInkPoints(shape.Polyline.Points.ConvertAll(i => new InkPoint(new Point(i.X, i.Y), .5f)), Matrix3x2.Identity);
                        break;
                    case ShapeType.Line:
                        stroke = _generalInkStrokeBuilder.CreateStrokeFromInkPoints(shape.Polyline.Points.ConvertAll(i => new InkPoint(new Point(i.X, i.Y), .5f)), Matrix3x2.Identity);
                        break;
                    case ShapeType.None:
                        break;
                }

                if (stroke != null) {
                    foreach (var s in _currentlyConvertedShapes) {
                        s.Selected = true;
                    }

                    InkPresenter.StrokeContainer.DeleteSelected();
                    _currentlyConvertedShapes.Clear();
                    InkPresenter.StrokeContainer.AddStroke(stroke);
                    _currentlyConvertedShapes.Add(stroke);
                }

                _generalInkStrokeBuilder.SetDefaultDrawingAttributes(_currentDrawingAttributes);
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "ConvertCurrentStrokeToShape: Exception", ex.ToString(), logLevel: LogLevel.Error);
            }

            _currentlyConverting = false;
        }

        /// trying to convert all selected InkStrokes to shapes (polygon, ellipse)
        private void ConvertSelectedInks() {
            foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes().ToArray()) {
                if (!inkStroke.Selected) continue;

                var geometryPoints = inkStroke.GetInkPoints().Select(point => new GeometryPoint(point.Position.X, point.Position.Y)).ToArray();
                if (geometryPoints.Length == 0) continue;

                // check highlighter conversion rules if highlighter
                if (inkStroke.DrawingAttributes.DrawAsHighlighter && !Preferences.MarkerInkConversionEnabled) continue;

                var shape = new GeometryPolyline(geometryPoints).GetShape();

                if (shape == null || shape.ShapeType == ShapeType.None) continue;

                _generalInkStrokeBuilder.SetDefaultDrawingAttributes(new InkDrawingAttributes {
                    Color = inkStroke.DrawingAttributes.Color,
                    Size = inkStroke.DrawingAttributes.Size,
                    IgnorePressure = true,
                    IgnoreTilt = true,
                    PenTip = PenTipShape.Circle,
                    FitToCurve = false,
                    DrawAsHighlighter = false,
                    PenTipTransform = Matrix3x2.Identity,
                });

                Logger.Log(shape.ShapeType);
                
                InkStroke stroke = null;
                switch (shape.ShapeType) {
                    case ShapeType.Ellipse:
                        stroke = _generalInkStrokeBuilder.CreateStrokeFromInkPoints(shape.Polyline.Points.ConvertAll(i => new InkPoint(new Point(i.X, i.Y), .5f)), Matrix3x2.Identity);
                        break;
                    case ShapeType.Polygon:
                        stroke = _generalInkStrokeBuilder.CreateStrokeFromInkPoints(shape.Polyline.Points.ConvertAll(i => new InkPoint(new Point(i.X, i.Y), .5f)), Matrix3x2.Identity);
                        break;
                    case ShapeType.Line:
                        stroke = _generalInkStrokeBuilder.CreateStrokeFromInkPoints(shape.Polyline.Points.ConvertAll(i => new InkPoint(new Point(i.X, i.Y), .5f)), Matrix3x2.Identity);
                        break;
                    case ShapeType.None:
                        break;
                }

                if (stroke == null) continue;
                InkPresenter.StrokeContainer.DeleteSelected();
                InkPresenter.StrokeContainer.AddStroke(stroke);
            }

            _generalInkStrokeBuilder.SetDefaultDrawingAttributes(_currentDrawingAttributes);
            ClearSelection();
            RemoveSelectionRectangle();
        }

        public void AddLine(List<Point> line) {
            _generalInkStrokeBuilder.SetDefaultDrawingAttributes(new InkDrawingAttributes {
                Color = _currentDrawingAttributes.Color,
                Size = _currentDrawingAttributes.Size,
                IgnorePressure = true,
                IgnoreTilt = true,
                PenTip = PenTipShape.Circle,
                FitToCurve = false,
                DrawAsHighlighter = false,
                PenTipTransform = Matrix3x2.Identity,
            });
            
            var stroke = _generalInkStrokeBuilder.CreateStrokeFromInkPoints(line.ConvertAll(i => new InkPoint(new Point(i.X, i.Y), .5f)), Matrix3x2.Identity);
            InkPresenter.StrokeContainer.AddStroke(stroke);
            
            _generalInkStrokeBuilder.SetDefaultDrawingAttributes(_currentDrawingAttributes);
        }

        /// tries to detect written text from currently selected strokes
        // TODO: Determine Font Size
        private readonly InkRecognizerContainer _inkRecognizerContainer = new InkRecognizerContainer();

        private async void DetectHandwriting() {
            try {
                var recognitionResults = await _inkRecognizerContainer.RecognizeAsync(InkPresenter.StrokeContainer, InkRecognitionTarget.Selected);

                var convertedStrokes = new List<int>();
                var lowestX = double.MaxValue;
                var lowestY = double.MaxValue;
                var finalString = "";

                foreach (var result in recognitionResults) {
                    convertedStrokes.AddRange(result.GetStrokes().Select(GetInkStrokeId));
                    finalString += result.GetTextCandidates()[0] + " ";
                }

                foreach (var stroke in InkPresenter.StrokeContainer.GetStrokes()) {
                    if (!stroke.Selected) continue;
                    stroke.Selected = false;
                }

                foreach (var stroke in convertedStrokes.Select(GetInkStrokeById)) {
                    if (stroke.BoundingRect.X < lowestX) lowestX = stroke.BoundingRect.X;
                    if (stroke.BoundingRect.Y < lowestY) lowestY = stroke.BoundingRect.Y;
                    stroke.Selected = true;
                }

                InkPresenter.StrokeContainer.DeleteSelected();
                RemoveSelectionRectangle();
                ClearSelection();
                
                var model = new ComponentModel {
                    Type = "TextComponent",
                    Content = "",
                    Deleted = false,
                    ZIndex = -1,
                    PosX = lowestX,
                    PosY = lowestY,
                    SizeX = 37,
                    SizeY = 40
                };
                
                var textComponent = new TextComponent(model);
                
                textComponent.CreateUserAction = false;
                
                textComponent.Content.SetUnformattedText(finalString);
                textComponent.Content.TextDocument.GetText(TextGetOptions.FormatRtf, out var txt);
                textComponent.SetContent(txt);
                
                textComponent.CreateUserAction = true;

                var userAction =  new UserAction(options => {
                    var inkStrokeIds = (int[]) options["inkStrokeIds"];

                    foreach (var inkStrokeId in inkStrokeIds) {
                        var inkStroke = GetInkStrokeById(inkStrokeId);
                        var newInkStroke = inkStroke.Clone();
                        UpdateInkStrokeObject(newInkStroke, inkStrokeId);
                        InkPresenter.StrokeContainer.AddStroke(newInkStroke);
                    }

                    RemoveSelectionRectangle();
                }, options => {
                    var inkStrokeIds = (int[]) options["inkStrokeIds"];

                    foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) inkStroke.Selected = false;
                    foreach (var inkStrokeId in inkStrokeIds) GetInkStrokeById(inkStrokeId).Selected = true;

                    InkPresenter.StrokeContainer.DeleteSelected();
                }, new Dictionary<string, object> {
                    {"inkStrokeIds", convertedStrokes.ToArray()},
                });
                
                UserActionManager.OnTextConverted(textComponent, userAction);
                
                App.EditorScreen.AddDocumentComponent(textComponent, new Point(lowestX, lowestY));
                
                textComponent.Init();
                textComponent.Select();
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "Error in DetectHandwriting:", ex.ToString(), logLevel: LogLevel.Error);
            }
        }

        /// resetting by clearing Control.InkPresenter.StrokeContainer, _overlayCanvas.Children and Element.ShapeComponents
        public void Reset() {
            Logger.Log("[CustomInkCanvas]", "-> Reset", logLevel: LogLevel.Verbose);
            OverlayCanvas?.Children.Clear();
            InkPresenter.StrokeContainer.Clear();
            _inkStrokesDictionary.Clear();
            Logger.Log("[CustomInkCanvas]", "<- Reset", logLevel: LogLevel.Verbose);
        }


        public event Action<InkStroke> ControlStrokeAdded;
        public void InvokeControlStrokeAdded(InkStroke transporter) => ControlStrokeAdded?.Invoke(transporter);

        public event Action<InkStroke> ControlStrokeChanged;
        public void InvokeControlStrokeChanged(InkStroke transporter) => ControlStrokeChanged?.Invoke(transporter);

        public event Action<int> ControlStrokeRemoved;
        public void InvokeControlStrokeRemoved(int strokeId) => ControlStrokeRemoved?.Invoke(strokeId);

        public event Action<IEnumerable<int>> ControlStrokesLoaded;
        public void InvokeControlStrokesLoaded(IEnumerable<int> strokeIds) => ControlStrokesLoaded?.Invoke(strokeIds);


        public UserAction DeleteAllSelectedStrokes() {
            var selectedInkStrokes = InkPresenter.StrokeContainer.GetStrokes().Where(inkStroke => inkStroke.Selected).ToArray();
            foreach (var stroke in selectedInkStrokes) {
                InvokeControlStrokeRemoved((int) stroke.Id);
            }

            InkPresenter.StrokeContainer.DeleteSelected();

            var ids = selectedInkStrokes.Select(GetInkStrokeId).ToArray();

            App.EditorScreen.EndSelection();

            return new UserAction(options => {
                var inkStrokeIds = (int[]) options["inkStrokeIds"];

                // TODO: fix bug using AddStrokes
                foreach (var inkStrokeId in inkStrokeIds) {
                    var inkStroke = GetInkStrokeById(inkStrokeId);
                    var newInkStroke = inkStroke.Clone();
                    UpdateInkStrokeObject(newInkStroke, inkStrokeId);
                    InkPresenter.StrokeContainer.AddStroke(newInkStroke);
                }

                RemoveSelectionRectangle();
            }, options => {
                var inkStrokeIds = (int[]) options["inkStrokeIds"];

                foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) inkStroke.Selected = false;
                foreach (var inkStrokeId in inkStrokeIds) GetInkStrokeById(inkStrokeId).Selected = true;

                InkPresenter.StrokeContainer.DeleteSelected();
            }, new Dictionary<string, object> {
                {"inkStrokeIds", ids},
            });
        }

        /// clears selection except given object
        public void ClearSelection(object except = null) {
            App.EditorScreen.Selecting = false;
            foreach (var stroke in InkPresenter.StrokeContainer.GetStrokes()) stroke.Selected = false;
            foreach (var component in App.EditorScreen.GetDocumentComponents().Where(component => component != except)) component?.Deselect();

            RemoveSelectionRectangle();
            App.EditorScreen.EndSelection();

            _selectionBoundingRect = new Rect(0, 0, 0, 0);
        }
        
        public void SelectByIds(int[] inkStrokeIds) {
            foreach (var id in inkStrokeIds) {
                GetInkStrokeById(id).Selected = true;
            }
        }

        private MDToolbarItem _ocrToolbarItem;
        private MDToolbarItem _convertToolbarItem;

        // TODO: Move to Overlay
        public void AddHandwritingConversionTools() {
            Logger.Log("[CustomInkCanvas]", "AddHandwritingConversionToolbarItems");
            _ocrToolbarItem ??= new MDToolbarItem(Icon.HandwritingRecognition, OnOcrTBIPressed) {
                BorderBrush = new SolidColorBrush(Colors.Black)
            };

            _convertToolbarItem ??= new MDToolbarItem(Icon.InkRecognition, OnInkRecognitionTBIPressed) {
                BorderBrush = new SolidColorBrush(Colors.Black)
            };

            // TODO: adaptive color and stroke width change button

            _ocrToolbarItem.IsSelected = false;
            _convertToolbarItem.IsSelected = false;

            App.EditorScreen.AddAbsoluteOverlayElement(_convertToolbarItem, new Point(_selectionBoundingRect.X + _selectionBoundingRect.Width + 12, _selectionBoundingRect.Y));
            App.EditorScreen.AddAbsoluteOverlayElement(_ocrToolbarItem, new Point(_selectionBoundingRect.X + _selectionBoundingRect.Width + 12, _selectionBoundingRect.Y + 60));
            
        }

        private void OnOcrTBIPressed(object s, EventArgs e) {
            RemoveHandwritingConversionTools();
            DetectHandwriting();
        }

        private void OnInkRecognitionTBIPressed(object s, EventArgs e) {
            RemoveHandwritingConversionTools();
            ConvertSelectedInks();
        }

        public void RemoveHandwritingConversionTools() {
            Logger.Log("[CustomInkCanvas]", "RemoveHandwritingConversionToolbarItems");
            App.EditorScreen.RemoveAbsoluteOverlayElement(_convertToolbarItem);
            App.EditorScreen.RemoveAbsoluteOverlayElement(_ocrToolbarItem);
        }

        /// selecting all strokes, shapes and components
        /// marking all selected objects with selection rectangle
        public void SelectAll() {
            App.EditorScreen.Selecting = true;
            var strokes = InkPresenter.StrokeContainer.GetStrokes();
            var components = App.EditorScreen.GetDocumentComponents();
            var x2 = 0.0;
            var y2 = 0.0;

            if (strokes.Count == 0 && components.Count == 0) return;
            _selectionBoundingRect = new Rect(double.PositiveInfinity, double.PositiveInfinity, 0, 0);

            Logger.Log("[CustomInkCanvas]", "SelectAll()", logLevel: LogLevel.Debug);

            foreach (var stroke in strokes) {
                stroke.Selected = true;

                if (_selectionBoundingRect.X > stroke.BoundingRect.X) _selectionBoundingRect.X = stroke.BoundingRect.X;
                if (_selectionBoundingRect.Y > stroke.BoundingRect.Y) _selectionBoundingRect.Y = stroke.BoundingRect.Y;
                if (x2 < stroke.BoundingRect.X + stroke.BoundingRect.Width) x2 = stroke.BoundingRect.X + stroke.BoundingRect.Width;
                if (y2 < stroke.BoundingRect.Y + stroke.BoundingRect.Height) y2 = stroke.BoundingRect.Y + stroke.BoundingRect.Height;
            }

            foreach (var component in components) {
                component.IsSelected = true;
                Logger.Log("[CustomInkCanvas]", $"{component.GetType()} selected", logLevel: LogLevel.Debug);

                if (_selectionBoundingRect == new Rect(0, 0, 0, 0)) {
                    _selectionBoundingRect.X = component.GetX();
                    _selectionBoundingRect.Y = component.GetY();
                }

                if (_selectionBoundingRect.X > component.GetX()) _selectionBoundingRect.X = component.GetX();
                if (_selectionBoundingRect.Y > component.GetY()) _selectionBoundingRect.Y = component.GetY();
                if (x2 < component.GetX() + component.Width) x2 = component.GetX() + component.Width;
                if (y2 < component.GetY() + component.Height) y2 = component.GetY() + component.Height;
            }

            _selectionBoundingRect.Width = x2 - _selectionBoundingRect.X;
            _selectionBoundingRect.Height = y2 - _selectionBoundingRect.Y;
            if (_selectionBoundingRect.X == double.PositiveInfinity || _selectionBoundingRect.Y == double.PositiveInfinity) {
                _selectionBoundingRect = new Rect(0, 0, 0, 0);
            }

            var handwritingSelected = strokes.Count > 0;
            if (handwritingSelected) AddHandwritingConversionTools();
            DrawSelectionRectangle();
        }

        /// selection on right click
        private void UnprocessedPointerPressed(InkUnprocessedInput sender, PointerEventArgs args) {
            App.EditorScreen.EndSelection();
            RemoveSelectionRectangle();

            _lasso = new Polyline {
                Stroke = new SolidColorBrush(Colors.DarkSlateGray),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection {10, 1}
            };

            _lasso.Points?.Add(args.CurrentPoint.RawPosition);
            OverlayCanvas.Children.Add(_lasso);
        }

        // TODO: Show selection preview while selecting
        /// lasso creation
        private void UnprocessedPointerMoved(InkUnprocessedInput sender, PointerEventArgs args) {
            try {
                // Logger.Log("[CustomInkCanvas]", "UnprocessedPointerMoved", args.CurrentPoint.RawPosition);
                _lasso.Points?.Add(args.CurrentPoint.RawPosition);

                // TODO: make line smooth when scrolling
                App.EditorScreen.OnComponentNobMoving(new Point(args.CurrentPoint.RawPosition.X, args.CurrentPoint.RawPosition.Y));
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "UnprocessedPointerMoved - Exception:", ex.ToString());
            }
        }

        private void UnprocessedPointerReleased(InkUnprocessedInput sender, PointerEventArgs args) {
            Logger.Log("[CustomInkCanvas]", "UnprocessedPointerReleased", logLevel: LogLevel.Debug);
            _lasso.Points?.Add(args.CurrentPoint.RawPosition);
            ClearSelection();
            _selectionBoundingRect = InkPresenter.StrokeContainer.SelectWithPolyLine(_lasso.Points);

            var x2 = _selectionBoundingRect.Width + _selectionBoundingRect.X;
            var y2 = _selectionBoundingRect.Height + _selectionBoundingRect.Y;

            foreach (var component in App.EditorScreen.GetDocumentComponents()) {
                var t0 = Time.CurrentTimeMillis();
                if (!IsPointInPolygon(_lasso.Points, new Point(component.GetX(), component.GetY()))) continue;
                if (!IsPointInPolygon(_lasso.Points, new Point(component.GetX() + component.Width, component.GetY()))) continue;
                if (!IsPointInPolygon(_lasso.Points, new Point(component.GetX(), component.GetY() + component.Height))) continue;
                if (!IsPointInPolygon(_lasso.Points, new Point(component.GetX() + component.Width, component.GetY() + component.Height))) continue;
                App.EditorScreen.Selecting = true;
                component.IsSelected = true;
                Logger.Log("[CustomInkCanvas]", $"{component.GetType()} selected | took {Time.CurrentTimeMillis() - t0}ms");

                if (_selectionBoundingRect == new Rect(0, 0, 0, 0)) {
                    _selectionBoundingRect.X = component.GetX();
                    _selectionBoundingRect.Y = component.GetY();
                }

                if (_selectionBoundingRect.X > component.GetX()) _selectionBoundingRect.X = component.GetX();
                if (_selectionBoundingRect.Y > component.GetY()) _selectionBoundingRect.Y = component.GetY();
                if (x2 < component.GetX() + component.Width) x2 = component.GetX() + component.Width;
                if (y2 < component.GetY() + component.Height) y2 = component.GetY() + component.Height;
            }

            _selectionBoundingRect.Width = x2 - _selectionBoundingRect.X;
            _selectionBoundingRect.Height = y2 - _selectionBoundingRect.Y;

            if (InkPresenter.StrokeContainer.GetStrokes().Any(inkStroke => inkStroke.Selected)) AddHandwritingConversionTools();
            DrawSelectionRectangle();
        }

        private static bool IsPointInPolygon(PointCollection polygon, Point testPoint) {
            var result = false;
            var j = polygon.Count - 1;
            for (var i = 0; i < polygon.Count; i++) {
                var x = testPoint.X;
                var y = testPoint.Y;
                if (polygon[i].Y < y && polygon[j].Y >= y || polygon[j].Y < y && polygon[i].Y >= y)
                    if (polygon[i].X + (y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < x)
                        result = !result;
                j = i;
            }

            return result;
        }

        public byte[] StrokeBytes {
            get {
                var ms = new MemoryStream();
                InkPresenter.StrokeContainer.SaveAsync(ms.AsOutputStream(), InkPersistenceFormat.Isf).GetAwaiter().GetResult();
                return ms.GetBuffer();
            }
        }

        public bool IsVisible {
            set => Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        public async void LoadStrokesFromIsf(byte[] strokes) {
            Logger.Log("[CustomInkCanvas]", "-> LoadStrokesFromGif()", logLevel: LogLevel.Debug);
            if (strokes == null || strokes.Length == 0) {
                Logger.Log("[CustomInkCanvas]", "LoadStrokesFromGif cancelled", logLevel: LogLevel.Warning);
                return;
            }

            try {
                var strokeStream = new MemoryStream(strokes).AsRandomAccessStream();
                await InkPresenter.StrokeContainer.LoadAsync(strokeStream);

                var strokeIds = new List<int>();
                foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) {
                    AddInkStrokeToDict(inkStroke);
                    strokeIds.Add((int) inkStroke.Id);
                }

                Logger.Log("[CustomInkCanvas]", "StrokeCount:", strokeIds.Count);
                InvokeControlStrokesLoaded(strokeIds);
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "Stroke Loading failed:", ex.ToString(), logLevel: LogLevel.Error);
                Logger.Log("[CustomInkCanvas]", "Strokes Backup", strokes.Serialize());
            }

            Logger.Log("[CustomInkCanvas]", "<- LoadStrokesFromGif", logLevel: LogLevel.Debug);
        }

        public void OnSelectionComponentResizing(RectangleD oldRect, RectangleD newRect, List<int> strokes = null) {
            var (oX, oY, oWidth, oHeight) = oldRect;
            var (_, _, nWidth, nHeight) = newRect;
            var xFactor = nWidth / oWidth;
            var yFactor = nHeight / oHeight;

            if (strokes != null) {
                foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) {
                    inkStroke.Selected = false;
                }

                foreach (var inkStroke in strokes.Select(GetInkStrokeById)) {
                    inkStroke.Selected = true;
                }
            }

            // resize strokes
            var scaleMatrix = Matrix3x2.CreateScale((float) xFactor, (float) yFactor, new Vector2((float) oX, (float) oY));
            foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) {
                if (!inkStroke.Selected) continue;

                inkStroke.PointTransform *= scaleMatrix;

                var drawingAttributes = inkStroke.DrawingAttributes;
                var daSize = drawingAttributes.Size;
                daSize.Width *= xFactor;
                daSize.Height *= yFactor;
                drawingAttributes.Size = daSize;
                inkStroke.DrawingAttributes = drawingAttributes;

                if (LocalSharingHelper.LiveSharing) {
                    InvokeControlStrokeChanged(inkStroke);
                }
            }

            // update the selection rect
            UpdateSelectionRectangle(newRect);
        }

        // move strokes with the selection component (calls UpdateBoundingRect!)
        public void OnSelectionComponentMoving(RectangleD oRect, RectangleD nRect, List<int> strokes = null) {
            var (oX, oY, _, _) = oRect;
            var (nX, nY, _, _) = nRect;

            if (strokes != null) {
                foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) {
                    inkStroke.Selected = false;
                }

                foreach (var inkStroke in strokes.Select(GetInkStrokeById)) {
                    inkStroke.Selected = true;
                }
            }

            // move strokes
            InkPresenter.StrokeContainer.MoveSelected(new Point(nX - oX, nY - oY));

            // send stroke changed event if live-sharing
            if (LocalSharingHelper.LiveSharing) {
                foreach (var stroke in InkPresenter.StrokeContainer.GetStrokes().Where(s => s.Selected)) {
                    InvokeControlStrokeChanged(stroke);
                }
            }

            UpdateSelectionRectangle(nRect);
        }

        public int AddStroke(InkStroke newStroke) {
            InkPresenter.StrokeContainer.AddStroke(newStroke);
            return (int) newStroke.Id;
        }

        public void AddStroke(List<StrokePointModel> points, Color color, double width, double height, double transparency, PenTip penTip, PenType penType, bool ignorePressure, bool antiAliased, bool fitToCurve) {
            var builder = new InkStrokeBuilder();
            var drawingAttributes = _currentDrawingAttributes;
            drawingAttributes.Color = color;
            drawingAttributes.Size = new Size(width, height);
            drawingAttributes.IgnorePressure = ignorePressure;
            switch (penTip) {
                case PenTip.Circle:
                    drawingAttributes.PenTip = PenTipShape.Circle;
                    break;
                case PenTip.Rectangle:
                    drawingAttributes.PenTip = PenTipShape.Rectangle;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(penTip), penTip, null);
            }

            switch (penType) {
                case PenType.Default:
                    drawingAttributes.DrawAsHighlighter = false;
                    break;
                case PenType.Marker:
                    drawingAttributes.DrawAsHighlighter = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(penType), penType, null);
            }

            drawingAttributes.IgnoreTilt = true;
            drawingAttributes.FitToCurve = fitToCurve;
            builder.SetDefaultDrawingAttributes(drawingAttributes);
            var stroke = builder.CreateStrokeFromInkPoints(points.ConvertAll(point => new InkPoint(new Point(point.X, point.Y), point.Pressure)), Matrix3x2.Identity);
            InkPresenter.StrokeContainer.AddStroke(stroke);
        }

        private void StrokeStarted(InkStrokeInput sender, PointerEventArgs args) {
            Logger.Log("[CustomInkCanvas]", "StrokeStarted", args.CurrentPoint.Position);
            _currentlyConvertedShapes.Clear();
            CustomDropdown.CloseDropdown();
            UserActionManager.ClearRedoActionStack();
            App.EditorScreen.EndSelection();
            ClearSelection();
            _currentStrokePoints.Clear();
            _currentStrokePoints.Add(args.CurrentPoint.Position);
            _lastPoint = args.CurrentPoint.Position;
            _conversionTimer.Interval = StrokeConversionInterval;
            _conversionTimer.Start();
        }

        private void StrokeContinued(InkStrokeInput sender, PointerEventArgs args) {
            try {
                // Logger.Log("[CustomInkCanvas]", "StrokeContinued", args.CurrentPoint.Position);
                _currentStrokePoints.Add(args.CurrentPoint.Position);
                var distanceToLastPoint = Math.Sqrt(Math.Pow(args.CurrentPoint.Position.X - _lastPoint.X, 2) + Math.Pow(args.CurrentPoint.Position.Y - _lastPoint.Y, 2));
                if (distanceToLastPoint < ToleranceRadius) return;

                if (distanceToLastPoint > ToleranceRadius + 1) {
                    foreach (var s in _currentlyConvertedShapes) s.Selected = true;
                    InkPresenter.StrokeContainer.DeleteSelected();
                    _currentlyConvertedShapes.Clear();
                }

                _conversionTimer.Interval = StrokeConversionInterval;
                _conversionTimer.Start();
                _lastPoint = args.CurrentPoint.Position;
            } catch (Exception ex) {
                Logger.Log("[CustomInkCanvas]", "StrokeContinued - Exception:", ex.ToString());
            }
        }

        private void StrokeEnded(InkStrokeInput sender, PointerEventArgs args) {
            _conversionTimer.Stop();
        }

        private void StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args) {
            foreach (var stroke in args.Strokes) {
                if (_convertedStrokeIds.Contains(stroke.Id)) continue;
                InvokeControlStrokeAdded(stroke);
            }

            var strokeId = AddInkStrokeToDict(args.Strokes[0]);
            OnStrokesAdded(new[] {strokeId});

            // delete converted stroke
            if (_currentlyConvertedShapes.Count >= 1 && args.Strokes.Count >= 1) {
                foreach (var stroke in InkPresenter.StrokeContainer.GetStrokes()) {
                    stroke.Selected = false;
                }

                args.Strokes[0].Selected = true;
                InkPresenter.StrokeContainer.DeleteSelected();
                _convertedStrokeIds.Add(args.Strokes[0].Id);

                // should only be one stroke
                var convertedId = AddInkStrokeToDict(_currentlyConvertedShapes[0]);

                OnStrokesConverted(new Dictionary<int, int> {
                    {strokeId, convertedId}
                });
            }
        }

        private void StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args) {
            foreach (var inkStroke in args.Strokes) {
                Logger.Log("[CustomInkCanvas]", "StrokesErased -> InvokeControlStrokeRemoved: ", inkStroke.Id, logLevel: LogLevel.Debug);
                InvokeControlStrokeRemoved((int) inkStroke.Id);
                _erasedInkStrokeIds.Add(GetInkStrokeId(inkStroke));
            }

            App.EditorScreen.EndSelection();
            ClearSelection();

            if (_erasedInkStrokeIds.Count < 1) return;
            OnStrokesRemoved(_erasedInkStrokeIds.ToArray());

            _erasedInkStrokeIds.Clear();
        }

        private void SetInputTransparency(bool b) {
            IsHitTestVisible = IsHitTestVisible = InkPresenter.IsInputEnabled = !b;
        }

        public void SetColor(Color color) {
            Logger.Log("SetColor", color);
            _currentDrawingAttributes.Color = color;
            InkPresenter.UpdateDefaultDrawingAttributes(_currentDrawingAttributes);
        }

        public void SetPenSize(Size size) {
            Logger.Log("SetPenSize", size);
            _currentDrawingAttributes.Size = size;
            InkPresenter.UpdateDefaultDrawingAttributes(_currentDrawingAttributes);
        }

        public void SetIgnorePressure(bool ignore) {
            Logger.Log("SetIgnorePressure", ignore);
            _currentDrawingAttributes.IgnorePressure = ignore;
            InkPresenter.UpdateDefaultDrawingAttributes(_currentDrawingAttributes);
        }

        public void SetPenTip(PenTipShape tip) {
            Logger.Log("SetPenTip", tip);
            _currentDrawingAttributes.PenTip = tip;
            InkPresenter.UpdateDefaultDrawingAttributes(_currentDrawingAttributes);
        }

        public void SetIgnoreTilt(bool ignore) {
            Logger.Log("SetIgnoreTilt", ignore);
            _currentDrawingAttributes.IgnoreTilt = ignore;
            InkPresenter.UpdateDefaultDrawingAttributes(_currentDrawingAttributes);
        }

        public void SetHighlighter(bool highlighter) {
            Logger.Log("SetHighlighter", highlighter);
            _currentDrawingAttributes.DrawAsHighlighter = highlighter;
            InkPresenter.UpdateDefaultDrawingAttributes(_currentDrawingAttributes);
        }

        public void SetDrawingMode(InkCanvasMode mode) {
            switch (mode) {
                case InkCanvasMode.Draw:
                    InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                    break;
                case InkCanvasMode.Erase:
                    InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
                    break;
                case InkCanvasMode.Ignore:
                    InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
                    break;
            }
        }

        private void DrawSelectionRectangle() {
            RemoveSelectionRectangle();
            if (_selectionBoundingRect.Width == 0 || _selectionBoundingRect.Height == 0 || _selectionBoundingRect.IsEmpty) return;
            _selectionRectangle = new Rectangle {
                Stroke = new SolidColorBrush(Colors.DarkSlateGray),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection {10, 2},
                Width = _selectionBoundingRect.Width + (RectPadding + ComponentPadding) * 2,
                Height = _selectionBoundingRect.Height + (RectPadding + ComponentPadding) * 2,
                IsHitTestVisible = false,
            };

            Canvas.SetLeft(_selectionRectangle, _selectionBoundingRect.X - (RectPadding + ComponentPadding));
            Canvas.SetTop(_selectionRectangle, _selectionBoundingRect.Y - (RectPadding + ComponentPadding));
            OverlayCanvas.Children.Add(_selectionRectangle);
            App.EditorScreen.StartSelection(new RectangleD(_selectionBoundingRect.X - RectPadding, _selectionBoundingRect.Y - RectPadding, _selectionBoundingRect.Width + RectPadding * 2, _selectionBoundingRect.Height + RectPadding * 2));
        }

        private void UpdateSelectionRectangle(RectangleD rectangle) {
            var (x, y, width, height) = rectangle;
            if (width == 0 || height == 0) return;
            _selectionRectangle.Width = width + ComponentPadding * 2 - 4;
            _selectionRectangle.Height = height + ComponentPadding * 2 - 4;

            _selectionRectangle.Translation = new Vector3((float) (x - Canvas.GetLeft(_selectionRectangle)), (float) (y - Canvas.GetTop(_selectionRectangle)), 0);

            if (_convertToolbarItem != null) {
                Canvas.SetLeft(_convertToolbarItem, rectangle.X + rectangle.Width + 12);
                Canvas.SetTop(_convertToolbarItem, rectangle.Y);
            }

            if (_ocrToolbarItem != null) {
                Canvas.SetLeft(_ocrToolbarItem, rectangle.X + rectangle.Width + 12);
                Canvas.SetTop(_ocrToolbarItem, rectangle.Y + 60);
            }
        }

        public List<int> GetSelectedInkStrokeIds() {
            return InkPresenter.StrokeContainer.GetStrokes().Where(stroke => stroke.Selected).Select(GetInkStrokeId).ToList();
        }

        private void RemoveSelectionRectangle() {
            App.EditorScreen.EndSelection();
            if (_selectionRectangle != null) OverlayCanvas.Children.Remove(_selectionRectangle);
            if (_lasso != null) OverlayCanvas.Children.Remove(_lasso);
        }

        private int AddInkStrokeToDict(InkStroke inkStroke) {
            _inkStrokesDictionary.Add(_inkStrokesDictionary.Count, inkStroke);
            return _inkStrokesDictionary.Count - 1;
        }

        private void UpdateInkStrokeObject(InkStroke inkStroke, int id) {
            if (!_inkStrokesDictionary.ContainsKey(id)) return;
            _inkStrokesDictionary[id] = inkStroke;
        }

        private InkStroke GetInkStrokeById(int id) => _inkStrokesDictionary[id];

        private int GetInkStrokeId(InkStroke inkStroke) => _inkStrokesDictionary.FirstOrDefault(x => x.Value == inkStroke).Key;

        private void OnStrokesAdded(int[] ids) {
            UserActionManager.AddUserAction(new UserAction(options => {
                var inkStrokeIds = (int[]) options["inkStrokeIds"];

                foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) inkStroke.Selected = false;
                foreach (var inkStrokeId in inkStrokeIds) GetInkStrokeById(inkStrokeId).Selected = true;
                InkPresenter.StrokeContainer.DeleteSelected();
            }, options => {
                var inkStrokeIds = (int[]) options["inkStrokeIds"];

                // TODO: fix bug using AddStrokes
                foreach (var inkStrokeId in inkStrokeIds) {
                    var inkStroke = GetInkStrokeById(inkStrokeId);
                    var newInkStroke = inkStroke.Clone();
                    UpdateInkStrokeObject(newInkStroke, inkStrokeId);
                    InkPresenter.StrokeContainer.AddStroke(newInkStroke);
                }
            }, new Dictionary<string, object> {
                {"inkStrokeIds", ids}
            }));
        }

        private void OnStrokesRemoved(int[] ids) {
            // TODO: fix push UserAction per stroke => UserAction on eraser released
            UserActionManager.AddUserAction(new UserAction(options => {
                var inkStrokeIds = (int[]) options["inkStrokeIds"];

                // TODO: fix bug using AddStrokes
                foreach (var inkStrokeId in inkStrokeIds) {
                    var inkStroke = GetInkStrokeById(inkStrokeId);
                    var newInkStroke = inkStroke.Clone();
                    UpdateInkStrokeObject(newInkStroke, inkStrokeId);
                    InkPresenter.StrokeContainer.AddStroke(newInkStroke);
                }
            }, options => {
                var inkStrokeIds = (int[]) options["inkStrokeIds"];

                foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) inkStroke.Selected = false;
                foreach (var inkStrokeId in inkStrokeIds) GetInkStrokeById(inkStrokeId).Selected = true;

                InkPresenter.StrokeContainer.DeleteSelected();
            }, new Dictionary<string, object> {
                {"inkStrokeIds", ids},
            }));
        }

        private void OnStrokesConverted(Dictionary<int, int> inkStrokePairs) {
            UserActionManager.AddUserAction(new UserAction(options => {
                var inkStrokeIds = options["inkStrokePairIds"] as Dictionary<int, int>;

                foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) inkStroke.Selected = false;

                // TODO: fix bug using AddStrokes
                if (inkStrokeIds != null)
                    foreach (var (strokeId, convertedId) in inkStrokeIds) {
                        GetInkStrokeById(convertedId).Selected = true;
                        var inkStroke = GetInkStrokeById(strokeId);
                        var newInkStroke = inkStroke.Clone();
                        UpdateInkStrokeObject(newInkStroke, strokeId);
                        InkPresenter.StrokeContainer.AddStroke(newInkStroke);
                    }

                InkPresenter.StrokeContainer.DeleteSelected();
            }, options => {
                var inkStrokeIds = options["inkStrokePairIds"] as Dictionary<int, int>;

                foreach (var inkStroke in InkPresenter.StrokeContainer.GetStrokes()) inkStroke.Selected = false;

                // TODO: fix bug using AddStrokes
                if (inkStrokeIds != null)
                    foreach (var (strokeId, convertedId) in inkStrokeIds) {
                        GetInkStrokeById(strokeId).Selected = true;
                        var inkStroke = GetInkStrokeById(convertedId);
                        var newInkStroke = inkStroke.Clone();
                        UpdateInkStrokeObject(newInkStroke, convertedId);
                        InkPresenter.StrokeContainer.AddStroke(newInkStroke);
                    }

                InkPresenter.StrokeContainer.DeleteSelected();
            }, new Dictionary<string, object> {
                {"inkStrokePairIds", inkStrokePairs},
            }));
        }

        // TODO: HasChanged
        public bool HasChanged() => true;
    }
}