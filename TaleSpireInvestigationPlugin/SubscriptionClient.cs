using BepInEx;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LordAshes
{
    public partial class DiceSetManagerPlugin : BaseUnityPlugin
    {
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
    }
}
