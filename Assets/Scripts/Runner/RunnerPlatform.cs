using UnityEngine;

public class RunnerPlatform : MonoBehaviour
{
    public int Id;
    public float Width;
    public bool IsSafe;
    public bool IsBranch;
    public bool IsBossGate;
    public bool IsArenaPlatform;
    public int ArenaOwnerId;
    public float MilestoneDistance;
    public float Left => transform.position.x - Width * .5f;
    public float Right => transform.position.x + Width * .5f;
    public float Top => transform.position.y;
}
