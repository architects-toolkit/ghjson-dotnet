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

using System.Diagnostics;
using GhJSON.Core.Serialization.DataTypes;

namespace GhJSON.Grasshopper.Serialization.DataTypes
{
    /// <summary>
    /// Registers geometric data type serializers with the DataTypeRegistry.
    /// Call <see cref="Initialize"/> once at application startup to register all
    /// Rhino geometry serializers.
    /// </summary>
    public static class GeometricSerializerRegistry
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes and registers all geometric serializers with the DataTypeRegistry.
        /// This method is thread-safe and can be called multiple times; subsequent calls have no effect.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                var registry = DataTypeRegistry.Instance;

                // Register geometric data type serializers
                registry.RegisterSerializer(new PointSerializer());
                registry.RegisterSerializer(new VectorSerializer());
                registry.RegisterSerializer(new LineSerializer());
                registry.RegisterSerializer(new PlaneSerializer());
                registry.RegisterSerializer(new CircleSerializer());
                registry.RegisterSerializer(new ArcSerializer());
                registry.RegisterSerializer(new BoxSerializer());
                registry.RegisterSerializer(new RectangleSerializer());
                registry.RegisterSerializer(new IntervalSerializer());

                Debug.WriteLine("[GeometricSerializerRegistry] Registered all geometric serializers");

                _initialized = true;
            }
        }
    }
}
