using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    #region Private Variables

    [SerializeField] private Transform player;

    #endregion

    #region Unity Functions

    void Update()
    {
        transform.position = player.transform.position;
    }

    #endregion
}