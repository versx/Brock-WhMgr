# Alarms

Alarms are used to define what Pokemon, raids, eggs, field research quests, Team Rocket invasions, lures, gyms, and weather are sent to which channels.  

There is no limit to the amount of alarms you can add under the `alarms` property list, although adding hundreds could potentially affect performance.  

**Note:** Place your active alarms in your `alarms` folder.  

## Example
```js
{
    // Enable or disable Pokemon alarms globally
    "enablePokemon": true,
    // Enable or disable Raid alarms globally
    "enableRaids": true,
    // Enable or disable Quest alarms globally
    "enableQuests": true,
    // Enable or disable Pokestop alarms globally
    "enablePokestops": true,
    // Enable or disable Gym alarms globally
    "enableGyms": true, 
    // Enable or disable Weather alarms globally
    "enableWeather": true,
    // List of alarms
	"alarms":
	[
		{
            // Alarm name
            "name":"City1-Rare",
            // Alerts file location (used to structure how the message will look)
            "alerts": "default.json",
            // Alarm filters
            "filters":"all.json",
            // Mentionable string that supports DTS  
            "mentions":"<!@324234324> <iv> L<lvl> <geofence>",  
            // Either the geofence file path (`geojson` or `ini` format) or the geofence name
            "geofences": ["geofence1.txt", "city1"],
            // Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
            // 100% IV alarm for City1
            "name":"City1-100iv",
            // Alerts file location (used to structure how the message will look)
            "alerts": "default.json",
            // Alarm filters
            "filters":"100iv.json",
            // Either the geofence file path (`geojson` or `ini` format) or the geofence name
            "geofences": ["geofence1.txt", "city1"],
            // Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"City1-Raids",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"raids.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geofence1.txt", "city1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"City1-LegendaryRaids",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"legendary_raids.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geofence1.txt", "city1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"City1-ExRaids",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"ex_raids.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geofence1.txt", "city1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name": "City1-Quests",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters": "quests.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geofence1.txt", "city1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name": "City1-Lures",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters": "lures.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geofence1.txt", "city1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name": "City1-Invasions",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters": "invasions.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geofence1.txt", "city1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name": "City1-Gyms",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters": "gyms.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geofence1.txt", "city1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"City2-Rare",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"all.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"City2-100iv",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"100iv.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"City2-Raids",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"raids.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"City2-LegendaryRaids",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"legendary_raids.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"City2-ExRaids",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"ex_raids.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name": "City2-Quests",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters": "quests.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name": "City2-Lures",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters": "lures.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name": "City2-Invasions",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters": "invasions.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name": "City2-Gyms",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters": "gyms.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		},
		{
			// Alarm name
			"name":"Absol-Quests",
			// Alerts file location (used to structure how the message will look)
			"alerts": "default.json",
			// Alarm filters
			"filters":"quests_absol.json",
			// Either the geofence file path (`geojson` or `ini` format) or the geofence name
			"geofences": ["geojson1.json", "geofence2.txt", "cityName1"],
			// Discord webhook url address
			"webhook":"<DISCORD_WEBHOOK_URL>"
		}
	]
}
```