﻿using Nrk.FluentCore.Experimental.Exceptions;
using Nrk.FluentCore.Experimental.GameManagement.Dependencies;
using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Installer.Data;
using Nrk.FluentCore.Experimental.GameManagement.Instances;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using static Nrk.FluentCore.Utils.IProgressReporter;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer;

/// <summary>
/// Fabric 实例安装器
/// </summary>
public partial class FabricInstanceInstaller : IInstanceInstaller<ModifiedMinecraftInstance>, IProgressReporter
{
    private readonly HttpClient httpClient = HttpUtils.HttpClient;

    public required string MinecraftFolder { get; set; }

    /// <summary>
    /// Fabric 安装所需数据
    /// </summary>
    public required FabricInstallData InstallData { get; set; }

    /// <summary>
    /// 原版 Minecraft 版本清单项
    /// </summary>
    public required VersionManifestItem McVersionManifestItem { get; set; }

    /// <summary>
    /// 镜像源
    /// </summary>
    public IDownloadMirror? DownloadMirror { get; set; }

    /// <summary>
    /// 强制检查所有依赖必须被下载
    /// </summary>
    public bool CheckAllDependencies { get; set; }

    /// <summary>
    /// 继承的原版实例（可选）
    /// </summary>
    public VanillaMinecraftInstance? InheritedInstance { get; set; }

    /// <summary>
    /// 自定义安装实例的 Id
    /// </summary>
    public string? CustomizedInstanceId { get; set; }

    public Task<ModifiedMinecraftInstance> InstallAsync() => InstallAsync(CancellationToken.None);

