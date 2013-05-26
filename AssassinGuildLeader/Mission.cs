using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coercion
{
    class Mission
    {
        public Player Assassin;
        public Player Target;
        public string Word;

        public Mission(Player player, Player target, string word)
        {
            Assassin = player;
            Target = target;
            Word = word;
        }
    }
}
