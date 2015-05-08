using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PlanetRandomizer
{
    static class PlanetAligner
    {
        public static void reloadDefaultPlanets()
        {
            Settings.Instance.Orbits = Settings.DefaultOrbits;
            AlignPlanetsToOrbits();
        }


        public static void BuildNewOrbits()
        {
            reloadDefaultPlanets();
            System.Random rand = new System.Random(Settings.Instance.seed);
            List<PlanetData> result = new CustomRandomizer(rand).MakePlanetData();
            Settings.Instance.Orbits = result.ToArray();
            AlignPlanetsToOrbits();
            ScienceModifier.BalanceScience(Planetarium.fetch.Home, -1);
        }

        public static void AlignPlanetsToOrbits()
        {
            print("Aligning " + Settings.Instance.Orbits + " Planets To New Orbits");
            CelestialBody sun = Planetarium.fetch.Sun;
            sun.orbitingBodies.Clear();
            foreach (PlanetData planet in Settings.Instance.Orbits)
            {
                CelestialBody target = (from c in FlightGlobals.Bodies where c.gameObject.name == planet.Name select c).FirstOrDefault();
                if (target != null)
                {
                    print("Aligning " + planet.ToString());
                    target.orbitingBodies.Clear();
                    double origRadius = target.Radius;
                    target.Radius = planet.Radius;

                    if (target.pqsController != null)
                    {
                        target.pqsController.radius = planet.Radius;
                    }


                    foreach (Transform t in ScaledSpace.Instance.scaledSpaceTransforms)
                    {
                        if (t.gameObject.name == target.gameObject.name)
                        {
                            float origLocalScale = t.localScale.x;
                            float scaleFactor = (float)((double)origLocalScale * planet.Radius / origRadius);
                            t.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                        }
                    }

                    target.Mass = planet.Mass;
                    target.GeeASL = target.Mass * (6.674E-11 / 9.81) / (target.Radius * target.Radius);
                    target.gMagnitudeAtCenter = target.Mass * 6.674E-11;
                    target.gravParameter = target.gMagnitudeAtCenter;

                    target.tidallyLocked = false;
                    target.rotationPeriod = planet.RotationPeriod;

                    target.orbit.semiMajorAxis = planet.SemiMajorAxis;
                    target.orbit.eccentricity = planet.Eccentricity;

                    CelestialBody targetref = (from cRef in FlightGlobals.Bodies where cRef.gameObject.name == planet.ReferenceBody select cRef).Single();
                    targetref.orbitingBodies.Add(target);
                    target.orbit.referenceBody = targetref;

                    target.orbit.inclination = planet.Inclination;
                    target.orbit.meanAnomalyAtEpoch = planet.MeanAnomalyAtEpoch;
                    target.orbit.LAN = planet.LAN;
                    target.orbit.argumentOfPeriapsis = planet.ArgumentOfPeriapsis;

                    target.orbitDriver.QueuedUpdate = true;
                    target.CBUpdate();
                    target.sphereOfInfluence = GetSOI(target);
                    target.hillSphere = GetHillSphere(target);
                    target.orbit.period = GetPeriod(target);

                    ScienceModifier.SetScienceIndex(target, planet.ScienceIndex);

                }
                else
                {
                    UnityEngine.Debug.LogError("No such planet: " + planet.Name);
                }
            }

            foreach (AtmosphereFromGround ag in Resources.FindObjectsOfTypeAll(typeof(AtmosphereFromGround)))
            {
                if (ag != null && ag.planet != null)
                {
                    // generalized version of Starwaster's code. Thanks Starwaster!
                    print("Found atmo for " + ag.planet.name + ": " + ag.name + ", has localScale " + ag.transform.localScale.x);
                    UpdateAFG(ag.planet, ag);
                    print("Atmo updated");
                }
            }

            print("Done changing planets");

        }

        public static void UpdateAFG(CelestialBody body, AtmosphereFromGround afg)
        {
            afg.outerRadius = (float)body.Radius * 1.025f;
            afg.innerRadius = afg.outerRadius * 0.975f;
            afg.KrESun = afg.Kr * afg.ESun;
            afg.KmESun = afg.Km * afg.ESun;
            afg.Kr4PI = afg.Kr * 4f * (float)Math.PI;
            afg.Km4PI = afg.Km * 4f * (float)Math.PI;
            afg.g2 = afg.g * afg.g;
            afg.outerRadius2 = afg.outerRadius * afg.outerRadius;
            afg.innerRadius2 = afg.innerRadius * afg.innerRadius;
            afg.scale = 1f / (afg.outerRadius - afg.innerRadius);
            afg.scaleDepth = -0.25f;
            afg.scaleOverScaleDepth = afg.scale / afg.scaleDepth;
        }

        private static double GetHillSphere(CelestialBody body)
        {
            return body.orbit.semiMajorAxis * (1.0 - body.orbit.eccentricity) * Math.Pow(body.Mass / (body.orbit.referenceBody.Mass + body.Mass), 1 / 3);
        }

        private static double GetSOI(CelestialBody body)
        {
            return Math.Max(body.orbit.semiMajorAxis * Math.Pow(body.Mass / (body.orbit.referenceBody.Mass + body.Mass), 0.5), body.Radius + body.scienceValues.spaceAltitudeThreshold * 1.2);
        }

        private static double GetPeriod(CelestialBody body)
        {
            return 2 * Math.PI * Math.Sqrt((Math.Pow(body.orbit.semiMajorAxis, 3) / 6.674E-11) / (body.Mass + body.referenceBody.Mass));
        }

        private static void print(object obj)
        {
            UnityEngine.Debug.Log(obj);
        }
    }
}
