using UnityEngine;

public class HeroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CapsuleCollider capsuleCollider;


    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float accelaration;
    [SerializeField] private float deceleration;
    [SerializeField] private float gravityForce;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpMaxHeight;
    [SerializeField] private float jumpDuration;
    [SerializeField] private float gravityMultiplier;

    private StateMachine stateMachine;
}
