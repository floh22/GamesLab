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

        #endregion

        #region Private Fields

        private RectTransform rectTransform;
        private MeshRenderer meshRenderer;
        private Collider collider;
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
            StopCoroutine(aliveTimer);
            StopCoroutine(rotation);
            rotation = null;
            aliveTimer = null;
            meshRenderer.enabled = false;
            if (GetComponent<PhotonView>().IsMine)
            {
                PhotonNetwork.Destroy(this.gameObject);
            }
        }

        #endregion

        #region Unity API

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            meshRenderer = GetComponent<MeshRenderer>();
            collider = GetComponent<Collider>();
            collider.enabled = false;
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
                }
            }
        }

        #endregion

        #region Coroutines

        public IEnumerator Drop(Vector3 position)
        {
            Debug.Log("Pages has been dropped on the ground.");
            photonView.TransferOwnership(photonView.OwnerActorNr);
            transform.SetParent(null, false);

            //Just does not work with its own position via set parent for some reason
            Vector3 transformPosition = position;
            transformPosition.y = 0.5f;
            transform.position = transformPosition;

            //Wait so that page cannot collide with same player on drop
            yield return new WaitForSeconds(0.1f);
            collider.enabled = true;
            aliveTimer = StartCoroutine(DespawnAfterTimeAlive());
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