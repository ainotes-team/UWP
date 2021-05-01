using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Helpers;
using Helpers.Essentials;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;

namespace AINotes.Helpers.Integrations {
    public class MoodleUserModel {
        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("firstname")]
        public string FirstName { get; set; }

        [JsonProperty("lastname")]
        public string LastName { get; set; }

        [JsonProperty("fullname")]
        public string FullName { get; set; }

        [JsonProperty("userid")]
        public int UserId { get; set; }

        [JsonProperty("userpictureurl")]
        public string UserPictureUrl { get; set; }

    }
    
    public class MoodleCourseModel {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("shortname")]
        public string ShortName { get; set; }
        
        [JsonProperty("fullname")]
        public string FullName { get; set; }
        
        [JsonProperty("displayname")]
        public string DisplayName { get; set; }
        
        [JsonProperty("enrolledusercount")]
        public int? EnrolledUserCount { get; set; }
        
        [JsonProperty("visible")]
        public bool? Visible { get; set; }

        [JsonProperty("hidden")]
        public bool? Hidden { get; set; }
        
        [JsonProperty("summary")]
        public string Summary { get; set; }
        
        [JsonProperty("showgrades")]
        public bool? ShowGrades { get; set; }
        
        [JsonProperty("category")]
        public int? Category { get; set; }
        
        [JsonProperty("progress")]
        public double? Progress { get; set; }
        
        [JsonProperty("completed")]
        public bool? Completed { get; set; }
        
    }

    public class MoodleCourseCategoryModel {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("section")]
        public int? Section { get; set; }

        [JsonProperty("visible")]
        public bool? Visible { get; set; }

        [JsonProperty("uservisible")]
        public bool? UserVisible { get; set; }

        [JsonProperty("modules")]
        public List<MoodleCourseCategoryModule> Modules { get; set; }
        
    }

    public class MoodleCompletionDataModel {
        [JsonProperty("state")]
        public int? State { get; set; }
        
        [JsonProperty("timecompleted")]
        public long? TimeCompleted { get; set; }
        
    }

    public class MoodleContentsInfoModel {
        [JsonProperty("filescount")]
        public int? FilesCount { get; set; }
        
        [JsonProperty("filessize")]
        public int? FilesSize { get; set; }
        
        [JsonProperty("lastmodified")]
        public long? LastModified { get; set; }
        
        [JsonProperty("mimetypes")]
        public List<string> MimeTypes { get; set; }
        
        [JsonProperty("repositorytype")]
        public string RepositoryType { get; set; }
        
    }

    public class MoodleContentModel {
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("filename")]
        public string FileName { get; set; }
        
        [JsonProperty("filepath")]
        public string FilePath { get; set; }
        
        [JsonProperty("filesize")]
        public int? FileSize { get; set; }
        
        [JsonProperty("fileurl")]
        public string FileUrl { get; set; }
        
        [JsonProperty("timecreated")]
        public long? TimeCreated { get; set; }
        
        [JsonProperty("timemodified")]
        public long? TimeModified { get; set; }
        
        [JsonProperty("sortorder")]
        public int? SortOrder { get; set; }
        
        [JsonProperty("mimetype")]
        public string MimeType { get; set; }
        
        [JsonProperty("isexternalfile")]
        public bool? IsExternalFile { get; set; }
        
        [JsonProperty("userid")]
        public string AuthorUserId { get; set; }
        
        [JsonProperty("author")]
        public string Author { get; set; }
        
        [JsonProperty("license")]
        public string License { get; set; }
        
    }

    public class MoodleCourseCategoryModule {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("visible")]
        public bool? Visible { get; set; }

        [JsonProperty("uservisible")]
        public bool? UserVisible { get; set; }

        [JsonProperty("modicon")]
        public string ModuleIconUrl { get; set; }

        [JsonProperty("modname")]
        public string ModuleName { get; set; }

        [JsonProperty("completiondata")]
        public MoodleCompletionDataModel CompletionData { get; set; }

        [JsonProperty("contents")]
        public List<MoodleContentModel> Contents { get; set; }
        
        [JsonProperty("contentsinfo")]
        public MoodleContentsInfoModel ContentsInfo { get; set; }
    }

    public class MoodleAssignmentsModel {
        [JsonProperty("courses")]
        public List<MoodleAssignmentsCourseModel> Courses { get; set; }

        [JsonProperty("warnings")]
        public List<object> Warnings { get; set; }
    }

    public class MoodleAssignmentsAssignmentModel {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("cmid")]
        public int? CategoryId { get; set; }
        
        [JsonProperty("course")]
        public int? CourseId { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("nosubmissions")]
        public int? NoSubmissions { get; set; }
        
        [JsonProperty("submissiondrafts")]
        public int? SubmissionDrafts { get; set; }
        
        [JsonProperty("duedate")]
        public long? DueDate { get; set; }
        
        [JsonProperty("grade")]
        public int? Grade { get; set; }
        
        [JsonProperty("timemodified")]
        public long? TimeModified { get; set; }
        
        [JsonProperty("completionsubmit")]
        public int? CompletionSubmit { get; set; }

        [JsonProperty("intro")]
        public string Intro { get; set; }

        [JsonProperty("introfiles")]
        public List<MoodleContentModel> IntroFiles { get; set; }

        [JsonProperty("introattachments")]
        public List<MoodleContentModel> IntroAttachments { get; set; }
    }
    
    public class MoodleAssignmentsCourseModel {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("fullname")]
        public string FullName { get; set; }
        
        [JsonProperty("shortname")]
        public string ShortName { get; set; }
        
        [JsonProperty("timemodified")]
        public long? TimeModified { get; set; }
        
        [JsonProperty("assignments")]
        public List<MoodleAssignmentsAssignmentModel> Assignments { get; set; }
        
    }
    
    public static class MoodleHelper {
        private static readonly HttpClient HttpClient = new HttpClient();
        
        private const string WebServiceEndpoint = "/webservice/rest/server.php";
        private const string TokenEndpoint = "/login/token.php";

        private const string ServiceName = "moodle_mobile_app";

        private static string _baseUrl;

        private static string _userToken;
        private static string UserToken {
            get => _userToken;
            set {
                _userToken = value;
                SavedStatePreferenceHelper.Set("moodle_user_token@" + _baseUrl, _userToken);
            }
        }

        public static bool IsLoggedIn => !string.IsNullOrWhiteSpace(UserToken);

        static MoodleHelper() {
            Initialize();
        }

        public static void Initialize() {
            _baseUrl = Preferences.MoodleUrl;
            _userToken = SavedStatePreferenceHelper.Get("moodle_user_token@" + _baseUrl, "");
        }

        public static async Task<MoodleUserModel> GetUserModel() {
            var resp = await SendRequest("core_webservice_get_site_info");
            if (resp == null) return null;
            var responseString = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<MoodleUserModel>(responseString);
        }

        public static async Task<List<MoodleCourseModel>> GetUserCourses(int userId) {
            var resp = await SendRequest("core_enrol_get_users_courses", new Dictionary<string, string> {
                {"userid", userId.ToString()}
            });
            if (resp == null) return null;
            var responseString = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<MoodleCourseModel>>(responseString);
        }

        public static async Task<List<MoodleCourseCategoryModel>> GetCourseCategories(int courseId) {
            var resp = await SendRequest("core_course_get_contents", new Dictionary<string, string> {
                {"courseid", courseId.ToString()}
            });
            if (resp == null) return null;
            var responseString = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<MoodleCourseCategoryModel>>(responseString);
        }
        
        public static async Task<MoodleAssignmentsModel> GetUserAssignments() {
            var resp = await SendRequest("mod_assign_get_assignments", new Dictionary<string, string> {
                {"includenotenrolledcourses", "1"}
            });
            if (resp == null) return null;
            var responseString = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<MoodleAssignmentsModel>(responseString);
        }
        
        // request builder helper
        public static async Task<HttpResponseMessage> SendRequest(string functionName, Dictionary<string, string> parameters=null) {
            try {
                // build query
                var query = HttpUtility.ParseQueryString(string.Empty);
                query["moodlewsrestformat"] = "json";
                query["wstoken"] = UserToken;
                query["wsfunction"] = functionName;

                if (parameters != null) {
                    foreach (var (key, value) in parameters) {
                        query[key] = value;
                    }
                }

                // build url
                var url = new UriBuilder($"{_baseUrl}{WebServiceEndpoint}") {
                    Port = -1,
                    Query = query.ToString()
                }.Uri;

                // send request
                return await HttpClient.GetAsync(url);
            } catch (Exception ex) {
                Logger.Log("[MoodleHelper]", $"SendRequest({functionName}, {parameters}): Exception:", ex.ToString(), logLevel: LogLevel.Error);
                return null;
            }
        }

        // login
        public static async Task<bool> LoginAsync(string username, string password) {
            try {
                var url = $"{_baseUrl}{TokenEndpoint}?username={username}&password={password}&service={ServiceName}";
                var response = await HttpClient.GetAsync(new Uri(url));
                if (!response.IsSuccessStatusCode) {
                    Logger.Log("[MoodleHelper]", "Login: Error Response:", response.StatusCode, response.Content, logLevel: LogLevel.Warning);
                    return false;
                }

                var responseContentString = await response.Content.ReadAsStringAsync();
                var responseJson = (JObject) JsonConvert.DeserializeObject(responseContentString);
                UserToken = (string) responseJson?.GetValue("token");
                if (string.IsNullOrWhiteSpace(UserToken)) {
                    Logger.Log("[MoodleHelper]", "Login: No Token - Response:", responseContentString, response.Content, logLevel: LogLevel.Warning);
                    return false;
                }
            } catch (Exception ex) {
                Logger.Log("[MoodleHelper]", "Login: Exception:", ex.ToString(), logLevel: LogLevel.Error);
                return false;
            }
            
            Logger.Log("[MoodleHelper]", "Login: Success");
            return true;
        }

        public static void Logout() {
            UserToken = null;
        }
    }
}