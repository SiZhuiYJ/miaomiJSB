import { expose } from 'threads/worker';
import { Decrypt } from '@/decrypt/index';

expose(Decrypt);
