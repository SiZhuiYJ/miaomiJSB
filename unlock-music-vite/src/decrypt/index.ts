import type { DecryptResult, FileInfo } from '@/decrypt/entity';
import { storage } from '@/utils/storage';
import InMemoryStorage from '@/utils/storage/InMemoryStorage';

function splitFilename(filename: string): { name: string; ext: string } {
  const pos = filename.lastIndexOf('.');
  if (pos < 0) return { name: filename, ext: '' };
  return {
    name: filename.substring(0, pos),
    ext: filename.substring(pos + 1).toLowerCase(),
  };
}

export async function Decrypt(file: FileInfo, config: Record<string, any>): Promise<DecryptResult> {
  // Worker thread will fallback to in-memory storage.
  if (storage instanceof InMemoryStorage) {
    await storage.setAll(config);
  }

  const raw = splitFilename(file.name);
  let rt_data: DecryptResult;
  switch (raw.ext) {
    case 'mg3d': // Migu Wav
      rt_data = await (await import('@/decrypt/mg3d')).Decrypt(file.raw, raw.name);
      break;
    case 'ncm': // Netease Mp3/Flac
      rt_data = await (await import('@/decrypt/ncm')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'uc': // Netease Cache
      rt_data = await (await import('@/decrypt/ncmcache')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'kwm': // Kuwo Mp3/Flac
      rt_data = await (await import('@/decrypt/kwm')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'xm': // Xiami Wav/M4a/Mp3/Flac
    case 'wav': // Xiami/Raw Wav
    case 'mp3': // Xiami/Raw Mp3
    case 'flac': // Xiami/Raw Flac
    case 'm4a': // Xiami/Raw M4a
      rt_data = await (await import('@/decrypt/xm')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'ogg': // Raw Ogg
      rt_data = await (await import('@/decrypt/raw')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'tm0': // QQ Music IOS Mp3
    case 'tm3': // QQ Music IOS Mp3
      rt_data = await (await import('@/decrypt/raw')).Decrypt(file.raw, raw.name, 'mp3');
      break;
    case 'qmc0': //QQ Music Android Mp3
    case 'qmc3': //QQ Music Android Mp3
    case 'qmc2': //QQ Music Android Ogg
    case 'qmc4': //QQ Music Android Ogg
    case 'qmc6': //QQ Music Android Ogg
    case 'qmc8': //QQ Music Android Ogg
    case 'qmcflac': //QQ Music Android Flac
    case 'qmcogg': //QQ Music Android Ogg
    case 'tkm': //QQ Music Accompaniment M4a
    // Moo Music
    case 'bkcmp3':
    case 'bkcm4a':
    case 'bkcflac':
    case 'bkcwav':
    case 'bkcape':
    case 'bkcogg':
    case 'bkcwma':
    // QQ Music v2
    case 'mggl': //QQ Music Mac
    case 'mflac': //QQ Music New Flac
    case 'mflac0': //QQ Music New Flac
    case 'mflach': //QQ Music New Flac
    case 'mgg': //QQ Music New Ogg
    case 'mgg1': //QQ Music New Ogg
    case 'mgg0':
    case 'mmp4': // QMC MP4 Container w/ E-AC-3 JOC
    case '666c6163': //QQ Music Weiyun Flac
    case '6d7033': //QQ Music Weiyun Mp3
    case '6f6767': //QQ Music Weiyun Ogg
    case '6d3461': //QQ Music Weiyun M4a
    case '776176': //QQ Music Weiyun Wav
      rt_data = await (await import('@/decrypt/qmc')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'tm2': // QQ Music IOS M4a
    case 'tm6': // QQ Music IOS M4a
      rt_data = await (await import('@/decrypt/tm')).Decrypt(file.raw, raw.name);
      break;
    case 'cache': //QQ Music Cache
      rt_data = await (await import('@/decrypt/qmccache')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'vpr':
    case 'kgm':
    case 'kgma':
      rt_data = await (await import('@/decrypt/kgm')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'ofl_en':
      throw '当前临时环境缺少 Joox 解密依赖，暂不支持 ofl_en 文件';
    case 'x2m':
    case 'x3m':
      rt_data = await (await import('@/decrypt/ximalaya')).Decrypt(file.raw, raw.name, raw.ext);
      break;
    case 'mflach': //QQ Music New Flac
      throw '网页版无法解锁，请使用<a target="_blank" href="https://git.unlock-music.dev/um/cli">CLI版本</a>'
    default:
      throw '不支持此文件格式';
  }

  if (!rt_data.rawExt) rt_data.rawExt = raw.ext;
  if (!rt_data.rawFilename) rt_data.rawFilename = raw.name;
  console.log(rt_data);
  return rt_data;
}
