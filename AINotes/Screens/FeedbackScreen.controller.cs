using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using AINotes.Helpers;
using Helpers;
using File = System.IO.File;

namespace AINotes.Screens {
    public partial class FeedbackScreen {
        private void LoadToolbar() {
            App.Page.OnBackPressed = () => App.Page.Load(App.FileManagerScreen);
        }
        
        private async Task SendFeedback(string message, bool includeLogs, Action<string> updateCallback=null) {
            try {
                if (includeLogs) {
                    // delete old zip
                    LocalFileHelper.DeleteFile("logs.zip");
                    while (File.Exists(LocalFileHelper.ToAbsolutePath("logs.zip"))) {
                        Thread.Sleep(20);
                    }

                    // create new zip
                    var logsZipPath = LocalFileHelper.ToAbsolutePath("logs.zip");
                    try {
                        ZipFile.CreateFromDirectory(LocalFileHelper.ToAbsolutePath("logs"), logsZipPath, CompressionLevel.NoCompression, false);
                    } catch (IOException ex) {
                        Logger.Log("[FeedbackScreen]", "Exception in sendLogsButtonCommand - ZipFile.CreateFromDirectory: ", ex.ToString(), logLevel: LogLevel.Warning);
                    }

                    updateCallback?.Invoke("Sending... Please wait :)");

                    // upload logs
                    // var responseString = System.Text.Encoding.ASCII.GetString(new WebClient().UploadFile("https://file.io/?expires=3m", logsPath));
                    // var link = (string) JObject.Parse(responseString).GetValue("link");
                    
                    // send
                    await Logger.SendFeedback("Feedback", message, logsZipPath);

                } else {
                    // send
                    await Logger.SendFeedback("Feedback", message);
                }
            
                // update status
                updateCallback?.Invoke("Sent successfully!");
            } catch (Exception ex) {
                Logger.Log("[FeedbackScreen]", "Exception in sendLogsButtonCommand: ", ex.ToString(), logLevel: LogLevel.Error);
            }
        }

        private void OpenContact(string txt) {
            if (txt.StartsWith("https://")) {
                _ = Windows.System.Launcher.LaunchUriAsync(new Uri(txt));
            } else {
                Clipboard.SetTextAsync(txt);
            }
        }
    }
}