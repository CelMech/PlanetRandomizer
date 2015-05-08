using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetRandomizer
{
    static class ScienceModifier
    {
        private static List<CelestialBody> sciencedBodies = new List<CelestialBody>();

        private static CelestialBodyScienceParams[] defaultScience;

        public static void MemorizeDefaultScience()
        {
            defaultScience = new CelestialBodyScienceParams[FlightGlobals.Bodies.Count];
            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                var body = FlightGlobals.Bodies[i];
                var science = new CelestialBodyScienceParams();

                science.LandedDataValue = body.scienceValues.LandedDataValue;
                science.SplashedDataValue = body.scienceValues.SplashedDataValue;
                science.FlyingLowDataValue = body.scienceValues.FlyingLowDataValue;
                science.FlyingHighDataValue = body.scienceValues.FlyingHighDataValue;
                science.InSpaceLowDataValue = body.scienceValues.InSpaceLowDataValue;
                science.InSpaceHighDataValue = body.scienceValues.InSpaceHighDataValue;

                defaultScience[i] = science;
            }
        }

        public static void BalanceScience(CelestialBody primary, int science)
        {
            UnityEngine.Debug.Log("Balancing Science for " + primary.name);
            assignScience(primary, science);

            sciencedBodies.Add(primary);
            foreach (CelestialBody body in primary.orbitingBodies.Except(sciencedBodies))
            {
                BalanceScience(body, science+1);
            }
            if(primary.referenceBody != null && !sciencedBodies.Contains(primary.referenceBody)) BalanceScience(primary.referenceBody, science+1);
        }

        public static void RestoreScience()
        {
            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                var body = FlightGlobals.Bodies[i];
                var science = defaultScience[i];

                body.scienceValues.LandedDataValue = science.LandedDataValue;
                body.scienceValues.SplashedDataValue = science.SplashedDataValue;
                body.scienceValues.FlyingLowDataValue = science.FlyingLowDataValue;
                body.scienceValues.FlyingHighDataValue = science.FlyingHighDataValue;
                body.scienceValues.InSpaceLowDataValue = science.InSpaceLowDataValue;
                body.scienceValues.InSpaceHighDataValue = science.InSpaceHighDataValue;
            }
        }

        private static void assignScience(CelestialBody primary, int science)
        {
            if (primary == Planetarium.fetch.Sun || primary == Planetarium.fetch.Home)
            {
                UnityEngine.Debug.Log("Science values kept.");
                return;
            }
            //Interplanetary bodies or those with high gravity increase their science.
            if (surfaceGravity(primary) > 2.2) science += 1;
            else if(!primary.atmosphere) {
                if (primary.referenceBody == Planetarium.fetch.Sun || science > 2) science += 1;
            }
            UnityEngine.Debug.Log("Science Index: "+science);

            Settings.Instance.Orbits.SkipWhile(d => d.currentBody != primary).First().ScienceIndex = science;

            SetScienceIndex(primary, science);
        }

        public static void SetScienceIndex(CelestialBody primary, int science)
        {
            switch (science)
            {
                case 0:
                    assignScience(primary, 5f, 5f, 4f, 4f, 3f, 2.25f);
                    break;
                case 1:
                    assignScience(primary, 7f, 7f, 6f, 6f, 5f, 3f);
                    break;
                case 2:
                    assignScience(primary, 9f, 9f, 8f, 8f, 7f, 5f);
                    break;
                case 3:
                    assignScience(primary, 12f, 12f, 10f, 10f, 9f, 8f);
                    break;
                default:
                    assignScience(primary, 14f, 14f, 12f, 12f, 10f, 9f);
                    break;
            }
        }

        private static void assignScience(CelestialBody primary, float landed, float splashed, float flyingLow, float flyingHigh, float lowSpace, float highSpace)
        {
            primary.scienceValues.LandedDataValue = landed;
            primary.scienceValues.SplashedDataValue = splashed;
            primary.scienceValues.FlyingHighDataValue = flyingHigh;
            primary.scienceValues.FlyingLowDataValue = flyingLow;
            primary.scienceValues.InSpaceHighDataValue = lowSpace;
            primary.scienceValues.InSpaceLowDataValue = highSpace;
        }

        private static double surfaceGravity(CelestialBody body)
        {
            return body.gMagnitudeAtCenter / (body.Radius * body.Radius);
        }
    }
}
