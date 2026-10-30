using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// TeamVoiceChatManager provides real-time team voice calls & radio communication.
/// - Captures player microphone with Voice Activity Detection (VAD) to minimize bandwidth.
/// - Segregates voice streams strictly by team (Protectors only hear Protectors, Destroyers only hear Destroyers).
/// - Procedurally generates tactical walkie-talkie / radio squawks for AI bot teammates in offline/solo matches.
/// - Supports full muting of mic and team speakers with PlayerPrefs persistence.
/// </summary>
public class TeamVoiceChatManager : MonoBehaviour
{
    public static TeamVoiceChatManager Instance { get; private set; }

    public static bool IsVoiceChatEnabled { get; private set; } = false;
    public static bool HasConfiguredVoicePreference { get; private set; } = false;
    public static bool IsMicMuted { get; private set; } = false;
    public static bool IsDeafened { get; private set; } = false;

    // Voice Activity & Levels
    public static bool IsLocalSpeaking { get; private set; } = false;
    public static float LocalMicLevel { get; private set; } = 0f;
    public static string ActiveSpeakerName { get; private set; } = "";
    public static float ActiveSpeakerTimer { get; private set; } = 0f;

    // Audio Capture Settings
    private const int SampleRate = 12000; // Efficient low-latency bandwidth (12 kHz mono)
    private const float MicVolumeThreshold = 0.025f; // RMS threshold for Voice Activity Detection
    private string selectedMicDevice = null;
    private AudioClip micRecordClip;
    private int lastSamplePosition = 0;
    private float lastTransmitTime = 0f;
    private const float PacketInterval = 0.10f; // 10 packets per second when speaking

    // Teammate Audio Playback
    private AudioSource voiceAudioSource;
    private AudioSource radioSfxSource;
    private AudioClip radioChirpClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject voiceRoot = new GameObject("TeamVoiceChatSystem");
            Instance = voiceRoot.AddComponent<TeamVoiceChatManager>();
            voiceRoot.AddComponent<TeamVoiceChatUI>();
            DontDestroyOnLoad(voiceRoot);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            HasConfiguredVoicePreference = PlayerPrefs.HasKey("Beanstalk_Voice_OptIn");
            IsVoiceChatEnabled = PlayerPrefs.GetInt("Beanstalk_Voice_OptIn", 0) == 1;
            IsMicMuted = PlayerPrefs.GetInt("Beanstalk_Voice_MicMuted", 0) == 1;
            IsDeafened = PlayerPrefs.GetInt("Beanstalk_Voice_Deafened", 0) == 1;

            voiceAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.spatialBlend = 0f;
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;
            voiceAudioSource.volume = 1f;

            radioSfxSource = gameObject.AddComponent<AudioSource>();
            radioSfxSource.spatialBlend = 0f;
            radioSfxSource.playOnAwake = false;
            radioSfxSource.volume = 0.75f;

