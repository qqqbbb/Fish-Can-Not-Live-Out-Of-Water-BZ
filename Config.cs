
using BepInEx.Configuration;
using Nautilus.Commands;
using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using System.Collections.Generic;

namespace Fish_Out_Of_Water
{
    internal class Config
    {
        public static ConfigEntry<float> hoursFishCanLiveOutOfWater;
        public static void Bind()
        {
            hoursFishCanLiveOutOfWater = Main.config.Bind("", "Number of hours fish live out of water", 1f, "");

        }
    }
}