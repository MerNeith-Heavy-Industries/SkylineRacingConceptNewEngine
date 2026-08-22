declare extern type BaseAi with
    runAi: (() -> nil)
end

declare BaseAi: {
}

declare extern type AiContext with
    players: { [integer]: ClientSidePlayer }
    player: ClientSidePlayer
    stage: BackendStage
    config: { [any]: any }|nil
end

declare AiContext: {
}

declare extern type BackendCar extends BackendGameObject with
    groundAt: integer
    maxRadius: integer
    wheelAngle: f64euler
    turningWheelAngle: f64euler
    wheels: { [integer]: Rad3dWheelDef }
    carPhysics: CarPhysics
    control: Control
    currentCheckpoint: integer
    currentLap: integer
    totalCheckpoint: integer
    lastCheckpointNode: integer
    placement: integer
    rad: Rad3d
    stats: CarStats
    wasted: boolean
    player: ClientSidePlayerInfo
    children: { [integer]: BackendGameObject }
    parent: BackendGameObject|nil
    position: fixed64vector3
    rotation: f64euler
    drive: ((stage: BackendStage) -> nil)
end

declare BackendCar: {
}

declare extern type BackendGameObject with
    children: { [integer]: BackendGameObject }
    parent: BackendGameObject|nil
    position: fixed64vector3
    rotation: f64euler
end

declare BackendGameObject: {
}

declare extern type BackendStage with
    pieces: { [integer]: BackendGameObject }
    nodes: { [integer]: StageObject }
    checkpoints: { [integer]: StageObject }
    fixHoops: { [integer]: StageObject }
    nlaps: integer
    name: string
    path: string
    stageLoader: StageLoader
end

declare BackendStage: {
}

declare extern type WallCollision extends BackendGameObject with
    boxes: { [integer]: Rad3dBoxDef }
    maxRadius: integer
    children: { [integer]: BackendGameObject }
    parent: BackendGameObject|nil
    position: fixed64vector3
    rotation: f64euler
end

declare WallCollision: {
}

declare extern type StageObject extends BackendGameObject with
    originalPlacement: PiecePlacement
    rad: Rad3d
    nodeKind: AiNodeKind
    isSpecial: boolean
    boxes: { [integer]: Rad3dBoxDef }
    maxRadius: integer
    fileName: string
    children: { [integer]: BackendGameObject }
    parent: BackendGameObject|nil
    position: fixed64vector3
    rotation: f64euler
end

declare StageObject: {
}

declare extern type IGamemodeContext with
end

declare IGamemodeContext: {
}

declare extern type FrameTrace with
end

declare FrameTrace: {
    addMessage: ((message: string) -> nil),
}

declare extern type ClientSidePlayer with
    info: ClientSidePlayerInfo
    index: integer
    car: BackendCar|nil
    bot: BaseAi|nil
    isFake: boolean
end

declare ClientSidePlayer: {
}

declare extern type ClientSidePlayerInfo with
    playerName: string
    carName: string
    color: Color3
    isBot: boolean
    isClientPlayer: boolean
end

declare ClientSidePlayerInfo: {
}

declare extern type HudStateData with
    speed: number
    power: number
    damage: number
    lap: integer
    totalLaps: integer
    lapTime: integer
    position: integer
    totalRacers: integer
    stateText: string
    lapDiffMs: integer|nil
    lastLapDiffMs: integer|nil
    chkDiffMs: integer|nil
    lastChkDiffMs: integer|nil
    countdownTimer: integer
    stateTextEndsAt: number|nil
end

declare HudStateData: {
}

declare extern type LuaClientContext with
    resetCheckpointGlow: (() -> nil)
    updateCheckpointGlow: ((currentCheckpoint: integer, isFinish: boolean) -> nil)
    getClientCarCallbacks: ((car: BackendCar) -> LuaClientCarContext)
end

declare LuaClientContext: {
}

declare extern type LuaClientCarContext with
    castsShadow: boolean
    getsShadowed: boolean|nil
    alphaOverride: number|nil
    glow: boolean|nil
    finish: boolean|nil
end

declare LuaClientCarContext: {
}

