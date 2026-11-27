using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;

using Wisp.Gateway.Features.JoinRoles;

namespace Wisp.Gateway.Tests;

/// <summary>
/// NetCord validates command and component definitions only when modules are loaded at startup, so a bad one
/// (like a custom ID containing the parameter separator) builds fine and then crashes the bot. Load them here instead.
/// </summary>
public sealed class ModuleDefinitionTests
{
    private static readonly System.Reflection.Assembly Gateway = typeof(JoinRolesExtensions).Assembly;

    [Fact]
    public void Application_command_modules_are_valid()
    {
        var exception = Record.Exception(() => new ApplicationCommandService<ApplicationCommandContext>().AddModules(Gateway));
        Assert.Null(exception);
    }

    [Fact]
    public void Role_menu_modules_are_valid()
    {
        var exception = Record.Exception(() => new ComponentInteractionService<RoleMenuInteractionContext>().AddModules(Gateway));
        Assert.Null(exception);
    }

    [Fact]
    public void Button_modules_are_valid()
    {
        var exception = Record.Exception(() => new ComponentInteractionService<ButtonInteractionContext>().AddModules(Gateway));
        Assert.Null(exception);
    }
}
