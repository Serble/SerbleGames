extends Node


func _ready() -> void:
	# Stop window close from killing app
	for connection in get_viewport().get_window().close_requested.get_connections():
		get_viewport().get_window().close_requested.disconnect(connection.callable)
