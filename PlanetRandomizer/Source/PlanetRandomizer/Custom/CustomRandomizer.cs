using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetRandomizer
{
    class CustomRandomizer : IComparer<PlanetData>
    {
        // randomizing parameters
        double eccMax = 0.4; // maximum eccentricity
        double incMax = 20; // maximum inclination
        double smaMinRadius = 10.0; // minimum semi-major axis as multiplier of reference body radius
        double smaMaxSOI = 0.4; // maximum semi-major axis as multiplier of reference body SOI radius
        double maxMassRatio = 0.1; // maximum ratio between the masses of a satellite and its primary
        double minTidalLockingMassRatio = 0.02; // minimum ratio between the masses of a satellite and its primary before the primary is tidally locked to the satellite
        double maxTidalLockingRadius = 80; // maximum separation between a primary and its satellite at which the satellite is tidally locked
        double maxRotationRate = 0.2; // maximum rotation speed as a multiple of orbital speed at the surface
        double minRotationFactor = 0.01; // minimum rotation speed factor as a multiple of maximum rotation speed
        double eccIncExponent = 2.0; // controls how circular/equatorial large planet/moon orbits are
        double soiSeparationFactor = 2.0; // controls how far apart planets/moons are in terms of their SOI;

        double probabilityOfSunOrbit = 0.2;

        List<PlanetData> changedPlanets;
        int totalPlanets;

        System.Random rng;

        public CustomRandomizer(System.Random rng)
        {
            this.rng = rng;
        }

        public List<PlanetData> MakePlanetData()
        {
            changedPlanets = new List<PlanetData>();

            //First pass, changes mass and radius. Planets may change size drastically, so don't change orbits yet.
            PlanetData sunData = new PlanetData();
            sunData.Name = "Sun";
            sunData.currentBody = Planetarium.fetch.Sun;
            sunData.Mass = sunData.currentBody.Mass;
            sunData.Radius = sunData.currentBody.Radius;
            sunData.sphereOfInfluence = FlightGlobals.Bodies.Skip(1).Max(b => b.orbit.ApR); //Select the apoapsis of the furthest orbiting planet in the default model. In some cases will be a modded outer planet.

            totalPlanets = 0;
            foreach (CelestialBody planet in FlightGlobals.Bodies.Skip(1))
            {
                totalPlanets++;
                PlanetData planetData = new PlanetData();
                PlanetData defaultData = Settings.DefaultOrbits[totalPlanets-1];

                //Used as a unique ID on load
                planetData.Name = planet.gameObject.name;
                planetData.currentBody = planet;

                if (planet != Planetarium.fetch.Home && planet.pqsController != null)
                {
                    planetData.Radius = defaultData.Radius * Math.Pow(2, 2.0 * rng.NextDouble() - 1);
                }
                else {                    
                    planetData.Radius = planet.Radius;
                }

                if (planet != Planetarium.fetch.Home)
                {
                    if (planet.Mass > sunData.Mass * 0.1)
                    {
                        planetData.Mass = sunData.Mass * 0.1 * Math.Pow(2, 2.0 * rng.NextDouble() - 1);
                    }
                    else
                    {
                        planetData.Mass = defaultData.Mass * Math.Pow(planetData.Radius / defaultData.Radius, 3) * Math.Pow(2, 2.0 * rng.NextDouble() - 1);
                    }
                }
                else
                {
                    planetData.Mass = planet.Mass;
                }

                /*planetData.RotationPeriod = planet.rotationPeriod;
                planetData.SemiMajorAxis = planet.orbit.semiMajorAxis;
                planetData.Eccentricity = planet.orbit.eccentricity;
                planetData.Inclination = planet.orbit.inclination;
                planetData.MeanAnomalyAtEpoch = planet.orbit.meanAnomalyAtEpoch;
                planetData.LAN = planet.orbit.LAN;
                planetData.ArgumentOfPeriapsis = planet.orbit.argumentOfPeriapsis;*/

                UnityEngine.Debug.Log("Planet " + planetData.Name + " Created");

                //Insert planet into list, sorted by mass.
                changedPlanets.Add(planetData);
            }

            changedPlanets.Sort(0, changedPlanets.Count, this);

            for (int rank = 1; rank <= changedPlanets.Count; rank++)
            {
                PlanetData planetData = changedPlanets[rank - 1];
                planetData.Rank = rank;

                for (int attempt = 0; attempt < 150; attempt++)
                {
                    int referenceIndex = (int)(rank - (rank * Math.Pow(rng.NextDouble(), 3)));
                    if (rng.NextDouble() <= probabilityOfSunOrbit)
                    {
                        referenceIndex = 0;
                    }

                    //Attempt to place planet around this body. Force if tried 150 times without results, since crashing due to bad luck is inelegant.
                    PlanetData existing;
                    if (referenceIndex == 0)
                    {
                        existing = sunData;
                    }
                    else
                    {
                        existing = changedPlanets[referenceIndex - 1];
                    }

                    if (constructOrbit(planetData, existing, attempt >= 149))
                    {
                        UnityEngine.Debug.Log("Rank " + rank + " : " + planetData.Name + " Now Orbiting " + existing.Name);
                        UnityEngine.Debug.Log("Tries: " + attempt);
                        break;
                    }
                }
            }

            return changedPlanets;
        }

        private bool constructOrbit(PlanetData moon, PlanetData reference, bool force = false)
        {
            if(force) UnityEngine.Debug.LogError("Forcing Orbit of " + moon.Name);

            if (moon.Mass > reference.Mass * maxMassRatio) return false;

            double minOrbit = Math.Max(Orbital.GetRocheLimit(moon.Radius, moon.Mass, reference.Mass), reference.Radius * smaMinRadius);
            double maxOrbit = reference.sphereOfInfluence *smaMaxSOI;

            double moonSOI = Orbital.GetSOI(moon.SemiMajorAxis, moon.Mass, reference.Mass);

            double? axis = null;
            double eccentricity = 0.0;

            int attempt = 0;
            while (axis == null && (++attempt < 150))
            {
                axis = rng.NextDouble() * (maxOrbit - minOrbit) + minOrbit;
                eccentricity = eccMax * rng.NextDouble() * stabilityFromMass(moon.Rank);
                if (double.IsInfinity(axis.Value))
                {
                    throw new ArithmeticException("Axis reaches infinity: " + minOrbit + "("+reference.Radius+")" + " to " + maxOrbit + "("+reference.sphereOfInfluence+")");
                }

                double apoapsis = Orbital.GetApoapsis(axis.Value, eccentricity);
                double periapsis = Orbital.GetPeriapsis(axis.Value, eccentricity);

                //Find other planets the orbit would collide with.
                foreach (PlanetData existingMoon in changedPlanets)
                {
                    if (existingMoon.ReferenceBody == reference.Name)
                    {
                        double existingSOI = Orbital.GetSOI(existingMoon.SemiMajorAxis, existingMoon.Mass, reference.Mass);
                        double exclusionMax = Orbital.GetApoapsis(existingMoon.SemiMajorAxis, existingMoon.Eccentricity) + (existingSOI + moonSOI) * soiSeparationFactor;
                        double exclusionMin = Orbital.GetPeriapsis(existingMoon.SemiMajorAxis, existingMoon.Eccentricity) - (existingSOI + moonSOI) * soiSeparationFactor;
                        if ((apoapsis < exclusionMax && apoapsis > exclusionMin) || (periapsis < exclusionMax && periapsis > exclusionMin))
                        {
                            axis = null;
                            break;
                        }
                    }
                }
            }
            UnityEngine.Debug.Log("Orbit Attempts: " + attempt);
            if (axis == null)
            {
                return false; //Failed to find an orbit around this body.
            }

            moon.ReferenceBody = reference.Name;

            moon.SemiMajorAxis = axis.Value;
            moon.sphereOfInfluence = Orbital.GetSOI(moon.SemiMajorAxis, moon.Mass, reference.Mass);
            moon.Eccentricity = eccentricity;
            moon.Inclination = incMax * rng.NextDouble() * stabilityFromMass(moon.Rank);

            moon.MeanAnomalyAtEpoch = 2 * Math.PI * rng.NextDouble();
            moon.LAN = 360 * rng.NextDouble();
            moon.ArgumentOfPeriapsis = 360 * rng.NextDouble();

            // tidal locking: distance close enough?
            if (moon.SemiMajorAxis <= reference.Radius * maxTidalLockingRadius)
            {
                // tidal locking of satellite to primary if close enough
                moon.RotationPeriod = 2 * Math.PI * Math.Sqrt((Math.Pow(moon.SemiMajorAxis, 3) / 6.674E-11) / (moon.Mass + reference.Mass));

                //reverse tidal locking: is moon large enough to lock both to eachother?
                if (moon.Mass >= reference.Mass * minTidalLockingMassRatio && moon.SemiMajorAxis <= reference.Radius * maxTidalLockingRadius / 3)
                {
                    // tidal locking of primary to satellite if close enough and massive enough
                    reference.RotationPeriod = 2 * Math.PI * Math.Sqrt((Math.Pow(moon.SemiMajorAxis, 3) / 6.674E-11) / (moon.Mass + reference.Mass));
                }
            }
            else
            {
                // if no tidal locking, rotation speed is inversely distributed between maxRotationRate and maxRotationRate*minRotationFactor
                moon.RotationPeriod = (1 / maxRotationRate * 2 * Math.PI * Math.Sqrt((Math.Pow(moon.Radius, 3) / 6.674E-11) / moon.Mass)) / (minRotationFactor + (1 - minRotationFactor) * rng.NextDouble());
            }

            return true;
        }

        private double stabilityFromMass(int rank)
        {
            return Math.Pow(rank / (double)totalPlanets, eccIncExponent);
        }

        public int Compare(PlanetData x, PlanetData y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (x == null)
                    return 1;
                else
                    return y.Mass.CompareTo(x.Mass);
            }
        }
    }
}
