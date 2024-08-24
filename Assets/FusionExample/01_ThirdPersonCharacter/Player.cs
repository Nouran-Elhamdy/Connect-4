using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

namespace Starter.ThirdPersonCharacter
{
	/// <summary>
	/// Main player scrip - controls player movement and animations.
	/// </summary>
	public sealed class Player : NetworkBehaviour
	{
		[Header("References")]
		public SimpleKCC KCC;
		public PlayerInput Input;
		public Animator Animator;
		public Transform CameraPivot;
		public Transform CameraHandle;

		[Header("Movement Setup")]
		public float WalkSpeed = 2f;
		public float SprintSpeed = 5f;
		public float JumpImpulse = 10f;
		public float UpGravity = 25f;
		public float DownGravity = 40f;
		public float RotationSpeed = 8f;

		[Header("Movement Accelerations")]
		public float GroundAcceleration = 55f;
		public float GroundDeceleration = 25f;
		public float AirAcceleration = 25f;
		public float AirDeceleration = 1.3f;

		[Header("Sounds")]
        public AudioClip[] FootstepAudioClips;
		public AudioClip LandingAudioClip;
		[Range(0f, 1f)]
		public float FootstepAudioVolume = 0.5f;

		[Networked]
		private Vector3 _moveVelocity { get; set; }
		[Networked]
		private NetworkBool _isJumping { get; set; }
		[Networked]
		private NetworkButtons _previousButtons { get; set; }

		// Animation IDs
		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFreeFall;
		private int _animIDMotionSpeed;

		public override void FixedUpdateNetwork()
		{
			var input = GetInput<GameplayInput>().GetValueOrDefault();
			ProcessInput(input, _previousButtons);

			if (KCC.IsGrounded)
			{
				// Stop jumping
				_isJumping = false;
			}

			// Save current button input as previous.
			// Previous buttons need to be networked to detect correctly pressed/released events.
			_previousButtons = input.Buttons;
		}

		public override void Render()
		{
			Animator.SetFloat(_animIDSpeed, KCC.RealSpeed, 0.15f, Time.deltaTime);
			Animator.SetFloat(_animIDMotionSpeed, 1f);
			Animator.SetBool(_animIDJump, _isJumping);
			Animator.SetBool(_animIDGrounded, KCC.IsGrounded);
			Animator.SetBool(_animIDFreeFall, KCC.RealVelocity.y < -10f);
		}

		private void Awake()
		{
			AssignAnimationIDs();
		}

		private void LateUpdate()
		{
			// Only InputAuthority needs to update camera
			if (HasInputAuthority == false)
				return;

			// Update camera pivot and transfer properties from camera handle to Main Camera.
			CameraPivot.rotation = Quaternion.Euler(Input.LookRotation);
			Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
		}

		private void ProcessInput(GameplayInput input, NetworkButtons previousButtons)
		{
			float jumpImpulse = 0f;

			// Comparing current input buttons to previous input buttons - this prevents glitches when input is lost
			if (KCC.IsGrounded && input.Buttons.WasPressed(previousButtons, EInputButton.Jump))
			{
				// Set world space jump vector
				jumpImpulse = JumpImpulse;
				_isJumping = true;
			}

			// It feels better when the player falls quicker
			KCC.SetGravity(KCC.RealVelocity.y >= 0f ? UpGravity : DownGravity);

			float speed = input.Buttons.IsSet(EInputButton.Sprint) ? SprintSpeed : WalkSpeed;

			var lookRotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
			// Calculate correct move direction from input (rotated based on camera look)
			var moveDirection = lookRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
			var desiredMoveVelocity = moveDirection * speed;

			float acceleration;
			if (desiredMoveVelocity == Vector3.zero)
			{
				// No desired move velocity - we are stopping
				acceleration = KCC.IsGrounded ? GroundDeceleration : AirDeceleration;
			}
			else
			{
				// Rotate the character towards move direction over time
				var currentRotation = KCC.TransformRotation;
				var targetRotation = Quaternion.LookRotation(moveDirection);
				var nextRotation = Quaternion.Lerp(currentRotation, targetRotation, RotationSpeed * Runner.DeltaTime);

				KCC.SetLookRotation(nextRotation.eulerAngles);

				acceleration = KCC.IsGrounded ? GroundAcceleration : AirAcceleration;
			}

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);

			// Ensure consistent movement speed even on steep slope
			if (KCC.ProjectOnGround(_moveVelocity, out var projectedVector))
			{
				_moveVelocity = projectedVector;
			}

			KCC.Move(_moveVelocity, jumpImpulse);
		}

		private void AssignAnimationIDs()
		{
			_animIDSpeed = Animator.StringToHash("Speed");
			_animIDGrounded = Animator.StringToHash("Grounded");
			_animIDJump = Animator.StringToHash("Jump");
			_animIDFreeFall = Animator.StringToHash("FreeFall");
			_animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
		}

		// Animation event
		private void OnFootstep(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight < 0.5f)
				return;

			if (FootstepAudioClips.Length > 0)
			{
				var index = Random.Range(0, FootstepAudioClips.Length);
				AudioSource.PlayClipAtPoint(FootstepAudioClips[index], KCC.Position, FootstepAudioVolume);
			}
		}

		// Animation event
		private void OnLand(AnimationEvent animationEvent)
		{
			AudioSource.PlayClipAtPoint(LandingAudioClip, KCC.Position, FootstepAudioVolume);
		}
	}
}
