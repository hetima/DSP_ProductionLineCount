//using System;
//using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProductionLineCountMod
{
    [BepInPlugin(__GUID__, __NAME__, "1.1.1")]
    public class ProductionLineCount : BaseUnityPlugin
    {
        public const string __NAME__ = "ProductionLineCount";
        public const string __GUID__ = "com.hetima.dsp." + __NAME__;

        new internal static ManualLogSource Logger;
        void Awake()
        {
            Logger = base.Logger;
            //Logger.LogInfo("Awake");

            new Harmony(__GUID__).PatchAll(typeof(Patch));
        }

        public static Text numText;
        public static Text maxText;
        public static Text maxStackedText;
        public static Text[] requireText;
        public static Text[] productText;
        public static Text[] requireNumText;
        public static Text[] productNumText;
        public static int _lastAssemblerId = 0;

        public static void UpdateState(UIAssemblerWindow assemblerWindow)
        {
            numText.text = "";
            maxText.text = "";
            maxStackedText.text = "";
            for (int i = 0; i < requireText.Length; i++)
            {
                requireText[i].text = "";
            }
            for (int i = 0; i < productText.Length; i++)
            {
                productText[i].text = "";
            }
            for (int i = 0; i < requireNumText.Length; i++)
            {
                requireNumText[i].text = "";
            }
            for (int i = 0; i < productNumText.Length; i++)
            {
                productNumText[i].text = "";
            }

            if (assemblerWindow.assemblerId == 0 || assemblerWindow.factory == null)
            {
                return;
            }
            AssemblerComponent assemblerComponent = assemblerWindow.factorySystem.assemblerPool[assemblerWindow.assemblerId];
            if (assemblerComponent.id != assemblerWindow.assemblerId)
            {
                return;
            }
            if (assemblerComponent.recipeId == 0 || assemblerComponent.entityId == 0)
            {
                return;
            }

            HashSet<int> cargoPaths = GetConnectedCargoPaths(assemblerWindow.factory, assemblerComponent.entityId, out int beltSpeed);
            if (beltSpeed>=9999)
            {
                //ベルトと接続されていない場合MK.IIIとして計算
                beltSpeed = 5;
            }

            // beltSpeed MK.I=1, MK.II=2, MK.III=5
            string beltLabel = (beltSpeed == 1) ? ". " : (beltSpeed == 2) ? ": " : "";
            //1分間の輸送量
            int beltCap = 6 * beltSpeed * 60;


            //1回の生産時間に対する設備補正
            //int speed = assemblerComponent.speed; //基本10,000 MK.I=7,500 MK.II=10,000 MK.III=15,000, 	Plane Smelter=20,000
            //1回の生産時間 設備補正なし
            int timeSpend = assemblerComponent.timeSpend; //timeSpend = recipeProto.TimeSpend * 10000 // 秒数 * 60 * 10000
            //int realTime = timeSpend / speed; // 秒数 * 60
            //1回の生産量or消費量
            int itemCount;
            int rItemCount = 0;
            int pItemCount = 0;
            float prRate = 1f;
            for (int k = 0; k < assemblerComponent.requireCounts.Length; k++)
            {
                rItemCount = Mathf.Max(rItemCount, assemblerComponent.requireCounts[k]);
                if (k < requireNumText.Length)
                {
                    float x = assemblerWindow.servingGroup.localPosition.x - 160f;
                    requireNumText[k].transform.localPosition = new Vector3(x + 50 * k, -30f, 0f);//38f
                    requireNumText[k].text = assemblerComponent.requireCounts[k].ToString();
                }
            }
            for (int k = 0; k < assemblerComponent.productCounts.Length; k++)
            {
                pItemCount = Mathf.Max(pItemCount, assemblerComponent.productCounts[k]);
                if (k < productNumText.Length)
                {
                    float x = assemblerWindow.recipeGroup.localPosition.x - 136f;
                    productNumText[k].transform.localPosition = new Vector3(x + 64 * k, -34f, 0f);//44f
                    productNumText[k].text = assemblerComponent.productCounts[k].ToString();
                }
            }
            
            //1台あたりの1分間の生産量or消費量 補正なしspeed=10,000
            itemCount = Mathf.Max(rItemCount, pItemCount);
            float perMin = itemCount * (60 / ((float)timeSpend / (60 * 10000)));

            //スタック数を考慮
            int stationPilerLevel = GameMain.history.stationPilerLevel;
            if (stationPilerLevel > 1)
            {
                if (pItemCount > 0)
                {
                    prRate = (float)rItemCount / (float)pItemCount;
                    if (prRate < 1f)
                    {
                        prRate = 1f;
                    }
                    if ((float)stationPilerLevel < prRate)
                    {
                        prRate = (float)stationPilerLevel;
                    }
                }
            }

            ItemProto itemProto = LDB.items.Select((int)assemblerWindow.factory.entityPool[assemblerComponent.entityId].protoId);
            if (itemProto != null)
            {
                string maxString;
                string maxStringStacked;
                string numStringFormat;
                //最大可能設置数
                float maxFacilities = beltCap / perMin;
                float maxFacilitiesStacked = beltCap * prRate / perMin;
                //実際の設置数
                ERecipeType recipeType = itemProto.prefabDesc.assemblerRecipeType;
                int cnt = FacilitiesCountInSameLine(assemblerWindow.factory, cargoPaths, assemblerComponent.recipeId, recipeType, out int a, out int b, out int c);
                if (recipeType == ERecipeType.Smelt)
                {
                    maxString = ((int)(maxFacilities)).ToString() + "/" + ((int)(maxFacilities / 2)).ToString();
                    maxStringStacked = ((int)(maxFacilitiesStacked)).ToString() + "/" + ((int)(maxFacilitiesStacked / 2)).ToString();
                    numStringFormat = "{0}/{1}";
                }
                else if (recipeType == ERecipeType.Assemble)
                {
                    maxString = ((int)(maxFacilities / 0.75)).ToString() + "/" + ((int)(maxFacilities)).ToString() + "/" + ((int)(maxFacilities / 1.5)).ToString();
                    maxStringStacked = ((int)(maxFacilitiesStacked / 0.75)).ToString() + "/" + ((int)(maxFacilitiesStacked)).ToString() + "/" + ((int)(maxFacilitiesStacked / 1.5)).ToString();
                    numStringFormat = "{0}/{1}/{2}";
                }
                else
                {
                    maxString = ((int)(maxFacilities)).ToString();
                    maxStringStacked = ((int)(maxFacilitiesStacked)).ToString();
                    numStringFormat = "{0}";
                }

                if (cnt > 0)
                {
                    string aStr = a > 0 ? a.ToString() : "--";
                    string bStr = b > 0 ? b.ToString() : "--";
                    string cStr = c > 0 ? c.ToString() : "--";
                    numText.text = string.Format(numStringFormat, aStr, bStr, cStr);
                }
                maxText.text = "max = " + beltLabel + maxString;
                if (stationPilerLevel > 1)
                {
                    maxStackedText.text =  "stack x" + prRate.ToString("0.0") + " = " + beltLabel + maxStringStacked;
                }

                //アイテムごとの最大値
                float speed = (float)assemblerComponent.speed / 10000f;
                for (int k = 0; k < assemblerComponent.requireCounts.Length; k++)
                {
                    if (k < requireText.Length)
                    {
                        float perMin2 = assemblerComponent.requireCounts[k] * (60 / ((float)timeSpend / (60 * 10000)));
                        float maxFacilities2 = beltCap / perMin2;

                        float x = assemblerWindow.servingGroup.localPosition.x - 164f;
                        requireText[k].transform.localPosition = new Vector3(x + 50 * k, 38f, 0f);//-30f
                        requireText[k].text = ((int)(maxFacilities2 / speed)).ToString();
                    }
                }
                for (int k = 0; k < assemblerComponent.productCounts.Length; k++)
                {
                    if (k < productText.Length)
                    {
                        float perMin2 = assemblerComponent.productCounts[k] * (60 / ((float)timeSpend / (60 * 10000)));
                        float maxFacilities2 = beltCap / perMin2;

                        float x = assemblerWindow.recipeGroup.localPosition.x - 138f;
                        productText[k].transform.localPosition = new Vector3(x + 64 * k, 44f, 0f);//-34f
                        productText[k].text = ((int)(maxFacilities2 / speed)).ToString();
                    }
                }
            }

            if (cargoPaths != null)
            {
                cargoPaths.Clear();
                cargoPaths = null;
            }
        }

        public static int FacilitiesCountInSameLine(PlanetFactory factory, HashSet<int> cargoPaths, int recipeId, ERecipeType recipeType, out int a, out int b, out int c)
        {
            a = 0;
            b = 0;
            c = 0;
            if (cargoPaths.Count == 0)
            {
                return 1;
            }
            int result = 0;
            FactorySystem fs = factory.factorySystem;
            HashSet<int> cargoPaths2 = new HashSet<int>(cargoPaths);

            for (int i = 1; i < fs.assemblerCursor; i++)
            {
                if (fs.assemblerPool[i].recipeId != recipeId)
                {
                    continue;
                }

                bool valid = true;
                for (int j = 0; j <= 11; j++)
                {
                    factory.ReadObjectConn(fs.assemblerPool[i].entityId, j, out bool isOutput, out int otherObjId, out int otherSlot);
                    if (otherObjId > 0 && factory.entityPool[otherObjId].inserterId > 0)
                    {
                        int insererId = factory.entityPool[otherObjId].inserterId;
                        InserterComponent ic = fs.inserterPool[insererId];
                        int beltId = isOutput ? factory.entityPool[ic.insertTarget].beltId : factory.entityPool[ic.pickTarget].beltId;
                        if (beltId <= 0)
                        {
                            continue;
                        }
                        CargoPath cargoPath = factory.cargoTraffic.GetCargoPath(factory.cargoTraffic.beltPool[beltId].segPathId);
                        if (cargoPaths.Contains(cargoPath.id))
                        {
                            cargoPaths2.Remove(cargoPath.id);
                        }
                        else
                        {
                            valid = false;
                            break;
                        }

                    }
                }

                if (valid && cargoPaths2.Count==0)
                {
                    result++;

                    int speed = fs.assemblerPool[i].speed;
                    if (recipeType == ERecipeType.Smelt)
                    {
                        if (speed == 10000)
                        {
                            a++;
                        }
                        else //20000
                        {
                            b++;
                        }

                    }
                    else if (recipeType == ERecipeType.Assemble)
                    {
                        if (speed == 7500)
                        {
                            a++;
                        }
                        else if (speed == 10000)
                        {
                            b++;
                        }
                        else //15000
                        {
                            c++;
                        }
                    }
                    else
                    {
                        a++;
                    }
                }
                cargoPaths2.UnionWith(cargoPaths);
            }

            cargoPaths2.Clear();
            cargoPaths2 = null;
            return result;
        }

        public static HashSet<int> GetConnectedCargoPaths(PlanetFactory factory, int assemblerEid, out int beltSpeed)
        {
            HashSet<int> cargoPaths = new HashSet<int>();
            beltSpeed = 9999;

            //EntityData e = factory.entityPool[assemblerEid];
            for (int j = 0; j <= 11; j++)
            {
                factory.ReadObjectConn(assemblerEid, j, out bool isOutput, out int otherObjId, out int otherSlot);
                if (otherObjId > 0 && factory.entityPool[otherObjId].inserterId>0)
                {
                    int insererId = factory.entityPool[otherObjId].inserterId;
                    InserterComponent ic = factory.factorySystem.inserterPool[insererId];
                    int beltId = isOutput ? factory.entityPool[ic.insertTarget].beltId : factory.entityPool[ic.pickTarget].beltId;
                    if (beltId <= 0)
                    {
                        continue;
                    }
                    CargoPath cargoPath = factory.cargoTraffic.GetCargoPath(factory.cargoTraffic.beltPool[beltId].segPathId);
                    if (cargoPaths.Contains(cargoPath.id))
                    {
                        continue;
                    }
                    beltSpeed = Mathf.Min(factory.cargoTraffic.beltPool[beltId].speed, beltSpeed);
                    cargoPaths.Add(cargoPath.id);
                    //if (cargoPath.outputPath != null)
                    //{
                    //    cargoPaths.Add(cargoPath.outputPath.id);
                    //}
                    //for (int l = 0; l < cargoPath.inputPaths.Count; l++)
                    //{
                    //    cargoPaths.Add(cargoPath.inputPaths[l]);
                    //}
                }
            }

            return cargoPaths;
        }

        static class Patch
        {
            internal static bool _initialized = false;

            public static void MakeUI()
            {
                UIAssemblerWindow assemblerWindow = UIRoot.instance.uiGame.assemblerWindow;
                Text powerText = assemblerWindow.powerText;
                Text stateText = assemblerWindow.stateText;
                float yDistance = stateText.rectTransform.localPosition.y - powerText.rectTransform.localPosition.y;
                yDistance -= yDistance > 0 ? 5 : -5;
                numText = Object.Instantiate<Text>(stateText, stateText.transform.parent);
                maxText = Object.Instantiate<Text>(stateText, stateText.transform.parent);
                maxStackedText = Object.Instantiate<Text>(stateText, stateText.transform.parent);
                Vector3 pos = numText.rectTransform.localPosition;
                pos.y += yDistance;
                numText.rectTransform.localPosition = pos;
                pos.y += yDistance;
                maxText.rectTransform.localPosition = pos;
                pos.y += yDistance;
                maxStackedText.rectTransform.localPosition = pos;
                pos.y += yDistance;
                numText.text = "";
                maxText.text = "";
                maxStackedText.text = "";
                maxText.color = new Color(0.58f, 0.58f, 0.58f, 0.65f);
                maxStackedText.color = new Color(0.58f, 0.58f, 0.58f, 0.65f);

                //move copy button
                RectTransform cpRect = assemblerWindow.copyButton.transform as RectTransform;
                cpRect.anchoredPosition = new Vector2(-20f, -146f);

                requireText = new Text[4];
                productText = new Text[2];
                requireNumText = new Text[4];
                productNumText = new Text[2];

                Text CreateCountText(string name_){
                    Text txt_ = Object.Instantiate<Text>(numText, numText.transform.parent);
                    txt_.gameObject.name = name_;
                    txt_.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    txt_.alignment = TextAnchor.MiddleLeft;
                    txt_.rectTransform.sizeDelta = new Vector2(24f, 20f);
                    return txt_;
                }
                for (int i = 0; i < requireText.Length; i++)
                {
                    requireText[i] = CreateCountText("require-text-" + (i + 1).ToString());
                }
                for (int i = 0; i < productText.Length; i++)
                {
                    productText[i] = CreateCountText("product-text-" + (i + 1).ToString());
                }
                for (int i = 0; i < requireNumText.Length; i++)
                {
                    requireNumText[i] = CreateCountText("require-num-text-" + (i + 1).ToString());
                }
                for (int i = 0; i < productNumText.Length; i++)
                {
                    productNumText[i] = CreateCountText("product-num-text-" + (i + 1).ToString());
                }

                //邪魔
                assemblerWindow.transferTipText.gameObject.SetActive(false);
            }


            [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
            public static void GameMain_Begin_Prefix()
            {
                if (!_initialized)
                {
                    _initialized = true;
                    MakeUI();
                }

            }

            [HarmonyPostfix, HarmonyPatch(typeof(UIAssemblerWindow), "_OnUpdate")]
            public static void UIAssemblerWindow_OnUpdate_Postfix(UIAssemblerWindow __instance)
            {
                int step = Time.frameCount % 60;
                if (_lastAssemblerId != __instance.assemblerId || step == 0)
                {
                    UpdateState(__instance);
                    _lastAssemblerId = __instance.assemblerId;
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(UIAssemblerWindow), "_OnClose")]
            public static void UIAssemblerWindow_OnClose_Postfix(UIAssemblerWindow __instance)
            {
                _lastAssemblerId = 0;
            }

        }


    }
}
