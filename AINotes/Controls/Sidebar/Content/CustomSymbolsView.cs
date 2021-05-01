using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Text;
using AINotes.Components.Implementations;
using Helpers;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using MaterialComponents;
using System;

namespace AINotes.Controls.Sidebar.Content {
    public class CustomSymbolsView : Frame, ISidebarView {
        public IEnumerable<MDToolbarItem> ExtraButtons { get; } = new MDToolbarItem[0];
        
        private readonly Dictionary<string, List<string>> _symbols = new Dictionary<string, List<string>> {
            {
                "symbolsMath", new List<string> {
                    "±", "∞", "=", "≠", "~", "×", "÷", "!", "∝", "<", "≪",
                    ">", "≫", "≤", "≥", "∓", "≅", "≈", "≡", "∀", "∁",
                    "∂", "√", "∛", "∜", "∪", "∩", "∅", "%", "°", "℉", "℃",
                    "∆", "∇", "∃", "∄", "∈", "∋", "←", "↑", "→", "↓", "↔",
                    "∴", "+", "−", "¬", "α", "β", "γ", "δ", "ε", "ϵ", "θ",
                    "ϑ", "μ", "π", "ρ", "σ", "τ", "φ", "ω", "∗", "∙", "⋮",
                    "⋯", "⋰", "⋱", "ℵ", "ℶ", "∎"
                }
            }, {
                "lowerCase", new List<string> {
                    "α", "β", "γ", "δ", "ε", "ϵ", "ζ", "η", "θ", "ϑ", "ι",
                    "κ", "λ", "μ", "ν", "ξ", "ο", "π", "ϖ", "ρ", "ϱ", "σ",
                    "ς", "τ", "υ", "φ", "ϕ", "χ", "ψ", "ω"
                }
            }, {
                "upperCase", new List<string> {
                    "Α", "Β", "Γ", "Δ", "Ε", "Ζ", "Η", "Θ", "Ι", "Κ", "Λ",
                    "Μ", "Ν", "Ξ", "Ο", "Π", "Ρ", "Σ", "Τ", "Υ", "Φ", "Χ",
                    "Ψ", "Ω"
                }
            }, {
                "similarToLetter", new List<string> {
                    "∀", "∁", "ℂ", "∂", "ð", "ℇ", "Ϝ"
                }
            }, {
                "binaryOperators", new List<string> {
                    "+", "−", "÷", "×", "±", "∓", "∝", "∕", "∗", "∘", "∙",
                    "⋅", "∩", "∪", "⊎", "⊓", "⊔", "∧", "∨"
                }
            }, {
                "relationalOperators", new List<string> {
                    "=", "≠", "<", ">", "≤", "≥", "≮", "≰", "≯", "≱", "≡",
                    "∼", "≃", "≈", "≅", "≢", "≄", "≉", "≇", "∝", "≪", "≫",
                    "∈", "∋", "∉", "⊂", "⊃", "⊆", "⊇", "≺", "≻", "≼", "≽",
                    "⊏", "⊐", "⊑", "⊒", "∥", "⊥", "⊢", "⊣", "⋈", "≍"
                }
            }, {
                "fundamentalOperators", new List<string> {
                    "∑", "∫", "∬", "∭", "∮", "∯", "∰", "∱", "∲", "∳",
                    "∏", "∐", "⋂", "⋃", "⋀", "⋁", "⨀", "⨂", "⨁", "⨄",
                    "⨃"
                }
            }, {
                "advancedBinaryOperators", new List<string> {
                    "∔", "∸", "∖", "⋒", "⋓", "⊟", "⊠", "⊡", "⊞", "⋇",
                    "⋉", "⋊", "⋋", "⋌", "⋏", "⋎", "⊝", "⊺", "⊕", "⊖",
                    "⊗", "⊘", "⊙", "⊛", "⊚", "†", "‡", "⋆", "⋄", "≀",
                    "△", "⋀", "⋁", "⨀", "⨂", "⨁", "⨅", "⨆", "⨄", "⨃"
                }
            }, {
                "advancedRelationalOperators", new List<string> {
                    "∴", "∵", "⋘", "⋙", "≦", "≧", "≲", "≳", "⋖", "⋗",
                    "≶", "⋚", "≷", "⋛", "≑", "≒", "≓", "∽", "≊", "⋍",
                    "≼", "≽", "⋞", "⋟", "≾", "≿", "⋜", "⋝", "⊆", "⊇",
                    "⊲", "⊳", "⊴", "⊵", "⊨", "⋐", "⋑", "⊏", "⊐", "⊩",
                    "⊪", "≖", "≗", "≜", "≏", "≎", "∝", "≬", "⋔", "≐",
                    "⋈"
                }
            }, {
                "arrows", new List<string> {
                    "←", "→", "↑", "↓", "↔", "↕", "⇐", "⇒", "⇑", "⇓", "⇔",
                    "⇕", "⟵", "⟶", "⟷", "⟸", "⟹", "⟺", "↗", "↖",
                    "↘", "↙", "↚", "↛", "↮", "⇍", "⇏", "⇎", "⇠", "⇢",
                    "↤", "↦", "⟻", "⟼", "↩", "↪", "↼", "↽", "⇀", "⇁",
                    "↿", "↾", "⇃", "⇂", "⇋", "⇌", "⇇", "⇉", "⇈", "⇊",
                    "⇆", "⇄", "↫", "↬", "↢", "↣", "↰", "↱", "↲", "↳",
                    "⇚", "⇛", "↞", "↠", "↶", "↷", "↺", "↻", "⊸", "↭",
                    "↜", "↝", "⇜", "⇝"
                }
            }, {
                "negativeRelations", new List<string> {
                    "≠", "≮", "≯", "≰", "≱", "≢", "≁", "≄", "≉", "≇", "≭",
                    "≨", "≩", "⊀", "⊁", "⋠", "⋡", "∉", "∌", "⊄", "⊅", "⊈",
                    "⊉", "⊊", "⊋", "⋢", "⋣", "⋦", "⋧", "⋨", "⋩", "⋪", "⋫",
                    "⋬", "⋭", "∤", "∦", "⊬", "⊭", "⊮", "⊯", "∄"
                }
            }, {
                "geometry", new List<string> {
                    "∟", "∠", "∡", "∢", "⊾", "⊿", "⋕", "⊥", "∤", "∥", "∦",
                    ":", "∷", "∴", "∵", "∎"
                }
            },
        };

