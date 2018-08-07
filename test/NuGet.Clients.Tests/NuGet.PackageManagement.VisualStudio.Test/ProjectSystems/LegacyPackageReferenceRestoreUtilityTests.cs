// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft;
using NuGet.Commands;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Protocol.Core.Types;
using NuGet.Test.Utility;
using NuGet.Versioning;
using NuGet.VisualStudio;
using Test.Utility;
using Test.Utility.Threading;
using Xunit;

namespace NuGet.PackageManagement.VisualStudio.Test
{
    [Collection(DispatcherThreadCollection.CollectionName)]
    public class LegacyPackageReferenceRestoreUtilityTests
    {
        private readonly IVsProjectThreadingService _threadingService;

        public LegacyPackageReferenceRestoreUtilityTests(DispatcherThreadFixture fixture)
        {
            Assumes.Present(fixture);

            _threadingService = new TestProjectThreadingService(fixture.JoinableTaskFactory);
        }

        [Fact]
        public async void DependencyGraphRestoreUtility_LegacyPackageRef_Restore_Success()
        {
            using (var packageSource = TestDirectory.Create())
            {
                // Arrange
                var sourceRepositoryProvider = TestSourceRepositoryUtility.CreateSourceRepositoryProvider(
                new List<Configuration.PackageSource>()
                {
                    new Configuration.PackageSource(packageSource.Path)
                });

                using (var testSolutionManager = new TestSolutionManager(true))
                using (var randomProjectFolderPath = TestDirectory.Create())
                {
                    var testSettings = PopulateSettingsWithSources(sourceRepositoryProvider, randomProjectFolderPath);
                    var testNuGetProjectContext = new TestNuGetProjectContext();
                    var deleteOnRestartManager = new TestDeleteOnRestartManager();
                    var nuGetPackageManager = new NuGetPackageManager(
                        sourceRepositoryProvider,
                        testSettings,
                        testSolutionManager,
                        deleteOnRestartManager);

                    var projectTargetFrameworkStr = "net45";
                    var fullProjectPath = Path.Combine(randomProjectFolderPath, "project1.csproj");
                    var projectNames = new ProjectNames(
                        fullName: fullProjectPath,
                        uniqueName: Path.GetFileName(fullProjectPath),
                        shortName: Path.GetFileNameWithoutExtension(fullProjectPath),
                        customUniqueName: Path.GetFileName(fullProjectPath));
                    var vsProjectAdapter = new TestVSProjectAdapter(
                        fullProjectPath,
                        projectNames,
                        projectTargetFrameworkStr);

                    var projectServices = new TestProjectSystemServices();
                    projectServices.SetupInstalledPackages(
                        NuGetFramework.Parse(projectTargetFrameworkStr),
                        new LibraryDependency
                        {
                            LibraryRange = new LibraryRange(
                                "packageA",
                                VersionRange.Parse("1.*"),
                                LibraryDependencyTarget.Package)
                        });

                    var legacyPRProject = new LegacyPackageReferenceProject(
                        vsProjectAdapter,
                        Guid.NewGuid().ToString(),
                        projectServices,
                        _threadingService);
                    testSolutionManager.NuGetProjects.Add(legacyPRProject);

                    var testLogger = new TestLogger();
                    var restoreContext = new DependencyGraphCacheContext(testLogger, testSettings);
                    var providersCache = new RestoreCommandProvidersCache();

                    var packageContext = new SimpleTestPackageContext("packageA", "1.0.0");
                    packageContext.AddFile("lib/net45/a.dll");
                    SimpleTestPackageUtility.CreateOPCPackage(packageContext, packageSource);

                    var dgSpec = await DependencyGraphRestoreUtility.GetSolutionRestoreSpec(testSolutionManager, restoreContext);

                    // Act
                    var restoreSummaries = await DependencyGraphRestoreUtility.RestoreAsync(
                        testSolutionManager,
                        restoreContext,
                        providersCache,
                        (c) => { },
                        sourceRepositoryProvider.GetRepositories(),
                        Guid.Empty,
                        false,
                        dgSpec,
                        testLogger,
                        CancellationToken.None);

                    // Assert
                    foreach (var restoreSummary in restoreSummaries)
                    {
                        Assert.True(restoreSummary.Success);
                        Assert.False(restoreSummary.NoOpRestore);
                        Assert.Equal(1, restoreSummary.InstallCount);
                    }
                }
            }
        }

