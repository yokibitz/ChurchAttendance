namespace ChurchAttendance.Web

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Oxpecker
open ChurchAttendance.Core
open ChurchAttendance.Core.Models
open ChurchAttendance.Core.Repository

module DsHandlers =
    open DsViews
    open System.IO
    open Microsoft.Extensions.Logging
    open Oxpecker.ViewEngine

    let getAllAttendees (ctx: HttpContext) =
        task {
            let repository = ctx.GetService<IRepository>()
            let result = AttendeeManager.getAllAttendees repository
            return! ctx.WriteJson result
        }
        :> Task

    let addAttendee (ctx: HttpContext) =
        task {
            let repository = ctx.GetService<IRepository>()
            let! newAttendee = ctx.BindJson<NewAttendee>()
            let result = AttendeeManager.addAttendee repository newAttendee
            return! ctx.Response.WriteAsJsonAsync(result)
        }
        :> Task

    let getMonthlyAttendanceReport month year (ctx: HttpContext) = 
        task {
            let repository = ctx.GetService<IRepository>()
            let result = AttendanceManager.getMonthlyAttendanceReportFromRepository repository month year
            return! ctx.WriteJson result
        }
        :> Task

    let getMonthlyAttendanceSheet month year (groupString:string) (ctx: HttpContext) = 
        task {
            let repository = ctx.GetService<IRepository>()
            let group = 
                parseGroup groupString
                |> Option.defaultValue Children                

            let result = AttendanceManager.getEnrichedMonthlyAttendanceSheetFromRepository repository month year group
            return! ctx.WriteJson result
        }
        :> Task

    let getMonthlyAttendanceSheetById (sheetIdString:string) (ctx: HttpContext) = 
        task {
            let repository = ctx.GetService<IRepository>()
            let sheetId = Guid.Parse(sheetIdString )
            let result = AttendanceManager.getMonthlyAttendanceSheetByIdFromRepository repository sheetId
            return! ctx.WriteJson result
        }
        :> Task

    let getMonthlyAttendanceSheetView month year (groupString:string) (ctx:HttpContext) = 
        task {
            let repository = ctx.GetService<IRepository>()
            let group = 
                parseGroup groupString
                |> Option.defaultValue Children            

            let result = 
                AttendanceManager.getEnrichedMonthlyAttendanceSheetFromRepository repository month year group

            let isHtmxRequest =                 
                ("HX-Request")
                |> ctx.TryGetHeaderValue
                |> Option.exists (fun value -> value = "true")

            match result with
            | Some sheet -> 
                let partialView = attendanceSheetView ctx sheet

                if isHtmxRequest then
                    return! ctx.WriteHtmlView partialView 
                else
                    let fullView = partialView |> mainView ctx
                    return! ctx.WriteHtmlView fullView 
            | None -> 
                ctx.Response.StatusCode <- StatusCodes.Status404NotFound
                return! ctx.Response.WriteAsync("Attendance sheet not found")
        }
        :> Task

    

    let createMonthlyAttendanceReport month year (ctx: HttpContext) =
        task {
            let repository = ctx.GetService<IRepository>()
            let result = AttendanceManager.createMonthlyAttendanceReportFromRepository repository month year

            match result with
            | Ok report -> return! ctx.WriteJson report
            | Error ex -> ()            
        }
        :> Task

    let createMonthlyAttendanceSheet month year (groupString:string) (ctx: HttpContext) = 
        task {
            let repository = ctx.GetService<IRepository>()
            let optionResultSheet = 
                parseGroup groupString
                |> Option.map (fun group -> AttendanceManager.createMonthlyAttendanceSheetFromRepository repository month year group)

            match optionResultSheet with
            | Some optionResult ->
                match optionResult with
                | Ok sheet -> return! ctx.WriteJson sheet
                | Error ex -> ()
            | None -> ()
        }
        :> Task

    let setAttendanceRecordInSheet (sheetIdString:string) (ctx: HttpContext) = 
        task {
            let logger = ctx.GetService<ILogger>()
            let repository = ctx.GetService<IRepository>()
            let sheetId = Guid.Parse(sheetIdString )
            
            // Log the request body
            ctx.Request.EnableBuffering()
            use reader = new StreamReader(ctx.Request.Body, leaveOpen = true)
            let! body = reader.ReadToEndAsync()
            ctx.Request.Body.Position <- 0L
            logger.LogInformation("Request Body: {body}", body)
            
            let! attendanceRecord = ctx.BindJson<NewAttendanceRecord>()
            let result = AttendanceManager.addAttendanceRecordToRepository repository sheetId attendanceRecord
            match result with
            | Ok sheet -> 
                let countOfPresent = sheet.Events
                                    |> List.find (fun e -> e.Date = attendanceRecord.Date)
                                    |> fun event -> event.Records |> List.filter (fun r -> r.Present = true)
                                    |> List.length
                return! ctx.WriteText <| $"Count: {string countOfPresent}"
            | Error _ -> ()

            // return! ctx.WriteJson result
        }
        :> Task

    let test (ctx: HttpContext) =
        task {
            let attendees = [
                { AttendeeId = Guid.Parse("0aab90cb-f977-4e63-9bba-c4d8b20a331c"); FirstName = "John"; LastName = "Doe"; Present = true }
                { AttendeeId = Guid.Parse("58508895-d221-4e9c-9624-7f60eb562aac"); FirstName = "Jane"; LastName = "Smith"; Present = false }
            ]

            let events = [
                { Date = DateTime(2024, 12, 8); Records = attendees }
                { Date = DateTime(2024, 12, 15); Records = attendees }
            ]

            let sheet = {
                Id = Guid.Parse("d20edfc3-af9b-4283-8b4e-e1e6b97eb737")
                Group = Children
                Month = 12
                Year = 2024
                Events = events
            }

            return! ctx.WriteJson sheet
        }
        :> Task