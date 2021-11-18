using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Unity.MLAgents;

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
        public Vector3 eulerAngles;
        [HideInInspector]
        public Vector3 position;
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
        }
    }

    public enum SceneMode
    {
        Training,
        Game
    }
    public SceneMode sceneMode = SceneMode.Training;
    //[Tooltip("Across ALL Environments")]
    public int maxEnvironmentSteps = 1750;
    public int resetCounter = 0;
    
    [Header("HUMAN PLAYER")]
    [Range(0, 1)]
    public int humanTeamID = 0;
    [Range(0, 1)]
    public int humanPlayerID = 0;
    
    [Header("PLAYERS")]
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
    public float ballThrowTimeout = 2f;
    [Range(0f, 1f)]
    public float ballMagnitudeThreshold = 0.005f;
    [Range(0f, 100f)]
    public float ballActiveTimeoutInSec = 25f;
    
    [Header("BALL PROJECTILE")]
    [Range(90f, 90f)]
    public float projectileAngle = -5.5f;
    [Range(0f, 1000f)]
    public float projectileForce = 44f;
    public ForceMode projectileForceMode = ForceMode.VelocityChange;

    void Awake()
    {
        _agentInfos = new List<List<AgentInfo>>
        {
            agentsListTeam0,
            agentsListTeam1
        };

        Initialize();
        
    carryInfo = new CarryInfo();
    throwInfo = new ThrowInfo();
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
            Vector3 eulerDelta = eulerAngles - _agentInfos[carryInfo.team][carryInfo.player].eulerAngles;
            if (eulerDelta.y != 0f)
            {
                ball.Rotate(_agentInfos[carryInfo.team][carryInfo.player].agentGO.transform, eulerDelta.y);
            }
            _agentInfos[carryInfo.team][carryInfo.player].eulerAngles = eulerAngles;
            
            Vector3 position = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.position;
            Vector3 posDelta = position - _agentInfos[carryInfo.team][carryInfo.player].position;
            //Debug.Log($"{posDelta.sqrMagnitude} - {posDelta.magnitude}");
            if (posDelta.sqrMagnitude > 0f)
            {
                ball.Translate(_agentInfos[carryInfo.team][carryInfo.player].agentGO.transform, posDelta);
            }
            _agentInfos[carryInfo.team][carryInfo.player].position = position;
        }
    } 
    
    public void CaughtBall(int teamID, int playerID)
    {
        Debug.Log($"Team {teamID} caught the ball, team {1 - teamID} didn't.");
        _simpleMultiAgentGroups[teamID].AddGroupReward(1f);
        _simpleMultiAgentGroups[1 - teamID].AddGroupReward(-1f);

        carryInfo.Set(teamID, playerID, Time.time);
        ball.Hold(_agentInfos[carryInfo.team][carryInfo.player].agentGO.transform);
        
        _agentInfos[carryInfo.team][carryInfo.player].eulerAngles = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.rotation.eulerAngles;
        _agentInfos[carryInfo.team][carryInfo.player].position = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.position;
    }

    public void ThrowBall(Transform playerTransform, Transform projectileTransform)
    {
        throwInfo.Set(carryInfo, Time.time);
        
        ball.rigidBody.velocity = Vector3.zero;
        ball.rigidBody.angularVelocity = Vector3.zero;
        ball.rigidBody.isKinematic = false;
        ball.rigidBody.AddForce(projectileTransform.forward * projectileForce, projectileForceMode);
        
        Debug.Log($"Team {throwInfo.team} throws ball.");
//        _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(0.75f);

        carryInfo.Reset();
    }

    public void HitByOpponent(int teamID, int playerID)
    {
        Debug.Log($"Team {teamID} hit by team {throwInfo.team}.");
//        _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(1f);
//        _agentInfos[throwInfo.team][throwInfo.player].agent.AddReward(1f);
//        _simpleMultiAgentGroups[teamID].AddGroupReward(-1f);
        
        _agentInfos[teamID][playerID].agent.gameObject.SetActive(false);

        playersRemaining[teamID] -= 1;
        //Debug.Log($"Team {teamID}: {playersRemaining[teamID]}");
        if (playersRemaining[teamID] <= 0)
        {
            Debug.Log($"Team {throwInfo.team} wins!");
            _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(2f);
            _simpleMultiAgentGroups[teamID].AddGroupReward(-1f);
            
            foreach (var group in _simpleMultiAgentGroups)
            {
                group.EndGroupEpisode();
            }
            ResetScene();
        }
    }
    
    public void HitByTeamplayer(int teamID)
    {
        Debug.Log($"Team {teamID} hit by own team.");
        _simpleMultiAgentGroups[teamID].AddGroupReward(-1f);
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
