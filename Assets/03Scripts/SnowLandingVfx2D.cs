using UnityEngine;

public enum SnowLandingImpactStrength
{
    Weak = 0,
    Medium = 1,
    Strong = 2
}

public sealed class SnowLandingVfx2D : MonoBehaviour
{
    [SerializeField] private ParticleSystem mainParticles;
    [SerializeField] private ParticleSystem accentParticles;
    [SerializeField] private ParticleSystem groundParticles;
    [SerializeField] private int sortingOrder = 80;
    [SerializeField] private bool destroyAfterPlay;
    [SerializeField] private float destroyDelay = 1.8f;

    private void Awake()
    {
        EnsureParticleSystems();
    }

    public static SnowLandingVfx2D Spawn(Vector3 position, SnowLandingImpactStrength strength)
    {
        GameObject root = new("VFX_SnowLanding");
        root.transform.position = position;

        SnowLandingVfx2D vfx = root.AddComponent<SnowLandingVfx2D>();
        vfx.destroyAfterPlay = true;
        vfx.Play(strength);
        return vfx;
    }

    public void Play(SnowLandingImpactStrength strength)
    {
        Play(strength, transform.position);
    }

    public void Play(SnowLandingImpactStrength strength, Vector3 position)
    {
        transform.position = position;
        EnsureParticleSystems();

        mainParticles.Clear(true);
        accentParticles.Clear(true);
        groundParticles.Clear(true);

        EmitMain(strength);

        if (strength >= SnowLandingImpactStrength.Medium)
            EmitAccent(strength);

        if (strength >= SnowLandingImpactStrength.Strong)
            EmitGround();

        mainParticles.Play();
        if (strength >= SnowLandingImpactStrength.Medium)
            accentParticles.Play();

        if (strength >= SnowLandingImpactStrength.Strong)
            groundParticles.Play();

        if (destroyAfterPlay)
            Destroy(gameObject, Mathf.Max(0.1f, destroyDelay));
    }

    private void EmitMain(SnowLandingImpactStrength strength)
    {
        int count = strength switch
        {
            SnowLandingImpactStrength.Strong => Random.Range(40, 61),
            SnowLandingImpactStrength.Medium => Random.Range(20, 31),
            _ => Random.Range(10, 16)
        };

        float speed = strength switch
        {
            SnowLandingImpactStrength.Strong => Random.Range(3f, 4f),
            SnowLandingImpactStrength.Medium => 2f,
            _ => 1f
        };

        float size = strength switch
        {
            SnowLandingImpactStrength.Strong => Random.Range(0.25f, 0.35f),
            SnowLandingImpactStrength.Medium => 0.2f,
            _ => 0.15f
        };

        float horizontalSpread = strength switch
        {
            SnowLandingImpactStrength.Strong => 1.35f,
            SnowLandingImpactStrength.Medium => 0.95f,
            _ => 0.55f
        };

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.EmitParams emitParams = new()
            {
                position = transform.position,
                velocity = new Vector3(
                    Random.Range(-horizontalSpread, horizontalSpread) * speed,
                    Random.Range(0.6f, 1.25f) * speed,
                    0f),
                startLifetime = Random.Range(0.45f, 0.82f),
                startSize = Random.Range(size * 0.72f, size * 1.25f),
                startColor = new Color(1f, 1f, 1f, Random.Range(0.72f, 0.95f))
            };

            mainParticles.Emit(emitParams, 1);
        }
    }

    private void EmitAccent(SnowLandingImpactStrength strength)
    {
        int count = strength == SnowLandingImpactStrength.Strong
            ? Random.Range(8, 13)
            : Random.Range(4, 7);

        float speed = strength == SnowLandingImpactStrength.Strong ? 3.4f : 2.1f;
        float horizontalSpread = strength == SnowLandingImpactStrength.Strong ? 1.55f : 1.1f;

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.EmitParams emitParams = new()
            {
                position = transform.position,
                velocity = new Vector3(
                    Random.Range(-horizontalSpread, horizontalSpread) * speed,
                    Random.Range(0.75f, 1.45f) * speed,
                    0f),
                startLifetime = Random.Range(0.38f, 0.72f),
                startSize = Random.Range(0.055f, 0.14f),
                startColor = new Color(0.56f, 0.66f, 0.78f, Random.Range(0.42f, 0.72f))
            };

            accentParticles.Emit(emitParams, 1);
        }
    }

    private void EmitGround()
    {
        int count = Random.Range(22, 34);
        for (int i = 0; i < count; i++)
        {
            float direction = Random.value < 0.5f ? -1f : 1f;
            ParticleSystem.EmitParams emitParams = new()
            {
                position = transform.position + new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(-0.02f, 0.04f), 0f),
                velocity = new Vector3(
                    direction * Random.Range(1.8f, 4.6f),
                    Random.Range(0.04f, 0.34f),
                    0f),
                startLifetime = Random.Range(0.26f, 0.48f),
                startSize = Random.Range(0.12f, 0.24f),
                startColor = new Color(0.82f, 0.92f, 1f, Random.Range(0.28f, 0.52f))
            };

            groundParticles.Emit(emitParams, 1);
        }
    }

    private void EnsureParticleSystems()
    {
        mainParticles = ResolveParticleSystem("ParticleSystem_Main", mainParticles, new Color(1f, 1f, 1f, 1f), 0.08f, sortingOrder);
        accentParticles = ResolveParticleSystem("ParticleSystem_Accent", accentParticles, new Color(0.56f, 0.66f, 0.78f, 0.7f), 0.04f, sortingOrder + 1);
        groundParticles = ResolveParticleSystem("ParticleSystem_Ground", groundParticles, new Color(0.82f, 0.92f, 1f, 0.5f), 0.02f, sortingOrder + 2);
    }

    private ParticleSystem ResolveParticleSystem(
        string systemName,
        ParticleSystem particleSystem,
        Color startColor,
        float gravityModifier,
        int order)
    {
        if (particleSystem == null)
        {
            Transform existing = transform.Find(systemName);
            if (existing != null)
                particleSystem = existing.GetComponent<ParticleSystem>();
        }

        if (particleSystem == null)
        {
            GameObject systemObject = new(systemName);
            systemObject.transform.SetParent(transform, false);
            particleSystem = systemObject.AddComponent<ParticleSystem>();
        }

        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.65f;
        main.startDelay = 0f;
        main.startLifetime = 0.7f;
        main.startSpeed = 0f;
        main.startSize = 0.18f;
        main.startColor = startColor;
        main.gravityModifier = gravityModifier;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 128;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new(
            new Keyframe(0f, 1f),
            new Keyframe(0.72f, 0.82f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = order;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return particleSystem;
    }

    private void OnValidate()
    {
        sortingOrder = Mathf.Max(-32768, sortingOrder);
        destroyDelay = Mathf.Max(0.1f, destroyDelay);
    }
}
