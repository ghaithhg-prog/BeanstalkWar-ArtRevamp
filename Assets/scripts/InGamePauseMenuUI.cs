using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// InGamePauseMenuUI:
/// AAA-Style In-Game Pause & Settings Menu accessible during active gameplay via ESC key or top HUD button.
/// Provides:
/// 1. Resume Game ("مواصلة اللعب")
/// 2. Change Language ("تغيير لغة اللعبة")
/// 3. Audio & Volume controls (Live slider + Mute toggle)
/// 4. Leave Match & Return to Grove/Main Menu ("العودة إلى الغابة والبداية")
/// 5. Exit Game to Desktop ("الخروج من اللعبة")
/// Fully localized across all 10 supported languages with Arabic RTL support.
/// </summary>
public class InGamePauseMenuUI : MonoBehaviour
{
    public static InGamePauseMenuUI Instance { get; private set; }
    public static bool IsPauseMenuOpen => Instance != null && Instance.isMenuOpen;

    private bool isMenuOpen = false;
    private float currentVolume = 1.0f;
    private bool isMuted = false;
    private float preMuteVolume = 1.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("InGamePauseMenuUI");
            go.AddComponent<InGamePauseMenuUI>();
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

        currentVolume = AudioListener.volume > 0.001f ? AudioListener.volume : 1.0f;
        preMuteVolume = currentVolume;
        isMuted = AudioListener.volume <= 0.001f;
    }

    void Update()
    {
        // Don't intercept ESC if language modal or briefing is already handling it
        if (LanguageSelectionUI.IsLanguageModalOpen) return;
        if (GameIntroBriefingUI.IsBriefingOpen) return;
        if (CharacterSelectionUI.IsSelectionOpen) return;
        if (LeaderboardUI.IsLeaderboardOpen) return;
        if (FriendsUI.IsFriendsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen)
            {
                ClosePauseMenu();
            }
            else
            {
                // Only allow pause menu if in active match or character is active
                if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())
    
                {
                    OpenPauseMenu();
                }
            }
        }
    }

    public void OpenPauseMenu()
    {
        isMenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ClosePauseMenu()
    {
        isMenuOpen = false;
    }

    public void TogglePauseMenu()
    {
        if (isMenuOpen) ClosePauseMenu();
        else OpenPauseMenu();
    }

    void OnGUI()
    {
        if (MatchEndStatsUI.IsGameOverActive) return;

        // 1. In-Game Pause & Settings Corner HUD Button (always visible during active gameplay when pause menu is closed)
        if (!isMenuOpen && 
            !LanguageSelectionUI.IsLanguageModalOpen && 
            !GameIntroBriefingUI.IsBriefingOpen && 
            !CharacterSelectionUI.IsSelectionOpen &&
            !PlayerProgressionAndShopUI.IsShopOpen)
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())

            {
                GUI.depth = -100;
                DrawPauseCornerButton();
            }
        }

        // 2. Full In-Game Pause & Settings Modal
        if (isMenuOpen)
        {
            GUI.depth = -450;
            DrawPauseModal();
        }
    }

    private void DrawPauseCornerButton()
    {
        float btnW = 145f;
        float btnH = 34f;
        float x = 16f;
        float y = 14f;

        string btnText = LanguageSelectionUI.Localize(
            "⚙️ PAUSE (ESC)",
            ar: "⚙️ إيقاف (ESC)",
            fa: "⚙️ مکث (ESC)",
            de: "⚙️ PAUSE (ESC)",
            it: "⚙️ PAUSA (ESC)",
            tr: "⚙️ DURAKLAT (ESC)",
            ru: "⚙️ ПАУЗА (ESC)",
            fr: "⚙️ PAUSE (ESC)",
            es: "⚙️ PAUSA (ESC)",
            hi: "⚙️ विराम (ESC)",
            zh: "⚙️ 暂停 (ESC)",
            ja: "⚙️ 一時停止 (ESC)",
            pt: "⚙️ PAUSA (ESC)"
        );

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(x, y, btnW, btnH), btnText, new Color(0.35f, 0.85f, 0.95f), 11))
        {
            OpenPauseMenu();
        }
    }

    private Vector2 pauseScrollPos = Vector2.zero;

    private void DrawPauseModal()
    {
        // Full screen dimmed emerald vignette
        GUI.color = new Color(0.02f, 0.05f, 0.03f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(560f, Screen.width - 24f);
        float modalH = Mathf.Min(680f, Screen.height - 20f);
        float mx = (Screen.width - modalW) / 2f;
        float my = (Screen.height - modalH) / 2f;

        // Leaf Plaque Frame
        EnchantedNatureGUI.DrawLeafPlaque(
            new Rect(mx, my, modalW, modalH),
            new Color(0.07f, 0.12f, 0.09f, 0.98f),
            new Color(0.35f, 0.88f, 0.55f)
        );

        // Header Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);

        string headerText = LanguageSelectionUI.Localize(
            "❦ PAUSE & SETTINGS ❦",
            ar: "❦ إيقاف مؤقت وإعدادات اللعبة ❦",
            fa: "❦ توقف و تنظیمات بازی ❦",
            de: "❦ PAUSE & EINSTELLUNGEN ❦",
            it: "❦ PAUSA E IMPOSTAZIONI ❦",
            tr: "❦ DURAKLATMA VE AYARLAR ❦",
            ru: "❦ ПАУЗА И НАСТРОЙКИ ❦",
            fr: "❦ PAUSE ET PARAMÈTRES ❦",
            es: "❦ PAUSA Y AJUSTES ❦",
            hi: "❦ विराम और सेटिंग्स ❦",
            zh: "❦ 暂停与设置 ❦",
            ja: "❦ 一時停止と設定 ❦",
            pt: "❦ PAUSA E CONFIGURAÇÕES ❦"
        );
        GUI.Label(new Rect(mx, my + 14f, modalW, 26f), headerText, titleStyle);

        // Subtitle & Match Stake Status
        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 11;
        subStyle.fontStyle = FontStyle.Italic;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.6f, 0.88f, 0.7f);

        string stakeInfo = PlayerProfile.Instance != null 
            ? $"🌰 {PlayerProfile.Instance.Coins}  |  " + LanguageSelectionUI.Localize("Stake: -60 (Win: +350)", ar: "الرهان: -60 (الفوز: +350)", fa: "هزینه: -۶۰ (پیروزی: +۳۵۰)", fr: "Mise: -60 (Gain: +350)")
            : "";
        GUI.Label(new Rect(mx + 20f, my + 40f, modalW - 40f, 20f), stakeInfo, subStyle);

        // Scroll Area for all settings
        Rect scrollOuter = new Rect(mx + 16f, my + 64f, modalW - 32f, modalH - 80f);
        float itemW = scrollOuter.width - 24f;
        float totalVirtualH = 680f;
        Rect scrollInner = new Rect(0, 0, itemW, totalVirtualH);

        pauseScrollPos = GUI.BeginScrollView(scrollOuter, pauseScrollPos, scrollInner);

        float curY = 4f;

        // 1. Resume Game Button
        string resumeText = LanguageSelectionUI.Localize(
            "▶ RESUME MATCH (مواصلة اللعب)",
            ar: "▶ مواصلة اللعب (متابعة المعركة)",
            fa: "▶ ادامه بازی (ادامه نبرد)",
            de: "▶ WEITERSPIELEN (ESC)",
            it: "▶ RIPRENDI PARTITA (ESC)",
            tr: "▶ MAÇA DEVAM ET (ESC)",
            ru: "▶ ПРОДОЛЖИТЬ БОЙ (ESC)",
            fr: "▶ REPRENDRE LA PARTIE (ESC)",
            es: "▶ CONTINUAR PARTIDA (ESC)",
            hi: "▶ खेल जारी रखें (ESC)",
            zh: "▶ 继续战斗 (ESC)",
            ja: "▶ 戦闘を再開 (ESC)",
            pt: "▶ CONTINUAR PARTIDA (ESC)"
        );

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 44f), resumeText, new Color(0.25f, 0.95f, 0.45f), 13))
        {
            ClosePauseMenu();
        }

        curY += 50f;

        // 2. Guide & Boss Seals Button
        string guideBtnText = LanguageSelectionUI.Localize(
            "📖 HOW TO PLAY & BOSS SEALS GUIDE",
            ar: "📖 دليل اللعبة وتوجيهات أختام الزعماء",
            fa: "📖 راهنمای بازی و مهرهای غول‌ها",
            ru: "📖 РУКОВОДСТВО ПО ИГРЕ И БОССАМ",
            fr: "📖 GUIDE DU JEU & SCEAUX DES BOSS",
            es: "📖 GUÍA DEL JUEGO Y SELLOS DE JEFES"
        );
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 40f), guideBtnText, EnchantedNatureGUI.AntiqueGoldBright, 11))
        {
            ClosePauseMenu();
            if (GameIntroBriefingUI.Instance != null)
            {
                GameIntroBriefingUI.Instance.OpenBriefing();
            }
        }

        curY += 46f;

        // 3. Change Language Button
        string langBtnText = LanguageSelectionUI.Localize(
            "🌐 GAME LANGUAGE: " + LanguageSelectionUI.CurrentLanguageCode.ToUpperInvariant(),
            ar: "🌐 تغيير لغة اللعبة (العربية)",
            fa: "🌐 تغییر زبان بازی (فارسی)",
            de: "🌐 SPRACHE ÄNDERN",
            it: "🌐 CAMBIA LINGUA",
            tr: "🌐 OYUN DİLİNİ DEĞİŞTİR",
            ru: "🌐 СМЕНИТЬ ЯЗЫК: " + LanguageSelectionUI.CurrentLanguageCode.ToUpperInvariant(),
            fr: "🌐 CHANGER DE LANGUE",
            es: "🌐 CAMBIAR IDIOMA",
            hi: "🌐 खेल की भाषा बदलें",
            zh: "🌐 更改游戏语言",
            ja: "🌐 ゲーム言語を変更",
            pt: "🌐 MUDAR IDIOMA"
        );

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 40f), langBtnText, new Color(0.35f, 0.85f, 0.98f), 11))
        {
            ClosePauseMenu();
            if (LanguageSelectionUI.Instance != null)
            {
                LanguageSelectionUI.Instance.OpenModal();
            }
        }

        curY += 46f;

        // 4. Audio & Volume Section Plate
        Rect audioPlateRect = new Rect(0, curY, itemW, 82f);
        EnchantedNatureGUI.DrawWoodPlate(audioPlateRect, new Color(0.06f, 0.12f, 0.08f, 0.95f), new Color(0.4f, 0.85f, 0.6f));

        GUIStyle audioTitleStyle = new GUIStyle(GUI.skin.label);
        audioTitleStyle.fontSize = 11;
        audioTitleStyle.fontStyle = FontStyle.Bold;
        audioTitleStyle.alignment = TextAnchor.MiddleLeft;
        audioTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.45f);

        int volPercent = isMuted ? 0 : Mathf.RoundToInt(currentVolume * 100f);
        string audioLabel = LanguageSelectionUI.Localize(
            $"🔊 MASTER AUDIO: {volPercent}%",
            ar: $"🔊 مستوى الصوت الرئيسي: {volPercent}%",
            fa: $"🔊 صدای کلی: {volPercent}%",
            ru: $"🔊 ГРОМКОСТЬ ЗВУКА: {volPercent}%",
            fr: $"🔊 VOLUME GÉNÉRAL : {volPercent}%",
            es: $"🔊 VOLUMEN PRINCIPAL: {volPercent}%"
        );
        GUI.Label(new Rect(14f, curY + 8f, itemW - 130f, 20f), audioLabel, audioTitleStyle);

        // Mute / Unmute Button
        string muteBtnText = isMuted ? "🔇 UNMUTE" : "🔊 MUTE";
        if (GUI.Button(new Rect(itemW - 105f, curY + 8f, 92f, 24f), muteBtnText))
        {
            isMuted = !isMuted;
            if (isMuted)
            {
                preMuteVolume = currentVolume;
                AudioListener.volume = 0f;
            }
            else
            {
                currentVolume = preMuteVolume > 0.05f ? preMuteVolume : 0.8f;
                AudioListener.volume = currentVolume;
            }
        }

        // Horizontal Volume Slider
        float sliderY = curY + 42f;
        float newVol = GUI.HorizontalSlider(new Rect(14f, sliderY, itemW - 28f, 24f), isMuted ? 0f : currentVolume, 0f, 1f);
        if (Mathf.Abs(newVol - (isMuted ? 0f : currentVolume)) > 0.01f)
        {
            currentVolume = newVol;
            isMuted = currentVolume <= 0.01f;
            AudioListener.volume = isMuted ? 0f : currentVolume;
        }

        curY += 92f;

        // 5. Music Jukebox Selector Button
        string jukeboxText = LanguageSelectionUI.Localize(
            "🎵 MUSIC JUKEBOX & TRACKS (مشغل الموسيقى)",
            ar: "🎵 خزانة الموسيقى وقائمة الأغاني",
            fa: "🎵 جعبه موسیقی و آهنگ‌ها",
            ru: "🎵 МУЗЫКАЛЬНЫЙ АВТОМАТ",
            fr: "🎵 JUKEBOX MUSICAL & PISTES",
            es: "🎵 JUKEBOX MUSICAL Y PISTAS"
        );
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 40f), jukeboxText, new Color(0.95f, 0.82f, 0.35f), 11))
        {
            ClosePauseMenu();
            ForestBGMManager.OpenJukebox();
        }

        curY += 46f;

        // 6. Mobile Touch HUD Toggle (F2 / Click)
        bool isMobileHudForced = MobileUI.ForceShowMobileControls;
        string hudToggleText = isMobileHudForced
            ? LanguageSelectionUI.Localize("📱 TOUCH HUD: ENABLED (Click to Hide)", ar: "📱 أزرار اللمس بالهاتف: ظاهرة (انقر للإخفاء)", fr: "📱 CONTRÔLES TACTILES : ACTIVÉS")
            : LanguageSelectionUI.Localize("📱 TOUCH HUD: DISABLED (Click to Show)", ar: "📱 أزرار اللمس بالهاتف: مخفية (انقر للإظهار)", fr: "📱 CONTRÔLES TACTILES : DÉSACTIVÉS");

        Color hudBtnColor = isMobileHudForced ? new Color(0.25f, 0.95f, 0.45f) : new Color(0.70f, 0.70f, 0.70f);
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 40f), hudToggleText, hudBtnColor, 11))
        {
            MobileUI.ForceShowMobileControls = !MobileUI.ForceShowMobileControls;
        }

        curY += 46f;

        // 7. Team Chat Mute / Unmute Toggle Button
        bool isChatMuted = InGameChatUI.IsChatMuted;
        string chatToggleText = isChatMuted
            ? LanguageSelectionUI.Localize(
                "🔕 TEAM CHAT: MUTED (Click to Enable)",
                ar: "🔕 محادثة الفريق: صامتة ومخفية (انقر للتفعيل)",
                fa: "🔕 گفتگوی تیم: غیرفعال (کلیک برای فعال‌سازی)",
                ru: "🔕 ЧАТ КОМАНДЫ: ОТКЛЮЧЕН (Включить)",
                fr: "🔕 CHAT D'ÉQUIPE : DÉSACTIVÉ (Activer)",
                es: "🔕 CHAT DE EQUIPO: SILENCIADO (Activar)")
            : LanguageSelectionUI.Localize(
                "💬 TEAM CHAT: ACTIVE (Click to Mute)",
                ar: "💬 محادثة الفريق: مفعّلة (انقر للإسكات والإخفاء)",
                fa: "💬 گفتگوی تیم: فعال (کلیک برای بی‌صدا کردن)",
                ru: "💬 ЧАТ КОМАНДЫ: ВКЛЮЧЕН (Заглушить)",
                fr: "💬 CHAT D'ÉQUIPE : ACTIF (Désactiver)",
                es: "💬 CHAT DE EQUIPO: ACTIVO (Silenciar)");

        Color chatBtnColor = isChatMuted ? new Color(0.95f, 0.45f, 0.35f) : new Color(0.35f, 0.88f, 0.95f);
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 40f), chatToggleText, chatBtnColor, 11))
        {
            InGameChatUI.ToggleChatMute();
        }

        curY += 46f;

        // 8. Live Team Voice Chat Toggle Button
        bool isVoiceEnabled = TeamVoiceChatManager.IsVoiceChatEnabled;
        string voiceToggleText = isVoiceEnabled
            ? LanguageSelectionUI.Localize(
                "🎙️ TEAM VOICE CALLS: ENABLED (Click to Disable)",
                ar: "🎙️ التواصل الصوتي المباشر: مفعّل (انقر للتعطيل)",
                fa: "🎙️ مکالمات صوتی زنده: فعال (کلیک برای غیرفعال‌سازی)",
                ru: "🎙️ ГОЛОСОВОЙ ЧАТ: ВКЛЮЧЕН (Отключить)",
                fr: "🎙️ APPELS VOCAUX : ACTIVÉS (Désactiver)",
                es: "🎙️ LLAMADAS DE VOZ: ACTIVADAS (Desactivar)")
            : LanguageSelectionUI.Localize(
                "🎙️ TEAM VOICE CALLS: DISABLED (Click to Enable)",
                ar: "🎙️ التواصل الصوتي المباشر: معطّل (انقر للتفعيل)",
                fa: "🎙️ مکالمات صوتی زنده: غیرفعال (کلیک برای فعال‌سازی)",
                ru: "🎙️ ГОЛОСОВОЙ ЧАТ: ВЫКЛЮЧЕН (Включить)",
                fr: "🎙️ APPELS VOCAUX : DÉSACTIVÉS (Activer)",
                es: "🎙️ LLAMADAS DE VOZ: DESACTIVADAS (Activar)");

        Color voiceBtnCol = isVoiceEnabled ? new Color(0.25f, 0.95f, 0.45f) : new Color(0.75f, 0.75f, 0.75f);
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 40f), voiceToggleText, voiceBtnCol, 11))
        {
            TeamVoiceChatManager.SetVoiceChatOption(!isVoiceEnabled, true);
        }

        curY += 46f;

        // 9. Return to Main Menu / Leave Match Button
        string leaveBtnText = LanguageSelectionUI.Localize(
            "🚪 RETURN TO GROVE / MAIN MENU",
            ar: "🚪 مغادرة المعركة والعودة للبداية",
            fa: "🚪 خروج و بازگشت به منوی اصلی",
            de: "🚪 ZUM HAUPTMENÜ ZURÜCKKEHREN",
            it: "🚪 ESCI AL MENU PRINCIPALE",
            tr: "🚪 ÇIKIŞ VE ANA MENÜYE DÖN",
            ru: "🚪 ВЫЙТИ В ГЛАВНОЕ МЕНЮ (ГРОТ)",
            fr: "🚪 QUITTER ET RETOURNER AU BOSQUET",
            es: "🚪 SALIR AL MENÚ PRINCIPAL",
            hi: "🚪 मुख्य मेनू पर वापस लौटें",
            zh: "🚪 离开比赛并返回主界面",
            ja: "🚪 試合を離脱してメインへ戻る",
            pt: "🚪 SAIR PARA O MENU PRINCIPAL"
        );

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 42f), leaveBtnText, new Color(0.95f, 0.65f, 0.25f), 11))
        {
            ClosePauseMenu();
            MatchEndStatsUI.ExecuteCleanGameRestart();
        }

        curY += 48f;

        // 10. Exit Game to Desktop Button
        string quitBtnText = LanguageSelectionUI.Localize(
            "❌ QUIT GAME TO DESKTOP",
            ar: "❌ الخروج النهائي من اللعبة",
            fa: "❌ خروج از بازی به دسکتاپ",
            de: "❌ SPIEL BEENDEN",
            it: "❌ ESCI DAL GIOCO",
            tr: "❌ OYUNDAN MASAÜSTÜNE ÇIK",
            ru: "❌ ВЫЙТИ ИЗ ИГРЫ НА РАБОЧИЙ СТОЛ",
            fr: "❌ QUITTER LE JEU",
            es: "❌ SALIR DEL JUEGO",
            hi: "❌ खेल बंद करें",
            zh: "❌ 退出游戏至桌面",
            ja: "❌ ゲームを終了してデスクトップへ",
            pt: "❌ SAIR DO JOGO"
        );

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(0, curY, itemW, 40f), quitBtnText, new Color(0.85f, 0.35f, 0.30f), 11))
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        GUI.EndScrollView();
    }
}
