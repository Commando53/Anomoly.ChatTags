using Anomoly.ChatTags.Models;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Anomoly.ChatTags.Services
{
    public class ChatFormatService
    {
        private ChatTagsPlugin pluginInstance => ChatTagsPlugin.Instance;
        private ChatTagsConfiguration configuration => pluginInstance.Configuration.Instance;

        public string Format(UnturnedPlayer player, ChatFormat format, EChatMode mode, string message)
        {
            string formattedMessage = configuration.DefaultChatFormat;

            if (format != null)
            {
                formattedMessage = format.Format;
            }                

            List<ChatTag> tags = pluginInstance.GetPlayerTags(player);

            string[] prefixes = tags.Where(x => !string.IsNullOrEmpty(x.Prefix))
                .Select(x => FormatRichText(x.Prefix))
                .ToArray();
            string[] suffixes = tags.Where(x => !string.IsNullOrEmpty(x.Suffix))
                .Select(x => FormatRichText(x.Suffix))
                .ToArray();

            string prefixFormat = prefixes.Length > 0 ? $"[{string.Join(", ", prefixes)}] " : string.Empty;
            string suffixFormat = suffixes.Length > 0 ? $" [{string.Join(", ", suffixes)}]" : string.Empty;

            formattedMessage = formattedMessage.Replace("{CHAT_MODE}", GetChatMode(mode));
            formattedMessage = formattedMessage.Replace("{PREFIXES}", prefixFormat);
            formattedMessage = formattedMessage.Replace("{PLAYER_NAME}", player.DisplayName);
            formattedMessage = formattedMessage.Replace("{SUFFIXES}", suffixFormat);
            formattedMessage = formattedMessage.Replace("{MESSAGE}", SerializeMessage(message));

            return formattedMessage;
        }

        private string FormatRichText(string text)
        {
            return text.Replace("{", "<").Replace("}", ">");
        }

        public ChatFormat GetPlayerFormat(UnturnedPlayer player)
        {
            List<Permission> permissions = R.Permissions.GetPermissions(player);

            return configuration.ChatFormats.FirstOrDefault(f => permissions.Any(p => p.Name.ToLower().Equals(f.Permission.ToLower())));
        }

        private string GetChatMode(EChatMode mode)
        {
            switch (mode)
            {
                case EChatMode.LOCAL:
                    return configuration.ChatModePrefixes.Area;
                case EChatMode.GLOBAL:
                    return configuration.ChatModePrefixes.World;
                case EChatMode.GROUP:
                    return configuration.ChatModePrefixes.Group;
                default:
                    return string.Empty;
            }
        }

        private string SerializeMessage(string message)
        {
            string pattern = @"<.*?>(.*?)</.*?>";

            return Regex.Replace(message, pattern, "$1");
        }
    }
}
