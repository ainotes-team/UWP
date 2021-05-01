using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Helpers.Extensions;

namespace AINotes.Helpers.Sidebar.RepresentationPlan.Models {
    public static class RepresentationColorHelper {
        public static readonly Dictionary<string, Color> KindColors = new Dictionary<string, Color> {
            {"Raumänderung", ColorCreator.FromHex("#49B04D")},
            {"Klasse frei", ColorCreator.FromHex("#F54133")},
            {"Zusatzunterricht", ColorCreator.FromHex("#9C25AF")},
            {"Unknown", ColorCreator.FromHex("#9C25AF")},
        };
    }
    
    public struct RepresentationItemModel {
        public string Class { get; set; }
        public string Day { get; set; }
        public string Position { get; set; }
        public string Subject { get; set; }
        public string Room { get; set; }
        public string VSubject { get; set; }
        public string VRoom { get; set; }
        public string Kind { get; set; }
        public string Info { get; set; }
        public string Comment { get; set; }
        public string Message { get; set; }

        public string DetailText => (string.IsNullOrWhiteSpace(VSubject) || Subject == VSubject ? Subject : VSubject + " statt " + Subject) + " in " + (string.IsNullOrWhiteSpace(VRoom) || Room == VRoom ? Room : VRoom + " statt " + Room) + (string.IsNullOrWhiteSpace(Info) ? "" : " - " + Info);
        public Brush Color => (RepresentationColorHelper.KindColors.ContainsKey(Kind) ? RepresentationColorHelper.KindColors[Kind] : RepresentationColorHelper.KindColors["Unknown"]).ToBrush();
    }
}