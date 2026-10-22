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

    // البيانات المحفوظة
    public string PlayerName { get; private set; } = "Ghaith";
    public int Coins { get; private set; }
    public int Gems { get; private set; }
    public int AccountLevel { get; private set; }
    public int AccountXP { get; private set; }
    public int AdsWatched { get; private set; }
    public int PurchasesMade { get; private set; }
    public int BossesKilled { get; private set; }

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

        // فتح السكن الأساسي (رقم 0) لجميع الشخصيات دائماً
        for (int charId = 0; charId < 20; charId++)
        {
            SetSkinUnlocked(charId, 0, true);
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
        PlayerPrefs.Save();

        OnProfileUpdated?.Invoke();
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
            OnRewardNotification?.Invoke($"+{amount} 🪙 Coins ({reason})");
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
