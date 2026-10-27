using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeakCraft.Coop;

// Local extension of PeakCraft Co-op: reversible handoff without closing Minecraft.
internal sealed class CoopMode
{
    public const string Minecraft = "Minecraft", Viewer = "Viewer";
    private readonly CoopConfig config;
    private float lastToggleAt = -1f;
    public bool IsViewer { get; private set; }
    public string PropValue => IsViewer ? "viewer" : "mc";
    public bool IsToggleKey(Key candidate) => Enum.TryParse(config.CoopToggleKey.Value.ToString(), true, out Key key) && key != Key.None && key == candidate;
    public static string RuntimeDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PeakCraftCoop");

    public CoopMode(CoopConfig config)
    {
        this.config = config;
        IsViewer = !string.Equals(config.CoopMode.Value, Minecraft, StringComparison.OrdinalIgnoreCase);
        // The upstream one-way toggle uses this sentinel to terminate Java. This local mode keeps it running.
        string legacyFlag = Path.Combine(RuntimeDir, "viewer.flag");
        if (File.Exists(legacyFlag)) File.Delete(legacyFlag);
        Plugin.Log.LogInfo("local mode: " + (IsViewer ? "PEAK native" : "Minecraft"));
    }

    public void Update()
    {
        if (Application.isFocused && Keyboard.current != null &&
            Enum.TryParse(config.CoopToggleKey.Value.ToString(), true, out Key key) &&
            key != Key.None && Keyboard.current[key].wasPressedThisFrame)
            ToggleFromKey();
    }

    private void ToggleFromKey()
    {
        if (Plugin.Instance.InputBridge.ScreenOpen || Time.unscaledTime - lastToggleAt < 0.25f) return;
        lastToggleAt = Time.unscaledTime;
        SetMinecraft(IsViewer);
    }

    public void SetMinecraft(bool enabled)
    {
        if (enabled == !IsViewer) return;
        Plugin p = Plugin.Instance;
        p.InputBridge.ReleaseAll();
        IsViewer = !enabled;
        config.CoopMode.Value = enabled ? Minecraft : Viewer;
        p.Config.Save();
        if (!enabled) {
            p.Follower.Release();
            p.Ownership.Update(false, p.Guest);
        } else {
            p.Collision.Reset("live mode enabled");
            p.Ownership.RequestTeleport();
        }
        Plugin.Log.LogInfo(enabled ? "local mode: Minecraft ON; safe handoff pending" : "local mode: PEAK native restored");
    }

    public void OnGUI()
    {
        // IMGUI receives short synthetic key taps too; debounce with the Input System path.
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == config.CoopToggleKey.Value) {
            ToggleFromKey();
            Event.current.Use();
        }
        if (Character.localCharacter == null || GUIManager.instance == null || !GUIManager.InPauseMenu) return;
        int depth = GUI.depth;
        GUI.depth = -1000;
        float x = Mathf.Max(12, Screen.width - 345);
        GUI.Box(new Rect(x, 28, 325, 146), "PeakCraft Co-op");
        if (GUI.Button(new Rect(x + 12, 60, 301, 36), IsViewer ? "ATIVAR modo Minecraft" : "DESATIVAR modo Minecraft"))
            SetMinecraft(IsViewer);
        GUI.Label(new Rect(x + 12, 102, 301, 25), config.CoopToggleKey.Value + " alterna sem reiniciar");
        GUI.Label(new Rect(x + 12, 130, 301, 30), IsViewer ? "PEAK normal: itens e escalada" : "Minecraft: inventario e blocos");
        GUI.depth = depth;
    }
}
