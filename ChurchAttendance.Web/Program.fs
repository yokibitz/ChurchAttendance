open System.Text.Json
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Oxpecker
open ChurchAttendance.Web.Common
open ChurchAttendance.Web.Handlers
open ChurchAttendance.Core.Repository
open ChurchAttendance.Data
open System.IO
open System.Text.Json.Serialization


let endpoints = [
    GET [
        route "/" <| redirectTo "/attendees" true
        route "/test" test
    ]
    subRoute "/attendees" [
        GET [ route "" getAllAttendees ]
        POST [ route "/new" <| addAttendee ]
    ]
    subRoute "/api/attendance" [
        GET [ routef "/sheet/{%i}/{%i}/{%s}" getMonthlyAttendanceSheet ] 
        GET [ routef "/sheet/{%O}" getMonthlyAttendanceSheetById ] 
        PATCH [ routef "/sheet/{%O}" setAttendanceRecordInSheet ] 
    ]
    subRoute "/attendance" [
        GET [ routef "/report/{%i}/{%i}" getMonthlyAttendanceReport ] // New route for monthly attendance report       
        GET [ routef "/sheet/{%s}" getMonthlyAttendanceSheetById ] 
        GET [ routef "/sheet/{%i}/{%i}/{%s}" getMonthlyAttendanceSheetView ]
        POST [ routef "/sheet/{%i}/{%i}/{%s}" createMonthlyAttendanceSheet ]
    ]
    // POST [
    //     subRoute "/attendees" [
    //         route "/new" <| addAttendee
    //     ]
    // ]
]

let configureApp (appBuilder: IApplicationBuilder) =    
    appBuilder
        .UseRouting()
        .UseCors()
        .UseOxpecker(endpoints) |> ignore     

let configureServices (services: IServiceCollection) =
    services
        .AddCors(fun options -> options.AddDefaultPolicy(fun policy ->
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore))
        .AddRouting()
        .AddOxpecker() |> ignore
        
    let jsonSerializerOptions = JsonSerializerOptions(JsonSerializerDefaults.Web)
    jsonSerializerOptions.Converters.Add(new GroupConverter())
    JsonFSharpOptions.Default().AddToJsonSerializerOptions(jsonSerializerOptions)
    
    services.AddSingleton<IJsonSerializer>(SystemTextJsonSerializer(jsonSerializerOptions)) |> ignore
    
        

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    let dbPath = Path.Combine (__SOURCE_DIRECTORY__, "database", "ChurchAttendance.db")

    builder.Services.AddScoped<IRepository>(fun _ -> new Repository(dbPath) :> IRepository) |> ignore

    configureServices builder.Services
    let app = builder.Build()
    configureApp app
    //app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore

    app.Run()

    0 // Exit code

