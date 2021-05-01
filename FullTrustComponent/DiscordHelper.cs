using System;
using DiscordRPC;

namespace FullTrustComponent {
    public static class DiscordHelper {
        private const string ApplicationId = "598196846288044055";
        
        private static DiscordRpcClient _discordClient;
        private static ulong _startTimestamp;

        public static bool Connected => _discordClient?.IsInitialized ?? false;
        
        public static void Initialize() {
            Console.WriteLine(@"DiscordHelper - Initialize");
            _startTimestamp = (ulong) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            
            _discordClient = new DiscordRpcClient(ApplicationId, autoEvents: true);

            _discordClient.OnConnectionFailed += (sender, args) => Console.WriteLine(@"DiscordRpc: ConnectionFailed: {0}", args);
            
            _discordClient.OnReady += (sender, e) => Console.WriteLine(@"DiscordRpc: Received Ready from {0}#{1}", e.User.Username, e.User.Discriminator);
            _discordClient.OnPresenceUpdate += (sender, e) => Console.WriteLine(@"DiscordRpc: Received Update! {0}", e.Presence);
            
            _discordClient.OnJoin += (sender, e) => Console.WriteLine(@"DiscordRpc: Received OnJoin {0} | {1}", e.Secret, e.Type);
            _discordClient.OnSpectate += (sender, e) => Console.WriteLine(@"DiscordRpc: Received OnSpectate {0} | {1}", e.Secret, e.Type);
            _discordClient.OnJoinRequested += (sender, e) => Console.WriteLine(@"DiscordRpc: Received OnJoinRequested {0}#{1} | {2}", e.User.Username, e.User.Discriminator, e.Type);
            
            _discordClient.RegisterUriScheme();
            _discordClient.SetSubscription(EventType.Join | EventType.Spectate | EventType.JoinRequest);
            _discordClient.Initialize();
            
            // set state
            SetPresence("Editing Stuff", "Working on File");
        }

        public static void Deinitialize() => _discordClient.Deinitialize();
        public static void Dispose() => _discordClient.Dispose();

        public static void SetPresence(string details, string detailsState) => _discordClient.SetPresence(new RichPresence {
            Details = details,
            State = detailsState,
            Assets = new Assets {
                LargeImageKey = "logo",
                LargeImageText = "AINotes",
                // SmallImageKey = "logo",
                // SmallImageText = "Fancy Software"
            },
            Timestamps = new Timestamps {
                StartUnixMilliseconds = _startTimestamp,
            },
            // Party = new Party {
            //     Max = 10,
            //     Size = 1,
            //     ID = "DummyParty"
            // },
            // Secrets = new Secrets {
            //     JoinSecret = "JoinSecret",
            //     SpectateSecret = "SpectateSecret",
            // }
        });
    }
}