using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISample.Common
{
    public sealed class BackToMainButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private string _mainSceneName = "MainScene";

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponentInChildren<Button>();
            }

            if (_button != null)
            {
                _button.onClick.AddListener(GoToMain);
            }
        }

        public void GoToMain()
        {
            Time.timeScale = 1f;
            DOTween.KillAll(false);
            SceneManager.LoadScene(_mainSceneName);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(GoToMain);
            }
        }
    }
}
