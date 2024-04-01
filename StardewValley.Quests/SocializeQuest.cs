using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;

namespace StardewValley.Quests;

public class SocializeQuest : Quest
{
	public readonly NetStringList whoToGreet = new NetStringList();

	[XmlElement("total")]
	public readonly NetInt total = new NetInt();

	public readonly NetDescriptionElementList parts = new NetDescriptionElementList();

	[XmlElement("objective")]
	public readonly NetDescriptionElementRef objective = new NetDescriptionElementRef();

	public SocializeQuest()
	{
		base.questType.Value = 5;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(this.whoToGreet, "whoToGreet").AddField(this.total, "total").AddField(this.parts, "parts")
			.AddField(this.objective, "objective");
	}

	public void loadQuestInfo()
	{
		if (this.whoToGreet.Count > 0)
		{
			return;
		}
		base.questTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:SocializeQuest.cs.13785");
		this.parts.Clear();
		this.parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:SocializeQuest.cs.13786", new DescriptionElement("Strings\\StringsFromCSFiles:SocializeQuest.cs." + base.random.Choose("13787", "13788", "13789"))));
		this.parts.Add("Strings\\StringsFromCSFiles:SocializeQuest.cs.13791");
		int curTotal = 0;
		foreach (KeyValuePair<string, CharacterData> entry in Game1.characterData)
		{
			string name = entry.Key;
			CharacterData data = entry.Value;
			if (data.IntroductionsQuest ?? (data.HomeRegion == "Town"))
			{
				curTotal++;
				if (data.SocialTab != SocialTabBehavior.AlwaysShown || (bool)base.dailyQuest)
				{
					this.whoToGreet.Add(name);
				}
			}
		}
		this.total.Value = curTotal;
		this.objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:SocializeQuest.cs.13802", (int)this.total - this.whoToGreet.Count, this.total.Value);
	}

	public override void reloadDescription()
	{
		if (base._questDescription == "")
		{
			this.loadQuestInfo();
		}
		if (this.parts.Count == 0 || this.parts == null)
		{
			return;
		}
		string descriptionBuilder = "";
		foreach (DescriptionElement a in this.parts)
		{
			descriptionBuilder += a.loadDescriptionElement();
		}
		base.questDescription = descriptionBuilder;
	}

	public override void reloadObjective()
	{
		this.loadQuestInfo();
		if (this.objective.Value == null && this.whoToGreet.Count > 0)
		{
			this.objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:SocializeQuest.cs.13802", (int)this.total - this.whoToGreet.Count, this.total.Value);
		}
		if (this.objective.Value != null)
		{
			base.currentObjective = this.objective.Value.loadDescriptionElement();
		}
	}

	public override bool checkIfComplete(NPC npc = null, int number1 = -1, int number2 = -1, Item item = null, string monsterName = null, bool probe = false)
	{
		this.loadQuestInfo();
		if (npc != null && !probe && this.whoToGreet.Remove(npc.Name))
		{
			Game1.dayTimeMoneyBox.moneyDial.animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(387, 497, 3, 8), 800f, 1, 0, Game1.dayTimeMoneyBox.position + new Vector2(228f, 244f), flicker: false, flipped: false, 1f, 0.01f, Color.White, 4f, 0.3f, 0f, 0f)
			{
				scaleChangeChange = -0.012f
			});
			Game1.dayTimeMoneyBox.pingQuest(this);
		}
		if (this.whoToGreet.Count == 0 && !base.completed)
		{
			if (!probe)
			{
				foreach (string s in Game1.player.friendshipData.Keys)
				{
					if (Game1.player.friendshipData[s].Points < 2729)
					{
						Game1.player.changeFriendship(100, Game1.getCharacterFromName(s));
					}
				}
				this.questComplete();
			}
			return true;
		}
		if (!probe)
		{
			this.objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:SocializeQuest.cs.13802", (int)this.total - this.whoToGreet.Count, this.total.Value);
		}
		return false;
	}
}
