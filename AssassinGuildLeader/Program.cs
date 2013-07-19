using System;
using DIRC;
using System.Collections.Generic;

namespace Coercion
{
    class Program
    {
        static Game coercion = new Game();
        static Connection irc = new Connection("boros", "irc.amazdong.com", 4080);

        static void GameLogic(string line)
        {
            // Ensure we have enough players to continue playing
            coercion.PauseIfNecessary(irc);

            // Make sure everyone playing has a mission
            coercion.EnsureEveryoneHasAMission(irc);

            // Update game target words
            coercion.UpdateGameWords("");

            // Handle user commands
            if (ParseIRC.IsMessage(line))
            {
                string player = ParseIRC.GetUsernameSpeaking(line);
                string host = ParseIRC.GetHostSpeaking(line);
                string message = ParseIRC.GetSpokenLine(line);
                string channel = ParseIRC.GetChannel(line);

                switch (message)
                {
                    case "!start":
                    case "!assassin":
                    case "!play":
                        bool newPlayer = coercion.AddPlayer(player, host);
                        if (newPlayer)
                        {
                            coercion.NotifyPlayer(irc, player, "You there -- yes, you. I've noticed you lurking these channels for a while now, and I have a job for you. A... dark job, if you think you can handle it. My name is Boros, and I am the leader of the Assassin Guild here on TDDIRC.");
                            Mission mission = coercion.AssignMissionTo(irc, new Player(player, host));

                            if (mission == null)
                            {
                                coercion.NotifyPlayer(irc, player, "It's in everyone's best interest to wait until there are more assassins in the Guild (there are currently " + coercion.activePlayers.Count + "). You will be assigned your first mission when the head count is high enough.");
                            }
                            else
                            {
                                coercion.NotifyPlayer(irc, player, "Pssst. I have a mission for you: I need you to slyly make " + mission.Target.Name + " say '" + mission.Word + "' using any means neccessary. Good luck.");
                            }
                        }
                        break;

                    case "!stop":
                    case "!quit":
                    case "!optout":
                        bool removedPlayer = coercion.RemovePlayer(irc, player, host);
                        if (removedPlayer)
                        {
                            coercion.NotifyPlayer(irc, player, "You can run, but you can't hide...");
                        }
                        break;

                    case "!reminder":
                    case "!mission":
                        if (coercion.IsPlayerInGame(player, host) && coercion.HasAMission(new Player(player, host)))
                        {
                            Player p = new Player(player, host);
                            Mission mission = coercion.GetMissionFor(p);
                            coercion.NotifyPlayer(irc, player, "Your mission is to make <" + mission.Target.Name + "> say '" + mission.Word + "'. If you can't, I could probably find you some new work if you type !newmission, but it'll cost you a mark.");
                        }
                        break;

                    case "!skipmission":
                    case "!nextmission":
                    case "!newmission":
                        if (coercion.IsPlayerInGame(player, host) && coercion.HasAMission(new Player(player, host)))
                        {
                            if (coercion.scoreboard.ScoreFor(player) > 0)
                            {
                                coercion.NotifyPlayer(irc, player, "I see. Well, if you can't handle it, you can't handle it. Fortunately for you, an Assassin's work is never done. I'll give you a new mission... but it's going to cost you a mark. I'll be in touch.");
                                coercion.FailMission(new Player(player, host));
                            }
                            else if (coercion.scoreboard.ScoreFor(player) > -3)
                            {
                                coercion.NotifyPlayer(irc, player, "Maybe you're not cut out to be an assassin after all; to think I saw potential in you! Maybe.. maybe you'll manage it. A new target usually costs a mark but, seeing you don't have any, lets just drop you negative and look the other way. I'll be in touch with a new job soon: a chance to redeem yourself.");
                                coercion.FailMission(new Player(player, host));
                            }
                            else
                            {
                                coercion.NotifyPlayer(irc, player, "You've had your chances. Either prove yourself as an assassin or leave the guild. No more do-overs, no more lenience, no more new missions. Take down your target first if you want another job.");
                            }
                        }
                        break;

                    case "!score":
                    case "!scores":
                        if (coercion.IsPlayerInGame(player, host)) {
                            coercion.NotifyPlayer(irc, player, "You have " + coercion.scoreboard.ScoreFor(player) + " marks.");
                        }
                        break;

                    case "!leaderboard":
                    case "!scoreboard":
                        if (coercion.IsPlayerInGame(player, host))
                        {
                            foreach (string p in coercion.scoreboard.People())
                            {
                                if (coercion.scoreboard.ScoreFor(p) > 0)
                                {
                                    coercion.NotifyPlayer(irc, player, p + " has " + coercion.scoreboard.ScoreFor(p) + " marks.");
                                }
                            }
                        }
                        break;

                    case "!rules":
                        coercion.NotifyPlayer(irc, player, "The rules are simple. I assign you a target and a word; and it is your mission to make your target say that word using any means neccessary. A successful mission will result in a nice reward for you. To get started, type !play");
                        break;

                    case "!assassins":
                    case "!players":
                        coercion.NotifyPlayer(irc, player, "There are currently " + coercion.activePlayers.Count + " assassins in the guild.");
                        break;

                    case "!halp":
                    case "!wat":
                    case "!help":
                        coercion.NotifyPlayer(irc, player, "Looking for help, I see? You're a lucky one; I just happen to overhear. I've noticed you here a few times and would like to extend a.. dangerous offer. If you're interested, type !rules to hear more.");
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

            if (coercion.IsPlayerInGame(name, host))
            {
                List<Mission> relevantMissons = coercion.MissionsWithTarget(player);
                if (relevantMissons.Count > 0)
                {
                    foreach (Mission m in relevantMissons)
                    {
                        // If the mission's word appears *somewhere* in this message, count it
                        if (message.ToLower().IndexOf(m.Word) > -1)
                        {
                            coercion.CompleteMission(m);
                            irc.MessageUser(m.Assassin.Name, "Congratulations, you've successfully coerced " + m.Target.Name + " to say " + m.Word + ". I'll be awarding you an Assassin's Mark (type !scores to see how many you've accumulated, and !scoreboard to compare yourself with others) and will be in contact with a new target soon. Good work.");
                            irc.MessageChannel("#guild", m.Assassin.Name + " has convinced " + m.Target.Name + " to say " + m.Word + ". +1 marks (" + coercion.scoreboard.ScoreFor(m.Assassin.Name) + " total marks)");
                            // Can assign new mission here immediately or wait until next line said
                        }
                    }
                }
            }
        }

        static void ListenForTargets(string line)
        {
            string[] splitLine = line.Split(' ');

            if (splitLine.Length > 1 && splitLine[1] == "QUIT")
            {
                string name = ParseIRC.GetUsernameSpeaking(line);
                string host = ParseIRC.GetHostSpeaking(line);

                coercion.RemovePlayer(irc, name, host);
            }

            // :test!dru@dru.dru NICK :dru
            if (splitLine.Length > 1 && splitLine[1] == "NICK")
            {
                string name = ParseIRC.GetUsernameSpeaking(line);
                string host = ParseIRC.GetHostSpeaking(line);

                coercion.RemovePlayer(irc, name, host);
            }
        }

        static void CacheUserChannels(string line)
        {

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
            //irc.AddChannel("#interns");
            irc.AddLineHandler(GameLogic);
            irc.AddLineHandler(ConversationLog);
            irc.AddLineHandler(ListenForKills);
            irc.AddLineHandler(ListenForTargets);

            // Connect and start game
            irc.Connect();
        }
    }
}
