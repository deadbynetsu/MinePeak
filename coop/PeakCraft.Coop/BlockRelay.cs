using System;
using System.Collections.Generic;
using System.Text;
using PeakCraft.Link;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeakCraft.Coop;

internal sealed class BlockRelay
{
    public const int LoopbackOffset = 3;
    private const int SnapshotBatch = 32;
    private readonly CoopNet net;
    private readonly ViewerBlocks viewer;
    private readonly PeakLink link;
    private readonly Func<bool> minecraftLinked;
    private readonly BlockJournal journal = new BlockJournal();
    private readonly Dictionary<int, byte[]> stringParts = new Dictionary<int, byte[]>();
    private readonly Queue<(int, int, int)> pending = new Queue<(int, int, int)>();
    private readonly HashSet<(int, int, int)> pendingSet = new HashSet<(int, int, int)>();
    private readonly Dictionary<int, List<BlockEdit>> transfers = new Dictionary<int, List<BlockEdit>>();
    private readonly Dictionary<int, float> lastRequest = new Dictionary<int, float>();
    private readonly HashSet<int> peers = new HashSet<int>();
    private string pendingBlockName = "", scene = "";
    private int session = -1, retryCount;
    private bool guestWasLinked;
    private float nextRequest, nextScan;
    public int Sent { get; private set; }
    public int Received { get; private set; }
    public int SnapshotChunksSent { get; private set; }
    public int SnapshotChunksReceived { get; private set; }
    public int JournalCount => journal.Count;
    public int PendingInputs => pending.Count;
    public event Action<string>? HeldChanged;

    public BlockRelay(CoopNet net, ViewerBlocks viewer, PeakLink link, Func<bool> minecraftLinked)
    {
        this.net = net; this.viewer = viewer; this.link = link; this.minecraftLinked = minecraftLinked;
        net.BlockReceived += OnRemote;
        net.SnapshotRequested += OnSnapshotRequest;
        net.SnapshotReceived += OnSnapshot;
    }

    public unsafe void OnMinecraftEvent(in Proto.GuestEvent e)
    {
        if (e.type == CoopProto.LinkEvString)
        {
            int id = (int)(e.formId >> 16), part = (int)((e.formId >> 8) & 255), count = (int)(e.formId & 255);
            if ((id != 1 && id != 2) || count == 0 || count > 8 || part >= count) return;
            if (part == 0 || !stringParts.TryGetValue(id, out byte[]? value) || value.Length != count * 24)
            { value = new byte[count * 24]; stringParts[id] = value; }
            Proto.GuestEvent copy = e;
            byte* bytes = (byte*)(&copy.a);
            for (int i = 0; i < 24; i++) value[part * 24 + i] = bytes[i];
            if (part == count - 1)
            {
                int end = Array.IndexOf(value, (byte)0);
                string text = Encoding.UTF8.GetString(value, 0, end < 0 ? value.Length : end);
                if (id == 1) HeldChanged?.Invoke(text); else pendingBlockName = text;
            }
        }
        else if (e.type == CoopProto.LinkEvBlockSet)
        {
            Proto.GuestEvent copy = e;
            int* xyz = (int*)(&copy.a);
            string name = pendingBlockName.Length > 0 ? pendingBlockName : "minecraft:stone";
            pendingBlockName = "";
            var edit = journal.Local(SceneManager.GetActiveScene().name, xyz[0], xyz[1], xyz[2], (int)e.formId, name, Math.Max(1, net.LocalActor));
            if (!journal.Merge(edit))
            {
                Plugin.Log.LogWarning("coop: local edit rejected (invalid state/name or journal limit of 65536 coordinates)");
                return;
            }
            viewer.Set(edit.X, edit.Y, edit.Z, edit.Name);
            Sent++;
            Plugin.Log.LogInfo($"coop: local block {name} (state {edit.State}) at ({edit.X}, {edit.Y}, {edit.Z}), revision {edit.Revision}");
            net.Send(CoopProto.PhotonBlockSet, edit.Packet(), reliable: true, cache: false);
        }
    }

    public void ResetScene()
    {
        string current = SceneManager.GetActiveScene().name;
        if (scene == current) return;
        scene = current;
        Clear();
    }

    private void Clear()
    {
        pendingBlockName = ""; stringParts.Clear(); viewer.Clear(); journal.Clear();
        pending.Clear(); pendingSet.Clear(); transfers.Clear(); lastRequest.Clear(); peers.Clear();
        nextRequest = Time.unscaledTime + 1f; retryCount = 0;
    }

