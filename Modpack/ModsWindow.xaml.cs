using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using PuppeteerSharp;
using System.Linq;
using System.Text.Json;

namespace WotModpackLoader
{
    public partial class ModsWindow : Window
    {
        public ModsWindow()
        {
            InitializeComponent();
            this.Closed += ModsWindow_Closed;
        }

        private void ModsWindow_Closed(object? sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Closed -= ModsWindow_Closed;
            if (this.Owner is MainWindow mainWindow)
                mainWindow.Show();
            this.Close();
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(this.Owner is MainWindow mainWindow) || string.IsNullOrWhiteSpace(mainWindow.GameFolderTextBox.Text) || !Directory.Exists(mainWindow.GameFolderTextBox.Text))
            {
                System.Windows.MessageBox.Show("Nie wskazano prawidłowego folderu gry!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string gameFolder = mainWindow.GameFolderTextBox.Text;

            InstallProgressBar.Visibility = Visibility.Visible;
            InstallProgressBar.Value = 0;
            InstallProgressBar.IsIndeterminate = false;
            try
            {
                int totalSteps = 0;
                if (XvmCheck.IsChecked == true) totalSteps++;
                if (PmodCheck.IsChecked == true) totalSteps++;
                if (BattleEquipmentCheck.IsChecked == true) totalSteps++;
                if (ClanRewardsCheck.IsChecked == true) totalSteps++;

                int currentStep = 0;
                if (totalSteps == 0)
                {
                    System.Windows.MessageBox.Show("Nie wybrano żadnych modów do instalacji!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    InstallProgressBar.Visibility = Visibility.Collapsed;
                    return;
                }

                // Usuwanie mods i res_mods oraz utworzenie folderów z wersją gry
                string wotVersion = await RemoveAndPrepareModsFoldersAsync(gameFolder);

                if (XvmCheck.IsChecked == true)
                {
                    InstallProgressBar.IsIndeterminate = true;
                    await InstallXvmAsync(gameFolder, wotVersion);
                    InstallProgressBar.IsIndeterminate = false;
                    currentStep++;
                    InstallProgressBar.Value = (double)currentStep / totalSteps * 100;
                }
                if (PmodCheck.IsChecked == true)
                {
                    InstallProgressBar.IsIndeterminate = true;
                    await InstallPmodAsync(gameFolder, wotVersion);
                    InstallProgressBar.IsIndeterminate = false;
                    currentStep++;
                    InstallProgressBar.Value = (double)currentStep / totalSteps * 100;
                }
                if (BattleEquipmentCheck.IsChecked == true)
                {
                    InstallProgressBar.IsIndeterminate = true;
                    await InstallBattleEquipmentAsync(gameFolder, wotVersion);
                    InstallProgressBar.IsIndeterminate = false;
                    currentStep++;
                    InstallProgressBar.Value = (double)currentStep / totalSteps * 100;
                }
                if (ClanRewardsCheck.IsChecked == true)
                {
                    InstallProgressBar.IsIndeterminate = true;
                    await InstallClanRewardsAutoClaimAsync(gameFolder, wotVersion);
                    InstallProgressBar.IsIndeterminate = false;
                    currentStep++;
                    InstallProgressBar.Value = (double)currentStep / totalSteps * 100;
                }

                InstallProgressBar.Value = 100;
                await Task.Delay(400);
                System.Windows.MessageBox.Show("Instalacja zakończona!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Wystąpił błąd podczas instalacji modów:\n\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                InstallProgressBar.Visibility = Visibility.Collapsed;
                InstallProgressBar.Value = 0;
                InstallProgressBar.IsIndeterminate = false;
            }
        }

        private async Task<string> RemoveAndPrepareModsFoldersAsync(string gameFolder)
        {
            string modsFolder = Path.Combine(gameFolder, "mods");
            string resModsFolder = Path.Combine(gameFolder, "res_mods");
            try
            {
                if (Directory.Exists(modsFolder))
                    Directory.Delete(modsFolder, true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd przy usuwaniu folderu mods: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            try
            {
                if (Directory.Exists(resModsFolder))
                    Directory.Delete(resModsFolder, true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd przy usuwaniu folderu res_mods: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Pobierz wersję gry
            string version = await GetWotVersionAsync();

            Directory.CreateDirectory(Path.Combine(modsFolder, version));
            Directory.CreateDirectory(Path.Combine(resModsFolder, version));

            return version;
        }

        private async Task<string> GetWotVersionAsync()
        {
            string url = "https://aslain.com/update_checker/WoT_installer.json";
            using (var http = new HttpClient())
            {
                var json = await http.GetStringAsync(url);
                using (var doc = JsonDocument.Parse(json))
                {
                    var version = doc.RootElement.GetProperty("installer").GetProperty("version").GetString();
                    if (!string.IsNullOrEmpty(version))
                    {
                        int lastDot = version.LastIndexOf('.');
                        if (lastDot > 0)
                        {
                            string trimmed = version.Substring(0, lastDot);
                            return trimmed;
                        }
                    }
                    throw new Exception("Nie można pobrać wersji WoT");
                }
            }
        }

        /// <summary>
        /// Kopiuje zawartość folderu mods do folderu wersji, ignorując podfoldery z innymi wersjami niż aktualna.
        /// </summary>
        private void CopyModContentToVersionedFolder(string sourceMods, string destVersionFolder, string wotVersion)
        {
            var subdirs = Directory.GetDirectories(sourceMods);
            var versionFolder = subdirs.FirstOrDefault(d => Path.GetFileName(d) == wotVersion);

            if (versionFolder != null)
            {
                foreach (string dir in Directory.GetDirectories(versionFolder))
                {
                    string destDir = Path.Combine(destVersionFolder, Path.GetFileName(dir));
                    CopyDirectory(dir, destDir);
                }
                foreach (string file in Directory.GetFiles(versionFolder))
                {
                    string destFile = Path.Combine(destVersionFolder, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
            }
            else
            {
                foreach (string dir in subdirs)
                {
                    string dirName = Path.GetFileName(dir);
                    if (IsVersionString(dirName) && dirName != wotVersion)
                        continue;
                    string destDir = Path.Combine(destVersionFolder, dirName);
                    CopyDirectory(dir, destDir);
                }
                foreach (string file in Directory.GetFiles(sourceMods))
                {
                    string destFile = Path.Combine(destVersionFolder, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
            }
        }

        /// <summary>
        /// Sprawdza, czy string wygląda jak wersja WoT (np. 1.28.1.0)
        /// </summary>
        private bool IsVersionString(string name)
        {
            return name.Count(c => c == '.') == 3 && name.All(c => char.IsDigit(c) || c == '.');
        }

        private async Task InstallPmodAsync(string gameFolder, string wotVersion)
        {
            string? pmodZipUrl = await GetPmodZipUrlAsync();
            if (string.IsNullOrEmpty(pmodZipUrl))
                throw new Exception("Nie znaleziono linku do pobrania PMOD!");

            string tempFolder = Path.Combine(gameFolder, ".tempModpackPMOD");
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);

            string zipPath = Path.Combine(tempFolder, "pmod.zip");
            using (var http = new HttpClient())
            using (var resp = await http.GetAsync(pmodZipUrl))
            {
                resp.EnsureSuccessStatusCode();
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                {
                    await resp.Content.CopyToAsync(fs);
                }
            }

            ZipFile.ExtractToDirectory(zipPath, tempFolder);

            var modsDirs = Directory.GetDirectories(tempFolder, "mods", SearchOption.AllDirectories);
            var pmodMods = modsDirs.FirstOrDefault();
            if (pmodMods != null)
            {
                string dest = Path.Combine(gameFolder, "mods", wotVersion);
                if (!Directory.Exists(dest))
                    Directory.CreateDirectory(dest);
                CopyModContentToVersionedFolder(pmodMods, dest, wotVersion);
            }
            else
            {
                throw new Exception("Nie znaleziono folderu 'mods' w paczce PMOD.");
            }

            Directory.Delete(tempFolder, true);
        }

        private async Task InstallXvmAsync(string gameFolder, string wotVersion)
        {
            string? xvmZipUrl = await GetXvmZipUrlAsync();
            if (string.IsNullOrEmpty(xvmZipUrl))
                throw new Exception("Nie znaleziono linku do pobrania XVM!");

            string tempFolder = Path.Combine(gameFolder, ".tempModpackXVM");
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);

            string zipPath = Path.Combine(tempFolder, "xvm.zip");
            using (var http = new HttpClient())
            using (var resp = await http.GetAsync(xvmZipUrl))
            {
                resp.EnsureSuccessStatusCode();
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                {
                    await resp.Content.CopyToAsync(fs);
                }
            }

            ZipFile.ExtractToDirectory(zipPath, tempFolder);

            string wgFolder = Path.Combine(tempFolder, "wg");
            foreach (var dir in new[] { "mods", "res_mods" })
            {
                string source = Path.Combine(wgFolder, dir);
                if (Directory.Exists(source))
                {
                    string dest = Path.Combine(gameFolder, dir, wotVersion);
                    if (!Directory.Exists(dest))
                        Directory.CreateDirectory(dest);
                    CopyModContentToVersionedFolder(source, dest, wotVersion);
                }
            }

            Directory.Delete(tempFolder, true);
        }

        private async Task InstallBattleEquipmentAsync(string gameFolder, string wotVersion)
        {
            string? zipUrl = await GetBattleEquipmentZipUrlAsync();
            if (string.IsNullOrEmpty(zipUrl))
                throw new Exception("Nie znaleziono linku do pobrania Battle Equipment!");

            string tempFolder = Path.Combine(gameFolder, ".tempModpackBE");
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);

            string zipPath = Path.Combine(tempFolder, "battleequipment.zip");
            using (var http = new HttpClient())
            using (var resp = await http.GetAsync(zipUrl))
            {
                resp.EnsureSuccessStatusCode();
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                {
                    await resp.Content.CopyToAsync(fs);
                }
            }

            ZipFile.ExtractToDirectory(zipPath, tempFolder);

            var modsDirs = Directory.GetDirectories(tempFolder, "mods", SearchOption.AllDirectories);
            var modMods = modsDirs.FirstOrDefault();
            if (modMods != null)
            {
                string dest = Path.Combine(gameFolder, "mods", wotVersion);
                if (!Directory.Exists(dest))
                    Directory.CreateDirectory(dest);
                CopyModContentToVersionedFolder(modMods, dest, wotVersion);
            }
            else
            {
                throw new Exception("Nie znaleziono folderu 'mods' w paczce Battle Equipment.");
            }

            Directory.Delete(tempFolder, true);
        }

        private async Task InstallClanRewardsAutoClaimAsync(string gameFolder, string wotVersion)
        {
            string modUrl = "https://github.com/Vretu-Dev/Modpack/raw/master/Mods/mod_wb_auto_claim_clan_reward.wotmod";
            string destDir = Path.Combine(gameFolder, "mods", wotVersion);
            Directory.CreateDirectory(destDir);

            string modFileName = "mod_wb_auto_claim_clan_reward.wotmod";
            string destFilePath = Path.Combine(destDir, modFileName);

            using (var http = new HttpClient())
            using (var resp = await http.GetAsync(modUrl))
            {
                resp.EnsureSuccessStatusCode();
                using (var fs = new FileStream(destFilePath, FileMode.Create, FileAccess.Write))
                {
                    await resp.Content.CopyToAsync(fs);
                }
            }
        }

        private async Task<string?> GetPmodZipUrlAsync()
        {
            const string url = "https://wotmods.net/world-of-tanks-mods/user-interface/pmod/";
            using (var http = new HttpClient())
            {
                var html = await http.GetStringAsync(url);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                var div = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'download-attachments')]");
                if (div != null)
                {
                    var a = div.SelectSingleNode(".//a[@href]");
                    if (a != null)
                    {
                        var href = a.GetAttributeValue("href", "");
                        if (href.StartsWith("/"))
                            href = "https://wotmods.net" + href;
                        return href;
                    }
                }
                return null;
            }
        }

        private async Task<string?> GetXvmZipUrlAsync()
        {
            const string xvmPage = "https://modxvm.com/en/download-xvm/";
            using (var http = new HttpClient())
            {
                var html = await http.GetStringAsync(xvmPage);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);
                var node = doc.DocumentNode
                    .SelectSingleNode("//div[contains(@class,'downloads-info')]//a[contains(@href,'.zip')]");
                if (node == null)
                    return null;

                var attr = node.Attributes["href"];
                return attr?.Value;
            }
        }

        private async Task<string?> GetBattleEquipmentZipUrlAsync()
        {
            const string pageUrl = "https://github.com/Kurzdor/wotmods-public/tree/master/battleequipment";
            using (var http = new HttpClient())
            {
                var html = await http.GetStringAsync(pageUrl);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                var node = doc.DocumentNode.SelectSingleNode("//a[contains(@href, '.zip')]");
                if (node != null)
                {
                    var href = node.GetAttributeValue("href", "");
                    href = href.Replace("/blob/", "/raw/");
                    if (href.StartsWith("/"))
                        href = "https://github.com" + href;
                    return href;
                }
                return null;
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }
    }
}