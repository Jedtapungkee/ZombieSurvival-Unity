using UnityEngine;

public class RotateYawSwingUI : MonoBehaviour
{
    [Header("มุมส่ายสูงสุด (องศา) เช่น 10-20")]
    public float maxAngle = 15f;

    [Header("ความเร็วส่าย")]
    public float speed = 2f;

    void Update()
    {
        float angle = Mathf.Sin(Time.unscaledTime * speed) * maxAngle;
        // หมุนรอบแกน Y (ซ้าย-ขวา)
        var e = transform.localEulerAngles;
        e.y = angle;
        transform.localEulerAngles = e;
    }
}
