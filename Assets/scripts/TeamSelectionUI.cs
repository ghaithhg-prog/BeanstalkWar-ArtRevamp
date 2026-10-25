using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Sacred Allegiance (Team Selection) UI:
/// Inscribed on ancient undulating tree bark scrolls without harsh straight lines.
/// Features symmetrical curved leaf wings and detailed lore explaining why to choose
/// Tree Protectors vs Tree Destroyers.
/// </summary>
public class TeamSelectionUI : MonoBehaviour
{
    [Header("Legacy Panel (Auto-hidden)")]
    public GameObject panel;
    public Button protectorButton;
    public Button destroyerButton;

    [Header("Next Screen")]
    public CharacterSelectionUI characterSelectionUI;

    public static TeamSelectionUI Instance { get; private set; }
    public static bool IsTeamSelectionOpen => Instance != null && Instance.showTeamMenu;

    private bool characterScreenOpened = false;
    private bool showTeamMenu = false;

    public void Reopen()
    {
        characterScreenOpened = false;
        showTeamMenu = true;
    }

    void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (protectorButton != null)
            protectorButton.onClick.AddListener(() => SelectTeam(PlayerMovement.TeamType.Protector));

        if (destroyerButton != null)
            destroyerButton.onClick.AddListener(() => SelectTeam(PlayerMovement.TeamType.Destroyer));
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            showTeamMenu = false;
            return;
        }

        NetworkObject player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (player == null)
        {
            showTeamMenu = false;
            return;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            showTeamMenu = false;
            return;
        }

        if (MatchEndStatsUI.isAutoRestartingMatch)
        {
            showTeamMenu = false;
            characterScreenOpened = true;
            return;
        }

        // إذا كان اللاعب قد اختار شخصيته بالفعل (كما في إعادة اللعب التلقائي) أو بدأت المباراة، لا نفتح القائمة أبداً
        if (movement.hasSelectedCharacter.Value || movement.selectedCharacterID.Value >= 0 ||
            (GameStateManager.Instance != null && GameStateManager.Instance.currentState != GameStateManager.GameState.WaitingForPlayers))
        {
            characterScreenOpened = true;
            showTeamMenu = false;
            return;
        }

        // Show choice if team not selected yet
        if (!movement.hasSelectedTeam.Value)
        {
            showTeamMenu = true;
            if (panel != null) panel.SetActive(false); // Hide legacy ugly buttons
            return;
        }

        // Team selected
        showTeamMenu = false;

        // Open Character Selection once
        if (!characterScreenOpened)
        {
            characterScreenOpened = true;
            if (characterSelectionUI != null)
                characterSelectionUI.Show();
        }
    }

    void OnGUI()
    {
        if (!showTeamMenu) return;
        if (GameIntroBriefingUI.IsBriefingOpen) return;
        if (PlayerProgressionAndShopUI.IsShopOpen) return;
        if (CharacterSelectionUI.IsSelectionOpen) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) return;

        // Dark enchanted emerald haze
        GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(840f, Screen.width - 24f);
        float modalH = Mathf.Min(470f, Screen.height - 24f);
        float modalX = (Screen.width - modalW) / 2f;
        float modalY = (Screen.height - modalH) / 2f;

        // Main Ancient Bark Scroll
        EnchantedNatureGUI.DrawOrganicScroll(new Rect(modalX, modalY, modalW, modalH), Color.white);

        // Top-left Back button
        float backW = 150f;
        float backH = 30f;
        string backText = LanguageSelectionUI.Localize(
            "⬅️ BACK",
            ar: "⬅️ رجوع للخلف",
            fa: "⬅️ بازگشت",
            de: "⬅️ ZURÜCK",
            it: "⬅️ INDIETRO",
            tr: "⬅️ GERİ",
            ru: "⬅️ НАЗАД",
            fr: "⬅️ RETOUR",
            es: "⬅️ ATRÁS",
            hi: "⬅️ वापस",
            zh: "⬅️ 返回",
            ja: "⬅️ 戻る",
            pt: "⬅️ VOLTAR"
        );
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(modalX + 18f, modalY + 14f, backW, backH), backText, new Color(0.85f, 0.45f, 0.35f), 10))
        {
            showTeamMenu = false;
            characterScreenOpened = false;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            if (PlayModeUI.Instance != null)
            {
                PlayModeUI.Instance.ResetMenu();
            }
            return;
        }

        // Header Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.98f, 0.86f, 0.35f);

        GUI.Label(new Rect(modalX, modalY + 16f, modalW, 28f), LanguageSelectionUI.Localize(
            "❦ SACRED ALLEGIANCE: CHOOSE THY PATH ❦",
            ar: "❦ الولاء المقدس: اختر مسارك ❦",
            fa: "❦ بیعت مقدس: مسیر خود را برگزینید ❦",
            de: "❦ HEILIGE TREUE: WÄHLE DEINEN PFAD ❦",
            it: "❦ SACRA FEDELTÀ: SCEGLI IL TUO CAMMINO ❦",
            tr: "❦ KUTSAL BAĞLILIK: YOLUNU SEÇ ❦",
            ru: "❦ СВЯЩЕННАЯ ВЕРНОСТЬ: ВЫБЕРИ СВОЙ ПУТЬ ❦",
            es: "❦ LEALTAD SAGRADA: ELIGE TU CAMINO ❦",
            fr: "❦ ALLÉGEANCE SACRÉE : CHOISISSEZ VOTRE VOIE ❦",
            hi: "❦ पवित्र निष्ठा: अपना मार्ग चुनें ❦",
            zh: "❦ 神圣誓约：选择你的道路 ❦",
            ja: "❦ 神聖なる誓い：汝の道を選べ ❦",
            pt: "❦ LEALDADE SAGRADA: ESCOLHA SEU CAMINHO ❦"), titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label);
        subTitleStyle.fontSize = 11;
        subTitleStyle.fontStyle = FontStyle.Italic;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.normal.textColor = new Color(0.55f, 0.90f, 0.65f);

        GUI.Label(new Rect(modalX, modalY + 44f, modalW, 18f), LanguageSelectionUI.Localize(
            "~ Inscribed Two Thousand Years Ago in the Heart of the Sacred Oak ~",
            ar: "~ كُتبت قبل ألفي عام في قلب شجرة البلوط المقدسة ~",
            fa: "~ دو هزار سال پیش در قلب بلوط کهن نگاشته شد ~",
            de: "~ Vor zweitausend Jahren im Herzen der Heiligen Eiche eingemeißelt ~",
            it: "~ Inciso duemila anni fa nel cuore della Quercia Sacra ~",
            tr: "~ İki bin yıl önce Kutsal Meşe'nin kalbine kazındı ~",
            ru: "~ Начертано две тысячи лет назад в сердце Священного Дуба ~",
            es: "~ Inscrito hace dos mil años en el corazón del Roble Sagrado ~",
            fr: "~ Gravé il y a deux mille ans au cœur du Chêne Sacré ~",
            hi: "~ दो हजार वर्ष पूर्व पवित्र बलूत के हृदय में उत्कीर्ण ~",
            zh: "~ 两千年前铭刻于神圣巨橡之心 ~",
            ja: "~ 二千年前、聖なる樫の木の心臓に刻まれし記録 ~",
            pt: "~ Inscrito há dois mil anos no coração do Carvalho Sagrado ~"), subTitleStyle);

        // Symmetrical Wing Tablets (Left: Protectors, Right: Destroyers)
        float colW = (modalW - 55f) / 2f;
        float colH = modalH - 95f;
        float leftX = modalX + 22f;
        float rightX = modalX + 33f + colW;
        float colY = modalY + 72f;

        // 1. LEFT WING: TREE PROTECTORS
        DrawAllegianceWing(
            leftX, colY, colW, colH,
            new Color(0.20f, 0.78f, 1.0f),
            LanguageSelectionUI.Localize(
                "🛡️ TREE PROTECTORS",
                ar: "🛡️ حماة الشجرة",
                fa: "🛡️ محافظان درخت",
                de: "🛡️ BAUMBESCHÜTZER",
                it: "🛡️ PROTETTORI DELL'ALBERO",
                tr: "🛡️ AĞAÇ KORUYUCULARI",
                ru: "🛡️ ЗАЩИТНИКИ ДРЕВА",
                es: "🛡️ PROTECTORES DEL ÁRBOL",
                fr: "🛡️ PROTECTEURS DE L'ARBRE",
                hi: "🛡️ वृक्ष रक्षक",
                zh: "🛡️ 巨树守护者",
                ja: "🛡️ 巨樹の守護者",
                pt: "🛡️ PROTETORES DA ÁRVORE"
            ),
            LanguageSelectionUI.Localize(
                "🌿 GUARDIANS OF LIFE",
                ar: "🌿 حراس الحياة",
                fa: "🌿 نگهبانان حیات",
                de: "🌿 HÜTER DES LEBENS",
                it: "🌿 GUARDIANI DELLA VITA",
                tr: "🌿 YAŞAM MUHAFIZLARI",
                ru: "🌿 СТРАЖИ ЖИЗНИ",
                es: "🌿 GUARDIANES DE LA VIDA",
                fr: "🌿 GARDIENS DE LA VIE",
                hi: "🌿 जीवन के संरक्षक",
                zh: "🌿 生命守卫者",
                ja: "🌿 生命の番人",
                pt: "🌿 GUARDIÕES DA VIDA"
            ),
            LanguageSelectionUI.Localize(
                "Nurture and shield the sacred beanstalk from fell axes.\n\n" +
                "• Slay all 3 Guardian Bosses to awaken the Celestial Crystal.\n" +
                "• Carry the fallen crystal to the ancient altar.\n" +
                "• Hunt down enemy woodcutters and claim eternal victory!",
                ar: "احمِ شجرة الفاصولياء المقدسة من الفؤوس.\n\n" +
                    "• اهزم الزعماء الثلاثة لإيقاظ الكريستالة السماوية.\n" +
                    "• انقل الكريستالة الساقطة إلى المذبح القديم.\n" +
                    "• اطرد المدمرين وحقق النصر الأبدي!",
                fa: "از ساقه لوبیا سحرآمیز در برابر تبرهای متجاوز پاسداری کنید.\n\n" +
                    "• هر ۳ غول نگهبان را شکست دهید تا بلور آسمانی بیدار شود.\n" +
                    "• بلور مقدس را به محراب کهن حمل کنید.\n" +
                    "• چوب‌بُران دشمن را تارومار کنید و پیروز جاودان شوید!",
                de: "Schütze die heilige Ranke vor den Holzfälleräxten.\n\n" +
                    "• Besiege alle 3 Wächterbosse, um den Himmelskristall zu wecken.\n" +
                    "• Trage den gefallenen Kristall zum uralten Altar.\n" +
                    "• Schlage die Zerstörer zurück und erringe den ewigen Sieg!",
                it: "Proteggi il sacro fagiolo dalle asce nemiche.\n\n" +
                    "• Sconfiggi i 3 Boss Guardiani per risvegliare il Cristallo.\n" +
                    "• Trasporta il cristallo caduto all'antico altare.\n" +
                    "• Caccia i distruttori e ottieni la vittoria eterna!",
                tr: "Kutsal fasulye sırığını baltalardan koruyun.\n\n" +
                    "• Göksel Kristali uyandırmak için 3 Muhafız Patronu yenin.\n" +
                    "• Düşen kristali antik sunağa taşıyın.\n" +
                    "• Ağaç kesen düşmanları püskürtüp ebedi zaferi kazanın!",
                ru: "Защитите священный стебель от топоров.\n\n" +
                    "• Победите 3 Боссов, чтобы пробудить Кристалл.\n" +
                    "• Доставьте кристалл к древнему алтарю.\n" +
                    "• Отразите атаки разрушителей!",
                es: "Protege el tallo sagrado de las hachas.\n\n" +
                    "• Derrota a los 3 Jefes para despertar el Cristal.\n" +
                    "• Lleva el cristal caído al altar ancestral.\n" +
                    "• ¡Vence a los destructores y triunfa!",
                fr: "Protégez le haricot magique sacré des haches.\n\n" +
                    "• Battez les 3 Boss pour éveiller le Cristal Céleste.\n" +
                    "• Transportez le cristal à l'autel antique.\n" +
                    "• Repoussez les destructeurs et triomphez !",
                hi: "पवित्र बीनस्टॉक को कुल्हाड़ियों से बचाएं।\n\n" +
                    "• दिव्य क्रिस्टल जगाने के लिए तीनों संरक्षक बॉसों को हराएं।\n" +
                    "• गिरे हुए क्रिस्टल को प्राचीन वेदी तक ले जाएं।\n" +
                    "• विनाशकों को खदेड़ें और शाश्वت विजय पाएं!",
                zh: "守护神圣的豆茎，抵御伐木巨斧。\n\n" +
                    "• 击败全部3位守护首领以唤醒苍穹水晶。\n" +
                    "• 将掉落的水晶运送至古老圣坛。\n" +
                    "• 驱逐敌人，夺取永恒的胜利！",
                ja: "聖なる巨木を伐採斧の脅威から守り抜け。\n\n" +
                    "• 3体の守護ボスを討伐し、天上の水晶を目覚めさせよ。\n" +
                    "• 落ちた水晶を拾い、古代の祭壇へと運び込め。\n" +
                    "• 破壊者の侵略を阻止し、永遠の勝利を掴め！",
                pt: "Proteja o feijoeiro sagrado contra os machados cruéis.\n\n" +
                    "• Derrote os 3 Chefes Guardiões para despertar o Cristal Celestial.\n" +
                    "• Carregue o cristal caído até o antigo altar.\n" +
                    "• Cace os lenhadores inimigos e alcance a vitória eterna!"
            ),
            LanguageSelectionUI.Localize(
                "🌿 BECOME A PROTECTOR",
                ar: "🌿 انضم إلى الحماة",
                fa: "🌿 پیوستن به محافظان",
                de: "🌿 ALS BESCHÜTZER BEITRETEN",
                it: "🌿 UNISCITI AI PROTETTORI",
                tr: "🌿 KORUYUCULARA KATIL",
                ru: "🌿 СТАТЬ ЗАЩИТНИКОМ",
                es: "🌿 SÉ UN PROTECTOR",
                fr: "🌿 DEVENIR UN PROTECTEUR",
                hi: "🌿 रक्षक बनें",
                zh: "🌿 加入守护者",
                ja: "🌿 守護者として参戦",
                pt: "🌿 TORNAR-SE PROTETOR"
            ),
            () => SelectTeam(PlayerMovement.TeamType.Protector)
        );

        // 2. RIGHT WING: TREE DESTROYERS
        DrawAllegianceWing(
            rightX, colY, colW, colH,
            new Color(1f, 0.32f, 0.18f),
            LanguageSelectionUI.Localize(
                "⚔️ TREE DESTROYERS",
                ar: "⚔️ مدمر الشجرة",
                fa: "⚔️ نابودکنندگان درخت",
                de: "⚔️ BAUMZERSTÖRER",
                it: "⚔️ DISTRUTTORI DELL'ALBERO",
                tr: "⚔️ AĞAÇ YOK EDİCİLERİ",
                ru: "⚔️ РАЗРУШИТЕЛИ ДРЕВА",
                es: "⚔️ DESTRUCTORES DEL ÁRBOL",
                fr: "⚔️ DESTRUCTEURS DE L'ARBRE",
                hi: "⚔️ वृक्ष विनाशक",
                zh: "⚔️ 巨树破坏者",
                ja: "⚔️ 巨樹の破壊者",
                pt: "⚔️ DESTRUIDORES DA ÁRVORE"
            ),
            LanguageSelectionUI.Localize(
                "🔥 AXEMEN OF CHAOS",
                ar: "🔥 حطابو الفوضى",
                fa: "🔥 تبرداران آشوب",
                de: "🔥 HOLZFÄLLER DES CHAOS",
                it: "🔥 TAGLIALEGNA DEL CAOS",
                tr: "🔥 KAOS ODUNCULARI",
                ru: "🔥 СЕКИРЫ ХАОСА",
                es: "🔥 LEÑADORES DEL CAOS",
                fr: "🔥 BÛCHERONS DU CHAOS",
                hi: "🔥 अराजकता के कुल्हाड़ीधारी",
                zh: "🔥 混沌伐木狂人",
                ja: "🔥 混沌の木こり軍団",
                pt: "🔥 LENHADORES DO CAOS"
            ),
            LanguageSelectionUI.Localize(
                "Fell the towering colossus and claim its ancient power.\n\n" +
                "• Slay all 3 Guardian Bosses to break the Tree Shield.\n" +
                "• Strike down the giant trunk with relentless woodcutter fury.\n" +
                "• Stop protectors from delivering crystals and conquer the realm!",
                ar: "أسقط الشجرة العملاقة واستولِ على قوتها القديمة.\n\n" +
                    "• اهزم زعماء الحراسة الثلاثة لكسر درع الشجرة.\n" +
                    "• اضرب الجذع العملاق بقوة فأسك التي لا ترحم.\n" +
                    "• امنع الحماة من إيصال الكريستالة وأسقط الشجرة!",
                fa: "درخت عظیم‌الجثه را سرنگون کنید و نیروی باستانی آن را به چنگ آورید.\n\n" +
                    "• هر ۳ غول محافظ را هلاک کنید تا سپر درخت شکسته شود.\n" +
                    "• با تبرهای مرگبار تنه عظیم درخت را خرد کنید.\n" +
                    "• مانع انتقال بلور توسط حامیان شوید و قلمرو را فتح کنید!",
                de: "Fälle den aufragenden Giganten und beanspruche seine uralte Macht.\n\n" +
                    "• Töte alle 3 Wächterbosse, um den Schild des Baumes zu brechen.\n" +
                    "• Schlage mit unerbittlicher Axtwut auf den Riesenstamm ein.\n" +
                    "• Halte die Beschützer vom Altar fern und erobere das Reich!",
                it: "Abbatti il colosso torreggiante e rivendica il suo antico potere.\n\n" +
                    "• Uccidi tutti e 3 i Boss Guardiani per spezzare lo Scudo dell'Albero.\n" +
                    "• Colpisci il tronco gigante con implacabile furia da boscaiolo.\n" +
                    "• Impedisci ai protettori di consegnare i cristalli e conquista il regno!",
                tr: "Görkemli devi devirin ve onun antik gücünü ele geçirin.\n\n" +
                    "• Ağaç Kalkanını kırmak için 3 Muhafız Patronu katledin.\n" +
                    "• Dev gövdeyi acımasız oduncu öfkenizle kesin.\n" +
                    "• Koruyucuların kristali teslim etmesini engelleyin ve dünyayı fethedin!",
                ru: "Срубите великое древо и заберите силу.\n\n" +
                    "• Убейте 3 Боссов-хранителей, чтобы снять щит древа.\n" +
                    "• Рубите ствол без пощады.\n" +
                    "• Не дайте защитникам донести кристалл!",
                es: "Derriba el coloso y reclama su poder ancestral.\n\n" +
                    "• Elimina a los 3 Jefes para romper el escudo del Árbol.\n" +
                    "• Tala el tronco gigante con furia implacable.\n" +
                    "• ¡Evita que lleven los cristales y gana!",
                fr: "Abattez le colosse et emparez-vous de son pouvoir.\n\n" +
                    "• Éliminez les 3 Boss pour briser le bouclier de l'arbre.\n" +
                    "• Frappez le tronc avec une fureur implacable.\n" +
                    "• Empêchez la livraison des cristaux et gagnez !",
                hi: "विशालकाय वृक्ष को गिराएं और उसकी प्राचीन शक्ति पर कब्जा करें।\n\n" +
                    "• वृक्ष की ढाल तोड़ने के लिए तीनों संरक्षकों का वध करें।\n" +
                    "• विशाल तने पर बेपरवाह कुल्हाड़ियों से वार करें।\n" +
                    "• संरक्षकों को क्रिस्टल पहुंचाने से रोकें और राज करें!",
                zh: "砍倒参天巨树，攫取其古老神力。\n\n" +
                    "• 击杀全部3名守护首领以粉碎神树护盾。\n" +
                    "• 以无情狂怒猛砍神树巨干。\n" +
                    "• 阻止守护者运送水晶，征服全境！",
                ja: "そびえ立つ巨木を伐採し、その古代の魔力を奪い去れ。\n\n" +
                    "• 3体の守護ボスを撃破し、巨樹の神聖シールドを破壊せよ。\n" +
                    "• 容赦なき怒りの斧で大樹の巨幹を叩き折れ。\n" +
                    "• 守護者の水晶運搬を阻止し、戦場を完全制覇せよ！",
                pt: "Derrube o colosso gigante e reivindique seu poder ancestral.\n\n" +
                    "• Elimine os 3 Chefes Guardiões para quebrar o Escudo da Árvore.\n" +
                    "• Golpeie o tronco gigante com a fúria implacável dos lenhadores.\n" +
                    "• Impeça os protetores de entregar os cristais e conquiste o reino!"
            ),
            LanguageSelectionUI.Localize(
                "🔥 BECOME A DESTROYER",
                ar: "🔥 انضم إلى المدمرين",
                fa: "🔥 پیوستن به نابودکنندگان",
                de: "🔥 ALS ZERSTÖRER BEITRETEN",
                it: "🔥 UNISCITI AI DISTRUTTORI",
                tr: "🔥 YOK EDİCİLERE KATIL",
                ru: "🔥 СТАТЬ РАЗРУШИТЕЛЕМ",
                es: "🔥 SÉ UN DESTRUCTOR",
                fr: "🔥 DEVENIR UN DESTRUCTEUR",
                hi: "🔥 विनाशक बनें",
                zh: "🔥 加入破坏者",
                ja: "🔥 破壊者として参戦",
                pt: "🔥 TORNAR-SE DESTRUIDOR"
            ),
            () => SelectTeam(PlayerMovement.TeamType.Destroyer)
        );
    }

    void DrawAllegianceWing(float x, float y, float w, float h, Color accent, string title, string badge, string lore, string btnText, System.Action onChoose)
    {
        // Curved Organic Leaf Scroll for each wing
        EnchantedNatureGUI.DrawOrganicScroll(new Rect(x, y, w, h), new Color(accent.r * 0.2f + 0.8f, accent.g * 0.2f + 0.8f, accent.b * 0.2f + 0.8f, 0.96f));

        // Badge
        GUIStyle badgeStyle = new GUIStyle(GUI.skin.label);
        badgeStyle.fontSize = 10;
        badgeStyle.fontStyle = FontStyle.Bold;
        badgeStyle.alignment = TextAnchor.MiddleCenter;
        badgeStyle.normal.textColor = accent;

        float badgeW = 160f;
        GUI.color = new Color(accent.r * 0.15f, accent.g * 0.15f, accent.b * 0.15f, 0.85f);
        GUI.DrawTexture(new Rect(x + (w - badgeW) / 2f, y + 14f, badgeW, 22f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + (w - badgeW) / 2f, y + 14f, badgeW, 22f), badge, badgeStyle);

        // Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x + 10f, y + 42f, w - 20f, 24f), title, titleStyle);

        // Lore & Explanation
        GUIStyle loreStyle = new GUIStyle(GUI.skin.label);
        loreStyle.fontSize = 11;
        loreStyle.wordWrap = true;
        loreStyle.normal.textColor = new Color(0.85f, 0.90f, 0.86f);

        GUI.Label(new Rect(x + 20f, y + 74f, w - 40f, h - 145f), lore, loreStyle);

        // Action Leaf Button
        float btnW = w - 40f;
        float btnH = 46f;
        float btnX = x + 20f;
        float btnY = y + h - btnH - 18f;

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(btnX, btnY, btnW, btnH), btnText, accent, 13))
        {
            onChoose?.Invoke();
        }
    }

    void SelectTeam(PlayerMovement.TeamType selectedTeam)
    {
        if (NetworkManager.Singleton == null) return;
        NetworkObject player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (player == null) return;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null) return;

        movement.RequestTeam(selectedTeam);
        movement.team = selectedTeam;
        movement.ApplyTeamVisualAura();
        showTeamMenu = false;
    }
}