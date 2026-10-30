using UnityEngine;

/// <summary>
/// TeamVoiceChatUI provides:
/// 1. Pre-match setup modal asking player if they want live team voice calls or text-only mode.
/// 2. Interactive help (?) card explaining data usage, battery, and speed trade-offs across 13 languages.
/// 3. In-match top ribbon controls [🎙️ Mic] and [🔊 Speaker] with animated VU voice meter and speaking indicators.
/// </summary>
public class TeamVoiceChatUI : MonoBehaviour
{
    public static TeamVoiceChatUI Instance { get; private set; }

    public static bool IsPreMatchModalOpen { get; private set; } = false;
    private static bool showHelpExplanation = false;
    private static bool rememberChoiceAlways = true;

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

    void Start()
    {
        // Prompt player on first match if they haven't configured their preference yet
        if (!TeamVoiceChatManager.HasConfiguredVoicePreference)
        {
            IsPreMatchModalOpen = true;
        }
    }

    public static void OpenPreMatchPrompt()
    {
        IsPreMatchModalOpen = true;
        showHelpExplanation = false;
    }

    public static void ClosePreMatchPrompt()
    {
        IsPreMatchModalOpen = false;
        showHelpExplanation = false;
    }

    void OnGUI()
    {
        if (MatchEndStatsUI.IsGameOverActive) return;
        if (PlayModeUI.isTitleScreenActive) return;
        if (LanguageSelectionUI.IsLanguageModalOpen) return;

        Font gameFont = EnchantedNatureGUI.GetActiveGameFont();
        if (gameFont != null) GUI.skin.font = gameFont;

        bool isPlaying = GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying();

        // 1. In-Match Top Voice HUD Controls
        if (isPlaying && !IsPreMatchModalOpen)
        {
            GUI.depth = -100;
            DrawTopVoiceHUDControls();
            DrawActiveSpeakerFloatingBanner();
        }

        // 2. Pre-Match Voice Selection Modal
        if (IsPreMatchModalOpen)
        {
            GUI.depth = -400;
            DrawPreMatchVoiceModal();
        }
    }

    // ==========================================
    // 1. Top Ribbon Voice HUD Controls
    // ==========================================

    private void DrawTopVoiceHUDControls()
    {
        // Placed on top ribbon: [Pause 145px] [Chat 44px] [Voice Controls]
        float pauseW = 145f;
        float chatW = 44f;
        float startX = 16f + pauseW + 8f + chatW + 8f; // x = 221f
        float y = 14f;
        float h = 34f;

        if (TeamVoiceChatManager.IsVoiceChatEnabled)
        {
            // A. Microphone Toggle Button
            float micW = 50f;
            bool isMicMuted = TeamVoiceChatManager.IsMicMuted;
            bool isSpeaking = TeamVoiceChatManager.IsLocalSpeaking;

            string micIcon = isMicMuted ? "🔇" : (isSpeaking ? "🎙️ ▂▃" : "🎙️");
            Color micAccent = isMicMuted
                ? new Color(1f, 0.35f, 0.35f)
                : (isSpeaking ? new Color(0.25f, 0.98f, 0.45f) : new Color(0.35f, 0.88f, 0.95f));

            if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(startX, y, micW, h), micIcon, micAccent, 11, isSpeaking))
            {
                TeamVoiceChatManager.ToggleMicMute();
            }

            // B. Speaker / Deafen Toggle Button
            float spkW = 44f;
            float spkX = startX + micW + 6f;
            bool isDeafened = TeamVoiceChatManager.IsDeafened;

            string spkIcon = isDeafened ? "🔈" : "🔊";
            Color spkAccent = isDeafened ? new Color(1f, 0.4f, 0.3f) : EnchantedNatureGUI.AntiqueGoldBright;

