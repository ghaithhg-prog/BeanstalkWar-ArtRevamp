using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// ضريح غنائم الزعيم (Boss Tombstone / Spoils Chest)
/// يظهر في موقع موت الزعيم ويحتوي على الكنز والقوة التي يستطيع فريق القاتل جمعها
/// </summary>
public class BossTombstone : MonoBehaviour
{
    public string bossTitle = "Ancient Boss";
    public PlayerMovement.TeamType rewardedTeam = PlayerMovement.TeamType.Protector;
    public string killerPlayerName = "Warrior";
    public int bonusCoins = 150;
    public int bonusXP = 300;

    private bool isCollected = false;
    private Light chestLight;
    private GameObject floatingCrest;

    public void Setup(string title, PlayerMovement.TeamType team, string killerName, int coins, int xp)
    {
        bossTitle = title;
        rewardedTeam = team;
        killerPlayerName = killerName;
        bonusCoins = coins;
        bonusXP = xp;

        BuildTombstoneVisuals();
    }

    void BuildTombstoneVisuals()
    {
        // 1. قاعدة الضريح الحجري
        GameObject baseStone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseStone.name = "StoneBase";
        baseStone.transform.SetParent(transform);
        baseStone.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        baseStone.transform.localScale = new Vector3(2.4f, 0.4f, 2.4f);
        Collider baseCol = baseStone.GetComponent<Collider>();
        if (baseCol != null) Destroy(baseCol);

        Renderer baseR = baseStone.GetComponent<Renderer>();
        if (baseR != null)
        {
            baseR.material.color = new Color(0.2f, 0.22f, 0.25f);
        }

        // 2. صندوق الكنز الذهبي / التابوت الملكي
        GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chest.name = "GoldenChest";
        chest.transform.SetParent(transform);
        chest.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        chest.transform.localScale = new Vector3(1.4f, 0.9f, 1.0f);
        Collider chestCol = chest.GetComponent<Collider>();
        if (chestCol != null) Destroy(chestCol);

        Color teamColor = rewardedTeam == PlayerMovement.TeamType.Protector 
            ? new Color(0.15f, 0.75f, 1f) 
            : new Color(1f, 0.3f, 0.1f);

        Renderer chestR = chest.GetComponent<Renderer>();
        if (chestR != null)
        {
            chestR.material.color = new Color(0.95f, 0.75f, 0.15f); // ذهبي
            chestR.material.EnableKeyword("_EMISSION");
            chestR.material.SetColor("_EmissionColor", new Color(0.6f, 0.45f, 0.05f));
        }

        // 3. بلورة/تاج يطفو ويدور فوق الصندوق
        floatingCrest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        floatingCrest.name = "FloatingRelic";
        floatingCrest.transform.SetParent(transform);
        floatingCrest.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        floatingCrest.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
        Collider relicCol = floatingCrest.GetComponent<Collider>();
        if (relicCol != null) Destroy(relicCol);

        Renderer relicR = floatingCrest.GetComponent<Renderer>();
        if (relicR != null)
        {
            relicR.material.color = teamColor;
            relicR.material.EnableKeyword("_EMISSION");
            relicR.material.SetColor("_EmissionColor", teamColor * 2f);
        }

        // 4. ضوء هالة ساطعة
        chestLight = gameObject.AddComponent<Light>();
        chestLight.type = LightType.Point;
        chestLight.range = 9f;
        chestLight.color = Color.Lerp(Color.yellow, teamColor, 0.4f);
        chestLight.intensity = 4f;

        // 5. محفز الالتقاط (Trigger)
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2.8f;
    }

