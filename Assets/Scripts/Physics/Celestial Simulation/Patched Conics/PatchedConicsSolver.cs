using System;
using System.Collections.Generic;
using Godot;
using Orbits;

public class PatchedConicsSolver
{
//--#
    #region Crossover points


    public static (bool patched, OrbitPoint? patchPoint, CelestialObject celestialObjectPatchedInto, bool patchedUp) RecalculatePatchPrediction(Orbit _orbit, OrbitDriver _orbitDriver) {

        OrbitPoint? firstPatchPoint = null;
        CelestialObject celestialObjectPatchedInto = null;
        List<PatchingCandidate> patchingCandidates = new();

        CelestialObject parent = _orbit.Parent;
        bool patchedUp = false;

        List<CelestialObject> siblings = new();
        foreach (Node sibling in parent.GetChildren()) {
            if ((sibling is CelestialObject siblingCelestialObject) && (siblingCelestialObject != _orbitDriver.celestialObject))
                siblings.Add(siblingCelestialObject);
        }

        // Get the list of potential candidates for patching
        OrbitDriver candidateOrbitDriver;
        foreach (CelestialObject sibling in siblings) {

            candidateOrbitDriver = sibling.orbitDriver;
            if (   sibling.soiDistance > 100 // SOI size prefilter
                && !(_orbit.Apoapsis < candidateOrbitDriver.orbits[0].Periapsis - sibling.soiDistance) // Apoapsis prefilter
                && !(_orbit.Periapsis > candidateOrbitDriver.orbits[0].Apoapsis + sibling.soiDistance) // Periapsis prefilter
            ) {

                patchingCandidates.Add(new(candidateOrbitDriver.orbits[0], sibling));

            }

        }


        if (_orbit.Apoapsis > parent.soiDistance) {
            double ta = _orbit.GetTrueAnomalyAtDistance(parent.soiDistance);
            if (ta <= _orbit.StartingTrueAnomaly) ta += 2 * Math.PI; // Sometimes it sneaks behind the starting anomaly and causes issues 

            var res = _orbit.GetCartesianAtTrueAnomaly(ta);
            firstPatchPoint = new(res.localPos, res.localVel, ta, _orbit.GetTimeAtTrueAnomaly(ta));

            celestialObjectPatchedInto = _orbit.Parent.orbitDriver.orbits[0].Parent;
            patchedUp = true;
        }

        double startDistance = _orbit.GetCartesianAtTime(_orbit.Epoch).localPos.Length();
        foreach (PatchingCandidate candidate in patchingCandidates) {

            CalculateOrbitCrossoverPointsForCandidate(_orbit, candidate, firstPatchPoint);

            /*
                Get closest approach between each crossover point until one has a closest approach less than SOI distance of the candidate.

                Each case here follows the same logic, just altered for certain situations
                - Get the closest approach time between the entry and exit of boundry
                - Check if that's within the candidate's SOI
                - If so set the first patch point to be on the boundary and clear the list to break out
                - Else remove the points just searched between from the list
                - Repeat until there are no points in list

            */
            double closestApproachTime = 0; 
            double finalTime = (double)((firstPatchPoint == null) ? _orbit.OrbitEndTime : firstPatchPoint?.Time);
            if (candidate.OrbitRangeCrossingPoints.Count <= 0) { // If there are no crossover points and it hasn't been culled by the prefilters then it must, until the first patch, be at all times within the candidate's orbit.
                
                closestApproachTime = SearchForTimeOfClosestApproach(_orbit.Epoch, finalTime, _orbit, candidate.Orbit);
                var res = _orbit.GetCartesianAtTime(closestApproachTime);

                if (_orbit.GetDistanceFromSiblingOrbitAtTime(candidate.Orbit, closestApproachTime) < candidate.SoiDistance) {
                    firstPatchPoint = SoiBoundarySearch(_orbit.Epoch, closestApproachTime, _orbit, candidate);
                    celestialObjectPatchedInto = candidate.CelestialObject;
                    patchedUp = false;
                    candidate.OrbitRangeCrossingPoints.Clear(); // Don't bother checking further
                }
            }
            
            // Special starting case, from starting time to first point's time
            else if (candidate.Orbit.Periapsis - candidate.SoiDistance <= startDistance && startDistance <= candidate.Orbit.Apoapsis + candidate.SoiDistance) {
                
                closestApproachTime = SearchForTimeOfClosestApproach(_orbit.Epoch, candidate.OrbitRangeCrossingPoints[0].Time, _orbit, candidate.Orbit);
                var res = _orbit.GetCartesianAtTime(closestApproachTime);

                if (_orbit.GetDistanceFromSiblingOrbitAtTime(candidate.Orbit, closestApproachTime) < candidate.SoiDistance) {
                    firstPatchPoint = SoiBoundarySearch(_orbit.Epoch, closestApproachTime, _orbit, candidate);
                    celestialObjectPatchedInto = candidate.CelestialObject;
                    patchedUp = false;
                    candidate.OrbitRangeCrossingPoints.Clear(); // Don't bother checking further
                }
                else candidate.OrbitRangeCrossingPoints.RemoveAt(0);
            }

            int itt = 0; // Juuuuuuuust in case, never trust a while loop. There should only ever be 2 of these checks max
            while (itt < 5 && candidate.OrbitRangeCrossingPoints.Count > 0) {
                
                // From first point to firstPatchPoint
                if (candidate.OrbitRangeCrossingPoints.Count == 1) {
                    
                    closestApproachTime = SearchForTimeOfClosestApproach(candidate.OrbitRangeCrossingPoints[0].Time, finalTime, _orbit, candidate.Orbit);
                    var res = _orbit.GetCartesianAtTime(closestApproachTime);

                    if (_orbit.GetDistanceFromSiblingOrbitAtTime(candidate.Orbit, closestApproachTime) < candidate.SoiDistance) {
                        firstPatchPoint = SoiBoundarySearch(candidate.OrbitRangeCrossingPoints[0].Time, closestApproachTime, _orbit, candidate);
                        celestialObjectPatchedInto = candidate.CelestialObject;
                        patchedUp = false;
                        candidate.OrbitRangeCrossingPoints.Clear(); // Don't bother checking further
                    }
                    else candidate.OrbitRangeCrossingPoints.RemoveAt(0);
                }
                
                // From first point to second point
                else {
                    
                    closestApproachTime = SearchForTimeOfClosestApproach(candidate.OrbitRangeCrossingPoints[0].Time, candidate.OrbitRangeCrossingPoints[1].Time, _orbit, candidate.Orbit);
                    var res = _orbit.GetCartesianAtTime(closestApproachTime);

                    if (_orbit.GetDistanceFromSiblingOrbitAtTime(candidate.Orbit, closestApproachTime) < candidate.SoiDistance) {
                        firstPatchPoint = SoiBoundarySearch(candidate.OrbitRangeCrossingPoints[0].Time, closestApproachTime, _orbit, candidate);
                        celestialObjectPatchedInto = candidate.CelestialObject;
                        patchedUp = false;
                        candidate.OrbitRangeCrossingPoints.Clear(); // Don't bother checking further
                    }
                    else {
                        candidate.OrbitRangeCrossingPoints.RemoveAt(0);
                        candidate.OrbitRangeCrossingPoints.RemoveAt(0);
                    }
                }

                itt++;
            }

        }

        return (firstPatchPoint != null, firstPatchPoint, celestialObjectPatchedInto, patchedUp);
    }


