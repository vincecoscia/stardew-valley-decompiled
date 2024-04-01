using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewValley.GameData;

namespace StardewValley;

/// <summary>Loads assets and translations from the game's content folder.</summary>
public class LocalizedContentManager : ContentManager
{
	public delegate void LanguageChangedHandler(LanguageCode code);

	/// <summary>A language supported by the game.</summary>
	public enum LanguageCode
	{
		/// <summary>The English language.</summary>
		en,
		/// <summary>The Japanese language.</summary>
		ja,
		/// <summary>The Russian language.</summary>
		ru,
		/// <summary>The Chinese language.</summary>
		zh,
		/// <summary>The Portuguese language.</summary>
		pt,
		/// <summary>The Spanish language.</summary>
		es,
		/// <summary>The German language.</summary>
		de,
		/// <summary>The Thai language.</summary>
		th,
		/// <summary>The French language.</summary>
		fr,
		/// <summary>The Korean language.</summary>
		ko,
		/// <summary>The Italian language.</summary>
		it,
		/// <summary>The Turkish language.</summary>
		tr,
		/// <summary>The Hungarian language.</summary>
		hu,
		/// <summary>A custom language added by a mod.</summary>
		mod
	}

	public static readonly Dictionary<string, string> localizedAssetNames = new Dictionary<string, string>();

	/// <summary>The backing field for <see cref="M:StardewValley.LocalizedContentManager.GetContentRoot" />.</summary>
	protected string _CachedContentRoot;

	/// <summary>The backing field for <see cref="P:StardewValley.LocalizedContentManager.CurrentLanguageCode" />.</summary>
	private static LanguageCode _currentLangCode = LocalizedContentManager.GetDefaultLanguageCode();

	/// <summary>The backing field for <see cref="P:StardewValley.LocalizedContentManager.CurrentLanguageString" />.</summary>
	private static string _currentLangString = null;

	private static ModLanguage _currentModLanguage = null;

	public CultureInfo CurrentCulture;

	protected static StringBuilder _timeFormatStringBuilder = new StringBuilder();

	/// <summary>The current language as a string which appears in localized asset names (like <c>pt-BR</c>).</summary>
	public static string CurrentLanguageString
	{
		get
		{
			if (LocalizedContentManager._currentLangString == null)
			{
				LocalizedContentManager._currentLangString = LocalizedContentManager.LanguageCodeString(LocalizedContentManager.CurrentLanguageCode);
			}
			return LocalizedContentManager._currentLangString;
		}
	}

	/// <summary>The current language as an enum.</summary>
	/// <remarks>Note that <see cref="F:StardewValley.LocalizedContentManager.LanguageCode.mod" /> is used for any custom language, so you'll need to use <see cref="P:StardewValley.LocalizedContentManager.CurrentLanguageString" /> to distinguish those.</remarks>
	public static LanguageCode CurrentLanguageCode
	{
		get
		{
			return LocalizedContentManager._currentLangCode;
		}
		set
		{
			if (LocalizedContentManager._currentLangCode != value)
			{
				LanguageCode prev = LocalizedContentManager._currentLangCode;
				LocalizedContentManager._currentLangCode = value;
				LocalizedContentManager._currentLangString = null;
				if (LocalizedContentManager._currentLangCode != LanguageCode.mod)
				{
					LocalizedContentManager._currentModLanguage = null;
				}
				Game1.log.Verbose("LocalizedContentManager.CurrentLanguageCode CHANGING from '" + prev.ToString() + "' to '" + LocalizedContentManager._currentLangCode.ToString() + "'");
				LocalizedContentManager.OnLanguageChange?.Invoke(LocalizedContentManager._currentLangCode);
				Game1.log.Verbose("LocalizedContentManager.CurrentLanguageCode CHANGED from '" + prev.ToString() + "' to '" + LocalizedContentManager._currentLangCode.ToString() + "'");
			}
		}
	}

	public static bool CurrentLanguageLatin
	{
		get
		{
			if (LocalizedContentManager.CurrentLanguageCode != 0 && LocalizedContentManager.CurrentLanguageCode != LanguageCode.es && LocalizedContentManager.CurrentLanguageCode != LanguageCode.de && LocalizedContentManager.CurrentLanguageCode != LanguageCode.pt && LocalizedContentManager.CurrentLanguageCode != LanguageCode.fr && LocalizedContentManager.CurrentLanguageCode != LanguageCode.it && LocalizedContentManager.CurrentLanguageCode != LanguageCode.tr && LocalizedContentManager.CurrentLanguageCode != LanguageCode.hu)
			{
				if (LocalizedContentManager.CurrentLanguageCode == LanguageCode.mod)
				{
					return LocalizedContentManager._currentModLanguage.UseLatinFont;
				}
				return false;
			}
			return true;
		}
	}

