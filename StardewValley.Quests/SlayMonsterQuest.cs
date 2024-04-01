using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Monsters;

namespace StardewValley.Quests;

public class SlayMonsterQuest : Quest
{
	public string targetMessage;

	[XmlElement("monsterName")]
	public readonly NetString monsterName = new NetString();

	[XmlElement("target")]
	public readonly NetString target = new NetString();

	[XmlElement("monster")]
	public readonly NetRef<Monster> monster = new NetRef<Monster>();

	[XmlElement("numberToKill")]
	public readonly NetInt numberToKill = new NetInt();

	[XmlElement("reward")]
	public readonly NetInt reward = new NetInt();

	[XmlElement("numberKilled")]
	public readonly NetInt numberKilled = new NetInt();

	public readonly NetDescriptionElementList parts = new NetDescriptionElementList();

	public readonly NetDescriptionElementList dialogueparts = new NetDescriptionElementList();

	[XmlElement("objective")]
	public readonly NetDescriptionElementRef objective = new NetDescriptionElementRef();

	public SlayMonsterQuest()
	{
		base.questType.Value = 4;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(this.parts, "parts").AddField(this.dialogueparts, "dialogueparts").AddField(this.objective, "objective")
			.AddField(this.monsterName, "monsterName")
			.AddField(this.target, "target")
			.AddField(this.monster, "monster")
			.AddField(this.numberToKill, "numberToKill")
			.AddField(this.reward, "reward")
			.AddField(this.numberKilled, "numberKilled");
	}

