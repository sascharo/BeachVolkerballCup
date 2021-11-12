using UnityEngine;

public class BeachVolkerballCupAgentMove : MonoBehaviour
{
    [Header("RUNNING")]
    [Range(0f, 100f)]
    public float agentRunSpeed = 10f;
    [Range(0f, 100f)]
    public float agentTerminalVel = 20f;
    public ForceMode runningForceMode = ForceMode.Impulse;
    [Header("ROTATING")]
    [Range(1f, 10f)]
    public float agentSensitivity = 3f; // 2f
    [Range(0f, 1f)]
    public float agentSmoothTime = 0.05f;
    [Header("RIGIDBODY")]
    [Range(0f, 1000f)]
    public float maxAngularVel = 50f;
    
    private float _yaw;
    private float _yawSmooth;
    private float _yawSmoothV;
    private Rigidbody _agentRB;
    
    void Awake()
    {
        _agentRB = GetComponent<Rigidbody>();
        _agentRB.maxAngularVelocity = maxAngularVel;
    }

    public void Turn(float rot = 0f)
    {
        _yaw += rot * agentSensitivity;
        float sy = _yawSmooth;
        _yawSmooth = Mathf.SmoothDampAngle(_yawSmooth, _yaw, ref _yawSmoothV, agentSmoothTime);
        _agentRB.MoveRotation(_agentRB.rotation * Quaternion.AngleAxis(Mathf.DeltaAngle(sy, _yawSmooth), transform.up));
    }
    
    public void Run(Vector3 dir)
    {
        var vel = _agentRB.velocity.magnitude;
        float adjustedSpeed = Mathf.Clamp(agentRunSpeed - vel, 0, agentTerminalVel);
        _agentRB.AddForce(dir * adjustedSpeed, runningForceMode);
    }
}
