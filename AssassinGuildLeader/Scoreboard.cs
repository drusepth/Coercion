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

        public void Initialize(string person)
        {
            Data.Add(person, 0);
        }

        public bool Contains(string person)
        {
            return Data.ContainsKey(person);
        }

        public int ScoreFor(string person)
        {
            return Data[person];
        }

        public void AddScoreFor(string person)
        {
            Data[person]++;
        }

        public List<string> People()
        {
            return new List<string>(Data.Keys);
        }

        public void LoadScores()
        {
            List<string> data = new List<string>(File.ReadAllLines(backend));
            foreach (string line in data)
            {
                string[] split = line.Split(' ');
                Data.Add(split[0], Int32.Parse(split[1]));
            }
        }

        public void SaveScores()
        {
            using (StreamWriter file = new StreamWriter(backend))
            {
                foreach (string person in Data.Keys)
                {
                    file.WriteLine("{0} {1}", person, Data[person]);
                }
            }
        }
    }
}
