
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
            VERSION = "4.0.0";

        static string configPath = Paths.ConfigPath + Path.DirectorySeparatorChar + MODNAME + Path.DirectorySeparatorChar + "Config.cfg";
        public static ConfigFile config;
        internal static OptionsMenu options;

        static public void LoadedGameSetup()
        {
            //AddDebug("LoadedGameSetup");
            Player.main.isUnderwaterForSwimming.changedEvent.AddHandler(Player.main, new UWE.Event<Utils.MonitoredValue<bool>>.HandleFunction(Patches.OnPlayerIsUnderwaterForSwimmingChanged));
            if (Player.main.IsUnderwaterForSwimming() == false)
                Patches.CheckFishInContainer(Inventory.main.container);

            if (Player.main.currentMountedVehicle)
                Patches.CheckVehicleInventory(Player.main.currentMountedVehicle, Player.main.currentMountedVehicle.wasAboveWater);
        }

        private void Start()
        {
            config = new ConfigFile(configPath, false);
            Fish_Out_Of_Water.Config.Bind();
            options = new OptionsMenu();
            OptionsPanelHandler.RegisterModOptions(options);
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll();
            SaveUtils.RegisterOnQuitEvent(Patches.CleanUp);
            Logger.LogInfo($"Plugin {GUID} {VERSION} is loaded ");

        }

        [HarmonyPatch(typeof(WaitScreen), "Hide")]
        internal class WaitScreen_Hide_Patch
        {
            public static void Postfix(WaitScreen __instance)
            {
                LoadedGameSetup();
            }
        }

    }
}