# Join roles

Give new members up to 25 roles when they join, or once they pass membership screening.

## Setting it up

1. Run `/join-roles`.
2. Pick the roles in the menu. Every change is saved straight away.
3. Press **Enable**. It stays greyed out until at least one role is picked.

To stop giving roles, press **Disable**. Your picked roles are kept for next time.

## Who gets the roles

- **New members**, as soon as they join.
- **Members behind [membership screening](https://support.discord.com/hc/en-us/articles/1500000466882)**, once they pass it and not before.
- **Not bots.** Bots joining the server are skipped.

Roles are only given on joining. Members who were already in the server when you turned it on don't get them, and Wisp only adds roles, never removes any.

## Roles Wisp can't give

When you pick a role Wisp can't give, the card skips it and tells you why:

| Reason                                | Fix                                                                                       |
| ------------------------------------- | ----------------------------------------------------------------------------------------- |
| Everyone already has it               | That's `@everyone`, which every member has anyway.                                        |
| It's managed by an integration or bot | Roles that belong to a bot or an integration can't be given by hand. Pick another role.   |
| It's not below my highest role        | Open **Server Settings → Roles** and drag Wisp's role above it. Then pick the role again. |
| Only 25 roles are allowed             | Discord's role menus are limited to 25.                                                   |

The card also warns you if Wisp is missing **Manage Roles**, since it can't give any role without it.

If a role is deleted, Wisp removes it from the list on its own.

::: warning
If a role is moved above Wisp's role after you picked it, or Wisp loses **Manage Roles**, new members get **none** of the roles until you fix it. Wisp gives all the roles in one go, and Discord rejects the whole change.
:::

## Other bots

Don't let another bot give roles on join at the same time. If both change a new member's roles at the same moment, one bot's roles can get lost.
