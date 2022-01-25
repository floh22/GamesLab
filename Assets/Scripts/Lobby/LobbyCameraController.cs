using System;
using System.Collections;
using UnityEngine;

namespace Lobby
{
    public class LobbyCameraController : MonoBehaviour
    {

        [SerializeField] private Vector3 menuPosition;
        [SerializeField] private Quaternion menuRotation;

        [SerializeField] private Vector3 lobbyPosition;
        [SerializeField] private Quaternion lobbyRotation;

        [SerializeField] private float moveDuration;

        public bool isMoving;

        [SerializeField] private Transform mainCamera;
        [SerializeField] private Transform lobbyCamera;
        
        private Vector3 posVelocity = Vector3.zero;
        private Quaternion derivVelocity = Quaternion.identity;
        
        // Start is called before the first frame update
        void Start()
        {
            menuPosition = mainCamera.position;
            menuRotation = mainCamera.rotation;

            lobbyPosition = lobbyCamera.position;
            lobbyRotation = lobbyCamera.rotation;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void MoveToWaitingForPlayers(Action onArrive = null)
        {
            if (isMoving)
                return;
            StartCoroutine(MoveCamera(lobbyPosition, lobbyRotation, onArrive));
        }

        public void MoveToMainMenu(Action onArrive = null)
        {
            if (isMoving)
                return;
            StartCoroutine(MoveCamera(menuPosition, menuRotation, onArrive));
        }

        IEnumerator MoveCamera(Vector3 goalPosition, Quaternion goalRotation, Action onArrive = null)
        {
            isMoving = true;
            posVelocity = Vector3.one;
            while (Vector3.Distance(mainCamera.position, goalPosition) > 0.1f || Quaternion.Angle(mainCamera.rotation, goalRotation) > 0.1f)
            {
                mainCamera.position = Vector3.SmoothDamp(mainCamera.position, goalPosition, ref posVelocity, moveDuration);
                mainCamera.rotation = QuaternionUtil.SmoothDamp(mainCamera.rotation, goalRotation,
                    ref derivVelocity, moveDuration - 0.1f);
                yield return null;
            }
            
            isMoving = false;
            onArrive?.Invoke();
        }
    }
}
