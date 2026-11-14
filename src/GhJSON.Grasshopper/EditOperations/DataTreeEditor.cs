/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GhJSON.Core.DataEditing;
using GhJSON.Grasshopper.Serialization;
using GhJSON.Grasshopper.Shared;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.EditOperations
{
    /// <summary>
    /// Applies surgical replacements inside persistent data trees (panels and
    /// <see cref="GH_PersistentParam{T}"/> stores) on the live canvas. Values
    /// are supplied in the GhJSON prefixed encoding and decoded through
    /// <see cref="DataTypeRegistry"/>, so edits share the same data-type
    /// contract as document serialization. Existing items that are not
    /// addressed are preserved natively — nothing is serialized and rebuilt.
    /// </summary>
    internal static class DataTreeEditor
    {
        /// <summary>
        /// Validates and applies replacement edits to a panel's text lines.
        /// Executes on the Rhino UI thread. Callers are responsible for
        /// recording undo, expiring downstream objects, and triggering a
        /// recompute or redraw afterwards.
        /// </summary>
        /// <param name="panel">The live panel to mutate.</param>
        /// <param name="edits">The replacement edits.</param>
        /// <returns>The operation outcome.</returns>
        internal static GhJsonDataEditResult EditPanelData(GH_Panel panel, IReadOnlyList<GhJsonDataEdit> edits)
        {
            if (panel == null || edits == null || edits.Count == 0)
            {
                return GhJsonDataEditResult.Fail("Nothing to apply: a panel and at least one edit are required.");
            }

            var result = GhJsonDataEditResult.Fail("Unknown failure");
            UiThreadDispatcher.RunAndWait(() =>
            {
                if (!TryParseAll(edits, out var parsed, out var error))
                {
                    result = GhJsonDataEditResult.Fail(error);
                    return;
                }

                var userText = panel.UserText ?? string.Empty;
                var separator = userText.Contains("\r\n") ? "\r\n" : "\n";

                // GH semantics: Properties.Multiline = true emits the whole text as
                // a single item ("item" mode); false emits each line as an item ("list").
                var itemMode = panel.Properties.Multiline;
                var lines = userText.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                var applies = new List<Action>();

                foreach (var (position, values, raw) in parsed)
                {
                    var isFlatPath = position.Path.Length == 1 && position.Path[0] == 0;
                    if (!isFlatPath)
                    {
                        result = GhJsonDataEditResult.Fail(
                            $"Panel data is a single flat branch {{0}}; position '{raw}' is out of scope.");
                        return;
                    }

                    if (position.IsItem)
                    {
                        if (!TryDecodeText(values, raw, out var decoded, out error))
                        {
                            result = GhJsonDataEditResult.Fail(error);
                            return;
                        }

                        var index = position.ItemIndex!.Value;
                        if (itemMode)
                        {
                            if (index != 0)
                            {
                                result = GhJsonDataEditResult.Fail(
                                    $"Panel '{panel.NickName}' emits a single item; only position '{{0}}(0)' is valid.");
                                return;
                            }
                        }
                        else
                        {
                            if (index >= lines.Count)
                            {
                                result = GhJsonDataEditResult.Fail(
                                    $"Item index {index} is out of range for panel '{panel.NickName}' ({lines.Count} line(s)).");
                                return;
                            }

                            if (decoded.IndexOf('\n') >= 0 || decoded.IndexOf('\r') >= 0)
                            {
                                result = GhJsonDataEditResult.Fail(
                                    $"Edit '{raw}': a list-mode panel line cannot contain line breaks.");
                                return;
                            }
                        }

                        applies.Add(() =>
                        {
                            lines[index] = decoded;
                            panel.UserText = string.Join(separator, lines);
                        });
                    }
                    else
                    {
                        var decodedLines = new List<string>(values.Count);
                        foreach (var token in values)
                        {
                            if (!TryDecodeTextToken(token, raw, out var decoded, out error))
                            {
                                result = GhJsonDataEditResult.Fail(error);
                                return;
                            }

                            if (!itemMode && (decoded.IndexOf('\n') >= 0 || decoded.IndexOf('\r') >= 0))
                            {
                                result = GhJsonDataEditResult.Fail(
                                    $"Edit '{raw}': a list-mode panel line cannot contain line breaks.");
                                return;
                            }

                            decodedLines.Add(decoded);
                        }

                        if (itemMode && decodedLines.Count != 1)
                        {
                            result = GhJsonDataEditResult.Fail(
                                $"Edit '{raw}': panel '{panel.NickName}' emits a single item; supply exactly one value.");
                            return;
                        }

                        applies.Add(() => panel.UserText = string.Join(separator, decodedLines));
                    }
                }

                foreach (var apply in applies)
                {
                    apply();
                }

                var lineCount = (panel.UserText ?? string.Empty).Length == 0
                    ? 1
                    : panel.UserText!.Split('\n').Length;
                result = GhJsonDataEditResult.Ok(
                    applies.Count,
                    new Dictionary<string, int> { ["{0}"] = lineCount });
            });
            return result;
        }

        /// <summary>
        /// Validates and applies replacement edits inside a parameter's
        /// persistent data tree. Executes on the Rhino UI thread. Callers are
        /// responsible for recording undo, expiring downstream objects, and
        /// triggering a recompute or redraw afterwards.
        /// </summary>
        /// <param name="param">The live parameter to mutate.</param>
        /// <param name="edits">The replacement edits.</param>
        /// <returns>The operation outcome.</returns>
        internal static GhJsonDataEditResult EditParamData(IGH_Param param, IReadOnlyList<GhJsonDataEdit> edits)
        {
            if (param == null || edits == null || edits.Count == 0)
            {
                return GhJsonDataEditResult.Fail("Nothing to apply: a parameter and at least one edit are required.");
            }

            var result = GhJsonDataEditResult.Fail("Unknown failure");
            UiThreadDispatcher.RunAndWait(() =>
            {
                if (!TryParseAll(edits, out var parsed, out var error))
                {
                    result = GhJsonDataEditResult.Fail(error);
                    return;
                }

                var persistentDataProperty = param.GetType().GetProperty("PersistentData");
                if (persistentDataProperty?.GetValue(param) is not IGH_Structure structure)
                {
                    result = GhJsonDataEditResult.Fail(
                        $"Parameter '{param.NickName}' ({param.GetType().Name}) has no persistent data store.");
                    return;
                }

                var gooType = GooFactory.FindGenericBaseType(param.GetType(), typeof(GH_PersistentParam<>))?.GetGenericArguments()[0];
                if (gooType == null)
                {
                    result = GhJsonDataEditResult.Fail(
                        $"Parameter '{param.NickName}' ({param.GetType().Name}) is not a persistent parameter.");
                    return;
                }

                var applies = new List<Action>();
                foreach (var (position, values, raw) in parsed)
                {
                    var ghPath = new GH_Path(position.Path);
                    var pathText = $"{{{string.Join(";", position.Path)}}}";
                    if (!structure.PathExists(ghPath))
                    {
                        result = GhJsonDataEditResult.Fail(
                            $"Edit '{raw}': branch {pathText} does not exist on '{param.NickName}'.");
                        return;
                    }

                    if (structure.get_Branch(ghPath) is not IList branch)
                    {
                        result = GhJsonDataEditResult.Fail(
                            $"Edit '{raw}': branch {pathText} on '{param.NickName}' is not editable.");
                        return;
                    }

                    if (position.IsItem)
                    {
                        var index = position.ItemIndex!.Value;
                        if (index >= branch.Count)
                        {
                            result = GhJsonDataEditResult.Fail(
                                $"Edit '{raw}': item index {index} is out of range for branch {pathText} ({branch.Count} item(s)).");
                            return;
                        }

                        if (!TryDecodeGoo(values, gooType, param, raw, out var goo, out error))
                        {
                            result = GhJsonDataEditResult.Fail(error);
                            return;
                        }

                        applies.Add(() => branch[index] = goo);
                    }
                    else
                    {
                        var goos = new List<object?>(values.Count);
                        foreach (var token in values)
                        {
                            if (!TryDecodeGooToken(token, gooType, param, raw, out var goo, out error))
                            {
                                result = GhJsonDataEditResult.Fail(error);
                                return;
                            }

                            goos.Add(goo);
                        }

                        applies.Add(() =>
                        {
                            branch.Clear();
                            foreach (var goo in goos)
                            {
                                branch.Add(goo);
                            }
                        });
                    }
                }

                foreach (var apply in applies)
                {
                    apply();
                }

                var shape = new Dictionary<string, int>();
                foreach (var path in structure.Paths)
                {
                    shape[path.ToString()] = structure.get_Branch(path)?.Count ?? 0;
                }

                result = GhJsonDataEditResult.Ok(applies.Count, shape);
            });
            return result;
        }

        private static bool TryParseAll(
            IReadOnlyList<GhJsonDataEdit> edits,
            out List<(GhJsonDataPosition position, IReadOnlyList<string> values, string raw)> parsed,
            out string error)
        {
            parsed = new List<(GhJsonDataPosition, IReadOnlyList<string>, string)>(edits.Count);
            error = string.Empty;

            for (var i = 0; i < edits.Count; i++)
            {
                var edit = edits[i];
                if (!GhJsonDataPosition.TryParse(edit?.Position, out var position, out var parseError))
                {
                    error = $"Edit #{i}: {parseError}";
                    return false;
                }

                var values = edit.Values ?? Array.Empty<string>();
                if (position.IsItem && values.Count != 1)
                {
                    error = $"Edit #{i} ({edit.Position}): item positions require exactly one encoded value.";
                    return false;
                }

                parsed.Add((position, values, edit.Position));
            }

            // Reject overlapping edits: duplicates, or a branch edit combined
            // with an item edit inside the same branch.
            var keys = new HashSet<GhJsonDataPosition>();
            var branchPaths = new HashSet<string>(StringComparer.Ordinal);
            var itemPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (position, _, raw) in parsed)
            {
                if (!keys.Add(position))
                {
                    error = $"Overlapping edits: '{raw}' is targeted more than once.";
                    return false;
                }

                var pathKey = string.Join(";", position.Path);
                if (position.IsItem)
                {
                    itemPaths.Add(pathKey);
                }
                else
                {
                    branchPaths.Add(pathKey);
                }
            }

            if (branchPaths.Overlaps(itemPaths))
            {
                error = "Overlapping edits: a branch replacement cannot be combined with item edits inside the same branch.";
                return false;
            }

            return true;
        }

        private static bool TryDecodeText(IReadOnlyList<string> values, string raw, out string decoded, out string error)
        {
            return TryDecodeTextToken(values.Count == 1 ? values[0] : null, raw, out decoded, out error);
        }

        private static bool TryDecodeTextToken(string? encoded, string raw, out string decoded, out string error)
        {
            decoded = string.Empty;
            error = string.Empty;
            if (string.IsNullOrEmpty(encoded))
            {
                error = $"Edit '{raw}': panel values must be 'text:'-encoded strings.";
                return false;
            }

            object? value;
            try
            {
                value = DataTypeRegistry.Deserialize(encoded);
            }
            catch (Exception ex)
            {
                error = $"Edit '{raw}': invalid encoded value '{encoded}' ({ex.Message}).";
                return false;
            }

            if (value is not string text)
            {
                error = value == null
                    ? $"Edit '{raw}': unrecognized encoding '{encoded}'. Panels store text; use 'text:…'."
                    : $"Edit '{raw}': panels store text; use 'text:…' (got '{encoded}').";
                return false;
            }

            decoded = text;
            return true;
        }

        private static bool TryDecodeGoo(IReadOnlyList<string> values, Type gooType, IGH_Param param, string raw, out object? goo, out string error)
        {
            return TryDecodeGooToken(values.Count == 1 ? values[0] : null, gooType, param, raw, out goo, out error);
        }

        private static bool TryDecodeGooToken(string? encoded, Type gooType, IGH_Param param, string raw, out object? goo, out string error)
        {
            goo = null;
            error = string.Empty;
            if (string.IsNullOrEmpty(encoded))
            {
                error = $"Edit '{raw}': values must be GhJSON-encoded strings (e.g. 'text:…', 'pointXYZ:1,2,3').";
                return false;
            }

            object? decoded;
            try
            {
                decoded = DataTypeRegistry.Deserialize(encoded);
            }
            catch (Exception ex)
            {
                error = $"Edit '{raw}': invalid encoded value '{encoded}' ({ex.Message}).";
                return false;
            }

            if (decoded == null)
            {
                error = $"Edit '{raw}': unrecognized data-type encoding '{encoded}'. Use a GhJSON prefixed value (text:, number:, integer:, boolean:, pointXYZ:, vectorXYZ:, line2p:, planeOXY:, circleCNRS:, arc3P:, boxOXY:, rectangleCXY:, interval:, bounds:, argb:).";
                return false;
            }

            goo = GooFactory.Create(gooType, decoded);
            if (goo == null)
            {
                error = $"Edit '{raw}': value of type '{decoded.GetType().Name}' cannot be stored in '{param.NickName}' ({gooType.Name}).";
                return false;
            }

            return true;
        }

    }
}
