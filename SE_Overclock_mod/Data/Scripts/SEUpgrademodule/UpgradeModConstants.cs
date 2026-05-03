namespace SEUpgrademodule
{
    /// <summary>Network channels and message sizes for the upgrade session component.</summary>
    public static class UpgradeSessionConstants
    {
        public const ushort ChannelUpgradeSync = 5856;
        public const ushort ChannelConfigRequest = 5853;
        public const ushort ChannelConfigResponse = 5854;

        /// <summary>Payload: entityId (8) + four int32 levels (16) = 24 bytes.</summary>
        public const int UpgradeSyncMessageByteLength = 24;
    }

    /// <summary>Per-cockpit logic timing (approx. 30 s at 60 Hz).</summary>
    public static class UpgradeLogicConstants
    {
        public const int InventoryRescanFrameInterval = 1800;
    }

    public static class LoadBalancerConstants
    {
        public const int PrintRefreshPeriodFrames = 60;
        public const int NetworkSyncPeriodFrames = 100;
    }
}
