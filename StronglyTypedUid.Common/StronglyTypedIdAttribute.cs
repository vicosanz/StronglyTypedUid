using System;

namespace StronglyTypedUid;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
public class StronglyTypedUidAttribute : Attribute
{
    public StronglyTypedUidAttribute() { }
    public StronglyTypedUidAttribute(bool asUlid = true, EnumAdditionalConverters[] converters = null)
    {
        AsUlid = asUlid;
        Converters = converters ?? [];
    }

    public bool AsUlid { get; }
    public EnumAdditionalConverters[] Converters { get; }
}
