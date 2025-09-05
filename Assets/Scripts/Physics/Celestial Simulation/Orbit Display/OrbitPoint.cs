using System;
using Godot;

namespace Orbits
{

/* 
    #==============================================================#
    
    Struct for containing a point on an orbit and the time at said
    point.


*/
[Serializable]
public struct OrbitPoint : IComparable<OrbitPoint> {
    public Vector3 Position;
    public Vector3 Velocity;
    public double TrueAnomaly;
    public double Time;

    public OrbitPoint(Vector3 _position, Vector3 _velocity, double _trueAnomaly, double _time) {
        Position = _position;
        Velocity = _velocity;
        TrueAnomaly = _trueAnomaly;
        Time = _time;
    }

    public readonly int CompareTo(OrbitPoint _other) {
        if (this.Time > _other.Time) return 1;
        else if (this.Time < _other.Time) return -1;
        else return 0;
    }
}

}