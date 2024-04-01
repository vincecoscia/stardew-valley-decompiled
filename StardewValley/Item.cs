using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Netcode.Validation;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Delegates;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Mods;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley;

[XmlInclude(typeof(Boots))]
[XmlInclude(typeof(Clothing))]
[XmlInclude(typeof(Hat))]
[XmlInclude(typeof(ModDataDictionary))]
[XmlInclude(typeof(Object))]
[XmlInclude(typeof(Ring))]
[XmlInclude(typeof(SpecialItem))]
[XmlInclude(typeof(Tool))]
[InstanceStatics]
[NotImplicitNetField]
public abstract class Item : IComparable, INetObject<NetFields>, ISalable, IHaveItemTypeId, IHaveModData
{
	public bool isLostItem;

	private readonly NetInt specialVariable = new NetInt();

	[XmlElement("category")]
	public readonly NetInt category = new NetInt();

	[XmlElement("hasBeenInInventory")]
	public readonly NetBool hasbeenInInventory = new NetBool();

	private HashSet<string> _contextTags;

	protected bool _contextTagsDirty;

	/// <summary>Temporary data used for special purposes like fish catches. This isn't synchronized or saved.</summary>
	[XmlIgnore]
	public Dictionary<string, object> tempData;

	/// <summary>The mail flag to set for the player when it's added to their inventory for the first time.</summary>
	/// <remarks>This is a temporary value (e.g. for a freshly caught fish), and isn't persisted or synchronized.</remarks>
	[XmlIgnore]
	public string SetFlagOnPickup;

	[XmlElement("name")]
	public readonly NetString netName = new NetString();

	/// <summary>The backing field for <see cref="P:StardewValley.Item.ParentSheetIndex" />.</summary>
	[XmlElement("parentSheetIndex")]
	public readonly NetInt parentSheetIndex = new NetInt();

	/// <summary>The backing field for <see cref="P:StardewValley.Item.ItemId" />.</summary>
	[XmlElement("itemId")]
	public NetString itemId = new NetString();

	/// <summary>The backing field for <see cref="P:StardewValley.Item.QualifiedItemId" />.</summary>
	[XmlIgnore]
	protected string _qualifiedItemId;

	public bool specialItem;

	[XmlElement("isRecipe")]
	public readonly NetBool isRecipe = new NetBool();

	[XmlElement("quality")]
	public readonly NetInt quality = new NetInt(0);

	[XmlElement("stack")]
	public readonly NetInt stack = new NetInt(1);

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

	public int SpecialVariable
	{
		get
		{
			return this.specialVariable;
		}
		set
		{
			this.specialVariable.Set(value);
		}
	}

	[XmlIgnore]
	public int Category
	{
		get
		{
			return this.category;
		}
		set
		{
			this.category.Set(value);
		}
	}

	[XmlIgnore]
	public bool HasBeenInInventory
	{
		get
		{
			return this.hasbeenInInventory;
		}
		set
		{
			this.hasbeenInInventory.Set(value);
		}
	}

	[XmlIgnore]
	public NetFields NetFields { get; } = new NetFields("Item");


	/// <summary>The sprite index within the item's spritesheet texture to draw for this item.</summary>
	[XmlIgnore]
	public int ParentSheetIndex
	{
		get
		{
			return this.parentSheetIndex;
		}
		set
		{
			this.parentSheetIndex.Value = value;
		}
	}

	/// <inheritdoc />
	public abstract string TypeDefinitionId { get; }

	/// <summary>A key which uniquely identifies the item data among others of the same type.</summary>
	/// <remarks>
	///   <para>This identifies the item data, not the instance itself. For example, Wine and Pufferfish have different item IDs, but two instances of Wine have the same ID. Note that flavored items use the same ID as their base item (like Blueberry Wine and Wine).</para>
	///
	///   <para>This should only contain alphanumeric, underscore, and dot characters. This should be globally unique for custom items, but vanilla items may not be for legacy reasons; you can use <see cref="P:StardewValley.Item.QualifiedItemId" /> instead as the globally unique ID.</para>
	/// </remarks>
	[XmlIgnore]
	public string ItemId
	{
		get
		{
			if (this.itemId.Value == null)
			{
				this.MigrateLegacyItemId();
			}
			return this.itemId.Value;
		}
		set
		{
			this.itemId.Value = value;
			this._qualifiedItemId = null;
		}
	}

