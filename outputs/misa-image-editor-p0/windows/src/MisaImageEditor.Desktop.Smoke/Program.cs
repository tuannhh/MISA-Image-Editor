using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MisaImageEditor.Desktop;
using MisaImageEditor.Domain;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length < 2) return 2;
        Environment.SetEnvironmentVariable("MISA_DATA_DIR", Path.GetFullPath(args[1]));
        var app = new Application();
        var xaml = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "../../../../MisaImageEditor.Desktop/App.xaml"));
        var resource = new System.Xml.Linq.XElement(xaml.Root!.Name.Namespace + "ResourceDictionary", xaml.Root.Attributes().Where(x => x.IsNamespaceDeclaration), xaml.Root.Elements().Single().Nodes());
        app.Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(resource.ToString());
        var window = new MainWindow();
        var results = new List<string>();
        var failed = false;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); results.Add("PASS " + message); }
        object Field(string name) => typeof(MainWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
        void Invoke(string name) => typeof(MainWindow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, [window, new RoutedEventArgs()]);
        window.Loaded += async (_, _) =>
        {
            try
            {
                var watch = Stopwatch.StartNew();
                await window.ImportDirectoryAsync(args[0]);
                results.Add($"Import registration: {watch.Elapsed.TotalSeconds:F3}s");
                Check(window.Files.Count == 16, "RAW is preferred over 12 same-name JPEG companions; four nested JPEGs remain");
                await window.ThumbnailCompletion.WaitAsync(TimeSpan.FromMinutes(3));
                results.Add($"All thumbnails: {watch.Elapsed.TotalSeconds:F3}s");
                Check(window.Files.All(x => x.Thumbnail != null), "all 16 selected thumbnails decoded");
                Check(window.Files.Count(x => x.Extension == ".arw" && x.Thumbnail != null) == 12, "12 Sony RAW thumbnails decoded");
                var catalog = (JsonCatalogStore)Field("_catalog");
                catalog.CreateCollection("RAW test", true);
                var list = (ListView)window.FindName("FilesList");
                list.SelectedItems.Add(window.Files[0]); list.SelectedItems.Add(window.Files[1]);
                window.AddSelectedToTarget();
                Check(catalog.Collections.Single(x => x.Name == "RAW test").AssetPaths.Count == 2, "batch selection added to target");
                window.AddSelectedToTarget();
                Check(catalog.Collections.Single(x => x.Name == "RAW test").AssetPaths.Count == 2, "B target add is idempotent");
                var collections = (ListBox)window.FindName("CollectionsList");
                collections.SelectedIndex = 0;
                Check(window.Files.Count == 2, "collection filters displayed photos");
                Invoke("ShowAllCatalog_Click");
                Check(window.Files.Count == 16, "all photos restores deduplicated catalog");
                list.SelectedItem = window.Files.First(x => x.Extension == ".arw");
                ((RadioButton)window.FindName("EditorTab")).IsChecked = true;
                var image = (Image)window.FindName("PreviewImage");
                var renderWatch = Stopwatch.StartNew();
                while (image.Source is null && renderWatch.Elapsed < TimeSpan.FromSeconds(15)) await Task.Delay(50);
                Check(image.Source is BitmapSource, "RAW editor preview rendered");
                var bitmap = (BitmapSource)image.Source!;
                Check(Math.Max(bitmap.PixelWidth, bitmap.PixelHeight) <= 1440, "interactive preview is bounded to 1440px");
                results.Add($"Editor preview: {renderWatch.Elapsed.TotalSeconds:F3}s");
                typeof(MainWindow).GetMethod("AutoBasic_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(window, [window, new RoutedEventArgs()]);
                var autoRecipe = catalog.LoadRecipe(((LibraryItem)list.SelectedItem).FullPath);
                Check(Math.Abs(autoRecipe.Basic.Exposure) > 0.001 || Math.Abs(autoRecipe.Basic.Temperature) > 0.001 ||
                      Math.Abs(autoRecipe.Basic.Tint) > 0.001, "Auto Basic writes editable non-destructive settings");
                ((Slider)window.FindName("ExposureSlider")).Value = 0.5;
                await Task.Delay(1000);
                Check(!ReferenceEquals(bitmap, image.Source), "slider produces a new preview");
                ((RadioButton)window.FindName("LibraryTab")).IsChecked = true;
                results.Add("ALL CHECKS PASSED");
            }
            catch (Exception ex) { failed = true; results.Add(ex.ToString()); }
            File.WriteAllLines(Path.Combine(args[1], "desktop-smoke.txt"), results);
            foreach (var line in results) Console.WriteLine(line);
            if (!args.Contains("--keep-open")) window.Close();
        };
        app.Run(window);
        return failed ? 1 : 0;
    }
}
