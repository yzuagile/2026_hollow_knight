using UnityEngine;

public class MiniMapToggle : MonoBehaviour
{
    [Header("Mini Map Object")]
    public GameObject miniMapPanel;

    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.M;

    void Update()
    {
        // Press M to show / hide mini map
        if (Input.GetKeyDown(toggleKey))
        {
            if (miniMapPanel != null)
            {
                miniMapPanel.SetActive(!miniMapPanel.activeSelf);
            }
        }
    }
}