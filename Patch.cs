using HarmonyLib;
using System;
//using SMLHelper.V2.Assets;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using static ErrorMessage;

namespace Fish_Out_Of_Water
{// not checking if exosuit picks up fish and leaves water
    class Fish_Out_Of_Water
    {
        public static Dictionary<LiveMixin, float> fishOutOfWater = new Dictionary<LiveMixin, float>();

        public static bool IsEatableFishAlive(GameObject go)
        {
            Creature creature = go.GetComponent<Creature>();
            Eatable eatable = go.GetComponent<Eatable>();
            LiveMixin liveMixin = go.GetComponent<LiveMixin>();

            return creature && eatable && liveMixin && liveMixin.IsAlive();
        }

        public static void OnPlayerIsUnderwaterForSwimmingChanged(Utils.MonitoredValue<bool> isUnderwaterForSwimming)
        {
            //AddDebug(" OnPlayerIsUnderwaterForSwimmingChanged " + Player.main.IsUnderwaterForSwimming());
            AddFishToList();
        }

        private static void CheckFish(LiveMixin liveMixin)
        {
            if (!fishOutOfWater.ContainsKey(liveMixin))
                return;

            float timeOutOfWater = DayNightCycle.main.timePassedAsFloat - fishOutOfWater[liveMixin];
            if (timeOutOfWater > Main.config.outOfWaterLifeTime * 60f)
            {
                //AddDebug("KillFish " + liveMixin.gameObject.name + " " + timeOutOfWater.ToString("0.0"));
                //Main.Log("Kill " + liveMixin.gameObject.name + " " + timeOutOfWater.ToString("0.0"));
                fishOutOfWater.Remove(liveMixin);
                KillFish(liveMixin);
            }
            //else
            //    AddDebug("CheckFish " + liveMixin.gameObject.name + " " + timeOutOfWater.ToString("0.0"));
        }

        static void KillFish(LiveMixin liveMixin)
        {
            //AddDebug("KillFish " + liveMixin.gameObject.name);
            //Main.Log("Kill " + liveMixin.gameObject.name);

            liveMixin.health = 0f;
            liveMixin.tempDamage = 0f;
            liveMixin.SyncUpdatingState();
            //if (liveMixin.deathClip)
            //    liveMixin.deathClip.Play();
            //if (liveMixin.deathSound)
            //    Utils.PlayFMODAsset(liveMixin.deathSound, liveMixin.transform);
            //if (liveMixin.passDamageDataOnDeath)
            //    liveMixin.gameObject.BroadcastMessage("OnKill", DamageType.Normal, SendMessageOptions.DontRequireReceiver);
            //else if (liveMixin.broadcastKillOnDeath)
            //    liveMixin.gameObject.BroadcastMessage("OnKill", SendMessageOptions.DontRequireReceiver);
            //if (liveMixin.sendKillOnDeath)
            //{
            //    if (liveMixin.passDamageDataOnDeath)
            //        liveMixin.gameObject.SendMessage("OnKill", DamageType.Normal, SendMessageOptions.DontRequireReceiver);
            //    else
            //        liveMixin.gameObject.SendMessage("OnKill", SendMessageOptions.DontRequireReceiver);
            //}

            AquariumFish af = liveMixin.GetComponent<AquariumFish>();
            if (af)
                UnityEngine.Object.Destroy(af);
            Locomotion locomotion = liveMixin.GetComponent<Locomotion>();
            locomotion.enabled = false;
            CreatureDeath creatureDeath = liveMixin.GetComponent<CreatureDeath>();
            Eatable eatable = liveMixin.GetComponent<Eatable>();
            eatable.SetDecomposes(true);
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

            if (creatureDeath)
            {
                if (creatureDeath.respawn && !creatureDeath.respawnOnlyIfKilledByCreature)
                    creatureDeath.SpawnRespawner();
                if (creatureDeath.removeCorpseAfterSeconds >= 0.0)
                    creatureDeath.Invoke("RemoveCorpse", creatureDeath.removeCorpseAfterSeconds);
                creatureDeath.SyncFixedUpdatingState();
            }
            Pickupable pickupable = liveMixin.GetComponent<Pickupable>();
            ItemsContainer container = pickupable.inventoryItem.container as ItemsContainer;
            if (container != null)
            { // fix offset decay bar
                container.RemoveItem(pickupable, true);
                container.AddItem(pickupable);
            }
        }

