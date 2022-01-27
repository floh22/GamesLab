using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lobby
{
    public class ServerStatusIndicator : MonoBehaviourPunCallbacks
    {
        public Image statusImage;
        public TMP_Text statusText;

        public Color disconnectedColor;

        public Color connectingColor;

        public Color connectedColor;
        // Start is called before the first frame update
        void Start()
        {
            statusImage.color = disconnectedColor;
            statusText.text = "DISCONNECTED";
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public override void OnConnectedToMaster()
        {
            statusImage.color = connectingColor;
            statusText.text = "CONNECTING";
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            statusImage.color = connectedColor;
            statusText.text = "CONNECTED";
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            statusImage.color = disconnectedColor;
            statusText.text = "DISCONNECTED";
        }
    }
}
