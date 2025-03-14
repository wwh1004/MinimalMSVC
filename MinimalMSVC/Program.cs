using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using FlexPE;

/********************************************************************************
 * 
 * Parse command line arguments
 * 
 ********************************************************************************/

args ??= [];
if (args.Any(t => t.Equals("-h", StringComparison.OrdinalIgnoreCase) || t.Equals("--help", StringComparison.OrdinalIgnoreCase))) {
	Console.WriteLine("""
		Usage: MinimalMSVC [-vsv=<version>] [-llvmv=<version>] [-vcv=<version>] [-winv=<version>] [-netv=<version>]
			-vsv=<version> : Visual Studio version (default: latest)
			-llvmv=<version> : LLVM version (default: latest)
			-vcv=<version> : MSVC version (default: latest)
			-winv=<version> : Windows SDK version (default: latest)
			-netv=<version> : .NET Framework SDK version (default: latest)
		""");
	return;
}
MyVersion? targetVsVer = null;
MyVersion? targetLlvmSDKVer = null;
MyVersion? targetMSVCSdkVer = null;
MyVersion? targetWinSDKVer = null;
MyVersion? targetNetFxSDKVer = null;
foreach (var arg in args) {
	if (arg.StartsWith("-vsv=", StringComparison.OrdinalIgnoreCase)) {
		if (!MyVersion.TryParse(arg["-vsv=".Length..], out var ver))
			throw new ArgumentException($"Invalid version: {arg["-vsv=".Length..]}");
		targetVsVer = ver;
	}
	else if (arg.StartsWith("-llvmv=", StringComparison.OrdinalIgnoreCase)) {
		if (!MyVersion.TryParse(arg["-llvmv=".Length..], out var ver))
			throw new ArgumentException($"Invalid version: {arg["-llvmv=".Length..]}");
		targetLlvmSDKVer = ver;
	}
	else if (arg.StartsWith("-vcv=", StringComparison.OrdinalIgnoreCase)) {
		if (!MyVersion.TryParse(arg["-vcv=".Length..], out var ver))
			throw new ArgumentException($"Invalid version: {arg["-vcv=".Length..]}");
		targetMSVCSdkVer = ver;
	}
	else if (arg.StartsWith("-winv=", StringComparison.OrdinalIgnoreCase)) {
		if (!MyVersion.TryParse(arg["-winv=".Length..], out var ver))
			throw new ArgumentException($"Invalid version: {arg["-winv=".Length..]}");
		targetWinSDKVer = ver;
	}
	else if (arg.StartsWith("-netv=", StringComparison.OrdinalIgnoreCase)) {
		if (!MyVersion.TryParse(arg["-netv=".Length..], out var ver))
			throw new ArgumentException($"Invalid version: {arg["-netv=".Length..]}");
		targetNetFxSDKVer = ver;
	}
	else {
		throw new ArgumentException($"Invalid argument: {arg}");
	}
}

/********************************************************************************
 * 
 * Get Visual Studio, LLVM, MSVC, Windows SDK and .NET Framework SDK paths
 * 
 ********************************************************************************/

