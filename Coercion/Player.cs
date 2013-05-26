using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coercion
{
    public class Player
    {
        public string Name;
        public string Host;

        public Player(string name, string host)
        {
            Name = name;
            Host = host;
        }

        public static bool operator ==(Player p1, Player p2)
        {
            return p1.Name == p2.Name && p1.Host == p2.Host;
        }

        public static bool operator !=(Player p1, Player p2)
        {
            return !(p1 == p2);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /*
        public static implicit operator string(Player instance)
        {
            if (instance == null)
            {
                return "";
            }
            return instance.Name;
        }
        */
    }
}
