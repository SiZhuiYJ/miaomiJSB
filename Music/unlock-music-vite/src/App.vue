<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import {
  Check,
  Close,
  Delete,
  Download,
  Edit,
  FolderOpened,
  Refresh,
  Setting,
  UploadFilled,
  VideoPlay,
  WarningFilled,
} from '@element-plus/icons-vue';

import { Decrypt } from '@/decrypt';
import type { DecryptResult, FileInfo } from '@/decrypt/entity';
import { storage } from '@/utils/storage';
import { DownloadBlobMusic, FilenamePolicy, RemoveBlobMusic } from '@/utils/utils';

type Track = DecryptResult & {
  id: number;
  sourceName: string;
  sourceSize: number;
  createdAt: number;
};

type Failure = {
  id: number;
  name: string;
  message: string;
};

const policyOptions = [
  { key: FilenamePolicy.ArtistAndTitle, text: '歌手 - 歌曲名' },
  { key: FilenamePolicy.TitleOnly, text: '歌曲名' },
  { key: FilenamePolicy.TitleAndArtist, text: '歌曲名 - 歌手' },
  { key: FilenamePolicy.SameAsOriginal, text: '同源文件名' },
];

const fileInput = ref<HTMLInputElement | null>(null);
const tracks = ref<Track[]>([]);
const failures = ref<Failure[]>([]);
const filenamePolicy = ref<FilenamePolicy>(FilenamePolicy.ArtistAndTitle);
const isDragging = ref(false);
const processing = ref(false);
const progressDone = ref(0);
const progressTotal = ref(0);
const currentFile = ref('');
const activeTrackId = ref<number | null>(null);
const activeAudioSrc = ref('');
const settingsVisible = ref(false);
const editVisible = ref(false);
const editingTrackId = ref<number | null>(null);
const editCoverInput = ref<HTMLInputElement | null>(null);

const settingsForm = reactive({
  jooxUUID: '',
});

const editForm = reactive({
  title: '',
  artist: '',
  album: '',
  albumartist: '',
  genre: '',
  picture: '',
});

const progressPercent = computed(() => {
  if (!progressTotal.value) return 0;
  return Math.round((progressDone.value / progressTotal.value) * 100);
});

const statusText = computed(() => {
  if (processing.value) return `${progressDone.value} / ${progressTotal.value}`;
  if (tracks.value.length) return `${tracks.value.length} 首已解锁`;
  return '等待文件';
});

const activeTrack = computed(() => tracks.value.find((track) => track.id === activeTrackId.value));

function nextId() {
  return Date.now() + Math.floor(Math.random() * 100000);
}

function openFilePicker() {
  if (processing.value) return;
  fileInput.value?.click();
}

function handleNativeFiles(event: Event) {
  const target = event.target as HTMLInputElement;
  if (target.files?.length) {
    void processFiles(Array.from(target.files));
  }
  target.value = '';
}

function handleDrop(event: DragEvent) {
  isDragging.value = false;
  if (processing.value) return;
  if (event.dataTransfer?.files.length) {
    void processFiles(Array.from(event.dataTransfer.files));
  }
}

async function processFiles(files: File[]) {
  if (!files.length) return;
  if (processing.value) {
    ElMessage.warning('正在处理当前队列');
    return;
  }

  processing.value = true;
  progressDone.value = 0;
  progressTotal.value = files.length;
  failures.value = [];

  const config = await storage.getAll();
  for (const file of files) {
    currentFile.value = file.name;
    try {
      const result = await Decrypt(toFileInfo(file), config);
      tracks.value.unshift(toTrack(result, file));
      ElMessage.success(`${file.name} 解锁完成`);
    } catch (error) {
      failures.value.unshift({
        id: nextId(),
        name: file.name,
        message: getErrorMessage(error),
      });
      ElMessage.error(`${file.name} 解锁失败`);
    } finally {
      progressDone.value += 1;
    }
  }

  currentFile.value = '';
  processing.value = false;
}

