using PeakCraft.Player;
using UnityEngine;
using static Program;

internal static class CorpseCacheTests
{
    public static void Run()
    {
        Test("registering a corpse in native mode caches ownership without changing its visibility", () =>
        {
            var scout = Scout(); var corpse = new Renderer(); var visibility = new NativeBodyVisibility();
            visibility.RegisterCorpse(scout, new[] { corpse });
            Check(!corpse.forceRenderingOff && !scout.Renderers[0].forceRenderingOff && visibility.HiddenCount == 0,
                "registration in native mode hid a native visual");
            Check(visibility.CorpseRendererCount == 1 && visibility.CorpseRegistrations == 1,
                "the native creation event did not cache its corpse");
            visibility.Update(scout, true);
            Check(corpse.forceRenderingOff && visibility.HiddenCount == 2,
                "switching to Minecraft failed to use the cached corpse");
            visibility.Release();
        });
        Test("mode changes reuse cached corpse renderers without replaying a creation event", () =>
        {
            var scout = Scout(); var corpse = new Renderer(); var visibility = new NativeBodyVisibility();
            visibility.RegisterCorpse(scout, new[] { corpse }); visibility.Update(scout, true);
            visibility.Release();
            Check(!corpse.forceRenderingOff && visibility.HiddenCount == 0 && visibility.CorpseRendererCount == 1,
                "native mode failed to restore visuals while retaining its ownership cache");
            int registrations = visibility.CorpseRegistrations;
            for (int frame = 0; frame < 20; frame++) visibility.Update(scout, false);
            visibility.Update(scout, true);
            Check(corpse.forceRenderingOff && visibility.HiddenCount == 2 && visibility.CorpseRegistrations == registrations,
                "returning to Minecraft required another corpse creation event");
            visibility.Release();
            Check(!corpse.forceRenderingOff, "the second native transition lost the corpse's original visibility");
        });
        Test("duplicate corpse registration does not grow its cache or registration counter", () =>
        {
            var scout = Scout(); var corpse = new Renderer(); var visibility = new NativeBodyVisibility();
            visibility.Update(scout, true);
            for (int notification = 0; notification < 30; notification++)
                visibility.RegisterCorpse(scout, new[] { corpse, corpse });
            Check(visibility.CorpseRendererCount == 1 && visibility.CorpseRegistrations == 1 && visibility.HiddenCount == 2,
                "repeated native notifications duplicated cached or hidden renderers");
            visibility.Release();
            Check(!corpse.forceRenderingOff, "duplicate notifications replaced the original renderer snapshot");
        });
        Test("a corpse created while the matching body is hidden joins its lease immediately", () =>
        {
            var scout = Scout(); var corpse = new Renderer();
            var originallyHidden = new Renderer { forceRenderingOff = true, enabled = false };
            var visibility = new NativeBodyVisibility(); visibility.Update(scout, true);
            visibility.RegisterCorpse(scout, new[] { corpse, originallyHidden });
            Check(corpse.forceRenderingOff && originallyHidden.forceRenderingOff && visibility.HiddenCount == 3,
                "a new corpse remained visible until another frame");
            visibility.Release();
            Check(!corpse.forceRenderingOff && originallyHidden.forceRenderingOff && !originallyHidden.enabled,
                "registered corpse cleanup did not preserve the original renderer states");
        });
        Test("another owner's cached corpse cannot be hidden through the currently bound character", () =>
        {
            var local = Scout(); var other = Scout(); var corpse = new Renderer();
            var visibility = new NativeBodyVisibility(); visibility.Update(local, true);
            visibility.RegisterCorpse(other, new[] { corpse });
            visibility.Update(local, true);
            Check(!corpse.forceRenderingOff && visibility.HiddenCount == 1,
                "a corpse owned by another character entered the active body's lease");
            visibility.Release(); visibility.Update(local, true);
            Check(!corpse.forceRenderingOff && visibility.HiddenCount == 1,
                "rebinding the original character ignored the corpse's cached owner");
            visibility.Update(other, true);
            Check(corpse.forceRenderingOff && !local.Renderers[0].forceRenderingOff && visibility.HiddenCount == 2,
                "changing to the actual corpse owner did not bind its own visuals");
            visibility.Release();
        });
        Test("a scene reset restores active corpses and clears all ownership metadata", () =>
        {
            var scout = Scout(); var corpse = new Renderer(); var visibility = new NativeBodyVisibility();
            visibility.RegisterCorpse(scout, new[] { corpse }); visibility.Update(scout, true);
            visibility.Reset();
            Check(!corpse.forceRenderingOff && !scout.Renderers[0].forceRenderingOff && visibility.HiddenCount == 0,
                "scene teardown left a visual override active");
            Check(visibility.CorpseRendererCount == 0 && visibility.CorpseRegistrations == 0,
                "scene teardown retained stale corpse metadata");
            visibility.Update(scout, true);
            Check(!corpse.forceRenderingOff && visibility.HiddenCount == 1,
                "a new scene reused the previous scene's corpse cache");
            visibility.RegisterCorpse(scout, new[] { corpse });
            Check(corpse.forceRenderingOff && visibility.CorpseRegistrations == 1,
                "a fresh scene could not register its first corpse");
            visibility.Reset(); visibility.Reset();
        });
        Test("registering a later corpse prunes destroyed cached renderers before reuse", () =>
        {
            var scout = Scout(); var destroyed = new Renderer(); var surviving = new Renderer(); var later = new Renderer();
            var visibility = new NativeBodyVisibility(); visibility.RegisterCorpse(scout, new[] { destroyed, surviving });
            destroyed.Destroyed = true;
            visibility.RegisterCorpse(scout, new[] { destroyed, surviving, later });
            Check(visibility.CorpseRendererCount == 2 && visibility.CorpseRegistrations == 2,
                "destroyed corpse references remained in the cache or blocked a later registration");
            visibility.Update(scout, true);
            Check(surviving.forceRenderingOff && later.forceRenderingOff && visibility.HiddenCount == 3,
                "the pruned cache did not include exactly the surviving corpse parts");
            visibility.Reset();
            Check(!surviving.forceRenderingOff && !later.forceRenderingOff,
                "a destroyed cached renderer prevented surviving-part restoration");
        });
        Test("missing or destroyed owners cannot replace valid cached ownership", () =>
        {
            var scout = Scout(); var corpse = new Renderer(); var foreign = new Renderer();
            var destroyedOwner = Scout(); destroyedOwner.Destroyed = true;
            var visibility = new NativeBodyVisibility(); visibility.RegisterCorpse(scout, new[] { corpse });
            visibility.RegisterCorpse(null!, new[] { foreign });
            visibility.RegisterCorpse(destroyedOwner, new[] { foreign });
            Check(visibility.CorpseRendererCount == 1 && visibility.CorpseRegistrations == 1,
                "an invalid native owner replaced a valid creation record");
            visibility.Update(scout, true);
            Check(corpse.forceRenderingOff && !foreign.forceRenderingOff,
                "an invalid owner's renderer was captured through another character");
            visibility.Reset();
        });
        Test("steady hidden frames reuse the captured hierarchy without redundant renderer writes", () =>
        {
            var scout = Scout(); var body = scout.Renderers[0]; var corpse = new Renderer();
            var visibility = new NativeBodyVisibility(); visibility.RegisterCorpse(scout, new[] { corpse });
            visibility.Update(scout, true);
            int bodyWrites = body.ForceWrites, corpseWrites = corpse.ForceWrites, hierarchyReads = scout.HierarchyReads;
            int bodyReads = body.ForceReads, corpseReads = corpse.ForceReads;
            for (int frame = 0; frame < 60; frame++) visibility.Update(scout, true);
            Check(body.ForceWrites == bodyWrites && corpse.ForceWrites == corpseWrites,
                "unchanged renderers were rewritten on every hidden frame");
            Check(scout.HierarchyReads == hierarchyReads && visibility.CorpseRegistrations == 1,
                "steady frames rescanned the hierarchy or replayed corpse registrations");
            Check(body.ForceReads > bodyReads && corpse.ForceReads > corpseReads,
                "steady frames stopped checking whether a native writer exposed a renderer");
            visibility.Reset();
        });
        Test("a native visibility change is repaired with one write and then becomes quiet again", () =>
        {
            var scout = Scout(); var corpse = new Renderer(); var visibility = new NativeBodyVisibility();
            visibility.RegisterCorpse(scout, new[] { corpse }); visibility.Update(scout, true);
            corpse.forceRenderingOff = false;
            int beforeRepair = corpse.ForceWrites;
            visibility.Update(scout, true);
            Check(corpse.forceRenderingOff && corpse.ForceWrites == beforeRepair + 1,
                "native animation was not repaired with exactly one required renderer write");
            int afterRepair = corpse.ForceWrites;
            for (int frame = 0; frame < 20; frame++) visibility.Update(scout, true);
            Check(corpse.ForceWrites == afterRepair, "the repaired renderer kept receiving redundant writes");
            visibility.Reset();
            Check(!corpse.forceRenderingOff, "the optimization lost the original native visibility value");
        });
    }

    private static Character Scout()
    {
        var scout = new Character(); scout.Renderers.Add(new Renderer()); return scout;
    }
}
