# Wisp

A small, self-hosted Discord bot.

It started because we needed temporary voice channels and didn't want to rely on a third-party bot for that, so we built our own. More small features will follow.

## Features

- **Temporary voice channels**: join a channel, get your own. It disappears once everyone leaves, and remembers your settings for next time.

## Running

You need a [Discord bot token](https://discord.com/developers/applications) and Docker.

```sh
# .env
DISCORD_TOKEN=your-bot-token
POSTGRES_PASSWORD=change-me # e.g. from `openssl rand -hex 32`
```

```yaml
# docker-compose.yaml
services:
  wisp:
    image: ghcr.io/nullmonkeys/wisp:latest
    restart: unless-stopped
    environment:
      Discord__Token: ${DISCORD_TOKEN}
      ConnectionStrings__database: Host=postgres;Database=wisp;Username=wisp;Password=${POSTGRES_PASSWORD}
      # Optional: sharding, only needed to split a large bot across several instances.
      # This instance runs shards Start to End - 1 (End is exclusive) out of TotalShardCount.
      # Discord__TotalShardCount: 1
      # Discord__ShardRange__Start: 0
      # Discord__ShardRange__End: 1
    depends_on:
      - postgres

  postgres:
    image: postgres:18
    restart: unless-stopped
    environment:
      POSTGRES_USER: wisp
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: wisp
    volumes:
      - postgres-data:/var/lib/postgresql

volumes:
  postgres-data:
```

```sh
docker compose up -d
```

`latest` is the latest release; `edge` follows `main`.

When inviting the bot, give it **Manage Channels**, **Manage Roles**, **Move Members**, **Mute Members**, **Deafen Members**, **View Channels**, **Connect** and **Speak**. To turn on temporary voice channels, run `/temp-voice enable` in your server.

## Development

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download), the [Aspire CLI](https://aspire.dev) and Docker.

```sh
aspire run    # starts Postgres and the bot
dotnet test
```

## License

[MIT](LICENSE). Fork it, host it, break it.
