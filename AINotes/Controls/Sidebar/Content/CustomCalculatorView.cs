using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class CustomCalculatorView : StackPanel, ISidebarView {
        public IEnumerable<MDToolbarItem> ExtraButtons { get; } = new MDToolbarItem[] { };
        
        public CustomCalculatorView() {
            var txt = new TextBlock {
                Text = "TODO"
            };
            
            Children.Add(txt);
            
            CreateInterface();
            _mathStringHistory.Push("");
        }

        private string _currentMathString = "";
        
        private readonly Stack<string> _mathStringHistory = new Stack<string>();

        private const int ButtonHeight = 50;

        private void UpdateMathString(string mathString) {
            _currentMathString = mathString;
        }

        private void CreateInterface() {
            var grid = new Grid {
                RowDefinitions = {
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition()
                },
                ColumnDefinitions = {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star)},
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star)},
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star)},
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star)},
                },
                ChildrenTransitions = new TransitionCollection {
                    new EntranceThemeTransition()
                }
            };
            
            void AddButton(string text, string mathText, int left, int top, int rowSpan = 1, int columnSpan = 1) {
                var button = new MDButton {
                    Text = text,
                    Command = () => {
                        UpdateMathString(_currentMathString + mathText);
                        _mathStringHistory.Push(_currentMathString + mathText);
                    },
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(2),
                    Height = ButtonHeight
                };
                
                Grid.SetRowSpan(button, rowSpan);
                Grid.SetColumnSpan(button, columnSpan);

                grid.Children.Add(button, top, left);
            }
            
            Children.Add(grid);
            
            
            // buttons
            
            // digits
            AddButton("1", "1", 0, 0);
            AddButton("2", "2", 1, 0);
            AddButton("3", "3", 2, 0);
            AddButton("4", "4", 0, 1);
            AddButton("5", "5", 1, 1);
            AddButton("6", "6", 2, 1);
            AddButton("7", "7", 0, 2);
            AddButton("8", "8", 1, 2);
            AddButton("9", "9", 2, 2);
            AddButton("0", "0", 0, 3);
            
            // other
            AddButton(".", ".", 1, 3);
            
            AddButton("÷", "/", 3, 0);
            AddButton("×", "*", 3, 1);
            AddButton("-", "-", 3, 2);
            AddButton("+", "+", 3, 3);
            
            
            // extra buttons
            grid.Children.Add(new MDButton {
                Text = "=",
                Command = Calculate,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(2),
                Height = ButtonHeight
            }, 3, 2);
        }

        private void Calculate() {
            
        }
    }
}