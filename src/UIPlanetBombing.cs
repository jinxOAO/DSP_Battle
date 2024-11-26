using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DSP_Battle
{
    public class UIPlanetBombing
    {
        public static GameObject guideButtonObj;
        public static GameObject purgeButtonObj;

        public static Button guideButton;
        public static Button purgeButton;

        public static Image guideButtonIcon;
        public static Image purgeButtonIcon;
        public static Image guideCircleImg;
        public static Image purgeCircleImg;
        public static GameObject guideBackGrayIconObj;
        public static GameObject purgeBackGrayIconObj;

        public static UIButton guideUIBtn;
        public static UIButton purgeUIBtn;

        public static Text guideCDText;
        public static Text purgeCDText;

        public static Color coolDownTextColor = new Color(1, 0.582f, 0.323f, 0.8113f);
        public static Color coolDownIconDisabledColor = new Color(0.7f, 0.7f, 0.7f, 0.1961f);
        public static Color oriDisabledColor = new Color(1, 1, 1, 0.0157f);

        public static void InitAll()
        {
            if(guideButtonObj == null)
            {
                GameObject parentObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Globe Panel");
                GameObject oriButtonObj = parentObj.transform.Find("button-1-bg").gameObject;
                GameObject oriSizeObj = parentObj.transform.Find("button-2-bg").gameObject;
                GameObject oriSize2Obj = oriSizeObj.transform.Find("button-2/icon").gameObject;
                if (parentObj == null || oriButtonObj == null || oriSizeObj == null || oriSize2Obj == null)
                    return;

                float size1 = oriSizeObj.GetComponent<RectTransform>().sizeDelta.x;
                float size2 = oriSize2Obj.GetComponent<RectTransform>().sizeDelta.x;

                UIButton oriUIBtn = oriButtonObj.GetComponent<UIButton>();
                guideButtonObj = GameObject.Instantiate(oriButtonObj, parentObj.transform);
                guideButtonObj.name = "button-guide-bombing";
                guideButtonObj.transform.localScale = Vector3.one;
                guideButtonObj.transform.localPosition = new Vector3(48, 237, 0);
                GameObject.DestroyImmediate(guideButtonObj.GetComponent<UIButton>());
                guideUIBtn = guideButtonObj.AddComponent<UIButton>();
                guideButton = guideButtonObj.GetComponent<Button>();
                guideButton.onClick.RemoveAllListeners();
                guideButton.onClick.AddListener(() => { OnGuideButtonClick(); });
                guideButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(size1, size1);
                guideUIBtn.tips.tipTitle = "引导太阳轰炸标题".Translate();
                guideUIBtn.tips.tipText = "引导太阳轰炸描述".Translate();
                guideUIBtn.tips.corner = oriUIBtn.tips.corner;
                guideUIBtn.tips.offset = oriUIBtn.tips.offset;
                guideUIBtn.tips.width = (int)(oriUIBtn.tips.width * 1.5f);
                guideUIBtn.tips.delay = oriUIBtn.tips.delay;
                GameObject guideButtonCenterObj = guideButtonObj.transform.Find("button-1").gameObject;
                GameObject guideButtonIconObj = guideButtonCenterObj.transform.Find("icon").gameObject;
                guideButtonIconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(size2, size2);
                guideButtonIcon = guideButtonIconObj.GetComponent<Image>();
                guideButtonIcon.sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/dispatch-fleet");
                guideButtonIcon.type = Image.Type.Filled;
                guideBackGrayIconObj = GameObject.Instantiate(guideButtonIconObj, guideButtonCenterObj.transform); // 创建一个灰色底图，用于在冷却的时候显示
                guideBackGrayIconObj.transform.SetAsFirstSibling();
                guideBackGrayIconObj.transform.localScale= Vector3.one;
                guideBackGrayIconObj.transform.localPosition = Vector3.zero;
                guideBackGrayIconObj.GetComponent<Image>().color = oriDisabledColor;
                guideCircleImg = guideButtonObj.transform.Find("circle").GetComponent<Image>();
                guideCircleImg.type = Image.Type.Filled;

                purgeButtonObj = GameObject.Instantiate(oriButtonObj, parentObj.transform);
                purgeButtonObj.name = "button-purge-bombing";
                purgeButtonObj.transform.localScale = Vector3.one;
                purgeButtonObj.transform.localPosition = new Vector3(125, 237, 0);
                GameObject.DestroyImmediate(purgeButtonObj.GetComponent<UIButton>());
                purgeUIBtn = purgeButtonObj.AddComponent<UIButton>();
                purgeButton = purgeButtonObj.GetComponent<Button>();
                purgeButton.onClick.RemoveAllListeners();
                purgeButton.onClick.AddListener(() => { OnPurgeButtonClick(); });
                purgeButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(size1, size1);
                purgeUIBtn.tips.tipTitle = "呼叫行星清洗标题".Translate();
                purgeUIBtn.tips.tipText = "呼叫行星清洗描述".Translate();
                purgeUIBtn.tips.corner = oriUIBtn.tips.corner;
                purgeUIBtn.tips.offset = oriUIBtn.tips.offset;
                purgeUIBtn.tips.width = oriUIBtn.tips.width;
                purgeUIBtn.tips.delay = oriUIBtn.tips.delay;
                GameObject purgeButtonCenterObj = purgeButtonObj.transform.Find("button-1").gameObject;
                GameObject purgeButtonIconObj = purgeButtonCenterObj.transform.Find("icon").gameObject;
                purgeButtonIconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(size2, size2);
                purgeButtonIcon = purgeButtonIconObj.GetComponent<Image>();
                purgeButtonIcon.sprite = Resources.Load<Sprite>("ui/textures/sprites/sci-fi/track-btn-3");
                purgeButtonIcon.type = Image.Type.Filled;
                purgeBackGrayIconObj = GameObject.Instantiate(purgeButtonIconObj, purgeButtonCenterObj.transform); // 创建一个灰色底图，用于在冷却的时候显示
                purgeBackGrayIconObj.transform.SetAsFirstSibling();
                purgeBackGrayIconObj.transform.localScale = Vector3.one;
                purgeBackGrayIconObj.transform.localPosition = Vector3.zero;
                purgeBackGrayIconObj.GetComponent<Image>().color = oriDisabledColor;
                purgeCircleImg = purgeButtonObj.transform.Find("circle").GetComponent<Image>();
                purgeCircleImg.type = Image.Type.Filled;

                // 一起修改UIButton的transitions
                guideUIBtn.transitions = new UIButton.Transition[2];
                purgeUIBtn.transitions = new UIButton.Transition[2];
                for (int i = 0; i < 2; i++)
                {
                    guideUIBtn.transitions[i] = new UIButton.Transition();
                    guideUIBtn.transitions[i].damp = oriUIBtn.transitions[i].damp;
                    guideUIBtn.transitions[i].mouseoverSize = oriUIBtn.transitions[i].mouseoverSize;
                    guideUIBtn.transitions[i].pressedSize = oriUIBtn.transitions[i].pressedSize;
                    guideUIBtn.transitions[i].normalColor = oriUIBtn.transitions[i].normalColor;
                    guideUIBtn.transitions[i].mouseoverColor = oriUIBtn.transitions[i].mouseoverColor;
                    guideUIBtn.transitions[i].pressedColor = oriUIBtn.transitions[i].pressedColor;
                    guideUIBtn.transitions[i].alphaOnly = oriUIBtn.transitions[i].alphaOnly;
                    guideUIBtn.transitions[i].highlightSizeMultiplier = oriUIBtn.transitions[i].highlightSizeMultiplier;
                    guideUIBtn.transitions[i].highlightColorMultiplier = oriUIBtn.transitions[i].highlightColorMultiplier;
                    guideUIBtn.transitions[i].highlightAlphaMultiplier = oriUIBtn.transitions[i].highlightAlphaMultiplier;
                    guideUIBtn.transitions[i].highlightColorOverride = oriUIBtn.transitions[i].highlightColorOverride;

                    purgeUIBtn.transitions[i] = new UIButton.Transition();
                    purgeUIBtn.transitions[i].damp = oriUIBtn.transitions[i].damp;
                    purgeUIBtn.transitions[i].mouseoverSize = oriUIBtn.transitions[i].mouseoverSize;
                    purgeUIBtn.transitions[i].pressedSize = oriUIBtn.transitions[i].pressedSize;
                    purgeUIBtn.transitions[i].normalColor = oriUIBtn.transitions[i].normalColor;
                    purgeUIBtn.transitions[i].mouseoverColor = oriUIBtn.transitions[i].mouseoverColor;
                    purgeUIBtn.transitions[i].pressedColor = oriUIBtn.transitions[i].pressedColor;
                    purgeUIBtn.transitions[i].alphaOnly = oriUIBtn.transitions[i].alphaOnly;
                    purgeUIBtn.transitions[i].highlightSizeMultiplier = oriUIBtn.transitions[i].highlightSizeMultiplier;
                    purgeUIBtn.transitions[i].highlightColorMultiplier = oriUIBtn.transitions[i].highlightColorMultiplier;
                    purgeUIBtn.transitions[i].highlightAlphaMultiplier = oriUIBtn.transitions[i].highlightAlphaMultiplier;
                    purgeUIBtn.transitions[i].highlightColorOverride = oriUIBtn.transitions[i].highlightColorOverride;

                    if (i == 0)
                    {
                        guideUIBtn.transitions[i].target = guideButtonCenterObj.GetComponent<Graphic>();
                        purgeUIBtn.transitions[i].target = purgeButtonCenterObj.GetComponent<Graphic>();
                        guideUIBtn.transitions[i].disabledColor = oriUIBtn.transitions[i].disabledColor;
                        purgeUIBtn.transitions[i].disabledColor = oriUIBtn.transitions[i].disabledColor;
                    }
                    else if (i == 1)
                    {
                        guideUIBtn.transitions[i].target = guideButtonIconObj.GetComponent<Graphic>();
                        purgeUIBtn.transitions[i].target = purgeButtonIconObj.GetComponent<Graphic>();
                        guideUIBtn.transitions[i].disabledColor = coolDownIconDisabledColor; // 这里的disabledColor用稍稍亮一点的颜色
                        purgeUIBtn.transitions[i].disabledColor = coolDownIconDisabledColor;
                    }
                }

                // 创建冷却时间文本
                GameObject oriTextObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Globe Panel/globe/position-text/position-text-news");
                if( oriTextObj != null ) 
                {
                    GameObject guideCDTextObj = GameObject.Instantiate(oriTextObj, guideButtonObj.transform);
                    guideCDTextObj.name = "cooldown";
                    guideCDTextObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                    guideCDTextObj.transform.localScale = Vector3.one;
                    guideCDTextObj.transform.localPosition = new Vector3(0, 0, 0);
                    guideCDText = guideCDTextObj.GetComponent<Text>();
                    guideCDText.alignment = TextAnchor.MiddleCenter;
                    guideCDText.color = coolDownTextColor;
                    guideCDText.fontSize = (int)(guideCDText.fontSize * 1.25);
                    guideCDText.text = "";

                    GameObject purgeCDTextObj = GameObject.Instantiate(oriTextObj, purgeButtonObj.transform);
                    purgeCDTextObj.name = "cooldown";
                    purgeCDTextObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                    purgeCDTextObj.transform.localScale = Vector3.one;
                    purgeCDTextObj.transform.localPosition = new Vector3(0, 0, 0);
                    purgeCDText = purgeCDTextObj.GetComponent<Text>();
                    purgeCDText.alignment = TextAnchor.MiddleCenter;
                    purgeCDText.color = coolDownTextColor;
                    purgeCDText.fontSize = (int)(guideCDText.fontSize * 1.25);
                    purgeCDText.text = "";
                }
            }
        }


        public static void OnUpdate()
        {
            if (purgeButtonObj == null || GameMain.history == null)
                return;

            if (Rank.rank >= 7) // 达到征服者才可以呼叫星球清洗
            {
                purgeButtonObj.SetActive(true);

                if (PlanetBombing.purgeReady)
                {
                    purgeButton.enabled = true;
                    purgeBackGrayIconObj.SetActive(false);
                    purgeCircleImg.fillAmount = 1;
                    purgeButtonIcon.fillAmount = 1;
                    purgeCDText.text = "";
                }
                else
                {
                    purgeButton.enabled = false;
                    purgeBackGrayIconObj.SetActive(true);
                    int coolDownFrame = PlanetBombing.purgeCoolDown;
                    purgeCircleImg.fillAmount = 1 - 1.0f * PlanetBombing.purgeCoolDown / PlanetBombing.purgeMaxCD;
                    purgeButtonIcon.fillAmount = 1 - 1.0f * PlanetBombing.purgeCoolDown / PlanetBombing.purgeMaxCD;
                    purgeCDText.text = string.Format("{0:D2} : {1:D2}", coolDownFrame / 60 / 60, coolDownFrame / 60 % 60);
                }
            }
            else
            {
                purgeButtonObj.SetActive(false);
            }

            if (GameMain.history.TechUnlocked(1997)) // 科技解锁才能使用引导太阳轰炸
            {
                guideButtonObj.SetActive(true);

                if (PlanetBombing.guideReady)
                {
                    guideButton.enabled = true;
                    guideBackGrayIconObj.SetActive(false);
                    guideCircleImg.fillAmount = 1;
                    guideButtonIcon.fillAmount = 1;
                    guideCDText.text = "";
                }
                else
                {
                    guideButton.enabled = false;
                    guideBackGrayIconObj.SetActive(true);
                    int coolDownFrame = PlanetBombing.guideCoolDown;
                    guideCircleImg.fillAmount = 1 - 1.0f * PlanetBombing.guideCoolDown / PlanetBombing.guideMaxCD;
                    guideButtonIcon.fillAmount = 1 - 1.0f * PlanetBombing.guideCoolDown / PlanetBombing.guideMaxCD;
                    guideCDText.text = string.Format("{0:D2} : {1:D2}", coolDownFrame / 60 / 60, coolDownFrame / 60 % 60);

                }
            }
            else
            {
                guideButtonObj.SetActive(false);
            }
        }


        public static void OnGuideButtonClick()
        {
            if(GameMain.localPlanet == null)
            {
                UIRealtimeTip.Popup("只能在行星上启动".Translate());
                return;
            }
            UIRealtimeTip.Popup("启动行星清洗警告".Translate());
            PlanetBombing.LaunchGuideBombing();
        }

        public static void OnPurgeButtonClick()
        {
            if (GameMain.localPlanet == null)
            {
                UIRealtimeTip.Popup("只能在行星上启动".Translate());
                return;
            }
            if (SkillPoints.UnusedPoints() > 0)
            {
                SkillPoints.totalPoints--;
                UIRealtimeTip.Popup("启动太阳轰炸警告".Translate());
                PlanetBombing.LaunchPurgeBombing();
            }
            else
            {
                UIRealtimeTip.Popup("授权点不足警告".Translate());
            }
        }
    }
}
