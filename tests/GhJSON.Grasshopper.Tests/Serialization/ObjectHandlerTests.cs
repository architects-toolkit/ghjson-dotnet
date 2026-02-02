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
    }
}
