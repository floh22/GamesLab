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

        #endregion

        #region Public API

        public void TurnOn()
        {
            meshRenderer.enabled = true;
            rotation = StartCoroutine(Rotate());
        }


        public void TurnOff()
        {
            if (aliveTimer != null)
            {
                StopCoroutine(aliveTimer);
            }

            StopCoroutine(rotation);
            rotation = null;
            aliveTimer = null;
            meshRenderer.enabled = false;
        }

        #endregion

        #region Unity API

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            meshRenderer = GetComponent<MeshRenderer>();
            // capsuleCollider.enabled = false;
            TurnOn();
        }

        void OnTriggerEnter(Collider collider)
        {
            GameObject colliderObject = collider.gameObject;
            if (colliderObject.tag == "Player")
            {
                PlayerController player = colliderObject.GetComponent<PlayerController>();
                if (!player.HasPage)
                {
                    player.PickUpPage();
                    TurnOff();
                    if (photonView.IsMine)
                    {
                        PhotonNetwork.Destroy(gameObject);
                    }
                }
            }
        }

        #endregion

        #region Coroutines

        public void Drop()
        {
            if (photonView.IsMine)
            {
                Debug.Log("Pages has been dropped on the ground.");
                photonView.TransferOwnership(photonView.OwnerActorNr);

                capsuleCollider.enabled = true;
                aliveTimer = StartCoroutine(DespawnAfterTimeAlive());
            }
        }

        public void Parent(Transform t)
        {
            transform.SetParent(t, true);
        }

        IEnumerator DespawnAfterTimeAlive()
        {
            yield return new WaitForSeconds(secondsAlive);
            TurnOff();
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