        [Fact]
        public async void DependencyGraphRestoreUtility_LegacyPackageRef_Restore_GenerateLockFile()
        {
            using (var packageSource = TestDirectory.Create())
            {
                // Arrange
                var sourceRepositoryProvider = TestSourceRepositoryUtility.CreateSourceRepositoryProvider(
                new List<Configuration.PackageSource>()
                {
                    new Configuration.PackageSource(packageSource.Path)
                });

                using (var testSolutionManager = new TestSolutionManager(true))
                using (var randomProjectFolderPath = TestDirectory.Create())
                {
                    var testSettings = PopulateSettingsWithSources(sourceRepositoryProvider, randomProjectFolderPath);
                    var testNuGetProjectContext = new TestNuGetProjectContext();
                    var deleteOnRestartManager = new TestDeleteOnRestartManager();
                    var nuGetPackageManager = new NuGetPackageManager(
                        sourceRepositoryProvider,
                        testSettings,
                        testSolutionManager,
                        deleteOnRestartManager);

                    var projectTargetFrameworkStr = "net45";
                    var fullProjectPath = Path.Combine(randomProjectFolderPath, "project1.csproj");
                    var projectNames = new ProjectNames(
                        fullName: fullProjectPath,
                        uniqueName: Path.GetFileName(fullProjectPath),
                        shortName: Path.GetFileNameWithoutExtension(fullProjectPath),
                        customUniqueName: Path.GetFileName(fullProjectPath));
                    var vsProjectAdapter = new TestVSProjectAdapter(
                        fullProjectPath,
                        projectNames,
                        projectTargetFrameworkStr,
                        restorePackagesWithLockFile: "true");

                    var projectServices = new TestProjectSystemServices();
                    projectServices.SetupInstalledPackages(
                        NuGetFramework.Parse(projectTargetFrameworkStr),
                        new LibraryDependency
                        {
                            LibraryRange = new LibraryRange(
                                "packageA",
                                VersionRange.Parse("1.*"),
                                LibraryDependencyTarget.Package)
                        });

                    var legacyPRProject = new LegacyPackageReferenceProject(
                        vsProjectAdapter,
                        Guid.NewGuid().ToString(),
                        projectServices,
                        _threadingService);
                    testSolutionManager.NuGetProjects.Add(legacyPRProject);

                    var testLogger = new TestLogger();
                    var restoreContext = new DependencyGraphCacheContext(testLogger, testSettings);
                    var providersCache = new RestoreCommandProvidersCache();

                    var packageContext = new SimpleTestPackageContext("packageA", "1.0.0");
                    packageContext.AddFile("lib/net45/a.dll");
                    SimpleTestPackageUtility.CreateOPCPackage(packageContext, packageSource);

                    var dgSpec = await DependencyGraphRestoreUtility.GetSolutionRestoreSpec(testSolutionManager, restoreContext);

                    // Act
                    var restoreSummaries = await DependencyGraphRestoreUtility.RestoreAsync(
                        testSolutionManager,
                        restoreContext,
                        providersCache,
                        (c) => { },
                        sourceRepositoryProvider.GetRepositories(),
                        Guid.Empty,
                        false,
                        dgSpec,
                        testLogger,
                        CancellationToken.None);

                    // Assert
                    foreach (var restoreSummary in restoreSummaries)
                    {
                        Assert.True(restoreSummary.Success);
                        Assert.False(restoreSummary.NoOpRestore);
                        Assert.Equal(1, restoreSummary.InstallCount);
                    }

                    Assert.True(File.Exists(NuGetLockFileUtilities.GetNuGetLockFilePath(dgSpec.GetProjectSpec(Path.GetFileName(fullProjectPath)))));
                }
            }
        }

