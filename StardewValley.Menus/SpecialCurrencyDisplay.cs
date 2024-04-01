using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;

namespace StardewValley.Menus;

public class SpecialCurrencyDisplay
{
	public class CurrencyDisplayType
	{
		public string key;

		public NetIntDelta netIntDelta;

		public Action<int> playSound;

		public Action<SpriteBatch, Vector2> drawSprite;
	}

	protected MoneyDial _moneyDial;

	protected float currentPosition;

	protected CurrencyDisplayType _currentDisplayedCurrency;

	public Dictionary<string, CurrencyDisplayType> registeredCurrencyDisplays;

	public float timeToLive;

	public Action<SpriteBatch, Vector2> drawSprite;

	public CurrencyDisplayType forcedOnscreenCurrencyType;

	public SpecialCurrencyDisplay()
	{
		this._moneyDial = new MoneyDial(3);
		this._moneyDial.onPlaySound = null;
		this.drawSprite = null;
		this.registeredCurrencyDisplays = new Dictionary<string, CurrencyDisplayType>();
	}

	public virtual void Register(string key, NetIntDelta net_int_delta, Action<int> sound_function = null, Action<SpriteBatch, Vector2> draw_function = null)
	{
		if (this.registeredCurrencyDisplays.ContainsKey(key))
		{
			this.Unregister(key);
		}
		CurrencyDisplayType currency_type = new CurrencyDisplayType();
		currency_type.key = key;
		currency_type.netIntDelta = net_int_delta;
		currency_type.playSound = sound_function;
		currency_type.drawSprite = draw_function;
		this.registeredCurrencyDisplays[key] = currency_type;
		this.registeredCurrencyDisplays[key].netIntDelta.fieldChangeVisibleEvent += OnCurrencyChange;
	}

	public virtual void ShowCurrency(string currency_type)
	{
		if (currency_type == null || !this.registeredCurrencyDisplays.TryGetValue(currency_type, out this.forcedOnscreenCurrencyType))
		{
			this.forcedOnscreenCurrencyType = null;
		}
		else
		{
			this.SetDisplayedCurrency(this.forcedOnscreenCurrencyType);
		}
	}

	public virtual void OnCurrencyChange(NetIntDelta field, int old_value, int new_value)
	{
		if (Game1.gameMode != 3)
		{
			return;
		}
		string currency_key = null;
		foreach (string key in this.registeredCurrencyDisplays.Keys)
		{
			if (this.registeredCurrencyDisplays[key].netIntDelta == field)
			{
				currency_key = key;
				break;
			}
		}
		if (currency_key != null)
		{
			this.SetDisplayedCurrency(currency_key);
			if (this._currentDisplayedCurrency != null)
			{
				this._moneyDial.currentValue = old_value;
				this._moneyDial.onPlaySound?.Invoke(new_value - old_value);
			}
			this.timeToLive = 5f;
		}
	}

	public virtual void SetDisplayedCurrency(CurrencyDisplayType currency_type)
	{
		if (currency_type == this._currentDisplayedCurrency || (this.forcedOnscreenCurrencyType != null && this.forcedOnscreenCurrencyType != currency_type))
		{
			return;
		}
		this._moneyDial.onPlaySound = null;
		this.drawSprite = null;
		this._currentDisplayedCurrency = currency_type;
		if (currency_type != null)
		{
			this._moneyDial.currentValue = this._currentDisplayedCurrency.netIntDelta.Value;
			this._moneyDial.previousTargetValue = this._moneyDial.currentValue;
			if (currency_type.playSound != null)
			{
				this._moneyDial.onPlaySound = currency_type.playSound;
			}
			else
			{
				this._moneyDial.onPlaySound = DefaultPlaySound;
			}
			if (currency_type.drawSprite != null)
			{
				this.drawSprite = currency_type.drawSprite;
			}
			else
			{
				this.drawSprite = DefaultDrawSprite;
			}
		}
	}

	public virtual void SetDisplayedCurrency(string key)
	{
		if (this.registeredCurrencyDisplays.TryGetValue(key, out var newCurrencyType))
		{
			this.SetDisplayedCurrency(newCurrencyType);
		}
	}

	public virtual void Unregister(string key)
	{
		if (this.registeredCurrencyDisplays.TryGetValue(key, out var newCurrencyType))
		{
			if (this._currentDisplayedCurrency == newCurrencyType)
			{
				this.SetDisplayedCurrency((CurrencyDisplayType)null);
			}
			newCurrencyType.netIntDelta.fieldChangeVisibleEvent -= OnCurrencyChange;
			this.registeredCurrencyDisplays.Remove(key);
		}
	}

