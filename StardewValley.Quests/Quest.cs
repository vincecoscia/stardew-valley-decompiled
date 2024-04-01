using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Mods;
using StardewValley.Monsters;

namespace StardewValley.Quests;

[XmlInclude(typeof(CraftingQuest))]
[XmlInclude(typeof(DescriptionElement))]
[XmlInclude(typeof(FishingQuest))]
[XmlInclude(typeof(GoSomewhereQuest))]
[XmlInclude(typeof(ItemDeliveryQuest))]
[XmlInclude(typeof(ItemHarvestQuest))]
[XmlInclude(typeof(LostItemQuest))]
[XmlInclude(typeof(ResourceCollectionQuest))]
[XmlInclude(typeof(SecretLostItemQuest))]
[XmlInclude(typeof(SlayMonsterQuest))]
[XmlInclude(typeof(SocializeQuest))]
public class Quest : INetObject<NetFields>, IQuest, IHaveModData
{
	public const int type_basic = 1;

	public const int type_crafting = 2;

	public const int type_itemDelivery = 3;

	public const int type_monster = 4;

	public const int type_socialize = 5;

	public const int type_location = 6;

	public const int type_fishing = 7;

	public const int type_building = 8;

	public const int type_harvest = 9;

	public const int type_resource = 10;

	public const int type_weeding = 11;

	public string _currentObjective = "";

	public string _questDescription = "";

	public string _questTitle = "";

	[XmlElement("rewardDescription")]
	public readonly NetString rewardDescription = new NetString();

	[XmlElement("completionString")]
	public readonly NetString completionString = new NetString();

