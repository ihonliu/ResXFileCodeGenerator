﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VocaDb.ResXFileCodeGenerator;

public sealed record FileOptions
{
	public string InnerClassInstanceName { get; init; }
	public string InnerClassName { get; init; }
	public InnerClassVisibility InnerClassVisibility { get; init; }
	public bool PartialClass { get; init; }
	public bool StaticMembers { get; init; } = true;
	public AdditionalText File { get; init; }
	public string FilePath { get; init; }
	public bool StaticClass { get; init; }
	public bool NullForgivingOperators { get; init; }
	public bool PublicClass { get; init; }
	public string ClassName { get; init; }
	public string? CustomToolNamespace { get; init; }
	public string LocalNamespace { get; init; }
	public bool Valid { get; init; }

	public FileOptions()
	{
		LocalNamespace = "";
		CustomToolNamespace = "";
		ClassName = "";
		InnerClassInstanceName = "";
		InnerClassName = "";
		File = null!;
		FilePath = "";
	}

	private FileOptions(AdditionalText file, AnalyzerConfigOptions options, GlobalOptions globalOptions)
	{
		File = file;
		var resxFilePath = file.Path;
		LocalNamespace = Utilities.GetLocalNamespace(
			resxFilePath,
			options.TryGetValue("build_metadata.EmbeddedResource.TargetPath", out var targetPath) &&
			targetPath is { Length: > 0 }
				? targetPath
				: null,
			globalOptions.ProjectFullPath,
			globalOptions.RootNamespace);

		CustomToolNamespace =
			options.TryGetValue("build_metadata.EmbeddedResource.CustomToolNamespace",
				out var customToolNamespace) && customToolNamespace is { Length: > 0 }
				? customToolNamespace
				: null;

		ClassName = Utilities.GetClassNameFromPath(resxFilePath);

		NullForgivingOperators = globalOptions.NullForgivingOperators;

		PublicClass =
			options.TryGetValue("build_metadata.EmbeddedResource.PublicClass", out var perFilePublicClassSwitch) &&
			perFilePublicClassSwitch is { Length: > 0 }
				? perFilePublicClassSwitch.Equals("true", StringComparison.OrdinalIgnoreCase)
				: globalOptions.PublicClass;

		StaticClass =
			options.TryGetValue("build_metadata.EmbeddedResource.StaticClass", out var perFileStaticClassSwitch) &&
			perFileStaticClassSwitch is { Length: > 0 }
				? !perFileStaticClassSwitch.Equals("false", StringComparison.OrdinalIgnoreCase)
				: globalOptions.StaticClass;

		StaticMembers =
			options.TryGetValue("build_metadata.EmbeddedResource.StaticMembers", out var staticMembersSwitch) &&
			staticMembersSwitch is { Length: > 0 }
				? staticMembersSwitch.Equals("false", StringComparison.OrdinalIgnoreCase)
				: globalOptions.StaticMembers;

		PartialClass =
			options.TryGetValue("build_metadata.EmbeddedResource.PartialClass", out var partialClassSwitch) &&
			partialClassSwitch is { Length: > 0 }
				? partialClassSwitch.Equals("true", StringComparison.OrdinalIgnoreCase)
				: globalOptions.PartialClass;

		InnerClassVisibility = globalOptions.InnerClassVisibility;
		if (options.TryGetValue("build_metadata.EmbeddedResource.InnerClassVisibility",
			    out var innerClassVisibilitySwitch) &&
		    Enum.TryParse(innerClassVisibilitySwitch, true, out InnerClassVisibility v) &&
		    v != InnerClassVisibility.SameAsOuter)
		{
			InnerClassVisibility = v;
		}

		InnerClassName = globalOptions.InnerClassName;
		if (options.TryGetValue("build_metadata.EmbeddedResource.InnerClassName", out var innerClassNameSwitch))
		{
			InnerClassName = innerClassNameSwitch;
		}

		InnerClassInstanceName = globalOptions.InnerClassInstanceName;
		if (options.TryGetValue("build_metadata.EmbeddedResource.InnerClassInstanceName",
			    out var innerClassInstanceNameSwitch))
		{
			InnerClassInstanceName = innerClassInstanceNameSwitch;
		}

		FilePath = resxFilePath;

		Valid = globalOptions.Valid;
	}

	internal static FileOptions Select(AdditionalText file, AnalyzerConfigOptionsProvider options,
		GlobalOptions globalOptions, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		return new(file, options.GetOptions(file), globalOptions);
	}
}
