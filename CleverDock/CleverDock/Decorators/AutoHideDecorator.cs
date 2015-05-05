﻿using CleverDock.Helpers;
using CleverDock.Managers;
using CleverDock.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace CleverDock.Decorators
{
    public class AutoHideDecorator
    {
        private MainWindow window;
        public bool DockIsVisible = true;
        public double HotspotHeight = 10;

        private Timer dockShowTimer = null;
        private Timer dockHideTimer = null;

        public AutoHideDecorator(MainWindow _window)
        {
            window = _window;

            WindowManager.Manager.ActiveWindowRectChanged += Manager_ActiveWindowRectChanged;
            WindowManager.Manager.CursorPositionChanged += Manager_CursorPositionChanged;
            SettingsManager.Settings.PropertyChanged += Settings_PropertyChanged;

            dockHideTimer = new Timer(SettingsManager.Settings.DockHideDelay);
            dockHideTimer.Elapsed += dockHideTimer_Elapsed;
            dockHideTimer.AutoReset = false;
            dockShowTimer = new Timer(SettingsManager.Settings.DockShowDelay);
            dockShowTimer.Elapsed += dockShowTimer_Elapsed;
            dockShowTimer.AutoReset = false;
        }

        void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AutoHide")
            {
                if (!SettingsManager.Settings.AutoHide && !DockIsVisible)
                    ShowDock();
            }
        }

        void Manager_CursorPositionChanged(object sender, Handlers.CursorPosEventArgs e)
        {
            if (!SettingsManager.Settings.AutoHide)
                return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!DockIsVisible && !ShouldShowDock)
                {
                    dockShowTimer.Stop();
                    dockShowTimer.Start();
                }
                else if (DockIsVisible && !ShouldHideDock)
                {
                    dockHideTimer.Stop();
                    dockHideTimer.Start();
                }
            });
        }

        void dockHideTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ShouldHideDock)
                    HideDock();
            });
        }

        void dockShowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ShouldShowDock)
                    ShowDock();
            });
        }

        bool ShouldShowDock
        {
            get
            {
                if (DockIsVisible)
                    return false;
                bool hover = window.DockIcons.IsMouseOver;
                bool isInHotspot = MouseHotspot.Contains(WindowManager.Manager.CursorPosition);
                return !DockIsVisible && isInHotspot;
            }
        }

        private bool ShouldHideDock
        {
            get
            {
                if (!DockIsVisible)
                    return false;
                var activeWindow = new Model.Window(WindowManager.Manager.ActiveWindow);
                if (activeWindow.FileName.EndsWith("explorer.exe") && activeWindow.Title == "")
                    return false; // Explorer.exe with no title should be the Desktop in most cases.
                return WindowManager.Manager.ActiveWindowRect.IntersectsWith(Rect) // Dock intersects with foreground window
                    && DockIsVisible
                    && !MouseHotspot.Contains(WindowManager.Manager.CursorPosition) // Mouse is not in hotspot 
                    && !window.DockIcons.IsMouseOver // Mouse is not over the dock icons.
                    && !window.ContextMenuOpened;
            }
        }

        private Rect MouseHotspot
        {
            get
            {
                double hotspotWidth = Math.Max(window.DockIcons.ActualWidth, ScreenHelper.ScreenWidth / 2);
                double hotspotLeft = (ScreenHelper.ScreenWidth - hotspotWidth) / 2;
                double hotspotHeight = DockIsVisible ? window.DockIcons.Height : HotspotHeight;
                return new Rect(hotspotLeft, ScreenHelper.ScreenHeight - hotspotHeight, hotspotWidth, hotspotHeight);
            }
        }

        private Rect Rect
        {
            get { return new Rect(window.DockLeft, window.DockTop + window.TopPadding, window.DockIcons.ActualWidth, window.DockIcons.Height); }
        }

        void Manager_ActiveWindowRectChanged(object sender, Handlers.WindowRectEventArgs e)
        {
            if (!SettingsManager.Settings.AutoHide)
                return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                Rect rect = e.Rect; // Implicit conversion to Windows.Rect
                bool intersects = rect.IntersectsWith(Rect); // Check if the active window is intersecting with the dock.
                bool hover = window.DockIcons.IsMouseOver;
                var activeWindow = new Model.Window(WindowManager.Manager.ActiveWindow);
                bool isDesktop = activeWindow.FileName.EndsWith("explorer.exe") && activeWindow.Title == "";
                if (intersects && !hover && !isDesktop)
                    HideDock();
                else
                    ShowDock();
            });
        }

        public void HideDock()
        {
            if (!DockIsVisible)
                return;
            double dockHeight = window.DockIcons.ActualHeight + window.Distance + window.TopPadding;
            AnimationTools.TranslateY(SettingsManager.Settings.DockHideDuration, dockHeight, Canvas.TopProperty, window.DockIcons);
            DockIsVisible = false;
        }

        public void ShowDock()
        {
            if (DockIsVisible)
                return;
            AnimationTools.TranslateY(SettingsManager.Settings.DockShowDuration, window.TopPadding, Canvas.TopProperty, window.DockIcons);
            DockIsVisible = true;
        }
    }
}