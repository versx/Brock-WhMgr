namespace WhMgr.Localization
{
    using System;
    using System.Collections.Generic;

    using ActivityType = POGOProtos.Rpc.HoloActivityType;
    using AlignmentType = POGOProtos.Rpc.PokemonDisplayProto.Types.Alignment;
    using CharacterCategory = POGOProtos.Rpc.EnumWrapper.Types.CharacterCategory;
    using ItemId = POGOProtos.Rpc.Item;
    using TemporaryEvolutionId = POGOProtos.Rpc.HoloTemporaryEvolutionId;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using WhMgr.Diagnostics;

    public class Translator : Language<string, string, Dictionary<string, string>>
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("LOCALE");

        #region Singleton

        private static Translator _instance;
        
        public static Translator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Translator();
                }
                return _instance;
            }
        }

        #endregion

        public override string Translate(string value)
        {
            try
            {
                return base.Translate(value) ?? value;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to find locale translation for key '{value}'");
                _logger.Error(ex);
            }
            return value;
        }

        public string Translate(string value, params object[] args)
        {
            try
            {
                var text = args?.Length > 0
                    ? string.Format(base.Translate(value), args)
                    : base.Translate(value);
                return text ?? value;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to find locale translation for key '{value}' and arguments: '{string.Join(",", args)}'");
                _logger.Error(ex);
            }
            return value;
        }

        public string GetPokemonName(uint pokeId)
        {
            return Translate($"poke_{pokeId}");
        }

        public string GetFormName(int formId, bool includeNormal = false)
        {
            if (formId == 0)
                return null;

            var form = Translate("form_" + formId);
            // TODO: Localize
            if (!includeNormal && string.Compare(form, "Normal", true) == 0)
                return string.Empty;
            return form;
        }

        public string GetCostumeName(int costumeId)
        {
            if (costumeId == 0)
                return null;

            var costume = Translate("costume_" + costumeId);
            return costume;
        }

        public string GetEvolutionName(int evoId)
        {
            if (evoId == 0)
                return null;

            var evo = Translate("evo_" + evoId);
            return evo;
        }

        public string GetMoveName(int moveId)
        {
            if (moveId == 0)
                return "Unknown"; // TODO: Localize

            return Translate($"move_{moveId}");
        }

        public string GetThrowName(ActivityType throwTypeId)
        {
            return Translate($"throw_type_{(int)throwTypeId}");
        }

        public string GetItem(ItemId item)
        {
            return Translate($"item_{(int)item}");
        }

        public string GetWeather(WeatherCondition weather)
        {
            return Translate($"weather_{(int)weather}");
        }

        public string GetAlignmentName(AlignmentType alignment)
        {
            return Translate($"alignment_{(int)alignment}");
        }

        public string GetCharacterCategoryName(CharacterCategory category)
        {
            return Translate($"character_category_{(int)category}");
        }

        public string GetEvolutionName(TemporaryEvolutionId evolution)
        {
            return Translate($"evo_{(int)evolution}");
        }
    }
}
