using UnityEngine;

public class HoverBobUI : MonoBehaviour
{
    [Header("ระยะเด้ง (หน่วย UI ของ RectTransform)")]
    public float amplitude = 30f;   // ลอง 30–60 ถ้า Canvas scale เล็ก
    [Header("ความเร็วเด้ง")]
    public float frequency = 2f;

    RectTransform rt;
    Vector2 startPos;

    void Awake()
    {
        rt = transform as RectTransform;
        startPos = rt.anchoredPosition;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.unscaledTime * frequency) * amplitude;
        rt.anchoredPosition = startPos + new Vector2(0f, y);
    }
}