    void Update()
    {
        if (isCollected) return;

        // دوران وتموج البلورة الأثيرية
        if (floatingCrest != null)
        {
            floatingCrest.transform.Rotate(0f, 60f * Time.deltaTime, 30f * Time.deltaTime, Space.Self);
            floatingCrest.transform.localPosition = new Vector3(0f, 2.2f + Mathf.Sin(Time.time * 3f) * 0.2f, 0f);
        }

        // نبض الضوء
        if (chestLight != null)
        {
            chestLight.intensity = 3f + Mathf.PingPong(Time.time * 2.5f, 2.5f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        Health h = other.GetComponent<Health>();
        if (h != null && h.isDead) return;

        // التحقق أن اللاعب من نفس الفريق الفائز بالزعيم
        if (pm.team != rewardedTeam)
        {
            return;
        }

        CollectTombstone(pm);
    }

    void CollectTombstone(PlayerMovement collector)
    {
        if (isCollected) return;
        isCollected = true;

        Debug.Log($"🎁 SPOILS CLAIMED: {collector.name} collected the spoils of {bossTitle}!");

        // منح العملات وXP
        if (collector.IsOwner && PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.AddCoins(bonusCoins);
            PlayerProfile.Instance.AddAccountXP(bonusXP);
            PlayerProgressionAndShopUI.Instance?.ShowNotification($"👑 Boss Spoils Claimed: +{bonusCoins} 🪙 | +{bonusXP} XP!");
        }

        // منح بف القوة الجبارة (+50% ضرر وهالة مضيئة لمدة 30 ثانية)
        TitanPowerBuff buff = collector.GetComponent<TitanPowerBuff>();
        if (buff == null)
        {
            buff = collector.gameObject.AddComponent<TitanPowerBuff>();
        }
        buff.ActivateBuff(bossTitle, 45f);

        StartCoroutine(CollectionSequence());
    }

    IEnumerator CollectionSequence()
    {
        if (chestLight != null) chestLight.intensity = 8f;

        float timer = 0.4f;
        Vector3 startScale = transform.localScale;
        while (timer > 0f)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, startScale, timer / 0.4f);
            timer -= Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private PlayerMovement cachedLocalPlayer;
    private float nextPlayerCheck = 0f;

    PlayerMovement GetLocalPlayer()
    {
        if (cachedLocalPlayer != null) return cachedLocalPlayer;
        if (Time.time < nextPlayerCheck) return null;
        nextPlayerCheck = Time.time + 1.0f;

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p != null && (p.IsOwner || p.GetComponent<BotController>() == null))
            {
                cachedLocalPlayer = p;
                return p;
            }
        }
        return null;
    }

