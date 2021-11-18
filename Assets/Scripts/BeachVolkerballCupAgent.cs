using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class BeachVolkerballCupAgent : Agent
{
    //private bool _initialized = false;
    //private bool _initializedByController = false;

    private BehaviorParameters _behaviorParameters;
    
    private BeachVolkerballCupController _controller;
    
    private Vector3 _restPos;
    private Quaternion _restRot;
    
    [Header("OBSERVATIONS")]
    //[SerializeField]
    //private Transform ball;
    public bool hasBall = false;
    public bool gotHit = false;
    
    [Header("OBSERVATIONS")]
    [HideInInspector]
    public BeachVolkerballCupAgentInput input;
    private float _inputH;
    private float _inputV;
    private float _inputAxisX;
    private float _inputThrowStrength;
    //private float _inputBoost;
    private BeachVolkerballCupAgentMove _move;
    
    [Header("TEAM")]
    //private int _teamId;
    public int playerId;

    /*private Material floorMaterial;
    [Header("MATERIALS")]
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private MeshRenderer floorMeshRenderer;*/

    //[Header("COLLIDERS / RIGID BODY")]
    //private Dictionary<string, Collider> _colliders;
    private Rigidbody _rigidBody;
    //private RigidbodyConstraints _constraints;
    
    [Header("COLLIDERS")]
    [HideInInspector]
    public Collider colliderFront;
    public Collider colliderBody;
    
    [Header("BALL PROJECTILE")]
    public Transform projectileTransform;

    [Header("DEBUG")]
    private Renderer _renderer;
    private Material _materialBodyDefault;
    public Material materialBodyCarry;

    public void Awake()
    {
        _renderer = transform.Find("Body").GetComponent<Renderer>();
        _materialBodyDefault = _renderer.material;
    }

    public override void Initialize()
    {
        _behaviorParameters = GetComponent<BehaviorParameters>();
        
        _controller = GetComponentInParent<BeachVolkerballCupController>();
        
        var tr = transform;
        _restPos = tr.localPosition;
        _restRot = tr.localRotation;
        
        input = GetComponent<BeachVolkerballCupAgentInput>();
        _move = GetComponent<BeachVolkerballCupAgentMove>();

        colliderFront = transform.Find("ColliderFront").GetComponent<CapsuleCollider>();
        colliderBody = transform.Find("Body").GetComponent<SphereCollider>();
        _rigidBody = GetComponent<Rigidbody>();
        //_constraints = _rigidBody.constraints;

        if (projectileTransform == null)
        {
            projectileTransform = transform.Find("ProjectileDirectionZ").transform;
        }
        
        //_initialized = true;
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
        //colliderBody.isTrigger = false;
        
        _rigidBody.velocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;
        
        var tr = transform;
        var randCirc = Random.insideUnitCircle * _controller.agentsSpawnRadius;
        /*Physics.ComputePenetration(
            thisCollider, transform.localPosition, transform.rotation,
            collider, otherPosition, otherRotation,
            out direction, out distance);*/
        tr.localPosition = new Vector3(randCirc.x, _restPos.y, randCirc.y);
        var euler = tr.eulerAngles;
        euler.Set(0f, Random.Range(0f, 360f), 0f);
        tr.localEulerAngles = euler;
        
        _renderer.material = _materialBodyDefault;
    }
    
    public override void OnEpisodeBegin()
    {
        hasBall = false;
        gotHit = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var tr = transform;
        var pos = tr.localPosition;
        
        //sensor.AddObservation(tr.localPosition);

        var posBall = _controller.ball.gameObject.transform.localPosition;
        
        //sensor.AddObservation(_controller.ball.gameObject.transform.localPosition);
        //sensor.AddObservation(posBall.y);
        sensor.AddObservation(Vector3.Distance(pos, posBall));
        //sensor.AddObservation(_controller.ball.gameObject.transform.InverseTransformVector(_controller.ball.rigidBody.velocity));
        sensor.AddObservation(_controller.ball.rigidBody.velocity.magnitude);

        var angle = Vector3.SignedAngle(transform.forward, posBall - pos, Vector3.up) / 180f;
        sensor.AddObservation(angle);
        
        sensor.AddObservation(hasBall);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;
        //var discreteActions = actionBuffers.DiscreteActions;
        
        _inputV = continuousActions[0];
        _inputH = continuousActions[1];
        _inputAxisX = continuousActions[2];
        _inputThrowStrength = continuousActions[3];
        //_inputBoost = discreteActions[0];
        
        // Handle rotation.
        _move.Turn(_inputAxisX);
        
        // Handle XZ movement.
        var moveDir = transform.TransformDirection(new Vector3(_inputH, 0f, _inputV));
        _move.Run(moveDir);
        
        if (hasBall && _inputThrowStrength > 0f)
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
        
        //var discreteActionsOut = actionsOut.DiscreteActions;
        //discreteActionsOut[0] = input.CheckIfInputSinceLastFrame(ref input.throwPressed) ? 1 : 0;
        //discreteActionsOut[1] = input.CheckIfInputSinceLastFrame(ref input.boostPressed) ? 1 : 0;
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

            //Debug.Log($"{contact.thisCollider.tag} - ");
            //Debug.Log($"agentFront{_behaviorParameters.TeamId}");
            //Debug.Log(thisContact.thisCollider.CompareTag($"agentFront{_behaviorParameters.TeamId}"));
            if (contact.thisCollider.CompareTag($"agentFront{_behaviorParameters.TeamId}"))
            {
                Debug.Log($"Agent {playerId} of team {_behaviorParameters.TeamId} caught ball.");
                colliderFront.isTrigger = true;
                _controller.ball.colliderSphere.isTrigger = true;
                hasBall = true;
                _controller.CaughtBall(_behaviorParameters.TeamId, playerId);
                AddReward(1f);
                _controller.ball.SetMaterialCarry();
                //_renderer.material = materialBodyCarry;
            }
            else
            {
                _controller.ball.SetMaterialDefault();
                
                //if (_controller.throwInfo.team == -1)
                //{
                //    Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} touched ball.");
                //}
                if (_controller.throwInfo.team > -1)
                {
                    if (_controller.throwInfo.team != _behaviorParameters.TeamId)
                    {
                        //Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} hit by other team {_controller.throwInfo.team}.");
                        _controller.HitByOpponent(_behaviorParameters.TeamId, playerId);
                    }
                    else if (_controller.throwInfo.player != playerId)
                    {
                        //Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} hit by own team ({_controller.throwInfo.team}).");
                        _controller.HitByTeamplayer(_behaviorParameters.TeamId, playerId);
                    }
                }
            }
        }
        /*else if (collision.transform.CompareTag("barrier"))
        {
            Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} hit barrier.");
        }*/
    }

    private void ThrowBall()
    {
        Debug.Log($"Agent {playerId} of team {_behaviorParameters.TeamId} throws ball.");
        colliderFront.isTrigger = false;
        _controller.ball.colliderSphere.isTrigger = false;
        hasBall = false;
        _controller.ThrowBall(transform, projectileTransform, _inputThrowStrength);
        _controller.ball.SetMaterialThrow();
        _renderer.material = _materialBodyDefault;
    }
}
