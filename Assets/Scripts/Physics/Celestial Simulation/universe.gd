extends Node3D

@export var floatingOrigin : Node3D

func _physics_process(delta: float) -> void:
	if floatingOrigin != null:
		global_position -= floatingOrigin.global_position