	/// <summary>A globally unique item ID which includes the <see cref="P:StardewValley.Item.TypeDefinitionId" /> and <see cref="P:StardewValley.Item.ItemId" />.</summary>
	[XmlIgnore]
	public string QualifiedItemId
	{
		get
		{
			if (this._qualifiedItemId == null)
			{
				this._qualifiedItemId = this.TypeDefinitionId + this.ItemId;
			}
			return this._qualifiedItemId;
		}
	}

	/// <summary>The translated display name for the item, including metadata like the "(Recipe)" suffix.</summary>
	public abstract string DisplayName { get; }

	/// <summary>The internal (non-translated) name for the item.</summary>
	[XmlIgnore]
	public virtual string Name
	{
		get
		{
			return this.netName.Value;
		}
		set
		{
			this.netName.Value = value;
		}
	}

	/// <summary>The quantity of items in this item stack.</summary>
	[XmlIgnore]
	public virtual int Stack
	{
		get
		{
			return Math.Max(0, this.stack);
		}
		set
		{
			if (Game1.gameMode != 3)
			{
				this.stack.Value = value;
			}
			else
			{
				this.stack.Value = Math.Min(Math.Max(0, value), (value == int.MaxValue) ? value : this.maximumStackSize());
			}
		}
	}

	[XmlIgnore]
	public int Quality
	{
		get
		{
			return this.quality;
		}
		set
		{
			this.quality.Value = value;
		}
	}

	[XmlIgnore]
	public bool IsRecipe
	{
		get
		{
			return this.isRecipe;
		}
		set
		{
			this.isRecipe.Value = value;
		}
	}

	public bool IsInfiniteStock()
	{
		if (this.isLostItem)
		{
			return true;
		}
		return false;
	}

	public void MarkContextTagsDirty()
	{
		this._contextTagsDirty = true;
	}

	public HashSet<string> GetContextTags()
	{
		if (this._contextTags == null || this._contextTagsDirty)
		{
			this._GenerateContextTags();
		}
		return this._contextTags;
	}

	/// <summary>Get whether the item has the given context tag.</summary>
	/// <param name="tag">The tag to match. This can be negated by prefixing with <c>!</c> (like <c>!wine_item</c> to check if the tags <em>don't</em> contain <c>wine_item</c>).</param>
	public bool HasContextTag(string tag)
	{
		return ItemContextTagManager.DoesTagMatch(tag, this.GetContextTags());
	}

	protected void _GenerateContextTags()
	{
		this._contextTagsDirty = false;
		this._contextTags = new HashSet<string>(ItemContextTagManager.GetBaseContextTags(this.QualifiedItemId));
		this._PopulateContextTags(this._contextTags);
	}

	protected virtual void _PopulateContextTags(HashSet<string> tags)
	{
		switch (this.quality.Value)
		{
		case 0:
			tags.Add("quality_none");
			break;
		case 1:
			tags.Add("quality_silver");
			break;
		case 2:
			tags.Add("quality_gold");
			break;
		case 4:
			tags.Add("quality_iridium");
			break;
		case 3:
			break;
		}
	}

	protected Item()
	{
		this.initNetFields();
		this.parentSheetIndex.Value = -1;
	}

	/// <summary>Get whether the <see cref="F:StardewValley.Item.parentSheetIndex" /> field should be serialized.</summary>
	public virtual bool ShouldSerializeparentSheetIndex()
	{
		return this.parentSheetIndex.Value != -1;
	}

