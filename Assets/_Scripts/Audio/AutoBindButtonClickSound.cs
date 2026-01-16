using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AutoBindButtonClickSound : MonoBehaviour
{
    [SerializeField] private AudioClip _clickClip;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindAllButtons();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindAllButtons();
    }

    private void BindAllButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var b in buttons)
        {
            b.onClick.RemoveListener(OnButtonClicked);
            b.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUi(_clickClip);
    }
}
