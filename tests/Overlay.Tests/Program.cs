using System;
using PeakCraft.Link;
using PeakCraft.Render;
using UnityEngine;

internal static class Program
{
    private static int count;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Case(string name, Action<PeakLink, Overlay> test)
    {
        Texture2D.Reset(); GUI.Reset();
        using var link = new PeakLink();
        var overlay = new Overlay(link);
        try { test(link, overlay); }
        finally { overlay.Clear(); }
        count++; Console.WriteLine("PASS: " + name);
    }
    private static void Draw(Overlay overlay, bool cursor = false) => overlay.Draw(cursor, new Vector2(12, 20));
    private static void Fresh(PeakLink link, Overlay overlay, uint width = 4, uint height = 4, uint flags = 0)
    {
        link.Queue(width, height, flags); overlay.Update();
    }
    public static int Main()
    {
        try
        {
            Case("cold overlay has no stale HUD or texture allocation", (link, overlay) => {
                overlay.Update(); Draw(overlay, true);
                Check(!overlay.HasFrame && Texture2D.CreatedCount == 0 && GUI.OverlayDraws == 0 && GUI.CursorDraws == 0, "Cold overlay drew or allocated");
            });
            Case("fresh RGBA capture uploads once and becomes drawable", (link, overlay) => {
                Fresh(link, overlay); Draw(overlay);
                Check(overlay.HasFrame && Texture2D.CreatedCount == 1 && Texture2D.UploadCount == 1 && GUI.OverlayDraws == 1, "Fresh capture not uploaded/drawn once");
            });
            Case("native transition invalidates drawing without freeing the GPU texture", (link, overlay) => {
                Fresh(link, overlay); Draw(overlay); var texture = GUI.LastTexture;
                overlay.Invalidate(); overlay.Update(); Draw(overlay, true);
                Check(!overlay.HasFrame && Texture2D.CreatedCount == 1 && Texture2D.DestroyedCount == 0 && GUI.OverlayDraws == 1 && GUI.CursorDraws == 0 && texture?.Destroyed == false, "Old HUD was drawn or texture destroyed");
            });
            Case("twenty native-control round trips reuse one screen texture and require fresh frames", (link, overlay) => {
                Fresh(link, overlay); Draw(overlay); var texture = GUI.LastTexture;
                for (int i = 0; i < 20; i++) {
                    overlay.Invalidate(); overlay.Update(); Draw(overlay);
                    Check(GUI.OverlayDraws == i + 1, "Stale HUD drew during native control");
                    Fresh(link, overlay); Draw(overlay);
                    Check(ReferenceEquals(texture, GUI.LastTexture), "Native round trip reallocated a texture");
                }
                Check(Texture2D.CreatedCount == 1 && Texture2D.DestroyedCount == 0 && Texture2D.UploadCount == 21 && GUI.OverlayDraws == 21 && overlay.TextureAllocations == 1 && overlay.UploadedFrames == 21, "Round-trip texture/upload accounting differs");
            });
            Case("viewport resize destroys the old texture exactly once", (link, overlay) => {
                Fresh(link, overlay); Draw(overlay); var old = GUI.LastTexture;
                overlay.Invalidate(); Fresh(link, overlay, 8, 6); Draw(overlay);
                Check(old?.Destroyed == true && Texture2D.CreatedCount == 2 && Texture2D.DestroyedCount == 1 && GUI.LastTexture?.width == 8 && GUI.LastTexture.height == 6, "Resize did not replace one texture");
            });
            Case("malformed dimensions cannot revive an old captured HUD", (link, overlay) => {
                Fresh(link, overlay); Draw(overlay);
                Fresh(link, overlay, 0, 4); Draw(overlay);
                Check(!overlay.HasFrame && GUI.OverlayDraws == 1 && Texture2D.UploadCount == 1, "Zero-width frame revived a stale capture");
                Fresh(link, overlay, 4000, 4); Draw(overlay);
                Check(!overlay.HasFrame && Texture2D.CreatedCount == 1 && GUI.OverlayDraws == 1, "Oversized frame allocated or drew");
            });
            Case("failed upload is hidden until a later successful frame", (link, overlay) => {
                Fresh(link, overlay); Draw(overlay);
                Texture2D.FailNextUpload = true;
                try { Fresh(link, overlay); throw new InvalidOperationException("Synthetic upload did not fail"); }
                catch (InvalidOperationException e) when (e.Message == "Synthetic upload failure") { }
                Draw(overlay);
                Check(!overlay.HasFrame && GUI.OverlayDraws == 1 && Texture2D.CreatedCount == 1, "Failed upload exposed old pixels");
                Fresh(link, overlay); Draw(overlay);
                Check(overlay.HasFrame && GUI.OverlayDraws == 2 && Texture2D.CreatedCount == 1, "Upload recovery did not reuse the texture");
            });
            Case("clear is idempotent and frees the screen texture", (link, overlay) => {
                Fresh(link, overlay); overlay.Clear(); overlay.Clear(); Draw(overlay);
                Check(!overlay.HasFrame && Texture2D.CreatedCount == 1 && Texture2D.DestroyedCount == 1 && GUI.OverlayDraws == 0, "Clear retained pixels or double-freed the texture");
            });
            Case("new link frame allocates only after full clear", (link, overlay) => {
                Fresh(link, overlay); overlay.Clear(); overlay.Update();
                Check(Texture2D.CreatedCount == 1, "Clear allocated before a new capture");
                Fresh(link, overlay); Draw(overlay);
                Check(overlay.HasFrame && Texture2D.CreatedCount == 2 && Texture2D.DestroyedCount == 1 && GUI.OverlayDraws == 1, "New link capture did not recover");
            });
            Case("cursor shares validity gating and is freed on full clear", (link, overlay) => {
                Fresh(link, overlay); Draw(overlay, true);
                Check(Texture2D.CreatedCount == 2 && GUI.CursorDraws == 1, "Cursor was not created once");
                overlay.Invalidate(); Draw(overlay, true);
                Check(Texture2D.CreatedCount == 2 && GUI.CursorDraws == 1, "Invalid frame drew/reallocated cursor");
                Fresh(link, overlay); Draw(overlay, true);
                Check(Texture2D.CreatedCount == 2 && GUI.CursorDraws == 2, "Cursor was not reused");
                overlay.Clear();
                Check(Texture2D.DestroyedCount == 2, "Full clear leaked cursor or screen texture");
            });
            Case("layout passes do not draw a valid captured frame", (link, overlay) => {
                Fresh(link, overlay); Event.current.type = EventType.Layout; Draw(overlay, true);
                Check(overlay.HasFrame && GUI.OverlayDraws == 0 && GUI.CursorDraws == 0 && Texture2D.CreatedCount == 1, "Layout pass drew or allocated a cursor");
            });
            Case("fresh frame orientation is retained across allocation reuse", (link, overlay) => {
                Fresh(link, overlay, flags: 0); Draw(overlay);
                Check(GUI.LastUv.y == 1 && GUI.LastUv.height == -1, "Top-down orientation wrong");
                overlay.Invalidate(); Fresh(link, overlay, flags: 1); Draw(overlay);
                Check(GUI.LastUv.y == 0 && GUI.LastUv.height == 1 && Texture2D.CreatedCount == 1, "Bottom-up reused texture orientation wrong");
            });
            Console.WriteLine($"PASS: {count} overlay resource and stale-frame tests");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine("FAIL: " + e); return 1; }
    }
}
