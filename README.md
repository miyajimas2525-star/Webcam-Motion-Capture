Webcam-Motion-Capture
Webカメラの映像から、Google MediaPipeを用いてリアルタイムでアバター（VRM）を動かすモーションキャプチャシステムです。

・主な機能と工夫した点
1. 数学的なアプローチによる関節角度の算出
MediaPipeから取得した3D座標データ（Point Annotation）を元に、C#の Mathf.Atan2 を用いて2点間のベクトルから回転角度を算出しています。

こだわり: 標準的なライブラリに頼る前に、3D空間における回転の仕組みを数学的に理解し、自力で制御することに主眼を置きました。

オフセット補正: アバターのモデルや初期姿勢の個体差を吸収するため、各関節に対して Vector3 による3軸オフセット調整機能を実装しています。

2. パフォーマンスを意識したオブジェクト管理
動的に生成される座標オブジェクトを効率よく扱うため、以下の最適化を行っています。

キャッシュ戦略: IEnumerator を活用し、オブジェクトの生成完了を待機してから配列にキャッシュすることで、Update 内での Find 処理（高負荷処理）を完全に排除しました。

検索範囲の限定: 親オブジェクト（pointsRoot）を指定可能にし、シーン全体ではなく特定階層のみを走査することで、CPUスパイクの発生を抑制しています。

3. 拡張性と堅牢性を備えた設計
シングルトン・パターン: PoseFollower.Instance を通じて、外部の生成管理スクリプトから直接座標データを注入（Dependency Injection）できる疎結合な設計を採用しました。

・使用技術
Unity:Unity 6.3 LTS(6000.3.10f1)
言語:C#
外部ライブラリ:Google MediaPipe (Unity Plugin)
開発環境:Windows 11/Visual Studio Code

・主要なソースコード
PoseFollower.cs:座標から回転への変換ロジック、キャッシュ機構、シングルトン管理を実装。
パス:Assets/_Recovery/PoseFollower.cs

・今後の展望とリファクタリング計画
本プロジェクトは現在も継続的に改善を行っています。

Quaternion.LookRotation への移行:
現状の Mathf.Atan2 ベースの計算を、Unity標準の Quaternion.LookRotation を用いたベクトル演算へリファクタリング予定です。これにより、計算負荷をさらに抑え、ジンバルロックに強い安定した回転制御を目指します。
LINQの排除:
初期化時の Where や OrderBy を for ループに置き換え、メモリ確保（GC Alloc）を最小限に抑える最適化を行います。
全身トラッキング:
腕部だけでなく、下半身や腰の独立した動きを反映させるロジックの実装。


・実装手順
1. 動作環境
・Unity 6 (または Unity 2021.3 LTS以上)
・MediaPipe Unity Plugin v0.16.3 導入済み
ダウンロード先(https://github.com/homuler/MediaPipeUnityPlugin)
・UniVRM (VRM1.0用) 導入済み
ダウンロード先(https://github.com/vrm-c/UniVRM)
※モデルのインポートおよびVRM1.0規格の制御に使用します。

2. インストール 
このリポジトリをgit cloneするか、ZIPでダウンロードして解凍します。

Unity Hubからプロジェクトを開きます。
MediaPipe Unity Pluginが導入されていることを確認してください。
もしエラーが出る場合は、公式のMediaPipe Unity Pluginの指示に従ってPackage Managerから再インストールが必要な場合があります。

3. シーンの準備 
Projectウィンドウから以下のパスにあるシーンを開きます。
Assets/Samples/MediaPipe Unity Plugin/0.16.3/Official Solutions/Scenes/Pose/Pose Landmark Detection
Hierarchyに自分のVRMモデル（HANA）を配置します。

4. スクリプトの設定 
配置したモデルにPoseFollower.csをアタッチします。

Inspectorで以下の項目をドラッグ＆ドロップで設定します：
・各関節のボーン: RightUpperArm,LeftUpperArmなど、対応するアバターのボーンを指定。
・Offset調整:自分のアバターの初期姿勢に合わせて、Inspector上のスライダー（rUpperOffset 等）を動かして調整してください。

5. 実行 
Unityエディタの再生ボタンを押します。
カメラに自分の全身が映るように立ちます。
画面にPoint Annotation（白い点）が出現すると、アバターが連動して動き出します。

・モデルが映らない場合: CameraのPriorityを -1 以上に設定し、Culling MaskがEverythingになっているか確認してください。
・動きが逆になる場合:InspectorのOffset数値にマイナスを入れて調整してください。
・シーン移動時の保存: シーンを切り替える際は、変更が失われないよう必ずSaveを選択してください。







