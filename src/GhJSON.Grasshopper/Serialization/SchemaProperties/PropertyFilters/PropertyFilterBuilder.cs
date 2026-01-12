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

namespace GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyFilters
{
    /// <summary>
    /// Fluent builder for creating custom property filter rules.
    /// </summary>
    public class PropertyFilterBuilder
    {
        private readonly PropertyFilterRule _rule;

        private PropertyFilterBuilder()
        {
            _rule = new PropertyFilterRule();
        }

        public static PropertyFilterBuilder Create()
        {
            return new PropertyFilterBuilder();
        }

        public static PropertyFilterBuilder FromContext(SerializationContext context)
        {
            var builder = new PropertyFilterBuilder();
            var baseRule = PropertyFilterConfig.ContextRules[context];

            builder._rule.IncludeCore = baseRule.IncludeCore;
            builder._rule.IncludeParameters = baseRule.IncludeParameters;
            builder._rule.IncludeComponents = baseRule.IncludeComponents;
            builder._rule.IncludeCategories = baseRule.IncludeCategories;
            builder._rule.AdditionalIncludes.UnionWith(baseRule.AdditionalIncludes);
            builder._rule.AdditionalExcludes.UnionWith(baseRule.AdditionalExcludes);

            return builder;
        }

        public PropertyFilterBuilder WithCore(bool include = true)
        {
            _rule.IncludeCore = include;
            return this;
        }

        public PropertyFilterBuilder WithParameters(bool include = true)
        {
            _rule.IncludeParameters = include;
            return this;
        }

        public PropertyFilterBuilder WithComponents(bool include = true)
        {
            _rule.IncludeComponents = include;
            return this;
        }

        public PropertyFilterBuilder WithCategories(ComponentCategory categories)
        {
            _rule.IncludeCategories = categories;
            return this;
        }

        public PropertyFilterBuilder AddCategories(ComponentCategory categories)
        {
            _rule.IncludeCategories |= categories;
            return this;
        }

        public PropertyFilterBuilder RemoveCategories(ComponentCategory categories)
        {
            _rule.IncludeCategories &= ~categories;
            return this;
        }

        public PropertyFilterBuilder WithEssentialCategories()
        {
            return WithCategories(ComponentCategory.Essential);
        }

        public PropertyFilterBuilder WithUICategories()
        {
            return WithCategories(ComponentCategory.UI);
        }

        public PropertyFilterBuilder Include(params string[] propertyNames)
        {
            foreach (var property in propertyNames)
            {
                _rule.AdditionalIncludes.Add(property);
            }

            return this;
        }

        public PropertyFilterBuilder Exclude(params string[] propertyNames)
        {
            foreach (var property in propertyNames)
            {
                _rule.AdditionalExcludes.Add(property);
            }

            return this;
        }

        public PropertyFilterBuilder Include(IEnumerable<string> propertyNames)
        {
            _rule.AdditionalIncludes.UnionWith(propertyNames);
            return this;
        }

        public PropertyFilterBuilder Exclude(IEnumerable<string> propertyNames)
        {
            _rule.AdditionalExcludes.UnionWith(propertyNames);
            return this;
        }

        public PropertyFilterBuilder Minimal()
        {
            return WithCore(true)
                .WithParameters(false)
                .WithComponents(false)
                .WithCategories(ComponentCategory.None);
        }

        public PropertyFilterBuilder Maximum()
        {
            return WithCore(true)
                .WithParameters(true)
                .WithComponents(true)
                .WithCategories(ComponentCategory.All);
        }

        public PropertyFilterBuilder Configure(Action<PropertyFilterRule> configureAction)
        {
            configureAction?.Invoke(_rule);
            return this;
        }

        public PropertyFilterRule Build()
        {
            return new PropertyFilterRule
            {
                IncludeCore = _rule.IncludeCore,
                IncludeParameters = _rule.IncludeParameters,
                IncludeComponents = _rule.IncludeComponents,
                IncludeCategories = _rule.IncludeCategories,
                AdditionalIncludes = new HashSet<string>(_rule.AdditionalIncludes),
                AdditionalExcludes = new HashSet<string>(_rule.AdditionalExcludes)
            };
        }

        public PropertyFilter BuildFilter()
        {
            return PropertyFilter.CreateCustom(Build());
        }

        public SchemaProperties.PropertyManagerV2 BuildManager()
        {
            return SchemaProperties.PropertyManagerV2.CreateCustom(Build());
        }
    }

    public static class PropertyFilterBuilderExtensions
    {
        public static PropertyFilterBuilder ForAI(this PropertyFilterBuilder builder)
        {
            return builder.WithCore(true)
                .WithParameters(true)
                .WithComponents(true)
                .WithEssentialCategories()
                .Exclude("VolatileData", "IsValid", "TypeDescription");
        }

        public static PropertyFilterBuilder ForDebugging(this PropertyFilterBuilder builder)
        {
            return builder.Maximum()
                .Include("InstanceDescription", "ComponentGuid", "InstanceGuid");
        }

        public static PropertyFilterBuilder ExcludeRuntime(this PropertyFilterBuilder builder)
        {
            return builder.Exclude(
                "VolatileData", "IsValid", "IsValidWhyNot",
                "TypeDescription", "TypeName", "Boundingbox",
                "ClippingBox", "ReferenceID", "IsReferencedGeometry",
                "IsGeometryLoaded", "QC_Type");
        }
    }
}
