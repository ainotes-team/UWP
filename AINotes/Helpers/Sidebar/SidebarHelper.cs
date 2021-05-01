using AINotes.Controls.Sidebar.Content;
using Helpers;

namespace AINotes.Helpers.Sidebar {
    public static class SidebarHelper {
        private const string TasksPath = "sidebar/tasks.json";
        private const string NotesPath = "sidebar/notes.json";
        private const string TimetablePath = "sidebar/timetable.json";
        private const string SubstitutionsPath = "sidebar/substitutions.json";
        private const string FormulaPath = "sidebar/formulas.json";

        static SidebarHelper() {
            if (!LocalFileHelper.FileExists(TasksPath)) LocalFileHelper.WriteFile(TasksPath, CustomTasksView.DefaultValue);
            if (!LocalFileHelper.FileExists(NotesPath)) LocalFileHelper.WriteFile(NotesPath, "{}");
            if (!LocalFileHelper.FileExists(TimetablePath)) LocalFileHelper.WriteFile(TimetablePath, CustomTimeTableView.DefaultValue);
            if (!LocalFileHelper.FileExists(SubstitutionsPath)) LocalFileHelper.WriteFile(SubstitutionsPath, "{}");
            if (!LocalFileHelper.FileExists(FormulaPath)) LocalFileHelper.WriteFile(FormulaPath, CustomFormulaView.DefaultValue);
        }

        // tasks
        public static string GetTasksJson() => LocalFileHelper.ReadFile(TasksPath);
        public static void SetTasksJson(string tasksJson) => LocalFileHelper.WriteFile(TasksPath, tasksJson);
        
        // notes
        public static string GetNotesJson() => LocalFileHelper.ReadFile(NotesPath);
        public static void SetNotesJson(string notesJson) => LocalFileHelper.WriteFile(NotesPath, notesJson);

        // timetable
        public static string GetTimetableJson() => LocalFileHelper.ReadFile(TimetablePath);
        public static void SetTimetableJson(string timeTableJson) => LocalFileHelper.WriteFile(TimetablePath, timeTableJson);

        // substitutions
        public static string GetSubstitutionsJson() => LocalFileHelper.ReadFile(SubstitutionsPath);
        public static void SetSubstitutionsJson(string substitutionsJson) => LocalFileHelper.WriteFile(SubstitutionsPath, substitutionsJson);
        
        // formula collection
        public static string GetFormulasJson() => LocalFileHelper.ReadFile(FormulaPath);
        public static void SetFormulasJson(string formulasJson) => LocalFileHelper.WriteFile(FormulaPath, formulasJson);
    }
}