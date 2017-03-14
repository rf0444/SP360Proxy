# SP360Proxy
SP360をAndroid経由でUSBで繋いでストリーム配信する。

## 使用方法

1. SP360 を Wifi モードに設定
2. Android 端末の USB デバッグを有効にする
3. Android 端末の Wifi を有効にし、SP360 へ接続
4. Android 端末と PC を USB で接続
5. Android 端末へ SP360Proxy をインストール
6. `adb forward tcp:8080 tcp:8080` を実行
7. localhost:8080 へアクセスすると、SP360 のストリーム画像が取得できる

