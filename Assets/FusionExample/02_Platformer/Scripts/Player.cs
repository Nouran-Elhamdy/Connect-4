using UnityEngine;
using Fusion;
using Random = UnityEngine.Random;
using System;
using System.Collections.Generic;
using System.Collections;
namespace Starter.Platformer
{
	public sealed class Player : NetworkBehaviour
	{
		[Header("References")]
		public PlayerInput Input;
        public PlayerAvatar playerAvatar;
		public UINameplate uiName;

		[Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
		public string Nickname { get; private set; }
        [Networked, OnChangedRender(nameof(OnAvatarIndexChanged))]
        public int AvatarIndex { get; private set; }
        [Networked]
        public int SlotIndex { get; set; }

        [Networked, HideInInspector]
		public NetworkBool IsFinished { get; set; }
		[Networked]
		private NetworkButtons _previousButtons { get; set; }

		public override void Spawned()
		{
			if (HasInputAuthority)
			{
				RPC_SetNickname(PlayerPrefs.GetString("PlayerName"));
                RPC_SetAvatar(Random.Range(0, playerAvatar.playerAvatars.Length));
            }
			if(!Runner.IsServer)
			{
				SetPosition();
			}
            OnNicknameChanged();
		}
        public void SetPosition(Transform parent = null)
        {
            parent = GridManager.Instance.playerPos[SlotIndex];
            transform.SetParent(parent, false);
        }
        public override void FixedUpdateNetwork()
		{
            var input = GetInput<GameplayInput>().GetValueOrDefault();
			ProcessInput(input, _previousButtons);

		}
        private void ProcessInput(GameplayInput input, NetworkButtons previousButtons)
		{
            if (SlotIndex == GridManager.Instance.currentPlayerTurn && !IsFinished && GridManager.Instance.isGameStarted)
            {
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(input.MoveDirection);

                float snappedX = Mathf.Round(worldPosition.x / 1) * 1;
                Vector3 snappedPosition = new Vector3(snappedX, 2.5f, 0f);

                GridManager.Instance.highlight.gameObject.SetActive(true);

                if (snappedX > 6)
                {
                    snappedX = 6;
                    GridManager.Instance.highlight.transform.position = new Vector3(snappedX, 2.5f, 0f);
                }
                else if (snappedX < 0)
                {
                    snappedX = 0;
                    GridManager.Instance.highlight.transform.position = new Vector3(snappedX, 2.5f, 0f);
                }
                else
                {
                    GridManager.Instance.highlight.transform.position = snappedPosition;
                }
                GridManager.Instance.NetworkTransform.Teleport(GridManager.Instance.highlight.transform.position);

                if (input.Buttons.WasPressed(previousButtons, 0) && !GridManager.Instance.IsFullColumn((int)snappedX))
                {
                    GridManager.Instance.SpawnShape((int)snappedX);
                    IsFinished = true;
                    GridManager.Instance.highlight.gameObject.SetActive(false);

                    StartCoroutine(wait());
                    IEnumerator wait()
                    {
                        yield return new WaitForSeconds(0.5f);
                        GridManager.Instance.ChangeTurn();
                        IsFinished = false;
                    }
                }
            }
        }

		private void OnNicknameChanged()
		{
            uiName.SetNickname(Nickname);
		}
        private void OnAvatarIndexChanged()
        {
            uiName.SetAvatar(AvatarIndex, playerAvatar.playerAvatars);
        }
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
		private void RPC_SetNickname(string nickname)
		{
			Nickname = nickname;
		}
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetAvatar(int index)
        {
            AvatarIndex = index;
        }
    }
}
