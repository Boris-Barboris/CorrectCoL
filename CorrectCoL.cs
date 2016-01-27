//The MIT License (MIT)

//Copyright (c) 2016 Boris-Barboris

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
//to deal in this Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CorrectCoL
{

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CorrectCoL: MonoBehaviour
    {
        public EditorVesselOverlays overlays;
        public EditorMarker_CoL old_CoL_marker;
        public static CoLMarkerFull new_CoL_marker;
        public static PhysicsGlobals.LiftingSurfaceCurve bodylift_curves;

        void Start()
        {
            Debug.Log("[CorrectCoL]: Starting!");
            overlays = GameObject.FindObjectOfType<EditorVesselOverlays>();
            if (overlays == null)
            {
                Debug.Log("[CorrectCoL]: overlays is null!");
                GameObject.Destroy(this.gameObject, 1.0f);
                return;
            }
            old_CoL_marker = overlays.CoLmarker;
            if (old_CoL_marker == null)
            {
                Debug.Log("[CorrectCoL]: CoL_marker is null!");
                GameObject.Destroy(this.gameObject, 1.0f);
                return;
            }
            bodylift_curves = PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift");
            if (new_CoL_marker == null)
            {
                new_CoL_marker = this.gameObject.AddComponent<CoLMarkerFull>();
                new_CoL_marker.lift_curves = bodylift_curves;
                new_CoL_marker.posMarkerObject = (GameObject)GameObject.Instantiate(old_CoL_marker.dirMarkerObject);
                new_CoL_marker.posMarkerObject.transform.parent = new_CoL_marker.transform;
                new_CoL_marker.posMarkerObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                new_CoL_marker.posMarkerObject.SetActive(false);
                GameEvents.onEditorRestart.Add(new EventVoid.OnEvent(TurnOffCoL));
            }
            new_CoL_marker.gameObject.SetActive(false);
            old_CoL_marker.gameObject.SetActive(false);
            overlays.CoLmarker = null;
            overlays.toggleCoLbtn.scriptWithMethodToInvoke = this;
            overlays.toggleCoLbtn.methodToInvoke = "ToggleCoL";            
        }

        public void ToggleCoL()
        {
            if (EditorLogic.fetch.ship != null && EditorLogic.fetch.ship.parts.Count > 0)
            {
                new_CoL_marker.gameObject.SetActive(!new_CoL_marker.gameObject.activeSelf);
            }
        }

        public void OnDestroy()
        {
            GameEvents.onEditorRestart.Remove(new EventVoid.OnEvent(TurnOffCoL));
        }

        public void TurnOffCoL()
        {
            new_CoL_marker.gameObject.SetActive(false);
        }

        public class CoLMarkerFull : MonoBehaviour
        {
            public PhysicsGlobals.LiftingSurfaceCurve lift_curves;

            public GameObject posMarkerObject;

            public float speed = 150.0f;
            public float altitude = 100.0f;
            public float AoA = 3.0f;
            
            double sound_speed = 0.0;
            double pressure = 0.0;
            double density = 0.0;
            float mach = 0.0f;

            CenterOfLiftQuery qry = new CenterOfLiftQuery();

            void LateUpdate()
            {
                setup(qry);

                force_occlusion_update_recurse(EditorLogic.RootPart);
                CenterOfLiftQueryRecurse(EditorLogic.RootPart, qry);
                
                if (EditorLogic.SelectedPart != null)
                {
                    if (!EditorLogic.fetch.ship.Contains(EditorLogic.SelectedPart))
                        if (EditorLogic.SelectedPart.potentialParent)
                        {
                            force_occlusion_update_recurse(EditorLogic.SelectedPart);
                            CenterOfLiftQueryRecurse(EditorLogic.SelectedPart, qry);
                            for (int i = 0; i < EditorLogic.SelectedPart.symmetryCounterparts.Count; i++)
                            {
                                force_occlusion_update_recurse(EditorLogic.SelectedPart.symmetryCounterparts[i]);
                                CenterOfLiftQueryRecurse(EditorLogic.SelectedPart.symmetryCounterparts[i], qry);                                    
                            }
                        }
                }
                if (qry.lift > 0.0f)
                {
                    if (!posMarkerObject.activeSelf)
                        posMarkerObject.SetActive(true);
                    posMarkerObject.transform.position = qry.pos / qry.lift;
                    //posMarkerObject.transform.forward = qry.refVector.normalized;//.dir.normalized;
                    posMarkerObject.transform.forward = qry.dir.normalized;
                }
                else
                {
                    if (posMarkerObject.activeSelf)
                        posMarkerObject.SetActive(false);
                }
            }

            void force_occlusion_update_recurse(Part p)
            {
                if (p == null)
                    return;
                force_occlusion_update(p);
                for (int i = 0; i < p.children.Count; i++)
                    force_occlusion_update_recurse(p.children[i]);                
            }

            void force_occlusion_update(Part p)
            {
                if (p == null)
                    return;
                if (!p.DragCubes.None)
                {
                    p.DragCubes.ForceUpdate(true, true);
                    p.DragCubes.SetDragWeights();
                    p.DragCubes.SetPartOcclusion();
                }
            }

            public void setup(CenterOfLiftQuery qry)
            {
                CelestialBody home = Planetarium.fetch.Home;
                pressure = home.GetPressure(altitude);
                density = home.GetDensity(pressure, home.GetTemperature(altitude));
                sound_speed = home.GetSpeedOfSound(pressure, density);
                mach = (float)(speed / sound_speed);

                qry.refAirDensity = density;
                qry.refStaticPressure = pressure;
                qry.refAltitude = altitude;
                qry.refVector = EditorLogic.VesselRotation * (Quaternion.AngleAxis(AoA, EditorLogic.RootPart.partTransform.right) * Vector3.up);
                qry.refVector *= speed;
                Debug.Log("[CoL] refVector = " + qry.refVector.ToString());
                
                qry.lift = 0.0f;
                qry.dir = Vector3.zero;
                qry.pos = Vector3.zero;

                local_qry.refAirDensity = qry.refAirDensity;
                local_qry.refAltitude = qry.refAltitude;
                local_qry.refStaticPressure = qry.refStaticPressure;
                local_qry.refVector = qry.refVector;
            }

            CenterOfLiftQuery local_qry = new CenterOfLiftQuery();

            public void CenterOfLiftQuery(Part p, CenterOfLiftQuery qry)
            {
                if (p == null)
                    return;

                Vector3 pos = Vector3.zero;
                Vector3 dir = Vector3.zero;
                float abs_lift = 0.0f;

                if (!p.ShieldedFromAirstream)
                {

                    var providers = p.FindModulesImplementing<ILiftProvider>();
                    if ((providers != null) && providers.Count > 0)
                        p.hasLiftModule = true;

                    if (!p.hasLiftModule)
                    {
                        // stock aero shenanigans
                        if (!p.DragCubes.None)
                        {
                            p.dragVector = qry.refVector;
                            p.dragVectorSqrMag = p.dragVector.sqrMagnitude;
                            p.dragVectorMag = Mathf.Sqrt(p.dragVectorSqrMag);
                            p.dragVectorDir = p.dragVector / p.dragVectorMag;
                            p.dragVectorDirLocal = -p.partTransform.InverseTransformDirection(p.dragVectorDir);

                            p.dynamicPressurekPa = qry.refAirDensity * 0.0005 * p.dragVectorSqrMag;
                            p.bodyLiftScalar = (float)(p.dynamicPressurekPa * p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier *
                                lift_curves.liftMachCurve.Evaluate(mach));

                            if (p.rb != null)
                                pos = p.rb.worldCenterOfMass + p.partTransform.rotation * p.CoLOffset;
                            else
                                pos = p.CoMOffset + p.partTransform.position + p.partTransform.rotation * p.CoLOffset;

                            p.DragCubes.SetDrag(p.dragVectorDirLocal, mach);
                            dir = p.partTransform.rotation * (p.bodyLiftScalar * p.DragCubes.LiftForce);
                            dir = Vector3.ProjectOnPlane(dir, -p.dragVectorDir);

                            abs_lift = dir.magnitude;
                            qry.pos += pos * abs_lift;
                            qry.dir += dir;
                            qry.lift += abs_lift;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < providers.Count; i++)
                        {
                            local_qry.lift = 0.0f;
                            local_qry.pos = Vector3.zero;
                            local_qry.dir = Vector3.zero;
                            providers[i].OnCenterOfLiftQuery(local_qry);
                            Vector3 corrected_lift = Vector3.ProjectOnPlane(local_qry.dir, qry.refVector);
                            local_qry.lift = Mathf.Abs(Vector3.Dot(corrected_lift, local_qry.dir)) * local_qry.lift;
                            pos += local_qry.pos * local_qry.lift;
                            dir += corrected_lift.normalized * local_qry.lift;
                            abs_lift += local_qry.lift;
                        }
                        qry.pos += pos;
                        qry.dir += dir;
                        qry.lift += abs_lift;
                    }
                }
            }

            public void CenterOfLiftQueryRecurse(Part p, CenterOfLiftQuery qry)
            {
                if (p == null)
                    return;

                CenterOfLiftQuery(p, qry);

                for (int i = 0; i < p.children.Count; i++)
                    CenterOfLiftQueryRecurse(p.children[i], qry);
            }
        }
    }
}
