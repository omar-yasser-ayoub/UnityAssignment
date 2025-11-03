using UnityEngine;
using UnityEngine.InputSystem;

public class LeverScript : MonoBehaviour
{
    [SerializeField] private GameObject fire; // Assign the fire GameObject in the Inspector

    void Start()
    {
        // Make sure fire is inactive at start
        if (fire != null)
        {
            fire.SetActive(false);
        }
    }

    void Update()
    {

    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Check for E key press using new Input System
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (fire != null)
                {
                    fire.SetActive(true);
                }
            }
        }
    }
}