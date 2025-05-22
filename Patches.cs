using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ErrorMessage;


namespace Fish_Out_Of_Water
{
    internal class Patches
    {
        static bool vehicleAboveWater = false;
        static HashSet<IItemsContainer> aquariums = new HashSet<IItemsContainer>();

        public static void CleanUp()
        {
            vehicleAboveWater = false;
            aquariums.Clear();
        }

        static bool IsEatableFish(GameObject go)
        {
            if (go.GetComponent<Creature>() == null) return false;
            if (go.GetComponent<Eatable>() == null) return false;
            return go.GetComponent<LiveMixin>();
        }

        static bool IsEatableFishAlive(GameObject go)
        {
            if (go.GetComponent<Creature>() == null) return false;
            if (go.GetComponent<Eatable>() == null) return false;
            LiveMixin liveMixin = go.GetComponent<LiveMixin>();
            return liveMixin && liveMixin.IsAlive();
        }

        static void KillFish(LiveMixin liveMixin)
        {
            //AddDebug("KillFish " + liveMixin.name);
            liveMixin.health = 0;
            liveMixin.tempDamage = 0;
            liveMixin.SyncUpdatingState();
            AquariumFish af = liveMixin.GetComponent<AquariumFish>();
            if (af)
                UnityEngine.Object.Destroy(af);

            Locomotion locomotion = liveMixin.GetComponent<Locomotion>();
            locomotion.enabled = false;
            //CreatureDeath creatureDeath = liveMixin.GetComponent<CreatureDeath>();
            Rigidbody rb = liveMixin.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.None;
                WorldForces worldForces = liveMixin.GetComponent<WorldForces>();
                if (worldForces)
                    worldForces.handleDrag = false;

                rb.drag = Mathf.Max(rb.drag, 1f);
                rb.angularDrag = Mathf.Max(rb.angularDrag, 1f);
            }
            liveMixin.gameObject.EnsureComponent<EcoTarget>().SetTargetType(EcoTargetType.DeadMeat);
            Eatable eatable = liveMixin.GetComponent<Eatable>();
            if (liveMixin.damageInfo != null)
            {
                liveMixin.damageInfo.Clear();
                LiveMixin.damageInfoPool.Return(liveMixin.damageInfo);
            }
            BehaviourUpdateUtils.Deregister(liveMixin);
            if (liveMixin.passDamageDataOnDeath)
                liveMixin.gameObject.BroadcastMessage("OnKill", SendMessageOptions.DontRequireReceiver);
            else if (liveMixin.broadcastKillOnDeath)
                liveMixin.gameObject.BroadcastMessage("OnKill", SendMessageOptions.DontRequireReceiver);

            eatable.timeDecayStart = 0;
            eatable.SetDecomposes(true);
        }

        static public void OnPlayerIsUnderwaterForSwimmingChanged(Utils.MonitoredValue<bool> isUnderwaterForSwimming)
        {
            //AddDebug(" OnPlayerIsUnderwaterForSwimmingChanged " + isUnderwaterForSwimming.value);
            if (isUnderwaterForSwimming.value)
                SaveFishInContainer(Inventory.main.container);
            else
                DoomFishInContainer(Inventory.main.container);
        }

        static void DoomFishInContainer(IItemsContainer container)
        {
            if (container == null)
                return;

            foreach (InventoryItem item in container)
            {
                GameObject go = item.item.gameObject;
                if (go.GetComponent<Creature>())
                {
                    LiveMixin liveMixin = go.GetComponent<LiveMixin>();
                    if (liveMixin == null || !liveMixin.IsAlive())
                        continue;

                    Eatable eatable = go.GetComponent<Eatable>();
                    if (eatable && eatable.timeDecayStart == 0)
                    {
                        //AddDebug("DoomFishInContainer " + item.item.gameObject.name);
                        eatable.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
                    }
                }
            }
        }

