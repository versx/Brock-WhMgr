# Filters
Filters allow you to narrow down what is reported. All filters are optional and can be omitted. Plenty of examples in the repository under the [`examples/Filters`](https://github.com/versx/WhMgr/tree/master/examples/filters) directory for all different needs.  

## Converters  
- WDR [Filter Converter](https://github.com/versx/WdrFilterConverter)

## Available Filter Options
```js
{
	"pokemon":
	{
		"enabled": true, // Filter is enabled
		"pokemon": [280,337,374], // List of Pokemon for the filter or empty for all
		"min_iv": 0, // Minimum IV of Pokemon to send
		"max_iv": 100, // Maximum IV of Pokemon to send
		"min_cp": 0, // Minimum CP of Pokemon
		"max_cp": 999999, // Maximum CP of Pokemon
		"gender": "m", // Only send male (m,f,*)
		"size": "Big", // Tiny, Small, Normal, Large, Big
		"great_league": true, // Great League
		"ultra_league": true, // Ultra League
		"min_rank": 1, // Minimum rank of #1 PVP stats
		"max_rank": 5, // Maximum rank of #5 PVP stats
		"type": "Include", // Include or Exclude the `pokemon` list
		"is_event": false, // Only send Pokemon checked with event accounts (GoFest, etc)
		"ignoreMissing": true // Ignore Pokemon missing stats
	},
	"eggs":
	{
		"enabled": true, // Filter is enabled
		"min_lvl": 1, // Minimum egg level to send
		"max_lvl": 6, // Maximum egg level to send
		"onlyEx": false, // Only send ex-eligible raids.
		"team": "All", // All, Valor, Mystic, Instinct, Neutral
	},
	"raids":
	{
		"enabled": true, // Filter is enabled
		"pokemon": [], // Raid bosses to include or none for all.
		"min_lvl": 1, // Minimum raid level to send
		"max_lvl": 6, // Maximum raid level to send
		"type": "Include", // Include or Exclude the `pokemon` list
		"onlyEx": false, // Only send ex-eligible raids.
		"team": "All", // All, Valor, Mystic, Instinct, Nuetral
		"ignoreMissing": true // Ignore raids missing stats
	},
	"quests":
	{
		"enabled": true, // Filter is enabled
        "rewards": ["spinda", "nincada"], // Quest reward string (Chansey, stardust, candy, etc.)
		"isShiny": false, // Only shiny encounter quests.
		"type": "Include" // Include or Exclude the `rewards` list
	},
	"pokestops":
	{
		"enabled": true, // Filter is enabled
		"lured": true, // Only send lured pokestops
		"lure_types": ["Normal", "Glacial", "Mossy", "Magnetic"], // Only send lures in type list  
		"invasions": false // Only send Team Rocket invasion pokestops
	},
	"gyms":
	{
		"enabled": true, // Filter is enabled
		"underAttack": true, // Only gyms that are under attack
		"team": "All" // Team change to notify about (i.e. Neutral/Mystic/Valor/Instinct/All)
	},
	"weather":
	{
		"enabled": true, // Filter is enabled
		"types": ["Clear", "Rainy", "PartlyCloudy", "Overcast", "Windy", "Snow", "Fog"] // Only send weather types that are in the list
	}
}
```
