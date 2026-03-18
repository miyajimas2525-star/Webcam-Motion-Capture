using UnityEngine;
using System.Linq;

public class PoseFollower : MonoBehaviour
{
    [Header("右腕の骨")]
    public Transform rightUpperArm;
    public Transform rightLowerArm;
    public Transform rightHand;

    [Header("右人差し指 (Index)")]
    public Transform rightIndex1; // 付け根
    public Transform rightIndex2; // 第2関節
    public Transform rightIndex3; // 第3関節

    [Header("左腕の骨")]
    public Transform leftUpperArm;
    public Transform leftLowerArm;
    public Transform leftHand;

    void LateUpdate()
    {
        var points = GameObject.FindObjectsOfType<GameObject>()
            .Where(o => o.name.Contains("Point Annotation"))
            .OrderBy(o => o.transform.GetSiblingIndex())
            .ToList();

        // 指の先端（20番以降）までデータがあるか確認
        if (points.Count > 20)
        {
            // --- 右腕 ---
            UpdateArm(points[12].transform, points[14].transform, rightUpperArm, -140f);
            UpdateArm(points[14].transform, points[16].transform, rightLowerArm, -140f);
            UpdateArm(points[16].transform, points[20].transform, rightHand, -140f);

            // --- 右人差し指 (Poseの点 16, 18, 20 を活用) ---
            // 付け根(16)から第2関節(18)の角度を適用
            UpdateArm(points[16].transform, points[18].transform, rightIndex1, -140f);
            // 第2関節(18)から指先(20)の角度を適用
            UpdateArm(points[18].transform, points[20].transform, rightIndex2, -140f);
            UpdateArm(points[18].transform, points[20].transform, rightIndex3, -140f);

            // --- 左腕 ---
            UpdateArm(points[11].transform, points[13].transform, leftUpperArm, -20f);
            UpdateArm(points[13].transform, points[15].transform, leftLowerArm, -20f);
            UpdateArm(points[15].transform, points[19].transform, leftHand, -20f);
        }
    }

    void UpdateArm(Transform start, Transform end, Transform bone, float offset)
    {
        if (bone == null) return;

        float diffX = end.position.x - start.position.x;
        float diffY = end.position.y - start.position.y;
        float diffZ = end.position.z - start.position.z;

        float angleY = Mathf.Atan2(diffY, diffX) * Mathf.Rad2Deg;
        float angleZ = Mathf.Atan2(diffZ, Mathf.Sqrt(diffX * diffX + diffY * diffY)) * Mathf.Rad2Deg;

        // 指も腕と同じロジックで回転させます
        Quaternion targetRotation = Quaternion.Euler(angleZ, 180, -angleY + offset);
        bone.rotation = Quaternion.Lerp(bone.rotation, targetRotation, 0.1f);
    }
}