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
        static Rect wnd_rect = new Rect(100.0f, 100.0f, 400.0f, 400.0f);
        public static bool shown = false;
        static string wndname = "Static stability tool";

        public static void OnGUI()
        {
            if (shown)
            {
                wnd_rect = GUI.Window(54665949, wnd_rect, _drawGUI, wndname);
            }
        }

        static void _drawGUI(int id)
        {
            GUI.DragWindow();
        }

        public static void save_settings()
        {
            try
            {
                PluginConfiguration conf = PluginConfiguration.CreateForType<CorrectCoL>();
                Debug.Log("[CorrectCoL]: serializing");
                conf.SetValue("x", wnd_rect.x.ToString());
                conf.SetValue("y", wnd_rect.y.ToString());
                conf.save();
            }
            catch (Exception) { }
        }

        public static void load_settings()
        {
            try
            {
                PluginConfiguration conf = PluginConfiguration.CreateForType<CorrectCoL>();
                conf.load();
                Debug.Log("[CorrectCoL]: deserializing");
                wnd_rect.x = conf.GetValue<float>("x");
                wnd_rect.y = conf.GetValue<float>("y");
            }
            catch (Exception) { }
        }
    }
}
