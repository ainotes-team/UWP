using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using System;

namespace AINotesUITests {
    public class MainSession {
        private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723/wd/hub";
        private const string AINotesAppId = "1875vincentscode.AINotes_xnbggcnkvqcny!App";

        protected static WindowsDriver<WindowsElement> Session;

        protected static void Setup() {
            if (Session != null) return;
            var appCapabilities = new DesiredCapabilities();
            appCapabilities.SetCapability("app", AINotesAppId);
            appCapabilities.SetCapability("deviceName", "WindowsPC");
            Session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), appCapabilities);
            Assert.IsNotNull(Session);
        }

        protected static void TearDown() {
            if (Session == null) return;
            Session.Quit();
            Session = null;
        }
    }
}