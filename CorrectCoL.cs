/*
The MIT License (MIT)

Copyright (c) 2016 Boris-Barboris

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in this Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using KSP.UI.Screens;

namespace CorrectCoL
{

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public partial class CorrectCoL: MonoBehaviour
    {
        public EditorVesselOverlays overlays;
        public EditorMarker_CoL old_CoL_marker;
        public static CoLMarkerFull new_CoL_marker;
        public static PhysicsGlobals.LiftingSurfaceCurve bodylift_curves;
        static bool far_searched = false;
        static bool far_found = false;

        Button.ButtonClickedEvent clickEvent;

        void Start()
        {
            Debug.Log("[CorrectCoL]: Starting!");

            if (!far_searched)
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (a.GetName().Name.Equals("FerramAerospaceResearch"))
                    {
                        far_found = true;
                        break;
                    }
                }
                far_searched = true;
            }
            if (far_found)
            {
                Debug.Log("[CorrectCoL]: FAR found, disabling itself!");
                GameObject.Destroy(this.gameObject);
                return;
            }

            overlays = EditorVesselOverlays.fetch;
            if (overlays == null)
            {
                Debug.Log("[CorrectCoL]: overlays is null!");
                GameObject.Destroy(this.gameObject);
                return;
            }
            old_CoL_marker = overlays.CoLmarker;
            if (old_CoL_marker == null)
            {
                Debug.Log("[CorrectCoL]: CoL_marker is null!");
                GameObject.Destroy(this.gameObject);
                return;
            }
            bodylift_curves = PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift");
            if (new_CoL_marker == null)
            {
                new_CoL_marker = this.gameObject.AddComponent<CoLMarkerFull>();
                CoLMarkerFull.lift_curves = bodylift_curves;
                new_CoL_marker.posMarkerObject = (GameObject)GameObject.Instantiate(old_CoL_marker.dirMarkerObject);
                new_CoL_marker.posMarkerObject.transform.parent = new_CoL_marker.transform;
                new_CoL_marker.posMarkerObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                new_CoL_marker.posMarkerObject.SetActive(false);
                new_CoL_marker.posMarkerObject.layer = 2;
                foreach (Transform child in new_CoL_marker.posMarkerObject.transform)
                {
                    child.gameObject.layer = 2;
                }
                GameEvents.onEditorRestart.Add(new EventVoid.OnEvent(TurnOffCoL));
                // should be called once, so let's deserialize graph here too                
                GraphWindow.load_settings();
                GraphWindow.init_textures(true);
                GraphWindow.init_reflections();

                clickEvent = new Button.ButtonClickedEvent();
                clickEvent.AddListener(ToggleCoL);
            }
            GameEvents.onGUIApplicationLauncherReady.Add(onAppLauncherLoad);
            onAppLauncherLoad();
            GraphWindow.shown = false;
            new_CoL_marker.enabled = false;
            old_CoL_marker.gameObject.SetActive(false);
            overlays.toggleCoLbtn.onClick = clickEvent;
            //overlays.toggleCoLbtn.methodToInvoke = "ToggleCoL";
        }

        public void ToggleCoL()
        {
            if (EditorLogic.fetch.ship != null && EditorLogic.fetch.ship.parts.Count > 0)
            {
                if (!new_CoL_marker.gameObject.activeSelf)
                    new_CoL_marker.gameObject.SetActive(true);
                new_CoL_marker.enabled = !new_CoL_marker.enabled;
            }
            else
                new_CoL_marker.enabled = false;
            new_CoL_marker.posMarkerObject.SetActive(new_CoL_marker.enabled);
        }

        public void OnDestroy()
        {
            GameEvents.onEditorRestart.Remove(new EventVoid.OnEvent(TurnOffCoL));
            GraphWindow.save_settings();
            GraphWindow.shown = false;
        }

        public void TurnOffCoL()
        {
            new_CoL_marker.enabled = false;
            new_CoL_marker.posMarkerObject.SetActive(false);
        }

        static ApplicationLauncherButton launcher_btn;

        void onAppLauncherLoad()
        {
            if (ApplicationLauncher.Ready)
            {
                bool hidden;
                bool contains = (launcher_btn == null) ? false : ApplicationLauncher.Instance.Contains(launcher_btn, out hidden);
                if (!contains)
                    launcher_btn = ApplicationLauncher.Instance.AddModApplication(
                        OnALTrue, OnALFalse, null, null, null, null,
                        ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                        GameDatabase.Instance.GetTexture("CorrectCoL/icon", false));
            }
        }

        static void OnALTrue()
        {
            GraphWindow.shown = true;
        }

        static void OnALFalse()
        {
            GraphWindow.shown = false;
        }

        void OnGUI()
        {
            GraphWindow.OnGUI();
        }

    }
}
