# Serble Games GDScript SDK (Godot 4.6+)

Single-file GDScript client that speaks the same protocol as `serble-games-sdk` and communicates with `Scripts/GameManagementServer.cs` over localhost TCP.

## Usage

Add `Scripts/SerbleGamesSdk.gd` to your project.

### Initialise once at game start

```gdscript
func _ready() -> void:
    var result := SerbleGamesSdk.serble_games_init("your_game_id")
    if result == 0:
        print("Connected – logged in: ", SerbleGamesSdk.last_handshake_logged_in,
              "  user: ", SerbleGamesSdk.last_handshake_username)
    else:
        print("Launcher not running or handshake failed")
```

The connection is kept open for the lifetime of the process.

### Grant achievements using the live connection

```gdscript
var ok := SerbleGamesSdk.serble_games_grant_achievement("your_game_id", "achievement_id")
print("Grant result:", ok)   # 0 = success, 1 = failure
```

If the connection has dropped since `init` was called (e.g. launcher restarted), it reconnects and re-handshakes automatically before sending the packet.

### Disconnect on exit

```gdscript
func _notification(what: int) -> void:
    if what == NOTIFICATION_WM_CLOSE_REQUEST:
        SerbleGamesSdk.serble_games_disconnect()
```

### Check connection status

```gdscript
if SerbleGamesSdk.is_connected_to_launcher():
    print("Still connected")
```

### Self-test helper

```gdscript
# Performs init + grant in one call, reusing the single connection.
var outcome := SerbleGamesSdk.run_self_test("your_game_id", "achievement_id")
print(outcome)
```

## API reference

| Function | Returns | Description |
|---|---|---|
| `serble_games_init(game_id, timeout_ms?)` | `int` | Connect + handshake. Stores the connection for reuse. |
| `serble_games_grant_achievement(game_id, achievement_id, timeout_ms?)` | `int` | Grant an achievement over the live connection. Auto-reconnects if needed. |
| `serble_games_disconnect()` | `void` | Explicitly close the connection. |
| `is_connected_to_launcher()` | `bool` | True if the TCP connection is currently open. |
| `serble_games_sdk_init(...)` | `int` | Alias for `serble_games_init`. |
| `run_self_test(game_id, achievement_id, timeout_ms?)` | `Dictionary` | Init + grant in one call, reusing the connection. |

## Notes

- Returns `0` on success, `1` on failure (mirrors the C SDK).
- The default timeout is 2000 ms per operation.
- `last_handshake_logged_in` and `last_handshake_username` are set after a successful handshake.
- All public function signatures are identical to the previous stateless version — no call-site changes required.