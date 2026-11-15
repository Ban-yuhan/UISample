using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISample.Common
{
    public sealed class MainHubController : MonoBehaviour
    {
        [SerializeField] private List<Button> _sceneButtons = new List<Button>();
        [SerializeField] private List<string> _sceneNames = new List<string>();
        [SerializeField] private Button _quitButton;
        [SerializeField] private GridLayoutGroup _sceneGrid;
        [SerializeField] private RectTransform _sceneGridRect;

        private void Awake()
        {
            Time.timeScale = 1f;

            for (int i = 0; i < _sceneButtons.Count && i < _sceneNames.Count; i++)
            {
                string targetScene = _sceneNames[i];
                if (_sceneButtons[i] != null)
                {
                    _sceneButtons[i].onClick.AddListener(() => LoadSampleScene(targetScene));
                }
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(QuitApp);
            }

            UpdateResponsiveGrid();
        }

        private void Start()
        {
            Canvas.ForceUpdateCanvases();
            UpdateResponsiveGrid();
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateResponsiveGrid();
        }

        public void UpdateResponsiveGrid()
        {
            if (_sceneGrid == null)
            {
                _sceneGrid = GetComponentInChildren<GridLayoutGroup>();
            }
            if (_sceneGrid == null) return;

            if (_sceneGridRect == null)
            {
                _sceneGridRect = _sceneGrid.GetComponent<RectTransform>();
            }
            if (_sceneGridRect == null) return;

            float availW = _sceneGridRect.rect.width;
            float availH = _sceneGridRect.rect.height;
            if (availW <= 1f || availH <= 1f) return;

            float spacingX = _sceneGrid.spacing.x;
            float spacingY = _sceneGrid.spacing.y;

            float cellWidth = Mathf.Clamp((availW - spacingX) * 0.5f, 320f, 860f);
            float cellHeight = Mathf.Clamp((availH - spacingY * 2f) / 3f, 120f, 200f);

            _sceneGrid.cellSize = new Vector2(cellWidth, cellHeight);
            LayoutRebuilder.MarkLayoutForRebuild(_sceneGridRect);
        }

        public void LoadSampleScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            Time.timeScale = 1f;
            DOTween.KillAll(false);
            SceneManager.LoadScene(sceneName);
        }

        public void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
