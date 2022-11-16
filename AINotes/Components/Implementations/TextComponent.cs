using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using AINotes.Components.Tools;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.UserActions;
using Helpers.Essentials;
using Helpers.Extensions;
using AINotes.Models;
using Helpers;
using Application = Windows.UI.Xaml.Application;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;
using Grid = Windows.UI.Xaml.Controls.Grid;
using Size = Windows.Foundation.Size;
using Style = Windows.UI.Xaml.Style;
using Thickness = Windows.UI.Xaml.Thickness;


namespace AINotes.Components.Implementations {
    public struct RichEditorFormatting {
        public float FontSize { get; set; }
        public FontFamily FontFamily { get; set; }
        public Color FontColor { get; set; }
        public TextAlignment TextAlignment { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }

        public RichEditorFormatting(RichEditorFormatting formatting) {
            FontSize = formatting.FontSize;
            FontFamily = formatting.FontFamily;
            FontColor = formatting.FontColor;
            TextAlignment = formatting.TextAlignment;
            Bold = formatting.Bold;
            Italic = formatting.Italic;
            Underline = formatting.Italic;
        }
    }
    
    public class CustomRichEditor : RichEditBox {
        // events
        public event Action<RichEditorFormatting> FormattingChanged;
        
        // native TextBoxView
        public FrameworkElement TextBoxView;

        // private state
        private Size _lastMeasure = Size.Empty;
        private bool _dropdownProtection;
        private bool _resizingListenersSet;
        
        // public state
        private RichEditorFormatting _currentFormatting;
        public RichEditorFormatting CurrentFormatting {
            get => new RichEditorFormatting(_currentFormatting);
            set {
                if (Equals(_currentFormatting, value)) {
                    return;
                }

                ApplyFormatting(value);
                _currentFormatting = value;
                FormattingChanged?.Invoke(value);
            }
        }

        public string TextString {
            get {
                Document.GetText(TextGetOptions.None, out var txt);
                return txt;
            }
            set => Document.SetText(TextSetOptions.None, value);
        }

        public string FormattedTextString {
            get {
                TextDocument.GetText(TextGetOptions.FormatRtf, out var txt);
                return txt;
            }
            set {
                Document.SetText(TextSetOptions.FormatRtf, value);

                // remove line break at the end
                Document.GetText(TextGetOptions.None, out var txt);
                Document.GetRange(txt.Length - 2, txt.Length).Text = "";
            }
        }

        public CustomRichEditor() {
            // config
            AcceptsReturn = true;
            // TODO: Use `TextWrapping.Wrap; MaxWidth = <SetWidth>;` for text wrap / max width
            TextWrapping = TextWrapping.NoWrap;
            HorizontalContentAlignment = HorizontalAlignment.Left;
            BorderThickness = new Thickness(0);
            IsSpellCheckEnabled = false;
            Style = (Style) Application.Current.Resources["CustomRichEditorStyle"];

            // events
            TextChanged += OnTextChanged;
            SelectionChanged += OnSelectionChanged;
            ContextMenuOpening += OnContextMenuOpening;
            BringIntoViewRequested += OnBringIntoViewRequested;
            Paste += OnPaste;
            GettingFocus += OnGettingFocus;
            LosingFocus += OnLosingFocus;

            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
        }

        private void OnGotFocus(object sender, RoutedEventArgs args) {
            if (Preferences.FocusDebugModeEnabled) Foreground = Colors.Red.ToBrush();
        }

        private void OnLostFocus(object sender, RoutedEventArgs args) {
            if (Preferences.FocusDebugModeEnabled) Foreground = Colors.Black.ToBrush();
        }

        public void ApplyFormatting(RichEditorFormatting formatting) {
            var selectedText = Document?.Selection;
            if (selectedText == null) return;
            var charFormatting = selectedText.CharacterFormat;
            charFormatting.Size = formatting.FontSize;
            FontFamily = formatting.FontFamily;
            charFormatting.ForegroundColor = formatting.FontColor;
            charFormatting.Bold = formatting.Bold ? FormatEffect.On : FormatEffect.Off;
            charFormatting.Italic = formatting.Italic ? FormatEffect.On : FormatEffect.Off;
            charFormatting.Underline = formatting.Underline ? UnderlineType.Single : UnderlineType.None;
        }

        public void SetListMode(bool b) {
            var selected = Document?.Selection;
            if (selected == null) return;
            if (b) {
                selected.ParagraphFormat.ListType = MarkerType.Bullet;
                selected.ParagraphFormat.ListStyle = MarkerStyle.Plain;
            } else {
                selected.ParagraphFormat.ListType = MarkerType.None;
            }
        }

