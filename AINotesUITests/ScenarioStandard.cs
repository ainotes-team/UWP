using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Drawing;
using OpenQA.Selenium.Interactions;

namespace AINotesUITests {
    [TestClass]
    public class ScenarioStandard : MainSession {
        #region App

        [TestCategory("App")]
        [TestMethod]
        public void Startup() {
            Assert.IsNotNull(Session);
        }

        #endregion

        #region FileManagerScreen

        [TestCategory("FileManagerScreen")]
        [TestMethod]
        public void AddFile() {
            // wait a moment
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // file list before
            var countBefore = Session.FindElementByName("FMSFileGridView").FindElementsByClassName("GridViewItem").Count;

            // add file button
            Session.FindElementByName("AddFileTBI").Click();

            // wait a moment
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // get add file popup content
            WindowsElement fileNameEdt = null;
            WindowsElement fileOkButton = null;
            foreach (var popup in Session.FindElementsByClassName("Popup")) {
                var fileNameEdtOptions = popup.FindElementsByName("File name");
                if (fileNameEdtOptions.Count == 0) continue;
                fileNameEdt = (WindowsElement) fileNameEdtOptions[0];

                var fileOkButtonOptions = popup.FindElementsByName("OK");
                if (fileOkButtonOptions.Count == 0) continue;
                fileOkButton = (WindowsElement) fileOkButtonOptions[0];
            }

            if (fileNameEdt == null) {
                Assert.Fail("fileNameEdt not found");
                return;
            }

            if (fileOkButton == null) {
                Assert.Fail("fileOkButton not found");
                return;
            }

            // create a new file
            fileNameEdt.SendKeys("Hallo Welt!");
            Thread.Sleep(TimeSpan.FromSeconds(0.3));
            fileOkButton.Click();

            // wait a moment
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // file list after
            var countAfter = Session.FindElementByName("FMSFileGridView").FindElementsByClassName("GridViewItem").Count;

            // check
            Assert.AreEqual(countBefore + 1, countAfter);
        }

        #endregion

        #region EditorScreen

        private void NavigateToEditorScreen() {
            var currentPageTitle = Session.FindElementByAccessibilityId("PageTitle").Text;
            switch (currentPageTitle) {
                case "Filemanager":
                    var firstFileItem = (WindowsElement) Session.FindElementByName("FMSFileGridView").FindElementsByClassName("GridViewItem")[0]; 
                    
                    Assert.IsNotNull(firstFileItem);
                    
                    var actionChain = new Actions(Session);
                    actionChain.MoveToElement(firstFileItem);
                    actionChain.DoubleClick();
                    actionChain.Perform();

                    // wait a moment
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    break;
                default:
                    return;
            }
        }

        [TestCategory("EditorScreen")]
        [TestMethod]
        public void RandomDrawing() {
            NavigateToEditorScreen();
        }

        [TestCategory("EditorScreen")]
        [TestMethod]
        public void RandomShapeDrawing() {
            NavigateToEditorScreen();
        }

        [TestCategory("EditorScreen")]
        [DataTestMethod]
        [DynamicData(nameof(GetShapeDrawingData), DynamicDataSourceType.Method)]
        public void ShapeDrawing(List<Point> points, List<Point> desiredOutcome) {
            NavigateToEditorScreen();
            
            // select handwriting
            Session.FindElementByName("HandwritingPTBI").Click();
            Thread.Sleep(TimeSpan.FromSeconds(0.2));
            
            // get ink canvas
            var inkCanvas = Session.FindElementByClassName("InkCanvas");
            
            var actionChain = new ExtendedActions(Session);
            actionChain.MoveToElement(inkCanvas);
            actionChain.Wait(200);
            actionChain.ClickAndHold();

            var currentPointerPos = new Point(0, 0);
            foreach (var pt in points) {
                var reqOffsetX = pt.X - currentPointerPos.X;
                var reqOffsetY = pt.Y - currentPointerPos.Y;
                currentPointerPos = pt;
                
                actionChain.MoveByOffset(reqOffsetX, reqOffsetY);
                actionChain.Wait(200);
            }
            
            actionChain.Wait(200);
            actionChain.Release();
            actionChain.Wait(2000);
            
            actionChain.Perform();
        }

        public static IEnumerable<object[]> GetShapeDrawingData() {
            yield return new object[] {
                new List<Point> {new Point(10, 10), new Point(100, 100) },
                new List<Point> {new Point(10, 10), new Point(100, 100) }
            };
            
            yield return new object[] {
                new List<Point> {new Point(100, 100), new Point(200, 100) },
                new List<Point> {new Point(100, 100), new Point(200, 100) }
            };
        }

        #endregion
        
        #region Setup
        
        [ClassInitialize]
        public static void ClassInitialize(TestContext context) => Setup();

        [ClassCleanup]
        public static void ClassCleanup() => TearDown();
        
        #endregion
    }
}