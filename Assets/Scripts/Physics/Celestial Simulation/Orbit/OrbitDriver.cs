using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Orbits {

public partial class OrbitDriver : Node
{    
//--#
    #region Variables


    [Export] public CelestialObject celestialObject {get; private set;}
    
    public List<Orbit> orbits {get; private set;} = new List<Orbit>(10);

    public OrbitPoint CurrentPoint;

    private OrbitSettingsSingleton orbitSettings;


    #endregion
//--#



//--#
    #region Godot events
    
    public override void _EnterTree() {
        orbitSettings = OrbitSettingsSingleton.inst;

        orbits = [];
        
        RecalculateOrbit(celestialObject.InitPos, celestialObject.InitVel, celestialObject.GetParent<CelestialObject>());

        celestialObject.TranslateObjectLocal(orbits[0].GetCartesianAtTime(UniversalTimeSingleton.inst.time).localPos);
    }

    public override void _PhysicsProcess(double _delta) {

        // Stopwatch watch = new();
        // watch.Start();

        var res = GetCartesianAtTime(UniversalTimeSingleton.inst.time, out int index);
        CurrentPoint = new(res.position, res.velocity, orbits[index].GetTrueAnomalyAtTime(UniversalTimeSingleton.inst.time), UniversalTimeSingleton.inst.time);
        celestialObject.Position = CurrentPoint.Position;

        if (index > 0) {
            Orbit newOrbit = orbits[index];
            orbits.Clear();

            celestialObject.InitPos = CurrentPoint.Position;
            celestialObject.InitVel = CurrentPoint.Velocity;

            celestialObject.Reparent(newOrbit.Parent);

            GD.PrintS(celestialObject.Name + " has patched into " + newOrbit.Parent.Name);

            return;
        }

        // RecalculateOrbit(celestialObject.initPos, celestialObject.initVel, celestialObject.GetParent<CelestialObject>());
        RecalculateOrbit(CurrentPoint.Position, CurrentPoint.Velocity, celestialObject.GetParent<CelestialObject>());
        RecalculatePatchedConics();

        // watch.Stop();
        // GD.Print($"{celestialObject.Name} took {(double)watch.ElapsedTicks * 1000 / Stopwatch.Frequency} ms");
        
    }


    #endregion
//--#



//--#
    #region Setup


    public void RecalculateOrbit(double _periapsis, double _eccentricity, double _inclination, double _rightAscensionOfAscendingNode, double _argumentOfPeriapsis, double _trueAnomaly, CelestialObject _parent) {
        orbits.Clear();
        orbits.Insert(0, new(_periapsis, _eccentricity, _inclination, _rightAscensionOfAscendingNode, _argumentOfPeriapsis, _trueAnomaly, _parent, UniversalTimeSingleton.inst.time));
    }
    public void RecalculateOrbit(Vector3 _pos, Vector3 _vel, CelestialObject _parent) {
        orbits.Clear();
        orbits.Insert(0, new Orbit(_pos, _vel, _parent, UniversalTimeSingleton.inst.time)); 
    }


    #endregion
//--#



//--#
    #region Orbit stuff


    public int GetOrbitindexAtTime(double _time) {
        for (int i = 0; i < orbits.Count; i++) {
            if (_time < orbits[i].OrbitEndTime) {
                return i;
            }
            else if (i == orbits.Count - 1) { // return the last one if reached
                return i;
            }
        }
        return -1;
    }

    public (Vector3 position, Vector3 velocity) GetCartesianAtTime(double _time) {
        return orbits[GetOrbitindexAtTime(_time)].GetCartesianAtTime(_time);
    }
    public (Vector3 position, Vector3 velocity) GetCartesianAtTime(double _time, out int _index) {
        _index = GetOrbitindexAtTime(_time);
        return orbits[_index].GetCartesianAtTime(_time);
    }


    #endregion
//--#



//--#
    #region Patching


    public void RecalculatePatchedConics() {

        OrbitDriver patchedOrbitDriver;

        Orbit currOrbit = orbits[0];
        currOrbit.ResetEndtime();
        orbits = [];
        orbits.Insert(0, currOrbit);
        orbits.Capacity = orbitSettings.patchDepthLimit;
        
        for (int i = 0; i < orbitSettings.patchDepthLimit-1; i++) {

            var patchingResults = PatchedConicsSolver.RecalculatePatchPrediction(orbits[i], this);
            if (patchingResults.patched) {
                
                orbits[i].EndingTrueAnomaly = (double)(patchingResults.patchPoint?.TrueAnomaly);
                if (orbits[i].EndingTrueAnomaly <= orbits[i].StartingTrueAnomaly) orbits[i].EndingTrueAnomaly += 2 * Math.PI;
                orbits[i].OrbitEndTime = (double)(patchingResults.patchPoint?.Time);

                Vector3 relativePos;
                Vector3 relativeVel;
                if (patchingResults.patchedUp) {
                    patchedOrbitDriver = orbits[i].Parent.orbitDriver;
                    var parentRes = patchedOrbitDriver.orbits[0].GetCartesianAtTime(orbits[i].OrbitEndTime);
                    relativePos = (Vector3)(patchingResults.patchPoint?.Position) + parentRes.localPos;
                    relativeVel = (Vector3)(patchingResults.patchPoint?.Velocity) + parentRes.localVel;
                }
                else {
                    patchedOrbitDriver = patchingResults.celestialObjectPatchedInto.orbitDriver;
                    var newParentRes = patchedOrbitDriver.orbits[0].GetCartesianAtTime(orbits[i].OrbitEndTime);
                    relativePos = (Vector3)(patchingResults.patchPoint?.Position) - newParentRes.localPos;
                    relativeVel = (Vector3)(patchingResults.patchPoint?.Velocity) - newParentRes.localVel;
                }
                orbits.Insert(i + 1, new(relativePos, relativeVel, patchingResults.celestialObjectPatchedInto, orbits[i].OrbitEndTime));
                
            }
            else 
                break;

        }

        celestialObject.orbitDisplay.ResetList(orbits);

    }


    #endregion
//--#
}

}