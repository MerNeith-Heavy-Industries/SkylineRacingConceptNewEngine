-- Sx hyperscript builder. Curried like the existing preact `x`:
--
--     x("view"){ style = s, x("text"){ "hi" } }
--     x(MyComponent){ name = "x", someSignalFn }
--
-- A call returns a plain descriptor table:
--     { type = <string|component fn|flow marker>, props = {named...}, children = {n...} }
-- where `children` holds the positional (numeric-key) entries — plain values (mounted
-- statically), functions (mounted reactively as accessors), or nested descriptors.

local types = require('./types')

local h = {}

local function x<P>(vtype: string | types.Component<P> | ((any) -> types.NodeDescriptor)): ((P) -> types.NodeDescriptor)
    return function(props)
        props = props or {}
        local named = {}
        local children = {}
        for k, v in pairs(props) do
            if type(k) == "number" then
                children[#children + 1] = v
            else
                named[k] = v
            end
        end
        return {
            type = vtype,
            props = named,
            children = children,
        }
    end
end

h.x = x

return h
