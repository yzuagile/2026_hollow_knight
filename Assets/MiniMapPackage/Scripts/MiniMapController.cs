using UnityEngine;

public class MiniMapController : MonoBehaviour
{
    [Header("References")]
    public RectTransform mapRect;        // MapImage
    public RectTransform playerIcon;     // PlayerIcon
    public Transform player;             // Player / MC

    [Header("World Map Range")]
    public float worldMinX = -10f;       // 場景最左邊
    public float worldMaxX = 10f;        // 場景最右邊
    public float worldMinY = -4f;        // 場景最下面
    public float worldMaxY = 4f;         // 場景最上面

    [Header("Mini Map Usable Area")]
    public float mapLeftPadding = 0f;    // 小地圖左邊空白
    public float mapRightPadding = 0f;   // 小地圖右邊空白
    public float mapTopPadding = 0f;     // 小地圖上方空白
    public float mapBottomPadding = 0f;  // 小地圖下方空白

    void Update()
    {
        if (mapRect == null || playerIcon == null || player == null)
        {
            return;
        }

        // 將玩家世界座標轉成 0 ~ 1
        float normalizedX = Mathf.InverseLerp(worldMinX, worldMaxX, player.position.x);
        float normalizedY = Mathf.InverseLerp(worldMinY, worldMaxY, player.position.y);

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        // 小地圖實際寬高
        float mapWidth = mapRect.rect.width;
        float mapHeight = mapRect.rect.height;

        // 真正可以放紅點的範圍，不一定是整張圖
        float usableLeft = -mapWidth / 2f + mapLeftPadding;
        float usableRight = mapWidth / 2f - mapRightPadding;
        float usableBottom = -mapHeight / 2f + mapBottomPadding;
        float usableTop = mapHeight / 2f - mapTopPadding;

        // 把 0~1 的比例換算到可用區域
        float mapX = Mathf.Lerp(usableLeft, usableRight, normalizedX);
        float mapY = Mathf.Lerp(usableBottom, usableTop, normalizedY);

        playerIcon.anchoredPosition = new Vector2(mapX, mapY);
    }
}