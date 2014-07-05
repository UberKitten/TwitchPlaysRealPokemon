TwitchPlaysRealPokemon
======================

The IRC client and Arduino code behind TwitchPlaysRealPokemon.

[TwitchPlaysPokemon](http://www.twitch.tv/twitchplayspokemon) is a Twitch stream emulating Pokemon games and taking in controller inputs via chat and them pressing them in-game. I loved the stream and I had my childhood Pokemon GameBoy Color laying around, so I took it apart, wired it to an Arduino, and started a new stream, TwitchPlaysRealPokemon.

Recently I was asked on Twitter to share the source code, so I tossed it on Github. **This code is not production-quality and was built overnight for a short project. It is undocumented, uncommented, and specific to my needs.** That being said, I designed the code fairly robust and it should serve as good inspiration. Try out the "TEST INPUT" feature, I could successfully run hundreds of button inputs a minute and was only limited by the speed of my servos.

Licensed under the MIT License. Made using the [IrcDotNet](https://github.com/w0rd-driven/ircdotnet) library, included because I had to compile it myself for reasons I forget why.

[Pictures here](https://imgur.com/a/57iHU), [reddit discussion thread here with QA](http://www.reddit.com/r/twitchplayspokemon/comments/1z1o0i/modding_my_gameboy_color_for/), [subreddit here](http://www.reddit.com/r/tprp).
