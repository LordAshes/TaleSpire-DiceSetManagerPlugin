using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace LordAshes
{
	[BepInPlugin(Guid, Name, Version)]
	public partial class DiceSetManagerPlugin : BaseUnityPlugin
	{
		// Plugin info
		public const string Name = "Dice Set Manager Plug-In";
		public const string Guid = "org.lordashes.plugins.dicesetmanager";
		public const string Version = "1.1.1.0";

		

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

			if (StrictKeyCheck(new KeyboardShortcut(KeyCode.D, KeyCode.RightControl)))
            {
				SystemMessage.AskForTextInput("Dice Creation", "Dice To Create (Name,Formula):", "OK", (s) => CreateDiceSet(s.Split(':')[0], s.Split(':')[1].Replace("X","D").Replace("x","D")), null, "Cancel", null);
            }

			if (StrictKeyCheck(new KeyboardShortcut(KeyCode.F, KeyCode.RightControl)))
			{
				Debug.Log("Tossing "+DiceSets.ElementAt(0).Key+": "+DiceSets.ElementAt(0).Value.Formula);
				DiceCamSetup(5, 70, 20, 25);
				DiceCamMoveTo(new Vector3(0, 3, -3));
				DiceCamRotateTo(new Vector3(45,0,0));
				ThrowDiceSet(DiceSets.ElementAt(0).Key, new Vector3(0,5,0));
				DiceCamTrackDiceSet(DiceSets.ElementAt(0).Key);
			}
		}

		private bool IsBoardLoaded()
		{
			return (CameraController.HasInstance && BoardSessionManager.HasInstance && BoardSessionManager.HasBoardAndIsInNominalState && !BoardSessionManager.IsLoading);
		}

		public static bool StrictKeyCheck(KeyboardShortcut check)
		{
			if (!check.IsUp()) { return false; }
			foreach (KeyCode modifier in new KeyCode[] { KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift })
			{
				if (Input.GetKey(modifier) != check.Modifiers.Contains(modifier)) { return false; }
			}
			return true;
		}

	}
}
