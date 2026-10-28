using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EnchantedNatureGUI - Premium Ancient Civilization & Magical Nature Procedural UI Engine:
/// Renders high-end, AAA-quality PC fantasy game interfaces procedurally:
/// - Translucent Obsidian-Emerald Glass Panels
/// - Multi-layer Beveled Antique Gold Borders & Corner Filigree
/// - Procedural Heraldic Mode Crests (Crossed Blades, Solar Corona, Alliance Aegis)
/// - Smooth Hover Easing & Micro-animation Transitions
/// - High-contrast Typography with Drop Shadows for 10-Language Support
/// </summary>
public static class EnchantedNatureGUI
{
    private static Texture2D panelGradientTex;
    private static Texture2D portalCardTex;
    private static Texture2D buttonGradientTex;
    private static Texture2D goldTrimTex;
    private static Texture2D crestGlowTex;
    private static Texture2D whitePixelTex;

    // Smooth hover interpolation dictionary (tracks per-element hover weight 0.0 -> 1.0)
    private static readonly Dictionary<int, float> hoverStates = new Dictionary<int, float>();

    // Color Palette Constants - Premium Ancient Civilization & Magical Nature
    public static readonly Color ObsidianDark       = new Color(0.025f, 0.05f, 0.038f, 0.96f);
    public static readonly Color ObsidianMedium     = new Color(0.05f, 0.095f, 0.07f, 0.95f);
    public static readonly Color ObsidianLight      = new Color(0.08f, 0.15f, 0.11f, 0.92f);

    public static readonly Color AntiqueGoldBright   = new Color(1.0f, 0.88f, 0.42f, 1.0f);
    public static readonly Color AntiqueGoldMid      = new Color(0.82f, 0.68f, 0.28f, 1.0f);
    public static readonly Color AntiqueGoldDark     = new Color(0.42f, 0.32f, 0.12f, 1.0f);
    public static readonly Color AntiqueGoldShadow   = new Color(0.18f, 0.14f, 0.05f, 0.85f);

    public static readonly Color EmeraldAccent       = new Color(0.24f, 0.88f, 0.48f, 1.0f);
    public static readonly Color AmberSolarAccent    = new Color(1.0f, 0.76f, 0.22f, 1.0f);
    public static readonly Color BronzeSanctuary     = new Color(0.92f, 0.60f, 0.32f, 1.0f);
    public static readonly Color CyanRuneAccent      = new Color(0.30f, 0.85f, 1.0f, 1.0f);

    // =========================================================================
    // DYNAMIC GAME FONT ENGINE (DecoType Thuluth for Arabic/Farsi, Aref Ruqaa for Latin)
    // =========================================================================
    private static Font cachedArabicFont;
    private static Font cachedLatinFont;

    public static Font GetActiveGameFont()
    {
        string lang = LanguageSelectionUI.CurrentLanguageCode;
        bool isArabicScript = (lang == "ar" || lang == "fa");

        if (isArabicScript)
        {
            if (cachedArabicFont == null)
            {
                cachedArabicFont = Resources.Load<Font>("Fonts/DecoTypeThuluth");
                if (cachedArabicFont == null)
                {
                    try
                    {
                        cachedArabicFont = Font.CreateDynamicFontFromOSFont(new string[] { "DecoType Thuluth", "Amiri", "Traditional Arabic", "Arial" }, 16);
                    }
                    catch {}
                }
            }
            return cachedArabicFont;
        }
        else
        {
            // Latin-script languages (English, German, French, Spanish, Italian, Portuguese, Turkish, etc.)
            bool isLatin = (lang == "en" || lang == "de" || lang == "fr" || lang == "es" || lang == "it" || lang == "pt" || lang == "tr");
            if (isLatin)
            {
                if (cachedLatinFont == null)
                {
                    cachedLatinFont = Resources.Load<Font>("Fonts/ArefRuqaa-Bold");
                    if (cachedLatinFont == null)
                    {
                        try
                        {
                            cachedLatinFont = Font.CreateDynamicFontFromOSFont(new string[] { "Aref Ruqaa", "Georgia", "Arial" }, 16);
                        }
                        catch {}
                    }
                }
                return cachedLatinFont;
            }

            // For other scripts (Cyrillic ru, CJK zh/ja, Devanagari hi), let Unity use default fallback font
            return null;
        }
    }