	public virtual void Cleanup()
	{
		foreach (string key in new List<string>(this.registeredCurrencyDisplays.Keys))
		{
			this.Unregister(key);
		}
	}

	public virtual void DefaultDrawSprite(SpriteBatch b, Vector2 position)
	{
		if (this._currentDisplayedCurrency == null)
		{
			return;
		}
		string key = this._currentDisplayedCurrency.key;
		if (!(key == "walnuts"))
		{
			if (key == "qiGems")
			{
				b.Draw(Game1.objectSpriteSheet, position, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 858, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
			}
		}
		else
		{
			b.Draw(Game1.objectSpriteSheet, position, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 73, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
		}
	}

	public virtual void DefaultPlaySound(int direction)
	{
		if (this._currentDisplayedCurrency != null)
		{
			if (direction < 0 && this._currentDisplayedCurrency.key == "walnuts")
			{
				Game1.playSound("goldenWalnut");
			}
			if (direction > 0 && this._currentDisplayedCurrency.key == "walnuts")
			{
				Game1.playSound("goldenWalnut");
			}
		}
	}

	public virtual void Update(GameTime time)
	{
		if (this.timeToLive > 0f)
		{
			this.timeToLive -= (float)time.ElapsedGameTime.TotalSeconds;
			if (this.timeToLive < 0f)
			{
				this.timeToLive = 0f;
			}
		}
		if (this.timeToLive > 0f || this.forcedOnscreenCurrencyType != null)
		{
			this.currentPosition += (float)time.ElapsedGameTime.TotalSeconds / 0.5f;
		}
		else
		{
			this.currentPosition -= (float)time.ElapsedGameTime.TotalSeconds / 0.5f;
		}
		this.currentPosition = Utility.Clamp(this.currentPosition, 0f, 1f);
	}

	public Vector2 GetUpperLeft()
	{
		return new Vector2(16f, (int)Utility.Lerp(-26f, 0f, this.currentPosition) * 4);
	}

	public virtual void Draw(SpriteBatch b)
	{
		if (this._currentDisplayedCurrency != null && !(this.currentPosition <= 0f))
		{
			Vector2 draw_position = this.GetUpperLeft();
			b.Draw(Game1.mouseCursors2, draw_position, new Rectangle(48, 176, 52, 26), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
			int displayed_value = this._currentDisplayedCurrency.netIntDelta.Value;
			if (this.currentPosition < 0.5f)
			{
				displayed_value = this._moneyDial.previousTargetValue;
			}
			this._moneyDial.draw(b, draw_position + new Vector2(108f, 40f), displayed_value);
			this.drawSprite?.Invoke(b, draw_position + new Vector2(4f, 6f) * 4f);
		}
	}

	public static void Draw(SpriteBatch b, Vector2 drawPosition, MoneyDial moneyDial, int displayedValue, Texture2D drawSpriteTexture, Rectangle drawSpriteSourceRect)
	{
		if (moneyDial != null && moneyDial.numDigits > 3)
		{
			b.Draw(Game1.mouseCursors_1_6, drawPosition, new Rectangle(42, 0, 57, 26), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
		}
		else
		{
			b.Draw(Game1.mouseCursors2, drawPosition, new Rectangle(48, 176, 52, 26), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
		}
		moneyDial?.draw(b, drawPosition + new Vector2(108f, 40f), displayedValue);
		b.Draw(drawSpriteTexture, drawPosition + new Vector2(4f, 6f) * 4f, drawSpriteSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
	}

	/// <summary>Draw a very basic static money dial which can only do 3 digits.</summary>
	public static void Draw(SpriteBatch b, Vector2 drawPosition, int displayedValue, Texture2D drawSpriteTexture, Rectangle drawSpriteSourceRect)
	{
		b.Draw(Game1.mouseCursors2, drawPosition, new Rectangle(48, 176, 52, 26), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
		int numDigits = 3;
		int xPosition = 0;
		int digitStrip = (int)Math.Pow(10.0, numDigits - 1);
		bool significant = false;
		for (int i = 0; i < numDigits; i++)
		{
			int currentDigit = displayedValue / digitStrip % 10;
			if (currentDigit > 0 || i == numDigits - 1)
			{
				significant = true;
			}
			if (significant)
			{
				b.Draw(Game1.mouseCursors, drawPosition + new Vector2(108f, 40f) + new Vector2(xPosition, 0f), new Rectangle(286, 502 - currentDigit * 8, 5, 8), Color.Maroon, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
			xPosition += 24;
			digitStrip /= 10;
		}
		b.Draw(drawSpriteTexture, drawPosition + new Vector2(4f, 6f) * 4f, drawSpriteSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
	}
}
