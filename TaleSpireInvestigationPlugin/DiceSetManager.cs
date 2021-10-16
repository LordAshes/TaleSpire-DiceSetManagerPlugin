using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LordAshes
{
    public partial class DiceSetManagerPlugin : BaseUnityPlugin
    {
        // Holds all dice sets indexed by the dice set name
        public Dictionary<string, DiceSet> DiceSets = new Dictionary<string, DiceSet>();

        // Holds all dice set subscription indexed by event type and then by subscriber identity
        public Dictionary<SubscriptionEvent, Dictionary<string, Action<string>>> Subscriptions = new Dictionary<SubscriptionEvent, Dictionary<string, Action<string>>>()
        {
            {SubscriptionEvent.diceAdd, new Dictionary<string,Action<string>>()},
            {SubscriptionEvent.diceResult, new Dictionary<string,Action<string>>()},
            {SubscriptionEvent.diceClear, new Dictionary<string,Action<string>>()},
        };

        // Holds the roll id of the last added dice
        private int rollId = -1;
        private string rollFormula = "";
        private Vector3 dicePos = Vector3.zero;
        private bool tossDie = false;

        // Holds a random generator for rotating dice during automated throws
        private System.Random ran = new System.Random();

        /// <summary>
        /// Method to create a new dice set
        /// </summary>
        /// <param name="dicesetName">Name of the dice set</param>
        /// <param name="formula">Formula defining the dice and modifiers for the dice set</param>
        public void CreateDiceSet(string dicesetName, string formula)
        {
            Debug.Log("Dice Set Manager Plugin: Loading DiceSet");
            rollFormula = formula;
            var command = $"talespire://dice/" + dicesetName.Replace(" ", "_").Replace(":", "-") + ":" + rollFormula;
            System.Diagnostics.Process.Start(command).WaitForExit();
            StartCoroutine((IEnumerator)ProcessDiceSetCreation(0.150f));
        }

        public void ShowDiceSet(string dicesetName, bool visible)
        {
            foreach(Die die in DiceSets[dicesetName].Dice)
            {
                die.enabled = visible;
            }
        }

        public void MoveDiceSet(string dicesetName, Vector3 pos)
        {
            Debug.Log("Dice Set Manager Plugin: Moving DiceSet");
            Instance.rollId = DiceSets[dicesetName].RollId;
            Instance.dicePos = pos;
            Instance.tossDie = false;
            StartCoroutine((IEnumerator)ProcessDiceSetMove(0.010f));
        }

        /// <summary>
        /// Method to toss a dice set
        /// This method raises the dice up, rotates them randomly and then engages the core TS throw dice routine
        /// </summary>
        /// <param name="dicesetName">Name of the dice set to be tossed</param>
        /// <param name="vertical">Optional parameter indciating how far the dice are lifted (default 5 units)</param>
        public void ThrowDiceSet(string dicesetName, float lift = 5.0f)
        {
            Debug.Log("Dice Set Manager Plugin: Throwing DiceSet (Lift="+lift+")");
            Instance.rollId = DiceSets[dicesetName].RollId;
            Instance.dicePos = new Vector3(DiceSets[dicesetName].Dice[0].gameObject.transform.position.x, DiceSets[dicesetName].Dice[0].gameObject.transform.position.y+lift, DiceSets[dicesetName].Dice[0].gameObject.transform.position.z);
            Instance.tossDie = true;
            StartCoroutine((IEnumerator)ProcessDiceSetMove(0.010f));
        }

        /// <summary>
        /// Method to toss a dice set
        /// This method raises the dice up, rotates them randomly and then engages the core TS throw dice routine
        /// </summary>
        /// <param name="dicesetName">Name of the dice set to be tossed</param>
        /// <param name="vertical">Optional parameter indciating how far the dice are lifted (default 5 units)</param>
        public void ThrowDiceSet(string dicesetName, Vector3 throwPos)
        {
            Debug.Log("Dice Set Manager Plugin: Throwing DiceSet (Throw Pos="+throwPos+")");
            Instance.rollId = DiceSets[dicesetName].RollId;
            Instance.dicePos = throwPos;
            Instance.tossDie = true;
            StartCoroutine((IEnumerator)ProcessDiceSetMove(0.010f));
        }

        /// <summary>
        /// Method to remove a dice set (removes all dice associated with the dice set)
        /// </summary>
        /// <param name="dicesetName">Name of the dice set to be removed</param>
        public void ClearDiceSet(string dicesetName)
        {
            if (DiceSets.ContainsKey(dicesetName))
            {
                DiceManager dm = DiceManager.Instance;
                foreach (KeyValuePair<string, Action<string>> sub in Subscriptions[SubscriptionEvent.diceClear])
                {
                    sub.Value(JsonConvert.SerializeObject(new Tuple<string, int>(dicesetName, DiceSets[dicesetName].RollId)));
                }
                dm.ClearAllDice(DiceSets[dicesetName].RollId);
                DiceSets.Remove(dicesetName);
            }
            else
            {
                Debug.Log("Dice Set Manager Plugin: No dice set named '" + dicesetName + "'");
            }
        }

        /// <summary>
        /// Method to remove all dice sets (removes all dice associated with any dice set)
        /// </summary>
        public void ClearAllDiceSets()
        {
            for (int ds = 0; ds < DiceSets.Keys.Count; ds++)
            {
                ClearDiceSet(DiceSets.Keys.ElementAt(ds)); ds--;
            }
        }



        /// <summary>
        /// Generic subscription method to subscribe to dice set events
        /// </summary>
        /// <param name="action">The dice set event to subscribe for (can be multiple ORed events)</param>
        /// <param name="identity">Uniquie identity of the subscriber allowing unsubscribing</param>
        /// <param name="callback">Callback function triggered when event occurs with event info returned as a JSON string</param>
        public void Subscribe(SubscriptionEvent action, string identity, Action<string> callback)
        {
            if (((int)action & (int)SubscriptionEvent.diceAdd) > 0)
            {
                Subscriptions[SubscriptionEvent.diceAdd].Add(identity, callback);
            }
            if (((int)action & (int)SubscriptionEvent.diceResult) > 0)
            {
                Subscriptions[SubscriptionEvent.diceResult].Add(identity, callback);
            }
            if (((int)action & (int)SubscriptionEvent.diceClear) > 0)
            {
                Subscriptions[SubscriptionEvent.diceClear].Add(identity, callback);
            }
        }

        /// <summary>
        /// Generic method to unsubscribe from dice set events
        /// </summary>
        /// <param name="action">The dice set event to be unsubscribed (can be multiple ORed events)</param>
        /// <param name="identity">Uniquie identity of the subscriber</param>
        public void Unsubscribe(SubscriptionEvent action, string identity)
        {
            if (((int)action & (int)SubscriptionEvent.diceAdd) > 0)
            {
                Subscriptions[SubscriptionEvent.diceAdd].Remove(identity);
            }
            if (((int)action & (int)SubscriptionEvent.diceResult) > 0)
            {
                Subscriptions[SubscriptionEvent.diceResult].Remove(identity);
            }
            if (((int)action & (int)SubscriptionEvent.diceClear) > 0)
            {
                Subscriptions[SubscriptionEvent.diceClear].Remove(identity);
            }
        }

        private IEnumerator ProcessDiceSetCreation(float waitTime)
        {
            Debug.Log("Dice Set Manager Plugin: Dice Creation Sequence Started");
            yield return new WaitForSeconds(waitTime);
            Debug.Log("Dice Set Manager Plugin: Spawning DiceSet");
            SpawnDice();
            yield return new WaitForSeconds(waitTime);
            Debug.Log("Dice Set Manager Plugin: Tossing DiceSet");
            RegisterDice(rollId);
        }

        private IEnumerator ProcessDiceSetMove(float waitTime)
        {
            bool reachedDestination = false;
            DiceManager dm = DiceManager.Instance;
            DiceSet ds = FindByRollId(Instance.rollId).Value;
            float moveSpeed = 1.0f;
            while (true)
            {
                foreach (Die die in ds.Dice)
                {
                    if (Vector3.Distance(Instance.dicePos, die.gameObject.transform.position) <= (2*moveSpeed))
                    {
                        reachedDestination = true;
                        break;
                    }
                }
                if(!reachedDestination)
                {
                    Vector3 trajectory = new Vector3(Instance.dicePos.x - ds.Dice[0].gameObject.transform.position.x, Instance.dicePos.y - ds.Dice[0].gameObject.transform.position.y, Instance.dicePos.z - ds.Dice[0].gameObject.transform.position.z);
                    trajectory = Vector3.ClampMagnitude(trajectory, moveSpeed);
                    dm.GatherDice(ds.Dice[0].gameObject.transform.position + trajectory, ds.RollId);
                    yield return new WaitForSeconds(waitTime);
                }
                else
                {
                    dm.GatherDice(Instance.dicePos, ds.RollId);
                    Debug.Log("Dice Set Manager Plugin: Move Complete");
                    if (Instance.tossDie)
                    {
                        Debug.Log("Dice Set Manager Plugin: Randomizing Dice");
                        foreach (Die die in ds.Dice)
                        {
                            die.gameObject.transform.rotation = Quaternion.Euler(ran.Next(0, 180), ran.Next(0, 180), ran.Next(0, 180));
                        }
                        Debug.Log("Dice Set Manager Plugin: Tossing Dice Set");
                        dm.ThrowDice(ds.RollId);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Method used spawn the dice in the dice tray to the board
        /// </summary>
        private void SpawnDice()
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
        private void RegisterDice(int rollId)
        {
            DiceManager dm = DiceManager.Instance;
            KeyValuePair<string, DiceSet> diceset = FindByRollId(rollId);
            Vector3 pos = diceset.Value.Dice[0].gameObject.transform.position;
            dm.GatherDice(new Vector3(pos.x, pos.y, pos.z + 1f), rollId);
            dm.ThrowDice(rollId);
        }

        /// <summary>
        /// Method to locate a dice set by the corresponding roll Id
        /// </summary>
        /// <param name="rollId">RollId of the dice set to be looked up</param>
        /// <returns>KeyValuePair of string and Diceset representing the dice set name and the dice set contents</returns>
        private KeyValuePair<string, DiceSet> FindByRollId(int rollId)
        {
            foreach (KeyValuePair<string, DiceSet> diceset in DiceSets)
            {
                if (diceset.Value.RollId == rollId) { return diceset; }
            }
            return default(KeyValuePair<string, DiceSet>);
        }
    }
}
