﻿using System;
using SoftwareCities.unityadapter;
using SoftwareCities.unityadapter.views;
using UnityEngine;

namespace GameControl
{
    public class KeyboardCameraController : MonoBehaviour
    {
        class CameraState
        {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t)
            {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;
                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            public void ViewToCuboid(Transform cuboid)
            {
                pitch = 90;
                yaw = 90;
                roll = 0;
                x = cuboid.position.x;
                y = cuboid.position.z * 1.1f; // camera height from object width
                z = cuboid.position.z;

                pitch = 45;
                yaw = 75;
                roll = 0;
                x = cuboid.position.x;
                y = cuboid.position.y;
                z = cuboid.position.z;
            }

            public void Translate(Vector3 translation)
            {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            public void UpdateTransform(Transform t)
            {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = new Vector3(x, y, z);
            }
        }

        CameraState mTargetCameraState = new CameraState();
        CameraState mInterpolatingCameraState = new CameraState();

        [Header("Movement Settings")] [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 3.5f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings")]
        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve =
            new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;

        void OnEnable()
        {
            mTargetCameraState.SetFromTransform(transform);
            mInterpolatingCameraState.SetFromTransform(transform);
        }

        public void ViewToRoot()
        {
            GameObject root = GameObject.Find("root");
            if (root == null) return;
            Transform cuboid = root.transform.GetChild(root.transform.childCount-1); // The base cuboid is always the last child of root
            mTargetCameraState.ViewToCuboid(cuboid);
            mTargetCameraState.UpdateTransform(transform);
            //mTargetCameraState.Translate(-1.1f * cuboid.position.z * transform.forward);
            mTargetCameraState.x += -1.1f * cuboid.position.z * transform.forward.x;
            mTargetCameraState.y += -1.1f * cuboid.position.z * transform.forward.y;
            mTargetCameraState.z += -1.1f * cuboid.position.z * transform.forward.z;
            mTargetCameraState.UpdateTransform(transform);
        }

        Vector3 GetInputTranslationDirection()
        {
            Vector3 direction = new Vector3();
            if (Input.GetKey(KeyCode.W))
            {
                direction += Vector3.up;
            }

            if (Input.GetKey(KeyCode.S))
            {
                direction += Vector3.down;
            }

            if (Input.GetKey(KeyCode.A))
            {
                direction += Vector3.left;
            }

            if (Input.GetKey(KeyCode.D))
            {
                direction += Vector3.right;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                direction += Vector3.back;
            }

            if (Input.GetKey(KeyCode.E))
            {
                direction += Vector3.forward;
            }

            return direction;
        }

        void Update()
        {
            // Exit Sample  

            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            if (Input.GetKey(KeyCode.R) || View.reset)
            {
                ViewToRoot();
                View.reset = false;
            }

            // Hide and lock cursor when right mouse button pressed
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Unlock and show cursor when right mouse button released
            if (Input.GetMouseButtonUp(1))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Rotation
            if (Input.GetMouseButton(1))
            {
                var mouseMovement =
                    new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));

                var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                mTargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
                mTargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
            }

            // Translation
            var translation = GetInputTranslationDirection() * Time.deltaTime;

            // Speed up movement when shift key held
            if (Input.GetKey(KeyCode.LeftShift))
            {
                translation *= 10.0f;
            }

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += Input.mouseScrollDelta.y * 0.2f;
            translation *= Mathf.Pow(2.0f, boost);

            mTargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            mInterpolatingCameraState.LerpTowards(mTargetCameraState, positionLerpPct, rotationLerpPct);

            mInterpolatingCameraState.UpdateTransform(transform);
        }
    }
}