    public async Task<ModifiedMinecraftInstance> InstallAsync(CancellationToken cancellationToken)
    {
        VanillaMinecraftInstance? vanillaInstance;
        FileInfo? fabricClientJson = null;
        ModifiedMinecraftInstance? instance = null;

        try
        {
            vanillaInstance = await ParseOrInstallVanillaInstance(cancellationToken);
            fabricClientJson = await DownloadFabricClientJson(vanillaInstance, cancellationToken);
            instance = ParseModifiedMinecraftInstance(fabricClientJson, cancellationToken);
            await DownloadFabricLibraries(instance, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 取消后清理产生的部分文件

            if (instance != null)
            {
                fabricClientJson!.Directory?.DeleteAllFiles();
                fabricClientJson!.Directory?.Delete();
            }

            ProgressReporterHelper.ReportWhenExceptionThrow(this);
            throw;
        }
        catch
        {
            ProgressReporterHelper.ReportWhenExceptionThrow(this);
            throw;
        }

        return instance ?? throw new ArgumentNullException(nameof(instance), "Unexpected null reference to variable");
    }

    /// <summary>
    /// 解析继承的原版实例或直接安装新的原版实例
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<VanillaMinecraftInstance> ParseOrInstallVanillaInstance(CancellationToken cancellationToken)
    {
        Progress.Report(ProgressUpdater.FromRunning("ParseOrInstallVanillaInstance"));

        if (InheritedInstance != null)
            return InheritedInstance;

        var vanillaInstanceInstaller = new VanillaInstanceInstaller()
        {
            DownloadMirror = DownloadMirror,
            McVersionManifestItem = McVersionManifestItem,
            MinecraftFolder = MinecraftFolder,
            CheckAllDependencies = true
        };

        vanillaInstanceInstaller.ProgressChanged += (object? sender, ProgressUpdater e) =>
        {
            e.Update(vanillaInstanceInstaller.Progresses);

            int totalTasks = vanillaInstanceInstaller.Progresses.Values.Select(x => x.TotalTasks).Sum();
            int totalFinishedTasks = vanillaInstanceInstaller.Progresses.Values.Select(x => x.FinishedTasks).Sum();

            Progress.Report(ProgressUpdater.FromUpdateAllTasks("ParseOrInstallVanillaInstance", totalFinishedTasks, totalTasks));
        };

        var instance = await vanillaInstanceInstaller.InstallAsync(cancellationToken);

        Progress.Report(ProgressUpdater.FromFinished("ParseOrInstallVanillaInstance"));

        return instance;
    }

    /// <summary>
    /// 下载 version.json 并写入文件
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<FileInfo> DownloadFabricClientJson(VanillaMinecraftInstance instance, CancellationToken cancellationToken)
    {
        Progress.Report(ProgressUpdater.FromRunning("DownloadFabricClientJson"));

        string requestUrl = $"https://meta.fabricmc.net/v2/versions/loader/{instance.InstanceId}/{InstallData.Loader.Version}/profile/json";

        if (DownloadMirror != null)
            requestUrl = DownloadMirror.GetMirrorUrl(requestUrl);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        using var responseMessage = await httpClient.SendAsync(requestMessage, cancellationToken);

        responseMessage.EnsureSuccessStatusCode();

        string jsonContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        string instanceId = CustomizedInstanceId ?? 
            (JsonNode.Parse(jsonContent)!["id"]?.GetValue<string>() ?? $"fabric-loader-{InstallData.Loader.Version}-{instance.InstanceId}");

        var jsonFile = new FileInfo(Path.Combine(MinecraftFolder, "versions", instanceId, $"{instanceId}.json"));
        
        if (!jsonFile.Directory!.Exists)
            jsonFile.Directory.Create();

        await File.WriteAllTextAsync(jsonFile.FullName, jsonContent, cancellationToken);

        Progress.Report(ProgressUpdater.FromFinished("DownloadFabricClientJson"));

        return jsonFile;
    }

    /// <summary>
    /// 解析 Modified Minecraft 实例
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    static ModifiedMinecraftInstance ParseModifiedMinecraftInstance(FileInfo fileInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var instance = MinecraftInstance.Parse(fileInfo.Directory!) as ModifiedMinecraftInstance;

        return instance ?? throw new InvalidOperationException("An incorrect modified instance was encountered");
    }

    /// <summary>
    /// 使用 MultipartDownloader 下载 Fabric Libraries
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    async Task DownloadFabricLibraries(MinecraftInstance instance, CancellationToken cancellationToken)
    {
        void DependencyDownloaded(object? sender, (DownloadRequest, DownloadResult) e)
            => Progress.Report(ProgressUpdater.FromIncrementFinishedTasks("DownloadFabricLibraries"));

        Progress.Report(ProgressUpdater.FromRunning("DownloadFabricLibraries"));

        cancellationToken.ThrowIfCancellationRequested();

        (var libraries, var _) = instance.GetRequiredLibraries();

        var dependencyResolver = new DependencyResolverBuilder()
            .AddDependencies(libraries)
            .Build();

        Progress.Report(ProgressUpdater.FromUpdateTotalTasks("DownloadFabricLibraries", dependencyResolver.Dependencies.Count));
        dependencyResolver.DependencyDownloaded += DependencyDownloaded;

        var groupDownloadResult = await dependencyResolver.VerifyAndDownloadDependenciesAsync(cancellationToken: cancellationToken);

        if (CheckAllDependencies && groupDownloadResult.Failed.Count > 0)
            throw new IncompleteDependenciesException(groupDownloadResult.Failed, "Dependency files are incomplete");

        Progress.Report(ProgressUpdater.FromFinished("DownloadFabricLibraries"));
    }
}

partial class FabricInstanceInstaller
{
    private readonly Progress<ProgressUpdater> _progress = new();

    IProgress<ProgressUpdater> Progress => _progress;
    IProgress<ProgressUpdater> IProgressReporter.Progress => _progress;

    public event EventHandler<ProgressUpdater>? ProgressChanged
    {
        add => _progress.ProgressChanged += value;
        remove => _progress.ProgressChanged -= value;
    }

    public Dictionary<string, ProgressData> Progresses { get; init; } =
        ProgressReporterHelper.CreateProgressesFromStringArray(
        [
            "ParseOrInstallVanillaInstance",
            "DownloadFabricClientJson",
            "DownloadFabricLibraries"
        ]);
}
