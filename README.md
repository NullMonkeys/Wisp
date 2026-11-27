<p align="center">
  <img src="docs/public/wisp-logo-rounded.png" alt="Wisp" width="128" height="128">
</p>

<h1 align="center">Wisp</h1>

<p align="center">
  A small, self-hosted Discord bot
  <br>
  <a href="https://wisp.docs.nullmonkeys.com">Docs</a> · <a href="https://wisp.docs.nullmonkeys.com/guide/getting-started">Getting started</a> · <a href="https://github.com/NullMonkeys/Wisp/releases">Releases</a>
</p>

It started because we needed temporary voice channels and didn't want to rely on a third-party bot for that, so we built our own. More small features will follow.

## Features

- **Temporary voice channels**: join a channel, get your own. It disappears once everyone leaves, and remembers your settings for next time.
- **Join roles**: give new members up to 25 roles when they join, or once they pass membership screening.

## Running

Wisp runs as one container next to Postgres. Setup, configuration and how each feature works are in the **[docs](https://wisp.docs.nullmonkeys.com)**:

- [Getting started](https://wisp.docs.nullmonkeys.com/guide/getting-started): create the bot, run it with Docker Compose, invite it
- [Configuration](https://wisp.docs.nullmonkeys.com/guide/configuration): every setting in one table
- [Running several instances](https://wisp.docs.nullmonkeys.com/guide/scaling): sharding and shared rate limits

## Development

Requires the [.NET 11 SDK](https://dotnet.microsoft.com/download), the [Aspire CLI](https://aspire.dev) and Docker.

```sh
aspire run    # starts Postgres, the REST proxy and the bot
dotnet test
```

More in [Development](https://wisp.docs.nullmonkeys.com/development).

## License

[MIT](LICENSE) Fork it, host it, break it.
