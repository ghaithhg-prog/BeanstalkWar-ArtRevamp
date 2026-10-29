using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TreeGrowth : MonoBehaviour
{
    public static TreeGrowth Instance { get; private set; }

    [Header("UI")]
    public Image healthBarFill;

    [Header("Growth Stages")]
    public float stage1Duration = 18.0f; // Level 1 -> Level 2 (~18s)
    public float stage2Duration = 18.0f; // Level 2 -> Level 3 (~18s)
    public float stage3Duration = 18.0f; // Level 3 -> TopLevel (~18s)
    public float level1Height = 5.80f;
    public float level2Height = 15.70f;
    public float level3Height = 25.70f;
    public float topLevelHeight = 40.0f;
    public float maxHeight = 40.0f;

    public float ElapsedGrowthTime { get; private set; } = 0f;
    public float CurrentHeight { get; private set; } = 5.80f;


    [Header("Shake Settings")]
    public float shakeDuration = 2f;
    public float shakeAmount = 0.1f;

    [Header("Health")]
    public float maxHealth = 9500f;
    public float currentHealth;

    [Header("Divine Shield 10s Countdown")]
    public static float ShieldCollapseCountdown { get; private set; } = 10f;
    public static bool IsShieldCollapsing { get; private set; } = false;
    private static bool allBossesDefeatedDetected = false;

    public static void ResetStaticState()
    {
        allBossesDefeatedDetected = false;
        IsShieldCollapsing = false;
        ShieldCollapseCountdown = 10f;
        OnTreeReachedTopLevel = null;
    }

    public static bool IsShieldActive
    {
        get
        {
            if (LevelBossManager.Instance == null || !LevelBossManager.Instance.AreAllBossesDefeated)
                return true;

            if (ShieldCollapseCountdown > 0f)
                return true;

            return false;
        }
    }

    [Header("Critical State")]
    public float criticalDuration = 4f;
    public float rescueHealThreshold = 1000f;

    [Header("Fall Settings")]
    public float fallSpeed = 120f;

    private bool isMature = false;
    private bool isShaking = false;
    private bool isCritical = false;
    private bool criticalUsed = false;
    private bool isDead = false;
    private bool isFalling = false;

    private float shakeTimer = 0f;
    private float criticalTimer = 0f;
    private float healDuringCritical = 0f;

    private Vector3 originalPivotPosition;

    void Awake()
    {
        Instance = this;
        // إجبار القيم دائماً لتجاوز أي قيمة قديمة محفوظة في المشهد
        maxHealth = 9500f;
        rescueHealThreshold = 1200f;
        currentHealth = maxHealth;
        ShieldCollapseCountdown = 10f;
        IsShieldCollapsing = false;
        allBossesDefeatedDetected = false;

        level1Height = 5.80f;
        level2Height = 15.70f;
        level3Height = 25.70f;
        topLevelHeight = 40.0f;
        maxHeight = 40.0f;

        // إضافة NavMeshObstacle لحفر مسار حول جذع الشجرة ومنع أي بوت من الدخول فيه نهائياً
        UnityEngine.AI.NavMeshObstacle obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        }
        obstacle.carving = true;
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
        obstacle.radius = 0.65f;
        obstacle.height = 5.0f;
    }

    void Start()
    {
        maxHealth = 9500f;
        rescueHealThreshold = 1200f;
        currentHealth = maxHealth;
        ShieldCollapseCountdown = 10f;
        IsShieldCollapsing = false;
        allBossesDefeatedDetected = false;

        level1Height = 5.80f;
        level2Height = 15.70f;
        level3Height = 25.70f;
        topLevelHeight = 40.0f;
        maxHeight = 40.0f;

        // ضمان بدء الشجرة صغيرة عند المستوى الأول (y = 5.80m) كما طلب غيث تماماً
        ElapsedGrowthTime = 0f;
        CurrentHeight = level1Height;
        MeshRenderer treeMr = GetComponent<MeshRenderer>();
        if (treeMr != null) treeMr.enabled = false;
        transform.localScale = new Vector3(6f, level1Height * 0.5f, 6f);
        transform.localPosition = new Vector3(0f, level1Height * 0.5f, 0f);
        isMature = false;

        FormatHealthBarUI();

        MajesticTrunkGrowth.Instance?.UpdateGrowth(level1Height);

        ReparentTreeComponentsToPivot();
        Invoke(nameof(ReparentTreeComponentsToPivot), 0.6f);
    }

    void FormatHealthBarUI()
    {
        if (healthBarFill != null)
        {
            RectTransform fillRect = healthBarFill.rectTransform;
            RectTransform parentRect = fillRect.parent as RectTransform;

            if (parentRect != null)
            {
                // إزالة المربع أو الخط الأبيض المشوه الموجود خلف شريط الصحة نهائياً
                Transform grandParent = parentRect.parent;
                if (grandParent != null)
                {
                    Image whiteImg = grandParent.GetComponent<Image>();
                    if (whiteImg != null)
                    {
                        whiteImg.enabled = false;
                        whiteImg.color = Color.clear;
                    }

                    RectTransform grandRect = grandParent as RectTransform;
                    if (grandRect != null)
                    {
                        grandRect.anchorMin = new Vector2(0.15f, 1f);
                        grandRect.anchorMax = new Vector2(0.85f, 1f);
                        grandRect.pivot = new Vector2(0.5f, 1f);
                        grandRect.anchoredPosition = new Vector2(0f, -10f);
                        grandRect.sizeDelta = new Vector2(0f, 40f);
                    }
                }

                // إعطاء خلفية الشريط تصميماً خشبياً داكناً بارتفاع ممتاز 38 وعرض متناسق
                Image parentImg = parentRect.GetComponent<Image>();
                if (parentImg != null)
                {
                    parentImg.color = new Color(0.12f, 0.16f, 0.12f, 0.94f); // خشب غابي داكن مزخرف
                }

                parentRect.anchorMin = new Vector2(0.15f, 1f);
                parentRect.anchorMax = new Vector2(0.85f, 1f);
                parentRect.pivot = new Vector2(0.5f, 1f);
                parentRect.anchoredPosition = new Vector2(0f, -10f);
                parentRect.sizeDelta = new Vector2(0f, 38f);
            }

            fillRect.anchorMin = new Vector2(0.003f, 0.08f);
            fillRect.anchorMax = new Vector2(0.997f, 0.92f);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
        }
    }

    // مضاعف الضرر والعلاج بحسب مستوى الارتفاع الذي يقف عنده اللاعب
    // بناءً على تصميم غيث الدقيق: 15% من الأرض (ضعيف جداً)، 35% في الطابق 1، 60% في الطابق 2، 100% في الطابق 3 والقمة (التأثير الأقوى)
    public static float GetHeightMultiplier(Vector3 attackerPosition)
    {
        float y = attackerPosition.y;

        GameObject treeLevels = GameObject.Find("TreeLevels");
        if (treeLevels != null)
        {
            Transform l3 = treeLevels.transform.Find("Level_3");
            Transform l2 = treeLevels.transform.Find("Level_2");
            Transform l1 = treeLevels.transform.Find("Level_1");

            if (l3 != null && y >= l3.position.y - 1.5f) return 1.00f; // Level 3 / Top: 100% (أقصى قوة كاملة)
            if (l2 != null && y >= l2.position.y - 1.5f) return 0.60f; // Level 2: 60% (أكثر)
            if (l1 != null && y >= l1.position.y - 1.5f) return 0.35f; // Level 1: 35% (أكثر قليلاً)
            return 0.15f; // Ground / Base (تحت المستوى الأول): 15% (تأثير ضعيف جداً)
        }

        if (y >= 22f) return 1.00f;
        if (y >= 14f) return 0.60f;
        if (y >= 4.5f) return 0.35f;
        return 0.15f;
    }

    void Update()
    {
        UpdateHealthBar();

        // إدارة مؤقت العد التنازلي لـ 10 ثوانٍ لصمود درع الشجرة بعد هزيمة الزعماء الثلاثة
        if (LevelBossManager.Instance != null && LevelBossManager.Instance.AreAllBossesDefeated)
        {
            if (!allBossesDefeatedDetected)
            {
                allBossesDefeatedDetected = true;
                IsShieldCollapsing = true;
                ShieldCollapseCountdown = 10f;
            }

            if (ShieldCollapseCountdown > 0f)
            {
                ShieldCollapseCountdown -= Time.deltaTime;
                if (ShieldCollapseCountdown <= 0f)
                {
                    ShieldCollapseCountdown = 0f;
                    IsShieldCollapsing = false;
                    Debug.Log("💥 [TreeGrowth] The 10-second Divine Shield countdown has ended! Tree is now vulnerable!");
                    ThirdPersonCamera.Shake(0.8f, 0.45f);
                }
            }
        }

        bool isMatchPlaying = GameStateManager.Instance == null ||
                              GameStateManager.Instance.currentState == GameStateManager.GameState.Playing;

        if (!isDead)
        {
            if (isMatchPlaying)
            {
                GrowTree();
            }
            CheckDeath();
        }

        if (isCritical)
        {
            HandleCriticalState();
        }

        if (isShaking)
        {
            ShakeTree();
        }

        if (isFalling)
        {
            HandleFall();
        }
    }

    // =========================
    // Growth
    // =========================

    void GrowTree()
    {
        if (!isMature)
        {
            ElapsedGrowthTime += Time.deltaTime;

            float targetHeight;
            if (ElapsedGrowthTime <= stage1Duration)
            {
                float t = Mathf.Clamp01(ElapsedGrowthTime / stage1Duration);
                targetHeight = Mathf.Lerp(level1Height, level2Height, t);
            }
            else if (ElapsedGrowthTime <= stage1Duration + stage2Duration)
            {
                float t = Mathf.Clamp01((ElapsedGrowthTime - stage1Duration) / stage2Duration);
                targetHeight = Mathf.Lerp(level2Height, level3Height, t);
            }
            else if (ElapsedGrowthTime <= stage1Duration + stage2Duration + stage3Duration)
            {
                float t = Mathf.Clamp01((ElapsedGrowthTime - stage1Duration - stage2Duration) / stage3Duration);
                targetHeight = Mathf.Lerp(level3Height, topLevelHeight, t);
            }
            else
            {
                targetHeight = topLevelHeight;
                isMature = true;
                StartShake(); // اهتزاز عند بلوغ القمة ونضج الشجرة بالكامل والتصاقها بالمنصة
                OnTreeReachedTopLevel?.Invoke();
            }

            CurrentHeight = targetHeight;

            MeshRenderer treeMr = GetComponent<MeshRenderer>();
            if (treeMr != null && treeMr.enabled) treeMr.enabled = false;

            transform.localScale = new Vector3(6f, CurrentHeight * 0.5f, 6f);
            transform.localPosition = new Vector3(0f, CurrentHeight * 0.5f, 0f);

            MajesticTrunkGrowth.Instance?.UpdateGrowth(CurrentHeight);
        }
    }

    /// <summary>
    /// يطلق نمواً انفجارياً هائلاً للشجرة عند تسليم الكريستالة بنجاح احتفالاً بنصر حماة الغابة!
    /// </summary>
    public void TriggerCelebratoryExplosiveGrowth(float boostHeight = 16f)
    {
        StartCoroutine(CelebratoryGrowthRoutine(boostHeight));
    }

    System.Collections.IEnumerator CelebratoryGrowthRoutine(float boostHeight)
    {
        float targetHeight = transform.localScale.y + boostHeight;
        float elapsed = 0f;
        float duration = 3.5f;
        Vector3 origScale = transform.localScale;
        StartShake();

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            float newY = Mathf.Lerp(origScale.y, targetHeight, smooth);
            float diff = newY - transform.localScale.y;

            transform.localScale = new Vector3(transform.localScale.x, newY, transform.localScale.z);
            transform.position += new Vector3(0, diff / 2f, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isMature = true;
    }

    // =========================
    // Critical System
    // =========================

    void CheckDeath()
    {
        // درع الزعماء الثلاثة والعد التنازلي لـ 10 ثوانٍ: لا يمكن إماتة الشجرة طالما الدرع فعال
        if (IsShieldActive) return;

        if (currentHealth <= 0 && !isDead)
        {
            // إذا كنا داخل الحالة الحرجة لا نفعل شيئًا
            if (isCritical)
                return;

            if (!criticalUsed)
            {
                EnterCriticalState();
            }
            else
            {
                FinalDeath();
            }
        }
    }

    void EnterCriticalState()
    {
        if (IsShieldActive) return;

        isCritical = true;
        criticalUsed = true;
        criticalTimer = criticalDuration;
        healDuringCritical = 0f;
        currentHealth = 0f;

        StartShake(); // اهتزاز أثناء الحالة الحرجة

        Debug.Log("Tree entered Critical State!");
    }

    void HandleCriticalState()
    {
        if (criticalTimer > 0f)
        {
            criticalTimer -= Time.deltaTime;
        }
        else
        {
            isCritical = false;
            FinalDeath();
        }
    }

    public static event System.Action OnTreeReachedTopLevel;
    public bool IsMature => isMature;

    void ExitCriticalState()
    {
        isCritical = false;
        currentHealth = maxHealth * 0.35f; // ✅ تعود بـ 35%

        Debug.Log("Tree rescued from Critical State!");
    }

    // =========================
    // Shake
    // =========================

    void StartShake()
    {
        isShaking = true;
        shakeTimer = shakeDuration;
        originalPivotPosition = transform.parent.position;
    }

    void ShakeTree()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;

            float offsetX = Random.Range(-shakeAmount, shakeAmount);
            float offsetZ = Random.Range(-shakeAmount, shakeAmount);

            transform.parent.position =
                originalPivotPosition + new Vector3(offsetX, 0f, offsetZ);
        }
        else
        {
            isShaking = false;
            transform.parent.position = originalPivotPosition;

            if (isMature && !isCritical)
            {
                bool allBossesSlain = LevelBossManager.Instance == null || LevelBossManager.Instance.AreAllBossesDefeated;
                if (allBossesSlain)
                {
                    Debug.Log("Tree reached Top Level & All 3 Bosses Slain! Crystal Ready!");
                    OnTreeReachedTopLevel?.Invoke();
                }
                else
                {
                    Debug.Log("Tree reached Top Level, awaiting defeat of all 3 Bosses to unseal Crystal!");
                }
            }
        }
    }

    public void ApplyBossKillGrowthBoost(float extraHeight)
    {
        if (isDead || isMature) return;
        float newY = Mathf.Min(maxHeight, transform.localScale.y + extraHeight);
        float diff = newY - transform.localScale.y;
        if (diff > 0.01f)
        {
            transform.localScale = new Vector3(transform.localScale.x, newY, transform.localScale.z);
            transform.position += new Vector3(0f, diff / 2f, 0f);
        }
        if (newY >= maxHeight)
        {
            isMature = true;
            if (LevelBossManager.Instance == null || LevelBossManager.Instance.AreAllBossesDefeated)
            {
                OnTreeReachedTopLevel?.Invoke();
            }
        }
    }

    // =========================
    // Fall & Cinematic Collapse
    // =========================

    private bool isCollapsingBannerActive = false;

    public void ReparentTreeComponentsToPivot()
    {
        Transform pivot = transform.parent; // TreePivot
        if (pivot == null) return;

        GameObject trunk = GameObject.Find("MajesticBraidedBeanstalkTrunk");
        if (trunk != null && trunk.transform.parent != pivot)
        {
            trunk.transform.SetParent(pivot, true);
        }

        GameObject canopies = GameObject.Find("TreeCanopyLevelDecorations");
        if (canopies != null && canopies.transform.parent != pivot)
        {
            canopies.transform.SetParent(pivot, true);
        }
    }

    void FinalDeath()
    {
        // حماية قاطعة: لا تسقط الشجرة أبداً إلا بعد موت الزعماء الثلاثة
        if (LevelBossManager.Instance != null && !LevelBossManager.Instance.AreAllBossesDefeated)
        {
            currentHealth = maxHealth * 0.35f;
            isCritical = false;
            return;
        }

        if (isDead) return;
        isDead = true;
        isFalling = true;

        Debug.Log("🌲💥 Tree permanently destroyed! Initiating cinematic collapse sequence...");
        StartCoroutine(CinematicTreeCollapseRoutine());
    }

    void HandleFall()
    {
        // يتم التحكم بالسقوط سينمائياً وبدقة عبر CinematicTreeCollapseRoutine
    }

    System.Collections.IEnumerator CinematicTreeCollapseRoutine()
    {
        isCollapsingBannerActive = true;

        // 1. ضمان التحام وتوحيد كافة أجزاء الشجرة تحت TreePivot
        ReparentTreeComponentsToPivot();

        Transform pivot = transform.parent;
        if (pivot == null)
        {
            isCollapsingBannerActive = false;
            GameStateManager.Instance?.EndGame("Destroyers");
            yield break;
        }

        // المرحلة الأولى: هزة عنيفة وتشقق الجذع واهتزاز الشاشة وصوت التصدع (1.2 ثانية)
        ThirdPersonCamera.Shake(1.4f, 0.45f);
        SpawnCollapseDebris(transform.position + Vector3.up * 2.5f, 16);

        float groanTime = 1.2f;
        float elapsedGroan = 0f;
        Quaternion startRot = pivot.rotation;
        Vector3 fallAxis = new Vector3(0.35f, 0f, 1f).normalized;

        while (elapsedGroan < groanTime)
        {
            elapsedGroan += Time.deltaTime;
            float groanAngle = Mathf.Lerp(0f, 4f, elapsedGroan / groanTime);
            pivot.rotation = startRot * Quaternion.AngleAxis(groanAngle, fallAxis);
            yield return null;
        }

        // المرحلة الثانية: سقوط جاذبي سينمائي متسارع مهيب (2.6 ثانية)
        float fallDuration = 2.6f;
        float elapsedFall = 0f;
        ThirdPersonCamera.Shake(fallDuration + 1f, 0.35f);

        while (elapsedFall < fallDuration)
        {
            elapsedFall += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedFall / fallDuration);
            float curve = Mathf.Pow(t, 2.3f);
            float angle = Mathf.Lerp(4f, 90f, curve);

            pivot.rotation = startRot * Quaternion.AngleAxis(angle, fallAxis);

            if (Random.value < 0.35f)
            {
                Vector3 midPoint = pivot.position + Vector3.up * 18f;
                SpawnFallLeavesDust(midPoint, 3);
            }

            yield return null;
        }

        pivot.rotation = startRot * Quaternion.AngleAxis(90f, fallAxis);

        // المرحلة الثالثة: الارتطام الكارثي بالأرض!
        ThirdPersonCamera.Shake(1.8f, 0.85f);
        SpawnGroundImpactShockwave(pivot.position, 90f);
        SpawnCollapseDebris(pivot.position + fallAxis * 20f, 25);

        // وقفة سينمائية مهيبة: الاحتفال الكارثي بسقوط الشجرة، أمطار النيازك والنيران المشتعلة (5.2 ثوانٍ)
        yield return StartCoroutine(ApocalypticDestructionCelebrationRoutine(pivot.position, fallAxis));

        isCollapsingBannerActive = false;

        // المرحلة الرابعة: إنهاء المعركة وإعلان فوز المدمرين باللغة المختارة وظهور شاشة النقاط
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EndGame("Destroyers");
        }
        else
        {
            MatchEndStatsUI.Instance?.ShowVictoryScreen("Destroyers");
        }
    }

    System.Collections.IEnumerator ApocalypticDestructionCelebrationRoutine(Vector3 treePos, Vector3 fallAxis)
    {
        // 1. Giant Void Light engulfing entire arena in apocalyptic red-orange
        GameObject voidLightObj = new GameObject("ApocalypticVoidLight");
        Light voidLight = voidLightObj.AddComponent<Light>();
        voidLight.type = LightType.Point;
        voidLight.color = new Color(1.0f, 0.25f, 0.06f);
        voidLight.intensity = 18.0f;
        voidLight.range = 140f;
        voidLightObj.transform.position = treePos + Vector3.up * 14f;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                          ?? Shader.Find("Unlit/Transparent") 
                          ?? Shader.Find("Standard");

        // 2. Chaotic Fire clusters along fallen trunk
        GameObject flameGroup = new GameObject("ApocalypticChaosFlames");
        flameGroup.transform.position = treePos;
        List<Transform> flameEmberList = new List<Transform>();

        Material flameMat = new Material(unlitShader);
        flameMat.color = new Color(1f, 0.35f, 0.05f, 0.9f);

        for (int i = 0; i < 24; i++)
        {
            GameObject fObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider c = fObj.GetComponent<Collider>();
            if (c != null) Destroy(c);
            fObj.transform.SetParent(flameGroup.transform);
            Vector3 flamePos = treePos + fallAxis * (i * 2.2f) + new Vector3(Random.Range(-2.8f, 2.8f), Random.Range(0.5f, 4.0f), Random.Range(-2.8f, 2.8f));
            fObj.transform.position = flamePos;
            fObj.transform.localScale = Vector3.one * Random.Range(0.8f, 1.8f);
            fObj.GetComponent<Renderer>().material = flameMat;
            flameEmberList.Add(fObj.transform);
        }

        float celebrationDuration = 5.5f;
        float elapsed = 0f;
        float nextMeteorTime = 0.05f;

        while (elapsed < celebrationDuration)
        {
            elapsed += Time.deltaTime;

            if (voidLight != null)
            {
                voidLight.intensity = 15f + Mathf.Sin(elapsed * 7f) * 6f;
                voidLight.color = Color.Lerp(new Color(1f, 0.22f, 0.04f), new Color(0.9f, 0.08f, 0.35f), Mathf.PingPong(elapsed * 2.2f, 1f));
            }

            for (int i = 0; i < flameEmberList.Count; i++)
            {
                if (flameEmberList[i] != null)
                {
                    float s = 1.0f + Mathf.Sin(elapsed * 12f + i) * 0.5f;
                    flameEmberList[i].localScale = new Vector3(s, s * 1.6f, s);
                }
            }

            // أمطار نيازك ضخمة وساطعة متلاحقة مباشرة في واجهة الكاميرا وأرجاء الساحة
            if (elapsed >= nextMeteorTime)
            {
                nextMeteorTime = elapsed + Random.Range(0.14f, 0.26f);

                Vector3 targetLand;
                if (Camera.main != null && Random.value < 0.65f)
                {
                    Vector3 camFwd = Camera.main.transform.forward;
                    camFwd.y = 0f;
                    if (camFwd.sqrMagnitude < 0.01f) camFwd = Vector3.forward; else camFwd.Normalize();
                    Vector3 camViewTarget = Camera.main.transform.position + camFwd * Random.Range(12f, 28f);
                    targetLand = camViewTarget + new Vector3(Random.Range(-12f, 12f), 0f, Random.Range(-12f, 12f));
                }
                else
                {
                    targetLand = treePos + fallAxis * Random.Range(0f, 40f) + new Vector3(Random.Range(-18f, 18f), 0f, Random.Range(-18f, 18f));
                }

                Vector3 meteorSpawn = targetLand + new Vector3(Random.Range(-24f, -8f), Random.Range(55f, 75f), Random.Range(-24f, -8f));
                StartCoroutine(SingleMeteorRoutine(meteorSpawn, targetLand));
            }

            yield return null;
        }

        if (voidLightObj != null) Destroy(voidLightObj);
        if (flameGroup != null) Destroy(flameGroup);
    }

    System.Collections.IEnumerator SingleMeteorRoutine(Vector3 from, Vector3 to)
    {
        // 1. جسم النيزك الضخم المتوهج بنيران أسطورية ساطعة
        GameObject mObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mObj.name = "CataclysmMeteorCore";
        Collider c = mObj.GetComponent<Collider>();
        if (c != null) Destroy(c);

        float meteorScale = Random.Range(3.4f, 5.0f);
        mObj.transform.position = from;
        mObj.transform.localScale = Vector3.one * meteorScale;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") 
                        ?? Shader.Find("Standard") 
                        ?? Shader.Find("Universal Render Pipeline/Unlit");

        Material meteorMat = new Material(litShader);
        meteorMat.color = new Color(1.0f, 0.40f, 0.05f, 1f);
        if (meteorMat.HasProperty("_EmissionColor"))
        {
            meteorMat.EnableKeyword("_EMISSION");
            meteorMat.SetColor("_EmissionColor", new Color(3.5f, 1.2f, 0.2f));
        }
        mObj.GetComponent<Renderer>().material = meteorMat;

        // ضوء ناري ديناميكي ساطع يتحرك مع كل نيزك
        Light mL = mObj.AddComponent<Light>();
        mL.type = LightType.Point;
        mL.color = new Color(1.0f, 0.55f, 0.15f);
        mL.intensity = 18f;
        mL.range = 45f;

        // 2. ذيل ناري ملتهب من 4 كرات لهب متتابعة خلف النيزك
        List<Transform> tailPieces = new List<Transform>();
        int tailCount = 4;
        for (int i = 0; i < tailCount; i++)
        {
            GameObject tObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider tc = tObj.GetComponent<Collider>();
            if (tc != null) Destroy(tc);
            float tailS = meteorScale * (1f - (i + 1) * 0.18f);
            tObj.transform.localScale = Vector3.one * tailS;
            Material tMat = new Material(meteorMat);
            tMat.color = new Color(1f, 0.28f + i * 0.15f, 0.04f, 0.85f);
            if (tMat.HasProperty("_EmissionColor"))
            {
                tMat.SetColor("_EmissionColor", new Color(2.8f, 0.8f, 0.1f) * (1f - i * 0.2f));
            }
            tObj.GetComponent<Renderer>().material = tMat;
            tailPieces.Add(tObj.transform);
        }

        Vector3 moveDir = (to - from).normalized;
        float flyTime = Random.Range(0.48f, 0.72f);
        float t = 0f;

        while (t < flyTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / flyTime);
            Vector3 curPos = Vector3.Lerp(from, to, progress);
            mObj.transform.position = curPos;

            for (int i = 0; i < tailPieces.Count; i++)
            {
                if (tailPieces[i] != null)
                {
                    tailPieces[i].position = curPos - moveDir * ((i + 1) * (meteorScale * 0.65f));
                }
            }
            yield return null;
        }

        Destroy(mObj);
        for (int i = 0; i < tailPieces.Count; i++)
        {
            if (tailPieces[i] != null) Destroy(tailPieces[i].gameObject);
        }

        // 3. انفجار كارثي وارتطام عنيف بالأرض
        StartCoroutine(MeteorImpactExplosionRoutine(to, meteorScale));
    }

    System.Collections.IEnumerator MeteorImpactExplosionRoutine(Vector3 impactPos, float radius)
    {
        ThirdPersonCamera.Shake(0.70f, 0.55f);
        SpawnCollapseDebris(impactPos, 14);

        // أ. كرة نار متفجرة وموجة صدمة ساطعة تتسع بسرعة
        GameObject boomSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boomSphere.name = "MeteorFireballExplosion";
        Collider bc = boomSphere.GetComponent<Collider>();
        if (bc != null) Destroy(bc);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Unlit");
        Material boomMat = new Material(litShader);
        boomMat.color = new Color(1.0f, 0.45f, 0.08f, 0.85f);
        if (boomMat.HasProperty("_EmissionColor"))
        {
            boomMat.EnableKeyword("_EMISSION");
            boomMat.SetColor("_EmissionColor", new Color(4.0f, 1.8f, 0.3f));
        }
        boomSphere.GetComponent<Renderer>().material = boomMat;
        boomSphere.transform.position = impactPos + Vector3.up * 1.5f;

        // ب. وميض ضوئي هائل للانفجار
        GameObject flashObj = new GameObject("MeteorImpactFlash");
        flashObj.transform.position = impactPos + Vector3.up * 2.5f;
        Light flashLight = flashObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1.0f, 0.65f, 0.2f);
        flashLight.intensity = 28f;
        flashLight.range = 55f;

        // ج. فوهة نيران مشتعلة على الأرض
        GameObject craterFire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        craterFire.name = "MeteorCraterFire";
        Collider cc = craterFire.GetComponent<Collider>();
        if (cc != null) Destroy(cc);
        craterFire.transform.position = impactPos + Vector3.up * 0.15f;
        craterFire.transform.localScale = new Vector3(radius * 1.8f, 0.15f, radius * 1.8f);
        Material craterMat = new Material(boomMat);
        craterMat.color = new Color(1.0f, 0.25f, 0.05f, 0.9f);
        if (craterMat.HasProperty("_EmissionColor"))
        {
            craterMat.SetColor("_EmissionColor", new Color(2.5f, 0.6f, 0.05f));
        }
        craterFire.GetComponent<Renderer>().material = craterMat;

        float boomDuration = 0.38f;
        float elapsed = 0f;
        while (elapsed < boomDuration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / boomDuration;
            float curScale = Mathf.Lerp(radius * 0.8f, radius * 3.5f, p);
            boomSphere.transform.localScale = Vector3.one * curScale;
            flashLight.intensity = Mathf.Lerp(28f, 0f, p);
            yield return null;
        }

        Destroy(boomSphere);
        Destroy(flashObj);

        // بقاء النيران مشتعلة وتلتهب على الأرض لمدة 2.6 ثانية
        float fireDuration = 2.6f;
        float fireElapsed = 0f;
        while (fireElapsed < fireDuration)
        {
            fireElapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(fireElapsed * 14f) * 0.2f;
            if (craterFire != null)
            {
                craterFire.transform.localScale = new Vector3(radius * 1.8f * pulse, 0.15f, radius * 1.8f * pulse);
            }
            yield return null;
        }

        if (craterFire != null) Destroy(craterFire);
    }

    void SpawnGroundImpactShockwave(Vector3 center, float treeHeight)
    {
        StartCoroutine(ShockwaveRingRoutine(center));
    }

    System.Collections.IEnumerator ShockwaveRingRoutine(Vector3 center)
    {
        GameObject shockObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shockObj.name = "TreeImpactShockwave";
        Collider c = shockObj.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Renderer rend = shockObj.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material shockMat = new Material(s);
        shockMat.color = new Color(0.85f, 0.75f, 0.55f, 0.65f);
        if (shockMat.HasProperty("_EmissionColor"))
        {
            shockMat.EnableKeyword("_EMISSION");
            shockMat.SetColor("_EmissionColor", new Color(0.9f, 0.8f, 0.5f) * 0.6f);
        }
        rend.material = shockMat;

        float duration = 1.4f;
        float elapsed = 0f;
        shockObj.transform.position = new Vector3(center.x, 0.2f, center.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float radius = Mathf.Lerp(4f, 32f, Mathf.Sqrt(t));
            shockObj.transform.localScale = new Vector3(radius * 2f, 0.15f, radius * 2f);

            Color col = shockMat.color;
            col.a = Mathf.Lerp(0.7f, 0f, t);
            shockMat.color = col;
            yield return null;
        }

        Destroy(shockObj);
    }

    void SpawnCollapseDebris(Vector3 origin, int count)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material debMat = new Material(s);
        debMat.color = new Color(0.42f, 0.28f, 0.14f);

        for (int i = 0; i < count; i++)
        {
            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.name = "TreeDebrisChunk";
            Collider c = chip.GetComponent<Collider>();
            if (c != null) Destroy(c);

            chip.GetComponent<Renderer>().material = debMat;
            chip.transform.position = origin + Random.insideUnitSphere * 2.5f;
            chip.transform.localScale = new Vector3(Random.Range(0.4f, 1.2f), Random.Range(0.4f, 1.2f), Random.Range(0.4f, 1.2f));

            Rigidbody rb = chip.AddComponent<Rigidbody>();
            rb.mass = 1.5f;
            Vector3 forceDir = (Random.onUnitSphere + Vector3.up * 1.2f).normalized;
            rb.AddForce(forceDir * Random.Range(6f, 16f), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 12f, ForceMode.Impulse);

            Destroy(chip, Random.Range(4f, 7f));
        }
    }

    void SpawnFallLeavesDust(Vector3 origin, int count)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material leafMat = new Material(s);
        leafMat.color = new Color(0.25f, 0.75f, 0.35f, 0.8f);

        for (int i = 0; i < count; i++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
            leaf.name = "TreeFallingLeaf";
            Collider c = leaf.GetComponent<Collider>();
            if (c != null) Destroy(c);

            leaf.GetComponent<Renderer>().material = leafMat;
            leaf.transform.position = origin + Random.insideUnitSphere * 6f;
            leaf.transform.localScale = Vector3.one * Random.Range(0.6f, 1.4f);

            Rigidbody rb = leaf.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.linearDamping = 1.5f;
            rb.AddForce(Vector3.down * Random.Range(2f, 5f) + Random.insideUnitSphere * 2f, ForceMode.Impulse);

            Destroy(leaf, Random.Range(3f, 5f));
        }
    }



    // =========================
    // Health
    // =========================

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (isDead) return;

        bool shieldActive = IsShieldActive;
        float minAllowedHealth = shieldActive ? (maxHealth * 0.35f) : 0f;
        bool isAlreadyAtShieldFloor = shieldActive && (currentHealth <= minAllowedHealth + 0.05f);

        if (attacker != null)
        {
            MatchEndStatsUI.Instance?.RegisterDamage(attacker, amount);

            // منح خبرة ضرب الشجرة للمدمر فقط إذا كانت الشجرة تقبل الضرر ولم تبلغ حد الدرع الحصين
            if (!isAlreadyAtShieldFloor && currentHealth > minAllowedHealth)
            {
                PlayerXP pxp = attacker.GetComponent<PlayerXP>();
                if (pxp != null) pxp.AddTreeHitXP();
            }
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, minAllowedHealth, maxHealth);
    }

    public void Heal(float amount, GameObject healer = null)
    {
        if (isDead) return;

        bool wasNotFull = currentHealth < maxHealth;

        if (isCritical)
        {
            healDuringCritical += amount;

            if (healDuringCritical >= rescueHealThreshold)
            {
                ExitCriticalState();
            }

            if (healer != null)
            {
                PlayerXP pxp = healer.GetComponent<PlayerXP>();
                if (pxp != null) pxp.AddTreeHealXP();
            }
            return;
        }

        // منح خبرة معالجة الشجرة للحامي فقط إذا كانت صحة الشجرة ناقصة فعلياً
        if (wasNotFull && healer != null)
        {
            PlayerXP pxp = healer.GetComponent<PlayerXP>();
            if (pxp != null) pxp.AddTreeHealXP();
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = currentHealth / maxHealth;
            healthBarFill.fillAmount = healthPercent;
            bool shieldActive = IsShieldActive;
            if (shieldActive && healthPercent <= 0.355f)
            {
                // لون ذهبي متوهج يدل على أن درع الزعماء الثلاثة يحمي الشجرة من السقوط (صامدة عند 35%)
                healthBarFill.color = new Color(1f, 0.82f, 0.2f);
            }
            else
            {
                healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
            }
        }
    }

    // =========================
    // واجهة شريط صحة الشجرة وحالة الدرع بكل اللغات
    // =========================
    void OnGUI()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()) return;

        if (isCollapsingBannerActive)
        {
            float bannerW = Mathf.Min(Screen.width * 0.88f, 700f);
            float bannerH = 60f;
            float bx = (Screen.width - bannerW) / 2f;
            float by = 60f;

            Rect bannerRect = new Rect(bx, by, bannerW, bannerH);
            EnchantedNatureGUI.DrawWoodPlate(bannerRect, new Color(0.35f, 0.08f, 0.05f, 0.95f), new Color(1f, 0.45f, 0.15f, 0.95f));

            GUIStyle collapseStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            collapseStyle.normal.textColor = new Color(1f, 0.88f, 0.2f);

            string collapseText = LanguageSelectionUI.Localize(
                "🔥 THE SACRED TREE IS COLLAPSING! 🔥",
                ar: "🔥 الشجرة المقدسة تنهار وتسقط الآن! 🔥",
                fa: "🔥 درخت مقدس در حال سرنگونی است! 🔥",
                de: "🔥 DER HEILIGE BAUM STÜRZT EIN! 🔥",
                it: "🔥 IL SACRO ALBERO STA CROLLANDO! 🔥",
                tr: "🔥 KUTSAL AĞAÇ ÇÖKÜYOR! 🔥",
                ru: "🔥 СВЯЩЕННОЕ ДЕРЕВО ПАДАЕТ! 🔥",
                fr: "🔥 L'ARBRE SACRÉ S'EFFONDRE ! 🔥",
                es: "🔥 ¡EL ÁRBOL SAGRADO SE DERRUMBA! 🔥",
                hi: "🔥 पवित्र वृक्ष गिर रहा है! 🔥",
                zh: "🔥 神圣巨树正在倾覆倒塌！🔥",
                ja: "🔥 聖なる巨木が倒壊している！🔥",
                pt: "🔥 A ÁRVORE SAGRADA ESTÁ DESABANDO! 🔥"
            );

            GUI.Label(bannerRect, collapseText, collapseStyle);
        }

        float sw = Screen.width;
        float barW = Mathf.Clamp(sw * 0.45f, 320f, 540f);
        float barH = 34f;
        float barX = (sw - barW) * 0.5f;
        float barY = 10f;

        // 1. عنوان صحة الشجرة بكل اللغات المدعومة
        string treeTitle = LanguageSelectionUI.Localize(
            "🌳 GREAT BEANSTALK HEALTH",
            ar: "🌳 صحة شجرة الفاصولياء العظيمة",
            fa: "🌳 سلامت ساقه لوبیای مقدس",
            de: "🌳 GESUNDHEIT DER RIESENBOHNE",
            it: "🌳 SALUTE DEL SACRO FAGIOLO",
            tr: "🌳 BÜYÜK FASULYE SIRIĞI SAĞLIĞI",
            ru: "🌳 ЗДОРОВЬЕ ВЕЛИКОГО СТЕБЛЯ",
            fr: "🌳 SANTÉ DU GRAND HARICOT SACRÉ",
            es: "🌳 SALUD DEL GRAN TALLO SAGRADO",
            hi: "🌳 महा बीनस्टॉक स्वास्थ्य",
            zh: "🌳 巨藤神树生命值",
            ja: "🌳 巨木の聖なる生命力",
            pt: "🌳 SAÚDE DO PÉ DE FEIJÃO"
        );

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = 11;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.85f, 1f, 0.85f);

        GUI.Label(new Rect(barX, barY - 14f, barW, 16f), treeTitle, titleStyle);

        // 2. حالة الدرع الإلهي ومؤقت الـ 10 ثوانٍ
        string shieldStatus = "";
        Color shieldColor = Color.white;

        if (LevelBossManager.Instance == null || !LevelBossManager.Instance.AreAllBossesDefeated)
        {
            int deadCount = LevelBossManager.Instance != null ? LevelBossManager.Instance.DefeatedBossCount : 0;
            if (deadCount < 3 && LevelBossManager.Instance != null)
            {
                LevelBossManager.Instance.CheckBossesDefeatStatus();
                deadCount = LevelBossManager.Instance.DefeatedBossCount;
            }

            bool isShieldCapped = currentHealth <= (maxHealth * 0.355f);
            if (isShieldCapped)
            {
                shieldStatus = LanguageSelectionUI.Localize(
                    $"🛡️ SHIELD LOCKED (35% HP)! Tree is immune to further damage until all 3 Bosses are slain ({deadCount}/3 defeated)!",
                    ar: $"🛡️ الدرع مفعل الآن (35% صحة)! لا يمكن إلحاق ضرر إضافي بالشجرة إلا بعد القضاء على الزعماء الثلاثة ({deadCount}/3 قتلى)!",
                    fa: $"🛡️ سپر هم‌اکنون فعال است (۳۵٪ سلامت)! درخت آسیب نمی‌بیند تا زمانی که هر ۳ غول نابود شوند ({deadCount}/۳ کشته شدند)!",
                    de: $"🛡️ SCHILD JETZT AKTIV (35% TP)! Baum immun gegen weiteren Schaden, bis alle 3 Bosse besiegt sind ({deadCount}/3)!",
                    it: $"🛡️ SCUDO ORA ATTIVO (35% PS)! L'albero non può subire altri danni finché tutti e 3 i boss non saranno sconfitti ({deadCount}/3)!",
                    tr: $"🛡️ KALKAN ŞİMDİ AKTİF (%35 CAN)! 3 Patron yenilene kadar ağaç daha fazla vurulamaz ({deadCount}/3)!",
                    ru: $"🛡️ ЩИТ АКТИВИРОВАН (35% HP)! Урон дереву заблокирован до уничтожения всех 3 боссов ({deadCount}/3)!",
                    fr: $"🛡️ BOUCLIER ACTIF (35% PV) ! Aucun dégât supplémentaire possible tant que les 3 boss ne sont pas vaincus ({deadCount}/3) !",
                    es: $"🛡️ ¡ESCUDO ACTIVO (35% PS)! ¡No se puede dañar más al árbol hasta derrotar a los 3 jefes ({deadCount}/3)!",
                    hi: $"🛡️ ढाल अब सक्रिय है (35% स्वास्थ्य)! जब तक सभी 3 बॉस पराजित नहीं होते, वृक्ष को नुकसान नहीं पहुँचेगा ({deadCount}/3)!",
                    zh: $"🛡️ 护盾现已锁定 (35% 生命)！在击败全部 3 位领主前，无法对神树造成更多伤害 ({deadCount}/3)！",
                    ja: $"🛡️ シールド現在発動中 (35% HP)！3体のボスを撃破するまで神木へのダメージはこれ以上入りません ({deadCount}/3)！",
                    pt: $"🛡️ ESCUDO ATIVO AGORA (35% VIDA)! Nenhum dano adicional é permitido até derrotar os 3 chefes ({deadCount}/3)!"
                );
                float flash = Mathf.PingPong(Time.time * 4f, 1f);
                shieldColor = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 1f, 0.65f), flash);
            }
            else
            {
                shieldStatus = LanguageSelectionUI.Localize(
                    $"🛡️ DIVINE SHIELD ACTIVE ({deadCount}/3 BOSS SEALS BROKEN) - Tree protected below 35% HP",
                    ar: $"🛡️ الدرع الإلهي مفعل ({deadCount}/3 أختام مكسورة) - الشجرة محمية عند 35% من الصحة",
                    fa: $"🛡️ سپر مقدس فعال است ({deadCount}/۳ مهر شکسته شد) - محافظت در زیر ۳۵٪",
                    de: $"🛡️ GÖTTLICHER SCHILD AKTIV ({deadCount}/3 BOSSSIEGEL GEBROCHEN) - Geschützt unter 35%",
                    it: $"🛡️ SCUDO DIVINO ATTIVO ({deadCount}/3 SIGILLI DEI BOSS SPEZZATI) - Protetto sotto il 35%",
                    tr: $"🛡️ İLAHİ KALKAN AKTİF ({deadCount}/3 PATRON MÜHRÜ KIRILDI) - %35 altında korumalı",
                    ru: $"🛡️ БОЖЕСТВЕННЫЙ ЩИТ АКТИВЕН ({deadCount}/3 СНЯТО) - Защита ниже 35%",
                    fr: $"🛡️ BOUCLIER SACRÉ ACTIF ({deadCount}/3 SCEAUX BRISÉS) - Protégé sous 35%",
                    es: $"🛡️ ESCUDO SAGRADO ACTIVO ({deadCount}/3 SELLOS ROTOS) - Protegido bajo 35%",
                    hi: $"🛡️ दिव्य ढाल सक्रिय ({deadCount}/3 बॉस सील टूट गईं) - 35% के नीचे सुरक्षित",
                    zh: $"🛡️ 神圣护盾生效中 ({deadCount}/3 领主封印已破) - 35% 生命锁死保护",
                    ja: $"🛡️ 神聖シールド展開中 (封印解除: {deadCount}/3) - HP 35%未満保護",
                    pt: $"🛡️ ESCUDO DIVINO ATIVO ({deadCount}/3 SELOS DE CHEFES QUEBRADOS) - Protegido abaixo de 35%"
                );
                shieldColor = new Color(1f, 0.85f, 0.25f);
            }
        }
        else if (ShieldCollapseCountdown > 0f)
        {
            int sec = Mathf.CeilToInt(ShieldCollapseCountdown);
            float ping = Mathf.PingPong(Time.time * 5f, 1f);
            shieldColor = Color.Lerp(Color.yellow, new Color(1f, 0.3f, 0.1f), ping);
            shieldStatus = LanguageSelectionUI.Localize(
                $"⚠️ SACRED SHIELD COLLAPSING IN: {sec}s!",
                ar: $"⚠️ الدرع الإلهي ينهار خلال: {sec} ثوانٍ!",
                fa: $"⚠️ سپر مقدس در حال فروپاشی: {sec} ثانیه!",
                de: $"⚠️ HEILIGER SCHILD BRICHT ZUSAMMEN IN: {sec}s!",
                it: $"⚠️ LO SCUDO SACRO STA CROLLANDO IN: {sec}s!",
                tr: $"⚠️ KUTSAL KALKAN ÇÖKÜYOR: {sec}sn!",
                ru: $"⚠️ ЩИТ РУШИТСЯ ЧЕРЕЗ: {sec} с!",
                fr: $"⚠️ LE BOUCLIER S'EFFONDRE DANS : {sec}s !",
                es: $"⚠️ ¡EL ESCUDO SE DERRUMBA EN: {sec}s!",
                hi: $"⚠️ पवित्र ढाल ढहने में समय: {sec} सेकंड!",
                zh: $"⚠️ 神圣护盾即将在 {sec} 秒内崩溃！",
                ja: $"⚠️ 神聖シールド崩壊まであと: {sec}秒！",
                pt: $"⚠️ ESCUDO SAGRADO EM COLAPSO EM: {sec}s!"
            );
        }
        else
        {
            shieldColor = new Color(1f, 0.35f, 0.35f);
            shieldStatus = LanguageSelectionUI.Localize(
                "⚔️ SHIELD BROKEN! THE TREE IS FULLY VULNERABLE!",
                ar: "⚔️ سقط الدرع! الشجرة معرضة للتدمير بالكامل!",
                fa: "⚔️ سپر شکسته شد! درخت کاملاً آسیب‌پذیر است!",
                de: "⚔️ SCHILD GEBROCHEN! DER BAUM IST VOLLSTÄNDIG VERWUNDBAR!",
                it: "⚔️ SCUDO SPEZZATO! L'ALBERO È COMPLETAMENTE VULNERABILE!",
                tr: "⚔️ KALKAN KIRILDI! AĞAÇ TAMAMEN SAVUNMASIZ!",
                ru: "⚔️ ЩИТ СЛОМАН! ДЕРЕВО УЯЗВИМО!",
                fr: "⚔️ BOUCLIER BRISÉ ! L'ARBRE EST VULNÉRABLE !",
                es: "⚔️ ¡ESCUDO ROTO! ¡EL ÁRBOL ESTÁ VULNERABLE!",
                hi: "⚔️ ढाल टूट गई! वृक्ष पूरी तरह से असुरक्षित है!",
                zh: "⚔️ 护盾已破！神树现已完全暴露，发起总攻！",
                ja: "⚔️ シールド崩壊！今すぐ神木を伐採せよ！",
                pt: "⚔️ ESCUDO QUEBRADO! A ÁRVORE ESTÁ TOTALMENTE VULNERÁVEL!"
            );
        }

        GUIStyle shieldStyle = new GUIStyle(GUI.skin.label);
        shieldStyle.alignment = TextAnchor.MiddleCenter;
        shieldStyle.fontSize = 12;
        shieldStyle.fontStyle = FontStyle.Bold;
        shieldStyle.normal.textColor = shieldColor;

        float statusW = Mathf.Clamp(sw * 0.75f, 420f, 750f);
        float statusX = (sw - statusW) * 0.5f;
        float statusY = barY + barH + 6f;
        float statusH = 24f;

        if (IsShieldActive && currentHealth <= (maxHealth * 0.355f))
        {
            EnchantedNatureGUI.DrawWoodPlate(new Rect(statusX - 10f, statusY - 2f, statusW + 20f, statusH + 4f), new Color(0.12f, 0.16f, 0.10f, 0.92f), new Color(1f, 0.85f, 0.25f, 0.95f));
        }

        GUI.Label(new Rect(statusX, statusY, statusW, statusH), shieldStatus, shieldStyle);
    }
}