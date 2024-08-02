using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using XIVControllerCombos.JobActions;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentFreeCompanyProfile.FCProfile;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Diagnostics;

namespace XIVControllerCombos
{
    public class IconReplacer
    {
        public delegate ulong OnCheckIsIconReplaceableDelegate(uint actionID);

        public delegate ulong OnGetIconDelegate(byte param1, uint param2);

        private readonly IconReplacerAddressResolver Address;
        private readonly Hook<OnCheckIsIconReplaceableDelegate> checkerHook;
        private readonly IClientState clientState;

        private IntPtr comboTimer = IntPtr.Zero;
        private IntPtr lastComboMove = IntPtr.Zero;
        private IntPtr lastAction = IntPtr.Zero;

		//private unsafe ActionManager* AM;

		private readonly XIVComboConfiguration Configuration;

        private readonly Hook<OnGetIconDelegate> iconHook;

        private IGameInteropProvider HookProvider;
        private IJobGauges JobGauges;
        private IPluginLog PluginLog;


		private unsafe ActionManager* actionManager;

		private unsafe delegate int* getArray(long* address);
        private unsafe bool clientModulesLoaded = false;

        public IconReplacer(ISigScanner scanner, IClientState clientState, IDataManager manager, XIVComboConfiguration configuration, IGameInteropProvider hookProvider, IJobGauges jobGauges, IPluginLog pluginLog)
		{

			clientState.Login += LoadClientModules;
			clientState.Logout += ClearClientModules;

			HookProvider = hookProvider;
            Configuration = configuration;
            JobGauges = jobGauges;
            PluginLog = pluginLog;

            Address = new IconReplacerAddressResolver(scanner);

			if (!clientState.IsLoggedIn)
            {
                clientState.Login += SetupComboData;
			}
            else
            {
                SetupComboData();
				LoadClientModules();
			}

			this.clientState = clientState;

			PluginLog.Verbose("===== X I V C O M B O =====");
            PluginLog.Verbose("IsIconReplaceable address {IsIconReplaceable}", Address.IsIconReplaceable);
            PluginLog.Verbose("ComboTimer address {ComboTimer}", comboTimer);
            PluginLog.Verbose("LastComboMove address {LastComboMove}", lastComboMove);

            iconHook = HookProvider.HookFromAddress<OnGetIconDelegate>((nint)ActionManager.Addresses.GetAdjustedActionId.Value, GetIconDetour);
            checkerHook = HookProvider.HookFromAddress<OnCheckIsIconReplaceableDelegate>(Address.IsIconReplaceable, CheckIsIconReplaceableDetour);
            HookProvider = hookProvider;
        }

        public unsafe void LoadClientModules()
		{
			actionManager = ActionManager.Instance();
            clientModulesLoaded = true;
            PluginLog.Verbose("Client Modules Loaded.");
		}

		private unsafe void ClearClientModules()
		{
			PluginLog.Verbose("Client Modules Cleared.");
			actionManager = null;
		}

		public unsafe void SetupComboData()
        {
            var actionmanager = (byte*)ActionManager.Instance();
            comboTimer = (IntPtr)(actionmanager + 0x60);
            lastComboMove = comboTimer + 0x4;
            //var lastAction = (IntPtr)(actionmanager + 0x5F);
        }

        public void Enable()
        {
            iconHook.Enable();
            checkerHook.Enable();
        }

        public void Dispose()
        {
            iconHook.Dispose();
            checkerHook.Dispose();
        }

        // I hate this function. This is the dumbest function to exist in the game. Just return 1.
        // Determines which abilities are allowed to have their icons updated.

        private ulong CheckIsIconReplaceableDetour(uint actionID)
        {
            return 1;
        }

        /// <summary>
        ///     Replace an ability with another ability
        ///     actionID is the original ability to be "used"
        ///     Return either actionID (itself) or a new Action table ID as the
        ///     ability to take its place.
        ///     I tend to make the "combo chain" button be the last move in the combo
        ///     For example, Souleater combo on DRK happens by dragging Souleater
        ///     onto your bar and mashing it.
        /// </summary>
        private unsafe ulong GetIconDetour(byte self, uint actionID)
        {
            if (clientState.LocalPlayer == null) return iconHook.Original(self, actionID);
            // Last resort. For some reason GetIcon fires after leaving the lobby but before ClientState.Login
            if (lastComboMove == IntPtr.Zero)
            {
                SetupComboData();
                return iconHook.Original(self, actionID);
            }
            if (comboTimer == IntPtr.Zero)
            {
                SetupComboData();
                return iconHook.Original(self, actionID);
            }

            var lastMove = Marshal.ReadInt32(lastComboMove);
            var comboTime = Marshal.PtrToStructure<float>(comboTimer);
            var level = clientState.LocalPlayer.Level;
            //var lastAction = Marshal.ReadInt32(this.lastAction);

            // DRAGOON

            // Change Jump/High Jump into Mirage Dive when Dive Ready
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonJumpFeature))
                if (actionID == DRG.Jump || actionID == DRG.HighJump)
                {
                    if (SearchBuffArray(1243))
                        return DRG.MirageDive;
                    return iconHook.Original(self, DRG.Jump);
                }

