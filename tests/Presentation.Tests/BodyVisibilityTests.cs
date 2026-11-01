using PeakCraft.Player;
using UnityEngine;
using static Program;

internal static class BodyVisibilityTests
{
    public static void Run()
    {
        Test("incapacitation hides only the local scout without enabling disabled native parts", () =>
        {
            var local = Scout();
            var disabledPart = new Renderer { enabled = false };
            local.Renderers.Add(disabledPart);
            var foreign = Scout();
            var visibility = new NativeBodyVisibility();
            visibility.Update(local, true);
            Check(visibility.HiddenCount == 2, "not all local native parts were captured");
            Check(local.Renderers[0].forceRenderingOff && disabledPart.forceRenderingOff, "local scout still rendered");
            Check(!disabledPart.enabled && local.Renderers[0].enabled, "native enabled flags were modified");
            Check(!foreign.Renderers[0].forceRenderingOff, "another player's body was hidden");
        });
        Test("revival restores each renderer's original forced-off flag exactly", () =>
        {
            var scout = Scout();
            var alreadyHidden = new Renderer { forceRenderingOff = true, enabled = false };
            scout.Renderers.Add(alreadyHidden);
            var visibility = new NativeBodyVisibility();
            visibility.Update(scout, true); visibility.Release();
            Check(!scout.Renderers[0].forceRenderingOff && alreadyHidden.forceRenderingOff, "original visual flags were lost");
            Check(!alreadyHidden.enabled, "a disabled native part was enabled during restore");
            Check(visibility.HiddenCount == 0, "revival retained a renderer lease");
        });
        Test("repeated hidden frames preserve the initial renderer snapshot", () =>
        {
            var scout = Scout(); var visibility = new NativeBodyVisibility();
            for (int frame = 0; frame < 30; frame++) visibility.Update(scout, true);
            Check(visibility.HiddenCount == 1, "repeated frames captured duplicate renderer state");
            visibility.Release();
            Check(!scout.Renderers[0].forceRenderingOff, "the temporary forced-off value replaced the original snapshot");
        });
        Test("native animation cannot reveal a hidden scout during incapacitation", () =>
        {
            var scout = Scout(); var renderer = scout.Renderers[0];
            var visibility = new NativeBodyVisibility(); visibility.Update(scout, true);
            renderer.forceRenderingOff = false;
            visibility.Update(scout, true);
            Check(renderer.forceRenderingOff, "native writer exposed the incapacitated scout");
            visibility.Release();
            Check(!renderer.forceRenderingOff, "reinforcement changed the original restore value");
        });
        Test("a native renderer enabled by a follower handoff remains visually suppressed", () =>
        {
            var scout = Scout(); var renderer = scout.Renderers[0]; renderer.enabled = false;
            var visibility = new NativeBodyVisibility(); visibility.Update(scout, true);
            // Follower releases physical ownership and restores its enabled flags.
            renderer.enabled = true;
            visibility.Update(scout, true);
            Check(renderer.enabled && renderer.forceRenderingOff, "visual suppression interfered with the native handoff");
            visibility.Release();
            Check(renderer.enabled && !renderer.forceRenderingOff, "revival undid the native enabled value");
        });
        Test("replacing the native character restores the old body before hiding the new one", () =>
        {
            var oldScout = Scout(); var newScout = Scout(); var visibility = new NativeBodyVisibility();
            visibility.Update(oldScout, true); visibility.Update(newScout, true);
            Check(!oldScout.Renderers[0].forceRenderingOff, "old character stayed hidden after replacement");
            Check(newScout.Renderers[0].forceRenderingOff && visibility.HiddenCount == 1, "new character was not bound independently");
            visibility.Release();
            Check(!newScout.Renderers[0].forceRenderingOff, "new character visibility was not restored");
        });
        Test("turning Minecraft mode off during death releases the visual body lease", () =>
        {
            var scout = Scout(); var visibility = new NativeBodyVisibility();
            var minecraft = PresentationPolicy.Evaluate(Owner.Cutscene, true, true, true, true, false, false);
            visibility.Update(scout, minecraft.HideNativeBody);
            var peak = PresentationPolicy.Evaluate(Owner.Cutscene, true, false, true, true, false, false);
            visibility.Update(scout, peak.HideNativeBody);
            Check(!scout.Renderers[0].forceRenderingOff && visibility.HiddenCount == 0, "native mode inherited a Minecraft visibility lease");
        });
        Test("Java link loss during death restores the native body's original visibility", () =>
        {
            var scout = Scout(); var visibility = new NativeBodyVisibility();
            visibility.Update(scout, true);
            var disconnected = PresentationPolicy.Evaluate(Owner.PeakOwns, false, true, true, true, false, false);
            visibility.Update(scout, disconnected.HideNativeBody);
            Check(!scout.Renderers[0].forceRenderingOff && visibility.HiddenCount == 0, "link loss left the native fallback invisible");
        });
        Test("destroyed Unity renderers are skipped while surviving parts are restored", () =>
        {
            var scout = Scout(); var surviving = new Renderer(); scout.Renderers.Add(surviving);
            var visibility = new NativeBodyVisibility(); visibility.Update(scout, true);
            scout.Renderers[0].Destroyed = true;
            visibility.Update(scout, true); visibility.Release();
            Check(!surviving.forceRenderingOff && visibility.HiddenCount == 0, "surviving body parts were not restored");
        });
        Test("a destroyed native character cannot retain a surviving renderer after teardown", () =>
        {
            var scout = Scout(); var renderer = scout.Renderers[0]; var visibility = new NativeBodyVisibility();
            visibility.Update(scout, true); scout.Destroyed = true;
            visibility.Update(null, false);
            Check(!renderer.forceRenderingOff && visibility.HiddenCount == 0, "destroyed-character null semantics bypassed visual cleanup");
        });
        Test("rebinding after Unity destroys the old character releases its surviving renderers", () =>
        {
            var oldScout = Scout(); var newScout = Scout(); var visibility = new NativeBodyVisibility();
            visibility.Update(oldScout, true); oldScout.Destroyed = true;
            visibility.Update(newScout, true);
            Check(!oldScout.Renderers[0].forceRenderingOff && newScout.Renderers[0].forceRenderingOff, "destroyed old character leaked renderer ownership");
            visibility.Release();
        });
        Test("visual cleanup is safe before binding and after repeated release", () =>
        {
            var visibility = new NativeBodyVisibility(); visibility.Release(); visibility.Update(null, true);
            var scout = Scout(); visibility.Update(scout, false);
            Check(!scout.Renderers[0].forceRenderingOff && visibility.HiddenCount == 0, "an unrequested body override was created");
            visibility.Update(scout, true); visibility.Release(); visibility.Release(); visibility.Update(null, false);
            Check(!scout.Renderers[0].forceRenderingOff && visibility.HiddenCount == 0, "duplicate cleanup corrupted original visibility");
        });
        Test("an external corpse cannot acquire a visibility lease without an active local body", () =>
        {
            var corpse = new Renderer(); var visibility = new NativeBodyVisibility();
            visibility.Include(new[] { corpse });
            Check(!corpse.forceRenderingOff && visibility.HiddenCount == 0, "unbound corpse was hidden");
            var scout = Scout(); visibility.Update(scout, true); visibility.Release();
            visibility.Include(new[] { corpse });
            Check(!corpse.forceRenderingOff && visibility.HiddenCount == 0, "released body retained an external capture scope");
        });
        Test("a separately spawned corpse is hidden immediately and restores its original render flags", () =>
        {
            var scout = Scout(); var corpse = new Renderer();
            var nativeHiddenPart = new Renderer { forceRenderingOff = true, enabled = false };
            var visibility = new NativeBodyVisibility(); visibility.Update(scout, true);
            visibility.Include(new[] { corpse, nativeHiddenPart });
            Check(corpse.forceRenderingOff && nativeHiddenPart.forceRenderingOff && visibility.HiddenCount == 3,
                "newly spawned corpse leaked before the next native frame");
            Check(!nativeHiddenPart.enabled, "corpse capture changed a native enabled flag");
            visibility.Release();
            Check(!corpse.forceRenderingOff && nativeHiddenPart.forceRenderingOff && !nativeHiddenPart.enabled,
                "corpse cleanup lost its native visual state");
        });
        Test("repeated corpse scans deduplicate body parts without overwriting their original snapshots", () =>
        {
            var scout = Scout(); var corpse = new Renderer(); var visibility = new NativeBodyVisibility();
            visibility.Update(scout, true);
            for (int scan = 0; scan < 20; scan++)
                visibility.Include(new[] { corpse, scout.Renderers[0], corpse });
            Check(visibility.HiddenCount == 2, "repeated hierarchy scans duplicated a renderer lease");
            visibility.Release();
            Check(!corpse.forceRenderingOff && !scout.Renderers[0].forceRenderingOff,
                "a later forced-off snapshot replaced the original corpse or body value");
        });
        Test("corpse renderers created or detached in a later frame join the existing lease immediately", () =>
        {
            var scout = Scout(); var skeleton = new Renderer(); var latePiece = new Renderer();
            var unrelated = new Renderer(); var visibility = new NativeBodyVisibility();
            visibility.Update(scout, true); visibility.Include(new[] { skeleton });
            visibility.Update(scout, true); visibility.Include(System.Array.Empty<Renderer>());
            visibility.Include(new[] { skeleton, latePiece });
            Check(skeleton.forceRenderingOff && latePiece.forceRenderingOff && visibility.HiddenCount == 3,
                "late corpse piece escaped the active lease");
            Check(!unrelated.forceRenderingOff, "a renderer outside the selected local corpse was touched");
            visibility.Release();
            Check(!skeleton.forceRenderingOff && !latePiece.forceRenderingOff,
                "later corpse renderers were not restored with the original body");
        });
        Test("character replacement clears external corpse snapshots before the next incapacitation", () =>
        {
            var first = Scout(); var second = Scout(); var corpse = new Renderer();
            var visibility = new NativeBodyVisibility(); visibility.Update(first, true); visibility.Include(new[] { corpse });
            visibility.Update(second, true);
            Check(!first.Renderers[0].forceRenderingOff && !corpse.forceRenderingOff && visibility.HiddenCount == 1,
                "the previous character's corpse remained hidden after replacement");
            visibility.Include(new[] { corpse });
            Check(visibility.HiddenCount == 2 && corpse.forceRenderingOff, "the new lease inherited a stale deduplication entry");
            visibility.Release();
            Check(!corpse.forceRenderingOff && !second.Renderers[0].forceRenderingOff,
                "the new lease failed to restore recaptured corpse state");
        });
        Test("corpse scans tolerate destroyed parts and reassert surviving parts changed by native animation", () =>
        {
            var scout = Scout(); var destroyed = new Renderer(); var surviving = new Renderer();
            var visibility = new NativeBodyVisibility(); visibility.Update(scout, true);
            visibility.Include(new[] { destroyed, surviving }); destroyed.Destroyed = true;
            surviving.forceRenderingOff = false;
            visibility.Include(new[] { destroyed, surviving });
            Check(surviving.forceRenderingOff && visibility.HiddenCount == 3,
                "a subsequent native writer revealed the corpse or broke deduplication");
            visibility.Release();
            Check(!surviving.forceRenderingOff && visibility.HiddenCount == 0,
                "destroyed corpse parts blocked surviving-part restoration");
        });
    }

    private static Character Scout()
    {
        var character = new Character(); character.Renderers.Add(new Renderer()); return character;
    }
}