        static void SaveFishInContainer(IItemsContainer container)
        {
            if (container == null)
                return;

            foreach (InventoryItem item in container)
            {
                GameObject go = item.item.gameObject;
                if (go.GetComponent<Creature>())
                {
                    LiveMixin liveMixin = go.GetComponent<LiveMixin>();
                    if (liveMixin == null || !liveMixin.IsAlive())
                        continue;

                    Eatable eatable = go.GetComponent<Eatable>();
                    if (eatable)
                        eatable.timeDecayStart = 0;
                }
            }
        }

        static void SaveFish(GameObject go)
        {
            Eatable eatable = go.GetComponent<Eatable>();
            if (eatable)
                eatable.timeDecayStart = 0;
        }

        static public void CheckFishInContainer(IItemsContainer container)
        {
            if (container == null)
                return;

            if (aquariums.Contains(container))
                return;

            //AddDebug("CheckFishInContainer " + container.label);
            List<InventoryItem> fishToKill = new List<InventoryItem>();
            foreach (InventoryItem item in container)
            {
                GameObject go = item.item.gameObject;
                if (go.GetComponent<Creature>() == null)
                    continue;

                LiveMixin liveMixin = go.GetComponent<LiveMixin>();
                if (liveMixin == null || !liveMixin.IsAlive())
                    continue;

                Eatable eatable = go.GetComponent<Eatable>();
                if (eatable)
                {
                    //AddDebug("CheckFishInContainer " + item.item.gameObject.name);
                    if (eatable.timeDecayStart == 0)
                        DoomFish(eatable);
                    else if (ShouldFishBeDead(eatable))
                        fishToKill.Add(item);
                }
            }
            //AddDebug("CheckFishInContainer fishToKill.Count " + fishToKill.Count);
            foreach (InventoryItem ii in fishToKill)
            {
                LiveMixin liveMixin = ii.item.GetComponent<LiveMixin>();
                if (liveMixin == null)
                    continue;

                KillFish(liveMixin);
            }
        }

        static void CheckFish(GameObject go)
        {
            if (go == null)
                return;

            if (go.GetComponent<Creature>() == null)
                return;

            LiveMixin liveMixin = go.GetComponent<LiveMixin>();
            if (liveMixin == null || !liveMixin.IsAlive())
                return;

            Eatable eatable = go.GetComponent<Eatable>();
            if (eatable == null)
                return;

            if (ShouldFishBeDead(eatable))
                KillFish(liveMixin);
            else
                DoomFish(eatable);
        }

        private static void DoomFish(Eatable eatable)
        {
            eatable.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
        }

        private static void DoomFish(GameObject go)
        {
            Eatable eatable = go.GetComponent<Eatable>();
            if (eatable == null) return;
            eatable.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
        }

        private static bool ShouldFishBeDead(Eatable eatable)
        {
            if (eatable && eatable.timeDecayStart > 0)
            {
                DateTime dateTimeNow = DayNightCycle.ToGameDateTime(DayNightCycle.main.timePassedAsFloat);
                DateTime dateTimeStartSuf = DayNightCycle.ToGameDateTime(eatable.timeDecayStart);
                double hoursOutOfWater = (dateTimeNow - dateTimeStartSuf).TotalHours;
                //AddDebug($"{eatable.name} hours out of water {hoursOutOfWater}");
                if (hoursOutOfWater > Config.hoursFishCanLiveOutOfWater.Value)
                    return true;
            }
            return false;
        }

        public static void CheckVehicleInventory(Vehicle vehicle, bool aboveWater)
        {
            //AddDebug($"CheckVehicleInventory {aboveWater}");
            Exosuit exosuit = vehicle as Exosuit;
            if (exosuit)
            {
                if (aboveWater)
                    CheckFishInContainer(exosuit.storageContainer.container);
                else
                    SaveFishInContainer(exosuit.storageContainer.container);

                return;
            }
            List<IItemsContainer> containers = new List<IItemsContainer>();
            vehicle.GetAllStorages(containers);
            //AddDebug($"vehicle containers {containers.Count}");
            foreach (var c in containers)
            {
                if (aboveWater)
                    CheckFishInContainer(c);
                else
                    SaveFishInContainer(c);
            }
        }

