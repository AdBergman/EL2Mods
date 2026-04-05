using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace EL2.SyntheticHarmony
{
    internal static class SyntheticApprovalLogic
    {
        private const string SandboxManagerTypeName = "Amplitude.Mercury.Sandbox.SandboxManager";
        private const string SandboxTypeName = "Amplitude.Mercury.Sandbox.Sandbox";

        private static object cachedSandbox;
        private static MemberInfo sandboxTurnMember;
        private static MemberInfo sandboxStaticMember;
        private static bool initialized;

        private static readonly Dictionary<Type, MemberInfo> isHumanMemberCache = new Dictionary<Type, MemberInfo>();
        private static readonly Dictionary<Type, MemberInfo> isAiMemberCache = new Dictionary<Type, MemberInfo>();
        private static readonly Dictionary<Type, MemberInfo> empireIndexMemberCache = new Dictionary<Type, MemberInfo>();
        private static readonly Dictionary<Type, MemberInfo> settlementsMemberCache = new Dictionary<Type, MemberInfo>();
        private static readonly Dictionary<Type, MemberInfo> approvalMemberCache = new Dictionary<Type, MemberInfo>();
        private static readonly Dictionary<Type, MethodInfo> getPropertyValueMethodCache = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> getPropertyIndexMethodCache = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, FieldInfo> propertiesFieldCache = new Dictionary<Type, FieldInfo>();
        private static readonly Dictionary<string, Type> typeByFullNameCache = new Dictionary<string, Type>();
        private static readonly Dictionary<string, FieldInfo> boxedFieldCache = new Dictionary<string, FieldInfo>();
        private static readonly Dictionary<Type, PropertyInfo> countPropertyCache = new Dictionary<Type, PropertyInfo>();
        private static readonly Dictionary<Type, FieldInfo> countFieldCache = new Dictionary<Type, FieldInfo>();

        private static Type cachedFixedPointType;
        private static MethodInfo cachedFixedPointImplicitFromInt;
        private static MethodInfo cachedFixedPointImplicitFromFloat;
        private static FieldInfo cachedFixedPointRawField;
        private static bool fixedPointReflectionInitialized;

        private static Type cachedSimulationControllerType;
        private static FieldInfo cachedGlobalPropertyRepositoryField;
        private static MethodInfo cachedSetGlobalPropertyValueMethod;
        private static bool globalRepositoryReflectionInitialized;

        internal static int GetTargetAIBaseSettlementApproval()
        {
            int turn = GetTurn();
            return 65 + (turn / 10) * 5;
        }

        internal static bool IsReady()
        {
            if (initialized && cachedSandbox != null && sandboxTurnMember != null)
                return true;

            try
            {
                object sandbox = GetSandbox();
                if (sandbox == null)
                    return false;

                sandboxTurnMember = FindFieldOrPropertyInHierarchy(sandbox.GetType(), "Turn");
                initialized = sandboxTurnMember != null;
                return initialized;
            }
            catch
            {
                initialized = false;
                sandboxTurnMember = null;
                return false;
            }
        }

        internal static int GetTurn()
        {
            if (!IsReady())
                return 0;

            object sandbox = GetSandbox();
            if (sandbox == null || sandboxTurnMember == null)
                return 0;

            try
            {
                object value = GetValue(sandboxTurnMember, sandbox);
                return value is int i ? i : 0;
            }
            catch
            {
                cachedSandbox = null;
                sandboxTurnMember = null;
                initialized = false;
                return 0;
            }
        }

        internal static bool IsHuman(object empire)
        {
            if (empire == null)
                return false;

            MemberInfo member = GetCachedHierarchyMember(isHumanMemberCache, empire.GetType(), "IsControlledByHuman");
            if (member == null)
                return false;

            object value = GetValue(member, empire);
            return value is bool isHuman && isHuman;
        }

        internal static bool IsAI(object empire)
        {
            if (empire == null)
                return false;

            if (IsHuman(empire))
                return false;

            MemberInfo member = GetCachedHierarchyMember(isAiMemberCache, empire.GetType(), "IsAIBrainActivated");
            if (member != null)
            {
                object value = GetValue(member, empire);
                if (value is bool isAiBrainActivated)
                    return isAiBrainActivated;
            }

            return true;
        }

        internal static IEnumerable GetMajorEmpires()
        {
            object sandbox = GetSandbox();
            if (sandbox == null)
                yield break;

            MemberInfo member = FindFieldOrPropertyInHierarchy(sandbox.GetType(), "MajorEmpires");
            if (member == null)
                yield break;

            object value = GetValue(member, sandbox);
            if (!(value is IEnumerable enumerable))
                yield break;

            foreach (object item in enumerable)
            {
                if (item != null)
                    yield return item;
            }
        }

        internal static int GetEmpireIndex(object empire)
        {
            if (empire == null)
                return -1;

            MemberInfo member = GetCachedHierarchyMember(empireIndexMemberCache, empire.GetType(), "Index");
            if (member == null)
                return -1;

            object value = GetValue(member, empire);
            return value is int i ? i : -1;
        }

        internal static bool HasAnySettlements(object empire)
        {
            if (empire == null)
                return false;

            MemberInfo member = GetCachedHierarchyMember(settlementsMemberCache, empire.GetType(), "Settlements");
            if (member == null)
                return false;

            object value = GetValue(member, empire);
            if (value == null)
                return false;

            Type valueType = value.GetType();

            if (!countPropertyCache.TryGetValue(valueType, out PropertyInfo countProperty))
            {
                countProperty = valueType.GetProperty(
                    "Count",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                countPropertyCache[valueType] = countProperty;
            }

            if (countProperty != null)
            {
                object countValue = countProperty.GetValue(value, null);
                return countValue is int count && count > 0;
            }

            if (!countFieldCache.TryGetValue(valueType, out FieldInfo countField))
            {
                countField = valueType.GetField(
                    "Count",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                countFieldCache[valueType] = countField;
            }

            if (countField != null)
            {
                object countValue = countField.GetValue(value);
                return countValue is int count && count > 0;
            }

            return false;
        }

        internal static string GetEmpireApprovalString(object empire)
        {
            if (empire == null)
                return "NULL";

            MemberInfo member = GetCachedHierarchyMember(approvalMemberCache, empire.GetType(), "Approval");
            if (member == null)
                return "NO_MEMBER";

            object value = GetValue(member, empire);
            return value != null ? value.ToString() : "NULL";
        }

        internal static string GetSimulationPropertyValueString(object entity, string propertyName)
        {
            if (entity == null || string.IsNullOrEmpty(propertyName))
                return "NULL";

            try
            {
                Type entityType = entity.GetType();

                if (!getPropertyValueMethodCache.TryGetValue(entityType, out MethodInfo method))
                {
                    method = entityType.GetMethod(
                        "GetPropertyValue",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(string) },
                        null);

                    getPropertyValueMethodCache[entityType] = method;
                }

                if (method == null)
                    return "NO_METHOD";

                object result = method.Invoke(entity, new object[] { propertyName });
                return result != null ? result.ToString() : "NULL";
            }
            catch (Exception ex)
            {
                return "ERR:" + ex.GetType().Name;
            }
        }

        internal static bool ForceAIBaseSettlementApproval(object empire, int approvalValue)
        {
            return ForceSimulationPropertyValue(empire, "BaseSettlementApproval", approvalValue);
        }

        internal static string GetBaseSettlementApprovalString(object empire)
        {
            return GetSimulationPropertyValueString(empire, "basesettlementapproval");
        }

        internal static string GetBonusApprovalOnSettlementString(object empire)
        {
            return GetSimulationPropertyValueString(empire, "bonusapprovalonsettlement");
        }

        internal static string GetSumOfApprovalString(object empire)
        {
            return GetSimulationPropertyValueString(empire, "sumofapproval");
        }

        internal static bool IsBaseSettlementApprovalEqualTo(object empire, int targetValue)
        {
            string current = GetBaseSettlementApprovalString(empire);
            if (string.IsNullOrEmpty(current))
                return false;

            if (current.EndsWith(".00", StringComparison.Ordinal))
                current = current.Substring(0, current.Length - 3);

            return int.TryParse(current, out int parsed) && parsed == targetValue;
        }

        private static bool ForceSimulationPropertyValue(object entity, string propertyName, int targetValue)
        {
            if (entity == null || string.IsNullOrEmpty(propertyName))
                return false;

            int rawValue = ConvertIntToFixedPointRaw(targetValue);
            if (rawValue == int.MinValue)
                return false;

            Type entityType = entity.GetType();

            if (!getPropertyIndexMethodCache.TryGetValue(entityType, out MethodInfo getPropertyIndexMethod))
            {
                getPropertyIndexMethod = entityType.GetMethod(
                    "GetPropertyIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string) },
                    null);

                getPropertyIndexMethodCache[entityType] = getPropertyIndexMethod;
            }

            if (getPropertyIndexMethod == null)
                return false;

            object propertyIndexObject = getPropertyIndexMethod.Invoke(entity, new object[] { propertyName });
            if (!(propertyIndexObject is int propertyIndex) || propertyIndex < 0)
                return false;

            if (!propertiesFieldCache.TryGetValue(entityType, out FieldInfo propertiesField))
            {
                propertiesField = FindFieldInHierarchy(entityType, "Properties");
                propertiesFieldCache[entityType] = propertiesField;
            }

            if (propertiesField == null)
                return false;

            Array propertiesArray = propertiesField.GetValue(entity) as Array;
            if (propertiesArray == null || propertyIndex >= propertiesArray.Length)
                return false;

            object localPropertyBoxed = propertiesArray.GetValue(propertyIndex);
            if (localPropertyBoxed == null)
                return false;

            SetFieldValueIfExists(localPropertyBoxed, "FixedPointRawValue", rawValue);
            int globalPropertyIndex = GetIntFieldValueIfExists(localPropertyBoxed, "GlobalPropertyIndex", -1);
            propertiesArray.SetValue(localPropertyBoxed, propertyIndex);

            if (globalPropertyIndex >= 0)
                TrySetGlobalPropertyValue(globalPropertyIndex, rawValue);

            MemberInfo namedPropertyMember = FindFieldOrPropertyInHierarchy(entityType, propertyName);
            if (namedPropertyMember is FieldInfo fieldInfo)
            {
                object propertyStructBoxed = fieldInfo.GetValue(entity);
                if (propertyStructBoxed != null)
                {
                    SetFieldValueIfExists(propertyStructBoxed, "FixedPointRawValue", rawValue);
                    fieldInfo.SetValue(entity, propertyStructBoxed);
                }
            }

            return true;
        }

        private static int ConvertIntToFixedPointRaw(int value)
        {
            EnsureFixedPointReflection();

            if (cachedFixedPointType == null || cachedFixedPointRawField == null)
                return int.MinValue;

            if (cachedFixedPointImplicitFromInt != null)
            {
                object fixedPoint = cachedFixedPointImplicitFromInt.Invoke(null, new object[] { value });
                if (fixedPoint != null)
                    return (int)cachedFixedPointRawField.GetValue(fixedPoint);
            }

            if (cachedFixedPointImplicitFromFloat != null)
            {
                object fixedPoint = cachedFixedPointImplicitFromFloat.Invoke(null, new object[] { (float)value });
                if (fixedPoint != null)
                    return (int)cachedFixedPointRawField.GetValue(fixedPoint);
            }

            return int.MinValue;
        }

        private static void EnsureFixedPointReflection()
        {
            if (fixedPointReflectionInitialized)
                return;

            cachedFixedPointType = FindTypeByFullName("Amplitude.Framework.FixedPoint")
                                ?? FindTypeByFullName("Amplitude.FixedPoint");

            if (cachedFixedPointType != null)
            {
                cachedFixedPointImplicitFromInt = cachedFixedPointType.GetMethod(
                    "op_Implicit",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(int) },
                    null);

                cachedFixedPointImplicitFromFloat = cachedFixedPointType.GetMethod(
                    "op_Implicit",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(float) },
                    null);

                cachedFixedPointRawField = cachedFixedPointType.GetField(
                    "RawValue",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            fixedPointReflectionInitialized = true;
        }

        private static void TrySetGlobalPropertyValue(int globalPropertyIndex, int rawValue)
        {
            EnsureGlobalRepositoryReflection();

            if (cachedGlobalPropertyRepositoryField == null || cachedSetGlobalPropertyValueMethod == null)
                return;

            object globalRepository = cachedGlobalPropertyRepositoryField.GetValue(null);
            if (globalRepository == null)
                return;

            cachedSetGlobalPropertyValueMethod.Invoke(globalRepository, new object[] { globalPropertyIndex, rawValue });
        }

        private static void EnsureGlobalRepositoryReflection()
        {
            if (globalRepositoryReflectionInitialized)
                return;

            cachedSimulationControllerType = FindTypeByFullName("Amplitude.Framework.Simulation.SimulationController");
            if (cachedSimulationControllerType != null)
            {
                cachedGlobalPropertyRepositoryField = cachedSimulationControllerType.GetField(
                    "GlobalPropertyRepository",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Type repositoryType = cachedGlobalPropertyRepositoryField != null
                    ? cachedGlobalPropertyRepositoryField.FieldType
                    : null;

                if (repositoryType != null)
                {
                    cachedSetGlobalPropertyValueMethod = repositoryType.GetMethod(
                        "SetGlobalPropertyValue",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(int), typeof(int) },
                        null);
                }
            }

            globalRepositoryReflectionInitialized = true;
        }

        private static object GetSandbox()
        {
            if (cachedSandbox != null)
                return cachedSandbox;

            if (sandboxStaticMember != null)
            {
                object sandbox = GetValue(sandboxStaticMember, null);
                if (sandbox != null)
                {
                    cachedSandbox = sandbox;
                    return sandbox;
                }
            }

            Type managerType = AccessTools.TypeByName(SandboxManagerTypeName);
            if (managerType != null)
            {
                sandboxStaticMember = FindStaticFieldOrProperty(managerType, "Sandbox");
                object sandbox = GetValue(sandboxStaticMember, null);
                if (sandbox != null)
                {
                    cachedSandbox = sandbox;
                    return sandbox;
                }
            }

            Type sandboxType = AccessTools.TypeByName(SandboxTypeName);
            if (sandboxType != null)
            {
                sandboxStaticMember = FindStaticFieldOrProperty(sandboxType, "Sandbox");
                object sandbox = GetValue(sandboxStaticMember, null);
                if (sandbox != null)
                {
                    cachedSandbox = sandbox;
                    return sandbox;
                }
            }

            return null;
        }

        internal static MemberInfo FindFieldOrPropertyInHierarchy(Type type, string name)
        {
            Type current = type;

            while (current != null)
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                FieldInfo field = current.GetField(name, flags);
                if (field != null)
                    return field;

                PropertyInfo property = current.GetProperty(name, flags);
                if (property != null)
                    return property;

                current = current.BaseType;
            }

            return null;
        }

        private static FieldInfo FindFieldInHierarchy(Type type, string name)
        {
            Type current = type;

            while (current != null)
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                FieldInfo field = current.GetField(name, flags);
                if (field != null)
                    return field;

                current = current.BaseType;
            }

            return null;
        }

        private static MemberInfo FindStaticFieldOrProperty(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name))
                return null;

            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo field = type.GetField(name, flags);
            if (field != null)
                return field;

            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null)
                return property;

            return null;
        }

        private static MemberInfo GetCachedHierarchyMember(
            Dictionary<Type, MemberInfo> cache,
            Type type,
            string memberName)
        {
            if (type == null)
                return null;

            if (!cache.TryGetValue(type, out MemberInfo member))
            {
                member = FindFieldOrPropertyInHierarchy(type, memberName);
                cache[type] = member;
            }

            return member;
        }

        private static object GetValue(MemberInfo member, object instance)
        {
            if (member == null)
                return null;

            if (member is PropertyInfo property)
                return property.GetValue(instance, null);

            if (member is FieldInfo field)
                return field.GetValue(instance);

            return null;
        }

        private static void SetFieldValueIfExists(object boxedStruct, string fieldName, object value)
        {
            if (boxedStruct == null || string.IsNullOrEmpty(fieldName))
                return;

            Type type = boxedStruct.GetType();
            string cacheKey = type.FullName + "|" + fieldName;

            if (!boxedFieldCache.TryGetValue(cacheKey, out FieldInfo field))
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                boxedFieldCache[cacheKey] = field;
            }

            if (field != null)
                field.SetValue(boxedStruct, value);
        }

        private static int GetIntFieldValueIfExists(object boxedStruct, string fieldName, int fallback)
        {
            if (boxedStruct == null || string.IsNullOrEmpty(fieldName))
                return fallback;

            Type type = boxedStruct.GetType();
            string cacheKey = type.FullName + "|" + fieldName;

            if (!boxedFieldCache.TryGetValue(cacheKey, out FieldInfo field))
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                boxedFieldCache[cacheKey] = field;
            }

            if (field == null)
                return fallback;

            object value = field.GetValue(boxedStruct);
            return value is int i ? i : fallback;
        }

        private static Type FindTypeByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            if (typeByFullNameCache.TryGetValue(fullName, out Type cachedType))
                return cachedType;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    typeByFullNameCache[fullName] = type;
                    return type;
                }
            }

            typeByFullNameCache[fullName] = null;
            return null;
        }
    }
}