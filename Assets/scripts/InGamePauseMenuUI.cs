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
                if ((GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) ||
                    (PlayerMovement.LocalPlayer != null && PlayerMovement.LocalPlayer.hasSelectedCharacter.Value))
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
        // 1. In-Game Pause & Settings Corner HUD Button (always visible during active gameplay when pause menu is closed)
        if (!isMenuOpen && 
            !LanguageSelectionUI.IsLanguageModalOpen && 
            !GameIntroBriefingUI.IsBriefingOpen && 
            !CharacterSelectionUI.IsSelectionOpen &&
            !PlayerProgressionAndShopUI.IsShopOpen)
        {
            if ((GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) ||
                (PlayerMovement.LocalPlayer != null && PlayerMovement.LocalPlayer.hasSelectedCharacter.Value))
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

    private void DrawPauseModal()
    {
        // Full screen dimmed emerald vignette
        GUI.color = new Color(0.02f, 0.05f, 0.03f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(520f, Screen.width - 24f);
        float modalH = Mathf.Min(545f, Screen.height - 24f);
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
        GUI.Label(new Rect(mx, my + 18f, modalW, 28f), headerText, titleStyle);

        // Subtitle
        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 11;
        subStyle.fontStyle = FontStyle.Italic;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.6f, 0.88f, 0.7f);

        string subText = LanguageSelectionUI.Localize(
            "~ Sacred Grove Sanctuary & Expedition Controls ~",
            ar: "~ ملاذ الغابة المقدسة وضوابط الرحلة ~",
            fa: "~ پناهگاه بیشه مقدس و تنظیمات سفر ~",
            de: "~ Heiliger Hain und Expeditionssteuerung ~",
            it: "~ Santuario del bosco sacro e controlli ~",
            tr: "~ Kutsal Koru Sığınağı ve Sefer Kontrolleri ~",
            ru: "~ Священная роща и управление экспедицией ~",
            fr: "~ Sanctuaire du bosquet sacré et commandes ~",
            es: "~ Santuario sagrado y controles de expedición ~",
            hi: "~ पवित्र ग्रोव अभयारण्य और अभियान नियंत्रण ~",
            zh: "~ 神圣树林圣所与远征控制 ~",
            ja: "~ 聖なる森の聖域と遠征設定 ~",
            pt: "~ Santuário do bosque sagrado e controles ~"
        );
        GUI.Label(new Rect(mx + 20f, my + 46f, modalW - 40f, 20f), subText, subStyle);

        float contentY = my + 80f;
        float itemW = modalW - 60f;
        float itemX = mx + 30f;

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

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(itemX, contentY, itemW, 46f), resumeText, new Color(0.25f, 0.95f, 0.45f), 13))
        {
            ClosePauseMenu();
        }

        contentY += 56f;

        // 2. Change Language Button
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

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(itemX, contentY, itemW, 44f), langBtnText, new Color(0.35f, 0.85f, 0.98f), 12))
        {
            ClosePauseMenu();
            if (LanguageSelectionUI.Instance != null)
            {
                LanguageSelectionUI.Instance.OpenModal();
            }
        }

        contentY += 56f;

        // 3. Audio & Volume Section Plate
        Rect audioPlateRect = new Rect(itemX, contentY, itemW, 86f);
        EnchantedNatureGUI.DrawWoodPlate(audioPlateRect, new Color(0.06f, 0.12f, 0.08f, 0.95f), new Color(0.4f, 0.85f, 0.6f));

        GUIStyle audioTitleStyle = new GUIStyle(GUI.skin.label);
        audioTitleStyle.fontSize = 12;
        audioTitleStyle.fontStyle = FontStyle.Bold;
        audioTitleStyle.alignment = TextAnchor.MiddleLeft;
        audioTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.45f);

        int volPercent = isMuted ? 0 : Mathf.RoundToInt(currentVolume * 100f);
        string audioLabel = LanguageSelectionUI.Localize(
            $"🔊 MASTER AUDIO: {volPercent}%",
            ar: $"🔊 مستوى الصوت الرئيسي: {volPercent}%",
            fa: $"🔊 صدای کلی: {volPercent}%",
            de: $"🔊 LAUTSTÄRKE: {volPercent}%",
            it: $"🔊 AUDIO PRINCIPALE: {volPercent}%",
            tr: $"🔊 ANA SES: %{volPercent}",
            ru: $"🔊 ГРОМКОСТЬ ЗВУКА: {volPercent}%",
            fr: $"🔊 VOLUME GÉNÉRAL : {volPercent}%",
            es: $"🔊 VOLUMEN PRINCIPAL: {volPercent}%",
            hi: $"🔊 मुख्य ध्वनि: {volPercent}%",
            zh: $"🔊 主音量: {volPercent}%",
            ja: $"🔊 マスター音量: {volPercent}%",
            pt: $"🔊 VOLUME PRINCIPAL: {volPercent}%"
        );
        GUI.Label(new Rect(itemX + 16f, contentY + 10f, itemW - 130f, 22f), audioLabel, audioTitleStyle);

        // Mute / Unmute Button
        string muteBtnText = isMuted ? "🔇 UNMUTE" : "🔊 MUTE";
        if (GUI.Button(new Rect(itemX + itemW - 110f, contentY + 10f, 96f, 26f), muteBtnText))
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
        float sliderY = contentY + 46f;
        float newVol = GUI.HorizontalSlider(new Rect(itemX + 16f, sliderY, itemW - 32f, 24f), isMuted ? 0f : currentVolume, 0f, 1f);
        if (Mathf.Abs(newVol - (isMuted ? 0f : currentVolume)) > 0.01f)
        {
            currentVolume = newVol;
            isMuted = currentVolume <= 0.01f;
            AudioListener.volume = isMuted ? 0f : currentVolume;
        }

        contentY += 98f;

        // 4. Music Jukebox Selector Button
        string jukeboxText = LanguageSelectionUI.Localize(
            "🎵 MUSIC JUKEBOX (تغيير نمط الموسيقى)",
            ar: "🎵 خزانة الموسيقى (تغيير النمط)",
            fa: "🎵 جعبه موسیقی (تغییر سبک)",
            ru: "🎵 МУЗЫКАЛЬНЫЙ АВТОМАТ",
            fr: "🎵 JUKEBOX MUSICAL",
            es: "🎵 JUKEBOX MUSICAL"
        );
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(itemX, contentY, itemW, 42f), jukeboxText, new Color(0.95f, 0.82f, 0.35f), 12))
        {
            ClosePauseMenu();
            ForestBGMManager.OpenJukebox();
        }

        contentY += 52f;

        // 5. Return to Main Menu / Leave Match Button
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

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(itemX, contentY, itemW, 44f), leaveBtnText, new Color(0.95f, 0.65f, 0.25f), 12))
        {
            ClosePauseMenu();
            MatchEndStatsUI.ExecuteCleanGameRestart();
        }

        contentY += 54f;

        // 5. Exit Game to Desktop Button
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

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(itemX, contentY, itemW, 42f), quitBtnText, new Color(0.85f, 0.35f, 0.30f), 12))
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
