using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using AINotes.Components;
using AINotes.Components.Implementations;
using AINotes.Controls;
using AINotes.Helpers.Extensions;
using AINotes.Helpers.UserActions;
using Helpers;
using Helpers.Extensions;
using Helpers.Lists;
using AINotes.Models;
using AINotes.Models.Enums;
using Point = Windows.Foundation.Point;

namespace AINotes.Screens {
    public partial class EditorScreen {
        // current file
        public int FileId;
        public FileModel LoadedFileModel;

        // selection state
        public bool Selecting = false;

        // selection
        public readonly ObservableList<Component> SelectedContent = new ObservableList<Component> {
            PreventDuplicates = true
        };

        public double BackgroundHorizontalStep => MaxSizeBackgroundCanvas.HorizontalStep;
        public double BackgroundVerticalStep => MaxSizeBackgroundCanvas.VerticalStep;

        public DocumentLineMode BackgroundLineMode {
            get => MaxSizeBackgroundCanvas.LineMode;
            set => MaxSizeBackgroundCanvas.LineMode = value;
        }

        public double ScrollX => Scroll.HorizontalOffset;
        public double ScrollY => Scroll.VerticalOffset;
        public float ScrollZoom => Scroll.ZoomFactor;

        public bool ScrollDisabled {
            set => Scroll.SetDisableScrolling(value);
        }

        public float ScrollMinZoom {
            get => Preferences.ZoomMinScale;
            set => Scroll.MinZoomFactor = value;
        }

        public float ScrollMaxZoom {
            get => Preferences.ZoomMaxScale;
            set => Scroll.MaxZoomFactor = value;
        }

        public void ChangeScrollView(double scrollX, double scrollY, float zoom) {
            Scroll.ChangeView(scrollX, scrollY, zoom);
        }

        public void AddDocumentComponent(Component c, Point p) {
            Document.AddChild(c, p);
        }

        public void AddDocumentComponent(Component c, RectangleD r) {
            Document.AddChild(c, r);
        }

        public void RemoveDocumentComponent(Component c) {
            Document.Children.Remove(c);
        }

        public void AddAbsoluteOverlayElement(FrameworkElement e) {
            if (e != null && !AbsoluteOverlay.Children.Contains(e)) {
                AbsoluteOverlay.Children.Add(e);
            }
        }

        public void AddAbsoluteOverlayElement(FrameworkElement e, Point p) {
            if (e != null && !AbsoluteOverlay.Children.Contains(e)) {
                AbsoluteOverlay.Children.Add(e, p);
            }
        }

        public void RemoveAbsoluteOverlayElement(FrameworkElement e) {
            if (e != null && AbsoluteOverlay.Children.Contains(e)) {
                AbsoluteOverlay.Children.Remove(e);
            }
        }

        public UserAction PasteStrokes(Point p) {
            return InkCanvas.PasteStrokes(p);
        }

        public int AddInkStroke(InkStroke s) {
            return InkCanvas.AddStroke(s);
        }

        public void DeleteInkStrokeById(int strokeId) {
            InkCanvas.DeleteStrokeById(strokeId);
        }

        public List<Component> GetDocumentComponents() {
            return Document.Children.Where(c => c is Component).Cast<Component>().ToList();
        }

        public IReadOnlyList<InkStroke> GetInkStrokes() {
            return InkCanvas.GetStrokes();
        }

        public List<int> GetSelectedInkStrokeIds() {
            return InkCanvas.GetSelectedInkStrokeIds();
        }

        public int GetSelectedInkStrokeCount() {
            return InkCanvas.GetSelectedInkStrokeCount();
        }

        public void SetInkDrawingMode(InkCanvasMode mode) {
            InkCanvas.SetDrawingMode(mode);
        }

        public void ResetInk() {
            InkCanvas.Reset();
        }

        public void LoadStrokes(byte[] strokeData) {
            InkCanvas.LoadStrokesFromIsf(strokeData);
        }

        public void SubscribeInkChanges(Action<IEnumerable<int>> strokesLoadedCallback) {
            InkCanvas.ControlStrokesLoaded += strokesLoadedCallback;
        }

        public void UnsubscribeInkChanges(Action<IEnumerable<int>> strokesLoadedCallback) {
            InkCanvas.ControlStrokesLoaded -= strokesLoadedCallback;
        }

        public void SetInkProperties(Color color, Size size, PenTipShape penTip, bool highlight, bool ignorePressure, bool ignoreTilt) {
            InkCanvas.SetColor(color);
            InkCanvas.SetPenSize(size);
            InkCanvas.SetPenTip(penTip);
            InkCanvas.SetHighlighter(highlight);
            InkCanvas.SetIgnorePressure(ignorePressure);
            InkCanvas.SetIgnoreTilt(ignoreTilt);
        }

        public void StartSelection(RectangleD rectangle) {
            if (_selectionComponent != null) return;
            _selectionComponent = new SelectionComponent();
            _selectionComponent.SetBounds(rectangle);
            _selectionComponent.SetInkCanvas(InkCanvas);
            _selectionComponent.SetChildren(App.EditorScreen.SelectedContent.ToList());
            AbsoluteOverlay.Children.Add(App.EditorScreen._selectionComponent);
            _selectionComponent.SetTouchHandler();
        }

        public void EndSelection() {
            if (_selectionComponent == null || !AbsoluteOverlay.Children.Contains(App.EditorScreen._selectionComponent)) return;
            Logger.Log("[CustomInkCanvas]", "RemoveSelectionComponent", logLevel: LogLevel.Debug);
            _selectionComponent.RemoveNobs();
            AbsoluteOverlay.Children.Remove(App.EditorScreen._selectionComponent);
            _selectionComponent = null;
            InkCanvas.RemoveHandwritingConversionTools();
        }

        public SelectionComponent GetSelectionComponent() => _selectionComponent;

        public void SetSelectedInkStrokes(int[] inkStrokeIds) {
            InkCanvas.ClearSelection();
            InkCanvas.SelectByIds(inkStrokeIds);
        }

        public CustomInkCanvas GetInkCanvas() => InkCanvas;
    }
}