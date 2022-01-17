using UnityEngine;
using UnityEngine.UI;

namespace NetworkedPlayer
{
	public class PlayerUI : MonoBehaviour
	{
		#region Private Fields

		[Tooltip("Pixel offset from the player target")]
		[SerializeField]
		private Vector3 screenOffset = new(0f, 30f, 0f);

		[Tooltip("UI Text to display Player's Name")]
		[SerializeField]
		private Text playerNameText;

		[Tooltip("UI Slider to display Player's Health")]
		[SerializeField]
		private Slider playerHealthSlider;

		private PlayerController target;

		private float characterControllerHeight;

		private Transform targetTransform;

		private Renderer targetRenderer;

		private CanvasGroup canvasGroup;

		private Vector3 targetPosition;

		#endregion

		#region MonoBehaviour Messages

		private void Awake()
		{

			canvasGroup = this.GetComponent<CanvasGroup>();
			
			this.transform.SetParent(GameObject.Find("Canvas").GetComponent<Transform>(), false);
			
		}
		
		private void Update()
		{
			// Destroy itself if the target is null, It's a fail safe when Photon is destroying Instances of a Player over the network
			if (target == null) {
				Destroy(this.gameObject);
				return;
			}


			// Reflect the Player Health
			if (playerHealthSlider != null)
			{
				playerHealthSlider.maxValue = target.MaxHealth;
				playerHealthSlider.value = target.Health;
			}
			else
			{
				Debug.LogError("playerHealthSlider is null in PlayerUI");
			}
		}

		private void LateUpdate () {

			// Do not show the UI if we are not visible to the camera, thus avoid potential bugs with seeing the UI, but not the player itself.
			if (targetRenderer!=null)
			{
				this.canvasGroup.alpha = targetRenderer.isVisible ? 1f : 0f;
			}
			
			// #Critical
			// Follow the Target GameObject on screen.
			if (targetTransform == null) return;
			targetPosition = targetTransform.position;
			targetPosition.y += characterControllerHeight;
				
			this.transform.position = Camera.main!.WorldToScreenPoint (targetPosition) + screenOffset;
			

		}




		#endregion

		#region Public Methods
		
		public void SetTarget(PlayerController toTarget){

			if (toTarget == null) {
				Debug.LogError("<Color=Red><b>Missing</b></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
				return;
			}

			// Cache references for efficiency because we are going to reuse them.
			this.target = toTarget;
			targetTransform = this.target.GetComponent<Transform>();
			targetRenderer = this.target.GetComponentInChildren<Renderer>();


			CharacterController characterController = this.target.GetComponent<CharacterController> ();

			// Get data from the Player that won't change during the lifetime of this Component
			if (characterController != null){
				characterControllerHeight = characterController.height;
			}

			if (playerNameText != null) {
				playerNameText.text = this.target.photonView.Owner.NickName;
			}
		}

		#endregion

	}
}
