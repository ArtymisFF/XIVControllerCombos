﻿namespace XIVControllerCombos.JobActions
{
    public static class RPR
    {
        public const byte JobID = 39;

        public const uint
            // Single Target
            Slice = 24373,
            WaxingSlice = 24374,
            InfernalSlice = 24375,
            // AoE
            SpinningScythe = 24376,
            NightmareScythe = 24377,
            // Shroud
            Enshroud = 24394,
            Communio = 24398,
            Perfectio = 36973,

            BloodStalk = 24389,
            UnveiledGibbet = 1,
            UnveiledGallows = 1,
            GrimSwathe = 24392,
            Gluttony = 24393,
            Gibbet = 24382,
            Gallows = 24383,
            VoidReaping = 24395,
            CrossReaping = 24396,
            Sacrificium = 36969,

            Egress = 24402,
            Ingress = 24401,
            Regress = 24403,

            ArcaneCircle = 24405,
            PlentifulHarvest = 24385;

        public static class Buffs
        {
            public const ushort
                Enshrouded = 2593,
                Threshold = 2595,
                ImSac1 = 2592,
                PerfectioParata = 3860,
                ImSac2 = 3204,
                EnhancedGibbet = 2588,
                EnhancedGallows = 2589,
                EnhancedVoidReaping = 2590,
                EnhancedCrossReaping = 2591;
        }

        public static class Debuffs
        {
            public const ushort
                Placeholder = 0;
        }

        public static class Levels
        {
            public const byte
                Slice = 1,
                WaxingSlice = 5,
                SpinningScythe = 25,
                InfernalSlice = 30,
                NightmareScythe = 45,
                Enshroud = 80,
                Communio = 90;
        }
    }
}