module Index

open AutoComplete
open Elmish
open Fable.Remoting.Client
open Shared
open Thoth.Elmish

type Completions = { Items : string array; Selected : string }

type Model = {
    PackageIds: Completions
    PackageVersions: Completions
    Debouncer: Debouncer.State
}

type Msg =
    | DebouncerSelfMsg of Debouncer.SelfMessage<Msg>
    | TypingPackageId of string
    | SetPackageId of string
    | UpdatedPackageIds of string []
    | SetPackageVersion of string
    | UpdatedPackageVersions of string []

let todosApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init(): Model * Cmd<Msg> =
    let model = {
        PackageIds = { Items = [||]; Selected = "" }
        PackageVersions = { Items = [||]; Selected = "" }
        Debouncer = Debouncer.create()
    }
    let cmd = Cmd.none
    model, cmd

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | DebouncerSelfMsg msg ->
        let (debouncer, cmd) = Debouncer.update msg model.Debouncer
        { model with Debouncer = debouncer }, cmd

    | TypingPackageId str ->
        let (debouncerModel, debouncerCmd) =
            model.Debouncer
            |> Debouncer.bounce (System.TimeSpan.FromMilliseconds 300.) "user_input" (str |> SetPackageId)
        let cmd = Cmd.batch [ Cmd.map DebouncerSelfMsg debouncerCmd ]
        { model with Debouncer = debouncerModel
                     PackageIds = { model.PackageIds with Selected = str } }, cmd

    | SetPackageId value ->
        let cmd = Cmd.batch [
            Cmd.OfAsync.perform todosApi.autoComplete value UpdatedPackageIds
            Cmd.OfAsync.perform todosApi.getVersions value UpdatedPackageVersions
        ]
        { model with PackageIds = { model.PackageIds with Selected = value } }, cmd

    | UpdatedPackageIds completions ->
        { model with PackageIds = { model.PackageIds with Items = completions } }, Cmd.none

    | UpdatedPackageVersions completions ->
        { model with PackageVersions = { model.PackageVersions with Items = completions } }, Cmd.none

    | SetPackageVersion value ->
        { model with PackageVersions = { model.PackageVersions with Selected = value } }, Cmd.none

open Fable.React
open Fable.React.Props
open Fulma

let navBrand =
    Navbar.Brand.div [ ] [
        Navbar.Item.a [
            Navbar.Item.Props [ Href "https://safe-stack.github.io/" ]
            Navbar.Item.IsActive true
        ] [
            img [
                Src "/favicon.png"
                Alt "Logo"
            ]
        ]
    ]

let containerBox (model : Model) (dispatch : Msg -> unit) =
    let itemStyle highlight = [
        Background(if highlight then "gray" else "none")
        Padding "5px 10px"
    ]
    let menuStyle = [
        Position PositionOptions.Absolute
        ZIndex 10.
        Background "rgba(255, 255, 255, 0.9) none repeat scroll 0% 0%"
        Left "unset"
        Top "unset"
        OverflowStyle OverflowOptions.Auto
        Border "2px solid #cccccc"
        BorderRadius 5.
    ]
    Box.box' [ ] [
        Field.div [
            Field.IsExpanded
            ] [
            AutoComplete.autocomplete [
                Items model.PackageIds.Items
                AutoCompleteProps<_>.OnChange (fun _ v -> v |> TypingPackageId |> dispatch)
                AutoCompleteProps<_>.Value model.PackageIds.Selected
                AutoCompleteProps<_>.OnSelect (SetPackageId >> dispatch)
                GetItemValue id
                RenderItem (fun value highlight ->
                    div [
                        Prop.Key value
                        Props.Style (highlight |> itemStyle)
                    ] [ str value ])
                InputProps [
                    ClassName "input is-primary";
                    Placeholder "Select package";
                ]
                MenuStyle menuStyle
                WrapperStyle [
                    Display DisplayOptions.Block
                ]
            ]
            AutoComplete.autocomplete [
                Items model.PackageVersions.Items
                AutoCompleteProps<_>.Value model.PackageVersions.Selected
                AutoCompleteProps<_>.OnSelect (SetPackageVersion >> dispatch)
                GetItemValue id
                RenderItem (fun value highlight ->
                    div [
                        Prop.Key value
                        Props.Style (highlight |> itemStyle)
                    ] [ str value ])
                InputProps [
                    ClassName "input is-primary";
                    Placeholder "Select version";
                ]
                MenuStyle menuStyle
                WrapperStyle [
                    Display DisplayOptions.Block
                ]
            ]
        ]
    ]

let view (model : Model) (dispatch : Msg -> unit) =
    Hero.hero [
        Hero.Color IsPrimary
        Hero.IsFullHeight
        Hero.Props [
            Style [
                Background """linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 0, 0.5)), url("https://unsplash.it/1200/900?random") no-repeat center center fixed"""
                BackgroundSize "cover"
            ]
        ]
    ] [
        Hero.head [ ] [
            Navbar.navbar [ ] [
                Container.container [ ] [ navBrand ]
            ]
        ]

        Hero.body [ ] [
            Container.container [ ] [
                Column.column [
                    Column.Width (Screen.All, Column.Is6)
                    Column.Offset (Screen.All, Column.Is3)
                ] [
                    Heading.p [ Heading.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] [ str "nugraph" ]
                    containerBox model dispatch
                ]
            ]
        ]
    ]
