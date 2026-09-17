import { ChangeEvent, useCallback, useEffect, useRef, useState } from "react";
import { Boxes, LoaderCircle, Tags, Trash2 } from "lucide-react";
import { api } from "../../api";
import { ClassificationDropdown } from "../../components/ClassificationDropdown";
import { hasDesktopVideoPicker, openDesktopDirectory, selectDesktopSourceVideos } from "../../desktopBridge";
import { usePersistentOperation } from "../../hooks/usePersistentOperation";
import type { ClassifiedMaterial, Product, ProductCategory, SourceVideo } from "../../types";

export function Materials({ products, act }: { products: Product[]; act: (work: () => Promise<unknown>, success: string) => Promise<boolean> }) {
  const desktopPicker = import.meta.env.VITE_DESKTOP_MODE === "1" && hasDesktopVideoPicker();
  const activeProducts = products.filter(item => item.status === "active");
  const [productCategoryInput, setProductCategoryInput] = useState(""); const [productInput, setProductInput] = useState("");
  const [productCategories, setProductCategories] = useState<ProductCategory[]>([]);
  const [sourceDir, setSourceDir] = useState(""); const [videos, setVideos] = useState<SourceVideo[]>([]);
  const [selectedTags, setSelectedTags] = useState<Record<string, string[]>>({}); const [selectedVideo, setSelectedVideo] = useState("");
  const [tagEditor, setTagEditor] = useState<SourceVideo | null>(null); const [tagInput, setTagInput] = useState("");
  const [selecting, setSelecting] = useState(false); const [deletingVideo, setDeletingVideo] = useState("");
  const videoInputRef = useRef<HTMLInputElement>(null);
  const [classificationResult, setClassificationResult] = useState<ClassifiedMaterial[]>([]);
  const [classificationMessage, setClassificationMessage] = useState("");
  const classificationOperation = usePersistentOperation("material_classification", state => {
    setClassificationMessage(state.status === "completed" ? (state.detail || "原视频归类已完成。") : `原视频归类未完成：${state.detail}`);
  });
  const loadProductCategories = useCallback(async () => {
    setProductCategories(await api<ProductCategory[]>("/products/categories"));
  }, []);
  useEffect(() => { loadProductCategories().catch(() => setProductCategories([])); }, [loadProductCategories]);

  async function uploadVideos(event: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files || []);
    if (!files.length) return;
    setSelecting(true);
    try {
      const form = new FormData(); files.forEach(file => form.append("files", file));
      await act(() => api<{ path: string; videos: SourceVideo[] }>("/human/source-videos/upload", { method: "POST", body: form }).then(result => {
        setSourceDir(result.path); setVideos(result.videos); setSelectedTags({}); setSelectedVideo(""); return result;
      }), `已上传 ${files.length} 个视频`);
    } finally { setSelecting(false); }
  }
  async function chooseDesktopVideos() {
    setSelecting(true);
    try {
      const result = await selectDesktopSourceVideos();
      if (!result || !result.videos.length) return;
      setSourceDir(result.sourceDir); setVideos(result.videos); setSelectedTags({}); setSelectedVideo("");
    } catch (reason) { await act(() => Promise.reject(reason), ""); }
    finally { setSelecting(false); }
  }
  async function deleteVideo(video: SourceVideo) {
    if (deletingVideo) return;
    setDeletingVideo(video.path);
    try {
      const deleted = desktopPicker || await act(() => api(`/human/source-videos?path=${encodeURIComponent(video.path)}`, { method: "DELETE" }), "已删除待归类视频");
      if (!deleted) return;
      setVideos(value => value.filter(item => item.path !== video.path));
      setSelectedVideo(value => value === video.path ? "" : value);
      setSelectedTags(value => { const next = { ...value }; delete next[video.path]; return next; });
      if (videos.length === 1) { setSourceDir(""); if (videoInputRef.current) videoInputRef.current.value = ""; }
    } finally { setDeletingVideo(""); }
  }
  function editVideoTags(video: SourceVideo) {
    setSelectedVideo(video.path); setTagEditor(video); setTagInput((selectedTags[video.path] ?? []).join("，"));
  }
  const parsedTagInput = Array.from(new Set(tagInput.split(/[,，]/).map(value => value.trim().replace(/\s+/g, " ")).filter(Boolean)));
  const tagInputError = parsedTagInput.length > 30 ? "每个视频最多设置 30 个标签" : parsedTagInput.find(value => value.length > 80) ? "单个标签不能超过 80 个字符" : "";
  function saveVideoTags() {
    if (!tagEditor || tagInputError) return;
    setSelectedTags(value => ({ ...value, [tagEditor.path]: parsedTagInput })); setTagEditor(null);
  }

  const ready = Boolean(productCategoryInput.trim() && productInput.trim() && videos.length > 0 && videos.every(video => (selectedTags[video.path] ?? []).length > 0));
  async function classify() {
    const operationId = classificationOperation.begin();
    if (!operationId) return;
    let completed = false; let movedAssets: ClassifiedMaterial[] = [];
    await act(() => api<{ assets: ClassifiedMaterial[] }>("/human/material-classifications", {
      method: "POST", headers: { "X-Operation-Id": operationId }, body: JSON.stringify({
        product_category: productCategoryInput.trim(), product_name: productInput.trim(), source_dir: sourceDir,
        items: videos.map(video => ({ source_path: video.path, tags: selectedTags[video.path] ?? [] })),
      }),
    }).then(value => { movedAssets = value.assets; completed = true; return value; }), "");
    classificationOperation.clear(operationId);
    if (completed) {
      setClassificationMessage(""); setClassificationResult(movedAssets); setVideos([]); setSelectedTags({}); setSelectedVideo("");
      if (videoInputRef.current) videoInputRef.current.value = "";
      await loadProductCategories().catch(() => undefined);
    }
  }

  const selectedVideoItem = videos.find(item => item.path === selectedVideo);
  const completedVideoCount = videos.filter(video => (selectedTags[video.path] ?? []).length > 0).length;
  const classificationDirectory = classificationResult[0]?.source_path.replace(/[\\/][^\\/]+$/, "") ?? "";
  return <section className="human-page material-classification-page">
    {classificationMessage && <div className="human-note classification-result-note full" role="status"><Boxes /><div><b>归类操作状态</b><span>{classificationMessage}</span></div></div>}
    {classificationResult.length > 0 && <div className="human-note classification-result-note full"><Boxes /><div><b>本次归类成功</b><span>{classificationResult[0].product_name} · 已移动并重命名 {classificationResult.length} 个原视频</span><small title={classificationDirectory}>{classificationDirectory}</small></div>{desktopPicker && <button type="button" className="human-secondary" onClick={() => openDesktopDirectory(classificationDirectory)}>打开归类目录</button>}</div>}
    <div className="human-card full"><div className="human-card-title"><div><h2>选择产品与视频</h2><p>产品分类和产品名称可以直接输入；成功归类后自动保留为历史。</p></div></div>
      <div className="classification-master-inputs">
        <label>产品分类<div className="classification-history-field"><ClassificationDropdown value={productCategoryInput} options={productCategories} onChange={setProductCategoryInput} onSelect={item => setProductCategoryInput(item.name)} placeholder="输入产品分类，或选择最近记录" /></div><small>展开显示最近 5 次输入；输入文字可模糊查询历史</small></label>
        <label>产品名称<div className="classification-history-field"><ClassificationDropdown value={productInput} options={activeProducts} onChange={setProductInput} onSelect={item => { setProductInput(item.name); if (item.category_name && item.category_name !== "未分类") setProductCategoryInput(item.category_name); }} placeholder="输入产品名称，或选择最近记录" /></div><small>展开显示最近 5 次输入；输入文字可模糊查询历史</small></label>
        <label>选择视频{desktopPicker ? <div className="desktop-file-picker"><button type="button" className="human-secondary" disabled={selecting} onClick={() => void chooseDesktopVideos()}>{selecting ? "选择中…" : "选择文件"}</button><span>{videos.length ? `已选择 ${videos.length} 个文件` : "未选择文件"}</span></div> : <input ref={videoInputRef} type="file" accept="video/*,.mp4,.mov,.m4v,.avi,.mkv,.webm" multiple disabled={selecting} onChange={uploadVideos} />}<small>{sourceDir ? `归类目录将创建在：${sourceDir}` : "从同一原始目录多选视频，归类文件夹将创建在该目录内"}</small></label>
      </div>
    </div>
    <div className="human-card full"><div className="human-card-title"><h2>逐个视频打标签</h2><span>像背景音乐一样自由输入多个标签，使用逗号分隔</span></div>
      <div className="classification-toolbar"><b>{selectedVideoItem ? `当前视频：${selectedVideoItem.name}` : "从列表中选择视频后设置标签"}</b><span>已完成 {completedVideoCount}/{videos.length}</span></div>
      {videos.length > 0 && <div className="classification-video-list">{videos.map(video => <article key={video.path} className={selectedVideo === video.path ? "selected" : ""} onClick={() => setSelectedVideo(video.path)}><input type="radio" name="classification-video" aria-label={`选择 ${video.name}`} checked={selectedVideo === video.path} onChange={() => setSelectedVideo(video.path)} onClick={event => event.stopPropagation()} /><div><b>{video.name}</b><small>{video.relative_path}</small><div className="video-applied-tags">{(selectedTags[video.path] ?? []).map(tag => <span key={tag}>{tag}</span>)}{!(selectedTags[video.path] ?? []).length && <em>未打标签</em>}</div></div><div className="classification-video-actions"><button type="button" className="human-secondary" onClick={event => { event.stopPropagation(); editVideoTags(video); }}><Tags />标签</button><button type="button" className="human-danger classification-video-delete" disabled={Boolean(deletingVideo)} aria-label={`删除 ${video.name}`} onClick={event => { event.stopPropagation(); void deleteVideo(video); }}>{deletingVideo === video.path ? <LoaderCircle className="spin" /> : <Trash2 />}{deletingVideo === video.path ? "删除中" : "删除"}</button></div></article>)}</div>}
      <button type="button" className="human-wide" disabled={!ready || classificationOperation.busy} onClick={classify}>{classificationOperation.busy && <LoaderCircle className="spin" />}{classificationOperation.busy ? "正在校验、移动并重命名原视频…" : `确认归类并移动 ${videos.length || ""} 个原视频`}</button>
      {classificationOperation.busy && <div className="copy-generation-progress" role="status" aria-live="polite"><LoaderCircle className="spin" /><div><b>原视频归类正在执行</b><span>刷新页面后仍会保持此状态，完成前请勿重复提交或移动这些文件。</span></div></div>}
      {tagEditor && <div className="confirm-dialog-backdrop" role="presentation"><section className="confirm-dialog music-tag-dialog classification-tag-dialog" role="dialog" aria-modal="true" aria-labelledby="classification-tag-title"><div><b id="classification-tag-title">设置视频标签</b><span>“{tagEditor.name}”</span><label>视频标签<input autoFocus value={tagInput} onChange={event => setTagInput(event.target.value)} onKeyDown={event => { if (event.key === "Enter") { event.preventDefault(); saveVideoTags(); } }} placeholder="例如：正面，近景，手持展示" /><small className={tagInputError ? "field-error" : ""}>{tagInputError || "逗号只用于分隔标签，文件名使用短横线连接；清空后保存可移除全部标签。"}</small></label></div><div><button type="button" className="human-secondary" onClick={() => setTagEditor(null)}>取消</button><button type="button" className="music-tag-confirm" disabled={Boolean(tagInputError)} onClick={saveVideoTags}>保存标签</button></div></section></div>}
    </div>
  </section>;
}
