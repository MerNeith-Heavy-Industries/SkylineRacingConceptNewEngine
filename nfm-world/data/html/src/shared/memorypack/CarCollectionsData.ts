import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";
import { CarCollectionData } from "./CarCollectionData";

export class CarCollectionsData {
    collections: (CarCollectionData | null)[] | null;

    constructor() {
        this.collections = null;

    }

    static serialize(value: CarCollectionsData | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: CarCollectionsData | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(1);
        writer.writeArray(value.collections, (writer, x) => CarCollectionData.serializeCore(writer, x));

    }

    static serializeArray(value: (CarCollectionsData | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (CarCollectionsData | null)[] | null): void {
        writer.writeArray(value, (writer, x) => CarCollectionsData.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): CarCollectionsData | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): CarCollectionsData | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new CarCollectionsData();
        if (count == 1) {
            value.collections = reader.readArray(reader => CarCollectionData.deserializeCore(reader));

        }
        else if (count > 1) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.collections = reader.readArray(reader => CarCollectionData.deserializeCore(reader)); if (count == 1) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (CarCollectionsData | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (CarCollectionsData | null)[] | null {
        return reader.readArray(reader => CarCollectionsData.deserializeCore(reader));
    }
}
