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
local t = require('./types')

local function copyStyle(dst: { [string]: any }, src: { [string]: any }): ()
    for k, v in pairs(src) do
        dst[k] = v
    end
end

type function rawget(tbl, key)
    local function find(t): type?
        if t:is("table") then
            for k, v in t:properties() do
                if k == key then
                    if v.read ~= v.write then
                        error("mismatched read/write types found for the property")
                    end
                    return v.read
                end
            end
            return nil
        elseif t:is("intersection") then
            local results = {}
            for _, part in t:components() do
                local result = find(part)
                if result ~= nil then
                    table.insert(results, result)
                end
            end
            if #results == 0 then
                return nil
            elseif #results == 1 then
                return results[1]
            else
                return types.intersectionof(table.unpack(results))
            end
        elseif t:is("union") then
            local results = {}
            for _, part in t:components() do
                local result = find(part)
                if result == nil then
                    return nil
                end
                table.insert(results, result)
            end
            if #results == 0 then
                return nil
            elseif #results == 1 then
                return results[1]
            else
                return types.unionof(table.unpack(results))
            end
        end
        return nil
    end

    local result = find(tbl)
    if result == nil then
        error("key not found!")
    end
    return result
end

type Style<T> = rawget<T, 'style'>

type function WithVariants(styleT)
    local result = types.newtable()

    local function collect(t)
        if t:is("table") then
            for k, v in t:properties() do
                result:setproperty(k, v.read)
            end
        elseif t:is("union") then
            for _, part in t:components() do
                collect(part)
            end
        elseif t:is("intersection") then
            for _, part in t:components() do
                collect(part)
            end
        end
        -- silently ignore nil / other branches (e.g. from an optional union)
    end

    collect(styleT)

    local optionalStyles = types.optional(result) -- based on the flat table, not raw styleT
    result:setproperty(types.singleton("hover"), optionalStyles)
    result:setproperty(types.singleton("focus"), optionalStyles)

    result:setindexer(types.string, types.never) -- reject unlisted string keys

    return result
end

type StyledInput<P> = WithVariants<Style<P>>

local function styled<P>(tag: (P) -> t.NodeDescriptor, name: string?): (StyledInput<P>) -> ((P) -> t.NodeDescriptor)
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

        local component = function(props: P?)
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
            } :: t.NodeDescriptor
        end

        -- Tag the component with a readable name so it shows up in the devtools tree.
        -- Styled closures are anonymous to debug.info (they'd otherwise all collapse to the
        -- styled.luau source line); prefix with the capitalized base tag for readability
        -- (e.g. styled('view') -> "StyledView").
        local tagName = name
        if tagName and #tagName > 0 then
            tagName = tagName:sub(1, 1):upper() .. tagName:sub(2)
            Sx.debug.setStyledName(component, tagName)
        end

        Sx.debug.setCallSite(component, debug.getinfo(2))

        return component
    end
end

return styled