        [HarmonyPatch(typeof(PDA))]
        class PDA_Patch
        {
            [HarmonyPostfix, HarmonyPatch("Open")]
            public static void OpenPostfix(PDA __instance, PDATab tab)
            {
                //AddDebug("PDA Open " + tab);
                if (Player.main.IsUnderwaterForSwimming())
                    return;

                CheckFishInContainer(Inventory.main.container);
                IItemsContainer itemsContainer = null;
                for (int i = 0; i < Inventory.main.usedStorage.Count; i++)
                {
                    itemsContainer = Inventory.main.GetUsedStorage(i);
                    if (itemsContainer != null)
                        break;
                }
                if (itemsContainer != null)
                    CheckFishInContainer(itemsContainer);
            }
        }

        [HarmonyPatch(typeof(Player))]
        class Player_Patch
        {
            [HarmonyPostfix, HarmonyPatch("Update")]
            public static void UpdatePostfix(Player __instance)
            {
                if (uGUI.isLoading || __instance.currentMountedVehicle == null || vehicleAboveWater == __instance.currentMountedVehicle.wasAboveWater)
                    return;

                vehicleAboveWater = __instance.currentMountedVehicle.wasAboveWater;
                CheckVehicleInventory(__instance.currentMountedVehicle, vehicleAboveWater);
            }
        }

        [HarmonyPatch(typeof(QuickSlots))]
        class QuickSlots_Patch
        {
            [HarmonyPostfix, HarmonyPatch("DrawAsTool")]
            public static void DrawAsToolPostfix(QuickSlots __instance, PlayerTool tool)
            {
                //AddDebug("QuickSlots DrawAsTool " + tool.name);
                if (Player.main.IsUnderwaterForSwimming() == false && IsEatableFishAlive(tool.gameObject))
                    CheckFish(tool.gameObject);
            }
        }

        [HarmonyPatch(typeof(LiveMixin))]
        class LiveMixin_Patch
        {
            [HarmonyPrefix, HarmonyPatch("Kill")]
            public static void KillPrefix(LiveMixin __instance)
            {
                SaveFish(__instance.gameObject);
            }
        }

        [HarmonyPatch(typeof(Aquarium))]
        class Aquarium_Patch
        {
            [HarmonyPostfix, HarmonyPatch("Start")]
            public static void StartPostfix(Aquarium __instance)
            {
                aquariums.Add(__instance.storageContainer.container);
            }
            [HarmonyPostfix, HarmonyPatch("AddItem")]
            public static void AddItemPostfix(Aquarium __instance, InventoryItem item)
            {
                //AddDebug("Aquarium AddItem " + item.item.name);
                SaveFish(item.item.gameObject);
            }
            [HarmonyPostfix, HarmonyPatch("RemoveItem")]
            public static void RemoveItemPostfix(Aquarium __instance, InventoryItem item)
            {
                if (Player.main.IsUnderwaterForSwimming() == false)
                {
                    //AddDebug("Aquarium RemoveItem " + item.item.name);
                    DoomFish(item.item.gameObject);
                }
            }
        }

        //[HarmonyPatch(typeof(TooltipFactory))]
        class TooltipFactory_Patch
        {
            //[HarmonyPostfix, HarmonyPatch("ItemCommons")]
            static void ItemCommonsPostfix(StringBuilder sb, TechType techType, GameObject obj)
            {
                if (IsEatableFishAlive(obj))
                {
                    Eatable eatable = obj.GetComponent<Eatable>();
                    if (eatable.timeDecayStart > 0)
                    {
                        sb.AppendLine();
                        sb.Append("FISH DOOMED");
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(Inventory))]
        class Inventory_Patch
        {
            //[HarmonyPostfix, HarmonyPatch("InternalDropItem")]
            public static void InternalDropItemPostfix(Inventory __instance, Pickupable pickupable)
            {
                //AddDebug("Inventory InternalDropItem " + pickupable.name);
                if (Player.main.IsUnderwater() == false && IsEatableFishAlive(pickupable.gameObject))
                {

                }
            }
        }

    }
}