var ProgramFilesX86Root = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
var VsWherePath = Path.Combine(ProgramFilesX86Root, @"Microsoft Visual Studio\Installer\vswhere.exe");
var VsRoot = string.Empty;
var WindowsSDKRoot = Path.Combine(ProgramFilesX86Root, "Windows Kits");
var NetFxSDKRoot = Path.Combine(ProgramFilesX86Root, @"Windows Kits\NETFXSDK");
var LlvmSDKRoot = string.Empty;
var MSVCSDKRoot = string.Empty;
var (vsVer, vsPath) = SelectVersion(GetVsInsts(), t => t.Ver, targetVsVer);
VsRoot = vsPath;
Console.WriteLine($"Found Visual Studio v{vsVer} at '{VsRoot}'.");
LlvmSDKRoot = Path.Combine(VsRoot, @"VC\Tools\Llvm");
var (llvmSDKVer, llvmSDKPath) = SelectVersion(GetLlvmSDKs(), t => t.Ver, targetLlvmSDKVer);
Console.WriteLine($"Found LLVM Toolset v{llvmSDKVer} at '{llvmSDKPath}'.");
MSVCSDKRoot = Path.Combine(VsRoot, @"VC\Tools\MSVC");
var (msvcSDKVer, msvcSDKPath) = SelectVersion(GetMSVCSDKs(), t => t.Ver, targetMSVCSdkVer);
Console.WriteLine($"Found MSVC Toolset v{msvcSDKVer} at '{msvcSDKPath}'.");
var (winSDKVer, winSDKPath) = SelectVersion(GetWinSDKs(), t => t.Ver, targetWinSDKVer);
Console.WriteLine($"Found Windows SDK v{winSDKVer} at '{winSDKPath}'.");
var (netFxSDKVer, netFxSDKPath) = SelectVersion(GetNetFxSDKs(), t => t.Ver, targetNetFxSDKVer);
Console.WriteLine($"Found .NET Framework SDK v{netFxSDKVer} at '{netFxSDKPath}'.");
Console.WriteLine();
T SelectVersion<T>(T[] values, Func<T, MyVersion> selector, MyVersion? targetVersion) {
	if (values.Length == 0)
		throw new InvalidOperationException("No installed version found.");
	// 1. If the target version is not specified, return the latest version.
	if (targetVersion is null)
		return values.OrderByDescending(selector).First();
	// 2. Return the version closest to and greater than or equal to the target version.
	if (values.Where(t => selector(t).CompareTo(targetVersion) >= 0).OrderBy(selector).FirstOrDefault() is T result)
		return result;
	throw new InvalidOperationException("No matched version found.");
}

/********************************************************************************
 * 
 * Collect include files
 * 
 ********************************************************************************/

var includeFiles = new List<MyFile>();
var includeDirs = new List<MyFile>();
CollectIncludeFiles(VsRoot, Path.Combine(llvmSDKPath, $@"lib\clang\{llvmSDKVer}\include"), false, true);
CollectIncludeFiles(VsRoot, Path.Combine(llvmSDKPath, $@"x64\lib\clang\{llvmSDKVer}\include"), true, true);
CollectIncludeFiles(VsRoot, Path.Combine(msvcSDKPath, "include"));
CollectIncludeFiles(VsRoot, Path.Combine(msvcSDKPath, @"atlmfc\include"));
CollectIncludeFiles(ProgramFilesX86Root, Path.Combine(winSDKPath, $@"Include\{winSDKVer}\ucrt"));
CollectIncludeFiles(ProgramFilesX86Root, Path.Combine(winSDKPath, $@"Include\{winSDKVer}\um"));
CollectIncludeFiles(ProgramFilesX86Root, Path.Combine(winSDKPath, $@"Include\{winSDKVer}\shared"));
CollectIncludeFiles(ProgramFilesX86Root, Path.Combine(netFxSDKPath, @"Include\um"));
void CollectIncludeFiles(string baseDir, string dir, bool? isX64 = null, bool isLlvm = false) {
	Console.WriteLine($"Collecting include files in '{dir}'.");
	includeFiles.AddRange(CollectFiles(dir).Select(t => new MyFile(t, Path.GetRelativePath(baseDir, t), isX64, isLlvm)));
	var relDir = Path.GetRelativePath(baseDir, dir);
	includeDirs.Add(new MyFile(dir, relDir, isX64, isLlvm));
	Console.WriteLine($"Inlcude(x64:{isX64},llvm:{isLlvm}): {relDir}");
}
Console.WriteLine();

/********************************************************************************
 * 
 * Collect lib files
 * 
 ********************************************************************************/

