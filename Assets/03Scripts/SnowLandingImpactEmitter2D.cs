using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class SnowLandingImpactEmitter2D : MonoBehaviour
{
    [SerializeField] private SnowLandingVfx2D reusableVfx;
    [SerializeField] private float weakFallSpeed = 1.5f;
    [SerializeField] private float mediumFallSpeed = 5f;
    [SerializeField] private float strongFallSpeed = 9f;
    [SerializeField] private float replayCooldown = 0.12f;

    private Rigidbody2D targetRigidbody;
    private Vector2 previousVelocity;
    private float lastPlayTime = -999f;

    private void Awake()
    {
        targetRigidbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (targetRigidbody != null)
            previousVelocity = targetRigidbody.linearVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float fallSpeed = Mathf.Max(0f, -previousVelocity.y);
        if (fallSpeed < weakFallSpeed || Time.time < lastPlayTime + replayCooldown)
            return;

        SnowLandingImpactStrength strength = ResolveStrength(fallSpeed);
        Vector3 position = ResolveImpactPosition(collision);
        Play(strength, position);
    }

    public void Play(SnowLandingImpactStrength strength, Vector3 position)
    {
        lastPlayTime = Time.time;

        if (reusableVfx != null)
        {
            reusableVfx.Play(strength, position);
            return;
        }

        SnowLandingVfx2D.Spawn(position, strength);
    }

    private SnowLandingImpactStrength ResolveStrength(float fallSpeed)
    {
        if (fallSpeed >= strongFallSpeed)
            return SnowLandingImpactStrength.Strong;

        if (fallSpeed >= mediumFallSpeed)
            return SnowLandingImpactStrength.Medium;

        return SnowLandingImpactStrength.Weak;
    }

    private static Vector3 ResolveImpactPosition(Collision2D collision)
    {
        if (collision != null && collision.contactCount > 0)
            return collision.GetContact(0).point;

        return Vector3.zero;
    }

    private void OnValidate()
    {
        weakFallSpeed = Mathf.Max(0.01f, weakFallSpeed);
        mediumFallSpeed = Mathf.Max(weakFallSpeed, mediumFallSpeed);
        strongFallSpeed = Mathf.Max(mediumFallSpeed, strongFallSpeed);
        replayCooldown = Mathf.Max(0f, replayCooldown);
    }
}
