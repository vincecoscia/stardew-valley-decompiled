using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StardewValley.Minigames;

public class PlaneFlyBy : IMinigame
{
	public const float robotSpeed = 1f;

	public const int skyLength = 2560;

	public int millisecondsSinceStart;

	public int backgroundPosition = -2560 + (int)((float)Game1.game1.localMultiplayerWindow.Height / Game1.options.zoomLevel);

	public int smokeTimer = 500;

	public Vector2 robotPosition = new Vector2(Game1.game1.localMultiplayerWindow.Width, Game1.game1.localMultiplayerWindow.Height / 2) * 1f / Game1.options.zoomLevel;

	public TemporaryAnimatedSpriteList tempSprites = new TemporaryAnimatedSpriteList();

	public bool overrideFreeMouseMovement()
	{
		return Game1.options.SnappyMenus;
	}

	public bool tick(GameTime time)
	{
		this.millisecondsSinceStart += time.ElapsedGameTime.Milliseconds;
		this.robotPosition.X -= 1f * (float)time.ElapsedGameTime.Milliseconds / 4f;
		this.smokeTimer -= time.ElapsedGameTime.Milliseconds;
		if (this.smokeTimer <= 0)
		{
			this.smokeTimer = 100;
			this.tempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(173, 1828, 15, 20), 1500f, 2, 0, this.robotPosition + new Vector2(68f, -24f), flicker: false, flipped: false)
			{
				motion = new Vector2(0f, 0.1f),
				scale = 4f,
				scaleChange = 0.002f,
				alphaFade = 0.0025f,
				rotation = -(float)Math.PI / 2f
			});
		}
		for (int i = this.tempSprites.Count - 1; i >= 0; i--)
		{
			if (this.tempSprites[i].update(time))
			{
				this.tempSprites.RemoveAt(i);
			}
		}
		if (this.robotPosition.X < -128f && !Game1.globalFade)
		{
			Game1.globalFadeToBlack(afterFade, 0.006f);
		}
		return false;
	}

	public void afterFade()
	{
		Game1.currentMinigame = null;
		Game1.globalFadeToClear();
		if (Game1.currentLocation.currentEvent != null)
		{
			Game1.currentLocation.currentEvent.CurrentCommand++;
			Game1.currentLocation.temporarySprites.Clear();
		}
	}

	public void receiveLeftClick(int x, int y, bool playSound = true)
	{
	}

	public void leftClickHeld(int x, int y)
	{
	}

	public void receiveRightClick(int x, int y, bool playSound = true)
	{
	}

	public void releaseLeftClick(int x, int y)
	{
	}

	public void releaseRightClick(int x, int y)
	{
	}

	public void receiveKeyPress(Keys k)
	{
		if (k == Keys.Escape)
		{
			this.robotPosition.X = -1000f;
			this.tempSprites.Clear();
		}
	}

	public void receiveKeyRelease(Keys k)
	{
	}

	public void draw(SpriteBatch b)
	{
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		b.Draw(Game1.mouseCursors, new Rectangle(0, this.backgroundPosition, Game1.graphics.GraphicsDevice.Viewport.Width, 2560), new Rectangle(264, 1858, 1, 84), Color.White);
		b.Draw(Game1.mouseCursors, new Vector2(0f, this.backgroundPosition), new Rectangle(0, 1454, 639, 188), Color.White * 0.5f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		b.Draw(Game1.mouseCursors, new Vector2(0f, this.backgroundPosition - 752), new Rectangle(0, 1454, 639, 188), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		b.Draw(Game1.mouseCursors, new Vector2(0f, this.backgroundPosition - 1504), new Rectangle(0, 1454, 639, 188), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		b.Draw(Game1.mouseCursors, new Vector2(0f, this.backgroundPosition - 2256), new Rectangle(0, 1454, 639, 188), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		b.Draw(Game1.mouseCursors, this.robotPosition, new Rectangle(222 + this.millisecondsSinceStart / 50 % 2 * 20, 1890, 20, 9), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		foreach (TemporaryAnimatedSprite tempSprite in this.tempSprites)
		{
			tempSprite.draw(b, localPosition: true);
		}
		b.End();
	}

	public void changeScreenSize()
	{
		float pixel_zoom_adjustment = 1f / Game1.options.zoomLevel;
		this.backgroundPosition = 2560 - (int)((float)Game1.game1.localMultiplayerWindow.Height * pixel_zoom_adjustment);
		this.robotPosition = new Vector2(Game1.game1.localMultiplayerWindow.Width / 2, Game1.game1.localMultiplayerWindow.Height) * pixel_zoom_adjustment;
	}

	public void unload()
	{
	}

	public void receiveEventPoke(int data)
	{
		throw new NotImplementedException();
	}

	public string minigameId()
	{
		return null;
	}

	public bool doMainGameUpdates()
	{
		return false;
	}

	public bool forceQuit()
	{
		return false;
	}
}
