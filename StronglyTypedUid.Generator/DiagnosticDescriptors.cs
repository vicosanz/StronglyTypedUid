using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace StronglyTypedUid.Generator
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor StructNotPartial =
            new(
                "STI001",
                "Struct must be declared as 'readonly partial record struct'",
                "Struct {0} must be declared as 'readonly partial record struct'",
                DiagnosticCategories.StrongTypedId,
                DiagnosticSeverity.Warning,
                true
            );
    }
}
