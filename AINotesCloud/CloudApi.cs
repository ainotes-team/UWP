using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AINotesCloud.Models;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using Newtonsoft.Json;
using HttpClient = System.Net.Http.HttpClient;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace AINotesCloud {
    public class CloudResponse {
        [JsonProperty("error")] public CloudResponseError Error { get; set; }

        [JsonProperty("token")] public string Token { get; set; }
    }

    public class CloudResponseError {
        [JsonProperty("statusCode")] public int StatusCode { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("message")] public string Message { get; set; }
    }

    public class ImageCreationResponse {
        [JsonProperty("_id")] public string ImageId { get; set; }
    }

    public class CloudApi {
        private readonly HttpClient _httpClient;

        // events
        public static event Action AccountChanged;

        // state
        private bool _isLoggedIn;

        public bool IsLoggedIn {
            get => _isLoggedIn;
            set => _isLoggedIn = value;
        }

        private string _token;

        public string Token {
            get => _token;
            set {
                _token = value;
                if (_token != null) {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    SavedStatePreferenceHelper.Set("cloudToken", _token);
                    IsLoggedIn = true;
                } else {
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                    SavedStatePreferenceHelper.Set("cloudToken", null);
                    IsLoggedIn = false;
                }
            }
        }

        public CloudApi(string baseAddress) {
            if (!baseAddress.StartsWith("https://") && !baseAddress.StartsWith("http://")) {
                baseAddress = "https://" + baseAddress;
            }

            _httpClient = new HttpClient {
                BaseAddress = new Uri(baseAddress)
            };
            
            Logger.Log($"[{nameof(CloudApi)}]", $"{baseAddress} is the base address");
            
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            OnLoggedIn += async () => { CurrentRemoteUserModel = await GetUser(); };

            OnLoggedOut += () => {
                CurrentRemoteUserModel = null;
                AccountChanged?.Invoke();
            };
        }


        #region UserController

        public event Action OnLoggedIn;
        public event Action OnPasswordChanged;
        public event Action OnLoggedOut;

        public static RemoteUserModel CurrentRemoteUserModel;

        public async Task<(bool success, string message)> RegisterAndLogin(string displayName, string email,
            string password) {
            try {
                var data = new {
                    displayName = displayName,
                    email = email,
                    password = password,
                    // profilePictureUrl = Configuration.DefaultProfilePicture
                }.Serialize();
                var response = await _httpClient.PostAsync("/users/",
                    new StringContent(data, Encoding.UTF8, "application/json"));

                if (response.StatusCode == (HttpStatusCode) 200) {
                    return await Login(email, password);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var cloudResponse = responseContent.Deserialize<CloudResponse>();
                return (false, cloudResponse.Error.Message);
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.Register:", ex.ToString(), logLevel: LogLevel.Error);
                return (false, ex.ToString());
            }
        }
        
        

        public async Task<(bool success, string message)> Login(string email, string password) {
            try {
                var data = new {
                    email = email,
                    password = password
                }.Serialize();
                var response = await _httpClient.PostAsync("/users/login",
                    new StringContent(data, Encoding.UTF8, "application/json"));
                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log("[CloudApi]", "Login Result:", responseContent);
                var cloudResponse = responseContent.Deserialize<CloudResponse>();

                var token = cloudResponse.Token;
                if (response.StatusCode == (HttpStatusCode) 200 && token != null) {
                    Token = token;

                    await GetUser();
                    OnLoggedIn?.Invoke();
                    return (true, "success");
                }

                return (false, cloudResponse.Error.Message);
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.Login:", ex.ToString(), logLevel: LogLevel.Error);
                return (false, ex.ToString());
            }
        }

        public async Task<bool> IsValidToken(string token) {
            using (var testClient = new HttpClient()) {
                testClient.BaseAddress = _httpClient.BaseAddress;
                testClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await testClient.GetAsync("/users/me");

                if (!response.IsSuccessStatusCode) {
                    return false;
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                // Logger.Log("[CloudApi]", "TestToken:", responseContent);
                var userModel = responseContent.Deserialize<RemoteUserModel>();

                return userModel.RemoteId != null;
            }
        }

        public async Task<(bool success, string message)> TokenLogin() {
            var savedToken = SavedStatePreferenceHelper.Get("cloudToken", null);
            if (savedToken == null) return (false, "no token");

            if (!await IsValidToken(savedToken)) return (false, "invalid token");
            Token = savedToken;

            await GetUser();
            OnLoggedIn?.Invoke();
            return (true, "success");
        }

        public async Task<RemoteUserModel> GetUser(string userId=null) {
            if (!IsLoggedIn) return null;
            if (userId == null) {
                var response = await _httpClient.GetAsync("/users/me");
                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log("[CloudApi]", "GetUserInfo:", "me", responseContent);

                CurrentRemoteUserModel = responseContent.Deserialize<RemoteUserModel>();

                AccountChanged?.Invoke();
                return CurrentRemoteUserModel;
            } else {
                var response = await _httpClient.GetAsync("/users/" + userId);
                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log("[CloudApi]", "GetUserInfo:", userId, responseContent);

                return responseContent.Deserialize<RemoteUserModel>();
            }
        }

        public async Task<RemoteUserModel> FindUser(string email) {
            if (!IsLoggedIn) return null;
            var response = await _httpClient.GetAsync("/users/find/" + email);
            
            if (!response.IsSuccessStatusCode) return null;
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Logger.Log("[CloudApi]", "FindUser:", email, responseContent);

            return responseContent.Deserialize<RemoteUserModel>();
        }

        public async Task<bool> PutUser(RemoteUserModel userModel) {
            if (!IsLoggedIn) return false;

            try {
                var data = userModel.Serialize();
                var response = await _httpClient.PutAsync("/users/me",
                    new StringContent(data, Encoding.UTF8, "application/json"));
                Logger.Log("[CloudApi]", "UpdateUserInfo:", response.StatusCode);

                var success = response.StatusCode == (HttpStatusCode) 200;
                if (success) {
                    await GetUser();
                }

                return success;
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.UpdateUserInfo:", ex.ToString(),
                    logLevel: LogLevel.Error);
                return false;
            }
        }

        public async Task<bool> ChangePassword(string oldPassword, string newPassword) {
            if (!IsLoggedIn) return false;

            try {
                var data = new {
                    oldPassword = oldPassword,
                    newPassword = newPassword
                }.Serialize();
                var response = await _httpClient.PutAsync("/users/change",
                    new StringContent(data, Encoding.UTF8, "application/json"));
                Logger.Log("[CloudApi]", "UpdatePassword:", response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();

                var cloudResponse = responseContent.Deserialize<CloudResponse>();

                var token = cloudResponse.Token;
                if (response.StatusCode == (HttpStatusCode) 200 && token != null) {
                    Token = token;
                    OnPasswordChanged?.Invoke();
                    return true;
                }

                return false;
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.UpdatePassword:", ex.ToString(),
                    logLevel: LogLevel.Error);
                return false;
            }
        }

        public void Logout() {
            Token = null;
            OnLoggedOut?.Invoke();
        }

        #endregion

        #region UserFilePermissionsController

        public async Task<bool> PostFilePermission(RemoteFilePermission filePermission) {
            var data = new {
                user = filePermission.UserId,
                file = filePermission.FileId,
                userPermission = filePermission.UserPermission
            }.Serialize();
            var response = await _httpClient.PostAsync("/user-file-permissions/",
                new StringContent(data, Encoding.UTF8, "application/json"));

            return response.StatusCode == (HttpStatusCode) 200;
        }

        public async Task<List<RemoteFilePermission>> GetFilePermissions() {
            try {
                var response = await _httpClient.GetAsync("/user-file-permissions");

                if (response.StatusCode == (HttpStatusCode) 200) {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Logger.Log("[CloudApi]", "GetRemoteFilePermissions", responseContent.Truncate(100, "..."),
                        logLevel: LogLevel.Verbose);
                    var responseModels = responseContent.Deserialize<List<RemoteFilePermission>>();
                    return responseModels;
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.GetRemoteFilePermissions:", ex.ToString(),
                    logLevel: LogLevel.Error);
            }

            return null;
        }
        
        public async Task<RemoteFilePermission> GetFilePermission(string remoteFileId) {
            try {
                var response = await _httpClient.GetAsync($"/user-file-permissions/{remoteFileId}");

                if (response.StatusCode == (HttpStatusCode) 200) {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Logger.Log("[CloudApi]", "GetRemoteFilePermission", responseContent.Truncate(100, "..."),
                        logLevel: LogLevel.Verbose);
                    var responseModel = responseContent.Deserialize<RemoteFilePermission>();
                    return responseModel;
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.GetRemoteFilePermissions:", ex.ToString(),
                    logLevel: LogLevel.Error);
            }

            return null;
        }

        public async Task<bool> DeleteFilePermission(string permissionId) {
            try {
                var response = await _httpClient.DeleteAsync($"/user-file-permissions/{permissionId}/");
                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log("[CloudApi]", "DeleteRemoteFilePermission:", response.StatusCode, responseContent);

                return response.StatusCode == (HttpStatusCode) 200;
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.DeleteRemoteFilePermission:", ex.ToString(),
                    logLevel: LogLevel.Error);
                return false;
            }
        }
        
        public async Task<bool> AcceptFilePermission(string permissionId) {
            try {
                var response = await _httpClient.PutAsync($"/user-file-permissions/{permissionId}/", null);
                return response.StatusCode == (HttpStatusCode) 200;
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.AcceptRemoteFilePermission:", ex.ToString(),
                    logLevel: LogLevel.Error);
                return false;
            }
        }

        #endregion

        #region FileController

        public async Task<string> PostFile(RemoteFileModel remoteFileModel) {
            try {
                var data = remoteFileModel.ToJson(false);
                var response = await _httpClient.PostAsync("/files/",
                    new StringContent(data, Encoding.UTF8, "application/json"));
                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log("[CloudApi]", "CreateRemoteFile:", response.StatusCode,
                    responseContent.Truncate(100, "..."));
                if (response.StatusCode == (HttpStatusCode) 200) {
                    var cloudResponse = responseContent.Deserialize<RemoteFileModel>();
                    return cloudResponse.RemoteId;
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.CreateRemoteFile:", ex.ToString(),
                    logLevel: LogLevel.Error);
            }

            return null;
        }

        public async Task<bool> PutFile(RemoteFileModel remoteFileModel) {
            try {
                var data = remoteFileModel.ToJson(true);
                var response = await _httpClient.PutAsync($"/files/{remoteFileModel.RemoteId}",
                    new StringContent(data, Encoding.UTF8, "application/json"));
                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log("[CloudApi]", "UpdateRemoteFile:", response.StatusCode, responseContent);

                return response.StatusCode == (HttpStatusCode) 200;
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.UpdateRemoteFile:", ex.ToString(),
                    logLevel: LogLevel.Error);
                return false;
            }
        }

        public async Task<RemoteFileModel> GetFile(string remoteFileId) {
            try {
                var response = await _httpClient.GetAsync($"/files/{remoteFileId}");
                if (response.StatusCode == (HttpStatusCode) 200) {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    // Logger.Log("[CloudApi]", "GetRemoteFile:", responseContent.Truncate(100, "..."), logLevel: LogLevel.Verbose);
                    var responseModel = responseContent.Deserialize<RemoteFileModel>();
                    return responseModel;
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.GetRemoteFile:", ex.ToString(),
                    logLevel: LogLevel.Error);
            }

            return null;
        }

        #endregion

        #region FilePluginController

        public async Task<(bool, string)> PostComponent(RemoteComponentModel remoteComponentModel) {
            try {
                var data = remoteComponentModel.Serialize();
                var response = await _httpClient.PostAsync($"/components/", new StringContent(data, Encoding.UTF8, "application/json"));
                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log("[CloudApi]", "CreateRemotePlugin Raw:", response.StatusCode, responseContent);
                if (response.StatusCode == (HttpStatusCode) 200) {
                    var cloudResponse = responseContent.Deserialize<RemoteComponentModel>();
                    return (true, cloudResponse.Id);
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.CreateRemotePlugin:", ex.ToString(), logLevel: LogLevel.Error);
            }
        
            return (false, "failed lol");
        }

        public async Task<bool> PutComponent(RemoteComponentModel remoteComponentModel) {
            try {
                var data = remoteComponentModel.Serialize();
                var response = await _httpClient.PutAsync($"/components/{remoteComponentModel.Id}", new StringContent(data, Encoding.UTF8, "application/json"));
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Logger.Log("[CloudApi]", "UpdateRemotePlugin:", response.StatusCode, responseContent);
                return response.StatusCode == (HttpStatusCode) 200;
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.UpdateRemotePlugin:", ex.ToString(), logLevel: LogLevel.Error);
                return false;
            }
        }

        public async Task<List<RemoteComponentModel>> GetRemoteComponents(string remoteFileId) {
            try {
                var response = await _httpClient.GetAsync($"/files/{remoteFileId}/components");
                if (response.StatusCode == (HttpStatusCode) 200) {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Logger.Log("[CloudApi]", "GetRemotePlugins:", responseContent.Truncate(100, "..."));
                    var responseModels = responseContent.Deserialize<List<RemoteComponentModel>>();
                    Logger.Log("[CloudApi]", "GetRemotePlugins Result:", responseModels.Count);
                    return responseModels;
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.GetRemotePlugins:", ex.ToString(),
                    logLevel: LogLevel.Error);
            }

            return null;
        }

        public async Task<List<RemoteFilePermission>> GetRemotePermissions(string remoteFileId) {
            try {
                var response = await _httpClient.GetAsync($"/files/{remoteFileId}/permissions");
                if (response.StatusCode == (HttpStatusCode) 200) {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Logger.Log("[CloudApi]", "GetRemotePermissions:", responseContent.Truncate(100, "..."));
                    var responseModels = responseContent.Deserialize<List<RemoteFilePermission>>();
                    Logger.Log("[CloudApi]", "GetRemotePermissions Result:", responseModels.Count);
                    return responseModels;
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.GetRemotePermissions:", ex.ToString(),
                    logLevel: LogLevel.Error);
            }

            return null;
        }

        public async Task<bool> DeleteRemoteComponent(string remoteFileId, string remotePluginId) {
            try {
                var response = await _httpClient.DeleteAsync($"/files/{remoteFileId}/components/{remotePluginId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                Logger.Log("[CloudApi]", "DeleteRemotePlugin:", response.StatusCode, responseContent);
                return response.StatusCode == (HttpStatusCode) 200;
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.DeleteRemotePlugin:", ex.ToString(),
                    logLevel: LogLevel.Error);
                return false;
            }
        }
        
        public async Task<RemoteComponentModel> GetRemoteComponent(string remoteComponentId) {
            try {
                var response = await _httpClient.GetAsync($"/components/{remoteComponentId}");
                if (response.StatusCode == (HttpStatusCode) 200) {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseModel = responseContent.Deserialize<RemoteComponentModel>();
                    return responseModel;
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.GetRemoteComponent:", ex.ToString(),
                    logLevel: LogLevel.Error);
            }

            return null;
        }

        #endregion

        #region ImageController

        public async Task<string> UploadImage(Stream imageStream, string remoteFileId = "") {
            try {
                var multiFormContent = remoteFileId != ""
                    ? new MultipartFormDataContent {
                        {new StreamContent(imageStream), "file", "componentFile.png"},
                        {new StringContent(remoteFileId), "fileId"}
                    }
                    : new MultipartFormDataContent {
                        {new StreamContent(imageStream), "file", "componentFile.png"}
                    };

                var response = await _httpClient.PostAsync("/images", multiFormContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                Logger.Log("[CloudApi]", "CreateRemoteImage:", response.StatusCode, responseContent);
                if (response.StatusCode == (HttpStatusCode) 200 ||
                    response.StatusCode == (HttpStatusCode) 204 && responseContent != null) {
                    return responseContent.Deserialize<ImageCreationResponse>().ImageId;
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.UploadImage:", ex.ToString(), logLevel: LogLevel.Error);
            }

            return null;
        }

        public async Task<bool> DownloadImage(string remoteImageId, string outPath) {
            try {
                Logger.Log("[CloudApi]", "CloudApi.DownloadImage: remoteImageId =", remoteImageId.Replace("\"", ""));
                var response = await _httpClient.GetAsync($"/images/{remoteImageId.Replace("\"", "")}");
                if (response.StatusCode == (HttpStatusCode) 200) {
                    var responseContent = await response.Content.ReadAsByteArrayAsync();
                    await LocalFileHelper.WriteFileAsync(outPath, responseContent);
                    return true;
                } else {
                    Logger.Log("[CloudApi]", "CloudApi.GetRemoteImage: Image not Found", logLevel: LogLevel.Warning);
                }
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.GetRemoteImage:", ex.ToString(),
                    logLevel: LogLevel.Error);
            }

            return false;
        }

        public async Task<bool> UploadProfilePicture(Stream imageStream) {
            try {
                var multiFormContent = new MultipartFormDataContent {
                    {new StreamContent(imageStream), "file", "componentFile.png"}
                };

                var response = await _httpClient.PostAsync($"/images/profilepicture", multiFormContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                Logger.Log("[CloudApi]", "CreateRemoteImage:", response.StatusCode, responseContent);
                return response.StatusCode == (HttpStatusCode) 200;
            } catch (Exception ex) {
                Logger.Log("[CloudApi]", "Exception in CloudApi.UploadImage:", ex.ToString(), logLevel: LogLevel.Error);
            }

            return false;
        }

        #endregion
    }
}