using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine;
using KSP;

namespace PlanetRandomizer
{
    public class Settings
    {
        private static Settings mInstance;
        public static Settings Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new Settings();
                }
                return mInstance;
            }
        }

        public static PlanetData[] DefaultOrbits;
        public static PlanetData DefaultKerbin;

        [Persistent]
        public int seed = 1;

        [Persistent(collectionIndex = "PLANET")]
        public PlanetData[] Orbits = new PlanetData[] { };

        public static bool Save(ConfigNode save)
        {
            //Prevents errors from corrupting the savefile.
            if (mInstance != null)
            {
                try
                {
                    ConfigNode.CreateConfigFromObject(mInstance, save);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
            return mInstance != null;
        }

        public static void Load(ConfigNode load)
        {
            mInstance = new Settings();
            ConfigNode.LoadObjectFromConfig(mInstance, load);
        }

        public static void MemorizeDefaultLayout()
        {
            DefaultOrbits = new PlanetData[FlightGlobals.Bodies.Count - 1];
            for (int i = 0; i < DefaultOrbits.Length; i++)
            {
                CelestialBody planet = FlightGlobals.Bodies[i+1];
                PlanetData planetData = new PlanetData();

                planetData.ArgumentOfPeriapsis = planet.orbit.argumentOfPeriapsis;
                planetData.currentBody = planet;
                planetData.Eccentricity = planet.orbit.eccentricity;
                planetData.Inclination = planet.orbit.inclination;
                planetData.LAN = planet.orbit.LAN;
                planetData.Mass = planet.Mass;
                planetData.MeanAnomalyAtEpoch = planet.orbit.meanAnomalyAtEpoch;
                planetData.Name = planet.gameObject.name;
                planetData.Radius = planet.Radius;
                planetData.Rank = 0;
                planetData.ReferenceBody = planet.referenceBody.gameObject.name;
                planetData.RotationPeriod = planet.rotationPeriod;
                planetData.SemiMajorAxis = planet.orbit.semiMajorAxis;
                planetData.sphereOfInfluence = planet.sphereOfInfluence;

                if (planetData.Name == "Kerbin") DefaultKerbin = planetData;

                DefaultOrbits[i] = planetData;
            }
        }
    }
}
