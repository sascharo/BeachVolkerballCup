using UnityEngine;

public class BeachVolkerballCupAgentInput : MonoBehaviour
{
    public bool disableInput;
    
    private BeachVolkerballCupInputActions _inputActions;
    private BeachVolkerballCupInputActions.PlayerActions _actionMap;

    public Vector2 moveInput;
    public float rotateInput;
    public float throwBall;

    void Awake()
    {
        _inputActions = new BeachVolkerballCupInputActions();
        _actionMap = _inputActions.Player;
    }

    void OnEnable()
    {
        _inputActions.Enable();
    }

    void OnDisable()
    {
        _inputActions.Disable();
    }
    
    public float InputCheckSinceLastFrame(ref float input)
    {
        if (input == 0f) return 0f;
        
        input = 1f;
        
        return 1f;
    }

    private void Update()
    {
        if (disableInput)
        {
            return;
        }
        
        moveInput = _actionMap.Run.ReadValue<Vector2>();
        rotateInput = _actionMap.Turn.ReadValue<float>();
        throwBall = _actionMap.Throw.ReadValue<float>();
    }
}
