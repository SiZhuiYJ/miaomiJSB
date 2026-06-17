import { DecryptResult } from './entity';

export async function Decrypt(file: Blob, raw_filename: string, raw_ext: string): Promise<DecryptResult> {
  void file;
  void raw_filename;
  void raw_ext;
  throw new Error('JOOX decrypt is temporarily disabled because @unlock-music/joox-crypto is unavailable.');
}
