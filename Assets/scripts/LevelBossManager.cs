using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// مدير زعماء المستويات:
/// يضمن وجود زعيم قوي وضخم في كل مستوى من مستويات الشجرة (Level 1, Level 2, Level 3)
/// ويستثني الـ TopLevel تماماً كما طلب المستخدم!
/// يعمل تلقائياً عند بدء اللعبة دون الحاجة لأي إعداد يدوي.
/// </summary>
public class LevelBossManager : MonoBehaviour
{
    private static LevelBossManager _instance;
    public static LevelBossManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<LevelBossManager>();
                if (_instance == null)
                {
                    GameObject managerObj = new GameObject("LevelBossManager");
                    _instance = managerObj.AddComponent<LevelBossManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Bot Prefab For Boss Generation")]
    public GameObject botPrefab;

    [Header("Tree Levels Parent")]
    public string treeLevelsParentName = "TreeLevels";

    private bool bossesSpawned = false;
    private readonly HashSet<int> defeatedBossLevels = new HashSet<int>();
    private bool allThreeBossesTriggered = false;

    public const int TotalBossCount = 3;
    public int DefeatedBossCount => defeatedBossLevels.Count;
    public bool AreAllBossesDefeated => DefeatedBossCount >= TotalBossCount;
    public bool IsBossLevelDefeated(int level) => defeatedBossLevels.Contains(level);

    public static event System.Action<string, string, PlayerMovement.TeamType, int> OnSingleBossDefeated;
    public static event System.Action OnAllThreeBossesDefeated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            LevelBossManager inScene = FindAnyObjectByType<LevelBossManager>();
            if (inScene != null)
            {
                Instance = inScene;
                return;
            }
            GameObject managerObj = new GameObject("LevelBossManager");
            managerObj.AddComponent<LevelBossManager>();
        }
    }

    public static void ClearStaticEvents()
    {
        OnSingleBossDefeated = null;
        OnAllThreeBossesDefeated = null;
    }

    public static void ResetStaticState()
    {
        ClearStaticEvents();
        if (Instance != null)
        {
            Instance.ResetBossManagerState();
        }
    }

    public void ResetBossManagerState()
    {
        defeatedBossLevels.Clear();
        allThreeBossesTriggered = false;
        bossesSpawned = false;
        if (tierSpawned != null)
        {
            System.Array.Clear(tierSpawned, 0, tierSpawned.Length);
        }
        if (configuredBossInstanceIDs != null)
        {
            configuredBossInstanceIDs.Clear();
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        defeatedBossLevels.Clear();
        allThreeBossesTriggered = false;
        bossesSpawned = false;
        if (tierSpawned != null) System.Array.Clear(tierSpawned, 0, tierSpawned.Length);
        if (configuredBossInstanceIDs != null) configuredBossInstanceIDs.Clear();

        FindBotPrefab();
        CleanupDuplicateBossesAndAdoptSceneBosses();
    }

    void OnEnable()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        ClearStaticEvents();
    }

    void Start()
    {
        FindBotPrefab();
        CleanupDuplicateBossesAndAdoptSceneBosses();
        // نُؤخر بناء NavMesh لإعطاء SpiralConnector وقتًا لإنشاء الممرات أولًا
        Invoke(nameof(RebuildNavigationMesh), 0.8f);
    }

    public void RegisterBossDefeated(int levelNumber, string bossTitle, string killerName, PlayerMovement.TeamType killerTeam)
    {
        if (defeatedBossLevels.Contains(levelNumber)) return;
        defeatedBossLevels.Add(levelNumber);

        // تنظيف أي نسخة متبقية أو مكررة لهذا المستوى لضمان عدم بقاء أي زعيم إضافي
        LevelBoss[] allBosses = FindObjectsByType<LevelBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in allBosses)
        {
            if (b != null && b.levelNumber == levelNumber)
            {
                Health h = b.GetComponent<Health>();
                if (h == null || !h.isDead)
                {
                    Destroy(b.gameObject);
                }
            }
        }

        // مكافأة الفريق الذي وجه الضربة القاضية للزعيم على الشجرة
        TreeGrowth tree = FindAnyObjectByType<TreeGrowth>();
        if (tree != null)
        {
            if (killerTeam == PlayerMovement.TeamType.Protector)
            {
                tree.Heal(350f);
                tree.ApplyBossKillGrowthBoost(1.6f);
            }
            else if (killerTeam == PlayerMovement.TeamType.Destroyer)
            {
                tree.TakeDamage(260f);
            }
        }

        OnSingleBossDefeated?.Invoke(bossTitle, killerName, killerTeam, DefeatedBossCount);

        // فتح المستوى التالي والممر المناسب وإيقاظ الزعيم التالي وتحديث شبكة الملاحة فوراً للبوتات
        AdvanceToNextLevelAfterBossKill(levelNumber, bossTitle);

        if (AreAllBossesDefeated && !allThreeBossesTriggered)
        {
            TriggerAllThreeBossesUnseal();
        }
    }

    /// <summary>
    /// تقدم مراحل اللعبة تلقائياً عند هزيمة زعيم كل طابق:
    /// 1. فتح ممر الصعود للطابق التالي
    /// 2. تفعيل منصة الطابق
    /// 3. إيقاظ الزعيم التالي فوراً
    /// 4. إعادة بناء NavMesh ليصعد البوتات فوراً للطابق التالي
    /// </summary>
    private void AdvanceToNextLevelAfterBossKill(int defeatedLevel, string bossTitle)
    {
        TreeLevelManager tlm = FindAnyObjectByType<TreeLevelManager>();
        if (tlm != null)
        {
            tlm.UnlockLevel(defeatedLevel);
        }

        if (defeatedLevel == 1)
        {
            // 1. فتح ممرات الصعود من الطابق 1 إلى الطابق 2
            if (SpiralConnector.Instance != null) SpiralConnector.Instance.SetSectionUnlocked(1, true);
            else FindAnyObjectByType<SpiralConnector>()?.SetSectionUnlocked(1, true);

            // 2. تفعيل منصة الطابق 2
            GameObject treeLevelsObj = GameObject.Find(treeLevelsParentName);
            GameObject level2Obj = treeLevelsObj != null ? treeLevelsObj.transform.Find("Level_2")?.gameObject : GameObject.Find("Level_2");
            if (level2Obj != null) level2Obj.SetActive(true);

            // 3. إيقاظ الزعيم الثاني (Inferno Titan)
            AwakenBossForLevel(2);

            // 4. تحديث شبكة الملاحة ليصعد البوتات فوراً
            RebuildNavigationMesh();

            PlayerProgressionAndShopUI.Instance?.ShowNotification("⚔️ LEVEL 1 BOSS SLAIN! Paths to Level 2 are OPEN!");
            Debug.Log("🚀 [LevelBossManager] Level 1 Boss slain! Level 2 paths unlocked & Boss 2 awakened!");
        }
        else if (defeatedLevel == 2)
        {
            // 1. فتح ممرات الصعود من الطابق 2 إلى الطابق 3
            if (SpiralConnector.Instance != null) SpiralConnector.Instance.SetSectionUnlocked(2, true);
            else FindAnyObjectByType<SpiralConnector>()?.SetSectionUnlocked(2, true);

            // 2. تفعيل منصة الطابق 3
            GameObject treeLevelsObj = GameObject.Find(treeLevelsParentName);
            GameObject level3Obj = treeLevelsObj != null ? treeLevelsObj.transform.Find("Level_3")?.gameObject : GameObject.Find("Level_3");
            if (level3Obj != null) level3Obj.SetActive(true);

            // 3. إيقاظ الزعيم الثالث (Ancient Overlord)
            AwakenBossForLevel(3);

            // 4. تحديث شبكة الملاحة
            RebuildNavigationMesh();

            PlayerProgressionAndShopUI.Instance?.ShowNotification("⚔️ LEVEL 2 BOSS SLAIN! Paths to Level 3 are OPEN!");
            Debug.Log("🚀 [LevelBossManager] Level 2 Boss slain! Level 3 paths unlocked & Boss 3 awakened!");
        }
        else if (defeatedLevel == 3)
        {
            // 1. فتح ممرات الصعود من الطابق 3 إلى قمة الشجرة (TopLevel)
            if (SpiralConnector.Instance != null) SpiralConnector.Instance.SetSectionUnlocked(3, true);
            else FindAnyObjectByType<SpiralConnector>()?.SetSectionUnlocked(3, true);

            // 2. تفعيل منصة القمة
            GameObject treeLevelsObj = GameObject.Find(treeLevelsParentName);
            GameObject topLevelObj = treeLevelsObj != null ? treeLevelsObj.transform.Find("TopLevel")?.gameObject : GameObject.Find("TopLevel");
            if (topLevelObj != null) topLevelObj.SetActive(true);

            // 3. تحديث شبكة الملاحة
            RebuildNavigationMesh();

            PlayerProgressionAndShopUI.Instance?.ShowNotification("👑 ALL BOSSES SLAIN! The path to the Sacred Crystal is OPEN!");
            Debug.Log("🚀 [LevelBossManager] Level 3 Boss slain! TopLevel paths unlocked & Sacred Crystal unsealed!");
        }
    }

    /// <summary>
    /// إيقاظ أو توليد زعيم المستوى المحدد فوراً فوق منصته
    /// </summary>
    public void AwakenBossForLevel(int level)
    {
        if (level < 1 || level > 3) return;
        if (defeatedBossLevels.Contains(level)) return;

        string levelName = level switch
        {
            1 => "Level_1",
            2 => "Level_2",
            3 => "Level_3",
            _ => $"Level_{level}"
        };

        GameObject treeLevelsObj = GameObject.Find(treeLevelsParentName);
        Transform levelTr = null;
        if (treeLevelsObj != null) levelTr = treeLevelsObj.transform.Find(levelName);
        if (levelTr == null)
        {
            GameObject d = GameObject.Find(levelName);
            if (d != null) levelTr = d.transform;
        }

        if (levelTr != null)
        {
            levelTr.gameObject.SetActive(true);
        }

        float expectedY = level switch
        {
            1 => 6.5f,
            2 => 16.5f,
            3 => 26.5f,
            _ => 6.5f
        };

        Vector3 spawnPos = (levelTr != null)
            ? new Vector3(levelTr.position.x, expectedY, levelTr.position.z)
            : new Vector3(0f, expectedY, 0f);

        // بناء شبكة الملاحة أولاً لضمان وجود سطح ملاحة حقيقي تحت قدمي الزعيم الجديد لمنع سقوطه للطابق السفلي
        RebuildNavigationMesh();

        // البحث عن الزعيم الموجود مسبقاً
        LevelBoss[] allBosses = FindObjectsByType<LevelBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        LevelBoss targetBoss = null;
        foreach (var b in allBosses)
        {
            if (b != null && b.levelNumber == level)
            {
                targetBoss = b;
                break;
            }
        }

        if (targetBoss == null)
        {
            FindBotPrefab();
            for (int i = 0; i < tiers.Length; i++)
            {
                if (tiers[i].levelNumber == level)
                {
                    GameObject spawnedObj = SpawnSingleLevelBoss(tiers[i], spawnPos, levelTr);
                    if (spawnedObj != null)
                    {
                        targetBoss = spawnedObj.GetComponent<LevelBoss>();
                    }
                    tierSpawned[level] = true;
                    break;
                }
            }
        }

        if (targetBoss != null)
        {
            targetBoss.gameObject.SetActive(true);
            targetBoss.AwakenBossNow(spawnPos);
            Debug.Log($"🔥 [LevelBossManager] Successfully awakened Boss for Level {level} ({targetBoss.bossTitle}) at {spawnPos}!");
        }

        RebuildNavigationMesh();
    }

    private void RebuildNavigationMesh()
    {
        Unity.AI.Navigation.NavMeshSurface surface = FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();
        if (surface == null)
        {
            GameObject navObj = new GameObject("RuntimeNavMeshSurface");
            surface = navObj.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            surface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0; // All layers (including Ground, Climb, etc.)
        }

        surface.BuildNavMesh();
        Debug.Log("🧭 [LevelBossManager] NavMesh successfully built dynamically across all active levels and ramps!");
    }

    private void TriggerAllThreeBossesUnseal()
    {
        if (allThreeBossesTriggered) return;
        allThreeBossesTriggered = true;

        // Ensure all 3 levels are marked defeated
        defeatedBossLevels.Add(1);
        defeatedBossLevels.Add(2);
        defeatedBossLevels.Add(3);

        Debug.Log("🔥💎 ALL 3 LEVEL BOSSES HAVE BEEN DEFEATED! Tree Shield Broken & Sacred Crystal Unsealed!");

        OnAllThreeBossesDefeated?.Invoke();

        // إظهار الكريستالة فوراً للحماة وفك درع الشجرة للمدمرين!
        if (CrystalItem.Instance != null && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer))
        {
            CrystalItem.Instance.SpawnAtTopServer();
        }
    }

    /// <summary>
    /// يعيد الزعيم المستيقظ والحي حالياً (الأقل مستوى أولاً) لكي يتوجه إليه البوتات الأذكياء لقتاله!
    /// </summary>
    public LevelBoss GetActiveAwakenedBoss()
    {
        LevelBoss[] bosses = FindObjectsByType<LevelBoss>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        LevelBoss best = null;
        int minLevel = int.MaxValue;

        foreach (LevelBoss b in bosses)
        {
            if (b != null && b.IsAwakenedAndAlive)
            {
                if (b.levelNumber < minLevel)
                {
                    minLevel = b.levelNumber;
                    best = b;
                }
            }
        }
        return best;
    }

    void FindBotPrefab()
    {
        if (botPrefab == null && BotManager.Instance != null)
        {
            botPrefab = BotManager.Instance.botPrefab;
        }

        if (botPrefab == null)
        {
            botPrefab = Resources.Load<GameObject>("Prefabs/Bot");
        }

        if (botPrefab == null)
        {
            BotController anyBot = FindAnyObjectByType<BotController>();
            if (anyBot != null)
            {
                botPrefab = anyBot.gameObject;
            }
        }
    }

    private readonly bool[] tierSpawned = new bool[4];

    private readonly BossTierConfig[] tiers = new BossTierConfig[]
    {
        new BossTierConfig
        {
            levelName = "Level_1",
            levelNumber = 1,
            title = "FOREST GOLIATH [LV 1]",
            health = 4500f,
            damage = 42f,
            scale = 2.2f,
            color = new Color(0.2f, 0.75f, 0.3f), // أخضر عملاق
            aura = new Color(0.4f, 1f, 0.2f),
            coins = 250,
            xp = 500,
            fallbackPos = new Vector3(-12.18f, 6.5f, 19.77f)
        },
        new BossTierConfig
        {
            levelName = "Level_2",
            levelNumber = 2,
            title = "INFERNO TITAN [LV 2]",
            health = 6500f,
            damage = 60f,
            scale = 2.5f,
            color = new Color(0.92f, 0.25f, 0.1f), // أحمر بركاني ناري
            aura = new Color(1f, 0.45f, 0f),
            coins = 400,
            xp = 750,
            fallbackPos = new Vector3(-12.18f, 16.5f, 19.77f)
        },
        new BossTierConfig
        {
            levelName = "Level_3",
            levelNumber = 3,
            title = "ANCIENT OVERLORD [LV 3]",
            health = 9000f,
            damage = 80f,
            scale = 2.8f,
            color = new Color(0.85f, 0.7f, 0.15f), // ذهبي ملكي
            aura = new Color(1f, 0.85f, 0.2f),
            coins = 600,
            xp = 1200,
            fallbackPos = new Vector3(-12.18f, 26.5f, 19.77f)
        }
    };

    private float checkBossTimer = 0f;
    private readonly HashSet<int> configuredBossInstanceIDs = new HashSet<int>();

    void Update()
    {
        // فحص دوري منظم ومحكوم (مرة كل 0.4 ثانية بدلاً من 120 مرة في الثانية) لمنع استنزاف الرام والـ GC!
        checkBossTimer -= Time.deltaTime;
        if (checkBossTimer <= 0f)
        {
            checkBossTimer = 0.4f;
            if (!bossesSpawned)
            {
                CheckAndSpawnBossesPerLevel();
            }
            CheckBossesDefeatStatus();
        }
    }

    public void CheckBossesDefeatStatus()
    {
        if (allThreeBossesTriggered) return;

        // فحص تلقائي ذكي وشامل لأي زعماء في المشهد تمت هزيمتهم لضمان عدم ضياع أي هزيمة
        LevelBoss[] sceneBosses = FindObjectsByType<LevelBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in sceneBosses)
        {
            if (b != null && (b.isDefeated || (b.GetComponent<Health>() != null && b.GetComponent<Health>().isDead)))
            {
                if (!defeatedBossLevels.Contains(b.levelNumber))
                {
                    Debug.Log($"🛡️ [LevelBossManager] Auto-registering defeated boss for level {b.levelNumber} ({b.bossTitle})");
                    RegisterBossDefeated(b.levelNumber, b.bossTitle, "Champion", PlayerMovement.TeamType.Protector);
                }
            }
        }

        // فك حماية الكريستال والشجرة يحدث حصراً وفقط عندما يُهزم الزعماء الثلاثة جميعهم!
        if (AreAllBossesDefeated && !allThreeBossesTriggered)
        {
            TriggerAllThreeBossesUnseal();
        }
    }

    /// <summary>
    /// فحص وتنظيف شامل لضمان وجود زعيم واحد فقط لا غير لكل مستوى
    /// وحذف أي نسخ مكررة وربط الزعماء الموجودين في المشهد تلقائياً
    /// </summary>
    public void CleanupDuplicateBossesAndAdoptSceneBosses()
    {
        LevelBoss[] allBosses = FindObjectsByType<LevelBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allBosses == null || allBosses.Length == 0) return;

        // خريطة لتتبع الزعيم المعتمد لكل مستوى وحذف أي مكرر
        Dictionary<int, LevelBoss> registeredPerLevel = new Dictionary<int, LevelBoss>();

        foreach (LevelBoss b in allBosses)
        {
            if (b == null) continue;
            int lvl = b.levelNumber;
            if (lvl < 1 || lvl > 3) continue;

            if (!registeredPerLevel.ContainsKey(lvl))
            {
                registeredPerLevel[lvl] = b;
                tierSpawned[lvl] = true;

                // الزعيم يتبع مباشرة حالة منصته: إذا كانت منصة مستواه نشطة يظهر، وإذا كانت غير نشطة يخمل
                GameObject treeLevelsObj = GameObject.Find(treeLevelsParentName);
                Transform pltTr = treeLevelsObj != null ? treeLevelsObj.transform.Find($"Level_{lvl}") : null;
                if (pltTr == null)
                {
                    GameObject pObj = GameObject.Find($"Level_{lvl}");
                    if (pObj != null) pltTr = pObj.transform;
                }

                bool platformActive = (pltTr != null && pltTr.gameObject.activeInHierarchy);

                // الزعيم للمستوى 2 والمستوى 3 يبدأ خاملاً ومخفياً فقط إذا كانت منصته غير نشطة ولم يُوقظ بعد
                if (lvl > 1 && !platformActive && !b.IsAwakenedAndAlive)
                {
                    b.gameObject.SetActive(false);
                    b.CheckPlatformReadiness();
                }
                else
                {
                    // إيقاظ الزعيم إذا لم يكن مهزوماً
                    if (!b.isDefeated && !defeatedBossLevels.Contains(lvl))
                    {
                        Health bh = b.GetComponent<Health>();
                        if (bh == null || !bh.isDead)
                        {
                            b.gameObject.SetActive(true);
                            Vector3 pPos = pltTr != null ? pltTr.position : b.transform.position;
                            b.AwakenBossNow(pPos);
                        }
                    }
                }

                // مطابقة وإعداد بيانات الزعيم مرة واحدة فقط لكل مجسم فريد لمنع استنزاف الذاكرة
                int instId = b.gameObject.GetInstanceID();
                if (!configuredBossInstanceIDs.Contains(instId))
                {
                    configuredBossInstanceIDs.Add(instId);
                    for (int i = 0; i < tiers.Length; i++)
                    {
                        if (tiers[i].levelNumber == lvl)
                        {
                            var cfg = tiers[i];
                            b.SetupBossData(cfg.levelNumber, cfg.title, cfg.health, cfg.damage, cfg.scale, cfg.color, cfg.aura, cfg.coins, cfg.xp);
                            break;
                        }
                    }
                }
            }
            else
            {
                // نسخة مكررة غير شرعية! نحذفها فوراً!
                Debug.LogWarning($"⚠️ [LevelBossManager] Duplicate boss detected for Level {lvl} ({b.name}). Destroying duplicate immediately!");
                Destroy(b.gameObject);
            }
        }

        bossesSpawned = (tierSpawned[1] && tierSpawned[2] && tierSpawned[3]);
    }

    /// <summary>
    /// توليد زعيم كل مستوى فقط عندما تكون منصته نشطة ومتاحة مع ضمان عدم توليد أي زعيم مكرر!
    /// </summary>
    public void CheckAndSpawnBossesPerLevel()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // إذا كان زعماء المشهد الثلاثة موجودين، لا نولد أي كائن جديد إطلاقاً لمنع الازدواجية!
        if (bossesSpawned) return;

        FindBotPrefab();
        if (botPrefab == null) return;

        GameObject treeLevelsObj = GameObject.Find(treeLevelsParentName);

        for (int i = 0; i < tiers.Length; i++)
        {
            var tier = tiers[i];
            if (tierSpawned[tier.levelNumber]) continue;
            if (defeatedBossLevels.Contains(tier.levelNumber)) continue;

            // 2. فحص صريح إضافي: هل يوجد زعيم بالفعل لهذا الطابق؟
            LevelBoss[] existing = FindObjectsByType<LevelBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool alreadyPresent = false;
            foreach (var b in existing)
            {
                if (b != null && b.levelNumber == tier.levelNumber)
                {
                    alreadyPresent = true;
                    tierSpawned[tier.levelNumber] = true;
                    int bId = b.gameObject.GetInstanceID();
                    if (!configuredBossInstanceIDs.Contains(bId))
                    {
                        configuredBossInstanceIDs.Add(bId);
                        b.SetupBossData(tier.levelNumber, tier.title, tier.health, tier.damage, tier.scale, tier.color, tier.aura, tier.coins, tier.xp);
                    }
                    break;
                }
            }
            if (alreadyPresent) continue;

            Transform levelTr = null;
            if (treeLevelsObj != null)
            {
                levelTr = treeLevelsObj.transform.Find(tier.levelName);
            }
            if (levelTr == null)
            {
                GameObject d = GameObject.Find(tier.levelName);
                if (d != null) levelTr = d.transform;
            }

            // المستوى 1 نشط دائماً منذ البداية. المستويان 2 و 3 يظهران فور نشاط منصتيهما.
            bool isPlatformReady = (tier.levelNumber == 1) || (levelTr != null && levelTr.gameObject.activeInHierarchy);

            if (isPlatformReady)
            {
                float expectedY = tier.levelNumber switch
                {
                    1 => 6.5f,
                    2 => 16.5f,
                    3 => 26.5f,
                    _ => 6.5f
                };

                Vector3 spawnPos = (levelTr != null)
                    ? new Vector3(levelTr.position.x, expectedY, levelTr.position.z)
                    : tier.fallbackPos;

                GameObject bossObj = SpawnSingleLevelBoss(tier, spawnPos, levelTr);
                if (bossObj != null)
                {
                    LevelBoss b = bossObj.GetComponent<LevelBoss>();
                    if (b != null)
                    {
                        b.AwakenBossNow(spawnPos);
                    }
                }
                tierSpawned[tier.levelNumber] = true;
                bossesSpawned = (tierSpawned[1] && tierSpawned[2] && tierSpawned[3]);
            }
        }
    }

    GameObject SpawnSingleLevelBoss(BossTierConfig config, Vector3 position, Transform parentContainer)
    {
        FindBotPrefab();
        if (botPrefab == null)
        {
            Debug.LogError($"[LevelBossManager] botPrefab is null! Cannot spawn boss {config.title}!");
            return null;
        }

        GameObject bossObj = Instantiate(botPrefab, position, Quaternion.identity);
        bossObj.name = $"Boss_{config.levelName}";

        // تأمين صارم: إيقاف الجاذبية وجعل الريجيدبودي Kinematic حتى لا يسقط الزعيم من منصته أبداً!
        Rigidbody rb = bossObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // إيقاف ذكاء البوت العادي
        BotController defaultBotAI = bossObj.GetComponent<BotController>();
        if (defaultBotAI != null)
        {
            defaultBotAI.enabled = false;
        }

        // إضافة وتجهيز سكربت الزعيم
        LevelBoss boss = bossObj.GetComponent<LevelBoss>();
        if (boss == null)
        {
            boss = bossObj.AddComponent<LevelBoss>();
        }

        boss.SetupBossData(
            config.levelNumber,
            config.title,
            config.health,
            config.damage,
            config.scale,
            config.color,
            config.aura,
            config.coins,
            config.xp
        );

        // نشر الكائن عبر الشبكة إذا كان النتكود مفعلًا
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = bossObj.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }
        }

        Debug.Log($"👑 Level Boss Created: {config.title} safely on Level {config.levelNumber} at {position}");
        return bossObj;
    }

    private struct BossTierConfig
    {
        public string levelName;
        public int levelNumber;
        public string title;
        public float health;
        public float damage;
        public float scale;
        public Color color;
        public Color aura;
        public int coins;
        public int xp;
        public Vector3 fallbackPos;
    }
}
