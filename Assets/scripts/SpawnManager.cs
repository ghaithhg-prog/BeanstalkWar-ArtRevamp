using UnityEngine;
using UnityEngine.AI;

public class SpawnManager : MonoBehaviour
{
    private static SpawnManager _instance;
    public static SpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SpawnManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SpawnManager");
                    _instance = obj.AddComponent<SpawnManager>();
                }
            }
            return _instance;
        }
        set => _instance = value;
    }

    [Header("Protector Spawn Points")]
    public Transform protectorSpawn;
    public Transform[] protectorSpawns;

    [Header("Destroyer Spawn Points")]
    public Transform destroyerSpawn;
    public Transform[] destroyerSpawns;

    private int nextProtectorSpawn = 0;
    private int nextDestroyerSpawn = 0;

    void Awake()
    {
        _instance = this;
        AutoFindSpawns();
    }

    public void AutoFindSpawns()
    {
        if (protectorSpawn == null)
        {
            GameObject pObj = GameObject.Find("ProtectorSpawn");
            if (pObj != null) protectorSpawn = pObj.transform;
        }

        if (destroyerSpawn == null)
        {
            GameObject dObj = GameObject.Find("DestroyerSpawn");
            if (dObj != null) destroyerSpawn = dObj.transform;
        }

        if ((protectorSpawns == null || protectorSpawns.Length == 0) && protectorSpawn != null)
        {
            int childCount = protectorSpawn.childCount;
            if (childCount > 0)
            {
                protectorSpawns = new Transform[childCount];
                for (int c = 0; c < childCount; c++)
                {
                    protectorSpawns[c] = protectorSpawn.GetChild(c);
                }
            }
        }

        if ((destroyerSpawns == null || destroyerSpawns.Length == 0) && destroyerSpawn != null)
        {
            int childCount = destroyerSpawn.childCount;
            if (childCount > 0)
            {
                destroyerSpawns = new Transform[childCount];
                for (int c = 0; c < childCount; c++)
                {
                    destroyerSpawns[c] = destroyerSpawn.GetChild(c);
                }
            }
        }
    }

    /// <summary>
    /// يحسب موقع تفريخ تكتيكي فريد لكل بوت أو لاعب على بعد 6 إلى 8 أمتار من الشجرة
    /// وعلى سطح NavMesh مباشرة لمنع التكدس أو البدء تحت جذع الشجرة مباشرة.
    /// </summary>
    public Vector3 GetSpawnPosition(PlayerMovement.TeamType team, int index = -1)
    {
        Vector3 treeCenter = Vector3.zero;
        if (TreeGrowth.Instance != null)
        {
            treeCenter = TreeGrowth.Instance.transform.position;
        }
        else
        {
            GameObject tree = GameObject.FindGameObjectWithTag("Tree") ?? GameObject.Find("Tree");
            if (tree != null) treeCenter = tree.transform.position;
        }
        treeCenter.y = 0f;

        // نقطة الارتكاز الأساسية على مسافة 7.2 أمتار من الشجرة (ضمن النطاق المطلوب 6 إلى 8 أمتار)
        float baseDistance = 7.2f;
        Vector3 teamDir = (team == PlayerMovement.TeamType.Protector) ? Vector3.left : Vector3.right;
        Vector3 basePos = treeCenter + teamDir * baseDistance;

        // انتشار جانبي تكتيكي منظم على محور Z لمنع تداخل الوحدات فوق بعضها
        if (index > 0)
        {
            float sideSign = (index % 2 == 1) ? 1f : -1f;
            float sideOffset = ((index + 1) / 2) * 1.5f;
            basePos += new Vector3(0f, 0f, sideSign * sideOffset);

            // ضمان بقاء المسافة الكلية عن الشجرة بدقة بين 6.5 إلى 7.8 أمتار
            Vector3 fromTree = basePos - treeCenter;
            fromTree.y = 0f;
            if (fromTree.magnitude > 7.8f)
            {
                basePos = treeCenter + fromTree.normalized * 7.8f;
            }
        }

        // معايرة الموقع بدقة فائقة على NavMesh لضمان وقوف البوتات واللاعبين فوراً وثباتهم
        if (NavMesh.SamplePosition(basePos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            return hit.position + Vector3.up * 0.15f;
        }

        return basePos + Vector3.up * 0.2f;
    }

    public Transform GetSpawnPoint(PlayerMovement.TeamType team)
    {
        AutoFindSpawns();

        Transform singleSpawn = (team == PlayerMovement.TeamType.Protector) ? protectorSpawn : destroyerSpawn;

        Vector3 treeCenter = Vector3.zero;
        if (TreeGrowth.Instance != null) treeCenter = TreeGrowth.Instance.transform.position;
        else
        {
            GameObject tree = GameObject.FindGameObjectWithTag("Tree") ?? GameObject.Find("Tree");
            if (tree != null) treeCenter = tree.transform.position;
        }
        treeCenter.y = 0f;

        Vector3 idealPos = treeCenter + ((team == PlayerMovement.TeamType.Protector) ? Vector3.left : Vector3.right) * 7.2f;

        if (singleSpawn != null)
        {
            if (NavMesh.SamplePosition(idealPos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                singleSpawn.position = hit.position;
            }
            else
            {
                singleSpawn.position = idealPos;
            }
            return singleSpawn;
        }

        GameObject fallback = new GameObject("SpawnPoint_" + team);
        if (NavMesh.SamplePosition(idealPos, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
        {
            fallback.transform.position = navHit.position;
        }
        else
        {
            fallback.transform.position = idealPos;
        }

        if (team == PlayerMovement.TeamType.Protector) protectorSpawn = fallback.transform;
        else destroyerSpawn = fallback.transform;

        return fallback.transform;
    }
}
