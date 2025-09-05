using Godot;
using InterplanetaryPostieGD.Assets.Scripts.Terrain.Quadtree;
using Orbits;

namespace InterplanetaryPostieGD.Assets.Scripts.Terrain;

[Tool]
public partial class CelestialTerrain : Node3D
{
#region Variables

	[Export] public double             radius       { get; private set; }
	[Export] public double             maxElevation { get; private set; }
	[Export] public double             axialTilt    { get; private set; }
	[Export] public Node3D             focus        { get; private set; }
	[Export] public FastNoiseLite      noise        { get; private set; }
	[Export] public float              noiseGain    { get; private set; }
	[Export] public StandardMaterial3D material     { get; private set; }
	public          Axis               axis         { get; private set; }

#endregion

#region Godot Methods

	public override void _EnterTree() {
		axis = new Axis { // An axis rotated around global right by axial tilt
			Up      = Vector3.Up.Rotated(Vector3.Right, double.DegreesToRadians(axialTilt)),
			Right   = Vector3.Right,
			Forward = Vector3.Forward.Rotated(Vector3.Right, double.DegreesToRadians(axialTilt))
		};

		if (noise == null) noise = new FastNoiseLite();
	}

#endregion
}