using System;
using System.Collections.Generic;
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
    }
    
    [Serializable]
    public class ThrowInfo
    {
        [Range(-1, 3)]
        public int team = -1;
        [Range(-1, 3)]
        public int player = -1;
        public float timeStamp = 0f;
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
    //private Rigidbody _ballRb;
    public CarryInfo carryInfo = new CarryInfo();
    public ThrowInfo throwInfo = new ThrowInfo();
    
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
        //Debug.Log("Reset Time.");
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
    
    public void ThrowBallTimeout()
    {
        throwInfo.team = -1;
        throwInfo.player = -1;
        throwInfo.timeStamp = 0f;
    }
    
    public void CaughtBall(int teamID, int playerID)
    {
        var teamOther = 1 - teamID;
        Debug.Log($"Team {teamID} caught the ball, team {teamOther} didn't.");
        _simpleMultiAgentGroups[teamID].AddGroupReward(2f);
        _simpleMultiAgentGroups[teamOther].AddGroupReward(-4f);

        carryInfo.team = teamID;
        carryInfo.player = playerID;
        carryInfo.timeStamp = Time.time;
        ball.Hold(_agentInfos[carryInfo.team][carryInfo.player].agentGO.transform);
        
        _agentInfos[carryInfo.team][carryInfo.player].eulerAngles = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.rotation.eulerAngles;
        _agentInfos[carryInfo.team][carryInfo.player].position = _agentInfos[carryInfo.team][carryInfo.player].agentGO.transform.position;
    }

    public void ThrowBall(Transform playerTransform, Transform projectileTransform)
    {
        throwInfo.team = carryInfo.team;
        throwInfo.player = carryInfo.player;
        throwInfo.timeStamp = Time.time;
        
        ball.rigidBody.velocity = Vector3.zero;
        ball.rigidBody.angularVelocity = Vector3.zero;
        ball.rigidBody.isKinematic = false;
        ball.rigidBody.AddForce(projectileTransform.forward * projectileForce, projectileForceMode);
        
        Debug.Log($"Team {throwInfo.team} throws ball.");
        _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(0.75f);
        
        carryInfo.team = -1;
        carryInfo.player = -1;
        carryInfo.timeStamp = 0f;
    }

    public void HitByOpponent(int teamID, int playerID)
    {
        Debug.Log($"Team {teamID} hit by team {throwInfo.team}.");
        _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(1f);
        _agentInfos[throwInfo.team][throwInfo.player].agent.AddReward(1f);
        _simpleMultiAgentGroups[teamID].AddGroupReward(-1f);
        
        _agentInfos[teamID][playerID].agent.gameObject.SetActive(false);

        playersRemaining[teamID] -= 1;
        //Debug.Log($"Team {teamID}: {playersRemaining[teamID]}");
        if (playersRemaining[teamID] <= 0)
        {
            Debug.Log($"Team {throwInfo.team} wins!.");
            _simpleMultiAgentGroups[throwInfo.team].AddGroupReward(10f);
            _simpleMultiAgentGroups[teamID].AddGroupReward(-5f);
            
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
        _simpleMultiAgentGroups[teamID].AddGroupReward(-2.5f);
    }
}

