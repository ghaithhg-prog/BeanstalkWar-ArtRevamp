using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ForestBGMManager:
/// نظام خزانة الموسيقى المتعددة (Multi-Track Music Jukebox) لشجرة الفاصولياء العملاقة (BeanstalkWar).
/// يحتوي على 12 نمطاً موسيقياً إجرائياً سينمائياً متنوعاً (شرقي، تركي، فرنسي، جاز، روك، فايكنج، أوركسترالي، هادئ، رعب، سريع...)
/// مع إمكانية كتم الصوت بالكامل، وتغيير الموسيقى في أي لحظة أثناء المعركة أو من قائمة الإيقاف المؤقت [ESC].
/// </summary>
public class ForestBGMManager : MonoBehaviour
{
    public static ForestBGMManager Instance { get; private set; }

    public enum MusicTrack
    {
        Mute = 0,
        ArabicHijazOud = 1,
        TurkishSazNey = 2,
        FrenchMusetteAccordion = 3,
        MidnightJazzSwing = 4,
        HeavyBattleRock = 5,
        HyperSpeedRush = 6,
        GrandAdventure = 7,
        DarkHorrorSuspense = 8,
        SerenePianoForest = 9,
        NordicVikingHymn = 10,
        EpicFantasyTheme = 11
    }

    public struct TrackMeta
    {
        public MusicTrack track;
        public string titleAr;
        public string titleEn;
        public string descAr;
        public string descEn;
        public string icon;

        public TrackMeta(MusicTrack track, string icon, string titleAr, string titleEn, string descAr, string descEn)
        {
            this.track = track;
            this.icon = icon;
            this.titleAr = titleAr;
            this.titleEn = titleEn;
            this.descAr = descAr;
            this.descEn = descEn;
        }
    }

    public static readonly TrackMeta[] AvailableTracks = new TrackMeta[]
    {
        new TrackMeta(MusicTrack.Mute, "🔇", "كتم الصوت بالكامل", "Mute Audio Completely", "إيقاف الموسيقى التصويرية تماماً واللعب في هدوء تام", "Completely mute music for quiet focus"),
        new TrackMeta(MusicTrack.ArabicHijazOud, "🪕", "عود وحجاز شرقي أصيل", "Arabic Oud & Hijaz", "تقاسيم عود أندلسي بمقام الحجاز ودربكة ورق ساحر", "Authentic Arabian Oud in Hijaz mode with hypnotic darbuka percussion"),
        new TrackMeta(MusicTrack.TurkishSazNey, "🪈", "صاز وناي تركي صوفي", "Turkish Saz & Mystical Ney", "أنغام الناي الصوفي الشجي وأوتار الصاز العذبة", "Deep mystical reed flute with gentle baglama saz tremolos"),
        new TrackMeta(MusicTrack.FrenchMusetteAccordion, "🪗", "أكورديون باريسي فرنسي", "French Musette Accordion", "فالس رومانسي مبهج بنبضات الأكورديون الباريسي الخفيف", "Joyful Parisian cafe waltz with sparkling detuned musette reeds"),
        new TrackMeta(MusicTrack.MidnightJazzSwing, "🎷", "جاز وسوينغ ليلي متألق", "Midnight Jazz & Swing", "باص مائل ناعم مع هاي-هات خفيف ونغمات ترومبيت دافئة", "Smooth walking bass with swinging ride cymbals and warm brass"),
        new TrackMeta(MusicTrack.HeavyBattleRock, "🎸", "روك حماسي بطبول ثقيلة", "Heavy Battle Rock", "طبول حربية قوية وباور كوردز جيتار كهربائي هادر", "Driving hard rock beats with aggressive distorted power chords"),
        new TrackMeta(MusicTrack.HyperSpeedRush, "⚡", "إيقاع الركض السريع الفائق", "Hyper Speed Rush", "نبضات متسارعة 148 BPM لركض ملحمي وحماس لا يتوقف", "Pounding 148 BPM sprint rhythm with galloping war drums"),
        new TrackMeta(MusicTrack.GrandAdventure, "🏰", "مغامرة أوركسترالية واسعة", "Grand Adventure Fanfare", "أبواق نحاسية ملحمية ووتريات أوركسترالية نصر بطولية", "Triumphant orchestral brass fanfare with soaring epic strings"),
        new TrackMeta(MusicTrack.DarkHorrorSuspense, "🕯️", "رعب وغموض وأوتار مشدودة", "Dark Horror & Suspense", "أجواء تشويق غامضة ونغمات وتريات مقلقة ودقات مرعبة", "Eerie microtonal strings and tense sub-bass cinema pulses"),
        new TrackMeta(MusicTrack.SerenePianoForest, "🎹", "بيانو هادئ وسكينة الطبيعة", "Serene Piano & Forest Birds", "عزف بيانو رقيق يبعث على الراحة والسكينة في أحضان الغابة", "Peaceful lyrical acoustic piano surrounded by calm nature chimes"),
        new TrackMeta(MusicTrack.NordicVikingHymn, "🛡️", "ترنيمة الفايكنج الإسكندنافية", "Nordic Viking War Hymn", "طبول الفايكنج الضخمة وأبواق الحرب القديمة مع باص زلزالي", "Massive primal war drums, ancient bronze horns, and Norse drone"),
        new TrackMeta(MusicTrack.EpicFantasyTheme, "🌿", "الملحمة الكلاسيكية لشجرة الفاصولياء", "Epic Beanstalk Fantasy Theme", "المقطوعة الملحمية الأصلية بطبول التاييكو وأبواق الحرب والهارب", "Original Beanstalk theme with thunderous Taiko and French horns")
    };

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.45f;