var libFiles = new List<MyFile>();
var libDirs = new List<MyFile>();
CollectLibFilesX86AndX64(VsRoot, Path.Combine(msvcSDKPath, "lib"));
CollectLibFilesX86AndX64(VsRoot, Path.Combine(msvcSDKPath, @"atlmfc\lib"));
CollectLibFilesX86AndX64(ProgramFilesX86Root, Path.Combine(winSDKPath, $@"Lib\{winSDKVer}\ucrt"));
CollectLibFilesX86AndX64(ProgramFilesX86Root, Path.Combine(winSDKPath, $@"Lib\{winSDKVer}\um"));
CollectLibFilesX86AndX64(ProgramFilesX86Root, Path.Combine(netFxSDKPath, @"Lib\um"));
void CollectLibFilesX86AndX64(string baseDir, string dir) {
	CollectLibFiles(baseDir, Path.Combine(dir, "x86"), false);
	CollectLibFiles(baseDir, Path.Combine(dir, "x64"), true);
}
void CollectLibFiles(string baseDir, string dir, bool isX64, bool isLlvm = false) {
	Console.WriteLine($"Collecting lib files in '{dir}'.");
	// Ignore *.pdb, msvcurt*.lib and clang_rt.fuzzer*.lib files.
	// msvcurt*.lib are C runtime libraries for the /clr:pure option which take about 500MB space but the /clr:pure option was already deprecated.
	// clang_rt.fuzzer*.lib are fuzzer libraries which take about 400MB space and are not necessary for most users.
	var libFilePaths = CollectFiles(dir, onSubDir: _ => false,
		onFile: path => !path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)
			&& !Path.GetFileName(path).StartsWith("msvcurt", StringComparison.OrdinalIgnoreCase)
			&& !Path.GetFileName(path).StartsWith("clang_rt.fuzzer", StringComparison.OrdinalIgnoreCase));
	libFiles.AddRange(libFilePaths.Select(t => new MyFile(t, Path.GetRelativePath(baseDir, t), isX64, isLlvm)));
	var relDir = Path.GetRelativePath(baseDir, dir);
	libDirs.Add(new MyFile(dir, relDir, isX64, isLlvm));
	Console.WriteLine($"Lib(x64:{isX64},llvm:{isLlvm}): {relDir}");
}
Console.WriteLine();

/********************************************************************************
 * 
 * Collect bin files
 * 
 ********************************************************************************/

var binFiles = new List<MyFile>();
var binDirs = new List<MyFile>();
bool hasEnglish = Directory.Exists(Path.Combine(msvcSDKPath, @"bin\Hostx64\x86\1033"));
string[] msvcFileList = ["cl.exe", "c1xx.dll", "c2.dll", "link.exe", "mspdb80.dll", "mspdb90.dll", "mspdb100.dll", "mspdb110.dll", "mspdb120.dll", "mspdb140.dll", "mspdbcore.dll", "msobj80.dll", "msobj90.dll", "msobj100.dll", "msobj110.dll", "msobj120.dll", "msobj140.dll", "cvtres.exe"];
CollectBinFiles(VsRoot, Path.Combine(llvmSDKPath, @"bin"), false, true, ["clang-cl.exe", "lld-link.exe"]);
CollectBinFiles(VsRoot, Path.Combine(llvmSDKPath, @"x64\bin"), true, true, ["clang-cl.exe", "lld-link.exe"]);
CollectBinFiles(VsRoot, Path.Combine(msvcSDKPath, @"bin\Hostx64\x86"), false, false, msvcFileList);
CollectBinFiles(VsRoot, Path.Combine(msvcSDKPath, hasEnglish ? @"bin\Hostx64\x86\1033" : @"bin\Hostx64\x86\2052"), false, false, ["clui.dll"], false);
CollectBinFiles(VsRoot, Path.Combine(msvcSDKPath, @"bin\Hostx64\x64"), true, false, msvcFileList);
CollectBinFiles(VsRoot, Path.Combine(msvcSDKPath, hasEnglish ? @"bin\Hostx64\x64\1033" : @"bin\Hostx64\x64\2052"), true, false, ["clui.dll"], false);
CollectBinFiles(ProgramFilesX86Root, Path.Combine(winSDKPath, $@"bin\{winSDKVer}\x86"), false, false, ["rc.exe"]);
CollectBinFiles(ProgramFilesX86Root, Path.Combine(winSDKPath, $@"bin\{winSDKVer}\x64"), true, false, ["rc.exe"]);
void CollectBinFiles(string baseDir, string dir, bool? isX64, bool isLlvm, string[] onlyFiles, bool addDir = true) {
	Console.WriteLine($"Collecting bin files in '{dir}'.");
	var dir2 = dir;
	if (isLlvm && isX64 == false) {
		// Re-use x64 bin files for x86 llvm.
		// We must copy the files to the x86 directory otherwise clang-cl will not work, see: https://developercommunity.visualstudio.com/t/clang-x64-on-x86-build-unknown-type-name-uintptr-t/1224638
		dir2 = dir.Replace(@"\bin", @"\x64\bin");
	}
	if (onlyFiles is not null) {
		var imports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		imports.UnionWith(onlyFiles);
		foreach (var onlyFile in onlyFiles)
			imports.UnionWith(GetImports(Path.Combine(dir2, onlyFile)));
		onlyFiles = [.. imports.Where(t => File.Exists(Path.Combine(dir2, t)))];
	}
	var binFilePaths = CollectFiles(dir2, onSubDir: _ => false,
		onFile: path => onlyFiles is null || onlyFiles.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)).ToArray();
	binFiles.AddRange(binFilePaths.Select(t => {
		var relPath = Path.GetRelativePath(baseDir, t);
		if (isLlvm && isX64 == false)
			relPath = relPath.Replace(@"\x64\bin", @"\bin");
		return new MyFile(t, relPath, isX64, isLlvm);
	}));
	if (addDir) {
		var relDir = Path.GetRelativePath(baseDir, dir);
		binDirs.Add(new MyFile(dir, relDir, isX64, isLlvm));
		Console.WriteLine($"Bin(x64:{isX64},llvm:{isLlvm}): {relDir} ({string.Join(',', onlyFiles!)})");
	}
}
Console.WriteLine();

