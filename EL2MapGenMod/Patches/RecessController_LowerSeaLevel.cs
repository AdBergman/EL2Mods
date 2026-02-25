﻿using System;
using System.Reflection;
using HarmonyLib;
using EL2MapGenMod.Tuning;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch]
    internal static class RecessController_LowerSeaLevel
    {
        private static Type _recessControllerType;
        private static MethodBase _lowerSeaLevelMethod;

        private static FieldInfo _recessCountFromStartField;
        private static FieldInfo _currentSeaLevelField;

        private static PropertyInfo _recessCountFromStartProp;
        private static PropertyInfo _currentSeaLevelProp;

        private static MethodBase TargetMethod()
        {
            // Resolve type once
            if (_recessControllerType == null)
            {
                // If namespace differs in your build, adjust the string to match the decompiled full name.
                _recessControllerType = AccessTools.TypeByName("Amplitude.Mercury.Simulation.RecessController");
            }

            if (_recessControllerType == null)
                return null;

            // Resolve method once
            if (_lowerSeaLevelMethod == null)
            {
                _lowerSeaLevelMethod = AccessTools.Method(_recessControllerType, "LowerSeaLevel", new Type[] { typeof(int) });
            }

            return _lowerSeaLevelMethod;
        }

        private static void Postfix(object __instance)
        {
            if (__instance == null)
                return;

            EnsureMemberCache(__instance.GetType());

            int recessCount = GetIntMember(__instance, _recessCountFromStartField, _recessCountFromStartProp, 0);
            int currentSeaLevel = GetIntMember(__instance, _currentSeaLevelField, _currentSeaLevelProp, 0);

            if (recessCount >= WorldGenTuningProfile.PersistentWaterClampFromRecessIndex &&
                currentSeaLevel < WorldGenTuningProfile.PersistentWaterMinSeaLevel)
            {
                SetIntMember(__instance, _currentSeaLevelField, _currentSeaLevelProp, WorldGenTuningProfile.PersistentWaterMinSeaLevel);
            }
        }

        private static void EnsureMemberCache(Type instanceType)
        {
            // Cache field/property lookups once (works whether they are fields OR properties)
            if (_recessCountFromStartField == null && _recessCountFromStartProp == null)
            {
                _recessCountFromStartField = AccessTools.Field(instanceType, "RecessCountFromStart");
                if (_recessCountFromStartField == null)
                    _recessCountFromStartProp = AccessTools.Property(instanceType, "RecessCountFromStart");
            }

            if (_currentSeaLevelField == null && _currentSeaLevelProp == null)
            {
                _currentSeaLevelField = AccessTools.Field(instanceType, "CurrentSeaLevel");
                if (_currentSeaLevelField == null)
                    _currentSeaLevelProp = AccessTools.Property(instanceType, "CurrentSeaLevel");
            }
        }

        private static int GetIntMember(object instance, FieldInfo field, PropertyInfo prop, int fallback)
        {
            try
            {
                if (field != null)
                {
                    object v = field.GetValue(instance);
                    if (v is int) return (int)v;
                    if (v is byte) return (byte)v;
                    if (v is sbyte) return (sbyte)v;
                }

                if (prop != null)
                {
                    object v = prop.GetValue(instance, null);
                    if (v is int) return (int)v;
                    if (v is byte) return (byte)v;
                    if (v is sbyte) return (sbyte)v;
                }
            }
            catch
            {
                // swallow; fallback below
            }

            return fallback;
        }

        private static void SetIntMember(object instance, FieldInfo field, PropertyInfo prop, int value)
        {
            try
            {
                if (field != null)
                {
                    // If the field is an int, set int; if byte/sbyte, clamp.
                    if (field.FieldType == typeof(int))
                        field.SetValue(instance, value);
                    else if (field.FieldType == typeof(byte))
                        field.SetValue(instance, ClampUtil.ClampByte(value));
                    else if (field.FieldType == typeof(sbyte))
                        field.SetValue(instance, ClampUtil.ClampSByte(value));
                    else
                        field.SetValue(instance, value);

                    return;
                }

                if (prop != null && prop.CanWrite)
                {
                    if (prop.PropertyType == typeof(int))
                        prop.SetValue(instance, value, null);
                    else if (prop.PropertyType == typeof(byte))
                        prop.SetValue(instance, ClampUtil.ClampByte(value), null);
                    else if (prop.PropertyType == typeof(sbyte))
                        prop.SetValue(instance, ClampUtil.ClampSByte(value), null);
                    else
                        prop.SetValue(instance, value, null);
                }
            }
            catch
            {
                // swallow; nothing else to do safely
            }
        }
    }
}