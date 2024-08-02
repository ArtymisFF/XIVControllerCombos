using System;

namespace XIVControllerCombos
{
    //TODO: reorganize the numbers lol lmao
    [Flags]
    public enum CustomComboPreset : long
    {
        None = 0,

        // VIPER
        [CustomComboInfo("Steel Fangs Combo", "Steel fangs goodies.", 41)]
		ViperSteelFangsCombo = 1L << 1,

        [CustomComboInfo("Steel Maw Combo", "Like steel fangs but AOE", 41)]
        ViperSteelMawCombo = 1L << 2,

        [CustomComboInfo("Coil Combo", "Replaces Coil actions with Twinfang Bite and Twinblood Bite depending on which one is used.", 41)]
        ViperCoilCombo = 1L << 3,

		[CustomComboInfo("Den Combo", "Replaces Den actions with Twinfang Thresh and Twinblood Thresh.", 41)]
		ViperDenCombo = 1L << 4,

		[CustomComboInfo("Generation Combo", "Replaces Steel Maw or Steel Fangs with Generation Combo.", 41)]
		ViperGenerationCombo = 1L << 5,

		// PICTOMANCER
		[CustomComboInfo("Motif into Muse", "Turns motifs into muses when activated.", 42)]
		PictomancerMotifIntoMuse = 1L << 6,

		[CustomComboInfo("Subtractive Pallet Combo", "Turns subtractive pallet into Blizard in Cyan or Blizzard II in Cyan combo", 42)]
		PictomancerSubtractivePalletCombo = 1L << 7,

		[CustomComboInfo("Hammer Stamp Fire Combo Injection", "Replaces Fire & Fire II combos with Hammerstamp when proc'd", 42)]
		PictomancerHammerStampInjection = 1L << 8,

		[CustomComboInfo("Holy Mog of White & Black", "Holy in White with Mog of the Ages when proc'd. Comet in Black when using subtractive pallet.", 42)]
		PictomancerHolyMogOfWhite = 1L << 9,

		// DRAGOON
		[CustomComboInfo("Jump + Mirage Dive", "Replace (High) Jump with Mirage Dive when Dive Ready", 22)]
        DragoonJumpFeature = 1L << 10,

        [CustomComboInfo("Coerthan Torment Combo", "Replace Coerthan Torment with its combo chain", 22)]
        DragoonCoerthanTormentCombo = 1L << 11,

        [CustomComboInfo("Full Chaos Combo", "Replace Full Thrust with its combo chain and swaps to Chaos Thrust chain if Power Surge buff is timing out soon.", 22)]
        DragoonFullChaosCombo = 1L << 12,

        // DARK KNIGHT
        [CustomComboInfo("Souleater Combo", "Replace Souleater with its combo chain", 32)]
        DarkSouleaterCombo = 1L << 13,

        [CustomComboInfo("Stalwart Soul Combo", "Replace Stalwart Soul with its combo chain", 32)]
        DarkStalwartSoulCombo = 1L << 14,

        // PALADIN
        [CustomComboInfo("Royal Authority Combo", "Replace Royal Authority/Rage of Halone with its combo chain", 19)]
        PaladinRoyalAuthorityCombo = 1L << 15,

        [CustomComboInfo("Atonement After Royal Authority", "Takes Royal Authority combo even further by applying atonement combo to the end.", 19)]
        PaladinAtonementCombo = 1L << 16,

        [CustomComboInfo("Prominence Combo", "Replace Prominence with its combo chain", 19)]
        PaladinProminenceCombo = 1L << 17,

        [CustomComboInfo("Requiescat Confiteor", "Replace Requiescat with Confiteor while under the effect of Requiescat", 19)]
        PaladinRequiescatCombo = 1L << 18,

        // WARRIOR
        [CustomComboInfo("Storms Path Combo", "Replace Storms Path with its combo chain", 21)]
        WarriorStormsPathCombo = 1L << 19,

        [CustomComboInfo("Storms Eye Combo", "Replace Storms Eye with its combo chain", 21)]
        WarriorStormsEyeCombo = 1L << 20,

        [CustomComboInfo("Mythril Tempest Combo", "Replace Mythril Tempest with its combo chain", 21)]
        WarriorMythrilTempestCombo = 1L << 21,

        [CustomComboInfo("IR to Primal Rend", "Replace Inner Release with Primal Rend when Primal Rend Ready", 21)]
        WarriorIRCombo = 1L << 22,

		// SAMURAI
		[CustomComboInfo("Single Target Combo", "Replace Hakaze with the relevant skill based on current combo progression & buffs.", 34)]
		SamuraiSingleTargetCombo = 1L << 23,

