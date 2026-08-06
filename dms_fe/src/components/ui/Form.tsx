import clsx from 'clsx';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export const Input = ({
  label,
  error,
  leftIcon,
  rightIcon,
  className,
  ...props
}: InputProps) => {
  return (
    <div className="space-y-1.5">
      {label && (
        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
          {label}
        </label>
      )}
      <div className="relative">
        {leftIcon && (
          <div className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
            {leftIcon}
          </div>
        )}
        <input
          className={clsx(
            'w-full px-3 py-2 rounded-xl border bg-white dark:bg-slate-800',
            'text-slate-900 dark:text-slate-100 placeholder-slate-400',
            'focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary',
            'transition-colors duration-200',
            error
              ? 'border-danger focus:ring-danger/50 focus:border-danger'
              : 'border-slate-200 dark:border-slate-700',
            leftIcon && 'pl-10',
            rightIcon && 'pr-10',
            className
          )}
          {...props}
        />
        {rightIcon && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
            {rightIcon}
          </div>
        )}
      </div>
      {error && <p className="text-xs text-danger">{error}</p>}
    </div>
  );
};

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
  options: { value: string | number; label: string }[];
}

export const Select = ({
  label,
  error,
  options,
  className,
  ...props
}: SelectProps) => {
  return (
    <div className="space-y-1.5">
      {label && (
        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
          {label}
        </label>
      )}
      <select
        className={clsx(
          'w-full px-3 py-2 rounded-xl border bg-white dark:bg-slate-800',
          'text-slate-900 dark:text-slate-100',
          'focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary',
          'transition-colors duration-200 cursor-pointer',
          error
            ? 'border-danger focus:ring-danger/50 focus:border-danger'
            : 'border-slate-200 dark:border-slate-700',
          className
        )}
        {...props}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && <p className="text-xs text-danger">{error}</p>}
    </div>
  );
};

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
}

export const Textarea = ({
  label,
  error,
  className,
  ...props
}: TextareaProps) => {
  return (
    <div className="space-y-1.5">
      {label && (
        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
          {label}
        </label>
      )}
      <textarea
        className={clsx(
          'w-full px-3 py-2 rounded-xl border bg-white dark:bg-slate-800',
          'text-slate-900 dark:text-slate-100 placeholder-slate-400',
          'focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary',
          'transition-colors duration-200 resize-none',
          error
            ? 'border-danger focus:ring-danger/50 focus:border-danger'
            : 'border-slate-200 dark:border-slate-700',
          className
        )}
        {...props}
      />
      {error && <p className="text-xs text-danger">{error}</p>}
    </div>
  );
};
