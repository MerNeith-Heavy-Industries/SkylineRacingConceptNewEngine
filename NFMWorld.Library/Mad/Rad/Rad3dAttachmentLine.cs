using MessagePack;

namespace NFMWorldLibrary.Rad;

[MessagePackObject]
public readonly record struct Rad3dAttachmentLine([property: Key(0)] AttachmentLineDirection Direction, [property: Key(1)] fix64 Offset);