extends DirectionalLight3D

@export var target : Node3D
@export var baseEnergy : float

# 1 AU, thus the light will have baseEnergy energy at Earth's distance
var baseDistance : float = 1.496e+11
var baseDistanceSqr : float

var distanceSqr : float

func _ready() -> void:
	baseDistanceSqr = baseDistance * baseDistance

func _physics_process(delta: float) -> void:
	if target != null:
		look_at(target.global_position)
		
		distanceSqr = (global_position - target.global_position).length_squared()
		light_energy = baseEnergy * (baseDistanceSqr / distanceSqr)
