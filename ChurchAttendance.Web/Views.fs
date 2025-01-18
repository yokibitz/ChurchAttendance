namespace ChurchAttendance.Web

open System
open Oxpecker
open Oxpecker.ViewEngine
open Oxpecker.Htmx
open ChurchAttendance.Core.Models
open Microsoft.AspNetCore.Http
open System.Text.Json
open System.Text.Json.Serialization

module Views = 
    let headers = 
        let value = (sprintf """{"Content-Type": "%s"}""" "application/json")        
        value

    let vals event attendee id = 
        (sprintf """{"date": "%s", "attendeeId": "%s", "present": document.getElementById("%s").checked}""" (event.Date.ToString("yyyy-MM-dd")) (attendee.AttendeeId.ToString()) id)        
        
    
    let private generateRows (ctx:HttpContext) sheetId (events:EnrichedEventAttendance list) = 
        let enrichedAttendees = 
            events 
            |> List.collect (fun event -> event.Records)
            |> List.distinctBy (fun r -> r.AttendeeId)

        Fragment() {
            yield! enrichedAttendees |> List.map (fun attendee ->
                tr(class'="odd:bg-gray-100 even:bg-white") { 
                    td(class'="py-2 px-4 border-b") { string attendee.AttendeeId }
                    td(class'="py-2 px-4 border-b") { string $"{attendee.LastName} {attendee.FirstName}" }
                    yield! (events |> List.map (fun event ->
                        let record = event.Records |> List.tryFind (fun r -> r.AttendeeId = attendee.AttendeeId)
                        match record with
                        | Some r -> td(class'="py-2 px-4 border-b") {
                                let id = DateTime.UtcNow.Ticks.ToString()
                                raw $"""<form hx-patch="/api/attendance/sheet/{sheetId}" 
                                hx-ext='json-enc'
                                hx-swap="innerHtml"                               
                                hx-target="#p{event.Date.ToString("yyyyMMdd")}"
                                hx-vals='js:{vals event attendee id}'
                                hx-trigger="change from:find .present-checkbox delay:1s">
                                <input id="{id}" type="checkbox" class="present-checkbox"
                                 {(if r.Present then "checked" else "")}/>
                                </form>
                                """
                            }
                        | None -> td(class'="py-2 px-4 border-b") {})
                    )
                }
            )
        }

    let dataOptions = sprintf """{'%s': '%s'}""" "contentType" "json"    

    let dataSignals event (r:EnrichedAttendanceRecord) id= 
        (sprintf """{"date": "%s", "attendeeId": "%s", "present": document.getElementById("%s").checked}""" (event.Date.ToString("yyyy-MM-dd")) (string r.AttendeeId) id)        

    let private generateDataStarRows (ctx:HttpContext) sheetId (events:EnrichedEventAttendance list) = 
        let enrichedAttendees = 
            events 
            |> List.collect (fun event -> event.Records)
            |> List.distinctBy (fun r -> r.AttendeeId)

        Fragment() {
            yield! enrichedAttendees |> List.map (fun attendee ->
                tr(class'="odd:bg-gray-100 even:bg-white") { 
                    td(class'="py-2 px-4 border-b") { string attendee.AttendeeId }
                    td(class'="py-2 px-4 border-b") { string $"{attendee.LastName} {attendee.FirstName}" }
                    yield! (events |> List.map (fun event ->
                        let record = event.Records |> List.tryFind (fun r -> r.AttendeeId = attendee.AttendeeId)
                        match record with
                        | Some r -> td(class'="py-2 px-4 border-b") {
                                let id = DateTime.UtcNow.Ticks.ToString()
                                raw $"""
                                <input data-on-change="@patch('/api/attendance/sheet/{sheetId}', {dataOptions})" 
                                data-signals='{dataSignals event r id}'
                                id="{id}" type="checkbox" class="present-checkbox"
                                {(if r.Present then "checked" else "")}
                                />
                                """
                            }
                        | None -> td(class'="py-2 px-4 border-b") {})
                    )
                }
            )
        }

    let attendanceSheetView (ctx:HttpContext) (sheet:EnrichedMonthlyAttendanceSheet) = 
        let jsonSerializerOptions = JsonSerializerOptions(JsonSerializerDefaults.Web)
        JsonFSharpOptions.Default().AddToJsonSerializerOptions(jsonSerializerOptions)

        Fragment(){
            div(class'="h-screen tabcontent overflow-x-auto") {
                h2(class'="text-4xl text-center font-semibold mb-2") { string sheet.Group }
                table(class'="min-w-full bg-white") {
                    thead() {
                        tr(class'="bg-gray-200") {
                            th(class'="py-2 px-4 border-b floating-total") { "Attendee ID" }
                            th(class'="py-2 px-4 border-b floating-total") { "Name" }
                            for event in sheet.Events do
                                th(class'="py-2 px-4 border-b floating-total") { 
                                    div() {                                        
                                        event.Date.ToString("yyyy-MM-dd") 
                                        div(){
                                            label(id="p"+event.Date.ToString("yyyyMMdd")) { $"Count: {event.Records |> List.filter (fun r -> r.Present) |> List.length}" }
                                        }                                        
                                    }                                    
                                }
                        }
                    }
                    tbody(class'="divide-y divide-gray-300") { generateRows ctx (string sheet.Id) sheet.Events }//.data("signals", JsonSerializer.Serialize(sheet, jsonSerializerOptions)) 
                }
                //raw """<div data-text="ctx.signals.JSON()"></div>"""
            }
        }

    let mainView (ctx:HttpContext) (content: HtmlElement)=
        html() {
            head() {
                title() { "Monthly Attendance" }
                link(rel="stylesheet", href="https://cdn.jsdelivr.net/npm/tailwindcss@2.2.19/dist/tailwind.min.css")                
                script(src="https://unpkg.com/htmx.org@1.9.10",
                    crossorigin="anonymous")
                script(src="https://unpkg.com/htmx.org@1.9.12/dist/ext/json-enc.js",
                    crossorigin="anonymous")
                raw """
                <script type="module" src="https://cdn.jsdelivr.net/gh/starfederation/datastar@v1.0.0-beta.1/bundles/datastar.js"></script>
                """
                raw """
                <style>
                    .floating-total {
                        position: sticky;
                        top: 0;
                        background-color: white;
                        z-index: 10;
                        padding: 0.5rem;
                        border-bottom: 2px solid #e2e8f0;
                    }                    
                    .tab-link {
                        padding: 0.5rem 1rem;
                        text-decoration: none;
                        color: #1a202c;
                        border-bottom: 2px solid transparent;
                    }
                    .tab-link:hover {
                        border-bottom: 2px solid #3182ce;
                    }
                    .tab-link.active {
                        border-bottom: 2px solid #3182ce;
                        font-weight: bold;
                    }
                </style>
                """
            }            
            body(class'="bg-gray-100 p-4") {
                main() {
                    div(class'="container mx-auto") {
                        span(class'="flex space-x-4 my-4") {
                            nav() {
                                a(class'="tab-link", href="/attendance/sheet/12/2024/children") { 
                                    span() 
                                    "Children" 
                                }
                            }
                            nav() {
                                a(class'="tab-link", href="/attendance/sheet/12/2024/men") { 
                                    span() 
                                    "Men" 
                                }
                            }
                        }
                        content
                    }
                }                    
            }
        }