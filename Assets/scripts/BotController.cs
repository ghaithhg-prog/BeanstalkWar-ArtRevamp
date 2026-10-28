using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class BotController : NetworkBehaviour
{
    public enum BotState
    {
        Defend,
        Advance,
        Fight,
        AttackTree,
        FetchCrystal,
        DeliverCrystal,
        EscortCarrier,
        HuntCarrier,
        GuardDroppedCrystal
    }

    [Header("Team")]
    public PlayerMovement.TeamType team;

    [Header("Hero Name")]
    public string botHeroName = "Champion";

    [Header("AI")]
    public BotState currentState;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float acceleration = 18f;
    public float angularSpeed = 360f;
    public float stoppingDistance = 0.8f;

    [Header("Fight")]
    public float detectionRange = 8f;
    public float loseEnemyRange = 12f;
    public float fightStoppingDistance = 2.5f;
    public float fightTimeout = 6f;

    [Header("Target")]
    public Transform treeTarget;

    [Header("Levels")]
    public Transform[] levelTargets;

    public Transform topLevelTarget;

    [Header("Protector")]
    public float minDefenseRadius = 5f;
    public float maxDefenseRadius = 8f;

    [Header("Decision")]
    public float decisionInterval = 0.5f;

    [Header("Tree Avoidance")]
    public float treeAvoidRadius = 4f;

    private NavMeshAgent agent;
    private Vector3 currentDestination;
    private bool hasDestination = false;
    private Transform currentTargetLevel;
    private Vector3 protectorTarget;
    private bool agentReady = false;
    private float nextDecisionTime = 0f;
    private float personalOffset;

    private Transform enemyTarget;
    private BotState stateBeforeFight;
    private float defendStartTime;
    private float fightStartTime;
    private float destinationSetTime;
    private float nextStrafeTime = 0f;
    private float currentStrafeSign = 1f;
    private float nextRepathTime = 0f;
    public const float DefaultNavMeshBaseOffset = 0.95f;
    private float lastBotJumpTime = -10f;
    private float botJumpCooldown = 4.0f;

    private bool IsServerOrAuthority => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer;

    void OnEnable()
    {
        LevelBossManager.OnSingleBossDefeated += HandleBossDefeated;
    }

    void OnDisable()
    {
        LevelBossManager.OnSingleBossDefeated -= HandleBossDefeated;
    }

    void HandleBossDefeated(string title, string killer, PlayerMovement.TeamType killerTeam, int level)
    {
        LeaveFight();
        hasDestination = false;
        currentTargetLevel = null;
    }

    void Start()
    {
        if (IsServerOrAuthority && agent == null)
        {
            InitializeBotServer();
        }
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsServerOrAuthority;

        if (!IsServerOrAuthority)
            return;

        InitializeBotServer();
    }

    void InitializeBotServer()
    {
        if (agent != null) return;
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("Bot has no NavMeshAgent.");
            enabled = false;
            return;
        }

        personalOffset = Random.Range(-0.5f, 0.5f);
        ConfigureAgent();
        FindTree();
        FindLevels();
        PrepareAgent();
        ChooseInitialState();
    }

    void ConfigureAgent()
    {
        agent.speed = moveSpeed + personalOffset;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.avoidancePriority = Random.Range(20, 80);
        agent.baseOffset = DefaultNavMeshBaseOffset;
    }

    public void OnRespawn(Vector3 targetSpawnPos)
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = true;
            agent.baseOffset = DefaultNavMeshBaseOffset;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetSpawnPos, out hit, 10f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                agentReady = true;
            }
            else
            {
                transform.position = targetSpawnPos;
                agentReady = false;
            }
            agent.isStopped = false;
            agent.ResetPath();
        }

        hasDestination = false;
        currentTargetLevel = null;
        enemyTarget = null;
        enabled = true;
        ChooseInitialState();
        nextDecisionTime = Time.time;
        Debug.Log($"🤖 Bot {gameObject.name} successfully respawned and resumed mission!");
    }

    void PrepareAgent()
    {
        if (agent == null) return;
        agent.baseOffset = DefaultNavMeshBaseOffset;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(transform.position, out hit, 8f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agentReady = true;
        }
        else
        {
            agentReady = false;
        }
    }

    void FindTree()
    {
        if (TreeGrowth.Instance != null)
        {
            treeTarget = TreeGrowth.Instance.transform;
            return;
        }
        GameObject tree = GameObject.FindGameObjectWithTag("Tree") ?? GameObject.Find("Tree");
        if (tree != null) treeTarget = tree.transform;
    }

    void FindLevels()
    {
        GameObject treeLevels = GameObject.Find("TreeLevels");
        if (treeLevels == null) return;

        levelTargets = new Transform[3];
        levelTargets[0] = treeLevels.transform.Find("Level_1");
        levelTargets[1] = treeLevels.transform.Find("Level_2");
        levelTargets[2] = treeLevels.transform.Find("Level_3");

        topLevelTarget = treeLevels.transform.Find("TopLevel");
    }

    void ChooseInitialState()
    {
        if (team == PlayerMovement.TeamType.Destroyer)
        {
            if (Random.value < 0.95f)
                currentState = BotState.Advance;
            else
                currentState = BotState.Defend;
        }
        else
        {
            if (Random.value < 0.15f)
            {
                currentState = BotState.Defend;
                FindProtectorPosition();
            }
            else
            {
                currentState = BotState.Advance;
            }
        }

        defendStartTime = Time.time;
        hasDestination = false;
        currentTargetLevel = null;
    }

    public void OnCrystalPickedUpByMe()
    {
        enemyTarget = null;
        currentState = BotState.DeliverCrystal;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0.1f;
        }
    }

    /// <summary>
    /// رد الفعل الفوري والذكي عند تعرض البوت للهجوم (Aggro, Self-Defense & Jump/Dodge):
    /// يدافع عن نفسه فوراً، يهاجم المعتدي، ويراوغ بالقفز والحركة التكتيكية.
    /// </summary>
    public void OnTakeDamageFrom(GameObject attacker, float damage = 0f)
    {
        if (attacker == null || attacker == gameObject) return;
        Health myHealth = GetComponent<Health>();
        if (myHealth != null && myHealth.isDead) return;

        // إذا كان حامياً يحمل الكريستالة متجهاً للمذبح، يركز على إيصالها ويطلق صدمة دفاعية
        if (currentState == BotState.DeliverCrystal)
        {
            if (CrystalItem.Instance != null && Vector3.Distance(transform.position, attacker.transform.position) <= 5.5f)
            {
                CrystalItem.Instance.RequestShockwaveRpc();
            }
            return;
        }

        CharacterCombat attCombat = attacker.GetComponent<CharacterCombat>();
        if (attCombat != null && attCombat.team == team) return; // تجاهل النيران الصديقة

        // رد الهجوم الفوري والانتقام من المعتدي
        EnterFight(attacker.transform);

        // قفزة تكتيكية رشيقة ومحسوبة للمراوغة بنسبة متزنة (25% وبفاصل زمني كافٍ)
        if (Random.value < 0.25f)
        {
            TriggerBotJump();
        }
    }

    /// <summary>
    /// تنفيذ قفزة انسيابية متزنة للبوت متزامنة مع أنيميشن Mixamo
    /// مع الحفاظ الدائم على ثبات القدمين على سطح الأرض (BaseOffset = 0.95f)
    /// </summary>
    public void TriggerBotJump()
    {
        if (Time.time - lastBotJumpTime < botJumpCooldown) return;
        if (agent == null || !agent.isOnNavMesh) return;
        Health myHp = GetComponent<Health>();
        if (myHp != null && myHp.isDead) return;

        lastBotJumpTime = Time.time;
        agent.baseOffset = DefaultNavMeshBaseOffset;

        CharacterVisualAnimator visualAnim = GetComponentInChildren<CharacterVisualAnimator>();
        if (visualAnim != null)
        {
            visualAnim.TriggerJumpGesture();
        }
    }

    public Transform GetLevel3Transform()
    {
        if (levelTargets != null && levelTargets.Length > 2 && levelTargets[2] != null) return levelTargets[2];
        GameObject treeLevels = GameObject.Find("TreeLevels");
        return treeLevels != null ? treeLevels.transform.Find("Level_3") : null;
    }

    void Update()
    {
        if (!IsServerOrAuthority || agent == null) return;

        if (GameStateManager.Instance == null)
        {
            GameStateManager.Instance = FindAnyObjectByType<GameStateManager>();
        }

        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        if (!agentReady)
        {
            PrepareAgent();
            if (!agentReady) return;
        }

        if (!agent.isOnNavMesh)
        {
            agentReady = false;
            return;
        }

        agent.isStopped = false;

        if (hasDestination)
        {
            bool arrived = !agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance + 0.4f || Vector3.Distance(transform.position, currentDestination) <= 1.2f);
            bool timedOut = Time.time - destinationSetTime > 5.5f;

            if (arrived || timedOut)
            {
                hasDestination = false;
                currentTargetLevel = null;
            }
        }

        // حماية تامة من اختراق جذع الشجرة
        EnforceTreeTrunkAvoidance();

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;
            UpdateDecision();
        }

        bool shouldSprint = (currentState == BotState.DeliverCrystal || 
                             currentState == BotState.FetchCrystal ||
                             currentState == BotState.HuntCarrier ||
                             currentState == BotState.EscortCarrier ||
                             currentState == BotState.Fight ||
                             currentState == BotState.Advance);

        if (currentState == BotState.AttackTree && treeTarget != null)
        {
            Vector3 treeDiff = transform.position - treeTarget.position;
            treeDiff.y = 0f;
            if (treeDiff.magnitude > 5.5f) shouldSprint = true;
        }

        if (agent.isOnNavMesh)
        {
            float targetSpeed = shouldSprint ? (sprintSpeed + personalOffset) : (moveSpeed + personalOffset);
            agent.speed = targetSpeed;
        }

        switch (currentState)
        {
            case BotState.Defend:
                UpdateDefend();
                break;

            case BotState.Advance:
                UpdateAdvance();
                break;

            case BotState.Fight:
                UpdateFight();
                break;

            case BotState.AttackTree:
                UpdateAttackTree();
                break;

            case BotState.FetchCrystal:
                UpdateFetchCrystal();
                break;

            case BotState.DeliverCrystal:
                UpdateDeliverCrystal();
                break;

            case BotState.EscortCarrier:
                UpdateEscortCarrier();
                break;

            case BotState.HuntCarrier:
                UpdateHuntCarrier();
                break;

            case BotState.GuardDroppedCrystal:
                UpdateGuardDroppedCrystal();
                break;
        }
    }

    void UpdateDecision()
    {
        if (treeTarget == null || !treeTarget.gameObject.activeInHierarchy) FindTree();
        if (levelTargets == null || levelTargets.Length == 0 || levelTargets[0] == null) FindLevels();

        bool allBossesSlain = LevelBossManager.Instance != null && LevelBossManager.Instance.AreAllBossesDefeated;

        // 1. إذا كان هذا البوت يحمل الكريستالة، هدفه الحتمي إيصالها للمذبح للفوز!
        if (CrystalItem.Instance != null && CrystalItem.Instance.IsCarrier(gameObject))
        {
            enemyTarget = null;
            currentState = BotState.DeliverCrystal;
            if (agent != null) agent.stoppingDistance = 0.2f;
            return;
        }

        // 2. إذا كانت الكريستالة يحملها لاعب آخر أو بوت آخر
        if (CrystalItem.Instance != null && CrystalItem.Instance.isCarried.Value)
        {
            GameObject carrier = CrystalItem.Instance.GetCarrierObject();
            Health carrierHealth = carrier != null ? carrier.GetComponent<Health>() : null;

            if (carrier != null && (carrierHealth == null || !carrierHealth.isDead))
            {
                if (team == PlayerMovement.TeamType.Protector)
                {
                    // إذا كان الحامي يقوم بتعميد الكريستالة في المذبح (عد 3 ثوانٍ):
                    // يندفع بقية الحماة فوراً لحمايته وتشكيل درع دفاعي حول المذبح!
                    if (CrystalAltar.IsAnyAltarChannelling)
                    {
                        currentState = BotState.EscortCarrier;
                        return;
                    }

                    Transform closeEnemy = FindClosestEnemy();
                    if (closeEnemy != null && Vector3.Distance(transform.position, closeEnemy.position) <= 6f)
                    {
                        EnterFight(closeEnemy);
                    }
                    else
                    {
                        currentState = BotState.EscortCarrier;
                    }
                    return;
                }
                else // Destroyer:
                {
                    // ذكاء تكتيكي خارق: إذا كان الحامي يقوم بتعميد الكريستالة في المذبح (عد 3 ثوانٍ):
                    // يتحول جميع المدمرين بدون استثناء وبأقصى سرعة لمهاجمة حامل الكريستالة وقتله فوراً لإيقاف التعميد!
                    if (CrystalAltar.IsAnyAltarChannelling)
                    {
                        EnterFight(carrier.transform);
                        return;
                    }

                    // تقسيم استراتيجي للمدمرين: 70% تدمير الشجرة و30% ملاحقة حامل الكريستالة ومنعه من الوصول للمذبح
                    int botRatio = Mathf.Abs(gameObject.GetInstanceID()) % 10;
                    if (botRatio >= 7) // 30% صيادو الكريستالة
                    {
                        EnterFight(carrier.transform);
                        return;
                    }
                    else // 70% مهاجمو الشجرة
                    {
                        enemyTarget = null;
                        currentState = BotState.AttackTree;
                        return;
                    }
                }
            }
        }

        // 3. بعد موت الزعماء الثلاثة أو توفر الكريستالة على الأرض:
        // حماة الشجرة (Protector Bots) ينطلقون بأنفسهم فوراً لأخذ الكريستالة وإيصالها للمذبح دون انتظار اللاعب!
        if (CrystalItem.Instance != null && (CrystalItem.Instance.IsAvailableForPickup || (allBossesSlain && !CrystalItem.Instance.isCarried.Value)))
        {
            if (!CrystalItem.Instance.IsAvailableForPickup && allBossesSlain)
            {
                CrystalItem.Instance.SpawnAtTopServer();
            }

            if (team == PlayerMovement.TeamType.Protector)
            {
                enemyTarget = null;
                currentState = BotState.FetchCrystal;
                if (agent != null) agent.stoppingDistance = 0.2f;
                return;
            }
        }

        // 4. للمدمرين (Destroyer Bots): بمجرد موت الزعماء الثلاثة وسقوط الدرع:
        if (team == PlayerMovement.TeamType.Destroyer && allBossesSlain)
        {
            int botRatio = Mathf.Abs(gameObject.GetInstanceID()) % 10;
            // 15% فقط من المدمرين يراقبون الكريستالة الساقطة بينما 85% يصبون كل تركيزهم على تقطيع الشجرة
            if (botRatio >= 9 && CrystalItem.Instance != null && CrystalItem.Instance.isDropped.Value)
            {
                currentState = BotState.GuardDroppedCrystal;
                return;
            }

            Transform veryCloseEnemy = FindClosestEnemy();
            // لا ينشغل المدمر بقتال جانبي إلا إذا كان العدو ملاصقاً له جداً (أقل من 2.0 م)
            if (veryCloseEnemy != null && Vector3.Distance(transform.position, veryCloseEnemy.position) <= 2.0f)
            {
                EnterFight(veryCloseEnemy);
                return;
            }

            // تدمير الشجرة بأقصى قوة: الصعود إلى الطابق الثالث إن أمكن لضرب الشجرة بمضاعف 100%!
            Transform l3 = GetLevel3Transform();
            if (l3 != null && (l3.position.y - transform.position.y) > 2.0f)
            {
                // إذا كان البوت يضرب الشجرة بالفعل على طابقه الحالي في نطاق الهجوم، يضرب فوراً
                CharacterCombat myCombat = GetComponent<CharacterCombat>();
                float reach = myCombat != null ? Mathf.Max(6.5f, myCombat.attackRange + 4.0f) : 6.5f;
                Vector3 treeDiff = treeTarget != null ? (transform.position - treeTarget.position) : Vector3.zero;
                treeDiff.y = 0f;
                if (treeDiff.magnitude <= reach)
                {
                    enemyTarget = null;
                    currentState = BotState.AttackTree;
                    return;
                }

                currentState = BotState.Advance;
                hasDestination = false;
                currentTargetLevel = l3;
                return;
            }

            // تركيز مباشر كاسح على ضرب الشجرة وتدميرها
            enemyTarget = null;
            currentState = BotState.AttackTree;
            return;
        }

        // فحص هل يمكن إلحاق ضرر إضافي بالشجرة حالياً (قبل بلوغ حد الـ 35% من صحتها في وجود الدرع)
        bool canTreeTakeMoreDamage = TreeGrowth.Instance != null &&
            (TreeGrowth.Instance.currentHealth > (TreeGrowth.IsShieldActive ? (TreeGrowth.Instance.maxHealth * 0.355f) : 10f));

        // 5. التحقق من الزعيم المستيقظ والطابق القتالي النشط:
        LevelBoss activeBoss = (LevelBossManager.Instance != null && !allBossesSlain)
            ? LevelBossManager.Instance.GetActiveAwakenedBoss()
            : null;
        bool hasBossAbove = activeBoss != null && (activeBoss.transform.position.y - transform.position.y) > 2.2f;

        Transform targetCombatLevel = GetBotCurrentCombatTargetLevel();
        bool hasHigherCombatLevel = targetCombatLevel != null && (targetCombatLevel.position.y - transform.position.y) > 2.0f;

        // الصعود السريع للأدوار العليا (Level 1, 2, 3):
        // كما طلب المستخدم: الصعود يمنح مضاعف ضرر عالي للشجرة (35% في الطابق الأول، 60% في الثاني، 100% في الثالث)
        // بالإضافة لاستهداف الزعيم النشط لكسر درع الشجرة!
        if (hasBossAbove || hasHigherCombatLevel)
        {
            // دفاع عن النفس: إذا كان هناك عدو ملتصق بنا في نطاق قريب على نفس الطابق، ندافع ونرد الهجوم
            Transform immediateThreat = FindClosestEnemy();
            if (immediateThreat != null)
            {
                float vDist = Mathf.Abs(immediateThreat.position.y - transform.position.y);
                float hDist = Vector3.Distance(transform.position, immediateThreat.position);
                CharacterCombat myCombat = GetComponent<CharacterCombat>();
                bool isRanged = myCombat != null && (myCombat.attackType == CharacterCombat.AttackType.Ranged || myCombat.attackRange >= 5.5f);
                float threatDist = isRanged ? 7.5f : 5.0f;

                if (vDist <= 2.5f && hDist <= threatDist && (Time.time - fightStartTime) < 3.0f)
                {
                    EnterFight(immediateThreat);
                    return;
                }
            }

            int botRatio = Mathf.Abs(gameObject.GetInstanceID()) % 10;
            if (team == PlayerMovement.TeamType.Destroyer)
            {
                // إذا كان البوت على الأرض (y < 3.2m):
                // 85% من المدمرين ينطلقون صعوداً بسرعة عبر الممرات والمنصات (Advance)
                // وفقط 15% (نسبة botRatio == 0) يشاغلون الشجرة من الأسفل إن كانت تقبل الضرر
                if (transform.position.y < 3.2f && botRatio == 0 && canTreeTakeMoreDamage)
                {
                    enemyTarget = null;
                    currentState = BotState.AttackTree;
                    return;
                }

                // الصعود الحتمي والسريع إلى الطابق الأعلى (Advance)
                currentState = BotState.Advance;
                hasDestination = false;
                currentTargetLevel = null;
                return;
            }
            else // Protector:
            {
                currentState = BotState.Advance;
                hasDestination = false;
                currentTargetLevel = null;
                return;
            }
        }

        // 6. للمدمرين (Destroyer Bots) إذا كان الزعيم المستيقظ على نفس الطابق أو تم الوصول إليه:
        if (team == PlayerMovement.TeamType.Destroyer && activeBoss != null)
        {
            // إذا كان هناك عدو (حامي) في النطاق يهددنا أو يهاجمنا على نفس الطابق، نقاتله وندافع عن أنفسنا
            Transform immediateThreat = FindClosestEnemy();
            CharacterCombat myCombat = GetComponent<CharacterCombat>();
            bool isRanged = myCombat != null && (myCombat.attackType == CharacterCombat.AttackType.Ranged || myCombat.attackRange >= 5.5f);
            float threatDist = isRanged ? 7.5f : 5.0f;
            if (immediateThreat != null && Vector3.Distance(transform.position, immediateThreat.position) <= threatDist &&
                Mathf.Abs(immediateThreat.position.y - transform.position.y) <= 2.5f)
            {
                EnterFight(immediateThreat);
                return;
            }

            // إذا بلغت الشجرة حد الدرع (35%): تركيز مطلق 100% على قتل الزعيم لكسر الدرع!
            if (!canTreeTakeMoreDamage)
            {
                EnterFight(activeBoss.transform);
                return;
            }

            // إذا كانت الشجرة ما تزال تقبل الضرر فوق 35%: 65% يركزون على الزعيم و35% يضربون الشجرة بضرر الطابق المضاعف!
            int botRatio = Mathf.Abs(gameObject.GetInstanceID()) % 10;
            if (botRatio < 6)
            {
                EnterFight(activeBoss.transform);
                return;
            }
            else
            {
                enemyTarget = null;
                currentState = BotState.AttackTree;
                return;
            }
        }

        // 7. إذا كان مدمراً (Destroyer) ولا يوجد زعيم مستيقظ حالياً: الصعود نحو الطابق النشط ومقاتلة الأعداء
        if (team == PlayerMovement.TeamType.Destroyer)
        {
            Transform closeEnemy = FindClosestEnemy();
            if (closeEnemy != null)
            {
                float vertDist = Mathf.Abs(closeEnemy.position.y - transform.position.y);
                float dist = Vector3.Distance(transform.position, closeEnemy.position);
                // قتال العدو فقط إذا كان على نفس الطابق أو في نطاق متقارب
                if (vertDist <= 2.8f && dist <= 12.0f)
                {
                    EnterFight(closeEnemy);
                    return;
                }
            }

            Transform targetLevel = GetBotCurrentCombatTargetLevel();
            // إذا كان الطابق القتالي النشط (Level 1, 2, 3, TopLevel) أعلى من موقع البوت الحالي، يصعد فوراً عبر الممرات!
            if (targetLevel != null && (targetLevel.position.y - transform.position.y) > 2.0f)
            {
                currentState = BotState.Advance;
                hasDestination = false;
                currentTargetLevel = null;
                return;
            }

            enemyTarget = null;
            currentState = BotState.AttackTree;
            return;
        }

        // 8. إذا كان حامياً (Protector): الدفاع أولاً عن النفس وعن الشجرة ضد الأعداء القريبين
        if (team == PlayerMovement.TeamType.Protector)
        {
            Transform foundEnemy = FindClosestEnemy();
            if (foundEnemy != null)
            {
                float vertDist = Mathf.Abs(foundEnemy.position.y - transform.position.y);
                float dist = Vector3.Distance(transform.position, foundEnemy.position);
                CharacterCombat myCombat = GetComponent<CharacterCombat>();
                bool isRanged = myCombat != null && (myCombat.attackType == CharacterCombat.AttackType.Ranged || myCombat.attackRange >= 5.5f);
                float defRange = isRanged ? 7.5f : 5.0f;
                if (vertDist <= 2.5f && dist <= defRange)
                {
                    EnterFight(foundEnemy);
                    return;
                }
            }

            Transform targetLevel = GetBotCurrentCombatTargetLevel();
            if (targetLevel != null && (targetLevel.position.y - transform.position.y) > 2.0f)
            {
                currentState = BotState.Advance;
                hasDestination = false;
                currentTargetLevel = null;
                return;
            }

            if (foundEnemy != null)
            {
                EnterFight(foundEnemy);
                return;
            }

            if (currentState == BotState.Defend)
            {
                if (Time.time - defendStartTime > 4f)
                {
                    currentState = BotState.Advance;
                    hasDestination = false;
                    currentTargetLevel = null;
                }
            }
            else if (currentState != BotState.Advance)
            {
                currentState = BotState.Defend;
                defendStartTime = Time.time;
            }
        }
    }

    void UpdateAttackTree()
    {
        if (treeTarget == null)
        {
            FindTree();
            if (treeTarget == null) return;
        }

        Vector3 treeCenter = treeTarget.position;
        Vector3 diff = transform.position - treeCenter;
        diff.y = 0f;
        float dist = diff.magnitude;

        CharacterCombat combat = GetComponent<CharacterCombat>();
        bool isRanged = combat != null && (combat.attackType == CharacterCombat.AttackType.Ranged || combat.attackRange >= 5.5f);
        float maxAttackDist = isRanged ? Mathf.Max(6.8f, combat.attackRange * 0.85f) : 5.6f;

        // التحرك إلى المحيط المباشر للشجرة (4.15 م) إذا كان البوت بعيداً
        if (dist > maxAttackDist)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.stoppingDistance = 0.3f;
            }
            Vector3 dest = FindClosestTreePosition();
            if (!hasDestination || Vector3.Distance(currentDestination, dest) > 1.2f)
            {
                SetNewDestination(dest);
            }
        }
        else
        {
            // في مدى ضرب الشجرة: توقف، التفت نحو جذع الشجرة، واضربه بكل قوة!
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            Vector3 lookDir = (treeCenter - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 12f * Time.deltaTime);
            }

            if (combat != null)
            {
                combat.TryAIAttack(treeTarget);
            }
        }
    }

    void UpdateFetchCrystal()
    {
        if (CrystalItem.Instance == null)
        {
            currentState = BotState.Defend;
            return;
        }

        // إذا كنت أنا حامل الكريستالة، أنطلق فوراً إلى المذبح دون أي تأخير!
        if (CrystalItem.Instance.IsCarrier(gameObject))
        {
            enemyTarget = null;
            currentState = BotState.DeliverCrystal;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.stoppingDistance = 0.1f;
            }
            return;
        }

        // إذا كان هناك حامي آخر يحمل الكريستالة بالفعل، أرافقه وأحميه فوراً!
        if (CrystalItem.Instance.isCarried.Value)
        {
            currentState = BotState.EscortCarrier;
            return;
        }

        // إذا لم تعد متاحة للالتقاط ولم يحملها أحد، عودة للدفاع
        if (!CrystalItem.Instance.IsAvailableForPickup)
        {
            currentState = BotState.Defend;
            return;
        }

        Vector3 crystalPos = CrystalItem.Instance.transform.position;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0.1f;

            Vector3 targetNavPos = crystalPos;
            if (NavMesh.SamplePosition(crystalPos, out NavMeshHit navHit, 8f, NavMesh.AllAreas))
            {
                targetNavPos = navHit.position;
            }

            if (!hasDestination || Vector3.Distance(currentDestination, targetNavPos) > 0.6f)
            {
                currentDestination = targetNavPos;
                hasDestination = true;
                destinationSetTime = Time.time;
                agent.SetDestination(targetNavPos);
            }
        }

        // إذا اقترب البوت الحامي من الكريستالة يلتقطها فوراً دون أي تردد أو انتظار
        Vector3 horizDiff = transform.position - crystalPos;
        horizDiff.y = 0f;
        if (horizDiff.magnitude <= (CrystalItem.Instance.pickupRadius + 2.4f) &&
            Mathf.Abs(transform.position.y - crystalPos.y) <= 5.5f)
        {
            CrystalItem.Instance.TryPickupServer(gameObject);
            if (CrystalItem.Instance.IsCarrier(gameObject))
            {
                enemyTarget = null;
                currentState = BotState.DeliverCrystal;
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = 0.1f;
                }
                return;
            }
        }
    }

    Transform FindClosestEnemy()
    {
        CharacterCombat[] characters = FindObjectsByType<CharacterCombat>(FindObjectsSortMode.None);
        Transform bestTarget = null;
        float bestScore = detectionRange * detectionRange;

        foreach (CharacterCombat character in characters)
        {
            if (character == null || character.gameObject == gameObject) continue;
            if (character.team == team) continue;

            // لا نستهدف زعيماً نائماً لم يُفتح طابقه بعد، ولا زعيماً في مستوى أعلى من المستوى الأدنى الحي
            LevelBoss bossComp = character.GetComponent<LevelBoss>();
            if (bossComp != null)
            {
                if (!bossComp.IsAwakenedAndAlive) continue;
                LevelBoss lowestActiveBoss = LevelBossManager.Instance != null ? LevelBossManager.Instance.GetActiveAwakenedBoss() : null;
                if (lowestActiveBoss != null && bossComp.levelNumber > lowestActiveBoss.levelNumber)
                {
                    continue; // البقاء في قتال الزعيم الأدنى حتى يموت قبل الانتقال للزعيم الأعلى
                }
            }

            // إذا كان هناك زعيم حي ومستيقظ في طابق علوي، نتجاهل الأعداء العاديين على الطوابق السفلية لنركز على الزعيم والصعود
            LevelBoss lowestActive = LevelBossManager.Instance != null ? LevelBossManager.Instance.GetActiveAwakenedBoss() : null;
            if (lowestActive != null && (lowestActive.transform.position.y - transform.position.y) > 2.8f)
            {
                // إذا كان هذا الكائن ليس الزعيم وهو على طابق سفلي، نتجاهله إلا إذا كان قريباً بما يكفي لتهديدنا
                if (bossComp == null && Mathf.Abs(character.transform.position.y - transform.position.y) <= 2.5f)
                {
                    float d = Vector3.Distance(transform.position, character.transform.position);
                    if (d > 6.5f) continue;
                }
            }

            Health health = character.GetComponent<Health>();
            if (health == null || health.isDead) continue;

            float sqrDistance = (character.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance > detectionRange * detectionRange) continue;

            // ذكاء تكتيكي: إعطاء أولوية للأعداء ذوي الصحة المنخفضة لحسم القتل، ولحامل الكريستالة!
            float effectiveScore = sqrDistance;
            float hpPct = health.maxHealth > 0f ? (health.currentHealth / health.maxHealth) : 1f;
            if (hpPct < 0.35f)
            {
                effectiveScore *= 0.45f; // تركيز الهجوم على العدو الجريح
            }

            if (CrystalItem.Instance != null && CrystalItem.Instance.IsCarrier(character.gameObject))
            {
                effectiveScore *= 0.25f; // أولوية قصوى لحامل الكريستالة
            }

            if (effectiveScore <= bestScore)
            {
                bestScore = effectiveScore;
                bestTarget = character.transform;
            }
        }

        return bestTarget;
    }

    void EnterFight(Transform target)
    {
        if (target == null) return;

        stateBeforeFight = currentState;
        currentState = BotState.Fight;
        enemyTarget = target;
        hasDestination = false;
        fightStartTime = Time.time;

        CharacterCombat combat = GetComponent<CharacterCombat>();
        bool isRanged = combat != null && (combat.attackType == CharacterCombat.AttackType.Ranged || combat.attackRange >= 5.5f);

        if (agent != null)
        {
            agent.stoppingDistance = isRanged ? Mathf.Clamp(combat.attackRange * 0.65f, 4.5f, 8.5f) : fightStoppingDistance;
        }
    }

    // قتال تكتيكي ذكي (Strafing & Kiting للرماة، واندفاع ومراوغة للمحاربين)
    void UpdateFight()
    {
        if (enemyTarget == null)
        {
            LeaveFight();
            return;
        }

        Health targetHealth = enemyTarget.GetComponent<Health>();
        if (targetHealth != null && targetHealth.isDead)
        {
            LeaveFight();
            return;
        }

        LevelBoss targetBoss = enemyTarget.GetComponent<LevelBoss>();
        if (targetBoss != null && !targetBoss.IsAwakenedAndAlive)
        {
            LeaveFight();
            return;
        }

        if (!IsEnemyStillValid())
        {
            LeaveFight();
            return;
        }

        float distance = Vector3.Distance(transform.position, enemyTarget.position);

        // إذا كان هناك زعيم مستيقظ في طابق علوي، الأولوية الحاسمة هي الصعود له وليس البقاء في شجار أرضي
        if (targetBoss == null && LevelBossManager.Instance != null && !LevelBossManager.Instance.AreAllBossesDefeated)
        {
            LevelBoss awakenedBoss = LevelBossManager.Instance.GetActiveAwakenedBoss();
            if (awakenedBoss != null && (awakenedBoss.transform.position.y - transform.position.y) > 2.0f)
            {
                LeaveFightAndAdvance();
                return;
            }
        }

        CharacterCombat combat = GetComponent<CharacterCombat>();
        bool isRanged = combat != null && (combat.attackType == CharacterCombat.AttackType.Ranged || combat.attackRange >= 5.5f);
        float effectiveRange = isRanged ? Mathf.Max(combat.attackRange * 0.85f, 7.5f) : Mathf.Max(fightStoppingDistance, 2.6f);

        float verticalDist = Mathf.Abs(enemyTarget.position.y - transform.position.y);
        bool onSameFloor = verticalDist <= 2.8f;

        if (distance <= effectiveRange && onSameFloor)
        {
            Vector3 toEnemy = (enemyTarget.position - transform.position);
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toEnemy), 12f * Time.deltaTime);
            }

            if (agent != null && agent.isOnNavMesh)
            {
                // حركة تكتيكية ناعمة تتغير فقط كل 2.0 إلى 2.5 ثانية بدلاً من التذبذب كل فريم (إلغاء مشكلة ارتعاش الصخور تماماً!)
                if (Time.time >= nextStrafeTime)
                {
                    nextStrafeTime = Time.time + Random.Range(1.8f, 2.6f);
                    currentStrafeSign = (Random.value > 0.5f) ? 1f : -1f;

                    Vector3 rightVec = Vector3.Cross(Vector3.up, toEnemy.normalized);
                    if (isRanged && distance < 4.0f)
                    {
                        Vector3 kitePos = transform.position - toEnemy.normalized * 2.5f + rightVec * (currentStrafeSign * 1.5f);
                        if (NavMesh.SamplePosition(kitePos, out NavMeshHit kiteHit, 3f, NavMesh.AllAreas))
                        {
                            agent.isStopped = false;
                            agent.stoppingDistance = 0.2f;
                            agent.SetDestination(kiteHit.position);
                        }
                    }
                    else if (isRanged || distance > 2.2f)
                    {
                        Vector3 strafePos = transform.position + rightVec * (currentStrafeSign * (isRanged ? 2.0f : 1.2f));
                        if (NavMesh.SamplePosition(strafePos, out NavMeshHit strafeHit, 2.5f, NavMesh.AllAreas))
                        {
                            agent.isStopped = false;
                            agent.stoppingDistance = 0.2f;
                            agent.SetDestination(strafeHit.position);
                        }
                    }
                    else
                    {
                        // المقاتل القريب يثبت أقدامه ويهاجم بحزم دون أي ارتعاش
                        agent.isStopped = true;
                    }
                }
            }

            if (combat != null)
            {
                combat.TryAIAttack(enemyTarget);
            }
        }
        else
        {
            // مطاردة الهدف أو الصعود إلى طابق الزعيم بسلاسة ودون تكرار الاستعلام في كل إطار
            if (agent != null && agent.isOnNavMesh)
            {
                if (Time.time >= nextRepathTime || agent.isStopped)
                {
                    nextRepathTime = Time.time + 0.35f;
                    agent.isStopped = false;
                    agent.stoppingDistance = onSameFloor ? (isRanged ? 5.0f : fightStoppingDistance) : 0.4f;
                    agent.SetDestination(enemyTarget.position);
                }
            }
        }
    }

    bool IsEnemyStillValid()
    {
        if (enemyTarget == null) return false;

        Health health = enemyTarget.GetComponent<Health>();
        if (health == null || health.isDead) return false;

        CharacterCombat combat = enemyTarget.GetComponent<CharacterCombat>();
        if (combat == null || combat.team == team) return false;

        float distance = Vector3.Distance(transform.position, enemyTarget.position);

        if (distance > loseEnemyRange) return false;
        if (Time.time - fightStartTime > fightTimeout) return false;

        return true;
    }

    void LeaveFight()
    {
        enemyTarget = null;
        currentState = stateBeforeFight;
        hasDestination = false;
        currentTargetLevel = null;

        if (agent != null)
            agent.stoppingDistance = stoppingDistance;
    }

    void LeaveFightAndAdvance()
    {
        enemyTarget = null;
        currentState = BotState.Advance;
        hasDestination = false;
        currentTargetLevel = null;

        if (agent != null)
            agent.stoppingDistance = stoppingDistance;
    }

    void UpdateDefend()
    {
        if (treeTarget == null) return;

        if (!hasDestination)
        {
            FindProtectorPosition();
            SetNewDestination(protectorTarget);
        }
    }

    void UpdateAdvance()
    {
        Transform targetLevel = GetBotCurrentCombatTargetLevel();

        if (targetLevel == null)
        {
            if (!hasDestination)
                SetNewDestination(FindClosestTreePosition());
            return;
        }

        if (currentTargetLevel != targetLevel)
        {
            currentTargetLevel = targetLevel;
            Vector3 newTarget = FindPointOnLevel(targetLevel);
            SetNewDestination(newTarget);
            return;
        }

        if (!hasDestination)
        {
            Vector3 newTarget = FindPointOnLevel(targetLevel);
            SetNewDestination(newTarget);
        }
    }

    /// <summary>
    /// يحدد الطابق القتالي الذي يجب على البوتات التواجد فيه حالياً:
    /// 1. طالما الزعيم 1 حي، يبقى كل البوتات في المستوى 1 ولا يصعدون للأعلى أبداً.
    /// 2. عند موت الزعيم 1، يصعدون للمستوى 2 لقتال الزعيم 2.
    /// 3. عند موت الزعيم 2، يصعدون للمستوى 3 لقتال الزعيم 3.
    /// 4. عند موت الزعماء الثلاثة، يصعدون لقمة الشجرة (TopLevel) للكريستال وللعرش.
    /// </summary>
    Transform GetBotCurrentCombatTargetLevel()
    {
        // 1. إذا كان هناك زعيم حي ومستيقظ، فالطابق القتالي الإلزامي للبوتات هو طابق ذلك الزعيم تحديداً
        if (LevelBossManager.Instance != null && !LevelBossManager.Instance.AreAllBossesDefeated)
        {
            LevelBoss activeBoss = LevelBossManager.Instance.GetActiveAwakenedBoss();
            if (activeBoss != null)
            {
                int targetLvlIndex = activeBoss.levelNumber - 1; // Level 1 -> index 0, Level 2 -> index 1, Level 3 -> index 2
                if (levelTargets != null && targetLvlIndex >= 0 && targetLvlIndex < levelTargets.Length)
                {
                    Transform targetFloor = levelTargets[targetLvlIndex];
                    if (targetFloor != null && targetFloor.gameObject.activeInHierarchy)
                    {
                        return targetFloor;
                    }
                }
            }
        }

        // 2. إذا هُزم الزعماء الثلاثة جميعهم، يصبح الهدف هو قمة الشجرة (TopLevel)
        if (levelTargets != null && levelTargets.Length > 0)
        {
            for (int i = levelTargets.Length - 1; i >= 0; i--)
            {
                Transform level = levelTargets[i];
                if (level != null && level.gameObject.activeInHierarchy) return level;
            }
        }

        return null;
    }

    Transform GetHighestCombatLevel()
    {
        return GetBotCurrentCombatTargetLevel();
    }

    void SetNewDestination(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        currentDestination = destination;
        hasDestination = true;
        destinationSetTime = Time.time;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(currentDestination);
    }

    Vector3 FindPointOnLevel(Transform level)
    {
        if (level == null) return transform.position;

        Renderer renderer = level.GetComponent<Renderer>();
        Vector3 center = level.position;
        float radius = 3f;

        if (renderer != null)
        {
            radius = Mathf.Min(renderer.bounds.extents.x, renderer.bounds.extents.z) * 0.65f;
            center = renderer.bounds.center;
        }

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 random = Random.insideUnitCircle * radius;
            Vector3 candidate = new Vector3(center.x + random.x, center.y, center.z + random.y);

            if (treeTarget != null)
            {
                Vector3 horizontal = candidate - treeTarget.position;
                horizontal.y = 0f;

                if (horizontal.magnitude < treeAvoidRadius)
                    continue;
            }

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 3f, NavMesh.AllAreas))
                return hit.position;
        }

        NavMeshHit centerHit;
        if (NavMesh.SamplePosition(center, out centerHit, 5f, NavMesh.AllAreas))
            return centerHit.position;

        return transform.position;
    }

    void FindProtectorPosition()
    {
        if (treeTarget == null) return;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle;
            if (circle.sqrMagnitude < 0.01f) continue;

            circle.Normalize();
            float radius = Random.Range(minDefenseRadius, maxDefenseRadius);

            Vector3 candidate = treeTarget.position + new Vector3(circle.x, 0f, circle.y) * radius;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 4f, NavMesh.AllAreas))
            {
                protectorTarget = hit.position;
                return;
            }
        }

        protectorTarget = transform.position;
    }

    Vector3 FindClosestTreePosition()
    {
        if (treeTarget == null) return transform.position;

        Vector3 direction = transform.position - treeTarget.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;

        direction.Normalize();

        Vector3 desired = new Vector3(treeTarget.position.x, transform.position.y, treeTarget.position.z) + direction * 4.15f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(desired, out hit, 5f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    // =========================
    // Crystal AI Behaviors
    // =========================

    void UpdateDeliverCrystal()
    {
        CrystalAltar altar = FindClosestAltar();
        if (altar != null)
        {
            Vector3 altarCenter = altar.transform.position;
            float horizDist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(altarCenter.x, altarCenter.z)
            );

            // ذكاء حاسم للبوت الحامي: إذا وصل داخل دائرة المذبح (أقل من 2.2م من المركز):
            // يثبت أقدامه فوراً في قلب المذبح ويبقى صامداً طوال الـ 3 ثوانٍ حتى يتم التعميد والفوز!
            if (horizDist <= 2.2f)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }

                Transform closeThreat = FindClosestEnemy();
                if (closeThreat != null)
                {
                    Vector3 lookDir = closeThreat.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
                    }
                    if (CrystalItem.Instance != null && Vector3.Distance(transform.position, closeThreat.position) <= 4.5f)
                    {
                        CrystalItem.Instance.RequestShockwaveRpc();
                    }
                    CharacterCombat myCombat = GetComponent<CharacterCombat>();
                    if (myCombat != null && Vector3.Distance(transform.position, closeThreat.position) <= myCombat.attackRange)
                    {
                        myCombat.TryAIAttack(closeThreat);
                    }
                }
                return;
            }

            // إذا لم يصل بعد، يتحرك نحو المركز الدقيق للمذبح
            if (NavMesh.SamplePosition(altarCenter, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
            {
                altarCenter = navHit.position;
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.stoppingDistance = 0.1f;
                agent.speed = Mathf.Max(sprintSpeed + personalOffset, 10.5f); // High-speed crystal sprint
                agent.SetDestination(altarCenter);
            }
            currentDestination = altarCenter;
            hasDestination = true;
        }
        else if (treeTarget != null)
        {
            SetNewDestination(FindClosestTreePosition());
        }

        if (CrystalItem.Instance != null)
        {
            Transform enemy = FindClosestEnemy();
            if (enemy != null && Vector3.Distance(transform.position, enemy.position) <= 5f)
            {
                CrystalItem.Instance.RequestShockwaveRpc();
            }
        }
    }

    void UpdateEscortCarrier()
    {
        if (CrystalItem.Instance == null || !CrystalItem.Instance.isCarried.Value)
        {
            currentState = BotState.Advance;
            return;
        }

        GameObject carrier = CrystalItem.Instance.GetCarrierObject();
        if (carrier == null)
        {
            currentState = BotState.Advance;
            return;
        }

        // إذا كان الحامل يعمد الكريستالة داخل المذبح (عد 3 ثوانٍ)، يقف الحامي المساعد على بعد 3.8م كحارس محيطي لمهاجمة المعتدين
        if (CrystalAltar.IsAnyAltarChannelling)
        {
            Transform threat = FindClosestEnemy();
            if (threat != null && Vector3.Distance(transform.position, threat.position) <= 8f)
            {
                EnterFight(threat);
                return;
            }

            Vector3 guardPos = carrier.transform.position + transform.right * 3.8f;
            SetNewDestination(guardPos);
            return;
        }

        Vector3 escortPos = carrier.transform.position + carrier.transform.right * 3f;
        SetNewDestination(escortPos);
    }

    void UpdateHuntCarrier()
    {
        if (CrystalItem.Instance == null || !CrystalItem.Instance.isCarried.Value)
        {
            currentState = BotState.Advance;
            return;
        }

        GameObject carrier = CrystalItem.Instance.GetCarrierObject();
        if (carrier != null)
        {
            EnterFight(carrier.transform);
        }
        else
        {
            currentState = BotState.Advance;
        }
    }

    void UpdateGuardDroppedCrystal()
    {
        if (CrystalItem.Instance == null || !CrystalItem.Instance.isDropped.Value)
        {
            currentState = BotState.Advance;
            return;
        }

        Vector3 guardPos = CrystalItem.Instance.transform.position;
        if (Vector3.Distance(transform.position, guardPos) > 4f)
        {
            SetNewDestination(guardPos);
        }
    }

    CrystalAltar FindClosestAltar()
    {
        CrystalAltar[] altars = FindObjectsByType<CrystalAltar>(FindObjectsSortMode.None);
        CrystalAltar closest = null;
        float minDist = float.MaxValue;

        foreach (CrystalAltar altar in altars)
        {
            if (altar == null) continue;
            float dist = Vector3.Distance(transform.position, altar.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = altar;
            }
        }

        return closest;
    }

    void EnforceTreeTrunkAvoidance()
    {
        if (treeTarget == null) return;

        Vector3 diff = transform.position - treeTarget.position;
        diff.y = 0f;
        float dist = diff.magnitude;
        float minTrunkDist = 3.85f; // نصف قطر جذع الشجرة في العالم 3.0م + هامش أمان للبوت

        if (dist < minTrunkDist)
        {
            Vector3 pushDir = dist > 0.05f ? diff.normalized : Vector3.forward;
            Vector3 safePos = treeTarget.position + pushDir * minTrunkDist;
            safePos.y = transform.position.y;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(safePos);
            }
            else
            {
                transform.position = safePos;
            }
        }
    }
}