/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GhJSON.Core.SchemaModels;
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.GetOperations
{
    /// <summary>
    /// Builds GhJSON metadata from Grasshopper document information and user-provided options.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The metadata builder automatically populates the following properties:
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Property</term>
    ///     <description>Source</description>
    ///   </listheader>
    ///   <item>
    ///     <term>title</term>
    ///     <description>User override → Document file name (without extension) → null</description>
    ///   </item>
    ///   <item>
    ///     <term>description</term>
    ///     <description>User override only</description>
    ///   </item>
    ///   <item>
    ///     <term>version</term>
    ///     <description>User override only</description>
    ///   </item>
    ///   <item>
    ///     <term>author</term>
    ///     <description>User override only</description>
    ///   </item>
    ///   <item>
    ///     <term>tags</term>
    ///     <description>User override only</description>
    ///   </item>
    ///   <item>
    ///     <term>modified</term>
    ///     <description>Current UTC timestamp</description>
    ///   </item>
    ///   <item>
    ///     <term>rhinoVersion</term>
    ///     <description>Rhino.RhinoApp.Version</description>
    ///   </item>
    ///   <item>
    ///     <term>grasshopperVersion</term>
    ///     <description>Grasshopper.Versioning.Version</description>
    ///   </item>
    ///   <item>
    ///     <term>componentCount</term>
    ///     <description>Count of serialized components</description>
    ///   </item>
    ///   <item>
    ///     <term>connectionCount</term>
    ///     <description>Count of extracted connections</description>
    ///   </item>
    ///   <item>
    ///     <term>groupCount</term>
    ///     <description>Count of extracted groups</description>
    ///   </item>
    ///   <item>
    ///     <term>dependencies</term>
    ///     <description>Non-standard plugin assemblies used by components</description>
    ///   </item>
    ///   <item>
    ///     <term>generatorName</term>
    ///     <description>User override → "ghjson-dotnet"</description>
    ///   </item>
    ///   <item>
    ///     <term>generatorVersion</term>
    ///     <description>User override → ghjson-dotnet assembly version</description>
    ///   </item>
    /// </list>
    /// </remarks>
    internal static class MetadataBuilder
    {
        /// <summary>
        /// Default generator name used when not overridden.
        /// </summary>
        internal const string DefaultGeneratorName = "ghjson-dotnet";

        /// <summary>
        /// Builds metadata from the provided context.
        /// </summary>
        /// <param name="components">The serialized components.</param>
        /// <param name="connections">The extracted connections (optional).</param>
        /// <param name="groups">The extracted groups (optional).</param>
        /// <param name="ghDocument">The source Grasshopper document (optional).</param>
        /// <param name="options">The get options containing user overrides.</param>
        /// <returns>A populated <see cref="GhJsonMetadata"/> instance.</returns>
        public static GhJsonMetadata Build(
            List<IGH_DocumentObject> components,
            List<GhJsonConnection>? connections,
            List<GhJsonGroup>? groups,
            GH_Document? ghDocument,
            GetOptions options)
        {
            var metadata = new GhJsonMetadata
            {
                Modified = DateTime.UtcNow,
                ComponentCount = components.Count,
            };

            ApplyUserOverrides(metadata, options, ghDocument);
            ApplyVersionInfo(metadata);
            ApplyCounts(metadata, connections, groups);
            ApplyGeneratorInfo(metadata, options);
            ApplyDependencies(metadata, components);

            return metadata;
        }

        /// <summary>
        /// Applies user-provided metadata overrides from options.
        /// </summary>
        private static void ApplyUserOverrides(GhJsonMetadata metadata, GetOptions options, GH_Document? ghDocument)
        {
            // Title: user override → document file name → null
            if (!string.IsNullOrWhiteSpace(options.MetadataTitle))
            {
                metadata.Title = options.MetadataTitle;
            }
            else
            {
                try
                {
                    var filePath = ghDocument?.FilePath;
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        metadata.Title = Path.GetFileNameWithoutExtension(filePath);
                    }
                }
                catch
                {
                    // Ignore file path errors
                }
            }

            // Description: user override only
            if (!string.IsNullOrWhiteSpace(options.MetadataDescription))
            {
                metadata.Description = options.MetadataDescription;
            }

            // Version: user override only
            if (!string.IsNullOrWhiteSpace(options.MetadataVersion))
            {
                metadata.Version = options.MetadataVersion;
            }

            // Author: user override only
            if (!string.IsNullOrWhiteSpace(options.MetadataAuthor))
            {
                metadata.Author = options.MetadataAuthor;
            }

            // Tags: user override only
            if (options.MetadataTags != null && options.MetadataTags.Count > 0)
            {
                metadata.Tags = new List<string>(options.MetadataTags);
            }
        }

        /// <summary>
        /// Applies Rhino and Grasshopper version information.
        /// </summary>
        private static void ApplyVersionInfo(GhJsonMetadata metadata)
        {
            // Rhino version
            try
            {
                metadata.RhinoVersion = Rhino.RhinoApp.Version.ToString();
            }
            catch
            {
                // Rhino might not be available in all contexts
            }

            // Grasshopper version (use Versioning.Version, not the assembly version)
            try
            {
                metadata.GrasshopperVersion = Versioning.Version.ToString();
            }
            catch
            {
                // Ignore if Grasshopper versioning is unavailable
            }
        }

        /// <summary>
        /// Applies connection and group counts.
        /// </summary>
        private static void ApplyCounts(GhJsonMetadata metadata, List<GhJsonConnection>? connections, List<GhJsonGroup>? groups)
        {
            if (connections != null)
            {
                metadata.ConnectionCount = connections.Count;
            }

            if (groups != null)
            {
                metadata.GroupCount = groups.Count;
            }
        }

        /// <summary>
        /// Applies generator name and version.
        /// </summary>
        private static void ApplyGeneratorInfo(GhJsonMetadata metadata, GetOptions options)
        {
            // Generator name: user override → default
            metadata.GeneratorName = !string.IsNullOrWhiteSpace(options.MetadataGeneratorName)
                ? options.MetadataGeneratorName
                : DefaultGeneratorName;

            // Generator version: user override → assembly version
            if (!string.IsNullOrWhiteSpace(options.MetadataGeneratorVersion))
            {
                metadata.GeneratorVersion = options.MetadataGeneratorVersion;
            }
            else
            {
                try
                {
                    var asm = typeof(MetadataBuilder).Assembly;
                    var infoVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    metadata.GeneratorVersion = infoVersion ?? asm.GetName().Version?.ToString();
                }
                catch
                {
                    // Ignore version retrieval errors
                }
            }
        }

        /// <summary>
        /// Collects non-standard plugin dependencies from component assemblies.
        /// </summary>
        private static void ApplyDependencies(GhJsonMetadata metadata, List<IGH_DocumentObject> components)
        {
            try
            {
                var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var grasshopperAssembly = typeof(Instances).Assembly.GetName().Name;

                foreach (var obj in components)
                {
                    var asmName = obj.GetType().Assembly.GetName().Name;
                    if (asmName != null &&
                        !asmName.StartsWith("Grasshopper", StringComparison.OrdinalIgnoreCase) &&
                        !asmName.StartsWith("RhinoCommon", StringComparison.OrdinalIgnoreCase) &&
                        !asmName.StartsWith("GH_IO", StringComparison.OrdinalIgnoreCase) &&
                        !asmName.Equals(grasshopperAssembly, StringComparison.OrdinalIgnoreCase))
                    {
                        deps.Add(asmName);
                    }
                }

                if (deps.Count > 0)
                {
                    metadata.Dependencies = new List<string>(deps);
                }
            }
            catch
            {
                // Ignore dependency collection errors
            }
        }
    }
}
