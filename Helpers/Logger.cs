using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Helpers.Essentials;
using Sentry;

namespace Helpers {
    public enum LogLevel {
        Timing,
        Verbose,
        Debug,
        Default,
        Warning,
        Error
    }

    public static class Logger {
        private static readonly Dictionary<LogLevel, string> LogLevels = new Dictionary<LogLevel, string> {
            {LogLevel.Timing,  "[TIMING ]  "},
            {LogLevel.Verbose, "[VERBOSE]  "},
            {LogLevel.Debug,   "[DEBUG  ]  "},
            {LogLevel.Default, "[DEFAULT]  "},
            {LogLevel.Warning, "[WARNING]  "},
            {LogLevel.Error,   "[ERROR  ]  "},
        };
        
        private static readonly Dictionary<LogLevel, BreadcrumbLevel> SentryLogLevels = new Dictionary<LogLevel, BreadcrumbLevel> {
            {LogLevel.Timing,  BreadcrumbLevel.Debug},
            {LogLevel.Verbose, BreadcrumbLevel.Debug},
            {LogLevel.Debug,   BreadcrumbLevel.Debug},
            {LogLevel.Default, BreadcrumbLevel.Info},
            {LogLevel.Warning, BreadcrumbLevel.Warning},
            {LogLevel.Error,   BreadcrumbLevel.Error},
        };

        private static StreamWriter _loggingStream;
        private static readonly HttpClient HttpClient = new HttpClient();

        private const int TagLength = 36;
        private static LogLevel MinimumLogLevel => (LogLevel) Enum.Parse(typeof(LogLevel), UserPreferenceHelper.Get("MinimumLogLevel", LogLevel.Timing.ToString()));
        private static bool LoggingEnabled => true; // Preferences.LoggingEnabled;
        private static bool WriteToFile { get; } = true;
        
        private static bool LogThreadingInfo { get; } = true;
        private static bool LogTime { get; } = true;

        // actual logger
        private static void InternalLog(string l) {
            // write to console
            Debug.WriteLine(l);

            // write to file
            if (!WriteToFile) return;
            if (!Directory.Exists(LocalFileHelper.ToAbsolutePath("logs"))) Directory.CreateDirectory(LocalFileHelper.ToAbsolutePath("logs"));
            
            if (_loggingStream == null) {
                _loggingStream = File.CreateText(LocalFileHelper.ToAbsolutePath($"logs/log_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.txt"));
                _loggingStream.AutoFlush = true;
            }
            
            _loggingStream.WriteLine(l);
        }
        
        // log prefix
        private static void LogPrefixed(LogLevel logLevel, object logString) {
            if (!LoggingEnabled) return;
            if (logLevel < MinimumLogLevel) return;

            var l = "";

            // add time
            if (LogTime) {
                l += DateTime.Now.ToString("[HH:mm:ss:fff] ");
            }
            
            // add threading info
            if (LogThreadingInfo) {
                var currentThread = Thread.CurrentThread;
                var currentThreadId = currentThread.ManagedThreadId;
                string currentThreadType;
                try {
                    var hasThreadAccess = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess;
                    currentThreadType = hasThreadAccess ? "Main" : currentThread.IsThreadPoolThread ? "Pool" : "????";
                } catch (Exception) {
                    currentThreadType = "????";
                }
                l += $"[ {currentThreadId, 2} | {(currentThreadType)} ] ";
            }
            
            // add the log level
            l += LogLevels[logLevel];
            
            // add actual log
            l += logString;
            InternalLog(l);

            // write to sentry
            SentrySdk.AddBreadcrumb(l, "log", level: SentryLogLevels[logLevel]);
        }
        
        // default log function
        public static void Log(object l0, object l1 = null, object l2 = null, object l3 = null, object l4 = null, object l5 = null, object l6 = null, object l7 = null, object l8 = null, object l9 = null, object l10 = null, string separator = " ", LogLevel logLevel = LogLevel.Default) {
            var logMessage = (l0 ?? "null").ToString();
            if (logMessage.StartsWith("[") && logMessage.EndsWith("]")) {
                logMessage = logMessage.Remove(logMessage.Length - 1);
                while (logMessage.Length < TagLength) logMessage += " ";

                logMessage += "]";
            }

            if (l1 != null) logMessage += separator + l1;
            if (l2 != null) logMessage += separator + l2;
            if (l3 != null) logMessage += separator + l3;
            if (l4 != null) logMessage += separator + l4;
            if (l5 != null) logMessage += separator + l5;
            if (l6 != null) logMessage += separator + l6;
            if (l7 != null) logMessage += separator + l7;
            if (l8 != null) logMessage += separator + l8;
            if (l9 != null) logMessage += separator + l9;
            if (l10 != null) logMessage += separator + l10;

            LogPrefixed(logLevel, logMessage);
        }
        
        public static async Task SendFeedback(string title, string message, string attachmentPath=null) {
            var eventId = SentrySdk.CaptureMessage($"{title} ({SystemInfo.GetSystemId()})", scope  => {
                if (attachmentPath == null) return;
                scope.AddAttachment(attachmentPath);
            });
            SentrySdk.CaptureUserFeedback(eventId, SystemInfo.GetSystemId(), message);
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
        }
    }
}