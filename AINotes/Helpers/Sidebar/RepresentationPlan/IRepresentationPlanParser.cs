using System.Collections.Generic;
using System.Threading.Tasks;
using AINotes.Helpers.Sidebar.RepresentationPlan.Models;

namespace AINotes.Helpers.Sidebar.RepresentationPlan {
    public interface IRepresentationPlanParser {
        Task<Dictionary<string, List<RepresentationItemModel>>> GetRepresentations(string url, string grade);
    }
}