using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// سكربت الزعيم المخصص لكل مستوى من مستويات الشجرة
/// كبير الحجم، قوي الضربات، يملك شريط صحة ضخم عائم،
/// وعند موته يمنح القاتل أموالاً (Coins)، ونقاط XP، وطاقة الجبار (Titan Buff)
/// </summary>
public class LevelBoss : NetworkBehaviour
{
    [Header("Boss Identity")]
    public string bossTitle = "TITAN OF LEVEL 1";
    public int levelNumber = 1;
    public int coinReward = 300;
    public int xpReward = 600;

    [Header("Stats")]
    public float maxBossHealth = 4500f;
    public float attackDamage = 45f;
    public float bossScale = 2.4f;

    [Header("Color Theme")]
    public Color bossColor = new Color(0.85f, 0.2f, 0.1f); // أحمر بركاني ناري
    public Color auraColor = new Color(1f, 0.4f, 0f);

    private Health health;
    private CharacterCombat combat;
    private NavMeshAgent agent;
    private Light bossLight;
    public bool isDefeated = false;
    private Vector3 guardCenter;
    private bool isAwaitingLevelUnlock = false;
    private GameObject myPlatformObject = null;
    private bool isDormant = false;

    public float GetExpectedLevelY()
    {
        return levelNumber switch
        {
            1 => 6.5f,
            2 => 16.5f,
            3 => 26.5f,
            _ => 6.5f
        };
    }

    public float GetExpectedMaxHealth()
    {
        return levelNumber switch
        {
            1 => 4500f,
            2 => 6500f,
            3 => 9000f,
            _ => 5000f
        };
    }

    public float GetExpectedAttackDamage()
    {
        return levelNumber switch
        {
            1 => 42f,
            2 => 60f,
            3 => 80f,
            _ => 45f
        };
    }

    void Awake()
    {
        // حماية هامة: إذا كان الزعيم موضوعاً كابن لمنصة ذات مقياس كبير، نخرجه منها لتجنب تضخم الحجم وتشوه الحركة
        if (transform.parent != null && (Mathf.Abs(transform.parent.lossyScale.x - 1f) > 0.05f || Mathf.Abs(transform.parent.lossyScale.y - 1f) > 0.05f || Mathf.Abs(transform.parent.lossyScale.z - 1f) > 0.05f))
        {
            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;
            transform.SetParent(null);
            transform.position = worldPos;
            transform.rotation = worldRot;
        }

        float y = GetExpectedLevelY();
        guardCenter = new Vector3(transform.position.x, y, transform.position.z);
        transform.position = guardCenter;
        InitializeBoss();
    }

