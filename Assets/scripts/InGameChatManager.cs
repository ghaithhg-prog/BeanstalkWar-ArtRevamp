using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// InGameChatManager handles team tactical chat, friendly room lobby chat, quick pings,
/// network synchronization, and intelligent bot chat interactions across 13 languages.
/// </summary>
public class InGameChatManager : MonoBehaviour
{
    public static InGameChatManager Instance { get; private set; }

    public enum ChatChannel
    {
        RoomLobby = 0,
        TeamProtector = 1,
        TeamDestroyer = 2
    }

    public enum QuickPingType
    {
        DefendTree = 0,
        AttackBoss = 1,
        EscortCrystal = 2,
        Charge = 3,
        NeedBackup = 4,
        Regroup = 5,
        WellPlayed = 6,
        ThankYou = 7
    }

    public enum LobbyGreetingType
    {
        Hello = 0,
        Ready = 1,
        GoodLuck = 2,
        EpicBattle = 3
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string senderName;
        public ChatChannel channel;
        public string messageText;
        public float timestamp;
        public bool isBot;
        public bool isQuickPing;
        public QuickPingType pingType;
        public string icon = "💬";
    }

    public event Action<ChatMessage> OnMessageReceived;

    private readonly List<ChatMessage> allMessages = new List<ChatMessage>();
    private const int MaxStoredMessages = 120;

    // Bot Chatter Timers & States
    private float lastBotMessageTime = -30f;
    private const float MinBotChatInterval = 18f;
    private bool matchStartChatTriggered = false;
    private float matchStartTime = 0f;
    private bool tree75AlertTriggered = false;
    private bool tree40AlertTriggered = false;
    private bool tree15AlertTriggered = false;
    private float nextAmbientBotChatTime = 35f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject chatRoot = new GameObject("InGameChatSystem");
            Instance = chatRoot.AddComponent<InGameChatManager>();
            chatRoot.AddComponent<InGameChatUI>();
            DontDestroyOnLoad(chatRoot);
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

