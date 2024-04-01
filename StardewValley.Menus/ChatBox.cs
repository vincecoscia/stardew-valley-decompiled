using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Logging;
using StardewValley.SaveMigrations;
using StardewValley.TokenizableStrings;

namespace StardewValley.Menus;

public class ChatBox : IClickableMenu
{
	public const int chatMessage = 0;

	public const int errorMessage = 1;

	public const int userNotificationMessage = 2;

	public const int privateMessage = 3;

	public const int defaultMaxMessages = 10;

	public const int timeToDisplayMessages = 600;

	public const int chatboxWidth = 896;

	public const int chatboxHeight = 56;

	public const int region_chatBox = 101;

	public const int region_emojiButton = 102;

	public ChatTextBox chatBox;

	public ClickableComponent chatBoxCC;

	/// <summary>A logger which copies messages to the chat box, used when entering commands through the chat.</summary>
	private readonly IGameLogger CheatCommandChatLogger;

	private List<ChatMessage> messages = new List<ChatMessage>();

	private KeyboardState oldKBState;

	private List<string> cheatHistory = new List<string>();

	private int cheatHistoryPosition = -1;

	public int maxMessages = 10;

	public static Texture2D emojiTexture;

	public ClickableTextureComponent emojiMenuIcon;

	public EmojiMenu emojiMenu;

	public bool choosingEmoji;

	private long lastReceivedPrivateMessagePlayerId;

	public ChatBox()
	{
		this.CheatCommandChatLogger = new CheatCommandChatLogger(this);
		Texture2D chatboxTexture = Game1.content.Load<Texture2D>("LooseSprites\\chatBox");
		this.chatBox = new ChatTextBox(chatboxTexture, null, Game1.smallFont, Color.White);
		this.chatBox.OnEnterPressed += textBoxEnter;
		this.chatBox.TitleText = "Chat";
		this.chatBoxCC = new ClickableComponent(new Rectangle(this.chatBox.X, this.chatBox.Y, this.chatBox.Width, this.chatBox.Height), "")
		{
			myID = 101
		};
		Game1.keyboardDispatcher.Subscriber = this.chatBox;
		ChatBox.emojiTexture = Game1.content.Load<Texture2D>("LooseSprites\\emojis");
		this.emojiMenuIcon = new ClickableTextureComponent(new Rectangle(0, 0, 40, 36), ChatBox.emojiTexture, new Rectangle(0, 0, 9, 9), 4f)
		{
			myID = 102,
			leftNeighborID = 101
		};
		this.emojiMenu = new EmojiMenu(this, ChatBox.emojiTexture, chatboxTexture);
		this.chatBoxCC.rightNeighborID = 102;
		this.updatePosition();
		this.chatBox.Selected = false;
	}

	public override void snapToDefaultClickableComponent()
	{
		base.currentlySnappedComponent = base.getComponentWithID(101);
		this.snapCursorToCurrentSnappedComponent();
	}

	private void updatePosition()
	{
		this.chatBox.Width = 896;
		this.chatBox.Height = 56;
		base.width = this.chatBox.Width;
		base.height = this.chatBox.Height;
		base.xPositionOnScreen = 0;
		base.yPositionOnScreen = Game1.uiViewport.Height - this.chatBox.Height;
		Utility.makeSafe(ref base.xPositionOnScreen, ref base.yPositionOnScreen, this.chatBox.Width, this.chatBox.Height);
		this.chatBox.X = base.xPositionOnScreen;
		this.chatBox.Y = base.yPositionOnScreen;
		this.chatBoxCC.bounds = new Rectangle(this.chatBox.X, this.chatBox.Y, this.chatBox.Width, this.chatBox.Height);
		this.emojiMenuIcon.bounds.Y = this.chatBox.Y + 8;
		this.emojiMenuIcon.bounds.X = this.chatBox.Width - this.emojiMenuIcon.bounds.Width - 8;
		if (this.emojiMenu != null)
		{
			this.emojiMenu.xPositionOnScreen = this.emojiMenuIcon.bounds.Center.X - 146;
			this.emojiMenu.yPositionOnScreen = this.emojiMenuIcon.bounds.Y - 248;
		}
	}

