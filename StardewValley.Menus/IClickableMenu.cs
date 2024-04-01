using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Enchantments;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley.Menus;

[InstanceStatics]
public abstract class IClickableMenu
{
	public delegate void onExit();

	protected IClickableMenu _childMenu;

	protected IClickableMenu _parentMenu;

	public const int upperRightCloseButton_ID = 9175502;

	public const int currency_g = 0;

	public const int currency_starTokens = 1;

	public const int currency_qiCoins = 2;

	public const int currency_qiGems = 4;

	public const int greyedOutSpotIndex = 57;

	public const int presentIconIndex = 58;

	public const int itemSpotIndex = 10;

	protected string closeSound = "bigDeSelect";

	public static int borderWidth = 40;

	public static int tabYPositionRelativeToMenuY = -48;

	public static int spaceToClearTopBorder = 96;

	public static int spaceToClearSideBorder = 16;

	public const int spaceBetweenTabs = 4;

	public int width;

	public int height;

	public int xPositionOnScreen;

	public int yPositionOnScreen;

	public Action<IClickableMenu> behaviorBeforeCleanup;

	public onExit exitFunction;

	public ClickableTextureComponent upperRightCloseButton;

	public bool destroy;

	protected int _dependencies;

	public List<ClickableComponent> allClickableComponents;

	public ClickableComponent currentlySnappedComponent;

	public static StringBuilder HoverTextStringBuilder = new StringBuilder();

	public Vector2 Position => new Vector2(this.xPositionOnScreen, this.yPositionOnScreen);

	public IClickableMenu()
	{
	}

	public IClickableMenu(int x, int y, int width, int height, bool showUpperRightCloseButton = false)
	{
		Game1.mouseCursorTransparency = 1f;
		this.initialize(x, y, width, height, showUpperRightCloseButton);
		if (Game1.gameMode == 3 && Game1.player != null && !Game1.eventUp)
		{
			Game1.player.Halt();
		}
	}

