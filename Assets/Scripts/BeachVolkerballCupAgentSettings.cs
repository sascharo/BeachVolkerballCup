using UnityEngine;

public class BeachVolkerballCupAgentSettings : MonoBehaviour
{
    /// <summary>
    /// The "walking speed" of the agents in the scene.
    /// </summary>
    public float agentRunSpeed = 3f;

    /// <summary>
    /// The agent rotation speed.
    /// Every agent will use this setting.
    /// </summary>
    public float agentRotationSpeed = 15f;

    /// <summary>
    /// The spawn area margin multiplier.
    /// ex: .9 means 90% of spawn area will be used.
    /// .1 margin will be left (so players don't spawn off of the edge).
    /// The higher this value, the longer training time required.
    /// </summary>
    public float spawnAreaMarginMultiplier = 0.5f;

    /// <summary>
    /// When a goal is scored the ground will switch to this
    /// material for a few seconds.
    /// </summary>
    public Material scoredMaterial;

    /// <summary>
    /// When an agent fails, the ground will turn this material for a few seconds.
    /// </summary>
    public Material failMaterial;
}
