using UnityEngine;

[System.Serializable]
public class PlanetData
{
    public string planetName = "Planet";

    [Tooltip("Mass relative to Earth (Earth = 1)")]
    public float relativeMass = 1f;

    [Tooltip("Distance from the sun in AU (Earth = 1)")]
    public float distanceFromSun = 1f;

    public bool explored = false;

    [Tooltip("Score used by probe to prioritize this planet as next target")]
    public float explorationScore = 0f;
}
