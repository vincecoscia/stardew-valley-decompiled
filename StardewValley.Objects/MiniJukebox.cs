using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.Menus;

namespace StardewValley.Objects;

public class MiniJukebox : Object
{
	private bool showNote;

	/// <inheritdoc />
	public override string TypeDefinitionId => "(BC)";

	public MiniJukebox()
	{
	}

	public MiniJukebox(Vector2 position)
		: base(position, "209")
	{
		this.Name = "Mini-Jukebox";
		base.type.Value = "Crafting";
		base.bigCraftable.Value = true;
		base.canBeSetDown.Value = true;
	}

	/// <inheritdoc />
	public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		GameLocation location = this.Location;
		if (!location.IsFarm && !location.IsGreenhouse && !(location is Cellar) && !(location is IslandWest))
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Mini_JukeBox_NotFarmPlay"));
		}
		else if (location.IsOutdoors && location.IsRainingHere())
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Mini_JukeBox_OutdoorRainy"));
		}
		else
		{
			List<string> jukeboxTracks = Utility.GetJukeboxTracks(Game1.player, Game1.player.currentLocation);
			jukeboxTracks.Insert(0, "turn_off");
			jukeboxTracks.Add("random");
			Game1.activeClickableMenu = new ChooseFromListMenu(jukeboxTracks, OnSongChosen, isJukebox: true, location.miniJukeboxTrack.Value);
		}
		return true;
	}

	public void RegisterToLocation()
	{
		this.Location?.OnMiniJukeboxAdded();
	}

	public override void performRemoveAction()
	{
		this.Location?.OnMiniJukeboxRemoved();
		base.performRemoveAction();
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		GameLocation environment = this.Location;
		if (environment != null && environment.IsMiniJukeboxPlaying())
		{
			base.showNextIndex.Value = true;
			if (this.showNote)
			{
				this.showNote = false;
				for (int i = 0; i < 4; i++)
				{
					environment.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(516, 1916, 7, 10), 9999f, 1, 1, base.tileLocation.Value * 64f + new Vector2(Game1.random.Next(48), -80f), flicker: false, flipped: false, (base.tileLocation.Value.Y + 1f) * 64f / 10000f, 0.01f, Color.White, 4f, 0f, 0f, 0f)
					{
						xPeriodic = true,
						xPeriodicLoopTime = 1200f,
						xPeriodicRange = 8f,
						motion = new Vector2((float)Game1.random.Next(-10, 10) / 100f, -1f),
						delayBeforeAnimationStart = 1200 + 300 * i
					});
				}
			}
		}
		else
		{
			base.showNextIndex.Value = false;
		}
		base.updateWhenCurrentLocation(time);
	}

	public void OnSongChosen(string selection)
	{
		GameLocation location = this.Location;
		if (location == null)
		{
			return;
		}
		if (selection == "turn_off")
		{
			location.miniJukeboxTrack.Value = "";
			return;
		}
		if (selection != location.miniJukeboxTrack)
		{
			this.showNote = true;
			base.shakeTimer = 1000;
		}
		location.miniJukeboxTrack.Value = selection;
		if (selection == "random")
		{
			location.SelectRandomMiniJukeboxTrack();
		}
	}
}
