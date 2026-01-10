using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EL2.QuestRecovery;

internal static class QuestGoalDebugBuilder
{
    // Keep output bounded for player-facing UI
    private const int MaxLinesPerSection = 120;
    private const int MaxArrayItemsPreview = 10;
    private const int MaxStringLen = 220;

    private const BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static string BuildGoalDebug(object questObj)
    {
        if (questObj == null) return "";

        var sb = new StringBuilder();

        string objectiveLoreKey = TryGetObjectiveLoreKey(questObj);
        if (!string.IsNullOrWhiteSpace(objectiveLoreKey))
        {
            sb.AppendLine("ObjectiveLoreKey:");
            sb.AppendLine("  " + objectiveLoreKey);
            sb.AppendLine();
        }

        AppendPrereqSection(sb, "CompletionPrerequisites", questObj, "CompletionPrerequisites");
        AppendPrereqSection(sb, "FailurePrerequisite", questObj, "FailurePrerequisite");

        return sb.ToString().TrimEnd();
    }

    private static string TryGetObjectiveLoreKey(object questObj)
    {
        try
        {
            object choiceDef = PatchHelper.ReadObj(questObj, "QuestChoiceDefinition");
            int stepIndex = PatchHelper.ReadInt(questObj, "StepIndex", -1);
            if (choiceDef == null || stepIndex < 0) return null;

            Array steps = PatchHelper.ReadAsArray(choiceDef, "QuestSteps");
            if (steps == null || stepIndex >= steps.Length) return null;

            object step = steps.GetValue(stepIndex);
            if (step == null) return null;

            return PatchHelper.ReadString(step, "ObjectiveLoreKey", null);
        }
        catch
        {
            return null;
        }
    }

    private static void AppendPrereqSection(StringBuilder sb, string title, object questObj, string fieldName)
    {
        Array prereqs = null;
        try { prereqs = PatchHelper.ReadAsArray(questObj, fieldName); }
        catch { prereqs = null; }

        int count = prereqs?.Length ?? 0;
        if (count <= 0) return;

        sb.AppendLine(title + ":");

        int emittedLines = 0;

        for (int i = 0; i < count; i++)
        {
            object prereq = prereqs.GetValue(i);
            if (prereq == null) continue;

            // Prefer the "definition" object if we can find it, but don't show scary failure text if we can't.
            object def = TryGetPrereqDefinition(prereq);
            object dumpTarget = def ?? prereq;

            string typeName = ShortTypeName(dumpTarget.GetType().Name);

            sb.AppendLine($"  [{i}] {typeName}");
            emittedLines++;
            if (emittedLines >= MaxLinesPerSection) break;

            foreach (var line in DumpPrereqLikeObject(dumpTarget))
            {
                sb.AppendLine("      " + line);
                emittedLines++;
                if (emittedLines >= MaxLinesPerSection)
                {
                    sb.AppendLine("      ... (truncated)");
                    emittedLines++;
                    break;
                }
            }

            if (emittedLines >= MaxLinesPerSection) break;
        }

        sb.AppendLine();
    }

    private static object TryGetPrereqDefinition(object prereqObj)
    {
        try
        {
            var t = prereqObj.GetType();

            var f = t.GetField("SimulationPrerequisiteDefinition", AnyInstance);
            if (f != null) return f.GetValue(prereqObj);

            var p = t.GetProperty("SimulationPrerequisiteDefinition", AnyInstance);
            if (p != null && p.CanRead && (p.GetIndexParameters()?.Length ?? 0) == 0)
                return p.GetValue(prereqObj);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> DumpPrereqLikeObject(object obj)
    {
        // We want "player signal", not plumbing.
        var lines = new List<string>();

        try
        {
            var t = obj.GetType();

            // High-noise members (player-unhelpful)
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // universal / internal
                "SourceID",
                "TargetID",
                "Name",
                "SourceTypeName",
                "StartingType",
                "TargetTypeName",
                "Weight",

                // internal / noisy toggles
                "PrerequisiteLocalizationOverride",
                "InjectCompletion",
                "InjectDuration",
                "IsHidden",
                "HideLocate",
                "CompiledPath",
                "Path",
                "PropertyPathIndex",

                // generated backing fields
                "<IgnoreQuestProtectionDataCheck>k__BackingField",
            };

            // Collect candidates: fields then properties (non-null only)
            AddMembers(lines, obj, t, excluded);

            // Special handling: include Path/CompiledPath ONLY if they look meaningful.
            // Your current output frequently shows just type names, which is useless.
            AppendIfMeaningful(lines, obj, t, "Path");
            AppendIfMeaningful(lines, obj, t, "CompiledPath");

            // Stable ordering that surfaces the most useful info first
            lines.Sort((a, b) => ScoreLine(a).CompareTo(ScoreLine(b)));

            if (lines.Count == 0)
                lines.Add("(no additional details)");

            return lines;
        }
        catch
        {
            return new[] { "(definition dump failed)" };
        }
    }

