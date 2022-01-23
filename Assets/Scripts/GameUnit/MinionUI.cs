using System.Numerics;
using GameManagement;
using NetworkedPlayer;
using Photon.Pun.Demo.PunBasics;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

namespace GameUnit
{
    public class MinionUI : MonoBehaviour
	{
		#region Private Fields

		[Tooltip("Pixel offset from the minion target")]
		[SerializeField]
		private Vector3 screenOffset = new(0f, 30f, 0f);

		[Tooltip("UI Text to display Minion's text")]
		[SerializeField]
		private Text minionText;

		[Tooltip("UI Slider to display Minion's Health")]
		[SerializeField]
		private Slider minionHealthSlider;

		private Minion target;

		private float minionHeight;

		private Minion minion;

		private Renderer targetRenderer;

		private CanvasGroup canvasGroup;

		private Vector3 targetPosition;

		private GameData.Team team; // TODO remove

		#endregion

		#region MonoBehaviour Messages

		private void Awake()
		{

			canvasGroup = this.GetComponent<CanvasGroup>();
			
			this.transform.SetParent(GameObject.Find("Canvas").GetComponent<Transform>(), false);
			
		}
		
		private void Update()
		{
			// Destroy itself if the target is null, It's a fail safe when Photon is destroying Instances of a Minion over the network
			if (target == null) {
				Destroy(this.gameObject);
				return;
			}


			// Reflect the Minion Health
			if (minionHealthSlider != null)
			{
				minionHealthSlider.maxValue = target.MaxHealth;
				minionHealthSlider.value = target.Health;
			}
			else
			{
				Debug.LogError("minionHealthSlider is null in MinionUI");
			}
		}

		private void LateUpdate ()
		{
			// Do not show the UI if we are not visible to the camera, thus avoid potential bugs with seeing the UI, but not the minion itself.
			if (targetRenderer!=null)
			{
				this.canvasGroup.alpha = targetRenderer.isVisible ? 1f : 0f;
			}
			
			// #Critical
			// Follow the Target GameObject on screen.
			if (minion == null) return;

			this.transform.localPosition = Camera.main!.WorldToScreenPoint(minion.transform.position) + screenOffset;

		}

		#endregion

		#region Public Methods
		
		public void SetTarget(Minion toTarget){

			if (toTarget == null) {
				Debug.LogError("<Color=Red><b>Missing</b></Color> Minion target for MinionUI.SetTarget.", this);
				return;
			}

			// Cache references for efficiency because we are going to reuse them.
			this.target = toTarget;
			targetRenderer = this.target.GetComponentInChildren<Renderer>();


			minion = this.target.GetComponent<Minion>();

			// Get data from the Minion that won't change during the lifetime of this Component
			if (minion != null)
			{
				minionHeight = minion.Height;
			}

			if (minionText != null)
			{
				team = minion.Team; // TODO remove
				minionText.text = minion.Team.ToString();
			}
		}

		#endregion

	}
}