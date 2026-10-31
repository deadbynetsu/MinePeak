// Minimal game boundaries for exercising the actual NativeClimb request/return
// flow without shipping or loading any proprietary game assembly.
namespace BepInEx.Configuration
{
    internal sealed class ConfigEntry<T> { public T Value; public ConfigEntry(T value) { Value = value; } }
    internal sealed class ConfigFile
    {
        public ConfigEntry<T> Bind<T>(string section, string name, T value, string description) => new(value);
    }
}
namespace UnityEngine
{
    internal static class Application { public static bool isFocused = true; }
    internal static class Time { public static float unscaledTime, fixedTime; }
    internal static class Mathf
    {
        public static float Abs(float value) => System.Math.Abs(value);
        public static float Clamp(float value, float min, float max) => System.Math.Clamp(value, min, max);
        public static float Max(float a, float b) => System.Math.Max(a, b);
    }
    internal struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    internal struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default;
        public string ToString(string format) => "feet";
        public static Vector3 ClampMagnitude(Vector3 v, float max)
        {
            float n = (float)System.Math.Sqrt(v.x*v.x + v.y*v.y + v.z*v.z);
            return n > max ? new(v.x*max/n, v.y*max/n, v.z*max/n) : v;
        }
    }
}
namespace UnityEngine.InputSystem
{
    internal enum Key { V, Space }
    internal sealed class KeyControl { public bool isPressed; }
    internal sealed class Keyboard
    {
        public static Keyboard current = new();
        private readonly KeyControl[] controls = { new(), new() };
        public KeyControl this[Key key] => controls[(int)key];
    }
}
namespace PeakCraft.World
{
    internal static class PeakApi { public static bool IsDead(CharacterData data) => data.dead; }
}
namespace PeakCraft.Player
{
    internal enum Owner { MinecraftOwns, NativeControl }
    internal sealed class Ownership { public Owner State = Owner.NativeControl; }
    internal static class Follower { public static UnityEngine.Vector3 PeakFeet(Character character) => default; }
}
namespace PeakCraft
{
    internal sealed class Plugin
    {
        public static Plugin Instance = new();
        public static Logger Log = new();
        public Player.Ownership Ownership = new();
    }
    internal sealed class Logger { public void LogInfo(string message) { } }
}
internal static class LoadingScreenHandler { public static bool loading; }
internal sealed class Character
{
    public bool warping;
    public CharacterData data = new();
    public CharacterRefs refs;
    public Character() { refs = new(this); }
    public bool CheckJump() => !data.fullyPassedOut && !data.isClimbing && data.currentClimbHandle == null;
}
internal sealed class CharacterData
{
    public bool dead, passedOut, fullyPassedOut, isClimbing, isGrounded, chargingJump;
    public bool isClimbingAnything => isClimbing;
    public object currentClimbHandle, currentItem;
    public float currentStamina = 1;
    public int extraJumps, jumpsRemaining;
    public float sinceJump = 1, sinceGrounded, groundedFor;
    public UnityEngine.Vector3 avarageVelocity;
}
internal sealed class CharacterRefs
{
    public FakeClimbing climbing;
    public FakeAfflictions afflictions = new();
    public FakeRagdoll ragdoll = new();
    public FakeMovement movement;
    public CharacterRefs(Character character) { climbing = new(character); movement = new(character); }
}
internal sealed class FakeClimbing
{
    private readonly Character character;
    public int Attempts, Stops;
    public FakeClimbing(Character c) { character = c; }
    public void TryClimb() { Attempts++; character.data.isClimbing = true; }
    public void StopAnyClimbing() { Stops++; character.data.isClimbing = false; }
}
internal sealed class FakeAction { public bool Held; public bool IsPressed() => Held; }
internal sealed class CharacterInput
{
    public static FakeAction action_usePrimary = new();
    public bool jumpWasPressed, usePrimaryWasPressed, usePrimaryIsPressed, usePrimaryWasReleased;
    public UnityEngine.Vector2 movementInput;
}
internal sealed class FakeAfflictions { public int totalExtraJumps; }
internal sealed class FakeRagdoll { public System.Collections.Generic.List<Bodypart> partList = new() { new(), new() }; }
internal sealed class Bodypart { public FakeRig Rig = new(); }
internal sealed class FakeRig { public UnityEngine.Vector3 linearVelocity; }
internal sealed class FakeMovement
{
    private readonly Character character;
    public int Attempts;
    public bool AcceptJump = true;
    public FakeMovement(Character c) { character = c; }
    public void TryToJump()
    {
        Attempts++;
        if (!AcceptJump || character.data.extraJumps <= 0 || !character.CheckJump()) return;
        character.data.extraJumps--;
        character.data.chargingJump = true;
    }
}
namespace PeakCraft.Link
{
    internal static class Proto
    {
        [System.Flags]
        public enum GuestFlags : uint
        {
            InWorld=1, ScreenOpen=2, OnGround=4, Dead=0x20, Swimming=0x40,
            Flying=0x80, Creative=0x100, MovementReady=0x200
        }
        public struct GuestState
        {
            public uint flags;
            public float tickMs;
            public double curX, curY, curZ, prevX, prevY, prevZ;
        }
    }
}
