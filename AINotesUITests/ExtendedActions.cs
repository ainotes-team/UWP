using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

namespace AINotesUITests {
    public class WaitAction : IAction {
        private readonly int _timeMs;

        public WaitAction(int timeMs) {
            _timeMs = timeMs;
        }

        public void Perform() {
            Thread.Sleep(_timeMs);
        }
    }

    public class ExtendedActions : Actions {
        public ExtendedActions(IWebDriver driver) : base(driver) { }

        public Actions Wait(int timeMs) {
            AddAction(new WaitAction(timeMs));
            return this;
        }
    }

    public class ExtendedTouchActions : TouchActions {
        public ExtendedTouchActions(IWebDriver driver) : base(driver) { }

        public Actions Wait(int timeMs) {
            AddAction(new WaitAction(timeMs));
            return this;
        }
    }
}