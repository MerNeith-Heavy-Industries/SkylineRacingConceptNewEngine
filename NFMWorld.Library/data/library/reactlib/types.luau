
export type MarkerTable = { [string]: any }

export type Fiber = {
  --- element type (tag string, component function, marker table)
  type: string | (...any) -> ...any | MarkerTable,
  ---
  tag: "host"|"function"|"text"|"fragment"|"provider"|"consumer"|"root",
  --- element props (children array included)
  props: { [string]: any },
  --- reconciliation key
  key: any,
  --- host instance (host/text) or container (root)
  stateNode: any,
  --- parent fiber
  parent: Fiber?,
  --- first child fiber
  child: Fiber?,
  --- next sibling fiber
  sibling: Fiber?,
  --- the committed fiber this one was built from
  alternate: Fiber?,
  --- true if this node is inside a host node
  placement: boolean?,
  --- true if this node is inside an update scope
  update: boolean?,
  --- list of children that are being deleted (unmounted)
  deletions: Array<Fiber>?,
  --- list of hooks for this node
  hooks: Array<Hook>?,
  --- position among siblings (1-based)
  index: integer,
  --- provider / consumer fibers
  context: Context<any>,
  --- element waiting on a host root fiber
  pendingElement: VNode?,
}

export type Hook = {
  state: any,
  --- pending useState actions
  queue: Array<any>?,
  --- last dependency array
  deps: Array<any>?,
  effectFn: (() -> any)?,
  effectDirty: boolean?,
  destroy: (() -> nil)?,
  ref: { current: any }?,
  subscribe: (() -> nil)?,
  getSnapshot: (() -> any)?,
  unsubscribe: (() -> nil)?,
  isStateHook: boolean?,
}

export type Context<T> = {
  defaultValue: T,
  Provider: { __reactProvider: Context<T> },
  Consumer: { __reactConsumer: Context<T> },
}

--- Functions the host environment (e.g. an RmlUI DOM adapter) must provide.
export type HostConfig = {
  createInstance: ((string, { [string]: any }) -> any),
  createTextInstance: ((string) -> any),
  appendChild: ((any, any) -> nil),
  insertBefore: ((any, any, any) -> nil),
  removeChild: ((any, any) -> nil),
  setProperty: ((any, string, any) -> nil),
  commitTextUpdate: ((any, string, string) -> nil),
}

export type Root = {
  container: any,
  current: Fiber,
  pendingElement: VNode?,
  dirty: boolean,
  rendering: boolean,
}

export type VNodeProps = {
  --- normalized child list (absent on text VNodes)
  children: Array<VNode>?,
  --- ref object (`{ current = ... }`) or function
  ref: any?,
  --- reconciliation key
  key: any?,
  --- function payload of a vdom.FUNCTION VNode
  fn: (() -> any)?,
  [string]: any,
}

export type VNode = {
  type: string | (...any) -> ...any | MarkerTable, -- tag string, component function, or marker table
  props: VNodeProps,
  key: any?,
}

export type Component = (props: VNodeProps) -> (VNode | string | number | boolean | nil | Array<VNode>)
export type ElementType = string | Component | { [string]: any }
export type Child = VNode | string | number | boolean | nil | ((value: any) -> any)
export type Children = Child | {Child} | {VNode} | {string} | {number} | {boolean} | {nil} | {((value: any) -> any)}

return nil