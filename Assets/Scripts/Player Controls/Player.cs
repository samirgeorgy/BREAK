using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region Private Variables

    [SerializeField] private float _speed = 5.0f;
    [SerializeField] private float _jumpHeight = 10.0f;
    [SerializeField] private float _gravity = 1.0f;

    private Vector3 _direction;

    private CharacterController _controller;

    #endregion

    #region Unity Functions

    // Start is called before the first frame update
    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        PlayerMovement();
    }

    #endregion

    #region Supporting Functions

    private void PlayerMovement()
    {
        if (_controller.isGrounded)
        {
            float horizontalMovement = Input.GetAxis("Horizontal");
            float verticalMovement = Input.GetAxis("Vertical");

            _direction = (transform.right * horizontalMovement + transform.forward * verticalMovement) * _speed;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _direction.y += _jumpHeight;
            }
        }

        _direction.y -= _gravity * Time.deltaTime;
        _controller.Move(_direction * Time.deltaTime);
    }

    #endregion
}
