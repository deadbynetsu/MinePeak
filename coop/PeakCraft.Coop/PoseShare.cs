using PeakCraft.Link;
using UnityEngine;

namespace PeakCraft.Coop;

internal sealed class PoseShare
{
	private const float Interval = 1f / 15f;

	private readonly CoopNet net;

	private float next;

	public PoseShare(CoopNet net)
	{
		this.net = net;
	}

	public void Update(bool active, Vector3 feet, in Proto.GuestState guest)
	{
		if (active && !(Time.unscaledTime < next))
		{
			next = Time.unscaledTime + 1f / 15f;
			int num = (int)((((guest.flags & 8) != 0) ? 1u : 0u) | (uint)(((guest.flags & 0x10) != 0) ? 2 : 0)) | (((guest.flags & 0x80) != 0) ? 4 : 0);
			net.Send(172, new object[8] { 1346585345, feet.x, feet.y, feet.z, guest.yaw, guest.pitch, guest.walkDist, num }, reliable: false, cache: false);
		}
	}
}
