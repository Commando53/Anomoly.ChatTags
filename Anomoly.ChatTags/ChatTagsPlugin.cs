using Anomoly.ChatTags.Helpers;
using Anomoly.ChatTags.Models;
using Anomoly.ChatTags.Services;
using do53Mute;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Core.Steam;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static UnityEngine.Scripting.GarbageCollector;
using Logger = Rocket.Core.Logging.Logger;

namespace Anomoly.ChatTags
{
    public class ChatTagsPlugin: RocketPlugin<ChatTagsConfiguration>
    {
        public static ChatTagsPlugin Instance { get; private set; }

        private Dictionary<string, string> playerAvatars;
        private ChatFormatService formatService;

        protected override void Load()
        {
            Instance = this;

            playerAvatars = new Dictionary<string, string>();
            formatService = new ChatFormatService();

            foreach (SteamPlayer client in Provider.clients)
            {
                UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(client);

                ThreadHelper.RunAsynchronously(() =>
                {
                    Profile profile = player.SteamProfile;

                    ThreadHelper.RunSynchronously(() =>
                    {
                        playerAvatars.Add(player.Id, profile.AvatarIcon.ToString());
                    });
                });
            }

            UnturnedPlayerEvents.OnPlayerChatted += UnturnedPlayerEvents_OnPlayerChatted;
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;


            Logger.Log($"{string.Format("ChatTags v{0}", Assembly.GetName().Version.ToString())} by Anomoly has loaded");
            Logger.Log("Need support? Join my Discord @ https://discord.gg/rVH9e7Kj9y");
        }

        protected override void Unload()
        {
            Instance = null;
            formatService = null;

            UnturnedPlayerEvents.OnPlayerChatted -= UnturnedPlayerEvents_OnPlayerChatted;
            U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= Events_OnPlayerDisconnected;
            
            playerAvatars.Clear();
            playerAvatars = null;

            Logger.Log($"{string.Format("ChatTags v{0}", Assembly.GetName().Version.ToString())} by Anomoly has unloaded");
            Logger.Log("Need support? Join my Discord @ https://discord.gg/rVH9e7Kj9y");
        }

        #region Events
        private void UnturnedPlayerEvents_OnPlayerChatted(UnturnedPlayer player, ref UnityEngine.Color color, string message, SDG.Unturned.EChatMode chatMode, ref bool cancel)
        {
            if (message.StartsWith("/") || cancel)
                return;
            cancel = true;
            if (do53MutePlugin.Instance.Mutes.Contains(do53MutePlugin.Instance.Mutes.Find(x => x.steamid64 == player.SteamPlayer().playerID.steamID.m_SteamID)))
            {
                if (chatMode == EChatMode.GLOBAL && do53MutePlugin.Instance.Configuration.Instance.MuteGlobalChat || chatMode == EChatMode.LOCAL && do53MutePlugin.Instance.Configuration.Instance.MuteAreaChat || chatMode == EChatMode.GROUP && do53MutePlugin.Instance.Configuration.Instance.MuteGroupChat)
                {
                    return;
                }
            }
            UnityEngine.Color msgColor = color;
            if (!string.IsNullOrEmpty(Configuration.Instance.BaseColor))
                msgColor = UnturnedChat.GetColorFromName(Configuration.Instance.BaseColor, color);

            ChatFormat format = formatService.GetPlayerFormat(player);
            string formattedMsg = formatService.Format(player, format, chatMode, message);

            bool useRichText = true;
            if (format != null)
                useRichText = format.UseRichText;

            string avatar = null;
            if(playerAvatars.ContainsKey(player.Id))
                   avatar = playerAvatars[player.Id];
            
            if (chatMode == EChatMode.LOCAL)
            {
                float areaRange = 16384f;
                List<Player> playersInRange;
                playersInRange = new List<Player>();
                PlayerTool.getPlayersInRadius(player.Position, areaRange, playersInRange);

                foreach (Player playerInRange in playersInRange)
                {
                    SteamPlayer client = playerInRange.channel.owner;
                    ChatManager.serverSendMessage(formattedMsg, msgColor, player.SteamPlayer(), client, chatMode, avatar, useRichText);
                }
            } else if (chatMode == EChatMode.GROUP)
            {
                foreach (Player serverPlayer in PlayerTool.EnumeratePlayers())
                {
                    if (serverPlayer.quests.isMemberOfSameGroupAs(player.Player))
                    {
                        SteamPlayer client = serverPlayer.channel.owner;
                        ChatManager.serverSendMessage(formattedMsg, msgColor, player.SteamPlayer(), client, chatMode, avatar, useRichText);
                    }
                }
            } else
            {
                ChatManager.serverSendMessage(formattedMsg, msgColor, player.SteamPlayer(), null, chatMode, avatar, useRichText);
            }
        }

        private void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            // Download the player Steam profile asynchronously, because it blocks the main thread
            ThreadHelper.RunAsynchronously(() =>
            {
                Profile profile = player.SteamProfile;

                // callback
                ThreadHelper.RunSynchronously(() =>
                {
                    if (playerAvatars.ContainsKey(player.Id))
                    {
                        playerAvatars[player.Id] = profile.AvatarIcon.ToString();
                    }
                    else
                    {
                        playerAvatars.Add(player.Id, profile.AvatarIcon.ToString());
                    }
                });
            });            
        }

        private void Events_OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (playerAvatars.ContainsKey(player.Id))
            {
                playerAvatars.Remove(player.Id);
            }                
        }

        #endregion


        public List<ChatTag> GetPlayerTags(UnturnedPlayer player)
        {

            if (Configuration.Instance.UsePriorityMode)
            {
                var tags = Configuration.Instance.ChatTags;
                var group = R.Permissions.GetGroups(player, true).FirstOrDefault(g => g.Permissions.Any(p => tags.Any(t => t.Permission.Equals(p.Name))));

                var tag = group?.Permissions.Select(p => tags.FirstOrDefault(t => t.Permission.Equals(p.Name))).FirstOrDefault(t => t != null);
                return [tag];
            }
            else
            {
                List<Permission> permissions = R.Permissions.GetPermissions(player);

                return Configuration.Instance.ChatTags.Where(x => permissions.Any(p => p.Name.Equals(x.Permission))).ToList();
            }
        }
    }
}
