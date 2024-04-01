using System;
using StardewValley.Companions;
using StardewValley.TokenizableStrings;

namespace StardewValley.Objects;

public class CompanionTrinketEffect : TrinketEffect
{
	public int variant;

	public CompanionTrinketEffect(Trinket trinket)
		: base(trinket)
	{
	}

	public override void GenerateRandomStats(Trinket trinket)
	{
		Random r = Utility.CreateRandom((int)trinket.generationSeed);
		if (r.NextDouble() < 0.2)
		{
			this.variant = 0;
		}
		else if (r.NextDouble() < 0.8)
		{
			this.variant = r.Next(3);
		}
		else if (r.NextDouble() < 0.8)
		{
			this.variant = r.Next(3) + 3;
		}
		else
		{
			this.variant = r.Next(2) + 6;
		}
		trinket.displayNameOverrideTemplate.Value = TokenStringBuilder.LocalizedText("Strings\\1_6_Strings:frog_variant_" + this.variant);
	}

	public override void Apply(Farmer farmer)
	{
		base._companion = new HungryFrogCompanion(this.variant);
		if (Game1.gameMode == 3)
		{
			farmer.AddCompanion(base._companion);
		}
	}

	public override void Unapply(Farmer farmer)
	{
		farmer.RemoveCompanion(base._companion);
	}
}
