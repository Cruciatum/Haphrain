using System;
using System.Collections.Generic;
using System.Text;

namespace Haphrain.Classes.MortyGame
{
    internal class Character
    {
        #region Props
        internal short CharID { get; set; }
        internal string CharName { get; set; }
        internal string Type { get; set; }
        internal string Rarity { get; set; }
        internal short HP { get; set; }
        internal short ATK { get; set; }
        internal short DEF { get; set; }
        internal short SPD { get; set; }
        internal int StatTotal { get; set; }
        internal short NeededToEvolve { get; set; }
        internal Character EvolvesTo { get; set; }
        internal string Dimension { get; set; }
        #endregion
    }
}
