using System.Collections.Generic;
using UnityEngine;

namespace PeakCraft.Player;

/// <summary>Local presentation only. Never takes or freezes native physics.</summary>
internal sealed class NativeBodyVisibility
{
    private Character? character;
    private readonly List<(Renderer Renderer, bool WasForcedOff)> renderers = new();
    private readonly HashSet<Renderer> seen = new();
    private Character? corpseOwner;
    private readonly List<Renderer> corpseRenderers = new();
    private readonly HashSet<Renderer> corpseSeen = new();

    public int HiddenCount => renderers.Count;
    public int CorpseRendererCount => corpseRenderers.Count;
    public int CorpseRegistrations { get; private set; }

    public void Update(Character? current, bool hide)
    {
        if (!hide || current == null) { Release(); return; }
        if (character != null && character != current) Release();
        if (character == null)
        {
            // Destroyed Unity characters can compare equal to null. Restore any
            // surviving renderers before binding the next native character.
            Release();
            character = current;
            Include(current.GetComponentsInChildren<Renderer>(true));
            if (corpseOwner == current) Include(corpseRenderers);
            Plugin.Log.LogInfo($"presentation: hidden local native body during incapacitation ({renderers.Count} renderers)");
        }
        foreach (var entry in renderers)
            if (entry.Renderer != null && !entry.Renderer.forceRenderingOff) entry.Renderer.forceRenderingOff = true;
    }

    // Native death spawns the local skeleton outside the character hierarchy.
    // Register those renderers without touching their revival/collision objects.
    public void Include(Renderer[] extra) => Include((IEnumerable<Renderer>)extra);

    private void Include(IEnumerable<Renderer> extra)
    {
        if (character == null) return;
        foreach (Renderer renderer in extra)
        {
            if (renderer == null) continue;
            if (seen.Add(renderer)) renderers.Add((renderer, renderer.forceRenderingOff));
            if (!renderer.forceRenderingOff) renderer.forceRenderingOff = true;
        }
    }

    // Called by native corpse creation hooks, including while in PEAK mode.
    // Cache once; toggling Minecraft can then reuse it without searching scenes.
    public void RegisterCorpse(Character owner, Renderer[] extra)
    {
        if (owner == null) return;
        if (corpseOwner != owner)
        {
            corpseRenderers.Clear();
            corpseSeen.Clear();
            corpseOwner = owner;
        }
        for (int i = corpseRenderers.Count - 1; i >= 0; i--)
            if (corpseRenderers[i] == null)
            {
                corpseSeen.Remove(corpseRenderers[i]);
                corpseRenderers.RemoveAt(i);
            }
        bool added = false;
        foreach (var renderer in extra)
            if (renderer != null && corpseSeen.Add(renderer))
            {
                corpseRenderers.Add(renderer);
                added = true;
            }
        if (added) CorpseRegistrations++;
        if (character == owner) Include(extra);
    }

    public void Reset()
    {
        Release();
        corpseOwner = null;
        corpseRenderers.Clear();
        corpseSeen.Clear();
        CorpseRegistrations = 0;
    }

    public void Release()
    {
        bool changed = renderers.Count != 0;
        foreach (var entry in renderers)
            if (entry.Renderer != null) entry.Renderer.forceRenderingOff = entry.WasForcedOff;
        renderers.Clear();
        seen.Clear();
        character = null;
        if (changed) Plugin.Log.LogInfo("presentation: restored local native body visibility");
    }
}
