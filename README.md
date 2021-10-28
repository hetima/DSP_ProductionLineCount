# ProductionLineCount 

Mod for Dyson Sphere Program. Needs BepInEx.

This mod displays in a Assembler Window how many facilities are operating on a single production line and how many maximum can be installed without getting stuck. The targets are Assembler, Smelter, Chemical plant, Oil refinery and Miniature particle collider.

Calculated from the quantity of materials/products and the performance of the belt. If there is a grade, such as Assembler, each grade will be displayed separately.

__Above__ : Current number of installations. This applies to facilities connected to exactly the same belt.  
__Bottom__ : Maximum number of possible installations without getting stuck. If the belt to be calculated is MK.I, it is marked with ".". MK.II will be marked with ":".

---

単一の生産ライン上に何台の施設が稼働しているのか、また最大何台まで詰まらせずに設置できるのかを施設のウィンドウ内に表示します。組立機、精錬所、化学プラント、精油所、小型粒子衝突型加速器が対象です。

素材/生成物の数量とベルトの性能から計算しています。組立機のようにグレードがあるものはそれぞれのグレードごとに表示します。

__上__ : 現在の設置数。同一のベルトに接続されている同一レシピの施設が対象です  
__下__ : 素材/生成物が詰まらない最大の設置可能数。計算対象のベルトがMK.Iの場合は「.」、MK.IIの場合は「:」のマークが添えられます。

設置可能数は [直線ライン構築情報 - Dyson Sphere Program JP Wiki](https://wikiwiki.jp/dsp/%E7%9B%B4%E7%B7%9A%E3%83%A9%E3%82%A4%E3%83%B3%E6%A7%8B%E7%AF%89%E6%83%85%E5%A0%B1) に記載されている内容と基本的に同一です。


## Release Notes

### v1.0.0

- Initial Release

