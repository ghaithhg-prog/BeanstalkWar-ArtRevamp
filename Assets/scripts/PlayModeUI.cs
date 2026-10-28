using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Ancient Enchanted Forest Mode Selection Scroll:
/// Inscribed on ancient undulating tree bark scrolls without harsh straight lines,
/// featuring organic leaf contours, druidic glyphs, and natural curved tablets.
/// </summary>
public class PlayModeUI : MonoBehaviour
{
    public enum PlayMode
    {
        None,
        Online,
        Offline,
        Friendly
    }

    public static PlayMode SelectedMode = PlayMode.None;
    public static PlayModeUI Instance { get; private set; }

    [Header("Legacy UI Panel (Auto-hidden)")]
    public GameObject panel;
    public Button onlineButton;
    public Button offlineButton;

    private bool isMenuVisible = true;
    private bool inOnlineSubmenu = false;
    private string onlineConnectIP = "127.0.0.1";

    public static bool isTitleScreenActive = true;
    private Texture2D splashArtTex = null;

    // -------------------------------------------------------------------------
    // MODE SELECTION & SPECIFICATION POPUP MODAL
    // -------------------------------------------------------------------------
    public static int activeModeInfoModal = -1; // -1 = closed, 0 = Solo, 1 = Online, 2 = Friendly

    public struct ModeCardData
    {
        public int index;
        public Color accent;
        public string badge;
        public string title;
        public string sub;
        public string desc;
        public string[] highlights;
        public string btnText;
        public System.Action onClick;
    }

    private static ModeCardData[] currentModeCards = new ModeCardData[3];

    public static Font GetSelectedFont()
    {
        return EnchantedNatureGUI.GetActiveGameFont();
    }

    private Texture2D LoadSplashTexture()
    {
        if (splashArtTex != null) return splashArtTex;

        splashArtTex = Resources.Load<Texture2D>("UI/BeanstalkWar_TitleSplash");
        if (splashArtTex != null)
        {
            splashArtTex.filterMode = FilterMode.Bilinear;
            splashArtTex.wrapMode = TextureWrapMode.Clamp;
            return splashArtTex;
        }

        string directPath = System.IO.Path.Combine(Application.dataPath, "Resources/UI/BeanstalkWar_TitleSplash.png");
        if (!System.IO.File.Exists(directPath))
        {
            directPath = System.IO.Path.Combine(Application.dataPath, "Resources/UI/BeanstalkWar_TitleSplash.jpg");
        }

        if (System.IO.File.Exists(directPath))
        {
            byte[] fileData = System.IO.File.ReadAllBytes(directPath);
            splashArtTex = new Texture2D(2, 2);
            splashArtTex.LoadImage(fileData);
            splashArtTex.filterMode = FilterMode.Bilinear;
            splashArtTex.wrapMode = TextureWrapMode.Clamp;
        }

        return splashArtTex;
    }

    void Awake()
    {
        Instance = this;
        isTitleScreenActive = true;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    void Start()
    {
        ResetMenu();
    }

    public void ResetMenu()
    {
        if (MatchEndStatsUI.isAutoRestartingMatch)
        {
            isMenuVisible = false;
            isTitleScreenActive = false;
            return;
        }

        SelectedMode = PlayMode.None;
        inOnlineSubmenu = false;
        isMenuVisible = true;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void StartOffline()
    {
        SelectedMode = PlayMode.Offline;
        isMenuVisible = false;
        Debug.Log("🌲 [PlayModeUI] Starting OFFLINE forest battle (Host + AI Bots)");

        if (panel != null) panel.SetActive(false);

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    public void StartOnlineHost()
    {
        SelectedMode = PlayMode.Online;
        isMenuVisible = false;
        Debug.Log("👑 [PlayModeUI] Starting ONLINE forest realm as HOST");

        if (panel != null) panel.SetActive(false);

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    public void StartOnlineClient()
    {
        SelectedMode = PlayMode.Online;
        isMenuVisible = false;
        Debug.Log($"🎮 [PlayModeUI] Connecting to ONLINE forest realm at {onlineConnectIP}");

        if (panel != null) panel.SetActive(false);

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null && !string.IsNullOrEmpty(onlineConnectIP))
            {
                transport.ConnectionData.Address = onlineConnectIP.Trim();
            }
            NetworkManager.Singleton.StartClient();
        }
    }

    public void StartFriendlyMode()
    {
        SelectedMode = PlayMode.Friendly;
        isMenuVisible = false;
        Debug.Log("🤝 [PlayModeUI] Opening FRIENDLY forest lobby");

        if (panel != null) panel.SetActive(false);

        if (FriendlyLobbyUI.Instance != null)
        {
            FriendlyLobbyUI.Instance.OpenLobby();
        }
    }

    public static bool IsPlayModeMenuOpen => Instance != null && Instance.isMenuVisible && SelectedMode == PlayMode.None;

    void OnGUI()
    {
        if (LanguageSelectionUI.IsLanguageModalOpen) return;
        if (!isMenuVisible || SelectedMode != PlayMode.None) return;
        if (GameIntroBriefingUI.IsBriefingOpen) return;
        if (PlayerProgressionAndShopUI.IsShopOpen) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) return;
        if (FriendlyLobbyUI.Instance != null && FriendlyLobbyUI.Instance.isLobbyActive) return;

        Font activeGameFont = EnchantedNatureGUI.GetActiveGameFont();
        if (activeGameFont != null)
        {
            GUI.skin.font = activeGameFont;
        }

        // Keep background haze strictly behind top bar buttons (which are at depth -100)
        GUI.depth = 10;

        if (isTitleScreenActive)
        {
            DrawTitleSplashScreen();
            return;
        }

        // Dark enchanted emerald haze behind the scroll
        GUI.color = new Color(0.04f, 0.09f, 0.05f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (inOnlineSubmenu)
        {
            DrawOnlineSubmenu();
        }
        else
        {
            DrawMainModeSelect();
        }
    }

    // =========================================================================
    // GRAND TITLE SPLASH SCREEN (AAA Cinematic Presentation & Opening Screen)
    // =========================================================================
    void DrawTitleSplashScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // 1. Full Screen Splash Background Art (Completely Static, Crystal-Clear & Resample-Free)
        Texture2D splash = LoadSplashTexture();
        if (splash != null)
        {
            if (splash.filterMode != FilterMode.Bilinear) splash.filterMode = FilterMode.Bilinear;
            if (splash.wrapMode != TextureWrapMode.Clamp) splash.wrapMode = TextureWrapMode.Clamp;

            float imgAspect = (float)splash.width / (float)splash.height;
            float screenAspect = sw / sh;
            float dw, dh;
            if (screenAspect > imgAspect)
            {
                dw = sw;
                dh = sw / imgAspect;
            }
            else
            {
                dh = sh;
                dw = sh * imgAspect;
            }
            int idx = Mathf.RoundToInt((sw - dw) * 0.5f);
            int idy = Mathf.RoundToInt((sh - dh) * 0.5f);
            int idw = Mathf.RoundToInt(dw);
            int idh = Mathf.RoundToInt(dh);

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(idx, idy, idw, idh), splash, ScaleMode.StretchToFill);

            // Subtle dark fantasy atmospheric vignette at top & bottom so title and buttons rest with high contrast
            GUI.color = new Color(0.01f, 0.03f, 0.02f, 0.38f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        else
        {
            // Procedural Emerald Forest Gradient
            GUI.color = new Color(0.04f, 0.10f, 0.06f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // 2. Decorative Ancient Gold Corner Filigree Frame
        EnchantedNatureGUI.DrawCrispBorder(new Rect(12f, 12f, sw - 24f, sh - 24f), new Color(0.85f, 0.72f, 0.32f, 0.65f), 1.5f);
        EnchantedNatureGUI.DrawCornerFiligree(new Rect(16f, 16f, sw - 32f, sh - 32f), new Color(0.95f, 0.82f, 0.38f, 0.85f), 24f);

        // Top-Right Utility: Audio / Jukebox Selector Button (Crisp & Solid)
        float audioBtnW = 46f;
        float audioBtnH = 28f;
        float audioX = sw - audioBtnW - 10f;
        float audioY = 8f;
        bool isAudioMuted = (ForestBGMManager.Instance != null && ForestBGMManager.Instance.IsMutedTrack);
        if (EnchantedNatureGUI.DrawMusicJukeboxButton(new Rect(audioX, audioY, audioBtnW, audioBtnH), isAudioMuted))
        {
            ForestBGMManager.ToggleJukeboxModal();
        }

        // Top Guide Button
        float guideBtnW = 96f;
        float flagBtnW = 44f;
        float guideX = audioX - flagBtnW - 6f - guideBtnW - 6f; // Leaves space for language flag button in-between
        if (guideX > 180f)
        {
            string guideText = LanguageSelectionUI.Localize("📖 GUIDE", ar: "📖 التوجيهات", fa: "📖 راهنما", ru: "📖 ИНФО", de: "📖 ANLEITUNG", it: "📖 GUIDA", tr: "📖 REHBER", es: "📖 GUÍA", fr: "📖 GUIDE", zh: "📖 指南", ja: "📖 ガイド", hi: "📖 निर्देश", pt: "📖 GUIA");
            if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(guideX, audioY, guideBtnW, audioBtnH), guideText, new Color(0.35f, 0.95f, 0.85f), 11))
            {
                GameIntroBriefingUI.Instance?.OpenBriefing();
            }
        }

        // 3. Grand Game Title Plaque (Center-Top)
        float titleBoxW = Mathf.Min(sw * 0.88f, 880f);
        float titleBoxH = 152f;
        float titleBoxX = (sw - titleBoxW) * 0.5f;
        float titleBoxY = Mathf.Max(26f, sh * 0.08f);

        Rect titleRect = new Rect(titleBoxX, titleBoxY, titleBoxW, titleBoxH);
        EnchantedNatureGUI.DrawWoodPlate(titleRect, new Color(0.08f, 0.14f, 0.09f, 0.94f), new Color(0.92f, 0.78f, 0.30f, 0.95f));

        // Floating Title Shimmer
        float titleGlow = (Mathf.Sin(Time.time * 2.5f) + 1f) * 0.5f;
        Color glowingGold = Color.Lerp(new Color(0.95f, 0.82f, 0.28f), new Color(1f, 0.98f, 0.65f), titleGlow);

        string grandTitle = LanguageSelectionUI.Localize(
            "BEANSTALK WAR",
            ar: "حَرْبُ شَجَرَةِ الفَاصُولِيَاء",
            fa: "جنگ ساقه لوبیا",
            ru: "БИТВА ЗА БОБОВЫЙ СТЕБЕЛЬ",
            de: "KRIEG DER BOHNENRANKE",
            it: "GUERRA DEL FAGIOLO SACRO",
            tr: "FASULYE SIRIĞI SAVAŞI",
            es: "LA GUERRA DEL TALLO SAGRADO",
            fr: "LA GUERRE DU HARICOT SACRÉ",
            zh: "巨藤神树之战",
            ja: "聖なる巨木の戦い",
            hi: "सेम के पौधे का महायुद्ध",
            pt: "A GUERRA DO PÉ DE FEIJÃO"
        );

        Font customTitleFont = GetSelectedFont();
        GUIStyle mainTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sw * 0.038f), 26, 44),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        if (customTitleFont != null)
        {
            mainTitleStyle.font = customTitleFont;
        }

        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(titleBoxX + 10f, titleBoxY + 12f, titleBoxW - 20f, 56f),
            grandTitle,
            mainTitleStyle,
            glowingGold,
            new Color(0.02f, 0.05f, 0.02f, 0.98f),
            2.5f, 2.5f
        );

