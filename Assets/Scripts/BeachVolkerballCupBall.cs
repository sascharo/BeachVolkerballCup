using System;
using UnityEngine;
using UnityEngine.Animations;
using Random = UnityEngine.Random;

public class BeachVolkerballCupBall : MonoBehaviour
{
    private BeachVolkerballCupController _controller;
    
    private Vector3 _restPos;
    //private Quaternion _restRot;
    private Vector3 _posDelta;
    
    [HideInInspector]
    public Rigidbody rigidBody;

    public Vector2 initialAngularVelocityStrength = new Vector2(1f, 2.5f);

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        _controller = GetComponentInParent<BeachVolkerballCupController>();
        
        SetRestPosition(transform);
    }
    
    /*private void OnEnable()
    {
        
    }*/

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
    }
    
    public void InitRb()
    {
        //rigidBody.velocity = Vector3.zero;
        rigidBody.velocity = 0.1f * Random.onUnitSphere;
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
        if (_controller.throwInfo.team >= 0)
        {
            if (collision.gameObject.CompareTag("floor") && Time.time - _controller.throwInfo.timeStamp >= 1f)
            {
                //Debug.Log(Time.time);
                _controller.ThrowBallTimeout();
            }
        }
    }
}
