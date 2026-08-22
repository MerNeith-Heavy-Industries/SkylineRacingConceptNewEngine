local R = require('./react/types')
local React = require('./react')

local function render(node: R.VNode)
    React.setHostConfig(UiLib)

    local root = UiLib.createRoot()
    React.render(node, root)

    UiLib.setActiveRoot(root)
end

return {render = render}