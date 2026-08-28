--!strict
-- Sx styled — CSS-in-JS component wrapper (Sx port of library/styled.luau).
--
-- styled(tag)(baseStyle) -> component(props). The component renders a host element of
-- `tag` ("view"|"text") with the base style merged with the caller's `style` override,
-- plus a `hover` variant driven by a hover signal (reactive style prop -> a single
-- setProperty on hover enter/leave). Hover events are wired ONLY when a `hover` variant
-- exists (avoids signal/effect churn for static components).
--
-- Other props (name, onmousedown, ...) pass through to the host element; `key` is
-- dropped (Sx For/Index handle keying).

local Sx = require('./signals')
local types = require('./types')

local function copyStyle(dst: { [string]: any }, src: { [string]: any }): ()
    for k, v in pairs(src) do
        dst[k] = v
    end
end

local function styled<P>(tag: (P) -> types.NodeDescriptor): (baseStyle: any?) -> ((P) -> types.NodeDescriptor)
    return function(baseStyle: any?)
        baseStyle = baseStyle or {}
        local hasHover = baseStyle.hover ~= nil

        -- static base style (variant keys stripped) computed once per styled() call
        local staticStyle = {}
        for k, v in pairs(baseStyle) do
            if k ~= "hover" and k ~= "focus" and k ~= "active" then
                staticStyle[k] = v
            end
        end

        return function(props: P?)
            props = props or {}

            local named: { [string]: any } = {}
            for k, v in pairs(props) do
                if k ~= "style" and k ~= "children" and k ~= "key" then
                    named[k] = v
                end
            end

            local override = props.style
            if hasHover then
                local hover, setHover = Sx.createSignal(false)
                local merged = Sx.createMemo(function()
                    local s = {}
                    copyStyle(s, staticStyle)
                    if override ~= nil then
                        if type(override) == "function" then
                            copyStyle(s, override())
                        else
                            copyStyle(s, override)
                        end
                    end
                    if hover() then
                        copyStyle(s, baseStyle.hover)
                    end
                    return s
                end)

                named.style = merged

                local userEnter = named.onmouseenter
                local userLeave = named.onmouseleave
                named.onmouseenter = function(evt)
                    setHover(true)
                    if userEnter then
                        userEnter(evt)
                    end
                end
                named.onmouseleave = function(evt)
                    setHover(false)
                    if userLeave then
                        userLeave(evt)
                    end
                end
            else
                local s = {}
                copyStyle(s, staticStyle)
                if override ~= nil then
                    if type(override) == "function" then
                        copyStyle(s, override())
                    else
                        copyStyle(s, override)
                    end
                end
                named.style = s
            end

            return {
                type = tag,
                props = named,
                children = props.children or {},
            } :: types.NodeDescriptor
        end
    end
end

return styled
