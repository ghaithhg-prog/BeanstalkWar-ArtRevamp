using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FreeFireCombatAudio:
/// High-fidelity, visceral procedural audio engine inspired by AAA titles (Free Fire, Wild Rift).
/// Features:
/// 1. Attack swings (air-cleaving blade whooshes, staff sweeps).
/// 2. Hit impacts (wood chopping for the Sacred Tree, flesh/blade impacts for players, metallic clang for bosses).
/// 3. Dynamic footstep audio with pitch jitter for walking and sprinting.
/// 4. Soft, elegant two-tone notification chimes for team chat and tactical pings.
/// 5. Majestic AAA Game Start sting when transitioning from Title Screen into the Grove.
/// 6. Subtle, atmospheric title screen suspense drone.
/// All synthesized procedurally with 0ms disk latency and zero asset file dependencies.
/// </summary>
public class FreeFireCombatAudio : MonoBehaviour
{
    public static FreeFireCombatAudio Instance { get; private set; }

    public enum HitMaterial
    {
        Wood,   // Sacred Beanstalk Tree
        Flesh,  // Players & Bots
        Armor   // Level Bosses
    }

    private AudioSource sfxSource2D;
    private AudioSource ambientSource;
    private AudioSource footstepSource;

    // Cached Procedural AudioClips
    private static AudioClip clipSwingLight;
    private static AudioClip clipSwingHeavy;
    private static AudioClip clipHitWood;
    private static AudioClip clipHitFlesh;
    private static AudioClip clipHitArmor;
    private static AudioClip clipFootstep;
    private static AudioClip clipChatNotif;
    private static AudioClip clipTacticalPing;
    private static AudioClip clipGameStartSting;
    private static AudioClip clipTitleAmbient;

    private static bool clipsGenerated = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("FreeFireCombatAudio");
            go.AddComponent<FreeFireCombatAudio>();
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

        sfxSource2D = gameObject.AddComponent<AudioSource>();
        sfxSource2D.playOnAwake = false;
        sfxSource2D.spatialBlend = 0f;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.spatialBlend = 0f;
        ambientSource.loop = true;

        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;

