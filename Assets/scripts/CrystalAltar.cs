using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sacred Crystal Altar (مذبح الكريستالة المقدس - أسلوب GTA Checkpoint):
/// 1. GTA Mission Checkpoint Visuals:
///    - Completely on the ground (flat and smooth flush with grass terrain)
///    - Luminous Ground Corona Disc with rotating runic energy ring
///    - Translucent vertical cylinder of glowing celestial light (pass-through)
///    - Orbiting light motes and glowing orbs revolving smoothly around the cylinder perimeter
///    - Sky Beacon beam visible from across the entire map
/// 2. Performance Optimized & Zero-Leak:
///    - Static shared textures and materials (generated once, shared across all 4 altars)
///    - Guarded single-pass initialization (no infinite recursion or object duplication)
///    - Single smooth ambient point light per altar (high performance, no GPU choke)
/// 3. Grand Victory Celebration:
///    - Massive ancient roots erupt from the earth around the altar and tree base
///    - The great beanstalk tree surges with rapid explosive celebratory growth
///    - Powerful nature victory fanfare audio plays
///    - Displays the Match End Statistics UI with full player statistics
/// </summary>
public class CrystalAltar : NetworkBehaviour
{
    public static CrystalAltar Instance { get; private set; }
    public static List<CrystalAltar> AllAltars = new List<CrystalAltar>();

    [Header("Altar Info")]
    public string altarName = "Sacred Crystal Sanctuary";
    public float triggerRadius = 4.2f;

    [Header("Protection Aura")]
    public float damageReductionPercent = 0.5f;

    [Header("Visual & Sound FX")]
    public ParticleSystem auraEffect;
    public ParticleSystem victoryEffect;
    public Light altarLight;
    public AudioSource audioSource;
    public AudioClip victorySound;

    private bool gameWon = false;
    private GameObject sanctuaryVisuals;
    private Light beaconLight;

    // GTA Checkpoint Dynamic Animated Visuals
    private Transform groundRuneRing;
    private Transform groundInnerCore;
    private Transform lightCylinder;
    private Transform skyBeacon;
    private Transform floatingCrystal;
    private Transform labelTransform;
    private TextMesh altarLabelTextMesh;
    private string lastAltarLanguage = "";
    private Light heartLight;

    private List<Transform> primaryOrbitOrbs = new List<Transform>();
    private List<Transform> secondaryOrbitOrbs = new List<Transform>();

    // Shared Materials & Textures (توليد لمرة واحدة ومشاركتها لمنع أي تسريب للذاكرة)
    private static Texture2D sharedCoronaTex;
    private static Texture2D sharedRuneTex;
    private static Material sharedCoronaMat;
    private static Material sharedRuneMat;
    private static Material sharedCylinderMat;
    private static Material sharedWhiteGlowMat;
    private static Material sharedEmeraldMat;
    private static Material sharedSkyBeamMat;

    [Header("Sacred Consecration (3-Second Altar Hold)")]
    public float innerCircleRadius = 2.8f;
    public const float REQUIRED_CHANNEL_TIME = 3.0f;
    private GameObject activeCarrier = null;
    private float channelTimer = 0f;

    public static CrystalAltar ChannellingAltar { get; private set; } = null;
    public static bool IsAnyAltarChannelling => ChannellingAltar != null && ChannellingAltar.activeCarrier != null && !ChannellingAltar.gameWon;
    public float ChannelProgress => Mathf.Clamp01(channelTimer / REQUIRED_CHANNEL_TIME);
    public float RemainingTime => Mathf.Max(0f, REQUIRED_CHANNEL_TIME - channelTimer);
    public GameObject ActiveCarrier => activeCarrier;
    public bool IsChannelling => activeCarrier != null && channelTimer > 0f && !gameWon;

    private static bool hasConfiguredAltars = false;

    public static void ResetStaticState()
    {
        hasConfiguredAltars = false;
        AllAltars.Clear();
        Instance = null;
        ChannellingAltar = null;
    }

    void Awake()
    {
        AllAltars.RemoveAll(a => a == null);
        if (!AllAltars.Contains(this)) AllAltars.Add(this);
        if (Instance == null) Instance = this;

        // تشغيل التهيئة لمرة واحدة فقط عند بدء التشغيل لمنع أي حلقة تكرارية
        if (!hasConfiguredAltars)
        {
            EnsureFourAltarsConfigured();
        }
    }

    void Start()
    {
        triggerRadius = innerCircleRadius;
        GroundSanctuary();
        BuildSanctuaryVisuals();

        // Pass-through trigger on the ground (no elevated obstacles)
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = innerCircleRadius;
        }
        else
        {
            existingCollider.isTrigger = true;
            if (existingCollider is SphereCollider sc)
            {
                sc.radius = innerCircleRadius;
            }
        }

        gameObject.tag = "Ground";

