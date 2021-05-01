using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using AINotes.Components.Implementations;
using AINotes.Controls.Input;
using AINotes.Controls.Pages;
using AINotes.Helpers.Imaging;
using AINotes.Models;
using Helpers;
using MaterialComponents;

namespace AINotes.Components.Tools {
    public class TextComponentTool : MDToolbarItem, ITool {
        public bool RequiresDrawingLayer => false;

        private readonly Dictionary<string, MDToolbarItem> _extraToolbarItems = new Dictionary<string, MDToolbarItem>();
        
        private bool _selected;
        private CustomFontSizeInput _fontSizeInput;

        public static bool DisableTextComponentRefocusToScroller;
        
        private bool _mouseWasDown;

        public TextComponentTool() {
            ImageSource = ImageSourceHelper.FromName(Icon.Text);
            Selectable = true;
            Deselectable = false;
            
            App.Page.ContentChanging += OnAppPageContentChanging;
        }

        public void Select() {
            CustomDropdown.CloseDropdown();
            if (!_selected) {
                _selected = true;
                App.Page.SecondaryToolbarChildren.Clear();
                AddExtraToolbarItem(new MDToolbarItem(Icon.Bold, OnFormattingToolbarItemPressed, true), "textBold");
                AddExtraToolbarItem(new MDToolbarItem(Icon.Italic, OnFormattingToolbarItemPressed, true), "textItalic");
                AddExtraToolbarItem(new MDToolbarItem(Icon.Underline, OnFormattingToolbarItemPressed, true), "textUnderline");
                
                AddExtraToolbarView(_fontSizeInput = new CustomFontSizeInput {Width = 70, Margin = new Thickness(6, 6, 6, 6)});
                _fontSizeInput.SelectionChanged += OnFormattingToolbarItemPressed;
                
                AddExtraToolbarItem(new MDToolbarItem(Icon.List, (s, _) => {
                    UpdateFormatting();
                    foreach (var component in App.EditorScreen.SelectedContent) {
                        if (!(component is TextComponent textComponent)) continue;
                        textComponent.Content.SetListMode(((MDToolbarItem) s).IsSelected);
                    }
                }, true), "listMode");
            }
            
            SendPress();
        }

        private void OnFormattingToolbarItemPressed(object _, object __) {
            UpdateFormatting();
        }

        public void Deselect() {
            _selected = false;
            App.Page.SecondaryToolbarChildren.Clear();
            _extraToolbarItems.Clear();
            
            IsSelected = false;
        }

        public void SubscribeToPressedEvents(EventHandler<EventArgs> handler) => Pressed += handler;

        private void OnAppPageContentChanging(CustomPageContent oldContent, CustomPageContent newContent) {
            _mouseWasDown = false;
        }

        private void AddExtraToolbarItem(MDToolbarItem toolbarItem, string extraTBIName, bool doRefocus = false) {
            _extraToolbarItems.Add(extraTBIName, toolbarItem);
            toolbarItem.PointerEntered += (_, _) => DisableTextComponentRefocusToScroller = true;
            toolbarItem.PointerExited += (_, _) => DisableTextComponentRefocusToScroller = false;
            App.EditorScreen.AddExtraToolbarItem(toolbarItem, doRefocus);
        }

        private void AddExtraToolbarView(UIElement toolbarView, bool doRefocus = false) {
            App.Page.SecondaryToolbarChildren.Add(toolbarView);
            if (!doRefocus) App.EditorScreen.DoNotRefocus.Add(toolbarView);
        }
        
        public void UpdateSecondaryToolbar(RichEditorFormatting formatting) {
            try {
                var boldTBI = _extraToolbarItems["textBold"];
                var italicTBI = _extraToolbarItems["textItalic"];
                var underlineTBI = _extraToolbarItems["textUnderline"];
                
                if (boldTBI != null) boldTBI.IsSelected = formatting.Bold;
                if (italicTBI != null) italicTBI.IsSelected = formatting.Italic;
                if (underlineTBI != null) underlineTBI.IsSelected = formatting.Underline;

                _fontSizeInput.SelectedItem = formatting.FontSize;
            } catch (Exception e) {
                Logger.Log("[TextComponentToolbarItem]", "Error in UpdateSecondaryToolbar:", e.Message, logLevel: LogLevel.Error);
            }
        }

        private void UpdateFormatting() {
            Logger.Log("UpdateFormatting", App.EditorScreen.SelectedContent.Count);
            foreach (var component in App.EditorScreen.SelectedContent) {
                Logger.Log("UpdateFormatting:", component);
                if (!(component is TextComponent textComponent)) continue;
                
                // get the current formatting
                var newFormatting = textComponent.Content.CurrentFormatting;
                
                // font size
                newFormatting.FontSize = _fontSizeInput.Value;
                Logger.Log("_fontSizeInput.Value", _fontSizeInput.Value);

                // bold
                newFormatting.Bold = _extraToolbarItems["textBold"].IsSelected;
                
                // italic
                newFormatting.Italic = _extraToolbarItems["textItalic"].IsSelected;
                
                // underline
                newFormatting.Underline = _extraToolbarItems["textUnderline"].IsSelected;
                
                // update
                textComponent.Content.CurrentFormatting = newFormatting;
            }
        }
        public void OnDocumentClicked(WTouchEventArgs touchEventArgs, ComponentModel componentModel) {
            if (touchEventArgs.ActionType == WTouchAction.Pressed) {
                _mouseWasDown = true;
                return;
            }

            if (touchEventArgs.ActionType != WTouchAction.Released) {
                touchEventArgs.Handled = true;
                return;
            }

            if (!App.EditorScreen.FileContentLoaded) {
                touchEventArgs.Handled = true;
                return;
            }

            if (!_mouseWasDown) return;
            
            Logger.Log("[TextComponentToolbarItem]", "OnDocumentClicked");

            // set model parameters accordingly
            componentModel.Type = "TextComponent";
            componentModel.Content = null;
            componentModel.RemoteId = null;
            componentModel.SizeX = 37;
            componentModel.SizeY = 40;
            
            var textComponent = new TextComponent(componentModel);
            
            App.EditorScreen.AddContentComponent(textComponent);

            textComponent.Init();
            textComponent.Select();
        }
    }
}