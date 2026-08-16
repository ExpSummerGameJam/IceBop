using UnityEngine;

/// <summary>
/// 별개의 연출용 스크립트: 기존 WorldCookingArea2D 코드는 건드리지 않고,
/// 같은 GameObject의 트리거 콜라이더에 플레이어가 처음 들어왔을 때만 대사를 재생한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class CookingAreaFirstEntryDialogue2D : MonoBehaviour
{
    private const string PlayedPrefsKey = "CookingAreaFirstEntryDialogue2D.Played";

    [SerializeField] private string playerTag = "Player";

    [SerializeField]
    private string[] dialogueLines =
    {
        "여기서 요리를 할수 있을 것 같군…",
        "핵심 재료 와 강화재료 한가지 씩만 있으면…",
        "스프라도 만들어 볼 수 있을 것 같은데…",
        "가방을 한번 열어 볼까…",
    };

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (HasAlreadyPlayed() || !IsPlayer(other.gameObject))
            return;

        NpcSpeechBubble2D speechBubble = ResolveSpeechBubble(other.gameObject);
        if (speechBubble == null)
            return;

        speechBubble.QueueSpeech(dialogueLines);
        MarkAsPlayed();
    }

    private NpcSpeechBubble2D ResolveSpeechBubble(GameObject player)
    {
        NpcSpeechBubble2D speechBubble = player.GetComponentInParent<NpcSpeechBubble2D>();
        if (speechBubble != null)
            return speechBubble;

        GameObject speaker = player.GetComponentInParent<GatheringPlayerMarker>() is { } marker
            ? marker.gameObject
            : player;

        return speaker.AddComponent<NpcSpeechBubble2D>();
    }

    private bool IsPlayer(GameObject candidate)
    {
        if (candidate == null)
            return false;

        if (candidate.GetComponentInParent<GatheringPlayerMarker>() != null)
            return true;

        return !string.IsNullOrEmpty(playerTag) &&
               string.Equals(candidate.tag, playerTag, System.StringComparison.Ordinal);
    }

    private static bool HasAlreadyPlayed()
    {
        return PlayerPrefs.GetInt(PlayedPrefsKey, 0) != 0;
    }

    private static void MarkAsPlayed()
    {
        PlayerPrefs.SetInt(PlayedPrefsKey, 1);
        PlayerPrefs.Save();
    }
}
