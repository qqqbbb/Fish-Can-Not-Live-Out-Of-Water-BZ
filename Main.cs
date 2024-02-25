
using HarmonyLib;
using System.Reflection;
using System;
using BepInEx;
using Nautilus.Handlers;
using System.Collections.Generic;
using UnityEngine;
using static ErrorMessage;

namespace Fish_Out_Of_Water
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        private const string
            MODNAME = "Fish can not live out of water",
            GUID = "qqqbbb.subnauticaBZ.fishOutOfWater",
            VERSION = "3.0.0";
        internal static Config config { get; } = OptionsPanelHandler.RegisterModOptions<Config>();

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

        private void Start()
        {
            //AddDebug("Mono Start ");
            //Logger.LogInfo("Mono Start");
            //config.Load();
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll();
        }
    }
}