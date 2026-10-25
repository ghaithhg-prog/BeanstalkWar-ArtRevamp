using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    private bool jumpButtonPressed = false;
    private int jumpCount = 0;
    private int maxJumps = 1;

    public enum TeamType
    {
        Protector,
        Destroyer,
        Boss
    }

    public static PlayerMovement LocalPlayer { get; private set; }
    public static bool MobileSprintActive { get; set; } = false;
    public static bool SprintToggleActive { get; set; } = false;

    [Header("Team")]
    public TeamType team;

    public NetworkVariable<bool> hasSelectedTeam =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<TeamType> networkTeam =
        new NetworkVariable<TeamType>(
            TeamType.Destroyer,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    [Header("Character Selection")]
    public CharacterDatabase characterDatabase;

    public NetworkVariable<bool> hasSelectedCharacter =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> selectedCharacterID =
        new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    [Header("Crystal Carry")]
    public NetworkVariable<bool> isCarryingCrystal =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public float crystalSpeedMultiplier = 1.35f;

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    public float climbSpeedMultiplier = 1.4f;

    private bool isOnClimbPath = false;

    public SimpleJoystick joystick;
    public float moveSpeed = 8f;
    public float jumpForce = 7f;

    private Rigidbody rb;

    // =========================
    // Network
    // =========================

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalPlayer = this;
            gameObject.name = PlayerProfile.GetSavedPlayerName();
        }

        networkTeam.OnValueChanged += OnTeamChanged;
        selectedCharacterID.OnValueChanged += OnCharacterChanged;

        if (hasSelectedTeam.Value)
            OnTeamChanged(networkTeam.Value, networkTeam.Value);

        if (hasSelectedCharacter.Value &&
            selectedCharacterID.Value >= 0)
        {
            ApplyCharacter(selectedCharacterID.Value);
        }

        if (IsOwner)
        {
            // وضع اللاعب على موضع وارتفاع آمن فوق الأرضية تماماً
            transform.position = new Vector3(-8f, 1.05f, 0f);
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (LocalPlayer == this)
        {
            LocalPlayer = null;
        }

        networkTeam.OnValueChanged -= OnTeamChanged;
        selectedCharacterID.OnValueChanged -= OnCharacterChanged;
    }

    // =========================
    // Team
    // =========================

    void OnTeamChanged(
        TeamType previousTeam,
        TeamType newTeam)
    {
        team = newTeam;

        CharacterCombat combat =
            GetComponent<CharacterCombat>();

        if (combat != null)
            combat.team = newTeam;

        ApplyTeamVisualAura();
    }

    public void ApplyTeamVisualAura()
    {
        ApplyTeamVisualAuraTo(gameObject, team);
    }

    public static void ApplyTeamVisualAuraTo(GameObject target, TeamType team)
    {
        if (target == null || team == TeamType.Boss) return;

        Color teamColor = (team == TeamType.Protector) 
            ? new Color(0.2f, 0.75f, 1f) 
            : new Color(1f, 0.25f, 0.1f);

        // 1. ضوء الهالة المحيطة بالفريق (توهج خفيف وهادئ حتى لا يطغى على الألوان الطبيعية للشخصية)
        Transform existingLight = target.transform.Find("TeamAuraLight");
        Light tLight;
        if (existingLight == null)
        {
            GameObject lightObj = new GameObject("TeamAuraLight");
            lightObj.transform.SetParent(target.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            tLight = lightObj.AddComponent<Light>();
            tLight.type = LightType.Point;
        }
        else
        {
            tLight = existingLight.GetComponent<Light>();
        }

        if (tLight != null)
        {
            tLight.color = teamColor;
            tLight.range = 2.2f;
            tLight.intensity = 0.15f; // توهج خفيف جداً يحافظ على اللون الطبيعي للأرتشر
        }

        // 2. هالة القدمين المضيئة الدائرية (Foot Aura Ring) تبقى ثابتة على الأرض عند القفز
        Transform existingAura = target.transform.Find("TeamFootAura");
        GameObject auraObj;
        if (existingAura == null)
        {
            auraObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            auraObj.name = "TeamFootAura";
            auraObj.transform.SetParent(target.transform, false);
            Collider col = auraObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
        else
        {
            auraObj = existingAura.gameObject;
        }

        auraObj.transform.localPosition = new Vector3(0f, -0.92f, 0f);
        auraObj.transform.localScale = new Vector3(1.35f, 0.010f, 1.35f);

        if (auraObj.GetComponent<GroundLockedFootAura>() == null)
        {
            auraObj.AddComponent<GroundLockedFootAura>();
        }

        Renderer ar = auraObj.GetComponent<Renderer>();
        if (ar != null)
        {
            Color auraCol = new Color(teamColor.r, teamColor.g, teamColor.b, 0.70f);
            ar.material.color = auraCol;
            if (ar.material.HasProperty("_EmissionColor"))
            {
                ar.material.EnableKeyword("_EMISSION");
                ar.material.SetColor("_EmissionColor", teamColor * 0.45f);
            }
        }
    }

    public void RequestTeam(TeamType requestedTeam)
    {
        if (!IsOwner)
            return;

        RequestTeamRpc(requestedTeam);
    }

    public void ResetTeamSelection()
    {
        if (IsServer)
        {
            hasSelectedTeam.Value = false;
            hasSelectedCharacter.Value = false;
            selectedCharacterID.Value = -1;
        }
        else if (IsOwner)
        {
            ResetTeamSelectionRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void ResetTeamSelectionRpc()
    {
        hasSelectedTeam.Value = false;
        hasSelectedCharacter.Value = false;
        selectedCharacterID.Value = -1;
    }

    [Rpc(SendTo.Server)]
    private void RequestTeamRpc(TeamType requestedTeam)
    {
        if (hasSelectedTeam.Value)
            return;

        networkTeam.Value = requestedTeam;
        hasSelectedTeam.Value = true;

        team = requestedTeam;

        CharacterCombat combat =
            GetComponent<CharacterCombat>();

        if (combat != null)
            combat.team = requestedTeam;

        if (SpawnManager.Instance != null)
        {
            Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(requestedTeam, 0);
            transform.position = spawnPos + Vector3.up * 0.5f;
            Transform spawnPoint = SpawnManager.Instance.GetSpawnPoint(requestedTeam);
            if (spawnPoint != null) transform.rotation = spawnPoint.rotation;
        }

        Debug.Log(
            "Player " + OwnerClientId +
            " selected team " + requestedTeam
        );
    }

    // =========================
    // Character Selection
    // =========================

    public void RequestCharacter(int characterID)
    {
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (isNetworkActive)
        {
            if (!IsOwner)
                return;

            if (!hasSelectedTeam.Value)
            {
                RequestTeam(TeamType.Protector);
            }

            RequestCharacterRpc(characterID);
        }

        // Apply immediately locally so visuals, 3D model, and stats are responsive
        ApplyCharacter(characterID);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void StartGameCountdownRpc(float duration)
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartCountdownFromNetwork(duration);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestCharacterRpc(int characterID)
    {
        if (hasSelectedCharacter.Value)
            return;

        if (characterDatabase == null)
        {
            Debug.LogError(
                "CharacterDatabase is not assigned to Player."
            );
            return;
        }

        CharacterData requestedCharacter =
            characterDatabase.GetCharacter(characterID);

        if (requestedCharacter == null)
            return;

        // نحسب كم لاعباً من نفس الفريق اختار هذه الشخصية
        int currentCount = 0;

        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

        foreach (PlayerMovement player in players)
        {
            if (!player.IsSpawned)
                continue;

            if (!player.hasSelectedTeam.Value ||
                !player.hasSelectedCharacter.Value)
                continue;

            if (player.networkTeam.Value != networkTeam.Value)
                continue;

            if (player.selectedCharacterID.Value == characterID)
                currentCount++;
        }

        // الشخصية ممتلئة
        if (currentCount >= requestedCharacter.maxPerTeam)
        {
            Debug.Log(
                requestedCharacter.characterName +
                " reached max players for this team."
            );

            return;
        }

        selectedCharacterID.Value = characterID;
        hasSelectedCharacter.Value = true;

        ApplyCharacter(characterID);

        Debug.Log(
            "Player " + OwnerClientId +
            " selected character " +
            requestedCharacter.characterName
        );
    }

    void OnCharacterChanged(
        int previousID,
        int newID)
    {
        if (newID >= 0)
            ApplyCharacter(newID);
    }

    public void ApplyCharacter(int characterID)
    {
        // حماية هامة: منع مسح أو تخفيض صحة وبيانات الزعماء إطلاقاً
        if (GetComponent<LevelBoss>() != null)
            return;

        if (characterDatabase == null)
            return;

        CharacterData data =
            characterDatabase.GetCharacter(characterID);

        if (data == null)
            return;

        CharacterCombat combat =
            GetComponent<CharacterCombat>();

        Health health =
            GetComponent<Health>();

        if (combat != null)
        {
            combat.characterData = data;
            combat.attackDamage = data.attackDamage;
            combat.attackCooldown = data.attackCooldown;
            combat.attackRange = data.attackRange;
            combat.attackType = data.attackType;
            combat.team = team;
        }

        walkSpeed = data.moveSpeed;
        runSpeed = Mathf.Max(data.moveSpeed * 1.65f, 10f);
        moveSpeed = data.moveSpeed;
        jumpForce = Mathf.Max(data.jumpForce, 7.5f);
        maxJumps = 2;

        if (health != null)
        {
            health.maxHealth = data.maxHealth;
            health.currentHealth = data.maxHealth;
        }

        // تطبيق السكن المختار ونقاط القوة الإضافية
        int selectedSkinIndex = 0;
        if (PlayerProfile.Instance != null)
        {
            selectedSkinIndex = PlayerProfile.Instance.GetSelectedSkin(characterID);
        }

        CharacterSkin[] skins = CharacterSkinDatabase.GetSkinsForCharacter(characterID);
        if (selectedSkinIndex >= 0 && selectedSkinIndex < skins.Length)
        {
            CharacterSkin skin = skins[selectedSkinIndex];
            if (combat != null) combat.attackDamage *= skin.damageMultiplier;
            if (health != null)
            {
                health.maxHealth *= skin.healthMultiplier;
                health.currentHealth = health.maxHealth;
            }
            walkSpeed *= skin.speedMultiplier;
            runSpeed *= skin.speedMultiplier;
            moveSpeed *= skin.speedMultiplier;

            ApplySkinVisuals(skin);
        }

        // نطبق مجسم الـ 3D الحقيقي المصنوع في Blender على جميع الأبطال مع تخصيص الدروع بحسب صنف البطل
        if (data != null)
        {
            AttachCharacter3DModel(data, characterID);
        }
    }

    public void AttachCharacter3DModel(CharacterData data, int characterID)
    {
        Attach3DModel(gameObject, data, characterID, team);
    }

    public static GameObject Attach3DModel(GameObject target, CharacterData data, int characterID, TeamType team, CharacterSkin skin = null, float customScale = 1.05f)
    {
        if (target == null) return null;

        Transform existing = target.transform.Find("Character_3D_Mesh");
        if (existing != null)
        {
            existing.name = "Character_3D_Mesh_Old";
            existing.gameObject.SetActive(false);
            Destroy(existing.gameObject);
        }

        // Model Selection: Erika Archer for Archer, and Warrior_Medieval for other classes
        GameObject prefab = null;
        bool isArcher = (data != null && data.characterName.Equals("Archer", System.StringComparison.OrdinalIgnoreCase))
                        || (data == null && target.name.ToLower().Contains("archer"));

        if (isArcher)
        {
            prefab = Resources.Load<GameObject>("Models/Erika_Archer");
            if (prefab == null) prefab = Resources.Load<GameObject>("Models/Akai");
        }
        else
        {
            prefab = Resources.Load<GameObject>("Models/Warrior_Medieval");
        }

        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Models/Warrior_Medieval");
            if (prefab == null) prefab = Resources.Load<GameObject>("Models/Erika_Archer");
            if (prefab == null) prefab = Resources.Load<GameObject>("Models/Akai");
        }
        if (prefab == null) return null;

        // 1. إخفاء كبسولة اللاعب أو البوت البدائية الرئيسية
        MeshRenderer baseRend = target.GetComponent<MeshRenderer>();
        if (baseRend != null) baseRend.enabled = false;

        // 2. إخفاء أي مجسمات بدائية للأطفال (Capsule / Cylinder / Sphere / Hat / Ring / Visual/Model)
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
        {
            if (r.transform != target.transform && 
                !r.name.Contains("Warrior") && 
                !r.name.Contains("Erika") &&
                !r.name.Contains("Akai") &&
                !r.name.Contains("Character_3D") && 
                !r.name.Contains("TeamFootAura") &&
                !r.name.Contains("BossDecorations") &&
                !r.name.Contains("BossAuraCrown"))
            {
                r.enabled = false;
            }
        }

        // 3. إخفاء مجلد Visual إذا كان موجوداً بالكامل
        Transform visualTr = target.transform.Find("Visual");
        if (visualTr != null)
        {
            visualTr.gameObject.SetActive(false);
        }

        // 4. ضبط قاعدة الملاحة للبوتات والزعماء حتى لا ينغرس نصفهم في الأرض
        UnityEngine.AI.NavMeshAgent agent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.baseOffset = 0.95f;
        }

        // 5. توليد المجسم ثلاثي الأبعاد الحقيقي
        GameObject model = Instantiate(prefab, target.transform);
        model.name = "Character_3D_Mesh";
        model.transform.localPosition = new Vector3(0f, -0.95f, 0f); // وضع الأقدام على الأرض تماماً
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * customScale;

        // 5. إزالة الكوليدرات والريجيدبودي الإضافية من داخل المجسم
        foreach (Collider col in model.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }
        foreach (Rigidbody childRb in model.GetComponentsInChildren<Rigidbody>())
        {
            Destroy(childRb);
        }
        foreach (Animator anim in model.GetComponentsInChildren<Animator>())
        {
            anim.applyRootMotion = false;
            if (isArcher)
            {
                anim.enabled = true;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            else
            {
                anim.enabled = false; // تعطيل المحرك لبقية الموديلات التي تعتمد على الحركة الإجرائية
            }
        }

        // 6. تطبيق الخامات والألوان (Erika Archer لها خامات أصلية طبيعية، وبقية الشخصيات تلون حسب الرتبة)
        if (isArcher || prefab.name.Contains("Erika") || prefab.name.Contains("Akai"))
        {
            ApplyErikaArcherMaterials(model, team);
        }
        else if (data != null)
        {
            StyleModelForCharacter(model, data, skin);
        }

        // 7. تفعيل محرك الحركة والأنيميشن الحيوي لمنع أي تجمد كالصنم
        CharacterVisualAnimator visualAnim = model.GetComponent<CharacterVisualAnimator>();
        if (visualAnim == null)
        {
            visualAnim = model.AddComponent<CharacterVisualAnimator>();
        }

        // 8. ربط أو إنشاء القوس في يد رامي السهام (Left Hand)
        if (isArcher)
        {
            Transform bowTr = null;
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Contains("Bow") || t.name.Contains("bow"))
                {
                    bowTr = t;
                    break;
                }
            }

            // إذا لم يكن القوس موجوداً كمجسم مسبق، نثبته في يد الشخصية اليسرى فوراً
            if (bowTr == null)
            {
                Transform leftHand = null;
                foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.ToLower().Contains("lefthand"))
                    {
                        leftHand = t;
                        break;
                    }
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
                        bowTr = bowInstance.transform;
                    }
                }
            }

            CharacterCombat combat = target.GetComponent<CharacterCombat>();
            if (combat != null && bowTr != null)
            {
                combat.firePoint = bowTr;
            }
        }

        // 9. تطبيق هالة الفريق للقدمين والضوء
        ApplyTeamVisualAuraTo(target, team);

        Debug.Log($"⚔️ Attached animated 3D model ({prefab.name}) for {target.name} | Champion: {data?.characterName} (ID: {characterID}) Team: {team}");
        return model;
    }

    public static void ApplyErikaArcherMaterials(GameObject model, TeamType team = TeamType.Protector)
    {
        if (model == null) return;

        Texture2D texClothes = Resources.Load<Texture2D>("Models/ErikaTextures/Erika_Archer_Clothes_diffuse");
        Texture2D texClothesNM = Resources.Load<Texture2D>("Models/ErikaTextures/Erika_Archer_Clothes_normal");
        Texture2D texBody = Resources.Load<Texture2D>("Models/ErikaTextures/FemaleFitA_Body_diffuse");
        Texture2D texBodyNM = Resources.Load<Texture2D>("Models/ErikaTextures/FemaleFitA_StdNM");
        Texture2D texBow = Resources.Load<Texture2D>("Models/ErikaTextures/Bow_DIFF");
        Texture2D texBowNM = Resources.Load<Texture2D>("Models/ErikaTextures/Bow_NM");
        Texture2D texLash = Resources.Load<Texture2D>("Models/ErikaTextures/FemaleFitA_eyelash_diffuse");

        Color subtleGlowColor = (team == TeamType.Destroyer)
            ? new Color(1.0f, 0.25f, 0.15f)
            : new Color(0.15f, 0.65f, 1.0f);

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            string rName = r.name.ToLower();

            Texture2D diff = texClothes;
            Texture2D norm = texClothesNM;
            bool isClothes = false;

            if (rName.Contains("bow"))
            {
                diff = texBow;
                norm = texBowNM;
            }
            else if (rName.Contains("lash"))
            {
                diff = texLash != null ? texLash : texBody;
                norm = null;
            }
            else if (rName.Contains("body") || rName.Contains("eye"))
            {
                diff = texBody;
                norm = texBodyNM;
            }
            else if (rName.Contains("clothes"))
            {
                diff = texClothes;
                norm = texClothesNM;
                isClothes = true;
            }

            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null) continue;

                // الحفاظ على الألوان الطبيعية الأصلية 100% دون أي صبغة زرقاء
                mat.color = Color.white;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

                if (diff != null)
                {
                    mat.mainTexture = diff;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diff);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diff);
                }

                if (norm != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", norm);
                }

                // توهج خفيف جداً وأنيق على الملابس فقط بينما البشرة والوجه والقوس بلونهم الطبيعي تماماً
                if (isClothes)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", subtleGlowColor * 0.05f);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", Color.black);
                }
            }
            r.materials = mats;
        }
    }

    public static void StyleModelForBoss(GameObject model, Color bossColor, Color auraColor)
    {
        if (model == null) return;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r == null || r.material == null) continue;
            string n = r.gameObject.name.ToLower();
            Material mat = r.material;

            if (n.Contains("shield") || n.Contains("crest") || n.Contains("sword") || n.Contains("blade"))
            {
                mat.color = auraColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", auraColor * 0.9f);
            }
            else if (n.Contains("cuirass") || n.Contains("chest") || n.Contains("pauldron") || n.Contains("helmet"))
            {
                mat.color = Color.Lerp(mat.color, bossColor, 0.7f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", bossColor * 0.4f);
            }
            else
            {
                mat.color = Color.Lerp(new Color(0.12f, 0.12f, 0.12f), bossColor, 0.5f);
            }
        }
    }

    public static void StyleModelForCharacter(GameObject model, CharacterData data, CharacterSkin skin = null)
    {
        if (model == null || data == null) return;

        Color themeColor;
        switch (data.role)
        {
            case CharacterData.CharacterRole.Tank:
                themeColor = new Color(0.28f, 0.58f, 0.95f); // Deep Azure / Guardian Blue
                break;
            case CharacterData.CharacterRole.Damage:
                themeColor = new Color(0.95f, 0.25f, 0.18f); // Crimson / Blood War
                break;
            case CharacterData.CharacterRole.Support:
                themeColor = new Color(0.25f, 0.85f, 0.42f); // Emerald / Nature Spirit
                break;
            case CharacterData.CharacterRole.Control:
                themeColor = new Color(0.72f, 0.35f, 0.95f); // Mystic Amethyst / Arcane
                break;
            case CharacterData.CharacterRole.Specialist:
                themeColor = new Color(0.98f, 0.68f, 0.18f); // Radiant Amber / Alchemy
                break;
            default:
                themeColor = new Color(0.85f, 0.75f, 0.3f);
                break;
        }

        // ألوان خاصة لشخصيات محددة لإضفاء طابع فريد
        string lowName = data.characterName.ToLower();
        if (lowName.Contains("druid")) themeColor = new Color(0.2f, 0.8f, 0.35f);
        else if (lowName.Contains("necromancer")) themeColor = new Color(0.65f, 0.15f, 0.85f);
        else if (lowName.Contains("paladin")) themeColor = new Color(1f, 0.88f, 0.35f);
        else if (lowName.Contains("berserker")) themeColor = new Color(0.9f, 0.15f, 0.12f);
        else if (lowName.Contains("shadowblade") || lowName.Contains("assassin")) themeColor = new Color(0.22f, 0.2f, 0.32f);
        else if (lowName.Contains("ironclad")) themeColor = new Color(0.38f, 0.42f, 0.48f);

        if (skin != null && skin.skinColor != Color.white)
        {
            themeColor = skin.skinColor;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            string n = r.gameObject.name.ToLower();
            Material mat = r.material;

            if (n.Contains("shield") || n.Contains("crest"))
            {
                mat.color = themeColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", themeColor * 0.5f);
            }
            else if (n.Contains("cuirass") || n.Contains("chest") || n.Contains("pauldron"))
            {
                mat.color = Color.Lerp(mat.color, themeColor, 0.4f);
            }
            else if (n.Contains("gambeson") || n.Contains("cloth") || n.Contains("tunic"))
            {
                mat.color = Color.Lerp(new Color(0.18f, 0.18f, 0.18f), themeColor, 0.55f);
            }
            else if (n.Contains("sword") || n.Contains("blade"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", themeColor * 0.25f);
            }
        }
    }

    public void ApplySkinVisuals(CharacterSkin skin)
    {
        if (skin == null || skin.skinColor == Color.white) return;

        Transform meshRoot = transform.Find("Character_3D_Mesh");

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (r.gameObject.CompareTag("Weapon") || r.gameObject.CompareTag("Crystal"))
                continue;

            string rn = r.name.ToLower();
            if (rn.Contains("erika") || rn.Contains("teamfootaura") || (meshRoot != null && r.transform.IsChildOf(meshRoot)))
                continue;

            r.material.color = skin.skinColor;
            if (skin.emissionColor != Color.black)
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", skin.emissionColor * 0.4f);
            }
        }
    }

    // =========================
    // Awake & Start
    // =========================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (GetComponent<LevelBoss>() != null)
        {
            // الزعيم يتم التحكم بفيزيائه وجاذبيته وحركته حصرياً عبر LevelBoss لمنع أي سقوط أو تعارض
            return;
        }

        if (rb == null) rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }
    }

    // =========================
    // Update
    // =========================

    void Update()
    {
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (isNetworkActive && !IsOwner)
            return;

        // حماية فورية من السقوط خارج حدود المشهد في الفراغ
        if (transform.position.y < -5f)
        {
            ResetToSafeSpawn();
            return;
        }

        // في طور الشبكة: ننتظر انتهاء العد التنازلي وبدء اللعبة
        if (isNetworkActive)
        {
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
            {
                if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }
                return;
            }
        }

        // التبديل السريع للجري التلقائي بنقرة واحدة على Shift (مثل الموبايل)
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            SprintToggleActive = !SprintToggleActive;
        }

        CheckGroundedRaycast();
        Move();
        Jump();
        RotateWithMouse();
    }

    private float lastJumpTime = -10f;
    private bool wasAirborne = false;
    private float highestFallY = float.MinValue;
    private bool isCurrentlyGrounded = false;

    void CheckGroundedRaycast()
    {
        isCurrentlyGrounded = false;
        Vector3 origin = transform.position + Vector3.up * 0.15f;
        if (Physics.SphereCast(origin, 0.30f, Vector3.down, out RaycastHit hit, 1.10f, ~LayerMask.GetMask("Ignore Raycast")))
        {
            if (!hit.collider.isTrigger && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
            {
                isCurrentlyGrounded = true;
                if (Time.time - lastJumpTime >= 0.25f)
                {
                    jumpCount = 0;
                }
            }
        }

        if (Time.time - lastJumpTime < 0.25f)
        {
            isCurrentlyGrounded = false;
        }

        TrackFallDamage(isCurrentlyGrounded);
    }

    void TrackFallDamage(bool grounded)
    {
        if (!grounded)
        {
            if (!wasAirborne)
            {
                wasAirborne = true;
                highestFallY = transform.position.y;
            }
            else if (transform.position.y > highestFallY)
            {
                highestFallY = transform.position.y;
            }
        }
        else
        {
            if (wasAirborne)
            {
                float fallDistance = highestFallY - transform.position.y;
                wasAirborne = false;
                highestFallY = float.MinValue;

                // نظام ضرر السقوط الواقعي والتصاعدي الدقيق كما طلبه المستخدم تماماً:
                // 1. القفزات الصغيرة العادية على نفس المنصة (< 3.8م): آمنة تماماً بدون أي ضرر (0 HP).
                // 2. السقوط من مستوى إلى المستوى الذي تحته مباشرة (~5.5م إلى 10م): ينقص الصحة قليلاً (15 إلى 40 HP).
                // 3. السقوط من مكان أعلى (طابقين ~16م إلى 20م): ينقص الصحة أكثر (70 إلى 125 HP).
                // 4. السقوط من أعلى قمة الشجرة (TopLevel) إلى الأرض مباشرة (أكثر من 30م إلى 40م): ضرر ساحق/قاتل (250 إلى 500+ HP)!
                if (fallDistance > 3.8f)
                {
                    float damage = (fallDistance - 3.8f) * 6.0f;

                    if (fallDistance > 11.0f)
                    {
                        damage += (fallDistance - 11.0f) * 5.0f;
                    }

                    if (fallDistance > 22.0f)
                    {
                        damage += (fallDistance - 22.0f) * 8.0f;
                    }

                    if (fallDistance > 32.0f)
                    {
                        damage += (fallDistance - 32.0f) * 15.0f;
                    }

                    Health h = GetComponent<Health>();
                    if (h != null && !h.isDead)
                    {
                        h.TakeDamage(damage);
                        Health.TriggerDamageFlash(0.6f);
                        ThirdPersonCamera.Shake(0.35f, Mathf.Clamp(damage / 100f, 0.2f, 0.8f));
                        Debug.Log($"💥 [Fall Damage] Dropped {fallDistance:F1}m -> Suffered {damage:F1} HP damage! (Remaining HP: {h.currentHealth}/{h.maxHealth})");
                    }
                }
            }
        }
    }

    public void ResetToSafeSpawn()
    {
        wasAirborne = false;
        highestFallY = float.MinValue;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Transform sp = null;
        if (SpawnManager.Instance != null)
            sp = SpawnManager.Instance.GetSpawnPoint(team);

        if (sp != null)
        {
            transform.position = new Vector3(Mathf.Clamp(sp.position.x, -14f, 14f), sp.position.y + 1.05f, Mathf.Clamp(sp.position.z, -14f, 14f));
            transform.rotation = sp.rotation;
        }
        else
        {
            transform.position = (team == TeamType.Destroyer) ? new Vector3(8f, 1.05f, 0f) : new Vector3(-8f, 1.05f, 0f);
        }
    }

    // =========================
    // Rotation
    // =========================

    void RotateWithMouse()
    {
        float mouseX = Input.GetAxis("Mouse X");

        transform.Rotate(
            Vector3.up *
            mouseX *
            200f *
            Time.deltaTime
        );
    }

    // =========================
    // Movement
    // =========================

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // دعم الجويستيك للموبايل والكمبيوتر أثناء التجربة
        if (joystick != null &&
            (Mathf.Abs(joystick.Horizontal) > 0.05f ||
             Mathf.Abs(joystick.Vertical) > 0.05f))
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }

        Vector3 inputDirection = new Vector3(h, 0f, v);

        // فحص الأرضية والمنحدرات تحت قدم اللاعب مباشرة بدقة تامة لمنع الطفو أو القفز اللاإرادي
        RaycastHit hit;
        bool hasHit = false;
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out hit, 1.35f, ~LayerMask.GetMask("Ignore Raycast")))
        {
            if (!hit.collider.isTrigger && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
            {
                hasHit = true;
            }
        }
        if (!hasHit && Physics.SphereCast(transform.position + Vector3.up * 0.2f, 0.25f, Vector3.down, out hit, 1.15f, ~LayerMask.GetMask("Ignore Raycast")))
        {
            if (!hit.collider.isTrigger && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
            {
                hasHit = true;
            }
        }
        bool isGroundedSlope = hasHit && hit.normal.y > 0.40f && (Time.time - lastJumpTime > 0.30f);

        // عند عدم الضغط على أي زر حركة:
        if (inputDirection.sqrMagnitude < 0.01f)
        {
            if (rb != null && !rb.isKinematic)
            {
                if (isGroundedSlope || isCurrentlyGrounded)
                {
                    // على الأرضية أو المنحدر: نثبت السرعة تماماً لمنع الانزلاق والقفز اللاإرادي
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    // في الهواء: نوقف الحركة الأفقية فقط ونمنع القفز اللاإرادي الصاعد إذا لم تكن قفزة متعمدة
                    float yVel = (Time.time - lastJumpTime < 0.30f) ? rb.linearVelocity.y : Mathf.Min(0f, rb.linearVelocity.y);
                    rb.linearVelocity = new Vector3(0f, yVel, 0f);
                }
            }
            return;
        }

        inputDirection.Normalize();

        Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        Vector3 cameraForward = cam != null ? cam.transform.forward : transform.forward;
        Vector3 cameraRight = cam != null ? cam.transform.right : transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * v + cameraRight * h;
        moveDirection.Normalize();

        bool isSprinting = SprintToggleActive || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || MobileSprintActive;
        float effectiveWalk = Mathf.Max(5f, walkSpeed);
        float effectiveRun = Mathf.Max(effectiveWalk * 1.65f, runSpeed);
        float currentSpeed = isSprinting ? effectiveRun : effectiveWalk;

        if (isOnClimbPath)
            currentSpeed *= climbSpeedMultiplier;

        if (isCarryingCrystal != null && isCarryingCrystal.Value)
            currentSpeed *= crystalSpeedMultiplier;

        // فحص الميلان لتسلق المنحدرات والممرات بسلاسة تامة دون أن يعلق في الجدران أو المنحدرات
        Vector3 targetMove = moveDirection;
        if (isGroundedSlope)
        {
            targetMove = Vector3.ProjectOnPlane(moveDirection, hit.normal).normalized;
        }

        if (rb != null && !rb.isKinematic)
        {
            Vector3 vel = targetMove * currentSpeed;
            if (isGroundedSlope)
            {
                // حركة موجهة بدقة بمحاذاة سطح المنحدر
                rb.linearVelocity = vel;

                // قوة التصاق سطحية تثبت اللاعب على مسار المنحدر المنحني وتمنع القفز أو الارتداد
                rb.AddForce(-hit.normal * 16f, ForceMode.Acceleration);
            }
            else
            {
                rb.linearVelocity = new Vector3(vel.x, rb.linearVelocity.y, vel.z);
            }
        }
        else
        {
            transform.position += targetMove * currentSpeed * Time.deltaTime;
        }

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                15f * Time.deltaTime
            );
        }
    }

    // =========================
    // Jump
    // =========================

    void Jump()
    {
        bool doJump = Input.GetKeyDown(KeyCode.Space) || jumpButtonPressed;
        jumpButtonPressed = false;

        if (!doJump || jumpCount >= maxJumps || (Time.time - lastJumpTime < 0.14f))
            return;

        lastJumpTime = Time.time;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                jumpForce,
                rb.linearVelocity.z
            );
        }

        jumpCount++;

        CharacterVisualAnimator visualAnim = GetComponentInChildren<CharacterVisualAnimator>();
        if (visualAnim != null)
        {
            visualAnim.TriggerJumpGesture();
        }
    }

    public void OnJumpButtonPressed()
    {
        jumpButtonPressed = true;
    }

    // =========================
    // Collision
    // =========================

    void OnCollisionEnter(Collision collision)
    {
        CheckGroundedCollision(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        CheckGroundedCollision(collision);
    }

    void CheckGroundedCollision(Collision collision)
    {
        if (Time.time - lastJumpTime >= 0.25f)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.3f)
                {
                    jumpCount = 0;
                    if (wasAirborne)
                    {
                        TrackFallDamage(true);
                    }
                    break;
                }
            }
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Climb"))
        {
            isOnClimbPath = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Climb"))
        {
            isOnClimbPath = false;
        }
    }
}

