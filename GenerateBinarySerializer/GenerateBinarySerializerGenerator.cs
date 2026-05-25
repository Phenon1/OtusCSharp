using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GenerateBinarySerializer
{
    [Generator]
    public class GenerateBinarySerializerGenerator : IIncrementalGenerator
    {
        
        private const string Id = "DSG001";

        private static readonly DiagnosticDescriptor UnsupportedTypeRule = new DiagnosticDescriptor(
            id: Id,
            title: "Unsupported type",
            messageFormat: "The type '{0}' is not supported for serialization",
            category: "GenerateSerializer",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
       
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = GenerateBinarySerializerSyntaxProvider.GetSyntaxProvider(context);

            context.RegisterSourceOutput(classDeclarations,
             (spc, source) => Execute(spc, source));
        }

      

        private void Execute(SourceProductionContext context, INamedTypeSymbol classSymbol)
        {
            var serializableType = BuildSerializableType(classSymbol, context);

            if (serializableType != null)
            {
                var source = GenerateSerializerClass(serializableType);

                context.AddSource(
                    serializableType.TypeName + ".Serializer.g.cs",
                    SourceText.From(source, Encoding.UTF8));
            }
        }


        private static SerializableType BuildSerializableType(INamedTypeSymbol classSymbol, SourceProductionContext context)
        {
            var props = new List<SerializableProperty>();

            foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (member.DeclaredAccessibility != Accessibility.Public) continue;
                if (member.GetMethod == null) continue;

                var type = member.Type;
                if (!IsSupportedType(type))
                {
                    ReportUnsupportedProperty(context, member, classSymbol);
                    continue;
                }

                var canonicalTypeName = GetCanonicalTypeName(type);
                props.Add(new SerializableProperty(member.Name, canonicalTypeName));
            }

            var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
                     ? string.Empty
                     : classSymbol.ContainingNamespace.ToDisplayString();

            return new SerializableType(ns, classSymbol.Name, props);
        }

        private static bool IsSupportedType(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Double:
                case SpecialType.System_Boolean:
                case SpecialType.System_String:
                case SpecialType.System_DateTime:
                    return true;
                default:
                    return false;
            }
        }

        private static string GetCanonicalTypeName(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Int32:
                    return "int";
                case SpecialType.System_Int64:
                    return "long";
                case SpecialType.System_Double:
                    return "double";
                case SpecialType.System_Boolean:
                    return "bool";
                case SpecialType.System_String:
                    return "string";
                case SpecialType.System_DateTime:
                    return "dateTime";
                default:
                    return type.ToDisplayString();
            }
        }

        private static void ReportUnsupportedProperty(
            SourceProductionContext context,
            IPropertySymbol property,
            INamedTypeSymbol classSymbol)
        {
            var location = property.Locations.FirstOrDefault();

            var diagnostic = Diagnostic.Create(
                UnsupportedTypeRule,
                location,
                property.Name,
                classSymbol.Name,
                property.Type.ToDisplayString());

            context.ReportDiagnostic(diagnostic);
        }

        private static string GenerateSerializerClass(SerializableType type)
        {

            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                sb.Append("namespace ").Append(type.Namespace).AppendLine(";");
                sb.AppendLine();
            }

            sb.Append("public partial class ").Append(type.TypeName).AppendLine();
            sb.AppendLine("{");
            sb.AppendLine("    public void SerializeToBinary(Stream stream)");
            sb.AppendLine("    {");
            sb.AppendLine("        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))");
            sb.AppendLine("        {");

            foreach (var prop in type.Properties)
            {
                AppendWriteForProperty(sb, prop);
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    public byte[] SerializeToBinary()");
            sb.AppendLine("    {");
            sb.AppendLine("        using var stream = new MemoryStream();");
            sb.AppendLine("        SerializeToBinary(stream);");
            sb.AppendLine("        return stream.ToArray();");
            sb.AppendLine("    }");
            sb.AppendLine();

            // --- МЕТОД ДЕСЕРИАЛИЗАЦИИ ---
            sb.Append("    public static ").Append(type.TypeName).AppendLine(" DeserializeFromBinary(ReadOnlySpan<byte> span)");
            sb.AppendLine("    {");
            sb.AppendLine("        using var stream = new MemoryStream(span.ToArray(), writable: false);");
            sb.AppendLine("        return DeserializeFromBinary(stream);");
            sb.AppendLine("    }");
            sb.AppendLine();


            sb.Append("    public static ").Append(type.TypeName).AppendLine(" DeserializeFromBinary(Stream stream)");
            sb.AppendLine("    {");
            sb.Append("        var instance = (").Append(type.TypeName).Append(")System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(").Append(type.TypeName).AppendLine("));");
            sb.AppendLine();
            sb.AppendLine("        using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))");
            sb.AppendLine("        {");

            foreach (var prop in type.Properties)
            {
                AppendReadForProperty(sb, prop);
            }

            sb.AppendLine("        }");
            sb.AppendLine("        return instance;");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void AppendWriteForProperty(StringBuilder sb, SerializableProperty prop)
        {
            // внутри using-блока → 12 пробелов для отступа
            switch (prop.TypeName)
            {
                case "int":
                    sb.Append("            writer.Write(this.")
                      .Append(prop.Name)
                      .AppendLine(");");
                    break;

                case "long":
                    sb.Append("            writer.Write(this.")
                      .Append(prop.Name)
                      .AppendLine(");");
                    break;

                case "double":
                    sb.Append("            writer.Write(this.")
                      .Append(prop.Name)
                      .AppendLine(");");
                    break;

                case "bool":
                    sb.Append("            writer.Write(this.")
                      .Append(prop.Name)
                      .AppendLine(");");
                    break;

                case "string":
                    sb.AppendLine("            if (this." + prop.Name + " == null)");
                    sb.AppendLine("            {");
                    sb.AppendLine("                writer.Write(-1);");
                    sb.AppendLine("            }");
                    sb.AppendLine("            else");
                    sb.AppendLine("            {");
                    sb.AppendLine("                var bytes = System.Text.Encoding.UTF8.GetBytes(this." + prop.Name + ");");
                    sb.AppendLine("                writer.Write(bytes.Length);");
                    sb.AppendLine("                writer.Write(bytes);");
                    sb.AppendLine("            }");
                    break;

                case "dateTime":
                    sb.Append("            writer.Write(this.")
                      .Append(prop.Name)
                      .Append(".Ticks);")
                      .AppendLine();
                    break;

                default:
                    sb.Append("            // Unsupported type: ")
                      .Append(prop.TypeName)
                      .AppendLine();
                    break;
            }
        }

        private static void AppendReadForProperty(StringBuilder sb, SerializableProperty prop)
        {
            switch (prop.TypeName)
            {
                case "int":
                    sb.Append("            instance.").Append(prop.Name).AppendLine(" = reader.ReadInt32();");
                    break;

                case "long":
                    sb.Append("            instance.").Append(prop.Name).AppendLine(" = reader.ReadInt64();");
                    break;

                case "double":
                    sb.Append("            instance.").Append(prop.Name).AppendLine(" = reader.ReadDouble();");
                    break;

                case "bool":
                    sb.Append("            instance.").Append(prop.Name).AppendLine(" = reader.ReadBoolean();");
                    break;

                case "string":
                    sb.Append("            int __len_").Append(prop.Name).AppendLine(" = reader.ReadInt32();");
                    sb.Append("            if (__len_").Append(prop.Name).AppendLine(" == -1)");
                    sb.AppendLine("            {");
                    sb.Append("                instance.").Append(prop.Name).AppendLine(" = null;");
                    sb.AppendLine("            }");
                    sb.AppendLine("            else");
                    sb.AppendLine("            {");
                    sb.Append("                byte[] __bytes_").Append(prop.Name).Append(" = reader.ReadBytes(__len_").Append(prop.Name).AppendLine(");");
                    sb.Append("                instance.").Append(prop.Name).Append(" = System.Text.Encoding.UTF8.GetString(__bytes_").Append(prop.Name).AppendLine(");");
                    sb.AppendLine("            }");
                    break;

                case "dateTime":
                    // Опечатка исправлена: теперь имя переменной уникально и совпадает при объявлении и чтении
                    sb.Append("            long __ticks_").Append(prop.Name).AppendLine(" = reader.ReadInt64();");
                    sb.Append("            instance.").Append(prop.Name).Append(" = new DateTime(__ticks_").Append(prop.Name).AppendLine(");");
                    break;

                default:
                    sb.Append("            // Unsupported type for reading: ").AppendLine(prop.TypeName);
                    break;
            }
        }


    }
}