            radioChirpClip = GenerateTacticalRadioChirp();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (IsVoiceChatEnabled)
        {
            InitializeMicrophone();
        }
    }

    void Update()
    {
        // Decay active speaker indicator timer
        if (ActiveSpeakerTimer > 0f)
        {
            ActiveSpeakerTimer -= Time.deltaTime;
            if (ActiveSpeakerTimer <= 0f)
            {
                ActiveSpeakerName = "";
            }
        }

        // Process Microphone input when voice is enabled and not muted
        if (IsVoiceChatEnabled && !IsMicMuted && micRecordClip != null && Microphone.IsRecording(selectedMicDevice))
        {
            ProcessMicrophoneAudio();
        }
        else
        {
            IsLocalSpeaking = false;
            LocalMicLevel = Mathf.Lerp(LocalMicLevel, 0f, 15f * Time.deltaTime);
        }
    }

    // ==========================================
    // Microphone Capture & Voice Activity Detection
    // ==========================================

    public void InitializeMicrophone()
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("🎙️ No microphone device detected on this system.");
            return;
        }

        selectedMicDevice = Microphone.devices[0];
        try
        {
            // Record 1-second rolling circular buffer
            micRecordClip = Microphone.Start(selectedMicDevice, true, 1, SampleRate);
            lastSamplePosition = 0;
            Debug.Log($"🎙️ Microphone successfully initialized: {selectedMicDevice} @ {SampleRate}Hz");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"🎙️ Failed to start microphone recording: {ex.Message}");
        }
    }

    private void StopMicrophone()
    {
        if (!string.IsNullOrEmpty(selectedMicDevice) && Microphone.IsRecording(selectedMicDevice))
        {
            Microphone.End(selectedMicDevice);
        }
        micRecordClip = null;
        IsLocalSpeaking = false;
        LocalMicLevel = 0f;
    }

    private void ProcessMicrophoneAudio()
    {
        int currentPos = Microphone.GetPosition(selectedMicDevice);
        if (currentPos < 0 || currentPos == lastSamplePosition) return;

        int sampleCount;
        if (currentPos > lastSamplePosition)
        {
            sampleCount = currentPos - lastSamplePosition;
        }
        else
        {
            sampleCount = (SampleRate - lastSamplePosition) + currentPos;
        }

        if (sampleCount <= 0) return;

        float[] samples = new float[sampleCount];
        micRecordClip.GetData(samples, lastSamplePosition);
        lastSamplePosition = currentPos;

        // Calculate Root Mean Square (RMS) volume level
        float sumSquare = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sumSquare += samples[i] * samples[i];
        }
        float rms = Mathf.Sqrt(sumSquare / samples.Length);
        LocalMicLevel = Mathf.Clamp01(rms * 8f);

        bool isVoiceDetected = rms > MicVolumeThreshold;
        IsLocalSpeaking = isVoiceDetected;

        // Transmit packet if speaking and interval elapsed
        if (isVoiceDetected && Time.time - lastTransmitTime >= PacketInterval)
        {
            lastTransmitTime = Time.time;
            TransmitVoiceAudioPacket(samples);
        }
    }

    private void TransmitVoiceAudioPacket(float[] samples)
    {
        // 8-bit PCM Quantization (compresses float to 1 byte per sample = 12 KB/sec)
        byte[] quantized = new byte[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            float clamped = Mathf.Clamp(samples[i], -1f, 1f);
            quantized[i] = (byte)((clamped + 1f) * 127.5f);
        }

        string playerName = PlayerProfile.GetSavedPlayerName();
        if (string.IsNullOrEmpty(playerName)) playerName = "Player";

        PlayerMovement localPlayer = PlayerMovement.LocalPlayer;
        PlayerMovement.TeamType team = localPlayer != null ? localPlayer.team : PlayerMovement.TeamType.Protector;

        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworkActive && localPlayer != null)
        {
            localPlayer.SendVoiceDataServerRpc(playerName, (int)team, quantized, SampleRate);
        }
        else
        {
            // Solo Offline Mode: Local test reflection
            OnReceiveVoiceData(playerName, team, quantized, SampleRate);
        }
    }

    public void OnReceiveVoiceData(string senderName, PlayerMovement.TeamType team, byte[] audioBytes, int sampleRate)
    {
        if (!IsVoiceChatEnabled || IsDeafened) return;

        // Strict Team Privacy: Only hear teammates!
        PlayerMovement localPlayer = PlayerMovement.LocalPlayer;
        PlayerMovement.TeamType myTeam = localPlayer != null ? localPlayer.team : PlayerMovement.TeamType.Protector;
        if (team != myTeam) return;

        // Update Active Speaker HUD Indicator
        ActiveSpeakerName = senderName;
        ActiveSpeakerTimer = 1.2f;

        // Play received voice chunk if from a remote teammate
        if (!string.Equals(senderName, PlayerProfile.GetSavedPlayerName(), StringComparison.OrdinalIgnoreCase))
        {
            PlayQuantizedVoiceAudio(audioBytes, sampleRate);
        }
    }

    private void PlayQuantizedVoiceAudio(byte[] quantized, int sampleRate)
    {
        if (voiceAudioSource == null || quantized == null || quantized.Length == 0) return;

        float[] floats = new float[quantized.Length];
        for (int i = 0; i < quantized.Length; i++)
        {
            floats[i] = (quantized[i] / 127.5f) - 1f;
        }

        AudioClip chunkClip = AudioClip.Create("VoiceChunk", floats.Length, 1, sampleRate, false);
        chunkClip.SetData(floats, 0);
        voiceAudioSource.PlayOneShot(chunkClip, 1f);
    }

    // ==========================================
    // Tactical Bot Radio Squawks & Walkie-Talkie FX
    // ==========================================

    public void TriggerBotRadioTransmission(string botName)
    {
        if (!IsVoiceChatEnabled || IsDeafened) return;

        ActiveSpeakerName = $"{botName} [RADIO]";
        ActiveSpeakerTimer = 2.4f;

        if (radioSfxSource != null && radioChirpClip != null)
        {
            radioSfxSource.PlayOneShot(radioChirpClip, 0.85f);
        }
    }

    private AudioClip GenerateTacticalRadioChirp()
    {
        int rate = 22050;
        int count = Mathf.RoundToInt(rate * 0.18f); // 180ms tactical squawk
        float[] buffer = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / rate;
            // Dual-tone frequency chirp (1200Hz + 2400Hz) with static noise burst
            float tone1 = Mathf.Sin(2f * Mathf.PI * 1250f * t);
            float tone2 = Mathf.Sin(2f * Mathf.PI * 2500f * t);
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
            float env = Mathf.Sin((float)i / count * Mathf.PI);

            buffer[i] = (tone1 * 0.35f + tone2 * 0.25f + noise) * env * 0.32f;
        }

        AudioClip clip = AudioClip.Create("RadioSquawk", count, 1, rate, false);
        clip.SetData(buffer, 0);
        return clip;
    }

    // ==========================================
    // Toggles & Preferences
    // ==========================================

    public static void SetVoiceChatOption(bool enabled, bool rememberAlways = true)
    {
        IsVoiceChatEnabled = enabled;
        HasConfiguredVoicePreference = true;

        if (rememberAlways)
        {
            PlayerPrefs.SetInt("Beanstalk_Voice_OptIn", enabled ? 1 : 0);
            PlayerPrefs.SetInt("Beanstalk_Voice_Configured", 1);
            PlayerPrefs.Save();
        }

        if (Instance != null)
        {
            if (enabled)
            {
                Instance.InitializeMicrophone();
            }
            else
            {
                Instance.StopMicrophone();
            }
        }

        string msg = enabled
            ? LanguageSelectionUI.Localize(
                "🎙️ Live Team Voice Calls Enabled!",
                ar: "🎙️ تم تفعيل التواصل الصوتي المباشر للفريق!",
                fa: "🎙️ مکالمات صوتی زنده تیم فعال شد!",
                ru: "🎙️ Голосовой чат команды включен!",
                fr: "🎙️ Chat vocal d'équipe activé !",
                es: "🎙️ ¡Llamadas de voz de equipo activadas!")
            : LanguageSelectionUI.Localize(
                "💬 Text Chat Only Selected (Saving Battery & Data).",
                ar: "💬 تم اختيار الشات النصي فقط (توفير الإنترنت والبطارية).",
                fa: "💬 فقط چت متنی انتخاب شد (صرفه‌جویی در اینترنت و باتری).",
                ru: "💬 Выбран только текстовый чат (экономия данных и батареи).",
                fr: "💬 Mode texte seul sélectionné (économie de données).",
                es: "💬 Solo chat de texto seleccionado (ahorro de datos).");

        PlayerProgressionAndShopUI.Instance?.ShowNotification(msg);
    }

    public static void ToggleMicMute()
    {
        IsMicMuted = !IsMicMuted;
        PlayerPrefs.SetInt("Beanstalk_Voice_MicMuted", IsMicMuted ? 1 : 0);
        PlayerPrefs.Save();

        if (IsMicMuted && Instance != null)
        {
            IsLocalSpeaking = false;
        }

        string msg = IsMicMuted
            ? LanguageSelectionUI.Localize("🔇 Microphone Muted", ar: "🔇 تم كتم الميكروفون", fa: "🔇 میکروفون قطع شد", ru: "🔇 Микрофон выключен")
            : LanguageSelectionUI.Localize("🎙️ Microphone Active", ar: "🎙️ الميكروفون شغال الآن", fa: "🎙️ میکروفون فعال شد", ru: "🎙️ Микрофон включен");

        PlayerProgressionAndShopUI.Instance?.ShowNotification(msg);
    }

    public static void ToggleDeafen()
    {
        IsDeafened = !IsDeafened;
        PlayerPrefs.SetInt("Beanstalk_Voice_Deafened", IsDeafened ? 1 : 0);
        PlayerPrefs.Save();

        string msg = IsDeafened
            ? LanguageSelectionUI.Localize("🔈 Team Voice Audio Muted", ar: "🔈 تم كتم صوت أعضاء الفريق", fa: "🔈 صدای هم‌تیمی‌ها قطع شد", ru: "🔈 Звук команды отключен")
            : LanguageSelectionUI.Localize("🔊 Team Voice Audio Active", ar: "🔊 صوت أعضاء الفريق مفعّل الآن", fa: "🔊 صدای هم‌تیمی‌ها فعال شد", ru: "🔊 Звук команды включен");

        PlayerProgressionAndShopUI.Instance?.ShowNotification(msg);
    }

    public void NotifyMatchStarted()
    {
        // When match starts, if voice is active and we have bot teammates, test a radio chirp
        if (IsVoiceChatEnabled)
        {
            PlayerMovement localPlayer = PlayerMovement.LocalPlayer;
            PlayerMovement.TeamType team = localPlayer != null ? localPlayer.team : PlayerMovement.TeamType.Protector;
            string botName = (team == PlayerMovement.TeamType.Protector) ? "Boran Stonebark" : "Grimlok Ashfall";
            Invoke(nameof(PlayInitialRadioSquawk), 3.5f);
        }
    }

    private void PlayInitialRadioSquawk()
    {
        PlayerMovement localPlayer = PlayerMovement.LocalPlayer;
        PlayerMovement.TeamType team = localPlayer != null ? localPlayer.team : PlayerMovement.TeamType.Protector;
        string botName = (team == PlayerMovement.TeamType.Protector) ? "Althea Starbloom" : "Morrigan Cinderveil";
        TriggerBotRadioTransmission(botName);
    }
}
