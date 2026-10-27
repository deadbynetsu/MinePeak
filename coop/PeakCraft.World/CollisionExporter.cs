using System;
using System.Collections.Generic;
using System.Diagnostics;
using PeakCraft.Link;
using UnityEngine;

namespace PeakCraft.World;

internal sealed class CollisionExporter
{
	private struct Span
	{
		public float top;

		public float bottom;
	}

	private struct Exported
	{
		public int ryMin;

		public int ryMax;

		public float time;

		public uint geometryRevision;

		public bool pendingGeometry;
	}

	public const int RegionSize = 8;

	private const float Step = 0.5f;

	private const int Cells = 16;

	private const int Verts = 17;

	private const int MaxSpans = 8;

	private const int Radius = 3;

	private const int WindowBelow = 3;

	private const int WindowAbove = 2;

	private const float JoinTolerance = 1f;

	private const float Big = 1E+09f;

	private const float NearRefreshSeconds = 6f;

	private const float FarRefreshSeconds = 45f;

	private const double BudgetMs = 2.5;

	private const long MaxBacklogBytes = 12582912L;

	private readonly PeakLink link;

	private readonly int mask;

	private readonly ColliderGeometry geometry = new ColliderGeometry();

	private readonly Action<Vector3, Vector3, Vector3> exactEmitter;

	private readonly Span[] spans = new Span[2312];

	private readonly byte[] spanCount = new byte[289];

	private readonly Dictionary<long, Exported> exported = new Dictionary<long, Exported>();

	private readonly List<long> scratchKeys = new List<long>();

	private readonly HashSet<ulong> cellPolygons = new HashSet<ulong>();

	private readonly Stopwatch watch = new Stopwatch();

	private List<Proto.ColTri>[] tris = Array.Empty<List<Proto.ColTri>>();

	private ulong[][] occupancy = Array.Empty<ulong[]>();

	private byte[] payload = new byte[65536];

	private int windowRyMin;

	private int windowRyMax;

	private float lastStats;

	private int statColumns;

	private int statTris;

	private int statRays;

	private int statExactTris;

	public uint Epoch { get; private set; } = 1u;

	public CollisionExporter(PeakLink link)
	{
		this.link = link;
		mask = LayerMask.GetMask("Terrain", "Map", "Default");
		exactEmitter = AddExactTriangle;
	}

	public unsafe void Reset(string why)
	{
		Epoch++;
		exported.Clear();
		geometry.Clear();
		uint epoch = Epoch;
		link.WriteCollision(1u, &epoch, 4u);
		Plugin.Log.LogInfo((object)$"collision: cleared, epoch {Epoch} ({why})");
	}

	public void Update(double centreX, double centreY, double centreZ)
	{
		if (link.CollisionBacklog <= 12582912)
		{
			int crx = FloorDiv((int)Math.Floor(centreX), 8);
			int num = FloorDiv((int)Math.Floor(centreY), 8);
			int crz = FloorDiv((int)Math.Floor(centreZ), 8);
			windowRyMin = num - 3;
			windowRyMax = num + 2;
			watch.Restart();
			Evict(crx, crz);
			float unscaledTime = Time.unscaledTime;
			int bestRx;
			int bestRz;
			while (watch.Elapsed.TotalMilliseconds < 2.5 && PickColumn(crx, num, crz, unscaledTime, out bestRx, out bestRz) && ExportColumn(bestRx, bestRz))
			{
			}
			if (unscaledTime - lastStats > 10f && statColumns > 0)
			{
				Plugin.Log.LogInfo((object)$"collision: {statColumns} region columns sent in the last {unscaledTime - lastStats:F0} s ({statTris} triangles, {statRays} ray casts, {statExactTris} actual collider triangles); meshes {geometry.ReadyMeshes} ready/{geometry.FailedMeshes} unavailable, {exported.Count} columns live, epoch {Epoch}");
				lastStats = unscaledTime;
				statColumns = (statTris = (statRays = 0));
				statExactTris = 0;
			}
		}
	}

	private static long Key(int rx, int rz)
	{
		return ((long)rx << 32) | (uint)rz;
	}

	private static int FloorDiv(int a, int b)
	{
		return ((a >= 0) ? a : (a - b + 1)) / b;
	}

