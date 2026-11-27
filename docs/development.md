# Development

Wisp is a .NET app built on [NetCord](https://netcord.dev), with Postgres through Entity Framework Core and [Aspire](https://aspire.dev) for running it locally.

## Requirements

- [.NET 11 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://aspire.dev)
- Docker
- A Discord bot for testing, set up like in [Getting started](./guide/getting-started#_1-create-the-bot). Use a separate one from your real bot.

## Running it

```sh
aspire run
```

This starts Postgres (with [pgweb](https://github.com/sosedoff/pgweb) to browse it), the [REST proxy](./guide/scaling#share-rate-limits) and the bot, and opens the Aspire dashboard with logs, traces and metrics for all of them. Aspire asks for the bot token the first time.

## Tests

```sh
dotnet test
```

Tests run against a real Postgres, which Aspire starts in Docker, so Docker has to be running.

## Project layout

| Path                        | What's in it                                                                                             |
| --------------------------- | -------------------------------------------------------------------------------------------------------- |
| `src/Wisp.Gateway`          | The bot itself. Each feature lives in `Features/`, with its commands, gateway events and services.       |
| `src/libs/Wisp.Discord`     | NetCord setup shared by features: interaction handling, settings cards, REST helpers and the REST proxy. |
| `src/libs/Wisp.Persistence` | The database: entities, configuration and migrations.                                                    |
| `aspire/`                   | The Aspire app host for local runs, and shared service defaults like telemetry and health checks.        |
| `tests/`                    | Tests.                                                                                                   |
| `docs/`                     | This site.                                                                                               |

## Docs

The docs are a [VitePress](https://vitepress.dev) site in `docs/`. They need two more tools:

- [Node.js](https://nodejs.org) 26. The version CI uses is in `docs/.node-version`, which version managers like fnm, nvm and mise pick up.
- [pnpm](https://pnpm.io/installation). Any recent version works: it switches to the version pinned in `docs/package.json` (12.15.0) on its own.

Then:

```sh
cd docs
pnpm install
pnpm dev
```

## Releases

Pushing a `v*.*.*` tag runs the tests, then builds and publishes the container image and creates a GitHub release. Stable versions also publish these docs, once the image is out, since they show its tag. Versions with a suffix, like `v1.0.0-beta.1`, are prereleases: they get an image and a release, but don't update `latest` or the docs.
