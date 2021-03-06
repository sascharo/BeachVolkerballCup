using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class BeachVolkerballCupAgent : Agent
{
    readonly double TOLERANCE = 0.000000001;
    
    private bool _initialized = false;
    //private bool _initializedByController = false;

    private BehaviorParameters _behaviorParameters;
    
    private BeachVolkerballCupController _controller;
    
    private Vector3 _restPos;
    //private Quaternion _restRot;

    [Header("OBSERVATIONS")]
    [HideInInspector]
    public BeachVolkerballCupAgentInput input;
    private float _inputH;
    private float _inputV;
    private float _inputAxisX;
    private float _inputThrowStrength;
    //private float _inputBoost;
    private BeachVolkerballCupAgentMove _move;
    [HideInInspector]
    public float gotHit { get; private set; } = 0f;
    
    [Header("TEAM")]
    //private int _teamId;
    public int playerId;

    [Header("COLLIDERS / RIGID BODY")]
    [HideInInspector]
    public Collider colliderFront;
    //[HideInInspector]
    //public Collider colliderBody;
    [HideInInspector]
    public GameObject colliderShieldGo;
    private Rigidbody _rigidBody;

    [Header("BALL PROJECTILE")]
    public Transform projectileTransform;

    [Header("DEBUG")]
    private Renderer _renderer;
    private Material _materialBodyDefault;
    //public Material materialBodyCarry;

    void Awake()
    {
        _renderer = transform.Find("Body").GetComponent<Renderer>();
        _materialBodyDefault = _renderer.material;
    }

    void Update()
    {
        if (_initialized) return;
        Debug.LogWarning($"PLAYER [{_behaviorParameters.TeamId}][{playerId}] NOT INITIALIZED!");
        Initialize();
        
        //if (_initializedByController) return;
    }
    
    public override void Initialize()
    {
        _behaviorParameters = GetComponent<BehaviorParameters>();
        
        _controller = GetComponentInParent<BeachVolkerballCupController>();
        
        var pos = transform.localPosition;
        _restPos = pos;

        input = GetComponent<BeachVolkerballCupAgentInput>();
        _move = GetComponent<BeachVolkerballCupAgentMove>();

        colliderFront = transform.Find("ColliderFront").GetComponent<CapsuleCollider>();
        colliderShieldGo = transform.Find("ColliderShield").gameObject;
        _rigidBody = GetComponent<Rigidbody>();

        if (projectileTransform == null)
        {
            projectileTransform = transform.Find("ProjectileDirectionZ").transform;
        }
        
        _initialized = true;
    }

    public void InitializeByController(int teamIndex, int playerIndex)
    {
        _behaviorParameters.TeamId = teamIndex;
        playerId = playerIndex;

        //_initializedByController = true;
    }
    
    public void Reset()
    {
        Initialize();
        
        colliderFront.isTrigger = false;
        colliderShieldGo.SetActive(false);
        
        _rigidBody.velocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;
        
        var tr = transform;
        var randCirc = Random.insideUnitCircle * _controller.agentsSpawnRadius;
        tr.localPosition = new Vector3(randCirc.x, _restPos.y, randCirc.y);
        var euler = tr.eulerAngles;
        euler.Set(0f, Random.Range(0f, 360f), 0f);
        tr.localEulerAngles = euler;
        
        _renderer.material = _materialBodyDefault;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var tr = transform;
        var pos = tr.localPosition;
        
        sensor.AddObservation(transform.InverseTransformVector(_rigidBody.velocity));
        
        var posBall = _controller.ball.gameObject.transform.localPosition;
        sensor.AddObservation(Vector3.Distance(pos, posBall));
        sensor.AddObservation(_controller.ball.velocity);
        var angle = Vector3.SignedAngle(transform.forward, posBall - pos, Vector3.up) / 180f;
        if (_controller.carryInfo.team == _behaviorParameters.TeamId && _controller.carryInfo.player == playerId)
        {
            angle = 0f;
        }
        sensor.AddObservation(angle);
        
        sensor.AddObservation(gotHit);
        if (Math.Abs(gotHit - 1f) < TOLERANCE)
        {
            gotHit = 0f;
        }
        
        var carried = -1f;
        if (_controller.carryInfo.team > -1)
        {
            carried = 2f;
            if (_controller.carryInfo.team == _behaviorParameters.TeamId)
            {
                carried = 1f;
                if (_controller.carryInfo.player == playerId)
                {
                    carried = 0f;
                }
            }
        }
        sensor.AddObservation(carried);
        var thrown = -1f;
        if (_controller.throwInfo.team > -1)
        {
            thrown = 2f;
            if (_controller.throwInfo.team == _behaviorParameters.TeamId)
            {
                thrown = 1f;
                if (_controller.throwInfo.player == playerId)
                {
                    thrown = 0f;
                }
            }
        }
        sensor.AddObservation(thrown);
        sensor.AddObservation(_controller.playersRemaining[_behaviorParameters.TeamId]);
        sensor.AddObservation(_controller.playersRemaining[1 - _behaviorParameters.TeamId]);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;

        _inputV = continuousActions[0];
        _inputH = continuousActions[1];
        _inputAxisX = continuousActions[2];
        _inputThrowStrength = continuousActions[3];

        // Rotation
        _move.Turn(_inputAxisX);
        
        // XZ movement
        var moveDir = transform.TransformDirection(new Vector3(_inputH, 0f, _inputV));
        _move.Run(moveDir);
        
        if (_controller.carryInfo.team == _behaviorParameters.TeamId && _controller.carryInfo.player == playerId && _inputThrowStrength > 0f)
        {
            ThrowBall();
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (_controller.humanTeamID != _behaviorParameters.TeamId || _controller.humanPlayerID != playerId)
            return;

        var contActionsOut = actionsOut.ContinuousActions;
        contActionsOut[0] = input.moveInput.y;
        contActionsOut[1] = input.moveInput.x;
        contActionsOut[2] = input.rotateInput;
        contActionsOut[3] = input.throwBall;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("ball") && _controller.carryInfo.team == -1)
        {
            ContactPoint contact = collision.GetContact(0);

            if (contact.thisCollider.CompareTag($"agentFront{_behaviorParameters.TeamId}"))
            {
                colliderFront.isTrigger = true;
                _controller.ball.colliderSphere.isTrigger = true;
                colliderShieldGo.SetActive(true);
                _controller.CaughtBall(_behaviorParameters.TeamId, playerId);
                _controller.ball.SetMaterialCarry();
            }
            else
            {
                _controller.ball.SetMaterialDefault();
                
                if (_controller.throwInfo.team > -1)
                {
                    if (_controller.throwInfo.team != _behaviorParameters.TeamId)
                    {
                        gotHit = 2f;
                        _controller.HitByOpponent(_behaviorParameters.TeamId, playerId);
                    }
                    else if (_controller.throwInfo.player != playerId)
                    {
                        gotHit = 1f;
                        _controller.HitByTeamPlayer(_behaviorParameters.TeamId, playerId);
                    }
                }
            }
        }
        /*else if (collision.transform.CompareTag("barrier"))
        {
            Debug.Log($"ENV [{_controller.envNumber}] Agent {playerID} of team {_behaviorParameters.TeamId} hit barrier.");
        }*/
    }

    private void ThrowBall()
    {
        colliderFront.isTrigger = false;
        _controller.ball.colliderSphere.isTrigger = false;
        colliderShieldGo.SetActive(false);
        _controller.ThrowBall(transform, projectileTransform, _inputThrowStrength);
        _controller.ball.SetMaterialThrow();
        _renderer.material = _materialBodyDefault;
    }
}
