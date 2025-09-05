using Godot;
using System.Collections.Generic;

namespace Orbits {

public partial class OrbitDisplay : Node
{
//--#
    #region Variables
    

    [Export] public CelestialObject celestialObject {get; private set;}
    [Export] public Color lineCol {get; private set;} = Color.FromHsv(0, 1, 1);
    
    public List<OrbitConicLine> OrbitConicLines;

    private DebugDraw3DScopeConfig drawConfig;
    private OrbitSettingsSingleton orbitSettings;


    #endregion
//--#



//--#
    #region Godot events


    public override void _EnterTree() {
        drawConfig = DebugDraw3D.ScopedConfig().SetThickness(0);
        OrbitConicLines = new();
        
        orbitSettings = OrbitSettingsSingleton.inst;
    }

    public override void _PhysicsProcess(double _delta) {
        
        if (orbitSettings.showVelocityDir) // Draw velocity arrow
            DebugDraw3D.DrawArrow(celestialObject.GlobalPosition, celestialObject.GlobalPosition + celestialObject.orbitDriver.CurrentPoint.Velocity.Normalized() * 100000000, lineCol, 0.05f);

        Vector3 offset = Vector3.Zero;
        double time = UniversalTimeSingleton.inst.time;
        
        if (orbitSettings.showOrbits) // Draw the orbit
            foreach (OrbitConicLine orbitConicLine in OrbitConicLines) {

                offset = orbitConicLine.Orbit.Parent.GlobalPosition;
                
                for (int i = 0; i < orbitConicLine.Points.Length - 1; i++) {
                    
                    if (orbitConicLine.Points[i].Time < time && orbitConicLine.Points[i + 1].Time > time) { // When the current position is between two points
                        Vector3 pos = celestialObject.orbitDriver.GetCartesianAtTime(time).position;

                        // Draw two lines from the first point, to the current pos, to the second point
                        DebugDraw3D.DrawLine(orbitConicLine.Points[i].Position + offset, pos + offset, lineCol);
                        DebugDraw3D.DrawLine(pos + offset, orbitConicLine.Points[i + 1].Position + offset, lineCol);

                    }
                    
                    else
                        DebugDraw3D.DrawLine(orbitConicLine.Points[i].Position + offset, orbitConicLine.Points[i + 1].Position + offset, lineCol);
                }

            }
        
        
        if (orbitSettings.showSoi && double.IsFinite(celestialObject.soiDistance)) // Draw sphere of influence
            DebugDraw3D.DrawSphere(celestialObject.GlobalPosition, (float)celestialObject.soiDistance, Color.Color8(113, 214, 227, 127));
    }


    #endregion
//--#



//--#
    #region Public methods


    public void ResetList(List<Orbit> _orbits) {
        OrbitConicLines.Clear();
        foreach (Orbit orbit in _orbits) {
            OrbitConicLines.Add(new(orbit, 128));
        }
    }


    #endregion
//--#

}

}