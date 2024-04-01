using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Monsters;
using xTile.Dimensions;
using xTile.Layers;

namespace StardewValley.Locations;

public class Woods : GameLocation
{
	public const int numBaubles = 25;

	private List<Vector2> baubles;

	private List<WeatherDebris> weatherDebris;

	[XmlElement("hasUnlockedStatue")]
	public readonly NetBool hasUnlockedStatue = new NetBool();

	[XmlElement("addedSlimesToday")]
	private readonly NetBool addedSlimesToday = new NetBool();

	[XmlIgnore]
	private readonly NetEvent0 statueAnimationEvent = new NetEvent0();

	protected Color _ambientLightColor = Color.White;

	private int statueTimer;

	public Woods()
	{
	}

	public Woods(string map, string name)
		: base(map, name)
	{
		base.isOutdoors.Value = true;
		base.ignoreDebrisWeather.Value = true;
		base.ignoreOutdoorLighting.Value = true;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(this.addedSlimesToday, "addedSlimesToday").AddField(this.statueAnimationEvent, "statueAnimationEvent").AddField(this.hasUnlockedStatue, "hasUnlockedStatue");
		this.statueAnimationEvent.onEvent += doStatueAnimation;
	}

	public bool localPlayerHasFoundStardrop()
	{
		return Game1.player.hasOrWillReceiveMail("CF_Statue");
	}

	public void statueAnimation(Farmer who)
	{
		if (!this.hasUnlockedStatue)
		{
			who.reduceActiveItemByOne();
			this.hasUnlockedStatue.Value = true;
			this.statueAnimationEvent.Fire();
		}
	}