declare extern type GamemodeContext with
    stage: BackendStage
    players: { [integer]: ClientSidePlayer }
    clientPlayer: ClientSidePlayer
    hudState: HudStateData
    physics: PhysicsController
    config: { [any]: any }|nil
    client: LuaClientContext
    countdownInterval: integer
    createCar: ((playerIndex: integer, x: fixed64, z: fixed64) -> BackendCar)
    calculatePositions: (() -> nil)
    handleCheckPoint: ((car: BackendCar) -> boolean)
    handleFixHoops: ((car: BackendCar) -> boolean)
    clientReset: (() -> nil)
    sendEvent: ((type: string, payload: { [any]: any }) -> nil)
    updateHudAndSounds: ((car: BackendCar) -> nil)
    removeFakePlayers: (() -> nil)
    clonePlayer: ((basedOnPlayer: ClientSidePlayer) -> ClientSidePlayer)
end

declare GamemodeContext: {
}

declare extern type ServerGamemodeContext with
    currentStage: BackendStage
    players: { [integer]: ServerSidePlayerInfo }
    config: { [any]: any }|nil
    countdownInterval: integer
    getPlayerPosition: ((playerId: string) -> fixed64vector3|nil)
    broadcastEvent: ((type: string, payload: { [any]: any }) -> nil)
    finishRace: ((standings: RaceStandings) -> nil)
end

declare ServerGamemodeContext: {
}

declare extern type TimeTrial with
    hasGhost: boolean
    begin: ((car: BackendCar) -> nil)
    applyGhost: ((ghostCar: BackendCar, tick: integer) -> nil)
    getSplitDiff: ((splitIndex: integer) -> number|nil)
    getLastSplitDiff: (() -> number|nil)
    getLapDiff: ((lapIndex: integer) -> number|nil)
    recordSplit: ((splitTime: number) -> nil)
    getLapTime: ((lapIndex: integer) -> number|nil)
    getLastSplitTime: (() -> number|nil)
    getBestLastSplitTime: (() -> number|nil)
    record: ((car: BackendCar) -> nil)
    save: (() -> nil)
end

declare TimeTrial: {
    new: ((stage: BackendStage) -> nil),
}

declare extern type PhysicsController with
    gameTick: (() -> nil)
end

declare PhysicsController: {
}

declare extern type SurfaceType with
end

declare SurfaceType: {
}

declare extern type CarPhysics with
    halted: boolean
    btab: boolean
    capcnt: integer
    capsized: boolean
    caught: { [integer]: boolean }
    stat: CarStats
    cn: integer
    cntdest: integer
    cntouch: integer
    collidingWithClientPlayer: boolean
    crank: { [integer]: integerView }
    lcrank: { [integer]: integerView }
    cxz: fixed64
    staticCameraXz: fixed64
    dcnt: integer
    dcomp: fixed64
    lcomp: fixed64
    wasted: boolean
    dominate: { [integer]: booleanView }
    drag: fixed64
    fixes: integer
    forca: fixed64
    ftab: boolean
    turnXz: fixed64
    gtouch: boolean
    hitmag: integer
    im: integer
    lastcolido: integer
    loop: integer
    lxz: fixed64
    mtouch: boolean
    mxz: fixed64
    numRoofDamage: integer
    newcar: boolean
    newedcar: integer
    nmlt: integer
    nofocus: boolean
    outshakedam: integer
    pd: boolean
    pl: boolean
    pmlt: integer
    point: integer
    power: fixed64
    powerup: fixed64
    pr: boolean
    pu: boolean
    pushed: boolean
    pxy: fixed64
    pzy: fixed64
    rcomp: fixed64
    rtab: boolean
    scx: { [integer]: fixed64 }
    scy: { [integer]: fixed64 }
    scz: { [integer]: fixed64 }
    shakedam: integer
    skid: integer
    speed: fixed64
    roofDamage: integer
    surfCount: integer
    surfing: boolean
    tilt: fixed64
    totalStuntXy: fixed64
    totalStuntXz: fixed64
    totalStuntZy: fixed64
    tcnt: integer
    txz: fixed64
    ucomp: fixed64
    wtouch: boolean
    xtpower: integer
end

declare CarPhysics: {
}

