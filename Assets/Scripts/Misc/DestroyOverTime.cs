using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
    #region Private Variables

    [SerializeField] private float _destroyAfter = 0;

    #endregion

    #region Unity Functions

    // Start is called before the first frame update
    void Start()
    {
        Destroy(this.gameObject, _destroyAfter);
    }

    #endregion
}