            if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(spkX, y, spkW, h), spkIcon, spkAccent, 11, false))
            {
                TeamVoiceChatManager.ToggleDeafen();
            }
        }
        else
        {
            // Voice Disabled - Compact icon to open settings anytime
            float offW = 54f;
            string offLabel = "🎙️ OFF";
            if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(startX, y, offW, h), offLabel, new Color(0.75f, 0.75f, 0.75f), 10, false))
            {
                OpenPreMatchPrompt();
            }
        }
    }

    // ==========================================
    // 2. Active Speaker Floating Banner
    // ==========================================

    private void DrawActiveSpeakerFloatingBanner()
    {
        if (string.IsNullOrEmpty(TeamVoiceChatManager.ActiveSpeakerName) || TeamVoiceChatManager.ActiveSpeakerTimer <= 0f) return;

        float bannerW = 280f;
        float bannerH = 30f;
        float x = (Screen.width - bannerW) / 2f;
        float y = 52f;

        Rect bannerRect = new Rect(x, y, bannerW, bannerH);

        // Dark Moss Plaque
        GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.88f);
        GUI.DrawTexture(bannerRect, Texture2D.whiteTexture);

        // Radiant emerald border
        GUI.color = new Color(0.3f, 0.95f, 0.55f, 0.90f);
        GUI.DrawTexture(new Rect(x, y, bannerW, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + bannerH - 2f, bannerW, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Animated sound wave bars
        int barCount = Mathf.RoundToInt(Mathf.PingPong(Time.time * 8f, 4f)) + 1;
        string bars = new string('▌', barCount);

        GUIStyle speakerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        speakerStyle.normal.textColor = new Color(0.4f, 1f, 0.65f);

        string speakerText = $"🎙️ {TeamVoiceChatManager.ActiveSpeakerName} {bars}";
        GUI.Label(bannerRect, speakerText, speakerStyle);
    }

    // ==========================================
    // 3. Pre-Match Voice Selection Modal
    // ==========================================

    private void DrawPreMatchVoiceModal()
    {
        // Dark Dim Background
        GUI.color = new Color(0.02f, 0.04f, 0.03f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Clamp(Screen.width * 0.55f, 380f, 620f);
        float modalH = showHelpExplanation ? Mathf.Min(Screen.height - 24f, 540f) : Mathf.Min(Screen.height - 24f, 380f);
        float mx = (Screen.width - modalW) / 2f;
        float my = (Screen.height - modalH) / 2f;

        Rect modalRect = new Rect(mx, my, modalW, modalH);

        // Ornate Leaf Plaque Frame
        EnchantedNatureGUI.DrawLeafPlaque(modalRect, new Color(0.07f, 0.12f, 0.09f, 0.98f), new Color(0.35f, 0.88f, 0.95f));

        // Header Title
        string title = LanguageSelectionUI.Localize(
            "🎙️ LIVE TEAM VOICE CALLS",
            ar: "🎙️ التواصل الصوتي المباشر (المكالمات مع الفريق)",
            fa: "🎙️ مکالمات صوتی زنده با هم‌تیمی‌ها",
            ru: "🎙️ ЖИВОЙ ГОЛОСОВОЙ ЧАТ КОМАНДЫ",
            fr: "🎙️ APPELS VOCAUX D'ÉQUIPE EN DIRECT",
            es: "🎙️ LLAMADAS DE VOZ DE EQUIPO EN VIVO",
            de: "🎙️ LIVE-TEAM-SPRACHCHAT",
            it: "🎙️ CHIAMATE VOCALI DI SQUADRA DAL VIVO",
            tr: "🎙️ CANLI TAKIM SESLİ SOHBETİ",
            pt: "🎙️ CHAMADAS DE VOZ DE EQUIPE AO VIVO",
            hi: "🎙️ लाइव टीम वॉयस कॉल",
            zh: "🎙️ 实时战队语音通话",
            ja: "🎙️ リアルタイムチーム音声通話");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(0.35f, 0.92f, 1f);
        GUI.Label(new Rect(mx + 20f, my + 14f, modalW - 40f, 26f), title, titleStyle);

        // Subtitle
        string subtitle = LanguageSelectionUI.Localize(
            "Choose your team communication preference for this match",
            ar: "اختر أسلوب التواصل المفضل لديك مع أعضاء فريقك في المعركة",
            fa: "شیوه ارتباطی مورد نظر خود را با هم‌تیمی‌ها انتخاب کنید",
            ru: "Выберите предпочтительный способ связи с командой",
            fr: "Choisissez votre mode de communication d'équipe",
            es: "Elige tu método de comunicación preferido con el equipo",
            de: "Wähle deine bevorzugte Team-Kommunikation",
            it: "Scegli il metodo di comunicazione per questa partita",
            tr: "Bu maç için takım iletişim tercihinizi seçin",
            pt: "Escolha seu método de comunicação com a equipe",
            hi: "इस मैच के लिए अपनी टीम संचार प्राथमिकता चुनें",
            zh: "为本次对局选择您偏好的战队沟通方式",
            ja: "この試合でのチームコミュニケーション方法を選択してください");

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter
        };
        subStyle.normal.textColor = new Color(0.75f, 0.88f, 0.80f);
        GUI.Label(new Rect(mx + 20f, my + 42f, modalW - 40f, 20f), subtitle, subStyle);

        // Help Explanation Toggle Button (❓ تفاصيل واستهلاك الإنترنت)
        float helpY = my + 68f;
        string helpBtnLabel = showHelpExplanation
            ? LanguageSelectionUI.Localize("▲ Hide Details & Data Usage Info", ar: "▲ إخفاء تفاصيل استهلاك الإنترنت", fa: "▲ بستن توضیحات مصرف اینترنت")
            : LanguageSelectionUI.Localize("❓ Details & Internet Data Usage (Click to read)", ar: "❓ تفاصيل استهلاك الإنترنت والبطارية (اضغط للتفسير)", fa: "❓ جزئیات مصرف اینترنت و باتری (کلیک کنید)");

        Color helpCol = showHelpExplanation ? new Color(1f, 0.85f, 0.35f) : new Color(0.4f, 0.85f, 0.6f);
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(mx + 30f, helpY, modalW - 60f, 30f), helpBtnLabel, helpCol, 11, showHelpExplanation))
        {
            showHelpExplanation = !showHelpExplanation;
        }

        float contentY = helpY + 36f;

        // Expanded Explanation Box
        if (showHelpExplanation)
        {
            float expBoxH = 150f;
            Rect expRect = new Rect(mx + 24f, contentY, modalW - 48f, expBoxH);
            EnchantedNatureGUI.DrawWoodPlate(expRect, new Color(0.05f, 0.09f, 0.06f, 0.95f), new Color(0.92f, 0.78f, 0.30f, 0.85f));

            GUIStyle expHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            GUIStyle expBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            expBodyStyle.normal.textColor = new Color(0.85f, 0.92f, 0.88f);

            // Left Block: Voice Mode Explanation
            float halfW = (expRect.width - 24f) / 2f;
            expHeaderStyle.normal.textColor = new Color(0.35f, 0.95f, 0.55f);
            GUI.Label(new Rect(expRect.x + 8f, expRect.y + 6f, halfW, 20f), "🎙️ " + LanguageSelectionUI.Localize("Voice Calls (Live Audio)", ar: "المكالمات الصوتية الحية", fa: "مکالمات صوتی زنده"), expHeaderStyle);

            string voiceDetails = LanguageSelectionUI.Localize(
                "• Instant live team talk like Free Fire.\n• Uses more internet data (~30 kbps).\n• Ideal for Wi-Fi or unlimited bundles.\n• Uses microphone & extra battery power.",
                ar: "• تواصل صوتي فوري في الزمن الحقيقي كما في فري فاير.\n• يستهلك باقة إنترنت وبيانات إضافية (~30 kbps).\n• ممتاز لشبكات Wi-Fi أو الباقات غير المحدودة.\n• يتطلب تشغيل المايك واستهلاك طاقة بطارية إضافية.",
                fa: "• مکالمه صوتی زنده و فوری مانند فری‌فایر.\n• مصرف اینترنت بیشتر (~30 کیلوبیت).\n• ایده‌آل برای وای‌فای و بسته‌های نامحدود.\n• مصرف بیشتر باتری به دلیل فعال بودن میکروفون.",
                ru: "• Живая голосовая связь как в Free Fire.\n• Потребляет больше трафика (~30 кб/с).\n• Рекомендуется для Wi-Fi сетей.\n• Больше расходует батарею.",
                fr: "• Voix en direct instantanée style Free Fire.\n• Consomme plus de données (~30 kbps).\n• Idéal pour les réseaux Wi-Fi.\n• Consomme plus de batterie.",
                es: "• Voz en directo como en Free Fire.\n• Consume más datos (~30 kbps).\n• Ideal para Wi-Fi o datos ilimitados.\n• Consume más batería del móvil.");
            GUI.Label(new Rect(expRect.x + 8f, expRect.y + 26f, halfW, expBoxH - 32f), voiceDetails, expBodyStyle);

            // Right Block: Text Only Explanation
            expHeaderStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            GUI.Label(new Rect(expRect.x + halfW + 16f, expRect.y + 6f, halfW, 20f), "💬 " + LanguageSelectionUI.Localize("Text & Quick Pings", ar: "الشات والإشارات السريعة", fa: "چت متنی و نشانه‌ها"), expHeaderStyle);

            string textDetails = LanguageSelectionUI.Localize(
                "• Maximum game speed & zero lag.\n• Zero extra internet data used.\n• Saves battery and device temperature.\n• 8 One-tap tactical pings and team chat.",
                ar: "• أقصى سرعة واستقرار للعبة وصفر لاج.\n• لا يستهلك أي باقة إنترنت إضافية.\n• يوفر بطارية الهاتف ويحافظ على برودة الجهاز.\n• تواصل سلس عبر 8 إشارات سريعة بضغطة زر.",
                fa: "• حداکثر سرعت بازی و بدون کوچک‌ترین لگ.\n• بدون مصرف حجم اینترنت اضافی.\n• صرفه‌جویی کامل در باتری و داغ نشدن گوشی.\n• ۸ پیام تاکتیکی سریع تنها با یک لمس.",
                ru: "• Максимальная скорость игры и ноль лагов.\n• Не тратит дополнительный интернет.\n• Экономит заряд аккумулятора.\n• 8 быстрых тактических сигналов.",
                fr: "• Vitesse de jeu maximale sans latence.\n• Zéro données internet supplémentaires.\n• Préserve la batterie de votre appareil.\n• 8 pings tactiques en un clic.",
                es: "• Máxima velocidad de juego sin lag.\n• Cero datos de internet extra.\n• Ahorra batería y no calienta el móvil.\n• 8 señales tácticas rápidas con un toque.");
            GUI.Label(new Rect(expRect.x + halfW + 16f, expRect.y + 26f, halfW, expBoxH - 32f), textDetails, expBodyStyle);

            contentY += expBoxH + 12f;
        }

        // Two Big Choice Buttons
        float btnW = modalW - 60f;
        float btnH = 46f;
        float btnX = mx + 30f;

        // Button 1: Enable Live Voice Calls
        string enableVoiceText = LanguageSelectionUI.Localize(
            "🎙️ YES, ENABLE LIVE VOICE CALLS (Faster Coordination)",
            ar: "🎙️ نعم، تفعيل المكالمات الصوتية المباشرة (تنسيق أسرع)",
            fa: "🎙️ بله، فعال‌سازی مکالمات صوتی زنده (هماهنگی سریع‌تر)",
            ru: "🎙️ ДА, ВКЛЮЧИТЬ ЖИВОЙ ГОЛОСОВОЙ ЧАТ (Быстрая связь)",
            fr: "🎙️ OUI, ACTIVER LA VOIX EN DIRECT (Coordination rapide)",
            es: "🎙️ SÍ, ACTIVAR VOZ EN DIRECTO (Coordinación rápida)",
            de: "🎙️ JA, LIVE-SPRACHCHAT AKTIVIEREN (Schnell)",
            it: "🎙️ SÌ, ATTIVA VOCE DAL VIVO (Più veloce)",
            tr: "🎙️ EVET, CANLI SESLİ SOHBETİ AÇ (Hızlı İletişim)",
            pt: "🎙️ SIM, ATIVAR VOZ AO VIVO (Coordenação Rápida)",
            hi: "🎙️ हाँ, लाइव वॉयस कॉल चालू करें (तेज़ समन्वय)",
            zh: "🎙️ 是的，开启实时语音通话（沟通更迅捷）",
            ja: "🎙️ はい、リアルタイム音声通話を有効にする（連携強化）");

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(btnX, contentY, btnW, btnH), enableVoiceText, new Color(0.2f, 0.95f, 0.45f), 12, true))
        {
            TeamVoiceChatManager.SetVoiceChatOption(true, rememberChoiceAlways);
            ClosePreMatchPrompt();
            return;
        }

        contentY += btnH + 10f;

        // Button 2: Text Chat Only (Save Data & Battery)
        string textOnlyText = LanguageSelectionUI.Localize(
            "💬 NO, TEXT & QUICK PINGS ONLY (Save Data & Battery)",
            ar: "💬 لا، تواصل نصي سريع فقط (توفير الإنترنت والبطارية)",
            fa: "💬 خیر، فقط چت متنی و نشانه‌ها (صرفه‌جویی در اینترنت و باتری)",
            ru: "💬 НЕТ, ТОЛЬКО ТЕКСТ И СИГНАЛЫ (Экономия батареи)",
            fr: "💬 NON, TEXTE ET PINGS SEULEMENT (Économie de données)",
            es: "💬 NO, SOLO TEXTO Y SEÑALES (Ahorrar batería y datos)",
            de: "💬 NEIN, NUR TEXT & PINGS (Daten & Akku sparen)",
            it: "💬 NO, SOLO TESTO E PING (Risparmia dati e batteria)",
            tr: "💬 HAYIR, SADECE METİN VE SİNYAL (Veri ve Pil Tasarrufu)",
            pt: "💬 NÃO, APENAS TEXTO E SINAIS (Economizar Bateria)",
            hi: "💬 नहीं, केवल टेक्स्ट और पिंग्स (डेटा और बैटरी बचाएं)",
            zh: "💬 否，仅使用文字与快捷战术信号（省流省电）",
            ja: "💬 いいえ、テキストとクイックシグナルのみ（省電力・軽量）");

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(btnX, contentY, btnW, btnH), textOnlyText, new Color(0.95f, 0.82f, 0.35f), 12, false))
        {
            TeamVoiceChatManager.SetVoiceChatOption(false, rememberChoiceAlways);
            ClosePreMatchPrompt();
            return;
        }

        contentY += btnH + 10f;

        // Remember Choice Checkbox
        string rememberText = LanguageSelectionUI.Localize(
            "☑ Remember my choice (Can change anytime in Pause Settings)",
            ar: "☑ تذكر اختياري دائماً (يمكنك تغييره في أي وقت من قائمة الإيقاف)",
            fa: "☑ انتخابم را به خاطر بسپار (قابل تغییر در منوی توقف)",
            ru: "☑ Запомнить выбор (можно изменить в меню паузы)",
            fr: "☑ Mémoriser mon choix (modifiable dans les paramètres)",
            es: "☑ Recordar mi elección (se puede cambiar en la pausa)");

        GUIStyle chkStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };
        chkStyle.normal.textColor = new Color(0.80f, 0.88f, 0.82f);
        GUI.Label(new Rect(btnX, contentY, btnW, 20f), rememberText, chkStyle);

        // Close Button (✕)
        GUI.color = new Color(0.9f, 0.3f, 0.3f);
        if (GUI.Button(new Rect(mx + modalW - 32f, my + 10f, 24f, 24f), "✕"))
        {
            ClosePreMatchPrompt();
        }
        GUI.color = Color.white;
    }
}
