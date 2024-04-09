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
            if (!metadata.Usings.Any(x => x.Equals("using System;", StringComparison.InvariantCultureIgnoreCase)))
            {
                WriteLine("using System;");
            }
            if (!metadata.Usings.Any(x => x.Equals("using System.ComponentModel;", StringComparison.InvariantCultureIgnoreCase)))
            {
                WriteLine("using System.ComponentModel;");
            }
            if (!metadata.Usings.Any(x => x.Equals("using System.Globalization;", StringComparison.InvariantCultureIgnoreCase)))
            {
                WriteLine("using System.Globalization;");
            }
            if (!metadata.Usings.Any(x => x.Equals("using System.Text.Json.Serialization;", StringComparison.InvariantCultureIgnoreCase)))
            {
                WriteLine("using System.Text.Json.Serialization;");
            }
            if (!metadata.Usings.Any(x => x.Equals("using System.Text.Json;", StringComparison.InvariantCultureIgnoreCase)))
            {
                WriteLine("using System.Text.Json;");
            }
            if (!metadata.Usings.Any(x => x.Equals("using System.Buffers;", StringComparison.InvariantCultureIgnoreCase)))
            {
                WriteLine("using System.Buffers;");
            }
            foreach (var @using in metadata.Usings)
            {
                Write(@using);
            }
            WriteLine();
            WriteLine("#nullable disable");
            WriteLine();

            if (!string.IsNullOrEmpty(metadata.Namespace))
            {
                WriteLine($"namespace {metadata.Namespace};");
            }
            WriteLine();
            WriteStronglyTyped();
        }

        private void WriteStronglyTyped()
        {
            WriteLine($"[TypeConverter(typeof({metadata.NameTyped}TypeConverter))]");
            WriteLine($"[System.Text.Json.Serialization.JsonConverter(typeof({metadata.NameTyped}JsonConverter))]");
            WriteBrace($"{metadata.Modifiers} record struct {metadata.NameTyped}({metadata.UidType} Value) : IStronglyTypedUid", () =>
            {
                WriteStatics();
                WriteToString();
            });
            WriteLine();
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
            WriteBrace($"public class {metadata.NameTyped}JsonConverter : JsonConverter<{metadata.NameTyped}>", () =>
            {
                WriteJsonRead();
                WriteJsonWrite();
            });

        }

        private void WriteCanConvertFrom()
        {
            WriteLine($"public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => ");
            WriteLine($"    sourceType == StringType || sourceType == UidType || base.CanConvertFrom(context, sourceType);");
            WriteLine();
            WriteLine($"public override object ConvertFrom(ITypeDescriptorContext context,");
            WriteNested(() =>
            {
                WriteLine($"CultureInfo culture, object value) => value switch");
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
            WriteLine($"public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>");
            WriteLine($"    destinationType == StringType || destinationType == UidType || base.CanConvertTo(context, destinationType);");
            WriteLine();
            WriteBrace($"public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)", () =>
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
                            WriteLine($"if (seq.Length != {metadata.UidBufferSize}) throw new JsonException(\"CustomerId invalid: length must be {metadata.UidBufferSize}\");");
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
                            WriteLine($"if (buf.Length != {metadata.UidBufferSize}) throw new JsonException(\"CustomerId invalid: length must be {metadata.UidBufferSize}\");");
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
                    WriteLine($"throw new JsonException(\"CustomerId invalid: length must be {metadata.UidBufferSize}\", e);");
                });
                WriteBrace($"catch (OverflowException e)", () =>
                {
                    WriteLine($"throw new JsonException(\"CustomerId invalid: invalid character\", e);");
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