		[CustomComboInfo("AOE Combo", "Replace Fuga with the relevant skill based on current combo progression & buffs.", 34)]
		SamuraiAoeCombo = 1L << 24,

		[CustomComboInfo("Yukikaze Combo", "Replace Yukikaze with its combo chain", 34)]
        SamuraiYukikazeCombo = 1L << 25,

        [CustomComboInfo("Gekko Combo", "Replace Gekko with its combo chain", 34)]
        SamuraiGekkoCombo = 1L << 26,

        [CustomComboInfo("Kasha Combo", "Replace Kasha with its combo chain", 34)]
        SamuraiKashaCombo = 1L << 27,

        [CustomComboInfo("Mangetsu Combo", "Replace Mangetsu with its combo chain", 34)]
        SamuraiMangetsuCombo = 1L << 28,

        [CustomComboInfo("Oka Combo", "Replace Oka with its combo chain", 34)]
        SamuraiOkaCombo = 1L << 29,

        [CustomComboInfo("Iaijutsu into Tsubame", "Replace Iaijutsu with Tsubame after using an Iaijutsu", 34)]
        SamuraiTsubameCombo = 1L << 30,

        [CustomComboInfo("Ogi Namikiri Combo", "Replace Ikishoten with Ogi Namiki and Kaeshi Namikiri when appropriate", 34)]
        SamuraiOgiCombo = 1L << 31,


        // NINJA
        [CustomComboInfo("Armor Crush Combo", "Replace Armor Crush with its combo chain", 30)]
        NinjaArmorCrushCombo = 1L << 32,

        [CustomComboInfo("Aeolian Edge Combo", "Replace Aeolian Edge with its combo chain", 30)]
        NinjaAeolianEdgeCombo = 1L << 33,

        [CustomComboInfo("Hakke Mujinsatsu Combo", "Replace Hakke Mujinsatsu with its combo chain", 30)]
        NinjaHakkeMujinsatsuCombo = 1L << 34,

        // GUNBREAKER
        [CustomComboInfo("Solid Barrel Combo", "Replace Solid Barrel with its combo chain", 37)]
        GunbreakerSolidBarrelCombo = 1L << 35,

        [CustomComboInfo("Gnashing Fang Continuation", "Put Continuation moves on Gnashing Fang when appropriate", 37)]
        GunbreakerGnashingFangCont = 1L << 36,

        [CustomComboInfo("Burst Strike Continuation", "Put Continuation moves on Burst Strike when appropriate", 37)]
        GunbreakerBurstStrikeCont = 1L << 37,

        [CustomComboInfo("Demon Slaughter Combo", "Replace Demon Slaughter with its combo chain", 37)]
        GunbreakerDemonSlaughterCombo = 1L << 38,

        // MACHINIST
        /*
        [CustomComboInfo("(Heated) Shot Combo", "Replace either form of Clean Shot with its combo chain", 31)]
        MachinistMainCombo = 1L << 39,

        [CustomComboInfo("Spread Shot Heat", "Replace Spread Shot or Scattergun with Auto Crossbow when overheated", 31)]
        MachinistSpreadShotFeature = 1L << 40,

        [CustomComboInfo("Heat Blast when overheated", "Replace Hypercharge with Heat Blast when overheated", 31)]
        MachinistOverheatFeature = 1L << 41,
        */

        // BLACK MAGE
        [CustomComboInfo("Base Rotation", "Changes Fire & Blizzard depending on the context of mana, astral fire/umbral ice & umbral hearts", 25)]
        BlackMageBaseRotation = 1L << 39,

        [CustomComboInfo("Enochian Stance Switcher", "Change Fire 4 and Blizzard 4 to the appropriate element depending on stance, as well as Flare and Freeze", 25)]
        BlackEnochianFeature = 1L << 40,

        [CustomComboInfo("(Between the) Ley Lines", "Change Ley Lines into BTL when Ley Lines is active", 25)]
        BlackLeyLines = 1L << 63,

        // ASTROLOGIAN
        //[CustomComboInfo("Draw on Play", "Play turns into Draw when no card is drawn, as well as the usual Play behavior", 33)]
        //AstrologianCardsOnDrawFeature = 1L << 44,

        // SUMMONER

        [CustomComboInfo("ED Fester", "Change Fester into Energy Drain when out of Aetherflow stacks", 27)]
        SummonerEDFesterCombo = 1L << 41,

        [CustomComboInfo("ES Painflare", "Change Painflare into Energy Syphon when out of Aetherflow stacks", 27)]
        SummonerESPainflareCombo = 1L << 42,
        
        // SCHOLAR
        [CustomComboInfo("Seraph Fey Blessing/Consolation", "Change Fey Blessing into Consolation when Seraph is out", 28)]
        ScholarSeraphConsolationFeature = 1L << 43,

