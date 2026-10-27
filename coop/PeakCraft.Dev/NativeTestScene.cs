using UnityEngine;
using PeakCraft.Player;

namespace PeakCraft.Dev;

/// <summary>Opt-in harness fixtures. Never created by normal gameplay.</summary>
internal static class NativeTestScene
{
    private static GameObject? wall;
    private static Mesh? mesh;

    public static void Wall(bool gpuMesh)
    {
        Clear();
        var c = Character.localCharacter;
        if (c == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(c.data.lookDirection, Vector3.up).normalized;
        if (forward.sqrMagnitude < .5f) forward = Vector3.forward;
        wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "PeakCraft synthetic thin wall";
        wall.layer = LayerMask.NameToLayer("Map");
        wall.transform.position = Follower.PeakFeet(c) + forward * 3f + Vector3.up * 1.5f;
        wall.transform.rotation = Quaternion.LookRotation(forward);
        wall.transform.localScale = new Vector3(3f, 3f, .08f);
        if (gpuMesh)
        {
            Object.DestroyImmediate(wall.GetComponent<BoxCollider>());
            mesh = Object.Instantiate(wall.GetComponent<MeshFilter>().sharedMesh);
            wall.GetComponent<MeshFilter>().sharedMesh = mesh;
            wall.AddComponent<MeshCollider>().sharedMesh = mesh;
            mesh.UploadMeshData(true);
        }
        Plugin.Instance.Collision.Reset("synthetic wall fixture");
        Plugin.Log.LogInfo($"native test: {(gpuMesh ? "GPU mesh" : "box")} wall center {wall.transform.position:F3}; normal {forward:F3}; thickness 0.08m");
    }

    public static void Clear()
    {
        if (wall != null) Object.Destroy(wall);
        if (mesh != null) Object.Destroy(mesh);
        wall = null; mesh = null;
    }

    public static void FindWalls()
    {
        var c = Character.localCharacter;
        if (c == null) return;
        int count = 0;
        foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (collider.isTrigger || !collider.enabled || collider.GetComponentInParent<Character>() != null) continue;
            var bounds = collider.bounds;
            if (bounds.size.y < 2f || (bounds.center - c.Center).sqrMagnitude > 900f) continue;
            Plugin.Log.LogInfo($"native test: wall candidate '{collider.name}' {collider.GetType().Name} center {bounds.center:F2} size {bounds.size:F2} layer {LayerMask.LayerToName(collider.gameObject.layer)}");
            if (++count >= 80) break;
        }
    }
}
