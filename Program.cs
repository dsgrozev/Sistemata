using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication5
{
    class Program
    {
        static ChromeDriver driver = null;
        static int lastWeek;

        static void Main(string[] args)
        {
            driver = StartDriver();

            // navigate to get teams average for each position
            string[] positions = Enum.GetNames(typeof(Position));
            
            CalculatePos(Position.QB.ToString());
                   
            CalcPlayerRatings(positions);
            
            driver.Quit();
            
            UpdatePlayers();

            SaveToExcel();
        }
        
        private static void CalcPlayerRatings(string pos)
        {
            CheckPlayerPosition("DEF", 16, false);
            lastWeek = CalcLastWeek();
            CheckPlayerPosition(pos, lastWeek);
        }

        private static void UpdatePlayers()
        {
            //Find average for teams / positiions
            CalcTeamAverage();
            foreach (Player p in Player.players)
            {
                p.CalcPlayerValues(lastWeek);
            }                    
        }

        private static void CalcTeamAverage()
        {
            foreach (var team in Team.teams)
            {
                team.CalcTeamValues();
            }
        }

        public static double CalculateStdDev(IEnumerable<double> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average      
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return ret;
        }

        public static double median(List<double> temp)
        {
            if (temp.Count == 0)
                return 0;
            temp.Sort();
            int size = temp.Count;
            int mid = size / 2;
            return (size % 2 != 0) ? temp[mid] : (temp[mid] + temp[mid - 1]) / 2;
        }

        private static void SaveToExcel(string pos = "")
        {
            Microsoft.Office.Interop.Excel._Application excel = new Microsoft.Office.Interop.Excel.Application();
            excel.Workbooks.Add();
            Microsoft.Office.Interop.Excel._Worksheet workSheet = excel.ActiveSheet;
            workSheet.Name = "Players";
            try
            {
                int col = 1;
                workSheet.Cells[1, col++] = "Name";
                workSheet.Cells[1, col++] = "Owner";
                workSheet.Cells[1, col++] = "Position";
                workSheet.Cells[1, col++] = "Rating";                
                workSheet.Cells[1, col++] = "Rest of season";
                workSheet.Cells[1, col++] = "Low";
                workSheet.Cells[1, col++] = "High";
               
                int minNextWeek = Player.players.Min(x => x.nextOpps.Keys.Min());

                for (int i = minNextWeek; i < 17; i++)
                {
                    workSheet.Cells[1, col++] = "Week " + i;
                }

                int row = 2; // start row (in row 1 are header cells)
                foreach (Player p in Player.players)
                {
                    col = 1;
                    workSheet.Cells[row, col++] = p.name;
                    workSheet.Cells[row, col++] = p.owner;
                    workSheet.Cells[row, col++] = p.pos.ToString();
                    workSheet.Cells[row, col++] = p.rating;
                    workSheet.Cells[row, col++] = p.rosRating;
                    workSheet.Cells[row, col++] = p.low;
                    workSheet.Cells[row, col++] = p.high;

                    for (int i = minNextWeek; i < 17; i++)
                    {
                        if (p.nextWeeksProjection.ContainsKey(i))
                        {
                            workSheet.Cells[row, col++] = p.nextWeeksProjection[i];
                        }
                        else
                        {
                            workSheet.Cells[row, col++] = "0";
                        }
                    }
                    row++;
                }

                excel.Worksheets.Add();
                workSheet = excel.ActiveSheet;
                workSheet.Name = "Raw";
                
                col = 1;
                workSheet.Cells[1, col++] = "Name";
                workSheet.Cells[1, col++] = "Week number";
                workSheet.Cells[1, col++] = "Opp";
                workSheet.Cells[1, col++] = "Points";

                row = 2;
                
                foreach (Player p in Player.players)
                {
                    foreach (Game g in p.games)
                    {
                        col = 1;
                        workSheet.Cells[row, col++] = p.name;
                        workSheet.Cells[row, col++] = g.week;
                        workSheet.Cells[row, col++] = g.opponentTeam.name;
                        workSheet.Cells[row, col++] = g.points;
                        row++;
                    }
                }

                excel.Worksheets.Add();
                workSheet = excel.ActiveSheet;
                workSheet.Name = "Teams";

                workSheet.Cells[1, "A"] = "Name";
                workSheet.Cells[1, "B"] = "QB";
                workSheet.Cells[1, "C"] = "RB";
                workSheet.Cells[1, "D"] = "WR";
                workSheet.Cells[1, "E"] = "TE";
                workSheet.Cells[1, "F"] = "K";
                workSheet.Cells[1, "G"] = "DEF";

                row = 2; // start row (in row 1 are header cells)
                foreach (Team t in Team.teams)
                {
                    workSheet.Cells[row, "A"] = t.name;
                    workSheet.Cells[row, "B"] = t.mean[Position.QB];
                    workSheet.Cells[row, "C"] = t.mean[Position.RB];
                    workSheet.Cells[row, "D"] = t.mean[Position.WR];
                    workSheet.Cells[row, "E"] = t.mean[Position.TE];
                    workSheet.Cells[row, "F"] = t.mean[Position.K];
                    workSheet.Cells[row, "G"] = t.mean[Position.DEF];
                    row++;
                }

                // Save this data as a file
                
                workSheet.SaveAs(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Fantasy\FantasyWeek" + DateTime.Now.ToShortDateString().Replace('/', '#') + pos +  "_med.xlsx");
            }
            catch (Exception)
            {
            }
            finally
            {
                // Quit Excel application
                excel.Quit();

                // Release COM objects (very important!)
                if (excel != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excel);

                if (workSheet != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workSheet);

                // Empty variables
                excel = null;
                workSheet = null;

                // Force garbage collector cleaning
                GC.Collect();
            }
        }

        static private void CalcPlayerRatings(string[] positions)
        {
            CheckPlayerPosition("DEF", 17, false);
            lastWeek = CalcLastWeek();
            foreach (string p in positions)
            {
                if (p != "DEF")
                {
                    CheckPlayerPosition(p, lastWeek);
                }
            }
        }

        private static int CalcLastWeek()
        {
            int nextWeek = 0;
            foreach (var team in Team.teams){
                nextWeek = Math.Max(nextWeek, team.nextWeekNumber);
            }
            if (nextWeek == 0)
                return 17;
            return nextWeek - 1;
        }

        private static void CheckPlayerPosition(string position, int lastWeek = 16, bool onlyPoints = true)
        {
            for (int i = 1; i < lastWeek + 1; i++)
            {
                Console.Write(position + " : " + i.ToString() + ": ");   
                int rowCount = 0;
                int count = 0;
                bool reachedEndOfWeek = false;
                do
                {
                    IWebElement table = null;
                    driver.Navigate().GoToUrl(@"https://football.fantasysports.yahoo.com/f1/849675/players?status=ALL&pos=" +
                        position + "&cut_type=9&stat1=S_W_" + i + "&sort=0&sdir=1&count=" + count);
                    while (true)
                    {
                        try
                        {
                            table = new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(
                                ExpectedConditions.ElementIsVisible(By.Id("players-table-wrapper")));
                            break;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                    var rows = table.FindElements(By.TagName("tr"));
                    rowCount = rows.Count;
                   
                    for (int j = 2; j < rowCount; j++)
                    {
                        reachedEndOfWeek = HandlePlayerRow(rows[j], i, position);
                    }
                    
                    count += 25;
                    
                    if (reachedEndOfWeek && onlyPoints)
                    {
                        break;
                    }
                    Console.Write(".");
                } while (rowCount == 27);
                Console.WriteLine();
            }
        }

        private static bool HandlePlayerRow(IWebElement row, int week, string position)
        {
            string rowText = row.Text;
            var fields = rowText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            var player = fields[1];

            int i = 3;
            while (fields[i].Length != 1)
                i++;

            var oppString = fields[i - 1];
            string oppTeam = "";

            if (oppString == "Bye")
            {
                oppTeam = "Bye";
            }
            else if (oppString.Contains("vs"))
            {
                oppTeam = oppString.Split(new string[]{"vs"}, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
            }
            else
            {
                oppTeam = oppString.Split('@')[1].Trim();
            }

            if (!Char.IsLetter(oppTeam[oppTeam.Length - 1]))
                oppTeam = oppTeam.Substring(0, oppTeam.Length - 1);

            if (fields[i + 1].StartsWith("Video"))
                i++;
            
            var owner = fields[i + 1];
            var gamesPlayed = fields[i + 2];
            var points = fields[i + 4];

            double realPoints = 0;

            if (gamesPlayed == "1" && oppTeam != "Bye")
            {
                realPoints = Double.Parse(points);
            }

            var nameParts = player.Split(' ');
            string shortTeamName = nameParts[nameParts.Length - 3];

            Team team = Team.teams.Find(x => x.shortName == shortTeamName);

            // this will work as we start with DEF and they always play
            if (team.nextWeekNumber < 0 && gamesPlayed != "1" && oppTeam != "Bye")
            {
                team.nextWeekNumber = week;
            }

            Player p = Player.players.Find(x => x.name == player);
            if (p == null)
            {
                p = new Player(player, (Position)Enum.Parse(typeof(Position), position), owner, team);
                Player.players.Add(p);
            }

            if (gamesPlayed == "1" && oppTeam != "Bye")
            {
                if ((p.games.FindAll(x => x.week == week).Count == 0))
                {
                    var opp = Team.teams.Find(x => x.shortName == oppTeam);
                    p.games.Add(new Game(week, opp, realPoints));
                    opp.scores.Add(new Tuple<Position, int, double>(p.pos, week, realPoints));
                }
            }
            else if (oppTeam != "Bye" && position == "DEF")
            {
                p.nextOpps.Add(week, Team.teams.Find(x => x.shortName == oppTeam));
            }

            return gamesPlayed != "1";
        }

        static private void CalculatePos(string pos)
        {
            driver.Navigate().GoToUrl(@"https://football.fantasysports.yahoo.com/f1/849675/pointsagainst?season=2018&pos=" + pos + "&mode=average");

            IWebElement table = null;
            while (true)
            {
                try
                {
                    table = new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(ExpectedConditions.ElementIsVisible(By.Id("statTable0")));
                    break;
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }

            var rows = table.FindElements(By.TagName("tr"));
            for (int i = 2; i < rows.Count; i++)
            {
                HandleRow(rows[i].Text, pos);
            }
        }

        static private void HandleRow(string text, string pos)
        {
            var fields = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            double avg = Convert.ToDouble(fields[fields.Count() - 1]);
            var team = fields[1].Substring(0, fields[1].IndexOf("vs") - 1);
            Team t = Team.teams.Find(x => x.name == team);
            if (t == null)
            {
                t = new Team(team);
                Team.teams.Add(t);
            }
            t.mean.Add((Position)(Enum.Parse(typeof(Position), pos)), avg);
        }

        static private ChromeDriver StartDriver()
        {
            ChromeOptions opt = new ChromeOptions();
            opt.Proxy = null;
            
            PhantomJSDriverService service = PhantomJSDriverService.CreateDefaultService();
            service.IgnoreSslErrors = true;
            service.LoadImages = true;
            service.ProxyType = "none";

            //var driver = new PhantomJSDriver(service);

            ChromeDriver driver = new ChromeDriver(@"C:\Users\Dimitar\Downloads\selenium-dotnet-3.5.1\", opt);
            //driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 10));
            
            driver.Url = "https://login.yahoo.com";
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            var elem = wait.Until(ExpectedConditions.ElementToBeClickable((By.CssSelector("input.phone-no#login-username")))); ;
            elem.SendKeys("dsgrozev@hotmail.com");
            driver.FindElementById("login-signin").Click();
            //driver.FindElementById("login-passwd").SendKeys("yahuutu1");
            //driver.FindElementById("login-signin").Click();
            return driver;
        }
    }
}
