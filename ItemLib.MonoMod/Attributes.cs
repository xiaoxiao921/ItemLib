using MonoMod;
using Mono.Cecil;
using System;

namespace RoR2
{
    [MonoModCustomMethodAttribute("NoInlining")]
    public class NoInlining : Attribute
    {
    }

    [MonoModCustomMethodAttribute("NoReadOnly")]
    public class NoReadOnly : Attribute
    {

    }
}

namespace MonoMod
{
    internal static class MonoModRules
    {
        // ReSharper disable once UnusedParameter.Global
        public static void NoInlining(MethodDefinition method, CustomAttribute attrib) => method.NoInlining = true;

        public static void NoReadOnly(FieldDefinition field, CustomAttribute attrib) => field.IsInitOnly = false;
    }
}