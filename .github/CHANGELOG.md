# v1.0.0 - コースティクス for YMM4

YukkuriMovieMaker4 向けのコースティクスエフェクトプラグインの初回リリースです。
Direct2D カスタムピクセルシェーダーが波の高さ場を手続き的に生成し、その傾きで映像を屈折させ、曲がり方から光の集まる筋を求めます。
参照画像や外部モデルを使わず、分散で光と映像に虹色のずれを、吸収で媒質の色みを加えます。
高さ場は座標・時刻・シード値から決定論的に決まります。
8 言語リソース構成の UI を備えます。

---

## 新機能

### 1. ピクセルシェーダー

`Caustics.hlsl` の `main` は、シーン座標から波の高さ場を求め、その傾きと曲がり方から集光模様と映像の屈折を計算します。追加テクスチャは使用しません。`strength`・`displacement`・`absorption` がすべて 0 以下で光のみ表示が無効のときは、ソースをそのまま返します。乱数は `Hash.hlsli` の `hash13` と `hash21` を用います。

#### 高さ場の微分

`NoiseDeriv` は値ノイズの高さ場について、勾配とヘッセ行列を解析的に求めます。集束度をヘッセ行列から求めるため、補間は C2 連続の quintic を用います。cubic ではセル境界でヘッセ行列が不連続になり、格子状の筋が出ます。`FieldDeriv` は 4 オクターブ分の勾配とヘッセ行列を、異方性行列を通して積算します。サンプル位置は `base = posScene.xy × invFeature + seedOfs − flow × time`、断面は `z = time × boilSpeed + seed × 3.7` です。

| 値 | 説明 |
|---|---|
| `invFeature` | スケールから求める高さ場の周波数 |
| `flowX` / `flowY` | 時刻に掛けてサンプル位置をずらす流れ |
| `boilSpeed` | 時刻に掛けて高さ場の断面を進める変化速度 |
| `anisoScale` / `anisoAngle` | 高さ場を一方向へ引き伸ばす異方性行列 |

#### 集光の明るさ

`CausticBrightness` は、屈折後の面積のヤコビアン行列式 `detJ = (1 + κ Hxx)(1 + κ Hyy) − κ² Hxy²` を求め、`exp(−|detJ| / sigma)` を明るさとします。`detJ` が 0 に近い場所ほど光が集まり、明るい筋になります。集束係数は `κ = focus × 3`、`sigma` はシャープさから求めます。

| 値 | 説明 |
|---|---|
| `Hxx` / `Hyy` / `Hxy` | 高さ場のヘッセ行列の各成分 |
| `κ` | 集束係数 `focus × 3` |
| `sigma` | シャープさから求める筋の細さ |

#### 分散

赤・緑・青で集束係数と変位量を分散でずらします。`dR = 1 − dispersion`、`dB = 1 + dispersion` を用いて、明るさは `κ × dR`・`κ`・`κ × dB` の 3 通りで求め、屈折した映像の赤と青も同じ比率でずらしてサンプリングします。この差で光の縁と映像に虹色のずれが生じます。

#### 屈折・吸収・合成

高さ場の勾配 `grad` を長さ 1 でクランプし、`grad × displacement` だけずらした位置で映像をサンプリングします。吸収が 0 より大きいときは、透過率 `pow(absorbColor, absorption × 2)` を映像へ掛けます。最後に光を映像へ加算します。

| 項目 | 式 |
|---|---|
| 集光の光 | `lightColor × brightness × (strength × 1.5)` |
| 屈折後の色 | `SampleInput(uv + grad × displacement)` |
| 透過率 | `pow(max(absorbColor, 0.001), absorption × 2)` |
| 出力色 | `min(col.rgb + light × col.a, a)` |
| 光のみ表示の出力 | `float4(min(light × source.a, aL), aL)` |

出力はいずれもプリマルチプライドを保ち、アルファは屈折後のアルファと光の最大チャンネルの大きい方を採ります。

---

### 2. カスタムシェーダーエフェクト

`CausticsCustomEffect` は `[CustomEffect(1)]` の 1 入力エフェクトです。公開プロパティは `SetValue` を介して定数バッファーへ転送します。各プロパティは代入時にシェーダーが前提とする範囲へ制限します。

