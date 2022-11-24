
using HarmonyLib;
using QModManager.API.ModLoading;
using System.Reflection;
using System;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using UnityEngine;
using static ErrorMessage;

namespace Fish_Out_Of_Water
{
    [QModCore]
    public class Main
    {
        internal static Config config { get; } = OptionsPanelHandler.RegisterModOptions<Config>();
        //static bool gameLoaded = false;
        //public static PDA pda;

        public static void Log(string str, QModManager.Utility.Logger.Level lvl = QModManager.Utility.Logger.Level.Debug)
        {
            QModManager.Utility.Logger.Log(lvl, str);
        }

        public static void Setup()
        {
            //Player.main.isUnderwaterForSwimming.changedEvent.AddHandler(Player.main, new UWE.Event<Utils.MonitoredValue<bool>>.HandleFunction(Fish_Out_Of_Water.OnPlayerIsUnderwaterForSwimmingChanged));
            //pda = Player.main.GetPDA();
        }

        [HarmonyPatch(typeof(IngameMenu), "QuitGameAsync")]
        internal class IngameMenu_QuitGameAsync_Patch
        {
            public static void Postfix(IngameMenu __instance, bool quitToDesktop)
            {
                if (!quitToDesktop)
                {
                    //AddDebug(" QuitGameAsync");
                    Fish_Out_Of_Water.fishOutOfWater = new Dictionary<LiveMixin, float>();
                    Fish_Out_Of_Water.fishInInventory = new Dictionary<LiveMixin, float>();
                }
            }
        }

        //[HarmonyPatch(typeof(WaitScreen), "Hide")]
        internal class WaitScreen_Hide_Patch
        { // fires after game loads
            public static void Postfix(WaitScreen __instance)
            {
                //AddDebug(" WaitScreen Hide");
                //if (uGUI.isLoading)
                {
                    //AddDebug(" WaitScreen Hide  !!!");
                    Setup();
                }
            }
        }

        [QModPatch]
        public static void Load()
        {
            config.Load();
            Assembly assembly = Assembly.GetExecutingAssembly();
            new Harmony($"qqqbbb_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }
}