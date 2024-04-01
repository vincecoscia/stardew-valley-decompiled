using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Locations;

namespace StardewValley.Menus;

public class InventoryMenu : IClickableMenu
{
	public delegate bool highlightThisItem(Item i);

	public enum BorderSide
	{
		Top,
		Left,
		Right,
		Bottom
	}

	public const int region_inventorySlot0 = 0;

	public const int region_inventorySlot1 = 1;

	public const int region_inventorySlot2 = 2;

	public const int region_inventorySlot3 = 3;

	public const int region_inventorySlot4 = 4;

	public const int region_inventorySlot5 = 5;

	public const int region_inventorySlot6 = 6;

	public const int region_inventorySlot7 = 7;

	public const int region_dropButton = 107;

	public const int region_inventoryArea = 9000;

	public string hoverText = "";

	public string hoverTitle = "";

	public string descriptionTitle = "";

	public string descriptionText = "";

	public List<ClickableComponent> inventory = new List<ClickableComponent>();

	protected Dictionary<int, double> _iconShakeTimer = new Dictionary<int, double>();

	public IList<Item> actualInventory;

	public highlightThisItem highlightMethod;

	public ItemGrabMenu.behaviorOnItemSelect onAddItem;

	public bool playerInventory;

	public bool drawSlots;

	public bool showGrayedOutSlots;

	public int capacity;

	public int rows;

	public int horizontalGap;

	public int verticalGap;

	public ClickableComponent dropItemInvisibleButton;

	public string moveItemSound = "dwop";

	public InventoryMenu(int xPosition, int yPosition, bool playerInventory, IList<Item> actualInventory = null, highlightThisItem highlightMethod = null, int capacity = -1, int rows = 3, int horizontalGap = 0, int verticalGap = 0, bool drawSlots = true)
		: base(xPosition, yPosition, 64 * (((capacity == -1) ? 36 : capacity) / rows), 64 * rows + 16)
	{
		this.drawSlots = drawSlots;
		this.horizontalGap = horizontalGap;
		this.verticalGap = verticalGap;
		this.rows = rows;
		this.capacity = ((capacity == -1) ? 36 : capacity);
		this.playerInventory = playerInventory;
		this.actualInventory = actualInventory;
		if (actualInventory == null)
		{
			this.actualInventory = Game1.player.Items;
		}
		for (int i = 0; i < (int)Game1.player.maxItems; i++)
		{
			if (Game1.player.Items.Count <= i)
			{
				Game1.player.Items.Add(null);
			}
		}
		for (int j = 0; j < this.capacity; j++)
		{
			int downNeighbor = 0;
			downNeighbor = ((!playerInventory) ? ((j >= this.capacity - this.capacity / rows) ? (-99998) : (j + this.capacity / rows)) : ((j < this.actualInventory.Count - this.capacity / rows) ? (j + this.capacity / rows) : ((j < this.actualInventory.Count - 3 && this.actualInventory.Count >= 36) ? (-99998) : ((j % 12 < 2) ? 102 : 101))));
			this.inventory.Add(new ClickableComponent(new Rectangle(xPosition + j % (this.capacity / rows) * 64 + horizontalGap * (j % (this.capacity / rows)), base.yPositionOnScreen + j / (this.capacity / rows) * (64 + verticalGap) + (j / (this.capacity / rows) - 1) * 4 - ((j <= this.capacity / rows && playerInventory && verticalGap == 0) ? 12 : 0), 64, 64), j.ToString() ?? "")
			{
				myID = j,
				leftNeighborID = ((j % (this.capacity / rows) != 0) ? (j - 1) : 107),
				rightNeighborID = (((j + 1) % (this.capacity / rows) != 0) ? (j + 1) : 106),
				downNeighborID = downNeighbor,
				upNeighborID = ((j < this.capacity / rows) ? (12340 + j) : (j - this.capacity / rows)),
				region = 9000,
				upNeighborImmutable = true,
				downNeighborImmutable = true,
				leftNeighborImmutable = true,
				rightNeighborImmutable = true
			});
		}
		this.highlightMethod = highlightMethod;
		if (highlightMethod == null)
		{
			this.highlightMethod = highlightAllItems;
		}
		this.dropItemInvisibleButton = new ClickableComponent(new Rectangle(xPosition - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 128, base.yPositionOnScreen - 12, 64, 64), "")
		{
			myID = (playerInventory ? 107 : (-500)),
			rightNeighborID = 0
		};
		foreach (ClickableComponent item in this.GetBorder(BorderSide.Top))
		{
			item.upNeighborImmutable = false;
		}
		foreach (ClickableComponent item2 in this.GetBorder(BorderSide.Bottom))
		{
			item2.downNeighborImmutable = false;
		}
		foreach (ClickableComponent item3 in this.GetBorder(BorderSide.Left))
		{
			item3.leftNeighborImmutable = false;
		}
		foreach (ClickableComponent item4 in this.GetBorder(BorderSide.Right))
		{
			item4.rightNeighborImmutable = false;
		}
	}