| プロパティ | 型 | 範囲 |
|---|---|---|
| `Displacement` | `float` | 0〜2000 |
| `InvFeature` | `float` | 1e-5〜1 |
| `Time` | `float` | 制限なし |
| `Strength` | `float` | 0〜10 |
| `Sigma` | `float` | 1e-4〜4 |
| `Dispersion` | `float` | 0〜0.5 |
| `Focus` | `float` | 0〜8 |
| `Seed` | `float` | 制限なし |
| `LightR` / `LightG` / `LightB` | `float` | 0〜1 |
| `LightOnly` | `int` | 0 または 1 |
| `AbsorbR` / `AbsorbG` / `AbsorbB` | `float` | 0〜1 |
| `Absorption` | `float` | 0〜4 |
| `FlowX` / `FlowY` | `float` | 制限なし |
| `AnisoScale` | `float` | 0.02〜1 |
| `AnisoAngle` | `float` | 制限なし |
| `BoilSpeed` | `float` | 制限なし |

`ConstantBuffer` のレイアウトは以下のとおりです。末尾に 3 つの詰め物を置き、合計 96 バイトを 16 バイトの倍数に揃えます。

| フィールド | 型 | 説明 |
|---|---|---|
| `Displacement` | `float` | 変位 |
| `InvFeature` | `float` | 高さ場の周波数 |
| `Time` | `float` | 時刻（秒） |
| `Strength` | `float` | 光の強さ |
| `Sigma` | `float` | シャープさ |
| `Dispersion` | `float` | 分散 |
| `Focus` | `float` | 集束 |
| `Seed` | `float` | シード値 |
| `LightR` / `LightG` / `LightB` | `float` | 光色 R/G/B |
| `LightOnly` | `int` | 光のみ表示 |
| `AbsorbR` / `AbsorbG` / `AbsorbB` | `float` | 吸収色 R/G/B |
| `Absorption` | `float` | 吸収 |
| `FlowX` / `FlowY` | `float` | 流れの横/縦成分 |
| `AnisoScale` | `float` | 異方性の強さ |
| `AnisoAngle` | `float` | 波の角度（ラジアン） |
| `BoilSpeed` | `float` | 変化速度 |
| `Pad0`〜`Pad2` | `float` | 詰め物 |

`MapInputRectsToOutputRect` は屈折で映像が外側へずれる分だけ出力矩形を拡張します。`MapOutputRectToInputRects` は同じ分だけ入力矩形を拡張します。拡張量は `ceil(min(displacement × (1 + dispersion) + 3, 4096))` で、光のみ表示のときは 0 です。退化した入力矩形はそのまま返します。

シェーダーリソース: `pack://application:,,,/Caustics;component/Shaders/Caustics.cso`（ps_5_0、`ShaderResourceUri.Get` が生成）

---

### 3. エフェクト定義

`CausticsEffect` は YMM4 の映像エフェクトとして宣言されます。

`[VideoEffect]` 属性は以下のパラメーターで宣言されます。

- 表示名：`Texts.CausticsEffectName`（ローカライズキー、日本語では「集光模様」）
- カテゴリー：`VideoEffectCategories.Filtering`
- 検索タグ：`TagCaustics`・`TagWater`・`TagLight`
- `IsAviUtlSupported = false` により AviUtl 向け EXO 出力は非対応
- `ResourceType = typeof(Texts)` でローカライズリソースを指定

`Label` プロパティは `Texts.CausticsEffectName` を返します。

公開プロパティは以下のとおりです。基本項目は「集光模様」グループ、それ以外は「集光模様の詳細」グループに属します。

| プロパティ | 型 | デフォルト | 内部範囲 | アニメーション |
|---|---|---|---|---|
| `Strength` | `Animation` | 50 | 0〜500 | あり |
| `Displacement` | `Animation` | 8 | 0〜500 | あり |
| `Scale` | `Animation` | 100 | 1〜2000 | あり |
| `Speed` | `Animation` | 50 | -1000〜1000 | あり |
| `LightColor` | `Color` | `#FFFFFFFF` | — | なし |
| `Sharpness` | `Animation` | 50 | 0〜100 | あり |
| `Focus` | `Animation` | 20 | 0〜400 | あり |
| `Dispersion` | `Animation` | 0 | 0〜100 | あり |
| `Absorption` | `Animation` | 0 | 0〜100 | あり |
| `AbsorptionColor` | `Color` | `#FF2E8B9A` | — | なし |
| `FlowSpeed` | `Animation` | 0 | -1000〜1000 | あり |
| `FlowAngle` | `Animation` | 0 | -36000〜36000 | あり |
| `Anisotropy` | `Animation` | 50 | 0〜95 | あり |
| `WaveAngle` | `Animation` | 0 | -36000〜36000 | あり |
| `LightOnly` | `bool` | false | — | なし |
| `Seed` | `int` | 0 | 0〜int.MaxValue | なし |

