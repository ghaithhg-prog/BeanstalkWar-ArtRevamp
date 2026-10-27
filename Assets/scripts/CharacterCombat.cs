using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class CharacterCombat : NetworkBehaviour
{
    private Vector3 lastAttackDirection =
        Vector3.forward;

    public enum AttackType
    {
        Melee,
        Ranged
    }

    [Header("Character Data")]
    public CharacterData characterData;

    [Header("Attack Settings")]
    public AttackType attackType =
        AttackType.Melee;

    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    public float attackRange = 2.5f;

    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Team")]
    public PlayerMovement.TeamType team;

    private float nextAttackTime = 0f;

    // زر الموبايل
    public static bool MobileAttackHeld { get; set; } = false;
    private bool attackButtonPressed = false;

    // هل هذا الكائن Bot؟
    private bool isBot = false;

    // =========================
    // Start
    // =========================

    void Start()
    {
        // حماية قطعية: إذا كان هذا الكائن زعيماً (LevelBoss)، فإنه يملك إعدادات قوة وصحة خاصة ومستقلة
        // ولا نطبق عليه سمات الشخصيات العادية الـ 200HP إطلاقاً
        if (GetComponent<LevelBoss>() != null)
        {
            return;
        }

        // إذا وجد BotController
        // فهذا ليس لاعبًا بشريًا.
        isBot =
            GetComponent<BotController>()
            != null;

        if (characterData != null)
        {
            ApplyCharacterData();
        }
    }

    // =========================
    // Character Data
    // =========================

    void ApplyCharacterData()
    {
        if (GetComponent<LevelBoss>() != null)
        {
            return;
        }

        attackDamage =
            characterData.attackDamage;

        attackCooldown =
            characterData.attackCooldown;

        attackRange =
            characterData.attackRange;

        attackType =
            characterData.attackType;

        PlayerMovement movement =
            GetComponent<PlayerMovement>();

        Health health =
            GetComponent<Health>();

        if (movement != null)
        {
            movement.moveSpeed =
                characterData.moveSpeed;
        }

        if (health != null)
        {
            health.maxHealth =
                characterData.maxHealth;

            health.currentHealth =
                characterData.maxHealth;
        }
    }

    // =========================
    // Update
    // =========================

    void Update()
    {
        // BOT ممنوع من قراءة
        // Mouse / Keyboard / Mobile Input.
        if (isBot)
            return;

        // اللاعب يتحكم فقط في شخصيته الخاصة
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworkActive && !IsOwner)
            return;

        // لا هجوم قبل بداية المباراة في طور الشبكة
        if (isNetworkActive && GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
        {
            return;
        }

        // لا يستطيع حامل الكريستالة استخدام الأسلحة العادية
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && movement.isCarryingCrystal.Value)
        {
            return;
        }

        AlignWithCamera();

        bool mouseAttack = Input.GetMouseButtonDown(0);
        bool keyAttack = Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E);
        bool mobileAttack = attackButtonPressed || MobileAttackHeld;
        attackButtonPressed = false;

        if (!mouseAttack && !keyAttack && !mobileAttack)
        {
            return;
        }

        // لا نهاجم إذا كان الضغط على زر تفاعلي في واجهة المستخدم
        if (mouseAttack && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var rayResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, rayResults);
            bool isButton = false;
            foreach (var r in rayResults)
            {
                if (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null || 
                    r.gameObject.GetComponent<UnityEngine.UI.Selectable>() != null)
                {
                    isButton = true;
                    break;
                }
            }
            if (isButton) return;
        }

        TryPlayerAttack();
    }

    // =========================
    // Player Attack
    // =========================

    void TryPlayerAttack()
    {
        if (Time.time <
            nextAttackTime)
        {
            return;
        }

        CalculateDirectionFromCenter();

        PerformAttack();

        nextAttackTime =
            Time.time +
            attackCooldown;
    }

    // =========================
    // AI Attack
    // =========================

    public bool TryAIAttack(
        Transform target)
    {
        // فقط Bot أو Boss يستطيع استخدام هذه الدالة
        if (!isBot && GetComponent<LevelBoss>() == null)
            return false;

        bool isServerOrAuthority = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer;
        if (!isServerOrAuthority)
            return false;

        if (target == null)
            return false;

        if (GameStateManager.Instance == null ||
            !GameStateManager.Instance.IsPlaying())
        {
            return false;
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && movement.isCarryingCrystal.Value)
        {
            return false;
        }

        if (Time.time <
            nextAttackTime)
        {
            return false;
        }

        // =========================
        // Direction
        // =========================

        Vector3 origin =
            firePoint != null
                ? firePoint.position
                : transform.position + Vector3.up * 1.2f;

        TreeGrowth targetTree = target.GetComponent<TreeGrowth>();
        if (targetTree == null) targetTree = target.GetComponentInParent<TreeGrowth>();
        if (targetTree == null && (target.CompareTag("Tree") || target.name.Contains("Trunk") || target.name.Contains("Tree")))
        {
            targetTree = TreeGrowth.Instance;
        }

        // إذا كان الهدف هو الشجرة، نصوب نحو جذعها على نفس ارتفاع البوت وليس نحو مركزها العلوي!
        Vector3 aimPoint = targetTree != null
            ? new Vector3(target.position.x, origin.y, target.position.z)
            : target.position;

        Vector3 direction =
            aimPoint -
            origin;

        if (direction.sqrMagnitude <
            0.001f)
        {
            return false;
        }

        lastAttackDirection =
            direction.normalized;

        // تدوير أفقي نحو الهدف.
        Vector3 flatDirection =
            new Vector3(
                direction.x,
                0f,
                direction.z
            );

        if (flatDirection.sqrMagnitude >
            0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    flatDirection
                );
        }

        PerformAttack();

        // ضمان إلحاق الضرر أو العلاج بالشجرة مباشرة عندما يهاجم البوت الشجرة عن قصد
        if (targetTree != null)
        {
            Vector3 flatDiff = new Vector3(target.position.x - transform.position.x, 0f, target.position.z - transform.position.z);
            float maxTreeReach = (attackType == AttackType.Ranged) ? Mathf.Max(9f, attackRange + 2f) : Mathf.Max(6.2f, attackRange + 4.0f);
            if (flatDiff.magnitude <= maxTreeReach)
            {
                PlayerXP xp = GetComponent<PlayerXP>();
                float xpMult = (xp != null) ? xp.GetDamageMultiplier() : 1f;
                float levelMult = TreeGrowth.GetHeightMultiplier(transform.position);
                // مضاعف فعال وقوي لتقطيع الشجرة للمدمرين وعلاجها للحماة مع الحفاظ على التكافؤ التام 50/50
                float treeImpactMult = (team == PlayerMovement.TeamType.Destroyer) ? 2.55f : 2.2f;
                float amount = attackDamage * levelMult * xpMult * treeImpactMult;

                if (team == PlayerMovement.TeamType.Destroyer)
                {
                    targetTree.TakeDamage(amount, gameObject);
                }
                else if (team == PlayerMovement.TeamType.Protector)
                {
                    targetTree.Heal(amount);
                }
            }
        }
        else if (target != null)
        {
            // ضمان إلحاق الضرر بالعدو المستهدف في قتال البوتات واللاعبين (سواء قتال قريب أو بعيد المدى)
            float maxReach = (attackType == AttackType.Ranged) ? Mathf.Max(14f, attackRange + 3.0f) : (attackRange + 2.5f);
            if (Vector3.Distance(transform.position, target.position) <= maxReach)
            {
                Health targetHp = target.GetComponent<Health>() ?? target.GetComponentInParent<Health>();
                CharacterCombat targetCombat = target.GetComponent<CharacterCombat>() ?? target.GetComponentInParent<CharacterCombat>() ?? (targetHp != null ? targetHp.GetComponent<CharacterCombat>() : null);
                if (targetHp != null && targetCombat != null && targetCombat.team != team && !targetHp.isDead)
                {
                    PlayerXP xp = GetComponent<PlayerXP>();
                    float xpMult = (xp != null) ? xp.GetDamageMultiplier() : 1f;
                    targetHp.TakeDamage(attackDamage * xpMult, gameObject);
                }
            }
        }

        nextAttackTime =
            Time.time +
            attackCooldown;

        return true;
    }

    // =========================
    // Camera Aim
    // =========================

    void AlignWithCamera()
    {
        if (Camera.main == null)
            return;

        Vector3 cameraForward =
            Camera.main.transform.forward;

        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude >
            0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    cameraForward
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    10f *
                    Time.deltaTime
                );
        }
    }

    // =========================
    // Center Aim
    // =========================

    public Vector3 CurrentAimTargetPoint { get; private set; }

    void CalculateDirectionFromCenter()
    {
        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(
                Screen.width / 2f,
                Screen.height / 2f,
                0f
            )
        );

        Vector3 targetPoint = ray.origin + ray.direction * 120f;

        // فحص دقيق يتجاهل تماماً كوليدر اللاعب أو الأسلحة، ويصيب الهدف المرصود بدقة
        RaycastHit[] hits = Physics.RaycastAll(ray, 250f);
        float closestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;

            // تجاهل جسم وسلاح وموديل مطلق السهم
            if (h.collider.gameObject == gameObject || h.collider.transform.IsChildOf(transform))
                continue;

            // تجاهل Trigger colliders غير الهادفة (Auras, Zones) إلا إذا كانت أهدافاً صالحة (شجرة، عدو)
            if (h.collider.isTrigger)
            {
                if (h.collider.GetComponentInParent<TreeGrowth>() == null &&
                    h.collider.GetComponentInParent<Health>() == null)
                {
                    continue;
                }
            }

            if (h.distance < closestDist && h.distance > 0.6f)
            {
                closestDist = h.distance;
                targetPoint = h.point;
            }
        }

        CurrentAimTargetPoint = targetPoint;

        Vector3 spawnOrigin = firePoint != null 
            ? firePoint.position 
            : transform.position + Vector3.up * 1.35f + transform.forward * 0.45f;

        lastAttackDirection = (targetPoint - spawnOrigin).normalized;
    }

    // =========================
    // Mobile Attack
    // =========================

    public void OnAttackButtonPressed()
    {
        if (isBot)
            return;

        attackButtonPressed = true;
    }

    public void SetAttackButtonHeld(bool held)
    {
        if (isBot)
            return;

        MobileAttackHeld = held;
    }

    // =========================
    // Perform Attack
    // =========================

    void PerformAttack()
    {
        CharacterVisualAnimator anim = GetComponentInChildren<CharacterVisualAnimator>();
        if (anim != null)
        {
            anim.TriggerAttackGesture();
        }

        if (attackType ==
            AttackType.Melee)
        {
            MeleeAttack();
        }
        else
        {
            RangedAttack();
        }
    }

    // =========================
    // Melee
    // =========================

    void MeleeAttack()
    {
        Vector3 attackPoint =
            transform.position +
            transform.forward *
            attackRange;

        Collider[] hits =
            Physics.OverlapSphere(
                attackPoint,
                1.8f
            );

        // الحصول على مضاعف XP
        PlayerXP xp = GetComponent<PlayerXP>();
        float xpMultiplier = (xp != null) ? xp.GetDamageMultiplier() : 1f;

        bool treeDamagedThisSwing = false;

        foreach (
            Collider hit in hits)
        {
            // =========================
            // Tree
            // =========================

            TreeGrowth tree =
                hit.GetComponent<TreeGrowth>();
            if (tree == null) tree = hit.GetComponentInParent<TreeGrowth>();
            if (tree == null && (hit.CompareTag("Tree") || hit.name.Contains("Trunk") || hit.name.Contains("Tree")))
            {
                tree = TreeGrowth.Instance;
            }

            if (tree != null && !treeDamagedThisSwing)
            {
                treeDamagedThisSwing = true;
                float levelMultiplier = TreeGrowth.GetHeightMultiplier(transform.position);
                float treeImpactMult = (team == PlayerMovement.TeamType.Destroyer) ? 2.55f : 2.2f;
                float finalAmount = attackDamage * levelMultiplier * xpMultiplier * treeImpactMult;

                if (team ==
                    PlayerMovement.TeamType.Destroyer)
                {
                    tree.TakeDamage(
                        finalAmount,
                        gameObject
                    );
                }
                else
                {
                    tree.Heal(
                        finalAmount
                    );
                }

                // منح XP عند ضرب الشجرة
                if (xp != null) xp.AddTreeHitXP();

                continue;
            }

            // =========================
            // Players / Bots
            // =========================

            Health health = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();

            if (health == null ||
                hit.gameObject == gameObject ||
                health.gameObject == gameObject)
            {
                continue;
            }

            CharacterCombat other = hit.GetComponent<CharacterCombat>() ?? hit.GetComponentInParent<CharacterCombat>() ?? health.GetComponent<CharacterCombat>();

            if (other != null &&
                other.team != team &&
                !health.isDead)
            {
                // تمرير المهاجم لتتبع القاتل ومنح XP
                health.TakeDamage(
                    attackDamage * xpMultiplier,
                    gameObject
                );
            }
        }

        // فحص إضافي مباشر لضمان تضرر الشجرة عندما يقف المدمر بجوارها ويهاجمها
        if (team == PlayerMovement.TeamType.Destroyer && !treeDamagedThisSwing)
        {
            GameObject treeObj = GameObject.FindGameObjectWithTag("Tree");
            if (treeObj != null)
            {
                Vector3 treePos = treeObj.transform.position;
                Vector3 flatDiff = treePos - transform.position;
                flatDiff.y = 0f;
                if (flatDiff.magnitude <= attackRange + 3.2f)
                {
                    float angle = Vector3.Angle(transform.forward, flatDiff.normalized);
                    if (angle <= 85f)
                    {
                        TreeGrowth tree = treeObj.GetComponent<TreeGrowth>();
                        if (tree != null)
                        {
                            float levelMultiplier = TreeGrowth.GetHeightMultiplier(transform.position);
                            float treeImpactMult = (team == PlayerMovement.TeamType.Destroyer) ? 2.55f : 2.2f;
                            float finalAmount = attackDamage * levelMultiplier * xpMultiplier * treeImpactMult;
                            if (team == PlayerMovement.TeamType.Destroyer)
                            {
                                tree.TakeDamage(finalAmount, gameObject);
                            }
                            else
                            {
                                tree.Heal(finalAmount);
                            }
                            if (xp != null) xp.AddTreeHitXP();
                        }
                    }
                }
            }
        }
    }

    // =========================
    // Ranged
    // =========================

    void RangedAttack()
    {
        // 1. اتجاه وموقع إطلاق السهم نحو النقطة التي يصوب عليها الكروس إير بدقة متناهية
        Vector3 spawnPos = firePoint != null 
            ? firePoint.position 
            : transform.position + Vector3.up * 1.35f + transform.forward * 0.45f;

        Vector3 flightDir;
        if (isBot)
        {
            flightDir = (lastAttackDirection.sqrMagnitude > 0.01f) ? lastAttackDirection.normalized : transform.forward;
        }
        else
        {
            flightDir = (CurrentAimTargetPoint - spawnPos).sqrMagnitude > 0.01f 
                ? (CurrentAimTargetPoint - spawnPos).normalized 
                : ((lastAttackDirection.sqrMagnitude > 0.01f) ? lastAttackDirection.normalized : transform.forward);
        }

        // 2. تحميل مجسم السهم الحقيقي الخاص بـ Erika Archer أولاً
        GameObject arrowPrefab = Resources.Load<GameObject>("Models/Erika_Arrow_Projectile");
        if (arrowPrefab == null) arrowPrefab = Resources.Load<GameObject>("Models/Arrow_3D");
        if (arrowPrefab == null) arrowPrefab = Resources.Load<GameObject>("Prefabs/Arrow");
        if (arrowPrefab == null) arrowPrefab = Resources.Load<GameObject>("Arrow");
        if (arrowPrefab == null) arrowPrefab = projectilePrefab;

        GameObject projectile = null;
        if (arrowPrefab != null)
        {
            projectile = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(flightDir));
            projectile.transform.localScale = Vector3.one * 1.25f;
            foreach (Animator anim in projectile.GetComponentsInChildren<Animator>())
            {
                Destroy(anim);
            }
            MeshFilter mf = projectile.GetComponentInChildren<MeshFilter>();
            SkinnedMeshRenderer smr = projectile.GetComponentInChildren<SkinnedMeshRenderer>();
            if ((mf == null || mf.sharedMesh == null) && (smr == null || smr.sharedMesh == null))
            {
                // مجسم البريفاب مفقود أو تالف! نتخلص منه ونبني سهماً حقيقياً مرئياً
                Destroy(projectile);
                projectile = null;
            }
        }

        if (projectile == null)
        {
            // بناء سهم متكامل ثلاثي الأبعاد يدوياً من مكونات Unity الأساسية - مرئي دائماً ومضمون
            projectile = BuildVisibleArrow(spawnPos, flightDir);
        }

        if (projectile == null) return;

        // 3. ضمان وجود مكون المقذوف
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj == null) proj = projectile.AddComponent<Projectile>();
        proj.damage = attackDamage;
        proj.team = team;
        proj.speed = 28f;
        proj.owner = gameObject;

        // 4. ضمان وجود كوليدر تصادم
        Collider col = projectile.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = projectile.AddComponent<SphereCollider>();
            sc.radius = 0.25f;
            sc.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        // 5. ضمان وجود Rigidbody
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 6. تطبيق خامة السهم الحقيقية (Arrow_DIFF) أو مادة السهم على كل Renderers الأطفال
        Texture2D arrowTex = Resources.Load<Texture2D>("Models/ErikaTextures/Arrow_DIFF");
        Material arrowMat = Resources.Load<Material>("Materials/Arrow_Material");
        foreach (Renderer r in projectile.GetComponentsInChildren<Renderer>())
        {
            if (r != null && r.material != null)
            {
                if (arrowTex != null)
                {
                    r.material.color = Color.white;
                    r.material.mainTexture = arrowTex;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", Color.white);
                    if (r.material.HasProperty("_BaseMap")) r.material.SetTexture("_BaseMap", arrowTex);
                }
                else if (arrowMat != null)
                {
                    r.material = arrowMat;
                }
                else
                {
                    r.material.color = new Color(0.76f, 0.52f, 0.28f);
                }
            }
        }

        // 7. إضافة تأثيرات مرئية (ذيل + ضوء)
        AddArrowTrailVisual(projectile);

        // 8. تشغيل صوت إطلاق القوس
        PlayBowReleaseSound();
    }

    /// <summary>بناء سهم مرئي بالكامل من مكونات Unity الأساسية دون الحاجة لأي FBX</summary>
    GameObject BuildVisibleArrow(Vector3 spawnPos, Vector3 flightDir)
    {
        // كائن رئيسي للسهم
        GameObject arrow = new GameObject("Arrow_Projectile");
        arrow.transform.position = spawnPos;
        arrow.transform.rotation = Quaternion.LookRotation(flightDir);

        // ——— جسم السهم (عمود خشبي) ———
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "ArrowShaft";
        shaft.transform.SetParent(arrow.transform, false);
        shaft.transform.localPosition = new Vector3(0f, 0f, 0f);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // الاستدارة ليتجه للأمام
        shaft.transform.localScale = new Vector3(0.06f, 0.38f, 0.06f);
        Collider shaftCol = shaft.GetComponent<Collider>();
        if (shaftCol != null) Object.Destroy(shaftCol);

        // ——— رأس السهم (مخروط صغير بواسطة Sphere) ———
        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "ArrowTip";
        tip.transform.SetParent(arrow.transform, false);
        tip.transform.localPosition = new Vector3(0f, 0f, 0.42f);
        tip.transform.localScale = new Vector3(0.09f, 0.09f, 0.18f);
        Collider tipCol = tip.GetComponent<Collider>();
        if (tipCol != null) Object.Destroy(tipCol);

        // ——— ريشة السهم (Cube مسطح) ———
        GameObject feather = GameObject.CreatePrimitive(PrimitiveType.Cube);
        feather.name = "ArrowFeather";
        feather.transform.SetParent(arrow.transform, false);
        feather.transform.localPosition = new Vector3(0f, 0f, -0.38f);
        feather.transform.localScale = new Vector3(0.14f, 0.08f, 0.12f);
        Collider featherCol = feather.GetComponent<Collider>();
        if (featherCol != null) Object.Destroy(featherCol);

        // تطبيق ألوان مناسبة
        Color woodColor = new Color(0.76f, 0.52f, 0.28f);
        Color metalColor = new Color(0.72f, 0.72f, 0.75f);
        Color featherColor = new Color(0.9f, 0.85f, 0.7f);

        SetPrimitiveColor(shaft, woodColor);
        SetPrimitiveColor(tip, metalColor);
        SetPrimitiveColor(feather, featherColor);

        return arrow;
    }

    void SetPrimitiveColor(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            r.material.color = color;
        }
    }

    void AddArrowTrailVisual(GameObject projectile)
    {
        Color teamCol = (team == PlayerMovement.TeamType.Protector) ? new Color(0.2f, 0.85f, 1f, 0.9f) : new Color(1f, 0.45f, 0.15f, 0.9f);

        TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.startWidth = 0.12f;
            trail.endWidth = 0.01f;
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null) trail.material = new Material(s);
            trail.startColor = teamCol;
            trail.endColor = new Color(teamCol.r, teamCol.g, teamCol.b, 0f);
        }

        // إضافة ضوء نقطي صغير يتبع السهم ليضيء مساره في الظلام
        Light arrowLight = projectile.GetComponent<Light>();
        if (arrowLight == null)
        {
            arrowLight = projectile.AddComponent<Light>();
            arrowLight.type = LightType.Point;
            arrowLight.color = teamCol;
            arrowLight.range = 5f;
            arrowLight.intensity = 2.5f;
        }
    }

    private static AudioClip cachedBowClip;

    void PlayBowReleaseSound()
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null) audio = gameObject.AddComponent<AudioSource>();

        audio.spatialBlend = 1.0f; // 3D sound so distant bots do not overwhelm audio
        audio.minDistance = 2.0f;
        audio.maxDistance = 20.0f;
        audio.rolloffMode = AudioRolloffMode.Linear;

        if (cachedBowClip == null)
        {
            cachedBowClip = CreateBowTwangClip();
        }

        if (cachedBowClip != null)
        {
            audio.PlayOneShot(cachedBowClip, 0.22f);
        }
    }

    static AudioClip CreateBowTwangClip()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        var rng = new System.Random(1337);

        // Low-pass filtered noise + soft woody thud
        float filterState = 0f;
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Attack & release envelope
            float attack = Mathf.Clamp01(t / 0.006f);
            float decay = Mathf.Exp(-t * 22f);
            float env = attack * decay;

            // Soft woody thud frequency (~140Hz decaying to ~90Hz)
            float freq = Mathf.Lerp(140f, 90f, t / duration);
            phase += 2f * Mathf.PI * freq / sampleRate;
            float thud = Mathf.Sin(phase);

            // Soft airy whoosh (filtered low noise)
            float rawNoise = (float)rng.NextDouble() * 2f - 1f;
            filterState += 0.08f * (rawNoise - filterState);
            float airWhoosh = filterState * 1.5f;

            // Combine woody pluck and airy whoosh
            float sample = (thud * 0.45f + airWhoosh * 0.55f) * env;
            samples[i] = Mathf.Clamp(sample * 0.6f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Bow_Twang", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}