using UnityEngine;
using UnityEngine.UI;

public class PlayerMana : MonoBehaviour
{
    [Header("Mana")]
    public int maxMana = 100;
    public int currentMana = 100;
    public Slider manaSlider;

    [Header("Mana Canvas")]
    public RectTransform manaCanvas;
    public Vector3 manaCanvasOffset = new Vector3(0f, 1.4f, 0f);
    public bool followPlayer = true;
    public bool useScreenSpacePosition = false;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (manaCanvas == null && manaSlider != null)
        {
            Canvas canvas = manaSlider.GetComponentInParent<Canvas>();

            if (canvas != null)
                manaCanvas = canvas.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        UpdateManaUI();
        UpdateManaCanvasPosition();
    }

    private void LateUpdate()
    {
        UpdateManaCanvasPosition();
    }

    public bool CanUseMana(int amount)
    {
        return currentMana >= amount;
    }

    public bool TryUseMana(int amount)
    {
        if (!CanUseMana(amount))
            return false;

        currentMana -= amount;
        UpdateManaUI();
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0)
            return;

        currentMana = Mathf.Min(currentMana + amount, maxMana);
        UpdateManaUI();
    }

    public void SetMana(int amount)
    {
        currentMana = Mathf.Clamp(amount, 0, maxMana);
        UpdateManaUI();
    }

    private void UpdateManaUI()
    {
        if (manaSlider == null)
            return;

        manaSlider.maxValue = maxMana;
        manaSlider.value = currentMana;
    }

    private void UpdateManaCanvasPosition()
    {
        if (!followPlayer || manaCanvas == null)
            return;

        Vector3 targetPosition = transform.position + manaCanvasOffset;

        if (useScreenSpacePosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
                targetPosition = mainCamera.WorldToScreenPoint(targetPosition);
        }

        manaCanvas.position = targetPosition;
    }
}
