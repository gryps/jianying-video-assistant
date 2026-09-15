import { useState } from "react";
import { ChevronDown } from "lucide-react";

export function SelectionDropdown<T extends { id: string; name: string }>({ value, options, placeholder, allLabel, disabled = false, onChange }: {
  value: string;
  options: T[];
  placeholder: string;
  allLabel?: string;
  disabled?: boolean;
  onChange: (value: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const selected = options.find(item => item.id === value);
  return <div className="classification-combobox selection-combobox">
    <input readOnly value={selected?.name ?? ""} placeholder={value === "" && allLabel ? allLabel : placeholder} disabled={disabled} aria-haspopup="listbox" aria-expanded={open} onFocus={() => setOpen(true)} onClick={() => setOpen(true)} onKeyDown={event => { if (event.key === "Enter" || event.key === "ArrowDown") setOpen(true); if (event.key === "Escape") setOpen(false); }} onBlur={() => window.setTimeout(() => setOpen(false), 100)} />
    <button type="button" className="classification-combobox-toggle human-secondary" aria-label="展开下拉列表" aria-expanded={open} disabled={disabled} onMouseDown={event => event.preventDefault()} onClick={() => setOpen(current => !current)}><ChevronDown /></button>
    {open && <div className="classification-combobox-options" role="listbox">
      {allLabel && <button type="button" className={value === "" ? "selected" : ""} onMouseDown={event => event.preventDefault()} onClick={() => { onChange(""); setOpen(false); }}>{allLabel}</button>}
      {options.map(item => <button type="button" key={item.id} className={item.id === value ? "selected" : ""} onMouseDown={event => event.preventDefault()} onClick={() => { onChange(item.id); setOpen(false); }}>{item.name}</button>)}
      {!options.length && <span>暂无可选项</span>}
    </div>}
  </div>;
}
