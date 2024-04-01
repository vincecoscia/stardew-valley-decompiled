using System.Collections;
using System.Collections.Generic;
using Netcode;
using StardewValley.Buildings;

namespace StardewValley.Network;

public class NetBuildingRef : INetObject<NetFields>, IEnumerable<Building>, IEnumerable
{
	private readonly NetString nameOfIndoors = new NetString();

	private readonly NetLocationRef location = new NetLocationRef();

	public NetFields NetFields { get; } = new NetFields("NetBuildingRef");


	public Building Value
	{
		get
		{
			string nameOfIndoors = this.nameOfIndoors.Get();
			if (nameOfIndoors == null)
			{
				return null;
			}
			if (this.location.Value == null)
			{
				return Game1.getFarm().getBuildingByName(nameOfIndoors);
			}
			return this.location.Value.getBuildingByName(nameOfIndoors);
		}
		set
		{
			if (value == null)
			{
				this.nameOfIndoors.Value = null;
				this.location.Value = null;
			}
			else
			{
				this.nameOfIndoors.Value = value.GetIndoorsName();
				this.location.Value = value.GetParentLocation();
			}
		}
	}

	public NetBuildingRef()
	{
		this.NetFields.SetOwner(this).AddField(this.nameOfIndoors, "nameOfIndoors").AddField(this.location.NetFields, "location.NetFields");
	}

	public IEnumerator<Building> GetEnumerator()
	{
		yield return this.Value;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}
