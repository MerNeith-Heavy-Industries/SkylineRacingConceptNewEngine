import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class CounterData {
    value: number;

    constructor() {
        this.value = 0;

    }

    static serialize(value: CounterData | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: CounterData | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(1);
        writer.writeInt32(value.value);

    }

    static serializeArray(value: (CounterData | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (CounterData | null)[] | null): void {
        writer.writeArray(value, (writer, x) => CounterData.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): CounterData | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): CounterData | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new CounterData();
        if (count == 1) {
            value.value = reader.readInt32()!;

        }
        else if (count > 1) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.value = reader.readInt32()!; if (count == 1) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (CounterData | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (CounterData | null)[] | null {
        return reader.readArray(reader => CounterData.deserializeCore(reader));
    }
}
