using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// نظام الملف الشخصي والاقتصاد والتقدم للاعب
/// يحفظ الأموال (Coins)، الجواهر (Gems)، الخبرة الحسابية (Account Level)،
/// السكنات المفتوحة، والإعلانات المشاهدة، والمشتريات
/// </summary>
public class PlayerProfile : MonoBehaviour
{
    public static PlayerProfile Instance { get; private set; }

    // =========================
    // أحداث لتحديث الواجهة تلقائياً
    // =========================
    public event Action OnProfileUpdated;
    public event Action<string> OnRewardNotification;

    // المفاتيح للحفظ
    private const string PREF_COINS = "BW_Coins";
    private const string PREF_GEMS = "BW_Gems";
    private const string PREF_ACC_LEVEL = "BW_AccountLevel";
    private const string PREF_ACC_XP = "BW_AccountXP";
    private const string PREF_ADS_WATCHED = "BW_AdsWatched";
    private const string PREF_PURCHASES = "BW_PurchasesCount";
    private const string PREF_BOSSES_KILLED = "BW_BossesKilled";
    private const string PREF_PLAYER_NAME = "BW_PlayerName";

    private const string PREF_RATING_ALLTIME = "BW_Rating_AllTime";
    private const string PREF_RATING_MONTHLY = "BW_Rating_Monthly";
    private const string PREF_RATING_WEEKLY = "BW_Rating_Weekly";
    private const string PREF_RATING_DAILY = "BW_Rating_Daily";
    private const string PREF_LAST_DAILY_DATE = "BW_LastDailyDate";
    private const string PREF_LAST_WEEKLY_DATE = "BW_LastWeeklyDate";
    private const string PREF_LAST_MONTHLY_DATE = "BW_LastMonthlyDate";

    // البيانات المحفوظة
    public string PlayerName { get; private set; } = "Ghaith";
    public int Coins { get; private set; }
    public int Gems { get; private set; }
    public int AccountLevel { get; private set; }
    public int AccountXP { get; private set; }
    public int AdsWatched { get; private set; }
    public int PurchasesMade { get; private set; }
    public int BossesKilled { get; private set; }

    public int RatingScore_AllTime { get; private set; }
    public int RatingScore_Monthly { get; private set; }
    public int RatingScore_Weekly { get; private set; }
    public int RatingScore_Daily { get; private set; }

    // ==========================================
    // نظام الـ 10 رتب التنافسية (10 Ranked Tiers)
    // ==========================================
    public struct RankTierInfo
    {
        public int tierIndex; // 1 to 10
        public string titleEn;
        public string titleAr;
        public string badgeIcon;
        public int minScore;
        public Color tierColor;

        public string GetLocalizedTitle()
        {
            return LanguageSelectionUI.Localize(
                $"{badgeIcon} {titleEn}",
                ar: $"{badgeIcon} {titleAr}",
                fa: $"{badgeIcon} {titleAr}",
                ru: $"{badgeIcon} {titleEn}",
                de: $"{badgeIcon} {titleEn}",
                it: $"{badgeIcon} {titleEn}",
                tr: $"{badgeIcon} {titleEn}",
                fr: $"{badgeIcon} {titleEn}",
                es: $"{badgeIcon} {titleEn}",
                zh: $"{badgeIcon} {titleEn}",
                ja: $"{badgeIcon} {titleEn}",
                hi: $"{badgeIcon} {titleEn}",
                pt: $"{badgeIcon} {titleEn}"
            );
        }
    }

