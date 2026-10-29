using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// مدير لوائح المتصدرين والترتيب التنافسي (Leaderboard Manager)
/// يدير لوائح الشرف الأربعة: اليومي (Daily)، الأسبوعي (Weekly)، الشهري (Monthly)، والعام (All-Time).
/// يدمج رصيد وترتيب اللاعب المحلي تلقائياً بين كبار أبطال المجتمع مع دعم إضافة الأصدقاء مباشرة.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    public enum Timeframe
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        AllTime = 3
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string playerName;
        public int score;
        public int tierIndex;
        public string favoriteHero;
        public int matchesWon;
        public bool isLocalPlayer;
    }

    [Serializable]
    private class SeededChampion
    {
        public string name;
        public string hero;
        public int allTimeScore;
        public int monthlyScore;
        public int weeklyScore;
        public int dailyScore;
        public int wins;
    }

    private readonly List<SeededChampion> communityChampions = new List<SeededChampion>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("LeaderboardManager");
            obj.AddComponent<LeaderboardManager>();
            DontDestroyOnLoad(obj);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCommunityChampions();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void InitializeCommunityChampions()
    {
        // مجتمع أبطال متنوع ومحترم يعكس أجواء التنافس الحقيقي في حماية شجرة الفاصولياء
        var seeds = new (string name, string hero, int allTime, int monthly, int weekly, int daily, int wins)[]
        {
            ("Sultan_Titan", "Knight", 25800, 7850, 2420, 580, 482),
            ("Zephyr_Blade", "Ranger", 24350, 7420, 2280, 540, 440),
            ("عاصفة_الشمال", "Druid", 23100, 6980, 2150, 510, 415),
            ("ShadowPhoenix", "Mage", 21850, 6650, 2010, 490, 395),
            ("Elora_Starcaller", "Druid", 20400, 6200, 1940, 470, 370),
            ("صقر_قريش", "Knight", 19200, 5890, 1820, 440, 348),
            ("Valeria_Storm", "Mage", 18150, 5540, 1710, 420, 326),
            ("IronClaw_Zak", "Paladin", 17300, 5280, 1630, 395, 310),
            ("نجم_الليل", "Ranger", 16400, 4980, 1540, 380, 296),
            ("Aura_Knight", "Knight", 15650, 4750, 1480, 360, 282),
            ("فارس_الأمل", "Paladin", 14800, 4520, 1400, 345, 268),
            ("Sylvia_Breeze", "Ranger", 13950, 4260, 1320, 330, 250),
            ("Garrick_Rock", "Knight", 13100, 4010, 1250, 310, 236),
            ("سيف_العدالة", "Knight", 12350, 3800, 1180, 295, 222),
            ("Rowan_TheBrave", "Paladin", 11600, 3580, 1110, 280, 210),
            ("Luna_Whisper", "Druid", 10900, 3370, 1040, 265, 198),
            ("درع_الصحراء", "Paladin", 10250, 3180, 980, 250, 185),
            ("Kaelen_Frost", "Mage", 9600, 2990, 930, 235, 174),
            ("Thorin_Oak", "Knight", 8950, 2800, 870, 220, 162),
            ("أسد_الغابة", "Druid", 8400, 2620, 810, 205, 150),
            ("Lyra_Valkyrie", "Paladin", 7800, 2450, 760, 190, 140),
            ("برق_السماء", "Mage", 7250, 2280, 710, 180, 130),
            ("Gideon_Shield", "Paladin", 6700, 2120, 660, 168, 122),
            ("سهم_الغروب", "Ranger", 6200, 1960, 610, 155, 112),
            ("Freya_Bloom", "Druid", 5700, 1810, 560, 142, 102),
            ("قاهر_الظلام", "Knight", 5250, 1670, 520, 130, 94),
            ("Alden_Swift", "Ranger", 4800, 1530, 475, 120, 86),
            ("رياح_الشرق", "Ranger", 4350, 1390, 430, 110, 78),
            ("Darius_Flame", "Mage", 3950, 1260, 390, 98, 70),
            ("حارس_الأشجار", "Druid", 3550, 1140, 350, 88, 63),
            ("Elysia_Green", "Druid", 3200, 1020, 315, 80, 57),
            ("فارس_الصمود", "Paladin", 2850, 910, 280, 72, 51),
            ("Finnick_Arrow", "Ranger", 2500, 800, 250, 64, 45),
            ("وميض_الأمل", "Mage", 2200, 700, 220, 56, 39),
            ("Corin_Stone", "Knight", 1900, 610, 190, 48, 34),
            ("نبض_الحياة", "Druid", 1600, 520, 160, 40, 29),
            ("Bran_Wanderer", "Ranger", 1350, 430, 135, 34, 24),
            ("سيد_الرياح", "Mage", 1100, 350, 110, 28, 20),
            ("Cedric_Light", "Paladin", 900, 280, 90, 22, 16),
            ("جندي_المستقبل", "Knight", 700, 220, 70, 18, 12),
            ("Mira_Glow", "Druid", 500, 160, 50, 12, 9),
            ("مستكشف_القمم", "Ranger", 350, 110, 35, 8, 6),
            ("Torin_Novice", "Knight", 200, 65, 20, 5, 3)
        };

        communityChampions.Clear();
        foreach (var s in seeds)
        {
            communityChampions.Add(new SeededChampion
            {
                name = s.name,
                hero = s.hero,
                allTimeScore = s.allTime,
                monthlyScore = s.monthly,
                weeklyScore = s.weekly,
                dailyScore = s.daily,
                wins = s.wins
            });
        }
    }

    /// <summary>
    /// يجلب قائمة المتصدرين لفترة معينة (Daily, Weekly, Monthly, AllTime) مع حقن اللاعب المحلي وترتيبه الفعلي بدقة
    /// </summary>
    public List<LeaderboardEntry> GetLeaderboard(Timeframe timeframe, out LeaderboardEntry localPlayerEntry)
    {
        var list = new List<LeaderboardEntry>();

        // جلب بيانات اللاعب المحلي الحالية
        int myScore = 0;
        string myName = "Hero";
        if (PlayerProfile.Instance != null)
        {
            myName = PlayerProfile.Instance.PlayerName;
            switch (timeframe)
            {
                case Timeframe.Daily:
                    myScore = PlayerProfile.Instance.RatingScore_Daily;
                    break;
                case Timeframe.Weekly:
                    myScore = PlayerProfile.Instance.RatingScore_Weekly;
                    break;
                case Timeframe.Monthly:
                    myScore = PlayerProfile.Instance.RatingScore_Monthly;
                    break;
                case Timeframe.AllTime:
                default:
                    myScore = PlayerProfile.Instance.RatingScore_AllTime;
                    break;
            }
        }

        var localTier = PlayerProfile.GetRankTier(myScore);
        int myAccountLvl = PlayerProfile.Instance != null ? PlayerProfile.Instance.AccountLevel : 1;

        localPlayerEntry = new LeaderboardEntry
        {
            playerName = myName,
            score = myScore,
            tierIndex = localTier.tierIndex,
            favoriteHero = "Knight",
            matchesWon = Mathf.Max(1, myAccountLvl * 3 + (myScore / 250)),
            isLocalPlayer = true
        };

        // إضافة أبطال المجتمع
        foreach (var c in communityChampions)
        {
            int score = 0;
            switch (timeframe)
            {
                case Timeframe.Daily:
                    score = c.dailyScore;
                    break;
                case Timeframe.Weekly:
                    score = c.weeklyScore;
                    break;
                case Timeframe.Monthly:
                    score = c.monthlyScore;
                    break;
                case Timeframe.AllTime:
                default:
                    score = c.allTimeScore;
                    break;
            }

            var champTier = PlayerProfile.GetRankTier(score);
            list.Add(new LeaderboardEntry
            {
                playerName = c.name,
                score = score,
                tierIndex = champTier.tierIndex,
                favoriteHero = c.hero,
                matchesWon = c.wins,
                isLocalPlayer = false
            });
        }

        // إضافة اللاعب المحلي للقائمة للترتيب
        list.Add(localPlayerEntry);

        // الترتيب التنازلي حسب النقاط
        list.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);
            if (cmp != 0) return cmp;
            // إذا تساوت النقاط، نفضل اللاعب المحلي لتشجيعه
            if (a.isLocalPlayer) return -1;
            if (b.isLocalPlayer) return 1;
            return a.playerName.CompareTo(b.playerName);
        });

        // ترقيم المراكز
        for (int i = 0; i < list.Count; i++)
        {
            list[i].rank = i + 1;
            if (list[i].isLocalPlayer)
            {
                localPlayerEntry.rank = i + 1;
            }
        }

        return list;
    }
}
