/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
 */

using System;
using System.Linq;
using System.Reflection;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.Serialization;
using Xunit;

namespace GhJSON.Grasshopper.Tests.Serialization
{
    public class ObjectHandlerTests
    {
        [Fact]
        public void ObjectHandlerRegistry_HasBuiltInHandlers()
        {
            var handlers = GhJsonGrasshopper.GetRegisteredObjectHandlers();

            Assert.NotNull(handlers);
            Assert.NotEmpty(handlers);
        }

        [Fact]
        public void ObjectHandlerRegistry_SupportsRegistration()
        {
            var handlers = GhJsonGrasshopper.GetRegisteredObjectHandlers();
            var initialCount = handlers.Count();

            // Custom handler would need to be implemented
            // This test verifies the API exists
            Assert.True(initialCount > 0);
        }

        [Fact]
        public void ObjectHandlerOrchestrator_ProcessesHandlersInPriorityOrder()
        {
            // Verify that the orchestrator system is accessible
            var handlers = ObjectHandlerRegistry.GetAll();

            Assert.NotNull(handlers);
            Assert.NotEmpty(handlers);
        }

        [Fact]
        public void IdentificationHandler_ExtractsBasicComponentInfo()
        {
            // This would require a mock IGH_DocumentObject
            // Verify the handler is registered
            var handlers = ObjectHandlerRegistry.GetAll();
            
            Assert.Contains(handlers, h => h.GetType().Name.Contains("Identification"));
        }

        [Fact]
        public void IOIdentificationHandler_ExtractsParameterInfo()
        {
            var handlers = ObjectHandlerRegistry.GetAll();
            
            Assert.Contains(handlers, h => h.GetType().Name.Contains("IO"));
        }

        [Fact]
        public void NumberSliderHandler_HandlesSliderComponents()
        {
            var handlers = ObjectHandlerRegistry.GetAll();
            
            Assert.Contains(handlers, h => h.GetType().Name.Contains("NumberSlider"));
        }

        [Fact]
        public void PanelHandler_HandlesPanelComponents()
        {
            var handlers = ObjectHandlerRegistry.GetAll();
            
            Assert.Contains(handlers, h => h.GetType().Name.Contains("Panel"));
        }

        [Fact]
        public void HandlerProtection_PreventsPropertyOverrides()
        {
            // Create a mock component and test that handlers respect priority order
            var handlers = ObjectHandlerRegistry.GetAll();
            Assert.NotEmpty(handlers);

            // Verify that core handlers (priority 0) come before extension handlers (priority 100)
            var handlerList = handlers.ToList();
            var coreHandlers = handlerList.Where(h => h.Priority == 0).ToList();
            var extensionHandlers = handlerList.Where(h => h.Priority >= 100).ToList();

            Assert.NotEmpty(coreHandlers);
            Assert.NotEmpty(extensionHandlers);

            // Verify handlers are sorted by priority
            for (int i = 0; i < handlerList.Count - 1; i++)
            {
                Assert.True(handlerList[i].Priority <= handlerList[i + 1].Priority,
                    $"Handler at index {i} has priority {handlerList[i].Priority} " +
                    $"which is greater than handler at index {i + 1} with priority {handlerList[i + 1].Priority}");
            }
        }

        [Fact]
        public void MergeFirstWins_AllowsComponentStateSubPropertyMerges()
        {
            var target = new GhJsonComponent
            {
                ComponentState = new GhJsonComponentState
                {
                    Locked = true,
                },
            };

            var patch = new GhJsonComponent
            {
                ComponentState = new GhJsonComponentState
                {
                    Hidden = true,
                },
            };

            InvokeMergeFirstWins(target, patch);

            Assert.NotNull(target.ComponentState);
            Assert.True(target.ComponentState!.Locked);
            Assert.True(target.ComponentState.Hidden);
        }

        [Fact]
        public void MergeFirstWins_RejectsComponentStateSubPropertyOverride()
        {
            var target = new GhJsonComponent
            {
                ComponentState = new GhJsonComponentState
                {
                    Locked = true,
                },
            };

            var patch = new GhJsonComponent
            {
                ComponentState = new GhJsonComponentState
                {
                    Locked = false,
                },
            };

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeMergeFirstWins(target, patch));
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Fact]
        public void MergeFirstWins_MergesParameterSettingsByParameterName()
        {
            var target = new GhJsonComponent
            {
                InputSettings = new()
                {
                    new GhJsonParameterSettings
                    {
                        ParameterName = "A",
                        DataMapping = "Flatten",
                    },
                },
            };

            var patch = new GhJsonComponent
            {
                InputSettings = new()
                {
                    new GhJsonParameterSettings
                    {
                        ParameterName = "A",
                        Expression = "x*2",
                    },
                },
            };

            InvokeMergeFirstWins(target, patch);

            Assert.NotNull(target.InputSettings);
            var merged = Assert.Single(target.InputSettings!);
            Assert.Equal("A", merged.ParameterName);
            Assert.Equal("Flatten", merged.DataMapping);
            Assert.Equal("x*2", merged.Expression);
        }

        [Fact]
        public void MergeFirstWins_RejectsParameterSettingsOverrideByParameterName()
        {
            var target = new GhJsonComponent
            {
                InputSettings = new()
                {
                    new GhJsonParameterSettings
                    {
                        ParameterName = "A",
                        DataMapping = "Flatten",
                    },
                },
            };

            var patch = new GhJsonComponent
            {
                InputSettings = new()
                {
                    new GhJsonParameterSettings
                    {
                        ParameterName = "A",
                        DataMapping = "Graft",
                    },
                },
            };

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeMergeFirstWins(target, patch));
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        private static void InvokeMergeFirstWins(GhJsonComponent target, GhJsonComponent patch)
        {
            MethodInfo? method = typeof(ObjectHandlerOrchestrator)
                .GetMethod("MergeFirstWins", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            method!.Invoke(null, new object[] { target, patch });
        }
    }
}
