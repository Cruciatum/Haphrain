using System;
using System.Collections.Generic;
using System.Text;

namespace Haphrain.Game.Lib
{
    internal class Character
    {
        public ulong CharacterID { get; set; }

        public uint Health { get; set; }

        public uint Attack { get; set; }

        public uint Defense { get; set; }

        public string CharacterName { get; set; }
    }
}
