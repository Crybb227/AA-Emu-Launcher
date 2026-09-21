using System;
using System.IO;
using System.Text;
using DiscordRPC;
using Newtonsoft.Json;

namespace AAEmu.Launcher
{
    /// <summary>
    /// Watches the status JSON file written by the server's mod-discord-presence module
    /// and mirrors it onto Discord Rich Presence for as long as JasonWoW is running.
    /// The server never talks to Discord directly; this is the only piece that does.
    /// </summary>
    public class DiscordPresenceService : IDisposable
    {
        // Registered at https://discord.com/developers/applications for the JasonWoW server.
        private const string DiscordApplicationId = "0000000000000000000";

        private class PresenceStatus
        {
            [JsonProperty("online")]
            public bool Online { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; } = string.Empty;

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("class")]
            public string Class { get; set; } = string.Empty;

            [JsonProperty("race")]
            public string Race { get; set; } = string.Empty;

            [JsonProperty("zone")]
            public string Zone { get; set; } = string.Empty;

            [JsonProperty("guild")]
            public string Guild { get; set; } = string.Empty;
        }

        private DiscordRpcClient client;
        private FileSystemWatcher watcher;
        private readonly string statusFilePath;
        private readonly DateTime sessionStart = DateTime.UtcNow;
        private string lastFileContents;

        public bool IsRunning => client != null;

        public DiscordPresenceService(string statusFilePath)
        {
            this.statusFilePath = statusFilePath;
        }

        public void Start()
        {
            if (client != null || string.IsNullOrWhiteSpace(statusFilePath))
                return;

            client = new DiscordRpcClient(DiscordApplicationId);
            client.Initialize();

            ApplyStatusFile();

            try
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(statusFilePath));
                var fileName = Path.GetFileName(statusFilePath);
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                    return;

                Directory.CreateDirectory(directory);

                watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                watcher.Changed += (sender, args) => ApplyStatusFile();
                watcher.Created += (sender, args) => ApplyStatusFile();
            }
            catch (Exception)
            {
                // Rich Presence is a nice-to-have; a watcher failure should never affect play.
                watcher?.Dispose();
                watcher = null;
            }
        }

        private void ApplyStatusFile()
        {
            if (client == null)
                return;

            string contents;
            try
            {
                using (var stream = new FileStream(statusFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    contents = reader.ReadToEnd();
                }
            }
            catch (IOException)
            {
                return; // File is mid-write (server does a rename, but be defensive); try again next event.
            }
            catch (Exception)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(contents) || contents == lastFileContents)
                return;

            lastFileContents = contents;

            PresenceStatus status;
            try
            {
                status = JsonConvert.DeserializeObject<PresenceStatus>(contents);
            }
            catch (JsonException)
            {
                return;
            }

            if (status == null || !status.Online)
            {
                client.ClearPresence();
                return;
            }

            var details = string.IsNullOrEmpty(status.Class)
                ? $"Level {status.Level}"
                : $"Level {status.Level} {status.Race} {status.Class}".Trim();
            var state = string.IsNullOrEmpty(status.Zone) ? "In JasonWoW" : status.Zone;

            client.SetPresence(new RichPresence
            {
                Details = details,
                State = state,
                Timestamps = new Timestamps { Start = sessionStart },
                Assets = new Assets
                {
                    LargeImageKey = "jasonwow_logo",
                    LargeImageText = "JasonWoW"
                }
            });
        }

        public void Stop()
        {
            watcher?.Dispose();
            watcher = null;

            client?.ClearPresence();
            client?.Dispose();
            client = null;
            lastFileContents = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
