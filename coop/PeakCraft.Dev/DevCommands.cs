using PeakCraft.Link;
using PeakCraft.Player;
using PeakCraft.World;
using UnityEngine;
using Zorro.Core;

namespace PeakCraft.Dev;

internal static class DevCommands
{
	public static void Register(Plugin plugin, DevHarness dev)
	{
		PeakLink link = plugin.Link;
		dev.Register("collisionprobe", _ => plugin.Collision.ProbeAhead());
		dev.Register("blockphysics", _ => plugin.Blocks.DescribePhysics());
		dev.Register("nativeinput", a => dev.SetNativeInput(plugin, DevHarness.F(a[0]), DevHarness.F(a[1]), a[2] == "1",
			a.Length > 3 ? DevHarness.F(a[3]) : 0f));
		dev.Register("native", a => {
			var c = Character.localCharacter;
			if (a.Length == 0 || a[0] == "status") {
				if (c == null) { Plugin.Log.LogInfo("native: no character"); return; }
				var item = c.data.currentItem;
				Plugin.Log.LogInfo($"native: state {plugin.Ownership.State}; item {(item != null ? item.name : "none")}; progress {(item != null ? item.progress : 0):F3}; throw {c.refs.items.throwChargeLevel:F3}; climbing {c.data.isClimbingAnything}; stamina {c.data.currentStamina:F3}; feet {Follower.PeakFeet(c):F3}");
				if (c.data.isClimbingAnything) Plugin.Log.LogInfo($"native: climb surface {(c.data.climbHit.collider != null ? c.data.climbHit.collider.name : "none")}; point {c.data.climbPos:F3}; normal {c.data.climbNormal:F3}");
				foreach (var view in c.refs.items.droppedItems) if (view != null) {
					var dropped = view.GetComponent<Item>();
					if (dropped != null && dropped.rig != null) Plugin.Log.LogInfo($"native: dropped {dropped.name} at {dropped.transform.position:F3} velocity {dropped.rig.linearVelocity:F3}");
				}
				return;
			}
			if (a[0] == "hold") plugin.Equipment.DevHold(DevHarness.F(a[1]));
			else if (a[0] == "walltest") NativeTestScene.Wall(a.Length > 1 && a[1] == "mesh");
			else if (a[0] == "wallclear") { NativeTestScene.Clear(); plugin.Collision.Reset("synthetic wall removed"); }
			else if (a[0] == "findwalls") NativeTestScene.FindWalls();
			else if (a[0] == "worlditems" && c != null) {
				foreach (var worldItem in Object.FindObjectsByType<Item>(FindObjectsSortMode.None)) {
					if (worldItem.holderCharacter != null || worldItem.rig == null || (worldItem.transform.position - c.Center).sqrMagnitude > 10000f) continue;
					Plugin.Log.LogInfo($"native: world item {worldItem.name} at {worldItem.transform.position:F3} velocity {worldItem.rig.linearVelocity:F3} distance {Vector3.Distance(c.Center,worldItem.transform.position):F2}");
				}
			}
			else if (a[0] == "slot") plugin.Equipment.DevSelect(int.Parse(a[1]));
			else if (a[0] == "use") plugin.Equipment.DevUse(DevHarness.F(a[1]), a.Length > 2 && a[2] == "secondary");
			else if (a[0] == "throw") plugin.Equipment.DevThrow(DevHarness.F(a[1]));
			else if (a[0] == "climb") plugin.Climb.DevHold(DevHarness.F(a[1]), a.Length > 2 ? DevHarness.F(a[2]) : 0);
			else if (a[0] == "listitems") {
				foreach (var entry in SingletonAsset<ItemDatabase>.Instance.itemLookup) {
					var prefab = entry.Value;
					if (a.Length < 2 || prefab.name.ToLowerInvariant().Contains(a[1].ToLowerInvariant())) Plugin.Log.LogInfo($"native: prefab {entry.Key} {prefab.name} primary {prefab.UIData.hasMainInteract} throwable {prefab.UIData.canDrop}");
				}
			}
			else if (a[0] == "give" && c != null) {
				if (!ItemDatabase.TryGetItem(a[1], out var prefab)) throw new System.ArgumentException("Unknown native item prefab");
				ItemDatabase.Add(prefab, c.Center + c.data.lookDirection * 1.5f);
			}
		});
		dev.Register("vitals", a => {
			Character c = Character.localCharacter;
			if (a.Length == 0) { plugin.Vitals.LogState(); return; }
			if (c == null) return;
			if (a[0] == "peak") {
				c.refs.afflictions.SetStatus(CharacterAfflictions.STATUSTYPE.Injury, 1f - DevHarness.F(a[1]) / 20f);
				c.refs.afflictions.SetStatus(CharacterAfflictions.STATUSTYPE.Hunger, 1f - DevHarness.F(a[2]) / 20f);
			} else if (a[0] == "damagepeak") {
				c.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, DevHarness.F(a[1]));
			} else if (a[0] == "eatpeak" || a[0] == "healpeak") {
				Item item = c.data.currentItem;
				if (item == null) {
					c.refs.items.EquipSlot(Zorro.Core.Optionable<byte>.Some(0));
					item = c.data.currentItem;
				}
				if (item == null) throw new System.InvalidOperationException("Hold a PEAK item before testing its food/heal action.");
				var go = new GameObject("PeakCraft food test action");
				go.SetActive(false);
				try {
					ItemActionBase action;
					if (a[0] == "eatpeak") {
						var food = go.AddComponent<Action_RestoreHunger>();
						food.restorationAmount = DevHarness.F(a[1]); action = food;
					} else {
						var heal = go.AddComponent<Action_ModifyStatus>();
						heal.statusType = CharacterAfflictions.STATUSTYPE.Injury;
						heal.changeAmount = -DevHarness.F(a[1]); action = heal;
					}
					HarmonyLib.AccessTools.Field(typeof(ItemActionBase), "item").SetValue(action, item);
					((ItemAction)action).RunAction();
					Plugin.Log.LogInfo("vitals test: real PEAK action " + a[0] + " " + a[1]);
				} finally { Object.Destroy(go); }
			}
		});
		dev.Register("key", (string[] a) =>
		{
			link.PushInput(Proto.InputType.Key, ushort.Parse(a[0]), int.Parse(a[1]));
		});
		dev.Register("mouse", (string[] a) =>
		{
			link.PushInput(Proto.InputType.MouseButton, ushort.Parse(a[0]), int.Parse(a[1]));
		});
		dev.Register("scroll", (string[] a) =>
		{
			link.PushInput(Proto.InputType.Scroll, 0, int.Parse(a[0]) * 120);
		});
		dev.Register("cursor", (string[] a) =>
		{
			link.PushInput(Proto.InputType.Cursor, 0, int.Parse(a[0]), int.Parse(a[1]));
		});
		dev.Register("text", (string[] a) =>
		{
			string text = string.Join(" ", a);
			foreach (char a2 in text)
			{
				link.PushInput(Proto.InputType.Text, 0, a2);
			}
		});
		dev.Register("releaseall", (string[] _) =>
		{
			link.PushInput(Proto.InputType.ReleaseAll, 0);
		});
		dev.Register("mcmenu", (string[] _) =>
		{
			link.PushInput(Proto.InputType.OpenMenu, 0);
		});
		dev.Register("look", (string[] a) =>
		{
			plugin.Camera.Yaw = DevHarness.F(a[0]);
			plugin.Camera.Pitch = DevHarness.F(a[1]);
		});
		dev.Register("interact", (string[] a) =>
		{
			plugin.Interact.DevHold((a.Length != 0) ? DevHarness.F(a[0]) : 0.2f);
		});
		dev.Register("teleport", (string[] _) =>
		{
			plugin.Ownership.RequestTeleport();
		});
		dev.Register("pause", (string[] _) =>
		{
			if (Character.localCharacter != null)
			{
				Character.localCharacter.input.pauseWasPressed = true;
				GUIManager.instance.UpdatePaused();
			}
		});
		dev.Register("tp", (string[] a) =>
		{
			Character.localCharacter.WarpPlayer(new Vector3(DevHarness.F(a[0]), DevHarness.F(a[1]) + 1.089f, DevHarness.F(a[2])), false);
		});
		dev.Register("find", (string[] a) =>
		{
			MonoBehaviour[] array = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
			foreach (MonoBehaviour monoBehaviour in array)
			{
				if (monoBehaviour.GetType().Name == a[0])
				{
					Plugin.Log.LogInfo((object)$"dev: {a[0]} '{monoBehaviour.name}' at {monoBehaviour.transform.position:F2} forward {monoBehaviour.transform.forward:F2} active {monoBehaviour.isActiveAndEnabled}");
				}
			}
		});
		dev.Register("progress", (string[] _) =>
		{
			MountainProgressHandler.ProgressPoint[] progressPoints = Singleton<MountainProgressHandler>.Instance.progressPoints;
			foreach (MountainProgressHandler.ProgressPoint progressPoint in progressPoints)
			{
				Plugin.Log.LogInfo((object)("dev: progress point '" + progressPoint.title + "' at " + ((progressPoint.transform != null) ? progressPoint.transform.position.ToString("F1") : "none")));
			}
		});
		dev.Register("fog", (string[] a) =>
		{
			OrbFogHandler instance = Singleton<OrbFogHandler>.Instance;
			instance.currentSize = DevHarness.F(a[0]);
			instance.sphere.ENABLE = 1f;
			Plugin.Log.LogInfo((object)$"dev: fog size {instance.currentSize}, player in fog: {Character.localCharacter.data.isInFog}");
		});
		dev.Register("res", (string[] a) =>
		{
			Plugin.Log.LogInfo((object)$"dev: resolution was {Screen.width}x{Screen.height} mode {(int)Screen.fullScreenMode}");
			Screen.SetResolution(int.Parse(a[0]), int.Parse(a[1]), (a.Length > 2) ? ((FullScreenMode)int.Parse(a[2])) : FullScreenMode.Windowed);
		});
		dev.Register("endreturn", (string[] _) =>
		{
			GUIManager.instance.endScreen.ReturnToAirport();
		});
		dev.Register("board", (string[] _) =>
		{
			GUIManager.instance.boardingPass.StartGame();
		});
		dev.Register("closewindows", (string[] _) =>
		{
			MenuWindow.CloseAllWindows();
		});
		dev.Register("where", (string[] _) =>
		{
			Character localCharacter = Character.localCharacter;
			if (localCharacter == null)
			{
				Plugin.Log.LogInfo((object)"dev: no local character");
			}
			else
			{
				Vector3 vector = Follower.PeakFeet(localCharacter);
				Coords.ToMc(vector, out var x, out var y, out var z);
				Proto.GuestState guest = plugin.Guest;
				bool flag = Physics.Raycast(vector + Vector3.up * 1.5f, Vector3.down, out var hitInfo, 50f, LayerMask.GetMask("Terrain", "Map", "Default"), QueryTriggerInteraction.Ignore);
				Plugin.Log.LogInfo((object)string.Format("dev: PEAK ground under the feet at unity y {0:F3} = mc y {1:F3} ({2}); camera mode {3} distance {4:F2}", flag ? hitInfo.point.y : float.NaN, flag ? ((double)hitInfo.point.y + Coords.YOffset) : double.NaN, flag ? hitInfo.collider.name : "none", guest.cameraMode, guest.cameraDistance));
				Plugin.Log.LogInfo((object)$"dev: state {plugin.Ownership.State}; PEAK feet {vector:F3} = mc ({x:F3}, {y:F3}, {z:F3}); Minecraft feet ({guest.x:F3}, {guest.y:F3}, {guest.z:F3}) yaw {guest.yaw:F1} pitch {guest.pitch:F1} flags 0x{guest.flags:X}; look yaw {plugin.Camera.Yaw:F1} pitch {plugin.Camera.Pitch:F1}; camera {MainCamera.instance.transform.position:F3}; kinematic {localCharacter.data.isKinecmatic}; stamina {localCharacter.data.currentStamina:F2}");
			}
		});
	}
}
