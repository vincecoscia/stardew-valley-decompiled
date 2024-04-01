using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.GameData;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.TokenizableStrings;

namespace StardewValley.Objects;

public class Trinket : Object
{
	protected string _description;

	protected TrinketData _data;

	protected TrinketEffect _trinketEffect;

	protected string _trinketEffectClassName;

	/// <summary>The parsed form of <see cref="F:StardewValley.Objects.Trinket.displayNameOverrideTemplate" /> used to build the display name for <see cref="M:StardewValley.Objects.Trinket.loadDisplayName" />.</summary>
	protected string displayNameOverride;

	/// <summary>The net-synced <see cref="T:StardewValley.TokenizableStrings.TokenParser">tokenized string</see> used to build the display name for <see cref="M:StardewValley.Objects.Trinket.loadDisplayName" />.</summary>
	public readonly NetString displayNameOverrideTemplate = new NetString();

	/// <summary>The net-synced <see cref="T:StardewValley.TokenizableStrings.TokenParser">tokenized strings</see> used to fill placeholders in <see cref="M:StardewValley.Objects.Trinket.getDescription" />.</summary>
	public readonly NetStringList descriptionSubstitutionTemplates = new NetStringList();

	public readonly NetStringDictionary<string, NetString> trinketMetadata = new NetStringDictionary<string, NetString>();

	[XmlElement("generationSeed")]
	public readonly NetInt generationSeed = new NetInt();

	public override string TypeDefinitionId { get; } = "(TR)";


	public Trinket()
	{
	}

	public Trinket(string itemId, int generationSeed)
		: this()
	{
		base.ItemId = itemId;
		this.generationSeed.Value = generationSeed;
		ParsedItemData data = ItemRegistry.GetDataOrErrorItem(itemId);
		base.ParentSheetIndex = data.SpriteIndex;
		this.GetEffect()?.GenerateRandomStats(this);
	}

	public static bool CanSpawnTrinket(Farmer f)
	{
		return f.stats.Get("trinketSlots") != 0;
	}

	public static void SpawnTrinket(GameLocation location, Vector2 spawnPoint)
	{
		Trinket t = Trinket.GetRandomTrinket();
		if (t != null)
		{
			Game1.createItemDebris(t, spawnPoint, Game1.random.Next(4), location);
		}
	}

	public override bool canBeShipped()
	{
		return false;
	}

	public override int sellToStorePrice(long specificPlayerID = -1L)
	{
		return 1000;
	}

	public static void TrySpawnTrinket(GameLocation location, Monster monster, Vector2 spawnPosition, double chanceModifier = 1.0)
	{
		if (!Trinket.CanSpawnTrinket(Game1.player))
		{
			return;
		}
		double baseChance = 0.004;
		if (monster != null)
		{
			baseChance += (double)monster.MaxHealth * 1E-05;
			if ((bool)monster.isGlider && monster.MaxHealth >= 150)
			{
				baseChance += 0.002;
			}
			if (monster is Leaper)
			{
				baseChance -= 0.005;
			}
		}
		baseChance = Math.Min(0.025, baseChance);
		baseChance += Game1.player.DailyLuck / 25.0;
		baseChance += (double)((float)Game1.player.LuckLevel * 0.00133f);
		baseChance *= chanceModifier;
		if (Game1.random.NextDouble() < baseChance)
		{
			Trinket.SpawnTrinket(location, spawnPosition);
		}
	}

	public static Trinket GetRandomTrinket()
	{
		Dictionary<string, TrinketData> data_sheet = DataLoader.Trinkets(Game1.content);
		Trinket t = null;
		while (t == null)
		{
			int which = Game1.random.Next(data_sheet.Count);
			int i = 0;
			foreach (TrinketData trinket in data_sheet.Values)
			{
				if (which == i && trinket.DropsNaturally)
				{
					t = ItemRegistry.Create<Trinket>("(TR)" + trinket.ID);
					break;
				}
				i++;
			}
		}
		return t;
	}

	public override bool canBeGivenAsGift()
	{
		return true;
	}

