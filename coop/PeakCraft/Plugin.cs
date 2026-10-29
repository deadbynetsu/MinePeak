using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PeakCraft.Coop;
using PeakCraft.Dev;
using PeakCraft.Hazards;
using PeakCraft.Input;
using PeakCraft.Link;
using PeakCraft.Patches;
using PeakCraft.Player;
using PeakCraft.Render;
using PeakCraft.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zorro.Core;

namespace PeakCraft;

[BepInAutoPlugin]
[DefaultExecutionOrder(-1000)]
[BepInPlugin("PeakCraftCoop", "PeakCraft Co-op", "0.4.0")]
public class Plugin : BaseUnityPlugin
{
	private ConfigEntry<string> linkName;

	private ConfigEntry<float> yOffset;

	private ConfigEntry<bool> devHarness;

	private Harmony? harmony;

	private bool linked;

	private uint guestPid;

	private Proto.GuestState guest;

	private Segment lastSegment;

	private float segmentResetAt = -1f;

	private float lastStateLog;

	private bool hudHidden;

	private string? lastHeld;

	private float lastFrameError = -100f;

	private float lastGuiError = -100f;

	private float lastCoopError = -100f;

	public const string Id = "PeakCraftCoop";

	public const string Name = "PeakCraft Co-op";

	public const string Version = "0.4.0";

	internal static ManualLogSource Log { get; private set; }

	internal static Plugin Instance { get; private set; }

	internal PeakLink Link { get; private set; }

	internal Ownership Ownership { get; } = new Ownership();

	internal Follower Follower { get; private set; }

	internal CameraDriver Camera { get; } = new CameraDriver();

	internal Interact Interact { get; } = new Interact();
	internal NativeActions Equipment { get; private set; }
	internal NativeClimb Climb { get; private set; }

	internal InputBridge InputBridge { get; private set; }

	internal CollisionExporter Collision { get; private set; }

	internal Overlay Overlay { get; private set; }

	internal RenderRing Blocks { get; private set; }

	internal AfflictionBridge Hazards { get; private set; }

	internal VitalsSync Vitals { get; private set; }

	internal Death Death { get; private set; }

	internal DevHarness? Dev { get; private set; }

	internal MeltyTest? Test { get; private set; }

	internal CoopConfig CoopConfig { get; private set; }

	internal CoopMode CoopMode { get; private set; }

	internal CoopNet CoopNet { get; private set; }

	internal McTextures CoopTextures { get; private set; }

	internal ViewerBlocks ViewerBlocks { get; private set; }

	internal BlockRelay BlockRelay { get; private set; }

	internal SkinShare SkinShare { get; private set; }

	internal PoseShare PoseShare { get; private set; }

	internal RemoteAvatars RemoteAvatars { get; private set; }

	internal ScoutHider ScoutHider { get; private set; }

	internal Proto.GuestState Guest => guest;

	private void Awake()
	{
		Log = base.Logger;
		try
		{
			Start0();
		}
		catch (Exception arg)
		{
			Log.LogError((object)$"PeakCraft Co-op could not start: {arg}");
			try
			{
				Harmony? val = harmony;
				if (val != null)
				{
					val.UnpatchSelf();
				}
			}
			catch
			{
			}
			((Behaviour)this).enabled = false;
		}
	}