	public static ModLanguage CurrentModLanguage => LocalizedContentManager._currentModLanguage;

	public static event LanguageChangedHandler OnLanguageChange;

	public static LanguageCode GetDefaultLanguageCode()
	{
		return LanguageCode.en;
	}

	public LocalizedContentManager(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture)
		: base(serviceProvider, rootDirectory)
	{
		this.CurrentCulture = currentCulture;
	}

	public LocalizedContentManager(IServiceProvider serviceProvider, string rootDirectory)
		: this(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture)
	{
	}

	protected static bool _IsStringAt(string source, string string_to_find, int index)
	{
		for (int i = 0; i < string_to_find.Length; i++)
		{
			int source_index = index + i;
			if (source_index >= source.Length)
			{
				return false;
			}
			if (source[source_index] != string_to_find[i])
			{
				return false;
			}
		}
		return true;
	}

	public static StringBuilder FormatTimeString(int time, string format)
	{
		LocalizedContentManager._timeFormatStringBuilder.Clear();
		int brace_start_index = -1;
		for (int i = 0; i < format.Length; i++)
		{
			char character = format[i];
			switch (character)
			{
			case '[':
			{
				if (brace_start_index < 0)
				{
					brace_start_index = i;
					continue;
				}
				for (int j = brace_start_index; j <= i; j++)
				{
					LocalizedContentManager._timeFormatStringBuilder.Append(format[j]);
				}
				brace_start_index = i;
				continue;
			}
			case ']':
				if (brace_start_index < 0)
				{
					break;
				}
				if (LocalizedContentManager._IsStringAt(format, "[HOURS_12]", brace_start_index))
				{
					LocalizedContentManager._timeFormatStringBuilder.Append((time / 100 % 12 == 0) ? "12" : (time / 100 % 12).ToString());
				}
				else if (LocalizedContentManager._IsStringAt(format, "[HOURS_12_0]", brace_start_index))
				{
					LocalizedContentManager._timeFormatStringBuilder.Append((time / 100 % 12 == 0) ? "0" : (time / 100 % 12).ToString());
				}
				else if (LocalizedContentManager._IsStringAt(format, "[HOURS_24]", brace_start_index))
				{
					LocalizedContentManager._timeFormatStringBuilder.Append(time / 100 % 24);
				}
				else if (LocalizedContentManager._IsStringAt(format, "[HOURS_24_00]", brace_start_index))
				{
					LocalizedContentManager._timeFormatStringBuilder.Append((time / 100 % 24).ToString("00"));
				}
				else if (LocalizedContentManager._IsStringAt(format, "[MINUTES]", brace_start_index))
				{
					LocalizedContentManager._timeFormatStringBuilder.Append((time % 100).ToString("00"));
				}
				else if (LocalizedContentManager._IsStringAt(format, "[AM_PM]", brace_start_index))
				{
					if (time < 1200 || time >= 2400)
					{
						LocalizedContentManager._timeFormatStringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10370"));
					}
					else
					{
						LocalizedContentManager._timeFormatStringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10371"));
					}
				}
				else
				{
					for (int k = brace_start_index; k <= i; k++)
					{
						LocalizedContentManager._timeFormatStringBuilder.Append(format[k]);
					}
				}
				brace_start_index = -1;
				continue;
			}
			if (brace_start_index < 0)
			{
				LocalizedContentManager._timeFormatStringBuilder.Append(character);
			}
		}
		return LocalizedContentManager._timeFormatStringBuilder;
	}

	public static void SetModLanguage(ModLanguage new_mod_language)
	{
		if (new_mod_language != LocalizedContentManager._currentModLanguage)
		{
			LocalizedContentManager._currentModLanguage = new_mod_language;
			LocalizedContentManager.CurrentLanguageCode = LanguageCode.mod;
		}
	}

