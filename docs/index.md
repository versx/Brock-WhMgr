# Welcome to Webhook Manager

Works with the following backends:  

- [RealDeviceMap](https://github.com/123FLO321/RealDeviceMap)  
- [Chuck](https://github.com/WatWowMap/Chuck)  
- [ChuckDeviceController](https://github.com/versx/ChuckDeviceController)  


Made in C#, runs on .NET Core CLR. Cross platform compatibility, can run on Windows, macOS, and Linux operating systems.  
Sends Discord notifications based on pre-defined filters for Pokemon, raids, raid eggs, field research quests, Team Rocket invasions, gym team changes, and weather. Also supports Discord user's subscribing to Pokemon, raid, quest, Team Rocket invasion, and Pokestop lure notifications via direct messages.

## Features  
- Supports multiple Discord servers.  
- Discord channel alarm reports for Pokemon, raids, eggs, quests, lures, invasions, gym team changes, and weather.  
- Per user custom Discord notifications for Pokemon, raids, quests, invasions, and lures.  
- User interface to configure Discord notifications with ease (as well as Discord commands). [WhMgr-UI](https://github.com/versx/WhMgr-UI)  
- Subscription notifications based on pre-defined distance.  
- Customizable alert messages with dynamic text replacement.  
- Support for multiple cities/areas using geofences per server.  
- Daily shiny stats reporting.  
- Automatic quest message purge at midnight.  
- Support for Donors/Supporters only notifications.  
- Direct messages of Pokemon notifications based on city roles assigned.  
- Pokemon and Raid subscription notifications based on specific forms.  
- Custom prefix support as well as mentionable user support for commands.  
- Raid subscription notifications for specific gyms.  
- Twilio text message alerts for ultra rare Pokemon.  
- Custom image support for Discord alarm reports.  
- Custom icon style selection for Discord user notifications.  
- External emoji server support.  
- Custom static map format support.  
- Support for language translation.  
- Multi threaded, low processing consumption.  
- [I.C.O.N.S.](https://github.com/Mygod/pokemon-icon-postprocessor) standard image support.
- Lots more...  

## Direct Message Notification Filters  
- Pokemon ID  
- Pokemon Form  
- Pokemon IV  
- Pokemon Level  
- List of Pokemon Attack/Defense/Stamina values  
- Pokemon Gender  
- Raid Boss  
- City  
- Gym Name  
- Quest Reward  
- Invasion Grunt Type  
- Pokestop Lure Type  
- Distance (meters)  

## Frameworks and Libraries
- .NET Core v2.1.803  
- DSharpPlus v3.2.3  
- DSharpPlus.CommandsNext v3.2.3  
- DSharpPlus.Interactivity v3.2.3  
- Microsoft.Win32.SystemEvents v4.7.0  
- Newtonsoft.Json v12.0.3  
- ServiceStack.OrmLite.MySql v5.8.0  
- Stripe.net v37.14.0  
- Twilio v5.44.0  


**[Click here](user-guide/config) to get started!**  