	private bool PickColumn(int crx, int cry, int crz, float now, out int bestRx, out int bestRz)
	{
		bestRx = (bestRz = 0);
		float num = float.MaxValue;
		for (int i = -3; i <= 3; i++)
		{
			for (int j = -3; j <= 3; j++)
			{
				int num2 = Math.Max(Math.Abs(j), Math.Abs(i));
				float num3;
				if (!exported.TryGetValue(Key(crx + j, crz + i), out var value))
				{
					num3 = num2;
				}
				else if (value.pendingGeometry && value.geometryRevision != geometry.Revision)
				{
					// A GPU-only collider mesh became available after this column was sampled.
					// Refresh it before the normal six-second near-column timer expires.
					num3 = num2 - 10f;
				}
				else if (value.ryMin > cry - 1 || value.ryMax < cry + 1)
				{
					num3 = num2;
				}
				else if (value.ryMin != windowRyMin && Math.Abs(value.ryMin - windowRyMin) >= 2)
				{
					num3 = 50 + num2;
				}
				else
				{
					float num4 = now - value.time;
					float num5 = ((num2 <= 1) ? 6f : 45f);
					if (num4 < num5)
					{
						continue;
					}
					num3 = (float)(100 + num2) - num4 * 0.01f;
				}
				if (num3 < num)
				{
					num = num3;
					bestRx = crx + j;
					bestRz = crz + i;
				}
			}
		}
		return num != float.MaxValue;
	}

	private void Evict(int crx, int crz)
	{
		scratchKeys.Clear();
		foreach (KeyValuePair<long, Exported> item in this.exported)
		{
			int num = (int)(item.Key >> 32);
			int num2 = (int)item.Key;
			if (Math.Abs(num - crx) > 5 || Math.Abs(num2 - crz) > 5)
			{
				scratchKeys.Add(item.Key);
			}
		}
		foreach (long scratchKey in scratchKeys)
		{
			Exported exported = this.exported[scratchKey];
			int rx = (int)(scratchKey >> 32);
			int rz = (int)scratchKey;
			for (int i = exported.ryMin; i <= exported.ryMax; i++)
			{
				if (!SendRegion(rx, i, rz, null, null))
				{
					return;
				}
			}
			this.exported.Remove(scratchKey);
		}
	}

	private unsafe bool ExportColumn(int rx, int rz)
	{
		int num = windowRyMin;
		int num2 = windowRyMax;
		int num3 = num2 - num + 1;
		if (tris.Length != num3)
		{
			tris = new List<Proto.ColTri>[num3];
			occupancy = new ulong[num3][];
			for (int i = 0; i < num3; i++)
			{
				tris[i] = new List<Proto.ColTri>(1024);
				occupancy[i] = new ulong[4096];
			}
		}
		for (int j = 0; j < num3; j++)
		{
			tris[j].Clear();
			Array.Clear(occupancy[j], 0, occupancy[j].Length);
		}
		float yTop = (float)((double)((num2 + 1) * 8) - Coords.YOffset);
		float yBottom = (float)((double)(num * 8) - Coords.YOffset);
		bool queriesHitBackfaces = Physics.queriesHitBackfaces;
		try
		{
			for (int k = 0; k < 17; k++)
			{
				for (int l = 0; l < 17; l++)
				{
					float ux = (float)(rx * 8) + (float)l * 0.5f;
					float uz = 0f - ((float)(rz * 8) + (float)k * 0.5f);
					fixed (Span* output = &spans[(k * 17 + l) * 8])
					{
						spanCount[k * 17 + l] = (byte)SampleColumn(ux, uz, yTop, yBottom, output);
					}
				}
			}
		}
		finally
		{
			Physics.queriesHitBackfaces = queriesHitBackfaces;
		}
		for (int m = 0; m < 16; m++)
		{
			for (int n = 0; n < 16; n++)
			{
				Triangulate(rx, rz, n, m, num, num2);
				Fill(n, m, num, num2);
			}
		}
		bool pendingGeometry = geometry.AppendColumn(rx, rz, yBottom, yTop, mask, exactEmitter);
		if (exported.TryGetValue(Key(rx, rz), out var value))
		{
			for (int num4 = value.ryMin; num4 <= value.ryMax; num4++)
			{
				if ((num4 < num || num4 > num2) && !SendRegion(rx, num4, rz, null, null))
				{
					return false;
				}
			}
		}
		for (int num5 = 0; num5 < num3; num5++)
		{
			if (!SendRegion(rx, num + num5, rz, tris[num5], occupancy[num5]))
			{
				return false;
			}
			statTris += tris[num5].Count;
		}
		exported[Key(rx, rz)] = new Exported
		{
			ryMin = num,
			ryMax = num2,
			time = Time.unscaledTime,
			geometryRevision = geometry.Revision,
			pendingGeometry = pendingGeometry
		};
		statColumns++;
		return true;
	}

