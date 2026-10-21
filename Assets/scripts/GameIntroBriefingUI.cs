using UnityEngine;
using Unity.Netcode;

/// <summary>
/// GameIntroBriefingUI:
/// 1. Displays an interactive, animated visual briefing & illustrated tutorial guide at the start of the game
///    explaining the 3 Boss Seals, Protector vs Destroyer missions, Crystal delivery, Tree shield, and controls.
/// 2. Displays a live 3-Boss Seal progress HUD during battle.
/// 3. Displays huge, dramatic center-screen announcements in English when each Boss is slain, and a
///    team-specific ultimate banner when all 3 Bosses are defeated!
/// </summary>
public class GameIntroBriefingUI : MonoBehaviour
{
    public static GameIntroBriefingUI Instance { get; private set; }
    public static bool IsBriefingOpen => Instance != null && Instance.showBriefingModal;

    private bool showBriefingModal = false;
    private bool autoPlayPresentation = true;
    private float slideTimer = 0f;
    private const float SlideDuration = 7.5f;
    private int currentSlide = 0;
    private const int TotalSlides = 4;

    // Scroll state for briefing slides
    private Vector2 briefingScrollPos = Vector2.zero;

    // Explanatory Video Walkthrough Mode
    private bool isVideoGuideOpen = false;
    private int videoTeamSelection = 0; // 0 = Protectors, 1 = Destroyers
    private bool isVideoPlaying = true;
    private float videoCurrentTime = 0f;
    private const float VideoPhaseDuration = 7.0f;
    private const int TotalVideoPhases = 4;
    private const float TotalVideoDuration = VideoPhaseDuration * TotalVideoPhases; // 28.0s

