-- Sx host adapter — thin wrappers over the game's `UiLib` host (the Yoga DOM bridge
-- registered by LuaUiLibrary). Captures `_G.UiLib` at module load, like
-- preact-luau/src/ui.luau. Kept as the single choke point so the renderer (dom.luau)
-- never touches the global directly.

local UiLib = _G.UiLib

local Host = {}

Host.defer = UiLib.defer

function Host.createRoot()
    return UiLib.createRoot()
end

function Host.createInstance(vtype, props)
    return UiLib.createInstance(vtype, props)
end

function Host.createTextInstance(text)
    return UiLib.createTextInstance(text)
end

function Host.appendChild(parent, child)
    UiLib.appendChild(parent, child)
end

function Host.insertBefore(parent, child, before)
    UiLib.insertBefore(parent, child, before)
end

function Host.removeChild(parent, child)
    UiLib.removeChild(parent, child)
end

function Host.setProperty(instance, key, value)
    UiLib.setProperty(instance, key, value)
end

function Host.commitTextUpdate(textInstance, oldText, newText)
    UiLib.commitTextUpdate(textInstance, oldText, newText)
end

function Host.setActiveRoot(root)
    UiLib.setActiveRoot(root)
end

return Host
