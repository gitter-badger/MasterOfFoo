﻿// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System.Text
open MasterOfFoo
open System.Data.SqlClient
open System.Data.Common

type internal SqlEnv(n: int) =
    inherit PrintfEnv<unit, unit, SqlCommand>(())
    let queryString = StringBuilder()
    let dbParameters = System.Collections.Generic.List<SqlParameter>(n)
    let mutable index = 0

    override x.Finalize() =
        let cmd = new SqlCommand (queryString.ToString())
        for parameter in dbParameters do
            cmd.Parameters.Add(parameter) |> ignore
        cmd

    override this.Write(s : PrintableElement) =
        let asPrintf = s.FormatAsPrintF()
        match s.Type with
        | PrintableElementType.FromFormatSpecifier -> 
            let paramName = sprintf "@Param%i" index
            let param = SqlParameter(paramName, s.Value)
            dbParameters.Add(param)
            index <- index + 1
            ignore(queryString.Append paramName)
        | _ ->
            ignore(queryString.Append asPrintf)

    override this.WriteT(()) = ()

let sqlCommandf (format : Format<'T, unit, unit, SqlCommand>) =
    Printf.doPrintf format (fun n -> SqlEnv(n) :> PrintfEnv<_, _, _>)

type internal QueryStringEnv() =
    inherit PrintfEnv<StringBuilder, unit, string>(StringBuilder())

    override this.Finalize() = this.State.ToString()

    override this.Write(s : PrintableElement) =
        let asPrintf = s.FormatAsPrintF()
        match s.Type with
        | PrintableElementType.FromFormatSpecifier -> 
            let escaped = System.Uri.EscapeDataString(asPrintf)
            ignore(this.State.Append escaped)
        | _ ->
            ignore(this.State.Append asPrintf)

    override this.WriteT(()) = ()

let queryStringf (format : Format<'T, StringBuilder, unit, string>) =
    Printf.doPrintf format (fun _ -> QueryStringEnv() :> PrintfEnv<_, _, _>)

type internal MyTestEnv<'Result>(k, state) = 
    inherit PrintfEnv<StringBuilder, unit, 'Result>(state)
    override this.Finalize() : 'Result =
        printfn "Finalizing"
        k ()
    override this.Write(s : PrintableElement) =
        printfn "Writing: %A" s
        state.Append(s.FormatAsPrintF()) |> ignore
    override this.WriteT(()) = 
        printfn "WTF"

let testprintf (sb: StringBuilder) (format : Format<'T, StringBuilder, unit, unit>) =
    Printf.doPrintf format (fun n -> 
        MyTestEnv(ignore, sb) :> PrintfEnv<_, _, _>
    )

let simple () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %-010i hello %s" 1000 "World"
    sb.Clear() |> ignore
    testprintf sb "Hello %-010i hello %s" 1000 "World"
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let percentStar () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello '%*i'" 5 42
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let chained () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %s %s %s %s %s %s" "1" "2" "3" "4" "5" "6"
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let complex () = 
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %s %s %s %s %s %s %06i %t" "1" "2" "3" "4" "5" "6" 5 (fun x -> x.Append("CALLED") |> ignore)
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

[<EntryPoint>]
let main argv =
    printfn "%s" (queryStringf "hello/uri+with space/x?foo=%s&bar=%s&work=%i" "#baz" "++Hello world && problem for arg √" 1)

    let cmd: SqlCommand =
        sqlCommandf
            "SELECT * FROM tbUser WHERE UserId=%i AND NAME=%s AND CREATIONDATE > %O"
            5
            "Test"
            System.DateTimeOffset.Now

    //simple ()
    //percentStar ()
    //chained ()
    //complex ()
    ignore(System.Console.ReadLine ())
    0 // return an integer exit code
