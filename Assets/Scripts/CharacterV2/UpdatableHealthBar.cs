using System.Collections;
using TMPro;
using UnityEngine;

namespace CharacterV2
{
    public class UpdatableHealthBar: MonoBehaviour
    {
        
        private TextMeshPro _text;
        private float _hp;
        private string _name;
        private bool _isReady;
        private bool _updating;
        
        void Start()
        {
            _text = GetComponent<TextMeshPro>();
            _text.SetText($"{_name}: {_hp}");
            _isReady = true;
        }

        public void SetName(string name)
        {
            _name = name;
            UpdateStatus();
        }

        public void SetHP(float hp)
        {
            _hp = hp;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_updating) {
                return;
            }
            StartCoroutine(UpdateText());
        }

        private IEnumerator UpdateText()
        {
            _updating = true;
            yield return new WaitUntil(() => _isReady);
            _text.SetText($"{_name}: {_hp}");
            _updating = false;
        }
    
    }
}