        GenerateClips();
    }

    // =========================================================================
    // PROCEDURAL AUDIO SYNTHESIZERS
    // =========================================================================

    private static void GenerateClips()
    {
        if (clipsGenerated) return;

        clipSwingLight = CreateWhooshClip("Attack_Swing_Light", 0.16f, 2200f, 320f);
        clipSwingHeavy = CreateWhooshClip("Attack_Swing_Heavy", 0.24f, 1800f, 180f);

        clipHitWood = CreateWoodChopClip("Hit_Wood_Tree", 0.22f);
        clipHitFlesh = CreateFleshImpactClip("Hit_Flesh_Player", 0.19f);
        clipHitArmor = CreateArmorClangClip("Hit_Armor_Boss", 0.28f);

        clipFootstep = CreateFootstepClip("Footstep_GrassDirt", 0.09f);

        clipChatNotif = CreateChatNotifClip("Chat_Message_Ding", 0.26f);
        clipTacticalPing = CreateTacticalPingClip("Tactical_Radio_Ping", 0.14f);

        clipGameStartSting = CreateGameStartStingClip("Title_Game_Start_Sting", 2.2f);
        clipTitleAmbient = CreateTitleAmbientDroneClip("Title_Ambient_Drone", 4.0f);

        clipsGenerated = true;
    }

    /// <summary>
    /// Visceral air-cutting blade whoosh.
    /// </summary>
    private static AudioClip CreateWhooshClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t, 0.7f));
            float phase = 2f * Mathf.PI * freq * ((float)i / sampleRate);

            // Sine core with low-passed noise modulation
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
            float raw = Mathf.Sin(phase) * 0.55f + noise;

            // Punchy parabolic envelope: quick rise, crisp sustain, natural tail
            float env = Mathf.Sin(t * Mathf.PI);
            env = Mathf.Pow(env, 1.4f);

            samples[i] = raw * env * 0.75f;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Resonant wood chopping impact + splinter crack for hitting the Great Beanstalk.
    /// </summary>
    private static AudioClip CreateWoodChopClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float tSec = (float)i / sampleRate;

            // 1. Sharp initial fracture transient (1400Hz exponential decay)
            float crack = Mathf.Sin(2f * Mathf.PI * 1400f * tSec) * Mathf.Exp(-tSec * 60f);

            // 2. Deep solid trunk body resonance (110Hz + 220Hz harmonic)
            float body = (Mathf.Sin(2f * Mathf.PI * 110f * tSec) * 0.7f + Mathf.Sin(2f * Mathf.PI * 220f * tSec) * 0.3f) 
                         * Mathf.Exp(-tSec * 16f);

            // 3. Splinter noise burst
            float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-tSec * 35f) * 0.35f;

            float val = (crack * 0.5f + body * 0.85f + noise) * Mathf.Clamp01((1f - t) * 1.5f);
            samples[i] = Mathf.Clamp(val * 0.88f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Visceral punch / blade slice flesh impact with low-end thump.
    /// </summary>
    private static AudioClip CreateFleshImpactClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float tSec = (float)i / sampleRate;

            // High frequency slap/snap transient
            float snap = Mathf.Sin(2f * Mathf.PI * 820f * tSec) * Mathf.Exp(-tSec * 75f);
            // Low visceral gut-punch body (75Hz -> 50Hz pitch drop)
            float subPitch = Mathf.Lerp(85f, 48f, t);
            float punch = Mathf.Sin(2f * Mathf.PI * subPitch * tSec) * Mathf.Exp(-tSec * 18f);

            // Organic flesh crunch noise
            float crunch = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-tSec * 45f) * 0.4f;

            float val = (snap * 0.45f + punch * 0.9f + crunch * 0.3f) * (1f - t);
            samples[i] = Mathf.Clamp(val * 0.92f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Metallic clang & stone crunch for striking Boss armor.
    /// </summary>
    private static AudioClip CreateArmorClangClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float tSec = (float)i / sampleRate;

            // Inharmonic metallic ring (1250Hz, 1820Hz, 2410Hz)
            float ring = (Mathf.Sin(2f * Mathf.PI * 1250f * tSec) * 0.45f +
                          Mathf.Sin(2f * Mathf.PI * 1820f * tSec) * 0.35f +
                          Mathf.Sin(2f * Mathf.PI * 2410f * tSec) * 0.20f) * Mathf.Exp(-tSec * 14f);

            // Heavy anvil sub-thump
            float sub = Mathf.Sin(2f * Mathf.PI * 95f * tSec) * Mathf.Exp(-tSec * 22f);

            float val = (ring * 0.7f + sub * 0.75f) * (1f - t);
            samples[i] = Mathf.Clamp(val * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Crisp, subtle footstep thud on grass/soil.
    /// </summary>
    private static AudioClip CreateFootstepClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float tSec = (float)i / sampleRate;

            // Low ground tap (110Hz drop)
            float tap = Mathf.Sin(2f * Mathf.PI * 115f * tSec) * Mathf.Exp(-tSec * 45f);
            // Soft foliage rustle
            float rustle = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-tSec * 35f) * 0.28f;

            samples[i] = (tap * 0.7f + rustle * 0.3f) * (1f - t) * 0.65f;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Soft, high-end modern game message pop chime (gentle D5 -> A5 with exponential decay).
    /// Calibrated to be very subtle and pleasant.
    /// </summary>
    private static AudioClip CreateChatNotifClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float tSec = (float)i / sampleRate;

            // Two-tone bell chime: D5 (587Hz) for first 0.05s, blending into A5 (880Hz)
            float note1 = Mathf.Sin(2f * Mathf.PI * 587.33f * tSec) * Mathf.Exp(-tSec * 16f);
            float note2 = (tSec > 0.035f) ? Mathf.Sin(2f * Mathf.PI * 880.00f * (tSec - 0.035f)) * Mathf.Exp(-(tSec - 0.035f) * 12f) : 0f;

            // Soft warm harmonic
            float overtone = Mathf.Sin(2f * Mathf.PI * 1760f * tSec) * Mathf.Exp(-tSec * 25f) * 0.15f;

            float val = (note1 * 0.5f + note2 * 0.75f + overtone) * (1f - t);
            samples[i] = Mathf.Clamp(val * 0.45f, -1f, 1f); // Gentle, subtle level
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Crisp tactical radio confirmation ping.
    /// </summary>
    private static AudioClip CreateTacticalPingClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float tSec = (float)i / sampleRate;

            float f = (t < 0.45f) ? 1240f : 1680f;
            float ping = Mathf.Sin(2f * Mathf.PI * f * tSec) * Mathf.Exp(-tSec * 22f);
            samples[i] = ping * (1f - t) * 0.42f;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// AAA Game Title Start Sting: Deep sub-boom + ascending golden chord + brass swell.
    /// Leaves a glorious, powerful impression when clicking to start the game!
    /// </summary>
    private static AudioClip CreateGameStartStingClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float tSec = (float)i / sampleRate;

            // 1. Deep Sub-Impact Boom (55Hz sub drop with punchy attack)
            float sub = Mathf.Sin(2f * Mathf.PI * 55f * tSec) * Mathf.Exp(-tSec * 2.5f);

            // 2. Ascending Golden Chords: C4 (261.6Hz), E4 (329.6Hz), G4 (392Hz), C5 (523.2Hz)
            float c4 = Mathf.Sin(2f * Mathf.PI * 261.63f * tSec) * Mathf.Exp(-tSec * 1.6f);
            float e4 = Mathf.Sin(2f * Mathf.PI * 329.63f * tSec) * Mathf.Exp(-tSec * 1.5f);
            float g4 = Mathf.Sin(2f * Mathf.PI * 392.00f * tSec) * Mathf.Exp(-tSec * 1.4f);
            float c5 = Mathf.Sin(2f * Mathf.PI * 523.25f * tSec) * Mathf.Exp(-tSec * 1.2f);
            float chord = (c4 + e4 + g4 + c5) * 0.25f;

            // 3. Shimmering celestial brass overtone
            float shimmer = Mathf.Sin(2f * Mathf.PI * 1046.5f * tSec) * Mathf.Exp(-tSec * 2.2f) * 0.15f;

            float env = (tSec < 0.08f) ? (tSec / 0.08f) : Mathf.Clamp01(1f - t * 0.7f);
            float val = (sub * 0.85f + chord * 0.75f + shimmer) * env;

            samples[i] = Mathf.Clamp(val * 0.95f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Faint, mystical low ambient drone (very quiet) for the title screen.
    /// </summary>
    private static AudioClip CreateTitleAmbientDroneClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float tSec = (float)i / sampleRate;

            // Low pulsing drone: 65Hz & 97.5Hz (perfect fifth) with gentle breathing swell
            float lfo = (Mathf.Sin(t * 2f * Mathf.PI) + 1f) * 0.5f;
            float drone = (Mathf.Sin(2f * Mathf.PI * 65.41f * tSec) * 0.6f +
                           Mathf.Sin(2f * Mathf.PI * 98.00f * tSec) * 0.4f) * (0.6f + 0.4f * lfo);

            samples[i] = drone * 0.08f; // Kept extremely faint
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // =========================================================================
    // PUBLIC PLAYBACK API
    // =========================================================================

    public static void PlayAttackSwing(Vector3 pos, bool isHeavy = false)
    {
        if (Instance == null || Instance.sfxSource2D == null) return;
        GenerateClips();
        AudioClip clip = isHeavy ? clipSwingHeavy : clipSwingLight;
        Instance.sfxSource2D.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
        Instance.sfxSource2D.PlayOneShot(clip, 0.42f);
    }

    public static void PlayHitImpact(Vector3 pos, HitMaterial material)
    {
        if (Instance == null || Instance.sfxSource2D == null) return;
        GenerateClips();

        AudioClip clip;
        float vol;
        switch (material)
        {
            case HitMaterial.Wood:
                clip = clipHitWood;
                vol = 0.68f;
                break;
            case HitMaterial.Armor:
                clip = clipHitArmor;
                vol = 0.65f;
                break;
            case HitMaterial.Flesh:
            default:
                clip = clipHitFlesh;
                vol = 0.58f;
                break;
        }

        Instance.sfxSource2D.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
        Instance.sfxSource2D.PlayOneShot(clip, vol);
    }

    public static void PlayFootstep(Vector3 pos, bool isSprint = false)
    {
        if (Instance == null || Instance.footstepSource == null) return;
        GenerateClips();

        Instance.footstepSource.pitch = UnityEngine.Random.Range(0.88f, 1.14f);
        float vol = isSprint ? 0.38f : 0.24f;
        Instance.footstepSource.PlayOneShot(clipFootstep, vol);
    }

    public static void PlayChatNotification()
    {
        if (Instance == null || Instance.sfxSource2D == null) return;
        GenerateClips();

        Instance.sfxSource2D.pitch = 1.0f;
        Instance.sfxSource2D.PlayOneShot(clipChatNotif, 0.22f); // Subtle, comfortable volume
    }

    public static void PlayTacticalPing()
    {
        if (Instance == null || Instance.sfxSource2D == null) return;
        GenerateClips();

        Instance.sfxSource2D.pitch = UnityEngine.Random.Range(0.98f, 1.03f);
        Instance.sfxSource2D.PlayOneShot(clipTacticalPing, 0.35f);
    }

    public static void PlayGameStartSting()
    {
        if (Instance == null || Instance.sfxSource2D == null) return;
        GenerateClips();

        Instance.sfxSource2D.pitch = 1.0f;
        Instance.sfxSource2D.PlayOneShot(clipGameStartSting, 0.95f);
    }

    public static void StartTitleAmbient()
    {
        if (Instance == null || Instance.ambientSource == null) return;
        GenerateClips();

        if (clipTitleAmbient != null && !Instance.ambientSource.isPlaying)
        {
            Instance.ambientSource.clip = clipTitleAmbient;
            Instance.ambientSource.volume = 0.12f;
            Instance.ambientSource.Play();
        }
    }

    public static void StopTitleAmbient()
    {
        if (Instance == null || Instance.ambientSource == null) return;
        if (Instance.ambientSource.isPlaying)
        {
            Instance.ambientSource.Stop();
        }
    }
}
