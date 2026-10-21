using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enchanted Forest Decorator:
/// Transforms the arena into an enchanting fantasy forest wonderland:
/// 1. Vibrant lush grass, ancient oak bark, and mossy canopies
/// 2. Majestic Natural Mountain Waterfall: Rugged organic boulders, multi-tiered curved cascades & crystal lagoon
/// 3. Radiant Azure Blue Sky, Fluffy 3D Cumulus Clouds, and Golden Sunbeam Shafts
/// 4. Massive gnarled tree roots and dense climbing ivy vines with leaves wrapping up the trunk
/// 5. Boundary forest of stylized pine & oak trees
/// 6. Fairy tale bioluminescent mushrooms & colorful wildflower meadows
/// 7. 40 intensely colorful butterflies with procedural curved double-sided wings (never black)
/// 8. Magical Forest Fairies (الجنّيات الساحرة) with fluttering translucent wings and ethereal soft glow
/// 9. Imaginary Forest Critters: Acorn Sprights hopping on rocks & Starlight Wisps gliding on vines
/// 10. 3D animated songbirds (3asafir) perched on branches and swooping in flight
/// 11. Crystal-clear atmosphere: No blinding white particle discs or obscuring fog
/// </summary>
[DefaultExecutionOrder(-50)]
public class EnchantedForestDecorator : MonoBehaviour
{
    public static EnchantedForestDecorator Instance { get; private set; }

    [Header("Environment Colors")]
    public Color grassColorA = new Color(0.18f, 0.62f, 0.28f);   // Vibrant emerald green
    public Color grassColorB = new Color(0.28f, 0.78f, 0.38f);   // Fresh spring meadow
    public Color barkColorA = new Color(0.32f, 0.20f, 0.10f);    // Deep oak brown
    public Color barkColorB = new Color(0.48f, 0.32f, 0.18f);    // Warm wood ridges
    public Color canopyColor = new Color(0.20f, 0.68f, 0.32f);   // Foliage canopy green
    public Color waterColor = new Color(0.15f, 0.78f, 0.95f, 0.85f); // Sparkling turquoise water

    private Material grassMaterial;
    private Material barkMaterial;
    private Material canopyMaterial;
    private Material stoneMaterial;
    private Material waterMaterial;
    private Material mushroomRedMaterial;
    private Material mushroomBlueMaterial;
    private Material darkBarkMaterial;
    private Material lushCanopyMaterial;
    private Material brightCanopyMaterial;
    private Material hollowGlowMaterial;
    private Material celestialPetalMaterial;
    private Material goldenPollenMaterial;
    private Material autumnLeafMaterial;
    private Material mountainRockMat;
    private Material mountainPeakSnowMat;
    private Material darkPineMat;
    private Material spruceMat;
    private Material darkOakMat;
    private Material highlandCedarMat;
    private Material sharedWaterStreamMat;
    private Material sharedFoamMat;
    private Material sharedPoolMat;
    private Material pinkSakuraMat;
    private Material goldenAutumnMat;
    private Material crimsonMapleMat;
    private Material vibrantSpringOakMat;
    private Material riverGrottoDarkMat;
    private Material birchBarkMat;
    private Material trunkDeepOakMat;
    private Material trunkWarmAmberCedarMat;
    private Material trunkMossyBarkMat;
    private Material trunkEmeraldSproutVineMat;
    private Material trunkGoldenChestnutMat;
    private Material trunkLichenVerdantMat;

