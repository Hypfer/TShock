﻿/*
TShock, a server mod for Terraria
Copyright (C) 2011-2013 Nyx Studios (fka. The TShock Team)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Terraria;
using TShockAPI.DB;

namespace TShockAPI
{
	public delegate void CommandDelegate(CommandArgs args);

	public class CommandArgs : EventArgs
	{
		public string Message { get; private set; }
		public TSPlayer Player { get; private set; }

		/// <summary>
		/// Parameters passed to the arguement. Does not include the command name.
		/// IE '/kick "jerk face"' will only have 1 argument
		/// </summary>
		public List<string> Parameters { get; private set; }

		public Player TPlayer
		{
			get { return Player.TPlayer; }
		}

		public CommandArgs(string message, TSPlayer ply, List<string> args)
		{
			Message = message;
			Player = ply;
			Parameters = args;
		}
	}

	public class Command
	{
		public string Name
		{
			get { return Names[0]; }
		}

		public List<string> Names { get; protected set; }
        public bool AllowServer { get; set; }
		public bool DoLog { get; set; }
		public List<string> Permissions { get; protected set; }

		private CommandDelegate commandDelegate;
		public CommandDelegate CommandDelegate
		{
			get { return commandDelegate; }
			set
			{
				if (value == null)
					throw new ArgumentNullException();

				commandDelegate = value;
			}
	 	}

		public Command(List<string> permissionsneeded, CommandDelegate cmd, params string[] names)
			: this(cmd, names)
		{
			Permissions = permissionsneeded;
		}

		public Command(string permissionneeded, CommandDelegate cmd, params string[] names)
			: this(cmd, names)
		{
			Permissions = new List<string> { permissionneeded };
		}

		public Command(CommandDelegate cmd, params string[] names)
		{
			if (cmd == null)
				throw new ArgumentNullException("cmd");
			if (names == null || names.Length < 1)
				throw new ArgumentException("names");
			Permissions = new List<string>();
			Names = new List<string>(names);
			CommandDelegate = cmd;
			AllowServer = true;
			DoLog = true;
		}

		public bool Run(string msg, TSPlayer ply, List<string> parms)
		{
			if (!CanRun(ply))
				return false;

			try
			{
				CommandDelegate(new CommandArgs(msg, ply, parms));
			}
			catch (Exception e)
			{
				ply.SendErrorMessage("Command failed, check logs for more details.");
				Log.Error(e.ToString());
			}

			return true;
		}

		public bool HasAlias(string name)
		{
			return Names.Contains(name);
		}

		public bool CanRun(TSPlayer ply)
		{
			if (Permissions == null || Permissions.Count < 1)
				return true;
			foreach (var Permission in Permissions)
			{
				if (ply.Group.HasPermission(Permission))
					return true;
			}
			return false;
		}
	}

	public static class Commands
	{
		public static List<Command> ChatCommands = new List<Command>();
		public static ReadOnlyCollection<Command> TShockCommands = new ReadOnlyCollection<Command>(new List<Command>());

		private delegate void AddChatCommand(string permission, CommandDelegate command, params string[] names);

		public static void InitCommands()
		{
			List<Command> tshockCommands = new List<Command>(100);
			Action<Command> add2 = (cmd) => 
			{
				tshockCommands.Add(cmd);
				ChatCommands.Add(cmd);
			};
			AddChatCommand add = (p, c, n) => add2(new Command(p, c, n));

			add2(new Command(AuthToken, "auth") { AllowServer = false });
			add2(new Command(Permissions.canchangepassword, PasswordUser, "password") { AllowServer = false, DoLog = false });
			add2(new Command(Permissions.canregister, RegisterUser, "register") { AllowServer = false, DoLog = false });
			add2(new Command(Permissions.user, ManageUsers, "user") { DoLog = false });
			add2(new Command(Permissions.canlogin, AttemptLogin, "login") { AllowServer = false, DoLog = false });
			add2(new Command(Permissions.buff, Buff, "buff") { AllowServer = false });
			add2(new Command(Permissions.worldspawn, SetSpawn, "setspawn") { AllowServer = false });
			add2(new Command(Permissions.grow, Grow, "grow") { AllowServer = false });
			add2(new Command(Permissions.item, Item, "item", "i") { AllowServer = false });
			add2(new Command(Permissions.home, Home, "home") { AllowServer = false });
			add2(new Command(Permissions.canpartychat, PartyChat, "p") { AllowServer = false });
			add2(new Command(Permissions.spawn, Spawn, "spawn") { AllowServer = false });
			add2(new Command(Permissions.tp, TP, "tp") { AllowServer = false });
			add2(new Command(Permissions.tphere, TPHere, "tphere") { AllowServer = false });
			add2(new Command(Permissions.tpallow, TPAllow, "tpallow") { AllowServer = false });
			add(Permissions.kick, Kick, "kick");
			add(Permissions.ban, Ban, "ban");
			add(Permissions.whitelist, Whitelist, "whitelist");
			add(Permissions.maintenance, Off, "off", "exit");
			add(Permissions.maintenance, Restart, "restart");
			add(Permissions.maintenance, OffNoSave, "off-nosave", "exit-nosave");
			add(Permissions.maintenance, CheckUpdates, "checkupdates");
		    add(Permissions.updateplugins, UpdatePlugins, "updateplugins");
			add(Permissions.causeevents, DropMeteor, "dropmeteor");
			add(Permissions.causeevents, Star, "star");
			add(Permissions.causeevents, Fullmoon, "fullmoon");
			add(Permissions.causeevents, Bloodmoon, "bloodmoon");
			add(Permissions.causeevents, Eclipse, "eclipse");
			add(Permissions.causeevents, Invade, "invade");
			add(Permissions.causeevents, Rain, "rain");
            add(Permissions.spawnboss, Eater, "eater");
            add(Permissions.spawnboss, Eye, "eye");
            add(Permissions.spawnboss, King, "king");
            add(Permissions.spawnboss, Skeletron, "skeletron");
            add(Permissions.spawnboss, WoF, "wof", "wallofflesh");
            add(Permissions.spawnboss, Twins, "twins");
            add(Permissions.spawnboss, Destroyer, "destroyer");
            add(Permissions.spawnboss, SkeletronPrime, "skeletronp", "prime");
            add(Permissions.spawnboss, Hardcore, "hardcore");
            add(Permissions.spawnmob, SpawnMob, "spawnmob", "sm");
			add(Permissions.warp, Warp, "warp");
			add(Permissions.managegroup, Group, "group");
			add(Permissions.managegroup, GroupDeprecated, "addgroup", "delgroup", "modgroup");
			add(Permissions.manageitem, ItemBan, "itemban");
			add(Permissions.manageitem, ItemBanDeprecated,
				"additem", "additemgroup", "banitem", "delitem", "delitemgroup", "listitems", "listbanneditems", "unbanitem");
            add(Permissions.manageregion, Region, "region");
            add(Permissions.manageregion, DebugRegions, "debugreg");
			add(Permissions.cfgreload, Reload, "reload");
			add(Permissions.cfgpassword, ServerPassword, "serverpassword");
			add(Permissions.worldsave, Save, "save");
			add(Permissions.worldsettle, Settle, "settle");
			add(Permissions.cfgmaxspawns, MaxSpawns, "maxspawns");
			add(Permissions.cfgspawnrate, SpawnRate, "spawnrate");
			add(Permissions.time, Time, "time");
			add(Permissions.slap, Slap, "slap");
			add(Permissions.editspawn, ToggleAntiBuild, "antibuild");
			add(Permissions.editspawn, ProtectSpawn, "protectspawn");
            add(Permissions.maintenance, GetVersion, "version");
			add(null, ListConnectedPlayers, "playing", "online", "who");
            add(null, Motd, "motd");
            add(null, Rules, "rules");
            add(null, Help, "help");
			add(Permissions.cantalkinthird, ThirdPerson, "me");
			add(Permissions.mute, Mute, "mute", "unmute");
			add(Permissions.logs, DisplayLogs, "displaylogs");
			add(Permissions.userinfo, GrabUserUserInfo, "userinfo", "ui");
			add(Permissions.authverify, AuthVerify, "auth-verify");
			add(Permissions.broadcast, Broadcast, "broadcast", "bc", "say");
			add(Permissions.whisper, Whisper, "whisper", "w", "tell");
			add(Permissions.whisper, Reply, "reply", "r");
			add(Permissions.annoy, Annoy, "annoy");
			add(Permissions.kill, Kill, "kill");
			add(Permissions.godmode, ToggleGodMode, "godmode");
			add(Permissions.butcher, Butcher, "butcher");
			add(Permissions.item, Give, "give", "g");
			add(Permissions.clearitems, ClearItems, "clear", "clearitems");
			add(Permissions.heal, Heal, "heal");
			add(Permissions.buffplayer, GBuff, "gbuff", "buffplayer");
			add(Permissions.hardmode, StartHardMode, "hardmode");
			add(Permissions.hardmode, DisableHardMode, "stophardmode", "disablehardmode");
			add(Permissions.serverinfo, ServerInfo, "stats");
			add(Permissions.worldinfo, WorldInfo, "world");
			add(Permissions.savessi, SaveSSC, "savessc");
			add(Permissions.savessi, OverrideSSC, "overridessc", "ossc");
		    add(Permissions.xmas, ForceXmas, "forcexmas");
		    add(Permissions.settempgroup, TempGroup, "tempgroup");
			add(null, Aliases, "aliases");
			add(Rests.RestPermissions.restmanage, ManageRest, "rest");
		    //add(null, TestCallbackCommand, "test");

			TShockCommands = new ReadOnlyCollection<Command>(tshockCommands);
		}

		public static bool HandleCommand(TSPlayer player, string text)
		{
			string cmdText = text.Remove(0, 1);

			var args = ParseParameters(cmdText);
			if (args.Count < 1)
				return false;

			string cmdName = args[0].ToLower();
			args.RemoveAt(0);

			if (Hooks.PlayerHooks.OnPlayerCommand(player, cmdName, cmdText, args))
				return true;

			IEnumerable<Command> cmds = ChatCommands.Where(c => c.HasAlias(cmdName));

			if (cmds.Count() == 0)
			{
				if (player.AwaitingResponse.ContainsKey(cmdName))
				{
					Action<CommandArgs> call = player.AwaitingResponse[cmdName];
					player.AwaitingResponse.Remove(cmdName);
					call(new CommandArgs(cmdText, player, args));
					return true;
				}
				player.SendErrorMessage("Invalid command entered. Type /help for a list of valid commands.");
				return true;
			}
            foreach (Command cmd in cmds)
            {
                if (!cmd.CanRun(player))
                {
                    TShock.Utils.SendLogs(string.Format("{0} tried to execute /{1}.", player.Name, cmdText), Color.PaleVioletRed, player);
                    player.SendErrorMessage("You do not have access to that command.");
                }
                else if (!cmd.AllowServer && !player.RealPlayer)
                {
                    player.SendErrorMessage("You must use this command in-game.");
                }
                else
                {
                    if (cmd.DoLog)
                        TShock.Utils.SendLogs(string.Format("{0} executed: /{1}.", player.Name, cmdText), Color.PaleVioletRed, player);
                    cmd.Run(cmdText, player, args);
                }
            }
		    return true;
		}

		/// <summary>
		/// Parses a string of parameters into a list. Handles quotes.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static List<String> ParseParameters(string str)
		{
			var ret = new List<string>();
			var sb = new StringBuilder();
			bool instr = false;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				if (instr)
				{
					if (c == '\\')
					{
						if (i + 1 >= str.Length)
							break;
						c = GetEscape(str[++i]);
					}
					else if (c == '"')
					{
						ret.Add(sb.ToString());
						sb.Clear();
						instr = false;
						continue;
					}
					sb.Append(c);
				}
				else
				{
					if (IsWhiteSpace(c))
					{
						if (sb.Length > 0)
						{
							ret.Add(sb.ToString());
							sb.Clear();
						}
					}
					else if (c == '"')
					{
						if (sb.Length > 0)
						{
							ret.Add(sb.ToString());
							sb.Clear();
						}
						instr = true;
					}
					else
					{
						sb.Append(c);
					}
				}
			}
			if (sb.Length > 0)
				ret.Add(sb.ToString());

			return ret;
		}

		private static char GetEscape(char c)
		{
			switch (c)
			{
				case '\\':
					return '\\';
				case '"':
					return '"';
				case 't':
					return '\t';
				default:
					return c;
			}
		}

		private static bool IsWhiteSpace(char c)
		{
			return c == ' ' || c == '\t' || c == '\n';
		}

        //private static void TestCallbackCommand(CommandArgs args)
        //{
        //    Action<object> a = (s) => { ((CommandArgs)s).Player.SendSuccessMessage("This is your callack"); };
        //    args.Player.AddResponse( "yes", a);
        //    args.Player.SendInfoMessage( "Type /yes to get called back." );
        //}

		#region Account commands

		private static void AttemptLogin(CommandArgs args)
		{
			if (args.Player.LoginAttempts > TShock.Config.MaximumLoginAttempts && (TShock.Config.MaximumLoginAttempts != -1))
			{
				Log.Warn(String.Format("{0} ({1}) had {2} or more invalid login attempts and was kicked automatically.",
					args.Player.IP, args.Player.Name, TShock.Config.MaximumLoginAttempts));
				TShock.Utils.Kick(args.Player, "Too many invalid login attempts.");
				return;
			}
            
			User user = TShock.Users.GetUserByName(args.Player.Name);
			string encrPass = "";

			if (args.Parameters.Count == 0)
			{
				if (Hooks.PlayerHooks.OnPlayerPreLogin(args.Player, args.Player.Name, ""))
					return;
				user = TShock.Users.GetUserByName(args.Player.Name);
			}
			else if (args.Parameters.Count == 1)
			{
				if (Hooks.PlayerHooks.OnPlayerPreLogin(args.Player, args.Player.Name, args.Parameters[0]))
					return;
				user = TShock.Users.GetUserByName(args.Player.Name);
				encrPass = TShock.Utils.HashPassword(args.Parameters[0]);
			}
			else if (args.Parameters.Count == 2 && TShock.Config.AllowLoginAnyUsername)
			{
				if (Hooks.PlayerHooks.OnPlayerPreLogin(args.Player, args.Parameters[0], args.Parameters[1]))
					return;

				user = TShock.Users.GetUserByName(args.Parameters[0]);
				encrPass = TShock.Utils.HashPassword(args.Parameters[1]);
				if (String.IsNullOrEmpty(args.Parameters[0]))
				{
					args.Player.SendErrorMessage("Bad login attempt.");
					return;
				}
			}
			else
			{
				args.Player.SendErrorMessage(String.Format("Syntax: /login{0} [password]", TShock.Config.AllowLoginAnyUsername ? " [username]" : " "));
				args.Player.SendErrorMessage("If you forgot your password, there is no way to recover it.");
				return;
			}
			try
			{
				if (user == null)
				{
					args.Player.SendErrorMessage("A user by that name does not exist.");
				}
				else if (user.Password.ToUpper() == encrPass.ToUpper() || (user.UUID == args.Player.UUID && !TShock.Config.DisableUUIDLogin))
				{
					args.Player.PlayerData = TShock.CharacterDB.GetPlayerData(args.Player, TShock.Users.GetUserID(user.Name));

					var group = TShock.Utils.GetGroup(user.Group);

					if (TShock.Config.ServerSideCharacter)
					{
						if (group.HasPermission(Permissions.bypassinventorychecks))
						{
							args.Player.IgnoreActionsForClearingTrashCan = false;
						}
						args.Player.PlayerData.RestoreCharacter(args.Player);
					}
					args.Player.LoginFailsBySsi = false;

					if (group.HasPermission(Permissions.ignorestackhackdetection))
						args.Player.IgnoreActionsForCheating = "none";

					if (group.HasPermission(Permissions.usebanneditem))
						args.Player.IgnoreActionsForDisabledArmor = "none";

					args.Player.Group = group;
				    args.Player.tempGroup = null;
					args.Player.UserAccountName = user.Name;
					args.Player.UserID = TShock.Users.GetUserID(args.Player.UserAccountName);
					args.Player.IsLoggedIn = true;
					args.Player.IgnoreActionsForInventory = "none";

					if (!args.Player.IgnoreActionsForClearingTrashCan)
					{
						args.Player.PlayerData.CopyCharacter(args.Player);
						TShock.CharacterDB.InsertPlayerData(args.Player);
					}
					args.Player.SendSuccessMessage("Authenticated as " + user.Name + " successfully.");

					Log.ConsoleInfo(args.Player.Name + " authenticated successfully as user: " + user.Name + ".");
					if ((args.Player.LoginHarassed) && (TShock.Config.RememberLeavePos))
					{
						if (TShock.RememberedPos.GetLeavePos(args.Player.Name, args.Player.IP) != Vector2.Zero)
						{
							Vector2 pos = TShock.RememberedPos.GetLeavePos(args.Player.Name, args.Player.IP);
							args.Player.Teleport((int)pos.X*16, (int)pos.Y *16 );
						}
						args.Player.LoginHarassed = false;

					}
					TShock.Users.SetUserUUID(user, args.Player.UUID);

				    Hooks.PlayerHooks.OnPlayerPostLogin(args.Player);
				}
				else
				{
					args.Player.SendErrorMessage("Incorrect password.");
					Log.Warn(args.Player.IP + " failed to authenticate as user: " + user.Name + ".");
					args.Player.LoginAttempts++;
				}
			}
			catch (Exception ex)
			{
				args.Player.SendErrorMessage("There was an error processing your request.");
				Log.Error(ex.ToString());
			}
		}

		private static void PasswordUser(CommandArgs args)
		{
			try
			{
				if (args.Player.IsLoggedIn && args.Parameters.Count == 2)
				{
					var user = TShock.Users.GetUserByName(args.Player.UserAccountName);
					string encrPass = TShock.Utils.HashPassword(args.Parameters[0]);
					if (user.Password.ToUpper() == encrPass.ToUpper())
					{
						args.Player.SendSuccessMessage("You changed your password to " + args.Parameters[1] + "!");
						TShock.Users.SetUserPassword(user, args.Parameters[1]); // SetUserPassword will hash it for you.
						Log.ConsoleInfo(args.Player.IP + " named " + args.Player.Name + " changed the password of account " + user.Name + ".");
					}
					else
					{
						args.Player.SendErrorMessage("You failed to change your password!");
						Log.ConsoleError(args.Player.IP + " named " + args.Player.Name + " failed to change password for account: " +
										 user.Name + ".");
					}
				}
				else
				{
					args.Player.SendErrorMessage("Not logged in or invalid syntax! Proper syntax: /password <oldpassword> <newpassword>");
				}
			}
			catch (UserManagerException ex)
			{
				args.Player.SendErrorMessage("Sorry, an error occured: " + ex.Message + ".");
				Log.ConsoleError("PasswordUser returned an error: " + ex);
			}
		}

		private static void RegisterUser(CommandArgs args)
		{
			try
			{
				var user = new User();

				if (args.Parameters.Count == 1)
				{
					user.Name = args.Player.Name;
					user.Password = args.Parameters[0];
				}
				else if (args.Parameters.Count == 2 && TShock.Config.AllowRegisterAnyUsername)
				{
					user.Name = args.Parameters[0];
					user.Password = args.Parameters[1];
				}
				else
				{
					args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /register <password>");
					return;
				}

				user.Group = TShock.Config.DefaultRegistrationGroupName; // FIXME -- we should get this from the DB. --Why?
				user.UUID = args.Player.UUID;

                if (TShock.Users.GetUserByName(user.Name) == null && user.Name != TSServerPlayer.AccountName) // Cheap way of checking for existance of a user
				{
					args.Player.SendSuccessMessage("Account " + user.Name + " has been registered.");
					args.Player.SendSuccessMessage("Your password is " + user.Password);
					TShock.Users.AddUser(user);
					TShock.CharacterDB.SeedInitialData(TShock.Users.GetUser(user));
					Log.ConsoleInfo(args.Player.Name + " registered an account: " + user.Name + ".");
				}
				else
				{
					args.Player.SendErrorMessage("Account " + user.Name + " has already been registered.");
					Log.ConsoleInfo(args.Player.Name + " failed to register an existing account: " + user.Name);
				}
			}
			catch (UserManagerException ex)
			{
				args.Player.SendErrorMessage("Sorry, an error occured: " + ex.Message + ".");
				Log.ConsoleError("RegisterUser returned an error: " + ex);
			}
		}

		private static void ManageUsers(CommandArgs args)
		{
			// This guy needs to be here so that people don't get exceptions when they type /user
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid user syntax. Try /user help.");
				return;
			}

			string subcmd = args.Parameters[0];

			// Add requires a username, password, and a group specified.
			if (subcmd == "add")
			{
				var user = new User();

				try
				{
					if (args.Parameters.Count == 4)
					{
						user.Name = args.Parameters[1];
						user.Password = args.Parameters[2];
						user.Group = args.Parameters[3];
							
                        args.Player.SendSuccessMessage("Account " + user.Name + " has been added to group " + user.Group + "!");
						TShock.Users.AddUser(user);
						TShock.CharacterDB.SeedInitialData(TShock.Users.GetUser(user));
						Log.ConsoleInfo(args.Player.Name + " added Account " + user.Name + " to group " + user.Group);
					}
					else
					{
						args.Player.SendErrorMessage("Invalid syntax. Try /user help.");
					}
				}
				catch (UserManagerException ex)
				{
					args.Player.SendErrorMessage(ex.Message);
					Log.ConsoleError(ex.ToString());
				}
			}
				// User deletion requires a username
			else if (subcmd == "del" && args.Parameters.Count == 2)
			{
				var user = new User();
				user.Name = args.Parameters[1];

				try
				{
					TShock.Users.RemoveUser(user);
					args.Player.SendSuccessMessage("Account removed successfully.");
					Log.ConsoleInfo(args.Player.Name + " successfully deleted account: " + args.Parameters[1] + ".");
				}
				catch (UserManagerException ex)
				{
					args.Player.SendMessage(ex.Message, Color.Red);
					Log.ConsoleError(ex.ToString());
				}
			}
				// Password changing requires a username, and a new password to set
			else if (subcmd == "password")
			{
				var user = new User();
				user.Name = args.Parameters[1];

				try
				{
					if (args.Parameters.Count == 3)
					{
						args.Player.SendSuccessMessage("Password change succeeded for " + user.Name + ".");
						TShock.Users.SetUserPassword(user, args.Parameters[2]);
						Log.ConsoleInfo(args.Player.Name + " changed the password of account " + user.Name);
					}
					else
					{
						args.Player.SendErrorMessage("Invalid user password syntax. Try /user help.");
					}
				}
				catch (UserManagerException ex)
				{
					args.Player.SendErrorMessage(ex.Message);
					Log.ConsoleError(ex.ToString());
				}
			}
				// Group changing requires a username or IP address, and a new group to set
			else if (subcmd == "group")
			{
                var user = new User();
                user.Name = args.Parameters[1];

				try
				{
					if (args.Parameters.Count == 3)
					{
						args.Player.SendSuccessMessage("Account " + user.Name + " has been changed to group " + args.Parameters[2] + "!");
						TShock.Users.SetUserGroup(user, args.Parameters[2]);
						Log.ConsoleInfo(args.Player.Name + " changed account " + user.Name + " to group " + args.Parameters[2] + ".");
					}
					else
					{
						args.Player.SendErrorMessage("Invalid user group syntax. Try /user help.");
					}
				}
				catch (UserManagerException ex)
				{
					args.Player.SendMessage(ex.Message, Color.Green);
					Log.ConsoleError(ex.ToString());
				}
			}
			else if (subcmd == "help")
			{
				args.Player.SendInfoMessage("Use command help:");
				args.Player.SendInfoMessage("/user add username password group   -- Adds a specified user");
				args.Player.SendInfoMessage("/user del username                  -- Removes a specified user");
				args.Player.SendInfoMessage("/user password username newpassword -- Changes a user's password");
				args.Player.SendInfoMessage("/user group username newgroup       -- Changes a user's group");
			}
			else
			{
				args.Player.SendErrorMessage("Invalid user syntax. Try /user help.");
			}
		}

		#endregion

		#region Stupid commands

		private static void ServerInfo(CommandArgs args)
		{
			args.Player.SendInfoMessage("Memory usage: " + Process.GetCurrentProcess().WorkingSet64);
			args.Player.SendInfoMessage("Allocated memory: " + Process.GetCurrentProcess().VirtualMemorySize64);
			args.Player.SendInfoMessage("Total processor time: " + Process.GetCurrentProcess().TotalProcessorTime);
			args.Player.SendInfoMessage("WinVer: " + Environment.OSVersion);
			args.Player.SendInfoMessage("Proc count: " + Environment.ProcessorCount);
			args.Player.SendInfoMessage("Machine name: " + Environment.MachineName);
		}

		private static void WorldInfo(CommandArgs args)
		{
			args.Player.SendInfoMessage("World name: " + Main.worldName);
			args.Player.SendInfoMessage("World size: {0}x{1}", Main.maxTilesX, Main.maxTilesY);
			args.Player.SendInfoMessage("World ID: " + Main.worldID);
		}

		#endregion

		#region Player Management Commands

		private static void GrabUserUserInfo(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /userinfo <player>");
				return;
			}

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
				return;
			}
			try
			{
				args.Player.SendSuccessMessage("IP Address: " + players[0].IP + " Logged in as: " + players[0].UserAccountName + " group: " + players[0].Group.Name);
			}
			catch (Exception)
			{
				args.Player.SendErrorMessage("Invalid player.");
			}
		}

		private static void Kick(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /kick <player> [reason]");
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendErrorMessage("Missing player name.");
				return;
			}

			string plStr = args.Parameters[0];
			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
			}
			else if (players.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
			}
			else
			{
				string reason = args.Parameters.Count > 1
									? String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))
									: "Misbehaviour.";
				if (!TShock.Utils.Kick(players[0], reason, !args.Player.RealPlayer, false, args.Player.Name))
				{
					args.Player.SendErrorMessage("You can't kick another admin!");
				}
			}
		}

		private static void Ban(CommandArgs args)
		{
			if (args.Parameters.Count == 0 || args.Parameters[0].ToLower() == "help")
			{
				args.Player.SendInfoMessage("Syntax: /ban [option] [arguments]");
				args.Player.SendInfoMessage("Options: list, listip, clear, add, addip, del, delip");
				args.Player.SendInfoMessage("Arguments: list, listip, clear [code], add [name], addip [ip], del [name], delip [name]");
				args.Player.SendInfoMessage("In addition, a reason may be provided for all new bans after the arguments.");
				return;
			}
			if (args.Parameters[0].ToLower() == "list")
			{
				#region List bans
				if (TShock.Bans.GetBans().Count == 0)
				{
					args.Player.SendErrorMessage("There are currently no players banned.");
					return;
				}

				string banString = "";
				foreach (Ban b in TShock.Bans.GetBans())
				{

					if (b.Name.Trim() == "")
					{
						continue;
					}

					if (banString.Length == 0)
					{
						banString = b.Name;
					}
					else
					{
						int length = banString.Length;
						while (length > 60)
						{
							length = length - 60;
						}
						if (length + b.Name.Length >= 60)
						{
							banString += "|, " + b.Name;
						}
						else
						{
							banString += ", " + b.Name;
						}
					}
				}

				String[] banStrings = banString.Split('|');

				if (banStrings.Length == 0)
				{
					args.Player.SendErrorMessage("There are currently no players with valid names banned.");
					return;
				}

				if (banStrings[0].Trim() == "")
				{
					args.Player.SendErrorMessage("There are currently no bans with valid names found.");
					return;
				}

				args.Player.SendInfoMessage("List of banned players:");
				foreach (string s in banStrings)
				{
					args.Player.SendInfoMessage(s);
				}
				return;
				#endregion List bans
			}

			if (args.Parameters[0].ToLower() == "listip")
			{
				#region List ip bans
				if (TShock.Bans.GetBans().Count == 0)
				{
					args.Player.SendWarningMessage("There are currently no players banned.");
					return;
				}

				string banString = "";
				foreach (Ban b in TShock.Bans.GetBans())
				{

					if (b.IP.Trim() == "")
					{
						continue;
					}

					if (banString.Length == 0)
					{
						banString = b.IP;
					}
					else
					{
						int length = banString.Length;
						while (length > 60)
						{
							length = length - 60;
						}
						if (length + b.Name.Length >= 60)
						{
							banString += "|, " + b.IP;
						}
						else
						{
							banString += ", " + b.IP;
						}
					}
				}

				String[] banStrings = banString.Split('|');

				if (banStrings.Length == 0)
				{
					args.Player.SendErrorMessage("There are currently no players with valid IPs banned.");
					return;
				}

				if (banStrings[0].Trim() == "")
				{
					args.Player.SendErrorMessage("There are currently no bans with valid IPs found.");
					return;
				}

				args.Player.SendInfoMessage("List of IP banned players:");
				foreach (string s in banStrings)
				{
					args.Player.SendInfoMessage(s);
				}
				return;
				#endregion List ip bans
			}

			if (args.Parameters.Count >= 2)
			{
				if (args.Parameters[0].ToLower() == "add")
				{
					#region Add ban
					string plStr = args.Parameters[1];
					var players = TShock.Utils.FindPlayer(plStr);
					if (players.Count == 0)
					{
						args.Player.SendErrorMessage("Invalid player!");
					}
					else if (players.Count > 1)
					{
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
					}
					else
					{
						string reason = args.Parameters.Count > 2
											? String.Join(" ", args.Parameters.GetRange(2, args.Parameters.Count - 2))
											: "Misbehavior.";
						if (!TShock.Utils.Ban(players[0], reason, !args.Player.RealPlayer, args.Player.UserAccountName))
						{
							args.Player.SendErrorMessage("You can't ban another admin!");
						}
					}
					return;
					#endregion Add ban
				}
				else if (args.Parameters[0].ToLower() == "addip")
				{
					#region Add ip ban
					string ip = args.Parameters[1];
					string reason = args.Parameters.Count > 2
										? String.Join(" ", args.Parameters.GetRange(2, args.Parameters.Count - 2))
										: "Manually added IP address ban.";
					TShock.Bans.AddBan(ip, "", "", reason, false, args.Player.UserAccountName);
					args.Player.SendSuccessMessage(ip + " banned.");
					return;
					#endregion Add ip ban
				}
				else if (args.Parameters[0].ToLower() == "delip")
				{
					#region Delete ip ban
					var ip = args.Parameters[1];
					var ban = TShock.Bans.GetBanByIp(ip);
					if (ban != null)
					{
						if (TShock.Bans.RemoveBan(ban.IP))
							args.Player.SendSuccessMessage(string.Format("Unbanned {0} ({1})!", ban.Name, ban.IP));
						else
							args.Player.SendErrorMessage(string.Format("Failed to unban {0} ({1})!", ban.Name, ban.IP));
					}
					else
					{
						args.Player.SendErrorMessage(string.Format("No bans for ip {0} exist", ip));
					}
					return;
					#endregion Delete ip ban
				}
				else if (args.Parameters[0].ToLower() == "del")
				{
					#region Delete ban
					string plStr = args.Parameters[1];
					var ban = TShock.Bans.GetBanByName(plStr, false);
					if (ban != null)
					{
						if (TShock.Bans.RemoveBan(ban.Name, true))
							args.Player.SendSuccessMessage(string.Format("Unbanned {0} ({1})!", ban.Name, ban.IP));
						else
							args.Player.SendErrorMessage(string.Format("Failed to unban {0} ({1})!", ban.Name, ban.IP));
					}
					else
					{
						args.Player.SendErrorMessage(string.Format("No bans for player {0} exist", plStr));
					}
					return;
					#endregion Delete ban
				}

				#region Clear bans
				if (args.Parameters[0].ToLower() == "clear")
				{
					if (args.Parameters.Count < 1 && ClearBansCode == -1)
					{
						ClearBansCode = new Random().Next(0, short.MaxValue);
						args.Player.SendInfoMessage("ClearBans Code: " + ClearBansCode);
						return;
					}
					if (args.Parameters.Count < 1)
					{
						args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /ban clear <code>");
						return;
					}

					int num;
					if (!int.TryParse(args.Parameters[1], out num))
					{
						args.Player.SendErrorMessage("Invalid syntax! Expected a number, didn't get one.");
						return;
					}

					if (num == ClearBansCode)
					{
						ClearBansCode = -1;
						if (TShock.Bans.ClearBans())
						{
							Log.ConsoleInfo("Bans cleared.");
							args.Player.SendSuccessMessage("Bans cleared.");
						}
						else
						{
							args.Player.SendErrorMessage("Failed to clear bans.");
						}
					}
					else
					{
						args.Player.SendErrorMessage("Incorrect clear code.");
					}
				}
				return;
				#endregion Clear bans
			}
			args.Player.SendErrorMessage("Invalid syntax or old command provided.");
			args.Player.SendErrorMessage("Type /ban help for more information.");
		}

		private static int ClearBansCode = -1;

		private static void Whitelist(CommandArgs args)
		{
			if (args.Parameters.Count == 1)
			{
				using (var tw = new StreamWriter(FileTools.WhitelistPath, true))
				{
					tw.WriteLine(args.Parameters[0]);
				}
				args.Player.SendSuccessMessage("Added " + args.Parameters[0] + " to the whitelist.");
			}
		}

		private static void DisplayLogs(CommandArgs args)
		{
			args.Player.DisplayLogs = (!args.Player.DisplayLogs);
			args.Player.SendSuccessMessage("You will " + (args.Player.DisplayLogs ? "now" : "no longer") + " receive logs.");
		}

		private static void SaveSSC(CommandArgs args)
		{
			if (TShock.Config.ServerSideCharacter)
			{
				args.Player.SendSuccessMessage("SSC has been saved.");
				foreach (TSPlayer player in TShock.Players)
				{
					if (player != null && player.IsLoggedIn && !player.IgnoreActionsForClearingTrashCan)
					{
						TShock.CharacterDB.InsertPlayerData(player);
					}
				}
			}
		}

		private static void OverrideSSC(CommandArgs args)
		{
			if (!TShock.Config.ServerSideCharacter)
			{
				args.Player.SendErrorMessage("Server Side Characters is disabled.");
				return;
			}
			if( args.Parameters.Count < 1 )
			{
				args.Player.SendErrorMessage("Correct usage: /overridessc|/ossc <player name>");
				return;
			}

			string playerNameToMatch = string.Join(" ", args.Parameters);
			var matchedPlayers = TShock.Utils.FindPlayer(playerNameToMatch);
			if( matchedPlayers.Count < 1 )
			{
				args.Player.SendErrorMessage("No players matched \"{0}\".", playerNameToMatch);
				return;
			}
			else if( matchedPlayers.Count > 1 )
			{
				TShock.Utils.SendMultipleMatchError(args.Player, matchedPlayers.Select(p => p.Name));
				return;
			}

			TSPlayer matchedPlayer = matchedPlayers[0];
			if (matchedPlayer.IsLoggedIn)
			{
				args.Player.SendErrorMessage("Player \"{0}\" is already logged in.", matchedPlayer.Name);
				return;
			}
			if (!matchedPlayer.LoginFailsBySsi)
			{
				args.Player.SendErrorMessage("Player \"{0}\" has to perform a /login attempt first.", matchedPlayer.Name);
				return;
			}
			if (matchedPlayer.IgnoreActionsForClearingTrashCan)
			{
				args.Player.SendErrorMessage("Player \"{0}\" has to reconnect first.", matchedPlayer.Name);
				return;
			}

			TShock.CharacterDB.InsertPlayerData(matchedPlayer);
			args.Player.SendSuccessMessage("SSC of player \"{0}\" has been overriden.", matchedPlayer.Name);
		}

        private static void ForceXmas(CommandArgs args)
        {
            if(args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Usage: /forcexmas [true/false]");
                args.Player.SendInfoMessage(
                    String.Format("The server is currently {0} force Christmas mode.",
                                (TShock.Config.ForceXmas ? "in" : "not in")));
                return;
            }

            if(args.Parameters[0].ToLower() == "true")
            {
                TShock.Config.ForceXmas = true;
                Main.checkXMas();
            }
            else if(args.Parameters[0].ToLower() == "false")
            {
                TShock.Config.ForceXmas = false;
                Main.checkXMas();
            }
            else
            {
                args.Player.SendErrorMessage("Usage: /forcexmas [true/false]");
                return;
            }

            args.Player.SendInfoMessage(
                    String.Format("The server is currently {0} force Christmas mode.",
                                (TShock.Config.ForceXmas ? "in" : "not in")));
        }

		private static void TempGroup(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendInfoMessage("Invalid usage");
                args.Player.SendInfoMessage("Usage: /tempgroup <username> <new group>");
                return;
            }

            List<TSPlayer> ply = TShock.Utils.FindPlayer(args.Parameters[0]);
            if(ply.Count < 1)
            {
                args.Player.SendErrorMessage(string.Format("Could not find player {0}.", args.Parameters[0]));
                return;
            }

            if (ply.Count > 1)
            {
				TShock.Utils.SendMultipleMatchError(args.Player, ply.Select(p => p.UserAccountName));
            }

            if(!TShock.Groups.GroupExists(args.Parameters[1]))
            {
                args.Player.SendErrorMessage(string.Format("Could not find group {0}", args.Parameters[1]));
                return;
            }

            Group g = TShock.Utils.GetGroup(args.Parameters[1]);

            ply[0].tempGroup = g;

            args.Player.SendSuccessMessage(string.Format("You have changed {0}'s group to {1}", ply[0].Name, g.Name));
            ply[0].SendSuccessMessage(string.Format("Your group has temporarily been changed to {0}", g.Name));
        }

		#endregion Player Management Commands

		#region Server Maintenence Commands

		private static void Broadcast(CommandArgs args)
		{
			string message = string.Join(" ", args.Parameters);

			TShock.Utils.Broadcast(
				"(Server Broadcast) " + message, 
				Convert.ToByte(TShock.Config.BroadcastRGB[0]), Convert.ToByte(TShock.Config.BroadcastRGB[1]), 
				Convert.ToByte(TShock.Config.BroadcastRGB[2]));
		}

		private static void Off(CommandArgs args)
		{

			if (TShock.Config.ServerSideCharacter)
			{
				foreach (TSPlayer player in TShock.Players)
				{
					if (player != null && player.IsLoggedIn && !player.IgnoreActionsForClearingTrashCan)
					{
						player.SaveServerCharacter();
					}
				}
			}

			string reason = ((args.Parameters.Count > 0) ? "Server shutting down: " + String.Join(" ", args.Parameters) : "Server shutting down!");
			TShock.Utils.StopServer(true, reason);
		}
		
		private static void Restart(CommandArgs args)
		{
			if (Main.runningMono)
			{
				Log.ConsoleInfo("Sorry, this command has not yet been implemented in Mono.");
			}
			else
			{
				string reason = ((args.Parameters.Count > 0) ? "Server shutting down: " + String.Join(" ", args.Parameters) : "Server shutting down!");
				TShock.Utils.RestartServer(true, reason);
			}
		}

		private static void OffNoSave(CommandArgs args)
		{
			string reason = ((args.Parameters.Count > 0) ? "Server shutting down: " + String.Join(" ", args.Parameters) : "Server shutting down!");
			TShock.Utils.StopServer(false, reason);
		}

		private static void CheckUpdates(CommandArgs args)
		{
            args.Player.SendInfoMessage("An update check has been queued.");
			ThreadPool.QueueUserWorkItem(UpdateManager.CheckUpdate);
		}

        private static void UpdatePlugins(CommandArgs args)
        {
            args.Player.SendInfoMessage("Starting plugin update process:");
            args.Player.SendInfoMessage("This may take a while, do not turn off the server!");
        }

		private static void ManageRest(CommandArgs args)
		{
			string subCommand = "help";
			if (args.Parameters.Count > 0)
				subCommand = args.Parameters[0];

			switch(subCommand.ToLower())
			{
				case "listusers":
				{
					int pageNumber;
					if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						return;

					Dictionary<string,int> restUsersTokens = new Dictionary<string,int>();
					foreach (Rests.SecureRest.TokenData tokenData in TShock.RestApi.Tokens.Values)
					{
						if (restUsersTokens.ContainsKey(tokenData.Username))
							restUsersTokens[tokenData.Username]++;
						else
							restUsersTokens.Add(tokenData.Username, 1);
					}

					List<string> restUsers = new List<string>(
						restUsersTokens.Select(ut => string.Format("{0} ({1} tokens)", ut.Key, ut.Value)));

					PaginationTools.SendPage(
						args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(restUsers), new PaginationTools.Settings {
							NothingToDisplayString = "There are currently no active REST users.",
							HeaderFormat = "Active REST Users ({0}/{1}):",
							FooterFormat = "Type /rest listusers {0} for more."
						}
					);

					break;
				}
				case "destroytokens":
				{
					TShock.RestApi.Tokens.Clear();
					args.Player.SendSuccessMessage("All REST tokens have been destroyed.");
					break;
				}
				default:
				{
					args.Player.SendInfoMessage("Available REST Sub-Commands:");
					args.Player.SendMessage("listusers - Lists all REST users and their current active tokens.", Color.White);
					args.Player.SendMessage("destroytokens - Destroys all current REST tokens.", Color.White);
					break;
				}
			}
		}

		#endregion Server Maintenence Commands

        #region Cause Events and Spawn Monsters Commands

        private static void DropMeteor(CommandArgs args)
		{
			WorldGen.spawnMeteor = false;
			WorldGen.dropMeteor();
            args.Player.SendInfoMessage("A meteor has been triggered.");
		}

		private static void Star(CommandArgs args)
		{
			int penis56 = 12;
			int penis57 = Main.rand.Next(Main.maxTilesX - 50) + 100;
			penis57 *= 0x10;
			int penis58 = Main.rand.Next((int) (Main.maxTilesY*0.05))*0x10;
			Vector2 vector = new Vector2(penis57, penis58);
			float speedX = Main.rand.Next(-100, 0x65);
			float speedY = Main.rand.Next(200) + 100;
			float penis61 = (float) Math.Sqrt(((speedX*speedX) + (speedY*speedY)));
			penis61 = (penis56)/penis61;
			speedX *= penis61;
			speedY *= penis61;
			Projectile.NewProjectile(vector.X, vector.Y, speedX, speedY, 12, 0x3e8, 10f, Main.myPlayer);
            args.Player.SendInfoMessage("An attempt has been made to spawn a star.");
		}

		private static void Fullmoon(CommandArgs args)
		{
			TSPlayer.Server.SetFullMoon(true);
			TShock.Utils.Broadcast(string.Format("{0} turned on the full moon.", args.Player.Name), Color.Green);
		}

		private static void Bloodmoon(CommandArgs args)
		{
			TSPlayer.Server.SetBloodMoon(true);
			TShock.Utils.Broadcast(string.Format("{0} turned on the blood moon.", args.Player.Name), Color.Green);
		}

		private static void Eclipse(CommandArgs args)
		{
			TSPlayer.Server.SetEclipse(true);
			TShock.Utils.Broadcast(string.Format("{0} has forced an Eclipse!", args.Player.Name), Color.Green);
		}
		
		private static void Invade(CommandArgs args)
		{
			if (Main.invasionSize <= 0)
			{
				TSPlayer.All.SendInfoMessage(string.Format("{0} has started a goblin army invasion.", args.Player.Name));
				TShock.StartInvasion();
			}
			else
			{
                TSPlayer.All.SendInfoMessage(string.Format("{0} has ended a goblin army invasion.", args.Player.Name));
				Main.invasionSize = 0;
			}
		}

        private static void StartHardMode(CommandArgs args)
        {
            if (!TShock.Config.DisableHardmode)
                WorldGen.StartHardmode();
            else
                args.Player.SendMessage("Hardmode is disabled via config.", Color.Red);
        }

        private static void DisableHardMode(CommandArgs args)
        {
            Main.hardMode = false;
            args.Player.SendMessage("Hardmode is now disabled.", Color.Green);
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void Eater(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /eater [amount]", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /eater [amount]", Color.Red);
                return;
            }
            amount = Math.Min(amount, Main.maxNPCs);
            NPC eater = TShock.Utils.GetNPCById(13);
            TSPlayer.Server.SpawnNPC(eater.type, eater.name, amount, args.Player.TileX, args.Player.TileY);
            TShock.Utils.Broadcast(string.Format("{0} has spawned eater of worlds {1} times!", args.Player.Name, amount));
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void Eye(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /eye [amount]", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /eye [amount]", Color.Red);
                return;
            }
            amount = Math.Min(amount, Main.maxNPCs);
            NPC eye = TShock.Utils.GetNPCById(4);
            TSPlayer.Server.SetTime(false, 0.0);
            TSPlayer.Server.SpawnNPC(eye.type, eye.name, amount, args.Player.TileX, args.Player.TileY);
            TShock.Utils.Broadcast(string.Format("{0} has spawned eye {1} times!", args.Player.Name, amount));
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void King(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /king [amount]", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /king [amount]", Color.Red);
                return;
            }
            amount = Math.Min(amount, Main.maxNPCs);
            NPC king = TShock.Utils.GetNPCById(50);
            TSPlayer.Server.SpawnNPC(king.type, king.name, amount, args.Player.TileX, args.Player.TileY);
            TShock.Utils.Broadcast(string.Format("{0} has spawned king slime {1} times!", args.Player.Name, amount));
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void Skeletron(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /skeletron [amount]", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /skeletron [amount]", Color.Red);
                return;
            }
            amount = Math.Min(amount, Main.maxNPCs);
            NPC skeletron = TShock.Utils.GetNPCById(35);
            TSPlayer.Server.SetTime(false, 0.0);
            TSPlayer.Server.SpawnNPC(skeletron.type, skeletron.name, amount, args.Player.TileX, args.Player.TileY);
            TShock.Utils.Broadcast(string.Format("{0} has spawned skeletron {1} times!", args.Player.Name, amount));
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void WoF(CommandArgs args)
        {
            if (Main.wof >= 0 || (args.Player.Y / 16f < (Main.maxTilesY - 205)))
            {
                args.Player.SendMessage("Can't spawn Wall of Flesh!", Color.Red);
                return;
            }
            NPC.SpawnWOF(new Vector2(args.Player.X, args.Player.Y));
            TShock.Utils.Broadcast(string.Format("{0} has spawned Wall of Flesh!", args.Player.Name));
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void Twins(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /twins [amount]", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /twins [amount]", Color.Red);
                return;
            }
            amount = Math.Min(amount, Main.maxNPCs);
            NPC retinazer = TShock.Utils.GetNPCById(125);
            NPC spaz = TShock.Utils.GetNPCById(126);
            TSPlayer.Server.SetTime(false, 0.0);
            TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, amount, args.Player.TileX, args.Player.TileY);
            TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, amount, args.Player.TileX, args.Player.TileY);
            TShock.Utils.Broadcast(string.Format("{0} has spawned the twins {1} times!", args.Player.Name, amount));
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void Destroyer(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /destroyer [amount]", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /destroyer [amount]", Color.Red);
                return;
            }
            amount = Math.Min(amount, Main.maxNPCs);
            NPC destroyer = TShock.Utils.GetNPCById(134);
            TSPlayer.Server.SetTime(false, 0.0);
            TSPlayer.Server.SpawnNPC(destroyer.type, destroyer.name, amount, args.Player.TileX, args.Player.TileY);
            TShock.Utils.Broadcast(string.Format("{0} has spawned the destroyer {1} times!", args.Player.Name, amount));
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void SkeletronPrime(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /prime [amount]", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /prime [amount]", Color.Red);
                return;
            }
            amount = Math.Min(amount, Main.maxNPCs);
            NPC prime = TShock.Utils.GetNPCById(127);
            TSPlayer.Server.SetTime(false, 0.0);
            TSPlayer.Server.SpawnNPC(prime.type, prime.name, amount, args.Player.TileX, args.Player.TileY);
            TShock.Utils.Broadcast(string.Format("{0} has spawned skeletron prime {1} times!", args.Player.Name, amount));
        }

        [Obsolete("This specific command for spawning mobs will replaced soon.")]
        private static void Hardcore(CommandArgs args) // TODO: Add all 8 bosses
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /hardcore [amount]", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /hardcore [amount]", Color.Red);
                return;
            }
            amount = Math.Min(amount, Main.maxNPCs / 4);
            NPC retinazer = TShock.Utils.GetNPCById(125);
            NPC spaz = TShock.Utils.GetNPCById(126);
            NPC destroyer = TShock.Utils.GetNPCById(134);
            NPC prime = TShock.Utils.GetNPCById(127);
            NPC eater = TShock.Utils.GetNPCById(13);
            NPC eye = TShock.Utils.GetNPCById(4);
            NPC king = TShock.Utils.GetNPCById(50);
            NPC skeletron = TShock.Utils.GetNPCById(35);
            TSPlayer.Server.SetTime(false, 0.0);
            TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, amount, args.Player.TileX, args.Player.TileY);
            TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, amount, args.Player.TileX, args.Player.TileY);
            TSPlayer.Server.SpawnNPC(destroyer.type, destroyer.name, amount, args.Player.TileX, args.Player.TileY);
            TSPlayer.Server.SpawnNPC(prime.type, prime.name, amount, args.Player.TileX, args.Player.TileY);
            TSPlayer.Server.SpawnNPC(eater.type, eater.name, amount, args.Player.TileX, args.Player.TileY);
            TSPlayer.Server.SpawnNPC(eye.type, eye.name, amount, args.Player.TileX, args.Player.TileY);
            TSPlayer.Server.SpawnNPC(king.type, king.name, amount, args.Player.TileX, args.Player.TileY);
            TSPlayer.Server.SpawnNPC(skeletron.type, skeletron.name, amount, args.Player.TileX, args.Player.TileY);
            TShock.Utils.Broadcast(string.Format("{0} has spawned all bosses {1} times!", args.Player.Name, amount));
        }

        private static void SpawnMob(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount]", Color.Red);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendMessage("Missing mob name/id", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 2 && !int.TryParse(args.Parameters[1], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount]", Color.Red);
                return;
            }

            amount = Math.Min(amount, Main.maxNPCs);

            var npcs = TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
            if (npcs.Count == 0)
            {
                args.Player.SendMessage("Invalid mob type!", Color.Red);
            }
            else if (npcs.Count > 1)
            {
				TShock.Utils.SendMultipleMatchError(args.Player, npcs.Select(n => n.name));
            }
            else
            {
                var npc = npcs[0];
                if (npc.type >= 1 && npc.type < Main.maxNPCTypes && npc.type != 113)
                //Do not allow WoF to spawn, in certain conditions may cause loops in client
                {
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, args.Player.TileX, args.Player.TileY, 50, 20);
                    TSPlayer.All.SendSuccessMessage(string.Format("{0} was spawned {1} time(s).", npc.name, amount));
                }
                else if (npc.type == 113)
                    args.Player.SendErrorMessage("Sorry, you can't spawn Wall of Flesh! Try /wof instead.");
                // Maybe perhaps do something with WorldGen.SpawnWoF?
                else
                    args.Player.SendMessage("Invalid mob type!", Color.Red);
            }
        }

		#endregion Cause Events and Spawn Monsters Commands

		#region Teleport Commands

		private static void Home(CommandArgs args)
		{
			args.Player.Spawn();
			args.Player.SendSuccessMessage("Teleported to your spawnpoint.");
		}

		private static void Spawn(CommandArgs args)
		{
			if (args.Player.Teleport(Main.spawnTileX*16, (Main.spawnTileY*16) -48))
				args.Player.SendSuccessMessage("Teleported to the map's spawnpoint.");
		}

		private static void TP(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /tp <player>");
				args.Player.SendErrorMessage("                               /tp <x> <y>");
				return;
			}

			if(args.Parameters.Count == 2)
			{
				float x, y;
				if (float.TryParse(args.Parameters[0], out x) && float.TryParse(args.Parameters[1], out y))
				{
					args.Player.Teleport(x, y);
					args.Player.SendSuccessMessage("Teleported!");
				}
			}
			else
			{
				string plStr = String.Join(" ", args.Parameters);
				var players = TShock.Utils.FindPlayer(plStr);
				if (players.Count == 0)
				{
					args.Player.SendErrorMessage("Invalid user name.");
					args.Player.SendErrorMessage("Proper syntax: /tp <player>");
					args.Player.SendErrorMessage("               /tp <x> <y>");
				}

				else if (players.Count > 1)
					TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
				else if (!players[0].TPAllow && !args.Player.Group.HasPermission(Permissions.tpall))
				{
					var plr = players[0];
					args.Player.SendErrorMessage(plr.Name + " has prevented users from teleporting to them.");
					plr.SendInfoMessage(args.Player.Name + " attempted to teleport to you.");
				}
				else
				{
					var plr = players[0];
					if (args.Player.Teleport(plr.TileX * 16, plr.TileY * 16 ))
					{
						args.Player.SendSuccessMessage(string.Format("Teleported to {0}.", plr.Name));
						if (!args.Player.Group.HasPermission(Permissions.tphide))
							plr.SendInfoMessage(args.Player.Name + " teleported to you.");
					}
				}
			}


	}

		private static void TPHere(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /tphere <player> ");
				return;
			}

			string plStr = String.Join(" ", args.Parameters);

			if (plStr == "all" || plStr == "*")
			{
				args.Player.SendInfoMessage(string.Format("You brought all players here."));
				for (int i = 0; i < Main.maxPlayers; i++)
				{
					if (Main.player[i].active && (Main.player[i] != args.TPlayer))
					{
						if (TShock.Players[i].Teleport(args.Player.TileX*16, args.Player.TileY*16 ))
							TShock.Players[i].SendSuccessMessage(string.Format("You were teleported to {0}.", args.Player.Name) + ".");
					}
				}
				return;
			}

			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
			}
			else if (players.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
			}
			else
			{
				var plr = players[0];
				if (plr.Teleport(args.Player.TileX*16, args.Player.TileY*16 ))
				{
					plr.SendInfoMessage(string.Format("You were teleported to {0}.", args.Player.Name));
					args.Player.SendSuccessMessage(string.Format("You brought {0} here.", plr.Name));
				}
			}
		}

		private static void TPAllow(CommandArgs args)
		{
			if (!args.Player.TPAllow)
				args.Player.SendSuccessMessage("You have removed your teleportation protection.");
			if (args.Player.TPAllow)
                args.Player.SendSuccessMessage("You have enabled teleportation protection.");
			args.Player.TPAllow = !args.Player.TPAllow;
		}

		private static void Warp(CommandArgs args)
		{
		    bool hasManageWarpPermission = args.Player.Group.HasPermission(Permissions.managewarp);
            if (args.Parameters.Count < 1)
            {
                if (hasManageWarpPermission)
                {
                    args.Player.SendInfoMessage("Invalid syntax! Proper syntax: /warp [command] [arguments]");
                    args.Player.SendInfoMessage("Commands: add, del, hide, list, send, [warpname]");
                    args.Player.SendInfoMessage("Arguments: add [warp name], del [warp name], list [page]");
                    args.Player.SendInfoMessage("Arguments: send [player] [warp name], hide [warp name] [Enable(true/false)]");
                    args.Player.SendInfoMessage("Examples: /warp add foobar, /warp hide foobar true, /warp foobar");
                    return;
                }
                else
                {
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /warp [name] or /warp list <page>");
                    return;
                }
            }

			if (args.Parameters[0].Equals("list"))
            {
                #region List warps
				int pageNumber;
				if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
					return;
				IEnumerable<string> warpNames = from warp in TShock.Warps.ListAllPublicWarps(Main.worldID.ToString())
												select warp.WarpName;
				PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(warpNames),
					new PaginationTools.Settings
					{
						HeaderFormat = "Warps ({0}/{1}):",
						FooterFormat = "Type /warp list {0} for more.",
						NothingToDisplayString = "There are currently no warps defined."
					});
                #endregion
            }
            else if (args.Parameters[0].ToLower() == "add" && hasManageWarpPermission)
            {
                #region Add warp
                if (args.Parameters.Count == 2)
                {
                    string warpName = args.Parameters[1];
                    if (warpName == "list" || warpName == "hide" || warpName == "del" || warpName == "add")
                    {
                        args.Player.SendErrorMessage("Name reserved, use a different name.");
                    }
                    else if (TShock.Warps.AddWarp(args.Player.TileX, args.Player.TileY, warpName, Main.worldID.ToString()))
                    {
                        args.Player.SendSuccessMessage("Warp added: " + warpName);
                    }
                    else
                    {
                        args.Player.SendErrorMessage("Warp " + warpName + " already exists.");
                    }
                }
                else
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /warp add [name]");
                #endregion
            }
            else if (args.Parameters[0].ToLower() == "del" && hasManageWarpPermission)
            {
                #region Del warp
                if (args.Parameters.Count == 2)
                {
                    string warpName = args.Parameters[1];
                    if (TShock.Warps.RemoveWarp(warpName))
                        args.Player.SendSuccessMessage("Warp deleted: " + warpName);
                    else
                        args.Player.SendErrorMessage("Could not find the specified warp.");
                }
                else
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /warp del [name]");
                #endregion
            }
            else if (args.Parameters[0].ToLower() == "hide" && hasManageWarpPermission)
            {
                #region Hide warp
                if (args.Parameters.Count == 3)
                {
                    string warpName = args.Parameters[1];
                    bool state = false;
                    if (Boolean.TryParse(args.Parameters[2], out state))
                    {
                        if (TShock.Warps.HideWarp(args.Parameters[1], state))
                        {
                            if (state)
                                args.Player.SendSuccessMessage("Warp " + warpName + " is now private.");
                            else
                                args.Player.SendSuccessMessage("Warp " + warpName + " is now public.");
                        }
                        else
                            args.Player.SendErrorMessage("Could not find specified warp.");
                    }
                    else
                        args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /warp hide [name] <true/false>");
                }
                else
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /warp hide [name] <true/false>");
                #endregion
            }
            else if (args.Parameters[0].ToLower() == "send" && args.Player.Group.HasPermission(Permissions.tphere))
            {
                #region Warp send
                if (args.Parameters.Count < 3)
                {
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /warp send [player] [warpname]");
                    return;
                }

                var foundplr = TShock.Utils.FindPlayer(args.Parameters[1]);
                if (foundplr.Count == 0)
                {
                    args.Player.SendErrorMessage("Invalid player!");
                    return;
                }
                else if (foundplr.Count > 1)
                {
					TShock.Utils.SendMultipleMatchError(args.Player, foundplr.Select(p => p.Name));
                    return;
                }
                string warpName = args.Parameters[2];
                var warp = TShock.Warps.FindWarp(warpName);
                var plr = foundplr[0];
                if (warp.WarpPos != Vector2.Zero)
                {
                    if (plr.Teleport((int)warp.WarpPos.X*16, (int)warp.WarpPos.Y*16 ))
                    {
                        plr.SendSuccessMessage(string.Format("{0} warped you to {1}.", args.Player.Name, warpName));
                        args.Player.SendSuccessMessage(string.Format("You warped {0} to {1}.", plr.Name, warpName));
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("Specified warp not found.");
                }
                #endregion
            }
            else
            {
                string warpName = String.Join(" ", args.Parameters);
                var warp = TShock.Warps.FindWarp(warpName);
                if (warp.WarpPos != Vector2.Zero)
                {
                    if (args.Player.Teleport((int)warp.WarpPos.X*16, (int)warp.WarpPos.Y*16 ))
                        args.Player.SendSuccessMessage("Warped to " + warpName + ".");
                }
                else
                {
                    args.Player.SendErrorMessage("The specified warp was not found.");
                }
            }
		}

		#endregion Teleport Commands

		#region Group Management

		private static void GroupDeprecated(CommandArgs args)
		{
			args.Player.SendInfoMessage("The group commands were merged into /group in TShock 4.1; check /group help.");
		}

		private static void Group(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendInfoMessage("Invalid syntax! Proper syntax: /group <command> [arguments]");
				args.Player.SendInfoMessage("Commands: add, addperm, del, delperm, list, listperm");
				args.Player.SendInfoMessage("Arguments: add <group name>, addperm <group name> <permissions...>, del <group name>");
				args.Player.SendInfoMessage("Arguments: delperm <group name> <permissions...>, list [page], listperm <group name> [page]");
				return;
			}

			switch (args.Parameters[0].ToLower())
			{
				case "add":
					#region Add group
					{
						if (args.Parameters.Count < 2)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /group add <group name> [permissions]");
							return;
						}

						string groupName = args.Parameters[1];
						args.Parameters.RemoveRange(0, 2);
						string permissions = String.Join(",", args.Parameters);

						try
						{
							string response = TShock.Groups.AddGroup(groupName, permissions);
							if (response.Length > 0)
							{
								args.Player.SendSuccessMessage(response);
							}
						}
						catch (GroupManagerException ex)
						{
							args.Player.SendErrorMessage(ex.ToString());
						}
					}
					#endregion
					return;
				case "addperm":
					#region Add permissions
					{
						if (args.Parameters.Count < 3)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /group addperm <group name> <permissions...>");
							return;
						}

						string groupName = args.Parameters[1];
						args.Parameters.RemoveRange(0, 2);
						if (groupName == "*")
						{
							foreach (Group g in TShock.Groups)
							{
								TShock.Groups.AddPermissions(g.Name, args.Parameters);
							}
							args.Player.SendSuccessMessage("Modified all groups.");
							return;
						}
						try
						{
							string response = TShock.Groups.AddPermissions(groupName, args.Parameters);
							if (response.Length > 0)
							{
								args.Player.SendSuccessMessage(response);
							}
							return;
						}
						catch (GroupManagerException ex)
						{
							args.Player.SendErrorMessage(ex.ToString());
						}
					}
					#endregion
					return;

				case "parent":
					#region Parent
					{
						if (args.Parameters.Count < 2)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /group parent <group name> [new parent group name]");
							return;
						}

						string groupName = args.Parameters[1];
						Group group = TShock.Groups.GetGroupByName(groupName);
						if (group == null)
						{
							args.Player.SendErrorMessage("No such group \"{0}\".", groupName);
							return;
						}

						if (args.Parameters.Count > 2)
						{
							string newParentGroupName = string.Join(" ", args.Parameters.Skip(2));
							if (!string.IsNullOrWhiteSpace(newParentGroupName) && !TShock.Groups.GroupExists(newParentGroupName))
							{
								args.Player.SendErrorMessage("No such group \"{0}\".", newParentGroupName);
								return;
							}

							try
							{
								TShock.Groups.UpdateGroup(groupName, newParentGroupName, group.Permissions, group.ChatColor);

								if (!string.IsNullOrWhiteSpace(newParentGroupName))
									args.Player.SendSuccessMessage("Parent of group \"{0}\" set to \"{1}\".", groupName, newParentGroupName);
								else
									args.Player.SendSuccessMessage("Removed parent of group \"{0}\".", groupName);
							}
							catch (GroupManagerException ex)
							{
								args.Player.SendErrorMessage(ex.Message);
							}
						}
						else
						{
							if (group.Parent != null)
								args.Player.SendSuccessMessage("Parent of \"{0}\" is \"{1}\".", group.Name, group.Parent.Name);
							else
								args.Player.SendSuccessMessage("Group \"{0}\" has no parent.", group.Name);
						}
					}
					#endregion
					return;
				case "del":
					#region Delete group
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /group del <group name>");
							return;
						}

						try
						{
							string response = TShock.Groups.DeleteGroup(args.Parameters[1]);
							if (response.Length > 0)
							{
								args.Player.SendSuccessMessage(response);
							}
						}
						catch (GroupManagerException ex)
						{
							args.Player.SendErrorMessage(ex.ToString());
						}
					}
					#endregion
					return;
				case "delperm":
					#region Delete permissions
					{
						if (args.Parameters.Count < 3)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /group delperm <group name> <permissions...>");
							return;
						}

						string groupName = args.Parameters[1];
						args.Parameters.RemoveRange(0, 2);
						if (groupName == "*")
						{
							foreach (Group g in TShock.Groups)
							{
								TShock.Groups.DeletePermissions(g.Name, args.Parameters);
							}
							args.Player.SendSuccessMessage("Modified all groups.");
							return;
						}
						try
						{
							string response = TShock.Groups.DeletePermissions(groupName, args.Parameters);
							if (response.Length > 0)
							{
								args.Player.SendSuccessMessage(response);
							}
							return;
						}
						catch (GroupManagerException ex)
						{
							args.Player.SendErrorMessage(ex.ToString());
						}
					}
					#endregion
					return;
				case "list":
					#region List groups
					{
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
							return;
						IEnumerable<string> groupNames = from grp in TShock.Groups.groups
														 select grp.Name;
						PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(groupNames),
							new PaginationTools.Settings
							{
								HeaderFormat = "Groups ({0}/{1}):",
								FooterFormat = "Type /group list {0} for more."
							});
					}
					#endregion
					return;
				case "listperm":
					#region List permissions
					{
						if (args.Parameters.Count == 1)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /group listperm <group name> [page]");
							return;
						}
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
							return;

						if (!TShock.Groups.GroupExists(args.Parameters[1]))
						{
							args.Player.SendErrorMessage("Invalid group.");
							return;
						}
						Group grp = TShock.Utils.GetGroup(args.Parameters[1]);
						List<string> permissions = grp.TotalPermissions;

						PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(permissions),
							new PaginationTools.Settings
							{
								HeaderFormat = "Permissions for " + grp.Name + " ({0}/{1}):",
								FooterFormat = "Type /group permlist " + grp.Name + " {0} for more.",
								NothingToDisplayString = "There are currently no permissions for " + grp.Name + "."
							});
					}
					#endregion
					return;
				case "help":
					args.Player.SendInfoMessage("Syntax: /group <command> [arguments]");
					args.Player.SendInfoMessage("Commands: add, addperm, parent, del, delperm, list, listperm");
					args.Player.SendInfoMessage("Arguments: add <group name>, addperm <group name> <permissions...>, del <group name>");
					args.Player.SendInfoMessage("Arguments: delperm <group name> <permissions...>, list [page], listperm <group name> [page]");
					return;
			}
		}
		#endregion Group Management

		#region Item Management

		private static void ItemBanDeprecated(CommandArgs args)
		{
			args.Player.SendInfoMessage("The item ban commands were merged into /itemban in TShock 4.1; check /itemban help.");
		}

		private static void ItemBan(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendInfoMessage("Invalid syntax! Proper syntax: /itemban <command> [arguments]");
				args.Player.SendInfoMessage("Commands: add, allow, del, disallow, list");
				args.Player.SendInfoMessage("Arguments: add <item name>, allow <item name> <group name>");
				args.Player.SendInfoMessage("Arguments: del <item name>, disallow <item name> <group name>, list [page]");
				return;
			}

			switch (args.Parameters[0].ToLower())
			{
				case "add":
					#region Add item
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /itemban add <item name>");
							return;
						}

						List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
						if (items.Count == 0)
						{
							args.Player.SendErrorMessage("Invalid item.");
						}
						else if (items.Count > 1)
						{
							TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => i.name));
						}
						else
						{
							TShock.Itembans.AddNewBan(items[0].name);
							args.Player.SendSuccessMessage("Banned " + items[0].name + ".");
						}
					}
					#endregion
					return;
				case "allow":
					#region Allow group to item
					{
						if (args.Parameters.Count != 3)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /itemban allow <item name> <group name>");
							return;
						}

						List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
						if (items.Count == 0)
						{
							args.Player.SendErrorMessage("Invalid item.");
						}
						else if (items.Count > 1)
						{
							TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => i.name));
						}
						else
						{
							if (!TShock.Groups.GroupExists(args.Parameters[2]))
							{
								args.Player.SendErrorMessage("Invalid group.");
								return;
							}

							ItemBan ban = TShock.Itembans.GetItemBanByName(items[0].name);
							if (ban == null)
							{
								args.Player.SendErrorMessage(items[0].name + " is not banned.");
								return;
							}
							if (!ban.AllowedGroups.Contains(args.Parameters[2]))
							{
								TShock.Itembans.AllowGroup(items[0].name, args.Parameters[2]);
								args.Player.SendSuccessMessage(String.Format("{0} has been allowed to use {1}.", args.Parameters[2], items[0].name));
							}
							else
							{
								args.Player.SendWarningMessage(String.Format("{0} is already allowed to use {1}.", args.Parameters[2], items[0].name));
							}
						}
					}
					#endregion
					return;
				case "del":
					#region Delete item
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /itemban del <item name>");
							return;
						}

						List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
						if (items.Count == 0)
						{
							args.Player.SendErrorMessage("Invalid item.");
						}
						else if (items.Count > 1)
						{
							TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => i.name));
						}
						else
						{
							TShock.Itembans.RemoveBan(items[0].name);
							args.Player.SendSuccessMessage("Unbanned " + items[0].name + ".");
						}
					}
					#endregion
					return;
				case "disallow":
					#region Allow group to item
					{
						if (args.Parameters.Count != 3)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /itemban disallow <item name> <group name>");
							return;
						}

						List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
						if (items.Count == 0)
						{
							args.Player.SendErrorMessage("Invalid item.");
						}
						else if (items.Count > 1)
						{
							TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => i.name));
						}
						else
						{
							if (!TShock.Groups.GroupExists(args.Parameters[2]))
							{
								args.Player.SendErrorMessage("Invalid group.");
								return;
							}

							ItemBan ban = TShock.Itembans.GetItemBanByName(items[0].name);
							if (ban == null)
							{
								args.Player.SendErrorMessage(items[0].name + " is not banned.");
								return;
							}
							if (ban.AllowedGroups.Contains(args.Parameters[2]))
							{
								TShock.Itembans.RemoveGroup(items[0].name, args.Parameters[2]);
								args.Player.SendSuccessMessage(String.Format("{0} has been disallowed to use {1}.", args.Parameters[2], items[0].name));
							}
							else
							{
								args.Player.SendWarningMessage(String.Format("{0} is already disallowed to use {1}.", args.Parameters[2], items[0].name));
							}
						}
					}
					#endregion
					return;
				case "help":
					args.Player.SendInfoMessage("Syntax: /itemban <command> [arguments]");
					args.Player.SendInfoMessage("Commands: add, allow, del, disallow, list");
					args.Player.SendInfoMessage("Arguments: add <item name>, allow <item name> <group name>");
					args.Player.SendInfoMessage("Arguments: del <item name>, disallow <item name> <group name>, list [page]");
					return;
				case "list":
					#region List items
					int pageNumber;
					if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						return;
					IEnumerable<string> itemNames = from itemBan in TShock.Itembans.ItemBans
													select itemBan.Name;
					PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(itemNames),
						new PaginationTools.Settings
						{
							HeaderFormat = "Item bans ({0}/{1}):",
							FooterFormat = "Type /itemban list {0} for more.",
							NothingToDisplayString = "There are currently no banned items."
						});
					#endregion
					return;
			}
		}
		#endregion Item Management

		#region Server Config Commands

		private static void SetSpawn(CommandArgs args)
		{
			Main.spawnTileX = args.Player.TileX + 1;
			Main.spawnTileY = args.Player.TileY + 3;
			SaveManager.Instance.SaveWorld(false);
			args.Player.SendSuccessMessage("Spawn has now been set at your location.");
		}

		private static void Reload(CommandArgs args)
		{
			TShock.Utils.Reload(args.Player);

			args.Player.SendSuccessMessage(
				"Configuration, permissions, and regions reload complete. Some changes may require a server restart.");
		}

		private static void ServerPassword(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /password \"<new password>\"");
				return;
			}
			string passwd = args.Parameters[0];
			TShock.Config.ServerPassword = passwd;
			args.Player.SendSuccessMessage(string.Format("Server password has been changed to: {0}.", passwd));
		}

		private static void Save(CommandArgs args)
		{
			SaveManager.Instance.SaveWorld(false);
			foreach (TSPlayer tsply in TShock.Players.Where(tsply => tsply != null))
			{
				tsply.SaveServerCharacter();
			}
			args.Player.SendSuccessMessage("Save succeeded.");
		}

		private static void Settle(CommandArgs args)
		{
			if (Liquid.panicMode)
			{
				args.Player.SendWarningMessage("Liquids are already settling!");
				return;
			}
			Liquid.StartPanic();
			args.Player.SendInfoMessage("Settling liquids.");
		}

		private static void MaxSpawns(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /maxspawns <maxspawns>");
				args.Player.SendErrorMessage("Proper syntax: /maxspawns show");
				args.Player.SendErrorMessage("Proper syntax: /maxspawns default");
				return;
			}

			if (args.Parameters[0] == "show")
			{
				args.Player.SendInfoMessage("Current maximum spawns is " + TShock.Config.DefaultMaximumSpawns + ".");
				return;
			}
			
			if(args.Parameters[0]=="default"){
				TShock.Config.DefaultMaximumSpawns = 5;
				NPC.defaultMaxSpawns = 5;
				TSPlayer.All.SendInfoMessage(string.Format("{0} changed the maximum spawns to 5.", args.Player.Name));
				return;
			}

			int amount = Convert.ToInt32(args.Parameters[0]);
			int.TryParse(args.Parameters[0], out amount);
			NPC.defaultMaxSpawns = amount;
			TShock.Config.DefaultMaximumSpawns = amount;
			TSPlayer.All.SendInfoMessage(string.Format("{0} changed the maximum spawns to {1}.", args.Player.Name, amount));
		}

		private static void SpawnRate(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /spawnrate <spawnrate>");
				args.Player.SendErrorMessage("/spawnrate show");
				args.Player.SendErrorMessage("/spawnrate default");
				return;
			}

			if (args.Parameters[0] == "show")
			{
				args.Player.SendInfoMessage("Current spawn rate is " + TShock.Config.DefaultSpawnRate + ".");
				return;
			}

			if (args.Parameters[0] == "default")
			{
				TShock.Config.DefaultSpawnRate = 600;
				NPC.defaultSpawnRate = 600;
				TSPlayer.All.SendInfoMessage(string.Format("{0} changed the spawn rate to 600.", args.Player.Name));
				return;
			}

			int amount = -1;
			if (!int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendWarningMessage(string.Format("Invalid spawnrate ({0})", args.Parameters[0]));
				return;
			}

			if (amount < 0)
			{
				args.Player.SendWarningMessage("Spawnrate cannot be negative!");
				return;
			}

			NPC.defaultSpawnRate = amount;
			TShock.Config.DefaultSpawnRate = amount;
			TSPlayer.All.SendInfoMessage(string.Format("{0} changed the spawn rate to {1}.", args.Player.Name, amount));
		}

		#endregion Server Config Commands

		#region Time/PvpFun Commands

		private static void Time(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /time <day/night/dusk/noon/midnight>");
				return;
			}

			switch (args.Parameters[0])
			{
				case "day":
					TSPlayer.Server.SetTime(true, 150.0);
					TSPlayer.All.SendInfoMessage(string.Format("{0} set the time to day.", args.Player.Name));
					break;
				case "night":
					TSPlayer.Server.SetTime(false, 0.0);
					TSPlayer.All.SendInfoMessage(string.Format("{0} set the time to night.", args.Player.Name));
					break;
				case "dusk":
					TSPlayer.Server.SetTime(false, 0.0);
					TSPlayer.All.SendInfoMessage(string.Format("{0} set the time to dusk.", args.Player.Name));
					break;
				case "noon":
					TSPlayer.Server.SetTime(true, 27000.0);
					TSPlayer.All.SendInfoMessage(string.Format("{0} set the time to noon.", args.Player.Name));
					break;
				case "midnight":
					TSPlayer.Server.SetTime(false, 16200.0);
					TSPlayer.All.SendInfoMessage(string.Format("{0} set the time to midnight.", args.Player.Name));
					break;
				default:
					args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /time <day/night/dusk/noon/midnight>");
					break;
			}
		}

		private static void Rain(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /rain <stop/start>");
				return;
			}

			switch (args.Parameters[0])
			{
				case "start":
					Main.StartRain();
					TSPlayer.All.SendInfoMessage(string.Format("{0} caused it to rain.", args.Player.Name));
					break;
				case "stop":
					Main.StopRain();
					TSPlayer.All.SendInfoMessage(string.Format("{0} ended the downpour.", args.Player.Name));
					break;
			}
		}
 

		private static void Slap(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /slap <player> [dmg]");
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendErrorMessage("Missing player name.");
				return;
			}

			string plStr = args.Parameters[0];
			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
			}
			else if (players.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
			}
			else
			{
				var plr = players[0];
				int damage = 5;
				if (args.Parameters.Count == 2)
				{
					int.TryParse(args.Parameters[1], out damage);
				}
				if (!args.Player.Group.HasPermission(Permissions.kill))
				{
					damage = TShock.Utils.Clamp(damage, 15, 0);
				}
				plr.DamagePlayer(damage);
				TSPlayer.All.SendSuccessMessage(string.Format("{0} slapped {1} for {2} damage.",
													 args.Player.Name, plr.Name, damage));
				Log.Info(args.Player.Name + " slapped " + plr.Name + " for " + damage + " damage.");
			}
		}

		#endregion Time/PvpFun Commands

        #region Region Commands

        private static void DebugRegions(CommandArgs args)
        {
            foreach (Region r in TShock.Regions.Regions)
            {
                args.Player.SendMessage(r.Name + ": P: " + r.DisableBuild + " X: " + r.Area.X + " Y: " + r.Area.Y + " W: " +
                                        r.Area.Width + " H: " + r.Area.Height);
                foreach (int s in r.AllowedIDs)
                {
                    args.Player.SendMessage(r.Name + ": " + s);
                }
            }
        }

        private static void Region(CommandArgs args)
        {
            string cmd = "help";
            if (args.Parameters.Count > 0)
            {
                cmd = args.Parameters[0].ToLower();
            }
            switch (cmd)
            {
                case "name":
                    {
                        {
                            args.Player.SendMessage("Hit a block to get the name of the region", Color.Yellow);
                            args.Player.AwaitingName = true;
                            args.Player.AwaitingNameParameters = args.Parameters.Skip(1).ToArray();
                        }
                        break;
                    }
                case "set":
                    {
                        int choice = 0;
                        if (args.Parameters.Count == 2 &&
                            int.TryParse(args.Parameters[1], out choice) &&
                            choice >= 1 && choice <= 2)
                        {
                            args.Player.SendMessage("Hit a block to Set Point " + choice, Color.Yellow);
                            args.Player.AwaitingTempPoint = choice;
                        }
                        else
                        {
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region set <1/2>", Color.Red);
                        }
                        break;
                    }
                case "define":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            if (!args.Player.TempPoints.Any(p => p == Point.Zero))
                            {
                                string regionName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
                                var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
                                var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
                                var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);

                                if (TShock.Regions.AddRegion(x, y, width, height, regionName, args.Player.UserAccountName,
                                                             Main.worldID.ToString()))
                                {
                                    args.Player.TempPoints[0] = Point.Zero;
                                    args.Player.TempPoints[1] = Point.Zero;
                                    args.Player.SendMessage("Set region " + regionName, Color.Yellow);
                                }
                                else
                                {
                                    args.Player.SendMessage("Region " + regionName + " already exists", Color.Red);
                                }
                            }
                            else
                            {
                                args.Player.SendMessage("Points not set up yet", Color.Red);
                            }
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region define <name>", Color.Red);
                        break;
                    }
                case "protect":
                    {
                        if (args.Parameters.Count == 3)
                        {
                            string regionName = args.Parameters[1];
                            if (args.Parameters[2].ToLower() == "true")
                            {
                                if (TShock.Regions.SetRegionState(regionName, true))
                                    args.Player.SendMessage("Protected region " + regionName, Color.Yellow);
                                else
                                    args.Player.SendMessage("Could not find specified region", Color.Red);
                            }
                            else if (args.Parameters[2].ToLower() == "false")
                            {
                                if (TShock.Regions.SetRegionState(regionName, false))
                                    args.Player.SendMessage("Unprotected region " + regionName, Color.Yellow);
                                else
                                    args.Player.SendMessage("Could not find specified region", Color.Red);
                            }
                            else
                                args.Player.SendMessage("Invalid syntax! Proper syntax: /region protect <name> <true/false>", Color.Red);
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region protect <name> <true/false>", Color.Red);
                        break;
                    }
                case "delete":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            string regionName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                            if (TShock.Regions.DeleteRegion(regionName))
                                args.Player.SendMessage("Deleted region " + regionName, Color.Yellow);
                            else
                                args.Player.SendMessage("Could not find specified region", Color.Red);
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region delete <name>", Color.Red);
                        break;
                    }
                case "clear":
                    {
                        args.Player.TempPoints[0] = Point.Zero;
                        args.Player.TempPoints[1] = Point.Zero;
                        args.Player.SendMessage("Cleared temp area", Color.Yellow);
                        args.Player.AwaitingTempPoint = 0;
                        break;
                    }
                case "allow":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            string regionName = "";

                            for (int i = 2; i < args.Parameters.Count; i++)
                            {
                                if (regionName == "")
                                {
                                    regionName = args.Parameters[2];
                                }
                                else
                                {
                                    regionName = regionName + " " + args.Parameters[i];
                                }
                            }
                            if (TShock.Users.GetUserByName(playerName) != null)
                            {
                                if (TShock.Regions.AddNewUser(regionName, playerName))
                                {
                                    args.Player.SendMessage("Added user " + playerName + " to " + regionName, Color.Yellow);
                                }
                                else
                                    args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
                            }
                            else
                            {
                                args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
                            }
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region allow <name> <region>", Color.Red);
                        break;
                    }
                case "remove":
                    if (args.Parameters.Count > 2)
                    {
                        string playerName = args.Parameters[1];
                        string regionName = "";

                        for (int i = 2; i < args.Parameters.Count; i++)
                        {
                            if (regionName == "")
                            {
                                regionName = args.Parameters[2];
                            }
                            else
                            {
                                regionName = regionName + " " + args.Parameters[i];
                            }
                        }
                        if (TShock.Users.GetUserByName(playerName) != null)
                        {
                            if (TShock.Regions.RemoveUser(regionName, playerName))
                            {
                                args.Player.SendMessage("Removed user " + playerName + " from " + regionName, Color.Yellow);
                            }
                            else
                                args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
                        }
                        else
                        {
                            args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
                        }
                    }
                    else
                        args.Player.SendMessage("Invalid syntax! Proper syntax: /region remove <name> <region>", Color.Red);
                    break;
                case "allowg":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string group = args.Parameters[1];
                            string regionName = "";

                            for (int i = 2; i < args.Parameters.Count; i++)
                            {
                                if (regionName == "")
                                {
                                    regionName = args.Parameters[2];
                                }
                                else
                                {
                                    regionName = regionName + " " + args.Parameters[i];
                                }
                            }
                            if (TShock.Groups.GroupExists(group))
                            {
                                if (TShock.Regions.AllowGroup(regionName, group))
                                {
                                    args.Player.SendMessage("Added group " + group + " to " + regionName, Color.Yellow);
                                }
                                else
                                    args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
                            }
                            else
                            {
                                args.Player.SendMessage("Group " + group + " not found", Color.Red);
                            }
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region allowg <group> <region>", Color.Red);
                        break;
                    }
                case "removeg":
                    if (args.Parameters.Count > 2)
                    {
                        string group = args.Parameters[1];
                        string regionName = "";

                        for (int i = 2; i < args.Parameters.Count; i++)
                        {
                            if (regionName == "")
                            {
                                regionName = args.Parameters[2];
                            }
                            else
                            {
                                regionName = regionName + " " + args.Parameters[i];
                            }
                        }
                        if (TShock.Groups.GroupExists(group))
                        {
                            if (TShock.Regions.RemoveGroup(regionName, group))
                            {
                                args.Player.SendMessage("Removed group " + group + " from " + regionName, Color.Yellow);
                            }
                            else
                                args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
                        }
                        else
                        {
                            args.Player.SendMessage("Group " + group + " not found", Color.Red);
                        }
                    }
                    else
                        args.Player.SendMessage("Invalid syntax! Proper syntax: /region removeg <group> <region>", Color.Red);
                    break;
				case "list":
					{
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
							return;

						IEnumerable<string> regionNames = from region in TShock.Regions.Regions
														  where region.WorldID == Main.worldID.ToString()
														  select region.Name;
						PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(regionNames),
							new PaginationTools.Settings
							{
								HeaderFormat = "Regions ({0}/{1}):",
								FooterFormat = "Type /region list {0} for more.",
								NothingToDisplayString = "There are currently no regions defined."
							});
						break;
					}
                case "info":
                    {
                        if (args.Parameters.Count == 1 || args.Parameters.Count > 4)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /region info <region> [-d] [page]");
                            break;
                        }

                        string regionName = args.Parameters[1];
                        bool displayBoundaries = args.Parameters.Skip(2).Any(
                            p => p.Equals("-d", StringComparison.InvariantCultureIgnoreCase)
                        );

                        Region region = TShock.Regions.GetRegionByName(regionName);
                        if (region == null)
                        {
                            args.Player.SendErrorMessage("Region \"{0}\" does not exist.", regionName);
                            break;
                        }

                        int pageNumberIndex = displayBoundaries ? 3 : 2;
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, pageNumberIndex, args.Player, out pageNumber))
                            break;

                        List<string> lines = new List<string>
                        {
                            string.Format("X: {0}; Y: {1}; W: {2}; H: {3}, Z: {4}", region.Area.X, region.Area.Y, region.Area.Width, region.Area.Height, region.Z),
                            string.Concat("Owner: ", region.Owner),
                            string.Concat("Protected: ", region.DisableBuild.ToString()),
                        };

                        if (region.AllowedIDs.Count > 0)
                        {
                            IEnumerable<string> sharedUsersSelector = region.AllowedIDs.Select(userId => 
                            {
                                User user = TShock.Users.GetUserByID(userId);
                                if (user != null)
                                    return user.Name;
                                else
                                    return string.Concat("{ID: ", userId, "}");
                            });
                            List<string> extraLines = PaginationTools.BuildLinesFromTerms(sharedUsersSelector.Distinct());
                            extraLines[0] = "Shared with: " + extraLines[0];
                            lines.AddRange(extraLines);
                        }
                        else
                        {
                            lines.Add("Region is not shared with any users.");
                        }

                        if (region.AllowedGroups.Count > 0)
                        {
                            List<string> extraLines = PaginationTools.BuildLinesFromTerms(region.AllowedGroups.Distinct());
                            extraLines[0] = "Shared with groups: " + extraLines[0];
                            lines.AddRange(extraLines);
                        }
                        else
                        {
                            lines.Add("Region is not shared with any groups.");
                        }

                        PaginationTools.SendPage(
                            args.Player, pageNumber, lines, new PaginationTools.Settings
                            {
                                HeaderFormat = string.Format("Information About Region \"{0}\" ({{0}}/{{1}}):", region.Name),
                                FooterFormat = string.Format("Type /region info {0} {{0}} for more information.", regionName)
                            }
                        );

                        if (displayBoundaries)
                        {
                            Rectangle regionArea = region.Area;
                            foreach (Point boundaryPoint in Utils.Instance.EnumerateRegionBoundaries(regionArea))
                            {
                                // Preferring dotted lines as those should easily be distinguishable from actual wires.
                                if ((boundaryPoint.X + boundaryPoint.Y & 1) == 0)
                                {
                                    // Could be improved by sending raw tile data to the client instead but not really 
                                    // worth the effort as chances are very low that overwriting the wire for a few 
                                    // nanoseconds will cause much trouble.
                                    Tile tile = Main.tile[boundaryPoint.X, boundaryPoint.Y];
                                    bool oldWireState = tile.wire();
	                                tile.wire(true);

                                    try {
                                        args.Player.SendTileSquare(boundaryPoint.X, boundaryPoint.Y, 1);
                                    } finally {
                                        tile.wire(oldWireState);
                                    }
                                }
                            }
                                
                            Timer boundaryHideTimer = null;
                            boundaryHideTimer = new Timer((state) => {
                                    foreach (Point boundaryPoint in Utils.Instance.EnumerateRegionBoundaries(regionArea))
                                        if ((boundaryPoint.X + boundaryPoint.Y & 1) == 0)
                                            args.Player.SendTileSquare(boundaryPoint.X, boundaryPoint.Y, 1);

                                    // ReSharper disable AccessToModifiedClosure
                                    Debug.Assert(boundaryHideTimer != null);
                                    boundaryHideTimer.Dispose();
                                    // ReSharper restore AccessToModifiedClosure
                                },
                                null, 5000, Timeout.Infinite
                            );
                        }

                        break;
                    }
                case "z":
                    {
                        if (args.Parameters.Count == 3)
                        {
                            string regionName = args.Parameters[1];
                            int z = 0;
                            if (int.TryParse(args.Parameters[2], out z))
                            {
                                if (TShock.Regions.SetZ(regionName, z))
                                    args.Player.SendMessage("Region's z is now " + z, Color.Yellow);
                                else
                                    args.Player.SendMessage("Could not find specified region", Color.Red);
                            }
                            else
                                args.Player.SendMessage("Invalid syntax! Proper syntax: /region z <name> <#>", Color.Red);
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region z <name> <#>", Color.Red);
                        break;
                    }
                case "resize":
                case "expand":
                    {
                        if (args.Parameters.Count == 4)
                        {
                            int direction;
                            switch (args.Parameters[2])
                            {
                                case "u":
                                case "up":
                                    {
                                        direction = 0;
                                        break;
                                    }
                                case "r":
                                case "right":
                                    {
                                        direction = 1;
                                        break;
                                    }
                                case "d":
                                case "down":
                                    {
                                        direction = 2;
                                        break;
                                    }
                                case "l":
                                case "left":
                                    {
                                        direction = 3;
                                        break;
                                    }
                                default:
                                    {
                                        direction = -1;
                                        break;
                                    }
                            }
                            int addAmount;
                            int.TryParse(args.Parameters[3], out addAmount);
                            if (TShock.Regions.resizeRegion(args.Parameters[1], addAmount, direction))
                            {
                                args.Player.SendMessage("Region Resized Successfully!", Color.Yellow);
                                TShock.Regions.ReloadAllRegions();
                            }
                            else
                            {
                                args.Player.SendMessage("Invalid syntax! Proper syntax: /region resize <region> <u/d/l/r> <amount>",
                                                        Color.Red);
                            }
                        }
                        else
                        {
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region resize <region> <u/d/l/r> <amount>",
                                                    Color.Red);
                        }
                        break;
                    }
                case "tp":
                    {
                        if (!args.Player.Group.HasPermission(Permissions.tp))
                        {
                          args.Player.SendErrorMessage("You don't have the necessary permission to do that.");
                          break;
                        }
                        if (args.Parameters.Count <= 1)
                        {
                          args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /region tp <region>.");
                          break;
                        }

                        string regionName = string.Join(" ", args.Parameters.Skip(1));
                        Region region = TShock.Regions.GetRegionByName(regionName);
                        if (region == null)
                        {
                          args.Player.SendErrorMessage("Region \"{0}\" does not exist.", regionName);
                          break;
                        }

                        args.Player.Teleport(region.Area.Center.X*16, region.Area.Center.Y*16 );

                        break;
                      }
                case "help":
                default:
                    {
                        int pageNumber;
                        int pageParamIndex = 0;
                        if (args.Parameters.Count > 1)
                          pageParamIndex = 1;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, pageParamIndex, args.Player, out pageNumber))
                          return;
                        
                        List<string> lines = new List<string> {
                          "set <1/2> - Sets the temporary region points.",
                          "clear - Clears the temporary region points.",
                          "define <name> - Defines the region with the given name.",
                          "delete <name> - Deletes the given region.",
                          "name [-u][-z][-p] - Shows the name of the region at the given point.",
                          "list - Lists all regions.",
                          "resize <region> <u/d/l/r> <amount> - Resizes a region.",
                          "allow <user> <region> - Allows a user to a region.",
                          "remove <user> <region> - Removes a user from a region.",
                          "allowg <group> <region> - Allows a user group to a region.",
                          "removeg <group> <region> - Removes a user group from a region.",
                          "info <region> [-d] - Displays several information about the given region.",
                          "protect <name> <true/false> - Sets whether the tiles inside the region are protected or not.",
                          "z <name> <#> - Sets the z-order of the region.",
                        };
                        if (args.Player.Group.HasPermission(Permissions.tp))
                          lines.Add("tp <region> - Teleports you to the given region's center.");

                        PaginationTools.SendPage(
                          args.Player, pageNumber, lines, 
                          new PaginationTools.Settings 
                          {
                            HeaderFormat = "Available Region Sub-Commands ({0}/{1}):",
                            FooterFormat = "Type /region {0} for more sub-commands."
                          }
                        );
                        break;
                    }
            }
        }

        #endregion Region Commands

        #region World Protection Commands

        private static void ToggleAntiBuild(CommandArgs args)
		{
			TShock.Config.DisableBuild = (TShock.Config.DisableBuild == false);
			TSPlayer.All.SendSuccessMessage(string.Format("Anti-build is now {0}.", (TShock.Config.DisableBuild ? "on" : "off")));
		}

		private static void ProtectSpawn(CommandArgs args)
		{
			TShock.Config.SpawnProtection = (TShock.Config.SpawnProtection == false);
			TSPlayer.All.SendSuccessMessage(string.Format("Spawn is now {0}.", (TShock.Config.SpawnProtection ? "protected" : "open")));
		}

		#endregion World Protection Commands

		#region General Commands

		private static void Help(CommandArgs args)
		{
			int pageNumber;
			if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
				return;
			IEnumerable<string> cmdNames = from cmd in ChatCommands
										   where cmd.CanRun(args.Player) && (cmd.Name != "auth" || TShock.AuthToken != 0)
										   select "/" + cmd.Name;
			PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cmdNames),
				new PaginationTools.Settings
				{
					HeaderFormat = "Commands ({0}/{1}):",
					FooterFormat = "Type /help {0} for more."
				});
		}

		private static void GetVersion(CommandArgs args)
		{
			args.Player.SendInfoMessage(string.Format("TShock: {0} ({1}): ({2}/{3})", TShock.VersionNum, TShock.VersionCodename,
												  TShock.Utils.ActivePlayers(), TShock.Config.MaxSlots));
		}

		private static void ListConnectedPlayers(CommandArgs args)
		{
			bool invalidUsage = (args.Parameters.Count > 2);

			bool displayIdsRequested = false;
			int pageNumber = 1;
			if (!invalidUsage) 
			{
				foreach (string parameter in args.Parameters)
				{
					if (parameter.Equals("-i", StringComparison.InvariantCultureIgnoreCase))
					{
						displayIdsRequested = true;
						continue;
					}

					if (!int.TryParse(parameter, out pageNumber))
					{
						invalidUsage = true;
						break;
					}
				}
			}
			if (invalidUsage)
			{
				args.Player.SendErrorMessage("Invalid usage, proper usage: /who [-i] [pagenumber]");
				return;
			}
			if (displayIdsRequested && !args.Player.Group.HasPermission(Permissions.seeids))
			{
				args.Player.SendErrorMessage("You don't have the required permission to list player ids.");
				return;
			}

			args.Player.SendSuccessMessage("Online Players ({0}/{1})", TShock.Utils.ActivePlayers(), TShock.Config.MaxSlots);
			PaginationTools.SendPage(
				args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(TShock.Utils.GetPlayers(displayIdsRequested)), 
				new PaginationTools.Settings 
				{
					IncludeHeader = false,
					FooterFormat = string.Format("Type /who {0}{{0}} for more.", displayIdsRequested ? "-i " : string.Empty)
				}
			);
		}

		private static void AuthToken(CommandArgs args)
		{
			if (TShock.AuthToken == 0)
			{
				args.Player.SendWarningMessage("Auth is disabled. This incident has been logged.");
				Log.Warn(args.Player.IP + " attempted to use /auth even though it's disabled.");
				return;
			}
			int givenCode = Convert.ToInt32(args.Parameters[0]);
			if (givenCode == TShock.AuthToken && args.Player.Group.Name != "superadmin")
			{
				try
				{
					args.Player.Group = TShock.Utils.GetGroup("superadmin");
					args.Player.SendInfoMessage("You are now superadmin, please do the following to finish your install:");
					args.Player.SendInfoMessage("/user add <username> <password> superadmin");
					args.Player.SendInfoMessage("Creates: <username> with the password <password> as part of the superadmin group.");
					args.Player.SendInfoMessage("Please use /login <username> <password> to login from now on.");
					args.Player.SendInfoMessage("If you understand, please /login <username> <password> now, and type /auth-verify.");
				}
				catch (UserManagerException ex)
				{
					Log.ConsoleError(ex.ToString());
					args.Player.SendErrorMessage(ex.Message);
				}
				return;
			}

			if (args.Player.Group.Name == "superadmin")
			{
				args.Player.SendInfoMessage("Please disable the auth system! If you need help, consult the forums. http://tshock.co/");
				args.Player.SendInfoMessage("This account is superadmin, please do the following to finish your install:");
				args.Player.SendInfoMessage("Please use /login <username> <password> to login from now on.");
				args.Player.SendInfoMessage("If you understand, please /login <username> <password> now, and type /auth-verify.");
				return;
			}

			args.Player.SendErrorMessage("Incorrect auth code. This incident has been logged.");
			Log.Warn(args.Player.IP + " attempted to use an incorrect auth code.");
		}

		private static void AuthVerify(CommandArgs args)
		{
			if (TShock.AuthToken == 0)
			{
				args.Player.SendWarningMessage("It appears that you have already turned off the auth token.");
				args.Player.SendWarningMessage("If this is a mistake, delete auth.lck.");
				return;
			}

			args.Player.SendSuccessMessage("Your new account has been verified, and the /auth system has been turned off.");
			args.Player.SendSuccessMessage("You can always use the /user command to manage players. Don't just delete the auth.lck.");
			args.Player.SendSuccessMessage("Thank you for using TShock! http://tshock.co/ & http://github.com/TShock/TShock");
			FileTools.CreateFile(Path.Combine(TShock.SavePath, "auth.lck"));
			File.Delete(Path.Combine(TShock.SavePath, "authcode.txt"));
			TShock.AuthToken = 0;
		}

		private static void ThirdPerson(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /me <text>");
				return;
			}
			if (args.Player.mute)
				args.Player.SendErrorMessage("You are muted.");
			else
				TSPlayer.All.SendMessage(string.Format("*{0} {1}", args.Player.Name, String.Join(" ", args.Parameters)), 205, 133, 63);
		}

		private static void PartyChat(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /p <team chat text>");
				return;
			}
			int playerTeam = args.Player.Team;

			if (args.Player.mute)
				args.Player.SendErrorMessage("You are muted.");
			else if (playerTeam != 0)
			{
				string msg = string.Format("<{0}> {1}", args.Player.Name, String.Join(" ", args.Parameters));
				foreach (TSPlayer player in TShock.Players)
				{
					if (player != null && player.Active && player.Team == playerTeam)
						player.SendMessage(msg, Main.teamColor[playerTeam].R, Main.teamColor[playerTeam].G, Main.teamColor[playerTeam].B);
				}
			}
			else
				args.Player.SendErrorMessage("You are not in a party!");
		}

		private static void Mute(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /mute <player> [reason]");
				return;
			}

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
				args.Player.SendErrorMessage("Invalid player!");
			else if (players.Count > 1)
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
			else if (players[0].Group.HasPermission(Permissions.mute))
			{
				args.Player.SendErrorMessage("You cannot mute this player.");
			}
			else if (players[0].mute)
			{
				var plr = players[0];
				plr.mute = false;
				TSPlayer.All.SendInfoMessage(String.Format("{0} has been unmuted by {1}.", plr.Name, args.Player.Name));
			}
			else
			{
				string reason = "misbehavior";
				if (args.Parameters.Count > 1)
					reason = String.Join(" ", args.Parameters.ToArray(), 1, args.Parameters.Count - 1);
				var plr = players[0];
				plr.mute = true;
				TSPlayer.All.SendInfoMessage(String.Format("{0} has been muted by {1} for {2}.", plr.Name, args.Player.Name, reason));
			}
		}

		private static void Motd(CommandArgs args)
		{
			TShock.Utils.ShowFileToUser(args.Player, "motd.txt");
		}

		private static void Rules(CommandArgs args)
		{
			TShock.Utils.ShowFileToUser(args.Player, "rules.txt");
		}

		private static void Whisper(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /whisper <player> <text>");
				return;
			}

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
			}
			else if (players.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
			}
			else if (args.Player.mute)
				args.Player.SendErrorMessage("You are muted.");
			else
			{
				var plr = players[0];
				var msg = string.Join(" ", args.Parameters.ToArray(), 1, args.Parameters.Count - 1);
				plr.SendMessage("(Whisper From)" + "<" + args.Player.Name + ">" + msg, Color.MediumPurple);
				args.Player.SendMessage("(Whisper To)" + "<" + plr.Name + ">" + msg, Color.MediumPurple);
				plr.LastWhisper = args.Player;
				args.Player.LastWhisper = plr;
			}
		}

		private static void Reply(CommandArgs args)
		{
			if (args.Player.mute)
				args.Player.SendErrorMessage("You are muted.");
			else if (args.Player.LastWhisper != null)
			{
				var msg = string.Join(" ", args.Parameters);
				args.Player.LastWhisper.SendMessage("(Whisper From)" + "<" + args.Player.Name + ">" + msg, Color.MediumPurple);
				args.Player.SendMessage("(Whisper To)" + "<" + args.Player.LastWhisper.Name + ">" + msg, Color.MediumPurple);
			}
			else
				args.Player.SendErrorMessage(
					"You haven't previously received any whispers. Please use /whisper to whisper to other people.");
		}

		private static void Annoy(CommandArgs args)
		{
			if (args.Parameters.Count != 2)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /annoy <player> <seconds to annoy>");
				return;
			}
			int annoy = 5;
			int.TryParse(args.Parameters[1], out annoy);

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
				args.Player.SendErrorMessage("Invalid player!");
			else if (players.Count > 1)
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
			else
			{
				var ply = players[0];
				args.Player.SendSuccessMessage("Annoying " + ply.Name + " for " + annoy + " seconds.");
				(new Thread(ply.Whoopie)).Start(annoy);
			}
		}

		private static void Aliases(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /aliases <command or alias>");
				return;
			}
			
			string givenCommandName = string.Join(" ", args.Parameters);
			if (string.IsNullOrWhiteSpace(givenCommandName)) {
				args.Player.SendErrorMessage("Please enter a proper command name or alias.");
				return;
			}

			string commandName;
			if (givenCommandName[0] == '/')
				commandName = givenCommandName.Substring(1);
			else
				commandName = givenCommandName;

			bool didMatch = false;
			foreach (Command matchingCommand in ChatCommands.Where(cmd => cmd.Names.IndexOf(commandName) != -1)) {
				if (matchingCommand.Names.Count > 1)
					args.Player.SendInfoMessage(
					    "Aliases of /{0}: /{1}", matchingCommand.Name, string.Join(", /", matchingCommand.Names.Skip(1)));
				else
					args.Player.SendInfoMessage("/{0} defines no aliases.", matchingCommand.Name);

				didMatch = true;
			}

			if (!didMatch)
				args.Player.SendErrorMessage("No command or command alias matching \"{0}\" found.", givenCommandName);
		}

		#endregion General Commands

		#region Cheat Commands

		private static void Kill(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /kill <player>");
				return;
			}

			string plStr = String.Join(" ", args.Parameters);
			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
			}
			else if (players.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
			}
			else
			{
				var plr = players[0];
				plr.DamagePlayer(999999);
				args.Player.SendSuccessMessage(string.Format("You just killed {0}!", plr.Name));
				plr.SendErrorMessage(string.Format("{0} just killed you!", args.Player.Name));
			}
		}

		private static void Butcher(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /butcher [killTownNPCs(true/false)]");
				return;
			}

			bool killTownNPCs = false;
			if (args.Parameters.Count == 1)
				bool.TryParse(args.Parameters[0], out killTownNPCs);

			int killcount = 0;
			for (int i = 0; i < Main.npc.Length; i++)
			{
				if (Main.npc[i].active && Main.npc[i].type != 0 && (!Main.npc[i].townNPC || killTownNPCs))
				{
					TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
					killcount++;
				}
			}
			TSPlayer.All.SendSuccessMessage(string.Format("Killed {0} NPCs.", killcount));
		}
		
		private static void Item(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /item <item name/id> [item amount] [prefix id/name]");
				return;
			}

			int amountParamIndex = -1;
			int itemAmount = 0;
			for (int i = 1; i < args.Parameters.Count; i++)
			{
				if (int.TryParse(args.Parameters[i], out itemAmount))
				{
					amountParamIndex = i;
					break;
				}
			}

			string itemNameOrId;
			if (amountParamIndex == -1)
				itemNameOrId = string.Join(" ", args.Parameters);
			else
				itemNameOrId = string.Join(" ", args.Parameters.Take(amountParamIndex));

			Item item;
			List<Item> matchedItems = TShock.Utils.GetItemByIdOrName(itemNameOrId);
			if (matchedItems.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid item type!");
				return;
			}
			else if (matchedItems.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, matchedItems.Select(i => i.name));
				return;
			}
			else
			{
				item = matchedItems[0];
			}
			if (item.type < 1 && item.type >= Main.maxItemTypes)
			{
				args.Player.SendErrorMessage("The item type {0} is invalid.", itemNameOrId);
				return;
			}

			int prefixId = 0;
			if (amountParamIndex != -1 && args.Parameters.Count > amountParamIndex + 1)
			{
				string prefixidOrName = args.Parameters[amountParamIndex + 1];
				var matchedPrefixIds = TShock.Utils.GetPrefixByIdOrName(prefixidOrName);
				if (matchedPrefixIds.Count > 1) 
				{
					TShock.Utils.SendMultipleMatchError(args.Player, matchedPrefixIds.Select(p => p.ToString()));
					return;
				}
				else if (matchedPrefixIds.Count == 0) 
				{
					args.Player.SendErrorMessage("No prefix matched \"{0}\".", prefixidOrName);
					return;
				}
				else
				{
					prefixId = matchedPrefixIds[0];
				}
			}

			if (args.Player.InventorySlotAvailable || item.name.Contains("Coin"))
			{
				if (itemAmount == 0 || itemAmount > item.maxStack)
					itemAmount = item.maxStack;

				if (args.Player.GiveItemCheck(item.type, item.name, item.width, item.height, itemAmount, prefixId))
				{
					item.prefix = (byte)prefixId;
					args.Player.SendSuccessMessage("Gave {0} {1}(s).", itemAmount, item.AffixName());
				}
				else
				{
					args.Player.SendErrorMessage("The item is banned and the config prevents you from spawning banned items.");
				}
			}
			else
			{
				args.Player.SendErrorMessage("Your inventory seems full.");
			}
		}

		private static void Give(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendErrorMessage(
					"Invalid syntax! Proper syntax: /give <item type/id> <player> [item amount] [prefix id/name]");
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendErrorMessage("Missing item name/id.");
				return;
			}
			if (args.Parameters[1].Length == 0)
			{
				args.Player.SendErrorMessage("Missing player name.");
				return;
			}
			int itemAmount = 0;
			int prefix = 0;
			var items = TShock.Utils.GetItemByIdOrName(args.Parameters[0]);
			args.Parameters.RemoveAt(0);
			string plStr = args.Parameters[0];
			args.Parameters.RemoveAt(0);
			if (args.Parameters.Count == 1)
				int.TryParse(args.Parameters[0], out itemAmount);
			else if (args.Parameters.Count == 2)
			{
				int.TryParse(args.Parameters[0], out itemAmount);
				var found = TShock.Utils.GetPrefixByIdOrName(args.Parameters[1]);
				if (found.Count == 1)
					prefix = found[0];
			}

			if (items.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid item type!");
			}
			else if (items.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => i.name));
			}
			else
			{
				var item = items[0];
				if (item.type >= 1 && item.type < Main.maxItemTypes)
				{
					var players = TShock.Utils.FindPlayer(plStr);
					if (players.Count == 0)
					{
						args.Player.SendErrorMessage("Invalid player!");
					}
					else if (players.Count > 1)
					{
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
					}
					else
					{
						var plr = players[0];
						if (plr.InventorySlotAvailable || item.name.Contains("Coin"))
						{
							if (itemAmount == 0 || itemAmount > item.maxStack)
								itemAmount = item.maxStack;
							if (plr.GiveItemCheck(item.type, item.name, item.width, item.height, itemAmount, prefix))
							{
								args.Player.SendSuccessMessage(string.Format("Gave {0} {1} {2}(s).", plr.Name, itemAmount, item.name));
								plr.SendSuccessMessage(string.Format("{0} gave you {1} {2}(s).", args.Player.Name, itemAmount, item.name));
							}
							else
							{
								args.Player.SendErrorMessage("The item is banned and the config prevents spawning banned items.");
							}
							
						}
						else
						{
							args.Player.SendErrorMessage("Player does not have free slots!");
						}
					}
				}
				else
				{
					args.Player.SendErrorMessage("Invalid item type!");
				}
			}
		}

		private static void ClearItems(CommandArgs args)
		{
			int radius = 50;
			if (args.Parameters.Count > 0)
			{
				if (args.Parameters[0].ToLower() == "all")
				{
					radius = Int32.MaxValue/16;
				}
				else
				{
					try
					{
						radius = Convert.ToInt32(args.Parameters[0]);
					}
					catch (Exception)
					{
						args.Player.SendErrorMessage(
							"Please either enter the keyword \"all\", or the block radius you wish to delete all items from.");
						return;
					}
				}
			}
			int count = 0;
			for (int i = 0; i < 200; i++)
			{
				if (
					(Math.Sqrt(Math.Pow(Main.item[i].position.X - args.Player.X, 2) +
							   Math.Pow(Main.item[i].position.Y - args.Player.Y, 2)) < radius*16) && (Main.item[i].active))
				{
					Main.item[i].active = false;
					NetMessage.SendData(0x15, -1, -1, "", i, 0f, 0f, 0f, 0);
					count++;
				}
			}
			args.Player.SendSuccessMessage("All " + count + " items within a radius of " + radius + " have been deleted.");
		}

		private static void Heal(CommandArgs args)
		{
			TSPlayer playerToHeal;
			if (args.Parameters.Count > 0)
			{
				string plStr = String.Join(" ", args.Parameters);
				var players = TShock.Utils.FindPlayer(plStr);
				if (players.Count == 0)
				{
					args.Player.SendErrorMessage("Invalid player!");
					return;
				}
				else if (players.Count > 1)
				{
					TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
					return;
				}
				else
				{
					playerToHeal = players[0];
				}
			}
			else if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You cant heal yourself!");
				return;
			}
			else
			{
				playerToHeal = args.Player;
			}

			playerToHeal.Heal();
			if (playerToHeal == args.Player)
			{
				args.Player.SendSuccessMessage("You just got healed!");
			}
			else
			{
				args.Player.SendSuccessMessage(string.Format("You just healed {0}", playerToHeal.Name));
				playerToHeal.SendSuccessMessage(string.Format("{0} just healed you!", args.Player.Name));
			}
		}

		private static void Buff(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /buff <buff id/name> [time(seconds)]");
				return;
			}
			int id = 0;
			int time = 60;
			if (!int.TryParse(args.Parameters[0], out id))
			{
				var found = TShock.Utils.GetBuffByName(args.Parameters[0]);
				if (found.Count == 0)
				{
					args.Player.SendErrorMessage("Invalid buff name!");
					return;
				}
				else if (found.Count > 1)
				{
					TShock.Utils.SendMultipleMatchError(args.Player, found.Select(f => Main.buffName[f]));
					return;
				}
				id = found[0];
			}
			if (args.Parameters.Count == 2)
				int.TryParse(args.Parameters[1], out time);
			if (id > 0 && id < Main.maxBuffs)
			{
				if (time < 0 || time > short.MaxValue)
					time = 60;
				args.Player.SetBuff(id, time*60);
				args.Player.SendSuccessMessage(string.Format("You have buffed yourself with {0}({1}) for {2} seconds!",
													  TShock.Utils.GetBuffName(id), TShock.Utils.GetBuffDescription(id), (time)));
			}
			else
				args.Player.SendErrorMessage("Invalid buff ID!");
		}

		private static void GBuff(CommandArgs args)
		{
			if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /gbuff <player> <buff id/name> [time(seconds)]");
				return;
			}
			int id = 0;
			int time = 60;
			var foundplr = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (foundplr.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (foundplr.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, foundplr.Select(p => p.Name));
				return;
			}
			else
			{
				if (!int.TryParse(args.Parameters[1], out id))
				{
					var found = TShock.Utils.GetBuffByName(args.Parameters[1]);
					if (found.Count == 0)
					{
						args.Player.SendErrorMessage("Invalid buff name!");
						return;
					}
					else if (found.Count > 1)
					{
						TShock.Utils.SendMultipleMatchError(args.Player, found.Select(b => Main.buffName[b]));
						return;
					}
					id = found[0];
				}
				if (args.Parameters.Count == 3)
					int.TryParse(args.Parameters[2], out time);
				if (id > 0 && id < Main.maxBuffs)
				{
					if (time < 0 || time > short.MaxValue)
						time = 60;
					foundplr[0].SetBuff(id, time*60);
					args.Player.SendSuccessMessage(string.Format("You have buffed {0} with {1}({2}) for {3} seconds!",
														  foundplr[0].Name, TShock.Utils.GetBuffName(id),
														  TShock.Utils.GetBuffDescription(id), (time)));
					foundplr[0].SendSuccessMessage(string.Format("{0} has buffed you with {1}({2}) for {3} seconds!",
														  args.Player.Name, TShock.Utils.GetBuffName(id),
														  TShock.Utils.GetBuffDescription(id), (time)));
				}
				else
					args.Player.SendErrorMessage("Invalid buff ID!");
			}
		}

		private static void Grow(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /grow [tree/epictree/mushroom/cactus/herb]");
				return;
			}
			var name = "Fail";
			var x = args.Player.TileX;
			var y = args.Player.TileY + 3;
			switch (args.Parameters[0].ToLower())
			{
				case "tree":
					for (int i = x - 1; i < x + 2; i++)
					{
						Main.tile[i, y].active(true);
						Main.tile[i, y].type = 2;
						Main.tile[i, y].wall = 0;
					}
					Main.tile[x, y - 1].wall = 0;
					WorldGen.GrowTree(x, y);
					name = "Tree";
					break;
				case "epictree":
					for (int i = x - 1; i < x + 2; i++)
					{
						Main.tile[i, y].active(true);
						Main.tile[i, y].type = 2;
						Main.tile[i, y].wall = 0;
					}
					Main.tile[x, y - 1].wall = 0;
					Main.tile[x, y - 1].liquid = 0;
					Main.tile[x, y - 1].active(true);
					WorldGen.GrowEpicTree(x, y);
					name = "Epic Tree";
					break;
				case "mushroom":
					for (int i = x - 1; i < x + 2; i++)
					{
						Main.tile[i, y].active(true);
						Main.tile[i, y].type = 70;
						Main.tile[i, y].wall = 0;
					}
					Main.tile[x, y - 1].wall = 0;
					WorldGen.GrowShroom(x, y);
					name = "Mushroom";
					break;
				case "cactus":
					Main.tile[x, y].type = 53;
					WorldGen.GrowCactus(x, y);
					name = "Cactus";
					break;
				case "herb":
					Main.tile[x, y].active(true);
					Main.tile[x, y].frameX = 36;
					Main.tile[x, y].type = 83;
					WorldGen.GrowAlch(x, y);
					name = "Herb";
					break;
				default:
					args.Player.SendErrorMessage("Unknown plant!");
					return;
			}
			args.Player.SendTileSquare(x, y);
			args.Player.SendSuccessMessage("Tried to grow a " + name + ".");
		}

		private static void ToggleGodMode(CommandArgs args)
		{
			TSPlayer playerToGod;
			if (args.Parameters.Count > 0)
			{
				string plStr = String.Join(" ", args.Parameters);
				var players = TShock.Utils.FindPlayer(plStr);
				if (players.Count == 0)
				{
					args.Player.SendErrorMessage("Invalid player!");
					return;
				}
				else if (players.Count > 1)
				{
					TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
					return;
				}
				else
				{
					playerToGod = players[0];
				}
			}
			else if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You cant god mode a non player!");
				return;
			}
			else
			{
				playerToGod = args.Player;
			}

			playerToGod.GodMode = !playerToGod.GodMode;

			if (playerToGod == args.Player)
			{
				args.Player.SendSuccessMessage(string.Format("You are {0} in god mode.", args.Player.GodMode ? "now" : "no longer"));
			}
			else
			{
				args.Player.SendSuccessMessage(string.Format("{0} is {1} in god mode.", playerToGod.Name, playerToGod.GodMode ? "now" : "no longer"));
				playerToGod.SendSuccessMessage(string.Format("You are {0} in god mode.", playerToGod.GodMode ? "now" : "no longer"));
			}
		}

		#endregion Cheat Comamnds
	}
}
