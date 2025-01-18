namespace ChurchAttendance.Data

open System
open LiteDB
open ChurchAttendance.Core.Repository
open ChurchAttendance.Core.Models
open LiteDB.FSharp
open Microsoft.Extensions.Logging


type Repository(databasePath: string) = 

    let loggerFactory = LoggerFactory.Create(fun builder -> builder.AddConsole() |> ignore)
    let logger = loggerFactory.CreateLogger("Repository")

    let mapper = FSharpBsonMapper()

    let db = new LiteDatabase(databasePath, mapper)

    interface IRepository with
        member _.getAllAttendees() =
            try
                logger.LogInformation("Fetching all attendees")
                let attendees = db.GetCollection<Attendee>("attendees").FindAll() |> Seq.toList
                Ok attendees
            with ex -> 
                logger.LogError(ex, "Error fetching attendees")
                Error ex.Message
            

        member _.addAttendee (newAttendee: NewAttendee) : Result<unit, string> =
            try
                logger.LogInformation("Adding a new attendee")
                let attendee = { 
                    Id = Guid.NewGuid()
                    FirstName = newAttendee.FirstName
                    LastName = newAttendee.LastName
                    DateOfBirth = newAttendee.DateOfBirth
                    Group = newAttendee.Group
                }
                let collection = db.GetCollection<Attendee>("attendees")
                collection.Insert(attendee) |> ignore
                Ok ()
            with ex ->                 
                logger.LogError(ex, "Error adding attendee")
                Error ex.Message

        member _.getMonthlyAttendanceReport month year =
            try
                db.GetCollection<MonthlyAttendanceReport>("attendanceReports")
                    .FindOne(fun r -> r.Month = month && r.Year = year)
                    |> Option.ofObj
                    |> Ok
                
            with ex -> Error ex.Message

        member _.getMonthlyAttendanceSheet month year group =
            try
                logger.LogInformation("Getting attendance sheet")
                db.GetCollection<MonthlyAttendanceSheet>("attendanceSheets")
                    .Find(fun r -> r.Month = month && r.Year = year)
                    |> Seq.toList
                    |> List.tryFind (fun s -> s.Group = group)
                    |> Ok
                
            with ex ->                 
                logger.LogError("Failed getting attendance sheet: {ex.Message}", ex.Message)
                Error ex.Message

        member _.getMonthlyAttendanceSheetById sheetId =
            try
                logger.LogInformation("Getting attendance sheet for Id: {sheetId}", sheetId)
                db.GetCollection<MonthlyAttendanceSheet>("attendanceSheets")
                    .FindById(sheetId)
                    |> Option.ofObj
                    |> Ok
                
            with ex ->                 
                logger.LogError("Failed getting attendance sheet: {ex.Message}", ex.Message)
                Error ex.Message

        member _.saveMonthlyAttendanceReport(report: MonthlyAttendanceReport) : Result<unit, string> =
            try
                logger.LogInformation("Saving attendance")
                db.GetCollection<MonthlyAttendanceReport>("attendanceReports").Upsert(report) |> ignore
                Ok ()
            with ex -> 
                logger.LogError(ex, "Failed to save attendance")
                Error ex.Message

        member _.saveMonthlyAttendanceSheet (monthlyAttendanceSheet: MonthlyAttendanceSheet) = 
            try
                logger.LogInformation("Saving attendance sheet")
                db.GetCollection<MonthlyAttendanceSheet>("attendanceSheets").Upsert(monthlyAttendanceSheet) |> ignore
                Ok monthlyAttendanceSheet
            with ex ->
                logger.LogError(ex, "Failed to save attendance sheet")
                Error ex.Message

    interface System.IDisposable with
        member _.Dispose() = db.Dispose()