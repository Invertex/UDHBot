using System;
using System.Reflection;
using Discord.Commands;

namespace DiscordBot.Tests.TestExtensions
{
    public static class ModuleExtensions
    {
        public static void SetContext(this ModuleBase module, ICommandContext context) 
        {
            var method = module.GetType().GetMethod("Discord.Commands.IModuleBase.SetContext", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(module, new object[] { context });
            }
        }
    }
}
