using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

///<summary>
///MediaPipe等のポーズ推定結果（Point Annotation）をボーンに反映させるクラス
///シングルトン化により、外部（生成側）からの座標注入にも対応
///</summary>
public class PoseFollower : MonoBehaviour{
    public static PoseFollower Instance { get; private set; }

    void Awake(){
        //シングルトンの初期化と重複チェック
        if(Instance!=null&&Instance!=this){
            Destroy(gameObject);
            return;
        }
        Instance=this;
    }

    //MediaPipe Pose ランドマークのインデックス定義
    private const int LEFT_SHOULDER  =11;
    private const int RIGHT_SHOULDER =12;
    private const int LEFT_ELBOW     =13;
    private const int RIGHT_ELBOW    =14;
    private const int LEFT_WRIST     =15;
    private const int RIGHT_WRIST    =16;
    private const int REQUIRED_POINTS=17;

    [Header("--- ボーン参照 ---")]
    public Transform rightUpperArm;
    public Transform rightLowerArm;
    public Transform leftUpperArm;
    public Transform leftLowerArm;

    [Header("--- Point Annotationの親 (任意) ---")]
    [Tooltip("HierarchyのSolutionをドラッグ&ドロップ。未設定でも動きます。")]
    public Transform pointsRoot;

    [Header("--- オフセット調整 (X=前後傾き, Y=ねじれ, Z=上下傾き) ---")]
    public Vector3 rOffset=new Vector3(0f, 88.1f, -183.8f);
    public Vector3 lOffset=new Vector3(0f, 59.8f, 33.4f);

    [Range(0f, 1f)] public float smoothness = 0.2f;

    private Transform[] _points=null;
    private bool _isReady=false;

    void Start(){
        //pointsRootが設定されていれば効率的な検索を、なければ全体検索を開始
        if(pointsRoot != null){
            StartCoroutine(WaitForPointsUnderRoot());
        }
        else{
            Debug.LogWarning("[PoseFollower] pointsRootが未設定です。FindObjectsOfTypeで検索します。");
            StartCoroutine(WaitAndCacheFallback());
        }
    }

    ///<summary>
    ///特定の親オブジェクト以下からPoint Annotationを探してキャッシュする
    ///</summary>
    IEnumerator WaitForPointsUnderRoot(){
        while(!_isReady){
            var found = pointsRoot
                .GetComponentsInChildren<Transform>()
                .Where(t=>t.name.Contains("Point Annotation"))
                .OrderBy(t=>t.GetSiblingIndex())
                .ToArray();

            if(found.Length>=REQUIRED_POINTS){
                _points=found;
                _isReady=true;
                Debug.Log($"[PoseFollower] キャッシュ完了 (Root経由): {_points.Length}点");
            }
            else{
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    ///<summary>
    ///シーン全体からPoint Annotationを探してキャッシュする（Fallback用）
    ///</summary>
    IEnumerator WaitAndCacheFallback(){
        while (!_isReady){
            var found=FindObjectsOfType<GameObject>()
                .Where(o=>o.name.Contains("Point Annotation"))
                .OrderBy(o=>o.transform.GetSiblingIndex())
                .ToArray();

            if(found.Length>=REQUIRED_POINTS){
                _points=found.Select(o => o.transform).ToArray();
                _isReady=true;
                Debug.Log($"[PoseFollower] キャッシュ完了 (Fallback): {_points.Length}点");
            }
            else{
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    ///<summary>
    ///外部の生成管理スクリプト等からポイント配列を直接受け取る
    ///</summary>
    public void SetPoints(Transform[] points){
        if(points==null||points.Length<REQUIRED_POINTS){
            Debug.LogWarning("[PoseFollower] SetPoints: 点数が不足しています");
            return;
        }
        _points=points;
        _isReady=true;
        StopAllCoroutines(); //検索ループを停止
        Debug.Log($"[PoseFollower] 外部からポイントを受け取りました: {_points.Length}点");
    }

    void LateUpdate(){
        if(!_isReady){
        return;
        }

        //右腕の計算
        UpdateArm(_points[RIGHT_SHOULDER], _points[RIGHT_ELBOW], rightUpperArm, rOffset);
        UpdateArm(_points[RIGHT_ELBOW], _points[RIGHT_WRIST], rightLowerArm, rOffset);
        //左腕の計算
        UpdateArm(_points[LEFT_SHOULDER], _points[LEFT_ELBOW], leftUpperArm, lOffset);
        UpdateArm(_points[LEFT_ELBOW], _points[LEFT_WRIST], leftLowerArm, lOffset);
    }

    ///<summary>
    ///startからendに向かうベクトルに基づいてboneの回転を更新
    ///</summary>
    void UpdateArm(Transform start, Transform end, Transform bone, Vector3 offset){
        if(bone==null||start==null||end==null){
        return;
        }

        Vector3 direction=end.position-start.position;
        //ベクトルが小さすぎる場合は計算をスキップ（ジッタリング対策）
        if(direction.sqrMagnitude<0.0001f){
        return;
        }

        //2軸の回転角を算出
        float angleY =Mathf.Atan2(direction.y, direction.x)*Mathf.Rad2Deg;
        float angleZ =Mathf.Atan2(direction.z, Mathf.Sqrt(direction.x*direction.x+direction.y*direction.y))*Mathf.Rad2Deg;

        //オフセットを加味してクォータニオンを生成
        Quaternion targetRotation=Quaternion.Euler(
            angleZ+offset.x,
            180f+offset.y,
            -angleY+offset.z
        );

        //smoothnessに基づいた滑らかな回転の適用
        bone.rotation=Quaternion.Slerp(bone.rotation, targetRotation, smoothness);
    }
}
