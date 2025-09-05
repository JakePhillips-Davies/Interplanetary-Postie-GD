using Godot;
using System;

namespace Orbits
{
	public partial class CelestialObject : Node3D
	{
		[ExportCategory("Refrences")]
		[Export] public OrbitDriver orbitDriver   { get; private set; }
		[Export] public OrbitDisplay orbitDisplay { get; private set; }

		[ExportCategory("Params")]
		[Export] public double mass                   { get; private set; }
		[Export] public double radius                 { get; private set; }
		[Export] public double gravitationalParameter { get; private set; }
		[Export] public double soiDistance            { get; private set; }

		[ExportCategory("Initial State")]
		[Export] public Vector3 InitPos;

		[Export] public Vector3 InitVel;

		private OrbitSettingsSingleton orbitSettings;


//--#

#region Godot methods

		public override void _EnterTree() {
			orbitSettings          = OrbitSettingsSingleton.inst;
			gravitationalParameter = mass * ExtraMaths.G;

			var s = DebugDraw3D.ScopedConfig().SetThickness(0);
		}

		public override void _Ready() {
			ReCalcSoi();
		}

#endregion

//--#


//--#

#region Misc functions

		public void ReCalcSoi() {
			if (orbitDriver != null)
				soiDistance = orbitDriver.orbits[0].Periapsis * Math.Pow(mass / orbitDriver.orbits[0].Parent.mass, 0.4);
			else soiDistance = double.PositiveInfinity;
		}

		public void EditorInit() {
			Position               = InitPos;
			gravitationalParameter = mass * ExtraMaths.G;
		}

#endregion

//--#
	}
}