using System;
using DemoInfo;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace CsGoVis
{
    class Program
    {
        static string RowString(Dictionary<string, string> values)
        {
            return string.Join(
                ",",
                values.GetValueOrDefault("map", ""),
                values.GetValueOrDefault("tick", ""),
                values.GetValueOrDefault("timestamp", ""),
                values.GetValueOrDefault("event", ""),
                values.GetValueOrDefault("winner", ""),
                values.GetValueOrDefault("round", ""),
                values.GetValueOrDefault("t1_score", ""),
                values.GetValueOrDefault("t2_score", ""),
                values.GetValueOrDefault("x", ""),
                values.GetValueOrDefault("y", ""),
                values.GetValueOrDefault("view_x", ""),
                values.GetValueOrDefault("view_y", ""),
                values.GetValueOrDefault("player", ""),
                values.GetValueOrDefault("team", ""),
                values.GetValueOrDefault("side", ""),
                values.GetValueOrDefault("kills", ""),
                values.GetValueOrDefault("deaths", ""),
                values.GetValueOrDefault("money", ""),
                values.GetValueOrDefault("weapon", ""),
                values.GetValueOrDefault("alive", ""),
                values.GetValueOrDefault("freeze_equip_value", ""),
                values.GetValueOrDefault("current_equip_value", ""),
                values.GetValueOrDefault("last_alive_pos_x", ""),
                values.GetValueOrDefault("last_alive_pos_y", ""),
                values.GetValueOrDefault("attacker", ""),
                values.GetValueOrDefault("attacker_pos_x", ""),
                values.GetValueOrDefault("attacker_pos_y", ""),
                values.GetValueOrDefault("attacker_team", ""),
                values.GetValueOrDefault("attacker_side", ""),
                values.GetValueOrDefault("bomb_site", ""),
                values.GetValueOrDefault("bomb_status", ""),
                values.GetValueOrDefault("bomb_planttime", "")
                );
        }

        static Dictionary<string, string> InitialValues(DemoParser parser)
        {
            var round = parser.TScore + parser.CTScore + 1;
            var dict = new Dictionary<string, string>();
            dict.Add("tick", parser.CurrentTick.ToString());
            dict.Add("timestamp", parser.CurrentTime.ToString());
            dict.Add("t1_score", (round < 16 ? parser.CTScore : parser.TScore).ToString());
            dict.Add("t2_score", (round < 16 ? parser.TScore : parser.CTScore).ToString());
            dict.Add("round", (parser.TScore + parser.CTScore + 1).ToString());
            return dict;
        }

        static string GetTeam(DemoParser parser, Player player)
        {
            var round = parser.TScore + parser.CTScore + 1;
            if (round < 16 && player.Team == Team.CounterTerrorist || round >= 16 && player.Team == Team.Terrorist)
            {
                return "Team 1";
            } else
            {
                return "Team 2";
            }
        }

        public static void Main(string[] args)
        {
            string header = string.Join(
                ",",
                "map",
                "tick",
                "timestamp",
                "event",
                "winner",
                "round",
                "t1_score",
                "t2_score",
                "x",
                "y",
                "view_x",
                "view_y",
                "player",
                "team",
                "side",
                "kills",
                "deaths",
                "money",
                "weapon",
                "alive",
                "freeze_equip_value",
                "current_equip_value",
                "last_alive_pos_x",
                "last_alive_pos_y",
                "attacker",
                "attacker_pos_x",
                "attacker_pos_y",
                "attacker_team",
                "attacker_side",
                "bomb_site",
                "bomb_status",
                "bomb_planttime"
            );

            Console.WriteLine(header);
            var timer = new Stopwatch();
            timer.Start();
            foreach(var file in args)
            {
                using (var fs = File.OpenRead(file))
                {
                    using (var parser = new DemoParser(fs))
                    {
                        parser.ParseHeader();

                        var outPrefix = parser.Header.Filestamp;
                        bool started = false;
                        parser.MatchStarted += (sender, e) =>
                        {
                            started = true;
                            var values = InitialValues(parser);
                            values.Add("event", "round_start");
                            values.Add("map", parser.Header.MapName);
                            Console.WriteLine(RowString(values));
                        };

                        while (!started)
                        {
                            parser.ParseNextTick();
                        }

                        bool bombIsPlanted = false;
                        parser.RoundStart += (sender, e) =>
                        {
                            bombIsPlanted = false;
                            var values = InitialValues(parser);
                            values.Add("event", "round_start");
                            values.Add("map", parser.Header.MapName);
                            Console.WriteLine(RowString(values));
                        };

                        parser.RoundEnd += (sender, e) =>
                        {
                            var values = InitialValues(parser);
                            values.Add("event", "round_end");
                            values.Add("winner", e.Winner.ToString());
                            values.Add("reason", e.Reason.ToString());

                            Console.WriteLine(RowString(values));
                        };


                        string bombPosX = "", bombPosY = "";
                        string site = "";
                        string status = "";
                        string planttime = "";

                        parser.BombPlanted += (sender, e) =>
                        {
                            bombIsPlanted = true;
                            bombPosX = e.Player.Position.X.ToString();
                            bombPosY = e.Player.Position.Y.ToString();
                            site = e.Site.ToString();
                            status = "planted";
                            planttime = parser.CurrentTime.ToString();
                        };

                        parser.BombExploded += (sender, e) =>
                        {
                            status = "exploded";
                        };

                        parser.BombDefused += (sender, e) =>
                        {
                            status = "defused";
                        };

                        parser.TickDone += (sender, e) =>
                        {
                            
                            if (parser.CurrentTick % 2 == 0)
                            {
                                if (bombIsPlanted)
                                {
                                    var values = InitialValues(parser);
                                    values.Add("event", "bomb");
                                    values.Add("x", bombPosX);
                                    values.Add("y", bombPosY);
                                    values.Add("bomb_site", site);
                                    values.Add("bomb_status", status);
                                    values.Add("bomb_planttime", planttime);
                                    Console.WriteLine(RowString(values));
                                }
                                foreach (var player in parser.PlayingParticipants)
                                {
                                    var values = InitialValues(parser);
                                    values.Add("event", "status");
                                    values.Add("x", player.Position.X.ToString());
                                    values.Add("y", player.Position.Y.ToString());
                                    values.Add("view_x", player.ViewDirectionX.ToString());
                                    values.Add("view_y", player.ViewDirectionY.ToString());
                                    values.Add("player", player.Name);
                                    values.Add("team", GetTeam(parser, player));
                                    values.Add("side", player.Team.ToString());
                                    values.Add("kills", player.AdditionaInformations.Kills.ToString());
                                    values.Add("deaths", player.AdditionaInformations.Deaths.ToString());
                                    values.Add("money", player.Money.ToString());
                                    if (player.ActiveWeapon != null)
                                    {
                                        values.Add("weapon", player.ActiveWeapon.Weapon.ToString());
                                    }
                                    values.Add("alive", player.IsAlive.ToString());
                                    values.Add("freeze_equip_value", player.FreezetimeEndEquipmentValue.ToString());
                                    values.Add("current_equip_value", player.CurrentEquipmentValue.ToString());
                                    values.Add("last_alive_pos_x", player.LastAlivePosition.X.ToString());
                                    values.Add("last_alive_pos_y", player.LastAlivePosition.Y.ToString());

                                    Console.WriteLine(RowString(values));
                                }
                            }
                        };

                        parser.WeaponFired += (sender, e) =>
                        {
                            var values = InitialValues(parser);
                            values.Add("event", "fire");
                            values.Add("x", e.Shooter.Position.X.ToString());
                            values.Add("y", e.Shooter.Position.Y.ToString());
                            values.Add("view_x", e.Shooter.ViewDirectionX.ToString());
                            values.Add("view_y", e.Shooter.ViewDirectionY.ToString());
                            values.Add("player", e.Shooter.Name);
                            values.Add("team", GetTeam(parser, e.Shooter));
                            values.Add("side", e.Shooter.Team.ToString());
                            values.Add("weapon", e.Shooter.ActiveWeapon.Weapon.ToString());

                            Console.WriteLine(RowString(values));
                        };
                        parser.PlayerHurt += (sender, e) =>
                        {
                            var values = InitialValues(parser);
                            values.Add("event", "fire");
                            values.Add("x", e.Player.Position.X.ToString());
                            values.Add("y", e.Player.Position.Y.ToString());
                            values.Add("player", e.Player.Name);
                            values.Add("team", GetTeam(parser, e.Player));
                            values.Add("side", e.Player.Team.ToString());
                            values.Add("weapon", e.Weapon.Weapon.ToString());
                            if (e.Attacker != null) {
                                values.Add("attacker", e.Attacker.Name);
                                values.Add("attacker_pos_x", e.Attacker.Position.X.ToString());
                                values.Add("attacker_pos_y", e.Attacker.Position.Y.ToString());
                                values.Add("attacker_team", GetTeam(parser, e.Attacker));
                                values.Add("attacker_side", e.Attacker.Team.ToString());
                            }

                            Console.WriteLine(RowString(values));
                        };
                        parser.PlayerKilled += (sender, e) =>
                        {
                            var values = InitialValues(parser);
                            values.Add("event", "kill");
                            if (e.Victim != null)
                            {
                                values.Add("x", e.Victim.Position.X.ToString());
                                values.Add("y", e.Victim.Position.Y.ToString());
                                values.Add("player", e.Victim.Name);
                                values.Add("team", GetTeam(parser, e.Victim));
                                values.Add("side", e.Victim.Team.ToString());
                            }
                            values.Add("weapon", e.Weapon.Weapon.ToString());
                            if (e.Killer != null)
                            {
                                values.Add("attacker", e.Killer.Name);
                                values.Add("attacker_pos_x", e.Killer.Position.X.ToString());
                                values.Add("attacker_pos_y", e.Killer.Position.Y.ToString());
                                values.Add("attacker_team", GetTeam(parser, e.Killer));
                                values.Add("attacker_side", e.Killer.Team.ToString());
                            }

                            Console.WriteLine(RowString(values));
                        };
                        parser.ParseToEnd();
                    }
                }
            }
            timer.Stop();
            Console.Error.WriteLine(timer.ElapsedMilliseconds);
        }
    }
}