declare extern type CarStats with
    swits: Int3
    acelf: fixed64vector3
    handb: integer
    airs: fixed64
    airc: integer
    grip: fixed64
    bounce: fixed64
    simag: fixed64
    moment: fixed64
    comprad: fixed64
    push: fixed64
    revpush: fixed64
    lift: integer
    revlift: integer
    powerloss: integer
    flipy: integer
    msquash: integer
    clrad: integer
    dammult: fixed64
    maxmag: integer
    dishandle: fixed64
    outdam: fixed64
    name: string
    enginsignature: integer
    turnradius: integer
    roadgrip: fixed64|nil
    offroadgrip: fixed64|nil
    offtrackgrip: fixed64|nil
    turn: fixed64
end

declare CarStats: {
}

declare extern type Int3 with
    x: integer
    y: integer
    z: integer
end

declare Int3: {
}

declare extern type Control with
    arrace: boolean
    chatup: integer
    down: boolean
    enter: boolean
    exit: boolean
    handb: boolean
    multion: integer
    mutem: boolean
    mutes: boolean
    radar: boolean
    right: boolean
    up: boolean
    left: boolean
    lookback: integer
    wall: integer
    zyinv: boolean
end

declare Control: {
}

declare extern type AiNodeKind with
end

declare AiNodeKind: {
}

declare extern type ServerSidePlayerInfo with
    id: string
    playerName: string
    carName: string
    color: Color3
end

declare ServerSidePlayerInfo: {
}

declare extern type AttachmentLineDirection with
end

declare AttachmentLineDirection: {
}

declare extern type LineType with
end

declare LineType: {
}

declare extern type PolyType with
end

declare PolyType: {
}

declare extern type Rad3d with
    maxRadius: integer
    colors: { [integer]: Color3 }
    stats: CarStats
    wheels: { [integer]: Rad3dWheelDef }
    rims: Rad3dRimsDef|nil
    boxes: { [integer]: Rad3dBoxDef }
    polys: { [integer]: Rad3dPoly }
    castsShadow: boolean
    atp: { [integer]: LuaVector2 }
    fileName: string
    atLines: { [integer]: Rad3dAttachmentLine }|nil
end

declare Rad3d: {
}

declare extern type Rad3dAttachmentLine with
    direction: AttachmentLineDirection
    offset: fixed64
end

declare Rad3dAttachmentLine: {
}

declare extern type Rad3dBoxDef with
    xy: integer
    zy: integer
    radius: fixed64vector3
    translation: fixed64vector3
    surfaceType: SurfaceType
    damage: integer
    notWall: boolean
    color: Color3
    tractionMultiplier: fixed64|nil
end

declare Rad3dBoxDef: {
}

declare extern type Rad3dPoly with
    color: Color3
    colNum: integer|nil
    polyType: PolyType
    lineType: LineType|nil
end

declare Rad3dPoly: {
}

declare extern type Rad3dRimsDef with
    color: Color3
    size: number
    depth: number
end

declare Rad3dRimsDef: {
}

declare extern type Rad3dWheelDef with
    position: fixed64vector3
    rotates: integer
    width: fixed64
    height: fixed64
end

declare Rad3dWheelDef: {
}

declare extern type PiecePlacement with
    type: PiecePlacementType
    object: Rad3d
    position: fixed64vector3
    rotation: f64euler
    nodeKind: AiNodeKind|nil
    isSpecial: boolean
    isWall: boolean
end

declare PiecePlacement: {
}

declare extern type PiecePlacementType with
end

declare PiecePlacementType: {
}

declare extern type StageWall with
    direction: WallDirection
    count: integer
    position: integer
    offset: integer
end

declare StageWall: {
}

declare extern type WallDirection with
end

declare WallDirection: {
}

declare extern type HierarchyGroup with
    name: string
    pieces: { [integer]: PiecePlacementView }
    coordinateKeys: { [integer]: stringView }
end

declare HierarchyGroup: {
}

declare extern type EnvironmentInstruction with
end

declare EnvironmentInstruction: {
}

declare extern type SnapInstruction extends EnvironmentInstruction with
    color: Color3
end

declare SnapInstruction: {
}

declare extern type SkyInstruction extends EnvironmentInstruction with
    color: Color3
end

declare SkyInstruction: {
}

declare extern type FogInstruction extends EnvironmentInstruction with
    color: Color3
end

declare FogInstruction: {
}

declare extern type CloudsInstruction extends EnvironmentInstruction with
    clouds: { [integer]: integer }
end

declare CloudsInstruction: {
}

