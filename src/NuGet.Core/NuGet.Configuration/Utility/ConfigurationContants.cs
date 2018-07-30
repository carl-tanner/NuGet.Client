// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.ObjectModel;

namespace NuGet.Configuration
{
    public static class ConfigurationConstants
    {
        public static readonly string Add = "add";

        public static readonly string Clear = "clear";

        public static readonly string ApiKeys = "apikeys";

        public static readonly string DisabledPackageSources = "disabledPackageSources";

        public static readonly string PackageSources = "packageSources";

        public static readonly string DefaultPushSource = "DefaultPushSource";

        public static readonly string PackageRestore = "packageRestore";

        public static readonly string Config = "config";

        public static readonly string Enabled = "enabled";

        public static readonly string ConfigurationDefaultsFile = "NuGetDefaults.config";

        public static readonly string CredentialsSectionName = "packageSourceCredentials";

        public static readonly string UsernameToken = "Username";

        public static readonly string PasswordToken = "Password";

        public static readonly string ClearTextPasswordToken = "ClearTextPassword";

        public static readonly string ActivePackageSourceSectionName = "activePackageSource";

        public static readonly string HostKey = "http_proxy";

        public static readonly string UserKey = "http_proxy.user";

        public static readonly string PasswordKey = "http_proxy.password";
        
        public static readonly string NoProxy = "no_proxy";

        public static readonly string KeyAttribute = "key";

        public static readonly string ValueAttribute = "value";

        public static readonly string ProtocolVersionAttribute = "protocolVersion";

        public static readonly string BeginIgnoreMarker = "NUGET: BEGIN LICENSE TEXT";

        public static readonly string EndIgnoreMarker = "NUGET: END LICENSE TEXT";

        public static readonly string FallbackPackageFolders = "fallbackPackageFolders";

        public static readonly string FingerprintAlgorithm = "fingerprintAlgorithm";

        public static readonly string ServiceIndex = "serviceIndex";

        public static readonly string Configuration = "configuration";

        public static readonly string RepositoryPath = "repositoryPath";

        public static readonly string GlobalPackagesFolder = "globalPackagesFolder";

        public static readonly string BindingRedirectsSection = "bindingRedirects";

        public static readonly string SkipBindingRedirectsKey = "skip";

        public static readonly string FailOnBindingRedirects = "successRequired";

        public static readonly string PackageManagementSection = "packageManagement";

        public static readonly string DoNotShowPackageManagementSelectionKey = "disabled";

        public static readonly string DefaultPackageManagementFormatKey = "format";
    }
}
