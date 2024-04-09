namespace StronglyTypedUid.Generator
{
    public class StronglyTypedUidWriter(Metadata metadata) : AbstractWriter
    {
        public string GetCode()
        {
            WriteFile();
            return GeneratedText();
        }

        private void WriteFile()
        {
            AddUsings("System");
            AddUsings("System.ComponentModel");
            AddUsings("System.Globalization");
            AddUsings("System.Text.Json.Serialization");
            AddUsings("System.Text.Json");
            AddUsings("System.Buffers");
            if (metadata.AdditionalConverters.Any(x => x == 0))
            {
                AddUsings("Microsoft.EntityFrameworkCore.Storage.ValueConversion");
            }
            foreach (var @using in metadata.Usings)
            {
                Write(@using);
            }
            WriteLine();
            WriteLine("#nullable enable");
            WriteLine();

            if (!string.IsNullOrEmpty(metadata.Namespace))
            {
                WriteLine($"namespace {metadata.Namespace};");
            }
            WriteLine();
            WriteStronglyTyped();
        }

        private void AddUsings(string assembly)
        {
            var assemblyUsing = $"using {assembly};";
            if (!metadata.Usings.Any(x => x.StartsWith(assemblyUsing, StringComparison.InvariantCultureIgnoreCase)))
            {
                WriteLine(assemblyUsing);
            }
        }

        private void WriteStronglyTyped()
        {
            WriteMainRecordStruct();
            WriteTypeConverter();
            WriteJsonConverter();
            if (metadata.AdditionalConverters.Any(x => x == 0)) //efcore
            {
                WriteEfcore();
            }
            if (metadata.AdditionalConverters.Any(x => x == 1)) //Dapper
            {
                WriteDapper();
            }
            if (metadata.AdditionalConverters.Any(x => x == 2)) //NewtonSoftJson
            {
                WriteNewtonSoftJson();
            }
        }

        private void WriteNewtonSoftJson()
        {
            WriteBrace($"public class {metadata.NameTyped}NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter", () =>
            {
                WriteLine($"public override bool CanConvert(System.Type type) => type == typeof({metadata.UidType});");
                WriteLine();
                WriteLine($"public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) =>");
                WriteLine($"    serializer.Serialize(writer, value is {metadata.NameTyped} id ? id.Value.ToString() : null);");
                WriteLine();

                WriteBrace($"public override object? ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)", () =>
                {
                    if (metadata.AsUlid)
                    {
                        WriteLine($"var text = serializer.Deserialize<string?>(reader);");
                        WriteLine($"return text != null ? new {metadata.NameTyped}(Ulid.Parse(text)) : null;");
                    }
                    else
                    {
                        WriteLine($"var uid = serializer.Deserialize<System.Guid?>(reader);");
                        WriteLine($"return uid.HasValue ? new {metadata.NameTyped}(uid.Value) : null;");
                    }
                });
            });
            WriteLine();
        }

        private void WriteDapper()
        {
            WriteBrace($"public partial class {metadata.NameTyped}DapperTypeHandler : Dapper.SqlMapper.TypeHandler<{metadata.NameTyped}>", () =>
            {
                WriteBrace($"public override void SetValue(System.Data.IDbDataParameter parameter, {metadata.NameTyped} value)", () =>
                {
                    WriteLine($"parameter.Value = value.Value;");
                });
                WriteLine();
                WriteLine($"public override {metadata.NameTyped} Parse(object value) => value switch");
                WriteNested("{", "};", () =>
                {
                    if (!metadata.AsUlid)
                    {
                        WriteLine($"System.Guid uid => new {metadata.NameTyped}(uid),");
                    }
                    WriteLine($"string text when !string.IsNullOrEmpty(text) && {metadata.UidType}.TryParse(text, out var result) => new {metadata.NameTyped}(result),");
                    WriteLine($"_ => throw new InvalidCastException($\"Unable to cast object of type {{value.GetType()}} to {metadata.NameTyped}\"),");
                });
            });
        }

        private void WriteEfcore()
        {
            WriteBrace($"public partial class {metadata.NameTyped}StringValueConverter : ValueConverter<{metadata.NameTyped}, string>", () =>
            {
                WriteLine($"public {metadata.NameTyped}StringValueConverter() : this(null) {{ }}");
                WriteLine();
                WriteNested($"public {metadata.NameTyped}StringValueConverter(ConverterMappingHints? mappingHints = null)", "{ }", () =>
                {
                    WriteNested($": base(", ")", () =>
                    {
                        WriteLine($"id => id.ToString(),");
                        WriteLine($"value => new {metadata.NameTyped}({metadata.UidType}.Parse(value)),");
                        WriteLine($"mappingHints");
                    });
                });
            });
            WriteLine();

            if (metadata.AsUlid)
            {
                WriteBrace($"public partial class {metadata.NameTyped}ByteArrayValueConverter : ValueConverter<{metadata.NameTyped}, byte[]>", () =>
                {
                    WriteLine($"private static readonly ConverterMappingHints defaultHints = new ConverterMappingHints(size: 16);");
                    WriteLine();
                    WriteLine($"public {metadata.NameTyped}ByteArrayValueConverter() : this(null) {{ }}");
                    WriteLine();
                    WriteNested($"public {metadata.NameTyped}ByteArrayValueConverter(ConverterMappingHints? mappingHints = null)", "{ }", () =>
                    {
                        WriteNested($": base(", ")", () =>
                        {
                            WriteLine($"id => id.Value.ToByteArray(),");
                            WriteLine($"value => new {metadata.NameTyped}(new Ulid(value)),");
                            WriteLine($"defaultHints.With(mappingHints)");
                        });
                    });
                });
            }
            else
            {
                WriteBrace($"public partial class {metadata.NameTyped}GuidValueConverter : ValueConverter<{metadata.NameTyped}, Guid>", () =>
                {
                    WriteLine($"public {metadata.NameTyped}GuidValueConverter() : this(null) {{ }}");
                    WriteLine();
                    WriteNested($"public {metadata.NameTyped}GuidValueConverter(ConverterMappingHints? mappingHints = null)", "{ }", () =>
                    {
                        WriteNested($": base(", ")", () =>
                        {
                            WriteLine($"id => id.Value,");
                            WriteLine($"value => new {metadata.NameTyped}(value),");
                            WriteLine($"mappingHints");
                        });
                    });
                });
            }
        }

        private void WriteJsonConverter()
        {
            WriteBrace($"public class {metadata.NameTyped}JsonConverter : JsonConverter<{metadata.NameTyped}>", () =>
            {
                WriteJsonRead();
                WriteJsonWrite();
            });
            WriteLine();
        }

        private void WriteTypeConverter()
        {
            WriteBrace($"public class {metadata.NameTyped}TypeConverter : TypeConverter", () =>
            {
                WriteLine($"private static readonly Type StringType = typeof(string);");
                WriteLine($"private static readonly Type UidType = typeof({metadata.UidType});");
                WriteLine();
                WriteCanConvertFrom();
                WriteLine();
                WriteCanConvertTo();
            });
            WriteLine();
        }

        private void WriteMainRecordStruct()
        {
            WriteLine($"[TypeConverter(typeof({metadata.NameTyped}TypeConverter))]");
            WriteLine($"[System.Text.Json.Serialization.JsonConverter(typeof({metadata.NameTyped}JsonConverter))]");
            WriteBrace($"{metadata.Modifiers} record struct {metadata.NameTyped}({metadata.UidType} Value) : IStronglyTypedUid", () =>
            {
                WriteStatics();
                WriteToString();
            });
            WriteLine();
        }

        private void WriteCanConvertFrom()
        {
            WriteLine($"public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => ");
            WriteLine($"    sourceType == StringType || sourceType == UidType || base.CanConvertFrom(context, sourceType);");
            WriteLine();
            WriteLine($"public override object? ConvertFrom(ITypeDescriptorContext? context,");
            WriteNested(() =>
            {
                WriteLine($"CultureInfo? culture, object value) => value switch");
                WriteNested("{", "};", () =>
                {
                    WriteLine($"{metadata.UidType} g => new {metadata.NameTyped}(g),");
                    WriteLine($"string stringValue => {metadata.NameTyped}.Parse(stringValue),");
                    WriteLine($"_ => base.ConvertFrom(context, culture, value),");
                });
            });
        }

        private void WriteCanConvertTo()
        {
            WriteLine($"public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>");
            WriteLine($"    destinationType == StringType || destinationType == UidType || base.CanConvertTo(context, destinationType);");
            WriteLine();
            WriteBrace($"public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)", () =>
            {
                WriteBrace($"if (value is {metadata.NameTyped} result)", () =>
                {
                    WriteBrace($"if (destinationType == StringType)", () =>
                    {
                        WriteLine($"return result.ToString();");
                    });
                    WriteBrace($"if (destinationType == UidType)", () =>
                    {
                        WriteLine($"return ({metadata.UidType})result;");
                    });
                });
                WriteLine($"return base.ConvertTo(context, culture, value, destinationType);");
            });
        }

        private void WriteJsonRead()
        {
            WriteBrace($"public override {metadata.NameTyped} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)", () =>
            {
                WriteBrace($"try", () =>
                {
                    WriteLine($"if (reader.TokenType != JsonTokenType.String) throw new JsonException(\"Expected string\");");
                    if (metadata.AsUlid)
                    { 
                        WriteBrace($"if (reader.HasValueSequence)", () =>
                        {
                            WriteLine($"var seq = reader.ValueSequence;");
                            WriteLine($"if (seq.Length != {metadata.UidBufferSize}) throw new JsonException(\"{metadata.NameTyped} invalid: length must be {metadata.UidBufferSize}\");");
                            WriteLine($"Span<byte> buf = stackalloc byte[{metadata.UidBufferSize}];");
                            WriteLine($"seq.CopyTo(buf);");
                            if (metadata.AsUlid)
                            {
                                WriteLine($"Ulid.TryParse(buf, out var uid);");
                            }
                            else
                            {
                                WriteLine($"Guid uid = new Guid(buf);");
                            }
                            WriteLine($"return new {metadata.NameTyped}(uid);");
                        });
                        WriteBrace($"else", () =>
                        {
                            WriteLine($"var buf = reader.ValueSpan;");
                            WriteLine($"if (buf.Length != {metadata.UidBufferSize}) throw new JsonException(\"{metadata.NameTyped} invalid: length must be {metadata.UidBufferSize}\");");
                            if (metadata.AsUlid)
                            {
                                WriteLine($"Ulid.TryParse(buf, out var uid);");
                            }
                            else
                            {
                                WriteLine($"Guid uid = new Guid(buf);");
                            }
                            WriteLine($"return new {metadata.NameTyped}(uid);");
                        });
                    }
                    else
                    {
                        WriteLine($"return new {metadata.NameTyped}(new Guid(reader.GetString()));");
                    }
                });
                WriteBrace($"catch (IndexOutOfRangeException e)", () =>
                {
                    WriteLine($"throw new JsonException(\"{metadata.NameTyped} invalid: length must be {metadata.UidBufferSize}\", e);");
                });
                WriteBrace($"catch (OverflowException e)", () =>
                {
                    WriteLine($"throw new JsonException(\"{metadata.NameTyped} invalid: invalid character\", e);");
                });
            });
        }

        private void WriteJsonWrite()
        {
            WriteBrace($"public override void Write(Utf8JsonWriter writer, {metadata.NameTyped} value, JsonSerializerOptions options)", () =>
            {
                if (metadata.AsUlid)
                {
                    WriteLine($"Span<byte> buf = stackalloc byte[{metadata.UidBufferSize}];");
                    WriteLine($"value.Value.TryWriteStringify(buf);");
                    WriteLine($"writer.WriteStringValue(buf);");
                }
                else
                {
                    WriteLine($"writer.WriteStringValue(value.ToString());");
                }
            });
        }

        private void WriteStatics()
        {
            WriteLine($"public static {metadata.NameTyped} Empty => new({metadata.UidType}.Empty);");
            WriteLine();
            WriteLine($"public static {metadata.NameTyped} New{metadata.NameTyped}() => new({metadata.UidType}.New{metadata.UidType}());");
            WriteLine();
            WriteLine($"public static implicit operator {metadata.NameTyped}({metadata.UidType} value) => new(value);");
            WriteLine();
            WriteLine($"public static explicit operator {metadata.UidType}({metadata.NameTyped} value) => value.Value;");
            WriteLine();
            WriteLine($"public bool IsEmpty => Value == {metadata.UidType}.Empty;");
            WriteLine();
        }

        private void WriteToString()
        {
            WriteLine($"public override string ToString() => Value.ToString();");
            WriteLine();
            WriteLine($"public static {metadata.NameTyped} Parse(string text) => new {metadata.NameTyped}({metadata.UidType}.Parse(text));");
            WriteLine();
            WriteBrace($"public static bool TryParse(string text, out {metadata.NameTyped} result)", () =>
            {
                WriteBrace("try", () =>
                {
                    WriteBrace($"if ({metadata.UidType}.TryParse(text, out {metadata.UidType} uid))", () =>
                    {
                        WriteLine("result = uid;");
                        WriteLine("return true;");
                    });
                });
                WriteBrace("catch (Exception)", () =>
                {
                });
                WriteLine("result = default;");
                WriteLine("return false;");
            });
        }
    }
}