// Platform options matching server-side enum
export enum Platforms {
  Desktop = 1,
  Tablet = 2,
  Mobile = 4,
  App = 8,
  Custom1 = 16,
  Custom2 = 32,
  Custom3 = 64,
  All = 127, // Desktop | Mobile | Tablet | App | Custom1 | Custom2 | Custom3
}

// File information with metadata for UI
export interface FileContainer {
  name: string;
  size: number;
  path: string;
  platform: string;
  isNew: boolean;
  file?: File;
  isMarkedForDeletion?: boolean;
}

export type FileContainerCollection = {
  platformFiles: { [key: string]: FileContainer[] };
};
