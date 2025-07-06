﻿// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

using Microsoft.Vault.Library;
using System;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.Vault.Explorer
{
    using System.IO;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Desktop;
    using Microsoft.Vault.Explorer.Common;
    using Microsoft.Vault.Explorer.Controls.MessageBox;
    using Microsoft.Vault.Explorer.Dialogs.Exceptions;

    public static class Program
    {
        private static readonly System.Windows.Forms.Timer IdleTimer = new System.Windows.Forms.Timer();
        private static readonly int TimeIntervalForApplicationIdle = (int)TimeSpan.FromHours(1).TotalMilliseconds;
        private static readonly int TimeIntervalForUserInput =  (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IdleTimer.Enabled = false;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += (s, e) => DeleteTokenCacheOnApplicationExit();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => TrackExceptionAndShowError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => TrackExceptionAndShowError(e.ExceptionObject as Exception);
            AppDomain.CurrentDomain.AssemblyResolve += (s, args) => ResolveMissingAssembly(args);

            CheckUpdateSettings();

            // First run install steps
            Utils.ClickOnce_SetAddRemoveProgramsIcon();
            ActivationUri.RegisterVaultProtocol();
            // In case ActivationUri was passed perform the action and exit
            //Add a message filter to check if application is idle
            LeaveIdleMessageFilter limf = new LeaveIdleMessageFilter(IdleTimer);
            Application.AddMessageFilter(limf);
            Application.Idle += new EventHandler(ApplicationIdle);
            IdleTimer.Interval = TimeIntervalForApplicationIdle;
            IdleTimer.Tick += TimeDone;
            var form = new MainForm(ActivationUri.Parse());
            if (!form.IsDisposed)
            {
                Application.Run(form);
            }
        }

        private static void CheckUpdateSettings()
        {
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();

                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.Save();
            }

            // Setup configuration files in proper user data directory if using default location
            SetupConfigurationFiles();
        }

        /// <summary>
        /// Sets up configuration files in proper user data directory when JsonConfigurationFilesRoot is set to default
        /// </summary>
        private static void SetupConfigurationFiles()
        {
            try
            {
                // Only setup if using default location
                if (Settings.Default.JsonConfigurationFilesRoot != @".\")
                {
                    return; // User has customized the location, don't interfere
                }

                // Define target directory
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string configDir = System.IO.Path.Combine(localAppData, "VaultExplorer", "Config");

                // Create directory if it doesn't exist
                if (!System.IO.Directory.Exists(configDir))
                {
                    System.IO.Directory.CreateDirectory(configDir);
                }

                // List of configuration files to copy
                string[] configFiles = { "Vaults.json", "VaultAliases.json", "SecretKinds.json", "CustomTags.json" };
                string appDir = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
                appDir = Path.Combine(appDir, "Config", "Templates");

                bool anyFilesCopied = false;

                foreach (string configFile in configFiles)
                {
                    string sourceFile = System.IO.Path.Combine(appDir, configFile);
                    string targetFile = System.IO.Path.Combine(configDir, configFile);

                    // Only copy if source exists and target doesn't exist
                    if (System.IO.File.Exists(sourceFile) && !System.IO.File.Exists(targetFile))
                    {
                        System.IO.File.Copy(sourceFile, targetFile);
                        anyFilesCopied = true;
                    }
                }

                // Update the setting to point to the new location if we copied any files
                if (anyFilesCopied || System.IO.Directory.GetFiles(configDir, "*.json").Length > 0)
                {
                    Settings.Default.JsonConfigurationFilesRoot = configDir;
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Failed to setup configuration files: {ex.Message}");
                // Application will continue using the default location
            }
        }

        /// <summary>
        /// Delete Token cache on Application Exit
        /// </summary>
        private static void DeleteTokenCacheOnApplicationExit()
        {
            FileTokenCache.ClearAllFileTokenCaches();
        }

        /// <summary>
        /// Microsoft.PS.Common.Vault.dll was renamed to Microsoft.Vault.Library.dll
        /// For backward compatibility reasons, to be able to deserialize old Vaults.json we resolve the missing
        /// assembly and point to our new Microsoft.Vault.Library.dll
        /// </summary>
        /// <seealso cref="BackwardCompatibility.cs"/>
        private static Assembly ResolveMissingAssembly(ResolveEventArgs args)
        {
            if (args.Name == "Microsoft.PS.Common.Vault")
            {
                var vaultLibrary = from a in AppDomain.CurrentDomain.GetAssemblies() where a.GetName().Name == "Microsoft.Vault.Library" select a;
                return vaultLibrary.FirstOrDefault();
            }
            return null;
        }

        /// <summary>
        /// Event Handler for Application Idle. Starts the Idle Timer 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static private void ApplicationIdle(Object sender, EventArgs e)
        {
            if (!IdleTimer.Enabled)
            {
                IdleTimer.Start();
            }
        }

        /// <summary>
        /// Called when Application is idle for the configured time interval
        /// </summary>
        static private void TimeDone(object sender, EventArgs e)
        {
            IdleTimer.Stop();
            const string message = "VaultExplorer is being closed due to inactivity. Do you want to continue working on it?";
            const string caption = "Closing Vault Explorer";
            using (AutoClosingMessageBox autoClosingMessageBox = new AutoClosingMessageBox(TimeIntervalForUserInput))
            {
                var result = autoClosingMessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (result == DialogResult.No)
                {
                    Application.Exit();
                }
                else if (result == DialogResult.Yes)
                {
                    IdleTimer.Stop();
                }
            }
        }

        private static void TrackExceptionAndShowError(Exception e)
        {
            if (e is OperationCanceledException)
            {
                if (UxOperation.WasUserCancelled.Value)
                {
                    return;
                }
            }
            // Show error
            var ed = new ExceptionDialog(e);
            ed.ShowDialog();
        }
    }

    /// <summary>
    /// This class filters (listens to) all messages for the application and if
    /// a relevant message (such as mouse or keyboard) is received then it resets the timer.
    /// </summary>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    public class LeaveIdleMessageFilter : IMessageFilter
    {
        const int WM_NCLBUTTONDOWN = 0x00A1;
        const int WM_NCLBUTTONUP = 0x00A2;
        const int WM_NCRBUTTONDOWN = 0x00A4;
        const int WM_NCRBUTTONUP = 0x00A5;
        const int WM_NCMBUTTONDOWN = 0x00A7;
        const int WM_NCMBUTTONUP = 0x00A8;
        const int WM_NCXBUTTONDOWN = 0x00AB;
        const int WM_NCXBUTTONUP = 0x00AC;
        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;
        const int WM_MOUSEMOVE = 0x0200;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_RBUTTONUP = 0x0205;
        const int WM_MBUTTONDOWN = 0x0207;
        const int WM_MBUTTONUP = 0x0208;
        const int WM_XBUTTONDOWN = 0x020B;
        const int WM_XBUTTONUP = 0x020C;

        // The Messages array must be sorted due to use of Array.BinarySearch
        static readonly int[] ActivityMessages = new int[] {WM_NCLBUTTONDOWN,
            WM_NCLBUTTONUP, WM_NCRBUTTONDOWN, WM_NCRBUTTONUP, WM_NCMBUTTONDOWN,
            WM_NCMBUTTONUP, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, WM_KEYDOWN, WM_KEYUP,
            WM_LBUTTONDOWN, WM_LBUTTONUP, WM_RBUTTONDOWN, WM_RBUTTONUP,
            WM_MBUTTONDOWN, WM_MBUTTONUP, WM_XBUTTONDOWN, WM_XBUTTONUP};

        private readonly System.Windows.Forms.Timer timer;

        public LeaveIdleMessageFilter(System.Windows.Forms.Timer timer)
        {
            if (timer == null)
            {
                throw new ArgumentNullException(nameof(timer));
            }

            this.timer = timer;
        }

        public bool PreFilterMessage(ref Message m)
        {
            // Stop the idle timer if we see user interaction message
            if (this.timer.Enabled && Array.BinarySearch(ActivityMessages, m.Msg) >= 0)
            {
                this.timer.Stop();
            }

            return false;
        }
    }
}
