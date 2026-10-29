using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// واجهة لوائح المتصدرين (Hall of Champions / Leaderboards UI):
/// نافذة تنافسية عتيقة ساحرة تعرض كبار أبطال اللعبة عبر 4 فترات زمنية (يومي، أسبوعي، شهري، عام).
/// تُظهر شريطاً علوياً ثابتاً لترتيب ورتبة اللاعب الحالي، مع إمكانية إرسال طلب صداقة لأي بطل بنقرة واحدة.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }
    public static bool IsLeaderboardOpen { get; private set; } = false;

    private LeaderboardManager.Timeframe activeTab = LeaderboardManager.Timeframe.AllTime;
    private Vector2 scrollPos = Vector2.zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("LeaderboardUI");
            obj.AddComponent<LeaderboardUI>();
            DontDestroyOnLoad(obj);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static void OpenLeaderboard()
    {
        IsLeaderboardOpen = true;
    }

    public static void CloseLeaderboard()
    {
        IsLeaderboardOpen = false;
    }

    public static void ToggleLeaderboard()
    {
        IsLeaderboardOpen = !IsLeaderboardOpen;
    }

    void Update()
    {
        if (IsLeaderboardOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLeaderboard();
        }
    }

    void OnGUI()
    {
        if (!IsLeaderboardOpen) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) return;

        Font activeFont = EnchantedNatureGUI.GetActiveGameFont();
        if (activeFont != null) GUI.skin.font = activeFont;

        // تعتيم الخلفية
        GUI.color = new Color(0.02f, 0.05f, 0.03f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float winW = Mathf.Clamp(Screen.width * 0.88f, 600f, 880f);
        float winH = Mathf.Clamp(Screen.height * 0.88f, 500f, 690f);
        float winX = (Screen.width - winW) * 0.5f;
        float winY = (Screen.height - winH) * 0.5f;

        Rect winRect = new Rect(winX, winY, winW, winH);

        // إطار خشبي عتيق ونقوش أوراق الشجر الذهبية
        EnchantedNatureGUI.DrawWoodPlate(winRect, new Color(0.06f, 0.11f, 0.07f, 0.98f), new Color(0.88f, 0.74f, 0.32f, 0.95f));
        EnchantedNatureGUI.DrawCornerFiligree(new Rect(winX + 4f, winY + 4f, winW - 8f, winH - 8f), new Color(0.95f, 0.82f, 0.35f, 0.85f), 20f);

        // زر الإغلاق العلوي
        if (GUI.Button(new Rect(winX + winW - 44f, winY + 12f, 32f, 32f), "✕"))
        {
            CloseLeaderboard();
            return;
        }

        // العنوان الرئيسي
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = EnchantedNatureGUI.AntiqueGoldBright;

        string title = LanguageSelectionUI.Localize(
            "🏆 HALL OF CHAMPIONS",
            ar: "🏆 مجلس المتصدرين الأسطوري",
            fa: "🏆 تالار قهرمانان افسانه‌ای",
            ru: "🏆 ЗАЛ СЛАВЫ ЧЕМПИОНОВ",
            de: "🏆 HALLE DER CHAMPIONS",
            fr: "🏆 LE PANTHÉON DES CHAMPIONS",
            es: "🏆 SALÓN DE CAMPEONES",
            it: "🏆 SALA DEI CAMPIONI",
            tr: "🏆 ŞAMPİYONLAR SALONU",
            zh: "🏆 荣耀英雄殿堂",
            ja: "🏆 歴戦の勇士殿堂",
            hi: "🏆 चैंपियंस का महामंच",
            pt: "🏆 SALÃO DOS CAMPEÕES"
        );
        GUI.Label(new Rect(winX, winY + 12f, winW, 28f), title, titleStyle);

        // الألسنة الزمنية الأربعة (Tabs)
        DrawTimeframeTabs(winX + 24f, winY + 46f, winW - 48f, 34f);

        // جلب قائمة الترتيب وحقن اللاعب المحلي
        LeaderboardManager.LeaderboardEntry localEntry = null;
        List<LeaderboardManager.LeaderboardEntry> entries = new List<LeaderboardManager.LeaderboardEntry>();
        if (LeaderboardManager.Instance != null)
        {
            entries = LeaderboardManager.Instance.GetLeaderboard(activeTab, out localEntry);
        }

        // شريط الترتيب الشخصي للاعب الحالي (Pinned Player Banner)
        DrawLocalPlayerBanner(winX + 24f, winY + 86f, winW - 48f, 54f, localEntry, entries.Count);

        // جدول المتصدرين القابل للتمرير
        DrawLeaderboardTable(winX + 24f, winY + 148f, winW - 48f, winH - 212f, entries);

        // الشريط السفلي: معلومة كسب النقاط وزر الأصدقاء السريع
        DrawBottomBar(winX + 24f, winY + winH - 54f, winW - 48f, 42f);
    }

    void DrawTimeframeTabs(float x, float y, float w, float h)
    {
        float tabW = w / 4f;

        string tabDaily = LanguageSelectionUI.Localize("📅 Daily", ar: "📅 اليومي", fa: "📅 روزانه", ru: "📅 День");
        string tabWeekly = LanguageSelectionUI.Localize("🗓️ Weekly", ar: "🗓️ الأسبوعي", fa: "🗓️ هفتگی", ru: "🗓️ Неделя");
        string tabMonthly = LanguageSelectionUI.Localize("🌙 Monthly", ar: "🌙 الشهري", fa: "🌙 ماهانه", ru: "🌙 Месяц");
        string tabAllTime = LanguageSelectionUI.Localize("👑 All-Time", ar: "👑 العام الأبدي", fa: "👑 کل ادوار", ru: "👑 За всё время");

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x, y, tabW - 4f, h), tabDaily, activeTab == LeaderboardManager.Timeframe.Daily ? EnchantedNatureGUI.EmeraldAccent : EnchantedNatureGUI.AntiqueGoldMid, 12, activeTab == LeaderboardManager.Timeframe.Daily))
        {
            activeTab = LeaderboardManager.Timeframe.Daily;
            scrollPos = Vector2.zero;
        }

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + tabW, y, tabW - 4f, h), tabWeekly, activeTab == LeaderboardManager.Timeframe.Weekly ? EnchantedNatureGUI.EmeraldAccent : EnchantedNatureGUI.AntiqueGoldMid, 12, activeTab == LeaderboardManager.Timeframe.Weekly))
        {
            activeTab = LeaderboardManager.Timeframe.Weekly;
            scrollPos = Vector2.zero;
        }

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + tabW * 2f, y, tabW - 4f, h), tabMonthly, activeTab == LeaderboardManager.Timeframe.Monthly ? EnchantedNatureGUI.EmeraldAccent : EnchantedNatureGUI.AntiqueGoldMid, 12, activeTab == LeaderboardManager.Timeframe.Monthly))
        {
            activeTab = LeaderboardManager.Timeframe.Monthly;
            scrollPos = Vector2.zero;
        }

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + tabW * 3f, y, tabW - 4f, h), tabAllTime, activeTab == LeaderboardManager.Timeframe.AllTime ? EnchantedNatureGUI.EmeraldAccent : EnchantedNatureGUI.AntiqueGoldMid, 12, activeTab == LeaderboardManager.Timeframe.AllTime))
        {
            activeTab = LeaderboardManager.Timeframe.AllTime;
            scrollPos = Vector2.zero;
        }
    }

    void DrawLocalPlayerBanner(float x, float y, float w, float h, LeaderboardManager.LeaderboardEntry localEntry, int totalCount)
    {
        Rect bannerRect = new Rect(x, y, w, h);
        EnchantedNatureGUI.DrawWoodPlate(bannerRect, new Color(0.12f, 0.18f, 0.12f, 0.98f), EnchantedNatureGUI.AntiqueGoldBright);

        if (localEntry == null) return;

        var tier = PlayerProfile.GetRankTier(localEntry.score);

        // شارة الرتبة وأيقونة البطل
        GUIStyle iconStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26 };
        GUI.Label(new Rect(x + 10f, y + 8f, 40f, 38f), tier.badgeIcon, iconStyle);

        // تفاصيل رتبة اللاعب الحالي
        GUIStyle myNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        myNameStyle.normal.textColor = Color.white;

        string youLabel = LanguageSelectionUI.Localize("YOU", ar: "أنت");
        GUI.Label(new Rect(x + 55f, y + 7f, 260f, 22f), $"⭐ {localEntry.playerName} ({youLabel})", myNameStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        subStyle.normal.textColor = tier.tierColor;
        GUI.Label(new Rect(x + 55f, y + 28f, 280f, 20f), $"{tier.GetLocalizedTitle()}", subStyle);

        // النقاط في المنتصف
        GUIStyle ptsStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        ptsStyle.normal.textColor = EnchantedNatureGUI.AntiqueGoldBright;
        string ptsStr = LanguageSelectionUI.Localize($"{localEntry.score} Points", ar: $"{localEntry.score} نقطة تصنيف");
        GUI.Label(new Rect(x + w * 0.45f, y + 14f, w * 0.25f, 26f), ptsStr, ptsStyle);

        // المركز والترتيب على اليمين
        GUIStyle rankStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 16, fontStyle = FontStyle.Bold };
        rankStyle.normal.textColor = localEntry.rank <= 3 ? new Color(1f, 0.85f, 0.2f) : new Color(0.35f, 0.9f, 0.5f);

        string rankText = LanguageSelectionUI.Localize(
            $"Rank: #{localEntry.rank} of {totalCount}",
            ar: $"الترتيب: المركز #{localEntry.rank} من {totalCount}",
            fa: $"رتبه: #{localEntry.rank} از {totalCount}"
        );
        GUI.Label(new Rect(x + w - 240f, y + 14f, 225f, 26f), rankText, rankStyle);
    }

    void DrawLeaderboardTable(float x, float y, float w, float h, List<LeaderboardManager.LeaderboardEntry> entries)
    {
        // ترويسة الجدول
        float headerH = 26f;
        Rect headerRect = new Rect(x, y, w, headerH);
        EnchantedNatureGUI.DrawWoodPlate(headerRect, new Color(0.08f, 0.13f, 0.09f, 0.95f), new Color(0.45f, 0.55f, 0.45f, 0.8f));

        GUIStyle headStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
        headStyle.normal.textColor = new Color(0.75f, 0.85f, 0.75f);

        GUI.Label(new Rect(x + 12f, y + 4f, 65f, 20f), LanguageSelectionUI.Localize("# RANK", ar: "# المركز"), headStyle);
        GUI.Label(new Rect(x + 85f, y + 4f, 180f, 20f), LanguageSelectionUI.Localize("CHAMPION", ar: "البطل المتصدر"), headStyle);
        GUI.Label(new Rect(x + 275f, y + 4f, 150f, 20f), LanguageSelectionUI.Localize("RANKED TIER", ar: "الرتبة التنافسية"), headStyle);
        GUI.Label(new Rect(x + 435f, y + 4f, 110f, 20f), LanguageSelectionUI.Localize("RATING", ar: "النقاط"), headStyle);
        GUI.Label(new Rect(x + w - 130f, y + 4f, 120f, 20f), LanguageSelectionUI.Localize("ACTION", ar: "التواصل"), headStyle);

        // المحتوى القابل للتمرير
        float listY = y + headerH + 4f;
        float listH = h - headerH - 4f;
        Rect listRect = new Rect(x, listY, w, listH);

        float rowH = 46f;
        float totalH = entries.Count * (rowH + 4f);
        Rect viewRect = new Rect(0, 0, w - 22f, Mathf.Max(totalH, listH));

        scrollPos = GUI.BeginScrollView(listRect, scrollPos, viewRect);

        GUIStyle rowRankStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 13, fontStyle = FontStyle.Bold };
        GUIStyle rowNameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 13, fontStyle = FontStyle.Bold };
        GUIStyle rowTierStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 11 };
        GUIStyle rowScoreStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 13, fontStyle = FontStyle.Bold };

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            float itemY = i * (rowH + 4f);
            Rect rowRect = new Rect(0, itemY, viewRect.width, rowH);

            var tier = PlayerProfile.GetRankTier(entry.score);

            // تظليل خاص للاعب المحلي، وألوان مميزة للثلاثة الأوائل
            Color bgCol = entry.isLocalPlayer
                ? new Color(0.20f, 0.22f, 0.10f, 0.96f)
                : (i % 2 == 0 ? new Color(0.09f, 0.14f, 0.10f, 0.92f) : new Color(0.07f, 0.11f, 0.08f, 0.92f));

            Color borderCol = entry.isLocalPlayer
                ? EnchantedNatureGUI.AntiqueGoldBright
                : (entry.rank == 1 ? new Color(1f, 0.85f, 0.2f, 0.8f) : (entry.rank == 2 ? new Color(0.85f, 0.85f, 0.9f, 0.7f) : (entry.rank == 3 ? new Color(0.85f, 0.55f, 0.25f, 0.7f) : new Color(0.3f, 0.4f, 0.3f, 0.5f))));

            EnchantedNatureGUI.DrawWoodPlate(rowRect, bgCol, borderCol);

            // رقم المركز (مع أوسمة الذهب/الفضة/البرونز)
            string medal = entry.rank == 1 ? "🥇" : (entry.rank == 2 ? "🥈" : (entry.rank == 3 ? "🥉" : $"#{entry.rank}"));
            rowRankStyle.normal.textColor = entry.rank == 1 ? new Color(1f, 0.85f, 0.2f) : (entry.rank <= 3 ? Color.white : new Color(0.7f, 0.8f, 0.7f));
            GUI.Label(new Rect(10f, itemY + 11f, 60f, 24f), medal, rowRankStyle);

            // اسم البطل
            rowNameStyle.normal.textColor = entry.isLocalPlayer ? EnchantedNatureGUI.AntiqueGoldBright : Color.white;
            string displayName = entry.isLocalPlayer ? $"⭐ {entry.playerName}" : entry.playerName;
            GUI.Label(new Rect(80f, itemY + 11f, 185f, 24f), displayName, rowNameStyle);

            // الرتبة التنافسية
            rowTierStyle.normal.textColor = tier.tierColor;
            GUI.Label(new Rect(270f, itemY + 12f, 155f, 22f), $"{tier.badgeIcon} {tier.GetLocalizedTitle()}", rowTierStyle);

            // النقاط
            rowScoreStyle.normal.textColor = EnchantedNatureGUI.AntiqueGoldBright;
            GUI.Label(new Rect(430f, itemY + 11f, 100f, 24f), $"{entry.score} Pts", rowScoreStyle);

            // زر التفاعل (إضافة صديق)
            float actX = viewRect.width - 125f;
            float actY = itemY + 8f;
            float actW = 115f;
            float actH = 30f;

            if (entry.isLocalPlayer)
            {
                GUIStyle youStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11, fontStyle = FontStyle.Bold };
                youStyle.normal.textColor = EnchantedNatureGUI.AntiqueGoldBright;
                GUI.Label(new Rect(actX, actY, actW, actH), LanguageSelectionUI.Localize("⭐ YOU", ar: "⭐ أنت"), youStyle);
            }
            else if (FriendsManager.Instance != null && FriendsManager.Instance.IsFriend(entry.playerName))
            {
                GUIStyle friendStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
                friendStyle.normal.textColor = new Color(0.35f, 0.95f, 0.5f);
                GUI.Label(new Rect(actX, actY, actW, actH), LanguageSelectionUI.Localize("✅ Friend", ar: "✅ صديق"), friendStyle);
            }
            else if (FriendsManager.Instance != null && FriendsManager.Instance.HasPendingRequest(entry.playerName))
            {
                GUIStyle pendStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
                pendStyle.normal.textColor = new Color(1f, 0.75f, 0.3f);
                GUI.Label(new Rect(actX, actY, actW, actH), LanguageSelectionUI.Localize("⏳ Pending", ar: "⏳ معلق"), pendStyle);
            }
            else
            {
                string addTxt = LanguageSelectionUI.Localize("➕ Add Friend", ar: "➕ أضف صديق");
                if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(actX, actY, actW, actH), addTxt, EnchantedNatureGUI.AntiqueGoldMid, 11, false))
                {
                    FriendsManager.Instance?.SendFriendRequest(entry.playerName);
                }
            }
        }

        GUI.EndScrollView();
    }

    void DrawBottomBar(float x, float y, float w, float h)
    {
        Rect btmRect = new Rect(x, y, w, h);
        EnchantedNatureGUI.DrawWoodPlate(btmRect, new Color(0.08f, 0.13f, 0.08f, 0.95f), new Color(0.4f, 0.5f, 0.4f, 0.7f));

        // نص التوضيح
        GUIStyle tipStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 11 };
        tipStyle.normal.textColor = new Color(0.8f, 0.9f, 0.8f);

        string tipText = LanguageSelectionUI.Localize(
            "💡 Rating Score: Online/Friendly matches award 100% Score | Offline matches award 25% Practice Score.",
            ar: "💡 نقاط التصنيف: مباريات الأونلاين والودية تمنح 100% نقاط تصنيف | الأوفلاين يمنح 25% نقاط تدريبية.",
            fa: "💡 امتیاز رتبه‌بندی: بازی‌های آنلاین ۱۰۰٪ امتیاز کامل | آفلاین ۲۵٪ امتیاز تمرینی دارند."
        );
        GUI.Label(new Rect(x + 12f, y + 8f, w - 190f, 26f), tipText, tipStyle);

        // زر فتح نافذة الأصدقاء مباشرة
        string friendsHubLabel = LanguageSelectionUI.Localize("👥 Friends Hub", ar: "👥 مركز الأصدقاء");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + w - 170f, y + 5f, 160f, 32f), friendsHubLabel, EnchantedNatureGUI.AntiqueGoldBright, 12, true))
        {
            CloseLeaderboard();
            FriendsUI.OpenFriends();
        }
    }
}
