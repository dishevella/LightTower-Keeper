using UnityEngine;

public sealed class PlayerInputAdapter
{
    public Vector2 ReadLookAxes()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    public Vector2 ReadMovementAxes()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    public bool IsHeld(KeyCode key)
    {
        return Input.GetKey(key);
    }

    public bool WasPressedThisFrame(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }
}