	public List<ClickableComponent> GetBorder(BorderSide side)
	{
		List<ClickableComponent> inventory_slots = new List<ClickableComponent>();
		int row_size = this.capacity / this.rows;
		switch (side)
		{
		case BorderSide.Bottom:
		{
			for (int i = 0; i < this.inventory.Count; i++)
			{
				if (i >= this.actualInventory.Count - row_size)
				{
					inventory_slots.Add(this.inventory[i]);
				}
			}
			break;
		}
		case BorderSide.Top:
		{
			for (int l = 0; l < this.inventory.Count; l++)
			{
				if (l < row_size)
				{
					inventory_slots.Add(this.inventory[l]);
				}
			}
			break;
		}
		case BorderSide.Left:
		{
			for (int k = 0; k < this.inventory.Count; k++)
			{
				if (k % row_size == 0)
				{
					inventory_slots.Add(this.inventory[k]);
				}
			}
			break;
		}
		case BorderSide.Right:
		{
			for (int j = 0; j < this.inventory.Count; j++)
			{
				if (j % row_size == row_size - 1)
				{
					inventory_slots.Add(this.inventory[j]);
				}
			}
			break;
		}
		}
		return inventory_slots;
	}

	public static bool highlightAllItems(Item i)
	{
		return true;
	}

	public static bool highlightNoItems(Item i)
	{
		return false;
	}

	public void SetPosition(int x, int y)
	{
		this.movePosition(-base.xPositionOnScreen, -base.yPositionOnScreen);
		this.movePosition(x, y);
	}

	public void movePosition(int x, int y)
	{
		base.xPositionOnScreen += x;
		base.yPositionOnScreen += y;
		foreach (ClickableComponent item in this.inventory)
		{
			item.bounds.X += x;
			item.bounds.Y += y;
		}
		this.dropItemInvisibleButton.bounds.X += x;
		this.dropItemInvisibleButton.bounds.Y += y;
	}

	public void ShakeItem(Item item)
	{
		this.ShakeItem(this.actualInventory.IndexOf(item));
	}

	public void ShakeItem(int index)
	{
		if (index >= 0 && index < this.inventory.Count)
		{
			this._iconShakeTimer[index] = Game1.currentGameTime.TotalGameTime.TotalSeconds + 0.5;
		}
	}

	public Item tryToAddItem(Item toPlace, string sound = "coin")
	{
		if (toPlace == null)
		{
			return null;
		}
		int originalStack = toPlace.Stack;
		foreach (ClickableComponent item in this.inventory)
		{
			int slotNumber = Convert.ToInt32(item.name);
			if (slotNumber >= this.actualInventory.Count || this.actualInventory[slotNumber] == null || !this.highlightMethod(this.actualInventory[slotNumber]) || !this.actualInventory[slotNumber].canStackWith(toPlace))
			{
				continue;
			}
			toPlace.Stack = this.actualInventory[slotNumber].addToStack(toPlace);
			if (toPlace.Stack <= 0)
			{
				try
				{
					Game1.playSound(sound);
					this.onAddItem?.Invoke(toPlace, this.playerInventory ? Game1.player : null);
				}
				catch (Exception)
				{
				}
				return null;
			}
		}
		foreach (ClickableComponent item2 in this.inventory)
		{
			int slotNumber2 = Convert.ToInt32(item2.name);
			if (slotNumber2 >= this.actualInventory.Count || (this.actualInventory[slotNumber2] != null && !this.highlightMethod(this.actualInventory[slotNumber2])) || this.actualInventory[slotNumber2] != null)
			{
				continue;
			}
			if (!string.IsNullOrEmpty(sound))
			{
				try
				{
					Game1.playSound(sound);
				}
				catch (Exception)
				{
				}
			}
			return Utility.addItemToInventory(toPlace, slotNumber2, this.actualInventory, this.onAddItem);
		}
		if (toPlace.Stack < originalStack)
		{
			Game1.playSound(sound);
		}
		return toPlace;
	}

