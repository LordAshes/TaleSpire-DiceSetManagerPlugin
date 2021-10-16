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
        /// Class for DiceSet which relates roll ids with the dice that comprise the roll
        /// </summary>
        public class DiceSet
        {
            public int RollId { get; set; } = -1;
            public string Formula { get; set; } = "";
            public List<Die> Dice { get; set; } = new List<Die>();
        }

        /// <summary>
        /// Class for holding dice set roll results
        /// </summary>
        public class DiceSetRollSpecs
        {
            // Dice set name
            public string Name { get; set; } = "";
            // Dice set total
            public int Total { get; set; } = 0;
            // Dice set roll details
            public DiceManager.DiceRollResultData Details { get; set; } = null;
        }
    }
}
