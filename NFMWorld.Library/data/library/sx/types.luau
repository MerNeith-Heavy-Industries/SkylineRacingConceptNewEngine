export type NodeDescriptor = {
	type: any,
	props: { [string]: any },
	children: { any },
	-- devtools: "short_src:line" of the `x(...){...}` creation site. Optional — only set
	-- while devtools call-site capture is enabled.
	callSite: string?,
}

export type Component<P> = (P) -> NodeDescriptor

return nil