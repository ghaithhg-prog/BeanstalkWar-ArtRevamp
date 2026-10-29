using UnityEngine;
using System.Collections;

/// <summary>
/// واجهة المستخدم الشاملة للاقتصاد، ونظام السكنات، ومشاهدة الإعلانات، والمتجر
/// تظهر رصيد العملات والجواهر ومستوى الحساب، وتسمح بتبديل وشراء السكنات
/// ومشاهدة الإعلانات لكسب المكافآت
/// </summary>
public class PlayerProgressionAndShopUI : MonoBehaviour
{
    public static PlayerProgressionAndShopUI Instance { get; private set; }
    public static bool IsShopOpen => Instance != null && (Instance.showShopModal || Instance.isWatchingAd);

    private bool showShopModal = false;
    private bool isWatchingAd = false;
    private float adTimer = 0f;
    private string activeNotification = "";
    private float notificationTimer = 0f;

    // السكن المختار حالياً للاستعراض
    private int currentBrowsingCharId = 0;
    private int currentBrowsingSkinIndex = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // التأكد من وجود PlayerProfile
        if (PlayerProfile.Instance == null)
        {
            GameObject profileObj = new GameObject("PlayerProfile");
            profileObj.AddComponent<PlayerProfile>();
        }

        // التأكد من وجود LevelBossManager
        if (LevelBossManager.Instance == null)
        {
            GameObject bossMgrObj = new GameObject("LevelBossManager");
            bossMgrObj.AddComponent<LevelBossManager>();
        }

