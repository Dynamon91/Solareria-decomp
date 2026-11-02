using AssetRipper.Processing.Textures;
using AssetRipper.Import.Logging;
using System.Diagnostics.CodeAnalysis;
using AssetRipper.Assets;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.Export.UnityProjects.Project;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.UnityProjects;
using AssetRipper.IO.Files;
if (args.Length == 3 && args[0] == "--copy-streaming-only") {
    Console.WriteLine("STREAMING_FILES_COPIED=" + StreamingCopy.Copy(args[1],args[2])); return 0;
}
if (args.Length!=2 || !Directory.Exists(args[0]) || Directory.Exists(args[1]) || File.Exists(args[1])) {
    Console.Error.WriteLine("Existing game input path and NEW output directory required"); return 2;
}
Logger.Add(new ConsoleLogger());
var config = new FullConfiguration();
config.ExportSettings.ScriptExportMode = ScriptExportMode.DllExportWithoutRenaming;
config.ExportSettings.ShaderExportMode = ShaderExportMode.Decompile;
config.ExportSettings.ExportUnreadableAssets = true;
var handler = new RepairExportHandler(config);
var data = handler.LoadAndProcess(new[]{Path.GetFullPath(args[0])}, LocalFileSystem.Instance);
handler.Export(data,Path.GetFullPath(args[1]),LocalFileSystem.Instance);
Console.WriteLine("STREAMING_FILES_COPIED=" + StreamingCopy.Copy(args[0],Path.Combine(args[1],"ExportedProject")));
Console.WriteLine("DIRECT_EXPORT_COMPLETE");
return 0;

sealed class RepairExportHandler(FullConfiguration settings) : ExportHandler(settings)
{
    protected override void BeforeExport(ProjectExporter exporter) => exporter.OverrideExporter<IUnityObjectBase>(new EmptyTextureYamlExporter());
}
sealed class EmptyTextureYamlExporter : DefaultYamlExporter
{
    public override bool TryCreateCollection(IUnityObjectBase asset, [NotNullWhen(true)] out IExportCollection? collection)
    {
        var group = asset as SpriteInformationObject ?? asset.MainAsset as SpriteInformationObject;
        var texture = asset as ITexture2D ?? group?.Texture;
        if (texture != null && texture.Name.String == "LiberationSans SDF Atlas" && texture.Width_C28 == 0 && texture.Height_C28 == 0 && texture.ImageData_C28.Length == 0 && (group == null || group.Sprites.Count == 0)) {
            collection = new EmptyTextureCollection(this, texture, group);
            return true;
        }
        collection = null;
        return false;
    }
}

sealed class EmptyTextureCollection : AssetsExportCollection<ITexture2D>
{
    public EmptyTextureCollection(IAssetExporter exporter, ITexture2D texture, SpriteInformationObject? group) : base(exporter,texture) { AddAsset(group); }
    protected override bool ExportInner(IExportContainer container,string path,string dir,FileSystem fileSystem) => AssetExporter.Export(container,Asset,path,fileSystem);
}

static class StreamingCopy
{
    public static int Copy(string input,string project)
    {
        input=Path.GetFullPath(input);project=Path.GetFullPath(project);
        if (!Directory.Exists(Path.Combine(project,"Assets"))) throw new IOException("Unity Assets folder missing");
        var options=new EnumerationOptions { RecurseSubdirectories=true, AttributesToSkip=FileAttributes.ReparsePoint };
        var directories=Directory.EnumerateDirectories(input,"StreamingAssets",options).ToArray();
        if (directories.Length != 1) throw new IOException("StreamingAssets folder missing or ambiguous");
        string source=directories[0], destination=Path.Combine(project,"Assets","StreamingAssets");
        if (Path.GetFullPath(destination).StartsWith(source+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
            throw new IOException("Output must be outside the source StreamingAssets");
        int count=0;
        foreach (string path in Directory.EnumerateFiles(source,"*",options)) {
            string target=Path.Combine(destination,Path.GetRelativePath(source,path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (File.Exists(target)) {
                using var a=File.OpenRead(path);using var b=File.OpenRead(target);
                if (!System.Security.Cryptography.SHA256.HashData(a).AsSpan().SequenceEqual(System.Security.Cryptography.SHA256.HashData(b)))
                    throw new IOException("StreamingAssets collision: "+target);
            } else File.Copy(path,target);
            count++;
        }
        return count;
    }
}
