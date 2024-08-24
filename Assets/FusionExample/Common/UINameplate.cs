using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starter
{
	/// <summary>
	/// Component that handle showing nicknames above player
	/// </summary>
	public class UINameplate : MonoBehaviour
	{
		public TextMeshProUGUI NicknameText;
        public Image avatar;
        private Transform _cameraTransform;
		public void SetNickname(string nickname)
		{
			NicknameText.text = nickname;
		}
        public void SetAvatar(int index, Sprite[] sprites)
        {
            avatar.sprite = sprites[index];
        }
	}
}