        [Fact]
        public async void DependencyGraphRestoreUtility_LegacyPackageRef_Restore_ReadLockFile()
        {
            using (var packageSource = TestDirectory.Create())
            {
                // Arrange
                var sourceRepositoryProvider = TestSourceRepositoryUtility.CreateSourceRepositoryProvider(
                new List<Configuration.PackageSource>()
                {
                    new Configuration.PackageSource(packageSource.Path)
                });

                using (var testSolutionManager = new TestSolutionManager(true))
                using (var randomProjectFolderPath = TestDirectory.Create())
                {
                    var testSettings = PopulateSettingsWithSources(sourceRepositoryProvider, randomProjectFolderPath);
                    var testNuGetProjectContext = new TestNuGetProjectContext();
                    var deleteOnRestartManager = new TestDeleteOnRestartManager();
                    var nuGetPackageManager = new NuGetPackageManager(
                        sourceRepositoryProvider,
                        testSettings,
                        testSolutionManager,
                        deleteOnRestartManager);

                    var projectTargetFrameworkStr = "net45";
                    var fullProjectPath = Path.Combine(randomProjectFolderPath, "project1.csproj");
                    var projectNames = new ProjectNames(
                        fullName: fullProjectPath,
                        uniqueName: Path.GetFileName(fullProjectPath),
                        shortName: Path.GetFileNameWithoutExtension(fullProjectPath),
                        customUniqueName: Path.GetFileName(fullProjectPath));
                    var vsProjectAdapter = new TestVSProjectAdapter(
                        fullProjectPath,
                        projectNames,
                        projectTargetFrameworkStr);

                    var projectServices = new TestProjectSystemServices();
                    projectServices.SetupInstalledPackages(
                        NuGetFramework.Parse(projectTargetFrameworkStr),
                        new LibraryDependency
                        {
                            LibraryRange = new LibraryRange(
                                "packageA",
                                VersionRange.Parse("1.*"),
                                LibraryDependencyTarget.Package)
                        });

                    var legacyPRProject = new LegacyPackageReferenceProject(
                        vsProjectAdapter,
                        Guid.NewGuid().ToString(),
                        projectServices,
                        _threadingService);
                    testSolutionManager.NuGetProjects.Add(legacyPRProject);

                    var testLogger = new TestLogger();
                    var restoreContext = new DependencyGraphCacheContext(testLogger, testSettings);
                    var providersCache = new RestoreCommandProvidersCache();

                    var packageContext = new SimpleTestPackageContext("packageA", "1.0.0");
                    packageContext.AddFile("lib/net45/a.dll");
                    SimpleTestPackageUtility.CreateOPCPackage(packageContext, packageSource);

                    var dgSpec = await DependencyGraphRestoreUtility.GetSolutionRestoreSpec(testSolutionManager, restoreContext);

                    var projectLockFilePath = Path.Combine(randomProjectFolderPath, "packages.project1.lock.json");
                    File.Create(projectLockFilePath);

                    var restoreSummaries = await DependencyGraphRestoreUtility.RestoreAsync(
                        testSolutionManager,
                        restoreContext,
                        providersCache,
                        (c) => { },
                        sourceRepositoryProvider.GetRepositories(),
                        Guid.Empty,
                        false,
                        dgSpec,
                        testLogger,
                        CancellationToken.None);

                    foreach (var restoreSummary in restoreSummaries)
                    {
                        Assert.True(restoreSummary.Success);
                        Assert.False(restoreSummary.NoOpRestore);
                    }

                    Assert.True(File.Exists(NuGetLockFileUtilities.GetNuGetLockFilePath(dgSpec.GetProjectSpec(Path.GetFileName(fullProjectPath)))));

                    // delete existing restore output files
                    File.Delete(Path.Combine(vsProjectAdapter.MSBuildProjectExtensionsPath, "project.lock.json"));
                    File.Delete(Path.Combine(vsProjectAdapter.MSBuildProjectExtensionsPath, "project1.csproj.nuget.cache"));

                    // add a new package
                    var newPackageContext = new SimpleTestPackageContext("packageA", "1.0.1");
                    newPackageContext.AddFile("lib/net45/a.dll");
                    SimpleTestPackageUtility.CreateOPCPackage(newPackageContext, packageSource);

                    // Act
                    restoreSummaries = await DependencyGraphRestoreUtility.RestoreAsync(
                        testSolutionManager,
                        restoreContext,
                        providersCache,
                        (c) => { },
                        sourceRepositoryProvider.GetRepositories(),
                        Guid.Empty,
                        false,
                        dgSpec,
                        testLogger,
                        CancellationToken.None);

                    // Assert
                    foreach (var restoreSummary in restoreSummaries)
                    {
                        Assert.True(restoreSummary.Success);
                        Assert.False(restoreSummary.NoOpRestore);
                    }

                    var lockFilePath = Path.Combine(vsProjectAdapter.MSBuildProjectExtensionsPath, "project.lock.json");
                    Assert.True(File.Exists(lockFilePath));

                    var lockFile = new LockFileFormat().Read(lockFilePath);
                    var resolvedVersion = lockFile.Targets.First().Libraries.First(library => library.Name.Equals("packageA", StringComparison.OrdinalIgnoreCase)).Version;
                    Assert.Equal("1.0.0", resolvedVersion.ToNormalizedString());
                }
            }
        }