	/// <summary>Set the <see cref="P:StardewValley.Item.ItemId" /> to match the <see cref="P:StardewValley.Item.ParentSheetIndex" />, for vanilla items in saves created before Stardew Valley 1.6.</summary>
	protected virtual void MigrateLegacyItemId()
	{
		this.itemId.Value = this.ParentSheetIndex.ToString();
	}

	/// <summary>Initialize the collection of fields to sync in multiplayer.</summary>
	protected virtual void initNetFields()
	{
		this.NetFields.SetOwner(this).AddField(this.specialVariable, "specialVariable").AddField(this.category, "category")
			.AddField(this.netName, "netName")
			.AddField(this.parentSheetIndex, "parentSheetIndex")
			.AddField(this.hasbeenInInventory, "hasbeenInInventory")
			.AddField(this.itemId, "itemId")
			.AddField(this.stack, "stack")
			.AddField(this.quality, "quality")
			.AddField(this.isRecipe, "isRecipe")
			.AddField(this.modData, "modData");
		this.itemId.fieldChangeVisibleEvent += delegate
		{
			this._qualifiedItemId = null;
		};
	}

	/// <summary>Reset the parent sheet index to match the underlying item data.</summary>
	/// <remarks>
	///   <para>This should only be used for regular items (e.g. machines) before fully recalculating their offset.</para>
	///   <para>Limitations:</para>
	///   <list type="bullet">
	///     <item><description>This doesn't change <see cref="F:StardewValley.Object.showNextIndex" />.</description></item>
	///     <item><description>This doesn't reapply dynamic offset logic (e.g. seasonal items with season-specific sprite indexes).</description></item>
	///   </list>
	/// </remarks>
	public void ResetParentSheetIndex()
	{
		this.ParentSheetIndex = ItemRegistry.GetDataOrErrorItem(this.QualifiedItemId).SpriteIndex;
	}

	/// <summary>Validate that the given item ID is unqualified. This logs a warning if the item ID is qualified, and removes the qualifier if it matches <see cref="P:StardewValley.Item.TypeDefinitionId" />.</summary>
	/// <param name="id">The item ID to validate and normalize.</param>
	protected string ValidateUnqualifiedItemId(string id)
	{
		if (ItemRegistry.IsQualifiedItemId(id))
		{
			string qualifier = this.TypeDefinitionId;
			if (id.StartsWith(qualifier))
			{
				Game1.log.Warn($"The {base.GetType().FullName} constructor was called with qualified item ID '{id}'. The '{qualifier}' prefix will be removed automatically.");
				id = id.Substring(qualifier.Length).TrimStart();
			}
			else
			{
				Game1.log.Warn($"The {base.GetType().FullName} constructor was called with qualified item ID '{id}'. This will likely result in an error item.");
			}
		}
		return id;
	}

	/// <inheritdoc />
	public string GetItemTypeId()
	{
		return this.TypeDefinitionId;
	}