    private AudioSource audioSource;
    private static Dictionary<MusicTrack, AudioClip> cachedTracks = new Dictionary<MusicTrack, AudioClip>();
    private static bool showJukeboxModal = false;
    private Vector2 scrollPos = Vector2.zero;

    public static MusicTrack CurrentTrack { get; private set; } = MusicTrack.EpicFantasyTheme;
    public bool IsMutedTrack => CurrentTrack == MusicTrack.Mute;
    public static bool IsMuted => Instance != null && Instance.IsMutedTrack;
    public static bool IsJukeboxOpen => showJukeboxModal;

    public static void OpenJukebox() => showJukeboxModal = true;
    public static void CloseJukebox() => showJukeboxModal = false;
    public static void ToggleJukeboxModal() => showJukeboxModal = !showJukeboxModal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject bgmObj = new GameObject("ForestBGMManager");
            bgmObj.AddComponent<ForestBGMManager>();
            DontDestroyOnLoad(bgmObj);
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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = musicVolume;

        int savedTrack = PlayerPrefs.GetInt("BGM_SelectedTrack", (int)MusicTrack.EpicFantasyTheme);
        CurrentTrack = (MusicTrack)Mathf.Clamp(savedTrack, 0, 11);
    }

    void Start()
    {
        PlayTrack(CurrentTrack);
    }

    void Update()
    {
        if (audioSource != null)
        {
            float targetVol = (CurrentTrack == MusicTrack.Mute) ? 0f : musicVolume;
            if (GameStateManager.Instance != null && GameStateManager.Instance.currentState == GameStateManager.GameState.Ended)
            {
                targetVol *= 0.5f;
            }

            audioSource.volume = Mathf.Lerp(audioSource.volume, targetVol, 4f * Time.deltaTime);
        }
    }

    public void SetVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (audioSource != null && CurrentTrack != MusicTrack.Mute)
        {
            audioSource.volume = musicVolume;
        }
    }

    public void PlayTrack(MusicTrack track)
    {
        CurrentTrack = track;
        PlayerPrefs.SetInt("BGM_SelectedTrack", (int)track);
        PlayerPrefs.Save();

        if (track == MusicTrack.Mute)
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
            return;
        }

        if (!cachedTracks.ContainsKey(track) || cachedTracks[track] == null)
        {
            cachedTracks[track] = GenerateTrack(track);
        }

        AudioClip clip = cachedTracks[track];
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.time = 0f;
            audioSource.volume = musicVolume;
            audioSource.Play();
        }
    }

    private AudioClip GenerateTrack(MusicTrack track)
    {
        switch (track)
        {
            case MusicTrack.ArabicHijazOud: return GenerateArabicHijazOud();
            case MusicTrack.TurkishSazNey: return GenerateTurkishSazNey();
            case MusicTrack.FrenchMusetteAccordion: return GenerateFrenchMusetteAccordion();
            case MusicTrack.MidnightJazzSwing: return GenerateMidnightJazzSwing();
            case MusicTrack.HeavyBattleRock: return GenerateHeavyBattleRock();
            case MusicTrack.HyperSpeedRush: return GenerateHyperSpeedRush();
            case MusicTrack.GrandAdventure: return GenerateGrandAdventure();
            case MusicTrack.DarkHorrorSuspense: return GenerateDarkHorrorSuspense();
            case MusicTrack.SerenePianoForest: return GenerateSerenePianoForest();
            case MusicTrack.NordicVikingHymn: return GenerateNordicVikingHymn();
            case MusicTrack.EpicFantasyTheme:
            default:
                return GenerateEpicFantasyTheme();
        }
    }

    void OnGUI()
    {
        if (showJukeboxModal)
        {
            GUI.depth = -900;
            DrawJukeboxModal();
        }
    }

    private void DrawJukeboxModal()
    {
        // Darkened atmospheric emerald background
        GUI.color = new Color(0.02f, 0.05f, 0.03f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(820f, Screen.width - 24f);
        float modalH = Mathf.Min(580f, Screen.height - 24f);
        float mx = (Screen.width - modalW) / 2f;
        float my = (Screen.height - modalH) / 2f;

        EnchantedNatureGUI.DrawLeafPlaque(new Rect(mx, my, modalW, modalH), new Color(0.06f, 0.12f, 0.08f, 0.98f), new Color(0.35f, 0.88f, 0.55f));

        // Header Title
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);

        string titleText = LanguageSelectionUI.Localize(
            "🎵 MUSIC JUKEBOX: SELECT SOUNDTRACK 🎵",
            ar: "🎵 خزانة الموسيقى: اختر النمط المفضل 🎵",
            fa: "🎵 جعبه موسیقی: انتخاب آهنگ بازی 🎵",
            ru: "🎵 МУЗЫКАЛЬНЫЙ АВТОМАТ: ВЫБОР ТЕМЫ 🎵",
            fr: "🎵 JUKEBOX: CHOISIR LA MUSIQUE 🎵",
            es: "🎵 JUKEBOX: ELEGIR BANDA SONORA 🎵"
        );
        GUI.Label(new Rect(mx + 20f, my + 14f, modalW - 40f, 28f), titleText, headerStyle);

        // Subtitle
        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };
        subStyle.normal.textColor = new Color(0.7f, 0.92f, 0.78f);

        string subText = LanguageSelectionUI.Localize(
            "~ Choose from 12 procedural battle themes or mute music entirely ~",
            ar: "~ اختر من بين 12 نمطاً موسيقياً ملحمياً للمعركة أو اكتم الصوت تماماً ~",
            fa: "~ از بین ۱۲ سبک موسیقی حماسی انتخاب کنید یا صدا را ببندید ~",
            ru: "~ Выберите из 12 боевых тем или выключите музыку ~",
            fr: "~ Choisissez parmi 12 thèmes épiques ou coupez la musique ~",
            es: "~ Elige entre 12 temas de batalla o silencia la música ~"
        );
        GUI.Label(new Rect(mx + 20f, my + 42f, modalW - 40f, 18f), subText, subStyle);

        // Volume Bar Controls
        float volY = my + 66f;
        Rect volPlate = new Rect(mx + 24f, volY, modalW - 48f, 32f);
        EnchantedNatureGUI.DrawWoodPlate(volPlate, new Color(0.08f, 0.15f, 0.10f, 0.92f), new Color(0.95f, 0.80f, 0.35f, 0.85f));

        GUIStyle volLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        volLabelStyle.normal.textColor = new Color(1f, 0.92f, 0.45f);

        int volPct = IsMutedTrack ? 0 : Mathf.RoundToInt(musicVolume * 100f);
        string volStr = LanguageSelectionUI.Localize($"🔊 VOLUME: {volPct}%", ar: $"🔊 مستوى الصوت: {volPct}%", ru: $"🔊 ГРОМКОСТЬ: {volPct}%");
        GUI.Label(new Rect(mx + 36f, volY + 5f, 140f, 22f), volStr, volLabelStyle);

        float sliderX = mx + 180f;
        float sliderW = modalW - 320f;
        float newVol = GUI.HorizontalSlider(new Rect(sliderX, volY + 10f, sliderW, 18f), musicVolume, 0f, 1f);
        if (Mathf.Abs(newVol - musicVolume) > 0.01f)
        {
            SetVolume(newVol);
        }

        // Mute shortcut button inside volume plate
        string muteText = IsMutedTrack ? "🔇 UNMUTE" : "🔇 MUTE";
        if (GUI.Button(new Rect(mx + modalW - 120f, volY + 4f, 86f, 24f), muteText))
        {
            if (IsMutedTrack) PlayTrack(MusicTrack.EpicFantasyTheme);
            else PlayTrack(MusicTrack.Mute);
        }

        // Track List Grid (Scrollable)
        float gridY = volY + 40f;
        float gridH = modalH - 165f;
        Rect viewRect = new Rect(mx + 18f, gridY, modalW - 36f, gridH);
        float totalContentH = (AvailableTracks.Length / 2) * 66f + 10f;
        Rect contentRect = new Rect(0, 0, modalW - 60f, totalContentH);

        scrollPos = GUI.BeginScrollView(viewRect, scrollPos, contentRect, false, true);

        float colW = (contentRect.width - 12f) / 2f;
        float cardH = 58f;

        for (int i = 0; i < AvailableTracks.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            float cx = col * (colW + 12f);
            float cy = row * (cardH + 8f);

            TrackMeta tm = AvailableTracks[i];
            bool isCurrent = (tm.track == CurrentTrack);

            Color borderCol = isCurrent ? new Color(0.25f, 0.98f, 0.45f) : new Color(0.85f, 0.72f, 0.32f, 0.75f);
            Color bgCol = isCurrent ? new Color(0.08f, 0.22f, 0.12f, 0.96f) : new Color(0.07f, 0.12f, 0.09f, 0.92f);

            EnchantedNatureGUI.DrawLeafPlaque(new Rect(cx, cy, colW, cardH), bgCol, borderCol);

            string title = LanguageSelectionUI.Localize(tm.titleEn, ar: tm.titleAr);
            string desc = LanguageSelectionUI.Localize(tm.descEn, ar: tm.descAr);

            GUIStyle cardTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            cardTitle.normal.textColor = isCurrent ? new Color(0.4f, 1f, 0.5f) : Color.white;

            GUIStyle cardDesc = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            cardDesc.normal.textColor = new Color(0.72f, 0.85f, 0.75f);

            GUI.Label(new Rect(cx + 8f, cy + 6f, colW - 85f, 20f), $"{tm.icon} {title}", cardTitle);
            GUI.Label(new Rect(cx + 8f, cy + 28f, colW - 85f, 24f), desc, cardDesc);

            // Select / Active Action Button
            string btnLabel = isCurrent ? "▶ PLAYING" : "SELECT";
            Color btnAcc = isCurrent ? new Color(0.25f, 0.95f, 0.45f) : EnchantedNatureGUI.AntiqueGoldMid;

            if (GUI.Button(new Rect(cx + colW - 74f, cy + 14f, 68f, 30f), btnLabel))
            {
                PlayTrack(tm.track);
            }
        }

        GUI.EndScrollView();

        // Footer Close Button
        float closeBtnW = 160f;
        float closeBtnH = 34f;
        float closeX = mx + (modalW - closeBtnW) / 2f;
        float closeY = my + modalH - 44f;

        string closeLabel = LanguageSelectionUI.Localize("✕ CLOSE", ar: "✕ إغلاق", ru: "✕ ЗАКРЫТЬ", es: "✕ CERRAR", fr: "✕ FERMER");
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(closeX, closeY, closeBtnW, closeBtnH), closeLabel, new Color(0.35f, 0.85f, 0.95f), 11))
        {
            CloseJukebox();
        }
    }

    // =========================================================================
    // Procedural Music Synthesis Engine: 11 Distinct Musical Themes
    // =========================================================================

    public static AudioClip GenerateEpicFantasyTheme()
    {
        int sampleRate = 22050;
        float bpm = 114f;
        float beatDuration = 60f / bpm;
        int totalBars = 16;
        float totalTime = totalBars * 4f * beatDuration;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] leftChannel = new float[totalSamples];
        float[] rightChannel = new float[totalSamples];

        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStart = bar * 4f * beatDuration;
            AddHeavyTaiko(leftChannel, rightChannel, barStart, sampleRate, 1.35f);
            AddWarSnare(leftChannel, rightChannel, barStart + beatDuration, sampleRate, 0.95f);
            AddHeavyTaiko(leftChannel, rightChannel, barStart + 2f * beatDuration, sampleRate, 1.25f);
            AddWarSnare(leftChannel, rightChannel, barStart + 3f * beatDuration, sampleRate, 1.05f);

            AddWarTom(leftChannel, rightChannel, barStart + 1.5f * beatDuration, sampleRate, 0.85f, 95f, -0.45f);
            AddWarTom(leftChannel, rightChannel, barStart + 3.5f * beatDuration, sampleRate, 0.90f, 130f, 0.45f);
        }

        float[] hornPitches = { 146.83f, 174.61f, 196.00f, 220.00f, 261.63f, 293.66f };
        for (int bar = 0; bar < totalBars; bar += 2)
        {
            float t = bar * 4f * beatDuration;
            int pIdx = (bar / 2) % hornPitches.Length;
            AddHeroicHorn(leftChannel, rightChannel, t + 0.1f, beatDuration * 2.2f, hornPitches[pIdx], sampleRate, 0.45f);
            AddHeroicHorn(leftChannel, rightChannel, t + 2.5f * beatDuration, beatDuration * 1.4f, hornPitches[(pIdx + 2) % hornPitches.Length], sampleRate, 0.42f);
        }

        return CreateStereoClip("BGM_EpicFantasy", leftChannel, rightChannel, sampleRate);
    }

    private static AudioClip GenerateArabicHijazOud()
    {
        int sampleRate = 22050;
        float bpm = 108f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        // Hijaz scale on D: D3(146.8), Eb3(155.6), F#3(185.0), G3(196.0), A3(220.0), Bb3(233.1), C4(261.6), D4(293.7)
        float[] hijaz = { 146.83f, 155.56f, 185.00f, 196.00f, 220.00f, 233.08f, 261.63f, 293.66f, 370.00f };

        // Darbuka & Riqq Maqsum Rhythm (Doom Tek ... Tek Doom ... Tek)
        for (int b = 0; b < bars; b++)
        {
            float t = b * 4f * beat;
            AddDarbukaHit(left, right, t, sampleRate, true, 1.2f);                      // Doom
            AddDarbukaHit(left, right, t + beat, sampleRate, false, 0.85f);             // Tek
            AddDarbukaHit(left, right, t + 1.5f * beat, sampleRate, false, 0.65f);      // tek
            AddDarbukaHit(left, right, t + 2.0f * beat, sampleRate, true, 1.0f);        // Doom
            AddDarbukaHit(left, right, t + 3.0f * beat, sampleRate, false, 0.95f);      // Tek
            AddDarbukaHit(left, right, t + 3.5f * beat, sampleRate, false, 0.70f);      // tek
        }

        // Melodic Oud Takasim on Hijaz
        int[] oudNotes = { 0, 1, 2, 3, 2, 1, 0, 2, 3, 4, 3, 2, 4, 5, 4, 3, 2, 1, 0 };
        for (int i = 0; i < oudNotes.Length; i++)
        {
            float t = i * beat * 0.82f;
            if (t + 1.2f >= totalTime) break;
            float freq = hijaz[oudNotes[i]];
            AddOudPluck(left, right, t, freq, sampleRate, 0.52f, (i % 2 == 0) ? -0.25f : 0.25f);
        }

        return CreateStereoClip("BGM_ArabicHijazOud", left, right, sampleRate);
    }

    private static AudioClip GenerateTurkishSazNey()
    {
        int sampleRate = 22050;
        float bpm = 96f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        // Ney & Saz Rast / Ussak mode: A2(110), B2(123.5), C3(130.8), D3(146.8), E3(164.8), F3(174.6), G3(196)
        float[] sazPitches = { 110.00f, 130.81f, 146.83f, 164.81f, 174.61f, 196.00f, 220.00f, 261.63f, 293.66f };

        // Soft frame drum (Bendir) heartbeat
        for (int b = 0; b < bars; b++)
        {
            float t = b * 4f * beat;
            AddHeavyTaiko(left, right, t, sampleRate, 0.9f);
            AddWarTom(left, right, t + 2f * beat, sampleRate, 0.65f, 110f, 0.2f);
        }

        // Breathy Ney flute long notes
        float[] neyFreqs = { 220.00f, 246.94f, 261.63f, 293.66f, 329.63f, 293.66f, 261.63f, 220.00f };
        for (int i = 0; i < neyFreqs.Length; i++)
        {
            float t = i * 4f * beat;
            if (t >= totalTime) break;
            AddFluteNote(left, right, t + 0.1f, beat * 3.5f, neyFreqs[i], sampleRate, 0.42f);
        }

        // Plucked Saz tremolos
        for (int i = 0; i < 28; i++)
        {
            float t = i * beat * 0.95f;
            if (t + 1f >= totalTime) break;
            float freq = sazPitches[i % sazPitches.Length];
            AddOudPluck(left, right, t, freq * 1.5f, sampleRate, 0.38f, 0.35f);
        }

        return CreateStereoClip("BGM_TurkishSazNey", left, right, sampleRate);
    }

    private static AudioClip GenerateFrenchMusetteAccordion()
    {
        int sampleRate = 22050;
        float bpm = 132f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 3f * beat; // 3/4 Waltz time!
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        // C major / A minor Parisian Waltz: C4(261.6), D4(293.7), E4(329.6), G4(392.0), A4(440.0), C5(523.3)
        float[] melNotes = { 329.63f, 392.00f, 523.25f, 440.00f, 392.00f, 329.63f, 293.66f, 329.63f };

        // 3/4 Bass-Chord-Chord Oom-pah-pah waltz
        for (int b = 0; b < bars; b++)
        {
            float t = b * 3f * beat;
            float root = (b % 4 < 2) ? 130.81f : 110.00f; // C3 or A2
            AddHeavyTaiko(left, right, t, sampleRate, 0.75f); // Bass on beat 1

            // Accordion chords on beat 2 and 3
            AddAccordionNote(left, right, t + beat, beat * 0.8f, root * 2f, sampleRate, 0.35f, -0.2f);
            AddAccordionNote(left, right, t + 2f * beat, beat * 0.8f, root * 2.5f, sampleRate, 0.35f, 0.2f);
        }

        for (int i = 0; i < melNotes.Length; i++)
        {
            float t = i * 3f * beat;
            if (t >= totalTime) break;
            AddAccordionNote(left, right, t + 0.05f, beat * 2.2f, melNotes[i], sampleRate, 0.45f, 0.1f);
        }

        return CreateStereoClip("BGM_FrenchMusette", left, right, sampleRate);
    }

    private static AudioClip GenerateMidnightJazzSwing()
    {
        int sampleRate = 22050;
        float bpm = 110f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        float[] walkingBass = { 73.42f, 82.41f, 92.50f, 98.00f, 110.00f, 98.00f, 82.41f, 73.42f };

        for (int b = 0; b < bars; b++)
        {
            float t = b * 4f * beat;
            // Swing ride cymbal pattern: 1, 2, 2-and, 3, 4, 4-and
            AddJazzRide(left, right, t, sampleRate, 0.55f);
            AddJazzRide(left, right, t + beat, sampleRate, 0.65f);
            AddJazzRide(left, right, t + 1.66f * beat, sampleRate, 0.45f);
            AddJazzRide(left, right, t + 2f * beat, sampleRate, 0.55f);
            AddJazzRide(left, right, t + 3f * beat, sampleRate, 0.65f);
            AddJazzRide(left, right, t + 3.66f * beat, sampleRate, 0.45f);

            // Walking acoustic bass
            for (int q = 0; q < 4; q++)
            {
                int idx = (b * 4 + q) % walkingBass.Length;
                AddJazzBassNote(left, right, t + q * beat, beat * 0.9f, walkingBass[idx], sampleRate, 0.75f);
            }
        }

        // Muted jazz brass melody
        float[] jazzHorn = { 220.00f, 261.63f, 293.66f, 329.63f, 392.00f, 349.23f, 293.66f, 261.63f };
        for (int i = 0; i < jazzHorn.Length; i++)
        {
            float t = i * 2f * beat;
            if (t >= totalTime) break;
            AddHeroicHorn(left, right, t + 0.1f, beat * 1.5f, jazzHorn[i], sampleRate, 0.35f);
        }

        return CreateStereoClip("BGM_MidnightJazz", left, right, sampleRate);
    }

    private static AudioClip GenerateHeavyBattleRock()
    {
        int sampleRate = 22050;
        float bpm = 128f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        float[] powerChords = { 73.42f, 73.42f, 87.31f, 98.00f, 73.42f, 110.00f, 98.00f, 87.31f }; // D, F, G, A

        for (int b = 0; b < bars; b++)
        {
            float t = b * 4f * beat;
            // Rock Drum Beat: Kick on 1 and 3, Snare on 2 and 4
            AddHeavyTaiko(left, right, t, sampleRate, 1.4f);
            AddWarSnare(left, right, t + beat, sampleRate, 1.3f);
            AddHeavyTaiko(left, right, t + 2f * beat, sampleRate, 1.35f);
            AddHeavyTaiko(left, right, t + 2.5f * beat, sampleRate, 1.15f);
            AddWarSnare(left, right, t + 3f * beat, sampleRate, 1.35f);

            // Driving distorted power chords
            float root = powerChords[(b / 2) % powerChords.Length];
            AddDistortedGuitarPowerChord(left, right, t, beat * 1.8f, root, sampleRate, 0.55f);
            AddDistortedGuitarPowerChord(left, right, t + 2f * beat, beat * 1.8f, root * 1.189f, sampleRate, 0.52f);
        }

        return CreateStereoClip("BGM_HeavyBattleRock", left, right, sampleRate);
    }

    private static AudioClip GenerateHyperSpeedRush()
    {
        int sampleRate = 22050;
        float bpm = 148f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        for (int b = 0; b < bars; b++)
        {
            float t = b * 4f * beat;
            // Galloping Double-Bass Beats
            AddHeavyTaiko(left, right, t, sampleRate, 1.3f);
            AddHeavyTaiko(left, right, t + beat * 0.5f, sampleRate, 1.1f);
            AddWarSnare(left, right, t + beat, sampleRate, 1.25f);
            AddHeavyTaiko(left, right, t + beat * 2f, sampleRate, 1.3f);
            AddHeavyTaiko(left, right, t + beat * 2.5f, sampleRate, 1.1f);
            AddWarSnare(left, right, t + beat * 3f, sampleRate, 1.3f);
        }

        // Fast sixteenth-note racing synth arpeggios
        float[] arpPitches = { 220.00f, 261.63f, 329.63f, 440.00f, 392.00f, 329.63f, 261.63f, 293.66f };
        for (int i = 0; i < 64; i++)
        {
            float t = i * (beat * 0.25f);
            if (t >= totalTime) break;
            float freq = arpPitches[i % arpPitches.Length];
            AddPluckedHarpNote(left, right, t, freq, sampleRate, 0.42f, (i % 2 == 0) ? -0.4f : 0.4f);
        }

        return CreateStereoClip("BGM_HyperSpeedRush", left, right, sampleRate);
    }

    private static AudioClip GenerateGrandAdventure()
    {
        int sampleRate = 22050;
        float bpm = 112f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        for (int b = 0; b < bars; b++)
        {
            float t = b * 4f * beat;
            AddHeavyTaiko(left, right, t, sampleRate, 1.2f);
            AddWarTom(left, right, t + 2f * beat, sampleRate, 0.85f, 120f, -0.3f);
            AddWarSnare(left, right, t + 3f * beat, sampleRate, 0.95f);
        }

        // Triumphant D Major Brass Fanfare
        float[] brass = { 146.83f, 220.00f, 293.66f, 370.00f, 440.00f, 370.00f, 293.66f, 220.00f };
        for (int i = 0; i < brass.Length; i++)
        {
            float t = i * 2f * beat;
            if (t >= totalTime) break;
            AddHeroicHorn(left, right, t + 0.1f, beat * 1.8f, brass[i], sampleRate, 0.52f);
            AddSustainedStringNote(left, right, t, beat * 2.0f, brass[i] * 0.5f, sampleRate, 0.35f, 0f);
        }

        return CreateStereoClip("BGM_GrandAdventure", left, right, sampleRate);
    }

    private static AudioClip GenerateDarkHorrorSuspense()
    {
        int sampleRate = 22050;
        float bpm = 80f;
        float beat = 60f / bpm;
        int bars = 12;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        for (int b = 0; b < bars; b++)
        {
            float t = b * 4f * beat;
            // Eerie distant heartbeat pulse
            AddHeavyTaiko(left, right, t, sampleRate, 1.0f);
            AddHeavyTaiko(left, right, t + beat * 0.4f, sampleRate, 0.75f);
        }

        // Dissonant suspense strings (Tritone intervals: D - G#)
        float[] horrorFreqs = { 110.00f, 155.56f, 116.54f, 164.81f, 123.47f, 174.61f };
        for (int i = 0; i < horrorFreqs.Length; i++)
        {
            float t = i * 4f * beat;
            if (t >= totalTime) break;
            AddSustainedStringNote(left, right, t, beat * 3.8f, horrorFreqs[i], sampleRate, 0.45f, -0.3f);
            AddSustainedStringNote(left, right, t, beat * 3.8f, horrorFreqs[i] * 1.414f, sampleRate, 0.40f, 0.3f);
        }

        return CreateStereoClip("BGM_DarkHorror", left, right, sampleRate);
    }

    private static AudioClip GenerateSerenePianoForest()
    {
        int sampleRate = 22050;
        float bpm = 90f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        // Lyrical acoustic piano melody in F major
        float[] pianoMelody = { 349.23f, 392.00f, 440.00f, 523.25f, 587.33f, 523.25f, 440.00f, 349.23f };

        for (int i = 0; i < pianoMelody.Length; i++)
        {
            float t = i * 2f * beat;
            if (t >= totalTime) break;
            AddPianoNote(left, right, t, beat * 2.2f, pianoMelody[i], sampleRate, 0.55f, -0.15f);
            // Soft bass note
            AddPianoNote(left, right, t, beat * 2.5f, pianoMelody[i] * 0.25f, sampleRate, 0.65f, 0.15f);
        }

        // Ambient harp & forest chimes
        for (int i = 0; i < 32; i++)
        {
            float t = i * (beat * 0.5f);
            if (t >= totalTime) break;
            if (i % 3 == 0)
            {
                AddPluckedHarpNote(left, right, t, 698.46f, sampleRate, 0.25f, 0.4f);
            }
        }

        return CreateStereoClip("BGM_SerenePiano", left, right, sampleRate);
    }

    private static AudioClip GenerateNordicVikingHymn()
    {
        int sampleRate = 22050;
        float bpm = 88f;
        float beat = 60f / bpm;
        int bars = 16;
        float totalTime = bars * 4f * beat;
        int totalSamples = (int)(totalTime * sampleRate);

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        for (int b = 0; b < bars; b++)
        {
            float t = b * 4f * beat;
            // Massive primal drum thud
            AddHeavyTaiko(left, right, t, sampleRate, 1.45f);
            AddWarTom(left, right, t + beat * 2f, sampleRate, 1.1f, 85f, -0.4f);
            AddHeavyTaiko(left, right, t + beat * 3f, sampleRate, 1.25f);
        }

        // Deep gutteral drone (Low D1 / A1)
        AddVikingDrone(left, right, 0f, totalTime, 73.42f, sampleRate, 0.55f);

        // Ancient Bronze Lur War Horn Call
        float[] lurHorn = { 146.83f, 220.00f, 146.83f, 293.66f, 220.00f, 146.83f };
        for (int i = 0; i < lurHorn.Length; i++)
        {
            float t = i * 2.5f * beat;
            if (t + 2f >= totalTime) break;
            AddHeroicHorn(left, right, t + 0.2f, beat * 2.2f, lurHorn[i], sampleRate, 0.55f);
        }

        return CreateStereoClip("BGM_NordicViking", left, right, sampleRate);
    }

    // =========================================================================
    // Synthesis DSP Instrument Primitives
    // =========================================================================

    private static AudioClip CreateStereoClip(string name, float[] left, float[] right, int sampleRate)
    {
        int totalSamples = left.Length;
        float[] interleaved = new float[totalSamples * 2];
        for (int i = 0; i < totalSamples; i++)
        {
            float l = Mathf.Clamp(Mathf.Sin(left[i] * 0.88f), -0.98f, 0.98f);
            float r = Mathf.Clamp(Mathf.Sin(right[i] * 0.88f), -0.98f, 0.98f);
            interleaved[i * 2] = l;
            interleaved[i * 2 + 1] = r;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 2, sampleRate, false);
        clip.SetData(interleaved, 0);
        return clip;
    }

    private static void AddHeavyTaiko(float[] left, float[] right, float startTime, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int hitSamples = (int)(0.55f * sampleRate);
        float phase = 0f;

        for (int s = 0; s < hitSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float malletClick = Mathf.Exp(-t * 50f) * 3.0f;
            float bodyBoom = Mathf.Exp(-t * 8.5f) * 1.35f;
            float subRumble = Mathf.Exp(-t * 5.0f) * 0.75f;

            float pitch = 54f + Mathf.Exp(-t * 26f) * 126f;
            phase += 2f * Mathf.PI * pitch / sampleRate;

            float drumWave = Mathf.Sin(phase) + Mathf.Sin(phase * 0.5f) * 0.45f + Mathf.Sin(phase * 2f) * 0.22f;
            float sample = (drumWave * (bodyBoom + subRumble) + malletClick) * volume * 0.46f;

            left[idx] += sample;
            right[idx] += sample;
        }
    }

    private static void AddWarSnare(float[] left, float[] right, float startTime, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int hitSamples = (int)(0.28f * sampleRate);
        float phase = 0f;
        var rnd = new System.Random((int)(startTime * 1000));

        for (int s = 0; s < hitSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float env = Mathf.Exp(-t * 22f);
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 35f);

            float pitch = 150f + Mathf.Exp(-t * 45f) * 120f;
            phase += 2f * Mathf.PI * pitch / sampleRate;

            float sample = (Mathf.Sin(phase) * 0.65f + noise * 0.55f) * env * volume * 0.38f;
            left[idx] += sample * 0.95f;
            right[idx] += sample * 1.05f;
        }
    }

    private static void AddWarTom(float[] left, float[] right, float startTime, int sampleRate, float volume, float basePitch, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int hitSamples = (int)(0.35f * sampleRate);
        float phase = 0f;

        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);

        for (int s = 0; s < hitSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float env = Mathf.Exp(-t * 14f);
            float punch = Mathf.Exp(-t * 45f) * 1.2f;

            float pitch = basePitch + Mathf.Exp(-t * 30f) * (basePitch * 0.85f);
            phase += 2f * Mathf.PI * pitch / sampleRate;

            float sample = (Mathf.Sin(phase) + punch) * env * volume * 0.35f;
            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    private static void AddHeroicHorn(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);
        float phase1 = 0f;
        float phase2 = 0.25f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.12f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.15f));
            float env = attack * release;

            phase1 += 2f * Mathf.PI * freq / sampleRate;
            phase2 += 2f * Mathf.PI * (freq * 2.002f) / sampleRate;

            float horn = Mathf.Sin(phase1) * 0.65f + Mathf.Sin(phase2) * 0.35f + Mathf.Sin(phase1 * 3f) * 0.15f;
            float sample = horn * env * volume;

            left[idx] += sample * 0.55f;
            right[idx] += sample * 0.45f;
        }
    }

    private static void AddSustainedStringNote(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);

        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);

        float phase1 = 0f;
        float phase2 = 0.5f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.22f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.22f));
            float env = attack * release;

            float vibrato = Mathf.Sin(2f * Mathf.PI * 4.8f * ((float)s / sampleRate)) * 0.015f;
            float noteFreq = freq * (1f + vibrato);

            phase1 += 2f * Mathf.PI * noteFreq / sampleRate;
            phase2 += 2f * Mathf.PI * (noteFreq * 1.004f) / sampleRate;

            float wave = (Mathf.Sin(phase1) * 0.65f + Mathf.Sin(phase2) * 0.35f);
            float sample = wave * env * volume;

            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    private static void AddPluckedHarpNote(float[] left, float[] right, float startTime, float freq, int sampleRate, float volume, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(0.55f * sampleRate);

        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);

        float phase = 0f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float env = Mathf.Exp(-t * 9f);

            phase += 2f * Mathf.PI * freq / sampleRate;
            float wave = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.25f + Mathf.Sin(phase * 3f) * 0.05f;
            float sample = wave * env * volume;

            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    private static void AddFluteNote(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);
        float phase = 0f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.09f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.09f));
            float env = attack * release;

            float vibrato = (s > sampleRate * 0.15f) ? Mathf.Sin(2f * Mathf.PI * 5.2f * ((float)s / sampleRate)) * 0.018f : 0f;
            phase += 2f * Mathf.PI * (freq * (1f + vibrato)) / sampleRate;

            float flute = Mathf.Sin(phase) * 0.85f + Mathf.Sin(phase * 2f) * 0.15f;
            float sample = flute * env * volume;

            left[idx] += sample * 0.52f;
            right[idx] += sample * 0.48f;
        }
    }

    private static void AddOudPluck(float[] left, float[] right, float startTime, float freq, int sampleRate, float volume, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(0.85f * sampleRate);
        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);
        float phase1 = 0f;
        float phase2 = 0.15f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float plectrumThwack = Mathf.Exp(-t * 80f) * 1.5f;
            float stringDecay = Mathf.Exp(-t * 6.5f);

            phase1 += 2f * Mathf.PI * freq / sampleRate;
            phase2 += 2f * Mathf.PI * (freq * 1.002f) / sampleRate;

            // Warm double-coursed wooden oud string timbre
            float wave = (Mathf.Sin(phase1) * 0.65f + Mathf.Sin(phase2) * 0.35f + Mathf.Sin(phase1 * 2f) * 0.25f);
            float sample = (wave * stringDecay + plectrumThwack) * volume * 0.42f;

            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    private static void AddDarbukaHit(float[] left, float[] right, float startTime, int sampleRate, bool isDoom, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int samples = (int)((isDoom ? 0.45f : 0.22f) * sampleRate);
        float phase = 0f;

        for (int s = 0; s < samples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float env = isDoom ? Mathf.Exp(-t * 9.5f) : Mathf.Exp(-t * 28f);

            float pitch = isDoom ? (78f + Mathf.Exp(-t * 22f) * 90f) : (340f + Mathf.Exp(-t * 40f) * 180f);
            phase += 2f * Mathf.PI * pitch / sampleRate;

            float sample = Mathf.Sin(phase) * env * volume * 0.48f;
            left[idx] += sample * (isDoom ? 1f : 0.85f);
            right[idx] += sample * (isDoom ? 1f : 1.15f);
        }
    }

    private static void AddAccordionNote(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);
        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);
        float p1 = 0f, p2 = 0f, p3 = 0f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.05f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.08f));
            float env = attack * release;

            p1 += 2f * Mathf.PI * freq / sampleRate;
            p2 += 2f * Mathf.PI * (freq * 1.006f) / sampleRate; // Musette detuning beat
            p3 += 2f * Mathf.PI * (freq * 2.0f) / sampleRate;

            float reed = (Mathf.Sin(p1) * 0.5f + Mathf.Sin(p2) * 0.35f + Mathf.Sin(p3) * 0.15f);
            float sample = reed * env * volume * 0.4f;

            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    private static void AddJazzRide(float[] left, float[] right, float startTime, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int hitSamples = (int)(0.35f * sampleRate);
        var rnd = new System.Random((int)(startTime * 1000));

        for (int s = 0; s < hitSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 18f);
            float ping = Mathf.Sin(2f * Mathf.PI * 4500f * t) * Mathf.Exp(-t * 25f) * 0.3f;

            float sample = (noise + ping) * volume * 0.28f;
            left[idx] += sample * 0.4f;
            right[idx] += sample * 0.6f;
        }
    }

    private static void AddJazzBassNote(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);
        float phase = 0f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float env = Mathf.Exp(-t * 3.5f);
            float plucker = Mathf.Exp(-t * 35f) * 1.1f;

            phase += 2f * Mathf.PI * freq / sampleRate;
            float wave = Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.25f;
            float sample = (wave * env + plucker) * volume * 0.42f;

            left[idx] += sample * 0.52f;
            right[idx] += sample * 0.48f;
        }
    }

    private static void AddDistortedGuitarPowerChord(float[] left, float[] right, float startTime, float duration, float rootFreq, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);
        float pRoot = 0f, pFifth = 0f, pOct = 0f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float env = Mathf.Exp(-t * 2.8f);

            pRoot += 2f * Mathf.PI * rootFreq / sampleRate;
            pFifth += 2f * Mathf.PI * (rootFreq * 1.498f) / sampleRate;
            pOct += 2f * Mathf.PI * (rootFreq * 2.001f) / sampleRate;

            float clean = Mathf.Sin(pRoot) + Mathf.Sin(pFifth) * 0.75f + Mathf.Sin(pOct) * 0.5f;
            // Overdrive wave clipping distortion
            float dist = Mathf.Clamp(clean * 3.2f, -1.0f, 1.0f) * 0.65f;
            float sample = dist * env * volume * 0.45f;

            left[idx] += sample * 0.95f;
            right[idx] += sample * 1.05f;
        }
    }

    private static void AddPianoNote(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);
        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);
        float phase1 = 0f, phase2 = 0f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float hammer = Mathf.Exp(-t * 60f) * 1.4f;
            float decay = Mathf.Exp(-t * 2.6f);

            phase1 += 2f * Mathf.PI * freq / sampleRate;
            phase2 += 2f * Mathf.PI * (freq * 2.001f) / sampleRate;

            float wave = Mathf.Sin(phase1) * 0.7f + Mathf.Sin(phase2) * 0.3f;
            float sample = (wave * decay + hammer) * volume * 0.45f;

            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    private static void AddVikingDrone(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);
        float p1 = 0f, p2 = 0.2f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            p1 += 2f * Mathf.PI * freq / sampleRate;
            p2 += 2f * Mathf.PI * (freq * 1.502f) / sampleRate;

            float drone = (Mathf.Sin(p1) * 0.65f + Mathf.Sin(p2) * 0.35f);
            float sample = drone * volume * 0.35f;

            left[idx] += sample;
            right[idx] += sample;
        }
    }
}
