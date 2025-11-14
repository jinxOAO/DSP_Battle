using MoreMegaStructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Random;

namespace DSP_Battle
{
    public class StarCannonAutoFire
    {
        public static Sprite checkboxOnSprite;
        public static Sprite checkboxOffSprite;
        public static GameObject autoFireObj = null;
        public static Image autoFireCheckboxImg;

        public static bool autoFireEnabled = false;

        public static void InitUIWhenLoad()
        {
            if (autoFireObj != null)
                return;

            GameObject parentTopBar = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/screen/star-cannon-state-text");
            if (parentTopBar == null)
            {
                Utils.Log("Fail to find parent UI.");
                return;
            }
            GameObject oriCheckbox = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/swarm/display-group/display-toggle-2/checkbox-editor");

            autoFireObj = GameObject.Instantiate(oriCheckbox, parentTopBar.transform);
            autoFireObj.transform.localScale = Vector3.one;
            autoFireObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            autoFireObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            autoFireObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-70, -30, 0);
            autoFireCheckboxImg = autoFireObj.GetComponent<Image>();

            checkboxOnSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/checkbox-on");
            checkboxOffSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/checkbox-off");

            GameObject autoFireTextObj = autoFireObj.transform.Find("in-editor-text").gameObject;
            autoFireTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(90, 0, 0);
            autoFireTextObj.GetComponent<Text>().color = new Color(1, 1, 1, 0.811f);
            autoFireTextObj.GetComponent<Localizer>().stringKey = "恒星炮自动开火";
            autoFireObj.GetComponent<UIButton>().tips.tipTitle = "恒星炮自动开火";
            autoFireObj.GetComponent<UIButton>().tips.tipText = "恒星炮自动开火说明";
            autoFireObj.GetComponent<UIButton>().tips.corner = 3;
            autoFireObj.GetComponent<UIButton>().tips.width = 266;


            autoFireObj.GetComponent<Button>().onClick.RemoveAllListeners();
            autoFireObj.GetComponent<Button>().onClick.AddListener(() => { OnAutoFireCheckboxClick(); });
            autoFireObj.SetActive(false);
            autoFireObj.SetActive(true);
        }

        public static void OnAutoFireCheckboxClick()
        {
            autoFireEnabled = !autoFireEnabled;
            RefreshUI();
        }

        public static void RefreshUI()
        {
            if(autoFireObj == null)
            {
                return;
            }

            if (autoFireEnabled)
                autoFireCheckboxImg.sprite = checkboxOnSprite;
            else
                autoFireCheckboxImg.sprite = checkboxOffSprite;
        }

        public static void CheckAutoFire()
        {
            if (autoFireEnabled)
            {
                MoreMegaStructure.StarCannon.starCannonStarIndex = MoreMegaStructure.MoreMegaStructure.GetStarCannonBuiltIndex();
                MoreMegaStructure.StarCannon.RefreshStarCannonProperties();
                if (MoreMegaStructure.StarCannon.starCannonStarIndex < 0)
                {
                    return;
                }
                if (MoreMegaStructure.StarCannon.starCannonLevel <= 0)
                {
                    return;
                }
                if (MoreMegaStructure.StarCannon.state == EStarCannonState.Standby) // 准备开火
                {
                    bool canFire = false;

                    if (AssaultController.assaultHives.Count > 0)
                    {
                        MoreMegaStructure.StarCannon.currentTargetStarIndex = AssaultController.assaultHives[0].starIndex;
                        if (MoreMegaStructure.StarCannon.currentTargetStarIndex == MoreMegaStructure.StarCannon.starCannonStarIndex)
                        {
                            MoreMegaStructure.StarCannon.currentTargetStarIndex = 0;
                            return;
                        }
                        SkillTarget currentTarget = MoreMegaStructure.StarCannon.SearchNextTarget();
                        MoreMegaStructure.StarCannon.priorTargetHiveOriAstroId = currentTarget.astroId;
                        MoreMegaStructure.StarCannon.currentTargetIds = new List<int>();

                        if (MoreMegaStructure.StarCannon.CheckAndSearchAllTargets())
                            canFire = true;
                    }

                    if (canFire)
                    {
                        MoreMegaStructure.StarCannon.StartAiming();
                    }
                }
            }
        }


        public static void Export(BinaryWriter w)
        {
            w.Write(autoFireEnabled ? 1 : 0);
        }

        public static void Import(BinaryReader r)
        {
            InitUIWhenLoad();
            if(Configs.versionWhenImporting <30251114)
            {
                IntoOtherSave();
            }
            else
            {
                autoFireEnabled = r.ReadInt32() > 0;
            }
            RefreshUI();
        }


        public static void IntoOtherSave()
        {
            InitUIWhenLoad();
            autoFireEnabled = false;
            RefreshUI();
        }
    }
}
