using Netcode;

namespace StardewValley.Network;

public class NetFarmerRoot : NetRoot<Farmer>
{
	public NetFarmerRoot()
	{
		base.Serializer = SaveGame.farmerSerializer;
	}

	public NetFarmerRoot(Farmer value)
		: base(value)
	{
		base.Serializer = SaveGame.farmerSerializer;
	}

	public override NetRoot<Farmer> Clone()
	{
		NetRoot<Farmer> result = base.Clone();
		if (Game1.serverHost != null && result.Value != null)
		{
			result.Value.teamRoot = Game1.serverHost.Value.teamRoot;
		}
		return result;
	}
}
