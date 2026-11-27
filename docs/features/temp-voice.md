# Temporary voice channels

Join a channel, get your own. It disappears once everyone leaves, and remembers your settings for next time.

## Turning it on

Run `/temp-voice` and press **Enable**. Wisp creates a **Temporary Voice** category with one voice channel in it, **[+] Create channel**.

The card also shows how many temporary channels are open right now. You can rename the category and the create channel, or move them around.

## How members use it

1. Join **[+] Create channel**.
2. Wisp creates a channel named after you, like **Alex's channel**, in the category and moves you into it. It's yours.
3. Once the last person leaves, the channel is deleted.

Everyone gets one channel at a time. Joining **[+] Create channel** while yours still exists moves you back into it.

### What owners can do

In their own channel, the owner can:

- rename it, and change the user limit, bitrate and region
- edit its permissions, for example to lock it or let only friends in
- mute, deafen, move and disconnect people

### Remembered settings

Wisp remembers the name, user limit, bitrate and permission changes of each member's channel, and applies them to their next one in the same server. Changes made by an admin to someone's channel are remembered for its owner too.

If Discord no longer accepts saved settings, for example a bitrate above what the server's boost level allows, the new channel uses the defaults instead.

## Permissions

New channels start with the category's permissions. To control who can see and join temporary channels, set that up on the **Temporary Voice** category.

Saved permission changes are stored as differences from the category, so if you change the category later, members' next channels still pick it up.

Wisp needs **Manage Channels**, **Manage Roles** and **Move Members** for this feature, plus **View Channels**, **Connect**, **Speak**, **Mute Members** and **Deafen Members** to hand them to owners. The [invite link](../guide/getting-started#_3-invite-it) asks for all of them.

## Turning it off

Run `/temp-voice` and press **Disable**.

::: warning
Disabling deletes the category, the create channel and **every open temporary channel**. Anyone still in one is disconnected. Members' remembered settings are kept for when you turn it back on.
:::

## When things go wrong

- **Someone deletes the category or the create channel.** Wisp turns the feature off and cleans up. Press **Enable** to set it up again.
- **Wisp was offline.** When it reconnects, it deletes temporary channels that emptied in the meantime and forgets the ones that were deleted.
- **A member leaves [+] Create channel before Wisp moves them.** The new channel is deleted straight away, so nothing is left behind.
