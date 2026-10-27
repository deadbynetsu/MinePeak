using PeakCraft.Link;
using PeakCraft.World;
using Zorro.Core;
using Zorro.UI.Modal;

namespace PeakCraft.Player;

internal sealed class Ownership
{
	public Owner State { get; private set; }
	public bool NativeOverride { get; set; }

	public uint TeleportSeq { get; private set; } = (uint)((int)(PeakLink.TickCount / 1000 % 1000000) * 64 + 2);

	public string CutsceneReason { get; private set; } = "";

	public bool PeakMovement
	{
		get
		{
			Owner state = State;
			if ((uint)state <= 1u || state == Owner.Cutscene || state == Owner.NativeControl)
			{
				return true;
			}
			return false;
		}
	}

	public bool PeakInput
	{
		get
		{
			Owner state = State;
			if (state == Owner.PeakOwns || state == Owner.PeakMenu || state == Owner.NativeControl)
			{
				return true;
			}
			return false;
		}
	}

	public bool PeakCamera
	{
		get
		{
			Owner state = State;
			if ((uint)state <= 1u || state == Owner.Cutscene || state == Owner.NativeControl)
			{
				return true;
			}
			return false;
		}
	}

	public bool PeakHud => State != Owner.MinecraftOwns;

	public bool CharacterShown
	{
		get
		{
			Owner state = State;
			if ((uint)state <= 1u || state == Owner.Cutscene || state == Owner.NativeControl)
			{
				return true;
			}
			return false;
		}
	}

	public bool OverlayAndBlocks
	{
		get
		{
			Owner state = State;
			if ((uint)(state - 2) <= 2u)
			{
				return true;
			}
			return false;
		}
	}

	public bool BodyFollows
	{
		get
		{
			Owner state = State;
			if ((uint)(state - 2) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public void RequestTeleport()
	{
		TeleportSeq++;
	}

	public void Update(bool linkUp, in Proto.GuestState guest)
	{
		Owner owner = Decide(linkUp, in guest);
		if (owner != State)
		{
			bool flag = owner == Owner.Handoff;
			if (flag)
			{
				Owner state = State;
				bool flag2 = ((state == Owner.PeakOwns || state == Owner.Cutscene || state == Owner.NativeControl) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				RequestTeleport();
			}
			Plugin.Log.LogInfo((object)string.Format("ownership: {0} -> {1}{2}{3}", State, owner, (owner == Owner.Cutscene) ? (" (" + CutsceneReason + ")") : "", (owner == Owner.Handoff) ? $" (teleport {TeleportSeq})" : ""));
			State = owner;
		}
	}

	private Owner Decide(bool linkUp, in Proto.GuestState guest)
	{
		if (!linkUp)
		{
			return Owner.PeakOwns;
		}
		Character localCharacter = Character.localCharacter;
		if (localCharacter == null)
		{
			if (!LoadingScreenHandler.loading)
			{
				return Owner.PeakOwns;
			}
			return Cutscene("loading");
		}
		string text = PeakHoldsCharacter(localCharacter);
		if (text != null)
		{
			return Cutscene(text);
		}
		if ((guest.flags & 1) == 0)
		{
			return Owner.PeakOwns;
		}
		if ((guest.flags & 0x20) != 0)
		{
			return Cutscene("Minecraft player dead");
		}
		if (NativeOverride) return Owner.NativeControl;
		Owner state = State;
		if ((state == Owner.PeakOwns || state == Owner.Cutscene || state == Owner.NativeControl) ? true : false)
		{
			return Owner.Handoff;
		}
		if (guest.teleportAck != TeleportSeq)
		{
			return Owner.Handoff;
		}
		if (!PeakMenuOpen())
		{
			return Owner.MinecraftOwns;
		}
		return Owner.PeakMenu;
	}

	private Owner Cutscene(string reason)
	{
		CutsceneReason = reason;
		return Owner.Cutscene;
	}

	private static string? PeakHoldsCharacter(Character character)
	{
		CharacterData data = character.data;
		if (LoadingScreenHandler.loading)
		{
			return "loading";
		}
		if (PeakApi.IsDead(data))
		{
			return "dead";
		}
		if (data.passedOut || data.fullyPassedOut)
		{
			return "passed out";
		}
		if (data.passedOutOnTheBeach > 0f || data.fallSeconds > 0f)
		{
			return "crash landing";
		}
		if (character.warping)
		{
			return "warp";
		}
		if (data.isCarried)
		{
			return "carried";
		}
		if (MainCamera.instance != null && (bool)MainCamera.instance.camOverride)
		{
			return "camera override";
		}
		if (Singleton_MainCameraMovement_IsGodCam())
		{
			return "god cam";
		}
		GUIManager instance = GUIManager.instance;
		if (instance != null && instance.endScreen != null && instance.endScreen.isOpen)
		{
			return "end screen";
		}
		if (character.refs.stats != null && character.refs.stats.won)
		{
			return "ending";
		}
		return null;
	}

	private static bool Singleton_MainCameraMovement_IsGodCam()
	{
		MainCameraMovement instance = Singleton<MainCameraMovement>.Instance;
		if (instance != null)
		{
			return instance.isGodCam;
		}
		return false;
	}

	public static bool PeakMenuOpen()
	{
		GUIManager instance = GUIManager.instance;
		if (instance != null && (instance.windowShowingCursor || instance.windowBlockingInput || instance.wheelActive))
		{
			return true;
		}
		return Modal.IsOpen;
	}
}