        public static void AddFishToList(ItemsContainer container = null)
        {
            bool underWater = Player.main.IsUnderwaterForSwimming();
            HashSet<LiveMixin> fishToKill = new HashSet<LiveMixin>();
            HashSet<LiveMixin> fishToCheck = new HashSet<LiveMixin>();
            //ErrorMessage.AddDebug("run AddFishToList ");
            GameObject parent = null;
            bool aquarium = false;
            if (container == null)
                container = Inventory.main.container;
            else
            {
                parent = container.tr.parent.gameObject;
                aquarium = parent.name == "SeaTruckAquariumModule(Clone)" || parent.name == "Aquarium(Clone)";
            }

            foreach (InventoryItem item in container)
            {
                //ErrorMessage.AddDebug("AddFishToList "+ item.item.gameObject.name);
                if (IsEatableFishAlive(item.item.gameObject))
                {
                    LiveMixin liveMixin = item.item.GetComponent<LiveMixin>();
                    //Main.Log("AddFishToList " + liveMixin.gameObject.name);
                    if (aquarium)
                    {
                        //AddDebug(container.tr.name + " Aquarium ");
                        fishOutOfWater.Remove(liveMixin);
                        continue;
                    }
                    if (underWater)
                    {
                        if (fishOutOfWater.ContainsKey(liveMixin))
                        {
                            //ErrorMessage.AddDebug("remove fish " + liveMixin.gameObject.name);
                            //Main.Log("remove fish " + liveMixin.gameObject.name);
                            if (DayNightCycle.main.timePassedAsFloat - fishOutOfWater[liveMixin] > Main.config.outOfWaterLifeTime * 60f)
                                fishToKill.Add(liveMixin);

                            fishOutOfWater.Remove(liveMixin);
                        }
                    }
                    else
                    {
                        if (fishOutOfWater.ContainsKey(liveMixin))
                        {
                            fishToCheck.Add(liveMixin);
                        }
                        else
                        {
                            //AddDebug("Add fish " + liveMixin.gameObject.name);
                            fishOutOfWater.Add(liveMixin, DayNightCycle.main.timePassedAsFloat);
                        }
                    }
                }
            }
            foreach (LiveMixin liveMixin in fishToCheck)
                CheckFish(liveMixin);
            foreach (LiveMixin liveMixin in fishToKill)
                KillFish(liveMixin);
        }

        private static void KillFishInContainer(ItemsContainer container)
        {
            //AddDebug("KillFishInContainer " + container.tr.name);
            HashSet<LiveMixin> fishToCheck = new HashSet<LiveMixin>();
            GameObject parent = container.tr.parent.gameObject;
            bool aquarium = parent.name == "SeaTruckAquariumModule(Clone)" || parent.name == "Aquarium(Clone)";
            AddFishToList(container);
            AddFishToList();
            foreach (InventoryItem item in container)
            {
                if (IsEatableFishAlive(item.item.gameObject))
                {
                    LiveMixin liveMixin = item.item.GetComponent<LiveMixin>();
                    if (fishOutOfWater.ContainsKey(liveMixin))
                    {
                        //AddDebug(" KillFishInContainer " + item.item.GetTechType());
                        if (aquarium)
                        {
                            //AddDebug(container.tr.name + " Aquarium ");
                            fishOutOfWater.Remove(liveMixin);
                            continue;
                        }
                        //ErrorMessage.AddDebug("fishOutOfWaterList " + item.item.GetTechType());
                        //CheckFish(liveMixin);
                        fishToCheck.Add(liveMixin);
                    }
                }
            }
            foreach (LiveMixin liveMixin in fishToCheck)
                CheckFish(liveMixin);
        }

