﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CameraMod.Button;
using CameraMod.Button.Buttons;
using CameraMod.Camera.Comps;
using Cinemachine;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

//using BepInEx;

#pragma warning disable CS0618
namespace CameraMod.Camera {
    public enum UpdateMode {
        Patch,
        Update,
        LateUpdate
    }

    public class CameraController : MonoBehaviour {
        public enum TPVModes {
            BACK,
            FRONT
        }

        public static CameraController Instance;

        public static UpdateMode UpdateMode = UpdateMode.Patch;
        public static bool bindEnabled = true;
        public GameObject CameraTablet;
        public GameObject FirstPersonCameraGO;
        public GameObject ThirdPersonCameraGO;
        public GameObject CMVirtualCameraGO;
        public GameObject FakeWebCam;
        public GameObject TabletCameraGO;
        public GameObject MainPage;
        public GameObject MiscPage;
        public GameObject LeftGrabCol;
        public GameObject RightGrabCol;
        public GameObject CameraFollower;
        public GameObject TPVBodyFollower;
        public GameObject ColorScreenGO;
        private List<BaseButton> Buttons = new List<BaseButton>();
        public List<BaseButton> ColorButtons = new List<BaseButton>();
        public List<Material> ScreenMats = new List<Material>();
        public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        public UnityEngine.Camera TabletCamera;
        public UnityEngine.Camera FirstPersonCamera;
        public UnityEngine.Camera ThirdPersonCamera;
        public CinemachineVirtualCamera CMVirtualCamera;

        public Text FovText;
        public Text NearClipText;
        public Text ColorScreenText;
        public Text MinDistText;
        public Text SpeedText;
        public Text SmoothText;
        public Text TPText;
        public Text TPRotText;

        public bool followheadrot = true;
        public bool isFaceCamera;
        public bool tpv;
        public bool fpv = true;
        public bool fp;
        public bool openedurl;
        public float minDist = 2f;
        public float fpspeed = 0.01f;
        public float smoothing = 0.07f;
        public TPVModes TPVMode = TPVModes.BACK;
        private float dist;
        private bool init;
        private Vector3 targetPosition;
        private Vector3 velocity = Vector3.zero;

        private void Awake() {
            Instance = this;
        }

        private void Update() {
            //void LateUpdate() {
            if (UpdateMode == UpdateMode.Update) AnUpdate();
        }

        private void LateUpdate() {
            if (UpdateMode == UpdateMode.LateUpdate) AnUpdate();
        }

        public void SetNearClip(float val) {
            var tabletCam = TabletCamera;
            tabletCam.nearClipPlane = val;
            if (tabletCam.nearClipPlane < 0.01) {
                tabletCam.nearClipPlane = 1f;
                ThirdPersonCamera.nearClipPlane = 1f;
            }
            if (tabletCam.nearClipPlane > 1.0) {
                tabletCam.nearClipPlane = 0.01f;
                ThirdPersonCamera.nearClipPlane = 0.01f;
            }

            ThirdPersonCamera.nearClipPlane = TabletCamera.nearClipPlane;
            NearClipText.text = TabletCamera.nearClipPlane.ToString("#.##");
        }

        public void ChangeNearClip(float diff) {
            SetNearClip(TabletCamera.nearClipPlane + diff);
            PlayerPrefs.SetFloat("CameraNearClip", TabletCamera.nearClipPlane);
        }

        private static float minSmoothing = 0.01f;
        private static float maxSmoothing = 1f;
        public void SetSmoothing(float val) {
            smoothing = val;
            if (smoothing < minSmoothing) smoothing = minSmoothing;
            if (smoothing > maxSmoothing) smoothing = maxSmoothing;
            SmoothText.text = smoothing.ToString("#.##");
        }
        public void ChangeSmoothing(float change) {
            SetSmoothing(smoothing + change);
            PlayerPrefs.SetFloat("CameraSmoothing", smoothing);
        }
        public void ChangeFov(float difference) {
            var controller = Instance;

            var max = 130;
            var min = 20;

            var newFov = Mathf.Clamp(controller.TabletCamera.fieldOfView + difference, min, max);
            SetFov(newFov);
            PlayerPrefs.SetInt("CameraFov", (int) newFov);
        }

