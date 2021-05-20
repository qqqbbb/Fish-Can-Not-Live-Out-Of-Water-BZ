﻿using HarmonyLib;
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
            //ErrorMessage.AddDebug(" OnPlayerIsUnderwaterForSwimmingChanged " + Player.main.IsUnderwaterForSwimming());
            AddFishToList();
        }

        private static void CheckFish(LiveMixin liveMixin)
        {
            if (DayNightCycle.main.timePassedAsFloat - fishOutOfWater[liveMixin] > Main.config.outOfWaterLifeTime * 60f)
            {
                fishOutOfWater.Remove(liveMixin);
                KillFish(liveMixin);
            }
        }

        static void KillFish(LiveMixin liveMixin)
        {
            //AddDebug("Kill " + liveMixin.gameObject.name);
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
            if (container == null)
                container = Inventory.main.container;

            foreach (InventoryItem item in container)
            {
                //ErrorMessage.AddDebug("AddFishToList "+ item.item.gameObject.name);
                if (IsEatableFishAlive(item.item.gameObject))
                {
                    LiveMixin liveMixin = item.item.GetComponent<LiveMixin>();
                    //Main.Log("AddFishToList " + liveMixin.gameObject.name);
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
                            //ErrorMessage.AddDebug("Add fish " + liveMixin.gameObject.name);
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
            //ErrorMessage.AddDebug("KillFishInContainer " );
            HashSet<LiveMixin> fishToCheck = new HashSet<LiveMixin>();
            foreach (InventoryItem item in container)
            {
                if (IsEatableFishAlive(item.item.gameObject))
                {
                    LiveMixin liveMixin = item.item.GetComponent<LiveMixin>();
                    if (fishOutOfWater.ContainsKey(liveMixin))
                    {
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
                    return (itemsContainer as ItemsContainer);
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

                if (!Player.main.IsUnderwaterForSwimming())
                    Player.main.StartCoroutine(KillCoroutine());
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

        [HarmonyPatch(typeof(Pickupable), "Drop", new Type[] { typeof(Vector3), typeof(Vector3) })]
        class Pickupable_Drop_Patch
        {
            public static void Postfix(Pickupable __instance, Vector3 dropPosition)
            {
                LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
                if (liveMixin && fishOutOfWater.ContainsKey(liveMixin))
                {
                    CheckFish(liveMixin);
                }
            }
        }

        //[HarmonyPatch(typeof(StorageContainer))]
        class StorageContainer_Open_Patch
        { // escape pod does not have this
            [HarmonyPatch(nameof(StorageContainer.Open), new Type[] { typeof(Transform) })]
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


    }
}