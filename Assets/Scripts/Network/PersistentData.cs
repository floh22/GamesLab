using System;
using GameManagement;
using UnityEngine;

namespace Network
{
    public class PersistentData : MonoBehaviour
    {
        public static GameData.Team? Team = null;
        
        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    
    }
}