    public static readonly RankTierInfo[] RankTiers = new RankTierInfo[]
    {
        new RankTierInfo { tierIndex = 1, titleEn = "Forest Scout", titleAr = "مستكشف الغابة", badgeIcon = "🪵", minScore = 0, tierColor = new Color(0.65f, 0.5f, 0.35f) },
        new RankTierInfo { tierIndex = 2, titleEn = "Oak Guardian", titleAr = "حارس البلوط", badgeIcon = "🌿", minScore = 500, tierColor = new Color(0.35f, 0.8f, 0.4f) },
        new RankTierInfo { tierIndex = 3, titleEn = "Root Defender", titleAr = "مدافع الجذور", badgeIcon = "🛡️", minScore = 1200, tierColor = new Color(0.3f, 0.7f, 0.9f) },
        new RankTierInfo { tierIndex = 4, titleEn = "Beanstalk Knight", titleAr = "فارس الشجرة", badgeIcon = "⚔️", minScore = 2200, tierColor = new Color(0.85f, 0.4f, 0.2f) },
        new RankTierInfo { tierIndex = 5, titleEn = "Shadow Ranger", titleAr = "صياد الظلال", badgeIcon = "🏹", minScore = 3500, tierColor = new Color(0.7f, 0.35f, 0.85f) },
        new RankTierInfo { tierIndex = 6, titleEn = "Verdant Champion", titleAr = "بطل الطبيعة", badgeIcon = "🌲", minScore = 5200, tierColor = new Color(0.2f, 0.95f, 0.5f) },
        new RankTierInfo { tierIndex = 7, titleEn = "Elder Druid", titleAr = "حكيم الغابة", badgeIcon = "🔮", minScore = 7500, tierColor = new Color(0.3f, 0.85f, 1f) },
        new RankTierInfo { tierIndex = 8, titleEn = "Storm Warlord", titleAr = "سيد العواصف", badgeIcon = "⚡", minScore = 10500, tierColor = new Color(1f, 0.85f, 0.2f) },
        new RankTierInfo { tierIndex = 9, titleEn = "Beanstalk Legend", titleAr = "أسطورة الفاصولياء", badgeIcon = "💎", minScore = 15000, tierColor = new Color(0.4f, 0.95f, 1f) },
        new RankTierInfo { tierIndex = 10, titleEn = "Titan Overlord", titleAr = "جبار الشجرة المقدسة", badgeIcon = "👑", minScore = 22000, tierColor = new Color(1f, 0.8f, 0.15f) }
    };

    public static RankTierInfo GetRankTier(int score)
    {
        for (int i = RankTiers.Length - 1; i >= 0; i--)
        {
            if (score >= RankTiers[i].minScore)
            {
                return RankTiers[i];
            }
        }
        return RankTiers[0];
    }

    public RankTierInfo CurrentTier => GetRankTier(RatingScore_AllTime);

    public static string GetSavedPlayerName()
    {
        return PlayerPrefs.GetString(PREF_PLAYER_NAME, "Ghaith");
    }