        public void SetFov(float fov) {
            var newFov = fov;
            TabletCamera.fieldOfView = newFov;
            ThirdPersonCamera.fieldOfView = newFov;

            ThirdPersonCamera.fieldOfView = TabletCamera.fieldOfView;
            FovText.text = TabletCamera.fieldOfView.ToString("#.##");
        }

        public HashSet<string> bindAliases = new HashSet<string>();

        public bool BindPressed() {
            var inputManager = InputManager.instance;
            foreach (var bindAlias in bindAliases) {
                if (bindAlias == "RP" && inputManager.RightPrimaryButton)
                    return true;
                if (bindAlias == "LP" && inputManager.LeftPrimaryButton)
                    return true;
                if (bindAlias == "RS" && inputManager.RightSecondaryButton)
                    return true;
                if (bindAlias == "LS" && inputManager.LeftSecondaryButton)
                    return true;
            }

            return false;
        } 
        
        public void Init() {
            var tagger = GorillaTagger.Instance;
            
            gameObject.AddComponent<InputManager>().gameObject.AddComponent<UI>();
            var assetsPath = Assembly.GetExecutingAssembly().GetName().Name + ".Camera.Assets";
            Debug.Log(assetsPath);
            ColorScreenGO = LoadBundle("ColorScreen", assetsPath + ".colorscreen");
            CameraTablet = LoadBundle("CameraTablet", assetsPath + ".yizzicam");
            FirstPersonCameraGO = tagger.mainCamera;

            ThirdPersonCameraGO = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
            CMVirtualCameraGO = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera/CM vcam1");
            TPVBodyFollower = tagger.bodyCollider.gameObject;
            CMVirtualCamera = CMVirtualCameraGO.GetComponent<CinemachineVirtualCamera>();
            FirstPersonCamera = FirstPersonCameraGO.GetComponent<UnityEngine.Camera>();


            ThirdPersonCamera = ThirdPersonCameraGO.GetComponent<UnityEngine.Camera>();

            CameraTablet.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            CameraFollower =
                GameObject.Find(
                    "Player Objects/Player VR Controller/GorillaPlayer/TurnParent/Main Camera/Camera Follower");


            TabletCameraGO = GameObject.Find("CameraTablet(Clone)/Camera");
            TabletCamera = TabletCameraGO.GetComponent<UnityEngine.Camera>();

            FakeWebCam = GameObject.Find("CameraTablet(Clone)/FakeCamera");
            LeftGrabCol = GameObject.Find("CameraTablet(Clone)/LeftGrabCol");
            RightGrabCol = GameObject.Find("CameraTablet(Clone)/RightGrabCol");
            LeftGrabCol.AddComponent<LeftGrabTrigger>();
            RightGrabCol.AddComponent<RightGrabTrigger>();
            MainPage = GameObject.Find("CameraTablet(Clone)/MainPage");
            MiscPage = GameObject.Find("CameraTablet(Clone)/MiscPage");
            FovText = GameObject.Find("CameraTablet(Clone)/MainPage/Canvas/FovValueText").GetComponent<Text>();
            SmoothText = GameObject.Find("CameraTablet(Clone)/MainPage/Canvas/SmoothingValueText").GetComponent<Text>();
            NearClipText = GameObject.Find("CameraTablet(Clone)/MainPage/Canvas/NearClipValueText")
                .GetComponent<Text>();
            MinDistText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/MinDistValueText").GetComponent<Text>();
            SpeedText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/SpeedValueText").GetComponent<Text>();
            TPText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/TPText").GetComponent<Text>();
            TPRotText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/TPRotText").GetComponent<Text>();
            
            RegisterButtons();
            
            CMVirtualCamera.enabled = false;
            ThirdPersonCameraGO.transform.SetParent(CameraTablet.transform, true);
            CameraTablet.transform.position = new Vector3(-65, 12, -82);
            ThirdPersonCameraGO.transform.position = TabletCamera.transform.position;
            ThirdPersonCameraGO.transform.rotation = TabletCamera.transform.rotation;
            CameraTablet.transform.Rotate(0, 180, 0);

            ColorScreenText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/ColorScreenText")
                .GetComponent<Text>();
            
            
            void SetColor(UnityEngine.Color color) {
                foreach (var mat in ScreenMats) mat.color = color;
            }
            
            ColorButtons.Add(Button("ColorScreen(Clone)/Stuff/RedButton", () => {
                SetColor(Color.red);
            }));
            ColorButtons.Add(Button("ColorScreen(Clone)/Stuff/GreenButton", () => {
                SetColor(Color.green);
            }));
            ColorButtons.Add(Button("ColorScreen(Clone)/Stuff/BlueButton", () => {
                SetColor(Color.blue);
            }));
            
            ScreenMats.Add(GameObject.Find("ColorScreen(Clone)/Screen1").GetComponent<MeshRenderer>().material);
            ScreenMats.Add(GameObject.Find("ColorScreen(Clone)/Screen2").GetComponent<MeshRenderer>().material);
            ScreenMats.Add(GameObject.Find("ColorScreen(Clone)/Screen3").GetComponent<MeshRenderer>().material);

            meshRenderers.Add(GameObject.Find("CameraTablet(Clone)/FakeCamera").GetComponent<MeshRenderer>());
            meshRenderers.Add(GameObject.Find("CameraTablet(Clone)/Tablet").GetComponent<MeshRenderer>());
            meshRenderers.Add(GameObject.Find("CameraTablet(Clone)/Handle").GetComponent<MeshRenderer>());
            meshRenderers.Add(GameObject.Find("CameraTablet(Clone)/Handle2").GetComponent<MeshRenderer>());

            ColorScreenGO.transform.position = new Vector3(-54.3f, 16.21f, -122.96f);
            ColorScreenGO.transform.Rotate(0, 30, 0);
            ColorScreenGO.SetActive(false);
            MiscPage.SetActive(false);
            ThirdPersonCamera.nearClipPlane = 0.1f;
            TabletCamera.nearClipPlane = 0.1f;
            FakeWebCam.transform.Rotate(-180, 180, 0);
            init = true;
            
            var fov = PlayerPrefs.GetInt("CameraFov", 100);
            SetFov(fov);
            
            var smoothing = PlayerPrefs.GetFloat("CameraSmoothing", 0.07f);
            SetSmoothing(smoothing);

            foreach (var bindAlias in Configs.controls.CurrentSettings.activateBind.Split(" ")) {
                bindAliases.Add(bindAlias);
            }
            Configs.controls.Changed += (cfg) => {
                bindAliases.Clear();
                foreach (var bindAlias in cfg.activateBind.Split(" ")) {
                    bindAliases.Add(bindAlias);
                }
            };
        }
        