    private static void AddMembers(List<string> lines, object obj, Type t, HashSet<string> excluded)
    {
        // Fields
        foreach (var f in t.GetFields(AnyInstance))
        {
            if (f == null || f.IsStatic) continue;
            string name = f.Name ?? "";
            if (excluded.Contains(name)) continue;

            object v;
            try { v = f.GetValue(obj); }
            catch { continue; }

            if (IsNullOrEmpty(v)) continue;
            lines.Add($"{name}={FormatValue(v)}");
        }

        // Properties
        foreach (var p in t.GetProperties(AnyInstance))
        {
            if (p == null || !p.CanRead) continue;
            if ((p.GetIndexParameters()?.Length ?? 0) > 0) continue;
            if (p.GetMethod != null && p.GetMethod.IsStatic) continue;

            string name = p.Name ?? "";
            if (excluded.Contains(name)) continue;

            // avoid duplicates with same name
            bool already = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith(name + "=", StringComparison.Ordinal))
                {
                    already = true;
                    break;
                }
            }
            if (already) continue;

            object v;
            try { v = p.GetValue(obj, null); }
            catch { continue; }

            if (IsNullOrEmpty(v)) continue;
            lines.Add($"{name}={FormatValue(v)}");
        }
    }

    private static void AppendIfMeaningful(List<string> lines, object obj, Type t, string memberName)
    {
        object v = null;

        try
        {
            var f = t.GetField(memberName, AnyInstance);
            if (f != null) v = f.GetValue(obj);
            else
            {
                var p = t.GetProperty(memberName, AnyInstance);
                if (p != null && p.CanRead && (p.GetIndexParameters()?.Length ?? 0) == 0)
                    v = p.GetValue(obj, null);
            }
        }
        catch { v = null; }

        if (IsNullOrEmpty(v)) return;

        string formatted;
        try { formatted = FormatValue(v); }
        catch { return; }

        // If it looks like just a type name, it's not meaningful for the player.
        // Examples you saw: "Amplitude.Framework.Simulation.Description.Path"
        if (LooksLikeTypeNameOnly(formatted)) return;

        // If it looks like empty braces, it's not meaningful
        if (LooksEmptyBraces(formatted)) return;

        lines.Add($"{memberName}={formatted}");
    }

    private static bool LooksLikeTypeNameOnly(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        s = s.Trim();

        // heuristics: "Namespace.Type" with no braces/values
        // also catches "Amplitude.Framework....Path"
        if (s.IndexOf('{') >= 0) return false;
        if (s.IndexOf('(') >= 0) return false;
        if (s.IndexOf('=') >= 0) return false;

        // If it has multiple dots and no spaces, it's almost certainly a type name
        int dots = 0;
        for (int i = 0; i < s.Length; i++) if (s[i] == '.') dots++;
        if (dots >= 2 && s.IndexOf(' ') < 0) return true;

        return false;
    }

    private static bool LooksEmptyBraces(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        s = s.Trim();
        return s.EndsWith("{}", StringComparison.Ordinal) || s.EndsWith("{ }", StringComparison.Ordinal);
    }

    private static int ScoreLine(string line)
    {
        // Lower => earlier
        if (StartsWithKey(line, "TargetedProperty")) return 0;
        if (StartsWithKey(line, "PropertyValue")) return 1;
        if (StartsWithKey(line, "ValueToReach")) return 2;
        if (StartsWithKey(line, "NumberOfTurnToValidate")) return 3;
        if (StartsWithKey(line, "NumberOfEntityToValidate")) return 4;
        if (StartsWithKey(line, "ComparisonOperator")) return 5;

        if (StartsWithKey(line, "TechnologyReference")) return 10;
        if (StartsWithKey(line, "TechnologyState")) return 11;

        if (StartsWithKey(line, "QuestPOI")) return 20;
        if (StartsWithKey(line, "ProtectedVariable")) return 21;
        if (StartsWithKey(line, "ProtectedTag")) return 22;

        if (StartsWithKey(line, "NumberOfVillagesToPacify")) return 30;
        if (StartsWithKey(line, "NumberOfDungeonToClear")) return 31;
        if (StartsWithKey(line, "NumberToConsume")) return 32;

        return 100;
    }

    private static bool StartsWithKey(string line, string key)
        => line != null && line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase);

    private static string ShortTypeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw ?? "";

        // Remove common long prefixes
        raw = raw.Replace("SimulationPrerequisiteDefinition_", "");
        raw = raw.Replace("SimulationPrerequisiteListenerDefinition_", "");
        raw = raw.Replace("SimulationPrerequisiteListener_", "");
        raw = raw.Replace("SimulationPrerequisiteDefinition", "");
        raw = raw.Trim('_');

        return raw;
    }

    private static bool IsNullOrEmpty(object v)
    {
        if (v == null) return true;

        if (v is string s)
            return string.IsNullOrWhiteSpace(s);

        if (v is Array a)
            return a.Length == 0;

        if (v is IList list)
            return list.Count == 0;

        return false;
    }

    private static string FormatValue(object v)
    {
        if (v == null) return "null";

        if (v is string s)
            return "\"" + Truncate(s, MaxStringLen) + "\"";

        if (v is bool b)
            return b ? "true" : "false";

        var t = v.GetType();
        if (t.IsEnum)
            return v.ToString();

        if (v is int || v is long || v is float || v is double || v is short || v is byte)
            return v.ToString();

        if (v is Array arr)
            return FormatArray(arr);

        if (v is IList list)
            return FormatIList(list);

        string dref = TryFormatDatatableElementReference(v);
        if (!string.IsNullOrEmpty(dref))
            return dref;

        try { return Truncate(v.ToString(), MaxStringLen); }
        catch { return "<unprintable>"; }
    }

    private static string FormatArray(Array arr)
    {
        int len = arr.Length;
        if (len == 0) return arr.GetType().Name + "(Length=0)";

        var sb = new StringBuilder();
        sb.Append(arr.GetType().Name);
        sb.Append("(Length=");
        sb.Append(len);
        sb.Append(")");

        int take = Math.Min(len, MaxArrayItemsPreview);
        sb.Append(" [");

        for (int i = 0; i < take; i++)
        {
            if (i > 0) sb.Append(", ");
            object item = null;
            try { item = arr.GetValue(i); } catch { /* ignore */ }

            if (item == null) sb.Append("null");
            else
            {
                string dref = TryFormatDatatableElementReference(item);
                sb.Append(!string.IsNullOrEmpty(dref) ? dref : Truncate(SafeToString(item), 80));
            }
        }

        if (take < len) sb.Append(", ...");
        sb.Append("]");

        return sb.ToString();
    }

    private static string FormatIList(IList list)
    {
        int count = list.Count;
        if (count == 0) return list.GetType().Name + "(Count=0)";

        var sb = new StringBuilder();
        sb.Append(list.GetType().Name);
        sb.Append("(Count=");
        sb.Append(count);
        sb.Append(")");

        int take = Math.Min(count, MaxArrayItemsPreview);
        sb.Append(" [");

        for (int i = 0; i < take; i++)
        {
            if (i > 0) sb.Append(", ");
            object item = null;
            try { item = list[i]; } catch { /* ignore */ }

            if (item == null) sb.Append("null");
            else
            {
                string dref = TryFormatDatatableElementReference(item);
                sb.Append(!string.IsNullOrEmpty(dref) ? dref : Truncate(SafeToString(item), 80));
            }
        }

        if (take < count) sb.Append(", ...");
        sb.Append("]");

        return sb.ToString();
    }

    private static string SafeToString(object o)
    {
        try { return o?.ToString() ?? ""; }
        catch { return "<unprintable>"; }
    }

    private static string TryFormatDatatableElementReference(object maybeRef)
    {
        if (maybeRef == null) return null;

        var t = maybeRef.GetType();
        string typeName = t.Name ?? "";

        bool looksLikeRef =
            typeName.IndexOf("DatatableElementReference", StringComparison.OrdinalIgnoreCase) >= 0 ||
            t.FullName?.IndexOf("DatatableElementReference", StringComparison.OrdinalIgnoreCase) >= 0;

        if (!looksLikeRef)
            return null;

        object idVal = null;

        string[] candidates =
        {
            "Value",
            "Reference",
            "Name",
            "Key",
            "Id",
            "ID",
            "StaticString",
            "ElementName",
            "ElementId",
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string name = candidates[i];

            var f = t.GetField(name, AnyInstance);
            if (f != null)
            {
                try { idVal = f.GetValue(maybeRef); }
                catch { /* ignore */ }
            }

            if (idVal == null)
            {
                var p = t.GetProperty(name, AnyInstance);
                if (p != null && p.CanRead && (p.GetIndexParameters()?.Length ?? 0) == 0)
                {
                    try { idVal = p.GetValue(maybeRef, null); }
                    catch { /* ignore */ }
                }
            }

            if (idVal != null)
                break;
        }

        if (idVal != null)
        {
            string inner;
            try { inner = idVal.ToString(); }
            catch { inner = "<unprintable>"; }

            inner = Truncate(inner, 120);
            if (!string.IsNullOrWhiteSpace(inner))
                return $"{typeName}({inner})";
        }

        return $"{typeName}(<set>)";
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length <= max) return s;
        return s.Substring(0, max) + "...";
    }
}