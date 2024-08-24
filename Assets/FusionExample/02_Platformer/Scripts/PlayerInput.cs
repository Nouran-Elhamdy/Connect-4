using UnityEngine;
using Fusion;

namespace Starter.Platformer
{

	/// <summary>
	/// Input structure sent over network to the server.
	/// </summary>
	public struct GameplayInput : INetworkInput
	{
		public Vector2 MoveDirection;
		public NetworkButtons Buttons;
	}

	/// <summary>
	/// PlayerInput handles accumulating player input from Unity and passes the accumulated input to Fusion.
	/// </summary>
	public sealed class PlayerInput : NetworkBehaviour
	{
		private GameplayInput _input;

		public override void Spawned()
		{
			if (HasInputAuthority == false)
				return;

			// Register to Fusion input poll callback
			var networkEvents = Runner.GetComponent<NetworkEvents>();
			networkEvents.OnInput.AddListener(OnInput);

		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (runner == null)
				return;

			var networkEvents = runner.GetComponent<NetworkEvents>();
			if (networkEvents != null)
			{
				networkEvents.OnInput.RemoveListener(OnInput);
			}
		}

		private void Update()
		{
			// Accumulate input from Keyboard/Mouse. Input accumulation is mandatory (at least for look rotation) as Update can be
			// called multiple times before next OnInput is called - common if rendering speed is faster than Fusion simulation.

			if (HasInputAuthority == false)
				return;

			_input.MoveDirection = Input.mousePosition;	

            _input.Buttons.Set(0, Input.anyKey);
		}

		// Fusion polls accumulated input. This callback can be executed multiple times in a row if there is a performance spike.
		private void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			networkInput.Set(_input);
		}
	}
}
