using NetworkedPlayer;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
    public class GameStateController : MonoBehaviourPunCallbacks
    {
        
        static public GameStateController Instance;
        
        [SerializeField]
        private GameObject playerPrefab;

        [SerializeField] private GameObject spawnPointHolder;


        private bool hasLeft = false;
        
        #region Photon Callbacks


        /// <summary>
        /// Called when the local player left the room. We need to load the launcher scene.
        /// </summary>
        public override void OnLeftRoom()
        {
            SceneManager.LoadScene(0);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);
        }

        #endregion


        #region Public Methods


        public void LeaveRoom()
        {
            if (hasLeft)
                return;
            hasLeft = true;
            PhotonNetwork.LeaveRoom();
        }


        #endregion
        
        void Start()
        {
            Instance = this;

            // in case we started this demo with the wrong scene being active, simply load the menu scene
            if (!PhotonNetwork.IsConnected)
            {
                SceneManager.LoadScene(0);

                return;
            }

            if (playerPrefab == null) { // #Tip Never assume public properties of Components are filled up properly, always check and inform the developer of it.

                Debug.LogError("<Color=Red><b>Missing</b></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
            } else {


                if (PlayerController.LocalPlayerInstance==null)
                {
                    Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
                    Vector3 pos = spawnPointHolder.transform.Find(PersistentData.Team.ToString()).transform.position;

                    // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                    GameObject pl = PhotonNetwork.Instantiate(this.playerPrefab.name, pos, Quaternion.identity, 0);
                    
                    pl.transform.Find("FogOfWarVisibleRangeMesh").gameObject.SetActive(true);
                }
                else{

                    Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
                }


            }

        }

        private void Update()
        {
            // "back" button of phone equals "Escape". quit app if that's pressed
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitApplication();
            }
        }
        
        public void QuitApplication()
        {
            Application.Quit();
        }

        
    }
}