    #endregion
//--#



//--#
    #region Crossover points


    public static void CalculateOrbitCrossoverPointsForCandidate(Orbit _orbit, PatchingCandidate _candidate, OrbitPoint? _firstPatchPoint) {

        double time1;
        double time2;


        // Get the two points where the main orbit crosses the minimum orbital radius for an encounter
        try{ 

            double ta = _orbit.GetTrueAnomalyAtDistance(_candidate.Orbit.Periapsis - _candidate.SoiDistance);
            if (ta != -1) {
                time1 = _orbit.GetTimeAtTrueAnomaly(ta);
                if ((time1 < _firstPatchPoint?.Time || _firstPatchPoint == null) && double.IsFinite(time1)) {
                    var res1 = _orbit.GetCartesianAtTrueAnomaly(ta);
                    _candidate.OrbitRangeCrossingPoints.Add(new(res1.localPos, res1.localVel, ta, time1));
                }

                time2 = _orbit.GetTimeAtTrueAnomaly(-ta);
                if ((time2 < _firstPatchPoint?.Time || _firstPatchPoint == null) && double.IsFinite(time2)) {
                    var res2 = _orbit.GetCartesianAtTrueAnomaly(-ta);
                    _candidate.OrbitRangeCrossingPoints.Add(new(res2.localPos, res2.localVel, -ta, time2));
                }
            }

        }
        catch (System.Exception){}


        // Get the two points where the main orbit crosses the maximum orbital radius for an encounter
        try{ 

            double ta = _orbit.GetTrueAnomalyAtDistance(_candidate.Orbit.Apoapsis + _candidate.SoiDistance);
            if (ta != -1) {
                time1 = _orbit.GetTimeAtTrueAnomaly(ta);
                if ((time1 < _firstPatchPoint?.Time || _firstPatchPoint == null) && double.IsFinite(time1)) {
                    var res1 = _orbit.GetCartesianAtTrueAnomaly(ta);
                    _candidate.OrbitRangeCrossingPoints.Add(new(res1.localPos, res1.localVel, ta, time1));
                }

                time2 = _orbit.GetTimeAtTrueAnomaly(-ta);
                if ((time2 < _firstPatchPoint?.Time || _firstPatchPoint == null) && double.IsFinite(time2)) {
                    var res2 = _orbit.GetCartesianAtTrueAnomaly(-ta);
                    _candidate.OrbitRangeCrossingPoints.Add(new(res2.localPos, res2.localVel, -ta, time2));
                }
            }

        }
        catch (System.Exception){}

        _candidate.OrbitRangeCrossingPoints.Sort();
        
    }


