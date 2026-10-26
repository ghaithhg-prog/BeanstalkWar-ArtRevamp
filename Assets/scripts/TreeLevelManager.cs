using UnityEngine;

public class TreeLevelManager : MonoBehaviour
{
    [Header("Tree")]
    public Transform tree;

    [Header("Levels")]
    public GameObject[] levels;
    public float[] unlockHeights;

    [Header("Paths")]
    public SpiralConnector spiralConnector;

    [Header("Navigation")]
    public RuntimeNavMeshUpdater navMeshUpdater;

    private bool[] unlocked;
    private GameObject[] cachedBosses = new GameObject[4];

    void Awake()
    {
        if (spiralConnector == null)
        {
            spiralConnector = SpiralConnector.Instance ?? FindAnyObjectByType<SpiralConnector>();
        }

        // إخفاء المستويات 1 و 2 و 3 (Level_2, Level_3, TopLevel) فوراً في Awake لضمان عدم ظهورها في بداية اللعبة
        if (levels != null)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                bool shouldBeActive = (i == 0);
                if (levels[i] != null)
                {
                    levels[i].SetActive(shouldBeActive);
                }
            }
        }

        // تأمين إضافي عبر البحث المباشر بالاسم لضمان إغلاق المنصات العليا حتى نمو الشجرة إليها
        GameObject treeLevelsParent = GameObject.Find("TreeLevels");
        if (treeLevelsParent != null)
        {
            Transform l2 = treeLevelsParent.transform.Find("Level_2");
            if (l2 != null) l2.gameObject.SetActive(false);
            Transform l3 = treeLevelsParent.transform.Find("Level_3");
            if (l3 != null) l3.gameObject.SetActive(false);
            Transform top = treeLevelsParent.transform.Find("TopLevel");
            if (top != null) top.gameObject.SetActive(false);
        }
        else
        {
            GameObject l2 = GameObject.Find("Level_2");
            if (l2 != null) l2.SetActive(false);
            GameObject l3 = GameObject.Find("Level_3");
            if (l3 != null) l3.SetActive(false);
            GameObject top = GameObject.Find("TopLevel");
            if (top != null) top.SetActive(false);
        }

        // تأمين إخفاء ممرات الصعود فوراً في Awake
        if (spiralConnector != null)
        {
            spiralConnector.SetSectionUnlocked(0, true);
            spiralConnector.SetSectionUnlocked(1, false);
            spiralConnector.SetSectionUnlocked(2, false);
            spiralConnector.SetSectionUnlocked(3, false);
        }
    }

    void Start()
    {
        if (spiralConnector == null)
        {
            spiralConnector = SpiralConnector.Instance ?? FindAnyObjectByType<SpiralConnector>();
        }

        unlocked = new bool[levels.Length];

        // 1. البحث عن الزعماء الثلاثة وتخزين مراجعهم بدقة قبل إخفائهم
        for (int b = 1; b <= 3; b++)
        {
            GameObject bossObj = GameObject.Find($"Boss_Level_{b}");
            if (bossObj != null)
            {
                cachedBosses[b] = bossObj;
            }
        }

        // بحث شامل إضافي في المشهد للزعماء حتى لو كانوا غير نشطين
        LevelBoss[] allBosses = FindObjectsByType<LevelBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var lb in allBosses)
        {
            if (lb != null && lb.levelNumber >= 1 && lb.levelNumber <= 3)
            {
                if (cachedBosses[lb.levelNumber] == null)
                {
                    cachedBosses[lb.levelNumber] = lb.gameObject;
                }
            }
        }

        // 2. ضبط تفعيل المستويات (المستوى 0 فقط هو النشط في البداية)
        for (int i = 0; i < levels.Length; i++)
        {
            bool shouldBeActive = i == 0;

            if (levels[i] != null)
            {
                levels[i].SetActive(shouldBeActive);
            }

            unlocked[i] = shouldBeActive;
        }

        // ضمان أن unlockHeights[3] يطابق قمة الشجرة (40.0م) ليفتح TopLevel في علو القمة بالضبط
        if (unlockHeights != null && unlockHeights.Length > 3)
        {
            unlockHeights[3] = 40.0f;
        }

        // تأمين إخفاء وإغلاق ممرات الصعود للطوابق العلوية في البداية
        if (spiralConnector != null)
        {
            spiralConnector.SetSectionUnlocked(0, true);
            spiralConnector.SetSectionUnlocked(1, false);
            spiralConnector.SetSectionUnlocked(2, false);
            spiralConnector.SetSectionUnlocked(3, false);
        }

        // 3. ضمان يقظة الزعيم الأول (Boss_Level_1) فوق المنصة الأولى (Level 1) فوراً
        if (cachedBosses[1] != null)
        {
            cachedBosses[1].SetActive(true);
            LevelBoss lb1 = cachedBosses[1].GetComponent<LevelBoss>();
            if (lb1 != null)
            {
                Vector3 p1 = (levels != null && levels.Length > 0 && levels[0] != null) ? levels[0].transform.position : cachedBosses[1].transform.position;
                lb1.AwakenBossNow(p1);
            }
        }

        // وضع زعماء المستويات المغلقة (المستوى 2 والمستوى 3) في حالة الخمول حتى تفتح منصاتهما
        for (int b = 2; b <= 3; b++)
        {
            if (cachedBosses[b] != null)
            {
                LevelBoss lb = cachedBosses[b].GetComponent<LevelBoss>();
                if (lb != null)
                {
                    lb.CheckPlatformReadiness();
                }
                else
                {
                    cachedBosses[b].SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        bool isMatchPlaying = GameStateManager.Instance == null ||
                              GameStateManager.Instance.currentState == GameStateManager.GameState.Playing;

        if (!isMatchPlaying)
        {
            return;
        }

        if (tree == null && TreeGrowth.Instance != null)
        {
            tree = TreeGrowth.Instance.transform;
        }

        float currentHeight = (TreeGrowth.Instance != null) ? TreeGrowth.Instance.CurrentHeight : (tree != null ? tree.localScale.y * 2f : 5.80f);
        float elapsed = (TreeGrowth.Instance != null) ? TreeGrowth.Instance.ElapsedGrowthTime : 0f;

        // المستوى 2: يفتح أوتوماتيكياً بعد مرور 18 ثانية أو عند وصول الشجرة لارتفاع 15.0م
        if (levels != null && levels.Length > 1 && !unlocked[1] && (elapsed >= 18.0f || currentHeight >= 15.0f))
        {
            UnlockLevel(1);
        }

        // المستوى 3: يفتح أوتوماتيكياً بعد مرور 36 ثانية أو عند وصول الشجرة لارتفاع 25.0م
        if (levels != null && levels.Length > 2 && !unlocked[2] && (elapsed >= 36.0f || currentHeight >= 25.0f))
        {
            UnlockLevel(2);
        }

        // القمة (TopLevel): تفتح أوتوماتيكياً بعد مرور 54 ثانية أو عند وصول الشجرة لارتفاع 39.5م وتلتصق بالمنصة تماماً
        if (levels != null && levels.Length > 3 && !unlocked[3] && (elapsed >= 54.0f || currentHeight >= 39.5f))
        {
            UnlockLevel(3);
        }
    }

    public void UnlockLevel(int index)
    {
        // =========================
        // فتح المستوى بسلاسة ونمو طبيعي رويداً رويداً
        // =========================

        if (index < levels.Length && levels[index] != null)
        {
            SmoothRevealLevel(levels[index]);
        }

        unlocked[index] = true;

        // =========================
        // فتح الممر المؤدي إليه
        // =========================

        if (spiralConnector == null)
        {
            spiralConnector = SpiralConnector.Instance ?? FindAnyObjectByType<SpiralConnector>();
        }

        if (spiralConnector != null)
        {
            spiralConnector.SetSectionUnlocked(index, true);
        }

        // =========================
        // بناء وتحديث شبكة الملاحة NavMesh بهدوء وسلاسة
        // =========================
        if (LevelBossManager.Instance != null)
        {
            LevelBossManager.Instance.RebuildNavigationMesh();
        }

        // =========================
        // إيقاظ الزعيم المخصص فوق المنصة
        // =========================
        int bossNumber = index + 1; // index 1 يفتح Level_2 (Boss 2) و index 2 يفتح Level_3 (Boss 3)
        if (bossNumber >= 2 && bossNumber <= 3)
        {
            if (LevelBossManager.Instance != null)
            {
                LevelBossManager.Instance.AwakenBossForLevel(bossNumber);
            }
        }

        Debug.Log(
            "Level " +
            (index + 1) +
            " unlocked with its paths!"
        );

        RevealLockedWispsForLevel(index);

        // =========================
        // تحديث الملاحة
        // =========================

        if (navMeshUpdater != null)
        {
            StartCoroutine(RebuildNavMeshDelayed(1.2f));
        }
    }

    private void SmoothRevealLevel(GameObject levelObj)
    {
        if (levelObj == null) return;
        levelObj.SetActive(true);
        StartCoroutine(AnimateLevelEmergenceRoutine(levelObj));
    }

    private System.Collections.IEnumerator AnimateLevelEmergenceRoutine(GameObject levelObj)
    {
        if (levelObj == null) yield break;
        Transform tr = levelObj.transform;
        Vector3 targetScale = tr.localScale;
        if (targetScale.sqrMagnitude < 0.01f) targetScale = Vector3.one;

        Vector3 startScale = new Vector3(targetScale.x * 0.95f, 0.02f, targetScale.z * 0.95f);
        tr.localScale = startScale;

        float duration = 1.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (tr != null)
            {
                tr.localScale = Vector3.Lerp(startScale, targetScale, t);
            }
            yield return null;
        }

        if (tr != null)
        {
            tr.localScale = targetScale;
        }
    }

    private System.Collections.IEnumerator RebuildNavMeshDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (navMeshUpdater != null)
        {
            navMeshUpdater.RebuildNavMesh();
        }
    }

    void RevealLockedWispsForLevel(int levelIndex)
    {
        float minY = levelIndex == 1 ? 7f : (levelIndex == 2 ? 14f : 999f);
        float maxY = levelIndex == 1 ? 14f : (levelIndex == 2 ? 999f : -1f);

        GameObject critterGroup = GameObject.Find("ImaginaryForestCritters");
        if (critterGroup == null) return;

        foreach (Transform child in critterGroup.transform)
        {
            if (child.name.Contains("_Locked"))
            {
                float y = child.position.y;
                if (y >= minY && y < maxY)
                {
                    child.gameObject.SetActive(true);
                    child.name = child.name.Replace("_Locked", "");
                }
            }
        }
    }
}