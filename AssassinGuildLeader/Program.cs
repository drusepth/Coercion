﻿using System;
using DIRC;
using System.Collections.Generic;

namespace Coercion
{
    class Program
    {
        static Game coercion = new Game();
        static Connection irc = new Connection("Boros", "irc.tddirc.net", 6667);

        static void GameLogic(string line)
        {
            // Ensure we have enough players to continue playing
            coercion.PauseIfNecessary(irc);

            // Make sure everyone playing has a mission
            coercion.EnsureEveryoneHasAMission(irc);

            // Update game target words
            coercion.UpdateGameWords();

            // Handle user commands
            if (ParseIRC.IsMessage(line))
            {
                string player = ParseIRC.GetUsernameSpeaking(line);
                string host = ParseIRC.GetHostSpeaking(line);
                string message = ParseIRC.GetSpokenLine(line);

                switch (message)
                {
                    case "!play":
                        bool newPlayer = coercion.AddPlayer(player, host);
                        if (newPlayer)
                        {
                            coercion.NotifyPlayer(irc, player, "You there -- yes, you. I've noticed you lurking these channels for a while now, and I have a job for you. A... dark job, if you think you can handle it. My name is Boros, and I am the leader of the Assassin Guild here on TDDIRC.");
                            Mission mission = coercion.AssignMissionTo(new Player(player, host));
                            if (mission == null)
                            {
                                coercion.NotifyPlayer(irc, player, "The game will resume when there are 4 people playing (there are currently " + coercion.activePlayers.Count + " playing). You will be assigned your first mission at that time.");
                            }
                            else
                            {
                                coercion.NotifyPlayer(irc, player, "Pssst. I have a mission for you: I need you to slyly make " + mission.Target.Name + " say '" + mission.Word + "' using any means neccessary. Good luck.");
                            }
                        }
                        break;

                    case "!quit":
                        bool removedPlayer = coercion.RemovePlayer(player, host);
                        if (removedPlayer)
                        {
                            coercion.NotifyPlayer(irc, player, "You can run, but you can't hide!");
                        }
                        break;

                    case "!mission":
                        if (coercion.IsPlayerInGame(player, host) && coercion.HasAMission(new Player(player, host)))
                        {
                            Player p = new Player(player, host);
                            Mission mission = coercion.GetMissionFor(p);
                            coercion.NotifyPlayer(irc, player, "Your mission is to make " + mission.Target.Name + " say " + mission.Word + ".");
                        }
                        
                        break;

                    case "!score":
                    case "!scores":
                        if (coercion.IsPlayerInGame(player, host)) {
                            coercion.NotifyPlayer(irc, player, "You have " + coercion.scoreboard[player] + " marks.");
                        }
                        break;

                    case "!scoreboard":
                        if (coercion.IsPlayerInGame(player, host))
                        {
                            foreach (string p in coercion.scoreboard.Keys)
                            {
                                if (coercion.scoreboard[p] > 0)
                                {
                                    coercion.NotifyPlayer(irc, player, p + " has " + coercion.scoreboard[p] + " marks.");
                                }
                            }
                        }
                        break;

                    case "!rules":
                        coercion.NotifyPlayer(irc, player, "The rules are simple. I assign you a target and a word; and it is your mission to make your target say that word using any means neccessary. A successful mission will result in a nice reward for you.");
                        break;

                    case "!players":
                        coercion.NotifyPlayer(irc, player, "There are currently " + coercion.activePlayers.Count + " assassins in the guild.");
                        break;
                }
            }
        }

        static void ListenForKills(string line)
        {
            if (!ParseIRC.IsMessage(line))
            {
                return;
            }

            string name = ParseIRC.GetUsernameSpeaking(line);
            string host = ParseIRC.GetHostSpeaking(line);
            string message = ParseIRC.GetSpokenLine(line);
            Player player = new Player(name, host);

            List<string> words_said = new List<string>(message.ToLower().Split(' '));

            if (coercion.IsPlayerInGame(name, host))
            {
                List<Mission> relevantMissons = coercion.MissionsWithTarget(player);
                if (relevantMissons.Count > 0)
                {
                    foreach (Mission m in relevantMissons)
                    {
                        if (words_said.Contains(m.Word))
                        {
                            coercion.CompleteMission(m);
                            irc.MessageUser(m.Assassin.Name, "Congratulations, you've successfully coerced " + m.Target.Name + " to say " + m.Word + ". I'll be awarding you an Assassin's Mark (type !scores to see how many you've accumulated, and !scoreboard to compare yourself with others) and will be in contact with a new target soon. Good work.");
                            irc.MessageChannel("#guild", m.Assassin.Name + " has convinced " + m.Target.Name + " to say " + m.Word + ". +1 marks (" + coercion.scoreboard[m.Assassin.Name] + " total marks)");
                            // Can assign new mission here immediately or wait until next line said
                        }
                    }
                }
            }
        }

        static void ListenForTargets(string line)
        {
            /*
            string[] splitLine = line.Split(' ');

            if (splitLine.Length > 0 && splitLine[1] == "353")
            {
                for (int i = 6; i < splitLine.Length - 1; i++)
                {
                    string name = splitLine[i].Replace('@', ' ').Replace('+', ' ').Replace('~', ' ').Replace('%', ' ').Trim();
                    if (!coercion.activeTargets.Contains(name))
                    {
                        coercion.activeTargets.Add(name);
                    }
                }
            }
            */
        }

        static void ConversationLog(string line)
        {
            if (ParseIRC.IsMessage(line))
            {
                Console.WriteLine("{0}: {1}", ParseIRC.GetUsernameSpeaking(line), ParseIRC.GetSpokenLine(line));
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        static void Main(string[] args)
        {
            // Set up bot
            irc.AddChannel("#guild");
            irc.AddChannel("#test");
            //irc.AddChannel("#hackerthreads");
            //irc.AddChannel("#thunked");
            //irc.AddChannel("#shells");
            //irc.AddChannel("#CoreCraft");
            irc.AddLineHandler(GameLogic);
            irc.AddLineHandler(ConversationLog);
            irc.AddLineHandler(ListenForKills);
            irc.AddLineHandler(ListenForTargets);

            // Connect and start game
            irc.Connect();
        }
    }
}
