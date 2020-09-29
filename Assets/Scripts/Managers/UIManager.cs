using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region Private Variables

    static private UIManager _instance;

    [SerializeField] private Slider _blastEnergySlider;

    #endregion

    #region Pubilc Properties

    /// <summary>
    /// Gets an instance of the UIManager
    /// </summary>
    static public UIManager Instance
    {
        get { return _instance; }
    }

    #endregion

    #region Unity Functions

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #endregion

    #region Supporting Functions

    public void UpdateBlastEnergyUI(float value)
    {
        if (_blastEnergySlider != null)
            _blastEnergySlider.value = value;
    }

    #endregion
}
