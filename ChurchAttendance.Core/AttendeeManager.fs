namespace ChurchAttendance.Core

open Repository

module AttendeeManager = 
    let getAllAttendees (repository:IRepository) =
            let result = repository.getAllAttendees()
            match result with
            | Ok attendees -> attendees
            | Error _ -> []
                

    let addAttendee (repository:IRepository) newAttendee = 
        let addResult = 
            newAttendee
            |> repository.addAttendee
        
        match addResult with
        | Ok _ -> ()
        | Error msg -> ()