`GetAnimatables` は `Strength`・`Displacement`・`Scale`・`Speed`・`Sharpness`・`Focus`・`Dispersion`・`Absorption`・`FlowSpeed`・`FlowAngle`・`Anisotropy`・`WaveAngle` を返します。

`CreateExoVideoFilters` は空のシーケンスを返します（EXO 非対応）。`CreateVideoEffect` は映像処理用のインスタンスを生成します。

---

### 4. フレームごとの更新

各フレームで YMM4 の `EffectDescription` からフレーム位置、アイテム長、FPS を取得し、アニメーション値を評価します。前フレームと値が異なる項目だけをカスタムシェーダーへ転送します。`Time` はフレーム位置と FPS から毎フレーム更新します。

| パラメータ | 変換 |
|---|---|
| `Strength` | `value / 100` |
| `Displacement` | px のまま |
| `Scale` | `1 / (1.5 × max(value, 1))` を `InvFeature` へ |
| `Sharpness` | `10^(−2.2 × value / 100)` を `Sigma` へ |
| `Focus` | `value / 100` |
| `Dispersion` | `value / 100 × 0.5` |
| `Absorption` | `value / 100` に吸収色の不透明度を掛ける |
| `FlowSpeed` / `FlowAngle` | `cos/sin(角度) × (速さ / 100)` を `FlowX`・`FlowY` へ |
| `Anisotropy` | `1 − value / 100` を `AnisoScale` へ |
| `WaveAngle` | 度からラジアンへ変換し `AnisoAngle` へ |
| `Speed` | `value / 100` を `BoilSpeed` へ |
| `LightColor` | `R/G/B` を 0〜1 の float へ変換し、いずれも不透明度を掛ける |
| `AbsorptionColor` | `R/G/B` を 0〜1 の float へ変換 |
| `LightOnly` | 真偽値を 1 または 0 へ |
| `Seed` | 整数のまま |
| `Time` | `frame / fps` |

入力は `SetInput(0, input, true)` でカスタムシェーダーへ接続します。エフェクトチェーンのクリア時は入力 0 を `null` に戻します。

---

### 5. ローカライズ

`Texts` クラスは `[AutoGenLocalizer]` 属性を持つ `partial` クラスとして宣言されます。
`YukkuriMovieMaker.Generator` のソースジェネレーターが `Texts.csv` を処理し、各ロケールのリソースファイルを自動生成します。

対応リソース：日本語（`ja-jp`）・英語（`en-us`）・中国語簡体字（`zh-cn`）・中国語繁体字（`zh-tw`）・韓国語（`ko-kr`）・スペイン語（`es-es`）・アラビア語（`ar-sa`）・インドネシア語（`id-id`）

ローカライズキーの一覧は以下のとおりです。

| キー | ja-jp |
|---|---|
| `CausticsEffectName` | 集光模様 |
| `TagCaustics` | コースティクス |
| `TagWater` | 水 |
| `TagLight` | 光 |
| `CausticsStrength` | 光の強さ |
| `CausticsDisplacement` | 変位 |
| `CausticsScale` | スケール |
| `CausticsSpeed` | 変化速度 |
| `CausticsDetailGroup` | 集光模様の詳細 |
| `CausticsLightColor` | 光の色 |
| `CausticsSharpness` | シャープさ |
| `CausticsFocus` | 集束 |
| `CausticsDispersion` | 分散 |
| `CausticsAbsorption` | 吸収 |
| `CausticsAbsorptionColor` | 吸収色 |
| `CausticsFlowSpeed` | 流れの速さ |
| `CausticsFlowAngle` | 流れの角度 |
| `CausticsAnisotropy` | 異方性 |
| `CausticsWaveAngle` | 波の角度 |
| `CausticsLightOnly` | 光のみ表示 |
| `CausticsSeed` | シード値 |