    #endregion
//--#



//--#
    #region Closest approach


    public static double SearchForTimeOfClosestApproach(double _startTime, double _endTime, Orbit _orbit, Orbit _orbitOther) {

        // Uses a golden section search to find the closest approach

        double F(double _t) => _orbit.GetDistanceFromSiblingOrbitAtTime(_orbitOther, _t);
        double a, b, c, d, fb, fc;
        double phi = ExtraMaths.Phi;

        a = _startTime;
        d = _endTime;

        b = d + (a - d) / phi;
        c = a + (d - a) / phi;

        fb = F(b);
        fc = F(c);

        int itr = 0;
        int itrMax = 50;
        double tolerance = 1d;
        while ((Math.Abs(d - a) > tolerance) && itr < itrMax) {
            
            if (fb < fc) {
                d = c;
                c = b;
                b = d + (a - d) / phi;

                fc = fb;
                fb = F(b);
            }
            else {
                a = b;
                b = c;
                c = a + (d - a) / phi;

                fb = fc;
                fc = F(c);
            }

            itr++;

        }

        return (a + d) / 2;
    }


    #endregion
//--#



//--#
    #region Boundary search


    public static OrbitPoint SoiBoundarySearch(double _startTime, double _endTime, Orbit _orbit, PatchingCandidate _candidate) {

        // Uses simple bisection method to find point where distance = SOI

        double F(double _t) => _orbit.GetDistanceFromSiblingOrbitAtTime(_candidate.Orbit, _t);
        double a, b, c, distance;
        a = _startTime;
        c = _endTime;
        b = (a + c) / 2;

        distance = F(b);
        
        int itr = 0;
        int itrMax = 50;
        double tolerance = 0.1d;
        while (Math.Abs(distance - _candidate.SoiDistance) > tolerance && itr < itrMax) {
            
            if (distance > _candidate.SoiDistance) {
                a = b;
                b = (a + c) / 2;

                distance = F(b);
            }
            else {
                c = b;
                b = (a + c) / 2;

                distance = F(b);
            }
            
            itr++;
        }

        var res = _orbit.GetCartesianAtTime(b);
        double trueAnomaly = _orbit.GetTrueAnomalyAtTime(b);
        if (trueAnomaly <= _orbit.StartingTrueAnomaly) {trueAnomaly += 2 * Math.PI;}

        return new OrbitPoint(res.localPos, res.localVel, trueAnomaly, b);

    }


    #endregion
//--#
}