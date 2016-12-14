using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Collections;

namespace SmiteStats
{
    class Program
    {
        static void Main(string[] args)
        {
            int match1Id = 300850468;
            int match2Id = 300656142;

            MatchDetails match1 = new MatchDetails(match1Id);
            MatchDetails match2 = new MatchDetails(match2Id);

            System.IO.File.WriteAllLines(@"..\..\html.txt", match1.trimmedHtml);
        }
    }

    class MatchDetails
    {
        private const string trimStart = "<div class=\"match-stats\">";
        private const string trimEnd = "  <footer class=\"hirez-footer\" role=\"contentinfo\">";

        private int matchId;
        private string statsUrl;
        public List<string> trimmedHtml;
        private MatchStats stats;

        private static List<string> GetHtml(string url)
        {
            WebClient client = new WebClient();
            string rawHtml = client.DownloadString(url);

            string[] htmlLines = rawHtml.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<string> trimmedHtml = new List<string>();
            bool trimming = false;
            foreach (string line in htmlLines)
            {
                if (trimming)
                {
                    if (line == trimEnd)
                    {
                        trimming = false;
                        break;
                    }
                    trimmedHtml.Add(line);
                    continue;
                }
                if (line == trimStart)
                {
                    trimming = true;
                    trimmedHtml.Add(line);
                }
            }
            return trimmedHtml;
        }

        public MatchDetails(int matchId)
        {
            this.matchId = matchId;
            statsUrl = "https://www.smitegame.com/match-details?match=" + matchId.ToString();
            trimmedHtml = GetHtml(statsUrl);
            stats = new MatchStats();
            ReadHtml(trimmedHtml);
        }


        private void ReadHtml(List<string> trimmedHtml)
        {

            bool winner = true;
            //string[] substrings = { "Time:","Duration:","Kills:","Assists:","Gold:", "Kills:", "Assists:", "Gold:","placeholder" };
            string characterMarker = "<tr>"; //occurs twolines before the character's god (in the alt text)
            string pictureMarker = "alt="; //identifier for gods and items
            string numberMarker = "<td><p>"; //occurs before every statline

            //foreach (string line in trimmedHtml)
            //if going back to the foreach loop (unlikely) replace trimmedHtml[lineNumber] with line
            for (int lineNumber = 0; lineNumber < trimmedHtml.Count; lineNumber++){
                if (trimmedHtml[lineNumber].Contains("Time:"))
                {
                    ProcessTimeAndDuration(stats, trimmedHtml, lineNumber);
                }
                else if (trimmedHtml[lineNumber].Contains("Kills:"))
                {
                    if (winner)
                    {
                        ProcessTeamStats(stats.winner, trimmedHtml, lineNumber);
                        winner = false;
                    }
                    else
                    {
                        ProcessTeamStats(stats.loser, trimmedHtml, lineNumber);
                    }
                }
                else if (trimmedHtml[lineNumber].Contains(characterMarker))
                {

                }
                //if (trimmedHtml[lineNumber].Contains(substrings[i]))
                //{
                //    string numberString = new string(trimmedHtml[lineNumber].Where(c => Char.IsDigit(c)).ToArray());
                //    int numberValue = 0;
                //    if (i != 0) { numberValue = Int32.Parse(numberString); }
                //    switch (i)
                //    {
                //        case 0:
                //            string[] dateline = trimmedHtml[lineNumber].Split(new Char[] { ':','/', ' ' ,'M' });
                //            int monthIdx = 2;
                //            int dayIdx = 3;
                //            int yearIdx = 4;
                //            int hourIdx = 5;
                //            int minIdx = 6;
                //            int secIdx = 7;
                //            int pmIdx = 8;
                //            stats.matchTime = new DateTime(Int32.Parse(dateline[yearIdx]), Int32.Parse(dateline[monthIdx]), Int32.Parse(dateline[dayIdx]),
                //                Int32.Parse(dateline[hourIdx]) + (dateline[pmIdx] == "P" ? 12 : 0), Int32.Parse(dateline[minIdx]), Int32.Parse(dateline[secIdx]));
                //            break;
                //        case 1:
                //            stats.duration = numberValue;
                //            break;
                //        case 2:
                //            stats.winner.kills = numberValue;
                //            break;
                //        case 3:
                //            stats.winner.assists = numberValue;
                //            break;
                //        case 4:
                //            stats.winner.gold = numberValue;
                //            break;
                //        case 5:
                //            stats.loser.kills = numberValue;
                //            break;
                //        case 6:
                //            stats.loser.assists = numberValue;
                //            break;
                //        case 7:
                //            stats.loser.gold = numberValue;
                //            break;
                //        default:
                //            break;
                //    }
                //    i++;               
                //}
            }
        }

        private static int GetNumbersFromLine(string line)
        {
            string numbers = new string(line.Where(c => Char.IsDigit(c)).ToArray());
            return Int32.Parse(numbers);
        }

        private void ProcessTeamStats(Team team, List<string> html, int lineNumber)
        {
            int killsSeparation = 0;
            int assistsSeparation = 1;
            int goldSeparation = 2;
            team.kills = GetNumbersFromLine(html[lineNumber + killsSeparation]);
            team.assists = GetNumbersFromLine(html[lineNumber + assistsSeparation]);
            team.gold = GetNumbersFromLine(html[lineNumber + goldSeparation]);
        }

        private void ProcessTimeAndDuration(MatchStats match, List<string> html, int lineNumber)
        {
            int DurationSeparation = 3;
            int monthIdx = 2;
            int dayIdx = 3;
            int yearIdx = 4;
            int hourIdx = 5;
            int minIdx = 6;
            int secIdx = 7;
            int pmIdx = 8;
            string[] dateline = html[lineNumber].Split(new Char[] { ':', '/', ' ', 'M' });
            match.matchTime = new DateTime(Int32.Parse(dateline[yearIdx]), Int32.Parse(dateline[monthIdx]), Int32.Parse(dateline[dayIdx]),
                Int32.Parse(dateline[hourIdx]) + (dateline[pmIdx] == "P" ? 12 : 0), Int32.Parse(dateline[minIdx]), Int32.Parse(dateline[secIdx]));
            match.duration = GetNumbersFromLine(html[lineNumber + DurationSeparation]);
        }

        class MatchStats
        {
            public Team winner;
            public Team loser;
            public DateTime matchTime;
            public int duration;

            public MatchStats()
            {
                winner = new Team();
                loser = new Team();
            }
        }

        class Team
        {
            public Player[] member;
            public int kills;
            public int assists;
            public int gold;

            public Team()
            {

            }


        }

        class Player
        {
            public string name, clan;
            public string god;
            public int level, kills, deaths, assists, gold, bestKillSpree, bestMultiKill, playerDamage, towerDamage, healing, minionDamage, damageTaken;
            public Item[] relic = new Item[2];
            public Item[] build = new Item[6];

            public Player()
            {

            }


        }

        class Item
        {
            public string name;

            public Item(string name)
            {
                this.name = name;
            }
        }
    }
}