    public void Update()
    {
        if (session != net.SessionGeneration)
        {
            session = net.SessionGeneration;
            scene = SceneManager.GetActiveScene().name;
            Clear();
        }
        bool linked = minecraftLinked();
        if (linked && !guestWasLinked) foreach (var edit in journal.Values) Queue(edit);
        guestWasLinked = linked;
        if (linked)
        {
            for (int i = 0; i < 16 && pending.Count > 0; i++)
            {
                var key = pending.Peek();
                // A busy local Minecraft can fill its input ring during a snapshot.
                // Keep this coordinate queued until it accepted the latest journal state;
                // otherwise a dropped removal can leave a permanent remote ghost block.
                if (journal.TryGet(key, out var edit) &&
                    !link.TryPushInput((Proto.InputType)CoopProto.LinkInSetBlock, (ushort)edit.State, edit.X, edit.Y, edit.Z)) break;
                pending.Dequeue(); pendingSet.Remove(key);
            }
        }
        if (!net.Online || !net.Share) return;
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.5f;
            var currentActors = new HashSet<int>(net.OtherActors);
            peers.RemoveWhere(actor => !currentActors.Contains(actor));
            foreach (int actor in new List<int>(transfers.Keys)) if (!currentActors.Contains(actor)) transfers.Remove(actor);
            foreach (int actor in new List<int>(lastRequest.Keys)) if (!currentActors.Contains(actor)) lastRequest.Remove(actor);
            foreach (int actor in currentActors)
                if (Compatible(actor) && peers.Add(actor)) { nextRequest = Time.unscaledTime; retryCount = 0; }
        }
        if (peers.Count > 0 && retryCount < 3 && Time.unscaledTime >= nextRequest)
        {
            net.Send(CoopProto.PhotonSnapshotRequest, new object[] { CoopProto.Magic, CoopProto.Version, scene }, true, false);
            nextRequest = Time.unscaledTime + 4f; retryCount++;
            Plugin.Log.LogInfo($"coop: requested scene snapshot ({scene}), attempt {retryCount}");
        }
        int budget = 2;
        foreach (int actor in new List<int>(transfers.Keys))
        {
            if (budget <= 0) break;
            var list = transfers[actor];
            int count = Math.Min(SnapshotBatch, list.Count);
            object[] batch = new object[count];
            for (int i = 0; i < count; i++) batch[i] = list[list.Count - 1 - i].Packet();
            if (net.SendToActor(CoopProto.PhotonSnapshot, new object[] { CoopProto.Magic, CoopProto.Version, scene, batch }, actor))
            {
                list.RemoveRange(list.Count - count, count); SnapshotChunksSent++; budget--;
                if (list.Count == 0) transfers.Remove(actor);
            }
            else transfers.Remove(actor);
        }
    }

    private bool Compatible(int actor) => net.Prop(actor, CoopProto.PropProto) is int v && v == CoopProto.Version;

    private void OnRemote(int actor, object[] data)
    {
        if (!BlockEdit.TryParse(data, out var edit) || edit.Scene != SceneManager.GetActiveScene().name) return;
        if (actor != CoopNet.LoopbackActor && edit.Actor != actor) return;
        if (actor == CoopNet.LoopbackActor)
            edit = new BlockEdit(edit.Scene, edit.X + LoopbackOffset, edit.Y, edit.Z, edit.State, edit.Name, edit.Revision, edit.Actor);
        Accept(edit);
    }

    private void Accept(in BlockEdit edit)
    {
        if (!journal.Merge(edit)) return;
        Received++;
        viewer.Set(edit.X, edit.Y, edit.Z, edit.Name);
        Queue(edit);
        Plugin.Log.LogInfo($"coop: remote block {edit.Name} from actor {edit.Actor} at ({edit.X}, {edit.Y}, {edit.Z}), revision {edit.Revision}");
    }

    private void Queue(in BlockEdit edit)
    {
        var key = (edit.X, edit.Y, edit.Z);
        if (pendingSet.Add(key)) pending.Enqueue(key);
    }

    private void OnSnapshotRequest(int actor, object[] data)
    {
        if (actor <= 0 || !Compatible(actor) || data.Length != 3 || !(data[0] is int magic) || magic != CoopProto.Magic || !(data[1] is int version) || version != CoopProto.Version ||
            data[2] as string != scene) return;
        if (lastRequest.TryGetValue(actor, out float last) && Time.unscaledTime - last < 3f) return;
        lastRequest[actor] = Time.unscaledTime;
        if (!transfers.ContainsKey(actor)) transfers[actor] = new List<BlockEdit>(journal.Values);
    }

    private void OnSnapshot(int actor, object[] data)
    {
        if (actor <= 0 || !Compatible(actor) || data.Length != 4 || !(data[0] is int magic) || magic != CoopProto.Magic || !(data[1] is int version) || version != CoopProto.Version ||
            data[2] as string != scene || !(data[3] is object[] batch) || batch.Length > SnapshotBatch) return;
        var parsed = new List<BlockEdit>(batch.Length);
        foreach (object item in batch)
        {
            if (!(item is object[] packet) || !BlockEdit.TryParse(packet, out var edit) || edit.Scene != scene) return;
            parsed.Add(edit);
        }
        SnapshotChunksReceived++;
        foreach (var edit in parsed) Accept(edit);
    }
}
