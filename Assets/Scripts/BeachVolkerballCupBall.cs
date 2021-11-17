using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BeachVolkerballCupBall : MonoBehaviour
{
    private BeachVolkerballCupController _controller;
    
    private Vector3 _restPos;
    //private Quaternion _restRot;
    private Vector3 _posDelta;
    
    [Header("RIGID BODY")]
    [HideInInspector]
    public Rigidbody rigidBody;
    public Vector2 initialVelocityStrength = new Vector2(0.75f, 7.5f);
    public Vector2 initialAngularVelocityStrength = new Vector2(1f, 2.5f);
    private float timer = 0;
    //[HideInInspector]
    public bool inactive = false;

    [Header("DEBUG")]
    public Material materialCarry;
    public Material materialThrow;
    private Material materialDefault;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        _controller = GetComponentInParent<BeachVolkerballCupController>();
        
        SetRestPosition(transform);

        materialDefault = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        if (rigidBody.velocity.magnitude < _controller.ballMagnitudeThreshold)
        {
            timer += Time.deltaTime;
        }
        if (timer > _controller.ballActiveTimeoutInSec)
        {
            inactive = true;
            _controller.BallInactive();
        }
        
        /*if (Time.time - _controller.throwInfo.timeStamp >= _controller.ballThrowTimeout)
        {
            _controller.ball.SetMaterialDefault();
        }*/
    }

    private void SetRestPosition(Transform tr)
    {
        _restPos = tr.position;
        //_restRot = tr.rotation;
    }
    
    public void Reset()
    {
        var tr = transform;
        var randCirc = Random.insideUnitCircle * _controller.ballSpawnRadius;
        tr.position = new Vector3(randCirc.x, _restPos.y, randCirc.y);
        tr.rotation = Random.rotation;
        SetRestPosition(tr);

        InitRb();

        SetMaterialDefault();
    }
    
    public void InitRb()
    {
        inactive = false;
        timer = 0f;

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
        _posDelta = tr.position - playerTransform.position;
        //isHeld = true;
    }
    
    public void Rotate(Transform playerTransform, float angleY)
    {
        //Quaternion rotation = new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
        transform.RotateAround(playerTransform.position, Vector3.up, angleY);

        //Vector3 delta = transform.position - playerTransform.position;
        //Debug.Log($"{_posDelta} >>> {delta}");
    }

    public void Translate(Transform playerTransform, Vector3 delta)
    {
        transform.position += delta;
    }

    public void Activate()
    {
        rigidBody.isKinematic = false;
        //isHeld = false;
    }

    /*public void FixedUpdate()
    {
        
    }*/

    void OnCollisionEnter(Collision collision)
    {
        if (_controller.throwInfo.team > -1)
        {
            if ((collision.gameObject.CompareTag("barrier")) || 
                (collision.gameObject.CompareTag("floor") && Time.time - _controller.throwInfo.timeStamp >= _controller.ballThrowTimeout))
            {
                //Debug.Log(Time.time);
                _controller.throwInfo.Reset();
                
                //_controller.ball.SetMaterialDefault();
            }
        }
    }

    public void SetMaterialDefault()
    {
        GetComponent<Renderer>().material = materialDefault;
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
