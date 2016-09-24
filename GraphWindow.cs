﻿/*
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
using System.Reflection;
using KSP.IO;

namespace CorrectCoL
{
    public static class GraphWindow
    {
        public const int wnd_width = 620;
        public const int wnd_height = 500;

        static Rect wnd_rect = new Rect(100.0f, 100.0f, wnd_width, wnd_height);
        public static bool shown = false;
        static PluginConfiguration conf;

        static bool init_gui = false;
        static bool locked = false;

        public static void OnGUI()
        {
            if (!init_gui)
            {
                init_styles();
                init_gui = true;
            }
            EditorLogic editorlogic = EditorLogic.fetch;
            if (shown)
            {
                if (wnd_rect.Contains(Input.mousePosition))
                {
                    if (!CameraMouseLook.MouseLocked && !locked)
                    {
                        //editorlogic.Lock(false, false, false, "CorrectCoLWindow");
                        locked = true;
                    }
                }
                else if (locked)
                {
                    //editorlogic.Unlock("CorrectCoLWindow");
                    locked = false;
                }
                wnd_rect = GUI.Window(54665949, wnd_rect, _drawGUI, "Static stability analysis");
            }
            else if (locked)
            {
                //editorlogic.Unlock("CorrectCoLWindow");
                locked = false;
            }
        }

        static void _drawGUI(int id)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(wnd_width));
                GUILayout.BeginVertical(GUILayout.Width(graph_width + 10));
                    // draw pitch box
                    GUILayout.Label("pitch");
                    GUILayout.Box(pitch_texture);
                    // draw yaw box
                    GUILayout.Label("yaw");
                    GUILayout.Box(yaw_texture);
                GUILayout.EndVertical();
                // draw side text
                GUILayout.BeginVertical(GUILayout.Width(wnd_width - graph_width - 30));
                    GUILayout.Label("side");
                    bool draw = GUILayout.Button("Update");
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("aoa range:");
                        aoa_range_str = GUILayout.TextField(aoa_range_str);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("aoa marks:");
                        aoa_mark_delta_str = GUILayout.TextField(aoa_mark_delta_str);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("aoa compress:");
                        aoa_compress_str = GUILayout.TextField(aoa_compress_str);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("speed:");
                        speed_str = GUILayout.TextField(speed_str);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("altitude:");
                        alt_str = GUILayout.TextField(alt_str);
                    GUILayout.EndHorizontal();
                    //GUILayout.BeginHorizontal();
                    //    GUILayout.Label("pitch:");
                    //    pitch_ctrl_str = GUILayout.TextField(pitch_ctrl_str);
                    //GUILayout.EndHorizontal();
                    
                    // traits system
                    GUILayout.Space(15.0f);
                    gui_traits();
                GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.DragWindow();

            if (draw)
                update_graphs();
        }

        public static void save_settings()
        {
            if (conf == null)
                conf = PluginConfiguration.CreateForType<CorrectCoL>();
            Debug.Log("[CorrectCoL]: serializing");
            conf.SetValue("x", wnd_rect.x.ToString());
            conf.SetValue("y", wnd_rect.y.ToString());
            conf.SetValue("range", aoa_range.ToString());
            conf.save();

            // clear lock
            if (locked)
            {
                EditorLogic.fetch.Unlock("CorrectCoLWindow");
                locked = false;
            }
        }

        public static void load_settings()
        {
            if (conf == null)
                conf = PluginConfiguration.CreateForType<CorrectCoL>();
            try
            {
                conf.load();
                Debug.Log("[CorrectCoL]: deserializing");
                wnd_rect.x = float.Parse(conf.GetValue<string>("x"));
                wnd_rect.y = float.Parse(conf.GetValue<string>("y"));
                aoa_range_str = conf.GetValue<string>("range");
                if (aoa_range_str == null || aoa_range_str.Length == 0)
                    aoa_range_str = "180";
                float.TryParse(aoa_range_str, out aoa_range);
            }
            catch (Exception) { }
        }

        public const int graph_width = 450;
        public const int graph_height = 200;
        static Texture2D pitch_texture = new Texture2D(graph_width, graph_height, TextureFormat.ARGB32, false);
        static Texture2D yaw_texture = new Texture2D(graph_width, graph_height, TextureFormat.ARGB32, false);

        public static void init_textures(bool apply = false)
        {
            var fillcolor = Color.black;
            var arr = pitch_texture.GetPixels();
            for (int i = 0; i < arr.Length; i++)
                arr[i] = fillcolor;
            pitch_texture.SetPixels(arr);
            yaw_texture.SetPixels(arr);
            if (apply)
            {
                pitch_texture.Apply();
                yaw_texture.Apply();
            }
        }

        static int aoa_mark_delta = 15;
        static string aoa_mark_delta_str = 15.ToString();

        static void init_axes()
        {
            // aoa axis
            int x0 = 0;
            int y0 = graph_height / 2;
            int x1 = graph_width - 1;
            int y1 = y0;
            DrawLine(pitch_texture, x0, y0, x1, y1, Color.white);
            DrawLine(yaw_texture, x0, y0, x1, y1, Color.white);
            // angular acc axis
            x0 = graph_width / 2;
            y0 = 0;
            x1 = x0;
            y1 = graph_height - 1;
            DrawLine(pitch_texture, x0, y0, x1, y1, Color.white);
            DrawLine(yaw_texture, x0, y0, x1, y1, Color.white);

            // let's build aoa transformation net
            float.TryParse(aoa_range_str, out aoa_range);
            aoa_range = Mathf.Min(180.0f, Mathf.Max(1.0f, aoa_range));
            float.TryParse(aoa_compress_str, out aoa_compress);
            aoa_compress = Mathf.Min(Mathf.Max(0.0f, aoa_compress), 1e5f);

            AoA_net.Clear();
            float x_max = num_pts - 1.0f;
            aoa_scaling = aoa_range / (x_max * (1.0f + aoa_compress * x_max));
            for (int i = -num_pts + 1; i < num_pts; i++)
            {
                float x = i;
                float y = aoa_scaling * (Mathf.Abs(x) + aoa_compress * x * x) * Mathf.Sign(x);
                AoA_net.Add(y);
            }

            // marks
            int.TryParse(aoa_mark_delta_str, out aoa_mark_delta);
            int mark_delta = Math.Max(1, aoa_mark_delta);
            y0 = graph_height / 2 - 5;
            y1 = graph_height / 2 + 5;

            float aoa = mark_delta;
            while (aoa < aoa_range)
            {
                x0 = aoa2pixel(aoa);
                DrawLine(pitch_texture, x0, y0, x0, y1, Color.white);
                DrawLine(yaw_texture, x0, y0, x0, y1, Color.white);
                aoa += mark_delta;
            }
            aoa = -mark_delta;
            while (aoa > -aoa_range)
            {
                x0 = aoa2pixel(aoa);
                DrawLine(pitch_texture, x0, y0, x0, y1, Color.white);
                DrawLine(yaw_texture, x0, y0, x0, y1, Color.white);
                aoa -= mark_delta;
            }
        }

        public static void update_graphs()
        {
            init_textures();
            init_axes();
            if (EditorLogic.fetch.ship != null && EditorLogic.fetch.ship.parts.Count > 0)
            {
                // here we calculate aerodynamics
                CorrectCoL.CoLMarkerFull.force_occlusion_update_recurse(EditorLogic.RootPart);
                calculate_moments();
                analyze_traits();
                draw_moments();
            }
            pitch_texture.Apply();
            yaw_texture.Apply();
        }

        static void DrawLine(Texture2D tex, int x1, int y1, int x2, int y2, Color col)
        {
            int dy = (int)(y2 - y1);
            int dx = (int)(x2 - x1);
            int stepx, stepy;

            if (dy < 0) { dy = -dy; stepy = -1; }
                else { stepy = 1; }
            if (dx < 0) { dx = -dx; stepx = -1; }
                else { stepx = 1; }
            dy <<= 1;
            dx <<= 1;

            float fraction = 0;

            tex.SetPixel(x1, y1, col);
            if (dx > dy)
            {
                fraction = dy - (dx >> 1);
                while (Mathf.Abs(x1 - x2) > 1)
                {
                    if (fraction >= 0)
                    {
                        y1 += stepy;
                        fraction -= dx;
                    }
                    x1 += stepx;
                    fraction += dy;
                    tex.SetPixel(x1, y1, col);
                }
            }
            else {
                fraction = dx - (dy >> 1);
                while (Mathf.Abs(y1 - y2) > 1)
                {
                    if (fraction >= 0)
                    {
                        x1 += stepx;
                        fraction -= dy;
                    }
                    y1 += stepy;
                    fraction += dx;
                    tex.SetPixel(x1, y1, col);
                }
            }
        }

        public const float dgr2rad = Mathf.PI / 180.0f;
        public const float rad2dgr = 1.0f / dgr2rad;

        static float aoa_range = 90.0f;
        static string aoa_range_str = 90.0f.ToString();

        const int num_pts = 40;
        static List<float> AoA_net = new List<float>(num_pts * 2);
        static float aoa_scaling = 1.0f;
        static float aoa_compress = 0.0f;
        static string aoa_compress_str = 0.0f.ToString();

        static Vector3 CoM = Vector3.zero;

        static float wet_mass = 0.0f;

        static float[] wet_torques_aoa = new float[num_pts * 2 - 1];
        static float[] dry_torques_aoa = new float[num_pts * 2 - 1];
        static float[] wet_torques_sideslip = new float[num_pts * 2 - 1];
        static float[] dry_torques_sideslip = new float[num_pts * 2 - 1];
        static float[] wet_lift = new float[num_pts * 2 - 1];
        static float[] wet_drag = new float[num_pts * 2 - 1];
        static float[] LtD = new float[num_pts * 2 - 1];

        static void calculate_moments()
        {
            double.TryParse(alt_str, out altitude);
            float.TryParse(speed_str, out speed);
            //float.TryParse(pitch_ctrl_str, out pitch_ctrl);
            //pitch_ctrl = Mathf.Max(-1.0f, Mathf.Min(1.0f, pitch_ctrl));
            //EditorLogic.RootPart.vessel.ctrlState.pitch = pitch_ctrl;

            // update CoM
            CoM = EditorMarker_CoM.findCenterOfMass(EditorLogic.RootPart);

            // wet cycles
            for (int i = 0; i < AoA_net.Count; i++)
            {
                float aoa = AoA_net[i];
                float lift = 0.0f;
                float drag = 0.0f;
                Vector3 sum_torque = get_torque_aoa(aoa, ref lift, ref drag);
                wet_lift[i] = lift;
                wet_drag[i] = drag;
                if (drag != 0.0f)
                    LtD[i] = lift / drag;
                else
                    LtD[i] = 0.0f;
                wet_torques_aoa[i] = Vector3.Dot(sum_torque, EditorLogic.RootPart.partTransform.right);
            }

            for (int i = 0; i < AoA_net.Count; i++)
            {
                float aoa = AoA_net[i];
                Vector3 sum_torque = get_torque_sideslip(aoa);
                wet_torques_sideslip[i] = Vector3.Dot(sum_torque, EditorLogic.RootPart.partTransform.forward);
            }

            // dry the ship by choosing dry CoM
            float smass = 0.0f;
            wet_mass = 0.0f;
            CoM = dry_CoM_recurs(EditorLogic.RootPart, ref smass, ref wet_mass);
            CoM = CoM / smass;

            // dry cycles
            for (int i = 0; i < AoA_net.Count; i++)
            {
                float aoa = AoA_net[i];
                float lift = 0.0f;
                float drag = 0.0f;
                Vector3 sum_torque = get_torque_aoa(aoa, ref lift, ref drag);
                dry_torques_aoa[i] = Vector3.Dot(sum_torque, EditorLogic.RootPart.partTransform.right);
            }

            for (int i = 0; i < AoA_net.Count; i++)
            {
                float aoa = AoA_net[i];
                Vector3 sum_torque = get_torque_sideslip(aoa);
                dry_torques_sideslip[i] = Vector3.Dot(sum_torque, EditorLogic.RootPart.partTransform.forward);
            }
        }

        const float draw_scale = 0.8f;

        static int aoa2pixel(float aoa)
        {
            int middle = graph_width / 2;
            float x2pixel = middle / (float)(num_pts - 1);
            float x = 0.0f;
            if (aoa_compress != 0.0f)
            {
                float D = 1.0f + 4.0f * Mathf.Abs(aoa) * aoa_compress / aoa_scaling;
                x = 0.5f * (-1.0f + Mathf.Sqrt(D)) / aoa_compress * Mathf.Sign(aoa);
            }
            else
                x = aoa / aoa_scaling;
            return middle + (int)Mathf.Round(x * x2pixel);
        }

        static void draw_moments()
        {
            // pitch moments
            float max_pmoment = Mathf.Max(Mathf.Abs(wet_torques_aoa.Max()), Mathf.Abs(wet_torques_aoa.Min()));
            max_pmoment = Mathf.Max(max_pmoment, Mathf.Abs(dry_torques_aoa.Max()));
            max_pmoment = Mathf.Max(max_pmoment, Mathf.Abs(dry_torques_aoa.Min()));
            float maxLtD = Mathf.Max(Mathf.Abs(LtD.Max()), Mathf.Abs(LtD.Min()));
            for (int i = 0; i < AoA_net.Count - 1; i++)
            {
                int x0 = aoa2pixel(AoA_net[i]);
                int x1 = aoa2pixel(AoA_net[i + 1]);
                // wet
                int y0 = (int)(Mathf.Round((1.0f + wet_torques_aoa[i] / max_pmoment * draw_scale) * graph_height / 2.0f));
                int y1 = (int)(Mathf.Round((1.0f + wet_torques_aoa[i + 1] / max_pmoment * draw_scale) * graph_height / 2.0f));
                DrawLine(pitch_texture, x0, y0, x1, y1, Color.green);
                // dry
                y0 = (int)(Mathf.Round((1.0f + dry_torques_aoa[i] / max_pmoment * draw_scale) * graph_height / 2.0f));
                y1 = (int)(Mathf.Round((1.0f + dry_torques_aoa[i + 1] / max_pmoment * draw_scale) * graph_height / 2.0f));
                DrawLine(pitch_texture, x0, y0, x1, y1, Color.yellow);
                // L/D ratio
                y0 = (int)(Mathf.Round((1.0f + LtD[i] / maxLtD * draw_scale) * graph_height / 2.0f));
                y1 = (int)(Mathf.Round((1.0f + LtD[i + 1] / maxLtD * draw_scale) * graph_height / 2.0f));
                DrawLine(pitch_texture, x0, y0, x1, y1, Color.blue);
            }

            // level flight aoa
            if (!float.IsNaN(level_flight_aoa))
            {
                int l0 = aoa2pixel(level_flight_aoa);
                DrawLine(pitch_texture, l0, 0, l0, graph_height, Color.blue);
            }

            // yaw moments
            float max_ymoment = Mathf.Max(Mathf.Abs(wet_torques_sideslip.Max()), Mathf.Abs(wet_torques_sideslip.Min()));
            max_ymoment = Mathf.Max(max_ymoment, Mathf.Abs(dry_torques_sideslip.Max()));
            max_ymoment = Mathf.Max(max_ymoment, Mathf.Abs(dry_torques_sideslip.Min()));
            for (int i = 0; i < AoA_net.Count - 1; i++)
            {
                int x0 = aoa2pixel(AoA_net[i]);
                int x1 = aoa2pixel(AoA_net[i + 1]);
                // wet
                int y0 = (int)(Mathf.Round((1.0f + wet_torques_sideslip[i] / max_ymoment * draw_scale) * graph_height / 2.0f));
                int y1 = (int)(Mathf.Round((1.0f + wet_torques_sideslip[i + 1] / max_ymoment * draw_scale) * graph_height / 2.0f));
                DrawLine(yaw_texture, x0, y0, x1, y1, Color.green);
                // dry
                y0 = (int)(Mathf.Round((1.0f + dry_torques_sideslip[i] / max_ymoment * draw_scale) * graph_height / 2.0f));
                y1 = (int)(Mathf.Round((1.0f + dry_torques_sideslip[i + 1] / max_ymoment * draw_scale) * graph_height / 2.0f));
                DrawLine(yaw_texture, x0, y0, x1, y1, Color.yellow);
            }
        }

        public static Vector3 get_torque_aoa(float aoa, ref float lift, ref float drag)
        {
            setup_qrys(aoa, 0.0f);
            return get_part_torque_recurs(EditorLogic.RootPart, CoM, ref lift, ref drag);
        }

        public static Vector3 get_torque_sideslip(float slip)
        {
            setup_qrys(0.0f, slip);
            float a = 0.0f, b = 0.0f;
            return get_part_torque_recurs(EditorLogic.RootPart, CoM, ref a, ref b);
        }

        static double altitude = 500.0;
        static string alt_str = 500.0.ToString();

        static double pressure, density, sound_speed;

        static float speed = 200.0f;
        static string speed_str = 200.0f.ToString();

        static float mach;

        static float pitch_ctrl = 0.0f;
        static string pitch_ctrl_str = 0.0f.ToString();

        static CenterOfLiftQuery qry = new CenterOfLiftQuery();

        static void setup_qrys(float AoA, float sideslip)
        {
            CelestialBody home = Planetarium.fetch.Home;

            pressure = home.GetPressure(Math.Max(0.0, altitude));
            density = home.GetDensity(pressure, home.GetTemperature(altitude));
            sound_speed = home.GetSpeedOfSound(pressure, density);
            mach = (float)(Mathf.Abs(speed) / sound_speed);

            qry.refAirDensity = density;
            qry.refStaticPressure = pressure;
            qry.refAltitude = altitude;
            qry.refVector = Quaternion.AngleAxis(AoA, EditorLogic.RootPart.partTransform.right) *
                Quaternion.AngleAxis(sideslip, EditorLogic.RootPart.partTransform.forward) *
                EditorLogic.RootPart.partTransform.up;
            qry.refVector *= speed;
        }

        static Vector3 get_part_torque_recurs(Part p, Vector3 CoM, ref float lift, ref float drag)
        {
            if (p == null)
                return Vector3.zero;

            Vector3 tq = get_part_torque(qry, p, CoM, ref lift, ref drag);

            for (int i = 0; i < p.children.Count; i++)
                tq += get_part_torque_recurs(p.children[i], CoM, ref lift, ref drag);

            return tq;
        }

        static FieldInfo deflection_field;

        public static void init_reflections()
        {
            deflection_field = typeof(ModuleControlSurface).GetField("deflection", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static Vector3 get_part_torque(CenterOfLiftQuery qry, Part p, Vector3 CoM, ref float lift, ref float drag)
        {
            if (p == null || p.physicalSignificance == Part.PhysicalSignificance.NONE)
                return Vector3.zero;

            Vector3 lift_pos = Vector3.zero;
            Vector3 drag_pos = Vector3.zero;

            if (!p.ShieldedFromAirstream)
            {
                var providers = p.FindModulesImplementing<ModuleLiftingSurface>();
                if ((providers != null) && providers.Count > 0)
                    p.hasLiftModule = true;

                if (!p.hasLiftModule)
                {
                    // stock aero shenanigans
                    if (!p.DragCubes.None)
                    {
                        Vector3 lift_force = Vector3.zero;
                        Vector3 drag_force = Vector3.zero;

                        p.dragVector = qry.refVector;
                        p.dragVectorSqrMag = p.dragVector.sqrMagnitude;
                        p.dragVectorMag = Mathf.Sqrt(p.dragVectorSqrMag);
                        p.dragVectorDir = p.dragVector / p.dragVectorMag;
                        p.dragVectorDirLocal = -p.partTransform.InverseTransformDirection(p.dragVectorDir);

                        p.dynamicPressurekPa = qry.refAirDensity * 0.0005 * p.dragVectorSqrMag;
                        p.bodyLiftScalar = (float)(p.dynamicPressurekPa * p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier *
                            CorrectCoL.CoLMarkerFull.lift_curves.liftMachCurve.Evaluate(mach));

                        lift_pos = p.partTransform.position + p.partTransform.rotation * (p.CoMOffset + p.CoLOffset);
                        drag_pos = p.partTransform.position + p.partTransform.rotation * (p.CoPOffset + p.CoMOffset);

                        p.DragCubes.SetDrag(p.dragVectorDirLocal, mach);

                        float pseudoreynolds = (float)(density * Mathf.Abs(speed));
                        float pseudoredragmult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate(pseudoreynolds);
                        float drag_k = p.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier * pseudoredragmult;
                        p.dragScalar = (float)(p.dynamicPressurekPa * drag_k * PhysicsGlobals.DragMultiplier);

                        lift_force = p.partTransform.rotation * (p.bodyLiftScalar * p.DragCubes.LiftForce);
                        lift_force = Vector3.ProjectOnPlane(lift_force, -p.dragVectorDir);
                        drag_force = p.dragScalar * -p.dragVectorDir;

                        Vector3 res = Vector3.zero;
                        res += Vector3.Cross(lift_force, lift_pos - CoM);
                        res += Vector3.Cross(drag_force, drag_pos - CoM);

                        Vector3 sum_force = lift_force + drag_force;

                        lift += Vector3.Dot(sum_force, Vector3.Cross(p.dragVectorDir, EditorLogic.RootPart.transform.right).normalized);
                        drag += Vector3.Dot(sum_force, -p.dragVectorDir);

                        return res;
                    }
                }
                else
                {
                    Vector3 res = Vector3.zero;
                    double q = 0.5 * qry.refAirDensity * qry.refVector.sqrMagnitude;

                    for (int i = 0; i < providers.Count; i++)
                    {
                        Vector3 dragvect;
                        Vector3 liftvect;
                        Vector3 lift_force = Vector3.zero;
                        Vector3 drag_force = Vector3.zero;
                        float abs;
                        ModuleLiftingSurface lsurf = providers[i];
                        ModuleControlSurface csurf = lsurf as ModuleControlSurface;
                        lsurf.SetupCoefficients(qry.refVector, out dragvect, out liftvect, out lsurf.liftDot, out abs);

                        lift_pos = p.partTransform.position + p.partTransform.rotation * (p.CoLOffset + p.CoMOffset);
                        drag_pos = p.partTransform.position + p.partTransform.rotation * (p.CoPOffset + p.CoMOffset);

                        lift_force = lsurf.GetLiftVector(liftvect, lsurf.liftDot, abs, q, mach);
                        if (lsurf.useInternalDragModel)
                            drag_force = lsurf.GetDragVector(dragvect, abs, q);

                        if (csurf != null)
                        {
                            float deflection = (float)deflection_field.GetValue(csurf);
                            Quaternion incidence = Quaternion.AngleAxis(csurf.ctrlSurfaceRange * deflection, p.partTransform.rotation * Vector3.right);
                            liftvect = incidence * liftvect;
                            lsurf.liftDot = Vector3.Dot(dragvect, liftvect);
                            abs = Mathf.Abs(lsurf.liftDot);
                            lift_force = lift_force * (1.0f - csurf.ctrlSurfaceArea);
                            lift_force += lsurf.GetLiftVector(liftvect, lsurf.liftDot, abs, q, mach) * csurf.ctrlSurfaceArea;
                            if (csurf.useInternalDragModel)
                            {
                                drag_force = drag_force * (1.0f - csurf.ctrlSurfaceArea);
                                drag_force += csurf.GetDragVector(dragvect, abs, q) * csurf.ctrlSurfaceArea;
                            }
                        }                       

                        res += Vector3.Cross(lift_force, lift_pos - CoM);
                        res += Vector3.Cross(drag_force, drag_pos - CoM);

                        Vector3 result_force = lift_force + drag_force;
                        lift += Vector3.Dot(result_force, Vector3.Cross(qry.refVector, EditorLogic.RootPart.transform.right).normalized);
                        drag += Vector3.Dot(result_force, -qry.refVector.normalized);
                    }
                    return res;
                }
            }

            return Vector3.zero;
        }

        public static Vector3 dry_CoM_recurs(Part p, ref float mass_counter, ref float wet_mass)
        {
            Vector3 res = Vector3.zero;
            if (p == null)
                return res;
            if (p.physicalSignificance == Part.PhysicalSignificance.FULL)
                res = (p.partTransform.position + p.partTransform.rotation * p.CoMOffset) * p.mass;
            mass_counter += p.mass;
            wet_mass += p.mass + p.GetResourceMass();
            for (int i = 0; i < p.children.Count; i++)
            {
                res += dry_CoM_recurs(p.children[i], ref mass_counter, ref wet_mass);
            }
            return res;
        }

        const float stability_region_req = 30.0f;
        static float pitch_wet_stability_region = 0.0f;
        static float pitch_dry_stability_region = 0.0f;
        static float yaw_wet_stability_region = 0.0f;
        static float yaw_dry_stability_region = 0.0f;

        static float level_flight_aoa = 0.0f;

        static StabilityReport[] stability_reports = new StabilityReport[4];

        static void analyze_traits()
        {
            // pitch wet statical stability
            pitch_wet_stability_region = find_stability_region(wet_torques_aoa);
            pitch_dry_stability_region = find_stability_region(dry_torques_aoa);
            yaw_wet_stability_region = find_stability_region(wet_torques_sideslip);
            yaw_dry_stability_region = find_stability_region(dry_torques_sideslip);

            // form reports
            stability_reports[0] = report_stability("fueled craft pitch", pitch_wet_stability_region);
            stability_reports[1] = report_stability("dry craft pitch", pitch_dry_stability_region);
            stability_reports[2] = report_stability("fueled craft yaw", yaw_wet_stability_region);
            stability_reports[3] = report_stability("dry craft yaw", yaw_dry_stability_region);

            // level flight aoa
            level_flight_aoa = find_level_flight_aoa();
        }

        static float find_level_flight_aoa()
        {
            float res = 0.0f;
            int i = num_pts;

            CelestialBody home = Planetarium.fetch.Home;
            double rad = home.Radius + altitude;
            double grav_acc = home.gMagnitudeAtCenter / rad / rad;
            float level_acc = (float)(grav_acc - speed * speed / rad);

            float cur_lift_acc = wet_lift[i] / wet_mass;
            int step = 1;
            if (cur_lift_acc < level_acc)
                step = 1;
            else
                step = -1;
            bool found = false;
            do
            {
                float new_lift_acc = wet_lift[i] / wet_mass;
                if (step > 0 ? new_lift_acc >= level_acc : new_lift_acc <= level_acc)
                {
                    res = Mathf.Lerp(AoA_net[i - step], AoA_net[i], (level_acc - cur_lift_acc) / (new_lift_acc - cur_lift_acc));
                    found = true;
                    break;
                }
                cur_lift_acc = new_lift_acc;
                i += step;                    
            } while (i < num_pts * 2 - 1 && i >= 0);

            if (!found)
                res = float.NaN;

            return res;
        }

        static float find_stability_region(float[] torque_data)
        {
            int start = num_pts;
            float torque = torque_data[start];
            int upper, lower;
            float upper_aoa = -0.5f, lower_aoa = 0.5f;
            if (torque == 0.0f)
            {
                if (torque_data[start + 1] > 0.0f)
                {
                    return -1.0f;
                }
                if (torque_data[start - 1] < 0.0f)
                {
                    return -1.0f;
                }
                upper = start + 1;
                lower = start - 1;
            }
            else
            {
                if (torque > 0.0f)
                {
                    // seek towards positive aoa to find equilibrium
                    while (torque > 0.0f)
                    {
                        start++;
                        if (start >= torque_data.Length)
                        {
                            // we're unstable
                            return -1.0f;
                        }
                        torque = torque_data[start];
                        if (AoA_net[start] > stability_region_req * 0.4f)
                        {
                            // we're unstable
                            return -1.0f;
                        }
                    }
                    // we've found equilibrium between start-1 and start
                    upper = start;
                    lower = start - 1;                    
                }
                else
                {
                    // seek towards negative aoa to find equilibrium
                    while (torque < 0.0f)
                    {
                        start--;
                        if (start < 0)
                        {
                            // we're unstable
                            return -1.0f;
                        }
                        torque = torque_data[start];
                        if (AoA_net[start] < -stability_region_req * 0.4f)
                        {
                            // we're unstable
                            return -1.0f;
                        }
                    }
                    // we've found equilibrium between start and start+1
                    upper = start + 1;
                    lower = start;
                }
            }

            // we now need to expand upper and lower bounds to find the size of stability region
            // upper bound:
            torque = torque_data[upper];
            while (torque < 0.0f)
            {
                upper++;
                if (upper >= torque_data.Length)
                {
                    upper_aoa = AoA_net[upper - 1];
                    break;
                }
                torque = torque_data[upper];
                if (torque >= 0.0f)                    
                    upper_aoa = Mathf.Lerp(AoA_net[upper - 1], AoA_net[upper],
                        Mathf.Abs(torque_data[upper - 1]) / (Mathf.Abs(torque_data[upper - 1]) + torque));
            }
            
            // lower bound:
            torque = torque_data[lower];
            while (torque > 0.0f)
            {
                lower--;
                if (lower < 0)
                {
                    lower_aoa = AoA_net[0];
                    break;
                }
                torque = torque_data[lower];
                if (torque <= 0.0f)
                    lower_aoa = Mathf.Lerp(AoA_net[lower + 1], AoA_net[lower],
                        torque_data[lower + 1] / (torque_data[lower + 1] + Mathf.Abs(torque)));
            }

            return upper_aoa - lower_aoa;
        }

        static GUIStyle stable_style, partial_style, unstable_style;

        public static void init_styles()
        {
            stable_style = new GUIStyle(GUI.skin.label);
            stable_style.normal.textColor = Color.green;

            partial_style = new GUIStyle(GUI.skin.label);
            partial_style.normal.textColor = Color.yellow;

            unstable_style = new GUIStyle(GUI.skin.label);
            unstable_style.normal.textColor = Color.red;
        }

        static void gui_traits()
        {
            if (AoA_net.Count > 3)
            {
                for (int i = 0; i < stability_reports.Length; i++)
                {
                    StabilityReport rep = stability_reports[i];
                    if (rep.report != null)
                    {
                        switch (rep.type)
                        {
                            case StabilityType.Stable:
                                GUILayout.Label(rep.report, stable_style);
                                break;
                            case StabilityType.PartiallyStable:
                                GUILayout.Label(rep.report, partial_style);
                                break;
                            case StabilityType.Unstable:
                                GUILayout.Label(rep.report, unstable_style);
                                break;                            
                        }
                    }
                }
            }
        }

        enum StabilityType
        {
            Stable,
            PartiallyStable,
            Unstable
        }

        struct StabilityReport
        {
            public string report;
            public StabilityType type;
            public StabilityReport(string r, StabilityType t)
            {
                report = r;
                type = t;
            }
        }

        static StabilityReport report_stability(string name, float region)
        {
            if (region >= 2.0f * stability_region_req)
                return new StabilityReport(name + " is stable", StabilityType.Stable);
            if (region > 0.0f)
                return new StabilityReport(name + " is partially stable", StabilityType.PartiallyStable);
            return new StabilityReport(name + " is unstable", StabilityType.Unstable);
        }

    }
}
