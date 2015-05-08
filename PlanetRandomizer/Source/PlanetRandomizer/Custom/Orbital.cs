using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetRandomizer
{
    static class Orbital
    {
        public static double GetApoapsis(double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * (1 + eccentricity);
        }
        public static double GetPeriapsis(double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * (1 - eccentricity);
        }

        //moons outside this limit would escape the gravity well of the planet.
        public static double GetSOI(double semiMajorAxis, double mass, double orbitingBodyMass) //ChangedPlanet body, CelestialBody reference)
        {
            return semiMajorAxis * Math.Pow(mass / (orbitingBodyMass + mass), 0.4);
        }

        public static double GetHillSphere(double semiMajorAxis, double mass, double orbitingBodyMass)
        {
            return semiMajorAxis * Math.Pow(mass / 3 * orbitingBodyMass,1.0/3.0);
        }

        //moons inside this limit would be torn apart and become rings instead.
        public static double GetRocheLimit(double radius, double mass, double orbitingBodyMass)
        {
            return 1.26 * radius * Math.Pow(orbitingBodyMass / mass, 1.0/3.0);
        }
    }
}