        private void OnGettingFocus(UIElement sender, GettingFocusEventArgs args) {
            // cancel in selection mode
            if (App.EditorScreen?.Selecting ?? false) {
                args.Cancel = true;
                return;
            }
            
            // cancel if text component tbi is not selected
            if (App.EditorScreen?.SelectedToolbarItem?.GetType() != typeof(TextComponentTool)) {
                args.Cancel = true;
                return;
            }

            Logger.Log("[CustomRichEditor]", "GettingFocus from", args.OldFocusedElement, "|", args.Direction, "|", args.FocusState);
        }

        private void OnLosingFocus(UIElement sender, LosingFocusEventArgs args) {
            try {
                if (TextComponentTool.DisableTextComponentRefocusToScroller && args.NewFocusedElement is ScrollViewer) {
                    Logger.Log("[CustomRichEditor]", $"Blocking loss of focus to ScrollViewer while DisableTextComponentRefocusToScroller is set ({args.NewFocusedElement})", logLevel: LogLevel.Debug);
                    args.Cancel = true;
                    return;
                }
                
                if (App.EditorScreen.DoNotRefocus.Contains(args.NewFocusedElement)) {
                    Logger.Log("[CustomRichEditor]", $"Blocking loss of focus to Element in EditorScreen.DoNotFocus ({args.NewFocusedElement})", logLevel: LogLevel.Debug);
                    args.Cancel = true;
                    return;
                }

                if (args.NewFocusedElement is ComboBoxItem) {
                    Logger.Log("[CustomRichEditor]", $"Blocking loss of focus to ComboBoxItem ({args.NewFocusedElement})", logLevel: LogLevel.Debug);
                    args.Cancel = true;
                    return;
                }

                Logger.Log("[CustomRichEditor]", $"Losing focus {args.OldFocusedElement} => {args.NewFocusedElement} | {args.Direction} | {args.CorrelationId} | {args.FocusState}", logLevel: LogLevel.Debug);
            } catch (Exception ex) when (ex is InvalidCastException || ex is ArgumentException) {
                Logger.Log("[CustomRichEditor]", ex.ToString(), logLevel: LogLevel.Error);
            }
        }

        public void SetFormattedText(string txt) => FormattedTextString = txt;
        public void SetUnformattedText(string txt) => TextString = txt;

        // prevent scrolling to top
        private void OnBringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args) {
            args.Handled = true;
        }

        // filter clipboard for images
        private void OnPaste(object sender, TextControlPasteEventArgs args) {
            var content = Clipboard.GetContent();
            Logger.Log("[CustomRichEditor]", "OnPaste", content.AvailableFormats.ToFString());
            if (!content.Contains(StandardDataFormats.Text) && !content.Contains(StandardDataFormats.Rtf)) {
                Logger.Log("[CustomRichEditor]", "Ignoring Paste", Clipboard.GetContent().AvailableFormats.ToFString());
                args.Handled = true;
            }

            Logger.Log("[CustomRichEditor]", "Pasting Text");
        }

        // measure spaces
        protected override Size MeasureOverride(Size availableSize) {
            // base value
            var returnValue = base.MeasureOverride(availableSize);

            // add size for spaces from current line end
            var retPlus = 30.0;
            if (Document.Selection.StartPosition > 0 && Document.Selection.Length == 0) {
                var itr = 1;
                bool addSpaces;
                double spaceWidth;
                while (true) {
                    var lastCharacterRange = Document.GetRange(Document.Selection.StartPosition - itr, Document.Selection.EndPosition);
                    var lastCharacters = lastCharacterRange.Text.ToCharArray();
                    if (lastCharacters.All(c => c == ' ')) {
                        itr += 1;
                        continue;
                    }

                    lastCharacterRange.GetRect(PointOptions.NoHorizontalScroll, out var rect, out _);
                    addSpaces = rect.X + rect.Width > returnValue.Width;
                    spaceWidth = rect.Width;
                    break;
                }

                if (addSpaces) {
                    retPlus += spaceWidth;
                }
            }

            returnValue.Width += retPlus;

            // update _lastMeasure and return
            _lastMeasure = returnValue;
            return returnValue;
        }

        // first measure
        protected override Size ArrangeOverride(Size finalSize) {
            try {
                return base.ArrangeOverride(_lastMeasure == Size.Empty ? MeasureOverride(finalSize) : _lastMeasure);
            } catch (Exception ex) {
                Logger.Log("[CustomRichEditor]", "Error in ArrangeOverride:", ex.ToString(), logLevel: LogLevel.Error);
                SentryHelper.CaptureCaughtException(ex);
                return Size.Empty;
            }
        }

