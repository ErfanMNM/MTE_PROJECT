import clsx from 'clsx';

export interface BadgeProps {
  children: React.ReactNode;
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'info' | 'primary';
  size?: 'sm' | 'md';
  className?: string;
}

export function Badge({ children, variant = 'default', size = 'sm', className }: BadgeProps) {
  const variants = {
    default: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
    success: 'bg-success-bg text-success dark:bg-green-900/50 dark:text-green-400',
    warning: 'bg-warning-bg text-warning dark:bg-amber-900/50 dark:text-amber-400',
    danger: 'bg-danger-bg text-danger dark:bg-red-900/50 dark:text-red-400',
    info: 'bg-info-bg text-info dark:bg-blue-900/50 dark:text-blue-400',
    primary: 'bg-primary-bg text-primary dark:bg-purple-900/50 dark:text-purple-400',
  };

  const sizes = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-2.5 py-1 text-sm',
  };

  return (
    <span
      className={clsx(
        'inline-flex items-center font-medium rounded-full',
        variants[variant],
        sizes[size],
        className
      )}
    >
      {children}
    </span>
  );
}