    void Update()
    {
        // Handle delayed match start bot greeting
        if (matchStartChatTriggered && Time.time - matchStartTime >= 4.5f)
        {
            matchStartChatTriggered = false;
            TriggerMatchStartBotChatter();
        }

        // Periodic ambient tactical chatter during active gameplay
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())
        {
            if (Time.time >= nextAmbientBotChatTime)
            {
                nextAmbientBotChatTime = Time.time + UnityEngine.Random.Range(28f, 48f);
                TriggerAmbientBotChatter();
            }
        }
    }

    // ==========================================
    // Send Messages & Quick Pings
    // ==========================================

    public void SendTextMessage(string text, bool inLobby)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();
        if (text.Length > 100) text = text.Substring(0, 100);

        string sender = PlayerProfile.GetSavedPlayerName();
        if (string.IsNullOrEmpty(sender)) sender = "Player";

        ChatChannel channel;
        if (inLobby)
        {
            channel = ChatChannel.RoomLobby;
        }
        else
        {
            channel = GetLocalPlayerChannel();
        }

        DispatchChat(sender, channel, text, false, false, QuickPingType.DefendTree, "💬");
    }

    public void SendQuickPing(QuickPingType pingType, bool inLobby)
    {
        string sender = PlayerProfile.GetSavedPlayerName();
        if (string.IsNullOrEmpty(sender)) sender = "Player";

        ChatChannel channel = inLobby ? ChatChannel.RoomLobby : GetLocalPlayerChannel();
        string localizedText = GetQuickPingText(pingType);
        string icon = GetQuickPingIcon(pingType);

        DispatchChat(sender, channel, localizedText, false, true, pingType, icon);
    }

    public void SendLobbyGreeting(LobbyGreetingType greeting)
    {
        string sender = PlayerProfile.GetSavedPlayerName();
        if (string.IsNullOrEmpty(sender)) sender = "Player";

        string text = GetLobbyGreetingText(greeting);
        string icon = GetLobbyGreetingIcon(greeting);

        DispatchChat(sender, ChatChannel.RoomLobby, text, false, true, QuickPingType.WellPlayed, icon);
    }

    private void DispatchChat(string sender, ChatChannel channel, string text, bool isBot, bool isQuickPing, QuickPingType pingType, string icon)
    {
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        PlayerMovement localPlayer = PlayerMovement.LocalPlayer;

        if (isNetworkActive && localPlayer != null)
        {
            localPlayer.SendChatMessageServerRpc(sender, (int)channel, text, isBot, isQuickPing, (int)pingType);
        }
        else
        {
            // Solo Offline Mode or Local fallback
            ReceiveMessageLocal(sender, channel, text, isBot, isQuickPing, pingType, icon);
        }
    }

    public void ReceiveMessageFromNetwork(string sender, ChatChannel channel, string text, bool isBot, bool isQuickPing, QuickPingType pingType)
    {
        string icon = isQuickPing ? GetQuickPingIcon(pingType) : "💬";
        ReceiveMessageLocal(sender, channel, text, isBot, isQuickPing, pingType, icon);
    }

    private void ReceiveMessageLocal(string sender, ChatChannel channel, string text, bool isBot, bool isQuickPing, QuickPingType pingType, string icon)
    {
        // Filter by channel: Ensure opponents can NEVER read rival team chat during match!
        if (channel != ChatChannel.RoomLobby)
        {
            ChatChannel myChannel = GetLocalPlayerChannel();
            if (channel != myChannel)
            {
                // Discard message intended for rival team
                return;
            }
        }

        // If it's a quick ping, dynamically localize it into the local player's active language!
        if (isQuickPing)
        {
            text = GetQuickPingText(pingType);
        }

        ChatMessage msg = new ChatMessage
        {
            senderName = sender,
            channel = channel,
            messageText = text,
            timestamp = Time.time,
            isBot = isBot,
            isQuickPing = isQuickPing,
            pingType = pingType,
            icon = icon
        };

        allMessages.Add(msg);
        if (allMessages.Count > MaxStoredMessages)
        {
            allMessages.RemoveAt(0);
        }

        OnMessageReceived?.Invoke(msg);
    }

    public List<ChatMessage> GetMessagesForChannel(ChatChannel channel)
    {
        List<ChatMessage> list = new List<ChatMessage>();
        for (int i = 0; i < allMessages.Count; i++)
        {
            if (allMessages[i].channel == channel)
            {
                list.Add(allMessages[i]);
            }
        }
        return list;
    }

    public ChatChannel GetLocalPlayerChannel()
    {
        PlayerMovement localPlayer = PlayerMovement.LocalPlayer;
        if (localPlayer != null && localPlayer.team == PlayerMovement.TeamType.Destroyer)
        {
            return ChatChannel.TeamDestroyer;
        }
        return ChatChannel.TeamProtector;
    }

    // ==========================================
    // Smart Bot Chatter Integration
    // ==========================================

    public void NotifyMatchStarted()
    {
        matchStartChatTriggered = true;
        matchStartTime = Time.time;
        tree75AlertTriggered = false;
        tree40AlertTriggered = false;
        tree15AlertTriggered = false;
        nextAmbientBotChatTime = Time.time + UnityEngine.Random.Range(20f, 35f);
    }

    private void TriggerMatchStartBotChatter()
    {
        ChatChannel myChannel = GetLocalPlayerChannel();
        string botName = GetFriendlyBotName(myChannel);
        if (string.IsNullOrEmpty(botName)) return;

        string phrase = (myChannel == ChatChannel.TeamProtector)
            ? LanguageSelectionUI.Localize(
                "🛡️ We are the guardians of the Sacred Grove! Stand firm!",
                ar: "🛡️ نحن حماة الغابة المقدسة! اثبتوا ولن تسقط الشجرة!",
                fa: "🛡️ ما نگهبانان جنگل مقدّسیم! استوار بایستید!",
                ru: "🛡️ Мы защитники Священной Рощи! Стойте насмерть!",
                fr: "🛡️ Nous sommes les gardiens du Bosquet Sacré ! Tenez bon !",
                es: "🛡️ ¡Somos los guardianes del Bosque Sagrado! ¡Firmes!",
                de: "🛡️ Wir sind die Hüter des Heiligen Hains! Haltet die Stellung!",
                it: "🛡️ Siamo i guardiani del Sacro Bosco! Resistete!",
                tr: "🛡️ Biz Kutsal Korunun muhafızlarıyız! Sağlam durun!",
                pt: "🛡️ Somos os guardiões do Bosque Sagrado! Firmeza!",
                hi: "🛡️ हम पवित्र उपवन के रक्षक हैं! डटे रहो!",
                zh: "🛡️ 我们是神圣森林的守护者！坚守阵地！",
                ja: "🛡️ 我らは聖なる森の守護者！持ちこたえよ！")
            : LanguageSelectionUI.Localize(
                "⚔️ Time to fell the great beanstalk! To battle!",
                ar: "⚔️ حان وقت قطع الشجرة العملاقة! إلى المعركة!",
                fa: "⚔️ زمان قطع ساقه غول‌پیکر فرا رسیده! به سوی نبرد!",
                ru: "⚔️ Пора срубить великий стебель! В бой!",
                fr: "⚔️ L'heure est venue d'abattre le haricot ! Au combat !",
                es: "⚔️ ¡Hora de talar el gran tallo! ¡A la batalla!",
                de: "⚔️ Zeit, die Bohnenranke zu fällen! Auf in die Schlacht!",
                it: "⚔️ È ora di abbattere il grande fagiolo! In battaglia!",
                tr: "⚔️ Büyük fasulye sırığını devirme vakti! Savaşa!",
                pt: "⚔️ Hora de derrubar o grande pé de feijão! À batalha!",
                hi: "⚔️ विशाल सेम के पेड़ को गिराने का समय आ गया है! युद्ध के लिए तैयार!",
                zh: "⚔️ 砍倒巨型魔豆树的时刻到了！进军！",
                ja: "⚔️ 巨大な豆の木を切り倒す時が来た！いざ戦場へ！");

        DeliverBotChat(botName, myChannel, phrase, false, QuickPingType.Charge, "⚔️");
    }

    public void NotifyTreeHealthChanged(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f) return;
        float pct = (currentHealth / maxHealth) * 100f;

        if (pct <= 75f && !tree75AlertTriggered)
        {
            tree75AlertTriggered = true;
            TriggerTreeDefenseBotAlert(75);
        }
        else if (pct <= 40f && !tree40AlertTriggered)
        {
            tree40AlertTriggered = true;
            TriggerTreeDefenseBotAlert(40);
        }
        else if (pct <= 15f && !tree15AlertTriggered)
        {
            tree15AlertTriggered = true;
            TriggerTreeDefenseBotAlert(15);
        }
    }

    private void TriggerTreeDefenseBotAlert(int threshold)
    {
        if (Time.time - lastBotMessageTime < MinBotChatInterval) return;

        ChatChannel myChannel = GetLocalPlayerChannel();
        string botName = GetFriendlyBotName(myChannel);
        if (string.IsNullOrEmpty(botName)) return;

        if (myChannel == ChatChannel.TeamProtector)
        {
            string alertText = (threshold <= 40)
                ? LanguageSelectionUI.Localize(
                    "⚠️ The Sacred Tree is under critical attack! Defend it now!",
                    ar: "⚠️ الشجرة المقدسة في خطر حرج! دافعوا عنها فورًا!",
                    fa: "⚠️ درخت مقدس در خطر بحرانی است! فورا دفاع کنید!",
                    ru: "⚠️ Священное Дерево в критической опасности! Защитите его!",
                    fr: "⚠️ L'Arbre Sacré subit de lourds dégâts ! Défendez-le vite !",
                    es: "⚠️ ¡El Árbol Sagrado está en peligro crítico! ¡Defendedlo ya!",
                    de: "⚠️ Der Heilige Baum ist in kritischer Gefahr! Sofort verteidigen!",
                    it: "⚠️ Il Sacro Albero è in pericolo critico! Difendetelo subito!",
                    tr: "⚠️ Kutsal Ağaç kritik tehlikede! Hemen savunun!",
                    pt: "⚠️ A Árvore Sagrada está em perigo crítico! Defendam agora!",
                    hi: "⚠️ पवित्र वृक्ष अत्यधिक संकट में है! तुरंत रक्षा करो!",
                    zh: "⚠️ 神圣巨树正遭受致命攻击！速速回防！",
                    ja: "⚠️ 聖なる巨木が危機的状況だ！直ちに防衛せよ！")
                : LanguageSelectionUI.Localize(
                    "🛡️ Tree is taking hits! Repel the attackers!",
                    ar: "🛡️ الشجرة تتعرض للضربات! اصدوا المهاجمين!",
                    fa: "🛡️ درخت ضربه می‌خورد! مهاجمان را دفع کنید!",
                    ru: "🛡️ Дерево атакуют! Отбейте нападение!",
                    fr: "🛡️ L'arbre prend des coups ! Repoussez l'ennemi !",
                    es: "🛡️ ¡El árbol está recibiendo daño! ¡Rechazad al enemigo!",
                    de: "🛡️ Der Baum wird angegriffen! Schlagt sie zurück!",
                    it: "🛡️ L'albero subisce colpi! Respingete gli attaccanti!",
                    tr: "🛡️ Ağaç darbe alıyor! Saldırganları püskürtün!",
                    pt: "🛡️ A árvore está levando dano! Rechaçem os atacantes!",
                    hi: "🛡️ वृक्ष पर प्रहार हो रहा है! हमलावरों को पीछे धकेलो!",
                    zh: "🛡️ 神树受到打击！击退入侵者！",
                    ja: "🛡️ 巨木が攻撃されている！敵を撃退せよ！");

            DeliverBotChat(botName, myChannel, alertText, true, QuickPingType.DefendTree, "🛡️");
        }
        else
        {
            string attackText = LanguageSelectionUI.Localize(
                "🔥 The tree's defenses are cracking! Keep pressing the assault!",
                ar: "🔥 دفاعات الشجرة تتصدع! واصلوا الهجوم بلا هوادة!",
                fa: "🔥 استحکامات درخت در حال فروپاشی است! حمله را ادامه دهید!",
                ru: "🔥 Оборона дерева трещит! Усильте натиск!",
                fr: "🔥 Les défenses de l'arbre cèdent ! Continuez d'attaquer !",
                es: "🔥 ¡Las defensas del árbol ceden! ¡Mantened la presión!",
                de: "🔥 Die Baumverteidigung bricht! Weiter angreifen!",
                it: "🔥 Le difese dell'albero cedono! Continuate a colpire!",
                tr: "🔥 Ağacın savunması çöküyor! Saldırıya devam!",
                pt: "🔥 As defesas da árvore estão cedendo! Mantenham a pressão!",
                hi: "🔥 वृक्ष की रक्षा टूट रही है! आक्रमण जारी रखो!",
                zh: "🔥 神树护盾即将瓦解！全员压上！",
                ja: "🔥 巨木の防御が崩れかけている！攻め立てろ！");

            DeliverBotChat(botName, myChannel, attackText, true, QuickPingType.Charge, "🔥");
        }
    }

    public void NotifyBossSpawned(string bossName)
    {
        if (Time.time - lastBotMessageTime < MinBotChatInterval) return;

        ChatChannel myChannel = GetLocalPlayerChannel();
        string botName = GetFriendlyBotName(myChannel);
        if (string.IsNullOrEmpty(botName)) return;

        string bossAlert = LanguageSelectionUI.Localize(
            $"⚔️ Look out! The {bossName} has awakened on the battlefield!",
            ar: $"⚔️ انتبهوا! استيقظ الزعيم ({bossName}) في ساحة المعركة!",
            fa: $"⚔️ مراقب باشید! غول ({bossName}) در میدان نبرد بیدار شد!",
            ru: $"⚔️ Внимание! Босс ({bossName}) пробудился на поле боя!",
            fr: $"⚔️ Attention ! Le Boss ({bossName}) s'est éveillé !",
            es: $"⚔️ ¡Cuidado! ¡El Jefe ({bossName}) ha despertado!",
            de: $"⚔️ Achtung! Der Boss ({bossName}) ist erwacht!",
            it: $"⚔️ Attenzione! Il Boss ({bossName}) si è risvegliato!",
            tr: $"⚔️ Dikkat! Boss ({bossName}) savaş alanında uyandı!",
            pt: $"⚔️ Cuidado! O Chefe ({bossName}) despertou na batalha!",
            hi: $"⚔️ सावधान! महाबली ({bossName}) युद्ध के मैदान में जाग गया है!",
            zh: $"⚔️ 注意！首领【{bossName}】在战场苏醒！",
            ja: $"⚔️ 注意せよ！ボス【{bossName}】が覚醒した！");

        DeliverBotChat(botName, myChannel, bossAlert, true, QuickPingType.AttackBoss, "⚔️");
    }

    public void NotifyBossDefeated(string bossName, string slayerName)
    {
        ChatChannel myChannel = GetLocalPlayerChannel();
        string botName = GetFriendlyBotName(myChannel);
        if (string.IsNullOrEmpty(botName)) return;

        string victoryShout = LanguageSelectionUI.Localize(
            $"👏 Glorious triumph! {bossName} has fallen! Outstanding work!",
            ar: $"👏 نصر بطولي عظيم! سقط الزعيم ({bossName})! كفو يا أبطال!",
            fa: $"👏 پیروزی شکوهمند! غول ({bossName}) سقوط کرد! دمتان گرم!",
            ru: $"👏 Славная победа! Босс ({bossName}) повержен! Браво!",
            fr: $"👏 Victoire glorieuse ! {bossName} est vaincu ! Bravo à tous !",
            es: $"👏 ¡Victoria gloriosa! ¡{bossName} ha caído! ¡Gran trabajo!",
            de: $"👏 Ruhmreicher Triumph! {bossName} ist gefallen! Hervorragend!",
            it: $"👏 Glorioso trionfo! {bossName} è caduto! Lavoro superbo!",
            tr: $"👏 Şanlı zafer! {bossName} devrildi! Harika iş çıkardınız!",
            pt: $"👏 Triunfo glorioso! {bossName} caiu! Trabalho incrível!",
            hi: $"👏 शानदार विजय! {bossName} धराशायी हो गया! अद्भुत कार्य!",
            zh: $"👏 辉煌胜利！首领【{bossName}】已被击溃！太棒了！",
            ja: $"👏 見事な勝利！ボス【{bossName}】は討ち取られた！");

        DeliverBotChat(botName, myChannel, victoryShout, true, QuickPingType.WellPlayed, "👏");
    }

    public void NotifyCrystalPickedUp(string carrierName)
    {
        if (Time.time - lastBotMessageTime < MinBotChatInterval) return;

        ChatChannel myChannel = GetLocalPlayerChannel();
        string botName = GetFriendlyBotName(myChannel);
        if (string.IsNullOrEmpty(botName)) return;

        if (myChannel == ChatChannel.TeamProtector)
        {
            string escortMsg = LanguageSelectionUI.Localize(
                $"✨ {carrierName} has the Sacred Crystal! Form a guard and escort to the Altar!",
                ar: $"✨ {carrierName} يحمل الكريستالة المقدسة! رافقوه واحموا الطريق للمذبح!",
                fa: $"✨ {carrierName} کریستال مقدس را برداشت! اسکورت کنید و به محراب برسانید!",
                ru: $"✨ {carrierName} несёт Священный Кристалл! Защищайте носителя!",
                fr: $"✨ {carrierName} porte le Cristal Sacré ! Escortez-le vers l'Autel !",
                es: $"✨ ¡{carrierName} lleva el Cristal Sagrado! ¡Escoltedlo al Altar!",
                de: $"✨ {carrierName} trägt den Kristall! Eskortiert ihn zum Altar!",
                it: $"✨ {carrierName} ha il Cristallo Sacro! Scortatelo all'Altare!",
                tr: $"✨ {carrierName} Kutsal Kristali taşıyor! Sunağa kadar eşlik edin!",
                pt: $"✨ {carrierName} está com o Cristal Sagrado! Escoltem-no até o Altar!",
                hi: $"✨ {carrierName} के पास पवित्र क्रिस्टल है! वेदी तक सुरक्षा घेरा बनाओ!",
                zh: $"✨ {carrierName} 拾取了神圣水晶！全员护送至圣坛！",
                ja: $"✨ {carrierName} が聖なる水晶を運んでいる！祭壇まで護衛せよ！");

            DeliverBotChat(botName, myChannel, escortMsg, true, QuickPingType.EscortCrystal, "✨");
        }
        else
        {
            string interceptMsg = LanguageSelectionUI.Localize(
                "⚠️ The Protectors are carrying the Crystal! Intercept and strike them!",
                ar: "⚠️ الحماة يحملون الكريستالة! اعترضوا طريقهم وأسقطوها فورًا!",
                fa: "⚠️ حامیان کریستال را حمل می‌کنند! سد راهشان شوید و حمله کنید!",
                ru: "⚠️ Защитники несут Кристалл! Перехватите и атакуйте их!",
                fr: "⚠️ Les Protecteurs portent le Cristal ! Interceptez-les vite !",
                es: "⚠️ ¡Los Protectores llevan el Cristal! ¡Interceptadlos y atacad!",
                de: "⚠️ Die Beschützer tragen den Kristall! Abfangen und angreifen!",
                it: "⚠️ I Protettori portano il Cristallo! Intercettateli e colpite!",
                tr: "⚠️ Koruyucular Kristali taşıyor! Önlerini kesin ve saldırın!",
                pt: "⚠️ Os Protetores estão com o Cristal! Interceptem e ataquem!",
                hi: "⚠️ संरक्षक क्रिस्टल ले जा रहे हैं! रास्ता रोको और हमला करो!",
                zh: "⚠️ 守护者正在护送水晶！立即拦截歼灭！",
                ja: "⚠️ 守護者が水晶を運搬中だ！迎撃して阻止せよ！");

            DeliverBotChat(botName, myChannel, interceptMsg, true, QuickPingType.Charge, "⚠️");
        }
    }

    public void NotifyCrystalDelivered()
    {
        ChatChannel myChannel = GetLocalPlayerChannel();
        string botName = GetFriendlyBotName(myChannel);
        if (string.IsNullOrEmpty(botName)) return;

        if (myChannel == ChatChannel.TeamProtector)
        {
            string deliveredMsg = LanguageSelectionUI.Localize(
                "🌟 The Sacred Crystal has reached the Altar! Victory is ours!",
                ar: "🌟 وصلت الكريستالة المقدسة إلى المذبح! النصر لنا يا أبطال!",
                fa: "🌟 کریستال مقدس به محراب رسید! پیروزی از آن ماست!",
                ru: "🌟 Священный Кристалл на Алтаре! Победа за нами!",
                fr: "🌟 Le Cristal Sacré est sur l'Autel ! La victoire est à nous !",
                es: "🌟 ¡El Cristal Sagrado llegó al Altar! ¡La victoria es nuestra!",
                de: "🌟 Der Kristall hat den Altar erreicht! Der Sieg gehört uns!",
                it: "🌟 Il Cristallo Sacro ha raggiunto l'Altare! Vittoria nostra!",
                tr: "🌟 Kutsal Kristal Sunağa ulaştı! Zafer bizim!",
                pt: "🌟 O Cristal Sagrado chegou ao Altar! A vitória é nossa!",
                hi: "🌟 पवित्र क्रिस्टल वेदी पर पहुंच गया! विजय हमारी है!",
                zh: "🌟 神圣水晶已送达圣坛！胜利属于我们！",
                ja: "🌟 聖なる水晶が祭壇に到達した！我らの勝利だ！");

            DeliverBotChat(botName, myChannel, deliveredMsg, true, QuickPingType.WellPlayed, "🌟");
        }
    }

    private void TriggerAmbientBotChatter()
    {
        if (Time.time - lastBotMessageTime < MinBotChatInterval) return;

        ChatChannel myChannel = GetLocalPlayerChannel();
        string botName = GetFriendlyBotName(myChannel);
        if (string.IsNullOrEmpty(botName)) return;

        QuickPingType randomPing = (QuickPingType)UnityEngine.Random.Range(0, 8);
        string pingText = GetQuickPingText(randomPing);
        string icon = GetQuickPingIcon(randomPing);

        DeliverBotChat(botName, myChannel, pingText, true, randomPing, icon);
    }

    private void DeliverBotChat(string botName, ChatChannel channel, string text, bool isQuickPing, QuickPingType pingType, string icon)
    {
        lastBotMessageTime = Time.time;
        ReceiveMessageLocal(botName, channel, text, true, isQuickPing, pingType, icon);
    }

    private string GetFriendlyBotName(ChatChannel channel)
    {
        if (channel == ChatChannel.TeamProtector)
        {
            string[] names = { "Althea Starbloom", "Boran Stonebark", "Silas Verdantguard", "Lyra Swiftbreeze", "Rowan Deeproot" };
            return names[UnityEngine.Random.Range(0, names.Length)];
        }
        else
        {
            string[] names = { "Grimlok Ashfall", "Morrigan Cinderveil", "Kragor Blightfang", "Zarek Bloodthorn", "Vorash Doomcleave" };
            return names[UnityEngine.Random.Range(0, names.Length)];
        }
    }

    // ==========================================
    // 13-Language Quick Pings & Lobby Greetings
    // ==========================================

    public static string GetQuickPingIcon(QuickPingType type)
    {
        switch (type)
        {
            case QuickPingType.DefendTree: return "🛡️";
            case QuickPingType.AttackBoss: return "⚔️";
            case QuickPingType.EscortCrystal: return "✨";
            case QuickPingType.Charge: return "🚀";
            case QuickPingType.NeedBackup: return "🆘";
            case QuickPingType.Regroup: return "⚠️";
            case QuickPingType.WellPlayed: return "👏";
            case QuickPingType.ThankYou: return "💚";
            default: return "💬";
        }
    }

    public static string GetQuickPingText(QuickPingType type)
    {
        switch (type)
        {
            case QuickPingType.DefendTree:
                return LanguageSelectionUI.Localize(
                    "🛡️ Defend the Sacred Tree!",
                    ar: "🛡️ احموا الشجرة المقدسة!",
                    fa: "🛡️ از درخت مقدس دفاع کنید!",
                    ru: "🛡️ Защитите Священное Дерево!",
                    fr: "🛡️ Défendez l'Arbre Sacré !",
                    es: "🛡️ ¡Defiendan el Árbol Sagrado!",
                    de: "🛡️ Verteidigt den Heiligen Baum!",
                    it: "🛡️ Difendete il Sacro Albero!",
                    tr: "🛡️ Kutsal Ağacı Koruyun!",
                    pt: "🛡️ Defendam a Árvore Sagrada!",
                    hi: "🛡️ पवित्र वृक्ष की रक्षा करो!",
                    zh: "🛡️ 保卫神圣巨树！",
                    ja: "🛡️ 聖なる巨木を守れ！");

            case QuickPingType.AttackBoss:
                return LanguageSelectionUI.Localize(
                    "⚔️ Attack the Boss now!",
                    ar: "⚔️ هاجموا الزعيم الآن!",
                    fa: "⚔️ اکنون به غول حمله کنید!",
                    ru: "⚔️ Атакуйте Босса сейчас!",
                    fr: "⚔️ Attaquez le Boss maintenant !",
                    es: "⚔️ ¡Ataquen al Jefe ahora!",
                    de: "⚔️ Greift jetzt den Boss an!",
                    it: "⚔️ Attaccate il Boss adesso!",
                    tr: "⚔️ Şimdi Boss'a Saldırın!",
                    pt: "⚔️ Ataquem o Chefe agora!",
                    hi: "⚔️ बॉस पर अभी हमला करो!",
                    zh: "⚔️ 全力攻击巨怪首领！",
                    ja: "⚔️ 今すぐボスを攻撃せよ！");

            case QuickPingType.EscortCrystal:
                return LanguageSelectionUI.Localize(
                    "✨ Escort the Crystal Carrier!",
                    ar: "✨ رافقوا حامل الكريستالة واحموه!",
                    fa: "✨ حامل کریستال را همراهی و محافظت کنید!",
                    ru: "✨ Сопровождайте носителя Кристалла!",
                    fr: "✨ Escortez le porteur du Cristal !",
                    es: "✨ ¡Escolten al portador del Cristal!",
                    de: "✨ Eskortiert den Kristallträger!",
                    it: "✨ Scortate il portatore del Cristallo!",
                    tr: "✨ Kristal Taşıyıcısını Koruyun!",
                    pt: "✨ Escoltem o portador do Cristal!",
                    hi: "✨ क्रिस्टल वाहक की सुरक्षा करो!",
                    zh: "✨ 护送水晶携带者！",
                    ja: "✨ 水晶の運搬者を護衛せよ！");

            case QuickPingType.Charge:
                return LanguageSelectionUI.Localize(
                    "🚀 Charge forward! Push now!",
                    ar: "🚀 هجوم شامل! تقدموا الآن!",
                    fa: "🚀 حمله سراسری! به جلو پیشروی کنید!",
                    ru: "🚀 В атаку! Вперёд!",
                    fr: "🚀 À l'assaut ! Avancez !",
                    es: "🚀 ¡Al ataque! ¡Avanzad ahora!",
                    de: "🚀 Sturmangriff! Vorwärts!",
                    it: "🚀 Alla carica! Avanzate adesso!",
                    tr: "🚀 Hücum! İleri atılın!",
                    pt: "🚀 Ao ataque! Avancem agora!",
                    hi: "🚀 आगे बढ़ो! आक्रमण करो!",
                    zh: "🚀 全军突击！冲锋！",
                    ja: "🚀 全軍突撃！前進せよ！");

            case QuickPingType.NeedBackup:
                return LanguageSelectionUI.Localize(
                    "🆘 I need backup!",
                    ar: "🆘 أحتاج إلى دعم ومساعدة!",
                    fa: "🆘 به پشتیبانی و کمک نیاز دارم!",
                    ru: "🆘 Мне нужна поддержка!",
                    fr: "🆘 J'ai besoin de renforts !",
                    es: "🆘 ¡Necesito apoyo!",
                    de: "🆘 Ich brauche Verstärkung!",
                    it: "🆘 Ho bisogno di rinforzi!",
                    tr: "🆘 Desteğe ihtiyacım var!",
                    pt: "🆘 Preciso de reforços!",
                    hi: "🆘 मुझे मदद की ज़रूरत है!",
                    zh: "🆘 我需要支援！",
                    ja: "🆘 救援を要請する！");

            case QuickPingType.Regroup:
                return LanguageSelectionUI.Localize(
                    "⚠️ Fall back and regroup!",
                    ar: "⚠️ تراجعوا وأعيدوا التجمع!",
                    fa: "⚠️ عقب‌نشینی کرده و تجدید قوا کنید!",
                    ru: "⚠️ Отступаем и перегруппировываемся!",
                    fr: "⚠️ Repliez-vous et regroupez-vous !",
                    es: "⚠️ ¡Retirada y reagruparse!",
                    de: "⚠️ Rückzug und neu formieren!",
                    it: "⚠️ Ritirata e riorganizzarsi!",
                    tr: "⚠️ Geri çekilin ve toplanın!",
                    pt: "⚠️ Recuar e reagrupar!",
                    hi: "⚠️ पीछे हटो और फिर से एकजुट हो जाओ!",
                    zh: "⚠️ 后撤重新集结！",
                    ja: "⚠️ 後退して再集結せよ！");

            case QuickPingType.WellPlayed:
                return LanguageSelectionUI.Localize(
                    "👏 Well played! Great job!",
                    ar: "👏 كفو يا بطل! عمل رائع!",
                    fa: "👏 دمت گرم! عالی بود!",
                    ru: "👏 Отличная игра! Молодцы!",
                    fr: "👏 Bien joué ! Super travail !",
                    es: "👏 ¡Bien jugado! ¡Gran trabajo!",
                    de: "👏 Gut gespielt! Tolle Leistung!",
                    it: "👏 Ben giocato! Ottimo lavoro!",
                    tr: "👏 İyi oyundu! Harika iş!",
                    pt: "👏 Muito bem jogado! Ótimo trabalho!",
                    hi: "👏 बहुत बढ़िया! शानदार खेल!",
                    zh: "👏 干得漂亮！太棒了！",
                    ja: "👏 ナイスプレイ！お見事！");

            case QuickPingType.ThankYou:
                return LanguageSelectionUI.Localize(
                    "💚 Thank you!",
                    ar: "💚 شكرًا جزيلاً لك!",
                    fa: "💚 بسیار متشکرم!",
                    ru: "💚 Большое спасибо!",
                    fr: "💚 Merci beaucoup !",
                    es: "💚 ¡Muchas gracias!",
                    de: "💚 Vielen Dank!",
                    it: "💚 Grazie mille!",
                    tr: "💚 Çok teşekkürler!",
                    pt: "💚 Muito obrigado!",
                    hi: "💚 बहुत-बहुत धन्यवाद!",
                    zh: "💚 非常感谢！",
                    ja: "💚 どうもありがとう！");

            default:
                return "";
        }
    }

    public static string GetLobbyGreetingIcon(LobbyGreetingType greeting)
    {
        switch (greeting)
        {
            case LobbyGreetingType.Hello: return "👋";
            case LobbyGreetingType.Ready: return "👍";
            case LobbyGreetingType.GoodLuck: return "🤝";
            case LobbyGreetingType.EpicBattle: return "⚔️";
            default: return "💬";
        }
    }

    public static string GetLobbyGreetingText(LobbyGreetingType greeting)
    {
        switch (greeting)
        {
            case LobbyGreetingType.Hello:
                return LanguageSelectionUI.Localize(
                    "👋 Hello everyone!",
                    ar: "👋 مرحبًا بالجميع!",
                    fa: "👋 سلام به همگی!",
                    ru: "👋 Всем привет!",
                    fr: "👋 Bonjour à tous !",
                    es: "👋 ¡Hola a todos!",
                    de: "👋 Hallo zusammen!",
                    it: "👋 Ciao a tutti!",
                    tr: "👋 Herkese merhaba!",
                    pt: "👋 Olá a todos!",
                    hi: "👋 सभी को नमस्ते!",
                    zh: "👋 大家好！",
                    ja: "👋 皆さんこんにちは！");

            case LobbyGreetingType.Ready:
                return LanguageSelectionUI.Localize(
                    "👍 I am ready for the battle!",
                    ar: "👍 أنا جاهز للمعركة!",
                    fa: "👍 من برای نبرد آماده‌ام!",
                    ru: "👍 Я готов к битве!",
                    fr: "👍 Je suis prêt pour la bataille !",
                    es: "👍 ¡Estoy listo para la batalla!",
                    de: "👍 Ich bin bereit für die Schlacht!",
                    it: "👍 Sono pronto per la battaglia!",
                    tr: "👍 Savaşa hazırım!",
                    pt: "👍 Estou pronto para a batalha!",
                    hi: "👍 मैं युद्ध के लिए तैयार हूँ!",
                    zh: "👍 我已准备好战斗！",
                    ja: "👍 戦いの準備は完了だ！");

            case LobbyGreetingType.GoodLuck:
                return LanguageSelectionUI.Localize(
                    "🤝 Good luck to both teams!",
                    ar: "🤝 بالتوفيق لكلا الفريقين!",
                    fa: "🤝 برای هر دو تیم آرزوی موفقیت دارم!",
                    ru: "🤝 Удачи обеим командам!",
                    fr: "🤝 Bonne chance aux deux équipes !",
                    es: "🤝 ¡Buena suerte a ambos equipos!",
                    de: "🤝 Viel Glück beiden Teams!",
                    it: "🤝 Buona fortuna a entrambe le squadre!",
                    tr: "🤝 Her iki takıma da başarılar!",
                    pt: "🤝 Boa sorte para ambas as equipes!",
                    hi: "🤝 दोनों टीमों को शुभकामनाएं!",
                    zh: "🤝 祝双方好运！",
                    ja: "🤝 両チームの健闘を祈る！");

            case LobbyGreetingType.EpicBattle:
                return LanguageSelectionUI.Localize(
                    "⚔️ May the best team prevail!",
                    ar: "⚔️ لتكن معركة ملحمية، ولينتصر الأفضل!",
                    fa: "⚔️ یک نبرد حماسی، باشد که بهترین‌ها پیروز شوند!",
                    ru: "⚔️ Да победит сильнейший в этой эпической битве!",
                    fr: "⚔️ Que la meilleure équipe l'emporte !",
                    es: "⚔️ ¡Que gane el mejor equipo!",
                    de: "⚔️ Möge das beste Team gewinnen!",
                    it: "⚔️ Che vinca la squadra migliore!",
                    tr: "⚔️ En iyi takım kazansın!",
                    pt: "⚔️ Que vença a melhor equipe!",
                    hi: "⚔️ सर्वश्रेष्ठ टीम की जीत हो!",
                    zh: "⚔️ 愿最强队伍获胜！决一胜负！",
                    ja: "⚔️ 最強のチームが勝利を掴まんことを！");

            default:
                return "";
        }
    }
}
