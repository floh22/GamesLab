using System;
using System.Collections;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;

namespace NetworkedPlayer
{
    public class Page : MonoBehaviourPunCallbacks
    {
        #region Public Fields

        public int secondsAlive = 30;
        [SerializeField] public CapsuleCollider capsuleCollider;

        #endregion

        #region Private Fields

        private RectTransform rectTransform;
        private MeshRenderer meshRenderer;
        private Coroutine rotation;
        private Coroutine aliveTimer;

        private bool isFollowing;
        private Transform following;
        private static readonly Vector3 FollowingOffset = new Vector3(0, 3.5f, 0);

        #endregion

        #region Public API
        
        public void Drop()
        {
            if (!photonView.IsMine) return;
            photonView.TransferOwnership(PhotonNetwork.MasterClient);

            capsuleCollider.enabled = true;
            aliveTimer = StartCoroutine(DespawnAfterTimeAlive());
            following = null;
            isFollowing = false;
            rectTransform.position -= FollowingOffset;
        }

        public void Follow(Transform target)
        {
            isFollowing = true;
            following = target;
        }

        #endregion

        #region Unity API

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            meshRenderer.enabled = true;
            rotation = StartCoroutine(Rotate());
        }

        void OnTriggerEnter(Collider collider)
        {
            GameObject colliderObject = collider.gameObject;
            if (!colliderObject.CompareTag("Player")) return;
            
            PlayerController player = colliderObject.GetComponent<PlayerController>();
            if (player.HasPage) return;

            photonView.TransferOwnership(player.photonView.Owner);
            following = colliderObject.transform;
            isFollowing = true;
            
            if (aliveTimer != null)
            {
                StopCoroutine(aliveTimer);
            }
            
            player.PickUpPage(this);
            
        }

        private void Update()
        {
            if (following)
            {
                rectTransform.position = following.position + FollowingOffset;
            }
        }

        #endregion

        #region Coroutines
        
        

        IEnumerator DespawnAfterTimeAlive()
        {
            yield return new WaitForSeconds(secondsAlive);
            //if for some reason this goes through and we are following a player, dont destroy
            if (isFollowing)
                yield break;
            PhotonNetwork.Destroy(this.gameObject);
        }

        IEnumerator Rotate()
        {
            while (true)
            {
                rectTransform.Rotate(0f, 3f, 0f);
                yield return new WaitForSeconds(0.01f);
            }
        }

        #endregion
    }
}