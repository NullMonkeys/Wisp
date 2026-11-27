namespace Wisp.Gateway.Features.TempVoice;

public interface ITempVoiceChannelsService
{
    Task UserJoinedAsync(
        ulong guildId,
        ulong userId,
        ulong channelId,
        string? displayName,
        CancellationToken ct = default);

    Task UserLeftAsync(
        ulong guildId,
        ulong channelId,
        ulong userId,
        CancellationToken ct = default);

    Task SavePreferencesAsync(
        ulong channelId,
        TempChannelSettings before,
        TempChannelSettings after,
        CancellationToken ct = default);

    Task<TransferResult> TransferOwnershipAsync(
        ulong guildId,
        ulong ownerId,
        ulong newOwnerId,
        CancellationToken ct = default);

    Task<bool> ForgetChannelAsync(
        ulong channelId,
        CancellationToken ct = default);

    Task DeleteAllAsync(
        ulong guildId,
        CancellationToken ct = default);

    Task<int> CountActiveAsync(
        ulong guildId,
        CancellationToken ct = default);

    Task ReconcileAsync(
        ulong guildId,
        IReadOnlySet<ulong> existingChannelIds,
        IReadOnlySet<ulong> occupiedChannelIds,
        CancellationToken ct = default);
}