	public int getInventoryPositionOfClick(int x, int y)
	{
		for (int i = 0; i < this.inventory.Count; i++)
		{
			if (this.inventory[i] != null && this.inventory[i].bounds.Contains(x, y))
			{
				return Convert.ToInt32(this.inventory[i].name);
			}
		}
		return -1;
	}

	public Item leftClick(int x, int y, Item toPlace, bool playSound = true)
	{
		foreach (ClickableComponent c in this.inventory)
		{
			if (!c.containsPoint(x, y))
			{
				continue;
			}
			int slotNumber = Convert.ToInt32(c.name);
			if (slotNumber >= this.actualInventory.Count || (this.actualInventory[slotNumber] != null && !this.highlightMethod(this.actualInventory[slotNumber]) && !this.actualInventory[slotNumber].canStackWith(toPlace)))
			{
				continue;
			}
			if (this.actualInventory[slotNumber] != null)
			{
				if (toPlace != null)
				{
					if (playSound)
					{
						Game1.playSound("stoneStep");
					}
					return Utility.addItemToInventory(toPlace, slotNumber, this.actualInventory, this.onAddItem);
				}
				if (playSound)
				{
					Game1.playSound(this.moveItemSound);
				}
				return Utility.removeItemFromInventory(slotNumber, this.actualInventory);
			}
			if (toPlace != null)
			{
				if (playSound)
				{
					Game1.playSound("stoneStep");
				}
				return Utility.addItemToInventory(toPlace, slotNumber, this.actualInventory, this.onAddItem);
			}
		}
		return toPlace;
	}

	public Vector2 snapToClickableComponent(int x, int y)
	{
		foreach (ClickableComponent c in this.inventory)
		{
			if (c.containsPoint(x, y))
			{
				return new Vector2(c.bounds.X, c.bounds.Y);
			}
		}
		return new Vector2(x, y);
	}

	public Item getItemAt(int x, int y)
	{
		foreach (ClickableComponent c in this.inventory)
		{
			if (c.containsPoint(x, y))
			{
				return this.getItemFromClickableComponent(c);
			}
		}
		return null;
	}

	public Item getItemFromClickableComponent(ClickableComponent c)
	{
		if (c != null)
		{
			int slotNumber = Convert.ToInt32(c.name);
			if (slotNumber < this.actualInventory.Count)
			{
				return this.actualInventory[slotNumber];
			}
		}
		return null;
	}

	public Item rightClick(int x, int y, Item toAddTo, bool playSound = true, bool onlyCheckToolAttachments = false)
	{
		foreach (ClickableComponent item in this.inventory)
		{
			int slotNumber = Convert.ToInt32(item.name);
			if (!item.containsPoint(x, y) || slotNumber >= this.actualInventory.Count || (this.actualInventory[slotNumber] != null && !this.highlightMethod(this.actualInventory[slotNumber])) || slotNumber >= this.actualInventory.Count || this.actualInventory[slotNumber] == null)
			{
				continue;
			}
			if (this.actualInventory[slotNumber] is Tool tool && (toAddTo == null || toAddTo is Object) && tool.canThisBeAttached((Object)toAddTo))
			{
				return tool.attach((Object)toAddTo);
			}
			if (onlyCheckToolAttachments)
			{
				return toAddTo;
			}
			if (toAddTo == null)
			{
				if (this.actualInventory[slotNumber].maximumStackSize() != -1)
				{
					if (slotNumber == Game1.player.CurrentToolIndex && this.actualInventory[slotNumber] != null && this.actualInventory[slotNumber].Stack == 1)
					{
						this.actualInventory[slotNumber].actionWhenStopBeingHeld(Game1.player);
					}
					Item tmp = this.actualInventory[slotNumber].getOne();
					if (this.actualInventory[slotNumber].Stack > 1 && Game1.isOneOfTheseKeysDown(Game1.oldKBState, new InputButton[1]
					{
						new InputButton(Keys.LeftShift)
					}))
					{
						tmp.Stack = (int)Math.Ceiling((double)this.actualInventory[slotNumber].Stack / 2.0);
						this.actualInventory[slotNumber].Stack = this.actualInventory[slotNumber].Stack / 2;
					}
					else if (this.actualInventory[slotNumber].Stack == 1)
					{
						this.actualInventory[slotNumber] = null;
					}
					else
					{
						this.actualInventory[slotNumber].Stack--;
					}
					if (this.actualInventory[slotNumber] != null && this.actualInventory[slotNumber].Stack <= 0)
					{
						this.actualInventory[slotNumber] = null;
					}
					if (playSound)
					{
						Game1.playSound(this.moveItemSound);
					}
					return tmp;
				}
			}
			else
			{
				if (!this.actualInventory[slotNumber].canStackWith(toAddTo) || toAddTo.Stack >= toAddTo.maximumStackSize())
				{
					continue;
				}
				if (Game1.isOneOfTheseKeysDown(Game1.oldKBState, new InputButton[1]
				{
					new InputButton(Keys.LeftShift)
				}))
				{
					int amountToAdd = (int)Math.Ceiling((double)this.actualInventory[slotNumber].Stack / 2.0);
					amountToAdd = Math.Min(toAddTo.maximumStackSize() - toAddTo.Stack, amountToAdd);
					toAddTo.Stack += amountToAdd;
					this.actualInventory[slotNumber].Stack -= amountToAdd;
				}
				else
				{
					toAddTo.Stack++;
					this.actualInventory[slotNumber].Stack--;
				}
				if (playSound)
				{
					Game1.playSound(this.moveItemSound);
				}
				if (this.actualInventory[slotNumber].Stack <= 0)
				{
					if (slotNumber == Game1.player.CurrentToolIndex)
					{
						this.actualInventory[slotNumber].actionWhenStopBeingHeld(Game1.player);
					}
					this.actualInventory[slotNumber] = null;
				}
				return toAddTo;
			}
		}
		return toAddTo;
	}

