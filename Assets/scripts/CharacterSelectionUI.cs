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
    private float lastCardClickTime = 0f;
    private int lastClickedCardID = -1;
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
        gridScrollPosition = Vector2.zero;
        detailsScrollPosition = Vector2.zero;
        lastCardClickTime = 0f;
        lastClickedCardID = -1;

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

        // دعم اختصار الكيبورد للتأكيد الفوري والدخول للمعركة (Enter أو مسافة)
        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Space)
            {
                Event.current.Use();
                ConfirmSelection();
                return;
            }
        }

        if (gradientTex == null) CreateCachedTextures();

        float sw = Screen.width;
        float sh = Screen.height;

        // 1. Full Screen Translucent Ambient Background
        DrawBackground(sw, sh);

        // 2. Top Header (Back Button "< CHARACTER SELECT" on left, Currency & Player Name on right)
        DrawHeader(sw, sh);

        // 3. Main Content Area (Spans from sh * 0.075f to sh * 0.96f)
        float contentY = sh * 0.078f;
        float contentH = sh * 0.880f;

        // A. Far-Left: Slim Vertical Role Sidebar (width: 52px)
        float roleBarW = 50f;
        float roleBarX = 14f;
        DrawVerticalRoleSidebar(new Rect(roleBarX, contentY, roleBarW, contentH));

        // B. Hero Grid (3 Columns of compact cards, width: ~31% of screen)
        float gridX = roleBarX + roleBarW + 10f;
        float gridW = sw * 0.31f;

        // C. Center: Grand 3D Hero Altar Showcase & Confirm Button (width: ~34% of screen)
        float centerX = gridX + gridW + 12f;
        float centerW = sw * 0.34f;

        // D. Right: Hero Dossier, Lore, Thin Luxury Stat Sliders & Wardrobe (width: ~28% of screen)
        float rightX = centerX + centerW + 12f;
        float rightW = sw - rightX - 14f;

        // Active Display Character
        int displayID = (hoveredCharacterID >= 0) ? hoveredCharacterID : (selectedCharacterID >= 0 ? selectedCharacterID : 0);
        CharacterData displayChar = (database != null) ? database.GetCharacter(displayID) : null;
        if (displayChar == null && database != null && database.characters != null && database.characters.Length > 0)
            displayChar = database.characters[0];

        // --- Column 1: Left Hero Roster ---
        DrawCharacterGrid(new Rect(gridX, contentY, gridW, contentH));

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

        // 4. Bottom Navigation Strip (Back / Exit keyboard hints)
        DrawBottomNavigationStrip(sw, sh);
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

        // ==================== HEADER (IMAGE 2 STYLE) ====================
    void DrawHeader(float sw, float sh)
    {
        // Top-left Luxury Back button: "< CHARACTER SELECT"
        float backW = Mathf.Clamp(sw * 0.22f, 180f, 260f);
        float backH = 32f;
        float backX = sw * 0.015f;
        float backY = sh * 0.015f;

        string backText = "< " + LanguageSelectionUI.Localize(
            "CHARACTER SELECT",
            ar: "Ø§Ø®ØªÙŠØ§Ø± Ø§Ù„Ø¨Ø·Ù„",
            ru: "Ð’Ð«Ð‘ÐžÐ  Ð“Ð•Ð ÐžÐ¯",
            fr: "SÃ‰LECTION DU HÃ‰ROS",
            es: "SELECCIÃ“N DE HÃ‰ROE",
            de: "HELDENAUSWAHL",
            hi: "à¤¨à¤¾à¤¯à¤• à¤šà¤¯à¤¨",
            zh: "è‹±é›„é€‰æ‹©",
            ja: "è‹±é›„é¸æŠž",
            pt: "SELEÃ‡ÃƒO DE HERÃ“IS",
            fa: "Ø§Ù†ØªØ®Ø§Ø¨ Ù‚Ù‡Ø±Ù…Ø§Ù†"
        );

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(backX, backY, backW, backH), backText, new Color(0.92f, 0.78f, 0.32f), 12))
        {
            Hide();
            if (!TeamVoiceChatManager.HasConfiguredVoicePreference)
            {
                TeamVoiceChatUI.OpenPreMatchPrompt();
            }
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

        // Top-Right: Luxury Currency Capsule (Image 2 style: 106 ðŸ’Ž  |  230 ðŸŸ¢  |  2145 ðŸª™)
        int curCoins = PlayerProfile.Instance != null ? PlayerProfile.Instance.Coins : 0;
        int curGems = PlayerProfile.Instance != null ? PlayerProfile.Instance.Gems : 0;
        int curRating = PlayerProfile.Instance != null ? PlayerProfile.Instance.RatingScore_AllTime : 420;

        float ecoW = Mathf.Clamp(sw * 0.24f, 210f, 280f);
        float ecoH = 32f;
        float ecoX = sw * 0.985f - ecoW;
        float ecoY = sh * 0.015f;

        Rect ecoRect = new Rect(ecoX, ecoY, ecoW, ecoH);
        EnchantedNatureGUI.DrawLeafPlaque(ecoRect, new Color(0.04f, 0.08f, 0.05f, 0.94f), new Color(0.85f, 0.72f, 0.32f, 0.85f));

        GUIStyle ecoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(ecoW * 0.045f), 10, 12),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.92f, 0.45f) }
        };
        string ecoStr = $"ðŸ’Ž {curGems}   ðŸŸ¢ {curRating}   ðŸª™ {curCoins}";
        GUI.Label(ecoRect, ecoStr, ecoStyle);

        // Name Tag to the left of the currency pill
        float nameW = Mathf.Clamp(sw * 0.16f, 140f, 190f);
        float nameX = ecoX - nameW - 10f;
        Rect nameRect = new Rect(nameX, ecoY, nameW, ecoH);
        EnchantedNatureGUI.DrawLeafPlaque(nameRect, new Color(0.04f, 0.08f, 0.05f, 0.90f), new Color(0.65f, 0.55f, 0.25f, 0.65f));

        GUIStyle nameLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.70f, 0.95f, 0.75f) }
        };
        string curName = PlayerProfile.GetSavedPlayerName();
        GUI.Label(nameRect, $"ðŸ‘¤ {curName}", nameLabelStyle);
    }

    // ==================== VERTICAL ROLE SIDEBAR (IMAGE 2 STYLE) ====================
    void DrawVerticalRoleSidebar(Rect rect)
    {
        // 6 Vertical Role Diamond Buttons: ALL, DAMAGE, TANK, SUPPORT, CONTROL, SPECIALIST
        int[] roles = new int[] { -1, 1, 0, 2, 3, 4 };
        string[] roleIcons = new string[] { "â–", "âš”ï¸", "ðŸ›¡ï¸", "ðŸ’š", "ðŸ¹", "âœ¨" };
        string[] roleLabels = new string[]
        {
            LanguageSelectionUI.Localize("ALL", "Ø§Ù„ÙƒÙ„", "Ð’Ð¡Ð•", "TOUS", "TODOS", "ALLE", "à¤¸à¤­à¥€", "å…¨éƒ¨", "ã™ã¹ã¦", "TODOS", "Ù‡Ù…Ù‡"),
            LanguageSelectionUI.Localize("ATK", "Ù‡Ø¬ÙˆÙ…", "Ð£Ð ÐžÐ", "DÃ‰G", "DAÃ‘O", "ATK", "à¤¹à¤®à¤²à¤¾", "è¾“å‡º", "æ”»æ’ƒ", "ATQ", "Ø­Ù…Ù„Ù‡"),
            LanguageSelectionUI.Localize("TANK", "Ù…Ø¯Ø±Ø¹", "Ð¢ÐÐÐš", "TANK", "TANQ", "TANK", "à¤Ÿà¥ˆà¤‚à¤•", "é‡è£…", "é˜²è¡›", "TNK", "ØªØ§Ù†Ú©"),
            LanguageSelectionUI.Localize("SUPP", "Ø¯Ø¹Ù…", "Ð¡ÐÐŸÐŸ", "SOUT", "APOY", "SUPP", "à¤¸à¤¹à¤¾à¤¯", "è¾…åŠ©", "æ”¯æ´", "SUP", "Ù¾Ø´ØªÛŒØ¨Ø§Ù†"),
            LanguageSelectionUI.Localize("RNG", "Ø±Ø§Ù…ÙŠ", "Ð›Ð£Ðš", "ARCH", "ARQ", "FERN", "à¤§à¤¨à¥à¤·", "å°„æ‰‹", "å°„æ‰‹", "RNG", "Ú©Ù…Ø§Ù†"),
            LanguageSelectionUI.Localize("MAGE", "Ø³Ø­Ø±", "ÐœÐÐ“", "MAGE", "MAGO", "MAG", "à¤œà¤¾à¤¦à¥‚", "æ³•æœ¯", "é­”æ³•", "MAG", "Ø¬Ø§Ø¯Ùˆ")
        };

        float btnW = rect.width;
        float btnH = Mathf.Min(54f, (rect.height - 8f * 5) / 6f);
        float spacing = 8f;

        for (int i = 0; i < roles.Length; i++)
        {
            int r = roles[i];
            bool isActive = (selectedRoleFilter == r);
            float by = rect.y + i * (btnH + spacing);
            Rect bRect = new Rect(rect.x, by, btnW, btnH);
            bool isHovered = bRect.Contains(Event.current.mousePosition);

            // Diamond background plaque
            Color fillCol = isActive 
                ? new Color(0.92f, 0.78f, 0.28f, 0.95f)
                : (isHovered ? new Color(0.12f, 0.22f, 0.16f, 0.92f) : new Color(0.03f, 0.06f, 0.04f, 0.88f));

            GUI.color = fillCol;
            GUI.DrawTexture(bRect, Texture2D.whiteTexture);

            Color borderCol = isActive
                ? new Color(1.0f, 0.95f, 0.65f, 1f)
                : (isHovered ? new Color(0.85f, 0.75f, 0.35f, 0.9f) : new Color(0.60f, 0.50f, 0.25f, 0.55f));

            DrawBorder(bRect, borderCol, isActive ? 2f : 1f);
            EnchantedNatureGUI.DrawCornerFiligree(bRect, borderCol, 6f);
            GUI.color = Color.white;

            // Icon & mini label
            GUIStyle iconSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = isActive ? 16 : 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = isActive ? new Color(0.08f, 0.06f, 0.02f) : (isHovered ? new Color(1f, 0.92f, 0.45f) : new Color(0.85f, 0.78f, 0.60f)) }
            };
            GUI.Label(new Rect(bRect.x, bRect.y + 4f, bRect.width, 24f), roleIcons[i], iconSt);

            GUIStyle tagSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 8,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = isActive ? new Color(0.10f, 0.08f, 0.02f) : (isHovered ? Color.white : new Color(0.65f, 0.75f, 0.68f)) }
            };
            GUI.Label(new Rect(bRect.x, bRect.y + bRect.height - 18f, bRect.width, 14f), roleLabels[i], tagSt);

            if (GUI.Button(bRect, GUIContent.none, GUIStyle.none))
            {
                selectedRoleFilter = r;
            }
        }
    }

    // ==================== COLUMN 1: LEFT HERO ROSTER (3-COLUMN LUXURY CARDS) ====================
    void DrawCharacterGrid(Rect area)
    {
        if (database == null || database.characters == null) return;

        // Obsidian Glass Frame with Antique Gold Filigree
        EnchantedNatureGUI.DrawAntiqueGoldFrame(area, new Color(0.24f, 0.85f, 0.45f));

        float pad = 6f;
        float headerH = 28f;

        // Title Header
        GUIStyle gridTitle = new GUIStyle(GUI.skin.label);
        gridTitle.fontSize = 11;
        gridTitle.fontStyle = FontStyle.Bold;
        gridTitle.alignment = TextAnchor.MiddleLeft;
        gridTitle.normal.textColor = new Color(0.85f, 0.78f, 0.35f, 0.95f);

        string rosterTitle = LanguageSelectionUI.Localize("âœ¦ ROSTER", "âœ¦ Ø§Ù„Ø£Ø¨Ø·Ø§Ù„", "âœ¦ Ð“Ð•Ð ÐžÐ˜", "âœ¦ HÃ‰ROS", "âœ¦ HÃ‰ROES", "âœ¦ HELDEN", "âœ¦ à¤¨à¤¾à¤¯à¤•", "âœ¦ è‹±é›„åå†Œ", "âœ¦ è‹±é›„ä¸€è¦§", "âœ¦ HERÃ“IS", "âœ¦ Ù‚Ù‡Ø±Ù…Ø§Ù†Ø§Ù†");
        GUI.Label(new Rect(area.x + pad + 2f, area.y + 4f, area.width * 0.55f, 20f), rosterTitle, gridTitle);

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
        countStyle.fontSize = 9;
        countStyle.alignment = TextAnchor.MiddleRight;
        countStyle.normal.textColor = new Color(0.65f, 0.85f, 0.70f, 0.85f);
        GUI.Label(new Rect(area.x, area.y + 5f, area.width - pad - 4f, 18f), $"{filteredList.Count} " + LanguageSelectionUI.Localize("Heroes", "Ø£Ø¨Ø·Ø§Ù„", "Ð³ÐµÑ€Ð¾ÐµÐ²", "HÃ©ros", "HÃ©roes", "Helden", "à¤¨à¤¾à¤¯à¤•", "åè‹±é›„", "è‹±é›„", "HerÃ³is", "Ù‚Ù‡Ø±Ù…Ø§Ù†"), countStyle);

        // Fine gold divider
        GUI.color = new Color(0.65f, 0.55f, 0.25f, 0.55f);
        GUI.DrawTexture(new Rect(area.x + pad, area.y + headerH, area.width - pad * 2, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Scrollable 3-Column Luxury Cards (Image 2 style)
        float scrollY = area.y + headerH + 4f;
        float scrollH = area.height - headerH - 8f;
        float innerW = area.width - pad * 2f;

        const int COLUMNS = 3;
        float cardSpacing = 5f;
        float cardW = (innerW - cardSpacing * (COLUMNS - 1)) / COLUMNS;
        float cardH = cardW * 1.20f;

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
                float now = Time.realtimeSinceStartup;
                bool isDoubleClick = (lastClickedCardID == ch.characterID && (now - lastCardClickTime) < 0.45f);
                lastCardClickTime = now;
                lastClickedCardID = ch.characterID;

                selectedCharacterID = ch.characterID;
                selectionAnimTime = 0f;
                currentSkinIndex = 0;
                skinEquipped = false;
                availableSkins = CharacterSkinDatabase.GetSkinsForCharacter(ch.characterID);
                if (availableSkins != null && availableSkins.Length > 0)
                    currentSkin = availableSkins[0];

                if (isDoubleClick)
                {
                    ConfirmSelection();
                    return;
                }
            }
        }

        GUI.EndScrollView();
        hoveredCharacterID = frameHovered;
    }

    // ==================== LUXURY RPG CHARACTER CARD (IMAGE 2 STYLE) ====================
    void DrawCharacterCard(Rect rect, CharacterData ch, bool isSelected, bool isHovered, float animT)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        float lift = animT * 2f;
        Rect drawRect = new Rect(rect.x, rect.y - lift, rect.width, rect.height);

        // 1. Deep Obsidian Card Body
        Color bgCol = isSelected
            ? new Color(0.08f, 0.12f, 0.09f, 0.98f)
            : (isHovered ? new Color(0.06f, 0.09f, 0.07f, 0.95f) : new Color(0.03f, 0.045f, 0.035f, 0.92f));
        GUI.color = bgCol;
        GUI.DrawTexture(drawRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. Upper Portrait / Crest Area (68% of card height)
        float crestH = drawRect.height * 0.68f;
        Rect crestRect = new Rect(drawRect.x + 2f, drawRect.y + 2f, drawRect.width - 4f, crestH);

        // Subtle gradient underlay
        GUI.color = new Color(roleColor.r * 0.20f, roleColor.g * 0.20f, roleColor.b * 0.20f, 0.65f);
        GUI.DrawTexture(crestRect, gradientTex, ScaleMode.StretchToFill);
        GUI.color = Color.white;

        if (ch.characterIcon != null && ch.characterIcon.texture != null)
        {
            GUI.DrawTexture(crestRect, ch.characterIcon.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            // Heraldic Antique Gold Ring + Role Sigil
            GUIStyle iconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(16, (int)(crestH * 0.44f)),
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(crestRect, GetRoleEmoji(ch.role), iconStyle);
        }

        // 3. Lower Name Banner (32% of card height)
        float textY = drawRect.y + crestH;
        float textH = drawRect.height - crestH;
        Rect textRect = new Rect(drawRect.x + 2f, textY, drawRect.width - 4f, textH);

        // Thin divider between portrait and name
        GUI.color = new Color(0.65f, 0.55f, 0.25f, 0.45f);
        GUI.DrawTexture(new Rect(drawRect.x + 4f, textY, drawRect.width - 8f, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string localizedCharName = LanguageSelectionUI.Localize(ch.characterName);
        GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(drawRect.width * 0.125f), 8, 10),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = isSelected ? new Color(1f, 0.92f, 0.45f) : new Color(0.90f, 0.88f, 0.80f) }
        };
        GUI.Label(new Rect(textRect.x, textRect.y + 2f, textRect.width, 14f), localizedCharName, nameStyle);

        // Tiny Role Tag below name
        GUIStyle roleTagStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 7,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(roleColor.r * 0.9f, roleColor.g * 0.9f, roleColor.b * 0.9f, 0.85f) }
        };
        GUI.Label(new Rect(textRect.x, textRect.y + 16f, textRect.width, 10f), GetRoleName(ch.role).ToUpper(), roleTagStyle);

        // 4. Frames & Corner Filigree (Image 2 signature aesthetic: Double radiant gold frame)
        if (isSelected)
        {
            float selPulse = 0.85f + 0.15f * Mathf.Sin(selectionAnimTime * 3.5f);
            Color goldGlow = new Color(1.0f * selPulse, 0.88f * selPulse, 0.35f, 0.98f);

            // Double antique gold frame
            DrawBorder(drawRect, goldGlow, 2f);
            Rect innerFrame = new Rect(drawRect.x + 2f, drawRect.y + 2f, drawRect.width - 4f, drawRect.height - 4f);
            DrawBorder(innerFrame, new Color(goldGlow.r, goldGlow.g, goldGlow.b, 0.45f), 1f);

            // Corner filigree brackets
            EnchantedNatureGUI.DrawCornerFiligree(drawRect, goldGlow, 8f);
        }
        else if (isHovered)
        {
            Color hoverBorder = new Color(0.85f, 0.75f, 0.35f, 0.85f);
            DrawBorder(drawRect, hoverBorder, 1.5f);
            EnchantedNatureGUI.DrawCornerFiligree(drawRect, hoverBorder, 6f);
        }
        else
        {
            DrawBorder(drawRect, new Color(0.60f, 0.50f, 0.25f, 0.45f), 1f);
            EnchantedNatureGUI.DrawCornerFiligree(drawRect, new Color(0.60f, 0.50f, 0.25f, 0.35f), 4f);
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
        string titleWatermark = $"âœ¦ {locName} âœ¦";

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

        // Pinned Grand Center Confirmation Button (Directly on 3D Stage - Exactly Image 2: < SELECTED >)
        float centerBtnW = Mathf.Min(340f, area.width * 0.90f);
        float centerBtnH = 46f;
        float centerBtnX = area.x + (area.width - centerBtnW) * 0.5f;
        float centerBtnY = area.y + area.height - centerBtnH - 26f;
        Rect centerConfirmRect = new Rect(centerBtnX, centerBtnY, centerBtnW, centerBtnH);

        string locChar = LanguageSelectionUI.Localize(ch.characterName).ToUpper();
        string centerBtnText = "< âš”ï¸ " + LanguageSelectionUI.Localize(
            "COMMENCE EXPEDITION",
            ar: "Ø§Ù†Ø·Ù„Ù‚ ÙÙŠ Ø§Ù„Ø­Ù…Ù„Ø©",
            ru: "ÐÐÐ§ÐÐ¢Ð¬ ÐŸÐžÐ¥ÐžÐ”",
            fr: "COMMENCER L'EXPÃ‰DITION",
            es: "INICIAR EXPEDICIÃ“N",
            de: "EXPEDITION STARTEN",
            hi: "à¤…à¤­à¤¿à¤¯à¤¾à¤¨ à¤¶à¥à¤°à¥‚ à¤•à¤°à¥‡à¤‚",
            zh: "å¼€å§‹è¿œå¾",
            ja: "é å¾ã‚’é–‹å§‹",
            pt: "INICIAR EXPEDIÃ‡ÃƒO",
            fa: "Ø¢ØºØ§Ø² Ù†Ø¨Ø±Ø¯"
        ) + $" ({locChar}) âš”ï¸ >";

        if (EnchantedNatureGUI.DrawOrganicLeafButton(centerConfirmRect, centerBtnText, new Color(0.25f, 0.95f, 0.45f), 13))
        {
            selectedCharacterID = ch.characterID;
            ConfirmSelection();
        }

        // Bottom Drag-to-Rotate Hint
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = Mathf.Max(8, (int)(area.width * 0.026f));
        hintStyle.alignment = TextAnchor.LowerCenter;
        hintStyle.normal.textColor = new Color(0.65f, 0.85f, 0.70f, 0.75f);
        hintStyle.fontStyle = FontStyle.Italic;

        string hintStr = LanguageSelectionUI.Localize(
            "âœ¦ Drag mouse to rotate | Press Enter/Space to select âœ¦",
            ar: "âœ¦ Ø§Ø³Ø­Ø¨ Ø¨Ø§Ù„ÙØ£Ø±Ø© Ù„Ù„ØªØ¯ÙˆÙŠØ± | Ø§Ø¶ØºØ· Enter Ø£Ùˆ Ù…Ø³Ø§ÙØ© Ù„Ù„Ø§Ø®ØªÙŠØ§Ø± âœ¦",
            ru: "âœ¦ Ð¢ÑÐ½Ð¸Ñ‚Ðµ Ð¼Ñ‹ÑˆÑŒ Ð´Ð»Ñ Ð²Ñ€Ð°Ñ‰ÐµÐ½Ð¸Ñ | Enter/ÐŸÑ€Ð¾Ð±ÐµÐ» Ð´Ð»Ñ Ð²Ñ‹Ð±Ð¾Ñ€Ð° âœ¦",
            fr: "âœ¦ Glissez pour pivoter | EntrÃ©e/Espace pour choisir âœ¦",
            es: "âœ¦ Arrastra para rotar | Pulsa Enter/Espacio para elegir âœ¦",
            de: "âœ¦ Ziehen zum Drehen | Enter/Leertaste zum AuswÃ¤hlen âœ¦",
            hi: "âœ¦ à¤˜à¥à¤®à¤¾à¤¨à¥‡ à¤•à¥‡ à¤²à¤¿à¤ à¤®à¤¾à¤‰à¤¸ à¤–à¥€à¤‚à¤šà¥‡à¤‚ | à¤šà¥à¤¨à¤¨à¥‡ à¤•à¥‡ à¤²à¤¿à¤ Enter à¤¦à¤¬à¤¾à¤à¤‚ âœ¦",
            zh: "âœ¦ æ‹–åŠ¨æ—‹è½¬è‹±é›„ | æŒ‰å›žè½¦æˆ–ç©ºæ ¼ç›´æŽ¥å‡ºæˆ˜ âœ¦",
            ja: "âœ¦ ãƒ‰ãƒ©ãƒƒã‚°ã§å›žè»¢ | Enter/ã‚¹ãƒšãƒ¼ã‚¹ã§æ±ºå®š âœ¦",
            pt: "âœ¦ Arraste para girar | Enter/EspaÃ§o para escolher âœ¦",
            fa: "âœ¦ Ú©Ø´ÛŒØ¯Ù† Ø¨Ø±Ø§ÛŒ Ú†Ø±Ø®Ø´ | ÙØ´Ø±Ø¯Ù† Enter/ÙØ§ØµÙ„Ù‡ Ø¨Ø±Ø§ÛŒ Ø§Ù†ØªØ®Ø§Ø¨ âœ¦"
        );
        GUI.Label(new Rect(area.x, area.y + area.height - 22f, area.width, 18f), hintStr, hintStyle);
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

        float pad = 8f;
        float headerH = 36f;

        // Pinned Dual-Tab Switcher at Top of Right Column
        float tabW = (area.width - pad * 2f - 4f) / 2f;
        Rect tab0Rect = new Rect(area.x + pad, area.y + 6f, tabW, 26f);
        Rect tab1Rect = new Rect(area.x + pad + tabW + 4f, area.y + 6f, tabW, 26f);

        string attrTabStr = "âš”ï¸ " + LanguageSelectionUI.Localize("ATTRIBUTES", "Ø¥Ø­ØµØ§Ø¦ÙŠØ§Øª Ø§Ù„Ù‚ØªØ§Ù„", "Ð¥ÐÐ ÐÐšÐ¢Ð•Ð Ð˜Ð¡Ð¢Ð˜ÐšÐ˜", "ATRIBUTOS", "ATTRIBUTS", "ATTRIBUTE", "à¤µà¤¿à¤¶à¥‡à¤·à¤¤à¤¾à¤à¤", "æˆ˜æ–—å±žæ€§", "ã‚¹ãƒ†ãƒ¼ã‚¿ã‚¹", "ATRIBUTOS", "Ù…Ø´Ø®ØµØ§Øª Ø±Ø²Ù…ÛŒ");
        string skinsTabStr = "ðŸ‘‘ " + LanguageSelectionUI.Localize("WARDROBE", "Ø§Ù„Ù…Ø¸Ø§Ù‡Ø± ÙˆØ§Ù„Ø£Ø³Ù„Ø­Ø©", "ÐžÐ‘Ð›Ð˜ÐšÐ˜", "ASPECTOS", "SKINS SACRÃ‰S", "SKINS", "à¤ªà¤µà¤¿à¤¤à¥à¤° à¤–à¤¾à¤²", "ç¥žåœ£çš®è‚¤", "è–ãªã‚‹ã‚¹ã‚­ãƒ³", "SKINS SAGRADAS", "Ù¾ÙˆØ³ØªÙ‡â€ŒÙ‡Ø§ Ùˆ Ø³Ù„Ø§Ø­â€ŒÙ‡Ø§");

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
        GUI.color = new Color(0.65f, 0.55f, 0.25f, 0.45f);
        GUI.DrawTexture(new Rect(area.x + pad, area.y + headerH, area.width - pad * 2, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Middle Scrollable Area: Spans FULL remaining height (NO duplicate bottom button or footer!)
        float scrollY = area.y + headerH + 2f;
        float scrollH = area.height - headerH - 8f;
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

    // ==================== ATTRIBUTES CODEX TAB (IMAGE 2 DOSSIER STYLE) ====================
    void DrawAttributesTab(Rect scrollRect, CharacterData ch)
    {
        float innerW = scrollRect.width - 16f;
        float totalVirtualH = 500f;
        Rect viewRect = new Rect(0, 0, innerW, totalVirtualH);

        detailsScrollPosition = GUI.BeginScrollView(scrollRect, detailsScrollPosition, viewRect, false, true);

        float curY = 4f;

        // 1. Hero Dossier Header (Image 2 style: "Velastra, the Dawnpiercer" / "Archer, the Forest Warden")
        string locCharName = LanguageSelectionUI.Localize(ch.characterName);
        string epithet = GetHeroEpithet(ch.characterName);
        string fullDossierTitle = $"{locCharName}, {epithet}";

        GUIStyle dossierTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(innerW * 0.056f), 13, 17),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(1.0f, 0.88f, 0.38f) }
        };
        GUI.Label(new Rect(2f, curY, innerW - 4f, 24f), fullDossierTitle, dossierTitleStyle);
        curY += 24f;

        // Subtitle: "Lv. 37   ðŸ›¡ï¸ Paladin"
        int accLevel = PlayerProfile.Instance != null ? PlayerProfile.Instance.AccountLevel : 37;
        string subText = $"Lv. {accLevel}   {GetRoleEmoji(ch.role)} {GetRoleName(ch.role)}";

        GUIStyle dossierSubStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.75f, 0.90f, 0.78f) }
        };
        GUI.Label(new Rect(2f, curY, innerW - 4f, 18f), subText, dossierSubStyle);
        curY += 22f;

        // Decorative antique gold divider line with center diamond
        GUI.color = new Color(0.85f, 0.75f, 0.35f, 0.65f);
        GUI.DrawTexture(new Rect(2f, curY, innerW - 4f, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        curY += 8f;

        // 2. Lore Backstory Narrative Box (Image 2 style)
        float loreH = 76f;
        Rect loreRect = new Rect(0, curY, innerW, loreH);

        GUIStyle loreBody = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(innerW * 0.034f), 9, 11),
            fontStyle = FontStyle.Italic,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.80f, 0.92f, 0.84f, 0.92f) }
        };
        string loreText = GetLocalizedCharacterLore(ch.characterName, ch.role, ch.attackType);
        GUI.Label(loreRect, loreText, loreBody);
        curY += loreH + 10f;

        // 3. Thin Luxury Stat Sliders (Image 2 style: Strength, Speed, Durability, Range, Cadence, Mobility)
        float sliderH = 24f;
        float sliderGap = 4f;

        // Strength (Attack Damage) -> Rating e.g. 780
        int strScore = Mathf.RoundToInt((ch.attackDamage / 40f) * 600f + 300f);
        DrawLuxuryStatSlider(
            new Rect(0, curY, innerW, sliderH),
            LanguageSelectionUI.Localize("Strength", "Ø§Ù„Ù‚ÙˆØ© Ø§Ù„Ù‡Ø¬ÙˆÙ…ÙŠØ©", "Ð¡Ð¸Ð»Ð°", "Force", "Fuerza", "StÃ¤rke", "à¤¶à¤•à¥à¤¤à¤¿", "åŠ›é‡", "æ”»æ’ƒåŠ›", "ForÃ§a", "Ù‚Ø¯Ø±Øª"),
            ch.attackDamage / 40f,
            strScore,
            new Color(0.95f, 0.35f, 0.25f)
        );
        curY += sliderH + sliderGap;

        // Speed (Movement Speed) -> Rating e.g. 550
        int spdScore = Mathf.RoundToInt((ch.moveSpeed / 12f) * 500f + 350f);
        DrawLuxuryStatSlider(
            new Rect(0, curY, innerW, sliderH),
            LanguageSelectionUI.Localize("Speed", "Ø³Ø±Ø¹Ø© Ø§Ù„ØªØ­Ø±Ùƒ", "Ð¡ÐºÐ¾Ñ€Ð¾ÑÑ‚ÑŒ", "Vitesse", "Velocidad", "Geschwindigkeit", "à¤—à¤¤à¤¿", "ç§»é€Ÿ", "ç§»å‹•é€Ÿåº¦", "Velocidade", "Ø³Ø±Ø¹Øª"),
            ch.moveSpeed / 12f,
            spdScore,
            new Color(0.25f, 0.85f, 0.95f)
        );
        curY += sliderH + sliderGap;

        // Durability (Max Health) -> Rating e.g. 820
        int durScore = Mathf.RoundToInt((ch.maxHealth / 400f) * 600f + 300f);
        DrawLuxuryStatSlider(
            new Rect(0, curY, innerW, sliderH),
            LanguageSelectionUI.Localize("Durability", "Ø§Ù„ØµÙ„Ø§Ø¨Ø© ÙˆØ§Ù„Ø¯ÙØ§Ø¹", "Ð¡Ñ‚Ð¾Ð¹ÐºÐ¾ÑÑ‚ÑŒ", "Robustesse", "Durabilidad", "Widerstandskraft", "à¤¸à¤¹à¤¨à¤¶à¥€à¤²à¤¤à¤¾", "ç”Ÿå­˜", "è€ä¹…åŠ›", "Durabilidade", "Ù¾Ø§ÛŒØ¯Ø§Ø±ÛŒ"),
            ch.maxHealth / 400f,
            durScore,
            new Color(0.30f, 0.92f, 0.45f)
        );
        curY += sliderH + sliderGap;

        // Combat Range -> Rating e.g. 600
        int rngScore = Mathf.RoundToInt((ch.attackRange / 20f) * 600f + 250f);
        DrawLuxuryStatSlider(
            new Rect(0, curY, innerW, sliderH),
            LanguageSelectionUI.Localize("Range", "Ù…Ø¯Ù‰ Ø§Ù„Ù‚ØªØ§Ù„", "Ð”Ð°Ð»ÑŒÐ½Ð¾ÑÑ‚ÑŒ", "PortÃ©e", "Alcance", "Reichweite", "à¤¸à¥€à¤®à¤¾", "å°„ç¨‹", "å°„ç¨‹", "Alcance", "Ø¨Ø±Ø¯"),
            ch.attackRange / 20f,
            rngScore,
            new Color(0.95f, 0.80f, 0.30f)
        );
        curY += sliderH + sliderGap;

        // Attack Cadence -> Rating e.g. 520
        float attacksPerSec = 1f / Mathf.Max(0.1f, ch.attackCooldown);
        int cadScore = Mathf.RoundToInt(Mathf.Clamp01(1f - (ch.attackCooldown / 3f)) * 500f + 300f);
        DrawLuxuryStatSlider(
            new Rect(0, curY, innerW, sliderH),
            LanguageSelectionUI.Localize("Cadence", "ØªØªØ§Ø¨Ø¹ Ø§Ù„Ø¶Ø±Ø¨Ø§Øª", "Ð¢ÐµÐ¼Ð¿", "Cadence", "Cadencia", "Schlagtempo", "à¤¤à¤¾à¤²", "æ”»é€Ÿ", "é€£æ’ƒ", "CadÃªncia", "Ø¢Ù‡Ù†Ú¯"),
            Mathf.Clamp01(1f - (ch.attackCooldown / 3f)),
            cadScore,
            new Color(0.80f, 0.50f, 0.95f)
        );
        curY += sliderH + sliderGap;

        // Mobility / Jump Force -> Rating e.g. 640
        int mobScore = Mathf.RoundToInt((ch.jumpForce / 16f) * 500f + 320f);
        DrawLuxuryStatSlider(
            new Rect(0, curY, innerW, sliderH),
            LanguageSelectionUI.Localize("Mobility", "Ø§Ù„Ù…Ø±ÙˆÙ†Ø© ÙˆØ§Ù„Ù‚ÙØ²", "ÐœÐ¾Ð±Ð¸Ð»ÑŒÐ½Ð¾ÑÑ‚ÑŒ", "MobilitÃ©", "Movilidad", "MobilitÃ¤t", "à¤—à¤¤à¤¿à¤¶à¥€à¤²à¤¤à¤¾", "æœºåŠ¨", "æ©Ÿå‹•åŠ›", "Mobilidade", "ØªØ­Ø±Ú©"),
            ch.jumpForce / 16f,
            mobScore,
            new Color(0.35f, 0.95f, 0.70f)
        );
        curY += sliderH + 10f;

        // 4. Squad Capacity & Tactical Role Card
        float ribH = 32f;
        Rect ribRect = new Rect(0, curY, innerW, ribH);
        EnchantedNatureGUI.DrawLeafPlaque(ribRect, new Color(0.04f, 0.08f, 0.06f, 0.90f), new Color(0.65f, 0.55f, 0.25f, 0.60f));

        GUIStyle squadStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.80f, 0.45f) }
        };
        string squadStr = $"ðŸ‘¥ {LanguageSelectionUI.Localize("Squad Limit: ", "Ø³Ù‚Ù Ø§Ù„ÙØ±ÙŠÙ‚: ", "Ð›Ð¸Ð¼Ð¸Ñ‚ Ð¾Ñ‚Ñ€ÑÐ´Ð°: ", "Limite d'Ã©quipe : ", "LÃ­mite: ", "Trupplimit: ", "à¤¦à¤¸à¥à¤¤à¤¾ à¤¸à¥€à¤®à¤¾: ", "å°é˜Ÿä¸Šé™: ", "ãƒãƒ¼ãƒ ä¸Šé™: ", "Limite: ", "Ø³Ù‚Ù ØªÛŒÙ…: ")}{ch.maxPerTeam}   â€¢   {GetAttackTypeLabel(ch.attackType)}";
        GUI.Label(ribRect, squadStr, squadStyle);
        curY += ribH + 12f;

        GUI.EndScrollView();
    }

    // ==================== SACRED WARDROBE TAB
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
                GUI.Label(new Rect(textX, cardR.y + 4f, textW, 18f), GetLocalizedSkinName(skin.skinName, s), sNameStyle);

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
                GUI.Label(new Rect(textX, cardR.y + 38f, textW, 28f), GetLocalizedSkinDescription(skin.description, s), descStyle);

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
        if (!TeamVoiceChatManager.HasConfiguredVoicePreference)
        {
            TeamVoiceChatUI.OpenPreMatchPrompt();
        }
    }

    public static string GetLocalizedSkinName(string rawName, int index)
    {
        switch (index)
        {
            case 0:
                return LanguageSelectionUI.Localize("Standard Attire", "الزي القياسي", "Стандартный наряд", "Atuendo estándar", "Tenue standard", "Standard-Gewand", "मानक पोशाक", "标准战袍", "標準装束", "Traje Padrão", it: "Veste Standard", tr: "Standart Kıyafet", fa: "لباس استاندارد");
            case 1:
                return LanguageSelectionUI.Localize("Grove Champion", "بطل الغابة", "Чемпион рощи", "Campeón del bosque", "Champion du bosquet", "Hain-Champion", "ग्रोव चैंपियन", "丛林勇士", "聖森の勇者", "Campeão do Bosque", it: "Campione del Bosco", tr: "Koru Şampiyonu", fa: "قهرمان بیشه");
            case 2:
                return LanguageSelectionUI.Localize("Solar Inferno", "جحيم الشمس", "Солнечное пламя", "Infierno solar", "Flamme solaire", "Sonnen-Inferno", "सौर ज्वाला", "烈日炽焰", "烈日の炎", "Inferno Solar", it: "Inferno Solare", tr: "Güneş Cehennemi", fa: "آتش خورشیدی");
            case 3:
                return LanguageSelectionUI.Localize("Storm Sovereign", "حاكم العاصفة", "Владыка бури", "Soberano de tormenta", "Souverain de tempête", "Sturmsouverän", "तूफान शासक", "风暴君王", "嵐の君主", "Soberano da Tempestade", it: "Sovrano della Tempesta", tr: "Fırtına Hükümdarı", fa: "فرمانروای توفان");
            default:
                return LanguageSelectionUI.Localize("Apex Celestial", "الكائن السماوي الأسمى", "Небесный владыка", "Celestial supremo", "Céleste suprême", "Himmlischer Herrscher", "सर्वोच्च आकाशीय", "苍穹极光", "天上の支配者", "Celestial Supremo", it: "Celeste Supremo", tr: "Yüce Semavi", fa: "آسمانی برین");
        }
    }

    public static string GetLocalizedSkinDescription(string rawDesc, int index)
    {
        switch (index)
        {
            case 0:
                return LanguageSelectionUI.Localize("Standard ceremonial attire, balanced for tree defense and scouting.", "الزي الاحتفالي القياسي، متوازن للدفاع عن الشجرة واستكشاف الغابة.", "Стандартное церемониальное облачение для защиты древа.", "Atuendo ceremonial estándar para la defensa del árbol.", "Tenue cérémonielle standard pour la défense de l'arbre.", "Rituelle Standard-Kluft für die Baumverteidigung.", "मानक औपचारिक पोशाक, वृक्ष रक्षा के लिए संतुलित।", "标准礼仪战甲，攻防兼备，专为守护巨树远征打造。", "神木防衛と探索のために調和された標準儀礼装束。", "Traje cerimonial padrão para defender a árvore.", it: "Abito cerimoniale standard, bilanciato per la difesa dell'albero.", tr: "Ağaç savunması ve keşifler için dengelenmiş standart tören kıyafeti.", fa: "لباس تشریفاتی استاندارد، متعادل برای دفاع از درخت کهنسال و گشت‌زنی در بیشه.");
            case 1:
                return LanguageSelectionUI.Localize("Tempered armor forged from petrified sacred wood, bestowing boosted vitality.", "درع صلب مصنوع من خشب مقدس متحجر، يمنح حيوية قتالية معززة.", "Закаленные доспехи из священной древесины, увеличивающие здоровье.", "Armadura forjada con madera sagrada que otorga vitalidad.", "Armure forgée dans du bois sacré, conférant une vitalité accrue.", "Gehärtete Rüstung aus versteinertem heiligen Holz für mehr Vitalität.", "पवित्र लकड़ी से बना कवच, जो अतिरिक्त जीवन शक्ति प्रदान करता है।", "由石化神木锻造的强化重铠，赋予更加充沛的战斗生命力。", "石化神木から鍛造された強化鎧。戦闘での生命力を高める。", "Armadura temperada em madeira sagrada, concedendo mais vitalidade.", it: "Armatura temprata in legno sacro pietrificato, che conferisce maggiore vitalità.", tr: "Taşlaşmış kutsal ahşaptan dövülmüş, gelişmiş savaş dayanıklılığı sağlayan zırh.", fa: "زره آبدیده از چوب سنگ‌شده مقدس که استقامت و پایداری جنگی را افزایش می‌دهد.");
            default:
                return LanguageSelectionUI.Localize("Enchanted garments radiant with molten ember magic, amplifying offensive devastation.", "رداء مسحور مشع بسحر الحمم والشرار، يضاعف القوة الهجومية التدميرية.", "Одеяние с магией раскаленной лавы, усиливающее урон.", "Prendas encantadas con magia de lava que aumentan el daño.", "Vêtements enchantés aux braises, amplifiant la puissance d'attaque.", "Verzauberte Gewänder aus Glutmagie, die den Angriffsschaden verstärken.", "पिघले हुए अंगारे के जादू से बने वस्त्र, जो घातक हमले को बढ़ाते हैं।", "熔火炎芒附魔的烈焰祭袍，大幅增强每一次进攻的致命杀伤力。", "灼熱の溶岩魔力を宿した装束。攻撃の破壊力を大幅に増幅する。", "Vestes encantadas com magia de brasas, amplificando o dano.", it: "Vesti incantate radianti di magia ardente, che amplificano la distruzione.", tr: "Erimiş kor büyüsüyle parıldayan ve saldırı gücünü artıran efsunlu giysiler.", fa: "جامه‌ای افسون‌شده آکنده از جادوی گدازه‌های سوزان که قدرت تخریب حملات را به اوج می‌رساند.");
        }
    }

    public static string GetLocalizedCharacterLore(string charName, CharacterData.CharacterRole role, CharacterCombat.AttackType attackType)
    {
        string n = (charName ?? "").ToLowerInvariant().Trim();
        if (n.Contains("warrior") || n.Contains("knight") || n.Contains("paladin") || n.Contains("templar"))
        {
            return LanguageSelectionUI.Localize(
                "A stalwart melee guardian of the colossal ancient tree, wielding enchanted martial arms and steadfast resolve to break enemy sieges and secure holy altars.",
                ar: "فارس متمرس للشجرة القديمة العملاقة، يمتلك مهارات قتالية وسحراً مقدساً لكسر حصار الأعداء وحماية المذابح وقيادة الحملات نحو قمة الشجرة.",
                ru: "Доблестный страж древнего древа, сокрушающий врагов в ближнем бою и защищающий святыни.",
                fr: "Un vaillant gardien du corps-à-corps protégeant l'arbre colossal et repoussant les assauts ennemis.",
                es: "Un valiente guardián cuerpo a cuerpo que defiende el árbol sagrado con armas encantadas.",
                de: "Ein standhafter Nahkampfwächter des uralten Baumes mit verzauberten Waffen zur Verteidigung der heiligen Altäre.",
                hi: "विशाल प्राचीन वृक्ष का एक निष्ठावान संरक्षक, जो पवित्र वेदियों की रक्षा करता है।",
                zh: "参天古树的百战勇士，披坚执锐粉碎敌军围攻，誓死捍卫神圣圣坛。",
                ja: "巨大なる神木の屈強な守護者。聖なる武具を手に敵の包囲を破り、祭壇を死守する。",
                pt: "Um valoroso guardião corpo a corpo que defende a árvore colossal com armas encantadas.",
                it: "Un valoroso guardiano da mischia dell'albero colossale, usa armi incantate per difendere gli altari sacri.",
                tr: "Devasa kutsal ağacın metanetli yakın dövüş muhafızı, kutsal sunakları korumak için savaşa liderlik eder.",
                fa: "نگهبان دلاور نبرد نزدیک درخت کهنسال و غول‌پیکر با سلاح‌های افسون‌شده برای شکستن محاصره دشمن و دفاع از محراب‌های مقدس."
            );
        }
        else if (n.Contains("mage") || n.Contains("elementalist") || n.Contains("enchantress") || n.Contains("necromancer"))
        {
            return LanguageSelectionUI.Localize(
                "A master of mystic arcane surges, unleashing torrents of primordial elements and spellcraft to devastate rivals from a safe distance.",
                ar: "سيد السحر والعناصر القديمة، يطلق صواعق سحرية وطاقات مدمرة لسحق صفوف الأعداء عن بعد والتحكم بمجريات المعركة.",
                ru: "Мастер древней магии, обрушивающий стихии на врагов с безопасного расстояния.",
                fr: "Un maître des arcanes mystiques déchaînant les éléments pour ravager les rangs ennemis.",
                es: "Un maestro del poder arcano que desata los elementos para arrasar a los rivales desde lejos.",
                de: "Ein Meister arkaner Strömungen, der urzeitliche Elemente entfesselt, um Feinde aus der Ferne zu vernichten.",
                hi: "रहस्यमयी जादुई शक्तियों का स्वामी, जो दूर से ही दुश्मनों पर भारी प्रहार करता है।",
                zh: "掌控远古奥术与自然元素的秘术贤者，于千里之外倾泻法术狂澜倾覆战场。",
                ja: "秘術の奔流を操る大魔導士。太古の元素魔法を放ち、遠距離から敵陣を灰燼に帰す。",
                pt: "Um mestre das correntes arcanas que canaliza os elementos para devastar inimigos à distância.",
                it: "Un maestro dei flussi arcani, scatena elementi primordiali per devastare i rivali a distanza di sicurezza.",
                tr: "Uzak mesafeden rakipleri yok etmek için ilkel elementleri ve büyü sanatını serbest bırakan gizemli usta.",
                fa: "استاد امواج جادویی که سیلابی از عناصر باستانی و طلسم‌ها را برای نابودی دشمنان از فاصله‌ای امن به راه می‌اندازد."
            );
        }
        else if (n.Contains("archer") || n.Contains("ranger") || n.Contains("windwalker"))
        {
            return LanguageSelectionUI.Localize(
                "A swift sharpshooter navigating high branches with supreme agility, delivering piercing arrows and tactical strikes across the canopy.",
                ar: "رامٍ سريع وقناص يتنقل بين أغصان الشجرة ببراعة وخفة فائقة، يسدد سهاماً خارقة وضربات تكتيكية حاسمة.",
                ru: "Меткий стрелок, перемещающийся по кронам с непревзойденной ловкостью и поражающий цели издалека.",
                fr: "Un tireur d'élite agile traversant la canopée pour décocher des flèches perçantes.",
                es: "Un veloz arquero que recorre las ramas altas con gran agilidad y dispara flechas certeras.",
                de: "Ein flinker Scharfschütze, der mit höchster Wendigkeit durch die Baumkrone gleitet und tödliche Pfeile schießt.",
                hi: "एक तीव्र निशानेबाज जो विशाल वृक्ष की शाखाओं पर फुर्ती से चलता है और अचूक तीर चलाता है।",
                zh: "穿梭于参天树冠的神射游侠，身形鬼魅，以致命的穿甲箭雨锁定敌首胜局。",
                ja: "木々の間を素早く駆け巡る名射手。天上の梢から必中の矢を放ち、戦況を支配する。",
                pt: "Um atirador veloz que percorre os galhos altos com suprema agilidade e precisão mortal.",
                it: "Un agile tiratore scelto che attraversa i rami alti con agilità suprema, scoccando frecce perforanti.",
                tr: "Ağaç tepelerinde üstün çeviklikle ilerleyen, delici oklar ve taktiksel vuruşlar yapan usta okçu.",
                fa: "تک‌تیراندازی تیزپا که با چابکی خیره‌کننده بر شاخه‌های بلند می‌جهد و تیرهای شکافنده و ضربات تعیین‌کننده فرود می‌آورد."
            );
        }
        else if (n.Contains("tank") || n.Contains("ironclad"))
        {
            return LanguageSelectionUI.Localize(
                "An immovable colossus encased in heavy enchanted plate, absorbing monstrous blows and anchoring the frontline at all costs.",
                ar: "صخرة دفاعية منيعة ترتدي دروعاً حديدية ثقيلة، يمتص أعتى ضربات الأعداء والزعماء ويصمد في الخط الأمامي لضمان النصر.",
                ru: "Несокрушимый исполин в тяжелой броне, принимающий на себя удары врагов и удерживающий строй.",
                fr: "Un colosse inébranlable en armure lourde absorbant les chocs et tenant la ligne de front.",
                es: "Un coloso inamovible con armadura pesada que absorbe los golpes enemigos y lidera el frente.",
                de: "Ein unerschütterlicher Koloss in schwerer Rüstung, der gewaltige Schläge abfängt und die Front hält.",
                hi: "भारी कवच में ढका एक अडिग विशाल रक्षक, जो शत्रुओं के भीषण वार को झेलता है।",
                zh: "身披重甲如壁立千仞的磐石要塞，稳立前线抵挡强敌与泰坦首领的惊涛狂澜。",
                ja: "重厚な装甲を纏った不屈の巨神。あらゆる強撃を受け止め、最前線を死守する防壁。",
                pt: "Um colosso inabalável em armadura pesada, absorvendo impactos e mantendo a linha de frente.",
                it: "Un colosso inamovibile avvolto in pesanti piastre, assorbe i colpi più duri e guida la prima linea.",
                tr: "Ağır zırhlara bürünmüş sarsılmaz bir dev, en sert darbeleri karşılayıp ön safları tutar.",
                fa: "غول شکست‌ناپذیری پوشیده در زره‌های سنگین که سهمگین‌ترین ضربات دشمنان را دفع می‌کند و پایدار در خط مقدم می‌ماند."
            );
        }
        else if (n.Contains("assassin") || n.Contains("rogue") || n.Contains("shadowblade"))
        {
            return LanguageSelectionUI.Localize(
                "A deadly shadow blade striking with lightning speed from behind, exploiting weak points to eliminate high-value targets instantly.",
                ar: "محارب ظل قاتل يضرب بسرعة البرق من المباغتة، يستغل نقاط الضعف لحسم القتل في اللحظة المناسبة ثم الانسحاب برشاقة.",
                ru: "Смертоносный клинок из тени, наносящий молниеносные удары в уязвимые места противника.",
                fr: "Une lame de l'ombre mortelle frappant à la vitesse de l'éclair pour abattre les cibles clés.",
                es: "Un asesino de las sombras que ataca a velocidad fulminante para eliminar objetivos prioritarios.",
                de: "Eine tödliche Schattenklinge, die blitzschnell aus dem Hinterhalt zuschlägt und Schwachstellen ausnutzt.",
                hi: "छाया से वार करने वाला एक घातक शिकारी, जो बिजली की गति से दुश्मनों का खात्मा करता है।",
                zh: "遁入阴影的致命影刃，以雷霆万钧之势突刺敌阵弱点，瞬息间收割战局关键要害。",
                ja: "影より出でて迅雷の如く急所を突く暗殺者。一撃で重要拠点の敵を葬り去る。",
                pt: "Uma lâmina sombria letal atacando com a velocidade da luz para eliminar alvos prioritários.",
                it: "Una letale lama d'ombra che colpisce con velocità fulminea, sfruttando i punti deboli dei rivali.",
                tr: "Gölgelerden yıldırım hızıyla vuran, zayıf noktaları hedef alarak kilit hedefleri anında eleyen suikastçı.",
                fa: "تیغه مرگبار سایه‌ها که با سرعت آذرخش از کمینگاه حمله می‌برد و با ضربه زدن به نقاط حساس، اهداف را از پای درمی‌آورد."
            );
        }
        else if (n.Contains("druid") || n.Contains("shaman") || n.Contains("cleric") || n.Contains("alchemist"))
        {
            return LanguageSelectionUI.Localize(
                "A sacred spiritual conduit channeling forest life essence to mend companions, empower tree defenses, and turn the tide of war.",
                ar: "وسيط روحي مقدّس يسخر طاقة الغابة الحيوية لشفاء الحلفاء، تعزيز دروع الشجرة، وقلب موازين المعركة.",
                ru: "Хранитель природных сил, исцеляющий союзников и укрепляющий защиту священного древа.",
                fr: "Un guide spirituel canalisant la sève vitale pour soigner ses alliés et renforcer le grand arbre.",
                es: "Un guía espiritual que canaliza la fuerza del bosque para curar aliados y reforzar el árbol.",
                de: "Ein spiritueller Heiler, der die Lebenskraft des Hains leitet, um Gefährten zu heilen und den Baum zu stärken.",
                hi: "वन की जीवन ऊर्जा को प्रवाहित करने वाला पवित्र साधक, जो साथियों को ठीक करता है।",
                zh: "连接大地生机的自然灵修贤者，呼唤远古甘霖治愈同袍，赋予神木生生不息的护佑。",
                ja: "大自然の生命力を操る導き手。仲間の傷を癒やし、神木の防壁を強化して戦局を覆す。",
                pt: "Um canalizador espiritual que usa a essência da floresta para curar aliados e fortalecer a árvore.",
                it: "Un condotto spirituale che incanala l'essenza della foresta per curare gli alleati e difendere l'albero.",
                tr: "Müttefikleri iyileştirmek ve ağaç savunmasını güçlendirmek için orman yaşam enerjisini yönlendiren ruhani rehber.",
                fa: "واسطه‌ای معنوی که جوهره حیات جنگل را برای درمان یاران و تقویت پایداری درخت به کار می‌بندد."
            );
        }
        else
        {
            return LanguageSelectionUI.Localize(
                "A seasoned guardian of the colossal ancient tree, harnessing primordial magic and martial prowess to defend sacred altars and lead expeditions to the apex canopy.",
                ar: "حارس متمرس للشجرة القديمة العملاقة، يمتلك مهارات قتالية وسحراً مقدساً لحماية المذابح وقيادة الحملات نحو قمة الشجرة.",
                ru: "Опытный страж древнего исполинского древа, защищающий священные алтари и ведущий походы к вершинам кроны.",
                fr: "Un gardien aguerri de l'arbre colossal ancestral, défendant les autels sacrés et guidant les expéditions.",
                es: "Un experimentado guardián del árbol antiguo colosal, defensor de los altares sagrados.",
                de: "Ein erfahrener Wächter des kolossalen alten Baumes, der heilige Altäre verteidigt und Expeditionen anführt.",
                hi: "विशाल प्राचीन वृक्ष का एक अनुभवी संरक्षक, जो पवित्र वेदियों की रक्षा करता है।",
                zh: "巨树古国的资深守护者，运用神圣法力捍卫祭坛并引领登顶远征。",
                ja: "巨大な古樹の熟練の守護者。聖なる祭壇を守り、頂上への遠征を導く。",
                pt: "Um guardião veterano da colossal árvore antiga, defendendo os altares sagrados.",
                it: "Un esperto guardiano dell'albero antico colossale, difensore degli altari sacri e guida delle spedizioni.",
                tr: "Kutsal sunakları korumak ve zirveye giden seferleri yönetmek için kadim güçleri kullanan tecrübeli muhafız.",
                fa: "نگهبانی باتجربه از درخت کهنسال و غول‌پیکر که از محراب‌های مقدس محافظت می‌کند و راهبر نبردهاست."
            );
        }
    }
    // ==================== LUXURY STAT SLIDER (IMAGE 2 STYLE) ====================
    void DrawLuxuryStatSlider(Rect rect, string label, float valueNorm, int scoreVal, Color barColor)
    {
        valueNorm = Mathf.Clamp01(valueNorm);

        float labelW = rect.width * 0.32f;
        float scoreW = 46f;
        float barW = rect.width - labelW - scoreW - 12f;

        // Label on left
        GUIStyle lblStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(rect.width * 0.040f), 10, 12),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.92f, 0.90f, 0.82f) }
        };
        GUI.Label(new Rect(rect.x, rect.y, labelW, rect.height), label, lblStyle);

        // Thin luxury groove in center
        float grooveH = 6f;
        float grooveY = rect.y + (rect.height - grooveH) * 0.5f;
        float barX = rect.x + labelW + 4f;
        Rect grooveRect = new Rect(barX, grooveY, barW, grooveH);

        // Groove background
        GUI.color = new Color(0.04f, 0.07f, 0.05f, 0.95f);
        GUI.DrawTexture(grooveRect, Texture2D.whiteTexture);

        // Active fill
        if (valueNorm > 0.01f)
        {
            Rect fillRect = new Rect(barX, grooveY, barW * valueNorm, grooveH);
            GUI.color = new Color(barColor.r, barColor.g, barColor.b, 0.92f);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

            // Leading edge highlight
            float edgeX = barX + barW * valueNorm - 2f;
            GUI.color = new Color(1f, 1f, 1f, 0.75f);
            GUI.DrawTexture(new Rect(edgeX, grooveY, 2f, grooveH), Texture2D.whiteTexture);
        }

        // Gold border for groove
        DrawBorder(grooveRect, new Color(0.70f, 0.60f, 0.25f, 0.65f), 1f);
        GUI.color = Color.white;

        // Exact numeric score on right (Image 2 style: 780, 550, 465, 607)
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(rect.width * 0.040f), 10, 12),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(1.0f, 0.88f, 0.40f) }
        };
        GUI.Label(new Rect(rect.x + rect.width - scoreW, rect.y, scoreW, rect.height), scoreVal.ToString(), scoreStyle);
    }

    // ==================== HERO FANTASY EPITHETS (IMAGE 2 STYLE) ====================
    string GetHeroEpithet(string charName)
    {
        string low = charName != null ? charName.ToLower() : "";
        if (low.Contains("archer")) return LanguageSelectionUI.Localize("the Forest Warden", "Ø­Ø§Ø±Ø³Ø© Ø§Ù„ØºØ§Ø¨Ø©", "Ð¡Ñ‚Ñ€Ð°Ð¶ Ð›ÐµÑÐ°", "Gardienne des Bois", "Guardiana del Bosque", "WaldhÃ¼terin", "à¤µà¤¨ à¤¸à¤‚à¤°à¤•à¥à¤·à¤•", "æ£®æž—å®ˆå«", "æ£®ã®ç•ªäºº", "GuardiÃ£ da Floresta", "Ù†Ú¯Ù‡Ø¨Ø§Ù† Ø¬Ù†Ú¯Ù„");
        if (low.Contains("paladin")) return LanguageSelectionUI.Localize("the Dawnpiercer", "Ù‚Ø§Ù‡Ø±Ø© Ø§Ù„Ø¸Ù„Ø§Ù…", "ÐŸÑ€Ð¾Ð½Ð·Ð°ÑŽÑ‰Ð°Ñ Ð Ð°ÑÑÐ²ÐµÑ‚", "Perce-Aube", "Perforaalbas", "DÃ¤mmerungsbrecherin", "à¤­à¥‹à¤°-à¤µà¥‡à¤§à¤•", "ç ´æ™“ä¹‹åˆƒ", "æšã®ç©¿ã¡æ‰‹", "Perfuradora da Alvorada", "Ø´Ú©Ø§ÙÙ†Ø¯Ù‡ Ø³Ù¾ÛŒØ¯Ù‡â€ŒØ¯Ù…");
        if (low.Contains("warrior")) return LanguageSelectionUI.Localize("the Barkwarden", "Ø­Ø§Ù…ÙŠ Ø§Ù„Ù„Ø­Ø§Ø¡", "Ð¡Ñ‚Ñ€Ð°Ð¶ ÐšÐ¾Ñ€Ñ‹", "Garde-Ã‰corce", "GuardiÃ¡n de la Corteza", "RindenwÃ¤chter", "à¤›à¤¾à¤² à¤¸à¤‚à¤°à¤•à¥à¤·à¤•", "æ ‘çš®å®ˆå«", "æ¨¹çš®ã®ç•ªäºº", "GuardiÃ£o da Casca", "Ù†Ú¯Ù‡Ø¨Ø§Ù† Ù¾ÙˆØ³Øª Ø¯Ø±Ø®Øª");
        if (low.Contains("berserker")) return LanguageSelectionUI.Localize("the Cinderveil", "Ù„Ù‡ÙŠØ¨ Ø§Ù„Ø±Ù…Ø§Ø¯", "ÐŸÐµÐ¿ÐµÐ»ÑŒÐ½Ñ‹Ð¹ Ð’Ð¸Ñ…Ñ€ÑŒ", "Voile de Cendre", "Velo de Ceniza", "Aschenschleier", "à¤°à¤¾à¤– à¤•à¤¾ à¤ªà¤°à¤¦à¤¾", "ç°çƒ¬ä¹‹å¹•", "ç°ã®å¸³", "VÃ©u das Cinzas", "Ù†Ù‚Ø§Ø¨ Ø®Ø§Ú©Ø³ØªØ±");
        if (low.Contains("mage")) return LanguageSelectionUI.Localize("the Blightfang", "Ø­Ø§Ø¦Ùƒ Ø§Ù„ØªØ¹Ø§ÙˆÙŠØ°", "Ð“Ð¸Ð±ÐµÐ»ÑŒÐ½Ñ‹Ð¹ ÐšÐ»Ñ‹Ðº", "Croc FlÃ©au", "Colmillo Corrupto", "Pestzahn", "à¤µà¤¿à¤¨à¤¾à¤¶à¤• à¤¦à¤‚à¤¤", "æž¯èŽä¹‹ç‰™", "ç—…ç‰™ã®é­”è¡“å¸«", "Presa Corrompida", "Ù†ÛŒØ´ Ø¢ÙØª");
        if (low.Contains("tank") || low.Contains("ironclad")) return LanguageSelectionUI.Localize("the Earthshaker", "Ø²Ù„Ø²Ø§Ù„ Ø§Ù„Ø£Ø±Ø¶", "Ð¡Ð¾Ñ‚Ñ€ÑÑÐ°Ñ‚ÐµÐ»ÑŒ Ð—ÐµÐ¼Ð»Ð¸", "Secoueur de Terre", "Sacudidor de Tierras", "ErderschÃ¼tterer", "à¤§à¤°à¤¤à¥€ à¤•à¤‚à¤ªà¤•à¤‚à¤ªà¤¾à¤¨à¥‡ à¤µà¤¾à¤²à¤¾", "éœ‡åœ°å·¨å…½", "å¤§åœ°ã‚’æºã‚‹ãŒã™è€…", "Abalador da Terra", "Ù„Ø±Ø²Ø§Ù†Ù†Ø¯Ù‡ Ø²Ù…ÛŒÙ†");
        if (low.Contains("assassin") || low.Contains("shadowblade")) return LanguageSelectionUI.Localize("the Nightstalker", "Ø´Ø¨Ø­ Ø§Ù„Ø¸Ù„Ø§Ù„", "ÐÐ¾Ñ‡Ð½Ð¾Ð¹ ÐžÑ…Ð¾Ñ‚Ð½Ð¸Ðº", "Traqueur Nocturne", "Acechador Nocturno", "Nachtpirscher", "à¤¨à¤¿à¤¶à¤¾à¤šà¤° à¤¶à¤¿à¤•à¤¾à¤°à¥€", "æš—å¤œæ½œè¡Œè€…", "å¤œã®ç‹©äºº", "Espreitador Noturno", "Ø´Ú©Ø§Ø±Ú†ÛŒ Ø´Ø¨");
        if (low.Contains("bomber")) return LanguageSelectionUI.Localize("the Doomhammer", "Ù…Ø·Ø±Ù‚Ø© Ø§Ù„Ù‡Ù„Ø§Ùƒ", "ÐœÐ¾Ð»Ð¾Ñ‚ Ð Ð¾ÐºÐ°", "Marteau Funeste", "Martillo del Juicio", "Schicksalshammer", "à¤ªà¥à¤°à¤²à¤¯ à¤•à¤¾ à¤¹à¤¥à¥Œà¤¡à¤¼à¤¾", "æœ«æ—¥é‡é”¤", "ç ´æ»…ã®æ§Œ", "Martelo da PerdiÃ§Ã£o", "Ù¾ØªÚ© Ù‡Ù„Ø§Ú©Øª");
        if (low.Contains("shaman")) return LanguageSelectionUI.Localize("the Spiritweaver", "Ù†Ø§Ø·Ù‚ Ø§Ù„Ø£Ø±ÙˆØ§Ø­", "Ð¢ÐºÐ°Ñ‡ Ð”ÑƒÑ…Ð¾Ð²", "Tisseur d'Esprits", "Tejedor de EspÃ­ritus", "Geisterweber", "à¤†à¤¤à¥à¤®à¤¾ à¤¬à¥à¤¨à¤•à¤°", "ç»‡çµè¨æ»¡", "ç²¾éœŠç¹”ã‚Š", "TecelÃ£o de EspÃ­ritos", "Ø±ÙˆØ­â€ŒØ¨Ø§Ù");
        if (low.Contains("druid")) return LanguageSelectionUI.Localize("the Canopycaller", "Ø­ÙƒÙŠÙ… Ø§Ù„Ø·Ø¨ÙŠØ¹Ø©", "Ð“Ð¾Ð»Ð¾Ñ ÐšÑ€Ð¾Ð½", "Voix des Cimes", "Llamador del Dosel", "Kronenrufer", "à¤µà¤¿à¤¤à¤¾à¤¨ à¤†à¤¹à¥à¤µà¤¾à¤¯à¤•", "æ ‘å† å‘¼å”¤è€…", "æ¨¹å† ã®å‘¼ã³æ‰‹", "Invocador da Copa", "ÙØ±Ø§Ø®ÙˆØ§Ù†Ù†Ø¯Ù‡ Ø³Ø§ÛŒØ¨Ø§Ù†");
        return LanguageSelectionUI.Localize("the Sacred Sentinel", "Ø§Ù„Ø­Ø§Ø±Ø³ Ø§Ù„Ù…Ù‚Ø¯Ø³", "Ð¡Ð²ÑÑ‰ÐµÐ½Ð½Ñ‹Ð¹ Ð¡Ñ‚Ñ€Ð°Ð¶", "Sentinelle SacrÃ©e", "Centinela Sagrado", "Heiliger WÃ¤chter", "à¤ªà¤µà¤¿à¤¤à¥à¤° à¤¸à¤‚à¤¤à¤°à¥€", "ç¥žåœ£å“¨å…µ", "è–ãªã‚‹ç•ªå…µ", "Sentinela Sagrado", "Ø¯ÛŒØ¯Ù‡â€ŒØ¨Ø§Ù† Ù…Ù‚Ø¯Ø³");
    }

    // ==================== BOTTOM NAVIGATION STRIP (IMAGE 2 STYLE) ====================
    void DrawBottomNavigationStrip(float sw, float sh)
    {
        float barY = sh * 0.970f;
        float barH = sh * 0.030f;

        GUIStyle hintKeyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(sw * 0.010f), 10, 12),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.95f, 0.85f, 0.35f) }
        };

        // Left Navigation Hints: [B] Back    [A] Select
        string backHint = LanguageSelectionUI.Localize("â’· Back", "â’· Ø±Ø¬ÙˆØ¹", "â’· ÐÐ°Ð·Ð°Ð´", "â’· Retour", "â’· Volver", "â’· ZurÃ¼ck", "â’· à¤µà¤¾à¤ªà¤¸", "â’· è¿”å›ž", "â’· æˆ»ã‚‹", "â’· Voltar", "â’· Ø¨Ø§Ø²Ú¯Ø´Øª");
        string selectHint = LanguageSelectionUI.Localize("â’¶ Select", "â’¶ Ø§Ø®ØªÙŠØ§Ø±", "â’¶ Ð’Ñ‹Ð±Ñ€Ð°Ñ‚ÑŒ", "â’¶ Choisir", "â’¶ Elegir", "â’¶ WÃ¤hlen", "â’¶ à¤šà¥à¤¨à¥‡à¤‚", "â’¶ é€‰æ‹©", "â’¶ é¸æŠž", "â’¶ Escolher", "â’¶ Ø§Ù†ØªØ®Ø§Ø¨");

        GUI.Label(new Rect(sw * 0.025f, barY, 120f, barH), backHint, hintKeyStyle);
        GUI.Label(new Rect(sw * 0.025f + 110f, barY, 130f, barH), selectHint, hintKeyStyle);

        // Right Navigation Hint: [ESC] Exit
        GUIStyle exitKeyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(sw * 0.010f), 10, 12),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.85f, 0.75f, 0.40f) }
        };
        string exitHint = LanguageSelectionUI.Localize("â“† Exit (Esc)", "â“† Ø®Ø±ÙˆØ¬ (Esc)", "â“† Ð’Ñ‹Ñ…Ð¾Ð´ (Esc)", "â“† Quitter (Ã‰chap)", "â“† Salir (Esc)", "â“† Beenden (Esc)", "â“† à¤¬à¤¾à¤¹à¤° à¤¨à¤¿à¤•à¤²à¥‡à¤‚", "â“† é€€å‡º (Esc)", "â“† çµ‚äº† (Esc)", "â“† Sair (Esc)", "â“† Ø®Ø±ÙˆØ¬ (Esc)");
        GUI.Label(new Rect(sw * 0.975f - 180f, barY, 180f, barH), exitHint, exitKeyStyle);
    }
}