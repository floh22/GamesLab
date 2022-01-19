using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI
{
    public class RandomBGValueGenerator : MonoBehaviour
    {
        #region Public Fields

        public bool IsReady { get; set; }
        public Sprite[] backgrounds;

        public int RandomValue { get; set; }

        #endregion

        #region Unity API

        public void Start()
        {
            RandomValue = Random.Range(0, backgrounds.Length);
            IsReady = true;
        }

        #endregion

        #region Coroutines

        #endregion
    }
}