    // Center-screen announcement state
    private float bigBannerTimer = 0f;
    private float bigBannerDuration = 6f;
    private bool isUltimateThreeBossBanner = false;
    private string bannerHeader = "";
    private string bannerMainText = "";
    private string bannerSubText = "";
    private Color bannerPrimaryColor = Color.yellow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("GameIntroBriefingUI");
            go.AddComponent<GameIntroBriefingUI>();
            DontDestroyOnLoad(go);
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
            return;
        }
    }

    void OnEnable()
    {
        LevelBossManager.OnSingleBossDefeated += HandleSingleBossDefeated;
        LevelBossManager.OnAllThreeBossesDefeated += HandleAllThreeBossesDefeated;
    }

    void OnDisable()
    {
        LevelBossManager.OnSingleBossDefeated -= HandleSingleBossDefeated;
        LevelBossManager.OnAllThreeBossesDefeated -= HandleAllThreeBossesDefeated;
    }

    private void HandleSingleBossDefeated(string bossTitle, string killerName, PlayerMovement.TeamType killerTeam, int defeatedCount)
    {
        if (defeatedCount >= LevelBossManager.TotalBossCount)
        {
            // The ultimate 3-boss banner will immediately take over with role-specific instructions
            return;
        }

        isUltimateThreeBossBanner = false;
        bigBannerDuration = 4.8f;
        bigBannerTimer = bigBannerDuration;

        bannerHeader = LanguageSelectionUI.Localize(
            $"👑 BOSS DEFEATED ({defeatedCount} / {LevelBossManager.TotalBossCount} SEALS BROKEN) 👑",
            ar: $"👑 تم القضاء على الزعيم ({defeatedCount} / {LevelBossManager.TotalBossCount} أختام كُسرت) 👑",
            ru: $"👑 БОСС ПОВЕРЖЕН ({defeatedCount} / {LevelBossManager.TotalBossCount} ПЕЧАТЕЙ СНЯТО) 👑",
            es: $"👑 ¡JEFE DERROTADO ({defeatedCount} / {LevelBossManager.TotalBossCount} SELLOS ROTOS)! 👑",
            fr: $"👑 BOSS VAINCU ({defeatedCount} / {LevelBossManager.TotalBossCount} SCEAUX BRISÉS) 👑"
        );

        bannerMainText = LanguageSelectionUI.Localize(
            $"{bossTitle} WAS SLAIN BY {killerName.ToUpper()}!",
            ar: $"تمت هزيمة {bossTitle} بواسطة {killerName.ToUpper()}!",
            ru: $"{bossTitle} ПОВЕРЖЕН ИГРОКОМ {killerName.ToUpper()}!",
            es: $"¡{bossTitle} DERROTADO POR {killerName.ToUpper()}!",
            fr: $"{bossTitle} A ÉTÉ VAINCU PAR {killerName.ToUpper()} !"
        );

        int remaining = LevelBossManager.TotalBossCount - defeatedCount;
        bannerSubText = killerTeam == PlayerMovement.TeamType.Protector
            ? LanguageSelectionUI.Localize(
                $"PROTECTORS BLESSING: Sacred Tree Healed +350 HP! ({remaining} Boss{(remaining > 1 ? "es" : "")} Remaining)",
                ar: $"بركة الحماة: تم شفاء الشجرة +350 نقطة! (متبقي {remaining} زعيم)",
                ru: $"БЛАГОСЛОВЕНИЕ: Дерево исцелено на +350 HP! (Осталось {remaining} босс)",
                es: $"BENDICIÓN PROTECTORA: ¡Árbol curado +350 HP! (Quedan {remaining} jefes)",
                fr: $"BÉNÉDICTION : L'Arbre Sacré a récupéré +350 PV ! ({remaining} restants)"
            )
            : LanguageSelectionUI.Localize(
                $"DESTROYERS SHOCKWAVE: Sacred Tree Damaged -260 HP! ({remaining} Boss{(remaining > 1 ? "es" : "")} Remaining)",
                ar: $"ضربة المدمرين: تضررت الشجرة -260 نقطة! (متبقي {remaining} زعيم)",
                ru: $"УДАРНАЯ ВОЛНА: Дерево получило -260 урона! (Осталось {remaining} босс)",
                es: $"ONDA DESTRUCTIVA: ¡Árbol dañado -260 HP! (Quedan {remaining} jefes)",
                fr: $"ONDE DE CHOC : L'Arbre Sacré a subi -260 dégâts ! ({remaining} restants)"
            );

        bannerPrimaryColor = killerTeam == PlayerMovement.TeamType.Destroyer
            ? new Color(1f, 0.35f, 0.2f)
            : new Color(0.25f, 0.85f, 1f);
    }

    private void HandleAllThreeBossesDefeated()
    {
        isUltimateThreeBossBanner = true;
        bigBannerDuration = 8.5f;
        bigBannerTimer = bigBannerDuration;
    }

    private PlayerMovement.TeamType GetLocalPlayerTeam()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localObj != null)
            {
                PlayerMovement pm = localObj.GetComponent<PlayerMovement>();
                if (pm != null) return pm.team;
            }
        }

        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement pm in allPlayers)
        {
            if (pm != null && pm.IsOwner)
                return pm.team;
        }

        return PlayerMovement.TeamType.Protector;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.F1))
        {
            ToggleBriefing();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            OpenVideoGuide();
        }

        if (showBriefingModal && !isVideoGuideOpen && autoPlayPresentation)
        {
            slideTimer += Time.unscaledDeltaTime;
            if (slideTimer >= SlideDuration)
            {
                slideTimer = 0f;
                currentSlide = (currentSlide + 1) % TotalSlides;
                briefingScrollPos = Vector2.zero;
            }
        }

        if (showBriefingModal && isVideoGuideOpen && isVideoPlaying)
        {
            videoCurrentTime += Time.unscaledDeltaTime;
            if (videoCurrentTime >= TotalVideoDuration + 1.5f)
            {
                videoCurrentTime = 0f;
            }
        }

        if (bigBannerTimer > 0f)
        {
            bigBannerTimer -= Time.deltaTime;
        }
    }

    public void CloseBriefing()
    {
        showBriefingModal = false;
        isVideoGuideOpen = false;
        isVideoPlaying = false;
        autoPlayPresentation = false;
    }

    public void OpenBriefing()
    {
        showBriefingModal = true;
        isVideoGuideOpen = false;
        autoPlayPresentation = false;
    }

    public void ToggleBriefing()
    {
        showBriefingModal = !showBriefingModal;
        autoPlayPresentation = false;
    }

    public void OpenVideoGuide()
    {
        showBriefingModal = true;
        isVideoGuideOpen = true;
        videoCurrentTime = 0f;
        isVideoPlaying = true;
    }

    void OnGUI()
    {
        if (LanguageSelectionUI.IsLanguageModalOpen) return;

        // Ensure Guide button and modals are ALWAYS in front of any screen overlay or background haze!
        GUI.depth = showBriefingModal ? -400 : -100;

        // If the Briefing Modal is open, render ONLY the Briefing Modal / Video Guide cleanly on its own!
        if (showBriefingModal)
        {
            if (isVideoGuideOpen)
            {
                DrawInteractiveVideoGuide();
            }
            else
            {
                DrawInteractiveBriefingModal();
            }
            return;
        }

        // Hide the corner button while in Character Selection, Shop, or Language Selection so each screen stays 100% clean
        if (!CharacterSelectionUI.IsSelectionOpen && !PlayerProgressionAndShopUI.IsShopOpen && !LanguageSelectionUI.IsLanguageModalOpen)
        {
            DrawHowToPlayToggleButton();
        }

        bool isPlaying = GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying();

        // During gameplay, Boss Seal & Shield status is rendered clearly in TreeGrowth directly under health bar.
        // Duplicate black box (DrawBossSealsStatusHUD) is removed as requested by user to keep the view clean.

        // Draw Giant Center-Screen Boss / Ultimate Objective Announcement Banner
        if (bigBannerTimer > 0f)
        {
            DrawGiantCenterAnnouncement();
        }
    }

    private void DrawHowToPlayToggleButton()
    {
        GUI.depth = -100;
        float btnW = 135f;
        float btnH = 38f;
        // Sits in top-right bar to the left of the Language Flag and Audio buttons:
        // [Guide (135px)] [Flag (64px)] [Audio (78px)]
        float x = Screen.width - 78f - 12f - 64f - 8f - btnW - 8f;
        float y = 8f;

        string btnLabel = showBriefingModal
            ? "❌ " + LanguageSelectionUI.Localize("CLOSE", ar: "إغلاق", ru: "ЗАКРЫТЬ", es: "CERRAR", fr: "FERMER")
            : LanguageSelectionUI.Localize("📖 GUIDE", ar: "📖 التوجيهات", ru: "📖 ИНФО", es: "📖 GUÍA", fr: "📖 GUIDE");

        Color accent = showBriefingModal ? new Color(1f, 0.78f, 0.22f) : EnchantedNatureGUI.AntiqueGoldBright;
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x, y, btnW, btnH), btnLabel, accent, 12, true))
        {
            showBriefingModal = !showBriefingModal;
            autoPlayPresentation = false;
        }
    }

    private void DrawBossSealsStatusHUD()
    {
        int defeated = LevelBossManager.Instance != null ? LevelBossManager.Instance.DefeatedBossCount : 0;
        bool allDead = LevelBossManager.Instance != null && LevelBossManager.Instance.AreAllBossesDefeated;

        float w = 500f;
        float h = 32f;
        float x = (Screen.width - w) / 2f;
        float y = 72f;

        Color bg = new Color(0.08f, 0.11f, 0.09f, 0.88f);
        Color border = allDead ? new Color(1f, 0.78f, 0.2f) : new Color(0.3f, 0.75f, 0.95f);
        EnchantedNatureGUI.DrawLeafPlaque(new Rect(x, y, w, h), bg, border);

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        string enSeals = "";
        string arSeals = "";
        string ruSeals = "";
        string esSeals = "";
        string frSeals = "";

        for (int i = 1; i <= 3; i++)
        {
            if (i <= defeated)
            {
                enSeals += " [💀 SLAIN] ";
                arSeals += " [💀 مهزوم] ";
                ruSeals += " [💀 ПОВЕРЖЕН] ";
                esSeals += " [💀 DERROTADO] ";
                frSeals += " [💀 VAINCU] ";
            }
            else
            {
                enSeals += $" [👑 BOSS {i}] ";
                arSeals += $" [👑 زعيم {i}] ";
                ruSeals += $" [👑 БОСС {i}] ";
                esSeals += $" [👑 JEFE {i}] ";
                frSeals += $" [👑 BOSS {i}] ";
            }
        }

        if (!allDead)
        {
            style.normal.textColor = new Color(0.92f, 0.96f, 1f);
            string activeShieldLabel = LanguageSelectionUI.Localize(
                $"🛡️ TREE SHIELD ACTIVE | SEALS:{enSeals}({defeated}/3)",
                ar: $"🛡️ درع الشجرة نشط | الأختام:{arSeals}({defeated}/3)",
                ru: $"🛡️ ЩИТ ДЕРЕВА АКТИВЕН | ПЕЧАТИ:{ruSeals}({defeated}/3)",
                es: $"🛡️ ESCUDO ACTIVO | SELLOS:{esSeals}({defeated}/3)",
                fr: $"🛡️ BOUCLIER ACTIF | SCEAUX:{frSeals}({defeated}/3)"
            );
            GUI.Label(new Rect(x, y, w, h), activeShieldLabel, style);
        }
        else
        {
            PlayerMovement.TeamType myTeam = GetLocalPlayerTeam();
            if (myTeam == PlayerMovement.TeamType.Destroyer)
            {
                style.normal.textColor = new Color(1f, 0.45f, 0.25f);
                string text = LanguageSelectionUI.Localize(
                    "🔥 ALL 3 BOSSES SLAIN! TREE SHIELD BROKEN — DESTROY THE TREE! 🔥",
                    ar: "🔥 تم القضاء على الزعماء الثلاثة! سقط الدرع — دمروا الشجرة الآن! 🔥",
                    ru: "🔥 ВСЕ 3 БОССА ПОВЕРЖЕНЫ! ЩИТ СНЯТ — СРУБИТЕ ДЕРЕВО! 🔥",
                    es: "🔥 ¡LOS 3 JEFES HAN CAÍDO! ESCUDO ROTO — ¡DESTRUYE EL ÁRBOL! 🔥",
                    fr: "🔥 LES 3 BOSS SONT VAINCUS ! BOUCLIER BRISÉ — DÉTRUISEZ L'ARBRE ! 🔥"
                );
                GUI.Label(new Rect(x, y, w, h), text, style);
            }
            else
            {
                style.normal.textColor = new Color(0.35f, 0.95f, 1f);
                string text = LanguageSelectionUI.Localize(
                    "💎 ALL 3 BOSSES SLAIN! CRYSTAL IS READY — DELIVER IT TO THE ALTAR! 💎",
                    ar: "💎 تم القضاء على الزعماء الثلاثة! الكريستالة جاهزة — أوصلوها إلى المذبح! 💎",
                    ru: "💎 ВСЕ 3 БОССА ПОВЕРЖЕНЫ! КРИСТАЛЛ ГОТОВ — ДОСТАВЬТЕ ЕГО К АЛТАРЮ! 💎",
                    es: "💎 ¡LOS 3 JEFES HAN CAÍDO! CRISTAL DISPONIBLE — ¡LLÉVALO AL ALTAR! 💎",
                    fr: "💎 LES 3 BOSS SONT VAINCUS ! LE CRISTAL EST PRÊT — LIVREZ-LE À L'AUTEL ! 💎"
                );
                GUI.Label(new Rect(x, y, w, h), text, style);
            }
        }
    }

    private void DrawGiantCenterAnnouncement()
    {
        float alpha = Mathf.Clamp01(bigBannerTimer / 0.6f);
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.03f;

        float w = Mathf.Min(840f, Screen.width * 0.88f) * pulse;
        float h = isUltimateThreeBossBanner ? 165f : 125f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.24f;

        PlayerMovement.TeamType localTeam = GetLocalPlayerTeam();

        string head = bannerHeader;
        string main = bannerMainText;
        string sub = bannerSubText;
        Color frameColor = bannerPrimaryColor;

        if (isUltimateThreeBossBanner)
        {
            if (localTeam == PlayerMovement.TeamType.Destroyer)
            {
                frameColor = new Color(1f, 0.28f, 0.15f, alpha);
                head = LanguageSelectionUI.Localize("🔥 ALL 3 GUARDIAN BOSSES HAVE BEEN DEFEATED! 🔥", ar: "🔥 تم القضاء على الزعماء الحراس الثلاثة بالكامل! 🔥", ru: "🔥 ВСЕ 3 БОССА ПОВЕРЖЕНЫ! 🔥", es: "🔥 ¡LOS 3 JEFES GUARDIANES HAN CAÍDO! 🔥", fr: "🔥 LES 3 BOSS GARDIENS ONT ÉTÉ VAINCUS ! 🔥");
                main = LanguageSelectionUI.Localize("YOU CAN NOW DESTROY THE SACRED TREE!", ar: "يمكنك الآن تدمير الشجرة المقدسة!", ru: "ТЕПЕРЬ ВЫ МОЖЕТЕ СРУБИТЬ ДЕРЕВО!", es: "¡AHORA PUEDES DESTRUIR EL ÁRBOL SAGRADO!", fr: "VOUS POUVEZ MAINTENANT DÉTRUIRE L'ARBRE SACRÉ !");
                sub = LanguageSelectionUI.Localize("10-Second Divine Shield Countdown active! Position yourselves — Tree becomes vulnerable in 10s!", ar: "العد التنازلي لدرع الصمود المقدس بدأ (10 ثوانٍ)! خذوا مواقعكم — الشجرة ستصبح مكشوفة بالكامل بعد 10 ثوانٍ!", ru: "ОТСЧЕТ 10 СЕКУНД ДО СНЯТИЯ ЩИТА! Займите позиции — Дерево станет уязвимым через 10 сек!", es: "¡CUENTA ATRÁS DE 10S DEL ESCUDO DIVINO! ¡El Árbol será vulnerable en 10s!", fr: "COMPTE À REBOURS DE 10S DU BOUCLIER DIVIN ! L'Arbre sera vulnérable dans 10s !");
            }
            else
            {
                frameColor = new Color(0.18f, 0.88f, 1f, alpha);
                head = LanguageSelectionUI.Localize("💎 ALL 3 GUARDIAN BOSSES HAVE BEEN DEFEATED! 💎", ar: "💎 تم القضاء على الزعماء الحراس الثلاثة بالكامل! 💎", ru: "💎 ВСЕ 3 БОССА ПОВЕРЖЕНЫ! 💎", es: "💎 ¡LOS 3 JEFES GUARDIANES HAN CAÍDO! 💎", fr: "💎 LES 3 BOSS GARDIENS ONT ÉTÉ VAINCUS ! 💎");
                main = LanguageSelectionUI.Localize("THE CRYSTAL IS READY — YOU CAN TAKE IT NOW!", ar: "الكريستالة جاهزة — يمكنك أخذها الآن!", ru: "КРИСТАЛЛ ГОТОВ — ЗАБЕРИТЕ ЕГО!", es: "¡EL CRISTAL ESTÁ LISTO — PUEDES TOMARLO AHORA!", fr: "LE CRISTAL EST PRÊT — PRENEZ-LE MAINTENANT !");
                sub = LanguageSelectionUI.Localize("The Sacred Crystal has appeared! Grab the Crystal and deliver it to the Sacred Altar to WIN!", ar: "ظهرت الكريستالة المقدسة! احمل الكريستالة وأوصلها إلى المذبح المقدس للفوز بالمعركة!", ru: "Священный кристалл появился! Отнесите его к алтарю для победы!", es: "¡El Cristal Sagrado ha aparecido! ¡Llévalo al Altar para ganar!", fr: "Le Cristal Sacré est apparu ! Portez-le à l'Autel pour gagner !");
            }
        }

        // Deep dramatic backdrop
        GUI.color = new Color(0.04f, 0.06f, 0.08f, 0.94f * alpha);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // Glowing top & bottom borders
        GUI.color = new Color(frameColor.r, frameColor.g, frameColor.b, alpha);
        GUI.DrawTexture(new Rect(x, y, w, 5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - 5f, w, 5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 5f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - 5f, y, 5f, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle headStyle = new GUIStyle(GUI.skin.label);
        headStyle.fontSize = isUltimateThreeBossBanner ? 18 : 15;
        headStyle.fontStyle = FontStyle.Bold;
        headStyle.alignment = TextAnchor.MiddleCenter;
        headStyle.normal.textColor = new Color(1f, 0.88f, 0.28f, alpha);

        GUIStyle mainStyle = new GUIStyle(GUI.skin.label);
        mainStyle.fontSize = isUltimateThreeBossBanner ? 26 : 21;
        mainStyle.fontStyle = FontStyle.Bold;
        mainStyle.alignment = TextAnchor.MiddleCenter;
        mainStyle.normal.textColor = new Color(frameColor.r, frameColor.g, frameColor.b, alpha);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 14;
        subStyle.fontStyle = FontStyle.Bold;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.wordWrap = true;
        subStyle.normal.textColor = new Color(0.95f, 0.97f, 1f, alpha);

        GUI.Label(new Rect(x + 16f, y + 12f, w - 32f, 28f), head, headStyle);
        GUI.Label(new Rect(x + 16f, y + 44f, w - 32f, 44f), main, mainStyle);
        GUI.Label(new Rect(x + 24f, y + 92f, w - 48f, 52f), sub, subStyle);
    }

    private void DrawInteractiveBriefingModal()
    {
        // Full-screen dim overlay
        GUI.color = new Color(0.02f, 0.04f, 0.03f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(920f, Screen.width - 32f);
        float modalH = Mathf.Min(600f, Screen.height - 32f);
        float mx = (Screen.width - modalW) / 2f;
        float my = (Screen.height - modalH) / 2f;

        EnchantedNatureGUI.DrawLeafPlaque(
            new Rect(mx, my, modalW, modalH),
            new Color(0.07f, 0.11f, 0.09f, 0.98f),
            new Color(0.32f, 0.88f, 0.48f, 1f)
        );

        // Top Title Header
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 21;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(1f, 0.88f, 0.32f);
        string briefingTitle = "🌿 " + LanguageSelectionUI.Localize(
            "BEANSTALK WAR — OFFICIAL TACTICAL BRIEFING",
            ar: "حرب نبتة الفاصولياء — الدليل التكتيكي الرسمي",
            ru: "ВОЙНА БОБОВОГО СТЕБЛЯ — ТАКТИЧЕСКИЙ ИНСТРУКТАЖ",
            es: "GUERRA DEL TALLO — GUÍA TÁCTICA OFICIAL",
            fr: "GUERRE DU HARICOT — GUIDE TACTIQUE OFFICIEL"
        ) + " 🌿";
        GUI.Label(new Rect(mx, my + 10f, modalW, 30f), briefingTitle, titleStyle);

        // Slide Progress Tabs + Video Guide Tab (5 Tabs Total)
        string[] tabNames = new string[]
        {
            LanguageSelectionUI.Localize("1. THE SACRED WAR", "1. الحرب المقدسة", "1. СВЯЩЕННАЯ ВОЙНА", "1. LA GUERRA SAGRADA", "1. LA GUERRE SACRÉE"),
            LanguageSelectionUI.Localize("2. THE 3 BOSS SEALS", "2. أختام الزعماء الثلاثة", "2. 3 ПЕЧАТИ БОССОВ", "2. LOS 3 SELLOS", "2. LES 3 SCEAUX"),
            LanguageSelectionUI.Localize("3. HOW TO WIN (ROLES)", "3. كيف تفوز (الأدوار)", "3. КАК ПОБЕДИТЬ", "3. CÓMO GANAR", "3. COMMENT GAGNER"),
            LanguageSelectionUI.Localize("4. CONTROLS & TIPS", "4. التحكم والنصائح", "4. УПРАВЛЕНИЕ И СОВЕТЫ", "4. CONTROLES Y CONSEJOS", "4. CONTRÔLES ET ASTUCES"),
            LanguageSelectionUI.Localize("🎬 VIDEO GUIDE", "🎬 دليل الفيديو", "🎬 ВИДЕОГИД", "🎬 VÍDEO GUÍA", "🎬 GUIDE VIDÉO")
        };

        float tabW = (modalW - 56f) / 5f;
        for (int i = 0; i < 5; i++)
        {
            Rect tabRect = new Rect(mx + 18f + i * (tabW + 5f), my + 46f, tabW, 32f);
            Color tabColor = (i == 4)
                ? new Color(0.32f, 0.95f, 0.88f)
                : ((currentSlide == i) ? new Color(1f, 0.82f, 0.25f) : new Color(0.22f, 0.55f, 0.35f));

            if (EnchantedNatureGUI.DrawOrganicLeafButton(tabRect, tabNames[i], tabColor, 10))
            {
                if (i == 4)
                {
                    isVideoGuideOpen = true;
                    videoCurrentTime = 0f;
                    isVideoPlaying = true;
                }
                else
                {
                    currentSlide = i;
                    briefingScrollPos = Vector2.zero;
                    autoPlayPresentation = false;
                    slideTimer = 0f;
                }
            }
        }

        // Auto-play progress bar
        if (autoPlayPresentation)
        {
            float pct = Mathf.Clamp01(slideTimer / SlideDuration);
            GUI.color = new Color(0.15f, 0.25f, 0.18f, 0.9f);
            GUI.DrawTexture(new Rect(mx + 20f, my + 82f, modalW - 40f, 4f), Texture2D.whiteTexture);
            GUI.color = new Color(0.35f, 0.95f, 0.55f, 0.95f);
            GUI.DrawTexture(new Rect(mx + 20f, my + 82f, (modalW - 40f) * pct, 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Content Area with Smooth Vertical Scrollbar
        Rect contentRect = new Rect(mx + 18f, my + 90f, modalW - 36f, modalH - 148f);
        float virtualH = GetSlideVirtualHeight(currentSlide);
        Rect viewRect = new Rect(0, 0, contentRect.width - (virtualH > contentRect.height ? 22f : 6f), virtualH);

        briefingScrollPos = GUI.BeginScrollView(contentRect, briefingScrollPos, viewRect, false, true);
        switch (currentSlide)
        {
            case 0:
                DrawSlide1Overview(viewRect);
                break;
            case 1:
                DrawSlide2ThreeBosses(viewRect);
                break;
            case 2:
                DrawSlide3RolesAndWinConditions(viewRect);
                break;
            case 3:
                DrawSlide4ControlsAndTips(viewRect);
                break;
        }
        GUI.EndScrollView();

        // Bottom Navigation Bar
        float navY = my + modalH - 50f;

        string prevBtn = LanguageSelectionUI.Localize("◀ PREVIOUS", "◀ السابق", "◀ НАЗАД", "◀ ANTERIOR", "◀ PRÉCÉDENT");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 18f, navY, 110f, 36f), prevBtn, new Color(0.4f, 0.75f, 0.95f), 11))
        {
            currentSlide = (currentSlide - 1 + TotalSlides) % TotalSlides;
            briefingScrollPos = Vector2.zero;
            autoPlayPresentation = false;
        }

        string nextBtn = LanguageSelectionUI.Localize("NEXT SLIDE ▶", "التالي ▶", "ВПЕРЕД ▶", "SIGUIENTE ▶", "SUIVANT ▶");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 136f, navY, 110f, 36f), nextBtn, new Color(0.4f, 0.75f, 0.95f), 11))
        {
            currentSlide = (currentSlide + 1) % TotalSlides;
            briefingScrollPos = Vector2.zero;
            autoPlayPresentation = false;
        }

        string playLabel = autoPlayPresentation
            ? LanguageSelectionUI.Localize("⏸ PAUSE", "⏸ إيقاف مؤقت", "⏸ ПАУЗА", "⏸ PAUSA", "⏸ PAUSE")
            : LanguageSelectionUI.Localize("▶ AUTO-PLAY", "▶ تشغيل تلقائي", "▶ АВТО", "▶ AUTO", "▶ LECTURE AUTO");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 254f, navY, 120f, 36f), playLabel, new Color(0.95f, 0.78f, 0.25f), 11))
        {
            autoPlayPresentation = !autoPlayPresentation;
            slideTimer = 0f;
        }

        string watchVideoBtn = LanguageSelectionUI.Localize("🎬 WATCH TACTICAL VIDEO GUIDE", "🎬 شاهد فيديو الشرح التكتيكي", "🎬 СМОТРЕТЬ ВИДЕОГИД", "🎬 VER VÍDEO TÁCTICO", "🎬 VOIR LE GUIDE VIDÉO");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 382f, navY, 260f, 36f), watchVideoBtn, new Color(0.25f, 0.92f, 1f), 12))
        {
            isVideoGuideOpen = true;
            videoCurrentTime = 0f;
            isVideoPlaying = true;
        }

        string enterBattleBtn = LanguageSelectionUI.Localize("⚔️ ENTER BATTLE (GOT IT!)", "⚔️ ادخل المعركة (فهمت!)", "⚔️ В БОЙ (ПОНЯТНО!)", "⚔️ ¡A LA BATALLA!", "⚔️ ENTRER AU COMBAT !");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + modalW - 215f, navY, 195f, 36f), enterBattleBtn, new Color(0.25f, 0.95f, 0.45f), 12))
        {
            showBriefingModal = false;
            autoPlayPresentation = false;
        }
    }

    private float GetSlideVirtualHeight(int slide)
    {
        switch (slide)
        {
            case 0: return 440f;
            case 1: return 490f;
            case 2: return 460f;
            case 3: return 440f;
            default: return 450f;
        }
    }

    private void DrawSlide1Overview(Rect area)
    {
        GUIStyle hStyle = CreateHeadingStyle(18, new Color(0.45f, 0.98f, 0.6f));
        GUIStyle bodyStyle = CreateBodyStyle(13, Color.white);

        string slide1Header = LanguageSelectionUI.Localize("🌳 THE BATTLE FOR THE SACRED BEANSTALK", "🌳 المعركة من أجل نبتة الفاصولياء المقدسة", "🌳 БИТВА ЗА СВЯЩЕННЫЙ БОБОВЫЙ СТЕБЕЛЬ", "🌳 LA BATALLA POR EL TALLO SAGRADO", "🌳 LA BATAILLE POUR LE HARICOT SACRÉ");
        GUI.Label(new Rect(area.x, area.y, area.width, 28f), slide1Header, hStyle);

        string slide1Intro = LanguageSelectionUI.Localize(
            "At the center of the forest stands the Giant Sacred Beanstalk. As it grows upward, it unlocks 3 high platforms (Level 1, Level 2, and Level 3), each guarded by a mighty Titan Boss!",
            "في قلب الغابة تقف نبتة الفاصولياء العملاقة المقدسة. كلما نمت للأعلى تفتح 3 منصات مرتفعة (المستوى 1 و2 و3)، ويحرس كل منصة زعيم جبار!",
            "В центре леса возвышается гигантский Священный стебель. Растя вверх, он открывает 3 платформы (Уровень 1, 2 и 3), каждую из которых охраняет могучий Босс!",
            "En el centro del bosque se alza el Tallo Sagrado. Al crecer, desbloquea 3 plataformas custodiadas por un temible Jefe Titán.",
            "Au centre de la forêt se dresse le Haricot Sacré Géant. En grandissant, il débloque 3 plateformes gardées par un puissant Boss Titan !"
        );
        GUI.Label(new Rect(area.x + 8f, area.y + 32f, area.width - 16f, 48f), slide1Intro, bodyStyle);

        float cardW = (area.width - 24f) / 3f;
        float cardH = 345f;
        float cy = area.y + 82f;

        // Card 1: Protectors
        Rect pRect = new Rect(area.x, cy, cardW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(pRect, new Color(0.06f, 0.18f, 0.28f, 0.95f), new Color(0.2f, 0.75f, 1f));
        string protTitle = LanguageSelectionUI.Localize("🛡️ PROTECTORS (BLUE)", "🛡️ الحماة (الفريق الأزرق)", "🛡️ ЗАЩИТНИКИ (СИНИЕ)", "🛡️ PROTECTORES (AZUL)", "🛡️ PROTECTEURS (BLEU)");
        GUI.Label(new Rect(pRect.x + 8f, pRect.y + 12f, pRect.width - 16f, 26f), protTitle, CreateHeadingStyle(14, new Color(0.35f, 0.88f, 1f), TextAnchor.MiddleCenter));

        string protBody = LanguageSelectionUI.Localize(
            "• Guardians of the Forest.\n\n• Your attacks on the Sacred Tree HEAL it instead of hurting it!\n\n• Slay the 3 Bosses to unseal the Sacred Crystal, then carry the Crystal to the Altar to WIN!",
            "• حراس الغابة المقدسة.\n\n• ضرباتكم على الشجرة المقدسة تشفيها وتزيد نقاط صحتها بدلاً من الإضرار بها!\n\n• اقضوا على الزعماء الثلاثة لفك أختام الكريستالة، ثم احملوها للمذبح للفوز بالمباراة!",
            "• Стражи леса.\n\n• Ваши атаки по Дереву ЛЕЧАТ его вместо нанесения урона!\n\n• Победите 3 Боссов, чтобы открыть Кристалл, и отнесите его к Алтарю для победы!",
            "• Guardianes del bosque.\n\n• ¡Tus ataques al Árbol lo CURAN en lugar de dañarlo!\n\n• ¡Vence a los 3 Jefes para liberar el Cristal y llévalo al Altar para ganar!",
            "• Gardiens de la Forêt.\n\n• Vos attaques sur l'Arbre le SOIGNENT au lieu de le blesser !\n\n• Éliminez les 3 Boss pour desceller le Cristal Sacré, puis portez-le à l'Autel pour GAGNER !"
        );
        GUI.Label(new Rect(pRect.x + 12f, pRect.y + 44f, pRect.width - 24f, cardH - 52f), protBody, bodyStyle);

        // Card 2: The Sacred Tree & 3 Levels
        Rect tRect = new Rect(area.x + cardW + 12f, cy, cardW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(tRect, new Color(0.08f, 0.20f, 0.10f, 0.95f), new Color(0.35f, 0.95f, 0.45f));
        string treeTitle = LanguageSelectionUI.Localize("🌲 THE 3 TREE LEVELS", "🌲 مستويات الشجرة الثلاثة", "🌲 3 УРОВНЯ ДЕРЕВА", "🌲 LOS 3 NIVELES DEL ÁRBOL", "🌲 LES 3 NIVEAUX DE L'ARBRE");
        GUI.Label(new Rect(tRect.x + 8f, tRect.y + 12f, tRect.width - 16f, 26f), treeTitle, CreateHeadingStyle(14, new Color(0.45f, 1f, 0.55f), TextAnchor.MiddleCenter));

        float animGrowth = 0.35f + Mathf.PingPong(Time.time * 0.25f, 0.65f);
        float barX = tRect.x + 20f;
        float barY = tRect.y + 48f;
        float barH = cardH - 64f;
        GUI.color = new Color(0.15f, 0.12f, 0.08f, 0.9f);
        GUI.DrawTexture(new Rect(barX, barY, 22f, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.28f, 0.88f, 0.38f, 0.95f);
        float fillH = barH * animGrowth;
        GUI.DrawTexture(new Rect(barX, barY + (barH - fillH), 22f, fillH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string lblTop = LanguageSelectionUI.Localize("💎 TOP: Crystal Unsealed (After 3 Bosses Die)", "💎 القمة: فتح الكريستالة (بعد موت الزعماء 3)", "💎 ВЕРШИНА: Кристалл (после гибели 3 боссов)", "💎 CIMA: Cristal disponible (tras 3 Jefes)", "💎 SOMMET : Cristal libéré (après 3 Boss)");
        string lblLv3 = LanguageSelectionUI.Localize("👑 LEVEL 3: Ancient Overlord (300% Power)", "👑 المستوى 3: الحاكم القديم (300% قوة)", "👑 УРОВЕНЬ 3: Древний Владыка (300% силы)", "👑 NIVEL 3: Soberano Ancestral (300% poder)", "👑 NIVEAU 3 : Seigneur Ancien (300% puissance)");
        string lblLv2 = LanguageSelectionUI.Localize("👑 LEVEL 2: Inferno Titan (220% Power)", "👑 المستوى 2: عملاق الجحيم (220% قوة)", "👑 УРОВЕНЬ 2: Титан Преисподней (220% силы)", "👑 NIVEL 2: Titán Infernal (220% poder)", "👑 NIVEAU 2 : Titan Infernal (220% puissance)");
        string lblLv1 = LanguageSelectionUI.Localize("👑 LEVEL 1: Forest Goliath (150% Power)", "👑 المستوى 1: جالوت الغابة (150% قوة)", "👑 УРОВЕНЬ 1: Лесной Голиаф (150% силы)", "👑 NIVEL 1: Goliat del Bosque (150% poder)", "👑 NIVEAU 1 : Goliath de la Forêt (150% puissance)");

        GUI.Label(new Rect(barX + 30f, barY, tRect.width - 60f, 34f), lblTop, CreateBodyStyle(12, new Color(0.4f, 0.95f, 1f)));
        GUI.Label(new Rect(barX + 30f, barY + barH * 0.28f, tRect.width - 60f, 34f), lblLv3, CreateBodyStyle(12, new Color(1f, 0.85f, 0.3f)));
        GUI.Label(new Rect(barX + 30f, barY + barH * 0.55f, tRect.width - 60f, 34f), lblLv2, CreateBodyStyle(12, new Color(1f, 0.55f, 0.25f)));
        GUI.Label(new Rect(barX + 30f, barY + barH * 0.80f, tRect.width - 60f, 34f), lblLv1, CreateBodyStyle(12, new Color(0.45f, 0.95f, 0.45f)));

        // Card 3: Destroyers
        Rect dRect = new Rect(area.x + (cardW + 12f) * 2f, cy, cardW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(dRect, new Color(0.26f, 0.08f, 0.07f, 0.95f), new Color(1f, 0.32f, 0.2f));
        string destTitle = LanguageSelectionUI.Localize("🔥 DESTROYERS (RED)", "🔥 المدمرون (الفريق الأحمر)", "🔥 РАЗРУШИТЕЛИ (КРАСНЫЕ)", "🔥 DESTRUCTORES (ROJO)", "🔥 DESTRUCTEURS (ROUGE)");
        GUI.Label(new Rect(dRect.x + 8f, dRect.y + 12f, dRect.width - 16f, 26f), destTitle, CreateHeadingStyle(14, new Color(1f, 0.42f, 0.28f), TextAnchor.MiddleCenter));

        string destBody = LanguageSelectionUI.Localize(
            "• Conquerors of the Forest.\n\n• The Tree Shield protects the Tree from falling below 15% HP until all 3 Bosses fall, followed by a 10s Divine Countdown!\n\n• Strike from higher platforms for up to 100% full damage (Ground deals only 20%) to fell the Tree and WIN!",
            "• غزاة الغابة.\n\n• درع الشجرة يحميها من السقوط دون 15% حتى يسقط الزعماء الثلاثة، يليه عد تنازلي 10 ثوانٍ للدرع!\n\n• هاجموا من المنصات المرتفعة للحصول على القوة الكاملة 100% (الأرض تعطي 20% فقط) لإسقاط الشجرة والفوز!",
            "• Завоеватели леса.\n\n• Щит защищает Дерево ниже 15% HP до гибели 3 Боссов, после чего идет 10-секундный отсчет!\n\n• Атакуйте с верхних ярусов для 100% урона (с земли лишь 20%), чтобы срубить Дерево и победить!",
            "• Conquistadores del bosque.\n\n• El escudo protege el Árbol hasta que caigan los 3 Jefes, seguido de una cuenta de 10s.\n\n• ¡Ataca desde plataformas altas para el 100% del daño (en el suelo solo 20%) y derriba el Árbol!",
            "• Conquérants de la Forêt.\n\n• Le bouclier protège l'Arbre jusqu'à la chute des 3 Boss, suivi d'un compte de 10s !\n\n• Attaquez depuis les hauteurs pour 100% des dégâts (au sol seulement 20%) pour abattre l'Arbre !"
        );
        GUI.Label(new Rect(dRect.x + 12f, dRect.y + 44f, dRect.width - 24f, cardH - 52f), destBody, bodyStyle);
    }

    private void DrawSlide2ThreeBosses(Rect area)
    {
        GUIStyle hStyle = CreateHeadingStyle(18, new Color(1f, 0.86f, 0.28f));
        GUIStyle bodyStyle = CreateBodyStyle(13, Color.white);

        string slide2Header = LanguageSelectionUI.Localize("👑 THE 3 GUARDIAN BOSSES — KEYS TO VICTORY", "👑 زعماء الحراسة الثلاثة — مفتاح النصر", "👑 3 БОССА-СТРАЖА — КЛЮЧ К ПОБЕДЕ", "👑 LOS 3 JEFES GUARDIANES — CLAVE DE LA VICTORIA", "👑 LES 3 BOSS GARDIENS — CLÉS DE LA VICTOIRE");
        GUI.Label(new Rect(area.x, area.y, area.width, 28f), slide2Header, hStyle);

        string slide2Rule = LanguageSelectionUI.Localize(
            "CRITICAL RULE: Neither team can finish the match until ALL 3 LEVEL BOSSES ARE DEFEATED!\n• The Tree Shield prevents the Tree from falling below 15% HP until all 3 Bosses die.\n• When Boss 3 dies, a 10-Second Divine Shield Countdown begins before the shield shatters completely!\n• The Sacred Crystal remains sealed until all 3 Bosses die.",
            "قاعدة حاسمة: لا يمكن لأي فريق إنهاء المباراة حتى يُهزم الزعماء الثلاثة جميعهم!\n• درع الشجرة يمنع سقوط صحة الشجرة دون 15% حتى يموت الزعماء الثلاثة.\n• عند موت الزعيم الثالث، يبدأ عد تنازلي 10 ثوانٍ لدرع الصمود المقدس قبل زواله تماماً!\n• الكريستالة المقدسة تظل مغلقة ومحكمة الختم حتى يموت الزعماء الثلاثة.",
            "ГЛАВНОЕ ПРАВИЛО: Ни одна команда не может победить, пока ВСЕ 3 БОССА НЕ БУДУТ ПОВЕРЖЕНЫ!\n• Щит не дает здоровью Дерева упасть ниже 15%.\n• При гибели 3-го Босса включается 10-секундный Божественный отсчет до падения щита!\n• Священный Кристалл запечатан до гибели всех 3 Боссов.",
            "REGLA CRÍTICA: ¡Ningún equipo puede ganar hasta DERROTAR A LOS 3 JEFES!\n• El Escudo evita que el Árbol baje del 15% de salud.\n• ¡Al morir el 3er Jefe, inicia una cuenta atrás de 10s del Escudo Divino antes de caer!\n• El Cristal Sagrado permanece sellado hasta que mueran los 3 Jefes.",
            "RÈGLE CRITIQUE : Aucune équipe ne peut gagner tant que LES 3 BOSS NE SONT PAS VAINCUS !\n• Le Bouclier empêche l'Arbre de descendre sous 15% de PV.\n• À la mort du 3e Boss, un compte à rebours de 10s du Bouclier Divin démarre avant sa chute !\n• Le Cristal Sacré reste scellé tant que les 3 Boss sont en vie."
        );
        GUI.Label(new Rect(area.x + 8f, area.y + 32f, area.width - 16f, 48f), slide2Rule, bodyStyle);

        float cardW = (area.width - 24f) / 3f;
        float cardH = 380f;
        float cy = area.y + 84f;

        DrawBossCard(
            new Rect(area.x, cy, cardW, cardH),
            LanguageSelectionUI.Localize("👑 BOSS 1: FOREST GOLIATH", "👑 الزعيم 1: جالوت الغابة", "👑 БОСС 1: ЛЕСНОЙ ГОЛИАФ", "👑 JEFE 1: GOLIAT DEL BOSQUE", "👑 BOSS 1 : GOLIATH DE LA FORÊT"),
            LanguageSelectionUI.Localize("Platform: LEVEL 1 (Active from Start)", "المنصة: المستوى 1 (نشط منذ البداية)", "Платформа: УРОВЕНЬ 1 (активен с начала)", "Plataforma: NIVEL 1 (activo desde el inicio)", "Plateforme : NIVEAU 1 (actif dès le début)"),
            LanguageSelectionUI.Localize("HP: 1,800  |  Reward: +250 Coins & 500 XP", "الصحة: 1,800  |  المكافأة: +250 نقود و 500 خبرة", "Здоровье: 1800  |  Награда: +250 монет и 500 опыта", "Salud: 1800  |  Recompensa: +250 Monedas y 500 XP", "PV : 1800  |  Récompense : +250 Pièces & 500 XP"),
            LanguageSelectionUI.Localize("Guards the Lower Canopy. Both teams must climb Level 1 immediately at the start of battle to contest this Boss!", "يحرس المظلة السفلية. يجب على كلا الفريقين صعود المستوى 1 فور بدء المعركة للقتال عليه!", "Охраняет нижний ярус. Обе команды должны сразу подняться на Уровень 1 для битвы с ним!", "Custodia el dosel inferior. ¡Ambos equipos deben subir al Nivel 1 de inmediato para combatirlo!", "Garde la canopée inférieure. Les deux équipes doivent monter au Niveau 1 dès le début !"),
            new Color(0.25f, 0.85f, 0.35f)
        );

        DrawBossCard(
            new Rect(area.x + cardW + 12f, cy, cardW, cardH),
            LanguageSelectionUI.Localize("👑 BOSS 2: INFERNO TITAN", "👑 الزعيم 2: عملاق الجحيم", "👑 БОСС 2: ТИТАН ПРЕИСПОДНЕЙ", "👑 JEFE 2: TITÁN INFERNAL", "👑 BOSS 2 : TITAN INFERNAL"),
            LanguageSelectionUI.Localize("Platform: LEVEL 2 (Unlocks at 35% Growth)", "المنصة: المستوى 2 (يفتح عند 35% نمو)", "Платформа: УРОВЕНЬ 2 (открывается при 35% роста)", "Plataforma: NIVEL 2 (se abre al 35% crecimiento)", "Plateforme : NIVEAU 2 (débloqué à 35% de croissance)"),
            LanguageSelectionUI.Localize("HP: 2,600  |  Reward: +400 Coins & 750 XP", "الصحة: 2,600  |  المكافأة: +400 نقود و 750 خبرة", "Здоровье: 2600  |  Награда: +400 монет и 750 опыта", "Salud: 2600  |  Recompensa: +400 Monedas y 750 XP", "PV : 2600  |  Récompense : +400 Pièces & 750 XP"),
            LanguageSelectionUI.Localize("Awakens as soon as the Beanstalk reaches Level 2. Deals heavy fiery strikes and drops a Spoils Chest on death!", "يستيقظ فور وصول النبتة للمستوى 2. يوجه ضربات نارية حارقة ويسقط صندوق غنائم عند موته!", "Пробуждается, когда Стебель достигает 2 уровня. Наносит огненные удары и оставляет сундук с трофеями!", "Despierta al llegar el tallo al Nivel 2. ¡Lanza golpes ardientes y suelta un cofre de botín al morir!", "S'éveille dès que le haricot atteint le Niveau 2. Frappes enflammées et coffre de butin à sa mort !"),
            new Color(1f, 0.45f, 0.18f)
        );

        DrawBossCard(
            new Rect(area.x + (cardW + 12f) * 2f, cy, cardW, cardH),
            LanguageSelectionUI.Localize("👑 BOSS 3: ANCIENT OVERLORD", "👑 الزعيم 3: الحاكم القديم", "👑 БОСС 3: ДРЕВНИЙ ВЛАДЫКА", "👑 JEFE 3: SOBERANO ANCESTRAL", "👑 BOSS 3 : SEIGNEUR ANCIEN"),
            LanguageSelectionUI.Localize("Platform: LEVEL 3 (Unlocks at 65% Growth)", "المنصة: المستوى 3 (يفتح عند 65% نمو)", "Платформа: УРОВЕНЬ 3 (открывается при 65% роста)", "Plataforma: NIVEL 3 (se abre al 65% crecimiento)", "Plateforme : NIVEAU 3 (débloqué à 65% de croissance)"),
            LanguageSelectionUI.Localize("HP: 3,500  |  Reward: +600 Coins & 1200 XP", "الصحة: 3,500  |  المكافأة: +600 نقود و 1200 خبرة", "Здоровье: 3500  |  Награда: +600 монет и 1200 опыта", "Salud: 3500  |  Recompensa: +600 Monedas y 1200 XP", "PV : 3500  |  Récompense : +600 Pièces & 1200 XP"),
            LanguageSelectionUI.Localize("The final Seal Guardian! Slaying the Overlord starts the 10s Divine Shield countdown for Destroyers AND unseals the Crystal for Protectors!", "حارس الختم الأخير! القضاء عليه يطلق عد 10 ثوانٍ لدرع الشجرة للمدمرين ويحرر الكريستالة للحماة!", "Последний страж! Победа над ним запускает 10-секундный отсчет снятия щита И освобождает Кристалл!", "¡El último guardián! Derrotarlo inicia la cuenta de 10s del escudo para Destructores Y libera el Cristal.", "Le dernier gardien ! Le vaincre lance le compte de 10s du bouclier pour les Destructeurs ET libère le Cristal !"),
            new Color(1f, 0.82f, 0.22f)
        );
    }

    private void DrawBossCard(Rect r, string title, string loc, string stats, string desc, Color accent)
    {
        EnchantedNatureGUI.DrawLeafPlaque(r, new Color(0.08f, 0.12f, 0.09f, 0.98f), accent);

        // Header plate for title so it never wraps or touches the outer border
        Rect titleRect = new Rect(r.x + 6f, r.y + 8f, r.width - 12f, 28f);
        EnchantedNatureGUI.DrawWoodPlate(titleRect, new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f, 0.95f), accent);
        GUI.Label(titleRect, title, CreateHeadingStyle(12, accent, TextAnchor.MiddleCenter));

        GUI.Label(new Rect(r.x + 10f, r.y + 40f, r.width - 20f, 22f), loc, CreateBodyStyle(12, new Color(0.85f, 0.95f, 1f)));
        GUI.Label(new Rect(r.x + 10f, r.y + 64f, r.width - 20f, 22f), stats, CreateBodyStyle(12, new Color(1f, 0.9f, 0.4f)));
        GUI.Label(new Rect(r.x + 10f, r.y + 88f, r.width - 20f, 96f), desc, CreateBodyStyle(12, new Color(0.92f, 0.95f, 0.92f)));

        // Dedicated plaque for LAST HIT BONUS
        Rect bonusRect = new Rect(r.x + 6f, r.y + 192f, r.width - 12f, 176f);
        EnchantedNatureGUI.DrawLeafPlaque(bonusRect, new Color(0.04f, 0.08f, 0.06f, 0.95f), new Color(1f, 0.82f, 0.25f, 0.7f));
        string bonusHeader = LanguageSelectionUI.Localize("⚡ LAST HIT BONUS", "⚡ مكافأة الضربة القاضية", "⚡ БОНУС ДОБИВАНИЯ", "⚡ BONO DEL ÚLTIMO GOLPE", "⚡ BONUS DU COUP DE GRÂCE");
        GUI.Label(new Rect(bonusRect.x + 6f, bonusRect.y + 6f, bonusRect.width - 12f, 22f), bonusHeader, CreateHeadingStyle(12, new Color(1f, 0.88f, 0.3f), TextAnchor.MiddleCenter));

        string bonusText = LanguageSelectionUI.Localize(
            "• Protectors Kill:\n  Heals Sacred Tree +350 HP!\n  Boosts Growth Rate!\n\n• Destroyers Kill:\n  Deals -260 Damage to Tree!\n  Speeds Tree Fall!",
            "• قتله الحماة:\n  شفاء الشجرة المقدسة +350 صحة!\n  تسريع معدل نمو الشجرة!\n\n• قتله المدمرون:\n  إلحاق -260 ضرر بالشجرة!\n  تسريع سقوط الشجرة!",
            "• Убийство Защитниками:\n  Лечит Дерево на +350 HP!\n  Ускоряет рост!\n\n• Убийство Разрушителями:\n  Наносит -260 урона Дереву!\n  Приближает падение!",
            "• Si matan Protectores:\n  ¡Cura el Árbol +350 Salud!\n  ¡Acelera el crecimiento!\n\n• Si matan Destructores:\n  ¡Inflige -260 Daño al Árbol!\n  ¡Acelera la caída!",
            "• Tué par Protecteurs :\n  Soigne l'Arbre +350 PV !\n  Accélère la croissance !\n\n• Tué par Destructeurs :\n  Inflige -260 Dégâts à l'Arbre !\n  Accélère sa chute !"
        );
        GUI.Label(new Rect(bonusRect.x + 8f, bonusRect.y + 30f, bonusRect.width - 16f, bonusRect.height - 36f), bonusText, CreateBodyStyle(12, Color.white));
    }

    private void DrawSlide3RolesAndWinConditions(Rect area)
    {
        float halfW = (area.width - 18f) / 2f;
        float cardH = 430f;

        // Left Panel: Protector Step-by-Step Guide
        Rect left = new Rect(area.x, area.y, halfW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(left, new Color(0.05f, 0.16f, 0.26f, 0.96f), new Color(0.25f, 0.85f, 1f));
        string protRoadmap = LanguageSelectionUI.Localize("🛡️ PROTECTOR ROADMAP TO VICTORY", "🛡️ خارطة طريق الحماة نحو النصر", "🛡️ ПЛАН ПОБЕДЫ ЗАЩИТНИКОВ", "🛡️ RUTA DE VICTORIA DE LOS PROTECTORES", "🛡️ FEUILLE DE ROUTE DES PROTECTEURS");
        GUI.Label(new Rect(left.x + 14f, left.y + 12f, left.width - 28f, 28f), protRoadmap, CreateHeadingStyle(15, new Color(0.35f, 0.92f, 1f)));

        string protectorSteps = LanguageSelectionUI.Localize(
            "STEP 1 — DEFEND & HEAL THE TREE:\nAttack the Sacred Tree trunk whenever it is damaged to restore its HP.\n\nSTEP 2 — SLAY ALL 3 BOSSES (LEVELS 1, 2, 3):\nClimb the platforms and defeat the 3 Bosses to accelerate tree growth and break the 3 Crystal Seals.\n\nSTEP 3 — GRAB THE SACRED CRYSTAL:\nWhen all 3 Bosses die, 'THE CRYSTAL IS READY!' Picking it up grants +35% Speed Boost and Sacred Feather Fall (0 fall damage)!\n\nSTEP 4 — DELIVER TO THE SACRED ALTAR:\nCarry the Crystal to the glowing Sacred Altar on the ground (Press [E] for Shockwave Blast) to WIN!",
            "الخطوة 1 — الدفاع عن الشجرة وعلاجها:\nهاجم جذع الشجرة المقدسة كلما تضررت لاستعادة نقاط صحتها.\n\nالخطوة 2 — القضاء على الزعماء الثلاثة (مستويات 1، 2، 3):\nاصعد المنصات واهزم الزعماء لتسريع نمو الشجرة وفك أختام الكريستالة الثلاثة.\n\nالخطوة 3 — أخذ الكريستالة المقدسة:\nعند موت الزعماء الثلاثة تظهر رسالة: 'الكريستالة جاهزة!'. حمل الكريستالة يمنح +35% سرعة إضافية وسقوط الريشة المقدس (0 ضرر سقوط)!\n\nالخطوة 4 — إيصالها إلى المذبح المقدس:\nاحمل الكريستالة إلى المذبح المتوهج على الأرض (اضغط [E] لإطلاق موجة صدمة) لتحقيق الفوز!",
            "ШАГ 1 — ЗАЩИТА И ЛЕЧЕНИЕ ДЕРЕВА:\nАтакуйте ствол Дерева, чтобы восстановить его здоровье.\n\nШАГ 2 — ПОБЕДИТЕ 3 БОССОВ (УРОВНИ 1, 2, 3):\nПоднимайтесь на платформы и побеждайте Боссов, снимая 3 Печати.\n\nШАГ 3 — ВОЗЬМИТЕ КРИСТАЛЛ:\nПри гибели 3 Боссов Кристалл готов! Носитель получает +35% к скорости и Падение Перышком (0 урона от падений)!\n\nШАГ 4 — ДОСТАВЬТЕ К АЛТАРЮ:\nОтнесите Кристалл к Алтарю на земле (нажмите [E] для волны) для ПОБЕДЫ!",
            "PASO 1 — DEFENDER Y CURAR EL ÁRBOL:\nAtaca el tronco para restaurar su salud.\n\nPASO 2 — VENCER A LOS 3 JEFES (NIVELES 1, 2, 3):\nSube a las plataformas y derrota a los 3 Jefes para romper los sellos.\n\nPASO 3 — RECOGER EL CRISTAL SAGRADO:\n¡El portador del Cristal recibe +35% de velocidad y Caída de Pluma Sagrada (0 daño de caída)!\n\nPASO 4 — LLEVARLO AL ALTAR SAGRADO:\nLlévalo al Altar en el suelo (pulsa [E] para onda expansiva) ¡para GANAR!",
            "ÉTAPE 1 — DÉFENDRE ET SOIGNER L'ARBRE :\nAttaquez le tronc pour restaurer ses PV.\n\nÉTAPE 2 — VAINCRE LES 3 BOSS (NIVEAUX 1, 2, 3) :\nMontez sur les plateformes et éliminez les 3 Boss pour briser les sceaux.\n\nÉTAPE 3 — PRENDRE LE CRISTAL SACRÉ :\nLe porteur du Cristal gagne +35% de vitesse et Chute de Plume Sacrée (0 dégât de chute) !\n\nÉTAPE 4 — LIVRER À L'AUTEL SACRÉ :\nPortez le Cristal à l'Autel au sol (appuyez sur [E] pour onde de choc) pour GAGNER !"
        );
        GUI.Label(new Rect(left.x + 16f, left.y + 48f, left.width - 32f, left.height - 60f), protectorSteps, CreateBodyStyle(12, Color.white));

        // Right Panel: Destroyer Step-by-Step Guide
        Rect right = new Rect(area.x + halfW + 18f, area.y, halfW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(right, new Color(0.25f, 0.07f, 0.06f, 0.96f), new Color(1f, 0.35f, 0.22f));
        string destRoadmap = LanguageSelectionUI.Localize("🔥 DESTROYER ROADMAP TO VICTORY", "🔥 خارطة طريق المدمرين نحو النصر", "🔥 ПЛАН ПОБЕДЫ РАЗРУШИТЕЛЕЙ", "🔥 RUTA DE VICTORIA DE LOS DESTRUCTORES", "🔥 FEUILLE DE ROUTE DES DESTRUCTEURS");
        GUI.Label(new Rect(right.x + 14f, right.y + 12f, right.width - 28f, 28f), destRoadmap, CreateHeadingStyle(15, new Color(1f, 0.45f, 0.28f)));

        string destroyerSteps = LanguageSelectionUI.Localize(
            "STEP 1 — WEAKEN THE TREE & HUNT BOSSES:\nStrike the Tree down toward its 15% Shield limit, and climb Levels 1, 2, and 3 to slay the 3 Guardian Bosses.\n\nSTEP 2 — 10s DIVINE SHIELD COUNTDOWN:\nWhen Boss 3 falls, a 10-second Divine countdown begins before the shield shatters completely!\n\nSTEP 3 — INTERCEPT CRYSTAL CARRIERS:\nIf a Protector grabs the Crystal, hunt them down immediately so they drop it on the ground!\n\nSTEP 4 — TOPPLE THE SACRED TREE:\nUnleash attacks on the Tree! Higher platforms grant full damage (Ground: 20%, L1: 35%, L2: 50%, L3/Top: 100%) until HP reaches 0 to WIN!",
            "الخطوة 1 — إضعاف الشجرة وصيد الزعماء:\nاضربوا الشجرة حتى حد درع 15%، واصعدوا المستويات 1 و2 و3 للقضاء على الزعماء الثلاثة.\n\nالخطوة 2 — عد تنازلي 10 ثوانٍ لدرع الصمود:\nعند سقوط الزعيم الثالث، يبدأ عد تنازلي مدته 10 ثوانٍ قبل زوال الدرع بالكامل!\n\nالخطوة 3 — اعتراض حاملي الكريستالة:\nإذا أخذ أحد الحماة الكريستالة، لاحقوه فوراً واقضوا عليه لتسقط الكريستالة على الأرض!\n\nالخطوة 4 — إسقاط الشجرة المقدسة:\nهاجموا جذع الشجرة! المنصات العالية تمنح القوة الكاملة (الأرض: 20%، م1: 35%، م2: 50%، م3/القمة: 100%) حتى تصل صحتها إلى 0 للفوز!",
            "ШАГ 1 — ОСЛАБЬТЕ ДЕРЕВО И ОХОТЬТЕСЬ НА БОССОВ:\nАтакуйте Дерево до предела Щита в 15% и поднимайтесь на Уровни 1, 2, 3 для убийства Боссов.\n\nШАГ 2 — 10 СЕКУНД ДО СНЯТИЯ ЩИТА:\nПри гибели 3-го Босса включается 10-секундный отсчет до полного снятия щита!\n\nШАГ 3 — ПЕРЕХВАТЫВАЙТЕ КРИСТАЛЛ:\nЕсли Защитник взял Кристалл, немедленно уничтожьте его, чтобы выбить Кристалл!\n\nШАГ 4 — СРУБИТЕ СВЯЩЕННОЕ ДЕРЕВО:\nОбрушьте всю мощь на ствол! Высота дает полный урон (Земля: 20%, Ур1: 35%, Ур2: 50%, Ур3/Верх: 100%) до 0 HP для победы!",
            "PASO 1 — DEBILITAR EL ÁRBOL Y CAZAR JEFES:\nGolpea el Árbol hasta el 15% y sube a los Niveles 1, 2 y 3 para abatir a los 3 Jefes.\n\nPASO 2 — CUENTA ATRÁS DE 10S DEL ESCUDO:\nAl caer el 3er Jefe, se inicia una cuenta de 10s antes de que caiga el escudo.\n\nPASO 3 — INTERCEPTAR PORTADORES DEL CRISTAL:\nSi un Protector toma el Cristal, ¡cázalo de inmediato para que lo suelte!\n\nPASO 4 — DERRIBAR EL ÁRBOL SAGRADO:\n¡Ataca el tronco! Las alturas dan daño completo (Suelo: 20%, N1: 35%, N2: 50%, N3/Cima: 100%) hasta 0 de salud para GANAR.",
            "ÉTAPE 1 — AFFAIBLIR L'ARBRE ET CHASSER LES BOSS :\nFrappez l'Arbre jusqu'à 15% de PV et montez aux Niveaux 1, 2 et 3 pour vaincre les 3 Boss.\n\nÉTAPE 2 — COMPTE À REBOURS DE 10S DU BOUCLIER :\nÀ la mort du 3e Boss, un compte à rebours de 10s s'enclenche avant la chute du bouclier !\n\nÉTAPE 3 — INTERCEPTER LES PORTEURS DU CRISTAL :\nSi un Protecteur prend le Cristal, éliminez-le vite pour lui faire lâcher le Cristal !\n\nÉTAPE 4 — ABATTRE L'ARBRE SACRÉ :\nAttaquez le tronc ! Les hauteurs donnent le plein impact (Sol : 20%, N1 : 35%, N2 : 50%, N3/Sommet : 100%) jusqu'à 0 PV pour GAGNER !"
        );
        GUI.Label(new Rect(right.x + 16f, right.y + 48f, right.width - 32f, right.height - 60f), destroyerSteps, CreateBodyStyle(12, Color.white));
    }

    private void DrawSlide4ControlsAndTips(Rect area)
    {
        string slide4Header = LanguageSelectionUI.Localize("⚔️ CONTROLS, MOVEMENT & PRO COMBAT SECRETS", "⚔️ أزرار التحكم والحركة وأسرار القتال الاحترافي", "⚔️ УПРАВЛЕНИЕ, ДВИЖЕНИЕ И БОЕВЫЕ СЕКРЕТЫ", "⚔️ CONTROLES, MOVIMIENTO Y SECRETOS DE COMBATE", "⚔️ CONTRÔLES, DÉPLACEMENT ET SECRETS DE COMBAT");
        GUI.Label(new Rect(area.x, area.y, area.width, 28f), slide4Header, CreateHeadingStyle(18, new Color(0.45f, 1f, 0.6f)));

        float colW = (area.width - 18f) / 2f;
        float cardH = 390f;
        Rect c1 = new Rect(area.x, area.y + 36f, colW, cardH);
        Rect c2 = new Rect(area.x + colW + 18f, area.y + 36f, colW, cardH);

        EnchantedNatureGUI.DrawLeafPlaque(c1, new Color(0.08f, 0.14f, 0.11f, 0.95f), new Color(0.35f, 0.88f, 0.5f));
        string kbTitle = LanguageSelectionUI.Localize("🎮 KEYBOARD & MOUSE CONTROLS", "🎮 أزرار لوحة المفاتيح والفأرة", "🎮 УПРАВЛЕНИЕ КЛАВИАТУРОЙ И МЫШЬЮ", "🎮 CONTROLES DE TECLADO Y RATÓN", "🎮 CONTRÔLES CLAVIER & SOURIS");
        GUI.Label(new Rect(c1.x + 14f, c1.y + 12f, c1.width - 28f, 26f), kbTitle, CreateHeadingStyle(14, new Color(1f, 0.88f, 0.35f)));

        string controlsText = LanguageSelectionUI.Localize(
            "• [W, A, S, D] : Move Character\n\n• [Hold Left Shift] : Sprint / Fast Run\n\n• [Space (1x)] : Jump & Ascend\n\n• [Space (2x in Air)] : Double Jump!\n\n• [Left Mouse Click / F] : Fire Bow / Attack\n\n• [E] (Crystal Carrier) : Shockwave Blast\n\n• [Q] (Crystal Carrier) : Drop Crystal\n\n• [B] : Open Emporium Shop  |  [H / V] : Guide / Video",
            "• [W, A, S, D] : تحريك الشخصية\n\n• [Shift الأيسر] : الجري السريع\n\n• [مسافة (مرة واحدة)] : القفز والصعود\n\n• [مسافة (مرتين في الهواء)] : قفز مزدوج!\n\n• [زر الفأرة الأيسر / F] : رمي السهام / الهجوم\n\n• [E] (لحامل الكريستال) : إطلاق موجة صدمة\n\n• [Q] (لحامل الكريستال) : إسقاط الكريستال\n\n• [B] : متجر الأسلحة  |  [H / V] : الدليل / الفيديو",
            "• [W, A, S, D] : Перемещение героя\n\n• [Удерживать Shift] : Быстрый бег / спринт\n\n• [Пробел (1x)] : Прыжок и подъем\n\n• [Пробел (2x в воздухе)] : Двойной прыжок!\n\n• [ЛКМ / F] : Стрельба из лука / Атака\n\n• [E] (Носитель кристалла) : Ударная волна\n\n• [Q] (Носитель кристалла) : Бросить кристалл\n\n• [B] : Магазин  |  [H / V] : Руководство / Видео",
            "• [W, A, S, D] : Mover personaje\n\n• [Mantener Shift] : Correr rápido\n\n• [Espacio (1x)] : Saltar y elevarse\n\n• [Espacio (2x en el aire)] : ¡Doble salto!\n\n• [Clic Izquierdo / F] : Disparar arco / Atacar\n\n• [E] (Portador del cristal) : Onda expansiva\n\n• [Q] (Portador del cristal) : Soltar cristal\n\n• [B] : Tienda  |  [H / V] : Guía / Vídeo",
            "• [W, A, S, D] : Déplacer le personnage\n\n• [Maintenir Shift] : Sprint / Course rapide\n\n• [Espace (1x)] : Sauter et s'élever\n\n• [Espace (2x en l'air)] : Double saut !\n\n• [Clic Gauche / F] : Tirer à l'arc / Attaque\n\n• [E] (Porteur du cristal) : Onde de choc\n\n• [Q] (Porteur du cristal) : Lâcher le cristal\n\n• [B] : Boutique  |  [H / V] : Guide / Vidéo"
        );
        GUI.Label(new Rect(c1.x + 16f, c1.y + 46f, c1.width - 32f, c1.height - 56f), controlsText, CreateBodyStyle(12, Color.white));

        EnchantedNatureGUI.DrawLeafPlaque(c2, new Color(0.08f, 0.14f, 0.11f, 0.95f), new Color(0.95f, 0.78f, 0.25f));
        string proTipsTitle = LanguageSelectionUI.Localize("💡 VETERAN PRO TIPS", "💡 نصائح المحترفين المخضرمين", "💡 СОВЕТЫ ОПЫТНЫХ ИГРОКОВ", "💡 CONSEJOS PRO PARA VETERANOS", "💡 ASTUCES DE PRO VÉTÉRAN");
        GUI.Label(new Rect(c2.x + 14f, c2.y + 12f, c2.width - 28f, 26f), proTipsTitle, CreateHeadingStyle(14, new Color(0.4f, 0.92f, 1f)));

        string proTipsText = LanguageSelectionUI.Localize(
            "1. HEIGHT MULTIPLIER SCALING:\nAttacking or healing the Tree scales with elevation:\n   • Ground: 20% impact  |  Level 1: 35%\n   • Level 2: 50% impact  |  Level 3 & Top: 100% full effect!\n\n2. FALL DAMAGE & FEATHER FALL:\nFalling >6m deals fall damage (12-38 HP). However, the Sacred Crystal carrier has Sacred Feather Fall (0 fall damage!) + 35% speed boost!\n\n3. 10s DIVINE SHIELD COUNTDOWN:\nWhen Boss 3 dies, a 10s countdown flashes on the Tree shield before it breaks completely, giving teams a final tactical rally window!",
            "1. مضاعف الارتفاع وقوة التأثير:\nمهاجمة أو شفاء الشجرة يتدرج بدقة حسب الارتفاع:\n   • الأرض: 20% تأثير  |  المستوى 1: 35%\n   • المستوى 2: 50% تأثير  |  المستوى 3 والقمة: 100% التأثير الكامل!\n\n2. ضرر السقوط وسقوط الريشة المقدس:\nالسقوط من علو (>6 أمتار) يلحق ضرراً (12-38 صحة). لكن حامل الكريستالة محمي ببركة الريشة (0 ضرر سقوط!) مع زيادة سرعة +35%!\n\n3. عد تنازلي 10 ثوانٍ لدرع الصمود:\nعند موت الزعيم الثالث، يظهر عد تنازلي 10 ثوانٍ فوق درع الشجرة قبل زواله، ليعطي الحماة فرصة للصمود والمدمرين فرصة للتجمع!",
            "1. МНОЖИТЕЛЬ ВЫСОТЫ:\nАтака и лечение Дерева зависят от яруса:\n   • Земля: 20%  |  Уровень 1: 35%\n   • Уровень 2: 50%  |  Уровень 3 и вершина: 100%!\n\n2. УРОН ОТ ПАДЕНИЯ И ПЕРО:\nПадение >6м наносит 12-38 HP урона. Но носитель Кристалла защищен Падением Перышком (0 урона!) и получает +35% к скорости!\n\n3. 10 СЕКУНД ДО СНЯТИЯ ЩИТА:\nПри гибели 3-го Босса щит держится еще 10 секунд с таймером, давая шанс перегруппироваться!",
            "1. MULTIPLICADOR POR ALTURA:\nAtacar o curar el Árbol escala según la altura:\n   • Suelo: 20%  |  Nivel 1: 35%\n   • Nivel 2: 50%  |  Nivel 3 y Cima: 100%!\n\n2. DAÑO POR CAÍDA Y PLUMA SAGRADA:\nCaer >6m inflige 12-38 daño. ¡El portador del Cristal tiene Caída de Pluma (0 daño!) y +35% velocidad!\n\n3. 10S DE ESCUDO DIVINO:\nAl morir el 3er Jefe, una cuenta de 10s aparece en el escudo antes de romperse por completo.",
            "1. MULTIPLICATEUR DE HAUTEUR :\nL'impact sur l'Arbre varie selon l'élévation :\n   • Sol : 20%  |  Niveau 1 : 35%\n   • Niveau 2 : 50%  |  Niveau 3 et Sommet : 100% !\n\n2. DÉGÂTS DE CHUTE & PLUME SACRÉE :\nUne chute >6m inflige 12-38 PV. Mais le porteur du Cristal a la Chute de Plume (0 dégât !) et +35% de vitesse !\n\n3. COMPTE À REBOURS DE 10S DU BOUCLIER :\nÀ la mort du 3e Boss, un compte de 10s clignote sur le bouclier avant sa destruction totale !"
        );
        GUI.Label(new Rect(c2.x + 16f, c2.y + 46f, c2.width - 32f, c2.height - 56f), proTipsText, CreateBodyStyle(12, Color.white));
    }

    // =========================================================================================
    // EXPLANATORY VIDEO WALKTHROUGH SYSTEM (فيديو توضيحي للعبة)
    // =========================================================================================
    private void DrawInteractiveVideoGuide()
    {
        // Full-screen dim overlay
        GUI.color = new Color(0.02f, 0.03f, 0.02f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(960f, Screen.width - 28f);
        float modalH = Mathf.Min(620f, Screen.height - 28f);
        float mx = (Screen.width - modalW) / 2f;
        float my = (Screen.height - modalH) / 2f;

        Color teamAccent = videoTeamSelection == 0
            ? new Color(0.25f, 0.88f, 1f)
            : new Color(1f, 0.42f, 0.22f);

        EnchantedNatureGUI.DrawLeafPlaque(
            new Rect(mx, my, modalW, modalH),
            new Color(0.06f, 0.09f, 0.08f, 0.99f),
            teamAccent
        );

        // Header Title
        GUIStyle titleStyle = CreateHeadingStyle(19, new Color(1f, 0.88f, 0.35f), TextAnchor.MiddleCenter);
        string vGuideHeader = "🎬 " + LanguageSelectionUI.Localize(
            "TACTICAL VIDEO GUIDE — STEP-BY-STEP MISSION WALKTHROUGH",
            ar: "فيديو الشرح التكتيكي — دليل خطوة بخطوة لكل مرحلة",
            ru: "ТАКТИЧЕСКИЙ ВИДЕОГИД — ПОШАГОВОЕ РУКОВОДСТВО ПО МИССИИ",
            es: "VÍDEO GUÍA TÁCTICA — PASO A PASO DE LA MISIÓN",
            fr: "GUIDE VIDÉO TACTIQUE — PARCOURS DE MISSION ÉTAPE PAR ÉTAPE"
        );
        GUI.Label(new Rect(mx, my + 10f, modalW, 26f), vGuideHeader, titleStyle);

        // Top Switcher Tabs: Protectors Video vs Destroyers Video
        float tabW = (modalW - 44f) / 2f;
        Rect tab1 = new Rect(mx + 18f, my + 40f, tabW, 34f);
        Rect tab2 = new Rect(mx + 26f + tabW, my + 40f, tabW, 34f);

        Color pCol = (videoTeamSelection == 0) ? new Color(0.35f, 0.92f, 1f) : new Color(0.18f, 0.38f, 0.45f);
        Color dCol = (videoTeamSelection == 1) ? new Color(1f, 0.48f, 0.28f) : new Color(0.45f, 0.22f, 0.18f);

        string pTabTitle = LanguageSelectionUI.Localize("🛡️ WATCH PROTECTORS VIDEO GUIDE", "🛡️ شاهد فيديو دليل الحماة", "🛡️ СМОТРЕТЬ ГИД ЗАЩИТНИКОВ", "🛡️ VER GUÍA DE PROTECTORES", "🛡️ VOIR GUIDE PROTECTEURS");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(tab1, pTabTitle, pCol, 12))
        {
            videoTeamSelection = 0;
            videoCurrentTime = 0f;
            isVideoPlaying = true;
        }

        string dTabTitle = LanguageSelectionUI.Localize("🔥 WATCH DESTROYERS VIDEO GUIDE", "🔥 شاهد فيديو دليل المدمرين", "🔥 СМОТРЕТЬ ГИД РАЗРУШИТЕЛЕЙ", "🔥 VER GUÍA DE DESTRUCTORES", "🔥 VOIR GUIDE DESTRUCTEURS");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(tab2, dTabTitle, dCol, 12))
        {
            videoTeamSelection = 1;
            videoCurrentTime = 0f;
            isVideoPlaying = true;
        }

        // Active Phase Calculation
        int activePhase = Mathf.Clamp((int)(videoCurrentTime / VideoPhaseDuration), 0, TotalVideoPhases - 1);
        float phaseTime = Mathf.Repeat(videoCurrentTime, VideoPhaseDuration);
        float phaseProgress = Mathf.Clamp01(phaseTime / VideoPhaseDuration);

        // Cinema Monitor Screen Area
        float screenW = modalW - 36f;
        float screenH = modalH - 200f;
        Rect screenRect = new Rect(mx + 18f, my + 80f, screenW, screenH);

        DrawVideoCanvas(screenRect, videoTeamSelection, activePhase, phaseProgress, videoCurrentTime);

        // Timeline Progress Bar & Scrub Bar
        float timelineY = screenRect.y + screenRect.height + 6f;
        float timelineH = 8f;
        Rect timelineRect = new Rect(mx + 18f, timelineY, screenW, timelineH);

        // Timeline track background
        GUI.color = new Color(0.12f, 0.16f, 0.14f, 0.95f);
        GUI.DrawTexture(timelineRect, Texture2D.whiteTexture);

        // Timeline progress fill
        float totalProgress = Mathf.Clamp01(videoCurrentTime / TotalVideoDuration);
        GUI.color = teamAccent;
        GUI.DrawTexture(new Rect(timelineRect.x, timelineRect.y, timelineRect.width * totalProgress, timelineH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Click to scrub on timeline
        if (Event.current.type == EventType.MouseDown && timelineRect.Contains(Event.current.mousePosition))
        {
            float clickPct = Mathf.Clamp01((Event.current.mousePosition.x - timelineRect.x) / timelineRect.width);
            videoCurrentTime = clickPct * TotalVideoDuration;
            Event.current.Use();
        }

        // Phase Quick-Jump Buttons (4 Phases)
        float phaseBtnY = timelineY + 12f;
        float pBtnW = (screenW - 24f) / 4f;
        string[] protectorPhaseTitles = new string[]
        {
            LanguageSelectionUI.Localize("1. Spawn & Defend", "1. الظهور والدفاع", "1. Возрождение и защита", "1. Aparición y defensa", "1. Apparition & Défense"),
            LanguageSelectionUI.Localize("2. Slay 3 Bosses", "2. صيد الزعماء 3", "2. Убийство 3 Боссов", "2. Vencer a 3 Jefes", "2. Vaincre 3 Boss"),
            LanguageSelectionUI.Localize("3. Grab Crystal", "3. أخذ الكريستالة", "3. Взять Кристалл", "3. Tomar Cristal", "3. Prendre Cristal"),
            LanguageSelectionUI.Localize("4. Altar & Victory!", "4. المذبح والنصر!", "4. Алтарь и победа!", "4. ¡Altar y Victoria!", "4. Autel & Victoire !")
        };
        string[] destroyerPhaseTitles = new string[]
        {
            LanguageSelectionUI.Localize("1. Strike Tree Base", "1. ضرب جذع الشجرة", "1. Атака базы дерева", "1. Atacar base del árbol", "1. Frapper la base"),
            LanguageSelectionUI.Localize("2. Slay 3 Bosses", "2. صيد الزعماء 3", "2. Убийство 3 Боссов", "2. Vencer a 3 Jefes", "2. Vaincre 3 Boss"),
            LanguageSelectionUI.Localize("3. Shield Shatters", "3. تحطم الدرع", "3. Разрушение щита", "3. Escudo roto", "3. Bouclier brisé"),
            LanguageSelectionUI.Localize("4. Fell Tree & Win!", "4. إسقاط الشجرة والفوز!", "4. Падение дерева и победа!", "4. ¡Talar árbol y ganar!", "4. Abattre l'arbre & Gagner !")
        };

        string[] currentTitles = videoTeamSelection == 0 ? protectorPhaseTitles : destroyerPhaseTitles;
        for (int p = 0; p < 4; p++)
        {
            Rect pbRect = new Rect(mx + 18f + p * (pBtnW + 8f), phaseBtnY, pBtnW, 26f);
            bool isActive = (activePhase == p);
            Color pbCol = isActive ? new Color(1f, 0.85f, 0.28f) : new Color(0.24f, 0.35f, 0.30f);
            if (EnchantedNatureGUI.DrawOrganicLeafButton(pbRect, (isActive ? "▶ " : "") + currentTitles[p], pbCol, 10))
            {
                videoCurrentTime = p * VideoPhaseDuration + 0.1f;
                isVideoPlaying = true;
            }
        }

        // Bottom Navigation Bar
        float navY = my + modalH - 46f;

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 18f, navY, 95f, 34f), "⏪ -5s", new Color(0.4f, 0.75f, 0.9f), 11))
        {
            videoCurrentTime = Mathf.Max(0f, videoCurrentTime - 5f);
        }

        string playToggleLabel = isVideoPlaying
            ? LanguageSelectionUI.Localize("⏸ PAUSE", "⏸ إيقاف مؤقت", "⏸ ПАУЗА", "⏸ PAUSA", "⏸ PAUSE")
            : LanguageSelectionUI.Localize("▶ PLAY", "▶ تشغيل", "▶ ПУСК", "▶ REPRODUCIR", "▶ LECTURE");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 121f, navY, 110f, 34f), playToggleLabel, new Color(1f, 0.85f, 0.3f), 12))
        {
            isVideoPlaying = !isVideoPlaying;
        }

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 239f, navY, 95f, 34f), "+5s ⏩", new Color(0.4f, 0.75f, 0.9f), 11))
        {
            videoCurrentTime = Mathf.Min(TotalVideoDuration - 0.5f, videoCurrentTime + 5f);
        }

        string replayLabel = LanguageSelectionUI.Localize("↺ REPLAY", "↺ إعادة التشغيل", "↺ ЗАНОВО", "↺ REPETIR", "↺ REJOUER");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 342f, navY, 100f, 34f), replayLabel, new Color(0.5f, 0.85f, 0.6f), 11))
        {
            videoCurrentTime = 0f;
            isVideoPlaying = true;
        }

        string backTextLabel = LanguageSelectionUI.Localize("📖 BACK TO TEXT GUIDE", "📖 العودة للدليل المكتوب", "📖 К ТЕКСТОВОМУ ГИДУ", "📖 VOLVER A LA GUÍA", "📖 RETOUR AU GUIDE TEXTE");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + modalW - 380f, navY, 180f, 34f), backTextLabel, new Color(0.85f, 0.82f, 0.35f), 11))
        {
            isVideoGuideOpen = false;
        }

        string enterBattleLabel = LanguageSelectionUI.Localize("⚔️ ENTER BATTLE", "⚔️ ادخل المعركة", "⚔️ В БОЙ", "⚔️ ENTRAR A BATALLA", "⚔️ ENTRER AU COMBAT");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + modalW - 190f, navY, 172f, 34f), enterBattleLabel, new Color(0.25f, 0.95f, 0.45f), 12))
        {
            showBriefingModal = false;
            isVideoGuideOpen = false;
        }
    }

    private void DrawVideoCanvas(Rect area, int team, int phase, float phaseProgress, float curTime)
    {
        // Dark cinema screen background
        Color teamAccent = team == 0 ? new Color(0.25f, 0.88f, 1f) : new Color(1f, 0.42f, 0.22f);
        EnchantedNatureGUI.DrawLeafPlaque(area, new Color(0.04f, 0.06f, 0.05f, 0.99f), teamAccent);

        // Top Cinema HUD line
        float topHudY = area.y + 6f;
        float pulse = Mathf.PingPong(Time.time * 3f, 1f);
        GUI.color = new Color(1f, 0.2f, 0.2f, 0.6f + pulse * 0.4f);
        GUI.DrawTexture(new Rect(area.x + 14f, topHudY + 4f, 8f, 8f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        string liveFeedLabel = LanguageSelectionUI.Localize("LIVE TACTICAL FEED", "بث تكتيكي مباشر", "ПРЯМОЙ ТАКТИЧЕСКИЙ ЭФИР", "RETRANSMISIÓN TÁCTICA", "FLUX TACTIQUE EN DIRECT");
        GUI.Label(new Rect(area.x + 28f, topHudY, 260f, 18f), $"{liveFeedLabel} • [{((int)curTime):D2}:{((int)((curTime % 1f) * 100)):D2} / 00:28]", CreateHeadingStyle(10, new Color(0.9f, 0.95f, 0.92f)));

        string roleTag = team == 0
            ? LanguageSelectionUI.Localize("🛡️ PROTECTOR OBJECTIVE", "🛡️ هدف الحماة", "🛡️ ЦЕЛЬ ЗАЩИТНИКОВ", "🛡️ OBJETIVO PROTECTOR", "🛡️ OBJECTIF PROTECTEUR")
            : LanguageSelectionUI.Localize("🔥 DESTROYER OBJECTIVE", "🔥 هدف المدمرين", "🔥 ЦЕЛЬ РАЗРУШИТЕЛЕЙ", "🔥 OBJETIVO DESTRUCTOR", "🔥 OBJECTIF DESTRUCTEUR");
        string phaseWord = LanguageSelectionUI.Localize("PHASE", "المرحلة", "ФАЗА", "FASE", "PHASE");
        GUI.Label(new Rect(area.x + area.width - 270f, topHudY, 255f, 18f), $"{roleTag} ({phaseWord} {phase + 1}/4)", CreateHeadingStyle(10, teamAccent, TextAnchor.MiddleRight));

        // Arena Coordinate Calculations
        float groundY = area.y + area.height - 76f;
        float treeBaseX = area.x + area.width * 0.25f;
        float treeTopY = area.y + 30f;
        float trunkW = 36f;
        float treeHeight = groundY - treeTopY;

        // Draw Forest Ground
        GUI.color = new Color(0.12f, 0.22f, 0.14f, 0.95f);
        GUI.DrawTexture(new Rect(area.x + 8f, groundY, area.width - 16f, 5f), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.55f, 0.28f, 0.85f);
        GUI.DrawTexture(new Rect(area.x + 8f, groundY + 5f, area.width - 16f, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Draw Sacred Tree Trunk
        GUI.color = new Color(0.32f, 0.20f, 0.12f, 0.98f);
        GUI.DrawTexture(new Rect(treeBaseX, treeTopY, trunkW, treeHeight), Texture2D.whiteTexture);
        // Vines/glow
        GUI.color = new Color(0.28f, 0.82f, 0.42f, 0.85f);
        GUI.DrawTexture(new Rect(treeBaseX + 6f, treeTopY + 10f, 5f, treeHeight - 10f), Texture2D.whiteTexture);
        // Canopy
        GUI.color = new Color(0.18f, 0.58f, 0.24f, 0.95f);
        GUI.DrawTexture(new Rect(treeBaseX - 32f, treeTopY - 12f, trunkW + 64f, 22f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Tree Label
        string sacredTreeLabel = LanguageSelectionUI.Localize("🌲 SACRED TREE", "🌲 الشجرة المقدسة", "🌲 СВЯЩЕННОЕ ДЕРЕВО", "🌲 ÁRBOL SAGRADO", "🌲 ARBRE SACRÉ");
        GUI.Label(new Rect(treeBaseX - 40f, treeTopY - 26f, trunkW + 80f, 16f), sacredTreeLabel, CreateHeadingStyle(9, new Color(0.45f, 1f, 0.55f), TextAnchor.MiddleCenter));

        // 3 Platforms
        float plat1Y = groundY - treeHeight * 0.28f;
        float plat2Y = groundY - treeHeight * 0.56f;
        float plat3Y = groundY - treeHeight * 0.82f;

        string lv1Str = LanguageSelectionUI.Localize("LEVEL 1", "المستوى 1", "УРОВЕНЬ 1", "NIVEL 1", "NIVEAU 1");
        string lv2Str = LanguageSelectionUI.Localize("LEVEL 2", "المستوى 2", "УРОВЕНЬ 2", "NIVEL 2", "NIVEAU 2");
        string lv3Str = LanguageSelectionUI.Localize("LEVEL 3", "المستوى 3", "УРОВЕНЬ 3", "NIVEL 3", "NIVEAU 3");

        DrawPlatformDiagram(new Rect(treeBaseX - 68f, plat1Y, 72f, 7f), lv1Str, new Color(0.3f, 0.85f, 0.4f));
        DrawPlatformDiagram(new Rect(treeBaseX + trunkW - 4f, plat2Y, 74f, 7f), lv2Str, new Color(1f, 0.55f, 0.2f));
        DrawPlatformDiagram(new Rect(treeBaseX - 70f, plat3Y, 74f, 7f), lv3Str, new Color(1f, 0.85f, 0.25f));

        // Sacred Altar (Right side on ground)
        float altarX = area.x + area.width * 0.82f;
        Rect altarRect = new Rect(altarX, groundY - 18f, 58f, 18f);
        EnchantedNatureGUI.DrawWoodPlate(altarRect, new Color(0.12f, 0.22f, 0.32f, 0.95f), new Color(0.35f, 0.9f, 1f));
        string altarLabel = LanguageSelectionUI.Localize("🏛️ ALTAR", "🏛️ المذبح", "🏛️ АЛТАРЬ", "🏛️ ALTAR", "🏛️ AUTEL");
        GUI.Label(altarRect, altarLabel, CreateHeadingStyle(9, new Color(0.4f, 0.95f, 1f), TextAnchor.MiddleCenter));

        // Dynamic Elements by Team & Phase
        if (team == 0)
        {
            DrawProtectorSimulation(area, phase, phaseProgress, groundY, treeBaseX, trunkW, treeTopY, plat1Y, plat2Y, plat3Y, altarX);
        }
        else
        {
            DrawDestroyerSimulation(area, phase, phaseProgress, groundY, treeBaseX, trunkW, treeTopY, plat1Y, plat2Y, plat3Y, altarX);
        }

        // Subtitle & Tactical Directives Bar at the bottom of the canvas
        DrawVideoSubtitles(area, team, phase);
    }

    private void DrawPlatformDiagram(Rect r, string label, Color color)
    {
        GUI.color = new Color(0.24f, 0.16f, 0.10f, 0.95f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x, r.y - 14f, r.width, 15f), label, CreateHeadingStyle(8, color, TextAnchor.MiddleCenter));
    }

    private void DrawProtectorSimulation(Rect area, int phase, float phaseProgress, float gy, float tx, float tw, float ty, float p1y, float p2y, float p3y, float ax)
    {
        Color cyan = new Color(0.25f, 0.92f, 1f);
        string protectorTag = LanguageSelectionUI.Localize("PROTECTOR", "حامي", "ЗАЩИТНИК", "PROTECTOR", "PROTECTEUR");
        string heroTag = LanguageSelectionUI.Localize("HERO", "بطل", "ГЕРОЙ", "HÉROE", "HÉROS");
        string carrierTag = LanguageSelectionUI.Localize("CARRIER", "حامل", "НОСИТЕЛЬ", "PORTADOR", "PORTEUR");

        switch (phase)
        {
            case 0: // Phase 1: Defend & Heal Tree
                float px = tx + tw + 18f;
                float py = gy - 24f;
                DrawPlayerToken(px, py, "🛡️", cyan, protectorTag);

                // Rising healing particles
                for (int i = 0; i < 4; i++)
                {
                    float offset = Mathf.Repeat(Time.time * 45f + i * 25f, gy - ty);
                    float hx = tx + 8f + (i % 2 == 0 ? 6f : 16f);
                    float hy = gy - offset;
                    GUI.color = new Color(0.35f, 1f, 0.5f, 0.85f);
                    GUI.DrawTexture(new Rect(hx, hy, 5f, 5f), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;

                string p1TreeBar = LanguageSelectionUI.Localize("TREE: 100% (HEALED!)", "الشجرة: 100% (تم الشفاء!)", "ДЕРЕВО: 100% (ИЗЛЕЧЕНО!)", "ÁRBOL: 100% (¡CURADO!)", "ARBRE : 100% (SOIGNÉ !)");
                DrawSimulatedHealthBar(new Rect(tx - 32f, ty - 42f, tw + 64f, 11f), 1.0f, new Color(0.25f, 0.95f, 0.45f), p1TreeBar);

                string p1CallTitle = LanguageSelectionUI.Localize("🛡️ DEFEND & HEAL SACRED TREE", "🛡️ الدفاع عن الشجرة وعلاجها", "🛡️ ЗАЩИТА И ЛЕЧЕНИЕ ДЕРЕВА", "🛡️ DEFENDER Y CURAR EL ÁRBOL", "🛡️ DÉFENDRE ET SOIGNER L'ARBRE");
                string p1CallBody = LanguageSelectionUI.Localize(
                    "• Your attacks on the Tree HEAL its HP!\n• Protect the base from enemy Destroyers.",
                    "• ضرباتكم على الشجرة تشفي نقاط صحتها!\n• احموا قاعدة الشجرة من غارات المدمرين.",
                    "• Ваши удары по Дереву ЛЕЧАТ его!\n• Защищайте базу от Разрушителей.",
                    "• ¡Tus ataques al Árbol lo CURAN!\n• Protege la base de los Destructores.",
                    "• Vos attaques SOIGNENT l'Arbre !\n• Protégez la base des Destructeurs."
                );
                DrawSimulationCallout(new Rect(area.x + area.width * 0.48f, area.y + 36f, 255f, 48f), p1CallTitle, p1CallBody, cyan);
                break;

            case 1: // Phase 2: Climb & Slay 3 Bosses
                bool b1Dead = phaseProgress > 0.32f;
                bool b2Dead = phaseProgress > 0.65f;
                bool b3Dead = phaseProgress > 0.92f;

                DrawBossToken(tx - 38f, p1y - 18f, "👑 B1", b1Dead, new Color(0.3f, 0.9f, 0.4f));
                DrawBossToken(tx + tw + 28f, p2y - 18f, "👑 B2", b2Dead, new Color(1f, 0.55f, 0.2f));
                DrawBossToken(tx - 40f, p3y - 18f, "👑 B3", b3Dead, new Color(1f, 0.85f, 0.25f));

                float climbX, climbY;
                if (!b1Dead)
                {
                    climbX = tx - 58f; climbY = p1y - 24f;
                }
                else if (!b2Dead)
                {
                    climbX = tx + tw + 10f; climbY = p2y - 24f;
                }
                else
                {
                    climbX = tx - 60f; climbY = p3y - 24f;
                }
                DrawPlayerToken(climbX, climbY, "⚔️", cyan, heroTag);

                // Attack slash animation
                float slashPulse = Mathf.PingPong(Time.time * 8f, 1f);
                GUI.color = new Color(1f, 0.9f, 0.3f, slashPulse);
                GUI.DrawTexture(new Rect(climbX + 16f, climbY + 2f, 14f, 4f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                int defeated = (b1Dead ? 1 : 0) + (b2Dead ? 1 : 0) + (b3Dead ? 1 : 0);
                string p2CallTitle = LanguageSelectionUI.Localize($"👑 SLAY 3 BOSSES ({defeated}/3 SLAIN)", $"👑 صيد الزعماء 3 ({defeated}/3 مهزوم)", $"👑 УБЕЙТЕ 3 БОССОВ ({defeated}/3)", $"👑 VENCE A 3 JEFES ({defeated}/3)", $"👑 VAINCRE 3 BOSS ({defeated}/3)");
                string p2CallBody = LanguageSelectionUI.Localize(
                    "• Climb L1, L2, L3 platforms to eliminate Bosses.\n• Last Hit heals Tree +350 HP & breaks Seals!",
                    "• اصعدوا المنصات 1 و2 و3 للقضاء على الزعماء.\n• الضربة القاضية تشفي الشجرة +350 وتفك الأختام!",
                    "• Поднимитесь на платформы для битвы с Боссами.\n• Добивание лечит Дерево на +350 HP и снимает Печати!",
                    "• Sube a las plataformas para eliminar a los Jefes.\n• ¡El último golpe cura +350 Salud y rompe Sellos!",
                    "• Montez sur les plateformes pour battre les Boss.\n• Le coup de grâce soigne +350 PV et brise les Sceaux !"
                );
                DrawSimulationCallout(new Rect(area.x + area.width * 0.48f, area.y + 36f, 255f, 52f), p2CallTitle, p2CallBody, new Color(1f, 0.85f, 0.25f));
                break;

            case 2: // Phase 3: Crystal Unseals & Pickup
                float crysY = ty - 8f;
                float crysPulse = 1f + Mathf.Sin(Time.time * 6f) * 0.15f;
                Rect crysRect = new Rect(tx + tw / 2f - 11f * crysPulse, crysY, 22f * crysPulse, 22f * crysPulse);
                GUI.color = new Color(0.35f, 0.95f, 1f, 0.95f);
                GUI.DrawTexture(crysRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                string crystalLabel = LanguageSelectionUI.Localize("💎 CRYSTAL", "💎 الكريستال", "💎 КРИСТАЛЛ", "💎 CRISTAL", "💎 CRISTAL");
                GUI.Label(new Rect(crysRect.x - 20f, crysRect.y - 15f, 62f, 15f), crystalLabel, CreateHeadingStyle(9, cyan, TextAnchor.MiddleCenter));

                DrawPlayerToken(tx + tw / 2f - 8f, crysY + 20f, "💎", cyan, carrierTag);

                string p3CallTitle = LanguageSelectionUI.Localize("💎 SACRED CRYSTAL UNSEALED!", "💎 فُكت أختام الكريستالة المقدسة!", "💎 КРИСТАЛЛ РАСПЕЧАТАН!", "💎 ¡CRISTAL SAGRADO DESBLOQUEADO!", "💎 CRISTAL SACRÉ DÉVERROUILLÉ !");
                string p3CallBody = LanguageSelectionUI.Localize(
                    "• All 3 Bosses are dead! The Crystal unlocks!\n• Climb to the top canopy and pick it up.",
                    "• مات الزعماء الثلاثة! أصبحت الكريستالة حرة!\n• اصعدوا إلى القمة والتقطوها.",
                    "• Все 3 Босса мертвы! Кристалл открыт!\n• Поднимитесь на вершину и возьмите его.",
                    "• ¡Los 3 Jefes han muerto! El Cristal está libre.\n• Sube a la cima y recógelo.",
                    "• Les 3 Boss sont morts ! Le Cristal est libre !\n• Montez au sommet pour le récupérer."
                );
                DrawSimulationCallout(new Rect(area.x + area.width * 0.48f, area.y + 36f, 255f, 52f), p3CallTitle, p3CallBody, cyan);
                break;

            case 3: // Phase 4: Sprint to Altar & Shockwave Victory
                float runProgress = Mathf.Clamp01(phaseProgress * 1.25f);
                float pxRun = Mathf.Lerp(tx + tw + 20f, ax - 8f, runProgress);
                DrawPlayerToken(pxRun, gy - 24f, "💎", cyan, carrierTag);

                float swRadius = Mathf.Repeat(Time.time * 60f, 65f);
                GUI.color = new Color(0.3f, 0.85f, 1f, 1f - (swRadius / 65f));
                GUI.DrawTexture(new Rect(pxRun - swRadius / 2f, gy - 18f - swRadius / 4f, swRadius, swRadius / 2f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                if (runProgress >= 0.95f)
                {
                    GUI.color = new Color(1f, 0.88f, 0.35f, 0.75f + Mathf.PingPong(Time.time * 4f, 0.25f));
                    GUI.DrawTexture(new Rect(ax + 18f, ty, 18f, gy - ty), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                string p4CallTitle = runProgress >= 0.95f
                    ? LanguageSelectionUI.Localize("🏆 VICTORY ACHIEVED! 🏆", "🏆 تحقق النصر العظيم! 🏆", "🏆 ПОБЕДА ДОСТИГНУТА! 🏆", "🏆 ¡VICTORIA CONSEGUIDA! 🏆", "🏆 VICTOIRE ÉCLATANTE ! 🏆")
                    : LanguageSelectionUI.Localize("🏃 DELIVER TO SACRED ALTAR", "🏃 الإيصال إلى المذبح المقدس", "🏃 ДОСТАВЬТЕ К АЛТАРЮ", "🏃 LLEVAR AL ALTAR SAGRADO", "🏃 LIVRER À L'AUTEL SACRÉ");

                string p4CallBody = runProgress >= 0.95f
                    ? LanguageSelectionUI.Localize(
                        "• Crystal delivered to Altar! Protectors WIN!\n• Sacred Forest is permanently defended!",
                        "• تم تسليم الكريستالة للمذبح! فاز الحماة!\n• تمت حماية الغابة المقدسة للأبد!",
                        "• Кристалл у Алтаря! Защитники ПОБЕДИЛИ!\n• Лес навсегда защищен!",
                        "• ¡Cristal en el Altar! ¡Protectores GANAN!\n• ¡El Bosque Sagrado está a salvo!",
                        "• Cristal livré à l'Autel ! Les Protecteurs GAGNENT !\n• La Forêt Sacrée est sauvée !"
                    )
                    : LanguageSelectionUI.Localize(
                        "• Carry Crystal to Altar on the ground!\n• Press [E] for Shockwave to blast enemies away!",
                        "• احمل الكريستالة نحو المذبح على الأرض!\n• اضغط [E] لإطلاق موجة صدمة تبعد الأعداء!",
                        "• Несите Кристалл к Алтарю на земле!\n• Нажмите [E] для волны, отбрасывающей врагов!",
                        "• ¡Lleva el Cristal al Altar en el suelo!\n• ¡Pulsa [E] para onda expansiva!",
                        "• Portez le Cristal à l'Autel au sol !\n• Appuyez sur [E] pour onde de choc !"
                    );

                DrawSimulationCallout(new Rect(area.x + area.width * 0.44f, area.y + 36f, 280f, 54f),
                    p4CallTitle, p4CallBody, runProgress >= 0.95f ? new Color(1f, 0.88f, 0.3f) : cyan);
                break;
        }
    }

    private void DrawDestroyerSimulation(Rect area, int phase, float phaseProgress, float gy, float tx, float tw, float ty, float p1y, float p2y, float p3y, float ax)
    {
        Color crimson = new Color(1f, 0.38f, 0.22f);
        string destroyerTag = LanguageSelectionUI.Localize("DESTROYER", "مدمر", "РАЗРУШИТЕЛЬ", "DESTRUCTOR", "DESTRUCTEUR");

        switch (phase)
        {
            case 0: // Phase 1: Attack Tree to 15% Shield
                float px = tx - 26f;
                float py = gy - 24f;
                DrawPlayerToken(px, py, "🔥", crimson, destroyerTag);

                float sparkPulse = Mathf.PingPong(Time.time * 7f, 1f);
                GUI.color = new Color(1f, 0.4f, 0.1f, sparkPulse);
                GUI.DrawTexture(new Rect(tx - 4f, gy - 18f, 8f, 8f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float treePct = Mathf.Lerp(1.0f, 0.15f, Mathf.Clamp01(phaseProgress * 1.5f));
                string shieldedTag = LanguageSelectionUI.Localize("(SHIELDED)", "(محمية بالدرع)", "(ПОД ЩИТОМ)", "(CON ESCUDO)", "(SOUS BOUCLIER)");
                DrawSimulatedHealthBar(new Rect(tx - 32f, ty - 42f, tw + 64f, 11f), treePct, new Color(1f, 0.35f, 0.2f), $"TREE: {(int)(treePct * 100)}% {shieldedTag}");

                float shieldAlpha = Mathf.Clamp01((1.0f - treePct) / 0.85f);
                GUI.color = new Color(1f, 0.85f, 0.25f, 0.22f * shieldAlpha + Mathf.PingPong(Time.time * 2f, 0.12f));
                GUI.DrawTexture(new Rect(tx - 8f, ty - 6f, tw + 16f, gy - ty + 10f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                string d1CallTitle = LanguageSelectionUI.Localize("🔥 STRIKE TREE TO 15% SHIELD", "🔥 ضرب الشجرة حتى درع 15%", "🔥 АТАКА ДЕРЕВА ДО ЩИТА 15%", "🔥 ATACAR ÁRBOL HASTA EL 15%", "🔥 FRAPPER L'ARBRE JUSQU'À 15%");
                string d1CallBody = LanguageSelectionUI.Localize(
                    "• Attack Tree trunk down to 15% HP limit.\n• Arcane Shield protects Tree until 3 Bosses die!",
                    "• هاجموا جذع الشجرة حتى حد 15% صحة.\n• الدرع السحري يحمي الشجرة حتى موت الزعماء 3!",
                    "• Бейте по стволу до предела 15% здоровья.\n• Магический Щит защищает Дерево до гибели 3 Боссов!",
                    "• Ataca el tronco hasta el límite de 15%.\n• ¡El Escudo protege el Árbol hasta que mueran 3 Jefes!",
                    "• Frappez le tronc jusqu'à 15% de PV.\n• Le Bouclier protège l'Arbre tant que les 3 Boss vivent !"
                );
                DrawSimulationCallout(new Rect(area.x + area.width * 0.48f, area.y + 36f, 255f, 52f), d1CallTitle, d1CallBody, crimson);
                break;

            case 1: // Phase 2: Climb & Slay 3 Bosses
                bool b1Dead = phaseProgress > 0.32f;
                bool b2Dead = phaseProgress > 0.65f;
                bool b3Dead = phaseProgress > 0.92f;

                DrawBossToken(tx - 38f, p1y - 18f, "👑 B1", b1Dead, new Color(0.3f, 0.9f, 0.4f));
                DrawBossToken(tx + tw + 28f, p2y - 18f, "👑 B2", b2Dead, new Color(1f, 0.55f, 0.2f));
                DrawBossToken(tx - 40f, p3y - 18f, "👑 B3", b3Dead, new Color(1f, 0.85f, 0.25f));

                float climbX, climbY;
                if (!b1Dead)
                {
                    climbX = tx - 58f; climbY = p1y - 24f;
                }
                else if (!b2Dead)
                {
                    climbX = tx + tw + 10f; climbY = p2y - 24f;
                }
                else
                {
                    climbX = tx - 60f; climbY = p3y - 24f;
                }
                DrawPlayerToken(climbX, climbY, "⚔️", crimson, destroyerTag);

                int defeated = (b1Dead ? 1 : 0) + (b2Dead ? 1 : 0) + (b3Dead ? 1 : 0);
                string d2CallTitle = LanguageSelectionUI.Localize($"👑 SLAY ALL 3 BOSSES ({defeated}/3)", $"👑 صيد الزعماء الثلاثة ({defeated}/3)", $"👑 УБЕЙТЕ ВСЕХ 3 БОССОВ ({defeated}/3)", $"👑 VENCE A LOS 3 JEFES ({defeated}/3)", $"👑 VAINCRE LES 3 BOSS ({defeated}/3)");
                string d2CallBody = LanguageSelectionUI.Localize(
                    "• Must kill all 3 Bosses to break the Tree Shield!\n• Last Hit bonus deals -260 Damage to Tree!",
                    "• يجب قتل الزعماء الثلاثة لكسر درع الشجرة!\n• الضربة القاضية تلحق -260 ضرر إضافي بالشجرة!",
                    "• Нужно убить 3 Боссов, чтобы разбить Щит!\n• Добивание наносит -260 урона Дереву!",
                    "• ¡Mata a los 3 Jefes para romper el Escudo!\n• ¡El último golpe inflige -260 daño al Árbol!",
                    "• Tuez les 3 Boss pour briser le Bouclier !\n• Le coup de grâce inflige -260 dégâts à l'Arbre !"
                );
                DrawSimulationCallout(new Rect(area.x + area.width * 0.48f, area.y + 36f, 255f, 52f), d2CallTitle, d2CallBody, new Color(1f, 0.82f, 0.25f));
                break;

            case 2: // Phase 3: Arcane Shield Shatters
                float shatterProgress = phaseProgress;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (Mathf.PI * 2f / 8f);
                    float dist = Mathf.Lerp(8f, 55f, shatterProgress);
                    float sx = (tx + tw / 2f) + Mathf.Cos(angle) * dist;
                    float sy = (ty + (gy - ty) / 2f) + Mathf.Sin(angle) * dist;
                    GUI.color = new Color(1f, 0.85f, 0.3f, 1f - shatterProgress);
                    GUI.DrawTexture(new Rect(sx, sy, 7f, 7f), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;

                string shatteredLabel = LanguageSelectionUI.Localize("TREE: 15% (SHIELD SHATTERED!)", "الشجرة: 15% (تحطم الدرع!)", "ДЕРЕВО: 15% (ЩИТ РАЗБИТ!)", "ÁRBOL: 15% (¡ESCUDO ROTO!)", "ARBRE : 15% (BOUCLIER BRISÉ !)");
                DrawSimulatedHealthBar(new Rect(tx - 32f, ty - 42f, tw + 64f, 11f), 0.15f, crimson, shatteredLabel);

                string d3CallTitle = LanguageSelectionUI.Localize("⚡ ARCANE SHIELD SHATTERED!", "⚡ تحطم الدرع السحري!", "⚡ МАГИЧЕСКИЙ ЩИТ РАЗБИТ!", "⚡ ¡ESCUDO ARCANO ROTO!", "⚡ BOUCLIER ARCANIQUE BRISÉ !");
                string d3CallBody = LanguageSelectionUI.Localize(
                    "• Boss 3 is dead! 'YOU CAN NOW DESTROY TREE!'\n• The Sacred Tree is now 100% vulnerable!",
                    "• مات الزعيم 3! 'يمكنكم الآن تدمير الشجرة!'\n• الشجرة المقدسة الآن معرضة للهجوم 100%!",
                    "• Босс 3 мертв! 'ТЕПЕРЬ МОЖНО СРУБИТЬ ДЕРЕВО!'\n• Священное Дерево уязвимо на 100%!",
                    "• ¡Murió el Jefe 3! '¡YA PUEDES DESTRUIR EL ÁRBOL!'\n• ¡El Árbol Sagrado es 100% vulnerable!",
                    "• Boss 3 vaincu ! 'DÉTRUISEZ L'ARBRE MAINTENANT !'\n• L'Arbre Sacré est 100% vulnérable !"
                );
                DrawSimulationCallout(new Rect(area.x + area.width * 0.44f, area.y + 36f, 275f, 52f), d3CallTitle, d3CallBody, new Color(1f, 0.88f, 0.3f));
                break;

            case 3: // Phase 4: Full Tree Siege & Collapse
                float attackTreePct = Mathf.Lerp(0.15f, 0.0f, Mathf.Clamp01(phaseProgress * 1.3f));
                string treeHpLabel = LanguageSelectionUI.Localize($"TREE: {(int)(attackTreePct * 100)}% HP", $"الشجرة: {(int)(attackTreePct * 100)}% صحة", $"ДЕРЕВО: {(int)(attackTreePct * 100)}% HP", $"ÁRBOL: {(int)(attackTreePct * 100)}% SALUD", $"ARBRE : {(int)(attackTreePct * 100)}% PV");
                DrawSimulatedHealthBar(new Rect(tx - 32f, ty - 42f, tw + 64f, 11f), attackTreePct, crimson, treeHpLabel);

                DrawPlayerToken(tx + tw + 10f, p2y - 24f, "🔥", crimson, destroyerTag);

                string dmg300Label = LanguageSelectionUI.Localize("⚡ 300% DMG!", "⚡ 300% ضرر!", "⚡ 300% УРОНА!", "⚡ ¡300% DAÑO!", "⚡ 300% DÉGÂTS !");
                GUI.Label(new Rect(tx - 8f, p2y - 15f, 110f, 16f), dmg300Label, CreateHeadingStyle(9, new Color(1f, 0.9f, 0.3f)));

                bool treeCollapsed = attackTreePct <= 0.01f;
                string d4CallTitle = treeCollapsed
                    ? LanguageSelectionUI.Localize("🔥 DESTROYER VICTORY! 🔥", "🔥 نصر المدمرين الساحق! 🔥", "🔥 ПОБЕДА РАЗРУШИТЕЛЕЙ! 🔥", "🔥 ¡VICTORIA DESTRUCTORES! 🔥", "🔥 VICTOIRE DESTRUCTEURS ! 🔥")
                    : LanguageSelectionUI.Localize("💥 FULL TREE SIEGE", "💥 حصار الشجرة الشامل", "💥 ШТУРМ ДЕРЕВА", "💥 ASEDIO AL ÁRBOL", "💥 SIÈGE DE L'ARBRE");

                string d4CallBody = treeCollapsed
                    ? LanguageSelectionUI.Localize(
                        "• Tree HP reached 0! Sacred Tree has toppled!\n• Destroyers conquer the forest and WIN!",
                        "• وصلت صحة الشجرة إلى 0! سقطت الشجرة المقدسة!\n• المدمرون استولوا على الغابة وفازوا بالمباراة!",
                        "• Здоровье Дерева на 0! Дерево рухнуло!\n• Разрушители захватили лес и ПОБЕДИЛИ!",
                        "• ¡La salud llegó a 0! ¡El Árbol Sagrado ha caído!\n• ¡Los Destructores conquistan y GANAN!",
                        "• PV de l'Arbre à 0 ! Le Haricot Sacré s'est effondré !\n• Les Destructeurs conquièrent et GAGNENT !"
                    )
                    : LanguageSelectionUI.Localize(
                        "• Attack Tree from higher platforms for 300% dmg!\n• Bring Tree HP to 0% to fell the Beanstalk and WIN!",
                        "• هاجموا الشجرة من المنصات العالية لضرر 300%!\n• خفضوا صحة الشجرة إلى 0% لإسقاطها والفوز!",
                        "• Бейте Дерево с верхних платформ (300% урона)!\n• Снизьте здоровье до 0% для победы!",
                        "• ¡Ataca el Árbol desde lo alto para 300% daño!\n• ¡Lleva su salud al 0% para derribarlo y GANAR!",
                        "• Attaquez depuis les hauteurs pour 300% dégâts !\n• Réduisez les PV à 0% pour abattre l'Arbre et GAGNER !"
                    );

                DrawSimulationCallout(new Rect(area.x + area.width * 0.44f, area.y + 36f, 280f, 54f),
                    d4CallTitle, d4CallBody, treeCollapsed ? new Color(1f, 0.4f, 0.2f) : new Color(1f, 0.85f, 0.3f));
                break;
        }
    }

    private void DrawVideoSubtitles(Rect area, int team, int phase)
    {
        float subH = 64f;
        Rect subRect = new Rect(area.x + 8f, area.y + area.height - subH - 6f, area.width - 16f, subH);
        Color teamCol = team == 0 ? new Color(0.25f, 0.92f, 1f) : new Color(1f, 0.45f, 0.25f);
        EnchantedNatureGUI.DrawWoodPlate(subRect, new Color(0.04f, 0.08f, 0.06f, 0.96f), teamCol);

        string enHead, enBody, enTip;
        if (team == 0) // Protectors
        {
            switch (phase)
            {
                case 0:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 1: SPAWN IN FOREST & DEFEND SACRED TREE",
                        "المرحلة 1: الظهور في الغابة والدفاع عن الشجرة المقدسة",
                        "ФАЗА 1: ПОЯВЛЕНИЕ В ЛЕСУ И ЗАЩИТА ДЕРЕВА",
                        "FASE 1: APARICIÓN Y DEFENSA DEL ÁRBOL SAGRADO",
                        "PHASE 1 : APPARITION ET DÉFENSE DE L'ARBRE SACRÉ"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Spawn at the base. Guard the Sacred Beanstalk trunk. Your attacks HEAL the Tree instead of damaging it!",
                        "تظهرون عند القاعدة. احموا جذع نبتة الفاصولياء. ضرباتكم تشفي الشجرة بدلاً من إيذائها!",
                        "Появитесь у подножия. Защищайте ствол. Ваши удары ЛЕЧАТ Дерево, а не наносят урон!",
                        "Aparece en la base. Custodia el tronco. ¡Tus ataques CURAN el Árbol en vez de dañarlo!",
                        "Apparaissez à la base. Gardez le tronc. Vos attaques SOIGNENT l'Arbre au lieu de l'abîmer !"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Stay near the Tree base to counter early Destroyer attacks while your team gathers.",
                        "💡 نصيحة تكتيكية: ابقوا قرب قاعدة الشجرة لصد هجمات المدمرين المبكرة ريثما يجتمع فريقكم.",
                        "💡 СОВЕТ: Держитесь у подножия, чтобы отбивать ранние атаки Разрушителей, пока собирается команда.",
                        "💡 CONSEJO TÁCTICO: Permanece cerca de la base para frenar ataques tempranos mientras se agrupa el equipo.",
                        "💡 CONSEIL TACTIQUE : Restez près de la base pour contrer les premières attaques le temps que l'équipe s'unisse."
                    );
                    break;

                case 1:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 2: CLIMB PLATFORMS & SLAY THE 3 GUARDIAN BOSSES",
                        "المرحلة 2: صعود المنصات والقضاء على الزعماء الحراس الثلاثة",
                        "ФАЗА 2: ПОДЪЕМ НА ПЛАТФОРМЫ И УБИЙСТВО 3 БОССОВ",
                        "FASE 2: SUBIR PLATAFORMAS Y ELIMINAR A LOS 3 JEFES",
                        "PHASE 2 : MONTER SUR LES PLATEFORMES ET ÉLIMINER LES 3 BOSS"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Climb to Levels 1, 2, and 3. Slay Forest Goliath, Inferno Titan, and Ancient Overlord to break the 3 Seals!",
                        "اصعدوا إلى المستويات 1 و2 و3. اقضوا على جالوت وعملاق الجحيم والحاكم القديم لفك الأختام الثلاثة!",
                        "Поднимитесь на Уровни 1, 2 и 3. Победите Голиафа, Титана и Владыку, чтобы снять 3 Печати!",
                        "Sube a los Niveles 1, 2 y 3. ¡Vence a Goliat, al Titán y al Soberano para romper los 3 Sellos!",
                        "Montez aux Niveaux 1, 2 et 3. Battez Goliath, le Titan et le Seigneur pour briser les 3 Sceaux !"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Landing the last hit on any Boss heals the Sacred Tree by +350 HP and boosts team XP!",
                        "💡 نصيحة تكتيكية: الضربة القاضية على أي زعيم تشفي الشجرة +350 صحة وتمنح نقاط خبرة للفريق!",
                        "💡 СОВЕТ: Добивающий удар по любому Боссу лечит Дерево на +350 HP и дает опыт всей команде!",
                        "💡 CONSEJO TÁCTICO: ¡Asestar el último golpe a un Jefe cura +350 Salud al Árbol y da experiencia al equipo!",
                        "💡 CONSEIL TACTIQUE : Le coup de grâce sur un Boss soigne l'Arbre de +350 PV et booste l'XP de l'équipe !"
                    );
                    break;

                case 2:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 3: 'THE CRYSTAL IS READY!' — ASCEND & GRAB CRYSTAL",
                        "المرحلة 3: 'الكريستالة جاهزة!' — الصعود وأخذ الكريستالة",
                        "ФАЗА 3: 'КРИСТАЛЛ ГОТОВ!' — ПОДНИМИТЕСЬ И ВОЗЬМИТЕ ЕГО",
                        "FASE 3: '¡EL CRISTAL ESTÁ LISTO!' — SUBIR Y RECOGERLO",
                        "PHASE 3 : 'LE CRISTAL EST PRÊT !' — MONTER ET RÉCUPÉRER"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "When Boss 3 falls, the Sacred Crystal unseals at the top canopy! Ascend to the peak and collect it.",
                        "عند سقوط الزعيم 3، تتحرر الكريستالة في القمة! اصعدوا للأعلى والتقطوها.",
                        "Когда падет Босс 3, Кристалл освободится на самой вершине! Поднимитесь и заберите его.",
                        "¡Al caer el 3er Jefe, el Cristal se desbloquea en la cima! Sube a lo más alto y recógelo.",
                        "À la mort du Boss 3, le Cristal se libère à la cime ! Montez au sommet pour le récupérer."
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Once picked up, the carrier glows brightly. Escort the carrier to protect them from ambush!",
                        "💡 نصيحة تكتيكية: يتوهج حامل الكريستالة بقوة. رافقوا الحامل واحموه من كمائن الأعداء!",
                        "💡 СОВЕТ: Носитель ярко светится. Сопровождайте его, чтобы защитить от вражеских засад!",
                        "💡 CONSEJO TÁCTICO: El portador brilla con fuerza. ¡Acompáñalo para defenderlo de emboscadas enemigas!",
                        "💡 CONSEIL TACTIQUE : Le porteur brille intensément. Escortez-le pour le protéger des embuscades !"
                    );
                    break;

                default:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 4: DELIVER CRYSTAL TO ALTAR & USE SHOCKWAVE FOR VICTORY",
                        "المرحلة 4: إيصال الكريستالة للمذبح واستخدام موجة الصدمة للفوز",
                        "ФАЗА 4: ДОСТАВКА КРИСТАЛЛА К АЛТАРЮ И УДАРНАЯ ВОЛНА",
                        "FASE 4: LLEVAR EL CRISTAL AL ALTAR Y USAR ONDA EXPANSIVA",
                        "PHASE 4 : LIVRER LE CRISTAL À L'AUTEL ET ONDE DE CHOC"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Carry Crystal down to the Sacred Altar on the ground. Press [E] for Shockwave to clear enemies. Touch Altar to WIN!",
                        "احملوا الكريستالة للمذبح على الأرض. اضغطوا [E] لإطلاق موجة صدمة تبعد الأعداء. المسوا المذبح للفوز!",
                        "Отнесите Кристалл к Алтарю на земле. Нажмите [E] для волны, разбрасывающей врагов. Коснитесь Алтаря для победы!",
                        "Lleva el Cristal al Altar en el suelo. Pulsa [E] para onda expansiva y apartar enemigos. ¡Toca el Altar para GANAR!",
                        "Portez le Cristal à l'Autel au sol. Appuyez sur [E] pour onde de choc et repousser les ennemis. Touchez l'Autel pour GAGNER !"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Press [E] whenever surrounded to trigger a radiant blast that knocks back all nearby enemies!",
                        "💡 نصيحة تكتيكية: اضغطوا [E] كلما حاصركم الأعداء لإطلاق انفجار إشعاعي يدفع كل من حولكم بعيداً!",
                        "💡 СОВЕТ: Нажимайте [E] в окружении, чтобы вызвать мощный импульс, сбивающий всех врагов с ног!",
                        "💡 CONSEJO TÁCTICO: ¡Pulsa [E] si te rodean para generar una explosión radiante que repele a los enemigos!",
                        "💡 CONSEIL TACTIQUE : Appuyez sur [E] si vous êtes encerclé pour déclencher une onde radiante qui repousse !"
                    );
                    break;
            }
        }
        else // Destroyers
        {
            switch (phase)
            {
                case 0:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 1: ATTACK THE SACRED TREE TO 15% SHIELD THRESHOLD",
                        "المرحلة 1: مهاجمة الشجرة المقدسة حتى حد درع 15%",
                        "ФАЗА 1: АТАКА ДЕРЕВА ДО ПОРОГА ЩИТА 15%",
                        "FASE 1: ATACAR EL ÁRBOL SAGRADO HASTA EL 15%",
                        "PHASE 1 : ATTAQUER L'ARBRE SACRÉ JUSQU'À 15%"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Assault the Sacred Tree trunk to lower its health to 15%. The Arcane Shield then locks the Tree until Bosses die.",
                        "هاجموا جذع الشجرة المقدسة لخفض صحتها إلى 15%. سيقفل الدرع السحري الشجرة بعدها حتى موت الزعماء.",
                        "Штурмуйте ствол, чтобы снизить здоровье до 15%. Затем Щит заблокирует урон до гибели Боссов.",
                        "Asalta el tronco para bajar su salud al 15%. El Escudo Arcano bloqueará el Árbol hasta que mueran los Jefes.",
                        "Assaillez le tronc pour baisser sa vie à 15%. Le Bouclier Arcanique bloquera l'Arbre jusqu'à la mort des Boss."
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Focus fire on the trunk early on, but do not waste attacks once the Arcane Shield activates.",
                        "💡 نصيحة تكتيكية: ركزوا هجماتكم على الجذع مبكراً، لكن لا تضيعوا ضرباتكم بعد تفعيل الدرع السحري.",
                        "💡 СОВЕТ: Сфокусируйте огонь на стволе вначале, но не тратьте удары впустую после активации Щита.",
                        "💡 CONSEJO TÁCTICO: Concéntrate en el tronco al inicio, pero no gastes ataques una vez activo el Escudo.",
                        "💡 CONSEIL TACTIQUE : Concentrez vos tirs sur le tronc au début, mais ne gaspillez pas d'attaques sous bouclier."
                    );
                    break;

                case 1:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 2: CLIMB PLATFORMS & HUNT DOWN ALL 3 BOSSES",
                        "المرحلة 2: صعود المنصات ومطاردة الزعماء الثلاثة والقضاء عليهم",
                        "ФАЗА 2: ПОДЪЕМ НА ПЛАТФОРМЫ И ОХОТА НА 3 БОССОВ",
                        "FASE 2: SUBIR A LAS PLATAFORMAS Y CAZAR A LOS 3 JEFES",
                        "PHASE 2 : MONTER SUR LES PLATEFORMES ET CHASSER LES 3 BOSS"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "The Tree is shielded! Climb Levels 1, 2, and 3 to eliminate the 3 Bosses. Each kill gives -260 Tree damage!",
                        "الشجرة محمية بالدرع! اصعدوا المستويات 1 و2 و3 للقضاء على الزعماء. كل زعيم مقتول يلحق -260 ضرر بالشجرة!",
                        "Дерево под Щитом! Поднимитесь на Уровни 1, 2 и 3 для ликвидации 3 Боссов. Каждое убийство дает -260 урона Дереву!",
                        "¡El Árbol tiene escudo! Sube a los Niveles 1, 2 y 3 para abatir a los 3 Jefes. ¡Cada baja inflige -260 daño al Árbol!",
                        "L'Arbre est protégé ! Montez aux Niveaux 1, 2 et 3 pour vaincre les 3 Boss. Chaque élimination inflige -260 dégâts !"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Each Boss slain by Destroyers unleashes a shockwave that inflicts -260 damage to the Tree!",
                        "💡 نصيحة تكتيكية: كل زعيم يقضي عليه المدمرون يطلق موجة ارتجاج تلحق -260 ضرر مباشر بالشجرة!",
                        "💡 СОВЕТ: Каждый Босс, убитый Разрушителями, вызывает ударную волну, наносящую -260 урона Дереву!",
                        "💡 CONSEJO TÁCTICO: ¡Cada Jefe abatido por Destructores desata una onda que daña al Árbol en -260 de Salud!",
                        "💡 CONSEIL TACTIQUE : Chaque Boss abattu par les Destructeurs libère une onde qui inflige -260 dégâts à l'Arbre !"
                    );
                    break;

                case 2:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 3: 'YOU CAN NOW DESTROY THE TREE!' — SHIELD SHATTERS",
                        "المرحلة 3: 'يمكنكم الآن تدمير الشجرة!' — تحطم الدرع",
                        "ФАЗА 3: 'ТЕПЕРЬ МОЖНО СРУБИТЬ ДЕРЕВО!' — ЩИТ РАЗБИТ",
                        "FASE 3: '¡YA PUEDES DESTRUIR EL ÁRBOL!' — EL ESCUDO SE ROMPE",
                        "PHASE 3 : 'DÉTRUISEZ L'ARBRE MAINTENANT !' — BOUCLIER BRISÉ"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Boss 3 falls! A giant announcement confirms the Arcane Shield is shattered. The Tree is now 100% vulnerable!",
                        "سقط الزعيم 3! إعلان ضخم يؤكد تحطم الدرع السحري. الشجرة المقدسة الآن عرضة للدمار بنسبة 100%!",
                        "Босс 3 повержен! Сообщение подтверждает падение Щита. Дерево теперь уязвимо на 100%!",
                        "¡Cae el 3er Jefe! Un anuncio gigante confirma la rotura del Escudo. ¡El Árbol es ahora 100% vulnerable!",
                        "Le Boss 3 tombe ! Une annonce géante confirme que le Bouclier est brisé. L'Arbre est 100% vulnérable !"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Intercept any Protectors trying to reach the Crystal while the rest of your team storms the Tree!",
                        "💡 نصيحة تكتيكية: اعترضوا أي حامٍ يحاول الوصول للكريستالة بينما يقتحم بقية فريقكم جذع الشجرة!",
                        "💡 СОВЕТ: Перехватывайте Защитников, идущих к Кристаллу, пока остальная команда штурмует Дерево!",
                        "💡 CONSEJO TÁCTICO: ¡Intercepta a los Protectores que busquen el Cristal mientras tu equipo asalta el Árbol!",
                        "💡 CONSEIL TACTIQUE : Interceptez les Protecteurs visant le Cristal pendant que le reste de l'équipe charge !"
                    );
                    break;

                default:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 4: FULL TREE SIEGE & TOPPLE THE BEANSTALK FOR VICTORY",
                        "المرحلة 4: الحصار الشامل وإسقاط نبتة الفاصولياء لتحقيق النصر",
                        "ФАЗА 4: ПОЛНЫЙ ШТУРМ И ПАДЕНИЕ ДЕРЕВА ДЛЯ ПОБЕДЫ",
                        "FASE 4: ASEDIO TOTAL Y DERRIBO DEL TALLO PARA GANAR",
                        "PHASE 4 : SIÈGE TOTAL ET CHUTE DU HARICOT POUR LA VICTOIRE"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Attack Tree trunk (higher platforms grant full 100% damage, while ground deals 20%)! Reduce Tree HP to 0 to WIN!",
                        "هاجموا جذع الشجرة (المنصات العالية تمنح حتى 300% ضرر مضاعف)! اخفضوا صحة الشجرة إلى 0 لإسقاطها والفوز!",
                        "Бейте по стволу (верхние платформы дают до 300% урона)! Снизьте здоровье до 0, чтобы свалить Дерево и ПОБЕДИТЬ!",
                        "¡Ataca el tronco (plataformas altas dan hasta 300% daño)! ¡Baja su salud a 0 para derribarlo y GANAR!",
                        "Attaquez le tronc (les plateformes hautes donnent jusqu'à 300% dégâts) ! Baissez les PV à 0 pour GAGNER !"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Climb to higher platforms to deal full 100% height impact directly to the trunk (ground is reduced to 20%)!",
                        "💡 نصيحة تكتيكية: اصعدوا لمنصات المستوى 2 أو 3 لتوجيه ضرر مضاعف بنسبة 300% بفضل ميزة الارتفاع مباشرة إلى الجذع!",
                        "💡 СОВЕТ: Поднимитесь на платформы 2 или 3 уровня, чтобы наносить до 300% урона по стволу благодаря множителю высоты!",
                        "💡 CONSEJO TÁCTICO: ¡Sube a plataformas de Nivel 2 o 3 para infligir hasta 300% de daño por multiplicador de altura!",
                        "💡 CONSEIL TACTIQUE : Montez aux Niveaux 2 ou 3 pour infliger jusqu'à 300% de dégâts de hauteur directement au tronc !"
                    );
                    break;
            }
        }

        GUI.Label(new Rect(subRect.x + 10f, subRect.y + 4f, subRect.width - 20f, 18f), enHead, CreateHeadingStyle(10, teamCol));
        GUI.Label(new Rect(subRect.x + 10f, subRect.y + 22f, subRect.width - 20f, 18f), enBody, CreateBodyStyle(10, Color.white));
        GUI.Label(new Rect(subRect.x + 10f, subRect.y + 40f, subRect.width - 20f, 18f), enTip, CreateBodyStyle(10, new Color(1f, 0.92f, 0.45f)));
    }

    private void DrawPlayerToken(float x, float y, string icon, Color color, string tag)
    {
        Rect r = new Rect(x, y, 20f, 20f);
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(r, icon, CreateHeadingStyle(11, Color.black, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(x - 24f, y - 13f, 68f, 13f), tag, CreateHeadingStyle(8, color, TextAnchor.MiddleCenter));
    }

    private void DrawBossToken(float x, float y, string tag, bool isDead, Color color)
    {
        Rect r = new Rect(x, y, 22f, 18f);
        if (isDead)
        {
            GUI.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(r, "💀", CreateHeadingStyle(10, Color.white, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(x - 14f, y - 12f, 50f, 12f), "SLAIN", CreateHeadingStyle(8, Color.gray, TextAnchor.MiddleCenter));
        }
        else
        {
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(r, "👑", CreateHeadingStyle(10, Color.black, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(x - 14f, y - 12f, 50f, 12f), tag, CreateHeadingStyle(8, color, TextAnchor.MiddleCenter));
        }
    }

    private void DrawSimulatedHealthBar(Rect r, float pct, Color fillCol, string label)
    {
        GUI.color = new Color(0.1f, 0.12f, 0.1f, 0.9f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = fillCol;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(pct), r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x, r.y - 13f, r.width, 13f), label, CreateHeadingStyle(8, fillCol, TextAnchor.MiddleCenter));
    }

    private void DrawSimulationCallout(Rect r, string title, string body, Color accent)
    {
        EnchantedNatureGUI.DrawLeafPlaque(r, new Color(0.06f, 0.10f, 0.08f, 0.95f), accent);
        GUI.Label(new Rect(r.x + 8f, r.y + 5f, r.width - 16f, 17f), title, CreateHeadingStyle(10, accent));
        GUI.Label(new Rect(r.x + 8f, r.y + 22f, r.width - 16f, r.height - 25f), body, CreateBodyStyle(9, Color.white));
    }

    private GUIStyle CreateHeadingStyle(int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = size;
        s.fontStyle = FontStyle.Bold;
        s.alignment = alignment;
        s.normal.textColor = color;
        return s;
    }

    private GUIStyle CreateBodyStyle(int size, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = size;
        s.fontStyle = FontStyle.Normal;
        s.wordWrap = true;
        s.alignment = alignment;
        s.normal.textColor = color;
        return s;
    }
}
