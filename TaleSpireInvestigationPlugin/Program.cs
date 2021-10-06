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
		public const string Version = "1.0.0.0";

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
		{
			UnityEngine.Debug.Log("Dice Set Plugin: Active.");

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
			DiceSetManager.Internal.UpdateDiceSetSequence();
		}
	}
}
