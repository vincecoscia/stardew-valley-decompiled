using Microsoft.Xna.Framework;

namespace StardewValley.Tools;

public class Lantern : Tool
{
	public const float baseRadius = 10f;

	public const int millisecondsPerFuelUnit = 6000;

	public const int maxFuel = 100;

	public int fuelLeft;

	private int fuelTimer;

	public bool on;

	public Lantern()
		: base("Lantern", 0, 74, 74, stackable: false)
	{
		base.InstantUse = true;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Lantern();
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		base.DoFunction(location, x, y, power, who);
		this.on = !this.on;
		base.CurrentParentTileIndex = base.IndexOfMenuItemView;
		if (this.on)
		{
			Game1.currentLightSources.Add(new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 2.5f + (float)this.fuelLeft / 100f * 10f * 0.75f, new Color(0, 131, 255), -85736, LightSource.LightContext.None, 0L));
		}
		else
		{
			Utility.removeLightSource(-85736);
		}
	}

	public override void tickUpdate(GameTime time, Farmer who)
	{
		if (this.on && this.fuelLeft > 0 && Game1.drawLighting)
		{
			this.fuelTimer += time.ElapsedGameTime.Milliseconds;
			if (this.fuelTimer > 6000)
			{
				this.fuelLeft--;
				this.fuelTimer = 0;
			}
			bool wasFound = false;
			foreach (LightSource i in Game1.currentLightSources)
			{
				if ((int)i.identifier == -85736)
				{
					i.position.Value = new Vector2(who.Position.X + 21f, who.Position.Y + 64f);
					wasFound = true;
					break;
				}
			}
			if (!wasFound)
			{
				Game1.currentLightSources.Add(new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 2.5f + (float)this.fuelLeft / 100f * 10f * 0.75f, new Color(0, 131, 255), -85736, LightSource.LightContext.None, 0L));
			}
		}
		if (this.on && this.fuelLeft <= 0)
		{
			Utility.removeLightSource(1);
		}
	}
}