	private void Start0()
	{
		Instance = this;
		linkName = ((BaseUnityPlugin)this).Config.Bind<string>("Link", "MappingName", "Local\\PeakCraftCoop_v1", "Name of the shared-memory mapping. Must match Minecraft's -Dskycraft.link (default Local\\PeakCraftCoop_v1).");
		yOffset = ((BaseUnityPlugin)this).Config.Bind<float>("World", "YOffset", -512f, "Added to PEAK's height to get Minecraft's. Keeps the mountain inside the mirror world's build range (-1024 to 1023). Use a multiple of 8.");
		devHarness = ((BaseUnityPlugin)this).Config.Bind<bool>("Dev", "Harness", false, "Development only: auto-start an offline session and run commands from BepInEx/peakcraft-dev.txt.");
		Coords.YOffset = yOffset.Value;
		Log.LogInfo((object)$"Plugin PeakCraft Local {Version} is loaded!");
		Test = MeltyTest.Detect();
		Test?.Pass("plugin-loaded");
		CoopConfig = new CoopConfig(((BaseUnityPlugin)this).Config);
		CoopMode = new CoopMode(CoopConfig);
		bool flag = Proto.SelfCheck(Log);
		bool flag2 = Rings.SelfTest(Log);
		Link = new PeakLink(Log);
		if (!flag || !flag2 || !Link.Create(linkName.Value) || !ApplyPatches())
		{
			Log.LogError((object)"PeakCraft is disabled for this session: PEAK runs unmodified.");
			((Behaviour)this).enabled = false;
			return;
		}
		Follower = new Follower(Link);
		Equipment = new NativeActions(Config);
		Climb = new NativeClimb(Config);
		InputBridge = new InputBridge(Link, Camera, Interact, ((BaseUnityPlugin)this).Config);
		Collision = new CollisionExporter(Link);
		Overlay = new Overlay(Link);
		Blocks = new RenderRing(Link, ((BaseUnityPlugin)this).Config);
		Hazards = new AfflictionBridge(Link, ((BaseUnityPlugin)this).Config);
		Death = new Death(Link);
		Vitals = new VitalsSync(Link, Config);
		CoopNet = new CoopNet(CoopConfig);
		CoopTextures = new McTextures(CoopConfig);
		ViewerBlocks = new ViewerBlocks(CoopTextures);
		BlockRelay = new BlockRelay(CoopNet, ViewerBlocks, Link, () => linked && Blocks.SceneReady);
		BlockRelay.HeldChanged += (string held) =>
		{
			if (!(held == lastHeld))
			{
				lastHeld = held;
				CoopNet.SetProp("pcc.held", held);
				Log.LogInfo((object)("coop: holding " + ((held.Length > 0) ? held : "nothing")));
			}
		};
		SkinShare = new SkinShare(CoopNet);
		PoseShare = new PoseShare(CoopNet);
		RemoteAvatars = new RemoteAvatars(CoopNet, CoopTextures);
		ScoutHider = new ScoutHider(RemoteAvatars, CoopNet);
		CoopNet.SetProp("pcc.v", CoopProto.Version);
		CoopNet.SetProp("pcc.mode", CoopMode.PropValue);
		SceneManager.sceneLoaded += OnSceneLoaded;
		if (devHarness.Value)
		{
			Dev = new DevHarness();
			DevCommands.Register(this, Dev);
			Dev.Register("mode", a => CoopMode.SetMinecraft(a.Length > 0 && (a[0] == "on" || a[0] == "1")));
			Dev.Register("loopback", a => {
				CoopConfig.CoopLoopback.Value = a.Length > 0 && a[0] == "on";
				Log.LogInfo($"dev: loopback {CoopNet.Loopback}; blocks sent {BlockRelay.Sent}, received {BlockRelay.Received}; viewer {ViewerBlocks.Count}; journal {BlockRelay.JournalCount}; pending {BlockRelay.PendingInputs}; snapshots sent {BlockRelay.SnapshotChunksSent}, received {BlockRelay.SnapshotChunksReceived}");
			});
		}
	}

