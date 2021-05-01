using Windows.UI.Xaml;
using Helpers;
using Helpers.Extensions;

// ReSharper disable All
namespace AINotes {
    public class Program {
        public static void Main(string[] args) {
            Logger.Log("[Program]", "AINotes with Arguments", args?.ToFString());
            Application.Start(_ => new App());
        }
    }
}