    void OnGUI()
    {
        if (isCollected) return;

        PlayerMovement localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        // لا يظهر الإشعار إلا إذا اقترب اللاعب من الصندوق (أقل من 7 أمتار) لمنع أي تشويش أثناء القتال والمعركة
        float distToPlayer = Vector3.Distance(localPlayer.transform.position, transform.position);
        if (distToPlayer > 7.0f) return;

        // وضع الإشعار في زاوية الشاشة السفلية بعيداً تماماً عن منتصف الشاشة ومنطقة القتال والكروس إير
        float sw = Screen.width;
        float sh = Screen.height;
        float width = 310f;
        float height = 54f;
        float x = sw - width - 24f;
        float y = sh - height - 24f;

        // خلفية زجاجية فائقة الشفافية وأنيقة دون أي مربع أسود معتم
        GUI.color = new Color(0.04f, 0.03f, 0.05f, 0.45f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);

        Color teamColor = rewardedTeam == PlayerMovement.TeamType.Protector 
            ? new Color(0.2f, 0.8f, 1f, 0.85f) 
            : new Color(1f, 0.4f, 0.2f, 0.85f);

        // إطار نيون نحيف بلون الفريق
        GUI.color = teamColor;
        GUI.DrawTexture(new Rect(x, y, width, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + height - 1.5f, width, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 1.5f, height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + width - 1.5f, y, 1.5f, height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // العنوان بالعربية وبجميع اللغات بوضوح فائق وظل داكن
        string localizedTitle = LanguageSelectionUI.Localize(
            $"👑 SPOILS: {bossTitle}",
            ar: $"👑 غنائم المعركة: {bossTitle}",
            fa: $"👑 غنائم نبرد: {bossTitle}",
            ru: $"👑 ТРОФЕИ: {bossTitle}",
            fr: $"👑 BUTIN : {bossTitle}",
            es: $"👑 BOTÍN: {bossTitle}",
            de: $"👑 BEUTE: {bossTitle}",
            hi: $"👑 युद्ध की लूट: {bossTitle}",
            zh: $"👑 战利品: {bossTitle}",
            ja: $"👑 戦利品: {bossTitle}",
            pt: $"👑 ESPÓLIOS: {bossTitle}",
            it: $"👑 BOTTINO: {bossTitle}",
            tr: $"👑 SAVAŞ GANİMETİ: {bossTitle}"
        );

        GUIStyle titleShadow = new GUIStyle(GUI.skin.label);
        titleShadow.alignment = TextAnchor.MiddleCenter;
        titleShadow.fontSize = 11;
        titleShadow.fontStyle = FontStyle.Bold;
        titleShadow.normal.textColor = new Color(0f, 0f, 0f, 0.95f);

        GUIStyle titleStyle = new GUIStyle(titleShadow);
        titleStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);

        Rect titleRect = new Rect(x, y + 4f, width, 20f);
        GUI.Label(new Rect(titleRect.x + 1f, titleRect.y + 1f, titleRect.width, titleRect.height), localizedTitle, titleShadow);
        GUI.Label(titleRect, localizedTitle, titleStyle);

        // النص الإرشادي واضح ومعرب بالكامل
        bool isForMyTeam = (localPlayer.team == rewardedTeam);
        string subText = isForMyTeam
            ? LanguageSelectionUI.Localize(
                $"🌿 Step near to claim reward (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                ar: $"🌿 اقترب لاستلام المكافأة (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                fa: $"🌿 برای دریافت پاداش نزدیک شوید (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                ru: $"🌿 Подойдите для награды (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                fr: $"🌿 Approchez pour récupérer (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                es: $"🌿 Acércate para reclamar (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                de: $"🌿 Nähern zum Einsammeln (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                hi: $"🌿 इनाम पाने के लिए पास जाएं (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                zh: $"🌿 靠近领取奖励 (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                ja: $"🌿 近づいて報酬を獲得 (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                pt: $"🌿 Aproxime-se para resgatar (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                it: $"🌿 Avvicinati per riscattare (+{bonusCoins} 🪙 | +{bonusXP} XP)",
                tr: $"🌿 Ödülü almak için yaklaşın (+{bonusCoins} 🪙 | +{bonusXP} XP)"
              )
            : LanguageSelectionUI.Localize(
                "⚔️ Reserved for opposing champions",
                ar: "⚔️ غنائم مخصصة للفريق المنافس",
                fa: "⚔️ ویژه قهرمانان تیم حریف",
                ru: "⚔️ Трофеи команды противника",
                fr: "⚔️ Réservé aux champions adverses",
                es: "⚔️ Reservado para el equipo rival",
                de: "⚔️ Reserviert für das gegnerische Team",
                hi: "⚔️ विपक्षी टीम के लिए आरक्षित",
                zh: "⚔️ 属于对手团队的战利品",
                ja: "⚔️ 敵チーム専用の戦利品",
                pt: "⚔️ Reservado para a equipe adversária",
                it: "⚔️ Riservato ai campioni avversari",
                tr: "⚔️ Rakip takım şampiyonlarına ayrılmıştır"
              );

        GUIStyle subShadow = new GUIStyle(GUI.skin.label);
        subShadow.alignment = TextAnchor.MiddleCenter;
        subShadow.fontSize = 10;
        subShadow.fontStyle = FontStyle.Normal;
        subShadow.normal.textColor = new Color(0f, 0f, 0f, 0.9f);

        GUIStyle subStyle = new GUIStyle(subShadow);
        subStyle.normal.textColor = isForMyTeam ? new Color(0.85f, 1f, 0.90f) : new Color(1f, 0.6f, 0.5f);

        Rect subRect = new Rect(x, y + 26f, width, 22f);
        GUI.Label(new Rect(subRect.x + 1f, subRect.y + 1f, subRect.width, subRect.height), subText, subShadow);
        GUI.Label(subRect, subText, subStyle);
    }
}
