using UnityEngine;

public class OptionMenuView : MonoBehaviour
{
    [SerializeField] private GameObject _root;

    // 이거 글로벌 매니져 같은거로 나중에 옮겨서 모든 씬에서 사용할 수 있도록 해
    // void Update()
    // {
    //     if (Keyboard.current.escapeKey.wasPressedThisFrame)
    //         CloseOptionPanel();
    // }

    public void CloseOptionPanel()
    {
        _root.SetActive(false);
    }
}