	public Item hover(int x, int y, Item heldItem)
	{
		this.descriptionText = "";
		this.descriptionTitle = "";
		this.hoverText = "";
		this.hoverTitle = "";
		Item toReturn = null;
		foreach (ClickableComponent c in this.inventory)
		{
			int slotNumber = Convert.ToInt32(c.name);
			c.scale = Math.Max(1f, c.scale - 0.025f);
			if (c.containsPoint(x, y) && slotNumber < this.actualInventory.Count && (this.actualInventory[slotNumber] == null || this.highlightMethod(this.actualInventory[slotNumber])) && slotNumber < this.actualInventory.Count && this.actualInventory[slotNumber] != null)
			{
				this.descriptionTitle = this.actualInventory[slotNumber].DisplayName;
				this.descriptionText = Environment.NewLine + this.actualInventory[slotNumber].getDescription();
				c.scale = Math.Min(c.scale + 0.05f, 1.1f);
				string s = this.actualInventory[slotNumber].getHoverBoxText(heldItem);
				if (s != null)
				{
					this.hoverText = s;
					this.hoverTitle = this.actualInventory[slotNumber].DisplayName;
				}
				else
				{
					this.hoverText = this.actualInventory[slotNumber].getDescription();
					this.hoverTitle = this.actualInventory[slotNumber].DisplayName;
				}
				if (toReturn == null)
				{
					toReturn = this.actualInventory[slotNumber];
				}
			}
		}
		if (toReturn is Object returnObj && Game1.RequireLocation<CommunityCenter>("CommunityCenter").couldThisIngredienteBeUsedInABundle(returnObj))
		{
			GameMenu.bundleItemHovered = true;
		}
		return toReturn;
	}

	public override void setUpForGamePadMode()
	{
		base.setUpForGamePadMode();
		List<ClickableComponent> list = this.inventory;
		if (list != null && list.Count > 0)
		{
			Game1.setMousePosition(this.inventory[0].bounds.Right - this.inventory[0].bounds.Width / 8, this.inventory[0].bounds.Bottom - this.inventory[0].bounds.Height / 8);
		}
	}

	public override void draw(SpriteBatch b)
	{
		this.draw(b, -1, -1, -1);
	}

