using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Match End Stats UI & Victory System:
/// Handles the grand match conclusion when Protectors deliver the Crystal or Destroyers chop down the tree:
/// 1. Tracks match-wide statistics for all players & bots (Kills, Deaths, Damage, Bosses, Crystal)
/// 2. Displays an ancient enchanted parchment victory screen with full hero statistics
/// 3. Awards bonus account XP (+500) and Acorn Coins (+250) to the player profile
/// 4. Provides options to Play Again or Return to Main Menu
/// </summary>
public class MatchEndStatsUI : MonoBehaviour
{
    public static MatchEndStatsUI Instance { get; private set; }

    public static bool isAutoRestartingMatch = false;
    public static int savedCharacterID = 0;
    public static PlayerMovement.TeamType savedTeam = PlayerMovement.TeamType.Protector;

    public class PlayerMatchStat
    {
        public int entityId;
        public string heroName = "Champion";
        public string characterClass = "";
        public PlayerMovement.TeamType team;
        public int kills = 0;
        public int deaths = 0;
        public float damageDealt = 0f;
        public int bossesKilled = 0;
        public bool deliveredCrystal = false;
        public bool isLocalPlayer = false;

        public int CalculateScore()
        {
            return (kills * 120) + (int)(damageDealt * 0.5f) + (bossesKilled * 400) + (deliveredCrystal ? 800 : 0);
        }
    }

    private Dictionary<int, PlayerMatchStat> stats = new Dictionary<int, PlayerMatchStat>();
    private bool isGameOver = false;
    private string winningTeamName = "Protectors";
    private float gameOverTime = 0f;
    private Vector2 scrollPos = Vector2.zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (FindFirstObjectByType<MatchEndStatsUI>() == null)
        {
            GameObject obj = new GameObject("MatchEndStatsUI");
            obj.AddComponent<MatchEndStatsUI>();
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void RegisterDamage(GameObject attacker, float amount)
    {
        if (attacker == null || amount <= 0f) return;
        PlayerMatchStat stat = GetOrCreateStat(attacker);
        if (stat != null) stat.damageDealt += amount;
    }

    public void RegisterKill(GameObject killer, GameObject victim)
    {
        if (victim != null)
        {
            PlayerMatchStat victimStat = GetOrCreateStat(victim);
            if (victimStat != null) victimStat.deaths++;
        }

        if (killer != null)
        {
            PlayerMatchStat killerStat = GetOrCreateStat(killer);
            if (killerStat != null) killerStat.kills++;
        }
    }

    public void RegisterBossKill(GameObject killer)
    {
        if (killer != null)
        {
            PlayerMatchStat stat = GetOrCreateStat(killer);
            if (stat != null) stat.bossesKilled++;
        }
    }

    public void RegisterCrystalDelivered(GameObject deliverer)
    {
        if (deliverer != null)
        {
            PlayerMatchStat stat = GetOrCreateStat(deliverer);
            if (stat != null) stat.deliveredCrystal = true;
        }
    }

    PlayerMatchStat GetOrCreateStat(GameObject go)
    {
        if (go == null) return null;
        int id = go.GetInstanceID();
        if (!stats.TryGetValue(id, out PlayerMatchStat s))
        {
            s = new PlayerMatchStat();
            s.entityId = id;

            PlayerMovement pm = go.GetComponent<PlayerMovement>();
            BotController bot = go.GetComponent<BotController>();
            CharacterCombat combat = go.GetComponent<CharacterCombat>();

            if (pm != null)
            {
                s.team = pm.team;
                s.isLocalPlayer = pm.IsOwner;
                s.heroName = pm.IsOwner ? PlayerProfile.GetSavedPlayerName() : go.name.Replace("(Clone)", "").Trim();

                int charId = pm.selectedCharacterID.Value;
                CharacterDatabase db = Resources.Load<CharacterDatabase>("Characters/CharacterDatabase");
                CharacterData cd = (charId >= 0 && db != null) ? db.GetCharacter(charId) : (combat != null ? combat.characterData : null);
                s.characterClass = cd != null ? cd.characterName : "Hero";
            }
            else if (bot != null)
            {
                s.team = bot.team;
                s.isLocalPlayer = false;
                s.heroName = !string.IsNullOrEmpty(bot.botHeroName) ? bot.botHeroName : go.name.Replace("(Clone)", "").Trim();

                CharacterData cd = combat != null ? combat.characterData : null;
                s.characterClass = cd != null ? cd.characterName : "Champion";
            }
            else
            {
                s.team = combat != null ? combat.team : PlayerMovement.TeamType.Protector;
                s.heroName = go.name.Replace("(Clone)", "").Trim();
                s.characterClass = "Warrior";
            }

            stats[id] = s;
        }
        return s;
    }

    public void ShowVictoryScreen(string winner)
    {
        if (isGameOver) return;
        isGameOver = true;
        winningTeamName = winner;
        gameOverTime = Time.time;

        // Populate all 10 players and bots into stats table
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            if (p != null) GetOrCreateStat(p.gameObject);
        }

        BotController[] allBots = FindObjectsByType<BotController>(FindObjectsSortMode.None);
        foreach (var b in allBots)
        {
            if (b != null) GetOrCreateStat(b.gameObject);
        }

        // Award match victory bonuses to local player profile
        if (PlayerProfile.Instance != null)
        {
            bool isProtectorWin = winner.ToLower().Contains("protector");
            int xpReward = isProtectorWin ? 500 : 250;
            int coinReward = isProtectorWin ? 250 : 120;
            PlayerProfile.Instance.AddCoins(coinReward);
            PlayerProfile.Instance.AddAccountXP(xpReward);
        }

        Debug.Log($"🏆 [MatchEndStatsUI] Victory sequence active for: {winner}!");
    }

