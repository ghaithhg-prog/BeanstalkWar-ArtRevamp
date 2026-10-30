using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// InGameChatUI provides:
/// 1. Sleek collapsible Messenger-style Team Chat during matches with 13-language Tactical Quick Pings.
/// 2. Floating HUD Toasts that automatically fade in/out so players never miss tactical calls during combat.
/// 3. In-Lobby Room Chat embedded inside Friendly Rooms to chat with friends before match begins.
/// 4. Complete Mute / Silence mode: allows players to completely silence & hide chat and notifications.
/// 5. Top-ribbon mini toggle button safely isolated from battle action and mobile touch controls.
/// </summary>
public class InGameChatUI : MonoBehaviour
{
    public static InGameChatUI Instance { get; private set; }

    public static bool IsChatOpen { get; set; } = false;
    public static bool IsTyping { get; private set; } = false;
    public static bool IsChatMuted { get; private set; } = false;

    private string currentInput = "";
    private Vector2 chatScrollPos = Vector2.zero;
    private int unreadMessagesCount = 0;

    // Floating HUD Toast structure
    private class ChatToast
    {
        public string text;
        public Color color;
        public float expireTime;
    }

    private readonly List<ChatToast> activeToasts = new List<ChatToast>();
    private const float ToastDuration = 5.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            IsChatMuted = PlayerPrefs.GetInt("Beanstalk_Chat_Muted", 0) == 1;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (InGameChatManager.Instance != null)
        {
            InGameChatManager.Instance.OnMessageReceived += HandleIncomingMessage;
        }
    }

    void OnDestroy()
    {
        if (InGameChatManager.Instance != null)
        {
            InGameChatManager.Instance.OnMessageReceived -= HandleIncomingMessage;
        }
    }

    private void HandleIncomingMessage(InGameChatManager.ChatMessage msg)
    {
        // When chat is muted, completely suppress toasts and unread counter!
        if (IsChatMuted) return;

        if (!IsChatOpen)
        {
            unreadMessagesCount++;
        }

        // Add toast notification for in-game matches
        bool isPlaying = GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying();
        if (isPlaying && msg.channel != InGameChatManager.ChatChannel.RoomLobby)
        {
            Color teamCol = (msg.channel == InGameChatManager.ChatChannel.TeamProtector)
                ? new Color(0.35f, 0.90f, 1f)
                : new Color(1f, 0.45f, 0.25f);

            string botTag = msg.isBot ? " [BOT]" : "";
            string toastText = $"{msg.icon} {msg.senderName}{botTag}: {msg.messageText}";

            activeToasts.Add(new ChatToast
            {
                text = toastText,
                color = teamCol,
                expireTime = Time.time + ToastDuration
            });

            if (activeToasts.Count > 4)
            {
                activeToasts.RemoveAt(0);
            }
        }

        // Auto-scroll chat to latest message
        chatScrollPos.y = float.MaxValue;
    }

    void Update()
    {
        // Expire old toasts
        for (int i = activeToasts.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeToasts[i].expireTime)
            {
                activeToasts.RemoveAt(i);
            }
        }

        // Toggle chat with PC Hotkeys: Enter, C, or T
        bool isPlaying = GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying();
        if (isPlaying && !MatchEndStatsUI.IsGameOverActive && !InGamePauseMenuUI.IsPauseMenuOpen && !LanguageSelectionUI.IsLanguageModalOpen)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!IsChatOpen)
                {
                    OpenChat();
                }
                else if (string.IsNullOrEmpty(currentInput.Trim()))
                {
                    CloseChat();
                }
            }
            else if (Input.GetKeyDown(KeyCode.C) && !IsTyping)
            {
                if (IsChatOpen) CloseChat();
                else OpenChat();
            }
        }

        // Close on ESC
        if (IsChatOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }
    }

    public static void OpenChat()
    {
        if (IsChatMuted)
        {
            SetChatMuted(false);
        }

        IsChatOpen = true;
        if (Instance != null)
        {
            Instance.unreadMessagesCount = 0;
            Instance.chatScrollPos.y = float.MaxValue;
        }
    }

    public static void CloseChat()
    {
        IsChatOpen = false;
        IsTyping = false;
        GUI.FocusControl(null);
    }

    public static void ToggleChatMute()
    {
        SetChatMuted(!IsChatMuted);
    }

    public static void SetChatMuted(bool muted)
    {
        IsChatMuted = muted;
        PlayerPrefs.SetInt("Beanstalk_Chat_Muted", IsChatMuted ? 1 : 0);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.activeToasts.Clear();
            Instance.unreadMessagesCount = 0;
            if (IsChatMuted)
            {
                IsChatOpen = false;
                IsTyping = false;
                GUI.FocusControl(null);
            }
        }

        string toastMsg = IsChatMuted
            ? LanguageSelectionUI.Localize(
                "🔕 Team chat muted & hidden. Click 🔕 at top or press [C] to re-open.",
                ar: "🔕 تم كتم وإخفاء المحادثة! اضغط على 🔕 في الأعلى أو ضغط [C] لإعادتها.",
                fa: "🔕 چت تیم بی‌صدا شد! برای بازگشت روی 🔕 بالا کلیک کنید یا [C] بزنید.",
                ru: "🔕 Чат заглушен! Нажмите 🔕 сверху или [C], чтобы включить.",
                fr: "🔕 Chat d'équipe masqué ! Cliquez sur 🔕 en haut ou [C] pour rouvrir.",
                es: "🔕 Chat silenciado! Pulsa 🔕 arriba o [C] para reactivarlo.")
            : LanguageSelectionUI.Localize(
                "💬 Team chat activated!",
                ar: "💬 تم تفعيل محادثة الفريق مجددًا!",
                fa: "💬 چت تیم فعال شد!",
                ru: "💬 Чат команды включен!",
                fr: "💬 Chat d'équipe activé !",
                es: "💬 ¡Chat de equipo activado!");

        PlayerProgressionAndShopUI.Instance?.ShowNotification(toastMsg);
    }

    void OnGUI()
    {
        // Completely hidden on Game Over, Title Screen, or Language Modal
        if (MatchEndStatsUI.IsGameOverActive) return;
        if (PlayModeUI.isTitleScreenActive) return;
        if (LanguageSelectionUI.IsLanguageModalOpen) return;

        Font gameFont = EnchantedNatureGUI.GetActiveGameFont();
        if (gameFont != null) GUI.skin.font = gameFont;

        bool isPlaying = GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying();

        // 1. In-Match Team Chat & Controls
        if (isPlaying)
        {
            // Top Mini Chat Toggle (Safe from battle action & mobile joystick)
            GUI.depth = -100;
            DrawTopMiniChatToggle();

            if (IsChatOpen)
            {
                GUI.depth = -150;
                DrawInMatchTeamChatWindow();
            }
            else if (!IsChatMuted)
            {
                // Active toasts and bottom button only when NOT muted
                DrawActiveToasts();

                // On desktop when not muted, also offer bottom floating button
                if (!MobileUI.IsMobileActive)
                {
                    GUI.depth = -100;
                    DrawFloatingMessengerButton();
                }
            }
        }
    }

    // ==========================================
    // 1. Floating HUD Message Toasts
    // ==========================================

    private void DrawActiveToasts()
    {
        if (IsChatMuted || IsChatOpen || activeToasts.Count == 0) return;

        GUI.depth = -80;
        float toastW = Mathf.Clamp(Screen.width * 0.45f, 260f, 440f);
        float toastH = 26f;
        float startX = 16f;
        float startY = Screen.height - 230f;

        GUIStyle toastStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        for (int i = 0; i < activeToasts.Count; i++)
        {
            ChatToast toast = activeToasts[i];
            float remainingTime = toast.expireTime - Time.time;
            float alpha = Mathf.Clamp01(remainingTime / 1.2f);

            Rect toastRect = new Rect(startX, startY + (i * (toastH + 4f)), toastW, toastH);

            // Subtle dark moss glass pill
            GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.75f * alpha);
            GUI.DrawTexture(toastRect, Texture2D.whiteTexture);

            // Colored left indicator edge
            GUI.color = new Color(toast.color.r, toast.color.g, toast.color.b, 0.90f * alpha);
            GUI.DrawTexture(new Rect(toastRect.x, toastRect.y, 3f, toastH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            toastStyle.normal.textColor = new Color(toast.color.r, toast.color.g, toast.color.b, alpha);
            GUI.Label(new Rect(toastRect.x + 8f, toastRect.y, toastW - 12f, toastH), toast.text, toastStyle);

            // Click toast to open chat
            if (Event.current.type == EventType.MouseDown && toastRect.Contains(Event.current.mousePosition))
            {
                OpenChat();
                Event.current.Use();
            }
        }
    }

    // ==========================================
    // 2. Top-Ribbon Mini Chat Toggle (Safe from Joystick)
    // ==========================================

    private void DrawTopMiniChatToggle()
    {
        // Placed in top ribbon next to [⚙️ PAUSE (ESC)] button (x=16, w=145 -> x=169)
        float pauseW = 145f;
        float x = 16f + pauseW + 8f;
        float y = 14f;
        float h = 34f;
        float w = IsChatMuted ? 44f : (unreadMessagesCount > 0 ? 56f : 44f);

        string label = IsChatMuted ? "🔕" : ((unreadMessagesCount > 0) ? $"💬 {unreadMessagesCount}" : "💬");
        Color accent = IsChatMuted
            ? new Color(1f, 0.40f, 0.30f)
            : ((unreadMessagesCount > 0) ? new Color(0.2f, 0.95f, 0.45f) : EnchantedNatureGUI.AntiqueGoldBright);

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x, y, w, h), label, accent, 11, IsChatMuted))
        {
            if (IsChatOpen) CloseChat();
            else OpenChat();
        }
    }

    // ==========================================
    // 3. Floating Messenger Button (Desktop)
    // ==========================================

    private void DrawFloatingMessengerButton()
    {
        if (IsChatMuted) return;

        float btnW = 54f;
        float btnH = 40f;
        float x = 16f;
        float y = Screen.height - 130f;

        Rect btnRect = new Rect(x, y, btnW, btnH);

        string chatBtnLabel = (unreadMessagesCount > 0) ? $"💬 {unreadMessagesCount}" : "💬";
        Color accentColor = (unreadMessagesCount > 0) ? new Color(1f, 0.3f, 0.2f) : EnchantedNatureGUI.EmeraldAccent;

        if (EnchantedNatureGUI.DrawAncientGoldButton(btnRect, chatBtnLabel, accentColor, 12, unreadMessagesCount > 0))
        {
            OpenChat();
        }

        // Desktop keyboard shortcut hint
        if (Screen.width > 768)
        {
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hintStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 0.70f);
            GUI.Label(new Rect(x, y + btnH + 1f, btnW, 14f), "[C/Enter]", hintStyle);
        }
    }

    // ==========================================
    // 4. Expanded Messenger Team Chat Window
    // ==========================================

    private void DrawInMatchTeamChatWindow()
    {
        float winW = Mathf.Clamp(Screen.width * 0.40f, 320f, 430f);
        float winH = Mathf.Clamp(Screen.height * 0.52f, 330f, 440f);
        float x = 16f;
        float y = Screen.height - winH - 25f;

        Rect mainRect = new Rect(x, y, winW, winH);

        InGameChatManager.ChatChannel myChannel = (InGameChatManager.Instance != null)
            ? InGameChatManager.Instance.GetLocalPlayerChannel()
            : InGameChatManager.ChatChannel.TeamProtector;

        bool isProt = (myChannel == InGameChatManager.ChatChannel.TeamProtector);
        Color teamBorderCol = isProt ? new Color(0.25f, 0.85f, 1f) : new Color(1f, 0.42f, 0.20f);

        // Ancient Plaque Frame
        EnchantedNatureGUI.DrawLeafPlaque(mainRect, new Color(0.06f, 0.11f, 0.08f, 0.96f), teamBorderCol);

        // Header Title
        string teamTitle = isProt
            ? LanguageSelectionUI.Localize(
                "🛡️ PROTECTORS TACTICAL CHAT",
                ar: "🛡️ محادثة فريق الحماة (تكتيكية خاصة)",
                fa: "🛡️ گفتگوی تاکتیکی تیم محافظان",
                ru: "🛡️ ЧАТ ЗАЩИТНИКОВ (КОМАНДНЫЙ)",
                fr: "🛡️ CHAT TACTIQUE DES PROTECTEURS",
                es: "🛡️ CHAT TÁCTICO DE PROTECTORES",
                de: "🛡️ TAKTISCHER CHAT DER BESCHÜTZER",
                it: "🛡️ CHAT TATTICA DEI PROTETTORI",
                tr: "🛡️ KORUYUCULAR TAKTİK SOHBETİ",
                pt: "🛡️ CHAT TÁTICO DOS PROTETORES",
                hi: "🛡️ संरक्षक सामरिक चैट",
                zh: "🛡️ 守护者战术队伍通讯",
                ja: "🛡️ 守護者チーム戦術チャット")
            : LanguageSelectionUI.Localize(
                "⚔️ DESTROYERS TACTICAL CHAT",
                ar: "⚔️ محادثة فريق المدمرين (تكتيكية خاصة)",
                fa: "⚔️ گفتگوی تاکتیکی تیم نابودگران",
                ru: "⚔️ ЧАТ РАЗРУШИТЕЛЕЙ (КОМАНДНЫЙ)",
                fr: "⚔️ CHAT TACTIQUE DES DESTRUCTEURS",
                es: "⚔️ CHAT TÁCTICO DE DESTRUCTORES",
                de: "⚔️ TAKTISCHER CHAT DER ZERSTÖRER",
                it: "⚔️ CHAT TATTICA DEI DISTRUTTORI",
                tr: "⚔️ YOK EDİCİLER TAKTİK SOHBETİ",
                pt: "⚔️ CHAT TÁTICO DOS DESTRUIDORES",
                hi: "⚔️ विनाशक सामरिक चैट",
                zh: "⚔️ 破坏者战术队伍通讯",
                ja: "⚔️ 破壊者チーム戦術チャット");

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        headerStyle.normal.textColor = teamBorderCol;
        GUI.Label(new Rect(x + 12f, y + 8f, winW - 120f, 22f), teamTitle, headerStyle);

        // Subtitle: Private Team Notice
        GUIStyle subNoticeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleLeft
        };
        subNoticeStyle.normal.textColor = new Color(0.70f, 0.85f, 0.75f, 0.85f);
        string subNotice = LanguageSelectionUI.Localize(
            "🔒 Encrypted team channel. Opponents cannot read this.",
            ar: "🔒 قناة خاصة مشفرة. لا يستطيع الخصوم قراءتها.",
            fa: "🔒 کانال اختصاصی تیم. حریفان قادر به خواندن نیستند.",
            ru: "🔒 Закрытый канал команды. Враги не видят сообщений.",
            fr: "🔒 Canal chiffré. Vos adversaires ne peuvent pas lire.",
            es: "🔒 Canal privado. Los rivales no pueden ver tus mensajes.",
            de: "🔒 Verschlüsselter Teamkanal. Gegner können nicht mitlesen.",
            it: "🔒 Canale sicuro di squadra. I rivali non possono leggere.",
            tr: "🔒 Şifreli takım kanalı. Rakipler göremez.",
            pt: "🔒 Canal seguro da equipe. Adversários não têm acesso.",
            hi: "🔒 सुरक्षित टीम चैनल। विरोधी इसे नहीं पढ़ सकते।",
            zh: "🔒 队伍加密通讯，敌方无法窥视。",
            ja: "🔒 チーム暗号化チャンネル。敵には見えません。");
        GUI.Label(new Rect(x + 12f, y + 27f, winW - 120f, 16f), subNotice, subNoticeStyle);

        // Mute / Silence Button (🔕 كتم الشات)
        float muteW = 66f;
        string muteTip = LanguageSelectionUI.Localize("🔕 Mute", ar: "🔕 كتم", fa: "🔕 بی‌صدا", ru: "🔕 Загл.", fr: "🔕 Muet", es: "🔕 Silenc.");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + winW - 36f - muteW, y + 7f, muteW, 25f), muteTip, new Color(1f, 0.65f, 0.35f), 10, false))
        {
            SetChatMuted(true);
            return;
        }

        // Close (✕) Button
        GUI.color = new Color(0.9f, 0.3f, 0.3f);
        if (GUI.Button(new Rect(x + winW - 30f, y + 7f, 24f, 25f), "✕"))
        {
            CloseChat();
            return;
        }
        GUI.color = Color.white;

        // Divider
        EnchantedNatureGUI.DrawCrispBorder(new Rect(x + 10f, y + 45f, winW - 20f, 1f), new Color(0.3f, 0.5f, 0.35f, 0.4f), 1f);

        // ==========================================
        // Scrollable Messages Log
        // ==========================================
        float logW = winW - 20f;
        float logH = winH - 170f;
        Rect logViewRect = new Rect(x + 10f, y + 48f, logW, logH);

        List<InGameChatManager.ChatMessage> messages = (InGameChatManager.Instance != null)
            ? InGameChatManager.Instance.GetMessagesForChannel(myChannel)
            : new List<InGameChatManager.ChatMessage>();

        float innerContentH = Mathf.Max(logH, messages.Count * 24f + 10f);
        Rect innerRect = new Rect(0, 0, logW - 16f, innerContentH);

        chatScrollPos = GUI.BeginScrollView(logViewRect, chatScrollPos, innerRect, false, true);

        float itemY = 4f;
        GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };

        for (int i = 0; i < messages.Count; i++)
        {
            InGameChatManager.ChatMessage m = messages[i];
            bool isMe = string.Equals(m.senderName, PlayerProfile.GetSavedPlayerName(), System.StringComparison.OrdinalIgnoreCase);

            Color nameCol = isMe
                ? new Color(1f, 0.88f, 0.35f) // Gold for local player
                : (m.isBot ? new Color(0.4f, 0.95f, 0.65f) : (isProt ? new Color(0.45f, 0.85f, 1f) : new Color(1f, 0.55f, 0.35f)));

            string botTag = m.isBot ? " 🤖" : "";
            string timeStr = System.DateTime.Now.ToString("HH:mm");
            string lineText = $"<color=#{ColorUtility.ToHtmlStringRGBA(new Color(0.6f, 0.6f, 0.6f, 0.7f))}>[{timeStr}]</color> <b><color=#{ColorUtility.ToHtmlStringRGBA(nameCol)}>{m.senderName}{botTag}:</color></b> {m.icon} {m.messageText}";

            msgStyle.richText = true;
            GUI.Label(new Rect(4f, itemY, innerRect.width - 8f, 22f), lineText, msgStyle);
            itemY += 22f;
        }

        GUI.EndScrollView();

        // ==========================================
        // Quick Tactical Pings Ribbon (2 rows of 4 buttons)
        // ==========================================
        float pingsY = y + winH - 116f;
        DrawQuickPingsGrid(x + 10f, pingsY, winW - 20f, false);

        // ==========================================
        // Text Input & Send Button
        // ==========================================
        float inputY = y + winH - 42f;
        float sendBtnW = 68f;
        float inputFieldW = winW - sendBtnW - 28f;

        GUI.SetNextControlName("InGameChatInputField");
        currentInput = GUI.TextField(new Rect(x + 10f, inputY, inputFieldW, 30f), currentInput, 90);

        // Detect if user is currently typing in input field
        IsTyping = (GUI.GetNameOfFocusedControl() == "InGameChatInputField");

        // Send Button
        string sendLabel = LanguageSelectionUI.Localize("➤ Send", ar: "➤ إرسال", fa: "➤ ارسال", ru: "➤ Отпр.", fr: "➤ Envoyer", es: "➤ Enviar");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x + 16f + inputFieldW, inputY, sendBtnW, 30f), sendLabel, EnchantedNatureGUI.EmeraldAccent, 11, true))
        {
            SubmitMessage(false);
        }

        // Send on Enter key if input is focused
        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        {
            if (IsTyping && !string.IsNullOrWhiteSpace(currentInput))
            {
                SubmitMessage(false);
                Event.current.Use();
            }
        }
    }

    private void SubmitMessage(bool inLobby)
    {
        if (string.IsNullOrWhiteSpace(currentInput)) return;

        if (InGameChatManager.Instance != null)
        {
            InGameChatManager.Instance.SendTextMessage(currentInput, inLobby);
        }

        currentInput = "";
        chatScrollPos.y = float.MaxValue;
    }

    // ==========================================
    // 5. Quick Tactical Pings Grid (8 Pings)
    // ==========================================

    private void DrawQuickPingsGrid(float startX, float startY, float totalWidth, bool inLobby)
    {
        float btnW = (totalWidth - 9f) / 4f;
        float btnH = 28f;

        // Row 1
        DrawPingButton(new Rect(startX, startY, btnW, btnH), InGameChatManager.QuickPingType.DefendTree, inLobby);
        DrawPingButton(new Rect(startX + btnW + 3f, startY, btnW, btnH), InGameChatManager.QuickPingType.AttackBoss, inLobby);
        DrawPingButton(new Rect(startX + (btnW + 3f) * 2, startY, btnW, btnH), InGameChatManager.QuickPingType.EscortCrystal, inLobby);
        DrawPingButton(new Rect(startX + (btnW + 3f) * 3, startY, btnW, btnH), InGameChatManager.QuickPingType.Charge, inLobby);

        // Row 2
        float row2Y = startY + btnH + 3f;
        DrawPingButton(new Rect(startX, row2Y, btnW, btnH), InGameChatManager.QuickPingType.NeedBackup, inLobby);
        DrawPingButton(new Rect(startX + btnW + 3f, row2Y, btnW, btnH), InGameChatManager.QuickPingType.Regroup, inLobby);
        DrawPingButton(new Rect(startX + (btnW + 3f) * 2, row2Y, btnW, btnH), InGameChatManager.QuickPingType.WellPlayed, inLobby);
        DrawPingButton(new Rect(startX + (btnW + 3f) * 3, row2Y, btnW, btnH), InGameChatManager.QuickPingType.ThankYou, inLobby);
    }

    private void DrawPingButton(Rect rect, InGameChatManager.QuickPingType ping, bool inLobby)
    {
        string icon = InGameChatManager.GetQuickPingIcon(ping);
        string shortLabel = GetShortPingLabel(ping);
        string displayLabel = $"{icon} {shortLabel}";

        if (EnchantedNatureGUI.DrawAncientGoldButton(rect, displayLabel, EnchantedNatureGUI.AntiqueGoldBright, 10, false))
        {
            if (InGameChatManager.Instance != null)
            {
                InGameChatManager.Instance.SendQuickPing(ping, inLobby);
                chatScrollPos.y = float.MaxValue;
            }
        }
    }

    private string GetShortPingLabel(InGameChatManager.QuickPingType ping)
    {
        switch (ping)
        {
            case InGameChatManager.QuickPingType.DefendTree:
                return LanguageSelectionUI.Localize("Defend", ar: "الشجرة", fa: "دفاع", ru: "Защита", fr: "Défense", es: "Defensa");
            case InGameChatManager.QuickPingType.AttackBoss:
                return LanguageSelectionUI.Localize("Boss", ar: "الزعيم", fa: "غول", ru: "Босс", fr: "Boss", es: "Jefe");
            case InGameChatManager.QuickPingType.EscortCrystal:
                return LanguageSelectionUI.Localize("Crystal", ar: "الكريستالة", fa: "کریستال", ru: "Кристалл", fr: "Cristal", es: "Cristal");
            case InGameChatManager.QuickPingType.Charge:
                return LanguageSelectionUI.Localize("Charge", ar: "هجوم!", fa: "حمله!", ru: "В атаку!", fr: "Assaut", es: "Ataque");
            case InGameChatManager.QuickPingType.NeedBackup:
                return LanguageSelectionUI.Localize("Backup", ar: "نجدة!", fa: "کمک!", ru: "Помощь", fr: "Aide", es: "Apoyo");
            case InGameChatManager.QuickPingType.Regroup:
                return LanguageSelectionUI.Localize("Regroup", ar: "تجمعوا", fa: "تجمع", ru: "Сбор", fr: "Repli", es: "Reagrupar");
            case InGameChatManager.QuickPingType.WellPlayed:
                return LanguageSelectionUI.Localize("Good Job", ar: "كفو!", fa: "عالی!", ru: "Молодцы", fr: "Bravo", es: "Bien!");
            case InGameChatManager.QuickPingType.ThankYou:
                return LanguageSelectionUI.Localize("Thanks", ar: "شكرًا", fa: "تشکر", ru: "Спасибо", fr: "Merci", es: "Gracias");
            default:
                return "";
        }
    }

    // ==========================================
    // 6. Embedded Friendly Lobby Room Chat
    // ==========================================

    public void DrawEmbeddedRoomChat(Rect rect)
    {
        // Dark Obsidian Glass Panel with Gold Border
        EnchantedNatureGUI.DrawWoodPlate(rect, new Color(0.05f, 0.08f, 0.06f, 0.94f), new Color(0.92f, 0.78f, 0.30f, 0.90f));

        // Header Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.yellow;

        string lobbyChatHeader = LanguageSelectionUI.Localize(
            "💬 ROOM CHAT (ALL PLAYERS)",
            ar: "💬 محادثة الغرفة (جميع الأصدقاء)",
            fa: "💬 گفتگوی اتاق (همه دوستان)",
            ru: "💬 ЧАТ КОМНАТЫ (ВСЕ ИГРОКИ)",
            fr: "💬 CHAT DU SALON (TOUS LES JOUEURS)",
            es: "💬 CHAT DE LA SALA (TODOS)",
            de: "💬 RAUM-CHAT (ALLE SPIELER)",
            it: "💬 CHAT DELLA STANZA (TUTTI)",
            tr: "💬 ODA SOHBETİ (HERKES)",
            pt: "💬 CHAT DA SALA (TODOS OS JOGADORES)",
            hi: "💬 कमरा चैट (सभी खिलाड़ी)",
            zh: "💬 房间大厅聊天室",
            ja: "💬 ルームロビーチャット");

        GUI.Label(new Rect(rect.x, rect.y + 6f, rect.width, 22f), lobbyChatHeader, titleStyle);

        // Scrollable Messages Log
        float logW = rect.width - 16f;
        float logH = rect.height - 130f;
        Rect logViewRect = new Rect(rect.x + 8f, rect.y + 30f, logW, logH);

        List<InGameChatManager.ChatMessage> messages = (InGameChatManager.Instance != null)
            ? InGameChatManager.Instance.GetMessagesForChannel(InGameChatManager.ChatChannel.RoomLobby)
            : new List<InGameChatManager.ChatMessage>();

        float innerH = Mathf.Max(logH, messages.Count * 22f + 10f);
        Rect innerRect = new Rect(0, 0, logW - 16f, innerH);

        chatScrollPos = GUI.BeginScrollView(logViewRect, chatScrollPos, innerRect, false, true);

        float itemY = 4f;
        GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            richText = true
        };

        for (int i = 0; i < messages.Count; i++)
        {
            InGameChatManager.ChatMessage m = messages[i];
            bool isMe = string.Equals(m.senderName, PlayerProfile.GetSavedPlayerName(), System.StringComparison.OrdinalIgnoreCase);
            Color nameCol = isMe ? new Color(1f, 0.88f, 0.35f) : new Color(0.35f, 0.85f, 1f);

            string line = $"<b><color=#{ColorUtility.ToHtmlStringRGBA(nameCol)}>{m.senderName}:</color></b> {m.icon} {m.messageText}";
            GUI.Label(new Rect(4f, itemY, innerRect.width - 8f, 20f), line, msgStyle);
            itemY += 21f;
        }

        GUI.EndScrollView();

        // 4 Quick Friendly Greetings Ribbon
        float greetY = rect.y + rect.height - 92f;
        float greetBtnW = (rect.width - 24f) / 4f;
        float greetBtnH = 26f;

        DrawLobbyGreetingBtn(new Rect(rect.x + 8f, greetY, greetBtnW, greetBtnH), InGameChatManager.LobbyGreetingType.Hello, "👋");
        DrawLobbyGreetingBtn(new Rect(rect.x + 12f + greetBtnW, greetY, greetBtnW, greetBtnH), InGameChatManager.LobbyGreetingType.Ready, "👍");
        DrawLobbyGreetingBtn(new Rect(rect.x + 16f + greetBtnW * 2, greetY, greetBtnW, greetBtnH), InGameChatManager.LobbyGreetingType.GoodLuck, "🤝");
        DrawLobbyGreetingBtn(new Rect(rect.x + 20f + greetBtnW * 3, greetY, greetBtnW, greetBtnH), InGameChatManager.LobbyGreetingType.EpicBattle, "⚔️");

        // Input Field & Send Button
        float inputY = rect.y + rect.height - 56f;
        float sendBtnW = 60f;
        float inputW = rect.width - sendBtnW - 24f;

        GUI.SetNextControlName("LobbyChatInputField");
        currentInput = GUI.TextField(new Rect(rect.x + 8f, inputY, inputW, 28f), currentInput, 90);

        IsTyping = (GUI.GetNameOfFocusedControl() == "LobbyChatInputField");

        string sendText = LanguageSelectionUI.Localize("➤", ar: "➤ إرسال", fa: "➤", ru: "➤", fr: "➤", es: "➤");
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(rect.x + 14f + inputW, inputY, sendBtnW, 28f), sendText, EnchantedNatureGUI.EmeraldAccent, 11, true))
        {
            SubmitMessage(true);
        }

        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        {
            if (IsTyping && !string.IsNullOrWhiteSpace(currentInput))
            {
                SubmitMessage(true);
                Event.current.Use();
            }
        }
    }

    private void DrawLobbyGreetingBtn(Rect rect, InGameChatManager.LobbyGreetingType greeting, string icon)
    {
        string label = $"{icon} {GetLobbyGreetingShortLabel(greeting)}";
        if (EnchantedNatureGUI.DrawAncientGoldButton(rect, label, EnchantedNatureGUI.AntiqueGoldBright, 10, false))
        {
            if (InGameChatManager.Instance != null)
            {
                InGameChatManager.Instance.SendLobbyGreeting(greeting);
                chatScrollPos.y = float.MaxValue;
            }
        }
    }

    private string GetLobbyGreetingShortLabel(InGameChatManager.LobbyGreetingType greeting)
    {
        switch (greeting)
        {
            case InGameChatManager.LobbyGreetingType.Hello:
                return LanguageSelectionUI.Localize("Hi", ar: "أهلاً", fa: "سلام", ru: "Привет", fr: "Salut", es: "Hola");
            case InGameChatManager.LobbyGreetingType.Ready:
                return LanguageSelectionUI.Localize("Ready", ar: "جاهز", fa: "آماده", ru: "Готов", fr: "Prêt", es: "Listo");
            case InGameChatManager.LobbyGreetingType.GoodLuck:
                return LanguageSelectionUI.Localize("Luck", ar: "توفيق", fa: "موفق", ru: "Удачи", fr: "Chance", es: "Suerte");
            case InGameChatManager.LobbyGreetingType.EpicBattle:
                return LanguageSelectionUI.Localize("Battle", ar: "معركة", fa: "نبرد", ru: "Битва", fr: "Bataille", es: "Batalla");
            default:
                return "";
        }
    }
}