    void Start()
    {
        InitializeBoss();
        if (levelNumber <= 1)
        {
            // الزعيم الأول (Level 1) نشط وموجود من اللحظة الأولى للعبة
            isDormant = false;
            isAwaitingLevelUnlock = false;
            gameObject.SetActive(true);
            SetBossDormant(false);

            GameObject lvl1Obj = GameObject.Find("Level_1");
            if (lvl1Obj != null)
            {
                guardCenter = lvl1Obj.transform.position + Vector3.up * 1.5f;
                transform.position = guardCenter;
            }

            if (agent != null)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh) agent.Warp(transform.position);
            }
        }
        else
        {
            CheckPlatformReadiness();
        }
    }

    public override void OnNetworkSpawn()
    {
        InitializeBoss();
    }

    private bool isBossDataInitialized = false;

    public void SetupBossData(int level, string title, float hp, float dmg, float scale, Color color, Color aura, int coins, int xp)
    {
        levelNumber = level;
        bossTitle = title;
        maxBossHealth = hp;
        attackDamage = dmg;
        bossScale = scale;
        bossColor = color;
        auraColor = aura;
        coinReward = coins;
        xpReward = xp;

        if (!isBossDataInitialized)
        {
            isBossDataInitialized = true;
            InitializeBoss();
        }

        if (levelNumber <= 1)
        {
            isDormant = false;
            isAwaitingLevelUnlock = false;
            SetBossDormant(false);
        }
        else
        {
            CheckPlatformReadiness();
        }
    }

    void InitializeBoss()
    {
        if (transform.parent != null && (Mathf.Abs(transform.parent.lossyScale.x - 1f) > 0.05f || Mathf.Abs(transform.parent.lossyScale.y - 1f) > 0.05f || Mathf.Abs(transform.parent.lossyScale.z - 1f) > 0.05f))
        {
            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;
            transform.SetParent(null);
            transform.position = worldPos;
            transform.rotation = worldRot;
        }

        // إيقاف وحذف أي BotController أو PlayerXP على الزعيم لمنع أي تعارض
        BotController bc = GetComponent<BotController>();
        if (bc != null)
        {
            bc.enabled = false;
            Destroy(bc);
        }

        PlayerXP pxp = GetComponent<PlayerXP>();
        if (pxp != null)
        {
            pxp.enabled = false;
            Destroy(pxp);
        }

        // 0. تأمين الريجيدبودي لمنع السقوط مع الجاذبية
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 1. Cap boss scale to prevent clipping through level platforms
        float clampedScale = Mathf.Min(bossScale, 1.4f);
        transform.localScale = Vector3.one * clampedScale;

        // 2. ضبط الفريق كـ Boss حتى يستطيع كلا الفريقين (Protectors & Destroyers) قتاله
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.team = PlayerMovement.TeamType.Boss;
        }

        // 3. ضبط الصحة - ضمان قيم قوية كاملة ومطابقة لمستوى الزعيم
        maxBossHealth = GetExpectedMaxHealth();
        attackDamage = GetExpectedAttackDamage();

        health = GetComponent<Health>();
        if (health != null)
        {
            health.maxHealth = maxBossHealth;
            health.currentHealth = maxBossHealth;
            health.isDead = false;
        }

        // 4. ضبط القوة القتالية
        combat = GetComponent<CharacterCombat>();
        if (combat != null)
        {
            combat.team = PlayerMovement.TeamType.Boss;
            combat.attackDamage = attackDamage;
            combat.attackRange = 4.0f; // مدى ضربة أوسع لأنه عملاق
            combat.attackCooldown = 1.2f;
        }

        // 5. ضبط الملاحة والدوريات
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.baseOffset = 0.95f;
            agent.speed = 4.5f;
            agent.stoppingDistance = 2.5f;
        }

        // 6. تلوين الزعيم وإضافة هالة مرعبة
        ApplyBossVisuals();

        guardCenter = transform.position;
    }

    void ApplyBossVisuals()
    {
        // 1. إخفاء كبسولة الزعيم البدائية الرئيسية ومجلد Model و Visual
        MeshRenderer baseRend = GetComponent<MeshRenderer>();
        if (baseRend != null) baseRend.enabled = false;

        Transform modelTr = transform.Find("Model");
        if (modelTr != null)
        {
            MeshRenderer mr = modelTr.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        Transform visualTr = transform.Find("Visual");
        if (visualTr != null)
        {
            visualTr.gameObject.SetActive(false);
        }

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.transform != transform && 
                !r.name.Contains("Warrior") && 
                !r.name.Contains("Character_3D") && 
                !r.name.Contains("BossDecorations") && 
                !r.name.Contains("BossAuraCrown") &&
                !r.name.Contains("Horn") &&
                !r.name.Contains("Shoulder"))
            {
                r.enabled = false;
            }
        }

        // 2. إرفاق مجسم الزعيم ثلاثي الأبعاد الحقيقي وضمان حركته وهيبته
        Transform existing = transform.Find("Character_3D_Mesh");
        GameObject model = null;
        if (existing != null)
        {
            model = existing.gameObject;
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>("Models/Warrior_Medieval");
            if (prefab == null) prefab = Resources.Load<GameObject>("Models/Akai");
            if (prefab != null)
            {
                model = Instantiate(prefab, transform);
                model.name = "Character_3D_Mesh";
                model.transform.localPosition = new Vector3(0f, -0.95f, 0f);
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * 1.25f;

                foreach (Collider col in model.GetComponentsInChildren<Collider>()) Destroy(col);
                foreach (Rigidbody childRb in model.GetComponentsInChildren<Rigidbody>()) Destroy(childRb);
                foreach (Animator anim in model.GetComponentsInChildren<Animator>()) anim.applyRootMotion = false;
            }
        }

        if (model != null)
        {
            if (!isDormant)
            {
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                }
            }

            // تلوين الزعيم بدقة بألوان الزعامة والهيبة والتوهج المرعب
            PlayerMovement.StyleModelForBoss(model, bossColor, auraColor);

            // تفعيل محرك الحركة والأنيميشن الحيوي للزعيم لمنع أي تجمد كالصنم نهائياً
            CharacterVisualAnimator visualAnim = model.GetComponent<CharacterVisualAnimator>();
            if (visualAnim == null)
            {
                visualAnim = model.AddComponent<CharacterVisualAnimator>();
            }
        }

        // 3. إضافة ضوء هالة محيطة
        if (bossLight == null)
        {
            bossLight = GetComponent<Light>();
            if (bossLight == null)
            {
                bossLight = gameObject.AddComponent<Light>();
            }
        }

        if (bossLight != null)
        {
            bossLight.type = LightType.Point;
            bossLight.color = auraColor;
            bossLight.range = 14f;
            bossLight.intensity = 4f;
        }

        // إضافة تاج/هالة أثيرية تطفو فوق رأس الزعيم كعلامة مميزة
        Transform existingCrown = transform.Find("BossAuraCrown");
        if (existingCrown == null)
        {
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "BossAuraCrown";
            crown.transform.SetParent(transform);
            crown.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            crown.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            Collider col = crown.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer cr = crown.GetComponent<Renderer>();
            if (cr != null)
            {
                cr.material.color = auraColor;
                cr.material.EnableKeyword("_EMISSION");
                cr.material.SetColor("_EmissionColor", auraColor * 1.5f);
            }
        }
        else
        {
            existingCrown.localPosition = new Vector3(0f, 1.05f, 0f);
        }

        // إضافة قرون وحشية وتفاصيل 3D مميزة للزعيم
        AddBossBeastDecorations();
    }

    void AddBossBeastDecorations()
    {
        if (transform.Find("BossDecorations") != null) return;

        GameObject decorContainer = new GameObject("BossDecorations");
        decorContainer.transform.SetParent(transform, false);

        // قرنان مرعبان يتناسبان مع خوذة المحارب
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.name = $"Horn_{(i > 0 ? "R" : "L")}";
            horn.transform.SetParent(decorContainer.transform, false);
            horn.transform.localPosition = new Vector3(i * 0.22f, 0.95f, 0.05f);
            horn.transform.localRotation = Quaternion.Euler(25f, 0f, i * -25f);
            horn.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            Collider col = horn.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer hr = horn.GetComponent<Renderer>();
            if (hr != null)
            {
                hr.material.color = Color.Lerp(bossColor, Color.black, 0.3f);
                hr.material.EnableKeyword("_EMISSION");
                hr.material.SetColor("_EmissionColor", auraColor * 0.8f);
            }
        }

        // دروع كتف شوكية
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject pauldron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pauldron.name = $"Shoulder_{(i > 0 ? "R" : "L")}";
            pauldron.transform.SetParent(decorContainer.transform, false);
            pauldron.transform.localPosition = new Vector3(i * 0.95f, 1.4f, 0f);
            pauldron.transform.localRotation = Quaternion.Euler(0f, 0f, i * -30f);
            pauldron.transform.localScale = new Vector3(0.5f, 0.25f, 0.5f);
            Collider col = pauldron.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer pr = pauldron.GetComponent<Renderer>();
            if (pr != null)
            {
                pr.material.color = bossColor;
                pr.material.EnableKeyword("_EMISSION");
                pr.material.SetColor("_EmissionColor", bossColor * 0.8f);
            }
        }
    }

    public void CheckPlatformReadiness()
    {
        // الزعيم الأول (المستوى 1) موجود دائماً منذ بداية اللعبة، لا نخفيه أبداً
        if (levelNumber <= 1) return;

        string targetPlatformName = $"Level_{levelNumber}";
        GameObject platformObj = null;

        // البحث عن المنصة حتى لو كانت غير نشطة (Inactive)
        GameObject treeLevels = GameObject.Find("TreeLevels");
        if (treeLevels != null)
        {
            foreach (Transform t in treeLevels.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals(targetPlatformName, System.StringComparison.OrdinalIgnoreCase))
                {
                    platformObj = t.gameObject;
                    break;
                }
            }
        }

        if (platformObj == null)
        {
            platformObj = GameObject.Find(targetPlatformName);
        }

        myPlatformObject = platformObj;

        // إذا كانت المنصة غير مفعلة في المشهد، نضع الزعيم في وضع الخمول (مخفي وبدون كوليدر) دون تعطيل الكائن ليستجيب للأوامر
        if (platformObj != null && !platformObj.activeInHierarchy)
        {
            isAwaitingLevelUnlock = true;
            SetBossDormant(true);
        }
    }


    /// <summary>إيقاظ فوري ومباشر للزعيم عند فتح مستواه من قبل TreeLevelManager</summary>
    public void AwakenBossNow(Vector3 platformPos)
    {
        // حماية صارمة: لا توقظ زعيماً إذا كان مستواه مسجلاً كهزيمة مسبقة في LevelBossManager
        if (LevelBossManager.Instance != null && LevelBossManager.Instance.IsBossLevelDefeated(levelNumber)) return;

        isDefeated = false;
        gameObject.SetActive(true);
        isAwaitingLevelUnlock = false;
        float y = GetExpectedLevelY();
        guardCenter = new Vector3(platformPos.x, y, platformPos.z);
        transform.position = guardCenter;
        SetBossDormant(false);

        maxBossHealth = GetExpectedMaxHealth();
        attackDamage = GetExpectedAttackDamage();

        if (health == null) health = GetComponent<Health>();
        if (health != null)
        {
            health.maxHealth = maxBossHealth;
            health.currentHealth = maxBossHealth;
            health.isDead = false;
        }

        if (combat == null) combat = GetComponent<CharacterCombat>();
        if (combat != null)
        {
            combat.team = PlayerMovement.TeamType.Boss;
            combat.attackDamage = attackDamage;
            combat.attackRange = 4.0f;
            combat.attackCooldown = 1.2f;
        }

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(guardCenter);
        }

        if (bossLight != null)
        {
            bossLight.enabled = true;
            bossLight.intensity = 4.5f;
        }

        Debug.Log($"🔥 BOSS AWAKENED! {bossTitle} has appeared on Level {levelNumber}!");

        // لا نظهر تنبيه البوب-آب أثناء شاشة اختيار اللغة أو قبل انطلاق المباراة فعلياً
        bool isMatchPlaying = GameStateManager.Instance == null || GameStateManager.Instance.currentState == GameStateManager.GameState.Playing;
        if (isMatchPlaying)
        {
            string notifMsg = LanguageSelectionUI.Localize(
                $"⚠️ WARNING: {bossTitle} HAS AWAKENED ON LEVEL {levelNumber}!",
                ar: $"⚠️ تنبيه: استيقظ زعيم الطابق {levelNumber} ({bossTitle})!",
                fa: $"⚠️ هشدار: {bossTitle} در طبقه {levelNumber} بیدار شد!",
                de: $"⚠️ WARNUNG: {bossTitle} IST AUF EBENE {levelNumber} ERWACHT!",
                it: $"⚠️ ATTENZIONE: {bossTitle} SI È RISVEGLIATO AL LIVELLO {levelNumber}!",
                tr: $"⚠️ UYARI: {bossTitle} {levelNumber}. SEVİYEDE UYANDI!",
                ru: $"⚠️ ВНИМАНИЕ: {bossTitle} ПРОБУДИЛСЯ НА УРОВНЕ {levelNumber}!",
                fr: $"⚠️ ATTENTION : {bossTitle} S'EST ÉVEILLÉ AU NIVEAU {levelNumber} !",
                es: $"⚠️ ¡ADVERTENCIA: {bossTitle} HA DESPERTADO EN EL NIVEL {levelNumber}!",
                hi: $"⚠️ चेतावनी: {bossTitle} स्तर {levelNumber} पर जाग गया है!",
                zh: $"⚠️ 警告：{bossTitle} 已在第 {levelNumber} 层苏醒！",
                ja: $"⚠️ 警告：{bossTitle} が第 {levelNumber} レベルで目覚めました！",
                pt: $"⚠️ AVISO: {bossTitle} DESPERTOU NO NÍVEL {levelNumber}!"
            );
            PlayerProgressionAndShopUI.Instance?.ShowNotification(notifMsg);
        }
    }

    void SetBossDormant(bool dormant)
    {
        isDormant = dormant;

        if (!dormant)
        {
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            foreach (Collider c in cols) c.enabled = true;

            ApplyBossVisuals();

            // تفعيل مجسم ثلاثي الأبعاد بصورة قطعية
            Transform mesh3D = transform.Find("Character_3D_Mesh");
            if (mesh3D != null)
            {
                foreach (Renderer r in mesh3D.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                }
            }

            Transform crown = transform.Find("BossAuraCrown");
            if (crown != null)
            {
                Renderer r = crown.GetComponent<Renderer>();
                if (r != null) r.enabled = true;
            }

            Transform decor = transform.Find("BossDecorations");
            if (decor != null)
            {
                foreach (Renderer r in decor.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                }
            }

            if (bossLight != null)
            {
                bossLight.enabled = true;
                bossLight.intensity = 4.5f;
            }
            if (agent != null)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh) agent.Warp(transform.position);
            }
        }
        else
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers) r.enabled = false;

            Collider[] cols = GetComponentsInChildren<Collider>(true);
            foreach (Collider c in cols) c.enabled = false;

            if (bossLight != null) bossLight.enabled = false;
            if (agent != null) agent.enabled = false;
        }
    }

    void Update()
    {
        // فحص ظهور المنصة لإيقاظ الزعيم فور ظهور ونشاط منصته
        if (isAwaitingLevelUnlock)
        {
            if (myPlatformObject == null) CheckPlatformReadiness();
            if (myPlatformObject != null && myPlatformObject.activeInHierarchy)
            {
                AwakenBossNow(myPlatformObject.transform.position);
            }
            return;
        }

        if (isDormant) return;

        // حماية قطعية: تثبيت ارتفاع الزعيم على طابقه المخصص بدقة لمنع أي سقوط لطابق أدنى
        float expectedY = GetExpectedLevelY();
        if (Mathf.Abs(transform.position.y - expectedY) > 0.8f)
        {
            Vector3 fixedPos = transform.position;
            fixedPos.y = expectedY;
            transform.position = fixedPos;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(fixedPos);
            }
        }

        // دوران ونبض تاج وهالة الزعيم
        Transform crown = transform.Find("BossAuraCrown");
        if (crown != null)
        {
            crown.Rotate(0f, 45f * Time.deltaTime, 0f, Space.Self);
            crown.localPosition = new Vector3(0f, 2.6f + Mathf.Sin(Time.time * 2.5f) * 0.15f, 0f);
        }

        if (bossLight != null)
        {
            bossLight.intensity = 3f + Mathf.PingPong(Time.time * 3f, 2.5f);
        }

        bool isServerOrAuthority = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer;
        if (!isServerOrAuthority) return;
        if (isDefeated) return;

        // التحقق من الموت
        if (health != null && health.isDead)
        {
            OnBossDefeated();
            return;
        }

        // لا يتحرك الزعيم ولا يهاجم حتى تبدأ المباراة رسمياً
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
        {
            return;
        }

        // الذكاء الاصطناعي لحراسة المنصة مع تقييد صارم لعدم مغادرة المنصة أبداً
        UpdateBossGuardAI();
    }

    public float GetLeashRadius()
    {
        if (levelNumber == 1) return 11.5f;
        if (levelNumber == 2) return 9.0f;
        if (levelNumber == 3) return 7.0f;
        return 9.0f;
    }

    void UpdateBossGuardAI()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        float maxLeash = GetLeashRadius();
        float expectedY = GetExpectedLevelY();
        guardCenter.y = expectedY;

        Vector2 centerXZ = new Vector2(guardCenter.x, guardCenter.z);
        Vector2 bossXZ = new Vector2(transform.position.x, transform.position.z);
        float currentDistFromCenter = Vector2.Distance(bossXZ, centerXZ);
        float verticalDiffFromCenter = Mathf.Abs(transform.position.y - expectedY);

        // حماية قصوى: إذا سقط الزعيم أو تم دفعه خارج حدوده يعود فوراً لمنصته ولا يسقط للأسفل أبداً
        if (currentDistFromCenter > maxLeash * 1.35f || verticalDiffFromCenter > 2.0f)
        {
            transform.position = guardCenter;
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(guardCenter);
            return;
        }

        // إذا وصل الزعيم لحافة منصته يعود للمنتصف ولا يتبع أحداً للأسفل أو للأعلى
        if (currentDistFromCenter > maxLeash)
        {
            agent.SetDestination(guardCenter);
            return;
        }

        // البحث عن الأعداء المتواجدين حصراً على نفس منصة الزعيم
        Collider[] nearby = Physics.OverlapSphere(guardCenter, maxLeash);
        Transform closestEnemy = null;
        float closestDist = float.MaxValue;

        foreach (Collider col in nearby)
        {
            if (col.gameObject == gameObject) continue;

            CharacterCombat enemyCombat = col.GetComponent<CharacterCombat>();
            Health enemyHealth = col.GetComponent<Health>();

            if (enemyCombat != null && enemyCombat.team != PlayerMovement.TeamType.Boss &&
                enemyHealth != null && !enemyHealth.isDead)
            {
                // التحقق أن العدو على نفس المنصة وليس على السلالم الحلزونية أو طابق آخر
                Vector2 enemyXZ = new Vector2(col.transform.position.x, col.transform.position.z);
                float enemyDistFromCenter = Vector2.Distance(enemyXZ, centerXZ);
                float enemyVerticalDiff = Mathf.Abs(col.transform.position.y - guardCenter.y);

                if (enemyDistFromCenter <= maxLeash && enemyVerticalDiff <= 2.2f)
                {
                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestEnemy = col.transform;
                    }
                }
            }
        }

        if (closestEnemy != null)
        {
            agent.SetDestination(closestEnemy.position);

            // الهجوم إذا كان في المدى
            if (closestDist <= 4.2f && combat != null)
            {
                combat.TryAIAttack(closestEnemy);
            }
        }
        else
        {
            // الرجوع لمركز الحراسة على المنصة إذا لم يكن هناك أعداء على المنصة
            if (currentDistFromCenter > 2.5f)
            {
                agent.SetDestination(guardCenter);
            }
        }
    }

    public bool IsAwakenedAndAlive => !isDormant && !isAwaitingLevelUnlock && !isDefeated && (health == null || !health.isDead);

    public void OnBossDefeated()
    {
        if (isDefeated) return;
        isDefeated = true;
        if (health != null) health.isDead = true;

        GameObject killer = health != null ? health.lastAttacker : null;
        string killerName = killer != null ? killer.name : "A Brave Champion";
        PlayerMovement.TeamType killerTeam = PlayerMovement.TeamType.Protector;

        if (killer != null)
        {
            CharacterCombat cc = killer.GetComponent<CharacterCombat>();
            if (cc != null) killerTeam = cc.team;
            else
            {
                PlayerMovement pm = killer.GetComponent<PlayerMovement>();
                if (pm != null) killerTeam = pm.team;
                else
                {
                    BotController bc = killer.GetComponent<BotController>();
                    if (bc != null) killerTeam = bc.team;
                }
            }
        }

        if (levelNumber < 1 || levelNumber > 3)
        {
            if (gameObject.name.Contains("1") || transform.position.y < 12f) levelNumber = 1;
            else if (gameObject.name.Contains("2") || transform.position.y < 22f) levelNumber = 2;
            else if (gameObject.name.Contains("3") || transform.position.y >= 22f) levelNumber = 3;
            else levelNumber = 1;
        }

        Debug.Log($"👑 BOSS SLAIN! {bossTitle} (Level {levelNumber}) was defeated by {killerName} ({killerTeam})!");

        // 1. تسجيل هزيمة الزعيم في مدير الزعماء فوراً كأولوية قصوى مطلقة لضمان فك الأختام والكريستالة
        try
        {
            if (LevelBossManager.Instance != null)
            {
                LevelBossManager.Instance.RegisterBossDefeated(levelNumber, bossTitle, killerName, killerTeam);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LevelBoss] Error in RegisterBossDefeated: {ex}");
        }

        // 2. منح المكافآت للاعب القاتل وتحديث الإحصائيات بأمان تام
        try
        {
            if (PlayerProfile.Instance != null)
            {
                PlayerProfile.Instance.RegisterBossKill(bossTitle, coinReward, xpReward, killer);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LevelBoss] Error in PlayerProfile.RegisterBossKill: {ex}");
        }

        try
        {
            if (killer != null && MatchEndStatsUI.Instance != null)
            {
                MatchEndStatsUI.Instance.RegisterBossKill(killer);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LevelBoss] Error in MatchEndStatsUI.RegisterBossKill: {ex}");
        }

        // 3. إشعار الشبكة والعملاء المتصلين
        try
        {
            if (IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                AnnounceBossDeathRpc(levelNumber, bossTitle, killerName, (int)killerTeam, coinReward, xpReward);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LevelBoss] Error in AnnounceBossDeathRpc: {ex}");
        }

        // 4. إطلاق مشهد موت وسقوط الزعيم السينمائي المهيب (يسقط أرضاً أولاً ثم يظهر القبر)
        StartCoroutine(BossDeathSequenceRoutine(bossTitle, killerTeam, killerName, coinReward, xpReward));
    }

    /// <summary>
    /// تسلسل سينمائي متكامل لموت الزعيم:
    /// 1. شل الحركة والقتال فوراً
    /// 2. تهاوي وسقوط الزعيم ببطء وواقعية للخلف نحو الأرض (Dramatic Collapse)
    /// 3. ارتطام بالأرض وهزة كاميرا وموجة صدمة ذهبية/نارية
    /// 4. البقاء ممدداً على الأرض لمدة 2.5 ثانية ليشاهد الجميع جثة الزعيم الساقطة
    /// 5. ظهور ضريح الغنائم (Tombstone) وتلاشي الجثة بسلام
    /// </summary>
    IEnumerator BossDeathSequenceRoutine(string title, PlayerMovement.TeamType team, string killer, int coins, int xp)
    {
        // 1. شل حركة وقتال الزعيم وتعطيل كوليدرات الهجوم فوراً
        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }
        if (combat != null) combat.enabled = false;

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        Transform crown = transform.Find("BossAuraCrown");
        if (crown != null)
        {
            crown.gameObject.SetActive(false);
        }

        // 2. تهاوي الزعيم وسقوطه على ظهره فوق سطح المنصة
        Transform meshTr = transform.Find("Character_3D_Mesh");
        if (meshTr == null) meshTr = transform.Find("Model");

        CharacterVisualAnimator visualAnim = meshTr != null ? meshTr.GetComponent<CharacterVisualAnimator>() : null;
        if (visualAnim != null && visualAnim.UsesRealDeathAnimation)
        {
            visualAnim.TriggerDeathAnimation();
        }

        Quaternion startRot = meshTr != null ? meshTr.localRotation : Quaternion.identity;
        Quaternion targetRot = Quaternion.Euler(-85f, 0f, 0f);
        Vector3 startLocalPos = meshTr != null ? meshTr.localPosition : Vector3.zero;
        Vector3 targetLocalPos = new Vector3(startLocalPos.x, -0.6f, startLocalPos.z);

        float fallDuration = 0.85f;
        float fallTimer = 0f;

        while (fallTimer < fallDuration)
        {
            fallTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fallTimer / fallDuration);
            // حركة سقوط متسارعة بفعل الجاذبية (Ease In)
            float easeT = t * t;

            if (meshTr != null)
            {
                meshTr.localRotation = Quaternion.Slerp(startRot, targetRot, easeT);
                meshTr.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, easeT);
            }

            if (bossLight != null)
            {
                bossLight.intensity = Mathf.Lerp(4.5f, 1.2f, t);
            }

            yield return null;
        }

        if (meshTr != null)
        {
            meshTr.localRotation = targetRot;
            meshTr.localPosition = targetLocalPos;
        }

        // 3. ارتطام هائل بالأرض مع هزة كاميرا وموجة صدمة أرضية
        CreateDeathImpactShockwave(transform.position);

        // 4. البقاء ممدداً على الأرض لمدة ثانيتين ونصف (2.5 ثانية) كما طلب المستخدم تماماً
        yield return new WaitForSeconds(2.5f);

        // 5. ظهور ضريح الغنائم (Boss Tombstone) في موقع موت وسقوط الزعيم
        Vector3 tombstonePos = transform.position;
        GameObject tombstoneObj = new GameObject($"Tombstone_{title}");
        tombstoneObj.transform.position = tombstonePos;
        BossTombstone tombstone = tombstoneObj.AddComponent<BossTombstone>();
        tombstone.Setup(title, team, killer, coins, xp);

        // 6. تلاشي مجسم الزعيم بهدوء وصعود طاقته للسماء
        float fadeTimer = 0.5f;
        Vector3 origScale = transform.localScale;
        while (fadeTimer > 0f)
        {
            fadeTimer -= Time.deltaTime;
            float ratio = Mathf.Clamp01(fadeTimer / 0.5f);
            transform.localScale = origScale * ratio;
            yield return null;
        }

        Destroy(gameObject);
    }

    void CreateDeathImpactShockwave(Vector3 groundPos)
    {
        // هزة كاميرا ناعمة وسلسة عند ارتطام الزعيم بالأرض دون تجميد أو إرباك للشاشة
        ThirdPersonCamera.Shake(0.25f, 0.035f);

        GameObject shockObj = new GameObject("BossDeathShockwave");
        shockObj.transform.position = groundPos + Vector3.up * 0.15f;
        StartCoroutine(AnimateShockwaveRoutine(shockObj));
    }

    IEnumerator AnimateShockwaveRoutine(GameObject shockObj)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.SetParent(shockObj.transform);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(1f, 0.04f, 1f);

        Collider c = ring.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Renderer rend = ring.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = bossColor;
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", auraColor * 2.5f);
        }

        float dur = 0.65f;
        float elapsed = 0f;
        Vector3 maxScale = new Vector3(9.5f, 0.04f, 9.5f);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            ring.transform.localScale = Vector3.Lerp(new Vector3(1f, 0.04f, 1f), maxScale, t);
            if (rend != null)
            {
                Color clr = bossColor;
                clr.a = 1f - t;
                rend.material.color = clr;
            }
            yield return null;
        }

        Destroy(shockObj);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void AnnounceBossDeathRpc(int bossLevel, string title, string killer, int killerTeamInt, int coins, int xp)
    {
        if (IsServer) return; // تم التسجيل بالفعل على السيرفر في OnBossDefeated
        PlayerMovement.TeamType kTeam = (PlayerMovement.TeamType)killerTeamInt;
        Debug.Log($"📢 [GLOBAL] {title} DEFEATED! Killer: {killer} ({kTeam}) earned {coins} Coins & {xp} XP!");

        if (LevelBossManager.Instance != null)
        {
            LevelBossManager.Instance.RegisterBossDefeated(bossLevel, title, killer, kTeam);
        }

        if (!isDefeated)
        {
            isDefeated = true;
            if (health != null) health.isDead = true;
            StartCoroutine(BossDeathSequenceRoutine(title, kTeam, killer, coins, xp));
        }
    }

    // =========================
    // رسم شريط صحة الزعيم الضخم في الـ GUI
    // =========================
    void OnGUI()
    {
        if (isDormant || isDefeated || health == null || health.isDead) return;
        if (Camera.main == null) return;

        // التحقق من المسافة للكاميرا
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distance > 45f) return; // شريط الزعيم يرى من مسافة أبعد (45 متراً)

        Vector3 headPos = transform.position + Vector3.up * (bossScale * 1.5f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(headPos);

        if (screenPos.z < 0) return; // خلف الكاميرا

        float barWidth = Mathf.Clamp(140f - distance * 1.5f, 90f, 150f);
        float barHeight = 14f;

        float x = screenPos.x - barWidth / 2f;
        float y = Screen.height - screenPos.y - barHeight - 15f;
        // منع تداخل شريط الزعيم مع شريط أزرار الزاوية العلوية والمحفظة
        if (x > Screen.width - 320f && y < 80f) y = 80f;

        // إطار أسود ذهبي
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, barWidth + 4, barHeight + 4), Texture2D.whiteTexture);

        // شريط دم الزعيم
        float pct = health.currentHealth / health.maxHealth;
        GUI.color = Color.Lerp(Color.red, Color.yellow, pct);
        GUI.DrawTexture(new Rect(x, y, barWidth * pct, barHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;

        // عنوان الزعيم فوق الشريط (مترجم حسب لغة اللعبة المختارة)
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 11;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.yellow;

        GUI.Label(new Rect(x - 60, y - 18, barWidth + 120, 18), $"👑 {GetLocalizedTitle()}", titleStyle);

        // رقم الصحة
        GUIStyle hpStyle = new GUIStyle(GUI.skin.label);
        hpStyle.fontSize = 9;
        hpStyle.fontStyle = FontStyle.Bold;
        hpStyle.alignment = TextAnchor.MiddleCenter;
        hpStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x, y, barWidth, barHeight), $"{Mathf.CeilToInt(health.currentHealth)} / {health.maxHealth}", hpStyle);
    }

    public string GetLocalizedTitle()
    {
        if (levelNumber == 1)
        {
            return LanguageSelectionUI.Localize(
                "FOREST GOLIATH [LV 1]",
                ar: "جالوت الغابة [طابق 1]",
                fa: "جالوت جنگل [طبقه ۱]",
                de: "WALDGOLIATH [STUFE 1]",
                it: "GOLIA DELLA FORESTA [LIV 1]",
                tr: "ORMAN GOLYATI [SEV 1]",
                ru: "ГОЛИАФ ЛЕСА [УР 1]",
                fr: "GOLIATH DES BOIS [NV 1]",
                es: "GOLIAT DEL BOSQUE [NV 1]",
                hi: "वन गोलियत [स्तर 1]",
                zh: "森林歌利亚 [第1阶]",
                ja: "森のゴライアス [第1層]",
                pt: "GOLIAS DA FLORESTA [NÍV 1]"
            );
        }
        else if (levelNumber == 2)
        {
            return LanguageSelectionUI.Localize(
                "INFERNO TITAN [LV 2]",
                ar: "عملاق الجحيم [طابق 2]",
                fa: "تایتان دوزخ [طبقه ۲]",
                de: "INFERNO-TITAN [STUFE 2]",
                it: "TITANO INFERNALE [LIV 2]",
                tr: "CEHENNEM TİTANI [SEV 2]",
                ru: "ОГНЕННЫЙ ТИТАН [УР 2]",
                fr: "TITAN DE FEU [NV 2]",
                es: "TITÁN INFERNAL [NV 2]",
                hi: "पाताल टाइटन [स्तर 2]",
                zh: "炼狱泰坦 [第2阶]",
                ja: "業火のタイタン [第2層]",
                pt: "TITÃ INFERNAL [NÍV 2]"
            );
        }
        else
        {
            return LanguageSelectionUI.Localize(
                "ANCIENT OVERLORD [LV 3]",
                ar: "الطاغية القديم [طابق 3]",
                fa: "فرمانروای باستانی [طبقه ۳]",
                de: "URALTER HERRSCHER [STUFE 3]",
                it: "SOVRANO ANCESTRALE [LIV 3]",
                tr: "ANTİK HÜKÜMDAR [SEV 3]",
                ru: "ДРЕВНИЙ ПОВЕЛИТЕЛЬ [УР 3]",
                fr: "SEIGNEUR ANCESTRAL [NV 3]",
                es: "SEÑOR ANCESTRAL [NV 3]",
                hi: "प्राचीन अधिपति [स्तर 3]",
                zh: "远古霸主 [第3阶]",
                ja: "古代の覇王 [第3層]",
                pt: "SOBERANO ANCESTRAL [NÍV 3]"
            );
        }
    }
}