        public void Flip() {
            isFaceCamera = !isFaceCamera;
            ThirdPersonCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
            TabletCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
            FakeWebCam.transform.Rotate(-180f, 180f, 0.0f);
        }
        
        public void RegisterButtons() {
            AddTabletButton("MainPage/MiscButton", () => {
                MainPage.SetActive(false);
                MiscPage.SetActive(true);
            });
            
            AddTabletButton("MainPage/FPVButton", () => {
                if (isFaceCamera) {
                    Flip();
                }

                fp = false;
                fpv = true;
            });
            
            AddHoldableButton("MainPage/SmoothingDownButton", () => ChangeSmoothing(-0.01f));
            AddHoldableButton("MainPage/SmoothingUpButton", () => ChangeSmoothing(0.01f));
            
            AddTabletButton("MainPage/FovUP", () => ChangeFov(5));
            AddTabletButton("MainPage/FovDown", () => ChangeFov(-5));
            
            AddTabletButton("MainPage/NearClipUp", () => ChangeNearClip(0.01f));
            AddTabletButton("MainPage/NearClipDown", () => ChangeNearClip(-0.01f));
            
            AddTabletButton("MainPage/FlipCamButton", () => {
                Flip();
            });
            
            AddTabletButton("MainPage/FPButton", () => fp = !fp);
            
            AddTabletButton("MainPage/ControlsButton", () => {
                if (!openedurl) {
                    Application.OpenURL("https://github.com/Yizzii/YizziCamModV2#controls");
                    openedurl = true;
                }
            });
            AddTabletButton("MiscPage/BackButton", () => {
                MainPage.SetActive(true);
                MiscPage.SetActive(false);
            });
            AddTabletButton("MainPage/TPVButton", () => {
                if (TPVMode == TPVModes.BACK) {
                    if (isFaceCamera) {
                        Flip();
                    }
                } else if (TPVMode == TPVModes.FRONT) {
                    if (!isFaceCamera) {
                        Flip();
                    }
                }

                fp = false;
                fpv = false;
                tpv = true;
            });
            
            AddTabletButton("MiscPage/MinDistDownButton", () => {
                minDist -= 0.1f;
                if (minDist < 1) minDist = 1;
                MinDistText.text = minDist.ToString("#.##");
            });
            AddTabletButton("MiscPage/MinDistUpButton", () => {
                minDist += 0.1f;
                if (minDist > 10) minDist = 10;
                MinDistText.text = minDist.ToString("#.##");
            });
            AddTabletButton("MiscPage/SpeedUpButton", () => {
                fpspeed += 0.01f;
                if (fpspeed > 0.1) fpspeed = 0.1f;
                SpeedText.text = fpspeed.ToString("#.##");
            });
            AddTabletButton("MiscPage/SpeedDownButton", () => {
                fpspeed -= 0.01f;
                if (fpspeed < 0.01) fpspeed = 0.01f;
                SpeedText.text = fpspeed.ToString("#.##");
            });
            AddTabletButton("MiscPage/TPModeDownButton", () => {
                if (TPVMode == TPVModes.BACK)
                    TPVMode = TPVModes.FRONT;
                else
                    TPVMode = TPVModes.BACK;
                TPText.text = TPVMode.ToString();
            });
            AddTabletButton("MiscPage/TPModeUpButton", () => {
                if (TPVMode == TPVModes.BACK)
                    TPVMode = TPVModes.FRONT;
                else
                    TPVMode = TPVModes.BACK;
                TPText.text = TPVMode.ToString();
            });
            AddTabletButton("MiscPage/TPRotButton", () => {
                followheadrot = !followheadrot;
                TPRotText.text = followheadrot.ToString().ToUpper();
            });
            AddTabletButton("MiscPage/TPRotButton1", () => {
                followheadrot = !followheadrot;
                TPRotText.text = followheadrot.ToString().ToUpper();
            });
            
            AddTabletButton("MiscPage/GreenScreenButton", () => {
                ColorScreenGO.active = !ColorScreenGO.active;
                if (ColorScreenGO.active)
                    ColorScreenText.text = "(ENABLED)";
                else
                    ColorScreenText.text = "(DISABLED)";
            });
        }

