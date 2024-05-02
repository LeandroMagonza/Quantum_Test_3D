using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Quantum;
using Cinemachine;
using ExitGames.Client.Photon;

public class PlayerHandler : MonoBehaviour {
    [SerializeField] private EntityView entityView;
    [SerializeField] private GameObject followAnchor;
    [SerializeField] private Animator animator;
    
    private bool _isLocalPlayer = false;
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

    private bool lastJump = false;
    private bool lastShot = false;
    private static readonly int Shoot = Animator.StringToHash("Shoot");

    public void OnEntityInstantiated() {
        Debug.Log("Player character controller OnEntityInstantiated");

        QuantumGame game = QuantumRunner.Default.Game;
        Frame frame = game.Frames.Verified;
        
        if (frame.TryGet(entityView.EntityRef, out PlayerLink playerLink)) {
            if (game.PlayerIsLocal(playerLink.Player)) {
                _isLocalPlayer = true;
                CinemachineVirtualCamera virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
                virtualCamera.m_Follow = followAnchor.transform;
            }
        }
    }


    private void Update() {
        AnimateMovement();
    }

    private void AnimateMovement()
    {
        var game = QuantumRunner.Default.Game;
        var frame = _isLocalPlayer ? game.Frames.Predicted : game.Frames.Verified;
        var animationState = frame.Get<PlayerAnimationState>(entityView.EntityRef);
        
        //animator.speed = isMoving ? ANIMATION_SPEED : 1.0f; 
        animator.SetFloat(MoveX, animationState.IsWalkingY);
        animator.SetFloat(MoveY, animationState.IsWalkingX);
        animator.SetBool(Grounded,animationState.IsGrounded);
        if (animationState.IsJumping) {
            if (!lastJump) {
                Debug.Log("JUMp");
                animator.SetTrigger(Jump);
                lastJump = true;
            }
        }
        else {
            lastJump = false;
        }     
        
        if (animationState.IsShooting) {
            if (!lastShot) {
                Debug.Log("Shoot");
                animator.SetTrigger(Shoot);
                lastShot = true;
            }
        }
        else {
            lastShot = false;
        }
    }
    // private void UpdateAnimator(PlayerAnimationState animState) {
    //     animator.SetBool("IsWalking", animState.IsWalking);
    //     animator.SetBool("IsJumping", animState.IsJumping);
    //     // Configura otros estados de animación según sea necesario
    // }
  
}
// public struct PlayerAnimationState : IComponent {
//     public bool IsWalking;
//     public bool IsJumping;
//     // otros estados de animación...
// }