        [Fact]
        public async void DependencyGraphRestoreUtility_LegacyPackageRef_Restore_UpdateLockFile()
        {
            using (var packageSource = TestDirectory.Create())
            {
                // Arrange
                var sourceRepositoryProvider = TestSourceRepositoryUtility.CreateSourceRepositoryProvider(
                new List<Configuration.PackageSource>()
                {
                    new Configuration.PackageSource(packageSource.Path)
                });

                using (var testSolutionManager = new TestSolutionManager(true))
                using (var randomProjectFolderPath = TestDirectory.Create())
                {
                    var testSettings = PopulateSettingsWithSources(sourceRepositoryProvider, randomProjectFolderPath);
                    var testNuGetProjectContext = new TestNuGetProjectContext();
                    var deleteOnRestartManager = new TestDeleteOnRestartManager();
                    var nuGetPackageManager = new NuGetPackageManager(
                        sourceRepositoryProvider,
                        testSettings,
                        testSolutionManager,
                        deleteOnRestartManager);

                    var projectTargetFrameworkStr = "net45";
                    var fullProjectPath = Path.Combine(randomProjectFolderPath, "project1.csproj");
                    var projectLockFilePath = Path.Combine(randomProjectFolderPath, "packages.custom.lock.json");
                    File.Create(projectLockFilePath);

                    var projectNames = new ProjectNames(
                        fullName: fullProjectPath,
                        uniqueName: Path.GetFileName(fullProjectPath),
                        shortName: Path.GetFileNameWithoutExtension(fullProjectPath),
                        customUniqueName: Path.GetFileName(fullProjectPath));
                    var vsProjectAdapter = new TestVSProjectAdapter(
                        fullProjectPath,
                        projectNames,
                        projectTargetFrameworkStr,
                        restorePackagesWithLockFile: null,
                        nuGetLockFilePath: projectLockFilePath);

                    var projectServices = new TestProjectSystemServices();
                    projectServices.SetupInstalledPackages(
                        NuGetFramework.Parse(projectTargetFrameworkStr),
                        new LibraryDependency
                        {
                            LibraryRange = new LibraryRange(
                                "packageA",
                                VersionRange.Parse("1.*"),
                                LibraryDependencyTarget.Package)
                        });

                    var legacyPRProject = new LegacyPackageReferenceProject(
                        vsProjectAdapter,
                        Guid.NewGuid().ToString(),
                        projectServices,
                        _threadingService);
                    testSolutionManager.NuGetProjects.Add(legacyPRProject);

                    var testLogger = new TestLogger();
                    var restoreContext = new DependencyGraphCacheContext(testLogger, testSettings);
                    var providersCache = new RestoreCommandProvidersCache();

                    var packageContextA = new SimpleTestPackageContext("packageA", "1.0.0");
                    packageContextA.AddFile("lib/net45/a.dll");
                    SimpleTestPackageUtility.CreateOPCPackage(packageContextA, packageSource);
                    var packageContextB = new SimpleTestPackageContext("packageB", "1.0.0");
                    packageContextB.AddFile("lib/net45/a.dll");
                    SimpleTestPackageUtility.CreateOPCPackage(packageContextB, packageSource);

                    var dgSpec = await DependencyGraphRestoreUtility.GetSolutionRestoreSpec(testSolutionManager, restoreContext);

                    var restoreSummaries = await DependencyGraphRestoreUtility.RestoreAsync(
                        testSolutionManager,
                        restoreContext,
                        providersCache,
                        (c) => { },
                        sourceRepositoryProvider.GetRepositories(),
                        Guid.Empty,
                        false,
                        dgSpec,
                        testLogger,
                        CancellationToken.None);

                    foreach (var restoreSummary in restoreSummaries)
                    {
                        Assert.True(restoreSummary.Success);
                        Assert.False(restoreSummary.NoOpRestore);
                    }

                    Assert.True(File.Exists(NuGetLockFileUtilities.GetNuGetLockFilePath(dgSpec.GetProjectSpec(Path.GetFileName(fullProjectPath)))));

                    // delete existing restore output files
                    File.Delete(Path.Combine(vsProjectAdapter.MSBuildProjectExtensionsPath, "project.lock.json"));

                    // install a new package
                    projectServices.SetupInstalledPackages(
                        NuGetFramework.Parse(projectTargetFrameworkStr),
                        new LibraryDependency
                        {
                            LibraryRange = new LibraryRange(
                                "packageA",
                                VersionRange.Parse("1.*"),
                                LibraryDependencyTarget.Package)
                        },
                        new LibraryDependency
                        {
                            LibraryRange = new LibraryRange(
                                "packageB",
                                VersionRange.Parse("1.0.0"),
                                LibraryDependencyTarget.Package)
                        });

                    // Act
                    restoreSummaries = await DependencyGraphRestoreUtility.RestoreAsync(
                        testSolutionManager,
                        restoreContext,
                        providersCache,
                        (c) => { },
                        sourceRepositoryProvider.GetRepositories(),
                        Guid.Empty,
                        false,
                        dgSpec,
                        testLogger,
                        CancellationToken.None);

                    // Assert
                    foreach (var restoreSummary in restoreSummaries)
                    {
                        Assert.True(restoreSummary.Success);
                        Assert.False(restoreSummary.NoOpRestore);
                    }

                    Assert.True(File.Exists(projectLockFilePath));

                    var lockFile = NuGetLockFileFormat.Read(projectLockFilePath);
                    Assert.Equal(2, lockFile.Targets.First().Dependencies.Count);
                }
            }
        }

        private ISettings PopulateSettingsWithSources(SourceRepositoryProvider sourceRepositoryProvider, TestDirectory settingsDirectory)
        {
            var Settings = new Settings(settingsDirectory);
            foreach (var source in sourceRepositoryProvider.GetRepositories())
                Settings.SetValue(ConfigurationConstants.PackageSources, ConfigurationConstants.PackageSources, source.PackageSource.Source);

            return Settings;
        }

        private SourceRepositoryProvider CreateSource(List<SourcePackageDependencyInfo> packages)
        {
            var resourceProviders = new List<Lazy<INuGetResourceProvider>>();
            resourceProviders.Add(new Lazy<INuGetResourceProvider>(() => new TestDependencyInfoProvider(packages)));
            resourceProviders.Add(new Lazy<INuGetResourceProvider>(() => new TestMetadataProvider(packages)));

            var packageSource = new Configuration.PackageSource("http://temp");
            var packageSourceProvider = new TestPackageSourceProvider(new[] { packageSource });

            return new SourceRepositoryProvider(packageSourceProvider, resourceProviders);
        }
    }
}
