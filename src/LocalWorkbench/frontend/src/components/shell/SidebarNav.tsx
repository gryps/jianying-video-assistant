import { Film, LogOut, PanelLeftClose, PanelLeftOpen, Settings2 } from "lucide-react";
import type { PlatformModule, View } from "../../types";
import { moduleGroups, videoNav, type ModuleNavItem, type ModuleNavKey } from "./moduleNavigation";

type SidebarNavProps = {
  sidebarCollapsed: boolean;
  module: PlatformModule;
  view: View;
  expandedModules: ModuleNavKey[];
  selectedSecondaryOwner: ModuleNavKey | "";
  username: string;
  onToggleSidebar: () => void;
  onPrimaryModuleClick: (item: ModuleNavItem) => void;
  onSelectVideoView: (owner: ModuleNavKey, key: View) => void;
  onLogout: () => void;
};

export function SidebarNav({ sidebarCollapsed, module, view, expandedModules, selectedSecondaryOwner, username, onToggleSidebar, onPrimaryModuleClick, onSelectVideoView, onLogout }: SidebarNavProps) {
  if (import.meta.env.VITE_DESKTOP_MODE !== "1") return <aside>
    <div className="human-brand"><Film /><span>电商内容平台<small>COMMERCE CONTENT</small></span></div>
    <button type="button" className="human-sidebar-toggle" aria-label={sidebarCollapsed ? "展开导航" : "收起导航"} onClick={onToggleSidebar}>{sidebarCollapsed ? <PanelLeftOpen /> : <PanelLeftClose />}</button>
    <div className="platform-module-switch" aria-label="业务模块">{moduleGroups.map(group => <section className="platform-module-group" key={group.title}><div className="platform-module-group-title">{group.title}</div>{group.items.map(item => <div className="platform-module-item" key={item.key}><button type="button" title={sidebarCollapsed ? item.label : undefined} className={module === item.key ? "active" : ""} onClick={() => onPrimaryModuleClick(item)}><item.Icon /><span>{item.label}</span></button>{item.key === "video" && expandedModules.includes("video") && <nav className="platform-secondary-nav">{videoNav.map(([key, label, Icon]) => <button key={key} title={sidebarCollapsed ? label : undefined} className={selectedSecondaryOwner === "video" && view === key ? "active" : ""} onClick={() => onSelectVideoView("video", key)}><Icon /><span>{label}</span></button>)}</nav>}</div>)}</section>)}</div>
    <button className="human-logout" onClick={onLogout}><LogOut /><span>退出 {username}</span></button>
  </aside>;
  return <aside>
    <div className="human-brand"><Film /><span>剪映视频助手<small>VIDEO WORKSPACE</small></span></div>
    <button type="button" className="human-sidebar-toggle" aria-label={sidebarCollapsed ? "展开导航" : "收起导航"} onClick={onToggleSidebar}>{sidebarCollapsed ? <PanelLeftOpen /> : <PanelLeftClose />}</button>
    <div className="platform-module-switch" aria-label="主导航">
      <section className="platform-module-group">
        <div className="platform-module-group-title">视频制作</div>
        <nav className="platform-direct-nav">{videoNav.filter(([key]) => key !== "flow").map(([key, label, Icon]) => <button key={key} title={sidebarCollapsed ? label : undefined} className={module === "video" && view === key ? "active" : ""} onClick={() => onSelectVideoView("video", key)}><Icon /><span>{label}</span></button>)}</nav>
      </section>
      <section className="platform-module-group">
        <div className="platform-module-group-title">应用设置</div>
        <nav className="platform-direct-nav"><button title={sidebarCollapsed ? "模型配置" : undefined} className={module === "models" ? "active" : ""} onClick={() => onPrimaryModuleClick({ key: "models", label: "模型配置", Icon: Settings2 })}><Settings2 /><span>模型配置</span></button></nav>
      </section>
    </div>
    <button className="human-logout" onClick={onLogout}><LogOut /><span>退出 {username}</span></button>
  </aside>;
}