declare extern type GroundInstruction extends EnvironmentInstruction with
    color: Color3
end

declare GroundInstruction: {
}

declare extern type TextureInstruction extends EnvironmentInstruction with
    texture: { [integer]: integer }
end

declare TextureInstruction: {
}

declare extern type PolysInstruction extends EnvironmentInstruction with
    color: Color3
end

declare PolysInstruction: {
}

declare extern type StageLoader with
    path: string
    nlaps: integer
    musicPath: string
    remasteredMusicPath: string
    musicFreqMul: number
    musicTempoMul: number
    name: string
    indexOffset: integer
    sx: integer
    sz: integer
    ncx: integer
    ncz: integer
    cloudCoverage: number|nil
    fogDensity: integer|nil
    fadeFrom: integer|nil
    lightsOn: boolean
    drawMountains: boolean
    mountainSeed: integer|nil
    mountainCoverage: number|nil
    lightDirection: LuaVector3|nil
    pieces: { [integer]: PiecePlacement }
    walls: { [integer]: Rad3dBoxDef }
    maxr: integer
    maxl: integer
    maxt: integer
    maxb: integer
    environmentInstructions: { [integer]: EnvironmentInstruction }
    drawPolys: boolean
    drawClouds: boolean
end

declare StageLoader: {
}

declare extern type Color3 with
    r: integer
    g: integer
    b: integer
end

declare Color3: {
}

declare extern type Key with
end

declare Key: {
}

declare extern type Keys with
    none: boolean
    lButton: boolean
    rButton: boolean
    cancel: boolean
    mButton: boolean
    xButton1: boolean
    xButton2: boolean
    back: boolean
    tab: boolean
    lineFeed: boolean
    clear: boolean
    returnKey: boolean
    enter: boolean
    shiftKey: boolean
    controlKey: boolean
    menu: boolean
    pause: boolean
    capital: boolean
    capsLock: boolean
    kanaMode: boolean
    hanguelMode: boolean
    hangulMode: boolean
    junjaMode: boolean
    finalMode: boolean
    hanjaMode: boolean
    kanjiMode: boolean
    escape: boolean
    iMEConvert: boolean
    iMENonconvert: boolean
    iMEAccept: boolean
    iMEAceept: boolean
    iMEModeChange: boolean
    space: boolean
    prior: boolean
    pageUp: boolean
    next: boolean
    pageDown: boolean
    endKey: boolean
    home: boolean
    left: boolean
    up: boolean
    right: boolean
    down: boolean
    select: boolean
    print: boolean
    execute: boolean
    snapshot: boolean
    printScreen: boolean
    insert: boolean
    delete: boolean
    help: boolean
    d0: boolean
    d1: boolean
    d2: boolean
    d3: boolean
    d4: boolean
    d5: boolean
    d6: boolean
    d7: boolean
    d8: boolean
    d9: boolean
    a: boolean
    b: boolean
    c: boolean
    d: boolean
    e: boolean
    f: boolean
    g: boolean
    h: boolean
    i: boolean
    j: boolean
    k: boolean
    l: boolean
    m: boolean
    n: boolean
    o: boolean
    p: boolean
    q: boolean
    r: boolean
    s: boolean
    t: boolean
    u: boolean
    v: boolean
    w: boolean
    x: boolean
    y: boolean
    z: boolean
    lWin: boolean
    rWin: boolean
    apps: boolean
    sleep: boolean
    numPad0: boolean
    numPad1: boolean
    numPad2: boolean
    numPad3: boolean
    numPad4: boolean
    numPad5: boolean
    numPad6: boolean
    numPad7: boolean
    numPad8: boolean
    numPad9: boolean
    multiply: boolean
    add: boolean
    separator: boolean
    subtract: boolean
    decimal: boolean
    divide: boolean
    f1: boolean
    f2: boolean
    f3: boolean
    f4: boolean
    f5: boolean
    f6: boolean
    f7: boolean
    f8: boolean
    f9: boolean
    f10: boolean
    f11: boolean
    f12: boolean
    f13: boolean
    f14: boolean
    f15: boolean
    f16: boolean
    f17: boolean
    f18: boolean
    f19: boolean
    f20: boolean
    f21: boolean
    f22: boolean
    f23: boolean
    f24: boolean
    numLock: boolean
    scroll: boolean
    lShiftKey: boolean
    rShiftKey: boolean
    lControlKey: boolean
    rControlKey: boolean
    lMenu: boolean
    rMenu: boolean
    browserBack: boolean
    browserForward: boolean
    browserRefresh: boolean
    browserStop: boolean
    browserSearch: boolean
    browserFavorites: boolean
    browserHome: boolean
    volumeMute: boolean
    volumeDown: boolean
    volumeUp: boolean
    mediaNextTrack: boolean
    mediaPreviousTrack: boolean
    mediaStop: boolean
    mediaPlayPause: boolean
    launchMail: boolean
    selectMedia: boolean
    launchApplication1: boolean
    launchApplication2: boolean
    oemSemicolon: boolean
    oem1: boolean
    oemplus: boolean
    oemcomma: boolean
    oemMinus: boolean
    oemPeriod: boolean
    oemQuestion: boolean
    oem2: boolean
    oem3: boolean
    oemtilde: boolean
    oemOpenBrackets: boolean
    oem4: boolean
    oemPipe: boolean
    oem5: boolean
    oemCloseBrackets: boolean
    oem6: boolean
    oem7: boolean
    oemQuotes: boolean
    oem8: boolean
    oem102: boolean
    oemBackslash: boolean
    processKey: boolean
    packet: boolean
    attn: boolean
    crsel: boolean
    exsel: boolean
    eraseEof: boolean
    play: boolean
    zoom: boolean
    noName: boolean
    pa1: boolean
    oemClear: boolean
