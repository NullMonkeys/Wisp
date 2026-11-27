using JetBrains.Annotations;

using NetCord;
using NetCord.Services.ApplicationCommands;

using Wisp.Discord.Interactions;

namespace Wisp.Gateway.Features.TempVoice.Modules;

[UsedImplicitly]
public class TempVoiceUserCommandsModule(ITempVoiceChannelsService channelsService) : WispCommandModule
{
    [UsedImplicitly]
    [UserCommand("Make temp channel owner", Contexts = [InteractionContextType.Guild])]
    public async Task TransferOwnership()
    {
        ArgumentNullException.ThrowIfNull(Context.Guild);
        var target = ((UserCommandInteraction)Context.Interaction).Data.TargetUser;

        if (target.IsBot)
        {
            await ReplyAsync(TempVoiceCards.BotCantOwn());
            return;
        }

        var result = await channelsService.TransferOwnershipAsync(Context.Guild.Id, Context.User.Id, target.Id);
        await ReplyAsync(TempVoiceCards.Transfer(result, target.Id));
    }
}