	private bool ApplyPatches()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		harmony = new Harmony("PeakCraftCoop");
		Type[] array = new Type[6]
		{
			typeof(CharacterInputPatch),
			typeof(CharacterMovementPatch),
			typeof(MainCameraMovementPatch),
			typeof(CharacterAfflictionsPatch),
			typeof(CharacterPatch),
			typeof(NativeHungerPatch)
		};
		foreach (Type type in array)
		{
			try
			{
				harmony.CreateClassProcessor(type).Patch();
				Log.LogInfo((object)("patch ok   " + type.Name));
			}
			catch (Exception ex)
			{
				Log.LogError((object)("patch FAIL " + type.Name + ": " + ex.Message));
				harmony.UnpatchSelf();
				return false;
			}
		}
		return true;
	}

	private void OnDestroy()
	{
		Dev?.ResetNativeInput();
		Quietly(() => PeakCraft.UI.MinePeakUi.Dispose());
		SceneManager.sceneLoaded -= OnSceneLoaded;
		Quietly(() =>
		{
			Follower?.Release();
		});
		Quietly(() =>
		{
			CoopNet?.Dispose();
		});
		Quietly(() =>
		{
			RemoteAvatars?.Dispose();
		});
		Quietly(() =>
		{
			ViewerBlocks?.Dispose();
		});
		Quietly(() =>
		{
			Blocks?.Dispose();
		});
		Quietly(() =>
		{
			CoopTextures?.Dispose();
		});
		Quietly(() =>
		{
			Harmony? val = harmony;
			if (val != null)
			{
				val.UnpatchSelf();
			}
		});
		Quietly(() =>
		{
			InputBridge?.Dispose();
		});
		Quietly(() =>
		{
			Link?.Close();
		});
	}

	private static void Quietly(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Log.LogWarning((object)("shutdown: " + ex.Message));
		}
	}

	private void FrameError(Exception e, ref float last, string where)
	{
		if (Time.unscaledTime - last > 10f)
		{
			last = Time.unscaledTime;
			Log.LogError((object)$"{where}: {e}");
		}
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Dev?.ResetNativeInput();
		Collision?.Reset("scene " + scene.name);
		Blocks?.Reset();
		BlockRelay?.ResetScene();
		Vitals?.Reset();
		Equipment?.Reset();
		Climb?.Reset();
	}

	private void Update()
	{
		Link.Heartbeat();
		try
		{
			Frame();
		}
		catch (Exception e)
		{
			FrameError(e, ref lastFrameError, "frame");
			try
			{
				PublishHostState(Character.localCharacter);
			}
			catch
			{
			}
		}
	}

	private void Frame()
	{
		Dev?.Update();
		bool guestAlive = Link.GuestAlive;
		if (guestAlive && (!linked || Link.GuestPid != guestPid))
		{
			guestPid = Link.GuestPid;
			Log.LogInfo((object)$"link up: Minecraft (pid {guestPid}) is on the other end");
			Collision.Reset("Minecraft linked");
			Link.ResetOverlay();
		}
		else if (!guestAlive && linked)
		{
			Log.LogInfo((object)"link down: Minecraft's heartbeat stopped; PEAK has the player");
			Overlay.Clear();
			Blocks.Reset();
		}
		linked = guestAlive;
		if (linked && Link.ReadGuestState(out var state))
		{
			guest = state;
		}
		DrainEvents();
		Character localCharacter = Character.localCharacter;
		bool nativeMenu = Ownership.PeakMenuOpen() || InputBridge.ScreenOpen;
		Equipment.UpdateRequest(linked && !CoopMode.IsViewer, nativeMenu, localCharacter);
		Climb.UpdateRequest(linked && !CoopMode.IsViewer, nativeMenu, localCharacter);
		Ownership.NativeOverride = Equipment.WantsNative || Climb.WantsNative;
		_ = Ownership.State;
		Ownership.Update(linked && !CoopMode.IsViewer, in guest);
		Owner state2 = Ownership.State;
		if (localCharacter != null && Ownership.PeakCamera)
		{
			Camera.SetLookFromPeak(localCharacter);
		}
		Follower.Update(Ownership, in guest);
		Equipment.TickNative(localCharacter, Climb.WantsNative);
		Climb.TickNative(localCharacter);
		Climb.ConsumeReturnTeleport(); // Ownership issues the fresh teleport on NativeControl -> Handoff.
		UpdateCollision(localCharacter);
		InputBridge.Update(Ownership, in guest);
		Vitals.Update(linked, Ownership);
		Hazards.Update(Ownership);
		Death.Update(linked, in guest);
		UpdateHud();
		if (Ownership.OverlayAndBlocks)
		{
			Overlay.Update();
		}
		if (linked)
		{
			Blocks.Update(Ownership.OverlayAndBlocks, Follower.Feet, Camera.Yaw, guest.cameraMode == 0, (CoopMode.IsViewer || state2 == Owner.NativeControl) && localCharacter != null);
		}
		PublishHostState(localCharacter);
		UpdateCoop();
		Test?.Update(linked, state2);
		if (linked && Time.unscaledTime - lastStateLog > 10f)
		{
			lastStateLog = Time.unscaledTime;
			Log.LogInfo((object)$"state: {state2}; {1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f):F0} fps; Minecraft flags 0x{guest.flags:X} feet ({guest.x:F2}, {guest.y:F2}, {guest.z:F2}) ack {guest.teleportAck}/{Ownership.TeleportSeq}");
		}
	}

	private void DrainEvents()
	{
		Proto.GuestEvent e;
		while (Link.PopEvent(out e))
		{
			if (e.type == 2)
			{
				if (!Vitals.Engaged && !CoopMode.IsViewer && Ownership.BodyFollows) Death.OnMinecraftDeath();
			}
			else if (e.type == 6 || e.type == 7)
			{
				// The event lane has no world ID. Old queued edits cannot be attributed
				// to the newly loaded PEAK scene while Java is still changing worlds.
				// Held-item strings (id 1) are independent of a construction's scene.
				if (Blocks.SceneReady || (e.type == 6 && (e.formId >> 16) == 1))
					BlockRelay.OnMinecraftEvent(in e);
			}
		}
	}

	private void UpdateCoop()
	{
		try
		{
			CoopMode.Update();
			// Remote clients must show the native scout during equipment/climbing,
			// cutscenes and a broken Java link, rather than a stale Minecraft pose.
			CoopNet.SetProp("pcc.mode", linked && !CoopMode.IsViewer && Ownership.BodyFollows ? "mc" : "viewer");
			CoopNet.Update();
			BlockRelay.Update();
			PoseShare.Update(linked && !CoopMode.IsViewer && Ownership.BodyFollows, Follower.Feet, in guest);
			RemoteAvatars.Update();
			ScoutHider.Update();
			if (!linked)
			{
				ViewerBlocks.Update();
			}
		}
		catch (Exception arg)
		{
			if (Time.unscaledTime - lastCoopError > 10f)
			{
				lastCoopError = Time.unscaledTime;
				Log.LogError((object)$"coop: {arg}");
			}
		}
	}

	private void UpdateCollision(Character? character)
	{
		if (!linked || character == null || LoadingScreenHandler.loading)
		{
			return;
		}
		MapHandler instance = Singleton<MapHandler>.Instance;
		if (instance != null)
		{
			Segment currentSegment = instance.GetCurrentSegment();
			if (currentSegment != lastSegment)
			{
				lastSegment = currentSegment;
				Collision.Reset($"segment {currentSegment}");
				segmentResetAt = Time.unscaledTime + 6f;
			}
		}
		if (segmentResetAt > 0f && Time.unscaledTime > segmentResetAt)
		{
			segmentResetAt = -1f;
			Collision.Reset("segment settled");
		}
		if (Ownership.BodyFollows)
		{
			Collision.Update(guest.x, guest.y, guest.z);
		}
		else if (!PeakApi.IsDead(character.data))
		{
			Coords.ToMc(PeakCraft.Player.Follower.PeakFeet(character), out var x, out var y, out var z);
			Collision.Update(x, y, z);
		}
	}

	private void UpdateHud()
	{
		GUIManager instance = GUIManager.instance;
		if (instance == null || instance.hudCanvas == null)
		{
			hudHidden = false;
			return;
		}
		bool flag = !Ownership.PeakHud;
		if (flag != hudHidden || instance.hudCanvas.enabled == flag)
		{
			hudHidden = flag;
			instance.hudCanvas.enabled = !flag;
		}
	}

	private void PublishHostState(Character? character)
	{
		Proto.HostState state = new Proto.HostState
		{
			collisionEpoch = Collision.Epoch,
			teleportSeq = Ownership.TeleportSeq,
			viewportW = (uint)Screen.width,
			viewportH = (uint)Screen.height,
			gameHour = 12f,
			yaw = Camera.Yaw,
			pitch = Camera.Pitch,
			worldId = PeakApi.CurrentWorldId
		};
		Owner state2 = Ownership.State;
		bool num = CoopMode.IsViewer || character == null || LoadingScreenHandler.loading || state2 == Owner.Cutscene || state2 == Owner.NativeControl;
		if (character != null)
		{
			state.flags |= 1u;
			Coords.ToMc(Ownership.BodyFollows ? Follower.Feet : PeakCraft.Player.Follower.PeakFeet(character), out state.posX, out state.posY, out state.posZ);
		}
		if (num)
		{
			state.flags |= 4u;
		}
		if (state2 == Owner.PeakMenu)
		{
			state.flags |= 2u;
		}
		Link.WriteHostState(in state);
	}

	private void DrawInteractPrompt()
	{
		if (Ownership.State == Owner.MinecraftOwns && !InputBridge.ScreenOpen && Event.current.type == EventType.Repaint && !(Interaction.instance == null) && Interaction.instance.bestInteractable != null)
		{
			IInteractible bestInteractable = Interaction.instance.bestInteractable;
			bool flag = bestInteractable is IInteractibleConstant interactibleConstant && interactibleConstant.IsConstantlyInteractable(Character.localCharacter);
			float progress = flag && Interaction.instance.currentHeldInteractible == bestInteractable ? Interaction.instance.constantInteractableProgress : 0f;
			PeakCraft.UI.MinePeakUi.DrawInteraction(InputBridge.InteractKeyName, bestInteractable.GetInteractionText(), flag, progress);
		}
	}

	private void OnGUI()
	{
		try
		{
			DrawGui();
		}
		catch (Exception e)
		{
			FrameError(e, ref lastGuiError, "gui");
		}
	}

	private void DrawGui()
	{
		CoopMode?.OnGUI();
		if (Ownership.OverlayAndBlocks && !PeakCraft.Player.Ownership.PeakMenuOpen())
		{
			Overlay.Draw(InputBridge.ScreenOpen, InputBridge.Cursor);
			DrawInteractPrompt();
		}
		Equipment?.Draw();
	}
}
