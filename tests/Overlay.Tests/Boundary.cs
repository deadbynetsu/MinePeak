using System;
using System.Runtime.InteropServices;

// This boundary counts actual texture allocation, upload, destruction and draw
// calls made by the linked production Overlay. It exposes no game or window API.
namespace UnityEngine
{
    internal class Object
    {
        public bool Destroyed;
        public static void Destroy(Object item)
        {
            if (item.Destroyed) throw new InvalidOperationException("Texture destroyed twice");
            item.Destroyed = true;
            if (item is Texture2D) Texture2D.DestroyedCount++;
        }
    }
    internal enum TextureFormat { RGBA32 }
    internal enum FilterMode { Point }
    internal enum TextureWrapMode { Clamp }
    internal enum HideFlags { HideAndDontSave }
    internal sealed class Texture2D : Object
    {
        public static int CreatedCount, DestroyedCount, UploadCount;
        public static bool FailNextUpload;
        public readonly int width, height;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public HideFlags hideFlags;
        public Texture2D(int width, int height, TextureFormat format, bool mipChain, bool linear = false)
        {
            this.width = width; this.height = height; CreatedCount++;
            if (format != TextureFormat.RGBA32 || mipChain || linear)
                throw new InvalidOperationException("Unexpected overlay texture configuration");
        }
        public void LoadRawTextureData(IntPtr pointer, int length)
        {
            if (Destroyed) throw new InvalidOperationException("Upload to destroyed texture");
            if (FailNextUpload) { FailNextUpload = false; throw new InvalidOperationException("Synthetic upload failure"); }
            if (pointer == IntPtr.Zero || length != width * height * 4)
                throw new InvalidOperationException("Incorrect RGBA upload extent");
            UploadCount++;
        }
        public void Apply(bool updateMipmaps = false)
        {
            if (Destroyed || updateMipmaps) throw new InvalidOperationException("Invalid texture apply");
        }
        public void SetPixels32(Color32[] pixels)
        {
            if (pixels.Length != width * height) throw new InvalidOperationException("Invalid cursor buffer");
        }
        public static void Reset()
        {
            CreatedCount = DestroyedCount = UploadCount = 0; FailNextUpload = false;
        }
    }
    internal readonly struct Vector2(float x, float y) { public readonly float x = x, y = y; }
    internal readonly struct Rect(float x, float y, float width, float height)
    {
        public readonly float x = x, y = y, width = width, height = height;
    }
    internal readonly struct Color32(byte r, byte g, byte b, byte a)
    {
        public readonly byte r = r, g = g, b = b, a = a;
    }
    internal enum EventType { Repaint, Layout }
    internal sealed class Event
    {
        public static Event current = new();
        public EventType type = EventType.Repaint;
    }
    internal static class Screen { public static int width = 1280, height = 720; }
    internal static class Time { public static float unscaledTime = 0f; }
    internal static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); }
    internal static class GUI
    {
        public static int OverlayDraws, CursorDraws;
        public static Texture2D? LastTexture;
        public static Rect LastUv;
        public static void DrawTextureWithTexCoords(Rect screen, Texture2D? texture, Rect uv, bool alphaBlend)
        {
            if (texture == null || texture.Destroyed || !alphaBlend)
                throw new InvalidOperationException("Invalid overlay draw");
            LastTexture = texture; LastUv = uv; OverlayDraws++;
        }
        public static void DrawTexture(Rect screen, Texture2D? texture)
        {
            if (texture == null || texture.Destroyed) throw new InvalidOperationException("Invalid cursor draw");
            CursorDraws++;
        }
        public static void Reset() { OverlayDraws = CursorDraws = 0; LastTexture = null; Event.current.type = EventType.Repaint; }
    }
}

namespace PeakCraft.Link
{
    internal static class Proto
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct OverlaySlotHdr { public uint width, height, flags, frameId; }
    }
    internal unsafe sealed class PeakLink : IDisposable
    {
        public Proto.OverlaySlotHdr* FrontHeader = (Proto.OverlaySlotHdr*)NativeMemory.AllocZeroed((nuint)sizeof(Proto.OverlaySlotHdr));
        public byte* FrontPixels = (byte*)NativeMemory.Alloc(1);
        private bool pending;
        public void Queue(uint width = 4, uint height = 4, uint flags = 0)
        {
            FrontHeader->width = width; FrontHeader->height = height; FrontHeader->flags = flags;
            FrontHeader->frameId++; pending = true;
        }
        public bool AcquireOverlayFrame() { bool result = pending; pending = false; return result; }
        public void Dispose() { NativeMemory.Free(FrontHeader); NativeMemory.Free(FrontPixels); }
    }
}
namespace PeakCraft
{
    internal static class Plugin
    {
        public static readonly Logger Log = new();
        internal sealed class Logger { public void LogInfo(object value) { } }
    }
}
