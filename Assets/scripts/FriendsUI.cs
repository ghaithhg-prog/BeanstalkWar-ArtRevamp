using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// واجهة مركز الأصدقاء (Friends Hub UI):
/// نافذة أنيقة بنمط خشب الغابة العتيقة تتيح تصفح الأصدقاء وحالاتهم،
/// قبول أو رفض طلبات الصداقة، والولوج المباشر لغرف الأصدقاء بضغطة زر واحدة.
/// </summary>
public class FriendsUI : MonoBehaviour
{
    public static FriendsUI Instance { get; private set; }
    public static bool IsFriendsOpen { get; private set; } = false;

    private int activeTab = 0; // 0 = Friends List, 1 = Requests, 2 = Add Friend
    private Vector2 scrollPos = Vector2.zero;
    private string addFriendInput = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("FriendsUI");
            obj.AddComponent<FriendsUI>();
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

    public static void OpenFriends()
    {
        IsFriendsOpen = true;
    }

    public static void CloseFriends()
    {
        IsFriendsOpen = false;
    }

    public static void ToggleFriends()
    {
        IsFriendsOpen = !IsFriendsOpen;
    }

    void Update()
    {
        if (IsFriendsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseFriends();
        }
    }

    void OnGUI()
    {
        if (!IsFriendsOpen) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) return;

        Font activeFont = EnchantedNatureGUI.GetActiveGameFont();
        if (activeFont != null) GUI.skin.font = activeFont;

        // خلفية عاتمة ناعمة لعزل النافذة
        GUI.color = new Color(0.03f, 0.07f, 0.04f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float winW = Mathf.Clamp(Screen.width * 0.82f, 540f, 780f);
        float winH = Mathf.Clamp(Screen.height * 0.84f, 440f, 620f);
        float winX = (Screen.width - winW) * 0.5f;
        float winY = (Screen.height - winH) * 0.5f;

        Rect winRect = new Rect(winX, winY, winW, winH);

        // لوحة خشبية عتيقة بإطار ذهبي منقوش
        EnchantedNatureGUI.DrawWoodPlate(winRect, new Color(0.07f, 0.12f, 0.08f, 0.98f), new Color(0.88f, 0.74f, 0.32f, 0.95f));
        EnchantedNatureGUI.DrawCornerFiligree(new Rect(winX + 4f, winY + 4f, winW - 8f, winH - 8f), new Color(0.95f, 0.82f, 0.35f, 0.85f), 20f);

        // زر الإغلاق العلوي
        if (GUI.Button(new Rect(winX + winW - 42f, winY + 12f, 32f, 32f), "✕"))
        {
            CloseFriends();
            return;
        }

        // عنوان النافذة
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = EnchantedNatureGUI.AntiqueGoldBright;

        string titleText = LanguageSelectionUI.Localize(
            "👥 SACRED FOREST FELLOWSHIP (FRIENDS)",
            ar: "👥 رِفَاقُ الغَابَةِ المُقَدَّسَةِ (الأَصْدِقَاء)",
            fa: "👥 یاران جنگل مقدس (دوستان)",
            ru: "👥 ТОВАРИЩИ ЛЕСА (ДРУЗЬЯ)",
            de: "👥 GEFÄHRTEN DES WALDES (FREUNDE)",
            fr: "👥 COMPAGNONS DE LA FORÊT (AMIS)",
            es: "👥 COMPAÑEROS DEL BOSQUE (AMIGOS)"
        );
        GUI.Label(new Rect(winX, winY + 14f, winW, 28f), titleText, titleStyle);

        // شريط التبويبات الثلاثة
        DrawTabs(winX + 24f, winY + 50f, winW - 48f);

        // محتوى التبويب النشط
        Rect contentRect = new Rect(winX + 20f, winY + 95f, winW - 40f, winH - 110f);
        switch (activeTab)
        {
            case 0:
                DrawFriendsListTab(contentRect);
                break;
            case 1:
                DrawRequestsTab(contentRect);
                break;
            case 2:
                DrawAddFriendTab(contentRect);
                break;
        }
    }