	public override void reloadSprite()
	{
		base.reloadSprite();
		this.GetEffect()?.GenerateRandomStats(this);
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(this.trinketMetadata, "trinketMetadata").AddField(this.generationSeed, "generationSeed").AddField(this.displayNameOverrideTemplate, "displayNameOverrideTemplate")
			.AddField(this.descriptionSubstitutionTemplates, "descriptionSubstitutionTemplates");
		this.displayNameOverrideTemplate.fieldChangeVisibleEvent += delegate(NetString field, string oldValue, string newValue)
		{
			this.displayNameOverride = TokenParser.ParseText(newValue);
		};
		this.descriptionSubstitutionTemplates.OnElementChanged += delegate
		{
			this._description = null;
		};
		this.descriptionSubstitutionTemplates.OnArrayReplaced += delegate
		{
			this._description = null;
		};
	}

	public string GetTrinketMetadata(string key)
	{
		if (this.trinketMetadata.ContainsKey(key))
		{
			return this.trinketMetadata[key];
		}
		TrinketData data = this.GetTrinketData();
		if (data != null && data.TrinketMetadata != null && data.TrinketMetadata.ContainsKey(key))
		{
			return data.TrinketMetadata[key];
		}
		return null;
	}

	public TrinketData GetTrinketData()
	{
		if (this._data == null && !DataLoader.Trinkets(Game1.content).TryGetValue(base.ItemId, out this._data))
		{
			this._data = null;
		}
		return this._data;
	}

	public virtual TrinketEffect GetEffect()
	{
		if (this._trinketEffect == null)
		{
			TrinketData data = this.GetTrinketData();
			if (data != null && this._trinketEffectClassName != data.TrinketEffectClass)
			{
				this._trinketEffectClassName = data.TrinketEffectClass;
				if (data.TrinketEffectClass != null)
				{
					Type trinket_effect_type = System.Type.GetType(data.TrinketEffectClass);
					if (trinket_effect_type != null)
					{
						this._trinketEffect = (TrinketEffect)Activator.CreateInstance(trinket_effect_type, this);
					}
				}
			}
		}
		return this._trinketEffect;
	}

	protected override string loadDisplayName()
	{
		ParsedItemData data = ItemRegistry.GetDataOrErrorItem(base.ItemId);
		return this.displayNameOverride ?? data.DisplayName;
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	public override string getDescription()
	{
		if (this._description == null)
		{
			string description = TokenParser.ParseText(ItemRegistry.GetDataOrErrorItem(base.ItemId).Description);
			if (this.descriptionSubstitutionTemplates.Count > 0)
			{
				object[] tokens = new object[this.descriptionSubstitutionTemplates.Count];
				for (int i = 0; i < this.descriptionSubstitutionTemplates.Count; i++)
				{
					tokens[i] = TokenParser.ParseText(this.descriptionSubstitutionTemplates[i]);
				}
				description = string.Format(description, tokens);
			}
			this._description = Game1.parseText(description, Game1.smallFont, this.getDescriptionWidth());
		}
		return this._description;
	}

	public override string getCategoryName()
	{
		return Game1.content.LoadString("Strings\\1_6_Strings:Trinket");
	}

	public override Color getCategoryColor()
	{
		return new Color(96, 81, 255);
	}

	public override bool isPlaceable()
	{
		return false;
	}

	public override bool performUseAction(GameLocation location)
	{
		this.GetEffect().OnUse(Game1.player);
		return false;
	}

	public override bool performToolAction(Tool t)
	{
		return false;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Trinket(base.ItemId, this.generationSeed.Value);
	}

	public override bool IsHeldOverHead()
	{
		return false;
	}

	public virtual void Apply(Farmer farmer)
	{
		this.GetEffect()?.Apply(farmer);
	}

	public virtual void Unapply(Farmer farmer)
	{
		this.GetEffect()?.Unapply(farmer);
	}

	public virtual void Update(Farmer farmer, GameTime time, GameLocation location)
	{
		this.GetEffect()?.Update(farmer, time, location);
	}

	public virtual void OnFootstep(Farmer farmer)
	{
		this.GetEffect()?.OnFootstep(farmer);
	}

	public virtual void OnReceiveDamage(Farmer farmer, int damageAmount)
	{
		this.GetEffect()?.OnReceiveDamage(farmer, damageAmount);
	}

	public virtual void OnDamageMonster(Farmer farmer, Monster m, int damageAmount)
	{
		this.GetEffect()?.OnDamageMonster(farmer, m, damageAmount);
	}
}
