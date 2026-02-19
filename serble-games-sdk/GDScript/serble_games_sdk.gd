class_name SerbleGamesSdk
extends Object

const HOST := "127.0.0.1"
const PORT := 46599
const DEFAULT_TIMEOUT_MS := 2000

const PACKET_HANDSHAKE := 0
const PACKET_HANDSHAKE_RESPONSE := 1
const PACKET_ACK := 2
const PACKET_GRANT_ACHIEVEMENT := 3

static var last_handshake_logged_in: bool = false
static var last_handshake_username: String = ""

static func serble_games_init(game_id: String, timeout_ms: int = DEFAULT_TIMEOUT_MS) -> int:
	if game_id.is_empty():
		return 1

	var peer := StreamPeerTCP.new()
	if _connect(peer, timeout_ms) != OK:
		print("no connect")
		return 1

	if _send_handshake(peer, game_id) != OK:
		print("no handshake")
		peer.disconnect_from_host()
		return 1

	var packet := _read_packet(peer, timeout_ms)
	peer.disconnect_from_host()
	if packet.error != OK:
		print("no get handshake")
		return 1

	if packet.type == PACKET_HANDSHAKE_RESPONSE:
		_parse_handshake_response(packet.data)
		return 0

	return 1

static func serble_games_grant_achievement(game_id: String, achievement_id: String, timeout_ms: int = DEFAULT_TIMEOUT_MS) -> int:
	if game_id.is_empty() or achievement_id.is_empty():
		return 1

	var peer := StreamPeerTCP.new()
	if _connect(peer, timeout_ms) != OK:
		return 1

	if _send_handshake(peer, game_id) != OK:
		peer.disconnect_from_host()
		return 1

	var handshake_packet := _read_packet(peer, timeout_ms)
	if handshake_packet.error != OK or handshake_packet.type != PACKET_HANDSHAKE_RESPONSE:
		peer.disconnect_from_host()
		return 1

	_parse_handshake_response(handshake_packet.data)

	if _send_string_packet(peer, PACKET_GRANT_ACHIEVEMENT, achievement_id) != OK:
		peer.disconnect_from_host()
		return 1

	var ack_packet := _read_packet(peer, timeout_ms)
	peer.disconnect_from_host()
	if ack_packet.error != OK:
		return 1

	return 0 if ack_packet.type == PACKET_ACK else 1

static func serble_games_sdk_init(game_id: String, timeout_ms: int = DEFAULT_TIMEOUT_MS) -> int:
	return serble_games_init(game_id, timeout_ms)

static func run_self_test(game_id: String, achievement_id: String, timeout_ms: int = DEFAULT_TIMEOUT_MS) -> Dictionary:
	var init_result := serble_games_init(game_id, timeout_ms)
	var grant_result := serble_games_grant_achievement(game_id, achievement_id, timeout_ms)
	return {
		"init_result": init_result,
		"grant_result": grant_result,
		"logged_in": last_handshake_logged_in,
		"username": last_handshake_username
	}

static func _connect(peer: StreamPeerTCP, timeout_ms: int) -> int:
	var err := peer.connect_to_host(HOST, PORT)
	if err != OK:
		return err

	var start := Time.get_ticks_msec()
	while true:
		peer.poll()
		var status := peer.get_status()
		if status == StreamPeerTCP.STATUS_CONNECTED:
			return OK
		if status == StreamPeerTCP.STATUS_ERROR:
			return ERR_CONNECTION_ERROR
		if Time.get_ticks_msec() - start > timeout_ms:
			return ERR_TIMEOUT
		OS.delay_msec(5)
	return ERR_BUG

static func _send_handshake(peer: StreamPeerTCP, game_id: String) -> int:
	return _send_string_packet(peer, PACKET_HANDSHAKE, game_id)

static func _send_string_packet(peer: StreamPeerTCP, packet_type: int, value: String) -> int:
	var payload := _write_string(value)
	return _send_packet(peer, packet_type, payload)

static func _send_packet(peer: StreamPeerTCP, packet_type: int, payload: PackedByteArray) -> int:
	var packet_length := 4 + payload.size()
	var packet := PackedByteArray()
	packet.append_array(_write_int32_be(packet_length))
	packet.append_array(_write_int32_be(packet_type))
	packet.append_array(payload)
	return peer.put_data(packet)

static func _read_packet(peer: StreamPeerTCP, timeout_ms: int) -> Dictionary:
	var length_bytes := _read_exact(peer, 4, timeout_ms)
	if length_bytes.error != OK:
		return {"error": length_bytes.error, "type": -1, "data": PackedByteArray()}

	var packet_length := _read_int32_be(length_bytes.data, 0)
	if packet_length < 4:
		return {"error": ERR_INVALID_DATA, "type": -1, "data": PackedByteArray()}

	var packet_bytes := _read_exact(peer, packet_length, timeout_ms)
	if packet_bytes.error != OK:
		return {"error": packet_bytes.error, "type": -1, "data": PackedByteArray()}

	var packet_type := _read_int32_be(packet_bytes.data, 0)
	var payload := PackedByteArray()
	if packet_length > 4:
		payload = packet_bytes.data.slice(4, packet_length)

	return {"error": OK, "type": packet_type, "data": payload}

static func _read_exact(peer: StreamPeerTCP, count: int, timeout_ms: int) -> Dictionary:
	var start := Time.get_ticks_msec()
	var buffer := PackedByteArray()

	while buffer.size() < count:
		if peer.get_status() != StreamPeerTCP.STATUS_CONNECTED:
			return {"error": ERR_CONNECTION_ERROR, "data": PackedByteArray()}

		var available := peer.get_available_bytes()
		if available > 0:
			var to_read = min(count - buffer.size(), available)
			var result := peer.get_data(to_read)
			if result[0] != OK:
				return {"error": result[0], "data": PackedByteArray()}
			buffer.append_array(result[1])
		else:
			if Time.get_ticks_msec() - start > timeout_ms:
				return {"error": ERR_TIMEOUT, "data": PackedByteArray()}
			OS.delay_msec(5)

	return {"error": OK, "data": buffer}

static func _parse_handshake_response(payload: PackedByteArray) -> void:
	last_handshake_logged_in = false
	last_handshake_username = ""

	if payload.size() < 2:
		return

	last_handshake_logged_in = payload[0] != 0
	var has_username := payload[1] != 0
	if not has_username:
		return

	if payload.size() < 6:
		return

	var name_length := _read_int32_be(payload, 2)
	if name_length <= 0:
		return
	if payload.size() < 6 + name_length:
		return

	var name_bytes := PackedByteArray()
	name_bytes.resize(name_length)
	for i in range(name_length):
		name_bytes[i] = payload[6 + i]
	last_handshake_username = name_bytes.get_string_from_utf8()

static func _write_int32_be(value: int) -> PackedByteArray:
	var out := PackedByteArray()
	out.resize(4)
	out[0] = (value >> 24) & 0xFF
	out[1] = (value >> 16) & 0xFF
	out[2] = (value >> 8) & 0xFF
	out[3] = value & 0xFF
	return out

static func _read_int32_be(data: PackedByteArray, offset: int) -> int:
	return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]

static func _write_string(value: String) -> PackedByteArray:
	var bytes := value.to_utf8_buffer()
	var out := PackedByteArray()
	out.append_array(_write_int32_be(bytes.size()))
	out.append_array(bytes)
	return out
