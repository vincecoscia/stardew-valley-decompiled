using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.GameData.WorldMaps;

namespace StardewValley.WorldMaps;

/// <summary>Manages data related to the world map shown in the game menu.</summary>
public static class WorldMapManager
{
	/// <summary>The <see cref="F:StardewValley.Game1.ticks" /> value when cached data should be reset.</summary>
	private static int NextClearCacheTick;

	/// <summary>The maximum update ticks before any cached data should be refreshed.</summary>
	private static int MaxCacheTicks;

	/// <summary>The cached map regions.</summary>
	private static readonly List<MapRegion> Regions;

	/// <summary>Initialize before the class is first accessed.</summary>
	static WorldMapManager()
	{
		WorldMapManager.MaxCacheTicks = 3600;
		WorldMapManager.Regions = new List<MapRegion>();
		WorldMapManager.ReloadData();
	}

	/// <summary>Load the raw world map data.</summary>
	public static void ReloadData()
	{
		WorldMapManager.Regions.Clear();
		foreach (KeyValuePair<string, WorldMapRegionData> pair in DataLoader.WorldMap(Game1.content))
		{
			WorldMapManager.Regions.Add(new MapRegion(pair.Key, pair.Value));
		}
		WorldMapManager.NextClearCacheTick = Game1.ticks + WorldMapManager.MaxCacheTicks;
	}

	/// <summary>Get all map regions in the underlying data which are currently valid.</summary>
	public static IEnumerable<MapRegion> GetMapRegions()
	{
		WorldMapManager.ReloadDataIfStale();
		return WorldMapManager.Regions;
	}

	/// <summary>Get the map position which contains a given location and tile coordinate, if any.</summary>
	/// <param name="location">The in-game location.</param>
	/// <param name="tile">The tile coordinate within the location.</param>
	public static MapAreaPosition GetPositionData(GameLocation location, Point tile)
	{
		if (location == null)
		{
			return null;
		}
		MapAreaPosition position = WorldMapManager.GetPositionDataWithoutFallback(location, tile);
		if (position != null)
		{
			return position;
		}
		Building building = location.GetContainingBuilding();
		if (building != null)
		{
			Point buildingTile = new Point(building.tileX.Value + building.tilesWide.Value / 2, building.tileY.Value + building.tilesHigh.Value / 2);
			position = WorldMapManager.GetPositionDataWithoutFallback(location, buildingTile);
			if (position != null)
			{
				return position;
			}
		}
		return null;
	}

	/// <summary>Update the world map data if needed.</summary>
	private static void ReloadDataIfStale()
	{
		if (Game1.ticks >= WorldMapManager.NextClearCacheTick)
		{
			WorldMapManager.ReloadData();
		}
	}

	/// <summary>Get the map position which contains a given location and tile coordinate, if any, without checking for parent buildings or locations.</summary>
	/// <param name="location">The in-game location.</param>
	/// <param name="tile">The tile coordinate within the location.</param>
	public static MapAreaPosition GetPositionDataWithoutFallback(GameLocation location, Point tile)
	{
		if (location == null)
		{
			return null;
		}
		foreach (MapRegion mapRegion in WorldMapManager.GetMapRegions())
		{
			MapAreaPosition position = mapRegion.GetPositionData(location, tile);
			if (position != null)
			{
				return position;
			}
		}
		return null;
	}
}
