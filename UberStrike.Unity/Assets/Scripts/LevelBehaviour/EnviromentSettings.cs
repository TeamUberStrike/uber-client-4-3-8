
using System;
using UnityEngine;

[Serializable]
public class EnviromentSettings
{
    public enum TYPE
    {
        NONE,
        WATER,
        SURFACE,
        LATTER,
    }

    public TYPE Type;
    public Bounds EnviromentBounds;

    public float GroundAcceleration = 15;
    public float GroundFriction = 8;

    public float AirAcceleration = 3;

    public float WaterAcceleration = 6;
    public float WaterFriction = 2;

    public float FlyAcceleration = 8;
    public float FlyFriction = 3;

    public float SpectatorFriction = 5;

    public float StopSpeed = 8;

    public float Gravity = 50;

    public void CheckPlayerEnclosure(Vector3 position, float height, out float enclosure)
    {
        //at least partly enclosed (1% - 100%)
        if (EnviromentBounds.Contains(position))
        {
            Vector3 top = position + Vector3.up * height;

            float distance;
            if (EnviromentBounds.IntersectRay(new Ray(top, Vector3.down), out distance))
            {
                enclosure = (height - distance) / height;
            }
            //thats weird and should never happen
            else
            {
                //Debug.LogError(EnviromentBounds + " does not contain point " + position);
                enclosure = 0;
            }
        }
        //outside of the enviroment (0%)
        else
        {
            enclosure = 0;
        }
    }

    public override string ToString()
    {
        return string.Format("Type: ", Type);
    }
}
