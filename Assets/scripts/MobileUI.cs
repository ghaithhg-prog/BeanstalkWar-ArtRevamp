using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// MobileUI (Free Fire Battle Royale Style):
/// A comprehensive mobile controls suite for BeanstalkWar modeled after Garena Free Fire.
/// Features:
/// 1. Tactile Left-Side Analog Virtual Joystick with directional compass and auto-center calibration.
/// 2. Left-Side Sprint Lock Toggle (🏃) for seamless hands-free sprinting.
/// 3. Free Fire Signature Right-Side Combat Arc:
///    - Primary Attack / Fire Button (⚔️ / 🎯) with continuous hold-to-attack and tactile press scale.
///    - Jump Button (🦘 / ⬆️) placed along the natural thumb arc above attack.
///    - Dash / Speed Burst Button (⚡).
///    - Ultimate Ability Button (R Key Skill 🔥) with Lv 5 lock status and live circular cooldown timer.
///    - Smart Contextual Interact Capsule (🖐️ / 💎 / 🪓 / 🏛️ - E/F/Q keys) adapting dynamically near crystals, altar, or tree.
/// 4. Full-Screen Right-Side Touch Aim & Camera Look Swipe (aim and rotate 3D camera smoothly by dragging thumb).
/// 5. Top Quick Utility Bar:
///    - ⚙️ Pause & Settings (ESC)
///    - 🌐 Language Selection (L)
///    - 🎒 Shop & Skins (B)
///    - 📖 Guide & Boss Seals (H / F1)
///    - 🎬 Video Walkthrough (V)
///    - 📱 PC/Mobile HUD Switcher (F2)
/// 6. Complete Cross-Platform Compatibility:
///    - Works automatically on Android, iOS, and handheld devices.
///    - Active and testable in Unity Editor & on PC with mouse clicks and F2 toggle.
///    - Fully localized across all 10 languages with Arabic RTL support.
/// </summary>
public class MobileUI : MonoBehaviour
{
    public static MobileUI Instance { get; private set; }

    [Header("Existing Scene References (Auto-Retargeted & Styled)")]
    public Button attackButton;
    public Button jumpButton;
    public SimpleJoystick joystick;
    public GameObject joystickBackground;

    [Header("Free Fire Extended Combat Buttons")]
    public Button sprintButton;
    public Button dashButton;
    public Button ultimateButton;
    public Button interactButton;
    public Button dropCrystalButton;

    [Header("Top Quick Utility Buttons")]
    public Button settingsButton;
    public Button languageButton;
    public Button shopButton;
    public Button guideButton;
    public Button videoGuideButton;
    public Button pcToggleButton;

    [Header("Status & Settings")]
    public static bool ForceShowMobileControls = true; // Enabled by default so users can immediately test on PC/Editor!
    public static bool IsMobileActive { get; private set; } = false;

    // Local Player & Combat References
    private PlayerMovement localPlayer;
    private CharacterCombat localCombat;
    private PlayerXP localXP;

    // Cached Textures & Sprites
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    // Contextual Interact State
    private string currentInteractPrompt = "INTERACT";
    private System.Action currentInteractAction = null;
    private bool showDropButton = false;
    private float pulseTimer = 0f;

    // Ultimate Button Text & Overlay
    private Text ultimateLevelText;
    private Image ultimateCooldownOverlay;
    private Text interactButtonText;