	private static bool Dynamic(Collider collider)
	{
		Rigidbody attachedRigidbody = collider.attachedRigidbody;
		if (attachedRigidbody != null)
		{
			return !attachedRigidbody.isKinematic;
		}
		return false;
	}

	private unsafe int SampleColumn(float ux, float uz, float yTop, float yBottom, Span* output)
	{
		int num = 0;
		bool flag = InsideSolid(new Vector3(ux, yTop, uz));
		float top = 1E+09f;
		float highY = yTop;
		float num2 = yTop;
		for (int i = 0; i < 24; i++)
		{
			if (num >= 7)
			{
				break;
			}
			Physics.queriesHitBackfaces = false;
			statRays++;
			if (!Physics.Raycast(new Vector3(ux, num2, uz), Vector3.down, out var hitInfo, num2 - yBottom, mask, QueryTriggerInteraction.Ignore))
			{
				break;
			}
			float y = hitInfo.point.y;
			num2 = y - 0.02f;
			if (!Dynamic(hitInfo.collider))
			{
				float ceiling;
				if (!flag)
				{
					flag = true;
					top = y;
				}
				else if (GapIsAir(ux, uz, y, highY, out ceiling))
				{
					output[num++] = new Span
					{
						top = top,
						bottom = ceiling
					};
					top = y;
				}
				highY = y;
			}
		}
		if (flag)
		{
			output[num++] = new Span
			{
				top = top,
				bottom = -1E+09f
			};
		}
		return num;
	}

	private bool GapIsAir(float ux, float uz, float lowY, float highY, out float ceiling)
	{
		ceiling = highY;
		float num = highY - lowY - 0.04f;
		if (num < 0.1f)
		{
			return false;
		}
		Vector3 origin = new Vector3(ux, lowY + 0.02f, uz);
		statRays += 2;
		Physics.queriesHitBackfaces = false;
		bool flag = Physics.Raycast(origin, Vector3.up, out var hitInfo, num, mask, QueryTriggerInteraction.Ignore);
		Physics.queriesHitBackfaces = true;
		bool flag2 = Physics.Raycast(origin, Vector3.up, out var hitInfo2, num + 0.03f, mask, QueryTriggerInteraction.Ignore);
		if (flag && (!flag2 || hitInfo.distance <= hitInfo2.distance + 0.01f))
		{
			ceiling = hitInfo.point.y;
			return true;
		}
		return false;
	}

	private bool InsideSolid(Vector3 point)
	{
		statRays += 2;
		Physics.queriesHitBackfaces = false;
		bool flag = Physics.Raycast(point, Vector3.up, out var hitInfo, 400f, mask, QueryTriggerInteraction.Ignore);
		Physics.queriesHitBackfaces = true;
		if (Physics.Raycast(point, Vector3.up, out var hitInfo2, 400f, mask, QueryTriggerInteraction.Ignore) && (!flag || hitInfo2.distance < hitInfo.distance - 0.01f))
		{
			return true;
		}
		return Physics.CheckSphere(point, 0.05f, mask, QueryTriggerInteraction.Ignore);
	}

