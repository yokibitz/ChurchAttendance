module ChurchAttendance.Web.Common

open System
open System.Text.Json
open System.Text.Json.Serialization
open ChurchAttendance.Core.Models

type GroupConverter() =
    inherit JsonConverter<Group>()

    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        let value = reader.GetString()
        match reader.GetString() with
        | _ when String.Equals(value, "Infants", StringComparison.OrdinalIgnoreCase) -> Infants
        | _ when String.Equals(value, "Children", StringComparison.OrdinalIgnoreCase) -> Children
        | _ when String.Equals(value, "CYN", StringComparison.OrdinalIgnoreCase) -> CYN
        | _ when String.Equals(value, "YAN", StringComparison.OrdinalIgnoreCase) -> YAN
        | _ when String.Equals(value, "Men", StringComparison.OrdinalIgnoreCase) -> Men
        | _ when String.Equals(value, "Women", StringComparison.OrdinalIgnoreCase) -> Women
        | _ -> failwith "Unknown group"

    override this.Write(writer: Utf8JsonWriter, value: Group, options: JsonSerializerOptions) =
        let groupString =
            match value with
            | Infants -> "Infants"
            | Children -> "Children"
            | CYN -> "CYN"
            | YAN -> "YAN"
            | Men -> "Men"
            | Women -> "Women"
        writer.WriteStringValue(groupString)