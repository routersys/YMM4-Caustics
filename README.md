# 集光模様 for YMM4

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](#)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](#)
[![Release](https://img.shields.io/github/v/release/routersys/YMM4-Caustics.svg)](https://github.com/routersys/YMM4-Caustics/releases)

---

YukkuriMovieMaker4（YMM4）上で動作する、水面を透過した光が集まる集光模様を手続き的に生成する映像エフェクトプラグインです。
参照画像や外部モデルを使わず、波の高さ場から光の筋と映像の屈折を計算します。
分散で光と映像に虹色のずれを加え、吸収で水の色みを付けられます。
数値パラメータはアニメーションに対応しています。

![Image](https://github.com/routersys/YMM4-Caustics/blob/main/docs/Caustics.png)

---

## 目次

1. [概要](#概要)
2. [動作要件](#動作要件)
3. [インストール方法](#インストール方法)
4. [主な機能](#主な機能)
   - [1. 集光模様の生成](#1-集光模様の生成)
   - [2. 屈折による映像の揺らぎ](#2-屈折による映像の揺らぎ)
   - [3. 光の形](#3-光の形)
   - [4. 分散と吸収](#4-分散と吸収)
   - [5. 流れと異方性](#5-流れと異方性)
5. [パラメータ一覧](#パラメータ一覧)
6. [制限事項](#制限事項)
7. [注意事項](#注意事項)
8. [免責事項](#免責事項)
9. [サードパーティライセンス](#サードパーティライセンス)
10. [ライセンス](#ライセンス)

---

## 概要

本プラグインは YMM4 の「映像エフェクト」として動作し、エフェクトの種類一覧では「集光模様」として表示されます。カテゴリはフィルタリングです。

Direct2D カスタムピクセルシェーダーが、シーン座標から波の高さ場を手続き的に求めます。高さ場の傾きで映像を屈折させて揺らし、高さ場の曲がり方から光が集まる場所を求めて明るい筋を重ねます。プールの底や水中に現れる、揺らめく光の網目のような模様を作ります。

高さ場はシード値とシーン座標と時刻から決定論的に生成します。流れの速さと変化速度を時間項に与えると、模様が移動・変形します。座標計算はシーン座標を基準とします。

このエフェクトは AviUtl 向けの EXO 出力（`.exo`）には対応していません。

---

## 動作要件

| 項目 | 要件 |
|---|---|
| OS | Windows 10 バージョン 2004（ビルド 19041）以降 / Windows 11（64bit） |
| YukkuriMovieMaker4 | 最新版を推奨 |
| ランタイム | .NET 10.0 |

---

## インストール方法

1. [Releases](https://github.com/routersys/YMM4-Caustics/releases/latest) ページから最新のプラグインファイル（`.ymme`）をダウンロードしてください。
2. YMM4 が起動していないことを確認し、ダウンロードしたファイルを実行してインストールします。
3. YMM4 を起動し、タイムライン上のアイテムに映像エフェクトを追加します。
4. 映像エフェクトの種類として「集光模様」を選択してください。

---

## 主な機能

### 1. 集光模様の生成

波の高さ場から、光が集まる場所を明るい筋として描きます。手動の点指定や参照画像は不要です。光の強さで筋の明るさ、スケールで模様の大きさ、変化速度で模様が変わる速さを調整します。光の色で筋の色みを決めます。

### 2. 屈折による映像の揺らぎ

高さ場の傾きに沿って元映像をずらし、水面越しに見たような屈折の揺らぎを与えます。変位でずらす量を調整します。変位を 0 にすると映像は歪まず、光の筋だけが重なります。

### 3. 光の形

シャープさで光の筋の細さを決めます。高いほど細く鋭い糸状になります。集束で光の集まりやすさを決めます。高いほど模様が強く折り重なり、明るい網目が密になります。

### 4. 分散と吸収

分散を上げると、波長ごとに屈折の量が変わります。光の縁と屈折した映像に虹色のずれが生じます。吸収を上げると、媒質を透過した光が吸収色へ近づき、水中のような色みになります。

### 5. 流れと異方性

流れの速さと角度で模様全体を平行移動させます。異方性で波を一方向へ引き伸ばし、波の角度でその方向を決めます。異方性を上げると、水路のように筋が伸びた模様になります。シード値を変えると模様の形が変わります。

---

## パラメータ一覧

| パラメータ名 | 型 | デフォルト | スライダー表示範囲 | アニメーション | 説明 |
|---|---|---|---|---|---|
| 光の強さ | 数値 | 50% | 0 〜 100% | ✔ | 集束した光の明るさです。 |
| 変位 | 数値 | 8px | 0 〜 50px | ✔ | 屈折による映像の揺らぎの大きさです。 |
| スケール | 数値 | 100% | 10 〜 500% | ✔ | 波の模様の大きさです。 |
| 変化速度 | 数値 | 50% | -200 〜 200% | ✔ | 波の形が変化する速さです。 |
| 光の色 | 色 | `#FFFFFFFF` | — | ✗ | 集束した光の色です。不透明度は光の明るさに反映されます。 |
| シャープさ | 数値 | 50% | 0 〜 100% | ✔ | 光の筋の細さです。高いほど細く鋭い糸状になります。 |
| 集束 | 数値 | 20% | 0 〜 200% | ✔ | 光の集まりやすさです。高いほど模様が強く折り重なります。 |
| 分散 | 数値 | 0% | 0 〜 100% | ✔ | 波長による屈折差です。光の縁と映像に虹色のずれが生じます。 |
| 吸収 | 数値 | 0% | 0 〜 100% | ✔ | 媒質による色の吸収の強さです。 |
| 吸収色 | 色 | `#FF2E8B9A` | — | ✗ | 媒質を透過した光が近づく色です。 |
| 流れの速さ | 数値 | 0% | -200 〜 200% | ✔ | 波の模様が平行移動する速さです。 |
| 流れの角度 | 数値 | 0° | 0 〜 360° | ✔ | 波の模様が流れる方向です。 |
| 異方性 | 数値 | 50% | 0 〜 95% | ✔ | 波を一方向に引き伸ばす度合いです。 |
| 波の角度 | 数値 | 0° | 0 〜 360° | ✔ | 異方性で引き伸ばす方向です。 |
| 光のみ表示 | ON/OFF | OFF | — | ✗ | 映像を消して光の模様だけを出力します。 |
| シード値 | 整数 | 0 | 0 〜 10000 | ✗ | 波の模様を決める乱数のシード値です。 |

光の色から下の項目は「集光模様の詳細」グループにまとまっています。

---

## 制限事項

- AviUtl 向けの EXO 出力（`.exo`）には対応していません。
- 完全に透明な部分には光の模様が乗りません。
- 変位で映像を屈折させるため、出力範囲は元の素材より大きくなります。

---

## 注意事項

- AviUtl 非対応: 本プラグインは AviUtl 向けの EXO 出力（`.exo`）に対応していません。
- 出力範囲の拡張: 変位で映像を外側へずらすため、変位と分散に応じて出力範囲が広がります。拡張量は上限 4096px です。光のみ表示のときは拡張しません。
- 模様の再現性: 高さ場は座標・時刻・シード値から生成するため、同じ条件では常に同じ結果になります。流れの速さと変化速度が 0 のとき、模様は時間で変化しません。
- 光のみ表示: 元の映像を出力せず、光の模様だけを不透明度付きで出力します。
- 本プラグインを使用する前に、YMM4 プロジェクトファイルのバックアップを作成することを推奨します。

---

## 免責事項

本プラグインは MIT ライセンスのもとで公開されています。

本ソフトウェアは「現状のまま」提供されており、明示・黙示を問わず、商品性、特定目的への適合性、および権利非侵害に関する保証を含む、いかなる種類の保証も行いません。

作者は、本プラグインの使用または使用不能に起因するいかなる損害についても、一切の責任を負いません。
ご利用は自己責任でお願いします。

---

## サードパーティライセンス

本プラグインは以下のサードパーティのコードを使用しています。ライセンスの全文は、リポジトリの [`Caustics/shaders/Hash.hlsli`](Caustics/shaders/Hash.hlsli) の冒頭に収録しています。

| ソフトウェア | 用途 | ライセンス |
|---|---|---|
| [Hash without Sine](https://www.shadertoy.com/view/4djSRW) | 波の高さ場に使うハッシュ関数（`Hash.hlsli`） | MIT License |

### Hash without Sine（MIT License）

`Caustics/shaders/Hash.hlsli` は David Hoskins 氏の "Hash without Sine" を基にし、饅頭遣い（manju-summoner）氏が改変したものです。以下は同ファイルに収録された著作権表示です。

```
Copyright (c)2014 David Hoskins.
Modifications Copyright (c) 2023 manju-summoner.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## ライセンス

[MIT License](LICENSE.txt)
