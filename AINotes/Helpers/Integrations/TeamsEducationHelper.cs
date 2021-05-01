using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Logger = Helpers.Logger;

namespace AINotes.Helpers.Integrations {
    public class TeamsEducationHelper {
        private static readonly IPublicClientApplication Pca;
        private static GraphServiceClient _graphClient;

        private const string ClientId = Configuration.LicenseKeys.MicrosoftGraph;
        
        // https://docs.microsoft.com/en-us/graph/permissions-reference#education-permissions
        private static readonly string[] AppScopes = {"EduAssignments.Read", "EduAssignments.ReadWrite", "EduAssignments.Read.All", "EduAssignments.ReadWrite.All"};

        static TeamsEducationHelper() {
            Pca = PublicClientApplicationBuilder.Create(ClientId).WithRedirectUri($"msal{ClientId}://auth").Build();
        }
        
        public static async Task Login() {
            var accounts = await Pca.GetAccountsAsync();
            var account = accounts.FirstOrDefault();

            AuthenticationResult result = null;
            try {
                result = await Pca.AcquireTokenSilent(AppScopes, account).ExecuteAsync();
                Logger.Log("Token:", result.AccessToken.Length);
            } catch {
                Logger.Log("AcquireTokenSilent failed");
            }

            if (result?.AccessToken == null) {
                Logger.Log("AcquireTokenInteractive");
                result = await Pca.AcquireTokenInteractive(AppScopes).WithAccount(account).ExecuteAsync();
                Logger.Log("Token:", result.AccessToken.Length);
            }
            
            _graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(requestMessage => {
                requestMessage.Headers.TryAddWithoutValidation("Authorization", "Bearer " + result.AccessToken);
                return Task.FromResult(0);
            }));
        }

        // https://github.com/microsoftgraph/msgraph-sdk-dotnet
        public static async Task<IEducationUserClassesCollectionWithReferencesPage> GetClasses() {
            return await _graphClient.Education.Me.Classes.Request().GetAsync();
        }
    }
}