	public virtual void drawTooltip(SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
	{
		if (overrideText != null && overrideText.Length != 0 && (overrideText.Length != 1 || overrideText[0] != ' '))
		{
			spriteBatch.DrawString(font, overrideText, new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 2f), Game1.textShadowColor * alpha);
			spriteBatch.DrawString(font, overrideText, new Vector2(x + 16, y + 16 + 4) + new Vector2(0f, 2f), Game1.textShadowColor * alpha);
			spriteBatch.DrawString(font, overrideText, new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 0f), Game1.textShadowColor * alpha);
			spriteBatch.DrawString(font, overrideText, new Vector2(x + 16, y + 16 + 4), Game1.textColor * 0.9f * alpha);
			y += (int)font.MeasureString(overrideText).Y + 4;
		}
	}

	public virtual void ModifyItemBuffs(BuffEffects buffs)
	{
	}

	public virtual Point getExtraSpaceNeededForTooltipSpecialIcons(SpriteFont font, int minWidth, int horizontalBuffer, int startingHeight, StringBuilder descriptionText, string boldTitleText, int moneyAmountToDisplayAtBottom)
	{
		return Point.Zero;
	}

	public bool ShouldDrawIcon()
	{
		return true;
	}

	public abstract void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow);

	public void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber)
	{
		this.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, Color.White, drawShadow: true);
	}

	public void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth)
	{
		this.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, StackDrawType.Draw, Color.White, drawShadow: true);
	}

	public void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize)
	{
		this.drawInMenu(spriteBatch, location, scaleSize, 1f, 0.9f, StackDrawType.Draw, Color.White, drawShadow: true);
	}

	public abstract int maximumStackSize();

	public void AdjustMenuDrawForRecipes(ref float transparency, ref float scale)
	{
		if ((bool)this.isRecipe)
		{
			transparency = 0.5f;
			scale *= 0.75f;
		}
	}

	public virtual void DrawMenuIcons(SpriteBatch sb, Vector2 location, float scale_size, float transparency, float layer_depth, StackDrawType drawStackNumber, Color color)
	{
		int drawnStack = this.Stack;
		bool shouldDrawStackNumber = ((drawStackNumber == StackDrawType.Draw && this.maximumStackSize() > 1 && drawnStack > 1) || drawStackNumber == StackDrawType.Draw_OneInclusive) && (double)scale_size > 0.3 && drawnStack != int.MaxValue;
		if (this.IsRecipe)
		{
			shouldDrawStackNumber = false;
		}
		if (shouldDrawStackNumber)
		{
			Utility.drawTinyDigits(drawnStack, sb, location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(drawnStack, 3f * scale_size)) + 3f * scale_size, 64f - 18f * scale_size + 1f), 3f * scale_size, Math.Min(1f, layer_depth + 1E-06f), color);
		}
		if (drawStackNumber != 0 && (int)this.quality > 0)
		{
			Rectangle qualityRect = (((int)this.quality < 4) ? new Rectangle(338 + ((int)this.quality - 1) * 8, 400, 8, 8) : new Rectangle(346, 392, 8, 8));
			Texture2D qualitySheet = Game1.mouseCursors;
			float yOffset = (((int)this.quality < 4) ? 0f : (((float)Math.Cos((double)Game1.currentGameTime.TotalGameTime.Milliseconds * Math.PI / 512.0) + 1f) * 0.05f));
			sb.Draw(qualitySheet, location + new Vector2(12f, 52f + yOffset), qualityRect, color * transparency, 0f, new Vector2(4f, 4f), 3f * scale_size * (1f + yOffset), SpriteEffects.None, layer_depth);
		}
		else if (drawStackNumber != 0 && this.Category == -102 && Game1.player.stats.Get(this.itemId) != 0)
		{
			sb.Draw(Game1.mouseCursors_1_6, location + new Vector2(12f, 44f), new Rectangle(244, 271, 9, 11), color * transparency, 0f, new Vector2(4f, 4f), 3f * scale_size * 1f, SpriteEffects.None, layer_depth);
		}
		this.DrawIconBar(sb, location, scale_size, transparency, layer_depth, drawStackNumber, color);
		if ((bool)this.isRecipe)
		{
			sb.Draw(Game1.objectSpriteSheet, location + new Vector2(16f, 16f), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 451, 16, 16), color, 0f, Vector2.Zero, 3f, SpriteEffects.None, layer_depth + 0.0001f);
		}
	}

	public virtual void DrawIconBar(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color)
	{
	}

	/// <summary>Merge an item's stack into this one if they're stackable and this stack has room.</summary>
	/// <param name="otherStack">The other stack to merge into this one.</param>
	/// <returns>Returns the amount that couldn't be added to this stack (e.g. because it reached the maximum stack size or the provided item can't stack with this one).</returns>
	public virtual int addToStack(Item otherStack)
	{
		int maxStack = this.maximumStackSize();
		if (maxStack != 1)
		{
			this.stack.Value += otherStack.Stack;
			if (this is Object obj && otherStack is Object otherObject && obj.IsSpawnedObject && !otherObject.IsSpawnedObject)
			{
				obj.IsSpawnedObject = false;
			}
			if ((int)this.stack > maxStack)
			{
				int result = (int)this.stack - maxStack;
				this.stack.Value = maxStack;
				return result;
			}
			return 0;
		}
		return otherStack.Stack;
	}

	public abstract string getDescription();

	public abstract bool isPlaceable();

	/// <inheritdoc />
	public virtual int sellToStorePrice(long specificPlayerID = -1L)
	{
		return this.salePrice() / 2;
	}

	/// <inheritdoc />
	public virtual int salePrice(bool ignoreProfitMargins = false)
	{
		return -1;
	}

	/// <inheritdoc />
	public virtual bool appliesProfitMargins()
	{
		return false;
	}

	/// <summary>Whether the player can lose this item when they die.</summary>
	public virtual bool CanBeLostOnDeath()
	{
		if (this.canBeTrashed())
		{
			return !this.HasContextTag("prevent_loss_on_death");
		}
		return false;
	}

	public virtual bool canBeTrashed()
	{
		if (!this.specialItem)
		{
			if (this is Tool tool && (!(tool is MeleeWeapon) || tool.isScythe()) && !(tool is FishingRod) && !(tool is Pan))
			{
				return tool is Slingshot;
			}
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public virtual bool actionWhenPurchased(string shopId)
	{
		if (this.isLostItem)
		{
			Game1.player.itemsLostLastDeath.Clear();
			this.isLostItem = false;
			Game1.player.recoveredItem = this;
			Game1.player.mailReceived.Remove("MarlonRecovery");
			Game1.addMailForTomorrow("MarlonRecovery");
			Game1.playSound("newArtifact");
			Game1.exitActiveMenu();
			bool use_plural = this.Stack > 1;
			Game1.DrawDialogue(Game1.getCharacterFromName("Marlon"), use_plural ? "Strings\\StringsFromCSFiles:ItemRecovery_Engaged_Stack" : "Strings\\StringsFromCSFiles:ItemRecovery_Engaged", Lexicon.makePlural(this.DisplayName, !use_plural));
			return true;
		}
		return false;
	}

	public virtual bool CanBuyItem(Farmer who)
	{
		return Game1.player.couldInventoryAcceptThisItem(this);
	}

	public virtual bool canBeDropped()
	{
		return true;
	}

	/// <summary>Detach the item from its net sync parent when it's removed from an inventory/player/container. This avoids 'changing net field parent' warnings when it's added to a new parent.</summary>
	public virtual void onDetachedFromParent()
	{
		this.NetFields.Parent = null;
	}

	/// <summary>Handle the item being equipped by the player (i.e. added to an equipment slot, or selected as the active tool).</summary>
	/// <param name="who">The player who equipped the item.</param>
	public virtual void onEquip(Farmer who)
	{
	}

	/// <summary>Handle the item being unequipped by the player (i.e. removed from an equipment slot, or deselected as the active tool).</summary>
	/// <param name="who">The player who unequipped the item.</param>
	public virtual void onUnequip(Farmer who)
	{
	}

	public virtual void actionWhenBeingHeld(Farmer who)
	{
	}

	public virtual void actionWhenStopBeingHeld(Farmer who)
	{
	}

	public int getRemainingStackSpace()
	{
		return this.maximumStackSize() - this.Stack;
	}

	public Item ConsumeStack(int amount)
	{
		if (amount == 0)
		{
			return this;
		}
		if (this.Stack - amount <= 0)
		{
			return null;
		}
		this.Stack -= amount;
		return this;
	}

	public virtual int healthRecoveredOnConsumption()
	{
		return 0;
	}

	public virtual int staminaRecoveredOnConsumption()
	{
		return 0;
	}

	public virtual string getHoverBoxText(Item hoveredItem)
	{
		return null;
	}

	public virtual bool canBeGivenAsGift()
	{
		return false;
	}

	public virtual void drawAttachments(SpriteBatch b, int x, int y)
	{
	}

	public virtual bool canBePlacedHere(GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false)
	{
		return false;
	}

	public virtual int attachmentSlots()
	{
		return 0;
	}

	/// <summary>Get the translated display name for the item's category, or an empty string if there is none.</summary>
	public virtual string getCategoryName()
	{
		return Object.GetCategoryDisplayName(this.Category);
	}

	/// <summary>Get the display color for the item's category.</summary>
	public virtual Color getCategoryColor()
	{
		return Object.GetCategoryColor(this.Category);
	}

	public virtual bool canStackWith(ISalable other)
	{
		if (!(other is Item otherItem) || other.GetType() != base.GetType())
		{
			return false;
		}
		if (this is ColoredObject coloredObj && other is ColoredObject otherColoredObj && !coloredObj.color.Value.Equals(otherColoredObj.color.Value))
		{
			return false;
		}
		if (this.maximumStackSize() <= 1 || other.maximumStackSize() <= 1)
		{
			return false;
		}
		if (this is Object obj && other is Object otherObj && otherObj.orderData.Value != obj.orderData.Value)
		{
			return false;
		}
		if (this.quality.Value != otherItem.quality.Value)
		{
			return false;
		}
		if (this.QualifiedItemId != otherItem.QualifiedItemId)
		{
			return false;
		}
		if (!this.Name.Equals(other.Name))
		{
			return false;
		}
		return true;
	}

	public virtual string checkForSpecialItemHoldUpMeessage()
	{
		return null;
	}

	/// <summary>Get a new copy of this item, with its stack size reset to one.</summary>
	public Item getOne()
	{
		Item oneNew = this.GetOneNew();
		oneNew.GetOneCopyFrom(this);
		return oneNew;
	}

	/// <summary>Called from <see cref="M:StardewValley.Item.getOne" /> to create a minimal instance of the same item type.</summary>
	/// <remarks>
	///   Items are cloned in two steps:
	///   <list type="number">
	///    <item><description><see cref="M:StardewValley.Item.GetOneNew" /> is called on the original item to create a new instance of the same type.</description></item>
	///    <item><description><see cref="M:StardewValley.Item.GetOneCopyFrom(StardewValley.Item)" /> is called on the new item to copy its values from the original.</description></item>
	///   </list>
	///
	///   This separation is needed to let subclasses extend the base copy logic without needing to duplicate it, so <see cref="M:StardewValley.Item.GetOneNew" /> shouldn't copy any values beyond calling the constructor.
	/// </remarks>
	protected abstract Item GetOneNew();

	/// <summary>Called from <see cref="M:StardewValley.Item.getOne" /> to copy values from the original instance.</summary>
	/// <param name="source">The original item instance to copy from.</param>
	/// <inheritdoc cref="M:StardewValley.Item.GetOneNew" path="/remarks" />
	protected virtual void GetOneCopyFrom(Item source)
	{
		this.ItemId = source.ItemId;
		this.IsRecipe = source.isRecipe;
		this.Quality = source.quality;
		this.Stack = 1;
		this.HasBeenInInventory = source.HasBeenInInventory;
		this.SpecialVariable = source.SpecialVariable;
		this.modData.Clear();
		foreach (string key in source.modData.Keys)
		{
			this.modData[key] = source.modData[key];
		}
	}

	public ISalable GetSalableInstance()
	{
		return this.getOne();
	}

	public virtual int CompareTo(object other)
	{
		if (other is Item otherItem)
		{
			if (otherItem.Category == this.Category)
			{
				if ((otherItem.Name.Equals(this.Name) && otherItem.QualifiedItemId == this.QualifiedItemId) || (this.Name == "" && otherItem.DisplayName.Equals(this.DisplayName) && otherItem.QualifiedItemId == this.QualifiedItemId))
				{
					int quality_comparison = otherItem.Quality.CompareTo(this.Quality);
					if (quality_comparison != 0)
					{
						return quality_comparison;
					}
					return this.Stack - otherItem.Stack;
				}
				string thisName = ((this.Name == "") ? this.DisplayName : this.Name);
				string otherName = ((otherItem.Name == "") ? otherItem.DisplayName : otherItem.Name);
				if (this is Object curObj && other is Object otherObj)
				{
					if (curObj.HasContextTag("use_reverse_name_for_sorting") || curObj is Trinket)
					{
						List<string> split2 = new List<string>(thisName.Split(' '));
						split2.Reverse();
						thisName = string.Join("", split2);
					}
					if (otherObj.HasContextTag("use_reverse_name_for_sorting") || otherObj is Trinket)
					{
						List<string> split = new List<string>(otherName.Split(' '));
						split.Reverse();
						otherName = string.Join("", split);
					}
					return string.Compare(string.Concat(curObj.type, thisName), string.Concat(otherObj.type, otherName));
				}
				return string.Compare(thisName, otherItem.Name);
			}
			return otherItem.getCategorySortValue() - this.getCategorySortValue();
		}
		return 0;
	}

	public int getCategorySortValue()
	{
		if (this.Category == -100)
		{
			return -94;
		}
		return this.Category;
	}

	protected virtual int getDescriptionWidth()
	{
		int minimum_size = 272;
		if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.fr)
		{
			minimum_size = 384;
		}
		if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.tr)
		{
			minimum_size = 336;
		}
		return Math.Max(minimum_size, (int)Game1.dialogueFont.MeasureString((this.DisplayName == null) ? "" : this.DisplayName).X);
	}

	/// <summary>Set a temporary value that's not synchronized or saved, and can be read from <see cref="F:StardewValley.Item.tempData" />.</summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="key">The temporary data kay.</param>
	/// <param name="value">The value to save.</param>
	public void SetTempData<T>(string key, T value)
	{
		if (this.tempData == null)
		{
			this.tempData = new Dictionary<string, object>();
		}
		this.tempData[key] = value;
	}

	/// <summary>Get a temporary value that was added via <see cref="M:StardewValley.Item.SetTempData``1(System.String,``0)" />, if found.</summary>
	/// <typeparam name="T">The expected value type.</typeparam>
	/// <param name="key">The temporary data kay.</param>
	/// <param name="value">The value that was saved, if found.</param>
	/// <returns>Returns whether the <paramref name="key" /> was found.</returns>
	public bool TryGetTempData<T>(string key, out T value)
	{
		if (this.tempData == null || !this.tempData.TryGetValue(key, out var rawValue))
		{
			value = default(T);
			return false;
		}
		if (rawValue == null)
		{
			value = default(T);
			return value == null;
		}
		if (rawValue is T parsed)
		{
			value = parsed;
			return true;
		}
		value = default(T);
		return false;
	}

	/// <summary>Ensure the <see cref="F:StardewValley.Item.stack" /> is set to a valid value.</summary>
	public virtual void FixStackSize()
	{
		this.stack.Value = Utility.Clamp(this.stack.Value, 1, this.maximumStackSize());
	}

	/// <summary>Ensure the <see cref="F:StardewValley.Item.quality" /> is set to a valid value.</summary>
	public virtual void FixQuality()
	{
		this.quality.Value = Utility.Clamp(this.quality.Value, 0, 4);
		if (this.quality.Value == 3)
		{
			this.quality.Value = 4;
		}
	}

	public virtual void resetState()
	{
	}

	public virtual bool HasEquipmentBuffs()
	{
		BuffEffects effects = new BuffEffects();
		this.AddEquipmentEffects(effects);
		return effects.HasAnyValue();
	}

	public virtual void AddEquipmentEffects(BuffEffects effects)
	{
	}

	public virtual IEnumerable<Buff> GetFoodOrDrinkBuffs()
	{
		return LegacyShims.EmptyArray<Buff>();
	}

	/// <summary>Perform an action for each item contained within this item (e.g. items stored within a chest, item placed on a table, etc). This doesn't include the item itself.</summary>
	/// <param name="handler">The action to perform for each item.</param>
	/// <returns>Returns whether to continue iterating.</returns>
	public virtual bool ForEachItem(ForEachItemDelegate handler)
	{
		return true;
	}
}
