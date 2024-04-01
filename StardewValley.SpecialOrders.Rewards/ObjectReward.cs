using System.Collections.Generic;
using Netcode;
using Netcode.Validation;

namespace StardewValley.SpecialOrders.Rewards;

public class ObjectReward : OrderReward
{
	public NetString itemKey = new NetString("");

	public NetInt amount = new NetInt(0);

	[NotNetField]
	public Object objectInstance;

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(this.itemKey, "itemKey").AddField(this.amount, "amount");
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		this.itemKey.Value = order.Parse(data["Item"]);
		this.amount.Value = int.Parse(order.Parse(data["Amount"]));
		this.objectInstance = new Object(this.itemKey, this.amount);
	}

	public override void Grant()
	{
		Object i = new Object(this.itemKey.Value, this.amount.Value);
		Game1.player.addItemByMenuIfNecessary(i);
	}
}
