using BepInEx;
using BepInEx.Configuration;
using System;
using UnityEngine;

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace LordAshes
{
	[BepInPlugin(Guid, Name, Version)]
	public partial class DiceSetManagerPlugin : BaseUnityPlugin
	{
		// Plugin info
		public const string Name = "Dice Set Manager Plug-In";
		public const string Guid = "org.lordashes.plugins.dicesetmanager";
		public const string Version = "1.1.0.0";

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
		{
			UnityEngine.Debug.Log("Dice Set Manager Plugin: Active.");

			SetInstance(this);
			var harmony = new Harmony(Guid);
			harmony.PatchAll();

			Subscribe(SubscriptionEvent.diceAdd | SubscriptionEvent.diceResult | SubscriptionEvent.diceClear, "Bob", (s) => Debug.Log(s));
		}

		/// <summary>
		/// Function of spawning dice from the dice tray to the board and registering the dice set.
		/// Most of the time this function just exits but when a new dice set is created it transition
		/// teh dice set from teh tray to the board and the rolls the dice to register them.
		/// </summary>
		void Update()
		{
			if(dolly==null && IsBoardLoaded())
            {
				dolly = new GameObject();
				dolly.transform.position = Vector3.zero;
				dolly.transform.rotation = Quaternion.Euler(Vector3.zero);
				camera = dolly.AddComponent<Camera>();
				camera.transform.position = Vector3.zero;
				camera.transform.rotation = Quaternion.Euler(Vector3.zero);
				camera.enabled = false;
			}

			if (Input.GetKeyUp(KeyCode.D))
            {
				SystemMessage.AskForTextInput("Dice Creation", "Dice To Create (Name,Formula):", "OK", (s) => CreateDiceSet(s.Split(':')[0], s.Split(':')[1].Replace("X","D").Replace("x","D")), null, "Cancel", null);
            }
			if (Input.GetKeyUp(KeyCode.Period))
			{
				ThrowDiceSet("Fire", 5f);
			}
			if (Input.GetKeyUp(KeyCode.Comma))
			{
				DiceCamSetup(5, 70, 20, 25);
				DiceCamMoveTo(new Vector3(0, 3, -3));
				DiceCamRotateTo(new Vector3(45,0,0));
				ThrowDiceSet("Fire", new Vector3(0,5,0));
				DiceCamTrackDiceSet("Fire");
			}
		}

		private bool IsBoardLoaded()
		{
			return (CameraController.HasInstance && BoardSessionManager.HasInstance && BoardSessionManager.HasBoardAndIsInNominalState && !BoardSessionManager.IsLoading);
		}
	}
}
