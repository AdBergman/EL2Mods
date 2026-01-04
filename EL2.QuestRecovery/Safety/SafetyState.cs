namespace EL2.QuestRecovery.Safety
{
    internal enum SafetyMode
    {
        Unknown = 0,
        SinglePlayer = 1,
        Multiplayer = 2
    }

    internal static class SafetyState
    {
        internal static volatile SafetyMode Mode = SafetyMode.Unknown;

        internal static void SetSinglePlayer() => Mode = SafetyMode.SinglePlayer;
        internal static void SetMultiplayer() => Mode = SafetyMode.Multiplayer;
        internal static void SetUnknown() => Mode = SafetyMode.Unknown;

        internal static bool IsAllowed()
        {
            // fail-closed
            return Mode == SafetyMode.SinglePlayer;
        }
    }
}