	private void doStatueAnimation()
	{
		base.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(8f, 7f) * 64f, Color.White, 9, flipped: false, 50f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(9f, 7f) * 64f, Color.Orange, 9, flipped: false, 70f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(8f, 6f) * 64f, Color.White, 9, flipped: false, 60f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(9f, 6f) * 64f, Color.OrangeRed, 9, flipped: false, 120f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(8f, 5f) * 64f, Color.Red, 9));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(9f, 5f) * 64f, Color.White, 9, flipped: false, 170f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(544f, 464f), Color.Orange, 9, flipped: false, 40f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(608f, 464f), Color.White, 9, flipped: false, 90f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(544f, 400f), Color.OrangeRed, 9, flipped: false, 190f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(608f, 400f), Color.White, 9, flipped: false, 80f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(544f, 336f), Color.Red, 9, flipped: false, 69f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(608f, 336f), Color.OrangeRed, 9, flipped: false, 130f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(480f, 464f), Color.Orange, 9, flipped: false, 40f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(672f, 368f), Color.White, 9, flipped: false, 90f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(480f, 464f), Color.Red, 9, flipped: false, 30f));
		base.temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(672f, 368f), Color.White, 9, flipped: false, 180f));
		base.localSound("secret1");
		this.updateStatueEyes();
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (who.IsLocalPlayer)
		{
			int tileIndexAt = base.getTileIndexAt(tileLocation, "Buildings");
			if ((uint)(tileIndexAt - 1140) <= 1u)
			{
				if (!this.hasUnlockedStatue)
				{
					if (who.ActiveObject?.QualifiedItemId == "(O)417")
					{
						this.statueTimer = 1000;
						who.freezePause = 1000;
						Game1.changeMusicTrack("none");
						base.playSound("newArtifact");
					}
					else
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Woods_Statue").Replace('\n', '^'));
					}
				}
				if ((bool)this.hasUnlockedStatue && !this.localPlayerHasFoundStardrop() && who.freeSpotsInInventory() > 0)
				{
					who.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create("(O)434"));
					Game1.player.mailReceived.Add("CF_Statue");
				}
				return true;
			}
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		for (int i = 0; i < base.characters.Count; i++)
		{
			if (base.characters[i] is Monster)
			{
				base.characters.RemoveAt(i);
				i--;
			}
		}
		this.addedSlimesToday.Value = false;
	}

	public override void cleanupBeforePlayerExit()
	{
		base.cleanupBeforePlayerExit();
		this.baubles?.Clear();
		this.weatherDebris?.Clear();
	}

	protected override void resetSharedState()
	{
		if (!this.addedSlimesToday)
		{
			this.addedSlimesToday.Value = true;
			Random rand = Utility.CreateRandom(Game1.stats.DaysPlayed, Game1.uniqueIDForThisGame, 12.0);
			for (int tries = 50; tries > 0; tries--)
			{
				Vector2 tile = base.getRandomTile();
				if (rand.NextDouble() < 0.25 && this.CanItemBePlacedHere(tile))
				{
					switch (base.GetSeason())
					{
					case Season.Spring:
						base.characters.Add(new GreenSlime(tile * 64f, 0));
						break;
					case Season.Summer:
						base.characters.Add(new GreenSlime(tile * 64f, 0));
						break;
					case Season.Fall:
						base.characters.Add(new GreenSlime(tile * 64f, rand.Choose(0, 40)));
						break;
					case Season.Winter:
						base.characters.Add(new GreenSlime(tile * 64f, 40));
						break;
					}
				}
			}
		}
		base.resetSharedState();
	}

	protected void _updateWoodsLighting()
	{
		if (Game1.currentLocation != this)
		{
			return;
		}
		int fade_start_time = Utility.ConvertTimeToMinutes(Game1.getStartingToGetDarkTime(this));
		int fade_end_time = Utility.ConvertTimeToMinutes(Game1.getModeratelyDarkTime(this));
		int light_fade_start_time = Utility.ConvertTimeToMinutes(Game1.getModeratelyDarkTime(this));
		int light_fade_end_time = Utility.ConvertTimeToMinutes(Game1.getTrulyDarkTime(this));
		float num = (float)Utility.ConvertTimeToMinutes(Game1.timeOfDay) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameMinute;
		float lerp = Utility.Clamp((num - (float)fade_start_time) / (float)(fade_end_time - fade_start_time), 0f, 1f);
		float light_lerp = Utility.Clamp((num - (float)light_fade_start_time) / (float)(light_fade_end_time - light_fade_start_time), 0f, 1f);
		Game1.ambientLight.R = (byte)Utility.Lerp((int)this._ambientLightColor.R, (int)Math.Max(this._ambientLightColor.R, Game1.isRaining ? Game1.ambientLight.R : Game1.outdoorLight.R), lerp);
		Game1.ambientLight.G = (byte)Utility.Lerp((int)this._ambientLightColor.G, (int)Math.Max(this._ambientLightColor.G, Game1.isRaining ? Game1.ambientLight.G : Game1.outdoorLight.G), lerp);
		Game1.ambientLight.B = (byte)Utility.Lerp((int)this._ambientLightColor.B, (int)Math.Max(this._ambientLightColor.B, Game1.isRaining ? Game1.ambientLight.B : Game1.outdoorLight.B), lerp);
		Game1.ambientLight.A = (byte)Utility.Lerp((int)this._ambientLightColor.A, (int)Math.Max(this._ambientLightColor.A, Game1.isRaining ? Game1.ambientLight.A : Game1.outdoorLight.A), lerp);
		Color light_color = Color.Black;
		light_color.A = (byte)Utility.Lerp(255f, 0f, light_lerp);
		foreach (LightSource light in Game1.currentLightSources)
		{
			if (light.lightContext.Value == LightSource.LightContext.MapLight)
			{
				light.color.Value = light_color;
			}
		}
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		this.updateStatueEyes();
	}

	protected override void resetLocalState()
	{
		this._ambientLightColor = new Color(150, 120, 50);
		base.ignoreOutdoorLighting.Value = false;
		Game1.player.mailReceived.Add("beenToWoods");
		base.resetLocalState();
		this._updateWoodsLighting();
		Random r = Utility.CreateDaySaveRandom();
		int numberOfBaubles = 25 + r.Next(0, 75);
		if (base.IsRainingHere())
		{
			return;
		}
		this.baubles = new List<Vector2>();
		for (int i = 0; i < numberOfBaubles; i++)
		{
			this.baubles.Add(new Vector2(Game1.random.Next(0, base.map.DisplayWidth), Game1.random.Next(0, base.map.DisplayHeight)));
		}
		Season season = base.GetSeason();
		if (season != Season.Winter)
		{
			this.weatherDebris = new List<WeatherDebris>();
			int spacing = 192;
			int leafType = 1;
			if (season == Season.Fall)
			{
				leafType = 2;
			}
			for (int j = 0; j < numberOfBaubles; j++)
			{
				this.weatherDebris.Add(new WeatherDebris(new Vector2(j * spacing % Game1.graphics.GraphicsDevice.Viewport.Width + Game1.random.Next(spacing), j * spacing / Game1.graphics.GraphicsDevice.Viewport.Width * spacing % Game1.graphics.GraphicsDevice.Viewport.Height + Game1.random.Next(spacing)), leafType, (float)Game1.random.Next(15) / 500f, (float)Game1.random.Next(-10, 0) / 50f, (float)Game1.random.Next(10) / 50f));
			}
		}
	}

	private void updateStatueEyes()
	{
		Layer frontLayer = base.map.RequireLayer("Front");
		if ((bool)this.hasUnlockedStatue && !this.localPlayerHasFoundStardrop())
		{
			frontLayer.Tiles[8, 6].TileIndex = 1117;
			frontLayer.Tiles[9, 6].TileIndex = 1118;
		}
		else
		{
			frontLayer.Tiles[8, 6].TileIndex = 1115;
			frontLayer.Tiles[9, 6].TileIndex = 1116;
		}
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool skipWasUpdatedFlush = false)
	{
		base.updateEvenIfFarmerIsntHere(time, skipWasUpdatedFlush);
		this.statueAnimationEvent.Poll();
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		this._updateWoodsLighting();
		if (this.statueTimer > 0)
		{
			this.statueTimer -= time.ElapsedGameTime.Milliseconds;
			if (this.statueTimer <= 0)
			{
				this.statueAnimation(Game1.player);
			}
		}
		if (this.baubles != null)
		{
			for (int i = 0; i < this.baubles.Count; i++)
			{
				Vector2 v = default(Vector2);
				v.X = this.baubles[i].X - Math.Max(0.4f, Math.Min(1f, (float)i * 0.01f)) - (float)((double)((float)i * 0.01f) * Math.Sin(Math.PI * 2.0 * (double)time.TotalGameTime.Milliseconds / 8000.0));
				v.Y = this.baubles[i].Y + Math.Max(0.5f, Math.Min(1.2f, (float)i * 0.02f));
				if (v.Y > (float)base.map.DisplayHeight || v.X < 0f)
				{
					v.X = Game1.random.Next(0, base.map.DisplayWidth);
					v.Y = -64f;
				}
				this.baubles[i] = v;
			}
		}
		if (this.weatherDebris == null)
		{
			return;
		}
		foreach (WeatherDebris weatherDebri in this.weatherDebris)
		{
			weatherDebri.update();
		}
		Game1.updateDebrisWeatherForMovement(this.weatherDebris);
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		base.drawAboveAlwaysFrontLayer(b);
		if (this.baubles != null)
		{
			for (int i = 0; i < this.baubles.Count; i++)
			{
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, this.baubles[i]), new Microsoft.Xna.Framework.Rectangle(346 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(i * 25)) % 600.0) / 150 * 5, 1971, 5, 5), Color.White, (float)i * ((float)Math.PI / 8f), Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
		}
		if (this.weatherDebris == null || base.currentEvent != null)
		{
			return;
		}
		foreach (WeatherDebris weatherDebri in this.weatherDebris)
		{
			weatherDebri.draw(b);
		}
	}
}