	protected Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed);

	[XmlElement("accepted")]
	public readonly NetBool accepted = new NetBool();

	[XmlElement("completed")]
	public readonly NetBool completed = new NetBool();

	[XmlElement("dailyQuest")]
	public readonly NetBool dailyQuest = new NetBool();

	[XmlElement("showNew")]
	public readonly NetBool showNew = new NetBool();

	[XmlElement("canBeCancelled")]
	public readonly NetBool canBeCancelled = new NetBool();

	[XmlElement("destroy")]
	public readonly NetBool destroy = new NetBool();

	[XmlElement("id")]
	public readonly NetString id = new NetString();

	[XmlElement("moneyReward")]
	public readonly NetInt moneyReward = new NetInt();

	[XmlElement("questType")]
	public readonly NetInt questType = new NetInt();

	[XmlElement("daysLeft")]
	public readonly NetInt daysLeft = new NetInt();

	[XmlElement("dayQuestAccepted")]
	public readonly NetInt dayQuestAccepted = new NetInt(-1);

	[XmlArrayItem("int")]
	public readonly NetStringList nextQuests = new NetStringList();

	private bool _loadedDescription;

	protected bool _loadedTitle;

	/// <inheritdoc />
	[XmlIgnore]
	public ModDataDictionary modData { get; } = new ModDataDictionary();


	/// <inheritdoc />
	[XmlElement("modData")]
	public ModDataDictionary modDataForSerialization
	{
		get
		{
			return this.modData.GetForSerialization();
		}
		set
		{
			this.modData.SetFromSerialization(value);
		}
	}

	public NetFields NetFields { get; }

	public string questTitle
	{
		get
		{
			if (!this._loadedTitle)
			{
				switch (this.questType.Value)
				{
				case 3:
					if (this is ItemDeliveryQuest quest4 && quest4.target.Value != null)
					{
						this._questTitle = Game1.content.LoadString("Strings\\1_6_Strings:ItemDeliveryQuestTitle", NPC.GetDisplayName(quest4.target.Value));
					}
					else
					{
						this._questTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13285");
					}
					break;
				case 4:
					if (this is SlayMonsterQuest quest3 && quest3.monsterName.Value != null)
					{
						this._questTitle = Game1.content.LoadString("Strings\\1_6_Strings:MonsterQuestTitle", Monster.GetDisplayName(quest3.monsterName.Value));
					}
					else
					{
						this._questTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:SlayMonsterQuest.cs.13696");
					}
					break;
				case 5:
					this._questTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:SocializeQuest.cs.13785");
					break;
				case 7:
					if (this is FishingQuest quest2 && quest2.ItemId.Value != null)
					{
						string fishName = "???";
						ParsedItemData data2 = ItemRegistry.GetDataOrErrorItem(quest2.ItemId.Value);
						if (!data2.IsErrorItem)
						{
							fishName = data2.DisplayName;
						}
						this._questTitle = Game1.content.LoadString("Strings\\1_6_Strings:FishingQuestTitle", fishName);
					}
					else
					{
						this._questTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingQuest.cs.13227");
					}
					break;
				case 10:
					if (this is ResourceCollectionQuest quest && quest.ItemId.Value != null)
					{
						string resourceName = "???";
						ParsedItemData data = ItemRegistry.GetDataOrErrorItem(quest.ItemId.Value);
						if (!data.IsErrorItem)
						{
							resourceName = data.DisplayName;
						}
						this._questTitle = Game1.content.LoadString("Strings\\1_6_Strings:ResourceQuestTitle", resourceName);
					}
					else
					{
						this._questTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13640");
					}
					break;
				}
				string[] fields = Quest.GetRawQuestFields(this.id.Value);
				this._questTitle = ArgUtility.Get(fields, 1, this._questTitle);
				this._loadedTitle = true;
			}
			if (this._questTitle == null)
			{
				this._questTitle = "";
			}
			return this._questTitle;
		}
		set
		{
			this._questTitle = value;
		}
	}

	[XmlIgnore]
	public string questDescription
	{
		get
		{
			if (!this._loadedDescription)
			{
				this.reloadDescription();
				string[] fields = Quest.GetRawQuestFields(this.id.Value);
				this._questDescription = ArgUtility.Get(fields, 2, this._questDescription);
				this._loadedDescription = true;
			}
			if (this._questDescription == null)
			{
				this._questDescription = "";
			}
			return this._questDescription;
		}
		set
		{
			this._questDescription = value;
		}
	}

	[XmlIgnore]
	public string currentObjective
	{
		get
		{
			string[] fields = Quest.GetRawQuestFields(this.id.Value);
			this._currentObjective = ArgUtility.Get(fields, 3, this._currentObjective, allowBlank: false);
			this.reloadObjective();
			if (this._currentObjective == null)
			{
				this._currentObjective = "";
			}
			return this._currentObjective;
		}
		set
		{
			this._currentObjective = value;
		}
	}

	public Quest()
	{
		this.NetFields = new NetFields(NetFields.GetNameForInstance(this));
		this.initNetFields();
	}

	protected virtual void initNetFields()
	{
		this.NetFields.SetOwner(this).AddField(this.rewardDescription, "rewardDescription").AddField(this.completionString, "completionString")
			.AddField(this.accepted, "accepted")
			.AddField(this.completed, "completed")
			.AddField(this.dailyQuest, "dailyQuest")
			.AddField(this.showNew, "showNew")
			.AddField(this.canBeCancelled, "canBeCancelled")
			.AddField(this.destroy, "destroy")
			.AddField(this.id, "id")
			.AddField(this.moneyReward, "moneyReward")
			.AddField(this.questType, "questType")
			.AddField(this.daysLeft, "daysLeft")
			.AddField(this.nextQuests, "nextQuests")
			.AddField(this.dayQuestAccepted, "dayQuestAccepted")
			.AddField(this.modData, "modData");
	}

	public static string[] GetRawQuestFields(string id)
	{
		if (id == null)
		{
			return null;
		}
		Dictionary<string, string> questData = DataLoader.Quests(Game1.content);
		if (questData == null || !questData.TryGetValue(id, out var rawData))
		{
			return null;
		}
		return rawData.Split('/');
	}

	public static Quest getQuestFromId(string id)
	{
		string[] fields = Quest.GetRawQuestFields(id);
		if (fields == null)
		{
			return null;
		}
		if (!ArgUtility.TryGet(fields, 0, out var questType, out var error, allowBlank: false) || !ArgUtility.TryGet(fields, 1, out var title, out error, allowBlank: false) || !ArgUtility.TryGet(fields, 2, out var description, out error, allowBlank: false) || !ArgUtility.TryGetOptional(fields, 3, out var objective, out error, null, allowBlank: false) || !ArgUtility.TryGetOptional(fields, 5, out var rawNextQuests, out error, null, allowBlank: false) || !ArgUtility.TryGetInt(fields, 6, out var moneyReward, out error) || !ArgUtility.TryGetOptional(fields, 7, out var rewardDescription, out error, null, allowBlank: false) || !ArgUtility.TryGetOptionalBool(fields, 8, out var canBeCancelled, out error))
		{
			return Quest.LogParseError(id, error);
		}
		string[] nextQuests = ArgUtility.SplitBySpace(rawNextQuests);
		Quest q;
		switch (questType)
		{
		case "Crafting":
		{
			if (!Quest.TryParseConditions(fields, out var conditions8, out error))
			{
				return Quest.LogParseError(id, error);
			}
			if (!ArgUtility.TryGet(conditions8, 0, out var itemId5, out error, allowBlank: false))
			{
				return Quest.LogConditionsParseError(id, error);
			}
			bool? isBigCraftable = null;
			if (ArgUtility.HasIndex(conditions8, 1))
			{
				if (!ArgUtility.TryGetOptionalBool(conditions8, 1, out var isBigCraftableValue, out error))
				{
					return Quest.LogConditionsParseError(id, error);
				}
				isBigCraftable = isBigCraftableValue;
			}
			if (!ItemRegistry.IsQualifiedItemId(itemId5))
			{
				itemId5 = ((!isBigCraftable.HasValue) ? (ItemRegistry.QualifyItemId(itemId5) ?? itemId5) : (isBigCraftable.Value ? ("(BC)" + itemId5) : ("(O)" + itemId5)));
			}
			q = new CraftingQuest(itemId5);
			q.questType.Value = 2;
			break;
		}
		case "Location":
		{
			if (!Quest.TryParseConditions(fields, out var conditions7, out error))
			{
				return Quest.LogParseError(id, error);
			}
			if (!ArgUtility.TryGet(conditions7, 0, out var locationName, out error, allowBlank: false))
			{
				return Quest.LogConditionsParseError(id, error);
			}
			q = new GoSomewhereQuest(locationName);
			q.questType.Value = 6;
			break;
		}
		case "Building":
		{
			if (!Quest.TryParseConditions(fields, out var conditions6, out error))
			{
				return Quest.LogParseError(id, error);
			}
			if (!ArgUtility.TryGet(conditions6, 0, out var completionString, out error, allowBlank: false))
			{
				return Quest.LogConditionsParseError(id, error);
			}
			q = new Quest();
			q.questType.Value = 8;
			q.completionString.Value = completionString;
			break;
		}
		case "ItemDelivery":
		{
			if (!Quest.TryParseConditions(fields, out var conditions5, out error) || !ArgUtility.TryGet(fields, 9, out var targetMessage, out error, allowBlank: false))
			{
				return Quest.LogParseError(id, error);
			}
			if (!ArgUtility.TryGet(conditions5, 0, out var npcName3, out error, allowBlank: false) || !ArgUtility.TryGet(conditions5, 1, out var itemId4, out error, allowBlank: false) || !ArgUtility.TryGetOptionalInt(conditions5, 2, out var numberRequired2, out error, 1))
			{
				return Quest.LogConditionsParseError(id, error);
			}
			ItemDeliveryQuest itemDeliveryQuest = new ItemDeliveryQuest(npcName3, itemId4);
			itemDeliveryQuest.targetMessage = targetMessage;
			itemDeliveryQuest.number.Value = numberRequired2;
			itemDeliveryQuest.questType.Value = 3;
			q = itemDeliveryQuest;
			break;
		}
		case "Monster":
		{
			if (!Quest.TryParseConditions(fields, out var conditions4, out error))
			{
				return Quest.LogParseError(id, error);
			}
			if (!ArgUtility.TryGet(conditions4, 0, out var monsterName, out error, allowBlank: false) || !ArgUtility.TryGetInt(conditions4, 1, out var numberToKill, out error) || !ArgUtility.TryGetOptional(conditions4, 2, out var targetNpc, out error))
			{
				return Quest.LogConditionsParseError(id, error);
			}
			SlayMonsterQuest slayQuest = new SlayMonsterQuest();
			slayQuest.loadQuestInfo();
			slayQuest.monster.Value.Name = monsterName.Replace('_', ' ');
			slayQuest.monsterName.Value = slayQuest.monster.Value.Name;
			slayQuest.numberToKill.Value = numberToKill;
			slayQuest.target.Value = targetNpc ?? "null";
			slayQuest.questType.Value = 4;
			q = slayQuest;
			break;
		}
		case "Basic":
			q = new Quest();
			q.questType.Value = 1;
			break;
		case "Social":
		{
			SocializeQuest socializeQuest = new SocializeQuest();
			socializeQuest.loadQuestInfo();
			q = socializeQuest;
			break;
		}
		case "ItemHarvest":
		{
			if (!Quest.TryParseConditions(fields, out var conditions3, out error))
			{
				return Quest.LogParseError(id, error);
			}
			if (!ArgUtility.TryGet(conditions3, 0, out var itemId3, out error, allowBlank: false) || !ArgUtility.TryGetOptionalInt(conditions3, 1, out var numberRequired, out error, 1))
			{
				return Quest.LogConditionsParseError(id, error);
			}
			q = new ItemHarvestQuest(itemId3, numberRequired);
			break;
		}
		case "LostItem":
		{
			if (!Quest.TryParseConditions(fields, out var conditions2, out error))
			{
				return Quest.LogParseError(id, error);
			}
			if (!ArgUtility.TryGet(conditions2, 0, out var npcName2, out error, allowBlank: false) || !ArgUtility.TryGet(conditions2, 1, out var itemId2, out error, allowBlank: false) || !ArgUtility.TryGet(conditions2, 2, out var locationOfItem, out error, allowBlank: false) || !ArgUtility.TryGetInt(conditions2, 3, out var tileX, out error) || !ArgUtility.TryGetInt(conditions2, 4, out var tileY, out error))
			{
				return Quest.LogConditionsParseError(id, error);
			}
			q = new LostItemQuest(npcName2, locationOfItem, itemId2, tileX, tileY);
			break;
		}
		case "SecretLostItem":
		{
			if (!Quest.TryParseConditions(fields, out var conditions, out error))
			{
				return Quest.LogParseError(id, error);
			}
			if (!ArgUtility.TryGet(conditions, 0, out var npcName, out error, allowBlank: false) || !ArgUtility.TryGet(conditions, 1, out var itemId, out error, allowBlank: false) || !ArgUtility.TryGetInt(conditions, 2, out var friendshipReward, out error) || !ArgUtility.TryGetOptional(conditions, 3, out var exclusiveQuestId, out error, null, allowBlank: false))
			{
				return Quest.LogConditionsParseError(id, error);
			}
			q = new SecretLostItemQuest(npcName, itemId, friendshipReward, exclusiveQuestId);
			break;
		}
		default:
			return Quest.LogParseError(id, "quest type '" + questType + "' doesn't match a known type.");
		}
		q.id.Value = id;
		q.questTitle = title;
		q.questDescription = description;
		q.currentObjective = objective;
		string[] array = nextQuests;
		for (int i = 0; i < array.Length; i++)
		{
			string nextQuest = array[i];
			if (nextQuest.StartsWith('h'))
			{
				if (!Game1.IsMasterGame)
				{
					continue;
				}
				nextQuest = nextQuest.Substring(1);
			}
			q.nextQuests.Add(nextQuest);
		}
		q.showNew.Value = true;
		q.moneyReward.Value = moneyReward;
		q.rewardDescription.Value = ((moneyReward == -1) ? null : rewardDescription);
		q.canBeCancelled.Value = canBeCancelled;
		return q;
	}

	public virtual void reloadObjective()
	{
	}

	public virtual void reloadDescription()
	{
	}

	public virtual void adjustGameLocation(GameLocation location)
	{
	}

	public virtual void accept()
	{
		this.accepted.Value = true;
	}

	public virtual bool checkIfComplete(NPC n = null, int number1 = -1, int number2 = -2, Item item = null, string str = null, bool probe = false)
	{
		if (this.completionString.Value != null && str != null && str.Equals(this.completionString.Value))
		{
			if (!probe)
			{
				this.questComplete();
			}
			return true;
		}
		return false;
	}

	public bool hasReward()
	{
		if ((int)this.moneyReward <= 0)
		{
			string value = this.rewardDescription.Value;
			if (value == null)
			{
				return false;
			}
			return value.Length > 2;
		}
		return true;
	}

	public virtual bool isSecretQuest()
	{
		return false;
	}

	public virtual void questComplete()
	{
		if ((bool)this.completed)
		{
			return;
		}
		if ((bool)this.dailyQuest)
		{
			Game1.stats.Increment("BillboardQuestsDone");
			if (!Game1.player.mailReceived.Contains("completedFirstBillboardQuest"))
			{
				Game1.player.mailReceived.Add("completedFirstBillboardQuest");
			}
			if (Game1.stats.Get("BillboardQuestsDone") % 3 == 0)
			{
				if (!Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)PrizeTicket")))
				{
					Game1.createItemDebris(ItemRegistry.Create("(O)PrizeTicket"), Game1.player.getStandingPosition(), 2);
				}
				if (Game1.stats.Get("BillboardQuestsDone") >= 6 && !Game1.player.mailReceived.Contains("gotFirstBillboardPrizeTicket"))
				{
					Game1.player.mailReceived.Add("gotFirstBillboardPrizeTicket");
				}
			}
		}
		if ((bool)this.dailyQuest || (int)this.questType == 7)
		{
			Game1.stats.QuestsCompleted++;
		}
		this.completed.Value = true;
		Game1.player.currentLocation?.customQuestCompleteBehavior(this.id);
		if (this.nextQuests.Count > 0)
		{
			foreach (string i in this.nextQuests)
			{
				if (this.IsValidId(i))
				{
					Game1.player.addQuest(i);
				}
			}
			Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Quest.cs.13636"), 2));
		}
		if ((int)this.moneyReward <= 0 && (this.rewardDescription.Value == null || this.rewardDescription.Value.Length <= 2))
		{
			Game1.player.questLog.Remove(this);
		}
		else
		{
			Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Quest.cs.13636"), 2));
		}
		Game1.playSound("questcomplete");
		if (this.id.Value == "126")
		{
			Game1.player.mailReceived.Add("emilyFiber");
			Game1.player.activeDialogueEvents.Add("emilyFiber", 2);
		}
		Game1.dayTimeMoneyBox.questsDirty = true;
		Game1.player.autoGenerateActiveDialogueEvent("questComplete_" + this.id);
	}

	public string GetName()
	{
		return this.questTitle;
	}

	public string GetDescription()
	{
		return this.questDescription;
	}

	public bool IsHidden()
	{
		return this.isSecretQuest();
	}

	public List<string> GetObjectiveDescriptions()
	{
		return new List<string> { this.currentObjective };
	}

	public bool CanBeCancelled()
	{
		return this.canBeCancelled.Value;
	}

	public bool HasReward()
	{
		if (!this.HasMoneyReward())
		{
			string value = this.rewardDescription.Value;
			if (value == null)
			{
				return false;
			}
			return value.Length > 2;
		}
		return true;
	}

	public bool HasMoneyReward()
	{
		if (this.completed.Value)
		{
			return this.moneyReward.Value > 0;
		}
		return false;
	}

	public void MarkAsViewed()
	{
		this.showNew.Value = false;
	}

	public bool ShouldDisplayAsNew()
	{
		return this.showNew.Value;
	}

	public bool ShouldDisplayAsComplete()
	{
		if (this.completed.Value)
		{
			return !this.IsHidden();
		}
		return false;
	}

	public bool IsTimedQuest()
	{
		if (!this.dailyQuest.Value)
		{
			return this.GetDaysLeft() > 0;
		}
		return true;
	}

	public int GetDaysLeft()
	{
		return this.daysLeft;
	}

	public int GetMoneyReward()
	{
		return this.moneyReward.Value;
	}

	public void OnMoneyRewardClaimed()
	{
		this.moneyReward.Value = 0;
		this.destroy.Value = true;
	}

	public bool OnLeaveQuestPage()
	{
		if ((bool)this.completed && (int)this.moneyReward <= 0)
		{
			this.destroy.Value = true;
		}
		if (this.destroy.Value)
		{
			Game1.player.questLog.Remove(this);
			return true;
		}
		return false;
	}

	/// <summary>Get whether the <see cref="F:StardewValley.Quests.Quest.id" /> is set to a valid value.</summary>
	protected bool HasId()
	{
		return this.IsValidId(this.id.Value);
	}

	/// <summary>Get whether the given quest ID is valid.</summary>
	/// <param name="id">The quest ID to check.</param>
	protected bool IsValidId(string id)
	{
		switch (id)
		{
		case "7":
			return Game1.whichModFarm?.Id != "MeadowlandsFarm";
		case null:
		case "-1":
		case "0":
			return false;
		default:
			return true;
		}
	}

	/// <summary>Get the split quest conditions from raw quest fields, if it's found and valid.</summary>
	/// <param name="questFields">The raw quest fields.</param>
	/// <param name="conditions">The parsed conditions.</param>
	/// <param name="error">The error message indicating why parsing failed.</param>
	/// <param name="allowBlank">Whether to match the argument even if it's null or whitespace. If false, it will be treated as invalid in that case.</param>
	/// <returns>Returns whether the conditions field was found and valid.</returns>
	protected static bool TryParseConditions(string[] questFields, out string[] conditions, out string error, bool allowBlank = false)
	{
		if (!ArgUtility.TryGet(questFields, 4, out var rawConditions, out error, allowBlank))
		{
			conditions = null;
			return false;
		}
		conditions = ArgUtility.SplitBySpace(rawConditions);
		error = null;
		return true;
	}

	/// <summary>Log an error message indicating that the quest data couldn't be parsed.</summary>
	/// <param name="id">The quest ID being parsed.</param>
	/// <param name="error">The error message indicating why parsing failed.</param>
	/// <returns>Returns a null quest for convenience.</returns>
	protected static Quest LogParseError(string id, string error)
	{
		Game1.log.Error("Failed to parse data for quest '" + id + "': " + error);
		return null;
	}

	/// <summary>Log an error message indicating that the conditions field in the quest data couldn't be parsed.</summary>
	/// <param name="id">The quest ID being parsed.</param>
	/// <param name="error">The error message indicating why parsing failed.</param>
	/// <returns>Returns a null quest for convenience.</returns>
	protected static Quest LogConditionsParseError(string id, string error)
	{
		Game1.log.Error("Failed to parse for quest '" + id + "': conditions field (index 4) is invalid: " + error);
		return null;
	}
}
