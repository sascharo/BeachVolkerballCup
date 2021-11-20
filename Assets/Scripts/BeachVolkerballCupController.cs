using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Unity.MLAgents;
using UnityEditor;
using Random = UnityEngine.Random;

public class BeachVolkerballCupController : MonoBehaviour
{
    private bool _initialized = false;
    
    [Serializable]
    public class AgentInfo
    {
        [HideInInspector]
        public GameObject agentGo;
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

    [Header("ENVIRONMENT")]
    //[HideInInspector]
    public int envNumber;
    [Tooltip("PER Environment")]
    public int maxEnvSteps = 10000;
    //public int numberEnvironments = 1;
    //[HideInInspector]
    public int resetEnvStepsCounter = 0;
    private EnvironmentParameters _envParameters;
    private float _timeBonus = 1f;
    [Range(0f, 10f)]
    public float timeBonusScalar = 1f;
    
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
    public float minThrowDistance = 4f;
    
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
    [Tooltip("Rigid Body")]
    [Range(0f, 1f)]
    public float ballMagnitudeThreshold = 0.005f;
    [Tooltip("Player")]
    [Range(0f, 1f)]
    public float ballTranslateThreshold = 0.0075f;
    [Range(0f, 600f)]
    public float ballInactiveTimeoutInSec = 15f;
    [Range(0f, 100f)]
    public float ballCarryTimeoutInSec = 5f;
    
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
        envNumber = Int32.Parse(Regex.Match(transform.name, @"\d+").Value);

        Initialize();
    }

    private void Initialize()
    {
        _envParameters = Academy.Instance.EnvironmentParameters;
        _timeBonus = _envParameters.GetWithDefault("time_bonus_scale", 1f);
        
        ballGO = ball.gameObject;

        _agentInfos = new List<List<AgentInfo>>
        {
            agentsListTeam0,
            agentsListTeam1
        };
        _simpleMultiAgentGroups = new List<SimpleMultiAgentGroup>();

        var teamIdx = 0;
        foreach (var list in _agentInfos)
        {
            _simpleMultiAgentGroups.Add(new SimpleMultiAgentGroup());
            playersRemaining.Add(list.Count);
            
            var playerIdx = 0;
            foreach (var item in list)
            {
                item.agent.gameObject.SetActive(true);
                item.agent.InitializeByController(teamIdx, playerIdx);
                item.agentGo = item.agent.gameObject;
                playerIdx += 1;
                
                _simpleMultiAgentGroups[teamIdx].RegisterAgent(item.agent);
            }
            teamIdx += 1;
        }

        ball.InitRb();
        
        carryInfo = new CarryInfo();
        throwInfo = new ThrowInfo(ball);
        
        _initialized = true;
    }

    void ResetScene()
    {
        //Debug.Log($"{_simpleMultiAgentGroups[1].GetRegisteredAgents()}");
        //_simpleMultiAgentGroups[1].RegisterAgent(_agentInfos[1][0].agent);
        //Debug.Log($"{_simpleMultiAgentGroups[1].GetRegisteredAgents()}");
        
        resetEnvStepsCounter = 0;
        
        _timeBonus = _envParameters.GetWithDefault("time_bonus_scale", 1f);

        var teamIdx = 0;
        foreach (var agentInfos in _agentInfos)
        {
            //Debug.Log($"teamIdx = {teamIdx}");
            playersRemaining[teamIdx] = agentInfos.Count;
            
            foreach (var agentInfo in agentInfos)
            {
                //Debug.Log($"agent = {agentInfo.agentGo.name}");
                agentInfo.agent.gameObject.SetActive(true);
                agentInfo.agent.Reset();
                
                _simpleMultiAgentGroups[teamIdx].RegisterAgent(agentInfo.agent);
            }

            teamIdx += 1;
        }

        carryInfo.Reset();
        throwInfo.Reset();
        
        ball.Reset();
    }

    void FixedUpdate()
    {
        resetEnvStepsCounter += 1;
        
        if (resetEnvStepsCounter >= maxEnvSteps && maxEnvSteps > 0)
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
        if (!_initialized)
        {
            Debug.LogWarning("NOT INITIALIZED!");
            Initialize();
        }
        
        if (carryInfo.team >= 0)
        {
            Vector3 eulerAngles = _agentInfos[carryInfo.team][carryInfo.player].agentGo.transform.rotation.eulerAngles;
            Vector3 eulerDelta = eulerAngles - _agentInfos[carryInfo.team][carryInfo.player].carryEulerAngles;
            if (eulerDelta.y != 0f)
            {
                ball.RotateAroundPlayer(_agentInfos[carryInfo.team][carryInfo.player].agentGo.transform, eulerDelta.y);
            }
            _agentInfos[carryInfo.team][carryInfo.player].carryEulerAngles = eulerAngles;
            
            Vector3 position = _agentInfos[carryInfo.team][carryInfo.player].agentGo.transform.localPosition;
            Vector3 posDelta = position - _agentInfos[carryInfo.team][carryInfo.player].carryPosition;
            if (posDelta.sqrMagnitude > 0f)
            {
                ball.Translate(_agentInfos[carryInfo.team][carryInfo.player].agentGo.transform, posDelta);
            }
            _agentInfos[carryInfo.team][carryInfo.player].carryPosition = position;
        }
    } 
    
