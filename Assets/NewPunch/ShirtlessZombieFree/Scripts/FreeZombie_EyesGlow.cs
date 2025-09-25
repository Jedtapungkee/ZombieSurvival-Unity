using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeZombie_EyesGlow : MonoBehaviour
{



    private int eyesTyp;
    public Material[] BodyMaterials = new Material[1];

    public enum EyesGlow
    {
        No,
        Yes
    }


    public EyesGlow eyesGlow;

    void OnValidate()
    {
        // ตรวจสอบว่า BodyMaterials มีค่าและไม่เป็น null
        if (BodyMaterials == null || BodyMaterials.Length == 0 || BodyMaterials[0] == null)
            return;

        if (eyesGlow == 0)
        {
            // ปิดการเรืองแสง - รองรับทั้ง Built-in และ URP
            if (BodyMaterials[0].HasProperty("_EMISSION"))
                BodyMaterials[0].DisableKeyword("_EMISSION");
            
            if (BodyMaterials[0].HasProperty("_EmissiveExposureWeight"))
                BodyMaterials[0].SetFloat("_EmissiveExposureWeight", 1);
        }
        else
        {
            // เปิดการเรืองแสง - รองรับทั้ง Built-in และ URP
            if (BodyMaterials[0].HasProperty("_EMISSION"))
                BodyMaterials[0].EnableKeyword("_EMISSION");
            
            if (BodyMaterials[0].HasProperty("_EmissiveExposureWeight"))
                BodyMaterials[0].SetFloat("_EmissiveExposureWeight", 0);
        }
    }
}
