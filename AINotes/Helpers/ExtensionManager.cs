using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using AINotes.Models;
using Helpers;
using Helpers.Extensions;

namespace AINotes.Helpers {
    public class ExtensionManager {
        private bool _initialized;
        public bool IsInitialized() => _initialized;

        public readonly AppExtensionCatalog Catalog;
        public ObservableCollection<Extension> Extensions { get; } = new ObservableCollection<Extension>();
        public event Action ExtensionsChanged;

        public ExtensionManager() {
            Catalog = AppExtensionCatalog.Open(Configuration.ExtensionContractName);

            Catalog.PackageInstalled += OnPackageInstalled;
            Catalog.PackageUpdated += OnPackageUpdated;
            Catalog.PackageUninstalling += OnPackageUninstalling;
            Catalog.PackageUpdating += OnPackageUpdating;
            Catalog.PackageStatusChanged += OnPackageStatusChanged;

            Extensions.CollectionChanged += (_, _) => ExtensionsChanged?.Invoke();
        }

        public IEnumerable<ExtensionModel> GetExtensionModels() {
            if (!_initialized) throw new Exception("Not Initialized");
            return Extensions.Select(ext => ext.ToExtensionModel());
        }

        public ExtensionModel GetExtensionModel(string id) {
            if (!_initialized) throw new Exception("Not Initialized");
            return Extensions.FirstOrDefault(e => e.UniqueId == id)?.ToExtensionModel();
        }


        public async Task InitializeAsync() {
            if (_initialized) throw new Exception("Already Initialized");
            _initialized = true;
            
            var extensions = await Catalog.FindAllAsync();
            foreach (var ext in extensions) {
                await LoadExtension(ext);
            }
        }

        private async void OnPackageInstalled(AppExtensionCatalog sender, AppExtensionPackageInstalledEventArgs args) {
            foreach (var ext in args.Extensions) {
                await LoadExtension(ext);
            }
        }

        private async void OnPackageUpdated(AppExtensionCatalog sender, AppExtensionPackageUpdatedEventArgs args) {
            foreach (var ext in args.Extensions) {
                await LoadExtension(ext);
            }
        }

        private void OnPackageUpdating(AppExtensionCatalog sender, AppExtensionPackageUpdatingEventArgs args) {
            UnloadExtensions(args.Package);
        }

        private void OnPackageUninstalling(AppExtensionCatalog sender, AppExtensionPackageUninstallingEventArgs args) {
            RemoveExtensions(args.Package);
        }

        private void OnPackageStatusChanged(AppExtensionCatalog sender, AppExtensionPackageStatusChangedEventArgs args) {
            if (!args.Package.Status.VerifyIsOK()) {
                if (args.Package.Status.PackageOffline) {
                    UnloadExtensions(args.Package);
                } else if (args.Package.Status.Servicing || args.Package.Status.DeploymentInProgress) { } else {
                    RemoveExtensions(args.Package);
                }
            } else {
                LoadExtensions(args.Package);
            }
        }

        public async Task LoadExtension(AppExtension ext) {
            var identifier = ext.AppInfo.AppUserModelId + "!" + ext.Id;

            var existingExt = Extensions.FirstOrDefault(e => e.UniqueId == identifier);

            if (existingExt == null) {
                var properties = await ext.GetExtensionPropertiesAsync() as PropertySet;
                var logoStream = await ext.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(1, 1)).OpenReadAsync();
                var logoBytes = logoStream.AsStreamForRead().ReadAllBytes();

                var newExtension = new Extension(ext, properties, logoBytes);
                Extensions.Add(newExtension);

                newExtension.MarkAsLoaded();
            } else {
                existingExt.Unload();
                await existingExt.Update(ext);
            }
        }