end

declare Keys: {
}

declare extern type MouseButton with
end

declare MouseButton: {
}

declare extern type MouseButtons with
end

declare MouseButtons: {
}

declare extern type LuaVector2 with
    x: number
    y: number
end

declare LuaVector2: {
}

declare extern type LuaVector3 with
    x: number
    y: number
    z: number
end

declare LuaVector3: {
}

declare extern type DeterministicRandom with
    next: (() -> integer)
    nextBetween: ((min: integer, max: integer) -> integer)
    nextf64: (() -> fixed64)
end

declare DeterministicRandom: {
}

declare extern type Stopwatch with
    isRunning: boolean
    elapsed: number
    elapsedMilliseconds: integer
    elapsedMicroseconds: integer
    stop: (() -> nil)
    start: (() -> nil)
    restart: (() -> nil)
    reset: (() -> nil)
end

declare Stopwatch: {
    new: (() -> nil),
    startNew: (() -> Stopwatch),
}

declare extern type Component extends Node with
    visualChildren: { [integer]: Node }
    canHaveChildren: boolean
    name: string
    isFocusable: boolean
    layoutMarginPosition: LuaVector2
    layoutMarginSize: LuaVector2
    layoutBorderPosition: LuaVector2
    layoutBorderSize: LuaVector2
    layoutPaddingPosition: LuaVector2
    layoutPaddingSize: LuaVector2
    layoutContentPosition: LuaVector2
    layoutContentSize: LuaVector2
    layoutMargin: LuaVector2
    layoutPadding: LuaVector2
    layoutBorder: LuaVector2
    layoutWidth: number
    layoutHeight: number
    layoutX: number
    layoutY: number
    layoutDirection: Direction
    hadOverflow: boolean
    layoutMarginTop: number
    layoutMarginBottom: number
    layoutMarginLeft: number
    layoutMarginRight: number
    layoutPaddingTop: number
    layoutPaddingBottom: number
    layoutPaddingLeft: number
    layoutPaddingRight: number
    layoutBorderTop: number
    layoutBorderBottom: number
    layoutBorderLeft: number
    layoutBorderRight: number
    hasNewLayout: boolean
    isDirty: boolean
    isReferenceBaseline: boolean
    scrollLeft: number
    scrollTop: number
    scrollableWidth: number
    scrollableHeight: number
    isClipping: boolean
    isDisplayed: boolean
    visualParent: Node|nil
    addChild: ((child: Node) -> nil)
    insertAt: ((index: integer, child: Node) -> nil)
    removeAt: ((index: integer) -> nil)
    scrollIntoView: (() -> nil)
end

declare Component: {
}