            // Replace Coerthan Torment with Coerthan Torment combo chain
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonCoerthanTormentCombo))
                if (actionID == DRG.CTorment)
                {
                    if (comboTime > 0)
                    {
                        if ((lastMove == DRG.DoomSpike || lastMove == DRG.DraconianFury) && level >= 62)
                            return DRG.SonicThrust;
                        if (lastMove == DRG.SonicThrust && level >= 72)
                            return DRG.CTorment;
                    }

                    return iconHook.Original(self, DRG.DoomSpike);
                }

            /*
            // Replace Chaos Thrust with the Chaos Thrust combo chain
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonChaosThrustCombo))
                if (actionID == DRG.ChaosThrust || actionID == DRG.ChaoticSpring)
                {
                    if (comboTime > 0)
                    {
                        if ((lastMove == DRG.TrueThrust || lastMove == DRG.RaidenThrust) && level >= 18)
                            return DRG.Disembowel;
                        if (lastMove == DRG.Disembowel)
                        {
                            if (level >= 86)
                                return DRG.ChaoticSpring;
                            if (level >= 50)
                                return DRG.ChaosThrust;
                        }
                    }
                    if (SearchBuffArray(DRG.BuffFangAndClawReady) && level >= 56)
                        return DRG.FangAndClaw;
                    if (SearchBuffArray(DRG.BuffWheelingThrustReady) && level >= 58)
                        return DRG.WheelingThrust;
                    if (SearchBuffArray(DRG.BuffDraconianFire) && level >= 76)
                        return DRG.RaidenThrust;

                    return DRG.TrueThrust;
                }
            */

            // Replace Full Thrust with the Full Thrust combo chain
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonFullChaosCombo))
            {
                if (actionID == DRG.TrueThrust)
                {
                    if (comboTime > 0)
                    {
                        if ((lastMove == DRG.TrueThrust || lastMove == DRG.RaidenThrust) && level >= 4)
                        {

                            if ((!SearchBuffArray(DRG.BuffPowerSurge) || GetBuffTimer(DRG.BuffPowerSurge) <= 10) && !SearchBuffArray(DRG.BuffLifeSurge))
                                return DRG.Disembowel;

                            return DRG.VorpalThrust;
                        }

                        if (lastMove == DRG.VorpalThrust)
                        {
                            if (level >= 86)
                                return DRG.HeavensThrust;
                            if (level >= 26)
                                return DRG.FullThrust;
                        }

                        if (lastMove == DRG.Disembowel)
                        {
                            if (level >= 50)
                                return DRG.ChaosThrust;
                        }
                    }
                    if (SearchBuffArray(DRG.BuffFangAndClawReady) && level >= 56)
                        return DRG.FangAndClaw;
                    if (SearchBuffArray(DRG.BuffWheelingThrustReady) && level >= 58)
                        return DRG.WheelingThrust;
                    if (SearchBuffArray(DRG.BuffDraconianFire) && level >= 76)
                        return DRG.RaidenThrust;

                    return DRG.TrueThrust;
                }
            }

            /**
            // Replace Full Thrust with the Full Thrust combo chain
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonFullThrustCombo))
                if (actionID == DRG.FullThrust || actionID == DRG.HeavensThrust)
                {
                    if (comboTime > 0)
                    {
                        if ((lastMove == DRG.TrueThrust || lastMove == DRG.RaidenThrust) && level >= 4)
                            return DRG.VorpalThrust;
                        if (lastMove == DRG.VorpalThrust)
                        {
                            if (level >= 86)
                                return DRG.HeavensThrust;
                            if (level >= 26)
                                return DRG.FullThrust;
                        }
                    }
                    if (SearchBuffArray(DRG.BuffFangAndClawReady) && level >= 56)
                        return DRG.FangAndClaw;
                    if (SearchBuffArray(DRG.BuffWheelingThrustReady) && level >= 58)
                        return DRG.WheelingThrust;
                    if (SearchBuffArray(DRG.BuffDraconianFire) && level >= 76)
                        return DRG.RaidenThrust;

                    return DRG.TrueThrust;
                }
            */

            // DARK KNIGHT

            // Replace Souleater with Souleater combo chain
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DarkSouleaterCombo))
                if (actionID == DRK.HardSlash)
                {
                    if (comboTime > 0)
                    {
                        if (lastMove == DRK.HardSlash && level >= 2)
                            return DRK.SyphonStrike;
                        if (lastMove == DRK.SyphonStrike && level >= 26)
                            return DRK.Souleater;
                    }

                    return DRK.HardSlash;
                }

            // Replace Stalwart Soul with Stalwart Soul combo chain
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DarkStalwartSoulCombo))
                if (actionID == DRK.Unleash)
                {
                    if (comboTime > 0)
                        if (lastMove == DRK.Unleash && level >= 40)
                            return DRK.StalwartSoul;

                    return DRK.Unleash;
                }

            // PALADIN

            // Replace Royal Authority with Royal Authority combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinRoyalAuthorityCombo))
                if (actionID == PLD.FastBlade)
                {
                    if (Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinAtonementCombo))
                        if (SearchBuffArray(PLD.BuffAtonementReady))
                        {
                            return PLD.Atonement;
                        }

                    if (SearchBuffArray(PLD.BuffSupplicationReady))
                    {
                        return PLD.Supplication;
                    }

                    if (SearchBuffArray(PLD.BuffSepulchreReady))
                    {
                        return PLD.Sepulchre;
                    }

                    if (comboTime > 0)
                    {

                        if (lastMove == PLD.FastBlade && level >= 4)
                            return PLD.RiotBlade;
                        if (lastMove == PLD.RiotBlade)
                        {
                            if (level >= 60)
                                return PLD.RoyalAuthority;
                            if (level >= 26)
                                return PLD.RageOfHalone;
                        }
                    }

                    return PLD.FastBlade;
                }

            // Replace Prominence with Prominence combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinProminenceCombo))
                if (actionID == PLD.TotalEclipse)
                {
                    if (comboTime > 0)
                        if (lastMove == PLD.TotalEclipse && level >= 40)
                            return PLD.Prominence;

                    return PLD.TotalEclipse;
                }

            // Replace Requiescat with Confiteor when under the effect of Requiescat
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinRequiescatCombo))
                if (actionID == PLD.Requiescat)
                {
                    if (SearchBuffArray(PLD.BuffRequiescat) && level >= 80)
                        return iconHook.Original(self, PLD.Confiteor);
                    return PLD.Requiescat;
                }

            // WARRIOR

            // Replace Storm's Path with Storm's Path combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorStormsPathCombo))
                if (actionID == WAR.StormsPath)
                {
                    if (comboTime > 0)
                    {
                        if (lastMove == WAR.HeavySwing && level >= 4)
                            return WAR.Maim;
                        if (lastMove == WAR.Maim && level >= 26)
                        {
                            if (level >= 50)
                            {
                                if (!SearchBuffArray(WAR.BuffSurgingTempest) || GetBuffTimer(WAR.BuffSurgingTempest) <= 10)
                                    return WAR.StormsEye;

                            }

                            return WAR.StormsPath;
                        }
                    }

                    return WAR.HeavySwing;
                }

            // Replace Storm's Eye with Storm's Eye combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorStormsEyeCombo))
                if (actionID == WAR.StormsEye)
                {
                    if (comboTime > 0)
                    {
                        if (lastMove == WAR.HeavySwing && level >= 4)
                            return WAR.Maim;
                        if (lastMove == WAR.Maim && level >= 50)
                            return WAR.StormsEye;
                    }

                    return WAR.HeavySwing;
                }

            // Replace Mythril Tempest with Mythril Tempest combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorMythrilTempestCombo))
                if (actionID == WAR.MythrilTempest)
                {
                    if (comboTime > 0)
                        if (lastMove == WAR.Overpower && level >= 40)
                            return WAR.MythrilTempest;
                    return WAR.Overpower;
                }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorIRCombo))
                if (actionID == WAR.InnerRelease || actionID == WAR.Berserk)
                {
                    if (SearchBuffArray(WAR.BuffPrimalRendReady))
                        return WAR.PrimalRend;
                    return iconHook.Original(self, actionID);
                }

            // SAMURAI
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiSingleTargetCombo))
            {
                if(actionID == SAM.Hakaze)
                {
                    if(SearchBuffArray(SAM.BuffMeikyoShisui))
					{
						var gauge = JobGauges.Get<SAMGauge>();

						bool hasKa = gauge.HasKa;
						bool hasGetsu = gauge.HasGetsu;
						bool hasSetsu = gauge.HasSetsu;

						if (!hasKa)
							return SAM.Kasha;

						if (!hasGetsu)
							return SAM.Gekko;

                        return SAM.Yukikaze;

					}

                    if(comboTime > 0)
					{

                        if (lastMove == SAM.Hakaze)
						{
							var gauge = JobGauges.Get<SAMGauge>();

							bool hasKa = gauge.HasKa;
							bool hasGetsu = gauge.HasGetsu;
							bool hasSetsu = gauge.HasSetsu;

							if ((!SearchBuffArray(SAM.BuffFuka) || GetBuffTimer(SAM.BuffFuka) <= 5) && level >= 18)
                            {
                                return SAM.Shifu;
                            }

                            if(!SearchBuffArray(SAM.BuffFugetsu) || GetBuffTimer(SAM.BuffFugetsu) <= 5)
                            {
                                return SAM.Jinpu;
                            }

                            if (!hasSetsu && level >= 50)
                                return SAM.Yukikaze;

                            if (!hasKa && level >= 18)
                                return SAM.Shifu;

                            return SAM.Jinpu;
                        }

                        if (lastMove == SAM.Shifu && level >= 40)
                            return SAM.Kasha;

                        if (lastMove == SAM.Jinpu && level >= 30)
                            return SAM.Gekko;
                    }

                    //return SAM.Hakaze; // Redundant?
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiAoeCombo))
            {
                if (actionID == SAM.Fuga)
                {
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                    {
                        var gauge = JobGauges.Get<SAMGauge>();

                        bool hasKa = gauge.HasKa;
                        bool hasGetsu = gauge.HasGetsu;

                        if (!hasKa)
                            return SAM.Oka;

                        return SAM.Mangetsu;
                    }

                    if (comboTime > 0)
                    {
                        if (lastMove == SAM.Fuga)
						{
							var gauge = JobGauges.Get<SAMGauge>();

							bool hasKa = gauge.HasKa;
							bool hasGetsu = gauge.HasGetsu;

							if ((!SearchBuffArray(SAM.BuffFuka) || GetBuffTimer(SAM.BuffFuka) <= 5) && level >= 45)
								return SAM.Oka;

                            if ((!SearchBuffArray(SAM.BuffFugetsu) || GetBuffTimer(SAM.BuffFuka) <= 5) && level >= 35)
                                return SAM.Mangetsu;

                            if(!hasKa && level >= 45)
                                return SAM.Oka;

                            if (!hasGetsu && level >= 35)
                                return SAM.Mangetsu;

						}
                    }
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiTsubameCombo))
                if (actionID == SAM.Iaijutsu)
                {
                    var x = iconHook.Original(self, SAM.Tsubame);
                    if (x != SAM.Tsubame) return x;
                    return iconHook.Original(self, actionID);
                }

            // Replace Yukikaze with Yukikaze combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiYukikazeCombo))
                if (actionID == SAM.Yukikaze)
                {
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Yukikaze;
                    if (comboTime > 0)
                        if (lastMove == SAM.Hakaze && level >= 50)
                            return SAM.Yukikaze;
                    return SAM.Hakaze;
                }

            // Replace Gekko with Gekko combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiGekkoCombo))
                if (actionID == SAM.Gekko)
                {
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Gekko;
                    if (comboTime > 0)
                    {
                        if (lastMove == SAM.Hakaze && level >= 4)
                            return SAM.Jinpu;
                        if (lastMove == SAM.Jinpu && level >= 30)
                            return SAM.Gekko;
                    }

                    return SAM.Hakaze;
                }

            // Replace Kasha with Kasha combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiKashaCombo))
                if (actionID == SAM.Kasha)
                {
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Kasha;
                    if (comboTime > 0)
                    {
                        if (lastMove == SAM.Hakaze && level >= 18)
                            return SAM.Shifu;
                        if (lastMove == SAM.Shifu && level >= 40)
                            return SAM.Kasha;
                    }

                    return SAM.Hakaze;
                }

            // Replace Mangetsu with Mangetsu combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiMangetsuCombo))
                if (actionID == SAM.Mangetsu)
                {
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Mangetsu;
                    if (comboTime > 0)
                        if ((lastMove == SAM.Fuga || lastMove == SAM.Fuko) && level >= 35)
                            return SAM.Mangetsu;
                    if (level >= 86)
                        return SAM.Fuko;
                    return SAM.Fuga;
                }

            // Replace Oka with Oka combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiOkaCombo))
                if (actionID == SAM.Oka)
                {
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Oka;
                    if (comboTime > 0)
                        if ((lastMove == SAM.Fuga || lastMove == SAM.Fuko) && level >= 45)
                            return SAM.Oka;
                    if (level >= 86)
                        return SAM.Fuko;
                    return SAM.Fuga;
                }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiOgiCombo))
                if (actionID == SAM.Ikishoten)
                {
                    if (SearchBuffArray(SAM.BuffOgiNamikiriReady))
                        return SAM.OgiNamikiri;
                    if (JobGauges.Get<SAMGauge>().Kaeshi == Kaeshi.NAMIKIRI)
                        return SAM.KaeshiNamikiri;

                    return SAM.Ikishoten;
                }

            // NINJA

            // Replace Armor Crush with Armor Crush combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaArmorCrushCombo))
                if (actionID == NIN.ArmorCrush)
                {
                    if (comboTime > 0)
                    {
                        if (lastMove == NIN.SpinningEdge && level >= 4)
                            return NIN.GustSlash;
                        if (lastMove == NIN.GustSlash && level >= 54)
                            return NIN.ArmorCrush;
                    }

                    return NIN.SpinningEdge;
                }

            // Replace Aeolian Edge with Aeolian Edge combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaAeolianEdgeCombo))
                if (actionID == NIN.AeolianEdge)
                {
                    if (comboTime > 0)
                    {
                        if (lastMove == NIN.SpinningEdge && level >= 4)
                            return NIN.GustSlash;
                        if (lastMove == NIN.GustSlash && level >= 26)
                            return NIN.AeolianEdge;
                    }

                    return NIN.SpinningEdge;
                }

            // Replace Hakke Mujinsatsu with Hakke Mujinsatsu combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaHakkeMujinsatsuCombo))
                if (actionID == NIN.HakkeM)
                {
                    if (comboTime > 0)
                    {
                        if (lastMove == NIN.DeathBlossom && level >= 52)
                            return NIN.HakkeM;
                    }
                    return NIN.DeathBlossom;
                }

            // GUNBREAKER

            // Replace Solid Barrel with Solid Barrel combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerSolidBarrelCombo))
                if (actionID == GNB.SolidBarrel)
                {
                    if (comboTime > 0)
                    {
                        if (lastMove == GNB.KeenEdge && level >= 4)
                            return GNB.BrutalShell;
                        if (lastMove == GNB.BrutalShell && level >= 26)
                            return GNB.SolidBarrel;
                    }
                    return GNB.KeenEdge;
                }

            // Replace Wicked Talon with Gnashing Fang combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerGnashingFangCont))
                if (actionID == GNB.GnashingFang)
                {
                    if (level >= GNB.LevelContinuation)
                    {
                        if (SearchBuffArray(GNB.BuffReadyToRip))
                            return GNB.JugularRip;
                        if (SearchBuffArray(GNB.BuffReadyToTear))
                            return GNB.AbdomenTear;
                        if (SearchBuffArray(GNB.BuffReadyToGouge))
                            return GNB.EyeGouge;
                    }
                    return iconHook.Original(self, GNB.GnashingFang);
                }

            // Replace Burst Strike with Continuation
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerBurstStrikeCont))
                if (actionID == GNB.BurstStrike)
                {
                    if (level >= GNB.LevelEnhancedContinuation)
                    {
                        if (SearchBuffArray(GNB.BuffReadyToBlast))
                            return GNB.Hypervelocity;
                    }
                    return GNB.BurstStrike;
                }

            // Replace Demon Slaughter with Demon Slaughter combo
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerDemonSlaughterCombo))
                if (actionID == GNB.DemonSlaughter)
                {
                    if (comboTime > 0)
                        if (lastMove == GNB.DemonSlice && level >= 40)
                            return GNB.DemonSlaughter;
                    return GNB.DemonSlice;
                }

            // MACHINIST

            // Replace Clean Shot with Heated Clean Shot combo
            // Or with Heat Blast when overheated.
            // For some reason the shots use their unheated IDs as combo moves
            /*
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistMainCombo))
                if (actionID == MCH.CleanShot || actionID == MCH.HeatedCleanShot)
                {
                    if (comboTime > 0)
                    {
                        if (lastMove == MCH.SplitShot)
                        {
                            if (level >= 60)
                                return MCH.HeatedSlugshot;
                            if (level >= 2)
                                return MCH.SlugShot;
                        }

                        if (lastMove == MCH.SlugShot)
                        {
                            if (level >= 64)
                                return MCH.HeatedCleanShot;
                            if (level >= 26)
                                return MCH.CleanShot;
                        }
                    }

                    if (level >= 54)
                        return MCH.HeatedSplitShot;
                    return MCH.SplitShot;
                }


            // Replace Hypercharge with Heat Blast when overheated
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistOverheatFeature))
                if (actionID == MCH.Hypercharge)
                {
                    var gauge = JobGauges.Get<MCHGauge>();
                    if (gauge.IsOverheated && level >= 35)
                        return MCH.HeatBlast;
                    return MCH.Hypercharge;
                }

            // Replace Spread Shot with Auto Crossbow when overheated.
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistSpreadShotFeature))
                if (actionID == MCH.SpreadShot || actionID == MCH.Scattergun)
                {
                    if (JobGauges.Get<MCHGauge>().IsOverheated && level >= 52)
                        return MCH.AutoCrossbow;
                    if (level >= 82)
                        return MCH.Scattergun;
                    return MCH.SpreadShot;
                }
            */

            // BLACK MAGE

            if(Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackMageBaseRotation))
            {
                var gauge = JobGauges.Get<BLMGauge>();
                var soulStacks = gauge.AstralSoulStacks;
                var fireStacks = gauge.AstralFireStacks;
                var iceStacks = gauge.UmbralIceStacks;
                var umbralHearts = gauge.UmbralHearts;

				float currentMP = clientState.LocalPlayer.CurrentMp;

                if (actionID == BLM.Fire)
				{
                    //PluginLog.Verbose("Enochian Timer: {Timer}", gauge.ElementTimeRemaining);

                    if(gauge.InAstralFire)
                    {
                        if (currentMP >= 1600 && gauge.ElementTimeRemaining >= 6000 && level >= 60)
                            return BLM.Fire4;
                    }

                    if (gauge.UmbralHearts >= 1)
                    {
                        if (gauge.InUmbralIce)
                        {
                            if (gauge.ElementTimeRemaining >= 6000)
                                return BLM.Blizzard4;

                            return BLM.Fire3;
						}

						if (level >= 60 && gauge.ElementTimeRemaining >= 6000)
							return BLM.Fire4;
					}

                    //if (SearchBuffArray(BLM.BuffFirestarter))
                    //    return BLM.Fire3;

                    if (level >= 50)
                        if (currentMP <= 3000 && gauge.ElementTimeRemaining <= 4200)
                            return BLM.Fire;

                        if (currentMP <= 2200 && fireStacks >= 1)
                        {
                            if (currentMP >= 1200)
                            {
								if(gauge.ElementTimeRemaining >= 4200)
								    return BLM.Flare;

                                return BLM.Fire;
                            }

                            if (currentMP >= 800 || fireStacks == 3)
                                return BLM.Blizzard3;

                            return BLM.Blizzard;
                        }

                    if (level >= 35)
                    {
                        // If we're in Ice phase
                        if (iceStacks >= 1) {
                            if (currentMP != 10000)
                            {
                                if (level >= 40 && level < 58)
                                    return BLM.Freeze;
                                else if (level >= 58)
                                {
                                    return BLM.Blizzard4;
                                }
							}
						}
					}

                    if (currentMP < 800)
                        return BLM.Blizzard;

					if (currentMP < 1200)
						return BLM.Blizzard3;
					//PluginLog.Verbose("Current Mana: {CurrentMana}", currentMP);
				}

                if(actionID == BLM.Fire2)
                {
                    if ((gauge.UmbralHearts >= 2))
                    {
                        return BLM.Fire2;
                    }

                    if (gauge.UmbralHearts == 1)
                        return BLM.Flare;

                    //if (SearchBuffArray(BLM.BuffFirestarter))
                    //	return BLM.Fire3;

                    if (level >= 50)
                        if (currentMP <= 3000 && fireStacks >= 1)
						{
							if (currentMP >= 1200)
								return BLM.Flare;

							return BLM.Blizzard2;
						}

					if (level >= 35)
					{
						// If we're in Ice phase
						if (iceStacks >= 1)
						{
                            if (currentMP != 10000)
                            {
                                if (level >= 40)
                                    return BLM.Freeze;
							}

							//if (currentMP == 10000)
							//	return BLM.Fire3;
						}
					}

					if (currentMP < 800)
						return BLM.Blizzard;

					if (currentMP < 1200)
                        return BLM.Blizzard3;

				}
            }

            // B4 and F4 change to each other depending on stance, as do Flare and Freeze.
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackEnochianFeature))
            {
                if (actionID == BLM.Fire4 || actionID == BLM.Blizzard4)
                {
                    var gauge = JobGauges.Get<BLMGauge>();
                    if (gauge.InUmbralIce && level >= 58)
                        return BLM.Blizzard4;
                    if (level >= 60)
                        return BLM.Fire4;
                }

                if (actionID == BLM.Flare || actionID == BLM.Freeze)
                {
                    var gauge = JobGauges.Get<BLMGauge>();
                    if (gauge.InAstralFire && level >= 50)
                        return BLM.Flare;
                    return BLM.Freeze;
                }
            }

            // Ley Lines and BTL
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackLeyLines))
                if (actionID == BLM.LeyLines)
                {
                    if (SearchBuffArray(BLM.BuffLeyLines) && level >= 62)
                        return BLM.BTL;
                    return BLM.LeyLines;
                }

			// ASTROLOGIAN

			/*
	// Make cards on the same button as play
	if (Configuration.ComboPresets.HasFlag(CustomComboPreset.AstrologianCardsOnDrawFeature))
		if (actionID == AST.Play)
		{
			var gauge = JobGauges.Get<ASTGauge>();
			switch (gauge.DrawnCard)
			{
				case CardType.BALANCE:
					return AST.Balance;
				case CardType.BOLE:
					return AST.Bole;
				case CardType.ARROW:
					return AST.Arrow;
				case CardType.SPEAR:
					return AST.Spear;
				case CardType.EWER:
					return AST.Ewer;
				case CardType.SPIRE:
					return AST.Spire;
				default:
					return AST.Draw;
			}
		}
			*/

			// SUMMONER
			// Change Fester into Energy Drain
			if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerEDFesterCombo))
                if (actionID == SMN.Fester || actionID == SMN.Necrotize)
                {
                    if (!JobGauges.Get<SMNGauge>().HasAetherflowStacks)
                        return SMN.EnergyDrain;

                    if (level >= 92)
                        return SMN.Necrotize;

                    return SMN.Fester;
                }

            //Change Painflare into Energy Syphon
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerESPainflareCombo))
                if (actionID == SMN.Painflare)
                {
                    if (!JobGauges.Get<SMNGauge>().HasAetherflowStacks)
                        return SMN.EnergySyphon;
                    return SMN.Painflare;
                }

            // SCHOLAR

            // Change Fey Blessing into Consolation when Seraph is out.
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ScholarSeraphConsolationFeature))
                if (actionID == SCH.FeyBless)
                {
                    if (JobGauges.Get<SCHGauge>().SeraphTimer > 0) return SCH.Consolation;
                    return SCH.FeyBless;
                }

            // Change Energy Drain into Aetherflow when you have no more Aetherflow stacks.
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ScholarEnergyDrainFeature))
                if (actionID == SCH.EnergyDrain)
                {
                    if (JobGauges.Get<SCHGauge>().Aetherflow == 0) return SCH.Aetherflow;
                    return SCH.EnergyDrain;
                }

            // DANCER
            if(Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerSingleAoERotation))
			{
				var gauge = JobGauges.Get<DNCGauge>();
				
                if(actionID == DNC.Cascade)
                {
                    if (SearchBuffArray(DNC.BuffSilkenSymmetry))
                        return DNC.ReverseCascade;

                    if (SearchBuffArray(DNC.BuffSilkenFlow))
                        return DNC.Fountainfall;

                    if (comboTime > 0 && lastMove == DNC.Cascade)
                        return DNC.Fountain;

                    return DNC.Cascade;
                }

                if(actionID == DNC.Windmill)
				{
					if (SearchBuffArray(DNC.BuffSilkenSymmetry))
						return DNC.RisingWindmill;

					if (SearchBuffArray(DNC.BuffSilkenFlow))
						return DNC.Bloodshower;

					if (comboTime > 0 && lastMove == DNC.Windmill)
                        return DNC.Bladeshower;

                    return DNC.Windmill;
                }

     //           if(actionID == DNC.TechnicalStep)
     //           {
     //               if (gauge.IsDancing)
					//{
					//	if (gauge.CompletedSteps <= 3)
					//	{
					//		if (gauge.NextStep == DNC.TechnicalStep)
					//			return DNC.DoubleStandardFinish;

					//		return gauge.NextStep;
					//	}
					//	else
					//	{
					//		if (level >= 96)
					//			return DNC.FinishingMove;

					//		return DNC.QuadrupleTechnicalFinish;
					//	}
					//}
     //           }

                if(actionID == DNC.StandardStep)
                {
                    if (gauge.IsDancing)
                    {
						//PluginLog.Verbose("Completed Steps: {CompletedSteps}", gauge.CompletedSteps);
                        //PluginLog.Verbose("Next Step: {NextStep}", gauge.NextStep);

                        if (gauge.CompletedSteps <= 3)
                        {
                            if (gauge.NextStep == DNC.TechnicalStep)
                                return DNC.DoubleStandardFinish;

							return gauge.NextStep;
						}
                        else
                        {
                            //if (level >= 96)
                            //    return DNC.FinishingMove;
                            return DNC.QuadrupleTechnicalFinish;
                        }

                        // Quadruple technical step
                        // Tillana

                        //if (gauge.NextStep == DNC.TechnicalStep && level >= 96)
                        //    return DNC.FinishingMove;

                        
                    }
                }
			}

			/*
            // AoE GCDs are split into two buttons, because priority matters
            // differently in different single-target moments. Thanks yoship.
            // Replaces each GCD with its procced version.
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerAoeGcdFeature))
            {
                if (actionID == DNC.Bloodshower)
                {
                    if (SearchBuffArray(DNC.BuffFlourishingFlow) || SearchBuffArray(DNC.BuffSilkenFlow))
                        return DNC.Bloodshower;
                    return DNC.Bladeshower;
                }

                if (actionID == DNC.RisingWindmill)
                {
                    if (SearchBuffArray(DNC.BuffFlourishingSymmetry) || SearchBuffArray(DNC.BuffSilkenSymmetry))
                        return DNC.RisingWindmill;
                    return DNC.Windmill;
                }
            }
            */

			// Fan Dance changes into Fan Dance 3 while flourishing.
			if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerFanDanceCombo))
            {
                if (actionID == DNC.FanDance1)
                {
                    if (SearchBuffArray(DNC.BuffThreefoldFanDance))
                        return DNC.FanDance3;
                    return DNC.FanDance1;
                }

                // Fan Dance 2 changes into Fan Dance 3 while flourishing.
                if (actionID == DNC.FanDance2)
                {
                    if (SearchBuffArray(DNC.BuffThreefoldFanDance))
                        return DNC.FanDance3;
                    return DNC.FanDance2;
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerFanDance4Combo))
            {
                if (actionID == DNC.Flourish)
                {
                    if (SearchBuffArray(DNC.BuffFourfoldFanDance))
                        return DNC.FanDance4;
                    return DNC.Flourish;
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerDevilmentCombo))
            {
                if (actionID == DNC.Devilment)
                {
                    if (SearchBuffArray(DNC.BuffStarfallDanceReady))
                        return DNC.StarfallDance;
                    return DNC.Devilment;
                }
            }

            // WHM

            // Replace Solace with Misery when full blood lily
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.WhiteMageSolaceMiseryFeature))
                if (actionID == WHM.Solace)
                {
                    if (JobGauges.Get<WHMGauge>().BloodLily == 3)
                        return WHM.Misery;
                    return WHM.Solace;
                }

            // Replace Solace with Misery when full blood lily
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.WhiteMageRaptureMiseryFeature))
                if (actionID == WHM.Rapture)
                {
                    if (JobGauges.Get<WHMGauge>().BloodLily == 3)
                        return WHM.Misery;
                    return WHM.Rapture;
                }

            // BARD

            // Replace HS/BS with SS/RA when procced.
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.BardStraightShotUpgradeFeature))
                if (actionID == BRD.HeavyShot || actionID == BRD.BurstShot)
                {
                    if (SearchBuffArray(BRD.BuffHawksEye) || SearchBuffArray(BRD.BuffBarrage))
                    {
                        if (level >= 70) return BRD.RefulgentArrow;
                        return BRD.StraightShot;
                    }

                    if (level >= 76) return BRD.BurstShot;
                    return BRD.HeavyShot;
                }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.BardQuickKnockUpgradeFeature))
            {
                if (actionID == BRD.QuickNock)
                {
                    if (SearchBuffArray(BRD.BuffHawksEye) || SearchBuffArray(BRD.BuffBarrage))
                    {
                        return BRD.WideVolley;
                    }

                    return BRD.QuickNock;
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.BardAoEUpgradeFeature))
                if (actionID == BRD.QuickNock || actionID == BRD.Ladonsbite)
                {
                    if (SearchBuffArray(BRD.BuffShadowbiteReady))
                    {
                        return BRD.Shadowbite;
                    }

                    return iconHook.Original(self, BRD.QuickNock);
                }

            // MONK
            // haha you get nothing now

            // RED MAGE

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageSingleCombo))
            {
                if (actionID == RDM.Verthunder || actionID == RDM.Veraero)
                {
                    if (SearchBuffArray(RDM.BuffDualcast) || SearchBuffArray(RDM.BuffDualcast) ||
                        SearchBuffArray(RDM.BuffAcceleration) || SearchBuffArray(RDM.BuffChainspell))
                    {
                        var gauge = JobGauges.Get<RDMGauge>();

                        if (gauge.BlackMana >= gauge.WhiteMana)
                            return RDM.Veraero;

                        return RDM.Verthunder;
                    }

					if (SearchBuffArray(RDM.BuffVerfireReady)) return RDM.Verfire;

					if (SearchBuffArray(RDM.BuffVerstoneReady)) return RDM.Verstone;

					if (level < 62)
                        return RDM.Jolt;

                    return RDM.Jolt2;
                }
            }

            // Replace Scatter
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageAoECombo))
            {
                if (actionID == RDM.Scatter)
                {
                    if (SearchBuffArray(RDM.BuffDualcast) || SearchBuffArray(RDM.BuffDualcast) ||
                        SearchBuffArray(RDM.BuffAcceleration) || SearchBuffArray(RDM.BuffChainspell))
                    {
                        return RDM.Scatter;
                    }

                    var gauge = JobGauges.Get<RDMGauge>();

                    if (gauge.BlackMana >= gauge.WhiteMana)
                        return RDM.Veraero2;

                    return RDM.Verthunder2;
                }

                /*
                if (actionID == RDM.Veraero2)
                {
                    if (SearchBuffArray(RDM.BuffSwiftcast) || SearchBuffArray(RDM.BuffDualcast) || 
                        SearchBuffArray(RDM.BuffAcceleration) || SearchBuffArray(RDM.BuffChainspell))
                    {
                        if (level >= 66) return RDM.Impact;
                        return RDM.Scatter;
                    }
                    return iconHook.Original(self, RDM.Veraero2);
                }

                if (actionID == RDM.Verthunder2)
                {
                    if (SearchBuffArray(RDM.BuffSwiftcast) || SearchBuffArray(RDM.BuffDualcast) ||
                        SearchBuffArray(RDM.BuffAcceleration) || SearchBuffArray(RDM.BuffChainspell))
                    {
                        if (level >= 66) return RDM.Impact;
                        return RDM.Scatter;
                    }
                    return iconHook.Original(self, RDM.Verthunder2);
                }
                */
            }

            // Replace Redoublement with Redoublement combo, Enchanted if possible.
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageMeleeCombo))
            {
                if (actionID == RDM.Redoublement)
                {
                    var gauge = JobGauges.Get<RDMGauge>();
                    if ((lastMove == RDM.Riposte || lastMove == RDM.ERiposte) && level >= 35)
                    {
                        if ((gauge.BlackMana >= 15 && gauge.WhiteMana >= 15) || SearchBuffArray(RDM.BuffMagickedSwordplay))
                            return RDM.EZwerchhau;
                        return RDM.Zwerchhau;
                    }

                    if (lastMove == RDM.Zwerchhau && level >= 50)
                    {
                        if ((gauge.BlackMana >= 15 && gauge.WhiteMana >= 15) || SearchBuffArray(RDM.BuffMagickedSwordplay))
                            return RDM.ERedoublement;
                        return RDM.Redoublement;
                    }

                    if ((gauge.BlackMana >= 20 && gauge.WhiteMana >= 20) || SearchBuffArray(RDM.BuffMagickedSwordplay))
                        return RDM.ERiposte;
                    return RDM.Riposte;
                }
            }
			/*
            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageVerprocCombo))
            {
                /*
                if (actionID == RDM.Verstone)
                {
                    if (level >= 80 && (lastMove == RDM.Verflare || lastMove == RDM.Verholy)) return RDM.Scorch;
                    if (level >= 90 && lastMove == RDM.Scorch) return RDM.Resolution;

                    if (SearchBuffArray(RDM.BuffVerstoneReady)) return RDM.Verstone;
                    if (level < 62) return RDM.Jolt;
                    return RDM.Jolt2;
                }
                if (actionID == RDM.Verfire)
                {
                    if (level >= 80 && (lastMove == RDM.Verflare || lastMove == RDM.Verholy)) return RDM.Scorch;
                    if (level >= 90 && lastMove == RDM.Scorch) return RDM.Resolution;

                    if (SearchBuffArray(RDM.BuffVerfireReady)) return RDM.Verfire;
                    if (level < 62) return RDM.Jolt;
                    return RDM.Jolt2;
				}

                if (actionID == RDM.Verfire || actionID == RDM.Verstone)
                {
                    if (SearchBuffArray(RDM.BuffVerfireReady)) return RDM.Verfire;

                    if (SearchBuffArray(RDM.BuffVerstoneReady)) return RDM.Verstone;

                    return actionID;
                }

            }
                */

			// PICTOMANCER

			if (Configuration.ComboPresets.HasFlag(CustomComboPreset.PictomancerMotifIntoMuse))
            {
                var gauge = JobGauges.Get<PCTGauge>();

                if (actionID == PCT.CreatureMotif || actionID == PCT.PomMotif || actionID == PCT.WingMotif)
                {
                    if (gauge.CreatureMotifDrawn)
                    {
                        if (gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Pom))
                            return PCT.PomMuse;

                        if (gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Wing))
                            return PCT.WingedMuse;

                        if (gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Claw))
                            return PCT.ClawedMuse;

                        if (gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Maw))
                            return PCT.FangedMuse;

					}
                }

                if (actionID == PCT.WeaponMotif)
                {
                    if (gauge.WeaponMotifDrawn)
                    {
                        if (gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Weapon))
                            return PCT.StrikingMuse;
                    }
                }

                if (actionID == PCT.LandscapeMotif) {

                    if (gauge.LandscapeMotifDrawn)
                    {
                        if (gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Landscape))
                            return PCT.StarryMuse;
                    }
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.PictomancerSubtractivePalletCombo))
            {

                if (actionID == PCT.BlizzardInCyan)
                {
                    if(SearchBuffArray(PCT.BuffSubtractivePallet))
                    {
                        if (SearchBuffArray(PCT.BuffAetherHues))
                            return PCT.StoneInYellow;

                        if (SearchBuffArray(PCT.BuffAetherHues2))
                            return PCT.ThunderInMagenta;

                        return PCT.BlizzardInCyan;
                    }

                    return PCT.SubtractivePallet;
                }

                if(actionID == PCT.Blizzard2InCyan)
				{
					if (SearchBuffArray(PCT.BuffSubtractivePallet))
					{
						if (SearchBuffArray(PCT.BuffAetherHues))
							return PCT.Stone2InYellow;

						if (SearchBuffArray(PCT.BuffAetherHues2))
							return PCT.Thunder2InMagenta;

						return PCT.Blizzard2InCyan;
					}

					return PCT.SubtractivePallet;
				}
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.PictomancerHammerStampInjection) && level >= 50)
            {
                if((actionID == PCT.FireInRed || actionID == PCT.Fire2InRed) && SearchBuffArray(PCT.BuffHammerTime))
                {
                    if(level >= 86)
                    {
                        if (comboTime > 0)
                        {
                            if (lastMove == PCT.HammerStamp)
                                return PCT.HammerBrush;

                            if (lastMove == PCT.HammerBrush)
                                return PCT.PolishingHammer;
                        }
                    }

                    return PCT.HammerStamp;
                }
            }

			if (Configuration.ComboPresets.HasFlag(CustomComboPreset.PictomancerHolyMogOfWhite))
            {
                if(actionID == PCT.HolyInWhite)
				{
					var gauge = JobGauges.Get<PCTGauge>();

					if (level < 80 || gauge.MooglePortraitReady)
                        return PCT.MogOfTheAges;

                    if (level >= 90 && SearchBuffArray(PCT.BuffMonochromeTones))
                        return PCT.CometInBlack;

                    return PCT.HolyInWhite;
				}
            }

			// VIPER

			if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperSteelFangsCombo))
            {
				//var actionmanager = (byte*)ActionManager.Instance();
    //            ActionManager.get

				if (actionID == VPR.SteelFangs)
				{
					if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperGenerationCombo))
					{
						var gauge = JobGauges.Get<VPRGauge>();

						if (SearchBuffArray(VPR.BuffReawakened))
						{
                            if (level >= 96)
							{
								if (gauge.AnguineTribute == 4)
									return VPR.SecondGeneration;

								if (gauge.AnguineTribute == 3)
									return VPR.ThirdGeneration;

								if (gauge.AnguineTribute == 2)
									return VPR.FourthGeneration;

                                if (gauge.AnguineTribute == 1)
                                    return VPR.Ouroboros;

								return VPR.FirstGeneration;
							}
                            else
                            {
                                if (gauge.AnguineTribute == 3)
                                    return VPR.SecondGeneration;

                                if (gauge.AnguineTribute == 2)
                                    return VPR.ThirdGeneration;

                                if (gauge.AnguineTribute == 1)
                                    return VPR.FourthGeneration;

                                return VPR.FirstGeneration;
                            }
						}
					}

					if (comboTime > 0)
					{

						if (lastMove == VPR.SwiftskinsSting)
						{
							if (SearchBuffArray(VPR.BuffHindstungVenom))
							{
								return VPR.HindstingStrike;
							}

							if (SearchBuffArray(VPR.BuffHindsbaneVenom))
							{
								return VPR.HindsbaneFang;
							}

							return VPR.HindstingStrike;
						}

						if (lastMove == VPR.HuntersSting)
						{
							if (SearchBuffArray(VPR.BuffFlanksbaneVenom))
							{
								return VPR.FlanksbaneFang;
							}

							if (SearchBuffArray(VPR.BuffFlankstungVenom))
							{
								return VPR.FlankstingStrike;
							}

                            return VPR.FlanksbaneFang;
						}

						if (lastMove == VPR.SteelFangs || lastMove == VPR.ReavingFangs && lastMove != VPR.SwiftskinsSting && lastMove != VPR.HuntersSting)
						{
							if (!SearchBuffArray(VPR.BuffSwiftscaled))
							{
								return VPR.SwiftskinsSting;
							}

							if (!SearchBuffArray(VPR.BuffHuntersInstict))
							{
								return VPR.HuntersSting;
							}

							if (GetBuffTimer(VPR.BuffSwiftscaled) <= GetBuffTimer(VPR.BuffHuntersInstict))
							{
								return VPR.SwiftskinsSting;
							}

							return VPR.HuntersSting;
						}

					}

                    if (SearchBuffArray(VPR.BuffHonedReavers))
                        return VPR.ReavingFangs;

                    return VPR.SteelFangs;
                }

                if(actionID == VPR.ReavingFangs)
				{
					return VPR.ReavingFangs;
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperSteelMawCombo))
            {
                if (actionID == VPR.SteelMaw)
                {
					if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperGenerationCombo))
					{
						var gauge = JobGauges.Get<VPRGauge>();

						if (SearchBuffArray(VPR.BuffReawakened))
						{

							if (level >= 96)
							{
								if (gauge.AnguineTribute == 4)
									return VPR.SecondGeneration;

								if (gauge.AnguineTribute == 3)
									return VPR.ThirdGeneration;

								if (gauge.AnguineTribute == 2)
									return VPR.FourthGeneration;

								if (gauge.AnguineTribute == 1)
									return VPR.Ouroboros;

								return VPR.FirstGeneration;
							}
							else
							{
								if (gauge.AnguineTribute == 3)
									return VPR.SecondGeneration;

								if (gauge.AnguineTribute == 2)
									return VPR.ThirdGeneration;

								if (gauge.AnguineTribute == 1)
									return VPR.FourthGeneration;

								return VPR.FirstGeneration;
							}
						}
					}

					if (comboTime > 0)
					{
						if (lastMove == VPR.SteelMaw || lastMove == VPR.ReavingMaw)
                        {
                            if (!SearchBuffArray(VPR.BuffSwiftscaled))
                            {
                                return VPR.SwiftskinsBite;
                            }

                            if (!SearchBuffArray(VPR.BuffHuntersInstict))
                            {
                                return VPR.HuntersBite;
                            }

                            if (GetBuffTimer(VPR.BuffSwiftscaled) <= GetBuffTimer(VPR.BuffHuntersInstict))
                            {
                                return VPR.SwiftskinsBite;
                            }

                            return VPR.HuntersBite;
						}

						if (lastMove == VPR.SwiftskinsBite || lastMove == VPR.HuntersBite)
						{
							if (SearchBuffArray(VPR.BuffGrimhuntersVenom))
							{
								return VPR.JaggedMaw;
							}

							if (SearchBuffArray(VPR.BuffGrimskinsVenom))
							{
								return VPR.BloodiedMaw;
							}

							return VPR.JaggedMaw;
						}
					}

                    if (SearchBuffArray(VPR.BuffHonedReavers))
                        return VPR.ReavingMaw;

					return VPR.SteelMaw;
			    }

				if (actionID == VPR.ReavingMaw)
				{
					return VPR.ReavingMaw;
				}
			}

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperDenCombo))
            {
                if (actionID == VPR.HuntersDen || actionID == VPR.SwiftskinsDen)
				{
					if (SearchBuffArray(VPR.BuffPoisedForTwinblood))
					{
						return VPR.UncoiledTwinblood;
					}
					if (SearchBuffArray(VPR.BuffPoisedForTwinfang))
					{
						return VPR.UncoiledTwinfang;
					}

					if (SearchBuffArray(VPR.BuffFellskinsVenom))
					{
						return VPR.TwinbloodThresh;
					}
					if (SearchBuffArray(VPR.BuffFellhuntersVenom))
					{
						return VPR.TwinfangTresh;
					}

					if (actionID == VPR.HuntersDen)
						if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperGenerationCombo))
							return VPR.HuntersDen;

					if (actionID == VPR.SwiftskinsDen)
						if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperGenerationCombo))
							return VPR.SwiftskinsDen;
				}
            }

            if(Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperCoilCombo))
			{
                if (actionID == VPR.HuntersCoil || actionID == VPR.SwiftskinsCoil)
				{
					if (SearchBuffArray(VPR.BuffPoisedForTwinblood))
					{
						return VPR.UncoiledTwinblood;
					}
					if (SearchBuffArray(VPR.BuffPoisedForTwinfang))
					{
						return VPR.UncoiledTwinfang;
					}

					if (SearchBuffArray(VPR.BuffHuntersVenom))
                    {
                        return VPR.TwinfangBite;
					}
					if (SearchBuffArray(VPR.BuffSwiftskinsVenom))
					{
						return VPR.TwinbloodBite;
					}

                    //if (lastMove != VPR.Dreadwinder)
                    //    return VPR.Dreadwinder;

                    if (actionID == VPR.SwiftskinsCoil)
                        return VPR.SwiftskinsCoil;

					return VPR.HuntersCoil;
                }

                /*
                if(actionID == VPR.SwiftskinsCoil)
                {
					if (SearchBuffArray(VPR.BuffHuntersVenom) || SearchBuffArray(VPR.BuffSwiftskinsVenom))
					{
						return VPR.TwinbloodBite;
					}

                    //if (lastMove != VPR.Dreadwinder)
                    //    return VPR.Dreadwinder;
					// Hunters Venom == Twinfang Bite

					// Swiftskin's Venom == Twinblood Bite

                    return VPR.SwiftskinsCoil;
                }
                */
            }

			// REAPER 

			if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperRotation))
			{
				if (actionID == RPR.Slice)
				{
					if (SearchBuffArray(RPR.Buffs.Enshrouded))
					{
						return RPR.Sacrificium;
					}

					if (comboTime > 0)
                    {
                        if (lastMove == RPR.Slice && level >= RPR.Levels.WaxingSlice)
                            return RPR.WaxingSlice;

                        if (lastMove == RPR.WaxingSlice && level >= RPR.Levels.InfernalSlice)
                            return RPR.InfernalSlice;
                    }
                    return RPR.Slice;
                }

                if (actionID == RPR.SpinningScythe)
				{
					if (SearchBuffArray(RPR.Buffs.Enshrouded))
					{
						return RPR.Sacrificium;
					}

					if (comboTime > 0)
                    {
                        if (lastMove == RPR.SpinningScythe && level >= RPR.Levels.NightmareScythe)
                            return RPR.NightmareScythe;
                    }

                    return RPR.SpinningScythe;
				}

				if((actionID == RPR.BloodStalk || actionID == RPR.GrimSwathe) && !SearchBuffArray(RPR.Buffs.Enshrouded) && level >= 76)
				{
                    bool gluttonyCooldown = IsActionOnCooldown(RPR.Gluttony);
                    if (!gluttonyCooldown)
                    {
                        return RPR.Gluttony;
                    }
                }

                if(actionID == RPR.Gibbet || actionID == RPR.Gallows)
                {
                    if (SearchBuffArray(RPR.Buffs.Enshrouded))
                    {
                        if (SearchBuffArray(RPR.Buffs.EnhancedCrossReaping))
                            return RPR.CrossReaping;

                        if (SearchBuffArray(RPR.Buffs.EnhancedVoidReaping))
                            return RPR.VoidReaping;
                    }
                    else
                    {
                        if (SearchBuffArray(RPR.Buffs.EnhancedGibbet))
                        {
                            return RPR.Gibbet;
                        }

                        if (SearchBuffArray(RPR.Buffs.EnhancedGallows))
                        {
                            return RPR.Gallows;
                        }
                    }
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperRegressFeature))
            {
                if (actionID == RPR.Egress || actionID == RPR.Ingress)
                {
                    if (SearchBuffArray(RPR.Buffs.Threshold)) return RPR.Regress;
                    return actionID;
                }
            }

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperEnshroudCombo))
            {
                if (actionID == RPR.Enshroud)
				{
					if (SearchBuffArray(RPR.Buffs.PerfectioParata))
						return RPR.Perfectio;

					if (SearchBuffArray(RPR.Buffs.Enshrouded))
                    {
                        return RPR.Communio;
                    }
                    return actionID;
				}
			}

            if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperArcaneFeature))
            {
                if (actionID == RPR.ArcaneCircle)
                {
                    if (SearchBuffArray(RPR.Buffs.ImSac1) ||
                        SearchBuffArray(RPR.Buffs.ImSac2))
                        return RPR.PlentifulHarvest;
                    return actionID;
                }
            }

            return iconHook.Original(self, actionID);
        }

        private unsafe bool IsActionOnCooldown(uint actionID)
		{
            if(actionManager == null)
                LoadClientModules();

            if (clientModulesLoaded)
            {
                return !actionManager->IsActionOffCooldown(ActionType.Action, RPR.Gluttony);
            }

			return true;
        }

        private float GetBuffTimer(ushort needle)
        {
            float timeLeft = 0;
			var buffs = clientState.LocalPlayer.StatusList;

            for (var i = 0; i < buffs.Length; i++)
                if (buffs[i].StatusId == needle)
                    timeLeft = buffs[i].RemainingTime;
				
            return timeLeft;
        }

        private bool SearchBuffArray(ushort needle)
        {
            if (needle == 0) return false;
            var buffs = clientState.LocalPlayer.StatusList;
            for (var i = 0; i < buffs.Length; i++)
                if (buffs[i].StatusId == needle)
                    return true;
            return false;
        }        
    }
}
