using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace EL2.SyntheticHarmony
{
    internal static class SyntheticApprovalLogic
    {
        private const string SandboxManagerTypeName = "Amplitude.Mercury.Sandbox.SandboxManager";
        private const string SandboxTypeName = "Amplitude.Mercury.Sandbox.Sandbox";

        internal const int TargetAIBaseSettlementApproval = 100;

        private static object cachedSandbox;
        private static MemberInfo sandboxTurnMember;
        private static bool initialized;

        internal static bool IsReady()
        {
            if (initialized)
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

            object value = GetValue(sandboxTurnMember, sandbox);
            return value is int i ? i : 0;
        }

        internal static bool IsAI(object empire)
        {
            if (empire == null)
                return false;

            MemberInfo member = FindFieldOrPropertyInHierarchy(empire.GetType(), "IsAIBrainActivated");
            if (member != null)
            {
                object value = GetValue(member, empire);
                if (value is bool isAiBrain)
                    return isAiBrain;
            }

            member = FindFieldOrPropertyInHierarchy(empire.GetType(), "IsControlledByHuman");
            if (member == null)
                return false;

            object humanValue = GetValue(member, empire);
            return humanValue is bool isHuman && !isHuman;
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

            MemberInfo member = FindFieldOrPropertyInHierarchy(empire.GetType(), "Index");
            if (member == null)
                return -1;

            object value = GetValue(member, empire);
            return value is int i ? i : -1;
        }

        internal static bool HasAnySettlements(object empire)
        {
            if (empire == null)
                return false;

            MemberInfo member = FindFieldOrPropertyInHierarchy(empire.GetType(), "Settlements");
            if (member == null)
                return false;

            object value = GetValue(member, empire);
            if (value == null)
                return false;

            PropertyInfo countProperty = value.GetType().GetProperty(
                "Count",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (countProperty != null)
            {
                object countValue = countProperty.GetValue(value, null);
                return countValue is int count && count > 0;
            }

            FieldInfo countField = value.GetType().GetField(
                "Count",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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

            MemberInfo member = FindFieldOrPropertyInHierarchy(empire.GetType(), "Approval");
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
                MethodInfo method = entity.GetType().GetMethod(
                    "GetPropertyValue",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string) },
                    null);

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

        private static bool ForceSimulationPropertyValue(object entity, string propertyName, int targetValue)
        {
            if (entity == null || string.IsNullOrEmpty(propertyName))
                return false;

            int rawValue = ConvertIntToFixedPointRaw(targetValue);
            if (rawValue == int.MinValue)
                return false;

            MethodInfo getPropertyIndexMethod = entity.GetType().GetMethod(
                "GetPropertyIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null);

            if (getPropertyIndexMethod == null)
                return false;

            object propertyIndexObject = getPropertyIndexMethod.Invoke(entity, new object[] { propertyName });
            if (!(propertyIndexObject is int propertyIndex) || propertyIndex < 0)
                return false;

            FieldInfo propertiesField = FindFieldInHierarchy(entity.GetType(), "Properties");
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

            MemberInfo namedPropertyMember = FindFieldOrPropertyInHierarchy(entity.GetType(), propertyName);
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
            Type fixedPointType = FindTypeByFullName("Amplitude.Framework.FixedPoint")
                               ?? FindTypeByFullName("Amplitude.FixedPoint");

            if (fixedPointType == null)
                return int.MinValue;

            MethodInfo implicitFromInt = fixedPointType.GetMethod(
                "op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(int) },
                null);

            if (implicitFromInt != null)
            {
                object fixedPoint = implicitFromInt.Invoke(null, new object[] { value });
                FieldInfo rawField = fixedPointType.GetField(
                    "RawValue",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (rawField != null)
                    return (int)rawField.GetValue(fixedPoint);
            }

            MethodInfo implicitFromFloat = fixedPointType.GetMethod(
                "op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(float) },
                null);

            if (implicitFromFloat != null)
            {
                object fixedPoint = implicitFromFloat.Invoke(null, new object[] { (float)value });
                FieldInfo rawField = fixedPointType.GetField(
                    "RawValue",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (rawField != null)
                    return (int)rawField.GetValue(fixedPoint);
            }

            return int.MinValue;
        }

        private static void TrySetGlobalPropertyValue(int globalPropertyIndex, int rawValue)
        {
            Type simulationControllerType = FindTypeByFullName("Amplitude.Framework.Simulation.SimulationController");
            if (simulationControllerType == null)
                return;

            FieldInfo globalRepositoryField = simulationControllerType.GetField(
                "GlobalPropertyRepository",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (globalRepositoryField == null)
                return;

            object globalRepository = globalRepositoryField.GetValue(null);
            if (globalRepository == null)
                return;

            MethodInfo setValueMethod = globalRepository.GetType().GetMethod(
                "SetGlobalPropertyValue",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(int) },
                null);

            if (setValueMethod == null)
                return;

            setValueMethod.Invoke(globalRepository, new object[] { globalPropertyIndex, rawValue });
        }

        private static object GetSandbox()
        {
            if (cachedSandbox != null)
                return cachedSandbox;

            Type managerType = AccessTools.TypeByName(SandboxManagerTypeName);
            if (managerType != null)
            {
                MemberInfo member = FindStaticFieldOrProperty(managerType, "Sandbox");
                object sandbox = GetValue(member, null);
                if (sandbox != null)
                {
                    cachedSandbox = sandbox;
                    return sandbox;
                }
            }

            Type sandboxType = AccessTools.TypeByName(SandboxTypeName);
            if (sandboxType != null)
            {
                MemberInfo member = FindStaticFieldOrProperty(sandboxType, "Sandbox");
                object sandbox = GetValue(member, null);
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
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            field?.SetValue(boxedStruct, value);
        }

        private static int GetIntFieldValueIfExists(object boxedStruct, string fieldName, int fallback)
        {
            if (boxedStruct == null || string.IsNullOrEmpty(fieldName))
                return fallback;

            Type type = boxedStruct.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return fallback;

            object value = field.GetValue(boxedStruct);
            return value is int i ? i : fallback;
        }

        private static Type FindTypeByFullName(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}