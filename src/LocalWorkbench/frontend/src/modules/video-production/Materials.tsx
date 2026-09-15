import { ChangeEvent, useCallback, useEffect, useRef, useState } from "react";
import { Boxes, LoaderCircle, Trash2 } from "lucide-react";
import { api } from "../../api";
import { ClassificationDropdown } from "../../components/ClassificationDropdown";
import { SelectionDropdown } from "../../components/SelectionDropdown";
import { hasDesktopVideoPicker, openDesktopDirectory, selectDesktopSourceVideos } from "../../desktopBridge";
import { usePersistentOperation } from "../../hooks/usePersistentOperation";
import type { ClassifiedMaterial, Product, ProductCategory, SourceVideo, Tag, TagCategory } from "../../types";
import { fuzzyRows } from "../../utils/fuzzy";
import { ProductManager } from "./ProductManager";
import { TagManager } from "./TagManager";

export function Materials({ products, act }: { products: Product[]; act: (work: () => Promise<unknown>, success: string) => Promise<boolean> }) {
  const desktopPicker = import.meta.env.VITE_DESKTOP_MODE === "1" && hasDesktopVideoPicker();
  const activeProducts = products.filter(item => item.status === "active");
  const [tab, setTab] = useState<"master" | "classify">("master");
  const [productId, setProductId] = useState(0); const [productInput, setProductInput] = useState("");
  const [productCategoryId, setProductCategoryId] = useState(""); const [productCategories, setProductCategories] = useState<ProductCategory[]>([]);
  const [sourceDir, setSourceDir] = useState(""); const [videos, setVideos] = useState<SourceVideo[]>([]);
  const [tags, setTags] = useState<Tag[]>([]); const [categories, setCategories] = useState<TagCategory[]>([]);
  const [selectedTags, setSelectedTags] = useState<Record<string, string[]>>({}); const [selectedVideo, setSelectedVideo] = useState("");
  const [selecting, setSelecting] = useState(false);
  const [deletingVideo, setDeletingVideo] = useState("");
  const videoInputRef = useRef<HTMLInputElement>(null);
  const [classificationResult, setClassificationResult] = useState<ClassifiedMaterial[]>([]);
  const [classificationMessage, setClassificationMessage] = useState("");
  const classificationOperation = usePersistentOperation("material_classification", state => {
    setClassificationMessage(state.status === "completed" ? (state.detail || "原视频归类已完成。") : `原视频归类未完成：${state.detail}`);
  });
  const loadMaster = useCallback(async () => {
    const [categoryRows, tagRows, productCategoryRows] = await Promise.all([api<{ items: TagCategory[] }>("/human/tag-categories?limit=5000"), api<{ items: Tag[] }>("/human/tags?limit=5000"), api<ProductCategory[]>("/products/categories")]);
    setCategories(categoryRows.items); setTags(tagRows.items); setProductCategories(productCategoryRows);
  }, []);
  useEffect(() => { loadMaster().catch(() => { setCategories([]); setTags([]); }); }, [loadMaster]);
  async function uploadVideos(event: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files || []);
    if (!files.length) return;
    setSelecting(true);
    try {
      const form = new FormData();
      files.forEach(file => form.append("files", file));
      await act(() => api<{ path: string; videos: SourceVideo[] }>("/human/source-videos/upload", { method: "POST", body: form }).then(result => {
        setSourceDir(result.path); setVideos(result.videos); setSelectedTags({}); setSelectedVideo("");
        return result;
      }), `已上传 ${files.length} 个视频`);
    } finally { setSelecting(false); }
  }
  async function chooseDesktopVideos() {
    setSelecting(true);
    try {
      const result = await selectDesktopSourceVideos();
      if (!result || !result.videos.length) return;
      setSourceDir(result.sourceDir); setVideos(result.videos); setSelectedTags({}); setSelectedVideo("");
    } catch (reason) {
      await act(() => Promise.reject(reason), "");
    } finally { setSelecting(false); }
  }
  async function deleteVideo(video: SourceVideo) {
    if (deletingVideo) return;
    setDeletingVideo(video.path);
    try {
      const deleted = desktopPicker || await act(() => api(`/human/source-videos?path=${encodeURIComponent(video.path)}`, { method: "DELETE" }), "已删除待归类视频");
      if (!deleted) return;
      setVideos(value => value.filter(item => item.path !== video.path));
      setSelectedVideo(value => value === video.path ? "" : value);
      setSelectedTags(value => {
        const next = { ...value };
        delete next[video.path];
        return next;
      });
      if (videos.length === 1) {
        setSourceDir("");
        if (videoInputRef.current) videoInputRef.current.value = "";
      }
    } finally { setDeletingVideo(""); }
  }
  function toggleTag(tagId: string) {
    if (!selectedVideo) return;
    const selectedTag = tags.find(item => item.id === tagId);
    if (!selectedTag) return;
    setSelectedTags(value => {
      const current = value[selectedVideo] ?? [];
      const alreadySelected = current.includes(tagId);
      const otherCategories = current.filter(id => tags.find(item => item.id === id)?.category_id !== selectedTag.category_id);
      return { ...value, [selectedVideo]: alreadySelected ? otherCategories : [...otherCategories, tagId] };
    });
  }
  const ready = productId > 0 && videos.length > 0 && videos.every(video => (selectedTags[video.path] ?? []).length > 0);
  async function classify() {
    const operationId = classificationOperation.begin();
    if (!operationId) return;
    let completed = false;
    let movedAssets: ClassifiedMaterial[] = [];
    await act(() => api<{ assets: ClassifiedMaterial[] }>("/human/material-classifications", {
      method: "POST", headers: { "X-Operation-Id": operationId }, body: JSON.stringify({
        product_id: productId, source_dir: sourceDir,
        items: videos.map(video => ({ source_path: video.path, tag_ids: selectedTags[video.path] ?? [] })),
      }),
    }).then(value => { movedAssets = value.assets; completed = true; return value; }), "");
    classificationOperation.clear(operationId);
    if (completed) {
      setClassificationMessage("");
      setClassificationResult(movedAssets); setVideos([]); setSelectedTags({}); setSelectedVideo("");
      if (videoInputRef.current) videoInputRef.current.value = "";
    }
  }
  const categoryProducts = productCategoryId ? activeProducts.filter(item => item.category_id === productCategoryId) : [];
  const productMatches = fuzzyRows(categoryProducts, productInput, item => item.name);
  const selectedVideoItem = videos.find(item => item.path === selectedVideo);
  const completedVideoCount = videos.filter(video => (selectedTags[video.path] ?? []).length > 0).length;
  const groupedTags = categories.map(category => ({ category, tags: tags.filter(tag => tag.category_id === category.id) })).filter(group => group.tags.length > 0);
  const classificationDirectory = classificationResult[0]?.source_path.replace(/[\\/][^\\/]+$/, "") ?? "";
  const saveProduct = async () => { const value = productInput.trim(); if (!value || !productCategoryId) return; await act(() => api("/products", { method: "POST", body: JSON.stringify({ name: value, category_id: productCategoryId }) }), "产品名称已单独保存"); setProductInput(""); setProductId(0); };
  return <section className="human-page material-classification-page">
    <div className="material-tabs full"><button className={tab === "master" ? "active" : ""} onClick={() => setTab("master")}>产品与标签管理</button><button className={tab === "classify" ? "active" : ""} onClick={() => setTab("classify")}>素材归类</button></div>
    {tab === "master" && <>
      <div className="master-data-layout full"><ProductManager products={products} act={act} /><TagManager categories={categories} tags={tags} reload={loadMaster} act={act} /></div>
    </>}
    {tab === "classify" && <>
      {classificationMessage && <div className="human-note classification-result-note full" role="status"><Boxes /><div><b>归类操作状态</b><span>{classificationMessage}</span></div></div>}
      {classificationResult.length > 0 && <div className="human-note classification-result-note full"><Boxes /><div><b>本次归类成功</b><span>{classificationResult[0].product_name} · 已移动并重命名 {classificationResult.length} 个原视频</span><small title={classificationDirectory}>{classificationDirectory}</small></div>{desktopPicker && <button type="button" className="human-secondary" onClick={() => openDesktopDirectory(classificationDirectory)}>打开归类目录</button>}</div>}
      <div className="human-card full"><div className="human-card-title"><div><h2>选择产品与视频</h2><p>先选择产品分类，再从该分类中查找产品名称。</p></div></div>
        <div className="classification-master-inputs">
          <label>产品分类<SelectionDropdown value={productCategoryId} options={productCategories} placeholder="选择产品分类" onChange={value => { setProductCategoryId(value); setProductInput(""); setProductId(0); }} /><small>分类用于缩小大量产品名称的选择范围</small></label>
          <label>产品名称<div><ClassificationDropdown value={productInput} options={productMatches} disabled={!productCategoryId} onChange={value => { setProductInput(value); setProductId(0); }} onSelect={item => { setProductInput(item.name); setProductId(item.id); }} placeholder={productCategoryId ? "输入可模糊查询，或展开列表" : "请先选择产品分类"} /><button className="human-secondary" disabled={!productCategoryId} onMouseDown={event => event.preventDefault()} onClick={saveProduct}>新增并保存</button></div><small>{productId ? `已选择：${activeProducts.find(item => item.id === productId)?.name}` : "请选择已保存的产品名称"}</small></label>
          <label>选择视频{desktopPicker ? <div className="desktop-file-picker"><button type="button" className="human-secondary" disabled={selecting} onClick={() => void chooseDesktopVideos()}>{selecting ? "选择中…" : "选择文件"}</button><span>{videos.length ? `已选择 ${videos.length} 个文件` : "未选择文件"}</span></div> : <input ref={videoInputRef} type="file" accept="video/*,.mp4,.mov,.m4v,.avi,.mkv,.webm" multiple disabled={selecting} onChange={uploadVideos} />}<small>{sourceDir ? `归类目录将创建在：${sourceDir}` : "从同一原始目录多选视频，归类文件夹将创建在该目录内"}</small></label>
        </div>
      </div>
      <div className="human-card full"><div className="human-card-title"><h2>逐个视频打标签</h2><span>单选视频后点选标签，未打标签的视频不能归类</span></div>
        <div className="classification-toolbar"><b>{selectedVideoItem ? `当前视频：${selectedVideoItem.name}` : "请先从列表中选择一个视频"}</b><span>已完成 {completedVideoCount}/{videos.length}</span></div>
        {videos.length > 0 && <div className="classification-video-list">{videos.map(video => <article key={video.path} className={selectedVideo === video.path ? "selected" : ""} onClick={() => setSelectedVideo(video.path)}><input type="radio" name="classification-video" aria-label={`选择 ${video.name}`} checked={selectedVideo === video.path} onChange={() => setSelectedVideo(video.path)} onClick={event => event.stopPropagation()} /><div><b>{video.name}</b><small>{video.relative_path}</small><div className="video-applied-tags">{(selectedTags[video.path] ?? []).map(id => tags.find(item => item.id === id)).filter(Boolean).map(tag => <span key={tag!.id}>{tag!.category} / {tag!.name}</span>)}{!(selectedTags[video.path] ?? []).length && <em>未打标签</em>}</div></div><button type="button" className="human-danger classification-video-delete" disabled={Boolean(deletingVideo)} aria-label={`删除 ${video.name}`} onClick={event => { event.stopPropagation(); void deleteVideo(video); }}>{deletingVideo === video.path ? <LoaderCircle className="spin" /> : <Trash2 />}{deletingVideo === video.path ? "删除中" : "删除"}</button></article>)}</div>}
        <div className="classification-tag-context"><b>{selectedVideoItem ? `为“${selectedVideoItem.name}”选择标签` : "选择视频后可点选标签"}</b><span>每个分类只能选择一个标签，再次点击已选标签可取消。</span></div>
        <div className="classification-tag-groups">{groupedTags.map(({ category, tags: categoryTags }) => <section key={category.id}><div className="classification-tag-category"><b>{category.name}</b><span>单选</span></div><div className="tag-picker">{categoryTags.map(tag => { const selected = (selectedTags[selectedVideo] ?? []).includes(tag.id); return <button type="button" key={tag.id} disabled={!selectedVideo} className={selected ? "on" : ""} aria-pressed={selected} onClick={() => toggleTag(tag.id)}>{tag.name}</button>; })}</div></section>)}</div>
        {!groupedTags.length && <div className="classification-tags-empty">尚未创建标签，请先到“产品与标签管理”中添加。</div>}
        <button type="button" className="human-wide" disabled={!ready || classificationOperation.busy} onClick={classify}>{classificationOperation.busy && <LoaderCircle className="spin" />}{classificationOperation.busy ? "正在校验、移动并重命名原视频…" : `确认归类并移动 ${videos.length || ""} 个原视频`}</button>
        {classificationOperation.busy && <div className="copy-generation-progress" role="status" aria-live="polite"><LoaderCircle className="spin" /><div><b>原视频归类正在执行</b><span>刷新页面后仍会保持此状态，完成前请勿重复提交或移动这些文件。</span></div></div>}
      </div>
    </>}
  </section>;
}
