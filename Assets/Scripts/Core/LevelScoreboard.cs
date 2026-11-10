using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public static class LevelScoreboard
    {
        [Serializable]



        public class Run
        {
            public int milliseconds;
            public int  deaths;
            public int coins;
            public int coinTotal;




            public string TimeLabel()
            {
                int minutes = milliseconds / 60000;
                int seconds = milliseconds / 1000 % 60;
                int leftover = milliseconds % 1000;

                return $"{minutes:00}:{seconds:00}.{leftover:000}";
            }

            public string CoinLabel()
            {
                if (coinTotal == 0) return "—";
                return coins + "/" + coinTotal;
            }
        }

        [Serializable]
        class History
        {
            public List<Run> runs = new List<Run>();
        }

        public class Result
        {
            public Run current;
            public int rank;
            public int totalRuns;
            public List<Run> bestRuns;
        }

        public static Result Record(string scenePath, float seconds, int deaths, int coins, int coinTotal)
        {
            string key = "Lightfooted.Scoreboard.v1:" + scenePath;
            History history;

            if (PlayerPrefs.HasKey(key))



                history = JsonUtility.FromJson<History>(PlayerPrefs.GetString(key));
            else
                history = new History();

            Run run = new Run();
            run.milliseconds = Mathf.RoundToInt(seconds * 1000f);
            run.deaths = deaths;
             run.coins = coins;
            run.coinTotal = coinTotal;

            int index = 0;
            while (index < history.runs.Count && history.runs[index].milliseconds <= run.milliseconds)
                index++;

                history.runs.Insert(index, run);

            PlayerPrefs.SetString(key, JsonUtility.ToJson(history));
            PlayerPrefs.Save();

            Result result = new Result();
            result.current = run;
             result.rank = index + 1;
            result.totalRuns = history.runs.Count;
            result.bestRuns = history.runs.GetRange(0, Mathf.Min(5, history.runs.Count));
            return result;
        }
    }
}