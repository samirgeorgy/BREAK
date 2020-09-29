using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region private variables

    //Assingables
    [SerializeField] private Transform _playerCam;
    [SerializeField] private Transform _orientation;
    [SerializeField] private Transform _handModel;

    //Other
    private Rigidbody _rb;

    //Rotation and look
    private float _xRotation;
    [SerializeField] private float _mouseSensitivity = 50f;
    private float _sensMultiplier = 1f;

    //Movement
    [SerializeField] private float _moveSpeed = 4500;
    [SerializeField] private float _maxSpeed = 20;
    [SerializeField] private bool _grounded;
    [SerializeField] private LayerMask _whatIsGround;

    [SerializeField] private float _counterMovement = 0.175f;
    private float _threshold = 0.01f;
    [SerializeField] private float _maxSlopeAngle = 35f;

    //Crouch & Slide
    private Vector3 _crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 _playerScale;
    [SerializeField] private float _slideForce = 400;
    [SerializeField] private float _slideCounterMovement = 0.2f;

    //Jumping
    private bool _readyToJump = true;
    private float _jumpCooldown = 0.25f;
    [SerializeField] private float _jumpForce = 550f;

    //Power Settings
    [SerializeField] private float _force = 700.0f;
    [SerializeField] private float _radius = 50.0f;
    [SerializeField] private Transform _energySpawnPoint;
    [SerializeField] private GameObject _energyEffect;
    [SerializeField] private LayerMask _whatIsBlastable;
    private bool _readyToBlast = true;
    private float _blastCoolDownTime = 60;
    private float _blastCountDown = 0;

    //Input
    private float _x, _y;
    private bool _jumping, _sprinting, _crouching;

    //Sliding
    private Vector3 _normalVector = Vector3.up;
    private Vector3 _wallNormalVector;

    //Animation
    [SerializeField] private Animator _anim;

    [SerializeField] private Transform _swordTrailPosition;
    [SerializeField] private GameObject _swordSwingTrail;

    private float _desiredX;
    private bool _cancellingGrounded;
    private bool _isBlocking = false;

    #endregion

    #region Unity Functions

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        _playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UIManager.Instance.UpdateBlastEnergyUI(_blastCoolDownTime);
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        MyInput();
        Look();

        if (Input.GetMouseButtonDown(0))
            Attack();

        if (Input.GetKeyDown(KeyCode.LeftShift) && _readyToBlast)
            ShootEnergyBlast();

        if (!_isBlocking)
            if (Input.GetMouseButtonDown(1))
                Block();
        if (_isBlocking)
            if (Input.GetMouseButtonUp(1))
                Unblock();

        if (!_readyToBlast)
        {
            _blastCountDown -= Time.deltaTime;
            UIManager.Instance.UpdateBlastEnergyUI(_blastCoolDownTime - _blastCountDown);

            if (_blastCountDown <= 0f)
                _readyToBlast = true;
        }
    }

    #endregion

    #region Supporting Functions

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    private void MyInput()
    {
        _x = Input.GetAxisRaw("Horizontal");
        _y = Input.GetAxisRaw("Vertical");
        _jumping = Input.GetButton("Jump");
        _crouching = Input.GetKey(KeyCode.LeftControl);

        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftControl))
            StopCrouch();
    }

    private void StartCrouch()
    {
        transform.localScale = _crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (_rb.velocity.magnitude > 0.5f)
        {
            if (_grounded)
            {
                _rb.AddForce(_orientation.transform.forward * _slideForce);
            }
        }
    }

    private void StopCrouch()
    {
        transform.localScale = _playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {
        //Extra gravity
        _rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(_x, _y, mag);

        //If holding jump && ready to jump, then jump
        if (_readyToJump && _jumping) Jump();

        //Set max speed
        float maxSpeed = this._maxSpeed;

        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (_crouching && _grounded && _readyToJump)
        {
            _rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (_x > 0 && xMag > maxSpeed) _x = 0;
        if (_x < 0 && xMag < -maxSpeed) _x = 0;
        if (_y > 0 && yMag > maxSpeed) _y = 0;
        if (_y < 0 && yMag < -maxSpeed) _y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!_grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        // Movement while sliding
        if (_grounded && _crouching) multiplierV = 0f;

        //Apply forces to move player
        _rb.AddForce(_orientation.transform.forward * _y * _moveSpeed * Time.deltaTime * multiplier * multiplierV);
        _rb.AddForce(_orientation.transform.right * _x * _moveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump()
    {
        if (_grounded && _readyToJump)
        {
            _readyToJump = false;

            //Add jump forces
            _rb.AddForce(Vector2.up * _jumpForce * 1.5f);
            _rb.AddForce(_normalVector * _jumpForce * 0.5f);

            //If jumping while falling, reset y velocity.
            Vector3 vel = _rb.velocity;
            if (_rb.velocity.y < 0.5f)
                _rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (_rb.velocity.y > 0)
                _rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), _jumpCooldown);
        }
    }

    private void ResetJump()
    {
        _readyToJump = true;
    }

    
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity * Time.fixedDeltaTime * _sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity * Time.fixedDeltaTime * _sensMultiplier;

        //Find current look rotation
        Vector3 rot = _playerCam.transform.localRotation.eulerAngles;
        _desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        //Perform the rotations
        _playerCam.transform.localRotation = Quaternion.Euler(_xRotation, _desiredX, 0);
        _orientation.transform.localRotation = Quaternion.Euler(0, _desiredX, 0);
        _handModel.transform.localRotation = Quaternion.Euler(0, _desiredX, 0);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!_grounded || _jumping) return;

        //Slow down sliding
        if (_crouching)
        {
            _rb.AddForce(_moveSpeed * Time.deltaTime * -_rb.velocity.normalized * _slideCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > _threshold && Math.Abs(x) < 0.05f || (mag.x < -_threshold && x > 0) || (mag.x > _threshold && x < 0))
        {
            _rb.AddForce(_moveSpeed * _orientation.transform.right * Time.deltaTime * -mag.x * _counterMovement);
        }
        if (Math.Abs(mag.y) > _threshold && Math.Abs(y) < 0.05f || (mag.y < -_threshold && y > 0) || (mag.y > _threshold && y < 0))
        {
            _rb.AddForce(_moveSpeed * _orientation.transform.forward * Time.deltaTime * -mag.y * _counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(_rb.velocity.x, 2) + Mathf.Pow(_rb.velocity.z, 2))) > _maxSpeed)
        {
            float fallspeed = _rb.velocity.y;
            Vector3 n = _rb.velocity.normalized * _maxSpeed;
            _rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = _orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(_rb.velocity.x, _rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = _rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < _maxSlopeAngle;
    }

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (_whatIsGround != (_whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                _grounded = true;
                _cancellingGrounded = false;
                _normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!_cancellingGrounded)
        {
            _cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void StopGrounded()
    {
        _grounded = false;
    }

    private void Attack()
    {
        int rand = (int)UnityEngine.Random.Range(1.0f, 2.9f);

        if (rand == 1)
            _anim.SetTrigger("Swing01");
        else if (rand == 2)
            _anim.SetTrigger("Swing02");

        Instantiate(_swordSwingTrail, _swordTrailPosition.position, Quaternion.identity);
    }

    private void ShootEnergyBlast()
    {
        _anim.SetTrigger("ShootEnergy");
        GameObject effect = Instantiate(_energyEffect, _energySpawnPoint.position, _energySpawnPoint.rotation);
        effect.transform.parent = _energySpawnPoint;

        Collider[] colliders = Physics.OverlapSphere(_energySpawnPoint.position, _radius, _whatIsBlastable);

        foreach (Collider nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponentInParent<Rigidbody>();

            if (rb != null)
                rb.AddExplosionForce(_force, _energySpawnPoint.position, _radius);
        }

        _blastCountDown = _blastCoolDownTime;
        UIManager.Instance.UpdateBlastEnergyUI(_blastCountDown - _blastCoolDownTime);
        _readyToBlast = false;
    }

    private void Block()
    {
        _isBlocking = true;
        _anim.SetBool("Block", _isBlocking);
    }

    private void Unblock()
    {
        _isBlocking = false;
        _anim.SetBool("Block", _isBlocking);
    }

    #endregion

}