using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;

namespace StardewValley.Menus;

internal class LinkCreditsBlock : ICreditsBlock
{
	private string text;

	private string url;

	private bool currentlyHovered;

	public LinkCreditsBlock(string text, string url)
	{
		this.text = text;
		this.url = url;
	}

	public override void draw(int topLeftX, int topLeftY, int widthToOccupy, SpriteBatch b)
	{
		SpriteText.drawString(b, this.text, topLeftX, topLeftY, 999999, widthToOccupy, 99999, 1f, 0.88f, junimoText: false, -1, "", this.currentlyHovered ? SpriteText.color_Green : SpriteText.color_Cyan);
		this.currentlyHovered = false;
	}

	public override int getHeight(int maxWidth)
	{
		if (!(this.text == ""))
		{
			return SpriteText.getHeightOfString(this.text, maxWidth);
		}
		return 64;
	}

	public override void hovered()
	{
		this.currentlyHovered = true;
	}

	public override void clicked()
	{
		Game1.playSound("bigSelect");
		try
		{
			Process.Start(this.url);
		}
		catch (Exception)
		{
		}
	}
}