        private readonly StackPanel _stack;
        private bool _isFirstOverride = true;
        private SymbolsGridView _currentlyOpenedView;

        public CustomSymbolsView() {
            _stack = new StackPanel {
                Transitions = new TransitionCollection {
                    new EntranceThemeTransition()
                }
            };

            Content = new ScrollViewer {
                Content = _stack
            };
        }

        protected override Size ArrangeOverride(Size finalSize) {
            if (!_isFirstOverride) return base.ArrangeOverride(finalSize);
            _isFirstOverride = false;

            foreach (var (symbolGroupName, symbolGroupValues) in _symbols) {
                SymbolsGridView groupGrid = null;

                var arrowRightTbi = new MDToolbarItem {
                    ImageSource = new BitmapImage(new Uri(Icon.ArrowRight)),
                };
                arrowRightTbi.Released += (_, _) => {
                    if (_currentlyOpenedView == groupGrid) {
                        _currentlyOpenedView = null;
                        if (groupGrid != null) groupGrid.ItemsSource = null;
                    } else {
                        if (_currentlyOpenedView != null) _currentlyOpenedView.ItemsSource = null;
                        _currentlyOpenedView = groupGrid;
                        if (groupGrid != null) groupGrid.ItemsSource = symbolGroupValues;
                    }
                };

                _stack.Children.Add(new StackPanel {
                    Orientation = Orientation.Horizontal,
                    Children = {
                        arrowRightTbi,
                        new MDLabel {
                            Text = symbolGroupName,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(6)
                        }
                    }
                });
                _stack.Children.Add(groupGrid = new SymbolsGridView());
            }

            return base.ArrangeOverride(finalSize);
        }
    }

    public class SymbolsGridView : GridView {
        public SymbolsGridView() {
            IsItemClickEnabled = true;
            App.EditorScreen.DoNotRefocus.Add(this);
            ItemClick += (_, args) => {
                var component = TextComponent.LastSelectedTextComponent;

                Logger.Log(component);

                if (component == null) return;

                component.Content.Document.Selection.SetText(TextSetOptions.None, args.ClickedItem.ToString());
                component.Content.Document.Selection.StartPosition += 1;
            };
        }
    }
}