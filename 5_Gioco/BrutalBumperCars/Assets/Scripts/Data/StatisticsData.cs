using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StatisticsData
{
    public long victories;
    public long defeats;

    public long kills;
    public long deaths;

    public long damageDealt;
    public long damageTaken;
    public long damageDefended;

    public List<CarDistanceData> carDistances;

    public double VSRatio
    {
        get
        {
            if (defeats == 0)
                return victories;

            float vl = (float)victories / defeats;

            return (float)Math.Round(vl,2);
        }
    }

    public double KDRatio
    {
        get
        {
            if (deaths == 0)
                return kills;

            float kd = (float)kills / deaths;

            return (float)Math.Round(kd, 2);

        }
    }

    public List<CarDistanceData> GetTopCars(int count)
    {
        return carDistances
            .OrderByDescending(car => car.distance)
            .Take(count)
            .ToList();
    }
}
