namespace WhMgr.Commands.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.Entities;

    using WhMgr.Configuration;
    using WhMgr.Extensions;
    using WhMgr.Localization;

    /// <summary>
    /// Subscription input class
    /// </summary>
    internal class SubscriptionInput
    {
        private readonly CommandContext _context;

        /// <summary>
        /// Instantiate a new <seealso cref="SubscriptionInput"/> class
        /// </summary>
        /// <param name="ctx"></param>
        public SubscriptionInput(CommandContext ctx)
        {
            _context = ctx;
        }

        /// <summary>
        /// Gets the Pokemon ID/Name list from the Discord interactivity from the user
        /// </summary>
        /// <returns>Returns a <seealso cref="PokemonValidation"/> object containing valid and invalid Pokemon specified.</returns>
        public async Task<PokemonValidation> GetPokemonResult(uint maxPokemonId)
        {
            var pokemonMessage = (await _context.RespondEmbed("Enter either the Pokemon name(s) or Pokedex ID(s) separated by a comma to subscribe to (i.e. Mewtwo,Dragonite):", DiscordColor.Blurple)).FirstOrDefault();
            var pokemonSubs = await _context.WaitForUserChoice();
            // Validate the provided pokemon list
            var validation = PokemonValidation.Validate(pokemonSubs, maxPokemonId);
            if (validation == null || validation.Valid.Count == 0)
            {
                await _context.RespondEmbed(Translator.Instance.Translate("NOTIFY_INVALID_POKEMON_IDS_OR_NAMES").FormatText(_context.User.Username, string.Join(", ", validation.Invalid)), DiscordColor.Red);
                return new PokemonValidation();
            }
            await pokemonMessage.DeleteAsync();
            return validation;
        }

        /// <summary>
        /// Gets the areas list from the Discord interacitivity from the user
        /// </summary>
        /// <param name="guildId">Discord server guild id to lookup valid areas</param>
        /// <returns>Returns a list of valid areas specified</returns>
        public async Task<List<string>> GetAreasResult(ulong guildId)
        {
            var config = (WhConfigHolder)_context.Services.GetService(typeof(WhConfigHolder));
            var server = config.Instance.Servers[guildId];
            var validAreas = server.Geofences.Select(g => g.Name).ToList();
            var message = (await _context.RespondEmbed($"Enter the areas to get notifications from separated by a comma (i.e. `city1,city2`):\n**Available Areas:**\n{string.Join("\n- ", validAreas)}\n- All", DiscordColor.Blurple)).FirstOrDefault();
            var cities = await _context.WaitForUserChoice(true);
            await message.DeleteAsync();

            // Check if provided areas are valid and only return valid areas
            var areas = SubscriptionAreas.GetAreas(server, cities);
            if (areas.Count == 0)
            {
                // No valid areas provided
                return new List<string>();
            }
            return areas;
        }
    }
}