using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Match End Stats UI & Victory System:
/// Handles the grand match conclusion when Protectors deliver the Crystal or Destroyers chop down the tree:
/// 1. Tracks match-wide statistics for all players & bots (Kills, Deaths, Damage, Bosses, Crystal)
/// 2. Displays an ancient enchanted parchment victory screen with full hero statistics
/// 3. Awards bonus account XP (+500), Acorn Coins (+250), and Rating Score for Leaderboards & 10 Ranked Tiers
/// 4. Provides options to Play Again, Return to Grove, or Add Opponents/Teammates as Friends directly (+👤)
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
    public static bool IsGameOverActive => Instance != null && Instance.isGameOver;
    private string winningTeamName = "Protectors";
    private float gameOverTime = 0f;
    private Vector2 scrollPos = Vector2.zero;
    private bool isLocalPlayerWinner = false;
    private int awardedRatingScore = 0;

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

        PlayerMovement pm = go.GetComponent<PlayerMovement>();
        BotController bot = go.GetComponent<BotController>();

        // STRICT FILTER: Bosses and non-hero world entities MUST NEVER appear on player scoreboard!
        if (pm == null && bot == null) return null;

        int id = go.GetInstanceID();
        if (!stats.TryGetValue(id, out PlayerMatchStat s))
        {
            s = new PlayerMatchStat();
            s.entityId = id;
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

        // Determine if local player won or if match is a peaceful Draw
        bool isDraw = winner.ToLower().Contains("draw") || winner.ToLower().Contains("tie") || winner.ToLower().Contains("peace");
        bool isProtWin = winner.ToLower().Contains("protector");
        PlayerMovement.TeamType winningTeam = isProtWin ? PlayerMovement.TeamType.Protector : PlayerMovement.TeamType.Destroyer;

        if (isDraw)
        {
            isLocalPlayerWinner = true; // Both teams receive positive peaceful rewards!
        }
        else
        {
            isLocalPlayerWinner = false;
            foreach (var stat in stats.Values)
            {
                if (stat != null && stat.isLocalPlayer)
                {
                    isLocalPlayerWinner = (stat.team == winningTeam);
                    break;
                }
            }

            // Fallback search directly on player gameobjects if not found in dictionary
            if (!isLocalPlayerWinner)
            {
                foreach (var p in allPlayers)
                {
                    if (p != null && p.IsOwner && p.team == winningTeam)
                    {
                        isLocalPlayerWinner = true;
                        break;
                    }
                }
            }
        }

        // Award match victory bounties or deduct match stake
        if (PlayerProfile.Instance != null)
        {
            if (isDraw)
            {
                PlayerProfile.Instance.AddCoins(250, "Mutual Peace & Harmony Bounty");
                PlayerProfile.Instance.AddAccountXP(350);
            }
            else if (isLocalPlayerWinner)
            {
                PlayerProfile.Instance.AddCoins(350, "Match Victory Bounty");
                PlayerProfile.Instance.AddAccountXP(500);
            }
            else
            {
                PlayerProfile.Instance.DeductCoins(60, "Match Defeat Stake");
                PlayerProfile.Instance.AddAccountXP(100);
            }

            // Award Rating Score for Leaderboards & 10 Ranked Tiers
            PlayerMatchStat localStat = null;
            foreach (var st in stats.Values)
            {
                if (st != null && st.isLocalPlayer) { localStat = st; break; }
            }
            int matchScore = localStat != null ? localStat.CalculateScore() : (isDraw ? 300 : (isLocalPlayerWinner ? 400 : 150));
            bool isOnline = (PlayModeUI.SelectedMode == PlayModeUI.PlayMode.Online || PlayModeUI.SelectedMode == PlayModeUI.PlayMode.Friendly);
            awardedRatingScore = PlayerProfile.Instance.AddRatingScore(matchScore, isOnline);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log($"🏆 [MatchEndStatsUI] Victory sequence active for: {winner}! IsDraw: {isDraw} | Local player won: {isLocalPlayerWinner} | Rating Award: +{awardedRatingScore}");
    }

    /// <summary>
    /// Dynamically converts an active single-winner end screen to a mutual Draw & Peace condition
    /// when the opposing team completes their objective within the simultaneous climax window.
    /// </summary>
    public void ConvertToDraw()
    {
        if (winningTeamName == "Draw") return;
        Debug.Log("🕊️ [MatchEndStatsUI] Converting active victory screen to CELESTIAL PEACE & DRAW!");

        if (PlayerProfile.Instance != null)
        {
            // If player was defeated, refund stake and give peace rewards
            if (!isLocalPlayerWinner)
            {
                PlayerProfile.Instance.AddCoins(60, "Refund Match Defeat Stake");
                PlayerProfile.Instance.AddCoins(250, "Mutual Peace & Harmony Bounty");
                PlayerProfile.Instance.AddAccountXP(250);
            }
            else
            {
                // Player was given 350, peace bounty is 250 (-100 difference)
                PlayerProfile.Instance.DeductCoins(100, "Adjust to Peace Bounty");
            }
        }

        winningTeamName = "Draw";
        isLocalPlayerWinner = true;
    }

    void OnGUI()
    {
        if (!isGameOver) return;

        GUI.depth = -500;

        // Dim background overlay
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float boxWidth = Mathf.Min(Screen.width * 0.94f, 820f);
        float boxHeight = Mathf.Min(Screen.height * 0.92f, 610f);
        float startX = (Screen.width - boxWidth) / 2f;
        float startY = Mathf.Max(20f, (Screen.height - boxHeight) / 2f);

        // 1. Ancient Parchment Background Frame with Radiant Pulsing Border
        float glowPulse = (Mathf.Sin(Time.time * 3.5f) + 1f) * 0.5f;
        Color glowingBorder = Color.Lerp(new Color(0.88f, 0.72f, 0.28f, 0.95f), new Color(1f, 0.95f, 0.55f, 1f), glowPulse);
        Rect mainRect = new Rect(startX, startY, boxWidth, boxHeight);
        EnchantedNatureGUI.DrawLeafPlaque(mainRect, new Color(0.10f, 0.16f, 0.10f, 0.97f), glowingBorder);

        // 2. Victory / Draw Header Banner with Multi-Language Support
        bool isDraw = winningTeamName.ToLower().Contains("draw") || winningTeamName.ToLower().Contains("tie") || winningTeamName.ToLower().Contains("peace");
        bool isProt = winningTeamName.ToLower().Contains("protector");

        string titleText;
        string subtitleText;
        Color bannerColor;

        if (isDraw)
        {
            titleText = LanguageSelectionUI.Localize(
                "🕊️ CELESTIAL PEACE & DRAW! HARMONY RESTORED! 🕊️",
                ar: "🕊️ تعادل وحال سلام! عمّ السلام الغابة المقدسة! 🕊️",
                fa: "🕊️ صلح آسمانی و برابری! هماهنگی برقرار شد! 🕊️",
                ru: "🕊️ НЕБЕСНЫЙ МИР И НИЧЬЯ! ГАРМОНИЯ ВОССТАНОВЛЕНА! 🕊️",
                fr: "🕊️ PAIX CÉLESTE & ÉGALITÉ ! L'HARMONIE EST RESTAURÉE ! 🕊️",
                es: "🕊️ ¡PAZ CELESTIAL Y EMPATE! ¡LA ARMONÍA HA SIDO RESTAURADA! 🕊️",
                de: "🕊️ HIMMLISCHER FRIEDEN & UNENTSCHIEDEN! 🕊️",
                it: "🕊️ PACE CELESTE E PAREGGIO! ARMONIA RESTAURATA! 🕊️",
                tr: "🕊️ İLAHİ BARIŞ VE BERABERLİK! ARMONİ YENİDEN SAĞLANDI! 🕊️",
                hi: "🕊️ दिव्य शांति और बराबरी! सामंजस्य पुनः स्थापित! 🕊️",
                zh: "🕊️ 天界和平与平局！森林重归和谐！🕊️",
                ja: "🕊️ 天上の平和と引き分け！調和が取り戻された！🕊️",
                pt: "🕊️ PAZ CELESTIAL E EMPATE! A HARMONIA FOI RESTAURADA! 🕊️"
            );

            subtitleText = LanguageSelectionUI.Localize(
                "🌿 The Sacred Tree Fell and The Crystal Arrived Together — Neither Side is Defeated! 🌿",
                ar: "🌿 دُمّرت الشجرة ووصلت الكريستالة في آنٍ واحد، لم يُهزم أحد وعمّ السلام! 🌿",
                fa: "🌿 درخت سقوط کرد و کریستال همزمان رسید — هیچ‌کس شکست نخورد! 🌿",
                ru: "🌿 Древо пало, и Кристалл доставлен одновременно — Никто не побежден! 🌿",
                fr: "🌿 L'Arbre est tombé et le Cristal est arrivé ensemble — Personne n'est vaincu ! 🌿",
                es: "🌿 ¡El Árbol cayó y el Cristal llegó a la vez — Nadie fue derrotado! 🌿",
                de: "🌿 Der Baum fiel und der Kristall kam zeitgleich an — Niemand ist besiegt! 🌿",
                it: "🌿 L'Albero è caduto e il Cristallo è arrivato insieme — Nessuno è sconfitto! 🌿",
                tr: "🌿 Ağaç devrildi ve Kristal aynı anda ulaştı — Kimse yenilmedi! 🌿",
                hi: "🌿 पवित्र पेड़ गिरा और क्रिस्टल एक साथ पहुँचा — कोई नहीं हारा! 🌿",
                zh: "🌿 神树倾覆而圣水晶同时送达——无人落败，森林和谐！🌿",
                ja: "🌿 神木は倒れ、水晶も同時に届いた——誰も敗北せず、調和が訪れた！🌿",
                pt: "🌿 A Árvore caiu e o Cristal chegou juntos — Ninguém foi derrotado! 🌿"
            );

            bannerColor = new Color(0.95f, 0.95f, 1f); // Radiant Celestial White-Gold
        }
        else if (isProt)
        {
            titleText = LanguageSelectionUI.Localize(
                "🌟 THE SACRED CRYSTAL REACHED THE SANCTUARY! PROTECTORS WIN! 🌟",
                "🌟 لقد وصلت الكريستالة للمذبح، فاز الحماة! 🌟",
                "🌟 КРИСТАЛЛ ДОСТИГ АЛТАРЯ! ЗАЩИТНИКИ ПОБЕДИЛИ! 🌟",
                "🌟 ¡EL CRISTAL HA LLEGADO AL ALTAR, GANARON LOS PROTECTORES! 🌟",
                "🌟 LE CRISTAL A ATTEINT L'AUTEL, LES PROTECTEURS ONT GAGNÉ ! 🌟",
                "🌟 DER KRISTALL HAT DEN ALTAR ERREICHT, DIE BESCHÜTZER HABEN GEWONNEN! 🌟",
                "🌟 क्रिस्टल वेदी पर पहुंच गया, संरक्षकों की विजय हुई! 🌟",
                "🌟 水晶已送达圣坛，守护者获胜！🌟",
                "🌟 水晶が祭壇に到達した、守護者の勝利！🌟",
                "🌟 O CRISTAL CHEGOU AO ALTAR, OS PROTETORES VENCERAM! 🌟");

            subtitleText = LanguageSelectionUI.Localize(
                "🌿 THE SACRED GROVE IS SAVED! 🌿",
                "🌿 تم إنقاذ الغابة المقدسة بسلام! 🌿",
                "🌿 СВЯЩЕННАЯ РОЩА СПАСЕНА! 🌿",
                "🌿 ¡EL BOSQUE SAGRADO ESTÁ SALVADO! 🌿",
                "🌿 LE BOSQUET SACRÉ EST SAUVÉ ! 🌿",
                "🌿 DER HEILIGE HAIN IST GERETTET! 🌿",
                "🌿 पवित्र उपवन बच गया! 🌿",
                "🌿 神圣森林得救了！🌿",
                "🌿 聖なる森は救われた！🌿",
                "🌿 O BOSQUE SAGRADO FOI SALVO! 🌿");

            bannerColor = new Color(0.2f, 0.9f, 0.4f);
        }
        else
        {
            titleText = LanguageSelectionUI.Localize(
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

            subtitleText = LanguageSelectionUI.Localize(
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

            bannerColor = new Color(1f, 0.35f, 0.2f);
        }

        Rect titleRect = new Rect(startX + 20f, startY + 20f, boxWidth - 40f, 32f);
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(boxWidth * 0.024f), 15, 20),
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
        subStyle.normal.textColor = isDraw ? new Color(0.85f, 0.95f, 1f) : new Color(1f, 0.88f, 0.35f);
        GUI.Label(subRect, subtitleText, subStyle);

        // Match Stakes & Rewards Banner with Rating Score & 10-Tier info
        Rect rewardRect = new Rect(startX + 20f, startY + 82f, boxWidth - 40f, 22f);
        GUIStyle rewardStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Italic
        };
        rewardStyle.normal.textColor = isDraw 
            ? new Color(0.95f, 0.95f, 1f)
            : (isLocalPlayerWinner ? new Color(0.85f, 1f, 0.85f) : new Color(1f, 0.85f, 0.65f));

        string rewardStr;
        if (isDraw)
        {
            rewardStr = LanguageSelectionUI.Localize(
                "🕊️ PEACE & HARMONY BOUNTY: +250 Acorn Coins & +350 Account XP Awarded to All! 🕊️",
                ar: "🕊️ مكافأة الهدنة والسلام: تم منح الجميع +250 عملة بلوط و +350 خبرة حساب! 🕊️",
                fa: "🕊️ پاداش صلح و برابری: دریافت +۲۵۰ سکه بلوط و +۳۵۰ تجربه برای همه! 🕊️",
                ru: "🕊️ НАГРАДА ЗА МИР: +250 Желудей и +350 Опыта всем игрокам! 🕊️",
                fr: "🕊️ PRIME DE PAIX & HARMONIE : +250 Glands & +350 EXP accordés à tous ! 🕊️",
                es: "🕊️ ¡RECOMPENSA DE PAZ Y ARMONÍA: +250 Bellotas y +350 EXP para todos! 🕊️",
                de: "🕊️ FRIEDENS- & HARMONIEPRÄMIE: +250 Eichel-Münzen & +350 EP an alle! 🕊️",
                it: "🕊️ PREMIO DI PACE E ARMONIA: +250 Monete Ghianda & +350 ESP a tutti! 🕊️",
                tr: "🕊️ BARIŞ VE ARMONİ ÖDÜLÜ: Herkese +250 Palamut Parası & +350 DP! 🕊️",
                hi: "🕊️ शांति और सामंजस्य पुरस्कार: सभी को +250 बलूत सिक्के और +350 अनुभव! 🕊️",
                zh: "🕊️ 和平与和谐赏金：全体玩家获得 +250 橡果金币与 +350 经验！🕊️",
                ja: "🕊️ 平和と調和の報酬：全プレイヤーに +250 ドングリコイン ＆ +350 経験値！🕊️",
                pt: "🕊️ RECOMPENSA DE PAZ E HARMONIA: +250 Moedas de Bolota & +350 XP para todos! 🕊️"
            );
        }
        else if (isLocalPlayerWinner)
        {
            rewardStr = LanguageSelectionUI.Localize(
                "✨ VICTORY BOUNTY: +350 Acorn Coins & +500 Account XP Awarded! ✨",
                ar: "✨ مكافأة الفوز: تم منحك +350 عملة بلوط و +500 خبرة حساب! ✨",
                ru: "✨ НАГРАДА ЗА ПОБЕДУ: +350 Желудей и +500 Опыта! ✨",
                fr: "✨ RÉCOMPENSE DE VICTOIRE : +350 Pièces Gland & +500 EXP ! ✨",
                es: "✨ ¡RECOMPENSA DE VICTORIA: +350 Monedas Bellota y +500 EXP! ✨",
                de: "✨ SIEGESPRÄMIE: +350 Eichel-Münzen & +500 Konto-EP! ✨",
                hi: "✨ विजय पुरस्कार: +350 बलूत के सिक्के और +500 अनुभव! ✨",
                zh: "✨ 胜利赏金：获得 +350 橡果金币与 +500 经验值！✨",
                ja: "✨ 勝利報酬：+350 ドングリコイン ＆ +500 経験値を獲得！✨",
                pt: "✨ RECOMPENSA DE VITÓRIA: +350 Moedas de Bolota & +500 XP! ✨",
                fa: "✨ پاداش پیروزی: دریافت +۳۵۰ سکه بلوط و +۵۰۰ تجربه! ✨",
                it: "✨ PREMIO VITTORIA: +350 Monete Ghianda & +500 ESP! ✨",
                tr: "✨ ZAFER ÖDÜLÜ: +350 Palamut Parası & +500 DP! ✨"
            );
        }
        else
        {
            rewardStr = LanguageSelectionUI.Localize(
                "⚔️ DEFEAT: -60 Acorn Match Stake Deducted (+100 XP Consolidation) ⚔️",
                ar: "⚔️ الهزيمة: تم خصم -60 عملة بلوط رهان اللقاء (+100 خبرة ترضية) ⚔️",
                ru: "⚔️ ПОРАЖЕНИЕ: Списана ставка -60 Желудей (+100 Опыта) ⚔️",
                fr: "⚔️ DÉFAITE : -60 Pièces Gland déduites (+100 EXP) ⚔️",
                es: "⚔️ DERROTA: Se dedujo la apuesta de -60 Bellotas (+100 EXP) ⚔️",
                de: "⚔️ NIEDERLAGE: -60 Eichel-Einsatz abgezogen (+100 EP) ⚔️",
                hi: "⚔️ पराजय: -60 बलूत दांव काटा गया (+100 अनुभव) ⚔️",
                zh: "⚔️ 战败：扣除 -60 橡果参赛费 (+100 经验慰藉) ⚔️",
                ja: "⚔️ 敗北：-60 ドングリ賭け金を消費 (+100 経験値) ⚔️",
                pt: "⚔️ DERROTA: -60 Moedas de aposta deduzidas (+100 XP) ⚔️",
                fa: "⚔️ شکست: کسر -۶۰ سکه بلوط هزینه نبرد (+۱۰۰ تجربه دلجویی) ⚔️",
                it: "⚔️ SCONFITTA: -60 Monete Ghianda detratte (+100 ESP) ⚔️",
                tr: "⚔️ YENİLGİ: -60 Palamut bahis bedeli kesildi (+100 DP) ⚔️"
            );
        }

        var curTier = PlayerProfile.Instance != null ? PlayerProfile.Instance.CurrentTier : default;
        string ratingSuffix = awardedRatingScore > 0 
            ? $" | 🏆 +{awardedRatingScore} Rating Pts ({curTier.badgeIcon} {curTier.GetLocalizedTitle()})"
            : "";
        GUI.Label(rewardRect, rewardStr + ratingSuffix, rewardStyle);

        // 3. Stats Table Header with Radiant Golden Polish
        float tableY = startY + 112f;
        Rect headerRect = new Rect(startX + 20f, tableY, boxWidth - 40f, 28f);
        Color headerBorder = Color.Lerp(new Color(0.65f, 0.50f, 0.25f, 0.85f), new Color(1f, 0.85f, 0.35f, 1f), glowPulse);
        EnchantedNatureGUI.DrawWoodPlate(headerRect, new Color(0.22f, 0.16f, 0.10f, 0.98f), headerBorder);

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
        GUI.Label(new Rect(colX, tableY + 4f, 45f, 20f), LanguageSelectionUI.Localize("BOSS", "زعيم", "БОСС", "JEFE", "BOSS", "BOSS", "बॉस", "首领", "ボス", "CHEFE"), colHeaderStyle);
        colX += 48f;
        GUI.Label(new Rect(colX, tableY + 4f, 58f, 20f), LanguageSelectionUI.Localize("CRYSTAL", "كريستال", "КРИСТАЛЛ", "CRISTAL", "CRISTAL", "KRISTALL", "क्रिस्टल", "水晶", "クリスタル", "CRISTAL"), colHeaderStyle);
        colX += 62f;
        GUI.Label(new Rect(colX, tableY + 4f, 58f, 20f), LanguageSelectionUI.Localize("SCORE", "نقاط", "СЧЕТ", "PUNTOS", "SCORE", "PUNKTE", "स्कोर", "积分", "スコア", "PONTUAÇÃO"), colHeaderStyle);
        colX += 62f;
        GUI.Label(new Rect(colX, tableY + 4f, 40f, 20f), "+👤", colHeaderStyle);

        // 4. Scrollable Stats Rows (Strictly 10 Players & Bots - Bosses Excluded)
        float listY = tableY + 32f;
        float listHeight = boxHeight - 245f;
        Rect listRect = new Rect(startX + 20f, listY, boxWidth - 40f, listHeight);

        // Sort stats by score and purge any non-player boss entities
        List<PlayerMatchStat> sortedList = new List<PlayerMatchStat>(stats.Values);
        sortedList.RemoveAll(s => s == null || s.heroName.Contains("Goliath") || s.heroName.Contains("Titan") || s.heroName.Contains("Overlord") || s.heroName.Contains("Boss") || s.heroName.Contains("Tree") || s.heroName.Contains("Altar"));
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
            rX += 62f;

            // زر إضافة كصديق مباشرة
            if (!s.isLocalPlayer && FriendsManager.Instance != null)
            {
                if (!FriendsManager.Instance.IsFriend(s.heroName) && !FriendsManager.Instance.HasPendingRequest(s.heroName))
                {
                    if (GUI.Button(new Rect(rX, currRowY + 3f, 36f, 22f), "+👤"))
                    {
                        FriendsManager.Instance.SendFriendRequest(s.heroName);
                    }
                }
                else if (FriendsManager.Instance.IsFriend(s.heroName))
                {
                    GUIStyle frStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
                    frStyle.normal.textColor = new Color(0.35f, 0.95f, 0.5f);
                    GUI.Label(new Rect(rX, currRowY + 4f, 36f, 20f), "🤝", frStyle);
                }
            }
        }

        GUI.EndScrollView();

        // 5. Economy & Wallet Status / Instant Top-Up Buttons
        float economyBarY = startY + boxHeight - 98f;
        int playerCoins = (PlayerProfile.Instance != null) ? PlayerProfile.Instance.Coins : 0;

        if (playerCoins <= 120)
        {
            float halfW = (boxWidth - 50f) * 0.5f;
            Rect adBtnRect = new Rect(startX + 20f, economyBarY, halfW, 32f);
            string adBtnText = LanguageSelectionUI.Localize(
                "📺 WATCH AD (+100 🌰)",
                ar: "📺 شاهد إعلاناً (+100 🌰)",
                ru: "📺 СМОТРЕТЬ РЕКЛАМУ (+100 🌰)",
                fr: "📺 REGARDER PUB (+100 🌰)",
                es: "📺 VER ANUNCIO (+100 🌰)",
                de: "📺 WERBUNG SCHAUEN (+100 🌰)",
                hi: "📺 विज्ञापन देखें (+100 🌰)",
                zh: "📺 观看短片 (+100 🌰)",
                ja: "📺 広告を見る (+100 🌰)",
                pt: "📺 ASSISTIR ANÚNCIO (+100 🌰)",
                fa: "📺 تماشای آگهی (+۱۰۰ 🌰)",
                it: "📺 GUARDA PUBBLICITÀ (+100 🌰)",
                tr: "📺 REKLAM İZLE (+100 🌰)"
            );
            if (EnchantedNatureGUI.DrawWoodButton(adBtnRect, adBtnText))
            {
                PlayerProfile.Instance?.WatchRewardedAd(success =>
                {
                    if (success) Debug.Log("Rewarded ad watched!");
                });
            }

            Rect buyBtnRect = new Rect(startX + 30f + halfW, economyBarY, halfW, 32f);
            string buyBtnText = LanguageSelectionUI.Localize(
                "💳 BUY 1,000 🌰 ($0.99)",
                ar: "💳 شراء 1000 🌰 ($0.99)",
                ru: "💳 1000 🌰 ($0.99)",
                fr: "💳 ACHETER 1000 🌰 ($0.99)",
                es: "💳 COMPRAR 1000 🌰 ($0.99)",
                de: "💳 1000 🌰 KAUFEN ($0.99)",
                hi: "💳 1000 🌰 खरीदें ($0.99)",
                zh: "💳 购买 1000 🌰 ($0.99)",
                ja: "💳 1000 🌰 を購入 ($0.99)",
                pt: "💳 COMPRAR 1000 🌰 ($0.99)",
                fa: "💳 خرید ۱۰۰۰ 🌰 ($۰.۹۹)",
                it: "💳 ACQUISTA 1000 🌰 ($0.99)",
                tr: "💳 1000 🌰 SATIN AL ($0.99)"
            );
            if (EnchantedNatureGUI.DrawWoodButton(buyBtnRect, buyBtnText))
            {
                PlayerProfile.Instance?.PurchaseCoinPackage(1000, "Acorn Pouch ($0.99)");
            }
        }
        else
        {
            Rect walletRect = new Rect(startX + 20f, economyBarY, boxWidth - 40f, 32f);
            string walletStr = LanguageSelectionUI.Localize(
                $"🌰 Your Acorn Purse: {playerCoins} Acorns",
                ar: $"🌰 محفظة البلوط الخاصة بك: {playerCoins} بلوطة",
                ru: $"🌰 Ваш кошелёк: {playerCoins} Желудей",
                fr: $"🌰 Votre bourse : {playerCoins} Glands",
                es: $"🌰 Tu monedero: {playerCoins} Bellotas",
                de: $"🌰 Dein Eichel-Beutel: {playerCoins} Eicheln",
                hi: $"🌰 आपकी बलूत थैली: {playerCoins} बलूत",
                zh: $"🌰 你的橡果钱袋：{playerCoins} 橡果",
                ja: $"🌰 所持ドングリ袋：{playerCoins} ドングリ",
                pt: $"🌰 Sua bolsa: {playerCoins} Bolotas",
                fa: $"🌰 کیسه بلوط شما: {playerCoins} بلوط",
                it: $"🌰 La tua borsa: {playerCoins} Ghiande",
                tr: $"🌰 Palamut Keseniz: {playerCoins} Palamut"
            );
            GUIStyle walletStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            walletStyle.normal.textColor = new Color(1f, 0.9f, 0.45f);
            GUI.Label(walletRect, walletStr, walletStyle);
        }

        // 6. Action Buttons at the Bottom
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
        LevelBossManager.ResetStaticState();

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
        LeaderboardUI.CloseLeaderboard();
        FriendsUI.CloseFriends();

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