        string grandSubtitle = LanguageSelectionUI.Localize(
            "⚔️ Battle for the Sacred Tree & Celestial Sanctuary ⚔️",
            ar: "⚔️ مَعْرَكَةُ الشَّجَرَةِ المُقَدَّسَةِ وَالمِحْرَابِ السَّمَاوِيّ ⚔️",
            fa: "⚔️ نبرد بر سر درخت مقدس و محراب آسمانی ⚔️",
            ru: "⚔️ Битва за Священное Древо и Небесный Алтарь ⚔️",
            de: "⚔️ Schlacht um den Heiligen Baum & Himmlischen Altar ⚔️",
            it: "⚔️ Battaglia per l'Albero Sacro e il Santuario Celeste ⚔️",
            tr: "⚔️ Kutsal Ağaç ve Göksel Sunak İçin Savaş ⚔️",
            es: "⚔️ Batalla por el Árbol Sagrado y el Altar Celestial ⚔️",
            fr: "⚔️ Bataille pour l'Arbre Sacré et le Sanctuaire Céleste ⚔️",
            zh: "⚔️ 神圣巨树守护与苍穹圣坛争夺战 ⚔️",
            ja: "⚔️ 聖なる巨木と天空の祭壇を巡る最終決戦 ⚔️",
            hi: "⚔️ पवित्र वृक्ष और दिव्य वेदी का महासंग्राम ⚔️",
            pt: "⚔️ Batalha pela Árvore Sagrada e Altar Celestial ⚔️"
        );

        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sw * 0.016f), 13, 17),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(titleBoxX + 10f, titleBoxY + 74f, titleBoxW - 20f, 28f),
            grandSubtitle,
            subtitleStyle,
            new Color(0.35f, 0.95f, 0.88f),
            new Color(0.01f, 0.04f, 0.02f, 0.95f),
            1.5f, 1.5f
        );

        // Feature badge ribbons below title
        string factionInfo = LanguageSelectionUI.Localize(
            "🛡️ Protectors of the Forest   ⚔️ VS ⚔️   🔥 Destroyers of the Canopy",
            ar: "🛡️ حُمَاةُ الغَابَةِ المُقَدَّسَةِ   ⚔️ ضِدَّ ⚔️   🔥 مُدَمِّرُو الشَّجَرَةِ",
            fa: "🛡️ مدافعان جنگل مقدس   ⚔️ در برابر ⚔️   🔥 ویرانگران درخت",
            ru: "🛡️ Защитники Священного Леса   ⚔️ ПРОТИВ ⚔️   🔥 Разрушители Древа",
            de: "🛡️ Hüter des Heiligen Waldes   ⚔️ GEGEN ⚔️   🔥 Zerstörer des Baumes",
            it: "🛡️ Protettori della Foresta   ⚔️ CONTRO ⚔️   🔥 Distruttori dell'Albero",
            tr: "🛡️ Kutsal Ormanın Koruyucuları   ⚔️ KARŞI ⚔️   🔥 Ağaç Yok Edicileri",
            es: "🛡️ Protectores del Bosque   ⚔️ VS ⚔️   🔥 Destructores del Árbol",
            fr: "🛡️ Protecteurs de la Forêt   ⚔️ VS ⚔️   🔥 Destructeurs de l'Arbre",
            zh: "🛡️ 圣林守护盟军   ⚔️ 阵营巅峰决战 ⚔️   🔥 烈焰伐树征服者",
            ja: "🛡️ 聖樹の守護同盟   ⚔️ 宿命の激突 ⚔️   🔥 巨木伐採破壊軍",
            hi: "🛡️ पवित्र वन के संरक्षक   ⚔️ बनाम ⚔️   🔥 वृक्ष के विनाशक",
            pt: "🛡️ Protetores da Floresta   ⚔️ VS ⚔️   🔥 Destruidores da Árvore"
        );
        GUIStyle factionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(sw * 0.013f), 11, 14),
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.92f, 0.85f, 0.85f) }
        };
        GUI.Label(new Rect(titleBoxX + 10f, titleBoxY + 110f, titleBoxW - 20f, 24f), factionInfo, factionStyle);

        // 5. Grand Interactive Call-to-Action (Center-Bottom)
        float startBtnW = Mathf.Clamp(sw * 0.50f, 360f, 560f);
        float startBtnH = 62f;
        float startBtnX = (sw - startBtnW) * 0.5f;
        float startBtnY = sh - startBtnH - 80f;

        float btnPulse = (Mathf.Sin(Time.time * 3.5f) + 1f) * 0.5f;
        Color btnBorderCol = Color.Lerp(new Color(0.28f, 0.95f, 0.45f), new Color(1f, 0.88f, 0.35f), btnPulse);

        string startText = LanguageSelectionUI.Localize(
            "⚔️ ENTER THE FOREST — PLAY NOW ⚔️",
            ar: "⚔️ ادْخُلْ الغَابَةَ — ابْدَأ اللَّعِبَ الآن ⚔️",
            fa: "⚔️ ورود به جنگل — آغاز نبرد ⚔️",
            ru: "⚔️ ВОЙТИ В ЛЕС — ИГРАТЬ ⚔️",
            de: "⚔️ BETRETE DEN WALD — JETZT SPIELEN ⚔️",
            it: "⚔️ ENTRA NELLA FORESTA — GIOCA ORA ⚔️",
            tr: "⚔️ ORMANA GİR — ŞİMDİ OYNA ⚔️",
            es: "⚔️ ENTRAR AL BOSQUE — JUGAR AHORA ⚔️",
            fr: "⚔️ ENTRER DANS LA FORÊT — JOUER ⚔️",
            zh: "⚔️ 进入神秘巨林 — 立即开启征程 ⚔️",
            ja: "⚔️ 神秘の森へ進撃 — 今すぐプレイ ⚔️",
            hi: "⚔️ जंगल में प्रवेश करें — अभी खेलें ⚔️",
            pt: "⚔️ ENTRAR NA FLORESTA — JOGAR AGORA ⚔️"
        );

        Rect startBtnRect = new Rect(startBtnX, startBtnY, startBtnW, startBtnH);
        if (EnchantedNatureGUI.DrawOrganicLeafButton(startBtnRect, startText, btnBorderCol, 15))
        {
            isTitleScreenActive = false;
        }

        // Subtitle Hint
        string hintText = LanguageSelectionUI.Localize(
            "~ Click the button, press SPACE or click anywhere on screen ~",
            ar: "~ انقر فوق الزر أو اضغط مسافة أو انقر في أي مكان للدخول ~",
            fa: "~ روی دکمه کلیک کنید یا کلید فاصله را فشار دهید ~",
            ru: "~ Нажмите кнопку, ПРОБЕЛ или кликните в любом месте экрана ~",
            de: "~ Klicke auf den Button, drücke LEERTASTE oder klicke irgendwo hin ~",
            it: "~ Fai clic sul pulsante, premi SPAZIO o clicca ovunque sullo schermo ~",
            tr: "~ Butona tıklayın, BOŞLUK tuşuna basın veya ekranda herhangi bir yere tıklayın ~",
            es: "~ Haz clic en el botón, presiona ESPACIO o haz clic en cualquier lugar ~",
            fr: "~ Cliquez sur le bouton, appuyez sur ESPACE ou cliquez n'importe où ~",
            zh: "~ 点击上方按钮、敲击空格键或在屏幕任意处点击开启冒险 ~",
            ja: "~ ボタンをクリック、スペースキーを押すか、画面の任意の場所をクリック ~",
            hi: "~ बटन पर क्लिक करें, स्पेस दबाएं या स्क्रीन पर कहीं भी क्लिक करें ~",
            pt: "~ Clique no botão, pressione ESPAÇO ou clique em qualquer lugar da tela ~"
        );
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.95f, 0.75f, 0.85f) }
        };
        GUI.Label(new Rect(startBtnX - 100f, startBtnY + startBtnH + 8f, startBtnW + 200f, 22f), hintText, hintStyle);

        // Click anywhere on screen (excluding top bar) to start
        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.mousePosition.y > 60f)
        {
            isTitleScreenActive = false;
            evt.Use();
        }
        else if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return))
        {
            isTitleScreenActive = false;
            evt.Use();
        }
    }

    // =========================================================================
    // TRIUMVIRATE GATEWAYS (3 VERTICAL AAA MONOLITHS)
    // =========================================================================
    void DrawMainModeSelect()
    {
        // 0. Top-Left: Return to Title Screen Button
        float backBtnW = 145f;
        float backBtnH = 38f;
        float backX = 12f;
        float backY = 8f;
        string backText = LanguageSelectionUI.Localize(
            "🔙 TITLE SCREEN",
            ar: "🔙 شاشة البداية",
            fa: "🔙 صفحه عنوان",
            ru: "🔙 ТИТУЛЬНЫЙ ЭКРАН",
            de: "🔙 TITELBILDSCHIRM",
            it: "🔙 SCHERMATA TITOLO",
            tr: "🔙 BAŞLIK EKRANI",
            es: "🔙 PANTALLA TÍTULO",
            fr: "🔙 ÉCRAN TITRE",
            zh: "🔙 返回标题画面",
            ja: "🔙 タイトル画面へ",
            hi: "🔙 शीर्षक स्क्रीन",
            pt: "🔙 TELA DE TÍTULO"
        );
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(backX, backY, backBtnW, backBtnH), backText, EnchantedNatureGUI.AntiqueGoldMid, 11))
        {
            isTitleScreenActive = true;
        }

        // 1. Unified Top Citadel Ribbon - Audio / Jukebox Selector (Top-Right, Solid & Crisp)
        float audioBtnW = 46f;
        float audioBtnH = 28f;
        float audioX = Screen.width - audioBtnW - 10f;
        float audioY = 8f;
        bool isMainMenuAudioMuted = (ForestBGMManager.Instance != null && ForestBGMManager.Instance.IsMutedTrack);
        if (EnchantedNatureGUI.DrawMusicJukeboxButton(new Rect(audioX, audioY, audioBtnW, audioBtnH), isMainMenuAudioMuted))
        {
            ForestBGMManager.ToggleJukeboxModal();
        }

        // 2. Main Menu Frame - Sits comfortably below the top bar (starts at y = 54f with zero overlap!)
        float topBarBottom = 46f;
        float modalY = topBarBottom + 8f;
        float modalHeight = Screen.height - modalY - 14f;
        float modalWidth = Mathf.Min(1100f, Screen.width - 24f);
        float modalX = (Screen.width - modalWidth) / 2f;

        // Master Obsidian & Antique Gold Frame
        EnchantedNatureGUI.DrawAntiqueGoldFrame(new Rect(modalX, modalY, modalWidth, modalHeight), Color.white, 0f);

        // 3. Central Grand Title: BEANSTALKWAR
        string title = "⚜️ BEANSTALKWAR ⚜️";
        string subTitle = LanguageSelectionUI.Localize(
            "~ Sacred Chronicles of the Colossal Tree & Rival Factions ~",
            ar: "~ أسطورة الشجرة العملاقة والتحالفات المتصارعة ~",
            ru: "~ Хроники Колоссального Древа и Воюющих Фракций ~",
            es: "~ Crónicas del Árbol Colosal y Facciones Rivales ~",
            fr: "~ Chroniques de l'Arbre Colossal et Factions Rivales ~",
            de: "~ Chroniken des Kolossalen Baumes & Kriegführenden Fraktionen ~",
            hi: "~ विशाल वृक्ष और प्रतिद्वंद्वी गुटों की गाथा ~",
            zh: "~ 巨树与敌对阵营之神圣史诗 ~",
            ja: "~ 巨木の聖樹と覇を競う部族たちの年代記 ~",
            pt: "~ Crônicas da Árvore Colossal e Facções Rivais ~",
            fa: "~ شرح وقایع درخت غول‌پیکر و قبایل رقیب ~",
            it: "~ Cronache dell'Albero Colossale e delle Fazioni Rivali ~",
            tr: "~ Devasa Ağacın ve Rakip Kabilelerin Kutsal Tarihi ~"
        );

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 22;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(modalX, modalY + 8f, modalWidth, 26f),
            title,
            titleStyle,
            EnchantedNatureGUI.AntiqueGoldBright,
            new Color(0.04f, 0.08f, 0.05f, 0.95f),
            1.5f, 1.5f
        );

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label);
        subTitleStyle.fontSize = 11;
        subTitleStyle.fontStyle = FontStyle.Italic;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(modalX, modalY + 34f, modalWidth, 18f),
            subTitle,
            subTitleStyle,
            new Color(0.68f, 0.92f, 0.76f),
            new Color(0.02f, 0.04f, 0.02f, 0.85f),
            1f, 1f
        );

        // Header flourish divider
        EnchantedNatureGUI.DrawFlourishDivider(new Rect(modalX + 60f, modalY + 54f, modalWidth - 120f, 6f), EnchantedNatureGUI.AntiqueGoldMid);

        // 4. The Triumvirate Portals (3 Columns Side-by-Side)
        float availW = modalWidth - 44f;
        float gap = 16f;
        float cardW = (availW - (2f * gap)) / 3f;
        float cardY = modalY + 64f;
        float cardH = modalHeight - 92f;

        // ==========================================
        // PORTAL 1: SOLO EXPEDITION (SOLO vs BOTS)
        // ==========================================
        float c1X = modalX + 24f;
        string c1Badge = LanguageSelectionUI.Localize("🍃 SACRED TRIAL",
            ar: "🍃 تجربة مقدسة",
            ru: "🍃 ИСПЫТАНИЕ",
            es: "🍃 PRUEBA SAGRADA",
            fr: "🍃 ÉPREUVE SACRÉE",
            de: "🍃 HEILIGE PRÜFUNG",
            hi: "🍃 पवित्र परीक्षण",
            zh: "🍃 神圣试炼",
            ja: "🍃 聖なる試練",
            pt: "🍃 PROVA SAGRADA",
            fa: "🍃 آزمون مقدس",
            it: "🍃 PROVA SACRA",
            tr: "🍃 KUTSAL İMTİHAN");

        string c1Title = LanguageSelectionUI.Localize("SOLO PLAY (OFFLINE)",
            ar: "اللعب الفردي (أوفلاين)",
            ru: "ОДИНОЧНЫЙ БОЙ (ОФЛАЙН)",
            es: "JUEGO INDIVIDUAL (OFFLINE)",
            fr: "JEU SOLO (HORS-LIGNE)",
            de: "SOLO-SPIEL (OFFLINE)",
            hi: "एकल खेल (ऑफ़लाइन)",
            zh: "单人游玩（离线）",
            ja: "ソロプレイ（オフライン）",
            pt: "JOGO INDIVIDUAL (OFFLINE)",
            fa: "بازی انفرادی (آفلاین)",
            it: "GIOCO SINGOLO (OFFLINE)",
            tr: "TEK OYUNCULU (ÇEVRİMDIŞI)");

        string c1Sub = LanguageSelectionUI.Localize("Offline • Against AI Bots",
            ar: "بدون إنترنت • ضد البوتات",
            ru: "Без интернета • Против ботов",
            es: "Sin internet • Contra bots IA",
            fr: "Sans connexion • Contre les bots IA",
            de: "Ohne Internet • Gegen KI-Bots",
            hi: "बिना इंटरनेट • बॉट्स के विरुद्ध",
            zh: "无需网络 • 对阵AI人机",
            ja: "オフライン • 対AIボット戦",
            pt: "Sem internet • Contra bots de IA",
            fa: "بدون اینترنت • در برابر بات‌ها",
            it: "Senza internet • Contro i bot IA",
            tr: "İnternetsiz • Yapay Zeka Botlarına Karşı");

        string[] c1Highlights = new string[]
        {
            LanguageSelectionUI.Localize("⚔️ 3 Guardian Titans", ar: "⚔️ ٣ زعماء عمالقة", ru: "⚔️ 3 титана-босса", es: "⚔️ 3 jefes titanes", fr: "⚔️ 3 titans gardiens", de: "⚔️ 3 Titanen-Bosse", hi: "⚔️ ३ अभिभावक टाइटन", zh: "⚔️ 三大森林巨像Boss", ja: "⚔️ 3体の守護タイタン", pt: "⚔️ 3 chefes titãs", fa: "⚔️ ۳ غول نگهبان", it: "⚔️ 3 titani guardiani", tr: "⚔️ 3 Muhafız Titan"),
            LanguageSelectionUI.Localize("🌲 Chop Tree or Take Crystal", ar: "🌲 اسقط الشجرة أو خذ الكريستال", ru: "🌲 Срубите древо или кристалл", es: "🌲 Tala el árbol o toma el cristal", fr: "🌲 Abattez l'arbre ou cristal", de: "🌲 Baum fällen oder Kristall", hi: "🌲 पेड़ काटें या क्रिस्टल लें", zh: "🌲 伐倒古树或争夺水晶", ja: "🌲 神木伐採または水晶奪回", pt: "🌲 Derrube a árvore ou cristal", fa: "🌲 قطع درخت یا تصاحب کریستال", it: "🌲 Abbatti l'albero o prendi il cristallo", tr: "🌲 Ağacı Devir veya Kristali Al"),
            LanguageSelectionUI.Localize("🛡️ Master 25 Hero Classes", ar: "🛡️ تدرّب على ٢٥ بطلاً", ru: "🛡️ 25 классов героев", es: "🛡️ Domina 25 héroes", fr: "🛡️ Maîtrisez 25 héros", de: "🛡️ Meistert 25 Helden", hi: "🛡️ २५ नायकों में महारت", zh: "🛡️ 驾驭25位传奇英雄", ja: "🛡️ 25人の英雄を極めろ", pt: "🛡️ Domine 25 heróis", fa: "🛡️ تسلط بر ۲۵ قهرمان", it: "🛡️ Domina 25 classi di eroi", tr: "🛡️ 25 Kahraman Sınıfında Ustalaş"),
            LanguageSelectionUI.Localize("💰 Offline Gold & XP", ar: "💰 ذهب ونقاط خبرة بدون نت", ru: "💰 Золото и опыт без сети", es: "💰 Oro y EXP sin internet", fr: "💰 Or et EXP hors-ligne", de: "💰 Gold & XP offline", hi: "💰 बिना नेट सोना व XP", zh: "💰 离线获取金币与经验", ja: "💰 オフラインで報酬獲得", pt: "💰 Ouro e EXP offline", fa: "💰 طلا و امتیاز تجربه بدون اینترنت", it: "💰 Oro ed ESP offline", tr: "💰 Çevrimdışı Altın ve TP")
        };

        string c1Desc = LanguageSelectionUI.Localize(
            "Venture into secluded groves to test thy valor against bot warriors. Challenge 3 titans and harvest gold & XP without internet.",
            ar: "انطلق في الغابة لتختبر شجاعتك ضد محاربي البوتات. اهزم الزعماء الثلاثة، واكسب الذهب ونقاط الخبرة بدون إنترنت.",
            ru: "Сразитесь с лесными ботами без интернета. Победите трех боссов и получайте золото и опыт.",
            es: "Adéntrate en el bosque contra bots guerreros. Derrota a los 3 jefes y gana oro y EXP sin internet.",
            fr: "Aventurez-vous dans les bois contre des guerriers bots. Battez les 3 boss et gagnez de l'or sans connexion.",
            de: "Begebt euch in die Wälder gegen Bot-Krieger. Besiegt 3 Bosse und sammelt Gold & XP ohne Internet.",
            hi: "जंगल में बॉट योद्धाओं के विरुद्ध अपने साहस का परीक्षण करें।",
            zh: "深入密林挑战人机守卫。击败三大Boss，无需联网即可斩获黄金与经验。",
            ja: "森のボット戦士たちと腕試し。3体のボスを討伐し、オフラインで報酬を獲得しよう。",
            pt: "Aventure-se na floresta contra bots guerreiros. Derrote os 3 chefes e ganhe ouro e EXP offline.",
            fa: "به بیشه پنهان بروید تا شجاعت خود را در برابر جنگجویان بات بسنجید. ۳ غول را شکست دهید و طلا و امتیاز بدون اینترنت کسب کنید.",
            it: "Avventurati nei boschi contro guerrieri bot. Sconfiggi 3 titani e raccogli oro ed ESP senza connessione.",
            tr: "Bot savaşçılara karşı ormana dalın. 3 titanı yenin ve internet olmadan altın ve TP kazanın."
        );

        string c1Btn = LanguageSelectionUI.Localize("🌲 PLAY SOLO",
            ar: "🌲 ابدأ اللعب الفردي",
            ru: "🌲 ИГРАТЬ ОДНОМУ",
            es: "🌲 JUGAR SOLO",
            fr: "🌲 JOUER EN SOLO",
            de: "🌲 SOLO SPIELEN",
            hi: "🌲 अकेले खेलें",
            zh: "🌲 开始单人游戏",
            ja: "🌲 ソロプレイ開始",
            pt: "🌲 JOGAR SOLO",
            fa: "🌲 آغاز بازی تک‌نفره",
            it: "🌲 GIOCA DA SOLO",
            tr: "🌲 TEK OYNA");

        currentModeCards[0] = new ModeCardData {
            index = 0,
            accent = EnchantedNatureGUI.EmeraldAccent,
            badge = c1Badge,
            title = c1Title,
            sub = c1Sub,
            desc = c1Desc,
            highlights = c1Highlights,
            btnText = c1Btn,
            onClick = () => StartOffline()
        };

        DrawVerticalPortalCard(c1X, cardY, cardW, cardH, currentModeCards[0]);

        // ==========================================
        // PORTAL 2: REALM WAR (ONLINE MULTIPLAYER)
        // ==========================================
        float c2X = c1X + cardW + gap;
        string c2Badge = LanguageSelectionUI.Localize("👑 LIVE WAR",
            ar: "👑 معركة حية",
            ru: "👑 ЖИВАЯ ВОЙНА",
            es: "👑 GUERRA EN VIVO",
            fr: "👑 GUERRE EN DIRECT",
            de: "👑 LIVE-SCHLACHT",
            hi: "👑 लाइव युद्ध",
            zh: "👑 实时全服战役",
            ja: "👑 リアルタイム戦",
            pt: "👑 GUERRA AO VIVO",
            fa: "👑 نبرد زنده",
            it: "👑 GUERRA DAL VIVO",
            tr: "👑 CANLI SAVAŞ");

        string c2Title = LanguageSelectionUI.Localize("ONLINE PLAY",
            ar: "اللعب عبر الإنترنت",
            ru: "ОНЛАЙН ИГРА",
            es: "JUEGO ONLINE",
            fr: "JEU EN LIGNE",
            de: "ONLINE-SPIEL",
            hi: "ऑनलाइन खेल",
            zh: "网络联机",
            ja: "オンライン対戦",
            pt: "JOGO ONLINE",
            fa: "بازی آنلاین",
            it: "GIOCO ONLINE",
            tr: "ÇEVRİMİÇİ OYUN");

        string c2Sub = LanguageSelectionUI.Localize("Live Multiplayer Battles",
            ar: "معارك حية متعددة اللاعبين",
            ru: "Живые сетевые сражения",
            es: "Batallas multijugador en vivo",
            fr: "Batailles multijoueurs en direct",
            de: "Live-Mehrspielerschlachten",
            hi: "लाइव मल्टीप्लेयर लड़ाइयाँ",
            zh: "实时多人在线对战",
            ja: "白熱のマルチプレイ対戦",
            pt: "Batalhas multijogador ao vivo",
            fa: "نبردهای زنده چندنفره",
            it: "Battaglie multigiocatore dal vivo",
            tr: "Canlı Çok Oyunculu Savaşlar");

        string[] c2Highlights = new string[]
        {
            LanguageSelectionUI.Localize("👑 Host Realm or Direct IP", ar: "👑 استضافة سيرفر أو بالـ IP", ru: "👑 Сервер хоста или прямой IP", es: "👑 Hostea o conecta por IP", fr: "👑 Hébergez ou rejoignez par IP", de: "👑 Server hosten oder IP-Beitritt", hi: "👑 सर्वर होस्ट या सीधा आईपी", zh: "👑 建立主机或直连IP", ja: "👑 サーバー作成・IP直結", pt: "👑 Hostear ou conectar via IP", fa: "👑 میزبانی سرور یا اتصال با آی‌پی", it: "👑 Ospita server o entra con IP", tr: "👑 Sunucu Aç veya IP ile Bağlan"),
            LanguageSelectionUI.Localize("⚔️ Protectors vs Destroyers", ar: "⚔️ حماة الشجرة ضد المدمرين", ru: "⚔️ Защитники против Разрушителей", es: "⚔️ Protectores vs Destructores", fr: "⚔️ Protecteurs vs Destructeurs", de: "⚔️ Beschützer vs Zerstörer", hi: "⚔️ रक्षक बनाम विनाशक", zh: "⚔️ 守护者对阵破坏者", ja: "⚔️ 守護者 vs 破壊者", pt: "⚔️ Protetores vs Destruidores", fa: "⚔️ حامیان در برابر ویرانگران", it: "⚔️ Protettori vs Distruttori", tr: "⚔️ Muhafızlar vs Yok Ediciler"),
            LanguageSelectionUI.Localize("💎 Live Crystal Altar War", ar: "💎 صراع الكريستالة المباشر", ru: "💎 Битва за алтарь кристаллов", es: "💎 Lucha en vivo por el altar", fr: "💎 Guerre du cristal en direct", de: "💎 Echtzeit-Kristallkampf", hi: "💎 लाइव क्रिस्टल वेदी युद्ध", zh: "💎 水晶祭坛争夺战", ja: "💎 水晶の祭壇をめぐる激戦", pt: "💎 Guerra pelo altar de cristal", fa: "💎 نبرد زنده محراب کریستال", it: "💎 Guerra per l'altare del cristallo", tr: "💎 Canlı Kristal Sunak Savaşı"),
            LanguageSelectionUI.Localize("🔥 10s Divine Shield Stand", ar: "🔥 صمود درع الشجرة الأخير", ru: "🔥 10с священного щита", es: "🔥 Escudo divino de 10 seg", fr: "🔥 Bouclier divin de 10s", de: "🔥 10s göttlicher Schutzschild", hi: "🔥 १० सेकंड दिव्य ढाल", zh: "🔥 10秒神圣护盾最后坚守", ja: "🔥 10秒間の神木シールド攻防", pt: "🔥 Escudo sagrado de 10s", fa: "🔥 ایستادگی ۱۰ ثانیه‌ای سپر مقدس", it: "🔥 Difesa Scudo Divino di 10s", tr: "🔥 10 sn İlahi Kalkan Mücadelesi")
        };

        string c2Desc = LanguageSelectionUI.Localize(
            "Unite with warriors across distant realms in live network warfare. Defend the Sacred Beanstalk or fell its colossal trunk in tactical multiplayer combat.",
            ar: "اتحد مع حلفائك عبر السيرفرات في معارك جماعية مباشرة عبر الشلالات ومناطق الغابة المقدسة.",
            ru: "Объединяйтесь с игроками по сети. Защищайте священное древо или сокрушите его ствол.",
            es: "Únete a guerreros en reinos lejanos. Defiende el árbol sagrado o derriba su tronco colosal.",
            fr: "Rejoignez d'autres guerriers en réseau. Défendez l'arbre sacré ou abattez son tronc colossal.",
            de: "Verbündet euch online. Verteidigt die Bohnenranke oder fällt ihren Stamm im Taktikkampf.",
            hi: "पवित्र वृक्ष की रक्षा करें या सामरिक मल्टीप्लेयर युद्ध में इसके विशाल तने को गिराएं।",
            zh: "与各方勇士联机作战。在战术对抗中守护圣豆蔓或斩断其参天巨干。",
            ja: "世界中の戦士と共闘。神木を守り抜くか、巨躯を切り倒すか、白熱の対戦へ。",
            pt: "Una-se a aliados online. Defenda a árvore sagrada ou destrua seu tronco colossal.",
            fa: "با جنگجویان سراسر قلمروها در نبرد شبکه‌ای زنده متحد شوید. از ساقه مقدس دفاع کنید یا تنه غول‌پیکر آن را واژگون سازید.",
            it: "Unisciti ai guerrieri di regni lontani in battaglie di rete dal vivo. Difendi il Sacro Fagiolo o abbatti il suo colossale tronco.",
            tr: "Uzak diyarlardaki savaşçılarla canlı ağ savaşında birleşin. Kutsal Fasulye Sırığını savunun veya devasa gövdesini devirin."
        );

        string c2Btn = LanguageSelectionUI.Localize("⚔️ PLAY ONLINE",
            ar: "⚔️ دخول أونلاين",
            ru: "⚔️ ВОЙТИ В ОНЛАЙН",
            es: "⚔️ ENTRAR ONLINE",
            fr: "⚔️ ENTRER EN LIGNE",
            de: "⚔️ ONLINE BEITRETEN",
            hi: "⚔️ ऑनलाइन प्रवेश करें",
            zh: "⚔️ 进入在线对战",
            ja: "⚔️ オンライン出撃",
            pt: "⚔️ ENTRAR ONLINE",
            fa: "⚔️ ورود آنلاین",
            it: "⚔️ ENTRA ONLINE",
            tr: "⚔️ ÇEVRİMİÇİ GİR");

        currentModeCards[1] = new ModeCardData {
            index = 1,
            accent = EnchantedNatureGUI.AmberSolarAccent,
            badge = c2Badge,
            title = c2Title,
            sub = c2Sub,
            desc = c2Desc,
            highlights = c2Highlights,
            btnText = c2Btn,
            onClick = () => { inOnlineSubmenu = true; }
        };

        DrawVerticalPortalCard(c2X, cardY, cardW, cardH, currentModeCards[1]);

        // ==========================================
        // PORTAL 3: TRIBE SANCTUARY (FRIENDLY LOBBY)
        // ==========================================
        float c3X = c2X + cardW + gap;
        string c3Badge = LanguageSelectionUI.Localize("🤝 COVEN",
            ar: "🤝 الأصدقاء",
            ru: "🤝 ДРУЗЬЯ",
            es: "🤝 AMIGOS",
            fr: "🤝 AMIS",
            de: "🤝 FREUNDE",
            hi: "🤝 मित्र",
            zh: "🤝 好友",
            ja: "🤝 フレンド",
            pt: "🤝 AMIGOS",
            fa: "🤝 دوستان",
            it: "🤝 AMICI",
            tr: "🤝 ARKADAŞLAR");

        string c3Title = LanguageSelectionUI.Localize("PLAY WITH FRIENDS",
            ar: "اللعب مع الأصدقاء",
            ru: "ИГРА С ДРУЗЬЯМИ",
            es: "JUGAR CON AMIGOS",
            fr: "JOUER AVEC DES AMIS",
            de: "MIT FREUNDEN SPIELEN",
            hi: "दोस्तों के साथ खेलें",
            zh: "与好友同玩",
            ja: "友達とプレイ",
            pt: "JOGAR COM AMIGOS",
            fa: "بازی با دوستان",
            it: "GIOCA CON GLI AMICI",
            tr: "ARKADAŞLARLA OYNA");

        string c3Sub = LanguageSelectionUI.Localize("Private Room with Secret Code",
            ar: "غرفة خاصة مخصصة برمز سري",
            ru: "Своя комната с секретным кодом",
            es: "Sala privada con código secreto",
            fr: "Salon privé avec code secret",
            de: "Eigener Raum mit Geheimcode",
            hi: "गुप्त कोड के साथ निजी कमरा",
            zh: "带密码的专属好友房间",
            ja: "秘密コード付きのプライベートルーム",
            pt: "Sala privada com código secreto",
            fa: "اتاق اختصاصی با رمز محرمانه",
            it: "Stanza privata con codice segreto",
            tr: "Gizli Kodlu Özel Oda");

        string[] c3Highlights = new string[]
        {
            LanguageSelectionUI.Localize("🤝 Secret Room Cipher Code", ar: "🤝 رمز سري خاص للغرفة", ru: "🤝 Секретный код комнаты", es: "🤝 Código secreto de sala", fr: "🤝 Code privé sécurisé", de: "🤝 Geheimer Raum-Code", hi: "🤝 गुप्त रूम कोड", zh: "🤝 专属私密房间密码", ja: "🤝 秘密のルームコード", pt: "🤝 Código secreto da sala", fa: "🤝 رمز مخفی اتاق", it: "🤝 Codice segreto della stanza", tr: "🤝 Gizli Oda Şifresi"),
            LanguageSelectionUI.Localize("⚖️ Custom Team Distribution", ar: "⚖️ توزيع الحلفاء والفرق", ru: "⚖️ Ручное распределение команд", es: "⚖️ División de equipos a medida", fr: "⚖️ Répartition libre des équipes", de: "⚖️ Freie Team-Einteilung", hi: "⚖️ कस्टम टीम आवंटन", zh: "⚖️ 自由阵营与队伍分配", ja: "⚖️ 自由なチーム編成", pt: "⚖️ Divisão livre de equipes", fa: "⚖️ توزیع سفارشی تیم‌ها", it: "⚖️ Distribuzione libera squadre", tr: "⚖️ Özel Takım Dağılımı"),
            LanguageSelectionUI.Localize("🤖 Fill Roster With AI Bots", ar: "🤖 ملء المقاعد بالبوتات", ru: "🤖 Заполнение слотов ботами", es: "🤖 Relleno con bots IA", fr: "🤖 Remplissage par bots IA", de: "🤖 Mit KI-Bots auffüllen", hi: "🤖 बॉट्स से सीटें भरें", zh: "🤖 支持AI人机补位", ja: "🤖 AIボット補充対応", pt: "🤖 Preencher vagas com bots", fa: "🤖 پر کردن ظرفیت با بات‌های هوشمند", it: "🤖 Riempi posti con bot IA", tr: "🤖 Boşlukları Yapay Zeka ile Doldur"),
            LanguageSelectionUI.Localize("🛡️ Practice Tactics & Duels", ar: "🛡️ تجربة خطط ومبارزات ودية", ru: "🛡️ Тренировка дуэлей и тактик", es: "🛡️ Duelos y tácticas amistosas", fr: "🛡️ Duels et tactiques amicales", de: "🛡️ Taktik-Training & Duelle", hi: "🛡️ अभ्यास और दोस्ताना मैच", zh: "🛡️ 演练战术与好友对决", ja: "🛡️ 戦術練習とフレンド対戦", pt: "🛡️ Treino tático e duelos", fa: "🛡️ تمرین تاکتیک‌ها و دوئل دوستانه", it: "🛡️ Allena tattiche e duelli", tr: "🛡️ Taktik ve Düello Çalışması")
        };

        string c3Desc = LanguageSelectionUI.Localize(
            "Gather thy chosen companions under a secret room cipher. Set custom rules, assign teams, and enjoy private battles.",
            ar: "اجمع أصدقاءك برمز غرفة خاص، قسّم الفرق بين حماة الشجرة ومدمرِيها مع تخصيص القواعد وإضافة البوتات.",
            ru: "Соберите друзей по секретному коду комнаты. Разделитесь на команды и играйте по своим правилам.",
            es: "Reúne a tus amigos con un código secreto. Divide equipos y disfruta de partidas privadas.",
            fr: "Rassemblez vos compagnons avec un code privé. Répartissez les équipes avec règles personnalisées.",
            de: "Versammelt Freunde mit einem Geheimcode. Teilt Teams ein und kämpft nach eigenen Regeln.",
            hi: "गुप्त कमरे के कोड के साथ मित्रों को इकट्ठा करें और निजी लड़ाइयों का आनंद लें।",
            zh: "凭借私密房间码召集好友。在守护者与破坏者间自由分配阵营并设定对战规则。",
            ja: "秘密のルームコードで仲間を招集。自由なルールと編成で親善試合を楽しもう。",
            pt: "Reúna seus amigos com um código privado. Divida as equipes e jogue partidas privadas.",
            fa: "دوستان خود را با یک رمز مخفی گرد هم آورید، تیم‌ها را میان حامیان و ویرانگران تقسیم کنید و با قوانین سفارشی مبارزه نمایید.",
            it: "Raduna i tuoi compagni con un codice segreto. Imposta regole personalizzate, assegna squadre e goditi battaglie private.",
            tr: "Gizli oda şifresiyle arkadaşlarınızı toplayın. Takımları Koruyucular ve Yok Ediciler olarak ayırın, özel kurallarla savaşın."
        );

        string c3Btn = LanguageSelectionUI.Localize("🤝 PLAY WITH FRIENDS",
            ar: "🤝 اللعب مع الأصدقاء",
            ru: "🤝 ИГРА С ДРУЗЬЯМИ",
            es: "🤝 JUGAR CON AMIGOS",
            fr: "🤝 JOUER AVEC DES AMIS",
            de: "🤝 MIT FREUNDEN SPIELEN",
            hi: "दोस्तों के साथ खेलें",
            zh: "🤝 与好友同玩",
            ja: "🤝 友達とプレイ",
            pt: "🤝 JOGAR COM AMIGOS",
            fa: "🤝 بازی با دوستان",
            it: "🤝 GIOCA CON GLI AMICI",
            tr: "🤝 ARKADAŞLARLA OYNA");

        currentModeCards[2] = new ModeCardData {
            index = 2,
            accent = EnchantedNatureGUI.BronzeSanctuary,
            badge = c3Badge,
            title = c3Title,
            sub = c3Sub,
            desc = c3Desc,
            highlights = c3Highlights,
            btnText = c3Btn,
            onClick = () => StartFriendlyMode()
        };

        DrawVerticalPortalCard(c3X, cardY, cardW, cardH, currentModeCards[2]);

        // 5. Sleek Footer Bar
        float footerY = modalY + modalHeight - 32f;
        GUIStyle footerStyle = new GUIStyle(GUI.skin.label);
        footerStyle.fontSize = 10;
        footerStyle.alignment = TextAnchor.MiddleCenter;
        footerStyle.normal.textColor = new Color(0.55f, 0.70f, 0.60f, 0.85f);
        string footerMsg = "Beanstalkwar v1.0.4 - Sacred Tree Sanctuary  |  [Press L for Languages | Esc to Pause]";
        GUI.Label(new Rect(modalX, footerY, modalWidth, 22f), footerMsg, footerStyle);

        // 6. Mode Info Popup Modal (if opened by clicking ❓ button)
        if (activeModeInfoModal >= 0 && activeModeInfoModal < currentModeCards.Length)
        {
            DrawModeInfoModal(currentModeCards[activeModeInfoModal]);
        }
    }

    /// <summary>
    /// Renders a monolithic AAA vertical portal card with smooth hover easing,
    /// vertical lift, heraldic mode crest, typography hierarchy, question mark specs button, and action button.
    /// </summary>
    void DrawVerticalPortalCard(float x, float y, float w, float h, ModeCardData data)
    {
        int elementId = (int)(x * 777f + y);
        bool isHover = new Rect(x, y, w, h).Contains(Event.current.mousePosition);
        float hover = EnchantedNatureGUI.GetSmoothHover(elementId, isHover, 9f);

        // Smooth vertical lift on hover (-4 pixels)
        float liftY = hover * -4f;
        Rect cardRect = new Rect(x, y + liftY, w, h);

        // 1. Multi-layer Antique Gold & Obsidian Glass Frame
        EnchantedNatureGUI.DrawAntiqueGoldFrame(cardRect, data.accent, hover);

        // 2. Top Pill Badge Banner
        float badgeW = w - 44f;
        float badgeH = 22f;
        float badgeX = x + 22f;
        float badgeY = y + liftY + 12f;

        GUIStyle badgeStyle = new GUIStyle(GUI.skin.label);
        badgeStyle.fontSize = 10;
        badgeStyle.fontStyle = FontStyle.Bold;
        badgeStyle.alignment = TextAnchor.MiddleCenter;

        Color badgeBg = Color.Lerp(
            new Color(data.accent.r * 0.16f, data.accent.g * 0.16f, data.accent.b * 0.16f, 0.90f),
            new Color(data.accent.r * 0.32f, data.accent.g * 0.32f, data.accent.b * 0.32f, 0.95f),
            hover
        );
        EnchantedNatureGUI.DrawLeafPlaque(new Rect(badgeX, badgeY, badgeW, badgeH), badgeBg, data.accent);
        badgeStyle.normal.textColor = Color.Lerp(data.accent, Color.white, hover * 0.4f);
        GUI.Label(new Rect(badgeX, badgeY, badgeW, badgeH), data.badge, badgeStyle);

        // 3. Heraldic Mode Crest (72x72)
        float crestSize = 72f;
        Rect crestRect = new Rect(x + (w - crestSize) / 2f, y + liftY + 44f, crestSize, crestSize);
        EnchantedNatureGUI.DrawHeraldicCrest(crestRect, data.index, data.accent, hover);

        // 4. Portal Title
        GUIStyle cardTitleStyle = new GUIStyle(GUI.skin.label);
        cardTitleStyle.fontSize = 15;
        cardTitleStyle.fontStyle = FontStyle.Bold;
        cardTitleStyle.alignment = TextAnchor.MiddleCenter;
        Color titleColor = Color.Lerp(EnchantedNatureGUI.AntiqueGoldBright, Color.white, hover * 0.3f);
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(x + 10f, y + liftY + 126f, w - 20f, 26f),
            data.title,
            cardTitleStyle,
            titleColor,
            new Color(0.02f, 0.04f, 0.02f, 0.95f),
            1f, 1f
        );

        // 5. Portal Subtitle
        GUIStyle cardSubStyle = new GUIStyle(GUI.skin.label);
        cardSubStyle.fontSize = 11;
        cardSubStyle.fontStyle = FontStyle.Italic;
        cardSubStyle.alignment = TextAnchor.MiddleCenter;
        Color subColor = Color.Lerp(new Color(0.72f, 0.88f, 0.78f), Color.white, hover * 0.4f);
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(x + 10f, y + liftY + 154f, w - 20f, 20f),
            data.sub,
            cardSubStyle,
            subColor,
            new Color(0.02f, 0.04f, 0.02f, 0.85f),
            1f, 1f
        );

        // 6. Flourish Divider
        EnchantedNatureGUI.DrawFlourishDivider(new Rect(x + 22f, y + liftY + 180f, w - 44f, 8f), data.accent);

        // 7. Middle Section: Mode Details Question-Mark Button (Replaces cluttered bullets & long paragraph!)
        float midBoxY = y + liftY + 204f;
        float midBoxW = w - 36f;
        float midBoxX = x + 18f;

        string detailsBtnText = LanguageSelectionUI.Localize(
            "❓ [ MODE DETAILS & SPECS ]",
            ar: "❓ [ كَافَّةُ المُمَيِّزَاتِ وَالتَّفَاصِيلِ ]",
            fa: "❓ [ مشخصات و جزئیات کامل ]",
            ru: "❓ [ ОПИСАНИЕ И ХАРАКТЕРИСТИКИ ]",
            es: "❓ [ DETALLES Y CARACTERÍSTICAS ]",
            fr: "❓ [ DÉTAILS ET SPÉCIFICATIONS ]",
            de: "❓ [ DETAILS & REGELN ANSEHEN ]",
            it: "❓ [ DETTAGLI E SPECIFICHE ]",
            tr: "❓ [ MOD DETAYLARI VE BİLGİ ]",
            zh: "❓ [ 查看模式特色与规则详情 ]",
            ja: "❓ [ モード詳細とルールを見る ]",
            hi: "❓ [ मोड विवरण और नियम देखें ]",
            pt: "❓ [ DETALHES E ESPECIFICAÇÕES ]"
        );

        float infoBtnH = 42f;
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(midBoxX, midBoxY, midBoxW, infoBtnH), detailsBtnText, data.accent, 11))
        {
            activeModeInfoModal = data.index;
        }

        string infoHint = LanguageSelectionUI.Localize(
            "Click for rules, titans & battle tactics",
            ar: "انقر لمعرفة القوانين والزعماء والخطط",
            fa: "برای مشاهده قوانین و غول‌ها کلیک کنید",
            ru: "Нажмите для просмотра правил и тактик",
            es: "Clic para ver reglas, titanes y tácticas",
            fr: "Cliquez pour règles, titans et tactiques",
            de: "Klicke für Regeln, Bosse und Taktiken",
            it: "Clicca per regole, titani e tattiche",
            tr: "Kurallar, titanlar ve taktikler için tıkla",
            zh: "点击查看对战规则、巨像首领与战术",
            ja: "ルール、ボス、戦術の詳細を確認",
            hi: "नियम, टाइटन्स और रणनीतियों के लिए क्लिक करें",
            pt: "Clique para regras, titãs e táticas"
        );
        GUIStyle infoHintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.68f, 0.82f, 0.72f, 0.85f) }
        };
        GUI.Label(new Rect(midBoxX, midBoxY + infoBtnH + 8f, midBoxW, 20f), infoHint, infoHintStyle);

        // 8. Bottom Action Button
        float btnW = w - 32f;
        float btnH = 48f;
        float btnX = x + 16f;
        float btnY = y + liftY + h - btnH - 18f;

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(btnX, btnY, btnW, btnH), data.btnText, data.accent, 13, isMajor: (data.index == 1)))
        {
            data.onClick?.Invoke();
        }
    }

    /// <summary>
    /// Renders an elegant popup dialog showing comprehensive narrative description,
    /// complete mechanics, and 4 highlight specifications for the selected mode.
    /// </summary>
    void DrawModeInfoModal(ModeCardData data)
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Dark dimming overlay behind modal
        GUI.depth = -100;
        GUI.color = new Color(0.01f, 0.03f, 0.02f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float mw = Mathf.Min(680f, sw - 24f);
        float mh = Mathf.Min(560f, sh - 24f);
        float mx = (sw - mw) * 0.5f;
        float my = (sh - mh) * 0.5f;
        Rect modalRect = new Rect(mx, my, mw, mh);

        // Master antique gold frame
        EnchantedNatureGUI.DrawAntiqueGoldFrame(modalRect, data.accent, 0.5f);

        // Close button (Top-Right)
        float closeBtnSize = 34f;
        Rect closeRect = new Rect(mx + mw - closeBtnSize - 14f, my + 12f, closeBtnSize, closeBtnSize);
        if (EnchantedNatureGUI.DrawAncientGoldButton(closeRect, "✕", EnchantedNatureGUI.AntiqueGoldBright, 13))
        {
            activeModeInfoModal = -1;
        }

        // Header Mode Title
        GUIStyle mTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(mx + 40f, my + 14f, mw - 80f, 28f),
            data.title,
            mTitleStyle,
            EnchantedNatureGUI.AntiqueGoldBright,
            new Color(0.02f, 0.04f, 0.02f, 0.95f),
            1.5f, 1.5f
        );

        // Header Subtitle
        GUIStyle mSubStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter
        };
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(mx + 40f, my + 42f, mw - 80f, 20f),
            data.sub,
            mSubStyle,
            new Color(0.40f, 0.95f, 0.85f),
            new Color(0.01f, 0.03f, 0.02f, 0.95f),
            1f, 1f
        );

        // Flourish divider
        EnchantedNatureGUI.DrawFlourishDivider(new Rect(mx + 30f, my + 64f, mw - 60f, 8f), data.accent);

        // Narrative Description Plaque
        float descY = my + 76f;
        float descH = 68f;
        Rect descBox = new Rect(mx + 24f, descY, mw - 48f, descH);
        EnchantedNatureGUI.DrawLeafPlaque(descBox, new Color(0.04f, 0.09f, 0.06f, 0.95f), data.accent);
        GUIStyle descStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.92f, 0.96f, 0.92f) }
        };
        GUI.Label(new Rect(mx + 34f, descY + 6f, mw - 68f, descH - 12f), data.desc, descStyle);

        // Highlights Section Title
        string specTitle = LanguageSelectionUI.Localize(
            "⚜️ MODE HIGHLIGHTS & SPECIFICATIONS ⚜️",
            ar: "⚜️ خَصَائِصُ وَمُمَيِّزَاتُ هَذَا الطَّوْرِ ⚜️",
            fa: "⚜️ مشخصات و ویژگی‌های این حالت ⚜️",
            ru: "⚜️ ОСОБЕННОСТИ И ХАРАКТЕРИСТИКИ РЕЖИМА ⚜️",
            es: "⚜️ CARACTERÍSTICAS Y ESPECIFICACIONES ⚜️",
            fr: "⚜️ CARACTÉRISTIQUES ET SPÉCIFICATIONS ⚜️",
            de: "⚜️ BESONDERHEITEN & SPEZIFIKATIONEN ⚜️",
            it: "⚜️ CARATTERISTICHE E SPECIFICHE ⚜️",
            tr: "⚜️ MOD ÖZELLİKLERİ VE AYRINTILARI ⚜️",
            zh: "⚜️ 模式核心特色与系统规则 ⚜️",
            ja: "⚜️ モードの特徴と詳細仕様 ⚜️",
            hi: "⚜️ मोड की मुख्य विशेषताएं और नियम ⚜️",
            pt: "⚜️ DESTAQUES E ESPECIFICAÇÕES DO MODO ⚜️"
        );
        GUIStyle specTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = EnchantedNatureGUI.AntiqueGoldBright }
        };
        GUI.Label(new Rect(mx + 24f, descY + descH + 10f, mw - 48f, 22f), specTitle, specTitleStyle);

        // Highlights Items (4 items)
        float hlStartY = descY + descH + 34f;
        if (data.highlights != null)
        {
            for (int i = 0; i < data.highlights.Length; i++)
            {
                Rect hlRect = new Rect(mx + 24f, hlStartY + (i * 42f), mw - 48f, 36f);
                GUI.color = new Color(data.accent.r * 0.12f, data.accent.g * 0.12f, data.accent.b * 0.12f, 0.85f);
                GUI.DrawTexture(hlRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                EnchantedNatureGUI.DrawCrispBorder(hlRect, new Color(data.accent.r, data.accent.g, data.accent.b, 0.45f), 1.2f);

                GUIStyle hlStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = LanguageSelectionUI.IsArabic ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.95f, 0.98f, 0.95f) }
                };
                GUI.Label(new Rect(mx + 38f, hlStartY + (i * 42f) + 4f, mw - 76f, 28f), data.highlights[i], hlStyle);
            }
        }

        // Bottom Action Buttons
        float btnY = my + mh - 60f;
        float launchBtnW = (mw - 60f) * 0.65f;
        float closeBtnW = (mw - 60f) * 0.35f;

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(mx + 24f, btnY, launchBtnW, 46f), data.btnText, data.accent, 13, isMajor: true))
        {
            activeModeInfoModal = -1;
            data.onClick?.Invoke();
        }

        string closeTxt = LanguageSelectionUI.Localize(
            "✕ CLOSE",
            ar: "✕ إِغْلاَق",
            fa: "✕ بستن",
            ru: "✕ ЗАКРЫТЬ",
            es: "✕ CERRAR",
            fr: "✕ FERMER",
            de: "✕ SCHLIESSEN",
            it: "✕ CHIUDI",
            tr: "✕ KAPAT",
            zh: "✕ 关闭",
            ja: "✕ 閉じる",
            hi: "✕ बंद करें",
            pt: "✕ FECHAR"
        );
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(mx + 24f + launchBtnW + 12f, btnY, closeBtnW, 46f), closeTxt, EnchantedNatureGUI.AntiqueGoldMid, 12))
        {
            activeModeInfoModal = -1;
        }

        // Escape key to close modal
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            activeModeInfoModal = -1;
            Event.current.Use();
        }
    }

    void DrawOnlineSubmenu()
    {
        float modalWidth = Mathf.Min(780f, Screen.width - 24f);
        float modalHeight = Mathf.Min(480f, Screen.height - 24f);
        float modalX = (Screen.width - modalWidth) / 2f;
        float modalY = Mathf.Max(20f, (Screen.height - modalHeight) / 2f);

        // Master Obsidian & Antique Gold Frame
        EnchantedNatureGUI.DrawAntiqueGoldFrame(new Rect(modalX, modalY, modalWidth, modalHeight), EnchantedNatureGUI.CyanRuneAccent, 0f);

        string titleText = LanguageSelectionUI.Localize("🌊 ONLINE FOREST REALM SETUP",
            ar: "🌊 إعدادات الاتصال أونلاين",
            ru: "🌊 НАСТРОЙКА ОНЛАЙН СЕРВЕРА",
            es: "🌊 CONFIGURACIÓN DEL REINO ONLINE",
            fr: "🌊 CONFIGURATION DU ROYAUME EN LIGNE",
            de: "🌊 ONLINE-WALDREICH-EINSTELLUNGEN",
            hi: "🌊 ऑनलाइन वन क्षेत्र सेटअप",
            zh: "🌊 在线森林领域设置",
            ja: "🌊 オンライン領域の設定",
            pt: "🌊 CONFIGURAÇÃO DE REINO ONLINE",
            fa: "🌊 تنظیمات اتصال آنلاین قلمرو جنگل",
            it: "🌊 CONFIGURAZIONE REGNO DELLA FORESTA ONLINE",
            tr: "🌊 ÇEVRİMİÇİ ORMAN DİYARI AYARLARI");

        string subTitleText = LanguageSelectionUI.Localize("~ Awaken a Wild Sanctuary or Step Across Distant Rivers ~",
            ar: "~ أنشئ خادمًا جديدًا أو انضم إلى معركة عبر الشبكة ~",
            ru: "~ Создайте священную комнату или подключитесь по сети ~",
            es: "~ Despierta un santuario o viaja a través de ríos lejanos ~",
            fr: "~ Réveillez un sanctuaire ou franchissez des fleuves lointains ~",
            de: "~ Erweckt ein Heiligtum oder schreitet über ferne Flüsse ~",
            hi: "~ एक अभयारण्य बनाएं या दूर की नदियों के पार कदम रखें ~",
            zh: "~ 唤醒狂野圣所，或跨越遥远之河 ~",
            ja: "~ 荒野の聖域を開くか、遥かなる大河を渡れ ~",
            pt: "~ Desperte um santuário ou atravesse rios distantes ~",
            fa: "~ یک پناهگاه باستانی برپا کنید یا از رودهای دوردست بگذرید ~",
            it: "~ Risveglia un santuario selvaggio o varca fiumi lontani ~",
            tr: "~ Vahşi bir sığınak kur veya uzak nehirleri aş ~");

        string hostTitle = LanguageSelectionUI.Localize("👑 HOST A FOREST REALM (SERVER)",
            ar: "👑 استضافة سيرفر للغابة (مضيف)",
            ru: "👑 СОЗДАТЬ СЕРВЕР (ХОСТ)",
            es: "👑 CREAR SERVIDOR (HOST)",
            fr: "👑 HÉBERGER UN SERVEUR (HÔTE)",
            de: "👑 WALDREICH BEHERBERGEN (SERVER)",
            hi: "👑 वन क्षेत्र होस्ट करें (सर्वर)",
            zh: "👑 创立森林领域 (主机)",
            ja: "👑 サーバーをホストする",
            pt: "👑 HOSPEDAR REINO (SERVIDOR)",
            fa: "👑 میزبانی سرور جنگل (میزبان)",
            it: "👑 CREA UN REGNO DELLA FORESTA (SERVER)",
            tr: "👑 ORMAN DİYARI OLUŞTUR (SUNUCU)");

        string hostDesc = LanguageSelectionUI.Localize("Awaken the ancient grove on your device. Other warriors can connect directly to your forest using your IP address.",
            ar: "افتح عالم الغابة على جهازك ليتمكن اللاعبون الآخرون من الاتصال بك مباشرة عبر الـ IP.",
            ru: "Запустите сервер на своем устройстве. Другие воины смогут подключиться напрямую по вашему IP.",
            es: "Despierta el bosque en tu dispositivo. Otros guerreros podrán conectarse directamente con tu dirección IP.",
            fr: "Réveillez le bois sacré sur votre appareil. Les autres guerriers pourront s'y connecter via votre IP.",
            de: "Erweckt den alten Hain auf eurem Gerät. Andere Krieger verbinden sich direkt über eure IP-Adresse.",
            hi: "अपने डिवाइस पर प्राचीन ग्रोव को जगाएं। अन्य योद्धा आपके आईपी पते का उपयोग करके जुड़ सकते हैं।",
            zh: "在您的设备上唤醒古老树林。其他勇士可直接通过您的IP地址连接加入森林战场。",
            ja: "お使いの端末で古代の木立ちを開設。他のプレイヤーがIPアドレスで直接接続可能になります。",
            pt: "Desperte o bosque no seu dispositivo. Outros jogadores podem conectar-se diretamente via seu IP.",
            fa: "بیشه باستانی را روی دستگاه خود برپا کنید. جنگجویان دیگر می‌توانند با نشانی IP مستقیماً به شما متصل شوند.",
            it: "Risveglia l'antico bosco sul tuo dispositivo. Gli altri guerrieri potranno connettersi direttamente tramite il tuo IP.",
            tr: "Kadim koruyu cihazınızda uyandırın. Diğer savaşçılar IP adresiniz üzerinden doğrudan bağlanabilirler.");

        string hostBtn = LanguageSelectionUI.Localize("👑 HOST REALM",
            ar: "👑 استضافة السيرفر",
            ru: "👑 ЗАПУСТИТЬ ХОСТ",
            es: "👑 CREAR SERVIDOR",
            fr: "👑 HÉBERGER",
            de: "👑 REICH STARTEN",
            hi: "👑 सर्वर बनाएं",
            zh: "👑 创建领域",
            ja: "👑 ホスト開始",
            pt: "👑 HOSPEDAR REINO",
            fa: "👑 میزبانی قلمرو",
            it: "👑 CREA SERVER",
            tr: "👑 DİYAR OLUŞTUR");

        string joinTitle = LanguageSelectionUI.Localize("🎮 JOURNEY TO A REMOTE REALM",
            ar: "🎮 الانضمام إلى سيرفر آخر",
            ru: "🎮 ПОДКЛЮЧИТЬСЯ К СЕРВЕРУ",
            es: "🎮 VIAJAR A UN REINO REMOTO",
            fr: "🎮 REJOINDRE UN ROYAUME",
            de: "🎮 ZU EINEM REICH REISEN",
            hi: "🎮 दूर के क्षेत्र में प्रवेश करें",
            zh: "🎮 前往远程领域",
            ja: "🎮 遠隔サーバーへ参加",
            pt: "🎮 JUNTAR-SE A UM REINO",
            fa: "🎮 پیوستن به سرور دیگر (مهمان)",
            it: "🎮 VIAGGIA VERSO UN ALTRO REGNO",
            tr: "🎮 UZAK BİR DİYARA KATIL");

        string joinDesc = LanguageSelectionUI.Localize("Enter the host's sacred IP address to step into their live arena:",
            ar: "أدخل عنوان الـ IP الخاص بالمضيف للدخول في معركتهم المباشرة:",
            ru: "Введите IP-адрес хоста для входа в активную сетевую битву:",
            es: "Introduce la dirección IP del anfitrión para unirte a su batalla:",
            fr: "Entrez l'adresse IP de l'hôte pour entrer dans son arène:",
            de: "Gebt die IP-Adresse des Gastgebers ein, um seiner Arena beizutreten:",
            hi: "लाइव अखाड़े में कदम रखने के लिए होस्ट का आईपी पता दर्ज करें:",
            zh: "输入主机的IP地址以踏入其战局领域：",
            ja: "ホストのIPアドレスを入力してリアルタイムバトルへ参加：",
            pt: "Insira o IP do anfitrião para entrar na arena:",
            fa: "نشانی IP مقدس میزبان را وارد کنید تا وارد میدان نبرد زنده شوید:",
            it: "Inserisci l'indirizzo IP dell'host per entrare nell'arena:",
            tr: "Canlı arenaya girmek için sunucunun kutsal IP adresini girin:");

        string joinBtn = LanguageSelectionUI.Localize("🌊 JOIN REALM",
            ar: "🌊 دخول السيرفر",
            ru: "🌊 ПОДКЛЮЧИТЬСЯ",
            es: "🌊 UNIRSE AL REINO",
            fr: "🌊 REJOINDRE",
            de: "🌊 BEITRETEN",
            hi: "🌊 क्षेत्र में शामिल हों",
            zh: "🌊 加入领域",
            ja: "🌊 参加する",
            pt: "🌊 ENTRAR NO REINO",
            fa: "🌊 ورود به قلمرو",
            it: "🌊 UNISCITI AL REGNO",
            tr: "🌊 DİYARA KATIL");

        string backBtn = LanguageSelectionUI.Localize("⬅️ Return to Gateways",
            ar: "⬅️ رجوع إلى البوابات",
            ru: "⬅️ Вернуться назад",
            es: "⬅️ Volver a portales",
            fr: "⬅️ Retour aux portails",
            de: "⬅️ Zurück zu Toren",
            hi: "⬅️ वापस लौटें",
            zh: "⬅️ 返回大厅",
            ja: "⬅️ 戻る",
            pt: "⬅️ Voltar aos portais",
            fa: "⬅️ بازگشت به دروازه‌ها",
            it: "⬅️ Torna ai portali",
            tr: "⬅️ Geçitlere Dön");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(modalX, modalY + 16f, modalWidth, 26f),
            titleText,
            titleStyle,
            EnchantedNatureGUI.AntiqueGoldBright,
            new Color(0.02f, 0.04f, 0.02f, 0.95f),
            1.5f, 1.5f
        );

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label);
        subTitleStyle.fontSize = 11;
        subTitleStyle.fontStyle = FontStyle.Italic;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(modalX, modalY + 44f, modalWidth, 20f),
            subTitleText,
            subTitleStyle,
            new Color(0.68f, 0.92f, 0.78f),
            new Color(0.02f, 0.04f, 0.02f, 0.85f),
            1f, 1f
        );

        float boxW = modalWidth - 48f;
        float boxX = modalX + 24f;
        float box1Y = modalY + 76f;

        // Host Monolith Card
        EnchantedNatureGUI.DrawAntiqueGoldFrame(new Rect(boxX, box1Y, boxW, 108f), EnchantedNatureGUI.AmberSolarAccent, 0.15f);

        GUIStyle boldStyle = new GUIStyle(GUI.skin.label);
        boldStyle.fontSize = 14;
        boldStyle.fontStyle = FontStyle.Bold;
        boldStyle.normal.textColor = EnchantedNatureGUI.AntiqueGoldBright;

        GUI.Label(new Rect(boxX + 20f, box1Y + 14f, boxW - 210f, 22f), hostTitle, boldStyle);

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.fontSize = 11;
        infoStyle.wordWrap = true;
        infoStyle.normal.textColor = new Color(0.85f, 0.92f, 0.88f);

        GUI.Label(new Rect(boxX + 20f, box1Y + 40f, boxW - 210f, 56f), hostDesc, infoStyle);

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(boxX + boxW - 180f, box1Y + 30f, 160f, 48f), hostBtn, EnchantedNatureGUI.AmberSolarAccent, 13, true))
        {
            StartOnlineHost();
        }

        // Join Monolith Card
        float box2Y = box1Y + 124f;
        EnchantedNatureGUI.DrawAntiqueGoldFrame(new Rect(boxX, box2Y, boxW, 126f), EnchantedNatureGUI.CyanRuneAccent, 0.15f);

        boldStyle.normal.textColor = EnchantedNatureGUI.CyanRuneAccent;
        GUI.Label(new Rect(boxX + 20f, box2Y + 14f, boxW - 210f, 22f), joinTitle, boldStyle);

        GUI.Label(new Rect(boxX + 20f, box2Y + 38f, boxW - 210f, 20f), joinDesc, infoStyle);

        // Sleek Antique Inset IP input field
        Rect ipRect = new Rect(boxX + 20f, box2Y + 68f, 250f, 34f);
        GUI.color = new Color(0.04f, 0.08f, 0.06f, 0.95f);
        GUI.DrawTexture(ipRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        EnchantedNatureGUI.DrawCrispBorder(ipRect, EnchantedNatureGUI.AntiqueGoldMid, 1.5f);

        GUIStyle ipStyle = new GUIStyle(GUI.skin.textField);
        ipStyle.fontSize = 13;
        ipStyle.fontStyle = FontStyle.Bold;
        ipStyle.alignment = TextAnchor.MiddleLeft;
        ipStyle.normal.textColor = Color.white;
        onlineConnectIP = GUI.TextField(new Rect(ipRect.x + 6f, ipRect.y + 2f, ipRect.width - 12f, ipRect.height - 4f), onlineConnectIP, ipStyle);

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(boxX + boxW - 180f, box2Y + 54f, 160f, 48f), joinBtn, EnchantedNatureGUI.CyanRuneAccent, 13, false))
        {
            StartOnlineClient();
        }

        // Back button
        float btmY = modalY + modalHeight - 52f;
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(modalX + (modalWidth - 220f) / 2f, btmY, 220f, 36f), backBtn, Color.white, 12))
        {
            inOnlineSubmenu = false;
        }
    }
}