/********************************************************************************
 * 
 * Create MSVC toolset environment files
 * 
 ********************************************************************************/

var msvcSDKsRoot = Path.GetFullPath($"MSVC-{msvcSDKVer}");
if (Directory.Exists(msvcSDKsRoot))
	Console.Error.WriteLine($"'{msvcSDKsRoot}' already exists.");
Console.WriteLine($"Copying files to '{msvcSDKsRoot}'.");
Directory.CreateDirectory(msvcSDKsRoot);
var files = includeFiles.Concat(libFiles).Concat(binFiles).ToArray();
var dirs = includeDirs.Concat(libDirs).Concat(binDirs).ToArray();
int maxRelPathLen = files.Concat(dirs).Select(t => t.RelPath.Length).Max();
File.WriteAllLines(Path.Combine(msvcSDKsRoot, "filelist.txt"), dirs.Concat(files).Select(t => $"{t.RelPath.PadRight(maxRelPathLen)} : {t.Path}"));
CollectEnvX86AndX64("clang-cl", true, WriteEnvAndBat);
CollectEnvX86AndX64("lld-link", true, WriteEnvAndBat);
CollectEnvX86AndX64("cl", false, WriteEnvAndBat);
CollectEnvX86AndX64("link", false, WriteEnvAndBat);
CollectEnvX86AndX64("rc", false, WriteEnvAndBat);
void CollectEnvX86AndX64(string tool, bool isLlvm, CollectEnvCallback callback) {
	CollectEnv(tool, false, isLlvm, callback);
	CollectEnv(tool, true, isLlvm, callback);
}
void CollectEnv(string tool, bool isX64, bool isLlvm, CollectEnvCallback callback) {
	var toolPath = binFiles.Single(t => Path.GetFileNameWithoutExtension(t.Path).Equals(tool, StringComparison.OrdinalIgnoreCase) && (t.IsX64 is null || t.IsX64.Value == isX64) && t.IsLlvm == isLlvm);
	var envs = new List<MyEnv>();
	AddDirs(envs, includeDirs, "INCLUDE", isX64, isLlvm);
	AddDirs(envs, libDirs, "LIB", isX64, isLlvm);
	AddDirs(envs, binDirs, "Path", isX64, isLlvm);
	callback(tool, isX64, isLlvm, toolPath, [.. envs]);

	void AddDirs(List<MyEnv> envs, List<MyFile> dirs, string envName, bool isX64, bool isLlvm) {
		if (isLlvm) {
			foreach (var dir in dirs.Where(t => t.IsX64 == isX64 && t.IsLlvm))
				envs.Add(new(envName, dir.RelPath, true));
			foreach (var dir in dirs.Where(t => t.IsX64 is null && t.IsLlvm))
				envs.Add(new(envName, dir.RelPath, true));
		}
		foreach (var dir in dirs.Where(t => t.IsX64 == isX64 && !t.IsLlvm))
			envs.Add(new(envName, dir.RelPath, true));
		foreach (var dir in dirs.Where(t => t.IsX64 is null && !t.IsLlvm))
			envs.Add(new(envName, dir.RelPath, true));
	}
}
void WriteEnvAndBat(string tool, bool isX64, bool isLlvm, MyFile toolPath, MyEnv[] envs) {
	var env = new StringBuilder();
	env.AppendLine(toolPath.RelPath);
	foreach (var (name, value, isPath) in envs) {
		if (isPath)
			env.AppendLine($"{name}:.\\{value}");
		else
			env.AppendLine($"{name}:{value}");
	}
	File.WriteAllText(Path.Combine(msvcSDKsRoot, $"{tool}-{(isX64 ? "x64" : "x86")}.env"), env.ToString());

	var bat = new StringBuilder();
	bat.AppendLine("@echo off");
	bat.AppendLine("setlocal");
	bat.AppendLine("set \"CURRENT_DIR=%~dp0\"");
	var sortedEnv = envs.GroupBy(t => t.Name).Select(t => (
		Name: t.Key,
		Values: t.Select(t => t.Value).Reverse().ToArray(),
		t.First().IsPath
	)).ToArray();
	foreach (var (name, values, isPath) in sortedEnv) {
		Debug.Assert(isPath || values.Length == 1, "The value of non-path environment variable must be unique.");
		if (isPath) {
			foreach (var value in values)
				bat.AppendLine($"set \"{name}=%CURRENT_DIR%{value};%{name}%\"");
		}
		else {
			bat.AppendLine($"set \"{name}={values[0]}\"");
		}
	}
	bat.AppendLine($"\"%CURRENT_DIR%{toolPath.RelPath}\" %*");
	File.WriteAllText(Path.Combine(msvcSDKsRoot, $"{tool}-{(isX64 ? "x64" : "x86")}.bat"), bat.ToString());
}

