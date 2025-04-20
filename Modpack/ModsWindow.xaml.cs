using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using PuppeteerSharp;
using System.Linq;
using System.Text.RegularExpressions;
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
                if (MarksOnGunExtendedCheck.IsChecked == true) totalSteps++;
                if (BattleEquipmentCheck.IsChecked == true) totalSteps++;
                if (ClanRewardsCheck.IsChecked == true) totalSteps++;
                if (TechTreeCheck.IsChecked == true) totalSteps++;
                if (ExtendedBlacklistCheck.IsChecked == true) totalSteps++;

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
                if (MarksOnGunExtendedCheck.IsChecked == true)
                {
                    InstallProgressBar.IsIndeterminate = true;
                    await InstallMarksOnGunExtendedAsync(gameFolder, wotVersion);
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
                if (TechTreeCheck.IsChecked == true)
                {
                    InstallProgressBar.IsIndeterminate = true;
                    await InstallTechTreeAsync(gameFolder, wotVersion);
                    InstallProgressBar.IsIndeterminate = false;
                    currentStep++;
                    InstallProgressBar.Value = (double)currentStep / totalSteps * 100;
                }
                if (ExtendedBlacklistCheck.IsChecked == true)
                {
                    InstallProgressBar.IsIndeterminate = true;
                    await InstallExtendedBlacklistAsync(gameFolder, wotVersion);
                    InstallProgressBar.IsIndeterminate = false;
                    currentStep++;
                    InstallProgressBar.Value = (double)currentStep / totalSteps * 100;
                }

                string modsPath = Path.Combine(gameFolder, "mods", wotVersion);
                RemoveOlderModVersions(modsPath, "me.poliroid.modslistapi");
                RemoveOlderModVersions(modsPath, "izeberg.modssettingsapi");

                InstallProgressBar.Value = 100;
                await Task.Delay(400);
                var win = new ModInstalledWindow { Owner = this };
                win.ShowDialog();
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

        // Pomocnicza funkcja
        private bool IsVersionString(string name)
        {
            return name.Count(c => c == '.') == 3 && name.All(c => char.IsDigit(c) || c == '.');
        }
        /// <summary>
        /// Kopiuje całą zawartość mods/ i res_mods/ z archiwum do gry:
        /// - wszystko z mods/<wersja_gry> → mods/<wersja_gry>
        /// - wszystko z res_mods/<wersja_gry> → res_mods/<wersja_gry>
        /// - pliki i foldery z mods/ (ale NIE mods/wersja_gry) → mods/
        /// - pliki i foldery z res_mods/ (ale NIE res_mods/wersja_gry) → res_mods/
        /// - nigdy nie tworzy mods/mods ani res_mods/res_mods
        /// </summary>
        private void CopyModsAndResMods(string tempFolder, string gameFolder, string wotVersion)
        {
            // MODS
            string modsInArchive = Path.Combine(tempFolder, "mods");
            string modsDest = Path.Combine(gameFolder, "mods");
            string modsVersionDest = Path.Combine(modsDest, wotVersion);

            if (Directory.Exists(modsInArchive))
            {
                Directory.CreateDirectory(modsVersionDest);
                foreach (var entry in Directory.GetFileSystemEntries(modsInArchive))
                {
                    string name = Path.GetFileName(entry);

                    if (Directory.Exists(entry))
                    {
                        // Jeśli to folder z wersją gry (dowolną) — wszystko kopiuj do mods/nowa_wersja/
                        if (IsVersionString(name))
                        {
                            foreach (var subEntry in Directory.GetFileSystemEntries(entry))
                            {
                                string dest = Path.Combine(modsVersionDest, Path.GetFileName(subEntry));
                                if (Directory.Exists(subEntry))
                                    CopyDirectory(subEntry, dest);
                                else
                                    File.Copy(subEntry, dest, true);
                            }
                        }
                        else
                        {
                            // Inny folder — kopiuj do mods/
                            string dest = Path.Combine(modsDest, name);
                            CopyDirectory(entry, dest);
                        }
                    }
                    else
                    {
                        // Plik luzem — do mods/
                        string dest = Path.Combine(modsDest, name);
                        File.Copy(entry, dest, true);
                    }
                }
            }

            // RES_MODS
            string resModsInArchive = Path.Combine(tempFolder, "res_mods");
            string resModsDest = Path.Combine(gameFolder, "res_mods");
            string resModsVersionDest = Path.Combine(resModsDest, wotVersion);

            if (Directory.Exists(resModsInArchive))
            {
                Directory.CreateDirectory(resModsVersionDest);
                foreach (var entry in Directory.GetFileSystemEntries(resModsInArchive))
                {
                    string name = Path.GetFileName(entry);

                    if (Directory.Exists(entry))
                    {
                        // Jeśli to folder z wersją gry (dowolną) — wszystko kopiuj do res_mods/nowa_wersja/
                        if (IsVersionString(name))
                        {
                            foreach (var subEntry in Directory.GetFileSystemEntries(entry))
                            {
                                string dest = Path.Combine(resModsVersionDest, Path.GetFileName(subEntry));
                                if (Directory.Exists(subEntry))
                                    CopyDirectory(subEntry, dest);
                                else
                                    File.Copy(subEntry, dest, true);
                            }
                        }
                        else
                        {
                            // Inny folder — kopiuj do res_mods/
                            string dest = Path.Combine(resModsDest, name);
                            CopyDirectory(entry, dest);
                        }
                    }
                    else
                    {
                        // Plik luzem — do res_mods/
                        string dest = Path.Combine(resModsDest, name);
                        File.Copy(entry, dest, true);
                    }
                }
            }
        }

        /// <summary>
        /// Kopiuje cały katalog rekurencyjnie (bez powielania mods/mods i res_mods/res_mods).
        /// </summary>
        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            var srcName = Path.GetFileName(sourceDir).ToLowerInvariant();
            var dstName = Path.GetFileName(destinationDir).ToLowerInvariant();
            if ((srcName == "mods" && dstName == "mods") || (srcName == "res_mods" && dstName == "res_mods"))
            {
                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
                foreach (string dir in Directory.GetDirectories(sourceDir))
                {
                    CopyDirectory(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
                }
                return;
            }
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

        private void RemoveOlderModVersions(string modsFolder, string modPrefix)
        {
            if (!Directory.Exists(modsFolder))
                return;

            var files = Directory.GetFiles(modsFolder, $"{modPrefix}_*.wotmod");
            if (files.Length <= 1) return;

            // Wydobądź wersje i posortuj malejąco
            var versionRegex = new Regex(Regex.Escape(modPrefix) + @"_(\d+\.\d+(\.\d+)?).wotmod$", RegexOptions.IgnoreCase);

            var versionedFiles = files.Select(f =>
            {
                var m = versionRegex.Match(Path.GetFileName(f));
                return new
                {
                    FilePath = f,
                    Version = m.Success ? m.Groups[1].Value : ""
                };
            })
            .Where(x => !string.IsNullOrEmpty(x.Version))
            .OrderByDescending(x => new Version(x.Version))
            .ToList();

            // Zostaw najnowszy, usuń resztę
            foreach (var oldFile in versionedFiles.Skip(1))
            {
                try { File.Delete(oldFile.FilePath); }
                catch { /* opcjonalnie obsłuż błąd */ }
            }
        }

        // --- Poniżej zaktualizowane metody instalacji ---

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

            // Uproszczone kopiowanie wszystkiego z wg/mods i wg/res_mods
            string wgFolder = Path.Combine(tempFolder, "wg");
            if (Directory.Exists(wgFolder))
                CopyModsAndResMods(wgFolder, gameFolder, wotVersion);

            Directory.Delete(tempFolder, true);
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
            CopyModsAndResMods(tempFolder, gameFolder, wotVersion);
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
            CopyModsAndResMods(tempFolder, gameFolder, wotVersion);
            Directory.Delete(tempFolder, true);
        }

        private async Task InstallTechTreeAsync(string gameFolder, string wotVersion)
        {
            string zipUrl = "https://github.com/Vretu-Dev/Modpack/raw/master/Mods/TechTree.zip";
            string tempFolder = Path.Combine(gameFolder, ".tempModpackTechTree");
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);

            string zipPath = Path.Combine(tempFolder, "TechTree.zip");
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
            CopyModsAndResMods(tempFolder, gameFolder, wotVersion);
            Directory.Delete(tempFolder, true);
        }

        private async Task InstallMarksOnGunExtendedAsync(string gameFolder, string wotVersion)
        {
            string zipUrl = "https://github.com/Vretu-Dev/Modpack/raw/master/Mods/Moe.zip";
            string tempFolder = Path.Combine(gameFolder, ".tempModpackMarksOnGunExtended");
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);

            string zipPath = Path.Combine(tempFolder, "MarksOnGunExtended.zip");
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
            CopyModsAndResMods(tempFolder, gameFolder, wotVersion);
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

        private async Task InstallExtendedBlacklistAsync(string gameFolder, string wotVersion)
        {
            string zipUrl = "https://github.com/Vretu-Dev/Modpack/raw/master/Mods/Extended_Blacklist.zip";
            string tempFolder = Path.Combine(gameFolder, ".tempModpackExtBlacklist");
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);

            string zipPath = Path.Combine(tempFolder, "Extended_Blacklist.zip");
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
            CopyModsAndResMods(tempFolder, gameFolder, wotVersion);
            Directory.Delete(tempFolder, true);
        }

        // --- Metody pobierania linków ZIP bez zmian ---
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
    }
}