    // Canvas & Container
    private Canvas targetCanvas;
    private RectTransform hudRoot;
    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        InitializeFreeFireHUD();
    }

    void Update()
    {
        // F2 Toggle for developers/players on PC to easily show or hide mobile HUD
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ForceShowMobileControls = !ForceShowMobileControls;
            Debug.Log($"📱 [MobileUI] Free Fire Mobile HUD toggled: {ForceShowMobileControls}");
        }

        // Determine if mobile HUD should be visible
        bool shouldShow = Application.isMobilePlatform || 
                          SystemInfo.deviceType == DeviceType.Handheld || 
                          ForceShowMobileControls;

        // Auto-hide during modal menus (Language, Pause, Character Selection, Shop, PlayMode)
        if (LanguageSelectionUI.IsLanguageModalOpen || 
            InGamePauseMenuUI.IsPauseMenuOpen || 
            CharacterSelectionUI.IsSelectionOpen || 
            PlayerProgressionAndShopUI.IsShopOpen || 
            PlayModeUI.IsPlayModeMenuOpen ||
            GameIntroBriefingUI.IsBriefingOpen ||
            MatchEndStatsUI.IsGameOverActive)
        {
            shouldShow = false;
        }

        IsMobileActive = shouldShow;

        if (hudRoot != null && hudRoot.gameObject.activeSelf != shouldShow)
        {
            hudRoot.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow) return;

        // Keep local player references hooked
        EnsurePlayerConnected();

        // Update Dynamic Contextual Button (Crystal, Tree, Altar, Shockwave)
        UpdateContextualInteractions();

        // Update Ultimate Button Cooldown / Level Lock
        UpdateUltimateButtonState();

        // Animate pulse timer
        pulseTimer += Time.deltaTime * 3.5f;
    }

    // =========================================================================
    // HUD INITIALIZATION & FREE FIRE STYLING
    // =========================================================================

    public void InitializeFreeFireHUD()
    {
        if (isInitialized) return;

        targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
        if (targetCanvas == null) targetCanvas = FindAnyObjectByType<Canvas>();

        if (targetCanvas == null)
        {
            GameObject canvasObj = new GameObject("FreeFireMobileCanvas");
            targetCanvas = canvasObj.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Ensure EventSystem exists
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Main HUD container root
        GameObject rootObj = new GameObject("FreeFireHUDRoot", typeof(RectTransform));
        rootObj.transform.SetParent(targetCanvas.transform, false);
        hudRoot = rootObj.GetComponent<RectTransform>();
        hudRoot.anchorMin = Vector2.zero;
        hudRoot.anchorMax = Vector2.one;
        hudRoot.sizeDelta = Vector2.zero;
        hudRoot.anchoredPosition = Vector2.zero;

        // 1. Setup Joystick & Sprint Lock (Bottom-Left)
        SetupJoystickCluster();

        // 2. Setup Combat Diamond Arc (Bottom-Right)
        SetupCombatCluster();

        // 3. Setup Top Quick Utility Bar
        SetupTopUtilityBar();

        isInitialized = true;
    }

    private void SetupJoystickCluster()
    {
        // 1. Virtual Joystick Base
        RectTransform joyBaseRT;
        if (joystickBackground != null)
        {
            joyBaseRT = joystickBackground.GetComponent<RectTransform>();
            joyBaseRT.SetParent(hudRoot, false);
        }
        else
        {
            GameObject jb = new GameObject("FF_JoystickBase", typeof(RectTransform), typeof(Image));
            jb.transform.SetParent(hudRoot, false);
            joyBaseRT = jb.GetComponent<RectTransform>();
            joystickBackground = jb;
        }

        joyBaseRT.anchorMin = new Vector2(0f, 0f);
        joyBaseRT.anchorMax = new Vector2(0f, 0f);
        joyBaseRT.pivot = new Vector2(0.5f, 0.5f);
        joyBaseRT.anchoredPosition = new Vector2(210f, 210f);
        joyBaseRT.sizeDelta = new Vector2(190f, 190f);

        Image baseImg = joyBaseRT.GetComponent<Image>();
        if (baseImg != null)
        {
            baseImg.sprite = GetJoystickBaseSprite();
            baseImg.color = Color.white;
            baseImg.raycastTarget = true;
        }

        // 2. Virtual Joystick Knob
        RectTransform joyKnobRT = null;
        if (joystick != null && joystick.handle != null)
        {
            joyKnobRT = joystick.handle;
            joyKnobRT.SetParent(joyBaseRT, false);
        }
        else
        {
            GameObject jk = new GameObject("FF_JoystickKnob", typeof(RectTransform), typeof(Image));
            jk.transform.SetParent(joyBaseRT, false);
            joyKnobRT = jk.GetComponent<RectTransform>();
        }

        joyKnobRT.anchorMin = new Vector2(0.5f, 0.5f);
        joyKnobRT.anchorMax = new Vector2(0.5f, 0.5f);
        joyKnobRT.pivot = new Vector2(0.5f, 0.5f);
        joyKnobRT.anchoredPosition = Vector2.zero;
        joyKnobRT.sizeDelta = new Vector2(85f, 85f);

        Image knobImg = joyKnobRT.GetComponent<Image>();
        if (knobImg != null)
        {
            knobImg.sprite = GetJoystickKnobSprite();
            knobImg.color = Color.white;
            knobImg.raycastTarget = false;
        }

        if (joystick == null)
        {
            joystick = joyBaseRT.gameObject.AddComponent<SimpleJoystick>();
        }
        joystick.handle = joyKnobRT;
        joystick.moveRange = 75f;
        joystick.RecalibrateStartPos();

        // 3. Sprint Lock / Auto-Run Toggle (🏃) placed above the joystick
        sprintButton = CreateStyledCircularButton(
            "FF_SprintLockBtn",
            hudRoot,
            new Vector2(0f, 0f),
            new Vector2(210f, 360f),
            new Vector2(72f, 72f),
            GetSprintSprite(false),
            () =>
            {
                PlayerMovement.MobileSprintActive = !PlayerMovement.MobileSprintActive;
                UpdateSprintButtonVisual();
            }
        );
    }

    private void SetupCombatCluster()
    {
        // 1. Primary Fire / Attack Button (Large Circle at Bottom-Right)
        if (attackButton != null)
        {
            RectTransform abRT = attackButton.GetComponent<RectTransform>();
            abRT.SetParent(hudRoot, false);
            abRT.anchorMin = new Vector2(1f, 0f);
            abRT.anchorMax = new Vector2(1f, 0f);
            abRT.pivot = new Vector2(0.5f, 0.5f);
            abRT.anchoredPosition = new Vector2(-165f, 165f);
            abRT.sizeDelta = new Vector2(130f, 130f);

            Image abImg = attackButton.GetComponent<Image>();
            if (abImg != null)
            {
                abImg.sprite = GetFireButtonSprite();
                abImg.color = Color.white;
            }

            // Remove legacy child texts that look ugly
            for (int i = abRT.childCount - 1; i >= 0; i--)
            {
                Destroy(abRT.GetChild(i).gameObject);
            }

            AddTactileFeedback(attackButton.gameObject, () =>
            {
                if (localCombat != null) localCombat.SetAttackButtonHeld(true);
            },
            () =>
            {
                if (localCombat != null) localCombat.SetAttackButtonHeld(false);
            });
        }
        else
        {
            attackButton = CreateStyledCircularButton(
                "FF_AttackBtn",
                hudRoot,
                new Vector2(1f, 0f),
                new Vector2(-165f, 165f),
                new Vector2(130f, 130f),
                GetFireButtonSprite(),
                null,
                () => { if (localCombat != null) localCombat.SetAttackButtonHeld(true); },
                () => { if (localCombat != null) localCombat.SetAttackButtonHeld(false); }
            );
        }

        // 2. Jump Button (Above Attack Button along thumb arc)
        if (jumpButton != null)
        {
            RectTransform jbRT = jumpButton.GetComponent<RectTransform>();
            jbRT.SetParent(hudRoot, false);
            jbRT.anchorMin = new Vector2(1f, 0f);
            jbRT.anchorMax = new Vector2(1f, 0f);
            jbRT.pivot = new Vector2(0.5f, 0.5f);
            jbRT.anchoredPosition = new Vector2(-150f, 335f);
            jbRT.sizeDelta = new Vector2(85f, 85f);

            Image jbImg = jumpButton.GetComponent<Image>();
            if (jbImg != null)
            {
                jbImg.sprite = GetJumpButtonSprite();
                jbImg.color = Color.white;
            }

            for (int i = jbRT.childCount - 1; i >= 0; i--)
            {
                Destroy(jbRT.GetChild(i).gameObject);
            }

            AddTactileFeedback(jumpButton.gameObject, () =>
            {
                if (localPlayer != null) localPlayer.OnJumpButtonPressed();
            }, null);
        }
        else
        {
            jumpButton = CreateStyledCircularButton(
                "FF_JumpBtn",
                hudRoot,
                new Vector2(1f, 0f),
                new Vector2(-150f, 335f),
                new Vector2(85f, 85f),
                GetJumpButtonSprite(),
                () => { if (localPlayer != null) localPlayer.OnJumpButtonPressed(); }
            );
        }

        // 3. Dash / Sprint Burst Button (To the left of Jump)
        dashButton = CreateStyledCircularButton(
            "FF_DashBtn",
            hudRoot,
            new Vector2(1f, 0f),
            new Vector2(-275f, 260f),
            new Vector2(80f, 80f),
            GetDashButtonSprite(),
            () =>
            {
                // Instant quick sprint jump
                if (localPlayer != null)
                {
                    PlayerMovement.MobileSprintActive = true;
                    localPlayer.OnJumpButtonPressed();
                }
            }
        );

        // 4. Ultimate Ability Button (R Key - Skill Blast)
        ultimateButton = CreateStyledCircularButton(
            "FF_UltimateBtn",
            hudRoot,
            new Vector2(1f, 0f),
            new Vector2(-270f, 140f),
            new Vector2(85f, 85f),
            GetUltimateButtonSprite(true),
            () =>
            {
                if (localXP != null)
                {
                    localXP.TryActivateUltimate();
                }
            }
        );

        // Cooldown radial overlay for Ultimate button
        GameObject cdOverlayObj = new GameObject("FF_UltimateCooldown", typeof(RectTransform), typeof(Image));
        cdOverlayObj.transform.SetParent(ultimateButton.transform, false);
        RectTransform cdRT = cdOverlayObj.GetComponent<RectTransform>();
        cdRT.anchorMin = Vector2.zero;
        cdRT.anchorMax = Vector2.one;
        cdRT.sizeDelta = Vector2.zero;
        ultimateCooldownOverlay = cdOverlayObj.GetComponent<Image>();
        ultimateCooldownOverlay.sprite = GetCircleOverlaySprite();
        ultimateCooldownOverlay.type = Image.Type.Filled;
        ultimateCooldownOverlay.fillMethod = Image.FillMethod.Radial360;
        ultimateCooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
        ultimateCooldownOverlay.color = new Color(0.08f, 0.08f, 0.08f, 0.75f);
        ultimateCooldownOverlay.raycastTarget = false;

        // Level text indicator on Ultimate button
        GameObject lvlTextObj = new GameObject("FF_UltimateLvlText", typeof(RectTransform), typeof(Text));
        lvlTextObj.transform.SetParent(ultimateButton.transform, false);
        RectTransform lvlRT = lvlTextObj.GetComponent<RectTransform>();
        lvlRT.anchorMin = Vector2.zero;
        lvlRT.anchorMax = Vector2.one;
        lvlRT.sizeDelta = Vector2.zero;
        ultimateLevelText = lvlTextObj.GetComponent<Text>();
        ultimateLevelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (ultimateLevelText.font == null) ultimateLevelText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        ultimateLevelText.fontSize = 13;
        ultimateLevelText.fontStyle = FontStyle.Bold;
        ultimateLevelText.alignment = TextAnchor.MiddleCenter;
        ultimateLevelText.color = new Color(1f, 0.85f, 0.2f);
        ultimateLevelText.raycastTarget = false;

        // 5. Smart Contextual Interact Button (Capsule Oval - E / F Key)
        interactButton = CreateStyledCapsuleButton(
            "FF_InteractPill",
            hudRoot,
            new Vector2(1f, 0f),
            new Vector2(-220f, 440f),
            new Vector2(170f, 56f),
            GetInteractPillSprite(),
            () =>
            {
                currentInteractAction?.Invoke();
            }
        );

        // Interact Button Text Label
        GameObject labelObj = new GameObject("FF_InteractLabel", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(interactButton.transform, false);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;
        interactButtonText = labelObj.GetComponent<Text>();
        interactButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (interactButtonText.font == null) interactButtonText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        interactButtonText.fontSize = 13;
        interactButtonText.fontStyle = FontStyle.Bold;
        interactButtonText.alignment = TextAnchor.MiddleCenter;
        interactButtonText.color = Color.white;
        interactButtonText.raycastTarget = false;

        // 6. Companion Drop Crystal Button (Q Key)
        dropCrystalButton = CreateStyledCircularButton(
            "FF_DropCrystalBtn",
            hudRoot,
            new Vector2(1f, 0f),
            new Vector2(-340f, 440f),
            new Vector2(56f, 56f),
            GetDropButtonSprite(),
            () =>
            {
                CrystalItem crystal = FindAnyObjectByType<CrystalItem>();
                if (crystal != null) crystal.TriggerCarrierDrop();
            }
        );
        dropCrystalButton.gameObject.SetActive(false);
    }

    private void SetupTopUtilityBar()
    {
        // 1. Pause & Settings Button (⚙️ - ESC on PC)
        settingsButton = CreateStyledCircularButton(
            "FF_SettingsBtn",
            hudRoot,
            new Vector2(1f, 1f),
            new Vector2(-50f, -45f),
            new Vector2(55f, 55f),
            GetSettingsSprite(),
            () =>
            {
                if (InGamePauseMenuUI.Instance != null)
                    InGamePauseMenuUI.Instance.OpenPauseMenu();
            }
        );

        // 2. Language Selection Button (🌐 - L on PC)
        languageButton = CreateStyledCircularButton(
            "FF_LanguageBtn",
            hudRoot,
            new Vector2(1f, 1f),
            new Vector2(-115f, -45f),
            new Vector2(55f, 55f),
            GetLanguageSprite(),
            () =>
            {
                if (LanguageSelectionUI.Instance != null)
                    LanguageSelectionUI.Instance.OpenModal();
            }
        );

        // 3. Shop & Progression Button (🎒 - B on PC)
        shopButton = CreateStyledCircularButton(
            "FF_ShopBtn",
            hudRoot,
            new Vector2(1f, 1f),
            new Vector2(-180f, -45f),
            new Vector2(55f, 55f),
            GetShopSprite(),
            () =>
            {
                if (PlayerProgressionAndShopUI.Instance != null)
                    PlayerProgressionAndShopUI.Instance.OpenShop();
            }
        );

        // 4. Guide & Boss Seals Button (📖 - H / F1 on PC)
        guideButton = CreateStyledCircularButton(
            "FF_GuideBtn",
            hudRoot,
            new Vector2(1f, 1f),
            new Vector2(-245f, -45f),
            new Vector2(55f, 55f),
            GetGuideSprite(),
            () =>
            {
                if (GameIntroBriefingUI.Instance != null)
                    GameIntroBriefingUI.Instance.OpenBriefing();
            }
        );

        // 5. Video Walkthrough Button (🎬 - V on PC)
        videoGuideButton = CreateStyledCircularButton(
            "FF_VideoBtn",
            hudRoot,
            new Vector2(1f, 1f),
            new Vector2(-310f, -45f),
            new Vector2(55f, 55f),
            GetVideoGuideSprite(),
            () =>
            {
                if (GameIntroBriefingUI.Instance != null)
                    GameIntroBriefingUI.Instance.OpenVideoGuide();
            }
        );

        // 6. PC / Mobile HUD Switcher Button (F2 on PC)
        pcToggleButton = CreateStyledCapsuleButton(
            "FF_PCToggleBtn",
            hudRoot,
            new Vector2(0f, 1f),
            new Vector2(65f, -45f),
            new Vector2(75f, 32f),
            GetInteractPillSprite(),
            () =>
            {
                ForceShowMobileControls = !ForceShowMobileControls;
            }
        );

        GameObject toggleLabel = new GameObject("Label", typeof(RectTransform), typeof(Text));
        toggleLabel.transform.SetParent(pcToggleButton.transform, false);
        RectTransform tlRT = toggleLabel.GetComponent<RectTransform>();
        tlRT.anchorMin = Vector2.zero;
        tlRT.anchorMax = Vector2.one;
        tlRT.sizeDelta = Vector2.zero;
        Text t = toggleLabel.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
        t.fontSize = 11;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.35f, 0.85f, 1f);
        t.text = "📱 HUD";
    }

    // =========================================================================
    // BUTTON BUILDER & TACTILE INTERACTION
    // =========================================================================

    private Button CreateStyledCircularButton(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite, System.Action onClick, System.Action onDown = null, System.Action onUp = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.None; // We use FreeFireTouchHandler for smooth spring scale!

        AddTactileFeedback(go, onDown, onUp, onClick);

        return btn;
    }

    private Button CreateStyledCapsuleButton(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite, System.Action onClick)
    {
        return CreateStyledCircularButton(name, parent, anchor, pos, size, sprite, onClick);
    }

    private void AddTactileFeedback(GameObject target, System.Action onDown, System.Action onUp, System.Action onClick = null)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        RectTransform rt = target.GetComponent<RectTransform>();
        Image img = target.GetComponent<Image>();
        Color originalColor = img != null ? img.color : Color.white;

        // PointerDown
        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) =>
        {
            if (rt != null) rt.localScale = Vector3.one * 0.90f;
            if (img != null) img.color = new Color(1.25f, 1.25f, 1.25f, 1f);
            onDown?.Invoke();
        });
        trigger.triggers.Add(downEntry);

        // PointerUp
        EventTrigger.Entry upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        upEntry.callback.AddListener((data) =>
        {
            if (rt != null) rt.localScale = Vector3.one;
            if (img != null) img.color = originalColor;
            onUp?.Invoke();
            onClick?.Invoke();
        });
        trigger.triggers.Add(upEntry);
    }

    private void UpdateSprintButtonVisual()
    {
        if (sprintButton != null)
        {
            Image img = sprintButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetSprintSprite(PlayerMovement.MobileSprintActive);
            }
        }
    }

    // =========================================================================
    // DYNAMIC GAMEPLAY LOGIC & CONTEXTUAL ACTIONS
    // =========================================================================

    private void EnsurePlayerConnected()
    {
        if (localPlayer == null || localCombat == null || localXP == null)
        {
            localPlayer = PlayerMovement.LocalPlayer;
            if (localPlayer == null)
            {
                PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    if (p != null && p.IsOwner)
                    {
                        localPlayer = p;
                        break;
                    }
                }
            }

            if (localPlayer != null)
            {
                localCombat = localPlayer.GetComponent<CharacterCombat>();
                localXP = localPlayer.GetComponent<PlayerXP>();

                if (joystick != null)
                {
                    localPlayer.joystick = joystick;
                }
            }
        }
    }

    private void UpdateContextualInteractions()
    {
        if (interactButton == null) return;

        bool hasCarriedCrystal = localPlayer != null && localPlayer.isCarryingCrystal.Value;

        if (hasCarriedCrystal)
        {
            // Case 1: Carrying Celestial Crystal -> Shockwave (E) & Drop (Q)
            currentInteractPrompt = LanguageSelectionUI.Localize(
                "⚡ SHOCKWAVE",
                ar: "⚡ موجة صاعقة",
                ru: "⚡ ВОЛНА",
                fr: "⚡ ONDE DE CHOC",
                es: "⚡ ONDA DE CHOQUE",
                de: "⚡ SCHOCKWELLE",
                hi: "⚡ आघात तरंग",
                zh: "⚡ 冲击波",
                ja: "⚡ 衝撃波",
                pt: "⚡ ONDA DE CHOQUE"
            );

            currentInteractAction = () =>
            {
                CrystalItem crystal = FindAnyObjectByType<CrystalItem>();
                if (crystal != null) crystal.TriggerCarrierShockwave();
            };

            showDropButton = true;
        }
        else
        {
            showDropButton = false;

            // Case 2: Near Fallen Crystal
            CrystalItem fallenCrystal = FindAnyObjectByType<CrystalItem>();
            if (fallenCrystal != null && fallenCrystal.IsAvailableForPickup && localPlayer != null &&
                Vector3.Distance(localPlayer.transform.position, fallenCrystal.transform.position) <= fallenCrystal.pickupRadius * 1.6f)
            {
                currentInteractPrompt = LanguageSelectionUI.Localize(
                    "💎 TAKE CRYSTAL",
                    ar: "💎 أخذ الكريستالة",
                    ru: "💎 ВЗЯТЬ КРИСТАЛЛ",
                    fr: "💎 PRENDRE LE CRISTAL",
                    es: "💎 TOMAR CRISTAL",
                    de: "💎 KRISTALL NEHMEN",
                    hi: "💎 क्रिस्टल उठाएं",
                    zh: "💎 拾取水晶",
                    ja: "💎 クリスタルを拾う",
                    pt: "💎 PEGAR CRISTAL"
                );

                currentInteractAction = () =>
                {
                    fallenCrystal.TryPickupServer(localPlayer.gameObject);
                };
            }
            // Case 3: Near Sacred Tree
            else if (localPlayer != null && TreeGrowth.Instance != null &&
                     Vector3.Distance(localPlayer.transform.position, TreeGrowth.Instance.transform.position) <= 8.5f)
            {
                if (localPlayer.team == PlayerMovement.TeamType.Destroyer)
                {
                    currentInteractPrompt = LanguageSelectionUI.Localize(
                        "🪓 CHOP TREE",
                        ar: "🪓 قطع الشجرة",
                        ru: "🪓 РУБИТЬ ДРЕВО",
                        fr: "🪓 COUPER L'ARBRE",
                        es: "🪓 TALAR ÁRBOL",
                        de: "🪓 BAUM FÄLLEN",
                        hi: "🪓 पेड़ काटें",
                        zh: "🪓 伐木砍树",
                        ja: "🪓 木を伐採する",
                        pt: "🪓 DERRUBAR ÁRVORE"
                    );

                    currentInteractAction = () =>
                    {
                        if (localCombat != null) localCombat.OnAttackButtonPressed();
                    };
                }
                else
                {
                    currentInteractPrompt = LanguageSelectionUI.Localize(
                        "🛡️ DEFEND TREE",
                        ar: "🛡️ حماية الشجرة",
                        ru: "🛡️ ЗАЩИТА ДРЕВА",
                        fr: "🛡️ DÉFENDRE L'ARBRE",
                        es: "🛡️ DEFENDER ÁRBOL",
                        de: "🛡️ BAUM VERTEIDIGEN",
                        hi: "🛡️ पेड़ की रक्षा करें",
                        zh: "🛡️ 守护巨树",
                        ja: "🛡️ 神木を守護",
                        pt: "🛡️ DEFENDER ÁRVORE"
                    );

                    currentInteractAction = () =>
                    {
                        if (localCombat != null) localCombat.OnAttackButtonPressed();
                    };
                }
            }
            // Case 4: Near Altar
            else if (localPlayer != null && CrystalAltar.GetDistanceToClosestAltar(localPlayer.transform.position) <= 7.0f)
            {
                currentInteractPrompt = LanguageSelectionUI.Localize(
                    "🏛️ ALTAR",
                    ar: "🏛️ المذبح المقدس",
                    ru: "🏛️ АЛТАРЬ",
                    fr: "🏛️ AUTEL",
                    es: "🏛️ ALTAR",
                    de: "🏛️ ALTAR",
                    hi: "🏛️ वेदी",
                    zh: "🏛️ 祭坛",
                    ja: "🏛️ 祭壇",
                    pt: "🏛️ ALTAR"
                );

                currentInteractAction = () =>
                {
                    // Altar interaction
                };
            }
            else
            {
                // Default Action
                currentInteractPrompt = LanguageSelectionUI.Localize(
                    "🖐️ ACTION (E)",
                    ar: "🖐️ تفاعل (E)",
                    ru: "🖐️ ДЕЙСТВИЕ (E)",
                    fr: "🖐️ ACTION (E)",
                    es: "🖐️ ACCIÓN (E)",
                    de: "🖐️ AKTION (E)",
                    hi: "🖐️ क्रिया (E)",
                    zh: "🖐️ 互动 (E)",
                    ja: "🖐️ アクション (E)",
                    pt: "🖐️ AÇÃO (E)"
                );

                currentInteractAction = () =>
                {
                    if (localCombat != null) localCombat.OnAttackButtonPressed();
                };
            }
        }

        if (interactButtonText != null)
        {
            interactButtonText.text = currentInteractPrompt;
        }

        if (dropCrystalButton != null && dropCrystalButton.gameObject.activeSelf != showDropButton)
        {
            dropCrystalButton.gameObject.SetActive(showDropButton);
        }

        // Animated breathing glow on interact button when near objective
        RectTransform ibRT = interactButton.GetComponent<RectTransform>();
        if (ibRT != null)
        {
            float scale = 1.0f + 0.04f * Mathf.Sin(pulseTimer);
            ibRT.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void UpdateUltimateButtonState()
    {
        if (ultimateButton == null) return;

        if (localXP == null)
        {
            if (ultimateLevelText != null) ultimateLevelText.text = "🔒 Lv 5";
            if (ultimateCooldownOverlay != null) ultimateCooldownOverlay.fillAmount = 1f;
            return;
        }

        if (localXP.CurrentLevel < 5)
        {
            if (ultimateLevelText != null)
            {
                ultimateLevelText.gameObject.SetActive(true);
                ultimateLevelText.text = "🔒 Lv 5";
            }
            if (ultimateCooldownOverlay != null) ultimateCooldownOverlay.fillAmount = 1f;
        }
        else
        {
            float remaining = localXP.GetRemainingUltimateCooldown();
            if (remaining > 0.05f)
            {
                if (ultimateLevelText != null)
                {
                    ultimateLevelText.gameObject.SetActive(true);
                    ultimateLevelText.text = remaining.ToString("F1") + "s";
                }
                if (ultimateCooldownOverlay != null)
                {
                    ultimateCooldownOverlay.fillAmount = localXP.GetUltimateCooldownRatio();
                }
            }
            else
            {
                if (ultimateLevelText != null) ultimateLevelText.gameObject.SetActive(false);
                if (ultimateCooldownOverlay != null) ultimateCooldownOverlay.fillAmount = 0f;
            }
        }
    }

    // =========================================================================
    // PROCEDURAL HIGH-RESOLUTION FREE FIRE STYLE SPRITES
    // =========================================================================

    public static Sprite GetJoystickBaseSprite()
    {
        return GetOrCreateSprite("FF_JoyBase", () =>
        {
            int size = 192;
            Color border = new Color(0.2f, 0.85f, 1f, 0.85f);
            Color fill = new Color(0.04f, 0.07f, 0.09f, 0.60f);

            return CreateCircleSprite(size, size, border, fill, 3.5f, (pixels, w, h) =>
            {
                // Draw tactical compass ticks (N, S, E, W)
                int cx = w / 2;
                int cy = h / 2;
                Color tickCol = new Color(0.35f, 0.9f, 1f, 0.9f);

                for (int i = 0; i < 14; i++)
                {
                    // Top (N)
                    pixels[(h - 10 - i) * w + cx] = tickCol;
                    pixels[(h - 10 - i) * w + cx + 1] = tickCol;
                    // Bottom (S)
                    pixels[(10 + i) * w + cx] = tickCol;
                    pixels[(10 + i) * w + cx + 1] = tickCol;
                    // Right (E)
                    pixels[cy * w + (w - 10 - i)] = tickCol;
                    pixels[(cy + 1) * w + (w - 10 - i)] = tickCol;
                    // Left (W)
                    pixels[cy * w + (10 + i)] = tickCol;
                    pixels[(cy + 1) * w + (10 + i)] = tickCol;
                }
            });
        });
    }

    public static Sprite GetJoystickKnobSprite()
    {
        return GetOrCreateSprite("FF_JoyKnob", () =>
        {
            int size = 96;
            Color border = new Color(0.3f, 0.92f, 1f, 0.95f);
            Color fill = new Color(0.10f, 0.16f, 0.20f, 0.85f);

            return CreateCircleSprite(size, size, border, fill, 3.0f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color core = new Color(0.4f, 0.95f, 1f, 1f);

                // Central glowing gem bead
                for (int y = cy - 7; y <= cy + 7; y++)
                {
                    for (int x = cx - 7; x <= cx + 7; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        if (d <= 7f) pixels[y * w + x] = core;
                    }
                }
            });
        });
    }

    public static Sprite GetFireButtonSprite()
    {
        return GetOrCreateSprite("FF_FireBtn", () =>
        {
            int size = 160;
            Color border = new Color(1.0f, 0.72f, 0.15f, 0.95f);
            Color fill = new Color(0.12f, 0.08f, 0.04f, 0.70f);

            return CreateCircleSprite(size, size, border, fill, 4.0f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color reticle = new Color(1.0f, 0.85f, 0.25f, 0.95f);

                // Crosshair reticle lines
                for (int i = 12; i <= 36; i++)
                {
                    // Up, Down, Left, Right
                    pixels[(cy + i) * w + cx] = reticle;
                    pixels[(cy + i) * w + cx + 1] = reticle;
                    pixels[(cy - i) * w + cx] = reticle;
                    pixels[(cy - i) * w + cx + 1] = reticle;
                    pixels[cy * w + (cx + i)] = reticle;
                    pixels[(cy + 1) * w + (cx + i)] = reticle;
                    pixels[cy * w + (cx - i)] = reticle;
                    pixels[(cy + 1) * w + (cx - i)] = reticle;
                }

                // Central fiery core
                for (int y = cy - 8; y <= cy + 8; y++)
                {
                    for (int x = cx - 8; x <= cx + 8; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        if (d <= 8f) pixels[y * w + x] = new Color(1f, 0.35f, 0.1f, 1f);
                    }
                }
            });
        });
    }

    public static Sprite GetJumpButtonSprite()
    {
        return GetOrCreateSprite("FF_JumpBtn", () =>
        {
            int size = 128;
            Color border = new Color(0.20f, 0.85f, 1f, 0.95f);
            Color fill = new Color(0.05f, 0.10f, 0.14f, 0.70f);

            return CreateCircleSprite(size, size, border, fill, 3.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color wing = new Color(0.4f, 0.92f, 1f, 0.95f);

                // Upward chevron wings (^)
                for (int i = -16; i <= 16; i++)
                {
                    int py1 = cy + 12 - Mathf.Abs(i);
                    int py2 = cy - 2 - Mathf.Abs(i);

                    if (py1 >= 0 && py1 < h && (cx + i) >= 0 && (cx + i) < w)
                    {
                        pixels[py1 * w + cx + i] = wing;
                        if (py1 + 1 < h) pixels[(py1 + 1) * w + cx + i] = wing;
                    }
                    if (py2 >= 0 && py2 < h && (cx + i) >= 0 && (cx + i) < w)
                    {
                        pixels[py2 * w + cx + i] = wing;
                        if (py2 + 1 < h) pixels[(py2 + 1) * w + cx + i] = wing;
                    }
                }
            });
        });
    }

    public static Sprite GetSprintSprite(bool active)
    {
        string key = active ? "FF_SprintActive" : "FF_SprintInactive";
        return GetOrCreateSprite(key, () =>
        {
            int size = 128;
            Color border = active ? new Color(1.0f, 0.88f, 0.25f, 1f) : new Color(1.0f, 0.60f, 0.15f, 0.85f);
            Color fill = active ? new Color(0.35f, 0.20f, 0.04f, 0.85f) : new Color(0.12f, 0.08f, 0.04f, 0.65f);

            return CreateCircleSprite(size, size, border, fill, 3.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color runner = active ? Color.white : new Color(1f, 0.85f, 0.4f, 0.95f);

                // Runner figure silhouette
                // Head
                for (int y = cy + 10; y <= cy + 18; y++)
                    for (int x = cx - 2; x <= cx + 6; x++)
                        if ((x - (cx + 2)) * (x - (cx + 2)) + (y - (cy + 14)) * (y - (cy + 14)) <= 16)
                            pixels[y * w + x] = runner;

                // Body
                for (int y = cy - 4; y <= cy + 10; y++)
                {
                    int bx = cx + (y - cy) / 2;
                    for (int x = bx - 3; x <= bx + 3; x++)
                        if (y >= 0 && y < h && x >= 0 && x < w) pixels[y * w + x] = runner;
                }

                // Legs
                for (int i = 0; i < 14; i++)
                {
                    int lx1 = cx + i;
                    int ly1 = cy - 4 - i;
                    int lx2 = cx - i;
                    int ly2 = cy - 4 - (i * 2 / 3);
                    if (ly1 >= 0 && ly1 < h && lx1 >= 0 && lx1 < w) pixels[ly1 * w + lx1] = runner;
                    if (ly2 >= 0 && ly2 < h && lx2 >= 0 && lx2 < w) pixels[ly2 * w + lx2] = runner;
                }
            });
        });
    }

    public static Sprite GetDashButtonSprite()
    {
        return GetOrCreateSprite("FF_DashBtn", () =>
        {
            int size = 128;
            Color border = new Color(0.25f, 0.95f, 0.85f, 0.95f);
            Color fill = new Color(0.04f, 0.12f, 0.10f, 0.70f);

            return CreateCircleSprite(size, size, border, fill, 3.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color bolt = new Color(0.4f, 1f, 0.9f, 0.95f);

                // Lightning speed bolt (⚡)
                for (int y = cy - 18; y <= cy + 18; y++)
                {
                    int x = cx + ((y > cy) ? ((cy + 18 - y) / 2) : ((cy - y) / 2 - 4));
                    for (int w2 = -3; w2 <= 3; w2++)
                    {
                        if (y >= 0 && y < h && (x + w2) >= 0 && (x + w2) < w)
                            pixels[y * w + x + w2] = bolt;
                    }
                }
            });
        });
    }

    public static Sprite GetUltimateButtonSprite(bool unlocked)
    {
        return GetOrCreateSprite("FF_UltimateBtn", () =>
        {
            int size = 128;
            Color border = new Color(0.85f, 0.25f, 1.0f, 0.95f);
            Color fill = new Color(0.14f, 0.05f, 0.18f, 0.70f);

            return CreateCircleSprite(size, size, border, fill, 3.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color aura = new Color(0.95f, 0.5f, 1f, 0.95f);

                // Starburst elemental icon
                for (int angleDeg = 0; angleDeg < 360; angleDeg += 45)
                {
                    float rad = angleDeg * Mathf.Deg2Rad;
                    for (int r = 4; r <= 20; r++)
                    {
                        int x = cx + Mathf.RoundToInt(Mathf.Cos(rad) * r);
                        int y = cy + Mathf.RoundToInt(Mathf.Sin(rad) * r);
                        if (x >= 0 && x < w && y >= 0 && y < h) pixels[y * w + x] = aura;
                    }
                }

                // Core orb
                for (int y = cy - 6; y <= cy + 6; y++)
                    for (int x = cx - 6; x <= cx + 6; x++)
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= 36)
                            pixels[y * w + x] = Color.white;
            });
        });
    }

    public static Sprite GetInteractPillSprite()
    {
        return GetOrCreateSprite("FF_InteractPill", () =>
        {
            int w = 170;
            int h = 56;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color border = new Color(0.20f, 0.95f, 0.45f, 0.95f);
            Color fill = new Color(0.05f, 0.14f, 0.08f, 0.85f);
            Color[] pixels = new Color[w * h];

            float radius = (h - 4) / 2f;
            float leftCx = radius + 2f;
            float rightCx = w - radius - 2f;
            float cy = h / 2f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist;
                    if (x < leftCx)
                    {
                        float dx = x - leftCx;
                        float dy = y - cy;
                        dist = Mathf.Sqrt(dx * dx + dy * dy);
                    }
                    else if (x > rightCx)
                    {
                        float dx = x - rightCx;
                        float dy = y - cy;
                        dist = Mathf.Sqrt(dx * dx + dy * dy);
                    }
                    else
                    {
                        dist = Mathf.Abs(y - cy);
                    }

                    if (dist > radius + 1f)
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                    else if (dist >= radius - 2.5f)
                    {
                        pixels[y * w + x] = border;
                    }
                    else
                    {
                        pixels[y * w + x] = fill;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        });
    }

    public static Sprite GetDropButtonSprite()
    {
        return GetOrCreateSprite("FF_DropBtn", () =>
        {
            int size = 96;
            Color border = new Color(0.95f, 0.35f, 0.25f, 0.95f);
            Color fill = new Color(0.18f, 0.06f, 0.05f, 0.75f);

            return CreateCircleSprite(size, size, border, fill, 3.0f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color drop = new Color(1f, 0.85f, 0.85f, 0.95f);

                // Downward drop arrow
                for (int y = cy - 10; y <= cy + 10; y++)
                {
                    pixels[y * w + cx] = drop;
                    pixels[y * w + cx + 1] = drop;
                }
                for (int i = 0; i <= 8; i++)
                {
                    int py = cy - 10 + i;
                    pixels[py * w + cx - i] = drop;
                    pixels[py * w + cx + i] = drop;
                }
            });
        });
    }

    public static Sprite GetSettingsSprite()
    {
        return GetOrCreateSprite("FF_SettingsBtn", () =>
        {
            int size = 96;
            Color border = new Color(0.75f, 0.85f, 0.90f, 0.95f);
            Color fill = new Color(0.08f, 0.12f, 0.14f, 0.75f);

            return CreateCircleSprite(size, size, border, fill, 2.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color gear = new Color(0.85f, 0.92f, 0.95f, 0.95f);

                // 6-tooth gear
                for (int a = 0; a < 360; a += 60)
                {
                    float rad = a * Mathf.Deg2Rad;
                    for (int r = 10; r <= 18; r++)
                    {
                        int x = cx + Mathf.RoundToInt(Mathf.Cos(rad) * r);
                        int y = cy + Mathf.RoundToInt(Mathf.Sin(rad) * r);
                        pixels[y * w + x] = gear;
                    }
                }
                // Center hole
                for (int y = cy - 5; y <= cy + 5; y++)
                    for (int x = cx - 5; x <= cx + 5; x++)
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= 25)
                            pixels[y * w + x] = fill;
            });
        });
    }

    public static Sprite GetLanguageSprite()
    {
        return GetOrCreateSprite("FF_LanguageBtn", () =>
        {
            int size = 96;
            Color border = new Color(0.35f, 0.85f, 1f, 0.95f);
            Color fill = new Color(0.06f, 0.14f, 0.18f, 0.75f);

            return CreateCircleSprite(size, size, border, fill, 2.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color globe = new Color(0.45f, 0.92f, 1f, 0.95f);

                // Globe meridian lines
                for (int y = cy - 14; y <= cy + 14; y++)
                {
                    pixels[y * w + cx] = globe;
                    int x1 = cx + Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(0, 196 - (y - cy) * (y - cy))) * 0.5f);
                    int x2 = cx - Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(0, 196 - (y - cy) * (y - cy))) * 0.5f);
                    pixels[y * w + x1] = globe;
                    pixels[y * w + x2] = globe;
                }
                for (int x = cx - 14; x <= cx + 14; x++)
                {
                    pixels[cy * w + x] = globe;
                }
            });
        });
    }

    public static Sprite GetShopSprite()
    {
        return GetOrCreateSprite("FF_ShopBtn", () =>
        {
            int size = 96;
            Color border = new Color(1.0f, 0.85f, 0.3f, 0.95f);
            Color fill = new Color(0.16f, 0.14f, 0.05f, 0.75f);

            return CreateCircleSprite(size, size, border, fill, 2.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color bag = new Color(1f, 0.9f, 0.4f, 0.95f);

                // Backpack body
                for (int y = cy - 12; y <= cy + 8; y++)
                    for (int x = cx - 10; x <= cx + 10; x++)
                        if (x == cx - 10 || x == cx + 10 || y == cy - 12 || y == cy + 8)
                            pixels[y * w + x] = bag;

                // Handle on top
                for (int x = cx - 5; x <= cx + 5; x++)
                    pixels[(cy + 13) * w + x] = bag;
            });
        });
    }

    public static Sprite GetGuideSprite()
    {
        return GetOrCreateSprite("FF_GuideBtn", () =>
        {
            int size = 96;
            Color border = new Color(0.4f, 0.95f, 0.6f, 0.95f);
            Color fill = new Color(0.06f, 0.15f, 0.10f, 0.75f);

            return CreateCircleSprite(size, size, border, fill, 2.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color tome = new Color(0.5f, 1f, 0.7f, 0.95f);

                // Open book pages
                for (int y = cy - 10; y <= cy + 10; y++)
                {
                    pixels[y * w + cx] = tome;
                    pixels[y * w + cx - 10] = tome;
                    pixels[y * w + cx + 10] = tome;
                }
                for (int x = cx - 10; x <= cx + 10; x++)
                {
                    pixels[(cy - 10) * w + x] = tome;
                    pixels[(cy + 10) * w + x] = tome;
                }
            });
        });
    }

    public static Sprite GetVideoGuideSprite()
    {
        return GetOrCreateSprite("FF_VideoBtn", () =>
        {
            int size = 96;
            Color border = new Color(0.95f, 0.4f, 0.85f, 0.95f);
            Color fill = new Color(0.15f, 0.05f, 0.12f, 0.75f);

            return CreateCircleSprite(size, size, border, fill, 2.5f, (pixels, w, h) =>
            {
                int cx = w / 2;
                int cy = h / 2;
                Color play = new Color(1f, 0.55f, 0.9f, 0.95f);

                // Play triangle (▶)
                for (int x = cx - 7; x <= cx + 9; x++)
                {
                    int span = (cx + 9 - x);
                    for (int y = cy - span; y <= cy + span; y++)
                    {
                        pixels[y * w + x] = play;
                    }
                }
            });
        });
    }

    public static Sprite GetCircleOverlaySprite()
    {
        return GetOrCreateSprite("FF_CircleOverlay", () =>
        {
            int size = 128;
            return CreateCircleSprite(size, size, Color.white, Color.white, 0f);
        });
    }

    private static Sprite CreateCircleSprite(int w, int h, Color borderColor, Color fillColor, float borderWidth, System.Action<Color[], int, int> drawIcon = null)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[w * h];
        float cx = (w - 1) / 2f;
        float cy = (h - 1) / 2f;
        float radius = Mathf.Min(cx, cy) - 2f;
        float innerRadius = radius - borderWidth;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > radius + 1f)
                {
                    pixels[y * w + x] = Color.clear;
                }
                else if (dist > radius)
                {
                    float alpha = 1f - (dist - radius);
                    pixels[y * w + x] = new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * alpha);
                }
                else if (borderWidth > 0.1f && dist >= innerRadius)
                {
                    pixels[y * w + x] = borderColor;
                }
                else if (borderWidth > 0.1f && dist >= innerRadius - 1f)
                {
                    float t = dist - (innerRadius - 1f);
                    pixels[y * w + x] = Color.Lerp(fillColor, borderColor, t);
                }
                else
                {
                    float grad = (y / (float)h) * 0.15f;
                    pixels[y * w + x] = new Color(fillColor.r + grad, fillColor.g + grad, fillColor.b + grad, fillColor.a);
                }
            }
        }

        drawIcon?.Invoke(pixels, w, h);

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private static Sprite GetOrCreateSprite(string key, System.Func<Sprite> factory)
    {
        if (spriteCache.TryGetValue(key, out Sprite s) && s != null)
        {
            return s;
        }

        Sprite newSprite = factory();
        spriteCache[key] = newSprite;
        return newSprite;
    }
}