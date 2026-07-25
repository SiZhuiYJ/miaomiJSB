declare module 'browser-id3-writer' {
  export default class ID3Writer {
    constructor(buffer: Uint8Array | ArrayBuffer);
    setFrame(frameName: string, frameValue: unknown): this;
    addTag(): Uint8Array;
  }
}

declare module 'metaflac-js' {
  export default class MetaFlac {
    constructor(buffer: Uint8Array | ArrayBuffer);
    setTag(tag: string): void;
    removeTag(tag: string): void;
    importPictureFromBuffer(buffer: Uint8Array | ArrayBuffer): void;
    save(): Uint8Array;
  }
}

declare module '@unlock-music/joox-crypto' {
  interface JooxDecryptor {
    decryptFile(buffer: Uint8Array): Uint8Array[];
  }

  export default function jooxFactory(buffer: Uint8Array, uuid: string): JooxDecryptor | undefined;
}