	public virtual void textBoxEnter(string text_to_send)
	{
		if (text_to_send.Length < 1)
		{
			return;
		}
		if (text_to_send[0] == '/')
		{
			string text = ArgUtility.SplitBySpaceAndGet(text_to_send, 0);
			if (text != null && text.Length > 1)
			{
				this.runCommand(text_to_send.Substring(1));
				return;
			}
		}
		text_to_send = Program.sdk.FilterDirtyWords(text_to_send);
		Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, text_to_send, Multiplayer.AllPlayers);
		this.receiveChatMessage(Game1.player.UniqueMultiplayerID, 0, LocalizedContentManager.CurrentLanguageCode, text_to_send);
	}

	public virtual void textBoxEnter(TextBox sender)
	{
		bool include_color_information;
		if (sender is ChatTextBox box)
		{
			if (box.finalText.Count > 0)
			{
				include_color_information = true;
				string message = box.finalText[0].message;
				if (message != null && message.StartsWith('/'))
				{
					string text = ArgUtility.SplitBySpaceAndGet(box.finalText[0].message, 0);
					if (text != null && text.Length > 1)
					{
						include_color_information = false;
					}
				}
				if (box.finalText.Count != 1)
				{
					goto IL_00c8;
				}
				if (box.finalText[0].message != null || box.finalText[0].emojiIndex != -1)
				{
					string message2 = box.finalText[0].message;
					if (message2 == null || message2.Trim().Length != 0)
					{
						goto IL_00c8;
					}
				}
			}
			goto IL_00dc;
		}
		goto IL_00e9;
		IL_00e9:
		sender.Text = "";
		this.clickAway();
		return;
		IL_00dc:
		box.reset();
		this.cheatHistoryPosition = -1;
		goto IL_00e9;
		IL_00c8:
		string textToSend = ChatMessage.makeMessagePlaintext(box.finalText, include_color_information);
		this.textBoxEnter(textToSend);
		goto IL_00dc;
	}

	public virtual void addInfoMessage(string message)
	{
		this.receiveChatMessage(0L, 2, LocalizedContentManager.CurrentLanguageCode, message);
	}

	public virtual void globalInfoMessage(string messageKey, params string[] args)
	{
		if (Game1.IsMultiplayer)
		{
			Game1.multiplayer.globalChatInfoMessage(messageKey, args);
		}
		else
		{
			this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_" + messageKey, args));
		}
	}

	public virtual void addErrorMessage(string message)
	{
		this.receiveChatMessage(0L, 1, LocalizedContentManager.CurrentLanguageCode, message);
	}

	public virtual void listPlayers(bool otherPlayersOnly = false)
	{
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_UserList"));
		foreach (Farmer f in Game1.getOnlineFarmers())
		{
			if (!otherPlayersOnly || f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
			{
				this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_UserListUser", ChatBox.formattedUserNameLong(f)));
			}
		}
	}

	public virtual void showHelp()
	{
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_Help"));
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpClear", "clear"));
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpList", "list"));
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpColor", "color"));
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpColorList", "color-list"));
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpPause", "pause"));
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpResume", "resume"));
		if (Game1.IsMultiplayer)
		{
			this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpMessage", "message"));
			this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpReply", "reply"));
		}
		if (Game1.IsServer)
		{
			this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpKick", "kick"));
			this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpBan", "ban"));
			this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpUnban", "unban"));
		}
	}

	protected virtual void runCommand(string command)
	{
		string[] split = ArgUtility.SplitBySpace(command);
		switch (split[0])
		{
		case "qi":
			if (Game1.player.mailReceived.Add("QiChat1"))
			{
				this.addMessage(Game1.content.LoadString("Strings\\UI:Chat_Qi1"), new Color(100, 50, 255));
			}
			else if (Game1.player.mailReceived.Add("QiChat2"))
			{
				this.addMessage(Game1.content.LoadString("Strings\\UI:Chat_Qi2"), new Color(100, 50, 255));
				this.addMessage(Game1.content.LoadString("Strings\\UI:Chat_Qi3"), Color.Yellow);
			}
			break;
		case "ape":
		case "concernedape":
		case "ConcernedApe":
		case "ca":
			if (Game1.player.mailReceived.Add("apeChat1"))
			{
				this.addMessage(Game1.content.LoadString("Strings\\UI:Chat_ConcernedApe"), new Color(104, 214, 255));
			}
			else
			{
				this.addMessage(Game1.content.LoadString("Strings\\UI:Chat_ConcernedApe2"), Color.Yellow);
			}
			break;
		case "dm":
		case "pm":
		case "message":
		case "whisper":
			this.sendPrivateMessage(command);
			break;
		case "reply":
		case "r":
			this.replyPrivateMessage(command);
			break;
		case "showmethemoney":
		case "imacheat":
		case "cheat":
		case "cheats":
		case "freegold":
		case "rosebud":
			this.addMessage(Game1.content.LoadString("Strings\\UI:Chat_ConcernedApeNiceTry"), new Color(104, 214, 255));
			break;
		case "debug":
		{
			string cheatCommand;
			string error;
			if (!Program.enableCheats)
			{
				this.addMessage(Game1.content.LoadString("Strings\\UI:Chat_ConcernedApeNiceTry"), new Color(104, 214, 255));
			}
			else if (!ArgUtility.TryGetRemainder(split, 1, out cheatCommand, out error))
			{
				this.addErrorMessage("invalid usage: requires a debug command to run");
			}
			else
			{
				this.cheat(cheatCommand, isDebug: true);
			}
			break;
		}
		case "logfile":
			this.cheat("LogFile");
			break;
		case "pause":
			if (!Game1.IsMasterGame)
			{
				this.addErrorMessage(Game1.content.LoadString("Strings\\UI:Chat_HostOnlyCommand"));
				break;
			}
			Game1.netWorldState.Value.IsPaused = !Game1.netWorldState.Value.IsPaused;
			if (Game1.netWorldState.Value.IsPaused)
			{
				this.globalInfoMessage("Paused");
			}
			else
			{
				this.globalInfoMessage("Resumed");
			}
			break;
		case "resume":
			if (!Game1.IsMasterGame)
			{
				this.addErrorMessage(Game1.content.LoadString("Strings\\UI:Chat_HostOnlyCommand"));
			}
			else if (Game1.netWorldState.Value.IsPaused)
			{
				Game1.netWorldState.Value.IsPaused = false;
				this.globalInfoMessage("Resumed");
			}
			break;
		case "printdiag":
		{
			StringBuilder sb2 = new StringBuilder();
			Program.AppendDiagnostics(sb2);
			this.addInfoMessage(sb2.ToString());
			Game1.log.Info(sb2.ToString());
			break;
		}
		case "color":
			if (split.Length > 1)
			{
				Game1.player.defaultChatColor = split[1];
			}
			break;
		case "color-list":
			this.addMessage("white, red, blue, green, jade, yellowgreen, pink, purple, yellow, orange, brown, gray, cream, salmon, peach, aqua, jungle, plum", Color.White);
			break;
		case "clear":
			this.messages.Clear();
			break;
		case "list":
		case "users":
		case "players":
			this.listPlayers();
			break;
		case "help":
		case "h":
			this.showHelp();
			break;
		case "kick":
			if (Game1.IsMultiplayer && Game1.IsServer)
			{
				this.kickPlayer(command);
			}
			break;
		case "ban":
			if (Game1.IsMultiplayer && Game1.IsServer)
			{
				this.banPlayer(command);
			}
			break;
		case "unban":
			if (Game1.IsServer)
			{
				this.unbanPlayer(command);
			}
			break;
		case "unbanAll":
		case "unbanall":
			if (Game1.IsServer)
			{
				if (Game1.bannedUsers.Count == 0)
				{
					this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_BannedPlayersList_None"));
				}
				else
				{
					this.unbanAll();
				}
			}
			break;
		case "ping":
		{
			if (!Game1.IsMultiplayer)
			{
				break;
			}
			StringBuilder sb = new StringBuilder();
			if (Game1.IsServer)
			{
				foreach (KeyValuePair<long, Farmer> farmer in Game1.otherFarmers)
				{
					sb.Clear();
					sb.AppendFormat("Ping({0}) {1}ms ", farmer.Value.Name, (int)Game1.server.getPingToClient(farmer.Key));
					this.addMessage(sb.ToString(), Color.White);
				}
				break;
			}
			sb.AppendFormat("Ping: {0}ms", (int)Game1.client.GetPingToHost());
			this.addMessage(sb.ToString(), Color.White);
			break;
		}
		case "mapscreenshot":
			if (Game1.game1.CanTakeScreenshots())
			{
				int scale = 25;
				string screenshot_name = null;
				if (split.Length > 2 && !int.TryParse(split[2], out scale))
				{
					scale = 25;
				}
				if (split.Length > 1)
				{
					screenshot_name = split[1];
				}
				if (scale <= 10)
				{
					scale = 10;
				}
				string result = Game1.game1.takeMapScreenshot((float)scale / 100f, screenshot_name, null);
				if (result != null)
				{
					this.addMessage("Wrote '" + result + "'.", Color.White);
				}
				else
				{
					this.addMessage("Failed.", Color.Red);
				}
			}
			break;
		case "mbp":
		case "movepermission":
		case "movebuildingpermission":
			if (!Game1.IsMasterGame)
			{
				break;
			}
			if (split.Length > 1)
			{
				switch (split[1])
				{
				case "off":
					Game1.player.team.farmhandsCanMoveBuildings.Value = FarmerTeam.RemoteBuildingPermissions.Off;
					break;
				case "owned":
					Game1.player.team.farmhandsCanMoveBuildings.Value = FarmerTeam.RemoteBuildingPermissions.OwnedBuildings;
					break;
				case "on":
					Game1.player.team.farmhandsCanMoveBuildings.Value = FarmerTeam.RemoteBuildingPermissions.On;
					break;
				}
				this.addMessage("movebuildingpermission " + Game1.player.team.farmhandsCanMoveBuildings.Value, Color.White);
			}
			else
			{
				this.addMessage("off, owned, on", Color.White);
			}
			break;
		case "sleepannouncemode":
			if (!Game1.IsMasterGame)
			{
				break;
			}
			if (split.Length > 1)
			{
				switch (split[1])
				{
				case "all":
					Game1.player.team.sleepAnnounceMode.Value = FarmerTeam.SleepAnnounceModes.All;
					break;
				case "first":
					Game1.player.team.sleepAnnounceMode.Value = FarmerTeam.SleepAnnounceModes.First;
					break;
				case "off":
					Game1.player.team.sleepAnnounceMode.Value = FarmerTeam.SleepAnnounceModes.Off;
					break;
				}
			}
			Game1.multiplayer.globalChatInfoMessage("SleepAnnounceModeSet", TokenStringBuilder.LocalizedText($"Strings\\UI:SleepAnnounceMode_{Game1.player.team.sleepAnnounceMode.Value}"));
			break;
		case "money":
			if (Program.enableCheats)
			{
				this.cheat(command);
			}
			else
			{
				this.addMessage(Game1.content.LoadString("Strings\\UI:Chat_ConcernedApeNiceTry"), new Color(104, 214, 255));
			}
			break;
		case "recountnuts":
			Game1.game1.RecountWalnuts();
			break;
		case "fixweapons":
			SaveMigrator_1_5.ResetForges();
			this.addMessage("Reset forged weapon attributes.", Color.White);
			break;
		case "e":
		case "emote":
		{
			if (!Game1.player.CanEmote())
			{
				break;
			}
			bool valid_emote = false;
			if (split.Length > 1)
			{
				string emote_type = split[1];
				emote_type = emote_type.Substring(0, Math.Min(emote_type.Length, 16));
				emote_type.Trim();
				emote_type.ToLower();
				for (int j = 0; j < Farmer.EMOTES.Length; j++)
				{
					if (emote_type == Farmer.EMOTES[j].emoteString)
					{
						valid_emote = true;
						break;
					}
				}
				if (valid_emote)
				{
					Game1.player.netDoEmote(emote_type);
				}
			}
			if (valid_emote)
			{
				break;
			}
			string emote_list = "";
			for (int i = 0; i < Farmer.EMOTES.Length; i++)
			{
				if (!Farmer.EMOTES[i].hidden)
				{
					emote_list += Farmer.EMOTES[i].emoteString;
					if (i < Farmer.EMOTES.Length - 1)
					{
						emote_list += ", ";
					}
				}
			}
			this.addMessage(emote_list, Color.White);
			break;
		}
		default:
			if (Program.enableCheats || Game1.isRunningMacro)
			{
				this.cheat(command);
			}
			break;
		}
	}

	public virtual void cheat(string command, bool isDebug = false)
	{
		string fullCommand = (isDebug ? "debug " : "") + command;
		Game1.debugOutput = null;
		this.addInfoMessage("/" + fullCommand);
		if (!Game1.isRunningMacro)
		{
			this.cheatHistory.Insert(0, "/" + fullCommand);
		}
		if (Game1.game1.parseDebugInput(command, this.CheatCommandChatLogger))
		{
			if (!string.IsNullOrEmpty(Game1.debugOutput))
			{
				this.addInfoMessage(Game1.debugOutput);
			}
		}
		else if (!string.IsNullOrEmpty(Game1.debugOutput))
		{
			this.addErrorMessage(Game1.debugOutput);
		}
		else
		{
			this.addErrorMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:ChatBox.cs.10261") + " " + ArgUtility.SplitBySpaceAndGet(command, 0));
		}
	}

	private void replyPrivateMessage(string command)
	{
		if (!Game1.IsMultiplayer)
		{
			return;
		}
		if (this.lastReceivedPrivateMessagePlayerId == 0L)
		{
			this.addErrorMessage(Game1.content.LoadString("Strings\\UI:Chat_NoPlayerToReplyTo"));
			return;
		}
		if (!Game1.otherFarmers.TryGetValue(this.lastReceivedPrivateMessagePlayerId, out var lastPlayer) || !lastPlayer.isActive())
		{
			this.addErrorMessage(Game1.content.LoadString("Strings\\UI:Chat_CouldNotReply"));
			return;
		}
		string[] split = ArgUtility.SplitBySpace(command);
		if (split.Length <= 1)
		{
			return;
		}
		string message = "";
		for (int i = 1; i < split.Length; i++)
		{
			message += split[i];
			if (i < split.Length - 1)
			{
				message += " ";
			}
		}
		message = Program.sdk.FilterDirtyWords(message);
		Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, message, this.lastReceivedPrivateMessagePlayerId);
		this.receiveChatMessage(Game1.player.UniqueMultiplayerID, 3, LocalizedContentManager.CurrentLanguageCode, message);
	}

	private void kickPlayer(string command)
	{
		int index = 0;
		Farmer farmer = this.findMatchingFarmer(command, ref index, allowMatchingByUserName: true);
		if (farmer != null)
		{
			Game1.server.kick(farmer.UniqueMultiplayerID);
			return;
		}
		this.addErrorMessage(Game1.content.LoadString("Strings\\UI:Chat_NoPlayerWithThatName"));
		this.listPlayers(otherPlayersOnly: true);
	}

	private void banPlayer(string command)
	{
		int index = 0;
		Farmer farmer = this.findMatchingFarmer(command, ref index, allowMatchingByUserName: true);
		if (farmer != null)
		{
			string userId = Game1.server.ban(farmer.UniqueMultiplayerID);
			if (userId == null || !Game1.bannedUsers.TryGetValue(userId, out var userName))
			{
				this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_BannedPlayerFailed"));
				return;
			}
			string userDisplay = ((userName != null) ? (userName + " (" + userId + ")") : userId);
			this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_BannedPlayer", userDisplay));
		}
		else
		{
			this.addErrorMessage(Game1.content.LoadString("Strings\\UI:Chat_NoPlayerWithThatName"));
			this.listPlayers(otherPlayersOnly: true);
		}
	}

	private void unbanAll()
	{
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_UnbannedAllPlayers"));
		Game1.bannedUsers.Clear();
	}

	private void unbanPlayer(string command)
	{
		if (Game1.bannedUsers.Count == 0)
		{
			this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_BannedPlayersList_None"));
			return;
		}
		bool listUnbannablePlayers = false;
		string[] split = ArgUtility.SplitBySpace(command);
		if (split.Length > 1)
		{
			string unbanId = split[1];
			string userId = null;
			if (Game1.bannedUsers.TryGetValue(unbanId, out var userName))
			{
				userId = unbanId;
			}
			else
			{
				foreach (KeyValuePair<string, string> bannedUser2 in Game1.bannedUsers)
				{
					if (bannedUser2.Value == unbanId)
					{
						userId = bannedUser2.Key;
						userName = bannedUser2.Value;
						break;
					}
				}
			}
			if (userId != null)
			{
				string userDisplay2 = ((userName != null) ? (userName + " (" + userId + ")") : userId);
				this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_UnbannedPlayer", userDisplay2));
				Game1.bannedUsers.Remove(userId);
			}
			else
			{
				listUnbannablePlayers = true;
				this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_UnbanPlayer_NotFound"));
			}
		}
		else
		{
			listUnbannablePlayers = true;
		}
		if (!listUnbannablePlayers)
		{
			return;
		}
		this.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_BannedPlayersList"));
		foreach (KeyValuePair<string, string> bannedUser in Game1.bannedUsers)
		{
			string userDisplay = "- " + bannedUser.Key;
			if (bannedUser.Value != null)
			{
				userDisplay = "- " + bannedUser.Value + " (" + bannedUser.Key + ")";
			}
			this.addInfoMessage(userDisplay);
		}
	}

	private Farmer findMatchingFarmer(string command, ref int matchingIndex, bool allowMatchingByUserName = false)
	{
		string[] split = ArgUtility.SplitBySpace(command);
		Farmer matchingFarmer = null;
		foreach (Farmer farmer in Game1.otherFarmers.Values)
		{
			string[] farmerNameSplit = ArgUtility.SplitBySpace(farmer.displayName);
			bool isMatch = true;
			int i;
			for (i = 0; i < farmerNameSplit.Length; i++)
			{
				if (split.Length > i + 1)
				{
					if (split[i + 1].ToLowerInvariant() != farmerNameSplit[i].ToLowerInvariant())
					{
						isMatch = false;
						break;
					}
					continue;
				}
				isMatch = false;
				break;
			}
			if (isMatch)
			{
				matchingFarmer = farmer;
				matchingIndex = i;
				break;
			}
			if (!allowMatchingByUserName)
			{
				continue;
			}
			isMatch = true;
			string[] userNameSplit = ArgUtility.SplitBySpace(Game1.multiplayer.getUserName(farmer.UniqueMultiplayerID));
			for (i = 0; i < userNameSplit.Length; i++)
			{
				if (split.Length > i + 1)
				{
					if (split[i + 1].ToLowerInvariant() != userNameSplit[i].ToLowerInvariant())
					{
						isMatch = false;
						break;
					}
					continue;
				}
				isMatch = false;
				break;
			}
			if (isMatch)
			{
				matchingFarmer = farmer;
				matchingIndex = i;
				break;
			}
		}
		return matchingFarmer;
	}

	private void sendPrivateMessage(string command)
	{
		if (!Game1.IsMultiplayer)
		{
			return;
		}
		string[] split = ArgUtility.SplitBySpace(command);
		int matchingIndex = 0;
		Farmer matchingFarmer = this.findMatchingFarmer(command, ref matchingIndex);
		if (matchingFarmer == null)
		{
			this.addErrorMessage(Game1.content.LoadString("Strings\\UI:Chat_NoPlayerWithThatName"));
			return;
		}
		string message = "";
		for (int i = matchingIndex + 1; i < split.Length; i++)
		{
			message += split[i];
			if (i < split.Length - 1)
			{
				message += " ";
			}
		}
		message = Program.sdk.FilterDirtyWords(message);
		Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, message, matchingFarmer.UniqueMultiplayerID);
		this.receiveChatMessage(Game1.player.UniqueMultiplayerID, 3, LocalizedContentManager.CurrentLanguageCode, message);
	}

	public bool isActive()
	{
		return this.chatBox.Selected;
	}

	public void activate()
	{
		this.chatBox.Selected = true;
		this.setText("");
	}

	public override void clickAway()
	{
		base.clickAway();
		if (!this.choosingEmoji || !this.emojiMenu.isWithinBounds(Game1.getMouseX(), Game1.getMouseY()) || Game1.input.GetKeyboardState().IsKeyDown(Keys.Escape))
		{
			bool selected = this.chatBox.Selected;
			this.chatBox.Selected = false;
			this.choosingEmoji = false;
			this.setText("");
			this.cheatHistoryPosition = -1;
			if (selected)
			{
				Game1.oldKBState = Game1.GetKeyboardState();
			}
		}
	}

	public override bool isWithinBounds(int x, int y)
	{
		if (x - base.xPositionOnScreen >= base.width || x - base.xPositionOnScreen < 0 || y - base.yPositionOnScreen >= base.height || y - base.yPositionOnScreen < -this.getOldMessagesBoxHeight())
		{
			if (this.choosingEmoji)
			{
				return this.emojiMenu.isWithinBounds(x, y);
			}
			return false;
		}
		return true;
	}

	public virtual void setText(string text)
	{
		this.chatBox.setText(text);
	}

	public override void receiveKeyPress(Keys key)
	{
		switch (key)
		{
		case Keys.Up:
			if (this.cheatHistoryPosition < this.cheatHistory.Count - 1)
			{
				this.cheatHistoryPosition++;
				string cheat2 = this.cheatHistory[this.cheatHistoryPosition];
				this.chatBox.setText(cheat2);
			}
			break;
		case Keys.Down:
			if (this.cheatHistoryPosition > 0)
			{
				this.cheatHistoryPosition--;
				string cheat = this.cheatHistory[this.cheatHistoryPosition];
				this.chatBox.setText(cheat);
			}
			break;
		}
		if (!Game1.options.doesInputListContain(Game1.options.moveUpButton, key) && !Game1.options.doesInputListContain(Game1.options.moveRightButton, key) && !Game1.options.doesInputListContain(Game1.options.moveDownButton, key) && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
		{
			base.receiveKeyPress(key);
		}
	}

	public override bool readyToClose()
	{
		return false;
	}

	public override void receiveGamePadButton(Buttons b)
	{
	}

	public bool isHoveringOverClickable(int x, int y)
	{
		if (this.emojiMenuIcon.containsPoint(x, y) || (this.choosingEmoji && this.emojiMenu.isWithinBounds(x, y)))
		{
			return true;
		}
		return false;
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (!this.chatBox.Selected)
		{
			return;
		}
		if (this.emojiMenuIcon.containsPoint(x, y))
		{
			this.choosingEmoji = !this.choosingEmoji;
			Game1.playSound("shwip");
			this.emojiMenuIcon.scale = 4f;
			return;
		}
		if (this.choosingEmoji && this.emojiMenu.isWithinBounds(x, y))
		{
			this.emojiMenu.leftClick(x, y, this);
			return;
		}
		this.chatBox.Update();
		if (this.choosingEmoji)
		{
			this.choosingEmoji = false;
			this.emojiMenuIcon.scale = 4f;
		}
		if (this.isWithinBounds(x, y))
		{
			this.chatBox.Selected = true;
		}
	}

	public static string formattedUserName(Farmer farmer)
	{
		string name = farmer.Name;
		if (name == null || name.Trim() == "")
		{
			name = Game1.content.LoadString("Strings\\UI:Chat_PlayerJoinedNewName");
		}
		return name;
	}

	public static string formattedUserNameLong(Farmer farmer)
	{
		string name = ChatBox.formattedUserName(farmer);
		return Game1.content.LoadString("Strings\\UI:Chat_PlayerName", name, Game1.multiplayer.getUserName(farmer.UniqueMultiplayerID));
	}

	private string formatMessage(long sourceFarmer, int chatKind, string message)
	{
		string userName = Game1.content.LoadString("Strings\\UI:Chat_UnknownUserName");
		Farmer farmer;
		if (sourceFarmer == Game1.player.UniqueMultiplayerID)
		{
			farmer = Game1.player;
		}
		else if (!Game1.otherFarmers.TryGetValue(sourceFarmer, out farmer))
		{
			farmer = null;
		}
		if (farmer != null)
		{
			userName = ChatBox.formattedUserName(farmer);
		}
		return chatKind switch
		{
			0 => Game1.content.LoadString("Strings\\UI:Chat_ChatMessageFormat", userName, message), 
			2 => Game1.content.LoadString("Strings\\UI:Chat_UserNotificationMessageFormat", message), 
			3 => Game1.content.LoadString("Strings\\UI:Chat_PrivateMessageFormat", userName, message), 
			_ => Game1.content.LoadString("Strings\\UI:Chat_ErrorMessageFormat", message), 
		};
	}

	protected virtual Color messageColor(int chatKind)
	{
		return chatKind switch
		{
			0 => this.chatBox.TextColor, 
			3 => Color.DarkCyan, 
			2 => Color.Yellow, 
			_ => Color.Red, 
		};
	}

	public virtual void receiveChatMessage(long sourceFarmer, int chatKind, LocalizedContentManager.LanguageCode language, string message)
	{
		string text = this.formatMessage(sourceFarmer, chatKind, message);
		ChatMessage c = new ChatMessage();
		string s = Game1.parseText(text, this.chatBox.Font, this.chatBox.Width - 16);
		c.timeLeftToDisplay = 600;
		c.verticalSize = (int)this.chatBox.Font.MeasureString(s).Y + 4;
		c.color = this.messageColor(chatKind);
		c.language = language;
		c.parseMessageForEmoji(s);
		this.messages.Add(c);
		if (this.messages.Count > this.maxMessages)
		{
			this.messages.RemoveAt(0);
		}
		if (chatKind == 3 && sourceFarmer != Game1.player.UniqueMultiplayerID)
		{
			this.lastReceivedPrivateMessagePlayerId = sourceFarmer;
		}
	}

	public virtual void addMessage(string message, Color color)
	{
		ChatMessage c = new ChatMessage();
		string s = Game1.parseText(message, this.chatBox.Font, this.chatBox.Width - 8);
		c.timeLeftToDisplay = 600;
		c.verticalSize = (int)this.chatBox.Font.MeasureString(s).Y + 4;
		c.color = color;
		c.language = LocalizedContentManager.CurrentLanguageCode;
		c.parseMessageForEmoji(s);
		this.messages.Add(c);
		if (this.messages.Count > this.maxMessages)
		{
			this.messages.RemoveAt(0);
		}
	}

	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
	}

	public override void performHoverAction(int x, int y)
	{
		this.emojiMenuIcon.tryHover(x, y, 1f);
		this.emojiMenuIcon.tryHover(x, y, 1f);
	}

	public override void update(GameTime time)
	{
		KeyboardState keyState = Game1.input.GetKeyboardState();
		Keys[] pressedKeys = keyState.GetPressedKeys();
		foreach (Keys key in pressedKeys)
		{
			if (!this.oldKBState.IsKeyDown(key))
			{
				this.receiveKeyPress(key);
			}
		}
		this.oldKBState = keyState;
		for (int i = 0; i < this.messages.Count; i++)
		{
			if (this.messages[i].timeLeftToDisplay > 0)
			{
				this.messages[i].timeLeftToDisplay--;
			}
			if (this.messages[i].timeLeftToDisplay < 75)
			{
				this.messages[i].alpha = (float)this.messages[i].timeLeftToDisplay / 75f;
			}
		}
		if (this.chatBox.Selected)
		{
			foreach (ChatMessage message in this.messages)
			{
				message.alpha = 1f;
			}
		}
		this.emojiMenuIcon.tryHover(0, 0, 1f);
	}

	public override void receiveScrollWheelAction(int direction)
	{
		if (this.choosingEmoji)
		{
			this.emojiMenu.receiveScrollWheelAction(direction);
		}
	}

	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		this.updatePosition();
	}

	public static SpriteFont messageFont(LocalizedContentManager.LanguageCode language)
	{
		return Game1.content.Load<SpriteFont>("Fonts\\SmallFont", language);
	}

	public int getOldMessagesBoxHeight()
	{
		int heightSoFar = 20;
		for (int i = this.messages.Count - 1; i >= 0; i--)
		{
			ChatMessage message = this.messages[i];
			if (this.chatBox.Selected || message.alpha > 0.01f)
			{
				heightSoFar += message.verticalSize;
			}
		}
		return heightSoFar;
	}

	public override void draw(SpriteBatch b)
	{
		int heightSoFar = 0;
		bool drawBG = false;
		for (int j = this.messages.Count - 1; j >= 0; j--)
		{
			ChatMessage message2 = this.messages[j];
			if (this.chatBox.Selected || message2.alpha > 0.01f)
			{
				heightSoFar += message2.verticalSize;
				drawBG = true;
			}
		}
		if (drawBG)
		{
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(301, 288, 15, 15), base.xPositionOnScreen, base.yPositionOnScreen - heightSoFar - 20 + ((!this.chatBox.Selected) ? this.chatBox.Height : 0), this.chatBox.Width, heightSoFar + 20, Color.White, 4f, drawShadow: false);
		}
		heightSoFar = 0;
		for (int i = this.messages.Count - 1; i >= 0; i--)
		{
			ChatMessage message = this.messages[i];
			heightSoFar += message.verticalSize;
			message.draw(b, base.xPositionOnScreen + 12, base.yPositionOnScreen - heightSoFar - 8 + ((!this.chatBox.Selected) ? this.chatBox.Height : 0));
		}
		if (this.chatBox.Selected)
		{
			this.chatBox.Draw(b, drawShadow: false);
			this.emojiMenuIcon.draw(b, Color.White, 0.99f);
			if (this.choosingEmoji)
			{
				this.emojiMenu.draw(b);
			}
			if (this.isWithinBounds(Game1.getMouseX(), Game1.getMouseY()) && !Game1.options.hardwareCursor)
			{
				Game1.mouseCursor = (Game1.options.gamepadControls ? Game1.cursor_gamepad_pointer : Game1.cursor_default);
			}
		}
	}
}
