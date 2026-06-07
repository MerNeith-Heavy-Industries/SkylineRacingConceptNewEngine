using MemoryPack;

namespace NFMWorldLibrary.Rad;

[MemoryPackable]
public readonly partial record struct Rad3dAttachmentLine([property: MemoryPackOrder(0)] AttachmentLineDirection Direction, [property: MemoryPackOrder(1)] fix64 Offset);