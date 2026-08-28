-- Sx host adapter — thin wrappers over the game's `UiLib` host (the Yoga DOM bridge
-- registered by LuaUiLibrary). Captures `_G.UiLib` at module load, like
-- preact-luau/src/ui.luau. Kept as the single choke point so the renderer (dom.luau)
-- never touches the global directly.

local UiLib = UiLib

local Host = {}

-- Host node / root handle: opaque host objects owned by UiLib/Yoga.
export type HostNode = any

Host.defer = UiLib.defer

local createRoot = UiLib.createRoot
local createInstance = UiLib.createInstance
local createTextInstance = UiLib.createTextInstance
local appendChild = UiLib.appendChild
local insertBefore = UiLib.insertBefore
local removeChild = UiLib.removeChild
local setProperty = UiLib.setProperty
local commitTextUpdate = UiLib.commitTextUpdate
local setActiveRoot = UiLib.setActiveRoot

function Host.createRoot(): any
    return createRoot()
end

function Host.createInstance(vtype: string, props: { [string]: any }): any
    return createInstance(vtype, props)
end

function Host.createTextInstance(text: string): any
    return createTextInstance(text)
end

function Host.appendChild(parent: any, child: any): ()
    appendChild(parent, child)
end

function Host.insertBefore(parent: any, child: any, before: any): ()
    insertBefore(parent, child, before)
end

function Host.removeChild(parent: any, child: any): ()
    removeChild(parent, child)
end

function Host.setProperty(instance: any, key: string, value: any): ()
    setProperty(instance, key, value)
end

function Host.commitTextUpdate(textInstance: any, oldText: string, newText: string): ()
    commitTextUpdate(textInstance, oldText, newText)
end

function Host.setActiveRoot(root: any): ()
    setActiveRoot(root)
end

return Host