        if (auraEffect != null && !auraEffect.isPlaying)
            auraEffect.Play();
    }

    void OnDestroy()
    {
        if (AllAltars.Contains(this)) AllAltars.Remove(this);
        if (Instance == this)
        {
            Instance = AllAltars.Count > 0 ? AllAltars[0] : null;
        }
    }

    public static CrystalAltar GetClosestAltar(Vector3 pos)
    {
        CrystalAltar closest = null;
        float minDist = float.MaxValue;
        for (int i = 0; i < AllAltars.Count; i++)
        {
            CrystalAltar a = AllAltars[i];
            if (a == null) continue;
            float d = Vector3.Distance(pos, a.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = a;
            }
        }
        return closest != null ? closest : Instance;
    }

    public static float GetDistanceToClosestAltar(Vector3 pos)
    {
        CrystalAltar closest = GetClosestAltar(pos);
        if (closest != null)
        {
            return Vector3.Distance(pos, closest.transform.position);
        }
        return float.MaxValue;
    }

    public void GroundSanctuary()
    {
        float targetGroundY = 0.025f;
        // Search specifically for the true ground terrain Plane (never elevated platforms which are at Y >= 4.0m)
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(transform.position.x, 40f, transform.position.z), Vector3.down, 80f);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null)
            {
                string cName = hit.collider.name.ToLower();
                if (cName.Contains("plane") || (hit.collider.CompareTag("Ground") && hit.point.y < 0.8f))
                {
                    targetGroundY = hit.point.y + 0.025f;
                    break;
                }
            }
        }
        transform.position = new Vector3(transform.position.x, targetGroundY, transform.position.z);
    }

    /// <summary>
    /// توزيع وتثبيت المذابح الأربعة على الأرض في السهل المفتوح بعيداً تماماً عن الشجرة
    /// يتم تنفيذها مرة واحدة فقط وبطريقة آمنة بدون تكرار
    /// </summary>
    public static void EnsureFourAltarsConfigured()
    {
        if (hasConfiguredAltars) return;
        hasConfiguredAltars = true;

        Vector3 center = Vector3.zero;
        GameObject tree = GameObject.FindGameObjectWithTag("Tree");
        if (tree == null) tree = GameObject.Find("Tree");
        if (tree != null) center = tree.transform.position;

        // إزالة أي مجسم قديم أو كائن زائد داخل أو ملاصق لجذع الشجرة
        if (tree != null)
        {
            for (int c = tree.transform.childCount - 1; c >= 0; c--)
            {
                Transform child = tree.transform.GetChild(c);
                string lower = child.name.ToLower();
                if (lower.Contains("altar") || lower.Contains("sanctuary") || lower.Contains("dais") || lower.Contains("platformmesh"))
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        // توزيع المذابح الأربعة على مسافة 26 متراً مسطحة تماماً على الأرض بنفس الارتفاع
        float radius = 26f;
        Vector3[] targetPositions = new Vector3[]
        {
            new Vector3(center.x, 0.025f, center.z + radius),     // North (شمال)
            new Vector3(center.x + radius, 0.025f, center.z),     // East (شرق)
            new Vector3(center.x, 0.025f, center.z - radius),    // South (جنوب)
            new Vector3(center.x - radius, 0.025f, center.z)     // West (غرب)
        };

        string[] names = new string[]
        {
            "Altar_North (شمال)",
            "Altar_East (شرق)",
            "Altar_South (جنوب)",
            "Altar_West (غرب)"
        };

        CrystalAltar[] existing = Object.FindObjectsByType<CrystalAltar>(FindObjectsSortMode.None);
        List<CrystalAltar> validList = new List<CrystalAltar>();

        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null && !validList.Contains(existing[i]))
            {
                validList.Add(existing[i]);
            }
        }

        GameObject group = GameObject.Find("SacredAltars");
        if (group == null) group = new GameObject("SacredAltars");

        for (int i = 0; i < 4; i++)
        {
            CrystalAltar altarComp = null;
            if (i < validList.Count)
            {
                altarComp = validList[i];
            }
            else
            {
                GameObject newObj = new GameObject(names[i]);
                newObj.transform.position = targetPositions[i];
                newObj.transform.SetParent(group.transform);
                altarComp = newObj.AddComponent<CrystalAltar>();
                validList.Add(altarComp);
            }

            altarComp.gameObject.name = names[i];
            altarComp.altarName = names[i];
            altarComp.transform.position = targetPositions[i];
            altarComp.GroundSanctuary();

            // تنظيف أي مجسمات قديمة متبقية أو كائنات زائدة
            for (int c = altarComp.transform.childCount - 1; c >= 0; c--)
            {
                Transform ch = altarComp.transform.GetChild(c);
                string chName = ch.name;
                if (chName.StartsWith("AltarLight") || chName == "PlatformMesh" || 
                    chName.Contains("Cylinder") || chName.Contains("Orb") || 
                    chName.Contains("Crystal") || chName.Contains("Beacon") ||
                    chName == "SanctuaryVisuals")
                {
                    if (Application.isPlaying) Destroy(ch.gameObject);
                    else DestroyImmediate(ch.gameObject);
                }
            }

            if (!AllAltars.Contains(altarComp)) AllAltars.Add(altarComp);
        }

        // إزالة أي مذابح زائدة عن العدد 4 إن وجدت
        for (int i = 4; i < validList.Count; i++)
        {
            if (validList[i] != null)
            {
                if (AllAltars.Contains(validList[i])) AllAltars.Remove(validList[i]);
                if (Application.isPlaying) Destroy(validList[i].gameObject);
                else DestroyImmediate(validList[i].gameObject);
            }
        }
    }

    /// <summary>
    /// بناء مظهر المذبح بأسلوب نقطة تفتيش مهمات GTA (GTA Mission Checkpoint / Corona)
    /// أسطوانة نور سماوي مضيئة ومتوهجة بشفافية تامة (Additive Blending) مع قلب مضيء وشعاع في السماء
    /// تضمن رؤية المذبح بوضوح تام من أي مكان في الخريطة دون أي سواد أو عتمة
    /// </summary>
    void BuildSanctuaryVisuals()
    {
        if (sanctuaryVisuals != null) Destroy(sanctuaryVisuals);

        sanctuaryVisuals = new GameObject("SanctuaryVisuals");
        sanctuaryVisuals.transform.SetParent(transform, false);

        primaryOrbitOrbs.Clear();
        secondaryOrbitOrbs.Clear();

        InitSharedMaterials();

        // =========================================================================
        // 0. PHYSICAL SACRED STONE DAIS (قاعدة المذبح الحجرية المنحوتة بنقوش مقدسة)
        // قاعدة دائرية صلبة بارتفاع خفيف flush مع العشب تمنح المذبح هيبة وواقعية
        // =========================================================================
        GameObject stoneDais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stoneDais.name = "AltarStoneDais";
        stoneDais.transform.SetParent(sanctuaryVisuals.transform, false);
        stoneDais.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        stoneDais.transform.localScale = new Vector3(5.8f, 0.07f, 5.8f);
        DestroyCollider(stoneDais);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material daisMat = new Material(litShader);
        daisMat.color = new Color(0.24f, 0.32f, 0.38f);
        if (daisMat.HasProperty("_BaseColor")) daisMat.SetColor("_BaseColor", daisMat.color);
        if (daisMat.HasProperty("_Smoothness")) daisMat.SetFloat("_Smoothness", 0.40f);
        stoneDais.GetComponent<Renderer>().material = daisMat;

        // =========================================================================
        // 1. GROUND CORONA RING (الهالة الدائرية المتوهجة المسطحة تماماً على الأرض)
        // =========================================================================
        GameObject outerCorona = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outerCorona.name = "GroundCoronaRing";
        outerCorona.transform.SetParent(sanctuaryVisuals.transform, false);
        outerCorona.transform.localPosition = new Vector3(0f, 0.075f, 0f);
        outerCorona.transform.localScale = new Vector3(5.6f, 0.001f, 5.6f);
        DestroyCollider(outerCorona);
        outerCorona.GetComponent<Renderer>().sharedMaterial = sharedCoronaMat;

        // حلقة رونية داخلية تدور بالاتجاه المعاكس
        GameObject innerRune = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerRune.name = "GroundRuneRing";
        innerRune.transform.SetParent(sanctuaryVisuals.transform, false);
        innerRune.transform.localPosition = new Vector3(0f, 0.080f, 0f);
        innerRune.transform.localScale = new Vector3(4.0f, 0.001f, 4.0f);
        DestroyCollider(innerRune);
        innerRune.GetComponent<Renderer>().sharedMaterial = sharedRuneMat;
        groundRuneRing = innerRune.transform;

        // قلب الهالة الداخلي
        GameObject innerCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerCore.name = "GroundInnerCore";
        innerCore.transform.SetParent(sanctuaryVisuals.transform, false);
        innerCore.transform.localPosition = new Vector3(0f, 0.085f, 0f);
        innerCore.transform.localScale = new Vector3(2.2f, 0.001f, 2.2f);
        DestroyCollider(innerCore);
        innerCore.GetComponent<Renderer>().sharedMaterial = sharedCoronaMat;
        groundInnerCore = innerCore.transform;

        // =========================================================================
        // 2. GTA VERTICAL LIGHT CYLINDER (عمود النور السماوي الشفاف بنمط GTA)
        // خامة Additive Blending مشعة وشفافة بالكامل - مستحيل أن تكون سوداء
        // =========================================================================
        GameObject vertCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vertCylinder.name = "GtaLightCylinder";
        vertCylinder.transform.SetParent(sanctuaryVisuals.transform, false);
        vertCylinder.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        vertCylinder.transform.localScale = new Vector3(4.6f, 3.5f, 4.6f);
        DestroyCollider(vertCylinder);
        vertCylinder.GetComponent<Renderer>().sharedMaterial = sharedCylinderMat;
        lightCylinder = vertCylinder.transform;

        // =========================================================================
        // 3. CENTRAL GLOWING HEART (قلب المذبح الكريستالي الطافي المشع في المنتصف)
        // "فيه ضوء يخرج منه ضوء على القلب، ليفهم اللاعب أن عليه الذهاب إلى هناك"
        // =========================================================================
        GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObj.name = "CentralGlowingHeart";
        coreObj.transform.SetParent(sanctuaryVisuals.transform, false);
        coreObj.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        coreObj.transform.localScale = new Vector3(0.65f, 1.05f, 0.65f);
        DestroyCollider(coreObj);
        coreObj.GetComponent<Renderer>().sharedMaterial = sharedWhiteGlowMat;
        floatingCrystal = coreObj.transform;

        // إضاءة مركزية ساطعة تشع مباشرة من داخل القلب الكريستالي
        GameObject hLightObj = new GameObject("HeartPointLight");
        hLightObj.transform.SetParent(coreObj.transform, false);
        hLightObj.transform.localPosition = Vector3.zero;
        heartLight = hLightObj.AddComponent<Light>();
        heartLight.type = LightType.Point;
        heartLight.color = new Color(0.40f, 0.95f, 1.0f);
        heartLight.range = 10f;
        heartLight.intensity = 4.5f;

        // =========================================================================
        // 4. SKY BEACON BEAM (شعاع سماوي ممتد للأعلى ليرى اللاعب موقع المذبح من بعيد)
        // =========================================================================
        GameObject skyBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        skyBeam.name = "SkyBeaconBeam";
        skyBeam.transform.SetParent(sanctuaryVisuals.transform, false);
        skyBeam.transform.localPosition = new Vector3(0f, 25f, 0f);
        skyBeam.transform.localScale = new Vector3(0.35f, 25f, 0.35f);
        DestroyCollider(skyBeam);
        skyBeam.GetComponent<Renderer>().sharedMaterial = sharedSkyBeamMat;
        skyBeacon = skyBeam.transform;

        // =========================================================================
        // 5. AMBIENT GROUND LIGHT (إضاءة المذبح المحيطية الناعمة على مستوى الأرض)
        // =========================================================================
        GameObject lightObj = new GameObject("AltarGroundLight");
        lightObj.transform.SetParent(sanctuaryVisuals.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        beaconLight = lightObj.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = new Color(0.22f, 0.88f, 1.0f);
        beaconLight.range = 15f;
        beaconLight.intensity = 3.5f;

        // =========================================================================
        // 6. 3D FLOATING BANNER (يافطة نصية ثلاثية الأبعاد واضحة من بعيد تلتزم بلغة اللعبة المختارة فقط)
        // =========================================================================
        GameObject labelObj = new GameObject("AltarLabelText");
        labelObj.transform.SetParent(sanctuaryVisuals.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 3.2f, 0f);
        altarLabelTextMesh = labelObj.AddComponent<TextMesh>();
        altarLabelTextMesh.fontSize = 28;
        altarLabelTextMesh.characterSize = 0.08f;
        altarLabelTextMesh.alignment = TextAlignment.Center;
        altarLabelTextMesh.anchor = TextAnchor.MiddleCenter;
        altarLabelTextMesh.color = new Color(0.45f, 0.95f, 1.0f);
        labelTransform = labelObj.transform;
        UpdateAltarLabelText();
    }

    void UpdateAltarLabelText()
    {
        if (altarLabelTextMesh == null) return;
        string currentLang = LanguageSelectionUI.CurrentLanguageCode;
        if (lastAltarLanguage == currentLang) return;
        lastAltarLanguage = currentLang;

        altarLabelTextMesh.text = LanguageSelectionUI.Localize(
            "✦ SACRED ALTAR ✦",
            ar: "✦ المذبح المقدس ✦",
            ru: "✦ СВЯЩЕННЫЙ АЛТАРЬ ✦",
            fr: "✦ AUTEL SACRÉ ✦",
            es: "✦ ALTAR SAGRADO ✦",
            de: "✦ HEILIGER ALTAR ✦",
            hi: "✦ पवित्र वेदी ✦",
            zh: "✦ 神圣祭坛 ✦",
            ja: "✦ 聖なる祭壇 ✦",
            pt: "✦ ALTAR SAGRADO ✦",
            fa: "✦ محراب مقدس ✦",
            it: "✦ ALTARE SACRO ✦",
            tr: "✦ KUTSAL SUNAK ✦"
        );
    }

    void Update()
    {
        if (gameWon) return;

        UpdateAltarLabelText();

        float t = Time.time;

        // نبض إضاءة الأرضية
        if (beaconLight != null)
        {
            beaconLight.intensity = 3.0f + Mathf.Sin(t * 3.0f) * 0.8f;
        }

        // نبض إضاءة القلب الساطع المشع
        if (heartLight != null)
        {
            heartLight.intensity = 4.0f + Mathf.Sin(t * 4.5f) * 1.5f;
        }

        // توجيه اليافطة النصية دائماً نحو الكاميرا الرئيسية
        if (Camera.main != null && labelTransform != null)
        {
            labelTransform.rotation = Quaternion.LookRotation(labelTransform.position - Camera.main.transform.position);
        }

        // دوران حلقات الهالة الأرضية
        if (groundRuneRing != null)
        {
            groundRuneRing.Rotate(Vector3.up, -22f * Time.deltaTime, Space.World);
        }
        if (groundInnerCore != null)
        {
            groundInnerCore.Rotate(Vector3.up, 30f * Time.deltaTime, Space.World);
        }

        // دوران عمود النور
        if (lightCylinder != null)
        {
            lightCylinder.Rotate(Vector3.up, 12f * Time.deltaTime, Space.World);
        }

        // دوران وحركة القلب المشع في المنتصف
        if (floatingCrystal != null)
        {
            floatingCrystal.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
            float bob = Mathf.Sin(t * 2.8f) * 0.14f;
            floatingCrystal.localPosition = new Vector3(0f, 1.35f + bob, 0f);
        }

        // دوران شعاع السماء
        if (skyBeacon != null)
        {
            skyBeacon.Rotate(Vector3.up, 8f * Time.deltaTime, Space.World);
        }

        bool isServerOrAuthority = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer;
        if (!isServerOrAuthority) return;

        UpdateAltarChannellingServer();
    }

    void UpdateAltarChannellingServer()
    {
        if (gameWon) return;

        // 1. إذا كان هناك بطل حامي يقوم حالياً بتعميد الكريستالة في هذا المذبح
        if (activeCarrier != null)
        {
            bool stillValid = false;
            if (activeCarrier.activeInHierarchy)
            {
                Health hp = activeCarrier.GetComponent<Health>();
                bool isAlive = hp == null || !hp.isDead;
                bool carriesCrystal = (CrystalItem.Instance != null && CrystalItem.Instance.IsCarrier(activeCarrier));

                Vector3 carrierPos = activeCarrier.transform.position;
                Vector3 altarCenter = transform.position;
                float horizDist = Vector2.Distance(
                    new Vector2(carrierPos.x, carrierPos.z),
                    new Vector2(altarCenter.x, altarCenter.z)
                );
                float vertDist = Mathf.Abs(carrierPos.y - altarCenter.y);

                // يجب أن يبقى الحامل حياً، ومعه الكريستالة، وداخل الدائرة الدقيقة للمذبح (أقل من 2.8م)
                if (isAlive && carriesCrystal && horizDist <= innerCircleRadius && vertDist <= 2.5f)
                {
                    stillValid = true;
                }
            }

            if (stillValid)
            {
                channelTimer += Time.deltaTime;
                ApplyChannellingDynamicVisuals(ChannelProgress);

                // بعد صمود الحامل 3 ثوانٍ كاملة وهو حي داخل الدائرة:
                if (channelTimer >= REQUIRED_CHANNEL_TIME)
                {
                    OnCrystalDeliveredServer(activeCarrier);
                }
            }
            else
            {
                // إذا مات الحامل أو خرج من الدائرة أو أسقط الكريستالة قبل انقضاء الـ 3 ثوانٍ
                CancelChannelling();
            }
            return;
        }

        // 2. البحث عن حامل كريستالة من الحماة دخل في قلب دائرة المذبح لبدء الـ 3 ثوانٍ
        if (CrystalItem.Instance != null && CrystalItem.Instance.isCarried.Value)
        {
            GameObject carrier = CrystalItem.Instance.GetCarrierObject();
            if (carrier != null && carrier.activeInHierarchy)
            {
                PlayerMovement pm = carrier.GetComponent<PlayerMovement>();
                BotController bot = carrier.GetComponent<BotController>();
                bool isProtector = (pm != null && pm.team == PlayerMovement.TeamType.Protector) ||
                                   (bot != null && bot.team == PlayerMovement.TeamType.Protector);

                if (isProtector)
                {
                    Health hp = carrier.GetComponent<Health>();
                    if (hp == null || !hp.isDead)
                    {
                        Vector3 carrierPos = carrier.transform.position;
                        Vector3 altarCenter = transform.position;
                        float horizDist = Vector2.Distance(
                            new Vector2(carrierPos.x, carrierPos.z),
                            new Vector2(altarCenter.x, altarCenter.z)
                        );
                        float vertDist = Mathf.Abs(carrierPos.y - altarCenter.y);

                        // دخول دائرة المذبح المقدسة (2.8 متر)
                        if (horizDist <= innerCircleRadius && vertDist <= 2.5f)
                        {
                            StartChannelling(carrier);
                        }
                    }
                }
            }
        }
    }

    void StartChannelling(GameObject carrier)
    {
        activeCarrier = carrier;
        channelTimer = 0f;
        ChannellingAltar = this;

        string cName = carrier.name.Replace("(Clone)", "").Trim();
        Debug.Log($"⚡ [CrystalAltar] {cName} entered sanctuary circle! 3-Second crystal consecration started!");

        string msg = LanguageSelectionUI.Localize(
            $"⚡ {cName.ToUpper()} PLACED CRYSTAL ON ALTAR! HOLD 3 SECONDS! ⚡",
            ar: $"⚡ وضع {cName} الكريستالة في المذبح! اثبت حياً في المذبح 3 ثوانٍ للفوز! ⚡",
            ru: $"⚡ {cName.ToUpper()} ПОМЕСТИЛ КРИСТАЛЛ! УДЕРЖИВАЙТЕ 3 СЕКУНДЫ! ⚡",
            fr: $"⚡ {cName.ToUpper()} A POSÉ LE CRISTAL ! TENEZ 3 SECONDES ! ⚡",
            es: $"⚡ ¡{cName.ToUpper()} COLOCÓ EL CRISTAL! ¡MANTÉN LA POSICIÓN 3 SEGUNDOS! ⚡",
            de: $"⚡ {cName.ToUpper()} HAT DEN KRISTALL PLATZIERT! 3 SEKUNDEN HALTEN! ⚡",
            hi: $"⚡ {cName} ने वेदी पर क्रिस्टल रखा! 3 सेकंड तक बने रहें! ⚡",
            zh: $"⚡ {cName} 已将水晶放入圣坛！坚守3秒即可获胜！⚡",
            ja: $"⚡ {cName}が水晶を祭壇に設置！3秒間維持せよ！⚡",
            pt: $"⚡ {cName.ToUpper()} COLOCOU O CRISTAL! SEGURE POR 3 SEGUNDOS! ⚡",
            fa: $"⚡ {cName} بلور را در محراب قرار داد! 3 ثانیه مقاومت کنید! ⚡",
            it: $"⚡ {cName.ToUpper()} HA MESSO IL CRISTALLO SULL'ALTARE! RESISTI 3 SECONDI! ⚡",
            tr: $"⚡ {cName.ToUpper()} KRİSTALİ SUNAĞA YERLEŞTİRDİ! 3 SANİYE DAYANIN! ⚡"
        );
        PlayerProgressionAndShopUI.Instance?.ShowNotification(msg);
    }

    public void CancelChannelling()
    {
        if (activeCarrier != null)
        {
            Debug.Log($"⚠️ [CrystalAltar] Crystal channelling interrupted!");
            ResetAltarVisualsToIdle();
        }
        activeCarrier = null;
        channelTimer = 0f;
        if (ChannellingAltar == this) ChannellingAltar = null;
    }

    void ApplyChannellingDynamicVisuals(float progress)
    {
        // 1. Soft Celestial Nature Aura (Cyan/Aquamarine to Vibrant Emerald - No harsh yellow light!)
        if (beaconLight != null)
        {
            float pulse = Mathf.PingPong(Time.time * 4f, 0.8f);
            beaconLight.intensity = Mathf.Lerp(3.2f, 4.8f, progress) + pulse;
            beaconLight.range = Mathf.Lerp(14f, 20f, progress);
            beaconLight.color = Color.Lerp(new Color(0.22f, 0.88f, 1.0f), new Color(0.20f, 0.95f, 0.75f), progress);
        }

        // 2. Crystal Heart Glimmer (Gentle, elegant celestial pulse)
        if (heartLight != null)
        {
            heartLight.intensity = Mathf.Lerp(3.8f, 5.5f, progress) + Mathf.PingPong(Time.time * 5f, 1.0f);
            heartLight.range = Mathf.Lerp(10f, 15f, progress);
            heartLight.color = Color.Lerp(new Color(0.35f, 0.90f, 1.0f), new Color(0.25f, 0.98f, 0.85f), progress);
        }

        // 3. تسارع دوران الحلقات الرونية وعمود النور
        if (groundRuneRing != null)
        {
            groundRuneRing.Rotate(Vector3.up, -120f * Time.deltaTime, Space.World);
        }
        if (lightCylinder != null)
        {
            lightCylinder.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
            float scaleSurge = 1f + progress * 0.25f + Mathf.Sin(Time.time * 12f) * 0.05f;
            lightCylinder.localScale = new Vector3(4.6f * scaleSurge, 3.5f * scaleSurge, 4.6f * scaleSurge);
        }
        if (floatingCrystal != null)
        {
            floatingCrystal.Rotate(Vector3.up, 180f * Time.deltaTime, Space.World);
        }
    }

    void ResetAltarVisualsToIdle()
    {
        if (beaconLight != null)
        {
            beaconLight.color = new Color(0.22f, 0.88f, 1.0f);
            beaconLight.range = 15f;
            beaconLight.intensity = 3.5f;
        }
        if (heartLight != null)
        {
            heartLight.color = new Color(0.40f, 0.95f, 1.0f);
            heartLight.range = 10f;
            heartLight.intensity = 4.5f;
        }
        if (lightCylinder != null)
        {
            lightCylinder.localScale = new Vector3(4.6f, 3.5f, 4.6f);
        }
    }

    public void OnCrystalDeliveredServer(GameObject carrier)
    {
        if (gameWon || carrier == null) return;

        // Mark gameWon across ALL altars to prevent duplicate triggers
        for (int i = 0; i < AllAltars.Count; i++)
        {
            if (AllAltars[i] != null) AllAltars[i].gameWon = true;
        }
        gameWon = true;
        GameStateManager.Instance?.NotifyCrystalConsecrated();
        InGameChatManager.Instance?.NotifyCrystalDelivered();
        ChannellingAltar = null;

        Debug.Log($"👑 CRYSTAL CONSECRATION COMPLETE AT {altarName} by {carrier.name}! PROTECTORS WIN!");

        // الإعلان الواضح فوق كل شيء: "لقد وصلت الكريستالة للمذبح، فاز الحماة"
        string victoryAnnouncement = LanguageSelectionUI.Localize(
            "🌟 THE SACRED CRYSTAL REACHED THE SANCTUARY! THE PROTECTORS HAVE WON! 🌟",
            ar: "🌟 لقد وصلت الكريستالة للمذبح، فاز الحماة! 🌟",
            ru: "🌟 КРИСТАЛЛ ДОСТИГ АЛТАРЯ! ЗАЩИТНИКИ ПОБЕДИЛИ! 🌟",
            fr: "🌟 LE CRISTAL A ATTEINT L'AUTEL, LES PROTECTEURS ONT GAGNÉ ! 🌟",
            es: "🌟 ¡EL CRISTAL HA LLEGADO AL ALTAR, GANARON LOS PROTECTORES! 🌟",
            de: "🌟 DER KRISTALL HAT DEN ALTAR ERREICHT, DIE BESCHÜTZER HABEN GEWONNEN! 🌟",
            hi: "🌟 क्रिस्टल वेदी पर पहुंच गया, संरक्षकों की विजय हुई! 🌟",
            zh: "🌟 水晶已送达圣坛，守护者获胜！🌟",
            ja: "🌟 水晶が祭壇に到達した、守護者の勝利！🌟",
            pt: "🌟 O CRISTAL CHEGOU AO ALTAR, OS PROTETORES VENCERAM! 🌟",
            fa: "🌟 بلور به محراب رسید، حامیان پیروز شدند! 🌟",
            it: "🌟 IL CRISTALLO SACRO HA RAGGIUNTO L'ALTARE! I PROTETTORI HANNO VINTO! 🌟",
            tr: "🌟 KUTSAL KRİSTAL SUNAĞA ULAŞTI! KORUYUCULAR KAZANDI! 🌟"
        );
        PlayerProgressionAndShopUI.Instance?.ShowNotification(victoryAnnouncement);

        // 1. Register stats safely
        try
        {
            MatchEndStatsUI.Instance?.RegisterCrystalDelivered(carrier);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CrystalAltar] RegisterCrystalDelivered exception: {ex}");
        }

        // 2. Trigger Victory Spectacle across all clients (with local fallback if unspawned)
        try
        {
            if (IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                TriggerVictorySpectacleRpc(transform.position, carrier.name);
            }
            else
            {
                TriggerVictorySpectacleLocal(transform.position, carrier.name);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CrystalAltar] TriggerVictorySpectacleRpc failed, falling back to local: {ex}");
            TriggerVictorySpectacleLocal(transform.position, carrier.name);
        }

        // 3. Trigger explosive celebratory tree growth
        try
        {
            TreeGrowth tree = FindAnyObjectByType<TreeGrowth>();
            if (tree != null)
            {
                tree.TriggerCelebratoryExplosiveGrowth(18f);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CrystalAltar] TriggerCelebratoryExplosiveGrowth exception: {ex}");
        }

        // 4. Begin Grand 5.2-Second Divine Victory Celebration (Sky Pillars, Golden Petals Raining, Flowers Blooming, Butterflies)
        // Only AFTER the full celebration completes will GameStateManager and the Scoreboard be shown!
        StartCoroutine(ProtectorVictoryCelebrationRoutine(carrier));
    }

    void OnGUI()
    {
        if (gameWon) return;
        if (activeCarrier == null || channelTimer <= 0f) return;

        // شريط تقدم تعميد الكريستالة والعد التنازلي لـ 3 ثوانٍ في أعلى الشاشة
        float sw = Screen.width;
        float boxW = Mathf.Clamp(sw * 0.48f, 360f, 560f);
        float boxH = 68f;
        float boxX = (sw - boxW) * 0.5f;
        float boxY = 72f;

        Rect bannerRect = new Rect(boxX, boxY, boxW, boxH);
        EnchantedNatureGUI.DrawWoodPlate(bannerRect, new Color(0.12f, 0.20f, 0.12f, 0.96f), new Color(1.0f, 0.85f, 0.25f, 0.98f));

        float remTime = RemainingTime;
        float progress = ChannelProgress;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(1.0f, 0.90f, 0.35f);

        string title = LanguageSelectionUI.Localize(
            $"⚡ CONSECRATING CRYSTAL: {remTime:F1}s ⚡",
            ar: $"⚡ جاري تثبيت الكريستالة في المذبح: {remTime:F1} ثوانٍ ⚡",
            ru: $"⚡ ОСВЯЩЕНИЕ КРИСТАЛЛА: {remTime:F1}с ⚡",
            fr: $"⚡ CONSÉCRATION DU CRISTAL : {remTime:F1}s ⚡",
            es: $"⚡ CONSAGRANDO EL CRISTAL: {remTime:F1}s ⚡",
            de: $"⚡ WEIHE DES KRISTALLS: {remTime:F1}s ⚡",
            hi: $"⚡ क्रिस्टल की प्रतिष्ठा: {remTime:F1}s ⚡",
            zh: $"⚡ 正在圣坛注入神圣水晶：{remTime:F1}秒 ⚡",
            ja: $"⚡ 聖なる水晶の奉納中: {remTime:F1}秒 ⚡",
            pt: $"⚡ CONSAGRANDO O CRISTAL: {remTime:F1}s ⚡",
            fa: $"⚡ تقدیس بلور مقدس در محراب: {remTime:F1} ثانیه ⚡",
            it: $"⚡ CONSACRAZIONE DEL CRISTALLO: {remTime:F1}s ⚡",
            tr: $"⚡ KRİSTAL KUTSANMASI: {remTime:F1}sn ⚡"
        );
        GUI.Label(new Rect(boxX, boxY + 6f, boxW, 22f), title, titleStyle);

        // شريط التقدم الذهبي المتوهج
        float barW = boxW - 40f;
        float barH = 14f;
        float barX = boxX + 20f;
        float barY = boxY + 30f;

        GUI.color = new Color(0.08f, 0.12f, 0.08f, 0.9f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

        GUI.color = Color.Lerp(new Color(0.2f, 0.8f, 1f), new Color(1f, 0.85f, 0.2f), progress);
        GUI.DrawTexture(new Rect(barX + 1f, barY + 1f, (barW - 2f) * progress, barH - 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // الإرشاد والتوجيه
        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Italic
        };
        subStyle.normal.textColor = new Color(0.85f, 1f, 0.85f);
        string sub = LanguageSelectionUI.Localize(
            "🛡️ Protect the carrier inside the sacred circle!",
            ar: "🛡️ اثبت حياً واحمِ الحامل داخل الدائرة المقدسة!",
            ru: "🛡️ Защитите носителя внутри круга!",
            fr: "🛡️ Protégez le porteur à l'intérieur du cercle sacré !",
            es: "🛡️ ¡Protege al portador dentro del círculo sagrado!",
            de: "🛡️ Beschütze den Träger im heiligen Kreis!",
            hi: "🛡️ पवित्र वृत्त के अंदर वाहक की रक्षा करें!",
            zh: "🛡️ 在神圣圆圈内保护水晶守护者！",
            ja: "🛡️ 聖なる円の中で運び手を守れ！",
            pt: "🛡️ Proteja o portador dentro do círculo sagrado!",
            fa: "🛡️ از حامل در داخل دایره مقدس محافظت کنید!",
            it: "🛡️ Proteggi il portatore all'interno del cerchio sacro!",
            tr: "🛡️ Kutsal çemberin içindeki taşıyıcıyı koruyun!"
        );
        GUI.Label(new Rect(boxX, boxY + 46f, boxW, 18f), sub, subStyle);
    }

    IEnumerator ProtectorVictoryCelebrationRoutine(GameObject carrier)
    {
        Vector3 altarPos = transform.position;

        // 1. Soft Celestial Ambient Glow (No blinding yellow screen or giant cylinders!)
        GameObject celestialLightObj = new GameObject("VictoryCelestialLight");
        Light celestialLight = celestialLightObj.AddComponent<Light>();
        celestialLight.type = LightType.Point;
        celestialLight.color = new Color(0.25f, 0.90f, 1.0f);
        celestialLight.intensity = 2.4f;
        celestialLight.range = 16f;
        celestialLightObj.transform.position = altarPos + Vector3.up * 2.2f;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                          ?? Shader.Find("Unlit/Transparent") 
                          ?? Shader.Find("Standard");

        // Expanding Ethereal Nature Aurora Rings on ground
        GameObject auroraRingGroup = new GameObject("CelebratoryAuroraRings");
        auroraRingGroup.transform.position = altarPos + Vector3.up * 0.05f;
        List<Transform> auroraRings = new List<Transform>();
        List<Material> auroraMats = new List<Material>();
        for (int r = 0; r < 3; r++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyCollider(ring);
            ring.transform.SetParent(auroraRingGroup.transform);
            ring.transform.position = altarPos + Vector3.up * (0.04f + r * 0.02f);
            ring.transform.localScale = new Vector3(2.5f, 0.02f, 2.5f);

            Material ringMat = new Material(unlitShader);
            ringMat.color = new Color(0.25f, 0.88f, 1.0f, 0.35f);
            ring.GetComponent<Renderer>().material = ringMat;
            auroraRings.Add(ring.transform);
            auroraMats.Add(ringMat);
        }

        // 2. Blooming Floral Meadow: 20 blooming colorful flowers sprouting out of the earth
        GameObject flowerGroup = new GameObject("CelebratoryBloomingMeadow");
        flowerGroup.transform.position = altarPos;
        List<Transform> flowers = new List<Transform>();

        Color[] flowerColors = new Color[]
        {
            new Color(1f, 0.22f, 0.38f),  // Rose
            new Color(1f, 0.88f, 0.12f),  // Golden buttercup
            new Color(0.35f, 0.82f, 1f),  // Sky blue orchid
            new Color(0.92f, 0.45f, 1f),  // Violet lily
            new Color(1f, 0.55f, 0.12f)   // Bright marigold
        };

        Material stemMat = new Material(unlitShader);
        stemMat.color = new Color(0.18f, 0.72f, 0.22f);

        for (int i = 0; i < 20; i++)
        {
            float ang = (float)i / 20f * Mathf.PI * 2f;
            float r = Random.Range(3.2f, 7.5f);
            Vector3 fPos = altarPos + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);

            GameObject flower = new GameObject($"Flower_{i}");
            flower.transform.SetParent(flowerGroup.transform);
            flower.transform.position = fPos;
            flower.transform.localScale = Vector3.zero;

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyCollider(stem);
            stem.transform.SetParent(flower.transform);
            stem.transform.localPosition = new Vector3(0, 0.45f, 0);
            stem.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
            stem.GetComponent<Renderer>().material = stemMat;

            Material petalMat = new Material(unlitShader);
            petalMat.color = flowerColors[i % flowerColors.Length];

            GameObject petalSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyCollider(petalSphere);
            petalSphere.transform.SetParent(flower.transform);
            petalSphere.transform.localPosition = new Vector3(0, 0.95f, 0);
            petalSphere.transform.localScale = new Vector3(0.48f, 0.38f, 0.48f);
            petalSphere.GetComponent<Renderer>().material = petalMat;

            flowers.Add(flower.transform);
        }

        // 3. Falling Golden Motes & Shimmering Flower Petals from Sky
        GameObject petalGroup = new GameObject("CelebratoryFallingPetals");
        petalGroup.transform.position = altarPos;
        List<Transform> petals = new List<Transform>();
        List<float> petalSpeeds = new List<float>();

        for (int i = 0; i < 30; i++)
        {
            GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyCollider(petal);
            petal.transform.SetParent(petalGroup.transform);
            float px = Random.Range(-14f, 14f);
            float pz = Random.Range(-14f, 14f);
            float py = Random.Range(18f, 40f);
            petal.transform.position = altarPos + new Vector3(px, py, pz);
            petal.transform.localScale = new Vector3(Random.Range(0.28f, 0.52f), Random.Range(0.28f, 0.52f), 1f);
            petal.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));

            Material pMat = new Material(unlitShader);
            pMat.color = (i % 2 == 0) ? new Color(1.0f, 0.92f, 0.35f, 0.88f) : new Color(1.0f, 0.60f, 0.80f, 0.88f);
            petal.GetComponent<Renderer>().material = pMat;

            petals.Add(petal.transform);
            petalSpeeds.Add(Random.Range(3.8f, 6.5f));
        }

        // 4. Fluttering Glowing Butterflies Spiraling Upwards
        GameObject butterflyGroup = new GameObject("CelebratoryButterflies");
        butterflyGroup.transform.position = altarPos;
        List<Transform> butterflies = new List<Transform>();
        List<float> bPhases = new List<float>();
        List<float> bRadii = new List<float>();

        for (int i = 0; i < 12; i++)
        {
            GameObject bObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyCollider(bObj);
            bObj.transform.SetParent(butterflyGroup.transform);
            bObj.transform.position = altarPos + Vector3.up * 1f;
            bObj.transform.localScale = new Vector3(0.35f, 0.22f, 0.35f);

            Material bMat = new Material(unlitShader);
            bMat.color = flowerColors[i % flowerColors.Length];
            bObj.GetComponent<Renderer>().material = bMat;

            butterflies.Add(bObj.transform);
            bPhases.Add((float)i / 12f * Mathf.PI * 2f);
            bRadii.Add(Random.Range(2.2f, 5.0f));
        }

        float celebrationDuration = 5.2f;
        float elapsed = 0f;

        while (elapsed < celebrationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / celebrationDuration;

            if (celestialLight != null)
            {
                celestialLight.intensity = 2.4f + Mathf.Sin(elapsed * 4f) * 0.5f;
            }

            for (int r = 0; r < auroraRings.Count; r++)
            {
                if (auroraRings[r] != null)
                {
                    float ringPhase = Mathf.Repeat(elapsed * 0.45f + (float)r / 3f, 1.0f);
                    float ringScale = Mathf.Lerp(2.5f, 13f, ringPhase);
                    auroraRings[r].localScale = new Vector3(ringScale, 0.02f, ringScale);
                    float ringAlpha = (1f - ringPhase) * 0.40f;
                    auroraMats[r].color = new Color(0.25f, 0.88f, 1.0f, ringAlpha);
                }
            }

            float bloomT = Mathf.Clamp01(elapsed / 1.8f);
            float smoothBloom = Mathf.SmoothStep(0f, 1f, bloomT);
            for (int i = 0; i < flowers.Count; i++)
            {
                if (flowers[i] != null)
                {
                    flowers[i].localScale = Vector3.one * (smoothBloom * 1.15f);
                }
            }

            for (int i = 0; i < petals.Count; i++)
            {
                if (petals[i] != null)
                {
                    petals[i].position += Vector3.down * petalSpeeds[i] * Time.deltaTime;
                    petals[i].Rotate(45f * Time.deltaTime, 60f * Time.deltaTime, 30f * Time.deltaTime);
                    if (petals[i].position.y < altarPos.y)
                    {
                        petals[i].position = altarPos + new Vector3(Random.Range(-14f, 14f), Random.Range(28f, 38f), Random.Range(-14f, 14f));
                    }
                }
            }

            for (int i = 0; i < butterflies.Count; i++)
            {
                if (butterflies[i] != null)
                {
                    float angle = bPhases[i] + elapsed * 2.2f;
                    float bY = altarPos.y + 0.8f + (elapsed * 3.4f) + Mathf.Sin(elapsed * 6f + i) * 0.4f;
                    float bx = altarPos.x + Mathf.Cos(angle) * bRadii[i];
                    float bz = altarPos.z + Mathf.Sin(angle) * bRadii[i];
                    butterflies[i].position = new Vector3(bx, bY, bz);
                    float flap = 0.22f + Mathf.Abs(Mathf.Sin(elapsed * 20f + i)) * 0.22f;
                    butterflies[i].localScale = new Vector3(flap, 0.2f, flap);
                }
            }

            yield return null;
        }

        if (celestialLightObj != null) Destroy(celestialLightObj);
        if (auroraRingGroup != null) Destroy(auroraRingGroup);
        if (flowerGroup != null) Destroy(flowerGroup);
        if (petalGroup != null) Destroy(petalGroup);
        if (butterflyGroup != null) Destroy(butterflyGroup);

        try
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.EndGame("Protectors");
            }
            else
            {
                MatchEndStatsUI.Instance?.ShowVictoryScreen("Protectors");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CrystalAltar] EndGame exception: {ex}");
            MatchEndStatsUI.Instance?.ShowVictoryScreen("Protectors");
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void TriggerVictorySpectacleRpc(Vector3 altarPos, string heroName)
    {
        TriggerVictorySpectacleLocal(altarPos, heroName);
    }

    public void TriggerVictorySpectacleLocal(Vector3 altarPos, string heroName)
    {
        if (victoryEffect != null)
        {
            victoryEffect.transform.position = altarPos + Vector3.up * 1.5f;
            victoryEffect.Play();
        }

        if (beaconLight != null)
        {
            beaconLight.color = new Color(0.25f, 0.90f, 1.0f);
            beaconLight.intensity = 3.0f;
            beaconLight.range = 18f;
        }

        PlayerProgressionAndShopUI.Instance?.ShowNotification($"🌟 {heroName.ToUpper()} PLACED THE SACRED CRYSTAL! THE FOREST SURGES WITH ETERNAL LIFE!");

        StartCoroutine(EruptAncientCelebratoryRoots(altarPos));
    }

    IEnumerator EruptAncientCelebratoryRoots(Vector3 center)
    {
        GameObject rootGroup = new GameObject("CelebratoryEruptingRoots");
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material rootMat = new Material(litShader);
        rootMat.color = new Color(0.32f, 0.20f, 0.10f);

        Material leafMat = new Material(litShader);
        leafMat.color = new Color(0.24f, 0.88f, 0.35f);

        List<Transform> eruptingRoots = new List<Transform>();
        int rootCount = 6;

        for (int i = 0; i < rootCount; i++)
        {
            float angle = (float)i / rootCount * Mathf.PI * 2f;
            Vector3 spawn = center + new Vector3(Mathf.Cos(angle) * 6.5f, -4f, Mathf.Sin(angle) * 6.5f);

            GameObject rObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rObj.name = $"EruptingRoot_{i}";
            rObj.transform.SetParent(rootGroup.transform);
            rObj.transform.position = spawn;
            rObj.transform.localScale = new Vector3(1.2f, 0.2f, 1.2f);
            rObj.transform.rotation = Quaternion.Euler(Random.Range(-25f, 25f), angle * Mathf.Rad2Deg, Random.Range(-25f, 25f));
            rObj.GetComponent<Renderer>().material = rootMat;
            DestroyCollider(rObj);

            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "LeafCluster";
            leaf.transform.SetParent(rObj.transform);
            leaf.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            leaf.transform.localScale = new Vector3(2.2f, 0.5f, 2.2f);
            leaf.GetComponent<Renderer>().material = leafMat;
            DestroyCollider(leaf);

            eruptingRoots.Add(rObj.transform);
        }

        float elapsed = 0f;
        float duration = 2.8f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < eruptingRoots.Count; i++)
            {
                Transform r = eruptingRoots[i];
                float targetY = 1.0f + Mathf.Sin(t * Mathf.PI * 0.5f) * 7.5f;
                r.position = new Vector3(r.position.x, Mathf.Lerp(-4f, targetY, smoothT), r.position.z);
                r.localScale = new Vector3(Mathf.Lerp(0.8f, 1.4f, smoothT), Mathf.Lerp(0.2f, 7.5f, smoothT), Mathf.Lerp(0.8f, 1.4f, smoothT));
                r.Rotate(0f, 15f * Time.deltaTime, 0f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// تهيئة الخامات والأنسجة المشتركة لمرة واحدة فقط لتوفير الذاكرة ومنع أي تسريب
    /// </summary>
    static void InitSharedMaterials()
    {
        if (sharedCoronaMat != null) return;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                          ?? Shader.Find("Unlit/Transparent") 
                          ?? Shader.Find("Standard");

        sharedCoronaTex = GenerateCoronaTexture(256);
        sharedRuneTex = GenerateRuneRingTexture(256);

        sharedCoronaMat = CreateTransparentMaterial(unlitShader, new Color(0.20f, 0.90f, 1.0f, 0.88f), sharedCoronaTex);
        sharedRuneMat = CreateTransparentMaterial(unlitShader, new Color(0.25f, 0.95f, 0.70f, 0.85f), sharedRuneTex);
        sharedCylinderMat = CreateAdditiveMaterial(unlitShader, new Color(0.20f, 0.85f, 1.0f, 0.35f), null);
        sharedWhiteGlowMat = CreateAdditiveMaterial(unlitShader, new Color(0.85f, 0.98f, 1.0f, 0.90f), null);
        sharedEmeraldMat = CreateAdditiveMaterial(unlitShader, new Color(0.25f, 0.95f, 0.70f, 0.85f), null);
        sharedSkyBeamMat = CreateAdditiveMaterial(unlitShader, new Color(0.25f, 0.90f, 1.0f, 0.40f), null);
    }

    /// <summary>
    /// توليد خامة شفافة ذات توهج جمعي (Additive Blending) تضمن التوهج الساطع التام ومستحيل أن تظهر بلون أسود أو داكن
    /// </summary>
    static Material CreateAdditiveMaterial(Shader shader, Color col, Texture2D tex)
    {
        Material mat = new Material(shader);
        mat.color = col;

        if (tex != null)
        {
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 1 = Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f); // 1 = Additive

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;

        return mat;
    }

    /// <summary>
    /// توليد خامة شفافة ذات توهج عالي متوافقة مع URP و Built-in
    /// </summary>
    static Material CreateTransparentMaterial(Shader shader, Color col, Texture2D tex)
    {
        Material mat = new Material(shader);
        mat.color = col;

        if (tex != null)
        {
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 1 = Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // 0 = Alpha

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    /// <summary>
    /// توليد هالة دائرية إشعاعية ناعمة لأسلوب GTA Corona Checkpoint
    /// </summary>
    static Texture2D GenerateCoronaTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / (size - 1);
                float v = (float)y / (size - 1);
                float dist = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;

                if (dist > 1.0f)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                float ring = 0f;
                if (dist >= 0.65f && dist <= 0.96f)
                {
                    float centerRing = 0.82f;
                    ring = 1.0f - Mathf.Abs(dist - centerRing) / (0.96f - centerRing);
                    ring = Mathf.Pow(Mathf.Clamp01(ring), 1.5f);
                }

                float innerGlow = Mathf.Clamp01((1.0f - dist)) * 0.35f;
                float totalAlpha = Mathf.Clamp01(ring * 0.95f + innerGlow);

                pixels[y * size + x] = new Color(1f, 1f, 1f, totalAlpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// توليد حلقة رونية مع قطاعات ضوئية دورانية
    /// </summary>
    static Texture2D GenerateRuneRingTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / (size - 1);
                float v = (float)y / (size - 1);
                Vector2 diff = new Vector2(u - 0.5f, v - 0.5f);
                float dist = diff.magnitude * 2f;

                if (dist > 1.0f || dist < 0.50f)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                float angle = Mathf.Atan2(diff.y, diff.x);
                float segments = Mathf.Sin(angle * 16f);
                float segAlpha = (segments > 0.15f) ? 1.0f : 0.15f;

                float band = 1.0f - Mathf.Abs(dist - 0.75f) / 0.25f;
                band = Mathf.Clamp01(band);

                float alpha = band * segAlpha * 0.90f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void DestroyCollider(GameObject obj)
    {
        Collider c = obj.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
