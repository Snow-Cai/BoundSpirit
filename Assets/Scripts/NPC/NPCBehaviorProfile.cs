using UnityEngine;

[CreateAssetMenu(fileName = "MomBehaviorProfile", menuName = "NPC/Mom Behavior Profile")]
public class MomBehaviorProfile : ScriptableObject
{
    [Header("Idle Settings")]
    public float minIdleTime = 1.5f;
    public float maxIdleTime = 4f;

    [Header("Behavior Modes")]
    public bool allowRandomWander = true;   // Allow random wandering mode
    public bool useSchedule = true;         // Uses scheduled points if enabled
}
