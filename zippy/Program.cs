using System;
using System.Diagnostics;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System.Resources;
using System.Reflection;
using Zippy.Properties;

namespace Zippy
{

    public class Utils
    {
        public static bool IsFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    FileAttributes attr = File.GetAttributes(path);
                    return !attr.HasFlag(FileAttributes.Directory);
                }
            }
            catch {}
            return false;
        }
        public static List<string> GetSelectedFiles()
        {
            string filename;
            List<String> selected = new List<String>();
            foreach (SHDocVw.InternetExplorer window in new SHDocVw.ShellWindows())
            {
                //if (!(window.Top == 0))
                //{
                //    continue;
                //}
                filename = Path.GetFileNameWithoutExtension(window.FullName).ToLower();
                if (filename.ToLowerInvariant() == "explorer")
                {
                    Shell32.FolderItems items = ((Shell32.IShellFolderViewDual2)window.Document).SelectedItems();
                    foreach (Shell32.FolderItem item in items)
                    {
                        selected.Add(item.Path);
                    }
                }
            }
            return selected;
        }

        public static void SetStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            rk.SetValue("zippy", Application.ExecutablePath);

        }
    }

    public class KeyboardManager
    {
        private IKeyboardMouseEvents m_GlobalHook;



        public void ExtractInThread()
        {
            Thread newThread = new(Extract);
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start();
        }
         public void Extract()
        {
            var files = Utils.GetSelectedFiles().Distinct().ToList();
            foreach (var file in files)
            {
                //if (!Utils.IsFile(file) || !(file.EndsWith(".zip") || file.EndsWith(".exe")))
                //{
                //    continue;
                //}
                // get absolute path the the same folder of the file
                string folderPath = Path.GetDirectoryName(file);
                string args = $"x \"{file}\" -o\"{folderPath}\\*\"";
                string programPath = "C:\\Program Files\\7-Zip\\7zG.exe";
                if (!Utils.IsFile(programPath))
                {
                    MessageBox.Show("Can't find 7zip. Please Install it from 7-zip.org");
                } else
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = programPath,
                        WorkingDirectory = folderPath,
                        Arguments = args,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                
            }
        }

        public void Subscribe()
        {
            var comb = Combination.TriggeredBy(Keys.E).With(Keys.LControlKey);
            var assignment = new Dictionary<Combination, Action>
             {
                 {comb, ExtractInThread}
             };
            // Note: for the application hook, use the Hook.AppEvents() instead
            Hook.GlobalEvents().OnCombination(assignment);
             
        }

        public void Unsubscribe()
        {

            //It is recommened to dispose it
            m_GlobalHook.Dispose();
        }

    }


    public class MainContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private KeyboardManager km;
        private Utils utils;


        public MainContext()
        {
            km = new KeyboardManager();

            utils = new Utils();
            Utils.SetStartup();

            
            // Initialize the tray icon and menu
            trayIcon = new NotifyIcon()
            {

                Icon = Resources.icon, // Set your icon here
                Text = "Zippy",
                Visible = true
            };

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exit", null, ExitMenuItem_Click);

            trayIcon.ContextMenuStrip = trayMenu;

            //km.Subscribe();
            km.Subscribe();
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // Handle "Exit" menu item click, e.g., close the application
            Application.Exit();
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainContext());
        }
    }
}
