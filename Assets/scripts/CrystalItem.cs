using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class CrystalItem : NetworkBehaviour
{
    public static CrystalItem Instance;

    [Header("Network State")]
    public NetworkVariable<bool> isCarried = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ulong> carrierNetworkObjectId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDropped = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> resetTimer = new NetworkVariable<float>(
        25f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Settings")]
    public float pickupRadius = 3.2f;
    public float maxDroppedDuration = 25f;
    public float shockwaveRadius = 8f;
    public float shockwaveForce = 16f;
    public float shockwaveCooldown = 12f;
    public Vector3 carrierOffset = new Vector3(0f, 2.2f, -0.4f);

    [Header("Spawn Points")]
    // ثلاث نقاط (أو أكثر) على الأرض حيث يمكن أن تظهر الكريستالة
    // ضعها في Unity Editor ككائنات فارغة (Empty GameObjects) على مستوى الـ Plane
    public Transform[] groundSpawnPoints;

    [Header("References")]
    public Transform topSpawnPoint;
    public GameObject visualModel;
    public Light glowLight;
    public ParticleSystem shockwaveEffect;
    public ParticleSystem droppedAuraEffect;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public AudioClip shockwaveSound;
    public AudioClip resetSound;

    private float nextShockwaveTime = 0f;
    private GameObject currentCarrierObj;

    void Awake()
    {
        Instance = this;
        SetupVisualCrystalGem();
    }

    private bool hasSpawnedOnField = false;
    private bool IsServerOrAuthority => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer;

    void Start()
    {
        SetupVisualCrystalGem();
        FindTopSpawnPoint();
        FindGroundSpawnPoints();

        // إخفاء الكريستالة فوراً عند بدء المشهد حتى يموت الزعماء الثلاثة جميعهم
        hasSpawnedOnField = false;
        ApplyVisualActiveLocal(false);
    }

    public override void OnNetworkSpawn()
    {
        TreeGrowth.OnTreeReachedTopLevel += OnTreeReachedTopLevel;

        isCarried.OnValueChanged += OnCarriedChanged;
        isDropped.OnValueChanged += OnDroppedChanged;

        FindTopSpawnPoint();
        FindGroundSpawnPoints();

        if (IsServerOrAuthority)
        {
            bool allBossesSlain = LevelBossManager.Instance != null && LevelBossManager.Instance.AreAllBossesDefeated;
            if (allBossesSlain)
            {
                SpawnAtTopServer();
            }
            else
            {
                hasSpawnedOnField = false;
                ApplyVisualActiveLocal(false);
                if (IsSpawned) SetVisualActiveRpc(false);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        TreeGrowth.OnTreeReachedTopLevel -= OnTreeReachedTopLevel;
        isCarried.OnValueChanged -= OnCarriedChanged;
        isDropped.OnValueChanged -= OnDroppedChanged;
    }

    void OnTreeReachedTopLevel()
    {
        if (IsServerOrAuthority)
        {
            if (LevelBossManager.Instance != null && LevelBossManager.Instance.AreAllBossesDefeated)
            {
                SpawnAtTopServer();
            }
        }
    }

    // =========================
    // Finding Spawn Points
    // =========================

    void FindTopSpawnPoint()
    {
        if (topSpawnPoint == null)
        {
            GameObject cradle = GameObject.Find("CrystalSanctumCradle");
            if (cradle != null)
            {
                topSpawnPoint = cradle.transform;
                return;
            }

            GameObject top = GameObject.Find("TopLevel");
            if (top != null)
                topSpawnPoint = top.transform;
            else
            {
                GameObject treeLevels = GameObject.Find("TreeLevels");
                if (treeLevels != null)
                {
                    Transform t = treeLevels.transform.Find("TopLevel");
                    if (t != null) topSpawnPoint = t;
                }
            }
        }
    }

    // البحث عن نقاط الظهور الأرضية تلقائياً من كائن "CrystalGroundSpawnPoints" في المشهد
    void FindGroundSpawnPoints()
    {
        if (groundSpawnPoints != null && groundSpawnPoints.Length > 0) return;

        GameObject holder = GameObject.Find("CrystalGroundSpawnPoints");
        if (holder != null)
        {
            Transform[] children = holder.GetComponentsInChildren<Transform>();
            var list = new System.Collections.Generic.List<Transform>(children);
            if (list.Count > 0) list.RemoveAt(0); // remove the parent
            groundSpawnPoints = list.ToArray();
        }
    }

    // الحصول على موضع صالح على الـ NavMesh والأرض لضمان وصول البوتات واللاعبين للكريستالة دائماً
    Vector3 GetValidGroundNavMeshPosition(Vector3 candidate)
    {
        RaycastHit hit;
        if (Physics.Raycast(candidate + Vector3.up * 5f, Vector3.down, out hit, 25f))
        {
            candidate = hit.point + Vector3.up * 0.25f;
        }

        if (UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit navHit, 8f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return navHit.position + Vector3.up * 0.25f;
        }

        return candidate;
    }

    // =========================
    // Spawning
    // =========================

    [ContextMenu("Spawn At Top (Server)")]
    public void SpawnAtTopServer()
    {
        if (!IsServerOrAuthority) return;

        // لا تظهر الكريستالة إلا بعد هزيمة الزعماء الثلاثة جميعاً وبشكل مؤكد!
        if (LevelBossManager.Instance == null || !LevelBossManager.Instance.AreAllBossesDefeated)
        {
            hasSpawnedOnField = false;
            ApplyVisualActiveLocal(false);
            if (IsSpawned) SetVisualActiveRpc(false);
            return;
        }

        FindTopSpawnPoint();
        FindGroundSpawnPoints();

        Vector3 spawnPos = Vector3.zero;
        bool foundPoint = false;

        // 1. الأولوية المطلقة: الظهور في القمة داخل مهد الوردة المتفتحة طافية في الهواء دون أي تداخل
        if (topSpawnPoint != null)
        {
            spawnPos = topSpawnPoint.position + Vector3.up * 1.6f;
            foundPoint = true;
        }
        else if (groundSpawnPoints != null && groundSpawnPoints.Length > 0)
        {
            int idx = Random.Range(0, groundSpawnPoints.Length);
            if (groundSpawnPoints[idx] != null)
            {
                spawnPos = groundSpawnPoints[idx].position + Vector3.up * 1.2f;
                foundPoint = true;
            }
        }

        // 2. إذا لم تكن هناك نقطة في القمة ولا أرضية، نضعها قرب الشجرة
        if (!foundPoint)
        {
            GameObject treeObj = GameObject.FindGameObjectWithTag("Tree");
            Vector3 center = treeObj != null ? new Vector3(treeObj.transform.position.x, 0.5f, treeObj.transform.position.z) : new Vector3(0f, 0.5f, 0f);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * 7.5f, 0.5f, Mathf.Sin(angle) * 7.5f);
            spawnPos = GetValidGroundNavMeshPosition(candidate);
        }

        transform.position = spawnPos;
        transform.SetParent(null);

        hasSpawnedOnField = true;
        isCarried.Value = false;
        carrierNetworkObjectId.Value = 0;
        currentCarrierObj = null;
        isDropped.Value = false;
        resetTimer.Value = maxDroppedDuration;

        ApplyVisualActiveLocal(true);
        if (IsSpawned)
        {
            SetVisualActiveRpc(true);
            PlayResetFxRpc(spawnPos);
        }

        Debug.Log("💎 Sacred Crystal unsealed & spawned on NavMesh at position: " + spawnPos);
    }

    private void ApplyVisualActiveLocal(bool active)
    {
        if (visualModel != null) visualModel.SetActive(active);
        if (glowLight != null) glowLight.enabled = active;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r != null) r.enabled = active;
        }
    }

    // =========================
    // Update Loop
    // =========================

    void Update()
    {
        if (IsServerOrAuthority)
        {
            UpdateServerLogic();
        }

        if (isCarried.Value && currentCarrierObj != null)
        {
            transform.position = currentCarrierObj.transform.position +
                currentCarrierObj.transform.TransformDirection(carrierOffset);
            transform.rotation = currentCarrierObj.transform.rotation;
        }

        if (isCarried.Value && IsLocalPlayerCarrier())
        {
            HandleCarrierInput();
        }

        // طوفان سحري مهيب ودوران للكريستالة في الهواء لمنع أي التصاق أو تداخل مع المنصة
        if (!isCarried.Value && visualModel != null && visualModel.activeInHierarchy)
        {
            visualModel.transform.Rotate(0f, 32f * Time.deltaTime, 0f, Space.World);
            visualModel.transform.localPosition = new Vector3(0f, 0.35f + Mathf.Sin(Time.time * 2.2f) * 0.14f, 0f);
        }
    }

    public bool IsAvailableForPickup =>
        !isCarried.Value &&
        (LevelBossManager.Instance != null && LevelBossManager.Instance.AreAllBossesDefeated) &&
        (hasSpawnedOnField || isDropped.Value);

    void UpdateServerLogic()
    {
        // ضمان تلقائي: بمجرد موت الزعماء الثلاثة، إذا لم تكن الكريستالة قد ظهرت بعد على الأرض، نظهرها فوراً!
        if (!hasSpawnedOnField && !isCarried.Value && !isDropped.Value &&
            LevelBossManager.Instance != null && LevelBossManager.Instance.AreAllBossesDefeated)
        {
            SpawnAtTopServer();
        }

        if (isCarried.Value)
        {
            if (currentCarrierObj == null) ResolveCarrierObject();
            if (currentCarrierObj == null)
            {
                DropCrystalServer();
                return;
            }

            Health carrierHealth = currentCarrierObj.GetComponent<Health>();
            if (carrierHealth != null && carrierHealth.isDead)
            {
                DropCrystalServer();
                return;
            }
        }

        if (isDropped.Value)
        {
            resetTimer.Value -= Time.deltaTime;

            if (resetTimer.Value <= 0f)
            {
                Debug.Log("Crystal dropped timeout elapsed. Resetting!");
                SpawnAtTopServer();
                return;
            }
        }

        if (!isCarried.Value && IsAvailableForPickup)
        {
            CheckPickupOverlapServer();
        }
    }

    // =========================
    // Pickup & Drop
    // =========================

    void CheckPickupOverlapServer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius);

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            PlayerMovement pm = hit.GetComponentInParent<PlayerMovement>();
            if (pm != null && pm.team == PlayerMovement.TeamType.Protector)
            {
                Health health = pm.GetComponent<Health>();
                if (health != null && health.isDead) continue;

                TryPickupServer(pm.gameObject);
                break;
            }

            BotController bot = hit.GetComponentInParent<BotController>();
            if (bot != null && bot.team == PlayerMovement.TeamType.Protector)
            {
                Health health = bot.GetComponent<Health>();
                if (health != null && health.isDead) continue;

                TryPickupServer(bot.gameObject);
                break;
            }
        }
    }

    public void TryPickupServer(GameObject protectorObj)
    {
        if (!IsServerOrAuthority || isCarried.Value || protectorObj == null || !IsAvailableForPickup) return;

        NetworkObject netObj = protectorObj.GetComponentInParent<NetworkObject>();
        GameObject rootObj = netObj != null ? netObj.gameObject : protectorObj;

        Health health = rootObj.GetComponent<Health>();
        if (health != null && health.isDead) return;

        PlayerMovement pm = rootObj.GetComponent<PlayerMovement>();
        BotController bot = rootObj.GetComponent<BotController>();

        bool isProtectorPlayer = (pm != null && pm.team == PlayerMovement.TeamType.Protector);
        bool isProtectorBot = (bot != null && bot.team == PlayerMovement.TeamType.Protector);

        if (!isProtectorPlayer && !isProtectorBot) return;

        isCarried.Value = true;
        carrierNetworkObjectId.Value = netObj != null ? netObj.NetworkObjectId : 0;
        isDropped.Value = false;
        resetTimer.Value = maxDroppedDuration;

        if (pm != null)
        {
            pm.isCarryingCrystal.Value = true;
        }

        currentCarrierObj = rootObj;

        ApplyVisualActiveLocal(true);
        if (IsSpawned) PlaySoundRpc(0);
        Debug.Log("💎 Crystal picked up by Protector: " + rootObj.name);
    }

    public void DropCrystalServer()
    {
        if (!IsServerOrAuthority || !isCarried.Value) return;

        if (currentCarrierObj != null)
        {
            PlayerMovement pm = currentCarrierObj.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.isCarryingCrystal.Value = false;
        }

        Vector3 dropPos = GetValidGroundNavMeshPosition(transform.position);

        transform.position = dropPos;
        transform.SetParent(null);

        isCarried.Value = false;
        carrierNetworkObjectId.Value = 0;
        currentCarrierObj = null;

        isDropped.Value = true;
        resetTimer.Value = maxDroppedDuration;

        ApplyVisualActiveLocal(true);
        if (IsSpawned)
        {
            SetVisualActiveRpc(true);
            PlaySoundRpc(1);
        }
        Debug.Log("💎 Crystal dropped on the ground at: " + dropPos);
    }

    // =========================
    // Carrier Input
    // =========================

    void HandleCarrierInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TriggerCarrierShockwave();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            TriggerCarrierDrop();
        }
    }

    public void TriggerCarrierShockwave()
    {
        if (Time.time >= nextShockwaveTime)
        {
            nextShockwaveTime = Time.time + shockwaveCooldown;
            RequestShockwaveRpc();
        }
    }

    public void TriggerCarrierDrop()
    {
        RequestDropRpc();
    }

    [Rpc(SendTo.Server)]
    public void RequestDropRpc()
    {
        DropCrystalServer();
    }

    [Rpc(SendTo.Server)]
    public void RequestShockwaveRpc()
    {
        if (!isCarried.Value || currentCarrierObj == null) return;

        Vector3 blastOrigin = transform.position;

        Collider[] hits = Physics.OverlapSphere(blastOrigin, shockwaveRadius);
        foreach (Collider col in hits)
        {
            if (col.gameObject == currentCarrierObj) continue;

            CharacterCombat combat = col.GetComponent<CharacterCombat>();
            if (combat != null && combat.team == PlayerMovement.TeamType.Destroyer)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 pushDir = (col.transform.position - blastOrigin).normalized;
                    pushDir.y = 0.3f;
                    rb.AddForce(pushDir * shockwaveForce, ForceMode.Impulse);
                }

                UnityEngine.AI.NavMeshAgent agent = col.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    Vector3 pushDir = (col.transform.position - blastOrigin).normalized;
                    agent.Move(pushDir * 3.5f);
                }
            }
        }

        PlayShockwaveFxRpc(blastOrigin);
    }

    // =========================
    // Callbacks & Visuals
    // =========================

    void OnCarriedChanged(bool previous, bool current)
    {
        if (current)
        {
            ResolveCarrierObject();
            if (droppedAuraEffect != null) droppedAuraEffect.Stop();
        }
        else
        {
            currentCarrierObj = null;
        }
    }

    void OnDroppedChanged(bool previous, bool current)
    {
        if (current)
        {
            if (droppedAuraEffect != null) droppedAuraEffect.Play();
        }
        else
        {
            if (droppedAuraEffect != null) droppedAuraEffect.Stop();
        }
    }

    void ResolveCarrierObject()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                carrierNetworkObjectId.Value, out NetworkObject netObj))
        {
            currentCarrierObj = netObj.gameObject;
        }
    }

    public bool IsCarrier(GameObject obj)
    {
        if (!isCarried.Value || obj == null) return false;
        if (currentCarrierObj == obj) return true;
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        return netObj != null && carrierNetworkObjectId.Value != 0 && netObj.NetworkObjectId == carrierNetworkObjectId.Value;
    }

    public GameObject GetCarrierObject()
    {
        if (!isCarried.Value) return null;
        if (currentCarrierObj == null) ResolveCarrierObject();
        return currentCarrierObj;
    }

    public bool IsLocalPlayerCarrier()
    {
        if (!isCarried.Value || NetworkManager.Singleton == null) return false;
        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        return localPlayer != null && localPlayer.NetworkObjectId == carrierNetworkObjectId.Value;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SetVisualActiveRpc(bool active)
    {
        if (visualModel != null) visualModel.SetActive(active);
        if (glowLight != null) glowLight.enabled = active;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayShockwaveFxRpc(Vector3 position)
    {
        if (shockwaveEffect != null)
        {
            shockwaveEffect.transform.position = position;
            shockwaveEffect.Play();
        }

        if (audioSource != null && shockwaveSound != null)
            audioSource.PlayOneShot(shockwaveSound);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayResetFxRpc(Vector3 position)
    {
        if (audioSource != null && resetSound != null)
            audioSource.PlayOneShot(resetSound);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlaySoundRpc(int soundIndex)
    {
        if (audioSource == null) return;

        if (soundIndex == 0 && pickupSound != null)
            audioSource.PlayOneShot(pickupSound);
        else if (soundIndex == 1 && dropSound != null)
            audioSource.PlayOneShot(dropSound);
    }

    /// <summary>
    /// توليد مجسم الكريستالة السحرية المشحوذة (Faceted Gemstone Mesh)
    /// لاستبدال شكل البيضة البيضاوية السابقة بكريستالة أسطورية حقيقية متعددة الأوجه
    /// </summary>
    public static Mesh GenerateFacetedCrystalMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "FacetedSacredGem";

        int sides = 8;
        float heightApex = 1.25f;
        float heightUpper = 0.55f;
        float heightLower = -0.55f;
        float radiusUpper = 0.55f;
        float radiusWaist = 0.85f;
        float radiusLower = 0.55f;

        Vector3 topApex = new Vector3(0f, heightApex, 0f);
        Vector3 bottomApex = new Vector3(0f, -heightApex, 0f);

        Vector3[] upperRing = new Vector3[sides];
        Vector3[] waistRing = new Vector3[sides];
        Vector3[] lowerRing = new Vector3[sides];

        for (int i = 0; i < sides; i++)
        {
            float angle = (i / (float)sides) * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            upperRing[i] = new Vector3(cos * radiusUpper, heightUpper, sin * radiusUpper);
            waistRing[i] = new Vector3(cos * radiusWaist, 0f, sin * radiusWaist);
            lowerRing[i] = new Vector3(cos * radiusLower, heightLower, sin * radiusLower);
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            int startIndex = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);

            Vector3 norm = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            normals.Add(norm);
            normals.Add(norm);
            normals.Add(norm);

            uvs.Add(new Vector2(0.5f, 1f));
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));

            triangles.Add(startIndex);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);
        }

        void AddQuad(Vector3 v00, Vector3 v10, Vector3 v11, Vector3 v01)
        {
            AddTriangle(v00, v10, v11);
            AddTriangle(v00, v11, v01);
        }

        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;

            // 1. القمة العلوية
            AddTriangle(topApex, upperRing[next], upperRing[i]);

            // 2. أوجه الحزام العلوي
            AddQuad(upperRing[i], upperRing[next], waistRing[next], waistRing[i]);

            // 3. أوجه الحزام السفلي
            AddQuad(waistRing[i], waistRing[next], lowerRing[next], lowerRing[i]);

            // 4. القمة السفلية
            AddTriangle(bottomApex, lowerRing[i], lowerRing[next]);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();

        return mesh;
    }

    void SetupVisualCrystalGem()
    {
        if (visualModel == null)
        {
            Transform vTr = transform.Find("CrystalVisual");
            if (vTr != null) visualModel = vTr.gameObject;
        }

        if (visualModel != null)
        {
            MeshFilter mf = visualModel.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = GenerateFacetedCrystalMesh();
            }

            visualModel.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

            MeshRenderer mr = visualModel.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.88f, 0.98f, 0.95f);
                mat.SetFloat("_Smoothness", 0.95f);
                mat.SetFloat("_Metallic", 0.05f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.15f, 0.9f, 1.0f) * 2.2f);
                mr.material = mat;
            }

            Collider col = visualModel.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
    }
}
