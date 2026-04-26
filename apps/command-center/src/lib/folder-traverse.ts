export interface QueuedFile {
  file: File;
  relativePath: string;
}

/**
 * Recursively walks a FileSystemEntry tree and accumulates files with
 * their preserved relative paths. The drag-drop API exposes folder uploads
 * via webkitGetAsEntry(), but only as a tree — this flattens it.
 */
export async function traverseEntry(
  entry: FileSystemEntry,
  prefix: string,
  collected: QueuedFile[],
): Promise<void> {
  if (entry.isFile) {
    const file = await new Promise<File>((resolve, reject) => {
      (entry as FileSystemFileEntry).file(resolve, reject);
    });
    collected.push({ file, relativePath: prefix ? `${prefix}/${file.name}` : file.name });
    return;
  }
  if (entry.isDirectory) {
    const dir = entry as FileSystemDirectoryEntry;
    const reader = dir.createReader();
    const entries = await new Promise<FileSystemEntry[]>((resolve, reject) => {
      reader.readEntries(resolve, reject);
    });
    await Promise.all(
      entries.map((child) => traverseEntry(child, prefix ? `${prefix}/${entry.name}` : entry.name, collected)),
    );
  }
}

/**
 * Convenience wrapper for drag-drop event data: walks every dropped folder/file
 * and falls back to flat FileList if webkitGetAsEntry() isn't available.
 */
export async function walkDataTransfer(dataTransfer: DataTransfer | null): Promise<QueuedFile[]> {
  if (!dataTransfer) return [];
  const items = dataTransfer.items;
  const collected: QueuedFile[] = [];
  if (!items) {
    return readFileList(dataTransfer.files);
  }
  const walkers: Promise<void>[] = [];
  for (let i = 0; i < items.length; i++) {
    const entry = (items[i] as DataTransferItem & { webkitGetAsEntry?: () => FileSystemEntry | null }).webkitGetAsEntry?.();
    if (entry) {
      walkers.push(traverseEntry(entry, '', collected));
    }
  }
  await Promise.all(walkers);
  if (collected.length === 0) {
    return readFileList(dataTransfer.files);
  }
  return collected;
}

/**
 * Builds QueuedFiles from a flat FileList. Honors webkitRelativePath when
 * available (folder picker via <input webkitdirectory>).
 */
export function readFileList(files: FileList | null): QueuedFile[] {
  if (!files) return [];
  const out: QueuedFile[] = [];
  for (let i = 0; i < files.length; i++) {
    const f = files.item(i);
    if (!f) continue;
    const relativePath = (f as File & { webkitRelativePath?: string }).webkitRelativePath || f.name;
    out.push({ file: f, relativePath });
  }
  return out;
}
