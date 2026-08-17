using System;
using UnityEngine;
using IceBop.JumpKing;

[DisallowMultipleComponent]
public sealed class PlayerHeightLayeredAudioController : MonoBehaviour
{
    private const int MaxLayerCount = 3;

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private ProgressiveLayeredAudioController layeredAudio;

    [Header("Layer Activation Heights (world Y, lowest layer first)")]
    [SerializeField] private float[] layerActivationHeights = new float[MaxLayerCount];

    private void Reset()
    {
        layeredAudio = GetComponent<ProgressiveLayeredAudioController>();
    }

    private void OnValidate()
    {
        if (layerActivationHeights == null || layerActivationHeights.Length != MaxLayerCount)
            Array.Resize(ref layerActivationHeights, MaxLayerCount);
    }

    private void Awake()
    {
        if (layeredAudio == null)
            layeredAudio = GetComponent<ProgressiveLayeredAudioController>();

        if (player == null)
        {
            JumpKingCharacter character = FindFirstObjectByType<JumpKingCharacter>();
            if (character != null)
                player = character.transform;
        }
    }

    private void Update()
    {
        if (player == null || layeredAudio == null)
            return;

        int targetLayerCount = ResolveActiveLayerCount(player.position.y);
        if (targetLayerCount != layeredAudio.ActiveLayerCount)
            layeredAudio.SetActiveLayerCount(targetLayerCount);
    }

    private int ResolveActiveLayerCount(float playerHeight)
    {
        int activeCount = 0;

        for (int i = 0; i < MaxLayerCount; i++)
        {
            if (playerHeight < layerActivationHeights[i])
                break;

            activeCount = i + 1;
        }

        return activeCount;
    }
}
