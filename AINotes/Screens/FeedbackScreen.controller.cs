using System;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AINotes.Helpers;
using Helpers;
using Newtonsoft.Json.Linq;
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
                    ZipFile.CreateFromDirectory(LocalFileHelper.ToAbsolutePath("logs"), LocalFileHelper.ToAbsolutePath("logs.zip"), CompressionLevel.NoCompression, false);

                    // upload logs
                    updateCallback?.Invoke("Sending... Please wait :)");
                    var responseString = System.Text.Encoding.ASCII.GetString(new WebClient().UploadFile("https://file.io/?expires=3m", LocalFileHelper.ToAbsolutePath("logs.zip")));

                    // send bullet
                    await Logger.SendBullet($"{SystemInfo.GetSystemId()}: Feedback & Logs", message, (string) JObject.Parse(responseString).GetValue("link"));

                } else {
                    // send bullet
                    await Logger.SendBullet($"{SystemInfo.GetSystemId()}: Feedback", message);
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