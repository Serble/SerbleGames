# serble-games-sdk

C SDK helper for connecting to the local Serble Games management server.

## Handshake

`serble_games_init(game_id)` connects to `127.0.0.1:46599`, sends a handshake packet, waits for a response, and returns `0` on success or `1` on error.

The caller must supply the game ID string.

## Grant Achievement

`serble_games_grant_achievement(game_id, achievement_id)` performs a handshake, sends a grant request, waits for an ack, and returns `0` on success or `1` on error.

## Build

This project uses CMake and builds a static library by default. To build the optional handshake example:

- Configure with `-DSERBLE_GAMES_SDK_BUILD_EXAMPLE=ON`
- Build the `serble_games_handshake_example` target

## Example

The example accepts a game ID and an optional achievement ID:

- `serble_games_handshake_example <game_id>` performs only the handshake.
- `serble_games_handshake_example <game_id> <achievement_id>` grants the achievement.

## Example Project

A standalone example project is available under `example_project/` and demonstrates handshake + grant with a single argument.