# Configuration

At a minimum you'll want to make sure your have your webhook listening port set as well as one Discord server added to the `servers` property.

## Main Properties  
| Key | Example | Description |  
|---|---|---|  
| host | `10.0.0.2` | Listening interface to receive webhook data. |  
| port | `8008` | Listening port used to receive incoming json webhooks. |  
| locale | `en` | Two letter country code used to translate bot messages. |  
| shortUrlApiUrl | `http://site.com/api.php?signature=` | Yourls.org URL shortener endpoint |  
| stripeApiKey | `rk_32uo3j2lkjlkj3l2kjdlk2j3ldj2` | Stripe API key used with `expire` command to let users check their subscription expiration date. |  
| eventPokemonIds | `[123,43,483]` | List of Pokemon ID(s) to treat as event Pokemon. Event Pokemon are not reported to any channels or DM unless 90% or higher IV. |  
| iconStyles | `{ "default": "icon_path", ... }` | List key value pairs demonstrating a list of icon styles to choose from. |  
| database.main | `{}` | Main database used to save subscriptions. |  
| database.scanner | `{}` | RDM scanner database used to retrieve pokestops table. |  
| database.nests | `{}` | PMSF nests database used for reporting nests. |  
| gmapsKey | `testkeyljdsflkjsdflkj=` | Needed if you want to use the address lookup DTS. |  
| despawnTimeMinimumMinutes | `5` | Minimum despawn time in minutes a Pokemon must have in order to send the alarm (default: 5) |  
| reloadSubscriptionChangesMinutes | `1` | Reload subscriptions every minute to sync with WhMgr-UI changes (default: 1) |  
| maxNotificationsPerMinute | `10` | Maximum amount of notifications a user can receive per minute before being rate limited |  
| debug | `false` | Log webhook payloads to a file for debugging (do not enable unless you're having issues receiving data |  
| logLevel | `Info` | Only show logs with higher or equal priority levels (Trace, Debug, Info, Warning, Error, Fatal, None) |  

## Database `database`  
| Key | Example | Description |  
|---|---|---|  
| host | `127.0.0.1` | Hostname or IP address of database server. |  
| port | `3306` | Listening port for database server. |  
| username | `root` | Database username to use when authenticating. |  
| password | `password` | Database password to use when authenticating. |  
| database | `rdmdb` | Database name |  

## Discord Server Specific `servers`  
| Key | Example | Description |  
|---|---|---|  
| commandPrefix | `!` | Prefix for all commands, leave blank to use bot mention string. |  
| guildId | `4032948092834` | Discord guild ID the bot will be connecting to. |  
| emojiGuildId | `3984729874298` | Discord guild ID to use emojis from. (Can be same as `guildId`) |  
| ownerId | `8184229834297` | Bot owner's unique Discord ID. |  
| donorRoleIds | `[00000001,00000002,...]` | List of donor/support role IDs to use with permissions. |  
| moderatorRoleIds | `[09020021,09029302,...]` | List of Discord role IDs for moderators. |  
| token | `lkj2l8sl98o9slil.o32oumjj3lkjlkA` | Bot Discord authentication token. |  
| alarms | `alarms-test.json` | File path to alarms file that'll be used with the Discord server. |  
| dmAlertsFile | `alerts-dm.json` | File path to alerts file that'll be used for DMM subscription notifications. |  
| enableSubscriptions | `true` | Allow users to subscribe to specific Pokemon, Raids, Quests, and Invasions with their own pre-defined filters.|  
| enableCities | `true` | Enable the city roles used to differentiate between the different areas. |  
| cityRoles | `["City1","City2"]` | List of city role names users will be able to subscribe to. |  
| citiesRequireSupporterRole | `true` | If `true`, any city role assignment command will require the user have a donor/supporter role. |  
| pruneQuestChannels | `true` | If `true`, prune designated quest channels every day at midnight. |  
| questChannelIds | `[098309389,987398790,...]` | |  
| nestsChannelId | `1347092710` | |  
| shinyStats.enabled | `true` | If `true`, enable shiny stats posting. |  
| shinyStats.clearMessages | `false` | Clear previous shiny stat messages. |  
| shinyStats.channelId | `1347092710` | Channel ID to post shiny stats. |  
| iconStyle | `Default` | Icon style to use for Pokemon, Raid, Quest, and Invasion images. |  
| botChannelIds | `[098309389,987398790,...]` | Prevents the bot from executing commands outside of listed channels. |  
| status | `Finding Pokemon...` | Custom Discord bot status, leave blank for bot version string |  

## Urls `urls`  
| Key | Example | Description |  
|---|---|---|  
| staticMap | `https://tiles.com:8080` | Static map tile server endpoint. |  
| scannerMap | `https://map.com/@/{0}/{1}/15` | Scanner map url for embed DTS `scanmaps_url`. |  

## StaticMaps `staticMaps`  
| Key | Example | Description |  
|---|---|---|  
| pokemon | `pokemon.example` | Name of staticmap template used for pokemon messages on tileserver. |  
| raids | `raids.example` | Name of staticmap template used for raids messages on tileserver. |  
| gyms | `gyms.example` | Name of staticmap template used for gym messages on tileserver. |  
| quests | `quests.example` | Name of staticmap template used for quest messages on tileserver. |  
| invasions | `invasions.example` | Name of staticmap template used for invasion messages on tileserver. |  
| lures | `lures.example` | Name of staticmap template used for lure messages on tileserver. |  
| weather | `weather.example` | Name of staticmap template used for weather messages on tileserver. |  
| nests | `nests.example` | Name of staticmap template used for nest messages on tileserver. |  

## Twilio `twilio`  
| Key | Example | Description |  
|---|---|---|  
| enabled | `false` | Determines if text message alerts are enabled |  
| accountSid | `ACb9ef2a14fa64...` | Twilio account SID (Get via Twilio dashboard) |  
| authToken | `19c2f1c032962f...` | Twilio account auth token (Get via Twilio dashboard) |  
| from | `8181234567` | Twilio phone number that will be sending the text message alert |  
| userIds | `[092830498234,80928340822]` | List of Discord user ids that can receive text message alerts |  
| pokemonIds | `[201,480,481,482,443,633,610]` | List of acceptable Pokemon to receive text message alerts for |  
| minIV | `100` | Minimum acceptable IV value for Pokemon if not ultra rare (Unown, Lake Trio) |  

## Example
```js
{
    // Http listening interface for raw webhook data.
    "host": "10.0.0.10",
    // Http listener port for raw webhook data.
    "port": 8008,
    // Locale language translation
    "locale": "en",
    // ShortURL API (yourls.org API, i.e. `https://domain.com/yourls-api.php?signature=XXXXXX`)
    "shortUrlApiUrl": "",
    // Stripe API key (Stripe production API key, i.e. rk_3824802934
    "stripeApiKey": ""
    // List of Discord servers to connect and post webhook messages to.
    "servers": {
        // Discord server #1 guild ID (replace `000000000000000001` with guild id of server)
        "000000000000000001": "discord1.json",
        // 2nd Discord server section (if applicable)
        "000000000000000002": "discord2.json"
    },
    // Database configuration
    "database": {
        // Database to store notification subscriptions.
        "main": {
            // Database hostname or IP address.
            "host": "127.0.0.1",
            // Database connection port.
            "port": 3306,
            // Database user account name.
            "username": "root",
            // Database user account password.
            "password": "password",
            // Brock database name.
            "database": "brock3"
        },
        // Scanner database config
        "scanner": {
            // Database hostname or IP address.
            "host": "127.0.0.1",
            // Database connection port.
            "port": 3306,
            // Database user account name.
            "username": "root",
            // Database user account password.
            "password": "password",
            // RDM database name.
            "database": "rdmdb"
        },
        // PMSF Nests database config
        "nests": {
            // Database hostname or IP address.
            "host": "127.0.0.1",
            // Database connection port.
            "port": 3306,
            // Database user account name.
            "username": "root",
            // Database user account password.
            "password": "password",
            // PMSF nests database name.
            "database": "manualdb"
        }
    },
    // Event Pokemon filtering
    "eventPokemon": {
        /* Filtering type to use with deemed "event" Pokemon. Set to `Exclude` if you do not want the Pokemon reported unless
           it meets the minimumIV value set (or is 0% or has PvP stats.
           Set to `Include` if you only want the Pokemon reported if it meets the minimum IV value set. No other Pokemon will
           be reported other than those in the event list.
		   
        */ 
        "type": "Exclude",
        // List of Pokemon IDs to treat as event and restrict postings and subscriptions to 90% IV or higher. (Filled in automatically with `event set` command)  
        "pokemonIds": [
            129,
            456,
            320
        ],
        // Minimum IV value for an event Pokemon to have to meet in order to post via Discord channel alarm or direct message subscription.
        "minimumIV": 90
    },
    // Image URL config
    "urls": {
        // Static map tileserver endpoint.  
        "staticMap": "http://tiles.example.com:8080",
        // Scanner map DTS option for embeds as `scanmaps_url`  
        "scannerMap": "http://map.example.com/@/{0}/{1}/15"
    },
    // Available icon styles
    "iconStyles": {
        "Default": "https://raw.githubusercontent.com/nileplumb/PkmnHomeIcons/ICONS/ICONS/",
        "Shuffle": "https://raw.githubusercontent.com/nileplumb/PkmnShuffleMap/master/ICONS_STANDARD/"
    },
    // Custom static map template files for each alarm type
    "staticMaps": {
        // Static map template for Pokemon
        "pokemon": "pokemon.example",
        // Static map template for Raids and Eggs
        "raids": "raids.example",
        // Static map template for field research quests
        "quests": "quests.example",
        // Static map template for Team Rocket invasions
        "invasions": "invasions.example",
        // Static map template for Pokestop lures
        "lures": "lures.example",
        // Static map template for Gym team control changes
        "gyms": "gyms.example",
        // Static map template for nest postings
        "nests": "nests.example",
        // Static map template for weather changes
        "weather": "weather.example"
    },
    // Get text message alerts with Twilio.com
    "twilio": {
        // Determines if text message alerts are enabled
        "enabled": false,
        // Twilio account SID (Get via Twilio dashboard)
        "accountSid": "",
        // Twilio account auth token (Get via Twilio dashboard)
        "authToken": "",
        // Twilio phone number that will be sending the text message alert
        "from": "",
        // List of Discord user ids that can receive text message alerts
        "userIds": [],
        // List of Discord roles that can receive text message alerts
        "roleIds": [],
        // List of acceptable Pokemon to receive text message alerts for
        "pokemonIds": [201, 480, 481, 482, 443, 444, 445, 633, 634, 635, 610, 611, 612],
        // Minimum acceptable IV value for Pokemon if not ultra rare (Unown, Lake Trio)
        "minIV": 100
    },
    // Needed if you want to use the address lookup DTS
    "gmapsKey": "",
    // Minimum despawn time in minutes a Pokemon must have in order to send the alarm (default: 5 minutes)
    "despawnTimeMinimumMinutes": 5,
    // Reload subscriptions every minute to sync with WhMgr-UI changes  
    "reloadSubscriptionChangesMinutes": 1,
    // Maximum amount of notifications a user can receive per minute before being rate limited  
    "maxNotificationsPerMinute": 10,
    // Log webhook payloads to a file for debugging (do not enable unless you're having issues receiving data
    "debug": false,
    // Only show logs with higher or equal priority levels (Trace, Debug, Info, Warning, Error, Fatal, None)
    "logLevel": "Trace"
}
```