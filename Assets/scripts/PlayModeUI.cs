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

    void Awake()
    {
        Instance = this;

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

        // Keep background haze strictly behind top bar buttons (which are at depth -100)
        GUI.depth = 10;

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
    // TRIUMVIRATE GATEWAYS (3 VERTICAL AAA MONOLITHS)
    // =========================================================================
    void DrawMainModeSelect()
    {
        // 1. Unified Top Citadel Ribbon - Audio Toggle (Top-Right)
        float audioBtnW = 78f;
        float audioBtnH = 38f;
        float audioX = Screen.width - audioBtnW - 12f;
        float audioY = 8f;
        string audioText = AudioListener.pause ? "🔇 OFF" : "🔊 ON";
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(audioX, audioY, audioBtnW, audioBtnH), audioText, EnchantedNatureGUI.AntiqueGoldMid, 11))
        {
            AudioListener.pause = !AudioListener.pause;
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

        string c1Title = LanguageSelectionUI.Localize("SOLO EXPEDITION",
            ar: "الحملة الفردية",
            ru: "ОДИНОЧНЫЙ БОЙ",
            es: "EXPEDICIÓN INDIVIDUAL",
            fr: "EXPÉDITION SOLO",
            de: "SOLO-EXPEDITION",
            hi: "एकल अभियान",
            zh: "单人离线远征",
            ja: "ソロ遠征",
            pt: "EXPEDIÇÃO INDIVIDUAL",
            fa: "اکسپدیشن انفرادی",
            it: "SPEDIZIONE INDIVIDUALE",
            tr: "TEK KİŞİLİK SEFER");

        string c1Sub = LanguageSelectionUI.Localize("Vs Forest Bot Guardians",
            ar: "ضد حراس الغابة الآليين",
            ru: "Против лесных ботов",
            es: "Contra guardianes bots",
            fr: "Contre gardiens bots",
            de: "Gegen Bot-Wächter",
            hi: "बॉट अभिभावकों के विरुद्ध",
            zh: "对阵森林人机守卫",
            ja: "ボットの守護者と対戦",
            pt: "Contra bots guardiões",
            fa: "در برابر نگهبانان بات بیشه",
            it: "Contro guardiani bot",
            tr: "Orman Bot Muhafızlarına Karşı");

        string[] c1Highlights = new string[]
        {
            LanguageSelectionUI.Localize("⚔️ 3 Guardian Titans", ar: "⚔️ ٣ زعماء عمالقة", ru: "⚔️ 3 титана-босса", es: "⚔️ 3 jefes titanes", fr: "⚔️ 3 titans gardiens", de: "⚔️ 3 Titanen-Bosse", hi: "⚔️ ३ अभिभावक टाइटन", zh: "⚔️ 三大森林巨像Boss", ja: "⚔️ 3体の守護タイタン", pt: "⚔️ 3 chefes titãs", fa: "⚔️ ۳ غول نگهبان", it: "⚔️ 3 titani guardiani", tr: "⚔️ 3 Muhafız Titan"),
            LanguageSelectionUI.Localize("🌲 Chop Tree or Take Crystal", ar: "🌲 اسقط الشجرة أو خذ الكريستال", ru: "🌲 Срубите древо или кристалл", es: "🌲 Tala el árbol o toma el cristal", fr: "🌲 Abattez l'arbre ou cristal", de: "🌲 Baum fällen oder Kristall", hi: "🌲 पेड़ काटें या क्रिस्टल लें", zh: "🌲 伐倒古树或争夺水晶", ja: "🌲 神木伐採または水晶奪回", pt: "🌲 Derrube a árvore ou cristal", fa: "🌲 قطع درخت یا تصاحب کریستال", it: "🌲 Abbatti l'albero o prendi il cristallo", tr: "🌲 Ağacı Devir veya Kristali Al"),
            LanguageSelectionUI.Localize("🛡️ Master 25 Hero Classes", ar: "🛡️ تدرّب على ٢٥ بطلاً", ru: "🛡️ 25 классов героев", es: "🛡️ Domina 25 héroes", fr: "🛡️ Maîtrisez 25 héros", de: "🛡️ Meistert 25 Helden", hi: "🛡️ २५ नायकों में महारत", zh: "🛡️ 驾驭25位传奇英雄", ja: "🛡️ 25人の英雄を極めろ", pt: "🛡️ Domine 25 heróis", fa: "🛡️ تسلط بر ۲۵ قهرمان", it: "🛡️ Domina 25 classi di eroi", tr: "🛡️ 25 Kahraman Sınıfında Ustalaş"),
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

        string c1Btn = LanguageSelectionUI.Localize("🌲 ENTER GROVE",
            ar: "🌲 ابدأ المعركة",
            ru: "🌲 ВОЙТИ В ЛЕС",
            es: "🌲 ENTRAR AL BOSQUE",
            fr: "🌲 ENTRER DANS LE BOIS",
            de: "🌲 HAIN BETRETEN",
            hi: "🌲 ग्रोव में प्रवेश",
            zh: "🌲 进入丛林",
            ja: "🌲 森に入る",
            pt: "🌲 ENTRAR NO BOSQUE",
            fa: "🌲 ورود به بیشه",
            it: "🌲 ENTRA NEL BOSCO",
            tr: "🌲 KORULUĞA GİR");

        DrawVerticalPortalCard(
            c1X, cardY, cardW, cardH,
            0,
            EnchantedNatureGUI.EmeraldAccent,
            c1Badge,
            c1Title,
            c1Sub,
            c1Desc,
            c1Highlights,
            c1Btn,
            () => StartOffline()
        );

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

        string c2Title = LanguageSelectionUI.Localize("REALM WAR",
            ar: "حرب العوالم",
            ru: "ВОЙНА РЕАЛЬМА",
            es: "GUERRA DEL REINO",
            fr: "GUERRE DES ROYAUMES",
            de: "REICH-KRIEG",
            hi: "क्षेत्र युद्ध",
            zh: "全域远征",
            ja: "領域大戦",
            pt: "GUERRA DO REINO",
            fa: "نبرد قلمروها",
            it: "GUERRA DEI REGNI",
            tr: "DİYAR SAVAŞI");

        string c2Sub = LanguageSelectionUI.Localize("Global Multiplayer Arena",
            ar: "معارك حية متعددة اللاعبين",
            ru: "Сетевая PvP-арена",
            es: "Arena multijugador global",
            fr: "Arène multijoueur en ligne",
            de: "Globale Mehrspieler-Arena",
            hi: "ग्लोबल मल्टीप्लेयर अखाड़ा",
            zh: "跨服多人实时联机竞技场",
            ja: "グローバルマルチプレイ闘技場",
            pt: "Arena multijugador global",
            fa: "میدان چندنفره جهانی",
            it: "Arena Multigiocatore Globale",
            tr: "Küresel Çok Oyunculu Arena");

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

        string c2Btn = LanguageSelectionUI.Localize("⚔️ ENTER REALM",
            ar: "⚔️ دخول أونلاين",
            ru: "⚔️ ИГРАТЬ ОНЛАЙН",
            es: "⚔️ ENTRAR ONLINE",
            fr: "⚔️ JOUER EN LIGNE",
            de: "⚔️ REICH BETRETEN",
            hi: "⚔️ क्षेत्र में प्रवेश",
            zh: "⚔️ 奔赴战场",
            ja: "⚔️ オンライン出撃",
            pt: "⚔️ ENTRAR NO REINO",
            fa: "⚔️ ورود به قلمرو",
            it: "⚔️ ENTRA NEL REGNO",
            tr: "⚔️ DİYARA GİR");

        DrawVerticalPortalCard(
            c2X, cardY, cardW, cardH,
            1,
            EnchantedNatureGUI.AmberSolarAccent,
            c2Badge,
            c2Title,
            c2Sub,
            c2Desc,
            c2Highlights,
            c2Btn,
            () => { inOnlineSubmenu = true; }
        );

        // ==========================================
        // PORTAL 3: TRIBE SANCTUARY (FRIENDLY LOBBY)
        // ==========================================
        float c3X = c2X + cardW + gap;
        string c3Badge = LanguageSelectionUI.Localize("🛡️ PRIVATE COVEN",
            ar: "🛡️ تحالف سري",
            ru: "🛡️ СВОЙ КЛАН",
            es: "🛡️ COFRADÍA SAGRADA",
            fr: "🛡️ ALLIANCE PRIVÉE",
            de: "🛡️ HEILIGER ZIRKEL",
            hi: "🛡️ पवित्र गठबंधन",
            zh: "🛡️ 私密部落",
            ja: "🛡️ 秘密の盟約",
            pt: "🛡️ CLÃ PRIVADO",
            fa: "🛡️ محفل اختصاصی",
            it: "🛡️ CONGREGA PRIVATA",
            tr: "🛡️ ÖZEL BİRLİK");

        string c3Title = LanguageSelectionUI.Localize("TRIBE SANCTUARY",
            ar: "ملاذ العشيرة",
            ru: "ДРУЖЕСКОЕ ЛОББИ",
            es: "SANTUARIO DE AMIGOS",
            fr: "SANCTUAIRE D'AMIS",
            de: "STAMMESHEILIGTUM",
            hi: "मित्र अभयारण्य",
            zh: "部落圣所",
            ja: "仲間たちの聖域",
            pt: "SANTUÁRIO DA TRIBO",
            fa: "پناهگاه قبیله",
            it: "SANTUARIO DELLA TRIBÙ",
            tr: "KABİLE SIĞINAĞI");

        string c3Sub = LanguageSelectionUI.Localize("Custom Private Lobby",
            ar: "غرفة خاصة مخصصة للأصدقاء",
            ru: "Своя комната с друзьями",
            es: "Lobby privado con amigos",
            fr: "Salon privé personnalisé",
            de: "Eigene Freundes-Lobby",
            hi: "कस्टम निजी लॉबी",
            zh: "自定义好友私密房间",
            ja: "カスタムフレンドロビー",
            pt: "Lobby privado personalizado",
            fa: "لابی خصوصی سفارشی",
            it: "Lobby Privata Personalizzata",
            tr: "Özel Kabile Odası");

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

        string c3Btn = LanguageSelectionUI.Localize("🤝 GATHER TRIBE",
            ar: "🤝 إنشاء اللوبي",
            ru: "🤝 СОЗДАТЬ ЛОББИ",
            es: "🤝 CREAR LOBBY",
            fr: "🤝 CRÉER LE SALON",
            de: "🤝 STAMM VERSAMMELN",
            hi: "🤝 कबीला इकट्ठा करें",
            zh: "🤝 召集部落",
            ja: "🤝 仲間を集める",
            pt: "🤝 REUNIR A TRIBO",
            fa: "🤝 تشکیل لابی",
            it: "🤝 RIUNISCI LA TRIBÙ",
            tr: "🤝 KABİLEYİ TOPLA");

        DrawVerticalPortalCard(
            c3X, cardY, cardW, cardH,
            2,
            EnchantedNatureGUI.BronzeSanctuary,
            c3Badge,
            c3Title,
            c3Sub,
            c3Desc,
            c3Highlights,
            c3Btn,
            () => StartFriendlyMode()
        );

        // 5. Sleek Footer Bar
        float footerY = modalY + modalHeight - 32f;
        GUIStyle footerStyle = new GUIStyle(GUI.skin.label);
        footerStyle.fontSize = 10;
        footerStyle.alignment = TextAnchor.MiddleCenter;
        footerStyle.normal.textColor = new Color(0.55f, 0.70f, 0.60f, 0.85f);
        string footerMsg = "Beanstalkwar v1.0.4 - Sacred Tree Sanctuary  |  [Press L for Languages | Esc to Pause]";
        GUI.Label(new Rect(modalX, footerY, modalWidth, 22f), footerMsg, footerStyle);
    }

    /// <summary>
    /// Renders a monolithic AAA vertical portal card with smooth hover easing,
    /// vertical lift, heraldic mode crest, typography hierarchy, and beveled action button.
    /// </summary>
    void DrawVerticalPortalCard(
        float x, float y, float w, float h,
        int portalIndex,
        Color accent,
        string badge,
        string title,
        string sub,
        string desc,
        string[] highlights,
        string btnText,
        System.Action onClick
    )
    {
        int elementId = (int)(x * 777f + y);
        bool isHover = new Rect(x, y, w, h).Contains(Event.current.mousePosition);
        float hover = EnchantedNatureGUI.GetSmoothHover(elementId, isHover, 9f);

        // Smooth vertical lift on hover (-4 pixels)
        float liftY = hover * -4f;
        Rect cardRect = new Rect(x, y + liftY, w, h);

        // 1. Multi-layer Antique Gold & Obsidian Glass Frame
        EnchantedNatureGUI.DrawAntiqueGoldFrame(cardRect, accent, hover);

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
            new Color(accent.r * 0.16f, accent.g * 0.16f, accent.b * 0.16f, 0.90f),
            new Color(accent.r * 0.32f, accent.g * 0.32f, accent.b * 0.32f, 0.95f),
            hover
        );
        EnchantedNatureGUI.DrawLeafPlaque(new Rect(badgeX, badgeY, badgeW, badgeH), badgeBg, accent);
        badgeStyle.normal.textColor = Color.Lerp(accent, Color.white, hover * 0.4f);
        GUI.Label(new Rect(badgeX, badgeY, badgeW, badgeH), badge, badgeStyle);

        // 3. Heraldic Mode Crest (72x72)
        float crestSize = 72f;
        Rect crestRect = new Rect(x + (w - crestSize) / 2f, y + liftY + 40f, crestSize, crestSize);
        EnchantedNatureGUI.DrawHeraldicCrest(crestRect, portalIndex, accent, hover);

        // 4. Portal Title
        GUIStyle cardTitleStyle = new GUIStyle(GUI.skin.label);
        cardTitleStyle.fontSize = 14;
        cardTitleStyle.fontStyle = FontStyle.Bold;
        cardTitleStyle.alignment = TextAnchor.MiddleCenter;
        Color titleColor = Color.Lerp(EnchantedNatureGUI.AntiqueGoldBright, Color.white, hover * 0.3f);
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(x + 10f, y + liftY + 118f, w - 20f, 22f),
            title,
            cardTitleStyle,
            titleColor,
            new Color(0.02f, 0.04f, 0.02f, 0.95f),
            1f, 1f
        );

        // 5. Portal Subtitle
        GUIStyle cardSubStyle = new GUIStyle(GUI.skin.label);
        cardSubStyle.fontSize = 10;
        cardSubStyle.fontStyle = FontStyle.Italic;
        cardSubStyle.alignment = TextAnchor.MiddleCenter;
        Color subColor = Color.Lerp(new Color(0.72f, 0.88f, 0.78f), Color.white, hover * 0.4f);
        EnchantedNatureGUI.DrawTextWithShadow(
            new Rect(x + 10f, y + liftY + 138f, w - 20f, 18f),
            sub,
            cardSubStyle,
            subColor,
            new Color(0.02f, 0.04f, 0.02f, 0.85f),
            1f, 1f
        );

        // 6. Flourish Divider
        EnchantedNatureGUI.DrawFlourishDivider(new Rect(x + 22f, y + liftY + 158f, w - 44f, 8f), accent);

        // 7. Feature Highlights Bullets
        float hlY = y + liftY + 172f;
        GUIStyle hlStyle = new GUIStyle(GUI.skin.label);
        hlStyle.fontSize = 11;
        hlStyle.alignment = LanguageSelectionUI.IsArabic ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        hlStyle.wordWrap = true;
        hlStyle.normal.textColor = new Color(0.92f, 0.96f, 0.93f);

        if (highlights != null)
        {
            for (int i = 0; i < highlights.Length; i++)
            {
                Rect hlRect = new Rect(x + 18f, hlY, w - 36f, 23f);
                GUI.color = new Color(accent.r, accent.g, accent.b, 0.12f);
                GUI.DrawTexture(hlRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                EnchantedNatureGUI.DrawCrispBorder(hlRect, new Color(accent.r, accent.g, accent.b, 0.28f), 1f);

                GUI.Label(new Rect(x + 24f, hlY + 2f, w - 48f, 19f), highlights[i], hlStyle);
                hlY += 27f;
            }
        }

        // 8. Description paragraph (subtle parchment flavor)
        GUIStyle descStyle = new GUIStyle(GUI.skin.label);
        descStyle.fontSize = 10;
        descStyle.wordWrap = true;
        descStyle.alignment = TextAnchor.UpperCenter;
        descStyle.normal.textColor = new Color(0.68f, 0.80f, 0.72f);
        float descH = (y + liftY + h - 68f) - hlY;
        if (descH > 28f)
        {
            GUI.Label(new Rect(x + 16f, hlY + 4f, w - 32f, descH), desc, descStyle);
        }

        // 9. Bottom Action Button
        float btnW = w - 32f;
        float btnH = 46f;
        float btnX = x + 16f;
        float btnY = y + liftY + h - btnH - 16f;

        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(btnX, btnY, btnW, btnH), btnText, accent, 13, isMajor: (portalIndex == 1)))
        {
            onClick?.Invoke();
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