using UnityEngine;

public static class ProceduralPlanetTexture
{
    const int W = 1024;
    const int H = 512;
    const float NormalStrength = 2.2f;

    public struct PlanetMaps
    {
        public Texture2D albedo;
        public Texture2D normal;
    }

    delegate Color ShadeFn(float lat, float lon, Vector3 p, out float height);

    public static PlanetMaps Generate(string name, Color fallback)
    {
        ShadeFn shade;
        switch (name)
        {
            case "Mercury": shade = ShadeMercury; break;
            case "Venus":   shade = ShadeVenus;   break;
            case "Earth":   shade = ShadeEarth;   break;
            case "Mars":    shade = ShadeMars;    break;
            case "Jupiter": shade = ShadeJupiter; break;
            case "Saturn":  shade = ShadeSaturn;  break;
            case "Uranus":  shade = ShadeUranus;  break;
            case "Neptune": shade = ShadeNeptune; break;
            default:        shade = (float lat, float lon, Vector3 p, out float h) => ShadeGeneric(p, fallback, out h); break;
        }
        return Build(name, shade, true);
    }

    public static PlanetMaps GenerateSun() => Build("Sun", ShadeSun, false);

    public static Texture2D GenerateSunGlow()
    {
        const int S = 256;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, true);
        tex.name = "SunGlow";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x - (S - 1) * 0.5f) / ((S - 1) * 0.5f);
                float dy = (y - (S - 1) * 0.5f) / ((S - 1) * 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float core = Mathf.Clamp01(1f - d / 0.25f);
                float halo = Mathf.Clamp01(1f - d);
                float a = core * 0.9f + halo * halo * halo * 0.35f;
                a = Mathf.Clamp01(a);
                pixels[y * S + x] = new Color32(255, 240, 200, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(true);
        return tex;
    }

    static PlanetMaps Build(string name, ShadeFn shade, bool withNormal)
    {
        var albedo = NewTexture(name + "_Albedo");
        var pixels = new Color32[W * H];
        var heights = withNormal ? new float[W * H] : null;

        var prev = Random.state;
        Random.InitState(name.GetHashCode());

        for (int y = 0; y < H; y++)
        {
            float v = y / (float)(H - 1);
            float lat = (v - 0.5f) * Mathf.PI;
            float cosLat = Mathf.Cos(lat);
            float sinLat = Mathf.Sin(lat);
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)W;
                float lon = u * Mathf.PI * 2f;
                Vector3 p = new Vector3(cosLat * Mathf.Cos(lon), sinLat, cosLat * Mathf.Sin(lon));
                Color c = shade(lat, lon, p, out float h);
                int idx = y * W + x;
                pixels[idx] = new Color32(
                    (byte)Mathf.Clamp(c.r * 255f, 0, 255),
                    (byte)Mathf.Clamp(c.g * 255f, 0, 255),
                    (byte)Mathf.Clamp(c.b * 255f, 0, 255),
                    255);
                if (heights != null) heights[idx] = Mathf.Clamp01(h);
            }
        }
        albedo.SetPixels32(pixels);
        albedo.Apply(true);

        Texture2D normal = null;
        if (withNormal)
        {
            normal = BuildNormalFromHeight(name + "_Normal", heights);
        }

        Random.state = prev;
        return new PlanetMaps { albedo = albedo, normal = normal };
    }

    static Texture2D NewTexture(string texName)
    {
        var tex = new Texture2D(W, H, TextureFormat.RGB24, true);
        tex.name = texName;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 16;
        return tex;
    }

    static Texture2D BuildNormalFromHeight(string texName, float[] heights)
    {
        var tex = new Texture2D(W, H, TextureFormat.RGB24, true, true);
        tex.name = texName;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;

        var pixels = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            int ym = (y == 0) ? 0 : y - 1;
            int yp = (y == H - 1) ? H - 1 : y + 1;
            for (int x = 0; x < W; x++)
            {
                int xm = (x - 1 + W) % W;
                int xp = (x + 1) % W;
                float dx = heights[y * W + xp] - heights[y * W + xm];
                float dy = heights[yp * W + x] - heights[ym * W + x];
                Vector3 n = new Vector3(-dx * NormalStrength, -dy * NormalStrength, 1f).normalized;
                pixels[y * W + x] = new Color32(
                    (byte)((n.x * 0.5f + 0.5f) * 255f),
                    (byte)((n.y * 0.5f + 0.5f) * 255f),
                    (byte)((n.z * 0.5f + 0.5f) * 255f),
                    255);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(true);
        return tex;
    }

    static float Hash3(int x, int y, int z)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + z * 1597334673);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xffffffu) / (float)0xffffffu;
    }

    static float ValueNoise(Vector3 p)
    {
        int xi = Mathf.FloorToInt(p.x);
        int yi = Mathf.FloorToInt(p.y);
        int zi = Mathf.FloorToInt(p.z);
        float xf = p.x - xi;
        float yf = p.y - yi;
        float zf = p.z - zi;
        float u = xf * xf * (3f - 2f * xf);
        float v = yf * yf * (3f - 2f * yf);
        float w = zf * zf * (3f - 2f * zf);
        float n000 = Hash3(xi,     yi,     zi    );
        float n100 = Hash3(xi + 1, yi,     zi    );
        float n010 = Hash3(xi,     yi + 1, zi    );
        float n110 = Hash3(xi + 1, yi + 1, zi    );
        float n001 = Hash3(xi,     yi,     zi + 1);
        float n101 = Hash3(xi + 1, yi,     zi + 1);
        float n011 = Hash3(xi,     yi + 1, zi + 1);
        float n111 = Hash3(xi + 1, yi + 1, zi + 1);
        float x00 = Mathf.Lerp(n000, n100, u);
        float x10 = Mathf.Lerp(n010, n110, u);
        float x01 = Mathf.Lerp(n001, n101, u);
        float x11 = Mathf.Lerp(n011, n111, u);
        float y0 = Mathf.Lerp(x00, x10, v);
        float y1 = Mathf.Lerp(x01, x11, v);
        return Mathf.Lerp(y0, y1, w);
    }

    static float Fbm(Vector3 p, int octaves, float lac = 2f, float gain = 0.5f)
    {
        float sum = 0f, amp = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += amp * ValueNoise(p);
            norm += amp;
            p *= lac;
            amp *= gain;
        }
        return sum / norm;
    }

    static float Ridged(Vector3 p, int octaves)
    {
        float sum = 0f, amp = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            float n = 1f - Mathf.Abs(ValueNoise(p) * 2f - 1f);
            sum += amp * n * n;
            norm += amp;
            p *= 2f;
            amp *= 0.5f;
        }
        return sum / norm;
    }

    static Color ShadeMercury(float lat, float lon, Vector3 p, out float height)
    {
        float terrain = Fbm(p * 2.5f, 6);
        float midDetail = Fbm(p * 8f + Vector3.one * 5f, 5);
        float craterField = Fbm(p * 14f + Vector3.one * 11f, 4);
        float fineDust = Fbm(p * 35f, 3);
        float craters = Mathf.SmoothStep(0.58f, 0.82f, craterField);
        height = Mathf.Clamp01(terrain * 0.55f + midDetail * 0.25f + fineDust * 0.08f - craters * 0.4f);
        float v = Mathf.Lerp(0.22f, 0.68f, terrain) + (midDetail - 0.5f) * 0.1f + (fineDust - 0.5f) * 0.04f;
        v -= craters * 0.32f;
        v = Mathf.Clamp01(v);
        return new Color(v * 1.02f, v * 0.96f, v * 0.88f);
    }

    static Color ShadeVenus(float lat, float lon, Vector3 p, out float height)
    {
        Vector3 flow = new Vector3(0f, Mathf.Sin(lat * 2.2f) * 0.35f, 0f);
        float n = Fbm(p * 1.8f + flow, 7);
        float swirl = Fbm(p * 4.5f + Vector3.one * 3f, 6);
        float bands = 0.5f + 0.5f * Mathf.Sin(lat * 6f + swirl * 2.2f);
        float t = n * 0.45f + swirl * 0.35f + bands * 0.25f;
        height = Mathf.Clamp01(n * 0.6f + swirl * 0.4f);
        Color deep   = new Color(0.56f, 0.38f, 0.14f);
        Color warm   = new Color(0.88f, 0.72f, 0.38f);
        Color bright = new Color(0.98f, 0.90f, 0.66f);
        Color c = t < 0.55f
            ? Color.Lerp(deep, warm,   Mathf.InverseLerp(0f, 0.55f, t))
            : Color.Lerp(warm, bright, Mathf.InverseLerp(0.55f, 1f, t));
        return c;
    }

    static Color ShadeEarth(float lat, float lon, Vector3 p, out float height)
    {
        float continents = Fbm(p * 1.8f, 7);
        float midDetail  = Fbm(p * 5f + Vector3.one * 7f, 5) * 0.18f;
        float microDetail = Fbm(p * 18f, 3) * 0.06f;
        float h = continents + midDetail + microDetail;

        Color c;
        if (h < 0.46f)
        {
            float t = Mathf.SmoothStep(0.22f, 0.46f, h);
            Color deep    = new Color(0.02f, 0.07f, 0.24f);
            Color mid     = new Color(0.05f, 0.20f, 0.45f);
            Color shallow = new Color(0.12f, 0.40f, 0.60f);
            c = Color.Lerp(deep, Color.Lerp(mid, shallow, t), t);
        }
        else if (h < 0.49f)
        {
            float t = Mathf.SmoothStep(0.46f, 0.49f, h);
            c = Color.Lerp(new Color(0.75f, 0.68f, 0.48f), new Color(0.52f, 0.55f, 0.30f), t);
        }
        else if (h < 0.68f)
        {
            float t = Mathf.SmoothStep(0.49f, 0.68f, h);
            Color forest = new Color(0.10f, 0.32f, 0.10f);
            Color plains = new Color(0.32f, 0.44f, 0.16f);
            Color arid   = new Color(0.55f, 0.48f, 0.22f);
            c = t < 0.5f
                ? Color.Lerp(forest, plains, t * 2f)
                : Color.Lerp(plains, arid, (t - 0.5f) * 2f);
        }
        else
        {
            float t = Mathf.SmoothStep(0.68f, 0.90f, h);
            c = Color.Lerp(new Color(0.45f, 0.38f, 0.24f), new Color(0.88f, 0.84f, 0.80f), t);
        }

        float iceBand = Mathf.Abs(lat) + (Fbm(p * 4f, 3) - 0.5f) * 0.22f;
        float ice = Mathf.SmoothStep(1.10f, 1.38f, iceBand);
        c = Color.Lerp(c, new Color(0.96f, 0.98f, 1f), ice);

        float cloudBase = Fbm(p * 2.2f + new Vector3(5f, 3f, 9f), 6);
        float cloudDetail = Fbm(p * 6f + new Vector3(1f, 4f, 2f), 4);
        float cloudMask = Mathf.SmoothStep(0.52f, 0.70f, cloudBase * 0.6f + cloudDetail * 0.4f);
        c = Color.Lerp(c, Color.white, cloudMask * 0.55f);

        height = Mathf.Clamp01(h);
        return c;
    }

    static Color ShadeMars(float lat, float lon, Vector3 p, out float height)
    {
        float broad = Fbm(p * 1.8f, 6);
        float mid = Fbm(p * 6f + Vector3.one * 3f, 5);
        float fine = Fbm(p * 18f, 3);
        float ridges = Ridged(p * 4f + Vector3.one * 9f, 5);
        float craters = Mathf.SmoothStep(0.62f, 0.82f, Fbm(p * 11f + Vector3.one * 17f, 3));
        float h = broad * 0.55f + mid * 0.25f + fine * 0.08f + ridges * 0.15f - craters * 0.25f;
        height = Mathf.Clamp01(h);

        Color lowland = new Color(0.38f, 0.14f, 0.06f);
        Color mid_c   = new Color(0.68f, 0.28f, 0.12f);
        Color highland = new Color(0.88f, 0.48f, 0.22f);
        Color rocky   = new Color(0.55f, 0.22f, 0.10f);

        Color c = h < 0.45f
            ? Color.Lerp(lowland, mid_c, Mathf.InverseLerp(0.15f, 0.45f, h))
            : Color.Lerp(mid_c, highland, Mathf.InverseLerp(0.45f, 0.85f, h));

        c = Color.Lerp(c, rocky, Mathf.SmoothStep(0.55f, 0.82f, ridges) * 0.4f);
        c = Color.Lerp(c, new Color(0.20f, 0.08f, 0.04f), craters * 0.35f);

        float polarNoise = Fbm(p * 3f + Vector3.one * 23f, 3) * 0.18f;
        float cap = Mathf.SmoothStep(1.18f, 1.42f, Mathf.Abs(lat) + polarNoise);
        c = Color.Lerp(c, new Color(0.96f, 0.94f, 0.92f), cap);
        return c;
    }

    static Color ShadeJupiter(float lat, float lon, Vector3 p, out float height)
    {
        float latJitter = Fbm(p * 2.5f, 4) * 0.18f;
        float bandsWave = Mathf.Sin((lat + latJitter) * 16f);
        float bandsBroad = Mathf.Sin((lat + latJitter * 0.3f) * 5f);
        float turb = Fbm(p * 5f + new Vector3(0f, lat * 4f, 0f), 6);
        float stormFine = Fbm(p * 10f + Vector3.one * 17f, 5);
        float t = Mathf.Clamp01(0.5f + 0.35f * bandsWave + 0.18f * bandsBroad + (turb - 0.5f) * 0.45f);
        height = Mathf.Clamp01(0.5f + 0.2f * bandsWave + (stormFine - 0.5f) * 0.25f);

        Color deepBrown   = new Color(0.42f, 0.26f, 0.14f);
        Color tanBand     = new Color(0.72f, 0.55f, 0.32f);
        Color creamBand   = new Color(0.94f, 0.86f, 0.68f);
        Color c = t < 0.5f
            ? Color.Lerp(deepBrown, tanBand, t * 2f)
            : Color.Lerp(tanBand, creamBand, (t - 0.5f) * 2f);
        c = Color.Lerp(c, new Color(0.55f, 0.40f, 0.22f), Mathf.SmoothStep(0.65f, 0.85f, stormFine) * 0.35f);

        float dLat = lat - (-0.38f);
        float dLon = Mathf.DeltaAngle(lon * Mathf.Rad2Deg, 240f) * Mathf.Deg2Rad;
        float spotDist = Mathf.Sqrt(dLat * dLat * 2.8f + dLon * dLon);
        float spotCore = Mathf.SmoothStep(0.22f, 0.05f, spotDist);
        float spotHalo = Mathf.SmoothStep(0.32f, 0.10f, spotDist) - spotCore;
        c = Color.Lerp(c, new Color(0.72f, 0.22f, 0.10f), spotCore);
        c = Color.Lerp(c, new Color(0.85f, 0.45f, 0.28f), spotHalo * 0.6f);
        height += spotCore * 0.15f;
        return c;
    }

    static Color ShadeSaturn(float lat, float lon, Vector3 p, out float height)
    {
        float latJitter = Fbm(p * 1.8f, 3) * 0.10f;
        float bands = Mathf.Sin((lat + latJitter) * 11f);
        float bandsBroad = Mathf.Sin(lat * 4f);
        float turb = Fbm(p * 3.5f, 5);
        float t = Mathf.Clamp01(0.5f + 0.3f * bands + 0.15f * bandsBroad + (turb - 0.5f) * 0.35f);
        height = Mathf.Clamp01(0.5f + 0.12f * bands + (turb - 0.5f) * 0.2f);

        Color deep  = new Color(0.78f, 0.62f, 0.36f);
        Color mid   = new Color(0.92f, 0.82f, 0.58f);
        Color light = new Color(0.98f, 0.92f, 0.74f);
        Color c = t < 0.5f
            ? Color.Lerp(deep, mid, t * 2f)
            : Color.Lerp(mid, light, (t - 0.5f) * 2f);

        float polarBand = Mathf.SmoothStep(1.05f, 1.35f, Mathf.Abs(lat) + Fbm(p * 4f, 3) * 0.15f);
        c = Color.Lerp(c, new Color(0.80f, 0.78f, 0.72f), polarBand * 0.5f);
        return c;
    }

    static Color ShadeUranus(float lat, float lon, Vector3 p, out float height)
    {
        float bands = Mathf.Sin(lat * 4f + Fbm(p * 2f, 4) * 0.7f);
        float turb = Fbm(p * 3f + Vector3.one * 5f, 5);
        float t = Mathf.Clamp01(0.5f + 0.18f * bands + (turb - 0.5f) * 0.3f);
        height = Mathf.Clamp01(0.5f + (turb - 0.5f) * 0.25f);

        Color c1 = new Color(0.55f, 0.82f, 0.85f);
        Color c2 = new Color(0.72f, 0.92f, 0.95f);
        Color c = Color.Lerp(c1, c2, t);
        return c;
    }

    static Color ShadeNeptune(float lat, float lon, Vector3 p, out float height)
    {
        float latJitter = Fbm(p * 2f, 3) * 0.25f;
        float bands = Mathf.Sin((lat + latJitter) * 8f);
        float turb = Fbm(p * 4f + Vector3.one * 3f, 6);
        float stormFine = Fbm(p * 12f + Vector3.one * 19f, 5);
        float t = Mathf.Clamp01(0.5f + 0.25f * bands + (turb - 0.5f) * 0.4f);
        height = Mathf.Clamp01(0.5f + 0.15f * bands + (stormFine - 0.5f) * 0.25f);

        Color deep  = new Color(0.08f, 0.22f, 0.55f);
        Color mid   = new Color(0.18f, 0.38f, 0.80f);
        Color bright = new Color(0.38f, 0.58f, 0.92f);
        Color c = t < 0.5f
            ? Color.Lerp(deep, mid, t * 2f)
            : Color.Lerp(mid, bright, (t - 0.5f) * 2f);

        float dLat = lat - (-0.45f);
        float dLon = Mathf.DeltaAngle(lon * Mathf.Rad2Deg, 170f) * Mathf.Deg2Rad;
        float spot = Mathf.SmoothStep(0.22f, 0.02f, Mathf.Sqrt(dLat * dLat * 3.5f + dLon * dLon));
        c = Color.Lerp(c, new Color(0.04f, 0.10f, 0.38f), spot);

        float cirrus = Mathf.SmoothStep(0.72f, 0.88f, stormFine);
        c = Color.Lerp(c, new Color(0.92f, 0.95f, 1f), cirrus * 0.35f);
        return c;
    }

    static Color ShadeGeneric(Vector3 p, Color baseColor, out float height)
    {
        float n = Fbm(p * 3f, 5);
        height = n;
        return Color.Lerp(baseColor * 0.6f, baseColor * 1.2f, n);
    }

    static Color ShadeSun(float lat, float lon, Vector3 p, out float height)
    {
        float granules = Fbm(p * 10f, 6);
        float flares   = Fbm(p * 2.5f + Vector3.one * 4f, 5);
        float hotSpots = Mathf.SmoothStep(0.65f, 0.88f, flares);
        float t = granules * 0.55f + flares * 0.45f;
        height = t;
        Color cool = new Color(1.0f, 0.45f, 0.12f);
        Color warm = new Color(1.0f, 0.78f, 0.30f);
        Color hot  = new Color(1.0f, 0.96f, 0.72f);
        Color c = t < 0.55f
            ? Color.Lerp(cool, warm, Mathf.InverseLerp(0f, 0.55f, t))
            : Color.Lerp(warm, hot,  Mathf.InverseLerp(0.55f, 1f, t));
        c = Color.Lerp(c, new Color(1f, 1f, 0.90f), hotSpots * 0.55f);
        return c;
    }
}
