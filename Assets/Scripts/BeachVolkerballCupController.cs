using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Unity.MLAgents;
using Unity.VisualScripting;
using Random = UnityEngine.Random;

public class BeachVolkerballCupController : MonoBehaviour
{
    //private bool _initialized = false;
    
    [Serializable]
    public class AgentInfo
    {
        [HideInInspector]
        public GameObject agentGO;
        public BeachVolkerballCupAgent agent;
        //public bool hasBall;
        //public bool gotHit;
        [HideInInspector]
        public Vector3 carryEulerAngles;
        [HideInInspector]
        public Vector3 carryPosition;
    }

    [Serializable]
    public class CarryInfo
    {
        [Range(-1, 3)]
        public int team = -1;
        [Range(-1, 3)]
        public int player = -1;
        public float timeStamp = 0f;

        public void Set(int te, int pl, float ts)
        {
            team = te;
            player = pl;
            timeStamp = ts;
        }
        
        public void Reset()
        {
            team = -1;
            player = -1;
            timeStamp = 0f;
        }
    }
    
    [Serializable]
    public class ThrowInfo
    {
        [Range(-1, 3)]
        public int team = -1;
        [Range(-1, 3)]
        public int player = -1;
        public float timeStamp = 0f;

        private BeachVolkerballCupBall _ball;

        public ThrowInfo(BeachVolkerballCupBall ball)
        {
            _ball = ball;
        }
        
        public void Set(CarryInfo ci, float ts)
        {
            team = ci.team;
            player = ci.player;
            timeStamp = ts;
        }
        
        public void Reset()
        {
            team = -1;
            player = -1;
            timeStamp = 0f;
            
            _ball.SetMaterialDefault();
        }
    }
    
    [Tooltip("PER Environment")]
    public int maxEnvironmentSteps = 5000;
    //public int numberEnvironments = 1;
    //[HideInInspector]
    public int resetCounter = 0;
    
    [Header("HUMAN PLAYER")]
    [Range(0, 1)]
    public int humanTeamID = 0;
    [Range(0, 1)]
    public int humanPlayerID = 0;
    
    [Header("PLAYERS / AGENTS")]
    [Range(0f, 100f)]
    public float agentsSpawnRadius = 9f;
    [Tooltip("<SimpleMultiAgentGroup>")]
    private List<SimpleMultiAgentGroup> _simpleMultiAgentGroups;
    [Tooltip("<AgentInfo>")]
    public List<AgentInfo> agentsListTeam0;
    [Tooltip("<AgentInfo>")]
    public List<AgentInfo> agentsListTeam1;
    private List<List<AgentInfo>> _agentInfos;
    public List<int> playersRemaining = new List<int>();
    [Tooltip("Min throw / hit radius")]
    [Range(0f, 100f)]
    public float minThrowDistance = 5f;
    
    [Header("BALL")]
    [Range(0f, 100f)]
    public float ballSpawnRadius = 9f;
    [HideInInspector]
    public GameObject ballGO;
    public BeachVolkerballCupBall ball;
    //[HideInInspector]
    public CarryInfo carryInfo;
    //[HideInInspector]
    public ThrowInfo throwInfo;
    [Range(0f, 100f)]
    public float ballThrowTimeoutInSec = 2f/3f;
    [Range(0f, 1f)]
    public float ballMagnitudeThreshold = 0.005f;
    [Range(0f, 100f)]
    public float ballActiveTimeoutInSec = 25f;
    
    [Header("BALL PROJECTILE")]
    //[Range(90f, 90f)]
    //public float projectileAngle = -5.5f;
    [Range(0f, 1000f)]
    public float projectileForceScalar = 44f;
    [Range(0f, 1f)]
    public float projectileForceInputScalar = 1f;
    public ForceMode projectileForceMode = ForceMode.VelocityChange;
    public Vector2 projectileAngularVelocityStrength = new Vector2(0.001f, 10f);

    void Awake()
    {
        _agentInfos = new List<List<AgentInfo>>
        {
            agentsListTeam0,
            agentsListTeam1
        };

        Initialize();
        
        carryInfo = new CarryInfo();
        throwInfo = new ThrowInfo(ball);
    }

    private void Initialize()
    {
        ballGO = ball.gameObject;
        
        _simpleMultiAgentGroups = new List<SimpleMultiAgentGroup>();

        int tIndex = 0;
        foreach (var list in _agentInfos)
        {
            _simpleMultiAgentGroups.Add(new SimpleMultiAgentGroup());
            playersRemaining.Add(list.Count);
            
            int pIndex = 0;
            foreach (var item in list)
            {
                item.agent.gameObject.SetActive(true);
                item.agent.InitializeByController(tIndex, pIndex);
                item.agentGO = item.agent.gameObject;
                //item.hasBall = false;
                //item.gotHit = false;
                pIndex += 1;
                
                _simpleMultiAgentGroups[tIndex].RegisterAgent(item.agent);
            }
            tIndex += 1;
        }

        ball.InitRb();

        //_initialized = true;
    }

