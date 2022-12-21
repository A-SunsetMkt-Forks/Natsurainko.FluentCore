﻿using Natsurainko.FluentCore.Model.Download;
using Natsurainko.FluentCore.Model.Install;
using System.Collections.Generic;
using System.IO;

namespace Natsurainko.FluentCore.Model.Launch;

public class GameCore
{
    public DirectoryInfo Root { get; set; }

    public FileResource ClientFile { get; set; }

    public FileResource AssetIndexFile { get; set; }

    public FileResource LogConfigFile { get; set; }

    public List<LibraryResource> LibraryResources { get; set; }

    public string MainClass { get; set; }

    public IEnumerable<string> FrontArguments { get; set; } = new string[] { };

    public IEnumerable<string> BehindArguments { get; set; } = new string[] { };

    public string Id { get; set; }

    public string Type { get; set; }

    public int JavaVersion { get; set; }

    public string InheritsFrom { get; set; }

    public string Source { get; set; }

    public bool IsVanilla { get; set; }

    public IEnumerable<ModLoaderInformation> ModLoaders { get; set; }

    public int LibrariesCount { get; set; }

    public int AssetsCount { get; set; }

    public long TotalSize { get; set; }
}