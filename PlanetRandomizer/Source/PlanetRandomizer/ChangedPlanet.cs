using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace PlanetRandomizer
{
    public class PlanetData
    {
        [Persistent]
        public string Name = "ChangedPlanets";
        [Persistent]
        public double Radius = 0.0;
        [Persistent]
        public double Mass = 0.0;
        [Persistent]
        public double RotationPeriod = 0.0;
        [Persistent]
        public double SemiMajorAxis = 0.0;
        [Persistent]
        public double Eccentricity = 0.0;
        [Persistent]
        public double Inclination = 0.0;
        [Persistent]
        public double MeanAnomalyAtEpoch = 0.0;
        [Persistent]
        public double LAN = 0.0;
        [Persistent]
        public double ArgumentOfPeriapsis = 0.0;
        [Persistent]
        public string ReferenceBody = "ReferenceBody";
        [Persistent]
        public int Rank = 1;

        [Persistent]
        public int ScienceIndex = 0;

        public CelestialBody currentBody;
        public double sphereOfInfluence;
        public PlanetData referenceBodyData;

        public override string ToString()
        {
            return string.Format("[PLANET]{0} Radius={1} Mass={2} RotationPeriod={3} SemiMajorAxis={4} Eccentricity={5} Inclination={6} MeanAnomalyAtEpoch={7} LAN={8} ArgumentOfPeriapsis={9} ReferenceBody={10} Rank={11}",
                Name, Radius, Mass, RotationPeriod, SemiMajorAxis, Eccentricity, Inclination, MeanAnomalyAtEpoch, LAN, ArgumentOfPeriapsis, ReferenceBody, Rank);
        }
    }
}