	public void loadQuestInfo()
	{
		for (int i = 0; i < base.random.Next(1, 100); i++)
		{
			base.random.Next();
		}
		if (this.target.Value != null && this.monster != null)
		{
			return;
		}
		base.questTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13696");
		List<string> possibleMonsters = new List<string>();
		int mineLevel = Utility.GetAllPlayerDeepestMineLevel();
		if (mineLevel < 39)
		{
			possibleMonsters.Add("Green Slime");
			if (mineLevel > 10)
			{
				possibleMonsters.Add("Rock Crab");
			}
			if (mineLevel > 30)
			{
				possibleMonsters.Add("Duggy");
			}
		}
		else if (mineLevel < 79)
		{
			possibleMonsters.Add("Frost Jelly");
			if (mineLevel > 70)
			{
				possibleMonsters.Add("Skeleton");
			}
			possibleMonsters.Add("Dust Spirit");
		}
		else
		{
			possibleMonsters.Add("Sludge");
			possibleMonsters.Add("Ghost");
			possibleMonsters.Add("Lava Crab");
			possibleMonsters.Add("Squid Kid");
		}
		int num;
		if (this.monsterName.Value != null)
		{
			num = ((this.numberToKill.Value == 0) ? 1 : 0);
			if (num == 0)
			{
				goto IL_0125;
			}
		}
		else
		{
			num = 1;
		}
		this.monsterName.Value = base.random.ChooseFrom(possibleMonsters);
		goto IL_0125;
		IL_0125:
		if (this.monsterName.Value == "Frost Jelly" || this.monsterName.Value == "Sludge")
		{
			this.monster.Value = new Monster("Green Slime", Vector2.Zero);
			this.monster.Value.Name = this.monsterName.Value;
		}
		else
		{
			this.monster.Value = new Monster(this.monsterName.Value, Vector2.Zero);
		}
		if (num != 0)
		{
			switch (this.monsterName.Value)
			{
			case "Green Slime":
				this.numberToKill.Value = base.random.Next(4, 11);
				this.numberToKill.Value = (int)this.numberToKill - (int)this.numberToKill % 2;
				this.reward.Value = (int)this.numberToKill * 60;
				break;
			case "Rock Crab":
				this.numberToKill.Value = base.random.Next(2, 6);
				this.reward.Value = (int)this.numberToKill * 75;
				break;
			case "Duggy":
				this.parts.Clear();
				this.parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13711", this.numberToKill.Value));
				this.target.Value = "Clint";
				this.numberToKill.Value = base.random.Next(2, 4);
				this.reward.Value = (int)this.numberToKill * 150;
				break;
			case "Frost Jelly":
				this.numberToKill.Value = base.random.Next(4, 11);
				this.numberToKill.Value = (int)this.numberToKill - (int)this.numberToKill % 2;
				this.reward.Value = (int)this.numberToKill * 85;
				break;
			case "Ghost":
				this.numberToKill.Value = base.random.Next(2, 4);
				this.reward.Value = (int)this.numberToKill * 250;
				break;
			case "Sludge":
				this.numberToKill.Value = base.random.Next(4, 11);
				this.numberToKill.Value = (int)this.numberToKill - (int)this.numberToKill % 2;
				this.reward.Value = (int)this.numberToKill * 125;
				break;
			case "Lava Crab":
				this.numberToKill.Value = base.random.Next(2, 6);
				this.reward.Value = (int)this.numberToKill * 180;
				break;
			case "Squid Kid":
				this.numberToKill.Value = base.random.Next(1, 3);
				this.reward.Value = (int)this.numberToKill * 350;
				break;
			case "Skeleton":
				this.numberToKill.Value = base.random.Next(6, 12);
				this.reward.Value = (int)this.numberToKill * 100;
				break;
			case "Dust Spirit":
				this.numberToKill.Value = base.random.Next(10, 21);
				this.reward.Value = (int)this.numberToKill * 60;
				break;
			default:
				this.numberToKill.Value = base.random.Next(3, 7);
				this.reward.Value = (int)this.numberToKill * 120;
				break;
			}
		}
		switch (this.monsterName.Value)
		{
		case "Green Slime":
		case "Frost Jelly":
		case "Sludge":
			this.parts.Clear();
			this.parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13723", this.numberToKill.Value, this.monsterName.Value.Equals("Frost Jelly") ? new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13725") : (this.monsterName.Value.Equals("Sludge") ? new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13727") : new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13728"))));
			this.target.Value = "Lewis";
			this.dialogueparts.Clear();
			this.dialogueparts.Add("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13730");
			if (base.random.NextBool())
			{
				this.dialogueparts.Add("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13731");
				this.dialogueparts.Add("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs." + base.random.Choose("13732", "13733"));
				this.dialogueparts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13734", new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs." + base.random.Choose("13735", "13736")), new DescriptionElement("Strings\\StringsFromCSFiles:Dialogue.cs." + base.random.Choose<string>("795", "796", "797", "798", "799", "800", "801", "802", "803", "804", "805", "806", "807", "808", "809", "810")), new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs." + base.random.Choose("13740", "13741", "13742"))));
			}
			else
			{
				this.dialogueparts.Add("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13744");
			}
			break;
		case "Rock Crab":
		case "Lava Crab":
			this.parts.Clear();
			this.parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13747", this.numberToKill.Value));
			this.target.Value = "Demetrius";
			this.dialogueparts.Clear();
			this.dialogueparts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13750", this.monster.Value));
			break;
		default:
			this.parts.Clear();
			this.parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13752", this.monster.Value, this.numberToKill.Value, new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs." + base.random.Choose("13755", "13756", "13757"))));
			this.target.Value = "Wizard";
			this.dialogueparts.Clear();
			this.dialogueparts.Add("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13760");
			break;
		}
		if (this.target.Value.Equals("Wizard") && !Utility.doesAnyFarmerHaveMail("wizardJunimoNote") && !Utility.doesAnyFarmerHaveMail("JojaMember"))
		{
			this.parts.Clear();
			this.parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13764", this.numberToKill.Value, this.monster.Value));
			this.target.Value = "Lewis";
			this.dialogueparts.Clear();
			this.dialogueparts.Add("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13767");
		}
		this.parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:FishingQuest.cs.13274", this.reward.Value));
		this.objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13770", "0", this.numberToKill.Value, this.monster.Value);
	}

	public override void reloadDescription()
	{
		if (base._questDescription == "")
		{
			this.loadQuestInfo();
		}
		string descriptionBuilder = "";
		string messageBuilder = "";
		if (this.parts != null && this.parts.Count != 0)
		{
			foreach (DescriptionElement a in this.parts)
			{
				descriptionBuilder += a.loadDescriptionElement();
			}
			base.questDescription = descriptionBuilder;
		}
		if (this.dialogueparts != null && this.dialogueparts.Count != 0)
		{
			foreach (DescriptionElement b in this.dialogueparts)
			{
				messageBuilder += b.loadDescriptionElement();
			}
			this.targetMessage = messageBuilder;
		}
		else if (base.HasId())
		{
			string[] fields = Quest.GetRawQuestFields(base.id.Value);
			this.targetMessage = ArgUtility.Get(fields, 9, this.targetMessage, allowBlank: false);
		}
	}

	public override void reloadObjective()
	{
		if ((int)this.numberKilled != 0 || !base.HasId())
		{
			if ((int)this.numberKilled < (int)this.numberToKill)
			{
				this.objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13770", this.numberKilled.Value, this.numberToKill.Value, this.monster.Value);
			}
			if (this.objective.Value != null)
			{
				base.currentObjective = this.objective.Value.loadDescriptionElement();
			}
		}
	}

	private bool isSlimeName(string s)
	{
		if (s.Contains("Slime") || s.Contains("Jelly") || s.Contains("Sludge"))
		{
			return true;
		}
		return false;
	}

	public override bool checkIfComplete(NPC n = null, int number1 = -1, int number2 = -1, Item item = null, string monsterName = null, bool probe = false)
	{
		if ((bool)base.completed)
		{
			return false;
		}
		if (monsterName == null)
		{
			monsterName = "Green Slime";
		}
		if (n == null && (monsterName.Contains(this.monsterName.Value) || (base.id.Equals("15") && this.isSlimeName(monsterName))) && (int)this.numberKilled < (int)this.numberToKill)
		{
			if (!probe)
			{
				this.numberKilled.Value = Math.Min(this.numberToKill, (int)this.numberKilled + 1);
				Game1.dayTimeMoneyBox.pingQuest(this);
				if ((int)this.numberKilled >= (int)this.numberToKill)
				{
					if (this.target.Value == null || this.target.Value.Equals("null"))
					{
						this.questComplete();
					}
					else
					{
						NPC actualTarget = Game1.getCharacterFromName(this.target);
						this.objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:FishingQuest.cs.13277", actualTarget);
						Game1.playSound("jingle1");
					}
				}
				else if (this.monster.Value == null)
				{
					if (monsterName == "Frost Jelly" || monsterName == "Sludge")
					{
						this.monster.Value = new Monster("Green Slime", Vector2.Zero);
						this.monster.Value.Name = monsterName;
					}
					else
					{
						this.monster.Value = new Monster(monsterName, Vector2.Zero);
					}
				}
			}
		}
		else if (n != null && this.target.Value != null && !this.target.Value.Equals("null") && (int)this.numberKilled >= (int)this.numberToKill && n.Name.Equals(this.target.Value) && n.IsVillager)
		{
			if (!probe)
			{
				this.reloadDescription();
				n.CurrentDialogue.Push(new Dialogue(n, null, this.targetMessage));
				base.moneyReward.Value = this.reward;
				this.questComplete();
				Game1.drawDialogue(n);
			}
			return true;
		}
		return false;
	}
}
