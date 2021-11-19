using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BeachVolkerballCupBall : MonoBehaviour
{
    private BeachVolkerballCupController _controller;
    
    private Vector3 _restPos;
    //private Quaternion _restRot;
    private Vector3 _posDelta;

    [Header("COLLIDER")]
    [HideInInspector]
    public Collider colliderSphere;
    
    [Header("RIGID BODY")]
    [HideInInspector]
    public Rigidbody rigidBody;
    public Vector2 initialVelocityStrength = new Vector2(0.75f, 7.5f);
    public Vector2 initialAngularVelocityStrength = new Vector2(1f, 2.5f);
    private float _timer = 0f;
    //[HideInInspector]
    public bool inactive = false;

    [Header("DEBUG")]
    public Material materialCarry;
    public Material materialThrow;
    private Material _materialDefault;

    void Awake()
    {
        colliderSphere = GetComponent<SphereCollider>();
        rigidBody = GetComponent<Rigidbody>();
        _controller = GetComponentInParent<BeachVolkerballCupController>();
        
        SetRestPosition(transform);

        _materialDefault = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        if (rigidBody.velocity.magnitude < _controller.ballMagnitudeThreshold)
        {
            _timer += Time.deltaTime;
        }
        if (_timer > _controller.ballInactiveTimeoutInSec && _controller.carryInfo.team <= -1)
        {
            inactive = true;
            _controller.BallInactive();
        }
        if (_timer > _controller.ballCarryTimeoutInSec && _controller.carryInfo.team > -1)
        {
            inactive = true;
            _controller.BallCarriedTooLong();
        }
        
        if (_controller.throwInfo.team > -1 && Time.time - _controller.throwInfo.timeStamp >= _controller.ballThrowTimeoutInSec)
        {
            _controller.throwInfo.Reset();
        }
    }

    private void SetRestPosition(Transform tr)
    {
        _restPos = tr.localPosition;
        //_restRot = tr.localRotation;
    }
    
    public void Reset()
    {
        var tr = transform;
        var randCirc = Random.insideUnitCircle * _controller.ballSpawnRadius;
        tr.localPosition = new Vector3(randCirc.x, _restPos.y, randCirc.y);
        tr.localRotation = Random.rotation;
        SetRestPosition(tr);

        colliderSphere.isTrigger = false;
        InitRb();

        SetMaterialDefault();
    }
    
    public void InitRb()
    {
        inactive = false;
        _timer = 0f;

        //rigidBody.velocity = Vector3.zero;
        rigidBody.velocity = Random.Range(initialVelocityStrength.x, initialVelocityStrength.y) * Random.onUnitSphere;
        //rigidBody.angularVelocity = Vector3.zero;
        rigidBody.angularVelocity = Random.Range(initialAngularVelocityStrength.x, initialAngularVelocityStrength.y) * Random.onUnitSphere;
        //rigidBody.AddForce(projectileTransform.forward * projectileForce, projectileForceMode);
        Activate();
    }

    public void Hold(Transform playerTransform)
    {
        rigidBody.isKinematic = true;
        var tr = transform;
        _posDelta = tr.localPosition - playerTransform.localPosition;
        //isHeld = true;
    }
    
    public void RotateAroundPlayer(Transform playerTransform, float angleY)
    {
        transform.RotateAround(playerTransform.position, Vector3.up, angleY);
    }

    public void Translate(Transform playerTransform, Vector3 delta)
    {
        transform.localPosition += delta;
    }

    public void Activate()
    {
        rigidBody.isKinematic = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_controller.throwInfo.team == -1) return;

        if ((collision.gameObject.CompareTag("barrier")) || 
            (collision.gameObject.CompareTag("floor") && Time.time - _controller.throwInfo.timeStamp >= _controller.ballThrowTimeoutInSec))
        {
            //Debug.Log([{_controller.envNumber}] Time.time);
            _controller.throwInfo.Reset();
                
            //_controller.ball.SetMaterialDefault();
        }
    }

    public void SetMaterialDefault()
    {
        GetComponent<Renderer>().material = _materialDefault;
    }
    
    public void SetMaterialCarry()
    {
        GetComponent<Renderer>().material = materialCarry;
    }
    
    public void SetMaterialThrow()
    {
        GetComponent<Renderer>().material = materialThrow;
    }
}
