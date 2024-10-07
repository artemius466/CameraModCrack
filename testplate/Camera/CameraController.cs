﻿using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.UI;
using YizziCamModV2.Comps;
//using BepInEx;

#pragma warning disable CS0618
namespace YizziCamModV2 {
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
        public GameObject LeftHandGO;
        public GameObject RightHandGO;
        public GameObject FakeWebCam;
        public GameObject TabletCameraGO;
        public GameObject MainPage;
        public GameObject MiscPage;
        public GameObject LeftGrabCol;
        public GameObject RightGrabCol;
        public GameObject CameraFollower;
        public GameObject TPVBodyFollower;
        public GameObject ColorScreenGO;
        public List<GameObject> Buttons = new List<GameObject>();
        public List<GameObject> ColorButtons = new List<GameObject>();
        public List<Material> ScreenMats = new List<Material>();
        public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        public Camera TabletCamera;
        public Camera FirstPersonCamera;
        public Camera ThirdPersonCamera;
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
        public bool canbeused;
        public bool flipped;
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

        public static void ChangeFov(float difference) {
            var controller = Instance;

            var max = 130;
            var min = 20;

            var newFov = Mathf.Clamp(controller.TabletCamera.fieldOfView + difference, min, max);
            SetFov(newFov);
        }

        public static void SetFov(float fov) {
            var controller = Instance;

            var newFov = fov;
            controller.TabletCamera.fieldOfView = newFov;
            controller.ThirdPersonCamera.fieldOfView = newFov;

            controller.ThirdPersonCamera.fieldOfView = controller.TabletCamera.fieldOfView;
            controller.FovText.text = controller.TabletCamera.fieldOfView.ToString();
            controller.canbeused = true;
        }

        public void YizziStart() {
            gameObject.AddComponent<InputManager>().gameObject.AddComponent<UI>();
            var assetsPath = Assembly.GetExecutingAssembly().GetName().Name + ".Camera.Assets";
            Debug.Log(assetsPath);
            ColorScreenGO = LoadBundle("ColorScreen", assetsPath + ".colorscreen");
            CameraTablet = LoadBundle("CameraTablet", assetsPath + ".yizzicam");
            FirstPersonCameraGO = GorillaTagger.Instance.mainCamera;

            ThirdPersonCameraGO = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
            CMVirtualCameraGO = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera/CM vcam1");
            TPVBodyFollower = GorillaTagger.Instance.bodyCollider.gameObject;
            CMVirtualCamera = CMVirtualCameraGO.GetComponent<CinemachineVirtualCamera>();
            FirstPersonCamera = FirstPersonCameraGO.GetComponent<Camera>();


            ThirdPersonCamera = ThirdPersonCameraGO.GetComponent<Camera>();

            LeftHandGO = GorillaTagger.Instance.leftHandTransform.gameObject;
            RightHandGO = GorillaTagger.Instance.rightHandTransform.gameObject;
            CameraTablet.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            CameraFollower =
                GameObject.Find(
                    "Player Objects/Player VR Controller/GorillaPlayer/TurnParent/Main Camera/Camera Follower");


            TabletCameraGO = GameObject.Find("CameraTablet(Clone)/Camera");
            TabletCamera = TabletCameraGO.GetComponent<Camera>();

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
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/MiscButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FPVButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FovUP"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FovDown"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FlipCamButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/NearClipUp"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/NearClipDown"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FPButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/ControlsButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/TPVButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/SmoothingDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/SmoothingUpButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/BackButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/GreenScreenButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/MinDistDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/MinDistUpButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/SpeedDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/SpeedUpButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/SpeedDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/TPModeDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/TPModeUpButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/TPRotButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/TPRotButton1"));
            foreach (var btns in Buttons) btns.AddComponent<YzGButton>();
            CMVirtualCamera.enabled = false;
            ThirdPersonCameraGO.transform.SetParent(CameraTablet.transform, true);
            CameraTablet.transform.position = new Vector3(-65, 12, -82);
            ThirdPersonCameraGO.transform.position = TabletCamera.transform.position;
            ThirdPersonCameraGO.transform.rotation = TabletCamera.transform.rotation;
            CameraTablet.transform.Rotate(0, 180, 0);
            
            ColorScreenText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/ColorScreenText")
                .GetComponent<Text>();
            
            ColorButtons.Add(GameObject.Find("ColorScreen(Clone)/Stuff/RedButton"));
            ColorButtons.Add(GameObject.Find("ColorScreen(Clone)/Stuff/GreenButton"));
            ColorButtons.Add(GameObject.Find("ColorScreen(Clone)/Stuff/BlueButton"));
            
            foreach (var btns in ColorButtons) btns.AddComponent<YzGButton>();
            
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
            Debug.Log("FOV SET");
            SetFov(100);
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

            if (bindEnabled && InputManager.instance.RightPrimaryButton && CameraTablet.transform.parent == null) {
                fp = false;
                fpv = false;
                tpv = false;
                if (!MainPage.active) {
                    foreach (var btns in Buttons) btns.SetActive(true);
                    SetTabletVisibility(true);
                    CameraTablet.transform.Rotate(0f, -180f, 0f);

                    MainPage.active = true;
                }

                var headTransform = Player.Instance.headCollider.transform;
                CameraTablet.transform.position = headTransform.position + headTransform.forward;
            }

            if (fp) {
                CameraTablet.transform.LookAt(2f * CameraTablet.transform.position - CameraFollower.transform.position);
                if (!flipped) {
                    flipped = true;
                    ThirdPersonCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                    TabletCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                    FakeWebCam.transform.Rotate(-180f, 180f, 0.0f);
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

                if (InputManager.instance.RightPrimaryButton) {
                    CameraTablet.transform.position = Player.Instance.headCollider.transform.position +
                                                      Player.Instance.headCollider.transform.forward;
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