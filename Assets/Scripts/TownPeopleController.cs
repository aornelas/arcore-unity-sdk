//-----------------------------------------------------------------------
// <copyright file="HelloARController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.HelloAR
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.UI;
    using GoogleARCore;

    /// <summary>
    /// Controlls the AR People.
    /// </summary>
    public class TownPeopleController : MonoBehaviour
    {
        /// <summary>
        /// Controller with ARCore-specific logic.
        /// </summary>
        public ARCoreForTownPeopleController m_arCoreController;

        /// <summary>
        /// The camera used when running on the Unity Editor.
        /// </summary>
        public Camera m_editorCamera;

        /// <summary>
        /// The prefab to create the town from
        /// </summary>
        public GameObject m_townPrefab;

        /// <summary>
        /// The list of prefabs to place when a raycast from a user touch hits a plane.
        /// </summary>
        public List<GameObject> m_characterPrefabs;

        /// <summary>
        /// The prefab to use when spawning an ad.
        /// </summary>
        public GameObject m_adPrefab;

        /// <summary>
        /// How hard to throw the ad.
        /// </summary>
        public float m_adThrowForce;

        /// <summary>
        /// How many characters to spawn between ads.
        /// </summary>
        public int m_adFrequency = 5;

        /// <summary>
        /// Developer UI
        /// </summary>
        public GameObject m_devPanel;
        public Text m_sizeLabel;
        public Text m_positionXLabel;
        public Text m_positionZLabel;

        /// <summary>
        /// The amount to increase or decrease in size for the town
        /// </summary>
        public float m_modifierStep;

        private bool _placingPeople;

        private int _spawnCount;

        private GameObject _currentTown;

        private Camera _activeCamera;


        /// <summary>
        /// The Unity Start() method.
        /// </summary>
        public void Start ()
        {
            m_devPanel.SetActive (false);
            _placingPeople = false;
#if UNITY_EDITOR
            m_editorCamera.gameObject.SetActive (true);
            _activeCamera = m_editorCamera;
#elif UNITY_ANDROID
            _activeCamera = m_arCoreController.Init ();
#elif UNITY_IOS
            // TODO: add ARKit Init
#endif
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update ()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown (0)) {
                if (_placingPeople) {
                    float randomX = Random.Range (0.0f, 2.0f);
                    float randomZ = Random.Range (0.0f, 2.0f);
                    Vector3 position = new Vector3 (randomX, 0, randomZ);
                    PlaceCharacter (position, _currentTown.transform);
                } else if (!m_devPanel.activeSelf) { // otherwise dev panel clicks overlap with placing towns
                    Vector3 position = new Vector3 (0, 0, 0);
                    PlaceTown (position, null);
                }
            }
#elif UNITY_ANDROID
            m_arCoreController.ARUpdate ();
#elif UNITY_IOS
            // TODO: add ARKit Update
#endif
        }

        public GameObject PlaceTown (Vector3 position, Transform parent)
        {
            if (m_devPanel.activeSelf)
            {
                return _currentTown;
            }
            if (_currentTown != null) {
                Destroy (_currentTown);
            }
            GetComponent<AudioSource> ().Play ();

            // Intanstiate town
            GameObject townObject = Instantiate (m_townPrefab, position, Quaternion.identity, parent);
            // Adjust size for prefabs
            float sizeMultiplier = 0.02f;
            townObject.transform.localScale = new Vector3 (sizeMultiplier, sizeMultiplier, sizeMultiplier);

            // Town should look at the camera but still be flush with the plane.
            townObject.transform.LookAt (_activeCamera.transform);
            townObject.transform.rotation = Quaternion.Euler (0.0f, townObject.transform.rotation.eulerAngles.y,
                townObject.transform.rotation.z);

            _currentTown = townObject;
            ToggleDevPanel (true);
            return townObject;
        }
        
        public GameObject PlaceCharacter (Vector3 position, Transform parent)
        {
            bool adTime = (_spawnCount + 1) % (m_adFrequency + 1) == 0;
            int randomIndex = Random.Range (0, m_characterPrefabs.Count - 1);

            GameObject spawnPrefab = adTime ? m_adPrefab : m_characterPrefabs [randomIndex];

            position.y += 0.5f;
#if UNITY_EDITOR
            // Spawn ad in front of editor camera
            if (adTime) {
                position.x = 1.5f;
                position.z = 0.0f;
            }
#endif

            GameObject spawnedObject = Instantiate (spawnPrefab, position, Quaternion.identity, null);

            // Adjust size for Simple Citizens prefabs
            float sizeMultiplier = _currentTown.transform.localScale.x;
            spawnedObject.transform.localScale = new Vector3 (sizeMultiplier, sizeMultiplier, sizeMultiplier);

            // Andy should look at the camera but still be flush with the plane.
            spawnedObject.transform.LookAt (_activeCamera.transform);
            spawnedObject.transform.rotation = Quaternion.Euler (0.0f, spawnedObject.transform.rotation.eulerAngles.y,
            spawnedObject.transform.rotation.z);
            spawnedObject.transform.SetParent (parent);

            _spawnCount++;

            if (adTime) {
                // Add some force to throw the ad cube
                spawnedObject.GetComponent<Rigidbody> ().AddForce (spawnedObject.transform.forward * m_adThrowForce * -0.5f);
                spawnedObject.GetComponent<Rigidbody> ().AddTorque (spawnedObject.transform.right * m_adThrowForce * 2.5f);
                spawnedObject.GetComponent<Rigidbody> ().AddTorque (spawnedObject.transform.forward * m_adThrowForce * -2.5f);
            } else {
                GetComponent<AudioSource> ().Play ();
            }

            return spawnedObject;
        }

        public void CommitTown ()
        {
            _currentTown.transform.Find ("glass-ball").gameObject.SetActive (false);
            ToggleDevPanel (false);
            _placingPeople = true;
            _spawnCount = 0;
        }

        public void DeleteTown ()
        {
            ToggleDevPanel (false);
            Destroy (_currentTown);
            _placingPeople = false;
        }

        public void ToggleDevPanel (bool show)
        {
            if (_currentTown == null) {
                return;
            }

            GetComponent<AudioSource> ().Play ();
            _UpdateSizeLabel ();
            _UpdatePositionXLabel ();
            _UpdatePositionZLabel ();
            m_devPanel.SetActive (show);
        }

        public void ChangeSize (int multiplier)
        {
            GetComponent<AudioSource> ().Play ();
            float sizeIncrement = m_modifierStep * multiplier;
            Vector3 currentSize = _currentTown.transform.localScale;
            _currentTown.transform.localScale = new Vector3 (currentSize.x + sizeIncrement,
                currentSize.y + sizeIncrement, currentSize.z + sizeIncrement);
            _UpdateSizeLabel ();
        }

        public void MoveSideways (int multiplier)
        {
            GetComponent<AudioSource> ().Play ();
            float moveAmount = m_modifierStep * multiplier * 10;
            Vector3 currentPosition = _currentTown.transform.position;
            _currentTown.transform.position = new Vector3 (currentPosition.x + moveAmount, currentPosition.y,
                currentPosition.z);
            _UpdatePositionXLabel ();
        }

        public void MoveNearways (int multiplier)
        {
            GetComponent<AudioSource> ().Play ();
            float moveAmount = m_modifierStep * multiplier * 10;
            Vector3 currentPosition = _currentTown.transform.position;
            _currentTown.transform.position = new Vector3 (currentPosition.x, currentPosition.y,
                currentPosition.z + moveAmount);
            _UpdatePositionZLabel ();
        }

        public void Rotate (int multiplier)
        {
            GetComponent<AudioSource> ().Play ();
            float rotateAmount = m_modifierStep * multiplier * 100;
            Quaternion currentRotation = _currentTown.transform.rotation;
            _currentTown.transform.rotation = Quaternion.Euler (_currentTown.transform.rotation.x,
                _currentTown.transform.rotation.eulerAngles.y + rotateAmount, _currentTown.transform.rotation.z);
        }

        private void _UpdateSizeLabel ()
        {
            Vector3 currentSize = _currentTown.transform.localScale;
            m_sizeLabel.text = string.Format ("{0},{1},{2}", _Round (currentSize.x), _Round (currentSize.y),
                _Round (currentSize.z));
        }

        private void _UpdatePositionXLabel ()
        {
            m_positionXLabel.text = _Round (_currentTown.transform.position.x).ToString ();
        }

        private void _UpdatePositionZLabel ()
        {
            m_positionZLabel.text = _Round (_currentTown.transform.position.z).ToString ();
        }

        private double _Round (float val)
        {
            return System.Math.Round (val, 3);
        }
    }
}