    void DrawTabs(float x, float y, float w)
    {
        float tabW = w / 3f;
        float tabH = 34f;

        int reqCount = (FriendsManager.Instance != null) ? FriendsManager.Instance.GetPendingRequests().Count : 0;
        string reqBadge = reqCount > 0 ? $" ({reqCount})" : "";

        string tab0 = LanguageSelectionUI.Localize("👥 Friends List", ar: "👥 قائمة الأصدقاء");
        string tab1 = LanguageSelectionUI.Localize($"📬 Requests{reqBadge}", ar: $"📬 طلبات الصداقة{reqBadge}");
        string tab2 = LanguageSelectionUI.Localize("🔍 Add Friend", ar: "🔍 إضافة صديق");

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x, y, tabW - 4f, tabH), tab0, activeTab == 0 ? EnchantedNatureGUI.EmeraldAccent : EnchantedNatureGUI.AntiqueGoldMid, 12, activeTab == 0))
        {
            activeTab = 0;
            scrollPos = Vector2.zero;
        }

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + tabW, y, tabW - 4f, tabH), tab1, activeTab == 1 ? EnchantedNatureGUI.EmeraldAccent : EnchantedNatureGUI.AntiqueGoldMid, 12, activeTab == 1))
        {
            activeTab = 1;
            scrollPos = Vector2.zero;
        }

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + tabW * 2f, y, tabW, tabH), tab2, activeTab == 2 ? EnchantedNatureGUI.EmeraldAccent : EnchantedNatureGUI.AntiqueGoldMid, 12, activeTab == 2))
        {
            activeTab = 2;
            scrollPos = Vector2.zero;
        }
    }

    void DrawFriendsListTab(Rect rect)
    {
        List<FriendsManager.FriendEntry> friends = FriendsManager.Instance != null ? FriendsManager.Instance.GetFriends() : new List<FriendsManager.FriendEntry>();

        if (friends.Count == 0)
        {
            GUIStyle emptyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            emptyStyle.normal.textColor = new Color(0.75f, 0.85f, 0.75f);
            GUI.Label(rect, LanguageSelectionUI.Localize("🍃 No friends yet. Add players from the leaderboard or invite them by name!", ar: "🍃 لا يوجد أصدقاء بعد. أضف أبطالاً من لائحة المتصدرين أو ابحث عنهم بالاسم!"), emptyStyle);
            return;
        }

        float rowH = 56f;
        float totalH = friends.Count * (rowH + 6f);
        Rect viewRect = new Rect(0, 0, rect.width - 24f, Mathf.Max(totalH, rect.height));

        scrollPos = GUI.BeginScrollView(rect, scrollPos, viewRect);

        GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        nameStyle.normal.textColor = Color.white;

        GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11
        };

        for (int i = 0; i < friends.Count; i++)
        {
            var friend = friends[i];
            float rowY = i * (rowH + 6f);
            Rect rowRect = new Rect(0, rowY, viewRect.width, rowH);

            var tier = PlayerProfile.GetRankTier(friend.score);
            EnchantedNatureGUI.DrawWoodPlate(rowRect, new Color(0.10f, 0.16f, 0.11f, 0.94f), new Color(0.35f, 0.5f, 0.35f, 0.75f));

            // أيقونة الرتبة
            GUIStyle badgeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
            GUI.Label(new Rect(8f, rowY + 12f, 36f, 32f), tier.badgeIcon, badgeStyle);

            // الاسم والرتبة
            GUI.Label(new Rect(48f, rowY + 6f, 220f, 22f), friend.playerName, nameStyle);
            GUI.Label(new Rect(48f, rowY + 28f, 240f, 20f), $"{tier.GetLocalizedTitle()} • {friend.score} Pts", statusStyle);

            // حالة الاتصال
            string statusStr;
            Color statusCol;
            bool isHostingOpenRoom = (friend.status == "HostingRoom" && friend.isOpenToFriends && !string.IsNullOrEmpty(friend.hostingRoomCode));

            if (isHostingOpenRoom)
            {
                statusStr = LanguageSelectionUI.Localize($"👑 Hosting: {friend.hostingRoomCode}", ar: $"👑 فاتح غرفة: {friend.hostingRoomCode}");
                statusCol = new Color(0.3f, 0.95f, 0.45f);
            }
            else if (friend.status == "InMatch")
            {
                statusStr = LanguageSelectionUI.Localize("⚔️ In Match", ar: "⚔️ في معركة حامية");
                statusCol = new Color(1f, 0.65f, 0.25f);
            }
            else
            {
                statusStr = LanguageSelectionUI.Localize("🟢 Online", ar: "🟢 متصل الآن");
                statusCol = new Color(0.4f, 0.85f, 1f);
            }

            GUIStyle liveStatusStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 11, fontStyle = FontStyle.Bold };
            liveStatusStyle.normal.textColor = statusCol;
            GUI.Label(new Rect(viewRect.width - 250f, rowY + 16f, 110f, 24f), statusStr, liveStatusStyle);

            // زر الانضمام للغرفة المفتوحة للأصدقاء
            if (isHostingOpenRoom)
            {
                string joinLabel = LanguageSelectionUI.Localize("🚀 Join Room", ar: "🚀 انضمام");
                if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(viewRect.width - 130f, rowY + 12f, 85f, 32f), joinLabel, EnchantedNatureGUI.EmeraldAccent, 11, true))
                {
                    JoinFriendRoom(friend);
                }
            }

            // زر الحذف
            GUI.color = new Color(0.7f, 0.2f, 0.2f, 0.75f);
            if (GUI.Button(new Rect(viewRect.width - 38f, rowY + 14f, 28f, 28f), "✕"))
            {
                FriendsManager.Instance?.RemoveFriend(friend.playerName);
            }
            GUI.color = Color.white;
        }

        GUI.EndScrollView();
    }

    void DrawRequestsTab(Rect rect)
    {
        List<string> requests = FriendsManager.Instance != null ? FriendsManager.Instance.GetPendingRequests() : new List<string>();

        if (requests.Count == 0)
        {
            GUIStyle emptyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            emptyStyle.normal.textColor = new Color(0.75f, 0.85f, 0.75f);
            GUI.Label(rect, LanguageSelectionUI.Localize("📬 No pending friend requests at the moment.", ar: "📬 لا توجد طلبات صداقة معلقة حالياً."), emptyStyle);
            return;
        }

        float rowH = 50f;
        float totalH = requests.Count * (rowH + 6f);
        Rect viewRect = new Rect(0, 0, rect.width - 24f, Mathf.Max(totalH, rect.height));

        scrollPos = GUI.BeginScrollView(rect, scrollPos, viewRect);

        GUIStyle reqNameStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        reqNameStyle.normal.textColor = Color.white;

        for (int i = 0; i < requests.Count; i++)
        {
            string reqName = requests[i];
            float rowY = i * (rowH + 6f);
            Rect rowRect = new Rect(0, rowY, viewRect.width, rowH);

            EnchantedNatureGUI.DrawWoodPlate(rowRect, new Color(0.12f, 0.16f, 0.12f, 0.94f), new Color(0.5f, 0.65f, 0.45f, 0.75f));

            GUI.Label(new Rect(18f, rowY + 14f, 260f, 24f), $"⚔️ {reqName}", reqNameStyle);

            // زر القبول
            string acceptLabel = LanguageSelectionUI.Localize("✓ Accept", ar: "✓ قبول");
            if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(viewRect.width - 170f, rowY + 10f, 75f, 30f), acceptLabel, EnchantedNatureGUI.EmeraldAccent, 11, true))
            {
                FriendsManager.Instance?.AcceptFriendRequest(reqName);
            }

            // زر الرفض
            string declineLabel = LanguageSelectionUI.Localize("✕ Decline", ar: "✕ رفض");
            if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(viewRect.width - 85f, rowY + 10f, 75f, 30f), declineLabel, new Color(0.85f, 0.25f, 0.25f), 11, false))
            {
                FriendsManager.Instance?.DeclineFriendRequest(reqName);
            }
        }

        GUI.EndScrollView();
    }

    void DrawAddFriendTab(Rect rect)
    {
        float boxW = Mathf.Min(480f, rect.width - 20f);
        float boxX = rect.x + (rect.width - boxW) * 0.5f;
        float boxY = rect.y + 30f;

        GUIStyle descStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            wordWrap = true
        };
        descStyle.normal.textColor = new Color(0.85f, 0.95f, 0.85f);

        string desc = LanguageSelectionUI.Localize(
            "Enter the warrior name of your companion to send them an official invitation to your Sacred Forest fellowship:",
            ar: "أدخل اسم البطل أو الرفيق لإرسال دعوة انضمام رسمية لرفاق الغابة المقدسة:",
            fa: "نام جنگجوی دوست خود را وارد کنید تا دعوت‌نامه بفرستید:",
            ru: "Введите имя воина, чтобы отправить ему приглашение в друзья:",
            de: "Gib den Namen des Kriegers ein, um ihm eine Einladung zu senden:",
            fr: "Entrez le nom du guerrier pour lui envoyer une invitation :",
            es: "Introduce el nombre del guerrero para enviarle una solicitud:"
        );
        GUI.Label(new Rect(boxX, boxY, boxW, 50f), desc, descStyle);

        // حقل إدخال الاسم
        addFriendInput = GUI.TextField(new Rect(boxX + 20f, boxY + 65f, boxW - 40f, 36f), addFriendInput);

        string sendLabel = LanguageSelectionUI.Localize("✉️ SEND FRIEND REQUEST", ar: "✉️ إرسال طلب الصداقة");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(boxX + 40f, boxY + 120f, boxW - 80f, 40f), sendLabel, EnchantedNatureGUI.AntiqueGoldBright, 13, true))
        {
            if (!string.IsNullOrWhiteSpace(addFriendInput))
            {
                FriendsManager.Instance?.SendFriendRequest(addFriendInput);
                addFriendInput = "";
            }
            else
            {
                PlayerProgressionAndShopUI.Instance?.ShowNotification("⚠️ Please enter a player name first!");
            }
        }
    }

    void JoinFriendRoom(FriendsManager.FriendEntry friend)
    {
        CloseFriends();

        if (FriendlyLobbyUI.Instance != null)
        {
            FriendlyLobbyUI.Instance.JoinFriendRoom(friend.hostingRoomIP, friend.hostingRoomCode);
        }
        else
        {
            PlayerProgressionAndShopUI.Instance?.ShowNotification($"Connecting to {friend.playerName}'s room: {friend.hostingRoomCode}...");
        }
    }
}
