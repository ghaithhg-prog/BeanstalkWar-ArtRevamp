using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class BotManager : MonoBehaviour
{
    private static BotManager _instance;
    public static BotManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<BotManager>();
            }
            return _instance;
        }
        set => _instance = value;
    }

    [Header("Bot")]
    public GameObject botPrefab;

    [Header("Characters")]
    public CharacterDatabase characterDatabase;

    private static readonly string[] ProtectorHeroNames = new string[]
    {
        "Eldrin Leafwhisper",
        "Aurelia Sunstrider",
        "Thorin Oakheart",
        "Faelynn Greensong",
        "Kaelen Swiftbreeze",
        "Rowan Barkwarden",
        "Boran Stonebark",
        "Althea Starbloom",
        "Silas Verdantguard",
        "Caelan Bramblethorn"
    };

    private static readonly string[] DestroyerHeroNames = new string[]
    {
        "Vorgar Ironbreaker",
        "Zarek Bloodthorn",
        "Grimlok Ashfall",
        "Valeria Nightshade",
        "Malakor Voidrender",
        "Kragor Blightfang",
        "Darius Doomhammer",
        "Morrigan Cinderveil",
        "Karn Blackblade",
        "Ignis Hellfire"
    };

    private List<string> availableProtectorNames = new List<string>();
    private List<string> availableDestroyerNames = new List<string>();

    private bool offlineBotsCreated = false;

    public void ResetBotManagerState()
    {
        offlineBotsCreated = false;
    }

    void Awake()
    {
        _instance = this;
    }

    // =========================
    // Offline 5v5
    // =========================

    public void CreateOfflineBots(
        PlayerMovement humanPlayer)
    {
        if (offlineBotsCreated)
            return;

        if (humanPlayer == null)
            return;

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
            return;

        if (botPrefab == null)
        {
            botPrefab = Resources.Load<GameObject>("Prefabs/Bot");
        }

        if (characterDatabase == null)
        {
            characterDatabase = Resources.Load<CharacterDatabase>("Characters/CharacterDatabase");
        }

        if (botPrefab == null || characterDatabase == null)
        {
            Debug.LogError("BotManager references are missing.");
            return;
        }

        offlineBotsCreated = true;

        string playerName = PlayerProfile.GetSavedPlayerName();
        availableProtectorNames = new List<string>(ProtectorHeroNames);
        availableProtectorNames.RemoveAll(n => string.Equals(n, playerName, System.StringComparison.OrdinalIgnoreCase));

        availableDestroyerNames = new List<string>(DestroyerHeroNames);
        availableDestroyerNames.RemoveAll(n => string.Equals(n, playerName, System.StringComparison.OrdinalIgnoreCase));

        PlayerMovement.TeamType humanTeam = humanPlayer.hasSelectedTeam.Value
            ? humanPlayer.networkTeam.Value
            : humanPlayer.team;

        PlayerMovement.TeamType enemyTeam = humanTeam == PlayerMovement.TeamType.Protector
            ? PlayerMovement.TeamType.Destroyer
            : PlayerMovement.TeamType.Protector;

        // =========================
        // فريق اللاعب
        // =========================

        List<CharacterData> humanComposition =
            new List<CharacterData>();

        CharacterData humanCharacter =
            characterDatabase.GetCharacter(
                humanPlayer.selectedCharacterID.Value
            );

        if (humanCharacter != null)
        {
            humanComposition.Add(
                humanCharacter
            );
        }

        // اللاعب الحقيقي + 4 Bots بمواقع انتشار تكتيكية فريدة
        for (int i = 0; i < 4; i++)
        {
            CharacterData selected =
                ChooseBestCharacter(
                    humanComposition
                );

            if (selected == null)
                break;

            humanComposition.Add(
                selected
            );

            SpawnBot(
                humanTeam,
                selected,
                i + 1
            );
        }

        // =========================
        // الفريق الخصم
        // =========================

        List<CharacterData> enemyComposition =
            new List<CharacterData>();

        // 5 Bots بمواقع انتشار تكتيكية فريدة
        for (int i = 0; i < 5; i++)
        {
            CharacterData selected =
                ChooseBestCharacter(
                    enemyComposition
                );

            if (selected == null)
                break;

            enemyComposition.Add(
                selected
            );

            SpawnBot(
                enemyTeam,
                selected,
                i
            );
        }

        // =========================
        // Debug
        // =========================

        Debug.Log(
            "Offline smart 5v5 teams created."
        );

        PrintComposition(
            "Human Team",
            humanComposition
        );

        PrintComposition(
            "Enemy Team",
            enemyComposition
        );

        // =========================
        // Start Match Countdown
        // =========================

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .RequestStartGame();
        }
        else
        {
            Debug.LogError(
                "GameStateManager Instance not found."
            );
        }
    }

    // =========================
    // Smart Character Selection
    // =========================

    CharacterData ChooseBestCharacter(
        List<CharacterData> currentTeam)
    {
        // أولاً:
        // نضمن Tank + Support + Control

        CharacterData.CharacterRole[] requiredRoles =
        {
            CharacterData.CharacterRole.Tank,
            CharacterData.CharacterRole.Support,
            CharacterData.CharacterRole.Control
        };

        foreach (
            CharacterData.CharacterRole role
            in requiredRoles)
        {
            if (!HasRole(
                    currentTeam,
                    role))
            {
                CharacterData candidate =
                    FindAvailableCharacter(
                        role,
                        currentTeam
                    );

                if (candidate != null)
                    return candidate;
            }
        }

        // =========================
        // بعد اكتمال الأدوار الأساسية
        // =========================

        // نعطي الأولوية للهجوم
        // حتى لا تتكرر Tanks وSupports
        // بلا حاجة.

        CharacterData.CharacterRole[] fillPriorities =
        {
            CharacterData.CharacterRole.Damage,
            CharacterData.CharacterRole.Specialist,
            CharacterData.CharacterRole.Control,
            CharacterData.CharacterRole.Support,
            CharacterData.CharacterRole.Tank
        };

        foreach (
            CharacterData.CharacterRole role
            in fillPriorities)
        {
            CharacterData candidate =
                FindAvailableCharacter(
                    role,
                    currentTeam
                );

            if (candidate != null)
                return candidate;
        }

        return null;
    }

    // =========================
    // Has Role
    // =========================

    bool HasRole(
        List<CharacterData> team,
        CharacterData.CharacterRole role)
    {
        foreach (
            CharacterData character
            in team)
        {
            if (character != null &&
                character.role == role)
            {
                return true;
            }
        }

        return false;
    }

    // =========================
    // Find Available Character
    // =========================

    CharacterData FindAvailableCharacter(
        CharacterData.CharacterRole role,
        List<CharacterData> currentTeam)
    {
        List<CharacterData> candidates =
            new List<CharacterData>();

        foreach (
            CharacterData character
            in characterDatabase.characters)
        {
            if (character == null)
                continue;

            if (character.role != role)
                continue;

            int count =
                CountCharacter(
                    currentTeam,
                    character.characterID
                );

            // احترام Max Per Team
            if (count >=
                character.maxPerTeam)
            {
                continue;
            }

            candidates.Add(
                character
            );
        }

        if (candidates.Count == 0)
            return null;

        // اختيار عشوائي من الشخصيات
        // المتاحة داخل الدور المطلوب.

        return candidates[
            Random.Range(
                0,
                candidates.Count
            )
        ];
    }

    // =========================
    // Count Character
    // =========================

    int CountCharacter(
        List<CharacterData> team,
        int characterID)
    {
        int count = 0;

        foreach (
            CharacterData character
            in team)
        {
            if (character != null &&
                character.characterID ==
                characterID)
            {
                count++;
            }
        }

        return count;
    }

    // =========================
    // Spawn Bot
    // =========================

    void SpawnBot(
        PlayerMovement.TeamType team,
        CharacterData character,
        int botIndex = -1)
    {
        Vector3 position = (SpawnManager.Instance != null)
            ? SpawnManager.Instance.GetSpawnPosition(team, botIndex)
            : (team == PlayerMovement.TeamType.Protector ? new Vector3(-15f, 0.5f, 0f) : new Vector3(15f, 0.5f, 0f));

        Quaternion rotation = (team == PlayerMovement.TeamType.Protector)
            ? Quaternion.LookRotation(Vector3.right)
            : Quaternion.LookRotation(Vector3.left);

        GameObject botObject =
            Instantiate(
                botPrefab,
                position,
                rotation
            );

        string botHeroName = "";
        if (team == PlayerMovement.TeamType.Protector)
        {
            if (availableProtectorNames.Count > 0)
            {
                int rIdx = Random.Range(0, availableProtectorNames.Count);
                botHeroName = availableProtectorNames[rIdx];
                availableProtectorNames.RemoveAt(rIdx);
            }
            else botHeroName = $"Protector Guardian #{botIndex + 1}";
        }
        else
        {
            if (availableDestroyerNames.Count > 0)
            {
                int rIdx = Random.Range(0, availableDestroyerNames.Count);
                botHeroName = availableDestroyerNames[rIdx];
                availableDestroyerNames.RemoveAt(rIdx);
            }
            else botHeroName = $"Destroyer Raider #{botIndex + 1}";
        }

        botObject.name = botHeroName;

        // =========================
        // Bot Controller
        // =========================

        BotController controller =
            botObject.GetComponent<BotController>();

        if (controller != null)
        {
            controller.team =
                team;

            controller.botHeroName =
                botHeroName;

            if (character != null)
            {
                controller.moveSpeed = character.moveSpeed;
            }
        }

        UnityEngine.AI.NavMeshAgent agent =
            botObject.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (agent != null)
        {
            agent.baseOffset = 0.95f;
            agent.enabled = true;
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, 6f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            if (character != null)
            {
                agent.speed = character.moveSpeed;
            }
        }

        // =========================
        // Visuals: Full AAA 3D Medieval Model & Team Aura
        // =========================
        PlayerMovement.Attach3DModel(botObject, character, character.characterID, team);

        // =========================
        // Combat
        // =========================

        CharacterCombat combat =
            botObject.GetComponent<CharacterCombat>();

        if (combat != null)
        {
            combat.team =
                team;

            combat.characterData =
                character;

            combat.attackDamage =
                character.attackDamage;

            combat.attackCooldown =
                character.attackCooldown;

            combat.attackRange =
                character.attackRange;

            combat.attackType =
                character.attackType;
        }

        // =========================
        // Health
        // =========================

        Health health =
            botObject.GetComponent<Health>();

        if (health != null)
        {
            health.maxHealth =
                character.maxHealth;

            health.currentHealth =
                character.maxHealth;
        }

        // =========================
        // Network
        // =========================

        NetworkObject networkObject =
            botObject.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn();
        }
        else
        {
            Debug.LogError(
                "Bot prefab has no NetworkObject."
            );
        }

        // =========================
        // Debug Bot
        // =========================

        Debug.Log(
            "Spawned Bot | Team: " +
            team +
            " | Character: " +
            character.characterName +
            " | Role: " +
            character.role
        );
    }

    // =========================
    // Debug Composition
    // =========================

    void PrintComposition(
        string title,
        List<CharacterData> team)
    {
        string result =
            title + ": ";

        foreach (
            CharacterData character
            in team)
        {
            result +=
                "[" +
                character.characterName +
                " - " +
                character.role +
                "] ";
        }

        Debug.Log(result);
    }
}