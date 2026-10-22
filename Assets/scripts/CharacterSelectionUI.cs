using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// CharacterSelectionUI — "The Hall of Sacred Ancients"
/// AAA PC Fantasy Game Character Selection System:
/// - 3-Column Cinematic Layout (Left Roster, Center 3D Altar Stage, Right Combat Codex & Wardrobe)
/// - Role Filtering Tabs: ALL (25), TANK (5), DAMAGE (7), SUPPORT (4), CONTROL (4), SPECIALIST (5)
/// - Interactive 3D Turntable with Mouse Drag Spin & Dual Studio Lighting (Warm Key + Emerald Rim)
/// - True Combat Attributes & Sacred Skins Multipliers (+HP, +ATK, +SPD)
/// - 100% Comprehensive Localization across 11 Languages (English, Arabic, Persian, French, Russian, Spanish, German, Hindi, Chinese, Japanese, Portuguese)
/// </summary>
public class CharacterSelectionUI : MonoBehaviour
{
    public static CharacterSelectionUI Instance { get; private set; }
    public static bool IsSelectionOpen { get; private set; } = false;

    [Header("References")]
    public CharacterDatabase database;
    public CharacterCardUI cardPrefab;
    public Transform cardContainer;
    public TextMeshProUGUI selectedNameText;
    public TextMeshProUGUI selectedDescText;
    public TextMeshProUGUI selectedStatsText;
    public Image selectedIcon;
    public Button confirmButton;

    [Header("State")]
    private int selectedCharacterID = 0; // Default to Archer (ID: 0)
    private bool isShowing = false;
    private int currentSkinIndex = 0;
    private bool skinEquipped = false;
    private CharacterSkin[] availableSkins;
    private CharacterSkin currentSkin;

    // Filter by Role (-1 = All, 0 = Tank, 1 = Damage, 2 = Support, 3 = Control, 4 = Specialist)
    private int selectedRoleFilter = -1;
    private int selectedDetailsTab = 0;

    // UI Scroll & Animation State
    private Vector2 gridScrollPosition;
    private Vector2 detailsScrollPosition;
    private int lastDetailsCharID = -999;
    private string skinNotification = "";
    private float skinNotificationTimer = 0f;
    private int hoveredCharacterID = -1;
    private float hoverAnimTime = 0f;
    private float selectionAnimTime = 0f;
    private float headerPulse = 0f;
    private float[] cardAnimTimers;

    // Interactive 3D Drag Rotation
    private bool isDraggingModel = false;
    private float lastMouseX = 0f;

    // Cached procedural textures
    private Texture2D gradientTex;
    private Texture2D glowTex;
    private Texture2D cardBgTex;

    // 3D Preview Studio
    private GameObject previewStudioRoot;
    private Camera previewCamera;
    private RenderTexture previewRenderTexture;
    private Transform turntable;
    private GameObject currentPreviewModel;
    private int currentPreviewID = -999;
    private const float STUDIO_X = 800f;
    private const float STUDIO_Y = -400f;
    private const float STUDIO_Z = 800f;

    // Role colors
    public static readonly Dictionary<CharacterData.CharacterRole, Color> RoleColors = new Dictionary<CharacterData.CharacterRole, Color>
    {
        { CharacterData.CharacterRole.Tank, new Color(0.28f, 0.60f, 0.95f) },
        { CharacterData.CharacterRole.Damage, new Color(0.95f, 0.28f, 0.20f) },
        { CharacterData.CharacterRole.Support, new Color(0.25f, 0.88f, 0.45f) },
        { CharacterData.CharacterRole.Control, new Color(0.75f, 0.35f, 0.95f) },
        { CharacterData.CharacterRole.Specialist, new Color(0.98f, 0.68f, 0.18f) }
    };

    public static string GetRoleName(CharacterData.CharacterRole role)
    {
        switch (role)
        {
            case CharacterData.CharacterRole.Tank:
                return LanguageSelectionUI.Localize("TANK", "مدرع", "ТАНК", "TANQUE", "TANK", "TANK", "टैंक", "坦克", "タンク", "TANQUE", "تانک");
            case CharacterData.CharacterRole.Damage:
                return LanguageSelectionUI.Localize("DAMAGE", "هجوم", "УРОН", "DAÑO", "DÉGÂTS", "SCHADEN", "क्षति", "伤害", "ダメージ", "DANO", "حمله");
            case CharacterData.CharacterRole.Support:
                return LanguageSelectionUI.Localize("SUPPORT", "دعم", "ПОДДЕРЖКА", "APOYO", "SOUTIEN", "UNTERSTÜTZUNG", "समर्थन", "辅助", "サポート", "SUPORTE", "پشتیبانی");
            case CharacterData.CharacterRole.Control:
                return LanguageSelectionUI.Localize("CONTROL", "تحكم", "КОНТРОЛЬ", "CONTROL", "CONTRÔLE", "KONTROLLE", "नियंत्रण", "控制", "コントロール", "CONTROLE", "کنترل");
            case CharacterData.CharacterRole.Specialist:
                return LanguageSelectionUI.Localize("SPECIALIST", "مختص", "СПЕЦИАЛИСТ", "ESPECIALISTA", "SPÉCIALISTE", "SPEZIALIST", "विशेषज्ञ", "专家", "スペシャリスト", "ESPECIALISTA", "متخصص");
            default:
                return LanguageSelectionUI.Localize("HERO", "بطل", "ГЕРОЙ", "HÉROE", "HÉROS", "HELD", "नायक", "英雄", "ヒーロー", "HERÓI", "قهرمان");
        }
    }

    public static string GetRoleEmoji(CharacterData.CharacterRole role)
    {
        switch (role)
        {
            case CharacterData.CharacterRole.Tank: return "🛡️";
            case CharacterData.CharacterRole.Damage: return "⚔️";
            case CharacterData.CharacterRole.Support: return "💚";
            case CharacterData.CharacterRole.Control: return "✨";
            case CharacterData.CharacterRole.Specialist: return "🔮";
            default: return "🌿";
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSelection);

        CreateCachedTextures();
        SetupPreviewStudio();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (previewRenderTexture != null)
        {
            previewRenderTexture.Release();
            Destroy(previewRenderTexture);
        }
        if (previewStudioRoot != null)
        {
            Destroy(previewStudioRoot);
        }
        if (gradientTex != null) Destroy(gradientTex);
        if (glowTex != null) Destroy(glowTex);
        if (cardBgTex != null) Destroy(cardBgTex);
        IsSelectionOpen = false;
    }