    public void CaughtBall(int teamId, int playerId)
    {
        //ball.Caught();
        
        //Debug.Log($"ENV [{envNumber}] Team {teamID} caught the ball, team {1 - teamID} didn't.");
        _simpleMultiAgentGroups[teamId].AddGroupReward(1f);
        _simpleMultiAgentGroups[1 - teamId].AddGroupReward(-1f);

        carryInfo.Set(teamId, playerId, Time.time);
        ball.Hold(_agentInfos[carryInfo.team][carryInfo.player].agentGo.transform);
        
        _agentInfos[carryInfo.team][carryInfo.player].carryEulerAngles = _agentInfos[carryInfo.team][carryInfo.player].agentGo.transform.localRotation.eulerAngles;
        _agentInfos[carryInfo.team][carryInfo.player].carryPosition = _agentInfos[carryInfo.team][carryInfo.player].agentGo.transform.localPosition;
    }

    public void ThrowBall(Transform playerTransform, Transform projectileTransform, float throwStrength)
    {
        throwInfo.Set(carryInfo, Time.time);
        
        ball.rigidBody.velocity = Vector3.zero;
        //ball.rigidBody.angularVelocity = Vector3.zero;
        ball.rigidBody.isKinematic = false;
        ball.rigidBody.angularVelocity = Random.Range(projectileAngularVelocityStrength.x, projectileAngularVelocityStrength.y) * Random.onUnitSphere;
        ball.rigidBody.AddForce(projectileTransform.forward * projectileForceScalar * (throwStrength * projectileForceInputScalar), projectileForceMode);

        var reward = true;
        foreach (var agentInfo in _agentInfos[1 - throwInfo.team])
        {
            var distance = (agentInfo.agentGo.transform.localPosition - _agentInfos[throwInfo.team][throwInfo.player].agentGo.transform.localPosition).magnitude;
            if (distance < minThrowDistance)
            {
                reward = false;
                break;
            }
        }
        if (reward)
        {
            Debug.Log($"ENV [{envNumber}] Agent {throwInfo.player} of team {throwInfo.team} threw ball above 'minThrowDistance'.");
            //Debug.Log($"ENV [{envNumber}] Team {throwInfo.team} throws ball.");
            _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(1f);
        }
        else
        {
            Debug.Log($"ENV [{envNumber}] Agent {throwInfo.player} of team {throwInfo.team} threw ball below 'minThrowDistance'.");
        }

        carryInfo.Reset();
    }

    public void HitByOpponent(int teamId, int playerId)
    {
        var distance = Vector3.Distance(_agentInfos[teamId][playerId].agentGo.transform.localPosition, _agentInfos[throwInfo.team][throwInfo.player].agentGo.transform.localPosition);
        
        if (distance >= minThrowDistance)
        {
            Debug.Log($"ENV [{envNumber}] Team {teamId} hit by team {throwInfo.team} at {distance} m.");
            _agentInfos[throwInfo.team][throwInfo.player].agent.AddReward(0.75f);

            _agentInfos[teamId][playerId].agent.gameObject.SetActive(false);
            carryInfo.Reset();
            ball.colliderSphere.isTrigger = false;

            playersRemaining[teamId] -= 1;
            //Debug.Log($"ENV [{envNumber}] Team {teamID}: {playersRemaining[teamID]}");
            
            if (playersRemaining[teamId] > 0)
            {
                _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(1f);
                _simpleMultiAgentGroups[teamId].AddGroupReward(-1f);
            }
            else
            {
                Debug.Log($"ENV [{envNumber}] Team {throwInfo.team} wins!");
                //_simpleMultiAgentGroups[teamId].AddGroupReward(2f - (_timeBonus * ((float)resetEnvStepsCounter / (float)maxEnvSteps)));
                _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(1f + timeBonusScalar - ( timeBonusScalar * ( (float)resetEnvStepsCounter / (float)maxEnvSteps ) ));
                _simpleMultiAgentGroups[teamId].AddGroupReward(-1f);
                
                foreach (var group in _simpleMultiAgentGroups)
                {
                    group.EndGroupEpisode();
                }
                ResetScene();
            }
        }
        else
        {
            Debug.Log($"ENV [{envNumber}] Team {throwInfo.team} hit team {teamId} too close at {distance} m.");
            _agentInfos[throwInfo.team][throwInfo.player].agent.AddReward(-0.5f);
            _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(-0.5f);
        }
    }
    
    public void HitByTeamplayer(int teamId, int playerId)
    {
        Debug.Log($"ENV [{envNumber}] Team {teamId} hit by own team.");
        _simpleMultiAgentGroups[teamId].AddGroupReward(-1f);
    }

    public void BallInactive()
    {
        Debug.Log($"ENV [{envNumber}] Ball inactive for too long.");
        
        var i = 0;
        foreach (var group in _simpleMultiAgentGroups)
        {
            //_simpleMultiAgentGroups[i].AddGroupReward(-1f);
            group.GroupEpisodeInterrupted();
            i += 1;
        }
        ResetScene();
    }
    
    public void BallCarriedTooLong()
    {
        Debug.Log($"ENV [{envNumber}] Team {carryInfo.team}, Player {carryInfo.player} holding ball too long [other team: {1 - carryInfo.team}].");
        
        _agentInfos[carryInfo.team][carryInfo.player].agent.AddReward(-0.5f);
        
        _simpleMultiAgentGroups[carryInfo.team].EndGroupEpisode();
        _simpleMultiAgentGroups[1 - carryInfo.team].GroupEpisodeInterrupted();
        //var i = 0;
        //foreach (var group in _simpleMultiAgentGroups)
        //{
        //    group.GroupEpisodeInterrupted();
        //    i += 1;
        //}
        ResetScene();
    }
}
