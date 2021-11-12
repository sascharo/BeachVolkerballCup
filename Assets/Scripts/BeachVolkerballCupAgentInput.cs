using UnityEngine;

public class BeachVolkerballCupAgentInput : MonoBehaviour
{
    public bool disableInput;
    
    private BeachVolkerballCupInputActions _inputActions;
    private BeachVolkerballCupInputActions.PlayerActions _actionMap;

    public Vector2 moveInput;
    public float rotateInput;
    public bool throwPressed;
    //public bool boostPressed;
    
    void Awake()
    {
        _inputActions = new BeachVolkerballCupInputActions();
        _actionMap = _inputActions.Player;
    }

    void OnEnable()
    {
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }
    
    public bool CheckIfInputSinceLastFrame(ref bool input)
    {
        if (input)
        {
            input = false;
            return true;
        }
        
        return false;
    }

    private void Start()
    {
        //throw new NotImplementedException();
    }

    private void Update()
    {
        if (disableInput)
        {
            return;
        }
        
        moveInput = _actionMap.Run.ReadValue<Vector2>();
        //Debug.Log(moveInput);
        rotateInput = _actionMap.Turn.ReadValue<float>();
        
        if (_actionMap.Throw.triggered)
        {
            throwPressed = true;
        }
        
        //if (_actionMap.Dash.triggered)
        //{
        //    dashPressed = true;
        //}
    }
}
