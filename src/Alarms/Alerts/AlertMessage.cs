namespace WhMgr.Alarms.Alerts
{
    using System;
    using System.Collections.Generic;

    public class AlertMessage : Dictionary<AlertMessageType, AlertMessageSettings>
    {
        public static readonly AlertMessage Defaults = new AlertMessage
        {
            {
                AlertMessageType.Pokemon, new AlertMessageSettings
                {
                    AvatarUrl = "{{pkmn_img_url}}",
                    Content = "{{pkmn_name}} {{form}}{{gender}} {{iv}} ({{atk_iv}}/{{def_iv}}/{{sta_iv}}) L{{lvl}}{{br}}**Despawn:** {{despawn_time}} ({{time_left}} left){{br}}**Details:** CP: {{cp}} IV: {{iv}} LV: {{lvl}}{{br}}**Size:** {{size}} | {{types_emoji}}{{#if has_weather}} | {{weather_emoji}}{{#if is_weather_boosted}} (Boosted){{/if}}{{/if}}{{br}}**Moveset:** {{moveset}}{{br}}{{#if near_pokestop}}**Near Pokestop:** [{{pokestop_name}}]({{pokestop_url}}){{br}}{{/if}}{{#if is_ditto}}**Catch Pokemon:** {{original_pkmn_name}}{{br}}{{/if}}{{#if has_capture_rates}}{{capture_1_emoji}} {{capture_1}}% {{capture_2_emoji}} {{capture_2}}% {{capture_3_emoji}} {{capture_3}}%{{br}}{{/if}}{{#if is_event}}Go Fest Spawn{{br}}{{/if}}{{#if is_pvp}}{{br}}{{pvp_stats}}{{/if}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pkmn_img_url}}",
                    Title = "{{geofence}}",
                    Url = "{{gmaps_url}}",
                    Username = "{{form}} {{pkmn_name}}{{gender}}",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.PokemonMissingStats, new AlertMessageSettings
                {
                    AvatarUrl = "{{pkmn_img_url}}",
                    Content = "{{pkmn_name}} {{form}}{{gender}}{{br}}**Despawn:** {{despawn_time}} ({{time_left}} left){{despawn_time_verified}}{{br}}**Types:** {{types_emoji}}{{br}}{{#if near_pokestop}}**Near Pokestop:** [{{pokestop_name}}]({{pokestop_url}}){{br}}{{/if}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pkmn_img_url}}",
                    Title = "{{geofence}}",
                    Url = "{{gmaps_url}}",
                    Username = "{{form}} {{pkmn_name}}{{gender}}",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Gyms, new AlertMessageSettings
                {
                    AvatarUrl = "{{gym_url}}",
                    Content = "{{#if team_changed}}Gym changed from {{old_gym_team_emoji}} {{old_gym_team}} to {{gym_team_emoji}} {{gym_team}}{{br}}{{/if}}{{#if in_battle}}Gym is under attack!{{br}}{{/if}}**Slots Available:** {{slots_available}}{{br}}{{#if is_ex}}{{ex_gym_emoji}} Gym!{{/if}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{gym_url}}",
                    Title = "{{geofence}}: {{gym_name}}",
                    Url = "{{gmaps_url}}",
                    Username = "{{gym_name}}",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Raids, new AlertMessageSettings
                {
                    AvatarUrl = "{{pkmn_img_url}}",
                    Content = "{{evolution}} {{form}} {{pkmn_name}}{{gender}} {{costume}} Raid Ends: {{end_time}} ({{end_time_left}} left){{br}}**Perfect CP:** {{perfect_cp}} / :white_sun_rain_cloud: {{perfect_cp_boosted}}{{br}}**Worst CP:** {{worst_cp}} / :white_sun_rain_cloud: {{worst_cp_boosted}}{{br}}**Types:** {{types_emoji}} | **Level:** {{lvl}} | **Team:** {{team_emoji}}{{br}}**Moveset:** {{moveset}}{{br}}**Weaknesses:** {{weaknesses_emoji}}{{br}}{{#if is_ex}}{{ex_emoji}} Gym!{{br}}{{/if}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pkmn_img_url}}",
                    Title = "{{geofence}}: {{gym_name}}",
                    Url = "{{gmaps_url}}",
                    Username = "{{form}} {{pkmn_name}} Raid",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Eggs, new AlertMessageSettings
                {
                    AvatarUrl = "{{pkmn_img_url}}",
                    Content = "Hatches: {{start_time}} ({{start_time_left}}){{br}}**Ends:** {{end_time}} ({{end_time_left}} left){{br}}**Team:** {{team_emoji}}{{br}}{{#if is_ex}}{{ex_emoji}} Gym!{{br}}{{/if}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pkmn_img_url}}",
                    Title = "{{geofence}}: {{gym_name}}",
                    Url = "{{gmaps_url}}",
                    Username = "Level {{lvl}} Egg",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Pokestops, new AlertMessageSettings
                {
                    AvatarUrl = "{{pokestop_url}}",
                    Content = "{{#if has_lure}}**Lure Expires** {{lure_expire_time}} ({{lure_expire_time_left}} left){{br}}**Lure Type:** {{lure_type}}{{br}}{{/if}}{{#if has_invasion}}**Expires:** {{invasion_expire_time}} ({{invasion_expire_time_left}} left){{br}}**Type:** {{grunt_type_emoji}} | **Gender:** {{grunt_gender}}{{br}}{{invasion_encounters}}{{br}}{{/if}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pokestop_url}}",
                    Title = "{{geofence}}: {{pokestop_name}}",
                    Url = "{{gmaps_url}}",
                    Username = "{{pokestop_name}}",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Quests, new AlertMessageSettings
                {
                    AvatarUrl = "{{quest_reward_img_url}}",
                    Content = "**Quest:** {{quest_task}}{{br}}{{#if has_quest_conditions}}**Condition(s):** {{quest_conditions}}{{br}}{{/if}}**Reward:** {{quest_reward}}{{br}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pokestop_url}}",
                    Title = "{{geofence}}: {{pokestop_name}}",
                    Url = "{{gmaps_url}}",
                    Username = "{{quest_task}}",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Invasions, new AlertMessageSettings
                {
                    AvatarUrl = "{{invasion_img_url}}",
                    Content = "{{#if has_invasion}}**Expires:** {{invasion_expire_time}} ({{invasion_expire_time_left}} left){{br}}**Type:** {{grunt_type_emoji}} | **Gender:** {{grunt_gender}}{{br}}{{invasion_encounters}}{{br}}{{/if}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pokestop_url}}",
                    Title = "{{geofence}}: {{pokestop_name}}",
                    Url = "{{gmaps_url}}",
                    Username = "{{pokestop_name}}",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Lures, new AlertMessageSettings
                {
                    AvatarUrl = "{{lure_img_url}}",
                    Content = "{{#if has_lure}}**Lure Expires:** {{lure_expire_time}} ({{lure_expire_time_left}} left){{br}}**Lure Type:** {{lure_type}}{{br}}{{/if}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pokestop_url}}",
                    Title = "{{geofence}}: {{pokestop_name}}",
                    Url = "{{gmaps_url}}",
                    Username = "{{pokestop_name}}",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Nests, new AlertMessageSettings
                {
                    AvatarUrl = "{{pkmn_img_url}}",
                    Content = "**Pokemon:** {{pkmn_name}}{{br}}**Average Spawns:** {{avg_spawns}}/h | **Types:** {{types_emojis}}{{br}}**[Google]({{gmaps_url}}) | [Apple]({{applemaps_url}}) | [Waze]({{wazemaps_url}}) | [Scanner]({{scanmaps_url}})**",
                    IconUrl = "{{pkmn_img_url}}",
                    Title = "{{geofence}}: {{nest_name}}",
                    Url = "{{gmaps_url}}",
                    Username = "",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            },
            {
                AlertMessageType.Weather, new AlertMessageSettings
                {
                    AvatarUrl = "{{weather_img_url}}",
                    Content = "**Weather Condition:** {{weather_emoji}} {{weather_condition}}{{br}}**Weather Cell ID:** #{{id}}",
                    IconUrl = "{{weather_img_url}}",
                    Title = "{{geofence}}",
                    Url = "{{gmaps_url}}",
                    Username = "Weather",
                    ImageUrl = "{{tilemaps_url}}",
                    Footer = new AlertMessageFooter
                    {
                        Text = "{{guild_name}} | {{date_time}}",
                        IconUrl = "{{guild_img_url}}"
                    }
                }
            }
        };
    }
}