	public void initialize(int x, int y, int width, int height, bool showUpperRightCloseButton = false)
	{
		if (Game1.player != null && !Game1.player.UsingTool && !Game1.eventUp)
		{
			Game1.player.forceCanMove();
		}
		this.xPositionOnScreen = x;
		this.yPositionOnScreen = y;
		this.width = width;
		this.height = height;
		if (showUpperRightCloseButton)
		{
			this.upperRightCloseButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + width - 36, this.yPositionOnScreen - 8, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f)
			{
				myID = 9175502
			};
		}
		for (int i = 0; i < 4; i++)
		{
			Game1.directionKeyPolling[i] = 250;
		}
	}

	public IClickableMenu GetChildMenu()
	{
		return this._childMenu;
	}

	public IClickableMenu GetParentMenu()
	{
		return this._parentMenu;
	}

	public void SetChildMenu(IClickableMenu menu)
	{
		this._childMenu = menu;
		if (this._childMenu != null)
		{
			this._childMenu._parentMenu = this;
		}
	}

	public void AddDependency()
	{
		this._dependencies++;
	}

	public void RemoveDependency()
	{
		this._dependencies--;
		if (this._dependencies <= 0 && Game1.activeClickableMenu != this && TitleMenu.subMenu != this)
		{
			(this as IDisposable)?.Dispose();
		}
	}

	public bool HasDependencies()
	{
		return this._dependencies > 0;
	}

	public virtual bool areGamePadControlsImplemented()
	{
		return false;
	}

	public virtual void receiveGamePadButton(Buttons b)
	{
	}

	public void drawMouse(SpriteBatch b, bool ignore_transparency = false, int cursor = -1)
	{
		if (!Game1.options.hardwareCursor)
		{
			float transparency = Game1.mouseCursorTransparency;
			if (ignore_transparency)
			{
				transparency = 1f;
			}
			if (cursor < 0)
			{
				cursor = ((Game1.options.snappyMenus && Game1.options.gamepadControls) ? 44 : 0);
			}
			b.Draw(Game1.mouseCursors, new Vector2(Game1.getMouseX(), Game1.getMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, cursor, 16, 16), Color.White * transparency, 0f, Vector2.Zero, 4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
		}
	}

	public virtual void populateClickableComponentList()
	{
		this.allClickableComponents = new List<ClickableComponent>();
		FieldInfo[] fields = base.GetType().GetFields();
		foreach (FieldInfo f in fields)
		{
			if (f.GetCustomAttribute<SkipForClickableAggregation>() != null || f.DeclaringType == typeof(IClickableMenu))
			{
				continue;
			}
			if (f.FieldType.IsSubclassOf(typeof(ClickableComponent)) || f.FieldType == typeof(ClickableComponent))
			{
				if (f.GetValue(this) != null)
				{
					this.allClickableComponents.Add((ClickableComponent)f.GetValue(this));
				}
			}
			else if (f.FieldType == typeof(List<ClickableComponent>))
			{
				List<ClickableComponent> n = (List<ClickableComponent>)f.GetValue(this);
				if (n == null)
				{
					continue;
				}
				for (int i = n.Count - 1; i >= 0; i--)
				{
					if (n[i] != null)
					{
						this.allClickableComponents.Add(n[i]);
					}
				}
			}
			else if (f.FieldType == typeof(List<ClickableTextureComponent>))
			{
				List<ClickableTextureComponent> l2 = (List<ClickableTextureComponent>)f.GetValue(this);
				if (l2 == null)
				{
					continue;
				}
				for (int j = l2.Count - 1; j >= 0; j--)
				{
					if (l2[j] != null)
					{
						this.allClickableComponents.Add(l2[j]);
					}
				}
			}
			else if (f.FieldType == typeof(List<List<ClickableTextureComponent>>))
			{
				List<List<ClickableTextureComponent>> l3 = (List<List<ClickableTextureComponent>>)f.GetValue(this);
				if (l3 == null)
				{
					continue;
				}
				for (int k = l3.Count - 1; k >= 0; k--)
				{
					if (l3[k] != null)
					{
						this.allClickableComponents.AddRange(l3[k]);
					}
				}
			}
			else if (f.FieldType == typeof(List<ClickableAnimatedComponent>))
			{
				List<ClickableAnimatedComponent> l4 = (List<ClickableAnimatedComponent>)f.GetValue(this);
				for (int l = l4.Count - 1; l >= 0; l--)
				{
					if (l4[l] != null)
					{
						this.allClickableComponents.Add(l4[l]);
					}
				}
			}
			else if (f.FieldType == typeof(List<Bundle>))
			{
				List<Bundle> l5 = (List<Bundle>)f.GetValue(this);
				for (int m = l5.Count - 1; m >= 0; m--)
				{
					if (l5[m] != null)
					{
						this.allClickableComponents.Add(l5[m]);
					}
				}
			}
			else if (f.FieldType == typeof(InventoryMenu))
			{
				this.allClickableComponents.AddRange(((InventoryMenu)f.GetValue(this)).inventory);
				this.allClickableComponents.Add(((InventoryMenu)f.GetValue(this)).dropItemInvisibleButton);
			}
			else if (f.FieldType == typeof(List<Dictionary<ClickableTextureComponent, CraftingRecipe>>))
			{
				foreach (Dictionary<ClickableTextureComponent, CraftingRecipe> d in (List<Dictionary<ClickableTextureComponent, CraftingRecipe>>)f.GetValue(this))
				{
					this.allClickableComponents.AddRange(d.Keys);
				}
			}
			else if (f.FieldType == typeof(Dictionary<int, List<List<ClickableTextureComponent>>>))
			{
				foreach (List<List<ClickableTextureComponent>> value in ((Dictionary<int, List<List<ClickableTextureComponent>>>)f.GetValue(this)).Values)
				{
					foreach (List<ClickableTextureComponent> l7 in value)
					{
						this.allClickableComponents.AddRange(l7);
					}
				}
			}
			else
			{
				if (!(f.FieldType == typeof(Dictionary<int, ClickableTextureComponent>)))
				{
					continue;
				}
				foreach (ClickableTextureComponent l6 in ((Dictionary<int, ClickableTextureComponent>)f.GetValue(this)).Values)
				{
					this.allClickableComponents.Add(l6);
				}
			}
		}
		if (Game1.activeClickableMenu is GameMenu game_menu && this == game_menu.GetCurrentPage())
		{
			game_menu.AddTabsToClickableComponents(this);
		}
		if (this.upperRightCloseButton != null)
		{
			this.allClickableComponents.Add(this.upperRightCloseButton);
		}
	}

	public virtual void applyMovementKey(int direction)
	{
		if (this.allClickableComponents == null)
		{
			this.populateClickableComponentList();
		}
		this.moveCursorInDirection(direction);
	}

	/// <summary>
	/// return true if this method is overriden and a default clickablecomponent is snapped to.
	/// </summary>
	/// <returns></returns>
	public virtual void snapToDefaultClickableComponent()
	{
	}

	public void applyMovementKey(Keys key)
	{
		if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
		{
			this.applyMovementKey(0);
		}
		else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
		{
			this.applyMovementKey(1);
		}
		else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
		{
			this.applyMovementKey(2);
		}
		else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
		{
			this.applyMovementKey(3);
		}
	}

	/// <summary>
	/// Only use this if the child class overrides
	/// </summary>
	/// <param name="id"></param>
	public virtual void setCurrentlySnappedComponentTo(int id)
	{
		this.currentlySnappedComponent = this.getComponentWithID(id);
	}

	public void moveCursorInDirection(int direction)
	{
		if (this.currentlySnappedComponent == null)
		{
			List<ClickableComponent> list = this.allClickableComponents;
			if (list != null && list.Count > 0)
			{
				this.snapToDefaultClickableComponent();
				if (this.currentlySnappedComponent == null)
				{
					this.currentlySnappedComponent = this.allClickableComponents[0];
				}
			}
		}
		if (this.currentlySnappedComponent == null)
		{
			return;
		}
		ClickableComponent old = this.currentlySnappedComponent;
		switch (direction)
		{
		case 0:
			if (this.currentlySnappedComponent.upNeighborID == -99999)
			{
				this.snapToDefaultClickableComponent();
			}
			else if (this.currentlySnappedComponent.upNeighborID == -99998)
			{
				this.automaticSnapBehavior(0, this.currentlySnappedComponent.region, this.currentlySnappedComponent.myID);
			}
			else if (this.currentlySnappedComponent.upNeighborID == -7777)
			{
				this.customSnapBehavior(0, this.currentlySnappedComponent.region, this.currentlySnappedComponent.myID);
			}
			else
			{
				this.currentlySnappedComponent = this.getComponentWithID(this.currentlySnappedComponent.upNeighborID);
			}
			if (this.currentlySnappedComponent != null && (old == null || (old.upNeighborID != -7777 && old.upNeighborID != -99998)) && !this.currentlySnappedComponent.downNeighborImmutable && !this.currentlySnappedComponent.fullyImmutable)
			{
				this.currentlySnappedComponent.downNeighborID = old.myID;
			}
			if (this.currentlySnappedComponent == null)
			{
				this.noSnappedComponentFound(0, old.region, old.myID);
			}
			break;
		case 1:
			if (this.currentlySnappedComponent.rightNeighborID == -99999)
			{
				this.snapToDefaultClickableComponent();
			}
			else if (this.currentlySnappedComponent.rightNeighborID == -99998)
			{
				this.automaticSnapBehavior(1, this.currentlySnappedComponent.region, this.currentlySnappedComponent.myID);
			}
			else if (this.currentlySnappedComponent.rightNeighborID == -7777)
			{
				this.customSnapBehavior(1, this.currentlySnappedComponent.region, this.currentlySnappedComponent.myID);
			}
			else
			{
				this.currentlySnappedComponent = this.getComponentWithID(this.currentlySnappedComponent.rightNeighborID);
			}
			if (this.currentlySnappedComponent != null && (old == null || (old.rightNeighborID != -7777 && old.rightNeighborID != -99998)) && !this.currentlySnappedComponent.leftNeighborImmutable && !this.currentlySnappedComponent.fullyImmutable)
			{
				this.currentlySnappedComponent.leftNeighborID = old.myID;
			}
			if (this.currentlySnappedComponent == null && old.tryDefaultIfNoRightNeighborExists)
			{
				this.snapToDefaultClickableComponent();
			}
			else if (this.currentlySnappedComponent == null)
			{
				this.noSnappedComponentFound(1, old.region, old.myID);
			}
			break;
		case 2:
			if (this.currentlySnappedComponent.downNeighborID == -99999)
			{
				this.snapToDefaultClickableComponent();
			}
			else if (this.currentlySnappedComponent.downNeighborID == -99998)
			{
				this.automaticSnapBehavior(2, this.currentlySnappedComponent.region, this.currentlySnappedComponent.myID);
			}
			else if (this.currentlySnappedComponent.downNeighborID == -7777)
			{
				this.customSnapBehavior(2, this.currentlySnappedComponent.region, this.currentlySnappedComponent.myID);
			}
			else
			{
				this.currentlySnappedComponent = this.getComponentWithID(this.currentlySnappedComponent.downNeighborID);
			}
			if (this.currentlySnappedComponent != null && (old == null || (old.downNeighborID != -7777 && old.downNeighborID != -99998)) && !this.currentlySnappedComponent.upNeighborImmutable && !this.currentlySnappedComponent.fullyImmutable)
			{
				this.currentlySnappedComponent.upNeighborID = old.myID;
			}
			if (this.currentlySnappedComponent == null && old.tryDefaultIfNoDownNeighborExists)
			{
				this.snapToDefaultClickableComponent();
			}
			else if (this.currentlySnappedComponent == null)
			{
				this.noSnappedComponentFound(2, old.region, old.myID);
			}
			break;
		case 3:
			if (this.currentlySnappedComponent.leftNeighborID == -99999)
			{
				this.snapToDefaultClickableComponent();
			}
			else if (this.currentlySnappedComponent.leftNeighborID == -99998)
			{
				this.automaticSnapBehavior(3, this.currentlySnappedComponent.region, this.currentlySnappedComponent.myID);
			}
			else if (this.currentlySnappedComponent.leftNeighborID == -7777)
			{
				this.customSnapBehavior(3, this.currentlySnappedComponent.region, this.currentlySnappedComponent.myID);
			}
			else
			{
				this.currentlySnappedComponent = this.getComponentWithID(this.currentlySnappedComponent.leftNeighborID);
			}
			if (this.currentlySnappedComponent != null && (old == null || (old.leftNeighborID != -7777 && old.leftNeighborID != -99998)) && !this.currentlySnappedComponent.rightNeighborImmutable && !this.currentlySnappedComponent.fullyImmutable)
			{
				this.currentlySnappedComponent.rightNeighborID = old.myID;
			}
			if (this.currentlySnappedComponent == null)
			{
				this.noSnappedComponentFound(3, old.region, old.myID);
			}
			break;
		}
		if (this.currentlySnappedComponent != null && old != null && this.currentlySnappedComponent.region != old.region)
		{
			this.actionOnRegionChange(old.region, this.currentlySnappedComponent.region);
		}
		if (this.currentlySnappedComponent == null)
		{
			this.currentlySnappedComponent = old;
		}
		this.snapCursorToCurrentSnappedComponent();
		if (this.currentlySnappedComponent != old)
		{
			Game1.playSound("shiny4");
		}
	}

	public virtual void snapCursorToCurrentSnappedComponent()
	{
		if (this.currentlySnappedComponent != null)
		{
			Game1.setMousePosition(this.currentlySnappedComponent.bounds.Right - this.currentlySnappedComponent.bounds.Width / 4, this.currentlySnappedComponent.bounds.Bottom - this.currentlySnappedComponent.bounds.Height / 4, ui_scale: true);
		}
	}

	protected virtual void noSnappedComponentFound(int direction, int oldRegion, int oldID)
	{
	}

	protected virtual void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
	}

	public virtual bool IsActive()
	{
		if (this._parentMenu == null)
		{
			return this == Game1.activeClickableMenu;
		}
		IClickableMenu root = this._parentMenu;
		while (root?._parentMenu != null)
		{
			root = root._parentMenu;
		}
		return root == Game1.activeClickableMenu;
	}

	public virtual void automaticSnapBehavior(int direction, int oldRegion, int oldID)
	{
		if (this.currentlySnappedComponent == null)
		{
			this.snapToDefaultClickableComponent();
			return;
		}
		Vector2 snap_direction = Vector2.Zero;
		switch (direction)
		{
		case 3:
			snap_direction.X = -1f;
			snap_direction.Y = 0f;
			break;
		case 1:
			snap_direction.X = 1f;
			snap_direction.Y = 0f;
			break;
		case 0:
			snap_direction.X = 0f;
			snap_direction.Y = -1f;
			break;
		case 2:
			snap_direction.X = 0f;
			snap_direction.Y = 1f;
			break;
		}
		float closest_distance = -1f;
		ClickableComponent closest_component_in_direction = null;
		for (int i = 0; i < this.allClickableComponents.Count; i++)
		{
			ClickableComponent other_component = this.allClickableComponents[i];
			if ((other_component.leftNeighborID == -1 && other_component.rightNeighborID == -1 && other_component.upNeighborID == -1 && other_component.downNeighborID == -1) || other_component.myID == -500 || !this.IsAutomaticSnapValid(direction, this.currentlySnappedComponent, other_component) || !other_component.visible || other_component == this.upperRightCloseButton || other_component == this.currentlySnappedComponent)
			{
				continue;
			}
			Vector2 offset = new Vector2(other_component.bounds.Center.X - this.currentlySnappedComponent.bounds.Center.X, other_component.bounds.Center.Y - this.currentlySnappedComponent.bounds.Center.Y);
			Vector2 normalized_offset = new Vector2(offset.X, offset.Y);
			normalized_offset.Normalize();
			float dot = Vector2.Dot(snap_direction, normalized_offset);
			if (!(dot > 0.01f))
			{
				continue;
			}
			float score = Vector2.DistanceSquared(Vector2.Zero, offset);
			bool close_enough = false;
			switch (direction)
			{
			case 0:
			case 2:
				if (Math.Abs(offset.X) < 32f)
				{
					close_enough = true;
				}
				break;
			case 1:
			case 3:
				if (Math.Abs(offset.Y) < 32f)
				{
					close_enough = true;
				}
				break;
			}
			if (this._ShouldAutoSnapPrioritizeAlignedElements() && (dot > 0.99999f || close_enough))
			{
				score *= 0.01f;
			}
			if (closest_distance == -1f || score < closest_distance)
			{
				closest_distance = score;
				closest_component_in_direction = other_component;
			}
		}
		if (closest_component_in_direction != null)
		{
			this.currentlySnappedComponent = closest_component_in_direction;
		}
	}

	protected virtual bool _ShouldAutoSnapPrioritizeAlignedElements()
	{
		return true;
	}

	public virtual bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
	{
		return true;
	}

	/// <summary>
	/// when the currentlySnappedComponent region changes 
	/// </summary>
	protected virtual void actionOnRegionChange(int oldRegion, int newRegion)
	{
	}

	public ClickableComponent getComponentWithID(int id)
	{
		if (id == -500)
		{
			return null;
		}
		if (this.allClickableComponents != null)
		{
			for (int j = 0; j < this.allClickableComponents.Count; j++)
			{
				if (this.allClickableComponents[j] != null && this.allClickableComponents[j].myID == id && this.allClickableComponents[j].visible)
				{
					return this.allClickableComponents[j];
				}
			}
			for (int i = 0; i < this.allClickableComponents.Count; i++)
			{
				if (this.allClickableComponents[i] != null && this.allClickableComponents[i].myAlternateID == id && this.allClickableComponents[i].visible)
				{
					return this.allClickableComponents[i];
				}
			}
		}
		return null;
	}

	public void initializeUpperRightCloseButton()
	{
		this.upperRightCloseButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width - 36, this.yPositionOnScreen - 8, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);
	}

	public virtual void drawBackground(SpriteBatch b)
	{
		if (this is ShopMenu)
		{
			for (int x = 0; x < Game1.uiViewport.Width; x += 400)
			{
				for (int y = 0; y < Game1.uiViewport.Height; y += 384)
				{
					b.Draw(Game1.mouseCursors, new Vector2(x, y), new Rectangle(527, 0, 100, 96), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.08f);
				}
			}
			return;
		}
		if (Game1.isDarkOut(Game1.currentLocation))
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(639, 858, 1, 144), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
		}
		else if (Game1.IsRainingHere())
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(640, 858, 1, 184), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
		}
		else
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(639 + Game1.seasonIndex, 1051, 1, 400), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
		}
		b.Draw(Game1.mouseCursors, new Vector2(-120f, Game1.uiViewport.Height - 592), new Rectangle(0, (Game1.season == Season.Winter) ? 1035 : ((Game1.isRaining || Game1.isDarkOut(Game1.currentLocation)) ? 886 : 737), 639, 148), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.08f);
		b.Draw(Game1.mouseCursors, new Vector2(2436f, Game1.uiViewport.Height - 592), new Rectangle(0, (Game1.season == Season.Winter) ? 1035 : ((Game1.isRaining || Game1.isDarkOut(Game1.currentLocation)) ? 886 : 737), 639, 148), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.08f);
		if (Game1.isRaining)
		{
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Blue * 0.2f);
		}
	}

	public virtual bool showWithoutTransparencyIfOptionIsSet()
	{
		if (this is GameMenu || this is ShopMenu || this is WheelSpinGame || this is ItemGrabMenu)
		{
			return true;
		}
		return false;
	}

	public virtual void clickAway()
	{
	}

	public virtual void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		this.xPositionOnScreen = (int)((float)(newBounds.Width - this.width) * ((float)this.xPositionOnScreen / (float)(oldBounds.Width - this.width)));
		this.yPositionOnScreen = (int)((float)(newBounds.Height - this.height) * ((float)this.yPositionOnScreen / (float)(oldBounds.Height - this.height)));
	}

	public virtual void setUpForGamePadMode()
	{
	}

	public virtual bool shouldClampGamePadCursor()
	{
		return false;
	}

	public virtual void releaseLeftClick(int x, int y)
	{
	}

	public virtual void leftClickHeld(int x, int y)
	{
	}

	public virtual void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (this.upperRightCloseButton != null && this.readyToClose() && this.upperRightCloseButton.containsPoint(x, y))
		{
			if (playSound)
			{
				Game1.playSound(this.closeSound);
			}
			this.exitThisMenu();
		}
	}

	public virtual bool overrideSnappyMenuCursorMovementBan()
	{
		return false;
	}

	public virtual void receiveRightClick(int x, int y, bool playSound = true)
	{
	}

	public virtual void receiveKeyPress(Keys key)
	{
		if (key != 0)
		{
			if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && this.readyToClose())
			{
				this.exitThisMenu();
			}
			else if (Game1.options.snappyMenus && Game1.options.gamepadControls && !this.overrideSnappyMenuCursorMovementBan())
			{
				this.applyMovementKey(key);
			}
		}
	}

	public virtual void gamePadButtonHeld(Buttons b)
	{
	}

	public virtual ClickableComponent getCurrentlySnappedComponent()
	{
		return this.currentlySnappedComponent;
	}

	public virtual void receiveScrollWheelAction(int direction)
	{
	}

	public virtual void performHoverAction(int x, int y)
	{
		this.upperRightCloseButton?.tryHover(x, y, 0.5f);
	}

	public virtual void draw(SpriteBatch b, int red = -1, int green = -1, int blue = -1)
	{
		if (this.upperRightCloseButton != null && this.shouldDrawCloseButton())
		{
			this.upperRightCloseButton.draw(b);
		}
	}

	public virtual void draw(SpriteBatch b)
	{
		if (this.upperRightCloseButton != null && this.shouldDrawCloseButton())
		{
			this.upperRightCloseButton.draw(b);
		}
	}

	public virtual bool isWithinBounds(int x, int y)
	{
		if (x - this.xPositionOnScreen < this.width && x - this.xPositionOnScreen >= 0 && y - this.yPositionOnScreen < this.height)
		{
			return y - this.yPositionOnScreen >= 0;
		}
		return false;
	}

	public virtual void update(GameTime time)
	{
	}

	protected virtual void cleanupBeforeExit()
	{
	}

	public virtual bool shouldDrawCloseButton()
	{
		return true;
	}

	public void exitThisMenuNoSound()
	{
		this.exitThisMenu(playSound: false);
	}

	public void exitThisMenu(bool playSound = true)
	{
		this.behaviorBeforeCleanup?.Invoke(this);
		this.cleanupBeforeExit();
		if (playSound)
		{
			Game1.playSound(this.closeSound);
		}
		if (this == Game1.activeClickableMenu)
		{
			Game1.exitActiveMenu();
		}
		else if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.GetCurrentPage() == this)
		{
			Game1.exitActiveMenu();
		}
		if (this._parentMenu != null)
		{
			IClickableMenu parentMenu = this._parentMenu;
			this._parentMenu = null;
			parentMenu.SetChildMenu(null);
		}
		if (this.exitFunction != null)
		{
			onExit onExit = this.exitFunction;
			this.exitFunction = null;
			onExit();
		}
	}

	public virtual void emergencyShutDown()
	{
	}

	public virtual bool readyToClose()
	{
		return true;
	}

	protected void drawHorizontalPartition(SpriteBatch b, int yPosition, bool small = false, int red = -1, int green = -1, int blue = -1)
	{
		Color tint = ((red == -1) ? Color.White : new Color(red, green, blue));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		if (small)
		{
			b.Draw(texture, new Rectangle(this.xPositionOnScreen + 32, yPosition, this.width - 64, 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 25), tint);
			return;
		}
		b.Draw(texture, new Vector2(this.xPositionOnScreen, yPosition), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 4), tint);
		b.Draw(texture, new Rectangle(this.xPositionOnScreen + 64, yPosition, this.width - 128, 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 6), tint);
		b.Draw(texture, new Vector2(this.xPositionOnScreen + this.width - 64, yPosition), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 7), tint);
	}

	protected void drawVerticalPartition(SpriteBatch b, int xPosition, bool small = false, int red = -1, int green = -1, int blue = -1, int heightOverride = -1)
	{
		Color tint = ((red == -1) ? Color.White : new Color(red, green, blue));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		if (small)
		{
			b.Draw(texture, new Rectangle(xPosition, this.yPositionOnScreen + 64 + 32, 64, (heightOverride != -1) ? heightOverride : (this.height - 128)), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 26), tint);
			return;
		}
		b.Draw(texture, new Vector2(xPosition, this.yPositionOnScreen + 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 1), tint);
		b.Draw(texture, new Rectangle(xPosition, this.yPositionOnScreen + 128, 64, (heightOverride != -1) ? heightOverride : (this.height - 192)), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 5), tint);
		b.Draw(texture, new Vector2(xPosition, this.yPositionOnScreen + ((heightOverride != -1) ? heightOverride : (this.height - 64))), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 13), tint);
	}

	protected void drawVerticalIntersectingPartition(SpriteBatch b, int xPosition, int yPosition, int red = -1, int green = -1, int blue = -1)
	{
		Color tint = ((red == -1) ? Color.White : new Color(red, green, blue));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		b.Draw(texture, new Vector2(xPosition, yPosition), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 59), tint);
		b.Draw(texture, new Rectangle(xPosition, yPosition + 64, 64, this.yPositionOnScreen + this.height - 64 - yPosition - 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 63), tint);
		b.Draw(texture, new Vector2(xPosition, this.yPositionOnScreen + this.height - 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 62), tint);
	}

	protected void drawVerticalUpperIntersectingPartition(SpriteBatch b, int xPosition, int partitionHeight, int red = -1, int green = -1, int blue = -1)
	{
		Color tint = ((red == -1) ? Color.White : new Color(red, green, blue));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		b.Draw(texture, new Vector2(xPosition, this.yPositionOnScreen + 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 44), tint);
		b.Draw(texture, new Rectangle(xPosition, this.yPositionOnScreen + 128, 64, partitionHeight - 32), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 63), tint);
		b.Draw(texture, new Vector2(xPosition, this.yPositionOnScreen + partitionHeight + 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 39), tint);
	}

	public static void drawTextureBox(SpriteBatch b, int x, int y, int width, int height, Color color)
	{
		IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, color);
	}

	public static void drawTextureBox(SpriteBatch b, Texture2D texture, Rectangle sourceRect, int x, int y, int width, int height, Color color, float scale = 1f, bool drawShadow = true, float draw_layer = -1f)
	{
		int cornerSize = sourceRect.Width / 3;
		float shadow_layer = draw_layer - 0.03f;
		if (draw_layer < 0f)
		{
			draw_layer = 0.8f - (float)y * 1E-06f;
			shadow_layer = 0.77f;
		}
		if (drawShadow)
		{
			b.Draw(texture, new Vector2(x + width - (int)((float)cornerSize * scale) - 8, y + 8), new Rectangle(sourceRect.X + cornerSize * 2, sourceRect.Y, cornerSize, cornerSize), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, shadow_layer);
			b.Draw(texture, new Vector2(x - 8, y + height - (int)((float)cornerSize * scale) + 8), new Rectangle(sourceRect.X, cornerSize * 2 + sourceRect.Y, cornerSize, cornerSize), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, shadow_layer);
			b.Draw(texture, new Vector2(x + width - (int)((float)cornerSize * scale) - 8, y + height - (int)((float)cornerSize * scale) + 8), new Rectangle(sourceRect.X + cornerSize * 2, cornerSize * 2 + sourceRect.Y, cornerSize, cornerSize), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, shadow_layer);
			b.Draw(texture, new Rectangle(x + (int)((float)cornerSize * scale) - 8, y + 8, width - (int)((float)cornerSize * scale) * 2, (int)((float)cornerSize * scale)), new Rectangle(sourceRect.X + cornerSize, sourceRect.Y, cornerSize, cornerSize), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
			b.Draw(texture, new Rectangle(x + (int)((float)cornerSize * scale) - 8, y + height - (int)((float)cornerSize * scale) + 8, width - (int)((float)cornerSize * scale) * 2, (int)((float)cornerSize * scale)), new Rectangle(sourceRect.X + cornerSize, cornerSize * 2 + sourceRect.Y, cornerSize, cornerSize), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
			b.Draw(texture, new Rectangle(x - 8, y + (int)((float)cornerSize * scale) + 8, (int)((float)cornerSize * scale), height - (int)((float)cornerSize * scale) * 2), new Rectangle(sourceRect.X, cornerSize + sourceRect.Y, cornerSize, cornerSize), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
			b.Draw(texture, new Rectangle(x + width - (int)((float)cornerSize * scale) - 8, y + (int)((float)cornerSize * scale) + 8, (int)((float)cornerSize * scale), height - (int)((float)cornerSize * scale) * 2), new Rectangle(sourceRect.X + cornerSize * 2, cornerSize + sourceRect.Y, cornerSize, cornerSize), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
			b.Draw(texture, new Rectangle((int)((float)cornerSize * scale / 2f) + x - 8, (int)((float)cornerSize * scale / 2f) + y + 8, width - (int)((float)cornerSize * scale), height - (int)((float)cornerSize * scale)), new Rectangle(cornerSize + sourceRect.X, cornerSize + sourceRect.Y, cornerSize, cornerSize), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, shadow_layer);
		}
		b.Draw(texture, new Rectangle((int)((float)cornerSize * scale) + x, (int)((float)cornerSize * scale) + y, width - (int)((float)cornerSize * scale * 2f), height - (int)((float)cornerSize * scale * 2f)), new Rectangle(cornerSize + sourceRect.X, cornerSize + sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Vector2(x, y), new Rectangle(sourceRect.X, sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Vector2(x + width - (int)((float)cornerSize * scale), y), new Rectangle(sourceRect.X + cornerSize * 2, sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Vector2(x, y + height - (int)((float)cornerSize * scale)), new Rectangle(sourceRect.X, cornerSize * 2 + sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Vector2(x + width - (int)((float)cornerSize * scale), y + height - (int)((float)cornerSize * scale)), new Rectangle(sourceRect.X + cornerSize * 2, cornerSize * 2 + sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Rectangle(x + (int)((float)cornerSize * scale), y, width - (int)((float)cornerSize * scale) * 2, (int)((float)cornerSize * scale)), new Rectangle(sourceRect.X + cornerSize, sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Rectangle(x + (int)((float)cornerSize * scale), y + height - (int)((float)cornerSize * scale), width - (int)((float)cornerSize * scale) * 2, (int)((float)cornerSize * scale)), new Rectangle(sourceRect.X + cornerSize, cornerSize * 2 + sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Rectangle(x, y + (int)((float)cornerSize * scale), (int)((float)cornerSize * scale), height - (int)((float)cornerSize * scale) * 2), new Rectangle(sourceRect.X, cornerSize + sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Rectangle(x + width - (int)((float)cornerSize * scale), y + (int)((float)cornerSize * scale), (int)((float)cornerSize * scale), height - (int)((float)cornerSize * scale) * 2), new Rectangle(sourceRect.X + cornerSize * 2, cornerSize + sourceRect.Y, cornerSize, cornerSize), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
	}

	public void drawBorderLabel(SpriteBatch b, string text, SpriteFont font, int x, int y)
	{
		int width = (int)font.MeasureString(text).X;
		y += 52;
		b.Draw(Game1.mouseCursors, new Vector2(x, y), new Rectangle(256, 267, 6, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
		b.Draw(Game1.mouseCursors, new Vector2(x + 24, y), new Rectangle(262, 267, 1, 16), Color.White, 0f, Vector2.Zero, new Vector2(width, 4f), SpriteEffects.None, 0.87f);
		b.Draw(Game1.mouseCursors, new Vector2(x + 24 + width, y), new Rectangle(263, 267, 6, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
		Utility.drawTextWithShadow(b, text, font, new Vector2(x + 24, y + 20), Game1.textColor);
	}

	public static void drawToolTip(SpriteBatch b, string hoverText, string hoverTitle, Item hoveredItem, bool heldItem = false, int healAmountToDisplay = -1, int currencySymbol = 0, string extraItemToShowIndex = null, int extraItemToShowAmount = -1, CraftingRecipe craftingIngredients = null, int moneyAmountToShowAtBottom = -1)
	{
		bool edibleItem = hoveredItem is Object hoveredObj && (int)hoveredObj.edibility != -300;
		string[] buffIcons = null;
		if (edibleItem && Game1.objectData.TryGetValue(hoveredItem.ItemId, out var rawData))
		{
			BuffEffects effects = new BuffEffects();
			int millisecondsDuration = int.MinValue;
			foreach (Buff buff in Object.TryCreateBuffsFromData(rawData, hoveredItem.Name, hoveredItem.DisplayName, 1f, hoveredItem.ModifyItemBuffs))
			{
				effects.Add(buff.effects);
				if (buff.millisecondsDuration == -2 || (buff.millisecondsDuration > millisecondsDuration && millisecondsDuration != -2))
				{
					millisecondsDuration = buff.millisecondsDuration;
				}
			}
			if (effects.HasAnyValue())
			{
				buffIcons = effects.ToLegacyAttributeFormat();
				if (millisecondsDuration != -2)
				{
					buffIcons[12] = " " + Utility.getMinutesSecondsStringFromMilliseconds(millisecondsDuration);
				}
			}
		}
		IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont, heldItem ? 40 : 0, heldItem ? 40 : 0, moneyAmountToShowAtBottom, hoverTitle, edibleItem ? ((int)(hoveredItem as Object).edibility) : (-1), buffIcons, hoveredItem, currencySymbol, extraItemToShowIndex, extraItemToShowAmount, -1, -1, 1f, craftingIngredients);
	}

	public static void drawHoverText(SpriteBatch b, string text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null, int currencySymbol = 0, string extraItemToShowIndex = null, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1f, CraftingRecipe craftingIngredients = null, IList<Item> additional_craft_materials = null, Texture2D boxTexture = null, Rectangle? boxSourceRect = null, Color? textColor = null, Color? textShadowColor = null, float boxScale = 1f, int boxWidthOverride = -1, int boxHeightOverride = -1)
	{
		IClickableMenu.HoverTextStringBuilder.Clear();
		IClickableMenu.HoverTextStringBuilder.Append(text);
		IClickableMenu.drawHoverText(b, IClickableMenu.HoverTextStringBuilder, font, xOffset, yOffset, moneyAmountToDisplayAtBottom, boldTitleText, healAmountToDisplay, buffIconsToDisplay, hoveredItem, currencySymbol, extraItemToShowIndex, extraItemToShowAmount, overrideX, overrideY, alpha, craftingIngredients, additional_craft_materials, boxTexture, boxSourceRect, textColor, textShadowColor, boxScale, boxWidthOverride, boxHeightOverride);
	}

	public static void drawHoverText(SpriteBatch b, StringBuilder text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null, int currencySymbol = 0, string extraItemToShowIndex = null, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1f, CraftingRecipe craftingIngredients = null, IList<Item> additional_craft_materials = null, Texture2D boxTexture = null, Rectangle? boxSourceRect = null, Color? textColor = null, Color? textShadowColor = null, float boxScale = 1f, int boxWidthOverride = -1, int boxHeightOverride = -1)
	{
		boxTexture = boxTexture ?? Game1.menuTexture;
		boxSourceRect = boxSourceRect ?? new Rectangle(0, 256, 60, 60);
		textColor = textColor ?? Game1.textColor;
		textShadowColor = textShadowColor ?? Game1.textShadowColor;
		if (text == null || text.Length == 0)
		{
			return;
		}
		if (moneyAmountToDisplayAtBottom <= -1 && currencySymbol == 0 && hoveredItem != null && Game1.player.stats.Get("Book_PriceCatalogue") != 0 && !(hoveredItem is Furniture) && hoveredItem.CanBeLostOnDeath() && !(hoveredItem is Clothing) && !(hoveredItem is Wallpaper) && (!(hoveredItem is Object) || !(hoveredItem as Object).bigCraftable) && hoveredItem.sellToStorePrice(-1L) > 0)
		{
			moneyAmountToDisplayAtBottom = hoveredItem.sellToStorePrice(-1L) * hoveredItem.Stack;
		}
		string bold_title_subtext = null;
		if (boldTitleText != null && boldTitleText.Length == 0)
		{
			boldTitleText = null;
		}
		int width = Math.Max((healAmountToDisplay != -1) ? ((int)font.MeasureString(healAmountToDisplay + "+ Energy" + 32).X) : 0, Math.Max((int)font.MeasureString(text).X, (boldTitleText != null) ? ((int)Game1.dialogueFont.MeasureString(boldTitleText).X) : 0)) + 32;
		int height = Math.Max(20 * 3, (int)font.MeasureString(text).Y + 32 + (int)((moneyAmountToDisplayAtBottom > -1) ? (font.MeasureString(moneyAmountToDisplayAtBottom.ToString() ?? "").Y + 4f) : 8f) + (int)((boldTitleText != null) ? (Game1.dialogueFont.MeasureString(boldTitleText).Y + 16f) : 0f));
		if (extraItemToShowIndex != null)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(O)" + extraItemToShowIndex);
			string objName = dataOrErrorItem.DisplayName;
			Rectangle sourceRect2 = dataOrErrorItem.GetSourceRect();
			string requirement2 = Game1.content.LoadString("Strings\\UI:ItemHover_Requirements", extraItemToShowAmount, (extraItemToShowAmount > 1) ? Lexicon.makePlural(objName) : objName);
			int spriteWidth = sourceRect2.Width * 2 * 4;
			width = Math.Max(width, spriteWidth + (int)font.MeasureString(requirement2).X);
		}
		if (buffIconsToDisplay != null)
		{
			foreach (string s in buffIconsToDisplay)
			{
				if (!s.Equals("0") && s != "")
				{
					height += 39;
				}
			}
			height += 4;
		}
		if (craftingIngredients != null && Game1.options.showAdvancedCraftingInformation && craftingIngredients.getCraftCountText() != null)
		{
			height += (int)font.MeasureString("T").Y;
		}
		string categoryName = null;
		if (hoveredItem != null)
		{
			if (hoveredItem is FishingRod)
			{
				if (hoveredItem.attachmentSlots() == 1)
				{
					height += 68;
				}
				else if (hoveredItem.attachmentSlots() > 1)
				{
					height += 136;
				}
			}
			else
			{
				height += 68 * hoveredItem.attachmentSlots();
			}
			categoryName = hoveredItem.getCategoryName();
			if (categoryName.Length > 0)
			{
				width = Math.Max(width, (int)font.MeasureString(categoryName).X + 32);
				height += (int)font.MeasureString("T").Y;
			}
			int maxStat = 9999;
			int buffer = 92;
			Point p = hoveredItem.getExtraSpaceNeededForTooltipSpecialIcons(font, width, buffer, height, text, boldTitleText, moneyAmountToDisplayAtBottom);
			width = ((p.X != 0) ? p.X : width);
			height = ((p.Y != 0) ? p.Y : height);
			if (hoveredItem is MeleeWeapon weapon2)
			{
				if (weapon2.GetTotalForgeLevels() > 0)
				{
					height += (int)font.MeasureString("T").Y;
				}
				if (weapon2.GetEnchantmentLevel<GalaxySoulEnchantment>() > 0)
				{
					height += (int)font.MeasureString("T").Y;
				}
			}
			if (hoveredItem is Object obj && (int)obj.edibility != -300)
			{
				height = ((healAmountToDisplay == -1) ? (height + 40) : (height + 40 * ((healAmountToDisplay <= 0) ? 1 : 2)));
				healAmountToDisplay = obj.staminaRecoveredOnConsumption();
				width = (int)Math.Max(width, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Energy", maxStat)).X + (float)buffer, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Health", maxStat)).X + (float)buffer));
			}
			if (buffIconsToDisplay != null)
			{
				for (int j = 0; j < buffIconsToDisplay.Length; j++)
				{
					if (!buffIconsToDisplay[j].Equals("0") && j <= 12)
					{
						width = (int)Math.Max(width, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Buff" + j, maxStat)).X + (float)buffer);
					}
				}
			}
		}
		Vector2 small_text_size = Vector2.Zero;
		if (craftingIngredients != null)
		{
			if (Game1.options.showAdvancedCraftingInformation)
			{
				int craftable_count = craftingIngredients.getCraftableCount(additional_craft_materials);
				if (craftable_count > 1)
				{
					bold_title_subtext = " (" + craftable_count + ")";
					small_text_size = Game1.smallFont.MeasureString(bold_title_subtext);
				}
			}
			width = (int)Math.Max(Game1.dialogueFont.MeasureString(boldTitleText).X + small_text_size.X + 12f, 384f);
			height += craftingIngredients.getDescriptionHeight(width - 8) + ((healAmountToDisplay == -1) ? (-32) : 0);
			if (craftingIngredients != null && hoveredItem != null && hoveredItem.getDescription().Equals(text.ToString()))
			{
				height -= (int)font.MeasureString(text.ToString()).Y;
			}
		}
		else if (bold_title_subtext != null && boldTitleText != null)
		{
			small_text_size = Game1.smallFont.MeasureString(bold_title_subtext);
			width = (int)Math.Max(width, Game1.dialogueFont.MeasureString(boldTitleText).X + small_text_size.X + 12f);
		}
		int x = Game1.getOldMouseX() + 32 + xOffset;
		int y = Game1.getOldMouseY() + 32 + yOffset;
		if (overrideX != -1)
		{
			x = overrideX;
		}
		if (overrideY != -1)
		{
			y = overrideY;
		}
		if (x + width > Utility.getSafeArea().Right)
		{
			x = Utility.getSafeArea().Right - width;
			y += 16;
		}
		if (y + height > Utility.getSafeArea().Bottom)
		{
			x += 16;
			if (x + width > Utility.getSafeArea().Right)
			{
				x = Utility.getSafeArea().Right - width;
			}
			y = Utility.getSafeArea().Bottom - height;
		}
		width += 4;
		int boxWidth = ((boxWidthOverride != -1) ? boxWidthOverride : (width + ((craftingIngredients != null) ? 21 : 0)));
		int boxHeight = ((boxHeightOverride != -1) ? boxHeightOverride : height);
		IClickableMenu.drawTextureBox(b, boxTexture, boxSourceRect.Value, x, y, boxWidth, boxHeight, Color.White * alpha, boxScale);
		if (boldTitleText != null)
		{
			Vector2 bold_text_size = Game1.dialogueFont.MeasureString(boldTitleText);
			IClickableMenu.drawTextureBox(b, boxTexture, boxSourceRect.Value, x, y, width + ((craftingIngredients != null) ? 21 : 0), (int)Game1.dialogueFont.MeasureString(boldTitleText).Y + 32 + (int)((hoveredItem != null && categoryName.Length > 0) ? font.MeasureString("asd").Y : 0f) - 4, Color.White * alpha, 1f, drawShadow: false);
			b.Draw(Game1.menuTexture, new Rectangle(x + 12, y + (int)Game1.dialogueFont.MeasureString(boldTitleText).Y + 32 + (int)((hoveredItem != null && categoryName.Length > 0) ? font.MeasureString("asd").Y : 0f) - 4, width - 4 * ((craftingIngredients != null) ? 1 : 6), 4), new Rectangle(44, 300, 4, 4), Color.White);
			b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 2f), textShadowColor.Value);
			b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2(x + 16, y + 16 + 4) + new Vector2(0f, 2f), textShadowColor.Value);
			b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2(x + 16, y + 16 + 4), textColor.Value);
			if (bold_title_subtext != null)
			{
				Utility.drawTextWithShadow(b, bold_title_subtext, Game1.smallFont, new Vector2((float)(x + 16) + bold_text_size.X, (int)((float)(y + 16 + 4) + bold_text_size.Y / 2f - small_text_size.Y / 2f)), Game1.textColor);
			}
			y += (int)Game1.dialogueFont.MeasureString(boldTitleText).Y;
		}
		if (hoveredItem != null && categoryName.Length > 0)
		{
			y -= 4;
			Utility.drawTextWithShadow(b, categoryName, font, new Vector2(x + 16, y + 16 + 4), hoveredItem.getCategoryColor(), 1f, -1f, 2, 2);
			y += (int)font.MeasureString("T").Y + ((boldTitleText != null) ? 16 : 0) + 4;
			if (hoveredItem is Tool tool && tool.GetTotalForgeLevels() > 0)
			{
				string forged_string2 = Game1.content.LoadString("Strings\\UI:Item_Tooltip_Forged");
				Utility.drawTextWithShadow(b, forged_string2, font, new Vector2(x + 16, y + 16 + 4), Color.DarkRed, 1f, -1f, 2, 2);
				int forges = tool.GetTotalForgeLevels();
				if (forges < tool.GetMaxForges() && !tool.hasEnchantmentOfType<DiamondEnchantment>())
				{
					Utility.drawTextWithShadow(b, " (" + forges + "/" + tool.GetMaxForges() + ")", font, new Vector2((float)(x + 16) + font.MeasureString(forged_string2).X, y + 16 + 4), Color.DimGray, 1f, -1f, 2, 2);
				}
				y += (int)font.MeasureString("T").Y;
			}
			if (hoveredItem is MeleeWeapon weapon && weapon.GetEnchantmentLevel<GalaxySoulEnchantment>() > 0)
			{
				GalaxySoulEnchantment enchantment = weapon.GetEnchantmentOfType<GalaxySoulEnchantment>();
				string forged_string = Game1.content.LoadString("Strings\\UI:Item_Tooltip_GalaxyForged");
				Utility.drawTextWithShadow(b, forged_string, font, new Vector2(x + 16, y + 16 + 4), Color.DarkRed, 1f, -1f, 2, 2);
				int level = enchantment.GetLevel();
				if (level < enchantment.GetMaximumLevel())
				{
					Utility.drawTextWithShadow(b, " (" + level + "/" + enchantment.GetMaximumLevel() + ")", font, new Vector2((float)(x + 16) + font.MeasureString(forged_string).X, y + 16 + 4), Color.DimGray, 1f, -1f, 2, 2);
				}
				y += (int)font.MeasureString("T").Y;
			}
		}
		else
		{
			y += ((boldTitleText != null) ? 16 : 0);
		}
		if (hoveredItem != null && craftingIngredients == null)
		{
			hoveredItem.drawTooltip(b, ref x, ref y, font, alpha, text);
		}
		else if (text != null && text.Length != 0 && (text.Length != 1 || text[0] != ' ') && (craftingIngredients == null || hoveredItem == null || !hoveredItem.getDescription().Equals(text.ToString())))
		{
			if (text.ToString().Contains("[line]"))
			{
				string[] textSplit = text.ToString().Split("[line]");
				b.DrawString(font, textSplit[0], new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 2f), textShadowColor.Value * alpha);
				b.DrawString(font, textSplit[0], new Vector2(x + 16, y + 16 + 4) + new Vector2(0f, 2f), textShadowColor.Value * alpha);
				b.DrawString(font, textSplit[0], new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 0f), textShadowColor.Value * alpha);
				b.DrawString(font, textSplit[0], new Vector2(x + 16, y + 16 + 4), textColor.Value * 0.9f * alpha);
				y += (int)font.MeasureString(textSplit[0]).Y - 16;
				Utility.drawLineWithScreenCoordinates(x + 16 - 4, y + 16 + 4, x + 16 + width - 28, y + 16 + 4, b, textShadowColor.Value);
				Utility.drawLineWithScreenCoordinates(x + 16 - 4, y + 16 + 5, x + 16 + width - 28, y + 16 + 5, b, textShadowColor.Value);
				if (textSplit.Length > 1)
				{
					y -= 16;
					b.DrawString(font, textSplit[1], new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 2f), textShadowColor.Value * alpha);
					b.DrawString(font, textSplit[1], new Vector2(x + 16, y + 16 + 4) + new Vector2(0f, 2f), textShadowColor.Value * alpha);
					b.DrawString(font, textSplit[1], new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 0f), textShadowColor.Value * alpha);
					b.DrawString(font, textSplit[1], new Vector2(x + 16, y + 16 + 4), textColor.Value * 0.9f * alpha);
					y += (int)font.MeasureString(textSplit[1]).Y;
				}
				y += 4;
			}
			else
			{
				b.DrawString(font, text, new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 2f), textShadowColor.Value * alpha);
				b.DrawString(font, text, new Vector2(x + 16, y + 16 + 4) + new Vector2(0f, 2f), textShadowColor.Value * alpha);
				b.DrawString(font, text, new Vector2(x + 16, y + 16 + 4) + new Vector2(2f, 0f), textShadowColor.Value * alpha);
				b.DrawString(font, text, new Vector2(x + 16, y + 16 + 4), textColor.Value * 0.9f * alpha);
				y += (int)font.MeasureString(text).Y + 4;
			}
		}
		if (craftingIngredients != null)
		{
			craftingIngredients.drawRecipeDescription(b, new Vector2(x + 16, y - 8), width, additional_craft_materials);
			y += craftingIngredients.getDescriptionHeight(width - 8);
		}
		if (healAmountToDisplay != -1)
		{
			int stamina_recovery = (hoveredItem as Object).staminaRecoveredOnConsumption();
			if (stamina_recovery >= 0)
			{
				int health_recovery = (hoveredItem as Object).healthRecoveredOnConsumption();
				Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16), new Rectangle((stamina_recovery < 0) ? 140 : 0, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
				Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Energy", ((stamina_recovery > 0) ? "+" : "") + stamina_recovery), font, new Vector2(x + 16 + 34 + 4, y + 16), Game1.textColor);
				y += 34;
				if (health_recovery > 0)
				{
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16), new Rectangle(0, 438, 10, 10), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
					Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Health", ((health_recovery > 0) ? "+" : "") + health_recovery), font, new Vector2(x + 16 + 34 + 4, y + 16), Game1.textColor);
					y += 34;
				}
			}
			else if (stamina_recovery != -300)
			{
				Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16), new Rectangle(140, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
				Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Energy", stamina_recovery.ToString() ?? ""), font, new Vector2(x + 16 + 34 + 4, y + 16), Game1.textColor);
				y += 34;
			}
		}
		if (buffIconsToDisplay != null)
		{
			y += 16;
			b.Draw(Game1.staminaRect, new Rectangle(x + 12, y + 6, width - ((craftingIngredients != null) ? 4 : 24), 2), new Color(207, 147, 103) * 0.8f);
			for (int i = 0; i < buffIconsToDisplay.Length; i++)
			{
				if (buffIconsToDisplay[i].Equals("0") || !(buffIconsToDisplay[i] != ""))
				{
					continue;
				}
				if (i == 12)
				{
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16), new Rectangle(410, 501, 9, 9), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
					Utility.drawTextWithShadow(b, buffIconsToDisplay[i], font, new Vector2(x + 16 + 34 + 4, y + 16), Game1.textColor);
				}
				else
				{
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16), new Rectangle(10 + i * 10, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
					string buffName = ((Convert.ToDouble(buffIconsToDisplay[i]) > 0.0) ? "+" : "") + buffIconsToDisplay[i] + " ";
					if (i <= 11)
					{
						buffName = Game1.content.LoadString("Strings\\UI:ItemHover_Buff" + i, buffName);
					}
					Utility.drawTextWithShadow(b, buffName, font, new Vector2(x + 16 + 34 + 4, y + 16), Game1.textColor);
				}
				y += 39;
			}
			y -= 8;
		}
		if (hoveredItem != null && hoveredItem.attachmentSlots() > 0)
		{
			hoveredItem.drawAttachments(b, x + 16, y + 16);
			if (moneyAmountToDisplayAtBottom > -1)
			{
				y += 68 * hoveredItem.attachmentSlots();
			}
		}
		if (moneyAmountToDisplayAtBottom > -1)
		{
			b.Draw(Game1.staminaRect, new Rectangle(x + 12, y + 22 - ((healAmountToDisplay <= 0) ? 6 : 0), width - ((craftingIngredients != null) ? 4 : 24), 2), new Color(207, 147, 103) * 0.5f);
			string moneyStr = moneyAmountToDisplayAtBottom.ToString();
			int extraY = 0;
			if ((buffIconsToDisplay != null && buffIconsToDisplay.Length > 1) || healAmountToDisplay > 0 || craftingIngredients != null)
			{
				extraY = 8;
			}
			b.DrawString(font, moneyStr, new Vector2(x + 16, y + 16 + 4 + extraY) + new Vector2(2f, 2f), textShadowColor.Value);
			b.DrawString(font, moneyStr, new Vector2(x + 16, y + 16 + 4 + extraY) + new Vector2(0f, 2f), textShadowColor.Value);
			b.DrawString(font, moneyStr, new Vector2(x + 16, y + 16 + 4 + extraY) + new Vector2(2f, 0f), textShadowColor.Value);
			b.DrawString(font, moneyStr, new Vector2(x + 16, y + 16 + 4 + extraY), textColor.Value);
			switch (currencySymbol)
			{
			case 0:
				b.Draw(Game1.debrisSpriteSheet, new Vector2((float)(x + 16) + font.MeasureString(moneyStr).X + 20f, y + 16 + 20 + extraY), Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16), Color.White, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, 0.95f);
				break;
			case 1:
				b.Draw(Game1.mouseCursors, new Vector2((float)(x + 8) + font.MeasureString(moneyStr).X + 20f, y + 16 - 5 + extraY), new Rectangle(338, 400, 8, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				break;
			case 2:
				b.Draw(Game1.mouseCursors, new Vector2((float)(x + 8) + font.MeasureString(moneyStr).X + 20f, y + 16 - 7 + extraY), new Rectangle(211, 373, 9, 10), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				break;
			case 4:
				b.Draw(Game1.objectSpriteSheet, new Vector2((float)(x + 8) + font.MeasureString(moneyStr).X + 20f, y + 16 - 7 + extraY), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 858, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				break;
			}
			y += 48;
		}
		if (extraItemToShowIndex != null)
		{
			if (moneyAmountToDisplayAtBottom == -1)
			{
				y += 8;
			}
			ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(extraItemToShowIndex);
			string displayName = dataOrErrorItem2.DisplayName;
			Texture2D texture = dataOrErrorItem2.GetTexture();
			Rectangle sourceRect = dataOrErrorItem2.GetSourceRect();
			string requirement = Game1.content.LoadString("Strings\\UI:ItemHover_Requirements", extraItemToShowAmount, displayName);
			float minimum_box_height = Math.Max(font.MeasureString(requirement).Y + 21f, 96f);
			IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y + 4, width + ((craftingIngredients != null) ? 21 : 0), (int)minimum_box_height, Color.White);
			y += 20;
			b.DrawString(font, requirement, new Vector2(x + 16, y + 4) + new Vector2(2f, 2f), textShadowColor.Value);
			b.DrawString(font, requirement, new Vector2(x + 16, y + 4) + new Vector2(0f, 2f), textShadowColor.Value);
			b.DrawString(font, requirement, new Vector2(x + 16, y + 4) + new Vector2(2f, 0f), textShadowColor.Value);
			b.DrawString(Game1.smallFont, requirement, new Vector2(x + 16, y + 4), textColor.Value);
			b.Draw(texture, new Vector2(x + 16 + (int)font.MeasureString(requirement).X + 21, y), sourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		}
		if (craftingIngredients != null && Game1.options.showAdvancedCraftingInformation)
		{
			Utility.drawTextWithShadow(b, craftingIngredients.getCraftCountText(), font, new Vector2(x + 16, y + 16 + 4), Game1.textColor, 1f, -1f, 2, 2);
			y += (int)font.MeasureString("T").Y + 4;
		}
	}
}