    private GameObject decorationsRoot;
    private bool hasDecorated = false;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterSceneLoader()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedCallback;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedCallback;
    }

    private static void OnSceneLoadedCallback(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!Application.isPlaying) return;

        // 1. إعادة ضبط قوائم نمط اللعب
        PlayModeUI.SelectedMode = PlayModeUI.PlayMode.None;
        if (PlayModeUI.Instance != null)
        {
            PlayModeUI.Instance.ResetMenu();
        }

        // 2. إيجاد أو إنشاء مزين الغابة وتشغيل الزينة الكاملة للشجرة والمحيط
        EnchantedForestDecorator dec = FindAnyObjectByType<EnchantedForestDecorator>();
        if (dec == null)
        {
            GameObject obj = new GameObject("EnchantedForestDecorator");
            dec = obj.AddComponent<EnchantedForestDecorator>();
        }
        else
        {
            dec.hasDecorated = false;
            dec.ApplyFullNatureMakeover();
        }

        // 3. تفعيل وإعداد نظام الكروس إير القروسطي الأسطوري
        MedievalCrosshairHUD.InitializeCrosshair();

        // 4. التأكد من وجود مدير الزعماء
        if (FindAnyObjectByType<LevelBossManager>() == null)
        {
            GameObject bossObj = new GameObject("LevelBossManager");
            bossObj.AddComponent<LevelBossManager>();
        }

        // 5. التأكد من وجود واجهة إحصائيات نهاية المباراة
        if (FindAnyObjectByType<MatchEndStatsUI>() == null)
        {
            GameObject statsObj = new GameObject("MatchEndStatsUI");
            statsObj.AddComponent<MatchEndStatsUI>();
        }

        // 6. التأكد من وجود وجاهزية مدير نقاط التفريخ والتمركز التكتيكي
        if (SpawnManager.Instance == null)
        {
            SpawnManager sm = FindAnyObjectByType<SpawnManager>();
            if (sm == null)
            {
                GameObject smObj = new GameObject("SpawnManager");
                sm = smObj.AddComponent<SpawnManager>();
            }
            SpawnManager.Instance = sm;
        }
        SpawnManager.Instance.AutoFindSpawns();

        // 7. التأكد من وجود وجاهزية مدير البوتات بالبيانات اللازمة
        if (BotManager.Instance == null)
        {
            BotManager bm = FindAnyObjectByType<BotManager>();
            if (bm == null)
            {
                GameObject bmObj = new GameObject("BotManager");
                bm = bmObj.AddComponent<BotManager>();
                bm.botPrefab = Resources.Load<GameObject>("Prefabs/Bot");
                bm.characterDatabase = Resources.Load<CharacterDatabase>("Characters/CharacterDatabase");
            }
            BotManager.Instance = bm;
        }

        // 8. التأكد من وجود وجاهزية مدير حالة المباراة
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetStateForNewMatch();
        }
    }

    void Awake()
    {
        if (!Application.isPlaying) return;
        Instance = this;
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        if (!hasDecorated)
        {
            hasDecorated = true;
            ApplyFullNatureMakeover();
        }
    }

    [ContextMenu("Re-Decorate Forest")]
    public void ApplyFullNatureMakeover()
    {
        if (!Application.isPlaying) return;

        decorationsRoot = GameObject.Find("EnchantedForestDecorations");
        if (decorationsRoot != null)
        {
            Destroy(decorationsRoot);
        }

        decorationsRoot = new GameObject("EnchantedForestDecorations");

        CreateForestMaterials();
        ReskinSceneMeshes();
        BuildDiverseFairytaleForestRing();
        BuildMajesticMountainRing();
        BuildMajesticWaterfall();
        BuildEnchantedBoundaryRiverRing();
        BuildMajesticBraidedBeanstalkTrunk();
        DecorateTreeCanopyLevels();
        TuneLightingAndAtmosphere();

        // تأمين إخفاء ممرات الصعود العلوية في البداية مع إبقاء جذور غيث الأصلية مفتوحة
        SpiralConnector sc = FindAnyObjectByType<SpiralConnector>();
        if (sc != null)
        {
            sc.SetSectionUnlocked(0, true);
            sc.SetSectionUnlocked(1, false);
            sc.SetSectionUnlocked(2, false);
            sc.SetSectionUnlocked(3, false);
        }

        Debug.Log("🌿 [EnchantedForestDecorator] Clean, high-performance nature environment active!");
    }

    void CreateForestMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Grass
        grassMaterial = new Material(litShader);
        grassMaterial.name = "ForestGrass_Runtime";
        grassMaterial.mainTexture = GenerateNoiseTexture(256, 256, grassColorA, grassColorB, 18f, true);
        grassMaterial.mainTextureScale = new Vector2(30f, 30f);
        if (grassMaterial.HasProperty("_Smoothness")) grassMaterial.SetFloat("_Smoothness", 0.15f);

        // Bark
        barkMaterial = new Material(litShader);
        barkMaterial.name = "AncientTreeBark_Runtime";
        barkMaterial.mainTexture = GenerateBarkTexture(256, 256, barkColorA, barkColorB);
        barkMaterial.mainTextureScale = new Vector2(3f, 14f);
        if (barkMaterial.HasProperty("_Smoothness")) barkMaterial.SetFloat("_Smoothness", 0.08f);
        if (barkMaterial.HasProperty("_EmissionColor"))
        {
            barkMaterial.EnableKeyword("_EMISSION");
            barkMaterial.SetColor("_EmissionColor", new Color(0.24f, 0.15f, 0.08f) * 0.35f);
        }

        // Canopy
        canopyMaterial = new Material(litShader);
        canopyMaterial.name = "CanopyFoliage_Runtime";
        canopyMaterial.mainTexture = GenerateNoiseTexture(256, 256, canopyColor * 0.85f, canopyColor * 1.25f, 12f, false);
        canopyMaterial.mainTextureScale = new Vector2(4f, 4f);
        if (canopyMaterial.HasProperty("_Smoothness")) canopyMaterial.SetFloat("_Smoothness", 0.2f);

        // Mossy Stone
        stoneMaterial = new Material(litShader);
        stoneMaterial.name = "ForestStone_Runtime";
        stoneMaterial.color = new Color(0.42f, 0.48f, 0.40f);
        stoneMaterial.mainTexture = GenerateNoiseTexture(128, 128, new Color(0.38f, 0.42f, 0.36f), new Color(0.25f, 0.42f, 0.20f), 8f, false);
        if (stoneMaterial.HasProperty("_Smoothness")) stoneMaterial.SetFloat("_Smoothness", 0.12f);

        // Water
        Shader waterShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        waterMaterial = new Material(waterShader);
        waterMaterial.name = "ForestWater_Runtime";
        waterMaterial.color = waterColor;
        if (waterMaterial.HasProperty("_Smoothness")) waterMaterial.SetFloat("_Smoothness", 0.95f);
        if (waterMaterial.HasProperty("_Metallic")) waterMaterial.SetFloat("_Metallic", 0.1f);
        if (waterMaterial.HasProperty("_EmissionColor"))
        {
            waterMaterial.EnableKeyword("_EMISSION");
            waterMaterial.SetColor("_EmissionColor", new Color(0.05f, 0.35f, 0.45f) * 0.4f);
        }

        // Mushrooms
        mushroomRedMaterial = new Material(litShader);
        mushroomRedMaterial.color = new Color(0.95f, 0.18f, 0.22f);
        if (mushroomRedMaterial.HasProperty("_EmissionColor"))
        {
            mushroomRedMaterial.EnableKeyword("_EMISSION");
            mushroomRedMaterial.SetColor("_EmissionColor", new Color(0.8f, 0.1f, 0.15f) * 0.5f);
        }

        mushroomBlueMaterial = new Material(litShader);
        mushroomBlueMaterial.color = new Color(0.15f, 0.75f, 1.0f);
        if (mushroomBlueMaterial.HasProperty("_EmissionColor"))
        {
            mushroomBlueMaterial.EnableKeyword("_EMISSION");
            mushroomBlueMaterial.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 0.95f) * 0.8f);
        }

        // Rich Warm Wood Bark for trunk boughs & support arches (Never black under shadows!)
        darkBarkMaterial = new Material(litShader);
        darkBarkMaterial.name = "DarkBark_Runtime";
        darkBarkMaterial.mainTexture = barkMaterial.mainTexture;
        darkBarkMaterial.mainTextureScale = barkMaterial.mainTextureScale;
        darkBarkMaterial.color = new Color(0.48f, 0.32f, 0.18f); // Warm rich cedar/oak wood brown
        if (darkBarkMaterial.HasProperty("_Smoothness")) darkBarkMaterial.SetFloat("_Smoothness", 0.12f);
        if (darkBarkMaterial.HasProperty("_EmissionColor"))
        {
            darkBarkMaterial.EnableKeyword("_EMISSION");
            // Soft warm ambient bounce tint to guarantee it never renders pitch-black under shadows!
            darkBarkMaterial.SetColor("_EmissionColor", new Color(0.25f, 0.15f, 0.08f) * 0.50f);
        }

        // Lush Foliage for canopy cushions and hanging vines
        lushCanopyMaterial = new Material(litShader);
        lushCanopyMaterial.name = "LushCanopy_Runtime";
        lushCanopyMaterial.mainTexture = canopyMaterial.mainTexture;
        lushCanopyMaterial.mainTextureScale = canopyMaterial.mainTextureScale;
        lushCanopyMaterial.color = new Color(0.22f, 0.72f, 0.32f);
        if (lushCanopyMaterial.HasProperty("_Smoothness")) lushCanopyMaterial.SetFloat("_Smoothness", 0.20f);

        // Bright Foliage for branch tips and sprouting leaf blades
        brightCanopyMaterial = new Material(litShader);
        brightCanopyMaterial.name = "BrightCanopy_Runtime";
        brightCanopyMaterial.mainTexture = canopyMaterial.mainTexture;
        brightCanopyMaterial.mainTextureScale = canopyMaterial.mainTextureScale;
        brightCanopyMaterial.color = new Color(0.32f, 0.84f, 0.38f);
        if (brightCanopyMaterial.HasProperty("_Smoothness")) brightCanopyMaterial.SetFloat("_Smoothness", 0.22f);

        // Magical Hollow Glow Material for ancient tree cavities
        hollowGlowMaterial = new Material(litShader);
        hollowGlowMaterial.name = "HollowGlow_Runtime";
        hollowGlowMaterial.color = new Color(0.3f, 0.95f, 0.75f, 0.9f);
        if (hollowGlowMaterial.HasProperty("_EmissionColor"))
        {
            hollowGlowMaterial.EnableKeyword("_EMISSION");
            hollowGlowMaterial.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 0.65f) * 1.5f);
        }

        // Luminous Celestial Petal Material for Grand Blooming Lotus at Top Level
        celestialPetalMaterial = new Material(litShader);
        celestialPetalMaterial.name = "CelestialPetal_Runtime";
        celestialPetalMaterial.color = new Color(0.28f, 0.92f, 0.76f); // Luminous turquoise emerald
        if (celestialPetalMaterial.HasProperty("_Smoothness")) celestialPetalMaterial.SetFloat("_Smoothness", 0.35f);
        if (celestialPetalMaterial.HasProperty("_EmissionColor"))
        {
            celestialPetalMaterial.EnableKeyword("_EMISSION");
            celestialPetalMaterial.SetColor("_EmissionColor", new Color(0.16f, 0.78f, 0.65f) * 0.85f);
        }

        // Golden Pollen Material for Flower Stamens
        goldenPollenMaterial = new Material(litShader);
        goldenPollenMaterial.name = "GoldenPollen_Runtime";
        goldenPollenMaterial.color = new Color(1.0f, 0.86f, 0.25f);
        if (goldenPollenMaterial.HasProperty("_EmissionColor"))
        {
            goldenPollenMaterial.EnableKeyword("_EMISSION");
            goldenPollenMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.78f, 0.15f) * 1.5f);
        }

        // Autumn Amber Leaves for Floor Scatter
        autumnLeafMaterial = new Material(litShader);
        autumnLeafMaterial.name = "AutumnLeaf_Runtime";
        autumnLeafMaterial.color = new Color(0.82f, 0.52f, 0.16f);
        if (autumnLeafMaterial.HasProperty("_Smoothness")) autumnLeafMaterial.SetFloat("_Smoothness", 0.15f);

        // Mountain Rock Granite & Slate (Warm natural granite with ambient fill - NEVER black!)
        mountainRockMat = new Material(litShader);
        mountainRockMat.name = "MountainRock_Runtime";
        mountainRockMat.mainTexture = stoneMaterial.mainTexture;
        mountainRockMat.mainTextureScale = new Vector2(6f, 6f);
        Color rockCol = new Color(0.65f, 0.62f, 0.58f);
        mountainRockMat.color = rockCol;
        if (mountainRockMat.HasProperty("_BaseColor")) mountainRockMat.SetColor("_BaseColor", rockCol);
        if (mountainRockMat.HasProperty("_Color")) mountainRockMat.SetColor("_Color", rockCol);
        if (mountainRockMat.HasProperty("_Smoothness")) mountainRockMat.SetFloat("_Smoothness", 0.14f);
        if (mountainRockMat.HasProperty("_EmissionColor"))
        {
            mountainRockMat.EnableKeyword("_EMISSION");
            mountainRockMat.SetColor("_EmissionColor", new Color(0.42f, 0.38f, 0.34f) * 0.50f); // Warm natural granite fill
        }

        // Alpine Snow & Frost Peaks (Brilliant pure white with glowing snow crystals)
        mountainPeakSnowMat = new Material(litShader);
        mountainPeakSnowMat.name = "MountainPeakSnow_Runtime";
        Color snowCol = new Color(0.98f, 0.99f, 1.0f);
        mountainPeakSnowMat.color = snowCol;
        if (mountainPeakSnowMat.HasProperty("_BaseColor")) mountainPeakSnowMat.SetColor("_BaseColor", snowCol);
        if (mountainPeakSnowMat.HasProperty("_Color")) mountainPeakSnowMat.SetColor("_Color", snowCol);
        if (mountainPeakSnowMat.HasProperty("_Smoothness")) mountainPeakSnowMat.SetFloat("_Smoothness", 0.65f);
        if (mountainPeakSnowMat.HasProperty("_EmissionColor"))
        {
            mountainPeakSnowMat.EnableKeyword("_EMISSION");
            mountainPeakSnowMat.SetColor("_EmissionColor", new Color(0.95f, 0.98f, 1.0f) * 0.95f); // Brilliant alpine snow glow
        }

        // Dreamy Pink Sakura / Cherry Blossom Foliage
        pinkSakuraMat = new Material(litShader);
        pinkSakuraMat.name = "PinkSakuraFoliage_Runtime";
        pinkSakuraMat.mainTexture = canopyMaterial.mainTexture;
        Color sakuraCol = new Color(1.0f, 0.68f, 0.82f);
        pinkSakuraMat.color = sakuraCol;
        if (pinkSakuraMat.HasProperty("_BaseColor")) pinkSakuraMat.SetColor("_BaseColor", sakuraCol);
        if (pinkSakuraMat.HasProperty("_Color")) pinkSakuraMat.SetColor("_Color", sakuraCol);
        if (pinkSakuraMat.HasProperty("_Smoothness")) pinkSakuraMat.SetFloat("_Smoothness", 0.25f);
        if (pinkSakuraMat.HasProperty("_EmissionColor"))
        {
            pinkSakuraMat.EnableKeyword("_EMISSION");
            pinkSakuraMat.SetColor("_EmissionColor", new Color(1.0f, 0.60f, 0.76f) * 0.38f);
        }

        // Golden Autumn Foliage
        goldenAutumnMat = new Material(litShader);
        goldenAutumnMat.name = "GoldenAutumnFoliage_Runtime";
        goldenAutumnMat.mainTexture = canopyMaterial.mainTexture;
        Color goldCol = new Color(1.0f, 0.76f, 0.18f);
        goldenAutumnMat.color = goldCol;
        if (goldenAutumnMat.HasProperty("_BaseColor")) goldenAutumnMat.SetColor("_BaseColor", goldCol);
        if (goldenAutumnMat.HasProperty("_Color")) goldenAutumnMat.SetColor("_Color", goldCol);
        if (goldenAutumnMat.HasProperty("_Smoothness")) goldenAutumnMat.SetFloat("_Smoothness", 0.22f);
        if (goldenAutumnMat.HasProperty("_EmissionColor"))
        {
            goldenAutumnMat.EnableKeyword("_EMISSION");
            goldenAutumnMat.SetColor("_EmissionColor", new Color(1.0f, 0.70f, 0.14f) * 0.38f);
        }

        // Crimson Scarlet Maple Foliage
        crimsonMapleMat = new Material(litShader);
        crimsonMapleMat.name = "CrimsonMapleFoliage_Runtime";
        crimsonMapleMat.mainTexture = canopyMaterial.mainTexture;
        Color crimsonCol = new Color(0.90f, 0.20f, 0.22f);
        crimsonMapleMat.color = crimsonCol;
        if (crimsonMapleMat.HasProperty("_BaseColor")) crimsonMapleMat.SetColor("_BaseColor", crimsonCol);
        if (crimsonMapleMat.HasProperty("_Color")) crimsonMapleMat.SetColor("_Color", crimsonCol);
        if (crimsonMapleMat.HasProperty("_Smoothness")) crimsonMapleMat.SetFloat("_Smoothness", 0.20f);
        if (crimsonMapleMat.HasProperty("_EmissionColor"))
        {
            crimsonMapleMat.EnableKeyword("_EMISSION");
            crimsonMapleMat.SetColor("_EmissionColor", new Color(0.80f, 0.14f, 0.16f) * 0.35f);
        }

        // Vibrant Spring Woodland Oak Foliage
        vibrantSpringOakMat = new Material(litShader);
        vibrantSpringOakMat.name = "SpringOakFoliage_Runtime";
        vibrantSpringOakMat.mainTexture = canopyMaterial.mainTexture;
        Color springCol = new Color(0.24f, 0.82f, 0.32f);
        vibrantSpringOakMat.color = springCol;
        if (vibrantSpringOakMat.HasProperty("_BaseColor")) vibrantSpringOakMat.SetColor("_BaseColor", springCol);
        if (vibrantSpringOakMat.HasProperty("_Color")) vibrantSpringOakMat.SetColor("_Color", springCol);
        if (vibrantSpringOakMat.HasProperty("_Smoothness")) vibrantSpringOakMat.SetFloat("_Smoothness", 0.22f);

        // Subterranean River Grotto Interior Dark Void
        riverGrottoDarkMat = new Material(litShader);
        riverGrottoDarkMat.name = "RiverGrottoDark_Runtime";
        Color darkCaveCol = new Color(0.06f, 0.06f, 0.08f);
        riverGrottoDarkMat.color = darkCaveCol;
        if (riverGrottoDarkMat.HasProperty("_BaseColor")) riverGrottoDarkMat.SetColor("_BaseColor", darkCaveCol);
        if (riverGrottoDarkMat.HasProperty("_Color")) riverGrottoDarkMat.SetColor("_Color", darkCaveCol);

        // Silver-White Birch Bark Material
        birchBarkMat = new Material(litShader);
        birchBarkMat.name = "BirchBark_Runtime";
        birchBarkMat.color = new Color(0.96f, 0.96f, 0.93f);
        if (birchBarkMat.HasProperty("_BaseColor")) birchBarkMat.SetColor("_BaseColor", birchBarkMat.color);
        if (birchBarkMat.HasProperty("_Color")) birchBarkMat.SetColor("_Color", birchBarkMat.color);

        // Dark Pine Needle Foliage
        darkPineMat = new Material(litShader);
        darkPineMat.name = "DarkPineFoliage_Runtime";
        darkPineMat.mainTexture = canopyMaterial.mainTexture;
        darkPineMat.mainTextureScale = new Vector2(5f, 5f);
        darkPineMat.color = new Color(0.12f, 0.42f, 0.20f);
        if (darkPineMat.HasProperty("_BaseColor")) darkPineMat.SetColor("_BaseColor", darkPineMat.color);
        if (darkPineMat.HasProperty("_Color")) darkPineMat.SetColor("_Color", darkPineMat.color);
        if (darkPineMat.HasProperty("_Smoothness")) darkPineMat.SetFloat("_Smoothness", 0.14f);

        // Emerald Spruce Foliage
        spruceMat = new Material(litShader);
        spruceMat.name = "SpruceFoliage_Runtime";
        spruceMat.mainTexture = canopyMaterial.mainTexture;
        spruceMat.mainTextureScale = new Vector2(4f, 4f);
        spruceMat.color = new Color(0.14f, 0.50f, 0.22f);
        if (spruceMat.HasProperty("_BaseColor")) spruceMat.SetColor("_BaseColor", spruceMat.color);
        if (spruceMat.HasProperty("_Color")) spruceMat.SetColor("_Color", spruceMat.color);
        if (spruceMat.HasProperty("_Smoothness")) spruceMat.SetFloat("_Smoothness", 0.16f);

        // Dark Woodland Oak Foliage
        darkOakMat = new Material(litShader);
        darkOakMat.name = "DarkOakFoliage_Runtime";
        darkOakMat.mainTexture = canopyMaterial.mainTexture;
        darkOakMat.mainTextureScale = new Vector2(4f, 4f);
        darkOakMat.color = new Color(0.16f, 0.55f, 0.22f);
        if (darkOakMat.HasProperty("_BaseColor")) darkOakMat.SetColor("_BaseColor", darkOakMat.color);
        if (darkOakMat.HasProperty("_Color")) darkOakMat.SetColor("_Color", darkOakMat.color);
        if (darkOakMat.HasProperty("_Smoothness")) darkOakMat.SetFloat("_Smoothness", 0.18f);

        // Highland Cedar / Cypress Foliage
        highlandCedarMat = new Material(litShader);
        highlandCedarMat.name = "HighlandCedarFoliage_Runtime";
        highlandCedarMat.mainTexture = canopyMaterial.mainTexture;
        highlandCedarMat.mainTextureScale = new Vector2(3f, 8f);
        highlandCedarMat.color = new Color(0.12f, 0.44f, 0.25f);
        if (highlandCedarMat.HasProperty("_BaseColor")) highlandCedarMat.SetColor("_BaseColor", highlandCedarMat.color);
        if (highlandCedarMat.HasProperty("_Color")) highlandCedarMat.SetColor("_Color", highlandCedarMat.color);
        if (highlandCedarMat.HasProperty("_Smoothness")) highlandCedarMat.SetFloat("_Smoothness", 0.15f);

        // Shared Water Streams & Rivers
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Texture2D streamTex = GenerateWaterfallStreamTexture(128, 256);
        sharedWaterStreamMat = new Material(unlitShader);
        sharedWaterStreamMat.name = "SharedWaterStream_Runtime";
        sharedWaterStreamMat.color = new Color(0.70f, 0.92f, 1.0f);
        sharedWaterStreamMat.mainTexture = streamTex;
        sharedWaterStreamMat.mainTextureScale = new Vector2(1f, 3.5f);
        if (sharedWaterStreamMat.HasProperty("_BaseColor")) sharedWaterStreamMat.SetColor("_BaseColor", sharedWaterStreamMat.color);
        if (sharedWaterStreamMat.HasProperty("_BaseMap")) sharedWaterStreamMat.SetTexture("_BaseMap", streamTex);

        // Shared Foam & Pool
        sharedFoamMat = new Material(unlitShader);
        sharedFoamMat.name = "SharedFoam_Runtime";
        Color sFoam = new Color(0.96f, 0.98f, 1.0f);
        sharedFoamMat.color = sFoam;
        if (sharedFoamMat.HasProperty("_BaseColor")) sharedFoamMat.SetColor("_BaseColor", sFoam);

        sharedPoolMat = new Material(unlitShader);
        sharedPoolMat.name = "SharedPool_Runtime";
        Color sPool = new Color(0.25f, 0.72f, 0.94f, 0.92f);
        sharedPoolMat.color = sPool;
        if (sharedPoolMat.HasProperty("_BaseColor")) sharedPoolMat.SetColor("_BaseColor", sPool);

        // -------------------------------------------------------------
        // Multi-Tonal Braided Trunk Materials (تنوع لوني جمالي ساحر للجذوع)
        // -------------------------------------------------------------
        // 1. Deep Dark Ancient Oak (بني داكن عميق)
        trunkDeepOakMat = new Material(litShader);
        trunkDeepOakMat.name = "TrunkDeepOak_Runtime";
        Color cDeepOak = new Color(0.28f, 0.16f, 0.08f);
        trunkDeepOakMat.color = cDeepOak;
        trunkDeepOakMat.mainTexture = GenerateBarkTexture(256, 256, new Color(0.20f, 0.11f, 0.05f), new Color(0.34f, 0.20f, 0.11f));
        trunkDeepOakMat.mainTextureScale = new Vector2(3f, 14f);
        if (trunkDeepOakMat.HasProperty("_BaseColor")) trunkDeepOakMat.SetColor("_BaseColor", cDeepOak);
        if (trunkDeepOakMat.HasProperty("_Color")) trunkDeepOakMat.SetColor("_Color", cDeepOak);
        if (trunkDeepOakMat.HasProperty("_Smoothness")) trunkDeepOakMat.SetFloat("_Smoothness", 0.08f);
        if (trunkDeepOakMat.HasProperty("_EmissionColor"))
        {
            trunkDeepOakMat.EnableKeyword("_EMISSION");
            trunkDeepOakMat.SetColor("_EmissionColor", new Color(0.18f, 0.10f, 0.05f) * 0.35f);
        }

        // 2. Warm Sunlit Cedar (بني فاتح مائل للعسلي/الدفء)
        trunkWarmAmberCedarMat = new Material(litShader);
        trunkWarmAmberCedarMat.name = "TrunkWarmAmberCedar_Runtime";
        Color cAmberCedar = new Color(0.60f, 0.38f, 0.19f);
        trunkWarmAmberCedarMat.color = cAmberCedar;
        trunkWarmAmberCedarMat.mainTexture = GenerateBarkTexture(256, 256, new Color(0.52f, 0.32f, 0.15f), new Color(0.66f, 0.44f, 0.24f));
        trunkWarmAmberCedarMat.mainTextureScale = new Vector2(3f, 14f);
        if (trunkWarmAmberCedarMat.HasProperty("_BaseColor")) trunkWarmAmberCedarMat.SetColor("_BaseColor", cAmberCedar);
        if (trunkWarmAmberCedarMat.HasProperty("_Color")) trunkWarmAmberCedarMat.SetColor("_Color", cAmberCedar);
        if (trunkWarmAmberCedarMat.HasProperty("_Smoothness")) trunkWarmAmberCedarMat.SetFloat("_Smoothness", 0.12f);
        if (trunkWarmAmberCedarMat.HasProperty("_EmissionColor"))
        {
            trunkWarmAmberCedarMat.EnableKeyword("_EMISSION");
            trunkWarmAmberCedarMat.SetColor("_EmissionColor", new Color(0.28f, 0.18f, 0.09f) * 0.40f);
        }

        // 3. Verdant Mossy Olive Bark (جذع غابي مائل للاخضرار الطحلبي)
        trunkMossyBarkMat = new Material(litShader);
        trunkMossyBarkMat.name = "TrunkMossyBark_Runtime";
        Color cMossy = new Color(0.35f, 0.46f, 0.22f);
        trunkMossyBarkMat.color = cMossy;
        trunkMossyBarkMat.mainTexture = GenerateBarkTexture(256, 256, new Color(0.28f, 0.38f, 0.17f), new Color(0.42f, 0.54f, 0.27f));
        trunkMossyBarkMat.mainTextureScale = new Vector2(3f, 14f);
        if (trunkMossyBarkMat.HasProperty("_BaseColor")) trunkMossyBarkMat.SetColor("_BaseColor", cMossy);
        if (trunkMossyBarkMat.HasProperty("_Color")) trunkMossyBarkMat.SetColor("_Color", cMossy);
        if (trunkMossyBarkMat.HasProperty("_Smoothness")) trunkMossyBarkMat.SetFloat("_Smoothness", 0.10f);
        if (trunkMossyBarkMat.HasProperty("_EmissionColor"))
        {
            trunkMossyBarkMat.EnableKeyword("_EMISSION");
            trunkMossyBarkMat.SetColor("_EmissionColor", new Color(0.18f, 0.26f, 0.11f) * 0.40f);
        }

        // 4. Luminous Living Emerald Vine (عرق لبلاب زمردي حي ومتوهج برقة)
        trunkEmeraldSproutVineMat = new Material(litShader);
        trunkEmeraldSproutVineMat.name = "TrunkEmeraldSproutVine_Runtime";
        Color cEmerald = new Color(0.24f, 0.64f, 0.28f);
        trunkEmeraldSproutVineMat.color = cEmerald;
        trunkEmeraldSproutVineMat.mainTexture = GenerateNoiseTexture(256, 256, new Color(0.18f, 0.52f, 0.22f), new Color(0.32f, 0.72f, 0.35f), 12f, false);
        trunkEmeraldSproutVineMat.mainTextureScale = new Vector2(3f, 14f);
        if (trunkEmeraldSproutVineMat.HasProperty("_BaseColor")) trunkEmeraldSproutVineMat.SetColor("_BaseColor", cEmerald);
        if (trunkEmeraldSproutVineMat.HasProperty("_Color")) trunkEmeraldSproutVineMat.SetColor("_Color", cEmerald);
        if (trunkEmeraldSproutVineMat.HasProperty("_Smoothness")) trunkEmeraldSproutVineMat.SetFloat("_Smoothness", 0.22f);
        if (trunkEmeraldSproutVineMat.HasProperty("_EmissionColor"))
        {
            trunkEmeraldSproutVineMat.EnableKeyword("_EMISSION");
            trunkEmeraldSproutVineMat.SetColor("_EmissionColor", new Color(0.16f, 0.42f, 0.18f) * 0.50f);
        }

        // 5. Golden Chestnut Honey Wood (خشب كستنائي عسلي دافئ ومشرق)
        trunkGoldenChestnutMat = new Material(litShader);
        trunkGoldenChestnutMat.name = "TrunkGoldenChestnut_Runtime";
        Color cChestnut = new Color(0.52f, 0.35f, 0.16f);
        trunkGoldenChestnutMat.color = cChestnut;
        trunkGoldenChestnutMat.mainTexture = GenerateBarkTexture(256, 256, new Color(0.44f, 0.29f, 0.12f), new Color(0.58f, 0.40f, 0.21f));
        trunkGoldenChestnutMat.mainTextureScale = new Vector2(3f, 14f);
        if (trunkGoldenChestnutMat.HasProperty("_BaseColor")) trunkGoldenChestnutMat.SetColor("_BaseColor", cChestnut);
        if (trunkGoldenChestnutMat.HasProperty("_Color")) trunkGoldenChestnutMat.SetColor("_Color", cChestnut);
        if (trunkGoldenChestnutMat.HasProperty("_Smoothness")) trunkGoldenChestnutMat.SetFloat("_Smoothness", 0.11f);
        if (trunkGoldenChestnutMat.HasProperty("_EmissionColor"))
        {
            trunkGoldenChestnutMat.EnableKeyword("_EMISSION");
            trunkGoldenChestnutMat.SetColor("_EmissionColor", new Color(0.25f, 0.16f, 0.08f) * 0.38f);
        }

        // 6. Ancient Sage Lichen Wood (لحاء عتيق مكسو بأشنات خضراء مائلة للرمادي الطبيعي)
        trunkLichenVerdantMat = new Material(litShader);
        trunkLichenVerdantMat.name = "TrunkLichenVerdant_Runtime";
        Color cLichen = new Color(0.38f, 0.48f, 0.35f);
        trunkLichenVerdantMat.color = cLichen;
        trunkLichenVerdantMat.mainTexture = GenerateBarkTexture(256, 256, new Color(0.32f, 0.42f, 0.29f), new Color(0.45f, 0.55f, 0.41f));
        trunkLichenVerdantMat.mainTextureScale = new Vector2(3f, 14f);
        if (trunkLichenVerdantMat.HasProperty("_BaseColor")) trunkLichenVerdantMat.SetColor("_BaseColor", cLichen);
        if (trunkLichenVerdantMat.HasProperty("_Color")) trunkLichenVerdantMat.SetColor("_Color", cLichen);
        if (trunkLichenVerdantMat.HasProperty("_Smoothness")) trunkLichenVerdantMat.SetFloat("_Smoothness", 0.14f);
        if (trunkLichenVerdantMat.HasProperty("_EmissionColor"))
        {
            trunkLichenVerdantMat.EnableKeyword("_EMISSION");
            trunkLichenVerdantMat.SetColor("_EmissionColor", new Color(0.20f, 0.28f, 0.20f) * 0.40f);
        }
    }

    public Material GetVineColumnMaterial(int v)
    {
        switch (v % 6)
        {
            case 0: return trunkDeepOakMat ?? barkMaterial;
            case 1: return trunkWarmAmberCedarMat ?? barkMaterial;
            case 2: return trunkMossyBarkMat ?? barkMaterial;
            case 3: return trunkEmeraldSproutVineMat ?? barkMaterial;
            case 4: return trunkGoldenChestnutMat ?? barkMaterial;
            case 5: return trunkLichenVerdantMat ?? barkMaterial;
            default: return barkMaterial;
        }
    }

    public Material GetCoreSegmentMaterial(int s)
    {
        int mod = s % 3;
        if (mod == 0) return trunkDeepOakMat ?? barkMaterial;
        if (mod == 1) return trunkWarmAmberCedarMat ?? barkMaterial;
        return trunkMossyBarkMat ?? barkMaterial;
    }

    void ReskinSceneMeshes()
    {
        // حذف الصندوق الأسود والكرات الصخرية ودائرة المياه التائهة نهائياً من أي مكان
        foreach (string strayName in new string[] { "RiverSinkGrotto", "CavernDepthVoid", "GrottoDrainWater" })
        {
            foreach (GameObject stray in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (stray != null && stray.name.Equals(strayName, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (Application.isPlaying) Destroy(stray);
                    else DestroyImmediate(stray);
                }
            }
        }

        // Ground Plane: Expand to vast 650m x 650m verdant meadow reaching under and past the boundary river
        // IMPORTANT: Specifically target "Plane" directly (never use FindWithTag("Ground") because platforms/roots also have Ground tag!)
        GameObject groundObj = GameObject.Find("Plane");
        if (groundObj == null) groundObj = GameObject.Find("Ground");
        if (groundObj == null)
        {
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("Ground"))
            {
                if (g.name.Equals("Plane", System.StringComparison.OrdinalIgnoreCase) ||
                    g.name.Equals("Ground", System.StringComparison.OrdinalIgnoreCase))
                {
                    groundObj = g;
                    break;
                }
            }
        }

        if (groundObj != null)
        {
            groundObj.transform.position = Vector3.zero;
            groundObj.transform.localScale = new Vector3(65f, 1f, 65f); // 650m x 650m (extends 325m radius in all directions, well into and under the mountains!)
            MeshRenderer mr = groundObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = grassMaterial;

            MeshCollider mc = groundObj.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.enabled = false;
                mc.enabled = true; // Refresh collider bounds
            }

            // Solid 4-meter thick physics foundation under the ground plane to prevent falling into the void
            BoxCollider bc = groundObj.GetComponent<BoxCollider>();
            if (bc == null) bc = groundObj.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, -2.0f, 0f);
            bc.size = new Vector3(10f, 4.0f, 10f);
        }
        else
        {
            // Fallback: create the vast ground plane if missing
            groundObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundObj.name = "Plane";
            groundObj.tag = "Ground";
            int gLayer = LayerMask.NameToLayer("Ground");
            groundObj.layer = gLayer >= 0 ? gLayer : 8;
            groundObj.transform.position = Vector3.zero;
            groundObj.transform.localScale = new Vector3(65f, 1f, 65f);
            MeshRenderer mr = groundObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = grassMaterial;

            BoxCollider bc = groundObj.GetComponent<BoxCollider>();
            if (bc == null) bc = groundObj.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, -2.0f, 0f);
            bc.size = new Vector3(10f, 4.0f, 10f);
        }

        // Tree Trunk: إخفاء الأسطوانة البدائية مع الحفاظ على الكوليدر والفيزياء لتظهر الجذوع المجدولة الفخمة بدلاً منها
        GameObject treeObj = GameObject.FindWithTag("Tree");
        if (treeObj == null) treeObj = GameObject.Find("Tree");
        if (treeObj != null)
        {
            MeshRenderer mr = treeObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = false;
            }
        }

        // Roots: قوائم وجذور غيث الأصلية الأربعة للصعود إلى المستوى 1 - تفعيلها وإكساؤها بخشب دافئ وصلب
        GameObject rootsContainer = SpiralConnector.FindRootsObject();
        if (rootsContainer != null)
        {
            rootsContainer.SetActive(true);
            EnhanceOriginalPlayerRoots(rootsContainer);
        }

        // Tree Levels
        GameObject levelsContainer = GameObject.Find("TreeLevels");
        if (levelsContainer != null)
        {
            foreach (MeshRenderer r in levelsContainer.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.material = canopyMaterial;
            }
        }

        // Paths: الحفاظ على مواد الممرات الشجرية النباتية الحية من SpiralConnector بدون استبدالها بالحجر
        GameObject pathsContainer = GameObject.Find("Paths");
        if (pathsContainer != null)
        {
            // نترك خامات عروش الممرات الحية كما تم توليدها لإظهار تدرجات الخشب واللحاء النباتي الساحر
        }
    }

    // =========================================================================
    // MAJESTIC NATURAL WATERFALL SYSTEM (Natural Boulders & Multi-Tier Cascades)
    // =========================================================================
    void BuildMajesticWaterfall()
    {
        GameObject waterfallObj = new GameObject("MajesticNaturalWaterfall");
        waterfallObj.transform.SetParent(decorationsRoot.transform);
        // Placed far away on the western alpine mountain range (R = 160m)
        waterfallObj.transform.position = new Vector3(-135f, 0f, -85f);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");

        // --- MATERIALS ---
        Material rockMat = mountainRockMat;

        // Procedural scrolling streaming water texture
        Texture2D waterStreamTex = GenerateWaterfallStreamTexture(128, 256);

        Material waterStreamMat = new Material(unlitShader);
        waterStreamMat.color = new Color(0.7f, 0.92f, 1.0f);
        waterStreamMat.mainTexture = waterStreamTex;
        waterStreamMat.mainTextureScale = new Vector2(1f, 3.5f);
        if (waterStreamMat.HasProperty("_BaseColor")) waterStreamMat.SetColor("_BaseColor", waterStreamMat.color);
        if (waterStreamMat.HasProperty("_BaseMap")) waterStreamMat.SetTexture("_BaseMap", waterStreamTex);

        // White foam
        Material foamMat = new Material(unlitShader);
        Color foamCol = new Color(0.96f, 0.98f, 1.0f);
        foamMat.color = foamCol;
        if (foamMat.HasProperty("_BaseColor")) foamMat.SetColor("_BaseColor", foamCol);

        // Dark forest moss
        Material mossMat = new Material(litShader);
        mossMat.color = new Color(0.22f, 0.52f, 0.18f);

        // Lagoon pool water
        Material poolMat = new Material(unlitShader);
        Color poolCol = new Color(0.25f, 0.72f, 0.94f, 0.92f);
        poolMat.color = poolCol;
        if (poolMat.HasProperty("_BaseColor")) poolMat.SetColor("_BaseColor", poolCol);

        // Try loading the sculpted Blender 3D Waterfall Model from Resources
        GameObject blenderModelPrefab = Resources.Load<GameObject>("Models/Waterfall_Mountain");
        if (blenderModelPrefab != null)
        {
            GameObject modelInstance = Instantiate(blenderModelPrefab, waterfallObj.transform, false);
            modelInstance.name = "Blender_Waterfall_Mountain";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            List<Renderer> bVerticalRenderers = new List<Renderer>();
            List<Renderer> bRiverRenderers = new List<Renderer>();
            List<Transform> bFoamPuffs = new List<Transform>();
            Transform bPoolTrans = null;

            foreach (Renderer rend in modelInstance.GetComponentsInChildren<Renderer>(true))
            {
                string rName = rend.gameObject.name;
                if (rName.Contains("Waterfall_Curtain"))
                {
                    rend.material = waterStreamMat;
                    bVerticalRenderers.Add(rend);
                }
                else if (rName.Contains("River_Surface"))
                {
                    rend.material = waterStreamMat;
                    bRiverRenderers.Add(rend);
                }
                else if (rName.Contains("Plunge_Pool"))
                {
                    rend.material = poolMat;
                    bPoolTrans = rend.transform;
                }
                else
                {
                    // إخفاء الصخر العملاق الذي كان يغطي الشلال ليبقى الشلال متدفقاً بوضوح ونقاء
                    rend.gameObject.SetActive(false);
                }

                Collider c = rend.GetComponent<Collider>();
                if (c != null) DestroyCollider(rend.gameObject);
            }

            // Impact foam puffs where the water lands
            for (int i = 0; i < 14; i++)
            {
                GameObject foam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foam.name = $"BlenderFoam_{i}";
                foam.transform.SetParent(waterfallObj.transform, false);
                foam.transform.localPosition = new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(0.2f, 0.8f), Random.Range(3.0f, 5.0f));
                foam.transform.localScale = Vector3.one * Random.Range(0.5f, 1.1f);
                foam.GetComponent<Renderer>().material = foamMat;
                DestroyCollider(foam);
                bFoamPuffs.Add(foam.transform);
            }

            // Attach dynamic animator
            WaterfallAnimator bAnimator = waterfallObj.AddComponent<WaterfallAnimator>();
            bAnimator.verticalWaterRenderers = bVerticalRenderers.ToArray();
            bAnimator.riverWaterRenderers = bRiverRenderers.ToArray();
            bAnimator.foamImpactPuffs = bFoamPuffs.ToArray();
            bAnimator.poolTransform = bPoolTrans;
            bAnimator.verticalFallSpeed = 3.6f;
            bAnimator.riverFlowSpeed = 1.3f;

            Debug.Log("🏔️ [EnchantedForestDecorator] Blender 3D Waterfall Model loaded and running with live flow animation!");

            BuildWaterfallMountainBackdrop(waterfallObj.transform, rockMat, mossMat);
            DecorateWaterfallOasis(waterfallObj, litShader, unlitShader);
            return;
        }

        // Lists for the procedural fallback animator
        List<Renderer> verticalWaterRenderers = new List<Renderer>();
        List<Renderer> riverWaterRenderers = new List<Renderer>();
        List<Transform> foamImpactPuffs = new List<Transform>();

        // =========================================================================
        // 1. ELEVATED MOUNTAIN PLATEAU & RIVER SOURCE (منبع النهر الجبلي المرتفع)
        // Water flows from a mountain gorge high above, not out of thin air!
        // =========================================================================
        // Massive sloping mountain foundation extending backwards (-Z)
        for (int row = 0; row < 5; row++)
        {
            float zPos = -3f - row * 3.2f;
            float yPos = 13.5f - row * 0.8f;
            for (int col = -2; col <= 2; col++)
            {
                if (Mathf.Abs(col) <= 1 && row < 3) continue; // Leave central canyon hollow for river!

                GameObject mtnRock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mtnRock.name = $"MtnRock_{row}_{col}";
                mtnRock.transform.SetParent(waterfallObj.transform, false);
                mtnRock.transform.localPosition = new Vector3(col * 3.8f, yPos, zPos);
                mtnRock.transform.localScale = new Vector3(5.5f, 5.0f, 5.5f);
                mtnRock.GetComponent<Renderer>().material = rockMat;
                DestroyCollider(mtnRock);
            }
        }

        // Alpine Pine Trees planted on the high mountain crest behind the canyon
        for (int t = -2; t <= 2; t += 2)
        {
            GameObject pine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pine.name = $"MountainPineTrunk_{t}";
            pine.transform.SetParent(waterfallObj.transform, false);
            pine.transform.localPosition = new Vector3(t * 4.5f, 16.5f, -11f);
            pine.transform.localScale = new Vector3(0.5f, 2.5f, 0.5f);
            pine.GetComponent<Renderer>().material = barkMaterial ?? rockMat;
            DestroyCollider(pine);

            GameObject needles = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            needles.name = "PineNeedles";
            needles.transform.SetParent(pine.transform, false);
            needles.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            needles.transform.localScale = new Vector3(4.5f, 3.5f, 4.5f);
            needles.GetComponent<Renderer>().material = canopyMaterial ?? mossMat;
            DestroyCollider(needles);
        }

        // =========================================================================
        // 2. MOUNTAIN RIVER CANYON BED (مجرى النهر الجبلي المنساب)
        // High river bed at Y = 13.0f, flowing forward towards the cliff lip (Z = 0)
        // =========================================================================
        GameObject riverSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        riverSurface.name = "MountainRiverBed";
        riverSurface.transform.SetParent(waterfallObj.transform, false);
        riverSurface.transform.localPosition = new Vector3(0f, 13.0f, -7.5f);
        riverSurface.transform.localScale = new Vector3(3.8f, 0.15f, 15f);
        riverSurface.GetComponent<Renderer>().material = waterStreamMat;
        DestroyCollider(riverSurface);
        riverWaterRenderers.Add(riverSurface.GetComponent<Renderer>());

        // Riverbanks: Rocks framing the canyon so water sits in a natural stone channel
        for (int i = 0; i < 6; i++)
        {
            float z = -1f - i * 2.4f;
            // Left bank
            GameObject bankL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bankL.name = $"RiverBankL_{i}";
            bankL.transform.SetParent(waterfallObj.transform, false);
            bankL.transform.localPosition = new Vector3(-2.6f, 13.4f, z);
            bankL.transform.localScale = new Vector3(2.2f, 1.6f, 2.8f);
            bankL.GetComponent<Renderer>().material = rockMat;
            DestroyCollider(bankL);

            // Right bank
            GameObject bankR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bankR.name = $"RiverBankR_{i}";
            bankR.transform.SetParent(waterfallObj.transform, false);
            bankR.transform.localPosition = new Vector3(2.6f, 13.4f, z);
            bankR.transform.localScale = new Vector3(2.2f, 1.6f, 2.8f);
            bankR.GetComponent<Renderer>().material = rockMat;
            DestroyCollider(bankR);
        }

        // =========================================================================
        // 3. CLIFF FACE & GORGE PRECIPICE (الجرف الصخري ومصب الشلال)
        // =========================================================================
        // Vertical cliff wall framing the sides (leaves center canyon open for water)
        for (int row = 0; row < 4; row++)
        {
            for (int col = -1; col <= 1; col += 2) // col is -1 or +1, leaving col=0 clear for water torrent!
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = $"CliffWall_{row}_{col}";
                rock.transform.SetParent(waterfallObj.transform, false);
                rock.transform.localPosition = new Vector3(col * 4.2f + Random.Range(-0.2f, 0.2f), row * 3.0f + 1.6f, -1.2f);
                rock.transform.localScale = new Vector3(4.2f, 3.4f, 3.0f);
                rock.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(-8f, 8f), 0f);
                rock.GetComponent<Renderer>().material = rockMat;
                DestroyCollider(rock);
            }
        }

        // Side gorge boulders framing the canyon without blocking water
        GameObject lipL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lipL.name = "LipBoulder_Left";
        lipL.transform.SetParent(waterfallObj.transform, false);
        lipL.transform.localPosition = new Vector3(-4.5f, 13.2f, -0.6f);
        lipL.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        lipL.GetComponent<Renderer>().material = rockMat;
        DestroyCollider(lipL);

        GameObject lipR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lipR.name = "LipBoulder_Right";
        lipR.transform.SetParent(waterfallObj.transform, false);
        lipR.transform.localPosition = new Vector3(4.5f, 13.2f, -0.6f);
        lipR.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        lipR.GetComponent<Renderer>().material = rockMat;
        DestroyCollider(lipR);

        // Flanking cliff crags
        for (int side = -1; side <= 1; side += 2)
        {
            for (int r = 0; r < 4; r++)
            {
                GameObject flank = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flank.name = $"FlankCrag_{side}_{r}";
                flank.transform.SetParent(waterfallObj.transform, false);
                flank.transform.localPosition = new Vector3(side * 5.6f, r * 3.2f + 1.8f, 0.2f);
                flank.transform.localScale = new Vector3(3.6f, 3.2f, 3.2f);
                flank.GetComponent<Renderer>().material = rockMat;
                DestroyCollider(flank);
            }
        }

        // =========================================================================
        // 4. MULTI-TIER RUSHING WATER CASCADES (الشلالات المنهمرة المتدفقة)
        // Angled, layered curtains with rapid UV scrolling downward!
        // =========================================================================
        // A. Crest Lip Leaping Water (curls over the precipice)
        GameObject lipWater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lipWater.name = "CrestLipWater";
        lipWater.transform.SetParent(waterfallObj.transform, false);
        lipWater.transform.localPosition = new Vector3(0f, 12.8f, 0.2f);
        lipWater.transform.localScale = new Vector3(3.0f, 0.4f, 1.2f);
        lipWater.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        lipWater.GetComponent<Renderer>().material = waterStreamMat;
        DestroyCollider(lipWater);
        verticalWaterRenderers.Add(lipWater.GetComponent<Renderer>());

        // B. Main Center Torrent (Wide, fast, rushing cascade)
        GameObject mainCurtain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainCurtain.name = "MainWaterCurtain";
        mainCurtain.transform.SetParent(waterfallObj.transform, false);
        mainCurtain.transform.localPosition = new Vector3(0f, 6.2f, 0.7f);
        mainCurtain.transform.localScale = new Vector3(3.0f, 12.4f, 0.18f);
        mainCurtain.GetComponent<Renderer>().material = waterStreamMat;
        DestroyCollider(mainCurtain);
        verticalWaterRenderers.Add(mainCurtain.GetComponent<Renderer>());

        // C. Left and Right Secondary Falls (Cascading over side ledges)
        GameObject leftCurtain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftCurtain.name = "LeftWaterCurtain";
        leftCurtain.transform.SetParent(waterfallObj.transform, false);
        leftCurtain.transform.localPosition = new Vector3(-1.8f, 5.2f, 0.55f);
        leftCurtain.transform.localScale = new Vector3(1.5f, 9.8f, 0.15f);
        leftCurtain.GetComponent<Renderer>().material = waterStreamMat;
        DestroyCollider(leftCurtain);
        verticalWaterRenderers.Add(leftCurtain.GetComponent<Renderer>());

        GameObject rightCurtain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightCurtain.name = "RightWaterCurtain";
        rightCurtain.transform.SetParent(waterfallObj.transform, false);
        rightCurtain.transform.localPosition = new Vector3(1.8f, 5.5f, 0.55f);
        rightCurtain.transform.localScale = new Vector3(1.3f, 10.4f, 0.15f);
        rightCurtain.GetComponent<Renderer>().material = waterStreamMat;
        DestroyCollider(rightCurtain);
        verticalWaterRenderers.Add(rightCurtain.GetComponent<Renderer>());

        // D. Lower Splashing Fan (spreads out over bottom shelf)
        GameObject lowerFan = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lowerFan.name = "LowerWaterFan";
        lowerFan.transform.SetParent(waterfallObj.transform, false);
        lowerFan.transform.localPosition = new Vector3(0f, 1.8f, 1.4f);
        lowerFan.transform.localScale = new Vector3(4.2f, 3.6f, 0.22f);
        lowerFan.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
        lowerFan.GetComponent<Renderer>().material = waterStreamMat;
        DestroyCollider(lowerFan);
        verticalWaterRenderers.Add(lowerFan.GetComponent<Renderer>());

        // =========================================================================
        // 5. PLUNGE POOL, IMPACT FOAM & MIST PUFFS (حوض الارتطام والرغوة المتصاعدة)
        // =========================================================================
        // Frothing impact foam puffs where water slams into the pool
        for (int i = 0; i < 12; i++)
        {
            GameObject foam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foam.name = $"ImpactFoamPuff_{i}";
            foam.transform.SetParent(waterfallObj.transform, false);
            foam.transform.localPosition = new Vector3(Random.Range(-2.4f, 2.4f), Random.Range(0.2f, 0.9f), Random.Range(1.8f, 3.2f));
            foam.transform.localScale = Vector3.one * Random.Range(0.55f, 1.15f);
            foam.GetComponent<Renderer>().material = foamMat;
            DestroyCollider(foam);
            foamImpactPuffs.Add(foam.transform);
        }

        // Crystal Lagoon Plunge Pool
        if (poolMat == null) poolMat = new Material(unlitShader);
        poolCol = new Color(0.25f, 0.72f, 0.94f, 0.92f);
        poolMat.color = poolCol;
        if (poolMat.HasProperty("_BaseColor")) poolMat.SetColor("_BaseColor", poolCol);

        GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pool.name = "WaterfallPlungePool";
        pool.transform.SetParent(waterfallObj.transform, false);
        pool.transform.localPosition = new Vector3(0f, 0.06f, 4.0f);
        pool.transform.localScale = new Vector3(7.5f, 0.10f, 6.5f);
        pool.GetComponent<Renderer>().material = poolMat;
        DestroyCollider(pool);

        // Smooth river stones encircling the lagoon rim
        for (int i = 0; i < 14; i++)
        {
            float a = (float)i / 14f * Mathf.PI * 2f;
            GameObject rimStone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rimStone.name = $"PoolRimStone_{i}";
            rimStone.transform.SetParent(waterfallObj.transform, false);
            rimStone.transform.localPosition = new Vector3(Mathf.Cos(a) * 3.8f, 0.16f, 4.0f + Mathf.Sin(a) * 3.1f);
            rimStone.transform.localScale = new Vector3(Random.Range(0.9f, 1.4f), Random.Range(0.4f, 0.7f), Random.Range(0.9f, 1.3f));
            rimStone.GetComponent<Renderer>().material = rockMat;
            DestroyCollider(rimStone);
        }

        // =========================================================================
        // 6. ATTACH WATERFALL ANIMATOR (تشغيل الحركة الحية للتدفق والرغوة)
        // =========================================================================
        WaterfallAnimator animator = waterfallObj.AddComponent<WaterfallAnimator>();
        animator.verticalWaterRenderers = verticalWaterRenderers.ToArray();
        animator.riverWaterRenderers = riverWaterRenderers.ToArray();
        animator.foamImpactPuffs = foamImpactPuffs.ToArray();
        animator.poolTransform = pool.transform;
        animator.verticalFallSpeed = 3.6f;
        animator.riverFlowSpeed = 1.3f;

        BuildWaterfallMountainBackdrop(waterfallObj.transform, rockMat, mossMat);
        DecorateWaterfallOasis(waterfallObj, litShader, unlitShader);
    }

    // =========================================================================
    // PROCEDURAL MOUNTAIN MASSIF GENERATOR (REAL 3D SLOPED MOUNTAINS, NO SPHERES!)
    // Generates authentic concave sloping mountain peaks with snow caps
    // =========================================================================
    GameObject CreateProceduralMountainMassif(string name, Transform parent, Vector3 worldPos, float baseRadiusX, float baseRadiusZ, float height, float peakOffsetX, float peakOffsetZ, int seed)
    {
        // حماية هندسية صارمة: لا يمكن لأي جبل أن يقترب من الشجرة أو ساحة القتال (R < 175m)
        float horizDist = new Vector2(worldPos.x, worldPos.z).magnitude;
        if (horizDist < 175f)
        {
            Vector2 dir = new Vector2(worldPos.x, worldPos.z).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = new Vector2(-0.7f, -0.7f).normalized;
            worldPos = new Vector3(dir.x * 190f, 0f, dir.y * 190f);
        }

        // تقييد نصف قطر قاعدة الجبل حتى لا تمتد نحو المركز أبداً
        baseRadiusX = Mathf.Min(baseRadiusX, 45f);
        baseRadiusZ = Mathf.Min(baseRadiusZ, 45f);

        GameObject massif = new GameObject(name);
        massif.transform.SetParent(parent, true);
        massif.transform.position = worldPos;

        int slices = 24; // 24-radial faceted alpine silhouette
        int tiers = 6;   // tier 0 (base Y=0), tier 1, tier 2 (mid-rock), tier 3 (snowline), tier 4, tier 5 (summit horn)

        Vector3[][] rings = new Vector3[tiers][];
        Vector3[][] ringNormals = new Vector3[tiers][];

        for (int t = 0; t < tiers; t++)
        {
            rings[t] = new Vector3[slices];
            ringNormals[t] = new Vector3[slices];
            float tNorm = (float)t / (tiers - 1); // 0.0, 0.2, 0.4, 0.6, 0.8, 1.0

            // Volcanic / Alpine concave exponential slope curve (Matterhorn slope)
            // Starts broad and gentle at base, steepens gracefully towards the summit horn
            float radiusScale = Mathf.Pow(Mathf.Clamp01(1.0f - tNorm), 0.68f);
            float h = (t == 0) ? 0.0f : (Mathf.Pow(tNorm, 1.32f) * height);

            float offX = peakOffsetX * tNorm;
            float offZ = peakOffsetZ * tNorm;

            for (int s = 0; s < slices; s++)
            {
                float angle = (float)s / slices * Mathf.PI * 2f;
                // Organic faceted ridge noise with natural alpine arêtes
                float n = Mathf.Sin(s * 2.3f + seed + t * 0.7f) * 0.14f + Mathf.Cos(s * 3.7f + seed * 1.3f) * 0.09f;
                float rx = baseRadiusX * radiusScale * (1.0f + n);
                float rz = baseRadiusZ * radiusScale * (1.0f + n);

                float y = (t == 0) ? 0.0f : (h + Mathf.Sin(s * 3.1f + seed) * (height * 0.025f));
                Vector3 pt = new Vector3(Mathf.Cos(angle) * rx + offX, y, Mathf.Sin(angle) * rz + offZ);
                rings[t][s] = pt;

                // Explicit outward and upward normal pointing to the sky (GUARANTEED NEVER PITCH BLACK!)
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 norm = (radial * 0.60f + Vector3.up * 0.80f).normalized;
                ringNormals[t][s] = norm;
            }
        }

        Vector3 apex = new Vector3(peakOffsetX, height, peakOffsetZ);
        Vector3 apexNormal = Vector3.up;

        // 1. Build Rock Slopes Mesh (from ground base Y=0 up to snowline tier 3)
        Mesh rockMesh = new Mesh();
        rockMesh.name = $"{name}_RockMesh";
        List<Vector3> rockVerts = new List<Vector3>();
        List<Vector3> rockNorms = new List<Vector3>();
        List<Vector2> rockUVs = new List<Vector2>();
        List<int> rockTris = new List<int>();

        for (int t = 0; t < 3; t++)
        {
            for (int s = 0; s < slices; s++)
            {
                int nextS = (s + 1) % slices;
                Vector3 p0 = rings[t][s];
                Vector3 p1 = rings[t][nextS];
                Vector3 p2 = rings[t + 1][nextS];
                Vector3 p3 = rings[t + 1][s];

                Vector3 n0 = ringNormals[t][s];
                Vector3 n1 = ringNormals[t][nextS];
                Vector3 n2 = ringNormals[t + 1][nextS];
                Vector3 n3 = ringNormals[t + 1][s];

                int baseIdx = rockVerts.Count;
                rockVerts.Add(p0); rockNorms.Add(n0);
                rockVerts.Add(p1); rockNorms.Add(n1);
                rockVerts.Add(p2); rockNorms.Add(n2);
                rockVerts.Add(p3); rockNorms.Add(n3);

                rockUVs.Add(new Vector2((float)s / slices * 4f, (float)t));
                rockUVs.Add(new Vector2((float)(s + 1) / slices * 4f, (float)t));
                rockUVs.Add(new Vector2((float)(s + 1) / slices * 4f, (float)(t + 1)));
                rockUVs.Add(new Vector2((float)s / slices * 4f, (float)(t + 1)));

                // Front faces (clockwise from outside)
                rockTris.Add(baseIdx);
                rockTris.Add(baseIdx + 3);
                rockTris.Add(baseIdx + 2);

                rockTris.Add(baseIdx);
                rockTris.Add(baseIdx + 2);
                rockTris.Add(baseIdx + 1);

                // Back faces (two-sided insurance so no faces are culled)
                rockTris.Add(baseIdx);
                rockTris.Add(baseIdx + 2);
                rockTris.Add(baseIdx + 3);

                rockTris.Add(baseIdx);
                rockTris.Add(baseIdx + 1);
                rockTris.Add(baseIdx + 2);
            }
        }

        rockMesh.SetVertices(rockVerts);
        rockMesh.SetNormals(rockNorms);
        rockMesh.SetUVs(0, rockUVs);
        rockMesh.SetTriangles(rockTris, 0);
        rockMesh.RecalculateBounds();

        GameObject rockObj = new GameObject("RockSlopes");
        rockObj.transform.SetParent(massif.transform, false);
        MeshFilter rockMf = rockObj.AddComponent<MeshFilter>();
        rockMf.sharedMesh = rockMesh;
        MeshRenderer rockMr = rockObj.AddComponent<MeshRenderer>();
        rockMr.material = mountainRockMat;

        // 2. Build Snow Cap Mesh (from snowline tier 3 to apex summit)
        Mesh snowMesh = new Mesh();
        snowMesh.name = $"{name}_SnowMesh";
        List<Vector3> snowVerts = new List<Vector3>();
        List<Vector3> snowNorms = new List<Vector3>();
        List<Vector2> snowUVs = new List<Vector2>();
        List<int> snowTris = new List<int>();

        for (int t = 3; t < tiers - 1; t++)
        {
            for (int s = 0; s < slices; s++)
            {
                int nextS = (s + 1) % slices;
                Vector3 p0 = rings[t][s];
                Vector3 p1 = rings[t][nextS];
                Vector3 p2 = rings[t + 1][nextS];
                Vector3 p3 = rings[t + 1][s];

                Vector3 n0 = ringNormals[t][s];
                Vector3 n1 = ringNormals[t][nextS];
                Vector3 n2 = ringNormals[t + 1][nextS];
                Vector3 n3 = ringNormals[t + 1][s];

                int baseIdx = snowVerts.Count;
                snowVerts.Add(p0); snowNorms.Add(n0);
                snowVerts.Add(p1); snowNorms.Add(n1);
                snowVerts.Add(p2); snowNorms.Add(n2);
                snowVerts.Add(p3); snowNorms.Add(n3);

                snowUVs.Add(new Vector2((float)s / slices * 3f, (float)(t - 3)));
                snowUVs.Add(new Vector2((float)(s + 1) / slices * 3f, (float)(t - 3)));
                snowUVs.Add(new Vector2((float)(s + 1) / slices * 3f, (float)(t - 2)));
                snowUVs.Add(new Vector2((float)s / slices * 3f, (float)(t - 2)));

                // Front faces
                snowTris.Add(baseIdx);
                snowTris.Add(baseIdx + 3);
                snowTris.Add(baseIdx + 2);

                snowTris.Add(baseIdx);
                snowTris.Add(baseIdx + 2);
                snowTris.Add(baseIdx + 1);

                // Back faces
                snowTris.Add(baseIdx);
                snowTris.Add(baseIdx + 2);
                snowTris.Add(baseIdx + 3);

                snowTris.Add(baseIdx);
                snowTris.Add(baseIdx + 1);
                snowTris.Add(baseIdx + 2);
            }
        }

        // Apex summit cone
        int topTier = tiers - 1;
        for (int s = 0; s < slices; s++)
        {
            int nextS = (s + 1) % slices;
            Vector3 p0 = rings[topTier][s];
            Vector3 p1 = rings[topTier][nextS];
            Vector3 pApex = apex;

            Vector3 n0 = ringNormals[topTier][s];
            Vector3 n1 = ringNormals[topTier][nextS];

            int baseIdx = snowVerts.Count;
            snowVerts.Add(p0); snowNorms.Add(n0);
            snowVerts.Add(p1); snowNorms.Add(n1);
            snowVerts.Add(pApex); snowNorms.Add(apexNormal);

            snowUVs.Add(new Vector2((float)s / slices * 3f, 1.0f));
            snowUVs.Add(new Vector2((float)(s + 1) / slices * 3f, 1.0f));
            snowUVs.Add(new Vector2((float)s / slices * 3f + 0.5f, 2.0f));

            snowTris.Add(baseIdx);
            snowTris.Add(baseIdx + 2);
            snowTris.Add(baseIdx + 1);

            snowTris.Add(baseIdx);
            snowTris.Add(baseIdx + 1);
            snowTris.Add(baseIdx + 2);
        }

        snowMesh.SetVertices(snowVerts);
        snowMesh.SetNormals(snowNorms);
        snowMesh.SetUVs(0, snowUVs);
        snowMesh.SetTriangles(snowTris, 0);
        snowMesh.RecalculateBounds();

        GameObject snowObj = new GameObject("SnowCap");
        snowObj.transform.SetParent(massif.transform, false);
        MeshFilter snowMf = snowObj.AddComponent<MeshFilter>();
        snowMf.sharedMesh = snowMesh;
        MeshRenderer snowMr = snowObj.AddComponent<MeshRenderer>();
        snowMr.material = mountainPeakSnowMat;

        return massif;
    }

    // =========================================================================
    // WATERFALL MOUNTAIN BACKDROP & GORGE (Real Mountain Massif Behind Waterfall)
    // =========================================================================
    void BuildWaterfallMountainBackdrop(Transform parent, Material rockMat, Material mossMat)
    {
        GameObject mtnBackdrop = new GameObject("WaterfallMountainBackdrop");
        mtnBackdrop.transform.SetParent(parent, false);

        // 1. Towering procedural mountain massif right behind the waterfall (in distant western mountain range, R = 174m)
        Vector3 mtnWorldPos = parent.position + new Vector3(0f, 0f, -25f);
        CreateProceduralMountainMassif("WaterfallMountainGorge", mtnBackdrop.transform, mtnWorldPos, 55f, 45f, 95f, 0f, -8f, 777);

        Material useRock = mountainRockMat ?? rockMat;

        // 2. High mountain gorge cave mouth where water rushes out (Y = 16-22m, Z = -5 to -10m)
        int archStones = 7;
        for (int i = 0; i < archStones; i++)
        {
            float a = (float)i / (archStones - 1) * Mathf.PI;
            float rx = Mathf.Cos(a) * 4.2f;
            float ry = Mathf.Sin(a) * 3.5f;

            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = $"WaterfallGorgeArch_{i}";
            stone.transform.SetParent(mtnBackdrop.transform, false);
            stone.transform.localPosition = new Vector3(rx, 18.5f + ry, -6.5f);
            stone.transform.localScale = new Vector3(2.8f, 2.2f, 2.8f);
            stone.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            stone.GetComponent<Renderer>().material = useRock;
            DestroyCollider(stone);
        }

        // Dark cave mouth interior at source high up on the distant mountain
        GameObject srcVoid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        srcVoid.name = "WaterfallSourceVoid";
        srcVoid.transform.SetParent(mtnBackdrop.transform, false);
        srcVoid.transform.localPosition = new Vector3(0f, 19.5f, -9.0f);
        srcVoid.transform.localScale = new Vector3(6.5f, 5.0f, 4.0f);
        srcVoid.GetComponent<Renderer>().material = riverGrottoDarkMat ?? stoneMaterial;
        DestroyCollider(srcVoid);
    }

    // =========================================================================
    // ENCHANTED OASIS SURROUNDING THE WATERFALL (أزهار نادرة، أشجار متنوعة، وصخور مميزة)
    // =========================================================================
    void DecorateWaterfallOasis(GameObject waterfallObj, Shader litShader, Shader unlitShader)
    {
        GameObject oasisGroup = new GameObject("WaterfallOasisSurroundings");
        oasisGroup.transform.SetParent(waterfallObj.transform, false);

        // --- MATERIALS ---
        // 1. صخور النهر الداكنة الرطبة الملساء (Wet River Slate)
        Material wetSlateMat = new Material(litShader);
        wetSlateMat.color = new Color(0.20f, 0.22f, 0.24f);
        if (wetSlateMat.HasProperty("_Smoothness")) wetSlateMat.SetFloat("_Smoothness", 0.90f);

        // 2. صخور الغابة المكسوة بالطحالب الزمردية (Mossy Forest Rock)
        Material mossyStoneMat = new Material(litShader);
        mossyStoneMat.color = new Color(0.38f, 0.36f, 0.32f);

        Material mossCapMat = new Material(litShader);
        mossCapMat.color = new Color(0.18f, 0.65f, 0.16f);

        // 3. أحجار بلورية متوهجة طفيفة (Crystalline Turquoise & Amethyst Geodes)
        Material geodeCyanMat = new Material(unlitShader);
        geodeCyanMat.color = new Color(0.25f, 0.95f, 0.92f, 0.98f);
        if (geodeCyanMat.HasProperty("_BaseColor")) geodeCyanMat.SetColor("_BaseColor", geodeCyanMat.color);

        Material geodeVioletMat = new Material(unlitShader);
        geodeVioletMat.color = new Color(0.78f, 0.35f, 1.0f, 0.98f);
        if (geodeVioletMat.HasProperty("_BaseColor")) geodeVioletMat.SetColor("_BaseColor", geodeVioletMat.color);

        // 4. خامات الأشجار المتنوعة:
        // أ. شجرة الكرز الساحرة المزهرة (Pink Cherry Blossom)
        Material cherryBlossomMat = new Material(litShader);
        cherryBlossomMat.color = new Color(1.0f, 0.68f, 0.82f);
        Material cherryBlossomWhiteMat = new Material(litShader);
        cherryBlossomWhiteMat.color = new Color(1.0f, 0.88f, 0.94f);

        // ب. شجرة البتول الفضي الأبيض (Silver Birch)
        Material birchBarkMat = new Material(litShader);
        birchBarkMat.color = new Color(0.96f, 0.96f, 0.93f);
        Material birchLeafMat = new Material(litShader);
        birchLeafMat.color = new Color(0.58f, 0.88f, 0.28f);

        // ج. شجرة الصفصاف الباكي (Weeping Willow)
        Material willowLeafMat = new Material(litShader);
        willowLeafMat.color = new Color(0.22f, 0.72f, 0.35f);

        // 5. خامات الأزهار المتنوعة وزنابق الماء:
        Material waterLilyPadMat = new Material(litShader);
        waterLilyPadMat.color = new Color(0.12f, 0.46f, 0.15f);

        Material lotusFlowerMat = new Material(unlitShader);
        lotusFlowerMat.color = new Color(1.0f, 0.88f, 0.95f);
        if (lotusFlowerMat.HasProperty("_BaseColor")) lotusFlowerMat.SetColor("_BaseColor", lotusFlowerMat.color);

        Material lotusCenterMat = new Material(unlitShader);
        lotusCenterMat.color = new Color(1.0f, 0.82f, 0.12f);
        if (lotusCenterMat.HasProperty("_BaseColor")) lotusCenterMat.SetColor("_BaseColor", lotusCenterMat.color);

        Material lavenderMat = new Material(unlitShader);
        lavenderMat.color = new Color(0.68f, 0.38f, 0.92f);
        if (lavenderMat.HasProperty("_BaseColor")) lavenderMat.SetColor("_BaseColor", lavenderMat.color);

        Material buttercupMat = new Material(unlitShader);
        buttercupMat.color = new Color(1.0f, 0.85f, 0.10f);
        if (buttercupMat.HasProperty("_BaseColor")) buttercupMat.SetColor("_BaseColor", buttercupMat.color);

        Material bluebellMat = new Material(unlitShader);
        bluebellMat.color = new Color(0.20f, 0.75f, 1.0f);
        if (bluebellMat.HasProperty("_BaseColor")) bluebellMat.SetColor("_BaseColor", bluebellMat.color);

        Material poppyMat = new Material(unlitShader);
        poppyMat.color = new Color(0.95f, 0.16f, 0.20f);
        if (poppyMat.HasProperty("_BaseColor")) poppyMat.SetColor("_BaseColor", poppyMat.color);

        // =========================================================================
        // A. أشجار أخرى بأنواع مختلفة حول الشلال (Cherry Blossom, Weeping Willow, Silver Birch)
        // =========================================================================

        // 1. شجرة الكرز المزهرة ذات التيجان الوردية الساحرة (على الضفة اليسرى المطلة على البركة)
        CreateCherryBlossomTree(oasisGroup.transform, new Vector3(-5.4f, 0f, 3.8f), cherryBlossomMat, cherryBlossomWhiteMat);

        // 2. شجرة الصفصاف الباكي (على الضفة اليمنى تتدلى فروعها الخضراء فوق صفحة الماء)
        CreateWeepingWillowTree(oasisGroup.transform, new Vector3(5.5f, 0f, 4.2f), willowLeafMat);

        // 3. شجرتا البتول الفضي الأبيض (Silver Birch) بجذوع بيضاء وأوراق ذهبية خضراء على المنحدرات الجانبية
        CreateSilverBirchTree(oasisGroup.transform, new Vector3(-7.2f, 0f, 6.8f), birchBarkMat, birchLeafMat);
        CreateSilverBirchTree(oasisGroup.transform, new Vector3(7.0f, 0f, 7.2f), birchBarkMat, birchLeafMat);

        // =========================================================================
        // B. أنواع معينة من الصخور حول الشلال والبحيرة
        // =========================================================================

        // 1. صخور النهر الداكنة الملساء (أحجار تخطٍ بارزة وواضحة فوق سطح الماء)
        Vector3[] slatePositions = new Vector3[]
        {
            new Vector3(-2.5f, 0.22f, 5.5f),
            new Vector3(-1.3f, 0.22f, 6.2f),
            new Vector3(0.0f, 0.22f, 6.8f),
            new Vector3(1.3f, 0.22f, 6.3f),
            new Vector3(2.4f, 0.22f, 5.7f),
            new Vector3(3.5f, 0.20f, 4.6f)
        };
        for (int i = 0; i < slatePositions.Length; i++)
        {
            GameObject slate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slate.name = $"RiverSlate_{i}";
            slate.transform.SetParent(oasisGroup.transform, false);
            slate.transform.localPosition = slatePositions[i];
            slate.transform.localScale = new Vector3(1.5f, 0.20f, 1.6f);
            slate.transform.localRotation = Quaternion.Euler(Random.Range(-3f, 3f), Random.Range(0f, 180f), Random.Range(-3f, 3f));
            slate.GetComponent<Renderer>().material = wetSlateMat;
            DestroyCollider(slate);
        }

        // 2. أحجار بلورية متوهجة طفيفة (Crystalline Geode Clusters) مع إضاءة سحرية هادئة
        Vector3[] geodePositions = new Vector3[]
        {
            new Vector3(-3.6f, 0.45f, 3.4f),
            new Vector3(3.8f, 0.45f, 3.6f),
            new Vector3(-3.5f, 0.35f, 6.2f),
            new Vector3(3.6f, 0.35f, 6.4f)
        };
        for (int i = 0; i < geodePositions.Length; i++)
        {
            GameObject geodeGroup = new GameObject($"CrystalGeodeCluster_{i}");
            geodeGroup.transform.SetParent(oasisGroup.transform, false);
            geodeGroup.transform.localPosition = geodePositions[i];

            Material gMat = (i % 2 == 0) ? geodeCyanMat : geodeVioletMat;

            // 3-4 بلورات موشورية في كل عنقود
            for (int c = 0; c < 3; c++)
            {
                float cAngle = c * 120f * Mathf.Deg2Rad;
                Vector3 cOffset = new Vector3(Mathf.Cos(cAngle) * 0.22f, 0f, Mathf.Sin(cAngle) * 0.22f);

                GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crystal.name = $"Crystal_{c}";
                crystal.transform.SetParent(geodeGroup.transform, false);
                crystal.transform.localPosition = cOffset + Vector3.up * 0.55f;
                crystal.transform.localScale = new Vector3(0.28f, 0.65f, 0.28f);
                crystal.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), c * 60f, Random.Range(-12f, 12f));
                crystal.GetComponent<Renderer>().material = gMat;
                DestroyCollider(crystal);

                // قمة بلورية حادة
                GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.name = $"Tip_{c}";
                tip.transform.SetParent(crystal.transform, false);
                tip.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                tip.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
                tip.GetComponent<Renderer>().material = gMat;
                DestroyCollider(tip);
            }

            // إضاءة خافتة ساحرة للمذنب البلوري
            Light gLight = geodeGroup.AddComponent<Light>();
            gLight.type = LightType.Point;
            gLight.color = (i % 2 == 0) ? new Color(0.3f, 0.95f, 0.9f) : new Color(0.8f, 0.4f, 1.0f);
            gLight.range = 5.5f;
            gLight.intensity = 2.2f;
        }

        // =========================================================================
        // C. أزهار متنوعة وزنابق ماء (Water Lilies, Lavender, Buttercups, Bluebells, Poppies)
        // =========================================================================

        // 1. زنابق الماء وأوراق اللوتس الطافية بوضوح فوق صفحة ماء البركة (ارتفاع 0.22 متر لتكون فوق سطح الماء)
        Vector3[] lilyPadPositions = new Vector3[]
        {
            new Vector3(-1.6f, 0.22f, 3.0f),
            new Vector3(-2.4f, 0.22f, 4.4f),
            new Vector3(1.8f, 0.22f, 3.2f),
            new Vector3(2.5f, 0.22f, 4.6f),
            new Vector3(-0.6f, 0.22f, 5.0f),
            new Vector3(0.8f, 0.22f, 3.8f)
        };
        for (int i = 0; i < lilyPadPositions.Length; i++)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = $"LilyPad_{i}";
            pad.transform.SetParent(oasisGroup.transform, false);
            pad.transform.localPosition = lilyPadPositions[i];
            pad.transform.localScale = new Vector3(1.4f, 0.03f, 1.4f);
            pad.GetComponent<Renderer>().material = waterLilyPadMat;
            DestroyCollider(pad);

            // زهرة اللوتس البيضاء والوردية فوق الورقة بتفاصيل واضحة
            GameObject lotus = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lotus.name = "LotusBloom";
            lotus.transform.SetParent(pad.transform, false);
            lotus.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            lotus.transform.localScale = new Vector3(0.65f, 0.45f, 0.65f);
            lotus.GetComponent<Renderer>().material = lotusFlowerMat;
            DestroyCollider(lotus);

            // قلب الزهرة الذهبي
            GameObject stamen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stamen.name = "LotusStamen";
            stamen.transform.SetParent(lotus.transform, false);
            stamen.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            stamen.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            stamen.GetComponent<Renderer>().material = lotusCenterMat;
            DestroyCollider(stamen);
        }

        // 2. مجموعات أزهار برية متنوعة وبارزة في محيط الشلال بتوازن مثالي دون اكتظاظ
        SpawnFlowerCluster(oasisGroup.transform, new Vector3(4.2f, 0f, 6.2f), lavenderMat, 5, "Lavender");
        SpawnFlowerCluster(oasisGroup.transform, new Vector3(-3.8f, 0f, 6.0f), bluebellMat, 5, "Bluebell");
        SpawnFlowerCluster(oasisGroup.transform, new Vector3(-2.2f, 0f, 8.5f), buttercupMat, 6, "Buttercup");
        SpawnFlowerCluster(oasisGroup.transform, new Vector3(2.4f, 0f, 8.5f), poppyMat, 5, "Poppy");
        SpawnFlowerCluster(oasisGroup.transform, new Vector3(0f, 0f, 9.4f), buttercupMat, 5, "Buttercup");
    }

    void CreateCherryBlossomTree(Transform parent, Vector3 localPos, Material blossomMat, Material whiteBlossomMat)
    {
        GameObject tree = new GameObject("CherryBlossomTree");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = localPos;

        // Trunk (sturdy and gracefully curved leaning toward the pool)
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 2.75f, 0f);
        trunk.transform.localScale = new Vector3(0.85f, 2.75f, 0.85f);
        trunk.transform.localRotation = Quaternion.Euler(5f, 0f, 8f);
        trunk.GetComponent<Renderer>().material = barkMaterial;
        DestroyCollider(trunk);

        // Lush pink blossom cloud canopy (multiple overlapping clouds)
        Vector3[] foliageOffsets = new Vector3[]
        {
            new Vector3(0.2f, 5.2f, 0.2f),
            new Vector3(-1.2f, 5.6f, 0.8f),
            new Vector3(1.3f, 5.4f, -0.6f),
            new Vector3(0.3f, 6.2f, 0f),
            new Vector3(-0.8f, 4.6f, -1.0f),
            new Vector3(1.0f, 4.8f, 1.2f),
            new Vector3(0f, 6.8f, 0.3f)
        };
        Vector3[] foliageScales = new Vector3[]
        {
            new Vector3(4.5f, 2.8f, 4.5f),
            new Vector3(3.4f, 2.4f, 3.4f),
            new Vector3(3.6f, 2.5f, 3.6f),
            new Vector3(3.8f, 2.4f, 3.8f),
            new Vector3(2.8f, 2.0f, 2.8f),
            new Vector3(3.0f, 2.2f, 3.0f),
            new Vector3(2.5f, 1.8f, 2.5f)
        };
        for (int i = 0; i < foliageOffsets.Length; i++)
        {
            GameObject bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bloom.name = $"BlossomCluster_{i}";
            bloom.transform.SetParent(tree.transform, false);
            bloom.transform.localPosition = foliageOffsets[i];
            bloom.transform.localScale = foliageScales[i];
            bloom.GetComponent<Renderer>().material = (i % 2 == 0) ? blossomMat : whiteBlossomMat;
            DestroyCollider(bloom);
        }

        // Falling sakura petals on the grass around base
        for (int p = 0; p < 8; p++)
        {
            float pAng = p * 45f * Mathf.Deg2Rad;
            GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            petal.name = $"FallenPetal_{p}";
            petal.transform.SetParent(tree.transform, false);
            petal.transform.localPosition = new Vector3(Mathf.Cos(pAng) * Random.Range(1.2f, 3.2f), 0.04f, Mathf.Sin(pAng) * Random.Range(1.2f, 3.2f));
            petal.transform.localScale = new Vector3(0.35f, 0.005f, 0.35f);
            petal.GetComponent<Renderer>().material = blossomMat;
            DestroyCollider(petal);
        }
    }

    void CreateWeepingWillowTree(Transform parent, Vector3 localPos, Material willowMat)
    {
        GameObject tree = new GameObject("WeepingWillowTree");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = localPos;

        // Trunk leaning gracefully toward the water
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 2.9f, 0f);
        trunk.transform.localScale = new Vector3(0.90f, 2.9f, 0.90f);
        trunk.transform.localRotation = Quaternion.Euler(-5f, 0f, -12f);
        trunk.GetComponent<Renderer>().material = barkMaterial;
        DestroyCollider(trunk);

        // Broad weeping crown
        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "WillowCrown";
        crown.transform.SetParent(tree.transform, false);
        crown.transform.localPosition = new Vector3(-0.6f, 5.6f, 0f);
        crown.transform.localScale = new Vector3(6.5f, 2.2f, 6.5f);
        crown.GetComponent<Renderer>().material = willowMat;
        DestroyCollider(crown);

        // Hanging weeping branches dipping gracefully toward the water
        for (int i = 0; i < 12; i++)
        {
            float ang = i * 30f * Mathf.Deg2Rad;
            Vector3 branchPos = new Vector3(Mathf.Cos(ang) * 2.4f - 0.6f, 3.6f, Mathf.Sin(ang) * 2.4f);

            GameObject streamer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            streamer.name = $"WeepingBranch_{i}";
            streamer.transform.SetParent(tree.transform, false);
            streamer.transform.localPosition = branchPos;
            streamer.transform.localScale = new Vector3(0.45f, 2.2f, 0.45f);
            streamer.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
            streamer.GetComponent<Renderer>().material = willowMat;
            DestroyCollider(streamer);
        }
    }

    void CreateSilverBirchTree(Transform parent, Vector3 localPos, Material birchBarkMat, Material birchLeafMat)
    {
        GameObject tree = new GameObject("SilverBirchTree");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = localPos;

        // Tall slender white birch trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "BirchTrunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 4.0f, 0f);
        trunk.transform.localScale = new Vector3(0.45f, 4.0f, 0.45f);
        trunk.GetComponent<Renderer>().material = birchBarkMat;
        DestroyCollider(trunk);

        // Airy golden-green foliage layers
        Vector3[] layerOffsets = new Vector3[]
        {
            new Vector3(0f, 6.2f, 0f),
            new Vector3(0f, 7.5f, 0f),
            new Vector3(0f, 8.6f, 0f),
            new Vector3(0f, 9.4f, 0f)
        };
        Vector3[] layerScales = new Vector3[]
        {
            new Vector3(3.8f, 2.0f, 3.8f),
            new Vector3(3.0f, 1.8f, 3.0f),
            new Vector3(2.2f, 1.5f, 2.2f),
            new Vector3(1.4f, 1.2f, 1.4f)
        };
        for (int i = 0; i < layerOffsets.Length; i++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = $"BirchFoliage_{i}";
            leaf.transform.SetParent(tree.transform, false);
            leaf.transform.localPosition = layerOffsets[i];
            leaf.transform.localScale = layerScales[i];
            leaf.GetComponent<Renderer>().material = birchLeafMat;
            DestroyCollider(leaf);
        }
    }

    void SpawnFlowerCluster(Transform parent, Vector3 center, Material petalMat, int count, string flowerType)
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material stemMat = new Material(litShader);
        stemMat.color = new Color(0.18f, 0.48f, 0.16f);

        for (int i = 0; i < count; i++)
        {
            Vector3 flowerPos = center + new Vector3(Random.Range(-0.65f, 0.65f), 0f, Random.Range(-0.65f, 0.65f));

            // Green stalk (tall enough to rise proudly above meadow grass)
            float stemHeight = Random.Range(0.75f, 1.15f);
            GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.name = $"{flowerType}_Stalk_{i}";
            stalk.transform.SetParent(parent, false);
            stalk.transform.localPosition = flowerPos + Vector3.up * (stemHeight / 2f);
            stalk.transform.localScale = new Vector3(0.06f, stemHeight / 2f, 0.06f);
            stalk.GetComponent<Renderer>().material = stemMat;
            DestroyCollider(stalk);

            // Petal bloom (distinct, prominent bloom)
            GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            petal.name = $"{flowerType}_Bloom_{i}";
            petal.transform.SetParent(parent, false);
            petal.transform.localPosition = flowerPos + Vector3.up * (stemHeight + 0.12f);
            petal.transform.localScale = new Vector3(0.48f, 0.38f, 0.48f);
            petal.GetComponent<Renderer>().material = petalMat;
            DestroyCollider(petal);
        }
    }



    GameObject CreateSolidCoreSegment(Transform parent, Vector3 p0, Vector3 p1, float thickness, Material mat, string segName = "CoreTrunkSeg")
    {
        Vector3 mid = (p0 + p1) / 2f;
        float dist = Vector3.Distance(p0, p1);
        if (dist < 0.001f) return null;

        GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        seg.name = segName;
        seg.tag = "Tree";
        seg.transform.SetParent(parent, false);
        seg.transform.localPosition = mid;
        Vector3 dir = (p1 - p0).normalized;
        if (dir.sqrMagnitude > 0.0001f)
        {
            seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
        seg.transform.localScale = new Vector3(thickness, dist / 2f, thickness);
        seg.GetComponent<Renderer>().material = mat ?? barkMaterial;

        // KEEP CapsuleCollider enabled and configured for solid physics & tree damage forwarding
        CapsuleCollider cap = seg.GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            cap.isTrigger = false;
        }
        if (seg.GetComponent<TreeDamageForwarder>() == null)
        {
            seg.AddComponent<TreeDamageForwarder>();
        }

        return seg;
    }

    GameObject CreateBranchSegment(Transform parent, Vector3 p0, Vector3 p1, float thickness, Material mat, string segName = "BranchSeg")
    {
        Vector3 mid = (p0 + p1) / 2f;
        float dist = Vector3.Distance(p0, p1);
        if (dist < 0.001f) return null;

        GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        seg.name = segName;
        seg.tag = "Tree";
        seg.transform.SetParent(parent, false);
        seg.transform.localPosition = mid;
        Vector3 dir = (p1 - p0).normalized;
        if (dir.sqrMagnitude > 0.0001f)
        {
            seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
        seg.transform.localScale = new Vector3(thickness, dist / 2f, thickness);
        seg.GetComponent<Renderer>().material = mat ?? barkMaterial;

        CapsuleCollider cap = seg.GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            cap.isTrigger = true; // Trigger collider so combat attacks hit it and deal tree damage
        }
        if (seg.GetComponent<TreeDamageForwarder>() == null)
        {
            seg.AddComponent<TreeDamageForwarder>();
        }

        return seg;
    }

    GameObject CreateBranchJoint(Transform parent, Vector3 pos, float diameter, Material mat, string jointName = "BranchJoint")
    {
        GameObject knuckle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knuckle.name = jointName;
        knuckle.tag = "Tree";
        knuckle.transform.SetParent(parent, false);
        knuckle.transform.localPosition = pos;
        knuckle.transform.localScale = Vector3.one * diameter;
        knuckle.GetComponent<Renderer>().material = mat ?? barkMaterial;

        SphereCollider sc = knuckle.GetComponent<SphereCollider>();
        if (sc != null)
        {
            sc.isTrigger = true; // Trigger collider so combat attacks hit it and deal tree damage
        }
        if (knuckle.GetComponent<TreeDamageForwarder>() == null)
        {
            knuckle.AddComponent<TreeDamageForwarder>();
        }

        return knuckle;
    }

    void SpawnLeafPuff(Transform parent, Vector3 localPos, float size, Material mat, string puffName)
    {
        GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        puff.name = puffName;
        puff.transform.SetParent(parent, false);
        puff.transform.localPosition = localPos;
        puff.transform.localScale = new Vector3(size, size * 0.65f, size);
        puff.GetComponent<Renderer>().material = mat;
        DestroyCollider(puff);
    }



    // =========================================================================
    // MAJESTIC SOLID & BRAIDED BEANSTALK TRUNK (جذع الشجرة العملاق المصمت ذو المنحنيات والأخاديد والأوراق)
    // =========================================================================
    void BuildMajesticBraidedBeanstalkTrunk()
    {
        GameObject trunkRoot = new GameObject("MajesticBraidedBeanstalkTrunk");
        GameObject treePivotObj = GameObject.Find("TreePivot");
        Transform targetParent = (treePivotObj != null) ? treePivotObj.transform : decorationsRoot.transform;
        trunkRoot.transform.SetParent(targetParent, false);

        MajesticTrunkGrowth growth = trunkRoot.AddComponent<MajesticTrunkGrowth>();

        int steps = 24;

        // Calculate exact surface height of the topmost platform (TopLevel)
        GameObject topLevelObj = null;
        GameObject treeLevels = GameObject.Find("TreeLevels");
        if (treeLevels != null)
        {
            Transform t = treeLevels.transform.Find("TopLevel");
            if (t != null) topLevelObj = t.gameObject;
        }
        if (topLevelObj == null) topLevelObj = GameObject.Find("TopLevel");

        float topLevelHeight = 40.0f;
        if (topLevelObj != null)
        {
            topLevelHeight = SpiralConnector.GetPlatformTopSurfaceY(topLevelObj.transform, 40.0f);
        }

        // The trunk terminates exactly upon reaching the Top Level floor surface
        float maxHeight = topLevelHeight;

        Vector3[] spinePoints = new Vector3[steps + 1];
        for (int s = 0; s <= steps; s++)
        {
            float t = (float)s / steps;
            float y = t * maxHeight;
            spinePoints[s] = GetBeanstalkCenterAtHeight(y, maxHeight);
        }

        // 1. MASSIVE CONTINUOUS SOLID CENTRAL CORE TRUNK WITH REAL COLLISION (اللب الشجري الضخم المصمت)
        // Keeps player from walking through the tree, and catches all projectile/melee attacks
        for (int s = 1; s <= steps; s++)
        {
            float t = (float)s / steps;
            float coreThickness = Mathf.Lerp(3.5f, 2.0f, t);

            Vector3 p0 = spinePoints[s - 1];
            Vector3 p1 = spinePoints[s];

            Material coreMat = GetCoreSegmentMaterial(s);
            GameObject coreSeg = CreateSolidCoreSegment(trunkRoot.transform, p0, p1, coreThickness, coreMat, $"TrunkSolidCore_{s}");
            if (coreSeg != null)
            {
                growth.RegisterExtrudingSegment(coreSeg, p0, p1);
            }

            // Solid knuckle joint at each core node
            GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            joint.name = $"TrunkCoreJoint_{s}";
            joint.tag = "Tree";
            joint.transform.SetParent(trunkRoot.transform, false);
            joint.transform.localPosition = p1;
            joint.transform.localScale = Vector3.one * (coreThickness * 1.08f);
            joint.GetComponent<Renderer>().material = coreMat;
            SphereCollider sc = joint.GetComponent<SphereCollider>();
            if (sc != null) sc.isTrigger = false;
            if (joint.GetComponent<TreeDamageForwarder>() == null)
            {
                joint.AddComponent<TreeDamageForwarder>();
            }
            growth.RegisterNodeJoint(joint, p1, p1.y);
        }

        // 2. 6 BRAIDED HELICAL VINE COLUMNS FUSED TIGHTLY INTO THE CORE (تضفيرات أسطورية مدمجة تشكل حفر وأخاديد طبيعية)
        int vineCount = 6;
        Vector3[][] vinePaths = new Vector3[vineCount][];
        for (int v = 0; v < vineCount; v++)
        {
            vinePaths[v] = new Vector3[steps + 1];
            float angleOffset = (float)v / vineCount * Mathf.PI * 2f;
            Material vineColMat = GetVineColumnMaterial(v);

            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                float y = t * maxHeight;
                Vector3 center = spinePoints[s];

                // Helical winding angle
                float theta = angleOffset + y * 0.22f + Mathf.Sin(y * 0.15f + v * 1.0f) * 0.40f;

                // Radius from center hugs the core tightly so strands fuse directly into the massive wood mass
                float baseR = Mathf.Lerp(1.75f, 1.15f, t);
                float pulse = Mathf.Sin(y * 0.45f + v * 2.1f) * 0.25f;
                float r = baseR + pulse;

                Vector3 radial = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)) * r;
                Vector3 p = center + radial;
                vinePaths[v][s] = p;

                float thickness = Mathf.Lerp(1.35f, 0.85f, t);

                if (s > 0)
                {
                    Vector3 prevP = vinePaths[v][s - 1];
                    GameObject vSeg = CreateBranchSegment(trunkRoot.transform, prevP, p, thickness, vineColMat, $"TrunkBraid_V{v}_S{s}");
                    if (vSeg != null)
                    {
                        growth.RegisterExtrudingSegment(vSeg, prevP, p);
                    }
                }

                GameObject vJoint = CreateBranchJoint(trunkRoot.transform, p, thickness * 1.12f, vineColMat, $"TrunkJoint_V{v}_S{s}");
                if (vJoint != null)
                {
                    growth.RegisterNodeJoint(vJoint, p, p.y);
                }
            }
        }

        // 3. 50+ CLIMBING IVY LEAF CLUSTERS & MOSS SHELVES (أوراق لبلاب كثيفة ورفوف طحالب مورقة تغطي الجذع)
        int ivyNodes = 42;
        for (int i = 0; i < ivyNodes; i++)
        {
            float it = (float)i / ivyNodes;
            float iy = it * (maxHeight - 2.5f) + 1.2f;
            Vector3 center = GetBeanstalkCenterAtHeight(iy, maxHeight);
            float iTheta = it * Mathf.PI * 10f + 0.4f;
            float iR = Mathf.Lerp(2.1f, 1.35f, it);

            Vector3 ivyPos = center + new Vector3(Mathf.Cos(iTheta), 0f, Mathf.Sin(iTheta)) * iR;

            // Ivy leaf cluster
            for (int l = 0; l < 3; l++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = $"TrunkIvyLeaf_{i}_{l}";
                leaf.transform.SetParent(trunkRoot.transform, false);
                leaf.transform.localPosition = ivyPos + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.2f, 0.2f), Random.Range(-0.35f, 0.35f));
                leaf.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
                leaf.transform.localRotation = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0f, 360f), Random.Range(-25f, 25f));
                leaf.GetComponent<Renderer>().material = (l % 2 == 0) ? brightCanopyMaterial : lushCanopyMaterial;
                DestroyCollider(leaf);
                growth.RegisterFoliage(leaf, iy);
            }

            // Moss shelf puffs every few nodes
            if (i % 2 == 0)
            {
                GameObject moss = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moss.name = $"TrunkMossShelf_{i}";
                moss.transform.SetParent(trunkRoot.transform, false);
                moss.transform.localPosition = ivyPos;
                moss.transform.localScale = new Vector3(1.1f, 0.45f, 1.1f);
                moss.GetComponent<Renderer>().material = lushCanopyMaterial;
                DestroyCollider(moss);
                growth.RegisterFoliage(moss, iy);
            }
        }

        // 4. INNER SHADOWED HOLLOW CAVITIES WITH GLOWING SPIRITS (تجاويف شجرية أسطورية متوهجة)
        float[] hollowHeights = new float[] { 3.5f, 12.0f, 22.0f, 32.0f };
        for (int h = 0; h < hollowHeights.Length; h++)
        {
            float hy = hollowHeights[h];
            if (hy >= maxHeight - 3.0f) continue;
            Vector3 center = GetBeanstalkCenterAtHeight(hy, maxHeight);
            float hAngle = h * 1.75f + 0.5f;
            Vector3 cavityPos = center + new Vector3(Mathf.Cos(hAngle), 0f, Mathf.Sin(hAngle)) * 1.6f;

            // Dark shadowy hollow alcove
            GameObject hollowCave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hollowCave.name = $"AncientHollowCave_{h}";
            hollowCave.transform.SetParent(trunkRoot.transform, false);
            hollowCave.transform.localPosition = cavityPos;
            hollowCave.transform.localScale = new Vector3(1.6f, 2.2f, 1.6f);
            hollowCave.GetComponent<Renderer>().material = darkBarkMaterial;
            DestroyCollider(hollowCave);
            growth.RegisterFoliage(hollowCave, hy);

            // Glowing magical spirit wisp tucked inside the hollow
            GameObject wisp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wisp.name = $"HollowSpiritWisp_{h}";
            wisp.transform.SetParent(trunkRoot.transform, false);
            wisp.transform.localPosition = cavityPos + new Vector3(Mathf.Cos(hAngle) * 0.2f, 0.1f, Mathf.Sin(hAngle) * 0.2f);
            wisp.transform.localScale = Vector3.one * 0.45f;
            wisp.GetComponent<Renderer>().material = hollowGlowMaterial;
            DestroyCollider(wisp);
            growth.RegisterFoliage(wisp, hy);

            // Subtle gentle point light inside the hollow
            GameObject wispLightObj = new GameObject($"HollowLight_{h}");
            wispLightObj.transform.SetParent(wisp.transform, false);
            Light wLight = wispLightObj.AddComponent<Light>();
            wLight.type = LightType.Point;
            wLight.color = new Color(0.2f, 0.9f, 0.7f);
            wLight.intensity = 1.2f;
            wLight.range = 4.5f;
            wLight.shadows = LightShadows.None;
        }

        // 5. GIANT ARCUATE BEANSTALK FAN LEAVES (أوراق فاصولياء عملاقة متفرعة مقوسة تحت القمة)
        float[] leafHeights = new float[] { 3.5f, 7.5f, 11.5f, 15.5f, 19.5f, 23.5f, 27.5f, 31.5f, 35.5f };
        for (int l = 0; l < leafHeights.Length; l++)
        {
            float ly = leafHeights[l];
            if (ly >= maxHeight - 2.5f) continue;
            Vector3 center = GetBeanstalkCenterAtHeight(ly, maxHeight);
            float leafAngle = l * 2.1f + 0.8f;
            Vector3 stemDir = new Vector3(Mathf.Cos(leafAngle), 0.28f, Mathf.Sin(leafAngle)).normalized;
            Vector3 stemStart = center + new Vector3(Mathf.Cos(leafAngle), 0f, Mathf.Sin(leafAngle)) * 1.8f;
            Vector3 stemEnd = stemStart + stemDir * 4.2f;

            // Arched leaf stem
            Vector3 stemMid = (stemStart + stemEnd) / 2f + Vector3.up * 0.85f;
            Material petMat = trunkEmeraldSproutVineMat ?? barkMaterial;
            GameObject pet1 = CreateBranchSegment(trunkRoot.transform, stemStart, stemMid, 0.38f, petMat, $"LeafPetiole_{l}_1");
            GameObject pet2 = CreateBranchSegment(trunkRoot.transform, stemMid, stemEnd, 0.26f, petMat, $"LeafPetiole_{l}_2");
            if (pet1 != null) growth.RegisterExtrudingSegment(pet1, stemStart, stemMid);
            if (pet2 != null) growth.RegisterExtrudingSegment(pet2, stemMid, stemEnd);

            // Broad heart-shaped beanstalk leaf blade
            GameObject leafBlade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leafBlade.name = $"GiantBeanstalkLeaf_{l}";
            leafBlade.transform.SetParent(trunkRoot.transform, false);
            leafBlade.transform.localPosition = stemEnd;
            leafBlade.transform.localScale = new Vector3(3.6f, 0.18f, 2.6f);
            leafBlade.transform.localRotation = Quaternion.LookRotation(stemDir) * Quaternion.Euler(15f, 0f, (l % 2 == 0 ? 12f : -12f));
            leafBlade.GetComponent<Renderer>().material = brightCanopyMaterial;
            DestroyCollider(leafBlade);
            growth.RegisterFoliage(leafBlade, ly);
        }

        // 6. GRAND BLOOMING CELESTIAL FLOWER & SACRED CRYSTAL CRADLE AT TOP LEVEL
        BuildTopLevelCrystalFlowerSanctum(trunkRoot.transform, spinePoints[steps], maxHeight);
    }

    void BuildTopLevelCrystalFlowerSanctum(Transform parent, Vector3 centerPos, float floorY)
    {
        GameObject sanctum = new GameObject("CrystalFlowerSanctum");
        sanctum.transform.SetParent(parent, false);
        sanctum.transform.localPosition = centerPos;

        GameObject topLevelObj = null;
        GameObject treeLevels = GameObject.Find("TreeLevels");
        if (treeLevels != null)
        {
            Transform t = treeLevels.transform.Find("TopLevel");
            if (t != null) topLevelObj = t.gameObject;
        }
        if (topLevelObj == null) topLevelObj = GameObject.Find("TopLevel");

        if (topLevelObj != null)
        {
            PlatformDecorationsVisibilityManager visManager = FindAnyObjectByType<PlatformDecorationsVisibilityManager>();
            if (visManager != null)
            {
                visManager.RegisterPair(topLevelObj, sanctum);
            }
            else
            {
                sanctum.SetActive(topLevelObj.activeInHierarchy);
            }
        }

        // 1. Central Carved Sacred Pedestal
        GameObject cradleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cradleObj.name = "CrystalSanctumCradle";
        cradleObj.transform.SetParent(sanctum.transform, false);
        cradleObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        cradleObj.transform.localScale = new Vector3(2.4f, 0.45f, 2.4f);
        cradleObj.GetComponent<Renderer>().material = darkBarkMaterial;

        // Upper altar tier
        GameObject tierObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tierObj.name = "CradleUpperTier";
        tierObj.transform.SetParent(cradleObj.transform, false);
        tierObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        tierObj.transform.localScale = new Vector3(0.75f, 0.18f, 0.75f);
        tierObj.GetComponent<Renderer>().material = stoneMaterial;
        DestroyCollider(tierObj);

        // Glowing Rune Ring in the center of the cradle
        GameObject runeRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        runeRing.name = "CradleRuneRing";
        runeRing.transform.SetParent(cradleObj.transform, false);
        runeRing.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        runeRing.transform.localScale = new Vector3(0.55f, 0.04f, 0.55f);
        runeRing.GetComponent<Renderer>().material = hollowGlowMaterial;
        DestroyCollider(runeRing);

        // 2. 10 Giant Sculpted Outer Petals
        int outerPetalCount = 10;
        for (int p = 0; p < outerPetalCount; p++)
        {
            float angle = (float)p / outerPetalCount * Mathf.PI * 2f;
            Vector3 petalDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            Vector3 pBase = new Vector3(petalDir.x * 1.25f, 0.12f, petalDir.z * 1.25f);
            Vector3 pMid = new Vector3(petalDir.x * 2.45f, 0.75f, petalDir.z * 2.45f);
            Vector3 pTip = new Vector3(petalDir.x * 3.55f, 1.25f, petalDir.z * 3.55f);

            // Sculpted Petal Sections
            CreatePetalSegment(sanctum.transform, pBase, pMid, 1.35f, 0.95f, celestialPetalMaterial, $"OuterPetal_{p}_base");
            CreatePetalSegment(sanctum.transform, pMid, pTip, 0.95f, 0.25f, celestialPetalMaterial, $"OuterPetal_{p}_tip");

            // Decorative glowing petal vein ridge
            CreateBranchSegment(sanctum.transform, pBase, pTip, 0.10f, brightCanopyMaterial, $"PetalVein_{p}");
        }

        // 3. 8 Inner Cupped Petals
        int innerPetalCount = 8;
        for (int ip = 0; ip < innerPetalCount; ip++)
        {
            float angle = ((float)ip / innerPetalCount * Mathf.PI * 2f) + (Mathf.PI / innerPetalCount);
            Vector3 pDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            Vector3 ipBase = new Vector3(pDir.x * 0.95f, 0.28f, pDir.z * 0.95f);
            Vector3 ipTip = new Vector3(pDir.x * 1.80f, 0.95f, pDir.z * 1.80f);

            CreatePetalSegment(sanctum.transform, ipBase, ipTip, 0.85f, 0.22f, celestialPetalMaterial, $"InnerPetal_{ip}");
        }

        // 4. 12 Bioluminescent Pollen Stamens
        int stamenCount = 12;
        for (int s = 0; s < stamenCount; s++)
        {
            float sAngle = (float)s / stamenCount * Mathf.PI * 2f + 0.25f;
            Vector3 sDir = new Vector3(Mathf.Cos(sAngle), 0f, Mathf.Sin(sAngle));
            Vector3 sStart = new Vector3(sDir.x * 1.15f, 0.38f, sDir.z * 1.15f);
            Vector3 sTip = new Vector3(sDir.x * 1.55f, 1.15f + (s % 2 == 0 ? 0.2f : -0.1f), sDir.z * 1.55f);

            CreateBranchSegment(sanctum.transform, sStart, sTip, 0.05f, brightCanopyMaterial, $"StamenFilament_{s}");

            // Glowing Pollen Anther Sphere
            GameObject anther = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            anther.name = $"PollenAnther_{s}";
            anther.transform.SetParent(sanctum.transform, false);
            anther.transform.localPosition = sTip;
            anther.transform.localScale = Vector3.one * 0.26f;
            anther.GetComponent<Renderer>().material = goldenPollenMaterial;
            DestroyCollider(anther);
        }

        // 5. Sacred Floating Light & Aura
        GameObject lightObj = new GameObject("FlowerSanctumLight");
        lightObj.transform.SetParent(sanctum.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        Light sl = lightObj.AddComponent<Light>();
        sl.type = LightType.Point;
        sl.color = new Color(0.2f, 0.95f, 0.85f);
        sl.range = 8.5f;
        sl.intensity = 2.8f;
        sl.shadows = LightShadows.None;
    }

    void CreatePetalSegment(Transform parent, Vector3 p0, Vector3 p1, float widthStart, float widthEnd, Material mat, string name)
    {
        Vector3 mid = (p0 + p1) * 0.5f;
        Vector3 dir = p1 - p0;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        petal.name = name;
        petal.transform.SetParent(parent, false);
        petal.transform.localPosition = mid;
        petal.transform.localRotation = Quaternion.LookRotation(dir);

        float avgWidth = (widthStart + widthEnd) * 0.5f;
        petal.transform.localScale = new Vector3(avgWidth, 0.08f, len);
        petal.GetComponent<Renderer>().material = mat;
        DestroyCollider(petal);
    }

    Vector3 GetBeanstalkCenterAtHeight(float y, float topHeight = 40.35f)
    {
        // Natural serpentine S-curve undulation around (0, 0) in world space
        // Gently bends out and in as it ascends through the levels
        float swayX = Mathf.Sin(y * 0.16f) * 1.6f + Mathf.Sin(y * 0.35f) * 0.6f;
        float swayZ = Mathf.Cos(y * 0.18f) * 1.5f + Mathf.Cos(y * 0.40f) * 0.5f;

        // Dampen sway at ground level (y=0) so roots remain centered at (0, 0, 0)
        float groundAnchor = Mathf.Clamp01(y / 4.0f);
        // Smoothly dampen sway as it approaches TopLevel so the trunk and flower are dead-center at (0, topHeight, 0)
        float topAnchor = Mathf.Clamp01((topHeight - y) / 7.0f);
        float totalAnchor = groundAnchor * topAnchor;

        return new Vector3(swayX * totalAnchor, y, swayZ * totalAnchor);
    }

    // =========================================================================
    // ENCHANTED TREE CANOPY LEVELS OVERHAUL (تحويل المنصات إلى عروش وأغصان شجرية حية)
    // تزيين جميع الطوابق (المستوى 1 و 2 و 3 والقمة) بجذور وعروق بارزة خارجة وأوراق متدلية
    // ومزامنة ظهورها مع فتح الطابق بشكل مضمون 100% عبر مدير نشط
    // =========================================================================
    void DecorateTreeCanopyLevels()
    {
        GameObject levelsContainer = GameObject.Find("TreeLevels");
        List<Transform> levelTransforms = new List<Transform>();

        if (levelsContainer != null)
        {
            for (int i = 0; i < levelsContainer.transform.childCount; i++)
            {
                Transform child = levelsContainer.transform.GetChild(i);
                if (child.name == "Level_0") continue;
                if (child.name.StartsWith("Level_") || child.name.Contains("Top"))
                {
                    levelTransforms.Add(child);
                }
            }
        }

        if (levelTransforms.Count == 0)
        {
            string[] names = new string[] { "Level_1", "Level_2", "Level_3", "TopLevel" };
            foreach (string nm in names)
            {
                GameObject go = GameObject.Find(nm);
                if (go != null) levelTransforms.Add(go.transform);
            }
        }

        if (levelTransforms.Count == 0) return;

        // Clean up any old sheared PlatformCrownDecorations directly on the platforms
        foreach (Transform levelTr in levelTransforms)
        {
            if (levelTr == null) continue;
            Transform existing = levelTr.Find("PlatformCrownDecorations");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
        }

        // Parent all canopy boughs directly to TreePivot (so they tilt with the tree)
        GameObject canopiesRoot = new GameObject("TreeCanopyLevelDecorations");
        GameObject treePivotObj = GameObject.Find("TreePivot");
        Transform targetParent = (treePivotObj != null) ? treePivotObj.transform : decorationsRoot.transform;
        canopiesRoot.transform.SetParent(targetParent, false);
        canopiesRoot.transform.position = Vector3.zero;
        canopiesRoot.transform.rotation = Quaternion.identity;
        canopiesRoot.transform.localScale = Vector3.one;

        // Active visibility manager running every frame on the active canopiesRoot
        PlatformDecorationsVisibilityManager visManager = canopiesRoot.AddComponent<PlatformDecorationsVisibilityManager>();

        foreach (Transform levelTr in levelTransforms)
        {
            if (levelTr == null) continue;

            bool isTop = levelTr.name == "TopLevel" || levelTr.name.Contains("Top");

            // إنشاء وعاء زينة الطابق
            GameObject levelDecor = new GameObject($"{levelTr.name}_CanopyDecorations");
            levelDecor.transform.SetParent(canopiesRoot.transform, false);
            levelDecor.transform.position = Vector3.zero;
            levelDecor.transform.rotation = Quaternion.identity;
            levelDecor.transform.localScale = Vector3.one;

            // تسجيل الطابق مع مدير الرؤية النشط
            visManager.RegisterPair(levelTr.gameObject, levelDecor);

            Vector3 centerPos = levelTr.position;
            Vector3 parentScale = levelTr.lossyScale;
            float worldRadius = Mathf.Max(parentScale.x, parentScale.z) * 0.5f;
            if (worldRadius < 3f) worldRadius = 6f; // Safe minimum radius

            float floorTopY = SpiralConnector.GetPlatformTopSurfaceY(levelTr, centerPos.y + (parentScale.y * 0.5f));
            float floorBottomY = centerPos.y - (parentScale.y * 0.5f);

            // 1. UNDERNEATH GIANT SUPPORT BOUGHS (عروش خشبية مقوسة تسند المنصة من الأسفل)
            int boughCount = worldRadius > 10f ? 8 : (worldRadius > 7f ? 6 : 4);
            for (int b = 0; b < boughCount; b++)
            {
                float boughAngle = (float)b / boughCount * Mathf.PI * 2f + 0.25f;
                Vector3 trunkAttach = new Vector3(centerPos.x, floorBottomY - 2.8f, centerPos.z);

                float midR = worldRadius * 0.50f;
                Vector3 midPoint = new Vector3(centerPos.x + Mathf.Cos(boughAngle) * midR, floorBottomY - 1.4f, centerPos.z + Mathf.Sin(boughAngle) * midR);

                float rimR = worldRadius * 0.90f;
                Vector3 rimPoint = new Vector3(centerPos.x + Mathf.Cos(boughAngle) * rimR, floorBottomY - 0.15f, centerPos.z + Mathf.Sin(boughAngle) * rimR);

                Material bMat = (b % 3 == 0) ? (trunkWarmAmberCedarMat ?? darkBarkMaterial) : ((b % 3 == 1) ? (trunkMossyBarkMat ?? barkMaterial) : (trunkDeepOakMat ?? darkBarkMaterial));

                CreateBranchSegment(levelDecor.transform, trunkAttach, midPoint, 0.95f, bMat, $"{levelTr.name}_Bough_{b}_1");
                CreateBranchSegment(levelDecor.transform, midPoint, rimPoint, 0.65f, bMat, $"{levelTr.name}_Bough_{b}_2");

                CreateBranchJoint(levelDecor.transform, midPoint, 1.10f, bMat, $"{levelTr.name}_Joint_{b}_mid");
                CreateBranchJoint(levelDecor.transform, rimPoint, 0.80f, bMat, $"{levelTr.name}_Joint_{b}_rim");
            }

            // 2. GENTLE CANOPY LEAF PUFFS UNDERNEATH RIM (وسائد أوراق خضراء ناعمة تسند حواف المنصة من الأسفل)
            int cushionCount = Mathf.Clamp(Mathf.RoundToInt(worldRadius * 0.85f), 8, 18);
            for (int c = 0; c < cushionCount; c++)
            {
                float cAngle = (float)c / cushionCount * Mathf.PI * 2f + 0.12f;
                float cDist = worldRadius * 0.96f;
                Vector3 cPos = new Vector3(centerPos.x + Mathf.Cos(cAngle) * cDist, floorBottomY - 0.25f, centerPos.z + Mathf.Sin(cAngle) * cDist);

                GameObject cushion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cushion.name = $"{levelTr.name}_CanopyCushion_{c}";
                cushion.transform.SetParent(levelDecor.transform, false);
                cushion.transform.position = cPos;
                cushion.transform.localScale = new Vector3(2.6f, 1.4f, 2.6f);
                cushion.GetComponent<Renderer>().material = (c % 2 == 0) ? lushCanopyMaterial : brightCanopyMaterial;
                DestroyCollider(cushion);
            }

            // 3. LIVING PERIMETER ROOT COLLAR (طوق جذور خشبية متداخلة يلتف حول حافة المنصة تماماً)
            int rimSegCount = Mathf.Clamp(Mathf.RoundToInt(worldRadius * 1.6f), 16, 36);
            for (int r = 0; r < rimSegCount; r++)
            {
                float a0 = (float)r / rimSegCount * Mathf.PI * 2f;
                float a1 = (float)(r + 1) / rimSegCount * Mathf.PI * 2f;

                float r0 = worldRadius + Mathf.Sin(r * 1.7f) * 0.35f;
                float r1 = worldRadius + Mathf.Sin((r + 1) * 1.7f) * 0.35f;

                float y0 = floorTopY + Mathf.Sin(r * 2.3f) * 0.16f;
                float y1 = floorTopY + Mathf.Sin((r + 1) * 2.3f) * 0.16f;

                Vector3 p0 = new Vector3(centerPos.x + Mathf.Cos(a0) * r0, y0, centerPos.z + Mathf.Sin(a0) * r0);
                Vector3 p1 = new Vector3(centerPos.x + Mathf.Cos(a1) * r1, y1, centerPos.z + Mathf.Sin(a1) * r1);

                Material rimMat = (r % 3 == 0) ? (trunkMossyBarkMat ?? darkBarkMaterial) : ((r % 3 == 1) ? (trunkDeepOakMat ?? darkBarkMaterial) : (trunkWarmAmberCedarMat ?? barkMaterial));

                CreateBranchSegment(levelDecor.transform, p0, p1, 0.46f, rimMat, $"{levelTr.name}_RimRoot_{r}");

                // عقدة جذرية طبيعية عند كل مفصل
                GameObject rootKnot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rootKnot.name = $"{levelTr.name}_RimKnot_{r}";
                rootKnot.transform.SetParent(levelDecor.transform, false);
                rootKnot.transform.position = p0;
                rootKnot.transform.localScale = new Vector3(0.70f, 0.48f, 0.70f);
                rootKnot.GetComponent<Renderer>().material = rimMat;
                DestroyCollider(rootKnot);
            }

            // 4. PROTRUDING ORGANIC WOOD VEINS & ROOT SPURS (عروق وجذور خشبية ضخمة خارجة وبارزة عن الإطار)
            // الميزة الجمالية الأسطورية المطلوبة: عروق تمتد للخارج 1.0 إلى 1.8 متر لتكسر الشكل الأسطواني تماماً
            int spurCount = Mathf.Clamp(Mathf.RoundToInt(worldRadius * 0.65f), 5, 10);
            for (int s = 0; s < spurCount; s++)
            {
                float sAngle = (float)s / spurCount * Mathf.PI * 2f + (s % 2 == 0 ? 0.32f : -0.22f);
                float rimBaseX = centerPos.x + Mathf.Cos(sAngle) * worldRadius;
                float rimBaseZ = centerPos.z + Mathf.Sin(sAngle) * worldRadius;
                Vector3 baseRimPos = new Vector3(rimBaseX, floorTopY - 0.05f, rimBaseZ);

                float reach = UnityEngine.Random.Range(1.15f, 1.85f);
                float elbowAngle = sAngle + UnityEngine.Random.Range(-0.15f, 0.15f);
                Vector3 elbowPos = new Vector3(
                    centerPos.x + Mathf.Cos(elbowAngle) * (worldRadius + reach),
                    floorTopY + UnityEngine.Random.Range(-0.35f, 0.25f),
                    centerPos.z + Mathf.Sin(elbowAngle) * (worldRadius + reach)
                );

                float tipAngle = elbowAngle + (s % 2 == 0 ? 0.22f : -0.22f);
                Vector3 tipPos = new Vector3(
                    centerPos.x + Mathf.Cos(tipAngle) * (worldRadius + reach * 0.82f),
                    elbowPos.y - UnityEngine.Random.Range(0.45f, 0.85f),
                    centerPos.z + Mathf.Sin(tipAngle) * (worldRadius + reach * 0.82f)
                );

                Material spurMat = (s % 3 == 0) ? (trunkDeepOakMat ?? darkBarkMaterial) : ((s % 3 == 1) ? (trunkWarmAmberCedarMat ?? barkMaterial) : (trunkMossyBarkMat ?? darkBarkMaterial));

                CreateBranchSegment(levelDecor.transform, baseRimPos, elbowPos, 0.52f, spurMat, $"{levelTr.name}_Spur_{s}_1");
                CreateBranchSegment(levelDecor.transform, elbowPos, tipPos, 0.34f, spurMat, $"{levelTr.name}_Spur_{s}_2");

                GameObject spurKnot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spurKnot.name = $"{levelTr.name}_SpurKnot_{s}";
                spurKnot.transform.SetParent(levelDecor.transform, false);
                spurKnot.transform.position = elbowPos;
                spurKnot.transform.localScale = new Vector3(0.85f, 0.65f, 0.85f);
                spurKnot.GetComponent<Renderer>().material = spurMat;
                DestroyCollider(spurKnot);

                GameObject spurFoliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spurFoliage.name = $"{levelTr.name}_SpurFoliage_{s}";
                spurFoliage.transform.SetParent(levelDecor.transform, false);
                spurFoliage.transform.position = tipPos;
                spurFoliage.transform.localScale = new Vector3(0.95f, 0.55f, 0.95f);
                spurFoliage.GetComponent<Renderer>().material = (s % 2 == 0) ? brightCanopyMaterial : lushCanopyMaterial;
                DestroyCollider(spurFoliage);
            }

            // 5. HANGING TENDRIL VINES & IVY (لبلاب وعروق معلقة تتدلى من أسفل الحافة)
            int vineCount = Mathf.Clamp(Mathf.RoundToInt(worldRadius * 0.95f), 8, 18);
            for (int v = 0; v < vineCount; v++)
            {
                float vAngle = (float)v / vineCount * Mathf.PI * 2f + 0.18f;
                float vDist = worldRadius + 0.08f;
                Vector3 vTop = new Vector3(centerPos.x + Mathf.Cos(vAngle) * vDist, floorBottomY + 0.15f, centerPos.z + Mathf.Sin(vAngle) * vDist);

                float vineLen = UnityEngine.Random.Range(1.3f, 2.6f);
                Vector3 vMid = vTop + new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), -vineLen * 0.5f, UnityEngine.Random.Range(-0.15f, 0.15f));
                Vector3 vBottom = vTop + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), -vineLen, UnityEngine.Random.Range(-0.25f, 0.25f));

                Material vMat = (v % 2 == 0) ? (trunkMossyBarkMat ?? barkMaterial) : (trunkDeepOakMat ?? darkBarkMaterial);

                CreateBranchSegment(levelDecor.transform, vTop, vMid, 0.20f, vMat, $"{levelTr.name}_HangVine_{v}_1");
                CreateBranchSegment(levelDecor.transform, vMid, vBottom, 0.14f, vMat, $"{levelTr.name}_HangVine_{v}_2");

                GameObject dropLeaf1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dropLeaf1.name = $"{levelTr.name}_VineLeaf_{v}_1";
                dropLeaf1.transform.SetParent(levelDecor.transform, false);
                dropLeaf1.transform.position = vMid;
                dropLeaf1.transform.localScale = new Vector3(0.50f, 0.70f, 0.50f);
                dropLeaf1.GetComponent<Renderer>().material = brightCanopyMaterial;
                DestroyCollider(dropLeaf1);

                GameObject dropLeaf2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dropLeaf2.name = $"{levelTr.name}_VineLeaf_{v}_2";
                dropLeaf2.transform.SetParent(levelDecor.transform, false);
                dropLeaf2.transform.position = vBottom;
                dropLeaf2.transform.localScale = new Vector3(0.60f, 0.85f, 0.60f);
                dropLeaf2.GetComponent<Renderer>().material = lushCanopyMaterial;
                DestroyCollider(dropLeaf2);
            }

            // 6. VERTICAL BARK BUTTRESSES (عروق لحاء عمودية تتسلق جدار الأسطوانة الخارجي)
            int buttressCount = Mathf.Clamp(Mathf.RoundToInt(worldRadius * 0.8f), 6, 16);
            for (int w = 0; w < buttressCount; w++)
            {
                float wAngle = (float)w / buttressCount * Mathf.PI * 2f + 0.08f;
                float wDist = worldRadius + 0.04f;
                Vector3 wBase = new Vector3(centerPos.x + Mathf.Cos(wAngle) * wDist, floorBottomY - 0.2f, centerPos.z + Mathf.Sin(wAngle) * wDist);
                Vector3 wTop = new Vector3(centerPos.x + Mathf.Cos(wAngle) * wDist, floorTopY + 0.10f, centerPos.z + Mathf.Sin(wAngle) * wDist);

                Material wMat = (w % 2 == 0) ? (trunkWarmAmberCedarMat ?? barkMaterial) : (trunkMossyBarkMat ?? darkBarkMaterial);
                CreateBranchSegment(levelDecor.transform, wBase, wTop, 0.42f, wMat, $"{levelTr.name}_WallVein_{w}");
            }

            // 7. LUSH FOLIAGE CLUSTERS & VINES HUGGING THE UPPER RIM (عناقيد أوراق خضراء ساحرة تغلف الحواف)
            int foliageCount = Mathf.Clamp(Mathf.RoundToInt(worldRadius * 1.3f), 12, 26);
            for (int f = 0; f < foliageCount; f++)
            {
                float fAngle = (float)f / foliageCount * Mathf.PI * 2f + 0.18f;
                float fDist = worldRadius + 0.15f;
                float fY = floorTopY + (f % 2 == 0 ? 0.08f : -0.15f);

                Vector3 fPos = new Vector3(centerPos.x + Mathf.Cos(fAngle) * fDist, fY, centerPos.z + Mathf.Sin(fAngle) * fDist);

                GameObject leafCluster = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leafCluster.name = $"{levelTr.name}_RimFoliage_{f}";
                leafCluster.transform.SetParent(levelDecor.transform, false);
                leafCluster.transform.position = fPos;
                leafCluster.transform.localScale = new Vector3(1.85f, 0.80f, 1.55f);
                leafCluster.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), fAngle * Mathf.Rad2Deg, UnityEngine.Random.Range(-15f, 15f));
                leafCluster.GetComponent<Renderer>().material = (f % 3 == 0) ? brightCanopyMaterial : lushCanopyMaterial;
                DestroyCollider(leafCluster);

                // أزهار برية وأبواغ ذهبية متناثرة على الحافة
                if (f % 3 == 0)
                {
                    GameObject flowerOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    flowerOrb.name = $"{levelTr.name}_WildFlower_{f}";
                    flowerOrb.transform.SetParent(levelDecor.transform, false);
                    flowerOrb.transform.position = fPos + Vector3.up * 0.38f;
                    flowerOrb.transform.localScale = Vector3.one * 0.45f;
                    flowerOrb.GetComponent<Renderer>().material = (f % 6 == 0) ? goldenPollenMaterial : (celestialPetalMaterial ?? pinkSakuraMat);
                    DestroyCollider(flowerOrb);
                }
            }
        }
    }

    // =========================================================================
    // DIVERSE FAIRYTALE FOREST RING (Distant Colorful Tree-Line with Huge Clearing)
    // Preserves an unobstructed 100m diameter clearing around the central beanstalk
    // Features Pink Sakura, Golden Birch, Crimson Maple, Emerald Spruce & Spring Oaks
    // =========================================================================
    void BuildDiverseFairytaleForestRing()
    {
        GameObject forestRing = new GameObject("DiverseFairytaleForestRing");
        forestRing.transform.SetParent(decorationsRoot.transform);

        int totalTrees = 48;

        for (int i = 0; i < totalTrees; i++)
        {
            float baseAngle = (float)i / totalTrees * Mathf.PI * 2f;
            float angle = baseAngle + Random.Range(-0.02f, 0.02f);

            // Stagger radius strictly between R = 78m and R = 108m (Dry land only, ZERO trees touching water!)
            int tier = (i % 3);
            float r = 0f;
            if (tier == 0) r = Random.Range(78f, 88f);
            else if (tier == 1) r = Random.Range(88f, 98f);
            else r = Random.Range(98f, 108f);

            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

            // 1. STRICT HERO CLEARING: Keep entire central arena clear (R < 76m - 152m diameter!)
            if (pos.magnitude < 76f) continue;

            // 2. Skip waterfall area
            if (Vector3.Distance(pos, new Vector3(-135f, 0f, -85f)) < 22f) continue;

            // 5 Diverse, colorful fairytale tree species
            int species = i % 5;
            float scale = Random.Range(1.2f, 1.8f);

            switch (species)
            {
                case 0:
                    CreatePinkSakuraTree(forestRing.transform, pos, scale);
                    break;
                case 1:
                    CreateGoldenAutumnBirch(forestRing.transform, pos, scale);
                    break;
                case 2:
                    CreateCrimsonMapleTree(forestRing.transform, pos, scale);
                    break;
                case 3:
                    CreateVibrantEmeraldSpruce(forestRing.transform, pos, scale);
                    break;
                case 4:
                    CreateSpringWoodlandOak(forestRing.transform, pos, scale);
                    break;
            }
        }

        // Scatter 18 mossy fallen logs along the distant forest border
        for (int l = 0; l < 18; l++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(80f, 106f);
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0.32f, Mathf.Sin(a) * r);
            if (Vector3.Distance(pos, new Vector3(-135f, 0f, -85f)) < 18f) continue;

            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = $"ForestFallenLog_{l}";
            log.transform.SetParent(forestRing.transform, false);
            log.transform.position = pos;
            log.transform.rotation = Quaternion.Euler(90f, Random.Range(0, 360f), 0f);
            log.transform.localScale = new Vector3(0.70f, Random.Range(3.2f, 6.0f), 0.70f);
            log.GetComponent<Renderer>().material = darkBarkMaterial ?? barkMaterial;
            DestroyCollider(log);
        }
    }

    void CreatePinkSakuraTree(Transform parent, Vector3 pos, float scale)
    {
        GameObject tree = new GameObject("PinkSakuraTree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;
        tree.transform.localScale = Vector3.one * scale;

        Material trunkMat = darkBarkMaterial ?? barkMaterial;
        Material bloomMat = pinkSakuraMat ?? brightCanopyMaterial;

        // Elegant curved trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 3.2f, 0f);
        trunk.transform.localScale = new Vector3(0.75f, 3.2f, 0.75f);
        trunk.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
        trunk.GetComponent<Renderer>().material = trunkMat;
        DestroyCollider(trunk);

        // 6 lush billowing pink cloud canopy domes
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(0f, 6.5f, 0f),
            new Vector3(-1.6f, 6.0f, 1.0f),
            new Vector3(1.8f, 5.8f, -0.8f),
            new Vector3(0.5f, 6.2f, 1.8f),
            new Vector3(-0.8f, 5.9f, -1.6f),
            new Vector3(0f, 7.8f, 0.2f)
        };
        Vector3[] scales = new Vector3[]
        {
            new Vector3(4.8f, 3.2f, 4.8f),
            new Vector3(3.6f, 2.5f, 3.6f),
            new Vector3(3.8f, 2.6f, 3.8f),
            new Vector3(3.4f, 2.4f, 3.4f),
            new Vector3(3.4f, 2.4f, 3.4f),
            new Vector3(3.0f, 2.2f, 3.0f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = $"SakuraDome_{i}";
            dome.transform.SetParent(tree.transform, false);
            dome.transform.localPosition = offsets[i];
            dome.transform.localScale = scales[i];
            dome.GetComponent<Renderer>().material = bloomMat;
            DestroyCollider(dome);
        }

        // Blossom petals on the grass around base
        for (int p = 0; p < 6; p++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float d = Random.Range(1.2f, 3.8f);
            GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            petal.name = "FallenPetal";
            petal.transform.SetParent(tree.transform, false);
            petal.transform.localPosition = new Vector3(Mathf.Cos(a) * d, 0.04f, Mathf.Sin(a) * d);
            petal.transform.localScale = new Vector3(0.28f, 0.02f, 0.28f);
            petal.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360f), 0f);
            petal.GetComponent<Renderer>().material = bloomMat;
            DestroyCollider(petal);
        }

        tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360f), 0f);
    }

    void CreateGoldenAutumnBirch(Transform parent, Vector3 pos, float scale)
    {
        GameObject tree = new GameObject("GoldenAutumnBirch");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;
        tree.transform.localScale = Vector3.one * scale;

        Material trunkMat = birchBarkMat ?? barkMaterial;
        Material leafMat = goldenAutumnMat ?? canopyMaterial;

        // Slender silver birch trunk
        float trunkH = Random.Range(7.5f, 10.5f);
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "BirchTrunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.45f, 0f);
        trunk.transform.localScale = new Vector3(0.50f, trunkH * 0.45f, 0.50f);
        trunk.GetComponent<Renderer>().material = trunkMat;
        DestroyCollider(trunk);

        // 4 stacked glowing golden canopy domes
        for (int t = 0; t < 4; t++)
        {
            float p = (float)t / 3f;
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = $"GoldDome_{t}";
            dome.transform.SetParent(tree.transform, false);
            dome.transform.localPosition = new Vector3(0f, trunkH * 0.38f + t * 2.2f, 0f);
            float w = Mathf.Lerp(4.5f, 2.0f, p);
            dome.transform.localScale = new Vector3(w, 2.6f, w);
            dome.GetComponent<Renderer>().material = leafMat;
            DestroyCollider(dome);
        }

        tree.transform.rotation = Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(0, 360f), Random.Range(-2f, 2f));
    }

    void CreateCrimsonMapleTree(Transform parent, Vector3 pos, float scale)
    {
        GameObject tree = new GameObject("CrimsonMapleTree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;
        tree.transform.localScale = Vector3.one * scale;

        Material trunkMat = darkBarkMaterial ?? barkMaterial;
        Material leafMat = crimsonMapleMat ?? canopyMaterial;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        trunk.transform.localScale = new Vector3(0.85f, 3.0f, 0.85f);
        trunk.GetComponent<Renderer>().material = trunkMat;
        DestroyCollider(trunk);

        Vector3[] offsets = new Vector3[]
        {
            new Vector3(0f, 6.2f, 0f),
            new Vector3(-1.7f, 5.6f, 1.2f),
            new Vector3(1.8f, 5.4f, -1.0f),
            new Vector3(0f, 7.4f, 0f)
        };
        Vector3[] scales = new Vector3[]
        {
            new Vector3(5.2f, 3.2f, 5.2f),
            new Vector3(3.8f, 2.6f, 3.8f),
            new Vector3(4.0f, 2.7f, 4.0f),
            new Vector3(3.2f, 2.2f, 3.2f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = $"CrimsonDome_{i}";
            dome.transform.SetParent(tree.transform, false);
            dome.transform.localPosition = offsets[i];
            dome.transform.localScale = scales[i];
            dome.GetComponent<Renderer>().material = leafMat;
            DestroyCollider(dome);
        }

        tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360f), 0f);
    }

    void CreateVibrantEmeraldSpruce(Transform parent, Vector3 pos, float scale)
    {
        GameObject tree = new GameObject("EmeraldSpruce");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;
        tree.transform.localScale = Vector3.one * scale;

        Material trunkMat = darkBarkMaterial ?? barkMaterial;
        Material foliageMat = spruceMat ?? canopyMaterial;

        float trunkH = Random.Range(7.0f, 9.5f);
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.45f, 0f);
        trunk.transform.localScale = new Vector3(0.65f, trunkH * 0.45f, 0.65f);
        trunk.GetComponent<Renderer>().material = trunkMat;
        DestroyCollider(trunk);

        int layers = 5;
        for (int l = 0; l < layers; l++)
        {
            float p = (float)l / (layers - 1);
            GameObject tier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tier.name = $"SpruceLayer_{l}";
            tier.transform.SetParent(tree.transform, false);
            tier.transform.localPosition = new Vector3(0f, trunkH * 0.35f + l * 1.6f, 0f);
            float w = Mathf.Lerp(5.4f, 1.5f, p);
            tier.transform.localScale = new Vector3(w, 2.2f, w);
            tier.GetComponent<Renderer>().material = foliageMat;
            DestroyCollider(tier);
        }

        tree.transform.rotation = Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(0, 360f), Random.Range(-2f, 2f));
    }

    void CreateSpringWoodlandOak(Transform parent, Vector3 pos, float scale)
    {
        GameObject tree = new GameObject("SpringWoodlandOak");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;
        tree.transform.localScale = Vector3.one * scale;

        Material trunkMat = darkBarkMaterial ?? barkMaterial;
        Material leafMat = vibrantSpringOakMat ?? canopyMaterial;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        trunk.transform.localScale = new Vector3(1.2f, 3.0f, 1.2f);
        trunk.GetComponent<Renderer>().material = trunkMat;
        DestroyCollider(trunk);

        Vector3[] offsets = new Vector3[]
        {
            new Vector3(0f, 6.0f, 0f),
            new Vector3(-1.8f, 5.4f, 1.0f),
            new Vector3(1.9f, 5.2f, -0.9f),
            new Vector3(0.6f, 5.6f, 1.9f),
            new Vector3(-0.7f, 5.3f, -1.8f),
            new Vector3(0f, 7.2f, 0f)
        };
        Vector3[] scales = new Vector3[]
        {
            new Vector3(5.0f, 3.2f, 5.0f),
            new Vector3(3.8f, 2.6f, 3.8f),
            new Vector3(3.8f, 2.6f, 3.8f),
            new Vector3(3.6f, 2.5f, 3.6f),
            new Vector3(3.6f, 2.5f, 3.6f),
            new Vector3(3.2f, 2.2f, 3.2f)
        };

        for (int c = 0; c < offsets.Length; c++)
        {
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = $"OakCanopy_{c}";
            dome.transform.SetParent(tree.transform, false);
            dome.transform.localPosition = offsets[c];
            dome.transform.localScale = scales[c];
            dome.GetComponent<Renderer>().material = leafMat;
            DestroyCollider(dome);
        }

        tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360f), 0f);
    }

    void CreateClingingPine(Transform parent, Vector3 localPos, float scale, Material foliageMat)
    {
        GameObject pine = new GameObject("ClingingAlpinePine");
        pine.transform.SetParent(parent, false);
        pine.transform.localPosition = localPos;
        pine.transform.localScale = Vector3.one * scale;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(pine.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        trunk.transform.localScale = new Vector3(0.45f, 1.8f, 0.45f);
        trunk.GetComponent<Renderer>().material = barkMaterial;
        DestroyCollider(trunk);

        for (int t = 0; t < 3; t++)
        {
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cone.name = $"Cone_{t}";
            cone.transform.SetParent(pine.transform, false);
            cone.transform.localPosition = new Vector3(0f, 3.2f + t * 1.4f, 0f);
            cone.transform.localScale = new Vector3(3.2f - t * 0.8f, 2.0f, 3.2f - t * 0.8f);
            cone.GetComponent<Renderer>().material = foliageMat;
            DestroyCollider(cone);
        }
    }

    // =========================================================================
    // MAJESTIC ALPINE MOUNTAIN RING (REAL 3D SLOPED MOUNTAINS, ZERO SPHERES!)
    // Generates authentic sloped mountain massifs with snow caps encircling 360 degrees
    // =========================================================================
    void BuildMajesticMountainRing()
    {
        GameObject mountainRing = new GameObject("MajesticAlpineMountainRing");
        mountainRing.transform.SetParent(decorationsRoot.transform);

        // 1. INNER ALPINE MOUNTAIN CHAIN (16 connected massifs, heights 85m - 110m)
        // Radii: 188m - 200m. Encircles the distant forest with authentic sloped massifs.
        int innerCount = 16;
        for (int i = 0; i < innerCount; i++)
        {
            float angle = (float)i / innerCount * Mathf.PI * 2f;
            float r = 192f + ((i % 2 == 0) ? 6f : -6f) + Random.Range(-3f, 3f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

            // Skip placing right over the waterfall mountain at (-135, 0, -85)
            if (Vector3.Distance(pos, new Vector3(-135f, 0f, -85f)) < 35f) continue;

            float baseRx = Random.Range(36f, 44f);
            float baseRz = Random.Range(36f, 44f);
            float height = Random.Range(85f, 110f);
            float pOffX = Random.Range(-6f, 6f);
            float pOffZ = Random.Range(-6f, 6f);

            CreateProceduralMountainMassif($"InnerMountain_{i}", mountainRing.transform, pos, baseRx, baseRz, height, pOffX, pOffZ, i * 19);
        }

        // 2. OUTER HORIZON TITAN MOUNTAINS (14 colossal massifs, heights 130m - 170m)
        // Radii: 220m - 250m. Closes all background gaps with gleaming snow-capped alpine horns 360 degrees
        int outerCount = 14;
        for (int j = 0; j < outerCount; j++)
        {
            float angle = ((float)j + 0.5f) / outerCount * Mathf.PI * 2f + Random.Range(-0.03f, 0.03f);
            float r = Random.Range(225f, 255f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

            float baseRx = Random.Range(40f, 45f);
            float baseRz = Random.Range(40f, 45f);
            float height = Random.Range(130f, 170f);

            CreateProceduralMountainMassif($"HorizonTitan_{j}", mountainRing.transform, pos, baseRx, baseRz, height, 0f, 0f, j * 37 + 200);
        }
    }

    // =========================================================================
    // ENCHANTED BOUNDARY RIVER RING & GREAT MOAT
    // 360-degree flowing turquoise circular river (R = 115m to 148m, width = 33m)
    // Features:
    // 1. Solid procedural riverbed mesh with Ground physics collider (seamless ground connection)
    // 2. Animated rushing river current with UV scrolling (AnimatedRiverRing)
    // 3. Shallow wading (R = 115m - 120m) & Deep drowning hazard (R >= 120m) with instant safe sanctuary return
    // 4. Natural riverbanks with smooth boulders and lush reeds/cattails
    // 5. Floating water lily pads with blooming glowing lotuses
    // 6. Living animated river frogs hopping on lily pads (EnchantedRiverFrog)
    // =========================================================================
    void BuildEnchantedBoundaryRiverRing()
    {
        GameObject riverRingObj = new GameObject("EnchantedBoundaryRiverRing");
        riverRingObj.transform.SetParent(decorationsRoot.transform);

        float innerR = 115f;
        float outerR = 148f;
        int segments = 128;
        float waterY = 0.10f;

        // 1. Solid River Bed Mesh (قاع النهر المصمت بفيزياء أرضية حقيقية لمنع السقوط في الفراغ نهائياً)
        float bedInnerR = 112f;
        float bedOuterR = 150f;
        float bedY = -0.05f;

        Mesh bedMesh = new Mesh();
        bedMesh.name = "EnchantedRiver_BedMesh";

        Vector3[] bedVertices = new Vector3[(segments + 1) * 2];
        Vector2[] bedUvs = new Vector2[(segments + 1) * 2];
        Vector3[] bedNormals = new Vector3[(segments + 1) * 2];
        int[] bedTriangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            int idxIn = i * 2;
            int idxOut = i * 2 + 1;

            bedVertices[idxIn] = new Vector3(cos * bedInnerR, bedY, sin * bedInnerR);
            bedVertices[idxOut] = new Vector3(cos * bedOuterR, bedY, sin * bedOuterR);

            bedUvs[idxIn] = new Vector2(t * 16f, 0f);
            bedUvs[idxOut] = new Vector2(t * 16f, 1f);

            bedNormals[idxIn] = Vector3.up;
            bedNormals[idxOut] = Vector3.up;
        }

        int bTriIdx = 0;
        for (int i = 0; i < segments; i++)
        {
            int vIn = i * 2;
            int vOut = i * 2 + 1;
            int nextIn = (i + 1) * 2;
            int nextOut = (i + 1) * 2 + 1;

            bedTriangles[bTriIdx++] = vIn;
            bedTriangles[bTriIdx++] = vOut;
            bedTriangles[bTriIdx++] = nextIn;

            bedTriangles[bTriIdx++] = vOut;
            bedTriangles[bTriIdx++] = nextOut;
            bedTriangles[bTriIdx++] = nextIn;
        }

        bedMesh.vertices = bedVertices;
        bedMesh.uv = bedUvs;
        bedMesh.normals = bedNormals;
        bedMesh.triangles = bedTriangles;
        bedMesh.RecalculateBounds();

        GameObject riverBedObj = new GameObject("RiverBedSolid");
        riverBedObj.transform.SetParent(riverRingObj.transform, false);
        riverBedObj.tag = "Ground";
        int gLayer = LayerMask.NameToLayer("Ground");
        riverBedObj.layer = gLayer >= 0 ? gLayer : 8;

        MeshFilter bedMf = riverBedObj.AddComponent<MeshFilter>();
        bedMf.sharedMesh = bedMesh;

        MeshRenderer bedMr = riverBedObj.AddComponent<MeshRenderer>();
        bedMr.sharedMaterial = mountainRockMat ?? stoneMaterial;

        MeshCollider bedCol = riverBedObj.AddComponent<MeshCollider>();
        bedCol.sharedMesh = bedMesh;

        // 2. Procedural Circular River Water Ring Mesh
        Mesh riverMesh = new Mesh();
        riverMesh.name = "EnchantedRiver_RingMesh";

        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[(segments + 1) * 2];
        Vector3[] normals = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6 * 2]; // Double-sided

        float uvTileX = 18f;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            int idxIn = i * 2;
            int idxOut = i * 2 + 1;

            vertices[idxIn] = new Vector3(cos * innerR, waterY, sin * innerR);
            vertices[idxOut] = new Vector3(cos * outerR, waterY, sin * outerR);

            uvs[idxIn] = new Vector2(t * uvTileX, 0f);
            uvs[idxOut] = new Vector2(t * uvTileX, 1f);

            normals[idxIn] = Vector3.up;
            normals[idxOut] = Vector3.up;
        }

        int triIdx = 0;
        for (int i = 0; i < segments; i++)
        {
            int vIn = i * 2;
            int vOut = i * 2 + 1;
            int nextIn = (i + 1) * 2;
            int nextOut = (i + 1) * 2 + 1;

            // Front face (upward)
            triangles[triIdx++] = vIn;
            triangles[triIdx++] = vOut;
            triangles[triIdx++] = nextIn;

            triangles[triIdx++] = vOut;
            triangles[triIdx++] = nextOut;
            triangles[triIdx++] = nextIn;

            // Back face (downward)
            triangles[triIdx++] = nextIn;
            triangles[triIdx++] = vOut;
            triangles[triIdx++] = vIn;

            triangles[triIdx++] = nextIn;
            triangles[triIdx++] = nextOut;
            triangles[triIdx++] = vOut;
        }

        riverMesh.vertices = vertices;
        riverMesh.uv = uvs;
        riverMesh.normals = normals;
        riverMesh.triangles = triangles;
        riverMesh.RecalculateBounds();

        GameObject waterSurfaceObj = new GameObject("RiverWaterSurface");
        waterSurfaceObj.transform.SetParent(riverRingObj.transform, false);

        MeshFilter mf = waterSurfaceObj.AddComponent<MeshFilter>();
        mf.sharedMesh = riverMesh;

        MeshRenderer mr = waterSurfaceObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = sharedWaterStreamMat ?? waterMaterial;

        // Attach UV scrolling animation script
        AnimatedRiverRing riverAnim = waterSurfaceObj.AddComponent<AnimatedRiverRing>();
        riverAnim.flowSpeed = 0.40f;
        riverAnim.swirlSpeed = 0.06f;

        // Attach Drowning Hazard monitor
        EnchantedRiverHazard hazard = riverRingObj.AddComponent<EnchantedRiverHazard>();
        hazard.shallowRadius = innerR;
        hazard.deepRadius = 120f;
        hazard.outerRadius = outerR;
        hazard.waterSurfaceY = 0.45f;

        // 3. Floating Lily Pads and Sacred Lotus Flowers (Clean & Lightweight)
        BuildLilyPadsAndLotuses(riverRingObj.transform, innerR, outerR);
    }

    void BuildLilyPadsAndLotuses(Transform parent, float innerR, float outerR)
    {
        Material lilyMat = canopyMaterial;
        Material lotusMat = pinkSakuraMat ?? celestialPetalMaterial;
        Material pollenMat = goldenPollenMaterial;

        int totalPads = 12;
        for (int i = 0; i < totalPads; i++)
        {
            float a = (float)i / totalPads * Mathf.PI * 2f + Random.Range(-0.05f, 0.05f);
            float r = Random.Range(innerR + 4.0f, outerR - 5.0f);
            Vector3 padPos = new Vector3(Mathf.Cos(a) * r, 0.12f, Mathf.Sin(a) * r);

            // 1. Lily Pad
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = $"LilyPad_{i}";
            pad.transform.SetParent(parent, false);
            pad.transform.position = padPos;
            float padScale = Random.Range(1.4f, 2.0f);
            pad.transform.localScale = new Vector3(padScale, 0.015f, padScale);
            pad.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            pad.GetComponent<Renderer>().material = lilyMat;
            DestroyCollider(pad);

            // 2. Sacred Water Lotus Flower
            if (i % 2 == 0)
            {
                CreateLotusFlower(pad.transform, lotusMat, pollenMat);
            }
        }
    }

    void CreateLotusFlower(Transform padTransform, Material petalMat, Material pollenMat)
    {
        GameObject lotus = new GameObject("LotusFlower");
        lotus.transform.SetParent(padTransform, false);
        lotus.transform.localPosition = new Vector3(Random.Range(-0.15f, 0.15f), 0.02f, Random.Range(-0.15f, 0.15f));
        lotus.transform.localScale = Vector3.one * 0.45f;

        // Golden center pistil
        GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        center.name = "Pistil";
        center.transform.SetParent(lotus.transform, false);
        center.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        center.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
        center.GetComponent<Renderer>().material = pollenMat;
        DestroyCollider(center);

        // 8 radiating petals
        for (int p = 0; p < 8; p++)
        {
            float petAngle = p * 45f;
            GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            petal.name = $"Petal_{p}";
            petal.transform.SetParent(lotus.transform, false);
            petal.transform.localRotation = Quaternion.Euler(-25f, petAngle, 0f);
            petal.transform.localPosition = Quaternion.Euler(0f, petAngle, 0f) * new Vector3(0f, 0.10f, 0.28f);
            petal.transform.localScale = new Vector3(0.20f, 0.04f, 0.38f);
            petal.GetComponent<Renderer>().material = petalMat;
            DestroyCollider(petal);
        }
    }

    void EnhanceOriginalPlayerRoots(GameObject rootsContainer)
    {
        if (rootsContainer == null) return;
        rootsContainer.SetActive(true);

        Material barkMat = darkBarkMaterial ?? barkMaterial;
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 8;

        for (int i = 0; i < rootsContainer.transform.childCount; i++)
        {
            Transform child = rootsContainer.transform.GetChild(i);
            child.gameObject.SetActive(true);
            child.gameObject.tag = "Ground";
            child.gameObject.layer = groundLayer;

            Collider col = child.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            Material rootMat = (i % 3 == 0) ? (trunkDeepOakMat ?? darkBarkMaterial) : ((i % 3 == 1) ? (trunkWarmAmberCedarMat ?? barkMaterial) : (trunkMossyBarkMat ?? darkBarkMaterial));

            MeshRenderer mr = child.GetComponent<MeshRenderer>();
            if (mr != null && rootMat != null)
            {
                mr.material = rootMat;
                mr.enabled = true;
            }

            foreach (MeshRenderer subMr in child.GetComponentsInChildren<MeshRenderer>(true))
            {
                subMr.gameObject.SetActive(true);
                subMr.gameObject.tag = "Ground";
                subMr.gameObject.layer = groundLayer;
                if (rootMat != null) subMr.material = rootMat;
                Collider c = subMr.GetComponent<Collider>();
                if (c != null) c.enabled = true;
            }

            // Decorative moss/ivy leaf accents along the root edges
            AddDecorativeFoliageToRoot(child);
        }
    }

    void AddDecorativeFoliageToRoot(Transform rootTrans)
    {
        if (rootTrans == null) return;
        if (rootTrans.Find("RootDecorFoliage") != null) return;

        GameObject folGroup = new GameObject("RootDecorFoliage");
        folGroup.transform.SetParent(rootTrans, false);

        Material folMat = lushCanopyMaterial ?? canopyMaterial;

        // Place 6 small leafy clusters along the side edges
        for (int l = 0; l < 6; l++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = $"RootLeaf_{l}";
            leaf.transform.SetParent(folGroup.transform, false);
            float t = ((float)l / 6f) - 0.5f;
            float side = (l % 2 == 0) ? 0.55f : -0.55f;
            leaf.transform.localPosition = new Vector3(side, 0.45f, t * 0.9f);
            leaf.transform.localRotation = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
            leaf.transform.localScale = new Vector3(0.25f, 0.08f, 0.35f);
            leaf.GetComponent<Renderer>().material = folMat;
            DestroyCollider(leaf);
        }
    }



    void TuneLightingAndAtmosphere()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] lights = FindObjectsByType<Light>();
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sun = l;
                    break;
                }
            }
        }

        if (sun != null)
        {
            sun.color = new Color(1.0f, 0.94f, 0.84f); // Warm fairytale golden sunlight
            sun.intensity = 1.30f;
            sun.transform.rotation = Quaternion.Euler(46f, -34f, 0);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.70f, 0.98f);     // Radiant enchanted azure sky
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.82f, 0.52f); // Vibrant lush emerald reflection
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.40f, 0.22f);  // Earth moss tone

        // Soft, dreamy distance aerial perspective (subtle fantasy horizon mist without obscuring combat)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.0022f;
        RenderSettings.fogColor = new Color(0.72f, 0.88f, 0.98f);

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.40f, 0.72f, 0.98f); // Radiant fairytale azure blue sky!
            Camera.main.targetDisplay = 0;
        }
    }

    void OnDestroy()
    {
        if (grassMaterial != null) { if (grassMaterial.mainTexture != null) Destroy(grassMaterial.mainTexture); Destroy(grassMaterial); }
        if (barkMaterial != null) { if (barkMaterial.mainTexture != null) Destroy(barkMaterial.mainTexture); Destroy(barkMaterial); }
        if (canopyMaterial != null) { if (canopyMaterial.mainTexture != null) Destroy(canopyMaterial.mainTexture); Destroy(canopyMaterial); }
        if (stoneMaterial != null) { if (stoneMaterial.mainTexture != null) Destroy(stoneMaterial.mainTexture); Destroy(stoneMaterial); }
        if (waterMaterial != null) Destroy(waterMaterial);
        if (mushroomRedMaterial != null) Destroy(mushroomRedMaterial);
        if (mushroomBlueMaterial != null) Destroy(mushroomBlueMaterial);
        if (darkBarkMaterial != null) Destroy(darkBarkMaterial);
        if (lushCanopyMaterial != null) Destroy(lushCanopyMaterial);
        if (brightCanopyMaterial != null) Destroy(brightCanopyMaterial);
        if (hollowGlowMaterial != null) Destroy(hollowGlowMaterial);
    }

    Texture2D GenerateNoiseTexture(int width, int height, Color colorA, Color colorB, float scale, bool isGrass)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width * scale;
                float v = (float)y / height * scale;
                float n1 = Mathf.PerlinNoise(u, v);
                float n2 = Mathf.PerlinNoise(u * 2.5f + 12.3f, v * 2.5f + 45.6f) * 0.5f;
                float blend = Mathf.Clamp01(n1 + n2 * 0.4f);

                Color col = Color.Lerp(colorA, colorB, blend);
                if (isGrass && (x + y) % 5 == 0)
                {
                    col = Color.Lerp(col, new Color(0.4f, 0.85f, 0.3f), 0.25f);
                }
                pixels[y * width + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    Texture2D GenerateBarkTexture(int width, int height, Color baseColor, Color ridgeColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width * 12f;
                float v = (float)y / height * 2.5f; // Vertical stretch for wood grain
                float n = Mathf.PerlinNoise(u, v);
                float ridges = Mathf.Sin(u * 3.14159f * 2f + n * 3f) * 0.5f + 0.5f;

                Color c = Color.Lerp(baseColor, ridgeColor, ridges * 0.7f + n * 0.3f);
                pixels[y * width + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    Texture2D GenerateWaterfallStreamTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        Color deepWater = new Color(0.20f, 0.65f, 0.95f, 0.88f);
        Color lightAqua = new Color(0.48f, 0.88f, 1.0f, 0.94f);
        Color foamWhite = new Color(0.96f, 0.99f, 1.0f, 0.98f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                // Heavily stretched in V (vertical) to create long rushing water ribbons & torrent streaks
                float n1 = Mathf.PerlinNoise(u * 12f, v * 1.8f);
                float n2 = Mathf.PerlinNoise(u * 24f + 3.7f, v * 3.2f + 9.1f) * 0.5f;
                float streak = Mathf.Clamp01(n1 + n2);

                Color col = Color.Lerp(deepWater, lightAqua, streak);

                // High crests become churning white foam
                if (streak > 0.64f)
                {
                    float foamAmount = (streak - 0.64f) / 0.36f;
                    col = Color.Lerp(col, foamWhite, foamAmount * 0.92f);
                }

                pixels[y * width + x] = col;
            }
        }

        tex.wrapMode = TextureWrapMode.Repeat;
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
}

/// <summary>
/// مدير مراقبة ومزامنة ظهور ديكورات الطوابق وعروش الشجرة
/// يعمل بشكل دائم ومستمر على كائن نشط (Active) ليضمن تفعيل زينة الطوابق 2 و 3 والقمة فور فتحها
/// </summary>
public class PlatformDecorationsVisibilityManager : MonoBehaviour
{
    [System.Serializable]
    public struct LevelDecorPair
    {
        public GameObject targetLevel;
        public GameObject decorObject;
    }

    public List<LevelDecorPair> watchedPairs = new List<LevelDecorPair>();

    public void RegisterPair(GameObject targetLevel, GameObject decorObject)
    {
        if (targetLevel == null || decorObject == null) return;
        watchedPairs.Add(new LevelDecorPair { targetLevel = targetLevel, decorObject = decorObject });
        decorObject.SetActive(targetLevel.activeInHierarchy);
    }

    void Update()
    {
        for (int i = 0; i < watchedPairs.Count; i++)
        {
            var pair = watchedPairs[i];
            if (pair.targetLevel != null && pair.decorObject != null)
            {
                bool active = pair.targetLevel.activeInHierarchy;
                if (pair.decorObject.activeSelf != active)
                {
                    pair.decorObject.SetActive(active);
                }
            }
        }
    }
}

/// <summary>
/// مزامنة ظهور الديكورات والغرف مع تفعيل الطابق الخاص بها
/// بحيث تظل الديكورات مخفية إذا كان الطابق مغلقاً، وتظهر فور فتح الطابق
/// </summary>
public class LevelVisibilitySync : MonoBehaviour
{
    public GameObject targetLevel;

    void Update()
    {
        if (targetLevel != null)
        {
            bool shouldBeActive = targetLevel.activeInHierarchy;
            if (gameObject.activeSelf != shouldBeActive)
            {
                gameObject.SetActive(shouldBeActive);
            }
        }
    }
}