    static EnchantedNatureGUI()
    {
        GenerateTextures();
    }

    private static void GenerateTextures()
    {
        if (whitePixelTex == null)
        {
            whitePixelTex = Texture2D.whiteTexture;
        }

        // 1. Multi-stop Obsidian-Emerald Panel Gradient (128px vertical)
        panelGradientTex = new Texture2D(1, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++)
        {
            float t = y / 127f;
            // Subtle specular top glow, deep obsidian body, soft vignette bottom
            Color c;
            if (t > 0.85f)
            {
                float topT = (t - 0.85f) / 0.15f;
                c = Color.Lerp(ObsidianMedium, new Color(0.09f, 0.18f, 0.13f, 0.98f), topT);
            }
            else if (t < 0.2f)
            {
                float btmT = t / 0.2f;
                c = Color.Lerp(new Color(0.015f, 0.03f, 0.02f, 0.98f), ObsidianDark, btmT);
            }
            else
            {
                c = ObsidianDark;
            }
            panelGradientTex.SetPixel(0, y, c);
        }
        panelGradientTex.wrapMode = TextureWrapMode.Clamp;
        panelGradientTex.Apply();

        // 2. Vertical Portal Monolith Card Gradient (128px)
        portalCardTex = new Texture2D(1, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++)
        {
            float t = y / 127f;
            Color c = Color.Lerp(
                new Color(0.03f, 0.055f, 0.04f, 0.97f),
                new Color(0.07f, 0.13f, 0.095f, 0.97f),
                Mathf.SmoothStep(0f, 1f, t)
            );
            portalCardTex.SetPixel(0, y, c);
        }
        portalCardTex.wrapMode = TextureWrapMode.Clamp;
        portalCardTex.Apply();

        // 3. Crisp Button Bevel Gradient (48px)
        buttonGradientTex = new Texture2D(1, 48, TextureFormat.RGBA32, false);
        for (int y = 0; y < 48; y++)
        {
            float t = y / 47f;
            float lum = Mathf.Lerp(0.65f, 1.0f, Mathf.Pow(t, 0.8f));
            buttonGradientTex.SetPixel(0, y, new Color(lum, lum, lum, 1f));
        }
        buttonGradientTex.wrapMode = TextureWrapMode.Clamp;
        buttonGradientTex.Apply();

        // 4. Antique Gold Metallic Trim (32px)
        goldTrimTex = new Texture2D(1, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            float t = y / 31f;
            Color g = Color.Lerp(AntiqueGoldDark, AntiqueGoldBright, Mathf.PingPong(t * 2f, 1f));
            goldTrimTex.SetPixel(0, y, g);
        }
        goldTrimTex.wrapMode = TextureWrapMode.Clamp;
        goldTrimTex.Apply();

        // 5. Soft Radial Glow Disc for Heraldic Crests (64x64)
        crestGlowTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(31.5f, 31.5f);
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / 32f;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 2.2f);
                crestGlowTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        crestGlowTex.wrapMode = TextureWrapMode.Clamp;
        crestGlowTex.Apply();
    }

    /// <summary>
    /// Smoothly computes the hover factor (0.0 to 1.0) for a given element ID over real time.
    /// </summary>
    public static float GetSmoothHover(int elementId, bool isHovered, float speed = 8.5f)
    {
        float current = 0f;
        if (hoverStates.TryGetValue(elementId, out var val))
        {
            current = val;
        }

        float target = isHovered ? 1f : 0f;
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) dt = 0.016f; // Safe fallback if paused

        float next = Mathf.MoveTowards(current, target, dt * speed);
        hoverStates[elementId] = next;
        return next;
    }

    // =========================================================================
    // MULTI-LAYER BEVELED ANTIQUE GOLD BORDER & FILIGREE ENGINE
    // =========================================================================

    /// <summary>
    /// Draws a complete AAA beveled antique gold frame with outer drop shadow,
    /// 3D beveled rims (top/left bright, bottom/right shaded), hairline inner inlay,
    /// and 4 ornamental corner brackets with diamond studs.
    /// </summary>
    public static void DrawAntiqueGoldFrame(Rect rect, Color accent, float hover = 0f)
    {
        if (panelGradientTex == null) GenerateTextures();

        // 1. Soft layered drop shadows
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + 6f, rect.width, rect.height), whitePixelTex);
        GUI.color = new Color(0f, 0f, 0f, 0.25f);
        GUI.DrawTexture(new Rect(rect.x + 12f, rect.y + 12f, rect.width, rect.height), whitePixelTex);

        // 2. Obsidian glass background with vertical gradient
        GUI.color = Color.white;
        GUI.DrawTexture(rect, panelGradientTex, ScaleMode.StretchToFill);

        // 3. Ambient magical tint overlay (reacts to accent color and hover)
        Color tint = Color.Lerp(
            new Color(accent.r * 0.12f, accent.g * 0.15f, accent.b * 0.12f, 0.65f),
            new Color(accent.r * 0.22f, accent.g * 0.28f, accent.b * 0.22f, 0.85f),
            hover
        );
        GUI.color = tint;
        GUI.DrawTexture(rect, whitePixelTex);

        // 4. Subtle top specular sheen (first 30% of card)
        GUI.color = new Color(1f, 1f, 1f, 0.04f + 0.06f * hover);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height * 0.32f), whitePixelTex);

        // 5. Beveled 3D Outer Rim
        // Top and Left: Antique Gold Highlight
        Color topRim = Color.Lerp(AntiqueGoldBright, Color.white, hover * 0.4f);
        GUI.color = topRim;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), whitePixelTex);

        // Bottom and Right: Antique Gold Shadow
        Color btmRim = Color.Lerp(AntiqueGoldDark, AntiqueGoldMid, hover * 0.5f);
        GUI.color = btmRim;
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x + rect.width - 2f, rect.y, 2f, rect.height), whitePixelTex);

        // 6. Hairline Inner Gold Trim (inset 4px)
        Color innerRim = Color.Lerp(
            new Color(AntiqueGoldMid.r, AntiqueGoldMid.g, AntiqueGoldMid.b, 0.35f),
            new Color(AntiqueGoldBright.r, AntiqueGoldBright.g, AntiqueGoldBright.b, 0.75f),
            hover
        );
        Rect inner = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
        DrawCrispBorder(inner, innerRim, 1f);

        // 7. Corner Ornamental Filigrees & Diamond Studs
        DrawCornerFiligree(rect, Color.Lerp(AntiqueGoldMid, AntiqueGoldBright, hover), 14f);

        // 8. Top Center Notch Jewel
        DrawTopCenterNotch(rect, accent, hover);

        GUI.color = Color.white;
    }

    /// <summary>
    /// Draws 4 corner L-bracket filigrees with center diamond rivets.
    /// </summary>
    public static void DrawCornerFiligree(Rect rect, Color color, float size = 14f)
    {
        GUI.color = color;
        float t = 2.5f;

        // Top-Left Bracket
        GUI.DrawTexture(new Rect(rect.x, rect.y, size, t), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, t, size), whitePixelTex);
        // Top-Left Diamond Stud
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, 4f, 4f), whitePixelTex);

        // Top-Right Bracket
        GUI.DrawTexture(new Rect(rect.x + rect.width - size, rect.y, size, t), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x + rect.width - t, rect.y, t, size), whitePixelTex);
        // Top-Right Diamond Stud
        GUI.DrawTexture(new Rect(rect.x + rect.width - 7f, rect.y + 3f, 4f, 4f), whitePixelTex);

        // Bottom-Left Bracket
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - t, size, t), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - size, t, size), whitePixelTex);
        // Bottom-Left Diamond Stud
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + rect.height - 7f, 4f, 4f), whitePixelTex);

        // Bottom-Right Bracket
        GUI.DrawTexture(new Rect(rect.x + rect.width - size, rect.y + rect.height - t, size, t), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x + rect.width - t, rect.y + rect.height - size, t, size), whitePixelTex);
        // Bottom-Right Diamond Stud
        GUI.DrawTexture(new Rect(rect.x + rect.width - 7f, rect.y + rect.height - 7f, 4f, 4f), whitePixelTex);

        GUI.color = Color.white;
    }

    private static void DrawTopCenterNotch(Rect rect, Color accent, float hover)
    {
        float notchW = 28f;
        float notchH = 5f;
        float cx = rect.x + (rect.width - notchW) / 2f;

        // Gold notch bracket
        GUI.color = Color.Lerp(AntiqueGoldMid, AntiqueGoldBright, hover);
        GUI.DrawTexture(new Rect(cx, rect.y, notchW, notchH), whitePixelTex);

        // Center jewel dot
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
        GUI.color = Color.Lerp(accent, Color.white, hover * 0.5f) * pulse;
        GUI.DrawTexture(new Rect(rect.x + rect.width / 2f - 3f, rect.y + 1f, 6f, 4f), whitePixelTex);
        GUI.color = Color.white;
    }

    // =========================================================================
    // PROCEDURAL HERALDIC CRESTS (MODE EMBLEMS)
    // =========================================================================

    /// <summary>
    /// Renders high-definition procedural heraldic crests:
    /// 0 = Solo Expedition (Crossed Blades of the Grove & Glowing Leaf)
    /// 1 = Realm War (Solar Corona Crown & Radiant Core Crystal)
    /// 2 = Tribe Sanctuary (Alliance Aegis Shield & Knotted Ivy)
    /// </summary>
    public static void DrawHeraldicCrest(Rect centerRect, int crestType, Color accent, float hover)
    {
        if (crestGlowTex == null) GenerateTextures();

        float cx = centerRect.x + centerRect.width / 2f;
        float cy = centerRect.y + centerRect.height / 2f;
        float r = Mathf.Min(centerRect.width, centerRect.height) * 0.44f;

        // 1. Ambient soft radial halo behind the crest
        float haloSize = r * 2.8f;
        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 2.8f + crestType);
        Color haloColor = new Color(accent.r, accent.g, accent.b, (0.22f + 0.25f * hover) * pulse);
        GUI.color = haloColor;
        GUI.DrawTexture(new Rect(cx - haloSize / 2f, cy - haloSize / 2f, haloSize, haloSize), crestGlowTex);

        // 2. Stone/Obsidian Backing Medallion
        GUI.color = new Color(0.04f, 0.08f, 0.06f, 0.95f);
        GUI.DrawTexture(new Rect(cx - r, cy - r, r * 2f, r * 2f), whitePixelTex);

        // 3. Ornate Outer Ring (Double Gold Rim)
        Color rimCol = Color.Lerp(AntiqueGoldMid, AntiqueGoldBright, hover);
        DrawCrispBorder(new Rect(cx - r, cy - r, r * 2f, r * 2f), rimCol, 2f);
        DrawCrispBorder(new Rect(cx - r + 3f, cy - r + 3f, r * 2f - 6f, r * 2f - 6f), new Color(rimCol.r, rimCol.g, rimCol.b, 0.4f), 1f);

        // 4. Crest Symbol Primitives
        GUI.color = Color.white;
        switch (crestType)
        {
            case 0: // SOLO EXPEDITION: Crossed Blades & Emerald Heart Leaf
                DrawCrossedBladesEmblem(cx, cy, r * 0.72f, accent, hover);
                break;

            case 1: // REALM WAR: Imperial Solar Crown & Pulsing Diamond Core
                DrawSolarCrownEmblem(cx, cy, r * 0.75f, accent, hover);
                break;

            case 2: // TRIBE SANCTUARY: Alliance Aegis & Entwined Rings
                DrawAllianceShieldEmblem(cx, cy, r * 0.72f, accent, hover);
                break;
        }

        GUI.color = Color.white;
    }

    private static void DrawCrossedBladesEmblem(float cx, float cy, float radius, Color accent, float hover)
    {
        // Crossed Blades (drawn with angled bar rects)
        Color steelCol = Color.Lerp(new Color(0.85f, 0.92f, 0.95f), AntiqueGoldBright, hover * 0.35f);
        GUI.color = steelCol;

        float bladeLen = radius * 1.8f;
        float bladeW = 3.5f;

        // Diagonal blade 1 (\)
        for (int i = -14; i <= 14; i++)
        {
            float offset = i * (bladeLen / 30f);
            GUI.DrawTexture(new Rect(cx + offset - bladeW / 2f, cy + offset - bladeW / 2f, bladeW, bladeW), whitePixelTex);
        }

        // Diagonal blade 2 (/)
        for (int i = -14; i <= 14; i++)
        {
            float offset = i * (bladeLen / 30f);
            GUI.DrawTexture(new Rect(cx + offset - bladeW / 2f, cy - offset - bladeW / 2f, bladeW, bladeW), whitePixelTex);
        }

        // Crossguard bars
        GUI.color = AntiqueGoldBright;
        GUI.DrawTexture(new Rect(cx - 14f, cy - 14f, 8f, 3f), whitePixelTex);
        GUI.DrawTexture(new Rect(cx + 6f, cy - 14f, 8f, 3f), whitePixelTex);
        GUI.DrawTexture(new Rect(cx - 14f, cy + 11f, 8f, 3f), whitePixelTex);
        GUI.DrawTexture(new Rect(cx + 6f, cy + 11f, 8f, 3f), whitePixelTex);

        // Center Glowing Emerald Leaf/Diamond
        Color coreCol = Color.Lerp(accent, Color.white, hover * 0.5f);
        GUI.color = coreCol;
        GUI.DrawTexture(new Rect(cx - 7f, cy - 7f, 14f, 14f), whitePixelTex);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cx - 4f, cy - 4f, 8f, 8f), whitePixelTex);
    }

    private static void DrawSolarCrownEmblem(float cx, float cy, float radius, Color accent, float hover)
    {
        // 5 Spiked Imperial Corona Crown
        Color gold = Color.Lerp(AntiqueGoldBright, Color.white, hover * 0.45f);
        GUI.color = gold;

        float baseY = cy + radius * 0.35f;
        float crownW = radius * 1.5f;

        // Base bar
        GUI.DrawTexture(new Rect(cx - crownW / 2f, baseY, crownW, 4f), whitePixelTex);

        // 5 Corona Spikes
        float[] spikeHeights = new float[] { 14f, 24f, 32f, 24f, 14f };
        for (int i = 0; i < 5; i++)
        {
            float sx = cx - crownW / 2f + (i * (crownW / 4f));
            float h = spikeHeights[i];
            GUI.DrawTexture(new Rect(sx - 2f, baseY - h, 4f, h), whitePixelTex);
            // Spike top gem
            GUI.color = accent;
            GUI.DrawTexture(new Rect(sx - 3.5f, baseY - h - 5f, 7f, 5f), whitePixelTex);
            GUI.color = gold;
        }

        // Central Radiant Crystal Diamond
        float pulse = 0.9f + 0.2f * Mathf.Sin(Time.unscaledTime * 4.5f);
        Color crystalCol = Color.Lerp(new Color(0.35f, 0.92f, 1f), Color.white, hover * 0.6f) * pulse;
        GUI.color = crystalCol;
        GUI.DrawTexture(new Rect(cx - 8f, cy - 4f, 16f, 16f), whitePixelTex);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cx - 4f, cy, 8f, 8f), whitePixelTex);
    }

    private static void DrawAllianceShieldEmblem(float cx, float cy, float radius, Color accent, float hover)
    {
        // Heraldic Aegis Shield Outline
        Color shieldCol = Color.Lerp(BronzeSanctuary, AntiqueGoldBright, hover * 0.5f);
        GUI.color = shieldCol;

        float sw = radius * 1.4f;
        float sh = radius * 1.6f;
        Rect sRect = new Rect(cx - sw / 2f, cy - sh / 2f, sw, sh);

        // Shield body
        GUI.color = new Color(0.08f, 0.12f, 0.10f, 0.95f);
        GUI.DrawTexture(sRect, whitePixelTex);

        // Shield border
        DrawCrispBorder(sRect, shieldCol, 2f);

        // Interlocking Alliance Knotwork / Concentric Rings
        GUI.color = Color.Lerp(AntiqueGoldBright, Color.white, hover * 0.5f);
        float ringR = radius * 0.42f;
        DrawCrispBorder(new Rect(cx - ringR - 6f, cy - ringR, ringR * 2f, ringR * 2f), shieldCol, 2f);
        DrawCrispBorder(new Rect(cx - ringR + 6f, cy - ringR, ringR * 2f, ringR * 2f), AntiqueGoldBright, 2f);

        // Heart of Friendship Core
        GUI.color = accent;
        GUI.DrawTexture(new Rect(cx - 5f, cy - 5f, 10f, 10f), whitePixelTex);
    }

    // =========================================================================
    // TEXT WITH SHADOW (CRISP AAA LEGIBILITY ACROSS ALL LANGUAGES)
    // =========================================================================

    public static void DrawTextWithShadow(Rect rect, string text, GUIStyle style, Color textColor, Color shadowColor, float offsetX = 1.5f, float offsetY = 1.5f)
    {
        if (string.IsNullOrEmpty(text)) return;

        Font activeFont = GetActiveGameFont();
        if (activeFont != null)
        {
            style.font = activeFont;
        }

        Color oldCol = style.normal.textColor;

        // Shadow pass
        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(rect.x + offsetX, rect.y + offsetY, rect.width, rect.height), text, style);

        // Main text pass
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);

        style.normal.textColor = oldCol;
    }

    // =========================================================================
    // PROCEDURAL AAA BUTTONS
    // =========================================================================

    /// <summary>
    /// Renders a responsive, beveled AAA fantasy button with smooth hover transition,
    /// metallic sheen, and golden typography.
    /// </summary>
    public static bool DrawAncientGoldButton(Rect rect, string text, Color accent, int fontSize = 13, bool isMajor = false)
    {
        if (buttonGradientTex == null) GenerateTextures();

        int id = (int)(rect.x * 1000f + rect.y);
        bool isHover = rect.Contains(Event.current.mousePosition);
        float hover = GetSmoothHover(id, isHover, 10f);

        // 1. Drop shadow
        GUI.color = new Color(0f, 0f, 0f, 0.5f + 0.15f * hover);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), whitePixelTex);

        // 2. Button Fill
        Color baseFill = isMajor
            ? Color.Lerp(new Color(0.22f, 0.16f, 0.05f), new Color(0.38f, 0.28f, 0.10f), hover)
            : Color.Lerp(new Color(0.06f, 0.12f, 0.09f), new Color(0.12f, 0.22f, 0.16f), hover);
        GUI.color = baseFill;
        GUI.DrawTexture(rect, buttonGradientTex, ScaleMode.StretchToFill);

        // 3. Top Sheen Specular Highlight
        GUI.color = new Color(1f, 1f, 1f, 0.06f + 0.14f * hover);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, rect.height * 0.42f), whitePixelTex);

        // 4. Beveled Outer Border
        Color rimCol = Color.Lerp(
            new Color(AntiqueGoldMid.r, AntiqueGoldMid.g, AntiqueGoldMid.b, 0.85f),
            new Color(AntiqueGoldBright.r, AntiqueGoldBright.g, AntiqueGoldBright.b, 1.0f),
            hover
        );
        DrawCrispBorder(rect, rimCol, isHover ? 2f : 1.5f);

        // 5. Button Label
        GUIStyle btnStyle = new GUIStyle(GUI.skin.label);
        btnStyle.fontSize = fontSize;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.alignment = TextAnchor.MiddleCenter;

        Font activeFont = GetActiveGameFont();
        if (activeFont != null)
        {
            btnStyle.font = activeFont;
        }

        Color textCol = isHover
            ? new Color(1.0f, 0.98f, 0.85f)
            : Color.Lerp(AntiqueGoldBright, Color.white, 0.5f);
        Color textShadow = new Color(0.02f, 0.04f, 0.03f, 0.9f);

        DrawTextWithShadow(rect, text, btnStyle, textCol, textShadow, 1f, 1f);

        // Invisible click trigger
        Color old = GUI.color;
        GUI.color = Color.clear;
        bool clicked = GUI.Button(rect, "");
        GUI.color = old;

        return clicked;
    }

    /// <summary>
    /// Draws an ornate flourish divider line with center diamond.
    /// </summary>
    public static void DrawFlourishDivider(Rect rect, Color color)
    {
        float cy = rect.y + rect.height / 2f;
        float cx = rect.x + rect.width / 2f;

        // Left tapered line
        GUI.color = new Color(color.r, color.g, color.b, 0.45f);
        GUI.DrawTexture(new Rect(rect.x + 12f, cy - 0.5f, rect.width / 2f - 24f, 1f), whitePixelTex);

        // Right tapered line
        GUI.DrawTexture(new Rect(cx + 12f, cy - 0.5f, rect.width / 2f - 24f, 1f), whitePixelTex);

        // Center diamond
        GUI.color = color;
        GUI.DrawTexture(new Rect(cx - 3.5f, cy - 3.5f, 7f, 7f), whitePixelTex);
        GUI.color = Color.white;
    }

    // =========================================================================
    // LEGACY COMPATIBILITY METHODS (Upgraded with new Art Direction)
    // =========================================================================

    public static void DrawOrganicScroll(Rect rect, Color tint)
    {
        DrawAntiqueGoldFrame(rect, tint, 0f);
    }

    public static void DrawButterflyCrest(Rect rect, Color tint)
    {
        DrawLeafPlaque(rect, ObsidianMedium, AntiqueGoldBright);
    }

    public static bool DrawOrganicLeafButton(Rect rect, string text, Color accent, int fontSize = 12)
    {
        return DrawAncientGoldButton(rect, text, accent, fontSize, false);
    }

    public static void DrawLeafPlaque(Rect rect, Color bg, Color border)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), whitePixelTex);

        GUI.color = new Color(bg.r, bg.g, bg.b, Mathf.Max(0.92f, bg.a));
        GUI.DrawTexture(rect, whitePixelTex);

        GUI.color = new Color(border.r, border.g, border.b, 0.08f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, Mathf.Min(28f, rect.height * 0.3f)), whitePixelTex);

        DrawCrispBorder(rect, border, 1.5f);
        DrawCornerFiligree(rect, border, 10f);
        GUI.color = Color.white;
    }

    public static void DrawWoodPlate(Rect rect, Color bg, Color border)
    {
        GUI.color = bg;
        GUI.DrawTexture(rect, whitePixelTex, ScaleMode.StretchToFill);

        if (border.a > 0.05f)
        {
            DrawCrispBorder(rect, border, 1.5f);
        }
        GUI.color = Color.white;
    }

    public static bool DrawWoodButton(Rect rect, string text, int fontSize = 13)
    {
        return DrawAncientGoldButton(rect, text, AntiqueGoldBright, fontSize, true);
    }

    public static void DrawCrispBorder(Rect rect, Color color, float thickness)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), whitePixelTex);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), whitePixelTex);
        GUI.color = Color.white;
    }
}