    public void SetPlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) newName = "Ghaith";
        newName = newName.Trim();
        if (newName.Length > 16) newName = newName.Substring(0, 16);
        PlayerName = newName;
        PlayerPrefs.SetString(PREF_PLAYER_NAME, PlayerName);
        PlayerPrefs.Save();
        OnProfileUpdated?.Invoke();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProfile();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadProfile()
    {
        PlayerName = PlayerPrefs.GetString(PREF_PLAYER_NAME, "Ghaith");
        Coins = PlayerPrefs.GetInt(PREF_COINS, 250); // رصيد افتراضي للبدء
        Gems = PlayerPrefs.GetInt(PREF_GEMS, 30);
        AccountLevel = PlayerPrefs.GetInt(PREF_ACC_LEVEL, 1);
        AccountXP = PlayerPrefs.GetInt(PREF_ACC_XP, 0);
        AdsWatched = PlayerPrefs.GetInt(PREF_ADS_WATCHED, 0);
        PurchasesMade = PlayerPrefs.GetInt(PREF_PURCHASES, 0);
        BossesKilled = PlayerPrefs.GetInt(PREF_BOSSES_KILLED, 0);

        RatingScore_AllTime = PlayerPrefs.GetInt(PREF_RATING_ALLTIME, 420); // نقاط بداية تشجيعية
        RatingScore_Monthly = PlayerPrefs.GetInt(PREF_RATING_MONTHLY, 260);
        RatingScore_Weekly = PlayerPrefs.GetInt(PREF_RATING_WEEKLY, 150);
        RatingScore_Daily = PlayerPrefs.GetInt(PREF_RATING_DAILY, 65);

        CheckDateRollovers();

        // فتح السكن الأساسي (رقم 0) لجميع الشخصيات دائماً
        for (int charId = 0; charId < 20; charId++)
        {
            SetSkinUnlocked(charId, 0, true);
        }
    }

    public void CheckDateRollovers()
    {
        string todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string thisMonthStr = DateTime.UtcNow.ToString("yyyy-MM");
        int currentWeekOfYear = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            DateTime.UtcNow, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        string thisWeekStr = $"{DateTime.UtcNow.Year}-W{currentWeekOfYear}";

        string savedDaily = PlayerPrefs.GetString(PREF_LAST_DAILY_DATE, "");
        string savedWeekly = PlayerPrefs.GetString(PREF_LAST_WEEKLY_DATE, "");
        string savedMonthly = PlayerPrefs.GetString(PREF_LAST_MONTHLY_DATE, "");

        if (savedDaily != todayStr)
        {
            RatingScore_Daily = 0;
            PlayerPrefs.SetString(PREF_LAST_DAILY_DATE, todayStr);
            PlayerPrefs.SetInt(PREF_RATING_DAILY, 0);
        }

        if (savedWeekly != thisWeekStr)
        {
            RatingScore_Weekly = 0;
            PlayerPrefs.SetString(PREF_LAST_WEEKLY_DATE, thisWeekStr);
            PlayerPrefs.SetInt(PREF_RATING_WEEKLY, 0);
        }

        if (savedMonthly != thisMonthStr)
        {
            RatingScore_Monthly = 0;
            PlayerPrefs.SetString(PREF_LAST_MONTHLY_DATE, thisMonthStr);
            PlayerPrefs.SetInt(PREF_RATING_MONTHLY, 0);
        }
    }

    public void SaveProfile()
    {
        PlayerPrefs.SetInt(PREF_COINS, Coins);
        PlayerPrefs.SetInt(PREF_GEMS, Gems);
        PlayerPrefs.SetInt(PREF_ACC_LEVEL, AccountLevel);
        PlayerPrefs.SetInt(PREF_ACC_XP, AccountXP);
        PlayerPrefs.SetInt(PREF_ADS_WATCHED, AdsWatched);
        PlayerPrefs.SetInt(PREF_PURCHASES, PurchasesMade);
        PlayerPrefs.SetInt(PREF_BOSSES_KILLED, BossesKilled);

        PlayerPrefs.SetInt(PREF_RATING_ALLTIME, RatingScore_AllTime);
        PlayerPrefs.SetInt(PREF_RATING_MONTHLY, RatingScore_Monthly);
        PlayerPrefs.SetInt(PREF_RATING_WEEKLY, RatingScore_Weekly);
        PlayerPrefs.SetInt(PREF_RATING_DAILY, RatingScore_Daily);

        PlayerPrefs.Save();

        OnProfileUpdated?.Invoke();
    }

    /// <summary>
    /// إضافة نقاط الأداء التنافسي للترتيب (Leaderboard Rating Points)
    /// في طور الأونلاين والأصدقاء: تحتسب 100% كاملة
    /// في طور الأوفلاين ضد البوتات: تحتسب 25% كنقاط تدريب عادلة
    /// </summary>
    public int AddRatingScore(int points, bool isOnlineMode)
    {
        int awardedPoints = isOnlineMode ? points : Mathf.Max(1, Mathf.RoundToInt(points * 0.25f));
        RankTierInfo previousTier = CurrentTier;

        RatingScore_AllTime += awardedPoints;
        RatingScore_Monthly += awardedPoints;
        RatingScore_Weekly += awardedPoints;
        RatingScore_Daily += awardedPoints;

        SaveProfile();

        RankTierInfo newTier = CurrentTier;
        if (newTier.tierIndex > previousTier.tierIndex)
        {
            OnRewardNotification?.Invoke($"👑 RANK UP! Promoted to {newTier.GetLocalizedTitle()}! (+{awardedPoints} Pts)");
        }
        else
        {
            string modeTag = isOnlineMode ? "" : " (Practice 25%)";
            OnRewardNotification?.Invoke($"🏆 +{awardedPoints} Rating Points earned!{modeTag}");
        }

        return awardedPoints;
    }

    // =========================
    // إضافة الأموال والجواهر
    // =========================
    public void AddCoins(int amount, string reason = "")
    {
        Coins += amount;
        SaveProfile();
        if (!string.IsNullOrEmpty(reason))
        {
            OnRewardNotification?.Invoke($"+{amount} 🌰 Acorns ({reason})");
        }
    }

    public void DeductCoins(int amount, string reason = "")
    {
        Coins = Mathf.Max(0, Coins - amount);
        SaveProfile();
        if (!string.IsNullOrEmpty(reason))
        {
            OnRewardNotification?.Invoke($"-{amount} 🌰 Acorns ({reason})");
        }
    }

    public bool SpendCoins(int amount)
    {
        if (Coins >= amount)
        {
            Coins -= amount;
            SaveProfile();
            return true;
        }
        return false;
    }

    public void AddGems(int amount, string reason = "")
    {
        Gems += amount;
        SaveProfile();
        if (!string.IsNullOrEmpty(reason))
        {
            OnRewardNotification?.Invoke($"+{amount} 💎 Gems ({reason})");
        }
    }

    public bool SpendGems(int amount)
    {
        if (Gems >= amount)
        {
            Gems -= amount;
            SaveProfile();
            return true;
        }
        return false;
    }

    // =========================
    // إضافة نقاط خبرة الحساب (Account XP)
    // =========================
    public void AddAccountXP(int amount)
    {
        AccountXP += amount;
        int xpNeeded = AccountLevel * 300;
        while (AccountXP >= xpNeeded)
        {
            AccountXP -= xpNeeded;
            AccountLevel++;
            AddCoins(100, $"Level Up to {AccountLevel}!");
            AddGems(5, $"Level Up Bonus");
            xpNeeded = AccountLevel * 300;
            OnRewardNotification?.Invoke($"⭐ ACCOUNT LEVEL UP! Now Level {AccountLevel}!");
        }
        SaveProfile();
    }

    // =========================
    // قتل الزعماء (Boss Defeat Rewards)
    // =========================
    public void RegisterBossKill(string bossName, int coinReward, int xpReward, GameObject killer)
    {
        BossesKilled++;
        AddCoins(coinReward, $"Boss Defeated: {bossName}");
        AddAccountXP(xpReward);

        // منح طاقة الجبار (Titan Power Buff) للقاتل
        if (killer != null)
        {
            TitanPowerBuff buff = killer.GetComponent<TitanPowerBuff>();
            if (buff == null)
            {
                buff = killer.AddComponent<TitanPowerBuff>();
            }
            buff.ActivateBuff(bossName);
        }

        OnRewardNotification?.Invoke($"🏆 BOSS SLAIN: {bossName}! +{coinReward} Coins, +{xpReward} XP & TITAN BUFF!");
        SaveProfile();
    }

    // =========================
    // مشاهدة الإعلانات (Watch Rewarded Ads)
    // =========================
    public void WatchRewardedAd(Action<bool> onComplete)
    {
        AdsWatched++;
        int bonusCoins = 80;
        AddCoins(bonusCoins, "Watched Ad Reward");
        AddAccountXP(50);
        SaveProfile();

        OnRewardNotification?.Invoke($"🎬 Ad Watched! +{bonusCoins} 🪙 Coins (Total Ads: {AdsWatched})");
        onComplete?.Invoke(true);
    }

    // =========================
    // متجر الشراء (In-App Purchases Simulation)
    // =========================
    public void PurchaseCoinPackage(int coinsAmount, string packageName)
    {
        PurchasesMade++;
        AddCoins(coinsAmount, packageName);
        AddAccountXP(150);
        SaveProfile();
        OnRewardNotification?.Invoke($"🛒 Purchase Successful: {packageName} (+{coinsAmount} 🪙)");
    }

    public void PurchaseGemPackage(int gemsAmount, string packageName)
    {
        PurchasesMade++;
        AddGems(gemsAmount, packageName);
        AddAccountXP(250);
        SaveProfile();
        OnRewardNotification?.Invoke($"🛒 Purchase Successful: {packageName} (+{gemsAmount} 💎)");
    }

    // =========================
    // نظام السكنات (Character Skins)
    // =========================
    public bool IsSkinUnlocked(int characterId, int skinIndex)
    {
        if (skinIndex == 0) return true; // السكن الافتراضي مفتوح دائماً
        return PlayerPrefs.GetInt($"BW_Skin_{characterId}_{skinIndex}", 0) == 1;
    }

    public void SetSkinUnlocked(int characterId, int skinIndex, bool unlocked)
    {
        PlayerPrefs.SetInt($"BW_Skin_{characterId}_{skinIndex}", unlocked ? 1 : 0);
        PlayerPrefs.Save();
        OnProfileUpdated?.Invoke();
    }

    public int GetSelectedSkin(int characterId)
    {
        return PlayerPrefs.GetInt($"BW_SelectedSkin_{characterId}", 0);
    }

    public void SetSelectedSkin(int characterId, int skinIndex)
    {
        PlayerPrefs.SetInt($"BW_SelectedSkin_{characterId}", skinIndex);
        PlayerPrefs.Save();
        OnProfileUpdated?.Invoke();
    }
}
