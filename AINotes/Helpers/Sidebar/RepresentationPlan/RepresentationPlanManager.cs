using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AINotes.Helpers.Sidebar.RepresentationPlan.Implementations;
using AINotes.Helpers.Sidebar.RepresentationPlan.Models;

namespace AINotes.Helpers.Sidebar.RepresentationPlan {
    public static class RepresentationPlanManager {
        public static readonly Dictionary<string, RepresentationPlanModel> AvailableSchools = new Dictionary<string, RepresentationPlanModel> {
            {"Stefan-George-Gymansium, Bingen", new RepresentationPlanModel { ParserType = typeof(DavinciParser), Url = "http://vertretungsplan.sgg-bingen.de/" }}
        };
        
        private static readonly Dictionary<string, IRepresentationPlanParser> Parsers = new Dictionary<string, IRepresentationPlanParser>();

        public static async Task<Dictionary<string, List<RepresentationItemModel>>> GetRepresentations() {
            var school = AvailableSchools[Preferences.SchoolsList];
            if (!Parsers.ContainsKey(Preferences.SchoolsList)) {
                Parsers.Add(Preferences.SchoolsList, (IRepresentationPlanParser) Activator.CreateInstance(school.ParserType));
            }
            return await Parsers[Preferences.SchoolsList].GetRepresentations(school.Url, Preferences.SchoolGrade);
        }
    }

}