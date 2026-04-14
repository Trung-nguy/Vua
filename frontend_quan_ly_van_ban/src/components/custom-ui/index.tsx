import React, { useEffect } from 'react'
import { X } from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { STATUS_BADGE_CLASS, STATUS_LABELS } from '@/utils'

// StatusBadge
interface BadgeProps { status: string; className?: string }
export function StatusBadge({ status, className = '' }: BadgeProps) {
  return (
    <span className={`${STATUS_BADGE_CLASS[status] ?? 'badge-draft'} ${className}`}>
      {STATUS_LABELS[status] ?? status}
    </span>
  )
}

// Spinner
export function Spinner({ size = 'md', className = '' }: { size?: 'sm'|'md'|'lg'; className?: string }) {
  const s = size === 'sm' ? 'h-4 w-4' : size === 'lg' ? 'h-8 w-8' : 'h-5 w-5'
  return (
    <svg className={`animate-spin ${s} ${className}`} viewBox="0 0 24 24" fill="none">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3"/>
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
    </svg>
  )
}

// EmptyState
interface EmptyProps { icon?: string; title: string; desc?: string; action?: React.ReactNode }
export function EmptyState({ icon = '📭', title, desc, action }: EmptyProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-6 text-center animate-fade-in">
      <span className="text-5xl mb-4">{icon}</span>
      <h3 className="text-base font-semibold text-ink-700 mb-1">{title}</h3>
      {desc && <p className="text-sm text-ink-400 max-w-xs">{desc}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}

// Pagination
interface PaginationProps {
  trang: number
  tongSoTrang: number
  onChange: (p: number) => void
}
export function Pagination({ trang, tongSoTrang, onChange }: PaginationProps) {
  if (tongSoTrang <= 1) return null
  const pages = Array.from({ length: Math.min(tongSoTrang, 7) }, (_, i) => {
    if (tongSoTrang <= 7) return i + 1
    if (trang <= 4) return i + 1 <= 5 ? i + 1 : tongSoTrang - (6 - i)
    if (trang >= tongSoTrang - 3) return i === 0 ? 1 : tongSoTrang - (6 - i)
    return i === 0 ? 1 : i === 6 ? tongSoTrang : trang - 2 + (i - 1)
  })

  return (
    <div className="flex items-center gap-1">
      <button onClick={() => onChange(trang - 1)} disabled={trang === 1}
        className="btn-ghost btn-icon disabled:opacity-30 text-xs">‹</button>
      {pages.map((p, i) => (
        <button key={i} onClick={() => typeof p === 'number' && onChange(p)}
          className={`btn-icon text-xs ${p === trang ? 'btn-primary' : 'btn-ghost'}`}>
          {p}
        </button>
      ))}
      <button onClick={() => onChange(trang + 1)} disabled={trang === tongSoTrang}
        className="btn-ghost btn-icon disabled:opacity-30 text-xs">›</button>
    </div>
  )
}

// Modal
interface ModalProps {
  open: boolean
  onClose: () => void
  title: string
  children: React.ReactNode
  size?: 'sm' | 'md' | 'lg' | 'xl'
  footer?: React.ReactNode
}
export function Modal({ open, onClose, title, children, size = 'md', footer }: ModalProps) {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    document.addEventListener('keydown', handler)
    return () => document.removeEventListener('keydown', handler)
  }, [onClose])

  const widths = { sm: 'max-w-sm', md: 'max-w-lg', lg: 'max-w-2xl', xl: 'max-w-4xl' }

  return (
    <AnimatePresence>
      {open && (
        <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-4">
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
            className="absolute inset-0 bg-ink-950/40 backdrop-blur-sm" onClick={onClose}/>
          <motion.div
            initial={{ opacity: 0, y: 20, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 20, scale: 0.97 }}
            transition={{ type: 'spring', damping: 28, stiffness: 400 }}
            className={`relative w-full ${widths[size]} card shadow-modal max-h-[90dvh] flex flex-col`}
          >
            <div className="flex items-center justify-between px-6 py-4 border-b border-ink-100">
              <h3 className="text-base font-semibold text-ink-900">{title}</h3>
              <button onClick={onClose} className="btn-ghost btn-icon"><X size={16}/></button>
            </div>
            <div className="flex-1 overflow-y-auto px-6 py-5">{children}</div>
            {footer && <div className="px-6 py-4 border-t border-ink-100 flex justify-end gap-2">{footer}</div>}
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  )
}

// ConfirmDialog
interface ConfirmProps {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  danger?: boolean
  onConfirm: () => void
  onCancel: () => void
  loading?: boolean
}
export function ConfirmDialog({ open, title, message, confirmLabel = 'Xác nhận', danger, onConfirm, onCancel, loading }: ConfirmProps) {
  return (
    <Modal open={open} onClose={onCancel} title={title} size="sm"
      footer={<>
        <button onClick={onCancel} className="btn-secondary">Huỷ</button>
        <button onClick={onConfirm} disabled={loading}
          className={danger ? 'btn-danger' : 'btn-primary'}>
          {loading ? <Spinner size="sm"/> : confirmLabel}
        </button>
      </>}
    >
      <p className="text-sm text-ink-600">{message}</p>
    </Modal>
  )
}

// FormField
import type { ReactNode } from 'react'
interface FormFieldProps { label: string; error?: string; required?: boolean; children: ReactNode; hint?: string }
export function FormField({ label, error, required, children, hint }: FormFieldProps) {
  return (
    <div className="space-y-1.5">
      <label className="label">
        {label} {required && <span className="text-rose-500">*</span>}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-ink-400">{hint}</p>}
      {error && <p className="error-msg">⚠ {error}</p>}
    </div>
  )
}

// Alert
const ALERT_STYLES = {
  info:    'bg-sky-50 border-sky-200 text-sky-800',
  success: 'bg-jade-50 border-jade-200 text-jade-800',
  warning: 'bg-amber-50 border-amber-200 text-amber-700',
  error:   'bg-rose-50 border-rose-200 text-rose-700',
}
interface AlertProps { type: keyof typeof ALERT_STYLES; title?: string; children: React.ReactNode }
export function Alert({ type, title, children }: AlertProps) {
  return (
    <div className={`rounded-xl border px-4 py-3 text-sm ${ALERT_STYLES[type]}`}>
      {title && <p className="font-semibold mb-0.5">{title}</p>}
      <div>{children}</div>
    </div>
  )
}

// Tabs
interface TabsProps {
  tabs: { key: string; label: string; count?: number }[]
  active: string
  onChange: (k: string) => void
}
export function Tabs({ tabs, active, onChange }: TabsProps) {
  return (
    <div className="flex gap-1 p-1 bg-ink-100 rounded-xl">
      {tabs.map(t => (
        <button key={t.key} onClick={() => onChange(t.key)}
          className={`flex-1 flex items-center justify-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-150
            ${active === t.key ? 'bg-white shadow-sm text-ink-900' : 'text-ink-500 hover:text-ink-700'}`}>
          {t.label}
          {t.count !== undefined && (
            <span className={`text-xs px-1.5 py-0.5 rounded-full font-semibold
              ${active === t.key ? 'bg-jade-600 text-white' : 'bg-ink-200 text-ink-500'}`}>
              {t.count}
            </span>
          )}
        </button>
      ))}
    </div>
  )
}