	public override void draw(SpriteBatch b, int red = -1, int green = -1, int blue = -1)
	{
		for (int j = 0; j < this.inventory.Count; j++)
		{
			if (this._iconShakeTimer.TryGetValue(j, out var endTime) && Game1.currentGameTime.TotalGameTime.TotalSeconds >= endTime)
			{
				this._iconShakeTimer.Remove(j);
			}
		}
		Color tint = ((red == -1) ? Color.White : new Color((int)Utility.Lerp(red, Math.Min(255, red + 150), 0.65f), (int)Utility.Lerp(green, Math.Min(255, green + 150), 0.65f), (int)Utility.Lerp(blue, Math.Min(255, blue + 150), 0.65f)));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		if (this.drawSlots)
		{
			for (int k = 0; k < this.capacity; k++)
			{
				Vector2 toDraw2 = new Vector2(base.xPositionOnScreen + k % (this.capacity / this.rows) * 64 + this.horizontalGap * (k % (this.capacity / this.rows)), base.yPositionOnScreen + k / (this.capacity / this.rows) * (64 + this.verticalGap) + (k / (this.capacity / this.rows) - 1) * 4 - ((k < this.capacity / this.rows && this.playerInventory && this.verticalGap == 0) ? 12 : 0));
				b.Draw(texture, toDraw2, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), tint, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);
				if ((this.playerInventory || this.showGrayedOutSlots) && k >= (int)Game1.player.maxItems)
				{
					b.Draw(texture, toDraw2, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57), tint * 0.5f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);
				}
				if (!Game1.options.gamepadControls && k < 12 && this.playerInventory)
				{
					string strToDraw = k switch
					{
						11 => "=", 
						10 => "-", 
						9 => "0", 
						_ => (k + 1).ToString() ?? "", 
					};
					Vector2 strSize = Game1.tinyFont.MeasureString(strToDraw);
					b.DrawString(Game1.tinyFont, strToDraw, toDraw2 + new Vector2(32f - strSize.X / 2f, 0f - strSize.Y), (k == Game1.player.CurrentToolIndex) ? Color.Red : Color.DimGray);
				}
			}
			for (int i = 0; i < this.capacity; i++)
			{
				Vector2 toDraw = new Vector2(base.xPositionOnScreen + i % (this.capacity / this.rows) * 64 + this.horizontalGap * (i % (this.capacity / this.rows)), base.yPositionOnScreen + i / (this.capacity / this.rows) * (64 + this.verticalGap) + (i / (this.capacity / this.rows) - 1) * 4 - ((i < this.capacity / this.rows && this.playerInventory && this.verticalGap == 0) ? 12 : 0));
				if (this.actualInventory.Count > i && this.actualInventory[i] != null)
				{
					bool highlight = this.highlightMethod(this.actualInventory[i]);
					if (this._iconShakeTimer.ContainsKey(i))
					{
						toDraw += 1f * new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
					}
					this.actualInventory[i].drawInMenu(b, toDraw, (this.inventory.Count > i) ? this.inventory[i].scale : 1f, (!this.highlightMethod(this.actualInventory[i])) ? 0.25f : 1f, 0.865f, StackDrawType.Draw, Color.White, highlight);
				}
			}
			return;
		}
		for (int l = 0; l < this.capacity; l++)
		{
			Vector2 toDraw3 = new Vector2(base.xPositionOnScreen + l % (this.capacity / this.rows) * 64 + this.horizontalGap * (l % (this.capacity / this.rows)), base.yPositionOnScreen + l / (this.capacity / this.rows) * (64 + this.verticalGap) + (l / (this.capacity / this.rows) - 1) * 4 - ((l < this.capacity / this.rows && this.playerInventory && this.verticalGap == 0) ? 12 : 0));
			if (this.actualInventory.Count > l && this.actualInventory[l] != null)
			{
				bool highlight2 = this.highlightMethod(this.actualInventory[l]);
				if (this._iconShakeTimer.ContainsKey(l))
				{
					toDraw3 += 1f * new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
				}
				this.actualInventory[l].drawInMenu(b, toDraw3, (this.inventory.Count > l) ? this.inventory[l].scale : 1f, (!highlight2) ? 0.25f : 1f, 0.865f, StackDrawType.Draw, Color.White, highlight2);
			}
		}
	}

	public List<Vector2> GetSlotDrawPositions()
	{
		List<Vector2> slot_draw_positions = new List<Vector2>();
		for (int i = 0; i < this.capacity; i++)
		{
			slot_draw_positions.Add(new Vector2(base.xPositionOnScreen + i % (this.capacity / this.rows) * 64 + this.horizontalGap * (i % (this.capacity / this.rows)), base.yPositionOnScreen + i / (this.capacity / this.rows) * (64 + this.verticalGap) + (i / (this.capacity / this.rows) - 1) * 4 - ((i < this.capacity / this.rows && this.playerInventory && this.verticalGap == 0) ? 12 : 0)));
		}
		return slot_draw_positions;
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
	}

	public override void performHoverAction(int x, int y)
	{
	}
}
