using HarmonyLib;
using Nautilus.Options;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Fish_Out_Of_Water
{
    internal class OptionsMenu : ModOptions
    {
        public OptionsMenu() : base("Fish can not live out of water")
        {
            ModSliderOption timeFlowSpeedSlider = Config.hoursFishCanLiveOutOfWater.ToModSliderOption(.1f, 10f, .1f, "{0:0.#}");
            AddItem(timeFlowSpeedSlider);
        }
    }
}
