using System;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

public class LocalInput : MonoBehaviour {
    [SerializeField] private double mouseSensitivity = 10;
    [SerializeField] private double moveSpeed = 10;
    [SerializeField] private Animator characterAnimator;
    
    private void OnEnable() {
        QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void PollInput(CallbackPollInput callback) {
        Quantum.Input i = new Quantum.Input();
        i.Jump = UnityEngine.Input.GetButton("Jump");
        i.Shoot = UnityEngine.Input.GetButton("Fire1");
    
        Vector2 inputDirection = Vector2.zero;

        inputDirection.x = UnityEngine.Input.GetAxis("Horizontal");
        inputDirection.y = UnityEngine.Input.GetAxis("Vertical");

        i.DirectionX = (short)(inputDirection.x * moveSpeed);
        i.DirectionY= (short)(inputDirection.y * moveSpeed);
        i.Rotation = (short)(UnityEngine.Input.GetAxis("Mouse X") * 100 * mouseSensitivity);

        callback.SetInput(i, DeterministicInputFlags.Repeatable);
    }
}