        private void CloseDropdown(bool ignoreCheck = false) {
            if (!ignoreCheck) {
                if (_dropdownProtection) {
                    _dropdownProtection = false;
                    return;
                }
            }

            CustomDropdown.CloseDropdown();
        }

        public event Action<Point> OpenContextMenu;
        private void OnControlOpenContextMenu(Point p) => OpenContextMenu?.Invoke(p);

        // prevent native context menu
        private void OnContextMenuOpening(object sender, ContextMenuEventArgs args) {
            if (ContextFlyout == null) {
                _dropdownProtection = true;
                OnControlOpenContextMenu(new Point((float) args.CursorLeft, (float) args.CursorTop));
                args.Handled = true;
                return;
            }

            EventHandler<object> contextFlyoutListener = null;
            ContextFlyout.Opening += contextFlyoutListener = (o, _) => {
                var flyout = (TextCommandBarFlyout) o;
                flyout.Hide();
                args.Handled = true;
                OnControlOpenContextMenu(new Point((float) args.CursorLeft, (float) args.CursorTop));
                _dropdownProtection = true;
                ContextFlyout.Opening -= contextFlyoutListener;
            };

            EventHandler<object> selectionFlyoutListener = null;
            SelectionFlyout.Opening += selectionFlyoutListener = (o, _) => {
                var flyout = (TextCommandBarFlyout) o;
                flyout.Hide();
                args.Handled = true;
                CloseDropdown(true);
                ContextFlyout.Opening -= selectionFlyoutListener;
            };

            EventHandler<object> menuFlyoutListener = null;
            ProofingMenuFlyout.Opening += menuFlyoutListener = (o, _) => {
                var flyout = (TextCommandBarFlyout) o;
                flyout.Hide();
                args.Handled = true;
                CloseDropdown(true);
                ContextFlyout.Opening -= menuFlyoutListener;
            };
        }

        private RichEditorFormatting GetCurrentFormatting(ITextCharacterFormat charFormatting) {
            return new RichEditorFormatting {
                FontSize = charFormatting.Size,
                FontFamily = FontFamily,
                FontColor = charFormatting.ForegroundColor,
                TextAlignment = TextAlignment.Start,
                Bold = charFormatting.Bold == FormatEffect.On,
                Italic = charFormatting.Italic == FormatEffect.On,
                Underline = charFormatting.Underline == UnderlineType.Single,
            };
        }

        private void OnSelectionChanged(object sender, RoutedEventArgs routedEventArgs) {
            var selectedText = Document?.Selection;
            if (selectedText == null) return;
            var charFormatting = selectedText.CharacterFormat;

            CurrentFormatting = GetCurrentFormatting(charFormatting);
            CloseDropdown();
        }

        private void OnTextChanged(object s, RoutedEventArgs args) {
            InvalidateMeasure();
            CloseDropdown();

            if (_resizingListenersSet) return;
            try {
                // find the TextBoxView
                foreach (var child in ((Grid) this.ListChildren(false)[0]).ListChildren(false)) {
                    if (child is StackPanel c) {
                        TextBoxView = (FrameworkElement) c.ListChildren(false)[0];
                    }
                }

                // subscribe to events
                TextBoxView.SizeChanged += (_, _) => UpdateSizeOnSizeChanged();
                TextChanged += (_, _) => UpdateSizeOnTextChanged();

                // only once
                _resizingListenersSet = true;
            } catch (Exception) {
                /* ignored */
            }
        }

        private void UpdateSizeOnTextChanged() {
            Width = Math.Max(DesiredSize.Width, TextBoxView.Width);
        }

