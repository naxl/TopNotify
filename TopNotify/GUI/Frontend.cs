﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopNotify.Common;
using TopNotify.Daemon;
using WebFramework;
using WebFramework.Backend;
using WebFramework.PT;

namespace TopNotify.GUI
{
    public class Frontend : WebScript
    {
        static bool isSaving = false;

        public override async Task DOMContentLoaded()
        {
            Document.Body.AddEventListener("spawnTestNotification", SpawnTestNotification);
            Document.Body.AddEventListener("uploadConfig", UploadConfig);
            Document.Body.AddEventListener("reactReady", ReactReady);
        }

        //Runs After The First React Render (Called Twice Sometimes)
        public async void ReactReady(JSEvent e)
        {
            Document.RunFunction("window.SetIsUWP", Util.FindExe().Contains("WindowsApps").ToString());
            Document.RunFunction("window.SetConfig", File.ReadAllText(Settings.GetFilePath()));
        }

        //Create A Test Notification
        public async void SpawnTestNotification(JSEvent e)
        {
            NotificationTester.Toast("Test Notification", "This Is A Test Notification");
        }

        //Called By JavaScript
        //Resizes The Window To Be The Same Size As A Notification
        //The User Then Drags The Window To The Position Of The Notification
        [JSFunction("EnterDragMode")]
        public static void EnterDragMode()
        {
            Logger.LogInfo("Entering Drag Mode");

            var native = (WindowManager.MainWindow as PTWebWindow).Native;
            var currentConfig = Settings.Get();

            native.Size = new System.Drawing.Size(396, 152 - 32); // Titlebar Height Is 32px
            native.Location = new Point(currentConfig.CustomPositionX, currentConfig.CustomPositionY + 32); // Set Position To What's In The Config
        }

        //Called By JavaScript
        //Captures Window Position And Resizes The Window To Be The Default
        [JSFunction("ExitDragMode")]
        public static void ExitDragMode()
        {
            Logger.LogInfo("Exiting Drag Mode");

            var native = (WindowManager.MainWindow as PTWebWindow).Native;
            var hwnd = (IntPtr)native.WindowHandle;

            //Find Position Of Window
            Rectangle DragRect = new Rectangle();
            NativeInterceptor.GetWindowRect(hwnd, ref DragRect);

            //Write It To The Config
            var currentConfig = Settings.Get();
            currentConfig.CustomPositionX = DragRect.X;
            currentConfig.CustomPositionY = DragRect.Y - 32; // Titlebar Height Is 32px
            UploadConfig(JsonConvert.SerializeObject(currentConfig));

            native.Size = new System.Drawing.Size(520, 780);
            native.Location = new Point(40, 60);
        }

        public static async void UploadConfig(JSEvent e)
        {
            UploadConfig(e.Data["newConfig"].ToString());
        }

        //Write Settings File
        public static async void UploadConfig(string data)
        {

            if (isSaving) { return; }
            isSaving = true;

            try
            {
                File.WriteAllText(Settings.GetFilePath(), data);
                Settings.Validate(Settings.Get());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }

            await Task.Delay(100); // Prevent Crashing Daemon From Spamming Button

            isSaving = false;
        }

        [JSFunction("OpenAppFolder")]
        public static async Task OpenAppFolder()
        {
            Process.Start("explorer.exe", await SharedIO.File.GetAppdataDirectory());
        }
    }
}
