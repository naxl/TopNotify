﻿#define TRACE // Enable Trace.WriteLine

using System;
using System.Diagnostics;
using System.Drawing;
using WebFramework;
using WebFramework.PT;
using Microsoft.Toolkit.Uwp.Notifications;
using WebFramework.Backend;
using TopNotify.Daemon;
using TopNotify.Common;
using TopNotify.GUI;

namespace TopNotify.Common
{
    public class Program
    {

        public static ThemeBasedColor TitlebarColor;

        public static Daemon.Daemon Background;

        [STAThread]
        public static void Main(string[] args)
        {
            //By Default, The App Will Be Launched In Daemon Mode
            //Daemon Mode Is A Background Process That Handles Changing The Position Of Notifications
            //If The "--settings" Arg Is Used, Then The App Will Launch In Settings Mode
            //Settings Mode Shows A GUI That Can Be Used To Configure The App
            //These Mode Switches Ensure All Functions Of The App Use The Same Executable

            //Find Other Instances Of TopNotify
            var validTopNotifyInstances = Process.GetProcessesByName("TopNotify").Where((p) => !p.HasExited && p.Id != Process.GetCurrentProcess().Id);

            var isGUIRunning = validTopNotifyInstances.Where((p) => {
                string commandLine;
                ProcessCommandLine.Retrieve(p, out commandLine, ProcessCommandLine.Parameter.CommandLine);
                return commandLine.ToLower().Contains("--settings");
            }).Any();

            var isDaemonRunning = validTopNotifyInstances.Where((p) => {
                string commandLine;
                ProcessCommandLine.Retrieve(p, out commandLine, ProcessCommandLine.Parameter.CommandLine);
                return !commandLine.ToLower().Contains("--settings");
            }).Any();

            if (!args.Contains("--settings") && isDaemonRunning && !isGUIRunning)
            {
                //Open GUI Instead Of Daemon
                TrayIcon.LaunchSettingsMode(null, null);
                Environment.Exit(1);
            }
            else if (args.Contains("--settings") && isGUIRunning)
            {
                //Exit To Prevent Multiple GUIs
                Environment.Exit(2);
            }
            else if (!args.Contains("--settings") && isDaemonRunning && isGUIRunning)
            {
                //Exit To Prevent Multiple Daemons
                Environment.Exit(3);
            }

            AppManager.Publisher = "SamsidParty";
            AppManager.AppID = "TopNotify";

            if (args.Contains("--debug-process")) { Debugger.Launch(); } // Start Debugging

            if (args.Contains("--settings"))
            {
                //Open The GUI App In Settings Mode
                Logger.SetFileName("gui");

                if (Settings.Get().EnableDebugNotifications)
                {
                    Console.WriteLine("Started Settings GUI");
                    NotificationTester.Toast("Debug Notification", "Started Settings GUI");
                }

                PTWindowProvider.Activate();
                AppManager.Validate(args, "SamsidParty", "TopNotify");
                App();
            }
            else
            {
                //Open The Background Daemon
                Logger.SetFileName("daemon");
                Background = new Daemon.Daemon();
            }

        }

        public static async Task App()
        {

            //DevTools.Enable();
            //DevTools.HotReload("http://127.0.0.1:25631"); // Vite Dev URL
            //Logger.ForceOpenConsole();

            //Change Color Based On Theme (light, dark)
            TitlebarColor = new ThemeBasedColor(Color.FromArgb(255, 255, 255), Color.FromArgb(34, 34, 34));


            WindowManager.Options = new WindowOptions()
            {
                TitlebarColor = TitlebarColor,
                StartWidthHeight = new Rectangle(400, 600, 520, 780),
                LockWidthHeight = true,
                EnableAcrylic = true,
                IconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WWW", "Image", "IconSmall.png")
            };

            WebScript.Register<Frontend>("frontend");

            await AppManager.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WWW"), OnReady);

            //Clean Up
            ToastNotificationManagerCompat.Uninstall();
        }

        public static async Task OnReady(WebWindow w)
        {
            w.BackgroundColor = TitlebarColor;
        }

    }
}

