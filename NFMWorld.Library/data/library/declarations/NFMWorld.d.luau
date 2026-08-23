declare extern type CarStatsData with
    name: string
    collection: Collection
    topSpeed: number
    acceleration: number
    handling: number
    powerSave: number
    strength: number
    maxHealth: number
    stunting: number
    hypergliding: number
    abing: number
end

declare CarStatsData: {
}

declare extern type CarCollectionsData with
    collections: { [integer|number]: CarCollectionData }
end

declare CarCollectionsData: {
}

declare extern type CarCollectionData with
    id: Collection
    name: string
    cars: { [integer|number]: CarStatsData }
end

declare CarCollectionData: {
}

declare extern type CurrentCollectionData with
    id: Collection
end

declare CurrentCollectionData: {
}

declare extern type PauseState with
    lap: integer
    totalLaps: integer
    position: integer
    totalRacers: integer
    stageName: string
end

declare PauseState: {
}

declare extern type AccountData with
    name: string
    isLoggedIn: boolean
    avatarUrl: string
    __eq: ((left: AccountData|nil, right: AccountData|nil) -> boolean)
end

declare AccountData: {
}

declare extern type CapturedKey with
    action: string
    keyCode: integer
    cancelled: boolean
end

declare CapturedKey: {
}

declare extern type SettingsSnapshot with
    selectedRenderer: integer
    selectedResolution: integer
    selectedDisplayMode: integer
    vsync: boolean
    fpsLimit: integer
    antialias: integer
    shadowCascadeLevel: integer
    shadowResolution: integer
    renderDistance: integer
    lowLatency: boolean
    lineWidth: number
    masterVolume: number
    musicVolume: number
    effectsVolume: number
    muteAll: boolean
    remasteredMusic: boolean
    fov: number
    followY: integer
    followZ: integer
    smoothFov: boolean
    keyBindings: { [integer|number]: KeyBindingData }
end

declare SettingsSnapshot: {
}

declare extern type KeyBindingData with
    action: string
    displayName: string
    keyCode: integer
end

declare KeyBindingData: {
}

declare extern type AvailableOptions with
    renderers: { [integer|number]: string }
    resolutions: { [integer|number]: string }
    displayModes: { [integer|number]: string }
    antialiasModes: { [integer|number]: string }
    shadowCascadeLevels: { [integer|number]: string }
    shadowResolutions: { [integer|number]: string }
    renderDistanceNames: { [integer|number]: string }
end

declare AvailableOptions: {
}

declare extern type CounterData with
    value: integer
end

declare CounterData: {
}