declare extern type TextInput extends Component with
    placeholder: string
    text: string
    visualChildren: { [integer]: Node }
    canHaveChildren: boolean
    name: string
    isFocusable: boolean
    layoutMarginPosition: LuaVector2
    layoutMarginSize: LuaVector2
    layoutBorderPosition: LuaVector2
    layoutBorderSize: LuaVector2
    layoutPaddingPosition: LuaVector2
    layoutPaddingSize: LuaVector2
    layoutContentPosition: LuaVector2
    layoutContentSize: LuaVector2
    layoutMargin: LuaVector2
    layoutPadding: LuaVector2
    layoutBorder: LuaVector2
    layoutWidth: number
    layoutHeight: number
    layoutX: number
    layoutY: number
    layoutDirection: Direction
    hadOverflow: boolean
    layoutMarginTop: number
    layoutMarginBottom: number
    layoutMarginLeft: number
    layoutMarginRight: number
    layoutPaddingTop: number
    layoutPaddingBottom: number
    layoutPaddingLeft: number
    layoutPaddingRight: number
    layoutBorderTop: number
    layoutBorderBottom: number
    layoutBorderLeft: number
    layoutBorderRight: number
    hasNewLayout: boolean
    isDirty: boolean
    isReferenceBaseline: boolean
    scrollLeft: number
    scrollTop: number
    scrollableWidth: number
    scrollableHeight: number
    isClipping: boolean
    isDisplayed: boolean
    visualParent: Node|nil
    addChild: ((child: Node) -> nil)
    insertAt: ((index: integer, child: Node) -> nil)
    removeAt: ((index: integer) -> nil)
    scrollIntoView: (() -> nil)
end

declare TextInput: {
}

declare extern type Direction with
end

declare Direction: {
}

declare extern type BaseMouseDragEvent with
    dragStart: LuaVector2
    position: LuaVector2
    button: integer
    buttons: MouseButtons
    ctrlKey: boolean
    metaKey: boolean
    shiftKey: boolean
end

declare BaseMouseDragEvent: {
}

declare extern type BaseMouseEvent with
    position: LuaVector2
    button: MouseButton
    buttons: MouseButtons
    ctrlKey: boolean
    altKey: boolean
    shiftKey: boolean
end

declare BaseMouseEvent: {
}

declare extern type BaseMouseMoveEvent with
    position: LuaVector2
    buttons: MouseButtons
    ctrlKey: boolean
    altKey: boolean
    shiftKey: boolean
end

declare BaseMouseMoveEvent: {
}

declare extern type BaseMouseWheelEvent with
    delta: LuaVector3
    position: LuaVector2
    buttons: MouseButtons
    ctrlKey: boolean
    metaKey: boolean
    shiftKey: boolean
end

declare BaseMouseWheelEvent: {
}

declare extern type KeyboardEvent with
    keyChar: Key
    keyCode: Key
    keys: Keys
end

declare KeyboardEvent: {
}

declare extern type KeyboardTypingEvent with
    keyChar: integer
end

declare KeyboardTypingEvent: {
}

declare extern type MouseDragEvent with
    dragStart: LuaVector2
    relativeDragStart: LuaVector2
    position: LuaVector2
    button: integer
    buttons: MouseButtons
    ctrlKey: boolean
    metaKey: boolean
    shiftKey: boolean
    relativePosition: LuaVector2
end

declare MouseDragEvent: {
}

declare extern type MouseEvent with
    position: LuaVector2
    button: MouseButton
    buttons: MouseButtons
    ctrlKey: boolean
    metaKey: boolean
    shiftKey: boolean
    relativePosition: LuaVector2
end

declare MouseEvent: {
}

declare extern type MouseMoveEvent with
    position: LuaVector2
    buttons: MouseButtons
    ctrlKey: boolean
    metaKey: boolean
    shiftKey: boolean
    relativePosition: LuaVector2
end

declare MouseMoveEvent: {
}

declare extern type MouseWheelEvent with
    delta: LuaVector3
    position: LuaVector2
    buttons: MouseButtons
    ctrlKey: boolean
    metaKey: boolean
    shiftKey: boolean
    relativePosition: LuaVector2
end

declare MouseWheelEvent: {
}

declare extern type Node with
    visualParent: Node|nil
    visualChildren: { [integer]: Node }
end

declare Node: {
}

declare extern type TextNode extends Node with
    visualChildren: { [integer]: Node }
    text: string
    visualParent: Node|nil
end

declare TextNode: {
}