	/// <summary>Get the absolute path to the root content directory from which this manager loads assets.</summary>
	public virtual string GetContentRoot()
	{
		if (this._CachedContentRoot == null)
		{
			string basePath = ((string)(typeof(TitleContainer).GetProperty("Location", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Can't get TitleContainer.Location property from MonoGame")).GetValue(null, null)) ?? throw new InvalidOperationException("Can't get value of TitleContainer.Location property from MonoGame");
			this._CachedContentRoot = Path.Combine(basePath, base.RootDirectory);
		}
		return this._CachedContentRoot;
	}

	/// <summary>Get whether an asset exists without loading it.</summary>
	/// <typeparam name="T">The expected asset type.</typeparam>
	/// <param name="assetName">The asset name to check.</param>
	public virtual bool DoesAssetExist<T>(string assetName)
	{
		if (assetName == null)
		{
			return false;
		}
		if (Environment.OSVersion.Platform != PlatformID.Win32NT)
		{
			assetName = assetName.Replace('\\', '/');
		}
		return File.Exists(Path.Combine(this.GetContentRoot(), assetName + ".xnb"));
	}

	/// <summary>Load an asset through the content pipeline.</summary>
	/// <typeparam name="T">The type of asset to load.</typeparam>
	/// <param name="baseAssetName">The unlocalized asset name relative to the game's root directory.</param>
	/// <param name="localizedAssetName">The localized asset name relative to the game's root directory.</param>
	/// <param name="languageCode">The language for which to load the asset.</param>
	public virtual T LoadImpl<T>(string baseAssetName, string localizedAssetName, LanguageCode languageCode)
	{
		return base.Load<T>(localizedAssetName);
	}

	/// <summary>Load an asset through the content pipeline.</summary>
	/// <typeparam name="T">The type of asset to load.</typeparam>
	/// <param name="assetName">The unlocalized asset name relative to the game's root directory.</param>
	public override T Load<T>(string assetName)
	{
		return this.Load<T>(assetName, LocalizedContentManager.CurrentLanguageCode);
	}

	/// <summary>Load an asset through the content pipeline.</summary>
	/// <typeparam name="T">The type of asset to load.</typeparam>
	/// <param name="assetName">The unlocalized asset name relative to the game's root directory.</param>
	/// <param name="language">The language for which to load the asset.</param>
	public virtual T Load<T>(string assetName, LanguageCode language)
	{
		if (language != 0)
		{
			if (!LocalizedContentManager.localizedAssetNames.TryGetValue(assetName, out var _))
			{
				bool fail = false;
				string localizedAssetName = assetName + "." + ((language == LocalizedContentManager.CurrentLanguageCode) ? LocalizedContentManager.CurrentLanguageString : LocalizedContentManager.LanguageCodeString(language));
				if (!this.DoesAssetExist<T>(localizedAssetName))
				{
					fail = true;
				}
				if (!fail)
				{
					try
					{
						this.LoadImpl<T>(assetName, localizedAssetName, language);
						LocalizedContentManager.localizedAssetNames[assetName] = localizedAssetName;
					}
					catch (ContentLoadException)
					{
						fail = true;
					}
				}
				if (fail)
				{
					fail = false;
					localizedAssetName = assetName + "_international";
					if (!this.DoesAssetExist<T>(localizedAssetName))
					{
						fail = true;
					}
					if (!fail)
					{
						try
						{
							this.LoadImpl<T>(assetName, localizedAssetName, language);
							LocalizedContentManager.localizedAssetNames[assetName] = localizedAssetName;
						}
						catch (ContentLoadException)
						{
							fail = true;
						}
					}
					if (fail)
					{
						LocalizedContentManager.localizedAssetNames[assetName] = assetName;
					}
				}
			}
			return this.LoadImpl<T>(assetName, LocalizedContentManager.localizedAssetNames[assetName], language);
		}
		return this.LoadImpl<T>(assetName, assetName, LanguageCode.en);
	}

	/// <summary>Get the language string which appears in localized asset names for a language (like <c>pt-BR</c>).</summary>
	/// <param name="code">The language whose asset name code to get.</param>
	/// <remarks>For the current language, see <see cref="P:StardewValley.LocalizedContentManager.CurrentLanguageString" /> instead.</remarks>
	public static string LanguageCodeString(LanguageCode code)
	{
		return code switch
		{
			LanguageCode.ja => "ja-JP", 
			LanguageCode.ru => "ru-RU", 
			LanguageCode.zh => "zh-CN", 
			LanguageCode.pt => "pt-BR", 
			LanguageCode.es => "es-ES", 
			LanguageCode.de => "de-DE", 
			LanguageCode.th => "th-TH", 
			LanguageCode.fr => "fr-FR", 
			LanguageCode.ko => "ko-KR", 
			LanguageCode.it => "it-IT", 
			LanguageCode.tr => "tr-TR", 
			LanguageCode.hu => "hu-HU", 
			LanguageCode.mod => (LocalizedContentManager._currentModLanguage ?? throw new InvalidOperationException("The game language is set to a custom one, but the language info is no longer available.")).LanguageCode, 
			_ => "", 
		};
	}

	/// <summary>Get the current language as an enum.</summary>
	public LanguageCode GetCurrentLanguage()
	{
		return LocalizedContentManager.CurrentLanguageCode;
	}

	/// <summary>Read a translation key from a loaded strings asset.</summary>
	/// <param name="strings">The loaded strings asset.</param>
	/// <param name="key">The translation key to load.</param>
	private string GetString(Dictionary<string, string> strings, string key)
	{
		if (strings.TryGetValue(key + ".desktop", out var result))
		{
			return result;
		}
		return strings[key];
	}

	/// <summary>Get whether a string is a valid translation key which can be loaded by methods like <see cref="M:StardewValley.LocalizedContentManager.LoadString(System.String)" />.</summary>
	/// <param name="path">The potential translation key to check.</param>
	public virtual bool IsValidTranslationKey(string path)
	{
		try
		{
			return this.LoadString(path) != path;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Get translation text from a data asset, if found.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <returns>Returns the loaded string if found, else <c>null</c>.</returns>
	public virtual string LoadStringReturnNullIfNotFound(string path)
	{
		string result = this.LoadString(path);
		if (!result.Equals(path))
		{
			return result;
		}
		return null;
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path)
	{
		this.parseStringPath(path, out var assetName, out var key);
		Dictionary<string, string> strings = this.Load<Dictionary<string, string>>(assetName);
		string sentence = ((strings?.ContainsKey(key) ?? false) ? this.GetString(strings, key) : this.LoadBaseString(path));
		return this.PreprocessString(sentence);
	}

	/// <summary>Apply generic preprocessing to strings loaded from <see cref="M:StardewValley.LocalizedContentManager.LoadString(System.String)" /> and its overloads.</summary>
	/// <param name="text">The text to preprocess.</param>
	public virtual string PreprocessString(string text)
	{
		Gender gender = Game1.player?.Gender ?? Gender.Male;
		text = Dialogue.applyGenderSwitchBlocks(gender, text);
		text = Dialogue.applyGenderSwitch(gender, text, altTokenOnly: true);
		return text;
	}

	public virtual bool ShouldUseGenderedCharacterTranslations()
	{
		if (LocalizedContentManager.CurrentLanguageCode == LanguageCode.pt)
		{
			return true;
		}
		if (LocalizedContentManager.CurrentLanguageCode == LanguageCode.mod && LocalizedContentManager.CurrentModLanguage != null)
		{
			return LocalizedContentManager.CurrentModLanguage.UseGenderedCharacterTranslations;
		}
		return false;
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path, object sub1)
	{
		string sentence = this.LoadString(path);
		try
		{
			return string.Format(sentence, sub1);
		}
		catch (Exception)
		{
			return sentence;
		}
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <param name="sub2">The value with which to replace the <c>{1}</c> placeholder in the loaded text.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path, object sub1, object sub2)
	{
		string sentence = this.LoadString(path);
		try
		{
			return string.Format(sentence, sub1, sub2);
		}
		catch (Exception)
		{
			return sentence;
		}
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <param name="sub2">The value with which to replace the <c>{1}</c> placeholder in the loaded text.</param>
	/// <param name="sub3">The value with which to replace the <c>{2}</c> placeholder in the loaded text.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path, object sub1, object sub2, object sub3)
	{
		string sentence = this.LoadString(path);
		try
		{
			return string.Format(sentence, sub1, sub2, sub3);
		}
		catch (Exception)
		{
			return sentence;
		}
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="substitutions">The values with which to replace placeholders like <c>{0}</c> in the loaded text.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path, params object[] substitutions)
	{
		string sentence = this.LoadString(path);
		if (substitutions.Length != 0)
		{
			try
			{
				return string.Format(sentence, substitutions);
			}
			catch (Exception)
			{
			}
		}
		return sentence;
	}

	public virtual string LoadBaseString(string path)
	{
		this.parseStringPath(path, out var assetName, out var key);
		Dictionary<string, string> strings = this.LoadImpl<Dictionary<string, string>>(assetName, assetName, LanguageCode.en);
		if (strings != null && strings.ContainsKey(key))
		{
			return this.GetString(strings, key);
		}
		return path;
	}

	private void parseStringPath(string path, out string assetName, out string key)
	{
		int i = path.IndexOf(':');
		if (i == -1)
		{
			throw new ContentLoadException("Unable to parse string path: " + path);
		}
		assetName = path.Substring(0, i);
		key = path.Substring(i + 1, path.Length - i - 1);
	}

	public virtual LocalizedContentManager CreateTemporary()
	{
		return new LocalizedContentManager(base.ServiceProvider, base.RootDirectory, this.CurrentCulture);
	}
}
