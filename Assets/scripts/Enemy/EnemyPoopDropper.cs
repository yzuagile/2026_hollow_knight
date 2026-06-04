using UnityEngine;

public class EnemyPoopDropper : MonoBehaviour
{
    [Header("Poop Settings")]
    [SerializeField] private GameObject poopPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropInterval = 2f;

    private float timer = 0f;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= dropInterval)
        {
            DropPoop();
            timer = 0f;
        }
    }

    private void DropPoop()
    {
        if (poopPrefab == null || dropPoint == null)
        {
            Debug.LogWarning("Poop Prefab 或 DropPoint 沒有設定");
            return;
        }

        Instantiate(poopPrefab, dropPoint.position, Quaternion.identity);
    }
}