        if (PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.OnRewardNotification += ShowNotification;
        }
    }

    void OnDestroy()
    {
        if (PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.OnRewardNotification -= ShowNotification;
        }
    }

    public void ShowNotification(string msg)
    {
        activeNotification = msg;
        notificationTimer = 4.5f;
    }

    private bool wasPlaying = false;

    void Update()
    {
        bool isPlaying = GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying();
        if (isPlaying && !wasPlaying)
        {
            // عند بداية المباراة، نغلق نافذة المتجر تلقائياً لتكون الشاشة نظيفة بالكامل
            showShopModal = false;
        }
        wasPlaying = isPlaying;

        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
        }

        if (isWatchingAd)
        {
            adTimer -= Time.deltaTime;
            if (adTimer <= 0f)
            {
                isWatchingAd = false;
                if (PlayerProfile.Instance != null)
                {
                    PlayerProfile.Instance.WatchRewardedAd(null);
                }
            }
        }

        // مفتاح اختصار للمتجر (B) إذا أراد اللاعب فتحه يدوياً
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleShop();
        }
    }

    public void OpenShop()
    {
        showShopModal = true;
    }

    public void CloseShop()
    {
        showShopModal = false;
    }

    public void ToggleShop()
    {
        showShopModal = !showShopModal;
    }

    void OnGUI()
    {
        if (GameIntroBriefingUI.IsBriefingOpen) return;

        bool isPlaying = GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying();

        // أثناء اللعب والقتال: إخفاء شريط الحالة العلوي والمتجر والنقاط لتكون ساحة المعركة واضحة تماماً
        if (isPlaying)
        {
            // تظهر فقط إذا فتح اللاعب المتجر يدوياً بالضغط على زر B
            if (showShopModal)
            {
                DrawShopWindow();
            }

            // إشعار فوز أو مكافأة قصير يختفي تلقائياً
            DrawNotificationBanner();
            return;
        }

        // عند فتح شاشات اللوبي، واختيار الفرق، واختيار الأبطال، أو شاشة البداية، نخفي الشريط العلوي
        if (PlayModeUI.isTitleScreenActive)
        {
            DrawNotificationBanner();
            return;
        }

        bool isModalOrLobbyOpen = CharacterSelectionUI.IsSelectionOpen || 
                                  TeamSelectionUI.IsTeamSelectionOpen || 
                                  (FriendlyLobbyUI.Instance != null && FriendlyLobbyUI.Instance.isLobbyActive) ||
                                  InGamePauseMenuUI.IsPauseMenuOpen ||
                                  LanguageSelectionUI.IsLanguageModalOpen ||
                                  LeaderboardUI.IsLeaderboardOpen ||
                                  FriendsUI.IsFriendsOpen;

        if (isModalOrLobbyOpen && !showShopModal && !isWatchingAd)
        {
            DrawNotificationBanner();
            return;
        }

        // قبل بدء المباراة (في القائمة الرئيسية فقط بدون نوافذ):
        DrawTopStatusBar();
        DrawNotificationBanner();

        if (isWatchingAd)
        {
            DrawAdSimulationOverlay();
        }

        if (showShopModal)
        {
            DrawShopWindow();
        }
    }

    // =========================
    // 1. شريط الحالة العلوي (العملات، الجواهر، المستوى، زر الإعلانات والمتجر)
    // =========================
    void DrawTopStatusBar()
    {
        if (PlayerProfile.Instance == null) return;

        float barWidth = 455f;
        float barHeight = 36f;
        float x = 140f;
        float y = 8f;

        // Antique Gold & Obsidian Glass Plaque
        EnchantedNatureGUI.DrawAntiqueGoldFrame(new Rect(x, y, barWidth, barHeight), EnchantedNatureGUI.EmeraldAccent, 0.1f);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 11;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.alignment = TextAnchor.MiddleLeft;

        // Acorns / Coins
        string acornsLabel = LanguageSelectionUI.Localize("Acorns", ar: "بلوط", ru: "Желуди", es: "Bellotas", fr: "Glands");
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(x + 10f, y + 2f, 100f, barHeight - 4f),
            $"🪙 {PlayerProfile.Instance.Coins} {acornsLabel}",
            textStyle,
            EnchantedNatureGUI.AntiqueGoldBright,
            new Color(0.02f, 0.04f, 0.02f, 0.95f),
            1f, 1f
        );

        // Gems / Emeralds
        string gemsLabel = LanguageSelectionUI.Localize("Gems", ar: "جواهر", ru: "Гемы", es: "Gemas", fr: "Gemmes");
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(x + 112f, y + 2f, 90f, barHeight - 4f),
            $"💎 {PlayerProfile.Instance.Gems} {gemsLabel}",
            textStyle,
            new Color(0.35f, 0.92f, 1f),
            new Color(0.02f, 0.04f, 0.02f, 0.95f),
            1f, 1f
        );

        // Account Level / Druid Rank
        string lvlLabel = LanguageSelectionUI.Localize($"⭐ Lv.{PlayerProfile.Instance.AccountLevel}", ar: $"⭐ مستوى {PlayerProfile.Instance.AccountLevel}", ru: $"⭐ Ур.{PlayerProfile.Instance.AccountLevel}", es: $"⭐ Nvl.{PlayerProfile.Instance.AccountLevel}", fr: $"⭐ Niv.{PlayerProfile.Instance.AccountLevel}");
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(x + 204f, y + 2f, 75f, barHeight - 4f),
            lvlLabel,
            textStyle,
            new Color(1f, 0.88f, 0.45f),
            new Color(0.02f, 0.04f, 0.02f, 0.95f),
            1f, 1f
        );

        // Forest Ad Blessing Button
        string giftLabel = LanguageSelectionUI.Localize("🍃 +80 🪙", ar: "🍃 +80 🪙", ru: "🍃 +80 🪙", es: "🍃 +80 🪙", fr: "🍃 +80 🪙");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + 282f, y + 3f, 78f, 30f), giftLabel, EnchantedNatureGUI.EmeraldAccent, 10, false))
        {
            StartAdSimulation();
        }

        // Wild Emporium / Shop Button
        string shopBtnLabel = showShopModal 
            ? LanguageSelectionUI.Localize("❌ Close", ar: "❌ إغلاق", ru: "❌ Закрыть", es: "❌ Cerrar", fr: "❌ Fermer") 
            : LanguageSelectionUI.Localize("🌿 Emporium", ar: "🌿 المتجر", ru: "🌿 Магазин", es: "🌿 Tienda", fr: "🌿 Boutique");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + 364f, y + 3f, 85f, 30f), shopBtnLabel, EnchantedNatureGUI.AntiqueGoldBright, 10, true))
        {
            showShopModal = !showShopModal;
        }

        // Leaderboards Button (🏆 المتصدرون)
        float leadX = x + barWidth + 8f;
        float leadW = 115f;
        string leadLabel = LanguageSelectionUI.Localize("🏆 Leaders", ar: "🏆 المتصدرون", fa: "🏆 برترین‌ها", ru: "🏆 Лидеры");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(leadX, y + 1f, leadW, 34f), leadLabel, EnchantedNatureGUI.AntiqueGoldBright, 11, LeaderboardUI.IsLeaderboardOpen))
        {
            LeaderboardUI.ToggleLeaderboard();
        }

        // Friends Hub Button (👥 الأصدقاء)
        float friendsX = leadX + leadW + 6f;
        float friendsW = 95f;
        string friendsLabel = LanguageSelectionUI.Localize("👥 Friends", ar: "👥 الأصدقاء", fa: "👥 دوستان", ru: "👥 Друзья");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(friendsX, y + 1f, friendsW, 34f), friendsLabel, EnchantedNatureGUI.EmeraldAccent, 11, FriendsUI.IsFriendsOpen))
        {
            FriendsUI.ToggleFriends();
        }
    }

    // =========================
    // 2. Floating Notification Banner
    // =========================
    void DrawNotificationBanner()
    {
        if (notificationTimer <= 0f || string.IsNullOrEmpty(activeNotification)) return;

        float width = 500f;
        float height = 38f;
        float x = (Screen.width - width) / 2f;
        float y = 108f;

        GUI.color = new Color(0.12f, 0.45f, 0.22f, 0.96f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);

        GUI.color = new Color(0.35f, 0.95f, 0.5f);
        GUI.DrawTexture(new Rect(x, y, width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + height - 2f, width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle notifStyle = new GUIStyle(GUI.skin.label);
        notifStyle.fontSize = 13;
        notifStyle.fontStyle = FontStyle.Bold;
        notifStyle.alignment = TextAnchor.MiddleCenter;
        notifStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x, y, width, height), activeNotification, notifStyle);
    }

    // =========================
    // 3. Rewarded Video Ad Simulation
    // =========================
    public void StartAdSimulation()
    {
        if (isWatchingAd) return;
        isWatchingAd = true;
        adTimer = 3.0f;
    }

    void DrawAdSimulationOverlay()
    {
        GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.97f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle centerStyle = new GUIStyle(GUI.skin.label);
        centerStyle.fontSize = 20;
        centerStyle.fontStyle = FontStyle.Bold;
        centerStyle.alignment = TextAnchor.MiddleCenter;
        centerStyle.normal.textColor = new Color(0.98f, 0.88f, 0.3f);

        GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 40), "🍃 SACRED FOREST SPONSORED BLESSING 🍃", centerStyle);

        centerStyle.fontSize = 15;
        centerStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height / 2f - 10, Screen.width, 30),
            $"Absorbing nature wisdom... Finished in {Mathf.CeilToInt(adTimer)}s", centerStyle);

        centerStyle.fontSize = 13;
        centerStyle.normal.textColor = new Color(0.3f, 0.95f, 0.55f);
        GUI.Label(new Rect(0, Screen.height / 2f + 30, Screen.width, 30),
            "Reward: +80 🪙 Acorns & Account XP & Progress towards Hero Skins!", centerStyle);
    }

    // =========================
    // 4. Sacred Forest Emporium Window
    // =========================
    void DrawShopWindow()
    {
        float w = 550f;
        float h = 400f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        // Carved Wood Background
        GUI.color = new Color(0.12f, 0.16f, 0.10f, 0.98f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // Emerald Vine Border
        GUI.color = new Color(0.24f, 0.82f, 0.38f);
        GUI.DrawTexture(new Rect(x, y, w, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - 4f, w, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 4f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - 4f, y, 4f, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 17;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = new Color(0.98f, 0.88f, 0.35f);

        GUI.Label(new Rect(x, y + 10, w, 30), "🌿 SACRED FOREST EMPORIUM 🌿", headerStyle);

        // Acorns and Gems Section
        GUIStyle subHead = new GUIStyle(GUI.skin.label);
        subHead.fontSize = 13;
        subHead.fontStyle = FontStyle.Bold;
        subHead.normal.textColor = new Color(0.4f, 0.95f, 0.6f);

        GUI.Label(new Rect(x + 20, y + 45, 250, 25), "🍃 Acorn & Gem Blessings (IAP):", subHead);

        // Bundle 1
        if (GUI.Button(new Rect(x + 25, y + 75, 150, 48), "+500 🪙 Acorns\n[ $1.99 ]"))
        {
            PlayerProfile.Instance.PurchaseCoinPackage(500, "Small Acorn Pouch");
        }

        // Bundle 2
        if (GUI.Button(new Rect(x + 195, y + 75, 150, 48), "+1500 🪙 Acorns\n[ $4.99 ]"))
        {
            PlayerProfile.Instance.PurchaseCoinPackage(1500, "Mountain of Acorns");
        }

        // Bundle 3
        if (GUI.Button(new Rect(x + 365, y + 75, 150, 48), "+80 💎 Emeralds\n[ $2.99 ]"))
        {
            PlayerProfile.Instance.PurchaseGemPackage(80, "Emerald Chest");
        }

        // Skins Section
        GUI.Label(new Rect(x + 20, y + 138, 320, 25), "🎭 Druid Beast Skins & Power Stats:", subHead);

        CharacterSkin[] skins = CharacterSkinDatabase.GetSkinsForCharacter(currentBrowsingCharId);
        if (skins != null && skins.Length > 0)
        {
            currentBrowsingSkinIndex = Mathf.Clamp(currentBrowsingSkinIndex, 0, skins.Length - 1);
            CharacterSkin activeSkin = skins[currentBrowsingSkinIndex];

            // أزرار التنقل بين السكنات
            if (GUI.Button(new Rect(x + 25, y + 165, 35, 28), "◀"))
            {
                currentBrowsingSkinIndex = (currentBrowsingSkinIndex - 1 + skins.Length) % skins.Length;
            }

            GUIStyle skinNameStyle = new GUIStyle(GUI.skin.label);
            skinNameStyle.fontSize = 14;
            skinNameStyle.fontStyle = FontStyle.Bold;
            skinNameStyle.alignment = TextAnchor.MiddleCenter;
            skinNameStyle.normal.textColor = activeSkin.skinColor;

            GUI.Label(new Rect(x + 65, y + 165, 250, 28), activeSkin.skinName, skinNameStyle);

            if (GUI.Button(new Rect(x + 320, y + 165, 35, 28), "▶"))
            {
                currentBrowsingSkinIndex = (currentBrowsingSkinIndex + 1) % skins.Length;
            }

            // وصف السكن ومضاعفات القوة
            GUIStyle descStyle = new GUIStyle(GUI.skin.label);
            descStyle.fontSize = 11;
            descStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 25, y + 200, w - 50, 36), activeSkin.description, descStyle);

            string statsText = $"⚡ Power Stats: +{(int)((activeSkin.healthMultiplier - 1f) * 100)}% HP | +{(int)((activeSkin.damageMultiplier - 1f) * 100)}% DMG | +{(int)((activeSkin.speedMultiplier - 1f) * 100)}% SPD";
            GUIStyle statsStyle = new GUIStyle(GUI.skin.label);
            statsStyle.fontSize = 12;
            statsStyle.fontStyle = FontStyle.Bold;
            statsStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(x + 25, y + 235, w - 50, 24), statsText, statsStyle);

            // حالة الفتح / التحديد
            bool isUnlocked = PlayerProfile.Instance.IsSkinUnlocked(currentBrowsingCharId, currentBrowsingSkinIndex);
            int selectedSkin = PlayerProfile.Instance.GetSelectedSkin(currentBrowsingCharId);

            if (isUnlocked)
            {
                if (selectedSkin == currentBrowsingSkinIndex)
                {
                    GUI.color = Color.green;
                    GUI.Box(new Rect(x + 25, y + 265, 180, 36), "✔ EQUIPPED");
                    GUI.color = Color.white;
                }
                else
                {
                    if (GUI.Button(new Rect(x + 25, y + 265, 180, 36), "Equip This Skin"))
                    {
                        PlayerProfile.Instance.SetSelectedSkin(currentBrowsingCharId, currentBrowsingSkinIndex);
                        ShowNotification($"Equipped {activeSkin.skinName}!");
                    }
                }
            }
            else
            {
                // خيارات الفتح بحسب النوع المطلوب
                switch (activeSkin.unlockMethod)
                {
                    case CharacterSkin.UnlockMethod.AccountLevel:
                        if (PlayerProfile.Instance.AccountLevel >= activeSkin.requiredLevel)
                        {
                            if (GUI.Button(new Rect(x + 25, y + 265, 200, 36), "Unlock Now (Level Reached!)"))
                            {
                                PlayerProfile.Instance.SetSkinUnlocked(currentBrowsingCharId, currentBrowsingSkinIndex, true);
                                PlayerProfile.Instance.SetSelectedSkin(currentBrowsingCharId, currentBrowsingSkinIndex);
                            }
                        }
                        else
                        {
                            GUI.color = Color.gray;
                            GUI.Box(new Rect(x + 25, y + 265, 220, 36), $"🔒 Requires Account Lv.{activeSkin.requiredLevel}");
                            GUI.color = Color.white;
                        }
                        break;

                    case CharacterSkin.UnlockMethod.WatchAds:
                        if (PlayerProfile.Instance.AdsWatched >= activeSkin.requiredAds)
                        {
                            if (GUI.Button(new Rect(x + 25, y + 265, 200, 36), "Unlock (Ads Goal Reached!)"))
                            {
                                PlayerProfile.Instance.SetSkinUnlocked(currentBrowsingCharId, currentBrowsingSkinIndex, true);
                                PlayerProfile.Instance.SetSelectedSkin(currentBrowsingCharId, currentBrowsingSkinIndex);
                            }
                        }
                        else
                        {
                            if (GUI.Button(new Rect(x + 25, y + 265, 230, 36), $"🎬 Watch Ad ({PlayerProfile.Instance.AdsWatched}/{activeSkin.requiredAds})"))
                            {
                                StartAdSimulation();
                            }
                        }
                        break;

                    case CharacterSkin.UnlockMethod.PurchaseCoins:
                        if (GUI.Button(new Rect(x + 25, y + 265, 220, 36), $"Buy for {activeSkin.requiredCost} 🪙"))
                        {
                            if (PlayerProfile.Instance.SpendCoins(activeSkin.requiredCost))
                            {
                                PlayerProfile.Instance.SetSkinUnlocked(currentBrowsingCharId, currentBrowsingSkinIndex, true);
                                PlayerProfile.Instance.SetSelectedSkin(currentBrowsingCharId, currentBrowsingSkinIndex);
                                ShowNotification($"Unlocked {activeSkin.skinName}!");
                            }
                            else
                            {
                                ShowNotification("❌ Not enough coins! Watch ads or defeat bosses!");
                            }
                        }
                        break;
                }
            }
        }

        // زر الإغلاق
        if (GUI.Button(new Rect(x + w - 100, y + h - 45, 80, 32), "Close"))
        {
            showShopModal = false;
        }
    }
}
