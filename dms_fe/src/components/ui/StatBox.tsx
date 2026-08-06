import { type ReactNode } from 'react';
import clsx from 'clsx';

export interface StatBoxProps {
  label: string;
  value: string | number;
  icon?: ReactNode;
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'primary';
  className?: string;
}

export function StatBox({ label, value, icon, variant = 'default', className }: StatBoxProps) {
  const variants = {
    default: 'bg-slate-100 dark:bg-slate-800 text-slate-900 dark:text-slate-100',
    success: 'bg-success-bg text-success dark:bg-green-900/30',
    warning: 'bg-warning-bg text-warning dark:bg-amber-900/30',
    danger: 'bg-danger-bg text-danger dark:bg-red-900/30',
    primary: 'bg-primary-bg text-primary dark:bg-purple-900/30',
  };

  return (
    <div className={clsx('flex items-center gap-3 px-4 py-3 rounded-xl', variants[variant], className)}>
      {icon && <div className="flex-shrink-0">{icon}</div>}
      <div className="min-w-0">
        <p className="text-sm text-slate-500 dark:text-slate-400 truncate">{label}</p>
        <p className="text-xl font-bold text-inherit truncate">{value}</p>
      </div>
    </div>
  );
}