public class GroundLockedFootAura : MonoBehaviour
{
    private float lastGroundY = 0.03f;
    private bool initialized = false;
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[8];
    private float currentRot = 0f;

    void LateUpdate()
    {
        Transform p = transform.parent;
        if (p == null) return;

        if (!initialized)
        {
            lastGroundY = p.position.y - 0.92f;
            initialized = true;
        }

        Vector3 rayOrigin = p.position + Vector3.up * 0.5f;
        int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, hitBuffer, 25f, ~LayerMask.GetMask("Ignore Raycast"));
        float bestY = float.MinValue;
        bool foundGround = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitBuffer[i].collider;
            if (col == null || col.isTrigger) continue;
            if (col.transform == p || col.transform.IsChildOf(p)) continue;
            if (col.GetComponentInParent<PlayerMovement>() != null || col.GetComponentInParent<BotController>() != null) continue;

            if (hitBuffer[i].point.y > bestY && hitBuffer[i].point.y <= p.position.y + 0.2f)
            {
                bestY = hitBuffer[i].point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            lastGroundY = bestY + 0.03f;
        }

        // تثبيت الدائرة على الأرض تماماً تحت الشخصية ودوران انسيابي سحري هادئ
        currentRot += Time.deltaTime * 24f;
        transform.position = new Vector3(p.position.x, lastGroundY, p.position.z);
        transform.rotation = Quaternion.Euler(0f, currentRot, 0f);
    }
}