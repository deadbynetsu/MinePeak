namespace PeakCraft.Coop;

internal static class CoopProto
{
	public const int Magic = 1346585345;

	public const int Version = 2;

	public const uint LinkEvString = 6u;

	public const uint LinkEvBlockSet = 7u;

	public const uint LinkInSetBlock = 9u;

	public const uint LinkRenTextureFlags = 4u;

	public const byte PhotonBlockSet = 171;

	public const byte PhotonPose = 172;
	public const byte PhotonSnapshotRequest = 173;
	public const byte PhotonSnapshot = 174;

	public const string PropProto = "pcc.v";

	public const string PropMode = "pcc.mode";

	public const string PropSkin = "pcc.skin";

	public const string PropSlim = "pcc.slim";

	public const string PropHeld = "pcc.held";

	public const uint LinkInOpenSettings = 10u;
}
