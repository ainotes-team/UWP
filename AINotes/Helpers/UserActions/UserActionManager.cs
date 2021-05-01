using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Components;
using AINotes.Components.Implementations;
using Helpers;
using Helpers.Essentials;
using Helpers.Lists;
using MaterialComponents;

namespace AINotes.Helpers.UserActions {
    public static class UserActionManager {
        private static readonly ExtendedStack<UserAction> ActionStack = new ExtendedStack<UserAction>();
        private static readonly ExtendedStack<UserAction> UndidActionStack = new ExtendedStack<UserAction>();
        
        public static readonly MDToolbarItem RedoToolbarItem;
        public static readonly MDToolbarItem UndoToolbarItem;

        static UserActionManager() {
            RedoToolbarItem = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(Icon.Redo)),
            };

            UndoToolbarItem = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(Icon.Undo)),
            };
            
            RedoToolbarItem.Pressed += OnRedoTBIPressed;
            UndoToolbarItem.Pressed += OnUndoTBIPressed;
        }

        private static void OnRedoTBIPressed(object s, EventArgs e) => Redo();

        private static void OnUndoTBIPressed(object s, EventArgs e) => Undo();

        public static int ActionStackCount() => ActionStack.Count;
        public static bool ActionStackContains(UserAction action) => ActionStack.Contains(action);

        public static void ClearStacks() {
            ActionStack.Clear();
            UndidActionStack.Clear();
        }

        public static void Undo() {
            if (ActionStack.Count == 0) return;
            var lastAction = ActionStack.Pop();
            lastAction.Undo();
            UndidActionStack.Push(lastAction);
            App.EditorScreen.EndSelection();
            RedoToolbarItem.IsEnabled = true;
            if (ActionStack.Count != 0) return;
            UndoToolbarItem.IsEnabled = false;
        }

        public static void Redo() {
            if (UndidActionStack.Count == 0) return;
            var lastAction = UndidActionStack.Pop();
            lastAction.Redo();
            ActionStack.Push(lastAction);
            App.EditorScreen.EndSelection();
            UndoToolbarItem.IsEnabled = true;
            if (UndidActionStack.Count != 0) return;
            RedoToolbarItem.IsEnabled = false;
        }
        
        public static void AddUserAction(UserAction userAction) {
            MainThread.BeginInvokeOnMainThread(() => {
                RedoToolbarItem.IsEnabled = false;
                UndoToolbarItem.IsEnabled = true;
                UndidActionStack.Clear();
                ActionStack.Push(userAction);
            });
        }
        
        public static void RemoveUserAction(UserAction userAction) {
            ActionStack.Remove(userAction);
            if (ActionStack.Count != 0) return;
            MainThread.BeginInvokeOnMainThread(() => {
                UndoToolbarItem.IsEnabled = false;
            });
        }

        public static void ClearRedoActionStack() {
            RedoToolbarItem.IsEnabled = false;
            UndidActionStack.Clear();
        }

        public static UserAction OnComponentAdded(Component addedComponent) {
            Logger.Log($"[{nameof(UserActionManager)}]", $"{nameof(OnComponentAdded)}");
            var userAction = new UserAction(objects => {
                var component = (Component) objects["component"];

                component.CreateUserAction = false;
                component.SetDeleted(true);
                component.CreateUserAction = true;
            }, objects => {
                var component = (Component) objects["component"];

                component.CreateUserAction = false;
                component.SetDeleted(false);
                component.CreateUserAction = true;
            }, new Dictionary<string, object> {
                {"component", addedComponent}
            });
            AddUserAction(userAction);
            return userAction;
        }

        public static void OnComponentDeleted(Component removedComponent) {
            Logger.Log($"[{nameof(UserActionManager)}]", $"{nameof(OnComponentDeleted)}");
            AddUserAction(new UserAction(objects => {
                var component = (Component) objects["component"];
                
                component.CreateUserAction = false;
                component.SetDeleted(false);
                component.CreateUserAction = true;
            }, objects => {
                var component = (Component) objects["component"];
                
                component.CreateUserAction = false;
                component.SetDeleted(true);
                component.CreateUserAction = true;
            }, new Dictionary<string, object> {
                {"component", removedComponent}
            }));
        }

        public static void OnComponentMoved(RectangleD from, Component movedComponent) {
            Logger.Log($"[{nameof(UserActionManager)}]", $"{nameof(OnComponentMoved)}");
            AddUserAction(new UserAction(objects => {
                var oldBounds = (RectangleD) objects["oldBounds"];
                var component = (Component) objects["component"];
                
                component.CreateUserAction = false;
                component.SetBounds(oldBounds);
                component.RepositionNobs();
                component.CreateUserAction = true;
            }, objects => {
                var newBounds = (RectangleD) objects["newBounds"];
                var component = (Component) objects["component"];
                
                component.CreateUserAction = false;
                component.SetBounds(newBounds);
                component.RepositionNobs();
                component.CreateUserAction = true;
            }, new Dictionary<string, object> {
                {"oldBounds", from},
                {"newBounds", movedComponent.GetBounds()},
                {"component", movedComponent}
            }));
        }
        
        public static void OnComponentsAdded(ImageComponent[] addedComponents) {
            Logger.Log($"[{nameof(UserActionManager)}]", $"{nameof(OnComponentsAdded)}");
            AddUserAction(new UserAction(objects => {
                var components = (Component[]) objects["components"];

                foreach (var component in components) {
                    component.CreateUserAction = false;
                    component.SetDeleted(true);
                    component.CreateUserAction = true;
                }
            }, objects => {
                var components = (Component[]) objects["components"];


                foreach (var component in components) {
                    component.CreateUserAction = false;
                    component.SetDeleted(false);
                    component.CreateUserAction = true;
                }
            }, new Dictionary<string, object> {
                {"components", addedComponents}
            }));
        }
        
        public static void OnComponentsStrokesDeleted(Component[] deletedComponents, UserAction deletedStrokesUserAction = null) {
            Logger.Log($"[{nameof(UserActionManager)}]", $"{nameof(OnComponentsStrokesDeleted)}");
            AddUserAction(new UserAction(objects => {
                var components = (Component[]) objects["components"];
                var strokesUserAction = (UserAction) objects["strokesUserAction"];
                
                foreach (var component in components) {
                    component.CreateUserAction = false;
                    component.SetDeleted(false);
                    component.CreateUserAction = true;
                }
                
                strokesUserAction?.Undo();
            }, objects => {
                var components = (Component[]) objects["components"];
                var strokesUserAction = (UserAction) objects["strokesUserAction"];
                
                foreach (var component in components) {
                    component.CreateUserAction = false;
                    component.SetDeleted(true);
                    component.CreateUserAction = true;
                }
                
                strokesUserAction?.Redo();
            }, new Dictionary<string, object> {
                {"components", deletedComponents},
                {"strokesUserAction", deletedStrokesUserAction}
            }));
        }
        
        public static void OnComponentsStrokesBoundsChanged(RectangleD from, RectangleD to, Component[] deletedComponents, int[] inkStrokeIds) {
            Logger.Log($"[{nameof(UserActionManager)}]", $"{nameof(OnComponentsStrokesBoundsChanged)} with oldBounds: {from.ToString()} | newBounds: " +
                                                         $"{to.ToString()} | {deletedComponents.Length} components | {inkStrokeIds.Length} inkStrokes");
            AddUserAction(new UserAction(objects => {
                var components = (Component[]) objects["components"];
                var inkStrokes = (int[]) objects["inkStrokeIds"];
                
                var nBounds = (RectangleD) objects["newBounds"];
                var oBounds = (RectangleD) objects["oldBounds"];
                
                var (x, y, width, height) = oBounds;
                var xOffset = x - nBounds.X;
                var yOffset = y - nBounds.Y;
                var xFactor = width / nBounds.Width;
                var yFactor = height / nBounds.Height;

                App.EditorScreen.GetInkCanvas().OnSelectionComponentMoving(nBounds, oBounds, inkStrokes.ToList());
                App.EditorScreen.GetInkCanvas().OnSelectionComponentResizing(nBounds, oBounds, inkStrokes.ToList());

                foreach (var c in components) {
                    c.CreateUserAction = false;
                    c.SetBounds(new RectangleD((c.GetX() - nBounds.X + xOffset) * xFactor + nBounds.X, (c.GetY() - nBounds.Y + yOffset) * yFactor + nBounds.Y, 
                        c.Width * xFactor, c.Height * yFactor), true);
                    c.CreateUserAction = true;
                    c.RepositionNobs();
                    App.EditorScreen.InvokeComponentChanged(c);
                }
                
                App.EditorScreen.EndSelection();
            }, objects => {
                var components = (Component[]) objects["components"];
                var inkStrokes = (int[]) objects["inkStrokeIds"];
                
                var oBounds = (RectangleD) objects["newBounds"];
                var nBounds = (RectangleD) objects["oldBounds"];
                
                var (x, y, width, height) = oBounds;
                var xOffset = x - nBounds.X;
                var yOffset = y - nBounds.Y;
                var xFactor = width / nBounds.Width;
                var yFactor = height / nBounds.Height;

                App.EditorScreen.GetInkCanvas().OnSelectionComponentMoving(nBounds, oBounds, inkStrokes.ToList());
                App.EditorScreen.GetInkCanvas().OnSelectionComponentResizing(nBounds, oBounds, inkStrokes.ToList());

                foreach (var c in components) {
                    c.CreateUserAction = false;
                    c.SetBounds(new RectangleD((c.GetX() - nBounds.X + xOffset) * xFactor + nBounds.X, (c.GetY() - nBounds.Y + yOffset) * yFactor + nBounds.Y, 
                        c.Width * xFactor, c.Height * yFactor), true);
                    c.CreateUserAction = true;
                    c.RepositionNobs();
                    App.EditorScreen.InvokeComponentChanged(c);
                }
                
                App.EditorScreen.EndSelection();
            }, new Dictionary<string, object> {
                {"components", deletedComponents},
                {"inkStrokeIds", inkStrokeIds},
                {"oldBounds", from},
                {"newBounds", to}
            }));
        }
        
        public static void OnTextChanged(TextComponent textComponent, string oldTextFormatted, string newTextFormatted) {
            Logger.Log($"[{nameof(UserActionManager)}]", $"{nameof(OnTextChanged)}");
            AddUserAction(new UserAction(objects => {
                var component = (TextComponent) objects["textComponent"];
                var oldText = (string) objects["oldTextFormatted"];
                
                App.EditorScreen.EndSelection();
               
                component.CreateUserAction = false;
                component.SetContent(oldText);
                component.CreateUserAction = true;
            }, objects => {
                var component = (TextComponent) objects["textComponent"];
                var newText = (string) objects["newTextFormatted"];
                
                App.EditorScreen.EndSelection();
               
                component.CreateUserAction = false;
                component.SetContent(newText);
                component.CreateUserAction = true;
            }, new Dictionary<string, object> {
                {"textComponent", textComponent},
                {"oldTextFormatted", oldTextFormatted},
                {"newTextFormatted", newTextFormatted},
            }));
        }

        public static void OnTextConverted(TextComponent textComponent, UserAction inkStrokeUndo) {
            Logger.Log($"[{nameof(UserActionManager)}]", $"{nameof(OnTextConverted)}");
            AddUserAction(new UserAction(objects => {
                var component = (TextComponent) objects["textComponent"];
                var convertedStrokesUserAction = (UserAction) objects["inkStrokeUndo"];
                
                App.EditorScreen.EndSelection();
                
                component.CreateUserAction = false;
                component.SetDeleted(true);
                component.CreateUserAction = true;
                
                convertedStrokesUserAction.Undo();
            }, objects => {
                var component = (TextComponent) objects["textComponent"];
                var convertedStrokesUserAction = (UserAction) objects["inkStrokeUndo"];
                
                App.EditorScreen.EndSelection();
               
                component.CreateUserAction = false;
                component.SetDeleted(false);
                component.CreateUserAction = true;
                
                convertedStrokesUserAction.Redo();
            }, new Dictionary<string, object> {
                {"inkStrokeUndo", inkStrokeUndo},
                {"textComponent", textComponent},
            }));
        }
    }
}