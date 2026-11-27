# Running several instances

One instance is enough for most bots. This page is for when it isn't: Discord requires sharding once a bot is in 2,500 servers, and you may want to spread shards over several containers or machines.

## Split shards across instances

Discord splits a bot's servers into shards, and each server always belongs to the same shard. Every instance runs a range of shards out of the total:

- `Discord__TotalShardCount` is the total, and must be the same on every instance.
- `Discord__ShardRange__Start` and `Discord__ShardRange__End` pick this instance's shards. The end is exclusive.

For example, 4 shards over 2 instances:

| Instance | `TotalShardCount` | `ShardRange__Start` | `ShardRange__End` | Runs shards |
| -------- | ----------------- | ------------------- | ----------------- | ----------- |
| wisp-0   | `4`               | `0`                 | `2`               | 0, 1        |
| wisp-1   | `4`               | `2`                 | `4`               | 2, 3        |

All instances share one database. Make sure the ranges cover every shard exactly once.

## Share rate limits <Badge type="tip" text="v0.0.3+" /> {#share-rate-limits}

Discord limits how fast a bot can call its API, per bot token. Each instance keeps its own count, so several instances sharing a token can run past the limits together.

Put a [twilight http-proxy](https://github.com/twilight-rs/http-proxy) in front of the API to keep one count for all of them, and point every instance at it with `Discord__RestProxy`:

```yaml-vue
services:
  wisp-0:
    image: ghcr.io/nullmonkeys/wisp:{{ $theme.imageTag }}
    restart: unless-stopped
    environment:
      Discord__Token: ${DISCORD_TOKEN}
      ConnectionStrings__database: Host=postgres;Database=wisp;Username=wisp;Password=${POSTGRES_PASSWORD}
      Discord__TotalShardCount: 4
      Discord__ShardRange__Start: 0
      Discord__ShardRange__End: 2
      Discord__RestProxy: http://rest-proxy
    depends_on:
      postgres:
        condition: service_healthy
      rest-proxy:
        condition: service_started

  wisp-1:
    # same as wisp-0, except:
    #   Discord__ShardRange__Start: 2
    #   Discord__ShardRange__End: 4

  rest-proxy:
    image: ghcr.io/twilight-rs/http-proxy:latest
    restart: unless-stopped
    environment:
      DISCORD_TOKEN: ${DISCORD_TOKEN}

  # postgres as in Getting started
```

::: warning
Run exactly **one** proxy per bot token. Two proxies would each keep their own count, which is the problem the proxy solves.
:::

The `ghcr.io/twilight-rs/http-proxy:metrics` image also serves [Prometheus](https://prometheus.io/) metrics on `/metrics`, if you want to watch how close you get to the limits.

If the proxy is down, every instance loses access to Discord's API: commands fail and channels aren't created until it's back. `restart: unless-stopped` covers most of that.