    void CreateCachedTextures()
    {
        // Smooth Vertical gradient
        gradientTex = new Texture2D(1, 64);
        for (int y = 0; y < 64; y++)
        {
            float t = y / 63f;
            gradientTex.SetPixel(0, y, new Color(0, 0, 0, Mathf.Lerp(0.95f, 0.35f, t)));
        }
        gradientTex.Apply();
        gradientTex.wrapMode = TextureWrapMode.Clamp;

        // Radial glow disc
        glowTex = new Texture2D(32, 32);
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                float dx = (x - 15.5f) / 16f;
                float dy = (y - 15.5f) / 16f;
                float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                glowTex.SetPixel(x, y, new Color(1, 1, 1, d * d));
            }
        }
        glowTex.Apply();

        // Card background texture
        cardBgTex = new Texture2D(1, 32);
        for (int y = 0; y < 32; y++)
        {
            float t = y / 31f;
            float v = Mathf.Lerp(0.05f, 0.12f, t);
            cardBgTex.SetPixel(0, y, new Color(v * 0.7f, v, v * 0.8f, 1f));
        }
        cardBgTex.Apply();
        cardBgTex.wrapMode = TextureWrapMode.Clamp;
    }

    // =========================================================================
    // 3D PREVIEW STUDIO (TURNTABLE + DUAL LIGHTING + HIGH-RES CAMERA)
    // =========================================================================
    void SetupPreviewStudio()
    {
        if (previewStudioRoot != null) return;

        previewStudioRoot = new GameObject("ChampionPreviewStudio");
        previewStudioRoot.transform.position = new Vector3(STUDIO_X, STUDIO_Y, STUDIO_Z);

        // High-Definition Render Texture (1024x1024)
        previewRenderTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
        previewRenderTexture.Create();

        // Studio Camera
        GameObject camObj = new GameObject("PreviewCamera");
        camObj.transform.SetParent(previewStudioRoot.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 1.18f, -4.05f);
        camObj.transform.localRotation = Quaternion.Euler(2.0f, 0f, 0f);

        previewCamera = camObj.AddComponent<Camera>();
        previewCamera.targetTexture = previewRenderTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.015f, 0.035f, 0.025f, 1f);
        previewCamera.fieldOfView = 31f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 30f;
        previewCamera.enabled = true;

        // Key Studio Light (Warm Solar Gold)
        GameObject lightObj = new GameObject("StudioKeyLight");
        lightObj.transform.SetParent(previewStudioRoot.transform, false);
        lightObj.transform.localPosition = new Vector3(1.6f, 3.8f, -2.2f);
        lightObj.transform.localRotation = Quaternion.Euler(42f, -32f, 0f);
        Light keyLight = lightObj.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1f, 0.96f, 0.86f);
        keyLight.intensity = 1.45f;

        // Rim Light (Sacred Emerald Glow)
        GameObject rimObj = new GameObject("StudioRimLight");
        rimObj.transform.SetParent(previewStudioRoot.transform, false);
        rimObj.transform.localPosition = new Vector3(-2.2f, 2.4f, 2.2f);
        rimObj.transform.localRotation = Quaternion.Euler(28f, 138f, 0f);
        Light rimLight = rimObj.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.color = new Color(0.22f, 0.92f, 0.48f);
        rimLight.intensity = 0.95f;

        // Turntable Root
        GameObject ttObj = new GameObject("Turntable");
        ttObj.transform.SetParent(previewStudioRoot.transform, false);
        ttObj.transform.localPosition = Vector3.zero;
        turntable = ttObj.transform;

        // Sacred Pedestal Dais
        GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dais.name = "SacredDais";
        dais.transform.SetParent(turntable, false);
        dais.transform.localPosition = new Vector3(0f, -0.16f, 0f);
        dais.transform.localScale = new Vector3(2.2f, 0.06f, 2.2f);
        Collider dCol = dais.GetComponent<Collider>();
        if (dCol != null) Destroy(dCol);
        Renderer dRend = dais.GetComponent<Renderer>();
        if (dRend != null)
        {
            dRend.material.color = new Color(0.08f, 0.14f, 0.10f);
        }

        UpdatePreviewStudioModel(selectedCharacterID >= 0 ? selectedCharacterID : 0);
    }

    void UpdatePreviewStudioModel(int characterID)
    {
        if (turntable == null) return;
        if (characterID == currentPreviewID && currentPreviewModel != null) return;

        currentPreviewID = characterID;

        if (currentPreviewModel != null)
        {
            Destroy(currentPreviewModel);
        }

        CharacterData ch = (database != null) ? database.GetCharacter(characterID) : null;
        string lowName = (ch != null && !string.IsNullOrEmpty(ch.characterName)) ? ch.characterName.ToLower() : "";
        bool isArcherModel = (characterID == 0) || lowName.Contains("archer") || lowName.Contains("ranger") || lowName.Contains("windwalker");

        GameObject prefab = null;
        if (isArcherModel)
        {
            prefab = Resources.Load<GameObject>("Models/Erika_Archer");
            if (prefab == null) prefab = Resources.Load<GameObject>("Models/Akai");
        }
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Models/Warrior_Medieval");
        }

        if (prefab != null)
        {
            currentPreviewModel = Instantiate(prefab, turntable);
            currentPreviewModel.name = "Preview_Champion_" + characterID;
            currentPreviewModel.transform.localPosition = new Vector3(0f, -0.10f, 0f);
            currentPreviewModel.transform.localRotation = Quaternion.identity;
            currentPreviewModel.transform.localScale = Vector3.one * 0.90f;

            foreach (Collider c in currentPreviewModel.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (Rigidbody r in currentPreviewModel.GetComponentsInChildren<Rigidbody>()) Destroy(r);
            foreach (Animator a in currentPreviewModel.GetComponentsInChildren<Animator>())
            {
                a.applyRootMotion = false;
                if (isArcherModel)
                {
                    a.enabled = true;
                    a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
                else
                {
                    a.enabled = false;
                }
            }

            if (isArcherModel)
            {
                PlayerMovement.ApplyErikaArcherMaterials(currentPreviewModel, PlayerMovement.TeamType.Protector);

                // Attach 3D Bow if not present
                Transform bowTr = null;
                foreach (Transform t in currentPreviewModel.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.ToLower().Contains("bow")) { bowTr = t; break; }
                }
                if (bowTr == null)
                {
                    Transform leftHand = null;
                    foreach (Transform t in currentPreviewModel.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.ToLower().Contains("lefthand")) { leftHand = t; break; }
                    }
                    if (leftHand != null)
                    {
                        GameObject bowPrefab = Resources.Load<GameObject>("Models/Bow_3D");
                        if (bowPrefab != null)
                        {
                            GameObject bowInstance = Instantiate(bowPrefab, leftHand);
                            bowInstance.name = "Bow_3D";
                            bowInstance.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                            bowInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                            bowInstance.transform.localScale = Vector3.one * (leftHand.lossyScale.x < 0.1f ? 100f : 1f);
                            PlayerMovement.ApplyErikaArcherMaterials(bowInstance, PlayerMovement.TeamType.Protector);
                        }
                    }
                }
            }
            else if (ch != null)
            {
                PlayerMovement.StyleModelForCharacter(currentPreviewModel, ch, currentSkin);
            }

            CharacterVisualAnimator visualAnim = currentPreviewModel.GetComponent<CharacterVisualAnimator>();
            if (visualAnim == null)
            {
                visualAnim = currentPreviewModel.AddComponent<CharacterVisualAnimator>();
            }
            if (visualAnim != null)
            {
                visualAnim.isPreviewShowcase = true;
            }
        }

        // Live sync with player standing outside in the 3D scene
        PlayerMovement localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            localPlayer.ApplyCharacter(characterID);
        }
    }

    public void Show()
    {
        if (MatchEndStatsUI.isAutoRestartingMatch) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.currentState != GameStateManager.GameState.WaitingForPlayers) return;

        isShowing = true;
        IsSelectionOpen = true;

        if (selectedCharacterID < 0)
            selectedCharacterID = 0; // Default to Archer

        hoveredCharacterID = -1;
        selectedRoleFilter = -1; // Default to ALL
        currentSkinIndex = 0;
        skinEquipped = false;
        hoverAnimTime = 0f;
        selectionAnimTime = 0f;
        isDraggingModel = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (database != null && database.characters != null)
        {
            cardAnimTimers = new float[database.characters.Length];
            for (int i = 0; i < cardAnimTimers.Length; i++)
                cardAnimTimers[i] = 0f;
        }

        if (previewStudioRoot == null)
        {
            SetupPreviewStudio();
        }
        else
        {
            previewStudioRoot.SetActive(true);
            if (previewCamera != null) previewCamera.enabled = true;
        }

        currentPreviewID = -999;
        UpdatePreviewStudioModel(selectedCharacterID);
    }

    public void Hide()
    {
        isShowing = false;
        IsSelectionOpen = false;
        isDraggingModel = false;
        if (previewCamera != null) previewCamera.enabled = false;
        if (previewStudioRoot != null) previewStudioRoot.SetActive(false);
    }

    void Update()
    {
        if (!isShowing) return;

        headerPulse += Time.deltaTime * 1.5f;
        hoverAnimTime += Time.deltaTime * 4f;
        selectionAnimTime += Time.deltaTime * 3f;
        if (skinNotificationTimer > 0f) skinNotificationTimer -= Time.deltaTime;

        // Turntable smooth auto-rotation when not interactively dragging
        if (!isDraggingModel && turntable != null)
        {
            turntable.Rotate(Vector3.up * 14f * Time.deltaTime, Space.World);
        }

        // Keep 3D preview model synced with hovered or selected character
        int activePreviewID = (hoveredCharacterID >= 0) ? hoveredCharacterID : (selectedCharacterID >= 0 ? selectedCharacterID : 0);
        if (activePreviewID != currentPreviewID)
        {
            UpdatePreviewStudioModel(activePreviewID);
        }

        // Animate card timers
        if (cardAnimTimers != null && database != null && database.characters != null)
        {
            for (int i = 0; i < cardAnimTimers.Length && i < database.characters.Length; i++)
            {
                if (database.characters[i] == null) continue;
                bool isHovered = database.characters[i].characterID == hoveredCharacterID;
                bool isSelected = database.characters[i].characterID == selectedCharacterID;

                float target = (isHovered || isSelected) ? 1f : 0f;
                cardAnimTimers[i] = Mathf.MoveTowards(cardAnimTimers[i], target, Time.deltaTime * 6f);
            }
        }
    }

    void OnGUI()
    {
        if (!isShowing) return;
        if (GameIntroBriefingUI.IsBriefingOpen) return;
        if (PlayerProgressionAndShopUI.IsShopOpen) return;
        if (gradientTex == null) CreateCachedTextures();

        float sw = Screen.width;
        float sh = Screen.height;

        // 1. Full Screen Translucent Ambient Background
        DrawBackground(sw, sh);

        // 2. Top Header (Back Button, Title, Subtitle)
        DrawHeader(sw, sh);

        // 3. Top Role Filter Ribbon (ALL, TANK, DAMAGE, SUPPORT, CONTROL, SPECIALIST)
        float filterY = sh * 0.075f;
        float filterH = 32f;
        DrawRoleFilterRibbon(new Rect(sw * 0.02f, filterY, sw * 0.96f, filterH));

        // 4. AAA 3-Column Layout:
        // Left: Hero Roster Gallery (28% width)
        // Center: Grand 3D Hero Altar Showcase (41% width)
        // Right: Combat Codex, Sacred Wardrobe & Confirm Button (28% width)
        float contentY = sh * 0.125f;
        float contentH = sh * 0.845f;
        float pad = sw * 0.015f;
        float leftW = sw * 0.28f;
        float centerW = sw * 0.40f;
        float rightW = sw * 0.28f;

        float leftX = pad;
        float centerX = leftX + leftW + pad * 0.5f;
        float rightX = centerX + centerW + pad * 0.5f;

        // Active Display Character
        int displayID = (hoveredCharacterID >= 0) ? hoveredCharacterID : (selectedCharacterID >= 0 ? selectedCharacterID : 0);
        CharacterData displayChar = (database != null) ? database.GetCharacter(displayID) : null;
        if (displayChar == null && database != null && database.characters != null && database.characters.Length > 0)
            displayChar = database.characters[0];

        // --- Column 1: Left Hero Roster ---
        DrawCharacterGrid(new Rect(leftX, contentY, leftW, contentH));

        // --- Column 2: Center Grand 3D Hero Altar ---
        if (displayChar != null)
        {
            DrawCenterHeroStage(new Rect(centerX, contentY, centerW, contentH), displayChar);
        }

        // --- Column 3: Right Combat Codex & Wardrobe ---
        if (displayChar != null)
        {
            DrawCharacterDetails(new Rect(rightX, contentY, rightW, contentH), displayChar);
        }
    }

    // ==================== BACKGROUND ====================
    void DrawBackground(float sw, float sh)
    {
        // Translucent deep obsidian-emerald haze
        GUI.color = new Color(0.018f, 0.045f, 0.030f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        GUI.color = new Color(0.04f, 0.15f, 0.08f, 0.28f);
        GUI.DrawTexture(new Rect(0, sh * 0.25f, sw, sh * 0.75f), gradientTex, ScaleMode.StretchToFill);

        // Header and Footer obsidian bars
        GUI.color = new Color(0.01f, 0.02f, 0.015f, 0.95f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh * 0.072f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, sh * 0.975f, sw, sh * 0.025f), Texture2D.whiteTexture);

        // Radiant antique gold trim line below header
        float pulse = 0.7f + 0.3f * Mathf.Sin(headerPulse);
        GUI.color = new Color(0.85f * pulse, 0.70f * pulse, 0.25f * pulse, 0.9f);
        GUI.DrawTexture(new Rect(sw * 0.02f, sh * 0.072f, sw * 0.96f, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // ==================== HEADER ====================
    void DrawHeader(float sw, float sh)
    {
        // Top-left Back button returning to Team Selection
        float backW = Mathf.Max(150f, sw * 0.13f);
        float backH = 30f;
        float backX = sw * 0.02f;
        float backY = sh * 0.016f;

        string backText = LanguageSelectionUI.Localize(
            "⬅️ BACK TO TEAMS",
            ar: "⬅️ رجوع لاختيار الفريق",
            ru: "⬅️ НАЗАД К КОМАНДАМ",
            fr: "⬅️ RETOUR AUX ÉQUIPES",
            es: "⬅️ VOLVER A EQUIPOS",
            de: "⬅️ ZURÜCK ZU TEAMS",
            hi: "⬅️ वापस (टीम चयन)",
            zh: "⬅️ 返回队伍选择",
            ja: "⬅️ チーム選択へ戻る",
            pt: "⬅️ VOLTAR AOS TIMES",
            fa: "⬅️ بازگشت به انتخاب تیم"
        );

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(backX, backY, backW, backH), backText, new Color(0.85f, 0.45f, 0.35f), 11))
        {
            Hide();
            PlayerMovement player = GetLocalPlayer();
            if (player != null)
            {
                player.ResetTeamSelection();
            }
            TeamSelectionUI teamUI = FindAnyObjectByType<TeamSelectionUI>();
            if (teamUI != null)
            {
                teamUI.Reopen();
            }
            return;
        }

        // Center Title & Lore Subtitle
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Max(15, (int)(sw * 0.018f));
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        float glow = 0.88f + 0.12f * Mathf.Sin(headerPulse);
        titleStyle.normal.textColor = new Color(1.0f * glow, 0.86f * glow, 0.38f, 1f);

        string titleStr = LanguageSelectionUI.Localize(
            "✦ CHOOSE YOUR SACRED CHAMPION ✦",
            ar: "✦ اختر بطلك المقدس ✦",
            ru: "✦ ВЫБЕРИТЕ ВАШЕГО ЧЕМПИОНА ✦",
            fr: "✦ CHOISISSEZ VOTRE CHAMPION SACRÉ ✦",
            es: "✦ ELIGE A TU CAMPEÓN SAGRADO ✦",
            de: "✦ WÄHLE DEINEN HEILIGEN HELDEN ✦",
            hi: "✦ अपने पवित्र नायक को चुनें ✦",
            zh: "✦ 选择你的神圣英雄 ✦",
            ja: "✦ 聖なる英雄を選択せよ ✦",
            pt: "✦ ESCOLHA SEU CAMPEÃO SAGRADO ✦",
            fa: "✦ قهرمان مقدس خود را انتخاب کنید ✦"
        );
        GUI.Label(new Rect(0, sh * 0.010f, sw, sh * 0.034f), titleStr, titleStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = Mathf.Max(9, (int)(sw * 0.0095f));
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.65f, 0.88f, 0.72f, 0.85f);
        subStyle.fontStyle = FontStyle.Italic;

        string subStr = LanguageSelectionUI.Localize(
            "Inspect your Guardian's 3D armor, sacred powers, and combat stats",
            ar: "مرر الفأرة أو انقر على أي حارس لفحص دروعه وقدراته وإحصائياته",
            ru: "Наведите курсор для просмотра брони и характеристик",
            fr: "Survolez ou cliquez pour inspecter l'armure et les stats",
            es: "Pasa el cursor para ver armadura y atributos",
            de: "Untersuche Rüstung, Kräfte und Kampfwerte",
            hi: "कवच, शक्ति और आँकड़े देखने के लिए क्लिक करें",
            zh: "查看守护者的3D装甲、神圣力量与战斗属性",
            ja: "鎧、能力、ステータスを確認せよ",
            pt: "Inspecione a armadura 3D, poderes e atributos de combate",
            fa: "برای بررسی زره، قدرت‌ها و مشخصات رزمی کلیک یا هاور کنید"
        );
        GUI.Label(new Rect(0, sh * 0.042f, sw, sh * 0.024f), subStr, subStyle);

        // Top-Right: Custom Hero Name Selection Box
        float nameBoxW = Mathf.Clamp(sw * 0.22f, 210f, 280f);
        float nameBoxH = 34f;
        float nameBoxX = sw * 0.98f - nameBoxW;
        float nameBoxY = sh * 0.015f;

        Rect nameRect = new Rect(nameBoxX, nameBoxY, nameBoxW, nameBoxH);
        EnchantedNatureGUI.DrawWoodPlate(nameRect, new Color(0.12f, 0.20f, 0.12f, 0.96f), new Color(0.85f, 0.72f, 0.32f, 0.95f));

        GUIStyle tagStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        tagStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);
        GUI.Label(new Rect(nameBoxX + 8f, nameBoxY + 7f, 65f, 20f), LanguageSelectionUI.Localize("NAME:", "الاسم:", "ИМЯ:", "NOM :", "NOMBRE:", "NAME:", "नाम:", "名字:", "名前:", "NOME:", "نام:"), tagStyle);

        string currentName = PlayerProfile.GetSavedPlayerName();
        GUIStyle inputStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        inputStyle.normal.textColor = new Color(0.35f, 1f, 0.6f);
        string edited = GUI.TextField(new Rect(nameBoxX + 75f, nameBoxY + 5f, nameBoxW - 85f, 24f), currentName, 16, inputStyle);
        if (edited != currentName)
        {
            PlayerProfile.Instance?.SetPlayerName(edited);
            if (PlayerPrefs.GetString("BW_PlayerName", "Ghaith") != edited)
            {
                PlayerPrefs.SetString("BW_PlayerName", edited);
                PlayerPrefs.Save();
            }
        }
    }

    // ==================== ROLE FILTER RIBBON ====================
    void DrawRoleFilterRibbon(Rect rect)
    {
        int[] filterRoles = new int[] { -1, 0, 1, 2, 3, 4 };
        string[] filterLabels = new string[]
        {
            LanguageSelectionUI.Localize("ALL (25)", "الكل (25)", "ВСЕ (25)", "TOUS (25)", "TODOS (25)", "ALLE (25)", "सभी (25)", "全部 (25)", "すべて (25)", "TODOS (25)", "همه (25)"),
            "🛡️ " + LanguageSelectionUI.Localize("TANK (5)", "مدرع (5)", "ТАНК (5)", "TANK (5)", "TANQUE (5)", "TANK (5)", "टैंक (5)", "坦克 (5)", "タンク (5)", "TANQUE (5)", "تانک (5)"),
            "⚔️ " + LanguageSelectionUI.Localize("DAMAGE (7)", "هجوم (7)", "УРОН (7)", "DÉGÂTS (7)", "DAÑO (7)", "SCHADEN (7)", "क्षति (7)", "伤害 (7)", "ダメージ (7)", "DANO (7)", "حمله (7)"),
            "💚 " + LanguageSelectionUI.Localize("SUPPORT (4)", "دعم (4)", "ПОДДЕРЖКА (4)", "SOUTIEN (4)", "APOYO (4)", "UNTERSTÜTZUNG (4)", "समर्थن (4)", "辅助 (4)", "サポート (4)", "SUPORTE (4)", "پشتیبانی (4)"),
            "✨ " + LanguageSelectionUI.Localize("CONTROL (4)", "تحكم (4)", "КОНТРОЛЬ (4)", "CONTRÔLE (4)", "CONTROL (4)", "KONTROLLE (4)", "नियंत्रण (4)", "控制 (4)", "コントロール (4)", "CONTROLE (4)", "کنترل (4)"),
            "🔮 " + LanguageSelectionUI.Localize("SPECIALIST (5)", "مختص (5)", "СПЕЦИАЛИСТ (5)", "SPÉCIALISTE (5)", "ESPECIALISTA (5)", "SPEZIALIST (5)", "विशेषज्ञ (5)", "专家 (5)", "スペシャリスト (5)", "ESPECIALISTA (5)", "متخصص (5)")
        };

        float btnW = (rect.width - 10f * (filterRoles.Length - 1)) / filterRoles.Length;
        for (int i = 0; i < filterRoles.Length; i++)
        {
            int r = filterRoles[i];
            bool isActive = (selectedRoleFilter == r);
            Rect bRect = new Rect(rect.x + i * (btnW + 10f), rect.y, btnW, rect.height);

            Color tabColor = isActive
                ? new Color(1.0f, 0.85f, 0.35f)
                : new Color(0.35f, 0.85f, 0.55f);

            if (EnchantedNatureGUI.DrawOrganicLeafButton(bRect, filterLabels[i], tabColor, 11))
            {
                selectedRoleFilter = r;
            }
        }
    }

    // ==================== COLUMN 1: LEFT HERO ROSTER ====================
    void DrawCharacterGrid(Rect area)
    {
        if (database == null || database.characters == null) return;

        // Obsidian Glass Frame with Antique Gold Filigree
        EnchantedNatureGUI.DrawAntiqueGoldFrame(area, new Color(0.24f, 0.85f, 0.45f));

        float pad = 10f;
        float headerH = 34f;

        // Title Header
        GUIStyle gridTitle = new GUIStyle(GUI.skin.label);
        gridTitle.fontSize = Mathf.Max(11, (int)(area.width * 0.044f));
        gridTitle.fontStyle = FontStyle.Bold;
        gridTitle.alignment = TextAnchor.MiddleLeft;
        gridTitle.normal.textColor = new Color(0.70f, 0.95f, 0.65f, 0.95f);

        string rosterTitle = LanguageSelectionUI.Localize("GUARDIANS OF THE WILD", "حراس البرية", "СТРАЖИ ДИКОЙ ПРИРОДЫ", "GARDIENS DE LA NATURE", "GUARDIANES DE LA NATURALEZA", "WÄCHTER DER WILDNIS", "जंगल के संरक्षक", "荒野守护者", "大自然の守護者", "GUARDIÕES DA NATUREZA", "نگهبانان طبیعت وحشی");
        GUI.Label(new Rect(area.x + pad, area.y + 6f, area.width * 0.65f, 24f), rosterTitle, gridTitle);

        // Filter Matching Characters
        List<CharacterData> filteredList = new List<CharacterData>();
        for (int i = 0; i < database.characters.Length; i++)
        {
            CharacterData c = database.characters[i];
            if (c == null) continue;
            if (selectedRoleFilter < 0 || (int)c.role == selectedRoleFilter)
            {
                filteredList.Add(c);
            }
        }

        GUIStyle countStyle = new GUIStyle(GUI.skin.label);
        countStyle.fontSize = Mathf.Max(9, (int)(area.width * 0.032f));
        countStyle.alignment = TextAnchor.MiddleRight;
        countStyle.normal.textColor = new Color(0.85f, 0.78f, 0.35f, 0.85f);
        GUI.Label(new Rect(area.x, area.y + 8f, area.width - pad, 20f), $"{filteredList.Count} " + LanguageSelectionUI.Localize("Ready", "جاهزون", "готовы", "Prêts", "Listos", "Bereit", "तैयार", "就绪", "待機中", "Prontos", "آماده"), countStyle);

        // Separator line
        GUI.color = new Color(0.35f, 0.65f, 0.40f, 0.45f);
        GUI.DrawTexture(new Rect(area.x + pad, area.y + headerH, area.width - pad * 2, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Scrollable 2-Column Monolith Cards
        float scrollY = area.y + headerH + 4f;
        float scrollH = area.height - headerH - 8f;
        float innerW = area.width - pad * 2f;

        const int COLUMNS = 2;
        float cardSpacing = 6f;
        float cardW = (innerW - cardSpacing) / COLUMNS;
        float cardH = cardW * 1.52f;

        int totalRows = Mathf.CeilToInt((float)filteredList.Count / COLUMNS);
        float totalVirtualH = totalRows * (cardH + cardSpacing) + 12f;

        gridScrollPosition = GUI.BeginScrollView(
            new Rect(area.x + pad, scrollY, innerW + 16f, scrollH),
            gridScrollPosition,
            new Rect(0, 0, innerW, totalVirtualH)
        );

        int frameHovered = -1;

        for (int i = 0; i < filteredList.Count; i++)
        {
            CharacterData ch = filteredList[i];
            if (ch == null) continue;

            int col = i % COLUMNS;
            int row = i / COLUMNS;

            float cx = col * (cardW + cardSpacing);
            float cy = row * (cardH + cardSpacing);

            Rect cardRect = new Rect(cx, cy, cardW, cardH);
            bool isSelected = (selectedCharacterID == ch.characterID);
            bool isHovered = cardRect.Contains(Event.current.mousePosition);

            if (isHovered) frameHovered = ch.characterID;

            float animT = (cardAnimTimers != null && ch.characterID < cardAnimTimers.Length) ? cardAnimTimers[ch.characterID] : 0f;

            DrawCharacterCard(cardRect, ch, isSelected, isHovered, animT);

            if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
            {
                selectedCharacterID = ch.characterID;
                selectionAnimTime = 0f;
                currentSkinIndex = 0;
                skinEquipped = false;
                availableSkins = CharacterSkinDatabase.GetSkinsForCharacter(ch.characterID);
                if (availableSkins != null && availableSkins.Length > 0)
                    currentSkin = availableSkins[0];
            }
        }

        GUI.EndScrollView();
        hoveredCharacterID = frameHovered;
    }

    // ==================== MONOLITH CHARACTER CARD ====================
    void DrawCharacterCard(Rect rect, CharacterData ch, bool isSelected, bool isHovered, float animT)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        float lift = animT * 3f;
        Rect drawRect = new Rect(rect.x, rect.y - lift, rect.width, rect.height);

        // Soft drop shadow on hover
        if (animT > 0.01f)
        {
            GUI.color = new Color(0, 0, 0, 0.45f * animT);
            GUI.DrawTexture(new Rect(drawRect.x + 2, drawRect.y + drawRect.height - 1, drawRect.width, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Obsidian Glass Body
        float bgBright = Mathf.Lerp(0.045f, 0.12f, animT);
        Color bgCol = isSelected
            ? new Color(roleColor.r * 0.22f, roleColor.g * 0.22f, roleColor.b * 0.22f, 0.98f)
            : new Color(bgBright * 0.7f, bgBright, bgBright * 0.8f, 0.94f);
        GUI.color = bgCol;
        GUI.DrawTexture(drawRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Crest Area (Top 40% of Card)
        float crestH = drawRect.height * 0.40f;
        Rect crestRect = new Rect(drawRect.x, drawRect.y, drawRect.width, crestH);

        // Top role gradient banner
        GUI.color = new Color(roleColor.r * 0.35f, roleColor.g * 0.35f, roleColor.b * 0.35f, 0.75f);
        GUI.DrawTexture(crestRect, gradientTex, ScaleMode.StretchToFill);
        GUI.color = Color.white;

        // Class Heraldry Emoji & Glow
        GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
        iconStyle.fontSize = Mathf.Max(16, (int)(crestH * 0.48f));
        iconStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(crestRect, GetRoleEmoji(ch.role), iconStyle);

        // Hero Name Area (Height: 22px, clearly separated without cutting line)
        float textY = drawRect.y + crestH + 4f;
        string localizedCharName = LanguageSelectionUI.Localize(ch.characterName);

        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
        int nameFont = (localizedCharName.Length > 12) ? 10 : 12;
        nameStyle.fontSize = nameFont;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.alignment = TextAnchor.MiddleCenter;
        nameStyle.wordWrap = false;
        nameStyle.clipping = TextClipping.Clip;
        nameStyle.normal.textColor = isSelected ? new Color(1f, 0.92f, 0.45f) : new Color(0.92f, 0.96f, 0.90f);

        Rect nameRect = new Rect(drawRect.x + 3, textY, drawRect.width - 6, 22f);
        GUI.Label(nameRect, localizedCharName, nameStyle);

        // Role Pill Badge (Neat pill with background and border - ZERO overlap!)
        float roleY = textY + 24f;
        float pillW = drawRect.width * 0.84f;
        float pillH = 17f;
        Rect pillRect = new Rect(drawRect.x + (drawRect.width - pillW) / 2f, roleY, pillW, pillH);

        GUI.color = new Color(roleColor.r * 0.25f, roleColor.g * 0.25f, roleColor.b * 0.25f, 0.85f);
        GUI.DrawTexture(pillRect, Texture2D.whiteTexture);
        DrawBorder(pillRect, new Color(roleColor.r, roleColor.g, roleColor.b, 0.70f), 1f);
        GUI.color = Color.white;

        GUIStyle roleStyle = new GUIStyle(GUI.skin.label);
        roleStyle.fontSize = 8;
        roleStyle.fontStyle = FontStyle.Bold;
        roleStyle.alignment = TextAnchor.MiddleCenter;
        roleStyle.normal.textColor = Color.white;
        GUI.Label(pillRect, GetRoleName(ch.role), roleStyle);

        // Mini Stat Preview Pips (Health & Damage) cleanly at bottom
        float barY = drawRect.y + drawRect.height - 18f;
        float barH = 3f;
        float barW = drawRect.width * 0.76f;
        float barX = drawRect.x + drawRect.width * 0.12f;

        float hpNorm = Mathf.Clamp01(ch.maxHealth / 400f);
        GUI.color = new Color(0.08f, 0.15f, 0.08f, 0.7f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.85f, 0.35f, 0.9f);
        GUI.DrawTexture(new Rect(barX, barY, barW * hpNorm, barH), Texture2D.whiteTexture);

        float dmgNorm = Mathf.Clamp01(ch.attackDamage / 40f);
        GUI.color = new Color(0.08f, 0.15f, 0.08f, 0.7f);
        GUI.DrawTexture(new Rect(barX, barY + 5f, barW, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.95f, 0.30f, 0.20f, 0.9f);
        GUI.DrawTexture(new Rect(barX, barY + 5f, barW * dmgNorm, barH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Selection & Hover Borders
        if (isSelected)
        {
            float selPulse = 0.8f + 0.2f * Mathf.Sin(selectionAnimTime * 2.8f);
            Color selBorder = new Color(roleColor.r * selPulse, roleColor.g * selPulse, roleColor.b * selPulse, 0.95f);
            DrawBorder(drawRect, selBorder, 2f);
            EnchantedNatureGUI.DrawCornerFiligree(drawRect, new Color(1f, 0.88f, 0.4f, selPulse), 10f);
        }
        else if (isHovered)
        {
            Color hoverBorder = new Color(0.5f, 0.9f, 0.6f, 0.75f + 0.25f * Mathf.Sin(hoverAnimTime));
            DrawBorder(drawRect, hoverBorder, 1.5f);
        }
        else
        {
            DrawBorder(drawRect, new Color(roleColor.r * 0.45f, roleColor.g * 0.45f, roleColor.b * 0.45f, 0.4f), 1f);
        }
    }

    // ==================== COLUMN 2: CENTER GRAND 3D HERO STAGE ====================
    void DrawCenterHeroStage(Rect area, CharacterData ch)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        // Antique Gold Frame for Center Stage
        EnchantedNatureGUI.DrawAntiqueGoldFrame(area, roleColor);

        // 3D Render Viewport inside stage
        float stagePad = 6f;
        Rect renderRect = new Rect(area.x + stagePad, area.y + stagePad, area.width - stagePad * 2, area.height - stagePad * 2);

        if (previewRenderTexture != null)
        {
            GUI.DrawTexture(renderRect, previewRenderTexture, ScaleMode.ScaleToFit);
        }

        // Interactive Mouse Drag for 360-Degree Turntable Rotation
        Event e = Event.current;
        if (renderRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isDraggingModel = true;
                lastMouseX = e.mousePosition.x;
            }
        }
        if (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp)
        {
            isDraggingModel = false;
        }
        if (isDraggingModel && e.type == EventType.MouseDrag && turntable != null)
        {
            float deltaX = e.mousePosition.x - lastMouseX;
            lastMouseX = e.mousePosition.x;
            turntable.Rotate(Vector3.up * -deltaX * 0.65f, Space.World);
        }

        // Top Floating Hero Title Watermark (Compact so it never hides champion's head)
        string locName = LanguageSelectionUI.Localize(ch.characterName).ToUpper();
        string titleWatermark = $"✦ {locName} ✦";

        GUIStyle wmStyle = new GUIStyle(GUI.skin.label);
        wmStyle.fontSize = Mathf.Max(14, (int)(area.width * 0.052f));
        wmStyle.fontStyle = FontStyle.Bold;
        wmStyle.alignment = TextAnchor.MiddleCenter;
        wmStyle.normal.textColor = new Color(1.0f, 0.90f, 0.45f, 0.95f);
        GUI.Label(new Rect(area.x, area.y + 8f, area.width, 24f), titleWatermark, wmStyle);

        // Floating Role Insignia Plaque below title
        string roleTag = GetRoleEmoji(ch.role) + "  " + GetRoleName(ch.role);
        float badgeW = Mathf.Min(180f, area.width * 0.48f);
        float badgeH = 20f;
        Rect badgeRect = new Rect(area.x + (area.width - badgeW) / 2f, area.y + 34f, badgeW, badgeH);

        GUI.color = new Color(roleColor.r * 0.22f, roleColor.g * 0.22f, roleColor.b * 0.22f, 0.82f);
        GUI.DrawTexture(badgeRect, Texture2D.whiteTexture);
        DrawBorder(badgeRect, new Color(roleColor.r, roleColor.g, roleColor.b, 0.85f), 1.2f);
        GUI.color = Color.white;

        GUIStyle roleBadgeStyle = new GUIStyle(GUI.skin.label);
        roleBadgeStyle.fontSize = Mathf.Max(9, (int)(area.width * 0.030f));
        roleBadgeStyle.fontStyle = FontStyle.Bold;
        roleBadgeStyle.alignment = TextAnchor.MiddleCenter;
        roleBadgeStyle.normal.textColor = Color.white;
        GUI.Label(badgeRect, roleTag, roleBadgeStyle);

        // Bottom Drag-to-Rotate Hint
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = Mathf.Max(8, (int)(area.width * 0.026f));
        hintStyle.alignment = TextAnchor.LowerCenter;
        hintStyle.normal.textColor = new Color(0.65f, 0.85f, 0.70f, 0.75f);
        hintStyle.fontStyle = FontStyle.Italic;

        string hintStr = LanguageSelectionUI.Localize(
            "✦ Drag mouse horizontally to rotate champion ✦",
            ar: "✦ اسحب بالفأرة أفقياً لتدوير البطل ✦",
            ru: "✦ Перетащите мышь для вращения героя ✦",
            fr: "✦ Glissez la souris pour faire pivoter ✦",
            es: "✦ Arrastra el ratón para rotar al héroe ✦",
            de: "✦ Ziehen zum Drehen des Helden ✦",
            hi: "✦ घुमाने के लिए माउस खींचें ✦",
            zh: "✦ 按住鼠标水平拖动以旋转英雄 ✦",
            ja: "✦ ドラッグして英雄を回転 ✦",
            pt: "✦ Arraste o mouse para girar o campeão ✦",
            fa: "✦ برای چرخاندن قهرمان، ماوس را بکشید ✦"
        );
        GUI.Label(new Rect(area.x, area.y + area.height - 24f, area.width, 20f), hintStr, hintStyle);
    }

    // ==================== COLUMN 3: RIGHT COMBAT CODEX & SACRED SKINS ====================
    void DrawCharacterDetails(Rect area, CharacterData ch)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        // Frame
        EnchantedNatureGUI.DrawAntiqueGoldFrame(area, roleColor);

        // Auto-load available skins and saved skin for this character
        if (availableSkins == null || availableSkins.Length == 0 || lastDetailsCharID != ch.characterID)
        {
            lastDetailsCharID = ch.characterID;
            availableSkins = CharacterSkinDatabase.GetSkinsForCharacter(ch.characterID);
            int savedSkin = PlayerProfile.Instance != null ? PlayerProfile.Instance.GetSelectedSkin(ch.characterID) : 0;
            currentSkinIndex = Mathf.Clamp(savedSkin, 0, availableSkins.Length - 1);
            currentSkin = availableSkins[currentSkinIndex];
            skinEquipped = (PlayerProfile.Instance == null || PlayerProfile.Instance.IsSkinUnlocked(ch.characterID, currentSkinIndex));
            ApplySkinToPreviewModel(ch, currentSkin);
        }

        float pad = 10f;

        pad = 8f;
        float headerH = 38f;

        // Pinned Dual-Tab Switcher at Top of Right Column
        float tabW = (area.width - pad * 2f - 4f) / 2f;
        Rect tab0Rect = new Rect(area.x + pad, area.y + 6f, tabW, 26f);
        Rect tab1Rect = new Rect(area.x + pad + tabW + 4f, area.y + 6f, tabW, 26f);

        string attrTabStr = "⚔️ " + LanguageSelectionUI.Localize("ATTRIBUTES", "إحصائيات القتال", "ХАРАКТЕРИСТИКИ", "ATRIBUTOS", "ATTRIBUTS", "ATTRIBUTE", "विशेषताएँ", "战斗属性", "ステータス", "ATRIBUTOS", "مشخصات رزمی");
        string skinsTabStr = "👑 " + LanguageSelectionUI.Localize("WARDROBE", "المظاهر والأسلحة", "ОБЛИКИ", "ASPECTOS", "SKINS SACRÉS", "SKINS", "पवित्र खाल", "神圣皮肤", "聖なるスキン", "SKINS SAGRADAS", "پوسته‌ها و سلاح‌ها");

        Color tab0Col = (selectedDetailsTab == 0) ? new Color(0.25f, 0.95f, 0.50f) : new Color(0.22f, 0.32f, 0.26f);
        Color tab1Col = (selectedDetailsTab == 1) ? new Color(1.0f, 0.85f, 0.35f) : new Color(0.32f, 0.28f, 0.22f);

        if (EnchantedNatureGUI.DrawOrganicLeafButton(tab0Rect, attrTabStr, tab0Col, 10))
        {
            selectedDetailsTab = 0;
        }
        if (EnchantedNatureGUI.DrawOrganicLeafButton(tab1Rect, skinsTabStr, tab1Col, 10))
        {
            selectedDetailsTab = 1;
        }

        // Separator line below tabs
        GUI.color = new Color(0.35f, 0.65f, 0.40f, 0.45f);
        GUI.DrawTexture(new Rect(area.x + pad, area.y + headerH, area.width - pad * 2, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Pinned Bottom Section: Grand Expedition Confirmation Gateway (Height 76px)
        float footerH = 76f;
        float footerY = area.y + area.height - footerH - 4f;

        // Separator above footer
        GUI.color = new Color(0.35f, 0.65f, 0.40f, 0.45f);
        GUI.DrawTexture(new Rect(area.x + pad, footerY - 2f, area.width - pad * 2, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Player Economy Status
        int curCoins = PlayerProfile.Instance != null ? PlayerProfile.Instance.Coins : 0;
        int curGems = PlayerProfile.Instance != null ? PlayerProfile.Instance.Gems : 0;
        int curLevel = PlayerProfile.Instance != null ? PlayerProfile.Instance.AccountLevel : 1;

        GUIStyle ecoStyle = new GUIStyle(GUI.skin.label);
        ecoStyle.fontSize = Mathf.Max(8, (int)(area.width * 0.026f));
        ecoStyle.alignment = TextAnchor.MiddleCenter;
        ecoStyle.fontStyle = FontStyle.Bold;

        if (skinNotificationTimer > 0f)
        {
            ecoStyle.normal.textColor = new Color(0.35f, 1f, 0.5f);
            GUI.Label(new Rect(area.x + pad, footerY + 2f, area.width - pad * 2, 16f), skinNotification, ecoStyle);
        }
        else
        {
            ecoStyle.normal.textColor = new Color(0.92f, 0.88f, 0.7f);
            string ecoStr = $"🪙 {curCoins} Coins   |   💎 {curGems} Gems   |   ⭐ Lv.{curLevel}";
            GUI.Label(new Rect(area.x + pad, footerY + 2f, area.width - pad * 2, 16f), ecoStr, ecoStyle);
        }

        // Pinned Grand Confirmation Button
        float confirmH = 46f;
        Rect confirmRect = new Rect(area.x + pad, footerY + 22f, area.width - pad * 2, confirmH);
        string locChar = LanguageSelectionUI.Localize(ch.characterName).ToUpper();
        string buttonText = "🌿 " + LanguageSelectionUI.Localize(
            "COMMENCE EXPEDITION",
            ar: "انطلق في الحملة",
            ru: "НАЧАТЬ ПОХОД",
            fr: "COMMENCER L'EXPÉDITION",
            es: "INICIAR EXPEDICIÓN",
            de: "EXPEDITION STARTEN",
            hi: "अभियान शुरू करें",
            zh: "开始远征",
            ja: "遠征を開始",
            pt: "INICIAR EXPEDIÇÃO",
            fa: "آغاز نبرد"
        ) + $" ({locChar}) 🌿";

        if (EnchantedNatureGUI.DrawOrganicLeafButton(confirmRect, buttonText, new Color(0.25f, 0.95f, 0.45f), 12))
        {
            selectedCharacterID = ch.characterID;
            ConfirmSelection();
        }

        // Middle Scrollable Area: Dedicated Tab View
        float scrollY = area.y + headerH + 2f;
        float scrollH = footerY - scrollY - 2f;
        Rect scrollRect = new Rect(area.x + 4f, scrollY, area.width - 8f, scrollH);

        if (selectedDetailsTab == 0)
        {
            DrawAttributesTab(scrollRect, ch);
        }
        else
        {
            DrawSkinsTab(scrollRect, ch);
        }
    }

    // ==================== ATTRIBUTES CODEX TAB ====================
    void DrawAttributesTab(Rect scrollRect, CharacterData ch)
    {
        float innerW = scrollRect.width - 16f;
        float totalVirtualH = 460f;
        Rect viewRect = new Rect(0, 0, innerW, totalVirtualH);

        detailsScrollPosition = GUI.BeginScrollView(scrollRect, detailsScrollPosition, viewRect, false, true);

        float curY = 4f;

        // 1. Archetype & Squad Capacity Ribbon
        float ribH = 34f;
        Rect ribRect = new Rect(0, curY, innerW, ribH);
        GUI.color = new Color(0.04f, 0.08f, 0.06f, 0.88f);
        GUI.DrawTexture(ribRect, Texture2D.whiteTexture);
        Color roleCol = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;
        DrawBorder(ribRect, new Color(roleCol.r * 0.5f, roleCol.g * 0.5f, roleCol.b * 0.5f, 0.6f), 1f);
        GUI.color = Color.white;

        // Role & Attack Type inside Ribbon
        GUIStyle ribRoleStyle = new GUIStyle(GUI.skin.label);
        ribRoleStyle.fontSize = Mathf.Clamp((int)(innerW * 0.038f), 9, 11);
        ribRoleStyle.fontStyle = FontStyle.Bold;
        ribRoleStyle.alignment = TextAnchor.MiddleLeft;
        ribRoleStyle.normal.textColor = Color.white;
        string ribRoleStr = $"{GetRoleEmoji(ch.role)} {GetRoleName(ch.role)}  •  {GetAttackTypeLabel(ch.attackType)}";
        GUI.Label(new Rect(8f, curY + 2f, innerW * 0.68f, 30f), ribRoleStr, ribRoleStyle);

        // Max Squad Limit
        GUIStyle ribLimitStyle = new GUIStyle(GUI.skin.label);
        ribLimitStyle.fontSize = Mathf.Clamp((int)(innerW * 0.032f), 8, 10);
        ribLimitStyle.alignment = TextAnchor.MiddleRight;
        ribLimitStyle.normal.textColor = new Color(0.85f, 0.80f, 0.45f);
        string maxStr = LanguageSelectionUI.Localize("👥 Squad Limit: ", "👥 حد الفريق: ", "👥 Лимит отряда: ", "👥 Límite: ", "👥 Limite d'équipe : ", "👥 Trupplimit: ", "👥 दस्ता सीमा: ", "👥 小队上限: ", "👥 チーム上限: ", "👥 Limite: ", "👥 سقف تیم: ") + ch.maxPerTeam;
        GUI.Label(new Rect(innerW * 0.62f, curY + 2f, innerW * 0.38f - 8f, 30f), maxStr, ribLimitStyle);

        curY += ribH + 6f;

        // 2. Six Luxury RPG Attribute Cards (Height 44px each, 5px spacing)
        float cardH = 44f;
        float cardGap = 5f;

        // Health
        string hpBonus = (skinEquipped && currentSkin != null && currentSkin.healthMultiplier != 1f)
            ? $" (+{(currentSkin.healthMultiplier - 1f) * 100:F0}%)" : "";
        string hpBadge = (ch.maxHealth >= 280f)
            ? LanguageSelectionUI.Localize("JUGGERNAUT", "درع صامد", "ДЖАГГЕРНАУТ", "IMPARABLE", "COLOSSE", "KOLOSS", "अजेय", "霸体重装", "巨漢", "COLOSSO", "زره مستحکم")
            : ((ch.maxHealth >= 180f)
                ? LanguageSelectionUI.Localize("STURDY", "صلب", "СТОЙКИЙ", "ROBUSTO", "ROBUSTE", "ROBUST", "मजबूत", "坚韧", "頑丈", "ROBUSTO", "مقاوم")
                : LanguageSelectionUI.Localize("AGILE", "رشيق", "ЛОВКИЙ", "ÁGIL", "AGILE", "WENDIG", "फुर्तीला", "灵动", "敏捷", "ÁGIL", "چابک"));
        Color hpBadgeCol = (ch.maxHealth >= 280f) ? new Color(1f, 0.85f, 0.35f) : ((ch.maxHealth >= 180f) ? new Color(0.35f, 0.9f, 0.45f) : new Color(0.35f, 0.75f, 0.95f));

        DrawLuxuryAttributeCard(
            new Rect(0, curY, innerW, cardH),
            "❤️",
            LanguageSelectionUI.Localize("MAX HEALTH", "الصحة القصوى", "МАКС. ЗДОРОВЬЕ", "SALUD MÁXIMA", "SANTÉ MAXIMALE", "MAX. GESUNDHEIT", "अधिकतम स्वास्थ्य", "最大生命", "最大HP", "VIDA MÁXIMA", "حداکثر سلامت"),
            ch.maxHealth.ToString("F0") + " HP",
            hpBonus,
            ch.maxHealth / 400f,
            new Color(0.25f, 0.88f, 0.40f),
            hpBadge,
            hpBadgeCol
        );
        curY += cardH + cardGap;

        // Attack Damage
        string dmgBonus = (skinEquipped && currentSkin != null && currentSkin.damageMultiplier != 1f)
            ? $" (+{(currentSkin.damageMultiplier - 1f) * 100:F0}%)" : "";
        string dmgBadge = (ch.attackDamage >= 32f)
            ? LanguageSelectionUI.Localize("LETHAL", "فتاك", "СМЕРТОНОСНЫЙ", "LETAL", "MORTEL", "TÖDLICH", "घातक", "毁灭性", "致命的", "LETAL", "مرگبار")
            : ((ch.attackDamage >= 22f)
                ? LanguageSelectionUI.Localize("POTENT", "قوي", "СИЛЬНЫЙ", "POTENTE", "PUISSANT", "KRAFTVOLL", "शक्तिशाली", "强劲", "強力", "POTENTE", "قدرتمند")
                : LanguageSelectionUI.Localize("BALANCED", "متوازن", "БАЛАНС", "EQUILIBRADO", "ÉQUILIBRÉ", "AUSGEWOGEN", "संतुलित", "均衡", "バランス", "EQUILIBRADO", "متعادل"));
        Color dmgBadgeCol = (ch.attackDamage >= 32f) ? new Color(0.95f, 0.25f, 0.20f) : ((ch.attackDamage >= 22f) ? new Color(0.98f, 0.55f, 0.20f) : new Color(0.85f, 0.80f, 0.35f));

        DrawLuxuryAttributeCard(
            new Rect(0, curY, innerW, cardH),
            "⚔️",
            LanguageSelectionUI.Localize("ATTACK DAMAGE", "القوة الهجومية", "СИЛА АТАКИ", "DAÑO DE ATAQUE", "DÉGÂTS D'ATTAQUE", "ANGRIFFSSCHADEN", "हमला क्षति", "攻击伤害", "攻撃力", "DANO DE ATAQUE", "قدرت حمله"),
            ch.attackDamage.ToString("F0") + " ATK",
            dmgBonus,
            ch.attackDamage / 40f,
            new Color(0.95f, 0.30f, 0.20f),
            dmgBadge,
            dmgBadgeCol
        );
        curY += cardH + cardGap;

        // Movement Speed
        string spdBonus = (skinEquipped && currentSkin != null && currentSkin.speedMultiplier != 1f)
            ? $" (+{(currentSkin.speedMultiplier - 1f) * 100:F0}%)" : "";
        string spdBadge = (ch.moveSpeed >= 7.5f)
            ? LanguageSelectionUI.Localize("WINDRUNNER", "سريع جداً", "БЫСТРЫЙ", "VELOZ", "TRÈS RAPIDE", "BLITZSCHNELL", "अति तीव्र", "迅捷如风", "疾風", "VELOZ", "بسیار سریع")
            : ((ch.moveSpeed >= 6.0f)
                ? LanguageSelectionUI.Localize("SWIFT", "رشيق", "ПРОВОРНЫЙ", "ÁGIL", "RAPIDE", "SCHNELL", "तेज", "敏捷", "素早い", "RÁPIDO", "سریع")
                : LanguageSelectionUI.Localize("HEAVY", "ثقيل", "ТЯЖЕЛЫЙ", "PESADO", "LOURD", "SCHWER", "भारी", "沉重", "重装", "PESADO", "سنگین"));
        Color spdBadgeCol = (ch.moveSpeed >= 7.5f) ? new Color(0.20f, 0.90f, 0.95f) : ((ch.moveSpeed >= 6.0f) ? new Color(0.35f, 0.75f, 0.95f) : new Color(0.50f, 0.55f, 0.75f));

        DrawLuxuryAttributeCard(
            new Rect(0, curY, innerW, cardH),
            "⚡",
            LanguageSelectionUI.Localize("MOVE SPEED", "سرعة التحرك", "СКОРОСТЬ ПЕРЕДВИЖЕНИЯ", "VELOCIDAD DE MOV.", "VITESSE DE DÉPL.", "LAUFGESCHWINDIGKEIT", "गति", "移动速度", "移動速度", "VELOCIDADE DE MOV.", "سرعت حرکت"),
            ch.moveSpeed.ToString("F1") + " m/s",
            spdBonus,
            ch.moveSpeed / 12f,
            new Color(0.25f, 0.78f, 0.95f),
            spdBadge,
            spdBadgeCol
        );
        curY += cardH + cardGap;

        // Combat Range
        string rngBadge = (ch.attackRange >= 10f)
            ? LanguageSelectionUI.Localize("SNIPER", "قناص بعيد", "СНАЙПЕР", "FRANCOTIRADOR", "SNIPER", "SCHARFSCHÜTZE", "निशानेबाज", "超远射程", "狙撃", "SNIPER", "تک‌تیرانداز")
            : ((ch.attackRange >= 4f)
                ? LanguageSelectionUI.Localize("MID-RANGE", "مدى متوسط", "СРЕДНЯЯ ДИСТ.", "DISTANCIA MEDIA", "PORTÉE MOYENNE", "MITTLERE REICHWEITE", "मध्यम सीमा", "中程", "中距離", "MÉDIO ALCANCE", "برد متوسط")
                : LanguageSelectionUI.Localize("MELEE", "اشتباك مباشر", "БЛИЖНИЙ БОЙ", "CUERPO A CUERPO", "CORPS À CORPS", "NAHKAMPF", "हाथापाई", "近战肉搏", "近接格闘", "CORPO A CORPO", "نبرد نزدیک"));
        Color rngBadgeCol = (ch.attackRange >= 10f) ? new Color(0.95f, 0.78f, 0.25f) : ((ch.attackRange >= 4f) ? new Color(0.85f, 0.65f, 0.35f) : new Color(0.75f, 0.45f, 0.25f));

        DrawLuxuryAttributeCard(
            new Rect(0, curY, innerW, cardH),
            "🏹",
            LanguageSelectionUI.Localize("COMBAT RANGE", "مدى الاشتباك", "ДАЛЬНОСТЬ БОЯ", "RANGO DE COMBATE", "PORTÉE DE COMBAT", "KAMPFREICHWEITE", "युद्ध सीमा", "攻击射程", "射程", "ALCANCE DE COMBATE", "برد درگیری"),
            ch.attackRange.ToString("F1") + " m",
            "",
            ch.attackRange / 20f,
            new Color(0.95f, 0.75f, 0.25f),
            rngBadge,
            rngBadgeCol
        );
        curY += cardH + cardGap;

        // Attack Cadence & Cooldown
        float attacksPerSec = 1f / Mathf.Max(0.1f, ch.attackCooldown);
        string cdBadge = (ch.attackCooldown <= 0.8f)
            ? LanguageSelectionUI.Localize("RAPID FLURRY", "ضربات متتالية", "БЫСТРЫЕ УДАРЫ", "RÁFAGA RÁPIDA", "RAFALE RAPIDE", "SCHNELLE SCHLÄGE", "तीव्र प्रहार", "连贯连击", "連撃", "GOLPES RÁPIDOS", "ضربات پیاپی")
            : ((ch.attackCooldown <= 1.2f)
                ? LanguageSelectionUI.Localize("MEASURED", "متزن", "УМЕРЕННЫЙ", "MEDIDO", "CADENCÉ", "GEMESSEN", "मापा हुआ", "匀速", "安定", "COMPASSADO", "سنجیده")
                : LanguageSelectionUI.Localize("POWER STRIKE", "ضربة قاضية", "МОЩНЫЙ УДАР", "GOLPE PODEROSO", "FRAPPE PUISSANTE", "KRAFTSCHLAG", "शक्ति प्रहार", "强力重击", "強打", "GOLPE FORTE", "ضربه سنگین"));
        Color cdBadgeCol = (ch.attackCooldown <= 0.8f) ? new Color(0.75f, 0.45f, 0.95f) : ((ch.attackCooldown <= 1.2f) ? new Color(0.65f, 0.55f, 0.85f) : new Color(0.85f, 0.35f, 0.65f));

        DrawLuxuryAttributeCard(
            new Rect(0, curY, innerW, cardH),
            "⏱️",
            LanguageSelectionUI.Localize("ATTACK CADENCE", "سرعة الضربات", "ТЕМП АТАКИ", "CADENCIA DE ATAQUE", "CADENCE D'ATTAQUE", "ANGRIFFSTEMPO", "हमला ताल", "攻击频率", "攻撃頻度", "CADÊNCIA DE ATAQUE", "آهنگ حمله"),
            ch.attackCooldown.ToString("F1") + "s (" + attacksPerSec.ToString("F1") + "/s)",
            "",
            Mathf.Clamp01(1f - (ch.attackCooldown / 3f)),
            new Color(0.78f, 0.45f, 0.95f),
            cdBadge,
            cdBadgeCol
        );
        curY += cardH + cardGap;

        // Jump & Mobility
        string jumpBadge = ch.canDoubleJump
            ? LanguageSelectionUI.Localize("DOUBLE JUMP", "قفز مزدوج متاح", "ДВОЙНОЙ ПРЫЖОК", "SALTO DOBLE ACTIVO", "DOUBLE SAUT ACTIF", "DOPPELSPRUNG AKTIV", "दोहरी छलांग सक्षम", "支持二段跳跃", "二段ジャンプ可能", "PULO DUPLO ATIVO", "پرش دوگانه فعال")
            : LanguageSelectionUI.Localize("SINGLE JUMP", "قفز أرضي ثابت", "СТАНДАРТНЫЙ ПРЫЖОК", "SALTO ESTÁNDAR", "SAUT STANDARD", "STANDARDSPRUNG", "मानक छलांग", "标准跳跃", "標準ジャンプ", "PULO PADRÃO", "پرش زمینی استاندارد");
        Color jumpBadgeCol = ch.canDoubleJump ? new Color(0.35f, 0.95f, 0.65f) : new Color(0.45f, 0.65f, 0.55f);

        DrawLuxuryAttributeCard(
            new Rect(0, curY, innerW, cardH),
            "🦘",
            LanguageSelectionUI.Localize("JUMP & MOBILITY", "القفز والقدرة الحركية", "ПРЫЖОК И МОБИЛЬНОСТЬ", "SALTO Y MOVILIDAD", "SAUT ET MOBILITÉ", "SPRUNG & MOBILITÄT", "छलांग और गतिशीलता", "跳跃与机动性", "跳躍と機動性", "SALTO E MOBILIDADE", "پرش و تحرک"),
            ch.jumpForce.ToString("F1") + " Force",
            "",
            ch.jumpForce / 16f,
            new Color(0.35f, 0.92f, 0.70f),
            jumpBadge,
            jumpBadgeCol
        );
        curY += cardH + 8f;

        // 3. Guardian Lore & Tactical Role Codex Card
        float loreH = 88f;
        Rect loreRect = new Rect(0, curY, innerW, loreH);

        GUI.color = new Color(0.04f, 0.07f, 0.05f, 0.90f);
        GUI.DrawTexture(loreRect, Texture2D.whiteTexture);
        DrawBorder(loreRect, new Color(0.70f, 0.60f, 0.25f, 0.60f), 1f);
        EnchantedNatureGUI.DrawCornerFiligree(loreRect, new Color(0.85f, 0.75f, 0.35f, 0.80f), 8f);
        GUI.color = Color.white;

        GUIStyle loreHeader = new GUIStyle(GUI.skin.label);
        loreHeader.fontSize = Mathf.Clamp((int)(innerW * 0.036f), 9, 11);
        loreHeader.fontStyle = FontStyle.Bold;
        loreHeader.normal.textColor = new Color(1.0f, 0.88f, 0.40f);
        string loreHeaderStr = "📜 " + LanguageSelectionUI.Localize(
            "GUARDIAN CODEX & TACTICAL ROLE",
            ar: "سجل الحارس والتكتيك القتالي",
            ru: "КОДЕКС СТРАЖА И ТАКТИЧЕСКАЯ РОЛЬ",
            fr: "CODEX DU GARDIEN ET RÔLE TACTIQUE",
            es: "CÓDICE DEL GUARDIÁN Y ROL TÁCTICO",
            de: "WÄCHTER-KODEX & TAKTISCHE ROLLE",
            hi: "संरक्षक संहिता और सामरिक भूमिका",
            zh: "守护者法典与战术定位",
            ja: "守護者の規範と戦術的役割",
            pt: "CÓDICE DO GUARDIÃO E PAPEL TÁTICO",
            fa: "طومار نگهبان و نقش تاکتیکی"
        );
        GUI.Label(new Rect(8f, curY + 6f, innerW - 16f, 18f), loreHeaderStr, loreHeader);

        GUIStyle loreBody = new GUIStyle(GUI.skin.label);
        loreBody.fontSize = Mathf.Clamp((int)(innerW * 0.030f), 8, 10);
        loreBody.wordWrap = true;
        loreBody.normal.textColor = new Color(0.80f, 0.90f, 0.82f, 0.90f);

        string loreText = !string.IsNullOrEmpty(ch.description)
            ? ch.description
            : LanguageSelectionUI.Localize(
                "A seasoned guardian of the colossal ancient tree, harnessing primordial magic and martial prowess to defend sacred altars and lead expeditions to the apex canopy.",
                ar: "حارس متمرس للشجرة القديمة العملاقة، يمتلك مهارات قتالية وسحراً مقدساً لحماية المذابح وقيادة الحملات نحو قمة الشجرة.",
                ru: "Опытный страж древнего исполинского древа, защищающий священные алтари и ведущий походы к вершинам кроны.",
                fr: "Un gardien aguerri de l'arbre colossal ancestral, défendant les autels sacrés et guidant les expéditions.",
                es: "Un experimentado guardián del árbol antiguo colosal, defensor de los altares sagrados.",
                de: "Ein erfahrener Wächter des kolossalen alten Baumes, der heilige Altäre verteidigt.",
                hi: "विशाल प्राचीन वृक्ष का एक अनुभवी संरक्षक, जो पवित्र वेदियों की रक्षा करता है।",
                zh: "巨树古国的资深守护者，运用神圣法力捍卫祭坛并引领登顶远征。",
                ja: "巨大な古樹の熟練の守護者。聖なる祭壇を守り、頂上への遠征を導く。",
                pt: "Um guardião veterano da colossal árvore antiga, defendendo os altares sagrados.",
                fa: "نگهبانی باتجربه از درخت کهنسال و غول‌پیکر که از محراب‌های مقدس محافظت می‌کند."
            );
        GUI.Label(new Rect(8f, curY + 26f, innerW - 16f, loreH - 30f), loreText, loreBody);

        curY += loreH + 12f;

        GUI.EndScrollView();
    }

    // ==================== SACRED WARDROBE TAB ====================
    void DrawSkinsTab(Rect scrollRect, CharacterData ch)
    {
        float innerW = scrollRect.width - 16f;
        int skinCount = (availableSkins != null) ? availableSkins.Length : 0;
        float totalVirtualH = Mathf.Max(380f, skinCount * 76f + 20f);
        Rect viewRect = new Rect(0, 0, innerW, totalVirtualH);

        detailsScrollPosition = GUI.BeginScrollView(scrollRect, detailsScrollPosition, viewRect, false, true);

        float curY = 4f;

        if (availableSkins != null)
        {
            for (int s = 0; s < availableSkins.Length; s++)
            {
                CharacterSkin skin = availableSkins[s];
                if (skin == null) continue;

                float cardH = 70f;
                Rect cardR = new Rect(0, curY, innerW, cardH);

                bool isUnlocked = (PlayerProfile.Instance == null) || PlayerProfile.Instance.IsSkinUnlocked(ch.characterID, s);
                bool isEquipped = (s == currentSkinIndex && skinEquipped);
                bool isPreviewing = (s == currentSkinIndex);

                // Card Background
                Color cardBg = isEquipped
                    ? new Color(0.08f, 0.22f, 0.12f, 0.95f)
                    : (isPreviewing ? new Color(0.12f, 0.16f, 0.14f, 0.95f) : new Color(0.05f, 0.08f, 0.06f, 0.92f));
                GUI.color = cardBg;
                GUI.DrawTexture(cardR, Texture2D.whiteTexture);

                Color borderCol = isEquipped
                    ? new Color(0.35f, 0.98f, 0.50f, 0.95f)
                    : (isUnlocked ? new Color(0.4f, 0.7f, 0.5f, 0.6f) : new Color(0.35f, 0.35f, 0.35f, 0.5f));
                DrawBorder(cardR, borderCol, isEquipped ? 2f : 1f);
                GUI.color = Color.white;

                // Left: Skin Color Swatch
                float iconBoxW = 36f;
                Rect iconBox = new Rect(cardR.x + 6f, cardR.y + 7f, iconBoxW, cardH - 14f);
                GUI.color = skin.skinColor;
                GUI.DrawTexture(iconBox, Texture2D.whiteTexture);
                DrawBorder(iconBox, Color.black, 1f);
                GUI.color = Color.white;

                string skinEmoji = (s == 0) ? "🛡️" : (s == 1 ? "🌿" : (s == 2 ? "🔥" : (s == 3 ? "⚡" : "👑")));
                GUIStyle sIconStyle = new GUIStyle(GUI.skin.label);
                sIconStyle.fontSize = 14;
                sIconStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(iconBox, skinEmoji, sIconStyle);

                // Middle: Name, Stat Buffs, and Description
                float textX = cardR.x + iconBoxW + 10f;
                float btnW = 98f;
                float textW = cardR.width - iconBoxW - btnW - 18f;

                GUIStyle sNameStyle = new GUIStyle(GUI.skin.label);
                sNameStyle.fontSize = Mathf.Clamp((int)(innerW * 0.034f), 9, 11);
                sNameStyle.fontStyle = FontStyle.Bold;
                sNameStyle.normal.textColor = isEquipped ? new Color(0.4f, 1f, 0.55f) : (isUnlocked ? new Color(0.95f, 0.88f, 0.5f) : new Color(0.7f, 0.7f, 0.7f));
                GUI.Label(new Rect(textX, cardR.y + 4f, textW, 18f), skin.skinName, sNameStyle);

                // Stat Buff tags
                string buffText = "";
                if (skin.healthMultiplier > 1f) buffText += $"+{(skin.healthMultiplier - 1f) * 100:F0}% HP  ";
                if (skin.damageMultiplier > 1f) buffText += $"+{(skin.damageMultiplier - 1f) * 100:F0}% ATK  ";
                if (skin.speedMultiplier > 1f) buffText += $"+{(skin.speedMultiplier - 1f) * 100:F0}% SPD  ";
                if (string.IsNullOrEmpty(buffText)) buffText = LanguageSelectionUI.Localize("Standard Base Attributes", "السمات الأساسية القياسية", "Базовые характеристики", "Attributs de base", "Atributos básicos", "Standard-Attribute", "मानक आधार विशेषताएँ", "标准基础属性", "基本ステータス", "Atributos Básicos", "ویژگی‌های پایه استاندارد");

                GUIStyle buffStyle = new GUIStyle(GUI.skin.label);
                buffStyle.fontSize = Mathf.Clamp((int)(innerW * 0.026f), 8, 9);
                buffStyle.fontStyle = FontStyle.Bold;
                buffStyle.normal.textColor = new Color(0.35f, 0.95f, 0.45f);
                GUI.Label(new Rect(textX, cardR.y + 22f, textW, 16f), buffText, buffStyle);

                GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                descStyle.fontSize = Mathf.Clamp((int)(innerW * 0.024f), 7, 8);
                descStyle.normal.textColor = new Color(0.75f, 0.82f, 0.75f, 0.80f);
                descStyle.wordWrap = true;
                GUI.Label(new Rect(textX, cardR.y + 38f, textW, 28f), skin.description, descStyle);

                // Right: Action Button
                Rect btnRect = new Rect(cardR.x + cardR.width - btnW - 6f, cardR.y + (cardH - 30f) / 2f, btnW, 30f);

                if (isEquipped)
                {
                    GUIStyle eqLabelStyle = new GUIStyle(GUI.skin.label);
                    eqLabelStyle.fontSize = Mathf.Clamp((int)(innerW * 0.028f), 8, 10);
                    eqLabelStyle.fontStyle = FontStyle.Bold;
                    eqLabelStyle.alignment = TextAnchor.MiddleCenter;
                    eqLabelStyle.normal.textColor = new Color(0.35f, 1f, 0.45f);
                    string eqStr = LanguageSelectionUI.Localize("✓ EQUIPPED", "✓ مجهّز", "✓ НАДЕТО", "✓ EQUIPADO", "✓ ÉQUIPÉ", "✓ AUSGERÜSTET", "✓ सुसज्जित", "✓ 已装备", "✓ 装備中", "✓ EQUIPADO", "✓ مجهز شده");
                    GUI.Label(btnRect, eqStr, eqLabelStyle);
                }
                else if (isUnlocked)
                {
                    string eqBtnStr = LanguageSelectionUI.Localize("EQUIP", "تجهيز", "НАДЕТЬ", "EQUIPAR", "ÉQUIPER", "AUSRÜSTEN", "सुसज्जित करें", "装备", "装備する", "EQUIPAR", "تجهیز");
                    if (DrawActionButton(btnRect, eqBtnStr, new Color(0.2f, 0.60f, 0.28f)))
                    {
                        EquipSkin(ch, s);
                    }
                }
                else
                {
                    // Locked Skin Button
                    switch (skin.unlockMethod)
                    {
                        case CharacterSkin.UnlockMethod.AccountLevel:
                            bool canClaim = PlayerProfile.Instance != null && PlayerProfile.Instance.AccountLevel >= skin.requiredLevel;
                            string lvlBtn = canClaim
                                ? LanguageSelectionUI.Localize($"CLAIM (Lv.{skin.requiredLevel})", $"استلام (مستوى {skin.requiredLevel})", $"ЗАБРАТЬ (Ур.{skin.requiredLevel})", $"RECLAMAR (Nvl.{skin.requiredLevel})", $"RÉCLAMER (Niv.{skin.requiredLevel})", $"BEANSPRUCHEN (St.{skin.requiredLevel})", $"दावा करें (स्तर {skin.requiredLevel})", $"领取 (等级 {skin.requiredLevel})", $"獲得 (Lv.{skin.requiredLevel})", $"REIVINDICAR (Nvl.{skin.requiredLevel})", $"دریافت (سطح {skin.requiredLevel})")
                                : $"🪙 {skin.requiredCost}";
                            Color lvlCol = canClaim ? new Color(0.25f, 0.65f, 0.35f) : new Color(0.65f, 0.5f, 0.15f);
                            if (DrawActionButton(btnRect, lvlBtn, lvlCol))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;

                        case CharacterSkin.UnlockMethod.WatchAds:
                            string adBtn = LanguageSelectionUI.Localize("🎬 1 AD", "🎬 إعلان 1", "🎬 1 РЕКЛАМА", "🎬 1 ANUNCIO", "🎬 1 PUB", "🎬 1 WERBUNG", "🎬 1 विज्ञापन", "🎬 1 广告", "🎬 1 広告", "🎬 1 ANÚNCIO", "🎬 ۱ تبلیغ");
                            if (DrawActionButton(btnRect, adBtn, new Color(0.25f, 0.5f, 0.75f)))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;

                        case CharacterSkin.UnlockMethod.PurchaseCoins:
                            string coinBtn = $"🪙 {skin.requiredCost}";
                            if (DrawActionButton(btnRect, coinBtn, new Color(0.7f, 0.55f, 0.15f)))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;

                        case CharacterSkin.UnlockMethod.PurchaseGems:
                            string gemBtn = $"💎 {skin.requiredCost}";
                            if (DrawActionButton(btnRect, gemBtn, new Color(0.6f, 0.25f, 0.75f)))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;
                    }
                }

                // Click to preview skin on model
                if (GUI.Button(new Rect(cardR.x, cardR.y, cardR.width - btnW - 8f, cardR.height), GUIContent.none, GUIStyle.none))
                {
                    currentSkinIndex = s;
                    currentSkin = skin;
                    ApplySkinToPreviewModel(ch, skin);
                }

                curY += cardH + 6f;
            }
        }

        GUI.EndScrollView();
    }

    // ==================== LUXURY RPG ATTRIBUTE CARD ====================
    void DrawLuxuryAttributeCard(Rect rect, string icon, string title, string value, string bonus, float ratio, Color gaugeColor, string badgeText, Color badgeColor)
    {
        ratio = Mathf.Clamp01(ratio);

        // Obsidian glass card background
        GUI.color = new Color(0.045f, 0.085f, 0.065f, 0.92f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        DrawBorder(rect, new Color(gaugeColor.r * 0.45f, gaugeColor.g * 0.45f, gaugeColor.b * 0.45f, 0.55f), 1f);
        GUI.color = Color.white;

        // Top line: Left Title with Icon
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Clamp((int)(rect.width * 0.042f), 10, 12);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleLeft;
        titleStyle.normal.textColor = new Color(0.95f, 0.92f, 0.85f);
        GUI.Label(new Rect(rect.x + 8f, rect.y + 3f, rect.width * 0.44f, 18f), $"{icon} {title}", titleStyle);

        // Top line: Right Rating Badge
        if (!string.IsNullOrEmpty(badgeText))
        {
            float badgeW = Mathf.Clamp(rect.width * 0.30f, 75f, 115f);
            float badgeH = 17f;
            Rect badgeRect = new Rect(rect.x + rect.width - badgeW - 6f, rect.y + 4f, badgeW, badgeH);

            GUI.color = new Color(badgeColor.r * 0.25f, badgeColor.g * 0.25f, badgeColor.b * 0.25f, 0.9f);
            GUI.DrawTexture(badgeRect, Texture2D.whiteTexture);
            DrawBorder(badgeRect, new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.85f), 1f);
            GUI.color = Color.white;

            GUIStyle badgeStyle = new GUIStyle(GUI.skin.label);
            badgeStyle.fontSize = Mathf.Clamp((int)(badgeW * 0.095f), 8, 10);
            badgeStyle.fontStyle = FontStyle.Bold;
            badgeStyle.alignment = TextAnchor.MiddleCenter;
            badgeStyle.normal.textColor = Color.white;
            GUI.Label(badgeRect, badgeText, badgeStyle);
        }

        // Top line: Value & Buff bonus (placed between title and badge)
        GUIStyle valStyle = new GUIStyle(GUI.skin.label);
        valStyle.fontSize = Mathf.Clamp((int)(rect.width * 0.040f), 10, 12);
        valStyle.fontStyle = FontStyle.Bold;
        valStyle.alignment = TextAnchor.MiddleRight;
        valStyle.normal.textColor = new Color(1.0f, 0.92f, 0.45f);

        float rightOffset = !string.IsNullOrEmpty(badgeText) ? (rect.width * 0.32f + 10f) : 10f;
        string fullVal = value + (!string.IsNullOrEmpty(bonus) ? $" <color=#55FF88>{bonus}</color>" : "");
        valStyle.richText = true;
        GUI.Label(new Rect(rect.x + rect.width * 0.40f, rect.y + 3f, rect.width - rect.width * 0.40f - rightOffset, 18f), fullVal, valStyle);

        // Bottom line: Jewel Gauge Bar (Height: 7px)
        float barX = rect.x + 8f;
        float barY = rect.y + rect.height - 13f;
        float barW = rect.width - 16f;
        float barH = 7f;

        // Recessed Groove
        Rect grooveRect = new Rect(barX, barY, barW, barH);
        GUI.color = new Color(0.02f, 0.04f, 0.03f, 0.95f);
        GUI.DrawTexture(grooveRect, Texture2D.whiteTexture);

        // Active Jewel Gradient Fill
        if (ratio > 0.01f)
        {
            Rect fillRect = new Rect(barX, barY, barW * ratio, barH);
            GUI.color = new Color(gaugeColor.r, gaugeColor.g, gaugeColor.b, 0.92f);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

            // Leading Edge Glow Specular
            float edgeX = barX + barW * ratio - 3f;
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            GUI.DrawTexture(new Rect(edgeX, barY, 3f, barH), Texture2D.whiteTexture);
        }

        // Tick marks at 25%, 50%, 75%
        GUI.color = new Color(1f, 1f, 1f, 0.25f);
        GUI.DrawTexture(new Rect(barX + barW * 0.25f, barY, 1f, barH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(barX + barW * 0.50f, barY, 1f, barH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(barX + barW * 0.75f, barY, 1f, barH), Texture2D.whiteTexture);

        // Gold frame for groove
        DrawBorder(grooveRect, new Color(0.70f, 0.60f, 0.25f, 0.65f), 1f);
        GUI.color = Color.white;
    }

    void EquipSkin(CharacterData ch, int skinIdx)
    {
        if (availableSkins == null || skinIdx < 0 || skinIdx >= availableSkins.Length) return;

        currentSkinIndex = skinIdx;
        currentSkin = availableSkins[skinIdx];
        skinEquipped = true;
        if (PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.SetSelectedSkin(ch.characterID, skinIdx);
        }
        ApplySkinToPreviewModel(ch, currentSkin);

        PlayerMovement localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            localPlayer.ApplyCharacter(ch.characterID);
        }

        skinNotification = LanguageSelectionUI.Localize(
            $"✓ EQUIPPED: {currentSkin.skinName}!",
            ar: $"✓ تم تجهيز: {currentSkin.skinName}!",
            ru: $"✓ НАДЕТ ОБЛИК: {currentSkin.skinName}!",
            es: $"✓ ¡EQUIPADO: {currentSkin.skinName}!",
            fr: $"✓ ÉQUIPÉ : {currentSkin.skinName}!",
            de: $"✓ AUSGERÜSTET: {currentSkin.skinName}!",
            hi: $"✓ सुसज्जित: {currentSkin.skinName}!",
            zh: $"✓ 已装备: {currentSkin.skinName}!",
            ja: $"✓ 装備完了: {currentSkin.skinName}!",
            pt: $"✓ EQUIPADO: {currentSkin.skinName}!",
            fa: $"✓ مجهز شد: {currentSkin.skinName}!"
        );
        skinNotificationTimer = 3f;
    }

    void UnlockSkin(CharacterData ch, int skinIdx, CharacterSkin skin)
    {
        if (PlayerProfile.Instance == null)
        {
            EquipSkin(ch, skinIdx);
            return;
        }

        switch (skin.unlockMethod)
        {
            case CharacterSkin.UnlockMethod.Free:
                PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                EquipSkin(ch, skinIdx);
                break;

            case CharacterSkin.UnlockMethod.AccountLevel:
                if (PlayerProfile.Instance.AccountLevel >= skin.requiredLevel)
                {
                    PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                    EquipSkin(ch, skinIdx);
                }
                else if (skin.requiredCost > 0 && PlayerProfile.Instance.Coins >= skin.requiredCost)
                {
                    if (PlayerProfile.Instance.SpendCoins(skin.requiredCost))
                    {
                        PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                        EquipSkin(ch, skinIdx);
                    }
                }
                else
                {
                    skinNotification = LanguageSelectionUI.Localize(
                        $"Requires Account Lv. {skin.requiredLevel} or {skin.requiredCost} Coins!",
                        ar: $"يتطلب مستوى حساب {skin.requiredLevel} أو {skin.requiredCost} قطعة نقدية!",
                        ru: $"Требуется уровень {skin.requiredLevel} или {skin.requiredCost} монет!",
                        es: $"¡Requiere Nivel {skin.requiredLevel} o {skin.requiredCost} Monedas!",
                        fr: $"Niveau {skin.requiredLevel} ou {skin.requiredCost} Pièces requis !",
                        de: $"Erfordert Kontostufe {skin.requiredLevel} oder {skin.requiredCost} Münzen!",
                        hi: $"खाता स्तर {skin.requiredLevel} या {skin.requiredCost} सिक्के चाहिए!",
                        zh: $"需要账号等级 {skin.requiredLevel} 或 {skin.requiredCost} 金币！",
                        ja: $"アカウントLv.{skin.requiredLevel}または{skin.requiredCost}コインが必要です！",
                        pt: $"Requer Nível {skin.requiredLevel} ou {skin.requiredCost} Moedas!",
                        fa: $"نیاز به سطح حساب {skin.requiredLevel} یا {skin.requiredCost} سکه دارد!"
                    );
                    skinNotificationTimer = 3f;
                }
                break;

            case CharacterSkin.UnlockMethod.WatchAds:
                PlayerProfile.Instance.WatchRewardedAd((success) =>
                {
                    if (success)
                    {
                        PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                        EquipSkin(ch, skinIdx);
                    }
                });
                break;

            case CharacterSkin.UnlockMethod.PurchaseCoins:
                if (PlayerProfile.Instance.SpendCoins(skin.requiredCost))
                {
                    PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                    EquipSkin(ch, skinIdx);
                }
                else
                {
                    skinNotification = LanguageSelectionUI.Localize(
                        $"Not enough coins! Need {skin.requiredCost} Coins.",
                        ar: $"العملات غير كافية! تحتاج إلى {skin.requiredCost} قطعة نقدية.",
                        ru: $"Недостаточно монет! Нужно {skin.requiredCost} монет.",
                        es: $"¡No tienes suficientes monedas! Necesitas {skin.requiredCost}.",
                        fr: $"Pas assez de pièces ! Besoin de {skin.requiredCost}.",
                        de: $"Nicht genug Münzen! Benötige {skin.requiredCost} Münzen.",
                        hi: $"पर्याप्त सिक्के नहीं हैं! {skin.requiredCost} सिक्के चाहिए।",
                        zh: $"金币不足！需要 {skin.requiredCost} 金币。",
                        ja: $"コインが不足しています！{skin.requiredCost}コイン必要です。",
                        pt: $"Moedas insuficientes! Precisa de {skin.requiredCost}.",
                        fa: $"سکه کافی نیست! نیاز به {skin.requiredCost} سکه دارید."
                    );
                    skinNotificationTimer = 3f;
                }
                break;

            case CharacterSkin.UnlockMethod.PurchaseGems:
                if (PlayerProfile.Instance.SpendGems(skin.requiredCost))
                {
                    PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                    EquipSkin(ch, skinIdx);
                }
                else
                {
                    skinNotification = LanguageSelectionUI.Localize(
                        $"Not enough gems! Need {skin.requiredCost} Gems.",
                        ar: $"الجواهر غير كافية! تحتاج إلى {skin.requiredCost} جوهرة.",
                        ru: $"Недостаточно кристаллов! Нужно {skin.requiredCost} гемов.",
                        es: $"¡No tienes suficientes gemas! Necesitas {skin.requiredCost}.",
                        fr: $"Pas assez de gemmes ! Besoin de {skin.requiredCost}.",
                        de: $"Nicht genug Edelsteine! Benötige {skin.requiredCost} Edelsteine.",
                        hi: $"पर्याप्त रत्न नहीं हैं! {skin.requiredCost} रत्न चाहिए।",
                        zh: $"宝石不足！需要 {skin.requiredCost} 宝石。",
                        ja: $"宝石が不足しています！{skin.requiredCost}宝石必要です。",
                        pt: $"Gemas insuficientes! Precisa de {skin.requiredCost}.",
                        fa: $"الماس کافی نیست! نیاز به {skin.requiredCost} الماس دارید."
                    );
                    skinNotificationTimer = 3f;
                }
                break;
        }
    }

    void ApplySkinToPreviewModel(CharacterData ch, CharacterSkin skin)
    {
        if (currentPreviewModel == null || skin == null) return;

        Renderer[] rends = currentPreviewModel.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r == null || r.materials == null) continue;
            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                if (skin.skinColor != Color.white)
                {
                    mat.color = skin.skinColor;
                }
                if (skin.emissionColor != Color.black)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", skin.emissionColor);
                }
            }
        }
    }

    // ==================== UTILITY DRAWING METHODS ====================
    void DrawStatBar(Rect rect, string label, float value, Color barColor, int fontSize)
    {
        value = Mathf.Clamp01(value);

        // Background groove
        GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.85f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        // Active Fill Bar
        GUI.color = new Color(barColor.r, barColor.g, barColor.b, 0.85f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * value, rect.height), Texture2D.whiteTexture);

        // Specular highlight at leading edge
        if (value > 0.02f)
        {
            float edgeX = rect.x + rect.width * value - 4f;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(edgeX, rect.y, 4f, rect.height), Texture2D.whiteTexture);
        }

        // Crisp border
        DrawBorder(rect, new Color(barColor.r * 0.5f, barColor.g * 0.5f, barColor.b * 0.5f, 0.45f), 1f);
        GUI.color = Color.white;

        // Label
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = new Color(0.92f, 0.96f, 0.88f);
        labelStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), label, labelStyle);
    }

    void DrawBorder(Rect rect, Color color, float thickness)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    bool DrawActionButton(Rect rect, string text, Color bgColor)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        float bright = hover ? 1.25f : 1f;
        GUI.color = new Color(bgColor.r * bright, bgColor.g * bright, bgColor.b * bright, 0.95f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        DrawBorder(rect, new Color(0.45f, 0.85f, 0.4f, 0.7f), 1f);
        GUI.color = Color.white;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.label);
        btnStyle.fontSize = Mathf.Max(9, (int)(rect.width * 0.080f));
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.alignment = TextAnchor.MiddleCenter;
        btnStyle.normal.textColor = hover ? new Color(1f, 1f, 0.6f) : new Color(0.90f, 0.95f, 0.85f);
        GUI.Label(rect, text, btnStyle);

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    string GetAttackTypeLabel(CharacterCombat.AttackType type)
    {
        switch (type)
        {
            case CharacterCombat.AttackType.Melee:
                return LanguageSelectionUI.Localize("Close-Range Melee", "قتال قريب", "Ближний бой", "Cuerpo a cuerpo", "Mêlée rapprochée", "Nahkampf", "हाथापाई युद्ध", "近战肉搏", "近接攻撃", "Corpo a corpo", "نبرد نزدیک");
            case CharacterCombat.AttackType.Ranged:
                return LanguageSelectionUI.Localize("Ranged Projectile", "مقذوف عن بعد", "Дальний бой", "Proyectil a distancia", "Projectile à distance", "Fernkampf-Projektil", "दूरी प्रक्षेप्य", "远程弹道", "遠距離射撃", "Projétil à distância", "پرتابه از دور");
            default:
                return LanguageSelectionUI.Localize("Unknown", "غير معروف", "Неизвестно", "Desconocido", "Inconnu", "Unbekannt", "अज्ञात", "未知", "不明", "Desconhecido", "نامشخص");
        }
    }

    public PlayerMovement GetLocalPlayer()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localObj != null)
            {
                PlayerMovement pm = localObj.GetComponent<PlayerMovement>();
                if (pm != null) return pm;
            }
        }

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>();
        foreach (var p in players)
        {
            if (p.IsOwner) return p;
        }

        return FindAnyObjectByType<PlayerMovement>();
    }

    void ConfirmSelection()
    {
        int charID = selectedCharacterID >= 0 ? selectedCharacterID : (hoveredCharacterID >= 0 ? hoveredCharacterID : 0);

        PlayerMovement player = GetLocalPlayer();
        if (player != null)
        {
            // 1. Ensure team is set
            if (!player.hasSelectedTeam.Value)
            {
                player.RequestTeam(PlayerMovement.TeamType.Protector);
                player.team = PlayerMovement.TeamType.Protector;
            }

            // 2. Request character via Network RPC and apply locally immediately
            player.RequestCharacter(charID);
            player.ApplyCharacter(charID);

            // 3. Apply selected skin if equipped
            if (skinEquipped)
            {
                player.ApplySkinVisuals(currentSkin);
            }

            // 4. In Host / Offline battle: Fill teams with AI bots if not spawned yet
            if (BotManager.Instance != null && (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer))
            {
                BotManager.Instance.CreateOfflineBots(player);
            }

            // 5. Official match start: trigger countdown to transition state to Playing
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.RequestStartGame();
            }
        }

        Hide();
    }
}
