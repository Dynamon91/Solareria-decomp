using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

static Dictionary<string, HashSet<string>> Types(string path)
{
    using var file = File.OpenRead(path);
    using var pe = new PEReader(file);
    var metadata = pe.GetMetadataReader();
    string Name(TypeDefinitionHandle handle) {
        var type = metadata.GetTypeDefinition(handle);
        var parent = type.GetDeclaringType();
        string name = metadata.GetString(type.Name);
        return parent.IsNil ? metadata.GetString(type.Namespace) + "." + name : Name(parent) + "+" + name;
    }
    var result = new Dictionary<string, HashSet<string>>();
    foreach (var handle in metadata.TypeDefinitions) {
        string name = Name(handle);
        // Compiler-generated types are regenerated during decompilation/recompilation.
        if (name.Contains('<')) continue;
        var type = metadata.GetTypeDefinition(handle);
        result.Add(name, type.GetFields().Select(f => metadata.GetString(metadata.GetFieldDefinition(f).Name)).Where(n => !n.Contains('<')).ToHashSet());
    }
    return result;
}

if (args.Length == 0 || args.Length % 2 != 0) {
    Console.Error.WriteLine("Supply original/rebuilt DLL pairs."); return 2;
}
var reports = new List<object>(); bool ok = true;
for (int i = 0; i < args.Length; i += 2) {
    string original = args[i], rebuilt = args[i + 1];
    var before = Types(original); var after = Types(rebuilt);
    string? identityBefore = AssemblyName.GetAssemblyName(original).FullName;
    string? identityAfter = AssemblyName.GetAssemblyName(rebuilt).FullName;
    var missing = before.Keys.Except(after.Keys).Order().ToArray();
    var fields = before.Where(t => after.ContainsKey(t.Key)).SelectMany(t => t.Value.Except(after[t.Key]).Select(f => t.Key + "::" + f)).Order().ToArray();
    bool compatible = identityBefore == identityAfter && missing.Length == 0 && fields.Length == 0;
    ok &= compatible;
    reports.Add(new { assembly = Path.GetFileName(original), identity_before = identityBefore, identity_after = identityAfter,
        original_nongenerated_types = before.Count, rebuilt_nongenerated_types = after.Count,
        missing_type_names = missing, missing_declared_field_names = fields, name_check_passed = compatible });
}
Console.WriteLine(JsonSerializer.Serialize(new { ok, scope = "assembly identity and nongenerated type/field names only; not serialized ABI or runtime validation", assemblies = reports }, new JsonSerializerOptions { WriteIndented = true }));
return ok ? 0 : 2;
