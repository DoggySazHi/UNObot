﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using UNObot.Services;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identitynamespace UNObot.Modules

namespace UNObot.Modules
{
    public class MusicBot : ModuleBase<SocketCommandContext>, IAsyncDisposable
    {
        [Command("playmusic", RunMode = RunMode.Async)]
        [Help(new string[] { ".playmusic (YouTube Link)" }, "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
        public async Task PlayMusic([Remainder] string Link)
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                _ = ReplyAsync("Please join a VC that I can connect to!");
                return;
            }

            try
            {
                _ = ReplyAsync($"Program is now querying, please wait warmly...");
                var Embed = await DisplayEmbed.DisplayAddSong(Context.User.Id, Context.Guild.Id, Link);
                await ReplyAsync("", false, Embed.Item1);

                //if (MusicPlayers.Where(o => o.Server == Context.Guild.Id).Count() == 0)
                //    MusicPlayers.Add(new Player(Context.Guild.Id, await AudioChannel.ConnectAsync()));

                // Wait before downloading...
                var Result2 = await YoutubeService.GetSingleton().Download(Link);
                AudioService ads = AudioService.GetSingleton();
                await ads.JoinAudio(Context.Guild, AudioChannel);
                await ads.SendAudioAsync(Context.Guild, Context.Channel, Result2);
                await ads.LeaveAudio(Context.Guild);
                File.Delete(Result2);
            }
            catch (Exception ex)
            {
                _ = ReplyAsync($"Error: {ex.Message}");
            }
        }

        [Command("vctest2", RunMode = RunMode.Async)]
        public async Task VCTest2([Remainder] string song)
        {
            var AudioChannel = (Context.User as IVoiceState)?.VoiceChannel;
            if (AudioChannel == null)
            {
                _ = ReplyAsync("Please join a VC that I can connect to!");
                return;
            }

            AudioService ads = AudioService.GetSingleton();
            await ads.JoinAudio(Context.Guild, AudioChannel);
            await ads.SendAudioAsync(Context.Guild, Context.Channel, song);
            await ads.LeaveAudio(Context.Guild);
        }

        [Command("timings", RunMode = RunMode.Async)]
        public async Task MusicBotTimings()
        {
            var Timings = YoutubeService.GetSingleton().Timings;
            await ReplyAsync($"Timings: \n" +
                $"File search: {Timings[0]} ms\n" +
                $"ID parse: {Timings[1]} ms\n" +
                $"Stream search: {Timings[2] + Timings[3] + Timings[4]} ms\n" +
                $"- Getting streams: {Timings[2]} ms\n" +
                $"- Finding best audio stream: {Timings[3]} ms\n" +
                $"- Getting extension: {Timings[4]} ms\n" +
                $"Download: {Timings[5]} ms");
        }
    }
}
