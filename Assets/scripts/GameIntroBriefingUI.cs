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
            fa: $"👑 غول شکست خورد ({defeatedCount} / {LevelBossManager.TotalBossCount} مهر شکسته شد) 👑",
            de: $"👑 BOSS BESIEGT ({defeatedCount} / {LevelBossManager.TotalBossCount} SIEGEL GEBROCHEN) 👑",
            it: $"👑 BOSS SCONFITTO ({defeatedCount} / {LevelBossManager.TotalBossCount} SIGILLI SPEZZATI) 👑",
            tr: $"👑 PATRON YENİLDİ ({defeatedCount} / {LevelBossManager.TotalBossCount} MÜHÜR KIRILDI) 👑",
            ru: $"👑 БОСС ПОВЕРЖЕН ({defeatedCount} / {LevelBossManager.TotalBossCount} ПЕЧАТЕЙ СНЯТО) 👑",
            fr: $"👑 BOSS VAINCU ({defeatedCount} / {LevelBossManager.TotalBossCount} SCEAUX BRISÉS) 👑",
            es: $"👑 ¡JEFE DERROTADO ({defeatedCount} / {LevelBossManager.TotalBossCount} SELLOS ROTOS)! 👑",
            hi: $"👑 बॉस पराजित ({defeatedCount} / {LevelBossManager.TotalBossCount} सील टूटी) 👑",
            zh: $"👑 守护领主已被击败（{defeatedCount} / {LevelBossManager.TotalBossCount} 封印解除）👑",
            ja: $"👑 ボス討伐成功！（封印解除: {defeatedCount} / {LevelBossManager.TotalBossCount}）👑",
            pt: $"👑 CHEFE DERROTADO ({defeatedCount} / {LevelBossManager.TotalBossCount} SELOS QUEBRADOS) 👑"
        );

        bannerMainText = LanguageSelectionUI.Localize(
            $"{bossTitle} WAS SLAIN BY {killerName.ToUpper()}!",
            ar: $"تمت هزيمة {bossTitle} بواسطة {killerName.ToUpper()}!",
            fa: $"{bossTitle} توسط {killerName.ToUpper()} نابود شد!",
            de: $"{bossTitle} WURDE VON {killerName.ToUpper()} BESIEGT!",
            it: $"{bossTitle} È STATO SCONFITTO DA {killerName.ToUpper()}!",
            tr: $"{bossTitle}, {killerName.ToUpper()} TARAFINDAN KATLEDİLDİ!",
            ru: $"{bossTitle} ПОВЕРЖЕН ИГРОКОМ {killerName.ToUpper()}!",
            fr: $"{bossTitle} A ÉTÉ VAINCU PAR {killerName.ToUpper()} !",
            es: $"¡{bossTitle} DERROTADO POR {killerName.ToUpper()}!",
            hi: $"{bossTitle} को {killerName.ToUpper()} द्वारा मार गिराया गया!",
            zh: $"{bossTitle} 已被 {killerName.ToUpper()} 斩杀！",
            ja: $"{bossTitle} は {killerName.ToUpper()} に討伐された！",
            pt: $"{bossTitle} FOI DERROTADO POR {killerName.ToUpper()}!"
        );

        int remaining = LevelBossManager.TotalBossCount - defeatedCount;
        bannerSubText = killerTeam == PlayerMovement.TeamType.Protector
            ? LanguageSelectionUI.Localize(
                $"PROTECTORS BLESSING: Sacred Tree Healed +350 HP! ({remaining} Boss{(remaining > 1 ? "es" : "")} Remaining)",
                ar: $"بركة الحماة: تم شفاء الشجرة +350 نقطة! (متبقي {remaining} زعيم)",
                fa: $"برکت محافظان: درخت مقدس +350 واحد درمان شد! ({remaining} غول باقی‌مانده)",
                de: $"SEGEN DER BESCHÜTZER: Heiliger Baum +350 HP geheilt! ({remaining} verbleibend)",
                it: $"BENEDIZIONE DEI PROTETTORI: Albero Sacro guarito +350 HP! ({remaining} rimasti)",
                tr: $"KORUYUCU LÜTFU: Kutsal Ağaç +350 Can İyileşti! ({remaining} Patron kaldı)",
                ru: $"БЛАГОСЛОВЕНИЕ: Дерево исцелено на +350 HP! (Осталось {remaining} босс)",
                fr: $"BÉNÉDICTION : L'Arbre Sacré a récupéré +350 PV ! ({remaining} restants)",
                es: $"BENDICIÓN PROTECTORA: ¡Árbol curado +350 HP! (Quedan {remaining} jefes)",
                hi: $"रक्षकों का आशीर्वाद: पवित्र वृक्ष को +350 स्वास्थ्य मिला! ({remaining} बॉस शेष)",
                zh: $"守护者神恩降临：神圣巨树生命恢复+350！（还剩{remaining}个守护领主）",
                ja: $"守護者の恩寵：神聖な巨木が+350回復！（残り{remaining}体のボス）",
                pt: $"BÊNÇÃO DOS PROTETORES: Árvore Sagrada curada em +350 PV! ({remaining} restantes)"
            )
            : LanguageSelectionUI.Localize(
                $"DESTROYERS SHOCKWAVE: Sacred Tree Damaged -260 HP! ({remaining} Boss{(remaining > 1 ? "es" : "")} Remaining)",
                ar: $"ضربة المدمرين: تضررت الشجرة -260 نقطة! (متبقي {remaining} زعيم)",
                fa: $"موج تخریب نابودکنندگان: درخت مقدس -260 واحد آسیب دید! ({remaining} غول باقی‌مانده)",
                de: $"SCHOCKWELLE DER ZERSTÖRER: Heiliger Baum erlitt -260 Schaden! ({remaining} verbleibend)",
                it: $"ONDA D'URTO DEI DISTRUTTORI: Albero Sacro danneggiato -260 HP! ({remaining} rimasti)",
                tr: $"YOK EDİCİ ŞOKU: Kutsal Ağaç -260 Hasar Aldı! ({remaining} Patron kaldı)",
                ru: $"УДАРНАЯ ВОЛНА: Дерево получило -260 урона! (Осталось {remaining} босс)",
                fr: $"ONDE DE CHOC : L'Arbre Sacré a subi -260 dégâts ! ({remaining} restants)",
                es: $"ONDA DESTRUCTIVA: ¡Árbol dañado -260 HP! (Quedan {remaining} jefes)",
                hi: $"विनाशक प्रहार: पवित्र वृक्ष को -260 क्षति! ({remaining} बॉस शेष)",
                zh: $"毁灭冲击：神圣巨树受到-260伤害！（还剩{remaining}个守护领主）",
                ja: $"破壊の衝撃波：巨樹に-260のダメージ！（残り{remaining}体のボス）",
                pt: $"ONDA DESTRUIDORA: Árvore Sagrada sofreu -260 de dano! ({remaining} restantes)"
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
        if (MatchEndStatsUI.IsGameOverActive || (GameStateManager.Instance != null && GameStateManager.Instance.currentState == GameStateManager.GameState.Ended)) return;
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

        bool isPlaying = GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying();

        // Hide top-right Guide & Jukebox buttons during active combat to keep the battlefield completely clear.
        // Both Guide & Jukebox are fully accessible via InGamePauseMenuUI (⚙️ PAUSE / ESC) during match.
        if (!isPlaying && 
            !CharacterSelectionUI.IsSelectionOpen && 
            !PlayerProgressionAndShopUI.IsShopOpen && 
            !LanguageSelectionUI.IsLanguageModalOpen &&
            !MatchEndStatsUI.IsGameOverActive)
        {
            DrawHowToPlayToggleButton();
        }

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
        float btnW = 96f;
        float btnH = 28f;
        float audioBtnW = 44f;
        float flagBtnW = 44f;
        // Sits in top-right ribbon: [Guide (96px)] [Flag (44px)] [Audio/Jukebox (44px)]
        float x = Screen.width - audioBtnW - 10f - flagBtnW - 6f - btnW - 6f;
        float y = 8f;

        string btnLabel = showBriefingModal
            ? "❌ " + LanguageSelectionUI.Localize("CLOSE", ar: "إغلاق", ru: "ЗАКРЫТЬ", es: "CERRAR", fr: "FERMER")
            : LanguageSelectionUI.Localize("📖 GUIDE", ar: "📖 التوجيهات", ru: "📖 ИНФО", es: "📖 GUÍA", fr: "📖 GUIDE");

        Color accent = showBriefingModal ? new Color(1f, 0.78f, 0.22f) : EnchantedNatureGUI.AntiqueGoldBright;
        if (EnchantedNatureGUI.DrawAncientGoldButton(new Rect(x, y, btnW, btnH), btnLabel, accent, 11, true))
        {
            showBriefingModal = !showBriefingModal;
            autoPlayPresentation = false;
        }

        // Top-right corner audio & jukebox button (accessible directly during matches, Solid & Crisp)
        float audioX = Screen.width - audioBtnW - 10f;
        bool isMatchAudioMuted = (ForestBGMManager.Instance != null && ForestBGMManager.Instance.IsMutedTrack);
        if (EnchantedNatureGUI.DrawMusicJukeboxButton(new Rect(audioX, y, audioBtnW, btnH), isMatchAudioMuted))
        {
            ForestBGMManager.ToggleJukeboxModal();
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
        string faSeals = "";
        string ruSeals = "";
        string esSeals = "";
        string frSeals = "";
        string deSeals = "";
        string itSeals = "";
        string trSeals = "";

        for (int i = 1; i <= 3; i++)
        {
            if (i <= defeated)
            {
                enSeals += " [💀 SLAIN] ";
                arSeals += " [💀 مهزوم] ";
                faSeals += " [💀 نابود شد] ";
                ruSeals += " [💀 ПОВЕРЖЕН] ";
                esSeals += " [💀 DERROTADO] ";
                frSeals += " [💀 VAINCU] ";
                deSeals += " [💀 BESIEGT] ";
                itSeals += " [💀 SCONFITTO] ";
                trSeals += " [💀 YENİLDİ] ";
            }
            else
            {
                enSeals += $" [👑 BOSS {i}] ";
                arSeals += $" [👑 زعيم {i}] ";
                faSeals += $" [👑 رئیس {i}] ";
                ruSeals += $" [👑 БОСС {i}] ";
                esSeals += $" [👑 JEFE {i}] ";
                frSeals += $" [👑 BOSS {i}] ";
                deSeals += $" [👑 BOSS {i}] ";
                itSeals += $" [👑 BOSS {i}] ";
                trSeals += $" [👑 PATRON {i}] ";
            }
        }

        if (!allDead)
        {
            style.normal.textColor = new Color(0.92f, 0.96f, 1f);
            string activeShieldLabel = LanguageSelectionUI.Localize(
                $"🛡️ TREE SHIELD ACTIVE | SEALS:{enSeals}({defeated}/3)",
                ar: $"🛡️ درع الشجرة نشط | الأختام:{arSeals}({defeated}/3)",
                fa: $"🛡️ سپر درخت فعال است | مهرها:{faSeals}({defeated}/3)",
                ru: $"🛡️ ЩИТ ДЕРЕВА АКТИВЕН | ПЕЧАТИ:{ruSeals}({defeated}/3)",
                es: $"🛡️ ESCUDO ACTIVO | SELLOS:{esSeals}({defeated}/3)",
                fr: $"🛡️ BOUCLIER ACTIF | SCEAUX:{frSeals}({defeated}/3)",
                de: $"🛡️ BAUMSCHILD AKTIV | SIEGEL:{deSeals}({defeated}/3)",
                it: $"🛡️ SCUDO ATTIVO | SIGILLI:{itSeals}({defeated}/3)",
                tr: $"🛡️ AĞAÇ KALKANI AKTİF | MÜHÜRLER:{trSeals}({defeated}/3)"
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
                    fa: "🔥 هر ۳ غول نابود شدند! سپر درخت شکست — اکنون درخت را نابود کنید! 🔥",
                    de: "🔥 ALLE 3 BOSSE BESIEGT! SCHILD GEBROCHEN — ZERSTÖRT DEN BAUM! 🔥",
                    it: "🔥 TUTTI E 3 I BOSS SCONFITTI! SCUDO SPEZZATO — DISTRUGGI L'ALBERO! 🔥",
                    tr: "🔥 3 PATRON DA YENİLDİ! KALKAN KIRILDI — AĞACI YOK EDİN! 🔥",
                    ru: "🔥 ВСЕ 3 БОССА ПОВЕРЖЕНЫ! ЩИТ СНЯТ — СРУБИТЕ ДЕРЕВО! 🔥",
                    fr: "🔥 LES 3 BOSS SONT VAINCUS ! BOUCLIER BRISÉ — DÉTRUISEZ L'ARBRE ! 🔥",
                    es: "🔥 ¡LOS 3 JEFES HAN CAÍDO! ESCUDO ROTO — ¡DESTRUYE EL ÁRBOL! 🔥",
                    hi: "🔥 तीनों बॉस मारे गए! ढाल टूट गई — अब पेड़ को नष्ट करें! 🔥",
                    zh: "🔥 全部3位领主已被斩杀！神树护盾粉碎 — 立即摧毁神树！🔥",
                    ja: "🔥 3大ボス討伐完了！シールド崩壊 — 神木を伐採せよ！🔥",
                    pt: "🔥 TODOS OS 3 CHEFES DERROTADOS! ESCUDO QUEBRADO — DESTRUA A ÁRVORE! 🔥"
                );
                GUI.Label(new Rect(x, y, w, h), text, style);
            }
            else
            {
                style.normal.textColor = new Color(0.35f, 0.95f, 1f);
                string text = LanguageSelectionUI.Localize(
                    "💎 ALL 3 BOSSES SLAIN! CRYSTAL IS READY — DELIVER IT TO THE ALTAR! 💎",
                    ar: "💎 تم القضاء على الزعماء الثلاثة! الكريستالة جاهزة — أوصلوها إلى المذبح! 💎",
                    fa: "💎 هر ۳ غول نابود شدند! بلور آماده است — آن را به محراب برسانید! 💎",
                    de: "💎 ALLE 3 BOSSE BESIEGT! KRISTALL BEREIT — ZUM ALTAR BRINGEN! 💎",
                    it: "💎 TUTTI E 3 I BOSS SCONFITTI! IL CRISTALLO È PRONTO — PORTALO ALL'ALTARE! 💎",
                    tr: "💎 3 PATRON DA YENİLDİ! KRİSTAL HAZIR — SUNAĞA TAŞIYIN! 💎",
                    ru: "💎 ВСЕ 3 БОССА ПОВЕРЖЕНЫ! КРИСТАЛЛ ГОТОВ — ДОСТАВЬТЕ ЕГО К АЛТАРЮ! 💎",
                    fr: "💎 LES 3 BOSS SONT VAINCUS ! LE CRISTAL EST PRÊT — LIVREZ-LE À L'AUTEL ! 💎",
                    es: "💎 ¡LOS 3 JEFES HAN CAÍDO! CRISTAL DISPONIBLE — ¡LLÉVALO AL ALTAR! 💎",
                    hi: "💎 तीनों बॉस मारे गए! क्रिस्टल तैयार है — इसे वेदी तक पहुँचाएँ! 💎",
                    zh: "💎 全部3位领主已被斩杀！神圣水晶现世 — 立即送往圣坛！💎",
                    ja: "💎 3大ボス討伐完了！水晶出現 — 祭壇へ奉納せよ！💎",
                    pt: "💎 TODOS OS 3 CHEFES DERROTADOS! O CRISTAL ESTÁ PRONTO — LEVE AO ALTAR! 💎"
                );
                GUI.Label(new Rect(x, y, w, h), text, style);
            }
        }
    }

    private void DrawGiantCenterAnnouncement()
    {
        float alpha = Mathf.Clamp01(bigBannerTimer / 0.6f);
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.03f;

        float w = Mathf.Min(680f, Screen.width * 0.85f) * pulse;
        float h = isUltimateThreeBossBanner ? 92f : 68f;
        float x = (Screen.width - w) / 2f;
        float y = 52f; // Near top of screen, far away from crosshair and center combat

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
                head = LanguageSelectionUI.Localize(
                    "🔥 ALL 3 GUARDIAN BOSSES HAVE BEEN DEFEATED! 🔥",
                    ar: "🔥 تم القضاء على الزعماء الحراس الثلاثة بالكامل! 🔥",
                    fa: "🔥 هر ۳ غول نگهبان کاملاً نابود شدند! 🔥",
                    de: "🔥 ALLE 3 WÄCHTER-BOSSE WURDEN BESIEGT! 🔥",
                    it: "🔥 TUTTI E 3 I BOSS GUARDIANI SONO STATI SCONFITTI! 🔥",
                    tr: "🔥 3 MUHAFIZ PATRONUN TAMAMI KATLEDİLDİ! 🔥",
                    ru: "🔥 ВСЕ 3 БОССА ПОВЕРЖЕНЫ! 🔥",
                    fr: "🔥 LES 3 BOSS GARDIENS ONT ÉTÉ VAINCUS ! 🔥",
                    es: "🔥 ¡LOS 3 JEFES GUARDIANES HAN CAÍDO! 🔥",
                    hi: "🔥 तीनों अभिभावक बॉस पराजित हो गए! 🔥",
                    zh: "🔥 全部3位守护首领已被彻底击溃！🔥",
                    ja: "🔥 3体の守護ボスを全て討伐完了！🔥",
                    pt: "🔥 TODOS OS 3 CHEFES GUARDIÕES FORAM DERROTADOS! 🔥"
                );
                main = LanguageSelectionUI.Localize(
                    "YOU CAN NOW DESTROY THE SACRED TREE!",
                    ar: "يمكنك الآن تدمير الشجرة المقدسة!",
                    fa: "اکنون می‌توانید درخت مقدس را نابود کنید!",
                    de: "IHR KÖNNT NUN DEN HEILIGEN BAUM ZERSTÖREN!",
                    it: "ORA PUOI DISTRUGGERE IL SACRO ALBERO!",
                    tr: "ARTIK KUTSAL AĞACI YOK EDEBİLİRSİNİZ!",
                    ru: "ТЕПЕРЬ ВЫ МОЖЕТЕ СРУБИТЬ ДЕРЕВО!",
                    fr: "VOUS POUVEZ MAINTENANT DÉTRUIRE L'ARBRE SACRÉ !",
                    es: "¡AHORA PUEDES DESTRUIR EL ÁRBOL SAGRADO!",
                    hi: "अब आप पवित्र वृक्ष को नष्ट कर सकते हैं!",
                    zh: "神圣巨树护盾已卸下，即刻伐倒它！",
                    ja: "今こそ聖なる巨木を伐採せよ！",
                    pt: "AGORA VOCÊ PODE DESTRUIR A ÁRVORE SAGRADA!"
                );
                sub = LanguageSelectionUI.Localize(
                    "10-Second Divine Shield Countdown active! Position yourselves — Tree becomes vulnerable in 10s!",
                    ar: "العد التنازلي لدرع الصمود المقدس بدأ (10 ثوانٍ)! خذوا مواقعكم — الشجرة ستصبح مكشوفة بالكامل بعد 10 ثوانٍ!",
                    fa: "شمارش معکوس سپر مقدس ۱۰ ثانیه‌ای آغاز شد! جای‌گیری کنید — درخت پس از ۱۰ ثانیه آسیب‌پذیر می‌شود!",
                    de: "10-Sekunden-Göttlicher-Schild aktiv! Geht in Stellung — Baum in 10s verwundbar!",
                    it: "Conto alla rovescia Scudo Divino di 10s attivo! Posizionatevi — L'albero sarà vulnerabile tra 10s!",
                    tr: "10 Saniyelik İlahi Kalkan Geri Sayımı başladı! Konum alın — Ağaç 10 sn içinde savunmasız kalacak!",
                    ru: "ОТСЧЕТ 10 СЕКУНД ДО СНЯТИЯ ЩИТА! Займите позиции — Дерево станет уязвимым через 10 сек!",
                    fr: "COMPTE À REBOURS DE 10S DU BOUCLIER DIVIN ! Prenez position — L'Arbre sera vulnérable dans 10s !",
                    es: "¡CUENTA ATRÁS DE 10S DEL ESCUDO DIVINO! ¡El Árbol será vulnerable en 10s!",
                    hi: "१० सेकंड दिव्य ढाल उलटी गिनती सक्रिय! स्थान लें — पेड़ १० सेकंड में असुरक्षित हो जाएगा!",
                    zh: "10秒神圣圣树护盾倒计时已启动！就位迎战 — 古树将在10秒后失去免疫！",
                    ja: "神聖シールドの10秒カウントダウン開始！配置につけ — 10秒後に神木の無敵が解除される！",
                    pt: "CONTAGEM REGRESSIVA DE 10S DO ESCUDO DIVINO! Posicione-se — Árvore vulnerável em 10s!"
                );
            }
            else
            {
                frameColor = new Color(0.18f, 0.88f, 1f, alpha);
                head = LanguageSelectionUI.Localize(
                    "💎 ALL 3 GUARDIAN BOSSES HAVE BEEN DEFEATED! 💎",
                    ar: "💎 تم القضاء على الزعماء الحراس الثلاثة بالكامل! 💎",
                    fa: "💎 هر ۳ غول نگهبان کاملاً نابود شدند! 💎",
                    de: "💎 ALLE 3 WÄCHTER-BOSSE WURDEN BESIEGT! 💎",
                    it: "💎 TUTTI E 3 I BOSS GUARDIANI SONO STATI SCONFITTI! 💎",
                    tr: "💎 3 MUHAFIZ PATRONUN TAMAMI KATLEDİLDİ! 💎",
                    ru: "💎 ВСЕ 3 БОССА ПОВЕРЖЕНЫ! 💎",
                    fr: "💎 LES 3 BOSS GARDIENS ONT ÉTÉ VAINCUS ! 💎",
                    es: "💎 ¡LOS 3 JEFES GUARDIANES HAN CAÍDO! 💎",
                    hi: "💎 तीनों अभिभावक बॉस पराजित हो गए! 💎",
                    zh: "💎 全部3位守护首领已被彻底击溃！💎",
                    ja: "💎 3体の守護ボスを全て討伐完了！💎",
                    pt: "💎 TODOS OS 3 CHEFES GUARDIÕES FORAM DERROTADOS! 💎"
                );
                main = LanguageSelectionUI.Localize(
                    "THE CRYSTAL IS READY — YOU CAN TAKE IT NOW!",
                    ar: "الكريستالة جاهزة — يمكنك أخذها الآن!",
                    fa: "بلور مقدس آماده است — اکنون می‌توانید آن را بردارید!",
                    de: "DER KRISTALL IST BEREIT — IHR KÖNNT IHN JETZT NEHMEN!",
                    it: "IL CRISTALLO È PRONTO — PUOI PRENDERLO ORA!",
                    tr: "KRİSTAL HAZIR — ARTIK ONI ALABİLİRSİNİZ!",
                    ru: "КРИСТАЛЛ ГОТОВ — ЗАБЕРИТЕ ЕГО!",
                    fr: "LE CRISTAL EST PRÊT — PRENEZ-LE MAINTENANT !",
                    es: "¡EL CRISTAL ESTÁ LISTO — PUEDES TOMARLO AHORA!",
                    hi: "क्रिस्टल तैयार है — अब आप इसे ले सकते हैं!",
                    zh: "圣域水晶已凝结 — 立即夺取水晶！",
                    ja: "聖なる水晶が解放された — 今すぐ手に入れよ！",
                    pt: "O CRISTAL ESTÁ PRONTO — VOCÊ PODE PEGÁ-LO AGORA!"
                );
                sub = LanguageSelectionUI.Localize(
                    "The Sacred Crystal has appeared! Grab the Crystal and deliver it to the Sacred Altar to WIN!",
                    ar: "ظهرت الكريستالة المقدسة! احمل الكريستالة وأوصلها إلى المذبح المقدس للفوز بالمعركة!",
                    fa: "بلور مقدس ظاهر شد! بلور را بردارید و برای پیروزی به محراب مقدس برسانید!",
                    de: "Der heilige Kristall ist erschienen! Tragt ihn zum Altar, um zu GEWINNEN!",
                    it: "Il Cristallo Sacro è apparso! Portalo all'Altare Sacro per VINCERE!",
                    tr: "Kutsal Kristal belirdi! Kristali alıp Kutsal Sunak'a taşıyarak KAZANIN!",
                    ru: "Священный кристалл появился! Отнесите его к алтарю для победы!",
                    fr: "Le Cristal Sacré est apparu ! Portez-le à l'Autel pour GAGNER !",
                    es: "¡El Cristal Sagrado ha aparecido! ¡Llévalo al Altar para ganar!",
                    hi: "पवित्र क्रिस्टल प्रकट हुआ है! जीतने के लिए इसे पवित्र वेदी तक पहुँचाएँ!",
                    zh: "神圣水晶已降世！夺取水晶并护送至圣坛即可夺取全盘胜利！",
                    ja: "聖なる水晶が出現した！水晶を運んで聖なる祭壇へ奉納し、勝利を掴め！",
                    pt: "O Cristal Sagrado apareceu! Leve-o ao Altar Sagrado para VENCER!"
                );
            }
        }

        // Translucent glass backdrop that never obscures gameplay or crosshairs
        GUI.color = new Color(0.02f, 0.04f, 0.05f, 0.45f * alpha);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // Sleek glowing neon border
        GUI.color = new Color(frameColor.r, frameColor.g, frameColor.b, 0.85f * alpha);
        GUI.DrawTexture(new Rect(x, y, w, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - 1.5f, w, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 1.5f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - 1.5f, y, 1.5f, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle headStyle = new GUIStyle(GUI.skin.label);
        headStyle.fontSize = isUltimateThreeBossBanner ? 13 : 11;
        headStyle.fontStyle = FontStyle.Bold;
        headStyle.alignment = TextAnchor.MiddleCenter;
        headStyle.normal.textColor = new Color(1f, 0.88f, 0.28f, alpha);

        GUIStyle mainStyle = new GUIStyle(GUI.skin.label);
        mainStyle.fontSize = isUltimateThreeBossBanner ? 17 : 14;
        mainStyle.fontStyle = FontStyle.Bold;
        mainStyle.alignment = TextAnchor.MiddleCenter;
        mainStyle.normal.textColor = new Color(frameColor.r, frameColor.g, frameColor.b, alpha);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = isUltimateThreeBossBanner ? 11 : 10;
        subStyle.fontStyle = FontStyle.Bold;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.wordWrap = true;
        subStyle.normal.textColor = new Color(0.95f, 0.97f, 1f, alpha);

        GUI.Label(new Rect(x + 10f, y + 4f, w - 20f, 18f), head, headStyle);
        GUI.Label(new Rect(x + 10f, y + 22f, w - 20f, 22f), main, mainStyle);
        GUI.Label(new Rect(x + 12f, y + 44f, w - 24f, isUltimateThreeBossBanner ? 40f : 20f), sub, subStyle);
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
            case 0: return 460f;
            case 1: return 490f;
            case 2: return 560f;
            case 3: return 540f;
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
            ar: "في قلب الغابة تقف نبتة الفاصولياء العملاقة المقدسة. كلما نمت للأعلى تفتح 3 منصات مرتفعة (المستوى 1 و2 و3)، ويحرس كل منصة زعيم جبار!",
            fa: "در قلب جنگل، ساقه لوبیای غول‌پیکر و مقدس برافراشته شده است. با رشد آن، ۳ سکوی مرتفع (طبقات ۱، ۲ و ۳) باز می‌شوند که هر یک تحت محافظت یک غول نیرومند قرار دارد!",
            de: "Im Herzen des Waldes steht die Riesenbohnenranke. Während sie wächst, schaltet sie 3 Plattformen frei (Stufe 1, 2 und 3), bewacht von je einem mächtigen Boss!",
            it: "Al centro della foresta si erge la Pianta di Fagiolo Sacra. Crescendo, sblocca 3 piattaforme (Livello 1, 2 e 3), ciascuna sorvegliata da un potente Boss!",
            tr: "Ormanın merkezinde devasa Kutsal Fasulye Sırığı yükseliyor. Büyüdükçe 3 yüksek platformu (Seviye 1, 2 ve 3) açar ve her birini devasa bir Muhafız Patron korur!",
            ru: "В центре леса возвышается гигантский Священный стебель. Растя вверх, он открывает 3 платформы (Уровень 1, 2 и 3), каждую из которых охраняет могучий Босс!",
            es: "En el centro del bosque se alza el Tallo Sagrado. Al crecer, desbloquea 3 plataformas custodiadas por un temible Jefe Titán.",
            fr: "Au centre de la forêt se dresse le Haricot Sacré Géant. En grandissant, il débloque 3 plateformes gardées par un puissant Boss Titan !",
            zh: "森林中央伫立着巨大的神圣魔豆藤。随着巨藤生长，它将解锁3座高空平台（第1、2、3层），每层都有强大的泰坦首领镇守！",
            ja: "森の中央にそびえ立つ巨大な聖なる豆の木。成長するにつれ3つの高空足場（階層1・2・3）が解放され、それぞれを強大なタイタンボスが守護しています！",
            hi: "जंगल के केंद्र में विशाल पवित्र सेम का पौधा खड़ा है। जैसे-जैसे यह बढ़ता है, यह 3 ऊंचे मंचों को खोलता है, जिनमें से प्रत्येक की रक्षा एक शक्तिशाली बॉस करता है!",
            pt: "No centro da floresta ergue-se o Pé de Feijão Sagrado. À medida que cresce, desbloqueia 3 plataformas (Nível 1, 2 e 3), cada uma guardada por um poderoso Chefe!"
        );
        GUI.Label(new Rect(area.x + 8f, area.y + 32f, area.width - 16f, 48f), slide1Intro, bodyStyle);

        float cardW = (area.width - 24f) / 3f;
        float cardH = 345f;
        float cy = area.y + 82f;

        // Card 1: Protectors
        Rect pRect = new Rect(area.x, cy, cardW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(pRect, new Color(0.06f, 0.18f, 0.28f, 0.95f), new Color(0.2f, 0.75f, 1f));
        string protTitle = LanguageSelectionUI.Localize("🛡️ PROTECTORS (BLUE)");
        GUI.Label(new Rect(pRect.x + 8f, pRect.y + 12f, pRect.width - 16f, 26f), protTitle, CreateHeadingStyle(14, new Color(0.35f, 0.88f, 1f), TextAnchor.MiddleCenter));

        string protBody = LanguageSelectionUI.Localize(
            "• Guardians of the Forest.\n\n• Your attacks on the Sacred Tree HEAL it instead of hurting it!\n\n• Slay the 3 Bosses to unseal the Sacred Crystal, then carry the Crystal to the Altar to WIN!",
            ar: "• حراس الغابة المقدسة.\n\n• ضرباتكم على الشجرة المقدسة تشفيها وتزيد نقاط صحتها بدلاً من الإضرار بها!\n\n• اقضوا على الزعماء الثلاثة لفك أختام الكريستالة، ثم احملوها للمذبح للفوز بالمباراة!",
            fa: "• نگهبانان جنگل مقدس.\n\n• حملات شما به درخت مقدس به جای آسیب، آن را درمان و التیام می‌بخشد!\n\n• هر ۳ غول را از پای درآورید تا بلور مقدس آزاد شود، سپس بلور را به محراب برسانید تا برنده شوید!",
            de: "• Hüter des Waldes.\n\n• Eure Angriffe auf den Baum HEILEN ihn!\n\n• Besiegt die 3 Bosse, um den Kristall freizuschalten, und bringt ihn zum Altar, um zu GEWINNEN!",
            it: "• Guardiani della foresta.\n\n• I tuoi attacchi all'Albero Sacro lo CURANO invece di danneggiarlo!\n\n• Sconfiggi i 3 Boss per sbloccare il Cristallo, poi portalo all'Altare per VINCERE!",
            tr: "• Ormanın Koruyucuları.\n\n• Kutsal Ağaca saldırılarınız ona zarar vermek yerine onu İYİLEŞTİRİR!\n\n• Kutsal Kristalin mührünü kırmak için 3 Patronu yok edin, ardından Kristali Sunaka taşıyarak KAZANIN!",
            ru: "• Стражи леса.\n\n• Ваши атаки по Дереву ЛЕЧАТ его вместо нанесения урона!\n\n• Победите 3 Боссов, чтобы открыть Кристалл, и отнесите его к Алтарю для победы!",
            es: "• Guardianes del bosque.\n\n• ¡Tus ataques al Árbol lo CURAN en lugar de dañarlo!\n\n• ¡Vence a los 3 Jefes para liberar el Cristal y llévalo al Altar para ganar!",
            fr: "• Gardiens de la Forêt.\n\n• Vos attaques sur l'Arbre le SOIGNENT au lieu de le blesser !\n\n• Éliminez les 3 Boss pour desceller le Cristal Sacré, puis portez-le à l'Autel pour GAGNER !",
            zh: "• 圣林守护者。\n\n• 你们对神树的攻击将治疗巨树并恢复生命，而非造成伤害！\n\n• 击杀全部3名首领解除圣水晶封印，将水晶送至祭坛即可获胜！",
            ja: "• 森の聖なる守護者。\n\n• 神聖な巨木への攻撃はダメージにならず、生命力を回復させます！\n\n• 3体のボスを討伐して水晶の封印を解き、祭壇へ運んで勝利をつかみ取れ！",
            hi: "• जंगल के संरक्षक।\n\n• पेड़ पर आपके हमले इसे नुकसान पहुँचाने के बजाय ठीक करते हैं!\n\n• पवित्र क्रिस्टल को खोलने के लिए 3 बॉस को मारें, फिर जीतने के लिए क्रिस्टल को वेदी तक ले जाएँ!",
            pt: "• Guardiões da floresta.\n\n• Seus ataques na Árvore Sagrada a CURAM em vez de feri-la!\n\n• Derrote os 3 Chefes para libertar o Cristal e leve-o ao Altar para VENCER!"
        );
        GUI.Label(new Rect(pRect.x + 12f, pRect.y + 44f, pRect.width - 24f, cardH - 52f), protBody, bodyStyle);

        // Card 2: The Sacred Tree & 3 Levels
        Rect tRect = new Rect(area.x + cardW + 12f, cy, cardW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(tRect, new Color(0.08f, 0.20f, 0.10f, 0.95f), new Color(0.35f, 0.95f, 0.45f));
        string treeTitle = LanguageSelectionUI.Localize("🌲 THE 3 TREE LEVELS", ar: "🌲 مستويات الشجرة الـ 3", ru: "🌲 3 УРОВНЯ ДЕРЕВА", es: "🌲 LOS 3 NIVELES DEL ÁRBOL", fr: "🌲 LES 3 NIVEAUX DE L'ARBRE");
        GUI.Label(new Rect(tRect.x + 8f, tRect.y + 12f, tRect.width - 16f, 26f), treeTitle, CreateHeadingStyle(14, new Color(0.45f, 1f, 0.55f), TextAnchor.MiddleCenter));

        float animGrowth = 0.35f + Mathf.PingPong(Time.time * 0.25f, 0.65f);
        float barX = tRect.x + 14f;
        float barY = tRect.y + 44f;
        float barH = cardH - 56f;
        float barW = 16f;
        GUI.color = new Color(0.15f, 0.12f, 0.08f, 0.9f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.28f, 0.88f, 0.38f, 0.95f);
        float fillH = barH * animGrowth;
        GUI.DrawTexture(new Rect(barX, barY + (barH - fillH), barW, fillH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float labelX = barX + barW + 10f;
        float labelW = tRect.x + tRect.width - labelX - 10f;

        // Level Top: Sacred Crystal
        float slot0Y = barY;
        GUI.Label(new Rect(labelX, slot0Y, labelW, 20f),
            LanguageSelectionUI.Localize("💎 TOP: Sacred Crystal", ar: "💎 القمة: الكريستالة المقدسة", ru: "💎 ВЕРШИНА: Священный Кристалл", es: "💎 CIMA: Cristal Sagrado", fr: "💎 SOMMET : Cristal Sacré", fa: "💎 اوج: بلور مقدس"),
            CreateHeadingStyle(11, new Color(0.4f, 0.95f, 1f)));
        GUI.Label(new Rect(labelX, slot0Y + 20f, labelW, 36f),
            LanguageSelectionUI.Localize("• Unsealed after 3 Bosses die\n• Carry to Altar to WIN!", ar: "• تفك أختامها بعد موت الـ 3 زعماء\n• احملوها للمذبح لتحقيق الفوز!", ru: "• Открывается после гибели 3 Боссов\n• Отнесите к Алтарю для победы!", es: "• Se libera tras vencer a los 3 Jefes\n• ¡Llévalo al Altar para ganar!", fr: "• Descellé après la mort des 3 Boss\n• Portez à l'Autel pour gagner !", fa: "• پس از مرگ ۳ غول آزاد می‌شود\n• برای پیروزی به محراب برسانید!"),
            CreateBodyStyle(10, new Color(0.85f, 0.95f, 1f)));

        // Level 3: Ancient Overlord
        float slot3Y = barY + barH * 0.27f;
        GUI.Label(new Rect(labelX, slot3Y, labelW, 20f),
            LanguageSelectionUI.Localize("👑 LEVEL 3: Ancient Overlord", ar: "👑 المستوى 3: الحاكم القديم", ru: "👑 УРОВЕНЬ 3: Древний Владыка", es: "👑 NIVEL 3: Soberano Antiguo", fr: "👑 NIVEAU 3 : Seigneur Ancien", fa: "👑 طبقه ۳: فرمانروای باستانی"),
            CreateHeadingStyle(11, new Color(1f, 0.85f, 0.3f)));
        GUI.Label(new Rect(labelX, slot3Y + 20f, labelW, 36f),
            LanguageSelectionUI.Localize("• 300% Height Damage Power!\n• Final Seal Guardian Boss", ar: "• 300% قوة ضرر مضاعفة بالقمة!\n• زعيم الختم الأخير والحاسم", ru: "• 300% урона с высоты!\n• Босс последней Печати", es: "• ¡300% Daño desde la cima!\n• Jefe del Sello Final", fr: "• 300% Dégâts depuis le sommet !\n• Boss du Sceau Final", fa: "• ۳۰۰٪ آسیب قدرتمند از اوج!\n• نگهبان آخرین مهر بلور"),
            CreateBodyStyle(10, new Color(1f, 0.92f, 0.65f)));

        // Level 2: Inferno Titan
        float slot2Y = barY + barH * 0.53f;
        GUI.Label(new Rect(labelX, slot2Y, labelW, 20f),
            LanguageSelectionUI.Localize("👑 LEVEL 2: Inferno Titan", ar: "👑 المستوى 2: عملاق الجحيم", ru: "👑 УРОВЕНЬ 2: Титан Преисподней", es: "👑 NIVEL 2: Titán Infernal", fr: "👑 NIVEAU 2 : Titan Infernal", fa: "👑 طبقه ۲: تایتان جهنمی"),
            CreateHeadingStyle(11, new Color(1f, 0.55f, 0.25f)));
        GUI.Label(new Rect(labelX, slot2Y + 20f, labelW, 36f),
            LanguageSelectionUI.Localize("• 220% Height Damage Power\n• Unlocks at 42% Tree Growth", ar: "• 220% قوة ضرر مضاعفة\n• يفتح عند نمو الشجرة لـ 42%", ru: "• 220% урона с высоты\n• Открывается при 42% роста", es: "• 220% Daño desde altura\n• Se abre al 42% de crecimiento", fr: "• 220% Dégâts d'élévation\n• S'ouvre à 42% de croissance", fa: "• ۲۲۰٪ قدرت آسیب ارتفاع\n• در ۴۲٪ رشد درخت باز می‌شود"),
            CreateBodyStyle(10, new Color(1f, 0.82f, 0.65f)));

        // Level 1: Forest Goliath
        float slot1Y = barY + barH * 0.79f;
        GUI.Label(new Rect(labelX, slot1Y, labelW, 20f),
            LanguageSelectionUI.Localize("👑 LEVEL 1: Forest Goliath", ar: "👑 المستوى 1: جالوت الغابة", ru: "👑 УРОВЕНЬ 1: Лесной Голиаф", es: "👑 NIVEL 1: Goliat del Bosque", fr: "👑 NIVEAU 1 : Goliath de la Forêt", fa: "👑 طبقه ۱: جالوت جنگل"),
            CreateHeadingStyle(11, new Color(0.45f, 0.95f, 0.45f)));
        GUI.Label(new Rect(labelX, slot1Y + 20f, labelW, 36f),
            LanguageSelectionUI.Localize("• 150% Height Damage Power\n• Active from match start!", ar: "• 150% قوة ضرر مضاعفة\n• متاح منذ بداية المباراة!", ru: "• 150% урона с высоты\n• Активен со старта матча!", es: "• 150% Daño desde altura\n• ¡Activo desde el inicio!", fr: "• 150% Dégâts d'élévation\n• Actif dès le début !", fa: "• ۱۵۰٪ قدرت آسیب ارتفاع\n• از آغاز نبرد فعال است!"),
            CreateBodyStyle(10, new Color(0.75f, 0.95f, 0.75f)));

        // Card 3: Destroyers
        Rect dRect = new Rect(area.x + (cardW + 12f) * 2f, cy, cardW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(dRect, new Color(0.26f, 0.08f, 0.07f, 0.95f), new Color(1f, 0.32f, 0.2f));
        string destTitle = LanguageSelectionUI.Localize("🔥 DESTROYERS (RED)");
        GUI.Label(new Rect(dRect.x + 8f, dRect.y + 12f, dRect.width - 16f, 26f), destTitle, CreateHeadingStyle(14, new Color(1f, 0.42f, 0.28f), TextAnchor.MiddleCenter));

        string destBody = LanguageSelectionUI.Localize(
            "• Conquerors of the Forest.\n\n• The Tree Shield protects the Tree from falling below 42% HP until all 3 Bosses fall, followed by a 10s Divine Countdown!\n\n• Strike from higher platforms for up to 100% full damage (Ground deals only 15%) to fell the Tree and WIN!",
            ar: "• غزاة الغابة.\n\n• درع الشجرة يحميها من السقوط دون 42% حتى يسقط الزعماء الثلاثة، يليه عد تنازلي 10 ثوانٍ للدرع!\n\n• هاجموا من المنصات المرتفعة للحصول على القوة الكاملة 100% (الأرض تعطي 15% فقط) لإسقاط الشجرة والفوز!",
            fa: "• فاتحان جنگل.\n\n• سپر درخت مانع از افت سلامت آن به زیر ۴۲٪ می‌شود تا زمانی که هر ۳ غول نابود شوند؛ سپس شمارش ۱۰ ثانیه‌ای آغاز می‌گردد!\n\n• از سکوهای بالاتر حمله کنید تا ۱۰۰٪ آسیب کامل وارد شود (زمین تنها ۱۵٪ آسیب می‌زند) تا درخت را سرنگون کنید و برنده شوید!",
            de: "• Eroberer des Waldes.\n\n• Der Baumschild schützt den Baum unter 42% HP, bis alle 3 Bosse fallen, gefolgt von einem 10s-Countdown!\n\n• Greift von höheren Plattformen für vollen 100%-Schaden an (Boden nur 15%), um den Baum zu fällen und zu GEWINNEN!",
            it: "• Conquistatori della foresta.\n\n• Lo Scudo protegge l'Albero sotto il 42% di HP fino alla caduta dei 3 Boss, seguito da un conto alla rovescia di 10s!\n\n• Colpisci dalle piattaforme elevate per il 100% di danno (da terra solo 15%) per abbattere l'Albero e VINCERE!",
            tr: "• Ormanın Fatihleri.\n\n• Ağaç Kalkanı, 3 Patron da ölene kadar Ağacın Canını %42'nin altına düşmekten korur, ardından 10 sn İlahi Geri Sayım başlar!\n\n• Ağacı devirip KAZANMAK için yüksek platformlardan saldırarak %100 tam hasar verin (Zemin sadece %15 vurur)!",
            ru: "• Завоеватели леса.\n\n• Щит защищает Дерево ниже 42% HP до гибели 3 Боссов, после чего идет 10-секундный отсчет!\n\n• Атакуйте с верхних ярусов для 100% урона (с земли лишь 15%), чтобы срубить Дерево и победить!",
            es: "• Conquistadores del bosque.\n\n• El escudo protege el Árbol hasta que caigan los 3 Jefes, seguido de una cuenta de 10s.\n\n• ¡Ataca desde plataformas altas para el 100% del daño (en el suelo solo 15%) y derriba el Árbol!",
            fr: "• Conquérants de la Forêt.\n\n• Le bouclier protège l'Arbre jusqu'à la chute des 3 Boss, suivi d'un compte de 10s !\n\n• Attaquez depuis les hauteurs pour 100% des dégâts (au sol seulement 15%) pour abattre l'Arbre !",
            zh: "• 森林征服者。\n\n• 神树护盾锁死最低42%生命值，直至击杀全部3名首领，随后触发10秒破盾倒计时！\n\n• 从高层平台攻击可造成100%完整满额伤害（地面仅有15%伤害），伐倒巨树赢得胜利！",
            ja: "• 森の征服者。\n\n• 3大ボスを全滅させるまで、シールドが巨木のHP42%以下への減少を防ぎます（撃破後に10秒カウントダウン）！\n\n• 上層の足場から攻撃すれば100%の最大威力を与えられます（地上はわずか15%）。神木を倒して勝利を掴め！",
            hi: "• जंगल के विजेता।\n\n• पेड़ की ढाल तीनों बॉस के मरने तक 42% से नीचे स्वास्थ्य गिरने से बचाती है!\n\n• पेड़ को गिराने और जीतने के लिए ऊंचे मंचों से 100% नुकसान पहुंचाएं!",
            pt: "• Conquistadores da floresta.\n\n• O Escudo protege a Árvore abaixo de 42% de vida até que os 3 Chefes caiam, seguido por contagem de 10s!\n\n• Ataque das plataformas altas para 100% de dano total (chão causa apenas 15%) para derrubar a Árvore e VENCER!"
        );
        GUI.Label(new Rect(dRect.x + 12f, dRect.y + 44f, dRect.width - 24f, cardH - 52f), destBody, bodyStyle);
    }

    private void DrawSlide2ThreeBosses(Rect area)
    {
        GUIStyle hStyle = CreateHeadingStyle(18, new Color(1f, 0.86f, 0.28f));
        GUIStyle bodyStyle = CreateBodyStyle(13, Color.white);

        string slide2Header = LanguageSelectionUI.Localize("👑 THE 3 GUARDIAN BOSSES — KEYS TO VICTORY");
        GUI.Label(new Rect(area.x, area.y, area.width, 28f), slide2Header, hStyle);

        string slide2Rule = LanguageSelectionUI.Localize(
            "CRITICAL RULE: Neither team can finish the match until ALL 3 LEVEL BOSSES ARE DEFEATED!\n• The Tree Shield prevents the Tree from falling below 42% HP until all 3 Bosses die.\n• When Boss 3 dies, a 10-Second Divine Shield Countdown begins before the shield shatters completely!\n• The Sacred Crystal remains sealed until all 3 Bosses die.",
            ar: "قاعدة حاسمة: لا يمكن لأي فريق إنهاء المباراة حتى يُهزم الزعماء الثلاثة جميعهم!\n• درع الشجرة يمنع سقوط صحة الشجرة دون 42% حتى يموت الزعماء الثلاثة.\n• عند موت الزعيم الثالث، يبدأ عد تنازلي 10 ثوانٍ لدرع الصمود المقدس قبل زواله تماماً!\n• الكريستالة المقدسة تظل مغلقة ومحكمة الختم حتى يموت الزعماء الثلاثة.",
            fa: "قاعده حیاتی: هیچ تیمی نمی‌تواند مسابقه را ببرد تا زمانی که هر ۳ غول شکست نخورند!\n• سپر درخت مانع از افت سلامت آن به زیر ۴۲٪ تا زمان نابودی هر ۳ غول می‌شود.\n• پس از مرگ غول ۳، شمارش ۱۰ ثانیه‌ای سپر آغاز می‌شود!\n• بلور مقدس تا زمان نابودی هر ۳ غول در مهر و موم باقی می‌ماند.",
            de: "WICHTIGE REGEL: Kein Team kann gewinnen, bevor ALLE 3 BOSSE BESIEGT SIND!\n• Der Baumschild verhindert, dass der Baum unter 42% HP fällt, bis alle 3 Bosse tot sind.\n• Stirbt Boss 3, startet ein 10s-Countdown bis zum Schildbruch!\n• Der Kristall bleibt versiegelt, bis alle 3 Bosse fallen.",
            it: "REGOLA CRITICA: Nessuna squadra può vincere finché TUTTI E 3 I BOSS NON SARANNO SCONFITTI!\n• Lo Scudo impedisce all'Albero di scendere sotto il 42% finché i 3 Boss sono vivi.\n• Alla morte del Boss 3, parte un conto di 10s prima che lo Scudo crolli!\n• Il Cristallo Sacro resta sigillato finché i 3 Boss non muoiono.",
            tr: "KRİTİK KURAL: 3 PATRONUN HEPSİ YENİLMEDEN hiçbir takım maçı bitiremez!\n• Ağaç Kalkanı, 3 Patron ölene kadar Canın %42'nin altına düşmesini engeller.\n• Patron 3 öldüğünde, kalkan kırılmadan önce 10 sn İlahi Geri Sayım başlar!\n• Kutsal Kristal, 3 Patron da ölene kadar mühürlü kalır.",
            ru: "ГЛАВНОЕ ПРАВИЛО: Ни одна команда не может победить, пока ВСЕ 3 БОССА НЕ БУДУТ ПОВЕРЖЕНЫ!\n• Щит не дает здоровью Дерева упасть ниже 42%.\n• При гибели 3-го Босса включается 10-секундный Божественный отсчет до падения щита!\n• Священный Кристалл запечатан до гибели всех 3 Боссов.",
            es: "REGLA CRÍTICA: ¡Ningún equipo puede ganar hasta DERROTAR A LOS 3 JEFES!\n• El Escudo evita que el Árbol baje del 42% de salud.\n• ¡Al morir el 3er Jefe, inicia una cuenta atrás de 10s del Escudo Divino antes de caer!\n• El Cristal Sagrado permanece sellado hasta que mueran los 3 Jefes.",
            fr: "RÈGLE CRITIQUE : Aucune équipe ne peut gagner tant que LES 3 BOSS NE SONT PAS VAINCUS !\n• Le Bouclier empêche l'Arbre de descendre sous 42% de PV.\n• À la mort du 3e Boss, un compte à rebours de 10s du Bouclier Divin démarre avant sa chute !\n• Le Cristal Sacré reste scellé tant que les 3 Boss sont en vie.",
            zh: "关键对决法则：在击败全部3位守护首领前，任何阵营均无法取胜！\n• 在3位首领全部阵亡前，神树护盾锁定生命不低于42%。\n• 首领3阵亡时，立即启动10秒神圣破盾倒计时！\n• 圣水晶在3位首领全灭前始终处于绝对封印状态。",
            ja: "最重要ルール：3大ボスを全滅させるまで、いかなる陣営も勝利することはできません！\n• 3大ボス討伐まで、神聖シールドが巨木のHP42%以下への減少を防ぎます。\n• 第3ボス討伐時、シールド完全崩壊までの10秒カウントダウンが開始！\n• 水晶は3体のボスが全滅するまで厳重に封印されています。",
            hi: "महत्वपूर्ण नियम: जब तक तीनों बॉस पराजित नहीं हो जाते, कोई भी टीम मैच खत्म नहीं कर सकती!\n• पेड़ की ढाल इसे 42% से नीचे जाने से रोकती है।\n• बॉस 3 के मरने पर 10 सेकंड की उलटी गिनती शुरू होती है!\n• पवित्र क्रिस्टल तीनों बॉस के मरने तक सील रहता है।",
            pt: "REGRA CRÍTICA: Nenhuma equipe pode vencer até DERROTAR OS 3 CHEFES!\n• O Escudo impede que a Árvore caia abaixo de 42% de vida até que os 3 Chefes morram.\n• Quando o Chefe 3 morre, inicia-se uma contagem de 10s antes do escudo quebrar!\n• O Cristal Sagrado permanece selado até que os 3 Chefes morram."
        );
        GUI.Label(new Rect(area.x + 8f, area.y + 32f, area.width - 16f, 48f), slide2Rule, bodyStyle);

        float cardW = (area.width - 24f) / 3f;
        float cardH = 380f;
        float cy = area.y + 84f;

        DrawBossCard(
            new Rect(area.x, cy, cardW, cardH),
            LanguageSelectionUI.Localize("👑 BOSS 1: FOREST GOLIATH"),
            LanguageSelectionUI.Localize("Platform: LEVEL 1 (Active from Start)"),
            LanguageSelectionUI.Localize("HP: 1,800  |  Reward: +250 Coins & 500 XP"),
            LanguageSelectionUI.Localize("Guards the Lower Canopy. Both teams must climb Level 1 immediately at the start of battle to contest this Boss!"),
            new Color(0.25f, 0.85f, 0.35f)
        );

        DrawBossCard(
            new Rect(area.x + cardW + 12f, cy, cardW, cardH),
            LanguageSelectionUI.Localize("👑 BOSS 2: INFERNO TITAN"),
            LanguageSelectionUI.Localize("Platform: LEVEL 2 (Unlocks at 42% Growth)"),
            LanguageSelectionUI.Localize("HP: 2,600  |  Reward: +400 Coins & 750 XP"),
            LanguageSelectionUI.Localize("Awakens as soon as the Beanstalk reaches Level 2. Deals heavy fiery strikes and drops a Spoils Chest on death!"),
            new Color(1f, 0.45f, 0.18f)
        );

        DrawBossCard(
            new Rect(area.x + (cardW + 12f) * 2f, cy, cardW, cardH),
            LanguageSelectionUI.Localize("👑 BOSS 3: ANCIENT OVERLORD"),
            LanguageSelectionUI.Localize("Platform: LEVEL 3 (Unlocks at 65% Growth)"),
            LanguageSelectionUI.Localize("HP: 3,500  |  Reward: +600 Coins & 1200 XP"),
            LanguageSelectionUI.Localize("The final Seal Guardian! Slaying the Overlord starts the 10s Divine Shield countdown for Destroyers AND unseals the Crystal for Protectors!"),
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
        string bonusHeader = LanguageSelectionUI.Localize("⚡ LAST HIT BONUS");
        GUI.Label(new Rect(bonusRect.x + 6f, bonusRect.y + 6f, bonusRect.width - 12f, 22f), bonusHeader, CreateHeadingStyle(12, new Color(1f, 0.88f, 0.3f), TextAnchor.MiddleCenter));

        string bonusText = LanguageSelectionUI.Localize(
            "• Protectors Kill:\n  Heals Sacred Tree +350 HP!\n  Boosts Growth Rate!\n\n• Destroyers Kill:\n  Deals -260 Damage to Tree!\n  Speeds Tree Fall!",
            ar: "• قتله الحماة:\n  شفاء الشجرة المقدسة +350 صحة!\n  تسريع معدل نمو الشجرة!\n\n• قتله المدمرون:\n  إلحاق -260 ضرر بالشجرة!\n  تسريع سقوط الشجرة!",
            fa: "• نابودی توسط محافظان:\n  درمان درخت مقدس به میزان +۳۵۰ سلامت!\n  افزایش سرعت رشد درخت!\n\n• نابودی توسط نابودکنندگان:\n  وارد کردن -۲۶۰ آسیب به درخت مقدس!\n  تسریع در سرنگونی درخت!",
            de: "• Tötung durch Beschützer:\n  Heilt den Baum um +350 HP!\n  Beschleunigt Wachstum!\n\n• Tötung durch Zerstörer:\n  Fügt dem Baum -260 Schaden zu!\n  Beschleunigt Baumfall!",
            it: "• Se ucciso dai Protettori:\n  Guarisce l'Albero di +350 HP!\n  Aumenta la crescita!\n\n• Se ucciso dai Distruttori:\n  Infligge -260 danni all'Albero!\n  Accelera la caduta!",
            tr: "• Koruyucular Katlederse:\n  Kutsal Ağacı +350 Can İyileştirir!\n  Büyümeyi hızlandırır!\n\n• Yok Ediciler Katlederse:\n  Ağaca -260 Hasar Verir!\n  Devrilmeyi hızlandırır!",
            ru: "• Убийство Защитниками:\n  Лечит Дерево на +350 HP!\n  Ускоряет рост!\n\n• Убийство Разрушителями:\n  Наносит -260 урона Дереву!\n  Приближает падение!",
            es: "• Si matan Protectores:\n  ¡Cura el Árbol +350 Salud!\n  ¡Acelera el crecimiento!\n\n• Si matan Destructores:\n  ¡Inflige -260 Daño al Árbol!\n  ¡Acelera la caída!",
            fr: "• Tué par Protecteurs :\n  Soigne l'Arbre +350 PV !\n  Accélère la croissance !\n\n• Tué par Destructeurs :\n  Inflige -260 Dégâts à l'Arbre !\n  Accélère sa chute !",
            zh: "• 守护者阵营击杀：\n  神圣古树立即恢复+350生命！\n  大幅提升古树成长速度！\n\n• 破坏者阵营击杀：\n  对神树直接造成-260重创伤害！\n  大幅加速古树倾覆！",
            ja: "• 守護者がラストヒット：\n  神聖な巨木が+350回復！\n  巨木の成長速度が加速！\n\n• 破壊者がラストヒット：\n  巨木に直接-260の大ダメージ！\n  神木の倒壊が劇的に加速！",
            hi: "• रक्षकों द्वारा मार:\n  पवित्र वृक्ष को +350 स्वास्थ्य!\n  विकास दर बढ़ाता है!\n\n• विध्वंसकों द्वारा मार:\n  पेड़ को -260 क्षति!\n  पेड़ गिरने की गति तेज!",
            pt: "• Se abatido por Protetores:\n  Cura a Árvore Sagrada em +350 PV!\n  Acelera o crescimento!\n\n• Se abatido por Destruidores:\n  Causa -260 de dano à Árvore!\n  Acelera a queda!"
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
        string protRoadmap = LanguageSelectionUI.Localize("🛡️ PROTECTOR ROADMAP TO VICTORY");
        GUI.Label(new Rect(left.x + 14f, left.y + 12f, left.width - 28f, 28f), protRoadmap, CreateHeadingStyle(15, new Color(0.35f, 0.92f, 1f)));

        string protectorSteps = LanguageSelectionUI.Localize(
            "STEP 1 — DEFEND & HEAL THE TREE:\nAttack the Sacred Tree trunk whenever it is damaged to restore its HP.\n\nSTEP 2 — SLAY ALL 3 BOSSES (LEVELS 1, 2, 3):\nClimb the platforms and defeat the 3 Bosses to accelerate tree growth and break the 3 Crystal Seals.\n\nSTEP 3 — GRAB THE SACRED CRYSTAL:\nWhen all 3 Bosses die, 'THE CRYSTAL IS READY!' Picking it up grants +35% Speed Boost and Sacred Feather Fall (0 fall damage)!\n\nSTEP 4 — DELIVER TO SACRED ALTAR (>2s BEFORE TREE FALLS):\nCarry the Crystal to the glowing Sacred Altar on the ground (Press [E] for Shockwave Blast) to WIN!\n*Must arrive >2.0s before Tree falls; simultaneous climax within 2s triggers a Peaceful Mutual Tie!",
            ar: "الخطوة 1 — الدفاع عن الشجرة وعلاجها:\nهاجم جذع الشجرة المقدسة كلما تضررت لاستعادة نقاط صحتها.\n\nالخطوة 2 — القضاء على الزعماء الثلاثة (مستويات 1، 2، 3):\nاصعد المنصات واهزم الزعماء لتسريع نمو الشجرة وفك أختام الكريستالة الثلاثة.\n\nالخطوة 3 — أخذ الكريستالة المقدسة:\nعند موت الزعماء الثلاثة تظهر رسالة: 'الكريستالة جاهزة!'. حمل الكريستالة يمنح +35% سرعة إضافية وسقوط الريشة المقدس (0 ضرر سقوط)!\n\nالخطوة 4 — إيصالها للمذبح المقدس (قبل سقوط الشجرة بـ >2ث):\nاحمل الكريستالة إلى المذبح المتوهج على الأرض (اضغط [E] لموجة صدمة) لتحقيق الفوز!\n*يجب أن تصل قبل سقوط الشجرة بأكثر من ثانيتين؛ تزامن الحدثين بفارق أقل من ثانيتين يحسم اللقاء تعادلاً سلمياً!",
            fa: "گام ۱ — دفاع و درمان درخت:\nهر زمان که به تنه درخت آسیب رسید، به آن حمله کنید تا سلامتش بازیابی شود.\n\nگام ۲ — نابودی هر ۳ غول (طبقات ۱، ۲، ۳):\nبه سکوها صعود کرده و ۳ غول را شکست دهید تا رشد تسریع شده و ۳ مهر بلور شکسته شوند.\n\nگام ۳ — برداشتن بلور مقدس:\nبا مرگ هر ۳ غول، 'بلور آماده است!' برداشتن آن +۳۵٪ افزایش سرعت و سقوط پر (۰ آسیب سقوط) می‌دهد!\n\nگام ۴ — تحویل به محراب مقدس (بیش از ۲ ثانیه قبل از سقوط درخت):\nبلور را به محراب برسانید تا برنده شوید! (رسیدن همزمان با سقوط درخت در کمتر از ۲ ثانیه مساوی و صلح است!)",
            de: "SCHRITT 1 — BAUM VERTEIDIGEN & HEILEN:\nGreift den Stamm an, um HP wiederherzustellen.\n\nSCHRITT 2 — ALLE 3 BOSSE TÖTEN:\nBesiegt die 3 Bosse, um das Wachstum zu beschleunigen und Siegel zu brechen.\n\nSCHRITT 3 — HEILIGEN KRISTALL NEHMEN:\nSind alle 3 Bosse tot, ist der Kristall bereit! Bringt +35% Tempo und Federfall!\n\nSCHRITT 4 — ZUM ALTAR BRINGEN (>2s VOR DEM BAUMFALL):\nTragt den Kristall zum Altar, um zu GEWINNEN! (Gleichzeitiges Eintreffen innerhalb 2s = Unentschieden!)",
            it: "PASSO 1 — DIFENDI E GUARISCI L'ALBERO:\nAttacca il tronco per ripristinare i suoi HP.\n\nPASSO 2 — UCCIDI I 3 BOSS (LIVELLI 1, 2, 3):\nSali sulle piattaforme ed elimina i 3 Boss per rompere i sigilli.\n\nPASSO 3 — PRENDI IL CRISTALLO SACRO:\nOttieni +35% Velocità e Caduta Piuma (0 danni da caduta)!\n\nPASSO 4 — CONSEGNA ALL'ALTARE (>2s PRIMA DELLA CADUTA):\nPorta il Cristallo all'Altare per VINCERE! (Simultaneo entro 2s = Pareggio pacifico!)",
            tr: "ADIM 1 — AĞACI SAVUN VE İYİLEŞTİR:\nCanını yenilemek için Kutsal Ağaç gövdesine vurun.\n\nADIM 2 — 3 PATRONU KATLET (SEVİYE 1, 2, 3):\nPlatformlara tırmanın ve 3 Kristal Mührünü kırmak için Boss'ları yenin.\n\nADIM 3 — KUTSAL KRİSTALİ AL:\nTaşıyıcı +%35 Hız ve Tüy Düşüşü kazanır!\n\nADIM 4 — KUTSAL SUNAĞA TAŞI (AĞAÇ YIKILMADAN >2 SN ÖNCE):\nKristali Sunaka taşıyarak KAZANIN! (2 saniye içinde eşzamanlılık Barışçıl Beraberliktir!)",
            ru: "ШАГ 1 — ЗАЩИТА И ЛЕЧЕНИЕ ДЕРЕВА:\nАтакуйте ствол Дерева для восстановления HP.\n\nШАГ 2 — ПОБЕДИТЕ 3 БОССОВ:\nСнимите 3 Печати.\n\nШАГ 3 — ВОЗЬМИТЕ КРИСТАЛЛ:\n+35% к скорости и Падение Перышком (0 урона)!\n\nШАГ 4 — ДОСТАВЬТЕ К АЛТАРЮ (>2 СЕК ДО ПАДЕНИЯ ДЕРЕВА):\nОтнесите Кристалл к Алтарю для ПОБЕДЫ! (При разнице менее 2 сек наступает Мирная Ничья!)",
            es: "PASO 1 — DEFENDER Y CURAR EL ÁRBOL:\nAtaca el tronco para restaurar su salud.\n\nPASO 2 — VENCER A LOS 3 JEFES:\nSube a las plataformas para romper sellos.\n\nPASO 3 — RECOGER EL CRISTAL SAGRADO:\n+35% velocidad y Caída de Pluma.\n\nPASO 4 — LLEVAR AL ALTAR (>2s ANTES DE CAER EL ÁRBOL):\n¡Llévalo al Altar para GANAR! (¡Si ambos caen en menos de 2s, es Empate Pacífico!)",
            fr: "ÉTAPE 1 — DÉFENDRE ET SOIGNER L'ARBRE :\nSoignez le tronc.\n\nÉTAPE 2 — VAINCRE LES 3 BOSS :\nBrisez les 3 sceaux.\n\nÉTAPE 3 — PRENDRE LE CRISTAL SACRÉ :\n+35% de vitesse et chute de plume.\n\nÉTAPE 4 — LIVRER À L'AUTEL (>2s AVANT LA CHUTE DE L'ARBRE) :\nPortez le Cristal à l'Autel pour GAGNER ! (Simultané en <2s = Match Nul Pacifique !)",
            zh: "第1步 — 守卫并治愈圣树：\n攻击树干为其补充生命值。\n\n第2步 — 击杀全部3位首领：\n斩杀首领破除3道封印。\n\n第3步 — 拾取神圣水晶：\n获得+35%移速与羽落无伤！\n\n第4步 — 护送至神圣祭坛（需在倒塌前>2秒）：\n送达祭坛即可夺冠！（相隔不足2秒触发和平平局！）",
            ja: "ステップ1 — 巨木の防衛と回復：\n樹幹を攻撃してHP回復。\n\nステップ2 — 3大ボスの討伐：\nボスを倒して封印解除。\n\nステップ3 — 聖なる水晶の奪回：\n移動速度+35%＆落下無傷！\n\nステップ4 — 祭壇へ奉納（伐倒より2秒以上前）：\n祭壇へ運んで勝利をつかめ！（2秒以内の同時発生は引き分け！）",
            hi: "चरण 1 — पेड़ की रक्षा और इलाज करें।\n\nचरण 2 — तीनों बॉस को मारकर मुहरें तोड़ें।\n\nचरण 3 — पवित्र क्रिस्टल लें (+35% गति)।\n\nचरण 4 — पेड़ गिरने से 2 सेकंड पहले वेदी तक पहुँचाएँ! (2 सेकंड में दोनों = शांतिपूर्ण बराबरी!)",
            pt: "PASSO 1 — DEFENDER E CURAR A ÁRVORE.\n\nPASSO 2 — DERROTAR OS 3 CHEFES.\n\nPASSO 3 — PEGAR O CRISTAL SACRADO (+35% velocidade).\n\nPASSO 4 — ENTREGAR NO ALTAR (>2s ANTES DA QUEDA):\nLeve ao Altar para VENCER! (Simultâneo em <2s = Empate Pacífico!)"
        );
        GUI.Label(new Rect(left.x + 16f, left.y + 48f, left.width - 32f, left.height - 60f), protectorSteps, CreateBodyStyle(12, Color.white));

        // Right Panel: Destroyer Step-by-Step Guide
        Rect right = new Rect(area.x + halfW + 18f, area.y, halfW, cardH);
        EnchantedNatureGUI.DrawLeafPlaque(right, new Color(0.25f, 0.07f, 0.06f, 0.96f), new Color(1f, 0.35f, 0.22f));
        string destRoadmap = LanguageSelectionUI.Localize("🔥 DESTROYER ROADMAP TO VICTORY");
        GUI.Label(new Rect(right.x + 14f, right.y + 12f, right.width - 28f, 28f), destRoadmap, CreateHeadingStyle(15, new Color(1f, 0.45f, 0.28f)));

        string destroyerSteps = LanguageSelectionUI.Localize(
            "STEP 1 — WEAKEN THE TREE & HUNT BOSSES:\nStrike Tree toward 42% limit, and climb Levels 1, 2, and 3 to slay the 3 Guardian Bosses.\n\nSTEP 2 — 10s DIVINE SHIELD COUNTDOWN:\nWhen Boss 3 falls, a 10-second Divine countdown begins before the shield shatters completely!\n\nSTEP 3 — INTERCEPT CRYSTAL CARRIERS:\nIf a Protector grabs the Crystal, hunt them down immediately so they drop it on the ground!\n\nSTEP 4 — TOPPLE TREE (>2s BEFORE CRYSTAL REACHES ALTAR):\nUnleash attacks on Tree! Higher platforms grant full damage until HP reaches 0 to WIN!\n*Must fell Tree >2.0s before Crystal consecration; simultaneous climax within 2s triggers a Peaceful Mutual Tie!",
            ar: "الخطوة 1 — إضعاف الشجرة وصيد الزعماء:\nاضربوا الشجرة حتى حد درع 42%، واصعدوا المستويات 1 و2 و3 للقضاء على الزعماء الثلاثة.\n\nالخطوة 2 — عد تنازلي 10 ثوانٍ لدرع الصمود:\nعند سقوط الزعيم الثالث، يبدأ عد تنازلي مدته 10 ثوانٍ قبل زوال الدرع بالكامل!\n\nالخطوة 3 — اعتراض حاملي الكريستالة:\nإذا أخذ أحد الحماة الكريستالة، لاحقوه فوراً واقضوا عليه لتسقط الكريستالة على الأرض!\n\nالخطوة 4 — إسقاط الشجرة (>2ث قبل إيصال الكريستالة للمذبح):\nهاجموا جذع الشجرة! المنصات العالية تمنح القوة الكاملة حتى تصل صحتها إلى 0 للفوز!\n*يجب إسقاط الشجرة قبل إيصال الكريستالة بأكثر من ثانيتين؛ تزامن الحدثين بفارق أقل من ثانيتين يحسم اللقاء تعادلاً سلمياً!",
            fa: "گام ۱ — تضعیف درخت و شکار غول‌ها:\nبه درخت ضربه بزنید تا به سقف سپر ۴۲٪ برسد و برای نابودی ۳ غول به طبقات صعود کنید.\n\nگام ۲ — شمارش ۱۰ ثانیه‌ای سپر مقدس:\nبا سقوط غول سوم، شمارش ۱۰ ثانیه‌ای تا خرد شدن کامل سپر آغاز می‌شود!\n\nگام ۳ — رهگیری حاملان بلور:\nاگر یک محافظ بلور را برداشت، سریعاً او را شکار کنید تا بلور روی زمین بیفتد!\n\nگام ۴ — واژگونی درخت (بیش از ۲ ثانیه قبل از تحویل بلور):\nتمام آتش خود را روی تنه بریزید تا سلامت به ۰ برسد و پیروز شوید! (همزمانی با تحویل بلور در کمتر از ۲ ثانیه مساوی و صلح است!)",
            de: "SCHRITT 1 — BAUM SCHWÄCHEN & BOSSE JAGEN:\nSchlagt den Baum auf das 42%-Limit und besiegt die 3 Bosse.\n\nSCHRITT 2 — 10S-COUNTDOWN DES SCHILDS:\nStirbt Boss 3, läuft ein 10s-Countdown vor dem vollständigen Schildbruch!\n\nSCHRITT 3 — KRISTALLTRÄGER ABFANGEN:\nNimmt ein Beschützer den Kristall, jagt ihn sofort!\n\nSCHRITT 4 — BAUM FÄLLEN (>2s VOR KRISTALL-WEIHE):\nGreift den Baum an bis 0 HP für den SIEG! (Eintreffen innerhalb 2s = Unentschieden!)",
            it: "PASSO 1 — INDEBOLISCI L'ALBERO E CACCIA I BOSS:\nColpisci l'Albero fino al 42% e abbatti i 3 Boss.\n\nPASSO 2 — CONTO ALLA ROVESCIA DI 10S DELLO SCUDO:\nCaduto il Boss 3, parte un conto di 10s prima del crollo totale!\n\nPASSO 3 — INTERCETTA I PORTATORI DEL CRISTALLO:\nSe un Protettore prende il Cristallo, abbattilo subito!\n\nPASSO 4 — ABBATTI IL SACRO ALBERO (>2s PRIMA DEL CRISTALLO):\nColpisci fino a 0 HP per VINCERE! (Simultaneo entro 2s = Pareggio pacifico!)",
            tr: "ADIM 1 — AĞACI ZAYIFLAT VE PATRONLARI AVLA:\nAğacı %42 Kalkan sınırına kadar vurun ve 3 Patronu avlayın.\n\nADIM 2 — 10 SN İLAHİ KALKAN GERİ SAYIMI:\nPatron 3 düştüğünde 10 saniyelik geri sayım başlar!\n\nADIM 3 — KRİSTAL TAŞIYICILARINI ENGELLE:\nKristal taşıyıcısını hemen avlayın!\n\nADIM 4 — AĞACI DEVİR (KRİSTAL SUNAĞA ULAŞMADAN >2 SN ÖNCE):\nCan 0 olunca KAZANIN! (2 saniye içinde eşzamanlılık Barışçıl Beraberliktir!)",
            ru: "ШАГ 1 — ОСЛАБЬТЕ ДЕРЕВО И ОХОТЬТЕСЬ НА БОССОВ:\nБейте Дерево до 42% и победите 3 Боссов.\n\nШАГ 2 — 10 СЕКУНД ДО СНЯТИЯ ЩИТА:\nОтсчет 10 секунд после гибели 3-го Босса!\n\nШАГ 3 — ПЕРЕХВАТЫВАЙТЕ КРИСТАЛЛ:\nВыбивайте Кристалл из рук Защитников!\n\nШАГ 4 — СРУБИТЕ ДЕРЕВО (>2 СЕК ДО ДОСТАВКИ КРИСТАЛЛА):\nСнизьте HP до 0 для ПОБЕДЫ! (При разнице менее 2 сек наступает Мирная Ничья!)",
            es: "PASO 1 — DEBILITAR EL ÁRBOL Y CAZAR JEFES:\nGolpea el Árbol hasta el 42% y abate a los 3 Jefes.\n\nPASO 2 — CUENTA ATRÁS DE 10S DEL ESCUDO:\nCuenta de 10s antes de que caiga el escudo.\n\nPASO 3 — INTERCEPTAR PORTADORES DEL CRISTAL:\n¡Caza al portador para que suelte el Cristal!\n\nPASO 4 — DERRIBAR EL ÁRBOL (>2s ANTES DEL CRISTAL):\n¡Lleva su salud al 0% para GANAR! (¡Si ambos caen en menos de 2s, es Empate Pacífico!)",
            fr: "ÉTAPE 1 — AFFAIBLIR L'ARBRE ET CHASSER LES BOSS :\nFrappez jusqu'à 42% et battez les 3 Boss.\n\nÉTAPE 2 — COMPTE À REBOURS DE 10S DU BOUCLIER :\nCompte à rebours avant la chute du bouclier !\n\nÉTAPE 3 — INTERCEPTER LES PORTEURS DU CRISTAL :\nÉliminez le porteur pour lui faire lâcher le Cristal !\n\nÉTAPE 4 — ABATTRE L'ARBRE (>2s AVANT LE CRISTAL) :\nRéduisez les PV à 0% pour GAGNER ! (Simultané en <2s = Match Nul Pacifique !)",
            zh: "第1步 — 削弱古树并围剿首领：\n猛攻树干直至42%护盾并击杀3名首领。\n\n第2步 — 10秒神圣破盾倒计时：\n击杀第3位首领开启破盾倒计时！\n\n第3步 — 全力截杀水晶持有者：\n击倒守护者截断水晶传递！\n\n第4步 — 伐倒神圣古树（需在水晶送达前>2秒）：\n生命归零即刻夺冠！（相隔不足2秒触发和平平局！）",
            ja: "ステップ1 — 巨木の弱体化とボス狩り：\nHP42%まで削り3大ボスを討て。\n\nステップ2 — シールド10秒破断カウント：\n第3ボス撃破でカウントダウン開始！\n\nステップ3 — 水晶運搬役の迎撃：\n水晶所持者を即座に迎撃せよ！\n\nステップ4 — 巨木の伐倒（水晶奉納より2秒以上前）：\nHPを0にして勝利！（2秒以内の同時発生は引き分け！）",
            hi: "चरण 1 — पेड़ को 42% तक कमजोर करें और 3 बॉस मारें।\n\nचरण 2 — 10 सेकंड ढाल उलटी गिनती।\n\nचरण 3 — क्रिस्टल वाहकों को रोकें।\n\nचरण 4 — क्रिस्टल पहुँचने से 2 सेकंड पहले पेड़ गिराएँ! (2 सेकंड में दोनों = शांतिपूर्ण बराबरी!)",
            pt: "PASSO 1 — ENFRAQUECER A ÁRVORE E CAÇAR OS CHEFES.\n\nPASSO 2 — CONTAGEM DE 10S DO ESCUDO DIVINO.\n\nPASSO 3 — INTERCEPTAR PORTADORES DO CRISTAL.\n\nPASSO 4 — DERRUBAR A ÁRVORE (>2s ANTES DO CRISTAL):\nZere a vida para VENCER! (Simultâneo em <2s = Empate Pacífico!)"
        );
        GUI.Label(new Rect(right.x + 16f, right.y + 48f, right.width - 32f, right.height - 60f), destroyerSteps, CreateBodyStyle(12, Color.white));

        // Dedicated Golden Plaque Banner: Dual Climax & Peaceful Mutual Tie (2-Second Window)
        float tieY = area.y + cardH + 12f;
        Rect tieRect = new Rect(area.x, tieY, area.width, 92f);
        EnchantedNatureGUI.DrawWoodPlate(tieRect, new Color(0.04f, 0.08f, 0.06f, 0.96f), new Color(1f, 0.85f, 0.35f, 0.95f));

        string tieHeader = LanguageSelectionUI.Localize(
            "🕊️ DUAL CLIMAX & PEACEFUL TIE RULE (2-SECOND MARGIN)",
            ar: "🕊️ قاعدة الحسم المزدوج والتعادل السلمي الشامل (فارق ثانيتين)",
            fa: "🕊️ قانون پایان همزمان و برابری صلح‌آمیز (فرصت ۲ ثانیه‌ای)",
            ru: "🕊️ ПРАВИЛО ДВОЙНОГО ФИНАЛА И МИРНОЙ НИЧЬЕЙ (2 СЕКУНДЫ)",
            es: "🕊️ ¡REGLA DE CLÍMAX DUAL Y EMPATE PACÍFICO (MARGEN DE 2 SEGUNDOS)!",
            fr: "🕊️ RÈGLE DU DOUBLE CLIMAX ET DU MATCH NUL DE PAIX (ÉCART DE 2 SECONDES)",
            de: "🕊️ REGEL DES PARALLELEN FINALES & FRIEDLICHES UNENTSCHIEDEN (2S-FENSTER)",
            it: "🕊️ REGOLA DEL DOPPIO CLIMAX E PAREGGIO PACIFICO (MARGINE DI 2 SECONDI)",
            tr: "🕊️ ÇİFTE FİNAL VE BARIŞÇIL BERABERLİK KURALI (2 SANİYE SINIRI)",
            hi: "🕊️ दोहरा चरम और शांतिपूर्ण बराबरी का नियम (2 सेकंड का अंतर)",
            zh: "🕊️ 双重决胜与和平平局法则（2秒绝对窗口）",
            ja: "🕊️ 同時決着と平和的引き分けルール（2秒のマージン）",
            pt: "🕊️ REGRA DO CLÍMAX DUPLO E EMPATE PACÍFICO (MARGEM DE 2 SEGUNDOS)"
        );
        GUI.Label(new Rect(tieRect.x + 12f, tieRect.y + 8f, tieRect.width - 24f, 24f), tieHeader, CreateHeadingStyle(13, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter));

        string tieBody = LanguageSelectionUI.Localize(
            "• TO WIN: Your team must achieve its victory objective MORE THAN 2 SECONDS before the enemy team completes theirs!\n• PEACEFUL TIE: If the Sacred Tree falls and the Sacred Crystal reaches the Altar within 2 seconds of each other, neither team wins — Celestial Peace is restored and both sides share a draw bounty!",
            ar: "• لتحقيق الفوز الحاسم: يجب على فريقك إتمام هدفه بفارق زمني يزيد عن ثانيتين كاملتين قبل أن ينجز الفريق الخصم هدفه!\n• التعادل السلمي: إذا سقطت الشجرة ووصلت الكريستالة للمذبح في نفس اللحظة (بفارق أقل من 2 ثانيتين)، يُحسم اللقاء تعادلاً سلمياً ويعم السلام دون فائز!",
            fa: "• برای پیروزی: تیم شما باید هدف خود را بیش از ۲ ثانیه قبل از تکمیل هدف تیم حریف محقق سازد!\n• برابری صلح‌آمیز: اگر سقوط درخت و رسیدن بلور به محراب با فاصله کمتر از ۲ ثانیه رخ دهد، هیچ تیمی پیروز نمی‌شود و صلح آسمانی برقرار می‌گردد!",
            ru: "• ДЛЯ ПОБЕДЫ: Ваша команда должна выполнить свою цель БОЛЕЕ ЧЕМ НА 2 СЕКУНДЫ раньше команды противника!\n• МИРНАЯ НИЧЬЯ: Если падение Дерева и доставка Кристалла произойдут с разницей менее 2 секунд, победителя нет — наступает Мир!",
            es: "• PARA GANAR: ¡Tu equipo debe lograr su objetivo con MÁS DE 2 SEGUNDOS de antelación respecto al rival!\n• EMPATE PACÍFICO: Si el Árbol cae y el Cristal llega al Altar en menos de 2 segundos, nadie gana — ¡reina la paz celestial!",
            fr: "• POUR GAGNER : Votre équipe doit accomplir son objectif PLUS DE 2 SECONDES avant l'équipe adverse !\n• MATCH NUL PACIFIQUE : Si l'Arbre s'effondre et le Cristal arrive en moins de 2s, personne ne gagne — la paix céleste est restaurée !",
            de: "• ZUM SIEG: Euer Team muss das Ziel MEHR ALS 2 SEKUNDEN vor dem gegnerischen Team abschließen!\n• FRIEDLICHES UNENTSCHIEDEN: Fallen Baum und Kristall innerhalb von 2 Sekunden, gewinnt niemand — Himmlischer Friede herrscht!",
            it: "• PER VINCERE: La tua squadra deve completare l'obiettivo PIÙ DI 2 SECONDI prima della squadra avversaria!\n• PAREGGIO PACIFICO: Se l'Albero cade e il Cristallo arriva entro 2 secondi, nessuno vince — la pace celeste è ripristinata!",
            tr: "• KAZANMAK İÇİN: Takımınız zafer hedefini rakip takımdan EN AZ 2 SANİYE ÖNCE tamamlamalıdır!\n• BARIŞÇIL BERABERLİK: Ağacın yıkılması ve Kristalin sunaka ulaşması 2 saniye içinde gerçekleşirse kimse kazanamaz — İlahi Barış ilan edilir!",
            hi: "• जीतने के लिए: आपकी टीम को विरोधी टीम से 2 सेकंड से अधिक पहले अपना लक्ष्य पूरा करना होगा!\n• शांतिपूर्ण बराबरी: यदि पेड़ गिरने और क्रिस्टल पहुँचने में 2 सेकंड से कम का अंतर हो, तो कोई नहीं जीतता — शांति बहाल होती है!",
            zh: "• 夺得绝对胜利：己方阵营必须比敌方阵营提前超过2秒达成决胜目标！\n• 和平平局机制：若神树倒塌与圣水晶送达在2秒内同时发生，双方均未获胜——天界重归和平，共享平局赏金！",
            ja: "• 勝利の絶対条件：敵チームが目的を果たすより2秒以上早く勝利条件を達成しなければならない！\n• 平和的引き分け：神木の倒壊と水晶の奉納が2秒以内で同時発生した場合、勝者は存在せず、天上の調和と平和が訪れる！",
            pt: "• PARA VENCER: Sua equipe deve atingir o objetivo MAIS DE 2 SEGUNDOS antes da equipe adversária!\n• EMPATE PACÍFICO: Se a Árvore cair e o Cristal chegar em menos de 2s, ninguém vence — a Paz Celestial é restaurada!"
        );
        GUI.Label(new Rect(tieRect.x + 14f, tieRect.y + 34f, tieRect.width - 28f, 54f), tieBody, CreateBodyStyle(12, Color.white));
    }

    private void DrawSlide4ControlsAndTips(Rect area)
    {
        string slide4Header = LanguageSelectionUI.Localize("⚔️ CONTROLS, MOVEMENT & PRO COMBAT SECRETS");
        GUI.Label(new Rect(area.x, area.y, area.width, 28f), slide4Header, CreateHeadingStyle(18, new Color(0.45f, 1f, 0.6f)));

        float colW = (area.width - 18f) / 2f;
        float cardH = 390f;
        Rect c1 = new Rect(area.x, area.y + 36f, colW, cardH);
        Rect c2 = new Rect(area.x + colW + 18f, area.y + 36f, colW, cardH);

        EnchantedNatureGUI.DrawLeafPlaque(c1, new Color(0.08f, 0.14f, 0.11f, 0.95f), new Color(0.35f, 0.88f, 0.5f));
        string kbTitle = LanguageSelectionUI.Localize("🎮 KEYBOARD & MOUSE CONTROLS");
        GUI.Label(new Rect(c1.x + 14f, c1.y + 12f, c1.width - 28f, 26f), kbTitle, CreateHeadingStyle(14, new Color(1f, 0.88f, 0.35f)));

        string controlsText = LanguageSelectionUI.Localize(
            "• [W, A, S, D] : Move Character  |  [Hold Shift] : Sprint / Fast Run\n\n• [Space (1x)] : Jump & Climb  |  [Space (2x)] : Double Jump!\n\n• [Left Click / F] : Attack / Fire Arrow  |  [R] : Ultimate Ability (Lv 5)\n\n• [E] : Shockwave Blast (Carrier)  |  [Q] : Drop Crystal\n\n• [Enter / C] : In-Game Team Chat (Send Message)\n\n• [Tab] : Match Scoreboard & Live Statistics\n\n• [B] : Open Emporium Arsenal Shop  |  [L] : Language Selection\n\n• [Esc / P] : Pause Menu & Settings  |  [H / V] : Guide / Video",
            ar: "• [W, A, S, D] : تحريك الشخصية  |  [Shift الأيسر] : الجري السريع\n\n• [مسافة (1x)] : القفز والصعود  |  [مسافة (2x)] : قفز مزدوج في الهواء!\n\n• [زر الفأرة الأيسر / F] : الهجوم ورمي السهام  |  [R] : القدرة الخارقة (مستوى 5)\n\n• [E] (لحامل الكريستالة) : موجة الصدمة  |  [Q] : إسقاط الكريستالة\n\n• [Enter / C] : الدردشة ومحادثة الفريق التكتيكية\n\n• [Tab] : لوحة نتائج المباراة والإحصائيات المباشرة\n\n• [B] : متجر الأسلحة والعتاد  |  [L] : تغيير لغة اللعبة\n\n• [Esc / P] : إيقاف مؤقت وقائمة الإعدادات  |  [H / V] : الدليل / الفيديو",
            ru: "• [W, A, S, D] : Движение  |  [Shift] : Спринт / Быстрый бег\n\n• [Пробел (1x)] : Прыжок  |  [Пробел (2x)] : Двойной прыжок!\n\n• [ЛКМ / F] : Атака / Стрельба  |  [R] : Суперспособность (Ур 5)\n\n• [E] : Ударная волна  |  [Q] : Сбросить кристалл\n\n• [Enter / C] : Командный чат (Отправка сообщений)\n\n• [Tab] : Таблица счета и статистика\n\n• [B] : Магазин оружия  |  [L] : Смена языка\n\n• [Esc / P] : Пауза и настройки  |  [H / V] : Гид / Видео",
            es: "• [W, A, S, D] : Mover  |  [Shift] : Esprintar\n\n• [Espacio (1x)] : Saltar  |  [Espacio (2x)] : ¡Doble salto!\n\n• [Clic Izq. / F] : Atacar / Disparar  |  [R] : Habilidad Definitiva (Niv 5)\n\n• [E] : Onda expansiva  |  [Q] : Soltar cristal\n\n• [Enter / C] : Chat de equipo en partida\n\n• [Tab] : Marcador y estadísticas en vivo\n\n• [B] : Tienda de armas  |  [L] : Cambiar idioma\n\n• [Esc / P] : Pausa y ajustes  |  [H / V] : Guía / Vídeo",
            fr: "• [W, A, S, D] : Déplacement  |  [Shift] : Sprint\n\n• [Espace (1x)] : Saut  |  [Espace (2x)] : Double saut !\n\n• [Clic Gauche / F] : Attaque / Tir  |  [R] : Capacité Ultime (Niv 5)\n\n• [E] : Onde de choc  |  [Q] : Lâcher cristal\n\n• [Enter / C] : Chat d'équipe en jeu\n\n• [Tab] : Tableau des scores & stats\n\n• [B] : Boutique d'armes  |  [L] : Changer de langue\n\n• [Esc / P] : Menu pause & options  |  [H / V] : Guide / Vidéo",
            fa: "• [W, A, S, D] : حرکت  |  [Shift] : دویدن سریع\n\n• [Space (۱ بار)] : پرش  |  [Space (۲ بار)] : پرش دوگانه!\n\n• [کلیک چپ / F] : حمله / تیراندازی  |  [R] : قدرت نهایی آلتیمیت (سطح ۵)\n\n• [E] : موج ضربه‌ای  |  [Q] : انداختن بلور\n\n• [Enter / C] : چت و گفتگوی تیمی در بازی\n\n• [Tab] : جدول نتایج و آمار زنده مسابقه\n\n• [B] : فروشگاه مهمات  |  [L] : تغییر زبان\n\n• [Esc / P] : توقف و تنظیمات  |  [H / V] : راهنما / ویدئو"
        );
        GUI.Label(new Rect(c1.x + 16f, c1.y + 46f, c1.width - 32f, c1.height - 56f), controlsText, CreateBodyStyle(11, Color.white));

        EnchantedNatureGUI.DrawLeafPlaque(c2, new Color(0.08f, 0.14f, 0.11f, 0.95f), new Color(0.95f, 0.78f, 0.25f));
        string proTipsTitle = LanguageSelectionUI.Localize("💡 VETERAN PRO TIPS");
        GUI.Label(new Rect(c2.x + 14f, c2.y + 12f, c2.width - 28f, 26f), proTipsTitle, CreateHeadingStyle(14, new Color(0.4f, 0.92f, 1f)));

        string proTipsText = LanguageSelectionUI.Localize(
            "1. HEIGHT SCALING DAMAGE MULTIPLIER:\nAttacking or healing the Tree scales dynamically with height:\n   • Ground: 15% (very low impact)  |  Level 1: 35% (light)\n   • Level 2: 60% (moderate impact)  |  Level 3 & Top: 100% FULL MAXIMUM POWER!\n\n2. FALL DAMAGE & FEATHER FALL:\nFalling >6m deals fall damage (12-38 HP). However, the Sacred Crystal carrier has Sacred Feather Fall (0 fall damage!) + 35% speed boost!\n\n3. 10S DIVINE SHIELD COUNTDOWN:\nWhen Boss 3 falls, a 10s Divine Countdown appears over the Tree Shield before shattering, giving Protectors a final defense window and Destroyers time to gather!",
            ar: "1. تدرج قوة التأثير حسب الارتفاع وعلو المنصة:\nمهاجمة أو شفاء الشجرة يتدرج بدقة حسب الارتفاع:\n   • الأرض: 15% (تأثير ضعيف جداً بالأسفل)  |  المستوى 1: 35% (تأثير خفيف)\n   • المستوى 2: 60% (تأثير متوسط)  |  المستوى 3 والقمة: 100% التأثير الكامل والأقصى!\n\n2. ضرر السقوط وسقوط الريشة المقدس:\nالسقوط من علو (>6 أمتار) يلحق ضرراً (12-38 صحة). لكن حامل الكريستالة محمي ببركة الريشة (0 ضرر سقوط!) مع زيادة سرعة +35%!\n\n3. عد تنازلي 10 ثوانٍ لدرع الصمود:\nعند موت الزعيم الثالث، يظهر عد تنازلي 10 ثوانٍ فوق درع الشجرة قبل زواله، ليعطي الحماة فرصة للصمود والمدمرين فرصة للتجمع!",
            fa: "۱. مقیاس ضریب آسیب با ارتفاع:\nحمله به درخت یا درمان آن با ارتفاع تغییر می‌کند:\n   • زمین: ۱۵٪ (اثر بسیار کم)  |  سطح ۱: ۴۲٪ (خفیف)\n   • سطح ۲: ۶۰٪ (متوسط)  |  سطح ۳ و قله: ۱۰۰٪ اثر کامل و بیشینه!\n\n۲. آسیب سقوط و پر مقدس:\nسقوط بیش از ۶ متر آسیب می‌زند (۱۲ تا ۳۸ سلامت). اما حامل بلور از موهبت پر مقدس (۰ آسیب سقوط!) و +۴۲٪ سرعت برخوردار است!\n\n۳. شمارش ۱۰ ثانیه‌ای سپر مقدس:\nهنگامی که غول ۳ کشته می‌شود، شمارش ۱۰ ثانیه‌ای روی سپر درخت چشمک می‌زند تا فرصت تجدید قوا به تیم‌ها بدهد!",
            de: "1. HÖHENMULTIPLIKATOR-SKALIERUNG:\nAngriff und Heilung des Baumes skalieren mit der Höhe:\n   • Boden: 15% (sehr gering)  |  Stufe 1: 35% (leicht)\n   • Stufe 2: 60% (mittel)  |  Stufe 3 & Spitze: 100% voller Maximaleffekt!\n\n2. FALLSCHADEN & FEDERFALL:\nStürze >6m verursachen Fallschaden (12-38 HP). Der Kristallträger hat jedoch Federfall (0 Schaden!) + 35% Tempo!\n\n3. 10S GÖTTLICHER SCHILD-COUNTDOWN:\nStirbt Boss 3, blinkt ein 10s-Countdown auf dem Schild vor dem Bruch auf!",
            it: "1. MOLTIPLICATORE PER ALTEZZA:\nAttacco e cura dell'Albero scalano con l'elevazione:\n   • Terra: 15% (molto debole)  |  Livello 1: 35% (leggero)\n   • Livello 2: 60% (moderato)  |  Livello 3 e Cima: 100% pieno effetto massimo!\n\n2. DANNI DA CADUTA E CADUTA PIUMA:\nCadere da >6m infligge 12-38 HP. Il portatore del Cristallo ha Caduta Piuma (0 danni!) + 35% velocità!\n\n3. CONTO ALLA ROVESCIA DI 10S DELLO SCUDO:\nMorto il Boss 3, un conto di 10s lampeggia sullo scudo prima del crollo totale!",
            tr: "1. YÜKSEKLİK ÇARPANI ÖLÇEĞİ:\nAğaca saldırmak veya iyileştirmek yüksekliğe göre değişir:\n   • Zemin: %15 etki (çok zayıf)  |  Seviye 1: %35 (hafif)\n   • Seviye 2: %60 (orta etki)  |  Seviye 3 ve Zirve: %100 tam ve azami etki!\n\n2. DÜŞME HASARI VE TÜY DÜŞÜŞÜ:\n>6m'den düşmek 12-38 Can hasar verir. Ancak Kristal taşıyıcısı Tüy Düşüşüne (0 hasar!) ve +%35 hıza sahiptir!\n\n3. 10 SN İLAHİ KALKAN GERİ SAYIMI:\nPatron 3 öldüğünde, kalkan kırılmadan önce 10 saniyelik geri sayım yanıp söner!",
            ru: "1. МНОЖИТЕЛЬ ВЫСОТЫ:\nАтака и лечение Дерева зависят от яруса:\n   • Земля: 15% (очень слабый)  |  Уровень 1: 35% (легкий)\n   • Уровень 2: 60% (средний)  |  Уровень 3 и вершина: 100% полный максимум!\n\n2. УРОН ОТ ПАДЕНИЯ И ПЕРО:\nПадение >6м наносит 12-38 HP урона. Но носитель Кристалла защищен Падением Перышком (0 урона!) и получает +35% к скорости!\n\n3. 10 СЕКУНД ДО СНЯТИЯ ЩИТА:\nПри гибели 3-го Босса щит держится еще 10 секунд с таймером, давая шанс перегруппироваться!",
            es: "1. MULTIPLICADOR POR ALTURA:\nAtacar o curar el Árbol escala según la altura:\n   • Suelo: 15% (muy bajo)  |  Nivel 1: 35% (ligero)\n   • Nivel 2: 60% (moderado)  |  Nivel 3 y Cima: 100% máximo completo!\n\n2. DAÑO POR CAÍDA Y PLUMA SAGRADA:\nCaer >6m inflige 12-38 daño. ¡El portador del Cristal tiene Caída de Pluma (0 daño!) y +35% velocidad!\n\n3. 10S DE ESCUDO DIVINO:\nAl morir el 3er Jefe, una cuenta de 10s aparece en el escudo antes de romperse por completo.",
            fr: "1. MULTIPLICATEUR DE HAUTEUR :\nL'impact sur l'Arbre varie selon l'élévation :\n   • Sol : 15% (très faible)  |  Niveau 1 : 35% (léger)\n   • Niveau 2 : 60% (modéré)  |  Niveau 3 et Sommet : 100% puissance maximale !\n\n2. DÉGÂTS DE CHUTE & PLUME SACRÉE :\nUne chute >6m inflige 12-38 PV. Mais le porteur du Cristal a la Chute de Plume (0 dégât !) et +35% de vitesse !\n\n3. COMPTE À REBOURS DE 10S DU BOUCLIER :\nÀ la mort du 3e Boss, un compte de 10s clignote sur le bouclier avant sa destruction totale !",
            zh: "1. 高度威力倍率加成法则：\n对神树造成伤害或恢复随站位高度梯级跃升：\n   • 地面基座：15% 效果（极弱）  |  第1层平台：42%（轻度）\n   • 第2层平台：60% 效果（中度）  |  第3层及树冠：100% 全额满额超强最大威力！\n\n2. 坠落伤害与神圣羽落：\n从高处（>6米）跌落受跌落伤害（12-38生命）。但水晶持有者享有神圣羽落（0跌落伤害！）及+35%移速光环！\n\n3. 10秒破盾终极倒计时：\n第3位首领阵亡时，神树护盾闪烁10秒倒计时，给予双方队伍绝地反攻或最后集结的最佳战略窗口！",
            ja: "1. 高度による威力倍率システム：\n巨木への攻撃および回復効果は交戦高度に応じて劇的に上昇：\n   • 地上：15%（極めて微弱）  |  階層1：42%（軽微）\n   • 階層2：60%（中程度）  |  階層3および頂上：100%の最大威力を発揮！\n\n2. 落下ダメージと聖なるフェザーフォール：\n6m以上の高所落下で12〜38HPのダメージ。ただし水晶運搬者はフェザーフォール（落下ダメージ完全無効！）と移動速度+35%を獲得！\n\n3. 神聖シールド10秒カウントダウン：\n第3ボス討伐時、シールド完全消失までの10秒タイマーが神木に点滅表示され、最終決戦の号砲となる！",
            hi: "1. ऊंचाई गुणक पैमाना:\nपेड़ पर हमला या इलाज ऊंचाई के साथ बढ़ता है:\n   • जमीन: 15% प्रभाव (बहुत कम)  |  स्तर 1: 35% (हल्का)\n   • स्तर 2: 60% प्रभाव (मध्यम)  |  स्तर 3 और शीर्ष: 100% पूरा अधिकतम प्रभाव!\n\n2. गिरावट क्षति और पंख गिरावट:\n>6 मीटर गिरने पर 12-38 क्षति होती है। हालांकि, क्रिस्टल वाहक को पंख गिरावट (0 क्षति!) और +35% गति मिलती है!\n\n3. 10 सेकंड दिव्य ढाल उलटी गिनती:\nजब बॉस 3 मरता है, तो ढाल टूटने से पहले 10 सेकंड की उलटी गिनती चमकती है!",
            pt: "1. MULTIPLICADOR POR ALTURA:\nAtaques ou cura na Árvore escalam com a altitude:\n   • Chão: 15% (muito fraco)  |  Nível 1: 35% (leve)\n   • Nível 2: 60% (médio)  |  Nível 3 e Topo: 100% de efeito máximo total!\n\n2. DANO DE QUEDA E QUEDA SUAVE:\nCair >6m causa 12-38 de dano. O portador do Cristal ganha Queda Suave (0 dano!) + 35% de velocidade!\n\n3. CONTAGEM DE 10S DO ESCUDO DIVINO:\nQuando o Chefe 3 cai, uma contagem de 10s pisca no escudo da Árvore antes da destruição total!"
        );
        GUI.Label(new Rect(c2.x + 16f, c2.y + 46f, c2.width - 32f, c2.height - 56f), proTipsText, CreateBodyStyle(12, Color.white));

        // Full-Width Plaque: Mobile & Tablet Touch Interface Notice
        float mobY = area.y + cardH + 46f;
        Rect mobRect = new Rect(area.x, mobY, area.width, 76f);
        EnchantedNatureGUI.DrawWoodPlate(mobRect, new Color(0.04f, 0.08f, 0.07f, 0.98f), new Color(0.35f, 0.92f, 0.55f, 0.9f));

        string mobHeader = LanguageSelectionUI.Localize(
            "📱 NOTICE FOR MOBILE & TABLET PLAYERS (TOUCH CONTROLS)",
            ar: "📱 تنبيه لمستخدمي الهواتف والأجهزة اللوحية (التحكم باللمس)",
            ru: "📱 ДЛЯ ИГРОКОВ НА СМАРТФОНАХ И ПЛАНШЕТАХ (СЕНСОР)",
            es: "📱 AVISO PARA MÓVILES Y TABLETS (CONTROLES TÁCTILES)",
            fr: "📱 JOUEURS SUR MOBILE & TABLETTE (COMMANDES TACTILES)",
            fa: "📱 نکته برای بازیکنان موبایل و تبلت (کنترل‌های لمسی)"
        );
        GUI.Label(new Rect(mobRect.x + 12f, mobRect.y + 6f, mobRect.width - 24f, 22f), mobHeader, CreateHeadingStyle(13, new Color(0.45f, 1f, 0.65f), TextAnchor.MiddleCenter));

        string mobBody = LanguageSelectionUI.Localize(
            "• Keyboard & mouse shortcuts above apply when playing on PC / Computer.\n• On Mobile Devices & Tablets, all actions (joystick movement, attack, jump, ultimate, team chat, emporium shop, and pause menu) are fully available as dedicated touch buttons directly on your screen!",
            ar: "• الأزرار واختصارات لوحة المفاتيح المذكورة أعلاه خاصة بمستخدمي أجهزة الكمبيوتر والحاسوب (PC).\n• أما إذا كنت تلعب عبر الهاتف المحمول أو التابلت، فستجد جميع أدوات التحكم (عصا الحركة، الهجوم، القفز، المهارة الخارقة، زر الدردشة، المتجر، والقائمة) متوفرة بالكامل كأزرار لمس مريحة ومباشرة على سطح شاشة هاتفك!",
            ru: "• Горячие клавиши выше предназначены для игры на ПК / Компьютере.\n• На смартфонах и планшетах все действия (джойстик движения, атака, прыжок, ультимейт, чат, магазин и пауза) доступны в виде сенсорных кнопок прямо на экране!",
            es: "• Los atajos de teclado y ratón anteriores son para jugar en PC / Ordenador.\n• ¡En móviles y tablets, todas las acciones (joystick, ataque, salto, definitiva, chat, tienda y pausa) están disponibles como botones táctiles en pantalla!",
            fr: "• Les raccourcis clavier et souris ci-dessus concernent le jeu sur PC.\n• Sur mobile et tablette, toutes les actions (joystick, attaque, saut, ultime, chat, boutique et pause) sont directement accessibles via les boutons tactiles à l'écran !",
            fa: "• کلیدهای فوق برای رایانه (PC) است.\n• در موبایل و تبلت، کلیه دکمه‌ها (جوستیک حرکت، حمله، پرش، آلتیمیت، چت، فروشگاه و منو) به صورت دکمه‌های لمسی روی صفحه در دسترس هستند!"
        );
        GUI.Label(new Rect(mobRect.x + 14f, mobRect.y + 28f, mobRect.width - 28f, 44f), mobBody, CreateBodyStyle(11, Color.white));
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
            fr: "GUIDE VIDÉO TACTIQUE — PARCOURS DE MISSION ÉTAPE PAR ÉTAPE",
            fa: "ویدئوی راهنمای تاکتیکی — گام‌به‌گام ماموریت"
        );
        GUI.Label(new Rect(mx, my + 10f, modalW, 26f), vGuideHeader, titleStyle);

        // Top Switcher Tabs: Protectors Video vs Destroyers Video
        float tabW = (modalW - 44f) / 2f;
        Rect tab1 = new Rect(mx + 18f, my + 40f, tabW, 34f);
        Rect tab2 = new Rect(mx + 26f + tabW, my + 40f, tabW, 34f);

        Color pCol = (videoTeamSelection == 0) ? new Color(0.35f, 0.92f, 1f) : new Color(0.18f, 0.38f, 0.45f);
        Color dCol = (videoTeamSelection == 1) ? new Color(1f, 0.48f, 0.28f) : new Color(0.45f, 0.22f, 0.18f);

        string pTabTitle = LanguageSelectionUI.Localize("🛡️ WATCH PROTECTORS VIDEO GUIDE", ar: "🛡️ شاهد فيديو دليل الحماة", ru: "🛡️ СМОТРЕТЬ ГИД ЗАЩИТНИКОВ", es: "🛡️ VER GUÍA DE PROTECTORES", fr: "🛡️ VOIR GUIDE PROTECTEURS", fa: "🛡️ تماشای راهنمای ویدئویی حامیان");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(tab1, pTabTitle, pCol, 12))
        {
            videoTeamSelection = 0;
            videoCurrentTime = 0f;
            isVideoPlaying = true;
        }

        string dTabTitle = LanguageSelectionUI.Localize("🔥 WATCH DESTROYERS VIDEO GUIDE", ar: "🔥 شاهد فيديو دليل المدمرين", ru: "🔥 СМОТРЕТЬ ГИД РАЗРУШИТЕЛЕЙ", es: "🔥 VER GUÍA DE DESTRUCTORES", fr: "🔥 VOIR GUIDE DESTRUCTEURS", fa: "🔥 تماشای راهنمای ویدئویی ویرانگران");
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
            LanguageSelectionUI.Localize("1. Spawn & Defend", ar: "1. الظهور والدفاع", ru: "1. Возрождение и защита", es: "1. Aparición y defensa", fr: "1. Apparition & Défense", fa: "۱. ظهور و دفاع"),
            LanguageSelectionUI.Localize("2. Slay 3 Bosses", ar: "2. صيد الزعماء 3", ru: "2. Убийство 3 Боссов", es: "2. Vencer a 3 Jefes", fr: "2. Vaincre 3 Boss", fa: "۲. شکار ۳ رئیس"),
            LanguageSelectionUI.Localize("3. Grab Crystal", ar: "3. أخذ الكريستالة", ru: "3. Взять Кристалл", es: "3. Tomar Cristal", fr: "3. Prendre Cristal", fa: "۳. تصاحب کریستال"),
            LanguageSelectionUI.Localize("4. Altar & Victory!", ar: "4. المذبح والنصر!", ru: "4. Алтарь и победа!", es: "4. ¡Altar y Victoria!", fr: "4. Autel & Victoire !", fa: "۴. محراب و پیروزی!")
        };
        string[] destroyerPhaseTitles = new string[]
        {
            LanguageSelectionUI.Localize("1. Strike Tree Base", ar: "1. ضرب جذع الشجرة", ru: "1. Атака базы дерева", es: "1. Atacar base del árbol", fr: "1. Frapper la base", fa: "۱. ضربه به ریشه درخت"),
            LanguageSelectionUI.Localize("2. Slay 3 Bosses", ar: "2. صيد الزعماء 3", ru: "2. Убийство 3 Боссов", es: "2. Vencer a 3 Jefes", fr: "2. Vaincre 3 Boss", fa: "۲. شکار ۳ رئیس"),
            LanguageSelectionUI.Localize("3. Shield Shatters", ar: "3. تحطم الدرع", ru: "3. Разрушение щита", es: "3. Escudo roto", fr: "3. Bouclier brisé", fa: "۳. شکستن سپر"),
            LanguageSelectionUI.Localize("4. Fell Tree & Win!", ar: "4. إسقاط الشجرة والفوز!", ru: "4. Падение дерева и победа!", es: "4. ¡Talar árbol y ganar!", fr: "4. Abattre l'arbre & Gagner !", fa: "۴. سقوط درخت و فتح!")
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
            ? LanguageSelectionUI.Localize("⏸ PAUSE", ar: "⏸ إيقاف مؤقت", ru: "⏸ ПАУЗА", es: "⏸ PAUSA", fr: "⏸ PAUSE", fa: "⏸ توقف")
            : LanguageSelectionUI.Localize("▶ PLAY", ar: "▶ تشغيل", ru: "▶ ПУСК", es: "▶ REPRODUCIR", fr: "▶ LECTURE", fa: "▶ پخش");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 121f, navY, 110f, 34f), playToggleLabel, new Color(1f, 0.85f, 0.3f), 12))
        {
            isVideoPlaying = !isVideoPlaying;
        }

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 239f, navY, 95f, 34f), "+5s ⏩", new Color(0.4f, 0.75f, 0.9f), 11))
        {
            videoCurrentTime = Mathf.Min(TotalVideoDuration - 0.5f, videoCurrentTime + 5f);
        }

        string replayLabel = LanguageSelectionUI.Localize("↺ REPLAY", ar: "↺ إعادة التشغيل", ru: "↺ ЗАНОВО", es: "↺ REPETIR", fr: "↺ REJOUER", fa: "↺ پخش مجدد");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 342f, navY, 100f, 34f), replayLabel, new Color(0.5f, 0.85f, 0.6f), 11))
        {
            videoCurrentTime = 0f;
            isVideoPlaying = true;
        }

        string backTextLabel = LanguageSelectionUI.Localize("📖 BACK TO TEXT GUIDE", ar: "📖 العودة للدليل المكتوب", ru: "📖 К ТЕКСТОВОМУ ГИДУ", es: "📖 VOLVER A LA GUÍA", fr: "📖 RETOUR AU GUIDE TEXTE", fa: "📖 بازگشت به راهنمای متنی");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + modalW - 380f, navY, 180f, 34f), backTextLabel, new Color(0.85f, 0.82f, 0.35f), 11))
        {
            isVideoGuideOpen = false;
        }

        string enterBattleLabel = LanguageSelectionUI.Localize("⚔️ ENTER BATTLE", ar: "⚔️ ادخل المعركة", ru: "⚔️ В БОЙ", es: "⚔️ ENTRAR A BATALLA", fr: "⚔️ ENTRER AU COMBAT", fa: "⚔️ ورود به نبرد");
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
        string liveFeedLabel = LanguageSelectionUI.Localize("LIVE TACTICAL FEED", ar: "بث تكتيكي مباشر", ru: "ПРЯМОЙ ТАКТИЧЕСКИЙ ЭФИР", es: "RETRANSMISIÓN TÁCTICA", fr: "FLUX TACTIQUE EN DIRECT", fa: "پخش زنده تاکتیکی");
        GUI.Label(new Rect(area.x + 28f, topHudY, 260f, 18f), $"{liveFeedLabel} • [{((int)curTime):D2}:{((int)((curTime % 1f) * 100)):D2} / 00:28]", CreateHeadingStyle(10, new Color(0.9f, 0.95f, 0.92f)));

        string roleTag = team == 0
            ? LanguageSelectionUI.Localize("🛡️ PROTECTOR OBJECTIVE", ar: "🛡️ هدف الحماة", ru: "🛡️ ЦЕЛЬ ЗАЩИТНИКОВ", es: "🛡️ OBJETIVO PROTECTOR", fr: "🛡️ OBJECTIF PROTECTEUR", fa: "🛡️ هدف حامیان")
            : LanguageSelectionUI.Localize("🔥 DESTROYER OBJECTIVE", ar: "🔥 هدف المدمرين", ru: "🔥 ЦЕЛЬ РАЗРУШИТЕЛЕЙ", es: "🔥 OBJETIVO DESTRUCTOR", fr: "🔥 OBJECTIF DESTRUCTEUR", fa: "🔥 هدف ویرانگران");
        string phaseWord = LanguageSelectionUI.Localize("PHASE", ar: "المرحلة", ru: "ФАЗА", es: "FASE", fr: "PHASE", fa: "مرحله");
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

                string p1TreeBar = LanguageSelectionUI.Localize("TREE: 100% (HEALED!)", ar: "الشجرة: 100% (تم الشفاء!)", ru: "ДЕРЕВО: 100% (ИЗЛЕЧЕНО!)", es: "ÁRBOL: 100% (¡CURADO!)", fr: "ARBRE : 100% (SOIGNÉ !)", fa: "درخت: ۱۰۰٪ (التیام یافت!)");
                DrawSimulatedHealthBar(new Rect(tx - 32f, ty - 42f, tw + 64f, 11f), 1.0f, new Color(0.25f, 0.95f, 0.45f), p1TreeBar);

                string p1CallTitle = LanguageSelectionUI.Localize("🛡️ DEFEND & HEAL SACRED TREE", ar: "🛡️ الدفاع عن الشجرة وعلاجها", ru: "🛡️ ЗАЩИТА И ЛЕЧЕНИЕ ДЕРЕВА", es: "🛡️ DEFENDER Y CURAR EL ÁRBOL", fr: "🛡️ DÉFENDRE ET SOIGNER L'ARBRE", fa: "🛡️ دفاع و درمان درخت مقدس");
                string p1CallBody = LanguageSelectionUI.Localize(
                    "• Your attacks on the Tree HEAL its HP!\n• Protect the base from enemy Destroyers.",
                    ar: "• ضرباتكم على الشجرة تشفي نقاط صحتها!\n• احموا قاعدة الشجرة من غارات المدمرين.",
                    ru: "• Ваши удары по Дереву ЛЕЧАТ его!\n• Защищайте базу от Разрушителей.",
                    es: "• ¡Tus ataques al Árbol lo CURAN!\n• Protege la base de los Destructores.",
                    fr: "• Vos attaques SOIGNENT l'Arbre !\n• Protégez la base des Destructeurs.",
                    fa: "• حملات شما به درخت، سلامت آن را درمان می‌کند!\n• از ریشه درخت در برابر ویرانگران دفاع کنید."
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
                string p2CallTitle = LanguageSelectionUI.Localize($"👑 SLAY 3 BOSSES ({defeated}/3 SLAIN)", ar: $"👑 صيد الزعماء 3 ({defeated}/3 مهزوم)", ru: $"👑 УБЕЙТЕ 3 БОССОВ ({defeated}/3)", es: $"👑 VENCE A 3 JEFES ({defeated}/3)", fr: $"👑 VAINCRE 3 BOSS ({defeated}/3)", fa: $"👑 نابودی ۳ رئیس ({defeated}/۳ شکست خوردند)");
                string p2CallBody = LanguageSelectionUI.Localize(
                    "• Climb L1, L2, L3 platforms to eliminate Bosses.\n• Last Hit heals Tree +350 HP & breaks Seals!",
                    ar: "• اصعدوا المنصات 1 و2 و3 للقضاء على الزعماء.\n• الضربة القاضية تشفي الشجرة +350 وتفك الأختام!",
                    ru: "• Поднимитесь на платформы для битвы с Боссами.\n• Добивание лечит Дерево на +350 HP и снимает Печати!",
                    es: "• Sube a las plataformas para eliminar a los Jefes.\n• ¡El último golpe cura +350 Salud y rompe Sellos!",
                    fr: "• Montez sur les plateformes pour battre les Boss.\n• Le coup de grâce soigne +350 PV et brise les Sceaux !",
                    fa: "• به سکوهای ۱، ۲ و ۳ صعود کنید تا رؤسا را نابود سازید.\n• ضربه آخر ۳۵۰+ واحد سلامت درخت را التیام می‌دهد و مهرها را می‌شکند!"
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
                string crystalLabel = LanguageSelectionUI.Localize("💎 CRYSTAL", ar: "💎 الكريستال", ru: "💎 КРИСТАЛЛ", es: "💎 CRISTAL", fr: "💎 CRISTAL", fa: "💎 کریستال");
                GUI.Label(new Rect(crysRect.x - 20f, crysRect.y - 15f, 62f, 15f), crystalLabel, CreateHeadingStyle(9, cyan, TextAnchor.MiddleCenter));

                DrawPlayerToken(tx + tw / 2f - 8f, crysY + 20f, "💎", cyan, carrierTag);

                string p3CallTitle = LanguageSelectionUI.Localize("💎 SACRED CRYSTAL UNSEALED!", ar: "💎 فُكت أختام الكريستالة المقدسة!", ru: "💎 КРИСТАЛЛ РАСПЕЧАТАН!", es: "💎 ¡CRISTAL SAGRADO DESBLOQUEADO!", fr: "💎 CRISTAL SACRÉ DÉVERROUILLÉ !", fa: "💎 کریستال مقدس آزاد شد!");
                string p3CallBody = LanguageSelectionUI.Localize(
                    "• All 3 Bosses are dead! The Crystal unlocks!\n• Climb to the top canopy and pick it up.",
                    ar: "• مات الزعماء الثلاثة! أصبحت الكريستالة حرة!\n• اصعدوا إلى القمة والتقطوها.",
                    ru: "• Все 3 Босса мертвы! Кристалл открыт!\n• Поднимитесь на вершину и возьмите его.",
                    es: "• ¡Los 3 Jefes han muerto! El Cristal está libre.\n• Sube a la cima y recógelo.",
                    fr: "• Les 3 Boss sont morts ! Le Cristal est libre !\n• Montez au sommet pour le récupérer.",
                    fa: "• هر ۳ رئیس کشته شدند! کریستال آزاد شد!\n• به اوج تاج درخت بروید تا آن را بردارید."
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
                    ? LanguageSelectionUI.Localize("🏆 VICTORY ACHIEVED! 🏆", ar: "🏆 تحقق النصر العظيم! 🏆", ru: "🏆 ПОБЕДА ДОСТИГНУТА! 🏆", es: "🏆 ¡VICTORIA CONSEGUIDA! 🏆", fr: "🏆 VICTOIRE ÉCLATANTE ! 🏆", fa: "🏆 پیروزی قطعی محقق شد! 🏆")
                    : LanguageSelectionUI.Localize("🏃 DELIVER TO SACRED ALTAR", ar: "🏃 الإيصال إلى المذبح المقدس", ru: "🏃 ДОСТАВЬТЕ К АЛТАРЮ", es: "🏃 LLEVAR AL ALTAR SAGRADO", fr: "🏃 LIVRER À L'AUTEL SACRÉ", fa: "🏃 رساندن به محراب مقدس");

                string p4CallBody = runProgress >= 0.95f
                    ? LanguageSelectionUI.Localize(
                        "• Crystal consecrated >2s before Tree fell! Protectors WIN!\n• (Arriving within 2s of Tree fall triggers a Peaceful Tie!)",
                        ar: "• وصلت الكريستالة للمذبح قبل سقوط الشجرة بأكثر من ثانيتين! فاز الحماة!\n• (الوصول بالتزامن مع سقوط الشجرة بفارق أقل من ثانيتين يحسم اللقاء تعادلاً سلمياً!)",
                        ru: "• Кристалл освящен более чем за 2 сек до падения Дерева! Защитники ПОБЕДИЛИ!\n• (Разница менее 2 сек приводит к Ничьей!)",
                        es: "• ¡Cristal en el Altar >2s antes de caer el Árbol! ¡Protectores GANAN!\n• (¡Simultáneo en <2s es Empate Pacífico!)",
                        fr: "• Cristal consacré >2s avant la chute de l'Arbre ! Les Protecteurs GAGNENT !\n• (Simultané en <2s = Match Nul Pacifique !)",
                        fa: "• کریستال بیش از ۲ ثانیه قبل از سقوط درخت به محراب رسید! پیروزی محافظان!\n• (همزمانی در کمتر از ۲ ثانیه مساوی و صلح است!)"
                    )
                    : LanguageSelectionUI.Localize(
                        "• Carry Crystal to Altar on the ground!\n• Press [E] for Shockwave to blast enemies away!",
                        ar: "• احمل الكريستالة نحو المذبح على الأرض!\n• اضغط [E] لإطلاق موجة صدمة تبعد الأعداء!",
                        ru: "• Несите Кристалл к Алтарю на земле!\n• Нажмите [E] для волны, отбрасывающей врагов!",
                        es: "• ¡Lleva el Cristal al Altar en el suelo!\n• ¡Pulsa [E] para onda expansiva!",
                        fr: "• Portez le Cristal à l'Autel au sol !\n• Appuyez sur [E] pour onde de choc !",
                        fa: "• کریستال را به محراب مقدس روی زمین برسانید!\n• کلید [E] را بزنید تا موج انفجاری دشمنان را پراکنده کند!"
                    );

                DrawSimulationCallout(new Rect(area.x + area.width * 0.44f, area.y + 36f, 280f, 54f),
                    p4CallTitle, p4CallBody, runProgress >= 0.95f ? new Color(1f, 0.88f, 0.3f) : cyan);
                break;
        }
    }

    private void DrawDestroyerSimulation(Rect area, int phase, float phaseProgress, float gy, float tx, float tw, float ty, float p1y, float p2y, float p3y, float ax)
    {
        Color crimson = new Color(1f, 0.38f, 0.22f);
        string destroyerTag = LanguageSelectionUI.Localize("DESTROYER", ar: "مدمر", ru: "РАЗРУШИТЕЛЬ", es: "DESTRUCTOR", fr: "DESTRUCTEUR", fa: "ویرانگر");

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
                string shieldedTag = LanguageSelectionUI.Localize("(SHIELDED)", ar: "(محمية بالدرع)", ru: "(ПОД ЩИТОМ)", es: "(CON ESCUDO)", fr: "(SOUS BOUCLIER)", fa: "(سپر فعال)");
                DrawSimulatedHealthBar(new Rect(tx - 32f, ty - 42f, tw + 64f, 11f), treePct, new Color(1f, 0.35f, 0.2f), $"TREE: {(int)(treePct * 100)}% {shieldedTag}");

                float shieldAlpha = Mathf.Clamp01((1.0f - treePct) / 0.85f);
                GUI.color = new Color(1f, 0.85f, 0.25f, 0.22f * shieldAlpha + Mathf.PingPong(Time.time * 2f, 0.12f));
                GUI.DrawTexture(new Rect(tx - 8f, ty - 6f, tw + 16f, gy - ty + 10f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                string d1CallTitle = LanguageSelectionUI.Localize("🔥 STRIKE TREE TO 15% SHIELD", ar: "🔥 ضرب الشجرة حتى درع 15%", ru: "🔥 АТАКА ДЕРЕВА ДО ЩИТА 15%", es: "🔥 ATACAR ÁRBOL HASTA EL 15%", fr: "🔥 FRAPPER L'ARBRE JUSQU'À 15%", fa: "🔥 تهاجم به درخت تا ۱۵٪ فعال‌سازی سپر");
                string d1CallBody = LanguageSelectionUI.Localize(
                    "• Attack Tree trunk down to 15% HP limit.\n• Arcane Shield protects Tree until 3 Bosses die!",
                    ar: "• هاجموا جذع الشجرة حتى حد 15% صحة.\n• الدرع السحري يحمي الشجرة حتى موت الزعماء 3!",
                    ru: "• Бейте по стволу до предела 15% здоровья.\n• Магический Щит защищает Дерево до гибели 3 Боссов!",
                    es: "• Ataca el tronco hasta el límite de 15%.\n• ¡El Escudo protege el Árbol hasta que mueran 3 Jefes!",
                    fr: "• Frappez le tronc jusqu'à 15% de PV.\n• Le Bouclier protège l'Arbre tant que les 3 Boss vivent !",
                    fa: "• با تبر خود بر تنه درخت ضربه بزنید!\n• در ۱۵٪ سلامت، سپر جادویی از درخت محافظت می‌کند."
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
                string d2CallTitle = LanguageSelectionUI.Localize($"👑 SLAY ALL 3 BOSSES ({defeated}/3)", ar: $"👑 صيد الزعماء الثلاثة ({defeated}/3)", ru: $"👑 УБЕЙТЕ ВСЕХ 3 БОССОВ ({defeated}/3)", es: $"👑 VENCE A LOS 3 JEFES ({defeated}/3)", fr: $"👑 VAINCRE LES 3 BOSS ({defeated}/3)", fa: $"👑 شکار و نابودی ۳ رئیس ({defeated}/۳)");
                string d2CallBody = LanguageSelectionUI.Localize(
                    "• Must kill all 3 Bosses to break the Tree Shield!\n• Last Hit bonus deals -260 Damage to Tree!",
                    ar: "• يجب قتل الزعماء الثلاثة لكسر درع الشجرة!\n• الضربة القاضية تلحق -260 ضرر إضافي بالشجرة!",
                    ru: "• Нужно убить 3 Боссов, чтобы разбить Щит!\n• Добивание наносит -260 урона Дереву!",
                    es: "• ¡Mata a los 3 Jefes para romper el Escudo!\n• ¡El último golpe inflige -260 daño al Árbol!",
                    fr: "• Tuez les 3 Boss pour briser le Bouclier !\n• Le coup de grâce inflige -260 dégâts à l'Arbre !",
                    fa: "• هر ۳ رئیس را از پای درآورید تا سپر درخت بشکند!\n• ضربه آخر ۲۶۰- آسیب به درخت وارد می‌کند!"
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

                string shatteredLabel = LanguageSelectionUI.Localize("TREE: 15% (SHIELD SHATTERED!)", ar: "الشجرة: 15% (تحطم الدرع!)", ru: "ДЕРЕВО: 15% (ЩИТ РАЗБИТ!)", es: "ÁRBOL: 15% (¡ESCUDO ROTO!)", fr: "ARBRE : 15% (BOUCLIER BRISÉ !)", fa: "درخت: ۱۵٪ (سپر جادویی خرد شد!)");
                DrawSimulatedHealthBar(new Rect(tx - 32f, ty - 42f, tw + 64f, 11f), 0.15f, crimson, shatteredLabel);

                string d3CallTitle = LanguageSelectionUI.Localize("⚡ ARCANE SHIELD SHATTERED!", ar: "⚡ تحطم الدرع السحري!", ru: "⚡ МАГИЧЕСКИЙ ЩИТ РАЗБИТ!", es: "⚡ ¡ESCUDO ARCANO ROTO!", fr: "⚡ BOUCLIER ARCANIQUE BRISÉ !", fa: "⚡ سپر کهن خرد شد!");
                string d3CallBody = LanguageSelectionUI.Localize(
                    "• Boss 3 is dead! 'YOU CAN NOW DESTROY TREE!'\n• The Sacred Tree is now 100% vulnerable!",
                    ar: "• مات الزعيم 3! 'يمكنكم الآن تدمير الشجرة!'\n• الشجرة المقدسة الآن معرضة للهجوم 100%!",
                    ru: "• Босс 3 мертв! 'ТЕПЕРЬ МОЖНО СРУБИТЬ ДЕРЕВО!'\n• Священное Дерево уязвимо на 100%!",
                    es: "• ¡Murió el Jefe 3! '¡YA PUEDES DESTRUIR EL ÁRBOL!'\n• ¡El Árbol Sagrado es 100% vulnerable!",
                    fr: "• Boss 3 vaincu ! 'DÉTRUISEZ L'ARBRE MAINTENANT !'\n• L'Arbre Sacré est 100% vulnérable !",
                    fa: "• رئیس سوم ساقط شد! 'اکنون می‌توانید درخت را نابود کنید!'\n• درخت مقدس اکنون ۱۰۰٪ آسیب‌پذیر است!"
                );
                DrawSimulationCallout(new Rect(area.x + area.width * 0.44f, area.y + 36f, 275f, 52f), d3CallTitle, d3CallBody, new Color(1f, 0.88f, 0.3f));
                break;

            case 3: // Phase 4: Full Tree Siege & Collapse
                float attackTreePct = Mathf.Lerp(0.15f, 0.0f, Mathf.Clamp01(phaseProgress * 1.3f));
                string treeHpLabel = LanguageSelectionUI.Localize($"TREE: {(int)(attackTreePct * 100)}% HP", ar: $"الشجرة: {(int)(attackTreePct * 100)}% صحة", ru: $"ДЕРЕВО: {(int)(attackTreePct * 100)}% HP", es: $"ÁRBOL: {(int)(attackTreePct * 100)}% SALUD", fr: $"ARBRE : {(int)(attackTreePct * 100)}% PV", fa: $"درخت: {(int)(attackTreePct * 100)}٪ سلامت");
                DrawSimulatedHealthBar(new Rect(tx - 32f, ty - 42f, tw + 64f, 11f), attackTreePct, crimson, treeHpLabel);

                DrawPlayerToken(tx + tw + 10f, p2y - 24f, "🔥", crimson, destroyerTag);

                string dmg300Label = LanguageSelectionUI.Localize("⚡ 300% DMG!", ar: "⚡ 300% ضرر!", ru: "⚡ 300% УРОНА!", es: "⚡ ¡300% DAÑO!", fr: "⚡ 300% DÉGÂTS !", fa: "⚡ ۳۰۰٪ آسیب!");
                GUI.Label(new Rect(tx - 8f, p2y - 15f, 110f, 16f), dmg300Label, CreateHeadingStyle(9, new Color(1f, 0.9f, 0.3f)));

                bool treeCollapsed = attackTreePct <= 0.01f;
                string d4CallTitle = treeCollapsed
                    ? LanguageSelectionUI.Localize("🔥 DESTROYER VICTORY! 🔥", ar: "🔥 نصر المدمرين الساحق! 🔥", ru: "🔥 ПОБЕДА РАЗРУШИТЕЛЕЙ! 🔥", es: "🔥 ¡VICTORIA DESTRUCTORES! 🔥", fr: "🔥 VICTOIRE DESTRUCTEURS ! 🔥", fa: "🔥 پیروزی مقتدرانه ویرانگران! 🔥")
                    : LanguageSelectionUI.Localize("💥 FULL TREE SIEGE", ar: "💥 حصار الشجرة الشامل", ru: "💥 ШТУРМ ДЕРЕВА", es: "💥 ASEDIO AL ÁRBOL", fr: "💥 SIÈGE DE L'ARBRE", fa: "💥 محاصره همه‌جانبه درخت");

                string d4CallBody = treeCollapsed
                    ? LanguageSelectionUI.Localize(
                        "• Tree toppled >2s before Crystal consecrated! Destroyers WIN!\n• (Falling within 2s of Crystal consecration triggers a Peaceful Tie!)",
                        ar: "• سقطت الشجرة قبل وصول الكريستالة للمذبح بأكثر من ثانيتين! فاز المدمرون!\n• (سقوط الشجرة مع وصول الكريستالة بفارق أقل من ثانيتين يحسم اللقاء تعادلاً سلمياً!)",
                        ru: "• Дерево срублено более чем за 2 сек до доставки Кристалла! Разрушители ПОБЕДИЛИ!\n• (Разница менее 2 сек приводит к Ничьей!)",
                        es: "• ¡Árbol caído >2s antes de llegar el Cristal! ¡Destructores GANAN!\n• (¡Simultáneo en <2s es Empate Pacífico!)",
                        fr: "• Arbre abattu >2s avant l'arrivée du Cristal ! Les Destructeurs GAGNENT !\n• (Simultané en <2s = Match Nul Pacifique !)",
                        fa: "• درخت بیش از ۲ ثانیه قبل از رسیدن کریستال واژگون شد! پیروزی ویرانگران!\n• (همزمانی در کمتر از ۲ ثانیه مساوی و صلح است!)"
                    )
                    : LanguageSelectionUI.Localize(
                        "• Attack Tree from higher platforms for 300% dmg!\n• Bring Tree HP to 0% to fell the Beanstalk and WIN!",
                        ar: "• هاجموا الشجرة من المنصات العالية لضرر 300%!\n• خفضوا صحة الشجرة إلى 0% لإسقاطها والفوز!",
                        ru: "• Бейте Дерево с верхних платформ (300% урона)!\n• Снизьте здоровье до 0% для победы!",
                        es: "• ¡Ataca el Árbol desde lo alto para 300% daño!\n• ¡Lleva su salud al 0% para derribarlo y GANAR!",
                        fr: "• Attaquez depuis les hauteurs pour 300% dégâts !\n• Réduisez les PV à 0% pour abattre l'Arbre et GAGNER !",
                        fa: "• از سکوهای بالا ضربه بزنید تا ۳۰۰٪ آسیب وارد شود!\n• سلامت درخت را به صفر برسانید تا پیروز شوید!"
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
                        ar: "المرحلة 1: الظهور في الغابة والدفاع عن الشجرة المقدسة",
                        ru: "ФАЗА 1: ПОЯВЛЕНИЕ В ЛЕСУ И ЗАЩИТА ДЕРЕВА",
                        es: "FASE 1: APARICIÓN Y DEFENSA DEL ÁRBOL SAGRADO",
                        fr: "PHASE 1 : APPARITION ET DÉFENSE DE L'ARBRE SACRÉ",
                        fa: "فاز ۱: حضور در بیشه و دفاع از درخت مقدس"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Spawn at the base. Guard the Sacred Beanstalk trunk. Your attacks HEAL the Tree instead of damaging it!",
                        ar: "تظهرون عند القاعدة. احموا جذع نبتة الفاصولياء. ضرباتكم تشفي الشجرة بدلاً من إيذائها!",
                        ru: "Появитесь у подножия. Защищайте ствол. Ваши удары ЛЕЧАТ Дерево, а не наносят урон!",
                        es: "Aparece en la base. Custodia el tronco. ¡Tus ataques CURAN el Árbol en vez de dañarlo!",
                        fr: "Apparaissez à la base. Gardez le tronc. Vos attaques SOIGNENT l'Arbre au lieu de l'abîmer !",
                        fa: "در پایگاه ظاهر شوید. از تنه درخت محافظت کنید. حملات شما درخت را درمان می‌کند نه آسیب!"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Stay near the Tree base to counter early Destroyer attacks while your team gathers.",
                        ar: "💡 نصيحة تكتيكية: ابقوا قرب قاعدة الشجرة لصد هجمات المدمرين المبكرة ريثما يجتمع فريقكم.",
                        ru: "💡 СОВЕТ: Держитесь у подножия, чтобы отбивать ранние атаки Разрушителей, пока собирается команда.",
                        es: "💡 CONSEJO TÁCTICO: Permanece cerca de la base para frenar ataques tempranos mientras se agrupa el equipo.",
                        fr: "💡 CONSEIL TACTIQUE : Restez près de la base pour contrer les premières attaques le temps que l'équipe s'unisse.",
                        fa: "💡 نکته تاکتیکی: نزدیک تنه درخت بمانید تا حملات اولیه ویرانگران را مهار کنید."
                    );
                    break;

                case 1:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 2: CLIMB PLATFORMS & SLAY THE 3 GUARDIAN BOSSES",
                        ar: "المرحلة 2: صعود المنصات والقضاء على الزعماء الحراس الثلاثة",
                        ru: "ФАЗА 2: ПОДЪЕМ НА ПЛАТФОРМЫ И УБИЙСТВО 3 БОССОВ",
                        es: "FASE 2: SUBIR PLATAFORMAS Y ELIMINAR A LOS 3 JEFES",
                        fr: "PHASE 2 : MONTER SUR LES PLATEFORMES ET ÉLIMINER LES 3 BOSS",
                        fa: "فاز ۲: صعود به سکوها و نابودی ۳ رئیس نگهبان"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Climb to Levels 1, 2, and 3. Slay Forest Goliath, Inferno Titan, and Ancient Overlord to break the 3 Seals!",
                        ar: "اصعدوا إلى المستويات 1 و2 و3. اقضوا على جالوت وعملاق الجحيم والحاكم القديم لفك الأختام الثلاثة!",
                        ru: "Поднимитесь на Уровни 1, 2 и 3. Победите Голиафа, Титана и Владыку, чтобы снять 3 Печати!",
                        es: "Sube a los Niveles 1, 2 y 3. ¡Vence a Goliat, al Titán y al Soberano para romper los 3 Sellos!",
                        fr: "Montez aux Niveaux 1, 2 et 3. Battez Goliath, le Titan et le Seigneur pour briser les 3 Sceaux !",
                        fa: "به سطوح ۱، ۲ و ۳ صعود کنید. جالوت جنگل، تایتان جهنمی و فرمانروای باستانی را برای شکستن ۳ مهر نابود کنید!"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Landing the last hit on any Boss heals the Sacred Tree by +350 HP and boosts team XP!",
                        ar: "💡 نصيحة تكتيكية: الضربة القاضية على أي زعيم تشفي الشجرة +350 صحة وتمنح نقاط خبرة للفريق!",
                        ru: "💡 СОВЕТ: Добивающий удар по любому Боссу лечит Дерево на +350 HP и дает опыт всей команде!",
                        es: "💡 CONSEJO TÁCTICO: ¡Asestar el último golpe a un Jefe cura +350 Salud al Árbol y da experiencia al equipo!",
                        fr: "💡 CONSEIL TACTIQUE : Le coup de grâce sur un Boss soigne l'Arbre de +350 PV et booste l'XP de l'équipe !",
                        fa: "💡 نکته تاکتیکی: ضربه نهایی به هر رئیس، ۳۵۰+ واحد درخت را درمان کرده و امتیاز تیم را بالا می‌برد!"
                    );
                    break;

                case 2:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 3: 'THE CRYSTAL IS READY!' — ASCEND & GRAB CRYSTAL",
                        ar: "المرحلة 3: 'الكريستالة جاهزة!' — الصعود وأخذ الكريستالة",
                        ru: "ФАЗА 3: 'КРИСТАЛЛ ГОТОВ!' — ПОДНИМИТЕСЬ И ВОЗЬМИТЕ ЕГО",
                        es: "FASE 3: '¡EL CRISTAL ESTÁ LISTO!' — SUBIR Y RECOGERLO",
                        fr: "PHASE 3 : 'LE CRISTAL EST PRÊT !' — MONTER ET RÉCUPÉRER",
                        fa: "فاز ۳: 'کریستال آماده است!' — صعود به اوج و تصاحب کریستال"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "When Boss 3 falls, the Sacred Crystal unseals at the top canopy! Ascend to the peak and collect it.",
                        ar: "عند سقوط الزعيم 3، تتحرر الكريستالة في القمة! اصعدوا للأعلى والتقطوها.",
                        ru: "Когда падет Босс 3, Кристалл освободится на самой вершине! Поднимитесь и заберите его.",
                        es: "¡Al caer el 3er Jefe, el Cristal se desbloquea en la cima! Sube a lo más alto y recógelo.",
                        fr: "À la mort du Boss 3, le Cristal se libère à la cime ! Montez au sommet pour le récupérer.",
                        fa: "با سقوط رئیس سوم، کریستال مقدس در بالاترین شاخه آزاد می‌شود! به اوج بروید و آن را بردارید."
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Once picked up, the carrier glows brightly. Escort the carrier to protect them from ambush!",
                        ar: "💡 نصيحة تكتيكية: يتوهج حامل الكريستالة بقوة. رافقوا الحامل واحموه من كمائن الأعداء!",
                        ru: "💡 СОВЕТ: Носитель ярко светится. Сопровождайте его, чтобы защитить от вражеских засад!",
                        es: "💡 CONSEJO TÁCTICO: El portador brilla con fuerza. ¡Acompáñalo para defenderlo de emboscadas enemigas!",
                        fr: "💡 CONSEIL TACTIQUE : Le porteur brille intensément. Escortez-le pour le protéger des embuscades !",
                        fa: "💡 نکته تاکتیکی: حامل کریستال می‌درخشد. او را اسکورت کنید تا از کمین دشمنان در امان بماند!"
                    );
                    break;

                default:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 4: DELIVER CRYSTAL TO ALTAR & USE SHOCKWAVE FOR VICTORY",
                        ar: "المرحلة 4: إيصال الكريستالة للمذبح واستخدام موجة الصدمة للفوز",
                        ru: "ФАЗА 4: ДОСТАВКА КРИСТАЛЛА К АЛТАРЮ И УДАРНАЯ ВОЛНА",
                        es: "FASE 4: LLEVAR EL CRISTAL AL ALTAR Y USAR ONDA EXPANSIVA",
                        fr: "PHASE 4 : LIVRER LE CRISTAL À L'AUTEL ET ONDE DE CHOC",
                        fa: "فاز ۴: رساندن کریستال به محراب و شلیک موج شوک برای پیروزی"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Carry Crystal down to the Sacred Altar on the ground. Press [E] for Shockwave to clear enemies. Touch Altar to WIN!",
                        ar: "احملوا الكريستالة للمذبح على الأرض. اضغطوا [E] لإطلاق موجة صدمة تبعد الأعداء. المسوا المذبح للفوز!",
                        ru: "Отнесите Кристалл к Алтарю на земле. Нажмите [E] для волны, разбрасывающей врагов. Коснитесь Алтаря для победы!",
                        es: "Lleva el Cristal al Altar en el suelo. Pulsa [E] para onda expansiva y apartar enemigos. ¡Toca el Altar para GANAR!",
                        fr: "Portez le Cristal à l'Autel au sol. Appuyez sur [E] pour onde de choc et repousser les ennemis. Touchez l'Autel pour GAGNER !",
                        fa: "کریستال را به محراب مقدس روی زمین برسانید. [E] را برای ایجاد موج انفجاری بزنید. محراب را لمس کنید تا برنده شوید!"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Consecrate Crystal before Tree falls (>2s gap)! If both happen within 2s, it's a Peaceful Tie!",
                        ar: "💡 نصيحة تكتيكية: أوصلوا الكريستالة قبل سقوط الشجرة بأكثر من ثانيتين! فإذا سقطت الشجرة ووُضعت الكريستالة بفارق أقل من ثانيتين يُحسم اللقاء تعادلاً سلمياً ولا يفوز أحد!",
                        ru: "💡 СОВЕТ: Освятите Кристалл до падения Дерева (>2 сек)! Разница менее 2 сек — Мирная Ничья!",
                        es: "💡 CONSEJO TÁCTICO: ¡Consagra el Cristal antes de caer el Árbol (>2s)! ¡En menos de 2s es Empate Pacífico!",
                        fr: "💡 CONSEIL TACTIQUE : Consacrez le Cristal avant la chute de l'Arbre (>2s) ! En moins de 2s = Match Nul Pacifique !",
                        fa: "💡 نکته تاکتیکی: بلور را قبل از سقوط درخت برسانید! همزمانی زیر ۲ ثانیه مساوی است!"
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
                        ar: "المرحلة 1: مهاجمة الشجرة المقدسة حتى حد درع 15%",
                        ru: "ФАЗА 1: АТАКА ДЕРЕВА ДО ПОРОГА ЩИТА 15%",
                        es: "FASE 1: ATACAR EL ÁRBOL SAGRADO HASTA EL 15%",
                        fr: "PHASE 1 : ATTAQUER L'ARBRE SACRÉ JUSQU'À 15%",
                        fa: "فاز ۱: حمله به درخت مقدس تا مرز ۱۵٪ و فعال شدن سپر"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Assault the Sacred Tree trunk to lower its health to 15%. The Arcane Shield then locks the Tree until Bosses die.",
                        ar: "هاجموا جذع الشجرة المقدسة لخفض صحتها إلى 15%. سيقفل الدرع السحري الشجرة بعدها حتى موت الزعماء.",
                        ru: "Штурмуйте ствол, чтобы снизить здоровье до 15%. Затем Щит заблокирует урон до гибели Боссов.",
                        es: "Asalta el tronco para bajar su salud al 15%. El Escudo Arcano bloqueará el Árbol hasta que mueran los Jefes.",
                        fr: "Assaillez le tronc pour baisser sa vie à 15%. Le Bouclier Arcanique bloquera l'Arbre jusqu'à la mort des Boss.",
                        fa: "به تنه درخت یورش برید تا سلامتش به ۱۵٪ برسد. سپس سپر جادویی تا زمان نابودی رؤسا درخت را قفل می‌کند."
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Focus fire on the trunk early on, but do not waste attacks once the Arcane Shield activates.",
                        ar: "💡 نصيحة تكتيكية: ركزوا هجماتكم على الجذع مبكراً، لكن لا تضيعوا ضرباتكم بعد تفعيل الدرع السحري.",
                        ru: "💡 СОВЕТ: Сфокусируйте огонь на стволе вначале, но не тратьте удары впустую после активации Щита.",
                        es: "💡 CONSEJO TÁCTICO: Concéntrate en el tronco al inicio, pero no gastes ataques una vez activo el Escudo.",
                        fr: "💡 CONSEIL TACTIQUE : Concentrez vos tirs sur le tronc au début, mais ne gaspillez pas d'attaques sous bouclier.",
                        fa: "💡 نکته تاکتیکی: در ابتدا آتش حملات را روی تنه متمرکز کنید، اما پس از فعال شدن سپر ضربات را هدر ندهید."
                    );
                    break;

                case 1:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 2: CLIMB PLATFORMS & HUNT DOWN ALL 3 BOSSES",
                        ar: "المرحلة 2: صعود المنصات ومطاردة الزعماء الثلاثة والقضاء عليهم",
                        ru: "ФАЗА 2: ПОДЪЕМ НА ПЛАТФОРМЫ И ОХОТА НА 3 БОССОВ",
                        es: "FASE 2: SUBIR A LAS PLATAFORMAS Y CAZAR A LOS 3 JEFES",
                        fr: "PHASE 2 : MONTER SUR LES PLATEFORMES ET CHASSER LES 3 BOSS",
                        fa: "فاز ۲: صعود به سکوها و شکار هر ۳ رئیس"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "The Tree is shielded! Climb Levels 1, 2, and 3 to eliminate the 3 Bosses. Each kill gives -260 Tree damage!",
                        ar: "الشجرة محمية بالدرع! اصعدوا المستويات 1 و2 و3 للقضاء على الزعماء. كل زعيم مقتول يلحق -260 ضرر بالشجرة!",
                        ru: "Дерево под Щитом! Поднимитесь на Уровни 1, 2 и 3 для ликвидации 3 Боссов. Каждое убийство дает -260 урона Дереву!",
                        es: "¡El Árbol tiene escudo! Sube a los Niveles 1, 2 y 3 para abatir a los 3 Jefes. ¡Cada baja inflige -260 daño al Árbol!",
                        fr: "L'Arbre est protégé ! Montez aux Niveaux 1, 2 et 3 pour vaincre les 3 Boss. Chaque élimination inflige -260 dégâts !",
                        fa: "درخت دارای سپر است! به سطوح ۱، ۲ و ۳ رفته و ۳ رئیس را نابود کنید. هر قتل ۲۶۰- آسیب مستقیم به درخت وارد می‌کند!"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Each Boss slain by Destroyers unleashes a shockwave that inflicts -260 damage to the Tree!",
                        ar: "💡 نصيحة تكتيكية: كل زعيم يقضي عليه المدمرون يطلق موجة ارتجاج تلحق -260 ضرر مباشر بالشجرة!",
                        ru: "💡 СОВЕТ: Каждый Босс, убитый Разрушителями, вызывает ударную волну, наносящую -260 урона Дереву!",
                        es: "💡 CONSEJO TÁCTICO: ¡Cada Jefe abatido por Destructores desata una onda que daña al Árbol en -260 de Salud!",
                        fr: "💡 CONSEIL TACTIQUE : Chaque Boss abattu par les Destructeurs libère une onde qui inflige -260 dégâts à l'Arbre !",
                        fa: "💡 نکته تاکتیکی: نابودی هر ۳ رئیس، سپر جادویی را برای همیشه می‌شکند و درخت را بی‌پناه می‌سازد!"
                    );
                    break;

                case 2:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 3: 'YOU CAN NOW DESTROY THE TREE!' — SHIELD SHATTERS",
                        ar: "المرحلة 3: 'يمكنكم الآن تدمير الشجرة!' — تحطم الدرع",
                        ru: "ФАЗА 3: 'ТЕПЕРЬ МОЖНО СРУБИТЬ ДЕРЕВО!' — ЩИТ РАЗБИТ",
                        es: "FASE 3: '¡YA PUEDES DESTRUIR EL ÁRBOL!' — EL ESCUDO SE ROMPE",
                        fr: "PHASE 3 : 'DÉTRUISEZ L'ARBRE MAINTENANT !' — BOUCLIER BRISÉ",
                        fa: "فاز ۳: 'اکنون می‌توانید درخت را نابود کنید!' — خرد شدن سپر"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Boss 3 falls! A giant announcement confirms the Arcane Shield is shattered. The Tree is now 100% vulnerable!",
                        ar: "سقط الزعيم 3! إعلان ضخم يؤكد تحطم الدرع السحري. الشجرة المقدسة الآن عرضة للدمار بنسبة 100%!",
                        ru: "Босс 3 повержен! Сообщение подтверждает падение Щита. Дерево теперь уязвимо на 100%!",
                        es: "¡Cae el 3er Jefe! Un anuncio gigante confirma la rotura del Escudo. ¡El Árbol es ahora 100% vulnerable!",
                        fr: "Le Boss 3 tombe ! Une annonce géante confirme que le Bouclier est brisé. L'Arbre est 100% vulnérable !",
                        fa: "رئیس سوم شکست خورد! اعلان بزرگ شکستن سپر را تأیید می‌کند. درخت اکنون ۱۰۰٪ آسیب‌پذیر است!"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Intercept any Protectors trying to reach the Crystal while the rest of your team storms the Tree!",
                        ar: "💡 نصيحة تكتيكية: اعترضوا أي حامٍ يحاول الوصول للكريستالة بينما يقتحم بقية فريقكم جذع الشجرة!",
                        ru: "💡 СОВЕТ: Перехватывайте Защитников, идущих к Кристаллу, пока остальная команда штурмует Дерево!",
                        es: "💡 CONSEJO TÁCTICO: ¡Intercepta a los Protectores que busquen el Cristal mientras tu equipo asalta el Árbol!",
                        fr: "💡 CONSEIL TACTIQUE : Interceptez les Protecteurs visant le Cristal pendant que le reste de l'équipe charge !",
                        fa: "💡 نکته تاکتیکی: جلوی هر حامی که به سمت کریستال می‌رود را بگیرید تا بقیه اعضا به درخت هجوم برند!"
                    );
                    break;

                default:
                    enHead = LanguageSelectionUI.Localize(
                        "PHASE 4: FULL TREE SIEGE & TOPPLE THE BEANSTALK FOR VICTORY",
                        ar: "المرحلة 4: الحصار الشامل وإسقاط نبتة الفاصولياء لتحقيق النصر",
                        ru: "ФАЗА 4: ПОЛНЫЙ ШТУРМ И ПАДЕНИЕ ДЕРЕВА ДЛЯ ПОБЕДЫ",
                        es: "FASE 4: ASEDIO TOTAL Y DERRIBO DEL TALLO PARA GANAR",
                        fr: "PHASE 4 : SIÈGE TOTAL ET CHUTE DU HARICOT POUR LA VICTOIRE",
                        fa: "فاز ۴: محاصره کامل درخت و قطع کردن ساقه غول‌پیکر برای فتح نهایی"
                    );
                    enBody = LanguageSelectionUI.Localize(
                        "Attack Tree trunk (higher platforms grant full 100% damage, while ground deals 20%)! Reduce Tree HP to 0 to WIN!",
                        ar: "هاجموا جذع الشجرة (المنصات العالية تمنح حتى 300% ضرر مضاعف)! اخفضوا صحة الشجرة إلى 0 لإسقاطها والفوز!",
                        ru: "Бейте по стволу (верхние платформы дают до 300% урона)! Снизьте здоровье до 0, чтобы свалить Дерево и ПОБЕДИТЬ!",
                        es: "¡Ataca el tronco (plataformas altas dan hasta 300% daño)! ¡Baja su salud a 0 para derribarlo y GANAR!",
                        fr: "Attaquez le tronc (les plateformes hautes donnent jusqu'à 300% dégâts) ! Baissez les PV à 0 pour GAGNER !",
                        fa: "به تنه حمله کنید (سکوهای بالا ۱۰۰٪ آسیب کامل و زمین ۲۰٪ وارد می‌کنند)! سلامت درخت را به صفر برسانید تا برنده شوید!"
                    );
                    enTip = LanguageSelectionUI.Localize(
                        "💡 TACTICAL TIP: Fell Tree before Crystal is delivered (>2s gap)! If both happen within 2s, it's a Peaceful Tie!",
                        ar: "💡 نصيحة تكتيكية: أسقطوا الشجرة قبل وصول الكريستالة للمذبح بأكثر من ثانيتين! فإذا سقطت الشجرة ووُضعت الكريستالة بفارق أقل من ثانيتين يُحسم اللقاء تعادلاً سلمياً ولا يفوز أحد!",
                        ru: "💡 СОВЕТ: Срубите Дерево до доставки Кристалла (>2 сек)! Разница менее 2 сек — Мирная Ничья!",
                        es: "💡 CONSEJO TÁCTICO: ¡Derriba el Árbol antes de que llegue el Cristal (>2s)! ¡En menos de 2s es Empate Pacífico!",
                        fr: "💡 CONSEIL TACTIQUE : Abattez l'Arbre avant la livraison du Cristal (>2s) ! En moins de 2s = Match Nul Pacifique !",
                        fa: "💡 نکته تاکتیکی: درخت را قبل از رسیدن بلور ساقط کنید! همزمانی زیر ۲ ثانیه مساوی است!"
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
