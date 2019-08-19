using System;
using System.Collections.Generic;
using System.Text;

namespace Haphrain.Game.Lib
{
    internal class Boss : Character
    {
        public Difficulty BossDifficulty { get; set; }

        public enum Difficulty { Tutorial = 1, Easy = 5, Medium = 10, Hard = 20, Nightmare = 100 }

        private Random RandomNum = new Random();

        public Boss(uint baseHP, Difficulty bossDifficulty)
        {
            BossDifficulty = bossDifficulty;
            if (BossDifficulty != Difficulty.Tutorial) { Health = baseHP * (uint)RandomNum.Next((int)Math.Round((double)BossDifficulty/2d), (int)BossDifficulty); }
            Attack = (uint)BossDifficulty;
            Defense = (uint)BossDifficulty;
        }
    }
}
