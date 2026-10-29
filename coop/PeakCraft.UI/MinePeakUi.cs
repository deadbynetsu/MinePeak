using System;
using PeakCraft.Input;
using PeakCraft.Player;
using PeakCraft.World;
using UnityEngine;

namespace PeakCraft.UI;

/// <summary>Small, shared IMGUI surfaces. Textures are generated once; no screen capture or blur pass.</summary>
internal static class MinePeakUi
{
    private static readonly Color Ink = new Color(.95f, .97f, 1f, 1f);
    private static readonly Color Muted = new Color(.66f, .73f, .81f, 1f);
    private static readonly Color Accent = new Color(.53f, .86f, .93f, 1f);
    private static readonly Color Green = new Color(.55f, .86f, .68f, 1f);
    private static readonly Color Amber = new Color(1f, .77f, .43f, 1f);
    private static readonly Color Rose = new Color(.99f, .59f, .62f, 1f);
    private static Texture2D? panelTexture, insetTexture, buttonTexture, hoverTexture, selectedTexture, shadowTexture, dotTexture;
    private static Font? font;
    private static GUIStyle? panel, inset, button, selected, shadow, title, heading, small, keycap, right;
    private static int lastHealth = int.MinValue, lastFood = int.MinValue;
    private static string healthText = "100%", foodText = "100%";
    private static KeyCode lastToggleKey = KeyCode.L;
    private static string toggleKeyText = "L";
    private static readonly GUIContent MeasureContent = new GUIContent();
    private static readonly GUIContent KeyContent = new GUIContent();