	private unsafe void Triangulate(int rx, int rz, int i, int j, int ryMin, int ryMax)
	{
		int* ptr = stackalloc int[4]
		{
			i,
			i + 1,
			i + 1,
			i
		};
		int* ptr2 = stackalloc int[4]
		{
			j,
			j,
			j + 1,
			j + 1
		};
		int* ptr3 = stackalloc int[4];
		int* ptr4 = stackalloc int[4];
		for (int k = 0; k < 4; k++)
		{
			ptr3[k] = ptr2[k] * 17 + ptr[k];
			ptr4[k] = spanCount[ptr3[k]];
		}
		if (*ptr4 + ptr4[1] + ptr4[2] + ptr4[3] == 0)
		{
			return;
		}
		bool* ptr5 = stackalloc bool[32];
		int* ptr6 = stackalloc int[4];
		float* ptr7 = stackalloc float[4];
		cellPolygons.Clear();
		for (int l = 0; l < 32; l++)
		{
			ptr5[l] = false;
		}
		while (true)
		{
			int num = -1;
			int num2 = -1;
			float num3 = -1E+09f;
			for (int m = 0; m < 4; m++)
			{
				for (int n = 0; n < ptr4[m]; n++)
				{
					float top = spans[ptr3[m] * 8 + n].top;
					if (!ptr5[m * 8 + n] && top < 1E+09f && top > num3)
					{
						num3 = top;
						num = m;
						num2 = n;
					}
				}
			}
			if (num < 0)
			{
				break;
			}
			int num4 = 0;
			for (int num5 = 0; num5 < 4; num5++)
			{
				ptr6[num5] = -1;
				if (num5 == num)
				{
					ptr6[num5] = num2;
				}
				else
				{
					float num6 = 1f;
					for (int num7 = 0; num7 < ptr4[num5]; num7++)
					{
						float top2 = spans[ptr3[num5] * 8 + num7].top;
						float num8 = Math.Abs(top2 - num3);
						if (!ptr5[num5 * 8 + num7] && top2 < 1E+09f && num8 <= num6)
						{
							num6 = num8;
							ptr6[num5] = num7;
						}
					}
				}
				if (ptr6[num5] >= 0)
				{
					ptr5[num5 * 8 + ptr6[num5]] = true;
					ptr7[num5] = spans[ptr3[num5] * 8 + ptr6[num5]].top;
					num4++;
				}
				else
				{
					ptr7[num5] = float.NaN;
				}
			}
			if (num4 < 4)
			{
				AnchorWalls(ptr3, ptr4, ptr6, ptr7, num3);
			}
			EmitPolygon(rx, rz, ptr, ptr2, ptr7, ryMin, ryMax);
		}
		for (int num9 = 0; num9 < 32; num9++)
		{
			ptr5[num9] = false;
		}
		while (true)
		{
			int num10 = -1;
			int num11 = -1;
			float num12 = -1E+09f;
			for (int num13 = 0; num13 < 4; num13++)
			{
				for (int num14 = 0; num14 < ptr4[num13]; num14++)
				{
					float bottom = spans[ptr3[num13] * 8 + num14].bottom;
					if (!ptr5[num13 * 8 + num14] && bottom > num12)
					{
						num12 = bottom;
						num10 = num13;
						num11 = num14;
					}
				}
			}
			if (num10 < 0)
			{
				break;
			}
			for (int num15 = 0; num15 < 4; num15++)
			{
				int num16 = ((num15 == num10) ? num11 : (-1));
				if (num15 != num10)
				{
					float num17 = 1f;
					for (int num18 = 0; num18 < ptr4[num15]; num18++)
					{
						float bottom2 = spans[ptr3[num15] * 8 + num18].bottom;
						float num19 = Math.Abs(bottom2 - num12);
						if (!ptr5[num15 * 8 + num18] && bottom2 > -1E+09f && num19 <= num17)
						{
							num17 = num19;
							num16 = num18;
						}
					}
				}
				if (num16 >= 0)
				{
					ptr5[num15 * 8 + num16] = true;
					ptr7[num15] = spans[ptr3[num15] * 8 + num16].bottom;
				}
				else
				{
					ptr7[num15] = float.NaN;
				}
			}
			EmitPolygon(rx, rz, ptr, ptr2, ptr7, ryMin, ryMax);
		}
	}

	private unsafe void AnchorWalls(int* column, int* n, int* group, float* ys, float height)
	{
		float num = -1E+09f;
		for (int i = 0; i < 4; i++)
		{
			if (group[i] >= 0)
			{
				num = Math.Max(num, spans[column[i] * 8 + group[i]].bottom);
			}
		}
		for (int j = 0; j < 4; j++)
		{
			if (group[j] >= 0)
			{
				continue;
			}
			float num2 = -1E+09f;
			bool flag = false;
			for (int k = 0; k < n[j]; k++)
			{
				Span span = spans[column[j] * 8 + k];
				if (span.bottom < height - 0.05f && span.top > height + 0.05f)
				{
					ys[j] = Math.Min(span.top, height + 4f);
					flag = true;
					break;
				}
				if (span.top <= height + 0.05f && span.top > num2)
				{
					num2 = span.top;
				}
			}
			if (!flag && num2 > -1E+09f && num <= num2 + 0.3f)
			{
				ys[j] = num2;
			}
		}
	}

