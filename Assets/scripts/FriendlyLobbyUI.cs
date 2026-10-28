using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// واجهة طور الأصدقاء (Friendly Match Lobby)
/// تتيح للاعب إنشاء غرفة أو الانضمام إليها، وتقسيم الفرق (Protectors vs Destroyers) بحرية تامة
/// مع إمكانية بدء المباراة من قبل المضيف
/// </summary>
public class FriendlyLobbyUI : MonoBehaviour
{
    public static FriendlyLobbyUI Instance { get; private set; }

    public bool isLobbyActive = false;
    private string roomCode = "ROOM-7782";
    private string connectIP = "127.0.0.1";
    private bool fillBots = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("FriendlyLobbyUI");
            obj.AddComponent<FriendlyLobbyUI>();
            DontDestroyOnLoad(obj);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // توليد رمز غرفة عشوائي ومميز
        roomCode = "ROOM-" + Random.Range(1000, 9999);
    }

    public void OpenLobby()
    {
        isLobbyActive = true;
    }

    public void CloseLobby()
    {
        isLobbyActive = false;
    }

    public void LeaveLobbyAndReturn()
    {
        CloseLobby();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
        PlayModeUI.SelectedMode = PlayModeUI.PlayMode.None;
        if (PlayModeUI.Instance != null)
        {
            PlayModeUI.Instance.ResetMenu();
        }
    }

    void Update()
    {
        if (isLobbyActive && Input.GetKeyDown(KeyCode.Escape))
        {
            LeaveLobbyAndReturn();
            return;
        }

        // إذا بدأت المباراة يتم إخفاء اللوبي تلقائياً
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())
        {
            isLobbyActive = false;
        }
    }

    void OnGUI()
    {
        if (!isLobbyActive) return;
        if (GameIntroBriefingUI.IsBriefingOpen) return;
        if (PlayerProgressionAndShopUI.IsShopOpen) return;
        if (PlayModeUI.SelectedMode != PlayModeUI.PlayMode.Friendly) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) return;

        Font activeGameFont = EnchantedNatureGUI.GetActiveGameFont();
        if (activeGameFont != null)
        {
            GUI.skin.font = activeGameFont;
        }

        // رسم نافذة اللوبي في منتصف الشاشة
        float winWidth = Mathf.Min(640f, Screen.width - 20f);
        float winHeight = Mathf.Min(480f, Screen.height - 20f);
        float winX = (Screen.width - winWidth) / 2f;
        float winY = (Screen.height - winHeight) / 2f;

        // خلفية داكنة نصف شفافة
        GUI.color = new Color(0.06f, 0.07f, 0.1f, 0.95f);
        GUI.DrawTexture(new Rect(winX, winY, winWidth, winHeight), Texture2D.whiteTexture);

        // إطار علوي مشع
        GUI.color = new Color(0.2f, 0.8f, 1f);
        GUI.DrawTexture(new Rect(winX, winY, winWidth, 3), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Top-left Prominent Back button
        float backW = 135f;
        float backH = 32f;
        string backText = LanguageSelectionUI.Localize(
            "⬅️ BACK (ESC)",
            ar: "⬅️ رجوع (ESC)",
            ru: "⬅️ НАЗАД (ESC)",
            fr: "⬅️ RETOUR (ESC)",
            es: "⬅️ ATRÁS (ESC)",
            de: "⬅️ ZURÜCK (ESC)",
            hi: "⬅️ वापस (ESC)",
            zh: "⬅️ 返回 (ESC)",
            ja: "⬅️ 戻る (ESC)",
            pt: "⬅️ VOLTAR (ESC)"
        );
        GUI.color = new Color(0.9f, 0.25f, 0.25f);
        if (GUI.Button(new Rect(winX + 12, winY + 10, backW, backH), backText))
        {
            LeaveLobbyAndReturn();
            return;
        }
        GUI.color = Color.white;

        // Top-right Close (X) button
        GUI.color = new Color(0.85f, 0.2f, 0.2f);
        if (GUI.Button(new Rect(winX + winWidth - 44, winY + 10, 32f, 32f), "✕"))
        {
            LeaveLobbyAndReturn();
            return;
        }
        GUI.color = Color.white;

        // Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.yellow;

        string lobbyTitle = LanguageSelectionUI.Localize(
            "🤝 PLAY WITH FRIENDS",
            ar: "🤝 اللعب مع الأصدقاء",
            fa: "🤝 بازی با دوستان",
            ru: "🤝 ИГРА С ДРУЗЬЯМИ",
            fr: "🤝 JOUER AVEC DES AMIS",
            es: "🤝 JUGAR CON AMIGOS",
            de: "🤝 MIT FREUNDEN SPIELEN",
            hi: "🤝 दोस्तों के साथ खेलें",
            zh: "🤝 与好友同玩",
            ja: "🤝 友達とプレイ",
            pt: "🤝 JOGAR COM AMIGOS",
            it: "🤝 GIOCA CON GLI AMICI",
            tr: "🤝 ARKADAŞLARLA OYNA"
        );
        GUI.Label(new Rect(winX, winY + 12, winWidth, 28), lobbyTitle, titleStyle);

        // Room code and connection status
        GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.fontSize = 12;
        infoStyle.alignment = TextAnchor.MiddleCenter;
        infoStyle.normal.textColor = Color.white;

        string netStatus = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            ? (NetworkManager.Singleton.IsServer ? "👑 Host (Server)" : "🎮 Connected (Client)")
            : "⚠️ Not Connected";

        GUI.Label(new Rect(winX, winY + 42, winWidth, 20), $"Room Code: {roomCode}  |  Status: {netStatus}", infoStyle);

        // ==========================================
        // Connection Setup (if not listening yet)
        // ==========================================
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            float boxY = winY + 85;
            GUI.Label(new Rect(winX + 40, boxY, winWidth - 80, 24), "Enter Host IP address or host your own room:", infoStyle);

            connectIP = GUI.TextField(new Rect(winX + (winWidth - 200) / 2f, boxY + 30, 200, 30), connectIP);

            if (GUI.Button(new Rect(winX + winWidth / 2f - 145, boxY + 80, 140, 42), "👑 Host Room"))
            {
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.StartHost();
                }
            }

            if (GUI.Button(new Rect(winX + winWidth / 2f + 5, boxY + 80, 140, 42), "🎮 Join Room"))
            {
                if (NetworkManager.Singleton != null)
                {
                    var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                    if (transport != null && !string.IsNullOrEmpty(connectIP))
                    {
                        transport.ConnectionData.Address = connectIP;
                    }
                    NetworkManager.Singleton.StartClient();
                }
            }

            if (GUI.Button(new Rect(winX + (winWidth - 150) / 2f, boxY + 140, 150, 34), "⬅️ Back to Menu"))
            {
                LeaveLobbyAndReturn();
                return;
            }
            return;
        }

        // ==========================================
        // Team Selection (Protectors vs Destroyers)
        // ==========================================
        float colWidth = (winWidth - 50) / 2f;
        float colHeight = winHeight - 165;
        float leftX = winX + 18;
        float rightX = winX + 32 + colWidth;
        float colY = winY + 75;

        // Tree Protectors Column
        DrawTeamBox(leftX, colY, colWidth, colHeight, PlayerMovement.TeamType.Protector, "🛡️ TREE PROTECTORS", new Color(0.15f, 0.6f, 1f));

        // Tree Destroyers Column
        DrawTeamBox(rightX, colY, colWidth, colHeight, PlayerMovement.TeamType.Destroyer, "⚔️ TREE DESTROYERS", new Color(1f, 0.3f, 0.15f));

        // Bottom Controls
        float btmY = winY + winHeight - 55;

        string leaveBtnText = LanguageSelectionUI.Localize(
            "🚪 LEAVE ROOM (ESC)",
            ar: "🚪 مغادرة الغرفة (ESC)",
            ru: "🚪 ВЫЙТИ ИЗ КОМНАТЫ (ESC)",
            fr: "🚪 QUITTER LE SALON (ESC)",
            es: "🚪 SALIR DE LA SALA (ESC)",
            de: "🚪 RAUM VERLASSEN (ESC)",
            hi: "🚪 कमरा छोड़ें (ESC)",
            zh: "🚪 离开房间 (ESC)",
            ja: "🚪 部屋を出る (ESC)",
            pt: "🚪 SAIR DA SALA (ESC)"
        );

        GUI.color = new Color(0.85f, 0.22f, 0.22f);
        if (GUI.Button(new Rect(winX + 18, btmY, 175, 40), leaveBtnText))
        {
            LeaveLobbyAndReturn();
            return;
        }
        GUI.color = Color.white;

        if (NetworkManager.Singleton.IsServer)
        {
            fillBots = GUI.Toggle(new Rect(winX + 205, btmY + 8, 185, 25), fillBots, "🤖 Fill slots with bots");

            GUI.color = new Color(0.2f, 0.9f, 0.3f);
            if (GUI.Button(new Rect(winX + winWidth - 190, btmY, 175, 40), "🚀 START MATCH!"))
            {
                StartFriendlyMatch(fillBots);
            }
            GUI.color = Color.white;
        }
        else
        {
            GUIStyle waitStyle = new GUIStyle(GUI.skin.label);
            waitStyle.alignment = TextAnchor.MiddleCenter;
            waitStyle.fontStyle = FontStyle.Bold;
            waitStyle.fontSize = 13;
            waitStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(winX + 200, btmY + 8, winWidth - 215, 25), "⏳ Waiting for host to start match...", waitStyle);
        }
    }

    void DrawTeamBox(float x, float y, float w, float h, PlayerMovement.TeamType team, string title, Color col)
    {
        // Column background
        GUI.color = new Color(col.r * 0.15f, col.g * 0.15f, col.b * 0.15f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // Border
        GUI.color = col;
        GUI.DrawTexture(new Rect(x, y, w, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.fontSize = 13;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = col;

        GUI.Label(new Rect(x, y + 8, w, 22), title, headerStyle);

        // Switch to team button
        PlayerMovement localPlayer = GetLocalPlayerMovement();
        bool isCurrentTeam = localPlayer != null && localPlayer.team == team;

        GUI.color = isCurrentTeam ? Color.gray : col;
        string btnText = isCurrentTeam ? "✅ CURRENT TEAM" : $"JOIN {title.Replace("🛡️ ", "").Replace("⚔️ ", "")}";
        if (GUI.Button(new Rect(x + 15, y + 36, w - 30, 30), btnText))
        {
            if (localPlayer != null && !isCurrentTeam)
            {
                localPlayer.RequestTeam(team);
                localPlayer.team = team;
                localPlayer.ApplyTeamVisualAura();
            }
        }
        GUI.color = Color.white;

        // Team Members List
        GUIStyle listStyle = new GUIStyle(GUI.skin.label);
        listStyle.fontSize = 11;
        listStyle.normal.textColor = Color.white;

        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        float itemY = y + 78;
        int count = 0;

        foreach (PlayerMovement p in allPlayers)
        {
            if (p == null) continue;
            if (p.team == team)
            {
                count++;
                string tag = p.IsOwner ? " [YOU]" : "";
                string hostTag = (p.IsServer && p.IsOwner) ? " 👑" : "";
                GUI.Label(new Rect(x + 20, itemY, w - 40, 20), $"• {p.gameObject.name}{tag}{hostTag}", listStyle);
                itemY += 22;
            }
        }

        if (count == 0)
        {
            GUIStyle emptyStyle = new GUIStyle(listStyle);
            emptyStyle.normal.textColor = Color.gray;
            emptyStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(x, y + 110, w, 25), "(No players yet)", emptyStyle);
        }
    }

    PlayerMovement GetLocalPlayerMovement()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return null;
        NetworkObject localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        return localObj != null ? localObj.GetComponent<PlayerMovement>() : null;
    }

    void StartFriendlyMatch(bool autoFillBots)
    {
        CloseLobby();

        PlayerMovement localPlayer = GetLocalPlayerMovement();

        // إنشاء البوتات لملء الفرق إن تم اختيار ذلك
        if (autoFillBots && BotManager.Instance != null && localPlayer != null)
        {
            BotManager.Instance.CreateOfflineBots(localPlayer);
        }

        // بدء العد التنازلي والمباراة
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RequestStartGame();
        }
    }
}