    // Logical pixels keep the same layout on 720p, 1080p and ultrawide displays.
    private static float Scale => Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .65f, 2.4f);
    private static float Width => Screen.width / Scale;
    private static float Height => Screen.height / Scale;

    private readonly struct Canvas : IDisposable
    {
        private readonly Matrix4x4 matrix;
        private readonly Color color, content, background;
        private readonly bool enabled;
        private readonly int depth;
        public Canvas(int layer)
        {
            matrix = GUI.matrix; color = GUI.color; content = GUI.contentColor; background = GUI.backgroundColor;
            enabled = GUI.enabled; depth = GUI.depth;
            GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
            GUI.color = GUI.contentColor = GUI.backgroundColor = Color.white;
            GUI.depth = layer;
        }
        public void Dispose()
        {
            GUI.matrix = matrix; GUI.color = color; GUI.contentColor = content; GUI.backgroundColor = background;
            GUI.enabled = enabled; GUI.depth = depth;
        }
    }

    public static void DrawPause(Plugin plugin, bool viewer, string toggleKey, Action<bool> setMinecraft)
    {
        var gui = GUIManager.instance;
        if (Character.localCharacter == null || gui == null || !GUIManager.InPauseMenu ||
            gui.pauseMenuMainPage == null || !gui.pauseMenuMainPage.isActiveAndEnabled) return;
        Ensure();
        using (new Canvas(-1100))
        {
            const float w = 352f, h = 506f;
            float x = Width - w - 24f, y = Mathf.Max(16f, Mathf.Min(32f, (Height - h) * .14f));
            Surface(new Rect(x, y, w, h));
            Text(new Rect(x + 20, y + 18, 170, 30), "MinePeak", title!);
            Text(new Rect(x + 21, y + 49, 170, 19), "PEAK + Minecraft", small!);
            bool linked = plugin.Link.GuestAlive && (plugin.Guest.flags & 1) != 0;
            Dot(new Rect(x + 239, y + 29, 6, 6), linked ? Green : Amber);
            Text(new Rect(x + 251, y + 22, 82, 20), linked ? "Conectado" : "Preparando", small!);
            if (plugin.Vitals.HudVisible)
            {
                CacheVitals(plugin.Vitals.HealthPercent, plugin.Vitals.FoodPercent);
                Text(new Rect(x + 21, y + 73, 310, 18), "Vida " + healthText + "    Comida " + foodText, small!);
            }
            Divider(new Rect(x + 20, y + 101, w - 40, 1));
            Text(new Rect(x + 20, y + 114, 180, 20), "Seu modo de explorar", heading!);
            float toggleWidth = KeyWidth(toggleKey, 28, 112);
            Key(new Rect(x + w - 20 - toggleWidth, y + 111, toggleWidth, 23), toggleKey);
            Rect left = new Rect(x + 20, y + 143, 153, 38), other = new Rect(x + 179, y + 143, 153, 38);
            if (GUI.Button(left, "PEAK", viewer ? selected! : button!)) setMinecraft(false);
            if (GUI.Button(other, "Minecraft", viewer ? button! : selected!)) setMinecraft(true);
            Text(new Rect(x + 21, y + 190, w - 42, 20), viewer ? "Escalada e equipamentos originais." : "Blocos, inventário e sua skin.", small!);

            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && !viewer && linked;
            if (GUI.Button(new Rect(x + 20, y + 221, w - 40, 32), "Opções do Minecraft", button!))
                MinecraftSettings.Open(plugin);
            GUI.enabled = oldEnabled;

            Divider(new Rect(x + 20, y + 272, w - 40, 1));
            Text(new Rect(x + 20, y + 285, w - 40, 20), "Controles", heading!);
            Control(x + 20, y + 317, toggleKey, "Alternar modo");
            Control(x + 181, y + 317, "Mouse", "Colocar / quebrar");
            Control(x + 20, y + 355, plugin.Equipment.KeyName, "Itens PEAK · 1–4");
            Control(x + 181, y + 355, plugin.Equipment.KeyName + " + " + plugin.Equipment.ThrowKeyName, "Arremessar item");
            Control(x + 20, y + 393, plugin.Climb.KeyName, "Escalar · WASD");
            Control(x + 181, y + 393, plugin.InputBridge.InteractKeyName, "Interagir");
            Control(x + 20, y + 431, "E", "Inventário");
            Control(x + 181, y + 431, "F5", "Mudar câmera");
            Text(new Rect(x + 20, y + 479, w - 40, 16), "Segure " + FriendlyKey(plugin.Equipment.KeyName) + " ou " + FriendlyKey(plugin.Climb.KeyName) + "; Espaço solta a parede.", small!);
        }
    }

    public static void DrawGameplay(Plugin plugin)
    {
        var c = Character.localCharacter;
        if (Event.current.type != EventType.Repaint || c == null || LoadingScreenHandler.loading ||
            PeakApi.IsDead(c.data) || c.data.passedOut || c.data.fullyPassedOut || plugin.Ownership.State == Owner.Cutscene ||
            Ownership.PeakMenuOpen() || plugin.InputBridge.ScreenOpen) return;
        Ensure();
        using (new Canvas(-1000))
        {
            bool stats = plugin.Vitals.HudVisible;
            bool linked = plugin.Link.GuestAlive && (plugin.Guest.flags & 1) != 0;
            string mode = plugin.Climb.WantsNative ? "Escalada PEAK" : plugin.Equipment.WantsNative ? "Itens PEAK" :
                plugin.CoopMode.IsViewer ? "PEAK" : !linked ? "PEAK · aguardando" : plugin.Ownership.State == Owner.Handoff ? "Conectando…" : "Minecraft";
            var toggle = plugin.CoopConfig.CoopToggleKey.Value;
            if (toggle != lastToggleKey) { lastToggleKey = toggle; toggleKeyText = FriendlyKey(toggle.ToString()); }
            float toggleWidth = KeyWidth(toggleKeyText, 25, 112);
            // A custom modifier can be wider than L. Grow to the left instead of cutting
            // either the key name or the current mode; the right margin stays anchored.
            float w = Mathf.Max(218f, 27f + Mathf.Ceil(TextWidth(mode, heading!)) + 12f + toggleWidth + 12f);
            float x = Width - w - 22f, y = 20f;
            Surface(new Rect(x, y, w, stats ? 83f : 40f), false);
            Dot(new Rect(x + 13, y + 17, 6, 6), linked ? Accent : Amber);
            Text(new Rect(x + 27, y + 8, w - toggleWidth - 49f, 24), mode, heading!);
            Key(new Rect(x + w - 12f - toggleWidth, y + 8, toggleWidth, 23), toggleKeyText);
            if (stats)
            {
                CacheVitals(plugin.Vitals.HealthPercent, plugin.Vitals.FoodPercent);
                Divider(new Rect(x + 13, y + 39, w - 26, 1));
                float statWidth = (w - 38f) * .5f;
                Stat(new Rect(x + 13, y + 46, statWidth, 26), "Vida", healthText, plugin.Vitals.HealthPercent, Rose);
                Stat(new Rect(x + 25f + statWidth, y + 46, statWidth, 26), "Comida", foodText, plugin.Vitals.FoodPercent, Amber);
            }
            string? context = plugin.Climb.WantsNative ? "WASD · mover    Espaço · soltar" :
                plugin.Equipment.WantsNative ? "1–4 · item    " + FriendlyKey(plugin.Equipment.ThrowKeyName) + " · segure / solte" : null;
            if (context != null)
            {
                float top = y + (stats ? 91f : 48f);
                Surface(new Rect(x, top, w, 30f), false);
                Text(new Rect(x + 12, top + 5, w - 24, 20), context, small!);
            }
        }
    }

    public static void DrawInteraction(string key, string text, bool hold, float progress)
    {
        if (Event.current.type != EventType.Repaint) return;
        Ensure();
        using (new Canvas(-1000))
        {
            const float w = 350f;
            float h = hold ? 63f : 50f, x = (Width - w) * .5f, y = Height - 220f;
            Surface(new Rect(x, y, w, h), false);
            float keyWidth = KeyWidth(key, 30, 112), textX = x + 24f + keyWidth;
            Key(new Rect(x + 12, y + 12, keyWidth, 26), key);
            Text(new Rect(textX, y + 8, w - keyWidth - 38f, 21), text, heading!);
            Text(new Rect(textX, y + 28, w - keyWidth - 38f, 17), hold ? "Segure para interagir" : "Interagir", small!);
            if (hold) Bar(new Rect(x + 13, y + h - 8, w - 26, 2), progress, Accent);
        }
    }

    private static void Control(float x, float y, string key, string action)
    {
        Key(new Rect(x, y, KeyWidth(key, 26, 148), 21f), key);
        Text(new Rect(x, y + 22, 150, 17), action, small!);
    }

    private static void Stat(Rect area, string name, string value, int percent, Color tint)
    {
        Text(new Rect(area.x, area.y, area.width, 18), name, small!);
        Text(new Rect(area.x, area.y, area.width, 18), value, right!);
        Bar(new Rect(area.x, area.y + 23, area.width, 2), percent * .01f, tint);
    }

    private static void CacheVitals(int health, int food)
    {
        if (lastHealth != health) { lastHealth = health; healthText = health + "%"; }
        if (lastFood != food) { lastFood = food; foodText = food + "%"; }
    }
    private static void Text(Rect rect, string value, GUIStyle style) => GUI.Label(rect, value, style);
    private static void Key(Rect rect, string value)
    {
        KeyContent.text = FriendlyKey(value); KeyContent.tooltip = value;
        GUI.Label(rect, KeyContent, keycap!);
    }
    private static float TextWidth(string value, GUIStyle style)
    {
        MeasureContent.text = value;
        return style.CalcSize(MeasureContent).x;
    }
    private static float KeyWidth(string value, float min, float max) => Mathf.Clamp(TextWidth(FriendlyKey(value), keycap!) + 16f, min, max);
    private static string FriendlyKey(string value)
    {
        int separator = value.IndexOf(" + ", StringComparison.Ordinal);
        if (separator >= 0) return FriendlyKey(value.Substring(0, separator)) + " + " + FriendlyKey(value.Substring(separator + 3));
        switch (value)
        {
            case "LeftControl": case "LeftCtrl": return "Ctrl esq.";
            case "RightControl": case "RightCtrl": return "Ctrl dir.";
            case "LeftShift": return "Shift esq.";
            case "RightShift": return "Shift dir.";
            case "LeftAlt": return "Alt esq.";
            case "RightAlt": return "Alt dir.";
            case "LeftWindows": case "LeftMeta": return "Win esq.";
            case "RightWindows": case "RightMeta": return "Win dir.";
            case "LeftCommand": case "LeftApple": return "Cmd esq.";
            case "RightCommand": case "RightApple": return "Cmd dir.";
            case "Return": case "Enter": return "Enter";
            case "Escape": return "Esc";
            case "Backspace": return "Back";
            case "Delete": return "Del";
            case "Insert": return "Ins";
            case "PageUp": return "PgUp";
            case "PageDown": return "PgDn";
            case "CapsLock": return "Caps";
            case "PrintScreen": return "PrtSc";
            case "LeftArrow": return "←";
            case "RightArrow": return "→";
            case "UpArrow": return "↑";
            case "DownArrow": return "↓";
            case "Space": return "Espaço";
            case "None": return "—";
        }
        string? number = value.StartsWith("Keypad", StringComparison.Ordinal) ? value.Substring(6) :
            value.StartsWith("Numpad", StringComparison.Ordinal) ? value.Substring(6) : null;
        if (number != null)
        {
            switch (number)
            {
                case "Plus": number = "+"; break;
                case "Minus": number = "−"; break;
                case "Multiply": number = "×"; break;
                case "Divide": number = "/"; break;
                case "Period": case "Decimal": number = "."; break;
                case "Equals": number = "="; break;
            }
            return "Num " + number;
        }
        return value;
    }
    private static void Divider(Rect rect) => Tint(rect, new Color(1f, 1f, 1f, .09f));
    private static void Bar(Rect rect, float value, Color color)
    {
        Tint(rect, new Color(1f, 1f, 1f, .13f));
        if (value > 0f) Tint(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), color);
    }
    private static void Tint(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
    }
    private static void Dot(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color; GUI.DrawTexture(rect, dotTexture!); GUI.color = old;
    }
    private static void Surface(Rect rect, bool deep = true)
    {
        if (Event.current.type != EventType.Repaint) return;
        shadow!.Draw(new Rect(rect.x - 9, rect.y - 6, rect.width + 18, rect.height + 21), GUIContent.none, false, false, false, false);
        (deep ? panel! : inset!).Draw(rect, GUIContent.none, false, false, false, false);
    }

    private static void Ensure()
    {
        if (panel != null) return;
        try { font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 16); } catch { font = null; }
        panelTexture = MakeSurface(new Color(.075f, .10f, .14f, .94f), new Color(.88f, .94f, 1f, .19f));
        insetTexture = MakeSurface(new Color(.055f, .077f, .108f, .78f), new Color(.88f, .94f, 1f, .16f));
        buttonTexture = MakeSurface(new Color(.18f, .23f, .29f, .5f), new Color(.88f, .94f, 1f, .13f), 11);
        hoverTexture = MakeSurface(new Color(.25f, .34f, .40f, .75f), new Color(.88f, .96f, 1f, .36f), 11);
        selectedTexture = MakeSurface(new Color(.20f, .43f, .49f, .88f), new Color(.63f, .94f, 1f, .47f), 11);
        shadowTexture = MakeShadow();
        dotTexture = MakeSurface(Color.white, Color.white, 31);
        panel = Background(panelTexture, 18); inset = Background(insetTexture, 18); shadow = Background(shadowTexture, 30);
        button = Style(13, Ink, TextAnchor.MiddleCenter, true);
        button.border = new RectOffset(12, 12, 12, 12);
        button.normal.background = buttonTexture; button.hover.background = hoverTexture;
        button.active.background = selectedTexture; button.focused.background = hoverTexture;
        selected = new GUIStyle(button); selected.normal.background = selectedTexture;
        title = Style(24, Ink, TextAnchor.MiddleLeft, true);
        heading = Style(13, Ink, TextAnchor.MiddleLeft, true);
        small = Style(11, Muted, TextAnchor.MiddleLeft);
        right = Style(11, Ink, TextAnchor.MiddleRight, true);
        keycap = Style(10, Ink, TextAnchor.MiddleCenter, true);
        keycap.normal.background = buttonTexture; keycap.border = new RectOffset(11, 11, 11, 11);
    }
    private static GUIStyle Style(int size, Color color, TextAnchor alignment, bool bold = false)
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = alignment, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            richText = false, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(0, 0, 0, 0) };
        if (font != null) style.font = font;
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
        return style;
    }
    private static GUIStyle Background(Texture2D texture, int border) => new GUIStyle { normal = { background = texture }, border = new RectOffset(border, border, border, border) };

    private static Texture2D MakeSurface(Color fill, Color stroke, float radius = 17f)
    {
        const int size = 64;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Abs(x + .5f - 32f) - (32f - radius), dy = Mathf.Abs(y + .5f - 32f) - (32f - radius);
            float d = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0)) + Mathf.Min(Mathf.Max(dx, dy), 0) - radius;
            float edge = Mathf.Clamp01(.5f - d);
            Color color = Color.Lerp(fill, stroke, Mathf.Clamp01(d + 1.5f));
            float sheen = Mathf.Pow(y / 63f, 3f) * .055f;
            color.r += sheen; color.g += sheen; color.b += sheen; color.a *= edge;
            pixels[y * size + x] = color;
        }
        return Texture(size, pixels);
    }
    private static Texture2D MakeShadow()
    {
        const int size = 96;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Abs(x + .5f - 48f) - 20f, dy = Mathf.Abs(y + .5f - 48f) - 20f;
            float d = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0)) + Mathf.Min(Mathf.Max(dx, dy), 0) - 17f;
            float outside = Mathf.Max(d, 0);
            pixels[y * size + x] = new Color(0f, .012f, .025f, .32f * Mathf.Exp(-outside * outside / 60f));
        }
        return Texture(size, pixels);
    }
    private static Texture2D Texture(int size, Color32[] pixels)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels); texture.Apply(false, true);
        return texture;
    }

    public static void Dispose()
    {
        foreach (var texture in new[] { panelTexture, insetTexture, buttonTexture, hoverTexture, selectedTexture, shadowTexture, dotTexture })
            if (texture != null) UnityEngine.Object.Destroy(texture);
        if (font != null) UnityEngine.Object.Destroy(font);
        panelTexture = insetTexture = buttonTexture = hoverTexture = selectedTexture = shadowTexture = dotTexture = null;
        panel = inset = button = selected = shadow = title = heading = small = keycap = right = null;
        font = null; lastHealth = lastFood = int.MinValue;
    }
}
