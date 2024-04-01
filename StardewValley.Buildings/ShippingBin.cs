using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace StardewValley.Buildings;

public class ShippingBin : Building
{
	private TemporaryAnimatedSprite shippingBinLid;

	private Farm farm;

	private Rectangle shippingBinLidOpenArea;

	protected Vector2 _lidGenerationPosition;

	public ShippingBin(Vector2 tileLocation)
		: base("Shipping Bin", tileLocation)
	{
		this.initLid();
	}

	public ShippingBin()
		: this(Vector2.Zero)
	{
	}

	public void initLid()
	{
		this.shippingBinLid = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(134, 226, 30, 25), new Vector2((int)base.tileX, (int)base.tileY - 1) * 64f + new Vector2(1f, -7f) * 4f, flipped: false, 0f, Color.White)
		{
			holdLastFrame = true,
			destroyable = false,
			interval = 20f,
			animationLength = 13,
			paused = true,
			scale = 4f,
			layerDepth = (float)(((int)base.tileY + 1) * 64) / 10000f + 0.0001f,
			pingPong = true,
			pingPongMotion = 0
		};
		this.shippingBinLidOpenArea = new Rectangle(((int)base.tileX - 1) * 64, ((int)base.tileY - 1) * 64, 256, 192);
		this._lidGenerationPosition = new Vector2((int)base.tileX, (int)base.tileY);
	}

	public override Rectangle? getSourceRectForMenu()
	{
		return new Rectangle(0, 0, base.texture.Value.Bounds.Width, base.texture.Value.Bounds.Height);
	}

	public override void resetLocalState()
	{
		base.resetLocalState();
		if (this.shippingBinLid != null)
		{
			_ = this.shippingBinLidOpenArea;
		}
		else
		{
			this.initLid();
		}
	}

	public override void Update(GameTime time)
	{
		base.Update(time);
		if (this.farm == null)
		{
			this.farm = Game1.getFarm();
		}
		if (this.shippingBinLid != null)
		{
			_ = this.shippingBinLidOpenArea;
			if (this._lidGenerationPosition.X == (float)(int)base.tileX && this._lidGenerationPosition.Y == (float)(int)base.tileY)
			{
				bool opening = false;
				foreach (Farmer farmer in base.GetParentLocation().farmers)
				{
					if (farmer.GetBoundingBox().Intersects(this.shippingBinLidOpenArea))
					{
						this.openShippingBinLid();
						opening = true;
					}
				}
				if (!opening)
				{
					this.closeShippingBinLid();
				}
				this.updateShippingBinLid(time);
				return;
			}
		}
		this.initLid();
	}

	/// <inheritdoc />
	public override void performActionOnBuildingPlacement()
	{
		base.performActionOnBuildingPlacement();
		this.initLid();
	}

	private void openShippingBinLid()
	{
		if (this.shippingBinLid != null)
		{
			if (this.shippingBinLid.pingPongMotion != 1 && base.IsInCurrentLocation())
			{
				Game1.currentLocation.localSound("doorCreak");
			}
			this.shippingBinLid.pingPongMotion = 1;
			this.shippingBinLid.paused = false;
		}
	}

	private void closeShippingBinLid()
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = this.shippingBinLid;
		if (temporaryAnimatedSprite != null && temporaryAnimatedSprite.currentParentTileIndex > 0)
		{
			if (this.shippingBinLid.pingPongMotion != -1 && base.IsInCurrentLocation())
			{
				Game1.currentLocation.localSound("doorCreakReverse");
			}
			this.shippingBinLid.pingPongMotion = -1;
			this.shippingBinLid.paused = false;
		}
	}

	private void updateShippingBinLid(GameTime time)
	{
		if (this.isShippingBinLidOpen(requiredToBeFullyOpen: true) && this.shippingBinLid.pingPongMotion == 1)
		{
			this.shippingBinLid.paused = true;
		}
		else if (this.shippingBinLid.currentParentTileIndex == 0 && this.shippingBinLid.pingPongMotion == -1)
		{
			if (!this.shippingBinLid.paused && base.IsInCurrentLocation())
			{
				Game1.currentLocation.localSound("woodyStep");
			}
			this.shippingBinLid.paused = true;
		}
		this.shippingBinLid.update(time);
	}

	private bool isShippingBinLidOpen(bool requiredToBeFullyOpen = false)
	{
		if (this.shippingBinLid != null && this.shippingBinLid.currentParentTileIndex >= ((!requiredToBeFullyOpen) ? 1 : (this.shippingBinLid.animationLength - 1)))
		{
			return true;
		}
		return false;
	}

	private void shipItem(Item i, Farmer who)
	{
		if (i != null)
		{
			who.removeItemFromInventory(i);
			this.farm?.getShippingBin(who).Add(i);
			if (i is Object obj && this.farm != null)
			{
				this.showShipment(obj, playThrowSound: false);
			}
			this.farm.lastItemShipped = i;
			if (Game1.player.ActiveObject == null)
			{
				Game1.player.showNotCarrying();
				Game1.player.Halt();
			}
		}
	}

	public override bool CanLeftClick(int x, int y)
	{
		Rectangle hit_rect = new Rectangle((int)base.tileX * 64, (int)base.tileY * 64, (int)base.tilesWide * 64, (int)base.tilesHigh * 64);
		hit_rect.Y -= 64;
		hit_rect.Height += 64;
		return hit_rect.Contains(x, y);
	}

	public override bool leftClicked()
	{
		if (this.farm != null && Game1.player.ActiveObject != null && Game1.player.ActiveObject.canBeShipped() && Vector2.Distance(Game1.player.Tile, new Vector2((float)(int)base.tileX + 0.5f, (int)base.tileY)) <= 2f)
		{
			this.farm.getShippingBin(Game1.player).Add(Game1.player.ActiveObject);
			this.farm.lastItemShipped = Game1.player.ActiveObject;
			Game1.player.showNotCarrying();
			this.showShipment(Game1.player.ActiveObject);
			Game1.player.ActiveObject = null;
			return true;
		}
		return base.leftClicked();
	}

	public void showShipment(Object o, bool playThrowSound = true)
	{
		if (this.farm != null)
		{
			GameLocation parentLocation = base.GetParentLocation();
			if (playThrowSound)
			{
				parentLocation.localSound("backpackIN");
			}
			DelayedAction.playSoundAfterDelay("Ship", playThrowSound ? 250 : 0);
			int temp = Game1.random.Next();
			parentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(524, 218, 34, 22), new Vector2((int)base.tileX, (int)base.tileY - 1) * 64f + new Vector2(-1f, 5f) * 4f, flipped: false, 0f, Color.White)
			{
				interval = 100f,
				totalNumberOfLoops = 1,
				animationLength = 3,
				pingPong = true,
				alpha = base.alpha,
				scale = 4f,
				layerDepth = (float)(((int)base.tileY + 1) * 64) / 10000f + 0.0002f,
				id = temp,
				extraInfoForEndBehavior = temp,
				endFunction = parentLocation.removeTemporarySpritesWithID
			});
			parentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(524, 230, 34, 10), new Vector2((int)base.tileX, (int)base.tileY - 1) * 64f + new Vector2(-1f, 17f) * 4f, flipped: false, 0f, Color.White)
			{
				interval = 100f,
				totalNumberOfLoops = 1,
				animationLength = 3,
				pingPong = true,
				alpha = base.alpha,
				scale = 4f,
				layerDepth = (float)(((int)base.tileY + 1) * 64) / 10000f + 0.0003f,
				id = temp,
				extraInfoForEndBehavior = temp
			});
			ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(o.QualifiedItemId);
			parentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(itemData.TextureName, itemData.GetSourceRect(), new Vector2((int)base.tileX, (int)base.tileY - 1) * 64f + new Vector2(7 + Game1.random.Next(6), 2f) * 4f, flipped: false, 0f, Color.White)
			{
				interval = 9999f,
				scale = 4f,
				alphaFade = 0.045f,
				layerDepth = (float)(((int)base.tileY + 1) * 64) / 10000f + 0.000225f,
				motion = new Vector2(0f, 0.3f),
				acceleration = new Vector2(0f, 0.2f),
				scaleChange = -0.05f
			});
		}
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if ((int)base.daysOfConstructionLeft <= 0 && tileLocation.X >= (float)(int)base.tileX && tileLocation.X <= (float)((int)base.tileX + 1) && tileLocation.Y == (float)(int)base.tileY)
		{
			if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
			{
				return false;
			}
			ItemGrabMenu itemGrabMenu = new ItemGrabMenu(null, reverseGrab: true, showReceivingMenu: false, Utility.highlightShippableObjects, shipItem, "", null, snapToBottom: true, canBeExitedWithKey: true, playRightClickSound: false, allowRightClick: true, showOrganizeButton: false, 0, null, -1, this);
			itemGrabMenu.initializeUpperRightCloseButton();
			itemGrabMenu.setBackgroundTransparency(b: false);
			itemGrabMenu.setDestroyItemOnClick(b: true);
			itemGrabMenu.initializeShippingBin();
			Game1.activeClickableMenu = itemGrabMenu;
			if (who.IsLocalPlayer)
			{
				Game1.playSound("shwip");
			}
			if (Game1.player.FacingDirection == 1)
			{
				Game1.player.Halt();
			}
			Game1.player.showCarrying();
			return true;
		}
		return base.doAction(tileLocation, who);
	}

	public override void drawInMenu(SpriteBatch b, int x, int y)
	{
		base.drawInMenu(b, x, y);
		b.Draw(Game1.mouseCursors, new Vector2(x + 4, y - 20), new Rectangle(134, 226, 30, 25), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
	}

	public override void draw(SpriteBatch b)
	{
		if (!base.isMoving)
		{
			base.draw(b);
			if (this.shippingBinLid != null && (int)base.daysOfConstructionLeft <= 0)
			{
				this.shippingBinLid.color = base.color;
				this.shippingBinLid.draw(b, localPosition: false, 0, 0, base.alpha * (((int)base.newConstructionTimer > 0) ? ((1000f - (float)(int)base.newConstructionTimer) / 1000f) : 1f));
			}
		}
	}
}