    void ResetScene()
    {
        resetCounter = 0;

        int tIndex = 0;
        foreach (var list in _agentInfos)
        {
            playersRemaining[tIndex] = list.Count;
            
            foreach (var item in list)
            {
                item.agent.gameObject.SetActive(true);
                item.agent.Reset();
            }
            
            tIndex += 1;
        }
        
        carryInfo.Reset();
        throwInfo.Reset();
        
        ball.Reset();
    }

    void FixedUpdate()
    {
        resetCounter += 1;
        
        if (resetCounter >= maxEnvironmentSteps && maxEnvironmentSteps > 0)
        {
            foreach (var group in _simpleMultiAgentGroups)
            {
                group.GroupEpisodeInterrupted();
            }
            ResetScene();
        }
    }
    
    void Update()
    {
        if (carryInfo.team >= 0)
        {
            Vector3 eulerAngles = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.rotation.eulerAngles;
            Vector3 eulerDelta = eulerAngles - _agentInfos[carryInfo.team][carryInfo.player].carryEulerAngles;
            if (eulerDelta.y != 0f)
            {
                ball.Rotate(_agentInfos[carryInfo.team][carryInfo.player].agentGO.transform, eulerDelta.y);
            }
            _agentInfos[carryInfo.team][carryInfo.player].carryEulerAngles = eulerAngles;
            
            Vector3 position = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.localPosition;
            Vector3 posDelta = position - _agentInfos[carryInfo.team][carryInfo.player].carryPosition;
            if (posDelta.sqrMagnitude > 0f)
            {
                ball.Translate(_agentInfos[carryInfo.team][carryInfo.player].agentGO.transform, posDelta);
            }
            _agentInfos[carryInfo.team][carryInfo.player].carryPosition = position;
        }
    } 
    
    public void CaughtBall(int teamID, int playerID)
    {
        //ball.Caught();
        
        //Debug.Log($"Team {teamID} caught the ball, team {1 - teamID} didn't.");
        _simpleMultiAgentGroups[teamID].AddGroupReward(1f);
        _simpleMultiAgentGroups[1 - teamID].AddGroupReward(-1f);

        carryInfo.Set(teamID, playerID, Time.time);
        ball.Hold(_agentInfos[carryInfo.team][carryInfo.player].agentGO.transform);
        
        _agentInfos[carryInfo.team][carryInfo.player].carryEulerAngles = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.localRotation.eulerAngles;
        _agentInfos[carryInfo.team][carryInfo.player].carryPosition = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.localPosition;
    }

    public void ThrowBall(Transform playerTransform, Transform projectileTransform, float throwStrength)
    {
        throwInfo.Set(carryInfo, Time.time);
        
        ball.rigidBody.velocity = Vector3.zero;
        //ball.rigidBody.angularVelocity = Vector3.zero;
        ball.rigidBody.isKinematic = false;
        ball.rigidBody.angularVelocity = Random.Range(projectileAngularVelocityStrength.x, projectileAngularVelocityStrength.y) * Random.onUnitSphere;
        ball.rigidBody.AddForce(projectileTransform.forward * projectileForceScalar * (throwStrength * projectileForceInputScalar), projectileForceMode);
        
        //Debug.Log($"Team {throwInfo.team} throws ball.");
        _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(0.5f);

        carryInfo.Reset();
    }

    public void HitByOpponent(int teamId, int playerId)
    {
        var distance = Vector3.Distance(_agentInfos[teamId][playerId].agentGO.transform.localPosition, _agentInfos[throwInfo.team][throwInfo.player].agentGO.transform.localPosition);

        if (distance > minThrowDistance)
        {
            Debug.Log($"Team {teamId} hit by team {throwInfo.team} at {distance} m.");
            _agentInfos[throwInfo.team][throwInfo.player].agent.AddReward(1f);

            _agentInfos[teamId][playerId].agent.gameObject.SetActive(false);
            carryInfo.Reset();
            ball.colliderSphere.isTrigger = false;

            playersRemaining[teamId] -= 1;
            //Debug.Log($"Team {teamID}: {playersRemaining[teamID]}");
            
            if (playersRemaining[teamId] > 0)
            {
                _simpleMultiAgentGroups[teamId].AddGroupReward(-1f);
                _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(1f);
            }
            else
            {
                Debug.Log($"Team {throwInfo.team} wins!");
                _simpleMultiAgentGroups[teamId].AddGroupReward(-2f);
                _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(2f);

                foreach (var group in _simpleMultiAgentGroups)
                {
                    group.EndGroupEpisode();
                }

                ResetScene();
            }
        }
        else
        {
            Debug.Log($"Team {throwInfo.team} hit team {teamId} too close at {distance} m.");
            _agentInfos[throwInfo.team][throwInfo.player].agent.AddReward(-1f);
        }
    }
    
    public void HitByTeamplayer(int teamId, int playerId)
    {
        Debug.Log($"Team {teamId} hit by own team.");
        _simpleMultiAgentGroups[teamId].AddGroupReward(-1f);
    }

    public void BallInactive()
    {
        int i = 0;
        foreach (var group in _simpleMultiAgentGroups)
        {
            _simpleMultiAgentGroups[i].AddGroupReward(-1f);
            group.GroupEpisodeInterrupted();
            i += 1;
        }
        ResetScene();
    }
}
