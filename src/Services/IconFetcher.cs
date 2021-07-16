namespace WhMgr.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;
    using Gender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;
    using QuestRewardType = POGOProtos.Rpc.QuestRewardProto.Types.Type;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;
    using ServiceStack;

    using WhMgr.Net.Models;
    using WhMgr.Utilities;

    class IconFetcher
    {
        private static readonly IconSet _availablePokemonForms = new IconSet();
        private static IReadOnlyDictionary<string, string> _iconStyles;

        #region Singleton

        private static IconFetcher _instance;

        public static IconFetcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new IconFetcher();
                }
                return _instance;
            }
        }

        #endregion

        public string GetPokemonIcon(string style, uint pokemonId, int form = 0, int evolution = 0, Gender gender = Gender.Unset, int costume = 0, bool shiny = false)
        {
            if (!_availablePokemonForms.ContainsKey(style))
            {
                return _iconStyles[style] + "pokemon/0.png"; // Substitute Pokemon
            }
            var evolutionSuffixes = (evolution > 0 ? new [] { "-e" + evolution, string.Empty }   : new [] { string.Empty }).ToList();
            var formSuffixes      = (form      > 0 ? new [] { "-f" + form, string.Empty }        : new [] { string.Empty }).ToList();
            var costumeSuffixes   = (costume   > 0 ? new [] { "-c" + costume, string.Empty }     : new [] { string.Empty }).ToList();
            var genderSuffixes    = (gender    > 0 ? new [] { "-g" + (int)gender, string.Empty } : new [] { string.Empty }).ToList();
            var shinySuffixes     = (shiny         ? new [] { "-shiny", string.Empty }           : new [] { string.Empty }).ToList();
            foreach (var evolutionSuffix in evolutionSuffixes)
            {
                foreach (var formSuffix in formSuffixes)
                {
                    foreach (var costumeSuffix in costumeSuffixes)
                    {
                        foreach (var genderSuffix in genderSuffixes)
                        {
                            foreach (var shinySuffix in shinySuffixes)
                            {
                                var result = $"{pokemonId}{evolutionSuffix}{formSuffix}{costumeSuffix}{genderSuffix}{shinySuffix}";
                                if (_availablePokemonForms[style].Contains(result))
                                {
                                    return _iconStyles[style] + $"pokemon/{result}.png";
                                }
                            }
                        }
                    }
                }
            }
            return _iconStyles[style] + "pokemon/0.png"; // Substitute Pokemon
        }

        public string GetRaidEggIcon(string style, int level, bool hatched = false, bool ex = false)
        {
            var sb = new StringBuilder();
            sb.Append("raid/");
            sb.Append(level);
            if (hatched) sb.Append("-hatched");
            if (ex)      sb.Append("-ex");
            sb.Append(".png");
            return _iconStyles[style] + sb.ToString();
        }

        public string GetQuestIcon(string style, QuestData quest)
        {
            var sb = new StringBuilder();
            sb.Append("reward/");
            var questRewardType = quest.Rewards[0].Type;
            switch (questRewardType)
            {
                case QuestRewardType.Candy:
                case QuestRewardType.Item:
                    sb.Append((int)questRewardType);
                    sb.Append("-i");
                    sb.Append((int)quest.Rewards[0].Info.Item);
                    sb.Append("-a");
                    sb.Append(quest.Rewards?[0]?.Info?.Amount ?? 1);
                    sb.Append(".png");
                    break;
                case QuestRewardType.PokemonEncounter:
                    return Instance.GetPokemonIcon
                    (
                        style,
                        quest.IsDitto ? 132 : quest.Rewards[0].Info.PokemonId,
                        quest.Rewards?[0].Info.FormId ?? 0,
                        0, //quest.Rewards?[0].Info.EvolutionId ?? 0,
                        Gender.Unset,
                        quest.Rewards?[0].Info.CostumeId ?? 0,
                        quest.Rewards?[0].Info.Shiny ?? false
                    );
                case QuestRewardType.Stardust:
                    sb.Append((int)questRewardType);
                    sb.Append("-a");
                    sb.Append(quest.Rewards[0].Info.Amount);
                    sb.Append(".png");
                    break;
                case QuestRewardType.AvatarClothing:
                case QuestRewardType.Experience:
                case QuestRewardType.Quest:
                    sb.Append((int)questRewardType);
                    sb.Append(".png");
                    break;
                case QuestRewardType.Unset:
                default:
                    return null;
            }
            return _iconStyles[style] + sb.ToString();
        }

        public string GetLureIcon(string style, PokestopLureType lureType)
        {
            return _iconStyles[style] + "reward/2-i" + (int)lureType + "-a1.png";
        }

        public string GetInvasionIcon(string style, InvasionCharacter gruntType)
        {
            return _iconStyles[style] + "invasion/" + (int)gruntType + ".png";
        }

        public string GetWeatherIcon(string style, WeatherCondition weatherType)
        {
            return _iconStyles[style] + "weather/" + (int)weatherType + ".png";
        }

        public void SetIconStyles(IReadOnlyDictionary<string, string> iconStyles)
        {
            _iconStyles = iconStyles;
            BuildAvailableFormsLists();
        }

        private void BuildAvailableFormsLists()
        {
            // Get available forms from remote icons repo to build form list for each icon style
            foreach (var style in _iconStyles)
            {
                // Check if style already checked, if so skip
                if (_availablePokemonForms.ContainsKey(style.Key))
                    continue;

                // Get the remote form index file from the icon repository
                var formsListJson = NetUtil.Get(style.Value + "pokemon/index.json");
                if (string.IsNullOrEmpty(formsListJson))
                {
                    // Failed to get form list, add empty form set and skip to the next style
                    _availablePokemonForms.Add(style.Key, new HashSet<string>());
                    continue;
                }
                // Deserialize json list to hash set
                var formsList = JsonConvert.DeserializeObject<HashSet<string>>(formsListJson);
                // Add style and form list
                _availablePokemonForms.Add(style.Key, formsList);
            }
        }
    }

    class IconSet : Dictionary<string, HashSet<string>>
    {
    }
}