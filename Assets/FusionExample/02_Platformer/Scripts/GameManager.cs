using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using System;

namespace Starter.Platformer
{
	/// <summary>
	/// Handles player connections (spawning of Player instances).
	/// </summary>
	public sealed class GameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
	{
		public int MinCoinsToWin = 10;
		public float GameOverTime = 4f;
		public Player PlayerPrefab;
		public float SpawnRadius = 3f;

		public Player LocalPlayer { get; private set; }

		[Networked]
		public PlayerRef Winner { get; private set; }
		[Networked]
		public TickTimer GameOverTimer { get; private set; }

		public List<Player> players = new(32);
        public void OnLineDetected(Player player)
        {
            if (HasStateAuthority == false)
                return;

            if (Winner != PlayerRef.None)
                return; // Someone was faster


            // Set this player as winner
            Winner = player.Object.InputAuthority;

            // Stop all players
            for (int i = 0; i < players.Count; i++)
            {
                players[i].IsFinished = true;
            }

            // Set some small timer to show the results. After this timer
            // expires all players are respawned in FixedUpdateNetwork
           // GameOverTimer = TickTimer.CreateFromSeconds(Runner, GameOverTime);
        }

        public override void FixedUpdateNetwork()
		{
			//bool resetGame = GameOverTimer.Expired(Runner);

			//for (int i = 0; i < _players.Count; i++)
			//{
			//	var player = _players[i];

			//	if (resetGame || player.KCC.Position.y < -15f)
			//	{
			//		player.Respawn(GetSpawnPosition(), resetGame);
			//	}
			//}

			//if (resetGame)
			//{
			//	Winner = PlayerRef.None;
			//	GameOverTimer = default;
			//}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			// Clear the reference because UI can try to access it even after despawn
			LocalPlayer = null;
		}

		public override void Render()
		{
			// Prepare LocalPlayer property that can be accessed from UI
			if (LocalPlayer == null || LocalPlayer.Object == null || LocalPlayer.Object.IsValid == false)
			{
				var playerObject = Runner.GetPlayerObject(Runner.LocalPlayer);
				LocalPlayer = playerObject != null ? playerObject.GetComponent<Player>() : null;
			}
		}

		public void PlayerJoined(PlayerRef playerRef)
		{
			if (HasStateAuthority == false)
				return;

			var player = Runner.Spawn(PlayerPrefab, Vector3.zero, Quaternion.identity, playerRef);
			Runner.SetPlayerObject(playerRef, player.Object);
			player.SlotIndex = Runner.ActivePlayers.Count() - 1;	
			// This list is state authority only,
			// so it is valid to have this list non-networked
			players.Add(player);

			if (Winner != PlayerRef.None)
			{
				// Game just finished, wait for next round
				player.IsFinished = true;
			}
			player.SetPosition();
		}

		public void PlayerLeft(PlayerRef playerRef)
		{
			if (HasStateAuthority == false)
				return;

			int index = players.FindIndex(t => t.Object.InputAuthority == playerRef);
			if (index >= 0)
			{
				Runner.Despawn(players[index].Object);
				players.RemoveAt(index);
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.DrawWireSphere(transform.position, SpawnRadius);
		}
	}
}
