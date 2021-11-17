using System.Collections.Generic;
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
    //public bool gotHit = false;
    
    [HideInInspector]
    public BeachVolkerballCupAgentInput input;
    private float _inputH;
    private float _inputV;
    private float _inputAxisX;
    private float _inputThrow;
    //private float _inputBoost;
    private BeachVolkerballCupAgentMove _move;
    
    [Header("TEAM")]
    //private int _teamID;
    public int playerID;

    /*private Material floorMaterial;
    [Header("MATERIALS")]
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private MeshRenderer floorMeshRenderer;*/

    //[Header("COLLIDERS / RIGID BODY")]
    //private Dictionary<string, Collider> _colliders;
    private Rigidbody _rigidBody;
    //private RigidbodyConstraints _constraints;
    
    [Header("BALL PROJECTILE")]
    public Transform projectileTransform;

    public override void Initialize()
    {
        _behaviorParameters = GetComponent<BehaviorParameters>();
        
        _controller = GetComponentInParent<BeachVolkerballCupController>();
        
        var tr = transform;
        _restPos = tr.position;
        _restRot = tr.rotation;
        
        input = GetComponent<BeachVolkerballCupAgentInput>();
        _move = GetComponent<BeachVolkerballCupAgentMove>();

        //_colliders = new Dictionary<string, Collider>
        //{
        //    {"body", transform.Find("Body").GetComponent<Collider>()},
        //    {"front", transform.Find("ColliderFront").GetComponent<Collider>()},
        //};
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
        playerID = playerIndex;

        //_initializedByController = true;
    }
    
    public void Reset()
    {
        Initialize();
        //Debug.Log($"{_initialized} - {_initializedByController}");
        
        //_rigidBody.constraints = _constraints;
        _rigidBody.velocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;
        
        var tr = transform;
        var randCirc = Random.insideUnitCircle * _controller.agentsSpawnRadius;
        /*Physics.ComputePenetration(
            thisCollider, transform.position, transform.rotation,
            collider, otherPosition, otherRotation,
            out direction, out distance);*/
        tr.position = new Vector3(randCirc.x, _restPos.y, randCirc.y);
        var euler = tr.eulerAngles;
        euler.Set(0f, Random.Range(0f, 360f), 0f);
        tr.eulerAngles = euler;
    }
    
    public override void OnEpisodeBegin()
    {
        hasBall = false;
        //gotHit = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var tr = transform;
        sensor.AddObservation(tr.localPosition);
        //sensor.AddObservation(tr.rotation.eulerAngles.y);

        var angle = Vector3.SignedAngle(transform.forward, _controller.ball.gameObject.transform.position - tr.position, Vector3.up) / 180f;
        sensor.AddObservation(angle);
        sensor.AddObservation(hasBall);
        
        sensor.AddObservation(_controller.ball.gameObject.transform.localPosition);
        //sensor.AddObservation(_controller.ball.gameObject.transform.InverseTransformVector(_controller.ball.rigidBody.velocity));
        sensor.AddObservation(_controller.ball.rigidBody.velocity.magnitude);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;
        
        _inputV = continuousActions[0];
        _inputH = continuousActions[1];
        _inputAxisX = continuousActions[2];
        _inputThrow = discreteActions[0];
        //_inputBoost = discreteActions[1];
        
        // Handle rotation.
        _move.Turn(_inputAxisX);
        
        // Handle XZ movement.
        var moveDir = transform.TransformDirection(new Vector3(_inputH, 0f, _inputV));
        _move.Run(moveDir);

        if (hasBall && _inputThrow > 0f)
        {
            ThrowBall();
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (_controller.humanTeamID != _behaviorParameters.TeamId || _controller.humanPlayerID != playerID)
            return;

        var contActionsOut = actionsOut.ContinuousActions;
        contActionsOut[0] = input.moveInput.y;
        contActionsOut[1] = input.moveInput.x;
        contActionsOut[2] = input.rotateInput;
        
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = input.CheckIfInputSinceLastFrame(ref input.throwPressed) ? 1 : 0;
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
            ContactPoint thisContact = collision.GetContact(0);

            //Debug.Log(thisContact.thisCollider.tag);
            //Debug.Log($"agentFront{_behaviorParameters.TeamId}");
            //Debug.Log(thisContact.thisCollider.CompareTag($"agentFront{_behaviorParameters.TeamId}"));
            if (thisContact.thisCollider.CompareTag($"agentFront{_behaviorParameters.TeamId}"))
            {
                _controller.ball.SetMaterialCarry();
                
                Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} caught ball.");
                hasBall = true;
                //_rigidBody.constraints = RigidbodyConstraints.FreezePosition;
                _controller.CaughtBall(_behaviorParameters.TeamId, playerID);
                AddReward(1f);
                //EndEpisode();
            }
            else
            {
                _controller.ball.SetMaterialDefault();
                
                if (_controller.throwInfo.team == -1)
                {
                    Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} touched ball.");
//                    AddReward(-0.01f);
                }
                else
                {
                    if (_controller.throwInfo.team != _behaviorParameters.TeamId)
                    {
                        Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} hit by team {_controller.throwInfo.team}.");
                        //AddReward(-1f);
                        _controller.HitByOpponent(_behaviorParameters.TeamId, playerID);
                    }
                    else if (_controller.throwInfo.player != playerID)
                    {
                        Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} hit by own team ({_controller.throwInfo.team}).");
                        //AddReward(-1f);
                        _controller.HitByTeamplayer(_behaviorParameters.TeamId);
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
        _controller.ball.SetMaterialThrow();
        
        Debug.Log($"Agent {playerID} of team {_behaviorParameters.TeamId} throws ball.");
//        AddReward(0.25f);
        hasBall = false;
        _controller.ThrowBall(transform, projectileTransform);
        //_rigidBody.constraints = _constraints;
    }
}
