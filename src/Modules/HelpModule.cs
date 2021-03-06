﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Astramentis.Attributes;

namespace Astramentis.Modules
{
    [Name("Help")]
    [Summary("You are here.")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        public CommandService CommandService { get; set; }
        private readonly IConfigurationRoot _config;

        private string commandPrefix;

        public HelpModule(IConfigurationRoot config)
        {
            _config = config;

            commandPrefix = _config["prefix"];
        }


        [Command("helpmod")]
        [Summary("Displays help for a specific module")]
        [Syntax("helpmod {module}")]
        [Example("helpmod market")]
        public async Task HelpModuleAsync(string requestedModule)
        {
            // crem-check
            if (requestedModule == "{module}")
            {
                await ReplyAsync("Don't be a dingus, you dingus.");
                return;
            }
            
            var builder = new EmbedBuilder()
            {
                Color = Color.Blue,
                //Description = "These are the commands you can use."
            };

            var module = CommandService.Modules.FirstOrDefault(x => x.Name.ToLower() == requestedModule.ToLower());

            // mistyped module
            if (module == null)
            {
                await ReplyAsync("There's no module by that name. Check your spelling, or use the `help` command.");
                return;
            }

            foreach (var cmd in module.Commands)
            {
                // figure out if the user can run this command
                var result = await cmd.CheckPreconditionsAsync(Context);

                // get custom example attribute for this command
                var example = cmd.Attributes.OfType<ExampleAttribute>().FirstOrDefault();
                var syntax = cmd.Attributes.OfType<SyntaxAttribute>().FirstOrDefault();

                StringBuilder descriptionBuilder = new StringBuilder();

                // if example attribute exists, append it to summary
                descriptionBuilder.Append(cmd.Summary);
                descriptionBuilder.AppendLine();
                if (syntax != null && syntax.SyntaxText != "")
                    descriptionBuilder.AppendLine($" - Syntax: *{syntax.SyntaxText}*");
                if (example != null && example.ExampleText != "")
                    descriptionBuilder.AppendLine($" - Example: *{example.ExampleText}*");
                

                // if user can run this command, build a field for it and add it into the response
                if (result.IsSuccess)
                {
                    builder.AddField(x =>
                    {
                        x.Name = $"{commandPrefix}{cmd.Aliases.First()}";
                        x.Value = $"{descriptionBuilder}";
                        x.IsInline = false;
                    });
                }
                    
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("help")]
        [Summary("Displays a list of command modules.")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder()
            {
                Color = Color.Blue,
                Description = $"These are the command modules you have access to. Use {commandPrefix}helpmod {{module}} to see the commands in each."
            };
            
            foreach (var module in CommandService.Modules.Where(x => x.Summary.Any()))
            {
                builder.AddField(x =>
                {
                    x.Name = module.Name;
                    x.Value = module.Summary;
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