	private unsafe void EmitPolygon(int rx, int rz, int* ci, int* cj, float* ys, int ryMin, int ryMax)
	{
		int num = 0;
		ulong num2 = 0uL;
		for (int i = 0; i < 4; i++)
		{
			bool flag = !float.IsNaN(ys[i]);
			num += (flag ? 1 : 0);
			num2 = (num2 << 16) | (ulong)(flag ? ((int)Math.Round(ys[i] * 16f) & 0xFFFF) : 65535);
		}
		if (num < 3 || !cellPolygons.Add(num2))
		{
			return;
		}
		float* ptr = stackalloc float[4];
		float* ptr2 = stackalloc float[4];
		float* ptr3 = stackalloc float[4];
		int num3 = 0;
		for (int j = 0; j < 4; j++)
		{
			if (!float.IsNaN(ys[j]))
			{
				ptr[num3] = (float)(rx * 8) + (float)ci[j] * 0.5f;
				ptr2[num3] = (float)((double)ys[j] + Coords.YOffset);
				ptr3[num3] = (float)(rz * 8) + (float)cj[j] * 0.5f;
				num3++;
			}
		}
		if (num3 == 3)
		{
			AddTriangle(ptr, ptr2, ptr3, 0, 1, 2, ryMin, ryMax);
		}
		else if (Math.Abs(*ptr2 - ptr2[2]) <= Math.Abs(ptr2[1] - ptr2[3]))
		{
			AddTriangle(ptr, ptr2, ptr3, 0, 1, 2, ryMin, ryMax);
			AddTriangle(ptr, ptr2, ptr3, 0, 2, 3, ryMin, ryMax);
		}
		else
		{
			AddTriangle(ptr, ptr2, ptr3, 0, 1, 3, ryMin, ryMax);
			AddTriangle(ptr, ptr2, ptr3, 1, 2, 3, ryMin, ryMax);
		}
	}

	private unsafe void AddTriangle(float* x, float* y, float* z, int a, int b, int c, int ryMin, int ryMax)
	{
		Proto.ColTri item = default;
		ref float v = ref item.v[0];
		v = x[a];
		item.v[1] = y[a];
		item.v[2] = z[a];
		item.v[3] = x[b];
		item.v[4] = y[b];
		item.v[5] = z[b];
		item.v[6] = x[c];
		item.v[7] = y[c];
		item.v[8] = z[c];
		if (!CollisionRegionSpan.TryGet(Mathf.Min(y[a], Mathf.Min(y[b], y[c])), Mathf.Max(y[a], Mathf.Max(y[b], y[c])), ryMin, ryMax, out int first, out int last)) return;
		for (int ry = first; ry <= last; ry++) tris[ry - ryMin].Add(item);
	}

