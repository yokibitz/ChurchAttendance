namespace ChurchAttendance.Core
module Models = 
    open System

    type Group = 
        | Infants
        | Children
        | CYN
        | YAN
        | Men
        | Women

    let parseGroup (groupStr: string) : Group option =
        match groupStr with
        | _ when String.Equals(groupStr, "Infants", StringComparison.OrdinalIgnoreCase) -> Some Infants
        | _ when String.Equals(groupStr, "Children", StringComparison.OrdinalIgnoreCase) -> Some Children
        | _ when String.Equals(groupStr, "CYN", StringComparison.OrdinalIgnoreCase) -> Some CYN
        | _ when String.Equals(groupStr, "YAN", StringComparison.OrdinalIgnoreCase) -> Some YAN
        | _ when String.Equals(groupStr, "Men", StringComparison.OrdinalIgnoreCase) -> Some Men
        | _ when String.Equals(groupStr, "Women", StringComparison.OrdinalIgnoreCase) -> Some Women
        | _ -> None

    type NewAttendee = {
        FirstName: string
        LastName: string
        DateOfBirth: DateTime option
        Group: Group
    }

    type Attendee = {
        Id: Guid
        FirstName: string
        LastName: string
        DateOfBirth: DateTime option
        Group: Group
    }

    type AttendanceRecord = {
        AttendeeId: Guid
        Present: bool
    }
    
    type NewAttendanceRecord = {
        Date: DateTime
        AttendeeId: Guid
        Present: bool
    }

    type EnrichedAttendanceRecord = {
        AttendeeId: Guid
        FirstName: string
        LastName: string
        Present: bool
    }

    type EventAttendance = {
        Date: DateTime
        Records: AttendanceRecord list
    }

    type EnrichedEventAttendance = {
        Date: DateTime
        Records: EnrichedAttendanceRecord list
    }

    type MonthlyAttendanceSheet = {
        Id: Guid
        Group: Group
        Month: int
        Year: int
        Events: EventAttendance list
    }

    type EnrichedMonthlyAttendanceSheet = {
        Id: Guid
        Group: Group
        Month: int
        Year: int
        Events: EnrichedEventAttendance list
    }

    type MonthlyAttendanceReport = {
        Id: Guid
        Month: int
        Year: int
        Sheets: MonthlyAttendanceSheet list
    }

    type EnrichedMonthlyAttendanceReport = {
        Id: Guid
        Month: int
        Year: int
        Sheets: EnrichedMonthlyAttendanceSheet list
    }


