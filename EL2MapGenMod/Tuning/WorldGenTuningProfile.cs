namespace EL2MapGenMod.Tuning
{
    internal static class WorldGenTuningProfile
    {
        // Map size scaling:
        // If WidthScale and HeightScale both use this percent, then AREA scales by (p/100)^2.
        // Example:
        // - 112% => 1.12 * 1.12 = 1.2544 (~ +25% area)
        // - 125% => 1.25 * 1.25 = 1.5625 (~ +56% area)
        public const int MapScalePercent = 112;
        
        // StartLandElevation MUST be 6, and the game will then start sea at (StartLandElevation - 1) => 5.
        public const sbyte StartLandElevationTarget = 6;

        // Ridges / verticality
        public const int MaxLandElevationRaise = 4;
        public const int ElevationGrowPercentBonus = 10;

        public const int RidgePresenceFloor = 75; // percent
        public const int RidgeMinElevationDelta = -1; // slightly easier to qualify as ridge candidate
        public const int RidgeMaxSizeBonus = 2;

        // Rivers
        public const int RiverPresenceFloor = 80; // percent
        public const int RiverSeedCountBonus = 20;
        public const int RiverMinCountBonus = 3;
        public const int RiverMaxCountBonus = 4;
        public const int RiverMaxLengthBonus = 3;
        public const int RiverSourcesMinDistanceDelta = -1;

        // Persistent water rule:
        // After the 3rd recession step, we clamp the sea to keep water on the map.
        public const int PersistentWaterClampFromRecessIndex = 3;
        public const int PersistentWaterMinSeaLevel = 2;
    }
}