/********************************************************************************
 * 
 * Copy files
 * 
 ********************************************************************************/

files.AsParallel().ForAll(file => {
	var destPath = Path.Combine(msvcSDKsRoot, file.RelPath);
	Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? throw new InvalidOperationException($"Can't get directory of '{destPath}'"));
	File.Copy(file.Path, destPath, true);
});
Console.WriteLine("Done.");
Console.WriteLine();

/********************************************************************************
 * 
 * Miscellanea
 * 
 ********************************************************************************/

(MyVersion Ver, string Dir)[] GetVsInsts() {
	using var process = Process.Start(new ProcessStartInfo(VsWherePath, "/format json /utf8") {
		CreateNoWindow = true,
		RedirectStandardOutput = true,
		StandardOutputEncoding = Encoding.UTF8,
		UseShellExecute = false
	});
	process!.WaitForExit();
	var stdout = process.StandardOutput.ReadToEnd();
	var vsPropsJson = (JsonNode.Parse(stdout) as JsonArray) ?? throw new InvalidOperationException("Can't parse vswhere stdout.");
	return [.. vsPropsJson.Select(t => (
		MyVersion.Parse((string?)t?["installationVersion"] ?? throw new InvalidOperationException("Can't get installationVersion.")),
		(string?)t?["installationPath"] ?? throw new InvalidOperationException("Can't get installationPath."))
	)];
}

(MyVersion Ver, string Dir)[] GetWinSDKs() {
	var list = new List<(MyVersion, string)>();
	foreach (var dir in Directory.EnumerateDirectories(WindowsSDKRoot)) {
		if (!MyVersion.TryParse(Path.GetFileName(dir), out _))
			continue;
		if (!Directory.Exists(Path.Combine(dir, "Include")))
			continue;
		foreach (var incDir in Directory.EnumerateDirectories(Path.Combine(dir, "Include"))) {
			if (!MyVersion.TryParse(Path.GetFileName(incDir), out var ver))
				continue;
			list.Add((ver, dir));
		}
	}
	return [.. list];
}

