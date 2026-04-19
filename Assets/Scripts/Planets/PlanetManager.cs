using System.Collections.Generic;
using UnityEngine;

public class PlanetManager : MonoBehaviour
{
    public static PlanetManager Instance { get; private set; }

    [Tooltip("Planets in the scene. If left empty, auto-populated from the scene at Awake.")]
    public List<Planet> planets = new List<Planet>();

    void Awake()
    {
        Instance = this;

        if (planets == null || planets.Count == 0)
            planets = new List<Planet>(FindObjectsByType<Planet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
    }

    public Planet FindByName(string n) =>
        planets.Find(p => p != null && p.data.planetName == n);
}
