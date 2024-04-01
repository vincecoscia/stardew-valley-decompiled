using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.TerrainFeatures;

namespace StardewValley.Objects;

[XmlInclude(typeof(BedFurniture))]
[XmlInclude(typeof(RandomizedPlantFurniture))]
[XmlInclude(typeof(StorageFurniture))]
[XmlInclude(typeof(TV))]
public class Furniture : Object, ISittable
{
	public const int chair = 0;

	public const int bench = 1;

	public const int couch = 2;

	public const int armchair = 3;

	public const int dresser = 4;

	public const int longTable = 5;

	public const int painting = 6;

	public const int lamp = 7;

	public const int decor = 8;

	public const int other = 9;

	public const int bookcase = 10;

	public const int table = 11;

	public const int rug = 12;

	public const int window = 13;

	public const int fireplace = 14;

	public const int bed = 15;

	public const int torch = 16;

	public const int sconce = 17;

	public const string furnitureTextureName = "TileSheets\\furniture";

	[XmlElement("furniture_type")]
	public readonly NetInt furniture_type = new NetInt();

	[XmlElement("rotations")]
	public readonly NetInt rotations = new NetInt();

	[XmlElement("currentRotation")]
	public readonly NetInt currentRotation = new NetInt();

	[XmlElement("sourceIndexOffset")]
	private readonly NetInt sourceIndexOffset = new NetInt();

	[XmlElement("drawPosition")]
	protected readonly NetVector2 drawPosition = new NetVector2();

	[XmlElement("sourceRect")]
	public readonly NetRectangle sourceRect = new NetRectangle();

	[XmlElement("defaultSourceRect")]
	public readonly NetRectangle defaultSourceRect = new NetRectangle();

	[XmlElement("defaultBoundingBox")]
	public readonly NetRectangle defaultBoundingBox = new NetRectangle();

	[XmlElement("drawHeldObjectLow")]
	public readonly NetBool drawHeldObjectLow = new NetBool();

	[XmlIgnore]
	public NetLongDictionary<int, NetInt> sittingFarmers = new NetLongDictionary<int, NetInt>();

	[XmlIgnore]
	public Vector2? lightGlowPosition;

	/// <summary>Whether this furniture can be removed if other checks pass.</summary>
	/// <remarks>This value only applies for the current instance, it's not synced in multiplayer or written to the save file.</remarks>
	[XmlIgnore]
	public bool AllowLocalRemoval = true;

	public static bool isDrawingLocationFurniture;

	protected static Dictionary<string, string> _frontTextureName;

	[XmlIgnore]
	private int _placementRestriction = -1;

	[XmlIgnore]
	private string _description;

	[XmlIgnore]
	public int placementRestriction
	{
		get
		{
			if (this._placementRestriction < 0)
			{
				bool use_default = true;
				string[] data = this.getData();
				if (data != null && data.Length > 6 && int.TryParse(data[6], out this._placementRestriction) && this._placementRestriction >= 0)
				{
					use_default = false;
				}
				if (use_default)
				{
					if (base.name.Contains("TV"))
					{
						this._placementRestriction = 0;
					}
					else if (this.IsTable() || this.furniture_type.Value == 1 || this.furniture_type.Value == 0 || this.furniture_type.Value == 8 || this.furniture_type.Value == 16)
					{
						this._placementRestriction = 2;
					}
					else
					{
						this._placementRestriction = 0;
					}
				}
			}
			return this._placementRestriction;
		}
	}

	[XmlIgnore]
	public string description
	{
		get
		{
			if (this._description == null)
			{
				this._description = this.loadDescription();
			}
			return this._description;
		}
	}

	/// <inheritdoc />
	public override string TypeDefinitionId { get; } = "(F)";


