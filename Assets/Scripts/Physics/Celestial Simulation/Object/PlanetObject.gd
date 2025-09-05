extends Node3D

@export var rotationPeriod : float

func _physics_process(delta: float) -> void:
	var scene = get_tree().current_scene
	var time : float = 0
	if scene != null: time = scene.get_node("Time").timeDelta
	
	var angle = 2 * PI * (fposmod(time, rotationPeriod) / rotationPeriod)
	rotate_object_local(Vector3.UP, angle)
