using Godot;
using System;

namespace Orbits {

public struct ConicMath {

    public static double MeanAnomalyToTrueAnomaly(double _meanAnomaly, double _eccentricity, double _trueAnomalyHint)
    {
        if (double.IsNaN(_trueAnomalyHint)) {GD.PushError("True anomaly hint is NaN"); _trueAnomalyHint = 0;}
        if (double.IsNaN(_meanAnomaly)) {GD.PushError("Mean anomaly is NaN"); return _trueAnomalyHint;}

        double tolerance = 0.000000001;
        int maxIter = 200;

        // Using Newton method to convert mean anomaly to true anomaly.
        if (_eccentricity < 1) {

            _meanAnomaly = Math.IEEERemainder(_meanAnomaly, 2.0 * Math.PI);
            double eccentricAnomaly = 2.0 * Math.Atan(Math.Tan(0.5 * _trueAnomalyHint) * Math.Sqrt((1.0 - _eccentricity) / (1.0 + _eccentricity)));

            double regionMin = _meanAnomaly - _eccentricity;
            double regionMax = _meanAnomaly + _eccentricity;

            for (int iter = 0; iter < maxIter; ++iter) // Newton iteration for Kepler equation;
            {
                eccentricAnomaly = Math.Clamp(eccentricAnomaly, regionMin, regionMax);

                double residual = eccentricAnomaly - _eccentricity * Math.Sin(eccentricAnomaly) - _meanAnomaly;
                double derivative = 1.0 - _eccentricity * Math.Cos(eccentricAnomaly);

                double delta = -residual / derivative;
                eccentricAnomaly += delta;
                if (Math.Abs(delta) < tolerance) { break; }

                if (iter + 1 == maxIter)
                { GD.Print("Mean anomaly to true anomaly conversion failed: the solver did not converge. " + _meanAnomaly +" "+ _eccentricity +" "+ _trueAnomalyHint); }
            }

            return 2.0 * Math.Atan(Math.Tan(0.5 * eccentricAnomaly) * Math.Sqrt((1.0 + _eccentricity) / (1.0 - _eccentricity)));

        } else if(_eccentricity == 1) {

            double z = Math.Cbrt(3.0 * _meanAnomaly + Math.Sqrt(1 + 9.0 * _meanAnomaly * _meanAnomaly));
            return 2.0 * Math.Atan(z - 1.0 / z);

        } else {

            double eccentricAnomaly = 2.0 * Math.Atanh(Math.Tan(0.5 * _trueAnomalyHint) * Math.Sqrt((_eccentricity - 1.0) / (_eccentricity + 1.0)));
            for (int iter = 0; iter < maxIter; ++iter) // Newton iteration for Kepler equation;
            {
                double residual = _eccentricity * Math.Sinh(eccentricAnomaly) - eccentricAnomaly - _meanAnomaly;
                double derivative = _eccentricity * Math.Cosh(eccentricAnomaly) - 1.0;

                double delta = -residual / derivative;
                eccentricAnomaly += delta;
                if (Math.Abs(delta) < tolerance) { break; }

                if (iter + 1 == maxIter)
                { GD.Print("Mean anomaly to true anomaly conversion failed: the solver did not converge." + _meanAnomaly +" "+ _eccentricity +" "+ _trueAnomalyHint +" "+ eccentricAnomaly); }
            }

            return 2.0 * Math.Atan(Math.Tanh(0.5 * eccentricAnomaly) * Math.Sqrt((_eccentricity + 1.0) / (_eccentricity - 1.0)));

        }
    }

    public static double TrueAnomalyToMeanAnomaly(double _trueAnomaly, double _eccentricity) {
        
        if (_eccentricity < 1) {

            double eccentricAnomaly = 2.0 * Math.Atan(Math.Tan(0.5 * _trueAnomaly) * Math.Sqrt((1.0 - _eccentricity) / (1.0 + _eccentricity)));
            return eccentricAnomaly - _eccentricity * Math.Sin(eccentricAnomaly);

        } else if(_eccentricity == 1) {

            double tan = Math.Tan(0.5 * _trueAnomaly);
            return 0.5 * tan * (1.0 + tan * tan / 3.0);

        } else {

            double eccentricAnomaly = 2.0 * Math.Atanh(Math.Clamp(Math.Tan(0.5 * _trueAnomaly) * Math.Sqrt((_eccentricity - 1.0) / (_eccentricity + 1.0)), -1d, 1d));
            double meanAnomaly = _eccentricity * Math.Sinh(eccentricAnomaly) - eccentricAnomaly;

            if (double.IsNaN(meanAnomaly)) GD.Print(eccentricAnomaly + " e|m " + meanAnomaly); 
            
            return meanAnomaly;

        }
    }
}

}
