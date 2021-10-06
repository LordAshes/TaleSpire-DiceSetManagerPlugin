using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LordAshes
{
    public partial class DiceSetManagerPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// Class for managing dice sets
        /// </summary>
        public static class DiceSetManager
        {
            /// <summary>
            /// Class for DiceSet which relates roll ids with the dice that comprise the roll
            /// </summary>
            public class DiceSet
            {
                public int RollId { get; set; } = -1;
                public List<Die> Dice { get; set; } = new List<Die>();
            }

            /// <summary>
            /// Class for holding dice set roll results
            /// </summary>
            public class DiceSetRollSpecs
            {
                // Dice set name
                public string name { get; set; } = "";
                // Dice set total
                public int total { get; set; } = 0;
                // Dice set roll details
                public DiceManager.DiceRollResultData details { get; set; } = null;
            }

            /// <summary>
            /// Subscription client for dice set callbacks
            /// </summary>
            public class SubscriptionClient
            {
                public string identity { get; set; } = "";
                public Action callback { get; set; } = null;
            }

            /// <summary>
            /// Subscription events enumeration
            /// </summary>
            public enum SubscriptionEvent
            {
                diceAdd = 1,
                diceResult = 2,
                diceClear = 4,
            }

            // Holds all dice sets indexed by the dice set name
            public static Dictionary<string, DiceSet> diceSets = new Dictionary<string, DiceSet>();

            // Holds all dice set subscription indexed by event type and then by subscriber identity
            public static Dictionary<SubscriptionEvent, Dictionary<string,Action<string>>> subscriptions = new Dictionary<SubscriptionEvent, Dictionary<string, Action<string>>>()
            {
                {SubscriptionEvent.diceAdd, new Dictionary<string,Action<string>>()},
                {SubscriptionEvent.diceResult, new Dictionary<string,Action<string>>()},
                {SubscriptionEvent.diceClear, new Dictionary<string,Action<string>>()},
            };

            // Holds the roll id of the last added dice
            public static int rollId = -1;

            // Holds a random generator for rotating dice during automated throws
            private static System.Random ran = new System.Random();

            /// <summary>
            /// Method to create a new dice set
            /// </summary>
            /// <param name="dicesetName">Name of the dice set</param>
            /// <param name="formula">Formula defining the dice and modifiers for the dice set</param>
            public static void CreateDiceSet(string dicesetName, string formula)
            {
                var command = $"talespire://dice/" + dicesetName.Replace(" ","_").Replace(":","-")+":"+formula;
                System.Diagnostics.Process.Start(command).WaitForExit();
                Internal.sequencer = 5;
            }

            /// <summary>
            /// Method to toss a dice set
            /// This method raises the dice up, rotates them randomly and then engages the core TS throw dice routine
            /// </summary>
            /// <param name="dicesetName">Name of the dice set to be tossed</param>
            /// <param name="vertical">Optional parameter indciating how far the dice are lifted (default 5 units)</param>
            public static void ThrowDiceSet(string dicesetName, float vertical = 5.0f)
            {
                if(diceSets.ContainsKey(dicesetName))
                {
                    foreach(Die die in diceSets[dicesetName].Dice)
                    {
                        die.gameObject.transform.rotation = Quaternion.Euler(ran.Next(0, 180), ran.Next(0, 180), ran.Next(0, 180));
                    }
                    Vector3 pos = diceSets[dicesetName].Dice[0].gameObject.transform.position;
                    DiceManager dm = DiceManager.Instance;
                    dm.GatherDice(new Vector3(pos.x, pos.y, pos.z + vertical), diceSets[dicesetName].RollId);
                    dm.ThrowDice(diceSets[dicesetName].RollId);
                }
                else
                {
                    Debug.Log("DiceSetManager has no dice set named '" + dicesetName + "'");
                }
            }

            /// <summary>
            /// Method to remove a dice set (removes all dice associated with the dice set)
            /// </summary>
            /// <param name="dicesetName">Name of the dice set to be removed</param>
            public static void ClearDiceSet(string dicesetName)
            {
                if (diceSets.ContainsKey(dicesetName))
                {
                    DiceManager dm = DiceManager.Instance;
                    foreach (KeyValuePair<string, Action<string>> sub in DiceSetManager.subscriptions[DiceSetManager.SubscriptionEvent.diceClear])
                    {
                        sub.Value(JsonConvert.SerializeObject(new Tuple<string, int>(dicesetName, diceSets[dicesetName].RollId)));
                    }
                    dm.ClearAllDice(diceSets[dicesetName].RollId);
                    diceSets.Remove(dicesetName);
                }
                else
                {
                    Debug.Log("DiceSetManager has no dice set named '" + dicesetName + "'");
                }
            }

            /// <summary>
            /// Method to remove all dice sets (removes all dice associated with any dice set)
            /// </summary>
            public static void ClearAllDiceSets()
            {
                for(int ds=0; ds<diceSets.Keys.Count; ds++)
                {
                    ClearDiceSet(diceSets.Keys.ElementAt(ds)); ds--;
                }
            }

            /// <summary>
            /// Method to locate a dice set by the corresponding roll Id
            /// </summary>
            /// <param name="rollId">RollId of the dice set to be looked up</param>
            /// <returns>KeyValuePair of string and Diceset representing the dice set name and the dice set contents</returns>
            public static KeyValuePair<string,DiceSet> FindByRollId(int rollId)
            {
                foreach(KeyValuePair<string,DiceSet> diceset in diceSets)
                {
                    if (diceset.Value.RollId == rollId) { return diceset; }
                }
                return default(KeyValuePair<string, DiceSet>);
            }

            /// <summary>
            /// Generic subscription method to subscribe to dice set events
            /// </summary>
            /// <param name="action">The dice set event to subscribe for (can be multiple ORed events)</param>
            /// <param name="identity">Uniquie identity of the subscriber allowing unsubscribing</param>
            /// <param name="callback">Callback function triggered when event occurs with event info returned as a JSON string</param>
            public static void Subscribe(SubscriptionEvent action, string identity, Action<string> callback)
            {
                if (((int)action % (int)SubscriptionEvent.diceAdd) > 0) 
                { 
                    subscriptions[SubscriptionEvent.diceAdd].Add(identity, callback); 
                }
                if (((int)action % (int)SubscriptionEvent.diceResult) > 0)
                {
                    subscriptions[SubscriptionEvent.diceResult].Add(identity, callback);
                }
                if (((int)action % (int)SubscriptionEvent.diceClear) > 0)
                {
                    subscriptions[SubscriptionEvent.diceClear].Add(identity, callback);
                }
            }

            /// <summary>
            /// Generic method to unsubscribe from dice set events
            /// </summary>
            /// <param name="action">The dice set event to be unsubscribed (can be multiple ORed events)</param>
            /// <param name="identity">Uniquie identity of the subscriber</param>
            public static void Unsubscribe(SubscriptionEvent action, string identity)
            {
                if (((int)action % (int)SubscriptionEvent.diceAdd) > 0)
                {
                    subscriptions[SubscriptionEvent.diceAdd].Remove(identity);
                }
                if (((int)action % (int)SubscriptionEvent.diceResult) > 0)
                {
                    subscriptions[SubscriptionEvent.diceResult].Remove(identity);
                }
                if (((int)action % (int)SubscriptionEvent.diceClear) > 0)
                {
                    subscriptions[SubscriptionEvent.diceClear].Remove(identity);
                }
            }

            /// <summary>
            /// Internal processing functions needed for functionality but not user requests
            /// </summary>
            public static class Internal
            {
                public static int sequencer = 0;

                /// <summary>
                /// Method called in Update function which sequences the creation of new dice
                /// </summary>
                public static void UpdateDiceSetSequence()
                {
                    if (sequencer > 0)
                    {
                        if (sequencer == 3)
                        {
                            Debug.Log("Spawning Dice");
                            SpawnDice();
                        }

                        if (sequencer == 1)
                        {
                            Debug.Log("Tossing Dice");
                            ThrowDice(DiceSetManagerPlugin.DiceSetManager.rollId);
                        }
                        sequencer--;
                    }
                }

                /// <summary>
                /// Method used spawn the dice in the dice tray to the board
                /// </summary>
                private static void SpawnDice()
                {
                    UIDiceTray dt = GameObject.FindObjectOfType<UIDiceTray>();
                    DiceSetManagerPlugin.overrideButton = 1;
                    dt.ButtonDown();
                    dt.SpawnDice();
                    DiceSetManagerPlugin.overrideButton = -1;
                    dt.ButtonUp();

                }

                /// <summary>
                /// Method used to toss the dice based on a RollId.
                /// Tossing the dice after they are created allows linking of the dice with the corresponding
                /// dice set name instead of a roll Id. When a new dice set is created, the RegisterDie patch
                /// records the roll Id of the dice set. When this function tosses the dice set based on the
                /// roll Id, the RPC_DiceResult patch allows the dice set to be associated with the corresponding
                /// dice set name (in addition to the roll Id).
                /// </summary>
                /// <param name="rollId">RollId of the dice set to be tossed</param>
                private static void ThrowDice(int rollId)
                {
                    DiceManager dm = DiceManager.Instance;
                    KeyValuePair<string,DiceSet> diceset = FindByRollId(DiceSetManagerPlugin.DiceSetManager.rollId);
                    Vector3 pos = diceset.Value.Dice[0].gameObject.transform.position;
                    dm.GatherDice(new Vector3(pos.x, pos.y, pos.z+1f), DiceSetManagerPlugin.DiceSetManager.rollId);
                    dm.ThrowDice(DiceSetManagerPlugin.DiceSetManager.rollId);
                }
            }
        }
    }
}
