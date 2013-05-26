using System;
using System.IO;
using System.Collections.Generic;

namespace Coercion
{
    public class Scoreboard
    {
        string backend;
        public Dictionary<string, int> Data = new Dictionary<string, int>();

        public Scoreboard(string filename)
        {
            backend = filename;
        }

        public void LoadScores()
        {
            
        }
    }
}
