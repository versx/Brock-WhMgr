namespace WhMgr.Commands.Input
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.Entities;

    using WhMgr.Extensions;
    using WhMgr.Localization;

    internal sealed class PokemonSubscriptionInput : SubscriptionInput
    {
        private readonly CommandContext _context;

        public PokemonSubscriptionInput(CommandContext ctx) : base(ctx)
        {
            _context = ctx;
        }

        public async Task<IVResult> GetIVResult()
        {
            var message = (await _context.RespondEmbed("Enter the minimum IV value or specific individual values (i.e. 95 or 0-14-15):", DiscordColor.Blurple)).FirstOrDefault();
            var userValue = await _context.WaitForUserChoice();
            var attack = -1;
            var defense = -1;
            var stamina = -1;
            var realIV = 0;

            // Check if IV value contains `-` and to expect individual values instead of whole IV value
            if (userValue.Contains("-"))
            {
                var split = userValue.Split('-');
                if (split.Length != 3)
                {
                    await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_IV_VALUES").FormatText(new
                    {
                        author = _context.User.Username,
                        iv = userValue,
                    }), DiscordColor.Red);
                    return new IVResult();
                }
                if (!int.TryParse(split[0], out attack))
                {
                    await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_ATTACK_VALUE").FormatText(new
                    {
                        author = _context.User.Username,
                        atk_iv = split[0],
                    }), DiscordColor.Red);
                    return new IVResult();
                }
                if (!int.TryParse(split[1], out defense))
                {
                    await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_DEFENSE_VALUE").FormatText(new
                    {
                        author = _context.User.Username,
                        def_iv = split[1],
                    }), DiscordColor.Red);
                    return new IVResult();
                }
                if (!int.TryParse(split[2], out stamina))
                {
                    await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_STAMINA_VALUE").FormatText(new
                    {
                        author = _context.User.Username,
                        sta_iv = split[2],
                    }), DiscordColor.Red);
                    return new IVResult();
                }
            }
            else
            {
                // User provided IV value as a whole
                if (!int.TryParse(userValue, out realIV) || realIV < Strings.MinimumIV || realIV > Strings.MaximumIV)
                {
                    await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_IV_RANGE").FormatText(new
                    {
                        author = _context.User.Username,
                        iv = userValue,
                    }), DiscordColor.Red);
                    return new IVResult();
                }
            }
            await message.DeleteAsync();

            return new IVResult
            {
                IV = (ushort)realIV,
                Attack = (short)attack,
                Defense = (short)defense,
                Stamina = (short)stamina,
            };
        }

        public async Task<LevelResult> GetLevelResult()
        {
            var message = (await _context.RespondEmbed($"Enter the minimum level or minimum and maximum level (i.e 25 or 25-35):", DiscordColor.Blurple)).FirstOrDefault();
            var levelSub = await _context.WaitForUserChoice();
            var minLevel = Strings.MinimumLevel;
            var maxLevel = Strings.MaximumLevel;
            // Check if level contains `-` and to expect a minimum and maximum level provided
            if (levelSub.Contains('-'))
            {
                var split = levelSub.Split('-');
                if (!int.TryParse(split[0], out minLevel))
                {
                    await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_MINIMUM_LEVEL", new
                    {
                        author = _context.User.Username,
                        level = split[0],
                    }), DiscordColor.Red);
                    return new LevelResult();
                }
                if (!int.TryParse(split[1], out maxLevel))
                {
                    await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_MAXIMUM_LEVEL", new
                    {
                        author = _context.User.Username,
                        level = split[1],
                    }), DiscordColor.Red);
                    return new LevelResult();
                }
            }
            else
            {
                // Only minimum level was provided
                if (!int.TryParse(levelSub, out minLevel))
                {
                    await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_MINIMUM_LEVEL", new
                    {
                        author = _context.User.Username,
                        level = levelSub,
                    }), DiscordColor.Red);
                    return new LevelResult();
                }
            }

            // Validate minimum and maximum levels are within range
            if (minLevel < 0 || minLevel > 35)
            {
                await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_LEVEL").FormatText(new
                {
                    author = _context.User.Username,
                    level = levelSub,
                }), DiscordColor.Red);
                return new LevelResult();
            }

            await message.DeleteAsync();

            return new LevelResult
            {
                MinimumLevel = (ushort)minLevel,
                MaximumLevel = (ushort)maxLevel,
            };
        }

        public async Task<string> GetGenderResult()
        {
            var message = (await _context.RespondEmbed($"Enter the gender to receive notifications for (i.e `m`, `f`, or `*`):", DiscordColor.Blurple)).FirstOrDefault();
            var gender = await _context.WaitForUserChoice();

            // Check if gender is a valid gender provided
            if (!Strings.ValidGenders.Contains(gender.ToLower()))
            {
                await _context.TriggerTypingAsync();
                await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_GENDER").FormatText(new
                {
                    author = _context.User.Username,
                    gender = gender,
                }), DiscordColor.Red);
                return "*";
            }

            await message.DeleteAsync();

            return gender;
        }
    }

    internal sealed class IVResult
    {
        public ushort IV { get; set; }

        public short Attack { get; set; }

        public short Defense { get; set; }

        public short Stamina { get; set; }

        public bool IsSet => Attack == -1 || Defense == -1 || Stamina == -1;

        public IVResult() : this(0, -1, -1, -1)
        {
        }

        public IVResult(ushort iv, short attack, short defense, short stamina)
        {
            IV = iv;
            Attack = attack;
            Defense = defense;
            Stamina = stamina;
        }
    }

    internal sealed class LevelResult
    {
        public ushort MinimumLevel { get; set; }

        public ushort MaximumLevel { get; set; }

        public LevelResult() : this(0, 35)
        {
        }

        public LevelResult(ushort minLevel, ushort maxLevel)
        {
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
        }
    }
}