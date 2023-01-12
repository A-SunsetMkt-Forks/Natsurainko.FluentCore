﻿using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.FluentCore.Model.Parser;
using Natsurainko.FluentCore.Module.Parser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Natsurainko.FluentCore.Module.Launcher;

public class GameCoreLocator : IGameCoreLocator<GameCore>
{
    public DirectoryInfo Root { get; private set; }

    public List<(string, Exception)> ErrorGameCores { get; private set; }

    public GameCoreLocator(string path) => Root = new DirectoryInfo(path);

    public GameCoreLocator(DirectoryInfo root) => Root = root;

    public GameCore GetGameCore(string id)
    {
        foreach (var core in GetGameCores())
            if (core.Id == id)
                return core;

        return null;
    }

    public IEnumerable<GameCore> GetGameCores()
    {
        var entities = new List<VersionJsonEntity>();

        var versionsFolder = new DirectoryInfo(Path.Combine(Root.FullName, "versions"));

        if (!versionsFolder.Exists)
            return Array.Empty<GameCore>();

        foreach (var item in versionsFolder.EnumerateDirectories())
            foreach (var files in item.EnumerateFiles())
                if (files.Name == $"{item.Name}.json")
                    try { entities.Add(JsonConvert.DeserializeObject<VersionJsonEntity>(File.ReadAllText(files.FullName))); } catch { }

        var parser = new GameCoreParser(Root, entities);
        var result = parser.GetGameCores();

        ErrorGameCores = parser.ErrorGameCores;

        return result;
    }
}
