using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;

namespace StardewValley.Menus;

public class PI_ItemList : ProfileItem
{
	protected List<Item> _items;

	protected List<ClickableTextureComponent> _components;

	protected float _height;

	protected List<Vector2> _emptyBoxPositions;

	public PI_ItemList(ProfileMenu context, string name, List<Item> values)
		: base(context, name)
	{
		this._items = values;
		this._components = new List<ClickableTextureComponent>();
		this._height = 0f;
		this._emptyBoxPositions = new List<Vector2>();
		this._UpdateIcons();
	}

	public override void Unload()
	{
		base.Unload();
		this._ClearItems();
	}

	protected void _ClearItems()
	{
		for (int i = 0; i < this._components.Count; i++)
		{
			base._context.UnregisterClickable(this._components[i]);
		}
		this._components.Clear();
	}

	protected void _UpdateIcons()
	{
		this._ClearItems();
		Vector2 draw_position = new Vector2(0f, 0f);
		for (int i = 0; i < this._items.Count; i++)
		{
			Item item = this._items[i];
			ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
			ClickableTextureComponent component = new ClickableTextureComponent(item.DisplayName, new Rectangle((int)draw_position.X, (int)draw_position.Y, 32, 32), null, "", itemData.GetTexture(), itemData.GetSourceRect(), 2f)
			{
				myID = 0,
				name = item.DisplayName,
				upNeighborID = -99998,
				downNeighborID = -99998,
				leftNeighborID = -99998,
				rightNeighborID = -99998,
				region = 502
			};
			this._components.Add(component);
			base._context.RegisterClickable(component);
		}
	}

	public override float HandleLayout(float draw_y, Rectangle content_rectangle, int index)
	{
		this._emptyBoxPositions.Clear();
		draw_y = base.HandleLayout(draw_y, content_rectangle, index);
		int draw_x = 0;
		int lowest_drawn_position = (int)draw_y;
		Point padding = new Point(4, 4);
		for (int i = 0; i < this._components.Count; i++)
		{
			ClickableTextureComponent component = this._components[i];
			if (draw_x + component.bounds.Width + padding.Y > content_rectangle.Width)
			{
				draw_x = 0;
				draw_y += (float)(component.bounds.Height + padding.Y);
			}
			component.bounds.X = content_rectangle.Left + draw_x;
			component.bounds.Y = (int)draw_y;
			draw_x += component.bounds.Width + padding.X;
			lowest_drawn_position = Math.Max((int)draw_y + component.bounds.Height, lowest_drawn_position);
		}
		for (; draw_x + 32 + padding.X <= content_rectangle.Width; draw_x += 32 + padding.X)
		{
			this._emptyBoxPositions.Add(new Vector2(content_rectangle.Left + draw_x, draw_y));
		}
		return lowest_drawn_position + 8;
	}

	public override void DrawItem(SpriteBatch b)
	{
		for (int j = 0; j < this._components.Count; j++)
		{
			ClickableTextureComponent component = this._components[j];
			b.Draw(Game1.menuTexture, new Rectangle(component.bounds.X, component.bounds.Y, 32, 32), new Rectangle(64, 128, 64, 64), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 4.3E-05f);
			b.Draw(Game1.menuTexture, new Rectangle(component.bounds.X, component.bounds.Y, 32, 32), new Rectangle(128, 128, 64, 64), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 4.3E-05f);
			this._components[j].draw(b, Color.White, 4.1E-05f);
			if (Game1.player.Items.ContainsId(this._items[j].ItemId))
			{
				b.Draw(Game1.mouseCursors, new Rectangle(this._components[j].bounds.X + 32 - 11, this._components[j].bounds.Y + 32 - 13, 11, 13), new Rectangle(268, 1436, 11, 13), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 4E-05f);
			}
		}
		for (int i = 0; i < this._emptyBoxPositions.Count; i++)
		{
			b.Draw(Game1.menuTexture, new Rectangle((int)this._emptyBoxPositions[i].X, (int)this._emptyBoxPositions[i].Y, 32, 32), new Rectangle(64, 896, 64, 64), Color.White * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 4.3E-05f);
			b.Draw(Game1.menuTexture, new Rectangle((int)this._emptyBoxPositions[i].X, (int)this._emptyBoxPositions[i].Y, 32, 32), new Rectangle(128, 128, 64, 64), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 4.3E-05f);
		}
	}

	public override void performHover(int x, int y)
	{
		for (int i = 0; i < this._components.Count; i++)
		{
			if (this._components[i].bounds.Contains(new Point(x, y)))
			{
				base._context.hoveredItem = this._items[i];
			}
		}
	}

	public override bool ShouldDraw()
	{
		return this._items.Count > 0;
	}
}
