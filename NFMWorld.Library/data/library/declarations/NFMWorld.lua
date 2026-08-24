---@class CarStatsData
---@field fileName string
---@field name string
---@field collection Collection
---@field topSpeed number
---@field acceleration number
---@field handling number
---@field powerSave number
---@field strength number
---@field maxHealth number
---@field stunting number
---@field hypergliding number
---@field abing number

CarStatsData = {}


---@class CarCollectionsData
---@field collections { [integer|number]: CarCollectionData }

CarCollectionsData = {}


---@class CarCollectionData
---@field id Collection
---@field name string
---@field cars { [integer|number]: CarStatsData }

CarCollectionData = {}


---@class CurrentCollectionData
---@field id Collection

CurrentCollectionData = {}


---@class PauseState
---@field lap integer
---@field totalLaps integer
---@field position integer
---@field totalRacers integer
---@field stageName string

PauseState = {}


---@class AccountData : System.IEquatable_AccountData
---@field name string
---@field isLoggedIn boolean
---@field avatarUrl string

AccountData = {}


---@class CapturedKey
---@field action string
---@field keyCode integer
---@field cancelled boolean

CapturedKey = {}


---@class SettingsSnapshot
---@field selectedRenderer integer
---@field selectedResolution integer
---@field selectedDisplayMode integer
---@field vsync boolean
---@field fpsLimit integer
---@field antialias integer
---@field shadowCascadeLevel integer
---@field shadowResolution integer
---@field renderDistance integer
---@field lowLatency boolean
---@field lineWidth number
---@field masterVolume number
---@field musicVolume number
---@field effectsVolume number
---@field muteAll boolean
---@field remasteredMusic boolean
---@field fov number
---@field followY integer
---@field followZ integer
---@field smoothFov boolean
---@field keyBindings { [integer|number]: KeyBindingData }
---@field distantOutlineBehavior integer

SettingsSnapshot = {}


---Creates a new SettingsSnapshot
---@return SettingsSnapshot
function SettingsSnapshot.new() end

---@class KeyBindingData
---@field action string
---@field displayName string
---@field keyCode integer

KeyBindingData = {}


---Creates a new KeyBindingData
---@return KeyBindingData
function KeyBindingData.new() end

---@class AvailableOptions
---@field renderers { [integer|number]: string }
---@field resolutions { [integer|number]: string }
---@field displayModes { [integer|number]: string }
---@field antialiasModes { [integer|number]: string }
---@field shadowCascadeLevels { [integer|number]: string }
---@field shadowResolutions { [integer|number]: string }
---@field renderDistanceNames { [integer|number]: string }

AvailableOptions = {}


---@class CounterData
---@field value integer

CounterData = {}


