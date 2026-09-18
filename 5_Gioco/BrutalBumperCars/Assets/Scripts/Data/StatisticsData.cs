using System;
using UnityEngine;

[Serializable]
public class StatisticsData
{
    public int victories;
    public int defeats;
    public int kills;
    public int deaths;

    public float VSRatio
    {
        get
        {
            if (defeats == 0)
                return victories;

            float vl = (float)victories / defeats;

            return (float)Math.Round(vl,2);
        }
    }

    public float KDRatio
    {
        get
        {
            if (deaths == 0)
                return kills;

            float kd = (float)kills / deaths;

            return (float)Math.Round(kd, 2);

        }
    }
}
