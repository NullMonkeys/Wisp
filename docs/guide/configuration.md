# Configuration

Wisp is configured with environment variables. The double underscore (`__`) separates sections, so `Discord__Token` is the `Token` setting in the `Discord` section.

## Settings

| Variable                                                 | Default       | Description                                                                                                                                                              |
| -------------------------------------------------------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Discord__Token`                                         | **required**  | Your bot's token.                                                                                                                                                        |
| `ConnectionStrings__database`                            | **required**  | Postgres connection string, like `Host=postgres;Database=wisp;Username=wisp;Password=...`.                                                                               |
| `Discord__TotalShardCount`                               | `1`           | How many shards the bot is split into across all instances. See [Running several instances](./scaling).                                                                  |
| `Discord__ShardRange__Start`                             | `0`           | First shard this instance runs.                                                                                                                                          |
| `Discord__ShardRange__End`                               | `1`           | Shard this instance stops before. The range is end-exclusive, so `0` to `1` runs shard 0 only.                                                                           |
| `Discord__RestProxy` <Badge type="tip" text="v0.0.3+" /> | none          | URL of a shared rate limiting proxy, like `http://rest-proxy`. Without it, Wisp talks to Discord directly. See [Running several instances](./scaling#share-rate-limits). |
| `Logging__LogLevel__Default`                             | `Information` | How much Wisp logs: `Debug`, `Information`, `Warning` or `Error`.                                                                                                        |
| `OTEL_EXPORTER_OTLP_ENDPOINT`                            | none          | Sends logs, traces and metrics to an [OpenTelemetry](https://opentelemetry.io/) collector at this address.                                                               |

## Health checks

Wisp listens on port `8080` inside the container, with two endpoints for uptime monitors or container orchestrators:

| Endpoint   | Healthy when                                                                           |
| ---------- | -------------------------------------------------------------------------------------- |
| `/healthz` | The process is running.                                                                |
| `/readyz`  | Every check passes, including the database and every shard being connected to Discord. |

Nothing else is served on that port, so it doesn't need to be reachable from the internet.

The container image already checks `/readyz` every 30 seconds, so `docker ps` and `docker compose ps` show whether Wisp is healthy without any setup.

## Data

Wisp stores each server's name and settings, the temporary channels it's tracking and the channel settings members chose.

When Wisp is removed from a server while it's running, everything it stored about that server is deleted. Discord only announces a removal once, so if Wisp was stopped at the time, that server's data stays in the database. It's never used again unless Wisp is added back, in which case the old settings come back too.
