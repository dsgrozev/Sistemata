using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication5
{
    public enum Position
    {
        DEF,
        QB,
        WR,
        RB,
        TE,
        K
    }

    public class Team
    {
        public static Dictionary<string, string> teamNames = new Dictionary<string, string>(){
            {"Arizona Cardinals", "Ari"},
            {"Atlanta Falcons", "Atl"},
            {"Baltimore Ravens", "Bal"},
            {"Buffalo Bills", "Buf"},
            {"Carolina Panthers", "Car"},
            {"Chicago Bears", "Chi"},
            {"Cincinnati Bengals", "Cin"},
            {"Cleveland Browns", "Cle"},
            {"Dallas Cowboys", "Dal"},
            {"Denver Broncos", "Den"},
            {"Detroit Lions", "Det"},
            {"Green Bay Packers", "GB"},
            {"Houston Texans", "Hou"},
            {"Indianapolis Colts", "Ind"},
            {"Jacksonville Jaguars", "Jax"},
            {"Kansas City Chiefs", "KC"},
            {"Los Angeles Rams", "LAR"},
            {"Los Angeles Chargers", "LAC"},
            {"Miami Dolphins", "Mia"},
            {"Minnesota Vikings", "Min"},
            {"New England Patriots", "NE"},
            {"New Orleans Saints", "NO"},
            {"New York Giants", "NYG"},
            {"New York Jets", "NYJ"},
            {"Oakland Raiders", "Oak"},
            {"Philadelphia Eagles", "Phi"},
            {"Pittsburgh Steelers", "Pit"},
            {"San Francisco 49ers", "SF"},
            {"Seattle Seahawks", "Sea"},
            {"Tampa Bay Buccaneers", "TB"},
            {"Tennessee Titans", "Ten"},
            {"Washington Redskins", "Was"}
        };

        static public List<Team> teams = new List<Team>();
        public string name { get; private set; }
        public string shortName;
        public int nextWeekNumber = -1;

        public Dictionary<Position, double> mean = new Dictionary<Position, double>();
        public Dictionary<Position, double> stdDev = new Dictionary<Position, double>();
        public List<Tuple<Position, int, double>> scores = new List<Tuple<Position, int, double>>();

        public Team(string name)
        {
            this.name = name;
            this.shortName = teamNames[name]; 
        }

        public void CalcTeamValues()
        {
            foreach (var pos in Enum.GetNames(typeof(Position)))
            {
                Position posit = (Position)Enum.Parse(typeof(Position), pos);
                var allScores = this.scores.FindAll(x => x.Item1 == posit);
                var weekScores =
                    from s in allScores
                    group s by s.Item2 into g
                    select new { week = g.Key, score = g.Sum(s => s.Item3) };
                List<double> scores = weekScores.Select(x => x.score).Cast<double>().ToList();
                this.stdDev[posit] = Program.CalculateStdDev(scores);
                this.mean[posit] = Program.median(scores);
            }
        }
    }
}