        public void LoadExtensions(Package package) {
            foreach (var e in Extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName)) {
                e.MarkAsLoaded();
            }
        }

        public void UnloadExtensions(Package package) {
            foreach (var e in Extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName)) {
                e.Unload();
            }
        }

        public void RemoveExtensions(Package package) {
            foreach (var e in Extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName)) {
                e.Unload();
                Extensions.Remove(e);
            }
        }

        public Task DisableExtensionAsync(ExtensionModel extModel) {
            throw new NotImplementedException();
        }

        public async Task RemoveExtensionAsync(ExtensionModel extModel) => await Catalog.RequestRemovePackageAsync(((Extension)extModel.Extension).AppExtension.Package.Id.FullName);

        public async Task<Dictionary<string, string>> InvokeExtensionActionAsync(ExtensionModel extensionModel, params (string, string)[] keyValuePairs) {
            var message = new ValueSet();
            foreach (var (key, value) in keyValuePairs) {
                message.Add(key, value);
            }
            return await ((Extension) extensionModel.Extension).Invoke(message);
        }
    }

    public class Extension : INotifyPropertyChanged {
        private readonly object _unloadLock = new object();

        public PropertySet Properties;
        public string ServiceName;
        
        public string UniqueId { get; }
        public byte[] LogoBytes;

        public bool Offline { get; private set; }
        public bool Loaded { get; private set; }
        public AppExtension AppExtension { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public Extension(AppExtension ext, PropertySet properties, byte[] logoBytes) {
            AppExtension = ext;
            Properties = properties;
            LogoBytes = logoBytes;
            
            Loaded = false;
            Offline = false;

            ServiceName = null;
            if (Properties != null) {
                if (Properties.ContainsKey("Service")) {
                    var serviceProperty = Properties["Service"] as PropertySet;
                    ServiceName = serviceProperty?["#text"].ToString();
                }
            }

            UniqueId = ext.AppInfo.AppUserModelId + "!" + ext.Id;
        }

        public ExtensionModel ToExtensionModel() {
            return new ExtensionModel(this, AppExtension.DisplayName, AppExtension.Description, UniqueId, LogoBytes);
        }
        
        public async Task<Dictionary<string, string>> Invoke(ValueSet message) {
            if (!Loaded) {
                Logger.Log("Service not loaded");
                return null;
            }
            try {
                using (var connection = new AppServiceConnection()) {
                    connection.AppServiceName = ServiceName;
                    connection.PackageFamilyName = AppExtension.Package.Id.FamilyName;

                    var status = await connection.OpenAsync();
                    connection.ServiceClosed += (_, _) => Logger.Log("ServiceClosed");
                    if (status != AppServiceConnectionStatus.Success) {
                        Logger.Log("Failed App Service Connection");
                    } else {
                        var response = await connection.SendMessageAsync(message);
                        Logger.Log("Response Status:", response.Status);
                        Logger.Log("Response Message:", response.Message);
                        if (response.Status == AppServiceResponseStatus.Success) {
                            var result = new Dictionary<string, string>();
                            foreach (var (key, value) in response.Message) {
                                result.Add(key, (string) value);
                            }
                            return result;
                        }
                    }
                }
            } catch (Exception) {
                Logger.Log("Calling the App Service failed");
            }

            return null;
        }

        public async Task Update(AppExtension ext) {
            var identifier = ext.AppInfo.AppUserModelId + "!" + ext.Id;
            if (identifier != UniqueId) return;

            Properties = await ext.GetExtensionPropertiesAsync() as PropertySet;
            LogoBytes = (await ext.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(1, 1)).OpenReadAsync()).AsStreamForRead().ReadAllBytes();
            AppExtension = ext;

            ServiceName = null;
            if (Properties != null) {
                if (Properties.ContainsKey("Service")) {
                    var serviceProperty = Properties["Service"] as PropertySet;
                    ServiceName = serviceProperty?["#text"].ToString();
                }
            }

            MarkAsLoaded();
        }

        public void MarkAsLoaded() {
            Loaded = true;
            Offline = false;
        }

        public void Unload() {
            lock (_unloadLock) {
                if (!Loaded) return;
                if (!AppExtension.Package.Status.VerifyIsOK() && !AppExtension.Package.Status.PackageOffline) {
                    Offline = true;
                }

                Loaded = false;
            }
        }
        
        public void Enable() => MarkAsLoaded();
        public void Disable() => Unload();
    }
}