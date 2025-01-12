namespace ChurchAttendance.Web

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Oxpecker
open ChurchAttendance.Core
open ChurchAttendance.Core.Models
open ChurchAttendance.Core.Repository

module Handlers =
    open Views
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

            match result with
            | Some sheet -> 
                let view = 
                    attendanceSheetView ctx sheet
                    |> mainView ctx

                return! ctx.WriteHtmlView view
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
            | Ok _ -> ()
            | Error _ -> ()

            // return! ctx.WriteJson result
        }
        :> Task