	/// <inheritdoc />
	public override string Name => base.name;

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(this.furniture_type, "furniture_type").AddField(this.rotations, "rotations").AddField(this.currentRotation, "currentRotation")
			.AddField(this.sourceIndexOffset, "sourceIndexOffset")
			.AddField(this.drawPosition, "drawPosition")
			.AddField(this.sourceRect, "sourceRect")
			.AddField(this.defaultSourceRect, "defaultSourceRect")
			.AddField(this.defaultBoundingBox, "defaultBoundingBox")
			.AddField(this.drawHeldObjectLow, "drawHeldObjectLow")
			.AddField(this.sittingFarmers, "sittingFarmers");
	}

	public Furniture()
	{
		this.updateDrawPosition();
		base.isOn.Value = false;
	}

	public Furniture(string itemId, Vector2 tile, int initialRotations)
		: this(itemId, tile)
	{
		for (int i = 0; i < initialRotations; i++)
		{
			this.rotate();
		}
		base.isOn.Value = false;
	}

	public virtual void OnAdded(GameLocation loc, Vector2 tilePos)
	{
		if (this.IntersectsForCollision(Game1.player.GetBoundingBox()))
		{
			Game1.player.TemporaryPassableTiles.Add(this.GetBoundingBoxAt((int)tilePos.X, (int)tilePos.Y));
		}
		if ((int)this.furniture_type == 13)
		{
			if (loc != null && loc.IsRainingHere())
			{
				this.sourceRect.Value = this.defaultSourceRect.Value;
				this.sourceIndexOffset.Value = 1;
			}
			else
			{
				this.sourceRect.Value = this.defaultSourceRect.Value;
				this.sourceIndexOffset.Value = 0;
				this.AddLightGlow();
			}
		}
		this.minutesElapsed(1);
	}

	public void OnRemoved(GameLocation loc, Vector2 tilePos)
	{
		this.RemoveLightGlow();
	}

	public override bool IsHeldOverHead()
	{
		return false;
	}

	/// <summary>Whether this is a table, which can have items placed on it.</summary>
	public virtual bool IsTable()
	{
		int furnitureType = this.furniture_type.Value;
		if (furnitureType != 11)
		{
			return furnitureType == 5;
		}
		return true;
	}

	public static Rectangle GetDefaultSourceRect(string itemId, Texture2D texture = null)
	{
		ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem("(F)" + itemId);
		string[] rawData = Furniture.getData(itemId);
		if (rawData == null)
		{
			return itemData.GetSourceRect();
		}
		if (rawData[2].Equals("-1"))
		{
			return Furniture.getDefaultSourceRectForType(itemData, Furniture.getTypeNumberFromName(rawData[1]), texture);
		}
		string[] array = ArgUtility.SplitBySpace(rawData[2]);
		int width = Convert.ToInt32(array[0]);
		int height = Convert.ToInt32(array[1]);
		return Furniture.getDefaultSourceRect(itemData, width, height, texture);
	}

	/// <summary>Set the furniture's position and rotation, and update all related data.</summary>
	/// <param name="x">The tile X position.</param>
	/// <param name="y">The tile X position.</param>
	/// <param name="rotations">The number of times to rotate the furniture, starting from its current rotation.</param>
	/// <returns>Returns the furniture instance for chaining.</returns>
	public Furniture SetPlacement(int x, int y, int rotations = 0)
	{
		return this.SetPlacement(new Vector2(x, y), rotations);
	}

	/// <summary>Set the furniture's position and rotation, and update all related data.</summary>
	/// <param name="tile">The tile position.</param>
	/// <param name="rotations">The number of times to rotate the furniture, starting from its current rotation.</param>
	/// <returns>Returns the furniture instance for chaining.</returns>
	public Furniture SetPlacement(Point tile, int rotations = 0)
	{
		return this.SetPlacement(Utility.PointToVector2(tile), rotations);
	}

	/// <summary>Set the furniture's position and rotation, and update all related data.</summary>
	/// <param name="tile">The tile position.</param>
	/// <param name="rotations">The number of times to rotate the furniture, starting from its current rotation.</param>
	/// <returns>Returns the furniture instance for chaining.</returns>
	public Furniture SetPlacement(Vector2 tile, int rotations = 0)
	{
		this.InitializeAtTile(tile);
		for (int i = 0; i < rotations; i++)
		{
			this.rotate();
		}
		return this;
	}

	/// <summary>Set the held object.</summary>
	/// <param name="obj">The object to hold.</param>
	/// <returns>Returns the furniture instance for chaining.</returns>
	public Furniture SetHeldObject(Object obj)
	{
		base.heldObject.Value = obj;
		if (obj != null)
		{
			if (obj is Furniture furniture)
			{
				furniture.InitializeAtTile(this.TileLocation);
			}
			else
			{
				obj.TileLocation = this.TileLocation;
			}
		}
		return this;
	}

	/// <summary>Set the furniture's tile position and update all position-related data.</summary>
	/// <param name="tile">The tile position.</param>
	public void InitializeAtTile(Vector2 tile)
	{
		Texture2D texture = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).GetTexture();
		string[] data = this.getData();
		if (data != null)
		{
			this.furniture_type.Value = Furniture.getTypeNumberFromName(data[1]);
			this.defaultSourceRect.Value = new Rectangle(base.ParentSheetIndex * 16 % texture.Width, base.ParentSheetIndex * 16 / texture.Width * 16, 1, 1);
			this.drawHeldObjectLow.Value = this.Name.ToLower().Contains("tea");
			this.sourceRect.Value = Furniture.GetDefaultSourceRect(base.ItemId);
			this.defaultSourceRect.Value = this.sourceRect.Value;
			this.rotations.Value = Convert.ToInt32(data[4]);
			base.price.Value = Convert.ToInt32(data[5]);
		}
		else
		{
			this.defaultSourceRect.Value = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).GetSourceRect();
		}
		if (tile != this.TileLocation)
		{
			this.TileLocation = tile;
		}
		else
		{
			this.RecalculateBoundingBox(data);
		}
	}

	public Furniture(string itemId, Vector2 tile)
	{
		base.isOn.Value = false;
		base.ItemId = itemId;
		base.ResetParentSheetIndex();
		base.name = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).InternalName;
		this.InitializeAtTile(tile);
	}

	/// <inheritdoc />
	public override void RecalculateBoundingBox()
	{
		this.RecalculateBoundingBox(this.getData());
	}

	/// <summary>Recalculate the item's bounding box based on its current position.</summary>
	/// <param name="data">The furniture data to apply.</param>
	private void RecalculateBoundingBox(string[] data)
	{
		string rawSize = ArgUtility.Get(data, 3);
		Rectangle box;
		if (rawSize != null)
		{
			if (rawSize == "-1")
			{
				box = this.getDefaultBoundingBoxForType(this.furniture_type.Value);
			}
			else
			{
				string[] sizeParts = ArgUtility.SplitBySpace(data[3]);
				box = new Rectangle((int)base.tileLocation.X * 64, (int)base.tileLocation.Y * 64, Convert.ToInt32(sizeParts[0]) * 64, Convert.ToInt32(sizeParts[1]) * 64);
			}
		}
		else
		{
			box = new Rectangle((int)base.tileLocation.X * 64, (int)base.tileLocation.Y * 64, 64, 64);
		}
		this.defaultBoundingBox.Value = box;
		base.boundingBox.Value = box;
		this.updateRotation();
	}

	protected string[] getData()
	{
		return Furniture.getData(base.ItemId);
	}

	protected static string[] getData(string itemId)
	{
		if (!DataLoader.Furniture(Game1.content).TryGetValue(itemId, out var rawData))
		{
			return null;
		}
		return rawData.Split('/');
	}

	/// <inheritdoc />
	protected override string loadDisplayName()
	{
		return ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).DisplayName;
	}

	protected virtual string loadDescription()
	{
		ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		if (itemData.IsErrorItem)
		{
			return itemData.Description;
		}
		return base.QualifiedItemId switch
		{
			"(F)TrashCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:TrashCatalogueDescription"), 
			"(F)RetroCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:RetroCatalogueDescription"), 
			"(F)JunimoCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:JunimoCatalogueDescription"), 
			"(F)WizardCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:WizardCatalogueDescription"), 
			"(F)JojaCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:JojaCatalogueDescription"), 
			"(F)1308" => Game1.parseText(Game1.content.LoadString("Strings\\Objects:CatalogueDescription"), Game1.smallFont, 320), 
			"(F)1226" => Game1.parseText(Game1.content.LoadString("Strings\\Objects:FurnitureCatalogueDescription"), Game1.smallFont, 320), 
			_ => this.placementRestriction switch
			{
				0 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_NotOutdoors"), 
				1 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_Outdoors_Description"), 
				2 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_Decoration_Description"), 
				_ => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12623"), 
			}, 
		};
	}

	public override string getDescription()
	{
		return Game1.parseText(this.description, Game1.smallFont, this.getDescriptionWidth());
	}

	/// <inheritdoc />
	public override Color getCategoryColor()
	{
		return new Color(100, 25, 190);
	}

	/// <inheritdoc />
	public override bool performDropDownAction(Farmer who)
	{
		this.actionOnPlayerEntryOrPlacement(this.Location, dropDown: true);
		return false;
	}

	public override void hoverAction()
	{
		base.hoverAction();
		if (!Game1.player.isInventoryFull())
		{
			Game1.mouseCursor = Game1.cursor_grab;
		}
	}

	/// <inheritdoc />
	public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		GameLocation location = this.Location;
		if (location == null)
		{
			return false;
		}
		if (justCheckingForActivity)
		{
			return true;
		}
		switch (base.QualifiedItemId)
		{
		case "(F)Cauldron":
			base.IsOn = !base.IsOn;
			base.SpecialVariable = (base.IsOn ? 388859 : 0);
			if (base.IsOn)
			{
				location.playSound("fireball");
				location.playSound("bubbles");
				for (int i = 0; i < 13; i++)
				{
					this.addCauldronBubbles(-0.5f - (float)i * 0.2f);
				}
			}
			break;
		case "(F)1402":
			Game1.activeClickableMenu = new Billboard();
			return true;
		case "(F)RetroCatalogue":
			Utility.TryOpenShopMenu("RetroFurnitureCatalogue", location);
			break;
		case "(F)TrashCatalogue":
			Utility.TryOpenShopMenu("TrashFurnitureCatalogue", location);
			break;
		case "(F)JunimoCatalogue":
			Utility.TryOpenShopMenu("JunimoFurnitureCatalogue", location);
			break;
		case "(F)WizardCatalogue":
			if (!Game1.player.mailReceived.Contains("WizardCatalogue"))
			{
				Game1.player.mailReceived.Add("WizardCatalogue");
				Game1.activeClickableMenu = new LetterViewerMenu(Game1.content.LoadString("Strings\\1_6_Strings:WizardCatalogueLetter"))
				{
					whichBG = 2
				};
			}
			else
			{
				Utility.TryOpenShopMenu("WizardFurnitureCatalogue", location);
			}
			return true;
		case "(F)JojaCatalogue":
			if (!Game1.player.mailReceived.Contains("JojaThriveTerms"))
			{
				Game1.player.mailReceived.Add("JojaThriveTerms");
				Game1.activeClickableMenu = new LetterViewerMenu(Game1.content.LoadString("Strings\\1_6_Strings:JojaCatalogueDescriptionTerms"))
				{
					whichBG = 4
				};
			}
			else
			{
				Utility.TryOpenShopMenu("JojaFurnitureCatalogue", location);
			}
			return true;
		case "(F)1308":
			Utility.TryOpenShopMenu("Catalogue", location);
			return true;
		case "(F)1226":
			Utility.TryOpenShopMenu("Furniture Catalogue", location);
			return true;
		case "(F)1309":
			Game1.playSound("openBox");
			base.shakeTimer = 500;
			if (Game1.getMusicTrackName().Equals("sam_acoustic1"))
			{
				Game1.changeMusicTrack("none", track_interruptable: true);
			}
			else
			{
				Game1.changeMusicTrack("sam_acoustic1");
			}
			return true;
		}
		if ((int)this.furniture_type == 14 || (int)this.furniture_type == 16)
		{
			base.isOn.Value = !base.isOn.Value;
			this.initializeLightSource(base.tileLocation.Value);
			this.setFireplace(playSound: true, broadcast: true);
			return true;
		}
		if (this.GetSeatCapacity() > 0)
		{
			who.BeginSitting(this);
			return true;
		}
		return this.clicked(who);
	}

	public virtual void setFireplace(bool playSound = true, bool broadcast = false)
	{
		GameLocation location = this.Location;
		if (location == null)
		{
			return;
		}
		if ((bool)base.isOn)
		{
			if (base.lightSource == null)
			{
				this.initializeLightSource(base.tileLocation.Value);
			}
			if (base.lightSource != null && (bool)base.isOn && !location.hasLightSource(base.lightSource.Identifier))
			{
				location.sharedLights[base.lightSource.identifier] = base.lightSource.Clone();
			}
			if (playSound)
			{
				location.localSound("fireball");
			}
			AmbientLocationSounds.addSound(new Vector2(base.tileLocation.X, base.tileLocation.Y), 1);
		}
		else
		{
			if (playSound)
			{
				location.localSound("fireball");
			}
			base.performRemoveAction();
			AmbientLocationSounds.removeSound(new Vector2(base.tileLocation.X, base.tileLocation.Y));
		}
	}

	public virtual void AttemptRemoval(Action<Furniture> removal_action)
	{
		removal_action?.Invoke(this);
	}

	public virtual bool canBeRemoved(Farmer who)
	{
		if (!this.AllowLocalRemoval)
		{
			return false;
		}
		GameLocation location = this.Location;
		if (location == null)
		{
			return false;
		}
		if (this.HasSittingFarmers())
		{
			return false;
		}
		if (base.heldObject.Value != null)
		{
			return false;
		}
		Rectangle bounds = base.GetBoundingBox();
		if (this.isPassable())
		{
			for (int x = bounds.Left / 64; x < bounds.Right / 64; x++)
			{
				for (int y = bounds.Top / 64; y < bounds.Bottom / 64; y++)
				{
					Furniture tileFurniture = location.GetFurnitureAt(new Vector2(x, y));
					if (tileFurniture != null && tileFurniture != this)
					{
						return false;
					}
					if (location.objects.ContainsKey(new Vector2(x, y)))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public override bool clicked(Farmer who)
	{
		Game1.haltAfterCheck = false;
		if ((int)this.furniture_type == 11 && who.ActiveObject != null && base.heldObject.Value == null)
		{
			return false;
		}
		if (base.heldObject.Value != null)
		{
			Object item = base.heldObject.Value;
			base.heldObject.Value = null;
			if (who.addItemToInventoryBool(item))
			{
				item.performRemoveAction();
				Game1.playSound("coin");
				return true;
			}
			base.heldObject.Value = item;
		}
		return false;
	}

	public virtual int GetSeatCapacity()
	{
		if (base.QualifiedItemId.Equals("(F)UprightPiano") || base.QualifiedItemId.Equals("(F)DarkPiano"))
		{
			return 1;
		}
		if ((int)this.furniture_type == 0)
		{
			return 1;
		}
		if ((int)this.furniture_type == 1)
		{
			return 2;
		}
		if ((int)this.furniture_type == 2)
		{
			return this.defaultBoundingBox.Width / 64 - 1;
		}
		if ((int)this.furniture_type == 3)
		{
			return 1;
		}
		return 0;
	}

	public virtual bool IsSeatHere(GameLocation location)
	{
		return location.furniture.Contains(this);
	}

	public virtual bool IsSittingHere(Farmer who)
	{
		return this.sittingFarmers.ContainsKey(who.UniqueMultiplayerID);
	}

	public virtual Vector2? GetSittingPosition(Farmer who, bool ignore_offsets = false)
	{
		if (this.sittingFarmers.TryGetValue(who.UniqueMultiplayerID, out var key))
		{
			return this.GetSeatPositions(ignore_offsets)[key];
		}
		return null;
	}

	public virtual bool HasSittingFarmers()
	{
		return this.sittingFarmers.Length > 0;
	}

	public virtual void RemoveSittingFarmer(Farmer farmer)
	{
		this.sittingFarmers.Remove(farmer.UniqueMultiplayerID);
	}

	public virtual int GetSittingFarmerCount()
	{
		return this.sittingFarmers.Length;
	}

	public virtual Rectangle GetSeatBounds()
	{
		Rectangle bounds = base.GetBoundingBox();
		return new Rectangle(bounds.X / 64, bounds.Y / 64, bounds.Width / 64, bounds.Height / 64);
	}

	public virtual int GetSittingDirection()
	{
		if (this.Name.Contains("Stool"))
		{
			return Game1.player.FacingDirection;
		}
		if (base.QualifiedItemId.Equals("(F)UprightPiano") || base.QualifiedItemId.Equals("(F)DarkPiano"))
		{
			return 0;
		}
		return this.currentRotation.Value switch
		{
			0 => 2, 
			1 => 1, 
			2 => 0, 
			3 => 3, 
			_ => 2, 
		};
	}

	public virtual Vector2? AddSittingFarmer(Farmer who)
	{
		List<Vector2> seat_positions = this.GetSeatPositions();
		int seat_index = -1;
		Vector2? sit_position = null;
		float distance = 96f;
		Vector2 playerPixel = who.getStandingPosition();
		for (int i = 0; i < seat_positions.Count; i++)
		{
			if (!this.sittingFarmers.Values.Contains(i))
			{
				float curr_distance = ((seat_positions[i] + new Vector2(0.5f, 0.5f)) * 64f - playerPixel).Length();
				if (curr_distance < distance)
				{
					distance = curr_distance;
					sit_position = seat_positions[i];
					seat_index = i;
				}
			}
		}
		if (sit_position.HasValue)
		{
			this.sittingFarmers[who.UniqueMultiplayerID] = seat_index;
		}
		return sit_position;
	}

	public virtual List<Vector2> GetSeatPositions(bool ignore_offsets = false)
	{
		List<Vector2> seat_positions = new List<Vector2>();
		if (base.QualifiedItemId.Equals("(F)UprightPiano") || base.QualifiedItemId.Equals("(F)DarkPiano"))
		{
			seat_positions.Add(this.TileLocation + new Vector2(1.5f, 0f));
		}
		if ((int)this.furniture_type == 0)
		{
			seat_positions.Add(this.TileLocation);
		}
		if ((int)this.furniture_type == 1)
		{
			for (int x = 0; x < this.getTilesWide(); x++)
			{
				for (int y = 0; y < this.getTilesHigh(); y++)
				{
					seat_positions.Add(this.TileLocation + new Vector2(x, y));
				}
			}
		}
		if ((int)this.furniture_type == 2)
		{
			int width = this.defaultBoundingBox.Width / 64 - 1;
			if ((int)this.currentRotation == 0 || (int)this.currentRotation == 2)
			{
				seat_positions.Add(this.TileLocation + new Vector2(0.5f, 0f));
				for (int i = 1; i < width - 1; i++)
				{
					seat_positions.Add(this.TileLocation + new Vector2((float)i + 0.5f, 0f));
				}
				seat_positions.Add(this.TileLocation + new Vector2((float)(width - 1) + 0.5f, 0f));
			}
			else if ((int)this.currentRotation == 1)
			{
				for (int j = 0; j < width; j++)
				{
					seat_positions.Add(this.TileLocation + new Vector2(1f, j));
				}
			}
			else
			{
				for (int k = 0; k < width; k++)
				{
					seat_positions.Add(this.TileLocation + new Vector2(0f, k));
				}
			}
		}
		if ((int)this.furniture_type == 3)
		{
			if ((int)this.currentRotation == 0 || (int)this.currentRotation == 2)
			{
				seat_positions.Add(this.TileLocation + new Vector2(0.5f, 0f));
			}
			else if ((int)this.currentRotation == 1)
			{
				seat_positions.Add(this.TileLocation + new Vector2(1f, 0f));
			}
			else
			{
				seat_positions.Add(this.TileLocation + new Vector2(0f, 0f));
			}
		}
		return seat_positions;
	}

	public bool timeToTurnOnLights()
	{
		if (this.Location != null)
		{
			if (!this.Location.IsRainingHere())
			{
				return Game1.timeOfDay >= Game1.getTrulyDarkTime(this.Location) - 100;
			}
			return true;
		}
		return false;
	}

	public override void DayUpdate()
	{
		base.DayUpdate();
		this.sittingFarmers.Clear();
		if (this.Location.IsRainingHere())
		{
			this.addLights();
		}
		else if (!this.timeToTurnOnLights() || Game1.newDay)
		{
			this.removeLights();
		}
		else
		{
			this.addLights();
		}
		this.RemoveLightGlow();
		if (Game1.IsMasterGame && Game1.season == Season.Winter && Game1.dayOfMonth == 25 && ((int)this.furniture_type == 11 || (int)this.furniture_type == 5) && base.heldObject.Value != null)
		{
			if (base.heldObject.Value.QualifiedItemId == "(O)223" && !Game1.player.mailReceived.Contains("CookiePresent_year" + Game1.year))
			{
				base.heldObject.Value = ItemRegistry.Create<Object>("(O)MysteryBox");
				Game1.player.mailReceived.Add("CookiePresent_year" + Game1.year);
			}
			else if (base.heldObject.Value.Category == -6 && !Game1.player.mailReceived.Contains("MilkPresent_year" + Game1.year))
			{
				base.heldObject.Value = ItemRegistry.Create<Object>("(O)MysteryBox");
				Game1.player.mailReceived.Add("MilkPresent_year" + Game1.year);
			}
		}
	}

	public virtual void AddLightGlow()
	{
		GameLocation location = this.Location;
		if (location != null && !this.lightGlowPosition.HasValue)
		{
			Vector2 light_glow_position = new Vector2(base.boundingBox.X + 32, base.boundingBox.Y + 64);
			if (!location.lightGlows.Contains(light_glow_position))
			{
				this.lightGlowPosition = light_glow_position;
				location.lightGlows.Add(light_glow_position);
			}
		}
	}

	public virtual void RemoveLightGlow()
	{
		GameLocation location = this.Location;
		if (location != null)
		{
			if (this.lightGlowPosition.HasValue && location.lightGlows.Contains(this.lightGlowPosition.Value))
			{
				location.lightGlows.Remove(this.lightGlowPosition.Value);
			}
			location.lightGlowLayerCache.Clear();
			this.lightGlowPosition = null;
		}
	}

	/// <inheritdoc />
	public override void actionOnPlayerEntry()
	{
		base.actionOnPlayerEntry();
		this.actionOnPlayerEntryOrPlacement(this.Location, dropDown: false);
		if (this.Location == null || !base.QualifiedItemId.Equals("(F)BirdHouse") || !this.Location.isOutdoors || Game1.isRaining || Game1.timeOfDay >= Game1.getStartingToGetDarkTime(this.Location))
		{
			return;
		}
		Random r = Utility.CreateDaySaveRandom(this.TileLocation.X * 74797f, this.TileLocation.Y * 77f, Game1.timeOfDay * 99);
		int doves = (int)Game1.stats.Get("childrenTurnedToDoves");
		if (r.NextDouble() < 0.06)
		{
			this.Location.instantiateCrittersList();
			int whichBird = ((Game1.season == Season.Fall) ? 45 : 25);
			int yOffset = 0;
			if (Game1.random.NextBool() && Game1.MasterPlayer.mailReceived.Contains("Farm_Eternal"))
			{
				whichBird = ((Game1.season == Season.Fall) ? 135 : 125);
			}
			if (whichBird == 25 && Game1.random.NextDouble() < 0.05)
			{
				whichBird = 165;
			}
			if (r.NextDouble() < (double)doves * 0.08)
			{
				whichBird = 175;
				yOffset = 12;
			}
			this.Location.critters.Add(new Birdie(this.TileLocation * 64f + new Vector2(32f, 64 + Game1.random.Next(3) * 4 + yOffset), -160f, whichBird, stationary: true));
		}
	}

	/// <summary>Handle the player entering the location containing the object, or the furniture being placed.</summary>
	/// <param name="environment">The location containing the object.</param>
	/// <param name="dropDown">Whether the item was just placed (instead of the player entering the location with it already placed).</param>
	public virtual void actionOnPlayerEntryOrPlacement(GameLocation environment, bool dropDown)
	{
		if (this.Location == null)
		{
			this.Location = environment;
		}
		this.RemoveLightGlow();
		this.removeLights();
		if ((int)this.furniture_type == 14 || (int)this.furniture_type == 16)
		{
			this.setFireplace(playSound: false);
		}
		if (this.timeToTurnOnLights())
		{
			this.addLights();
			if (base.heldObject.Value is Furniture furniture)
			{
				furniture.addLights();
			}
		}
		if (base.QualifiedItemId == "(F)1971" && !dropDown)
		{
			environment.instantiateCrittersList();
			environment.addCritter(new Butterfly(environment, environment.getRandomTile()).setStayInbounds(stayInbounds: true));
			while (Game1.random.NextBool())
			{
				environment.addCritter(new Butterfly(environment, environment.getRandomTile()).setStayInbounds(stayInbounds: true));
			}
		}
	}

	/// <inheritdoc />
	public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false)
	{
		GameLocation location = this.Location;
		if (location == null)
		{
			return false;
		}
		if (!(dropInItem is Object dropIn))
		{
			return false;
		}
		if (this.IsTable() && base.heldObject.Value == null && !dropIn.bigCraftable && !(dropIn is Wallpaper) && (!(dropIn is Furniture furniture) || (furniture.getTilesWide() == 1 && furniture.getTilesHigh() == 1)))
		{
			if (!probe)
			{
				base.heldObject.Value = (Object)dropIn.getOne();
				base.heldObject.Value.Location = this.Location;
				base.heldObject.Value.TileLocation = base.tileLocation.Value;
				base.heldObject.Value.boundingBox.X = base.boundingBox.X;
				base.heldObject.Value.boundingBox.Y = base.boundingBox.Y;
				base.heldObject.Value.performDropDownAction(who);
				location.playSound("woodyStep");
				if (who != null)
				{
					who.reduceActiveItemByOne();
					if (returnFalseIfItemConsumed)
					{
						return false;
					}
				}
			}
			return true;
		}
		return false;
	}

	protected virtual int lightSourceIdentifier()
	{
		return (int)(base.tileLocation.X * 2000f + base.tileLocation.Y);
	}

	private bool isLampStyleLightSource()
	{
		if ((int)this.furniture_type != 7 && (int)this.furniture_type != 17)
		{
			return base.QualifiedItemId == "(F)1369";
		}
		return true;
	}

	public virtual void addLights()
	{
		GameLocation environment = this.Location;
		if (environment == null)
		{
			return;
		}
		if (base.heldObject.Value is Furniture furniture)
		{
			base.heldObject.Value.Location = this.Location;
			furniture.addLights();
		}
		if (this.isLampStyleLightSource())
		{
			this.sourceRect.Value = this.defaultSourceRect.Value;
			this.sourceIndexOffset.Value = 1;
			if (base.lightSource == null)
			{
				environment.removeLightSource(this.lightSourceIdentifier());
				base.lightSource = new LightSource(4, new Vector2(base.boundingBox.X + 32, base.boundingBox.Y + (((int)this.furniture_type == 7) ? (-64) : 64)), ((int)this.furniture_type == 7) ? 2f : 1f, (base.QualifiedItemId == "(F)1369") ? (Color.RoyalBlue * 0.7f) : Color.Black, this.lightSourceIdentifier(), LightSource.LightContext.None, 0L);
				environment.sharedLights[base.lightSource.identifier] = base.lightSource.Clone();
			}
		}
		else if (base.QualifiedItemId == "(F)1440")
		{
			environment.removeLightSource(this.lightSourceIdentifier());
			base.lightSource = new LightSource(4, new Vector2(base.boundingBox.X + 96, (float)base.boundingBox.Y - 32f), 1.5f, Color.Black, this.lightSourceIdentifier(), LightSource.LightContext.None, 0L);
			environment.sharedLights[base.lightSource.identifier] = base.lightSource.Clone();
		}
		else if ((int)this.furniture_type == 13)
		{
			this.sourceRect.Value = this.defaultSourceRect.Value;
			this.sourceIndexOffset.Value = 1;
			this.RemoveLightGlow();
		}
		else if (this is FishTankFurniture && base.lightSource == null)
		{
			int identifier = this.lightSourceIdentifier();
			Vector2 light_position = new Vector2(base.tileLocation.X * 64f + 32f + 2f, base.tileLocation.Y * 64f + 12f);
			for (int i = 0; i < this.getTilesWide(); i++)
			{
				environment.removeLightSource(identifier);
				base.lightSource = new LightSource(8, light_position, 2f, Color.Black, identifier, LightSource.LightContext.None, 0L);
				environment.sharedLights[identifier] = base.lightSource.Clone();
				light_position.X += 64f;
				identifier += 2000;
			}
		}
	}

	public virtual void removeLights()
	{
		GameLocation environment = this.Location;
		if (base.heldObject.Value is Furniture furniture)
		{
			furniture.removeLights();
		}
		if (this.isLampStyleLightSource() || base.QualifiedItemId == "(F)1440")
		{
			this.sourceRect.Value = this.defaultSourceRect.Value;
			this.sourceIndexOffset.Value = 0;
			environment?.removeLightSource(this.lightSourceIdentifier());
			base.lightSource = null;
		}
		else if ((int)this.furniture_type == 13)
		{
			if (environment != null && environment.IsRainingHere())
			{
				this.sourceRect.Value = this.defaultSourceRect.Value;
				this.sourceIndexOffset.Value = 1;
			}
			else
			{
				this.sourceRect.Value = this.defaultSourceRect.Value;
				this.sourceIndexOffset.Value = 0;
				this.AddLightGlow();
			}
		}
		else if (this is FishTankFurniture)
		{
			int identifier = this.lightSourceIdentifier();
			for (int i = 0; i < this.getTilesWide(); i++)
			{
				environment?.removeLightSource(identifier);
				identifier += 2000;
			}
			base.lightSource = null;
		}
	}

	/// <inheritdoc />
	public override bool minutesElapsed(int minutes)
	{
		if (this.Location == null)
		{
			return false;
		}
		if (this.timeToTurnOnLights())
		{
			this.addLights();
		}
		else
		{
			this.removeLights();
		}
		return false;
	}

	public override void performRemoveAction()
	{
		this.removeLights();
		if (this.Location != null)
		{
			if ((int)this.furniture_type == 14 || (int)this.furniture_type == 16)
			{
				base.isOn.Value = false;
				this.setFireplace(playSound: false);
			}
			this.RemoveLightGlow();
			base.performRemoveAction();
			if ((int)this.furniture_type == 14 || (int)this.furniture_type == 16)
			{
				base.lightSource = null;
			}
			if (base.QualifiedItemId == "(F)1309" && Game1.getMusicTrackName().Equals("sam_acoustic1"))
			{
				Game1.changeMusicTrack("none", track_interruptable: true);
			}
			this.sittingFarmers.Clear();
		}
	}

	public virtual void rotate()
	{
		if ((int)this.rotations >= 2)
		{
			int rotationAmount = (((int)this.rotations == 4) ? 1 : 2);
			this.currentRotation.Value += rotationAmount;
			this.currentRotation.Value %= 4;
			this.updateRotation();
		}
	}

	public virtual void updateRotation()
	{
		base.flipped.Value = false;
		if (this.currentRotation.Value > 0)
		{
			Point specialRotationOffsets = this.furniture_type.Value switch
			{
				2 => new Point(-1, 1), 
				5 => new Point(-1, 0), 
				3 => new Point(-1, 1), 
				_ => Point.Zero, 
			};
			bool differentSizesFor2Rotations = this.IsTable() || this.furniture_type.Value == 12 || base.QualifiedItemId == "(F)724" || base.QualifiedItemId == "(F)727";
			bool sourceRectRotate = this.defaultBoundingBox.Width != this.defaultBoundingBox.Height;
			if (differentSizesFor2Rotations && this.currentRotation.Value == 2)
			{
				this.currentRotation.Value = 1;
			}
			if (sourceRectRotate)
			{
				int oldBoundingBoxHeight = base.boundingBox.Height;
				switch (this.currentRotation.Value)
				{
				case 0:
				case 2:
					base.boundingBox.Height = this.defaultBoundingBox.Height;
					base.boundingBox.Width = this.defaultBoundingBox.Width;
					break;
				case 1:
				case 3:
					base.boundingBox.Height = base.boundingBox.Width + specialRotationOffsets.X * 64;
					base.boundingBox.Width = oldBoundingBoxHeight + specialRotationOffsets.Y * 64;
					break;
				}
			}
			Point specialSpecialSourceRectOffset = ((this.furniture_type.Value == 12) ? new Point(1, -1) : Point.Zero);
			if (sourceRectRotate)
			{
				switch (this.currentRotation.Value)
				{
				case 0:
					this.sourceRect.Value = this.defaultSourceRect.Value;
					break;
				case 1:
					this.sourceRect.Value = new Rectangle(this.defaultSourceRect.X + this.defaultSourceRect.Width, this.defaultSourceRect.Y, this.defaultSourceRect.Height - 16 + specialRotationOffsets.Y * 16 + specialSpecialSourceRectOffset.X * 16, this.defaultSourceRect.Width + 16 + specialRotationOffsets.X * 16 + specialSpecialSourceRectOffset.Y * 16);
					break;
				case 2:
					this.sourceRect.Value = new Rectangle(this.defaultSourceRect.X + this.defaultSourceRect.Width + this.defaultSourceRect.Height - 16 + specialRotationOffsets.Y * 16 + specialSpecialSourceRectOffset.X * 16, this.defaultSourceRect.Y, this.defaultSourceRect.Width, this.defaultSourceRect.Height);
					break;
				case 3:
					this.sourceRect.Value = new Rectangle(this.defaultSourceRect.X + this.defaultSourceRect.Width, this.defaultSourceRect.Y, this.defaultSourceRect.Height - 16 + specialRotationOffsets.Y * 16 + specialSpecialSourceRectOffset.X * 16, this.defaultSourceRect.Width + 16 + specialRotationOffsets.X * 16 + specialSpecialSourceRectOffset.Y * 16);
					base.flipped.Value = true;
					break;
				}
			}
			else
			{
				base.flipped.Value = this.currentRotation.Value == 3;
				if ((int)this.rotations == 2)
				{
					this.sourceRect.Value = new Rectangle(this.defaultSourceRect.X + ((this.currentRotation.Value == 2) ? 1 : 0) * this.defaultSourceRect.Width, this.defaultSourceRect.Y, this.defaultSourceRect.Width, this.defaultSourceRect.Height);
				}
				else
				{
					this.sourceRect.Value = new Rectangle(this.defaultSourceRect.X + ((this.currentRotation.Value == 3) ? 1 : this.currentRotation.Value) * this.defaultSourceRect.Width, this.defaultSourceRect.Y, this.defaultSourceRect.Width, this.defaultSourceRect.Height);
				}
			}
			if (differentSizesFor2Rotations && this.currentRotation.Value == 1)
			{
				this.currentRotation.Value = 2;
			}
		}
		else
		{
			this.sourceRect.Value = this.defaultSourceRect.Value;
			base.boundingBox.Value = this.defaultBoundingBox.Value;
		}
		this.updateDrawPosition();
	}

	public virtual bool isGroundFurniture()
	{
		if ((int)this.furniture_type != 13 && (int)this.furniture_type != 6 && (int)this.furniture_type != 17)
		{
			return (int)this.furniture_type != 13;
		}
		return false;
	}

	public override bool canBeGivenAsGift()
	{
		return false;
	}

	public static Furniture GetFurnitureInstance(string itemId, Vector2? position = null)
	{
		if (!position.HasValue)
		{
			position = Vector2.Zero;
		}
		switch (itemId)
		{
		case "1466":
		case "1468":
		case "1680":
		case "2326":
		case "RetroTV":
			return new TV(itemId, position.Value);
		default:
		{
			string[] data = Furniture.getData(itemId);
			if (data != null)
			{
				string furniture_type = data[1];
				if (furniture_type == "fishtank")
				{
					return new FishTankFurniture(itemId, position.Value);
				}
				if (furniture_type.StartsWith("bed"))
				{
					return new BedFurniture(itemId, position.Value);
				}
				if (furniture_type == "dresser")
				{
					return new StorageFurniture(itemId, position.Value);
				}
				if (furniture_type == "randomized_plant")
				{
					return new RandomizedPlantFurniture(itemId, position.Value);
				}
			}
			return new Furniture(itemId, position.Value);
		}
		}
	}

	public virtual bool IsCloseEnoughToFarmer(Farmer f, int? override_tile_x = null, int? override_tile_y = null)
	{
		Rectangle furniture_rect = new Rectangle((int)base.tileLocation.X * 64, (int)base.tileLocation.Y * 64, this.getTilesWide() * 64, this.getTilesHigh() * 64);
		if (override_tile_x.HasValue)
		{
			furniture_rect.X = override_tile_x.Value * 64;
		}
		if (override_tile_y.HasValue)
		{
			furniture_rect.Y = override_tile_y.Value * 64;
		}
		furniture_rect.Inflate(96, 96);
		return furniture_rect.Contains(Game1.player.StandingPixel);
	}

	public virtual int GetModifiedWallTilePosition(GameLocation l, int tile_x, int tile_y)
	{
		if (this.isGroundFurniture())
		{
			return tile_y;
		}
		if (l != null)
		{
			if (l is DecoratableLocation decoratableLocation)
			{
				int top_y = decoratableLocation.GetWallTopY(tile_x, tile_y);
				if (top_y != -1)
				{
					return top_y;
				}
			}
			return tile_y;
		}
		return tile_y;
	}

	public override bool canBePlacedHere(GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false)
	{
		if (!l.CanPlaceThisFurnitureHere(this))
		{
			return false;
		}
		if (!this.isGroundFurniture())
		{
			tile.Y = this.GetModifiedWallTilePosition(l, (int)tile.X, (int)tile.Y);
		}
		CollisionMask ignorePassables = CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.TerrainFeatures;
		bool passable = this.isPassable();
		if (passable)
		{
			ignorePassables |= CollisionMask.Characters | CollisionMask.Farmers;
		}
		collisionMask &= ~(CollisionMask.Furniture | CollisionMask.Objects);
		int tilesWide = this.getTilesWide();
		int tilesHigh = this.getTilesHigh();
		for (int x = 0; x < tilesWide; x++)
		{
			for (int y = 0; y < tilesHigh; y++)
			{
				Vector2 curTile = new Vector2(tile.X + (float)x, tile.Y + (float)y);
				Vector2 curPixel = new Vector2(curTile.X + 0.5f, curTile.Y + 0.5f) * 64f;
				if (!l.isTilePlaceable(curTile, passable))
				{
					return false;
				}
				foreach (Furniture f in l.furniture)
				{
					if ((int)f.furniture_type == 11 && f.GetBoundingBox().Contains((int)curPixel.X, (int)curPixel.Y) && f.heldObject.Value == null && tilesWide == 1 && tilesHigh == 1)
					{
						return true;
					}
					if (((int)f.furniture_type != 12 || (int)this.furniture_type == 12) && f.GetBoundingBox().Contains((int)curPixel.X, (int)curPixel.Y) && !f.AllowPlacementOnThisTile((int)tile.X + x, (int)tile.Y + y))
					{
						return false;
					}
				}
				if (l.objects.TryGetValue(curTile, out var tileObj) && (!tileObj.isPassable() || !this.isPassable()))
				{
					return false;
				}
				if (!this.isGroundFurniture())
				{
					if (l.IsTileOccupiedBy(curTile, collisionMask, ignorePassables))
					{
						return false;
					}
					continue;
				}
				if ((int)this.furniture_type == 15 && y == 0)
				{
					if (l.IsTileOccupiedBy(curTile, collisionMask, ignorePassables))
					{
						return false;
					}
					continue;
				}
				if (l.IsTileBlockedBy(curTile, collisionMask, ignorePassables))
				{
					return false;
				}
				if (l.terrainFeatures.ContainsKey(curTile) && l.terrainFeatures[curTile] is HoeDirt { crop: not null })
				{
					return false;
				}
			}
		}
		if (this.GetAdditionalFurniturePlacementStatus(l, (int)tile.X * 64, (int)tile.Y * 64) != 0)
		{
			return false;
		}
		return true;
	}

	public virtual void updateDrawPosition()
	{
		this.drawPosition.Value = new Vector2(base.boundingBox.X, base.boundingBox.Y - (this.sourceRect.Height * 4 - base.boundingBox.Height));
	}

	public virtual int getTilesWide()
	{
		return base.boundingBox.Width / 64;
	}

	public virtual int getTilesHigh()
	{
		return base.boundingBox.Height / 64;
	}

	/// <inheritdoc />
	public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
	{
		if (!this.isGroundFurniture())
		{
			y = this.GetModifiedWallTilePosition(location, x / 64, y / 64) * 64;
		}
		if (this.GetAdditionalFurniturePlacementStatus(location, x, y, who) != 0)
		{
			return false;
		}
		Vector2 tile = new Vector2(x / 64, y / 64);
		if (this.TileLocation != tile)
		{
			this.TileLocation = tile;
		}
		else
		{
			this.RecalculateBoundingBox();
		}
		foreach (Furniture f in location.furniture)
		{
			if ((int)f.furniture_type == 11 && f.heldObject.Value == null && f.GetBoundingBox().Intersects(base.boundingBox.Value))
			{
				f.performObjectDropInAction(this, probe: false, who ?? Game1.player);
				return true;
			}
		}
		return base.placementAction(location, x, y, who);
	}

	/// <summary>Get the reason the furniture can't be placed at a given position, if applicable.</summary>
	/// <param name="location">The location in which the furniture is being placed.</param>
	/// <param name="x">The X pixel position at which the furniture is being placed.</param>
	/// <param name="y">The Y pixel position at which the furniture is being placed.</param>
	/// <param name="who">The player placing the furniture, if applicable.</param>
	/// <returns>
	///   Returns one of these values:
	///   <list type="bullet">
	///     <item><description>0: valid placement.</description></item>
	///     <item><description>1: the object is a wall placed object but isn't being placed on a wall.</description></item>
	///     <item><description>2: the object can't be placed here due to the tile being marked as not furnishable.</description></item>
	///     <item><description>3: the object isn't a wall placed object, but is trying to be placed on a wall.</description></item>
	///     <item><description>4: the current location isn't decorable.</description></item>
	///     <item><description>-1: general fail condition.</description></item>
	///   </list>
	/// </returns>
	public virtual int GetAdditionalFurniturePlacementStatus(GameLocation location, int x, int y, Farmer who = null)
	{
		if (location.CanPlaceThisFurnitureHere(this))
		{
			Point anchor = new Point(x / 64, y / 64);
			base.tileLocation.Value = new Vector2(anchor.X, anchor.Y);
			bool paintingAtRightPlace = false;
			if ((int)this.furniture_type == 6 || (int)this.furniture_type == 17 || (int)this.furniture_type == 13 || base.QualifiedItemId == "(F)1293")
			{
				int offset = ((base.QualifiedItemId == "(F)1293") ? 3 : 0);
				bool foundWall = false;
				if (location is DecoratableLocation decoratable_location)
				{
					if (((int)this.furniture_type == 6 || (int)this.furniture_type == 17 || (int)this.furniture_type == 13 || offset != 0) && decoratable_location.isTileOnWall(anchor.X, anchor.Y - offset) && decoratable_location.GetWallTopY(anchor.X, anchor.Y - offset) + offset == anchor.Y)
					{
						foundWall = true;
					}
					else if (!this.isGroundFurniture() && decoratable_location.isTileOnWall(anchor.X, anchor.Y - 1) && decoratable_location.GetWallTopY(anchor.X, anchor.Y) + 1 == anchor.Y)
					{
						foundWall = true;
					}
				}
				if (!foundWall)
				{
					return 1;
				}
				paintingAtRightPlace = true;
			}
			int tiles_high_to_check = this.getTilesHigh();
			if ((int)this.furniture_type == 6 && tiles_high_to_check > 2)
			{
				tiles_high_to_check = 2;
			}
			for (int furnitureX = anchor.X; furnitureX < anchor.X + this.getTilesWide(); furnitureX++)
			{
				for (int furnitureY = anchor.Y; furnitureY < anchor.Y + tiles_high_to_check; furnitureY++)
				{
					if (location.doesTileHaveProperty(furnitureX, furnitureY, "NoFurniture", "Back") != null)
					{
						return 2;
					}
					if (!paintingAtRightPlace && location is DecoratableLocation decoratableLocation && decoratableLocation.isTileOnWall(furnitureX, furnitureY))
					{
						if (!(this is BedFurniture) || furnitureY != anchor.Y)
						{
							return 3;
						}
						continue;
					}
					int buildings_index = location.getTileIndexAt(furnitureX, furnitureY, "Buildings");
					if (buildings_index != -1 && (!(location is IslandFarmHouse) || buildings_index < 192 || buildings_index > 194 || !(location.getTileSheetIDAt(furnitureX, furnitureY, "Buildings") == "untitled tile sheet")))
					{
						return -1;
					}
				}
			}
			return 0;
		}
		return 4;
	}

	public override bool isPassable()
	{
		if (this.furniture_type.Value == 12)
		{
			return true;
		}
		return base.isPassable();
	}

	public override bool isPlaceable()
	{
		return true;
	}

	public virtual bool AllowPlacementOnThisTile(int tile_x, int tile_y)
	{
		return false;
	}

	/// <inheritdoc />
	public override Rectangle GetBoundingBoxAt(int x, int y)
	{
		if (base.isTemporarilyInvisible)
		{
			return Rectangle.Empty;
		}
		return base.boundingBox.Value;
	}

	protected static Rectangle getDefaultSourceRectForType(ParsedItemData itemData, int type, Texture2D texture = null)
	{
		int width;
		int height;
		switch (type)
		{
		case 0:
			width = 1;
			height = 2;
			break;
		case 1:
			width = 2;
			height = 2;
			break;
		case 2:
			width = 3;
			height = 2;
			break;
		case 3:
			width = 2;
			height = 2;
			break;
		case 4:
			width = 2;
			height = 2;
			break;
		case 5:
			width = 5;
			height = 3;
			break;
		case 6:
			width = 2;
			height = 2;
			break;
		case 17:
			width = 1;
			height = 2;
			break;
		case 7:
			width = 1;
			height = 3;
			break;
		case 8:
			width = 1;
			height = 2;
			break;
		case 10:
			width = 2;
			height = 3;
			break;
		case 11:
			width = 2;
			height = 3;
			break;
		case 12:
			width = 3;
			height = 2;
			break;
		case 13:
			width = 1;
			height = 2;
			break;
		case 14:
			width = 2;
			height = 5;
			break;
		case 16:
			width = 1;
			height = 2;
			break;
		default:
			width = 1;
			height = 2;
			break;
		}
		return Furniture.getDefaultSourceRect(itemData, width, height, texture);
	}

	protected static Rectangle getDefaultSourceRect(ParsedItemData itemData, int spriteWidth, int spriteHeight, Texture2D texture = null)
	{
		texture = texture ?? itemData.GetTexture();
		return new Rectangle(itemData.SpriteIndex * 16 % texture.Width, itemData.SpriteIndex * 16 / texture.Width * 16, spriteWidth * 16, spriteHeight * 16);
	}

	protected virtual Rectangle getDefaultBoundingBoxForType(int type)
	{
		int width;
		int height;
		switch (type)
		{
		case 0:
			width = 1;
			height = 1;
			break;
		case 1:
			width = 2;
			height = 1;
			break;
		case 2:
			width = 3;
			height = 1;
			break;
		case 3:
			width = 2;
			height = 1;
			break;
		case 4:
			width = 2;
			height = 1;
			break;
		case 5:
			width = 5;
			height = 2;
			break;
		case 6:
			width = 2;
			height = 2;
			break;
		case 17:
			width = 1;
			height = 2;
			break;
		case 7:
			width = 1;
			height = 1;
			break;
		case 8:
			width = 1;
			height = 1;
			break;
		case 10:
			width = 2;
			height = 1;
			break;
		case 11:
			width = 2;
			height = 2;
			break;
		case 12:
			width = 3;
			height = 2;
			break;
		case 13:
			width = 1;
			height = 2;
			break;
		case 14:
			width = 2;
			height = 1;
			break;
		case 16:
			width = 1;
			height = 1;
			break;
		default:
			width = 1;
			height = 1;
			break;
		}
		return new Rectangle((int)base.tileLocation.X * 64, (int)base.tileLocation.Y * 64, width * 64, height * 64);
	}

	public static int getTypeNumberFromName(string typeName)
	{
		if (typeName.ToLower().StartsWith("bed"))
		{
			return 15;
		}
		return typeName.ToLower() switch
		{
			"chair" => 0, 
			"bench" => 1, 
			"couch" => 2, 
			"armchair" => 3, 
			"dresser" => 4, 
			"long table" => 5, 
			"painting" => 6, 
			"lamp" => 7, 
			"decor" => 8, 
			"bookcase" => 10, 
			"table" => 11, 
			"rug" => 12, 
			"window" => 13, 
			"fireplace" => 14, 
			"torch" => 16, 
			"sconce" => 17, 
			_ => 9, 
		};
	}

	/// <inheritdoc />
	public override int salePrice(bool ignoreProfitMargins = false)
	{
		return base.price;
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	protected virtual float getScaleSize()
	{
		int tilesWide = this.defaultSourceRect.Width / 16;
		int tilesHigh = this.defaultSourceRect.Height / 16;
		if (tilesWide >= 7)
		{
			return 0.5f;
		}
		if (tilesWide >= 6)
		{
			return 0.66f;
		}
		if (tilesWide >= 5)
		{
			return 0.75f;
		}
		if (tilesHigh >= 5)
		{
			return 0.8f;
		}
		if (tilesHigh >= 3)
		{
			return 1f;
		}
		if (tilesWide <= 2)
		{
			return 2f;
		}
		if (tilesWide <= 4)
		{
			return 1f;
		}
		return 0.1f;
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		if (this.Location == null)
		{
			return;
		}
		if (Game1.IsMasterGame && this.sittingFarmers.Length > 0)
		{
			List<long> ids_to_remove = null;
			foreach (long uid2 in this.sittingFarmers.Keys)
			{
				if (!Game1.player.team.playerIsOnline(uid2))
				{
					if (ids_to_remove == null)
					{
						ids_to_remove = new List<long>();
					}
					ids_to_remove.Add(uid2);
				}
			}
			if (ids_to_remove != null)
			{
				foreach (long uid in ids_to_remove)
				{
					this.sittingFarmers.Remove(uid);
				}
			}
		}
		if (base.shakeTimer > 0)
		{
			base.shakeTimer -= time.ElapsedGameTime.Milliseconds;
		}
		if (base.IsOn && base.SpecialVariable == 388859)
		{
			base.lastNoteBlockSoundTime += (int)time.ElapsedGameTime.TotalMilliseconds;
			if (base.lastNoteBlockSoundTime > 500)
			{
				base.lastNoteBlockSoundTime = 0;
				this.addCauldronBubbles();
			}
		}
	}

	private void addCauldronBubbles(float speed = -0.5f)
	{
		this.Location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(372, 1956, 10, 10), this.TileLocation * 64f + new Vector2(41.6f, -21f) + new Vector2(Game1.random.Next(-12, 21), Game1.random.Next(16)), flipped: false, 0.002f, Color.Lime)
		{
			alphaFade = 0.001f - speed / 300f,
			alpha = 0.75f,
			motion = new Vector2(0f, speed),
			acceleration = new Vector2(0f, 0f),
			interval = 99999f,
			layerDepth = (float)(base.boundingBox.Bottom - 3 - Game1.random.Next(5)) / 10000f,
			scale = 3f,
			scaleChange = 0.01f,
			rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f
		});
	}

	public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
	{
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		base.AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		Rectangle sourceRect = itemData.GetSourceRect();
		spriteBatch.Draw(itemData.GetTexture(), location + new Vector2(32f, 32f), itemData.GetSourceRect(), color * transparency, 0f, new Vector2(sourceRect.Width / 2, sourceRect.Height / 2), 1f * this.getScaleSize() * scaleSize, SpriteEffects.None, layerDepth);
		this.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		if (base.isTemporarilyInvisible)
		{
			return;
		}
		Rectangle drawn_source_rect = this.sourceRect.Value;
		drawn_source_rect.X += drawn_source_rect.Width * this.sourceIndexOffset.Value;
		ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		Texture2D texture = itemData.GetTexture();
		string textureName = itemData.TextureName;
		if (itemData.IsErrorItem)
		{
			drawn_source_rect = itemData.GetSourceRect();
		}
		if (Furniture._frontTextureName == null)
		{
			Furniture._frontTextureName = new Dictionary<string, string>();
		}
		if (Furniture.isDrawingLocationFurniture)
		{
			if (!Furniture._frontTextureName.TryGetValue(textureName, out var frontTexturePath))
			{
				frontTexturePath = textureName + "Front";
				Furniture._frontTextureName[textureName] = frontTexturePath;
			}
			Texture2D frontTexture = null;
			if (this.HasSittingFarmers() || base.SpecialVariable == 388859)
			{
				try
				{
					frontTexture = Game1.content.Load<Texture2D>(frontTexturePath);
				}
				catch
				{
					frontTexture = null;
				}
			}
			Vector2 actualDrawPosition = Game1.GlobalToLocal(Game1.viewport, this.drawPosition.Value + ((base.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero));
			SpriteEffects spriteEffects = (base.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
			Color color = Color.White * alpha;
			if (this.HasSittingFarmers())
			{
				spriteBatch.Draw(texture, actualDrawPosition, drawn_source_rect, color, 0f, Vector2.Zero, 4f, spriteEffects, (float)(base.boundingBox.Value.Top + 16) / 10000f);
				if (frontTexture != null && drawn_source_rect.Right <= frontTexture.Width && drawn_source_rect.Bottom <= frontTexture.Height)
				{
					spriteBatch.Draw(frontTexture, actualDrawPosition, drawn_source_rect, color, 0f, Vector2.Zero, 4f, spriteEffects, (float)(base.boundingBox.Value.Bottom - 8) / 10000f);
				}
			}
			else
			{
				spriteBatch.Draw(texture, actualDrawPosition, drawn_source_rect, color, 0f, Vector2.Zero, 4f, spriteEffects, ((int)this.furniture_type == 12) ? (2E-09f + base.tileLocation.Y / 100000f) : ((float)(base.boundingBox.Value.Bottom - (((int)this.furniture_type == 6 || (int)this.furniture_type == 17 || (int)this.furniture_type == 13) ? 48 : 8)) / 10000f));
				if (base.SpecialVariable == 388859 && frontTexture != null && drawn_source_rect.Right <= frontTexture.Width && drawn_source_rect.Bottom <= frontTexture.Height)
				{
					spriteBatch.Draw(frontTexture, actualDrawPosition, drawn_source_rect, color, 0f, Vector2.Zero, 4f, spriteEffects, (float)(base.boundingBox.Value.Bottom - 2) / 10000f);
				}
			}
		}
		else
		{
			spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + ((base.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 - (drawn_source_rect.Height * 4 - base.boundingBox.Height) + ((base.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), drawn_source_rect, Color.White * alpha, 0f, Vector2.Zero, 4f, base.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, ((int)this.furniture_type == 12) ? (2E-09f + base.tileLocation.Y / 100000f) : ((float)(base.boundingBox.Value.Bottom - (((int)this.furniture_type == 6 || (int)this.furniture_type == 17 || (int)this.furniture_type == 13) ? 48 : 8)) / 10000f));
		}
		if (base.heldObject.Value != null)
		{
			if (base.heldObject.Value is Furniture furniture)
			{
				furniture.drawAtNonTileSpot(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(base.boundingBox.Center.X - 32, base.boundingBox.Center.Y - furniture.sourceRect.Height * 4 - (this.drawHeldObjectLow ? (-16) : 16))), (float)(base.boundingBox.Bottom - 7) / 10000f, alpha);
			}
			else
			{
				ParsedItemData heldItemData = ItemRegistry.GetDataOrErrorItem(base.heldObject.Value.QualifiedItemId);
				spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(base.boundingBox.Center.X - 32, base.boundingBox.Center.Y - (this.drawHeldObjectLow ? 32 : 85))) + new Vector2(32f, 53f), Game1.shadowTexture.Bounds, Color.White * alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)base.boundingBox.Bottom / 10000f);
				if (base.heldObject.Value is ColoredObject)
				{
					base.heldObject.Value.drawInMenu(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(base.boundingBox.Center.X - 32, base.boundingBox.Center.Y - (this.drawHeldObjectLow ? 32 : 85))), 1f, 1f, (float)(base.boundingBox.Bottom + 1) / 10000f, StackDrawType.Hide, Color.White, drawShadow: false);
				}
				else
				{
					spriteBatch.Draw(heldItemData.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(base.boundingBox.Center.X - 32, base.boundingBox.Center.Y - (this.drawHeldObjectLow ? 32 : 85))), heldItemData.GetSourceRect(), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(base.boundingBox.Bottom + 1) / 10000f);
				}
			}
		}
		if ((bool)base.isOn && (int)this.furniture_type == 14)
		{
			Rectangle bounds = this.GetBoundingBoxAt(x, y);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(base.boundingBox.Center.X - 12, base.boundingBox.Center.Y - 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(bounds.Bottom - 2) / 10000f);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(base.boundingBox.Center.X - 32 - 4, base.boundingBox.Center.Y - 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 2047) + (double)(y * 98)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(bounds.Bottom - 1) / 10000f);
		}
		else if ((bool)base.isOn && (int)this.furniture_type == 16)
		{
			Rectangle bounds2 = this.GetBoundingBoxAt(x, y);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(base.boundingBox.Center.X - 20, (float)base.boundingBox.Center.Y - 105.6f)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(bounds2.Bottom - 2) / 10000f);
		}
		if (Game1.debugMode)
		{
			spriteBatch.DrawString(Game1.smallFont, base.QualifiedItemId, Game1.GlobalToLocal(Game1.viewport, this.drawPosition.Value), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
		}
	}

	public virtual void drawAtNonTileSpot(SpriteBatch spriteBatch, Vector2 location, float layerDepth, float alpha = 1f)
	{
		ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		Rectangle drawn_source_rect = this.sourceRect.Value;
		drawn_source_rect.X += drawn_source_rect.Width * (int)this.sourceIndexOffset;
		if (itemData.IsErrorItem)
		{
			drawn_source_rect = itemData.GetSourceRect();
		}
		spriteBatch.Draw(itemData.GetTexture(), location, drawn_source_rect, Color.White * alpha, 0f, Vector2.Zero, 4f, base.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
	}

	public virtual int GetAdditionalTilePropertyRadius()
	{
		return 0;
	}

	public virtual bool DoesTileHaveProperty(int tile_x, int tile_y, string property_name, string layer_name, ref string property_value)
	{
		return false;
	}

	public virtual bool IntersectsForCollision(Rectangle rect)
	{
		return base.GetBoundingBox().Intersects(rect);
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Furniture(base.ItemId, base.tileLocation.Value);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Furniture fromFurniture)
		{
			this.drawPosition.Value = fromFurniture.drawPosition.Value;
			this.defaultBoundingBox.Value = fromFurniture.defaultBoundingBox.Value;
			base.boundingBox.Value = fromFurniture.boundingBox.Value;
			base.isOn.Value = false;
			this.rotations.Value = fromFurniture.rotations;
			this.currentRotation.Value = (int)fromFurniture.currentRotation - ((this.rotations.Value == 4) ? 1 : 2);
			this.rotate();
		}
	}
}
