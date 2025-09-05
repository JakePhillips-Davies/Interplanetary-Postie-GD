using System;
using System.Collections.Generic;

namespace Orbits
{

/* 
    #==============================================================#

    Struct for containing a patching candidate of a celestial
    object.


*/
[Serializable]
public struct PatchingCandidate {
    public List<OrbitPoint> OrbitRangeCrossingPoints;
    public Orbit Orbit;
    public CelestialObject CelestialObject;
    public double SoiDistance;
    
    public PatchingCandidate(Orbit _orbit, CelestialObject _celestialObject) {
        Orbit = _orbit;
        CelestialObject = _celestialObject;
        SoiDistance = CelestialObject.soiDistance;

        OrbitRangeCrossingPoints = new();
    }
}

}