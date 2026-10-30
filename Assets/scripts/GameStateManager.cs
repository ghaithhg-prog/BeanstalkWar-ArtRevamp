using UnityEngine;
using System.Collections;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager _instance;
    public static GameStateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameStateManager>();
                if (_instance == null)
                {
                    GameObject go = GameObject.Find("GameManager");
                    if (go == null) go = new GameObject("GameStateManager");
                    _instance = go.AddComponent<GameStateManager>();
                }
            }
            return _instance;
        }
        set => _instance = value;
    }

    public enum GameState
    {
        WaitingForPlayers,
        Countdown,
        Playing,
        Ended
    }

    public GameState currentState =
        GameState.WaitingForPlayers;

    [Header("Match Start")]
    public float countdownDuration = 3f;

    [Header("UI")]
    public TextMeshProUGUI countdownText;

    private bool countdownStarted = false;
    private string countdownDisplayString = "";
    private float countdownDisplayScale = 1f;

    void Awake()
    {
        _instance = this;

        if (countdownText == null)
        {
            countdownText = FindAnyObjectByType<TextMeshProUGUI>();
        }

        // نخفي النص عند بداية اللعبة
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    [Header("Mutual Climax & Draw Detection")]
    public float TreeDestroyedTime { get; private set; } = -1f;
    public float CrystalConsecratedTime { get; private set; } = -1f;
    public bool IsDrawMatch { get; private set; } = false;
    private float matchEndedTimestamp = -1f;
    private string finalWinner = "";

    public void ResetStateForNewMatch()
    {
        StopAllCoroutines();
        currentState = GameState.WaitingForPlayers;
        countdownStarted = false;
        countdownDisplayString = "";
        TreeDestroyedTime = -1f;
        CrystalConsecratedTime = -1f;
        IsDrawMatch = false;
        matchEndedTimestamp = -1f;
        finalWinner = "";
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    // =========================
    // Request Match Start
    // =========================

    public void RequestStartGame()
    {
        if (currentState != GameState.WaitingForPlayers)
            return;

        if (countdownStarted)
            return;

        // إذا كنا على شبكة (سيرفر/مضيف)، نبث العد التنازلي لجميع الأجهزة والعملاء عبر RPC
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (PlayerMovement p in players)
            {
                if (p != null && p.IsSpawned && p.IsServer)
                {
                    p.StartGameCountdownRpc(countdownDuration);
                    return;
                }
            }
        }

        StartCountdownInternal();
    }

    public void StartCountdownFromNetwork(float duration)
    {
        countdownDuration = duration;
        StartCountdownInternal();
    }

    private void StartCountdownInternal()
    {
        if (currentState != GameState.WaitingForPlayers && currentState != GameState.Countdown)
            return;

        if (countdownStarted)
            return;

        countdownStarted = true;
        StartCoroutine(CountdownCoroutine());
    }

    // =========================
    // Countdown
    // =========================

    IEnumerator CountdownCoroutine()
    {
        currentState =
            GameState.Countdown;

        int count =
            Mathf.CeilToInt(
                countdownDuration
            );

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        while (count > 0)
        {
            countdownDisplayString = count.ToString();
            countdownDisplayScale = 1.35f;

            Debug.Log("🌿 [GameStateManager] MATCH STARTS IN " + count);

            if (countdownText != null)
            {
                countdownText.text = count.ToString();
            }

            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                countdownDisplayScale = Mathf.Lerp(countdownDisplayScale, 1.0f, Time.deltaTime * 5f);
                yield return null;
            }

            count--;
        }

        // =========================
        // GO!
        // =========================

        countdownDisplayString = LanguageSelectionUI.Localize("⚔️ GO! ⚔️", "⚔️ انطلق! ⚔️", "⚔️ В БОЙ! ⚔️", "⚔️ ¡ADELANTE! ⚔️", "⚔️ EN AVANT ! ⚔️");
        countdownDisplayScale = 1.5f;

        Debug.Log("🌿 [GameStateManager] GO!");

        if (countdownText != null)
        {
            countdownText.text = "GO!";
        }

        float goTimer = 0f;
        while (goTimer < 0.75f)
        {
            goTimer += Time.deltaTime;
            countdownDisplayScale = Mathf.Lerp(countdownDisplayScale, 1.0f, Time.deltaTime * 4f);
            yield return null;
        }

        countdownDisplayString = "";
        StartGame();

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    void OnGUI()
    {
        if (currentState != GameState.Countdown || string.IsNullOrEmpty(countdownDisplayString))
            return;

        float boxW = 320f * countdownDisplayScale;
        float boxH = 110f * countdownDisplayScale;
        float x = (Screen.width - boxW) / 2f;
        float y = (Screen.height - boxH) / 2f - 60f;

        Rect bannerRect = new Rect(x, y, boxW, boxH);
        EnchantedNatureGUI.DrawLeafPlaque(bannerRect, new Color(0.08f, 0.16f, 0.08f, 0.94f), new Color(0.95f, 0.82f, 0.35f, 0.98f));

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(44 * countdownDisplayScale),
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = new Color(1f, 0.92f, 0.4f);

        GUI.Label(bannerRect, countdownDisplayString, style);
    }

    // =========================
    // Start Match
    // =========================

    void StartGame()
    {
        if (currentState !=
            GameState.Countdown)
            return;

        currentState =
            GameState.Playing;

        InGameChatManager.Instance?.NotifyMatchStarted();
        TeamVoiceChatManager.Instance?.NotifyMatchStarted();

        Debug.Log(
            "MATCH STARTED!"
        );
    }

    // =========================
    // End Match & Mutual Climax Resolution
    // =========================

    public void NotifyTreeDestroyed()
    {
        if (TreeDestroyedTime > 0f) return;
        TreeDestroyedTime = Time.time;
        Debug.Log($"[GameStateManager] 🌲 Tree Destroyed timestamp recorded at {TreeDestroyedTime:F2}s");

        // If Crystal was already consecrated within 3.5s window
        if (CrystalConsecratedTime > 0f && Mathf.Abs(TreeDestroyedTime - CrystalConsecratedTime) <= 3.5f)
        {
            DeclareDraw();
            return;
        }

        // If game was already marked ended for Protectors within 3.0s, convert to Draw!
        if (currentState == GameState.Ended && (Time.time - matchEndedTimestamp <= 3.0f))
        {
            DeclareDraw();
        }
    }

    public void NotifyCrystalConsecrated()
    {
        if (CrystalConsecratedTime > 0f) return;
        CrystalConsecratedTime = Time.time;
        Debug.Log($"[GameStateManager] 🌟 Crystal Consecrated timestamp recorded at {CrystalConsecratedTime:F2}s");

        // If Tree was already destroyed within 3.5s window, OR Tree is currently dead and collapsing
        bool isTreeDead = (TreeGrowth.Instance != null && TreeGrowth.Instance.IsTreeDead);
        if (TreeDestroyedTime > 0f || isTreeDead)
        {
            DeclareDraw();
            return;
        }

        // If game was already marked ended for Destroyers within 3.0s, convert to Draw!
        if (currentState == GameState.Ended && (Time.time - matchEndedTimestamp <= 3.0f))
        {
            DeclareDraw();
        }
    }

    public void DeclareDraw()
    {
        if (IsDrawMatch) return;
        IsDrawMatch = true;
        finalWinner = "Draw";
        Debug.Log("🕊️ [GameStateManager] MUTUAL CLIMAX! Both Sacred Tree fell and Crystal was consecrated within the time window. DECLARING CELESTIAL PEACE & DRAW!");

        if (currentState == GameState.Ended)
        {
            if (MatchEndStatsUI.Instance != null)
            {
                MatchEndStatsUI.Instance.ConvertToDraw();
            }
        }
        else
        {
            EndGame("Draw");
        }
    }

    public void EndGame(
        string winner)
    {
        if (currentState == GameState.Ended)
        {
            // If match already ended within the last 3.0s, check for mutual tie
            if (!IsDrawMatch && (Time.time - matchEndedTimestamp <= 3.0f))
            {
                if ((winner == "Destroyers" && finalWinner.ToLower().Contains("protector")) ||
                    (winner == "Protectors" && finalWinner.ToLower().Contains("destroyer")) ||
                    winner == "Draw")
                {
                    DeclareDraw();
                }
            }
            return;
        }

        // Check mutual climax conditions before locking
        bool isTreeDead = (TreeGrowth.Instance != null && TreeGrowth.Instance.IsTreeDead);
        if (winner == "Draw" || IsDrawMatch ||
            (winner.ToLower().Contains("protector") && (TreeDestroyedTime > 0f || isTreeDead)) ||
            (winner.ToLower().Contains("destroyer") && (CrystalConsecratedTime > 0f)))
        {
            winner = "Draw";
            IsDrawMatch = true;
        }

        currentState = GameState.Ended;
        matchEndedTimestamp = Time.time;
        finalWinner = winner;

        Debug.Log(
            "Game Ended! Winner: " +
            winner
        );

        // إظهار شاشة النصر والإحصائيات الشاملة فوراً
        if (MatchEndStatsUI.Instance != null)
        {
            MatchEndStatsUI.Instance.ShowVictoryScreen(winner);
        }
        else
        {
            MatchEndStatsUI found = FindFirstObjectByType<MatchEndStatsUI>();
            if (found != null)
            {
                found.ShowVictoryScreen(winner);
            }
            else
            {
                GameObject statsObj = new GameObject("MatchEndStatsUI");
                statsObj.AddComponent<MatchEndStatsUI>().ShowVictoryScreen(winner);
            }
        }

        StartCoroutine(
            EndDelay()
        );
    }

    IEnumerator EndDelay()
    {
        yield return
            new WaitForSeconds(0.6f);

        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

        foreach (
            PlayerMovement player
            in players)
        {
            if (player != null) player.enabled = false;
        }

        BotController[] bots =
            FindObjectsByType<BotController>(
                FindObjectsSortMode.None
            );

        foreach (BotController bot in bots)
        {
            if (bot != null) bot.enabled = false;
        }

        CharacterCombat[] combats =
            FindObjectsByType<CharacterCombat>(
                FindObjectsSortMode.None
            );

        foreach (CharacterCombat cc in combats)
        {
            if (cc != null) cc.enabled = false;
        }

        Debug.Log(
            "Combat, bots, and players disabled cleanly after match end."
        );
    }

    // =========================
    // Helpers
    // =========================

    public bool IsPlaying()
    {
        return currentState ==
               GameState.Playing;
    }

    public bool IsWaiting()
    {
        return currentState ==
               GameState.WaitingForPlayers;
    }
}