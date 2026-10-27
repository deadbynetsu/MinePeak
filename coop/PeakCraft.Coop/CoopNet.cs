using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace PeakCraft.Coop;

internal sealed class CoopNet : IDisposable
{
	public const int LoopbackActor = -1;

	private readonly CoopConfig config;

	private readonly Dictionary<string, object> props = new Dictionary<string, object>();

	private readonly HashSet<string> changed = new HashSet<string>();

	private bool inRoom;

	private bool propsDirty;
	private Room? room;
	public int SessionGeneration { get; private set; }
	public int LocalActor => PhotonNetwork.LocalPlayer?.ActorNumber ?? 1;
	public bool Online => PhotonNetwork.InRoom && !PhotonNetwork.OfflineMode;
	public IEnumerable<int> OtherActors
	{
		get { foreach (var player in PhotonNetwork.PlayerListOthers) yield return player.ActorNumber; }
	}

	public bool Share => config.CoopShare.Value;

	public bool Loopback => config.CoopLoopback.Value;

	public IReadOnlyDictionary<string, object> LocalProps => props;

	public event Action<int, object[]>? BlockReceived;

	public event Action<int, object[]>? PoseReceived;
	public event Action<int, object[]>? SnapshotRequested;
	public event Action<int, object[]>? SnapshotReceived;

	public CoopNet(CoopConfig config)
	{
		this.config = config;
		PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
	}

	public void Dispose()
	{
		PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
	}

	public void SetProp(string key, object value)
	{
		if (!props.TryGetValue(key, out object value2) || (!object.Equals(value2, value) && (!(value2 is byte[] array) || !(value is byte[] array2) || !array.AsSpan().SequenceEqual(array2))))
		{
			props[key] = value;
			changed.Add(key);
			propsDirty = true;
		}
	}

	public void Update()
	{
		bool flag = PhotonNetwork.InRoom;
		bool sessionChanged = flag != inRoom || (flag && !ReferenceEquals(room, PhotonNetwork.CurrentRoom));
		if (sessionChanged)
		{
			SessionGeneration++;
			room = PhotonNetwork.CurrentRoom;
		}
		if (flag && sessionChanged)
		{
			string text = PhotonNetwork.CurrentRoom?.Name ?? "?";
			Plugin.Log.LogInfo((object)string.Format("coop: joined room {0} ({1}), {2} player(s) of {3}", text, PhotonNetwork.OfflineMode ? "offline" : PhotonNetwork.CloudRegion, PhotonNetwork.CurrentRoom?.PlayerCount ?? 0, PhotonNetwork.CurrentRoom?.MaxPlayers ?? 0));
			propsDirty = true;
			changed.UnionWith(props.Keys);
		}
		inRoom = flag;
		if (!inRoom || !propsDirty || !Share || !PhotonNetwork.IsMessageQueueRunning)
		{
			return;
		}
		Hashtable hashtable = new Hashtable();
		foreach (string item in changed)
		{
			if (props.TryGetValue(item, out object value))
			{
				hashtable[item] = value;
			}
		}
		if (hashtable.Count > 0)
		{
			if (!PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable)) return;
		}
		changed.Clear();
		propsDirty = false;
	}

	public bool SendToActor(byte code, object[] data, int actor)
	{
		if (!Share || !Online || actor <= 0 || !PhotonNetwork.IsMessageQueueRunning) return false;
		return PhotonNetwork.RaiseEvent(code, data, new RaiseEventOptions { TargetActors = new[] { actor } }, SendOptions.SendReliable);
	}

	public void Send(byte code, object[] data, bool reliable, bool cache)
	{
		if (Share)
		{
			if (PhotonNetwork.InRoom && !PhotonNetwork.OfflineMode && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1)
			{
				RaiseEventOptions raiseEventOptions = new RaiseEventOptions
				{
					Receivers = ReceiverGroup.Others,
					CachingOption = (cache ? EventCaching.AddToRoomCache : EventCaching.DoNotCache)
				};
				PhotonNetwork.RaiseEvent(code, data, raiseEventOptions, reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
			}
			if (Loopback)
			{
				Dispatch(code, -1, data);
			}
		}
	}

	private void OnEvent(EventData photonEvent)
	{
		if ((photonEvent.Code < CoopProto.PhotonBlockSet || photonEvent.Code > CoopProto.PhotonSnapshot) ||
			!(photonEvent.CustomData is object[] array) || array.Length == 0 || !(array[0] is int num) || num != CoopProto.Magic || !Share ||
			PhotonNetwork.CurrentRoom?.GetPlayer(photonEvent.Sender) == null)
		{
			return;
		}
		try
		{
			Dispatch(photonEvent.Code, photonEvent.Sender, array);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)$"coop: bad event {photonEvent.Code} from actor {photonEvent.Sender}: {ex.Message}");
		}
	}

	private void Dispatch(byte code, int actor, object[] data)
	{
		switch (code)
		{
		case 171:
			BlockReceived?.Invoke(actor, data);
			break;
		case 172:
			PoseReceived?.Invoke(actor, data);
			break;
		case CoopProto.PhotonSnapshotRequest:
			SnapshotRequested?.Invoke(actor, data);
			break;
		case CoopProto.PhotonSnapshot:
			SnapshotReceived?.Invoke(actor, data);
			break;
		}
	}

	public object? Prop(int actor, string key)
	{
		if (actor == -1)
		{
			if (!props.TryGetValue(key, out object value))
			{
				return null;
			}
			return value;
		}
		Photon.Realtime.Player player = PhotonNetwork.CurrentRoom?.GetPlayer(actor);
		if (player == null || !player.CustomProperties.TryGetValue(key, out var value2))
		{
			return null;
		}
		return value2;
	}
}
