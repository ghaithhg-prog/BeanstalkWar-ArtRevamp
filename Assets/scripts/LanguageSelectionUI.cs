using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LanguageSelectionUI:
/// Provides a comprehensive multi-language system with high-definition national flag textures
/// for English, Arabic (Tunisia/Arab World), Russian, French, Spanish, German, Hindi, Chinese, Japanese, and Portuguese.
/// Translates all game UI, descriptions, character names, roles, announcements, and briefing slides instantly.
/// </summary>
public class LanguageSelectionUI : MonoBehaviour
{
    public static LanguageSelectionUI Instance { get; private set; }
    public static bool IsLanguageModalOpen => Instance != null && Instance.showLanguageModal;

    public static bool IsInActiveMatch
    {
        get
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())
                return true;
            if (PlayerMovement.LocalPlayer != null && PlayerMovement.LocalPlayer.hasSelectedCharacter.Value)
                return true;
            return false;
        }
    }

    public void OpenModal()
    {
        showLanguageModal = true;
    }

    public struct LanguageOption
    {
        public string code;
        public string englishName;
        public string nativeName;
        public string flagEmoji;
        public string region;
        public bool isNativeSupported;

        public LanguageOption(string code, string englishName, string nativeName, string flagEmoji, string region, bool supported = true)
        {
            this.code = code;
            this.englishName = englishName;
            this.nativeName = nativeName;
            this.flagEmoji = flagEmoji;
            this.region = region;
            this.isNativeSupported = supported;
        }
    }

    public static readonly LanguageOption[] AvailableLanguages = new LanguageOption[]
    {
        new LanguageOption("en", "English", "English", "🇬🇧", "Global / North America & Europe", true),
        new LanguageOption("ar", "Arabic", "العربية", "🇸🇦", "المملكة العربية السعودية والوطن العربي / Saudi Arabia & Arab World", true),
        new LanguageOption("it", "Italian", "Italiano", "🇮🇹", "Italia / Italy & Europe", true),
        new LanguageOption("tr", "Turkish", "Türkçe", "🇹🇷", "Türkiye / Turkey & Eurasia", true),
        new LanguageOption("fa", "Persian", "فارسی", "🇮🇷", "ایران و خاورمیانه / Iran & Middle East", true),
        new LanguageOption("ru", "Russian", "Русский", "🇷🇺", "Россия / Eastern Europe & Central Asia", true),
        new LanguageOption("fr", "French", "Français", "🇫🇷", "France & Monde Francophone", true),
        new LanguageOption("es", "Spanish", "Español", "🇪🇸", "España & Latinoamérica", true),
        new LanguageOption("de", "German", "Deutsch", "🇩🇪", "Deutschland, Österreich, Schweiz", true),
        new LanguageOption("hi", "Hindi", "हिन्दी", "🇮🇳", "भारत / India & South Asia", true),
        new LanguageOption("zh", "Chinese", "中文 (简体)", "🇨🇳", "中国 / China & Asia-Pacific", true),
        new LanguageOption("ja", "Japanese", "日本語", "🇯🇵", "日本 / Japan", true),
        new LanguageOption("pt", "Portuguese", "Português", "🇧🇷", "Brasil & Portugal", true)
    };

    // Default to false so the main menu opens immediately with English active;
    // Player can click the language button or press 'L' at any time to switch languages.
    private bool showLanguageModal = false;
    private int selectedIndex = 0;
    private Vector2 listScrollPos = Vector2.zero;

    // Cache of procedural pixel-perfect flag textures (36x24)
    private static readonly Dictionary<string, Texture2D> flagTextureCache = new Dictionary<string, Texture2D>();

    public static string CurrentLanguageCode => PlayerPrefs.GetString("GameLanguage", "en");
    public static bool IsArabic => CurrentLanguageCode == "ar";

    #region Master Translation Dictionary & Localization Engine

    private static Dictionary<string, Dictionary<string, string>> masterDict;

    /// <summary>
    /// Translates text into the currently active game language.
    /// Supports explicit parameters as well as automatic deep lookup in the Master Dictionary.
    /// Arabic strings are automatically shaped and reversed via ArabicFixer for Unity IMGUI.
    /// </summary>
    public static string Localize(string en, string ar = null, string ru = null, string es = null, string fr = null,
                                  string de = null, string hi = null, string zh = null, string ja = null, string pt = null, string fa = null,
                                  string it = null, string tr = null)
    {
        if (string.IsNullOrEmpty(en)) return "";

        string code = CurrentLanguageCode;

        switch (code)
        {
            case "ar":
                if (!string.IsNullOrEmpty(ar)) return ArabicFixer.Fix(ar);
                string dictAr = LookupDictionary(en, "ar");
                return !string.IsNullOrEmpty(dictAr) ? ArabicFixer.Fix(dictAr) : ArabicFixer.Fix(en);

            case "it":
                if (!string.IsNullOrEmpty(it)) return it;
                string dictIt = LookupDictionary(en, "it");
                return !string.IsNullOrEmpty(dictIt) ? dictIt : en;

            case "tr":
                if (!string.IsNullOrEmpty(tr)) return tr;
                string dictTr = LookupDictionary(en, "tr");
                return !string.IsNullOrEmpty(dictTr) ? dictTr : en;

            case "fa":
                if (!string.IsNullOrEmpty(fa)) return ArabicFixer.Fix(fa);
                string dictFa = LookupDictionary(en, "fa");
                if (!string.IsNullOrEmpty(dictFa)) return ArabicFixer.Fix(dictFa);
                if (!string.IsNullOrEmpty(ar)) return ArabicFixer.Fix(ar);
                return en;

            case "ru":
                if (!string.IsNullOrEmpty(ru)) return ru;
                string dictRu = LookupDictionary(en, "ru");
                return !string.IsNullOrEmpty(dictRu) ? dictRu : en;

            case "es":
                if (!string.IsNullOrEmpty(es)) return es;
                string dictEs = LookupDictionary(en, "es");
                return !string.IsNullOrEmpty(dictEs) ? dictEs : en;

            case "fr":
                if (!string.IsNullOrEmpty(fr)) return fr;
                string dictFr = LookupDictionary(en, "fr");
                return !string.IsNullOrEmpty(dictFr) ? dictFr : en;

            case "de":
                if (!string.IsNullOrEmpty(de)) return de;
                string dictDe = LookupDictionary(en, "de");
                return !string.IsNullOrEmpty(dictDe) ? dictDe : en;

            case "hi":
                if (!string.IsNullOrEmpty(hi)) return hi;
                string dictHi = LookupDictionary(en, "hi");
                return !string.IsNullOrEmpty(dictHi) ? dictHi : en;

            case "zh":
                if (!string.IsNullOrEmpty(zh)) return zh;
                string dictZh = LookupDictionary(en, "zh");
                return !string.IsNullOrEmpty(dictZh) ? dictZh : en;

            case "ja":
                if (!string.IsNullOrEmpty(ja)) return ja;
                string dictJa = LookupDictionary(en, "ja");
                return !string.IsNullOrEmpty(dictJa) ? dictJa : en;

            case "pt":
                if (!string.IsNullOrEmpty(pt)) return pt;
                string dictPt = LookupDictionary(en, "pt");
                return !string.IsNullOrEmpty(dictPt) ? dictPt : en;

            default:
                return en;
        }
    }

    private static string LookupDictionary(string key, string langCode)
    {
        if (masterDict == null) InitMasterDictionary();

        string cleanKey = key.Trim();
        if (masterDict.TryGetValue(cleanKey, out var langMap))
        {
            if (langMap.TryGetValue(langCode, out var val))
            {
                return val;
            }
        }

        // Try stripped key without emojis
        string strippedKey = System.Text.RegularExpressions.Regex.Replace(cleanKey, @"[^\w\s\(\)\[\]\:\-\.\,\/]", "").Trim();
        if (!string.IsNullOrEmpty(strippedKey) && strippedKey != cleanKey)
        {
            if (masterDict.TryGetValue(strippedKey, out var strippedMap))
            {
                if (strippedMap.TryGetValue(langCode, out var val))
                {
                    return val;
                }
            }
        }

        return null;
    }

    private static void InitMasterDictionary()
    {
        masterDict = new Dictionary<string, Dictionary<string, string>>(System.StringComparer.OrdinalIgnoreCase);

        void Add(string en, string ar, string ru, string fr, string es, string de, string hi, string zh, string ja, string pt, string it = null, string tr = null, string fa = null)
        {
            var dict = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "ar", ar },
                { "fa", string.IsNullOrEmpty(fa) ? ar : fa },
                { "ru", ru },
                { "fr", fr },
                { "es", es },
                { "de", de },
                { "hi", hi },
                { "zh", zh },
                { "ja", ja },
                { "pt", pt },
                { "it", string.IsNullOrEmpty(it) ? en : it },
                { "tr", string.IsNullOrEmpty(tr) ? en : tr },
                { "en", en }
            };
            masterDict[en] = dict;
        }

        void SetLang(string en, string it, string tr)
        {
            if (masterDict.TryGetValue(en, out var dict))
            {
                dict["it"] = it;
                dict["tr"] = tr;
            }
        }

        // --- Core Game Titles & Menus ---
        Add("THE SACRED BEANSTALK: FOREST WAR", "حرب نبتة الفاصولياء المقدسة", "ВОЙНА СВЯЩЕННОГО БОБА", "LA GUERRE DU HARICOT SACRÉ", "LA GUERRA DEL TALLO SAGRADO", "DER KRIEG DER BOHNENRANKE", "पवित्र सेम के पौधे का युद्ध", "圣豆蔓森林之战", "聖なる豆の木の戦い", "A GUERRA DO FEIJÃO SAGRADO");
        Add("Inscribed on Ancient Oak Bark Two Millennia Past", "نُقشت على لحاء البلوط القديم منذ ألفي عام", "Высечено на коре древнего дуба два тысячелетия назад", "Gravé sur l'écorce de chêne ancestral", "Inscrito en la corteza de roble ancestral", "Vor zwei Jahrtausenden in alte Eichenrinde geritzt", "दो सहस्राब्दी पहले प्राचीन ओक की छाल पर अंकित", "两千年前铭刻于古橡木树皮之上", "二千年の昔、古代の樫の樹皮に刻まれし記録", "Gravado na casca de carvalho ancestral há dois milênios");
        Add("OFFLINE EXPEDITION (SOLO vs BOTS)", "معركة فردية (ضد البوتات)", "ОДИНОЧНЫЙ БОЙ (ПРОТИВ БОТОВ)", "EXPÉDITION SOLO (vs BOTS)", "EXPEDICIÓN INDIVIDUAL (SOLO vs BOTS)", "OFFLINE-EXPEDITION (SOLO vs BOTS)", "ऑफ़लाइन अभियान (एकल बनाम बॉट्स)", "离线远征 (单人对人机)", "オフライン遠征 (ソロ vs ボット)", "EXPEDIÇÃO OFFLINE (SOLO vs BOTS)");
        Add("REALM EXPEDITION (ONLINE MULTIPLAYER)", "معركة أونلاين (متعدد اللاعبين)", "СЕТЕВАЯ ИГРА (ОНЛАЙН)", "EXPÉDITION EN LIGNE (MULTIJOUEUR)", "EXPEDICIÓN ONLINE (MULTIJUGADOR)", "REALM-EXPEDITION (ONLINE-MEHRSPIELER)", "ऑनलाइन अभियान (मल्टीप्लेयर)", "在线远征 (多人联机)", "オンライン遠征 (マルチプレイ)", "EXPEDIÇÃO ONLINE (MULTIJOGADOR)");
        Add("TRIBE SANCTUARY (FRIENDLY LOBBY)", "غرفة خاصة للأصدقاء (لوبي)", "ДРУЖЕСКОЕ ЛОББИ (СВОЯ КОМНАТА)", "SANCTUAIRE D'AMIS (SALON PRIVÉ)", "SANTUARIO DE AMIGOS (LOBBY PRIVADO)", "STAMMESHEILIGTUM (FREUNDES-LOBBY)", "मित्र अभयारण्य (निजी लॉबी)", "部落圣所 (好友房间)", "仲間たちの聖域 (フレンドロビー)", "SANTUÁRIO DA TRIBO (LOBBY PRIVADO)");
        Add("ENTER GROVE", "ابدأ المعركة", "ВОЙТИ В ЛЕС", "ENTRER DANS LE BOIS", "ENTRAR AL BOSQUE", "HAIN BETRETEN", "ग्रोव में प्रवेश करें", "进入丛林", "森に入る", "ENTRAR NO BOSQUE");
        Add("EMBARK ONLINE", "دخول أونلاين", "ИГРАТЬ ОНЛАЙН", "JOUER EN LIGNE", "ENTRAR ONLINE", "ONLINE STARTEN", "ऑनलाइन शुरू करें", "在线出发", "オンラインへ", "EMBARCAR ONLINE");
        Add("GATHER TRIBE", "إنشاء اللوبي", "СОЗДАТЬ ЛОББИ", "CRÉER LE SALON", "CREAR LOBBY", "STAMM VERSAMMELN", "कबीला इकट्ठा करें", "召集部落", "仲間を集める", "REUNIR A TRIBO");

        // --- Teams & Allegiances ---
        Add("TREE PROTECTORS", "حماة الشجرة", "ЗАЩИТНИКИ ДРЕВА", "PROTECTEURS DE L'ARBRE", "PROTECTORES DEL ÁRBOL", "BAUM-BESCHÜTZER", "वृक्ष रक्षक", "巨树守护者", "木の守護者", "PROTETORES DA ÁRVORE");
        Add("TREE DESTROYERS", "مدمر الشجرة", "РАЗРУШИТЕЛИ ДРЕВА", "DESTRUCTEURS DE L'ARBRE", "DESTRUCTORES DEL ÁRBOL", "BAUM-ZERSTÖRER", "वृक्ष विनाशक", "巨树破坏者", "木の破壊者", "DESTRUIDORES DA ÁRVORE");
        Add("GUARDIANS OF LIFE", "حراس الحياة", "СТРАЖИ ЖИЗНИ", "GARDIENS DE LA VIE", "GUARDIANES DE LA VIDA", "WÄCHTER DES LEBENS", "जीवन के संरक्षक", "生命卫士", "生命の守護者", "GUARDIÕES DA VIDA");
        Add("AXEMEN OF CHAOS", "حطابو الفوضى", "СЕКИРЫ ХАОСА", "BÛCHERONS DU CHAOS", "LEÑADORES DEL CAOS", "ÄXTE DES CHAOS", "अराजकता के कुल्हाड़ीबाज", "混沌斧手", "混沌の木こり", "LENHADORES DO CAOS");
        Add("BECOME A PROTECTOR", "انضم إلى الحماة", "СТАТЬ ЗАЩИТНИКОМ", "DEVENIR UN PROTECTEUR", "SÉ UN PROTECTOR", "BESCHÜTZER WERDEN", "रक्षक बनें", "成为守护者", "守護者になる", "TORNAR-SE PROTETOR");
        Add("BECOME A DESTROYER", "انضم إلى المدمرين", "СТАТЬ РАЗРУШИТЕЛЕМ", "DEVENIR UN DESTRUCTEUR", "SÉ UN DESTRUCTOR", "ZERSTÖRER WERDEN", "विनाशक बनें", "成为破坏者", "破壊者になる", "TORNAR-SE DESTRUIDOR");
        Add("SACRED ALLEGIANCE: CHOOSE THY PATH", "الولاء المقدس: اختر مسارك", "СВЯЩЕННАЯ ВЕРНОСТЬ: ВЫБЕРИ СВОЙ ПУТЬ", "ALLÉGEANCE SACRÉE : CHOISISSEZ VOTRE VOIE", "LEALTAD SAGRADA: ELIGE TU CAMINO", "HEILIGE TREUE: WÄHLE DEINEN PFAD", "पवित्र निष्ठा: अपना मार्ग चुनें", "神圣誓约：选择你的道路", "聖なる忠誠：汝の道を選べ", "LEALDADE SAGRADA: ESCOLHA SEU CAMINHO");

        // --- Boss Titles ---
        Add("FOREST GOLIATH [LV 1]", "عملاق الغابة [مستوى 1]", "ЛЕСНОЙ ГОЛИАФ [УР 1]", "GOLIATH DES BOIS [NV 1]", "GOLIAT DEL BOSQUE [NV 1]", "WALDGOLIATH [STUFE 1]", "वन गोलियत [स्तर 1]", "森林巨人 [1级]", "森のゴライアス [LV 1]", "GOLIAS DA FLORESTA [NV 1]");
        Add("INFERNO TITAN [LV 2]", "تيتان الحمم [مستوى 2]", "ОГНЕННЫЙ ТИТАН [УР 2]", "TITAN DE FEU [NV 2]", "TITÁN INFERNAL [NV 2]", "INFERNOTITAN [STUFE 2]", "नरक टाइटन [स्तर 2]", "地狱泰坦 [2级]", "インフェルノタイタン [LV 2]", "TITÃ INFERNAL [NV 2]");
        Add("ANCIENT OVERLORD [LV 3]", "الحاكم القديم [مستوى 3]", "ДРЕВНИЙ ПОВЕЛИТЕЛЬ [УР 3]", "SEIGNEUR ANCESTRAL [NV 3]", "SEÑOR ANCESTRAL [NV 3]", "ANTIKER HERRSCHER [STUFE 3]", "प्राचीन अधिपति [स्तर 3]", "远古霸主 [3级]", "古代の覇王 [LV 3]", "SOBERANO ANCESTRAL [NV 3]");

        // --- Grand Game Announcements ---
        Add("ALL 3 GUARDIAN BOSSES HAVE BEEN DEFEATED!", "تم القضاء على الزعماء الحراس الثلاثة بالكامل!", "ВСЕ 3 БОССА ПОВЕРЖЕНЫ!", "LES 3 BOSS GARDIENS ONT ÉTÉ VAINCUS !", "¡LOS 3 JEFES GUARDIANES HAN CAÍDO!", "ALLE 3 WÄCHTER-BOSSE WURDEN BESIEGT!", "सभी 3 अभिभावक बॉस पराजित हो गए हैं!", "全部3个守护者Boss均已被击败！", "3体の守護者ボスがすべて討伐された！", "TODOS OS 3 CHEFES GUARDIÕES FORAM DERROTADOS!");
        Add("YOU CAN NOW DESTROY THE SACRED TREE!", "يمكنك الآن تدمير الشجرة المقدسة!", "ТЕПЕРЬ ВЫ МОЖЕТЕ СРУБИТЬ ДЕРЕВО!", "VOUS POUVEZ MAINTENANT DÉTRUIRE L'ARBRE SACRÉ !", "¡AHORA PUEDES DESTRUIR EL ÁRBOL SAGRADO!", "IHR KÖNNT NUN DEN HEILIGEN BAUM ZERSTÖREN!", "अब आप पवित्र वृक्ष को नष्ट कर सकते हैं!", "你现在可以摧毁圣树了！", "今すぐ聖なる木を破壊できる！", "AGORA VOCÊ PODE DESTRUIR A ÁRVORE SAGRADA!");
        Add("THE CRYSTAL IS READY — YOU CAN TAKE IT NOW!", "الكريستالة جاهزة — يمكنك أخذها الآن!", "КРИСТАЛЛ ГОТОВ — ЗАБЕРИТЕ ЕГО!", "LE CRISTAL EST PRÊT — PRENEZ-LE MAINTENANT !", "¡EL CRISTAL ESTÁ LISTO — PUEDES TOMARLO AHORA!", "DER KRISTALL IST BEREIT — NIMM IHN JETZT!", "क्रिस्टल तैयार है — आप इसे अभी ले सकते हैं!", "水晶已就绪 — 你现在可以拾取了！", "クリスタルの準備完了 — 今すぐ運べ！", "O CRISTAL ESTÁ PRONTO — PEGUE-O AGORA!");
        Add("TREE SHIELD ACTIVE", "درع الشجرة نشط", "ЩИТ ДЕРЕВА АКТИВЕН", "BOUCLIER ACTIF", "ESCUDO ACTIVO", "BAUMSCHILD AKTIV", "वृक्ष ढाल सक्रिय", "巨树护盾激活中", "ツリーシールド有効", "ESCUDO DA ÁRVORE ATIVO");

        // --- Common Buttons & Controls ---
        Add("START GAME", "ابدأ اللعبة", "НАЧАТЬ ИГРУ", "COMMENCER LE JEU", "INICIAR JUEGO", "SPIEL STARTEN", "खेल शुरू करें", "开始游戏", "ゲーム開始", "INICIAR JOGO");
        Add("CLOSE", "إغلاق", "ЗАКРЫТЬ", "FERMER", "CERRAR", "SCHLIESSEN", "बंद करें", "关闭", "閉じる", "FECHAR");
        Add("BATTLE AGAIN", "العب من جديد", "ИГРАТЬ СНОВА", "COMBATTRE À NOUVEAU", "LUCHAR DE NUEVO", "ERNEUT KÄMPFEN", "फिर से लड़ें", "再次战斗", "もう一度戦う", "JOGAR NOVAMENTE");
        Add("RETURN TO GROVE", "العودة إلى الغابة", "ВЕРНУТЬСЯ В ГРОТ", "RETOUR AU BOSQUET", "VOLVER A LA ARBOLEDA", "ZUM HAIN ZURÜCK", "ग्रोव पर वापस लौटें", "返回树林", "木立ちへ戻る", "VOLTAR AO BOSQUE");
        Add("COMMENCE EXPEDITION", "انطلق في الرحلة", "НАЧАТЬ ПОХОД", "COMMENCER L'EXPÉDITION", "INICIAR EXPEDICIÓN", "EXPEDITION STARTEN", "अभियान शुरू करें", "开始远征", "遠征を開始", "INICIAR EXPEDIÇÃO");
        Add("CONFIRM HERO", "تأكيد البطل", "ВЫБРАТЬ ГЕРОЯ", "CONFIRMER LE HÉROS", "CONFIRMAR HÉROE", "HELD BESTÄTIGEN", "नायक की पुष्टि करें", "确认英雄", "ヒーローを確定", "CONFIRMAR HERÓI");
        Add("EQUIP", "تجهيز", "НАДЕТЬ", "ÉQUIPER", "EQUIPAR", "AUSRÜSTEN", "सुसज्जित करें", "装备", "装備する", "EQUIPAR");
        Add("EQUIPPED", "مجهز", "НАДЕТО", "ÉQUIPÉ", "EQUIPADO", "AUSGERÜSTET", "सुसज्जित", "已装备", "装備中", "EQUIPADO");
        Add("CLAIM", "استلام", "ЗАБРАТЬ", "RÉCLAMER", "RECLAMAR", "BEANSPRUCHEN", "दावा करें", "领取", "獲得する", "REIVINDICAR");
        Add("SELECT", "اختيار", "ВЫБРАТЬ", "CHOISIR", "SELECCIONAR", "WÄHLEN", "चुनें", "选择", "選択", "SELECIONAR");
        Add("SELECTED", "مختارة", "ВЫБРАНО", "SÉLECTIONNÉ", "SELECCIONADO", "AUSGEWÄHLT", "चयनित", "已选择", "選択済み", "SELECIONADO");
        Add("PREVIOUS", "السابق", "НАЗАД", "PRÉCÉDENT", "ANTERIOR", "ZURÜCK", "पिछला", "上一页", "前へ", "ANTERIOR");
        Add("NEXT", "التالي", "ВПЕРЕД", "SUIVANT", "SIGUIENTE", "WEITER", "अगला", "下一页", "次へ", "PRÓXIMO");

        // --- All 25 Character Names ---
        Add("Warrior", "المحارب", "Воин", "Guerrier", "Guerrero", "Krieger", "योद्धा", "战士", "戦士", "Guerreiro");
        Add("Mage", "الساحر", "Маг", "Mage", "Mago", "Magier", "जादूगर", "法师", "魔法使い", "Mago");
        Add("Archer", "الرامي", "Лучник", "Archer", "Arquero", "Bogenschütze", "धनुर्धर", "弓箭手", "弓兵", "Arqueiro");
        Add("Tank", "المدرع", "Танк", "Tank", "Tanque", "Tank", "टैंक", "坦克", "タンク", "Tanque");
        Add("Assassin", "السفاح", "Ассасин", "Assassin", "Asesino", "Attentäter", "हत्यारा", "刺客", "暗殺者", "Assassino");
        Add("Bomber", "المفجر", "Подрывник", "Bombardier", "Bombardero", "Bomber", "बमवर्षक", "炸弹人", "ボンバー", "Bombardeiro");
        Add("Druid", "الدرويد", "Друид", "Druide", "Druida", "Druide", "ड्र्यूड", "德鲁伊", "ドルイド", "Druida");
        Add("Berserker", "المحارب الهائج", "Берсерк", "Berserker", "Bersérker", "Berserker", "बर्बर योद्धा", "狂战士", "バーサーカー", "Berserker");
        Add("Paladin", "الفارس المقدس", "Паладин", "Paladin", "Paladín", "Paladin", "धर्मयोद्धा", "圣骑士", "パラディン", "Paladino");
        Add("Ranger", "الحارس الجوال", "Рейнджер", "Rôdeur", "Guardabosques", "Waldläufer", "वनरक्षक", "游侠", "レンジャー", "Patrulheiro");
        Add("Necromancer", "مستحضر الأرواح", "Некромант", "Nécromancien", "Nigromante", "Totenbeschwörer", "प्रेतविद्याविद", "死灵法师", "ネクロマンサー", "Necromante");
        Add("Shaman", "الشامان", "Шаман", "Chaman", "Chamán", "Schamane", "तांत्रिक", "萨满", "シャーマン", "Xamã");
        Add("Rogue", "اللص المحتال", "Разбойник", "Voleur", "Pícaro", "Schurke", "दुष्ट", "潜行者", "ローグ", "Ladino");
        Add("Cleric", "الكاهن", "Жрец", "Clerc", "Clérigo", "Kleriker", "पादरी", "牧师", "クレリック", "Clérigo");
        Add("Elementalist", "سيد العناصر", "Элементалист", "Élémentaliste", "Elementalista", "Elementarist", "तत्ववादी", "元素使", "エレメンタリスト", "Elementalista");
        Add("Barbarian", "الهمجي", "Варвар", "Barbare", "Bárbaro", "Barbar", "बर्बर", "野蛮人", "バーバリアン", "Bárbaro");
        Add("Enchantress", "الساحرة الفاتنة", "Чародейка", "Enchanteresse", "Hechicera", "Zauberin", "मोहिनी", "幻术师", "魅惑の術士", "Feiticeira");
        Add("Knight", "الفارس النبيل", "Рыцарь", "Chevalier", "Caballero", "Ritter", "शूरवीर", "骑士", "騎士", "Cavaleiro");
        Add("Alchemist", "الخيميائي", "Алхимик", "Alchimiste", "Alquimista", "Alchemist", "कीमियागर", "炼金术士", "錬金術師", "Alquimista");
        Add("Summoner", "المستدعي", "Призыватель", "Invocateur", "Invocador", "Beschwörer", "आवाह्नकर्ता", "召唤师", "サモナー", "Invocador");
        Add("Beastmaster", "مروّض الوحوش", "Повелитель зверей", "Maître des bêtes", "Maestro de bestias", "Bestienmeister", "पशुपालक", "兽王", "ビーストマスター", "Mestre das Feras");
        Add("Templar", "فارس المعبد", "Тамплиер", "Templier", "Templario", "Templer", "मंदिर योद्धा", "圣殿骑士", "テンプル騎士", "Templário");
        Add("Shadowblade", "نصل الظل", "Теневой клинок", "Lame d'ombre", "Hoja sombría", "Schattenklinge", "छाया तलवार", "影刃", "シャドウブレード", "Lâmina Sombria");
        Add("Windwalker", "عابر الرياح", "Ветроход", "Marcheur du vent", "Caminante del viento", "Windläufer", "पवन पथिक", "风行者", "ウィンドウォーカー", "Andarilho do Vento");
        Add("Ironclad", "المصفح الحديدي", "Броненосец", "Cuirassé", "Acorazado", "Eisengepanzerter", "लौह कवच", "铁甲卫士", "アイアンクラッド", "Blindado de Ferro");

        // --- Roles & Combat Stats ---
        Add("TANK", "مدرع", "ТАНК", "TANK", "TANQUE", "TANK", "टैंक", "坦克", "タンク", "TANQUE");
        Add("DAMAGE", "هجوم", "УРОН", "DÉGÂTS", "DAÑO", "SCHADEN", "क्षति", "伤害", "ダメージ", "DANO");
        Add("SUPPORT", "دعم", "ПОДДЕРЖКА", "SOUTIEN", "APOYO", "UNTERSTÜTZUNG", "समर्थन", "辅助", "サポート", "SUPORTE");
        Add("CONTROL", "تحكم", "КОНТРОЛЬ", "CONTRÔLE", "CONTROL", "KONTROLLE", "नियंत्रण", "控制", "コントロール", "CONTROLE");
        Add("SPECIALIST", "مختص", "СПЕЦИАЛИСТ", "SPÉCIALISTE", "ESPECIALISTA", "SPEZIALIST", "विशेषज्ञ", "专家", "スペシャリスト", "ESPECIALISTA");
        Add("HERO", "بطل", "ГЕРОЙ", "HÉROS", "HÉROE", "HELD", "नायक", "英雄", "ヒーロー", "HERÓI");

        Add("HP", "الصحة", "ЗДОРОВЬЕ", "PV", "SALUD", "LEBEN", "स्वास्थ्य", "生命", "HP", "VIDA");
        Add("ATK", "الهجوم", "АТАКА", "ATTAQUE", "ATAQUE", "ANGRIFF", "हमला", "攻击", "攻撃", "ATAQUE");
        Add("SPD", "السرعة", "СКОРОСТЬ", "VITESSE", "VELOCIDAD", "TEMPO", "गति", "速度", "速度", "VELOCIDADE");
        Add("RNG", "المدى", "ДАЛЬНОСТЬ", "PORTÉE", "RANGO", "REICHWEITE", "सीमा", "范围", "射程", "ALCANCE");
        Add("COOLDOWN", "زمن التهدئة", "ПЕРЕЗАРЯДКА", "RECHARGE", "RECARGA", "ABKLINGZEIT", "कूलडाउन", "冷却", "クールダウン", "RECARGA");
        Add("JUMP", "القفز", "ПРЫЖОК", "SAUT", "SALTO", "SPRUNG", "छलांग", "跳跃", "ジャンプ", "SALTO");
        Add("Close-Range Melee", "قتال قريب", "Ближний бой", "Mêlée rapprochée", "Cuerpo a cuerpo", "Nahkampf", "हाथापाई युद्ध", "近战肉搏", "近接攻撃", "Corpo a corpo");
        Add("Ranged Projectile", "مقذوف عن بعد", "Дальний бой", "Projectile à distance", "Proyectil a distancia", "Fernkampf-Projektil", "दूरी प्रक्षेप्य", "远程弹道", "遠距離射撃", "Projétil à distância");

        // --- Shop & Currency ---
        Add("Acorns", "بلوط", "Жёлуди", "Glands", "Bellotas", "Eicheln", "बलूत", "橡子", "ドングリ", "Bellotas");
        Add("Gems", "جواهر", "Самоцветы", "Gemmes", "Gemas", "Edelsteine", "रत्न", "宝石", "宝石", "Gemas");
        Add("Emporium", "المتجر", "Магазин", "Boutique", "Tienda", "Basar", "बाज़ार", "商店", "交易所", "Empório");
        Add("Gift", "هدية", "Подарок", "Cadeau", "Regalo", "Geschenk", "उपहार", "礼物", "ギフト", "Presente");

        // --- Navigation & In-Game Menus ---
        Add("BACK", "رجوع للخلف", "НАЗАД", "RETOUR", "ATRÁS", "ZURÜCK", "वापस", "返回", "戻る", "VOLTAR");
        Add("BACK TO MODES", "الرجوع لاختيار الطور", "НАЗАД К РЕЖИМАМ", "RETOUR AUX MODES", "VOLVER A MODOS", "ZURÜCK ZU MODI", "मोड पर वापस जाएं", "返回模式选择", "モード選択へ戻る", "VOLTAR AOS MODOS");
        Add("BACK TO TEAMS", "الرجوع لاختيار الفريق", "НАЗАД К КОМАНДАМ", "RETOUR AUX ÉQUIPES", "VOLVER A EQUIPOS", "ZURÜCK ZU TEAMS", "टीम चयन पर वापस जाएं", "返回队伍选择", "チーム選択へ戻る", "VOLTAR AOS TIMES");
        Add("LEAVE LOBBY", "مغادرة الغرفة", "ПОКИНУТЬ ЛОББИ", "QUITTER LE SALON", "SALIR DEL LOBBY", "LOBBY VERLASSEN", "लॉबी छोड़ें", "离开房间", "ロビーを退出", "SAIR DO LOBBY");
        Add("CHANGE LANGUAGE", "تغيير لغة اللعبة", "СМЕНИТЬ ЯЗЫК", "CHANGER DE LANGUE", "CAMBIAR IDIOMA", "SPRACHE ÄNDERN", "भाषा बदलें", "更改语言", "言語を変更", "MUDAR IDIOMA");
        Add("RESUME MATCH", "مواصلة اللعب", "ПРОДОЛЖИТЬ БОЙ", "CONTINUER LA PARTIE", "CONTINUAR PARTIDA", "WEITERSPIELEN", "खेल जारी रखें", "继续比赛", "戦闘を続ける", "CONTINUAR PARTIDA");
        Add("PAUSE & SETTINGS", "إيقاف مؤقت وإعدادات اللعبة", "ПАУЗА И НАСТРОЙКИ", "PAUSE ET PARAMÈTRES", "PAUSA Y AJUSTES", "PAUSE & EINSTELLUNGEN", "विराम और सेटिंग्स", "暂停与设置", "一時停止と設定", "PAUSA E CONFIGURAÇÕES");
        Add("AUDIO & SOUND", "الصوت والمؤثرات", "ЗВУК И МУЗЫКА", "AUDIO ET SONS", "SONIDO Y AUDIO", "AUDIO & SOUND", "ऑडियो और ध्वनि", "音频与音效", "オーディオとサウンド", "ÁUDIO E SONS");
        Add("SOUND: ON", "الصوت: مفعّل", "ЗВУК: ВКЛ", "SON : ACTIVÉ", "SONIDO: ACTIVADO", "SOUND: AN", "ध्वनि: चालू", "音效: 开启", "サウンド: オン", "SOM: ATIVADO");
        Add("SOUND: MUTED", "الصوت: مكتوم", "ЗВУК: ВЫКЛ", "SON : COUPÉ", "SONIDO: SILENCIADO", "SOUND: AUS", "ध्वनि: मौन", "音效: 静音", "サウンド: ミュート", "SOM: MUTADO");
        Add("RETURN TO MAIN MENU", "العودة إلى القائمة الرئيسية", "ГЛАВНОЕ МЕНЮ", "RETOUR AU MENU PRINCIPAL", "VOLVER AL MENÚ", "ZUM HAUPTMENÜ", "मुख्य मेनू पर वापस जाएं", "返回主菜单", "メインメニューへ", "VOLTAR AO MENU");
        Add("QUIT GAME", "الخروج من اللعبة", "ВЫЙТИ ИЗ ИГРЫ", "QUITTER LE JEU", "SALIR DEL JUEGO", "SPIEL BEENDEN", "खेल से बाहर निकलें", "退出游戏", "ゲームを終了", "SAIR DO JOGO");

        // --- Altar Translations ---
        Add("SACRED ALTAR", "المذبح المقدس", "СВЯЩЕННЫЙ АЛТАРЬ", "AUTEL SACRÉ", "ALTAR SAGRADO", "HEILIGER ALTAR", "पवित्र वेदी", "神圣祭坛", "聖なる祭壇", "ALTAR SAGRADO", it: "ALTARE SACRO", tr: "KUTSAL SUNAK");
        Add("✦ SACRED ALTAR ✦", "✦ المذبح المقدس ✦", "✦ СВЯЩЕННЫЙ АЛТАРЬ ✦", "✦ AUTEL SACRÉ ✦", "✦ ALTAR SAGRADO ✦", "✦ HEILIGER ALTAR ✦", "✦ पवित्र वेदी ✦", "✦ 神圣祭坛 ✦", "✦ 聖なる祭壇 ✦", "✦ ALTAR SAGRADO ✦", it: "✦ ALTARE SACRO ✦", tr: "✦ KUTSAL SUNAK ✦");
        Add("ALTAR", "المذبح", "АЛТАРЬ", "AUTEL", "ALTAR", "ALTAR", "वेदी", "祭坛", "祭壇", "ALTAR", it: "ALTARE", tr: "SUNAK");

        // --- Comprehensive Italian (it) & Turkish (tr) Dictionary Enrichment ---
        SetLang("THE SACRED BEANSTALK: FOREST WAR", "LA GUERRA DEL FAGIOLO SACRO: BATTAGLIA DELLA FORESTA", "KUTSAL FASULYE SIRIĞI: ORMAN SAVAŞI");
        SetLang("Inscribed on Ancient Oak Bark Two Millennia Past", "Inciso sulla corteccia di quercia antica due millenni fa", "İki bin yıl önce kadim meşe kabuğuna kazınmış");
        SetLang("OFFLINE EXPEDITION (SOLO vs BOTS)", "SPEDIZIONE OFFLINE (SOLO vs BOT)", "ÇEVRİMDIŞI SEFER (BOTLARA KARŞI TEK BAŞINA)");
        SetLang("REALM EXPEDITION (ONLINE MULTIPLAYER)", "SPEDIZIONE DEL REGNO (MULTIGIOCATORE ONLINE)", "DİYAR SEFERİ (ÇEVRİMİÇİ ÇOK OYUNCULU)");
        SetLang("TRIBE SANCTUARY (FRIENDLY LOBBY)", "SANTUARIO DELLA TRIBÙ (LOBBY PRIVATA)", "KABİLE SIĞINAĞI (DOST LOBİSİ)");
        SetLang("ENTER GROVE", "ENTRA NEL BOSCO", "KORULUĞA GİR");
        SetLang("EMBARK ONLINE", "GIOCA ONLINE", "ÇEVRİMİÇİ BAŞLA");
        SetLang("GATHER TRIBE", "RADUNA LA TRIBÙ", "KABİLEYİ TOPLA");

        SetLang("TREE PROTECTORS", "PROTETTORI DELL'ALBERO", "AĞAÇ KORUYUCULARI");
        SetLang("TREE DESTROYERS", "DISTRUTTORI DELL'ALBERO", "AĞAÇ YOK EDİCİLERİ");
        SetLang("GUARDIANS OF LIFE", "GUARDIANI DELLA VITA", "HAYAT MUHAFIZLARI");
        SetLang("AXEMEN OF CHAOS", "BOSCAIOLI DEL CAOS", "KAOS BALTACILARI");
        SetLang("BECOME A PROTECTOR", "DIVENTA UN PROTETTORE", "KORUYUCU OL");
        SetLang("BECOME A DESTROYER", "DIVENTA UN DISTRUTTORE", "YOK EDİCİ OL");
        SetLang("SACRED ALLEGIANCE: CHOOSE THY PATH", "LEALTÀ SACRA: SCEGLI IL TUO CAMMINO", "KUTSAL BAĞLILIK: YOLUNU SEÇ");

        SetLang("FOREST GOLIATH [LV 1]", "GOLIA DELLA FORESTA [LV 1]", "ORMAN GOLYATI [SEV 1]");
        SetLang("INFERNO TITAN [LV 2]", "TITANO DELL'INFERNO [LV 2]", "CEHENNEM TİTANI [SEV 2]");
        SetLang("ANCIENT OVERLORD [LV 3]", "SIGNORE ANCESTRALE [LV 3]", "KADİM HÜKÜMDAR [SEV 3]");

        SetLang("ALL 3 GUARDIAN BOSSES HAVE BEEN DEFEATED!", "TUTTI E 3 I BOSS GUARDIANI SONO STATI SCONFITTI!", "3 MUHAFIZ PATRONUN HEPSİ YENİLDİ!");
        SetLang("YOU CAN NOW DESTROY THE SACRED TREE!", "ORA PUOI DISTRUGGERE L'ALBERO SACRO!", "ARTIK KUTSAL AĞACI YOK EDEBİLİRSİNİZ!");
        SetLang("THE CRYSTAL IS READY — YOU CAN TAKE IT NOW!", "IL CRISTALLO È PRONTO — PUOI PRENDERLO ORA!", "KRİSTAL HAZIR — ŞİMDİ ALABİLİRSİNİZ!");
        SetLang("TREE SHIELD ACTIVE", "SCUDO DELL'ALBERO ATTIVO", "AĞAÇ KALKANI AKTİF");

        SetLang("START GAME", "INIZIA GIOCO", "OYUNU BAŞLAT");
        SetLang("CLOSE", "CHIUDI", "KAPAT");
        SetLang("BATTLE AGAIN", "GIOCA DI NUOVO", "TEKRAR SAVAŞ");
        SetLang("RETURN TO GROVE", "TORNA AL BOSCO", "KORULUĞA DÖN");
        SetLang("COMMENCE EXPEDITION", "INIZIA SPEDIZIONE", "SEFERİ BAŞLAT");
        SetLang("CONFIRM HERO", "CONFERMA EROE", "KAHRAMANI ONAYLA");
        SetLang("EQUIP", "EQUIPAGGIA", "KUŞAN");
        SetLang("EQUIPPED", "EQUIPAGGIATO", "KUŞANILDI");
        SetLang("CLAIM", "RISCATTA", "AL");
        SetLang("SELECT", "SELEZIONA", "SEÇ");
        SetLang("SELECTED", "SELEZIONATO", "SEÇİLDİ");
        SetLang("PREVIOUS", "PRECEDENTE", "ÖNCEKİ");
        SetLang("NEXT", "SUCCESSIVO", "SONRAKİ");

        SetLang("Warrior", "Guerriero", "Savaşçı");
        SetLang("Mage", "Mago", "Büyücü");
        SetLang("Archer", "Arciere", "Okçu");
        SetLang("Tank", "Corazzato", "Tank");
        SetLang("Assassin", "Assassino", "Suikastçı");
        SetLang("Bomber", "Bombardiere", "Bombacı");
        SetLang("Druid", "Druido", "Druid");
        SetLang("Berserker", "Berserker", "Çılgın Savaşçı");
        SetLang("Paladin", "Paladino", "Paladin");
        SetLang("Ranger", "Guardaboschi", "Kolcu");
        SetLang("Necromancer", "Negromante", "Ölüm Büyücüsü");
        SetLang("Shaman", "Sciamano", "Şaman");
        SetLang("Rogue", "Ladro", "Haydut");
        SetLang("Cleric", "Chierico", "Rahip");
        SetLang("Elementalist", "Elementalista", "Element Uzmanı");
        SetLang("Barbarian", "Barbaro", "Barbar");
        SetLang("Enchantress", "Incantatrice", "Büyücü Kadın");
        SetLang("Knight", "Cavaliere", "Şövalye");
        SetLang("Alchemist", "Alchimista", "Simyacı");
        SetLang("Summoner", "Evocatore", "Çağırıcı");
        SetLang("Beastmaster", "Signore delle Bestie", "Canavar Terbiyecisi");
        SetLang("Templar", "Templare", "Tapınakçı");
        SetLang("Shadowblade", "Lama dell'Ombra", "Gölge Bıçağı");
        SetLang("Windwalker", "Camminatore del Vento", "Rüzgar Gezgini");
        SetLang("Ironclad", "Corazzato di Ferro", "Demir Zırhlı");

        SetLang("TANK", "TANK", "TANK");
        SetLang("DAMAGE", "DANNO", "HASAR");
        SetLang("SUPPORT", "SUPPORTO", "DESTEK");
        SetLang("CONTROL", "CONTROLLO", "KONTROL");
        SetLang("SPECIALIST", "SPECIALISTA", "UZMAN");
        SetLang("HERO", "EROE", "KAHRAMAN");
        SetLang("HP", "SALUTE", "CAN");
        SetLang("ATK", "ATTACCO", "SALDIRI");
        SetLang("SPD", "VELOCITÀ", "HIZ");
        SetLang("RNG", "GITTATA", "MENZİL");
        SetLang("COOLDOWN", "RICARICA", "BEKLEME SÜRESİ");
        SetLang("JUMP", "SALTO", "ZIPLAMA");
        SetLang("Close-Range Melee", "Mischia ravvicinata", "Yakın Dövüş");
        SetLang("Ranged Projectile", "Proiettile a distanza", "Menzilli Fırlatma");

        SetLang("Acorns", "Ghiande", "Palamutlar");
        SetLang("Gems", "Gemme", "Mücevherler");
        SetLang("Emporium", "Emporio", "Mağaza");
        SetLang("Gift", "Regalo", "Hediye");
        SetLang("BACK", "INDIETRO", "GERİ");
        SetLang("BACK TO MODES", "TORNA ALLE MODALITÀ", "MODLARA DÖN");
        SetLang("BACK TO TEAMS", "TORNA ALLE SQUADRE", "TAKIMLARA DÖN");
        SetLang("LEAVE LOBBY", "LASCIA LA STANZA", "LOBİDEN AYRIL");
        SetLang("CHANGE LANGUAGE", "CAMBIA LINGUA", "DİLİ DEĞİŞTİR");
        SetLang("RESUME MATCH", "RIPRENDI PARTITA", "OYUNA DEVAM ET");
        SetLang("PAUSE & SETTINGS", "PAUSA E IMPOSTAZIONI", "DURAKLAT & AYARLAR");
        SetLang("AUDIO & SOUND", "AUDIO E SUONO", "SES VE MÜZİK");
        SetLang("SOUND: ON", "SUONO: ATTIVO", "SES: AÇIK");
        SetLang("SOUND: MUTED", "SUONO: MUTO", "SES: KAPALI");
        SetLang("RETURN TO MAIN MENU", "TORNA AL MENU PRINCIPALE", "ANA MENÜYE DÖN");
        SetLang("QUIT GAME", "ESCI DAL GIOCO", "OYUNDAN ÇIK");

        // --- Authentic Persian (فارسی) Comprehensive Translations ---
        void SetLangFa(string en, string fa)
        {
            if (masterDict.TryGetValue(en, out var dict))
            {
                dict["fa"] = fa;
            }
        }

        SetLangFa("THE SACRED BEANSTALK: FOREST WAR", "نبرد ساقه لوبیا مقدس: جنگ جنگل");
        SetLangFa("Inscribed on Ancient Oak Bark Two Millennia Past", "حک شده بر پوست بلوط کهن از دو هزار سال پیش");
        SetLangFa("OFFLINE EXPEDITION (SOLO vs BOTS)", "نبرد آفلاین (تک‌نفره با ربات‌ها)");
        SetLangFa("REALM EXPEDITION (ONLINE MULTIPLAYER)", "نبرد آنلاین (چند نفره)");
        SetLangFa("TRIBE SANCTUARY (FRIENDLY LOBBY)", "پناهگاه قبیله (لابی دوستانه)");
        SetLangFa("ENTER GROVE", "ورود به بیشه");
        SetLangFa("EMBARK ONLINE", "آغاز آنلاین");
        SetLangFa("GATHER TRIBE", "ایجاد لابی");

        SetLangFa("TREE PROTECTORS", "محافظان درخت");
        SetLangFa("TREE DESTROYERS", "نابودکنندگان درخت");
        SetLangFa("GUARDIANS OF LIFE", "نگهبانان زندگی");
        SetLangFa("AXEMEN OF CHAOS", "تبرداران آشوب");
        SetLangFa("BECOME A PROTECTOR", "پیوستن به محافظان");
        SetLangFa("BECOME A DESTROYER", "پیوستن به نابودکنندگان");
        SetLangFa("SACRED ALLEGIANCE: CHOOSE THY PATH", "وفاداری مقدس: مسیر خود را برگزینید");

        SetLangFa("FOREST GOLIATH [LV 1]", "غول جنگل [سطح ۱]");
        SetLangFa("INFERNO TITAN [LV 2]", "تایتان دوزخ [سطح ۲]");
        SetLangFa("ANCIENT OVERLORD [LV 3]", "فرمانروای باستانی [سطح ۳]");

        SetLangFa("ALL 3 GUARDIAN BOSSES HAVE BEEN DEFEATED!", "هر ۳ غول نگهبان کاملاً شکست خوردند!");
        SetLangFa("YOU CAN NOW DESTROY THE SACRED TREE!", "اکنون می‌توانید درخت مقدس را نابود کنید!");
        SetLangFa("THE CRYSTAL IS READY — YOU CAN TAKE IT NOW!", "بلور آماده است — اکنون می‌توانید آن را بردارید!");
        SetLangFa("TREE SHIELD ACTIVE", "سپر محافظ درخت فعال است");

        SetLangFa("START GAME", "شروع بازی");
        SetLangFa("CLOSE", "بستن");
        SetLangFa("BATTLE AGAIN", "نبرد دوباره");
        SetLangFa("RETURN TO GROVE", "بازگشت به بیشه");
        SetLangFa("COMMENCE EXPEDITION", "آغاز ماجراجویی");
        SetLangFa("CONFIRM HERO", "تأیید قهرمان");
        SetLangFa("EQUIP", "تجهیز");
        SetLangFa("EQUIPPED", "مجهز شد");
        SetLangFa("CLAIM", "دریافت");
        SetLangFa("SELECT", "انتخاب");
        SetLangFa("SELECTED", "انتخاب شد");
        SetLangFa("PREVIOUS", "قبلی");
        SetLangFa("NEXT", "بعدی");

        SetLangFa("Warrior", "جنگجو");
        SetLangFa("Mage", "جادوگر");
        SetLangFa("Archer", "کماندار");
        SetLangFa("Tank", "زره‌پوش");
        SetLangFa("Assassin", "قاتل");
        SetLangFa("Bomber", "بمب‌انداز");
        SetLangFa("Druid", "دروید");
        SetLangFa("Berserker", "جنگجوی خشمگین");
        SetLangFa("Paladin", "شوالیه مقدس");
        SetLangFa("Ranger", "تکاور جنگل");
        SetLangFa("Necromancer", "احضارگر ارواح");
        SetLangFa("Shaman", "شمن");
        SetLangFa("Rogue", "راه‌زن");
        SetLangFa("Cleric", "روحانی");
        SetLangFa("Elementalist", "استاد عناصر");
        SetLangFa("Barbarian", "بربر");
        SetLangFa("Enchantress", "افسونگر");
        SetLangFa("Knight", "شوالیه");
        SetLangFa("Alchemist", "کیمیاگر");
        SetLangFa("Summoner", "احضارکننده");
        SetLangFa("Beastmaster", "رام‌کننده هیولا");
        SetLangFa("Templar", "شوالیه معبد");
        SetLangFa("Shadowblade", "تیغه سایه");
        SetLangFa("Windwalker", "بادپیما");
        SetLangFa("Ironclad", "پولادین");

        SetLangFa("TANK", "زره‌پوش");
        SetLangFa("DAMAGE", "حمله");
        SetLangFa("SUPPORT", "پشتیبان");
        SetLangFa("CONTROL", "مهارکننده");
        SetLangFa("SPECIALIST", "متخصص");
        SetLangFa("HERO", "قهرمان");
        SetLangFa("HP", "سلامت");
        SetLangFa("ATK", "حمله");
        SetLangFa("SPD", "سرعت");
        SetLangFa("RNG", "برد");
        SetLangFa("COOLDOWN", "زمان بازیابی");
        SetLangFa("JUMP", "پرش");
        SetLangFa("Close-Range Melee", "نبرد نزدیک");
        SetLangFa("Ranged Projectile", "پرتاب از دور");

        SetLangFa("Acorns", "بلوط‌ها");
        SetLangFa("Gems", "گوهرها");
        SetLangFa("Emporium", "فروشگاه");
        SetLangFa("Gift", "هدیه");
        SetLangFa("BACK", "بازگشت");
        SetLangFa("BACK TO MODES", "بازگشت به حالت‌ها");
        SetLangFa("BACK TO TEAMS", "بازگشت به تیم‌ها");
        SetLangFa("LEAVE LOBBY", "ترک لابی");
        SetLangFa("CHANGE LANGUAGE", "تغییر زبان");
        SetLangFa("RESUME MATCH", "ادامه نبرد");
        SetLangFa("PAUSE & SETTINGS", "توقف و تنظیمات");
        SetLangFa("AUDIO & SOUND", "صدا و جلوه‌ها");
        SetLangFa("SOUND: ON", "صدا: روشن");
        SetLangFa("SOUND: MUTED", "صدا: خاموش");
        SetLangFa("RETURN TO MAIN MENU", "بازگشت به منوی اصلی");
        SetLangFa("QUIT GAME", "خروج از بازی");

        SetLangFa("SACRED ALTAR", "محراب مقدس");
        SetLangFa("✦ SACRED ALTAR ✦", "✦ محراب مقدس ✦");
        SetLangFa("ALTAR", "محراب");
        SetLangFa("Protector", "حامی");
        SetLangFa("Destroyer", "نابودکننده");
        SetLangFa("HERO & CLASS", "قهرمان و رده");
        SetLangFa("TEAM", "تیم");
        SetLangFa("KILLS", "کشته‌ها");
        SetLangFa("DEATHS", "مرگ‌ها");
        SetLangFa("DAMAGE", "آسیب");
        SetLangFa("BOSS", "غول");
        SetLangFa("CRYSTAL", "بلور");
        SetLangFa("SCORE", "امتیاز");
        SetLangFa("⭐ YES", "⭐ بله");

        // --- Deep Localization for Internal Cards, Attributes, Play Modes & Guides ---
        void Reg(string en, string ar, string fa, string de, string it, string tr, string ru, string fr, string es, string hi, string zh, string ja, string pt)
        {
            var d = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "en", en }, { "ar", ar }, { "fa", fa }, { "de", de }, { "it", it }, { "tr", tr },
                { "ru", ru }, { "fr", fr }, { "es", es }, { "hi", hi }, { "zh", zh }, { "ja", ja }, { "pt", pt }
            };
            masterDict[en] = d;
        }

        // RPG Attributes & Badges
        Reg("MAX HEALTH", "الصحة القصوى", "حداکثر سلامت", "MAX. GESUNDHEIT", "SALUTE MASSIMA", "MAKSİMUM CAN", "МАКС. ЗДОРОВЬЕ", "SANTÉ MAXIMALE", "SALUD MÁXIMA", "अधिकतम स्वास्थ्य", "最大生命", "最大HP", "VIDA MÁXIMA");
        Reg("ATTACK DAMAGE", "القوة الهجومية", "قدرت حمله", "ANGRIFFSSCHADEN", "DANNO DI ATTACCO", "SALDIRI HASARI", "СИЛА АТАКИ", "DÉGÂTS D'ATTAQUE", "DAÑO DE ATAQUE", "हमला क्षति", "攻击伤害", "攻撃力", "DANO DE ATAQUE");
        Reg("MOVE SPEED", "سرعة التحرك", "سرعت حرکت", "LAUFGESCHWINDIGKEIT", "VELOCITÀ DI MOVIMENTO", "HAREKET HIZI", "СКОРОСТЬ ПЕРЕДВИЖЕНИЯ", "VITESSE DE DÉPL.", "VELOCIDAD DE MOV.", "गति", "移动速度", "移動速度", "VELOCIDADE DE MOV.");
        Reg("COMBAT RANGE", "مدى الاشتباك", "برد درگیری", "KAMPFREICHWEITE", "GITTATA DI COMBATTIMENTO", "DÖVÜŞ MENZİLİ", "ДАЛЬНОСТЬ БОЯ", "PORTÉE DE COMBAT", "RANGO DE COMBATE", "युद्ध सीमा", "攻击射程", "射程", "ALCANCE DE COMBATE");
        Reg("ATTACK CADENCE", "سرعة الضربات", "آهنگ حمله", "ANGRIFFSTEMPO", "CADENZA DI ATTACCO", "SALDIRI RİTMİ", "ТЕМП АТАКИ", "CADENCE D'ATTAQUE", "CADENCIA DE ATAQUE", "हमला ताल", "攻击频率", "攻撃頻度", "CADÊNCIA DE ATAQUE");
        Reg("JUMP & MOBILITY", "القفز والقدرة الحركية", "پرش و تحرک", "SPRUNG & MOBILITÄT", "SALTO E MOBILITÀ", "ZIPLAMA VE HAREKET", "ПРЫЖОК И МОБИЛЬНОСТЬ", "SAUT ET MOBILITÉ", "SALTO Y MOVILIDAD", "छलांग और गतिशीलता", "跳跃与机动性", "跳躍と機動性", "SALTO E MOBILIDADE");

        Reg("JUGGERNAUT", "درع صامد", "زره مستحکم", "KOLOSS", "COLOSSO", "DURDURULAMAZ", "ДЖАГГЕРНАУТ", "COLOSSE", "IMPARABLE", "अजेय", "霸体重装", "巨漢", "COLOSSO");
        Reg("STURDY", "صلب", "مقاوم", "ROBUST", "ROBUSTO", "DAYANIKLI", "СТОЙКИЙ", "ROBUSTE", "ROBUSTO", "मजबूत", "坚韧", "頑丈", "ROBUSTO");
        Reg("AGILE", "رشيق", "چابک", "WENDIG", "AGILE", "ÇEVİK", "ЛОВКИЙ", "AGILE", "ÁGIL", "फुर्तीला", "灵动", "敏捷", "ÁGIL");
        Reg("LETHAL", "فتاك", "مرگبار", "TÖDLICH", "LETALE", "ÖLÜMCÜL", "СМЕРТОНОСНЫЙ", "MORTEL", "LETAL", "घातक", "毁灭性", "致命的", "LETAL");
        Reg("POTENT", "قوي", "قدرتمند", "KRAFTVOLL", "POTENTE", "GÜÇLÜ", "СИЛЬНЫЙ", "PUISSANT", "POTENTE", "शक्तिशाली", "强劲", "強力", "POTENTE");
        Reg("BALANCED", "متوازن", "متعادل", "AUSGEWOGEN", "EQUILIBRATO", "DENGELİ", "БАЛАНС", "ÉQUILIBRÉ", "EQUILIBRADO", "संतुलित", "均衡", "バランス", "EQUILIBRADO");
        Reg("WINDRUNNER", "سريع جداً", "بسیار سریع", "BLITZSCHNELL", "VELOCISSIMO", "RÜZGAR GİBİ", "БЫСТРЫЙ", "TRÈS RAPIDE", "VELOZ", "अति तीव्र", "迅捷如风", "疾風", "VELOZ");
        Reg("SWIFT", "رشيق", "سریع", "SCHNELL", "RAPIDO", "ÇABUK", "ПРОВОРНЫЙ", "RAPIDE", "ÁGIL", "तेज", "敏捷", "素早い", "RÁPIDO");
        Reg("HEAVY", "ثقيل", "سنگین", "SCHWER", "PESANTE", "AĞIR", "ТЯЖЕЛЫЙ", "LOURD", "PESADO", "भारी", "沉重", "重装", "PESADO");
        Reg("SNIPER", "قناص بعيد", "تک‌تیرانداز", "SCHARFSCHÜTZE", "CECCHINO", "KESKİN NİŞANCI", "СНАЙПЕР", "SNIPER", "FRANCOTIRADOR", "निशानेबाज", "超远射程", "狙撃", "SNIPER");
        Reg("MID-RANGE", "مدى متوسط", "برد متوسط", "MITTLERE REICHWEITE", "MEDIO RAGGIO", "ORTA MENZİL", "СРЕДНЯЯ ДИСТ.", "PORTÉE MOYENNE", "DISTANCIA MEDIA", "मध्यम सीमा", "中程", "中距離", "MÉDIO ALCANCE");
        Reg("MELEE", "اشتباك مباشر", "نبرد نزدیک", "NAHKAMPF", "MISCHIA", "YAKIN DÖVÜŞ", "БЛИЖНИЙ БОЙ", "CORPS À CORPS", "CUERPO A CUERPO", "हाथापाई", "近战肉搏", "近接格闘", "CORPO A CORPO");
        Reg("RAPID FLURRY", "ضربات متتالية", "ضربات پیاپی", "SCHNELLE SCHLÄGE", "RAFFICA RAPIDA", "SERİ SALDIRILAR", "БЫСТРЫЕ УДАРЫ", "RAFALE RAPIDE", "RÁFAGA RÁPIDA", "तीव्र प्रहार", "连贯连击", "連撃", "GOLPES RÁPIDOS");
        Reg("MEASURED", "متزن", "سنجیده", "GEMESSEN", "MISURATO", "ÖLÇÜLÜ", "УМЕРЕННЫЙ", "CADENCÉ", "MEDIDO", "मापा हुआ", "匀速", "安定", "COMPASSADO");
        Reg("POWER STRIKE", "ضربة قاضية", "ضربه سنگین", "KRAFTSCHLAG", "COLPO POTENTE", "GÜÇLÜ VURUŞ", "МОЩНЫЙ УДАР", "FRAPPE PUISSANTE", "GOLPE PODEROSO", "शक्ति प्रहार", "强力重击", "強打", "GOLPE FORTE");
        Reg("DOUBLE JUMP", "قفز مزدوج متاح", "پرش دوگانه فعال", "DOPPELSPRUNG AKTIV", "DOPPIO SALTO", "ÇİFT ZIPLAMA", "ДВОЙНОЙ ПРЫЖОК", "DOUBLE SAUT", "SALTO DOBLE", "दोहरी छलांग", "二段跳跃", "二段ジャンプ", "PULO DUPLO");
        Reg("SINGLE JUMP", "قفز أرضي ثابت", "پرش استاندارد", "STANDARDSPRUNG", "SALTO SINGOLO", "TEK ZIPLAMA", "ОДИНАРНЫЙ ПРЫЖОК", "SAUT UNIQUE", "SALTO SIMPLE", "एकल छलांग", "单段跳跃", "通常ジャンプ", "PULO SIMPLES");

        Reg("GUARDIAN CODEX & TACTICAL ROLE", "سجل الحارس والتكتيك القتالي", "طومار نگهبان و نقش تاکتیکی", "WÄCHTER-KODEX & TAKTISCHE ROLLE", "CODICE DEL GUARDIANO & RUOLO TATTICO", "MUHAFIZ KÜTÜĞÜ VE TAKTİK ROLÜ", "КОДЕКС СТРАЖА И ТАКТИЧЕСКАЯ РОЛЬ", "CODEX DU GARDIEN ET RÔLE TACTIQUE", "CÓDICE DEL GUARDIÁN Y ROL TÁCTICO", "संरक्षक संहिता और सामरिक भूमिका", "守护者法典与战术定位", "守護者の規範と戦術的役割", "CÓDICE DO GUARDIÃO E PAPEL TÁTICO");
        Reg("Standard Base Attributes", "السمات الأساسية القياسية", "ویژگی‌های پایه استاندارد", "Standard-Attribute", "Attributi Base Standard", "Standart Temel Nitelikler", "Базовые характеристики", "Attributs de base", "Atributos básicos", "मानक आधार विशेषताएँ", "标准基础属性", "基本ステータス", "Atributos Básicos");
        Reg("ALL (25)", "الكل (25)", "همه (۲۵)", "ALLE (25)", "TUTTI (25)", "HEPSİ (25)", "ВСЕ (25)", "TOUS (25)", "TODOS (25)", "सभी (25)", "全部 (25)", "全て (25)", "TODOS (25)");

        // Play Mode Cards & Navigation
        Reg("SACRED TRIAL", "تجربة مقدسة", "آزمون مقدس", "HEILIGE PRÜFUNG", "PROVA SACRA", "KUTSAL DENEME", "ИСПЫТАНИЕ", "ÉPREUVE SACRÉE", "PRUEBA SAGRADA", "पवित्र परीक्षण", "神圣试炼", "聖なる試練", "PROVA SAGRADA");
        Reg("SOLO EXPEDITION", "الحملة الفردية", "اکتشاف تک‌نفره", "SOLO-EXPEDITION", "SPEDIZIONE SOLITARIA", "TEK KİŞİLİK SEFER", "ОДИНОЧНЫЙ БОЙ", "EXPÉDITION SOLO", "EXPEDICIÓN INDIVIDUAL", "एकल अभियान", "单人离线远征", "ソロ遠征", "EXPEDIÇÃO INDIVIDUAL");
        Reg("Vs Forest Bot Guardians", "ضد حراس الغابة الآليين", "در برابر ربات‌های نگهبان جنگل", "Gegen Bot-Wächter", "Contro Guardiani Bot", "Orman Bot Muhafızlarına Karşı", "Против лесных ботов", "Contre gardiens bots", "Contra guardianes bots", "बॉट अभिभावकों के विरुद्ध", "对阵森林人机守卫", "ボットの守護者と対戦", "Contra bots guardiões");
        Reg("3 Guardian Titans", "٣ زعماء عمالقة", "۳ غول تایتان نگهبان", "3 Titanen-Bosse", "3 Titani Guardiani", "3 Muhafız Titan", "3 титана-босса", "3 titans gardiens", "3 jefes titanes", "३ अभिभावक टाइटन", "三大森林巨像Boss", "3体の守護タイタン", "3 chefes titãs");
        Reg("Chop Tree or Take Crystal", "اسقط الشجرة أو خذ الكريستال", "قطع درخت یا تصاحب بلور", "Baum fällen oder Kristall", "Abbatti l'albero o prendi il cristallo", "Ağacı Kes veya Kristali Al", "Срубите древо или кристалл", "Abattez l'arbre ou cristal", "Tala el árbol o toma el cristal", "पेड़ काटें या क्रिस्टल लें", "伐倒古树或争夺水晶", "神木伐採または水晶奪回", "Derrube a árvore ou cristal");
        Reg("Master 25 Hero Classes", "تدرّب على ٢٥ بطلاً", "تسلط بر ۲۵ رده قهرمان", "Meistert 25 Helden", "Padroneggia 25 eroi", "25 Kahraman Sınıfında Ustalaş", "25 классов героев", "Maîtrisez 25 héros", "Domina 25 héroes", "२५ नायकों में महारत", "驾驭25位传奇英雄", "25人の英雄を極めろ", "Domine 25 heróis");
        Reg("Offline Gold & XP", "ذهب ونقاط خبرة بدون نت", "طلا و تجربه آفلاین", "Gold & XP offline", "Oro ed ESP offline", "Çevrimdışı Altın ve TP", "Золото и опыт без сети", "Or et EXP hors-ligne", "Oro y EXP sin internet", "बिना नेट सोना व XP", "离线获取金币与经验", "オフラインで報酬獲得", "Ouro e EXP offline");

        Reg("LIVE WAR", "معركة حية", "نبرد زنده", "LIVE-SCHLACHT", "GUERRA DAL VIVO", "CANLI SAVAŞ", "ЖИВАЯ ВОЙНА", "GUERRE EN DIRECT", "GUERRA EN VIVO", "लाइव युद्ध", "实时全服战役", "リアルタイム戦", "GUERRA AO VIVO");
        Reg("REALM WAR", "حرب العوالم", "نبرد قلمروها", "REICH-KRIEG", "GUERRA DEI REGNI", "DİYAR SAVAŞI", "ВОЙНА РЕАЛЬМА", "GUERRE DES ROYAUMES", "GUERRA DEL REINO", "क्षेत्र युद्ध", "全域远征", "領域大戦", "GUERRA DO REINO");
        Reg("Global Multiplayer Arena", "معارك حية متعددة اللاعبين", "میدان چندنفره جهانی", "Globale Mehrspieler-Arena", "Arena Multigiocatore Globale", "Küresel Çok Oyunculu Arena", "Сетевая PvP-арена", "Arène multijoueur en ligne", "Arena multijugador global", "ग्लोबल मल्टीप्लेयर अखाड़ा", "跨服多人实时联机竞技场", "グローバルマルチプレイ闘技場", "Arena multijogador global");
        Reg("PRIVATE COVEN", "تحالف سري", "پیمان خصوصی", "HEILIGER ZIRKEL", "CONGREGA PRIVATA", "ÖZEL BİRLİK", "СВОЙ КЛАН", "ALLIANCE PRIVÉE", "COFRADÍA SAGRADA", "पवित्र गठबंधन", "私密部落", "秘密の盟約", "CLÃ PRIVADO");
        Reg("TRIBE SANCTUARY", "ملاذ العشيرة", "پناهگاه قبیله", "STAMMESHEILIGTUM", "SANTUARIO DELLA TRIBÙ", "KABİLE SIĞINAĞI", "ДРУЖЕСКОЕ ЛОББИ", "SANCTUAIRE D'AMIS", "SANTUARIO DE AMIGOS", "मित्र अभयारण्य", "部落圣所", "仲間たちの聖域", "SANTUÁRIO DA TRIBO");
        Reg("Co-op Sanctuary with Allies", "ملاذ تعاوني مع الأصدقاء", "پناهگاه همکاری با یاران", "Koop-Zuflucht mit Verbündeten", "Santuario cooperativo con alleati", "Dostlarla Eşli Sığınak", "Кооператив с союзниками", "Sanctuaire coop avec alliés", "Santuario cooperativo", "सहयोगियों के साथ सह-ऑप", "好友协同游玩据点", "仲間との協力聖域", "Santuário cooperativo com aliados");

        // Guide, Briefing & In-Game Announcements
        Reg("GUIDE", "التوجيهات", "راهنما", "LEITFADEN", "GUIDA", "REHBER", "ИНФО", "GUIDE", "GUÍA", "मार्गदर्शिका", "指南", "ガイド", "GUIA");
        Reg("BEANSTALK WAR — OFFICIAL TACTICAL BRIEFING", "حرب نبتة الفاصولياء — الدليل التكتيكي الرسمي", "نبرد ساقه لوبیا — راهنمای تاکتیکی رسمی", "BOHNENRANKEN-KRIEG — OFFIZIELLES TAKTIK-BRIEFING", "GUERRA DEL FAGIOLO — BRIEFING TATTICO UFFICIALE", "FASULYE SIRIĞI SAVAŞI — RESMİ TAKTİK BİLGİLENDİRME", "ВОЙНА БОБОВОГО СТЕБЛЯ — ТАКТИЧЕСКИЙ ИНСТРУКТАЖ", "GUERRE DU HARICOT — GUIDE TACTIQUE OFFICIEL", "GUERRA DEL TALLO — GUÍA TÁCTICA OFICIAL", "सेम का युद्ध — आधिकारिक सामरिक जानकारी", "圣豆之战 — 官方战术指南", "豆の木の戦い — 公式戦術ブリーフィング", "GUERRA DO FEIJÃO — BRIEFING TÁTICO OFICIAL");
        Reg("THE BATTLE FOR THE SACRED BEANSTALK", "المعركة من أجل نبتة الفاصولياء المقدسة", "نبرد برای ساقه لوبیای مقدس", "DIE SCHLACHT UM DIE HEILIGE BOHNENRANKE", "LA BATTAGLIA PER IL FAGIOLO SACRO", "KUTSAL FASULYE SIRIĞI İÇİN SAVAŞ", "БИТВА ЗА СВЯЩЕННЫЙ БОБОВЫЙ СТЕБЕЛЬ", "LA BATAILLE POUR LE HARICOT SACRÉ", "LA BATALLA POR EL TALLO SAGRADO", "पवित्र सेम के लिए युद्ध", "神圣圣豆森林之战", "聖なる豆の木を巡る戦い", "A BATALHA PELO FEIJÃO SAGRADO");
        Reg("THE 3 TREE LEVELS", "مستويات الشجرة الثلاثة", "۳ طبقه درخت", "DIE 3 BAUMSTUFEN", "I 3 LIVELLI DELL'ALBERO", "3 AĞAÇ SEVİYESİ", "3 УРОВНЯ ДЕРЕВА", "LES 3 NIVEAUX DE L'ARBRE", "LOS 3 NIVELES DEL ÁRBOL", "३ वृक्ष स्तर", "古树三大层阶", "神木の3つの階層", "OS 3 NÍVEIS DA ÁRVORE");
        Reg("THE 3 GUARDIAN BOSS TITANS", "زعماء الحراس العمالقة الثلاثة", "۳ تایتان نگهبان غول‌پیکر", "DIE 3 WÄCHTER-TITANEN", "I 3 TITANI GUARDIANI", "3 MUHAFIZ TİTAN PATRON", "3 ТИТАНА-СТРАЖА", "LES 3 TITANS GARDIENS", "LOS 3 TITANES GUARDIANES", "३ अभिभावक टाइटन बॉस", "三大泰坦守护首领", "3体の守護タイタンボス", "OS 3 TITÃS GUARDIÕES");
        Reg("WIN CONDITIONS & SACRED OBJECTIVES", "شروط النصر والأهداف المقدسة", "شرایط پیروزی و اهداف مقدس", "SIEG-BEDINGUNGEN & HEILIGE ZIELE", "CONDIZIONI DI VITTORIA E OBIETTIVI SACRI", "ZAFER KOŞULLARI VE KUTSAL HEDEFLER", "УСЛОВИЯ ПОБЕДЫ И ЦЕЛИ", "CONDITIONS DE VICTOIRE ET OBJECTIFS", "CONDICIONES DE VICTORIA Y OBJETIVOS", "जीत की शर्तें और पवित्र लक्ष्य", "胜利条件与神圣目标", "勝利条件と聖なる目的", "CONDIÇÕES DE VITÓRIA E OBJETIVOS");
        Reg("CONTROLS & MASTER TACTICAL TIPS", "التحكم والنصائح التكتيكية الأساسية", "کنترل‌ها و نکات کلیدی تاکتیکی", "STEUERUNG & MEISTER-TIPPS", "COMANDI E CONSIGLI TATTICI", "KONTROLLER VE USTA TAKTİK İPUÇLARI", "УПРАВЛЕНИЕ И СОВЕТЫ", "COMMANDES ET CONSEILS TACTIQUES", "CONTROLES Y CONSEJOS TÁCTICOS", "नियंत्रण और मुख्य सामरिक सुझाव", "操作说明与大师战术技巧", "操作方法と上級戦術の秘訣", "CONTROLES E DICAS TÁTICAS");

        Reg("10-Second Divine Shield Countdown active! Position yourselves — Tree becomes vulnerable in 10s!", "العد التنازلي لدرع الصمود المقدس بدأ (10 ثوانٍ)! خذوا مواقعكم — الشجرة ستصبح مكشوفة بالكامل بعد 10 ثوانٍ!", "شمارش معکوس سپر مقدس ۱۰ ثانیه‌ای آغاز شد! جای‌گیری کنید — درخت پس از ۱۰ ثانیه آسیب‌پذیر می‌شود!", "10-Sekunden-Göttlicher-Schild aktiv! Geht in Stellung — Baum in 10s verwundbar!", "Conto alla rovescia Scudo Divino di 10s attivo! Posizionatevi — L'albero sarà vulnerabile tra 10s!", "10 Saniyelik İlahi Kalkan Geri Sayımı başladı! Konum alın — Ağaç 10 sn içinde savunmasız kalacak!", "ОТСЧЕТ 10 СЕКУНД ДО СНЯТИЯ ЩИТА! Займите позиции — Дерево станет уязвимым через 10 сек!", "COMPTE À REBOURS DE 10S DU BOUCLIER DIVIN ! Prenez position — L'Arbre sera vulnérable dans 10s !", "¡CUENTA ATRÁS DE 10S DEL ESCUDO DIVINO! ¡El Árbol será vulnerable en 10s!", "१० सेकंड दिव्य ढाल उलटी गिनती सक्रिय! स्थान लें — पेड़ १० सेकंड में असुरक्षित हो जाएगा!", "10秒神圣圣树护盾倒计时已启动！就位迎战 — 古树将在10秒后失去免疫！", "神聖シールドの10秒カウントダウン開始！配置につけ — 10秒後に神木の無敵が解除される！", "CONTAGEM REGRESSIVA DE 10S DO ESCUDO DIVINO! Posicione-se — Árvore vulnerável em 10s!");
        Reg("The Sacred Crystal has appeared! Grab the Crystal and deliver it to the Sacred Altar to WIN!", "ظهرت الكريستالة المقدسة! احمل الكريستالة وأوصلها إلى المذبح المقدس للفوز بالمعركة!", "بلور مقدس ظاهر شد! بلور را بردارید و برای پیروزی به محراب مقدس برسانید!", "Der heilige Kristall ist erschienen! Tragt ihn zum Altar, um zu GEWINNEN!", "Il Cristallo Sacro è apparso! Portalo all'Altare Sacro per VINCERE!", "Kutsal Kristal belirdi! Kristali alıp Kutsal Sunak'a taşıyarak KAZANIN!", "Священный кристалл появился! Отнесите его к алтарю для победы!", "Le Cristal Sacré est apparu ! Portez-le à l'Autel pour GAGNER !", "¡El Cristal Sagrado ha aparecido! ¡Llévalo al Altar para ganar!", "पवित्र क्रिस्टल प्रकट हुआ है! जीतने के लिए इसे पवित्र वेदी तक पहुँचाएँ!", "神圣水晶已降世！夺取水晶并护送至圣坛即可夺取全盘胜利！", "聖なる水晶が出現した！水晶を運んで聖なる祭壇へ奉納し、勝利を掴め！", "O Cristal Sagrado apareceu! Leve-o ao Altar Sagrado para VENCER!");
        Reg("HOLD THE ALTAR FOR 3 SECONDS TO WIN!", "ابقَ في المذبح لمدة ٣ ثوانٍ للفوز!", "برای پیروزی ۳ ثانیه در محراب بایستید!", "HALTET DEN ALTAR FÜR 3 SEKUNDEN ZUM SIEG!", "MANTIENI L'ALTARE PER 3 SECONDI PER VINCERE!", "KAZANMAK İÇİN SUNAKTA 3 SANİYE KALIN!", "УДЕРЖИВАЙТЕ АЛТАРЬ 3 СЕКУНДЫ ДЛЯ ПОБЕДЫ!", "TENEZ L'AUTEL PENDANT 3 SECONDES POUR GAGNER !", "¡MANTÉN EL ALTAR 3 SEGUNDOS PARA GANAR!", "जीतने के लिए वेदी पर ३ सेकंड रुकें!", "在祭坛坚守3秒即可获胜！", "勝利のために祭壇で3秒間耐えよ！", "MANTENHA O ALTAR POR 3 SEGUNDOS PARA VENCER!");
        Reg("SACRED CRYSTAL DELIVERED — PROTECTORS WIN!", "لقد وصلت الكريستالة للمذبح — فاز الحماة!", "بلور به محراب رسید — محافظان پیروز شدند!", "KRISTALL AM ALTAR — DIE BESCHÜTZER GEWINNEN!", "CRISTALLO CONSEGNATO — I PROTETTORI VINCONO!", "KRİSTAL SUNAĞA ULAŞTI — KORUYUCULAR KAZANDI!", "КРИСТАЛЛ ДОСТАВЛЕН — ПОБЕДА ЗАЩИТНИКОВ!", "LE CRISTAL EST À L'AUTEL — VICTOIRE DES PROTECTEURS !", "¡CRISTAL ENTREGADO — GANAN LOS PROTECTORES!", "क्रिस्टल वेदी पर पहुँचा — रक्षक जीते!", "圣晶归坛 — 守护者获胜！", "水晶奉納完了 — 守護者の大勝利！", "CRISTAL ENTREGUE — PROTETORES VENCEM!");
    }

    #endregion

    #region Procedural Crisp National Flag Textures (36x24)

    /// <summary>
    /// Generates or retrieves an authentic, vibrant procedural national flag texture.
    /// Never depends on system font emoji rendering; works reliably on Windows, Mac, Linux, and Android.
    /// </summary>
    public static Texture2D GetFlagTexture(string code)
    {
        if (string.IsNullOrEmpty(code)) code = "en";
        code = code.ToLowerInvariant();

        if (flagTextureCache.TryGetValue(code, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        Texture2D flag = CreateFlagTexture(code);
        flagTextureCache[code] = flag;
        return flag;
    }

    private static Texture2D CreateFlagTexture(string code)
    {
        int w = 36;
        int h = 24;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[w * h];

        // Default fill
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.gray;

        switch (code)
        {
            case "ar": // Saudi Arabia (علم المملكة العربية السعودية - أخضر زاهٍ مع السيف والشهادة باللون الأبيض)
                Color saGreen = new Color(0.0f, 0.45f, 0.22f);
                Color saWhite = Color.white;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c = saGreen;

                        // السيف الأبيض في الجزء السفلي (y = 6..8, x = 9..27)
                        // نصل السيف الأفقي
                        if (y == 7 && x >= 12 && x <= 27)
                        {
                            c = saWhite;
                        }
                        // مقبض وقبضة السيف على اليسار
                        if ((x == 11 && y >= 6 && y <= 8) || (x == 9 && y == 7))
                        {
                            c = saWhite;
                        }

                        // نقش الشهادة بالخط العربي الأبيض في المنتصف (y = 11..16, x = 7..28)
                        if (y >= 11 && y <= 16 && x >= 8 && x <= 28)
                        {
                            bool isLetterStroke = (y == 12 || y == 15) && (x % 2 == 0);
                            bool isVerticalStem = (x == 10 || x == 14 || x == 18 || x == 22 || x == 26) && (y >= 12 && y <= 16);
                            bool isDot = (y == 16 && (x == 12 || x == 16 || x == 20 || x == 24));
                            if (isLetterStroke || isVerticalStem || isDot)
                            {
                                c = saWhite;
                            }
                        }

                        pixels[y * w + x] = c;
                    }
                }
                break;

            case "it": // Italy (Green, White, Red vertical tricolor)
                Color itGreen = new Color(0.0f, 0.57f, 0.27f);
                Color itWhite = Color.white;
                Color itRed = new Color(0.81f, 0.17f, 0.17f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color stripe = (x < 12) ? itGreen : ((x < 24) ? itWhite : itRed);
                        pixels[y * w + x] = stripe;
                    }
                }
                break;

            case "tr": // Turkey (Red background with White Crescent and 5-pointed Star)
                Color trRed = new Color(0.89f, 0.05f, 0.12f);
                Color trWhite = Color.white;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c = trRed;

                        // Crescent outer circle centered at (14.5, 12) radius 5.2
                        float dx1 = x - 14.5f;
                        float dy1 = y - 12.0f;
                        float distOuter = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1);

                        // Crescent inner circle centered at (16.2, 12) radius 4.1
                        float dx2 = x - 16.2f;
                        float dy2 = y - 12.0f;
                        float distInner = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2);

                        if (distOuter <= 5.2f && distInner >= 4.0f)
                        {
                            c = trWhite;
                        }

                        // Star centered at (21.5, 12) radius 2.2
                        float sx = x - 21.5f;
                        float sy = y - 12.0f;
                        float sDist = Mathf.Sqrt(sx * sx + sy * sy);
                        if (sDist <= 2.2f)
                        {
                            c = trWhite;
                        }

                        pixels[y * w + x] = c;
                    }
                }
                break;

            case "ru": // Russia (White, Blue, Red horizontal stripes)
                Color ruWhite = Color.white;
                Color ruBlue = new Color(0.0f, 0.22f, 0.65f);
                Color ruRed = new Color(0.83f, 0.17f, 0.12f);
                for (int y = 0; y < h; y++)
                {
                    Color bandCol = (y >= 16) ? ruWhite : ((y >= 8) ? ruBlue : ruRed);
                    for (int x = 0; x < w; x++) pixels[y * w + x] = bandCol;
                }
                break;

            case "fr": // France (Blue, White, Red vertical stripes)
                Color frBlue = new Color(0.0f, 0.15f, 0.55f);
                Color frWhite = Color.white;
                Color frRed = new Color(0.93f, 0.16f, 0.22f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color stripe = (x < 12) ? frBlue : ((x < 24) ? frWhite : frRed);
                        pixels[y * w + x] = stripe;
                    }
                }
                break;

            case "es": // Spain (Red, Yellow, Red horizontal stripes)
                Color esRed = new Color(0.75f, 0.08f, 0.11f);
                Color esYellow = new Color(0.98f, 0.78f, 0.0f);
                for (int y = 0; y < h; y++)
                {
                    Color stripe = (y >= 18 || y < 6) ? esRed : esYellow;
                    for (int x = 0; x < w; x++)
                    {
                        // Spanish coat of arms accent dot
                        if (y >= 9 && y <= 14 && x >= 9 && x <= 13)
                        {
                            pixels[y * w + x] = new Color(0.7f, 0.2f, 0.1f);
                        }
                        else
                        {
                            pixels[y * w + x] = stripe;
                        }
                    }
                }
                break;

            case "de": // Germany (Black, Red, Gold horizontal stripes)
                Color deBlack = new Color(0.08f, 0.08f, 0.08f);
                Color deRed = new Color(0.87f, 0.0f, 0.0f);
                Color deGold = new Color(1.0f, 0.8f, 0.0f);
                for (int y = 0; y < h; y++)
                {
                    Color stripe = (y >= 16) ? deBlack : ((y >= 8) ? deRed : deGold);
                    for (int x = 0; x < w; x++) pixels[y * w + x] = stripe;
                }
                break;

            case "hi": // India (Saffron, White, Green horizontal stripes with Ashoka Chakra)
                Color hiSaffron = new Color(1.0f, 0.6f, 0.2f);
                Color hiWhite = Color.white;
                Color hiGreen = new Color(0.07f, 0.53f, 0.03f);
                Color hiChakra = new Color(0.0f, 0.0f, 0.5f);
                for (int y = 0; y < h; y++)
                {
                    Color stripe = (y >= 16) ? hiSaffron : ((y >= 8) ? hiWhite : hiGreen);
                    for (int x = 0; x < w; x++)
                    {
                        if (y >= 8 && y < 16)
                        {
                            float dx = x - 18f;
                            float dy = y - 11.5f;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist >= 2.0f && dist <= 3.5f)
                            {
                                pixels[y * w + x] = hiChakra;
                                continue;
                            }
                            if (dist < 1.0f)
                            {
                                pixels[y * w + x] = hiChakra;
                                continue;
                            }
                        }
                        pixels[y * w + x] = stripe;
                    }
                }
                break;

            case "zh": // China (Red with Gold stars)
                Color zhRed = new Color(0.88f, 0.14f, 0.12f);
                Color zhGold = new Color(1.0f, 0.88f, 0.0f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c = zhRed;
                        // Main big star at (7, 16)
                        float dx = x - 7f;
                        float dy = y - 16f;
                        if (dx * dx + dy * dy <= 5.5f) c = zhGold;

                        // 4 small stars around
                        if ((x == 12 && y == 20) || (x == 14 && y == 17) || (x == 14 && y == 13) || (x == 12 && y == 10))
                        {
                            c = zhGold;
                        }
                        pixels[y * w + x] = c;
                    }
                }
                break;

            case "ja": // Japan (White with Crimson Sun disc)
                Color jaWhite = Color.white;
                Color jaRed = new Color(0.74f, 0.0f, 0.18f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float dx = x - 18f;
                        float dy = y - 12f;
                        pixels[y * w + x] = (dx * dx + dy * dy <= 38f) ? jaRed : jaWhite;
                    }
                }
                break;

            case "fa": // Iran (Green, White, Red horizontal stripes with central emblem)
                Color faGreen = new Color(0.14f, 0.62f, 0.28f);
                Color faWhite = Color.white;
                Color faRed = new Color(0.85f, 0.15f, 0.18f);
                for (int y = 0; y < h; y++)
                {
                    Color band = (y >= 16) ? faGreen : ((y >= 8) ? faWhite : faRed);
                    for (int x = 0; x < w; x++)
                    {
                        if (y >= 8 && y < 16)
                        {
                            float dx = x - 18f;
                            float dy = y - 11.5f;
                            if (dx * dx + dy * dy <= 4.0f)
                            {
                                pixels[y * w + x] = faRed;
                                continue;
                            }
                        }
                        pixels[y * w + x] = band;
                    }
                }
                break;

            case "pt": // Portugal / Brazil (Green field, Yellow rhombus, Blue celestial circle)
                Color ptGreen = new Color(0.0f, 0.62f, 0.24f);
                Color ptYellow = new Color(1.0f, 0.87f, 0.0f);
                Color ptBlue = new Color(0.01f, 0.13f, 0.41f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float rx = Mathf.Abs(x - 18f) / 13f;
                        float ry = Mathf.Abs(y - 12f) / 8.5f;
                        if (rx + ry <= 1.0f)
                        {
                            float cx = x - 18f;
                            float cy = y - 12f;
                            if (cx * cx + cy * cy <= 18f)
                            {
                                pixels[y * w + x] = ptBlue;
                            }
                            else
                            {
                                pixels[y * w + x] = ptYellow;
                            }
                        }
                        else
                        {
                            pixels[y * w + x] = ptGreen;
                        }
                    }
                }
                break;

            case "en": // United Kingdom / British Union Jack
            default:
                Color ukBlue = new Color(0.01f, 0.14f, 0.42f);   // Deep Navy / Royal Blue
                Color ukRed = new Color(0.82f, 0.11f, 0.15f);    // St. George & St. Patrick Red
                Color ukWhite = Color.white;                     // St. Andrew & Cross White

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c = ukBlue;

                        // Diagonals: aspect ratio is 36 / 24 = 3 / 2
                        // Diagonal 1: 2*x - 3*y - 0.5 = 0
                        // Diagonal 2: 2*x + 3*y - 69.5 = 0
                        float d1 = Mathf.Abs(2f * x - 3f * y - 0.5f);
                        float d2 = Mathf.Abs(2f * x + 3f * y - 69.5f);

                        // White Saltire (St. Andrew)
                        if (d1 <= 6.5f || d2 <= 6.5f)
                        {
                            c = ukWhite;
                        }

                        // Red Saltire (St. Patrick)
                        if (d1 <= 2.8f || d2 <= 2.8f)
                        {
                            c = ukRed;
                        }

                        // St. George's White Cross (border)
                        if ((x >= 14 && x <= 21) || (y >= 8 && y <= 15))
                        {
                            c = ukWhite;
                        }

                        // St. George's Red Cross (center)
                        if ((x >= 16 && x <= 19) || (y >= 10 && y <= 13))
                        {
                            c = ukRed;
                        }

                        pixels[y * w + x] = c;
                    }
                }
                break;
        }

        // Add 1px crisp subtle border around the entire flag
        Color border = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        for (int x = 0; x < w; x++)
        {
            pixels[x] = border;
            pixels[(h - 1) * w + x] = border;
        }
        for (int y = 0; y < h; y++)
        {
            pixels[y * w] = border;
            pixels[y * w + (w - 1)] = border;
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return tex;
    }

    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("LanguageSelectionUI");
            go.AddComponent<LanguageSelectionUI>();
            DontDestroyOnLoad(go);
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
            return;
        }

        string saved = CurrentLanguageCode;
        for (int i = 0; i < AvailableLanguages.Length; i++)
        {
            if (AvailableLanguages[i].code.Equals(saved, System.StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
                break;
            }
        }

        // Always default to English and keep main menu open; player can freely choose any language via the top-left button or 'L' key
        showLanguageModal = false;
    }

    void OnDestroy()
    {
        foreach (var kvp in flagTextureCache)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        flagTextureCache.Clear();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            // Allow toggling when not typing in chat or other locked modals
            if (!CharacterSelectionUI.IsSelectionOpen && !PlayerProgressionAndShopUI.IsShopOpen)
            {
                showLanguageModal = !showLanguageModal;
            }
        }

        if (showLanguageModal && Input.GetKeyDown(KeyCode.Escape))
        {
            if (PlayerPrefs.GetInt("LanguagePromptSeen", 0) == 1)
            {
                showLanguageModal = false;
            }
        }
    }

    void OnGUI()
    {
        if (showLanguageModal)
        {
            // Highest priority depth ensures language screen is strictly in front of everything
            GUI.depth = -500;
            DrawLanguageSelectionModal();
            return;
        }

        // Draw top-right Language Quick Toggle button during main menus or gameplay
        if (!CharacterSelectionUI.IsSelectionOpen && 
            !PlayerProgressionAndShopUI.IsShopOpen && 
            !GameIntroBriefingUI.IsBriefingOpen)
        {
            GUI.depth = -100;
            DrawLanguageCornerButton();
        }
    }

    private void DrawLanguageCornerButton()
    {
        float btnW = 64f;
        float btnH = 38f;
        // Positioned in top-right bar between Guide and Audio buttons:
        // [Guide (130px)] [Flag (64px)] [Audio (78px)]
        float x = Screen.width - 78f - 12f - btnW - 8f;
        float y = 8f;

        LanguageOption current = AvailableLanguages[Mathf.Clamp(selectedIndex, 0, AvailableLanguages.Length - 1)];

        Rect btnRect = new Rect(x, y, btnW, btnH);
        if (EnchantedNatureGUI.DrawAncientGoldButton(btnRect, "", EnchantedNatureGUI.CyanRuneAccent, 11, false))
        {
            showLanguageModal = true;
        }

        // Draw national flag badge centered inside compact button
        Texture2D flag = GetFlagTexture(current.code);
        if (flag != null)
        {
            float flagW = 36f;
            float flagH = 24f;
            GUI.DrawTexture(new Rect(x + (btnW - flagW) / 2f, y + (btnH - flagH) / 2f, flagW, flagH), flag);
        }
    }

    private void DrawLanguageSelectionModal()
    {
        // Full screen dimmed backdrop
        GUI.color = new Color(0.02f, 0.04f, 0.03f, 0.95f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(840f, Screen.width - 24f);
        float modalH = Mathf.Min(600f, Screen.height - 24f);
        float mx = (Screen.width - modalW) / 2f;
        float my = (Screen.height - modalH) / 2f;

        EnchantedNatureGUI.DrawLeafPlaque(
            new Rect(mx, my, modalW, modalH),
            new Color(0.06f, 0.10f, 0.08f, 0.98f),
            new Color(0.35f, 0.88f, 0.55f)
        );

        LanguageOption activeOpt = AvailableLanguages[Mathf.Clamp(selectedIndex, 0, AvailableLanguages.Length - 1)];

        // Dynamic Header Title & Subtitle for chosen language
        string headerTitle;
        string headerSubtitle;

        switch (activeOpt.code)
        {
            case "ar":
                headerTitle = ArabicFixer.Fix("🌐 اختيار لغة اللعبة الرسمية / GAME LANGUAGE");
                headerSubtitle = ArabicFixer.Fix("اختر لغتك المفضلة لبدء اللعبة. كل النصوص والأسماء والشروحات ستتحول مباشرة إليها (L)");
                break;
            case "fa":
                headerTitle = ArabicFixer.Fix("🌐 انتخاب زبان بازی / GAME LANGUAGE");
                headerSubtitle = ArabicFixer.Fix("زبان مورد علاقه خود را انتخاب کنید. همه منوها، نام‌ها و راهنماها به این زبان تغییر خواهند کرد (L)");
                break;
            case "ru":
                headerTitle = "🌐 ВЫБОР ЯЗЫКА ИГРЫ / GAME LANGUAGE";
                headerSubtitle = "Выберите ваш язык. Весь интерфейс, имена и инструкции мгновенно переключатся (L)";
                break;
            case "es":
                headerTitle = "🌐 SELECCIONA EL IDIOMA DEL JUEGO / GAME LANGUAGE";
                headerSubtitle = "Elige tu idioma preferido. Toda la interfaz, nombres y tutoriales cambiarán al instante (L)";
                break;
            case "fr":
                headerTitle = "🌐 CHOISIR LA LANGUE DU JEU / GAME LANGUAGE";
                headerSubtitle = "Choisissez votre langue. L'ensemble des textes, héros et guides s'adapteront immédiatement (L)";
                break;
            case "de":
                headerTitle = "🌐 SPIELSPRACHE WÄHLEN / GAME LANGUAGE";
                headerSubtitle = "Wählen Sie Ihre bevorzugte Sprache. Menüs, Charaktere und Videos wechseln sofort (L)";
                break;
            case "hi":
                headerTitle = "🌐 खेल की भाषा चुनें / GAME LANGUAGE";
                headerSubtitle = "अपनी पसंदीदा भाषा चुनें। खेल के सभी मेनू, नाम और गाइड तुरंत इस भाषा में बदल जाएंगे (L)";
                break;
            case "zh":
                headerTitle = "🌐 选择游戏语言 / SELECT GAME LANGUAGE";
                headerSubtitle = "选择您的母语。所有菜单、角色名称及教程将立即切换至该语言 (L)";
                break;
            case "ja":
                headerTitle = "🌐 言語を選択してください / SELECT GAME LANGUAGE";
                headerSubtitle = "お好みの言語を選択してください。すべてのUI、英雄名、解説が瞬時に切り替わります (L)";
                break;
            case "pt":
                headerTitle = "🌐 SELECIONE O IDIOMA DO JOGO / GAME LANGUAGE";
                headerSubtitle = "Escolha seu idioma favorito. Todo o jogo, heróis e tutoriais mudarão instantaneamente (L)";
                break;
            case "it":
                headerTitle = "🌐 SELEZIONA LA LINGUA DEL GIOCO / GAME LANGUAGE";
                headerSubtitle = "Scegli la tua lingua preferita. Tutti i menu, nomi e guide si aggiorneranno immediatamente (L)";
                break;
            case "tr":
                headerTitle = "🌐 OYUN DİLİNİ SEÇİN / GAME LANGUAGE";
                headerSubtitle = "Tercih ettiğiniz dili seçin. Tüm menüler, isimler ve rehberler anında güncellenecektir (L)";
                break;
            default:
                headerTitle = "🌐 SELECT GAME LANGUAGE / CHOISIR LA LANGUE";
                headerSubtitle = "Select your preferred language. All game menus, names, and briefings will update immediately (L)";
                break;
        }

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);
        GUI.Label(new Rect(mx, my + 14f, modalW, 28f), headerTitle, titleStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 12;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.85f, 0.95f, 0.90f);
        GUI.Label(new Rect(mx + 20f, my + 44f, modalW - 40f, 22f), headerSubtitle, subStyle);

        // Active Language Banner
        Rect activeBannerRect = new Rect(mx + 24f, my + 72f, modalW - 48f, 42f);
        EnchantedNatureGUI.DrawWoodPlate(activeBannerRect, new Color(0.08f, 0.18f, 0.22f, 0.95f), new Color(0.35f, 0.92f, 1f));

        // Draw Active Flag
        Texture2D activeFlag = GetFlagTexture(activeOpt.code);
        if (activeFlag != null)
        {
            GUI.DrawTexture(new Rect(activeBannerRect.x + 14f, activeBannerRect.y + 9f, 36f, 24f), activeFlag);
        }

        GUIStyle activeStyle = new GUIStyle(GUI.skin.label);
        activeStyle.fontSize = 13;
        activeStyle.fontStyle = FontStyle.Bold;
        activeStyle.alignment = TextAnchor.MiddleLeft;
        activeStyle.normal.textColor = new Color(1f, 0.95f, 0.5f);

        string bannerMsg;
        switch (activeOpt.code)
        {
            case "ar":
                bannerMsg = ArabicFixer.Fix($"اللغة المختارة: العربية (المملكة العربية السعودية والوطن العربي)  —  [كامل اللعبة باللغة العربية مع دعم الخط المتصل]");
                break;
            case "it":
                bannerMsg = $"LINGUA ATTIVA: Italiano (Italia)  —  [TRADUZIONE COMPLETA DEI MENU E GUIDA]";
                break;
            case "tr":
                bannerMsg = $"AKTİF DİL: Türkçe (Türkiye)  —  [MENÜLER VE KILAVUZLAR TAMAMEN TÜRKÇE]";
                break;
            case "fa":
                bannerMsg = ArabicFixer.Fix($"زبان انتخاب‌شده: فارسی (ایران و خاورمیانه)  —  [پشتیبانی کامل از زبان فارسی]");
                break;
            case "ru":
                bannerMsg = $"АКТИВНЫЙ ЯЗЫК: Русский (Россия)  —  [ПОЛНЫЙ ПЕРЕВОД МЕНЮ И БРИФИНГА]";
                break;
            case "fr":
                bannerMsg = $"LANGUE SÉLECTIONNÉE : Français (France)  —  [TRADUCTION COMPLÈTE EN DIRECT]";
                break;
            case "es":
                bannerMsg = $"IDIOMA SELECCIONADO: Español (España / Latam)  —  [SISTEMA TOTALMENTE TRADUCIDO]";
                break;
            case "de":
                bannerMsg = $"GEWÄHLTE SPRACHE: Deutsch (Deutschland)  —  [VOLLSTÄNDIGE LOKALISIERUNG]";
                break;
            case "hi":
                bannerMsg = $"सक्रिय भाषा: हिन्दी (भारत)  —  [सभी मेनू और चरित्र पूरी तरह अनुवादित]";
                break;
            case "zh":
                bannerMsg = $"当前选择: 中文 (中国)  —  [全部界面与英雄即刻汉化]";
                break;
            case "ja":
                bannerMsg = $"現在の言語: 日本語 (日本)  —  [完全ローカライズ対応]";
                break;
            case "pt":
                bannerMsg = $"IDIOMA ATUAL: Português (Brasil / Portugal)  —  [JOGO TOTALMENTE TRADUZIDO]";
                break;
            default:
                bannerMsg = $"CURRENT LANGUAGE: English (Global)  —  [ORIGINAL FULL SYSTEM ACTIVE]";
                break;
        }

        GUI.Label(new Rect(activeBannerRect.x + 60f, activeBannerRect.y + 10f, activeBannerRect.width - 70f, 24f), bannerMsg, activeStyle);

        // Scrollable List of 10 Languages
        float listY = my + 122f;
        float listH = modalH - 182f;
        Rect scrollOuter = new Rect(mx + 24f, listY, modalW - 48f, listH);
        float itemH = 46f;
        float virtualH = AvailableLanguages.Length * (itemH + 6f);
        Rect scrollInner = new Rect(0, 0, scrollOuter.width - 20f, virtualH);

        listScrollPos = GUI.BeginScrollView(scrollOuter, listScrollPos, scrollInner, false, true);

        for (int i = 0; i < AvailableLanguages.Length; i++)
        {
            LanguageOption opt = AvailableLanguages[i];
            bool isSelected = (selectedIndex == i);
            Rect itemRect = new Rect(0, i * (itemH + 6f), scrollInner.width, itemH);

            Color bgCol = isSelected
                ? new Color(0.12f, 0.28f, 0.20f, 0.98f)
                : new Color(0.08f, 0.12f, 0.10f, 0.92f);
            Color borderCol = isSelected
                ? new Color(1f, 0.85f, 0.28f)
                : new Color(0.35f, 0.85f, 0.55f);

            EnchantedNatureGUI.DrawLeafPlaque(itemRect, bgCol, borderCol);

            // Draw National Flag Texture
            Texture2D rowFlag = GetFlagTexture(opt.code);
            if (rowFlag != null)
            {
                GUI.DrawTexture(new Rect(itemRect.x + 14f, itemRect.y + 11f, 36f, 24f), rowFlag);
            }

            // Display Name (Formatted for RTL if Arabic)
            string displayNative = (opt.code == "ar") ? ArabicFixer.Fix("العربية") : opt.nativeName;
            string displayLabel = $"{opt.englishName}  —  {displayNative}";

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.fontSize = 14;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.alignment = TextAnchor.MiddleLeft;
            nameStyle.normal.textColor = isSelected ? new Color(1f, 0.92f, 0.4f) : Color.white;

            GUI.Label(new Rect(itemRect.x + 60f, itemRect.y + 4f, 350f, 22f), displayLabel, nameStyle);

            GUIStyle regionStyle = new GUIStyle(GUI.skin.label);
            regionStyle.fontSize = 11;
            regionStyle.normal.textColor = new Color(0.75f, 0.85f, 0.80f);
            string regionText = (opt.code == "ar") ? ArabicFixer.Fix(opt.region) : opt.region;
            GUI.Label(new Rect(itemRect.x + 60f, itemRect.y + 24f, 350f, 18f), regionText, regionStyle);

            // Status Badge Button
            Rect badgeRect = new Rect(itemRect.x + itemRect.width - 185f, itemRect.y + 9f, 170f, 28f);
            string badgeText;
            if (isSelected)
            {
                badgeText = (activeOpt.code == "ar") ? ArabicFixer.Fix("✔ مختارة") : "★ SELECTED";
            }
            else
            {
                badgeText = (activeOpt.code == "ar") ? ArabicFixer.Fix("اختر هذه اللغة") : "SELECT";
            }

            Color badgeColor = isSelected 
                ? new Color(1f, 0.82f, 0.25f) 
                : new Color(0.3f, 0.9f, 0.5f);

            if (EnchantedNatureGUI.DrawOrganicLeafButton(badgeRect, badgeText, badgeColor, 11))
            {
                selectedIndex = i;
                PlayerPrefs.SetString("GameLanguage", opt.code);
                PlayerPrefs.Save();
            }

            // Clicking entire row selects
            if (GUI.Button(itemRect, "", GUIStyle.none))
            {
                selectedIndex = i;
                PlayerPrefs.SetString("GameLanguage", opt.code);
                PlayerPrefs.Save();
            }
        }

        GUI.EndScrollView();

        // Bottom Action Bar
        float bottomY = my + modalH - 50f;

        // Dynamic Start / Continue Button
        bool inMatch = IsInActiveMatch;
        string actionBtnText;

        if (inMatch)
        {
            switch (activeOpt.code)
            {
                case "ar":
                    actionBtnText = ArabicFixer.Fix("▶ مواصلة اللعب بالعربية (متابعة)");
                    break;
                case "fa":
                    actionBtnText = ArabicFixer.Fix("▶ ادامه بازی به زبان فارسی");
                    break;
                case "ru":
                    actionBtnText = "▶ ПРОДОЛЖИТЬ ИГРУ НА РУССКОМ";
                    break;
                case "es":
                    actionBtnText = "▶ CONTINUAR PARTIDA EN ESPAÑOL";
                    break;
                case "fr":
                    actionBtnText = "▶ CONTINUER LA PARTIE EN FRANÇAIS";
                    break;
                case "de":
                    actionBtnText = "▶ WEITERSPIELEN AUF DEUTSCH";
                    break;
                case "hi":
                    actionBtnText = "▶ हिन्दी में खेल जारी रखें";
                    break;
                case "zh":
                    actionBtnText = "▶ 继续游戏 (简体中文)";
                    break;
                case "ja":
                    actionBtnText = "▶ 日本語でゲームを続ける";
                    break;
                case "pt":
                    actionBtnText = "▶ CONTINUAR JOGO EM PORTUGUÊS";
                    break;
                case "it":
                    actionBtnText = "▶ CONTINUA A GIOCARE IN ITALIANO";
                    break;
                case "tr":
                    actionBtnText = "▶ TÜRKÇE OYNAMAYA DEVAM ET";
                    break;
                default:
                    actionBtnText = "▶ CONTINUE PLAYING IN ENGLISH";
                    break;
            }
        }
        else
        {
            switch (activeOpt.code)
            {
                case "ar":
                    actionBtnText = ArabicFixer.Fix("▶ ابدأ اللعبة بالعربية (تأكيد)");
                    break;
                case "fa":
                    actionBtnText = ArabicFixer.Fix("▶ شروع بازی به زبان فارسی (تأیید)");
                    break;
                case "ru":
                    actionBtnText = "▶ НАЧАТЬ ИГРУ НА РУССКОМ";
                    break;
                case "es":
                    actionBtnText = "▶ INICIAR JUEGO EN ESPAÑOL";
                    break;
                case "fr":
                    actionBtnText = "▶ COMMENCER LE JEU EN FRANÇAIS";
                    break;
                case "de":
                    actionBtnText = "▶ SPIEL STARTEN AUF DEUTSCH";
                    break;
                case "hi":
                    actionBtnText = "▶ हिन्दी में खेल शुरू करें";
                    break;
                case "zh":
                    actionBtnText = "▶ 开始游戏 (简体中文)";
                    break;
                case "ja":
                    actionBtnText = "▶ 日本語でゲームを開始";
                    break;
                case "pt":
                    actionBtnText = "▶ INICIAR JOGO EM PORTUGUÊS";
                    break;
                case "it":
                    actionBtnText = "▶ INIZIA IL GIOCO IN ITALIANO";
                    break;
                case "tr":
                    actionBtnText = "▶ OYUNU TÜRKÇE BAŞLAT";
                    break;
                default:
                    actionBtnText = "▶ START GAME IN ENGLISH";
                    break;
            }
        }

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + 24f, bottomY, 320f, 36f), actionBtnText, new Color(0.25f, 0.95f, 0.45f), 12))
        {
            PlayerPrefs.SetString("GameLanguage", AvailableLanguages[selectedIndex].code);
            PlayerPrefs.SetInt("LanguagePromptSeen", 1);
            PlayerPrefs.Save();
            showLanguageModal = false;
        }

        // Close Button
        string closeBtnText;
        switch (activeOpt.code)
        {
            case "ar":
                closeBtnText = ArabicFixer.Fix("إغلاق ❌");
                break;
            case "fa":
                closeBtnText = ArabicFixer.Fix("بستن ❌");
                break;
            case "ru":
                closeBtnText = "❌ ЗАКРЫТЬ (ESC)";
                break;
            case "fr":
                closeBtnText = "❌ FERMER (ESC)";
                break;
            case "es":
                closeBtnText = "❌ CERRAR (ESC)";
                break;
            case "de":
                closeBtnText = "❌ SCHLIESSEN (ESC)";
                break;
            case "hi":
                closeBtnText = "❌ बंद करें (ESC)";
                break;
            case "zh":
                closeBtnText = "❌ 关闭 (ESC)";
                break;
            case "ja":
                closeBtnText = "❌ 閉じる (ESC)";
                break;
            case "pt":
                closeBtnText = "❌ FECHAR (ESC)";
                break;
            case "it":
                closeBtnText = "❌ CHIUDI (ESC)";
                break;
            case "tr":
                closeBtnText = "❌ KAPAT (ESC)";
                break;
            default:
                closeBtnText = "❌ CLOSE (ESC)";
                break;
        }

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(mx + modalW - 160f, bottomY, 136f, 36f), closeBtnText, new Color(0.85f, 0.45f, 0.35f), 12))
        {
            showLanguageModal = false;
        }
    }
}
