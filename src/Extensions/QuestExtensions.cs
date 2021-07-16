namespace WhMgr.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using POGOProtos.Rpc;
    using AlignmentId = POGOProtos.Rpc.PokemonDisplayProto.Types.Alignment;
    using CharacterCategory = POGOProtos.Rpc.EnumWrapper.Types.CharacterCategory;
    using TemporaryEvolutionId = POGOProtos.Rpc.HoloTemporaryEvolutionId;
    using QuestConditionType = POGOProtos.Rpc.QuestConditionProto.Types.ConditionType;
    using QuestRewardType = POGOProtos.Rpc.QuestRewardProto.Types.Type;

    using WhMgr.Data.Models;
    using WhMgr.Localization;
    using WhMgr.Net.Models;

    public static class QuestExtensions
    {
        public static string GetQuestMessage(this QuestData quest)
        {
            return GetQuestMessage(quest.Type, quest.Target);
        }

        public static string GetQuestMessage(this QuestType type, int target)
        {
            return Translator.Instance.Translate("quest_" + Convert.ToInt32(type)).FormatText(new
            {
                amount = target,
            });
        }

        public static string GetConditions(this QuestData quest)
        {
            return GetConditions(quest.Conditions);
        }

        public static string GetConditions(this List<QuestConditionMessage> conditions)
        {
            if (conditions == null)
                return null;

            var list = new List<string>();
            for (var i = 0; i < conditions.Count; i++)
            {
                try
                {
                    var condition = conditions[i].GetCondition();
                    if (string.IsNullOrEmpty(condition))
                        continue;

                    list.Add(condition);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return string.Join(", ", list);
        }

        public static string GetCondition(this QuestConditionMessage condition)
        {
            var conditionKey = "quest_condition_" + Convert.ToInt32(condition.Type);
            var formattedSuffix = "_formatted";
            switch (condition.Type)
            {
                case QuestConditionType.WithPokemonCategory:
                    return Translator.Instance.Translate(conditionKey + formattedSuffix).FormatText(new
                    {
                        pokemon = string.Join(", ", condition.Info.PokemonIds?.Select(x =>
                            Translator.Instance.GetPokemonName(x)).ToList()
                        ),
                    });
                case QuestConditionType.WithPokemonType:
                    return Translator.Instance.Translate(conditionKey + formattedSuffix).FormatText(new
                    {
                        types = string.Join(", ", condition.Info.PokemonTypeIds?.Select(x =>
                            Convert.ToString((PokemonType)x))) + "-type",
                    });
                case QuestConditionType.WithRaidLevel:
                    return Translator.Instance.Translate(conditionKey + formattedSuffix).FormatText(new
                    {
                        levels = string.Join(", ", condition.Info.RaidLevels),
                    });
                case QuestConditionType.WithThrowType:
                    return Translator.Instance.Translate(conditionKey + formattedSuffix).FormatText(new
                    {
                        throw_type = Translator.Instance.GetThrowName(condition.Info.ThrowTypeId),
                    });
                case QuestConditionType.WithThrowTypeInARow:
                    return Translator.Instance.Translate(conditionKey + formattedSuffix).FormatText(new
                    {
                        throw_type = Translator.Instance.GetThrowName(condition.Info.ThrowTypeId),
                    });
                case QuestConditionType.WithPokemonAlignment:
                    return Translator.Instance.Translate(conditionKey + formattedSuffix).FormatText(new
                    {
                        alignments = string.Join(", ", condition.Info.AlignmentIds?.Select(x =>
                            Translator.Instance.GetAlignmentName((AlignmentId)x))
                        ),
                    });
                case QuestConditionType.WithInvasionCharacter:
                    return Translator.Instance.Translate(conditionKey + formattedSuffix).FormatText(new
                    {
                        categories = string.Join(", ", condition.Info.CharacterCategoryIds?.Select(x =>
                            Translator.Instance.GetCharacterCategoryName((CharacterCategory)x))
                        ),
                    });
                case QuestConditionType.WithTempEvoPokemon: // Mega evo
                    return Translator.Instance.Translate(conditionKey + formattedSuffix).FormatText(new
                    {
                        pokemon = string.Join(", ", condition.Info.RaidPokemonEvolutions?.Select(x =>
                            Translator.Instance.GetEvolutionName((TemporaryEvolutionId)x))
                        ),
                    });

                case QuestConditionType.WithBadgeType:
                case QuestConditionType.WithCurveBall:
                case QuestConditionType.WithDailyCaptureBonus:
                case QuestConditionType.WithDailySpinBonus:
                case QuestConditionType.WithDaysInARow:
                case QuestConditionType.WithSuperEffectiveCharge:
                case QuestConditionType.WithQuestContext:
                case QuestConditionType.WithItem:
                case QuestConditionType.WithNewFriend:
                case QuestConditionType.WithPlayerLevel:
                case QuestConditionType.WithUniquePokestop:
                case QuestConditionType.WithWeatherBoost:
                case QuestConditionType.WithWinBattleStatus:
                case QuestConditionType.WithWinGymBattleStatus:
                case QuestConditionType.WithWinRaidStatus:
                case QuestConditionType.WithUniquePokemon:
                case QuestConditionType.WithNpcCombat:
                case QuestConditionType.WithPvpCombat:
                case QuestConditionType.WithLocation:
                case QuestConditionType.WithDistance:
                case QuestConditionType.WithBuddy:
                default:
                    return Translator.Instance.Translate(conditionKey);
            }
        }

        public static string GetReward(this QuestData quest)
        {
            return GetReward(quest.Rewards?.FirstOrDefault());
        }

        public static string GetReward(this QuestRewardMessage reward)
        {
            return GetReward(reward.Type, reward.Info);
        }

        public static string GetReward(this Pokestop pokestop)
        {
            return pokestop.QuestRewards?.FirstOrDefault()?.GetReward();
        }

        public static string GetReward(this QuestRewardType type, QuestReward info)
        {
            var rewardKey = "quest_reward_" + Convert.ToInt32(type);
            var formattedSuffix = "_formatted";
            switch (type)
            {
                case QuestRewardType.AvatarClothing:
                case QuestRewardType.Quest:
                    return Translator.Instance.Translate(rewardKey);

                case QuestRewardType.Candy:
                case QuestRewardType.MegaResource:
                case QuestRewardType.XlCandy:
                    return Translator.Instance.Translate(rewardKey + formattedSuffix, new
                    {
                        amount = info.Amount.ToString(),
                        pokemon = Translator.Instance.GetPokemonName(info.Ditto ? 132 : info.PokemonId),
                    });
                
                case QuestRewardType.Experience:
                case QuestRewardType.Pokecoin:
                case QuestRewardType.Stardust:
                    return Translator.Instance.Translate(rewardKey + formattedSuffix).FormatText(new
                    {
                        amount = info.Amount.ToString(),
                    });

                case QuestRewardType.Item:
                    return Translator.Instance.Translate(rewardKey + formattedSuffix).FormatText(new
                    {
                        amount = info.Amount.ToString(),
                        item = Translator.Instance.GetItem(info.Item),
                    });

                case QuestRewardType.PokemonEncounter:
                    return Translator.Instance.Translate(rewardKey + formattedSuffix).FormatText(new
                    {
                        pokemon = (info.Shiny ? $"**SHINY** " : "") + Translator.Instance.GetPokemonName(info.Ditto ? 132 : info.PokemonId),
                        form = info.FormId > 0 ? Translator.Instance.GetFormName(info.FormId) : null,
                    });

                case QuestRewardType.Sticker:
                    //return Translator.Instance.Translate(rewardKey, info.StickerId, info.Amount);
                    return Translator.Instance.Translate(rewardKey + formattedSuffix, new
                    {
                        sticker = info.StickerId,
                        amount = info.Amount.ToString(),
                    });

                case QuestRewardType.LevelCap:
                    return type.ToString();
            }

            return "Unknown";
        }
    }
}
