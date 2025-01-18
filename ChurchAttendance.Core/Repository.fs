namespace ChurchAttendance.Core

open Models

module Repository =
    open System
    type IRepository =
        abstract member addAttendee: newAttendee: NewAttendee -> Result<unit, string>    
        abstract member getAllAttendees: unit -> Result<Attendee list, string>
        abstract member saveMonthlyAttendanceReport: monthlyAttendanceReport: MonthlyAttendanceReport -> Result<unit, string>
        abstract member saveMonthlyAttendanceSheet: monthlyAttendanceSheet: MonthlyAttendanceSheet -> Result<MonthlyAttendanceSheet, string>
        abstract member getMonthlyAttendanceReport: int -> int -> Result<MonthlyAttendanceReport option, string>
        abstract member getMonthlyAttendanceSheet: int -> int -> Group -> Result<MonthlyAttendanceSheet option, string>
        abstract member getMonthlyAttendanceSheetById: Guid -> Result<MonthlyAttendanceSheet option, string>