	private unsafe void AddExactTriangle(Vector3 a, Vector3 b, Vector3 c)
	{
		float sum = a.x + a.y + a.z + b.x + b.y + b.z + c.x + c.y + c.z;
		if (float.IsNaN(sum) || float.IsInfinity(sum)) return;
		if (Vector3.Cross(b - a, c - a).sqrMagnitude < 0.0000000001f) return;
		float minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y)) + (float)Coords.YOffset;
		float maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y)) + (float)Coords.YOffset;
		if (!CollisionRegionSpan.TryGet(minY, maxY, windowRyMin, windowRyMax, out int first, out int last)) return;
		Proto.ColTri triangle = default;
		triangle.v[0] = a.x; triangle.v[1] = a.y + (float)Coords.YOffset; triangle.v[2] = -a.z;
		triangle.v[3] = b.x; triangle.v[4] = b.y + (float)Coords.YOffset; triangle.v[5] = -b.z;
		triangle.v[6] = c.x; triangle.v[7] = c.y + (float)Coords.YOffset; triangle.v[8] = -c.z;
		// A large vertical face must be present in every intersected Y region. Assigning
		// it only to its centroid made walls disappear near region boundaries.
		for (int ry = first; ry <= last; ry++)
		{
			List<Proto.ColTri> target = tris[ry - windowRyMin];
			if (target.Count >= 32768) continue;
			target.Add(triangle);
			statExactTris++;
		}
	}

	public void ProbeAhead()
	{
		Camera camera = Camera.main;
		if (camera == null) return;
		if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 12f, mask, QueryTriggerInteraction.Ignore))
		{
			Collider c = hit.collider;
			MeshCollider mesh = c as MeshCollider;
			Plugin.Log.LogInfo($"collision probe: '{c.name}' layer {LayerMask.LayerToName(c.gameObject.layer)} type {c.GetType().Name}, hit {hit.point:F3}, distance {hit.distance:F3}, mesh readable {(mesh != null && mesh.sharedMesh != null && mesh.sharedMesh.isReadable)}; cache {geometry.ReadyMeshes} ready/{geometry.FailedMeshes} unavailable");
		}
		else Plugin.Log.LogInfo("collision probe: no world collider ahead within 12 metres");
	}

	private void Fill(int i, int j, int ryMin, int ryMax)
	{
		int num = j * 17 + i;
		int val = ryMin * 64;
		int num2 = (ryMax + 1) * 64 - 1;
		for (int k = 0; k < spanCount[num]; k++)
		{
			Span span = spans[num * 8 + k];
			double num3 = ((span.top >= 1E+09f) ? ((double)num2 / 8.0 + 1.0) : ((double)span.top + Coords.YOffset));
			int num4 = Math.Max((int)Math.Floor(Math.Max((double)span.bottom + Coords.YOffset, (span.top >= 1E+09f) ? (-1000000000.0) : (num3 - 1.0)) * 8.0), val);
			int num5 = Math.Min((int)Math.Ceiling(num3 * 8.0) - 1, num2);
			for (int l = num4; l <= num5; l++)
			{
				int num6 = FloorDiv(l, 64);
				int num7 = l - num6 * 64;
				ulong[] array = occupancy[num6 - ryMin];
				int num8 = num7 >> 3;
				int num9 = num7 & 7;
				for (int m = 0; m < 4; m++)
				{
					int num10 = j * 4 + m;
					int num11 = num10 >> 3;
					int num12 = num10 & 7;
					for (int n = 0; n < 4; n++)
					{
						int num13 = i * 4 + n;
						int num14 = num13 >> 3;
						int num15 = num13 & 7;
						array[(num14 + 8 * (num11 + 8 * num8)) * 8 + num9] |= (ulong)(1L << num12 * 8 + num15);
					}
				}
			}
		}
	}

	private unsafe bool SendRegion(int rx, int ry, int rz, List<Proto.ColTri>? triangles, ulong[]? region)
	{
		Proto.ColRegionHdr colRegionHdr = new Proto.ColRegionHdr
		{
			minX = rx * 8,
			minY = ry * 8,
			minZ = rz * 8,
			maxX = rx * 8 + 8 - 1,
			maxY = ry * 8 + 8 - 1,
			maxZ = rz * 8 + 8 - 1,
			epoch = Epoch
		};
		int num = triangles?.Count ?? 0;
		int num2 = sizeof(Proto.ColRegionHdr) + Math.Max(num * sizeof(Proto.ColTri), 512 * sizeof(Proto.ColBlock));
		if (payload.Length < num2)
		{
			payload = new byte[num2];
		}
		fixed (byte* ptr = payload)
		{
			colRegionHdr.count = (uint)num;
			*(Proto.ColRegionHdr*)ptr = colRegionHdr;
			Proto.ColTri* ptr2 = (Proto.ColTri*)(ptr + sizeof(Proto.ColRegionHdr));
			for (int i = 0; i < num; i++)
			{
				ptr2[i] = triangles[i];
			}
			if (!link.WriteCollision(3u, ptr, (uint)(sizeof(Proto.ColRegionHdr) + num * sizeof(Proto.ColTri))))
			{
				return false;
			}
			uint num3 = 0u;
			Proto.ColBlock* ptr3 = (Proto.ColBlock*)(ptr + sizeof(Proto.ColRegionHdr));
			if (region != null)
			{
				for (int j = 0; j < 512; j++)
				{
					ulong num4 = 0uL;
					for (int k = 0; k < 8; k++)
					{
						num4 |= region[j * 8 + k];
					}
					if (num4 != 0L)
					{
						Proto.ColBlock* ptr4 = ptr3 + num3++;
						ptr4->x = rx * 8 + (j & 7);
						ptr4->z = rz * 8 + ((j >> 3) & 7);
						ptr4->y = ry * 8 + (j >> 6);
						ptr4->pad = 0u;
						for (int l = 0; l < 8; l++)
						{
							ptr4->bits[l] = region[j * 8 + l];
						}
					}
				}
			}
			colRegionHdr.count = num3;
			*(Proto.ColRegionHdr*)ptr = colRegionHdr;
			return link.WriteCollision(2u, ptr, (uint)(sizeof(Proto.ColRegionHdr) + num3 * sizeof(Proto.ColBlock)));
		}
	}
}
