using UnityEngine;
using System.Collections;
using System.Text;

public class GoActiver : MonoBehaviour {
    
    public int Code
    {
        get;
        set;
    }
    
    [SerializeField, SetProperty("Active")]
    private bool active;
    public bool Active
    {
        get
        {
            return active;
        }

        private set
        {
            active = value;
            gameObject.SetActive(value);
            RemoteGameObjectControl.RemoteActive(gameObject, value);
        }
    }

    void Start()
    {
        active = gameObject.activeSelf;
    }

}
