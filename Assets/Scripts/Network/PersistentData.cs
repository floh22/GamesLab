using System;
using GameManagement;
using UnityEngine;

namespace Network
{
    public class PersistentData : MonoBehaviour
    {
        public static GameData.Team Team = GameData.Team.NONE;
        
        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    
    }
}
