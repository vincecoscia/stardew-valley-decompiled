using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace StardewValley.Locations;

public class DecoratableLocation : GameLocation
{
	/// <summary>Obsolete.</summary>
	public readonly DecorationFacade wallPaper = new DecorationFacade();

	[XmlIgnore]
	public readonly NetStringList wallpaperIDs = new NetStringList();

	public readonly NetStringDictionary<string, NetString> appliedWallpaper = new NetStringDictionary<string, NetString>
	{
		InterpolationWait = false
	};

	[XmlIgnore]
	public readonly Dictionary<string, List<Vector3>> wallpaperTiles = new Dictionary<string, List<Vector3>>();

	/// <summary>Obsolete.</summary>
	public readonly DecorationFacade floor = new DecorationFacade();

	[XmlIgnore]
	public readonly NetStringList floorIDs = new NetStringList();

	public readonly NetStringDictionary<string, NetString> appliedFloor = new NetStringDictionary<string, NetString>
	{
		InterpolationWait = false
	};

	[XmlIgnore]
	public readonly Dictionary<string, List<Vector3>> floorTiles = new Dictionary<string, List<Vector3>>();

	protected Dictionary<string, TileSheet> _wallAndFloorTileSheets = new Dictionary<string, TileSheet>();

	protected Map _wallAndFloorTileSheetMap;

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(this.appliedWallpaper, "appliedWallpaper").AddField(this.appliedFloor, "appliedFloor").AddField(this.floorIDs, "floorIDs")
			.AddField(this.wallpaperIDs, "wallpaperIDs");
		this.appliedWallpaper.OnValueAdded += delegate(string key, string value)
		{
			this.UpdateWallpaper(key);
		};
		this.appliedWallpaper.OnConflictResolve += delegate(string key, NetString rejected, NetString accepted)
		{
			this.UpdateWallpaper(key);
		};
		this.appliedWallpaper.OnValueTargetUpdated += delegate(string key, string old_value, string new_value)
		{
			if (this.appliedWallpaper.FieldDict.TryGetValue(key, out var value3))
			{
				value3.CancelInterpolation();
			}
			this.UpdateWallpaper(key);
		};
		this.appliedFloor.OnValueAdded += delegate(string key, string value)
		{
			this.UpdateFloor(key);
		};
		this.appliedFloor.OnConflictResolve += delegate(string key, NetString rejected, NetString accepted)
		{
			this.UpdateFloor(key);
		};
		this.appliedFloor.OnValueTargetUpdated += delegate(string key, string old_value, string new_value)
		{
			if (this.appliedFloor.FieldDict.TryGetValue(key, out var value2))
			{
				value2.CancelInterpolation();
			}
			this.UpdateFloor(key);
		};
	}

	public DecoratableLocation()
	{
	}

	public DecoratableLocation(string mapPath, string name)
		: base(mapPath, name)
	{
	}

	public override void updateLayout()
	{
		base.updateLayout();
		if (Game1.IsMasterGame)
		{
			this.setWallpapers();
			this.setFloors();
		}
	}

	public virtual void ReadWallpaperAndFloorTileData()
	{
		this.updateMap();
		this.wallpaperTiles.Clear();
		this.floorTiles.Clear();
		this.wallpaperIDs.Clear();
		this.floorIDs.Clear();
		Dictionary<string, string> initial_values = new Dictionary<string, string>();
		if (base.TryGetMapProperty("WallIDs", out var wallProperty))
		{
			string[] array = wallProperty.Split(',');
			for (int k = 0; k < array.Length; k++)
			{
				string[] data_split2 = ArgUtility.SplitBySpace(array[k]);
				if (data_split2.Length >= 1)
				{
					this.wallpaperIDs.Add(data_split2[0]);
				}
				if (data_split2.Length >= 2)
				{
					initial_values[data_split2[0]] = data_split2[1];
				}
			}
		}
		if (this.wallpaperIDs.Count == 0)
		{
			List<Microsoft.Xna.Framework.Rectangle> walls = this.getWalls();
			for (int j = 0; j < walls.Count; j++)
			{
				string id2 = "Wall_" + j;
				this.wallpaperIDs.Add(id2);
				Microsoft.Xna.Framework.Rectangle rect = walls[j];
				if (!this.wallpaperTiles.ContainsKey(j.ToString()))
				{
					this.wallpaperTiles[id2] = new List<Vector3>();
				}
				foreach (Point tile2 in rect.GetPoints())
				{
					this.wallpaperTiles[id2].Add(new Vector3(tile2.X, tile2.Y, tile2.Y - rect.Top));
				}
			}
		}
		else
		{
			for (int x2 = 0; x2 < base.map.Layers[0].LayerWidth; x2++)
			{
				for (int y2 = 0; y2 < base.map.Layers[0].LayerHeight; y2++)
				{
					string tile_property2 = this.doesTileHaveProperty(x2, y2, "WallID", "Back");
					base.getTileIndexAt(new Point(x2, y2), "Back");
					if (tile_property2 == null)
					{
						continue;
					}
					if (!this.wallpaperIDs.Contains(tile_property2))
					{
						this.wallpaperIDs.Add(tile_property2);
					}
					if (this.appliedWallpaper.TryAdd(tile_property2, "0") && initial_values.TryGetValue(tile_property2, out var initial_value2))
					{
						if (this.appliedWallpaper.TryGetValue(initial_value2, out var newValue2))
						{
							this.appliedWallpaper[tile_property2] = newValue2;
						}
						else if (this.GetWallpaperSource(initial_value2).Value >= 0)
						{
							this.appliedWallpaper[tile_property2] = initial_value2;
						}
					}
					if (!this.wallpaperTiles.TryGetValue(tile_property2, out var areas2))
					{
						areas2 = (this.wallpaperTiles[tile_property2] = new List<Vector3>());
					}
					areas2.Add(new Vector3(x2, y2, 0f));
					string tilesheet_id = base.getTileSheetIDAt(x2, y2, "Back");
					base.map.GetTileSheet(tilesheet_id);
					if (this.IsFloorableOrWallpaperableTile(x2, y2 + 1, "Back"))
					{
						areas2.Add(new Vector3(x2, y2 + 1, 1f));
					}
					if (this.IsFloorableOrWallpaperableTile(x2, y2 + 2, "Buildings"))
					{
						areas2.Add(new Vector3(x2, y2 + 2, 2f));
					}
					else if (this.IsFloorableOrWallpaperableTile(x2, y2 + 2, "Back") && !this.IsFloorableTile(x2, y2 + 2, "Back"))
					{
						areas2.Add(new Vector3(x2, y2 + 2, 2f));
					}
				}
			}
		}
		initial_values.Clear();
		if (base.TryGetMapProperty("FloorIDs", out var floorProperty))
		{
			string[] array = floorProperty.Split(',');
			for (int k = 0; k < array.Length; k++)
			{
				string[] data_split = ArgUtility.SplitBySpace(array[k]);
				if (data_split.Length >= 1)
				{
					this.floorIDs.Add(data_split[0]);
				}
				if (data_split.Length >= 2)
				{
					initial_values[data_split[0]] = data_split[1];
				}
			}
		}
		if (this.floorIDs.Count == 0)
		{
			List<Microsoft.Xna.Framework.Rectangle> floors = this.getFloors();
			for (int i = 0; i < floors.Count; i++)
			{
				string id = "Floor_" + i;
				this.floorIDs.Add(id);
				Microsoft.Xna.Framework.Rectangle rect2 = floors[i];
				if (!this.floorTiles.ContainsKey(i.ToString()))
				{
					this.floorTiles[id] = new List<Vector3>();
				}
				foreach (Point tile in rect2.GetPoints())
				{
					this.floorTiles[id].Add(new Vector3(tile.X, tile.Y, 0f));
				}
			}
		}
		else
		{
			for (int x = 0; x < base.map.Layers[0].LayerWidth; x++)
			{
				for (int y = 0; y < base.map.Layers[0].LayerHeight; y++)
				{
					string tile_property = this.doesTileHaveProperty(x, y, "FloorID", "Back");
					if (tile_property == null)
					{
						continue;
					}
					if (!this.floorIDs.Contains(tile_property))
					{
						this.floorIDs.Add(tile_property);
					}
					if (this.appliedFloor.TryAdd(tile_property, "0") && initial_values.TryGetValue(tile_property, out var initial_value))
					{
						if (this.appliedFloor.TryGetValue(initial_value, out var newValue))
						{
							this.appliedFloor[tile_property] = newValue;
						}
						else if (this.GetFloorSource(initial_value).Value >= 0)
						{
							this.appliedFloor[tile_property] = initial_value;
						}
					}
					if (!this.floorTiles.TryGetValue(tile_property, out var areas))
					{
						areas = (this.floorTiles[tile_property] = new List<Vector3>());
					}
					areas.Add(new Vector3(x, y, 0f));
				}
			}
		}
		this.setFloors();
		this.setWallpapers();
	}

	public virtual TileSheet GetWallAndFloorTilesheet(string id)
	{
		if (base.map != this._wallAndFloorTileSheetMap)
		{
			this._wallAndFloorTileSheets.Clear();
			this._wallAndFloorTileSheetMap = base.map;
		}
		if (this._wallAndFloorTileSheets.TryGetValue(id, out var wallAndFloorTilesheet))
		{
			return wallAndFloorTilesheet;
		}
		try
		{
			foreach (ModWallpaperOrFlooring entry in DataLoader.AdditionalWallpaperFlooring(Game1.content))
			{
				if (!(entry.Id != id))
				{
					Texture2D texture = Game1.content.Load<Texture2D>(entry.Texture);
					if (texture.Width != 256)
					{
						Game1.log.Warn($"The tilesheet for wallpaper/floor '{entry.Id}' is {texture.Width} pixels wide, but it must be exactly {256} pixels wide.");
					}
					TileSheet tilesheet = new TileSheet("x_WallsAndFloors_" + id, base.map, entry.Texture, new Size(texture.Width / 16, texture.Height / 16), new Size(16, 16));
					base.map.AddTileSheet(tilesheet);
					base.map.LoadTileSheets(Game1.mapDisplayDevice);
					this._wallAndFloorTileSheets[id] = tilesheet;
					return tilesheet;
				}
			}
			Game1.log.Error("The tilesheet for wallpaper/floor '" + id + "' could not be loaded: no such ID found in Data/AdditionalWallpaperFlooring.");
			this._wallAndFloorTileSheets[id] = null;
			return null;
		}
		catch (Exception ex)
		{
			Game1.log.Error("The tilesheet for wallpaper/floor '" + id + "' could not be loaded.", ex);
			this._wallAndFloorTileSheets[id] = null;
			return null;
		}
	}

	public virtual KeyValuePair<int, int> GetFloorSource(string pattern_id)
	{
		int pattern_index;
		if (pattern_id.Contains(':'))
		{
			string[] pattern_split = pattern_id.Split(':');
			TileSheet tilesheet2 = this.GetWallAndFloorTilesheet(pattern_split[0]);
			if (int.TryParse(pattern_split[1], out pattern_index) && tilesheet2 != null)
			{
				return new KeyValuePair<int, int>(base.map.TileSheets.IndexOf(tilesheet2), pattern_index);
			}
		}
		if (int.TryParse(pattern_id, out pattern_index))
		{
			TileSheet tilesheet = base.map.GetTileSheet("walls_and_floors");
			return new KeyValuePair<int, int>(base.map.TileSheets.IndexOf(tilesheet), pattern_index);
		}
		return new KeyValuePair<int, int>(-1, -1);
	}

	public virtual KeyValuePair<int, int> GetWallpaperSource(string pattern_id)
	{
		int pattern_index;
		if (pattern_id.Contains(':'))
		{
			string[] pattern_split = pattern_id.Split(':');
			TileSheet tilesheet2 = this.GetWallAndFloorTilesheet(pattern_split[0]);
			if (int.TryParse(pattern_split[1], out pattern_index) && tilesheet2 != null)
			{
				return new KeyValuePair<int, int>(base.map.TileSheets.IndexOf(tilesheet2), pattern_index);
			}
		}
		if (int.TryParse(pattern_id, out pattern_index))
		{
			TileSheet tilesheet = base.map.GetTileSheet("walls_and_floors");
			return new KeyValuePair<int, int>(base.map.TileSheets.IndexOf(tilesheet), pattern_index);
		}
		return new KeyValuePair<int, int>(-1, -1);
	}

	public virtual void UpdateFloor(string floor_id)
	{
		this.updateMap();
		if (!this.appliedFloor.TryGetValue(floor_id, out var pattern_id) || !this.floorTiles.TryGetValue(floor_id, out var tiles))
		{
			return;
		}
		foreach (Vector3 item in tiles)
		{
			int x = (int)item.X;
			int y = (int)item.Y;
			KeyValuePair<int, int> source = this.GetFloorSource(pattern_id);
			if (source.Value < 0)
			{
				continue;
			}
			int tilesheet_index = source.Key;
			int floor_pattern_id = source.Value;
			int tiles_wide = base.map.TileSheets[tilesheet_index].SheetWidth;
			string id = base.map.TileSheets[tilesheet_index].Id;
			string layer = "Back";
			floor_pattern_id = floor_pattern_id * 2 + floor_pattern_id / (tiles_wide / 2) * tiles_wide;
			if (id == "walls_and_floors")
			{
				floor_pattern_id += this.GetFirstFlooringTile();
			}
			if (!this.IsFloorableOrWallpaperableTile(x, y, layer))
			{
				continue;
			}
			Tile old_tile = base.map.RequireLayer(layer).Tiles[x, y];
			base.setMapTile(x, y, this.GetFlooringIndex(floor_pattern_id, x, y), layer, null, tilesheet_index);
			Tile new_tile = base.map.RequireLayer(layer).Tiles[x, y];
			if (old_tile == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, PropertyValue> property in old_tile.Properties)
			{
				new_tile.Properties[property.Key] = property.Value;
			}
		}
	}

	public virtual void UpdateWallpaper(string wallpaper_id)
	{
		this.updateMap();
		if (!this.appliedWallpaper.ContainsKey(wallpaper_id) || !this.wallpaperTiles.ContainsKey(wallpaper_id))
		{
			return;
		}
		string pattern_id = this.appliedWallpaper[wallpaper_id];
		foreach (Vector3 item in this.wallpaperTiles[wallpaper_id])
		{
			int x = (int)item.X;
			int y = (int)item.Y;
			int type = (int)item.Z;
			KeyValuePair<int, int> source = this.GetWallpaperSource(pattern_id);
			if (source.Value < 0)
			{
				continue;
			}
			int tile_sheet_index = source.Key;
			int tile_id = source.Value;
			int tiles_wide = base.map.TileSheets[tile_sheet_index].SheetWidth;
			string layer = "Back";
			if (type == 2)
			{
				layer = "Buildings";
				if (!this.IsFloorableOrWallpaperableTile(x, y, "Buildings"))
				{
					layer = "Back";
				}
			}
			if (!this.IsFloorableOrWallpaperableTile(x, y, layer))
			{
				continue;
			}
			Tile old_tile = base.map.RequireLayer(layer).Tiles[x, y];
			base.setMapTile(x, y, tile_id / tiles_wide * tiles_wide * 3 + tile_id % tiles_wide + type * tiles_wide, layer, null, tile_sheet_index);
			Tile new_tile = base.map.RequireLayer(layer).Tiles[x, y];
			if (old_tile == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, PropertyValue> property in old_tile.Properties)
			{
				new_tile.Properties[property.Key] = property.Value;
			}
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		if (!base.wasUpdated)
		{
			base.UpdateWhenCurrentLocation(time);
		}
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		if (!(this is FarmHouse))
		{
			this.ReadWallpaperAndFloorTileData();
			this.setWallpapers();
			this.setFloors();
		}
		if (base.getTileIndexAt(Game1.player.TilePoint.X, Game1.player.TilePoint.Y, "Buildings") != -1)
		{
			Game1.player.position.Y += 64f;
		}
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (Game1.player.mailReceived.Add("button_tut_1"))
		{
			Game1.onScreenMenus.Add(new ButtonTutorialMenu(0));
		}
	}

	public override bool CanFreePlaceFurniture()
	{
		return true;
	}

	public virtual bool isTileOnWall(int x, int y)
	{
		foreach (string id in this.wallpaperTiles.Keys)
		{
			foreach (Vector3 tile_data in this.wallpaperTiles[id])
			{
				if ((int)tile_data.X == x && (int)tile_data.Y == y)
				{
					return true;
				}
			}
		}
		return false;
	}

	public int GetWallTopY(int x, int y)
	{
		foreach (string id in this.wallpaperTiles.Keys)
		{
			foreach (Vector3 tile_data in this.wallpaperTiles[id])
			{
				if ((int)tile_data.X == x && (int)tile_data.Y == y)
				{
					return y - (int)tile_data.Z;
				}
			}
		}
		return -1;
	}

	public virtual void setFloors()
	{
		foreach (KeyValuePair<string, string> pair in this.appliedFloor.Pairs)
		{
			this.UpdateFloor(pair.Key);
		}
	}

	public virtual void setWallpapers()
	{
		foreach (KeyValuePair<string, string> pair in this.appliedWallpaper.Pairs)
		{
			this.UpdateWallpaper(pair.Key);
		}
	}

	public void SetFloor(string which, string which_room)
	{
		if (which_room == null)
		{
			foreach (string key in this.floorIDs)
			{
				this.appliedFloor[key] = which;
			}
			return;
		}
		this.appliedFloor[which_room] = which;
	}

	public void SetWallpaper(string which, string which_room)
	{
		if (which_room == null)
		{
			foreach (string key in this.wallpaperIDs)
			{
				this.appliedWallpaper[key] = which;
			}
			return;
		}
		this.appliedWallpaper[which_room] = which;
	}

	public void OverrideSpecificWallpaper(string which, string which_room, string wallpaperStyleToOverride)
	{
		if (which_room == null)
		{
			foreach (string key in this.wallpaperIDs)
			{
				if (this.appliedWallpaper.ContainsKey(key) && this.appliedWallpaper[key] == wallpaperStyleToOverride)
				{
					this.appliedWallpaper[key] = which;
				}
			}
			return;
		}
		if (this.appliedWallpaper[which_room] == wallpaperStyleToOverride)
		{
			this.appliedWallpaper[which_room] = which;
		}
	}

	public void OverrideSpecificFlooring(string which, string which_room, string flooringStyleToOverride)
	{
		if (which_room == null)
		{
			foreach (string key in this.floorIDs)
			{
				if (this.appliedFloor.ContainsKey(key) && this.appliedFloor[key] == flooringStyleToOverride)
				{
					this.appliedFloor[key] = which;
				}
			}
			return;
		}
		if (this.appliedFloor[which_room] == flooringStyleToOverride)
		{
			this.appliedFloor[which_room] = which;
		}
	}

	public string GetFloorID(int x, int y)
	{
		foreach (string id in this.floorTiles.Keys)
		{
			foreach (Vector3 tile_data in this.floorTiles[id])
			{
				if ((int)tile_data.X == x && (int)tile_data.Y == y)
				{
					return id;
				}
			}
		}
		return null;
	}

	public string GetWallpaperID(int x, int y)
	{
		foreach (string id in this.wallpaperTiles.Keys)
		{
			foreach (Vector3 tile_data in this.wallpaperTiles[id])
			{
				if ((int)tile_data.X == x && (int)tile_data.Y == y)
				{
					return id;
				}
			}
		}
		return null;
	}

	protected bool IsFloorableTile(int x, int y, string layer_name)
	{
		int tile_index = base.getTileIndexAt(x, y, "Buildings");
		if (tile_index >= 197 && tile_index <= 199 && base.getTileSheetIDAt(x, y, "Buildings") == "untitled tile sheet")
		{
			return false;
		}
		return this.IsFloorableOrWallpaperableTile(x, y, layer_name);
	}

	public bool IsWallAndFloorTilesheet(string tilesheet_id)
	{
		if (tilesheet_id.StartsWith("x_WallsAndFloors_"))
		{
			return true;
		}
		return tilesheet_id == "walls_and_floors";
	}

	protected bool IsFloorableOrWallpaperableTile(int x, int y, string layer_name)
	{
		Layer layer = base.map.GetLayer(layer_name);
		if (layer != null && x < layer.LayerWidth && y < layer.LayerHeight && layer.Tiles[x, y] != null && layer.Tiles[x, y].TileSheet != null && this.IsWallAndFloorTilesheet(layer.Tiles[x, y].TileSheet.Id))
		{
			return true;
		}
		return false;
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		if (l is DecoratableLocation decoratable_location)
		{
			if (!decoratable_location.appliedWallpaper.Keys.Any())
			{
				this.ReadWallpaperAndFloorTileData();
				for (int j = 0; j < decoratable_location.wallPaper.Count; j++)
				{
					try
					{
						string key2 = this.wallpaperIDs[j];
						string value2 = decoratable_location.wallPaper[j].ToString();
						this.appliedWallpaper[key2] = value2;
					}
					catch (Exception)
					{
					}
				}
				for (int i = 0; i < decoratable_location.floor.Count; i++)
				{
					try
					{
						string key = this.floorIDs[i];
						string value = decoratable_location.floor[i].ToString();
						this.appliedFloor[key] = value;
					}
					catch (Exception)
					{
					}
				}
			}
			else
			{
				foreach (string key4 in decoratable_location.appliedWallpaper.Keys)
				{
					this.appliedWallpaper[key4] = decoratable_location.appliedWallpaper[key4];
				}
				foreach (string key3 in decoratable_location.appliedFloor.Keys)
				{
					this.appliedFloor[key3] = decoratable_location.appliedFloor[key3];
				}
			}
		}
		this.setWallpapers();
		this.setFloors();
		base.TransferDataFromSavedLocation(l);
	}

	public Furniture getRandomFurniture(Random r)
	{
		return r.ChooseFrom(base.furniture);
	}

	public virtual string getFloorRoomIdAt(Point p)
	{
		foreach (string key in this.floorTiles.Keys)
		{
			foreach (Vector3 tile_data in this.floorTiles[key])
			{
				if ((int)tile_data.X == p.X && (int)tile_data.Y == p.Y)
				{
					return key;
				}
			}
		}
		return null;
	}

	public virtual int GetFirstFlooringTile()
	{
		return 336;
	}

	public virtual int GetFlooringIndex(int base_tile_sheet, int tile_x, int tile_y)
	{
		int replaced_tile_index = base.getTileIndexAt(tile_x, tile_y, "Back");
		if (replaced_tile_index < 0)
		{
			return 0;
		}
		string tilesheet_name = base.getTileSheetIDAt(tile_x, tile_y, "Back");
		TileSheet tilesheet = base.map.GetTileSheet(tilesheet_name);
		int tiles_wide = 16;
		if (tilesheet != null)
		{
			tiles_wide = tilesheet.SheetWidth;
		}
		if (tilesheet_name == "walls_and_floors")
		{
			replaced_tile_index -= this.GetFirstFlooringTile();
		}
		int x_offset = tile_x % 2;
		int y_offset = tile_y % 2;
		return base_tile_sheet + x_offset + tiles_wide * y_offset;
	}

	public virtual List<Microsoft.Xna.Framework.Rectangle> getFloors()
	{
		return new List<Microsoft.Xna.Framework.Rectangle>();
	}
}
