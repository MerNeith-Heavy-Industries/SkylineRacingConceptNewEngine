export type NodeDescriptor = {
	type: any,
	props: { [string]: any },
	children: { any },
}

export type Component<P> = (P) -> NodeDescriptor

return nil