        public ClickButton Button(string buttonPath, Action onClick) {
            var button = GameObject.Find(buttonPath)
                .AddComponent<ClickButton>();
            button.OnClick(onClick);
            return button;
        }
        public void AddTabletButton(string relativeButtonPath, Action onClick) {
            Buttons.Add(Button("CameraTablet(Clone)/" + relativeButtonPath, onClick));
        }
        public void AddHoldableButton(string buttonPath, Action onClick) {
            Buttons.Add(
                GameObject.Find("CameraTablet(Clone)/"+buttonPath)
                    .AddComponent<HoldableButton>()
                    .OnClick(onClick)
            );
        }

        public void SetTabletVisibility(bool visible) {
            foreach (var mr in meshRenderers) mr.enabled = visible;
        }

        public void AnUpdate() {
            if (!init) return;

            if (fpv) {
                if (MainPage.active) {
                    SetTabletVisibility(false);
                    MainPage.active = false;
                }

                CameraTablet.transform.position = CameraFollower.transform.position;
                CameraTablet.transform.rotation = Quaternion.Lerp(CameraTablet.transform.rotation,
                    CameraFollower.transform.rotation, smoothing);
            }

            if (bindEnabled && BindPressed() && CameraTablet.transform.parent == null) {
                fp = false;
                fpv = false;
                tpv = false;
                if (!MainPage.active) {
                    foreach (var btns in Buttons) btns.gameObject.SetActive(true);
                    SetTabletVisibility(true);

                    MainPage.active = true;
                }

                var headTransform = Player.Instance.headCollider.transform;
                CameraTablet.transform.position = headTransform.position + headTransform.forward;
                CameraTablet.transform.LookAt(headTransform.position);
                CameraTablet.transform.Rotate(0f, -180f, 0f);
            }

            if (fp) {
                CameraTablet.transform.LookAt(2f * CameraTablet.transform.position - CameraFollower.transform.position);
                if (!isFaceCamera) {
                    Flip();
                }

                dist = Vector3.Distance(CameraFollower.transform.position, CameraTablet.transform.position);
                if (dist > minDist)
                    CameraTablet.transform.position = Vector3.Lerp(CameraTablet.transform.position,
                        CameraFollower.transform.position, fpspeed);
            }

            if (tpv) {
                if (MainPage.active) {
                    SetTabletVisibility(false);
                    MainPage.active = false;
                }

                if (TPVMode == TPVModes.BACK) {
                    if (followheadrot)
                        targetPosition = CameraFollower.transform.TransformPoint(new Vector3(0.3f, 0.1f, -1.5f));
                    else
                        targetPosition = TPVBodyFollower.transform.TransformPoint(new Vector3(0.3f, 0.1f, -1.5f));
                    CameraTablet.transform.position = Vector3.SmoothDamp(CameraTablet.transform.position,
                        targetPosition, ref velocity, 0.1f);
                    CameraTablet.transform.LookAt(CameraFollower.transform.position);
                } else if (TPVMode == TPVModes.FRONT) {
                    if (followheadrot)
                        targetPosition = CameraFollower.transform.TransformPoint(new Vector3(0.1f, 0.3f, 2.5f));
                    else
                        targetPosition = TPVBodyFollower.transform.TransformPoint(new Vector3(0.1f, 0.3f, 2.5f));
                    CameraTablet.transform.position = Vector3.SmoothDamp(CameraTablet.transform.position,
                        targetPosition, ref velocity, 0.1f);
                    CameraTablet.transform.LookAt(2f * CameraTablet.transform.position -
                                                  CameraFollower.transform.position);
                }

                if (BindPressed()) {
                    var headT = Player.Instance.headCollider.transform;
                    
                    CameraTablet.transform.position = headT.position + headT.forward;
                    CameraTablet.transform.LookAt(headT.position);
                    CameraTablet.transform.Rotate(0f, -180f, 0f);
                    
                    SetTabletVisibility(true);
                    CameraTablet.transform.parent = null;
                    tpv = false;
                }
            }
        }

        private GameObject LoadBundle(string goname, string resourcename) {
            var str = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcename);
            var asb = AssetBundle.LoadFromStream(str);
            var go = Instantiate(asb.LoadAsset<GameObject>(goname));
            asb.Unload(false);
            str.Close();
            return go;
        }
    }
}