# Serble Games GDScript SDK (Godot 4.6+)

Single-file GDScript client that speaks the same protocol as `serble-games-sdk` and communicates with `Scripts/GameManagementServer.cs` over localhost TCP.

## Usage

Add `Scripts/SerbleGamesSdk.gd` to your project and call the static API:

```gdscript
var result := SerbleGamesSdk.serble_games_init("your_game_id")
if result == 0:
    print("Handshake ok", SerbleGamesSdk.last_handshake_logged_in, SerbleGamesSdk.last_handshake_username)

var grant := SerbleGamesSdk.serble_games_grant_achievement("your_game_id", "achievement_id")
print("Grant result:", grant)
```

Optional helper:

```gdscript
var outcome := SerbleGamesSdk.run_self_test("your_game_id", "achievement_id")
print(outcome)
```

## Notes

- Returns `0` on success, `1` on failure (mirrors the C SDK).
- Uses a short connect/read timeout (default 2000ms).
