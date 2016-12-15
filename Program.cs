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
            int match3Id = 300850467;

            int onePlayer = 300850465;
            int nameless = 300850464;

            MatchDetails match1 = new MatchDetails(match1Id);
            MatchDetails match2 = new MatchDetails(match2Id);
            MatchDetails match3 = new MatchDetails(match3Id);

            MatchDetails matchOnePlayer = new MatchDetails(onePlayer);
            MatchDetails namelessGame = new MatchDetails(nameless);

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
            bool editingWinner = true;
            bool skippingLines = false;
            string characterMarker = "<tr>"; //occurs twolines before the character's god (in the alt text)

            string victoryBuildStatsMarker = "<table id=\"victory-builds-stats\"";
            string defeatDetailsMarker = "<h4>Defeat</h4>";
            string defeatBuildStatsMarker = "<table id=\"defeat-builds-stats\"";

            for (int lineNumber = 0; lineNumber < trimmedHtml.Count; lineNumber++){
                //ignoring item description stuff, probably easier to do from a database given my intent is only to analyze per patch
                if (trimmedHtml[lineNumber].Contains(victoryBuildStatsMarker)) {skippingLines = true;}
                else if (trimmedHtml[lineNumber].Contains(defeatDetailsMarker)) { skippingLines = false;}
                else if (trimmedHtml[lineNumber].Contains(defeatBuildStatsMarker)) { skippingLines = true;}
                if (skippingLines){continue;}

                if (trimmedHtml[lineNumber].Contains("Time:"))
                {
                    ProcessTimeAndDuration(stats, trimmedHtml, lineNumber);
                }
                else if (trimmedHtml[lineNumber].Contains("Kills:"))
                {
                    if (stats.winner.gold == 0)
                    {
                        ProcessTeamStats(stats.winner, trimmedHtml, lineNumber);
                    }
                    else
                    {
                        editingWinner = false;
                        ProcessTeamStats(stats.loser, trimmedHtml, lineNumber);
                    }
                }
                else if (trimmedHtml[lineNumber].Contains(characterMarker))
                {
                    if (editingWinner)
                    {
                        Player newmember = new Player();
                        stats.winner.member.Add(newmember);
                        ProcessTeamMember(stats.winner.member.Last(), trimmedHtml, lineNumber);
                    }
                    else
                    {
                        Player newmember = new Player();
                        stats.loser.member.Add(newmember);
                        ProcessTeamMember(stats.loser.member.Last(), trimmedHtml, lineNumber);
                    }
                }
            }
        }

        private static int GetNumbersFromLine(string line)
        {
            string numbers = new string(line.Where(c => Char.IsDigit(c)).ToArray());
            return Int32.Parse(numbers);
        }

        private static string GetPictureName(string line)
        {
            string[] pictureLine = line.Split(new string[] { "alt=\"", "\" />", "\"/>" }, StringSplitOptions.None);
            return pictureLine[1];
        }

        private static void GetPlayerName(Player player, string line)
        {
            string[] playerLine = line.Split(new string[] { "[", "]", "<p>", "</p>" }, StringSplitOptions.None);
            int clanLength = 5;
            if (playerLine.Length == clanLength)
            {
                player.name = playerLine[3];
                player.clan = playerLine[2];
            }
            else
            {
                player.name = playerLine[2];
                player.clan = null;
            }
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
            int durationSeparation = 3;
            int monthIdx = 2;
            int dayIdx = 3;
            int yearIdx = 4;
            int hourIdx = 5;
            int minIdx = 6;
            int secIdx = 7;
            int pmIdx = 8;
            string[] dateLine = html[lineNumber].Split(new Char[] { ':', '/', ' ', 'M' });
            match.matchTime = new DateTime(Int32.Parse(dateLine[yearIdx]), Int32.Parse(dateLine[monthIdx]), Int32.Parse(dateLine[dayIdx]),
                Int32.Parse(dateLine[hourIdx]) + (dateLine[pmIdx] == "P" ? 12 : 0), Int32.Parse(dateLine[minIdx]), Int32.Parse(dateLine[secIdx]));
            match.duration = GetNumbersFromLine(html[lineNumber + durationSeparation]);
        }

        private void ProcessTeamMember(Player member,List<string> html, int lineNumber)
        {
            int godOffset = 2;
            int nameOffset = 4;
            int levelOffset = 6;
            int kdaOffset = 7;
            int spreeOffset = 8;
            int multiOffset = 9;
            int goldOffset = 10;
            int gpmOffset = 11;
            int damageOffset = 12;
            int towerOffset = 13;
            int healOffset = 14;
            int minionOffset = 15;
            int takenOffset = 16;
            member.god = GetPictureName(html[lineNumber + godOffset]);
            GetPlayerName(member, html[lineNumber + nameOffset]);
            member.level = GetNumbersFromLine(html[lineNumber + levelOffset]);
            ProcessKDA(member, html[lineNumber + kdaOffset]);
            member.bestKillSpree = GetNumbersFromLine(html[lineNumber + spreeOffset]);
            member.bestMultiKill = GetNumbersFromLine(html[lineNumber + multiOffset]);
            member.gold = GetNumbersFromLine(html[lineNumber + goldOffset]);
            member.goldPerMinute = GetNumbersFromLine(html[lineNumber + gpmOffset]);
            member.playerDamage = GetNumbersFromLine(html[lineNumber + damageOffset]);
            member.towerDamage = GetNumbersFromLine(html[lineNumber + towerOffset]);
            member.healing = GetNumbersFromLine(html[lineNumber + healOffset]);
            member.minionDamage = GetNumbersFromLine(html[lineNumber + minionOffset]);
            member.damageTaken = GetNumbersFromLine(html[lineNumber + takenOffset]);

            lineNumber = lineNumber + 17;

            string characterEndMarker = "</tr>";
            string itemMarker = "alt=";
            int itemNumber = 0;
            for (string line = html[lineNumber]; !line.Contains(characterEndMarker);line = html[lineNumber++])
            {
                if (line.Contains(itemMarker))
                {
                    if (itemNumber < 2)
                    {
                        member.relic[itemNumber] = new Item(GetPictureName(line));
                        itemNumber++;
                    }
                    else
                    {
                        member.build[itemNumber - 2] = new Item(GetPictureName(line));
                        itemNumber++;
                    }
                }
            }
        }

        private void ProcessKDA(Player player,string line)
        {
            string[] kdaString = line.Split(new string[] { "<td><p>", "/", "</p></td>"},StringSplitOptions.None);
            player.kills = Int32.Parse(kdaString[1]);
            player.deaths = Int32.Parse(kdaString[2]);
            player.assists = Int32.Parse(kdaString[3]);
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
            public List<Player> member;
            public int kills;
            public int assists;
            public int gold;

            public Team()
            {
                this.member = new List<Player>();
            }
        }

        class Player
        {
            public string name, clan;
            public string god;
            public int level, kills, deaths, assists, gold, goldPerMinute, bestKillSpree, bestMultiKill, playerDamage, towerDamage, healing, minionDamage, damageTaken;
            public Item[] relic;
            public Item[] build;

            public Player()
            {
                relic = new Item[2];
                build = new Item[6];
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