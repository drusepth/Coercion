using DIRC;
using System;
using System.Collections.Generic;
using System.IO;

namespace Coercion
{
    class Game
    {
        public bool isPaused = true;

        public List<Mission> activeMissions = new List<Mission>();
        public List<Player> activePlayers = new List<Player>();
        public List<string> gameWords = new List<string>(File.ReadAllLines("../../Words.txt"));

        public Scoreboard scoreboard = new Scoreboard("../../Scoreboard.txt");

        public void PauseIfNecessary(Connection irc)
        {
            bool wasPaused = isPaused;
            isPaused = (activePlayers.Count < 4);

            if (!wasPaused && isPaused)
            {
                NotifyPlayers(irc, "There are currently only " + activePlayers.Count + " player at the moment. I will notify you when a new job comes in (4 players are needed to continue).");
            }

            if (wasPaused && !isPaused)
            {
                NotifyPlayers(irc, "Good news, assassin. There are now " + activePlayers.Count + " heads in the game, and play has resumed. Don't forget, if you ever forget your mission you may message me with !mission to be reminded.");
            }
        }

        public void UpdateGameWords()
        {
            Random rng = new Random();

            // Only update game words every 10 lines or so
            if (rng.Next(0, 10) == 1)
            {
                gameWords = new List<string>(File.ReadAllLines("../../Words.txt"));
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

        public bool RemovePlayer(string name, string host)
        {
            Player player = new Player(name, host);

            // Don't do anything if this player is not in the game
            if (!IsPlayerInGame(name, host))
            {
                return false;
            }

            activePlayers.Remove(player);
            return true;
        }

        public void EnsureEveryoneHasAMission(Connection irc)
        {
            foreach (Player p in activePlayers)
            {
                if (!HasAMission(p))
                {
                    Mission m = AssignMissionTo(p);
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

        public Mission AssignMissionTo(Player p)
        {
            if (isPaused)
            {
                return null;
            }

            Random rng = new Random();
            Player target = activePlayers[rng.Next(0, activePlayers.Count)];

            while (activePlayers.Count > 1 && target == p)
            {
                target = activePlayers[rng.Next(0, activePlayers.Count)];
            }

            string word = gameWords[rng.Next(0, gameWords.Count)];

            Mission mission = new Mission(p, target, word);
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
