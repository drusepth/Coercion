using DIRC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Coercion
{
    class Game
    {
        public bool isPaused = true;

        public List<Mission> activeMissions = new List<Mission>();
        public List<Player> activePlayers = new List<Player>();
        public List<string> gameWords = new List<string>(File.ReadAllLines("../../Wordlists/global-wordlist.txt"));

        public Scoreboard scoreboard = new Scoreboard("../../Scoreboard.txt");

        public void PauseIfNecessary(Connection irc)
        {
            bool wasPaused = isPaused;
            isPaused = (activePlayers.Count < 2);

            if (!wasPaused && isPaused)
            {
                NotifyPlayers(irc, "There are currently only " + activePlayers.Count + " player at the moment. I will notify you when a new job comes in (4 players are needed to continue).");
            }

            if (wasPaused && !isPaused)
            {
                NotifyPlayers(irc, "Good news, assassin. There are now " + activePlayers.Count + " heads in the guild and play has resumed. I will be in touch with you shortly with some dirty work. Don't forget, if you ever forget your mission you may message me with !mission to be reminded.");
            }
        }

        public void UpdateGameWords(string channel)
        {
            ClearGameWords();
            AddGameWords(channel);
        }

        public void ClearGameWords()
        {
            gameWords = new List<string>(File.ReadAllLines("../../Wordlists/global-wordlist.txt"));
        }

        public void AddGameWords(string channel)
        {
            try
            {
                gameWords.AddRange(new List<string>(File.ReadAllLines("../../Wordlists/" + channel + ".txt")));
            }
            catch (Exception e)
            {

            }
        }

        public void NotifyPlayers(Connection irc, string message)
        {
            foreach (Player p in activePlayers)
            {
                irc.MessageUser(p.Name, message);
            }
        }

        public void NotifyPlayer(Connection irc, string player, string message)
        {
            irc.MessageUser(player, message);
        }

        public bool IsPlayerInGame(string name, string host)
        {
            Player game_player = new Player(name, host);
            foreach (Player p in activePlayers)
            {
                // #todo remove Name == name for security and multiname protection
                if (game_player == p)
                {
                    return true;
                }
            }
            return false;
        }

        public bool AddPlayer(string name, string host)
        {
            Player player = new Player(name, host);

            if (!scoreboard.Contains(player.Name))
            {
                scoreboard.Initialize(player.Name);
            }

            // Don't do anything if this player is already in the game
            if (IsPlayerInGame(name, host))
            {
                return false;
            }

            activePlayers.Add(player);
            return true;
        }

        public bool RemovePlayer(Connection irc, string name, string host)
        {
            Player player = new Player(name, host);

            // Don't do anything if this player is not in the game
            if (!IsPlayerInGame(name, host))
            {
                return false;
            }

            // Find player and remove them
            for (int i = 0; i < activePlayers.Count; i++)
            {
                if (activePlayers[i] == player)
                {
                    activePlayers.RemoveAt(i);
                }
            }

            // Cancel all open missions with them as the target and assign new missions
            for (int i = 0; i < activeMissions.Count; i++)
            {
                if (activeMissions[i].Target == player)
                {
                    irc.MessageUser(activeMissions[i].Assassin.Name, "Hey there. Your active assignment on " + activeMissions[i].Target.Name + " has expired. As soon as I come across a new job, I'll be in touch.");
                    activeMissions.RemoveAt(i);
                    i--;
                }
            }

            return true;
        }

        public void EnsureEveryoneHasAMission(Connection irc)
        {
            foreach (Player p in activePlayers)
            {
                if (!HasAMission(p))
                {
                    Mission m = AssignMissionTo(irc, p);
                    if (m != null)
                    {
                        irc.MessageUser(p.Name, "Pssst; I have a mission for you. The Guild needs you to make " + m.Target.Name + " say " + m.Word + " as soon as possible. Can you handle that?");
                    }
                }
            }
        }

        public List<Mission> MissionsWithTarget(Player p)
        {
            List<Mission> missions = new List<Mission>();
            foreach (Mission m in activeMissions)
            {
                if (m.Target == p)
                {
                    missions.Add(m);
                }
            }

            return missions;
        }

        public void CompleteMission(Mission m)
        {
            // Give points to the assassin
            scoreboard.AddScoreFor(m.Assassin.Name);

            // Remove the mission from active missions
            activeMissions.Remove(m);

            // Save the scoreboards
            scoreboard.SaveScores();
        }

        public void FailMission(Player p)
        {
            Mission m = GetMissionFor(p);

            // Remove mark for failure
            scoreboard.RemoveScoreFor(p.Name);

            // Remove the mission from active missions
            activeMissions.Remove(m);

            // Save the scoreboards
            scoreboard.SaveScores();
        }

        public bool HasAMission(Player p)
        {
            foreach (Mission m in activeMissions)
            {
                if (m.Assassin == p)
                {
                    return true;
                }
            }

            return false;
        }

        public Mission AssignMissionTo(Connection irc, Player p)
        {
            if (isPaused)
            {
                return null;
            }

            Random rng = new Random();

            List<string> potential_targets = new List<string>();
            ClearGameWords();
            foreach (string channel in irc.UserChannels(p.Name))
            {
                AddGameWords(channel);

                Console.WriteLine("Gettting users in " + channel);
                potential_targets.AddRange(irc.UsersInChannel("#" + channel));
            }

            // Strip out target duplicates
            potential_targets = potential_targets.Distinct().ToList();

            // Strip out obvious problems
            for (int i = 0; i < potential_targets.Count; i++)
            {
                if (potential_targets[i] == "Boros" || potential_targets[i] == p.Name || potential_targets[i] == "nanobot")
                {
                    potential_targets.RemoveAt(i);
                    i--;
                }
            }

            if (potential_targets.Count == 0)
            {
                return null;
            }

            string target = potential_targets[rng.Next(0, potential_targets.Count)];

            while (activePlayers.Count > 1 && target == p.Name)
            {
                target = potential_targets[rng.Next(0, potential_targets.Count)];
            }

            string word = gameWords[rng.Next(0, gameWords.Count)];

            Mission mission = new Mission(p, new Player(target, ""), word);
            activeMissions.Add(mission);

            return mission;
        }

        public Mission GetMissionFor(Player p)
        {
            if (IsPlayerInGame(p.Name, p.Host))
            {
                foreach (Mission m in activeMissions)
                {
                    if (m.Assassin == p)
                    {
                        return m;
                    }
                }
            }
            return null;
        }
    }
}
