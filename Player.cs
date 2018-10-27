using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication5
{
    public struct Game
    {
        public int week;
        public Team opponentTeam;
        public double points;

        public Game(int week, Team opponentTeam, double points)
        {
            this.week = week;
            this.opponentTeam = opponentTeam;
            this.points = points;
        }
    }

    public class Player
    {
        static public List<Player> players = new List<Player>();

        public string name { get; set; }
        public double rating { get; set; }
        public string owner { get; set; }
        public Position pos { get; set; }
        public double low { get; set; }
        public double high { get; set; }
               
        public List<Game> games = new List<Game>();
        public Dictionary<int, Team> nextOpps = new Dictionary<int, Team>();
        public Dictionary<int, double> nextWeeksProjection = new Dictionary<int, double>();
        public double rosRating { get; set; }
        public Team team { get; set; }

        public Player(string name, Position pos, string owner, Team team)
        {
            this.pos = pos;
            this.name = name;
            this.owner = owner;
            this.team = team;
        }

        public double CalcPlayerRating()
        {
            List<double> temp = new List<double>();
            foreach (var g in this.games)
            {
                if (g.opponentTeam != null)
                    temp.Add(g.points / g.opponentTeam.mean[this.pos]);
            }
            if (temp.Count > 3)
            {
                double std = Program.CalculateStdDev(temp);
                this.low = temp.Average() - std;
                this.high = temp.Average() + std;
                return Program.median(temp);
            }
            if (temp.Count == 3)
            {
                return (temp.Sum() - temp.Max()) / 2;
            }
            throw new Exception();
        }

        public void CalcPlayerValues(int lastWeek)
        {
            //fill in next opps
            if (this.nextOpps.Count == 0 && lastWeek < 17)
            {
                this.nextOpps = Player.players.Find(x => x.team == this.team && x.nextOpps.Count != 0).nextOpps;
            }

            if (this.games.Count > 2)
            {
                this.rating = CalcPlayerRating();
                if (this.nextOpps.Count > 0)
                {
                    bool first = true;
                    // calc ros sum of proj
                    foreach (var team in this.nextOpps)
                    {
                        if (first)
                        {
                            this.low *= team.Value.mean[this.pos];
                            this.high *= team.Value.mean[this.pos];
                            first = false;
                        }

                        if (team.Key < 17)
                        {
                            double weeklyProj = this.rating * team.Value.mean[this.pos];
                            this.rosRating += weeklyProj;
                            this.nextWeeksProjection.Add(team.Key, weeklyProj);
                        }
                    }
                }
            }
        }
    }
}