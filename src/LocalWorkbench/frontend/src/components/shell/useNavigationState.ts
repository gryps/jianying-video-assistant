import { useEffect, useState } from "react";
import type { PlatformModule, View } from "../../types";
import {
  getActiveTitle,
  getExpandedModule,
  getHeaderSubtitle,
  validExpandedModules,
  type ModuleNavItem,
  type ModuleNavKey,
} from "./moduleNavigation";

const desktop = import.meta.env.VITE_DESKTOP_MODE === "1";
const validModules: PlatformModule[] = desktop ? ["video", "models"] : ["aiVideo", "video", "models"];

const storedPlatformModule = (): PlatformModule => {
  const stored = localStorage.getItem("platform_module");
  return validModules.includes(stored as PlatformModule) ? stored as PlatformModule : "video";
};

const storedExpandedModules = (module: PlatformModule): ModuleNavKey[] => {
  const stored = localStorage.getItem("platform_expanded_modules");
  if (stored) {
    try {
      const parsed = JSON.parse(stored);
      if (Array.isArray(parsed)) return parsed.filter((key): key is ModuleNavKey => validExpandedModules.includes(key));
    } catch {
      localStorage.removeItem("platform_expanded_modules");
    }
  }
  const initial = getExpandedModule(module);
  return initial ? [initial] : [];
};

export function useNavigationState() {
  const [module, setModule] = useState<PlatformModule>(storedPlatformModule);
  const [view, setView] = useState<View>(desktop ? "materials" : "flow");
  const [viewRevision, setViewRevision] = useState(0);
  const [expandedModules, setExpandedModules] = useState<ModuleNavKey[]>(() => storedExpandedModules(module));
  const [selectedSecondaryOwner, setSelectedSecondaryOwner] = useState<ModuleNavKey | "">("");

  useEffect(() => {
    localStorage.setItem("platform_module", module);
  }, [module]);

  useEffect(() => {
    localStorage.setItem("platform_expanded_modules", JSON.stringify(expandedModules));
  }, [expandedModules]);

  const handlePrimaryModuleClick = (item: ModuleNavItem) => {
    if (import.meta.env.VITE_DESKTOP_MODE !== "1" && item.key === "video") {
      setExpandedModules(current => current.includes("video") ? current.filter(key => key !== "video") : [...current, "video"]);
    } else setExpandedModules([]);
    setSelectedSecondaryOwner("");
    setModule(item.key);
  };

  const selectVideoView = (owner: ModuleNavKey, key: View) => {
    setSelectedSecondaryOwner(owner);
    setModule("video");
    setView(key);
    setViewRevision(value => value + 1);
  };

  return {
    module,
    view,
    viewRevision,
    expandedModules,
    selectedSecondaryOwner,
    activeTitle: getActiveTitle(module, view),
    headerSubtitle: getHeaderSubtitle(module),
    handlePrimaryModuleClick,
    selectVideoView,
  };
}
