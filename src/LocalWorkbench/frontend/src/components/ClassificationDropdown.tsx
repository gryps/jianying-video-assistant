import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { fuzzyRows } from "../utils/fuzzy";

export function ClassificationDropdown<T extends { id: string | number; name: string }>({ value, options, placeholder, disabled = false, onChange, onSelect }: {
  value: string;
  options: T[];
  placeholder: string;
  disabled?: boolean;
  onChange: (value: string) => void;
  onSelect: (item: T) => void;
}) {
  const [open, setOpen] = useState(false);
  const rows = value.trim() ? fuzzyRows(options, value, item => item.name, 50) : options.slice(0, 5);
  return <div className="classification-combobox">
    <input value={value} placeholder={placeholder} disabled={disabled} autoComplete="off" onFocus={() => setOpen(true)} onChange={event => { onChange(event.target.value); setOpen(true); }} onBlur={() => {
      const exact = options.find(item => item.name.trim().toLocaleLowerCase() === value.trim().toLocaleLowerCase());
      if (exact) onSelect(exact);
      window.setTimeout(() => setOpen(false), 100);
    }} />
    <button type="button" className="classification-combobox-toggle human-secondary" aria-label="展开最近输入" aria-expanded={open} disabled={disabled} onMouseDown={event => event.preventDefault()} onClick={() => setOpen(current => !current)}><ChevronDown /></button>
    {open && <div className="classification-combobox-options">
      {rows.map(item => <button type="button" key={item.id} className={item.name === value ? "selected" : ""} onMouseDown={event => event.preventDefault()} onClick={() => { onSelect(item); setOpen(false); }}>{item.name}</button>)}
      {rows.length === 0 && <span>没有匹配历史，可直接使用当前输入</span>}
    </div>}
  </div>;
}
