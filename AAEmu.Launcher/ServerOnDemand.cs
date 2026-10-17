using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AAEmu.Launcher
{
    /// <summary>
    /// Public, read-only server-on-demand catalog contract. The launcher consumes
    /// this data; it never receives controller credentials or starts containers.
    /// Unknown future states are preserved in State instead of being collapsed.
    /// </summary>
    internal sealed class OnDemandCatalogPayload
    {
        [JsonProperty("games")] public List<OnDemandGamePayload> Games { get; set; }
    }

    internal sealed class OnDemandGamePayload
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("enabled")] public bool Enabled { get; set; }
        [JsonProperty("state")] public string State { get; set; }
        [JsonProperty("ready")] public bool Ready { get; set; }
        [JsonProperty("blockedBy")] public string BlockedBy { get; set; }
        [JsonProperty("remainingSeconds")] public int? RemainingSeconds { get; set; }
        [JsonProperty("connection")] public OnDemandConnectionPayload Connection { get; set; }
        [JsonProperty("wake")] public OnDemandWakePayload Wake { get; set; }

        internal bool HasKnownState { get { return OnDemandLifecycleStates.IsKnown(State); } }
    }

    internal sealed class OnDemandConnectionPayload
    {
        [JsonProperty("kind")] public string Kind { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
    }

    internal sealed class OnDemandWakePayload
    {
        [JsonProperty("method")] public string Method { get; set; }
        [JsonProperty("command")] public string Command { get; set; }
    }

    internal static class OnDemandLifecycleStates
    {
        internal const string Sleeping = "sleeping";
        internal const string Starting = "starting";
        internal const string Online = "online";
        internal const string Idle = "idle";
        internal const string Stopping = "stopping";
        internal const string BackingUp = "backing_up";
        internal const string Maintenance = "maintenance";
        internal const string Recovering = "recovering";
        internal const string RecoveryStopped = "recovery_stopped";
        internal const string Failed = "failed";
        internal const string Blocked = "blocked";
        internal const string Unavailable = "unavailable";

        private static readonly HashSet<string> Known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Sleeping, Starting, Online, Idle, Stopping, BackingUp, Maintenance,
            Recovering, RecoveryStopped, Failed, Blocked, Unavailable
        };

        internal static bool IsKnown(string state) { return !string.IsNullOrWhiteSpace(state) && Known.Contains(state); }
    }
}