(MyVersion Ver, string Dir)[] GetNetFxSDKs() {
	var list = new List<(MyVersion, string)>();
	foreach (var dir in Directory.EnumerateDirectories(NetFxSDKRoot)) {
		if (!MyVersion.TryParse(Path.GetFileName(dir), out var ver))
			continue;
		list.Add((ver, dir));
	}
	return [.. list];
}

(MyVersion Ver, string Dir)[] GetLlvmSDKs() {
	var list = new List<(MyVersion, string)>();
	foreach (var dir in Directory.EnumerateDirectories(Path.Combine(LlvmSDKRoot, @"lib\clang"))) {
		if (!MyVersion.TryParse(Path.GetFileName(dir), out var ver))
			continue;
		list.Add((ver, LlvmSDKRoot));
	}
	return [.. list];
}

(MyVersion Ver, string Dir)[] GetMSVCSDKs() {
	var list = new List<(MyVersion, string)>();
	foreach (var dir in Directory.EnumerateDirectories(MSVCSDKRoot)) {
		if (!MyVersion.TryParse(Path.GetFileName(dir), out var ver))
			continue;
		list.Add((ver, dir));
	}
	return [.. list];
}

static IEnumerable<string> CollectFiles(string dir, Predicate<string>? onSubDir = null, Predicate<string>? onFile = null) {
	foreach (var file in Directory.EnumerateFiles(dir)) {
		if (onFile is null || onFile(file))
			yield return file;
	}
	foreach (var subDir in Directory.EnumerateDirectories(dir)) {
		if (onSubDir is not null && !onSubDir(subDir))
			continue;
		foreach (var file in CollectFiles(subDir, onSubDir, onFile))
			yield return file;
	}
}

static string[] GetImports(string filePath) {
	if (!File.Exists(filePath))
		return [];
	var visited = new HashSet<string>(StringComparer.Ordinal) { Path.GetFileName(filePath) };
	var imports = GetOneImports(filePath).ToHashSet(StringComparer.Ordinal);
	while (true) {
		int oldCount = imports.Count;
		foreach (var node in imports.Where(t => !visited.Contains(t)).ToArray()) {
			visited.Add(node);
			var nodePath = Path.Combine(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException($"Can't get directory of '{filePath}'"), node);
			if (!File.Exists(nodePath))
				continue;
			imports.UnionWith(GetOneImports(nodePath));
		}
		if (oldCount == imports.Count)
			break;
	}
	return [.. imports];
}

unsafe static string[] GetOneImports(string filePath) {
	using var fileMapping = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
	using var accessor = fileMapping.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
	var peImage = new PEImage((byte*)accessor.SafeMemoryMappedViewHandle.DangerousGetHandle());
	return [.. peImage.ImportDirectory.ImportView.Select(t => t.Name.ReadAsciiString())];
}

record MyEnv(string Name, string Value, bool IsPath);

delegate void CollectEnvCallback(string tool, bool isX64, bool isLlvm, MyFile toolPath, MyEnv[] envs);

record MyFile(string Path, string RelPath, bool? IsX64, bool IsLlvm);

class MyVersion : IComparable<MyVersion> {
	readonly string s;
	readonly int majorVer;
	readonly Version? ver;

	MyVersion(string s, int majorVer, Version? ver) {
		this.s = s;
		this.majorVer = majorVer;
		this.ver = ver;
	}

	public static MyVersion Parse(string s) {
		return TryParse(s, out var result) ? result : throw new FormatException($"Invalid version: {s}");
	}

	public static bool TryParse([NotNullWhen(true)] string? s, [NotNullWhen(true)] out MyVersion? result) {
		if (int.TryParse(s, out int majorVer)) {
			result = new MyVersion(s, majorVer, null);
			return true;
		}
		if (Version.TryParse(s, out var ver)) {
			result = new MyVersion(s, 0, ver);
			return true;
		}
		result = default;
		return false;
	}

	public int CompareTo(MyVersion? other) {
		if (ReferenceEquals(this, other))
			return 0;
		if (other is null)
			return 1;
		return ToVersion().CompareTo(other.ToVersion());
	}

	Version ToVersion() {
		return ver ?? new Version(majorVer, 0);
	}

	public override string ToString() {
		return s;
	}
}