    void OnGUI()
    {
        if (!isGameOver) return;

        // Dim background overlay
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float boxWidth = Mathf.Min(Screen.width * 0.94f, 760f);
        float boxHeight = Mathf.Min(Screen.height * 0.90f, 590f);
        float startX = (Screen.width - boxWidth) / 2f;
        float startY = (Screen.height - boxHeight) / 2f;

        // 1. Ancient Parchment Background Frame
        Rect mainRect = new Rect(startX, startY, boxWidth, boxHeight);
        EnchantedNatureGUI.DrawLeafPlaque(mainRect, new Color(0.12f, 0.18f, 0.12f, 0.95f), new Color(0.85f, 0.72f, 0.32f, 0.9f));

        // 2. Victory Header Banner with 10-Language Support
        bool isProt = winningTeamName.ToLower().Contains("protector");
        string titleText = isProt
            ? LanguageSelectionUI.Localize(
                "🌟 THE SACRED CRYSTAL REACHED THE SANCTUARY! PROTECTORS WIN! 🌟",
                "🌟 لقد وصلت الكريستالة للمذبح، فاز الحماة! 🌟",
                "🌟 КРИСТАЛЛ ДОСТИГ АЛТАРЯ! ЗАЩИТНИКИ ПОБЕДИЛИ! 🌟",
                "🌟 ¡EL CRISTAL HA LLEGADO AL ALTAR, GANARON LOS PROTECTORES! 🌟",
                "🌟 LE CRISTAL A ATTEINT L'AUTEL, LES PROTECTEURS ONT GAGNÉ ! 🌟",
                "🌟 DER KRISTALL HAT DEN ALTAR ERREICHT, DIE BESCHÜTZER HABEN GEWONNEN! 🌟",
                "🌟 क्रिस्टल वेदी पर पहुंच गया, संरक्षकों की विजय हुई! 🌟",
                "🌟 水晶已送达圣坛，守护者获胜！🌟",
                "🌟 水晶が祭壇に到達した、守護者の勝利！🌟",
                "🌟 O CRISTAL CHEGOU AO ALTAR, OS PROTETORES VENCERAM! 🌟")
            : LanguageSelectionUI.Localize(
                "🔥 THE GREAT BEANSTALK HAS FALLEN! 🔥",
                "🔥 سقطت شجرة الفاصولياء العظيمة! 🔥",
                "🔥 ВЕЛИКИЙ СТЕБЕЛЬ ПАЛ! 🔥",
                "🔥 ¡EL GRAN TALLO HA CAÍDO! 🔥",
                "🔥 LE GRAND HARICOT EST TOMBÉ ! 🔥",
                "🔥 DIE GROSSE BOHNENRANKE IST GEFALLEN! 🔥",
                "🔥 विशाल सेम का पेड़ गिर गया है! 🔥",
                "🔥 巨大的魔豆藤倒下了！🔥",
                "🔥 巨大な豆の木が倒れた！🔥",
                "🔥 O GRANDE PÉ DE FEIJÃO CAIU! 🔥");

        string subtitleText = isProt
            ? LanguageSelectionUI.Localize(
                "🌿 THE SACRED GROVE IS SAVED! 🌿",
                "🌿 تم إنقاذ الغابة المقدسة بسلام! 🌿",
                "🌿 СВЯЩЕННАЯ РОЩА СПАСЕНА! 🌿",
                "🌿 ¡EL BOSQUE SAGRADO ESTÁ SALVADO! 🌿",
                "🌿 LE BOSQUET SACRÉ EST SAUVÉ ! 🌿",
                "🌿 DER HEILIGE HAIN IST GERETTET! 🌿",
                "🌿 पवित्र उपवन बच गया! 🌿",
                "🌿 神圣森林得救了！🌿",
                "🌿 聖なる森は救われた！🌿",
                "🌿 O BOSQUE SAGRADO FOI SALVO! 🌿")
            : LanguageSelectionUI.Localize(
                "⚔️ DESTROYERS VICTORIOUS! ⚔️",
                "⚔️ انتصار فريق المدمرين! ⚔️",
                "⚔️ ПОБЕДА РАЗРУШИТЕЛЕЙ! ⚔️",
                "⚔️ ¡VICTORIA DE LOS DESTRUCTORES! ⚔️",
                "⚔️ VICTOIRE DES DESTRUCTEURS ! ⚔️",
                "⚔️ DIE ZERSTÖRER HABEN GESIEGT! ⚔️",
                "⚔️ विनाशकों की विजय! ⚔️",
                "⚔️ 破坏者获胜！⚔️",
                "⚔️ 破壊者の勝利！⚔️",
                "⚔️ VITÓRIA DOS DESTRUIDORES! ⚔️");

        Color bannerColor = isProt ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.35f, 0.2f);

