using System;
using Godot;

namespace Orbits
{
/*
    #==============================================================#




*/
	[Serializable]
	public class Orbit
	{
//--#

#region Variables

		// Orbital prerameters
		public double Periapsis;
		public double Apoapsis;

		public double  SemiMajorAxis;
		public double  Inclination;
		public double  Eccentricity;
		public Vector3 EccentricityVector;
		public double  ArgumentOfPeriapsis;
		public double  RightAscensionOfAscendingNode;

		public double StartingTrueAnomaly;
		public double EndingTrueAnomaly;

		public CelestialObject Parent;


		// Time
		public double Epoch;
		public double MeanAnomalyAtEpoch;
		public double OrbitEndTime;
		public double Period;


		// Misc
		public double MeanMotion;

		public double  AngularMomentum;
		public Vector3 AngularMomentumVector;

		private double velSqr;

#endregion

//--#


//--#

#region Constructors

		public Orbit() { }

		public Orbit(Vector3 _pos, Vector3 _vel, CelestialObject _parent, double _epoch) {
			Parent = _parent;
			Epoch  = _epoch;
			CartesianToKeplerian(_pos, _vel);
		}

		public Orbit(
			double            _periapsis
			, double          _eccentricity
			, double          _inclination
			, double          _rightAscensionOfAscendingNode
			, double          _argumentOfPeriapsis
			, double          _trueAnomaly
			, CelestialObject _parent
			, double          _epoch) {
			Periapsis    = _periapsis;
			Eccentricity = _eccentricity;
			if (Eccentricity == 1) Eccentricity = 1.0000000001d;
			Inclination = _inclination;
			if (Math.Abs(Inclination) < 1e-6d) Inclination = 1e-6d;
			RightAscensionOfAscendingNode = _rightAscensionOfAscendingNode;
			ArgumentOfPeriapsis           = _argumentOfPeriapsis;
			StartingTrueAnomaly           = _trueAnomaly;
			Epoch                         = _epoch;
			MeanAnomalyAtEpoch            = ConicMath.TrueAnomalyToMeanAnomaly(StartingTrueAnomaly, Eccentricity);
			SemiMajorAxis                 = Periapsis / (1 - Eccentricity + double.Epsilon);

			Parent          = _parent;
			AngularMomentum = Math.Sqrt(Parent.gravitationalParameter * Periapsis * (1.0 + Eccentricity));

			var res = GetCartesianAtTrueAnomaly(StartingTrueAnomaly);
			CartesianToKeplerian(res.localPos, res.localVel);
		}

#endregion

//--#


//--#

#region Kepler to Cartesian

		public (Vector3 localPos, Vector3 localVel) GetCartesianAtTime(double _time) {
			MeanMotion = GetMeanMotionFromKeplerian();

			double newMeanAnomaly                  = MeanAnomalyAtEpoch + (_time - Epoch) * MeanMotion;
			if (Eccentricity < 1.0) newMeanAnomaly = Math.IEEERemainder(newMeanAnomaly, 2.0 * Math.PI);

			double newTrueAnomaly =
				ConicMath.MeanAnomalyToTrueAnomaly(newMeanAnomaly, Eccentricity, this.StartingTrueAnomaly);

			var results = GetCartesianAtTrueAnomaly(newTrueAnomaly);

			return (results.localPos, results.localVel);
		}

		public (Vector3 localPos, Vector3 localVel) GetCartesianAtTrueAnomaly(double _trueAnomaly) {
			// Updating the distance.
			double distance = Periapsis * (1.0 + Eccentricity) / (1.0 + Eccentricity * Math.Cos(_trueAnomaly));

			// Position
			double x = distance *
			           (Math.Cos(RightAscensionOfAscendingNode) * Math.Cos(ArgumentOfPeriapsis + _trueAnomaly)
			            - Math.Sin(RightAscensionOfAscendingNode) * Math.Sin(ArgumentOfPeriapsis + _trueAnomaly) *
			            Math.Cos(Inclination));

			double z = distance *
			           (Math.Sin(RightAscensionOfAscendingNode) * Math.Cos(ArgumentOfPeriapsis + _trueAnomaly)
			            + Math.Cos(RightAscensionOfAscendingNode) * Math.Sin(ArgumentOfPeriapsis + _trueAnomaly) *
			            Math.Cos(Inclination));

			double y = distance * (Math.Sin(Inclination) * Math.Sin(ArgumentOfPeriapsis + _trueAnomaly));

			// Velocity
			double p = SemiMajorAxis * (1 - Eccentricity * Eccentricity);
			if (Eccentricity == 1 || double.IsInfinity(SemiMajorAxis)) p = float.Epsilon;

			double vX = (x * AngularMomentum * Eccentricity / (distance * p)) * Math.Sin(_trueAnomaly)
			            - (AngularMomentum / distance) *
			            (Math.Cos(RightAscensionOfAscendingNode) * Math.Sin(ArgumentOfPeriapsis + _trueAnomaly)
			             + Math.Sin(RightAscensionOfAscendingNode) * Math.Cos(ArgumentOfPeriapsis + _trueAnomaly) *
			             Math.Cos(Inclination));

			double vZ = (z * AngularMomentum * Eccentricity / (distance * p)) * Math.Sin(_trueAnomaly)
			            - (AngularMomentum / distance) *
			            (Math.Sin(RightAscensionOfAscendingNode) * Math.Sin(ArgumentOfPeriapsis + _trueAnomaly)
			             - Math.Cos(RightAscensionOfAscendingNode) * Math.Cos(ArgumentOfPeriapsis + _trueAnomaly) *
			             Math.Cos(Inclination));

			double vY = (y * AngularMomentum * Eccentricity / (distance * p)) * Math.Sin(_trueAnomaly)
			            + (AngularMomentum / distance) *
			            (Math.Cos(ArgumentOfPeriapsis + _trueAnomaly) * Math.Sin(Inclination));

			Vector3 localPos = new(x, y, z);
			Vector3 localVel = new(vX, vY, vZ);

			return (localPos, localVel);
		}

