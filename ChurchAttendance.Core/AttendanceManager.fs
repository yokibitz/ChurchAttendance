namespace ChurchAttendance.Core

open System
open Models
open Repository

module AttendanceManager = 

    let private getDaysInMonth (dayOfWeek: DayOfWeek) (month: int) (year: int) : DateTime list =
        let daysInMonth = DateTime.DaysInMonth(year, month)
        [1 .. daysInMonth]
        |> List.map (fun day -> DateTime(year, month, day))
        |> List.filter (fun date -> date.DayOfWeek = dayOfWeek)

    let getAllFridays (month: int) (year: int) : DateTime list =
        getDaysInMonth DayOfWeek.Friday month year

    let getAllSundays (month: int) (year: int) : DateTime list =
        getDaysInMonth DayOfWeek.Sunday month year

    let createMonthlyAttendanceReport (attendees: Attendee list) (month: int) (year: int) : MonthlyAttendanceReport =
        let fridays = getAllFridays month year
        let sundays = getAllSundays month year
        let groupedAttendees = attendees |> List.groupBy (fun attendee -> attendee.Group)
        let events = List.append fridays sundays |> List.map (fun date -> { Date = date; Records = [] })
        let sheets : MonthlyAttendanceSheet list = 
            groupedAttendees
            |> List.map (fun (group, attendees) ->
                {
                    Id = Guid.NewGuid()
                    Group = group
                    Month = month
                    Year = year
                    Events = 
                        events 
                        |> List.map (fun event ->
                            { 
                                Date = event.Date
                                Records = attendees 
                                            |> List.map (fun attendee -> { AttendeeId = attendee.Id; Present = false }) 
                            })
                })
        {
            Id = Guid.NewGuid()
            Month = month
            Year = year
            Sheets = sheets
        }

    let getMonthlyAttendanceReportFromRepository (repository:IRepository) month year = 
        match repository.getMonthlyAttendanceReport month year with
        | Ok report -> report
        | Error msg -> None

    let getMonthlyAttendanceSheetFromRepository (repository:IRepository) month year group = 
        match repository.getMonthlyAttendanceSheet month year group with
        | Ok sheet -> sheet
        | Error msg -> None

    let getMonthlyAttendanceSheetByIdFromRepository (repository:IRepository) sheetId = 
        match repository.getMonthlyAttendanceSheetById sheetId with
        | Ok sheet -> sheet
        | Error msg -> None

    let enrichEventAttendance (attendeeMap: Map<Guid, Attendee>) (event: EventAttendance) : EnrichedEventAttendance =
        let enrichedRecords =
            event.Records |> List.map (fun record ->
                match Map.tryFind record.AttendeeId attendeeMap with
                | Some attendee -> { AttendeeId = record.AttendeeId; FirstName = attendee.FirstName; LastName = attendee.LastName; Present = record.Present }
                | None -> { AttendeeId = record.AttendeeId; FirstName = ""; LastName = ""; Present = record.Present }
            )
        { Date = event.Date; Records = enrichedRecords }

    let getEnrichedMonthlyAttendanceSheetFromRepository (repository: IRepository) month year group = 
        let groupAttendees = 
            AttendeeManager.getAllAttendees repository
            |> List.filter (fun a -> a.Group = group)

        let attendeeMap = groupAttendees |> List.map (fun a -> a.Id, a) |> Map.ofList

        getMonthlyAttendanceSheetFromRepository repository month year group
        |> Option.map (fun sheet ->
                let enrichedEvents = sheet.Events |> List.map (enrichEventAttendance attendeeMap)
                { Id = sheet.Id; Group = sheet.Group; Month = sheet.Month; Year = sheet.Year; Events = enrichedEvents }
            )        

    let createMonthlyAttendanceReportFromRepository (repository: IRepository) (month: int) (year: int) =
        match getMonthlyAttendanceReportFromRepository repository month year with
        | Some report -> Ok report
        | None ->
            match repository.getAllAttendees() with
            | Ok attendees -> 
                let report = createMonthlyAttendanceReport attendees month year
                match (repository.saveMonthlyAttendanceReport report) with
                | Ok _ -> Ok report
                | Error msg -> Error msg
            | Error msg -> Error msg

    let createMonthlyAttendanceSheet (attendees : Attendee list) month year group : MonthlyAttendanceSheet = 
        let fridays = getAllFridays month year
        let sundays = getAllSundays month year
        let groupAttendees = attendees |> List.filter (fun a -> a.Group = group)
        let events : EventAttendance list = List.append fridays sundays |> List.map (fun d -> { Date = d; Records = [] })
        let records = 
            groupAttendees
            |> List.map (fun ga -> { AttendeeId = ga.Id; Present = false})
        {
            Id = Guid.NewGuid()
            Group = group
            Month = month
            Year = year
            Events = events |> List.map (fun e -> {e with Records = records})
        }

    let createMonthlyAttendanceSheetFromRepository (repository: IRepository) month year group : Result<MonthlyAttendanceSheet, string> =
        getMonthlyAttendanceSheetFromRepository repository month year group
        |> Option.map Ok
        |> Option.defaultWith (fun () ->
            repository.getAllAttendees()
            |> Result.bind (fun attendees ->
                let sheet = createMonthlyAttendanceSheet attendees month year group
                repository.saveMonthlyAttendanceSheet sheet
                |> Result.map (fun _ -> sheet)
            )
        )

    let addAttendanceRecord (sheet: MonthlyAttendanceSheet) (date: DateTime) (attendeeId: Guid) (present: bool) : MonthlyAttendanceSheet =        
        let updatedEvents =
            sheet.Events
            |> List.map (fun event ->
                if event.Date.Date = date.Date then
                    let updatedRecords = 
                        { AttendeeId = attendeeId; Present = present } :: 
                        (event.Records |> List.filter (fun record -> record.AttendeeId <> attendeeId))
                    { event with Records = updatedRecords }
                else
                    event)
        { sheet with Events = updatedEvents }

    let addAttendanceRecordToRepository (repository:IRepository) (sheetId: Guid) (newAttendanceRecord:NewAttendanceRecord) =
        repository.getMonthlyAttendanceSheetById sheetId
        |> Result.bind (fun result ->
            result
            |> Option.map (fun sheet -> 
                addAttendanceRecord sheet newAttendanceRecord.Date newAttendanceRecord.AttendeeId newAttendanceRecord.Present
                |> repository.saveMonthlyAttendanceSheet
            )
            |> Option.defaultValue (Error "Sheet not found")
        )
        



    
