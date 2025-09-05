using Godot;
using System;

namespace Orbits {

/* 
    #==============================================================#
    
    Struct for containing an array of points on an orbit from it's
    starting TA to an ending TA.


*/
[Serializable]
public struct OrbitConicLine {

//--#
    #region Variables


    public OrbitPoint[] Points;
    public OrbitPoint StartPoint;
    public OrbitPoint EndPoint;

    public int OrbitDetail;
    public double StartTrueAnomaly;
    public double EndTrueAnomaly;
    public Orbit Orbit;
    

    #endregion
//--#



//--#
    #region Constructors


    public OrbitConicLine(Orbit _orbit, int _orbitDetail) {

        Orbit = _orbit;
        OrbitDetail = _orbitDetail;
        StartTrueAnomaly = _orbit.StartingTrueAnomaly;
        EndTrueAnomaly = _orbit.EndingTrueAnomaly;
        
        var startRes = Orbit.GetCartesianAtTrueAnomaly(StartTrueAnomaly);
        StartPoint = new OrbitPoint(startRes.localPos, startRes.localVel, StartTrueAnomaly, Orbit.GetTimeAtTrueAnomaly(StartTrueAnomaly));
        var endRes = Orbit.GetCartesianAtTrueAnomaly(EndTrueAnomaly);
        EndPoint = new OrbitPoint(endRes.localPos, endRes.localVel, EndTrueAnomaly, Orbit.GetTimeAtTrueAnomaly(EndTrueAnomaly));

        Points = new OrbitPoint[OrbitDetail];

        UpdateOrbitPoints();

    }

    public OrbitConicLine(Orbit _orbit, int _orbitDetail, double _startTrueAnomaly, double _endTrueAnomaly) {

        Orbit = _orbit;
        OrbitDetail = _orbitDetail;
        StartTrueAnomaly = _startTrueAnomaly;
        EndTrueAnomaly = _endTrueAnomaly;

        var startRes = Orbit.GetCartesianAtTrueAnomaly(StartTrueAnomaly);
        StartPoint = new OrbitPoint(startRes.localPos, startRes.localVel, StartTrueAnomaly, Orbit.GetTimeAtTrueAnomaly(StartTrueAnomaly));
        var endRes = Orbit.GetCartesianAtTrueAnomaly(EndTrueAnomaly);
        EndPoint = new OrbitPoint(endRes.localPos, endRes.localVel, EndTrueAnomaly, Orbit.GetTimeAtTrueAnomaly(EndTrueAnomaly));

        Points = new OrbitPoint[OrbitDetail];

        UpdateOrbitPoints();

    }


    #endregion
//--#



//--#
    #region Update points


    public void UpdateOrbitPoints() {

        double time;
        Vector3 pos;
        Vector3 vel;
        (Vector3 localPos, Vector3 localVel) res;

        double trueAnomaly = StartTrueAnomaly;
        double trueAnomalyStep = (EndTrueAnomaly - StartTrueAnomaly) / (OrbitDetail - 1);
        
        for (int i = 0; i < OrbitDetail; i++) {

            res = Orbit.GetCartesianAtTrueAnomaly(trueAnomaly);
            pos = res.localPos;
            vel = res.localVel;
            time = Orbit.GetTimeAtTrueAnomaly(trueAnomaly);
            if (i == 0) time = Orbit.Epoch;

            Points[i] = new OrbitPoint(pos, vel, trueAnomaly, time);

            trueAnomaly += trueAnomalyStep;

        }

    }


    #endregion
//--#
}

}