		public Vector3 GetPeriapsisVector() {
			return GetCartesianAtTrueAnomaly(0).localPos;
		}

		public Vector3 GetApoapsisVector() {
			return GetCartesianAtTrueAnomaly(Math.PI).localPos;
		}

		public Vector3 GetAscendingNodeVector() {
			return GetCartesianAtTrueAnomaly(-ArgumentOfPeriapsis).localPos;
		}

		public Vector3 GetDescendingNodeVector() {
			return GetCartesianAtTrueAnomaly(Math.PI - ArgumentOfPeriapsis).localPos;
		}

		public double GetTimeAtTrueAnomaly(double _trueAnomaly) {
			double meanAnomaly = ConicMath.TrueAnomalyToMeanAnomaly(_trueAnomaly, Eccentricity);

			if ((Eccentricity >= 1) && ((meanAnomaly - MeanAnomalyAtEpoch) < 0)) return double.PositiveInfinity;

			else meanAnomaly -= MeanAnomalyAtEpoch;

			if (meanAnomaly < 0) meanAnomaly += 2 * Math.PI; // Not sure on the math here but if it checks out

			MeanMotion = GetMeanMotionFromKeplerian(); // Just in case
			return Epoch + meanAnomaly / MeanMotion;
		}

		public double GetTrueAnomalyAtTime(double _time) {
			double meanAnomaly                  = MeanAnomalyAtEpoch + (_time - Epoch) * MeanMotion;
			if (Eccentricity < 1.0) meanAnomaly = Math.IEEERemainder(meanAnomaly, 2.0 * Math.PI);

			return ConicMath.MeanAnomalyToTrueAnomaly(meanAnomaly, Eccentricity, StartingTrueAnomaly);
		}

		/// <summary>
		/// <para>
		/// Gets the first true anomaly, after periapsis, at a certain distance.
		/// </para>
		/// </summary>
		/// <param name="distance"></param>
		/// <returns>
		/// -1 if doesn't exist
		/// </returns>
		/*
		    Uses a simple bisection algorithm
		*/
		public double GetTrueAnomalyAtDistance(double _distance) {
			if (_distance > Apoapsis || _distance < Periapsis) return -1;

			double trueAnomalyGuess = (Eccentricity >= 1) ? EndingTrueAnomaly : Math.PI;
			double trueAnomalyStep  = trueAnomalyGuess / 2d;
			double distance         = GetCartesianAtTrueAnomaly(trueAnomalyGuess).localPos.Length();
			int    itteration       = 0;
			int    maxItterations   = 50;

			while (!double.IsFinite(distance) ||
			       ((Math.Abs(_distance - distance) > 1) && (itteration < maxItterations))) {
				if (distance > _distance) {
					trueAnomalyGuess -= trueAnomalyStep;
				}
				else {
					trueAnomalyGuess += trueAnomalyStep;
				}

				trueAnomalyStep *= 0.5d;
				distance        =  GetCartesianAtTrueAnomaly(trueAnomalyGuess).localPos.Length();
				itteration++;
			}

			return trueAnomalyGuess;
		}

		/// <summary>
		/// MUST BE A SIBLING ORBIT!
		/// </summary>
		/// <param name="_other"></param>
		/// <param name="_time"></param>
		/// <returns></returns>
		public double GetDistanceFromSiblingOrbitAtTime(Orbit _other, double _time) {
			Vector3 thisPos  = GetCartesianAtTime(_time).localPos;
			Vector3 otherPos = _other.GetCartesianAtTime(_time).localPos;

			return (thisPos - otherPos).Length();
		}

#endregion

//--#


//--#

#region Cartesian to Kepler

