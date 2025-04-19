using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace WotModpackLoader
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Automatycznie wykryj folder gry przy uruchomieniu
            var detectedPath = TryDetectWotPath();
            if (!string.IsNullOrEmpty(detectedPath))
                GameFolderTextBox.Text = detectedPath;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Wybierz folder główny gry World of Tanks";
            dialog.ShowNewFolderButton = false;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GameFolderTextBox.Text = dialog.SelectedPath;
            }
        }

        private void GameFolderTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            NextButton.IsEnabled = !string.IsNullOrWhiteSpace(GameFolderTextBox.Text)
                                   && Directory.Exists(GameFolderTextBox.Text);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var modsWindow = new ModsWindow();
            modsWindow.Owner = this;
            modsWindow.Show();
            this.Hide();
        }

        /// <summary>
        /// Próbuje wykryć folder gry World of Tanks:
        /// 1. Z rejestru (wersje 32/64-bit)
        /// 2. Najpopularniejsze ścieżki
        /// 3. Dynamiczne wyszukiwanie folderów zaczynających się od "World_of_Tanks"
        /// </summary>
        private string? TryDetectWotPath()
        {
            // 1. Rejestr 64-bit
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Wargaming.net\WorldOfTanks"))
                {
                    var installFolder = key?.GetValue("InstallFolder") as string;
                    if (!string.IsNullOrEmpty(installFolder) && Directory.Exists(installFolder))
                        return installFolder;
                }
            }
            catch { /* zignoruj */ }

            // 2. Rejestr 32-bit
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wargaming.net\WorldOfTanks"))
                {
                    var installFolder = key?.GetValue("InstallFolder") as string;
                    if (!string.IsNullOrEmpty(installFolder) && Directory.Exists(installFolder))
                        return installFolder;
                }
            }
            catch { /* zignoruj */ }

            // 3. Popularne lokalizacje (dodane EU oraz dynamiczne ścieżki)
            string[] candidates = new[]
            {
                @"C:\Games\World_of_Tanks",
                @"C:\Games\World_of_Tanks_EU",
                @"C:\Program Files (x86)\World_of_Tanks",
                @"C:\Program Files (x86)\World_of_Tanks_EU",
                @"C:\Program Files\World_of_Tanks",
                @"C:\Program Files\World_of_Tanks_EU",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "World_of_Tanks"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "World_of_Tanks_EU"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "World_of_Tanks"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "World_of_Tanks_EU")
            };
            foreach (var path in candidates)
            {
                if (Directory.Exists(path))
                    return path;
            }

            // 4. Dynamiczne wyszukiwanie - C:\Games
            try
            {
                string gamesDir = @"C:\Games";
                if (Directory.Exists(gamesDir))
                {
                    var dirs = Directory.GetDirectories(gamesDir, "World_of_Tanks*", SearchOption.TopDirectoryOnly);
                    if (dirs.Length > 0)
                        return dirs[0];
                }
            }
            catch { /* zignoruj */ }

            // 5. Dynamiczne wyszukiwanie - Program Files (x86)
            try
            {
                string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (Directory.Exists(pf86))
                {
                    var dirs = Directory.GetDirectories(pf86, "World_of_Tanks*", SearchOption.TopDirectoryOnly);
                    if (dirs.Length > 0)
                        return dirs[0];
                }
            }
            catch { /* zignoruj */ }

            // 6. Dynamiczne wyszukiwanie - Program Files
            try
            {
                string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                if (Directory.Exists(pf))
                {
                    var dirs = Directory.GetDirectories(pf, "World_of_Tanks*", SearchOption.TopDirectoryOnly);
                    if (dirs.Length > 0)
                        return dirs[0];
                }
            }
            catch { /* zignoruj */ }

            // 7. Nie znaleziono - użytkownik musi wybrać ręcznie
            return null;
        }
    }
}