﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Ploch.Common;

namespace Ploch.EditorConfigTools.ConsoleUI
{
    public static class ConfigurationSetup
    {
        public static void DefaultFileConfiguration(IConfigurationBuilder configurationBuilder,
                                                    IEnumerable<string>? configurationFileNames = null,
                                                    Action<IConfigurationBuilder>? configurationBuilderAction = null)
        {
            if (configurationFileNames == null)
            {
                configurationFileNames = new[] { "appsettings.json" };
            }

            var basePath = EnvironmentUtilities.GetCurrentAppPath();

            var builder = configurationBuilder.SetBasePath(basePath);
            foreach (var fileName in configurationFileNames)
            {
                if (File.Exists(Path.Combine(basePath, fileName)))
                {
                    builder.AddJsonFile(fileName);
                }
            }

            builder.AddCommandLine(EnvironmentUtilities.GetEnvironmentCommandLine().ToArray()).AddEnvironmentVariables();
            configurationBuilderAction?.Invoke(configurationBuilder);
        }
    }
}