		public void CartesianToKeplerian(Vector3 _pos, Vector3 _vel) {
			double epsilon                         = 1e-6;
			if (Math.Abs(_pos.X) < epsilon) _pos.X = epsilon;
			if (Math.Abs(_pos.Y) < epsilon) _pos.Y = epsilon;
			if (Math.Abs(_pos.Z) < epsilon) _pos.Z = epsilon;

			if (Math.Abs(_vel.X) < epsilon) _vel.X = epsilon;
			if (Math.Abs(_vel.Y) < epsilon) _vel.Y = epsilon;
			if (Math.Abs(_vel.Z) < epsilon) _vel.Z = epsilon;

			double distance = _pos.Length();
			velSqr = _vel.LengthSquared();

			AngularMomentumVector = -_pos.Cross(_vel);
			AngularMomentum       = AngularMomentumVector.Length();

			Inclination =
				Math.Acos(
					Math.Clamp((AngularMomentumVector.Y / AngularMomentum), -1d, 1d)
				); // Acos must be between -1 and 1
			if (Math.Abs(Inclination) < 1e-6d) Inclination = 1e-6d;

			Vector3 upVector = new(0, 1, 0);
			Vector3 n        = -upVector.Cross(AngularMomentumVector);
			double  nMag     = n.Length();

			RightAscensionOfAscendingNode =
				Math.Acos(Math.Clamp((n.X / (nMag + double.Epsilon)), -1d, 1d)); // Acos must be between -1 and 1
			if (n.Z < 0) RightAscensionOfAscendingNode = 2 * Math.PI - RightAscensionOfAscendingNode;

			EccentricityVector = (((velSqr / Parent.gravitationalParameter) - (1 / distance)) * _pos) -
			                     (_pos.Dot(_vel) / Parent.gravitationalParameter) * _vel;
			Eccentricity = EccentricityVector.Length();

			MeanMotion = GetMeanMotionFromKeplerian();
			Period     = 2 * Math.PI / MeanMotion;

			ArgumentOfPeriapsis =
				Math.Acos(
					Math.Clamp(n.Dot(EccentricityVector) / (nMag * Eccentricity + double.Epsilon), -1d, 1d)
				); // Acos must be between -1 and 1
			if (EccentricityVector.Y < 0) ArgumentOfPeriapsis = 2 * Math.PI - ArgumentOfPeriapsis;

			StartingTrueAnomaly =
				Math.Acos(
					Math.Clamp(EccentricityVector.Dot(_pos) / (Eccentricity * distance + double.Epsilon), -1d, 1d)
				); // Acos must be between -1 and 1
			if ((_pos / distance).Dot(_vel) < 0) StartingTrueAnomaly = 2 * Math.PI - StartingTrueAnomaly;

			MeanAnomalyAtEpoch = ConicMath.TrueAnomalyToMeanAnomaly(StartingTrueAnomaly, Eccentricity);
			ResetEndtime();

			SemiMajorAxis = -Parent.gravitationalParameter /
			                (2 * (0.5 * velSqr - Parent.gravitationalParameter / distance));

			Periapsis = (AngularMomentum * AngularMomentum) / (Parent.gravitationalParameter * (1 + Eccentricity));
			Apoapsis  = (Eccentricity >= 1) ? double.PositiveInfinity : -Periapsis + 2 * SemiMajorAxis;
		}

		public void ResetEndtime() {
			if (Eccentricity < 1) EndingTrueAnomaly = StartingTrueAnomaly + 2 * Math.PI;
			else {
				EndingTrueAnomaly = Math.Acos(-1 / Eccentricity) * 0.99999d;
				if (StartingTrueAnomaly >= EndingTrueAnomaly) StartingTrueAnomaly -= 2 * Math.PI;
			}

			if (Eccentricity >= 1) OrbitEndTime = GetTimeAtTrueAnomaly(EndingTrueAnomaly);
			else OrbitEndTime                   = Epoch + Period;
		}

#endregion

//--#


//--#

#region --

#endregion

//--#


//--#

#region Misc methods

		public double GetMeanMotionFromKeplerian() {
			double multiplier = (Eccentricity == 1.0) ? 1.0 : Math.Abs(1.0 - Eccentricity * Eccentricity);
			multiplier = Math.Sqrt(multiplier * multiplier * multiplier);
			return multiplier * Parent.gravitationalParameter * Parent.gravitationalParameter /
			       (AngularMomentum * AngularMomentum * AngularMomentum);
		}

		public override String ToString() {
			return "Orbit: " + "\n" +
			       "Periapsis: " + Periapsis + "\n" +
			       "Apoapsis: " + Apoapsis + "\n" +
			       "SemiMajorAxis: " + SemiMajorAxis + "\n" +
			       "Inclination: " + Inclination + "\n" +
			       "Eccentricity: " + Eccentricity + "\n" +
			       "ArgumentOfPeriapsis: " + ArgumentOfPeriapsis + "\n" +
			       "RightAscensionOfAscendingNode: " + RightAscensionOfAscendingNode + "\n" +
			       "TrueAnomaly: " + StartingTrueAnomaly + "\n" +
			       "parent.gravitationalParameter: " + Parent.gravitationalParameter + "\n" +
			       "Epoch: " + Epoch + "\n" +
			       "MeanAnomalyAtEpoch: " + MeanAnomalyAtEpoch + "\n" +
			       "OrbitEndTime: " + OrbitEndTime + "\n" +
			       "MeanMotion: " + MeanMotion + "\n" +
			       "AngularMomentum: " + AngularMomentum + "\n";
		}

#endregion

//--#
	}
}