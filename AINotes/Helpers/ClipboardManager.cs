using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using AINotes.Components.Implementations;
using AINotes.Components.Tools;
using AINotes.Controls.Pages;
using AINotes.Models;
using Helpers;
using Helpers.Extensions;

namespace AINotes.Helpers {
    public static class ClipboardManager {
        public static Point SelectionPosition;
        public static readonly List<ComponentModel> TemporaryClipboard = new List<ComponentModel>();
        
        public static async void Paste(Point position) {
            Logger.Log("[ClipboardManager]", "-> Paste:", position.X, "|", position.Y);
            CustomDropdown.CloseDropdown();

            ClipboardContent clipboardContent;
            try {
                clipboardContent = await Clipboard.GetContent();
            } catch (Exception ex) {
                Logger.Log("[ClipboardManager]", "-> Paste: Exception on GetContent:", ex.ToString(), logLevel: LogLevel.Error);
                return;
            }

            switch (clipboardContent.ContentType) {
                case ClipboardContentType.Ink:
                    foreach (var componentModel in TemporaryClipboard) {
                        var (x, y) = position;
                        var model = new ComponentModel {
                            ComponentId = -1,
                            FileId = componentModel.FileId,
                            Type = componentModel.Type,
                            Content = componentModel.Content,
                            Position = (componentModel.Position.Item1 - SelectionPosition.X + x, componentModel.Position.Item2 - SelectionPosition.Y + y),
                            Size = componentModel.Size,
                            Deleted = false,
                            RemoteId = null,
                            ZIndex = componentModel.ZIndex
                        };

                        await model.SaveAsync();
                        
                        App.EditorScreen.AddContentComponent(model.ToComponent());
                    }

                    App.EditorScreen.PasteStrokes(position);
                    return;
                case ClipboardContentType.Image: {
                    Logger.Log("[EditorScreen]", "PasteShortcut: ClipboardContentType.Image");
                    var imageBytes = (byte[]) clipboardContent.Content;
            
                    var model = new ComponentModel {
                        Type = "ImageComponent",
                        Content = null,
                        RemoteId = null,
                        PosX = position.X,
                        PosY = position.Y,
                        SizeX = 50,
                        SizeY = 50,
                        FileId = App.EditorScreen.FileId,
                        Deleted = false,
                        ZIndex = -1,
                        ComponentId = -1,
                    };

                    await model.SaveAsync();
                    
                    var imageComponent = new ImageComponent(model);

                    var imageSavingPath = imageComponent.GetImageSavingPath();
                    await LocalFileHelper.WriteFileAsync(imageSavingPath, imageBytes);
                        
                    imageComponent.SetContent(imageSavingPath);
                    ImageComponentTool.AddDocumentComponents(imageComponent);
                    break;
                }
                case ClipboardContentType.ImageFile: {
                    Logger.Log("[EditorScreen]", "ClipboardContentType.ImageFile");
                    
                    var imageComponents = new List<ImageComponent>();

                    var sFs = (List<StorageFile>) clipboardContent.Content;
                    foreach (var sF in sFs) {
                        var model = new ComponentModel {
                            Type = "ImageComponent",
                            Content = null,
                            RemoteId = null,
                            PosX = position.X,
                            PosY = position.Y,
                            SizeX = 50,
                            SizeY = 50,
                            FileId = App.EditorScreen.FileId,
                            Deleted = false,
                            ZIndex = -1,
                            ComponentId = -1,
                        };
                        
                        await model.SaveAsync();
                        
                        var newImageComponent = new ImageComponent(model);

                        var savingPath = newImageComponent.GetImageSavingPath();
                        await LocalFileHelper.WriteFileAsync(savingPath, (await FileIO.ReadBufferAsync(sF)).ToArray());
                        
                        newImageComponent.SetContent(savingPath);
                        imageComponents.Add(newImageComponent);
                    }
                    
                    ImageComponentTool.AddDocumentComponents(imageComponents, position);
                    return;   
                }
                case ClipboardContentType.Rtf: {
                    Logger.Log("[EditorScreen]", "ClipboardContentType.Rtf");

                    if (!(clipboardContent.Content is string textContent)) return;
                    Logger.Log("[EditorScreen]", "ClipboardContentType.Rtf => TextComponent", textContent.Truncate(20, "..."));

                    var model = new ComponentModel {
                        Type = "TextComponent",
                        Content = null,
                        RemoteId = null,
                        PosX = position.X,
                        PosY = position.Y,
                        SizeX = 50,
                        SizeY = 50,
                        FileId = App.EditorScreen.FileId,
                        Deleted = false,
                        ZIndex = -1,
                        ComponentId = -1,
                    };
                    
                    await model.SaveAsync();
                    
                    var textComponent = new TextComponent(model);
                    App.EditorScreen.AddContentComponent(textComponent);
                    
                    textComponent.Init();

                    textComponent.CreateUserAction = false;
                    textComponent.SetContent(textContent);
                    textComponent.CreateUserAction = true;
                    
                    textComponent.RepositionNobs();
                    break;
                }
                case ClipboardContentType.Text: {
                    Logger.Log("[EditorScreen]", "ClipboardContentType.Text");

                    var textContent = clipboardContent.Content as string;
                    Logger.Log("[EditorScreen]", "ClipboardContentType.Text => TextComponent", textContent.Truncate(20, "..."));

                    var model = new ComponentModel {
                        Type = "TextComponent",
                        Content = null,
                        RemoteId = null,
                        PosX = position.X,
                        PosY = position.Y,
                        SizeX = 50,
                        SizeY = 50,
                        FileId = App.EditorScreen.FileId,
                        Deleted = false,
                        ZIndex = -1,
                        ComponentId = -1,
                    };
                    
                    await model.SaveAsync();
                    
                    var textComponent = new TextComponent(model);
                    App.EditorScreen.AddContentComponent(textComponent);
                    
                    textComponent.Init();

                    textComponent.CreateUserAction = false;
                    textComponent.Content.SetUnformattedText(textContent);
                    textComponent.SetContent(textComponent.Content.FormattedTextString);
                    textComponent.CreateUserAction = true;
                    
                    textComponent.RepositionNobs();
                    break;
                }
                case ClipboardContentType.Other:
                    Logger.Log("[EditorScreen]", "ClipboardContentType.Other");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}