        Rect titleRect = new Rect(startX + 20f, startY + 20f, boxWidth - 40f, 32f);
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = bannerColor;
        GUI.Label(titleRect, titleText, titleStyle);

        Rect subRect = new Rect(startX + 20f, startY + 52f, boxWidth - 40f, 28f);
        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };
        subStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);
        GUI.Label(subRect, subtitleText, subStyle);

        // Match Reward Note with 10-Language Support
        Rect rewardRect = new Rect(startX + 20f, startY + 82f, boxWidth - 40f, 22f);
        GUIStyle rewardStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Italic
        };
        rewardStyle.normal.textColor = new Color(0.8f, 1f, 0.8f);
        GUI.Label(rewardRect, LanguageSelectionUI.Localize(
            "✨ Rewards Granted: +500 Account XP & +250 Acorn Coins! ✨",
            "✨ المكافآت الممنوحة: +500 خبرة الحساب و +250 عملة بلوط! ✨",
            "✨ Награды: +500 Опыта аккаунта и +250 Желудей! ✨",
            "✨ Recompensas: +500 EXP de cuenta y +250 Monedas Bellota! ✨",
            "✨ Récompenses : +500 EXP de compte & +250 Pièces Gland ! ✨",
            "✨ Belohnungen: +500 Konto-EP & +250 Eichel-Münzen! ✨",
            "✨ पुरस्कार दिए गए: +500 खाता अनुभव और +250 बलूत के सिक्के! ✨",
            "✨ 获得奖励：+500 账号经验值与 +250 橡果金币！✨",
            "✨ 獲得報酬：+500 アカウント経験値 ＆ +250 ドングリコイン！✨",
            "✨ Recompensas: +500 XP de conta & +250 Moedas de Bolota! ✨"), rewardStyle);

        // 3. Stats Table Header
        float tableY = startY + 112f;
        Rect headerRect = new Rect(startX + 20f, tableY, boxWidth - 40f, 28f);
        EnchantedNatureGUI.DrawWoodPlate(headerRect, new Color(0.22f, 0.16f, 0.10f, 0.95f), new Color(0.65f, 0.50f, 0.25f, 0.8f));

        GUIStyle colHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        colHeaderStyle.normal.textColor = new Color(1f, 0.9f, 0.6f);

        float colX = headerRect.x + 8f;
        GUI.Label(new Rect(colX, tableY + 4f, 175f, 20f), LanguageSelectionUI.Localize("HERO & CLASS", "البطل والفئة", "ГЕРОЙ И КЛАСС", "HÉROE Y CLASE", "HÉROS & CLASSE", "HELD & KLASSE", "नायक और वर्ग", "英雄与职业", "英雄とクラス", "HERÓI E CLASSE"), colHeaderStyle);
        colX += 180f;
        GUI.Label(new Rect(colX, tableY + 4f, 80f, 20f), LanguageSelectionUI.Localize("TEAM", "الفريق", "КОМАНДА", "EQUIPO", "ÉQUIPE", "TEAM", "दल", "队伍", "チーム", "EQUIPE"), colHeaderStyle);
        colX += 85f;
        GUI.Label(new Rect(colX, tableY + 4f, 45f, 20f), LanguageSelectionUI.Localize("KILLS", "قتلى", "УБИЙСТВА", "BAJAS", "ÉLIMINATIONS", "KILLS", "हताहत", "击杀", "キル", "ABATES"), colHeaderStyle);
        colX += 48f;
        GUI.Label(new Rect(colX, tableY + 4f, 45f, 20f), LanguageSelectionUI.Localize("DEATHS", "وفيات", "СМЕРТИ", "MUERTES", "MORTS", "TODE", "मृत्यु", "死亡", "デス", "MORTES"), colHeaderStyle);
        colX += 48f;
        GUI.Label(new Rect(colX, tableY + 4f, 65f, 20f), LanguageSelectionUI.Localize("DAMAGE", "ضرر", "УРОН", "DAÑO", "DÉGÂTS", "SCHADEN", "क्षति", "伤害", "ダメージ", "DANO"), colHeaderStyle);
        colX += 68f;
        GUI.Label(new Rect(colX, tableY + 4f, 45f, 20f), LanguageSelectionUI.Localize("BOSS", "زعيم", "БОСС", "JEFE", "BOSS", "BOSS", "बॉس", "首领", "ボス", "CHEFE"), colHeaderStyle);
        colX += 48f;
        GUI.Label(new Rect(colX, tableY + 4f, 58f, 20f), LanguageSelectionUI.Localize("CRYSTAL", "كريستال", "КРИСТАЛЛ", "CRISTAL", "CRISTAL", "KRISTALL", "क्रिस्टल", "水晶", "クリスタル", "CRISTAL"), colHeaderStyle);
        colX += 62f;
        GUI.Label(new Rect(colX, tableY + 4f, 58f, 20f), LanguageSelectionUI.Localize("SCORE", "نقاط", "СЧЕТ", "PUNTOS", "SCORE", "PUNKTE", "स्कोर", "积分", "スコア", "PONTUAÇÃO"), colHeaderStyle);

        // 4. Scrollable Stats Rows
        float listY = tableY + 32f;
        float listHeight = boxHeight - 210f;
        Rect listRect = new Rect(startX + 20f, listY, boxWidth - 40f, listHeight);

        // Sort stats by score
        List<PlayerMatchStat> sortedList = new List<PlayerMatchStat>(stats.Values);
        sortedList.Sort((a, b) => b.CalculateScore().CompareTo(a.CalculateScore()));

        float rowHeight = 32f;
        float totalContentHeight = Mathf.Max(sortedList.Count * rowHeight, listHeight);

        scrollPos = GUI.BeginScrollView(listRect, scrollPos, new Rect(0, 0, listRect.width - 20f, totalContentHeight));

        for (int i = 0; i < sortedList.Count; i++)
        {
            PlayerMatchStat s = sortedList[i];
            float currRowY = i * rowHeight;
            Rect rowRect = new Rect(0, currRowY, listRect.width - 20f, 28f);

            Color rowBg = (i % 2 == 0) ? new Color(0.15f, 0.22f, 0.16f, 0.75f) : new Color(0.12f, 0.17f, 0.13f, 0.75f);
            if (s.isLocalPlayer) rowBg = new Color(0.25f, 0.38f, 0.22f, 0.9f); // Highlight player row

            Color borderCol = s.isLocalPlayer ? new Color(1f, 0.85f, 0.2f) : (i == 0 ? new Color(1f, 0.75f, 0.1f) : new Color(0.35f, 0.45f, 0.35f, 0.4f));
            EnchantedNatureGUI.DrawWoodPlate(rowRect, rowBg, borderCol);

            GUIStyle rowTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = s.isLocalPlayer ? FontStyle.Bold : FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            rowTextStyle.normal.textColor = s.isLocalPlayer ? Color.white : new Color(0.9f, 0.9f, 0.9f);

            float rX = 6f;
            string prefix = (i == 0) ? "👑 " : (s.isLocalPlayer ? "⭐ " : "⚔️ ");
            string heroDisplay = $"{prefix}{s.heroName}";
            if (!string.IsNullOrEmpty(s.characterClass))
            {
                heroDisplay += $" ({s.characterClass})";
            }
            GUI.Label(new Rect(rX, currRowY + 4f, 175f, 20f), heroDisplay, rowTextStyle);
            rX += 180f;

            Color tCol = (s.team == PlayerMovement.TeamType.Protector) ? new Color(0.3f, 0.85f, 1f) : new Color(1f, 0.4f, 0.2f);
            GUIStyle teamStyle = new GUIStyle(rowTextStyle);
            teamStyle.normal.textColor = tCol;
            string teamStr = (s.team == PlayerMovement.TeamType.Protector)
                ? LanguageSelectionUI.Localize("Protector", "حامي", "Защитник", "Protector", "Protecteur", "Beschützer", "संरक्षक", "守护者", "守護者", "Protetor")
                : LanguageSelectionUI.Localize("Destroyer", "مدمر", "Разрушитель", "Destructor", "Destructeur", "Zerstörer", "विनाशक", "破坏者", "破壊者", "Destruidor");
            GUI.Label(new Rect(rX, currRowY + 4f, 80f, 20f), teamStr, teamStyle);
            rX += 85f;

            GUI.Label(new Rect(rX, currRowY + 4f, 45f, 20f), s.kills.ToString(), rowTextStyle);
            rX += 48f;
            GUI.Label(new Rect(rX, currRowY + 4f, 45f, 20f), s.deaths.ToString(), rowTextStyle);
            rX += 48f;
            GUI.Label(new Rect(rX, currRowY + 4f, 65f, 20f), ((int)s.damageDealt).ToString(), rowTextStyle);
            rX += 68f;
            GUI.Label(new Rect(rX, currRowY + 4f, 45f, 20f), s.bossesKilled > 0 ? $"⚔️{s.bossesKilled}" : "-", rowTextStyle);
            rX += 48f;

            string crystalStr = s.deliveredCrystal
                ? LanguageSelectionUI.Localize("⭐ YES", "⭐ نعم", "⭐ ДА", "⭐ SÍ", "⭐ OUI", "⭐ JA", "⭐ हाँ", "⭐ 是", "⭐ 完了", "⭐ SIM")
                : "-";
            GUI.Label(new Rect(rX, currRowY + 4f, 58f, 20f), crystalStr, rowTextStyle);
            rX += 62f;

            GUIStyle scoreStyle = new GUIStyle(rowTextStyle);
            scoreStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);
            GUI.Label(new Rect(rX, currRowY + 4f, 58f, 20f), s.CalculateScore().ToString(), scoreStyle);
        }

        GUI.EndScrollView();

        // 5. Action Buttons at the Bottom
        float btnY = boxHeight + startY - 55f;
        float btnWidth = 200f;
        float btnHeight = 40f;

        Rect playAgainRect = new Rect(startX + (boxWidth / 2f) - btnWidth - 15f, btnY, btnWidth, btnHeight);
        if (EnchantedNatureGUI.DrawWoodButton(playAgainRect, LanguageSelectionUI.Localize(
            "🌿 BATTLE AGAIN",
            "🌿 قتال من جديد",
            "🌿 СНОВА В БОЙ",
            "🌿 LUCHAR DE NUEVO",
            "🌿 COMBATTRE À NOUVEAU",
            "🌿 ERNEUT KÄMPFEN",
            "🌿 फिर से लड़ें",
            "🌿 再战一场",
            "🌿 再戦する",
            "🌿 LUTAR NOVAMENTE")))
        {
            RestartMatch(true);
        }

        Rect menuRect = new Rect(startX + (boxWidth / 2f) + 15f, btnY, btnWidth, btnHeight);
        if (EnchantedNatureGUI.DrawWoodButton(menuRect, LanguageSelectionUI.Localize(
            "🏰 RETURN TO GROVE",
            "🏰 العودة إلى الغابة",
            "🏰 ВЕРНУТЬСЯ В РОЩУ",
            "🏰 VOLVER A LA ARBOLEDA",
            "🏰 RETOUR AU BOSQUET",
            "🏰 ZURÜCK ZUM HAIN",
            "🏰 उपवन पर लौटें",
            "🏰 返回神殿",
            "🏰 聖域へ戻る",
            "🏰 VOLTAR AO BOSQUE")))
        {
            RestartMatch(false);
        }
    }

    public static void ExecuteCleanGameRestart()
    {
        Debug.Log("🔄 [MatchEndStatsUI] Executing pristine 100% clean-slate game restart...");

        // 1. Reset match end flags & saved replay data
        isAutoRestartingMatch = false;
        savedCharacterID = 0;
        savedTeam = PlayerMovement.TeamType.Protector;

        if (Instance != null)
        {
            Instance.isGameOver = false;
            Instance.stats.Clear();
            Instance.winningTeamName = "";
        }

        // 2. Reset PlayModeUI state & show pristine mode selection
        PlayModeUI.SelectedMode = PlayModeUI.PlayMode.None;
        if (PlayModeUI.Instance != null)
        {
            PlayModeUI.Instance.ResetMenu();
        }

        // 3. Reset GameStateManager
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetStateForNewMatch();
        }

        // 4. Reset static singletons & game objects
        CrystalAltar.ResetStaticState();
        TreeGrowth.ResetStaticState();
        LevelBossManager.ClearStaticEvents();

        if (LevelBossManager.Instance != null)
        {
            LevelBossManager.Instance.ResetBossManagerState();
        }

        if (BotManager.Instance != null)
        {
            BotManager.Instance.ResetBotManagerState();
        }

        // 5. Close all active modal dialogs
        if (InGamePauseMenuUI.Instance != null)
        {
            InGamePauseMenuUI.Instance.ClosePauseMenu();
        }
        if (GameIntroBriefingUI.Instance != null)
        {
            GameIntroBriefingUI.Instance.CloseBriefing();
        }
        if (PlayerProgressionAndShopUI.Instance != null)
        {
            PlayerProgressionAndShopUI.Instance.CloseShop();
        }
        if (CharacterSelectionUI.Instance != null)
        {
            CharacterSelectionUI.Instance.Hide();
        }

        // 6. Safely shut down Netcode without destroying the GameObject (Unity will reload the scene's NetworkManager cleanly)
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        // 7. Load scene freshly
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void RestartMatch(bool autoRestart = false)
    {
        ExecuteCleanGameRestart();
    }
}
