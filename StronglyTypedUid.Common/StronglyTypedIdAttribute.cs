using System;

namespace StronglyTypedUid;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
public class StronglyTypedUidAttribute : Attribute
{
    public StronglyTypedUidAttribute() { }
    public StronglyTypedUidAttribute(bool asUlid = true) => AsUlid = asUlid;

    public bool AsUlid { get; }
}