        [CustomComboInfo("ED Aetherflow", "Change Energy Drain into Aetherflow when you have no more Aetherflow stacks", 28)]
        ScholarEnergyDrainFeature = 1L << 44,

		// DANCER
		[CustomComboInfo("Single Target & AoE Rotation with Standard Step Combo", "Consolidates single target & aoe rotation (including procs) as well as turning standard step into the appropriate dance move when activated.", 38)]
		DancerSingleAoERotation = 1L << 45,

        //[CustomComboInfo("Standard Step Combo", "Turns standard step into the appropriate dance move when activated.", 38)]
        //DancerStandardStepCombo = 1L << 46,
		//[CustomComboInfo("AoE GCD procs", "DNC AoE procs turn into their normal abilities when not procced", 38)]
  //      DancerAoeGcdFeature = 1L << 46,

        [CustomComboInfo("Fan Dance Combos", "Change Fan Dance and Fan Dance 2 into Fan Dance 3 while flourishing", 38)]
        DancerFanDanceCombo = 1L << 47,

        [CustomComboInfo("Fan Dance IV", "Change Flourish into Fan Dance IV while flourishing", 38)]
        DancerFanDance4Combo = 1L << 48,

        [CustomComboInfo("Devilment into Starfall", "Change Devilment into Starfall Dance while under the effect of Flourishing Starfall", 38)]
        DancerDevilmentCombo = 1L << 62,

        // WHITE MAGE
        [CustomComboInfo("Solace into Misery", "Replaces Afflatus Solace with Afflatus Misery when Misery is ready to be used", 24)]
        WhiteMageSolaceMiseryFeature = 1L << 49,

        [CustomComboInfo("Rapture into Misery", "Replaces Afflatus Rapture with Afflatus Misery when Misery is ready to be used", 24)]
        WhiteMageRaptureMiseryFeature = 1L << 50,

        // BARD
        [CustomComboInfo("Heavy Shot into Straight Shot", "Replaces Heavy Shot/Burst Shot with Straight Shot/Refulgent Arrow when procced", 23)]
        BardStraightShotUpgradeFeature = 1L << 51,

        [CustomComboInfo("Quick Knock into Wide Volley", "Replaces Quick Knock with Wide Volley when procced.", 23)]
        BardQuickKnockUpgradeFeature = 1L << 52,

        [CustomComboInfo("Quick Nock into Shadowbite", "Replaces Quick Nock/Ladonsbite with Shadowbite when procced", 23)]
        BardAoEUpgradeFeature = 1L << 53,

        // MONK
        // you get nothing, you lose, have a nice day etc

        // RED MAGE
        [CustomComboInfo("Red Mage Single Target Combo", "Replaces VerThunder and VerAero with VT/VA or Jolt based on mana and dualcast proc", 35)]
        RedMageSingleCombo = 1L << 54,

        [CustomComboInfo("Red Mage AoE Combo", "Replaces Scatter with VerThunder 2 or VerAero 2 based on mana dualcast proc", 35)]
        RedMageAoECombo = 1L << 55,

        [CustomComboInfo("Redoublement combo", "Replaces Redoublement with its combo chain, following enchantment rules", 35)]
        RedMageMeleeCombo = 1L << 56,
        /*
        [CustomComboInfo("Verproc into Jolt", "Replaces Verstone/Verfire with Jolt/Scorch when no proc is available.", 35)]
        RedMageVerprocCombo = 1L << 61,
        */

        // REAPER
        [CustomComboInfo("Reaper Rotation", "Replace Slice with its combo chain and spinning scythe with its combo chain.", 39)]
        ReaperRotation = 1L << 57,

        //[CustomComboInfo("Scythe Combo", "Replace Spinning Scythe with its combo chain.", 39)]
        //ReaperScytheCombo = 1L << 58,

        [CustomComboInfo("Double Regress", "Regress always replaces both Hell's Egress and Hell's Ingress.", 39)]
        ReaperRegressFeature = 1L << 59,

        [CustomComboInfo("Enshroud Combo", "Replace Enshroud with Communio while you are Enshrouded.", 39)]
        ReaperEnshroudCombo = 1L << 60,

        [CustomComboInfo("Arcane Circle Combo", "Replace Arcane Circle with Plentiful Harvest while you have Immortal Sacrifice.", 39)]
        ReaperArcaneFeature = 1L << 61,
    }

    public class CustomComboInfoAttribute : Attribute
    {
        internal CustomComboInfoAttribute(string fancyName, string description, byte classJob)
        {
            FancyName = fancyName;
            Description = description;
            ClassJob = classJob;
        }

        public string FancyName { get; }
        public string Description { get; }
        public byte ClassJob { get; }

    }
}