function toFileInfo(file: File): FileInfo {
  return {
    status: 'ready',
    name: file.name,
    size: file.size,
    percentage: 0,
    uid: nextId(),
    raw: file,
  };
}

function toTrack(result: DecryptResult, file: File): Track {
  const dot = file.name.lastIndexOf('.');
  const sourceName = dot > 0 ? file.name.slice(0, dot) : file.name;
  const sourceExt = dot > 0 ? file.name.slice(dot + 1).toLowerCase() : result.ext;

  return {
    ...result,
    id: nextId(),
    title: result.title || result.rawFilename || sourceName,
    artist: result.artist || '',
    album: result.album || '',
    rawFilename: result.rawFilename || sourceName,
    rawExt: result.rawExt || sourceExt,
    sourceName: file.name,
    sourceSize: file.size,
    createdAt: Date.now(),
  };
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) return error.message;
  if (typeof error === 'string') return error;
  return '未知错误';
}

function formatSize(size: number) {
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / 1024 / 1024).toFixed(1)} MB`;
}

function playTrack(track: Track) {
  activeTrackId.value = track.id;
  activeAudioSrc.value = track.file;
}

function downloadTrack(track: Track) {
  DownloadBlobMusic(track, filenamePolicy.value);
}

function deleteTrack(track: Track) {
  RemoveBlobMusic(track);
  tracks.value = tracks.value.filter((item) => item.id !== track.id);
  if (activeTrackId.value === track.id) {
    activeTrackId.value = null;
    activeAudioSrc.value = '';
  }
}

function clearTracks() {
  tracks.value.forEach(RemoveBlobMusic);
  tracks.value = [];
  activeTrackId.value = null;
  activeAudioSrc.value = '';
}

async function openSettings() {
  settingsForm.jooxUUID = await storage.loadJooxUUID('');
  settingsVisible.value = true;
}

async function saveSettings() {
  const uuid = settingsForm.jooxUUID.trim();
  if (uuid && !/^[\da-fA-F]{32}$/.test(uuid)) {
    ElMessage.warning('JOOX UUID 需要是 32 位十六进制字符');
    return;
  }

  await storage.saveJooxUUID(uuid);
  settingsVisible.value = false;
  ElMessage.success('设置已保存');
}

function openEditor(track: Track) {
  editingTrackId.value = track.id;
  editForm.title = track.title || '';
  editForm.artist = track.artist || '';
  editForm.album = track.album || '';
  editForm.albumartist = '';
  editForm.genre = '';
  editForm.picture = track.picture || '';
  editVisible.value = true;
}

function chooseEditCover() {
  editCoverInput.value?.click();
}

function handleEditCover(event: Event) {
  const target = event.target as HTMLInputElement;
  const file = target.files?.[0];
  if (!file) return;

  if (editForm.picture?.startsWith('blob:')) {
    URL.revokeObjectURL(editForm.picture);
  }
  editForm.picture = URL.createObjectURL(file);
  target.value = '';
}

function saveEdit() {
  const track = tracks.value.find((item) => item.id === editingTrackId.value);
  if (!track) return;

  if (track.picture?.startsWith('blob:') && track.picture !== editForm.picture) {
    URL.revokeObjectURL(track.picture);
  }

  track.title = editForm.title.trim() || track.title;
  track.artist = editForm.artist.trim();
  track.album = editForm.album.trim();
  track.picture = editForm.picture;
  editVisible.value = false;
  ElMessage.success('信息已更新');
}

onBeforeUnmount(() => {
  tracks.value.forEach(RemoveBlobMusic);
  if (editForm.picture?.startsWith('blob:')) {
    URL.revokeObjectURL(editForm.picture);
  }
});
</script>

<template>
  <div class="app-shell">
    <header class="topbar">
      <div class="brand-block">
        <div class="brand-mark">UM</div>
        <div>
          <h1>Unlock Music</h1>
          <p>本地音乐解锁工具</p>
        </div>
      </div>

      <div class="top-actions">
        <el-select v-model="filenamePolicy" class="policy-select" size="large" aria-label="下载命名规则">
          <el-option
            v-for="option in policyOptions"
            :key="option.key"
            :label="option.text"
            :value="option.key"
          />
        </el-select>
        <el-button :icon="Setting" circle size="large" aria-label="设置" @click="openSettings" />
      </div>
    </header>

    <main class="workspace">
      <section class="upload-band">
        <input ref="fileInput" class="hidden-input" multiple type="file" @change="handleNativeFiles" />
        <button
          class="dropzone"
          :class="{ 'is-dragging': isDragging, 'is-disabled': processing }"
          type="button"
          @click="openFilePicker"
          @dragenter.prevent="isDragging = true"
          @dragover.prevent
          @dragleave.prevent="isDragging = false"
          @drop.prevent="handleDrop"
        >
          <el-icon><UploadFilled /></el-icon>
          <span class="drop-title">选择或拖放音乐文件</span>
          <span class="drop-subtitle">ncm / qmc / kgm / kwm / xm / mflac / cache / mp3 / flac</span>
        </button>

        <div class="status-row">
          <div>
            <strong>{{ statusText }}</strong>
            <span v-if="currentFile">{{ currentFile }}</span>
          </div>
          <el-progress
            v-if="processing || progressTotal"
            :percentage="progressPercent"
            :stroke-width="10"
            class="progress"
          />
        </div>
      </section>

      <section v-if="activeAudioSrc" class="player-band">
        <div>
          <span>正在播放</span>
          <strong>{{ activeTrack?.title || 'Untitled' }}</strong>
        </div>
        <audio :src="activeAudioSrc" controls />
      </section>

      <section class="content-grid">
        <div class="results-panel">
          <div class="section-heading">
            <div>
              <h2>解锁结果</h2>
              <span>{{ tracks.length }} 个文件</span>
            </div>
            <el-button
              :disabled="!tracks.length"
              :icon="Refresh"
              plain
              size="small"
              @click="clearTracks"
            >
              清空
            </el-button>
          </div>

          <div v-if="tracks.length" class="table-wrap">
            <el-table :data="tracks" row-key="id">
              <el-table-column label="封面" width="96">
                <template #default="{ row }">
                  <div class="cover-cell">
                    <img v-if="row.picture" :alt="row.title" :src="row.picture" />
                    <el-icon v-else><FolderOpened /></el-icon>
                  </div>
                </template>
              </el-table-column>
              <el-table-column label="歌曲" min-width="180">
                <template #default="{ row }">
                  <div class="song-cell">
                    <strong>{{ row.title || 'Untitled' }}</strong>
                    <span>{{ row.sourceName }}</span>
                  </div>
                </template>
              </el-table-column>
              <el-table-column label="歌手" min-width="140">
                <template #default="{ row }">{{ row.artist || 'Unknown Artist' }}</template>
              </el-table-column>
              <el-table-column label="专辑" min-width="140">
                <template #default="{ row }">{{ row.album || '-' }}</template>
              </el-table-column>
              <el-table-column label="格式" width="88">
                <template #default="{ row }">
                  <el-tag effect="plain">{{ row.ext }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="大小" width="104">
                <template #default="{ row }">{{ formatSize(row.sourceSize) }}</template>
              </el-table-column>
              <el-table-column label="操作" fixed="right" width="190">
                <template #default="{ row }">
                  <div class="action-row">
                    <el-tooltip content="播放" placement="top">
                      <el-button :icon="VideoPlay" circle size="small" type="success" @click="playTrack(row)" />
                    </el-tooltip>
                    <el-tooltip content="下载" placement="top">
                      <el-button :icon="Download" circle size="small" type="primary" @click="downloadTrack(row)" />
                    </el-tooltip>
                    <el-tooltip content="编辑" placement="top">
                      <el-button :icon="Edit" circle size="small" @click="openEditor(row)" />
                    </el-tooltip>
                    <el-tooltip content="移除" placement="top">
                      <el-button :icon="Delete" circle size="small" type="danger" @click="deleteTrack(row)" />
                    </el-tooltip>
                  </div>
                </template>
              </el-table-column>
            </el-table>
          </div>
          <el-empty v-else description="暂无文件" />
        </div>

        <aside class="side-panel">
          <div class="section-heading compact">
            <div>
              <h2>处理状态</h2>
              <span>{{ failures.length }} 个错误</span>
            </div>
          </div>

          <div v-if="failures.length" class="error-list">
            <div v-for="item in failures" :key="item.id" class="error-item">
              <el-icon><WarningFilled /></el-icon>
              <div>
                <strong>{{ item.name }}</strong>
                <span>{{ item.message }}</span>
              </div>
            </div>
          </div>
          <div v-else class="quiet-state">
            <el-icon><Check /></el-icon>
            <span>未发现错误</span>
          </div>
        </aside>
      </section>
    </main>

    <footer class="app-footer">
      <div>
        <!--如果进行二次开发，此行版权信息不得移除且应明显地标注于页面上-->
        <span>Copyright &copy; 2019 - {{ new Date().getFullYear() }} MengYX</span>
        音乐解锁使用
        <a href="https://git.unlock-music.dev/um/web/src/branch/main/LICENSE" target="_blank" rel="noreferrer">
          MIT许可协议
        </a>
        开放源代码
      </div>
    </footer>

    <el-dialog v-model="settingsVisible" title="解密设置" width="420px">
      <el-form label-position="top">
        <el-form-item label="JOOX UUID">
          <el-input v-model="settingsForm.jooxUUID" clearable maxlength="32" placeholder="32 位十六进制字符" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :icon="Close" @click="settingsVisible = false">取消</el-button>
        <el-button :icon="Check" type="primary" @click="saveSettings">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="editVisible" title="编辑信息" width="520px">
      <input ref="editCoverInput" accept="image/*" class="hidden-input" type="file" @change="handleEditCover" />
      <div class="edit-layout">
        <button class="edit-cover" type="button" @click="chooseEditCover">
          <img v-if="editForm.picture" :src="editForm.picture" alt="" />
          <el-icon v-else><FolderOpened /></el-icon>
        </button>
        <el-form label-position="top">
          <el-form-item label="歌曲">
            <el-input v-model="editForm.title" />
          </el-form-item>
          <el-form-item label="歌手">
            <el-input v-model="editForm.artist" />
          </el-form-item>
          <el-form-item label="专辑">
            <el-input v-model="editForm.album" />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button :icon="Close" @click="editVisible = false">取消</el-button>
        <el-button :icon="Check" type="primary" @click="saveEdit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
  background:
    linear-gradient(180deg, #f7f8fa 0%, #eef2f5 100%);
  color: #20242a;
}

.topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  padding: 20px clamp(16px, 4vw, 48px);
  border-bottom: 1px solid #d8dde5;
  background: rgba(255, 255, 255, 0.88);
  backdrop-filter: blur(14px);
  position: sticky;
  top: 0;
  z-index: 10;
}

.brand-block {
  display: flex;
  align-items: center;
  gap: 14px;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 44px;
  height: 44px;
  border-radius: 8px;
  background: #111827;
  color: #f9fafb;
  font-weight: 700;
}

h1,
h2,
p {
  margin: 0;
}

h1 {
  font-size: 24px;
  line-height: 1.1;
  font-weight: 750;
  letter-spacing: 0;
}

.brand-block p,
.section-heading span,
.song-cell span,
.status-row span,
.quiet-state,
.error-item span,
.drop-subtitle {
  color: #667085;
}

.top-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.policy-select {
  width: 180px;
}

.workspace {
  width: min(1440px, 100%);
  margin: 0 auto;
  padding: 22px clamp(14px, 3vw, 36px) 28px;
}

.upload-band,
.player-band,
.results-panel,
.side-panel {
  border: 1px solid #d7dde6;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 12px 28px rgba(16, 24, 40, 0.06);
}

.upload-band {
  padding: 16px;
}

.hidden-input {
  display: none;
}

.dropzone {
  width: 100%;
  min-height: 170px;
  display: grid;
  place-items: center;
  align-content: center;
  gap: 8px;
  border: 1px dashed #9aa6b8;
  border-radius: 8px;
  background: #f9fafb;
  color: #20242a;
  cursor: pointer;
  transition:
    border-color 0.2s ease,
    background 0.2s ease,
    transform 0.2s ease;
}

.dropzone .el-icon {
  font-size: 34px;
  color: #1f7a4d;
}

.drop-title {
  font-size: 20px;
  font-weight: 700;
}

.dropzone:hover,
.dropzone.is-dragging {
  border-color: #1f7a4d;
  background: #f1f8f4;
}

.dropzone.is-disabled {
  cursor: wait;
  opacity: 0.72;
}

.status-row {
  display: grid;
  grid-template-columns: minmax(180px, 320px) 1fr;
  gap: 18px;
  align-items: center;
  margin-top: 14px;
}

.status-row div {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.status-row strong,
.song-cell strong,
.error-item strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.progress {
  min-width: 0;
}

.player-band {
  display: grid;
  grid-template-columns: minmax(180px, 280px) 1fr;
  gap: 16px;
  align-items: center;
  margin-top: 18px;
  padding: 14px 16px;
}

.player-band div {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.player-band span {
  color: #667085;
  font-size: 13px;
}

.player-band audio {
  width: 100%;
}

.content-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 320px;
  gap: 18px;
  margin-top: 18px;
}

.results-panel,
.side-panel {
  min-width: 0;
  padding: 16px;
}

.section-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 14px;
}

.section-heading.compact {
  margin-bottom: 10px;
}

.section-heading h2 {
  font-size: 18px;
  line-height: 1.2;
  font-weight: 700;
}

.section-heading div {
  display: flex;
  flex-direction: column;
}

.table-wrap {
  overflow-x: auto;
}

.cover-cell {
  display: grid;
  place-items: center;
  width: 56px;
  height: 56px;
  overflow: hidden;
  border: 1px solid #d7dde6;
  border-radius: 8px;
  background: #f2f4f7;
}

.cover-cell img,
.edit-cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.cover-cell .el-icon,
.edit-cover .el-icon,
.quiet-state .el-icon {
  color: #667085;
  font-size: 22px;
}

.song-cell {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.action-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.error-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.error-item {
  display: grid;
  grid-template-columns: 20px minmax(0, 1fr);
  gap: 10px;
  align-items: start;
  padding: 10px;
  border-radius: 8px;
  background: #fff7ed;
  color: #9a3412;
}

.error-item div {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.quiet-state {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 80px;
}

.edit-layout {
  display: grid;
  grid-template-columns: 132px minmax(0, 1fr);
  gap: 18px;
}

.edit-cover {
  width: 132px;
  height: 132px;
  display: grid;
  place-items: center;
  overflow: hidden;
  border: 1px solid #d7dde6;
  border-radius: 8px;
  background: #f2f4f7;
  cursor: pointer;
}

.app-footer {
  width: min(1440px, 100%);
  margin: 0 auto;
  padding: 0 clamp(14px, 3vw, 36px) 32px;
  color: #667085;
  font-size: 13px;
  line-height: 1.8;
  text-align: center;
}

.app-footer a {
  color: #1f7a4d;
  text-decoration: none;
}

.app-footer a:hover {
  text-decoration: underline;
}

@media (max-width: 900px) {
  .topbar,
  .top-actions,
  .player-band,
  .content-grid,
  .status-row,
  .edit-layout {
    grid-template-columns: 1fr;
  }

  .topbar {
    align-items: stretch;
    display: grid;
  }

  .top-actions {
    display: grid;
  }

  .policy-select {
    width: 100%;
  }
}

@media (max-width: 560px) {
  .brand-mark {
    width: 40px;
    height: 40px;
  }

  h1 {
    font-size: 21px;
  }

  .dropzone {
    min-height: 148px;
  }

  .drop-title {
    font-size: 18px;
  }

  .drop-subtitle {
    max-width: 260px;
  }
}
</style>
