# Getting started

Wisp runs as one container next to Postgres. You need [Docker](https://docs.docker.com/get-docker/) and a Discord bot of your own, which takes a few minutes to create.

## 1. Create the bot

1. Open the [Discord Developer Portal](https://discord.com/developers/applications) and click **New Application**. The name you pick is the name your bot will have.
2. Under **Bot**, click **Reset Token** and copy it. Treat it like a password: anyone with it can act as your bot.
3. Still under **Bot**, in **Privileged Gateway Intents**, turn on **Presence Intent**, **Server Members Intent** and **Message Content Intent**.

::: warning
Wisp asks Discord for every gateway intent. If any of the three above is off, Discord refuses the connection and Wisp won't start.
:::

## 2. Run it

Put the token and a database password in a `.env` file:

```sh
# .env
DISCORD_TOKEN=your-bot-token
POSTGRES_PASSWORD=change-me # e.g. from `openssl rand -hex 32`
```

And this next to it as `docker-compose.yaml`:

```yaml-vue
services:
  wisp:
    image: ghcr.io/nullmonkeys/wisp:{{ $theme.imageTag }}
    restart: unless-stopped
    environment:
      Discord__Token: ${DISCORD_TOKEN}
      ConnectionStrings__database: Host=postgres;Database=wisp;Username=wisp;Password=${POSTGRES_PASSWORD}
    depends_on:
      postgres:
        condition: service_healthy

  postgres:
    image: postgres:18
    restart: unless-stopped
    environment:
      POSTGRES_USER: wisp
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: wisp
    volumes:
      - postgres-data:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U wisp -d wisp"]
      interval: 5s
      timeout: 5s
      retries: 10

volumes:
  postgres-data:
```

Start it:

```sh
docker compose up -d
docker compose ps # wisp shows as healthy once it's connected to Discord
```

Wisp creates its database tables on first start, so there's nothing else to set up. Every other setting is optional; see [Configuration](./configuration).

The Wisp image has its own health check, so there's no `healthcheck` for it above. It reports healthy once the database is reachable and Wisp is connected to Discord. If it stays unhealthy, `docker compose logs wisp` tells you why.

## 3. Invite it

Find your **Application ID** under **General Information** in the developer portal, put it into this link and open it:

```
https://discord.com/oauth2/authorize?client_id=YOUR_APPLICATION_ID&scope=bot+applications.commands&permissions=300944400
```

That asks for these permissions:

| Permission                                                  | Why                                                                                   |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| Manage Channels                                             | Create and delete temporary voice channels and their category                         |
| Manage Roles                                                | Give join roles, and set permissions on temporary channels                            |
| Move Members                                                | Move people into their new channel                                                    |
| View Channels, Connect, Speak, Mute Members, Deafen Members | Hand these to channel owners. Discord only lets a bot grant permissions it has itself |
| Send Messages                                               | Announce the new owner in a temporary channel's chat when it's handed over           |

::: tip
Wisp can only give out roles that sit **below its own role**. After inviting it, open **Server Settings → Roles** and drag Wisp's role above any role you want it to hand out.
:::

## 4. Set it up

Each feature is managed from one command, which opens a settings card only you can see:

- `/temp-voice` turns [temporary voice channels](../features/temp-voice) on or off.
- `/join-roles` picks the [roles new members get](../features/join-roles) and turns them on.

Only server admins see these commands.

## Updating

The example above pins the newest release, so you choose when to update. Check the [release notes](https://github.com/NullMonkeys/Wisp/releases) for what changed, put the new version in `image:`, then:

```sh
docker compose pull
docker compose up -d
```

Database changes are applied automatically when Wisp starts.

If you'd rather not pin a version, `ghcr.io/nullmonkeys/wisp:latest` always follows the newest stable release, and a tag like `0.0` follows patch releases of one minor version.

::: tip Backups
Everything Wisp remembers lives in Postgres. To take a backup:

```sh
docker compose exec postgres pg_dump -U wisp wisp > wisp.sql
```

:::
