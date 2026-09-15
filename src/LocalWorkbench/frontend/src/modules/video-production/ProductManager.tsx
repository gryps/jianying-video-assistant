import { FormEvent, useCallback, useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, FolderTree, Search } from "lucide-react";
import { api } from "../../api";
import { ConfirmDeleteDialog } from "../../components/ConfirmDeleteDialog";
import { SelectionDropdown } from "../../components/SelectionDropdown";
import type { DeleteConfirmation, Product, ProductCategory, ProductPage } from "../../types";

const PAGE_SIZE = 20;

export function ProductManager({ act }: {
  products: Product[];
  act: (work: () => Promise<unknown>, success: string) => Promise<boolean>;
}) {
  const [categories, setCategories] = useState<ProductCategory[]>([]);
  const [rows, setRows] = useState<Product[]>([]);
  const [name, setName] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [categoryName, setCategoryName] = useState("");
  const [editingMasterCategoryId, setEditingMasterCategoryId] = useState("");
  const [editingMasterCategoryName, setEditingMasterCategoryName] = useState("");
  const [queryInput, setQueryInput] = useState("");
  const [query, setQuery] = useState("");
  const [filterCategoryId, setFilterCategoryId] = useState("");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editingName, setEditingName] = useState("");
  const [editingCategoryId, setEditingCategoryId] = useState("");
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [confirmation, setConfirmation] = useState<DeleteConfirmation | null>(null);

  const loadCategories = useCallback(async () => {
    setCategories(await api<ProductCategory[]>("/products/categories"));
  }, []);

  const loadPage = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), page_size: String(PAGE_SIZE) });
      if (query) params.set("query", query);
      if (filterCategoryId) params.set("category_id", filterCategoryId);
      const value = await api<ProductPage>(`/products/page?${params}`);
      setRows(value.items);
      setPages(value.pages);
      setTotal(value.total);
      if (page > value.pages) setPage(value.pages);
    } finally {
      setLoading(false);
    }
  }, [filterCategoryId, page, query]);

  useEffect(() => { loadCategories().catch(() => setCategories([])); }, [loadCategories]);
  useEffect(() => { loadPage().catch(() => setRows([])); }, [loadPage]);

  async function createCategory(event: FormEvent) {
    event.preventDefault();
    const nextName = categoryName.trim();
    if (!nextName) return;
    if (await act(() => api("/products/categories", { method: "POST", body: JSON.stringify({ name: nextName }) }), "产品分类已添加")) {
      setCategoryName("");
      await loadCategories();
    }
  }

  async function create(event: FormEvent) {
    event.preventDefault();
    const nextName = name.trim();
    if (!nextName || !categoryId) return;
    if (await act(() => api("/products", { method: "POST", body: JSON.stringify({ name: nextName, category_id: categoryId }) }), "产品名称已添加")) {
      setName("");
      setPage(1);
      await Promise.all([loadCategories(), loadPage()]);
    }
  }

  async function saveCategory(item: ProductCategory) {
    const nextName = editingMasterCategoryName.trim();
    if (!nextName) return;
    if (await act(() => api(`/products/categories/${item.id}`, { method: "PATCH", body: JSON.stringify({ name: nextName }) }), "产品分类已更新")) {
      setEditingMasterCategoryId("");
      await Promise.all([loadCategories(), loadPage()]);
    }
  }

  async function saveProduct(product: Product) {
    const nextName = editingName.trim();
    if (!nextName || !editingCategoryId) return;
    if (await act(() => api(`/products/${product.id}`, { method: "PATCH", body: JSON.stringify({ name: nextName, category_id: editingCategoryId }) }), "产品信息已更新")) {
      setEditingId(null);
      await Promise.all([loadCategories(), loadPage()]);
    }
  }

  return <div className="human-card product-library-card full">
    <div className="human-card-title"><div><h2>产品分类与产品名称</h2><p>先建立稳定的产品分类，再在分类下积累产品名称。</p></div><span>{total} 个产品名称</span></div>

    <div className="product-master-create-grid">
      <form className="product-master-create" onSubmit={createCategory}>
        <div className="product-manager-heading"><FolderTree /><span><b>新增产品分类</b><small>例如服饰、家居、数码</small></span></div>
        <div><input value={categoryName} onChange={event => setCategoryName(event.target.value)} placeholder="输入分类名称" maxLength={80} required /><button>添加分类</button></div>
      </form>
      <form className="product-master-create" onSubmit={create}>
        <div className="product-manager-heading"><span><b>新增产品名称</b><small>产品名称必须归入一个分类</small></span></div>
        <div><SelectionDropdown value={categoryId} options={categories} placeholder="选择产品分类" onChange={setCategoryId} /><input value={name} onChange={event => setName(event.target.value)} placeholder="输入产品名称" maxLength={160} required /><button disabled={!categoryId}>添加产品</button></div>
      </form>
    </div>

    <div className="product-category-list" aria-label="产品分类列表">{categories.map(item => <article key={item.id}>
      <div>{editingMasterCategoryId === item.id ? <input autoFocus value={editingMasterCategoryName} maxLength={80} onChange={event => setEditingMasterCategoryName(event.target.value)} onKeyDown={event => { if (event.key === "Escape") setEditingMasterCategoryId(""); if (event.key === "Enter") saveCategory(item); }} /> : <><b>{item.name}</b><small>{item.product_count} 个产品</small></>}</div>
      <div>{editingMasterCategoryId === item.id ? <><button type="button" onClick={() => saveCategory(item)}>保存</button><button type="button" className="human-secondary" onClick={() => setEditingMasterCategoryId("")}>取消</button></> : <><button type="button" className="human-secondary" onClick={() => { setEditingMasterCategoryId(item.id); setEditingMasterCategoryName(item.name); }}>改名</button><button type="button" className="human-secondary danger" disabled={item.product_count > 0} title={item.product_count > 0 ? "请先调整该分类下的产品" : "删除空分类"} onClick={() => setConfirmation({ title: `删除产品分类“${item.name}”？`, message: "该分类删除后无法恢复。", onConfirm: async () => { const ok = await act(() => api(`/products/categories/${item.id}`, { method: "DELETE" }), "产品分类已删除"); if (ok) await loadCategories(); } })}>删除</button></>}</div>
    </article>)}</div>

    <div className="product-library-toolbar">
      <form onSubmit={event => { event.preventDefault(); setPage(1); setQuery(queryInput.trim()); }}>
        <Search /><input value={queryInput} onChange={event => setQueryInput(event.target.value)} placeholder="搜索产品名称" /><button className="human-secondary">搜索</button>
      </form>
      <SelectionDropdown value={filterCategoryId} options={categories.map(item => ({ ...item, name: `${item.name}（${item.product_count}）` }))} placeholder="全部产品分类" allLabel="全部产品分类" onChange={value => { setFilterCategoryId(value); setPage(1); }} />
    </div>

    <div className="product-table-wrap" aria-busy={loading}>
      <table className="product-table">
        <thead><tr><th>产品分类</th><th>产品名称</th><th>素材数量</th><th>系统编号</th><th>操作</th></tr></thead>
        <tbody>{rows.map(product => <tr key={product.id}>
          <td>{editingId === product.id ? <SelectionDropdown value={editingCategoryId} options={categories} placeholder="选择产品分类" onChange={setEditingCategoryId} /> : <span className="category-pill">{product.category_name}</span>}</td>
          <td>{editingId === product.id ? <input autoFocus value={editingName} maxLength={160} onChange={event => setEditingName(event.target.value)} onKeyDown={event => { if (event.key === "Escape") setEditingId(null); if (event.key === "Enter") { event.preventDefault(); saveProduct(product); } }} /> : <b>{product.name}</b>}</td>
          <td>{product.asset_count}</td><td><code>{product.system_code}</code></td>
          <td><div className="table-actions">{editingId === product.id ? <><button type="button" onClick={() => saveProduct(product)}>保存</button><button type="button" className="human-secondary" onClick={() => setEditingId(null)}>取消</button></> : <button type="button" className="human-secondary" onClick={() => { setEditingId(product.id); setEditingName(product.name); setEditingCategoryId(product.category_id || categories[0]?.id || ""); }}>编辑</button>}<button type="button" className="human-secondary danger" onClick={() => setConfirmation({ title: `删除产品“${product.name}”？`, message: "将删除产品及数据库关联，磁盘上的视频不会移动或改名。", onConfirm: async () => { const ok = await act(() => api(`/human/products/${product.id}`, { method: "DELETE" }), "产品已删除"); if (ok) { await Promise.all([loadCategories(), loadPage()]); } } })}>删除</button></div></td>
        </tr>)}</tbody>
      </table>
      {!loading && rows.length === 0 && <div className="product-library-empty">没有符合条件的产品名称。</div>}
      {loading && <div className="product-library-empty">正在加载产品列表…</div>}
    </div>
    <div className="resource-pagination product-pagination"><span>第 {page} / {pages} 页 · 共 {total} 条</span><div><button className="human-secondary" disabled={page <= 1 || loading} onClick={() => setPage(value => value - 1)}><ChevronLeft />上一页</button><button className="human-secondary" disabled={page >= pages || loading} onClick={() => setPage(value => value + 1)}>下一页<ChevronRight /></button></div></div>
    <ConfirmDeleteDialog confirmation={confirmation} close={() => setConfirmation(null)} />
  </div>;
}
