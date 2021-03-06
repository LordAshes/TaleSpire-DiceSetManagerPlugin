using System;
using System.Collections.Generic;
using System.IO;

using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace LordAshes
{
    public partial class DiceSetManagerPlugin : BaseUnityPlugin
    {
        public static int overrideButton = -1;

        public static DiceSetManagerPlugin Instance = null;

        public static void SetInstance(DiceSetManagerPlugin self)
        {
            Instance = self;
        }

        /// <summary>
        /// Patch for overcoming the conditions in the core ThrowDice method
        /// </summary>
        [HarmonyPatch(typeof(UIDiceTray), "ButtonDown")]
        public static class PatchButtonDown
        {
            public static bool Prefix()
            {
                // Bypass the original code
                return false;
            }

            public static void Postfix()
            {
                // Set the buttonHeld to the override value if set
                UIDiceTray __instance = GameObject.FindObjectOfType<UIDiceTray>();
                if (overrideButton == -1)
                {
                    PatchAssistant.SetField(__instance, "_buttonHeld", Input.GetMouseButtonDown(0));
                }
                else if (overrideButton == 0)
                {
                    PatchAssistant.SetField(__instance, "_buttonHeld", false);
                }
                else
                {
                    PatchAssistant.SetField(__instance, "_buttonHeld", true);
                }
            }
        }

        /// <summary>
        /// Patch for overcoming the conditions in the core ThrowDice method
        /// </summary>
        [HarmonyPatch(typeof(UIDiceTray), "GMButtonDown")]
        public static class PatchGMButtonDown
        {
            public static bool Prefix()
            {
                // Bypass the original code
                return false;
            }

            public static void Postfix()
            {
                // Set the gmButtonHeld to the override value if set
                UIDiceTray __instance = GameObject.FindObjectOfType<UIDiceTray>();
                if (overrideButton == -1)
                {
                    PatchAssistant.SetField(__instance, "_gmButtonHeld", Input.GetMouseButtonDown(0));
                }
                else if (overrideButton == 0)
                {
                    PatchAssistant.SetField(__instance, "_gmButtonHeld", false);
                }
                else
                {
                    PatchAssistant.SetField(__instance, "_gmButtonHeld", true);
                }
            }
        }

        /// <summary>
        /// Patch for overcoming the conditions in the core ThrowDice method
        /// </summary>
        [HarmonyPatch(typeof(UIDiceTray), "ButtonUp")]
        public static class PatchButtonUp
        {

            public static bool Prefix()
            {
                // Bypass the original code
                return false;
            }

            public static void Postfix()
            {
                // Set the buttonHeld and gmButtonHeld to the override value if set
                UIDiceTray __instance = GameObject.FindObjectOfType<UIDiceTray>();
                if (overrideButton == -1 || overrideButton == 0)
                {
                    PatchAssistant.SetField(__instance, "_buttonHeld", false);
                    PatchAssistant.SetField(__instance, "_gmButtonHeld", false);
                }
                else 
                {
                    PatchAssistant.SetField(__instance, "_buttonHeld", true);
                    PatchAssistant.SetField(__instance, "_gmButtonHeld", true);
                }
            }
        }

        /// <summary>
        /// Patch for detecting new dice sets
        /// </summary>
        [HarmonyPatch(typeof(DiceManager), "RegisterDie")]
        public static class PatchRegisterDie
        {
            public static bool Prefix(Die die, int rollId, bool gmOnly)
            {
                // Execute original code
                return true;
            }

            public static void Postfix(Die die, int rollId, bool gmOnly)
            {
                // See if the dice set is already registered
                bool found = false;
                foreach(KeyValuePair<string,DiceSet> diceset in Instance.DiceSets)
                {
                    // If the dice set is already registered
                    if(diceset.Value.RollId==rollId)
                    { 
                        // Add die to dice set
                        diceset.Value.Dice.Add(die);
                        if (diceset.Value.Formula == "") { diceset.Value.Formula = DiceSetManagerPlugin.Instance.rollFormula; }
                        foreach (KeyValuePair<string, Action<string>> sub in Instance.Subscriptions[SubscriptionEvent.diceAdd])
                        {
                            sub.Value("{\"RollId\": "+rollId+", \"DiceCount\": "+diceset.Value.Dice.Count+"}");
                        }
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    // If the dice set does not already exist
                    string tempId = System.Guid.NewGuid().ToString();
                    Instance.DiceSets.Add(tempId, new DiceSet() { RollId = rollId, Formula = DiceSetManagerPlugin.Instance.rollFormula });
                    Instance.DiceSets[tempId].Dice.Add(die);
                    foreach (KeyValuePair<string, Action<string>> sub in Instance.Subscriptions[SubscriptionEvent.diceAdd])
                    {
                        sub.Value("{\"RollId\": " + rollId + ", \"DiceCount\": " + Instance.DiceSets[tempId].Dice.Count + "}");
                    }
                }

                // Process dice set create notifications
                Instance.rollId = rollId;
            }
        }

        /// <summary>
        /// Patch for detecting dice rolls
        /// </summary>
        [HarmonyPatch(typeof(DiceManager), "RPC_DiceResult")]
        public static class PatchDiceResults
        {
            public static bool Prefix(bool isGmOnly, byte[] diceListData, PhotonMessageInfo msgInfo)
            {
                // Bypass the original code
                return false;
            }

            public static void Postfix(bool isGmOnly, byte[] diceListData, PhotonMessageInfo msgInfo)
            {
                if (BoardSessionManager.InBoard)
                {
                    // Parse roll result
                    int total = 0;
                    string message = "";
                    DiceManager.DiceRollResultData diceRollResultData = BinaryIO.FromByteArray<DiceManager.DiceRollResultData>(diceListData, (BinaryReader br) => br.ReadDiceRollResultData());
                    foreach (DiceManager.DiceGroupResultData dgrd in diceRollResultData.GroupResults)
                    {
                        message = message + dgrd.Name + " : ";
                        foreach (DiceManager.DiceResultData drd in dgrd.Dice)
                        {
                            foreach (int value in drd.Results)
                            {
                                message = message + drd.Resource.Replace("numbered", "") + ((drd.DiceOperator == DiceManager.DiceOperator.Add) ? "+" : "-") + drd.Modifier+"=";
                                message = message + value + ", ";
                                total = total + value;
                            }
                            total = total + ((drd.DiceOperator == DiceManager.DiceOperator.Add) ? drd.Modifier : -1 * drd.Modifier);
                        }
                    }
                    message = message.Substring(0, message.Length - 2);
                    message = message + " = " + total;
                    // Update dice set name if different from current dictionary entry
                    string tempId = "";
                    foreach(KeyValuePair<string, DiceSet> diceset in Instance.DiceSets)
                    {
                        if(diceset.Value.RollId == diceRollResultData.RollId)
                        {
                            if((diceset.Key != diceRollResultData.GroupResults[0].Name) && (diceRollResultData.GroupResults[0].Name != ""))
                            {
                                Instance.DiceSets.Add(diceRollResultData.GroupResults[0].Name, diceset.Value);
                                tempId = diceset.Key;
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    if (tempId != "") { Instance.DiceSets.Remove(tempId); }

                    // Process dice result notifications
                    foreach(KeyValuePair<string,Action<string>> sub in Instance.Subscriptions[SubscriptionEvent.diceResult])
                    {
                        sub.Value(JsonConvert.SerializeObject(new DiceSetRollSpecs() { Name = diceRollResultData.GroupResults[0].Name, Total = total, Details = diceRollResultData }));
                    }
                }
            }
        }
    }
}
