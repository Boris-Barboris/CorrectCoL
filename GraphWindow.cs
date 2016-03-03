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
using KSP.IO;

namespace CorrectCoL
{
    public static class GraphWindow
    {
        public const int wnd_width = 620;
        public const int wnd_height = 485;

        static Rect wnd_rect = new Rect(100.0f, 100.0f, wnd_width, wnd_height);
        public static bool shown = false;
        static PluginConfiguration conf;

        public static void OnGUI()
        {
            if (shown)
            {
                wnd_rect = GUI.Window(54665949, wnd_rect, _drawGUI, "Static stability analysis");
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
            conf.save();
        }

        public static void load_settings()
        {
            if (conf == null)
                conf = PluginConfiguration.CreateForType<CorrectCoL>();
            try
            {
                conf.load();
                Debug.Log("[CorrectCoL]: deserializing");
                Debug.Log("[CorrectCoL]: x = " + float.Parse(conf.GetValue<string>("x")).ToString());
                Debug.Log("[CorrectCoL]: y = " + float.Parse(conf.GetValue<string>("y")).ToString());
                wnd_rect.x = float.Parse(conf.GetValue<string>("x"));
                wnd_rect.y = float.Parse(conf.GetValue<string>("y"));
            }
            catch (Exception) { }
        }

        public const int graph_width = 450;
        public const int graph_height = 200;
        static Texture2D pitch_texture = new Texture2D(graph_width, graph_height);
        static Texture2D yaw_texture = new Texture2D(graph_width, graph_height);

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
                pitch_texture.Apply(false);
                yaw_texture.Apply(false);
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

            // marks
            int.TryParse(aoa_mark_delta_str, out aoa_mark_delta);
            int mark_delta = Math.Max(1, aoa_mark_delta);
            y0 = graph_height / 2 - 5;
            y1 = graph_height / 2 + 5;

            float aoa = mark_delta;
            while (aoa < 180.0f)
            {
                x0 = aoa2pixel(aoa);
                DrawLine(pitch_texture, x0, y0, x0, y1, Color.white);
                DrawLine(yaw_texture, x0, y0, x0, y1, Color.white);
                aoa += mark_delta;
            }
            aoa = -mark_delta;
            while (aoa > -180.0f)
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
            if (EditorLogic.fetch.ship != null && EditorLogic.fetch.ship.parts.Count > 0)
            {
                // here we calculate aerodynamics
                CorrectCoL.CoLMarkerFull.force_occlusion_update_recurse(EditorLogic.RootPart);
                calculate_moments();
                init_axes();
                draw_moments();
            }
            pitch_texture.Apply(false);
            yaw_texture.Apply(false);
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

        const int num_pts = 40;
        static List<float> AoA_net = new List<float>(num_pts * 2);
        static float aoa_scaling = 1.0f;
        static float aoa_compress = 1.0f;
        static string aoa_compress_str = 1.0f.ToString();

        static Vector3 CoM = Vector3.zero;

        static float[] wet_torques_aoa = new float[num_pts * 2 - 1];
        static float[] dry_torques_aoa = new float[num_pts * 2 - 1];
        static float[] wet_torques_sideslip = new float[num_pts * 2 - 1];
        static float[] dry_torques_sideslip = new float[num_pts * 2 - 1];

        static void calculate_moments()
        {
            float.TryParse(aoa_compress_str, out aoa_compress);
            aoa_compress = Mathf.Min(Mathf.Max(0.0f, aoa_compress), 1e5f);
            double.TryParse(alt_str, out altitude);
            float.TryParse(speed_str, out speed);

            // let's build aoa transformation net
            AoA_net.Clear(); 
            float x_max = num_pts - 1.0f;
            aoa_scaling = 180.0f / (x_max * (1.0f + aoa_compress * x_max));
            for (int i = -num_pts + 1; i < num_pts; i++)
            {
                float x = i;
                float y = aoa_scaling * (Mathf.Abs(x) + aoa_compress * x * x) * Mathf.Sign(x);
                AoA_net.Add(y);
            }

            // force occlusion update on the craft
            CorrectCoL.CoLMarkerFull.force_occlusion_update_recurse(EditorLogic.RootPart);
            // update CoM
            CoM = EditorMarker_CoM.findCenterOfMass(EditorLogic.RootPart);

            // wet cycles
            for (int i = 0; i < AoA_net.Count; i++)
            {
                float aoa = AoA_net[i];
                Vector3 sum_torque = get_torque_aoa(aoa);
                wet_torques_aoa[i] = Vector3.Dot(sum_torque, EditorLogic.RootPart.partTransform.right);
            }

            for (int i = 0; i < AoA_net.Count; i++)
            {
                float aoa = AoA_net[i];
                Vector3 sum_torque = get_torque_sideslip(aoa);
                wet_torques_sideslip[i] = Vector3.Dot(sum_torque, EditorLogic.RootPart.partTransform.forward);
            }

            // dry the ship

            // dry cycles

            // wet ship back
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
            // pitch wet moments
            float max_pmoment = Mathf.Max(Mathf.Abs(wet_torques_aoa.Max()), Mathf.Abs(wet_torques_aoa.Min()));
            for (int i = 0; i < AoA_net.Count - 1; i++)
            {
                int x0 = aoa2pixel(AoA_net[i]);
                int x1 = aoa2pixel(AoA_net[i + 1]);
                int y0 = (int)(Mathf.Round((1.0f - wet_torques_aoa[i] / max_pmoment * draw_scale) * graph_height / 2.0f));
                int y1 = (int)(Mathf.Round((1.0f - wet_torques_aoa[i + 1] / max_pmoment * draw_scale) * graph_height / 2.0f));

                DrawLine(pitch_texture, x0, y0, x1, y1, Color.green);
            }

            // yaw wet moments
            float max_ymoment = Mathf.Max(Mathf.Abs(wet_torques_sideslip.Max()), Mathf.Abs(wet_torques_sideslip.Min()));
            for (int i = 0; i < AoA_net.Count - 1; i++)
            {
                int x0 = aoa2pixel(AoA_net[i]);
                int x1 = aoa2pixel(AoA_net[i + 1]);
                int y0 = (int)(Mathf.Round((1.0f - wet_torques_sideslip[i] / max_ymoment * draw_scale) * graph_height / 2.0f));
                int y1 = (int)(Mathf.Round((1.0f - wet_torques_sideslip[i + 1] / max_ymoment * draw_scale) * graph_height / 2.0f));

                DrawLine(yaw_texture, x0, y0, x1, y1, Color.green);
            }
        }

        public static Vector3 get_torque_aoa(float aoa)
        {
            setup_qrys(aoa, 0.0f);
            return get_part_torque_recurs(EditorLogic.RootPart, CoM);
        }

        public static Vector3 get_torque_sideslip(float slip)
        {
            setup_qrys(0.0f, slip);
            return get_part_torque_recurs(EditorLogic.RootPart, CoM);
        }

        static double altitude = 500.0;
        static string alt_str = 500.0.ToString();

        static double pressure, density, sound_speed;

        static float speed = 200.0f;
        static string speed_str = 200.0f.ToString();

        static float mach;

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

        static Vector3 get_part_torque_recurs(Part p, Vector3 CoM)
        {
            if (p == null)
                return Vector3.zero;

            Vector3 tq = get_part_torque(qry, p, CoM);

            for (int i = 0; i < p.children.Count; i++)
                tq += get_part_torque_recurs(p.children[i], CoM);

            return tq;
        }

        public static Vector3 get_part_torque(CenterOfLiftQuery qry, Part p, Vector3 CoM)
        {
            if (p == null)
                return Vector3.zero;

            Vector3 lift_pos = Vector3.zero;
            Vector3 drag_pos = Vector3.zero;
            Vector3 lift_force = Vector3.zero;
            Vector3 drag_force = Vector3.zero;

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
                        p.dragVector = qry.refVector;
                        p.dragVectorSqrMag = p.dragVector.sqrMagnitude;
                        p.dragVectorMag = Mathf.Sqrt(p.dragVectorSqrMag);
                        p.dragVectorDir = p.dragVector / p.dragVectorMag;
                        p.dragVectorDirLocal = -p.partTransform.InverseTransformDirection(p.dragVectorDir);

                        p.dynamicPressurekPa = qry.refAirDensity * 0.0005 * p.dragVectorSqrMag;
                        p.bodyLiftScalar = (float)(p.dynamicPressurekPa * p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier *
                            CorrectCoL.CoLMarkerFull.lift_curves.liftMachCurve.Evaluate(mach));

                        if (p.rb != null)
                        {
                            lift_pos = p.rb.worldCenterOfMass + p.partTransform.rotation * p.CoLOffset;
                            drag_pos = p.rb.worldCenterOfMass + p.partTransform.rotation * p.CoPOffset;
                        }
                        else
                        {
                            lift_pos = p.partTransform.position + p.partTransform.rotation * (p.CoLOffset + p.CoMOffset);
                            drag_pos = p.partTransform.position + p.partTransform.rotation * (p.CoPOffset + p.CoMOffset);
                        }

                        p.DragCubes.SetDrag(p.dragVectorDirLocal, mach);

                        float pseudoreynolds = (float)(density * speed);
                        float pseudoredragmult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate(pseudoreynolds);
                        float drag_k = p.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier * pseudoredragmult;
                        p.dragScalar = (float)(p.dynamicPressurekPa * drag_k * PhysicsGlobals.DragMultiplier);

                        lift_force = p.partTransform.rotation * (p.bodyLiftScalar * p.DragCubes.LiftForce);
                        lift_force = Vector3.ProjectOnPlane(lift_force, -p.dragVectorDir);
                        drag_force = p.dragScalar * -p.dragVectorDir;

                        Vector3 res = Vector3.zero;
                        res -= Vector3.Cross(lift_force, lift_pos - CoM);
                        res -= Vector3.Cross(drag_force, drag_pos - CoM);
                        return res;
                    }
                }
                else
                {
                    Vector3 res = Vector3.zero;
                    for (int i = 0; i < providers.Count; i++)
                    {
                        double q = 0.5 * qry.refAirDensity * qry.refVector.sqrMagnitude;
                        Vector3 dragvect;
                        Vector3 liftvect;
                        float abs;
                        ModuleLiftingSurface lsurf = providers[i];
                        lsurf.SetupCoefficients(qry.refVector, out dragvect, out liftvect, out lsurf.liftDot, out abs);

                        if (p.rb != null)
                        {
                            lift_pos = p.rb.worldCenterOfMass + lsurf.part.partTransform.rotation * lsurf.part.CoLOffset;
                            drag_pos = p.rb.worldCenterOfMass + lsurf.part.partTransform.rotation * lsurf.part.CoPOffset;
                        }
                        else
                        {
                            lift_pos = p.partTransform.position + lsurf.part.partTransform.rotation * (lsurf.part.CoLOffset + lsurf.part.CoMOffset);
                            drag_pos = p.partTransform.position + lsurf.part.partTransform.rotation * (lsurf.part.CoMOffset + lsurf.part.CoPOffset);
                        }


                        lift_force = lsurf.GetLiftVector(liftvect, lsurf.liftDot, abs, q, mach);
                        drag_force = lsurf.GetDragVector(dragvect, abs, q);

                        res -= Vector3.Cross(lift_force, lift_pos - CoM);
                        res -= Vector3.Cross(drag_force, drag_pos - CoM);
                    }
                    return res;
                }
            }

            return Vector3.zero;
        }

    }
}
