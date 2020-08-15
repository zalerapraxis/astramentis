# Astramentis

Astramentis is a self-hosted Discord bot with a primary focus on parsing & presenting market data for *Final Fantasy XIV*. It is written in C# and uses the [Discord.Net](https://github.com/discord-net/Discord.Net) library, and its primary focus is to provide various tailor-made services for my friends & teammates.

**Feature set:**

* Market (Aether DC only):
  * Real-time price checks & analysis
  * Determine the highest-value items to buy with ingame currencies (tomes, scrips, nuts, etc)
  * Order system - build the cheapest & most time efficient route for purchasing large quantities of different materials across the datacenter
  * Watchlist - monitor specified items for exceptionally low-priced listings & alert me via DM
* Tag system - store & retrieve info, links, funny stuff, etc
* Schedule alerts - alert your team to upcoming events such as raid nights or community events



![](https://i.imgur.com/QJgd9cz.png)![](https://i.imgur.com/Bx87sDK.jpeg)



**Requirements & supporting systems:**

* .Net Core 2.2
* Market API - I use a custom implementation similar to the old XIVAPI, but you could reconfigure it for Universalis, at the cost of losing realtime data
* MongoDB - for the tag & scheduling systems



