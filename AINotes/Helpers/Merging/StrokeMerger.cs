using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using AINotes.Helpers.InkCanvas;
using AINotes.Models;
using AINotesCloud.Models;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using Newtonsoft.Json;

namespace AINotes.Helpers.Merging {
    public static class StrokeMerger {
        public static async Task<FileModel> MergeStrokeContent(FileModel localFileModel, RemoteFileModel remoteFileModel) {
            var strokeContainer = new InkStrokeContainer();
            var localStrokeString = localFileModel.StrokeContent;
            var remoteStrokeString = remoteFileModel.StrokeContent;
                
            if (localStrokeString == remoteStrokeString) return localFileModel;
            
            if (localStrokeString == null) {
                localFileModel.StrokeContent = remoteStrokeString;
                if (localFileModel.FileId == App.EditorScreen.FileId) {
                    App.EditorScreen.GetInkCanvas().LoadStrokesFromIsf(remoteStrokeString.Deserialize<byte[]>());
                }
                
                return localFileModel;
            }
            
            if (remoteStrokeString == null) {
                return localFileModel;
            }
            
            var matchedStrokes = await ExtractAndMatchStrokes(localStrokeString, remoteStrokeString);
            Logger.Log($"[{nameof(StrokeMerger)}]", $"{nameof(MergeStrokeContent)} - Matched local and remote strokes - {matchedStrokes.Count} matches");

            var strokesToAdd = new List<InkStroke>();
            var strokesToDelete = new List<InkStroke>();
            var strokesToChange = new List<InkStroke>();
            
            foreach (var (lStroke, rStroke) in matchedStrokes) {
                if (lStroke == null) {
                    if (rStroke == null) continue;
                    if (localFileModel.LastChangedDate > remoteFileModel.LastChangedDate) continue;
                    
                    strokeContainer.AddStroke(rStroke.Clone());
                    Logger.Log($"[{nameof(StrokeMerger)}]", $"{nameof(MergeStrokeContent)} - Adding stroke to StrokeContainer since it did not exist locally");
                    
                    if (localFileModel.FileId == App.EditorScreen.FileId) {
                        strokesToAdd.Add(rStroke.Clone());
                        Logger.Log($"[{nameof(StrokeMerger)}]", $"{nameof(MergeStrokeContent)} - Adding stroke to EditorScreen since it did not exist locally");
                    }
                    continue;
                }
            
                if (rStroke == null) {
                    if (remoteFileModel.LastChangedDate > localFileModel.LastChangedDate) {
                        Logger.Log($"[{nameof(StrokeMerger)}]", $"{nameof(MergeStrokeContent)} - Remote stroke was deleted");
                        strokesToDelete.Add(lStroke);
                        continue;
                    }
                    
                    strokeContainer.AddStroke(lStroke.Clone());
                    Logger.Log($"[{nameof(StrokeMerger)}]", $"{nameof(MergeStrokeContent)} - Remote stroke did not exist");
                    continue;
                }
            
                if (lStroke.StrokeStartedTime.HasValue && rStroke.StrokeStartedTime.HasValue) {
                    if (lStroke.StrokeStartedTime.Value.UtcTicks >= rStroke.StrokeStartedTime.Value.UtcTicks) {
                        Logger.Log($"[{nameof(StrokeMerger)}]", $"{nameof(MergeStrokeContent)} - Local stroke was updated most lately");
                        
                        strokeContainer.AddStroke(lStroke.Clone());
                    } else {
                        strokeContainer.AddStroke(rStroke.Clone());
                        if (localFileModel.FileId == App.EditorScreen.FileId) {
                            Logger.Log($"[{nameof(StrokeMerger)}]", $"{nameof(MergeStrokeContent)} - Adding stroke to EditorScreen since it was changed");
                            strokesToChange.Add(rStroke);
                        }
                    }
                }
            }
            
            MainThread.BeginInvokeOnMainThread(() => {
                foreach (var inkStroke in App.EditorScreen.GetInkCanvas().InkPresenter.StrokeContainer.GetStrokes().Where(stroke => stroke.Selected)) {
                    inkStroke.Selected = false;
                }
                
                foreach (var inkStroke in App.EditorScreen.GetInkCanvas().InkPresenter.StrokeContainer.GetStrokes()) {
                    var select = strokesToDelete.Any(stroke => inkStroke.StrokeStartedTime != null 
                                                               && stroke.StrokeStartedTime != null && 
                                                               stroke.StrokeStartedTime.Value.Equals(inkStroke.StrokeStartedTime.Value));
                    if (select) inkStroke.Selected = true;
                }

                App.EditorScreen.GetInkCanvas().InkPresenter.StrokeContainer.DeleteSelected();
                App.EditorScreen.GetInkCanvas().InkPresenter.StrokeContainer.AddStrokes(strokesToAdd);

                // foreach (var inkStroke in App.EditorScreen.GetInkCanvas().InkPresenter.StrokeContainer.GetStrokes()) {
                //     InkStroke remoteInkStroke = null;
                //     
                //     var toBeChanged = strokesToChange.Any(stroke => {
                //         var match = inkStroke.StrokeStartedTime != null && stroke.StrokeStartedTime != null && stroke.StrokeStartedTime.Value.Equals(inkStroke.StrokeStartedTime.Value);
                //         if (match) remoteInkStroke = stroke;
                //         return match;
                //     });
                //
                //     if (toBeChanged && remoteInkStroke != null) {
                //         inkStroke.PointTransform = remoteInkStroke.PointTransform;
                //     }
                // }
            });

            var memoryStream = new MemoryStream();
            await strokeContainer.SaveAsync(memoryStream.AsOutputStream(), InkPersistenceFormat.Isf);
            
            localFileModel.StrokeContent = JsonConvert.SerializeObject(memoryStream.GetBuffer());
            
            Logger.Log(localFileModel.StrokeContent.Substring(0, 100));
            Logger.Log($"[{nameof(StrokeMerger)}]", $"{nameof(MergeStrokeContent)} - Extracting stroke content from merging result");

            return localFileModel;
        }

        private static async Task<List<(InkStroke, InkStroke)>> ExtractAndMatchStrokes(string localStrokeString, string remoteStrokeString) {
            var result = new List<(InkStroke, InkStroke)>();
            
            var localStrokes = (await InkHelper.GetStrokesFromIsf(localStrokeString.Deserialize<byte[]>())).ToList();
            var remoteStrokes = (await InkHelper.GetStrokesFromIsf(remoteStrokeString.Deserialize<byte[]>())).ToList();
            
            foreach (var localStroke in localStrokes) {
                var matchingRemoteStroke = remoteStrokes.FirstOrDefault(remoteStroke => 
                    localStroke.StrokeStartedTime != null && remoteStroke.StrokeStartedTime != null 
                                                          && remoteStroke.StrokeStartedTime.Value.Equals(localStroke.StrokeStartedTime.Value));
                result.Add((localStroke, matchingRemoteStroke));
                if (matchingRemoteStroke != null) remoteStrokes.Remove(matchingRemoteStroke);
            }

            result.AddRange(remoteStrokes.Select(remoteStroke => ((InkStroke, InkStroke)) (null, remoteStroke)));

            return result;
        }
    }
}