        private void UpdateSizeOnSizeChanged() {
            Width = Math.Max(DesiredSize.Width, TextBoxView.ActualWidth);
            Height = TextBoxView.ActualHeight;
        }
    }
    
    public class TextComponent : Component {
        public readonly CustomRichEditor Content = new CustomRichEditor();
        
        private bool _initialized;
        private bool _setDataProtection;

        public static TextComponent LastSelectedTextComponent;

        public TextComponent(ComponentModel componentModel) : base(componentModel) {
            Logger.Log("[TextComponent]", "-> TextComponent()");
            Movable = ResizeableToRight = true;
            Resizeable = false;
            
            MinWidth = 80;
            MinHeight = 25;
            
            Children.Add(Content);

            Content.TextChanged += OnTextChanged;
            
            Content.OpenContextMenu += position => OpenContextMenu(new Point(position.X, position.Y));
            
            Content.LostFocus += OnLostFocus;
            Content.GotFocus += OnGotFocus;

            Content.KeyUp += OnKeyUp;
            Logger.Log("[TextComponent]", "<- TextComponent()");
        }

        private UserAction _createUserAction;
        private string _lastUndoText;
        
        private void OnKeyUp(object s, KeyRoutedEventArgs e) {
            if (!_initialized) return;
            if(!ShouldDeleteSelf()) _createUserAction ??= UserActionManager.OnComponentAdded(this);
            
            if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter) {
                var newFormattedString = Content.FormattedTextString;
                UserActionManager.OnTextChanged(this, _lastUndoText, newFormattedString);
                _lastUndoText = newFormattedString;
            }
            
            if (_setDataProtection) {
                _setDataProtection = false;
                Logger.Log("[TextComponent]", $"{nameof(OnKeyUp)} - {nameof(_setDataProtection)}: return");
                return;
            }

            App.EditorScreen.InvokeComponentChanged(this);
        }

        private void OnTextChanged(object sender, RoutedEventArgs args) {
            Logger.Log("[TextComponent]", "-> OnTextChanged");
            SetWidth(Content.ActualWidth);
            SetHeight(Content.ActualHeight);
            SetContent(Content.FormattedTextString);

            if (!ShouldDeleteSelf() || _createUserAction == null || UserActionManager.ActionStackCount() == 0) return;
            Logger.Log("[TextComponent]", $"OnTextChanged: Remove {nameof(_createUserAction)} from ActionStack", UserActionManager.ActionStackContains(_createUserAction));
            Logger.Log(UserActionManager.ActionStackCount());
            UserActionManager.RemoveUserAction(_createUserAction);
            Logger.Log(UserActionManager.ActionStackCount());
            _createUserAction = null;
        }

        private void OnLostFocus(object s, RoutedEventArgs e) {
            Logger.Log("[TextComponent]", "-> OnLostFocus");
            IsSelected = false;
        }

        private void OnGotFocus(object s, RoutedEventArgs e) {
            Logger.Log("[TextComponent]", "-> OnGotFocus");
            LastSelectedTextComponent = this;
            IsSelected = true;
            LoadNobs();
        }

        public void Init() {
            if (_initialized) return;
            Logger.Log("[TextComponent]", "-> Init");
            LoadNobs();
            Content.FormattingChanged += formatting => {
                foreach (var view in App.Page.PrimaryToolbarChildren) {
                    if (!(view is ITool toolbarItem)) continue;
                    if (!(toolbarItem is TextComponentTool textComponentToolbarItem)) continue;
                    textComponentToolbarItem.UpdateSecondaryToolbar(formatting);
                }
            };

            Deselected += async (_, _) => {
                if (!ShouldDeleteSelf()) return;
                CreateUserAction = false;
                SetDeleted(true);
                await DeleteFromDatabase();
            };

            _initialized = true;
            Logger.Log("[TextComponent]", "<- Init");
        }

        public bool ShouldDeleteSelf() {
            return string.IsNullOrWhiteSpace(Content.FormattedTextString) || string.IsNullOrWhiteSpace(Content.TextString);
        }

        protected override FrameworkElement GetFocusTarget() => Content;
        
        protected override void Focus() {
            Logger.Log("[TextComponent]", "-> Focus");
            MainThread.BeginInvokeOnMainThread(() => Content.Focus(FocusState.Programmatic));
            Logger.Log("[TextComponent]", "<- Focus", Content.FocusState);
        }

        public override void Unfocus() {
            Logger.Log("[TextComponent]", "-> Unfocus");
            var isTabStop = Content.IsTabStop;
            Content.IsTabStop = false;
            Content.IsEnabled = false;
            Content.IsEnabled = true;
            Content.IsTabStop = isTabStop;
            Logger.Log("[TextComponent]", "<- Unfocus");
        }

        public override void Copy() {
            base.Copy();
            
            var dp = new DataPackage {
                RequestedOperation = DataPackageOperation.Copy,
            };
            
            dp.SetRtf(Content.FormattedTextString);
            
            Clipboard.SetContent(dp);
        }

        public override void Cut() {
            base.Cut();
            
            var dp = new DataPackage {
                RequestedOperation = DataPackageOperation.Copy
            };
            
            dp.SetRtf(Content.FormattedTextString);
            
            Clipboard.SetContent(dp);
        }

        protected override void OnContentChanged(string content) {
            base.OnContentChanged(content);

            Logger.Log($"[{nameof(TextComponent)}]", $"{nameof(OnContentChanged)} - content changed to {content}");
            if (_lastUndoText == null && content != null) _lastUndoText = content;
            
            if (content == Content.FormattedTextString || content == null) return;
            Content.SetFormattedText(content);
        }
    }
}