        static IEnumerator KillCoroutine()
        {
            KillFishInContainer(Inventory.main.container);
            ItemsContainer openContainer = GetOpenContainer();
            if (openContainer != null)
                KillFishInContainer(openContainer);

            yield return new WaitForSeconds(1);
            if (Main.pda == null)
                AddDebug("Main.pda  null ");
            if (Main.pda.isInUse)
                Player.main.StartCoroutine(KillCoroutine());
        }

        public static ItemsContainer GetOpenContainer()
        {
            int storageCount = Inventory.main.usedStorage.Count;
            if (Inventory.main.usedStorage.Count > 0)
            {
                IItemsContainer itemsContainer = Inventory.main.usedStorage[storageCount - 1];

                if (itemsContainer is ItemsContainer)
                {
                    ItemsContainer container = itemsContainer as ItemsContainer;
                    //GameObject parent = container.tr.parent.gameObject;
                    //AddDebug(" parent " + parent.name);
                    //Main.Log(" parent " + parent.name);
                    //if (parent.GetComponentInChildren<Aquarium>())
                    //if (parent.name == "SeaTruckAquariumModule(Clone)" || parent.name == "Aquarium(Clone)")
                    //{
                        //AddDebug(container.tr.name + " Aquarium ");
                        //return null;
                    //}
                    return container;
                }
            }
            return null;
        }

        [HarmonyPatch(typeof(PDA))]
        public class PDA_Open_Patch
        {
            [HarmonyPatch(nameof(PDA.Open))]
            public static void Postfix(PDA __instance, PDATab tab)
            {
                //ErrorMessage.AddDebug("tab " + tab);
                //ErrorMessage.AddDebug("usedStorage.Count " + Inventory.main.usedStorage.Count);
                ItemsContainer container = GetOpenContainer();
                if (container != null)
                    AddFishToList(container);

                if (!Player.main.IsUnderwater())
                    Player.main.StartCoroutine(KillCoroutine());
            }
        }

        [HarmonyPatch(typeof(Pickupable), "Drop", new Type[] { typeof(Vector3), typeof(Vector3) })]
        class Pickupable_Drop_Patch
        {
            public static void Postfix(Pickupable __instance, Vector3 dropPosition)
            {
                LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
                if (liveMixin && fishOutOfWater.ContainsKey(liveMixin))
                {
                    if (Player.main.IsUnderwater())
                    {
                        //AddDebug("reset time " + liveMixin.gameObject.name);
                        fishOutOfWater.Remove(liveMixin);
                    }
                    else
                        CheckFish(liveMixin);
                }
            }
        }

        //[HarmonyPatch(typeof(Inventory))]
        class Inventory_GetAllItemActions_Patch
        {
            [HarmonyPatch("GetAllItemActions")]
            public static void Postfix(Inventory __instance, InventoryItem item, ItemAction __result)
            {
                IItemsContainer oppositeContainer = __instance.GetOppositeContainer(item);
                AddDebug("GetAllItemActions " + item.item.GetTechName() + " " + oppositeContainer.AllowedToAdd(item.item, false));

            }
        }

        //[HarmonyPatch(typeof(StorageContainer))]
        class StorageContainer_Open_Patch
        { // escape pod does not have this
            //[HarmonyPatch(nameof(StorageContainer.Open), new Type[] { typeof})]
            public static void Postfix(StorageContainer __instance)
            {
                ErrorMessage.AddDebug("StorageContainer Open");

            }
        }

        //[HarmonyPatch(typeof(CreatureDeath))]
        class CreatureDeath_OnKill_Patch
        {
            [HarmonyPatch(nameof(CreatureDeath.OnKill))]
            public static void Postfix(CreatureDeath __instance)
            {
                ErrorMessage.AddDebug("OnKill " + __instance.gameObject.name);

            }
        }

        //[HarmonyPatch(typeof(QuickSlots), "NotifySelect")]
        //class QuickSlots_NotifySelect_Patch
        //{
        //    public static void Postfix(QuickSlots __instance, int slotID)
        //    {
        //        LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
        //        if (liveMixin && fishOutOfWater.ContainsKey(liveMixin))
        //        {
        //            CheckFish(liveMixin);
        //        }
        //    }
        //}

    }
}