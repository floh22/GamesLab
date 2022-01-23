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

        public int secondsAlive = 5;
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
            

            capsuleCollider.enabled = true;
            following = null;
            isFollowing = false;
            rectTransform.position -= FollowingOffset;
            
            if (aliveTimer != null)
            {
                StopCoroutine(aliveTimer);
            }

            aliveTimer = StartCoroutine(DespawnAfterTimeAlive());
        }

        public void Follow(Transform target)
        {
            isFollowing = true;
            following = target;
            
            if (aliveTimer != null)
            {
                StopCoroutine(aliveTimer);
            }
        }

        #endregion

        #region Unity API

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            meshRenderer.enabled = true;
            rotation = StartCoroutine(Rotate());
            
            DontDestroyOnLoad(this);
        }

        private void Update()
        {
            if (!photonView.IsMine) return;
            
            if(following)
                rectTransform.position = following.position + FollowingOffset;
            else
                aliveTimer ??= StartCoroutine(DespawnAfterTimeAlive());
        }
        
        
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(this.isFollowing);
            }
            else
            {
                this.isFollowing = (bool) stream.ReceiveNext();
            }
        }

        #endregion

        #region Coroutines
        
        

        IEnumerator DespawnAfterTimeAlive()
        {
            Debug.Log($"Page despawning in {secondsAlive}");
            yield return new WaitForSeconds(secondsAlive);
            //if for some reason this goes through and we are following a player, dont destroy
            if (isFollowing || !photonView.IsMine)
                yield break;
            
            Debug.Log("Page despawned!");
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