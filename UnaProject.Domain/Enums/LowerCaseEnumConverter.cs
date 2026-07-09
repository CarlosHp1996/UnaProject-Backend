using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnaProject.Domain.Enums
{
    /// <summary>
    /// Converte enums para/de JSON usando o nome do membro em minúsculas
    /// (ex.: EnumOrderStatus.Pending -> "pending"). Desserialização é case-insensitive.
    /// Aplicado por-enum via [JsonConverter], NÃO altera o wire-format dos demais enums
    /// (EnumCategory/EnumColor/EnumState/EnumGender) que seguem usando o conversor global PascalCase.
    /// </summary>
    public class LowerCaseEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
                return result;

            throw new JsonException($"Valor '{value}' inválido para o enum {typeof(TEnum).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString().ToLowerInvariant());
        }
    }
}
